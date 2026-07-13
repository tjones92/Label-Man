import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

/*
 * Late-decade Single opportunity diagnostic.
 *
 * Usage:
 *   node SimTools/analyze-late-single-opportunity.mjs <enabled-run> <control-run>
 *
 * The annual count/yield bridge is exact from decade-annual-rollup.csv. When
 * bounded record-genre explanation rows are present, the script also reports a
 * same-observation legacy-transfer counterfactual. That counterfactual is a
 * diagnostic sample, not an acceptance result: lean runs intentionally emit
 * only the explanation header.
 */

const logDirectory = path.resolve("SimLogs");

function splitCsv(line) {
  const values = [];
  let value = "";
  let quoted = false;
  for (let index = 0; index < line.length; index++) {
    const character = line[index];
    if (character === '"') {
      if (quoted && line[index + 1] === '"') { value += character; index++; }
      else quoted = !quoted;
    } else if (character === "," && !quoted) {
      values.push(value);
      value = "";
    } else value += character;
  }
  values.push(value);
  return values;
}

function prefix(run) { return path.isAbsolute(run) ? run : path.join(logDirectory, run); }
function number(value, fallback = 0) { const result = Number(value); return Number.isFinite(result) ? result : fallback; }
function ratio(numerator, denominator) { return denominator ? numerator / denominator : null; }

async function visitRows(file, visitor) {
  const input = fs.createReadStream(file);
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers = null;
  for await (const line of lines) {
    if (headers === null) { headers = splitCsv(line); continue; }
    if (!line) continue;
    const values = splitCsv(line);
    visitor(Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""])));
  }
}

async function loadRollup(run) {
  const result = new Map();
  await visitRows(`${prefix(run)}-decade-annual-rollup.csv`, row => result.set(number(row.year), row));
  return result;
}

async function loadSingleIds(run) {
  const result = new Set();
  await visitRows(`${prefix(run)}-release-strategy.csv`, row => {
    if (row.chosenFormat === "Single") result.add(row.recordId);
  });
  return result;
}

function emptyExplanationYear() {
  return { rows: 0, weight: 0, enabledDemandSeam: 0, legacyDemandSeam: 0,
    finalAcceptance: 0, legacyAcceptance: 0, enabledMultiplier: 0,
    legacyMultiplier: 0, transferRatio: 0, formatTilt: 0, catalogBaseline: 0,
    segmentRouted: 0, legacyMomentum: 0, momentumContribution: 0, clampDelta: 0 };
}

async function loadExplanation(run, singleIds) {
  const file = `${prefix(run)}-record-genre-explanation.csv`;
  const years = new Map();
  if (!fs.existsSync(file)) return years;
  await visitRows(file, row => {
    if (!singleIds.has(row.recordId)) return;
    const year = number(row.year);
    const target = years.get(year) ?? emptyExplanationYear();
    const weight = Math.max(0, number(row.salesEffectiveAwareness))
      * Math.max(0, number(row.chartVisibilityMultiplier, 1))
      * Math.max(0, number(row.radioSalesMultiplier, 1))
      * Math.max(0, number(row.sentimentMultiplier, 1))
      * Math.max(0, number(row.awardMultiplier, 1))
      * Math.max(0, number(row.distributionMultiplier, 1))
      * Math.max(0, number(row.conversionSeasonalityMultiplier, 1));
    const enabledMultiplier = number(row.enabledSingleDemandMultiplier);
    const legacyMultiplier = number(row.legacySingleDemandMultiplier);
    const formatTilt = number(row.formatTilt, 1);
    target.rows++;
    target.weight += weight;
    target.enabledDemandSeam += weight * enabledMultiplier * formatTilt;
    target.legacyDemandSeam += weight * legacyMultiplier;
    target.finalAcceptance += number(row.finalAcceptance);
    target.legacyAcceptance += number(row.legacyAcceptanceComparator);
    target.enabledMultiplier += enabledMultiplier;
    target.legacyMultiplier += legacyMultiplier;
    target.transferRatio += number(row.singleDemandTransferRatio);
    target.formatTilt += formatTilt;
    target.catalogBaseline += number(row.catalogBaselineAcceptance);
    target.segmentRouted += number(row.segmentRoutedAcceptance);
    target.legacyMomentum += number(row.legacyMomentum);
    target.momentumContribution += number(row.legacyMomentumAcceptanceContribution);
    target.clampDelta += number(row.acceptanceClampDelta);
    years.set(year, target);
  });
  return years;
}

