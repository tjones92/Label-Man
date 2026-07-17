#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";

/*
 * Fail-closed adjudicator for the market-clearing / format-memory candidate.
 *
 * Usage:
 *   node SimTools/analyze-market-clearing-format-memory.mjs \
 *     <directory> <enabled-run> [control-run]
 *
 * The retained Directive 6 control is used when control-run is omitted.
 */

const [directory = "SimLogs", run, controlRun = "d6-transition-envelope-decade-control-1001"] =
  process.argv.slice(2);
if (!run) {
  throw new Error(
    "Usage: node SimTools/analyze-market-clearing-format-memory.mjs " +
    "<directory> <enabled-run> [control-run]"
  );
}

const TARGET_YEARS = Array.from({ length: 10 }, (_, index) => 1960 + index);
const EPSILON = 1e-6;
const MONEY_TOLERANCE = 0.02;
const fail = message => { throw new Error(`FAIL_CLOSED: ${message}`); };

function splitCsv(line, file, lineNumber) {
  const values = [];
  let value = "";
  let quoted = false;
  for (let index = 0; index < line.length; index++) {
    const character = line[index];
    if (character === '"') {
      if (quoted && line[index + 1] === '"') {
        value += '"';
        index++;
      } else {
        quoted = !quoted;
      }
    } else if (character === "," && !quoted) {
      values.push(value);
      value = "";
    } else {
      value += character;
    }
  }
  if (quoted) fail(`unclosed quote in ${file}:${lineNumber}`);
  values.push(value);
  return values;
}

function parse(prefix, name, options = {}) {
  const file = path.join(directory, `${prefix}-${name}.csv`);
  if (!fs.existsSync(file)) fail(`missing ${file}`);
  const text = fs.readFileSync(file, "utf8");
  const lines = text.split(/\r?\n/);
  while (lines.length && lines.at(-1) === "") lines.pop();
  if (!lines.length || !lines[0]) fail(`empty ${file}`);
  const header = splitCsv(lines[0], file, 1);
  if (header.some(column => !column)) fail(`blank header in ${file}`);
  if (new Set(header).size !== header.length) fail(`duplicate header in ${file}`);
  for (const required of options.columns ?? []) {
    if (!header.includes(required)) fail(`missing column ${required} in ${file}`);
  }
  const rows = lines.slice(1).filter(line => line !== "").map((line, index) => {
    const values = splitCsv(line, file, index + 2);
    if (values.length !== header.length) {
      fail(`malformed row ${index + 2} in ${file}: expected ${header.length} fields, found ${values.length}`);
    }
    return Object.fromEntries(header.map((column, valueIndex) => [column, values[valueIndex]]));
  });
  if (!options.allowEmpty && rows.length === 0) fail(`no data rows in ${file}`);
  return { file, name, header, rows };
}

function number(row, key, source, options = {}) {
  const raw = row[key];
  if (options.allowBlank && (raw === "" || raw === undefined)) return null;
  const value = Number(raw);
  if (!Number.isFinite(value)) fail(`${source}.${key} is not numeric: ${JSON.stringify(raw)}`);
  return value;
}

function integer(row, key, source) {
  const value = number(row, key, source);
  if (!Number.isSafeInteger(value)) fail(`${source}.${key} is not a safe integer: ${value}`);
  return value;
}

function boolean(row, key, source) {
  if (row[key] === "true" || row[key] === "1") return true;
  if (row[key] === "false" || row[key] === "0") return false;
  fail(`${source}.${key} is not boolean: ${JSON.stringify(row[key])}`);
}

function nearlyEqual(left, right, tolerance = MONEY_TOLERANCE) {
  return Math.abs(left - right) <= Math.max(tolerance, EPSILON * Math.max(1, Math.abs(left), Math.abs(right)));
}

function assertEqual(actual, expected, message) {
  if (actual !== expected) fail(`${message}: ${actual} != ${expected}`);
}

function assertNear(actual, expected, message, tolerance = MONEY_TOLERANCE) {
  if (!nearlyEqual(actual, expected, tolerance)) fail(`${message}: ${actual} != ${expected}`);
}

function sum(rows, selector) {
  return rows.reduce((total, row) => total + selector(row), 0);
}

function mean(rows, selector) {
  return rows.length ? sum(rows, selector) / rows.length : 0;
}

function ratio(numerator, denominator, context) {
  if (!Number.isFinite(numerator) || !Number.isFinite(denominator) || denominator === 0) {
    fail(`invalid ratio denominator for ${context}: ${numerator}/${denominator}`);
  }
  return numerator / denominator;
}

function group(rows, selector) {
  const groups = new Map();
  for (const row of rows) {
    const key = selector(row);
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(row);
  }
  return groups;
}

function uniqueMap(rows, selector, context) {
  const result = new Map();
  for (const row of rows) {
    const key = selector(row);
    if (result.has(key)) fail(`duplicate ${context} key ${key}`);
    result.set(key, row);
  }
  return result;
}

function f(value, digits = 4) {
  return Number.isFinite(value) ? value.toFixed(digits) : "n/a";
}

