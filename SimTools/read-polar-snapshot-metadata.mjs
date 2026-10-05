import fs from 'node:fs';
import zlib from 'node:zlib';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';

// World snapshots can exceed V8's maximum string length. Read only header/settings metadata,
// streaming the entire gzip for integrity; never construct or parse the full world as a string.
export async function snapshotMetadata(path) {
 const names=['Year','Month','Day','CompletedAuditWeeks','ShapeVariationVersion'];
 const found={};let carry='';
 for await(const buffer of fs.createReadStream(path).pipe(zlib.createGunzip())) {
  const text=carry+buffer.toString('utf8');
  for(const name of names)if(found[name]===undefined){const m=text.match(new RegExp('"'+name+'"\\s*:\\s*(\\d+)\\s*[,}]'));if(m)found[name]=Number(m[1]);}
  carry=text.slice(-256);
 }
 for(const name of names)assert(Number.isInteger(found[name]),'Snapshot metadata absent: '+name);
 return {date:[found.Year,found.Month,found.Day],weeks:found.CompletedAuditWeeks,shape:found.ShapeVariationVersion};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href)console.log(JSON.stringify(await snapshotMetadata(process.argv[2])));
