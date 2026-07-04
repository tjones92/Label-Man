import fs from "node:fs";
import path from "node:path";

function line(line) {
  const out = []; let value = "", quoted = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (quoted) { if (c === '"' && line[i + 1] === '"') { value += '"'; i++; } else if (c === '"') quoted = false; else value += c; }
    else if (c === '"') quoted = true; else if (c === ',') { out.push(value); value = ""; } else value += c;
  }
  out.push(value); return out;
}
function csv(file) {
  const lines = fs.readFileSync(file, "utf8").trim().split(/\r?\n/);
  const headers = line(lines.shift() ?? "");
  return lines.filter(Boolean).map(text => { const values = line(text); return Object.fromEntries(headers.map((h, i) => [h, values[i] ?? ""])); });
}
const n = value => Number(value || 0);
const mean = values => values.length ? values.reduce((a, b) => a + b, 0) / values.length : null;
function median(values) { if (!values.length) return null; const s = [...values].sort((a,b)=>a-b), i=(s.length-1)/2; return (s[Math.floor(i)]+s[Math.ceil(i)])/2; }
function corr(xs, ys) {
  if (xs.length < 2) return null; const mx=mean(xs), my=mean(ys); let xy=0, xx=0, yy=0;
  for(let i=0;i<xs.length;i++){const x=xs[i]-mx,y=ys[i]-my;xy+=x*y;xx+=x*x;yy+=y*y;} return xx&&yy?xy/Math.sqrt(xx*yy):null;
}
const adult = new Set(["Jazz","EasyListening","Folk","TraditionalPop","BossaNova","Country"]);
const youth = new Set(["RockAndRoll","TeenPop","RnB","DooWop","GirlGroup"]);
const baselines = {1001:0.494,1002:0.529,1003:0.578};

