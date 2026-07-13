import fs from "node:fs";
import path from "node:path";

/*
 * Offline bridge from supply allocation to realized economics.
 *
 * Usage:
 *   node SimTools/analyze-supply-economic-bridge.mjs <enabled-run> <control-run> [--output file]
 *
 * Inputs are audit CSVs only. No simulator values are fitted or recomputed.
 */

const logDirectory = path.resolve("SimLogs");
const EPSILON = 1e-9;

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

function csvRows(file, required = true) {
  if (!fs.existsSync(file)) {
    if (required) throw new Error(`Missing required telemetry: ${file}`);
    return [];
  }
  const lines = fs.readFileSync(file, "utf8").trim().split(/\r?\n/);
  if (!lines.length || !lines[0]) return [];
  const headers = splitCsv(lines.shift());
  return lines.filter(Boolean).map(line => {
    const values = splitCsv(line);
    return Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
  });
}

function prefix(run) { return path.isAbsolute(run) ? run : path.join(logDirectory, run); }
function number(value, fallback = 0) { const result = Number(value); return Number.isFinite(result) ? result : fallback; }
function sum(values) { return values.reduce((total, value) => total + value, 0); }
function ratio(numerator, denominator) { return denominator ? numerator / denominator : null; }
function get(map, key, create) { if (!map.has(key)) map.set(key, create()); return map.get(key); }
function mean(values) { return values.length ? sum(values) / values.length : null; }
function reachBucket(reach) { return reach < .75 ? "low(<0.75)" : reach < 1.25 ? "mid(0.75-1.25)" : "high(>=1.25)"; }
function key(...parts) { return parts.join("|"); }

function mapLegacy(genre, year) {
  switch (genre) {
    case "Psychedelic": return "PsychedelicRock";
    case "BritishInvasion": return "BritishBeat";
    case "Motown": return "Soul";
    case "GirlGroup": return "TeenPop";
    case "Skiffle": return "Folk";
    case "SkaRocksteady": return year <= 1965 ? "Ska" : year <= 1967 ? "Rocksteady" : "Reggae";
    default: return genre;
  }
}

// Mirrors GenreMigration.Canonicalize. Girl Group's destination depends on
// the unmapped secondary identity retained in fork telemetry.
function canonicalGenre(primary, secondary, year) {
  if (primary === "GirlGroup") return secondary === "Soul" || secondary === "RnB" ? "Soul" : "TeenPop";
  return mapLegacy(primary, year);
}

function annualMarketNetPerUnit(rows) {
  const result = new Map();
  for (const row of rows) {
    if (row.period !== "annual" || row.labelTier !== "All" || !["Single", "Album"].includes(row.releaseFormat)) continue;
    result.set(row.releaseFormat, ratio(number(row.marketNet), number(row.totalMarketUnits)) ?? 0);
  }
  return result;
}

function sourceFromDecision(row, selection, enabled) {
  if (!enabled) return row.genre;
  if (selection?.artistIdentity) return selection.artistIdentity;
  // The corrected fork stream preserves the original identity as the secondary
  // genre for routed projects. Week-one pre-live decisions have no selection.
  return row.rawSecondaryGenre || row.genre;
}

