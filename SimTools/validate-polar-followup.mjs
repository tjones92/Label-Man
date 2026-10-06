import fs from 'node:fs';
import crypto from 'node:crypto';
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const dir='SimLogs', files=fs.readdirSync(dir);
function rows(f){const lines=fs.readFileSync(f,'utf8').trim().split(/\r?\n/),keys=lines.shift().split(',');return lines.map(l=>{const values=l.split(',').map(v=>v.replace(/^"|"$/g,''));return Object.fromEntries(keys.map((k,i)=>[k,values[i]]));});}
const neutrality=[];
for(const seed of [1001,1002]){
 const old=`polar-data-control-${seed}`,run=`polar-followup-neutral-${seed}`;
 const baseline=files.filter(f=>f.startsWith(old+'-')&&f.endsWith('.csv'));
 const mismatches=baseline.map(f=>f.slice(old.length+1)).filter(s=>!fs.existsSync(`${dir}/${run}-${s}`)||hash(`${dir}/${old}-${s}`)!==hash(`${dir}/${run}-${s}`));
 const complete=fs.readFileSync(`${dir}/${run}.log`,'utf8').includes(`CHART_AUDIT_COMPLETE run=${run} weeks=52`);
 neutrality.push({seed,streams:baseline.length,complete,byteIdentical:complete&&baseline.length===79&&!mismatches.length,mismatches});
}
const smoke=[];
for(const seed of [1001,1002])for(const mode of ['off','on']){
 const run=`polar-followup-checkpoint-${mode}-${seed}`, log=fs.readFileSync(`${dir}/${run}.log`,'utf8');
 const assignments=rows(`${dir}/${run}-polar-master-assignments.csv`),avoidance=rows(`${dir}/${run}-polar-refusal-avoidance.csv`),live=rows(`${dir}/${run}-polar-live-census.csv`);
 const sum=k=>live.reduce((n,r)=>n+Number(r[k]),0);
 if(live.some(r=>+r.recentCovers!==+r.recentHitCovers + +r.ordinaryCovers||+r.slots!==+r.originals + +r.traditional + +r.standards + +r.recentHitCovers + +r.ordinaryCovers))throw Error('Bad census partition');
 smoke.push({seed,mode,run,invocation:JSON.parse(fs.readFileSync(`${dir}/${run}-invocation.json`)),complete:log.includes(`CHART_AUDIT_COMPLETE run=${run} weeks=2`),masterAssignments:assignments.length,singles:assignments.filter(r=>r.kind==='single').length,albumTracks:assignments.filter(r=>r.kind==='albumTrack').length,strongRefusals:assignments.filter(r=>r.strongRefusal==='True').length,excludedAttempts:avoidance.filter(r=>r.event==='excluded').length,recordingUnfilled:avoidance.filter(r=>r.event==='unfilled').length,liveUnfilled:avoidance.filter(r=>r.event==='live-unfilled').length,sets:sum('sets'),shortSets:sum('shortSets'),slots:sum('slots'),originals:sum('originals'),recentHits:sum('recentHitCovers'),ordinaryCovers:sum('ordinaryCovers'),traditional:sum('traditional'),standards:sum('standards')});
}
const fixtures=['polar-song-data-check','polar-song-fit-check','polar-song-behavior-check','polar-recording-realization-check','polar-rock-songbook-check','polar-player-perception-check','polar-refusal-avoidance-check'].map(name=>{
 const file=`${dir}/polar-followup-checkpoint-${name}.log`,log=fs.readFileSync(file,'utf8');const lines=log.split(/\r?\n/).filter(l=>/^POLAR_.*(PASS|FIXTURE)/.test(l));return {name,file,pass:lines.some(l=>l.includes('PASS'))&&!log.includes('_FAIL'),lines};
});
const result={date:'2026-10-03',scope:'Pre-decade checkpoint; two-week census only; retained ten-year baseline; no new decade run',neutrality,smoke,fixtures};
fs.writeFileSync('SimTools/PolarFollowupValidation.json',JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({neutrality,smoke:smoke.map(({invocation,...r})=>r),fixtures:fixtures.map(({name,pass})=>({name,pass}))},null,2));
if(neutrality.some(r=>!r.byteIdentical)||smoke.some(r=>!r.complete||r.mode==='on'&&r.strongRefusals)||fixtures.some(r=>!r.pass))process.exitCode=1;
