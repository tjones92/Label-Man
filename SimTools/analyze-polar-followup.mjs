import fs from 'node:fs';
import crypto from 'node:crypto';
const dir='SimLogs', prefix='polar-final-verified';
function csv(file) {
  const lines=fs.readFileSync(file,'utf8').trim().split(/\r?\n/), headers=lines.shift().split(',');
  return lines.map(line=>{ const cells=[]; let s='',q=false;
    for(let i=0;i<line.length;i++){let c=line[i];if(c==='"'){if(q&&line[i+1]==='"'){s+='"';i++;}else q=!q;}else if(c===','&&!q){cells.push(s);s='';}else s+=c;}cells.push(s);
    return Object.fromEntries(headers.map((h,i)=>[h,cells[i]])); });
}
function emit(name,rows){if(!rows.length)return;const keys=Object.keys(rows[0]);fs.writeFileSync(`${dir}/polar-followup-${name}.csv`,[keys.join(','),...rows.map(r=>keys.map(k=>r[k]).join(','))].join('\n')+'\n');}
const sum=(rs,k)=>rs.reduce((v,r)=>v+Number(r[k]),0), annual=[], shorts=[], composition=[], inputs=[];
function read(run,suffix){const file=`${dir}/${run}-${suffix}.csv`;inputs.push({path:file,sha256:crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')});return csv(file);}
for(const seed of [1001,1002]){
 const modes={};
 for(const mode of ['off','on']){
  const run=`${prefix}-${mode}-${seed}`,weekly=read(run,'polar-final-weekly'),live=read(run,'polar-live-census'),materials=read(run,'song-material'),lanes=read(run,'single-release-lanes');
  const log=fs.readFileSync(`${dir}/${run}.log`,'utf8');
  if(weekly.length!==521||!log.includes(`CHART_AUDIT_COMPLETE run=${run} weeks=521`)||!log.includes('POLAR_FINAL_DATE date=12/31/1969'))throw Error('Incomplete source '+run);
  modes[mode]={};
  for(let year=1960;year<=1969;year++){
   const rows=weekly.filter(r=>+r.year===year), n=sum(rows,'newSingleCount');
   // Lane events identify observed direct versus promoted releases. Weekly singles is a separate pipeline counter.
   const laneRows=lanes.filter(r=>+r.year===year), laneCounts={};for(const r of laneRows)laneCounts[r.releaseLane]=(laneCounts[r.releaseLane]??0)+1;
   modes[mode][year]={labelNet:sum(rows,'labelNet'),directPipelineSingles:sum(rows,'singles'),observedSingles:n,albumDrops:sum(rows,'albums'),quality:rows.reduce((v,r)=>v+(+r.newSingleCount)*(+r.meanNewSingleQuality),0)/n,dying:+rows.at(-1).dyingLabels,laneCounts};
   const genres=[...new Set(materials.filter(r=>+r.year===year).map(r=>r.genre))];
   for(const genre of genres){const selected=materials.filter(r=>+r.year===year&&r.genre===genre),cats={};for(const r of selected)cats[r.songSource]=(cats[r.songSource]??0)+1;for(const [source,count]of Object.entries(cats))composition.push({seed,mode,year,genre,source,count,coverage:'observed-release-material-not-all-album-masters'});}
  }
  for(let year=1960;year<=1963;year++)for(const genre of [...new Set(live.filter(r=>+r.year===year).map(r=>r.genre))]){
   const rows=live.filter(r=>+r.year===year&&r.genre===genre),sets=sum(rows,'sets'),short=sum(rows,'shortSets');
   shorts.push({seed,mode,year,genre,sets,shortSets:short,shortShare:short/sets,slots:sum(rows,'slots'),originals:sum(rows,'originals'),traditional:sum(rows,'traditional'),standards:sum(rows,'standards'),otherCovers:sum(rows,'recentCovers')});
  }
 }
 for(let year=1960;year<=1969;year++){const a=modes.off[year],b=modes.on[year];annual.push({seed,year,labelNetOff:a.labelNet,labelNetOn:b.labelNet,labelNetDeltaPct:100*(b.labelNet/a.labelNet-1),directPipelineOff:a.directPipelineSingles,directPipelineOn:b.directPipelineSingles,directPipelineDelta:b.directPipelineSingles-a.directPipelineSingles,observedOff:a.observedSingles,observedOn:b.observedSingles,observedDelta:b.observedSingles-a.observedSingles,qualityOff:a.quality,qualityOn:b.quality,qualityDelta:b.quality-a.quality,dyingOff:a.dying,dyingOn:b.dying,dyingDelta:b.dying-a.dying,laneCountsOff:JSON.stringify(a.laneCounts).replaceAll(',',';'),laneCountsOn:JSON.stringify(b.laneCounts).replaceAll(',',';')});}
}
// JSON lane maps contain quotes/commas; store those separately, keeping CSV fields unambiguous.
fs.writeFileSync(`${dir}/polar-followup-existing-analysis.json`,JSON.stringify({annual,inputs},null,2));
emit('annual-existing',annual.map(({laneCountsOff,laneCountsOn,...r})=>r));emit('short-sets-existing',shorts);emit('release-material-existing',composition);
console.log(JSON.stringify({annual:annual.filter(r=>r.year>=1967),shortSetChanges:[1001,1002].map(seed=>{const a=shorts.filter(r=>r.seed===seed&&r.mode==='off'),b=shorts.filter(r=>r.seed===seed&&r.mode==='on');return{seed,off:sum(a,'shortSets'),on:sum(b,'shortSets'),delta:sum(b,'shortSets')-sum(a,'shortSets'),byGenre:[...new Set([...a,...b].map(r=>r.genre))].map(genre=>({genre,off:sum(a.filter(r=>r.genre===genre),'shortSets'),on:sum(b.filter(r=>r.genre===genre),'shortSets')})).sort((x,y)=>(y.on-y.off)-(x.on-x.off))};})},null,2));
