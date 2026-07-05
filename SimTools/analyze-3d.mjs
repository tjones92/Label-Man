import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const logDirectory = path.resolve("SimLogs");

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

async function rows(file, visit) {
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
    const row = Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
    visit(row);
  }
}

const number = value => value === "" || value == null ? null : Number(value);
const mean = values => values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;
function median(values) {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const middle = (sorted.length - 1) / 2;
  return (sorted[Math.floor(middle)] + sorted[Math.ceil(middle)]) / 2;
}
function correlation(pairs) {
  if (pairs.length < 2) return null;
  const meanX = mean(pairs.map(pair => pair[0]));
  const meanY = mean(pairs.map(pair => pair[1]));
  let covariance = 0;
  let varianceX = 0;
  let varianceY = 0;
  for (const [x, y] of pairs) {
    const dx = x - meanX;
    const dy = y - meanY;
    covariance += dx * dy;
    varianceX += dx * dx;
    varianceY += dy * dy;
  }
  return varianceX > 0 && varianceY > 0 ? covariance / Math.sqrt(varianceX * varianceY) : null;
}
function yearObject(map, year, create) {
  if (!map.has(year)) map.set(year, create());
  return map.get(year);
}

async function analyze(run) {
  const prefix = path.join(logDirectory, run);
  const yearByWeek = new Map();
  const weeks = new Map();
  await rows(`${prefix}-weeks.csv`, row => {
    const year = number(row.year);
    const week = number(row.week);
    yearByWeek.set(week, year);
    const annual = yearObject(weeks, year, () => ({ entries: 0, units: 0 }));
    annual.entries += number(row.newEntriesTop100) ?? 0;
    annual.units += number(row.totalMarketUnits) ?? 0;
  });

  const releases = new Map();
  await rows(`${prefix}-release-capacity.csv`, row => {
    const year = number(row.year);
    releases.set(year, (releases.get(year) ?? 0) + (number(row.successfulReleases) ?? 0));
  });

  const records = new Map();
  await rows(`${prefix}-records.csv`, row => {
    const position = number(row.currentPosition);
    if (!(position > 0)) return;
    const year = number(row.year);
    const byId = yearObject(records, year, () => new Map());
    const id = row.recordId;
    const existing = byId.get(id);
    if (!existing) byId.set(id, { quality: number(row.quality), peak: position });
    else existing.peak = Math.min(existing.peak, position);
  });

  const closed = new Map();
  await rows(`${prefix}-lifecycles.csv`, row => {
    const peak = number(row.peakPosition);
    if (!(peak > 0 && peak <= 40)) return;
    const year = yearByWeek.get(number(row.week));
    yearObject(closed, year, () => []).push(number(row.weeksOnChart));
  });

  const rollups = new Map();
  await rows(`${prefix}-decade-annual-rollup.csv`, row => rollups.set(number(row.year), row));

  const annual = {};
  for (const year of [...weeks.keys()].sort()) {
    const charting = [...(records.get(year)?.values() ?? [])];
    const rollup = rollups.get(year) ?? {};
    annual[year] = {
      units: number(rollup.singleUnits) + (number(rollup.albumUnits) ?? 0),
      singleUnits: number(rollup.singleUnits),
      albumUnits: number(rollup.albumUnits),
      pearson: charting.length ? correlation(charting.map(record => [record.quality, 101 - record.peak])) : number(rollup.singlePearson),
      pearsonN: charting.length || (number(rollup.singlePearsonN) ?? 0),
      closedTop40Median: (closed.get(year)?.length ?? 0) ? median(closed.get(year)) : number(rollup.closedTop40Median),
      closedTop40N: (closed.get(year)?.length ?? 0) || (number(rollup.closedTop40N) ?? 0),
      competitionRatio: weeks.get(year).entries ? (releases.get(year) ?? 0) / weeks.get(year).entries : null,
      releases: releases.get(year) ?? 0,
      entries: weeks.get(year).entries
    };
    for (const [key, value] of Object.entries(rollup)) {
      if (!(key in annual[year]) && key !== "seed" && key !== "year") annual[year][key] = number(value);
    }
  }
  return { run, annual };
}

const compact = process.argv.includes("--compact");
const runs = process.argv.slice(2).filter(argument => argument !== "--compact");
if (!runs.length) throw new Error("Pass one or more SimLogs run prefixes.");
const results = [];
for (const run of runs) results.push(await analyze(run));
if (compact) {
  const compactResults = results.flatMap(result => Object.entries(result.annual).map(([year, annual]) => ({
    run: result.run,
    year: Number(year),
    units: annual.units,
    pearson: annual.pearson,
    pearsonN: annual.pearsonN,
    closedTop40Median: annual.closedTop40Median,
    closedTop40N: annual.closedTop40N,
    competitionRatio: annual.competitionRatio
  })));
  process.stdout.write(`${JSON.stringify(compactResults, null, 2)}\n`);
} else process.stdout.write(`${JSON.stringify(results, null, 2)}\n`);
