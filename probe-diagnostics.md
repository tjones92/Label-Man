# Phase 3b probe diagnostics

Measured 2026-07-04 with Godot 4.7 Mono, 52 weeks, seeds 1001/1002/1003.

## Executive adjudication

The original Checkpoint A simulation is present locally and was used. The apparent Pearson failure of `0.381-0.397` is not the live `analyze-chart-audit.mjs` Pearson: it is a second, incompatible completed-lifecycle quality-versus-lifetime-units statistic from `analyze-album-project-pipeline.ps1`. On the byte-identical CKPT-A CSVs, the live peak-based Pearson is `0.506/0.509/0.584`. The Pearson guard therefore requires rebasing.

After rebasing to REF-3A measured by one analyzer, CKPT-A still loses an average `0.040` live Pearson and `1.67` closed Top-40 weeks. The dominant common mechanism is volume crowding. BASELINE and CKPT-A have nearly identical singles competition ratios (`4.247` and `4.241`), never-charted shares (`81.4%` and `81.5%`), and short median life (`10.83` and `10.00`). REF-3A is less crowded (`3.196`), has fewer never-charted casualties (`76.4%`), and lasts longer (`11.67`).

Gemini's youth-pool selection hypothesis is not supported as the primary cause. CKPT-A restored the adult completed-pool share from `5.8-7.7%` in REF-3A to `18.6-20.2%`, but degradation was not concentrated in youth. The age-capped and completed-only Pearson variants also degrade, so right censoring does not explain the rebased regression.

## Build identity and prerequisite

- Branch/base: `main` at `6cef775de6034ef8f8b5931db192c37e4b705204` (`roi logic and revenue memory`). There is no tag or CKPT branch.
- Preserved CKPT-A source: the uncommitted working tree on that base, specifically the recorded 106-line Checkpoint A diff in `Systems/CompetitorManager.cs`, `Systems/ChartManager.cs`, and `SimTools/ChartAuditRunner.cs`. It contains `compilationProductionMultiplier = 0.60f`, `priorSingleExpectedTailUnitScalar = 300000f`, the deterministic tail term, the four-resolvable-single proxy, and centralized compilation costing. It matches `SimTools/AlbumProjectPipelineAudit.md` line-for-line in behavior.
- Preserved pre-instrumentation binary path: `C:\Project\Label-Man\.godot\mono\temp\bin\Debug\Label Man.dll`; SHA-256 before the diagnostic-only rebuild: `907A5BCA022F24F6B9FBEABF29869D09ACAB6671CA8E6CD02764EDD4B054BC33`.
- Instrumented CKPT-A binary SHA-256: `9A3165772C1C760F0785D8DB06F142EDA569F6FFBBAFD4105104E76089C00AC0`. The only post-confirmation addition was `album-track-links.csv`; decision/economy/RNG code was unchanged.
- REF-3A/BASELINE build: a clean archive of commit `6cef775...`, with the same diagnostic-only track-link stream. Instrumented DLL SHA-256: `5AB994DCD787AFC91ECE7B4CB2E0DB8882F5547AFAC283F5A8791740DEC99859`.
- CKPT-A seed-1001 outputs reproduce the audit's recorded hashes exactly: `records.csv` `7C3C568135622E713769658F451E051F09C4EE50AB10C8FE3347C597F52227E8`, `release-strategy.csv` `7AFC01C393EB29E65136CA4CD1F1F8E1EF34F18F95E06E33B1BF572CF2275D42`, `live-records-snapshot.csv` `B180F8763FD3A8475B90FADF25553CEB922B8B3A39AAFC1F4B628887FE2BC9F7`, and `release-outcomes.csv` `BF649072913A563CAB341AEEA2F6843A6A69336329CED71AEECFF10A9D4AF9DB`.

This establishes that the preserved source/build is the original Checkpoint A simulation rather than an approximation.

## P1: reference fidelity

### P1a - BASELINE

BASELINE seed 1001 passes every published Phase 1 anchor exactly:

