import fs from 'node:fs';
import assert from 'node:assert/strict';

// Writes the report from EasyJazzFollowUpValidation.json (same shape as the v1 Easy soundtrack report).
const v=JSON.parse(fs.readFileSync('SimLogs/EasyJazzFollowUpValidation.json','utf8'));
const tag=v.inputs[0].run.match(/^easy-jazz-(.+)-matched/)[1];
assert(v.checks.every(c=>c.passed),'All regression checks must pass before the report is written');
const fmt=x=>x==null?'absent':x.toFixed(2);
const L='C:/Project/Label-Man/',link=(p,n)=>`[${p}:${n}](${L}${p}:${n})`;
const easy=v.measures.filter(m=>m.genre==='EasyListening'),jazz=v.measures.filter(m=>m.genre==='Jazz');
const pop=m=>`| ${m.unsigned?'Unsigned':'Signed'} | ${m.seed} |`;
const ba=(m,k)=>`${fmt(m.before[k])}% → ${fmt(m.after[k])}%`;
const s=v.easyActSpread;
const lines=['# Easy Listening contemporary covers, Jazz lineups and Jazz film themes','',`October 5, 2026. Run tag ${tag}; development seeds 1001/1002.`,'',
 "Three changes, bundled at Alice's direction. (1) Jazz acts can now be instrumental: every Jazz lineup was generated around a lead vocalist. (2) Easy Listening reserves a share of its Contemporary-source slots for recent hits, ramped by era and leaned by act disposition; teen hits qualify only when big. (3) Instrumental Jazz acts take screen themes at a calibrated per-slot opportunity that replaces only non-inherited, non-media covers.",'',
 'Values signed off by Alice on the v3 measurement: jazzInstrumentalBandShare 0.85, jazzInstrumentalLeaderShare 0.50, jazzScreenOpportunity 0.34, Easy recentHitFloor 0.10 → recentHitCeiling 0.70 over 1962–1966, recentHitActLeanScale 0.08, teenRecentHitMinimumSize 0.70.','',
 '## Implementation','',
 `- ${link('Systems/ArtistManager.cs',375)}: keyed Jazz instrumental lineups (bands 85%, solo/duo 50%, vocal groups never), called at creation (${link('Systems/ArtistManager.cs',335)}). No population RNG consumed. Creation-time only: existing saved acts are not migrated, and acts that drift into Jazz keep their singer.`,
 `- ${link('Systems/LiveRepertoire.cs',18)}: comparator switch; disposition lean at line 23 (commercialPragmatism − rootsAttachment through a logistic, multiplier in (0,2)); hit-size gate at line 33.`,
 `- ${link('Systems/SongMaterialSelectionService.cs',685)}: Easy recent-hit channel inside the Contemporary source (draw at line 707). Soundtracks still compete on ranking for every other slot; no cap.`,
 `- ${link('Systems/SongMaterialSelectionService.cs',618)}: Jazz screen channel; replacement pass at line 626.`,
 `- ${link('Systems/PolarRepertoireTable.cs',68)}: era ramp on the live-set mix row; values in Data/PolarRepertoireTable.json.`,
 `- ${link('SimTools/ChartAuditRunner.GenreFollowUp.cs',67)}: census reference disables only the two selection channels (same world, same private census RNG).`,
 '- Snapshots: the lineup change is a world change, so the 1963 census starts from freshly generated trajectories (folk-easy-easy-jazz-v2; 1960 → 1962-10-05 for 1001 and 1962-11-02 for 1002, matching the old s5 dates).','',
 '## Matched results: Easy Listening','','| Population | Seed | Contemporary hits | Seeded recent hits | Screen | Stage/film | External total | Inherited | Pre-1940 |','|---|---|---:|---:|---:|---:|---:|---:|---:|'];
for(const m of easy)lines.push(`${pop(m)} ${['contemporaryHits','recentHits','screen','stageFilm','media','inherited','pre1940'].map(k=>ba(m,k)).join(' | ')} |`);
lines.push('',`Contemporary hits = Recent Pop/Teen/Country Hit seed families plus other acts' charted originals covered live. Per-act spread (after, ${s.acts} acts): ${s.zero} took none in the window; p75 ${fmt(s.p75)}%, p90 ${fmt(s.p90)}%, max ${fmt(s.max)}% of their set.`,
 '','## Matched results: Jazz','','| Population | Seed | Screen | Stage/film | External total | Inherited | Pre-1940 |','|---|---|---:|---:|---:|---:|---:|');
