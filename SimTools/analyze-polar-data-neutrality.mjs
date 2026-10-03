import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

// Exact files, not numeric tolerances: the data slice must preserve every existing CSV byte.
const directory = process.argv[2] ?? 'SimLogs';
const seeds = [1001, 1002];
const files = readdirSync(directory);
const result = [];
for (const seed of seeds) {
  const controlPrefix = `polar-data-control-${seed}-`;
  const candidatePrefix = `polar-data-candidate-${seed}-`;
  const controls = files.filter(f => f.startsWith(controlPrefix) && f.endsWith('.csv')).sort();
  const candidates = files.filter(f => f.startsWith(candidatePrefix) && f.endsWith('.csv')).sort();
  const mismatches = [];
  if (!controls.length || controls.length !== candidates.length) mismatches.push('CSV stream count');
  for (const file of controls) {
    const candidate = candidatePrefix + file.slice(controlPrefix.length);
    if (!candidates.includes(candidate) || !readFileSync(join(directory, file)).equals(readFileSync(join(directory, candidate))))
      mismatches.push(file.slice(controlPrefix.length));
  }
  for (const [kind, prefix] of [['control', controlPrefix], ['candidate', candidatePrefix]]) {
    const run = prefix.slice(0, -1);
    const log = readFileSync(join(directory, `${run}.log`), 'utf8');
    if (!log.includes(`CHART_AUDIT_COMPLETE run=${run} weeks=52`)) mismatches.push(`${kind} incomplete`);
  }
  result.push({ seed, streams: controls.length, byteIdentical: mismatches.length === 0, mismatches });
}
writeFileSync(join(directory, 'polar-data-neutrality.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (result.some(r => !r.byteIdentical)) process.exitCode = 1;
