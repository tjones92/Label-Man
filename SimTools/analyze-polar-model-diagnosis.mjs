// Read-only analysis of retained censuses and authored tables; no simulation or tuning.
import fs from 'node:fs';
import crypto from 'node:crypto';
const inputs = ['Data/PolarSongTable.json', 'Data/GenreCatalog.cs', 'SimLogs/polar-census209-analysis.json'];
const table = JSON.parse(fs.readFileSync(inputs[0], 'utf8'));
const genres = [...fs.readFileSync(inputs[1], 'utf8').matchAll(/Add\("([^"]+)", Genre\.(\w+), GenreFamily\.(\w+),/g)]
  .map(([, id, genre, family]) => ({ id, genre, family }));
const census = JSON.parse(fs.readFileSync(inputs[2], 'utf8'));
const focus = ['BossaNova', 'ContemporaryFolk', 'Country', 'Folk', 'Gospel', 'Jazz', 'RnB', 'SurfRock', 'EasyListening'];
const sourceAudit = genres.map(g => {
  const prior = table.genreOverrides[g.genre] ?? table.families[g.family];
  const rows = year => table.archetypes.filter(r => r.fromYear <= year && r.families.includes(g.family)).map(r => r.name);
  return { ...g, fallback: prior.fallback, explicitGenrePrior: !!table.genreOverrides[g.genre], rows1960: rows(1960), rows1963: rows(1963) };
});
const shares = [];
const shortages = [];
for (const run of census.results) {
  for (const population of ['unsigned', 'all']) {
    for (const genre of focus) {
      const m = run.table.find(m => m.population === population && m.genre === genre && m.period === '1960-63');
      if (!m) continue;
      const slots = m.filled + m.originals;
      const songbooks = (m.categories.standard ?? 0) + (m.categories.traditional ?? 0);
      shares.push({ seed: run.seed, mode: run.mode, population, genre, sets: m.sets, slots, coverSlots: m.filled,
        songbookSlots: songbooks, originals: m.originals, categories: m.categories,
        songbookPct: 100 * songbooks / slots, originalPct: 100 * m.originals / slots,
        songbookCoverPct: 100 * songbooks / m.filled, idTieCoverPct: run.mode === 'on' ? 100 * m.idTieSelected / m.filled : null,
        top10CoverPct: 100 * m.top10Share, topSongs: m.topSongs.slice(0, 3) });
    }
  }
  for (const genre of ['LatinPop', 'TexMex', 'Classical', 'Comedy', 'Childrens']) {
    const months = run.table.filter(m => m.population === 'unsigned' && m.genre === genre && /^196[0-3]-(?:0[1-9]|1[0-2])$/.test(m.period))
      .sort((a, b) => a.period.localeCompare(b.period));
    shortages.push({ seed: run.seed, mode: run.mode, genre, months: months.map(m => ({ period: m.period, sets: m.sets,
      meanEligible: Object.values(m.pool).reduce((n, p) => n + p.count, 0) / m.sets, empty: m.emptyPoolSets,
      short: m.shortSets, shortWithEnoughPool: m.shortWithEnoughPoolSets })) });
  }
}
// Illustrative arithmetic, not a measured artist, a C# replay, or proposed calibration.
const row = table.archetypes.find(r => r.name === 'CountryTwoBeat');
const act = [...Array(6).fill(.65), ...table.families.Country.identity];
const illustrativeFit = delta => {
  const axes = row.axes.map((v, i) => i < 6 ? v + delta : v);
  const reach = .5 * row.plasticity;
  for (let i = 6; i < 10; i++) axes[i] += Math.max(-reach, Math.min(reach, act[i] - axes[i]));
  let penalty = 0, dominant = 0;
  for (let i = 0; i < 6; i++) {
    if (axes[i] > axes[dominant]) dominant = i;
    penalty += axes[i] * Math.max(0, axes[i] - act[i]) ** 2 * table.numbers.capabilityPenalty;
  }
  const capability = Math.min(1, Math.exp(-penalty) + Math.max(0, act[dominant] - axes[dominant]) * table.numbers.headroomBonus);
  const distance = Math.sqrt(table.identityWeights.reduce((n, w, i) => n + w * (axes[6 + i] - act[6 + i]) ** 2, 0)
    / table.identityWeights.reduce((a, b) => a + b, 0));
  const identity = Math.max(0, Math.min(1, 1 - distance * (table.numbers.identityBasePenalty + .5 * table.numbers.rigidityPenalty)));
  const moment = table.numbers.neutralMoment;
  return { demandDelta: delta, axes, capability, identity, moment,
    score: capability * table.numbers.selectionCapability + identity * table.numbers.selectionIdentity + moment * table.numbers.selectionMoment };
};
const output = { scope: 'Retained 209-week census reanalysis and static table coverage; illustrative arithmetic only; no gameplay edits.',
  hashes: Object.fromEntries(inputs.map(p => [p, crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')])),
  sourceAudit, shares, shortages, plateauIllustration: [illustrativeFit(0), illustrativeFit(.04)] };
fs.writeFileSync('SimLogs/polar-model-diagnosis.json', JSON.stringify(output, null, 2) + '\n');
console.log(JSON.stringify({ archetypes: table.archetypes.length, explicitGenrePriors: Object.keys(table.genreOverrides).length,
  genres: genres.length, coverage: sourceAudit.filter(g => focus.includes(g.genre) || ['Classical','LatinPop','TexMex','Comedy','Childrens'].includes(g.genre)),
  plateau: output.plateauIllustration, shares: shares.filter(r => r.seed === 1001 && r.mode === 'on' && r.population === 'unsigned') }, null, 2));
