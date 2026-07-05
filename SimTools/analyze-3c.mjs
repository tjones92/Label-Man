import fs from "node:fs";
import path from "node:path";

function parse(line) {
  const out=[]; let value="", quoted=false;
  for(let i=0;i<line.length;i++){const c=line[i];if(quoted){if(c==='"'&&line[i+1]==='"'){value+='"';i++;}else if(c==='"')quoted=false;else value+=c;}else if(c==='"')quoted=true;else if(c===','){out.push(value);value="";}else value+=c;}out.push(value);return out;
}
function csv(file){const lines=fs.readFileSync(file,"utf8").trim().split(/\r?\n/);const headers=parse(lines.shift()??"");return lines.filter(Boolean).map(line=>{const values=parse(line);return Object.fromEntries(headers.map((h,i)=>[h,values[i]??""]));});}
const n=value=>Number(value||0);
const mean=values=>values.length?values.reduce((a,b)=>a+b,0)/values.length:null;
const youth=new Set(["RockAndRoll","TeenPop","RnB","DooWop","GirlGroup"]);
const dir=process.argv[2]??"SimLogs";
const runs=process.argv.slice(3);
const result=[];
for(const run of runs){
  const compositions=csv(path.join(dir,`${run}-album-composition.csv`));
  const links=csv(path.join(dir,`${run}-album-track-links.csv`));
  const projects=csv(path.join(dir,`${run}-album-projects.csv`));
  const economics=csv(path.join(dir,`${run}-a3-economic-decisions.csv`));
  const compIds=new Set(compositions.filter(row=>row.albumFormat==="Compilation").map(row=>row.recordId));
  const compLinks=links.filter(row=>compIds.has(row.albumRecordId));
  const usesBySource=new Map();
  for(const row of compLinks) usesBySource.set(row.sourceRecordId,(usesBySource.get(row.sourceRecordId)??0)+1);
  const distribution={};
  for(const uses of usesBySource.values()) distribution[uses]=(distribution[uses]??0)+1;
  const linksByAlbum=new Map();
  for(const row of compLinks){if(!linksByAlbum.has(row.albumRecordId))linksByAlbum.set(row.albumRecordId,[]);linksByAlbum.get(row.albumRecordId).push(row);}
  const staleIds=new Set([...linksByAlbum].filter(([,rows])=>rows.some(row=>n(row.timesCompUsedAtGeneration)>=1)).map(([id])=>id));
  const comps=compositions.filter(row=>compIds.has(row.recordId));
  const fresh=comps.filter(row=>!staleIds.has(row.recordId));
  const stale=comps.filter(row=>staleIds.has(row.recordId));
  const econByRecord=new Map(economics.map(row=>[row.recordId,row]));
  const youthAlbums=projects.filter(row=>youth.has(row.genre));
  const youthHitBearing=youthAlbums.filter(row=>n(econByRecord.get(row.albumRecordId)?.chartedSingles)>0);
  result.push({run,
    compUseCountDistribution:Object.fromEntries(Object.entries(distribution).sort((a,b)=>n(a[0])-n(b[0]))),
    maxTimesCompUsed:usesBySource.size?Math.max(...usesBySource.values()):0,
    releasedCompilations:comps.length,
    staleContainingCompilations:stale.length,
    pooledAppeal:{freshN:fresh.length,freshMean:mean(fresh.map(row=>n(row.pooledAppeal))),staleN:stale.length,staleMean:mean(stale.map(row=>n(row.pooledAppeal)))},
    youthHitConcentration:{albums:youthAlbums.length,hitBearing:youthHitBearing.length,share:youthAlbums.length?youthHitBearing.length/youthAlbums.length:null}
  });
}
console.log(JSON.stringify(result,null,2));
fs.writeFileSync(path.join(dir,`${runs.join("_")}-3c-analysis.json`),JSON.stringify(result,null,2));
