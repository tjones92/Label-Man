#!/usr/bin/env node
// Streaming validator and cohort analyzer for immutable Album settlement telemetry.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const args = process.argv.slice(2);
const validationOnly = args.includes('--telemetry-validation');
const positional = args.filter(arg => !arg.startsWith('--'));
const candidate = positional[0];
const control = positional[1];
if (!candidate || (!validationOnly && !control)) {
  console.error('Usage: analyze-m5-album-catalog-cohorts.mjs <candidate-prefix> [control-prefix] [--telemetry-validation]');
  process.exit(2);
}

const root = process.cwd();
const logs = path.join(root, 'SimLogs');
const cohorts = ['NEW', 'MID', 'CATALOG'];
const targetYears = validationOnly ? null : new Set(['1967', '1968']);
const inputs = new Set();
const failures = [];
const warnings = [];
const fail = message => {
  if (failures.length < 1000) failures.push(message);
};
const warn = message => {
  if (warnings.length < 1000) warnings.push(message);
};
const file = (prefix, suffix) => path.join(logs, `${prefix}-${suffix}.csv`);
const n = value => value === '' || value == null ? 0 : Number(value);
const finite = value => Number.isFinite(value);
const bool = value => value === 'true';
const cohortForAge = age => age <= 25 ? 'NEW' : age <= 51 ? 'MID' : 'CATALOG';
const entryKey = row => `${row.settlementId}|${row.recordId}`;
const regionKey = row => `${row.settlementId}|${row.recordId}|${row.regionId}`;
const clearingKey = row => `${row.week}|${row.regionId}`;
const close = (actual, expected, absolute = 1e-5, relative = 1e-6) =>
  Math.abs(actual - expected) <= Math.max(absolute, Math.abs(expected) * relative);
const roundGodotNonNegative = value => Math.floor(value + 0.5);
const ratio = (a, b) => b === 0 ? null : a / b;
const pct = value => value == null ? 'n/a' : `${(value * 100).toFixed(2)}%`;
const fixed = (value, places = 6) => value == null ? 'n/a' : Number(value).toFixed(places);

async function* csvRows(filename) {
  if (!fs.existsSync(filename)) throw new Error(`Missing input: ${filename}`);
  inputs.add(filename);
  const stream = fs.createReadStream(filename, {encoding: 'utf8'});
  let row = [], field = '', quoted = false, header = null, pendingCR = false;
  for await (const chunk of stream) {
    for (let i = 0; i < chunk.length; i++) {
      const ch = chunk[i];
      if (pendingCR) {
        pendingCR = false;
        if (ch === '\n') continue;
      }
      if (quoted) {
        if (ch === '"') {
          if (chunk[i + 1] === '"') {
            field += '"';
            i++;
          } else {
            quoted = false;
          }
        } else {
          field += ch;
        }
        continue;
      }
      if (ch === '"' && field.length === 0) {
        quoted = true;
      } else if (ch === ',') {
        row.push(field);
        field = '';
      } else if (ch === '\n' || ch === '\r') {
        row.push(field);
        field = '';
        if (ch === '\r') pendingCR = true;
        if (header == null) {
          header = row;
        } else if (row.some(value => value !== '')) {
          const result = {};
          for (let j = 0; j < header.length; j++) result[header[j]] = row[j] ?? '';
          yield result;
        }
        row = [];
      } else {
        field += ch;
      }
    }
  }
  if (quoted) throw new Error(`Unclosed CSV quote: ${filename}`);
  if (field.length || row.length) {
    row.push(field);
    if (header == null) {
      header = row;
    } else {
      const result = {};
      for (let j = 0; j < header.length; j++) result[header[j]] = row[j] ?? '';
      yield result;
    }
  }
}

async function hashInput(filename) {
  const hash = crypto.createHash('sha256');
  for await (const chunk of fs.createReadStream(filename)) hash.update(chunk);
  return {
    file: path.basename(filename),
    bytes: fs.statSync(filename).size,
    sha256: hash.digest('hex')
  };
}

const metricNames = [
  'buyerPool',
  'penetration',
  'exhaustion',
  'catalogDecayMultiplier',
  'effectiveAwareness',
  'conversionBeforeCannibalization',
  'cannibalizationSuppression',
  'rawDemandBeforeCannibalization',
  'rawDemandAfterCannibalization',
  'weeksSinceLastCharted',
  'weeksSinceSalesAboveRetirementFloor'
];

function makeAggregate(year, cohort) {
  return {
    year: Number(year),
    cohort,
    titles: new Set(),
    recordWeeks: 0,
    recordRegionWeeks: 0,
    units: 0,
    gross: 0,
    manufacturingCost: 0,
    artistRoyalty: 0,
    distributionSkim: 0,
    labelNet: 0,
    distributionIncome: 0,
    marketNet: 0,
    rawIntentExact: 0,
    rawIntentRounded: 0,
    serviceableIntent: 0,
    localCleared: 0,
    spilloverCleared: 0,
    finalCleared: 0,
    physicalBackorders: 0,
    marketDisplacedDemand: 0,
    inventoryMovement: 0,
    retirementEligibleRecordWeeks: 0,
    chartedRecordWeeks: 0,
    recentSalesFloorRecordWeeks: 0,
    metrics: Object.fromEntries(metricNames.map(name => [name, []]))
  };
}

