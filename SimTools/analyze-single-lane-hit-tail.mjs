#!/usr/bin/env node
// Focused, fail-closed acceptance checker for Single-lane telemetry.
import fs from 'node:fs';
import path from 'node:path';
import { StringDecoder } from 'node:string_decoder';

const positional = process.argv.slice(2).filter(value => !value.startsWith('--'));
const options = Object.fromEntries(process.argv.slice(2).filter(value => value.startsWith('--')).map(value => {
  const split = value.indexOf('=');
  return split < 0 ? [value.slice(2), true] : [value.slice(2, split), value.slice(split + 1)];
}));
const [logsDir, candidatePrefix] = positional;
if (!logsDir || !candidatePrefix) {
  throw new Error('usage: analyze-single-lane-hit-tail.mjs <logs-dir> <candidate-prefix> [--repeat-prefix=<prefix>] [--control-prefix=<prefix>] [--json=<file>]');
}

const failures = [];
const warnings = [];
const fail = message => failures.push(message);
const warn = message => warnings.push(message);
const fileFor = (prefix, suffix) => path.join(logsDir, `${prefix}${suffix}`);

function parseCsvLine(line) {
  const values = [];
  let value = '';
  let quoted = false;
  for (let index = 0; index < line.length; index++) {
    const character = line[index];
    if (character === '"') {
      if (quoted && line[index + 1] === '"') { value += '"'; index++; }
      else quoted = !quoted;
    } else if (character === ',' && !quoted) {
      values.push(value);
      value = '';
    } else value += character;
  }
  values.push(value);
  return values;
}

function readCsv(prefix, suffix, { optional = false, allowEmpty = false } = {}) {
  const file = fileFor(prefix, suffix);
  if (!fs.existsSync(file)) {
    if (optional) return null;
    throw new Error(`missing required telemetry: ${file}`);
  }
  const lines = fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '').trimEnd().split(/\r?\n/);
  if (!lines.length || !lines[0]) throw new Error(`missing CSV header: ${file}`);
  const columns = parseCsvLine(lines[0]);
  const rows = lines.slice(1).filter(Boolean).map((line, rowIndex) => {
    const values = parseCsvLine(line);
    if (values.length !== columns.length) throw new Error(`column mismatch in ${file}:${rowIndex + 2}: expected ${columns.length}, got ${values.length}`);
    return Object.fromEntries(columns.map((column, index) => [column, values[index]]));
  });
  if (!allowEmpty && rows.length === 0) throw new Error(`no data rows in required telemetry: ${file}`);
  return { file, columns, rows };
}

function *readLinesSync(file) {
  const descriptor = fs.openSync(file, 'r');
  const decoder = new StringDecoder('utf8');
  const chunk = Buffer.allocUnsafe(1024 * 1024);
  let pending = '';
  try {
    for (;;) {
      const bytes = fs.readSync(descriptor, chunk, 0, chunk.length, null);
      if (!bytes) break;
      pending += decoder.write(chunk.subarray(0, bytes));
      let newline;
      while ((newline = pending.indexOf('\n')) >= 0) {
        let line = pending.slice(0, newline);
        pending = pending.slice(newline + 1);
        if (line.endsWith('\r')) line = line.slice(0, -1);
        yield line;
      }
    }
    pending += decoder.end();
    if (pending) yield pending.endsWith('\r') ? pending.slice(0, -1) : pending;
  } finally {
    fs.closeSync(descriptor);
  }
}

function forEachCsvRow(prefix, suffix, requiredColumns, visit, { optional = false, allowEmpty = false } = {}) {
  const file = fileFor(prefix, suffix);
  if (!fs.existsSync(file)) {
    if (optional) return null;
    throw new Error(`missing required telemetry: ${file}`);
  }
  let columns = null;
  let rowCount = 0;
  for (let line of readLinesSync(file)) {
    if (columns === null) {
      line = line.replace(/^\uFEFF/, '');
      if (!line) throw new Error(`missing CSV header: ${file}`);
      columns = parseCsvLine(line);
      requireColumns({ file, columns }, requiredColumns);
      continue;
    }
    if (!line) continue;
    const values = parseCsvLine(line);
    if (values.length !== columns.length) throw new Error(`column mismatch in ${file}:${rowCount + 2}: expected ${columns.length}, got ${values.length}`);
    const row = Object.fromEntries(columns.map((column, index) => [column, values[index]]));
    visit(row, rowCount, `${path.basename(file)}:${rowCount + 2}`);
    rowCount++;
  }
  if (columns === null) throw new Error(`missing CSV header: ${file}`);
  if (!allowEmpty && rowCount === 0) throw new Error(`no data rows in required telemetry: ${file}`);
  return { file, columns, rowCount };
}

