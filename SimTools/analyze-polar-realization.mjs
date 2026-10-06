import fs from 'node:fs';
import path from 'node:path';

const directory = process.argv[2] ?? 'SimLogs';
const mode = process.argv[3] ?? 'probes';
if (!['probes', 'initial', 'refinement', 'development', 'validation'].includes(mode)) throw new Error(`Unknown analysis mode: ${mode}`);
const partial = process.argv.includes('--partial');
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
    endingCash: sum(ending, 'cashReserves'), nonPositiveCashLabels: ending.filter(r => +r.cashReserves <= 0).length,
    sourceGroups: groups, quarters };
}
const cases = mode === 'probes' ? ['base', 'no-hook', 'no-capability', 'no-critic', 'no-moment', 'legacy-group'] :
  ['off', mode === 'initial' ? 'candidate' : mode === 'refinement' ? 'refined' : 'calibrated'];
const seeds = mode === 'validation' ? [1011, 1012] : [1001, 1002];
const results = [];
for (const seed of seeds) for (const name of cases) {
  const prefix = `polar-realization-${name}`;
  if (partial && (!fs.existsSync(path.join(directory, `${prefix}-${seed}.log`)) ||
      !fs.readFileSync(path.join(directory, `${prefix}-${seed}.log`), 'utf8').includes(`CHART_AUDIT_COMPLETE run=${prefix}-${seed} weeks=52`))) continue;
  const r = describe(`${prefix}-${seed}`, seed, prefix);
  if (r.weeks !== 52) throw new Error(`Incomplete matched window: ${r.run}`);
  for (const key of ['singles', 'albums', 'meanLiveQuality', 'meanLiveHook', 'meanLiveProduction', 'marketUnits', 'weeklyNetSum', 'endingCash']) {
    if (!Number.isFinite(r[key])) throw new Error(`Non-finite ${key}: ${r.run}`);
  }
  if (name !== 'off') {
    const fit = csv(r.run, 'polar-fit'), budget = csv(r.run, 'polar-fit-budget');
    const components = ['capability', 'identity', 'moment', 'deficit', 'stretch', 'referenceIdentityDistance',
      'realizedCapability', 'realizedIdentity', 'realizedMoment', 'realizedStretch'];
    const invalid = fit.filter(row => components.some(k => row[k] !== '' && (!Number.isFinite(+row[k]) || +row[k] < 0 || +row[k] > 1)) ||
      (row.hasMarketEvidence === 'True' && +row.tasteAsOfWeek >= +row.week));
    r.fitAudit = { rows: fit.length, invalid: invalid.length,
      invalidBudget: budget.some(row => +row.written > +row.rowLimit) || sum(budget, 'written') !== fit.length };
    if (!fit.length || invalid.length || r.fitAudit.invalidBudget) throw new Error(`Invalid fit audit: ${r.run}`);
  }
  const baseline = name === cases[0] ? r : results.find(b => b.seed === seed && b.prefix.endsWith(cases[0]));
  r.delta = {};
  for (const k of ['singles', 'albums', 'rolls', 'meanLiveQuality', 'meanLiveHook', 'meanLiveProduction', 'marketUnits', 'weeklyNetSum', 'endingCash']) {
    r.delta[k] = r[k] - baseline[k];
    r.delta[k + 'Percent'] = baseline[k] ? (r[k] / baseline[k] - 1) * 100 : null;
  }
  results.push(r);
}
const summary = results.map(r => ({ run: r.run, singles: r.singles, albums: r.albums, rolls: r.rolls,
  hook: r.meanLiveHook, production: r.meanLiveProduction, quality: r.meanLiveQuality,
  units: r.marketUnits, net: r.weeklyNetSum, cash: r.endingCash, status: r.endingStatus,
  rockShare: r.sourceGroups.RockFamily?.rareShare, delta: r.delta, fitAudit: r.fitAudit }));
if (mode !== 'probes') {
  // Review guardrails chosen before holdouts: ±3% singles, ≤.02 quality loss,
  // ≤5% net loss, and rare (nonzero pooled, <2% per seed) rock songbook sourcing.
  const enabled = results.filter(r => r.prefix.endsWith(cases[1]));
  const rareCount = enabled.reduce((n, r) => n + (r.sourceGroups.RockFamily?.standards ?? 0) + (r.sourceGroups.RockFamily?.traditional ?? 0), 0);
  for (const r of enabled) {
    r.guardrails = { releaseVolume: Math.abs(r.delta.singlesPercent) <= 3,
      quality: r.delta.meanLiveQuality >= -.02, labelNet: r.delta.weeklyNetSumPercent >= -5,
      rareRock: rareCount > 0 && r.sourceGroups.RockFamily?.rareShare < .02 };
    if (Object.values(r.guardrails).some(v => !v)) process.exitCode = 1;
    summary.find(s => s.run === r.run).guardrails = r.guardrails;
  }
}
fs.writeFileSync(path.join(directory, `polar-realization-${mode}${partial ? '-partial' : ''}-summary.json`), JSON.stringify(results, null, 2) + '\n');
console.log(JSON.stringify(summary, null, 2));
