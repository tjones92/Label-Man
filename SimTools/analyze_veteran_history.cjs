// Usage: node SimTools/analyze_veteran_history.cjs
// The opening 1959-12-28 chart is counted in 1960, matching simulation week zero.
const fs = require('fs');
const readline = require('readline');
function parse(line) {
  return (line.match(/("(?:[^"]|"")*"|[^,]*)(,|$)/g) || []).map(field => {
    field = field.replace(/,$/, '');
    return field[0] === '"' ? field.slice(1, -1).replace(/""/g, '"') : field;
  }).slice(0, -1);
}
async function read(file, visit) {
  let header;
  for await (const line of readline.createInterface({input: fs.createReadStream(file), crlfDelay: Infinity})) {
    if (!header) { header = parse(line); continue; }
    const values = parse(line);
    visit(Object.fromEntries(header.map((key, i) => [key, values[i]])));
  }
}
const normalize = name => name.toLowerCase().replace(/^the\s+/, '').replace(/&/g, 'and').replace(/[^a-z0-9]/g, '');
(async () => {
  const rows = [], first = new Map();
  await read('SimLogs/historical-hot100-1960-64/hot100_weekly.csv', r => {
    const year = Math.max(1960, +r.chartDate.slice(0, 4)), artist = normalize(r.artist);
    first.set(artist, Math.min(first.get(artist) ?? year, year));
    rows.push({...r, year, artist});
  });
  const annual = {};
  for (const r of rows) {
    const a = annual[r.year] ??= {slots:0, oldSlots:0, newSlots:0, top:0, oldTop:0};
    const cohort = first.get(r.artist);
    a.slots++;
    if (cohort === 1960) a.oldSlots++;
    if (cohort >= r.year - 1) a.newSlots++;
    if (+r.position <= 10) { a.top++; if (cohort === 1960) a.oldTop++; }
  }
  const top1968 = {slots:0, artistsSeen1960to1964:0};
  await read('SimLogs/historical-top10-1968/records_1968_grouped.csv', r => {
    top1968.slots += +r.slots;
    if (first.has(normalize(r.artist))) top1968.artistsSeen1960to1964 += +r.slots;
  });
  console.log(JSON.stringify({annual, top1968}, null, 2));
})().catch(e => {console.error(e); process.exitCode = 1});
