# Album project pipeline audit

Measured 2026-07-03 with Godot 4.7 Mono. Checkpoint A was implemented and measured for 52 weeks with seeds 1001, 1002, and 1003. The independent enabled seed-1001 repeat was byte-identical. Checkpoint A did **not** pass its hard gates, so Checkpoint B was not started.

## Checkpoint A implementation

- `ChartAuditRunner` writes a final `*-live-records-snapshot.csv` before closing its writers. It contains every active runtime record and defines `observedNetLowerBound` as `lifetimeLabelNet - sunkProductionCost`. These rows do not enter `release-outcomes.csv` or revenue memory.
- Album production cost is centralized in `CompetitorManager.CalculateProductionCost`. Singles use the label base cost, Compilation albums use the base cost times `compilationProductionMultiplier` plus actual packaging cost, and all other albums use 2.4 times base cost plus actual packaging cost. The same calculated amount is charged and stored as sunk production cost.
- `ChartManager.TryGetTrackSnapshot` provides a side-effect-free live/archive lookup. `HasFourResolvableSingles` traverses the artist's released single IDs in reverse and stops after four resolutions. It does not alter retired-track telemetry.
- The Album prior uses the four-resolvable-single proxy to select Compilation or regular production cost while retaining `priorAssumedAlbumPackaging`.
- The Single prior adds a deterministic risk-neutral expected-tail term. The term uses existing artist base quality, career-state stature, label distribution strength, and a bounded genre/year market factor computed from regional genre markets. It consumes no RNG and does not call `CalculateRecordQuality`.
- A new `*-prior-cost-assumptions.csv` stream records the prior's `assumedCompilationCost` flag and the generated album's actual format without modifying an existing CSV schema.
- `SimTools/analyze-album-project-pipeline.ps1` reproduces the A strategy, chart-composition, exact/censored error, career-band, and coverage calculations.

The enabled decision still computes Single then Album deterministically, blends label-local memory, and consumes exactly two projection-noise draws in Single/Album order. The album-disabled path returns Single before prior, memory, or RNG work.

## Settings and probes

| Setting | Starting value | Probe/final value |
|---|---:|---:|
| `priorUnitScalarSingle` | 12,000 | 12,000 |
| `priorUnitScalarAlbum` | 100,000 | 100,000 |
| `priorAssumedAlbumPackaging` | 0.50 | 0.50 |
| `compilationProductionMultiplier` | new | 0.60 |
| `priorSingleExpectedTailUnitScalar` | new | 300,000 |

The tail term uses quality to the fourth power so the correction is concentrated toward the expected hit tail rather than added uniformly to ordinary releases. A 3A-reference probe used tail scalar 0 and Compilation multiplier 2.4 only to measure the prior adult-singles population. The coherent A implementation and settings above were restored before the final build.

## Baseline and determinism

| Check | Result | Status |
|---|---:|---|
| Album-disabled seed-1001 units | 154,810,982 | Pass |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` | Pass |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` | Pass |
| Enabled seed-1001 repeat | All 21 emitted CSVs byte-identical | Pass |

Representative enabled seed-1001 hashes include `records.csv` `7C3C568135622E713769658F451E051F09C4EE50AB10C8FE3347C597F52227E8`, `release-strategy.csv` `7AFC01C393EB29E65136CA4CD1F1F8E1EF34F18F95E06E33B1BF572CF2275D42`, `live-records-snapshot.csv` `B180F8763FD3A8475B90FADF25553CEB922B8B3A39AAFC1F4B628887FE2BC9F7`, and `release-outcomes.csv` `BF649072913A563CAB341AEEA2F6843A6A69336329CED71AEECFF10A9D4AF9DB`.

## Checkpoint A hard gates

| Seed | Successful decisions | Album choice | Adult Album choice | Youth Album choice | Adult album-chart rows | Closed Top-40 median | Quality/outcome Pearson |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 4,431 | 2.28% (101) | 10.49% (99/944) | 0.00% (0/2,184) | 100.00% (1,742/1,742) | 10.0 | 0.381 |
| 1002 | 4,578 | 2.47% (113) | 12.24% (112/915) | 0.00% (0/2,395) | 100.00% (1,695/1,695) | 10.0 | 0.389 |
| 1003 | 4,635 | 2.16% (100) | 10.57% (99/937) | 0.00% (0/2,499) | 100.00% (1,716/1,716) | 10.0 | 0.397 |

