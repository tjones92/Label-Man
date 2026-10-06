import fs from 'node:fs';
import readline from 'node:readline';
for(const seed of [1001,1002])for(const mode of ['off','on']){
 const totals={unsignedMissing:0,allMissing:0,unsignedShort:0,allShort:0};const stats={};for(const genre of ['Country','EasyListening'])stats[genre]={genre,sets:0,slots:0,songs:new Set(),idTie:0,replacements:0};
 for(const suffix of ['sets','songs']){
  let keys;for await(const line of readline.createInterface({input:fs.createReadStream(`SimLogs/polar-census209-${mode}-${seed}-polar-diversity-${suffix}.csv`),crlfDelay:Infinity})){
   if(!keys){keys=line.split(',');continue;}const r=Object.fromEntries(line.split(',').map((v,i)=>[keys[i],v.replace(/^"|"$/g,'')]));if(+r.year>1960||+r.month>1)break;if(suffix==='sets'){totals.allMissing+=+r.requestedCovers - +r.filledCovers;totals.allShort+=r.shortSet==='True';if(r.unsigned==='True'){totals.unsignedMissing+=+r.requestedCovers - +r.filledCovers;totals.unsignedShort+=r.shortSet==='True';}}const m=stats[r.genre];if(!m)continue;
   if(suffix==='sets'){m.sets++;m.slots+=+r.filledCovers;m.idTie+=+r.idTieSelected;m.replacements+=+r.reverseIdChangedSlots;}else m.songs.add(r.songId);
  }
 }
 console.log(JSON.stringify({seed,mode,totals,January:Object.values(stats).map(({songs,...m})=>({...m,uniqueSongs:songs.size}))}));
}