function quantiles(values) {
  if (!values.length) return null;
  values.sort((a, b) => a - b);
  const at = p => values[Math.min(values.length - 1, Math.max(0, Math.ceil(p * values.length) - 1))];
  let sum = 0;
  for (const value of values) sum += value;
  return {
    count: values.length,
    min: values[0],
    p10: at(.10),
    p25: at(.25),
    median: at(.50),
    p75: at(.75),
    p90: at(.90),
    p95: at(.95),
    p99: at(.99),
    mean: sum / values.length,
    max: values[values.length - 1]
  };
}

const weeks = new Map();
let weekRows = 0;
let maxWeek = 0;
for await (const row of csvRows(file(candidate, 'weeks'))) {
  weeks.set(row.week, row.year);
  weekRows++;
  maxWeek = Math.max(maxWeek, n(row.week));
}
const years = targetYears ?? new Set([...weeks.values()]);
if (!validationOnly && (weekRows !== 469 || maxWeek !== 469 || weeks.has('470'))) {
  fail(`Final analysis requires exactly 469 completed ticks ending at week 469; observed rows=${weekRows}, maxWeek=${maxWeek}.`);
}

const aggregates = new Map();
for (const year of years) {
  for (const cohort of cohorts) aggregates.set(`${year}|${cohort}`, makeAggregate(year, cohort));
}
const aggregateFor = (year, cohort) => aggregates.get(`${year}|${cohort}`);

const entries = new Map();
const annualSettlement = new Map();
for await (const row of csvRows(file(candidate, 'completed-week-settlement'))) {
  if (row.format !== 'Album' || !years.has(row.year)) continue;
  const key = entryKey(row);
  if (entries.has(key)) {
    fail(`Duplicate Album settlement entry ${key}.`);
    continue;
  }
  const entry = {
    key,
    week: n(row.week),
    year: row.year,
    settlementId: n(row.settlementId),
    recordId: row.recordId,
    labelId: row.labelId,
    labelTier: row.labelTier,
    genre: row.genre,
    units: n(row.totalUnits),
    regionalUnits: n(row.regionalUnits),
    gross: n(row.gross),
    manufacturingCost: n(row.manufacturingCost),
    artistRoyalty: n(row.artistRoyalty),
    distributionSkim: n(row.distributionSkim),
    labelNet: n(row.labelNet),
    distributionIncome: n(row.distributionIncome),
    marketNet: n(row.marketNet),
    retiredAfterSettlement: bool(row.retiredAfterSettlement),
    bookedCount: n(row.bookedCount),
    auditedCount: n(row.auditedCount),
    regionCount: 0,
    regionUnits: 0,
    snapshot: null
  };
  if (entry.bookedCount !== 1 || entry.auditedCount !== 1) {
    fail(`Settlement ${key} booked/audited ${entry.bookedCount}/${entry.auditedCount}; expected 1/1.`);
  }
  entries.set(key, entry);
  const annual = annualSettlement.get(row.year) ?? {
    units: 0, gross: 0, labelNet: 0, distributionIncome: 0, marketNet: 0
  };
  annual.units += entry.units;
  annual.gross += entry.gross;
  annual.labelNet += entry.labelNet;
  annual.distributionIncome += entry.distributionIncome;
  annual.marketNet += entry.marketNet;
  annualSettlement.set(row.year, annual);
}
if (!entries.size) fail('No Album settlement entries exist in the requested analysis years.');

async function* albumRegionalRows() {
  for await (const row of csvRows(file(candidate, 'completed-week-settlement-regional'))) {
    if (!years.has(row.year)) continue;
    const entry = entries.get(entryKey(row));
    if (entry) yield {row, entry};
  }
}

async function* diagnosticRows() {
  for await (const row of csvRows(file(candidate, 'album-catalog-settlement-diagnostic'))) {
    if (years.has(row.year)) yield row;
  }
}

const clearingAggregates = new Map();
const regionalIterator = albumRegionalRows()[Symbol.asyncIterator]();
const diagnosticIterator = diagnosticRows()[Symbol.asyncIterator]();
let previousDiagnosticKey = null;
let diagnosticRowsSeen = 0;
let regionalRowsSeen = 0;
let causalIdentityViolations = 0;

