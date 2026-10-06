import fs from 'node:fs';
import {execFileSync} from 'node:child_process';
let source=fs.readFileSync('SimTools/analyze-polar-census209.mjs','utf8');
source=source.replace("process.argv[2]??'polar-census209'", "'polar-census-diversity-smoke'")
 .replace('for(const seed of [1001,1002])for(const mode of [\'off\',\'on\'])', "for(const seed of [1001])for(const mode of ['on'])")
 .replace('weeks=209','weeks=2').replace('weekly!==209','weekly!==2')
 .replace(/if\(months.size[^\n]+;/,'if(months.size!==1)throw Error("Wrong smoke month count");')
 .replaceAll('SimLogs/polar-census209-analysis.json','SimLogs/polar-census-smoke-validation.json')
 .replaceAll('SimLogs/polar-census209-diversity.csv','SimLogs/polar-census-smoke-validation.csv')
 .replace("scope:'209 weeks, seeds 1001/1002, off/on; monthly January 1960 through December 1963'", "scope:'Analyzer smoke validation: two weeks, enabled seed 1001, January 1960 only'")
 .replace("fs.writeFileSync('SimTools/PolarCensus209Report.md',report);", "// No full-census report is emitted by the smoke check.");
const temp='SimTools/census-analyzer-smoke.tmp.mjs';
try{fs.writeFileSync(temp,source);console.log(execFileSync(process.execPath,[temp],{encoding:'utf8'}));}finally{fs.unlinkSync(temp);}