function requireColumns(csv, columns) {
  for (const column of columns) if (!csv.columns.includes(column)) fail(`missing column ${column} in ${csv.file}`);
}

function number(row, field, context) {
  const value = Number(row[field]);
  if (!Number.isFinite(value)) {
    fail(`non-finite ${field} at ${context}`);
    return 0;
  }
  return value;
}

function percentile(sorted, fraction) {
  if (!sorted.length) return 0;
  const position = (sorted.length - 1) * fraction;
  const lower = Math.floor(position), upper = Math.ceil(position);
  if (lower === upper) return sorted[lower];
  return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
}

function topShare(values, fraction) {
  const total = values.reduce((sum, value) => sum + value, 0);
  if (total <= 0 || values.length === 0) return 0;
  const count = Math.max(1, Math.ceil(values.length * fraction));
  return [...values].sort((a, b) => b - a).slice(0, count).reduce((sum, value) => sum + value, 0) / total;
}

function gini(values) {
  const sorted = [...values].filter(value => value >= 0).sort((a, b) => a - b);
  const total = sorted.reduce((sum, value) => sum + value, 0);
  if (!sorted.length || total <= 0) return 0;
  let weighted = 0;
  for (let index = 0; index < sorted.length; index++) weighted += (index + 1) * sorted[index];
  return (2 * weighted) / (sorted.length * total) - (sorted.length + 1) / sorted.length;
}

function distribution(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const median = percentile(sorted, .5);
  return {
    count: sorted.length,
    units: sorted.reduce((sum, value) => sum + value, 0),
    mean: sorted.length ? sorted.reduce((sum, value) => sum + value, 0) / sorted.length : 0,
    median,
    p90: percentile(sorted, .90),
    p99: percentile(sorted, .99),
    maximum: sorted.at(-1) ?? 0,
    p99Median: median > 0 ? percentile(sorted, .99) / median : null,
    top10Share: topShare(sorted, .10),
    top1Share: topShare(sorted, .01),
    gini: gini(sorted)
  };
}