for(const m of jazz)lines.push(`${pop(m)} ${['screen','stageFilm','media','inherited','pre1940'].map(k=>ba(m,k)).join(' | ')} |`);
lines.push('',`Acceptance (3–5% screen): ${v.jazzAcceptance.map(a=>`${a.unsigned?'unsigned':'signed'} ${a.seed} ${fmt(a.screen)}% ${a.inScreenBand?'PASS':'MISS'}`).join('; ')}.`,
 '','Inherited delta is exactly 0 in every Jazz row by construction. Signed Jazz misses the band: signed acts play more of their own originals (22.3% of slots vs 16.5% unsigned), leaving fewer replaceable covers, and the signed sample has somewhat fewer instrumental acts (54% vs 61%, within sampling noise). One parameter cannot place both populations in band; Alice accepted the miss over adding a signed-specific rate.',
 '',`Jazz lineups in the sampled population: ${v.jazzInstrumental.instrumental} of ${v.jazzInstrumental.acts} acts instrumental (v1 world: ${v.v1JazzInstrumental.instrumental} of ${v.v1JazzInstrumental.acts}). Below the ~70% design share because the sample includes many solo acts (50%) and acts that drifted into Jazz.`,
 '','## World change (descriptive, cross-world)','',"v1 matched census (old world, v1 selector) against this run's reference (new Jazz lineups, v1 selector). Different worlds and samples, so this is descriptive, not a matched comparison.",'','| Genre | Population | v1 inherited | New-world inherited | v1 media | New-world media |','|---|---|---:|---:|---:|---:|');
for(const w of v.worldChange.filter(w=>w.seed==='pooled'))lines.push(`| ${w.genre} | ${w.unsigned?'Unsigned':'Signed'} | ${fmt(w.v1.inherited)}% | ${fmt(w.newWorldReference.inherited)}% | ${fmt(w.v1.media)}% | ${fmt(w.newWorldReference.media)}% |`);
lines.push('','Jazz inherited rises from 43.16% to 58.69% unsigned and 38.08% to 50.23% signed (pooled) with instrumental lineups. That is the lineup change, not the screen channel; it is the largest world-change movement and is unreviewed. Other genres move by a few points either way, as expected between two different worlds and samples. Gospel moves about −1 pp; Alice chose to skip the fresh Gospel trajectory and rely on this comparison.',
 '','## Full existing-cover family tables');
for(const m of [...easy,...jazz]) {
 lines.push('',`### ${m.genre}, ${m.unsigned?'unsigned':'signed'}, seed ${m.seed}`,'','| Family | Before | After |','|---|---:|---:|');
 for(const f of [...new Set([...Object.keys(m.before.existingCoverFamilies),...Object.keys(m.after.existingCoverFamilies)])].sort())lines.push(`| ${f} | ${fmt(m.before.existingCoverFamilies[f]??0)}% | ${fmt(m.after.existingCoverFamilies[f]??0)}% |`);
}
lines.push('','## Regression results','',`Other genres: ${v.otherGenresAccepted?'PASS':'FAIL'} for the 0.5 pp media and inherited limits; ${v.otherGenreMovements.every(m=>m.identical)?'every non-Easy/Jazz genre is bit-identical before and after':'not bit-identical'} (${v.otherGenreMovements.length} rows).`,
 'Build: dotnet build -c Debug --no-restore -v quiet passes with four existing warnings and no errors.','','| Check | Seed | Result |','|---|---|---|');
for(const c of v.checks)lines.push(`| ${c.check} | ${c.seed} | ${c.passed?'PASS':'FAIL'} |`);
lines.push('','directive3 runs with default shape flags; the other three use --composition-shape-v1=on --composition-shape-v2=on. New repair-check assertions: teen-hit size gate, era ramp, act-lean separation, no duplicate channel picks, Jazz lineup shares within 3 pp of the authored values, vocal groups stay vocal, screen themes never displace a standard, vocal Jazz keeps the cue bar, realized screen opportunity within 3 pp of the authored rate. The v1 merged-ranking assertion now runs with the comparator off.',
 '',"Gospel fresh trajectory: skipped at Alice's direction. The selection changes cannot reach Gospel (same-world Gospel is bit-identical); the cross-world shift is shown above.",
 '','Tuning history: the first measurement (tag v2: jazzScreenOpportunity 0.20, Easy ramp 0.08 → 0.45) gave Jazz screen 2.77% unsigned / 1.43% signed and Easy contemporary 4.36% / 3.23% pooled. Retained in EasyJazzFollowUpValidation-v2-initial.json.',
 '','## Source SHA-256 hashes','','| Source | SHA-256 |','|---|---|');
for(const x of v.sources)lines.push(`| ${x.path} | ${x.sha256} |`);
lines.push('','Complete per-genre deltas, invocation fingerprints, input hashes and check-log hashes are in EasyJazzFollowUpValidation.json. Partial census frames are excluded.');
fs.writeFileSync('SimLogs/EasyJazzFollowUpReport.md',lines.join('\n')+'\n');
console.log('wrote SimLogs/EasyJazzFollowUpReport.md');
