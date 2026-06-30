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
    else if (char === ',') { values.push(value); value = ""; }
    else value += char;
  }
  values.push(value);
  return values;
}

function readCsv(file) {
  const lines = fs.readFileSync(file, "utf8").trim().split(/\r?\n/);
  const headers = parseCsvLine(lines.shift());
  return lines.filter(Boolean).map(line => Object.fromEntries(headers.map((header, i) => [header, parseCsvLine(line)[i]])));
}

const num = value => Number(value ?? 0);
const mean = values => values.length ? values.reduce((a, b) => a + b, 0) / values.length : null;
const median = values => percentile(values, 0.5);
function percentile(values, p) {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const index = (sorted.length - 1) * p;
  const lower = Math.floor(index), upper = Math.ceil(index);
  return sorted[lower] + (sorted[upper] - sorted[lower]) * (index - lower);
}
function correlation(xs, ys) {
  if (xs.length < 2) return null;
  const mx = mean(xs), my = mean(ys);
  let covariance = 0, vx = 0, vy = 0;
  for (let i = 0; i < xs.length; i++) {
    const dx = xs[i] - mx, dy = ys[i] - my;
    covariance += dx * dy; vx += dx * dx; vy += dy * dy;
  }
  return vx && vy ? covariance / Math.sqrt(vx * vy) : null;
}
function rank(values) {
  const sorted = values.map((value, index) => ({value, index})).sort((a, b) => a.value - b.value);
  const ranks = Array(values.length);
  for (let i = 0; i < sorted.length;) {
    let j = i + 1;
    while (j < sorted.length && sorted[j].value === sorted[i].value) j++;
    const averageRank = (i + j - 1) / 2 + 1;
    for (let k = i; k < j; k++) ranks[sorted[k].index] = averageRank;
    i = j;
  }
  return ranks;
}
function summarize(values) {
  return {count: values.length, min: values.length ? Math.min(...values) : null, median: median(values), mean: mean(values), p95: percentile(values, .95), max: values.length ? Math.max(...values) : null};
}

function coefficientOfVariation(values) {
  const average = mean(values);
  if (!values.length || !average) return null;
  const variance = mean(values.map(value => (value - average) ** 2));
  return Math.sqrt(variance) / average;
}