while (true) {
  const [regionalNext, diagnosticNext] = await Promise.all([
    regionalIterator.next(),
    diagnosticIterator.next()
  ]);
  if (regionalNext.done || diagnosticNext.done) {
    if (regionalNext.done !== diagnosticNext.done) {
      fail(`Album diagnostic/regional row-count mismatch after ${diagnosticRowsSeen} paired rows.`);
    }
    break;
  }

  const {row: regional, entry} = regionalNext.value;
  const diagnostic = diagnosticNext.value;
  regionalRowsSeen++;
  diagnosticRowsSeen++;
  const expectedKey = regionKey(regional);
  const actualKey = regionKey(diagnostic);
  if (actualKey === previousDiagnosticKey) fail(`Duplicate Album diagnostic key ${actualKey}.`);
  previousDiagnosticKey = actualKey;
  if (expectedKey !== actualKey) {
    fail(`Album diagnostic key mismatch: regional=${expectedKey}, diagnostic=${actualKey}.`);
    continue;
  }

  const exact = n(diagnostic.rawIntentExact);
  const rounded = n(diagnostic.rawIntentRounded);
  const numericFields = [
    'rawIntentExact', 'buyerPool', 'penetration', 'exhaustion',
    'catalogDecayMultiplier', 'effectiveAwareness', 'conversionBeforeCannibalization',
    'cannibalizationSuppression', 'rawDemandBeforeCannibalization',
    'rawDemandAfterCannibalization'
  ];
  for (const field of numericFields) {
    if (!finite(n(diagnostic[field]))) fail(`Non-finite ${field} at ${actualKey}.`);
  }
  if (rounded !== roundGodotNonNegative(exact)) {
    fail(`rawIntentRounded identity failed at ${actualKey}: exact=${exact}, rounded=${rounded}.`);
  }

  const integerPairs = [
    ['rawIntent', 'rawIntentRounded'],
    ['serviceableIntent', 'serviceableIntent'],
    ['localCleared', 'localCleared'],
    ['spilloverCleared', 'spilloverCleared'],
    ['finalCleared', 'finalCleared'],
    ['physicalBackorders', 'physicalBackorders'],
    ['marketDisplacedDemand', 'marketDisplacedDemand'],
    ['inventoryMovement', 'inventoryMovement']
  ];
  for (const [regionalField, diagnosticField] of integerPairs) {
    if (n(regional[regionalField]) !== n(diagnostic[diagnosticField])) {
      fail(`${regionalField}/${diagnosticField} mismatch at ${actualKey}.`);
    }
  }

  const buyerPool = n(diagnostic.buyerPool);
  const cumulative = n(diagnostic.regionalCumulativeUnitsBeforeSale);
  const penetration = n(diagnostic.penetration);
  const exhaustion = n(diagnostic.exhaustion);
  const awareness = n(diagnostic.effectiveAwareness);
  const conversion = n(diagnostic.conversionBeforeCannibalization);
  const suppression = n(diagnostic.cannibalizationSuppression);
  const rawBefore = n(diagnostic.rawDemandBeforeCannibalization);
  const rawAfter = n(diagnostic.rawDemandAfterCannibalization);
  const expectedPenetration = cumulative / Math.max(1, buyerPool);
  const expectedExhaustion = Math.max(.15, 1 / (1 + penetration * 4));
  const expectedBefore = buyerPool * awareness * conversion;
  const expectedAfter = rawBefore * (1 - suppression);
  if (!close(penetration, expectedPenetration, 1e-6, 2e-5) ||
      !close(exhaustion, expectedExhaustion, 1e-6, 2e-5) ||
      !close(rawBefore, expectedBefore, .02, 2e-5) ||
      !close(rawAfter, expectedAfter, .02, 2e-5)) {
    causalIdentityViolations++;
    fail(`Causal arithmetic identity failed at ${actualKey}.`);
  }

  const snapshot = {
    labelId: diagnostic.labelId,
    labelTier: diagnostic.labelTier,
    genre: diagnostic.genre,
    weeksSinceRelease: n(diagnostic.weeksSinceRelease),
    weeksOnChart: n(diagnostic.weeksOnChart),
    currentPosition: n(diagnostic.currentPosition),
    lastChartedAge: n(diagnostic.lastChartedAge),
    lastSalesAboveRetirementFloorAge: n(diagnostic.lastSalesAboveRetirementFloorAge),
    weeksSinceLastCharted: n(diagnostic.weeksSinceLastCharted),
    weeksSinceSalesAboveRetirementFloor: n(diagnostic.weeksSinceSalesAboveRetirementFloor),
    retirementEligibleAfterSettlement: bool(diagnostic.retirementEligibleAfterSettlement)
  };
  if (snapshot.weeksSinceRelease < 0) fail(`Negative Album age at ${actualKey}.`);
  if (diagnostic.labelId !== entry.labelId || diagnostic.labelTier !== entry.labelTier ||
      diagnostic.genre !== entry.genre) {
    fail(`Diagnostic entry identity differs from settlement at ${actualKey}.`);
  }
  if (snapshot.retirementEligibleAfterSettlement !== entry.retiredAfterSettlement) {
    fail(`Retirement eligibility differs from settlement at ${actualKey}.`);
  }
  if (entry.snapshot == null) {
    entry.snapshot = snapshot;
    const cohort = cohortForAge(snapshot.weeksSinceRelease);
    const aggregate = aggregateFor(entry.year, cohort);
    aggregate.titles.add(entry.recordId);
    aggregate.recordWeeks++;
    aggregate.units += entry.units;
    aggregate.gross += entry.gross;
    aggregate.manufacturingCost += entry.manufacturingCost;
    aggregate.artistRoyalty += entry.artistRoyalty;
    aggregate.distributionSkim += entry.distributionSkim;
    aggregate.labelNet += entry.labelNet;
    aggregate.distributionIncome += entry.distributionIncome;
    aggregate.marketNet += entry.marketNet;
    if (snapshot.retirementEligibleAfterSettlement) aggregate.retirementEligibleRecordWeeks++;
    if (snapshot.currentPosition > 0) aggregate.chartedRecordWeeks++;
    if (snapshot.weeksSinceSalesAboveRetirementFloor < 52) aggregate.recentSalesFloorRecordWeeks++;
  } else {
    for (const [field, value] of Object.entries(snapshot)) {
      if (entry.snapshot[field] !== value) fail(`Entry-level ${field} differs across regions at ${actualKey}.`);
    }
  }

  const cohort = cohortForAge(snapshot.weeksSinceRelease);
  const aggregate = aggregateFor(entry.year, cohort);
  aggregate.recordRegionWeeks++;
  aggregate.rawIntentExact += exact;
  aggregate.rawIntentRounded += rounded;
  aggregate.serviceableIntent += n(diagnostic.serviceableIntent);
  aggregate.localCleared += n(diagnostic.localCleared);
  aggregate.spilloverCleared += n(diagnostic.spilloverCleared);
  aggregate.finalCleared += n(diagnostic.finalCleared);
  aggregate.physicalBackorders += n(diagnostic.physicalBackorders);
  aggregate.marketDisplacedDemand += n(diagnostic.marketDisplacedDemand);
  aggregate.inventoryMovement += n(diagnostic.inventoryMovement);
  for (const metric of metricNames) {
    const value = metric === 'weeksSinceLastCharted' || metric === 'weeksSinceSalesAboveRetirementFloor'
      ? snapshot[metric]
      : n(diagnostic[metric]);
    aggregate.metrics[metric].push(value);
  }

  entry.regionCount++;
  entry.regionUnits += n(diagnostic.finalCleared);
  const byRegion = clearingAggregates.get(clearingKey(diagnostic)) ?? {
    rawExact: 0, serviceable: 0, cleared: 0
  };
  byRegion.rawExact += exact;
  byRegion.serviceable += n(diagnostic.serviceableIntent);
  byRegion.cleared += n(diagnostic.finalCleared);
  clearingAggregates.set(clearingKey(diagnostic), byRegion);
}

