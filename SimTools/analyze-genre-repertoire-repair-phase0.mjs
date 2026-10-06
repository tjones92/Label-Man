import fs from 'node:fs';
import zlib from 'node:zlib';
import readline from 'node:readline';
import {csvRows} from './analyze-polar-research.mjs';
import assert from 'node:assert/strict';

export const readCsv=path=>{const parsed=csvRows(path);assert.equal(parsed.malformed,0,path);return parsed.rows;};
export const sum=(rows,fn=r=>+r.sampleWeight)=>rows.reduce((a,r)=>a+fn(r),0);
export const group=(rows,fn)=>{const m=new Map();for(const r of rows){const k=fn(r);if(!m.has(k))m.set(k,[]);m.get(k).push(r);}return m;};
export const yearBin=y=>y<1940?'pre-1940':y<1950?'1940–49':y<1955?'1950–54':y<1960?'1955–59':'1960+';
// Pretty-printed retained snapshots are too large for one V8 string. Read only
// individual composition objects, discarding every other world section.
export async function songsFromSnapshot(path) {
 const result=new Map();const recordPeaks=new Map();result.recordPeaks=recordPeaks;
 let section=false,inside=false,lines=[],records=false,recordId=null,recordPeak=null,recordFormat=null;
 const input=readline.createInterface({input:fs.createReadStream(path).pipe(zlib.createGunzip()),crlfDelay:Infinity});
 for await(const line of input) {
  if(!section){
   if(/^      "Records": \[$/.test(line))records=true;
   if(records){
    if(/^        \{$/.test(line)){recordId=null;recordPeak=null;recordFormat=null;}
    const id=line.match(/^            "recordId": (".*"),?$/);if(id)recordId=JSON.parse(id[1]);
    const peak=line.match(/^          "peakPosition": (\d+),?$/);if(peak)recordPeak=+peak[1];
    const format=line.match(/^            "format": (\d+),?$/);if(format)recordFormat=+format[1];
    if(/^        \},?$/.test(line)&&recordId&&recordPeak!==null)recordPeaks.set(recordId,{peakPosition:recordPeak,format:recordFormat});
    if(/^      \],?$/.test(line))records=false;
   }
   if(/^\s*"Songs": \{$/.test(line))section=true;continue;
  }
  if(!inside){if(/^        \},?$/.test(line))break;if(/^          ".*": \{$/.test(line)){inside=true;lines=['{'];}continue;}
  lines.push(line);
  if(/^          \},?$/.test(line)){lines[lines.length-1]='}';const s=JSON.parse(lines.join('\n'));result.set(s.songId,s);inside=false;}
 }
 return result;
}