function load(run, enabled) {
  const base = prefix(run);
  const forks = csvRows(`${base}-fork-ratios.csv`);
  const details = csvRows(`${base}-format-decision-cohort-details.csv`);
  const live = csvRows(`${base}-live-records-snapshot.csv`);
  const outcomes = csvRows(`${base}-release-outcomes.csv`);
  const marketRevenue = csvRows(`${base}-market-revenue.csv`);
  const strategies = csvRows(`${base}-release-strategy.csv`);
  const selections = csvRows(`${base}-supply-selections.csv`, false);
  const detailById = new Map(details.map(row => [row.recordId, row]));
  const liveById = new Map(live.map(row => [row.recordId, row]));
  const outcomeById = new Map(outcomes.map(row => [row.recordId, row]));
  const strategyById = new Map(strategies.map(row => [row.recordId, row]));
  const selectionByAttempt = new Map();
  for (const selection of selections) {
    const attempt = key(selection.week, selection.labelId, selection.artistId);
    if (selectionByAttempt.has(attempt)) throw new Error(`${run} has multiple supply selections for ${attempt}.`);
    selectionByAttempt.set(attempt, selection);
  }
  const formatMarketNetPerUnit = annualMarketNetPerUnit(marketRevenue);
  const rows = forks.map(fork => {
    const selection = selectionByAttempt.get(key(fork.week, fork.labelId, fork.artistId));
    const detail = detailById.get(fork.recordId);
    const active = liveById.get(fork.recordId);
    const outcome = outcomeById.get(fork.recordId);
    const format = fork.chosenFormat;
    const units = detail ? number(detail.realizedUnits) : active ? number(active.totalUnitsSold) : 0;
    const observedLabelNet = outcome ? number(outcome.realizedNet) : active ? number(active.observedNetLowerBound) : 0;
    const sourceGenre = canonicalGenre(sourceFromDecision(fork, selection, enabled), "", number(fork.year));
    const mode = enabled ? (selection?.selectionMode ?? "PreLiveOrUnmatched") : "ControlIdentity";
    const destinationGenre = canonicalGenre(fork.genre, fork.rawSecondaryGenre, number(fork.year));
    return {
      run, enabled, recordId: fork.recordId, week: number(fork.week), year: number(fork.year), labelId: fork.labelId,
      artistId: fork.artistId, sourceGenre, destinationGenre, mode, format, careerBand: fork.careerBand,
      qualityQuartile: fork.qualityQuartile, reachBucket: reachBucket(number(fork.reachFactor)),
      units, observedLabelNet, allocatedMarketNet: units * (formatMarketNetPerUnit.get(format) ?? 0),
      realized: Boolean(detail || active), financeObserved: Boolean(outcome || active),
      releaseStrategyMatched: strategyById.has(fork.recordId), selectionMatched: Boolean(selection),
      selectionDestinationMatched: !selection || canonicalGenre(selection.chosenProjectGenre, "", number(fork.year)) === destinationGenre,
      transition: `${sourceGenre} -> ${destinationGenre}`
    };
  });
  return { run, enabled, rows, selections, formatMarketNetPerUnit };
}

function rollup(rows, selector) {
  const groups = new Map();
  for (const row of rows) {
    const group = get(groups, selector(row), () => ({ projects: 0, units: 0, observedLabelNet: 0, allocatedMarketNet: 0,
      realizedProjects: 0, financeObservedProjects: 0 }));
    group.projects++; group.units += row.units; group.observedLabelNet += row.observedLabelNet; group.allocatedMarketNet += row.allocatedMarketNet;
    group.realizedProjects += row.realized ? 1 : 0; group.financeObservedProjects += row.financeObserved ? 1 : 0;
  }
  for (const group of groups.values()) group.unitsPerProject = ratio(group.units, group.projects) ?? 0;
  return groups;
}

function transitionRows(rows) {
  const groups = rollup(rows, row => key(row.sourceGenre, row.destinationGenre, row.mode, row.format));
  return [...groups].map(([groupKey, value]) => {
    const [sourceGenre, destinationGenre, mode, format] = groupKey.split("|");
    return { sourceGenre, destinationGenre, transition: `${sourceGenre} -> ${destinationGenre}`, mode, format, ...value };
  }).sort((a, b) => b.units - a.units || a.transition.localeCompare(b.transition) || a.format.localeCompare(b.format));
}

function modeRows(rows) {
  const groups = rollup(rows, row => key(row.mode, row.format));
  return [...groups].map(([groupKey, value]) => {
    const [mode, format] = groupKey.split("|"); return { mode, format, ...value };
  }).sort((a, b) => a.mode.localeCompare(b.mode) || a.format.localeCompare(b.format));
}