for (const entry of entries.values()) {
  if (entry.regionCount === 0) fail(`Album settlement ${entry.key} has no diagnostic regions.`);
  if (entry.regionUnits !== entry.units || entry.regionUnits !== entry.regionalUnits) {
    fail(`Album settlement ${entry.key} regional units ${entry.regionUnits} != entry ${entry.units}/${entry.regionalUnits}.`);
  }
}

const clearingValidation = {
  rows: 0,
  rawExactDeltaSum: 0,
  rawExactMaximumAbsoluteDelta: 0,
  rawExactMaximumAllowedDelta: 0,
  serviceableDelta: 0,
  clearedDelta: 0,
  unmatchedDiagnosticGroups: 0
};
const matchedClearingKeys = new Set();
for await (const row of csvRows(file(candidate, 'market-clearing-weekly'))) {
  if (!years.has(row.year)) continue;
  const key = clearingKey(row);
  const actual = clearingAggregates.get(key) ?? {rawExact: 0, serviceable: 0, cleared: 0};
  matchedClearingKeys.add(key);
  const marketRaw = n(row.rawAlbumDemand);
  const delta = Math.abs(actual.rawExact - marketRaw);
  const allowed = Math.max(.25, Math.abs(marketRaw) * 5e-6);
  clearingValidation.rows++;
  clearingValidation.rawExactDeltaSum += delta;
  clearingValidation.rawExactMaximumAbsoluteDelta =
    Math.max(clearingValidation.rawExactMaximumAbsoluteDelta, delta);
  clearingValidation.rawExactMaximumAllowedDelta =
    Math.max(clearingValidation.rawExactMaximumAllowedDelta, allowed);
  clearingValidation.serviceableDelta += Math.abs(actual.serviceable - n(row.serviceableAlbumIntent));
  clearingValidation.clearedDelta += Math.abs(actual.cleared - n(row.clearedAlbumUnits));
  if (delta > allowed) fail(`Exact raw Album intent exceeds 5 ppm float tolerance at ${key}: delta=${delta}, allowed=${allowed}.`);
}
for (const key of clearingAggregates.keys()) {
  if (!matchedClearingKeys.has(key)) clearingValidation.unmatchedDiagnosticGroups++;
}
if (clearingValidation.serviceableDelta !== 0 || clearingValidation.clearedDelta !== 0 ||
    clearingValidation.unmatchedDiagnosticGroups !== 0) {
  fail(`Clearing reconciliation failed: serviceableDelta=${clearingValidation.serviceableDelta}, clearedDelta=${clearingValidation.clearedDelta}, unmatchedGroups=${clearingValidation.unmatchedDiagnosticGroups}.`);
}

async function readAnnualAlbum(prefix) {
  const result = new Map();
  for await (const row of csvRows(file(prefix, 'market-revenue'))) {
    if (row.period.toLowerCase() !== 'annual' || row.labelTier !== 'All' ||
        row.releaseFormat !== 'Album' || !years.has(row.year)) continue;
    result.set(row.year, {
      units: n(row.totalMarketUnits),
      gross: n(row.gross),
      labelNet: n(row.labelNet),
      distributionIncome: n(row.distributionIncome),
      marketNet: n(row.marketNet)
    });
  }
  return result;
}

