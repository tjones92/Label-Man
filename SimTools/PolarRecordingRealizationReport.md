# Polar recording realization calibration

October 2, 2026. Continues the calibrated rock sourcing slice. The user's current request authorizes controlled recording-realization experiments, tuning, and fresh-seed validation. The supplied directive/review are design context; their staged sign-off language is not a new approval requirement for this authorized pass. `UsePolarFitSelection` remains default-off.

## Experiment

All comparisons use 52 weeks, the real autoload chain, genre-market V2 and artist-population lifecycle. Development seeds are 1001/1002. Rock source policy, polar selection, album promotion/master reuse, release cadence, rights and genre acceptance stay fixed across the four individual ablations. Each switch removes just one consumer: hook ceiling, Capability loss, critic Identity adjustment or Moment conversion. An additional legacy-realization group provides an anchor; it restores the old source/application bridge as well as the grouped consumers and is not an independent fifth effect.

Changes run from population initialization through the completed year. Financial states, rosters, material pools and RNG traversal can diverge. These interventions cannot be added to obtain an exact causal decomposition. Quality means unique new single release lanes excluding `ExternalOrLegacy`; project counts and rock material shares also include the observed initial/prewarm cohort, matching the prior report. Label net and market units are sums of weekly flows; ending cash and statuses are endpoint stocks.

The probe table and four source hashes are saved under ignored `SimLogs/polar-realization-probe-*`. Probe runs precede the hook calibration. The final source/config hashes will be recorded separately.

## Recording contract

The earlier enabled path applied `min(performanceHook, compositionHook)` and then multiplied hook/production by the Capability execution factor. That retained material's downside but omitted the composition-to-performance upside of the legacy bridge. Album quality also took a blanket execution multiplier in addition to its changed hook and production.

The calibrated hook stage blends the generated performance hook toward the raw composition hook. A gradual ceiling constrains the remaining excess above that composition value. Capability still multiplies the resulting hook and production; Identity still adjusts critic craft; Moment still changes market conversion only with lagged evidence. No aggregate selection fit or composition craft is added to demand axes.

Both singles and album cuts use this hook stage. Album quality changes by the realized hook and production deltas at the existing single-quality weights, preserving the cut's other latent qualities. Promoted singles reuse the completed performance and its master. The disabled legacy calculation is untouched.

Full execution begins at Capability 0.90. Below it, the shortfall grows continuously to the existing maximum loss of 35% at zero Capability. Minor inferred deficits therefore stop imposing a tax on nearly every recording; severe mismatches still cost execution. Critic and Moment strengths are unchanged.

Final table values are `recordingHookBlend=0.90`, `hookCeilingStrength=0.15`, `executionFullCapability=0.90`. The album quality delta uses `recordingQualityHookWeight=0.50` and `recordingQualityProductionWeight=0.30`, copied from `RecordRuntimeData.GetQuality`; these are consistency weights, not new economy targets. `promotionRealizationWeight=0.65` multiplies autonomous promo fit by `lerp(1, completedPerformanceQuality, .65)` before the existing rock songbook factor. This reads the reusable cut's hook/production/danceability, not composition craft or a player-visible aggregate. The new values are calibration proposals implemented under this request, not historical estimates or claimed spec-mandated values.

This deliberately replaces the hard composition-hook ceiling with a soft constraint: a strong performance of weak material can exceed the composition's hook. Material still matters through the blend and gradual cap, and unsupported demands still reduce execution. This supports forced choices that can succeed without imposing a universal quality loss.

## Results and validation

The independent **pre-calibration** probes:

| Consumer removed | Singles 1001 / 1002 | Quality delta 1001 / 1002 | Label net delta 1001 / 1002 |
|---|---:|---:|---:|
| None (full enabled) | 4943 / 4865 | baseline .513343 / .513198 | baseline $137.325m / $137.834m |
| Hard hook ceiling | 4941 / 4948 | +.028246 / +.031132 | +5.59% / +8.92% |
| Capability penalty | 4878 / 4910 | +.005452 / +.005971 | +4.97% / −.08% |
| Critic adjustment | 4943 / 4865 | .000000 / .000000 | .00% / .00% |
| Moment conversion | 4836 / 4890 | +.000706 / +.003347 | +2.45% / +1.25% |
| Legacy realization group anchor | 5005 / 4933 | +.044666 / +.041478 | +8.34% / +5.98% |

The hook ceiling is the largest consistent **direct quality/financial** contributor measured. Single-count responses are mixed; removing Moment actually reduces volume on 1001. The critic probe changes critical readings in frozen fixtures but has no observed effect on the measured release/economy streams in this window. This does not prove critics can never affect a longer simulation.

The initial `.65` hook blend with the old Capability curve restored finances and volume but failed the engineering quality guardrail: quality .568381/.562030 versus disabled .610547/.608066. Singles were 4997/4974, label net $157.315m/$153.124m. Only development seeds were used to refine the blend and remove the near-capable execution tax. The first-candidate table and measurements remain archived; this failed gate is not hidden.

The `.90` blend/full-execution threshold refinement still failed quality on 1001 (.582560). Its orphan singles averaged .618475 quality, while promos averaged .528308: fit-only promo choice favored suitable weak performances over stronger completed cuts. The final calibration therefore includes completed-recording realization in promo choice, retaining fit, master reuse and the .02 rock exception factors. This is the additional consumer change required by the measured residual; no player UI or enabled playtest was performed.

Final development seeds both pass: singles 5076/5058 versus disabled 4983/5044 (+1.87%/+.28%); quality .635619/.624924 versus .610547/.608066; label net $165.992m/$167.869m versus $156.901m/$158.954m (+5.79%/+5.61%). Recording quality is not forced to match the old mean; the completed-cut preference improves which suitable performance becomes a promo.