function annualBridge(enabled, control) {
  const result = [];
  for (const year of [...enabled.keys()].sort()) {
    const e = enabled.get(year);
    const c = control.get(year);
    if (!c) continue;
    const enabledSingleDecisions = number(e.decisions) * (1 - number(e.albumDecisionShare));
    const controlSingleDecisions = number(c.decisions) * (1 - number(c.albumDecisionShare));
    const enabledAlbumDecisions = number(e.decisions) * number(e.albumDecisionShare);
    const controlAlbumDecisions = number(c.decisions) * number(c.albumDecisionShare);
    const enabledUnits = number(e.singleUnits) + number(e.albumUnits);
    const controlUnits = number(c.singleUnits) + number(c.albumUnits);
    result.push({
      year,
      unitRatio: ratio(enabledUnits, controlUnits),
      singleUnitRatio: ratio(number(e.singleUnits), number(c.singleUnits)),
      albumUnitRatio: ratio(number(e.albumUnits), number(c.albumUnits)),
      singleDecisionRatio: ratio(enabledSingleDecisions, controlSingleDecisions),
      singleUnitsPerDecisionRatio: ratio(number(e.singleUnits) / enabledSingleDecisions, number(c.singleUnits) / controlSingleDecisions),
      albumDecisionRatio: ratio(enabledAlbumDecisions, controlAlbumDecisions),
      albumUnitsPerDecisionRatio: ratio(number(e.albumUnits) / enabledAlbumDecisions, number(c.albumUnits) / controlAlbumDecisions)
    });
  }
  return result;
}

function explanationSummary(years) {
  return [...years.entries()].sort(([left], [right]) => left - right).map(([year, row]) => ({
    year,
    rows: row.rows,
    finalAcceptanceMean: ratio(row.finalAcceptance, row.rows),
    legacyAcceptanceMean: ratio(row.legacyAcceptance, row.rows),
    enabledMultiplierMean: ratio(row.enabledMultiplier, row.rows),
    legacyMultiplierMean: ratio(row.legacyMultiplier, row.rows),
    transferRatioMean: ratio(row.transferRatio, row.rows),
    formatTiltMean: ratio(row.formatTilt, row.rows),
    catalogBaselineMean: ratio(row.catalogBaseline, row.rows),
    segmentRoutedMean: ratio(row.segmentRouted, row.rows),
    legacyMomentumMean: ratio(row.legacyMomentum, row.rows),
    momentumContributionMean: ratio(row.momentumContribution, row.rows),
    clampDeltaMean: ratio(row.clampDelta, row.rows),
    exposureWeightedDemandSeamRatio: ratio(row.enabledDemandSeam, row.legacyDemandSeam)
  }));
}

const [enabledRun, controlRun] = process.argv.slice(2);
if (!enabledRun || !controlRun) throw new Error("Usage: node SimTools/analyze-late-single-opportunity.mjs <enabled-run> <control-run>");

const [enabledRollup, controlRollup, singleIds] = await Promise.all([
  loadRollup(enabledRun), loadRollup(controlRun), loadSingleIds(enabledRun)
]);
const explanations = await loadExplanation(enabledRun, singleIds);
console.log(JSON.stringify({
  schema: "late-single-opportunity/v1",
  enabledRun,
  controlRun,
  annualBridge: annualBridge(enabledRollup, controlRollup),
  explanationSample: explanationSummary(explanations),
  caveat: "The explanation sample uses bounded Top-40/launch rows and an exposure proxy. The annual bridge is exact."
}, null, 2));
