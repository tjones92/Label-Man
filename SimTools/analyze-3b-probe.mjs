import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";

function parseCsvLine(line) {
  const values = [];
  let value = "";
  let quoted = false;
  for (let i = 0; i < line.length; i++) {
    const char = line[i];
    if (quoted) {
      if (char === '"' && line[i + 1] === '"') { value += '"'; i++; }
      else if (char === '"') quoted = false;
      else value += char;
    } else if (char === '"') quoted = true;
    else if (char === ",") { values.push(value); value = ""; }
    else value += char;
  }
  values.push(value);
  return values;
}

function readCsv(file, optional = false) {
  if (!fs.existsSync(file)) {
    if (optional) return [];
    throw new Error(`Missing required CSV: ${file}`);
  }
  const text = fs.readFileSync(file, "utf8").trim();
  if (!text) return [];
  const lines = text.split(/\r?\n/);
  const headers = parseCsvLine(lines.shift());
  return lines.filter(Boolean).map(line => {
    const values = parseCsvLine(line);
    return Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
  });
}

const number = value => Number(value ?? 0);
const mean = values => values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;
function percentile(values, fraction) {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const position = (sorted.length - 1) * fraction;
  const low = Math.floor(position), high = Math.ceil(position);
  return sorted[low] + (sorted[high] - sorted[low]) * (position - low);
}
const median = values => percentile(values, 0.5);
function correlation(pairs) {
  if (pairs.length < 2) return null;
  const mx = mean(pairs.map(pair => pair[0]));
  const my = mean(pairs.map(pair => pair[1]));
  let covariance = 0, vx = 0, vy = 0;
  for (const [x, y] of pairs) {
    const dx = x - mx, dy = y - my;
    covariance += dx * dy;
    vx += dx * dx;
    vy += dy * dy;
  }
  return vx && vy ? covariance / Math.sqrt(vx * vy) : null;
}
function summarize(values) {
  return {
    count: values.length,
    min: values.length ? Math.min(...values) : null,
    median: median(values),
    mean: mean(values),
    p95: percentile(values, 0.95),
    max: values.length ? Math.max(...values) : null
  };
}
function sha256(file) {
  return crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex").toUpperCase();
}

const adultGenres = new Set(["Jazz", "EasyListening", "Folk", "TraditionalPop", "BossaNova", "Country"]);
function genreCohort(record) { return adultGenres.has(record.genre) ? "Adult" : "Youth"; }
function careerBand(record) {
  switch (record.careerState) {
    case "Unsigned": case "NewSigning": return "New/Unsigned";
    case "Rising": return "Rising";
    case "Established": return "Established";
    case "Star": case "Superstar": return "Star/Superstar";
    default: return record.careerState || "Unknown";
  }
}

function metricPair(records, closedById, predicate = () => true) {
  const pearsonPopulation = records.filter(record => record.peak < 999 && predicate(record));
  const completedPopulation = [...closedById.values()].filter(row => row.record && predicate(row.record));
  const medianPopulation = [...closedById.values()].filter(row => {
    const record = row.record;
    return record && row.peak > 0 && row.peak <= 40 && predicate(record);
  });
  return {
    completedN: completedPopulation.length,
    completedPoolShare: closedById.size ? completedPopulation.length / closedById.size : null,
    pearsonN: pearsonPopulation.length,
    pearson: correlation(pearsonPopulation.map(record => [record.quality, 101 - record.peak])),
    closedTop40N: medianPopulation.length,
    closedTop40MedianWeeks: median(medianPopulation.map(row => row.weeks))
  };
}

function peakDistribution(records) {
  const charted = records.filter(record => record.peak < 999);
  const neverCharted = records.filter(record => record.peak === 999);
  const bands = {numberOne: 0, top10: 0, top20: 0, top40: 0, top100: 0};
  for (const record of charted) {
    if (record.peak === 1) bands.numberOne++;
    if (record.peak <= 10) bands.top10++;
    if (record.peak <= 20) bands.top20++;
    if (record.peak <= 40) bands.top40++;
    if (record.peak <= 100) bands.top100++;
  }
  return {
    completedSingles: records.length,
    neverCharted: neverCharted.length,
    neverChartedShare: records.length ? neverCharted.length / records.length : null,
    charted: charted.length,
    chartedPeak: summarize(charted.map(record => record.peak)),
    chartedCumulativeBands: bands
  };
}

