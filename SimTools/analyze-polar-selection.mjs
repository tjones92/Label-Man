import fs from 'node:fs';
import path from 'node:path';

const directory = process.argv[2] ?? 'SimLogs';
function cells(line) {
  const result = []; let value = '', quoted = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (quoted) { if (c === '"' && line[i + 1] === '"') { value += c; i++; } else if (c === '"') quoted = false; else value += c; }
    else if (c === '"') quoted = true;
    else if (c === ',') { result.push(value); value = ''; }
    else value += c;
  }
  result.push(value); return result;
}
function csv(run, suffix) {
  const lines = fs.readFileSync(path.join(directory, `${run}-${suffix}.csv`), 'utf8').trim().split(/\r?\n/);
  const header = cells(lines.shift());
  return lines.filter(Boolean).map(line => Object.fromEntries(cells(line).map((v, i) => [header[i], v])));
}
function describe(run) {
  const rows = csv(run, 'song-material');
  const projects = [...new Map(csv(run, 'artist-project-identity').map(r => [r.recordId, r])).values()];
  const sources = {}, genres = {}, assignments = {};
  const projectMap = new Map(projects.map(r => [r.recordId, r]));
  for (const r of rows) {
    sources[r.songSource] = (sources[r.songSource] ?? 0) + 1;
    const genre = projectMap.get(r.recordId)?.currentArtistGenre ?? r.genre;
    const g = genres[genre] ??= { materials: 0, standardsTraditional: 0 };
    g.materials++;
    if (['CoverStandard', 'TraditionalPublicDomain', 'AdaptedTraditional'].includes(r.songSource)) g.standardsTraditional++;
    const key = `${r.songSource} -> ${genre}`;
    assignments[key] = (assignments[key] ?? 0) + 1;
  }
  for (const g of Object.values(genres)) g.share = g.standardsTraditional / g.materials;
  return { materialReleases: rows.length, singles: projects.filter(r => r.format === 'Single').length,
    albums: projects.filter(r => r.format === 'Album').length, sources, genres, assignments };
}
const files = fs.readdirSync(directory), results = [];
for (const seed of [1001, 1002]) {
  const baseline = `polar-shadow-control-${seed}`, off = `polar-selection-off-${seed}`, on = `polar-selection-on-${seed}`;
  for (const run of [baseline, off, on]) {
    if (!fs.readFileSync(path.join(directory, run + '.log'), 'utf8').includes(`CHART_AUDIT_COMPLETE run=${run} weeks=52`)) throw new Error('Incomplete run: ' + run);
  }
  const streams = files.filter(f => f.startsWith(baseline + '-') && f.endsWith('.csv'));
  const mismatches = streams.filter(f => !fs.readFileSync(path.join(directory, f)).equals(fs.readFileSync(path.join(directory, off + f.slice(baseline.length)))));
  const fit = csv(on, 'polar-fit'), budgets = csv(on, 'polar-fit-budget');
  const invalid = fit.filter(r => ['capability', 'identity', 'moment', 'deficit', 'stretch', 'realizedCapability', 'realizedIdentity', 'realizedMoment', 'realizedStretch'].some(k =>
    r[k] !== '' && (!Number.isFinite(+r[k]) || +r[k] < 0 || +r[k] > 1)) || (r.hasMarketEvidence === 'True' && +r.tasteAsOfWeek >= +r.week));
  const invalidBudget = budgets.some(r => +r.written > +r.rowLimit) || budgets.reduce((sum, r) => sum + +r.written, 0) !== fit.length;
  const selected = fit.filter(r => r.phase === 'selected' || r.phase === 'forced');
  const means = Object.fromEntries(['realizedCapability', 'realizedIdentity', 'realizedMoment', 'realizedStretch'].map(k => [k, selected.reduce((sum, r) => sum + +r[k], 0) / selected.length]));
  const control = describe(off), candidate = describe(on);
  results.push({ seed, weeks: 52, disabledStreams: streams.length, disabledByteIdentical: mismatches.length === 0, mismatches,
    invalidFitRows: invalid.length, invalidBudget, selectedObserved: selected.length, selectedDiagnosticMeans: means,
    singleReleaseDelta: candidate.singles - control.singles, singleReleaseChange: candidate.singles / control.singles - 1,
    control, candidate });
  if (!streams.length || mismatches.length || !fit.length || invalid.length || invalidBudget) process.exitCode = 1;
}
fs.writeFileSync(path.join(directory, 'polar-selection-summary.json'), JSON.stringify(results, null, 2) + '\n');
console.log(JSON.stringify(results.map(({ control, candidate, ...r }) => ({ ...r,
  controlSingles: control.singles, candidateSingles: candidate.singles,
  controlAlbums: control.albums, candidateAlbums: candidate.albums })), null, 2));