| Check | Measured | Expected | Result |
|---|---:|---:|---|
| Annual market units | 154,810,982 | 154,810,982 | Pass |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` | same | Pass |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` | same | Pass |

### P1b - REF-3A and rebase

| Config | Seed | Live Pearson (N) | Closed Top-40 median (N) | Legacy incompatible Pearson (N) |
|---|---:|---:|---:|---:|
| BASELINE | 1001 | 0.494 (983) | 10.5 (200) | 0.393 (2,746) |
| BASELINE | 1002 | 0.529 (994) | 11.0 (207) | 0.382 (2,831) |
| BASELINE | 1003 | 0.578 (1,001) | 11.0 (212) | 0.387 (2,906) |
| REF-3A | 1001 | 0.567 (927) | 12.0 (180) | 0.402 (1,926) |
| REF-3A | 1002 | 0.550 (967) | 11.0 (199) | 0.408 (2,028) |
| REF-3A | 1003 | 0.603 (935) | 12.0 (177) | 0.412 (2,028) |
| CKPT-A | 1001 | 0.506 (974) | 10.0 (218) | 0.381 (2,726) |
| CKPT-A | 1002 | 0.509 (1,000) | 10.0 (222) | 0.389 (2,795) |
| CKPT-A | 1003 | 0.584 (981) | 10.0 (209) | 0.397 (2,785) |

REF-3A is outside the old absolute bands in seed 1003 for Pearson (`0.603 > 0.595`) and seeds 1001/1003 for median (`12.0 > 11.5`). Per the directive, the reference is rebased to these REF-3A measurements. Paired CKPT-A deltas are Pearson `-0.061/-0.041/-0.019` and median `-2/-1/-2` weeks.

### Exact rule diff

- Live Pearson: one row per distinct record in `records.csv` that ever has `currentPosition > 0`, including records still active at week 52. Quality is the first observed `quality`; outcome is `101 - best observed currentPosition`. No completion or prewarm exclusion.
- Closed Top-40 median: lifecycle rows only, terminal `peakPosition` 1-40; outcome is terminal `weeksOnChart`. Live records are excluded.
- The `0.381-0.397` CKPT-A values in `AlbumProjectPipelineAudit.md` came from `analyze-album-project-pipeline.ps1`: lifecycle rows only, `leftCensoredAtRunStart != true`, quality joined from the first record row, outcome `lifetimeUnitsSold`. It uses neither the live Pearson population nor the peak-based outcome.

Pearson and median-life therefore do not share a population. The pipeline audit's statistic must not be labeled as the live guard.

## P2: cohort splits

Each cell below is `Pearson (N) / closed Top-40 median (N)`.

### Adult versus youth

| Config | Seed | Adult | Youth | Adult completed share |
|---|---:|---:|---:|---:|
| BASELINE | 1001 | 0.503 (288) / 11 (72) | 0.532 (695) / 10 (128) | 21.4% |
| BASELINE | 1002 | 0.520 (238) / 10 (69) | 0.574 (756) / 11 (138) | 20.6% |
| BASELINE | 1003 | 0.587 (279) / 10 (56) | 0.584 (722) / 11 (156) | 21.4% |
| REF-3A | 1001 | 0.705 (57) / 16 (23) | 0.556 (870) / 12 (157) | 7.7% |
| REF-3A | 1002 | 0.635 (55) / 14 (25) | 0.543 (912) / 11 (174) | 6.0% |
| REF-3A | 1003 | 0.497 (47) / 13.5 (14) | 0.610 (888) / 12 (163) | 5.8% |
| CKPT-A | 1001 | 0.558 (270) / 9 (79) | 0.537 (704) / 11 (139) | 20.2% |
| CKPT-A | 1002 | 0.542 (233) / 10 (77) | 0.544 (767) / 11 (145) | 19.0% |
| CKPT-A | 1003 | 0.648 (261) / 10 (57) | 0.573 (720) / 11 (152) | 18.6% |

The CKPT-A adult Pearson is not stably near REF-3A (`-0.147/-0.093/+0.151` paired), and the youth change is modest (`-0.019/+0.001/-0.037`). Median degradation is larger in adult than youth, the opposite of the proposed youth-drain signature.

