import fs from 'node:fs';
import path from 'node:path';

const directory = process.argv[2] ?? 'SimLogs';
const prefixes = process.argv.slice(3).length ? process.argv.slice(3) : ['polar-selection-off', 'polar-selection-on', 'polar-cause-realization', 'polar-cause-promotion', 'polar-rock-calibrated'];
const catalog = fs.readFileSync('Data/GenreCatalog.cs', 'utf8');
const rockGenres = new Set([...catalog.matchAll(/Add\("[^"]+", Genre\.(\w+), GenreFamily\.Rock,/g)].map(m => m[1]));
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
  const lines = fs.readFileSync(path.join(directory, `${run}-${suffix}.csv`), 'utf8').trim().split(/\r?\n/), headers = cells(lines.shift());
  return lines.filter(Boolean).map(line => Object.fromEntries(cells(line).map((v, i) => [headers[i], v])));
}
const sum = (rows, key) => rows.reduce((total, r) => total + +r[key], 0);
const mean = (rows, key) => rows.length ? sum(rows, key) / rows.length : 0;
const unique = rows => [...new Map(rows.map(r => [r.recordId, r])).values()];
function describe(run, seed, prefix) {
  const complete = fs.readFileSync(path.join(directory, run + '.log'), 'utf8').match(new RegExp(`CHART_AUDIT_COMPLETE run=${run} weeks=(\\d+)`));
  if (!complete) throw new Error('Incomplete run: ' + run);
  const projects = unique(csv(run, 'artist-project-identity'));
  const projectMap = new Map(projects.map(r => [r.recordId, r]));
  const materials = unique(csv(run, 'song-material'));
  const lanes = unique(csv(run, 'single-release-lanes'));
  const laneMap = new Map(lanes.map(r => [r.recordId, r]));
  const capacity = csv(run, 'release-capacity');
  const finance = csv(run, 'label-finance');
  const lastWeek = Math.max(...capacity.map(r => +r.week));
  const ending = finance.filter(r => +r.week === lastWeek);
  const weeks = csv(run, 'weeks');
  const rare = r => ['CoverStandard', 'CoverCatalogSong', 'TraditionalPublicDomain', 'AdaptedTraditional'].includes(r.songSource);
  const groups = {};
  for (const r of materials) {
    const p = projectMap.get(r.recordId);
    const genre = p?.currentArtistGenre ?? r.genre;
    const lane = laneMap.get(r.recordId)?.releaseLane ?? 'Unknown';
    for (const key of [genre, `${genre}:${lane}`, ...(rockGenres.has(genre) ? ['RockFamily', `RockFamily:${lane}`] : [])]) {
      const g = groups[key] ??= { total: 0, standards: 0, traditional: 0, sources: {}, projectGenres: {} };
      g.total++;
      if (['CoverStandard', 'CoverCatalogSong'].includes(r.songSource)) g.standards++;
      if (['TraditionalPublicDomain', 'AdaptedTraditional'].includes(r.songSource)) g.traditional++;
      g.sources[r.songSource] = (g.sources[r.songSource] ?? 0) + 1;
      if (rare(r)) g.projectGenres[r.genre] = (g.projectGenres[r.genre] ?? 0) + 1;
    }
  }
  for (const g of Object.values(groups)) g.rareShare = (g.standards + g.traditional) / g.total;
  const statusCounts = {};
  for (const r of ending) statusCounts[r.status] = (statusCounts[r.status] ?? 0) + 1;
  const liveLanes = lanes.filter(r => r.releaseLane !== 'ExternalOrLegacy');
  const quarters = [1, 2, 3, 4].map(q => {
    const lo = (q - 1) * 13, hi = q * 13;
    const c = capacity.filter(r => +r.week > lo && +r.week <= hi);
    const f = finance.filter(r => +r.week > lo && +r.week <= hi);
    return { quarter: q, rolls: sum(c, 'releaseRollsFired'), releases: sum(c, 'successfulReleases'),
      weeklyNetSum: sum(f, 'weeklyNet'), units: sum(weeks.filter(r => +r.week > lo && +r.week <= hi), 'totalMarketUnits') };
  });
  return { run, seed, prefix, weeks: +complete[1],
    singles: projects.filter(r => r.format === 'Single').length, albums: projects.filter(r => r.format === 'Album').length,
    rolls: sum(capacity, 'releaseRollsFired'), successfulReleases: sum(capacity, 'successfulReleases'),
    failures: sum(capacity, 'failedReleaseRolls'), cooldownMismatches: sum(capacity, 'cooldownMismatchRolls'),
    otherFailures: sum(capacity, 'otherFailedRolls'),
    meanLiveHook: mean(liveLanes, 'hookStrength'), meanLiveProduction: mean(liveLanes, 'productionQuality'), meanLiveQuality: mean(liveLanes, 'quality'),
    marketUnits: sum(weeks, 'totalMarketUnits'), weeklyNetSum: sum(finance, 'weeklyNet'), endingStatus: statusCounts,
    sourceGroups: groups, quarters };
}
const results = [];
for (const prefix of prefixes) for (const seed of [1001, 1002]) results.push(describe(`${prefix}-${seed}`, seed, prefix));
for (const r of results) {
  if (r.weeks !== 52) throw new Error('Calibration comparison requires the matched 52-week window: ' + r.run);
  if (r.prefix !== 'polar-rock-calibrated') continue;
  const original = `polar-shadow-control-${r.seed}`, disabled = `polar-rock-off-${r.seed}`;
  if (!fs.readFileSync(path.join(directory, disabled + '.log'), 'utf8').includes(`CHART_AUDIT_COMPLETE run=${disabled} weeks=52`)) throw new Error('Incomplete disabled control: ' + disabled);
  const streams = fs.readdirSync(directory).filter(f => f.startsWith(original + '-') && f.endsWith('.csv'));
  const mismatches = streams.filter(f => !fs.readFileSync(path.join(directory, f)).equals(fs.readFileSync(path.join(directory, disabled + f.slice(original.length)))));
  r.disabledNeutrality = { streams: streams.length, mismatches };
  const fit = csv(r.run, 'polar-fit'), budget = csv(r.run, 'polar-fit-budget');
  const invalid = fit.filter(row => ['capability', 'identity', 'moment', 'deficit', 'stretch', 'referenceIdentityDistance', 'realizedCapability', 'realizedIdentity', 'realizedMoment', 'realizedStretch'].some(k =>
    row[k] !== '' && (!Number.isFinite(+row[k]) || +row[k] < 0 || +row[k] > 1)) || (row.hasMarketEvidence === 'True' && +row.tasteAsOfWeek >= +row.week));
  const invalidBudget = budget.some(row => +row.written > +row.rowLimit) || sum(budget, 'written') !== fit.length;
  r.fitAudit = { rows: fit.length, invalidRows: invalid.length, invalidBudget };
  const rareShare = r.sourceGroups.RockFamily?.rareShare;
  r.rareRockPassed = Number.isFinite(rareShare) && rareShare > 0 && rareShare < .02;
  if (!streams.length || mismatches.length || !fit.length || invalid.length || invalidBudget || !r.rareRockPassed) process.exitCode = 1;
}
fs.writeFileSync(path.join(directory, 'polar-calibration-summary.json'), JSON.stringify(results, null, 2) + '\n');
console.log(JSON.stringify(results.map(r => ({ run: r.run, singles: r.singles, albums: r.albums,
  releaseRolls: r.rolls, failedRolls: r.failures, meanLiveQuality: r.meanLiveQuality,
  rockAndRollShare: r.sourceGroups.RockAndRoll?.rareShare, rockFamilyShare: r.sourceGroups.RockFamily?.rareShare,
  disabledNeutrality: r.disabledNeutrality, fitAudit: r.fitAudit, rareRockPassed: r.rareRockPassed })), null, 2));