function clearingSummary(rows) {
  return {
    capacity: sum(rows, row => row.purchaseCapacity),
    serviceableSingle: sum(rows, row => row.serviceableSingleIntent),
    serviceableAlbum: sum(rows, row => row.serviceableAlbumIntent),
    clearedSingle: sum(rows, row => row.clearedSingleUnits),
    clearedAlbum: sum(rows, row => row.clearedAlbumUnits),
    displacedSingle: sum(rows, row => row.serviceableSingleIntent - row.clearedSingleUnits),
    displacedAlbum: sum(rows, row => row.serviceableAlbumIntent - row.clearedAlbumUnits),
    physicalBackorders: sum(rows, row => row.physicalBackorders)
  };
}

function parseClearing() {
  const dataset = parse(run, "market-clearing-weekly", {
    columns: [
      "week", "year", "regionId", "activeIntentCount",
      "rawSingleDemand", "rawAlbumDemand", "rawTotalDemand",
      "serviceableSingleIntent", "serviceableAlbumIntent", "serviceableTotalIntent",
      "purchaseCapacity", "clearedSingleUnits", "clearedAlbumUnits", "clearedTotalUnits",
      "unusedCapacity", "rationingFactor", "physicalBackorders", "marketDisplacedDemand",
      "inventoryViolationCount", "allocationViolationCount", "reconciliationDelta"
    ]
  });
  const rows = dataset.rows.map((row, index) => {
    const source = `${dataset.name}[${index + 2}]`;
    const parsed = {
      week: integer(row, "week", source),
      year: integer(row, "year", source),
      regionId: row.regionId,
      activeIntentCount: integer(row, "activeIntentCount", source),
      rawSingleDemand: number(row, "rawSingleDemand", source),
      rawAlbumDemand: number(row, "rawAlbumDemand", source),
      rawTotalDemand: number(row, "rawTotalDemand", source),
      serviceableSingleIntent: integer(row, "serviceableSingleIntent", source),
      serviceableAlbumIntent: integer(row, "serviceableAlbumIntent", source),
      serviceableTotalIntent: integer(row, "serviceableTotalIntent", source),
      purchaseCapacity: integer(row, "purchaseCapacity", source),
      clearedSingleUnits: integer(row, "clearedSingleUnits", source),
      clearedAlbumUnits: integer(row, "clearedAlbumUnits", source),
      clearedTotalUnits: integer(row, "clearedTotalUnits", source),
      unusedCapacity: integer(row, "unusedCapacity", source),
      rationingFactor: number(row, "rationingFactor", source),
      physicalBackorders: integer(row, "physicalBackorders", source),
      marketDisplacedDemand: integer(row, "marketDisplacedDemand", source),
      inventoryViolationCount: integer(row, "inventoryViolationCount", source),
      allocationViolationCount: integer(row, "allocationViolationCount", source),
      reconciliationDelta: integer(row, "reconciliationDelta", source)
    };
    if (!parsed.regionId) fail(`${source}.regionId is blank`);
    for (const key of [
      "activeIntentCount", "rawSingleDemand", "rawAlbumDemand", "rawTotalDemand",
      "serviceableSingleIntent", "serviceableAlbumIntent", "serviceableTotalIntent",
      "purchaseCapacity", "clearedSingleUnits", "clearedAlbumUnits", "clearedTotalUnits",
      "unusedCapacity", "physicalBackorders", "marketDisplacedDemand"
    ]) {
      if (parsed[key] < 0) fail(`${source}.${key} is negative`);
    }
    assertNear(parsed.rawSingleDemand + parsed.rawAlbumDemand, parsed.rawTotalDemand,
      `${source} raw-demand reconciliation`, 0.2);
    assertEqual(parsed.serviceableSingleIntent + parsed.serviceableAlbumIntent,
      parsed.serviceableTotalIntent, `${source} serviceable-intent reconciliation`);
    assertEqual(parsed.clearedSingleUnits + parsed.clearedAlbumUnits,
      parsed.clearedTotalUnits, `${source} cleared-format reconciliation`);
    if (parsed.clearedTotalUnits > parsed.purchaseCapacity) fail(`${source} exceeds purchase capacity`);
    if (parsed.clearedTotalUnits > parsed.serviceableTotalIntent) fail(`${source} exceeds serviceable intent`);
    if (parsed.clearedSingleUnits > parsed.serviceableSingleIntent) fail(`${source} Single clearing exceeds intent`);
    if (parsed.clearedAlbumUnits > parsed.serviceableAlbumIntent) fail(`${source} Album clearing exceeds intent`);
    assertEqual(parsed.unusedCapacity, Math.max(0, parsed.purchaseCapacity - parsed.clearedTotalUnits),
      `${source} unused-capacity reconciliation`);
    assertEqual(parsed.marketDisplacedDemand,
      parsed.serviceableTotalIntent - parsed.clearedTotalUnits,
      `${source} market-displacement reconciliation`);
    const expectedFactor = parsed.serviceableTotalIntent > 0
      ? Math.min(1, parsed.clearedTotalUnits / parsed.serviceableTotalIntent)
      : 1;
    assertNear(parsed.rationingFactor, expectedFactor, `${source} rationing-factor reconciliation`, 1e-5);
    assertEqual(parsed.inventoryViolationCount, 0, `${source} inventory violations`);
    assertEqual(parsed.allocationViolationCount, 0, `${source} allocation violations`);
    assertEqual(parsed.reconciliationDelta, 0, `${source} allocation reconciliation delta`);
    return parsed;
  });

  uniqueMap(rows, row => `${row.week}|${row.year}|${row.regionId}`, "clearing");
  const byWeek = group(rows, row => `${row.week}|${row.year}`);
  let expectedRegions = null;
  const capacityByRegion = new Map();
  for (const [weekKey, weekRows] of byWeek) {
    const regions = weekRows.map(row => row.regionId).sort();
    if (new Set(regions).size !== regions.length) fail(`duplicate region in clearing week ${weekKey}`);
    if (expectedRegions === null) expectedRegions = regions;
    else if (regions.join("|") !== expectedRegions.join("|")) {
      fail(`regional clearing manifest changed at week ${weekKey}`);
    }
    for (const row of weekRows) {
      if (capacityByRegion.has(row.regionId) && capacityByRegion.get(row.regionId) !== row.purchaseCapacity) {
        fail(`purchase capacity changed for ${row.regionId}`);
      }
      capacityByRegion.set(row.regionId, row.purchaseCapacity);
    }
  }
  if (expectedRegions?.length !== 7) fail(`expected seven clearing regions, found ${expectedRegions?.length ?? 0}`);
  return { rows, byWeek, regions: expectedRegions };
}