function combinedCohorts(enabledRows, controlRows) {
  const e = rollup(enabledRows, row => key(row.destinationGenre, row.format));
  const c = rollup(controlRows, row => key(row.destinationGenre, row.format));
  const keys = new Set([...e.keys(), ...c.keys()]);
  return [...keys].map(groupKey => {
    const [genre, format] = groupKey.split("|");
    const enabled = e.get(groupKey) ?? { projects: 0, units: 0, unitsPerProject: 0, allocatedMarketNet: 0, observedLabelNet: 0, realizedProjects: 0, financeObservedProjects: 0 };
    const control = c.get(groupKey) ?? { projects: 0, units: 0, unitsPerProject: 0, allocatedMarketNet: 0, observedLabelNet: 0, realizedProjects: 0, financeObservedProjects: 0 };
    const countAllocationEffect = (enabled.projects - control.projects) * control.unitsPerProject;
    const realizedYieldEffect = enabled.projects * (enabled.unitsPerProject - control.unitsPerProject);
    return {
      genre, format, support: enabled.projects && control.projects ? "supported" : enabled.projects ? "enabled-only" : "control-only",
      enabledProjects: enabled.projects, controlProjects: control.projects, enabledUnits: enabled.units, controlUnits: control.units,
      unitDelta: enabled.units - control.units, enabledUnitsPerProject: enabled.unitsPerProject, controlUnitsPerProject: control.unitsPerProject,
      countAllocationEffect, realizedYieldEffect, reconciliationResidual: (enabled.units - control.units) - countAllocationEffect - realizedYieldEffect,
      enabledAllocatedMarketNet: enabled.allocatedMarketNet, controlAllocatedMarketNet: control.allocatedMarketNet,
      allocatedMarketNetDelta: enabled.allocatedMarketNet - control.allocatedMarketNet,
      enabledObservedLabelNet: enabled.observedLabelNet, controlObservedLabelNet: control.observedLabelNet,
      observedLabelNetDelta: enabled.observedLabelNet - control.observedLabelNet,
      enabledRealizedProjects: enabled.realizedProjects, controlRealizedProjects: control.realizedProjects,
      enabledFinanceObservedProjects: enabled.financeObservedProjects, controlFinanceObservedProjects: control.financeObservedProjects
    };
  }).sort((a, b) => Math.abs(b.unitDelta) - Math.abs(a.unitDelta) || a.genre.localeCompare(b.genre) || a.format.localeCompare(b.format));
}

function standardization(enabledRows, controlRows) {
  const byCohort = rows => {
    const result = new Map();
    for (const row of rows) get(result, key(row.destinationGenre, row.format), () => []).push(row);
    return result;
  };
  const enabled = byCohort(enabledRows), control = byCohort(controlRows);
  const cohortKeys = new Set([...enabled.keys(), ...control.keys()]);
  const output = [];
  for (const cohortKey of cohortKeys) {
    const [genre, format] = cohortKey.split("|");
    const perStratum = rows => rollup(rows, row => key(row.careerBand, row.qualityQuartile, row.reachBucket));
    const e = perStratum(enabled.get(cohortKey) ?? []), c = perStratum(control.get(cohortKey) ?? []);
    const common = [...e.keys()].filter(stratum => c.has(stratum));
    const totalWeight = sum(common.map(stratum => e.get(stratum).projects + c.get(stratum).projects));
    const enabledStandardized = totalWeight ? sum(common.map(stratum => {
      const weight = (e.get(stratum).projects + c.get(stratum).projects) / totalWeight;
      return weight * e.get(stratum).unitsPerProject;
    })) : null;
    const controlStandardized = totalWeight ? sum(common.map(stratum => {
      const weight = (e.get(stratum).projects + c.get(stratum).projects) / totalWeight;
      return weight * c.get(stratum).unitsPerProject;
    })) : null;
    output.push({ genre, format, supportedStrata: common.length,
      enabledStandardizedUnitsPerProject: enabledStandardized, controlStandardizedUnitsPerProject: controlStandardized,
      standardizedYieldDifference: enabledStandardized === null ? null : enabledStandardized - controlStandardized });
  }
  return output.sort((a, b) => Math.abs(b.standardizedYieldDifference ?? 0) - Math.abs(a.standardizedYieldDifference ?? 0) || a.genre.localeCompare(b.genre));
}