const candidateAnnual = await readAnnualAlbum(candidate);
const controlAnnual = control ? await readAnnualAlbum(control) : new Map();
const annualReconciliation = {};
for (const year of years) {
  const settlement = annualSettlement.get(year);
  const annual = candidateAnnual.get(year);
  if (!settlement || !annual) {
    fail(`Missing settlement or annual Album economics for ${year}.`);
    continue;
  }
  const delta = {
    units: settlement.units - annual.units,
    gross: settlement.gross - annual.gross,
    labelNet: settlement.labelNet - annual.labelNet,
    distributionIncome: settlement.distributionIncome - annual.distributionIncome,
    marketNet: settlement.marketNet - annual.marketNet
  };
  annualReconciliation[year] = delta;
  if (delta.units !== 0 || !close(delta.gross, 0, .05, 0) ||
      !close(delta.labelNet, 0, .05, 0) ||
      !close(delta.distributionIncome, 0, .05, 0) ||
      !close(delta.marketNet, 0, .05, 0)) {
    fail(`Annual Album settlement reconciliation failed for ${year}: ${JSON.stringify(delta)}.`);
  }
}

async function readScheduledAlbums(prefix) {
  const result = Object.fromEntries([...years].map(year => [year, 0]));
  if (!prefix) return result;
  const prefixWeeks = new Map();
  for await (const row of csvRows(file(prefix, 'weeks'))) prefixWeeks.set(row.week, row.year);
  for await (const row of csvRows(file(prefix, 'album-projects'))) {
    const year = prefixWeeks.get(row.scheduledWeek);
    if (year != null && Object.hasOwn(result, year)) result[year]++;
  }
  return result;
}

const [candidateScheduledAlbums, controlScheduledAlbums] = await Promise.all([
  readScheduledAlbums(candidate),
  readScheduledAlbums(control)
]);

const cohortRows = [...aggregates.values()]
  .sort((a, b) => a.year - b.year || cohorts.indexOf(a.cohort) - cohorts.indexOf(b.cohort))
  .map(aggregate => {
    const distributions = Object.fromEntries(
      metricNames.map(metric => [metric, quantiles(aggregate.metrics[metric])])
    );
    return {
      year: aggregate.year,
      cohort: aggregate.cohort,
      titleCount: aggregate.titles.size,
      recordWeeks: aggregate.recordWeeks,
      recordRegionWeeks: aggregate.recordRegionWeeks,
      units: aggregate.units,
      gross: aggregate.gross,
      manufacturingCost: aggregate.manufacturingCost,
      artistRoyalty: aggregate.artistRoyalty,
      distributionSkim: aggregate.distributionSkim,
      labelNet: aggregate.labelNet,
      distributionIncome: aggregate.distributionIncome,
      marketNet: aggregate.marketNet,
      rawIntentExact: aggregate.rawIntentExact,
      rawIntentRounded: aggregate.rawIntentRounded,
      serviceableIntent: aggregate.serviceableIntent,
      localCleared: aggregate.localCleared,
      spilloverCleared: aggregate.spilloverCleared,
      finalCleared: aggregate.finalCleared,
      physicalBackorders: aggregate.physicalBackorders,
      marketDisplacedDemand: aggregate.marketDisplacedDemand,
      inventoryMovement: aggregate.inventoryMovement,
      retirementEligibleRecordWeeks: aggregate.retirementEligibleRecordWeeks,
      retirementEligibilityRate: ratio(aggregate.retirementEligibleRecordWeeks, aggregate.recordWeeks),
      chartedRecordWeeks: aggregate.chartedRecordWeeks,
      recentSalesFloorRecordWeeks: aggregate.recentSalesFloorRecordWeeks,
      recentSalesFloorRate: ratio(aggregate.recentSalesFloorRecordWeeks, aggregate.recordWeeks),
      serviceableOverRaw: ratio(aggregate.serviceableIntent, aggregate.rawIntentExact),
      clearedOverServiceable: ratio(aggregate.finalCleared, aggregate.serviceableIntent),
      distributions
    };
  });

for (const year of years) {
  const rows = cohortRows.filter(row => row.year === Number(year));
  const totals = rows.reduce((sum, row) => {
    for (const field of ['rawIntentExact', 'serviceableIntent', 'finalCleared', 'units', 'gross', 'labelNet', 'marketNet']) {
      sum[field] += row[field];
    }
    return sum;
  }, {rawIntentExact: 0, serviceableIntent: 0, finalCleared: 0, units: 0, gross: 0, labelNet: 0, marketNet: 0});
  for (const row of rows) {
    row.shares = Object.fromEntries(
      Object.keys(totals).map(field => [field, ratio(row[field], totals[field])])
    );
  }
}

let answers = [];
let supportedCorrectionSurface = 'NOT_ADJUDICABLE';
let classification = validationOnly
  ? failures.length ? 'TELEMETRY_VALIDATION_FAIL' : 'TELEMETRY_VALIDATION_PASS'
  : 'EXISTING_DATA_INSUFFICIENT';