const marketColumns = [
  "period", "week", "year", "labelTier", "releaseFormat",
  "totalMarketUnits", "gross", "labelNet", "distributionIncome", "marketNet"
];
const annualColumns = [
  "year", "singleUnits", "singleGross", "singleNet",
  "albumUnits", "albumGross", "albumNet", "decisions", "albumDecisionShare"
];
const releaseColumns = ["week", "year", "successfulReleases"];
const forkColumns = [
  "week", "year", "recordId", "priorSingleNet", "priorAlbumNet",
  "projectedSingleNet", "projectedAlbumNet", "chosenFormat"
];
const explanationColumns = [
  "week", "year", "recordId", "labelId", "chosenFormat",
  "singleMemoryBlend", "albumMemoryBlend", "confidenceSingle", "confidenceAlbum",
  "finalSingleMargin", "finalAlbumMargin", "memoryScope"
];

function loadEconomicRun(prefix, candidate) {
  return {
    market: parse(prefix, "market-revenue", { columns: marketColumns }).rows,
    weeks: parse(prefix, "weeks", { columns: ["week", "year", "totalMarketUnits"] }).rows,
    annual: parse(prefix, "decade-annual-rollup", { columns: annualColumns }).rows,
    releases: parse(prefix, "release-capacity", { columns: releaseColumns }).rows,
    forks: parse(prefix, "fork-ratios", { columns: forkColumns }).rows,
    explanations: candidate
      ? parse(prefix, "format-decision-explanation", { columns: explanationColumns }).rows
      : [],
    strategies: parse(prefix, "release-strategy", {
      columns: ["week", "year", "recordId", "chosenFormat"]
    }).rows
  };
}