The required overall Album band was 18%-28%, Adult Album band 45%-75%, Youth Album band 2%-15%, closed Top-40 median 11.0-11.5, and Pearson at least 0.50. All five gates failed in every seed. Adult album-chart composition passed its 95% gate. No youth album was selected, so the generated-youth-Compilation condition had no sample.

## Adult singles hypothesis

Adult means Jazz, Easy Listening, Folk, Traditional Pop, Bossa Nova, and Country. The A correction materially restored adult representation on the singles chart:

| Seed | 3A-reference adult singles-chart rows | Checkpoint A rows | Delta |
|---:|---:|---:|---:|
| 1001 | 4.96% (258/5,200) | 26.79% (1,393/5,200) | +21.83 pp |
| 1002 | 4.40% (229/5,200) | 24.96% (1,298/5,200) | +20.56 pp |
| 1003 | 4.10% (213/5,200) | 26.71% (1,389/5,200) | +22.62 pp |

Despite that restoration, Pearson was only 0.381-0.397, below both the 0.50 hard gate and the earlier 0.535-0.595 reference band. This falsifies the proposed selection-mechanism explanation for the Pearson regression in this probe. Per the directive's stop condition, demand, chart, retirement, and generation constants were not changed and Checkpoint B was not attempted.

## Exact and censored projection errors

Signed error is `projected - realized`. The censored statistic substitutes each live record's observed lower bound and is therefore an error ceiling, not a completed outcome or two-sided censoring correction.

| Seed | Format | Retired exact N | Live N | Unmatched | Exact mean signed error | All-cohort signed-error ceiling |
|---:|---|---:|---:|---:|---:|---:|
| 1001 | Single | 2,726 | 1,604 | 0 | -$2,466 | -$3,366 (N=4,330) |
| 1001 | Album | 0 | 101 | 0 | N/A | -$1,914 (N=101) |
| 1002 | Single | 2,795 | 1,670 | 0 | -$3,041 | -$3,172 (N=4,465) |
| 1002 | Album | 0 | 113 | 0 | N/A | -$195 (N=113) |
| 1003 | Single | 2,785 | 1,750 | 0 | -$1,830 | -$3,131 (N=4,535) |
| 1003 | Album | 0 | 100 | 0 | N/A | -$813 (N=100) |

The completed Single target of +/-$5,000 passed. No eligible Album completed within the 52-week horizon, so the completed Album calibration target cannot be claimed. The live Album lower bounds cannot prove a two-sided pass.

Completed Single error by career band:

| Seed | New/Unsigned | Rising | Established | Star/Superstar |
|---:|---:|---:|---:|---:|
| 1001 | -$1,630 (N=2,544) | -$16,430 (N=152) | -$12,126 (N=7) | N/A |
| 1002 | -$2,498 (N=2,595) | -$11,610 (N=174) | +$2,873 (N=7) | N/A |
| 1003 | -$982 (N=2,571) | -$8,964 (N=182) | -$76,119 (N=13) | N/A |

The sparse Established samples are unstable, and no completed Star/Superstar sample was available. The global Single correction therefore does not establish good calibration for every career band.

## Checkpoint B status

No Checkpoint B data model, scheduler, promotion snapshot, synergy, cannibalization, project memory routing, transfer handling, or telemetry was implemented. There are consequently no B RNG, reconciliation, transfer, memory-accounting, or launch-correlation results. This is intentional compliance with the A hard gate.

## Known limitations and build

- Retirement occurs before `CompetitorManager.ProcessWeeklyRevenue`; retirement-week revenue remains absent from `lifetimeLabelNet`. No synthetic settlement was added.
- The 52-week horizon left all selected A albums live, so exact Album signed error was unavailable.
- Live lower bounds are not terminal estimates and were not trained into memory.
- `dotnet build "Label Man.sln" --no-restore` succeeds with zero errors and the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning.
- Godot completes every audit successfully and then prints the pre-existing `MissingSingletonsTemp.cs` autoload warning after `CHART_AUDIT_COMPLETE`.

Checkpoint A did not pass. The implementation is left at the coherent A diagnostic checkpoint, and the album-project phase remains gated off.
