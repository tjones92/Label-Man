import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const logs = path.resolve("SimLogs");

function splitCsv(line) {
  const values = [];
  let value = "";
  let quoted = false;
  for (let i = 0; i < line.length; i++) {
    const character = line[i];
    if (character === '"') {
      if (quoted && line[i + 1] === '"') {
        value += '"';
        i++;
      } else quoted = !quoted;
    } else if (character === "," && !quoted) {
      values.push(value);
      value = "";
    } else value += character;
  }
  values.push(value);
  return values;
}

async function visitRows(file, visit) {
  const input = fs.createReadStream(file);
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers;
  for await (const line of lines) {
    if (!headers) {
      headers = splitCsv(line);
      continue;
    }
    if (!line) continue;
    const values = splitCsv(line);
    visit(Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""])));
  }
}

const add = (map, key, value) => map.set(key, (map.get(key) ?? 0) + Number(value || 0));
const object = map => Object.fromEntries([...map.entries()].sort(([a], [b]) => a.localeCompare(b)));

async function analyze(run, summaryOnly = false) {
  const prefix = path.join(logs, run);
  const homeRegions = new Map();
  await visitRows(`${prefix}-label-geography.csv`, row => add(homeRegions, row.homeRegion, 1));

  const annualUnits = new Map();
  await visitRows(`${prefix}-decade-annual-rollup.csv`, row => {
    annualUnits.set(row.year, Number(row.singleUnits || 0) + Number(row.albumUnits || 0));
  });

	const regionGenreCharted = new Map();
	const annualGenreUnits = new Map();
  const tierUnits = new Map();
  const tierHomeUnits = new Map();
  const destinationBackorders = new Map();
  const destinationNonNationalBackorders = new Map();
	await visitRows(`${prefix}-geography-metrics.csv`, row => {
		add(regionGenreCharted, `${row.regionId}|${row.genre}`, row.chartedUnits);
		add(annualGenreUnits, `${row.year}|${row.genre}`, row.totalUnits);
    add(tierUnits, row.labelTier, row.totalUnits);
    add(tierHomeUnits, row.labelTier, row.homeRegionUnits);
    add(destinationBackorders, row.destinationTier, row.backorders);
    add(destinationNonNationalBackorders, row.destinationTier, row.nonNationalBackorders);
  });

  const glGpGenreShare = {};
  const genres = new Set([...regionGenreCharted.keys()].map(key => key.split("|")[1]));
  for (const genre of [...genres].sort()) {
    const greatLakes = regionGenreCharted.get(`greatlakes|${genre}`) ?? 0;
    const greatPlains = regionGenreCharted.get(`greatplains|${genre}`) ?? 0;
    const total = greatLakes + greatPlains;
    glGpGenreShare[genre] = {
      greatLakesUnits: greatLakes,
      greatPlainsUnits: greatPlains,
      greatLakesShare: total ? greatLakes / total : null,
      greatPlainsShare: total ? greatPlains / total : null
    };
  }

  const homeShareByTier = {};
  for (const tier of [...tierUnits.keys()].sort()) {
    const total = tierUnits.get(tier);
    const home = tierHomeUnits.get(tier) ?? 0;
    homeShareByTier[tier] = { homeUnits: home, totalUnits: total, share: total ? home / total : null };
  }

  let dealMetrics = {};
  await visitRows(`${prefix}-deal-metrics.csv`, row => { dealMetrics = row; });
  return {
    run,
		annualUnits: object(annualUnits),
		...(summaryOnly ? {} : { annualGenreUnits: object(annualGenreUnits) }),
    homeRegionDistribution: object(homeRegions),
    glGpGenreShare,
    homeShareByTier,
    backordersByDestinationTier: object(destinationBackorders),
    nonNationalBackordersByDestinationTier: object(destinationNonNationalBackorders),
    dealMetrics
  };
}

async function baselineGenreUnits(run) {
  const totals = new Map();
  await visitRows(path.join(logs, `${run}-records.csv`), row => add(totals, `${row.year}|${row.genre}`, row.unitsThisWeek));
  return object(totals);
}

const summaryOnly = process.argv.includes("--summary");
const runs = process.argv.slice(2).filter(argument => !argument.startsWith("--baseline=") && argument !== "--summary");
const baseline = process.argv.slice(2).find(argument => argument.startsWith("--baseline="))?.split("=")[1];
if (!runs.length) throw new Error("Usage: node SimTools/analyze-geography-4b.mjs <run> [<run> ...] [--baseline=<run>]");

const result = { runs: [] };
for (const run of runs) result.runs.push(await analyze(run, summaryOnly));
if (baseline) result.baselineGenreUnits = await baselineGenreUnits(baseline);
console.log(JSON.stringify(result, null, 2));