function normalizeEconomicRun(prefix, data) {
  const weeklyMarket = data.market.filter(row =>
    row.period === "weekly" && row.labelTier === "All" && row.releaseFormat === "All");
  const weeklyMarketByKey = uniqueMap(weeklyMarket, row => `${row.week}|${row.year}`,
    `${prefix} weekly All/All market-revenue`);
  const weekByKey = uniqueMap(data.weeks, row => `${row.week}|${row.year}`, `${prefix} weeks`);
  if (weeklyMarketByKey.size !== weekByKey.size) {
    fail(`${prefix} weekly market-revenue/weeks row-count mismatch`);
  }
  for (const [key, row] of weekByKey) {
    const market = weeklyMarketByKey.get(key);
    if (!market) fail(`${prefix} missing weekly All/All market-revenue row for ${key}`);
    assertEqual(integer(row, "totalMarketUnits", `${prefix}.weeks.${key}`),
      integer(market, "totalMarketUnits", `${prefix}.market.${key}`),
      `${prefix} weeks/market-revenue units at ${key}`);
  }

  const annualByYear = uniqueMap(data.annual, row => row.year, `${prefix} annual rollup`);
  const releasesByYear = group(data.releases, row => row.year);
  const forksByYear = group(data.forks, row => row.year);
  uniqueMap(data.forks, row => row.recordId, `${prefix} fork recordId`);
  const strategyById = uniqueMap(data.strategies, row => row.recordId, `${prefix} release-strategy recordId`);
  if (strategyById.size !== data.forks.length) {
    fail(`${prefix} release-strategy/fork-ratios decision-count mismatch`);
  }
  for (const fork of data.forks) {
    const strategy = strategyById.get(fork.recordId);
    if (!strategy) fail(`${prefix} missing release-strategy row for ${fork.recordId}`);
    if (strategy.chosenFormat !== fork.chosenFormat) {
      fail(`${prefix} chosen-format mismatch for ${fork.recordId}`);
    }
  }

  const marketAnnualRows = data.market.filter(row => row.period === "annual" && row.labelTier === "All");
  const marketAnnual = uniqueMap(marketAnnualRows, row => `${row.year}|${row.releaseFormat}`,
    `${prefix} annual market-revenue`);
  const summaries = new Map();
  for (const year of TARGET_YEARS) {
    const yearKey = String(year);
    const rollup = annualByYear.get(yearKey);
    const single = marketAnnual.get(`${year}|Single`);
    const album = marketAnnual.get(`${year}|Album`);
    const all = marketAnnual.get(`${year}|All`);
    if (!rollup || !single || !album || !all) fail(`${prefix} lacks complete annual economics for ${year}`);

    const singleUnits = integer(single, "totalMarketUnits", `${prefix}.${year}.Single`);
    const albumUnits = integer(album, "totalMarketUnits", `${prefix}.${year}.Album`);
    const totalUnits = integer(all, "totalMarketUnits", `${prefix}.${year}.All`);
    assertEqual(singleUnits + albumUnits, totalUnits, `${prefix} ${year} format-unit reconciliation`);
    assertEqual(integer(rollup, "singleUnits", `${prefix}.rollup.${year}`), singleUnits,
      `${prefix} ${year} Single rollup reconciliation`);
    assertEqual(integer(rollup, "albumUnits", `${prefix}.rollup.${year}`), albumUnits,
      `${prefix} ${year} Album rollup reconciliation`);
    assertNear(number(rollup, "singleGross", `${prefix}.rollup.${year}`),
      number(single, "gross", `${prefix}.market.${year}.Single`),
      `${prefix} ${year} Single gross reconciliation`);
    assertNear(number(rollup, "albumGross", `${prefix}.rollup.${year}`),
      number(album, "gross", `${prefix}.market.${year}.Album`),
      `${prefix} ${year} Album gross reconciliation`);
    assertNear(number(rollup, "singleNet", `${prefix}.rollup.${year}`),
      number(single, "labelNet", `${prefix}.market.${year}.Single`),
      `${prefix} ${year} Single label-net reconciliation`);
    assertNear(number(rollup, "albumNet", `${prefix}.rollup.${year}`),
      number(album, "labelNet", `${prefix}.market.${year}.Album`),
      `${prefix} ${year} Album label-net reconciliation`);

    const decisions = forksByYear.get(yearKey) ?? [];
    const albums = decisions.filter(row => row.chosenFormat === "Album").length;
    if (decisions.some(row => row.chosenFormat !== "Single" && row.chosenFormat !== "Album")) {
      fail(`${prefix} ${year} contains an unknown chosen format`);
    }
    assertEqual(integer(rollup, "decisions", `${prefix}.rollup.${year}`), decisions.length,
      `${prefix} ${year} decision reconciliation`);
    if (decisions.length) {
      assertNear(number(rollup, "albumDecisionShare", `${prefix}.rollup.${year}`),
        albums / decisions.length, `${prefix} ${year} Album-decision-share reconciliation`, 1e-5);
    }
    const releaseRows = releasesByYear.get(yearKey);
    if (!releaseRows?.length) fail(`${prefix} lacks release-capacity rows for ${year}`);
    summaries.set(year, {
      year,
      singleUnits,
      albumUnits,
      totalUnits,
      singleGross: number(single, "gross", `${prefix}.${year}.Single`),
      albumGross: number(album, "gross", `${prefix}.${year}.Album`),
      gross: number(all, "gross", `${prefix}.${year}.All`),
      labelNet: number(all, "labelNet", `${prefix}.${year}.All`),
      marketNet: number(all, "marketNet", `${prefix}.${year}.All`),
      releases: sum(releaseRows, row => integer(row, "successfulReleases", `${prefix}.release.${year}`)),
      decisions: decisions.length,
      scheduledAlbums: albums,
      albumShare: decisions.length ? albums / decisions.length : 0
    });
  }
  return { ...data, summaries, weekByKey, weeklyMarketByKey };
}

function reconcileClearing(clearing, economic) {
  for (const [key, rows] of clearing.byWeek) {
    const week = economic.weekByKey.get(key);
    const market = economic.weeklyMarketByKey.get(key);
    if (!week || !market) fail(`clearing week ${key} is absent from weeks/market-revenue`);
    const summary = clearingSummary(rows);
    const total = summary.clearedSingle + summary.clearedAlbum;
    assertEqual(total, integer(week, "totalMarketUnits", `weeks.${key}`),
      `clearing/weeks units at ${key}`);
    assertEqual(total, integer(market, "totalMarketUnits", `market-revenue.${key}`),
      `clearing/market-revenue units at ${key}`);
  }
  if (clearing.byWeek.size !== economic.weekByKey.size) {
    fail(`clearing/weeks row-count mismatch: ${clearing.byWeek.size} != ${economic.weekByKey.size}`);
  }
  const clearingByYear = group(clearing.rows, row => row.year);
  for (const year of TARGET_YEARS) {
    const rows = clearingByYear.get(year);
    if (!rows?.length) fail(`clearing telemetry lacks year ${year}`);
    const summary = clearingSummary(rows);
    const annual = economic.summaries.get(year);
    assertEqual(summary.clearedSingle, annual.singleUnits,
      `clearing/annual-rollup Single units for ${year}`);
    assertEqual(summary.clearedAlbum, annual.albumUnits,
      `clearing/annual-rollup Album units for ${year}`);
  }
}

