import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {analyzePolarResearch,csvRows} from './analyze-polar-research.mjs';
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const benchmark=['polar-research-none-check-1001','polar-research-sample-check-1001','polar-research-full-check-1001'];
const measured=benchmark.map(prefix=>({prefix,invocation:json(`SimLogs/${prefix}-invocation.json`),timings:csvRows(`SimLogs/${prefix}-polar-research-timing.csv`).rows}));
const sample=analyzePolarResearch(benchmark[1]),full=analyzePolarResearch(benchmark[2]),panel=analyzePolarResearch('polar-research-panel-check-1002');
const checkPaths=['SimLogs/polar-research-check-neutrality-polar-research-none-check-1001.json','SimLogs/polar-research-check-neutrality-polar-research-none-check-1002.json','SimLogs/polar-research-check-subset-polar-research-full-check-1001.json','SimLogs/polar-research-check-stopped-polar-research-stop-check-1001.json','SimLogs/polar-research-check-stopped-polar-research-kill-check-1001.json','SimLogs/polar-research-check-replay-polar-research-replay-a-1001.json'];
const checks=checkPaths.map(json);
for(const p of ['polar-research-kill-replay-1001','polar-research-budget-check-1001']) {
 const log=fs.readFileSync(`SimLogs/${p}.log`,'utf8');
 assert(log.includes(p==='polar-research-kill-replay-1001'?'POLAR_CHECKPOINT_ROUNDTRIP_PASS':'reason=time-budget'));
}
const census=m=>m.timings.find(r=>r.operation==='census');
const speedup=+census(measured[2]).seconds/(+census(measured[1]).seconds);
const sources=['SimTools/ChartAuditRunner.cs','SimTools/ChartAuditRunner.PolarFinal.cs','SimTools/ChartAuditRunner.PolarDiversity.cs','SimTools/ChartAuditRunner.PolarRepertoire.cs','SimTools/ChartAuditRunner.PolarResearch.cs','SimTools/run-polar-research.ps1','SimTools/run-polar-final-audit.ps1','SimTools/run-polar-followup-audit.ps1','SimTools/analyze-polar-research.mjs','SimTools/analyze-polar-repertoire.mjs','SimTools/check-polar-research.mjs','SimTools/check-polar-research-analyzer.mjs','SimTools/check-polar-research-kill.ps1','SimTools/write-polar-research-report.mjs','Data/PolarSongTable.json','Data/PolarRepertoireTable.json','.godot/mono/temp/bin/Debug/Label Man.dll'];
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const hashes=Object.fromEntries([...sources,...checkPaths,...measured.map(m=>`SimLogs/${m.prefix}-invocation.json`)].map(p=>[p,hash(p)]));
const evidence={date:'October 4, 2026',scope:'Research infrastructure and bounded validation only; no repertoire repair or long-run acceptance',definitions:sample.definitions,benchmark:measured,censusSpeedup:speedup,checks,panelCoverage:panel.coverage,panelTransitions:panel.panelTransitions,forcedCheckpointLoad:true,timeBudgetStop:true,hashes};
fs.writeFileSync('SimTools/PolarResearchInfrastructureValidation.json',JSON.stringify(evidence,null,2)+'\n');
const runtimeTable=measured.map(m=>{const c=census(m);return `| ${m.invocation.census} | ${c?.population??'—'} | ${c?.observed??'—'} | ${c?.candidateEntries??'—'} | ${c?(+c.seconds).toFixed(2):'disabled'} | ${m.invocation.elapsedSeconds.toFixed(2)} |`;}).join('\n');
const genreComparison=['Country','Folk','Gospel'].map(genre=>{const f=full.summaries.find(r=>r.period==='all'&&r.population==='signed'&&r.genre===genre),s=sample.summaries.find(r=>r.period==='all'&&r.population==='signed'&&r.genre===genre);return `| ${genre} | ${f.sampledActMonths} | ${s.sampledActMonths} | ${f.share.toFixed(2)} | ${s.share.toFixed(2)} |`;}).join('\n');
const text=`# Polar research infrastructure: bounded observation

October 4, 2026. Work package 0 infrastructure is implemented and its short-run acceptance checks pass. This report follows the interrupted-research report. The attached next directive was used as context; this work implements the agreed optimization and recovery workflow, not packages 1–5. No musical-fit weight, access constant, suitability window, historical band, taxonomy or repertoire policy was changed in this task. No hold-out run was launched or inspected.

## Measured runtime

The final-build comparison uses development seed 1001 and two simulation weeks, January 1–15, 1960. Each observation mode uses the same assembly and tables. Census timing includes the sample selection, live-set construction, slot/fit observations, output flushing and completion manifest; checkpoint cost is logged separately. Whole-process timing includes startup, simulation and checkpoints. These are single short measurements, not a four-year runtime prediction.

| Mode | Eligible population | Observed acts | Candidate entries traversed | Census seconds | Whole-process seconds |
| --- | ---: | ---: | ---: | ---: | ---: |
${runtimeTable}

Sampling reduces the measured candidate workload by ${(100*(1-(+census(measured[1]).candidateEntries)/(+census(measured[2]).candidateEntries))).toFixed(1)}%, and census time by ${(100*(1-1/speedup)).toFixed(1)}% (${speedup.toFixed(1)}× faster). Whole-process speedup is ${(measured[2].invocation.elapsedSeconds/measured[1].invocation.elapsedSeconds).toFixed(1)}×. Startup dominates these tiny windows. The census still evaluates each sampled act's real selection pool; fit semantics were preserved.

## What changed

- Monthly observation defaults to four acts per genre/cohort/signed-or-unsigned/writing-ability stratum. Smaller strata are observed completely. Sampling uses seed/artist hash ranks with no month in the hash and no global RNG consumption. The cap is configurable.
- The first sample becomes a persistent panel. Later censuses observe the current cross-sectional sample plus surviving panel members. Cross-sectional rows have population/sample weights; panel-only rows have zero population weight. Population share/fill estimates and within-act retention remain separate.
- Expensive additional full-candidate diagnostics are disabled by default. A separate per-month act budget enables them; one diagnostic act per genre/cohort/vocal-role/population is eligible within that budget. Requested/filled slots and selected-song observations are still collected for sampled acts.
- Slot and set CSVs carry cohort labels, writing bins, stratum counts, sample weights, cross-sectional membership and panel membership. New baseline runs receive the same cohort labels. Historical baseline files were preserved rather than relabeled retroactively.
- Admissions, ordering, weekly and other open audit writers flush before and after each census and every 16 observed acts. Completion manifests are written atomically after the corresponding rows are flushed. Only complete months enter weighted estimates.
- Atomic compressed world checkpoints are written before a census, after a completed census, on graceful stop and at normal completion. A forced kill retains the checkpoint from before the interrupted census. A graceful stop saves the current completed simulation boundary, closes writers and keeps partial observations for diagnosis.
- The preferred helper defaults to two weeks, sampled observation and a 300-second cooperative stop budget. Full observation is limited to eight weeks; development helpers accept only seeds 1001/1002. Existing helpers now also default to short, sampled, bounded work.

## Validation and denominators

Build passed with the four existing warnings. The analyzer's synthetic fixture verifies a weighted ratio that differs from the raw ratio, zero-weight panel supplementation, and exclusion of an incomplete month.

Seed 1001: full, sampled and disabled observation produce identical endpoint world state and 76 identical simulation CSV streams. All 918 selected slots for the 227 sampled acts match their full-census counterparts, including song identity, provenance and arrangement archetype.

Seed 1002: six simulation weeks, January 1–February 12, contain two completed census months. The sampled run, including two optional diagnostic acts per month, is identical to disabled observation in endpoint world state and 76 simulation CSV streams. It observes 240 acts initially and 380 in February, totaling 620 act-months and 2,475 filled slots. February's observed population is 3,211; its larger observation count includes the retained panel. Four ordering probes were written. There are 240 adjacent-month panel pairs: 31 change population, zero change genre, and 141 of the remaining pairs change filled set size. These are observer checks, not Gospel or retention acceptance results. The analyzer reports both variable-size and fixed-size pair estimates with their denominators.

Graceful interruption after 16 observations retains 8,083 admission rows, two ordering rows and a loadable checkpoint. Forced interruption occurs mid-full-census after at least 32 flushed observations; its last durable manifest records 48. It retains 8,083 admission rows, two ordering rows and the pre-census checkpoint. Both incomplete months are excluded from estimates. Weekly files are nonempty headers at this startup interruption, since no simulation week has yet completed.

Both interruption checkpoints pass lossless capture/load checks and advance a simulation week after loading. Loading the graceful-stop checkpoint twice produces identical continuation worlds. A separate one-second research-budget fixture reports a safe time-budget stop and writes its checkpoint without a completion marker. The stop budget is cooperative: an ongoing act/week operation and checkpoint serialization finish before exit.

## Sampling precision and remaining limits

The default cap is a fast diagnostic setting, not sufficient evidence for narrow numerical acceptance bands. January seed 1001 illustrates the sampling difference:

| Signed genre | Full acts | Sample acts | Full corrected share % | Weighted sample share % |
| --- | ---: | ---: | ---: | ---: |
${genreComparison}

The Gospel estimate differs by about seven percentage points at this small cap. Use the complete retained/fixed-world Gospel cohort for attribution, and increase the cap when precision matters. Do not tune against a sparse estimate or treat repeated act-months as independent worlds. Distinct-cover counts describe observed sample support; unseen rare compositions and full-population diversity are not estimated. Absent strata are explicitly listed (44 in the January seed-1001 frame, including writing-bin/population subdivisions); they are not successful zero-valued outcomes.

The existing save implementation restores global RNG by reseeding from world seed and chart week. Thus checkpoints support reproducible repeated-load research, but continuation is not guaranteed to match an uninterrupted trajectory byte for byte. Audit telemetry history is not resumed: a loaded snapshot starts a new audit prefix/window. Loaded worlds preserve composition admission-route metadata, while fresh-start admission events are cleared rather than mistaken for restored-world events.

No late-1961 workload benchmark, 1962–63 treatment run, historical cohort relabeling, hold-out confirmation, Gospel repair, retention attribution, recording-shape dispersion study or four-year acceptance is claimed. Work packages 1–5 remain separate research. The next bounded step is Gospel attribution using retained fit cases and identical candidate pools, with the wider sampled panel as a regression check.

## Running the workflow

From the workspace, use a fresh run prefix:

~~~powershell
.\\SimTools\\run-polar-research.ps1 -Run polar-next-1001 -Seed 1001
node SimTools/analyze-polar-research.mjs polar-next-1001
~~~

To enable at most eight diagnostic acts per month, add \`-DiagnosticActs 8\`. Increase sampling with \`-SamplePerStratum 8\`. For a short full validation use \`-Census full -Weeks 2\`; for simulation without census use \`-Census none\`.

For a manual graceful stop, create \`SimLogs/<run>.stop\` while the run is active. The helper reports a safe stop and keeps the checkpoint/partial evidence. To load a saved world into a fresh bounded audit:

~~~powershell
.\\SimTools\\run-polar-research.ps1 -Run polar-replay-next-1001 -Seed 1001 -Weeks 2 -LoadSnapshot 'SimLogs\\polar-next-1001-polar-checkpoint.json.gz' -VerifySnapshot
~~~

The sample-aware analyzer writes \`<run>-polar-research-analysis.json\` with the corrected numerator and denominator in its definitions header. The old repertoire analyzer rejects sampled/disabled observation instead of treating it as an unweighted full census. The legacy live-census CSV is emitted only in full mode and retains legacy categories for historical compatibility; it is not the source of corrected provenance estimates.

Evidence: [PolarResearchInfrastructureValidation.json](PolarResearchInfrastructureValidation.json), per-run invocation/timing/manifest/analysis files, and [check-polar-research.mjs](check-polar-research.mjs). Reproduce analyzer/metadata validation with \`node SimTools/check-polar-research-analyzer.mjs\`, and regenerate this report with \`node SimTools/write-polar-research-report.mjs\`. Raw interrupted and prior treatment evidence is preserved.
`;
fs.writeFileSync('SimTools/PolarResearchInfrastructureReport.md',text);
console.log(JSON.stringify({report:'SimTools/PolarResearchInfrastructureReport.md',validation:'SimTools/PolarResearchInfrastructureValidation.json',censusSpeedup:speedup,checks:checks.length}));
