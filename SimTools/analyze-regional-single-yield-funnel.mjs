import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

/*
 * Offline regional Single-yield funnel.  Reads only existing audit telemetry.
 *
 * Usage:
 *   node SimTools/analyze-regional-single-yield-funnel.mjs <enabled-run> <control-run> [--output file]
 */

const logDirectory = path.resolve("SimLogs");
const TARGETS = new Set(["TeenPop", "TraditionalPop", "Country", "DooWop"]);
const EXPLANATION_FIELDS = [
  { column: "legacyAcceptanceComparator", id: "legacyAcceptance" },
  { column: "finalAcceptance", id: "finalAcceptance" },
  { column: "legacySingleDemandMultiplier", id: "legacySingleMultiplier" },
  { column: "enabledSingleDemandMultiplier", id: "enabledSingleMultiplier" },
  { column: "singleDemandTransferRatio", id: "singleTransferRatio" },
  { column: "finalDemandSeam", id: "finalDemandSeam" },
  { column: "formatTilt", id: "formatTilt" },
  { column: "chartVisibilityMultiplier", id: "chartVisibilityMultiplier" },
  { column: "radioSalesMultiplier", id: "radioSalesMultiplier" },
  { column: "radioFactor", id: "radioFactor" },
  { column: "sentimentMultiplier", id: "sentimentMultiplier" },
  { column: "awardMultiplier", id: "awardMultiplier" },
  { column: "distributionMultiplier", id: "distributionMultiplier" },
  { column: "conversionSeasonalityMultiplier", id: "conversionSeasonalityMultiplier" },
  { column: "catalogBaselineAcceptance", id: "catalogBaselineAcceptance" },
  { column: "regionalAdjustedAcceptance", id: "regionalAdjustedAcceptance" },
  { column: "segmentRoutedAcceptance", id: "segmentRoutedAcceptance" },
  { column: "primaryWeightedRoutedAcceptance", id: "primaryWeightedRoutedAcceptance" },
  { column: "secondaryBlendAcceptanceContribution", id: "secondaryBlendAcceptanceContribution" },
  { column: "legacyMomentum", id: "legacyMomentum" },
  { column: "legacyMomentumAcceptanceContribution", id: "legacyMomentumAcceptanceContribution" },
  { column: "acceptanceClampDelta", id: "acceptanceClampDelta" },
  { column: "salesRecordAwareness", id: "salesRecordAwareness" },
  { column: "salesRegionalAwareness", id: "salesRegionalAwareness" },
  { column: "salesEffectiveAwareness", id: "salesEffectiveAwareness" },
  { column: "salesRadioHeat", id: "salesRadioHeat" },
  { column: "salesRegionalRadioPlay", id: "salesRegionalRadioPlay" }
];

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
  values.push(value); return values;
}

async function csvRows(file, required = true) {
  if (!fs.existsSync(file)) {
    if (required) throw new Error(`Missing required telemetry: ${file}`);
    return [];
  }
  const input = fs.createReadStream(file); const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers = null; const rows = [];
  for await (const line of lines) {
    if (headers === null) { headers = splitCsv(line); continue; }
    if (!line) continue;
    const values = splitCsv(line); rows.push(Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""])));
  }
  return rows;
}

function prefix(run) { return path.isAbsolute(run) ? run : path.join(logDirectory, run); }
function number(value, fallback = 0) { const result = Number(value); return Number.isFinite(result) ? result : fallback; }
function sum(values) { return values.reduce((total, value) => total + value, 0); }
function ratio(numerator, denominator) { return denominator ? numerator / denominator : null; }
function mean(values) { return values.length ? sum(values) / values.length : null; }
function key(...parts) { return parts.join("|"); }
function get(map, groupKey, create) { if (!map.has(groupKey)) map.set(groupKey, create()); return map.get(groupKey); }
function reachBucket(reach) { return reach < .75 ? "low(<0.75)" : reach < 1.25 ? "mid(0.75-1.25)" : "high(>=1.25)"; }

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
function canonicalGenre(primary, secondary, year) {
  if (primary === "GirlGroup") return secondary === "Soul" || secondary === "RnB" ? "Soul" : "TeenPop";
  return mapLegacy(primary, year);
}

