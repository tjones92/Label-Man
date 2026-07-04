import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";

function parseCsvLine(line) {
  const values = []; let value = "", quoted = false;
  for (let i = 0; i < line.length; i++) {
    const char = line[i];
    if (quoted) {
      if (char === '"' && line[i + 1] === '"') { value += '"'; i++; }
      else if (char === '"') quoted = false; else value += char;
    } else if (char === '"') quoted = true;
    else if (char === ",") { values.push(value); value = ""; } else value += char;
  }
  values.push(value); return values;
}
function readCsv(file) {
  const text = fs.readFileSync(file, "utf8").trim(); if (!text) return [];
  const lines = text.split(/\r?\n/), headers = parseCsvLine(lines.shift());
  return lines.filter(Boolean).map(line => { const values = parseCsvLine(line);
    return Object.fromEntries(headers.map((header, i) => [header, values[i] ?? ""])); });
}
const num = value => Number(value ?? 0);
const mean = values => values.length ? values.reduce((a, b) => a + b, 0) / values.length : null;
function percentile(values, fraction) { if (!values.length) return null; const sorted = [...values].sort((a,b)=>a-b);
  const p = (sorted.length - 1) * fraction, lo = Math.floor(p), hi = Math.ceil(p); return sorted[lo] + (sorted[hi] - sorted[lo]) * (p - lo); }
const median = values => percentile(values, 0.5);
function correlation(pairs) { if (pairs.length < 2) return null; const mx=mean(pairs.map(x=>x[0])), my=mean(pairs.map(x=>x[1]));
  let cov=0,vx=0,vy=0; for(const [x,y] of pairs){const dx=x-mx,dy=y-my;cov+=dx*dy;vx+=dx*dx;vy+=dy*dy;} return vx&&vy?cov/Math.sqrt(vx*vy):null; }
const sha256 = file => crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex").toUpperCase();
const adult = new Set(["Jazz","EasyListening","Folk","TraditionalPop","BossaNova","Country"]);
const youth = new Set(["RockAndRoll","TeenPop","RnB","DooWop","GirlGroup"]);

