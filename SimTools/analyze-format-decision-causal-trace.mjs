import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

/*
 * Offline causal trace for the format fork.  The trace deliberately does not
 * estimate new values: it reuses the recorded deterministic priors, memory
 * blends, and noise multipliers.  This keeps the decision-stage accounting
 * reproducible and makes a no-memory counterfactual a telemetry replay.
 *
 * Usage:
 *   node SimTools/analyze-format-decision-causal-trace.mjs <enabled-run> [control-run] [--output file]
 *
 * A run is either a full path without the CSV suffix or a run name in SimLogs.
 */

const logDirectory = path.resolve("SimLogs");
const EPSILON = 1e-5;

function splitCsv(line) {
  const values = []; let value = ""; let quoted = false;
  for (let index = 0; index < line.length; index++) {
    const character = line[index];
    if (character === '"') {
      if (quoted && line[index + 1] === '"') { value += character; index++; }
      else quoted = !quoted;
    } else if (character === "," && !quoted) { values.push(value); value = ""; }
    else value += character;
  }
  values.push(value);
  return values;
}

async function csvRows(file) {
  if (!fs.existsSync(file)) return [];
  const input = fs.createReadStream(file);
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers = null; const rows = [];
  for await (const line of lines) {
    if (headers === null) { headers = splitCsv(line); continue; }
    if (!line) continue;
    const values = splitCsv(line);
    rows.push(Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""])));
  }
  return rows;
}

function runPrefix(run) { return path.isAbsolute(run) ? run : path.join(logDirectory, run); }
function number(value, fallback = 0) { const result = Number(value); return Number.isFinite(result) ? result : fallback; }
function optionalNumber(value) { return value === "" || value === undefined ? null : number(value, null); }
function sum(values) { return values.reduce((total, value) => total + value, 0); }
function mean(values) { return values.length ? sum(values) / values.length : null; }
function ratio(numerator, denominator) { return denominator ? numerator / denominator : null; }
function get(map, key, factory) { if (!map.has(key)) map.set(key, factory()); return map.get(key); }
function compare(a, b) { return a > b; }
function sameChoice(a, b) { return a === b ? a : b; }

function finiteComponents(row) {
  return [row.singlePreTilt, row.albumPreTilt, row.acceptedOpportunity, row.singleTilt, row.albumTilt,
    row.singleProductionCost, row.albumProductionCost, row.marginPerUnit].every(Number.isFinite);
}

function priorComponents(row) {
  // At an opportunity of 1 and a neutral format tilt, compare the Album
  // affinity revenue with the Single contribution.  The remaining components
  // then telescope exactly to priorAlbumNet - priorSingleNet.
  if (!finiteComponents(row) || row.acceptedOpportunity <= EPSILON) return null;
  const baseAlbumAffinityRevenue = row.albumPreTilt / row.acceptedOpportunity * row.marginPerUnit;
  const affinityDemand = baseAlbumAffinityRevenue - row.singlePreTilt;
  const acceptedOpportunity = baseAlbumAffinityRevenue * (row.acceptedOpportunity - 1);
  const formatTilts = baseAlbumAffinityRevenue * row.acceptedOpportunity * (row.albumTilt - 1)
    - row.singlePreTilt * (row.singleTilt - 1);
  const hitInventory = row.weightedHitUnits * row.marginPerUnit;
  const productionCosts = row.singleProductionCost - row.albumProductionCost;
  const total = affinityDemand + acceptedOpportunity + formatTilts + hitInventory + productionCosts;
  return { affinityDemand, acceptedOpportunity, formatTilts, hitInventory, productionCosts, total,
    residual: (row.priorAlbumNet - row.priorSingleNet) - total };
}