function analyze(prefix) {
  const releasesCsv = readCsv(prefix, '-single-release-lanes.csv');
  const memoryCsv = readCsv(prefix, '-format-memory-revisions.csv', { allowEmpty: true });
  const clearingCsv = readCsv(prefix, '-market-clearing-weekly.csv');
  const catastrophicCsv = readCsv(prefix, '-catastrophic-fail-fast.csv', { optional: true, allowEmpty: true });

  requireColumns(releasesCsv, ['week','year','recordId','projectId','releaseLane','genre','careerState','quality','enabledOpportunityMass','acceptedOpportunityMass','cohortNormalizer','normalizerSource','coldStartFallback']);
  requireColumns(memoryCsv, ['week','releaseId','labelId','projectId','releaseLane','estimatorLane','revisionOrdinal','nonFiniteViolation']);
  requireColumns(clearingCsv, ['week','inventoryViolationCount','allocationViolationCount','reconciliationDelta','settlementDelta']);

  const allowedLanes = new Set(['OrphanSingle', 'PromoSingle', 'ExternalOrLegacy']);
  const releases = new Map();
  const laneCounts = Object.fromEntries([...allowedLanes].map(lane => [lane, 0]));
  const normalizer = {};
  for (const [index, row] of releasesCsv.rows.entries()) {
    const context = `${path.basename(releasesCsv.file)}:${index + 2}`;
    if (!row.recordId) fail(`blank recordId at ${context}`);
    if (releases.has(row.recordId)) fail(`duplicate release identity ${row.recordId}`);
    if (!allowedLanes.has(row.releaseLane)) fail(`unclassified Single ${row.recordId}: ${row.releaseLane || '<blank>'}`);
    releases.set(row.recordId, row);
    laneCounts[row.releaseLane] = (laneCounts[row.releaseLane] ?? 0) + 1;
    const value = number(row, 'cohortNormalizer', context);
    if (value < .25 || value > 4) fail(`normalizer safety bound hit for ${row.recordId}: ${value}`);
    const key = `${row.releaseLane}|${row.normalizerSource}|${row.coldStartFallback}`;
    const rollup = normalizer[key] ?? { lane: row.releaseLane, source: row.normalizerSource, coldStartFallback: row.coldStartFallback === 'true', releases: 0, enabledOpportunityMass: 0, acceptedOpportunityMass: 0, min: value, max: value };
    rollup.releases++;
    rollup.enabledOpportunityMass += number(row, 'enabledOpportunityMass', context);
    rollup.acceptedOpportunityMass += number(row, 'acceptedOpportunityMass', context);
    rollup.min = Math.min(rollup.min, value);
    rollup.max = Math.max(rollup.max, value);
    normalizer[key] = rollup;
  }
  if (!laneCounts.OrphanSingle) fail('M2 coverage missing OrphanSingle releases');
  if (!laneCounts.PromoSingle) fail('M2 coverage missing PromoSingle releases');

  const demandByRecord = new Map();
  const demandUnitsByWeekRecord = new Map();
  let maxWeek = 0;
  let maxRawDemandResidual = 0;
  let maxRawDemandRelativeResidual = 0;
  let demandGroup = '';
  let demandGroupRegions = new Set();
  const demandCsv = forEachCsvRow(prefix, '-single-demand-stages.csv',
    ['week','year','recordId','releaseLane','region','age','potentialAudience','baselineAwareness','earnedDiscoveryExposure','awareBuyers','intrinsicConversionRate','rawDemand','serviceableDemand','clearedUnits'],
    (row, index, context) => {
    const release = releases.get(row.recordId);
    if (!release) fail(`partial join: demand record ${row.recordId} has no release identity`);
    else if (release.releaseLane !== row.releaseLane) fail(`lane drift for ${row.recordId}: ${release.releaseLane} -> ${row.releaseLane}`);
    const group = `${row.week}|${row.recordId}`;
    if (group !== demandGroup) { demandGroup = group; demandGroupRegions = new Set(); }
    if (demandGroupRegions.has(row.region)) fail(`duplicate demand-stage row ${group}|${row.region}`);
    demandGroupRegions.add(row.region);
    const week = number(row, 'week', context);
    const age = number(row, 'age', context);
    const potential = number(row, 'potentialAudience', context);
    const baseline = number(row, 'baselineAwareness', context);
    const exposure = number(row, 'earnedDiscoveryExposure', context);
    const aware = number(row, 'awareBuyers', context);
    const conversion = number(row, 'intrinsicConversionRate', context);
    const raw = number(row, 'rawDemand', context);
    const cleared = number(row, 'clearedUnits', context);
    maxWeek = Math.max(maxWeek, week);
    if (potential < 0 || aware < -.01 || aware > potential + Math.max(.5, potential * 1e-6)) fail(`aware buyers exceed potential audience at ${context}`);
    if (baseline < 0 || baseline > 1 || exposure < 0 || exposure > 1) fail(`awareness stage outside [0,1] at ${context}`);
    if (conversion < 0 || raw < 0 || cleared < 0) fail(`negative demand-stage value at ${context}`);
    const residual = Math.abs(aware * conversion - raw);
    const relativeResidual = residual / Math.max(1, Math.abs(raw));
    maxRawDemandResidual = Math.max(maxRawDemandResidual, residual);
    maxRawDemandRelativeResidual = Math.max(maxRawDemandRelativeResidual, relativeResidual);
    if (residual > Math.max(.5, Math.abs(raw) * 1e-5)) fail(`raw-demand reconstruction failed at ${context}: residual=${residual}`);
    const recordYield = demandByRecord.get(row.recordId) ?? { first3: 0, weeks4To14: 0 };
    if (age >= 1 && age <= 3) recordYield.first3 += cleared;
    else if (age >= 4 && age <= 14) recordYield.weeks4To14 += cleared;
    demandByRecord.set(row.recordId, recordYield);
    const weekRecordKey = `${week}|${row.recordId}`;
    demandUnitsByWeekRecord.set(weekRecordKey, (demandUnitsByWeekRecord.get(weekRecordKey) ?? 0) + cleared);
  });

  const settlementKeys = new Set();
  const settlementSingleUnitsByWeek = new Map();
  const settlementLaneUnitsByWeek = new Map();
  const settlementCsv = forEachCsvRow(prefix, '-completed-week-settlement.csv',
    ['week','settlementId','recordId','format','releaseLane','regionalUnits','totalUnits','retiredAfterSettlement','bookedCount','auditedCount'],
    (row, index, context) => {
    const key = `${row.settlementId}|${row.recordId}`;
    if (settlementKeys.has(key)) fail(`duplicate finance posting ${key}`);
    settlementKeys.add(key);
    const regional = number(row, 'regionalUnits', context);
    const total = number(row, 'totalUnits', context);
    if (regional !== total) fail(`regional/settlement unit mismatch at ${context}`);
    if (number(row, 'bookedCount', context) !== 1 || number(row, 'auditedCount', context) !== 1) fail(`finance posting count is not exactly once at ${context}`);
    if (row.format === 'Single') {
      if (!allowedLanes.has(row.releaseLane)) fail(`unclassified settlement Single ${row.recordId}: ${row.releaseLane || '<blank>'}`);
      settlementSingleUnitsByWeek.set(row.week, (settlementSingleUnitsByWeek.get(row.week) ?? 0) + total);
      const laneKey = `${row.week}|${row.releaseLane}`;
      settlementLaneUnitsByWeek.set(laneKey, (settlementLaneUnitsByWeek.get(laneKey) ?? 0) + total);
      const demandUnits = demandUnitsByWeekRecord.get(`${row.week}|${row.recordId}`);
      if (demandUnits === undefined) {
        if (row.retiredAfterSettlement !== 'true') fail(`settled Single ${row.recordId} is missing demand stages in week ${row.week} without retirement`);
      } else if (demandUnits !== total) fail(`demand/settlement units differ for ${row.recordId} in week ${row.week}: demand=${demandUnits}, settlement=${total}`);
    }
  });
  for (const [week, units] of settlementSingleUnitsByWeek) {
    let laneUnits = 0;
    for (const lane of allowedLanes) laneUnits += settlementLaneUnitsByWeek.get(`${week}|${lane}`) ?? 0;
    if (laneUnits !== units) fail(`weekly Single settlement-lane reconciliation failed in week ${week}: lanes=${laneUnits}, settlement=${units}`);
  }

  let clearingViolationRows = 0;
  for (const [index, row] of clearingCsv.rows.entries()) {
    const context = `${path.basename(clearingCsv.file)}:${index + 2}`;
    const fields = ['inventoryViolationCount','allocationViolationCount','reconciliationDelta','settlementDelta'];
    if (fields.some(field => number(row, field, context) !== 0)) clearingViolationRows++;
  }
  if (clearingViolationRows) fail(`${clearingViolationRows} market-clearing rows contain invariant violations`);

  const memoryKeys = new Set();
  const promoMemoryIds = new Set();
  let nonFiniteMemoryRows = 0;
  for (const [index, row] of memoryCsv.rows.entries()) {
    const context = `${path.basename(memoryCsv.file)}:${index + 2}`;
    const key = `${row.labelId}|${row.releaseId}|${row.estimatorLane}|${row.revisionOrdinal}`;
    if (memoryKeys.has(key)) fail(`duplicate memory observation ${key}`);
    memoryKeys.add(key);
    if (row.nonFiniteViolation === 'true') nonFiniteMemoryRows++;
    if (row.releaseLane === 'PromoSingle') {
      if (row.estimatorLane !== 'PromoSingle') fail(`promo ${row.releaseId} routed to ${row.estimatorLane} memory`);
      promoMemoryIds.add(row.releaseId);
    }
    if (row.estimatorLane === 'OrphanSingle' && row.releaseLane !== 'OrphanSingle') fail(`${row.releaseId} incorrectly routed to orphan memory`);
  }
  if (nonFiniteMemoryRows) fail(`${nonFiniteMemoryRows} non-finite memory rows`);
  for (const row of releases.values()) {
    if (row.releaseLane === 'PromoSingle' && maxWeek - Number(row.week) >= 12 && !promoMemoryIds.has(row.recordId)) fail(`mature promo ${row.recordId} has no memory observation`);
  }

  if (catastrophicCsv && catastrophicCsv.rows.length) fail(`catastrophic stream is not header-only: ${catastrophicCsv.file}`);

  const completedYears = completedAnnualYears(prefix);
  const yieldsByLane = {};
  const yieldsByYearLane = new Map();
  for (const row of releases.values()) {
    if (row.releaseLane === 'ExternalOrLegacy' || maxWeek - Number(row.week) < 13) continue;
    const recordYield = demandByRecord.get(row.recordId) ?? { first3: 0, weeks4To14: 0 };
    const first3 = recordYield.first3;
    const weeks4To14 = recordYield.weeks4To14;
    const lane = yieldsByLane[row.releaseLane] ?? { first3: [], weeks4To14: [], first14: [] };
    lane.first3.push(first3);
    lane.weeks4To14.push(weeks4To14);
    lane.first14.push(first3 + weeks4To14);
    yieldsByLane[row.releaseLane] = lane;
    if (completedYears.has(row.year)) {
      const key = `${row.year}|${row.releaseLane}`;
      const yearLane = yieldsByYearLane.get(key) ?? { year: Number(row.year), lane: row.releaseLane, first3: [], weeks4To14: [], first14: [] };
      yearLane.first3.push(first3);
      yearLane.weeks4To14.push(weeks4To14);
      yearLane.first14.push(first3 + weeks4To14);
      yieldsByYearLane.set(key, yearLane);
    }
  }
  const distributions = {};
  for (const [lane, values] of Object.entries(yieldsByLane)) {
    distributions[lane] = {
      first3: distribution(values.first3),
      weeks4To14: distribution(values.weeks4To14),
      first14: distribution(values.first14)
    };
  }
  const annualDistributions = {};
  for (const values of [...yieldsByYearLane.values()].sort((a, b) => a.year - b.year || a.lane.localeCompare(b.lane))) {
    const result = {
      first3: distribution(values.first3),
      weeks4To14: distribution(values.weeks4To14),
      first14: distribution(values.first14)
    };
    (annualDistributions[values.year] ??= {})[values.lane] = result;
    if (values.first14.length >= 200) {
      if (result.first14.top1Share > .35) fail(`${values.year} ${values.lane} top-1% first-14 yield share exceeds 35%`);
      if (result.first14.top10Share > .40) fail(`${values.year} ${values.lane} top-10% first-14 yield share exceeds 40%`);
    }
  }

  return {
    prefix,
    weeks: maxWeek,
    rows: { releases: releasesCsv.rows.length, demandStages: demandCsv.rowCount, memoryRevisions: memoryCsv.rows.length, settlements: settlementCsv.rowCount, marketClearing: clearingCsv.rows.length },
    laneCounts,
    normalizer: Object.values(normalizer),
    demand: { maxRawDemandResidual, maxRawDemandRelativeResidual },
    memory: { uniqueObservationKeys: memoryKeys.size, promoReleaseIdsObserved: promoMemoryIds.size, nonFiniteRows: nonFiniteMemoryRows },
    finance: { uniquePostingKeys: settlementKeys.size },
    marketClearing: { violationRows: clearingViolationRows },
    distributions,
    annualDistributions
  };
}