function aggregateEffects(cohorts) {
  const formats = ["Single", "Album", "All"];
  return formats.map(format => {
    const rows = format === "All" ? cohorts : cohorts.filter(row => row.format === format);
    return { format, unitDelta: sum(rows.map(row => row.unitDelta)), countAllocationEffect: sum(rows.map(row => row.countAllocationEffect)),
      realizedYieldEffect: sum(rows.map(row => row.realizedYieldEffect)), reconciliationResidual: sum(rows.map(row => row.reconciliationResidual)),
      allocatedMarketNetDelta: sum(rows.map(row => row.allocatedMarketNetDelta)), observedLabelNetDelta: sum(rows.map(row => row.observedLabelNetDelta)) };
  });
}

function integrity(data) {
  const rows = data.rows;
  const selected = data.enabled ? rows.filter(row => row.selectionMatched) : [];
  return {
    decisions: rows.length, releaseStrategyMatches: rows.filter(row => row.releaseStrategyMatched).length,
    realizedUnitCoverage: ratio(rows.filter(row => row.realized).length, rows.length), financeCoverage: ratio(rows.filter(row => row.financeObserved).length, rows.length),
    supplySelections: data.selections.length, selectionMatches: selected.length,
    selectionDestinationMismatches: selected.filter(row => !row.selectionDestinationMatched).map(row => ({
      week: row.week, labelId: row.labelId, artistId: row.artistId, recordId: row.recordId,
      transition: row.transition, destinationGenre: row.destinationGenre
    }))
  };
}

const args = process.argv.slice(2);
const outputIndex = args.indexOf("--output");
const output = outputIndex >= 0 ? args[outputIndex + 1] : null;
if (outputIndex >= 0) args.splice(outputIndex, 2);
if (args.length !== 2) throw new Error("Usage: node SimTools/analyze-supply-economic-bridge.mjs <enabled-run> <control-run> [--output file]");

const enabled = load(args[0], true);
const control = load(args[1], false);
const cohorts = combinedCohorts(enabled.rows, control.rows);
const result = {
  schema: "supply-economic-bridge/v1",
  accounting: {
    countAllocationEffect: "(enabled projects - control projects) * control units per project",
    realizedYieldEffect: "enabled projects * (enabled units per project - control units per project)",
    allocatedMarketNet: "observed project units * annual all-label market-net-per-unit for the project's format; this apportions aggregate market net and is not record-level finance.",
    standardizedUnitsPerProject: "Direct standardization over common format-by-career-band-by-quality-quartile-by-reach-bucket strata, weighted by pooled project counts."
  },
  enabled: { run: enabled.run, integrity: integrity(enabled), transitions: transitionRows(enabled.rows), countsByModeAndFormat: modeRows(enabled.rows) },
  control: { run: control.run, integrity: integrity(control), transitions: transitionRows(control.rows), countsByModeAndFormat: modeRows(control.rows) },
  genreFormatCohorts: cohorts,
  standardizedUnitsPerProject: standardization(enabled.rows, control.rows),
  aggregateEffects: aggregateEffects(cohorts),
  hotspots: cohorts.filter(row => row.format === "Single").slice(0, 30)
};
const text = `${JSON.stringify(result, null, 2)}\n`;
if (output) fs.writeFileSync(output, text);
process.stdout.write(text);
