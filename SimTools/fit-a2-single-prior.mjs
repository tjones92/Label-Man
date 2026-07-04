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

function percentile(values, fraction) {
  const sorted = [...values].sort((a, b) => a - b);
  const position = (sorted.length - 1) * fraction;
  const low = Math.floor(position), high = Math.ceil(position);
  return sorted[low] + (sorted[high] - sorted[low]) * (position - low);
}

const careerBands = ["New/Unsigned", "Rising", "Established", "Star/Superstar"];
function careerBand(state) {
  if (state === "NewSigning" || state === "Unsigned") return "New/Unsigned";
  if (state === "Rising") return "Rising";
  if (state === "Established") return "Established";
  if (state === "Star" || state === "Superstar") return "Star/Superstar";
  return null;
}

const logs = path.resolve("SimLogs");
const runs = [
  "a2-cal-baseline-1001", "a2-cal-baseline-1002", "a2-cal-baseline-1003",
  "a2-cal-ref3a-1001", "a2-cal-ref3a-1002", "a2-cal-ref3a-1003"
];
const epsilon = 1e-6;
const eligible = [];
const unexpectedCareerStates = new Map();
let epsilonInvocations = 0;

for (const run of runs) {
  const decisions = new Map(readCsv(path.join(logs, `${run}-calibration-decisions.csv`))
    .map(row => [row.recordId, row]));
  for (const outcome of readCsv(path.join(logs, `${run}-release-outcomes.csv`))) {
    if (outcome.format !== "Single" || outcome.memoryEligible !== "true") continue;
    const decision = decisions.get(outcome.recordId);
    if (!decision) throw new Error(`${run}: missing decision input for ${outcome.recordId}`);
    const band = careerBand(decision.careerState);
    if (!band) {
      unexpectedCareerStates.set(decision.careerState,
        (unexpectedCareerStates.get(decision.careerState) ?? 0) + 1);
      continue;
    }
    const modifier = Number(decision.reachFactor) * Number(decision.genreSinglesMarketFactor);
    if (modifier < epsilon) epsilonInvocations++;
    eligible.push({
      run,
      recordId: outcome.recordId,
      quality: Number(decision.qualityEstimate),
      careerBand: band,
      normalizedContribution: (Number(outcome.realizedNet) + Number(outcome.sunkProductionCost)) /
        Math.max(modifier, epsilon)
    });
  }
}

const qualityCutPoints = [0.25, 0.5, 0.75].map(fraction =>
  percentile(eligible.map(row => row.quality), fraction));
function qualityIndex(value) {
  if (value <= qualityCutPoints[0]) return 0;
  if (value <= qualityCutPoints[1]) return 1;
  if (value <= qualityCutPoints[2]) return 2;
  return 3;
}

const raw = Array.from({ length: 4 }, () => Array.from({ length: 4 }, () => []));
for (const row of eligible) raw[qualityIndex(row.quality)][careerBands.indexOf(row.careerBand)].push(row.normalizedContribution);
const mean = values => values.reduce((sum, value) => sum + value, 0) / values.length;
const populated = [];
for (let q = 0; q < 4; q++) for (let c = 0; c < 4; c++) {
  if (raw[q][c].length >= 20) populated.push({ q, c });
}

function sourceFor(q, c) {
  if (raw[q][c].length >= 20) return { q, c };
  return [...populated].sort((a, b) =>
    (Math.abs(a.q - q) + Math.abs(a.c - c)) - (Math.abs(b.q - q) + Math.abs(b.c - c)) ||
    Number(b.c === c) - Number(a.c === c) ||
    Math.abs(a.q - q) - Math.abs(b.q - q) ||
    a.c - b.c || a.q - b.q)[0];
}

const buckets = [];
for (let q = 0; q < 4; q++) for (let c = 0; c < 4; c++) {
  const source = sourceFor(q, c);
  buckets.push({
    qualityQuartile: `Q${q + 1}`,
    careerBand: careerBands[c],
    rawN: raw[q][c].length,
    rawMeanNormalizedContribution: raw[q][c].length ? mean(raw[q][c]) : null,
    effectiveMeanNormalizedContribution: mean(raw[source.q][source.c]),
    sourceBucket: `Q${source.q + 1} x ${careerBands[source.c]}`,
    sourceN: raw[source.q][source.c].length,
    borrowed: source.q !== q || source.c !== c
  });
}

console.log(JSON.stringify({
  runs,
  epsilon,
  epsilonInvocations,
  eligibleN: eligible.length,
  unexpectedCareerStates: Object.fromEntries([...unexpectedCareerStates].sort()),
  qualityCutPoints,
  buckets
}, null, 2));
