import fs from 'node:fs';
import {csvRows} from './analyze-polar-research.mjs';
const mask=(1n<<64n)-1n;
function hash(k){let h=14695981039346656037n;for(const c of k){h^=BigInt(c.charCodeAt(0));h=h*1099511628211n&mask;}h^=h>>33n;h=h*0xff51afd7ed558ccdn&mask;h^=h>>33n;h=h*0xc4ceb9fe1a85ec53n&mask;return h^(h>>33n);}
const unit=k=>Number(hash(k)>>40n)/16777216;
const manifest=JSON.parse(fs.readFileSync('SimLogs/polar-directive3-trajectory-shape-v2-runs.json','utf8').replace(/^\uFEFF/,''));
const acts=[];
for(const seed of [1001,1002]) {
 const raw=manifest.filter(r=>r.seed===seed).flatMap(r=>csvRows('SimLogs/'+r.run+'-directive2-quarter-slots.csv').rows).filter(r=>r.genre==='Gospel');
 const rows=[...new Map(raw.map(r=>[r.date+'|'+r.artistId+'|'+r.slot,r])).values()];
 const grouped=Map.groupBy(rows,r=>r.date+'|'+r.artistId);
 for(const a of grouped.values()){const r=a[0],month=+r.date.split('/')[0],year=+r.year;acts.push({seed,quarter:year+'-Q'+r.quarter,id:r.artistId,year,month,slots:a.length,covers:a.filter(r=>r.category!=='newlyAuthored').length});}
}
const results=[];
for(const mean of [.75,.77,.78,.79,.80])for(const spread of [.1,.2,.25]) {
 const groups=new Map();
 for(const a of acts){const key=a.seed+'|'+a.quarter;if(!groups.has(key))groups.set(key,{seed:a.seed,quarter:a.quarter,slots:0,inherited:0});const g=groups.get(key);g.slots+=a.slots;
  const share=Math.min(1,Math.max(0,mean+(unit(hash(a.id+'|repertoire')+'|inherited-affinity')*2-1)*spread));
  for(let i=0;i<a.covers;i++)g.inherited+=unit(a.id+'|live-source|'+a.year+'|'+a.month+'|'+i)<share;
 }
 const quarters=[...groups.values()].map(g=>({...g,share:100*g.inherited/g.slots}));
 results.push({mean,spread,inside:quarters.filter(q=>q.share>=65&&q.share<=70).length,min:Math.min(...quarters.map(q=>q.share)),max:Math.max(...quarters.map(q=>q.share)),quarters});
}
fs.writeFileSync('SimTools/GospelLiveShareCalibration.json',JSON.stringify({kind:'Predicted source draws on retained act/slot frames; not a fresh trajectory or fit-gate result',results},null,2)+'\n');
console.log(results.map(({quarters,...r})=>r));
