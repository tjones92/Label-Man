// Read-only progress snapshot. Partial months are explicitly excluded; not acceptance evidence.
import fs from 'node:fs';
import readline from 'node:readline';
function fields(line){const out=[];let value='',quoted=false;for(let i=0;i<line.length;i++){const c=line[i];if(c==='"'){if(quoted&&line[i+1]==='"'){value+='"';i++;}else quoted=!quoted;}else if(c===','&&!quoted){out.push(value);value='';}else value+=c;}out.push(value);return out;}
const snapshots=[];for(const prefix of process.argv.slice(2)){
 const root='SimLogs/'+prefix,log=fs.readFileSync(root+'.log','utf8');
 const dates=[...log.matchAll(/Advanced to Friday, (\w+) (\d+), (\d+)/g)];
 const names=['January','February','March','April','May','June','July','August','September','October','November','December'];
 const last=dates.at(-1),year=last?+last[3]:1960,month=last?names.indexOf(last[1])+1:1;
 const rows={};let header;
 for await(const line of readline.createInterface({input:fs.createReadStream(root+'-repertoire-slots.csv'),crlfDelay:Infinity})){
  const values=fields(line);if(!header){header=values;continue;}if(values.length!==header.length)continue;
  const r=Object.fromEntries(header.map((h,i)=>[h,values[i]]));
  if(+r.year>year||(+r.year===year&&+r.month>=month))continue;
  const key=[r.unsigned==='True'?'unsigned':'signed',r.genre,r.cohort??'all'].join('|');
  const m=rows[key]??={slots:0,numerator:0,originals:0,existing:0,families:{},origins:{},archetypes:{}};m.slots++;if(['establishedStandard','traditionalLineage'].includes(r.category))m.numerator++;if(r.category==='newlyAuthored')m.originals++;if(r.category==='existingCover')m.existing++;if(r.category!=='newlyAuthored'){for(const [name,value] of [['families',r.seedFamily||'ordinary'],['origins',r.originKind],['archetypes',r.archetype]])m[name][value]=(m[name][value]??0)+1;}
 }
 snapshots.push({prefix,lastDate:last?last.slice(1).join(' '):'starting',scope:'closed months only, provisional progress',rows:Object.entries(rows).filter(([k])=>/BossaNova|ContemporaryFolk|Country|Folk|Gospel|Jazz|RnB|SurfRock|EasyListening/.test(k)).map(([key,m])=>({key,...m,share:100*m.numerator/m.slots,originalShare:100*m.originals/m.slots}))});
}
console.log(JSON.stringify(snapshots,null,2));