if (!validationOnly) {
  const by = (year, cohort) => cohortRows.find(row => row.year === year && row.cohort === cohort);
  const new67 = by(1967, 'NEW'), new68 = by(1968, 'NEW');
  const mid67 = by(1967, 'MID'), mid68 = by(1968, 'MID');
  const catalog67 = by(1967, 'CATALOG'), catalog68 = by(1968, 'CATALOG');
  const album67 = candidateAnnual.get('1967'), album68 = candidateAnnual.get('1968');
  if (![new67, new68, mid67, mid68, catalog67, catalog68, album67, album68].every(Boolean)) {
    fail('Final cohort questions require complete 1967 and 1968 cohort and annual rows.');
  } else {
    const unitIncrease = album68.units - album67.units;
    const grossIncrease = album68.gross - album67.gross;
    const catalogUnitIncrease = catalog68.units - catalog67.units;
    const catalogGrossIncrease = catalog68.gross - catalog67.gross;
    const catalogRawGrowth = ratio(catalog68.rawIntentExact, catalog67.rawIntentExact);
    const newRawGrowth = ratio(new68.rawIntentExact, new67.rawIntentExact);
    const catalogRawShareChange = catalog68.shares.rawIntentExact - catalog67.shares.rawIntentExact;
    const catalogServiceableShareChange = catalog68.shares.serviceableIntent - catalog67.shares.serviceableIntent;
    const catalogClearedShareChange = catalog68.shares.finalCleared - catalog67.shares.finalCleared;
    const buyerPoolGrowth = ratio(
      catalog68.distributions.buyerPool?.mean,
      catalog67.distributions.buyerPool?.mean
    );
    const penetrationChange = catalog68.distributions.penetration?.mean -
      catalog67.distributions.penetration?.mean;
    const exhaustionChange = catalog68.distributions.exhaustion?.mean -
      catalog67.distributions.exhaustion?.mean;
    const catalogMajorityUnits = catalogUnitIncrease > unitIncrease / 2;
    const catalogMajorityGross = catalogGrossIncrease > grossIncrease / 2;
    const preClearingExcess = catalogRawGrowth != null && newRawGrowth != null &&
      catalogRawGrowth > newRawGrowth && catalogRawShareChange > 0;
    const headroomMechanism = buyerPoolGrowth != null && buyerPoolGrowth > 1.02 &&
      penetrationChange < 0 && exhaustionChange > 0;
    const retirementResetMechanism = catalog68.recentSalesFloorRate > .50 &&
      catalog68.retirementEligibilityRate < .10;
    const decayWeak = catalog68.distributions.catalogDecayMultiplier?.median > .75;
    const conversionGrowth = catalog68.distributions.conversionBeforeCannibalization?.mean >
      catalog67.distributions.conversionBeforeCannibalization?.mean * 1.02;
    const awarenessGrowth = catalog68.distributions.effectiveAwareness?.mean >
      catalog67.distributions.effectiveAwareness?.mean * 1.02;
    const weakerCannibalization = catalog68.distributions.cannibalizationSuppression?.mean <
      catalog67.distributions.cannibalizationSuppression?.mean - .01;
    const clearingEffect = catalogClearedShareChange - catalogRawShareChange;
    const serviceabilityEffect = catalogServiceableShareChange - catalogRawShareChange;

    answers = [
      {
        question: 'Did the 52+ cohort account for a majority of the candidate 1967-to-1968 Album-unit increase?',
        answer: catalogMajorityUnits ? 'YES' : 'NO',
        evidence: {catalogUnitIncrease, totalAlbumUnitIncrease: unitIncrease, contribution: ratio(catalogUnitIncrease, unitIncrease)}
      },
      {
        question: 'Did the 52+ cohort account for a majority of the candidate 1967-to-1968 Album-gross increase?',
        answer: catalogMajorityGross ? 'YES' : 'NO',
        evidence: {catalogGrossIncrease, totalAlbumGrossIncrease: grossIncrease, contribution: ratio(catalogGrossIncrease, grossIncrease)}
      },
      {
        question: 'Was the 52+ excess present in exact raw demand before inventory and common clearing?',
        answer: preClearingExcess ? 'YES' : 'NO',
        evidence: {catalogRawGrowth, newRawGrowth, catalogRawShareChange}
      },
      {
        question: 'Did serviceability or common clearing materially amplify the 52+ excess?',
        answer: Math.max(serviceabilityEffect, clearingEffect) > .01 ? 'YES' : 'NO',
        evidence: {
          rawShareChange: catalogRawShareChange,
          serviceableShareChange: catalogServiceableShareChange,
          clearedShareChange: catalogClearedShareChange,
          finding: clearingEffect > .01 ? 'AMPLIFIED' : clearingEffect < -.01 ? 'SUPPRESSED' : 'NEUTRAL'
        }
      },
      {
        question: 'Did expanding buyer pool reduce penetration and increase exhaustion headroom for 52+ Albums?',
        answer: headroomMechanism ? 'YES' : 'NO',
        evidence: {buyerPoolGrowth, penetrationChange, exhaustionChange}
      },
      {
        question: 'Did weak decay, awareness, conversion, or cannibalization materially mediate the 52+ excess?',
        answer: decayWeak || conversionGrowth || awarenessGrowth || weakerCannibalization ? 'YES' : 'NO',
        evidence: {
          weakCatalogDecay: decayWeak,
          awarenessGrowth,
          conversionGrowth,
          weakerCannibalization,
          catalog1967: {
            decayMedian: catalog67.distributions.catalogDecayMultiplier?.median,
            awarenessMean: catalog67.distributions.effectiveAwareness?.mean,
            conversionMean: catalog67.distributions.conversionBeforeCannibalization?.mean,
            suppressionMean: catalog67.distributions.cannibalizationSuppression?.mean
          },
          catalog1968: {
            decayMedian: catalog68.distributions.catalogDecayMultiplier?.median,
            awarenessMean: catalog68.distributions.effectiveAwareness?.mean,
            conversionMean: catalog68.distributions.conversionBeforeCannibalization?.mean,
            suppressionMean: catalog68.distributions.cannibalizationSuppression?.mean
          }
        }
      },
      {
        question: 'Did repeated sales-floor resets materially prevent retirement of 52+ Albums?',
        answer: retirementResetMechanism ? 'YES' : 'NO',
        evidence: {
          recentSalesFloorRate1968: catalog68.recentSalesFloorRate,
          retirementEligibilityRate1968: catalog68.retirementEligibilityRate,
          weeksSinceSalesFloorMedian1968: catalog68.distributions.weeksSinceSalesAboveRetirementFloor?.median
        }
      },
      {
        question: 'Can ordinary current-year Album scheduling explain the 52+ excess?',
        answer: catalogMajorityUnits && preClearingExcess ? 'NO' : 'NOT_ADJUDICABLE',
        evidence: {
          candidateScheduledAlbums1967: candidateScheduledAlbums['1967'],
          candidateScheduledAlbums1968: candidateScheduledAlbums['1968'],
          controlScheduledAlbums1967: controlScheduledAlbums['1967'],
          controlScheduledAlbums1968: controlScheduledAlbums['1968'],
          newUnitIncrease: new68.units - new67.units,
          catalogUnitIncrease
        }
      }
    ];

    if (!catalogMajorityUnits && !catalogMajorityGross && !preClearingExcess) {
      supportedCorrectionSurface = 'NO_CATALOG_CORRECTION_SUPPORTED';
    } else if (headroomMechanism) {
      supportedCorrectionSurface = 'ALBUM_BUYER_POOL_PENETRATION_EXHAUSTION';
    } else if (retirementResetMechanism) {
      supportedCorrectionSurface = 'ALBUM_CATALOG_RETIREMENT_RELEVANCE';
    } else if (decayWeak) {
      supportedCorrectionSurface = 'ALBUM_CATALOG_DECAY';
    } else if (awarenessGrowth || conversionGrowth || weakerCannibalization) {
      supportedCorrectionSurface = 'ALBUM_CATALOG_DEMAND_COMPOSITION';
    } else if (preClearingExcess) {
      supportedCorrectionSurface = 'CATALOG_RAW_DEMAND_MECHANISM_UNRESOLVED';
    } else {
      supportedCorrectionSurface = 'COMMON_CLEARING_OR_SERVICEABILITY';
    }
  }

  if (failures.length) {
    classification = 'EXISTING_DATA_INSUFFICIENT';
    answers = answers.map(item => ({
      ...item,
      answer: 'NOT_ADJUDICABLE',
      evidence: {reason: 'Telemetry or economic reconciliation failed; see validation.failures.'}
    }));
    supportedCorrectionSurface = 'NOT_ADJUDICABLE';
  } else if (supportedCorrectionSurface === 'CATALOG_RAW_DEMAND_MECHANISM_UNRESOLVED') {
    classification = 'EXISTING_DATA_CONFIRMS_CATALOG_EXCESS_BUT_NOT_MECHANISM';
  } else {
    classification = 'EXISTING_DATA_SUFFICIENT_FOR_CORRECTION_SURFACE';
  }
}