function annualFormatRows(prefix) {
  const csv = readCsv(prefix, '-format-mix.csv');
  requireColumns(csv, ['period','year','releaseFormat','units','gross','labelNet']);
  return csv.rows.filter(row => row.period === 'annual');
}

function completedAnnualYears(prefix) {
  const csv = readCsv(prefix, '-weeks.csv');
  requireColumns(csv, ['week','year']);
  const weeksByYear = new Map();
  for (const row of csv.rows) {
    const year = Number(row.year);
    const weeks = weeksByYear.get(year) ?? new Set();
    weeks.add(Number(row.week));
    weeksByYear.set(year, weeks);
  }
  return new Set([...weeksByYear].filter(([, weeks]) => weeks.size >= 52).map(([year]) => String(year)));
}

function compareAnnual(candidate, control) {
  const candidateRows = annualFormatRows(candidate);
  const controlRows = annualFormatRows(control);
  const completedYears = completedAnnualYears(candidate);
  const index = rows => new Map(rows.map(row => [`${row.year}|${row.releaseFormat}`, row]));
  const candidateIndex = index(candidateRows), controlIndex = index(controlRows);
  const years = [...new Set(candidateRows.map(row => row.year))]
    .filter(year => completedYears.has(year) && controlRows.some(row => row.year === year)).sort();
  const result = [];
  for (const year of years) {
    const cSingle = candidateIndex.get(`${year}|Single`), cAlbum = candidateIndex.get(`${year}|Album`);
    const bSingle = controlIndex.get(`${year}|Single`), bAlbum = controlIndex.get(`${year}|Album`);
    if (!cSingle || !cAlbum || !bSingle || !bAlbum) { warn(`incomplete annual format rows for ${year}`); continue; }
    const cSingleUnits = Number(cSingle.units), cAlbumUnits = Number(cAlbum.units);
    const bSingleUnits = Number(bSingle.units), bAlbumUnits = Number(bAlbum.units);
    const row = {
      year: Number(year),
      singleUnitsRatio: cSingleUnits / bSingleUnits,
      albumUnitsRatio: cAlbumUnits / bAlbumUnits,
      totalUnitsRatio: (cSingleUnits + cAlbumUnits) / (bSingleUnits + bAlbumUnits),
      grossRatio: (Number(cSingle.gross) + Number(cAlbum.gross)) / (Number(bSingle.gross) + Number(bAlbum.gross)),
      labelNetRatio: (Number(cSingle.labelNet) + Number(cAlbum.labelNet)) / (Number(bSingle.labelNet) + Number(bAlbum.labelNet))
    };
    if (row.singleUnitsRatio < .85 || row.singleUnitsRatio > 1.15) fail(`${year} Single-unit ratio outside [0.85,1.15]: ${row.singleUnitsRatio}`);
    if (row.totalUnitsRatio < .85 || row.totalUnitsRatio > 1.15) fail(`${year} total-unit ratio outside [0.85,1.15]: ${row.totalUnitsRatio}`);
    result.push(row);
  }
  return result;
}