function routeCategory(row) {
  if (!row.enabled) return "ControlIdentity";
  if (row.mode === "Retained") return "Retained";
  if (row.sourceGenre !== row.genre) return "IncomingTransition";
  return row.mode;
}

function emptyFunnel() {
  return { observations: 0, awareBuyerExposure: 0, rawDemand: 0, fulfilledUnits: 0, initialStock: 0,
    restockAmount: 0, requestedRestockAmount: 0, capacityCappedWeeks: 0, stockoutDemand: 0,
    early: { units: 0, rawDemand: 0, awareBuyers: 0 }, middle: { units: 0, rawDemand: 0, awareBuyers: 0 }, late: { units: 0, rawDemand: 0, awareBuyers: 0 },
    radioGainExposure: 0, mediaInputExposure: 0, breakoutActiveWeeks: 0, maxObservedAge: 0, postWeek14Observations: 0,
    firstStockByRegion: new Map(), rowsByExplanationKey: new Map() };
}
function emptyExplanation() {
  return {
    rows: 0,
    sums: Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, 0])),
    counts: Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, 0])),
    means: Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, null])),
    transfer: { matchedObservations: 0, enabledRawDemand: 0, legacyCounterfactualRawDemand: 0 }
  };
}
function addFunnelObservation(funnel, row) {
  const units = number(row.unitsSoldThisWeek), raw = number(row.rawSales), aware = number(row.awareBuyers), age = number(row.weeksSinceRelease);
  funnel.observations++; funnel.awareBuyerExposure += aware; funnel.rawDemand += raw; funnel.fulfilledUnits += units;
  funnel.restockAmount += number(row.restockAmount); funnel.requestedRestockAmount += number(row.requestedRestockAmount);
  funnel.capacityCappedWeeks += row.capacityCapped === "true" ? 1 : 0; funnel.stockoutDemand += Math.max(0, raw - units);
  funnel.radioGainExposure += number(row.breakoutRadioGain); funnel.mediaInputExposure += number(row.mediaInput);
  funnel.breakoutActiveWeeks += row.breakoutStage && row.breakoutStage !== "None" ? 1 : 0;
  funnel.maxObservedAge = Math.max(funnel.maxObservedAge, age); if (age > 14) funnel.postWeek14Observations++;
  const bucket = age <= 3 ? funnel.early : age <= 14 ? funnel.middle : funnel.late;
  bucket.units += units; bucket.rawDemand += raw; bucket.awareBuyers += aware;
  const region = row.regionId;
  funnel.rowsByExplanationKey.set(key(row.week, region), { rawDemand: raw, awareBuyers: aware });
  const previous = funnel.firstStockByRegion.get(region);
  if (!previous || age < previous.age) funnel.firstStockByRegion.set(region, { age, stock: number(row.weekStartStock) });
}
function finishFunnel(funnel) {
  funnel.initialStock = sum([...funnel.firstStockByRegion.values()].map(row => row.stock));
  delete funnel.firstStockByRegion;
  funnel.conversionRate = ratio(funnel.rawDemand, funnel.awareBuyerExposure) ?? 0;
  funnel.fulfillmentRate = ratio(funnel.fulfilledUnits, funnel.rawDemand) ?? 0;
  funnel.lateUnitShare = ratio(funnel.late.units, funnel.fulfilledUnits) ?? 0;
  funnel.radioGainPerObservation = ratio(funnel.radioGainExposure, funnel.observations) ?? 0;
  funnel.mediaInputPerObservation = ratio(funnel.mediaInputExposure, funnel.observations) ?? 0;
  funnel.breakoutActiveShare = ratio(funnel.breakoutActiveWeeks, funnel.observations) ?? 0;
  return funnel;
}