function analyze(directory, run) {
  const file = suffix => path.join(directory, `${run}-${suffix}.csv`);
  const fork = readCsv(file("fork-ratios")), outcomes = readCsv(file("release-outcomes"));
  const live = readCsv(file("live-records-snapshot")), records = readCsv(file("records"));
  const lifecycles = readCsv(file("lifecycles")), albumChart = readCsv(file("album-chart"));
  const costs = readCsv(file("prior-cost-assumptions"));
  const byId = new Map(fork.map(row => [row.recordId, row]));
  const selected = predicate => fork.filter(predicate), albums = selected(row => row.chosenFormat === "Album");
  const adultRows = selected(row => adult.has(row.genre)), youthRows = selected(row => youth.has(row.genre));
  const firstAndPeak = new Map(); let adultSingleChartRows = 0, singleChartRows = 0;
  for (const row of records) {
    if (!firstAndPeak.has(row.recordId)) firstAndPeak.set(row.recordId, { quality:num(row.quality), peak:999 });
    if (num(row.currentPosition) > 0) {
      firstAndPeak.get(row.recordId).peak = Math.min(firstAndPeak.get(row.recordId).peak, num(row.currentPosition));
      singleChartRows++; if (adult.has(row.genre)) adultSingleChartRows++;
    }
  }
  const pearsonPairs = [...firstAndPeak.values()].filter(row=>row.peak<999).map(row=>[row.quality,101-row.peak]);
  const closed40 = lifecycles.filter(row=>num(row.peakPosition)>0&&num(row.peakPosition)<=40).map(row=>num(row.weeksOnChart));
  const completed = outcomes.filter(row=>row.memoryEligible==="true"&&byId.has(row.recordId));
  function errorView(format) {
    const rows=completed.filter(row=>row.format===format), prior=[], projected=[];
    for(const row of rows){const f=byId.get(row.recordId), realized=num(row.realizedNet);
      prior.push(num(format==="Single"?f.priorSingleNet:f.priorAlbumNet)-realized);
      projected.push(num(format==="Single"?f.projectedSingleNet:f.projectedAlbumNet)-realized);}
    const liveRows=live.filter(row=>row.format===format&&byId.has(row.recordId)), ceiling=[...projected];
    for(const row of liveRows){const f=byId.get(row.recordId);ceiling.push(num(format==="Single"?f.projectedSingleNet:f.projectedAlbumNet)-num(row.observedNetLowerBound));}
    return {completedN:rows.length,priorMeanSignedError:mean(prior),projectedMeanSignedError:mean(projected),liveN:liveRows.length,
      lowerBoundSubstitutionCeiling:mean(ceiling),ceilingN:ceiling.length};
  }
  const singleErrorsByBucket=[];
  for(const group of [...new Set(fork.map(row=>`${row.qualityQuartile}|${row.careerBand}`))].sort()){
    const [qualityQuartile,careerBand]=group.split("|"), errors=[];
    for(const row of completed.filter(row=>row.format==="Single")){const f=byId.get(row.recordId);
      if(f.qualityQuartile===qualityQuartile&&f.careerBand===careerBand)errors.push(num(f.projectedSingleNet)-num(row.realizedNet));}
    if(errors.length)singleErrorsByBucket.push({qualityQuartile,careerBand,n:errors.length,meanSignedError:mean(errors)});
  }
  const forkGroups=[];
  for(const key of [...new Set(fork.map(row=>`${row.genreGroup}|${row.careerBand}`))].sort()){
    const [genreGroup,careerBand]=key.split("|"), rows=fork.filter(row=>row.genreGroup===genreGroup&&row.careerBand===careerBand);
    forkGroups.push({genreGroup,careerBand,n:rows.length,meanPriorSingle:mean(rows.map(r=>num(r.priorSingleNet))),meanPriorAlbum:mean(rows.map(r=>num(r.priorAlbumNet))),
      meanProjectedSingle:mean(rows.map(r=>num(r.projectedSingleNet))),meanProjectedAlbum:mean(rows.map(r=>num(r.projectedAlbumNet))),
      meanAlbumMinusSingle:mean(rows.map(r=>num(r.albumMinusSingleNet))),albumChoices:rows.filter(r=>r.chosenFormat==="Album").length,
      albumChoiceShare:rows.filter(r=>r.chosenFormat==="Album").length/rows.length,undefinedRatios:rows.filter(r=>r.albumToSingleRatio==="").length,
      undefinedRatioShare:rows.filter(r=>r.albumToSingleRatio==="").length/rows.length});
  }
  const youthIds=new Set(youthRows.filter(row=>row.chosenFormat==="Album").map(row=>row.recordId));
  const youthCompilation=costs.filter(row=>youthIds.has(row.recordId)&&row.actualAlbumFormat==="Compilation").length;
  const albumCostRows=costs.filter(row=>byId.get(row.recordId)?.chosenFormat==="Album");
  const formatMixPassed = albums.length/fork.length>=.18&&albums.length/fork.length<=.28&&
    adultRows.filter(r=>r.chosenFormat==="Album").length/adultRows.length>=.45&&adultRows.filter(r=>r.chosenFormat==="Album").length/adultRows.length<=.75&&
    youthRows.filter(r=>r.chosenFormat==="Album").length/youthRows.length>=.02&&youthRows.filter(r=>r.chosenFormat==="Album").length/youthRows.length<=.15&&
    albumChart.filter(r=>adult.has(r.genre)).length/albumChart.length>=.95&&adultSingleChartRows/singleChartRows>=.15;
  return {run, decisions:fork.length, formatMix:{albumChoices:albums.length,albumShare:albums.length/fork.length,
    adultAlbumChoices:adultRows.filter(r=>r.chosenFormat==="Album").length,adultDecisions:adultRows.length,adultAlbumShare:adultRows.filter(r=>r.chosenFormat==="Album").length/adultRows.length,
    youthAlbumChoices:youthRows.filter(r=>r.chosenFormat==="Album").length,youthDecisions:youthRows.length,youthAlbumShare:youthRows.filter(r=>r.chosenFormat==="Album").length/youthRows.length,
    youthCompilation,youthAlbums:youthIds.size,adultAlbumChartRows:albumChart.filter(r=>adult.has(r.genre)).length,albumChartRows:albumChart.length,
    adultAlbumChartShare:albumChart.filter(r=>adult.has(r.genre)).length/albumChart.length,adultSingleChartRows,singleChartRows,adultSingleChartShare:adultSingleChartRows/singleChartRows,passed:formatMixPassed},
    singleError:errorView("Single"),albumError:errorView("Album"),singleErrorsByBucket,
    conditionalGuards:{applied:formatMixPassed,livePeakPearson:correlation(pearsonPairs),pearsonN:pearsonPairs.length,closedTop40Median:median(closed40),closedTop40N:closed40.length},
    albumCostAssumptions:{n:albumCostRows.length,assumedCompilation:albumCostRows.filter(r=>r.assumedCompilationCost==="true").length,
      actualCompilation:albumCostRows.filter(r=>r.actualAlbumFormat==="Compilation").length,matches:albumCostRows.filter(r=>(r.assumedCompilationCost==="true")===(r.actualAlbumFormat==="Compilation")).length},
    forkGroups,undefinedRatios:fork.filter(r=>r.albumToSingleRatio==="").length,
    hashes:{marketRevenue:sha256(file("market-revenue")),releaseCapacity:sha256(file("release-capacity"))}};
}

const directory=process.argv[2]??"SimLogs", runs=process.argv.slice(3); if(!runs.length)throw new Error("Pass one or more run names.");
const result=runs.map(run=>analyze(directory,run));
fs.writeFileSync(path.join(directory,`${runs.join("_")}-a2-analysis.json`),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