function analyzeRun(directory, run) {
  const csv = suffix => path.join(directory, `${run}-${suffix}.csv`);
  const rows = readCsv(csv("records"));
  const lifecycleRows = readCsv(csv("lifecycles"));
  const weeks = readCsv(csv("weeks"));
  const strategies = readCsv(csv("release-strategy"));
  const releaseCapacity = readCsv(csv("release-capacity"));
  const albumComposition = readCsv(csv("album-composition"));
  const trackLinks = readCsv(csv("album-track-links"), true);
  const recordsById = new Map();

  for (const row of rows) {
    const id = row.recordId;
    const week = number(row.week);
    const age = number(row.weeksSinceRelease);
    let record = recordsById.get(id);
    if (!record) {
      record = {
        id,
        quality: number(row.quality),
        genre: row.genre,
        careerState: row.launchCareerState,
        peak: 999,
        firstObservedWeek: week,
        releaseWeek: week - Math.max(1, age) + 1,
        finalTotalUnits: number(row.totalUnitsSold)
      };
      recordsById.set(id, record);
    }
    if (!record.careerState && row.launchCareerState) record.careerState = row.launchCareerState;
    if (number(row.currentPosition) > 0) record.peak = Math.min(record.peak, number(row.currentPosition));
    record.finalTotalUnits = number(row.totalUnitsSold);
  }
  const records = [...recordsById.values()];
  const closedById = new Map(lifecycleRows.map(row => {
    const value = {
      id: row.recordId,
      peak: number(row.peakPosition),
      weeks: number(row.weeksOnChart),
      units: number(row.lifetimeUnitsSold),
      leftCensoredAtRunStart: row.leftCensoredAtRunStart === "true",
      record: recordsById.get(row.recordId)
    };
    return [value.id, value];
  }));
  const completedRecords = [...closedById.values()].filter(row => row.record).map(row => row.record);

  const strategyById = new Map(strategies.map(row => [row.recordId, row]));
  const capacitySuccessfulReleases = releaseCapacity.reduce((sum, row) => sum + number(row.successfulReleases), 0);
  const disabledAllSingleFallback = strategies.length === 0 && capacitySuccessfulReleases > 0;
  const singleStrategies = strategies.filter(row => row.chosenFormat === "Single");
  const reusedSingleIds = new Set(trackLinks.map(row => row.sourceRecordId).filter(Boolean));
  const orphanSingles = singleStrategies.filter(row => !reusedSingleIds.has(row.recordId));
  const albumIds = new Set(albumComposition.map(row => row.recordId));
  const completedSingles = [...closedById.keys()]
    .filter(id => !albumIds.has(id))
    .map(id => recordsById.get(id))
    .filter(Boolean);
  const chartEntries = weeks.reduce((sum, row) => sum + number(row.newEntriesTop100), 0);

  const byGenreCohort = Object.fromEntries(["Adult", "Youth"].map(cohort =>
    [cohort, metricPair(records, closedById, record => genreCohort(record) === cohort)]));
  const careerBands = [...new Set(records.map(careerBand))].sort();
  const byCareerBand = Object.fromEntries(careerBands.map(band =>
    [band, metricPair(records, closedById, record => careerBand(record) === band)]));

  const sortedCompleted = [...closedById.values()].filter(row => row.record).sort((a, b) => a.record.quality - b.record.quality);
  const qualityQuartiles = {};
  for (let quartile = 0; quartile < 4; quartile++) {
    const members = sortedCompleted.filter((_, index) => Math.min(3, Math.floor(index * 4 / sortedCompleted.length)) === quartile);
    const ids = new Set(members.map(row => row.id));
    qualityQuartiles[`Q${quartile + 1}`] = {
      count: members.length,
      completedPoolShare: sortedCompleted.length ? members.length / sortedCompleted.length : null,
      quality: summarize(members.map(row => row.record.quality)),
      meanLifetimeUnits: mean(members.map(row => row.units)),
      ...metricPair(records, closedById, record => ids.has(record.id))
    };
  }

  const liveCharted = records.filter(record => record.peak < 999 && !closedById.has(record.id));
  const completedCharted = records.filter(record => record.peak < 999 && closedById.has(record.id));
  const ageCappedCompleted = completedCharted.filter(record => record.releaseWeek >= 1 && record.releaseWeek <= 26);
  const censoringPearson = {
    liveAsCoded: {n: liveCharted.length + completedCharted.length, value: correlation([...completedCharted, ...liveCharted].map(record => [record.quality, 101 - record.peak]))},
    completedOnlyPeak: {n: completedCharted.length, value: correlation(completedCharted.map(record => [record.quality, 101 - record.peak]))},
    censoringInclusiveLiteralLowerBound: {
      n: completedCharted.length + liveCharted.length,
      value: correlation([
        ...completedCharted.map(record => [record.quality, 101 - record.peak]),
        ...liveCharted.map(record => [record.quality, record.finalTotalUnits])
      ])
    },
    completedOnlyReleasedWeeks1To26: {n: ageCappedCompleted.length, value: correlation(ageCappedCompleted.map(record => [record.quality, 101 - record.peak]))}
  };

  const marketRevenueFile = csv("market-revenue");
  const releaseCapacityFile = csv("release-capacity");
  const pipelineGuardPopulation = [...closedById.values()].filter(row => row.record && !row.leftCensoredAtRunStart);
  return {
    run,
    reference: {
      annualMarketUnits: weeks.reduce((sum, row) => sum + number(row.totalMarketUnits), 0),
      marketRevenueSha256: sha256(marketRevenueFile),
      releaseCapacitySha256: sha256(releaseCapacityFile),
      incompatiblePipelineAuditPearson: correlation(pipelineGuardPopulation.map(row => [row.record.quality, row.units])),
      incompatiblePipelineAuditPearsonN: pipelineGuardPopulation.length,
      ...metricPair(records, closedById)
    },
    populationRules: {
      pearson: "Every distinct record in records.csv that ever has currentPosition > 0; includes right-censored/live records; quality is the first observed quality; outcome is 101 - best observed position.",
      medianLife: "Only lifecycle rows whose terminal peakPosition is 1..40; right-censored/live records are excluded; outcome is terminal weeksOnChart."
    },
    cohorts: {byGenreCohort, byCareerBand, completedQualityQuartiles: qualityQuartiles},
    crowding: {
      successfulReleases: capacitySuccessfulReleases,
      successfulSingles: disabledAllSingleFallback ? capacitySuccessfulReleases : singleStrategies.length,
      reusedSingles: disabledAllSingleFallback ? 0 : singleStrategies.length - orphanSingles.length,
      orphanSingles: disabledAllSingleFallback ? capacitySuccessfulReleases : orphanSingles.length,
      chartEntries,
      allSinglesCompetitionRatio: chartEntries ? (disabledAllSingleFallback ? capacitySuccessfulReleases : singleStrategies.length) / chartEntries : null,
      orphanSinglesCompetitionRatio: chartEntries ? (disabledAllSingleFallback ? capacitySuccessfulReleases : orphanSingles.length) / chartEntries : null,
      completedSinglePeakDistribution: peakDistribution(completedSingles),
      closedTop40MedianWeeks: metricPair(records, closedById).closedTop40MedianWeeks
    },
    censoringPearson
  };
}

const directory = process.argv[2] ?? "SimLogs";
const runs = process.argv.slice(3);
if (!runs.length) throw new Error("Usage: node SimTools/analyze-3b-probe.mjs <directory> <run> [run ...]");
const result = runs.map(run => analyzeRun(directory, run));
const output = path.join(directory, "phase3b-probe-analysis.json");
fs.writeFileSync(output, JSON.stringify(result, null, 2));
console.log(JSON.stringify({output, runs: result.length}, null, 2));