const outputRows = cohortRows.map(row => ({
  year: row.year,
  cohort: row.cohort,
  titleCount: row.titleCount,
  recordWeeks: row.recordWeeks,
  recordRegionWeeks: row.recordRegionWeeks,
  rawIntentExact: row.rawIntentExact,
  rawShare: row.shares?.rawIntentExact,
  serviceableIntent: row.serviceableIntent,
  serviceableShare: row.shares?.serviceableIntent,
  finalCleared: row.finalCleared,
  clearedShare: row.shares?.finalCleared,
  units: row.units,
  unitShare: row.shares?.units,
  gross: row.gross,
  grossShare: row.shares?.gross,
  labelNet: row.labelNet,
  labelNetShare: row.shares?.labelNet,
  marketNet: row.marketNet,
  marketNetShare: row.shares?.marketNet,
  retirementEligibilityRate: row.retirementEligibilityRate,
  recentSalesFloorRate: row.recentSalesFloorRate,
  buyerPoolMedian: row.distributions.buyerPool?.median,
  penetrationMedian: row.distributions.penetration?.median,
  exhaustionMedian: row.distributions.exhaustion?.median,
  catalogDecayMedian: row.distributions.catalogDecayMultiplier?.median,
  awarenessMedian: row.distributions.effectiveAwareness?.median,
  conversionMedian: row.distributions.conversionBeforeCannibalization?.median,
  cannibalizationMedian: row.distributions.cannibalizationSuppression?.median
}));