function parseAndReconcileMemory(economic) {
  const memory = parse(run, "format-memory-adjustment", {
    columns: [
      "week", "year", "recordId", "labelId", "memoryScope",
      "rawSingleConfidence", "rawAlbumConfidence",
      "effectiveSingleConfidence", "effectiveAlbumConfidence",
      "singleCapApplied", "albumCapApplied"
    ]
  });
  const explanationById = uniqueMap(economic.explanations, row => row.recordId,
    `${run} decision-explanation recordId`);
  const forkById = uniqueMap(economic.forks, row => row.recordId, `${run} fork recordId`);
  const rows = memory.rows.map((row, index) => {
    const source = `${memory.name}[${index + 2}]`;
    const parsed = {
      week: integer(row, "week", source),
      year: integer(row, "year", source),
      recordId: row.recordId,
      labelId: row.labelId,
      memoryScope: row.memoryScope,
      rawSingleConfidence: number(row, "rawSingleConfidence", source),
      rawAlbumConfidence: number(row, "rawAlbumConfidence", source),
      effectiveSingleConfidence: number(row, "effectiveSingleConfidence", source),
      effectiveAlbumConfidence: number(row, "effectiveAlbumConfidence", source),
      singleCapApplied: boolean(row, "singleCapApplied", source),
      albumCapApplied: boolean(row, "albumCapApplied", source)
    };
    if (!parsed.recordId || !parsed.labelId) fail(`${source} has a blank identity`);
    if (parsed.memoryScope !== "LabelFormat" && parsed.memoryScope !== "ProjectPrior") {
      fail(`${source} has unknown memory scope ${parsed.memoryScope}`);
    }
    for (const key of [
      "rawSingleConfidence", "rawAlbumConfidence",
      "effectiveSingleConfidence", "effectiveAlbumConfidence"
    ]) {
      if (parsed[key] < 0 || parsed[key] > 1 + EPSILON) fail(`${source}.${key} is outside [0,1]`);
    }
    if (parsed.effectiveSingleConfidence > parsed.rawSingleConfidence + EPSILON ||
        parsed.effectiveAlbumConfidence > parsed.rawAlbumConfidence + EPSILON) {
      fail(`${source} effective confidence exceeds raw confidence`);
    }
    if (parsed.memoryScope === "ProjectPrior") {
      assertNear(parsed.effectiveSingleConfidence, 0, `${source} ProjectPrior Single confidence`, EPSILON);
      assertNear(parsed.effectiveAlbumConfidence, 0, `${source} ProjectPrior Album confidence`, EPSILON);
    } else if (parsed.effectiveSingleConfidence > 0.75 + EPSILON ||
               parsed.effectiveAlbumConfidence > 0.75 + EPSILON) {
      fail(`${source} LabelFormat confidence exceeds 0.75`);
    }
    const expectedSingle = Math.min(parsed.rawSingleConfidence, 0.75);
    const expectedAlbum = Math.min(parsed.rawAlbumConfidence, 0.75);
    assertNear(parsed.effectiveSingleConfidence, expectedSingle,
      `${source} effective Single confidence`, EPSILON);
    assertNear(parsed.effectiveAlbumConfidence, expectedAlbum,
      `${source} effective Album confidence`, EPSILON);
    assertEqual(parsed.singleCapApplied, parsed.rawSingleConfidence > 0.75,
      `${source} Single cap-applied flag`);
    assertEqual(parsed.albumCapApplied, parsed.rawAlbumConfidence > 0.75,
      `${source} Album cap-applied flag`);

    const explanation = explanationById.get(parsed.recordId);
    const fork = forkById.get(parsed.recordId);
    if (!explanation || !fork) fail(`${source} has no joined decision trace`);
    if (integer(explanation, "week", `explanation.${parsed.recordId}`) !== parsed.week ||
        integer(explanation, "year", `explanation.${parsed.recordId}`) !== parsed.year) {
      fail(`${source} week/year does not match its decision trace`);
    }
    if (explanation.memoryScope !== parsed.memoryScope) {
      fail(`${source} memory scope does not match decision explanation`);
    }
    assertNear(number(explanation, "confidenceSingle", `explanation.${parsed.recordId}`),
      parsed.effectiveSingleConfidence, `${source} Single confidence/explanation`, 1e-5);
    assertNear(number(explanation, "confidenceAlbum", `explanation.${parsed.recordId}`),
      parsed.effectiveAlbumConfidence, `${source} Album confidence/explanation`, 1e-5);
    return parsed;
  });
  uniqueMap(rows, row => `${row.week}|${row.recordId}`, "format-memory adjustment");
  return { rows, explanationById, forkById };
}

