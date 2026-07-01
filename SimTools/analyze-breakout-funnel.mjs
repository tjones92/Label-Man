import fs from "node:fs";
import path from "node:path";

function parseCsvLine(line) {
  const values = [];
  let value = "", quoted = false;
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
  const text = fs.readFileSync(file, "utf8").trim();
  if (!text) return [];
  const lines = text.split(/\r?\n/);
  const headers = parseCsvLine(lines.shift());
  return lines.filter(Boolean).map(line => {
    const values = parseCsvLine(line);
    return Object.fromEntries(headers.map((header, index) => [header, values[index]]));
  });
}

const num = value => Number(value ?? 0);
const bool = value => value === "true";
const mean = values => values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;
function median(values) {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const middle = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}
const rate = (numerator, denominator) => denominator ? numerator / denominator : null;

function analyzeRun(directory, run) {
  const funnel = readCsv(path.join(directory, `${run}-breakout-funnel.csv`));
  const recordRows = readCsv(path.join(directory, `${run}-records.csv`));
  const lifecycleRows = readCsv(path.join(directory, `${run}-lifecycles.csv`));
  const pairMap = new Map();
  const releaseMap = new Map();

  for (const row of funnel) {
    const pairKey = `${row.recordId}\u0000${row.regionId}`;
    if (!pairMap.has(pairKey)) pairMap.set(pairKey, {tier: row.labelTier, covered: bool(row.distributionRegionCoverage), rows: []});
    pairMap.get(pairKey).rows.push(row);
    if (!releaseMap.has(row.recordId)) releaseMap.set(row.recordId, {tier: row.labelTier, rows: []});
    releaseMap.get(row.recordId).rows.push(row);
  }

  const byTierCoverage = {};
  for (const pair of pairMap.values()) {
    const key = `${pair.tier}:${pair.covered ? "covered" : "uncovered"}`;
    byTierCoverage[key] ??= {pairs: 0, triggered: 0, subThresholdBackorder: 0, triggerEvents: [], weekly: {1: [], 2: [], 3: []}};
    const group = byTierCoverage[key];
    const everTriggered = pair.rows.some(row => bool(row.preChartBreakoutTriggered));
    const backorders = pair.rows.map(row => num(row.unitsBackordered));
    const hasSubThresholdOnly = backorders.some(value => value > 0) && Math.max(...backorders) <= 500;
    group.pairs++;
    if (everTriggered) group.triggered++;
    if (hasSubThresholdOnly) group.subThresholdBackorder++;
    for (const row of pair.rows) {
      group.weekly[num(row.weeksSinceRelease)].push(row);
      if (bool(row.preChartBreakoutTriggered)) group.triggerEvents.push(row);
    }
  }

  for (const [key, group] of Object.entries(byTierCoverage)) {
    const restocks = group.triggerEvents.map(row => num(row.restockAmount));
    const restockPercents = group.triggerEvents.map(row => rate(num(row.restockAmount), num(row.weekStartStock)) ?? 0);
    const summarizedWeeks = {};
    for (const [week, rows] of Object.entries(group.weekly)) {
      summarizedWeeks[week] = {
        observations: rows.length,
        triggerRate: rate(rows.filter(row => bool(row.preChartBreakoutTriggered)).length, rows.length),
        medianRawSales: median(rows.map(row => num(row.rawSales))),
        medianWeekStartStock: median(rows.map(row => num(row.weekStartStock))),
        medianAwareBuyers: median(rows.map(row => num(row.awareBuyers))),
        medianConversionRate: median(rows.map(row => num(row.conversionRate)))
      };
    }
    byTierCoverage[key] = {
      releaseRegionFunnels: group.pairs,
      everTriggerRate: rate(group.triggered, group.pairs),
      subThresholdBackorderRate: rate(group.subThresholdBackorder, group.pairs),
      triggerEvents: group.triggerEvents.length,
      restockAmount: {median: median(restocks), mean: mean(restocks)},
      restockAsStartingStock: {median: median(restockPercents), mean: mean(restockPercents)},
      capacityCapRate: rate(group.triggerEvents.filter(row => bool(row.capacityCapped)).length, group.triggerEvents.length),
      weekly: summarizedWeeks
    };
  }

	const byTierCoverageRelease = {};
	for (const release of releaseMap.values()) {
		for (const covered of [true, false]) {
			const rows = release.rows.filter(row => bool(row.distributionRegionCoverage) === covered);
			if (!rows.length) continue;
			const key = `${release.tier}:${covered ? "covered" : "uncovered"}`;
			byTierCoverageRelease[key] ??= {releases: 0, triggered: 0, subThresholdBackorder: 0, nonTriggeredSubThresholdBackorder: 0, events: [], weekly: {1: [], 2: [], 3: []}};
			const group = byTierCoverageRelease[key];
			const backorders = rows.map(row => num(row.unitsBackordered));
			group.releases++;
			const everTriggered = rows.some(row => bool(row.preChartBreakoutTriggered));
			const subThresholdBackorder = backorders.some(value => value > 0) && Math.max(...backorders) <= 500;
			if (everTriggered) group.triggered++;
			if (subThresholdBackorder) group.subThresholdBackorder++;
			if (!everTriggered && subThresholdBackorder) group.nonTriggeredSubThresholdBackorder++;
			group.events.push(...rows.filter(row => bool(row.preChartBreakoutTriggered)));
			for (const age of [1, 2, 3]) {
				const ageRows = rows.filter(row => num(row.weeksSinceRelease) === age);
				if (ageRows.length) group.weekly[age].push(ageRows);
			}
		}
	}
	for (const [key, group] of Object.entries(byTierCoverageRelease)) {
		const restocks = group.events.map(row => num(row.restockAmount));
		const restockPercents = group.events.map(row => rate(num(row.restockAmount), num(row.weekStartStock)) ?? 0);
		const restockPreRestockPercents = group.events.map(row => rate(num(row.restockAmount), num(row.preRestockStock))).filter(value => value !== null);
		const restockDemandPercents = group.events.map(row => rate(num(row.restockAmount), num(row.rawSales))).filter(value => value !== null);
		const weekly = {};
		for (const [age, releases] of Object.entries(group.weekly)) {
			const rows = releases.flat();
			weekly[age] = {
				releases: releases.length,
				triggerRate: rate(releases.filter(ageRows => ageRows.some(row => bool(row.preChartBreakoutTriggered))).length, releases.length),
				medianRawSalesPerRegion: median(rows.map(row => num(row.rawSales))),
				medianWeekStartStockPerRegion: median(rows.map(row => num(row.weekStartStock))),
				medianAwareBuyersPerRegion: median(rows.map(row => num(row.awareBuyers))),
				medianConversionRate: median(rows.map(row => num(row.conversionRate)))
			};
		}
		byTierCoverageRelease[key] = {
			releases: group.releases,
			everTriggerRate: rate(group.triggered, group.releases),
			subThresholdBackorderRate: rate(group.subThresholdBackorder, group.releases),
			nonTriggeredSubThresholdBackorderRate: rate(group.nonTriggeredSubThresholdBackorder, group.releases),
			triggerEvents: group.events.length,
			restockAmount: {median: median(restocks), mean: mean(restocks)},
			restockAsStartingStock: {median: median(restockPercents), mean: mean(restockPercents)},
			restockAsPreRestockStock: {median: median(restockPreRestockPercents), mean: mean(restockPreRestockPercents)},
			restockAsRawDemand: {median: median(restockDemandPercents), mean: mean(restockDemandPercents)},
			capacityCapRate: rate(group.events.filter(row => bool(row.capacityCapped)).length, group.events.length),
			weekly
		};
	}

  const age14Rows = new Map();
  for (const row of recordRows) {
    if (!releaseMap.has(row.recordId) || num(row.weeksSinceRelease) < 14 || age14Rows.has(row.recordId)) continue;
    age14Rows.set(row.recordId, {peak: num(row.currentPosition) > 0 ? Math.min(num(row.peakPosition) || 999, num(row.currentPosition)) : num(row.peakPosition)});
  }
  const lifecycleById = new Map(lifecycleRows.map(row => [row.recordId, row]));

  const outcomes = {};
  for (const [recordId, release] of releaseMap) {
    const first = [...release.rows].sort((a, b) => num(a.week) - num(b.week))[0];
    let outcome = age14Rows.get(recordId);
    if (!outcome) {
      const lifecycle = lifecycleById.get(recordId);
      const inferredAge = lifecycle ? num(first.weeksSinceRelease) + num(lifecycle.week) - num(first.week) : 0;
      if (lifecycle && inferredAge >= 14) outcome = {peak: num(lifecycle.peakPosition)};
    }
    if (!outcome) continue;

    const triggered = release.rows.some(row => bool(row.preChartBreakoutTriggered));
    const key = `${release.tier}:${triggered ? "triggered" : "notTriggered"}`;
    outcomes[key] ??= {matureReleases: 0, charted: 0, peaks: []};
    outcomes[key].matureReleases++;
    if (outcome.peak > 0 && outcome.peak <= 100) {
      outcomes[key].charted++;
      outcomes[key].peaks.push(outcome.peak);
    }
  }
  for (const [key, outcome] of Object.entries(outcomes)) {
    outcomes[key] = {
      matureReleases: outcome.matureReleases,
      chartedByWeek14Rate: rate(outcome.charted, outcome.matureReleases),
      medianPeakIfCharted: median(outcome.peaks)
    };
  }

  return {run, rows: funnel.length, byTierCoverageRelease, byTierCoverageRegion: byTierCoverage, outcomes};
}

const directory = process.argv[2] ?? "SimLogs";
const runs = process.argv.slice(3);
if (!runs.length) throw new Error("Pass at least one run name.");
const result = runs.map(run => analyzeRun(directory, run));
const output = path.join(directory, `${runs.join("_")}-breakout-analysis.json`);
fs.writeFileSync(output, JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
