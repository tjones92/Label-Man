import http from 'node:http';
import fs from 'node:fs';
const file=new URL('./PolarShapeSampleSheet.html',import.meta.url);
http.createServer((req,res)=>{
 if(req.url!=='/'&&req.url!=='/PolarShapeSampleSheet.html'){res.writeHead(404);res.end();return;}
 res.writeHead(200,{'Content-Type':'text/html; charset=utf-8','Cache-Control':'no-store'});fs.createReadStream(file).pipe(res);
}).listen(8765,'127.0.0.1',()=>console.log('Sample sheet: http://127.0.0.1:8765/PolarShapeSampleSheet.html'));