async function scanFunnel(file, projects) {
  const input = fs.createReadStream(file); const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers = null; let matchedRows = 0;
  for await (const line of lines) {
    if (headers === null) { headers = splitCsv(line); continue; }
    if (!line) continue;
    const values = splitCsv(line); const row = Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
    const project = projects.get(row.recordId);
    if (!project) continue;
    addFunnelObservation(project.funnel, row); matchedRows++;
  }
  for (const project of projects.values()) finishFunnel(project.funnel);
  return matchedRows;
}

async function scanExplanations(file, projects) {
  if (!fs.existsSync(file)) return { matchedRows: 0, fieldsPresent: Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, false])) };
  const input = fs.createReadStream(file); const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers = null; let fieldsPresent = null; let matchedRows = 0;
  for await (const line of lines) {
    if (headers === null) {
      headers = splitCsv(line); const columns = new Set(headers);
      fieldsPresent = Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, columns.has(field.column)]));
      continue;
    }
    if (!line) continue;
    const values = splitCsv(line); const row = Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
    const project = projects.get(row.recordId);
    if (!project) continue;
    project.explanation.rows++; matchedRows++;
    for (const field of EXPLANATION_FIELDS) {
      if (!fieldsPresent[field.id] || row[field.column] === "") continue;
      project.explanation.sums[field.id] += number(row[field.column]);
      project.explanation.counts[field.id]++;
    }
    const observed = project.funnel.rowsByExplanationKey.get(key(row.week, row.region));
    const legacyMultiplier = number(row.legacySingleDemandMultiplier, null);
    const enabledMultiplier = number(row.enabledSingleDemandMultiplier, null);
    if (observed && legacyMultiplier !== null && enabledMultiplier !== null && enabledMultiplier > 0) {
      project.explanation.transfer.matchedObservations++;
      project.explanation.transfer.enabledRawDemand += observed.rawDemand;
      project.explanation.transfer.legacyCounterfactualRawDemand += observed.rawDemand * legacyMultiplier / enabledMultiplier;
    }
  }
  for (const project of projects.values()) {
    for (const field of EXPLANATION_FIELDS)
      project.explanation.means[field.id] = ratio(project.explanation.sums[field.id], project.explanation.counts[field.id]);
  }
  return { matchedRows, fieldsPresent: fieldsPresent ?? Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, false])) };
}

function lifecycleById(rows) {
  return new Map(rows.map(row => [row.recordId, { debutPosition: number(row.debutPosition), peakPosition: number(row.peakPosition), weeksOnChart: number(row.weeksOnChart) }]));
}

async function load(run, enabled) {
  const base = prefix(run);
  const [forks, selections, lifecycles, strategies] = await Promise.all([
    csvRows(`${base}-fork-ratios.csv`), csvRows(`${base}-supply-selections.csv`, false), csvRows(`${base}-lifecycles.csv`), csvRows(`${base}-release-strategy.csv`)
  ]);
  const strategyIds = new Set(strategies.map(row => row.recordId));
  const selectionByAttempt = new Map(selections.map(row => [key(row.week, row.labelId, row.artistId), row]));
  const lifecycle = lifecycleById(lifecycles);
  const projects = new Map();
  for (const fork of forks) {
    if (fork.chosenFormat !== "Single") continue;
    const genre = canonicalGenre(fork.genre, fork.rawSecondaryGenre, number(fork.year));
    if (!TARGETS.has(genre)) continue;
    const selection = selectionByAttempt.get(key(fork.week, fork.labelId, fork.artistId));
    const sourceRaw = enabled ? (selection?.artistIdentity ?? (fork.rawSecondaryGenre || fork.genre)) : fork.genre;
    const sourceGenre = canonicalGenre(sourceRaw, "", number(fork.year));
    const project = {
      run, enabled, recordId: fork.recordId, week: number(fork.week), genre, sourceGenre,
      mode: enabled ? (selection?.selectionMode ?? "PreLiveOrUnmatched") : "ControlIdentity",
      careerBand: fork.careerBand, qualityQuartile: fork.qualityQuartile, reachBucket: reachBucket(number(fork.reachFactor)),
      funnel: emptyFunnel(), explanation: emptyExplanation(),
      lifecycle: lifecycle.get(fork.recordId) ?? null, releaseStrategyMatched: strategyIds.has(fork.recordId)
    };
    project.routeCategory = routeCategory(project);
    projects.set(project.recordId, project);
  }
  const [funnelRows, geography] = await Promise.all([
    scanFunnel(`${base}-breakout-funnel.csv`, projects), csvRows(`${base}-geography-metrics.csv`)
  ]);
  const explanationScan = await scanExplanations(`${base}-record-genre-explanation.csv`, projects);
  return { run, enabled, projects: [...projects.values()], funnelRows, explanationRows: explanationScan.matchedRows,
    explanationFieldsPresent: explanationScan.fieldsPresent, geography };
}