function analyzeRun(directory, run) {
  const weeks = readCsv(path.join(directory, `${run}-weeks.csv`));
  const rows = readCsv(path.join(directory, `${run}-records.csv`));
  const lifecycles = readCsv(path.join(directory, `${run}-lifecycles.csv`));
  const records = new Map();
  const weeklyChart = new Map();
  const saturationChanges = {x: [], y: []};
	const normalizedSaturation = {x: [], y: []};
	const withinRecordSaturationCorrelations = [];

  for (const row of rows) {
    const id = row.recordId;
    const week = num(row.week);
    let record = records.get(id);
    if (!record) {
      record = {id, title: row.title, quality: num(row.quality), tier: row.labelTier, peak: 999, rows: [], firstTotal: num(row.totalUnitsSold), leftCensored: num(row.weeksSinceRelease) > 1};
      records.set(id, record);
    }
    record.rows.push(row);
    if (num(row.currentPosition) > 0) {
      record.peak = Math.min(record.peak, num(row.currentPosition));
      if (!weeklyChart.has(week)) weeklyChart.set(week, []);
      weeklyChart.get(week).push(row);
    }
  }

  const moves = [], upwardMoves = [], downwardMoves = [];
  let hugeRise = 0, hugeFall = 0, transitions = 0;
  for (const record of records.values()) {
    record.rows.sort((a, b) => num(a.week) - num(b.week));
    const final = record.rows.at(-1);
    record.finalTotal = num(final.totalUnitsSold);
    record.finalWeeks = num(final.weeksOnChart);
    record.maxSaturation = Math.max(...record.rows.map(row => num(row.saturation)));
		const recordPeakUnits = Math.max(...record.rows.map(row => num(row.unitsThisWeek)));
		const recordSaturation = [];
		const recordNextUnits = [];
    for (let i = 1; i < record.rows.length; i++) {
      const previous = record.rows[i - 1], current = record.rows[i];
      const p = num(previous.currentPosition), c = num(current.currentPosition);
      if (p > 0 && c > 0) {
        const delta = c - p;
        moves.push(Math.abs(delta)); transitions++;
        if (delta < 0) upwardMoves.push(-delta); else if (delta > 0) downwardMoves.push(delta);
        if (p >= 80 && c <= 5) hugeRise++;
        if (p <= 5 && c >= 80) hugeFall++;
      }
      const priorUnits = num(previous.unitsThisWeek), currentUnits = num(current.unitsThisWeek);
      if (priorUnits > 0) {
        saturationChanges.x.push(num(previous.saturation));
        saturationChanges.y.push((currentUnits - priorUnits) / priorUnits);
      }
			if (recordPeakUnits > 0) {
				const saturation = num(previous.saturation);
				const nextUnitFraction = currentUnits / recordPeakUnits;
				normalizedSaturation.x.push(saturation);
				normalizedSaturation.y.push(nextUnitFraction);
				recordSaturation.push(saturation);
				recordNextUnits.push(nextUnitFraction);
			}
    }
		if (recordSaturation.length >= 4) {
			const value = correlation(recordSaturation, recordNextUnits);
			if (value !== null && Number.isFinite(value)) withinRecordSaturationCorrelations.push(value);
		}
  }

  for (const chartRows of weeklyChart.values()) chartRows.sort((a, b) => num(a.currentPosition) - num(b.currentPosition));
  const numberOnes = weeks.map(row => row.numberOneRecordId);
  const oneStreaks = [];
  for (let i = 0; i < numberOnes.length;) {
    let j = i + 1;
    while (j < numberOnes.length && numberOnes[j] === numberOnes[i]) j++;
    oneStreaks.push(j - i); i = j;
  }
  const numberOneSet = new Set(numberOnes);
  const numberOneRecords = [...numberOneSet].map(id => records.get(id)).filter(Boolean);
  const midRecords = [...records.values()].filter(record => record.peak >= 40 && record.peak <= 70);
  const chartingRecords = [...records.values()].filter(record => record.peak < 999);
  const qualities = chartingRecords.map(record => record.quality);
  const outcomes = chartingRecords.map(record => 101 - record.peak);

  const tiers = {};
  for (const record of records.values()) {
    if (!record.tier) continue;
    tiers[record.tier] ??= {releases: 0, top100: 0, top20: 0, numberOne: 0, top20Qualities: []};
    const tier = tiers[record.tier];
    tier.releases++;
    if (record.peak <= 100) tier.top100++;
    if (record.peak <= 20) { tier.top20++; tier.top20Qualities.push(record.quality); }
    if (record.peak === 1) tier.numberOne++;
  }
  for (const tier of Object.values(tiers)) {
    tier.top100Rate = tier.top100 / tier.releases;
    tier.top20Rate = tier.top20 / tier.releases;
    tier.averageTop20Quality = mean(tier.top20Qualities);
    delete tier.top20Qualities;
  }

	const launchesByCareerState = {};
	const launchesByCareerAndTier = {};
	for (const record of records.values()) {
		const first = record.rows[0];
		const initialAwareness = num(first.initialLaunchAwareness);
		const initialStock = num(first.initialLaunchStock);
		if (record.leftCensored || initialStock <= 0) continue;
		const careerState = first.launchCareerState || "Unknown";
		const careerAndTier = `${careerState}:${record.tier || "Unknown"}`;
		const launchStrength = initialAwareness + (initialStock / 100000);
		launchesByCareerState[careerState] ??= {awareness: [], stock: [], strength: [], multiplier: []};
		launchesByCareerAndTier[careerAndTier] ??= {awareness: [], stock: [], strength: [], multiplier: []};
		launchesByCareerState[careerState].awareness.push(initialAwareness);
		launchesByCareerState[careerState].stock.push(initialStock);
		launchesByCareerState[careerState].strength.push(launchStrength);
		launchesByCareerState[careerState].multiplier.push(num(first.perceivedQualityMultiplier));
		launchesByCareerAndTier[careerAndTier].awareness.push(initialAwareness);
		launchesByCareerAndTier[careerAndTier].stock.push(initialStock);
		launchesByCareerAndTier[careerAndTier].strength.push(launchStrength);
		launchesByCareerAndTier[careerAndTier].multiplier.push(num(first.perceivedQualityMultiplier));
	}
	for (const groups of [launchesByCareerState, launchesByCareerAndTier]) {
		for (const [group, values] of Object.entries(groups)) {
		groups[group] = {
			count: values.strength.length,
			awareness: summarize(values.awareness),
			stock: summarize(values.stock),
			strength: summarize(values.strength),
			strengthCoefficientOfVariation: coefficientOfVariation(values.strength),
			perceivedQualityMultiplier: summarize(values.multiplier)
		};
		}
	}

  let pointTiesAtTop = 0, unitTiesAtTop = 0;
  const pointGaps = [];
  for (const chartRows of weeklyChart.values()) {
    if (chartRows.length < 2) continue;
    const p1 = num(chartRows[0].chartPoints), p2 = num(chartRows[1].chartPoints);
    if (Math.abs(p1 - p2) < .5) pointTiesAtTop++;
    if (num(chartRows[0].unitsThisWeek) === num(chartRows[1].unitsThisWeek)) unitTiesAtTop++;
    if (p1) pointGaps.push((p1 - p2) / p1);
  }

  const closed = lifecycles.map(row => ({...row, peak: num(row.peakPosition), weeks: num(row.weeksOnChart), units: num(row.lifetimeUnitsSold)}));
  const closedTop40 = closed.filter(row => row.peak > 0 && row.peak <= 40);
  const closedNumberOne = closed.filter(row => row.peak === 1);
  const closedMid = closed.filter(row => row.peak >= 40 && row.peak <= 70);

  return {
    run,
    weekly: {
      weeks: weeks.length,
      annualMarketUnits: weeks.reduce((sum, row) => sum + num(row.totalMarketUnits), 0),
      averageMarketUnits: mean(weeks.map(row => num(row.totalMarketUnits))),
      averageChartUnits: mean(weeks.map(row => num(row.totalChartUnits))),
      initialActiveRecords: num(weeks[0].activeRecords),
      finalActiveRecords: num(weeks.at(-1).activeRecords),
      averageNewEntriesTop100: mean(weeks.map(row => num(row.newEntriesTop100))),
      averageExitsTop100: mean(weeks.map(row => num(row.exitsTop100)))
    },
    numberOne: {
      distinct: numberOneSet.size,
      streaks: summarize(oneStreaks),
      streakHistogram: Object.fromEntries([...new Set(oneStreaks)].sort((a,b)=>a-b).map(value => [value, oneStreaks.filter(v => v === value).length])),
      observedLifetimeUnits: summarize(numberOneRecords.map(record => record.finalTotal)),
      pointTiesAtTop,
      unitTiesAtTop,
      relativePointGap: summarize(pointGaps)
    },
    chartLife: {
      closedCount: closed.length,
      closedTop40Weeks: summarize(closedTop40.map(row => row.weeks)),
      closedTop40Count: closedTop40.length,
      closedNumberOneUnits: summarize(closedNumberOne.map(row => row.units)),
      closedMidUnits: summarize(closedMid.map(row => row.units)),
      observedTop40Weeks: summarize(chartingRecords.filter(record => record.peak <= 40).map(record => record.finalWeeks)),
      observedMidLifetimeUnits: summarize(midRecords.map(record => record.finalTotal))
    },
    quality: {
      chartingRecordCount: chartingRecords.length,
      pearsonOutcome: correlation(qualities, outcomes),
      spearmanOutcome: correlation(rank(qualities), rank(outcomes)),
      numberOneQuality: summarize(numberOneRecords.map(record => record.quality)),
      top20Quality: summarize(chartingRecords.filter(record => record.peak <= 20).map(record => record.quality)),
      allChartingQuality: summarize(qualities)
    },
    volatility: {
      moves: summarize(moves), upwardMoves: summarize(upwardMoves), downwardMoves: summarize(downwardMoves),
      movesOver20: moves.filter(value => value > 20).length,
      movesOver20Rate: moves.filter(value => value > 20).length / transitions,
      movesOver40: moves.filter(value => value > 40).length,
      hugeRise80To5: hugeRise, hugeFall5To80: hugeFall
    },
    saturation: {
      maximum: Math.max(...[...records.values()].map(record => record.maxSaturation)),
      recordsOverOne: [...records.values()].filter(record => record.maxSaturation > 1).length,
			correlationWithNextWeekSalesChange: correlation(saturationChanges.x, saturationChanges.y),
			correlationWithNextWeekNormalizedUnits: correlation(normalizedSaturation.x, normalizedSaturation.y),
			medianWithinRecordCorrelationWithNextWeekNormalizedUnits: median(withinRecordSaturationCorrelations)
    },
    tiers,
		launchesByCareerState,
		launchesByCareerAndTier,
    totalDistinctRecords: records.size
  };
}

const directory = process.argv[2] ?? "SimLogs";
const runs = process.argv.slice(3);
if (!runs.length) runs.push("baseline-1", "baseline-2", "baseline-3");
const result = runs.map(run => analyzeRun(directory, run));
fs.writeFileSync(path.join(directory, `${runs.join("_")}-analysis.json`), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
