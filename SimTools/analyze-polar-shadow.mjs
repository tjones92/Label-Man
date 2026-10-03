import fs from 'node:fs';
import path from 'node:path';

function cells(line) {
  const values = []; let cell = '', quoted = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (quoted) { if (c === '"' && line[i + 1] === '"') { cell += c; i++; } else if (c === '"') quoted = false; else cell += c; }
    else if (c === '"') quoted = true;
    else if (c === ',') { values.push(cell); cell = ''; }
    else cell += c;
  }
  values.push(cell); return values;
}
function csv(file) {
  const lines = fs.readFileSync(file, 'utf8').trim().split(/\r?\n/), header = cells(lines.shift());
  return lines.filter(Boolean).map(line => Object.fromEntries(cells(line).map((v, i) => [header[i], v])));
}
const directory = process.argv[2] ?? 'SimLogs';
const files = fs.readdirSync(directory);
const summary = [];
for (const seed of [1001, 1002]) {
  const control = `polar-shadow-control-${seed}`, candidate = `polar-shadow-candidate-${seed}`;
  const complete = run => fs.readFileSync(path.join(directory, `${run}.log`), 'utf8').includes(`CHART_AUDIT_COMPLETE run=${run} weeks=52`);
  if (!complete(control) || !complete(candidate)) throw new Error(`Incomplete run pair for seed ${seed}`);
  const existing = files.filter(f => f.startsWith(control + '-') && f.endsWith('.csv')).sort();
  const mismatches = existing.filter(f => !fs.readFileSync(path.join(directory, f)).equals(fs.readFileSync(path.join(directory, candidate + f.slice(control.length)))));
  const shadow = csv(path.join(directory, `${candidate}-polar-fit.csv`));
  const budget = csv(path.join(directory, `${candidate}-polar-fit-budget.csv`));
  const invalid = shadow.filter(r => ['capability', 'identity', 'moment', 'deficit', 'stretch', 'referenceIdentityDistance', 'realizedCapability', 'realizedIdentity', 'realizedMoment', 'realizedStretch'].some(k =>
    r[k] !== '' && (!Number.isFinite(Number(r[k])) || Number(r[k]) < 0 || Number(r[k]) > 1)) ||
    (r.hasMarketEvidence === 'True' && Number(r.tasteAsOfWeek) >= Number(r.week)));
  const means = {};
  for (const key of ['capability', 'identity', 'moment', 'stretch']) means[key] = shadow.reduce((a, r) => a + Number(r[key]), 0) / shadow.length;
  const byPhase = Object.fromEntries([...new Set(shadow.map(r => r.phase))].map(p => [p, shadow.filter(r => r.phase === p).length]));
  const omitted = budget.reduce((a, r) => a + Number(r.omitted), 0);
  const invalidBudget = budget.some(r => Number(r.written) > Number(r.rowLimit)) || budget.reduce((a, r) => a + Number(r.written), 0) !== shadow.length;
  const item = { seed, existingStreams: existing.length, byteIdentical: mismatches.length === 0, mismatches,
    shadowRows: shadow.length, omitted, byPhase, invalidRows: invalid.length, invalidBudget,
    inferredTaxonomyShare: shadow.filter(r => r.inferredTaxonomy === 'True').length / shadow.length,
    marketEvidenceShare: shadow.filter(r => r.hasMarketEvidence === 'True').length / shadow.length, means };
  summary.push(item);
  if (!existing.length || mismatches.length || !shadow.length || invalid.length || invalidBudget) process.exitCode = 1;
}
fs.writeFileSync(path.join(directory, 'polar-shadow-summary.json'), JSON.stringify(summary, null, 2) + '\n');
console.log(JSON.stringify(summary, null, 2));