const funnelKeys = ["awareBuyerExposure", "rawDemand", "fulfilledUnits", "initialStock", "restockAmount", "requestedRestockAmount", "capacityCappedWeeks", "stockoutDemand", "radioGainExposure", "mediaInputExposure", "breakoutActiveWeeks"];
function aggregateProjects(projects) {
  const result = { projects: projects.length, lifecycleProjects: 0, chartEntries: 0, chartWeeks: 0, explanationProjects: 0, explanationRows: 0,
    explanationSums: Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, 0])),
    explanationFieldRows: Object.fromEntries(EXPLANATION_FIELDS.map(field => [field.id, 0])),
    transferMatchedObservations: 0, transferEnabledRawDemand: 0, transferLegacyCounterfactualRawDemand: 0 };
  for (const name of funnelKeys) result[name] = 0;
  for (const project of projects) {
    const funnel = project.funnel;
    for (const name of funnelKeys) result[name] += funnel[name];
    result.earlyUnits = (result.earlyUnits ?? 0) + funnel.early.units; result.middleUnits = (result.middleUnits ?? 0) + funnel.middle.units; result.lateUnits = (result.lateUnits ?? 0) + funnel.late.units;
    if (project.lifecycle) { result.lifecycleProjects++; result.chartEntries += project.lifecycle.debutPosition > 0 ? 1 : 0; result.chartWeeks += project.lifecycle.weeksOnChart; }
    if (project.explanation.rows) {
      result.explanationProjects++; result.explanationRows += project.explanation.rows;
      for (const field of EXPLANATION_FIELDS) {
        result.explanationSums[field.id] += project.explanation.sums[field.id];
        result.explanationFieldRows[field.id] += project.explanation.counts[field.id];
      }
      result.transferMatchedObservations += project.explanation.transfer.matchedObservations;
      result.transferEnabledRawDemand += project.explanation.transfer.enabledRawDemand;
      result.transferLegacyCounterfactualRawDemand += project.explanation.transfer.legacyCounterfactualRawDemand;
    }
  }
  result.unitsPerProject = ratio(result.fulfilledUnits, result.projects) ?? 0;
  result.rawDemandPerProject = ratio(result.rawDemand, result.projects) ?? 0;
  result.awareBuyersPerProject = ratio(result.awareBuyerExposure, result.projects) ?? 0;
  result.initialStockPerProject = ratio(result.initialStock, result.projects) ?? 0;
  result.earlyUnitsPerProject = ratio(result.earlyUnits, result.projects) ?? 0;
  result.middleUnitsPerProject = ratio(result.middleUnits, result.projects) ?? 0;
  result.lateUnitsPerProject = ratio(result.lateUnits, result.projects) ?? 0;
  result.conversionRate = ratio(result.rawDemand, result.awareBuyerExposure) ?? 0;
  result.fulfillmentRate = ratio(result.fulfilledUnits, result.rawDemand) ?? 0;
  result.stockoutDemandShare = ratio(result.stockoutDemand, result.rawDemand) ?? 0;
  result.lateUnitShare = ratio(result.lateUnits, result.fulfilledUnits) ?? 0;
  result.capacityCappedWeeksPerProject = ratio(result.capacityCappedWeeks, result.projects) ?? 0;
  result.restockPerProject = ratio(result.restockAmount, result.projects) ?? 0;
  result.radioGainPerProject = ratio(result.radioGainExposure, result.projects) ?? 0;
  result.mediaInputPerProject = ratio(result.mediaInputExposure, result.projects) ?? 0;
  result.breakoutActiveWeeksPerProject = ratio(result.breakoutActiveWeeks, result.projects) ?? 0;
  result.chartEntryShareCompleted = ratio(result.chartEntries, result.lifecycleProjects);
  result.chartWeeksPerCompletedProject = ratio(result.chartWeeks, result.lifecycleProjects);
  result.explanationCoverage = ratio(result.explanationProjects, result.projects);
  result.explanationMeans = Object.fromEntries(EXPLANATION_FIELDS.map(field =>
    [field.id, ratio(result.explanationSums[field.id], result.explanationFieldRows[field.id])]
  ));
  result.explanationFieldCoverage = Object.fromEntries(EXPLANATION_FIELDS.map(field =>
    [field.id, ratio(result.explanationFieldRows[field.id], result.explanationRows)]
  ));
  result.transferRawDemandDelta = result.transferEnabledRawDemand - result.transferLegacyCounterfactualRawDemand;
  result.transferRawDemandShare = ratio(result.transferRawDemandDelta, result.transferEnabledRawDemand);
  result.transferObservationCoverage = ratio(result.transferMatchedObservations, result.explanationRows);
  result.transferEnabledRawDemandPerProject = ratio(result.transferEnabledRawDemand, result.projects);
  result.transferLegacyCounterfactualRawDemandPerProject = ratio(result.transferLegacyCounterfactualRawDemand, result.projects);
  result.transferRawDemandDeltaPerProject = ratio(result.transferRawDemandDelta, result.projects);
  delete result.explanationSums;
  result.maxObservedAge = Math.max(0, ...projects.map(project => project.funnel.maxObservedAge));
  result.postWeek14Observations = sum(projects.map(project => project.funnel.postWeek14Observations));
  return result;
}

