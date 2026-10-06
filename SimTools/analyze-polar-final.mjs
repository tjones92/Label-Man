import fs from 'node:fs';
import path from 'node:path';

const directory = process.argv[2] ?? 'SimLogs';
const prefix = process.argv[3] ?? 'polar-final-verified';
const genreCatalog = fs.readFileSync('Data/GenreCatalog.cs','utf8');
const rockGenres = new Set([...genreCatalog.matchAll(/Add\("[^"]+", Genre\.(\w+), GenreFamily\.Rock,/g)].map(m=>m[1]));
function csv(run, suffix) {
  const lines = fs.readFileSync(path.join(directory, `${run}-${suffix}.csv`), 'utf8').trim().split(/\r?\n/);
  const headers = lines.shift().split(',');
  return lines.filter(Boolean).map(line => {
    const cells = []; let value = '', quoted = false;
    for (let i=0;i<line.length;i++) {
      const c=line[i];
      if (quoted) { if (c==='"' && line[i+1]==='"') {value+='"';i++;} else if(c==='"') quoted=false; else value+=c; }
      else if(c==='"') quoted=true;
      else if(c===',') {cells.push(value);value='';}
      else value+=c;
    }
    cells.push(value);
    return Object.fromEntries(cells.map((v,i)=>[headers[i],v]));
  });
}
const sum = (rows, key) => rows.reduce((n, r) => n + Number(r[key]), 0);
function totals(rows) {
  const qualityCount = sum(rows, 'newSingleCount');
  return { weeks: rows.length, directSingles: sum(rows, 'singles'), albums: sum(rows, 'albums'),
    observedNewSingles: qualityCount,
    meanNewSingleQuality: rows.reduce((n, r) => n + +r.newSingleCount * +r.meanNewSingleQuality, 0) / qualityCount,
    marketUnits: sum(rows, 'marketUnits'), labelNet: sum(rows, 'labelNet'),
    endingCash: +rows.at(-1).endingCash, dyingLabels: +rows.at(-1).dyingLabels, bankruptLabels: +rows.at(-1).bankruptLabels,
    fittedSingleMasters: sum(rows, 'newFittedMasters'), invalidFit: sum(rows, 'invalidFit') };
}
function census(rows) {
  const result = {};
  for (const r of rows) {
    const g = result[r.genre] ??= {sets:0,slots:0,originals:0,standards:0,traditional:0,recentCovers:0,shortSets:0,duplicateCovers:0};
    for (const key of Object.keys(g)) g[key] += +r[key];
    if (+r.slots !== +r.originals + +r.standards + +r.traditional + +r.recentCovers) throw Error('Unclassified census slots');
  }
  for (const g of Object.values(result)) {
    g.songbookSlots = g.standards + g.traditional;
    g.songbookShare = g.slots ? g.songbookSlots / g.slots : null;
    g.coverShare = g.slots > g.originals ? g.songbookSlots / (g.slots - g.originals) : null;
  }
  return result;
}
const results = [];
for (const seed of [1001,1002]) for (const mode of ['off','on']) {
  const run = `${prefix}-${mode}-${seed}`;
  const log = fs.readFileSync(path.join(directory, run + '.log'), 'utf8');
  if (!log.includes(`CHART_AUDIT_COMPLETE run=${run} weeks=521`) || !log.includes('POLAR_FINAL_DATE date=12/31/1969')) throw Error('Incomplete decade: ' + run);
  const invocation = JSON.parse(fs.readFileSync(path.join(directory, run + '-invocation.json')));
  if ((mode === 'off') !== invocation.arguments.includes('--disable-polar-fit-selection')) throw Error('Wrong startup flag: ' + run);
  if (mode === 'on' && invocation.arguments.includes('--use-polar-fit-selection')) throw Error('Enabled run must verify default startup: '+run);
  if (!log.includes(`POLAR_FINAL_START date=1/1/1960 enabled=${mode === 'on' ? 'True' : 'False'}`)) throw Error('Unexpected initial state: '+run);
  const weekly = csv(run, 'polar-final-weekly');
  if (weekly.length !== 521 || weekly.some((r,i) => +r.week !== i+1) || weekly.at(-1).date !== '12/26/1969') throw Error('Missing weekly observation: ' + run);
  if (weekly.some(r=>['singles','albums','newSingleCount','marketUnits','dyingLabels','bankruptLabels','newFittedMasters'].some(k=>!Number.isFinite(+r[k]) || +r[k]<0) ||
    ['labelNet','endingCash'].some(k=>!Number.isFinite(+r[k])) || !Number.isFinite(+r.meanNewSingleQuality) || +r.meanNewSingleQuality<0 || +r.meanNewSingleQuality>1)) throw Error('Invalid economic/quality series: '+run);
  const live = csv(run, 'polar-live-census');
  const months = [...new Set(live.map(r => `${r.year}-${r.month}`))];
  if (months.length !== 48 || live.some(r => +r.year < 1960 || +r.year > 1963)) throw Error('Incomplete live-set census: ' + run);
  if (live.some(r => +r.duplicateCovers !== 0)) throw Error('Duplicate census covers: ' + run);
  const annual = {};
  for (const year of [...new Set(weekly.map(r => r.year))]) annual[year] = totals(weekly.filter(r => r.year === year));
  const summary = totals(weekly);
  if (summary.invalidFit || (mode === 'on' && !summary.fittedSingleMasters)) throw Error('Invalid fit: ' + run);
  const genreShape = csv(run,'genre-decade-shape');
  if (new Set(genreShape.map(r=>r.year)).size !== 10 || genreShape.some(r=>+r.year<1960 || +r.year>1969)) throw Error('Missing genre years: '+run);
  if (genreShape.some(r=>['newReleases','marketUnits','chartRecordWeeks','uniqueChartingRecords'].some(k=>!Number.isFinite(+r[k]) || +r[k]<0) ||
    ['marketUnitsShare','chartWeekShare','chartUnitsShare'].some(k=>!Number.isFinite(+r[k]) || +r[k]<0 || +r[k]>1))) throw Error('Invalid genre shape: '+run);
  const genreYears = Object.fromEntries([...new Set(genreShape.map(r=>r.year))].map(year=>{
    const rows=genreShape.filter(r=>r.year===year), share=sum(rows,'marketUnitsShare');
    if(Math.abs(share-1)>.005) throw Error('Unnormalized genre shares: '+run+' '+year);
    return [year,{observedGenres:rows.length,genresWithReleases:rows.filter(r=>+r.newReleases>0).length,
      genresWithMarketUnits:rows.filter(r=>+r.marketUnits>0).length,marketShareSum:share,
      rows:rows.map(({genre,family,newReleases,marketUnits,marketUnitsShare,uniqueChartingRecords})=>
        ({genre,family,newReleases:+newReleases,marketUnits:+marketUnits,marketUnitsShare:+marketUnitsShare,uniqueChartingRecords:+uniqueChartingRecords}))}];
  }));
  results.push({run,seed,mode,start:'1960-01-01',end:'1969-12-31',lastSettlementObservation:weekly.at(-1).date,
    invocation, summary, annual, genreYears, censusMonths:months.length, census:census(live),
    rockCensus:census(live.filter(r=>rockGenres.has(r.genre)).map(r=>({...r,genre:'RockFamily'}))).RockFamily,
    censusByYear:Object.fromEntries([1960,1961,1962,1963].map(y => [y,census(live.filter(r => +r.year === y))]))});
}
for (const key of ['assemblySha256','tableSha256','perceptionTableSha256']) {
  if (new Set(results.map(r=>r.invocation[key])).size !== 1) throw Error('Unmatched fingerprints: '+key);
}
const comparisons = [1001,1002].map(seed => {
  const off = results.find(r => r.seed === seed && r.mode === 'off');
  const on = results.find(r => r.seed === seed && r.mode === 'on');
  const change = key => on.summary[key] / off.summary[key] - 1;
  const censusComparisons = Object.fromEntries([...new Set([...Object.keys(off.census), ...Object.keys(on.census)])].sort().map(genre => {
    const before = off.census[genre], after = on.census[genre];
    return [genre,{off:before,on:after,delta: before?.songbookShare != null && after?.songbookShare != null ? after.songbookShare - before.songbookShare : null}];
  }));
  return {seed,observedSingleChange:change('observedNewSingles'),directSingleChange:change('directSingles'),albumChange:change('albums'),
    qualityDelta:on.summary.meanNewSingleQuality-off.summary.meanNewSingleQuality,marketUnitsChange:change('marketUnits'),labelNetChange:change('labelNet'),
    endingCashChange:change('endingCash'),censusComparisons,
    rockCensus:{off:off.rockCensus,on:on.rockCensus,delta:on.rockCensus.songbookShare-off.rockCensus.songbookShare},
    // Prior one-year engineering guardrails are reference checks, not new historical targets.
    referenceGuardrails:{singleVolume:Math.abs(change('observedNewSingles'))<=.03,
      quality:on.summary.meanNewSingleQuality-off.summary.meanNewSingleQuality>=-.02,labelNet:change('labelNet')>=-.05},
    annual:Object.fromEntries(Object.keys(off.annual).map(year => [year,{
      off:off.annual[year],on:on.annual[year],singleChange:on.annual[year].observedNewSingles/off.annual[year].observedNewSingles-1,
      qualityDelta:on.annual[year].meanNewSingleQuality-off.annual[year].meanNewSingleQuality,
      labelNetChange:on.annual[year].labelNet/off.annual[year].labelNet-1}]))};
});
const output = {date:'2026-10-03',results,comparisons};
fs.writeFileSync(path.join(directory,'polar-final-summary.json'),JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify(comparisons.map(({censusComparisons,annual,...r}) => r),null,2));