async function load(run) {
  const base = runPrefix(run);
  const required = `${base}-fork-ratios.csv`;
  if (!fs.existsSync(required)) throw new Error(`Missing fork-ratios telemetry for ${run}.`);
  const [forkRows, explanationRows, economicRows, memoryRows, outcomeRows] = await Promise.all([
    csvRows(required), csvRows(`${base}-format-decision-explanation.csv`),
    csvRows(`${base}-a3-economic-decisions.csv`), csvRows(`${base}-revenue-memory.csv`),
    csvRows(`${base}-release-outcomes.csv`)
  ]);
  const explanationById = new Map(explanationRows.map(row => [row.recordId, row]));
  const economicById = new Map(economicRows.map(row => [row.recordId, row]));
  const rows = [];
  for (const fork of forkRows) {
    const explanation = explanationById.get(fork.recordId);
    const economic = economicById.get(fork.recordId);
    if (!explanation || !economic) throw new Error(`${run} lacks joined explanation/economic telemetry for ${fork.recordId}.`);
    const singleTilt = number(explanation.singleFormatTilt, 1) || 1;
    const albumTilt = number(explanation.albumFormatTilt, 1) || 1;
    const reportedSingleCost = optionalNumber(explanation.singleProductionCost);
    const albumCost = optionalNumber(explanation.albumProductionCost);
    const priorSingleNet = number(fork.priorSingleNet);
    const priorAlbumNet = number(fork.priorAlbumNet);
    const totalExpectedUnits = number(economic.totalExpectedAlbumUnits);
    const usableAlbumCost = albumCost ?? 0;
    const marginPerUnit = totalExpectedUnits > EPSILON ? (priorAlbumNet + usableAlbumCost) / totalExpectedUnits : null;
    const explanationPreTilt = optionalNumber(explanation.albumPreTiltContribution);
    const affinityUnits = number(economic.affinityUnits);
    const albumPreTilt = explanationPreTilt !== null && explanationPreTilt > EPSILON
      ? explanationPreTilt : affinityUnits / albumTilt;
    const explanationOpportunity = optionalNumber(explanation.acceptedOpportunity);
    // Older control logs predate the explicit explanation fields.  Their
    // recorded Album demand factor is the same accepted opportunity at neutral
    // tilt, so it remains usable for the accounting fallback.
    const acceptedOpportunity = explanationOpportunity !== null && explanationOpportunity > EPSILON
      ? explanationOpportunity : number(economic.albumDemandFactor, null);
    const singlePreTilt = optionalNumber(explanation.singlePreTiltContribution);
    // The first version of this stream omitted singleProductionCost for the
    // orphan-Single telemetry initializer. Recover it from the frozen prior
    // identity for those legacy rows; current telemetry emits it explicitly.
    const inferredSingleCost = singlePreTilt !== null && singlePreTilt > EPSILON
      ? singlePreTilt * singleTilt - priorSingleNet : null;
    const singleCost = reportedSingleCost !== null && Math.abs(reportedSingleCost) > EPSILON
      ? reportedSingleCost : inferredSingleCost;
    rows.push({
      run, week: number(fork.week), year: number(fork.year), recordId: fork.recordId, labelId: fork.labelId,
      artistId: fork.artistId, genre: fork.genre, careerBand: fork.careerBand, qualityQuartile: fork.qualityQuartile,
      chosenFormat: fork.chosenFormat, priorSingleNet, priorAlbumNet,
      finalSingleMargin: number(explanation.finalSingleMargin, number(fork.projectedSingleNet)),
      finalAlbumMargin: number(explanation.finalAlbumMargin, number(fork.projectedAlbumNet)),
      singleMemoryBlend: number(explanation.singleMemoryBlend, priorSingleNet),
      albumMemoryBlend: number(explanation.albumMemoryBlend, priorAlbumNet),
      singleMemoryEma: number(explanation.singleMemoryEma), albumMemoryEma: number(explanation.albumMemoryEma),
      confidenceSingle: number(explanation.confidenceSingle), confidenceAlbum: number(explanation.confidenceAlbum),
      singleNoise: number(explanation.singleNoise, 1) || 1, albumNoise: number(explanation.albumNoise, 1) || 1,
      singlePreTilt: singlePreTilt !== null && singlePreTilt > EPSILON ? singlePreTilt : null,
      albumPreTilt, acceptedOpportunity, singleTilt, albumTilt, singleProductionCost: singleCost,
      albumProductionCost: albumCost, weightedHitUnits: number(economic.weightedHitUnits), totalExpectedUnits, marginPerUnit
    });
  }
  const decisionById = new Map(rows.map(row => [row.recordId, row]));
  const outcomes = outcomeRows.filter(row => row.memoryEligible === "true").map(row => ({
    week: number(row.week), labelId: row.labelId, recordId: row.recordId, format: row.format,
    realizedNet: number(row.realizedNet), decision: decisionById.get(row.recordId) ?? null
  }));
  return { run, rows, memoryRows, outcomes };
}

function stageSummary(rows) {
  const prior = rows.map(row => compare(row.priorAlbumNet, row.priorSingleNet));
  const memory = rows.map(row => compare(row.albumMemoryBlend, row.singleMemoryBlend));
  const final = rows.map(row => compare(row.finalAlbumMargin, row.finalSingleMargin));
  const albums = values => values.filter(Boolean).length;
  return {
    decisions: rows.length,
    deterministicPriorAlbumShare: ratio(albums(prior), rows.length),
    memoryBlendedAlbumShare: ratio(albums(memory), rows.length),
    finalAlbumShare: ratio(albums(final), rows.length),
    selectedAlbumShare: ratio(rows.filter(row => row.chosenFormat === "Album").length, rows.length),
    priorToMemoryFlips: rows.filter((_, index) => prior[index] !== memory[index]).length,
    memoryToFinalFlips: rows.filter((_, index) => memory[index] !== final[index]).length,
    priorToFinalFlips: rows.filter((_, index) => prior[index] !== final[index]).length,
    meanPriorAlbumMinusSingle: mean(rows.map(row => row.priorAlbumNet - row.priorSingleNet)),
    meanMemoryAlbumMinusSingle: mean(rows.map(row => row.albumMemoryBlend - row.singleMemoryBlend)),
    meanFinalAlbumMinusSingle: mean(rows.map(row => row.finalAlbumMargin - row.finalSingleMargin)),
    meanMemoryMarginShift: mean(rows.map(row => (row.albumMemoryBlend - row.singleMemoryBlend) - (row.priorAlbumNet - row.priorSingleNet)))
  };
}