function decisionStages(economic, memoryJoin) {
  const rows = economic.forks.map(fork => {
    const recordId = fork.recordId;
    const explanation = memoryJoin.explanationById.get(recordId);
    if (!explanation) fail(`missing format-decision explanation for ${recordId}`);
    if (fork.chosenFormat !== explanation.chosenFormat) {
      fail(`chosen-format mismatch in decision trace for ${recordId}`);
    }
    const priorSingle = number(fork, "priorSingleNet", `fork.${recordId}`);
    const priorAlbum = number(fork, "priorAlbumNet", `fork.${recordId}`);
    const memorySingle = number(explanation, "singleMemoryBlend", `explanation.${recordId}`);
    const memoryAlbum = number(explanation, "albumMemoryBlend", `explanation.${recordId}`);
    const finalSingle = number(explanation, "finalSingleMargin", `explanation.${recordId}`);
    const finalAlbum = number(explanation, "finalAlbumMargin", `explanation.${recordId}`);
    const finalChoice = finalAlbum > finalSingle ? "Album" : "Single";
    if (finalChoice !== fork.chosenFormat) fail(`final-margin decision mismatch for ${recordId}`);
    return {
      year: integer(fork, "year", `fork.${recordId}`),
      recordId,
      scope: explanation.memoryScope,
      priorAlbum: priorAlbum > priorSingle,
      memoryAlbum: memoryAlbum > memorySingle,
      finalAlbum: fork.chosenFormat === "Album"
    };
  });
  const summarize = groupRows => ({
    decisions: groupRows.length,
    priorAlbums: groupRows.filter(row => row.priorAlbum).length,
    memoryAlbums: groupRows.filter(row => row.memoryAlbum).length,
    finalAlbums: groupRows.filter(row => row.finalAlbum).length
  });
  return {
    rows,
    byYear: new Map([...group(rows, row => row.year)].map(([key, values]) => [key, summarize(values)])),
    byYearScope: new Map([...group(rows, row => `${row.year}|${row.scope}`)]
      .map(([key, values]) => [key, summarize(values)]))
  };
}

function parseScoutingFailures() {
  const scouting = parse(run, "label-scouting-vacancy-weekly", {
    columns: ["week", "year", "labelId", "maxRosterSize", "operatingRosterTarget", "rosterSize"]
  });
  return scouting.rows.filter((row, index) => {
    const source = `${scouting.name}[${index + 2}]`;
    const max = integer(row, "maxRosterSize", source);
    const target = integer(row, "operatingRosterTarget", source);
    const roster = integer(row, "rosterSize", source);
    return roster > target || target > max;
  });
}

function aggregateSummaries(summaries) {
  const rows = TARGET_YEARS.map(year => summaries.get(year));
  const keys = [
    "singleUnits", "albumUnits", "totalUnits", "singleGross", "albumGross", "gross",
    "labelNet", "marketNet", "releases", "decisions", "scheduledAlbums"
  ];
  const result = {};
  for (const key of keys) result[key] = sum(rows, row => row[key]);
  result.albumShare = result.scheduledAlbums / result.decisions;
  return result;
}

function addRatioGate(failures, scope, metric, enabled, control, low, high) {
  const value = ratio(enabled, control, `${scope} ${metric}`);
  if (value < low - EPSILON || value > high + EPSILON) {
    failures.push({
      type: "ORDINARY",
      gate: `${scope} ${metric}`,
      enabled,
      control,
      value,
      band: `[${low.toFixed(2)},${high.toFixed(2)}]`
    });
  }
  return value;
}

const enabled = normalizeEconomicRun(run, loadEconomicRun(run, true));
const control = normalizeEconomicRun(controlRun, loadEconomicRun(controlRun, false));
const clearing = parseClearing();
reconcileClearing(clearing, enabled);
const memory = parseAndReconcileMemory(enabled);
const stages = decisionStages(enabled, memory);
const scoutingFailures = parseScoutingFailures();
const catastrophic = parse(run, "catastrophic-fail-fast", {
  columns: ["gate", "metric", "enabledValue", "controlValue", "week", "date", "state"],
  allowEmpty: true
});

const failures = [];
if (scoutingFailures.length) {
  failures.push({
    type: "INVARIANT",
    gate: "operating-target snapshot",
    value: scoutingFailures.length,
    band: "rosterSize <= operatingRosterTarget <= maxRosterSize"
  });
}
for (const row of catastrophic.rows) {
  failures.push({
    type: "CATASTROPHIC",
    gate: `${row.gate}/${row.metric}`,
    enabled: row.enabledValue,
    control: row.controlValue,
    value: row.week,
    band: `no data rows; date=${row.date}; state=${row.state}`
  });
}

const annualRatios = new Map();
for (const year of TARGET_YEARS) {
  const candidate = enabled.summaries.get(year);
  const baseline = control.summaries.get(year);
  const values = {
    releases: addRatioGate(failures, String(year), "successful releases",
      candidate.releases, baseline.releases, 0.85, 1.15),
    scheduledAlbums: addRatioGate(failures, String(year), "scheduled Albums",
      candidate.scheduledAlbums, baseline.scheduledAlbums, 0.80, 1.20),
    singleUnits: addRatioGate(failures, String(year), "Single units",
      candidate.singleUnits, baseline.singleUnits, 0.85, 1.15),
    albumUnits: addRatioGate(failures, String(year), "Album units",
      candidate.albumUnits, baseline.albumUnits, 0.80, 1.20),
    totalUnits: addRatioGate(failures, String(year), "total units",
      candidate.totalUnits, baseline.totalUnits, 0.85, 1.15),
    gross: addRatioGate(failures, String(year), "gross",
      candidate.gross, baseline.gross, 0.85, 1.15),
    labelNet: addRatioGate(failures, String(year), "label net",
      candidate.labelNet, baseline.labelNet, 0.85, 1.15),
    marketNet: addRatioGate(failures, String(year), "market net",
      candidate.marketNet, baseline.marketNet, 0.85, 1.15)
  };
  annualRatios.set(year, values);
}

