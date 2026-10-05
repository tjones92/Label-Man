import fs from 'node:fs';
import assert from 'node:assert/strict';
import {analyzePolarResearch} from './analyze-polar-research.mjs';
const prefix='polar-research-analyzer-fixture',root='SimLogs/'+prefix,paths=[];
assert(!fs.readdirSync('SimLogs').some(p=>p.startsWith(prefix)),'Fixture output already exists');
function write(suffix,text){const p=root+suffix;paths.push(p);fs.writeFileSync(p,text);}
function csv(suffix,rows){const header=Object.keys(rows[0]);write(suffix,header.join(',')+'\n'+rows.map(r=>header.map(h=>r[h]??'').join(',')).join('\n')+'\n');}
try {
 write('-polar-research.json',JSON.stringify({schemaVersion:1,censusMode:'sample',definitions:{provenance:'Traditional lineage plus established standards / all filled slots'},months:[{Date:'1/1/1960',Status:'complete',Planned:3},{Date:'2/1/1960',Status:'inProgress',Planned:1,Observed:1}]}));
 const rows=[{date:'1/1/1960',artistId:'a',writingBin:'high',population:10,sample:1,weight:10,crossSection:'True',panel:'True'}, {date:'1/1/1960',artistId:'b',writingBin:'low',population:2,sample:1,weight:2,crossSection:'True',panel:'True'}, {date:'1/1/1960',artistId:'panel-only',writingBin:'high',population:10,sample:1,weight:0,crossSection:'False',panel:'True'}, {date:'2/1/1960',artistId:'partial',writingBin:'high',population:1000,sample:1,weight:1000,crossSection:'True',panel:'True'}].map(r=>({...r,genre:'Gospel',cohort:'inheritedRepertoire',unsigned:'True',requestedSlots:1,filledSlots:1}));
 csv('-polar-sample-acts.csv',rows);
 csv('-repertoire-slots.csv',rows.map(r=>({...r,year:1960,month:r.date[0],originYear:1900,sampleWeight:r.weight,category:r.artistId==='a'?'traditionalLineage':'existingCover',songId:r.artistId,slot:0})));
 csv('-polar-diversity-sets.csv',rows.map(r=>({...r,filledCovers:1,originals:0,requestedCovers:1})));
 csv('-polar-sample-design.csv',[{date:'1/1/1960',genre:'Gospel',cohort:'inheritedRepertoire',unsigned:'True',writingBin:'high',population:10,sample:1,panelObserved:2,weight:10},{date:'1/1/1960',genre:'Gospel',cohort:'inheritedRepertoire',unsigned:'True',writingBin:'low',population:2,sample:1,panelObserved:1,weight:2}]);
 paths.push(root+'-polar-research-analysis.json');
 const result=analyzePolarResearch(prefix),total=result.summaries.find(r=>r.period==='all');
 assert.equal(total.numerator,10);assert.equal(total.denominator,12);assert.equal(total.share,100*10/12);assert.equal(total.sampledActMonths,2);
 assert.equal(result.coverage.excludedPartialActMonths,1);assert.equal(result.coverage.excludedPartialSlots,1);
 console.log('PASS: weighted ratio, zero-weight panel supplement and incomplete-month exclusion');
} finally {for(const p of paths)if(fs.existsSync(p))fs.unlinkSync(p);}