function componentSummary(rows) {
  const components = rows.map(priorComponents).filter(Boolean);
  const keys = ["affinityDemand", "acceptedOpportunity", "formatTilts", "hitInventory", "productionCosts", "total", "residual"];
  const result = {
    rowsWithExactComponentInputs: components.length,
    excludedRows: rows.length - components.length,
    zeroAcceptedOpportunityRows: rows.filter(row => Number.isFinite(row.acceptedOpportunity) && row.acceptedOpportunity <= EPSILON).length,
    meanSinglePreTiltContribution: mean(rows.filter(row => Number.isFinite(row.singlePreTilt)).map(row => row.singlePreTilt)),
    meanAlbumPreTiltContribution: mean(rows.filter(row => Number.isFinite(row.albumPreTilt)).map(row => row.albumPreTilt)),
    meanAcceptedOpportunityInput: mean(rows.filter(row => Number.isFinite(row.acceptedOpportunity)).map(row => row.acceptedOpportunity)),
    meanSingleFormatTilt: mean(rows.filter(row => Number.isFinite(row.singleTilt)).map(row => row.singleTilt)),
    meanAlbumFormatTilt: mean(rows.filter(row => Number.isFinite(row.albumTilt)).map(row => row.albumTilt)),
    meanSingleProductionCostInput: mean(rows.filter(row => Number.isFinite(row.singleProductionCost)).map(row => row.singleProductionCost)),
    meanAlbumProductionCostInput: mean(rows.filter(row => Number.isFinite(row.albumProductionCost)).map(row => row.albumProductionCost)),
    meanWeightedHitUnitsInput: mean(rows.map(row => row.weightedHitUnits))
  };
  for (const key of keys) result[`mean${key[0].toUpperCase()}${key.slice(1)}`] = mean(components.map(component => component[key]));
  return result;
}

function byGenre(rows) {
  const groups = new Map();
  for (const row of rows) get(groups, row.genre, () => []).push(row);
  return [...groups].map(([genre, group]) => ({ genre, stages: stageSummary(group), priorDecomposition: componentSummary(group) }))
    .sort((a, b) => a.genre.localeCompare(b.genre));
}