function groupProjects(projects, selector) {
  const groups = new Map();
  for (const project of projects) get(groups, selector(project), () => []).push(project);
  return groups;
}

function groupRows(projects, selector, fieldNames) {
  const groups = groupProjects(projects, selector);
  return [...groups].map(([groupKey, items]) => ({ groupKey, ...aggregateProjects(items), items })).map(row => {
    const result = { ...row }; delete result.items; return result;
  });
}

const metricNames = ["awareBuyersPerProject", "conversionRate", "rawDemandPerProject", "initialStockPerProject", "restockPerProject", "fulfillmentRate", "stockoutDemandShare", "capacityCappedWeeksPerProject", "unitsPerProject", "earlyUnitsPerProject", "middleUnitsPerProject", "lateUnitsPerProject", "lateUnitShare", "radioGainPerProject", "mediaInputPerProject", "breakoutActiveWeeksPerProject", "chartEntryShareCompleted", "chartWeeksPerCompletedProject"];
function standardize(left, right) {
  const stratum = project => key(project.careerBand, project.qualityQuartile, project.reachBucket);
  const leftGroups = groupProjects(left, stratum), rightGroups = groupProjects(right, stratum);
  const common = [...leftGroups.keys()].filter(groupKey => rightGroups.has(groupKey));
  const totalWeight = sum(common.map(groupKey => leftGroups.get(groupKey).length + rightGroups.get(groupKey).length));
  const summary = { commonStrata: common.length, commonProjectsLeft: sum(common.map(groupKey => leftGroups.get(groupKey).length)), commonProjectsRight: sum(common.map(groupKey => rightGroups.get(groupKey).length)) };
  for (const metric of metricNames) {
    const noPostWeek14Coverage = (metric === "lateUnitsPerProject" || metric === "lateUnitShare") &&
      common.every(groupKey => aggregateProjects(leftGroups.get(groupKey)).postWeek14Observations === 0 && aggregateProjects(rightGroups.get(groupKey)).postWeek14Observations === 0);
    if (!totalWeight || noPostWeek14Coverage) { summary[`left${metric}`] = null; summary[`right${metric}`] = null; summary[`difference${metric}`] = null; continue; }
    const standardized = side => sum(common.map(groupKey => {
      const items = side === "left" ? leftGroups.get(groupKey) : rightGroups.get(groupKey);
      const weight = (leftGroups.get(groupKey).length + rightGroups.get(groupKey).length) / totalWeight;
      return weight * aggregateProjects(items)[metric];
    }));
    summary[`left${metric}`] = standardized("left"); summary[`right${metric}`] = standardized("right");
    summary[`difference${metric}`] = summary[`left${metric}`] - summary[`right${metric}`];
  }
  return summary;
}