const enabledDecade = aggregateSummaries(enabled.summaries);
const controlDecade = aggregateSummaries(control.summaries);
const decadeRatios = {
  releases: addRatioGate(failures, "Decade", "successful releases",
    enabledDecade.releases, controlDecade.releases, 0.85, 1.15),
  scheduledAlbums: addRatioGate(failures, "Decade", "scheduled Albums",
    enabledDecade.scheduledAlbums, controlDecade.scheduledAlbums, 0.80, 1.20),
  singleUnits: addRatioGate(failures, "Decade", "Single units",
    enabledDecade.singleUnits, controlDecade.singleUnits, 0.85, 1.15),
  albumUnits: addRatioGate(failures, "Decade", "Album units",
    enabledDecade.albumUnits, controlDecade.albumUnits, 0.80, 1.20),
  totalUnits: addRatioGate(failures, "Decade", "total units",
    enabledDecade.totalUnits, controlDecade.totalUnits, 0.90, 1.10),
  gross: addRatioGate(failures, "Decade", "gross",
    enabledDecade.gross, controlDecade.gross, 0.90, 1.10),
  labelNet: addRatioGate(failures, "Decade", "label net",
    enabledDecade.labelNet, controlDecade.labelNet, 0.90, 1.10),
  marketNet: addRatioGate(failures, "Decade", "market net",
    enabledDecade.marketNet, controlDecade.marketNet, 0.90, 1.10)
};

const share1969 = enabled.summaries.get(1969).albumShare;
if (share1969 < 0.78 - EPSILON || share1969 > 0.85 + EPSILON) {
  failures.push({
    type: "ORDINARY",
    gate: "1969 scheduled-Album share",
    enabled: enabled.summaries.get(1969).scheduledAlbums,
    control: enabled.summaries.get(1969).decisions,
    value: share1969,
    band: "[0.78,0.85]"
  });
}

const lines = [
  "# Market clearing and format-memory report",
  "",
  `Enabled: \`${run}\`  `,
  `Control: \`${controlRun}\``,
  "",
  "## Exact reconciliation",
  "",
  `- Clearing rows: ${clearing.rows.length}`,
  `- Regions: ${clearing.regions.join(", ")}`,
  `- Weeks reconciled to weeks.csv and market-revenue.csv: ${clearing.byWeek.size}`,
  "- Annual Single/Album units reconciled to market-revenue.csv and decade-annual-rollup.csv: 1960-1969",
  `- Memory adjustments joined to decision telemetry: ${memory.rows.length}`,
  `- Operating-target snapshot violations: ${scoutingFailures.length}`,
  `- Catastrophic rows: ${catastrophic.rows.length}`,
  "",
  "## Clearing by year",
  "",
  "| Year | Capacity | Cleared | Utilization | Single displaced | Album displaced | Total displaced | Physical backorders |",
  "|---:|---:|---:|---:|---:|---:|---:|---:|"
];

const clearingByYear = group(clearing.rows, row => row.year);
for (const year of [...clearingByYear.keys()].sort((a, b) => a - b)) {
  const summary = clearingSummary(clearingByYear.get(year));
  const cleared = summary.clearedSingle + summary.clearedAlbum;
  lines.push(`| ${year} | ${summary.capacity} | ${cleared} | ${f(cleared / summary.capacity)} | ` +
    `${summary.displacedSingle} | ${summary.displacedAlbum} | ` +
    `${summary.displacedSingle + summary.displacedAlbum} | ${summary.physicalBackorders} |`);
}

lines.push(
  "",
  "## Clearing by region",
  "",
  "| Region | Capacity | Cleared | Utilization | Single displaced | Album displaced | Total displaced |",
  "|---|---:|---:|---:|---:|---:|---:|"
);
const clearingByRegion = group(clearing.rows, row => row.regionId);
for (const region of [...clearingByRegion.keys()].sort()) {
  const summary = clearingSummary(clearingByRegion.get(region));
  const cleared = summary.clearedSingle + summary.clearedAlbum;
  lines.push(`| ${region} | ${summary.capacity} | ${cleared} | ${f(cleared / summary.capacity)} | ` +
    `${summary.displacedSingle} | ${summary.displacedAlbum} | ` +
    `${summary.displacedSingle + summary.displacedAlbum} |`);
}

