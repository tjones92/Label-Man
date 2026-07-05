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
    else if (char === ",") { values.push(value); value = ""; }
    else value += char;
  }
  values.push(value);
  return values;
}

function readCsv(file) {
  const lines = fs.readFileSync(file, "utf8").trim().split(/\r?\n/);
  const headers = parseCsvLine(lines.shift());
  return lines.filter(Boolean).map(line => {
    const values = parseCsvLine(line);
    return Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
  });
}

const qualityCutPoints = [0.465511, 0.550559, 0.623968];
const qualityIndex = value => value <= qualityCutPoints[0] ? 0 : value <= qualityCutPoints[1] ? 1 : value <= qualityCutPoints[2] ? 2 : 3;
const careerIndex = state => state === "NewSigning" || state === "Unsigned" ? 0 : state === "Rising" ? 1 :
  state === "Established" ? 2 : state === "Star" || state === "Superstar" ? 3 : null;
const mean = values => values.reduce((sum, value) => sum + value, 0) / values.length;
const peakScore = peak => peak <= 0 || peak > 80 ? 0 : (80 - peak) / 79;
const logs = path.resolve("SimLogs");
const runs = [
  "a2-cal-baseline-1001", "a2-cal-baseline-1002", "a2-cal-baseline-1003",
  "a2-cal-ref3a-1001", "a2-cal-ref3a-1002", "a2-cal-ref3a-1003",
  "a3-ship-1001a", "a3-ship-1002", "a3-ship-1003"
];
const buckets = Array.from({ length: 4 }, () => Array.from({ length: 4 }, () => []));
for (const run of runs) {
  const peaks = new Map();
  for (const row of readCsv(path.join(logs, `${run}-records.csv`))) {
    const position = Number(row.currentPosition);
    if (position <= 0) continue;
    peaks.set(row.recordId, Math.min(peaks.get(row.recordId) ?? 101, position));
  }
  for (const row of readCsv(path.join(logs, `${run}-calibration-decisions.csv`))) {
    if (row.chosenFormat !== "Single") continue;
    const c = careerIndex(row.careerState);
    if (c == null) continue;
    buckets[qualityIndex(Number(row.qualityEstimate))][c].push(peakScore(peaks.get(row.recordId) ?? 101));
  }
}

// B telemetry is used only for the descriptive promo chart-life estimate.
const promoRuns = ["b-enabled-1001a", "b-enabled-1002", "b-enabled-1003"];
const promoChartWeeks = [];
const completedPromoChartWeeks = [];
for (const run of promoRuns) {
  const promoIds = new Set(readCsv(path.join(logs, `${run}-album-projects.csv`)).map(row => row.promoSingleId).filter(Boolean));
  const maxWeeks = new Map();
  for (const row of readCsv(path.join(logs, `${run}-records.csv`))) {
    if (!promoIds.has(row.recordId)) continue;
    maxWeeks.set(row.recordId, Math.max(maxWeeks.get(row.recordId) ?? 0, Number(row.weeksOnChart)));
  }
  promoChartWeeks.push(...maxWeeks.values());
	for (const row of readCsv(path.join(logs, `${run}-lifecycles.csv`))) {
		if (promoIds.has(row.recordId)) completedPromoChartWeeks.push(Number(row.weeksOnChart));
	}
}

const populated = [];
for (let q = 0; q < 4; q++) for (let c = 0; c < 4; c++) if (buckets[q][c].length >= 20) populated.push({ q, c });
function sourceFor(q, c) {
  if (buckets[q][c].length >= 20) return { q, c };
  return [...populated].sort((a, b) =>
    (Math.abs(a.q - q) + Math.abs(a.c - c)) - (Math.abs(b.q - q) + Math.abs(b.c - c)) ||
    Number(b.c === c) - Number(a.c === c) || Math.abs(a.q - q) - Math.abs(b.q - q) || a.c - b.c || a.q - b.q)[0];
}
const effective = buckets.map((row, q) => row.map((values, c) => {
  const source = sourceFor(q, c);
  return mean(buckets[source.q][source.c]);
}));
const sources = buckets.map((row, q) => row.map((values, c) => {
  const source = sourceFor(q, c);
  return `Q${source.q + 1}xC${source.c + 1}`;
}));

console.log(JSON.stringify({
  runs,
  unchartedPeakSentinel: 101,
  expectedPeakScoreByBucket: effective,
	  sampleCounts: buckets.map(row => row.map(values => values.length)),
	  effectiveSources: sources,
  promoChartLife: {
	  runs: promoRuns,
	  allObservedN: promoChartWeeks.length,
	  meanWeeksOnChartAllObserved: mean(promoChartWeeks),
	  chartedObservedN: promoChartWeeks.filter(value => value > 0).length,
	  meanWeeksOnChartConditionalOnCharting: mean(promoChartWeeks.filter(value => value > 0)),
	  completedN: completedPromoChartWeeks.length,
	  meanWeeksOnChartCompleted: mean(completedPromoChartWeeks)
	}
}, null, 2));