function standardizeExplanation(left, right, fieldsPresent) {
  const stratum = project => key(project.careerBand, project.qualityQuartile, project.reachBucket);
  const leftExplained = left.filter(project => project.explanation.rows > 0);
  const rightExplained = right.filter(project => project.explanation.rows > 0);
  const leftGroups = groupProjects(leftExplained, stratum), rightGroups = groupProjects(rightExplained, stratum);
  const common = [...leftGroups.keys()].filter(groupKey => rightGroups.has(groupKey));
  const totalWeight = sum(common.map(groupKey => leftGroups.get(groupKey).length + rightGroups.get(groupKey).length));
  const summary = {
    candidateProjectsLeft: left.length,
    candidateProjectsRight: right.length,
    explainedProjectsLeft: leftExplained.length,
    explainedProjectsRight: rightExplained.length,
    explanationCoverageLeft: ratio(leftExplained.length, left.length),
    explanationCoverageRight: ratio(rightExplained.length, right.length),
    commonStrata: common.length,
    commonProjectsLeft: sum(common.map(groupKey => leftGroups.get(groupKey).length)),
    commonProjectsRight: sum(common.map(groupKey => rightGroups.get(groupKey).length))
  };
  for (const field of EXPLANATION_FIELDS) {
    const hasField = fieldsPresent[field.id];
    const fieldHasRows = common.some(groupKey =>
      aggregateProjects(leftGroups.get(groupKey)).explanationFieldRows[field.id] > 0 &&
      aggregateProjects(rightGroups.get(groupKey)).explanationFieldRows[field.id] > 0
    );
    if (!totalWeight || !hasField || !fieldHasRows) {
      summary[`left${field.id}`] = null; summary[`right${field.id}`] = null; summary[`difference${field.id}`] = null;
      continue;
    }
    const standardized = side => sum(common.map(groupKey => {
      const items = side === "left" ? leftGroups.get(groupKey) : rightGroups.get(groupKey);
      const weight = (leftGroups.get(groupKey).length + rightGroups.get(groupKey).length) / totalWeight;
      return weight * aggregateProjects(items).explanationMeans[field.id];
    }));
    summary[`left${field.id}`] = standardized("left"); summary[`right${field.id}`] = standardized("right");
    summary[`difference${field.id}`] = summary[`left${field.id}`] - summary[`right${field.id}`];
  }
  for (const metric of ["transferEnabledRawDemandPerProject", "transferLegacyCounterfactualRawDemandPerProject", "transferRawDemandDeltaPerProject", "transferRawDemandShare"]) {
    const metricCommon = common.filter(groupKey =>
      aggregateProjects(leftGroups.get(groupKey)).transferMatchedObservations > 0 &&
      aggregateProjects(rightGroups.get(groupKey)).transferMatchedObservations > 0
    );
    const metricTotalWeight = sum(metricCommon.map(groupKey => leftGroups.get(groupKey).length + rightGroups.get(groupKey).length));
    if (!metricTotalWeight) {
      summary[`left${metric}`] = null; summary[`right${metric}`] = null; summary[`difference${metric}`] = null;
      continue;
    }
    const standardized = side => sum(metricCommon.map(groupKey => {
      const items = side === "left" ? leftGroups.get(groupKey) : rightGroups.get(groupKey);
      const weight = (leftGroups.get(groupKey).length + rightGroups.get(groupKey).length) / metricTotalWeight;
      return weight * aggregateProjects(items)[metric];
    }));
    summary[`left${metric}`] = standardized("left"); summary[`right${metric}`] = standardized("right");
    summary[`difference${metric}`] = summary[`left${metric}`] - summary[`right${metric}`];
  }
  return summary;
}

