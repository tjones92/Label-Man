// Usage: node SimTools/analyze_veteran_chart.cjs <run-prefix> [<run-prefix> ...]
// Cohorts are first observed Hot 100 year, including the left-censored opening chart.
const fs = require('fs');
const readline = require('readline');
function parse(line) {
  const fields = (line.match(/("(?:[^"]|"")*"|[^,]*)(,|$)/g) || []).map(field => {
    field = field.replace(/,$/, '');
    return field[0] === '"' ? field.slice(1, -1).replace(/""/g, '"') : field;
  }).slice(0, -1);
  if (line.endsWith(',')) fields.push('');
  return fields;
}
async function read(file, visit) {
  let header;
  for await (const line of readline.createInterface({input: fs.createReadStream(file), crlfDelay: Infinity})) {
    if (!header) { header = parse(line); continue; }
    const values = parse(line);
    if (values.length !== header.length) { console.error(`Skipping incomplete row in ${file}`); continue; }
    visit(Object.fromEntries(header.map((key, i) => [key, values[i]])));
  }
}
async function analyze(prefix) {
  const first = new Map(), yearly = new Map(), topRecords = new Map();
  await read(`SimLogs/${prefix}-records.csv`, r => {
    const pos = +r.currentPosition, year = +r.year;
    if (pos < 1 || pos > 100) return;
    first.set(r.artistId, Math.min(first.get(r.artistId) ?? year, year));
    const key = `${year}|${r.artistId}`;
    let item = yearly.get(key) || {year, artist: r.artistId, slots: 0, top: 0};
    item.slots++; if (pos <= 10) item.top++;
    yearly.set(key, item);
    if ((year === 1963 || year === 1964) && pos <= 10)
      topRecords.set(r.recordId, (topRecords.get(r.recordId) || 0) + 1);
  });
  const annual = {};
  for (const x of yearly.values()) {
    const cohort = first.get(x.artist);
    const a = annual[x.year] ??= {slots:0, top:0, oldSlots:0, newSlots:0, oldTop:0, newTop:0, earlySlots:0, earlyTop:0};
    a.slots += x.slots; a.top += x.top;
    if (cohort === 1960) { a.oldSlots += x.slots; a.oldTop += x.top; }
    if (cohort >= x.year - 1 && cohort <= x.year) { a.newSlots += x.slots; a.newTop += x.top; }
    if (cohort <= 1964) { a.earlySlots += x.slots; a.earlyTop += x.top; }
  }
  const cohort1960 = [...first].filter(([, year]) => year === 1960).map(([artist]) => artist);
  const old1964 = cohort1960.filter(artist => yearly.has(`1964|${artist}`));
  const new1964 = [...first].filter(([, year]) => year === 1963 || year === 1964)
    .map(([artist]) => artist).filter(artist => yearly.has(`1964|${artist}`));
  const survivors = [1961, 1962].map(year => {
    const artists = [...first].filter(([, cohort]) => cohort === year).map(([artist]) => artist);
    return {cohort: year, debutants: artists.length, chartingTwoYearsLater: artists.filter(artist => yearly.has(`${year+2}|${artist}`)).length};
  });
  const weeks = artists => artists.length ? artists.reduce((sum, artist) => sum + yearly.get(`1964|${artist}`).slots, 0) / artists.length : null;
  const cohortMetrics = {cohort1960Size: cohort1960.length, cohort1960Charting1964: old1964.length,
    old1964WeeksPerAct: weeks(old1964), new1964ChartingActs: new1964.length, new1964WeeksPerAct: weeks(new1964), survivors};
  const tier = new Map(), cost = new Map(), advance = new Map();
  await read(`SimLogs/${prefix}-single-launch-quality.csv`, r => tier.set(r.singleId, r.tier));
  await read(`SimLogs/${prefix}-release-campaign-costs.csv`, r => cost.set(r.recordId, +r.marketingCost));
  await read(`SimLogs/${prefix}-following.csv`, r => {
    if (r.kind !== 'launch' || !r.detail.startsWith('Single|')) return;
    const fields = r.detail.split('|');
    const stocked = fields.find(x => x.startsWith('stocked='));
    if (stocked) advance.set(fields.at(-1), +stocked.slice('stocked='.length));
  });
  const groups = {};
  await read(`SimLogs/${prefix}-single-launch-attribution.csv`, r => {
    if (!topRecords.has(r.singleId)) return;
    const cohort = first.get(r.artistId);
    const group = cohort === 1960 ? '1960' : cohort === 1963 || cohort === 1964 ? '1963-64' : '1961-62';
    (groups[group] ??= []).push({...r, slotWeeks: topRecords.get(r.singleId), tier: tier.get(r.singleId), marketingCost: cost.get(r.singleId), advanceStock: advance.get(r.singleId) ?? 0, otherStock: +r.initialStock - (advance.get(r.singleId) ?? 0)});
  });
  const metrics = ['quality','initialStock','initialAwareness','effectiveRecognition','launchCampaignImpact','artistMomentum','artistReputation','marketingCost','advanceStock','otherStock'];
  const launches = {};
  for (const [group, records] of Object.entries(groups)) {
    const counts = key => records.reduce((out, r) => {out[r[key] ?? 'missing'] = (out[r[key] ?? 'missing'] || 0) + r.slotWeeks; return out;}, {});
    const means = Object.fromEntries(metrics.map(key => {
      const values = records.map(r => r[key]).filter(x => x !== undefined && x !== '').map(Number);
      return [key, values.length ? values.reduce((a,b) => a+b,0) / values.length : null];
    }));
    launches[group] = {records: records.length, slotWeeks: records.reduce((a,r)=>a+r.slotWeeks,0), means, slotWeeksByTier:counts('tier'), slotWeeksByOpeningArchetype:counts('openingArchetype')};
  }
  console.log(JSON.stringify({prefix, annual, cohortMetrics, launches}, null, 2));
}
(async () => { if (process.argv.length < 3) throw Error('Supply at least one run prefix'); for (const prefix of process.argv.slice(2)) await analyze(prefix); })().catch(e => {console.error(e); process.exitCode = 1});



