import fs from "node:fs";
import path from "node:path";

function parseCsvLine(line) {
  const values = []; let value = "", quoted = false;
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
  values.push(value); return values;
}

function readCsv(file) {
  const lines = fs.readFileSync(file, "utf8").trim().split(/\r?\n/);
  if (lines.length < 2) return [];
  const headers = parseCsvLine(lines.shift());
  return lines.filter(Boolean).map(line => {
    const values = parseCsvLine(line);
    return Object.fromEntries(headers.map((header, index) => [header, values[index]]));
  });
}

const num = value => Number(value ?? 0);
const mean = values => values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;
const median = values => {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b), middle = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
};
const rate = (n, d) => d ? n / d : null;
const indieFamily = new Set(["Independent", "Boutique", "Small"]);
const stageRank = {None: 0, LocalTraction: 1, NeighboringMarketTest: 2, RegionalBreakout: 3, NationalCrossoverCandidate: 4};
const scoreBand = score => score < .2 ? "0-.20" : score < .3 ? ".20-.30" : score < .4 ? ".30-.40" : score < .5 ? ".40-.50" : ".50+";

function analyzeRun(directory, run) {
  const recordRows = readCsv(path.join(directory, `${run}-records.csv`));
  const funnelRows = readCsv(path.join(directory, `${run}-breakout-funnel.csv`));
  const lifecycleRows = readCsv(path.join(directory, `${run}-lifecycles.csv`));
  const records = new Map(), regions = new Map(), regionsByRecord = new Map();

  for (const row of recordRows) {
    const entry = records.get(row.recordId) ?? {id: row.recordId, title: row.title, tier: row.labelTier, quality: num(row.quality), rows: [], peak: 0};
    entry.rows.push(row);
    const position = num(row.currentPosition);
    if (position > 0 && (!entry.peak || position < entry.peak)) entry.peak = position;
    records.set(row.recordId, entry);
  }
  for (const row of lifecycleRows) {
    const entry = records.get(row.recordId);
    if (entry && num(row.peakPosition) > 0 && (!entry.peak || num(row.peakPosition) < entry.peak)) entry.peak = num(row.peakPosition);
  }
  for (const row of funnelRows) {
    const key = `${row.recordId}\u0000${row.regionId}`;
    const entry = regions.get(key) ?? {recordId: row.recordId, regionId: row.regionId, tier: row.labelTier, covered: row.distributionRegionCoverage === "true", rows: []};
    entry.rows.push(row); regions.set(key, entry);
		if (!regionsByRecord.has(row.recordId)) regionsByRecord.set(row.recordId, new Set());
		regionsByRecord.get(row.recordId).add(entry);
  }

  const matured = [...records.values()].filter(record => record.rows.some(row => num(row.weeksSinceRelease) >= 14));
  for (const record of matured) {
    const age14 = record.rows
			.filter(row => num(row.weeksSinceRelease) >= 14)
			.sort((a, b) => num(a.weeksSinceRelease) - num(b.weeksSinceRelease))[0];
    record.chartedBy14 = num(age14?.currentPosition) > 0;
    record.top20By14 = num(age14?.currentPosition) > 0 && num(age14?.currentPosition) <= 20;
    const recordRegions = [...(regionsByRecord.get(record.id) ?? [])];
    record.peakBreakoutScore = Math.max(0, ...recordRegions.flatMap(region => region.rows.map(row => num(row.breakoutScore))));
    record.maxStage = Math.max(0, ...recordRegions.flatMap(region => region.rows.map(row => stageRank[row.breakoutStage] ?? 0)));
    record.regions = recordRegions;
  }

  const week14ByTier = {};
  for (const tier of ["Major", "MidTier", "Independent", "Boutique", "Small"]) {
    const group = matured.filter(record => record.tier === tier);
    const charted = group.filter(record => record.chartedBy14);
    week14ByTier[tier] = {
      matured: group.length, charted: charted.length, chartRate: rate(charted.length, group.length),
      top20: group.filter(record => record.top20By14).length,
      peakIfCharted: {median: median(charted.map(record => record.peak)), min: charted.length ? Math.min(...charted.map(record => record.peak)) : null}
    };
  }

  const progression = {};
  for (const tier of ["Major", "MidTier", "Independent", "Boutique", "Small"]) {
    const group = matured.filter(record => record.tier === tier);
    progression[tier] = Object.fromEntries(Object.entries(stageRank).map(([stage, rank]) =>
      [stage, group.filter(record => record.maxStage >= rank && rank > 0).length]));
  }

  const matchedCoverage = {};
  for (const region of regions.values()) {
    for (const row of region.rows) {
      const band = scoreBand(num(row.breakoutScore));
      const key = `${band}:${region.covered ? "covered" : "uncovered"}`;
      const group = matchedCoverage[key] ?? {observations: 0, rawDemand: [], sales: [], restock: [], backorders: []};
      group.observations++;
      group.rawDemand.push(num(row.rawSales)); group.sales.push(num(row.unitsSoldThisWeek));
      group.restock.push(num(row.restockAmount)); group.backorders.push(num(row.unitsBackordered));
      matchedCoverage[key] = group;
    }
  }
  for (const [key, group] of Object.entries(matchedCoverage)) {
    matchedCoverage[key] = {
      observations: group.observations, medianRawDemand: median(group.rawDemand), medianSales: median(group.sales),
      fulfillmentRate: rate(group.sales.reduce((a,b)=>a+b,0), group.rawDemand.reduce((a,b)=>a+b,0)),
      medianRestock: median(group.restock), medianBackorders: median(group.backorders)
    };
  }

  const stageEvents = Object.fromEntries(Object.keys(stageRank).map(stage => [stage,
    new Set(funnelRows.filter(row => row.breakoutStage === stage).map(row => row.recordId)).size]));
	const funnelByTier = {};
	for (const tier of ["Major", "MidTier", "Independent", "Boutique", "Small"]) {
		// Keep the before/after launch funnel comparable with the legacy harness,
		// which captured only release ages 1-3.
		const rows = funnelRows.filter(row => row.labelTier === tier && num(row.weeksSinceRelease) <= 3);
		const triggered = rows.filter(row => (row.restockTriggered ?? row.preChartBreakoutTriggered) === "true").length;
		funnelByTier[tier] = {
			observations: rows.length,
			medianStartingStock: median(rows.map(row => num(row.weekStartStock))),
			medianAwareBuyers: median(rows.map(row => num(row.awareBuyers))),
			medianConversionRate: median(rows.map(row => num(row.conversionRate))),
			medianRawDemand: median(rows.map(row => num(row.rawSales))),
			medianActualSales: median(rows.map(row => num(row.unitsSoldThisWeek))),
			medianBackorders: median(rows.map(row => num(row.unitsBackordered))),
			restockTriggerRate: rate(triggered, rows.length),
			medianRestock: median(rows.filter(row => num(row.restockAmount) > 0).map(row => num(row.restockAmount)))
		};
	}
  const gains = {
    awareness: funnelRows.reduce((sum, row) => sum + num(row.breakoutAwarenessGain), 0),
    radio: funnelRows.reduce((sum, row) => sum + num(row.breakoutRadioGain), 0),
    wordOfMouth: funnelRows.reduce((sum, row) => sum + num(row.breakoutWordOfMouthGain), 0)
  };

  function example(record) {
    const region = record.regions.sort((a,b) => Math.max(...b.rows.map(r=>num(r.breakoutScore))) - Math.max(...a.rows.map(r=>num(r.breakoutScore))))[0];
    const peakRow = region?.rows.reduce((best, row) => !best || num(row.breakoutScore) > num(best.breakoutScore) ? row : best, null);
    const age14 = record.rows.filter(row => num(row.weeksSinceRelease) >= 14).sort((a,b) => num(a.weeksSinceRelease) - num(b.weeksSinceRelease))[0];
    return {recordId: record.id, title: record.title, tier: record.tier, quality: record.quality, chartPeak: record.peak || null,
		age14Position: num(age14?.currentPosition), age14Units: num(age14?.unitsThisWeek), age14Points: num(age14?.chartPoints),
		age14CutoffDistance: num(age14?.distanceFrom100Cutoff), age14Awareness: num(age14?.awareness), age14Radio: num(age14?.radioHeat),
      peakBreakoutScore: record.peakBreakoutScore, stage: peakRow?.breakoutStage, region: region?.regionId,
      covered: region?.covered, week: num(peakRow?.week), age: num(peakRow?.weeksSinceRelease), rawDemand: num(peakRow?.rawSales),
      sales: num(peakRow?.unitsSoldThisWeek), velocity: num(peakRow?.salesVelocity), awarenessInput: num(peakRow?.audienceInput),
      mediaInput: num(peakRow?.mediaInput), backorders: num(peakRow?.unitsBackordered), restock: num(peakRow?.restockAmount)};
  }
  const indie = matured.filter(record => indieFamily.has(record.tier));
  const examples = {
    successful: indie.filter(record => record.chartedBy14).sort((a,b) => b.peakBreakoutScore - a.peakBreakoutScore).slice(0, 6).map(example),
    failed: indie.filter(record => !record.chartedBy14).sort((a,b) => b.peakBreakoutScore - a.peakBreakoutScore).slice(0, 6).map(example)
  };

  return {run, rows: {records: recordRows.length, funnel: funnelRows.length}, week14ByTier, funnelByTier, progression, stageEvents, gains, matchedCoverage, examples};
}

const directory = process.argv[2] ?? "SimLogs";
const runs = process.argv.slice(3);
if (!runs.length) throw new Error("Pass at least one run name.");
const result = runs.map(run => analyzeRun(directory, run));
const output = path.join(directory, `${runs.join("_")}-regional-breakout-analysis.json`);
fs.writeFileSync(output, JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