function compareRepeat(candidate, repeat) {
  const suffixes = [
    '-single-release-lanes.csv', '-single-demand-stages.csv', '-format-memory-revisions.csv',
    '-completed-week-settlement.csv', '-market-clearing-weekly.csv', '-format-mix.csv'
  ];
  const differences = [];
  for (const suffix of suffixes) {
    const candidateFile = fileFor(candidate, suffix), repeatFile = fileFor(repeat, suffix);
    if (!fs.existsSync(candidateFile) || !fs.existsSync(repeatFile)) { differences.push(`${suffix}:missing`); continue; }
    if (!fs.readFileSync(candidateFile).equals(fs.readFileSync(repeatFile))) differences.push(suffix);
  }
  if (differences.length) fail(`deterministic repeat differs: ${differences.join(', ')}`);
  return { prefix: repeat, exactCoreTelemetry: differences.length === 0, differences };
}

const candidate = analyze(candidatePrefix);
let repeat = null;
if (options['repeat-prefix']) {
  const repeatAnalysis = analyze(options['repeat-prefix']);
  repeat = { analysis: repeatAnalysis, comparison: compareRepeat(candidatePrefix, options['repeat-prefix']) };
}
let annualControlComparison = [];
if (options['control-prefix']) annualControlComparison = compareAnnual(candidatePrefix, options['control-prefix']);
else warn('no control prefix supplied; annual control ratios and control-tail health were not evaluated');

const report = {
  status: failures.length ? 'FAIL' : 'PASS',
  candidate,
  repeat,
  annualControlComparison,
  failures,
  warnings
};
const rendered = JSON.stringify(report, null, 2);
if (options.json) fs.writeFileSync(options.json, `${rendered}\n`);
console.log(rendered);
if (failures.length) process.exitCode = 1;