const inputHashes = await Promise.all([...inputs].sort().map(hashInput));
const report = {
  title: validationOnly ? 'M5 Album catalog telemetry validation' : 'M5 Album catalog cohort analysis',
  mode: validationOnly ? 'telemetry-validation' : 'final-cohort-analysis',
  candidate,
  control: control ?? null,
  scope: {
    years: [...years].map(Number).sort((a, b) => a - b),
    completedWeekRows: weekRows,
    maximumWeek: maxWeek,
    week470Excluded: !validationOnly
  },
  inputs: inputHashes,
  validation: {
    passed: failures.length === 0,
    failures,
    warnings,
    albumSettlementEntries: entries.size,
    albumRegionalRows: regionalRowsSeen,
    albumDiagnosticRows: diagnosticRowsSeen,
    causalIdentityViolations,
    clearing: clearingValidation,
    annualReconciliation,
    rawExactTolerance: 'Per week-region: max(0.25 units, 5 ppm of market-clearing rawAlbumDemand). Integer serviceable and cleared totals must be exact.'
  },
  scheduledAlbums: {
    candidate: candidateScheduledAlbums,
    control: controlScheduledAlbums
  },
  cohorts: cohortRows,
  answers,
  supportedCorrectionSurface,
  classification
};

function csvEscape(value) {
  const text = String(value ?? '');
  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
}

function writeCsv(filename, rows) {
  const headers = [...new Set(rows.flatMap(row => Object.keys(row)))];
  const lines = [headers.join(',')];
  for (const row of rows) lines.push(headers.map(header => csvEscape(row[header])).join(','));
  fs.writeFileSync(filename, `${lines.join('\n')}\n`);
}

const stem = validationOnly
  ? `${candidate}-album-catalog-telemetry-validation`
  : `${candidate}-album-catalog-cohort-analysis`;
const jsonPath = path.join(logs, `${stem}.json`);
const mdPath = path.join(logs, `${stem}.md`);
const csvPath = path.join(logs, `${stem}.csv`);
writeCsv(csvPath, outputRows);
fs.writeFileSync(jsonPath, `${JSON.stringify(report, null, 2)}\n`);

const tableRows = outputRows.map(row =>
  `| ${row.year} | ${row.cohort} | ${row.titleCount.toLocaleString()} | ${row.recordWeeks.toLocaleString()} | ${row.recordRegionWeeks.toLocaleString()} | ${row.units.toLocaleString()} | ${pct(row.unitShare)} | ${row.gross.toFixed(2)} | ${pct(row.grossShare)} | ${pct(row.rawShare)} | ${pct(row.serviceableShare)} | ${pct(row.clearedShare)} |`
).join('\n');
const findings = validationOnly
  ? 'Final 1967–1968 causal questions are intentionally not adjudicated in telemetry-validation mode.'
  : answers.map((item, index) =>
      `${index + 1}. **${item.answer}** — ${item.question}\n\n   Evidence: \`${JSON.stringify(item.evidence)}\``
    ).join('\n\n');
const finalClassification = validationOnly ? '' : `
## Classification

${classification}
`;
const markdown = `# ${report.title}

${validationOnly ? `**Classification: ${classification}**` : '**Final classification appears at the end of this report.**'}

Telemetry, immutable-key, causal-arithmetic, settlement, clearing, and annual-economic validation: **${failures.length === 0 ? 'PASS' : 'FAIL'}**.

Candidate: \`${candidate}\`  
Control: \`${control ?? 'not required in validation mode'}\`  
Completed ticks: ${weekRows}; maximum week: ${maxWeek}

| Year | Cohort | Titles | Record-weeks | Region-weeks | Units | Unit share | Gross | Gross share | Raw share | Serviceable share | Cleared share |
|---:|:---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
${tableRows}

## Findings

${findings}

## Supported correction surface

\`${supportedCorrectionSurface}\`

## Validation

- Album settlement entries: ${entries.size.toLocaleString()}
- Album regional settlement rows: ${regionalRowsSeen.toLocaleString()}
- Album diagnostic rows: ${diagnosticRowsSeen.toLocaleString()}
- Causal identity violations: ${causalIdentityViolations}
- Exact raw-intent maximum absolute delta: ${fixed(clearingValidation.rawExactMaximumAbsoluteDelta)}
- Integer serviceable delta: ${clearingValidation.serviceableDelta}
- Integer cleared delta: ${clearingValidation.clearedDelta}
- Failures: ${failures.length}
- Warnings: ${warnings.length}

${failures.length ? `### Failures\n\n${failures.map(value => `- ${value}`).join('\n')}\n` : ''}
The companion JSON contains all quantiles (p10, p25, median, p75, p90, p95, p99, mean, minimum, and maximum), reconciliation deltas, input SHA-256 hashes, and question evidence.
${finalClassification}
`;
fs.writeFileSync(mdPath, markdown);

console.log(JSON.stringify({
  mode: report.mode,
  classification,
  passed: failures.length === 0,
  failures: failures.length,
  outputs: [path.basename(csvPath), path.basename(jsonPath), path.basename(mdPath)]
}, null, 2));
if (failures.length) process.exitCode = 1;
