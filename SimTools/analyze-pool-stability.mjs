import fs from "node:fs";
import path from "node:path";

function readCsv(file) {
  const [header, ...lines] = fs.readFileSync(file, "utf8").trim().split(/\r?\n/);
  const columns = header.split(",");
  return lines.filter(Boolean).map(line => {
    const values = line.split(",");
    return Object.fromEntries(columns.map((column, index) => [column, values[index]?.replace(/^"|"$/g, "")]));
  });
}

function slope(values) {
  if (values.length < 2) return 0;
  const meanX = (values.length - 1) / 2;
  const meanY = values.reduce((sum, value) => sum + value, 0) / values.length;
  let numerator = 0, denominator = 0;
  for (let index = 0; index < values.length; index++) {
    numerator += (index - meanX) * (values[index] - meanY);
    denominator += (index - meanX) ** 2;
  }
  return numerator / denominator;
}

const directory = process.argv[2] ?? "SimLogs";
const run = process.argv[3] ?? "stability-5y";
const rows = readCsv(path.join(directory, `${run}-weeks.csv`)).map(row => ({
  week: Number(row.week), year: Number(row.year), active: Number(row.activeRecords),
  newRecords: Number(row.newRecords ?? 0), retiredRecords: Number(row.retiredRecords ?? 0)
}));

const years = [...new Set(rows.map(row => row.year))].map(year => {
  const values = rows.filter(row => row.year === year).map(row => row.active);
	const yearRows = rows.filter(row => row.year === year);
  const tail = values.slice(-Math.min(13, values.length));
  return {
    year,
    weeks: values.length,
    start: values[0],
    end: values.at(-1),
    net: values.at(-1) - values[0],
    average: values.reduce((sum, value) => sum + value, 0) / values.length,
    minimum: Math.min(...values),
    maximum: Math.max(...values),
		newRecords: yearRows.reduce((sum, row) => sum + row.newRecords, 0),
		retiredRecords: yearRows.reduce((sum, row) => sum + row.retiredRecords, 0),
    finalQuarterSlopePerWeek: slope(tail)
  };
});

const quarters = [];
for (let start = 0; start < rows.length; start += 13) {
  const group = rows.slice(start, start + 13);
  const values = group.map(row => row.active);
  quarters.push({
    weeks: `${group[0].week}-${group.at(-1).week}`,
    start: values[0],
    end: values.at(-1),
    net: values.at(-1) - values[0],
    slopePerWeek: slope(values)
  });
}

const finalYearValues = rows.slice(-52).map(row => row.active);
const result = {
  run,
  totalWeeks: rows.length,
  start: rows[0].active,
  end: rows.at(-1).active,
  years,
  quarters,
  finalYearSlopePerWeek: slope(finalYearValues),
  finalQuarterSlopePerWeek: slope(finalYearValues.slice(-13))
};

const output = path.join(directory, `${run}-stability.json`);
fs.writeFileSync(output, JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
