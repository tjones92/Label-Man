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
const sum = values => values.reduce((total, value) => total + value, 0);
const percentile = (values, p) => {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const index = (sorted.length - 1) * p, lower = Math.floor(index), upper = Math.ceil(index);
  return sorted[lower] + (sorted[upper] - sorted[lower]) * (index - lower);
};
const distribution = values => ({
  count: values.length,
  min: values.length ? Math.min(...values) : null,
  median: percentile(values, .5),
  p90: percentile(values, .9),
  max: values.length ? Math.max(...values) : null
});

function analyzeRun(directory, run) {
  const weeks = readCsv(path.join(directory, `${run}-weeks.csv`));
  const records = readCsv(path.join(directory, `${run}-records.csv`));
  const retirement = readCsv(path.join(directory, `${run}-retirement.csv`));
  const tierVolume = readCsv(path.join(directory, `${run}-tier-volume.csv`));
  const finalWeek = Math.max(...records.map(row => num(row.week)));
  const finalRows = records.filter(row => num(row.week) === finalWeek);
  const offChart = finalRows.filter(row => num(row.currentPosition) === 0);
  const activeOffChart = retirement.filter(row => row.status === "active_off_chart_week52");
  const neverRetired = retirement.filter(row => row.status === "retired" && num(row.weeksOnChart) === 0);
  const neverActive = activeOffChart.filter(row => num(row.weeksOnChart) === 0);
  const tiers = {};
  for (const row of tierVolume) {
    tiers[row.labelTier] ??= {launchUnits: 0, middleUnits: 0, catalogTailUnits: 0, totalUnits: 0};
    for (const key of Object.keys(tiers[row.labelTier])) tiers[row.labelTier][key] += num(row[key]);
  }
  for (const row of records.filter(row => num(row.weeksSinceRelease) > 8 && row.isPlayerOwned !== "true")) {
    tiers[row.labelTier] ??= {launchUnits: 0, middleUnits: 0, catalogTailUnits: 0, totalUnits: 0};
    const key = num(row.weeksOnChart) > 0 ? "catalogTailChartedUnits" : "catalogTailNeverChartedUnits";
    tiers[row.labelTier][key] = (tiers[row.labelTier][key] ?? 0) + num(row.unitsThisWeek);
  }

  const recordsById = new Map();
  for (const row of records) {
    const item = recordsById.get(row.recordId) ?? {tier: row.labelTier, rows: []};
		item.rows.push(row);
		recordsById.set(row.recordId, item);
  }
  const week14ByTier = {};
  for (const tier of ["Major", "MidTier", "Independent", "Boutique", "Small"]) {
		const matured = [...recordsById.values()].filter(record => record.tier === tier && record.rows.some(row => num(row.weeksSinceRelease) >= 14));
		const charted = matured.filter(record => {
			const age14 = record.rows.filter(row => num(row.weeksSinceRelease) >= 14)
				.sort((a, b) => num(a.weeksSinceRelease) - num(b.weeksSinceRelease))[0];
			return num(age14?.currentPosition) > 0;
		});
    week14ByTier[tier] = {matured: matured.length, charted: charted.length, rate: matured.length ? charted.length / matured.length : null};
  }

  return {
    run,
    annualMarketUnits: sum(weeks.map(row => num(row.totalMarketUnits))),
    finalActiveRecords: num(weeks.at(-1).activeRecords),
    finalOffChart: {
      total: offChart.length,
      charted: offChart.filter(row => num(row.weeksOnChart) > 0).length,
      neverCharted: offChart.filter(row => num(row.weeksOnChart) === 0).length,
      chartedUnderFloor: offChart.filter(row => num(row.weeksOnChart) > 0 && num(row.unitsThisWeek) < 50).length,
      neverChartedUnderFloor: offChart.filter(row => num(row.weeksOnChart) === 0 && num(row.unitsThisWeek) < 50).length,
      neverChartedConfirmedDead6: neverActive.filter(row => num(row.unitsThisWeek) < 50 && num(row.weeksSinceSalesAboveFloor) >= 6).length
    },
    neverChartedRetirement: {
      cullAge: distribution(neverRetired.map(row => num(row.weeksSinceRelease))),
      floorBreachAge: distribution(neverRetired.map(row => num(row.floorBreachAge))),
      underFloorStreakAtCull: distribution(neverRetired.map(row => num(row.weeksSinceSalesAboveFloor)))
    },
    tiers,
    week14ByTier
  };
}

const directory = process.argv[2] ?? "SimLogs";
const runs = process.argv.slice(3);
if (!runs.length) throw new Error("Pass one or more run prefixes.");
const result = runs.map(run => analyzeRun(directory, run));
const output = path.join(directory, `${runs.join("_")}-market-volume-turnover.json`);
fs.writeFileSync(output, JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