### Career band at release

`Share` is share of the completed pool. Unexpected release states are shown as their own rows.

| Config | Seed | Band | Share | Pearson (N) | Median (N) |
|---|---:|---|---:|---:|---:|
| BASELINE | 1001 | New/Unsigned | 94.9% | 0.436 (692) | 11 (164) |
| BASELINE | 1001 | Rising | 4.3% | 0.664 (250) | 9.5 (32) |
| BASELINE | 1001 | Established | 0.2% | 0.751 (21) | 9 (4) |
| BASELINE | 1001 | Star/Superstar | 0% | 0.890 (5) | — (0) |
| BASELINE | 1001 | Dropped | 0.5% | 0.230 (15) | — (0) |
| BASELINE | 1001 | Declining | <0.1% | — (0) | — (0) |
| BASELINE | 1002 | New/Unsigned | 93.9% | 0.502 (686) | 11 (162) |
| BASELINE | 1002 | Rising | 5.0% | 0.713 (253) | 9 (39) |
| BASELINE | 1002 | Established | 0.4% | 0.691 (29) | 7 (6) |
| BASELINE | 1002 | Star/Superstar | 0% | 0.955 (3) | — (0) |
| BASELINE | 1002 | Dropped | 0.6% | 0.108 (20) | — (0) |
| BASELINE | 1002 | Declining | 0.1% | 0.854 (3) | — (0) |
| BASELINE | 1003 | New/Unsigned | 94.0% | 0.566 (701) | 11 (165) |
| BASELINE | 1003 | Rising | 4.9% | 0.716 (247) | 9 (42) |
| BASELINE | 1003 | Established | 0.2% | 0.780 (25) | 9 (5) |
| BASELINE | 1003 | Star/Superstar | 0% | 0.992 (3) | — (0) |
| BASELINE | 1003 | Dropped | 0.8% | 0.347 (20) | — (0) |
| BASELINE | 1003 | Declining | 0.1% | 0.091 (5) | — (0) |
| REF-3A | 1001 | New/Unsigned | 95.0% | 0.539 (691) | 13 (149) |
| REF-3A | 1001 | Rising | 4.3% | 0.774 (206) | 10 (29) |
| REF-3A | 1001 | Established | 0.1% | 0.845 (15) | 13 (2) |
| REF-3A | 1001 | Star/Superstar | 0% | 1.000 (2) | — (0) |
| REF-3A | 1001 | Dropped | 0.5% | 0.129 (13) | — (0) |
| REF-3A | 1002 | New/Unsigned | 93.3% | 0.508 (667) | 12 (169) |
| REF-3A | 1002 | Rising | 5.6% | 0.715 (256) | 8.5 (26) |
| REF-3A | 1002 | Established | 0.5% | 0.849 (23) | 9 (4) |
| REF-3A | 1002 | Star/Superstar | 0% | 1.000 (2) | — (0) |
| REF-3A | 1002 | Dropped | 0.6% | 0.635 (17) | — (0) |
| REF-3A | 1002 | Declining | 0.1% | 1.000 (2) | — (0) |
| REF-3A | 1003 | New/Unsigned | 94.4% | 0.598 (655) | 12 (138) |
| REF-3A | 1003 | Rising | 4.5% | 0.734 (239) | 11 (36) |
| REF-3A | 1003 | Established | 0.2% | 0.775 (21) | 12 (3) |
| REF-3A | 1003 | Star/Superstar | 0% | 0.447 (4) | — (0) |
| REF-3A | 1003 | Dropped | 0.8% | -0.155 (16) | — (0) |
| REF-3A | 1003 | Declining | 0.1% | — (0) | — (0) |
| CKPT-A | 1001 | New/Unsigned | 94.3% | 0.459 (691) | 11 (174) |
| CKPT-A | 1001 | Rising | 4.7% | 0.699 (243) | 8 (43) |
| CKPT-A | 1001 | Established | 0.2% | 0.651 (23) | 11 (1) |
| CKPT-A | 1001 | Star/Superstar | 0% | 0.986 (3) | — (0) |
| CKPT-A | 1001 | Dropped | 0.7% | 0.692 (12) | — (0) |
| CKPT-A | 1001 | Declining | <0.1% | 1.000 (2) | — (0) |
| CKPT-A | 1002 | New/Unsigned | 93.9% | 0.510 (687) | 11 (179) |
| CKPT-A | 1002 | Rising | 5.3% | 0.660 (270) | 8 (38) |
| CKPT-A | 1002 | Established | 0.2% | 0.627 (24) | 7 (4) |
| CKPT-A | 1002 | Star/Superstar | 0% | 1.000 (2) | — (0) |
| CKPT-A | 1002 | Dropped | 0.5% | 0.542 (15) | 8 (1) |
| CKPT-A | 1002 | Declining | <0.1% | 1.000 (2) | — (0) |
| CKPT-A | 1003 | New/Unsigned | 93.4% | 0.570 (653) | 11 (165) |
| CKPT-A | 1003 | Rising | 5.6% | 0.710 (269) | 8 (38) |
| CKPT-A | 1003 | Established | 0.4% | 0.779 (32) | 9.5 (6) |
| CKPT-A | 1003 | Star/Superstar | 0% | 0.705 (4) | — (0) |
| CKPT-A | 1003 | Dropped | 0.5% | 0.453 (22) | — (0) |
| CKPT-A | 1003 | Declining | 0.1% | — (1) | — (0) |

