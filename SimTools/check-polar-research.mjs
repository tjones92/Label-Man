import fs from 'node:fs';
import zlib from 'node:zlib';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {analyzePolarResearch,csvRows} from './analyze-polar-research.mjs';

const args=process.argv.slice(2),kind=args.shift();
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const snapshot=p=>JSON.parse(zlib.gunzipSync(fs.readFileSync(`SimLogs/${p}-polar-checkpoint.json.gz`)).toString());
const invocation=p=>JSON.parse(fs.readFileSync(`SimLogs/${p}-invocation.json`,'utf8').replace(/^\uFEFF/,''));
let result;
if(kind==='neutrality') {
 assert(args.length>=2);
 const base=args[0],world=snapshot(base).Save.World;
 const checks=[];
 for(const other of args.slice(1)) {
  const sameAssembly=invocation(base).assemblySha256===invocation(other).assemblySha256;
  assert(sameAssembly,'Neutrality requires the same assembly');
  assert.deepEqual(snapshot(other).Save.World,world,'Observer changed endpoint world');
  const csvs=fs.readdirSync('SimLogs').filter(p=>p.startsWith(base+'-')&&p.endsWith('.csv')).map(p=>p.slice(base.length))
   .filter(s=>!s.startsWith('-repertoire-')&&!s.startsWith('-polar-')&&!s.includes('performance-profile'));
  let compared=0;for(const suffix of [...csvs,'-polar-final-weekly.csv']) {
   const a=`SimLogs/${base}${suffix}`,b=`SimLogs/${other}${suffix}`;
   assert(fs.existsSync(b),'Missing comparison stream '+b);
   assert.equal(hash(a),hash(b),'Observer changed stream '+suffix);compared++;
  }
  checks.push({base,other,sameAssembly,endpointWorldIdentical:true,identicalCsvStreams:compared});
 }
 result={kind,checks};
} else if(kind==='subset') {
 assert.equal(args.length,2);
 const [full,sample]=args.map(analyzePolarResearch);
 assert.equal(full.censusMode,'full');
 const fullRows=csvRows(`SimLogs/${args[0]}-repertoire-slots.csv`).rows;
 const sampleRows=csvRows(`SimLogs/${args[1]}-repertoire-slots.csv`).rows;
 const key=r=>[r.date,r.artistId,r.slot].join('|'),lookup=new Map(fullRows.map(r=>[key(r),r]));
 for(const row of sampleRows){const reference=lookup.get(key(row));assert(reference);for(const field of ['genre','cohort','songId','category','archetype','arrangementArchetype','originYear'])assert.equal(row[field],reference[field],field+' differs in sampled set');}
 result={kind,full:args[0],sample:args[1],identicalSelectedSlots:sampleRows.length};
} else if(kind==='stopped') {
 assert.equal(args.length,1);const prefix=args[0],root=`SimLogs/${prefix}`;
 const r=analyzePolarResearch(prefix);assert.equal(r.completeMonths.length,0);assert(r.coverage.excludedPartialActMonths>0);assert.equal(r.summaries.length,0);
 for(const suffix of ['-repertoire-admissions.csv','-repertoire-ordering.csv','-polar-final-weekly.csv'])assert(fs.statSync(root+suffix).size>0);
 assert(csvRows(root+'-repertoire-admissions.csv').rows.length>0);
 assert(csvRows(root+'-repertoire-ordering.csv').rows.length>0);
 assert(snapshot(prefix).Save.World.Artists.length>0);
 result={kind,prefix,partialExcluded:true,durableAdmissionRows:csvRows(root+'-repertoire-admissions.csv').rows.length,durableOrderingRows:csvRows(root+'-repertoire-ordering.csv').rows.length,loadableSnapshotEnvelope:true};
} else if(kind==='replay') {
 assert.equal(args.length,2);assert.deepEqual(snapshot(args[0]).Save.World,snapshot(args[1]).Save.World);
 for(const p of args)assert(fs.readFileSync(`SimLogs/${p}.log`,'utf8').includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS'));
 result={kind,runs:args,roundTripVerified:true,repeatedLoadContinuationIdentical:true};
} else throw Error('Use neutrality, subset, stopped or replay followed by run prefixes');
const out=`SimLogs/polar-research-check-${kind}-${args[0]}.json`;
fs.writeFileSync(out,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