function comparisons(enabled, control) {
  const output = [];
  for (const genre of TARGETS) {
    const e = enabled.projects.filter(project => project.genre === genre);
    const c = control.projects.filter(project => project.genre === genre);
    output.push({ comparison: `enabled ${genre} vs control ${genre}`, genre, left: "enabled", right: "control", ...standardize(e, c) });
  }
  for (const genre of ["TeenPop", "TraditionalPop"]) {
    const retained = enabled.projects.filter(project => project.genre === genre && project.routeCategory === "Retained");
    const incoming = enabled.projects.filter(project => project.genre === genre && project.routeCategory === "IncomingTransition");
    output.push({ comparison: `enabled retained ${genre} vs incoming ${genre}`, genre, left: "retained", right: "incoming", ...standardize(retained, incoming) });
  }
  return output;
}

function comparatorComparisons(enabled) {
  const output = [];
  for (const genre of ["TeenPop", "TraditionalPop"]) {
    const target = enabled.projects.filter(project => project.genre === genre);
    for (const controlGenre of ["Country", "DooWop"]) {
      const negativeControl = enabled.projects.filter(project => project.genre === controlGenre);
      output.push({ comparison: `enabled ${genre} vs enabled ${controlGenre}`, genre, left: genre, right: controlGenre,
        ...standardizeExplanation(target, negativeControl, enabled.explanationFieldsPresent) });
    }
    const retained = enabled.projects.filter(project => project.genre === genre && project.routeCategory === "Retained");
    const incoming = enabled.projects.filter(project => project.genre === genre && project.routeCategory === "IncomingTransition");
    output.push({ comparison: `enabled retained ${genre} vs incoming ${genre}`, genre, left: "retained", right: "incoming",
      ...standardizeExplanation(retained, incoming, enabled.explanationFieldsPresent) });
  }
  return output;
}

function comparatorCoverageRows(enabled) {
  return groupRows(enabled.projects, project => key(project.genre, project.sourceGenre, project.routeCategory), []).map(row => {
    const [genre, sourceGenre, routeCategory] = row.groupKey.split("|");
    return { genre, sourceGenre, routeCategory, projects: row.projects, explanationProjects: row.explanationProjects,
      explanationRows: row.explanationRows, explanationCoverage: row.explanationCoverage,
      fieldCoverage: row.explanationFieldCoverage, explanationMeans: row.explanationMeans,
      transferMatchedObservations: row.transferMatchedObservations,
      transferObservationCoverage: row.transferObservationCoverage,
      transferEnabledRawDemand: row.transferEnabledRawDemand,
      transferLegacyCounterfactualRawDemand: row.transferLegacyCounterfactualRawDemand,
      transferRawDemandDelta: row.transferRawDemandDelta,
      transferRawDemandShare: row.transferRawDemandShare };
  }).sort((a, b) => b.projects - a.projects || a.genre.localeCompare(b.genre));
}

function transferCausalAccounting(enabled) {
  return ["TeenPop", "TraditionalPop", "Country", "DooWop"].map(genre => {
    const retained = enabled.projects.filter(project => project.genre === genre && project.routeCategory === "Retained");
    const summary = aggregateProjects(retained);
    return { genre, routeCategory: "Retained", projects: summary.projects, explanationRows: summary.explanationRows,
      transferMatchedObservations: summary.transferMatchedObservations,
      transferObservationCoverage: summary.transferObservationCoverage,
      transferEnabledRawDemand: summary.transferEnabledRawDemand,
      transferLegacyCounterfactualRawDemand: summary.transferLegacyCounterfactualRawDemand,
      transferRawDemandDelta: summary.transferRawDemandDelta,
      transferRawDemandShare: summary.transferRawDemandShare };
  });
}