if(process.argv.includes('--phase0')) {
 const runs=['genre-followup-v1-matched-1001','genre-followup-v1-matched-1002',...['42001','42002','42003','42004'].flatMap(s=>[`genre-followup-v1-late-${s}`,`genre-followup-v1-late-${s}-resume1`])];
 const completeDates=run=>new Set(JSON.parse(fs.readFileSync(`SimLogs/${run}-polar-research.json`,'utf8')).months.filter(m=>m.Status==='complete').map(m=>m.Date));
 const slots=runs.flatMap(run=>{const dates=completeDates(run);return readCsv(`SimLogs/${run}-genre-followup-slots.csv`).filter(r=>dates.has(r.date)&&r.crossSection==='True'&&r.date.endsWith('/1963')&&+r.date.split('/')[0]<=2).map(r=>({...r,run}));});
 assert.equal(new Set(slots.map(r=>[r.seed,r.date,r.artistId,r.slot].join('|'))).size,slots.length,'No duplicated retained observations');
 const output={runs,questions:{},limitations:['Retained pool/catalogue censuses observe January and February, not every chart week. No new weekly baseline was invented.','Source chart evidence is retained runtime peak and completed chart history as of checkpoint. Album-track sources use their parent album peak, not an independently charted cut; synthetic PreGameStandard has no source-record chart history.']};
 const books=[];const enrich=[];
 for(const seed of [1001,1002,42001,42002,42003,42004]) {
  const run=seed<42000?`genre-followup-v1-matched-${seed}`:`genre-followup-v1-late-${seed}-resume1`;
  const songs=await songsFromSnapshot(`SimLogs/${run}-polar-checkpoint.json.gz`);assert(songs.size>5000,'Snapshot compositions parsed');
  const book=[...songs.values()].filter(s=>s.repertoireSeedFamily==='Classical works');
  books.push({seed,size:book.length,years:Object.fromEntries([...group(book,s=>yearBin(s.originYear))].map(([k,v])=>[k,v.length]))});
  for(const r of slots.filter(r=>+r.seed===seed)){const s=songs.get(r.songId);const sourceId=s?.songId?.startsWith('song_orig_')?s.songId.slice('song_orig_'.length):null;
   const parentId=sourceId?.replace(/_t\d+$/,'');const direct=sourceId?songs.recordPeaks.get(sourceId):null,parent=parentId!==sourceId?songs.recordPeaks.get(parentId):null;
   const history=(s?.recordings??[]),completed=history.find(x=>x.recordId===sourceId);
   const sourcePeak=direct?.peakPosition??parent?.peakPosition??completed?.peakPosition??null;
   enrich.push({...r,originArtistId:s?.originArtistId??'',memberWriterIds:(s?.credits??[]).filter(c=>c.isArtistMember).map(c=>c.writerId),
    charted:sourcePeak===null?null:sourcePeak>0,sourcePeak,sourceChartEvidence:direct?'sourceRecordRuntime':parent?'parentAlbumRuntime':completed?'completedSourceRecording':'unavailable',
    parentAlbumRecordId:parent?parentId:null,anyRecordingCharted:history.some(x=>x.peakPosition>0),sourceRecordIds:history.map(x=>x.recordId),originalSourceRecordId:sourceId});}
 }
 output.questions.Q1={catalogue:runs.flatMap(run=>readCsv(`SimLogs/${run}-genre-followup-catalog.csv`).filter(r=>completeDates(run).has(r.date)&&['Screen instrumental','Stage and film songs'].includes(r.seedFamily)).map(r=>({...r,run}))),pools:runs.flatMap(run=>readCsv(`SimLogs/${run}-genre-followup-pools.csv`).filter(r=>completeDates(run).has(r.date)&&r.crossSection==='True'&&['Screen instrumental','Stage and film songs'].includes(r.seedFamily))),mechanism:'Per-cut keyed access, then song fit-band ranking. Exact EasyListening .22, secondary TraditionalPop .08, other Pop family .035, Country via Pop cross-scene .015; identity drift interpolates tiers. No vocal-act instrumental eligibility constraint.'};
 output.questions.Q2=[];
 for(const genre of ['Country','TraditionalPop','TeenPop'])for(const bucket of ['ArtistOriginal','PreGameStandard','allExistingCover']) {
  const rows=enrich.filter(r=>r.unsigned==='True'&&r.genre===genre&&r.category==='existingCover'&&(bucket==='allExistingCover'||r.originKind===bucket&&(bucket!=='ArtistOriginal'||r.primaryGenre===genre)&&(bucket!=='PreGameStandard'||genre!=='Country'||r.seedFamily==='Country Standard')));
  for(const bin of ['pre-1940','1940–49','1950–54','1955–59','1960+'])for(const charted of [true,false,null]) {
   const selected=rows.filter(r=>yearBin(+r.originYear)===bin&&r.charted===charted);if(selected.length)output.questions.Q2.push({genre,bucket,bin,charted,observedSlots:selected.length,weightedSlots:sum(selected),bucketPct:100*sum(selected)/sum(rows),titles:[...new Set(selected.map(r=>r.songTitle))]});
  }
 }
 output.unsignedDenominators=Object.fromEntries(['Country','TraditionalPop','TeenPop'].map(g=>[g,sum(enrich.filter(r=>r.unsigned==='True'&&r.genre===g))]));
 output.enrichedRows=enrich;
 const routines=enrich.filter(r=>r.genre==='Comedy'&&r.seedFamily==='Comedy routines');
 output.questions.Q3={rows:routines,observed:routines.length,weightedSlots:sum(routines),originatorMatchSlots:sum(routines.filter(r=>r.originArtistId===r.artistId)),unknownAuthorSlots:sum(routines.filter(r=>!r.originArtistId&&!r.memberWriterIds.length)),note:'Originator IDs and member writer evidence retained. Seeded anonymous routines cannot be asserted authored by the performer; unknown is separate from proven non-author.'};
 output.questions.Q4={books,classicalOriginalRows:enrich.filter(r=>['Comedy','Childrens','TeenPop','Country','TraditionalPop'].includes(r.genre)&&r.primaryGenre==='Classical'&&r.originKind==='ArtistOriginal')};
 fs.writeFileSync('SimTools/GenreRepertoireRepairPhase0.json',JSON.stringify(output,null,2)+'\n');
 console.log(JSON.stringify({Q2:output.questions.Q2.map(({titles,...r})=>r),Q3:{...output.questions.Q3,rows:undefined},Q4:{books,classicalOriginalRows:output.questions.Q4.classicalOriginalRows.slice(0,12)}}));
}