The completed statistic is inherently range-restricted: New/Unsigned is `93.3-95.0%` in every run. Established and Star/Superstar median samples are too sparse for inference.

### Completed-pool quality quartiles

Quartiles are rank-split within each run. Each cell is `N / pool share / mean lifetime units / Pearson / Top-40 median`.

| Config | Seed | Q1 | Q2 | Q3 | Q4 |
|---|---:|---:|---:|---:|---:|
| BASELINE | 1001 | 806 / 25.0% / 5,161 / — / — | 806 / 25.0% / 10,950 / 0.270 / 3 | 806 / 25.0% / 18,683 / 0.165 / 5 | 805 / 25.0% / 83,238 / 0.466 / 11 |
| BASELINE | 1002 | 830 / 25.0% / 5,334 / — / — | 829 / 25.0% / 10,794 / 0.209 / 7 | 830 / 25.0% / 19,512 / -0.033 / 5 | 829 / 25.0% / 87,612 / 0.405 / 11 |
| BASELINE | 1003 | 847 / 25.0% / 5,876 / — / — | 847 / 25.0% / 11,676 / -0.087 / 2.5 | 847 / 25.0% / 20,547 / -0.092 / 5 | 847 / 25.0% / 89,483 / 0.487 / 11 |
| REF-3A | 1001 | 601 / 25.0% / 5,087 / — / — | 601 / 25.0% / 10,909 / -0.058 / 1 | 601 / 25.0% / 19,424 / 0.032 / 6.5 | 600 / 25.0% / 99,039 / 0.521 / 13 |
| REF-3A | 1002 | 629 / 25.0% / 5,556 / — / — | 629 / 25.0% / 10,999 / 0.522 / 7 | 629 / 25.0% / 21,144 / 0.198 / 6 | 628 / 25.0% / 97,636 / 0.467 / 12 |
| REF-3A | 1003 | 628 / 25.0% / 5,549 / — / — | 627 / 25.0% / 11,308 / -0.041 / 1 | 628 / 25.0% / 20,103 / 0.138 / 7 | 627 / 25.0% / 104,652 / 0.542 / 12 |
| CKPT-A | 1001 | 801 / 25.0% / 5,434 / — / — | 801 / 25.0% / 10,883 / 0.272 / 2.5 | 801 / 25.0% / 19,274 / 0.069 / 5 | 800 / 25.0% / 87,552 / 0.404 / 11 |
| CKPT-A | 1002 | 821 / 25.0% / 5,641 / — / — | 820 / 25.0% / 10,988 / -0.054 / 7 | 821 / 25.0% / 20,122 / 0.122 / 5 | 820 / 25.0% / 92,257 / 0.437 / 11 |
| CKPT-A | 1003 | 817 / 25.0% / 5,702 / — / — | 817 / 25.0% / 11,900 / -0.363 / 1 | 817 / 25.0% / 20,718 / 0.084 / 6 | 816 / 25.0% / 89,811 / 0.514 / 11 |