function routeRows(enabled) {
  return groupRows(enabled.projects, project => key(project.genre, project.sourceGenre, project.routeCategory), []).map(row => {
    const [genre, sourceGenre, routeCategory] = row.groupKey.split("|");
    return { genre, sourceGenre, routeCategory, ...row };
  }).sort((a, b) => b.fulfilledUnits - a.fulfilledUnits || a.genre.localeCompare(b.genre));
}

function geographyRows(enabled, control) {
  const summarize = data => {
    const groups = new Map();
    for (const row of data.geography) {
      if (!TARGETS.has(row.genre)) continue;
      const group = get(groups, key(row.genre, row.regionId), () => ({ units: 0, records: 0, chartedUnits: 0, backorders: 0 }));
      group.units += number(row.totalUnits); group.records += number(row.recordCount); group.chartedUnits += number(row.chartedUnits); group.backorders += number(row.backorders);
    }
    return groups;
  };
  const e = summarize(enabled), c = summarize(control), keys = new Set([...e.keys(), ...c.keys()]);
  return [...keys].map(groupKey => {
    const [genre, region] = groupKey.split("|"); const left = e.get(groupKey) ?? { units: 0, records: 0, chartedUnits: 0, backorders: 0 }; const right = c.get(groupKey) ?? { units: 0, records: 0, chartedUnits: 0, backorders: 0 };
    return { genre, region, enabledUnits: left.units, controlUnits: right.units, unitDelta: left.units - right.units, enabledBackorders: left.backorders, controlBackorders: right.backorders };
  }).sort((a, b) => Math.abs(b.unitDelta) - Math.abs(a.unitDelta) || a.genre.localeCompare(b.genre));
}

const args = process.argv.slice(2);
const outputIndex = args.indexOf("--output");
const output = outputIndex >= 0 ? args[outputIndex + 1] : null;
if (outputIndex >= 0) args.splice(outputIndex, 2);
if (args.length !== 2) throw new Error("Usage: node SimTools/analyze-regional-single-yield-funnel.mjs <enabled-run> <control-run> [--output file]");
const [enabled, control] = await Promise.all([load(args[0], true), load(args[1], false)]);
const result = {
  schema: "regional-single-yield-funnel/v2",
  caveats: {
    conversion: "Conversion is directly observed as raw demand / aware-buyer exposure from breakout-funnel telemetry.",
    marketAcceptanceComparator: enabled.explanationFieldsPresent.legacyAcceptance
      ? "Comparator coverage and direct-standardized means are calculated from enabled record-genre-explanation rows; coverage is reported by field and cohort."
      : "This enabled run predates comparator-column telemetry. Comparator means are null and field coverage is zero; a fresh observational enabled run is required.",
    chartCoverage: "Lifecycle chart entry/persistence is available only for completed records; aggregate-only runs do not retain weekly records.csv rows for active projects.",
    postWeek14: "breakout-funnel only records the 14-week breakout diagnostic window, so post-week-14 units and persistence cannot be classified from this run.",
    geography: "geography-metrics is all-format regional context and cannot attribute a row to one record or transition."
  },
  enabled: { run: enabled.run, projects: enabled.projects.length, matchedBreakoutRows: enabled.funnelRows, explanationRows: enabled.explanationRows,
    explanationFieldsPresent: enabled.explanationFieldsPresent, routes: routeRows(enabled), comparatorCoverageByCohort: comparatorCoverageRows(enabled) },
  control: { run: control.run, projects: control.projects.length, matchedBreakoutRows: control.funnelRows, explanationRows: control.explanationRows,
    explanationFieldsPresent: control.explanationFieldsPresent },
  standardizedComparisons: comparisons(enabled, control),
  comparatorStandardizedComparisons: comparatorComparisons(enabled),
  transferCausalAccounting: transferCausalAccounting(enabled),
  regionalAllFormatContext: geographyRows(enabled, control)
};
const text = `${JSON.stringify(result, null, 2)}\n`;
if (output) fs.writeFileSync(output, text);
process.stdout.write(text);