function nativeSoulTrace(data) {
  const rows = data.rows.filter(row => row.genre === "Soul").sort((a, b) => a.week - b.week || a.recordId.localeCompare(b.recordId));
  const outcomesByLabelFormat = new Map();
  for (const outcome of data.outcomes) {
    const key = `${outcome.labelId}|${outcome.format}`;
    get(outcomesByLabelFormat, key, () => []).push(outcome);
  }
  for (const outcomes of outcomesByLabelFormat.values()) outcomes.sort((a, b) => a.week - b.week || a.recordId.localeCompare(b.recordId));
  const trace = rows.map(row => {
    const knownBefore = format => (outcomesByLabelFormat.get(`${row.labelId}|${format}`) ?? [])
      .filter(outcome => outcome.week < row.week);
    const summarizeObserved = format => {
      const observations = knownBefore(format);
      const native = observations.filter(outcome => outcome.decision?.genre === "Soul").length;
      const other = observations.filter(outcome => outcome.decision && outcome.decision.genre !== "Soul").length;
      const unknown = observations.filter(outcome => !outcome.decision).length;
      return { native, other, unknown };
    };
    const singleObserved = summarizeObserved("Single"), albumObserved = summarizeObserved("Album");
    const noMemorySingle = row.priorSingleNet * row.singleNoise;
    const noMemoryAlbum = row.priorAlbumNet * row.albumNoise;
    return {
      week: row.week, labelId: row.labelId, recordId: row.recordId, careerBand: row.careerBand,
      priorSingleNet: row.priorSingleNet, priorAlbumNet: row.priorAlbumNet,
      singleMemoryEma: row.singleMemoryEma, albumMemoryEma: row.albumMemoryEma,
      confidenceSingle: row.confidenceSingle, confidenceAlbum: row.confidenceAlbum,
      singleMemoryBlend: row.singleMemoryBlend, albumMemoryBlend: row.albumMemoryBlend,
      finalSingleMargin: row.finalSingleMargin, finalAlbumMargin: row.finalAlbumMargin,
      chosenFormat: row.chosenFormat, noMemoryChoice: noMemoryAlbum > noMemorySingle ? "Album" : "Single",
      noMemoryFinalSingleMargin: noMemorySingle, noMemoryFinalAlbumMargin: noMemoryAlbum,
      knownPreDecisionObservations: { single: singleObserved, album: albumObserved }
    };
  });
  const noMemoryFlips = trace.filter(row => row.noMemoryChoice !== row.chosenFormat);
  const memoryLed = trace.filter(row => (row.confidenceSingle > 0 || row.confidenceAlbum > 0) &&
    ((row.priorAlbumNet > row.priorSingleNet) !== (row.albumMemoryBlend > row.singleMemoryBlend)));
  const crossGenreOnly = trace.filter(row => {
    const observed = [row.knownPreDecisionObservations.single, row.knownPreDecisionObservations.album];
    return observed.some(value => value.other > 0) && observed.every(value => value.native === 0);
  });
  return {
    cohort: "Native Soul (raw primary genre Soul)", stages: stageSummary(rows), decisionsWithPositiveMemoryConfidence: trace.filter(row => row.confidenceSingle > 0 || row.confidenceAlbum > 0).length,
    memoryLedDecisionFlips: memoryLed.length, noMemoryCounterfactual: {
      replayedNoise: true, changedChoices: noMemoryFlips.length, albumChoices: trace.filter(row => row.noMemoryChoice === "Album").length,
      changedRows: noMemoryFlips.map(row => ({ week: row.week, labelId: row.labelId, recordId: row.recordId, observed: row.chosenFormat, noMemory: row.noMemoryChoice }))
    },
    knownCrossGenreOnlyMemoryLowerBound: {
      decisions: crossGenreOnly.length,
      note: "Uses only in-run memory-eligible outcomes completed before the decision week; unknown pre-run observations are intentionally not attributed.",
      rows: crossGenreOnly.map(row => ({ week: row.week, labelId: row.labelId, recordId: row.recordId,
        single: row.knownPreDecisionObservations.single, album: row.knownPreDecisionObservations.album }))
    },
    byLabel: [...new Map(trace.map(row => [row.labelId, []])).entries()].map(([labelId]) => ({
      labelId, decisions: trace.filter(row => row.labelId === labelId)
    })).sort((a, b) => a.labelId.localeCompare(b.labelId))
  };
}

function summarizeRun(data) {
  return { run: data.run, allDecisions: stageSummary(data.rows), byGenre: byGenre(data.rows), nativeSoul: nativeSoulTrace(data) };
}

function comparison(enabled, control) {
  const genres = new Set([...enabled.rows.map(row => row.genre), ...control.rows.map(row => row.genre)]);
  const byTargetGenre = [...genres].map(genre => {
    const enabledRows = enabled.rows.filter(row => row.genre === genre), controlRows = control.rows.filter(row => row.genre === genre);
    return { genre, enabled: { stages: stageSummary(enabledRows), priorDecomposition: componentSummary(enabledRows) },
      control: { stages: stageSummary(controlRows), priorDecomposition: componentSummary(controlRows) } };
  }).sort((a, b) => a.genre.localeCompare(b.genre));
  return { enabledRun: enabled.run, controlRun: control.run, byGenre: byTargetGenre };
}

const args = process.argv.slice(2);
const outputIndex = args.indexOf("--output");
const output = outputIndex >= 0 ? args[outputIndex + 1] : null;
if (outputIndex >= 0) args.splice(outputIndex, 2);
if (!args.length || args.length > 2) throw new Error("Usage: node SimTools/analyze-format-decision-causal-trace.mjs <enabled-run> [control-run] [--output file]");
const enabled = await load(args[0]);
const control = args[1] ? await load(args[1]) : null;
const result = {
  schema: "format-decision-causal-trace/v1",
  accounting: {
    deterministicPrior: "priorAlbum - priorSingle = affinity/demand + accepted opportunity + format tilts + hit inventory + production costs",
    memory: "recorded blend = lerp(deterministic prior, label-format EMA, recorded confidence)",
    final: "recorded final margin = recorded blend * recorded format noise",
    scope: "All counterfactuals are offline replays; no simulator state is changed."
  },
  enabled: summarizeRun(enabled),
  control: control ? summarizeRun(control) : null,
  enabledControlComparison: control ? comparison(enabled, control) : null
};
const text = `${JSON.stringify(result, null, 2)}\n`;
if (output) fs.writeFileSync(output, text);
process.stdout.write(text);
