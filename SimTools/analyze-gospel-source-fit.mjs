import fs from 'node:fs';
import {csvRows} from './analyze-polar-research.mjs';
const results=[];
for(const stage of ['start','later'])for(const seed of [1001,1002]) {
 const prefix=`SimLogs/polar-directive3-final-${stage}-${seed}`;
 const metadata=new Map(csvRows(prefix+'-gospel-candidates.csv').rows.filter(r=>r.primaryGenre==='Gospel'||r.originKind==='ArtistOriginal').map(r=>[r.artistId+'|'+r.songId,r]));
 const groups=new Map();
 for(const r of csvRows(prefix+'-directive3-fits.csv').rows) {
  if(r.genre!=='Gospel'||!['v1Resolved','v2Resolved','v1Frozen','v2Frozen'].includes(r.variant))continue;
  const song=metadata.get(r.artistId+'|'+r.songId);
  const source=song?.family==='Gospel Standard'?'Gospel Standard':song?.originKind==='ArtistOriginal'?'ArtistOriginal':'other';
  const key=source+'|'+r.variant;
  if(!groups.has(key))groups.set(key,{stage,seed,source,variant:r.variant,n:0,capability:0,moment:0,identity:0,capabilityTop:0,momentTop:0});
  const g=groups.get(key);g.n++;for(const k of ['capability','identity','moment'])g[k]+=+r[k];g.capabilityTop+=+r.capability>=.8;g.momentTop+=+r.moment>=.8;
 }
 for(const g of groups.values()){for(const k of ['capability','identity','moment','capabilityTop','momentTop'])g[k]/=g.n;results.push(g);}
}
fs.writeFileSync('SimTools/GospelSourceFitComparison.json',JSON.stringify(results,null,2)+'\n');
console.log(JSON.stringify(results.filter(r=>r.source!=='other'&&r.variant.endsWith('Resolved')),null,2));
