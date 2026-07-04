import fs from "node:fs";
import path from "node:path";

function parseLine(line) {
  const values=[]; let value="", quoted=false;
  for(let i=0;i<line.length;i++){const c=line[i]; if(quoted){if(c==='"'&&line[i+1]==='"'){value+='"';i++;}else if(c==='"')quoted=false;else value+=c;}
    else if(c==='"')quoted=true;else if(c===','){values.push(value);value="";}else value+=c;}
  values.push(value); return values;
}
function csv(file){const text=fs.readFileSync(file,"utf8").trim();if(!text)return[];const lines=text.split(/\r?\n/),headers=parseLine(lines.shift());
  return lines.filter(Boolean).map(line=>Object.fromEntries(headers.map((h,i)=>[h,parseLine(line)[i]??""])));}
const n=v=>Number(v??0), mean=a=>a.length?a.reduce((x,y)=>x+y,0)/a.length:null;
const adult=new Set(["Jazz","EasyListening","Folk","TraditionalPop","BossaNova","Country"]);
const youth=new Set(["RockAndRoll","TeenPop","RnB","DooWop","GirlGroup"]);
const share=(a,b)=>b?a/b:null;

function analyze(dir,run){const file=s=>path.join(dir,`${run}-${s}.csv`), rows=csv(file("a3-economic-decisions")), outcomes=csv(file("release-outcomes"));
  const selected=rows.filter(r=>r.chosenFormat==="Album"), adults=rows.filter(r=>adult.has(r.genre)), youths=rows.filter(r=>youth.has(r.genre));
  const youthAlbums=youths.filter(r=>r.chosenFormat==="Album"), inventory=youths.filter(r=>n(r.chartedSingles)>0), hitless=youths.filter(r=>n(r.chartedSingles)===0);
  const weight1=selected.filter(r=>n(r.compCostWeight)===1), weight0=selected.filter(r=>n(r.compCostWeight)===0), blend=selected.filter(r=>n(r.compCostWeight)===.48);
  const formats=group=>Object.fromEntries([...new Set(group.map(r=>r.actualAlbumFormat))].sort().map(f=>[f,group.filter(r=>r.actualAlbumFormat===f).length]));
  const byId=new Map(rows.map(r=>[r.recordId,r])), completed=outcomes.filter(r=>r.memoryEligible==="true"&&byId.has(r.recordId));
  function error(format){const group=completed.filter(r=>r.format===format), prior=[],final=[];for(const o of group){const d=byId.get(o.recordId),actual=n(o.realizedNet);prior.push(n(format==="Single"?d.priorSingleNet:d.priorAlbumNet)-actual);final.push(n(format==="Single"?d.projectedSingleNet:d.projectedAlbumNet)-actual);}return{n:group.length,priorMeanSignedError:mean(prior),finalMeanSignedError:mean(final)};}
  const youthComp=completed.filter(o=>o.format==="Album"&&youth.has(byId.get(o.recordId).genre)&&byId.get(o.recordId).actualAlbumFormat==="Compilation");
  const youthCompNets=youthComp.map(o=>n(o.realizedNet)), youthCompPrior=youthComp.map(o=>n(byId.get(o.recordId).priorAlbumNet)-n(o.realizedNet)), youthCompFinal=youthComp.map(o=>n(byId.get(o.recordId).projectedAlbumNet)-n(o.realizedNet));
  const forkGroups=[];for(const career of [...new Set(youths.map(r=>r.careerState))].sort()){const g=youths.filter(r=>r.careerState===career);forkGroups.push({careerState:career,n:g.length,meanPriorSingle:mean(g.map(r=>n(r.priorSingleNet))),meanPriorAlbum:mean(g.map(r=>n(r.priorAlbumNet))),meanFinalSingle:mean(g.map(r=>n(r.projectedSingleNet))),meanFinalAlbum:mean(g.map(r=>n(r.projectedAlbumNet))),meanDifference:mean(g.map(r=>n(r.projectedAlbumNet)-n(r.projectedSingleNet))),albumChoices:g.filter(r=>r.chosenFormat==="Album").length});}
  return{run,decisions:rows.length,format:{albums:selected.length,albumShare:share(selected.length,rows.length),adultAlbums:adults.filter(r=>r.chosenFormat==="Album").length,adultDecisions:adults.length,adultShare:share(adults.filter(r=>r.chosenFormat==="Album").length,adults.length),youthAlbums:youthAlbums.length,youthDecisions:youths.length,youthShare:share(youthAlbums.length,youths.length),youthCompilations:youthAlbums.filter(r=>r.actualAlbumFormat==="Compilation").length},
    mechanism:{selectedYouthWithChartedInventory:youthAlbums.filter(r=>n(r.chartedSingles)>0).length,selectedYouthAlbums:youthAlbums.length,selectedShare:share(youthAlbums.filter(r=>n(r.chartedSingles)>0).length,youthAlbums.length),inventoryAlbumChoices:inventory.filter(r=>r.chosenFormat==="Album").length,inventoryDecisions:inventory.length,inventoryChoiceShare:share(inventory.filter(r=>r.chosenFormat==="Album").length,inventory.length),hitlessAlbumChoices:hitless.filter(r=>r.chosenFormat==="Album").length,hitlessDecisions:hitless.length,hitlessChoiceShare:share(hitless.filter(r=>r.chosenFormat==="Album").length,hitless.length),meanSelectedHitScore:mean(youthAlbums.map(r=>n(r.hitScore))),meanIdsExamined:mean(rows.map(r=>n(r.releasedSingleIdsExamined))),meanResolved:mean(rows.map(r=>n(r.resolvedSingles)))},
    expectedCost:{weight1:{n:weight1.length,agreement:weight1.filter(r=>r.actualAlbumFormat==="Compilation").length,formats:formats(weight1)},weight0:{n:weight0.length,agreement:weight0.filter(r=>r.actualAlbumFormat!=="Compilation").length,formats:formats(weight0)},adultBlend:{n:blend.length,compilations:blend.filter(r=>r.actualAlbumFormat==="Compilation").length,compilationShare:share(blend.filter(r=>r.actualAlbumFormat==="Compilation").length,blend.length),deviationFrom48:share(blend.filter(r=>r.actualAlbumFormat==="Compilation").length,blend.length)-.48,formats:formats(blend)},conceptPreemptions:selected.filter(r=>r.actualAlbumFormat==="Concept").length},
    singleError:error("Single"),albumCompletedError:error("Album"),youthCompilationCompleted:{n:youthComp.length,meanRealizedNet:mean(youthCompNets),priorMeanSignedError:mean(youthCompPrior),finalMeanSignedError:mean(youthCompFinal)},youthForkGroups:forkGroups};
}
const dir=process.argv[2]??"SimLogs",runs=process.argv.slice(3);if(!runs.length)throw new Error("Pass run names.");const result=runs.map(r=>analyze(dir,r));
const output=path.join(dir,`${runs.join("_")}-a3-analysis.json`);fs.writeFileSync(output,JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