const allClearing = clearingSummary(clearing.rows);
lines.push(
  "",
  "## Clearing by format",
  "",
  "| Format | Serviceable intent | Cleared | Displaced | Clearance |",
  "|---|---:|---:|---:|---:|",
  `| Single | ${allClearing.serviceableSingle} | ${allClearing.clearedSingle} | ` +
    `${allClearing.displacedSingle} | ${f(allClearing.clearedSingle / Math.max(1, allClearing.serviceableSingle))} |`,
  `| Album | ${allClearing.serviceableAlbum} | ${allClearing.clearedAlbum} | ` +
    `${allClearing.displacedAlbum} | ${f(allClearing.clearedAlbum / Math.max(1, allClearing.serviceableAlbum))} |`,
  "",
  "## Format-memory confidence",
  "",
  "| Year | Scope | Decisions | Raw Single | Effective Single | Raw Album | Effective Album | Single caps | Album caps |",
  "|---:|---|---:|---:|---:|---:|---:|---:|---:|"
);
const memoryGroups = group(memory.rows, row => `${row.year}|${row.memoryScope}`);
for (const key of [...memoryGroups.keys()].sort((left, right) => left.localeCompare(right))) {
  const rows = memoryGroups.get(key);
  const [year, scope] = key.split("|");
  lines.push(`| ${year} | ${scope} | ${rows.length} | ` +
    `${f(mean(rows, row => row.rawSingleConfidence))} | ${f(mean(rows, row => row.effectiveSingleConfidence))} | ` +
    `${f(mean(rows, row => row.rawAlbumConfidence))} | ${f(mean(rows, row => row.effectiveAlbumConfidence))} | ` +
    `${rows.filter(row => row.singleCapApplied).length} | ${rows.filter(row => row.albumCapApplied).length} |`);
}

lines.push(
  "",
  "## Format-decision stages",
  "",
  "| Year | Decisions | Prior Albums | Prior share | After-memory Albums | After-memory share | Final Albums | Final share |",
  "|---:|---:|---:|---:|---:|---:|---:|---:|"
);
for (const year of [...stages.byYear.keys()].sort((a, b) => a - b)) {
  const stage = stages.byYear.get(year);
  lines.push(`| ${year} | ${stage.decisions} | ${stage.priorAlbums} | ${f(stage.priorAlbums / stage.decisions)} | ` +
    `${stage.memoryAlbums} | ${f(stage.memoryAlbums / stage.decisions)} | ` +
    `${stage.finalAlbums} | ${f(stage.finalAlbums / stage.decisions)} |`);
}

lines.push(
  "",
  "### Decision stages by memory scope",
  "",
  "| Year | Scope | Decisions | Prior Album share | After-memory Album share | Final Album share |",
  "|---:|---|---:|---:|---:|---:|"
);
for (const key of [...stages.byYearScope.keys()].sort()) {
  const [year, scope] = key.split("|");
  const stage = stages.byYearScope.get(key);
  lines.push(`| ${year} | ${scope} | ${stage.decisions} | ${f(stage.priorAlbums / stage.decisions)} | ` +
    `${f(stage.memoryAlbums / stage.decisions)} | ${f(stage.finalAlbums / stage.decisions)} |`);
}

lines.push(
  "",
  "## Annual control ratios",
  "",
  "| Year | Releases | Scheduled Albums | Album share | Single units | Album units | Total units | Gross | Label net | Market net |",
  "|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|"
);
for (const year of TARGET_YEARS) {
  const values = annualRatios.get(year);
  lines.push(`| ${year} | ${f(values.releases)} | ${f(values.scheduledAlbums)} | ` +
    `${f(enabled.summaries.get(year).albumShare)} | ${f(values.singleUnits)} | ${f(values.albumUnits)} | ` +
    `${f(values.totalUnits)} | ${f(values.gross)} | ${f(values.labelNet)} | ${f(values.marketNet)} |`);
}

lines.push(
  "",
  "## Decade control ratios",
  "",
  "| Releases | Scheduled Albums | Single units | Album units | Total units | Gross | Label net | Market net |",
  "|---:|---:|---:|---:|---:|---:|---:|---:|",
  `| ${f(decadeRatios.releases)} | ${f(decadeRatios.scheduledAlbums)} | ${f(decadeRatios.singleUnits)} | ` +
    `${f(decadeRatios.albumUnits)} | ${f(decadeRatios.totalUnits)} | ${f(decadeRatios.gross)} | ` +
    `${f(decadeRatios.labelNet)} | ${f(decadeRatios.marketNet)} |`,
  "",
  "## Gate failures",
  ""
);

if (failures.length === 0) {
  lines.push("PASS: no ordinary, invariant, or catastrophic gate failures.");
} else {
  lines.push("| Type | Gate | Enabled | Control | Ratio/value | Required |", "|---|---|---:|---:|---:|---|");
  for (const failure of failures) {
    const value = typeof failure.value === "number" ? f(failure.value, 6) : String(failure.value ?? "");
    lines.push(`| ${failure.type} | ${failure.gate} | ${failure.enabled ?? ""} | ${failure.control ?? ""} | ` +
      `${value} | ${failure.band} |`);
  }
}

lines.push("", "GENRE_DIAGNOSTICS_DEFERRED");
process.stdout.write(lines.join("\n") + "\n");
process.exitCode = failures.length ? 1 : 0;