function analyze(dir, run, seed) {
  const read = suffix => csv(path.join(dir, `${run}-${suffix}.csv`));
  const weeks=read("weeks"), rows=read("records"), life=read("lifecycles"), strategies=read("release-strategy"),
    projects=read("album-projects"), outcomes=read("release-outcomes"), economic=read("a3-economic-decisions"), demand=read("album-project-demand"), albumChart=read("album-chart");
  const byRecord = new Map();
  for (const row of rows) { if (!byRecord.has(row.recordId)) byRecord.set(row.recordId, []); byRecord.get(row.recordId).push(row); }
  const charting=[];
  for (const [id, recordRows] of byRecord) { const positioned=recordRows.filter(r=>n(r.currentPosition)>0); if(!positioned.length) continue; charting.push({id,quality:n(recordRows[0].quality),peak:Math.min(...positioned.map(r=>n(r.currentPosition)))}); }
  const pearson=corr(charting.map(r=>r.quality),charting.map(r=>101-r.peak));
  const choices=strategies.filter(r=>r.strategy || r.chosenFormat);
  const albums=choices.filter(r=>(r.strategy || (r.chosenFormat==="Album"?"Album":"OrphanSingle"))!=="OrphanSingle");
  const adultChoices=choices.filter(r=>adult.has(r.genre)), youthChoices=choices.filter(r=>youth.has(r.genre));
  const adultAlbums=adultChoices.filter(r=>r.strategy!=="OrphanSingle" && (r.strategy || r.chosenFormat)==="Album" || r.strategy?.startsWith("Album"));
  const youthAlbums=youthChoices.filter(r=>r.strategy!=="OrphanSingle" && (r.strategy || r.chosenFormat)==="Album" || r.strategy?.startsWith("Album"));
  const econById=new Map(economic.map(r=>[r.recordId,r]));
  const youthCompilation=projects.filter(p=>youth.has(p.genre)&&econById.get(p.albumRecordId)?.actualAlbumFormat==="Compilation");
  const state = name => projects.filter(p=>p.terminalState===name).length;
  const outcomeById=new Map(outcomes.map(r=>[r.recordId,r]));
  const projectByPromo=new Map(projects.filter(p=>p.promoSingleId).map(p=>[p.promoSingleId,p]));
  const projectByAlbum=new Map(projects.map(p=>[p.albumRecordId,p]));
  const orphanOutcomes=outcomes.filter(o=>o.memoryEligible==="true"&&o.format==="Single"&&!projectByPromo.has(o.recordId));
  const standaloneCompleted=projects.filter(p=>p.strategy==="AlbumStandalone"&&p.projectRealizedNet!=="");
  const promoCompleted=projects.filter(p=>p.strategy==="AlbumWithPromo"&&p.projectRealizedNet!=="");
  const heldPromo=projects.filter(p=>p.promoRetired==="true"&&p.albumRetired!=="true"&&p.terminalState!=="Cancelled");
  const redirected=projects.filter(p=>p.terminalState==="Cancelled"&&p.promoRetired==="true");
  const unresolvedAlbum=projects.filter(p=>p.albumRetired==="true"&&p.strategy==="AlbumWithPromo"&&p.promoRetired!=="true");
  const eligible=outcomes.filter(o=>o.memoryEligible==="true"&&(projectByPromo.has(o.recordId)||projectByAlbum.has(o.recordId)||choices.some(s=>s.recordId===o.recordId))).length;
  const recordEquivalent=orphanOutcomes.length+standaloneCompleted.length+2*promoCompleted.length+heldPromo.length+redirected.length+unresolvedAlbum.length;
  const orphanStrategies=choices.filter(s=>(s.strategy||"OrphanSingle")==="OrphanSingle");
  const singleErrors=orphanStrategies.map(s=>outcomeById.has(s.recordId)?n(s.projectedOrphanSingleNet||s.projectedSingleNet)-n(outcomeById.get(s.recordId).realizedNet):null).filter(v=>v!==null);
  const cohort = (items, expectedField, realizedFn) => {
    const completed=items.filter(p=>p.projectRealizedNet!=="" || (expectedField==="projectedAlbumNet"&&outcomeById.has(p.albumRecordId)));
    const expected=completed.map(p=>n(strategies.find(s=>s.projectId===p.projectId)?.[expectedField]));
    const realized=completed.map(realizedFn);
    return {total:items.length,completed:completed.length,pending:items.filter(p=>p.terminalState==="PendingAtAuditEnd").length,
      cancelled:items.filter(p=>p.terminalState==="Cancelled").length,unretired:items.filter(p=>p.terminalState==="Released"&&(expectedField==="projectedAlbumNet"?!outcomeById.has(p.albumRecordId):p.projectRealizedNet==="")).length,
      meanExpected:mean(expected),meanRealized:mean(realized),meanSignedError:mean(expected.map((v,i)=>v-realized[i]))};
  };
  const albumAdultRows=albumChart.filter(r=>adult.has(r.genre)).length;
  const singleChartRows=rows.filter(r=>n(r.currentPosition)>0), adultSingleRows=singleChartRows.filter(r=>adult.has(r.genre)).length;
  const denominator=weeks.reduce((sum,r)=>sum+n(r.newEntriesTop100),0), allSingles=orphanStrategies.length+choices.filter(s=>s.strategy==="AlbumWithPromo").length;
  const promoLaunch=[];
  const demandByProject=new Map(demand.map(r=>[r.projectId,r]));
  for(const p of projects.filter(p=>p.strategy==="AlbumWithPromo"&&p.terminalState==="Released")){const launch=demandByProject.get(p.projectId);if(launch)promoLaunch.push({score:n(p.promoPeakScore),awareness:n(launch.initialLaunchAwareness),stock:n(launch.initialLaunchStock)});}
  const raw=demand.reduce((s,r)=>s+n(r.rawDemandBeforeCannibalization),0), suppressed=demand.reduce((s,r)=>s+n(r.suppressedDemand),0);
  const standaloneDemand=demand.filter(r=>r.strategy==="AlbumStandalone"), standRaw=standaloneDemand.reduce((s,r)=>s+n(r.rawDemandBeforeCannibalization),0), standSupp=standaloneDemand.reduce((s,r)=>s+n(r.suppressedDemand),0);
  return {run,seed,decisions:choices.length,albumDecisions:albums.length,albumShare:albums.length/choices.length,
    adultAlbum:[adultAlbums.length,adultChoices.length,adultAlbums.length/adultChoices.length],youthAlbum:[youthAlbums.length,youthChoices.length,youthAlbums.length/youthChoices.length],
    youthCompilation:[youthCompilation.length,youthAlbums.length],physicalAlbumDrops:state("Released"),strategySplit:Object.fromEntries(Object.entries(Object.groupBy(choices,r=>r.strategy||r.chosenFormat)).map(([k,v])=>[k,v.length])),
    adultAlbumChart:[albumAdultRows,albumChart.length,albumAdultRows/albumChart.length],adultSingleChart:[adultSingleRows,singleChartRows.length,adultSingleRows/singleChartRows.length],
    pearson,pearsonDelta:pearson-baselines[seed],closedTop40Median:median(life.filter(r=>n(r.peakPosition)>0&&n(r.peakPosition)<=40).map(r=>n(r.weeksOnChart))),
    singleError:{n:singleErrors.length,mean:mean(singleErrors)},competition:{numerator:allSingles,orphanNumerator:orphanStrategies.length,denominator,ratio:allSingles/denominator,orphanRatio:orphanStrategies.length/denominator},
    projects:{scheduled:projects.length,released:state("Released"),cancelled:state("Cancelled"),pending:state("PendingAtAuditEnd"),transferred:projects.filter(p=>p.wasTransferred==="true").length,transferCount:projects.reduce((s,p)=>s+n(p.transferCount),0),overduePending:projects.filter(p=>p.terminalState==="PendingAtAuditEnd"&&n(p.dropWeek)<=52).length},
    memory:{eligibleRetiredRecords:eligible,orphanSingleObservations:orphanOutcomes.length,standaloneAlbumObservations:standaloneCompleted.length,promoProjectAlbumObservations:promoCompleted.length,heldPromo:heldPromo.length,redirectedPromo:redirected.length,unresolvedAlbum:unresolvedAlbum.length,recordEquivalent,
      singleObservations:orphanOutcomes.length+redirected.length,albumObservations:standaloneCompleted.length+promoCompleted.length,heldUnresolvedObservations:heldPromo.length+unresolvedAlbum.length},
    synergy:{n:promoLaunch.length,positiveScores:promoLaunch.filter(v=>v.score>0).length,awarenessCorrelation:corr(promoLaunch.map(v=>v.score),promoLaunch.map(v=>v.awareness)),stockCorrelation:corr(promoLaunch.map(v=>v.score),promoLaunch.map(v=>v.stock))},
    cannibalization:{raw,suppressed,ratio:raw? suppressed/raw:null,standaloneN:standaloneDemand.length,standaloneRaw:standRaw,standaloneSuppressed:standSupp,standaloneRatio:standRaw?standSupp/standRaw:null},
    youthCompilationWatch:cohort(youthCompilation,"projectedAlbumNet",p=>n(outcomeById.get(p.albumRecordId)?.realizedNet)),
    promoProjectWatch:cohort(projects.filter(p=>p.strategy==="AlbumWithPromo"),"projectedAlbumWithPromoNet",p=>n(p.projectRealizedNet))};
}

const dir=process.argv[2]??"SimLogs";
const specs=[["b-enabled-1001a",1001],["b-enabled-1002",1002],["b-enabled-1003",1003]];
const result=specs.map(([run,seed])=>analyze(dir,run,seed));
for(const item of result){const frozenRun=`a3-final-${item.seed===1001?"1001b":item.seed}`;const frozen=csv(path.join(dir,`${frozenRun}-release-strategy.csv`));const frozenWeeks=csv(path.join(dir,`${frozenRun}-weeks.csv`));const frozenDen=frozenWeeks.reduce((s,r)=>s+n(r.newEntriesTop100),0), orphan=frozen.filter(r=>r.chosenFormat==="Single").length;item.competition.frozenOrphanNumerator=orphan;item.competition.frozenDenominator=frozenDen;item.competition.frozenOrphanRatio=orphan/frozenDen;item.competition.orphanAbsoluteChange=item.competition.orphanRatio-orphan/frozenDen;item.competition.orphanPercentageChange=(item.competition.orphanRatio/(orphan/frozenDen)-1);}
fs.writeFileSync(path.join(dir,"b-validation-analysis.json"),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