Fresh validation uses seeds 1011/1012, previously absent from this polar audit archive; these were not reused for tuning. Before opening them, review guardrails were ±3% singles, no more than .02 absolute mean-quality loss, no more than 5% label-net loss against matched disabled, and nonzero pooled rock exceptions below 2% per seed. These are engineering guardrails for this pass, not user-specified historical tolerances. **Both fresh seeds pass every guardrail**, with no tuning after they were opened.

| Fresh seed / state | Singles | Albums | Release rolls | Mean new-single quality | Market units | Summed weekly label net | Ending label cash |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1011 disabled | 5074 | 1881 | 4767 | .604099 | 194.057m | $152.229m | $133.164m |
| 1011 calibrated | 5009 | 1805 | 4720 | .624428 | 196.768m | $165.200m | $144.913m |
| 1012 disabled | 4886 | 1896 | 4607 | .605838 | 195.221m | $153.382m | $135.104m |
| 1012 calibrated | 4919 | 1974 | 4650 | .635233 | 199.311m | $168.486m | $147.861m |

Single volume changes −1.28%/+.68%; label net +8.52%/+9.85%; ending cash +8.82%/+9.44%. Rock-family single material has 9/867 = **1.04%** songbook exceptions on 1011 (5 standards, 4 traditional) and 13/799 = **1.63%** on 1012 (7 standards, 6 traditional). Recent-hit covers remain ordinary sources: 457/867 and 452/799. No historical title or artist was whitelisted.

Financial improvement is aggregate, not uniform across labels. Endpoint statuses (disabled → calibrated):

| Seed | Rising | Stable | Struggling | Dying | Defunct |
|---|---:|---:|---:|---:|---:|
| 1011 | 427 → 416 | 48 → 49 | 2 → 1 | 43 → 58 | 99 → 95 |
| 1012 | 390 → 417 | 57 → 43 | 3 → 5 | 65 → 51 | 104 → 105 |

The 1011 increase in Dying labels remains a cohort-level observation for the next playtest/longer audit; it is not concealed by the higher net/cash totals. There is no claim that all label statuses improve. Both enabled fresh runs contain 106,496 bounded fit rows with zero invalid ranges/lag violations and valid telemetry budgets. Full quarter, hook/production, source/lane and financial summaries are in ignored `SimLogs/polar-realization-validation-summary.json`.

## Verification

Build succeeds with the four existing compatibility/unused-event warnings. Ownership/tag/save fixtures and F1–F4 fit/purity/determinism/lag fixtures pass. Calibrated recording, player behavior and rock songbook checks pass. The frozen realization fixture reports strong quality .760000, failed-execution quality .580333, weak-material hook .259500 (.270000 with the soft cap removed), critic craft .640000, and positive Moment conversion 1.050000. Its assertions isolate each consumer, compare single/cut realization, and verify completed-performance preference at equal fit.

Rock fixture results remain 40/1536 live-cover exceptions, 49/6144 album-plan exceptions, and 27/512 LP plans containing an exception. It checks exact/distinct slots, cross-project protection, unchanged Folk priors, and ordinary recent-hit access. No actual player live-set playtest is claimed.

The enabled two-week full-world round trip passes repeated serialization and gzip I/O: 107,745,230 JSON bytes / 7,891,940 compressed bytes, chartWeek 2. Both initial disabled 52-week controls are byte-identical across all 79 legacy streams to the prior shadow baseline. The final compiled calibration's seed-1001 disabled run also matches all 79 streams. The original-consumer replay matches the original full-enabled seed-1001 run across all 79 legacy streams, establishing reproducibility of the causal reference after calibration. Polar fit streams differ in mapping-version metadata as expected; their ranges, lag and budget are audited separately.

## Reproduction

Build Debug first; Godot loads the compiled assembly rather than the source files. See `HeadlessGodotExecutionNote.md` for the installed runtime path.

```powershell
./SimTools/run-polar-realization.ps1 -Godot $godot
node SimTools/analyze-polar-realization.mjs SimLogs probes
./SimTools/run-polar-realization.ps1 -Godot $godot -Cases off,calibrated -Seeds 1011,1012
node SimTools/analyze-polar-realization.mjs SimLogs validation
```

The probe cases use `--polar-fit-audit-original-realization` to reproduce the old hard ceiling, linear Capability curve, blanket album quality multiplier and fit-only promo preference in the current binary. New tuning constants are ignored on that path. The archived probe table documents the original numeric inputs. The runner refuses to overwrite an existing run family and validates the completion marker; new launches save table/assembly fingerprints and arguments beside their logs. `-Cases replay` checks the original full-enabled consumer path; `-Cases verified-off` checks the final disabled binary. Developer comparisons use `node SimTools/analyze-polar-realization.mjs SimLogs development`; earlier failed candidates remain available with modes `initial` and `refinement`.

Frozen-consumer fixture: `SaveLoadRoundTripRunner.tscn -- --weeks=0 --seed=1001 --polar-recording-realization-check`. Existing ownership/fit/behavior/rock fixtures and a full-world enabled save round trip are retained. Audit switches are headless diagnostics, not saved gameplay settings.

After this pass, the next milestone is an enabled player playtest of material selection, album tracks, live sets and promotion. Population fixtures do not substitute for that playtest, a four-year live-set census or decade-scale validation.

`PolarRecordingRealizationHashes.json` fingerprints the source/config, harness, analyzer and this report for the completed calibration. Earlier manifests continue to describe earlier slices. The flag remains default-off and no UI playtest or decade run was performed in this pass.