The signal is carried disproportionately by Q4. Q1 has no charting variance in these completed pools, and Q2/Q3 correlations are unstable; this is direct range-restriction evidence.

## P3: crowding

| Config | Seed | Successful releases | Singles | Orphans | Chart entries | All ratio | Orphan ratio | Completed never-charted | Charted peak median | Top-40 median |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| BASELINE | 1001 | 4,439 | 4,439 | 4,439 | 1,056 | 4.204 | 4.204 | 81.2% (2,617/3,223) | 52 | 10.5 |
| BASELINE | 1002 | 4,492 | 4,492 | 4,492 | 1,064 | 4.222 | 4.222 | 81.1% (2,691/3,318) | 54 | 11 |
| BASELINE | 1003 | 4,595 | 4,595 | 4,595 | 1,065 | 4.315 | 4.315 | 81.9% (2,776/3,388) | 52 | 11 |
| REF-3A | 1001 | 4,096 | 3,186 | 3,185 | 1,040 | 3.063 | 3.062 | 75.4% (1,813/2,403) | 55 | 12 |
| REF-3A | 1002 | 4,273 | 3,339 | 3,337 | 1,040 | 3.211 | 3.209 | 76.5% (1,924/2,515) | 52 | 11 |
| REF-3A | 1003 | 4,434 | 3,407 | 3,406 | 1,028 | 3.314 | 3.313 | 77.3% (1,941/2,510) | 56 | 12 |
| CKPT-A | 1001 | 4,431 | 4,330 | 4,319 | 1,041 | 4.159 | 4.149 | 80.8% (2,587/3,203) | 53 | 10 |
| CKPT-A | 1002 | 4,578 | 4,465 | 4,443 | 1,072 | 4.165 | 4.145 | 81.5% (2,674/3,282) | 53 | 10 |
| CKPT-A | 1003 | 4,635 | 4,535 | 4,521 | 1,031 | 4.399 | 4.385 | 82.3% (2,690/3,267) | 53 | 10 |

“Never charted” has no numeric peak by definition; its distribution is the explicit 999/no-entry category. The charted subset's peak median is reported separately.

The directive's expected `~3,200-3,400 BASELINE releases` is actually the number of completed singles, not successful releases. The authoritative release-capacity stream reports `4,439-4,595` successful BASELINE releases. This matters: BASELINE is volume-comparable to CKPT-A, not REF-3A, and its crowding outcomes match CKPT-A.

Orphans are measured by exact `recordId` linkage. Albums mostly reuse prewarm catalog singles: only `1/2/1` current-year REF singles and `11/22/14` current-year CKPT singles were reused.

## P4: censoring sensitivity

| Config | Seed | Live as coded | Completed, peak | Live lower-bound substitution | Completed, release weeks 1-26 |
|---|---:|---:|---:|---:|---:|
| BASELINE | 1001 | 0.494 (983) | 0.463 (606) | 0.402 (983) | 0.464 (451) |
| BASELINE | 1002 | 0.529 (994) | 0.471 (627) | 0.355 (994) | 0.483 (455) |
| BASELINE | 1003 | 0.578 (1,001) | 0.528 (612) | 0.370 (1,001) | 0.517 (468) |
| REF-3A | 1001 | 0.567 (927) | 0.524 (590) | 0.412 (927) | 0.517 (414) |
| REF-3A | 1002 | 0.550 (967) | 0.510 (591) | 0.374 (967) | 0.566 (418) |
| REF-3A | 1003 | 0.603 (935) | 0.578 (569) | 0.366 (935) | 0.601 (422) |
| CKPT-A | 1001 | 0.506 (974) | 0.443 (616) | 0.408 (974) | 0.408 (462) |
| CKPT-A | 1002 | 0.509 (1,000) | 0.481 (608) | 0.297 (1,000) | 0.510 (441) |
| CKPT-A | 1003 | 0.584 (981) | 0.535 (577) | 0.385 (981) | 0.521 (448) |

