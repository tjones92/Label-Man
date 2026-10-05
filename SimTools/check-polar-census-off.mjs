import fs from 'node:fs';
import {execFileSync} from 'node:child_process';
let source=fs.readFileSync('SimTools/analyze-polar-census209.mjs','utf8')
 .replace("for(const mode of ['off','on'])", "for(const mode of ['off'])")
 .replaceAll('SimLogs/polar-census209-analysis.json','SimLogs/polar-census209-off-validation.json')
 .replaceAll('SimLogs/polar-census209-diversity.csv','SimLogs/polar-census209-off-validation.csv')
 .replace("scope:'209 weeks, seeds 1001/1002, off/on; monthly January 1960 through December 1963'", "scope:'Completed off trajectories only: 209 weeks, seeds 1001/1002, 48 monthly censuses'")
 .replace("fs.writeFileSync('SimTools/PolarCensus209Report.md',report);", '// Full comparison report awaits the on runs.');
const temp='SimTools/census-off-validation.tmp.mjs';
try{fs.writeFileSync(temp,source);console.log(execFileSync(process.execPath,[temp],{encoding:'utf8',maxBuffer:1024*1024}));}finally{fs.unlinkSync(temp);}