The lower-bound variant follows the directive literally: completed records retain `101 - peak`, while live records substitute units-to-date. It mixes outcome units and is not suitable as a future gate, but is included as requested.

Mean REF-to-CKPT changes are `-0.040` live, `-0.051` completed-peak, `-0.021` literal lower-bound, and `-0.081` age-capped. The completed and age-capped variants do not recover; censoring is exonerated as the primary regression mechanism.

## P5: synthesis

| Guard | Candidate | Evidence for | Evidence against | Verdict |
|---|---|---|---|---|
| Pearson | Analyzer/population drift | The reported CKPT `0.381-0.397` exactly reproduces only the incompatible completed quality-vs-units formula. REF also misses the old absolute band in one seed. | One consistent live analyzer still shows a smaller paired CKPT loss in all seeds. | **Implicated** in the original failure magnitude; rebase required. |
| Pearson | Selection composition | REF adult completed share is only `5.8-7.7%`; CKPT changes population composition materially. | CKPT restores adult share to `18.6-20.2%`; loss is not youth-concentrated and adult comparisons are mixed. | **Exonerated** as the primary/Gemini mechanism. |
| Pearson | Crowding | REF ratio/never share `3.20/76.4%`; CKPT `4.24/81.5%`. BASELINE independently matches CKPT at `4.25/81.4%` and has the same mean live Pearson (`0.534` vs `0.533`). | This is a three-config observational comparison, not a fixed-composition volume intervention. | **Implicated**. |
| Pearson | Censoring artifact | Live and completed populations differ in level. | Completed and age-capped variants degrade at least as much as live. | **Exonerated**. |
| Median life | Analyzer/population drift | REF is `12/11/12`, outside the old absolute band twice; the band is stale for current main. | Median's lifecycle-only rule itself is unchanged and CKPT falls in every paired comparison. | **Exonerated** for the CKPT-relative loss; legacy absolute band is stale. |
| Median life | Selection composition | Adult composition changes sharply between REF and CKPT. | Adult median falls more (`14.5` mean to `9.67`) than youth (`11.67` to `11.0`), opposite the youth-drain prediction. | **Exonerated** as the proposed mechanism. |
| Median life | Crowding | BASELINE and CKPT have near-identical ratio, never-charted share, and median; REF is less crowded and longer-lived. Every seed follows the same direction. | No randomized fixed-composition volume intervention was run. | **Implicated**. |
| Median life | Censoring artifact | Median excludes all week-52 live records. | It is terminal lifecycle-only by construction; paired loss persists. | **Exonerated**. |

No candidate remains indeterminate. A stronger causal estimate for crowding, if desired, would require a diagnostic-only replay holding the release cohort fixed while varying admission volume; it is not necessary to distinguish the mechanisms in this directive.

## Diagnostic schema and validation

New stream: `{run}-album-track-links.csv`.

| Column | Meaning |
|---|---|
| `week`, `year` | First week the album is observed by the audit |
| `albumRecordId` | Album record identity |
| `artistId` | Album artist identity |
| `sourceRecordId` | Exact reused single record identity from `Album.trackRefs` |

The stream is write-only audit telemetry, consumes no RNG, and changes no existing CSV schema. `SimTools/analyze-3b-probe.mjs` writes `SimLogs/phase3b-probe-analysis.json` with all populations and raw measurements used above.

Validation:

- `dotnet build "Label Man.sln" --no-restore`: 0 errors; one pre-existing unused-event warning.
- BASELINE seed-1001 repeat: all 20 emitted CSVs byte-identical.
- REF-3A seed-1001 repeat: all 20 emitted CSVs byte-identical.
- CKPT-A seed-1001 repeat: all 22 emitted CSVs byte-identical.
- Godot completed every run and then emitted the pre-existing `MissingSingletonsTemp.cs` post-completion autoload warning.
