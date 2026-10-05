# Polar repertoire repair and calibration
Generated 2026-10-04T03:10:00.552Z. Scope: January 1960–December 1963; unsigned and signed separate. Four-year acceptance requires 209 weeks and 48 monthly live censuses. Holdout 73019 remains unused; confirmation is pending.

All new taxonomy, assignment weights, repertoire access, preference, supply and historical bands remain provisional and unsigned. The user authorized implementation and approved seed 73019. The Phase 4 choice was separately approved with the requirement that fully original sets remain rare, with greater availability for composer-led Jazz. The attached directive supplies requirements; it does not independently authorize unrelated work.

## Measurement definitions and evidence
Each filled live slot belongs to exactly one category: newly authored, existing cover, established standard as of the observation year, or traditional lineage. Supplied unpublished professional material occupies a new-material slot. Public domain is an independent rights field. The calibration numerator is established standards plus traditional lineage; the denominator includes every filled live slot. Empty or refused slots are excluded and short-set counts are reported separately.

The legacy diagnosis definition (standard generation flag or traditional/public-domain) remains available for comparability. It is not the calibration numerator. Live selections, newly recorded masters and first-chart events are separate observations. Category sums are asserted against slot denominators during analysis.

Phase 0 neutrality: all 82 comparable pre-change and measurement-only smoke CSV streams were byte-identical for seed 1001. See `../SimLogs/polar-repertoire-phase0-neutrality.json`. Full baseline runs add the four-year provenance census for both seeds. The eight retained reference percentages below reproduce from `polar-model-diagnosis.json` using enabled seed 1001, unsigned, all filled slots, 1960–63. Input SHA-256 values are in `polar-repertoire-analysis.json`.

| Genre | Legacy reference | Reproduced | Numerator | Denominator |
| --- | --- | --- | --- | --- |
| BossaNova | 77.21% | 77.21% | 5782 | 7489 |
| ContemporaryFolk | 78.00% | 78.00% | 9184 | 11775 |
| Country | 76.60% | 76.60% | 78968 | 103088 |
| Folk | 75.80% | 75.80% | 43623 | 57548 |
| Gospel | 77.88% | 77.88% | 25371 | 32575 |
| Jazz | 76.31% | 76.31% | 36986 | 48466 |
| RnB | 45.14% | 45.14% | 43825 | 97089 |
| SurfRock | 2.31% | 2.31% | 486 | 21046 |

## Changes by phase and causal evidence

| Phase | Implementation symbols | Causal confirmation / limit |
| --- | --- | --- |
| 0 | RepertoireProvenance.Category / EstablishedAsOf; ChartAuditRunner.CaptureRepertoireAudit / FlushRepertoireAdmissions | Legacy semantics confound rights with lineage; 82 neutral streams; category partition asserted. |
| 1 | RepertoireTaxonomy.Assign; SongProfileDeriver.Derive; CompositionCatalogService.GenerateStandardFamily / GenerateRecentHitFamily; PolarCoverResolver.Propose | Same starting catalogue, actors and date: phase 0→1. Explicit tables replace fallback clones; persistent bounded offsets; 16 new rows. |
| 2 | LiveRepertoire.Pool / AccessWeight; CompositionCatalogService.RegisterCoverableHit | Same January world: phase 1→2. Regression covers 3/4/5/80 ordinary Easy Listening covers; deterministic/date-filtered/deduplicated access. |
| 3 | LiveRepertoire.Preference; PolarSongBehavior.RankLive / LiveProposal; SongMaterialSelectionService.SelectLiveCovers | Pre-switch reference/resolved A/B on all 3000 starting acts in both seeds. Same January world: phase 2→3. Resolved fit ranks non-rock live covers; stable preference replaces final ID sorting. Rock retains bounded source sampling and its existing source priors. |
| 4 | LiveRepertoire.OriginalCount / WritingPropensity; PlayerDesk.BuildLiveSet / ResolveMaterial | Same January world: phase 3→4; user-approved allocation with rare fully original books. |
| 5 | CompositionCatalogService.GenerateNativeRepertoire / AdmitUnpublished / OnRecordReleased / OnRecordChartRunComplete; SongMaterialApplicationService.ApplyIdentityToAlbumTrack; RepertoireProvenance.GameplayStandard | Same January world: phase 4→5; route-level admissions and inventory in full runs. Chart-completion admission remains. |
| 6 | Data/PolarRepertoireTable.json bands; analyze-polar-repertoire.mjs | Both tuning seeds; annual and aggregate cohort results below; no hard share quotas. Any unmet or unobserved cohort is explicitly reported. |

The phase probe advances repairs on the same 3000 January 1960 actors and catalogue without advancing time; actor IDs, genres, skills and career state are asserted unchanged. It is a causal same-world initial-state probe, not a retained monthly save replay. Four-year baseline/treatment trajectories may diverge in membership and chart outcomes. The original retained audits did not contain serialized monthly worlds; no fixed-cohort four-year causal claim is made.

| Seed / phase / population / genre | Standard+lineage / slots | Share | Original share | Top-ten cover share | Dominant selected demo archetype |
| --- | --- | --- | --- | --- | --- |
| 1001/0/signed/RockAndRoll | 25/1990 | 1.26% | 23.67% | 7.64% | MidTempoRocker: 1370 |
| 1001/0/signed/TraditionalPop | 581/1154 | 50.35% | 24.96% | 100.00% | LushStandard: 866 |
| 1001/0/signed/TeenPop | 0/1221 | 0.00% | 23.83% | 100.00% | BrightPopNumber: 930 |
| 1001/0/signed/RnB | 0/1758 | 0.00% | 24.35% | 100.00% | HornDrivenSoulNumber: 1330 |
| 1001/0/signed/Blues | 402/545 | 73.76% | 24.04% | 100.00% | SlowBlues: 414 |
| 1001/0/signed/Folk | 511/698 | 73.21% | 26.79% | 100.00% | VerseDrivenSong: 511 |
| 1001/0/signed/DooWop | 0/378 | 0.00% | 24.07% | 100.00% | HornDrivenSoulNumber: 287 |
| 1001/0/signed/Soul | 0/1724 | 0.00% | 25.17% | 100.00% | HornDrivenSoulNumber: 1290 |
| 1001/0/signed/Country | 445/751 | 59.25% | 23.04% | 100.00% | CountryTwoBeat: 578 |
| 1001/0/signed/Classical | 0/44 | 0.00% | 100.00% | unobserved |  |
| 1001/0/signed/EasyListening | 15/348 | 4.31% | 26.44% | 100.00% | BrightPopNumber: 234 |
| 1001/0/signed/Jazz | 301/467 | 64.45% | 23.13% | 100.00% | Swinger: 359 |
| 1001/0/signed/Gospel | 93/222 | 41.89% | 19.37% | 100.00% | SpiritualShout: 179 |
| 1001/0/unsigned/Classical | 0/24 | 0.00% | 100.00% | unobserved |  |
| 1001/0/unsigned/EasyListening | 7/249 | 2.81% | 23.69% | 98.42% | BrightPopNumber: 177 |
| 1001/0/signed/Comedy | 0/14 | 0.00% | 100.00% | unobserved |  |
| 1001/0/unsigned/LatinPop | 0/19 | 0.00% | 100.00% | unobserved |  |
| 1001/0/unsigned/TexMex | 0/4 | 0.00% | 100.00% | unobserved |  |
| 1001/0/unsigned/Childrens | 0/7 | 0.00% | 100.00% | unobserved |  |
| 1001/0/unsigned/Comedy | 0/5 | 0.00% | 100.00% | unobserved |  |
| 1001/0/signed/Childrens | 0/3 | 0.00% | 100.00% | unobserved |  |
| 1001/1/signed/RockAndRoll | 8/1990 | 0.40% | 23.67% | 10.86% | ShuffleTwelveBar: 1023 |
| 1001/1/signed/TraditionalPop | 365/1154 | 31.63% | 24.96% | 59.70% | CharmSong: 572 |
| 1001/1/signed/TeenPop | 0/1221 | 0.00% | 23.83% | 56.13% | MidTempoPopSong: 477 |
| 1001/1/signed/RnB | 227/1758 | 12.91% | 24.35% | 72.93% | ShuffleTwelveBar: 1227 |
| 1001/1/signed/Blues | 144/545 | 26.42% | 24.04% | 68.12% | ShuffleTwelveBar: 369 |
| 1001/1/signed/Folk | 511/698 | 73.21% | 26.79% | 87.87% | SlowBallad: 511 |
| 1001/1/signed/DooWop | 0/378 | 0.00% | 24.07% | 79.44% | SingAlongChant: 214 |
| 1001/1/signed/Soul | 213/1724 | 12.35% | 25.17% | 73.57% | ShuffleTwelveBar: 1208 |
| 1001/1/signed/Country | 361/751 | 48.07% | 23.04% | 65.40% | CountryTwoBeat: 350 |
| 1001/1/signed/Classical | 0/44 | 0.00% | 100.00% | unobserved |  |
| 1001/1/signed/EasyListening | 71/348 | 20.40% | 26.44% | 51.95% | MidTempoPopSong: 156 |
| 1001/1/signed/Jazz | 176/467 | 37.69% | 23.13% | 76.88% | CharmSong: 306 |
| 1001/1/signed/Gospel | 152/222 | 68.47% | 19.37% | 88.27% | QuietHymn: 150 |
| 1001/1/unsigned/Classical | 0/24 | 0.00% | 100.00% | unobserved |  |
| 1001/1/unsigned/EasyListening | 40/249 | 16.06% | 23.69% | 56.84% | MidTempoPopSong: 130 |
| 1001/1/signed/Comedy | 0/14 | 0.00% | 100.00% | unobserved |  |
| 1001/1/unsigned/LatinPop | 0/19 | 0.00% | 100.00% | unobserved |  |
| 1001/1/unsigned/TexMex | 0/4 | 0.00% | 100.00% | unobserved |  |
| 1001/1/unsigned/Childrens | 0/7 | 0.00% | 100.00% | unobserved |  |
| 1001/1/unsigned/Comedy | 0/5 | 0.00% | 100.00% | unobserved |  |
| 1001/1/signed/Childrens | 0/3 | 0.00% | 100.00% | unobserved |  |
| 1001/2/signed/RockAndRoll | 5/1990 | 0.25% | 23.67% | 15.01% | ShuffleTwelveBar: 1068 |
| 1001/2/signed/TraditionalPop | 362/1154 | 31.37% | 24.96% | 23.21% | CharmSong: 535 |
| 1001/2/signed/TeenPop | 2/1221 | 0.16% | 23.83% | 30.75% | MidTempoPopSong: 524 |
| 1001/2/signed/RnB | 147/1758 | 8.36% | 24.35% | 34.59% | ShuffleTwelveBar: 1214 |
| 1001/2/signed/Blues | 160/545 | 29.36% | 24.04% | 34.06% | ShuffleTwelveBar: 373 |
| 1001/2/signed/Folk | 235/698 | 33.67% | 26.79% | 14.68% | CountryWaltz: 318 |
| 1001/2/unsigned/Classical | 49/131 | 37.40% | 18.32% | 18.69% | CharmSong: 84 |
| 1001/2/signed/DooWop | 71/378 | 18.78% | 24.07% | 13.59% | ShuffleTwelveBar: 239 |
| 1001/2/signed/Soul | 286/1724 | 16.59% | 25.17% | 11.16% | ShuffleTwelveBar: 1133 |
| 1001/2/signed/Country | 338/751 | 45.01% | 23.04% | 26.12% | CountryTwoBeat: 307 |
| 1001/2/signed/Classical | 58/159 | 36.48% | 27.67% | 19.13% | CharmSong: 89 |
| 1001/2/signed/EasyListening | 97/348 | 27.87% | 26.44% | 16.02% | CharmSong: 146 |
| 1001/2/unsigned/Comedy | 2/29 | 6.90% | 17.24% | 45.83% | BrightPopNumber: 14 |
| 1001/2/signed/Jazz | 170/467 | 36.40% | 23.13% | 35.38% | CharmSong: 267 |
| 1001/2/unsigned/LatinPop | 15/81 | 18.52% | 23.46% | 25.81% | BrightPopNumber: 16 |
| 1001/2/signed/Gospel | 49/222 | 22.07% | 19.37% | 24.02% | ShuffleTwelveBar: 68 |
| 1001/2/unsigned/EasyListening | 63/249 | 25.30% | 23.69% | 16.32% | CharmSong: 90 |
| 1001/2/signed/Comedy | 15/60 | 25.00% | 23.33% | 28.26% | CharmSong: 18 |
| 1001/2/unsigned/TexMex | 6/17 | 35.29% | 23.53% | 76.92% | CharmSong: 5 |
| 1001/2/signed/Childrens | 4/17 | 23.53% | 17.65% | 71.43% | CharmSong: 6 |
| 1001/2/unsigned/Childrens | 1/23 | 4.35% | 30.43% | 62.50% | BrightPopNumber: 6 |
| 1001/2/signed/TexMex | 3/5 | 60.00% | 0.00% | 100.00% | BrightPopNumber: 2 |
| 1001/3/signed/RockAndRoll | 5/1990 | 0.25% | 23.67% | 15.21% | ShuffleTwelveBar: 1052 |
| 1001/3/signed/TraditionalPop | 443/1154 | 38.39% | 24.96% | 12.70% | ModernJazzInstrumental: 584 |
| 1001/3/signed/TeenPop | 82/1221 | 6.72% | 23.83% | 9.78% | BrightPopNumber: 302 |
| 1001/3/signed/RnB | 164/1758 | 9.33% | 24.35% | 5.11% | SlowBlues: 283 |
| 1001/3/signed/Blues | 189/545 | 34.68% | 24.04% | 9.18% | SlowBlues: 165 |
| 1001/3/signed/Folk | 321/698 | 45.99% | 26.79% | 8.22% | SlowBallad: 238 |
| 1001/3/unsigned/Classical | 61/131 | 46.56% | 18.32% | 12.15% | CharmSong: 33 |
| 1001/3/signed/DooWop | 41/378 | 10.85% | 24.07% | 10.45% | SingAlongChant: 60 |
| 1001/3/signed/Soul | 410/1724 | 23.78% | 25.17% | 3.80% | GrooveRiffVamp: 176 |
| 1001/3/signed/Country | 263/751 | 35.02% | 23.04% | 7.96% | SlowBallad: 131 |
| 1001/3/signed/Classical | 65/159 | 40.88% | 27.67% | 16.52% | ModernJazzInstrumental: 35 |
| 1001/3/signed/EasyListening | 155/348 | 44.54% | 26.44% | 8.98% | SlowBallad: 60 |
| 1001/3/unsigned/Comedy | 11/29 | 37.93% | 17.24% | 41.67% | CharmSong: 10 |
| 1001/3/signed/Jazz | 205/467 | 43.90% | 23.13% | 15.32% | ModernJazzInstrumental: 228 |
| 1001/3/unsigned/LatinPop | 24/81 | 29.63% | 23.46% | 17.74% | SlowBallad: 12 |
| 1001/3/signed/Gospel | 47/222 | 21.17% | 19.37% | 11.17% | ShuffleTwelveBar: 56 |
| 1001/3/unsigned/EasyListening | 107/249 | 42.97% | 23.69% | 11.05% | LushStandard: 45 |
| 1001/3/signed/Comedy | 25/60 | 41.67% | 23.33% | 21.74% | CharmSong: 22 |
| 1001/3/unsigned/TexMex | 7/17 | 41.18% | 23.53% | 76.92% | SlowBallad: 7 |
| 1001/3/signed/Childrens | 5/17 | 29.41% | 17.65% | 71.43% | CharmSong: 5 |
| 1001/3/unsigned/Childrens | 7/23 | 30.43% | 30.43% | 62.50% | ModernJazzInstrumental: 5 |
| 1001/3/signed/TexMex | 3/5 | 60.00% | 0.00% | 100.00% | CharmSong: 2 |
| 1001/4/signed/RockAndRoll | 3/1997 | 0.15% | 27.09% | 15.18% | ShuffleTwelveBar: 1009 |
| 1001/4/signed/TraditionalPop | 394/1108 | 35.56% | 30.51% | 12.86% | ModernJazzInstrumental: 521 |
| 1001/4/signed/TeenPop | 77/1230 | 6.26% | 29.43% | 9.79% | BrightPopNumber: 277 |
| 1001/4/signed/RnB | 147/1750 | 8.40% | 30.97% | 4.80% | SlowBlues: 267 |
| 1001/4/signed/Blues | 170/539 | 31.54% | 27.46% | 10.49% | SlowBlues: 158 |
| 1001/4/signed/Folk | 380/698 | 54.44% | 13.32% | 7.60% | SlowBallad: 282 |
| 1001/4/unsigned/Classical | 63/135 | 46.67% | 20.74% | 12.15% | CharmSong: 32 |
| 1001/4/signed/DooWop | 35/360 | 9.72% | 26.11% | 11.28% | SingAlongChant: 58 |
| 1001/4/signed/Soul | 370/1703 | 21.73% | 30.48% | 3.89% | GrooveRiffVamp: 162 |
| 1001/4/signed/Country | 230/737 | 31.21% | 31.61% | 8.53% | SlowBallad: 115 |
| 1001/4/signed/Classical | 62/158 | 39.24% | 31.65% | 15.74% | ModernJazzInstrumental: 33 |
| 1001/4/signed/EasyListening | 181/337 | 53.71% | 9.20% | 8.50% | LushStandard: 66 |
| 1001/4/unsigned/Comedy | 11/28 | 39.29% | 17.86% | 43.48% | CharmSong: 9 |
| 1001/4/signed/Jazz | 221/458 | 48.25% | 17.90% | 15.69% | ModernJazzInstrumental: 247 |
| 1001/4/unsigned/LatinPop | 26/83 | 31.33% | 26.51% | 18.03% | SlowBallad: 12 |
| 1001/4/signed/Gospel | 56/222 | 25.23% | 10.36% | 10.05% | ShuffleTwelveBar: 58 |
| 1001/4/unsigned/EasyListening | 132/252 | 52.38% | 6.75% | 10.21% | LushStandard: 61 |
| 1001/4/signed/Comedy | 23/56 | 41.07% | 26.79% | 24.39% | CharmSong: 22 |
| 1001/4/unsigned/TexMex | 7/18 | 38.89% | 27.78% | 76.92% | SlowBallad: 7 |
| 1001/4/signed/Childrens | 5/16 | 31.25% | 25.00% | 83.33% | CharmSong: 5 |
| 1001/4/unsigned/Childrens | 9/24 | 37.50% | 25.00% | 55.56% | ModernJazzInstrumental: 5 |
| 1001/4/signed/TexMex | 3/5 | 60.00% | 0.00% | 100.00% | CharmSong: 2 |
| 1001/5/signed/RockAndRoll | 19/1997 | 0.95% | 27.09% | 11.68% | ShuffleTwelveBar: 1004 |
| 1001/5/signed/TraditionalPop | 467/1108 | 42.15% | 30.51% | 12.86% | ModernJazzInstrumental: 518 |
| 1001/5/signed/TeenPop | 79/1230 | 6.42% | 29.43% | 7.95% | BrightPopNumber: 320 |
| 1001/5/signed/RnB | 195/1750 | 11.14% | 30.97% | 4.72% | SlowBlues: 257 |
| 1001/5/signed/Blues | 215/539 | 39.89% | 27.46% | 10.49% | SlowBlues: 159 |
| 1001/5/signed/Folk | 404/698 | 57.88% | 13.32% | 7.60% | SlowBallad: 285 |
| 1001/5/unsigned/Classical | 100/135 | 74.07% | 20.74% | 32.71% | ClassicalSolo: 63 |
| 1001/5/signed/DooWop | 37/360 | 10.28% | 26.11% | 11.28% | SingAlongChant: 57 |
| 1001/5/signed/Soul | 381/1703 | 22.37% | 30.48% | 4.22% | HornDrivenSoulNumber: 179 |
| 1001/5/signed/Country | 266/737 | 36.09% | 31.61% | 8.13% | SlowBallad: 116 |
| 1001/5/signed/Classical | 104/158 | 65.82% | 31.65% | 32.41% | ClassicalSolo: 60 |
| 1001/5/signed/EasyListening | 199/337 | 59.05% | 9.20% | 7.84% | SlowBallad: 64 |
| 1001/5/unsigned/Comedy | 10/28 | 35.71% | 17.86% | 69.57% | JauntyMusicHallRomp: 11 |
| 1001/5/signed/Jazz | 261/458 | 56.99% | 17.90% | 15.69% | ModernJazzInstrumental: 244 |
| 1001/5/unsigned/LatinPop | 38/83 | 45.78% | 26.51% | 27.87% | LatinBolero: 24 |
| 1001/5/signed/Gospel | 75/222 | 33.78% | 10.36% | 10.05% | ShuffleTwelveBar: 58 |
| 1001/5/unsigned/EasyListening | 137/252 | 54.37% | 6.75% | 10.21% | LushStandard: 63 |
| 1001/5/signed/Comedy | 19/56 | 33.93% | 26.79% | 51.22% | JauntyMusicHallRomp: 23 |
| 1001/5/unsigned/TexMex | 7/18 | 38.89% | 27.78% | 84.62% | TexMexSong: 8 |
| 1001/5/signed/Childrens | 12/16 | 75.00% | 25.00% | 83.33% | CharmSong: 7 |
| 1001/5/unsigned/Childrens | 18/24 | 75.00% | 25.00% | 66.67% | Novelty: 8 |
| 1001/5/signed/TexMex | 4/5 | 80.00% | 0.00% | 100.00% | TexMexSong: 3 |
| 1002/0/unsigned/EasyListening | 21/265 | 7.92% | 15.85% | 100.00% | BrightPopNumber: 192 |
| 1002/0/signed/Jazz | 252/576 | 43.75% | 29.17% | 100.00% | Swinger: 408 |
| 1002/0/signed/RockAndRoll | 20/1973 | 1.01% | 23.06% | 8.43% | MidTempoRocker: 1393 |
| 1002/0/signed/Country | 282/690 | 40.87% | 25.22% | 100.00% | CountryTwoBeat: 516 |
| 1002/0/signed/RnB | 0/1710 | 0.00% | 24.33% | 100.00% | HornDrivenSoulNumber: 1294 |
| 1002/0/signed/Blues | 238/362 | 65.75% | 21.27% | 100.00% | SlowBlues: 285 |
| 1002/0/signed/Soul | 0/1676 | 0.00% | 24.46% | 100.00% | HornDrivenSoulNumber: 1266 |
| 1002/0/signed/TraditionalPop | 525/1062 | 49.44% | 24.86% | 100.00% | LushStandard: 798 |
| 1002/0/signed/TeenPop | 0/1306 | 0.00% | 24.12% | 100.00% | BrightPopNumber: 991 |
| 1002/0/unsigned/LatinPop | 0/28 | 0.00% | 100.00% | unobserved |  |
| 1002/0/signed/DooWop | 0/478 | 0.00% | 21.55% | 100.00% | HornDrivenSoulNumber: 375 |
| 1002/0/signed/Folk | 458/621 | 73.75% | 26.25% | 100.00% | VerseDrivenSong: 458 |
| 1002/0/signed/EasyListening | 17/323 | 5.26% | 27.86% | 100.00% | BrightPopNumber: 209 |
| 1002/0/unsigned/Classical | 0/36 | 0.00% | 100.00% | unobserved |  |
| 1002/0/signed/Gospel | 129/165 | 78.18% | 20.61% | 100.00% | SpiritualShout: 131 |
| 1002/0/signed/Classical | 0/55 | 0.00% | 100.00% | unobserved |  |
| 1002/0/unsigned/Childrens | 0/13 | 0.00% | 100.00% | unobserved |  |
| 1002/0/signed/Comedy | 0/19 | 0.00% | 100.00% | unobserved |  |
| 1002/0/signed/TexMex | 0/2 | 0.00% | 100.00% | unobserved |  |
| 1002/0/signed/Childrens | 0/11 | 0.00% | 100.00% | unobserved |  |
| 1002/0/unsigned/Comedy | 0/8 | 0.00% | 100.00% | unobserved |  |
| 1002/0/unsigned/TexMex | 0/4 | 0.00% | 100.00% | unobserved |  |
| 1002/1/unsigned/EasyListening | 34/265 | 12.83% | 15.85% | 51.57% | MidTempoPopSong: 113 |
| 1002/1/signed/Jazz | 316/576 | 54.86% | 29.17% | 74.75% | CharmSong: 344 |
| 1002/1/signed/RockAndRoll | 21/1973 | 1.06% | 23.06% | 10.67% | ShuffleTwelveBar: 971 |
| 1002/1/signed/Country | 317/690 | 45.94% | 25.22% | 62.98% | CountryTwoBeat: 280 |
| 1002/1/signed/RnB | 0/1710 | 0.00% | 24.33% | 73.72% | ShuffleTwelveBar: 1212 |
| 1002/1/signed/Blues | 180/362 | 49.72% | 21.27% | 70.53% | ShuffleTwelveBar: 249 |
| 1002/1/signed/Soul | 0/1676 | 0.00% | 24.46% | 74.96% | ShuffleTwelveBar: 1181 |
| 1002/1/signed/TraditionalPop | 164/1062 | 15.44% | 24.86% | 57.64% | CharmSong: 570 |
| 1002/1/signed/TeenPop | 0/1306 | 0.00% | 24.12% | 56.41% | BrightPopNumber: 518 |
| 1002/1/unsigned/LatinPop | 0/28 | 0.00% | 100.00% | unobserved |  |
| 1002/1/signed/DooWop | 0/478 | 0.00% | 21.55% | 84.53% | SingAlongChant: 287 |
| 1002/1/signed/Folk | 458/621 | 73.75% | 26.25% | 85.81% | SlowBallad: 458 |
| 1002/1/signed/EasyListening | 30/323 | 9.29% | 27.86% | 52.79% | MidTempoPopSong: 134 |
| 1002/1/unsigned/Classical | 0/36 | 0.00% | 100.00% | unobserved |  |
| 1002/1/signed/Gospel | 98/165 | 59.39% | 20.61% | 84.73% | QuietHymn: 114 |
| 1002/1/signed/Classical | 0/55 | 0.00% | 100.00% | unobserved |  |
| 1002/1/unsigned/Childrens | 0/13 | 0.00% | 100.00% | unobserved |  |
| 1002/1/signed/Comedy | 0/19 | 0.00% | 100.00% | unobserved |  |
| 1002/1/signed/TexMex | 0/2 | 0.00% | 100.00% | unobserved |  |
| 1002/1/signed/Childrens | 0/11 | 0.00% | 100.00% | unobserved |  |
| 1002/1/unsigned/Comedy | 0/8 | 0.00% | 100.00% | unobserved |  |
| 1002/1/unsigned/TexMex | 0/4 | 0.00% | 100.00% | unobserved |  |
| 1002/2/unsigned/EasyListening | 95/265 | 35.85% | 15.85% | 21.52% | CharmSong: 144 |
| 1002/2/signed/Jazz | 231/576 | 40.10% | 29.17% | 34.80% | CharmSong: 295 |
| 1002/2/signed/RockAndRoll | 4/1973 | 0.20% | 23.06% | 15.02% | ShuffleTwelveBar: 1062 |
| 1002/2/signed/Country | 253/690 | 36.67% | 25.22% | 28.68% | CountryTwoBeat: 255 |
| 1002/2/signed/RnB | 73/1710 | 4.27% | 24.33% | 36.24% | ShuffleTwelveBar: 1198 |
| 1002/2/signed/Blues | 180/362 | 49.72% | 21.27% | 39.30% | ShuffleTwelveBar: 256 |
| 1002/2/signed/Soul | 363/1676 | 21.66% | 24.46% | 9.48% | ShuffleTwelveBar: 1117 |
| 1002/2/signed/TraditionalPop | 260/1062 | 24.48% | 24.86% | 25.19% | CharmSong: 540 |
| 1002/2/signed/TeenPop | 1/1306 | 0.08% | 24.12% | 27.85% | MidTempoPopSong: 494 |
| 1002/2/unsigned/LatinPop | 15/123 | 12.20% | 22.76% | 21.05% | MidTempoPopSong: 46 |
| 1002/2/signed/DooWop | 111/478 | 23.22% | 21.55% | 11.73% | ShuffleTwelveBar: 313 |
| 1002/2/signed/Folk | 203/621 | 32.69% | 26.25% | 17.69% | CountryWaltz: 299 |
| 1002/2/signed/EasyListening | 76/323 | 23.53% | 27.86% | 19.31% | CharmSong: 138 |
| 1002/2/unsigned/Classical | 78/193 | 40.41% | 18.65% | 14.01% | CharmSong: 125 |
| 1002/2/signed/Comedy | 14/73 | 19.18% | 26.03% | 20.37% | BrightPopNumber: 21 |
| 1002/2/signed/Gospel | 26/165 | 15.76% | 20.61% | 22.90% | ShuffleTwelveBar: 59 |
| 1002/2/signed/Classical | 72/198 | 36.36% | 27.78% | 15.38% | CharmSong: 102 |
| 1002/2/unsigned/Childrens | 5/48 | 10.42% | 27.08% | 34.29% | BrightPopNumber: 18 |
| 1002/2/signed/TexMex | 2/12 | 16.67% | 16.67% | 100.00% | MidTempoPopSong: 7 |
| 1002/2/signed/Childrens | 8/36 | 22.22% | 30.56% | 44.00% | BrightPopNumber: 9 |
| 1002/2/unsigned/Comedy | 5/37 | 13.51% | 21.62% | 37.93% | CharmSong: 10 |
| 1002/2/unsigned/TexMex | 1/15 | 6.67% | 26.67% | 100.00% | BrightPopNumber: 5 |
| 1002/3/unsigned/EasyListening | 134/265 | 50.57% | 15.85% | 9.87% | SlowBallad: 55 |
| 1002/3/signed/Jazz | 235/576 | 40.80% | 29.17% | 14.46% | ModernJazzInstrumental: 239 |
| 1002/3/signed/RockAndRoll | 6/1973 | 0.30% | 23.06% | 13.83% | ShuffleTwelveBar: 1055 |
| 1002/3/signed/Country | 226/690 | 32.75% | 25.22% | 7.95% | SlowBallad: 93 |
| 1002/3/signed/RnB | 176/1710 | 10.29% | 24.33% | 5.49% | SlowBlues: 285 |
| 1002/3/signed/Blues | 150/362 | 41.44% | 21.27% | 11.58% | SlowBlues: 103 |
| 1002/3/signed/Soul | 396/1676 | 23.63% | 24.46% | 3.87% | GrooveRiffVamp: 166 |
| 1002/3/signed/TraditionalPop | 478/1062 | 45.01% | 24.86% | 11.28% | ModernJazzInstrumental: 518 |
| 1002/3/signed/TeenPop | 90/1306 | 6.89% | 24.12% | 8.78% | BrightPopNumber: 330 |
| 1002/3/unsigned/LatinPop | 41/123 | 33.33% | 22.76% | 14.74% | SlowBallad: 30 |
| 1002/3/signed/DooWop | 45/478 | 9.41% | 21.55% | 11.47% | SingAlongChant: 72 |
| 1002/3/signed/Folk | 302/621 | 48.63% | 26.25% | 8.95% | SlowBallad: 224 |
| 1002/3/signed/EasyListening | 140/323 | 43.34% | 27.86% | 11.16% | ModernJazzInstrumental: 57 |
| 1002/3/unsigned/Classical | 85/193 | 44.04% | 18.65% | 12.74% | CharmSong: 43 |
| 1002/3/signed/Comedy | 22/73 | 30.14% | 26.03% | 20.37% | CharmSong: 14 |
| 1002/3/signed/Gospel | 40/165 | 24.24% | 20.61% | 16.79% | ShuffleTwelveBar: 49 |
| 1002/3/signed/Classical | 94/198 | 47.47% | 27.78% | 13.99% | ModernJazzInstrumental: 39 |
| 1002/3/unsigned/Childrens | 12/48 | 25.00% | 27.08% | 28.57% | CharmSong: 13 |
| 1002/3/signed/TexMex | 6/12 | 50.00% | 16.67% | 100.00% | SlowBallad: 3 |
| 1002/3/signed/Childrens | 12/36 | 33.33% | 30.56% | 40.00% | CharmSong: 13 |
| 1002/3/unsigned/Comedy | 16/37 | 43.24% | 21.62% | 34.48% | CharmSong: 10 |
| 1002/3/unsigned/TexMex | 5/15 | 33.33% | 26.67% | 90.91% | BrightPopNumber: 3 |
| 1002/4/unsigned/EasyListening | 148/266 | 55.64% | 5.64% | 8.37% | SlowBallad: 60 |
| 1002/4/signed/Jazz | 273/585 | 46.67% | 21.03% | 14.72% | ModernJazzInstrumental: 275 |
| 1002/4/signed/RockAndRoll | 8/1976 | 0.40% | 25.56% | 14.07% | ShuffleTwelveBar: 1026 |
| 1002/4/signed/Country | 195/706 | 27.62% | 34.42% | 8.42% | SlowBallad: 87 |
| 1002/4/signed/RnB | 162/1710 | 9.47% | 30.00% | 5.85% | SlowBlues: 269 |
| 1002/4/signed/Blues | 135/346 | 39.02% | 25.72% | 12.06% | SlowBlues: 98 |
| 1002/4/signed/Soul | 361/1700 | 21.24% | 30.65% | 4.07% | SpiritualShout: 152 |
| 1002/4/signed/TraditionalPop | 443/1082 | 40.94% | 30.41% | 11.29% | ModernJazzInstrumental: 496 |
| 1002/4/signed/TeenPop | 86/1289 | 6.67% | 29.56% | 9.47% | BrightPopNumber: 303 |
| 1002/4/unsigned/LatinPop | 38/118 | 32.20% | 27.97% | 16.47% | SlowBallad: 28 |
| 1002/4/signed/DooWop | 49/487 | 10.06% | 23.41% | 10.99% | SingAlongChant: 70 |
| 1002/4/signed/Folk | 370/632 | 58.54% | 12.03% | 7.91% | SlowBallad: 269 |
| 1002/4/signed/EasyListening | 178/332 | 53.61% | 10.84% | 9.12% | ModernJazzInstrumental: 63 |
| 1002/4/unsigned/Classical | 82/189 | 43.39% | 21.16% | 12.08% | CharmSong: 44 |
| 1002/4/signed/Comedy | 22/76 | 28.95% | 28.95% | 20.37% | CharmSong: 16 |
| 1002/4/signed/Gospel | 44/163 | 26.99% | 11.04% | 15.86% | ShuffleTwelveBar: 51 |
| 1002/4/signed/Classical | 91/201 | 45.27% | 33.33% | 14.93% | ModernJazzInstrumental: 41 |
| 1002/4/unsigned/Childrens | 10/43 | 23.26% | 30.23% | 33.33% | CharmSong: 10 |
| 1002/4/signed/TexMex | 5/12 | 41.67% | 25.00% | 100.00% | SlowBallad: 3 |
| 1002/4/signed/Childrens | 9/34 | 26.47% | 41.18% | 50.00% | CharmSong: 11 |
| 1002/4/unsigned/Comedy | 16/35 | 45.71% | 22.86% | 37.04% | CharmSong: 10 |
| 1002/4/unsigned/TexMex | 5/18 | 27.78% | 33.33% | 83.33% | BrightPopNumber: 4 |
| 1002/5/unsigned/EasyListening | 155/266 | 58.27% | 5.64% | 8.37% | LushStandard: 58 |
| 1002/5/signed/Jazz | 320/585 | 54.70% | 21.03% | 14.72% | ModernJazzInstrumental: 272 |
| 1002/5/signed/RockAndRoll | 14/1976 | 0.71% | 25.56% | 10.94% | ShuffleTwelveBar: 997 |
| 1002/5/signed/Country | 233/706 | 33.00% | 34.42% | 7.99% | SlowBallad: 89 |
| 1002/5/signed/RnB | 201/1710 | 11.75% | 30.00% | 5.76% | SlowBlues: 258 |
| 1002/5/signed/Blues | 146/346 | 42.20% | 25.72% | 11.67% | SlowBlues: 100 |
| 1002/5/signed/Soul | 399/1700 | 23.47% | 30.65% | 4.07% | HornDrivenSoulNumber: 144 |
| 1002/5/signed/TraditionalPop | 496/1082 | 45.84% | 30.41% | 11.29% | ModernJazzInstrumental: 492 |
| 1002/5/signed/TeenPop | 92/1289 | 7.14% | 29.56% | 7.93% | BrightPopNumber: 323 |
| 1002/5/unsigned/LatinPop | 48/118 | 40.68% | 27.97% | 23.53% | LatinBolero: 37 |
| 1002/5/signed/DooWop | 50/487 | 10.27% | 23.41% | 10.99% | SingAlongChant: 70 |
| 1002/5/signed/Folk | 392/632 | 62.03% | 12.03% | 7.91% | SlowBallad: 274 |
| 1002/5/signed/EasyListening | 182/332 | 54.82% | 10.84% | 9.12% | LushStandard: 63 |
| 1002/5/unsigned/Classical | 134/189 | 70.90% | 21.16% | 29.53% | ClassicalSolo: 80 |
| 1002/5/signed/Comedy | 23/76 | 30.26% | 28.95% | 44.44% | Novelty: 25 |
| 1002/5/signed/Gospel | 55/163 | 33.74% | 11.04% | 15.86% | ShuffleTwelveBar: 51 |
| 1002/5/signed/Classical | 127/201 | 63.18% | 33.33% | 28.36% | ClassicalSolo: 70 |
| 1002/5/unsigned/Childrens | 25/43 | 58.14% | 30.23% | 43.33% | Novelty: 15 |
| 1002/5/signed/TexMex | 7/12 | 58.33% | 25.00% | 100.00% | TexMexSong: 5 |
| 1002/5/signed/Childrens | 19/34 | 55.88% | 41.18% | 80.00% | CharmSong: 11 |
| 1002/5/unsigned/Comedy | 15/35 | 42.86% | 22.86% | 55.56% | JauntyMusicHallRomp: 15 |
| 1002/5/unsigned/TexMex | 11/18 | 61.11% | 33.33% | 91.67% | LatinBolero: 8 |

### Same-world pool size

| Seed / phase / population / genre | Acts | Mean eligible | Empty books | Filled covers | Originals |
| --- | --- | --- | --- | --- | --- |
| 1001/0/signed/RockAndRoll | 497 | 1290.00 | 0 | 1519 | 471 |
| 1001/0/signed/TraditionalPop | 285 | 1200.00 | 0 | 866 | 288 |
| 1001/0/signed/TeenPop | 310 | 160.00 | 0 | 930 | 291 |
| 1001/0/signed/RnB | 437 | 550.00 | 0 | 1330 | 428 |
| 1001/0/signed/Blues | 134 | 400.00 | 0 | 414 | 131 |
| 1001/0/signed/Folk | 176 | 500.00 | 0 | 511 | 187 |
| 1001/0/unsigned/Classical | 34 | 0.00 | 34 | 0 | 24 |
| 1001/0/signed/DooWop | 94 | 120.00 | 0 | 287 | 91 |
| 1001/0/signed/Soul | 427 | 670.00 | 0 | 1290 | 434 |
| 1001/0/signed/Country | 189 | 570.00 | 0 | 578 | 173 |
| 1001/0/signed/Classical | 41 | 0.00 | 41 | 0 | 44 |
| 1001/0/signed/EasyListening | 87 | 1360.00 | 0 | 256 | 92 |
| 1001/0/unsigned/Comedy | 7 | 0.00 | 7 | 0 | 5 |
| 1001/0/signed/Jazz | 116 | 500.00 | 0 | 359 | 108 |
| 1001/0/unsigned/LatinPop | 20 | 0.00 | 20 | 0 | 19 |
| 1001/0/signed/Gospel | 53 | 350.00 | 0 | 179 | 43 |
| 1001/0/unsigned/EasyListening | 64 | 1360.00 | 0 | 190 | 59 |
| 1001/0/signed/Comedy | 14 | 0.00 | 14 | 0 | 14 |
| 1001/0/unsigned/TexMex | 4 | 0.00 | 4 | 0 | 4 |
| 1001/0/signed/Childrens | 4 | 0.00 | 4 | 0 | 3 |
| 1001/0/unsigned/Childrens | 6 | 0.00 | 6 | 0 | 7 |
| 1001/0/signed/TexMex | 1 | 0.00 | 1 | 0 | 0 |
| 1001/1/signed/RockAndRoll | 497 | 1290.00 | 0 | 1519 | 471 |
| 1001/1/signed/TraditionalPop | 285 | 1200.00 | 0 | 866 | 288 |
| 1001/1/signed/TeenPop | 310 | 160.00 | 0 | 930 | 291 |
| 1001/1/signed/RnB | 437 | 550.00 | 0 | 1330 | 428 |
| 1001/1/signed/Blues | 134 | 400.00 | 0 | 414 | 131 |
| 1001/1/signed/Folk | 176 | 500.00 | 0 | 511 | 187 |
| 1001/1/unsigned/Classical | 34 | 0.00 | 34 | 0 | 24 |
| 1001/1/signed/DooWop | 94 | 120.00 | 0 | 287 | 91 |
| 1001/1/signed/Soul | 427 | 670.00 | 0 | 1290 | 434 |
| 1001/1/signed/Country | 189 | 570.00 | 0 | 578 | 173 |
| 1001/1/signed/Classical | 41 | 0.00 | 41 | 0 | 44 |
| 1001/1/signed/EasyListening | 87 | 1360.00 | 0 | 256 | 92 |
| 1001/1/unsigned/Comedy | 7 | 0.00 | 7 | 0 | 5 |
| 1001/1/signed/Jazz | 116 | 500.00 | 0 | 359 | 108 |
| 1001/1/unsigned/LatinPop | 20 | 0.00 | 20 | 0 | 19 |
| 1001/1/signed/Gospel | 53 | 350.00 | 0 | 179 | 43 |
| 1001/1/unsigned/EasyListening | 64 | 1360.00 | 0 | 190 | 59 |
| 1001/1/signed/Comedy | 14 | 0.00 | 14 | 0 | 14 |
| 1001/1/unsigned/TexMex | 4 | 0.00 | 4 | 0 | 4 |
| 1001/1/signed/Childrens | 4 | 0.00 | 4 | 0 | 3 |
| 1001/1/unsigned/Childrens | 6 | 0.00 | 6 | 0 | 7 |
| 1001/1/signed/TexMex | 1 | 0.00 | 1 | 0 | 0 |
| 1001/2/signed/RockAndRoll | 497 | 99.78 | 0 | 1519 | 471 |
| 1001/2/signed/TraditionalPop | 285 | 317.94 | 0 | 866 | 288 |
| 1001/2/signed/TeenPop | 310 | 93.04 | 0 | 930 | 291 |
| 1001/2/signed/RnB | 437 | 205.99 | 0 | 1330 | 428 |
| 1001/2/signed/Blues | 134 | 97.47 | 0 | 414 | 131 |
| 1001/2/signed/Folk | 176 | 160.78 | 0 | 511 | 187 |
| 1001/2/unsigned/Classical | 34 | 27.68 | 0 | 107 | 24 |
| 1001/2/signed/DooWop | 94 | 78.62 | 0 | 287 | 91 |
| 1001/2/signed/Soul | 427 | 78.45 | 0 | 1290 | 434 |
| 1001/2/signed/Country | 189 | 186.24 | 0 | 578 | 173 |
| 1001/2/signed/Classical | 41 | 27.85 | 0 | 115 | 44 |
| 1001/2/signed/EasyListening | 87 | 100.66 | 0 | 256 | 92 |
| 1001/2/unsigned/Comedy | 7 | 30.00 | 0 | 24 | 5 |
| 1001/2/signed/Jazz | 116 | 130.10 | 0 | 359 | 108 |
| 1001/2/unsigned/LatinPop | 20 | 27.75 | 0 | 62 | 19 |
| 1001/2/signed/Gospel | 53 | 87.17 | 0 | 179 | 43 |
| 1001/2/unsigned/EasyListening | 64 | 99.89 | 0 | 190 | 59 |
| 1001/2/signed/Comedy | 14 | 29.07 | 0 | 46 | 14 |
| 1001/2/unsigned/TexMex | 4 | 22.25 | 0 | 13 | 4 |
| 1001/2/signed/Childrens | 4 | 24.00 | 0 | 14 | 3 |
| 1001/2/unsigned/Childrens | 6 | 26.17 | 0 | 16 | 7 |
| 1001/2/signed/TexMex | 1 | 27.00 | 0 | 5 | 0 |
| 1001/3/signed/RockAndRoll | 497 | 99.78 | 0 | 1519 | 471 |
| 1001/3/signed/TraditionalPop | 285 | 317.94 | 0 | 866 | 288 |
| 1001/3/signed/TeenPop | 310 | 93.04 | 0 | 930 | 291 |
| 1001/3/signed/RnB | 437 | 205.99 | 0 | 1330 | 428 |
| 1001/3/signed/Blues | 134 | 97.47 | 0 | 414 | 131 |
| 1001/3/signed/Folk | 176 | 160.78 | 0 | 511 | 187 |
| 1001/3/unsigned/Classical | 34 | 27.68 | 0 | 107 | 24 |
| 1001/3/signed/DooWop | 94 | 78.62 | 0 | 287 | 91 |
| 1001/3/signed/Soul | 427 | 78.45 | 0 | 1290 | 434 |
| 1001/3/signed/Country | 189 | 186.24 | 0 | 578 | 173 |
| 1001/3/signed/Classical | 41 | 27.85 | 0 | 115 | 44 |
| 1001/3/signed/EasyListening | 87 | 100.66 | 0 | 256 | 92 |
| 1001/3/unsigned/Comedy | 7 | 30.00 | 0 | 24 | 5 |
| 1001/3/signed/Jazz | 116 | 130.10 | 0 | 359 | 108 |
| 1001/3/unsigned/LatinPop | 20 | 27.75 | 0 | 62 | 19 |
| 1001/3/signed/Gospel | 53 | 87.17 | 0 | 179 | 43 |
| 1001/3/unsigned/EasyListening | 64 | 99.89 | 0 | 190 | 59 |
| 1001/3/signed/Comedy | 14 | 29.07 | 0 | 46 | 14 |
| 1001/3/unsigned/TexMex | 4 | 22.25 | 0 | 13 | 4 |
| 1001/3/signed/Childrens | 4 | 24.00 | 0 | 14 | 3 |
| 1001/3/unsigned/Childrens | 6 | 26.17 | 0 | 16 | 7 |
| 1001/3/signed/TexMex | 1 | 27.00 | 0 | 5 | 0 |
| 1001/4/signed/RockAndRoll | 497 | 99.78 | 0 | 1456 | 541 |
| 1001/4/signed/TraditionalPop | 285 | 317.94 | 0 | 770 | 338 |
| 1001/4/signed/TeenPop | 310 | 93.04 | 0 | 868 | 362 |
| 1001/4/signed/RnB | 437 | 205.99 | 0 | 1208 | 542 |
| 1001/4/signed/Blues | 134 | 97.47 | 0 | 391 | 148 |
| 1001/4/signed/Folk | 176 | 160.78 | 0 | 605 | 93 |
| 1001/4/unsigned/Classical | 34 | 27.68 | 0 | 107 | 28 |
| 1001/4/signed/DooWop | 94 | 78.62 | 0 | 266 | 94 |
| 1001/4/signed/Soul | 427 | 78.45 | 0 | 1184 | 519 |
| 1001/4/signed/Country | 189 | 186.24 | 0 | 504 | 233 |
| 1001/4/signed/Classical | 41 | 27.85 | 0 | 108 | 50 |
| 1001/4/signed/EasyListening | 87 | 100.66 | 0 | 306 | 31 |
| 1001/4/unsigned/Comedy | 7 | 30.00 | 0 | 23 | 5 |
| 1001/4/signed/Jazz | 116 | 130.10 | 0 | 376 | 82 |
| 1001/4/unsigned/LatinPop | 20 | 27.75 | 0 | 61 | 22 |
| 1001/4/signed/Gospel | 53 | 87.17 | 0 | 199 | 23 |
| 1001/4/unsigned/EasyListening | 64 | 99.89 | 0 | 235 | 17 |
| 1001/4/signed/Comedy | 14 | 29.07 | 0 | 41 | 15 |
| 1001/4/unsigned/TexMex | 4 | 22.25 | 0 | 13 | 5 |
| 1001/4/signed/Childrens | 4 | 24.00 | 0 | 12 | 4 |
| 1001/4/unsigned/Childrens | 6 | 26.17 | 0 | 18 | 6 |
| 1001/4/signed/TexMex | 1 | 27.00 | 0 | 5 | 0 |
| 1001/5/signed/RockAndRoll | 497 | 102.66 | 0 | 1456 | 541 |
| 1001/5/signed/TraditionalPop | 285 | 335.31 | 0 | 770 | 338 |
| 1001/5/signed/TeenPop | 310 | 111.22 | 0 | 868 | 362 |
| 1001/5/signed/RnB | 437 | 212.78 | 0 | 1208 | 542 |
| 1001/5/signed/Blues | 134 | 98.04 | 0 | 391 | 148 |
| 1001/5/signed/Folk | 176 | 172.82 | 0 | 605 | 93 |
| 1001/5/unsigned/Classical | 34 | 27.03 | 0 | 107 | 28 |
| 1001/5/signed/DooWop | 94 | 82.55 | 0 | 266 | 94 |
| 1001/5/signed/Soul | 427 | 87.04 | 0 | 1184 | 519 |
| 1001/5/signed/Country | 189 | 193.73 | 0 | 504 | 233 |
| 1001/5/signed/Classical | 41 | 26.49 | 0 | 108 | 50 |
| 1001/5/signed/EasyListening | 87 | 114.10 | 0 | 306 | 31 |
| 1001/5/unsigned/Comedy | 7 | 14.29 | 0 | 23 | 5 |
| 1001/5/signed/Jazz | 116 | 140.59 | 0 | 376 | 82 |
| 1001/5/unsigned/LatinPop | 20 | 38.60 | 0 | 61 | 22 |
| 1001/5/signed/Gospel | 53 | 87.70 | 0 | 199 | 23 |
| 1001/5/unsigned/EasyListening | 64 | 112.63 | 0 | 235 | 17 |
| 1001/5/signed/Comedy | 14 | 13.07 | 0 | 41 | 15 |
| 1001/5/unsigned/TexMex | 4 | 35.25 | 0 | 13 | 5 |
| 1001/5/signed/Childrens | 4 | 20.25 | 0 | 12 | 4 |
| 1001/5/unsigned/Childrens | 6 | 19.50 | 0 | 18 | 6 |
| 1001/5/signed/TexMex | 1 | 33.00 | 0 | 5 | 0 |
| 1002/0/unsigned/EasyListening | 67 | 1360.00 | 0 | 223 | 42 |
| 1002/0/signed/Jazz | 147 | 500.00 | 0 | 408 | 168 |
| 1002/0/signed/RockAndRoll | 496 | 1290.00 | 0 | 1518 | 455 |
| 1002/0/signed/Country | 178 | 570.00 | 0 | 516 | 174 |
| 1002/0/signed/RnB | 427 | 550.00 | 0 | 1294 | 416 |
| 1002/0/signed/Blues | 87 | 400.00 | 0 | 285 | 77 |
| 1002/0/signed/Soul | 423 | 670.00 | 0 | 1266 | 410 |
| 1002/0/signed/TraditionalPop | 266 | 1200.00 | 0 | 798 | 264 |
| 1002/0/signed/TeenPop | 328 | 160.00 | 0 | 991 | 315 |
| 1002/0/unsigned/LatinPop | 29 | 0.00 | 29 | 0 | 28 |
| 1002/0/signed/DooWop | 122 | 120.00 | 0 | 375 | 103 |
| 1002/0/signed/Folk | 158 | 500.00 | 0 | 458 | 163 |
| 1002/0/signed/EasyListening | 82 | 1360.00 | 0 | 233 | 90 |
| 1002/0/unsigned/Classical | 47 | 0.00 | 47 | 0 | 36 |
| 1002/0/signed/Comedy | 18 | 0.00 | 18 | 0 | 19 |
| 1002/0/signed/Gospel | 40 | 350.00 | 0 | 131 | 34 |
| 1002/0/signed/Classical | 49 | 0.00 | 49 | 0 | 55 |
| 1002/0/unsigned/Childrens | 11 | 0.00 | 11 | 0 | 13 |
| 1002/0/signed/TexMex | 3 | 0.00 | 3 | 0 | 2 |
| 1002/0/signed/Childrens | 9 | 0.00 | 9 | 0 | 11 |
| 1002/0/unsigned/Comedy | 9 | 0.00 | 9 | 0 | 8 |
| 1002/0/unsigned/TexMex | 4 | 0.00 | 4 | 0 | 4 |
| 1002/1/unsigned/EasyListening | 67 | 1360.00 | 0 | 223 | 42 |
| 1002/1/signed/Jazz | 147 | 500.00 | 0 | 408 | 168 |
| 1002/1/signed/RockAndRoll | 496 | 1290.00 | 0 | 1518 | 455 |
| 1002/1/signed/Country | 178 | 570.00 | 0 | 516 | 174 |
| 1002/1/signed/RnB | 427 | 550.00 | 0 | 1294 | 416 |
| 1002/1/signed/Blues | 87 | 400.00 | 0 | 285 | 77 |
| 1002/1/signed/Soul | 423 | 670.00 | 0 | 1266 | 410 |
| 1002/1/signed/TraditionalPop | 266 | 1200.00 | 0 | 798 | 264 |
| 1002/1/signed/TeenPop | 328 | 160.00 | 0 | 991 | 315 |
| 1002/1/unsigned/LatinPop | 29 | 0.00 | 29 | 0 | 28 |
| 1002/1/signed/DooWop | 122 | 120.00 | 0 | 375 | 103 |
| 1002/1/signed/Folk | 158 | 500.00 | 0 | 458 | 163 |
| 1002/1/signed/EasyListening | 82 | 1360.00 | 0 | 233 | 90 |
| 1002/1/unsigned/Classical | 47 | 0.00 | 47 | 0 | 36 |
| 1002/1/signed/Comedy | 18 | 0.00 | 18 | 0 | 19 |
| 1002/1/signed/Gospel | 40 | 350.00 | 0 | 131 | 34 |
| 1002/1/signed/Classical | 49 | 0.00 | 49 | 0 | 55 |
| 1002/1/unsigned/Childrens | 11 | 0.00 | 11 | 0 | 13 |
| 1002/1/signed/TexMex | 3 | 0.00 | 3 | 0 | 2 |
| 1002/1/signed/Childrens | 9 | 0.00 | 9 | 0 | 11 |
| 1002/1/unsigned/Comedy | 9 | 0.00 | 9 | 0 | 8 |
| 1002/1/unsigned/TexMex | 4 | 0.00 | 4 | 0 | 4 |
| 1002/2/unsigned/EasyListening | 67 | 101.72 | 0 | 223 | 42 |
| 1002/2/signed/Jazz | 147 | 130.44 | 0 | 408 | 168 |
| 1002/2/signed/RockAndRoll | 496 | 100.12 | 0 | 1518 | 455 |
| 1002/2/signed/Country | 178 | 185.34 | 0 | 516 | 174 |
| 1002/2/signed/RnB | 427 | 204.84 | 0 | 1294 | 416 |
| 1002/2/signed/Blues | 87 | 98.01 | 0 | 285 | 77 |
| 1002/2/signed/Soul | 423 | 78.08 | 0 | 1266 | 410 |
| 1002/2/signed/TraditionalPop | 266 | 317.36 | 0 | 798 | 264 |
| 1002/2/signed/TeenPop | 328 | 92.69 | 0 | 991 | 315 |
| 1002/2/unsigned/LatinPop | 29 | 28.14 | 0 | 95 | 28 |
| 1002/2/signed/DooWop | 122 | 78.29 | 0 | 375 | 103 |
| 1002/2/signed/Folk | 158 | 161.39 | 0 | 458 | 163 |
| 1002/2/signed/EasyListening | 82 | 101.09 | 0 | 233 | 90 |
| 1002/2/unsigned/Classical | 47 | 27.62 | 0 | 157 | 36 |
| 1002/2/signed/Comedy | 18 | 27.33 | 0 | 54 | 19 |
| 1002/2/signed/Gospel | 40 | 87.00 | 0 | 131 | 34 |
| 1002/2/signed/Classical | 49 | 28.61 | 0 | 143 | 55 |
| 1002/2/unsigned/Childrens | 11 | 28.55 | 0 | 35 | 13 |
| 1002/2/signed/TexMex | 3 | 33.67 | 0 | 10 | 2 |
| 1002/2/signed/Childrens | 9 | 30.11 | 0 | 25 | 11 |
| 1002/2/unsigned/Comedy | 9 | 30.56 | 0 | 29 | 8 |
| 1002/2/unsigned/TexMex | 4 | 29.00 | 0 | 11 | 4 |
| 1002/3/unsigned/EasyListening | 67 | 101.72 | 0 | 223 | 42 |
| 1002/3/signed/Jazz | 147 | 130.44 | 0 | 408 | 168 |
| 1002/3/signed/RockAndRoll | 496 | 100.12 | 0 | 1518 | 455 |
| 1002/3/signed/Country | 178 | 185.34 | 0 | 516 | 174 |
| 1002/3/signed/RnB | 427 | 204.84 | 0 | 1294 | 416 |
| 1002/3/signed/Blues | 87 | 98.01 | 0 | 285 | 77 |
| 1002/3/signed/Soul | 423 | 78.08 | 0 | 1266 | 410 |
| 1002/3/signed/TraditionalPop | 266 | 317.36 | 0 | 798 | 264 |
| 1002/3/signed/TeenPop | 328 | 92.69 | 0 | 991 | 315 |
| 1002/3/unsigned/LatinPop | 29 | 28.14 | 0 | 95 | 28 |
| 1002/3/signed/DooWop | 122 | 78.29 | 0 | 375 | 103 |
| 1002/3/signed/Folk | 158 | 161.39 | 0 | 458 | 163 |
| 1002/3/signed/EasyListening | 82 | 101.09 | 0 | 233 | 90 |
| 1002/3/unsigned/Classical | 47 | 27.62 | 0 | 157 | 36 |
| 1002/3/signed/Comedy | 18 | 27.33 | 0 | 54 | 19 |
| 1002/3/signed/Gospel | 40 | 87.00 | 0 | 131 | 34 |
| 1002/3/signed/Classical | 49 | 28.61 | 0 | 143 | 55 |
| 1002/3/unsigned/Childrens | 11 | 28.55 | 0 | 35 | 13 |
| 1002/3/signed/TexMex | 3 | 33.67 | 0 | 10 | 2 |
| 1002/3/signed/Childrens | 9 | 30.11 | 0 | 25 | 11 |
| 1002/3/unsigned/Comedy | 9 | 30.56 | 0 | 29 | 8 |
| 1002/3/unsigned/TexMex | 4 | 29.00 | 0 | 11 | 4 |
| 1002/4/unsigned/EasyListening | 67 | 101.72 | 0 | 251 | 15 |
| 1002/4/signed/Jazz | 147 | 130.44 | 0 | 462 | 123 |
| 1002/4/signed/RockAndRoll | 496 | 100.12 | 0 | 1471 | 505 |
| 1002/4/signed/Country | 178 | 185.34 | 0 | 463 | 243 |
| 1002/4/signed/RnB | 427 | 204.84 | 0 | 1197 | 513 |
| 1002/4/signed/Blues | 87 | 98.01 | 0 | 257 | 89 |
| 1002/4/signed/Soul | 423 | 78.08 | 0 | 1179 | 521 |
| 1002/4/signed/TraditionalPop | 266 | 317.36 | 0 | 753 | 329 |
| 1002/4/signed/TeenPop | 328 | 92.69 | 0 | 908 | 381 |
| 1002/4/unsigned/LatinPop | 29 | 28.14 | 0 | 85 | 33 |
| 1002/4/signed/DooWop | 122 | 78.29 | 0 | 373 | 114 |
| 1002/4/signed/Folk | 158 | 161.39 | 0 | 556 | 76 |
| 1002/4/signed/EasyListening | 82 | 101.09 | 0 | 296 | 36 |
| 1002/4/unsigned/Classical | 47 | 27.62 | 0 | 149 | 40 |
| 1002/4/signed/Comedy | 18 | 27.33 | 0 | 54 | 22 |
| 1002/4/signed/Gospel | 40 | 87.00 | 0 | 145 | 18 |
| 1002/4/signed/Classical | 49 | 28.61 | 0 | 134 | 67 |
| 1002/4/unsigned/Childrens | 11 | 28.55 | 0 | 30 | 13 |
| 1002/4/signed/TexMex | 3 | 33.67 | 0 | 9 | 3 |
| 1002/4/signed/Childrens | 9 | 30.11 | 0 | 20 | 14 |
| 1002/4/unsigned/Comedy | 9 | 30.56 | 0 | 27 | 8 |
| 1002/4/unsigned/TexMex | 4 | 29.00 | 0 | 12 | 6 |
| 1002/5/unsigned/EasyListening | 67 | 115.72 | 0 | 251 | 15 |
| 1002/5/signed/Jazz | 147 | 140.91 | 0 | 462 | 123 |
| 1002/5/signed/RockAndRoll | 496 | 103.07 | 0 | 1471 | 505 |
| 1002/5/signed/Country | 178 | 194.80 | 0 | 463 | 243 |
| 1002/5/signed/RnB | 427 | 212.00 | 0 | 1197 | 513 |
| 1002/5/signed/Blues | 87 | 98.26 | 0 | 257 | 89 |
| 1002/5/signed/Soul | 423 | 85.61 | 0 | 1179 | 521 |
| 1002/5/signed/TraditionalPop | 266 | 334.01 | 0 | 753 | 329 |
| 1002/5/signed/TeenPop | 328 | 109.23 | 0 | 908 | 381 |
| 1002/5/unsigned/LatinPop | 29 | 36.34 | 0 | 85 | 33 |
| 1002/5/signed/DooWop | 122 | 82.34 | 0 | 373 | 114 |
| 1002/5/signed/Folk | 158 | 172.26 | 0 | 556 | 76 |
| 1002/5/signed/EasyListening | 82 | 114.99 | 0 | 296 | 36 |
| 1002/5/unsigned/Classical | 47 | 25.91 | 0 | 149 | 40 |
| 1002/5/signed/Comedy | 18 | 13.78 | 0 | 54 | 22 |
| 1002/5/signed/Gospel | 40 | 87.35 | 0 | 145 | 18 |
| 1002/5/signed/Classical | 49 | 25.47 | 0 | 134 | 67 |
| 1002/5/unsigned/Childrens | 11 | 16.73 | 0 | 30 | 13 |
| 1002/5/signed/TexMex | 3 | 29.67 | 0 | 9 | 3 |
| 1002/5/signed/Childrens | 9 | 18.67 | 0 | 20 | 14 |
| 1002/5/unsigned/Comedy | 9 | 12.56 | 0 | 27 | 8 |
| 1002/5/unsigned/TexMex | 4 | 30.25 | 0 | 12 | 6 |

### Pre-switch arrangement ordering A/B
Reference and proposed-arrangement ranking use identical phase-2 actors, candidate pools and requested cover counts. Changed slots count resolved top-N compositions absent from reference top-N. This comparison precedes preference selection and refusal filtering; it is not a percentage of final performed sets. Material divergence warrants resolved suitability for non-rock live ranking. The existing bounded rock sampler shares studio infrastructure; its live source mixing remains unchanged, and its final ID ordering is removed.

| Seed / population / genre | Actors | Changed actors | Changed / requested slots | Changed share |
| --- | --- | --- | --- | --- |
| 1001/signed/RockAndRoll | 497 | 496 | 1389/1519 | 91.44% |
| 1001/signed/TraditionalPop | 285 | 284 | 837/866 | 96.65% |
| 1001/signed/TeenPop | 310 | 304 | 817/930 | 87.85% |
| 1001/signed/RnB | 437 | 436 | 1291/1330 | 97.07% |
| 1001/signed/Blues | 134 | 134 | 382/414 | 92.27% |
| 1001/signed/Folk | 176 | 173 | 419/511 | 82.00% |
| 1001/unsigned/Classical | 34 | 31 | 64/107 | 59.81% |
| 1001/signed/DooWop | 94 | 93 | 266/287 | 92.68% |
| 1001/signed/Soul | 427 | 423 | 1173/1290 | 90.93% |
| 1001/signed/Country | 189 | 189 | 559/578 | 96.71% |
| 1001/signed/Classical | 41 | 41 | 99/115 | 86.09% |
| 1001/signed/EasyListening | 87 | 87 | 244/256 | 95.31% |
| 1001/unsigned/Comedy | 7 | 7 | 16/24 | 66.67% |
| 1001/signed/Jazz | 116 | 116 | 348/359 | 96.94% |
| 1001/unsigned/LatinPop | 20 | 19 | 40/62 | 64.52% |
| 1001/signed/Gospel | 53 | 49 | 102/179 | 56.98% |
| 1001/unsigned/EasyListening | 64 | 63 | 173/190 | 91.05% |
| 1001/signed/Comedy | 14 | 14 | 34/46 | 73.91% |
| 1001/unsigned/TexMex | 4 | 4 | 11/13 | 84.62% |
| 1001/signed/Childrens | 4 | 4 | 9/14 | 64.29% |
| 1001/unsigned/Childrens | 6 | 6 | 14/16 | 87.50% |
| 1001/signed/TexMex | 1 | 1 | 4/5 | 80.00% |
| 1002/unsigned/EasyListening | 67 | 66 | 199/223 | 89.24% |
| 1002/signed/Jazz | 147 | 147 | 402/408 | 98.53% |
| 1002/signed/RockAndRoll | 496 | 495 | 1370/1518 | 90.25% |
| 1002/signed/Country | 178 | 178 | 497/516 | 96.32% |
| 1002/signed/RnB | 427 | 427 | 1241/1294 | 95.90% |
| 1002/signed/Blues | 87 | 87 | 266/285 | 93.33% |
| 1002/signed/Soul | 423 | 422 | 1128/1266 | 89.10% |
| 1002/signed/TraditionalPop | 266 | 266 | 773/798 | 96.87% |
| 1002/signed/TeenPop | 328 | 324 | 835/991 | 84.26% |
| 1002/unsigned/LatinPop | 29 | 29 | 71/95 | 74.74% |
| 1002/signed/DooWop | 122 | 122 | 337/375 | 89.87% |
| 1002/signed/Folk | 158 | 156 | 405/458 | 88.43% |
| 1002/signed/EasyListening | 82 | 80 | 208/233 | 89.27% |
| 1002/unsigned/Classical | 47 | 43 | 87/157 | 55.41% |
| 1002/signed/Comedy | 18 | 17 | 37/54 | 68.52% |
| 1002/signed/Gospel | 40 | 33 | 68/131 | 51.91% |
| 1002/signed/Classical | 49 | 47 | 111/143 | 77.62% |
| 1002/unsigned/Childrens | 11 | 10 | 20/35 | 57.14% |
| 1002/signed/TexMex | 3 | 3 | 7/10 | 70.00% |
| 1002/signed/Childrens | 9 | 9 | 23/25 | 92.00% |
| 1002/unsigned/Comedy | 9 | 8 | 21/29 | 72.41% |
| 1002/unsigned/TexMex | 4 | 4 | 10/11 | 90.91% |

## Full-run category partition and before/after
Every row below is aggregate 1960–63. Full monthly and annual partitions with category numerators, denominators, supplied-new and contemporary/recent cover counts are in `../SimLogs/polar-repertoire-analysis.json` (`runs[].rows`).

| Run | Population | Genre | New | Existing cover | Established | Lineage | All slots | Standard+lineage share | PD separately | Recent covers | Supplied new |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 baseline | unsigned | Classical | 8347 | 18108 | 0 | 0 | 26455 | 0.00% | 0 | 18108 | 0 |
| 1001 baseline | unsigned | Comedy | 7666 | 21173 | 0 | 0 | 28839 | 0.00% | 0 | 21173 | 0 |
| 1001 baseline | unsigned | LatinPop | 5019 | 3112 | 0 | 0 | 8131 | 0.00% | 0 | 3112 | 0 |
| 1001 baseline | unsigned | EasyListening | 15471 | 49241 | 88 | 0 | 64800 | 0.14% | 89 | 43854 | 0 |
| 1001 baseline | unsigned | TexMex | 3101 | 2368 | 0 | 0 | 5469 | 0.00% | 0 | 2368 | 0 |
| 1001 baseline | unsigned | Childrens | 5664 | 15793 | 0 | 0 | 21457 | 0.00% | 0 | 15793 | 0 |
| 1001 baseline | signed | RockAndRoll | 19125 | 56429 | 655 | 0 | 76209 | 0.86% | 708 | 55737 | 0 |
| 1001 baseline | signed | TraditionalPop | 12582 | 15221 | 18615 | 0 | 46418 | 40.10% | 8135 | 8991 | 0 |
| 1001 baseline | signed | TeenPop | 12901 | 39645 | 0 | 0 | 52546 | 0.00% | 0 | 39645 | 0 |
| 1001 baseline | signed | RnB | 18569 | 46484 | 6802 | 0 | 71855 | 9.47% | 28345 | 22940 | 0 |
| 1001 baseline | signed | Blues | 4840 | 7757 | 6727 | 0 | 19324 | 34.81% | 9396 | 3212 | 0 |
| 1001 baseline | signed | Folk | 9110 | 1 | 0 | 24575 | 33686 | 72.95% | 24575 | 46 | 0 |
| 1001 baseline | signed | DooWop | 6401 | 18740 | 0 | 0 | 25141 | 0.00% | 0 | 18740 | 0 |
| 1001 baseline | signed | Soul | 18806 | 52754 | 0 | 0 | 71560 | 0.00% | 144 | 48186 | 0 |
| 1001 baseline | signed | Country | 12546 | 4210 | 33153 | 0 | 49909 | 66.43% | 8109 | 153 | 0 |
| 1001 baseline | signed | Classical | 3564 | 5987 | 0 | 0 | 9551 | 0.00% | 0 | 5987 | 0 |
| 1001 baseline | signed | EasyListening | 7016 | 18989 | 189 | 0 | 26194 | 0.72% | 144 | 16300 | 0 |
| 1001 baseline | signed | Jazz | 6219 | 7071 | 11137 | 0 | 24427 | 45.59% | 3894 | 662 | 0 |
| 1001 baseline | signed | Gospel | 3128 | 4378 | 6790 | 0 | 14296 | 47.50% | 9415 | 2597 | 0 |
| 1001 baseline | signed | Comedy | 2504 | 6804 | 0 | 0 | 9308 | 0.00% | 0 | 6804 | 0 |
| 1001 baseline | signed | Childrens | 1628 | 4290 | 0 | 0 | 5918 | 0.00% | 0 | 4290 | 0 |
| 1001 baseline | unsigned | Folk | 13925 | 0 | 0 | 43623 | 57548 | 75.80% | 43623 | 0 | 0 |
| 1001 baseline | unsigned | Country | 24061 | 7466 | 71561 | 0 | 103088 | 69.42% | 19380 | 103 | 0 |
| 1001 baseline | unsigned | RnB | 24209 | 60032 | 12848 | 0 | 97089 | 13.23% | 38150 | 33344 | 0 |
| 1001 baseline | unsigned | RockAndRoll | 25748 | 86756 | 932 | 0 | 113436 | 0.82% | 1166 | 85680 | 0 |
| 1001 baseline | unsigned | Soul | 27316 | 86395 | 0 | 0 | 113711 | 0.00% | 0 | 75114 | 0 |
| 1001 baseline | unsigned | Gospel | 7204 | 9182 | 16189 | 0 | 32575 | 49.70% | 23780 | 5294 | 0 |
| 1001 baseline | unsigned | DooWop | 13733 | 43214 | 0 | 0 | 56947 | 0.00% | 0 | 43214 | 0 |
| 1001 baseline | unsigned | TraditionalPop | 17673 | 21948 | 35644 | 0 | 75265 | 47.36% | 15508 | 13814 | 0 |
| 1001 baseline | unsigned | SurfRock | 4823 | 15976 | 247 | 0 | 21046 | 1.17% | 257 | 15728 | 0 |
| 1001 baseline | unsigned | Jazz | 11480 | 13713 | 23273 | 0 | 48466 | 48.02% | 6481 | 1481 | 0 |
| 1001 baseline | unsigned | TeenPop | 19811 | 64326 | 0 | 0 | 84137 | 0.00% | 0 | 64326 | 0 |
| 1001 baseline | unsigned | Blues | 9570 | 18075 | 13258 | 0 | 40903 | 32.41% | 18320 | 7746 | 0 |
| 1001 baseline | signed | LatinPop | 1573 | 1115 | 0 | 0 | 2688 | 0.00% | 0 | 1115 | 0 |
| 1001 baseline | signed | TexMex | 1361 | 987 | 0 | 0 | 2348 | 0.00% | 0 | 987 | 0 |
| 1001 baseline | signed | SurfRock | 1735 | 5176 | 75 | 0 | 6986 | 1.07% | 60 | 5098 | 0 |
| 1001 baseline | unsigned | ContemporaryFolk | 2589 | 2 | 0 | 9184 | 11775 | 78.00% | 9184 | 2 | 0 |
| 1001 baseline | signed | ContemporaryFolk | 985 | 0 | 0 | 3205 | 4190 | 76.49% | 3205 | 0 | 0 |
| 1001 baseline | unsigned | BossaNova | 1707 | 2199 | 3583 | 0 | 7489 | 47.84% | 582 | 278 | 0 |
| 1001 baseline | signed | BossaNova | 868 | 1012 | 1568 | 0 | 3448 | 45.48% | 230 | 170 | 0 |
| 1001 baseline | unsigned | BritishBeat | 13 | 133 | 0 | 0 | 146 | 0.00% | 0 | 133 | 0 |
| 1001 baseline | unsigned | GarageRock | 196 | 638 | 10 | 0 | 844 | 1.18% | 10 | 625 | 0 |
| 1001 baseline | signed | GarageRock | 112 | 316 | 0 | 0 | 428 | 0.00% | 0 | 316 | 0 |
| 1001 baseline | signed | BritishBeat | 0 | 16 | 0 | 0 | 16 | 0.00% | 0 | 16 | 0 |
| 1002 baseline | unsigned | EasyListening | 14605 | 49684 | 163 | 0 | 64452 | 0.25% | 163 | 46292 | 0 |
| 1002 baseline | unsigned | LatinPop | 5671 | 0 | 0 | 0 | 5671 | 0.00% | 0 | 0 | 0 |
| 1002 baseline | unsigned | Classical | 8528 | 9617 | 0 | 0 | 18145 | 0.00% | 0 | 9617 | 0 |
| 1002 baseline | unsigned | Childrens | 6332 | 18569 | 0 | 0 | 24901 | 0.00% | 0 | 18569 | 0 |
| 1002 baseline | unsigned | Comedy | 7889 | 24530 | 0 | 0 | 32419 | 0.00% | 0 | 24530 | 0 |
| 1002 baseline | unsigned | TexMex | 3551 | 0 | 0 | 0 | 3551 | 0.00% | 0 | 0 | 0 |
| 1002 baseline | signed | Jazz | 7643 | 3456 | 15043 | 0 | 26142 | 57.54% | 6715 | 348 | 0 |
| 1002 baseline | signed | RockAndRoll | 19842 | 55431 | 709 | 0 | 75982 | 0.93% | 659 | 54838 | 0 |
| 1002 baseline | signed | Country | 12720 | 13331 | 22074 | 0 | 48125 | 45.87% | 1524 | 3908 | 0 |
| 1002 baseline | signed | RnB | 18985 | 48754 | 3331 | 0 | 71070 | 4.69% | 24806 | 28322 | 0 |
| 1002 baseline | signed | Blues | 3633 | 5596 | 6151 | 0 | 15380 | 39.99% | 6451 | 2311 | 0 |
| 1002 baseline | signed | Soul | 16836 | 48567 | 0 | 0 | 65403 | 0.00% | 2250 | 40131 | 0 |
| 1002 baseline | signed | TraditionalPop | 12435 | 15822 | 20356 | 0 | 48613 | 41.87% | 8209 | 13731 | 0 |
| 1002 baseline | signed | TeenPop | 13133 | 39632 | 0 | 0 | 52765 | 0.00% | 0 | 39632 | 0 |
| 1002 baseline | signed | DooWop | 6602 | 21158 | 0 | 0 | 27760 | 0.00% | 0 | 21158 | 0 |
| 1002 baseline | signed | Folk | 7919 | 0 | 0 | 22503 | 30422 | 73.97% | 22503 | 597 | 0 |
| 1002 baseline | signed | EasyListening | 5664 | 15679 | 294 | 0 | 21637 | 1.36% | 297 | 13944 | 0 |
| 1002 baseline | signed | Gospel | 3277 | 3350 | 6734 | 0 | 13361 | 50.40% | 4634 | 1361 | 0 |
| 1002 baseline | signed | Classical | 3446 | 2780 | 0 | 0 | 6226 | 0.00% | 0 | 2780 | 0 |
| 1002 baseline | signed | Comedy | 3062 | 7248 | 0 | 0 | 10310 | 0.00% | 0 | 7248 | 0 |
| 1002 baseline | signed | TexMex | 1028 | 0 | 0 | 0 | 1028 | 0.00% | 0 | 0 | 0 |
| 1002 baseline | signed | Childrens | 1900 | 5234 | 0 | 0 | 7134 | 0.00% | 0 | 5234 | 0 |
| 1002 baseline | unsigned | TraditionalPop | 17490 | 26766 | 32483 | 0 | 76739 | 42.33% | 11368 | 23076 | 0 |
| 1002 baseline | unsigned | RnB | 22756 | 69886 | 4709 | 0 | 97351 | 4.84% | 37727 | 34985 | 0 |
| 1002 baseline | unsigned | Jazz | 13620 | 5837 | 34072 | 0 | 53529 | 63.65% | 13292 | 195 | 0 |
| 1002 baseline | unsigned | Blues | 9855 | 16604 | 16932 | 0 | 43391 | 39.02% | 17546 | 5666 | 0 |
| 1002 baseline | unsigned | Country | 24862 | 29050 | 51631 | 0 | 105543 | 48.92% | 4206 | 6561 | 0 |
| 1002 baseline | unsigned | DooWop | 13357 | 45519 | 0 | 0 | 58876 | 0.00% | 0 | 45519 | 0 |
| 1002 baseline | unsigned | Soul | 27071 | 84530 | 0 | 0 | 111601 | 0.00% | 112 | 61007 | 0 |
| 1002 baseline | unsigned | Folk | 13617 | 0 | 0 | 42865 | 56482 | 75.89% | 42865 | 1739 | 0 |
| 1002 baseline | unsigned | RockAndRoll | 25811 | 85723 | 1014 | 0 | 112548 | 0.90% | 1037 | 84477 | 0 |
| 1002 baseline | unsigned | Gospel | 6840 | 8106 | 15158 | 0 | 30104 | 50.35% | 11251 | 3848 | 0 |
| 1002 baseline | unsigned | TeenPop | 20313 | 67785 | 0 | 0 | 88098 | 0.00% | 0 | 67785 | 0 |
| 1002 baseline | unsigned | SurfRock | 4431 | 13780 | 200 | 0 | 18411 | 1.09% | 188 | 13575 | 0 |
| 1002 baseline | signed | LatinPop | 1552 | 0 | 0 | 0 | 1552 | 0.00% | 0 | 0 | 0 |
| 1002 baseline | signed | SurfRock | 1561 | 4191 | 96 | 0 | 5848 | 1.64% | 52 | 4126 | 0 |
| 1002 baseline | unsigned | ContemporaryFolk | 2844 | 4 | 0 | 8804 | 11652 | 75.56% | 8804 | 209 | 0 |
| 1002 baseline | signed | ContemporaryFolk | 1083 | 0 | 0 | 2722 | 3805 | 71.54% | 2722 | 1 | 0 |
| 1002 baseline | unsigned | BossaNova | 1721 | 629 | 4671 | 0 | 7021 | 66.53% | 1452 | 25 | 0 |
| 1002 baseline | signed | BossaNova | 583 | 142 | 1264 | 0 | 1989 | 63.55% | 326 | 0 | 0 |
| 1002 baseline | unsigned | GarageRock | 277 | 793 | 0 | 0 | 1070 | 0.00% | 6 | 784 | 0 |
| 1002 baseline | signed | GarageRock | 71 | 186 | 0 | 0 | 257 | 0.00% | 0 | 184 | 0 |
| 1002 baseline | signed | BritishBeat | 9 | 24 | 6 | 0 | 39 | 15.38% | 0 | 24 | 0 |
| 1002 baseline | unsigned | BritishBeat | 2 | 5 | 0 | 0 | 7 | 0.00% | 0 | 5 | 0 |

### Recording assignments and chart observations, reported separately
These quantities do not use the live-slot denominator. Master assignments observe newly recorded material; chart-event rows include run-start chart observations with explicit left-censoring. They are not a calibrated chart-success probability. Baseline assignment telemetry retains its legacy category; final runs append source and corrected provenance.

| Run | New master assignments | First-chart observation rows |
| --- | --- | --- |
| 1001 baseline | 113479 | 358 |
| 1002 baseline | 109455 | 357 |

| Run | Act genre / year / kind / source / provenance | Assignments |
| --- | --- | --- |
| 1001 baseline | Jazz/1960/single/original/legacyCategoryOnly | 40 |
| 1001 baseline | RockAndRoll/1960/single/recentHit/legacyCategoryOnly | 271 |
| 1001 baseline | Jazz/1960/single/standard/legacyCategoryOnly | 29 |
| 1001 baseline | Country/1960/single/original/legacyCategoryOnly | 151 |
| 1001 baseline | RnB/1960/single/original/legacyCategoryOnly | 360 |
| 1001 baseline | Soul/1960/albumTrack/original/legacyCategoryOnly | 3979 |
| 1001 baseline | Soul/1960/albumTrack/recentHit/legacyCategoryOnly | 982 |
| 1001 baseline | Soul/1960/albumTrack/traditional/legacyCategoryOnly | 523 |
| 1001 baseline | TeenPop/1960/single/original/legacyCategoryOnly | 381 |
| 1001 baseline | RnB/1960/single/traditional/legacyCategoryOnly | 77 |
| 1001 baseline | DooWop/1960/single/original/legacyCategoryOnly | 63 |
| 1001 baseline | TraditionalPop/1960/single/recentHit/legacyCategoryOnly | 21 |
| 1001 baseline | Folk/1960/albumTrack/traditional/legacyCategoryOnly | 798 |
| 1001 baseline | Folk/1960/albumTrack/recentHit/legacyCategoryOnly | 223 |
| 1001 baseline | Folk/1960/albumTrack/original/legacyCategoryOnly | 1058 |
| 1001 baseline | Folk/1960/albumTrack/standard/legacyCategoryOnly | 332 |
| 1001 baseline | Gospel/1960/single/original/legacyCategoryOnly | 11 |
| 1001 baseline | RockAndRoll/1960/single/original/legacyCategoryOnly | 317 |
| 1001 baseline | Soul/1960/albumTrack/standard/legacyCategoryOnly | 8 |
| 1001 baseline | RnB/1960/albumTrack/recentHit/legacyCategoryOnly | 255 |
| 1001 baseline | RnB/1960/albumTrack/original/legacyCategoryOnly | 1015 |
| 1001 baseline | RnB/1960/albumTrack/traditional/legacyCategoryOnly | 137 |
| 1001 baseline | TeenPop/1960/single/standard/legacyCategoryOnly | 28 |
| 1001 baseline | TraditionalPop/1960/albumTrack/original/legacyCategoryOnly | 973 |
| 1001 baseline | TraditionalPop/1960/albumTrack/standard/legacyCategoryOnly | 915 |
| 1001 baseline | TraditionalPop/1960/albumTrack/traditional/legacyCategoryOnly | 644 |
| 1001 baseline | TraditionalPop/1960/albumTrack/recentHit/legacyCategoryOnly | 258 |
| 1001 baseline | Country/1960/single/standard/legacyCategoryOnly | 18 |
| 1001 baseline | RockAndRoll/1960/albumTrack/original/legacyCategoryOnly | 695 |
| 1001 baseline | RockAndRoll/1960/albumTrack/recentHit/legacyCategoryOnly | 519 |
| 1001 baseline | Classical/1960/single/standard/legacyCategoryOnly | 5 |
| 1001 baseline | TexMex/1960/albumTrack/traditional/legacyCategoryOnly | 19 |
| 1001 baseline | TexMex/1960/albumTrack/original/legacyCategoryOnly | 21 |
| 1001 baseline | TexMex/1960/albumTrack/recentHit/legacyCategoryOnly | 13 |
| 1001 baseline | EasyListening/1960/single/original/legacyCategoryOnly | 39 |
| 1001 baseline | DooWop/1960/albumTrack/original/legacyCategoryOnly | 56 |
| 1001 baseline | DooWop/1960/albumTrack/recentHit/legacyCategoryOnly | 46 |
| 1001 baseline | DooWop/1960/albumTrack/standard/legacyCategoryOnly | 3 |
| 1001 baseline | DooWop/1960/albumTrack/traditional/legacyCategoryOnly | 23 |
| 1001 baseline | EasyListening/1960/albumTrack/traditional/legacyCategoryOnly | 364 |
| 1001 baseline | EasyListening/1960/albumTrack/original/legacyCategoryOnly | 529 |
| 1001 baseline | EasyListening/1960/albumTrack/standard/legacyCategoryOnly | 490 |
| 1001 baseline | EasyListening/1960/albumTrack/recentHit/legacyCategoryOnly | 141 |
| 1001 baseline | TraditionalPop/1960/single/standard/legacyCategoryOnly | 57 |
| 1001 baseline | Blues/1960/albumTrack/original/legacyCategoryOnly | 638 |
| 1001 baseline | Blues/1960/albumTrack/traditional/legacyCategoryOnly | 379 |
| 1001 baseline | Blues/1960/albumTrack/recentHit/legacyCategoryOnly | 401 |
| 1001 baseline | Blues/1960/albumTrack/standard/legacyCategoryOnly | 112 |
| 1001 baseline | TeenPop/1960/albumTrack/recentHit/legacyCategoryOnly | 113 |
| 1001 baseline | TeenPop/1960/albumTrack/original/legacyCategoryOnly | 680 |
| 1001 baseline | TeenPop/1960/albumTrack/traditional/legacyCategoryOnly | 113 |
| 1001 baseline | TeenPop/1960/albumTrack/standard/legacyCategoryOnly | 39 |
| 1001 baseline | Blues/1960/single/original/legacyCategoryOnly | 32 |
| 1001 baseline | Comedy/1960/albumTrack/recentHit/legacyCategoryOnly | 106 |
| 1001 baseline | Comedy/1960/albumTrack/original/legacyCategoryOnly | 127 |
| 1001 baseline | Comedy/1960/albumTrack/traditional/legacyCategoryOnly | 78 |
| 1001 baseline | Comedy/1960/albumTrack/standard/legacyCategoryOnly | 36 |
| 1001 baseline | EasyListening/1960/single/standard/legacyCategoryOnly | 32 |
| 1001 baseline | Folk/1960/single/traditional/legacyCategoryOnly | 24 |
| 1001 baseline | Soul/1960/single/recentHit/legacyCategoryOnly | 50 |
| 1001 baseline | DooWop/1960/single/recentHit/legacyCategoryOnly | 48 |
| 1001 baseline | Country/1960/albumTrack/original/legacyCategoryOnly | 660 |
| 1001 baseline | Country/1960/albumTrack/standard/legacyCategoryOnly | 63 |
| 1001 baseline | Country/1960/albumTrack/traditional/legacyCategoryOnly | 111 |
| 1001 baseline | Country/1960/albumTrack/recentHit/legacyCategoryOnly | 100 |
| 1001 baseline | RnB/1960/single/recentHit/legacyCategoryOnly | 101 |
| 1001 baseline | Jazz/1960/single/traditional/legacyCategoryOnly | 19 |
| 1001 baseline | Soul/1960/single/original/legacyCategoryOnly | 127 |
| 1001 baseline | Soul/1960/single/traditional/legacyCategoryOnly | 30 |
| 1001 baseline | Jazz/1960/albumTrack/original/legacyCategoryOnly | 367 |
| 1001 baseline | Jazz/1960/albumTrack/traditional/legacyCategoryOnly | 179 |
| 1001 baseline | Jazz/1960/albumTrack/standard/legacyCategoryOnly | 308 |
| 1001 baseline | Jazz/1960/albumTrack/recentHit/legacyCategoryOnly | 71 |
| 1001 baseline | RockAndRoll/1960/albumTrack/traditional/legacyCategoryOnly | 7 |
| 1001 baseline | Gospel/1960/albumTrack/traditional/legacyCategoryOnly | 165 |
| 1001 baseline | Gospel/1960/albumTrack/standard/legacyCategoryOnly | 71 |
| 1001 baseline | Gospel/1960/albumTrack/recentHit/legacyCategoryOnly | 30 |
| 1001 baseline | Gospel/1960/albumTrack/original/legacyCategoryOnly | 43 |
| 1001 baseline | Gospel/1960/single/recentHit/legacyCategoryOnly | 4 |
| 1001 baseline | TeenPop/1960/single/recentHit/legacyCategoryOnly | 82 |
| 1001 baseline | TraditionalPop/1960/single/traditional/legacyCategoryOnly | 30 |
| 1001 baseline | DooWop/1960/single/traditional/legacyCategoryOnly | 30 |
| 1001 baseline | TraditionalPop/1960/single/original/legacyCategoryOnly | 73 |
| 1001 baseline | Classical/1960/albumTrack/original/legacyCategoryOnly | 281 |
| 1001 baseline | Classical/1960/albumTrack/traditional/legacyCategoryOnly | 232 |
| 1001 baseline | Classical/1960/albumTrack/recentHit/legacyCategoryOnly | 249 |
| 1001 baseline | Classical/1960/albumTrack/standard/legacyCategoryOnly | 203 |
| 1001 baseline | Country/1960/single/traditional/legacyCategoryOnly | 27 |
| 1001 baseline | Gospel/1960/single/traditional/legacyCategoryOnly | 24 |
| 1001 baseline | Folk/1960/single/original/legacyCategoryOnly | 22 |
| 1001 baseline | TeenPop/1960/single/traditional/legacyCategoryOnly | 52 |
| 1001 baseline | LatinPop/1960/single/recentHit/legacyCategoryOnly | 2 |
| 1001 baseline | EasyListening/1960/single/traditional/legacyCategoryOnly | 15 |
| 1001 baseline | Jazz/1960/single/recentHit/legacyCategoryOnly | 10 |
| 1001 baseline | Country/1960/single/recentHit/legacyCategoryOnly | 30 |
| 1001 baseline | Childrens/1960/albumTrack/original/legacyCategoryOnly | 54 |
| 1001 baseline | Childrens/1960/albumTrack/recentHit/legacyCategoryOnly | 52 |
| 1001 baseline | Childrens/1960/albumTrack/traditional/legacyCategoryOnly | 43 |
| 1001 baseline | Childrens/1960/albumTrack/standard/legacyCategoryOnly | 16 |
| 1001 baseline | Comedy/1960/single/recentHit/legacyCategoryOnly | 3 |
| 1001 baseline | Comedy/1960/single/traditional/legacyCategoryOnly | 1 |
| 1001 baseline | EasyListening/1960/single/recentHit/legacyCategoryOnly | 8 |
| 1001 baseline | LatinPop/1960/albumTrack/recentHit/legacyCategoryOnly | 32 |
| 1001 baseline | LatinPop/1960/albumTrack/traditional/legacyCategoryOnly | 19 |
| 1001 baseline | LatinPop/1960/albumTrack/original/legacyCategoryOnly | 35 |
| 1001 baseline | LatinPop/1960/albumTrack/standard/legacyCategoryOnly | 14 |
| 1001 baseline | Blues/1960/single/traditional/legacyCategoryOnly | 15 |
| 1001 baseline | TexMex/1960/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | LatinPop/1960/single/original/legacyCategoryOnly | 4 |
| 1001 baseline | Folk/1960/single/recentHit/legacyCategoryOnly | 8 |
| 1001 baseline | Classical/1960/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | Gospel/1960/single/standard/legacyCategoryOnly | 10 |
| 1001 baseline | RnB/1960/albumTrack/standard/legacyCategoryOnly | 1 |
| 1001 baseline | Soul/1960/single/standard/legacyCategoryOnly | 3 |
| 1001 baseline | Childrens/1960/single/traditional/legacyCategoryOnly | 2 |
| 1001 baseline | Blues/1960/single/recentHit/legacyCategoryOnly | 22 |
| 1001 baseline | RnB/1960/single/standard/legacyCategoryOnly | 8 |
| 1001 baseline | RockAndRoll/1960/albumTrack/standard/legacyCategoryOnly | 7 |
| 1001 baseline | Folk/1960/single/standard/legacyCategoryOnly | 9 |
| 1001 baseline | LatinPop/1960/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | TexMex/1960/single/recentHit/legacyCategoryOnly | 2 |
| 1001 baseline | DooWop/1960/single/standard/legacyCategoryOnly | 5 |
| 1001 baseline | TexMex/1960/albumTrack/standard/legacyCategoryOnly | 4 |
| 1001 baseline | RockAndRoll/1960/single/traditional/legacyCategoryOnly | 7 |
| 1001 baseline | Comedy/1960/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | Blues/1960/single/standard/legacyCategoryOnly | 4 |
| 1001 baseline | LatinPop/1960/single/traditional/legacyCategoryOnly | 3 |
| 1001 baseline | RockAndRoll/1960/single/standard/legacyCategoryOnly | 1 |
| 1001 baseline | Comedy/1960/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | SurfRock/1960/albumTrack/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | SurfRock/1960/albumTrack/original/legacyCategoryOnly | 5 |
| 1001 baseline | Classical/1960/single/recentHit/legacyCategoryOnly | 3 |
| 1001 baseline | Classical/1960/single/traditional/legacyCategoryOnly | 3 |
| 1001 baseline | TraditionalPop/1961/albumTrack/traditional/legacyCategoryOnly | 685 |
| 1001 baseline | TraditionalPop/1961/albumTrack/recentHit/legacyCategoryOnly | 242 |
| 1001 baseline | TraditionalPop/1961/albumTrack/original/legacyCategoryOnly | 995 |
| 1001 baseline | TraditionalPop/1961/albumTrack/standard/legacyCategoryOnly | 897 |
| 1001 baseline | Country/1961/single/original/legacyCategoryOnly | 179 |
| 1001 baseline | Country/1961/single/standard/legacyCategoryOnly | 25 |
| 1001 baseline | RnB/1961/single/traditional/legacyCategoryOnly | 64 |
| 1001 baseline | Soul/1961/albumTrack/recentHit/legacyCategoryOnly | 876 |
| 1001 baseline | Soul/1961/albumTrack/original/legacyCategoryOnly | 3603 |
| 1001 baseline | Soul/1961/albumTrack/traditional/legacyCategoryOnly | 507 |
| 1001 baseline | Classical/1961/albumTrack/original/legacyCategoryOnly | 235 |
| 1001 baseline | Classical/1961/albumTrack/standard/legacyCategoryOnly | 177 |
| 1001 baseline | Classical/1961/albumTrack/recentHit/legacyCategoryOnly | 180 |
| 1001 baseline | Classical/1961/albumTrack/traditional/legacyCategoryOnly | 214 |
| 1001 baseline | RockAndRoll/1961/single/recentHit/legacyCategoryOnly | 294 |
| 1001 baseline | Soul/1961/single/recentHit/legacyCategoryOnly | 37 |
| 1001 baseline | TeenPop/1961/single/original/legacyCategoryOnly | 311 |
| 1001 baseline | TeenPop/1961/single/traditional/legacyCategoryOnly | 53 |
| 1001 baseline | RnB/1961/single/original/legacyCategoryOnly | 363 |
| 1001 baseline | RnB/1961/albumTrack/original/legacyCategoryOnly | 870 |
| 1001 baseline | RnB/1961/albumTrack/recentHit/legacyCategoryOnly | 215 |
| 1001 baseline | RnB/1961/albumTrack/traditional/legacyCategoryOnly | 128 |
| 1001 baseline | Jazz/1961/albumTrack/original/legacyCategoryOnly | 444 |
| 1001 baseline | Jazz/1961/albumTrack/traditional/legacyCategoryOnly | 237 |
| 1001 baseline | Jazz/1961/albumTrack/standard/legacyCategoryOnly | 365 |
| 1001 baseline | Jazz/1961/albumTrack/recentHit/legacyCategoryOnly | 76 |
| 1001 baseline | RnB/1961/single/recentHit/legacyCategoryOnly | 111 |
| 1001 baseline | Jazz/1961/single/traditional/legacyCategoryOnly | 11 |
| 1001 baseline | Folk/1961/albumTrack/standard/legacyCategoryOnly | 345 |
| 1001 baseline | Folk/1961/albumTrack/original/legacyCategoryOnly | 1045 |
| 1001 baseline | Folk/1961/albumTrack/recentHit/legacyCategoryOnly | 229 |
| 1001 baseline | Folk/1961/albumTrack/traditional/legacyCategoryOnly | 774 |
| 1001 baseline | Soul/1961/single/original/legacyCategoryOnly | 149 |
| 1001 baseline | DooWop/1961/albumTrack/traditional/legacyCategoryOnly | 84 |
| 1001 baseline | DooWop/1961/albumTrack/recentHit/legacyCategoryOnly | 142 |
| 1001 baseline | DooWop/1961/albumTrack/original/legacyCategoryOnly | 162 |
| 1001 baseline | Country/1961/albumTrack/original/legacyCategoryOnly | 866 |
| 1001 baseline | Country/1961/albumTrack/recentHit/legacyCategoryOnly | 134 |
| 1001 baseline | Country/1961/albumTrack/traditional/legacyCategoryOnly | 157 |
| 1001 baseline | Country/1961/albumTrack/standard/legacyCategoryOnly | 88 |
| 1001 baseline | RockAndRoll/1961/albumTrack/recentHit/legacyCategoryOnly | 451 |
| 1001 baseline | RockAndRoll/1961/albumTrack/original/legacyCategoryOnly | 609 |
| 1001 baseline | TeenPop/1961/albumTrack/original/legacyCategoryOnly | 658 |
| 1001 baseline | TeenPop/1961/albumTrack/recentHit/legacyCategoryOnly | 114 |
| 1001 baseline | TeenPop/1961/albumTrack/traditional/legacyCategoryOnly | 110 |
| 1001 baseline | DooWop/1961/single/recentHit/legacyCategoryOnly | 66 |
| 1001 baseline | Soul/1961/single/traditional/legacyCategoryOnly | 23 |
| 1001 baseline | TraditionalPop/1961/single/traditional/legacyCategoryOnly | 30 |
| 1001 baseline | RockAndRoll/1961/albumTrack/traditional/legacyCategoryOnly | 9 |
| 1001 baseline | Childrens/1961/albumTrack/original/legacyCategoryOnly | 49 |
| 1001 baseline | Childrens/1961/albumTrack/traditional/legacyCategoryOnly | 36 |
| 1001 baseline | Childrens/1961/albumTrack/recentHit/legacyCategoryOnly | 38 |
| 1001 baseline | Childrens/1961/albumTrack/standard/legacyCategoryOnly | 16 |
| 1001 baseline | Folk/1961/single/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | TeenPop/1961/single/recentHit/legacyCategoryOnly | 67 |
| 1001 baseline | Jazz/1961/single/original/legacyCategoryOnly | 36 |
| 1001 baseline | Gospel/1961/albumTrack/traditional/legacyCategoryOnly | 325 |
| 1001 baseline | Gospel/1961/albumTrack/standard/legacyCategoryOnly | 116 |
| 1001 baseline | Gospel/1961/albumTrack/recentHit/legacyCategoryOnly | 55 |
| 1001 baseline | Gospel/1961/albumTrack/original/legacyCategoryOnly | 91 |
| 1001 baseline | EasyListening/1961/albumTrack/standard/legacyCategoryOnly | 348 |
| 1001 baseline | EasyListening/1961/albumTrack/traditional/legacyCategoryOnly | 294 |
| 1001 baseline | EasyListening/1961/albumTrack/original/legacyCategoryOnly | 400 |
| 1001 baseline | Country/1961/single/traditional/legacyCategoryOnly | 37 |
| 1001 baseline | RockAndRoll/1961/single/original/legacyCategoryOnly | 293 |
| 1001 baseline | Classical/1961/single/standard/legacyCategoryOnly | 4 |
| 1001 baseline | EasyListening/1961/albumTrack/recentHit/legacyCategoryOnly | 96 |
| 1001 baseline | Blues/1961/albumTrack/original/legacyCategoryOnly | 557 |
| 1001 baseline | Blues/1961/albumTrack/traditional/legacyCategoryOnly | 345 |
| 1001 baseline | Blues/1961/albumTrack/recentHit/legacyCategoryOnly | 349 |
| 1001 baseline | Classical/1961/single/traditional/legacyCategoryOnly | 4 |
| 1001 baseline | TeenPop/1961/single/standard/legacyCategoryOnly | 16 |
| 1001 baseline | TeenPop/1961/albumTrack/standard/legacyCategoryOnly | 24 |
| 1001 baseline | EasyListening/1961/single/recentHit/legacyCategoryOnly | 9 |
| 1001 baseline | DooWop/1961/single/traditional/legacyCategoryOnly | 38 |
| 1001 baseline | LatinPop/1961/single/standard/legacyCategoryOnly | 3 |
| 1001 baseline | TraditionalPop/1961/single/original/legacyCategoryOnly | 51 |
| 1001 baseline | TraditionalPop/1961/single/standard/legacyCategoryOnly | 31 |
| 1001 baseline | Folk/1961/single/original/legacyCategoryOnly | 17 |
| 1001 baseline | RockAndRoll/1961/albumTrack/standard/legacyCategoryOnly | 8 |
| 1001 baseline | Jazz/1961/single/standard/legacyCategoryOnly | 19 |
| 1001 baseline | LatinPop/1961/single/original/legacyCategoryOnly | 12 |
| 1001 baseline | TexMex/1961/single/recentHit/legacyCategoryOnly | 3 |
| 1001 baseline | Blues/1961/albumTrack/standard/legacyCategoryOnly | 100 |
| 1001 baseline | EasyListening/1961/single/standard/legacyCategoryOnly | 24 |
| 1001 baseline | Blues/1961/single/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | Jazz/1961/single/recentHit/legacyCategoryOnly | 5 |
| 1001 baseline | DooWop/1961/single/original/legacyCategoryOnly | 60 |
| 1001 baseline | Soul/1961/albumTrack/standard/legacyCategoryOnly | 10 |
| 1001 baseline | Folk/1961/single/standard/legacyCategoryOnly | 5 |
| 1001 baseline | Blues/1961/single/traditional/legacyCategoryOnly | 12 |
| 1001 baseline | EasyListening/1961/single/original/legacyCategoryOnly | 30 |
| 1001 baseline | Comedy/1961/single/original/legacyCategoryOnly | 5 |
| 1001 baseline | Comedy/1961/single/traditional/legacyCategoryOnly | 3 |
| 1001 baseline | TexMex/1961/albumTrack/original/legacyCategoryOnly | 49 |
| 1001 baseline | TexMex/1961/albumTrack/recentHit/legacyCategoryOnly | 43 |
| 1001 baseline | TexMex/1961/albumTrack/traditional/legacyCategoryOnly | 35 |
| 1001 baseline | DooWop/1961/albumTrack/standard/legacyCategoryOnly | 9 |
| 1001 baseline | LatinPop/1961/albumTrack/original/legacyCategoryOnly | 80 |
| 1001 baseline | LatinPop/1961/albumTrack/traditional/legacyCategoryOnly | 68 |
| 1001 baseline | LatinPop/1961/albumTrack/recentHit/legacyCategoryOnly | 66 |
| 1001 baseline | Gospel/1961/single/traditional/legacyCategoryOnly | 23 |
| 1001 baseline | EasyListening/1961/single/traditional/legacyCategoryOnly | 16 |
| 1001 baseline | TraditionalPop/1961/single/recentHit/legacyCategoryOnly | 11 |
| 1001 baseline | LatinPop/1961/albumTrack/standard/legacyCategoryOnly | 17 |
| 1001 baseline | SurfRock/1961/single/original/legacyCategoryOnly | 6 |
| 1001 baseline | Country/1961/single/recentHit/legacyCategoryOnly | 32 |
| 1001 baseline | Folk/1961/single/traditional/legacyCategoryOnly | 16 |
| 1001 baseline | RnB/1961/albumTrack/standard/legacyCategoryOnly | 1 |
| 1001 baseline | Comedy/1961/albumTrack/traditional/legacyCategoryOnly | 117 |
| 1001 baseline | Comedy/1961/albumTrack/recentHit/legacyCategoryOnly | 118 |
| 1001 baseline | Comedy/1961/albumTrack/original/legacyCategoryOnly | 134 |
| 1001 baseline | LatinPop/1961/single/recentHit/legacyCategoryOnly | 2 |
| 1001 baseline | Classical/1961/single/original/legacyCategoryOnly | 4 |
| 1001 baseline | Classical/1961/single/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | Comedy/1961/albumTrack/standard/legacyCategoryOnly | 58 |
| 1001 baseline | TexMex/1961/albumTrack/standard/legacyCategoryOnly | 12 |
| 1001 baseline | DooWop/1961/single/standard/legacyCategoryOnly | 7 |
| 1001 baseline | SurfRock/1961/single/recentHit/legacyCategoryOnly | 4 |
| 1001 baseline | Blues/1961/single/original/legacyCategoryOnly | 20 |
| 1001 baseline | Comedy/1961/single/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | Childrens/1961/single/traditional/legacyCategoryOnly | 2 |
| 1001 baseline | ContemporaryFolk/1961/albumTrack/traditional/legacyCategoryOnly | 8 |
| 1001 baseline | ContemporaryFolk/1961/albumTrack/standard/legacyCategoryOnly | 3 |
| 1001 baseline | ContemporaryFolk/1961/albumTrack/recentHit/legacyCategoryOnly | 1 |
| 1001 baseline | ContemporaryFolk/1961/albumTrack/original/legacyCategoryOnly | 9 |
| 1001 baseline | RockAndRoll/1961/single/traditional/legacyCategoryOnly | 6 |
| 1001 baseline | TexMex/1961/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | RnB/1961/single/standard/legacyCategoryOnly | 17 |
| 1001 baseline | Soul/1961/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | Gospel/1961/single/standard/legacyCategoryOnly | 7 |
| 1001 baseline | SurfRock/1961/albumTrack/recentHit/legacyCategoryOnly | 24 |
| 1001 baseline | SurfRock/1961/albumTrack/original/legacyCategoryOnly | 34 |
| 1001 baseline | LatinPop/1961/single/traditional/legacyCategoryOnly | 4 |
| 1001 baseline | Gospel/1961/single/original/legacyCategoryOnly | 6 |
| 1001 baseline | Childrens/1961/single/original/legacyCategoryOnly | 2 |
| 1001 baseline | RockAndRoll/1961/single/standard/legacyCategoryOnly | 3 |
| 1001 baseline | TexMex/1961/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | TexMex/1961/single/traditional/legacyCategoryOnly | 3 |
| 1001 baseline | Gospel/1961/single/recentHit/legacyCategoryOnly | 2 |
| 1001 baseline | Childrens/1961/single/standard/legacyCategoryOnly | 1 |
| 1001 baseline | Blues/1961/single/standard/legacyCategoryOnly | 3 |
| 1001 baseline | TraditionalPop/1962/albumTrack/traditional/legacyCategoryOnly | 700 |
| 1001 baseline | TraditionalPop/1962/albumTrack/recentHit/legacyCategoryOnly | 236 |
| 1001 baseline | TraditionalPop/1962/albumTrack/original/legacyCategoryOnly | 1010 |
| 1001 baseline | TraditionalPop/1962/albumTrack/standard/legacyCategoryOnly | 949 |
| 1001 baseline | RockAndRoll/1962/albumTrack/recentHit/legacyCategoryOnly | 511 |
| 1001 baseline | RockAndRoll/1962/albumTrack/original/legacyCategoryOnly | 708 |
| 1001 baseline | Comedy/1962/albumTrack/traditional/legacyCategoryOnly | 263 |
| 1001 baseline | Comedy/1962/albumTrack/recentHit/legacyCategoryOnly | 255 |
| 1001 baseline | Comedy/1962/albumTrack/original/legacyCategoryOnly | 325 |
| 1001 baseline | RnB/1962/albumTrack/original/legacyCategoryOnly | 897 |
| 1001 baseline | RnB/1962/albumTrack/traditional/legacyCategoryOnly | 137 |
| 1001 baseline | RnB/1962/albumTrack/recentHit/legacyCategoryOnly | 221 |
| 1001 baseline | Gospel/1962/single/traditional/legacyCategoryOnly | 33 |
| 1001 baseline | Soul/1962/albumTrack/original/legacyCategoryOnly | 3841 |
| 1001 baseline | Soul/1962/albumTrack/recentHit/legacyCategoryOnly | 930 |
| 1001 baseline | Soul/1962/albumTrack/traditional/legacyCategoryOnly | 548 |
| 1001 baseline | Comedy/1962/single/traditional/legacyCategoryOnly | 9 |
| 1001 baseline | RnB/1962/single/original/legacyCategoryOnly | 328 |
| 1001 baseline | RockAndRoll/1962/single/original/legacyCategoryOnly | 245 |
| 1001 baseline | TeenPop/1962/albumTrack/standard/legacyCategoryOnly | 32 |
| 1001 baseline | TeenPop/1962/albumTrack/traditional/legacyCategoryOnly | 123 |
| 1001 baseline | TeenPop/1962/albumTrack/original/legacyCategoryOnly | 686 |
| 1001 baseline | TeenPop/1962/albumTrack/recentHit/legacyCategoryOnly | 113 |
| 1001 baseline | TeenPop/1962/single/original/legacyCategoryOnly | 252 |
| 1001 baseline | DooWop/1962/single/traditional/legacyCategoryOnly | 48 |
| 1001 baseline | Soul/1962/single/original/legacyCategoryOnly | 82 |
| 1001 baseline | RockAndRoll/1962/single/recentHit/legacyCategoryOnly | 204 |
| 1001 baseline | Blues/1962/single/original/legacyCategoryOnly | 15 |
| 1001 baseline | Blues/1962/albumTrack/original/legacyCategoryOnly | 486 |
| 1001 baseline | Blues/1962/albumTrack/recentHit/legacyCategoryOnly | 311 |
| 1001 baseline | Blues/1962/albumTrack/traditional/legacyCategoryOnly | 315 |
| 1001 baseline | DooWop/1962/single/original/legacyCategoryOnly | 59 |
| 1001 baseline | Country/1962/single/traditional/legacyCategoryOnly | 46 |
| 1001 baseline | TraditionalPop/1962/single/original/legacyCategoryOnly | 42 |
| 1001 baseline | TeenPop/1962/single/traditional/legacyCategoryOnly | 48 |
| 1001 baseline | DooWop/1962/single/recentHit/legacyCategoryOnly | 45 |
| 1001 baseline | DooWop/1962/albumTrack/original/legacyCategoryOnly | 432 |
| 1001 baseline | DooWop/1962/albumTrack/recentHit/legacyCategoryOnly | 341 |
| 1001 baseline | DooWop/1962/albumTrack/traditional/legacyCategoryOnly | 226 |
| 1001 baseline | Soul/1962/single/standard/legacyCategoryOnly | 5 |
| 1001 baseline | Classical/1962/albumTrack/recentHit/legacyCategoryOnly | 199 |
| 1001 baseline | Classical/1962/albumTrack/original/legacyCategoryOnly | 257 |
| 1001 baseline | Classical/1962/albumTrack/standard/legacyCategoryOnly | 188 |
| 1001 baseline | Classical/1962/albumTrack/traditional/legacyCategoryOnly | 224 |
| 1001 baseline | Country/1962/albumTrack/original/legacyCategoryOnly | 1491 |
| 1001 baseline | Country/1962/albumTrack/traditional/legacyCategoryOnly | 261 |
| 1001 baseline | Country/1962/albumTrack/standard/legacyCategoryOnly | 156 |
| 1001 baseline | Country/1962/albumTrack/recentHit/legacyCategoryOnly | 229 |
| 1001 baseline | ContemporaryFolk/1962/albumTrack/standard/legacyCategoryOnly | 40 |
| 1001 baseline | ContemporaryFolk/1962/albumTrack/original/legacyCategoryOnly | 125 |
| 1001 baseline | ContemporaryFolk/1962/albumTrack/traditional/legacyCategoryOnly | 107 |
| 1001 baseline | ContemporaryFolk/1962/albumTrack/recentHit/legacyCategoryOnly | 30 |
| 1001 baseline | Folk/1962/albumTrack/original/legacyCategoryOnly | 1135 |
| 1001 baseline | Folk/1962/albumTrack/standard/legacyCategoryOnly | 369 |
| 1001 baseline | Folk/1962/albumTrack/recentHit/legacyCategoryOnly | 229 |
| 1001 baseline | Folk/1962/albumTrack/traditional/legacyCategoryOnly | 819 |
| 1001 baseline | LatinPop/1962/albumTrack/original/legacyCategoryOnly | 145 |
| 1001 baseline | LatinPop/1962/albumTrack/traditional/legacyCategoryOnly | 110 |
| 1001 baseline | LatinPop/1962/albumTrack/recentHit/legacyCategoryOnly | 105 |
| 1001 baseline | LatinPop/1962/albumTrack/standard/legacyCategoryOnly | 62 |
| 1001 baseline | TexMex/1962/albumTrack/recentHit/legacyCategoryOnly | 98 |
| 1001 baseline | TexMex/1962/albumTrack/original/legacyCategoryOnly | 124 |
| 1001 baseline | TexMex/1962/albumTrack/traditional/legacyCategoryOnly | 96 |
| 1001 baseline | TexMex/1962/albumTrack/standard/legacyCategoryOnly | 40 |
| 1001 baseline | LatinPop/1962/single/recentHit/legacyCategoryOnly | 1 |
| 1001 baseline | Comedy/1962/albumTrack/standard/legacyCategoryOnly | 110 |
| 1001 baseline | Soul/1962/albumTrack/standard/legacyCategoryOnly | 18 |
| 1001 baseline | Gospel/1962/single/standard/legacyCategoryOnly | 8 |
| 1001 baseline | Jazz/1962/albumTrack/traditional/legacyCategoryOnly | 263 |
| 1001 baseline | Jazz/1962/albumTrack/original/legacyCategoryOnly | 516 |
| 1001 baseline | Jazz/1962/albumTrack/standard/legacyCategoryOnly | 429 |
| 1001 baseline | RnB/1962/single/traditional/legacyCategoryOnly | 58 |
| 1001 baseline | Soul/1962/single/traditional/legacyCategoryOnly | 21 |
| 1001 baseline | Folk/1962/single/traditional/legacyCategoryOnly | 19 |
| 1001 baseline | Blues/1962/albumTrack/standard/legacyCategoryOnly | 86 |
| 1001 baseline | Country/1962/single/original/legacyCategoryOnly | 187 |
| 1001 baseline | Soul/1962/single/recentHit/legacyCategoryOnly | 23 |
| 1001 baseline | TeenPop/1962/single/recentHit/legacyCategoryOnly | 63 |
| 1001 baseline | EasyListening/1962/albumTrack/original/legacyCategoryOnly | 569 |
| 1001 baseline | EasyListening/1962/albumTrack/standard/legacyCategoryOnly | 499 |
| 1001 baseline | EasyListening/1962/albumTrack/traditional/legacyCategoryOnly | 414 |
| 1001 baseline | EasyListening/1962/albumTrack/recentHit/legacyCategoryOnly | 141 |
| 1001 baseline | Jazz/1962/albumTrack/recentHit/legacyCategoryOnly | 95 |
| 1001 baseline | RnB/1962/single/standard/legacyCategoryOnly | 13 |
| 1001 baseline | Blues/1962/single/recentHit/legacyCategoryOnly | 14 |
| 1001 baseline | EasyListening/1962/single/original/legacyCategoryOnly | 29 |
| 1001 baseline | TraditionalPop/1962/single/standard/legacyCategoryOnly | 23 |
| 1001 baseline | RnB/1962/single/recentHit/legacyCategoryOnly | 89 |
| 1001 baseline | Folk/1962/single/original/legacyCategoryOnly | 16 |
| 1001 baseline | Jazz/1962/single/standard/legacyCategoryOnly | 25 |
| 1001 baseline | Country/1962/single/standard/legacyCategoryOnly | 24 |
| 1001 baseline | Gospel/1962/albumTrack/traditional/legacyCategoryOnly | 476 |
| 1001 baseline | Gospel/1962/albumTrack/standard/legacyCategoryOnly | 152 |
| 1001 baseline | Gospel/1962/albumTrack/original/legacyCategoryOnly | 128 |
| 1001 baseline | Gospel/1962/albumTrack/recentHit/legacyCategoryOnly | 73 |
| 1001 baseline | EasyListening/1962/single/standard/legacyCategoryOnly | 8 |
| 1001 baseline | Blues/1962/single/traditional/legacyCategoryOnly | 12 |
| 1001 baseline | LatinPop/1962/single/traditional/legacyCategoryOnly | 9 |
| 1001 baseline | Comedy/1962/single/original/legacyCategoryOnly | 6 |
| 1001 baseline | TeenPop/1962/single/standard/legacyCategoryOnly | 21 |
| 1001 baseline | Country/1962/single/recentHit/legacyCategoryOnly | 25 |
| 1001 baseline | Gospel/1962/single/original/legacyCategoryOnly | 8 |
| 1001 baseline | Jazz/1962/single/traditional/legacyCategoryOnly | 19 |
| 1001 baseline | TraditionalPop/1962/single/traditional/legacyCategoryOnly | 21 |
| 1001 baseline | Jazz/1962/single/original/legacyCategoryOnly | 38 |
| 1001 baseline | Folk/1962/single/standard/legacyCategoryOnly | 7 |
| 1001 baseline | Jazz/1962/single/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | Blues/1962/single/standard/legacyCategoryOnly | 4 |
| 1001 baseline | Childrens/1962/albumTrack/traditional/legacyCategoryOnly | 137 |
| 1001 baseline | Childrens/1962/albumTrack/original/legacyCategoryOnly | 177 |
| 1001 baseline | Childrens/1962/albumTrack/standard/legacyCategoryOnly | 66 |
| 1001 baseline | Childrens/1962/albumTrack/recentHit/legacyCategoryOnly | 142 |
| 1001 baseline | SurfRock/1962/albumTrack/recentHit/legacyCategoryOnly | 176 |
| 1001 baseline | SurfRock/1962/albumTrack/original/legacyCategoryOnly | 237 |
| 1001 baseline | LatinPop/1962/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | RockAndRoll/1962/albumTrack/traditional/legacyCategoryOnly | 9 |
| 1001 baseline | DooWop/1962/albumTrack/standard/legacyCategoryOnly | 22 |
| 1001 baseline | EasyListening/1962/single/recentHit/legacyCategoryOnly | 10 |
| 1001 baseline | EasyListening/1962/single/traditional/legacyCategoryOnly | 15 |
| 1001 baseline | SurfRock/1962/single/recentHit/legacyCategoryOnly | 9 |
| 1001 baseline | TexMex/1962/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | Comedy/1962/single/standard/legacyCategoryOnly | 3 |
| 1001 baseline | TexMex/1962/single/original/legacyCategoryOnly | 12 |
| 1001 baseline | ContemporaryFolk/1962/single/traditional/legacyCategoryOnly | 5 |
| 1001 baseline | TraditionalPop/1962/single/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | LatinPop/1962/single/original/legacyCategoryOnly | 9 |
| 1001 baseline | Childrens/1962/single/original/legacyCategoryOnly | 5 |
| 1001 baseline | Folk/1962/single/recentHit/legacyCategoryOnly | 4 |
| 1001 baseline | Classical/1962/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | RockAndRoll/1962/single/traditional/legacyCategoryOnly | 4 |
| 1001 baseline | Comedy/1962/single/recentHit/legacyCategoryOnly | 5 |
| 1001 baseline | DooWop/1962/single/standard/legacyCategoryOnly | 3 |
| 1001 baseline | SurfRock/1962/single/original/legacyCategoryOnly | 10 |
| 1001 baseline | SurfRock/1962/albumTrack/standard/legacyCategoryOnly | 3 |
| 1001 baseline | TexMex/1962/single/recentHit/legacyCategoryOnly | 4 |
| 1001 baseline | RockAndRoll/1962/albumTrack/standard/legacyCategoryOnly | 6 |
| 1001 baseline | Classical/1962/single/standard/legacyCategoryOnly | 4 |
| 1001 baseline | RockAndRoll/1962/single/standard/legacyCategoryOnly | 5 |
| 1001 baseline | Gospel/1962/single/recentHit/legacyCategoryOnly | 5 |
| 1001 baseline | ContemporaryFolk/1962/single/standard/legacyCategoryOnly | 4 |
| 1001 baseline | Classical/1962/single/traditional/legacyCategoryOnly | 4 |
| 1001 baseline | ContemporaryFolk/1962/single/recentHit/legacyCategoryOnly | 1 |
| 1001 baseline | BossaNova/1962/albumTrack/standard/legacyCategoryOnly | 79 |
| 1001 baseline | BossaNova/1962/albumTrack/recentHit/legacyCategoryOnly | 13 |
| 1001 baseline | BossaNova/1962/albumTrack/traditional/legacyCategoryOnly | 56 |
| 1001 baseline | BossaNova/1962/albumTrack/original/legacyCategoryOnly | 67 |
| 1001 baseline | ContemporaryFolk/1962/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | Childrens/1962/single/traditional/legacyCategoryOnly | 1 |
| 1001 baseline | Childrens/1962/single/recentHit/legacyCategoryOnly | 1 |
| 1001 baseline | SurfRock/1962/albumTrack/traditional/legacyCategoryOnly | 2 |
| 1001 baseline | Childrens/1962/single/standard/legacyCategoryOnly | 1 |
| 1001 baseline | Classical/1962/single/recentHit/legacyCategoryOnly | 1 |
| 1001 baseline | TexMex/1962/single/traditional/legacyCategoryOnly | 3 |
| 1001 baseline | BossaNova/1962/single/standard/legacyCategoryOnly | 1 |
| 1001 baseline | TraditionalPop/1963/albumTrack/original/legacyCategoryOnly | 949 |
| 1001 baseline | TraditionalPop/1963/albumTrack/standard/legacyCategoryOnly | 897 |
| 1001 baseline | TraditionalPop/1963/albumTrack/traditional/legacyCategoryOnly | 661 |
| 1001 baseline | Country/1963/albumTrack/traditional/legacyCategoryOnly | 453 |
| 1001 baseline | Country/1963/albumTrack/recentHit/legacyCategoryOnly | 359 |
| 1001 baseline | Country/1963/albumTrack/original/legacyCategoryOnly | 2578 |
| 1001 baseline | TraditionalPop/1963/albumTrack/recentHit/legacyCategoryOnly | 207 |
| 1001 baseline | Folk/1963/albumTrack/standard/legacyCategoryOnly | 312 |
| 1001 baseline | Folk/1963/albumTrack/original/legacyCategoryOnly | 1056 |
| 1001 baseline | Folk/1963/albumTrack/traditional/legacyCategoryOnly | 780 |
| 1001 baseline | Folk/1963/albumTrack/recentHit/legacyCategoryOnly | 211 |
| 1001 baseline | RnB/1963/albumTrack/original/legacyCategoryOnly | 1096 |
| 1001 baseline | RnB/1963/albumTrack/traditional/legacyCategoryOnly | 161 |
| 1001 baseline | RnB/1963/albumTrack/recentHit/legacyCategoryOnly | 240 |
| 1001 baseline | Soul/1963/albumTrack/recentHit/legacyCategoryOnly | 773 |
| 1001 baseline | Soul/1963/albumTrack/original/legacyCategoryOnly | 3336 |
| 1001 baseline | Soul/1963/albumTrack/traditional/legacyCategoryOnly | 470 |
| 1001 baseline | TeenPop/1963/single/original/legacyCategoryOnly | 217 |
| 1001 baseline | RockAndRoll/1963/albumTrack/recentHit/legacyCategoryOnly | 504 |
| 1001 baseline | RockAndRoll/1963/albumTrack/original/legacyCategoryOnly | 802 |
| 1001 baseline | Comedy/1963/albumTrack/standard/legacyCategoryOnly | 220 |
| 1001 baseline | Comedy/1963/albumTrack/original/legacyCategoryOnly | 553 |
| 1001 baseline | Comedy/1963/albumTrack/traditional/legacyCategoryOnly | 356 |
| 1001 baseline | Comedy/1963/albumTrack/recentHit/legacyCategoryOnly | 387 |
| 1001 baseline | RnB/1963/single/traditional/legacyCategoryOnly | 38 |
| 1001 baseline | Country/1963/albumTrack/standard/legacyCategoryOnly | 273 |
| 1001 baseline | RockAndRoll/1963/single/original/legacyCategoryOnly | 174 |
| 1001 baseline | BossaNova/1963/single/traditional/legacyCategoryOnly | 5 |
| 1001 baseline | Jazz/1963/albumTrack/traditional/legacyCategoryOnly | 348 |
| 1001 baseline | Jazz/1963/albumTrack/original/legacyCategoryOnly | 689 |
| 1001 baseline | Jazz/1963/albumTrack/standard/legacyCategoryOnly | 547 |
| 1001 baseline | Jazz/1963/albumTrack/recentHit/legacyCategoryOnly | 108 |
| 1001 baseline | Classical/1963/albumTrack/original/legacyCategoryOnly | 446 |
| 1001 baseline | Classical/1963/albumTrack/traditional/legacyCategoryOnly | 317 |
| 1001 baseline | Classical/1963/albumTrack/recentHit/legacyCategoryOnly | 290 |
| 1001 baseline | Classical/1963/albumTrack/standard/legacyCategoryOnly | 327 |
| 1001 baseline | Gospel/1963/albumTrack/traditional/legacyCategoryOnly | 797 |
| 1001 baseline | Gospel/1963/albumTrack/standard/legacyCategoryOnly | 210 |
| 1001 baseline | Gospel/1963/albumTrack/original/legacyCategoryOnly | 254 |
| 1001 baseline | RnB/1963/single/original/legacyCategoryOnly | 223 |
| 1001 baseline | EasyListening/1963/albumTrack/traditional/legacyCategoryOnly | 657 |
| 1001 baseline | EasyListening/1963/albumTrack/original/legacyCategoryOnly | 937 |
| 1001 baseline | EasyListening/1963/albumTrack/standard/legacyCategoryOnly | 857 |
| 1001 baseline | EasyListening/1963/albumTrack/recentHit/legacyCategoryOnly | 211 |
| 1001 baseline | RockAndRoll/1963/albumTrack/traditional/legacyCategoryOnly | 11 |
| 1001 baseline | LatinPop/1963/albumTrack/recentHit/legacyCategoryOnly | 218 |
| 1001 baseline | LatinPop/1963/albumTrack/original/legacyCategoryOnly | 325 |
| 1001 baseline | LatinPop/1963/albumTrack/traditional/legacyCategoryOnly | 159 |
| 1001 baseline | LatinPop/1963/albumTrack/standard/legacyCategoryOnly | 123 |
| 1001 baseline | Classical/1963/single/recentHit/legacyCategoryOnly | 6 |
| 1001 baseline | DooWop/1963/albumTrack/traditional/legacyCategoryOnly | 284 |
| 1001 baseline | DooWop/1963/albumTrack/recentHit/legacyCategoryOnly | 406 |
| 1001 baseline | DooWop/1963/albumTrack/original/legacyCategoryOnly | 556 |
| 1001 baseline | TexMex/1963/albumTrack/original/legacyCategoryOnly | 226 |
| 1001 baseline | TexMex/1963/albumTrack/traditional/legacyCategoryOnly | 149 |
| 1001 baseline | TexMex/1963/albumTrack/standard/legacyCategoryOnly | 77 |
| 1001 baseline | TexMex/1963/albumTrack/recentHit/legacyCategoryOnly | 152 |
| 1001 baseline | TeenPop/1963/albumTrack/original/legacyCategoryOnly | 817 |
| 1001 baseline | TeenPop/1963/albumTrack/recentHit/legacyCategoryOnly | 121 |
| 1001 baseline | TeenPop/1963/albumTrack/traditional/legacyCategoryOnly | 135 |
| 1001 baseline | Soul/1963/single/recentHit/legacyCategoryOnly | 11 |
| 1001 baseline | Blues/1963/albumTrack/traditional/legacyCategoryOnly | 263 |
| 1001 baseline | Blues/1963/albumTrack/recentHit/legacyCategoryOnly | 250 |
| 1001 baseline | Blues/1963/albumTrack/original/legacyCategoryOnly | 428 |
| 1001 baseline | Blues/1963/albumTrack/standard/legacyCategoryOnly | 71 |
| 1001 baseline | Soul/1963/single/original/legacyCategoryOnly | 60 |
| 1001 baseline | TeenPop/1963/albumTrack/standard/legacyCategoryOnly | 58 |
| 1001 baseline | TeenPop/1963/single/traditional/legacyCategoryOnly | 46 |
| 1001 baseline | Country/1963/single/original/legacyCategoryOnly | 166 |
| 1001 baseline | SurfRock/1963/single/original/legacyCategoryOnly | 36 |
| 1001 baseline | RnB/1963/single/recentHit/legacyCategoryOnly | 42 |
| 1001 baseline | TeenPop/1963/single/recentHit/legacyCategoryOnly | 34 |
| 1001 baseline | Soul/1963/single/traditional/legacyCategoryOnly | 8 |
| 1001 baseline | TraditionalPop/1963/single/standard/legacyCategoryOnly | 21 |
| 1001 baseline | Gospel/1963/albumTrack/recentHit/legacyCategoryOnly | 129 |
| 1001 baseline | Comedy/1963/single/recentHit/legacyCategoryOnly | 7 |
| 1001 baseline | Jazz/1963/single/original/legacyCategoryOnly | 31 |
| 1001 baseline | ContemporaryFolk/1963/albumTrack/traditional/legacyCategoryOnly | 378 |
| 1001 baseline | ContemporaryFolk/1963/albumTrack/original/legacyCategoryOnly | 460 |
| 1001 baseline | ContemporaryFolk/1963/albumTrack/recentHit/legacyCategoryOnly | 97 |
| 1001 baseline | ContemporaryFolk/1963/albumTrack/standard/legacyCategoryOnly | 149 |
| 1001 baseline | Childrens/1963/albumTrack/original/legacyCategoryOnly | 495 |
| 1001 baseline | Childrens/1963/albumTrack/recentHit/legacyCategoryOnly | 342 |
| 1001 baseline | Childrens/1963/albumTrack/standard/legacyCategoryOnly | 171 |
| 1001 baseline | Childrens/1963/albumTrack/traditional/legacyCategoryOnly | 320 |
| 1001 baseline | RockAndRoll/1963/single/recentHit/legacyCategoryOnly | 148 |
| 1001 baseline | Folk/1963/single/standard/legacyCategoryOnly | 8 |
| 1001 baseline | BossaNova/1963/albumTrack/traditional/legacyCategoryOnly | 254 |
| 1001 baseline | BossaNova/1963/albumTrack/original/legacyCategoryOnly | 390 |
| 1001 baseline | BossaNova/1963/albumTrack/standard/legacyCategoryOnly | 442 |
| 1001 baseline | BossaNova/1963/albumTrack/recentHit/legacyCategoryOnly | 81 |
| 1001 baseline | Soul/1963/albumTrack/standard/legacyCategoryOnly | 26 |
| 1001 baseline | Country/1963/single/recentHit/legacyCategoryOnly | 33 |
| 1001 baseline | SurfRock/1963/albumTrack/original/legacyCategoryOnly | 854 |
| 1001 baseline | SurfRock/1963/albumTrack/recentHit/legacyCategoryOnly | 548 |
| 1001 baseline | DooWop/1963/albumTrack/standard/legacyCategoryOnly | 27 |
| 1001 baseline | DooWop/1963/single/original/legacyCategoryOnly | 64 |
| 1001 baseline | Country/1963/single/standard/legacyCategoryOnly | 26 |
| 1001 baseline | SurfRock/1963/albumTrack/traditional/legacyCategoryOnly | 11 |
| 1001 baseline | DooWop/1963/single/traditional/legacyCategoryOnly | 23 |
| 1001 baseline | Country/1963/single/traditional/legacyCategoryOnly | 51 |
| 1001 baseline | Jazz/1963/single/standard/legacyCategoryOnly | 19 |
| 1001 baseline | TraditionalPop/1963/single/original/legacyCategoryOnly | 29 |
| 1001 baseline | TraditionalPop/1963/single/traditional/legacyCategoryOnly | 15 |
| 1001 baseline | Jazz/1963/single/traditional/legacyCategoryOnly | 13 |
| 1001 baseline | EasyListening/1963/single/original/legacyCategoryOnly | 30 |
| 1001 baseline | TexMex/1963/single/traditional/legacyCategoryOnly | 7 |
| 1001 baseline | DooWop/1963/single/recentHit/legacyCategoryOnly | 37 |
| 1001 baseline | SurfRock/1963/single/recentHit/legacyCategoryOnly | 28 |
| 1001 baseline | EasyListening/1963/single/standard/legacyCategoryOnly | 23 |
| 1001 baseline | LatinPop/1963/single/original/legacyCategoryOnly | 4 |
| 1001 baseline | TexMex/1963/single/recentHit/legacyCategoryOnly | 3 |
| 1001 baseline | Gospel/1963/single/original/legacyCategoryOnly | 12 |
| 1001 baseline | Comedy/1963/single/traditional/legacyCategoryOnly | 14 |
| 1001 baseline | Childrens/1963/single/recentHit/legacyCategoryOnly | 5 |
| 1001 baseline | RnB/1963/albumTrack/standard/legacyCategoryOnly | 12 |
| 1001 baseline | EasyListening/1963/single/traditional/legacyCategoryOnly | 13 |
| 1001 baseline | LatinPop/1963/single/recentHit/legacyCategoryOnly | 4 |
| 1001 baseline | Blues/1963/single/original/legacyCategoryOnly | 8 |
| 1001 baseline | Classical/1963/single/standard/legacyCategoryOnly | 8 |
| 1001 baseline | TexMex/1963/single/original/legacyCategoryOnly | 15 |
| 1001 baseline | Folk/1963/single/original/legacyCategoryOnly | 9 |
| 1001 baseline | Childrens/1963/single/original/legacyCategoryOnly | 5 |
| 1001 baseline | Comedy/1963/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | Comedy/1963/single/original/legacyCategoryOnly | 10 |
| 1001 baseline | Folk/1963/single/traditional/legacyCategoryOnly | 19 |
| 1001 baseline | Gospel/1963/single/traditional/legacyCategoryOnly | 24 |
| 1001 baseline | ContemporaryFolk/1963/single/traditional/legacyCategoryOnly | 6 |
| 1001 baseline | Blues/1963/single/traditional/legacyCategoryOnly | 6 |
| 1001 baseline | ContemporaryFolk/1963/single/original/legacyCategoryOnly | 10 |
| 1001 baseline | LatinPop/1963/single/traditional/legacyCategoryOnly | 5 |
| 1001 baseline | DooWop/1963/single/standard/legacyCategoryOnly | 8 |
| 1001 baseline | TexMex/1963/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | BossaNova/1963/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | RnB/1963/single/standard/legacyCategoryOnly | 8 |
| 1001 baseline | TeenPop/1963/single/standard/legacyCategoryOnly | 15 |
| 1001 baseline | RockAndRoll/1963/single/standard/legacyCategoryOnly | 3 |
| 1001 baseline | Childrens/1963/single/traditional/legacyCategoryOnly | 4 |
| 1001 baseline | TraditionalPop/1963/single/recentHit/legacyCategoryOnly | 2 |
| 1001 baseline | Classical/1963/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | Gospel/1963/single/standard/legacyCategoryOnly | 5 |
| 1001 baseline | GarageRock/1963/albumTrack/original/legacyCategoryOnly | 80 |
| 1001 baseline | GarageRock/1963/albumTrack/recentHit/legacyCategoryOnly | 54 |
| 1001 baseline | EasyListening/1963/single/recentHit/legacyCategoryOnly | 2 |
| 1001 baseline | RockAndRoll/1963/albumTrack/standard/legacyCategoryOnly | 12 |
| 1001 baseline | ContemporaryFolk/1963/single/standard/legacyCategoryOnly | 4 |
| 1001 baseline | SurfRock/1963/albumTrack/standard/legacyCategoryOnly | 14 |
| 1001 baseline | RockAndRoll/1963/single/traditional/legacyCategoryOnly | 3 |
| 1001 baseline | GarageRock/1963/albumTrack/traditional/legacyCategoryOnly | 3 |
| 1001 baseline | BossaNova/1963/single/original/legacyCategoryOnly | 4 |
| 1001 baseline | GarageRock/1963/single/original/legacyCategoryOnly | 3 |
| 1001 baseline | Blues/1963/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | Gospel/1963/single/recentHit/legacyCategoryOnly | 3 |
| 1001 baseline | Folk/1963/single/recentHit/legacyCategoryOnly | 3 |
| 1001 baseline | Childrens/1963/single/standard/legacyCategoryOnly | 1 |
| 1001 baseline | LatinPop/1963/single/standard/legacyCategoryOnly | 1 |
| 1001 baseline | Blues/1963/single/recentHit/legacyCategoryOnly | 1 |
| 1001 baseline | Soul/1963/single/standard/legacyCategoryOnly | 2 |
| 1001 baseline | Jazz/1963/single/recentHit/legacyCategoryOnly | 1 |
| 1001 baseline | GarageRock/1963/single/recentHit/legacyCategoryOnly | 1 |
| 1002 baseline | Jazz/1960/single/standard/legacyCategoryOnly | 42 |
| 1002 baseline | TraditionalPop/1960/single/standard/legacyCategoryOnly | 59 |
| 1002 baseline | Country/1960/single/original/legacyCategoryOnly | 169 |
| 1002 baseline | Country/1960/albumTrack/traditional/legacyCategoryOnly | 119 |
| 1002 baseline | Country/1960/albumTrack/standard/legacyCategoryOnly | 73 |
| 1002 baseline | Country/1960/albumTrack/original/legacyCategoryOnly | 670 |
| 1002 baseline | Country/1960/albumTrack/recentHit/legacyCategoryOnly | 104 |
| 1002 baseline | RnB/1960/single/traditional/legacyCategoryOnly | 53 |
| 1002 baseline | RnB/1960/single/original/legacyCategoryOnly | 389 |
| 1002 baseline | RockAndRoll/1960/single/original/legacyCategoryOnly | 398 |
| 1002 baseline | TeenPop/1960/single/recentHit/legacyCategoryOnly | 88 |
| 1002 baseline | TeenPop/1960/single/original/legacyCategoryOnly | 380 |
| 1002 baseline | Soul/1960/albumTrack/traditional/legacyCategoryOnly | 493 |
| 1002 baseline | Soul/1960/albumTrack/original/legacyCategoryOnly | 3791 |
| 1002 baseline | Soul/1960/albumTrack/recentHit/legacyCategoryOnly | 928 |
| 1002 baseline | RockAndRoll/1960/single/recentHit/legacyCategoryOnly | 269 |
| 1002 baseline | TraditionalPop/1960/single/original/legacyCategoryOnly | 66 |
| 1002 baseline | Folk/1960/albumTrack/recentHit/legacyCategoryOnly | 188 |
| 1002 baseline | Folk/1960/albumTrack/traditional/legacyCategoryOnly | 667 |
| 1002 baseline | Folk/1960/albumTrack/original/legacyCategoryOnly | 912 |
| 1002 baseline | Folk/1960/albumTrack/standard/legacyCategoryOnly | 290 |
| 1002 baseline | Jazz/1960/single/traditional/legacyCategoryOnly | 17 |
| 1002 baseline | Jazz/1960/single/original/legacyCategoryOnly | 38 |
| 1002 baseline | TeenPop/1960/single/standard/legacyCategoryOnly | 30 |
| 1002 baseline | DooWop/1960/albumTrack/traditional/legacyCategoryOnly | 89 |
| 1002 baseline | DooWop/1960/albumTrack/recentHit/legacyCategoryOnly | 145 |
| 1002 baseline | DooWop/1960/albumTrack/original/legacyCategoryOnly | 184 |
| 1002 baseline | TeenPop/1960/single/traditional/legacyCategoryOnly | 65 |
| 1002 baseline | TraditionalPop/1960/albumTrack/recentHit/legacyCategoryOnly | 226 |
| 1002 baseline | TraditionalPop/1960/albumTrack/original/legacyCategoryOnly | 875 |
| 1002 baseline | TraditionalPop/1960/albumTrack/standard/legacyCategoryOnly | 857 |
| 1002 baseline | TraditionalPop/1960/albumTrack/traditional/legacyCategoryOnly | 575 |
| 1002 baseline | RnB/1960/albumTrack/recentHit/legacyCategoryOnly | 236 |
| 1002 baseline | RnB/1960/albumTrack/original/legacyCategoryOnly | 964 |
| 1002 baseline | RnB/1960/albumTrack/traditional/legacyCategoryOnly | 128 |
| 1002 baseline | Blues/1960/single/recentHit/legacyCategoryOnly | 14 |
| 1002 baseline | EasyListening/1960/single/traditional/legacyCategoryOnly | 12 |
| 1002 baseline | RockAndRoll/1960/albumTrack/recentHit/legacyCategoryOnly | 496 |
| 1002 baseline | RockAndRoll/1960/albumTrack/original/legacyCategoryOnly | 680 |
| 1002 baseline | Gospel/1960/albumTrack/standard/legacyCategoryOnly | 120 |
| 1002 baseline | Gospel/1960/albumTrack/original/legacyCategoryOnly | 72 |
| 1002 baseline | Gospel/1960/albumTrack/traditional/legacyCategoryOnly | 279 |
| 1002 baseline | Gospel/1960/albumTrack/recentHit/legacyCategoryOnly | 50 |
| 1002 baseline | TeenPop/1960/albumTrack/recentHit/legacyCategoryOnly | 102 |
| 1002 baseline | TeenPop/1960/albumTrack/standard/legacyCategoryOnly | 42 |
| 1002 baseline | TeenPop/1960/albumTrack/original/legacyCategoryOnly | 607 |
| 1002 baseline | TeenPop/1960/albumTrack/traditional/legacyCategoryOnly | 93 |
| 1002 baseline | Blues/1960/albumTrack/original/legacyCategoryOnly | 453 |
| 1002 baseline | Blues/1960/albumTrack/standard/legacyCategoryOnly | 93 |
| 1002 baseline | Blues/1960/albumTrack/traditional/legacyCategoryOnly | 266 |
| 1002 baseline | Blues/1960/albumTrack/recentHit/legacyCategoryOnly | 293 |
| 1002 baseline | Comedy/1960/albumTrack/recentHit/legacyCategoryOnly | 135 |
| 1002 baseline | Comedy/1960/albumTrack/original/legacyCategoryOnly | 152 |
| 1002 baseline | Comedy/1960/albumTrack/standard/legacyCategoryOnly | 53 |
| 1002 baseline | Comedy/1960/albumTrack/traditional/legacyCategoryOnly | 96 |
| 1002 baseline | Classical/1960/albumTrack/recentHit/legacyCategoryOnly | 270 |
| 1002 baseline | Classical/1960/albumTrack/original/legacyCategoryOnly | 307 |
| 1002 baseline | Classical/1960/albumTrack/standard/legacyCategoryOnly | 255 |
| 1002 baseline | Classical/1960/albumTrack/traditional/legacyCategoryOnly | 268 |
| 1002 baseline | EasyListening/1960/single/original/legacyCategoryOnly | 21 |
| 1002 baseline | EasyListening/1960/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | TraditionalPop/1960/single/traditional/legacyCategoryOnly | 33 |
| 1002 baseline | RockAndRoll/1960/albumTrack/standard/legacyCategoryOnly | 4 |
| 1002 baseline | Soul/1960/single/original/legacyCategoryOnly | 121 |
| 1002 baseline | Country/1960/single/recentHit/legacyCategoryOnly | 29 |
| 1002 baseline | Folk/1960/single/original/legacyCategoryOnly | 27 |
| 1002 baseline | Soul/1960/single/recentHit/legacyCategoryOnly | 34 |
| 1002 baseline | RockAndRoll/1960/albumTrack/traditional/legacyCategoryOnly | 12 |
| 1002 baseline | DooWop/1960/single/recentHit/legacyCategoryOnly | 65 |
| 1002 baseline | DooWop/1960/single/original/legacyCategoryOnly | 70 |
| 1002 baseline | Jazz/1960/albumTrack/traditional/legacyCategoryOnly | 216 |
| 1002 baseline | Jazz/1960/albumTrack/original/legacyCategoryOnly | 413 |
| 1002 baseline | Jazz/1960/albumTrack/standard/legacyCategoryOnly | 338 |
| 1002 baseline | Jazz/1960/albumTrack/recentHit/legacyCategoryOnly | 77 |
| 1002 baseline | Country/1960/single/traditional/legacyCategoryOnly | 24 |
| 1002 baseline | DooWop/1960/single/traditional/legacyCategoryOnly | 40 |
| 1002 baseline | EasyListening/1960/single/standard/legacyCategoryOnly | 31 |
| 1002 baseline | RnB/1960/single/recentHit/legacyCategoryOnly | 105 |
| 1002 baseline | Soul/1960/single/traditional/legacyCategoryOnly | 21 |
| 1002 baseline | EasyListening/1960/albumTrack/recentHit/legacyCategoryOnly | 119 |
| 1002 baseline | EasyListening/1960/albumTrack/standard/legacyCategoryOnly | 425 |
| 1002 baseline | EasyListening/1960/albumTrack/original/legacyCategoryOnly | 443 |
| 1002 baseline | EasyListening/1960/albumTrack/traditional/legacyCategoryOnly | 297 |
| 1002 baseline | Classical/1960/single/recentHit/legacyCategoryOnly | 10 |
| 1002 baseline | Childrens/1960/albumTrack/traditional/legacyCategoryOnly | 83 |
| 1002 baseline | Childrens/1960/albumTrack/standard/legacyCategoryOnly | 37 |
| 1002 baseline | Childrens/1960/albumTrack/recentHit/legacyCategoryOnly | 88 |
| 1002 baseline | Childrens/1960/albumTrack/original/legacyCategoryOnly | 106 |
| 1002 baseline | Gospel/1960/single/traditional/legacyCategoryOnly | 18 |
| 1002 baseline | Comedy/1960/single/recentHit/legacyCategoryOnly | 4 |
| 1002 baseline | Classical/1960/single/original/legacyCategoryOnly | 9 |
| 1002 baseline | Classical/1960/single/standard/legacyCategoryOnly | 8 |
| 1002 baseline | Soul/1960/single/standard/legacyCategoryOnly | 7 |
| 1002 baseline | Blues/1960/single/original/legacyCategoryOnly | 15 |
| 1002 baseline | Comedy/1960/single/traditional/legacyCategoryOnly | 1 |
| 1002 baseline | LatinPop/1960/single/original/legacyCategoryOnly | 7 |
| 1002 baseline | Comedy/1960/single/original/legacyCategoryOnly | 4 |
| 1002 baseline | LatinPop/1960/albumTrack/recentHit/legacyCategoryOnly | 24 |
| 1002 baseline | LatinPop/1960/albumTrack/traditional/legacyCategoryOnly | 20 |
| 1002 baseline | LatinPop/1960/albumTrack/original/legacyCategoryOnly | 35 |
| 1002 baseline | LatinPop/1960/albumTrack/standard/legacyCategoryOnly | 5 |
| 1002 baseline | Folk/1960/single/traditional/legacyCategoryOnly | 16 |
| 1002 baseline | RnB/1960/albumTrack/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Blues/1960/single/traditional/legacyCategoryOnly | 16 |
| 1002 baseline | Country/1960/single/standard/legacyCategoryOnly | 21 |
| 1002 baseline | Gospel/1960/single/standard/legacyCategoryOnly | 6 |
| 1002 baseline | Soul/1960/albumTrack/standard/legacyCategoryOnly | 8 |
| 1002 baseline | DooWop/1960/albumTrack/standard/legacyCategoryOnly | 8 |
| 1002 baseline | LatinPop/1960/single/recentHit/legacyCategoryOnly | 4 |
| 1002 baseline | DooWop/1960/single/standard/legacyCategoryOnly | 12 |
| 1002 baseline | TraditionalPop/1960/single/recentHit/legacyCategoryOnly | 19 |
| 1002 baseline | Folk/1960/single/standard/legacyCategoryOnly | 6 |
| 1002 baseline | Blues/1960/single/standard/legacyCategoryOnly | 4 |
| 1002 baseline | LatinPop/1960/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | LatinPop/1960/single/traditional/legacyCategoryOnly | 2 |
| 1002 baseline | RnB/1960/single/standard/legacyCategoryOnly | 11 |
| 1002 baseline | RockAndRoll/1960/single/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Childrens/1960/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | Folk/1960/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | TexMex/1960/single/original/legacyCategoryOnly | 2 |
| 1002 baseline | TexMex/1960/albumTrack/standard/legacyCategoryOnly | 4 |
| 1002 baseline | TexMex/1960/albumTrack/traditional/legacyCategoryOnly | 6 |
| 1002 baseline | TexMex/1960/albumTrack/recentHit/legacyCategoryOnly | 6 |
| 1002 baseline | TexMex/1960/albumTrack/original/legacyCategoryOnly | 9 |
| 1002 baseline | Jazz/1960/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | RockAndRoll/1960/single/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | Gospel/1960/single/original/legacyCategoryOnly | 2 |
| 1002 baseline | TexMex/1960/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | Childrens/1960/single/original/legacyCategoryOnly | 1 |
| 1002 baseline | SurfRock/1960/albumTrack/recentHit/legacyCategoryOnly | 7 |
| 1002 baseline | SurfRock/1960/albumTrack/original/legacyCategoryOnly | 6 |
| 1002 baseline | Classical/1960/single/traditional/legacyCategoryOnly | 1 |
| 1002 baseline | TexMex/1960/single/traditional/legacyCategoryOnly | 1 |
| 1002 baseline | Childrens/1960/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | Soul/1961/albumTrack/original/legacyCategoryOnly | 3144 |
| 1002 baseline | Soul/1961/albumTrack/traditional/legacyCategoryOnly | 449 |
| 1002 baseline | Soul/1961/albumTrack/recentHit/legacyCategoryOnly | 766 |
| 1002 baseline | RockAndRoll/1961/single/recentHit/legacyCategoryOnly | 270 |
| 1002 baseline | Jazz/1961/single/traditional/legacyCategoryOnly | 15 |
| 1002 baseline | RnB/1961/single/original/legacyCategoryOnly | 378 |
| 1002 baseline | Soul/1961/single/recentHit/legacyCategoryOnly | 28 |
| 1002 baseline | RnB/1961/single/recentHit/legacyCategoryOnly | 103 |
| 1002 baseline | RnB/1961/albumTrack/original/legacyCategoryOnly | 624 |
| 1002 baseline | RnB/1961/albumTrack/recentHit/legacyCategoryOnly | 153 |
| 1002 baseline | RnB/1961/albumTrack/traditional/legacyCategoryOnly | 92 |
| 1002 baseline | TeenPop/1961/albumTrack/original/legacyCategoryOnly | 644 |
| 1002 baseline | TeenPop/1961/albumTrack/traditional/legacyCategoryOnly | 108 |
| 1002 baseline | TeenPop/1961/albumTrack/recentHit/legacyCategoryOnly | 105 |
| 1002 baseline | Folk/1961/single/standard/legacyCategoryOnly | 10 |
| 1002 baseline | TeenPop/1961/single/recentHit/legacyCategoryOnly | 58 |
| 1002 baseline | RockAndRoll/1961/albumTrack/recentHit/legacyCategoryOnly | 406 |
| 1002 baseline | RockAndRoll/1961/albumTrack/original/legacyCategoryOnly | 538 |
| 1002 baseline | EasyListening/1961/single/standard/legacyCategoryOnly | 17 |
| 1002 baseline | TraditionalPop/1961/single/original/legacyCategoryOnly | 47 |
| 1002 baseline | Country/1961/albumTrack/original/legacyCategoryOnly | 858 |
| 1002 baseline | Country/1961/albumTrack/traditional/legacyCategoryOnly | 163 |
| 1002 baseline | Country/1961/albumTrack/recentHit/legacyCategoryOnly | 139 |
| 1002 baseline | TeenPop/1961/albumTrack/standard/legacyCategoryOnly | 21 |
| 1002 baseline | Folk/1961/albumTrack/traditional/legacyCategoryOnly | 564 |
| 1002 baseline | Folk/1961/albumTrack/recentHit/legacyCategoryOnly | 172 |
| 1002 baseline | Folk/1961/albumTrack/standard/legacyCategoryOnly | 267 |
| 1002 baseline | Folk/1961/albumTrack/original/legacyCategoryOnly | 799 |
| 1002 baseline | Comedy/1961/albumTrack/original/legacyCategoryOnly | 189 |
| 1002 baseline | Comedy/1961/albumTrack/standard/legacyCategoryOnly | 60 |
| 1002 baseline | Comedy/1961/albumTrack/recentHit/legacyCategoryOnly | 147 |
| 1002 baseline | Comedy/1961/albumTrack/traditional/legacyCategoryOnly | 136 |
| 1002 baseline | Soul/1961/single/original/legacyCategoryOnly | 105 |
| 1002 baseline | TraditionalPop/1961/albumTrack/traditional/legacyCategoryOnly | 652 |
| 1002 baseline | TraditionalPop/1961/albumTrack/standard/legacyCategoryOnly | 873 |
| 1002 baseline | TraditionalPop/1961/albumTrack/recentHit/legacyCategoryOnly | 231 |
| 1002 baseline | TraditionalPop/1961/albumTrack/original/legacyCategoryOnly | 971 |
| 1002 baseline | Folk/1961/single/original/legacyCategoryOnly | 20 |
| 1002 baseline | Gospel/1961/single/standard/legacyCategoryOnly | 8 |
| 1002 baseline | DooWop/1961/single/recentHit/legacyCategoryOnly | 66 |
| 1002 baseline | RockAndRoll/1961/single/original/legacyCategoryOnly | 308 |
| 1002 baseline | Country/1961/single/original/legacyCategoryOnly | 184 |
| 1002 baseline | DooWop/1961/albumTrack/original/legacyCategoryOnly | 202 |
| 1002 baseline | DooWop/1961/albumTrack/recentHit/legacyCategoryOnly | 171 |
| 1002 baseline | DooWop/1961/albumTrack/traditional/legacyCategoryOnly | 96 |
| 1002 baseline | Country/1961/albumTrack/standard/legacyCategoryOnly | 81 |
| 1002 baseline | Jazz/1961/albumTrack/recentHit/legacyCategoryOnly | 91 |
| 1002 baseline | Jazz/1961/albumTrack/traditional/legacyCategoryOnly | 262 |
| 1002 baseline | Jazz/1961/albumTrack/standard/legacyCategoryOnly | 410 |
| 1002 baseline | Jazz/1961/albumTrack/original/legacyCategoryOnly | 493 |
| 1002 baseline | EasyListening/1961/single/traditional/legacyCategoryOnly | 19 |
| 1002 baseline | DooWop/1961/albumTrack/standard/legacyCategoryOnly | 7 |
| 1002 baseline | SurfRock/1961/single/original/legacyCategoryOnly | 1 |
| 1002 baseline | Country/1961/single/standard/legacyCategoryOnly | 25 |
| 1002 baseline | Classical/1961/albumTrack/recentHit/legacyCategoryOnly | 221 |
| 1002 baseline | Classical/1961/albumTrack/original/legacyCategoryOnly | 230 |
| 1002 baseline | Classical/1961/albumTrack/traditional/legacyCategoryOnly | 218 |
| 1002 baseline | Classical/1961/albumTrack/standard/legacyCategoryOnly | 176 |
| 1002 baseline | TeenPop/1961/single/original/legacyCategoryOnly | 333 |
| 1002 baseline | DooWop/1961/single/traditional/legacyCategoryOnly | 55 |
| 1002 baseline | RnB/1961/single/traditional/legacyCategoryOnly | 63 |
| 1002 baseline | Blues/1961/albumTrack/original/legacyCategoryOnly | 425 |
| 1002 baseline | Blues/1961/albumTrack/traditional/legacyCategoryOnly | 266 |
| 1002 baseline | Blues/1961/albumTrack/recentHit/legacyCategoryOnly | 269 |
| 1002 baseline | RockAndRoll/1961/albumTrack/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | Blues/1961/albumTrack/standard/legacyCategoryOnly | 67 |
| 1002 baseline | DooWop/1961/single/standard/legacyCategoryOnly | 8 |
| 1002 baseline | Gospel/1961/single/traditional/legacyCategoryOnly | 32 |
| 1002 baseline | EasyListening/1961/albumTrack/traditional/legacyCategoryOnly | 293 |
| 1002 baseline | EasyListening/1961/albumTrack/standard/legacyCategoryOnly | 387 |
| 1002 baseline | EasyListening/1961/albumTrack/original/legacyCategoryOnly | 436 |
| 1002 baseline | EasyListening/1961/albumTrack/recentHit/legacyCategoryOnly | 109 |
| 1002 baseline | TraditionalPop/1961/single/standard/legacyCategoryOnly | 51 |
| 1002 baseline | Country/1961/single/traditional/legacyCategoryOnly | 44 |
| 1002 baseline | Jazz/1961/single/standard/legacyCategoryOnly | 34 |
| 1002 baseline | Soul/1961/single/traditional/legacyCategoryOnly | 18 |
| 1002 baseline | DooWop/1961/single/original/legacyCategoryOnly | 90 |
| 1002 baseline | EasyListening/1961/single/original/legacyCategoryOnly | 25 |
| 1002 baseline | TeenPop/1961/single/traditional/legacyCategoryOnly | 60 |
| 1002 baseline | TeenPop/1961/single/standard/legacyCategoryOnly | 28 |
| 1002 baseline | Blues/1961/single/recentHit/legacyCategoryOnly | 11 |
| 1002 baseline | Gospel/1961/albumTrack/traditional/legacyCategoryOnly | 323 |
| 1002 baseline | Gospel/1961/albumTrack/standard/legacyCategoryOnly | 116 |
| 1002 baseline | Gospel/1961/albumTrack/original/legacyCategoryOnly | 77 |
| 1002 baseline | TraditionalPop/1961/single/traditional/legacyCategoryOnly | 27 |
| 1002 baseline | Blues/1961/single/original/legacyCategoryOnly | 12 |
| 1002 baseline | Classical/1961/single/recentHit/legacyCategoryOnly | 9 |
| 1002 baseline | LatinPop/1961/albumTrack/original/legacyCategoryOnly | 54 |
| 1002 baseline | LatinPop/1961/albumTrack/recentHit/legacyCategoryOnly | 48 |
| 1002 baseline | LatinPop/1961/albumTrack/standard/legacyCategoryOnly | 19 |
| 1002 baseline | LatinPop/1961/albumTrack/traditional/legacyCategoryOnly | 40 |
| 1002 baseline | Childrens/1961/single/original/legacyCategoryOnly | 5 |
| 1002 baseline | Gospel/1961/albumTrack/recentHit/legacyCategoryOnly | 47 |
| 1002 baseline | Country/1961/single/recentHit/legacyCategoryOnly | 32 |
| 1002 baseline | TraditionalPop/1961/single/recentHit/legacyCategoryOnly | 8 |
| 1002 baseline | Blues/1961/single/standard/legacyCategoryOnly | 5 |
| 1002 baseline | Childrens/1961/albumTrack/traditional/legacyCategoryOnly | 102 |
| 1002 baseline | Childrens/1961/albumTrack/original/legacyCategoryOnly | 118 |
| 1002 baseline | Childrens/1961/albumTrack/recentHit/legacyCategoryOnly | 106 |
| 1002 baseline | Childrens/1961/albumTrack/standard/legacyCategoryOnly | 44 |
| 1002 baseline | Folk/1961/single/traditional/legacyCategoryOnly | 13 |
| 1002 baseline | Jazz/1961/single/original/legacyCategoryOnly | 38 |
| 1002 baseline | TexMex/1961/single/original/legacyCategoryOnly | 4 |
| 1002 baseline | Gospel/1961/single/original/legacyCategoryOnly | 8 |
| 1002 baseline | EasyListening/1961/single/recentHit/legacyCategoryOnly | 7 |
| 1002 baseline | Comedy/1961/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | Jazz/1961/single/recentHit/legacyCategoryOnly | 9 |
| 1002 baseline | Classical/1961/single/standard/legacyCategoryOnly | 3 |
| 1002 baseline | RnB/1961/single/standard/legacyCategoryOnly | 11 |
| 1002 baseline | TexMex/1961/albumTrack/original/legacyCategoryOnly | 27 |
| 1002 baseline | TexMex/1961/albumTrack/traditional/legacyCategoryOnly | 18 |
| 1002 baseline | TexMex/1961/albumTrack/recentHit/legacyCategoryOnly | 23 |
| 1002 baseline | TexMex/1961/albumTrack/standard/legacyCategoryOnly | 10 |
| 1002 baseline | SurfRock/1961/albumTrack/original/legacyCategoryOnly | 23 |
| 1002 baseline | SurfRock/1961/albumTrack/recentHit/legacyCategoryOnly | 19 |
| 1002 baseline | Soul/1961/albumTrack/standard/legacyCategoryOnly | 9 |
| 1002 baseline | RockAndRoll/1961/single/standard/legacyCategoryOnly | 4 |
| 1002 baseline | Classical/1961/single/traditional/legacyCategoryOnly | 5 |
| 1002 baseline | Blues/1961/single/traditional/legacyCategoryOnly | 2 |
| 1002 baseline | Soul/1961/single/standard/legacyCategoryOnly | 4 |
| 1002 baseline | TexMex/1961/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | Childrens/1961/single/standard/legacyCategoryOnly | 3 |
| 1002 baseline | Folk/1961/single/recentHit/legacyCategoryOnly | 3 |
| 1002 baseline | TexMex/1961/single/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Comedy/1961/single/traditional/legacyCategoryOnly | 4 |
| 1002 baseline | LatinPop/1961/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | TexMex/1961/single/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | RockAndRoll/1961/albumTrack/standard/legacyCategoryOnly | 5 |
| 1002 baseline | Classical/1961/single/original/legacyCategoryOnly | 1 |
| 1002 baseline | LatinPop/1961/single/original/legacyCategoryOnly | 2 |
| 1002 baseline | Gospel/1961/single/recentHit/legacyCategoryOnly | 4 |
| 1002 baseline | LatinPop/1961/single/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Comedy/1961/single/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Comedy/1961/single/original/legacyCategoryOnly | 7 |
| 1002 baseline | SurfRock/1961/single/recentHit/legacyCategoryOnly | 3 |
| 1002 baseline | Childrens/1961/single/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | Childrens/1961/single/recentHit/legacyCategoryOnly | 1 |
| 1002 baseline | RnB/1961/albumTrack/standard/legacyCategoryOnly | 1 |
| 1002 baseline | ContemporaryFolk/1961/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | EasyListening/1962/albumTrack/standard/legacyCategoryOnly | 438 |
| 1002 baseline | EasyListening/1962/albumTrack/original/legacyCategoryOnly | 479 |
| 1002 baseline | EasyListening/1962/albumTrack/traditional/legacyCategoryOnly | 338 |
| 1002 baseline | EasyListening/1962/albumTrack/recentHit/legacyCategoryOnly | 106 |
| 1002 baseline | RockAndRoll/1962/albumTrack/recentHit/legacyCategoryOnly | 517 |
| 1002 baseline | RockAndRoll/1962/albumTrack/original/legacyCategoryOnly | 714 |
| 1002 baseline | Country/1962/albumTrack/original/legacyCategoryOnly | 1551 |
| 1002 baseline | Country/1962/albumTrack/standard/legacyCategoryOnly | 182 |
| 1002 baseline | Country/1962/albumTrack/recentHit/legacyCategoryOnly | 237 |
| 1002 baseline | Country/1962/albumTrack/traditional/legacyCategoryOnly | 266 |
| 1002 baseline | TraditionalPop/1962/albumTrack/original/legacyCategoryOnly | 1069 |
| 1002 baseline | TraditionalPop/1962/albumTrack/standard/legacyCategoryOnly | 998 |
| 1002 baseline | TraditionalPop/1962/albumTrack/traditional/legacyCategoryOnly | 747 |
| 1002 baseline | TraditionalPop/1962/albumTrack/recentHit/legacyCategoryOnly | 238 |
| 1002 baseline | TeenPop/1962/single/original/legacyCategoryOnly | 275 |
| 1002 baseline | RockAndRoll/1962/single/original/legacyCategoryOnly | 249 |
| 1002 baseline | Soul/1962/albumTrack/original/legacyCategoryOnly | 2938 |
| 1002 baseline | Soul/1962/albumTrack/recentHit/legacyCategoryOnly | 711 |
| 1002 baseline | Soul/1962/albumTrack/traditional/legacyCategoryOnly | 421 |
| 1002 baseline | RnB/1962/single/original/legacyCategoryOnly | 304 |
| 1002 baseline | TeenPop/1962/albumTrack/recentHit/legacyCategoryOnly | 123 |
| 1002 baseline | TeenPop/1962/albumTrack/original/legacyCategoryOnly | 729 |
| 1002 baseline | TeenPop/1962/albumTrack/standard/legacyCategoryOnly | 35 |
| 1002 baseline | TeenPop/1962/albumTrack/traditional/legacyCategoryOnly | 124 |
| 1002 baseline | DooWop/1962/albumTrack/recentHit/legacyCategoryOnly | 327 |
| 1002 baseline | DooWop/1962/albumTrack/original/legacyCategoryOnly | 406 |
| 1002 baseline | DooWop/1962/albumTrack/traditional/legacyCategoryOnly | 197 |
| 1002 baseline | RockAndRoll/1962/single/recentHit/legacyCategoryOnly | 243 |
| 1002 baseline | TraditionalPop/1962/single/standard/legacyCategoryOnly | 26 |
| 1002 baseline | Folk/1962/single/original/legacyCategoryOnly | 22 |
| 1002 baseline | Gospel/1962/single/traditional/legacyCategoryOnly | 34 |
| 1002 baseline | RockAndRoll/1962/albumTrack/traditional/legacyCategoryOnly | 13 |
| 1002 baseline | RnB/1962/albumTrack/traditional/legacyCategoryOnly | 158 |
| 1002 baseline | RnB/1962/albumTrack/original/legacyCategoryOnly | 1065 |
| 1002 baseline | RnB/1962/albumTrack/recentHit/legacyCategoryOnly | 255 |
| 1002 baseline | Jazz/1962/albumTrack/standard/legacyCategoryOnly | 452 |
| 1002 baseline | Jazz/1962/albumTrack/original/legacyCategoryOnly | 580 |
| 1002 baseline | Jazz/1962/albumTrack/traditional/legacyCategoryOnly | 329 |
| 1002 baseline | TraditionalPop/1962/single/traditional/legacyCategoryOnly | 24 |
| 1002 baseline | Soul/1962/single/original/legacyCategoryOnly | 76 |
| 1002 baseline | Blues/1962/albumTrack/traditional/legacyCategoryOnly | 293 |
| 1002 baseline | Blues/1962/albumTrack/recentHit/legacyCategoryOnly | 274 |
| 1002 baseline | Blues/1962/albumTrack/original/legacyCategoryOnly | 453 |
| 1002 baseline | Blues/1962/albumTrack/standard/legacyCategoryOnly | 74 |
| 1002 baseline | Folk/1962/albumTrack/original/legacyCategoryOnly | 1091 |
| 1002 baseline | Folk/1962/albumTrack/traditional/legacyCategoryOnly | 816 |
| 1002 baseline | Folk/1962/albumTrack/standard/legacyCategoryOnly | 366 |
| 1002 baseline | Folk/1962/albumTrack/recentHit/legacyCategoryOnly | 228 |
| 1002 baseline | Jazz/1962/albumTrack/recentHit/legacyCategoryOnly | 102 |
| 1002 baseline | RnB/1962/single/traditional/legacyCategoryOnly | 51 |
| 1002 baseline | Classical/1962/albumTrack/traditional/legacyCategoryOnly | 285 |
| 1002 baseline | Classical/1962/albumTrack/standard/legacyCategoryOnly | 230 |
| 1002 baseline | Classical/1962/albumTrack/original/legacyCategoryOnly | 319 |
| 1002 baseline | Classical/1962/albumTrack/recentHit/legacyCategoryOnly | 229 |
| 1002 baseline | EasyListening/1962/single/traditional/legacyCategoryOnly | 13 |
| 1002 baseline | Gospel/1962/albumTrack/traditional/legacyCategoryOnly | 618 |
| 1002 baseline | Gospel/1962/albumTrack/original/legacyCategoryOnly | 155 |
| 1002 baseline | Gospel/1962/albumTrack/standard/legacyCategoryOnly | 184 |
| 1002 baseline | Gospel/1962/albumTrack/recentHit/legacyCategoryOnly | 92 |
| 1002 baseline | DooWop/1962/single/traditional/legacyCategoryOnly | 46 |
| 1002 baseline | Blues/1962/single/traditional/legacyCategoryOnly | 11 |
| 1002 baseline | TeenPop/1962/single/standard/legacyCategoryOnly | 26 |
| 1002 baseline | LatinPop/1962/albumTrack/traditional/legacyCategoryOnly | 83 |
| 1002 baseline | LatinPop/1962/albumTrack/original/legacyCategoryOnly | 125 |
| 1002 baseline | LatinPop/1962/albumTrack/recentHit/legacyCategoryOnly | 91 |
| 1002 baseline | LatinPop/1962/albumTrack/standard/legacyCategoryOnly | 36 |
| 1002 baseline | Country/1962/single/original/legacyCategoryOnly | 190 |
| 1002 baseline | Comedy/1962/single/original/legacyCategoryOnly | 8 |
| 1002 baseline | RnB/1962/single/recentHit/legacyCategoryOnly | 85 |
| 1002 baseline | Blues/1962/single/original/legacyCategoryOnly | 15 |
| 1002 baseline | DooWop/1962/single/original/legacyCategoryOnly | 66 |
| 1002 baseline | DooWop/1962/single/recentHit/legacyCategoryOnly | 47 |
| 1002 baseline | EasyListening/1962/single/original/legacyCategoryOnly | 18 |
| 1002 baseline | Jazz/1962/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | TeenPop/1962/single/recentHit/legacyCategoryOnly | 55 |
| 1002 baseline | TeenPop/1962/single/traditional/legacyCategoryOnly | 45 |
| 1002 baseline | EasyListening/1962/single/standard/legacyCategoryOnly | 19 |
| 1002 baseline | TexMex/1962/albumTrack/original/legacyCategoryOnly | 64 |
| 1002 baseline | TexMex/1962/albumTrack/recentHit/legacyCategoryOnly | 48 |
| 1002 baseline | TexMex/1962/albumTrack/standard/legacyCategoryOnly | 17 |
| 1002 baseline | TexMex/1962/albumTrack/traditional/legacyCategoryOnly | 27 |
| 1002 baseline | Jazz/1962/single/traditional/legacyCategoryOnly | 21 |
| 1002 baseline | Jazz/1962/single/standard/legacyCategoryOnly | 28 |
| 1002 baseline | Country/1962/single/traditional/legacyCategoryOnly | 46 |
| 1002 baseline | TraditionalPop/1962/single/original/legacyCategoryOnly | 43 |
| 1002 baseline | Comedy/1962/albumTrack/original/legacyCategoryOnly | 313 |
| 1002 baseline | Comedy/1962/albumTrack/traditional/legacyCategoryOnly | 256 |
| 1002 baseline | Comedy/1962/albumTrack/standard/legacyCategoryOnly | 109 |
| 1002 baseline | Comedy/1962/albumTrack/recentHit/legacyCategoryOnly | 255 |
| 1002 baseline | Childrens/1962/albumTrack/traditional/legacyCategoryOnly | 210 |
| 1002 baseline | Childrens/1962/albumTrack/original/legacyCategoryOnly | 234 |
| 1002 baseline | Childrens/1962/albumTrack/recentHit/legacyCategoryOnly | 198 |
| 1002 baseline | Childrens/1962/albumTrack/standard/legacyCategoryOnly | 94 |
| 1002 baseline | Classical/1962/single/original/legacyCategoryOnly | 9 |
| 1002 baseline | SurfRock/1962/single/recentHit/legacyCategoryOnly | 14 |
| 1002 baseline | Gospel/1962/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | EasyListening/1962/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | Childrens/1962/single/recentHit/legacyCategoryOnly | 3 |
| 1002 baseline | Country/1962/single/standard/legacyCategoryOnly | 24 |
| 1002 baseline | Comedy/1962/single/recentHit/legacyCategoryOnly | 4 |
| 1002 baseline | Classical/1962/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | Jazz/1962/single/original/legacyCategoryOnly | 38 |
| 1002 baseline | Classical/1962/single/traditional/legacyCategoryOnly | 6 |
| 1002 baseline | Country/1962/single/recentHit/legacyCategoryOnly | 30 |
| 1002 baseline | SurfRock/1962/single/original/legacyCategoryOnly | 16 |
| 1002 baseline | ContemporaryFolk/1962/single/original/legacyCategoryOnly | 4 |
| 1002 baseline | Folk/1962/single/traditional/legacyCategoryOnly | 15 |
| 1002 baseline | Soul/1962/albumTrack/standard/legacyCategoryOnly | 13 |
| 1002 baseline | TexMex/1962/single/original/legacyCategoryOnly | 10 |
| 1002 baseline | Folk/1962/single/standard/legacyCategoryOnly | 9 |
| 1002 baseline | Gospel/1962/single/standard/legacyCategoryOnly | 13 |
| 1002 baseline | Soul/1962/single/standard/legacyCategoryOnly | 3 |
| 1002 baseline | TraditionalPop/1962/single/recentHit/legacyCategoryOnly | 10 |
| 1002 baseline | LatinPop/1962/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | SurfRock/1962/albumTrack/original/legacyCategoryOnly | 176 |
| 1002 baseline | SurfRock/1962/albumTrack/recentHit/legacyCategoryOnly | 136 |
| 1002 baseline | Soul/1962/single/recentHit/legacyCategoryOnly | 14 |
| 1002 baseline | Childrens/1962/single/original/legacyCategoryOnly | 5 |
| 1002 baseline | DooWop/1962/albumTrack/standard/legacyCategoryOnly | 23 |
| 1002 baseline | RockAndRoll/1962/single/traditional/legacyCategoryOnly | 6 |
| 1002 baseline | LatinPop/1962/single/original/legacyCategoryOnly | 5 |
| 1002 baseline | Gospel/1962/single/original/legacyCategoryOnly | 7 |
| 1002 baseline | ContemporaryFolk/1962/albumTrack/traditional/legacyCategoryOnly | 95 |
| 1002 baseline | ContemporaryFolk/1962/albumTrack/standard/legacyCategoryOnly | 39 |
| 1002 baseline | ContemporaryFolk/1962/albumTrack/recentHit/legacyCategoryOnly | 26 |
| 1002 baseline | ContemporaryFolk/1962/albumTrack/original/legacyCategoryOnly | 108 |
| 1002 baseline | DooWop/1962/single/standard/legacyCategoryOnly | 6 |
| 1002 baseline | LatinPop/1962/single/traditional/legacyCategoryOnly | 5 |
| 1002 baseline | Soul/1962/single/traditional/legacyCategoryOnly | 7 |
| 1002 baseline | Folk/1962/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | RnB/1962/single/standard/legacyCategoryOnly | 8 |
| 1002 baseline | RockAndRoll/1962/albumTrack/standard/legacyCategoryOnly | 5 |
| 1002 baseline | Classical/1962/single/standard/legacyCategoryOnly | 4 |
| 1002 baseline | TexMex/1962/single/traditional/legacyCategoryOnly | 4 |
| 1002 baseline | Comedy/1962/single/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Blues/1962/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | BossaNova/1962/albumTrack/original/legacyCategoryOnly | 17 |
| 1002 baseline | BossaNova/1962/albumTrack/recentHit/legacyCategoryOnly | 6 |
| 1002 baseline | BossaNova/1962/albumTrack/traditional/legacyCategoryOnly | 12 |
| 1002 baseline | BossaNova/1962/albumTrack/standard/legacyCategoryOnly | 26 |
| 1002 baseline | Childrens/1962/single/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | Childrens/1962/single/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Comedy/1962/single/traditional/legacyCategoryOnly | 4 |
| 1002 baseline | ContemporaryFolk/1962/single/traditional/legacyCategoryOnly | 4 |
| 1002 baseline | SurfRock/1962/albumTrack/traditional/legacyCategoryOnly | 2 |
| 1002 baseline | RockAndRoll/1962/single/standard/legacyCategoryOnly | 3 |
| 1002 baseline | TexMex/1962/single/recentHit/legacyCategoryOnly | 1 |
| 1002 baseline | SurfRock/1962/single/traditional/legacyCategoryOnly | 1 |
| 1002 baseline | SurfRock/1962/albumTrack/standard/legacyCategoryOnly | 2 |
| 1002 baseline | LatinPop/1962/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | BossaNova/1962/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | BossaNova/1962/single/original/legacyCategoryOnly | 2 |
| 1002 baseline | TexMex/1962/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | Blues/1963/albumTrack/traditional/legacyCategoryOnly | 377 |
| 1002 baseline | Blues/1963/albumTrack/recentHit/legacyCategoryOnly | 367 |
| 1002 baseline | Blues/1963/albumTrack/original/legacyCategoryOnly | 607 |
| 1002 baseline | Blues/1963/albumTrack/standard/legacyCategoryOnly | 72 |
| 1002 baseline | RockAndRoll/1963/single/recentHit/legacyCategoryOnly | 130 |
| 1002 baseline | ContemporaryFolk/1963/albumTrack/standard/legacyCategoryOnly | 137 |
| 1002 baseline | ContemporaryFolk/1963/albumTrack/traditional/legacyCategoryOnly | 330 |
| 1002 baseline | ContemporaryFolk/1963/albumTrack/original/legacyCategoryOnly | 403 |
| 1002 baseline | ContemporaryFolk/1963/albumTrack/recentHit/legacyCategoryOnly | 89 |
| 1002 baseline | TraditionalPop/1963/albumTrack/original/legacyCategoryOnly | 1127 |
| 1002 baseline | TraditionalPop/1963/albumTrack/standard/legacyCategoryOnly | 1088 |
| 1002 baseline | TraditionalPop/1963/albumTrack/recentHit/legacyCategoryOnly | 244 |
| 1002 baseline | TraditionalPop/1963/albumTrack/traditional/legacyCategoryOnly | 736 |
| 1002 baseline | Gospel/1963/albumTrack/original/legacyCategoryOnly | 260 |
| 1002 baseline | Gospel/1963/albumTrack/traditional/legacyCategoryOnly | 770 |
| 1002 baseline | Gospel/1963/albumTrack/standard/legacyCategoryOnly | 227 |
| 1002 baseline | Gospel/1963/albumTrack/recentHit/legacyCategoryOnly | 110 |
| 1002 baseline | Soul/1963/albumTrack/traditional/legacyCategoryOnly | 419 |
| 1002 baseline | Soul/1963/albumTrack/original/legacyCategoryOnly | 2909 |
| 1002 baseline | Soul/1963/albumTrack/recentHit/legacyCategoryOnly | 664 |
| 1002 baseline | RockAndRoll/1963/albumTrack/recentHit/legacyCategoryOnly | 503 |
| 1002 baseline | RockAndRoll/1963/albumTrack/original/legacyCategoryOnly | 793 |
| 1002 baseline | EasyListening/1963/single/standard/legacyCategoryOnly | 28 |
| 1002 baseline | Soul/1963/single/original/legacyCategoryOnly | 41 |
| 1002 baseline | RockAndRoll/1963/single/original/legacyCategoryOnly | 196 |
| 1002 baseline | TraditionalPop/1963/single/standard/legacyCategoryOnly | 29 |
| 1002 baseline | RnB/1963/albumTrack/traditional/legacyCategoryOnly | 168 |
| 1002 baseline | RnB/1963/albumTrack/original/legacyCategoryOnly | 1099 |
| 1002 baseline | RnB/1963/albumTrack/recentHit/legacyCategoryOnly | 256 |
| 1002 baseline | DooWop/1963/albumTrack/recentHit/legacyCategoryOnly | 350 |
| 1002 baseline | DooWop/1963/albumTrack/traditional/legacyCategoryOnly | 266 |
| 1002 baseline | DooWop/1963/albumTrack/original/legacyCategoryOnly | 473 |
| 1002 baseline | DooWop/1963/albumTrack/standard/legacyCategoryOnly | 39 |
| 1002 baseline | EasyListening/1963/albumTrack/standard/legacyCategoryOnly | 771 |
| 1002 baseline | EasyListening/1963/albumTrack/original/legacyCategoryOnly | 803 |
| 1002 baseline | EasyListening/1963/albumTrack/traditional/legacyCategoryOnly | 530 |
| 1002 baseline | EasyListening/1963/albumTrack/recentHit/legacyCategoryOnly | 181 |
| 1002 baseline | RnB/1963/single/recentHit/legacyCategoryOnly | 74 |
| 1002 baseline | TexMex/1963/albumTrack/original/legacyCategoryOnly | 236 |
| 1002 baseline | TexMex/1963/albumTrack/traditional/legacyCategoryOnly | 131 |
| 1002 baseline | TexMex/1963/albumTrack/standard/legacyCategoryOnly | 75 |
| 1002 baseline | TexMex/1963/albumTrack/recentHit/legacyCategoryOnly | 144 |
| 1002 baseline | TeenPop/1963/albumTrack/original/legacyCategoryOnly | 1024 |
| 1002 baseline | TeenPop/1963/albumTrack/recentHit/legacyCategoryOnly | 159 |
| 1002 baseline | TeenPop/1963/albumTrack/traditional/legacyCategoryOnly | 177 |
| 1002 baseline | TeenPop/1963/albumTrack/standard/legacyCategoryOnly | 66 |
| 1002 baseline | Country/1963/single/original/legacyCategoryOnly | 187 |
| 1002 baseline | Comedy/1963/albumTrack/original/legacyCategoryOnly | 711 |
| 1002 baseline | Comedy/1963/albumTrack/traditional/legacyCategoryOnly | 459 |
| 1002 baseline | Comedy/1963/albumTrack/recentHit/legacyCategoryOnly | 452 |
| 1002 baseline | Comedy/1963/albumTrack/standard/legacyCategoryOnly | 242 |
| 1002 baseline | RnB/1963/single/original/legacyCategoryOnly | 231 |
| 1002 baseline | RnB/1963/single/standard/legacyCategoryOnly | 7 |
| 1002 baseline | Folk/1963/albumTrack/standard/legacyCategoryOnly | 360 |
| 1002 baseline | Folk/1963/albumTrack/original/legacyCategoryOnly | 1154 |
| 1002 baseline | Folk/1963/albumTrack/traditional/legacyCategoryOnly | 817 |
| 1002 baseline | Folk/1963/albumTrack/recentHit/legacyCategoryOnly | 215 |
| 1002 baseline | Jazz/1963/albumTrack/recentHit/legacyCategoryOnly | 99 |
| 1002 baseline | Jazz/1963/albumTrack/traditional/legacyCategoryOnly | 388 |
| 1002 baseline | Jazz/1963/albumTrack/standard/legacyCategoryOnly | 586 |
| 1002 baseline | Jazz/1963/albumTrack/original/legacyCategoryOnly | 745 |
| 1002 baseline | RnB/1963/single/traditional/legacyCategoryOnly | 50 |
| 1002 baseline | SurfRock/1963/albumTrack/recentHit/legacyCategoryOnly | 485 |
| 1002 baseline | SurfRock/1963/albumTrack/original/legacyCategoryOnly | 737 |
| 1002 baseline | TeenPop/1963/single/original/legacyCategoryOnly | 220 |
| 1002 baseline | Country/1963/albumTrack/standard/legacyCategoryOnly | 249 |
| 1002 baseline | Country/1963/albumTrack/original/legacyCategoryOnly | 2401 |
| 1002 baseline | Country/1963/albumTrack/traditional/legacyCategoryOnly | 407 |
| 1002 baseline | Country/1963/albumTrack/recentHit/legacyCategoryOnly | 357 |
| 1002 baseline | Jazz/1963/single/original/legacyCategoryOnly | 33 |
| 1002 baseline | TeenPop/1963/single/standard/legacyCategoryOnly | 19 |
| 1002 baseline | Classical/1963/single/standard/legacyCategoryOnly | 3 |
| 1002 baseline | EasyListening/1963/single/original/legacyCategoryOnly | 27 |
| 1002 baseline | Childrens/1963/albumTrack/traditional/legacyCategoryOnly | 326 |
| 1002 baseline | Childrens/1963/albumTrack/original/legacyCategoryOnly | 455 |
| 1002 baseline | Childrens/1963/albumTrack/recentHit/legacyCategoryOnly | 283 |
| 1002 baseline | TexMex/1963/single/traditional/legacyCategoryOnly | 10 |
| 1002 baseline | Classical/1963/albumTrack/recentHit/legacyCategoryOnly | 216 |
| 1002 baseline | Classical/1963/albumTrack/original/legacyCategoryOnly | 330 |
| 1002 baseline | Classical/1963/albumTrack/traditional/legacyCategoryOnly | 234 |
| 1002 baseline | DooWop/1963/single/traditional/legacyCategoryOnly | 29 |
| 1002 baseline | TexMex/1963/single/original/legacyCategoryOnly | 11 |
| 1002 baseline | Country/1963/single/traditional/legacyCategoryOnly | 42 |
| 1002 baseline | SurfRock/1963/single/original/legacyCategoryOnly | 32 |
| 1002 baseline | TeenPop/1963/single/recentHit/legacyCategoryOnly | 24 |
| 1002 baseline | LatinPop/1963/single/recentHit/legacyCategoryOnly | 8 |
| 1002 baseline | Childrens/1963/albumTrack/standard/legacyCategoryOnly | 167 |
| 1002 baseline | Jazz/1963/single/standard/legacyCategoryOnly | 29 |
| 1002 baseline | Classical/1963/albumTrack/standard/legacyCategoryOnly | 255 |
| 1002 baseline | Blues/1963/single/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | Country/1963/single/standard/legacyCategoryOnly | 25 |
| 1002 baseline | Soul/1963/single/recentHit/legacyCategoryOnly | 9 |
| 1002 baseline | DooWop/1963/single/recentHit/legacyCategoryOnly | 45 |
| 1002 baseline | DooWop/1963/single/original/legacyCategoryOnly | 70 |
| 1002 baseline | Folk/1963/single/original/legacyCategoryOnly | 11 |
| 1002 baseline | LatinPop/1963/single/standard/legacyCategoryOnly | 2 |
| 1002 baseline | Jazz/1963/single/recentHit/legacyCategoryOnly | 4 |
| 1002 baseline | LatinPop/1963/albumTrack/recentHit/legacyCategoryOnly | 203 |
| 1002 baseline | LatinPop/1963/albumTrack/traditional/legacyCategoryOnly | 245 |
| 1002 baseline | LatinPop/1963/albumTrack/original/legacyCategoryOnly | 364 |
| 1002 baseline | LatinPop/1963/albumTrack/standard/legacyCategoryOnly | 128 |
| 1002 baseline | Folk/1963/single/standard/legacyCategoryOnly | 4 |
| 1002 baseline | RockAndRoll/1963/albumTrack/traditional/legacyCategoryOnly | 9 |
| 1002 baseline | EasyListening/1963/single/traditional/legacyCategoryOnly | 12 |
| 1002 baseline | Comedy/1963/single/recentHit/legacyCategoryOnly | 7 |
| 1002 baseline | Folk/1963/single/recentHit/legacyCategoryOnly | 6 |
| 1002 baseline | TeenPop/1963/single/traditional/legacyCategoryOnly | 36 |
| 1002 baseline | LatinPop/1963/single/traditional/legacyCategoryOnly | 9 |
| 1002 baseline | Comedy/1963/single/traditional/legacyCategoryOnly | 10 |
| 1002 baseline | Blues/1963/single/original/legacyCategoryOnly | 6 |
| 1002 baseline | Folk/1963/single/traditional/legacyCategoryOnly | 14 |
| 1002 baseline | DooWop/1963/single/standard/legacyCategoryOnly | 6 |
| 1002 baseline | SurfRock/1963/albumTrack/standard/legacyCategoryOnly | 4 |
| 1002 baseline | RockAndRoll/1963/albumTrack/standard/legacyCategoryOnly | 10 |
| 1002 baseline | Classical/1963/single/recentHit/legacyCategoryOnly | 5 |
| 1002 baseline | Gospel/1963/single/traditional/legacyCategoryOnly | 27 |
| 1002 baseline | Childrens/1963/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | BossaNova/1963/albumTrack/traditional/legacyCategoryOnly | 141 |
| 1002 baseline | BossaNova/1963/albumTrack/standard/legacyCategoryOnly | 206 |
| 1002 baseline | BossaNova/1963/albumTrack/original/legacyCategoryOnly | 201 |
| 1002 baseline | BossaNova/1963/albumTrack/recentHit/legacyCategoryOnly | 36 |
| 1002 baseline | Soul/1963/single/traditional/legacyCategoryOnly | 11 |
| 1002 baseline | Soul/1963/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | Comedy/1963/single/original/legacyCategoryOnly | 11 |
| 1002 baseline | Gospel/1963/single/recentHit/legacyCategoryOnly | 6 |
| 1002 baseline | Jazz/1963/single/traditional/legacyCategoryOnly | 16 |
| 1002 baseline | TraditionalPop/1963/single/traditional/legacyCategoryOnly | 16 |
| 1002 baseline | EasyListening/1963/single/recentHit/legacyCategoryOnly | 3 |
| 1002 baseline | ContemporaryFolk/1963/single/recentHit/legacyCategoryOnly | 3 |
| 1002 baseline | Soul/1963/albumTrack/standard/legacyCategoryOnly | 29 |
| 1002 baseline | Country/1963/single/recentHit/legacyCategoryOnly | 22 |
| 1002 baseline | Gospel/1963/single/standard/legacyCategoryOnly | 12 |
| 1002 baseline | Classical/1963/single/original/legacyCategoryOnly | 2 |
| 1002 baseline | TexMex/1963/single/recentHit/legacyCategoryOnly | 8 |
| 1002 baseline | GarageRock/1963/single/original/legacyCategoryOnly | 5 |
| 1002 baseline | Blues/1963/single/recentHit/legacyCategoryOnly | 4 |
| 1002 baseline | TraditionalPop/1963/single/original/legacyCategoryOnly | 31 |
| 1002 baseline | SurfRock/1963/single/recentHit/legacyCategoryOnly | 16 |
| 1002 baseline | SurfRock/1963/albumTrack/traditional/legacyCategoryOnly | 12 |
| 1002 baseline | TexMex/1963/single/standard/legacyCategoryOnly | 4 |
| 1002 baseline | RockAndRoll/1963/single/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | ContemporaryFolk/1963/single/original/legacyCategoryOnly | 12 |
| 1002 baseline | LatinPop/1963/single/original/legacyCategoryOnly | 10 |
| 1002 baseline | Gospel/1963/single/original/legacyCategoryOnly | 8 |
| 1002 baseline | BossaNova/1963/single/standard/legacyCategoryOnly | 4 |
| 1002 baseline | ContemporaryFolk/1963/single/traditional/legacyCategoryOnly | 3 |
| 1002 baseline | Childrens/1963/single/original/legacyCategoryOnly | 3 |
| 1002 baseline | RnB/1963/albumTrack/standard/legacyCategoryOnly | 6 |
| 1002 baseline | Childrens/1963/single/recentHit/legacyCategoryOnly | 2 |
| 1002 baseline | Comedy/1963/single/standard/legacyCategoryOnly | 7 |
| 1002 baseline | GarageRock/1963/albumTrack/original/legacyCategoryOnly | 60 |
| 1002 baseline | GarageRock/1963/albumTrack/recentHit/legacyCategoryOnly | 39 |
| 1002 baseline | TraditionalPop/1963/single/recentHit/legacyCategoryOnly | 3 |
| 1002 baseline | BossaNova/1963/single/original/legacyCategoryOnly | 2 |
| 1002 baseline | GarageRock/1963/single/recentHit/legacyCategoryOnly | 1 |
| 1002 baseline | BossaNova/1963/single/traditional/legacyCategoryOnly | 1 |
| 1002 baseline | Classical/1963/single/traditional/legacyCategoryOnly | 2 |
| 1002 baseline | BritishBeat/1963/albumTrack/original/legacyCategoryOnly | 7 |
| 1002 baseline | BritishBeat/1963/albumTrack/recentHit/legacyCategoryOnly | 4 |
| 1002 baseline | BritishBeat/1963/single/recentHit/legacyCategoryOnly | 1 |
| 1002 baseline | Blues/1963/single/standard/legacyCategoryOnly | 1 |
| 1002 baseline | GarageRock/1963/albumTrack/standard/legacyCategoryOnly | 1 |

## Taxonomy authoring and actual use
Axes in the table are the existing six demand and four identity axes. Existing row IDs were preserved; new enum rows append to the vocabulary. JamExtendedWorkout stays unavailable before 1965 and ChamberBaroquePiece before 1964. Assignment rows are filtered by era at creation. Hard instrumental constraints are applied after persistent variation; unknown vocal metadata remains unknown and never silently becomes instrumental. A known vocal song can receive an explicit instrumental reading only for an act with known performers and no vocalist.

| New row | Demand / identity axes | Plasticity | First year | Families | Pace / mood |
| --- | --- | --- | --- | --- | --- |
| CountryShuffle | 0.45, 0.4, 0.5, 0.55, 0.4, 0.2, 0.45, 0.3, 0.75, 0.65 | 0.65 | 1900 | Country | shuffle / jaunty |
| CountryWaltz | 0.35, 0.55, 0.45, 0.45, 0.45, 0.25, 0.25, 0.45, 0.8, 0.65 | 0.65 | 1900 | Country | waltz / yearning |
| WesternSwing | 0.4, 0.45, 0.7, 0.75, 0.35, 0.3, 0.4, 0.55, 0.65, 0.6 | 0.55 | 1930 | Country | swing / celebratory |
| NashvilleBallad | 0.35, 0.65, 0.55, 0.7, 0.4, 0.6, 0.2, 0.65, 0.75, 0.7 | 0.6 | 1955 | Country | slow / yearning |
| BossaSong | 0.25, 0.6, 0.65, 0.6, 0.45, 0.35, 0.2, 0.75, 0.65, 0.6 | 0.5 | 1958 | Jazz, Latin | syncopated / intimate |
| ModernJazzInstrumental | 0, 0, 0.85, 0.75, 0, 0.35, 0.4, 0.85, 0.6, 0.6 | 0.4 | 1945 | Jazz | swing / exploratory |
| SurfInstrumental | 0, 0, 0.65, 0.7, 0, 0.35, 0.6, 0.35, 0.55, 0.35 | 0.45 | 1958 | Rock | fast / driving |
| QuietHymn | 0.3, 0.45, 0.35, 0.35, 0.4, 0.15, 0.15, 0.35, 0.95, 0.75 | 0.55 | 1900 | Gospel | slow / reverent |
| GospelQuartet | 0.55, 0.6, 0.45, 0.75, 0.45, 0.2, 0.3, 0.45, 0.9, 0.7 | 0.5 | 1900 | Gospel | measured / communal |
| GospelChoir | 0.65, 0.5, 0.55, 0.85, 0.4, 0.3, 0.3, 0.5, 0.9, 0.7 | 0.45 | 1900 | Gospel | dramatic-build / exultant |
| LatinBolero | 0.35, 0.65, 0.6, 0.6, 0.45, 0.3, 0.25, 0.65, 0.75, 0.65 | 0.55 | 1900 | Latin | slow / yearning |
| LatinDance | 0.45, 0.45, 0.65, 0.75, 0.35, 0.3, 0.4, 0.55, 0.65, 0.45 | 0.55 | 1900 | Latin, Caribbean | fast / celebratory |
| TexMexSong | 0.4, 0.5, 0.55, 0.65, 0.5, 0.25, 0.4, 0.4, 0.8, 0.6 | 0.6 | 1900 | Latin | two-beat / narrative |
| ClassicalOrchestral | 0, 0, 0.9, 0.95, 0, 0.5, 0.3, 0.95, 0.65, 0.8 | 0.2 | 1700 | Classical | measured / dramatic |
| ClassicalChamber | 0, 0, 0.85, 0.85, 0, 0.35, 0.2, 0.9, 0.75, 0.8 | 0.25 | 1700 | Classical | measured / reflective |
| ClassicalSolo | 0, 0, 0.9, 0.25, 0, 0.25, 0.2, 0.9, 0.75, 0.8 | 0.25 | 1700 | Classical | measured / reflective |

### Full family assignment table for review
Equal weight 1 means equal prior probability among year-eligible rows. Density/form/meter are explicit authored values, not fallback guesses.

| Family | Archetype | Weight | Lyric | Voice | Density | Form | Meter |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Tin Pan Alley | LushStandard | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Tin Pan Alley | SaloonBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Tin Pan Alley | CharmSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Tin Pan Alley | Swinger | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Tin Pan Alley | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Christmas Standard | QuietHymn | 1 | AdviceExhortation | Present | 0.4 | Strophic | FourFour |
| Christmas Standard | CharmSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Christmas Standard | SingAlongChant | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Christmas Standard | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Jazz Standard | Swinger | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Jazz Standard | SaloonBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Jazz Standard | CharmSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Jazz Standard | ModernJazzInstrumental | 1 | Instrumental | Instrumental | 0 | Aaba | FourFour |
| Jazz Standard | LushStandard | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Country Standard | CountryTwoBeat | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Country Standard | CountryShuffle | 1 | RomanticAddress | Present | 0.4 | Aaba | Shuffle |
| Country Standard | CountryWaltz | 1 | RomanticAddress | Present | 0.4 | Aaba | ThreeFour |
| Country Standard | WesternSwing | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Country Standard | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Country Standard | VerseDrivenSong | 1 | Narrative | Present | 0.65 | Strophic | FourFour |
| Country Standard | NashvilleBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Blues Standard | ShuffleTwelveBar | 1 | RomanticAddress | Present | 0.4 | TwelveBar | Shuffle |
| Blues Standard | SlowBlues | 1 | RomanticAddress | Present | 0.4 | TwelveBar | FourFour |
| Blues Standard | GrooveRiffVamp | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Gospel Standard | QuietHymn | 1 | AdviceExhortation | Present | 0.4 | Strophic | FourFour |
| Gospel Standard | GospelQuartet | 1 | AdviceExhortation | Present | 0.4 | Aaba | FourFour |
| Gospel Standard | GospelChoir | 1 | AdviceExhortation | Present | 0.4 | Aaba | FourFour |
| Gospel Standard | SpiritualShout | 1 | AdviceExhortation | Present | 0.4 | Aaba | FourFour |
| Folk Traditional | VerseDrivenSong | 1 | Narrative | Present | 0.65 | Strophic | FourFour |
| Folk Traditional | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Folk Traditional | ProtestMessageSong | 1 | TopicalTestimony | Present | 0.65 | Aaba | FourFour |
| R&B Catalog | ShuffleTwelveBar | 1 | RomanticAddress | Present | 0.4 | TwelveBar | Shuffle |
| R&B Catalog | SlowBlues | 1 | RomanticAddress | Present | 0.4 | TwelveBar | FourFour |
| R&B Catalog | GrooveRiffVamp | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| R&B Catalog | HornDrivenSoulNumber | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| R&B Catalog | DeepSoulPleader | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent RnR Hit | StomperRocker | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent RnR Hit | MidTempoRocker | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent RnR Hit | ShuffleTwelveBar | 1 | RomanticAddress | Present | 0.4 | TwelveBar | Shuffle |
| Recent RnR Hit | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent R&B Hit | HornDrivenSoulNumber | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent R&B Hit | DeepSoulPleader | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent R&B Hit | GrooveRiffVamp | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent R&B Hit | ShuffleTwelveBar | 1 | RomanticAddress | Present | 0.4 | TwelveBar | Shuffle |
| Recent Pop Hit | BrightPopNumber | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Pop Hit | MidTempoPopSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Pop Hit | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Pop Hit | CharmSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Teen Hit | BrightPopNumber | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Teen Hit | MidTempoPopSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Teen Hit | DanceNumber | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Teen Hit | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent DooWop Hit | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent DooWop Hit | SingAlongChant | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent DooWop Hit | MidTempoPopSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Country Hit | CountryTwoBeat | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Country Hit | CountryShuffle | 1 | RomanticAddress | Present | 0.4 | Aaba | Shuffle |
| Recent Country Hit | NashvilleBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Recent Country Hit | CountryWaltz | 1 | RomanticAddress | Present | 0.4 | Aaba | ThreeFour |
| Recent Country Hit | VerseDrivenSong | 1 | Narrative | Present | 0.65 | Strophic | FourFour |
| Latin songbook | LatinBolero | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Latin songbook | LatinDance | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| TexMex songbook | TexMexSong | 1 | Narrative | Present | 0.4 | Aaba | FourFour |
| TexMex songbook | LatinBolero | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Classical works | ClassicalOrchestral | 1 | Instrumental | Instrumental | 0 | ThroughComposed | FourFour |
| Classical works | ClassicalChamber | 1 | Instrumental | Instrumental | 0 | ThroughComposed | FourFour |
| Classical works | ClassicalSolo | 1 | Instrumental | Instrumental | 0 | ThroughComposed | FourFour |
| Comedy routines | Novelty | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Comedy routines | SpokenWord | 1 | McPatterFrame | Present | 0.4 | Aaba | FourFour |
| Comedy routines | JauntyMusicHallRomp | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Children songs | SingAlongChant | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Children songs | Novelty | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Children songs | CharmSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Brazilian songbook | LatinBolero | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Brazilian songbook | CharmSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Brazilian songbook | BossaSong | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Contemporary folk | VerseDrivenSong | 1 | Narrative | Present | 0.65 | Strophic | FourFour |
| Contemporary folk | ProtestMessageSong | 1 | TopicalTestimony | Present | 0.65 | Aaba | FourFour |
| Contemporary folk | SlowBallad | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |
| Surf adaptations | SurfInstrumental | 1 | Instrumental | Instrumental | 0 | Aaba | FourFour |
| Surf adaptations | GrooveRiffVamp | 1 | RomanticAddress | Present | 0.4 | Aaba | FourFour |

### Dominant performed archetypes
Shares below use cover slots. Demo and resolved live arrangement are separate: the former describes composition creation, the latter what the act proposes to perform. Full distributions are in the analysis JSON; generated inventory is grouped by genre, family, release year and archetype (`runs[].generated`). A diverse table alone is not evidence of diverse selected or performed music.

| Run | Population | Genre | Cover slots | Top demo row | Share | Top live arrangement | Share |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 baseline | unsigned | Classical | 18108 | ReverieMoodPiece | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Comedy | 21173 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | LatinPop | 3112 | DanceNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | EasyListening | 49329 | ReverieMoodPiece | 98.25% | baseline not captured | unobserved |
| 1001 baseline | unsigned | TexMex | 2368 | DanceNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Childrens | 15793 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | RockAndRoll | 57084 | MidTempoRocker | 64.46% | baseline not captured | unobserved |
| 1001 baseline | signed | TraditionalPop | 33836 | LushStandard | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | TeenPop | 39645 | BrightPopNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | RnB | 53286 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Blues | 14484 | SlowBlues | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Folk | 24576 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | DooWop | 18740 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Soul | 52754 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Country | 37363 | CountryTwoBeat | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Classical | 5987 | ReverieMoodPiece | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | EasyListening | 19178 | ReverieMoodPiece | 89.86% | baseline not captured | unobserved |
| 1001 baseline | signed | Jazz | 18208 | Swinger | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Gospel | 11168 | SpiritualShout | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Comedy | 6804 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | Childrens | 4290 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Folk | 43623 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Country | 79027 | CountryTwoBeat | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | RnB | 72880 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | RockAndRoll | 87688 | MidTempoRocker | 53.36% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Soul | 86395 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Gospel | 25371 | SpiritualShout | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | DooWop | 43214 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | TraditionalPop | 57592 | LushStandard | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | SurfRock | 16223 | HornDrivenSoulNumber | 56.85% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Jazz | 36986 | Swinger | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | TeenPop | 64326 | BrightPopNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | Blues | 31333 | SlowBlues | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | LatinPop | 1115 | DanceNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | TexMex | 987 | DanceNumber | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | SurfRock | 5251 | HornDrivenSoulNumber | 59.72% | baseline not captured | unobserved |
| 1001 baseline | unsigned | ContemporaryFolk | 9186 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | ContemporaryFolk | 3205 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | BossaNova | 5782 | Swinger | 100.00% | baseline not captured | unobserved |
| 1001 baseline | signed | BossaNova | 2580 | Swinger | 100.00% | baseline not captured | unobserved |
| 1001 baseline | unsigned | BritishBeat | 133 | HornDrivenSoulNumber | 62.41% | baseline not captured | unobserved |
| 1001 baseline | unsigned | GarageRock | 648 | HornDrivenSoulNumber | 56.02% | baseline not captured | unobserved |
| 1001 baseline | signed | GarageRock | 316 | HornDrivenSoulNumber | 54.11% | baseline not captured | unobserved |
| 1001 baseline | signed | BritishBeat | 16 | HornDrivenSoulNumber | 68.75% | baseline not captured | unobserved |
| 1002 baseline | unsigned | EasyListening | 49847 | ReverieMoodPiece | 98.04% | baseline not captured | unobserved |
| 1002 baseline | unsigned | LatinPop | 0 |  | unobserved | baseline not captured | unobserved |
| 1002 baseline | unsigned | Classical | 9617 | ReverieMoodPiece | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Childrens | 18569 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Comedy | 24530 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | TexMex | 0 |  | unobserved | baseline not captured | unobserved |
| 1002 baseline | signed | Jazz | 18499 | Swinger | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | RockAndRoll | 56140 | MidTempoRocker | 65.44% | baseline not captured | unobserved |
| 1002 baseline | signed | Country | 35405 | CountryTwoBeat | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | RnB | 52085 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | Blues | 11747 | SlowBlues | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | Soul | 48567 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | TraditionalPop | 36178 | LushStandard | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | TeenPop | 39632 | BrightPopNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | DooWop | 21158 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | Folk | 22503 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | EasyListening | 15973 | ReverieMoodPiece | 91.19% | baseline not captured | unobserved |
| 1002 baseline | signed | Gospel | 10084 | SpiritualShout | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | Classical | 2780 | ReverieMoodPiece | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | Comedy | 7248 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | TexMex | 0 |  | unobserved | baseline not captured | unobserved |
| 1002 baseline | signed | Childrens | 5234 | SpokenWord | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | TraditionalPop | 59249 | LushStandard | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | RnB | 74595 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Jazz | 39909 | Swinger | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Blues | 33536 | SlowBlues | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Country | 80681 | CountryTwoBeat | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | DooWop | 45519 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Soul | 84530 | HornDrivenSoulNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Folk | 42865 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | RockAndRoll | 86737 | MidTempoRocker | 54.91% | baseline not captured | unobserved |
| 1002 baseline | unsigned | Gospel | 23264 | SpiritualShout | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | TeenPop | 67785 | BrightPopNumber | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | SurfRock | 13980 | HornDrivenSoulNumber | 56.05% | baseline not captured | unobserved |
| 1002 baseline | signed | LatinPop | 0 |  | unobserved | baseline not captured | unobserved |
| 1002 baseline | signed | SurfRock | 4287 | HornDrivenSoulNumber | 57.03% | baseline not captured | unobserved |
| 1002 baseline | unsigned | ContemporaryFolk | 8808 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | ContemporaryFolk | 2722 | VerseDrivenSong | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | BossaNova | 5300 | Swinger | 100.00% | baseline not captured | unobserved |
| 1002 baseline | signed | BossaNova | 1406 | Swinger | 100.00% | baseline not captured | unobserved |
| 1002 baseline | unsigned | GarageRock | 793 | HornDrivenSoulNumber | 60.15% | baseline not captured | unobserved |
| 1002 baseline | signed | GarageRock | 186 | HornDrivenSoulNumber | 61.83% | baseline not captured | unobserved |
| 1002 baseline | signed | BritishBeat | 30 | HornDrivenSoulNumber | 56.67% | baseline not captured | unobserved |
| 1002 baseline | unsigned | BritishBeat | 5 | HornDrivenSoulNumber | 60.00% | baseline not captured | unobserved |

## Access, concentration and stability
The songbook is a persistent latent per-act membership rule, recomputed from the current eligible index using a saved seed. It is independent of requested cover count. It includes exact, secondary, related-family and declared cross-scene access; it does not store a stale materialized book. A four-song introductory book prevents an empty scene entry when eligible repertoire exists. The existing genre-evolution prior center and last-change year drive access and genre preference over twelve months. The source state records only a year, so January is an approximation of the transition date. Seasonal preference changes continuously on a four-year sinusoid; no per-booking shuffle is introduced.

The fit plateau remains; a 0.025 suitability grouping allows stable preference to choose among near-suitable candidates. Final ascending-ID decisions are zero by construction after phase 3; this does not mean fit plateaus disappeared. Plateau ties and concentration remain independent measurements. Monthly churn includes changes in set length and eligibility, so preference stability alone does not imply identical sets.

| Run | Population / genre | Mean eligible | Empty / sets | Short sets | ID tie share | Fit plateau share | Top-ten covers | Mean monthly Jaccard | Mean retained |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 baseline | unsigned/Classical | 2.19 | 51/8877 | 2207 | 98.01% | 98.01% | 100.00% | 0.92 | 0.96 |
| 1001 baseline | unsigned/Comedy | 3.59 | 172/8434 | 1509 | 82.22% | 94.20% | 100.00% | 0.80 | 0.91 |
| 1001 baseline | unsigned/LatinPop | 0.55 | 2504/5616 | 5051 | 0.00% | 0.00% | 100.00% | 1.00 | 1.00 |
| 1001 baseline | unsigned/EasyListening | 38.21 | 0/16224 | 0 | 45.80% | 46.10% | 96.14% | 0.69 | 0.83 |
| 1001 baseline | unsigned/TexMex | 0.64 | 1304/3672 | 3313 | 0.00% | 0.00% | 100.00% | 1.00 | 1.00 |
| 1001 baseline | unsigned/Childrens | 3.67 | 96/6193 | 998 | 82.21% | 94.73% | 100.00% | 0.80 | 0.91 |
| 1001 baseline | signed/RockAndRoll | 1340.16 | 0/19044 | 0 | 0.00% | 0.00% | 6.28% | 0.29 | 0.38 |
| 1001 baseline | signed/TraditionalPop | 1229.84 | 0/11607 | 0 | 88.69% | 88.69% | 52.24% | 0.63 | 0.77 |
| 1001 baseline | signed/TeenPop | 175.57 | 0/13148 | 0 | 96.95% | 96.95% | 55.47% | 0.67 | 0.81 |
| 1001 baseline | signed/RnB | 562.99 | 0/17964 | 0 | 96.91% | 96.91% | 60.01% | 0.64 | 0.78 |
| 1001 baseline | signed/Blues | 402.24 | 0/4847 | 0 | 93.21% | 93.21% | 68.04% | 0.63 | 0.77 |
| 1001 baseline | signed/Folk | 518.88 | 0/8403 | 0 | 95.46% | 95.46% | 85.51% | 0.69 | 0.82 |
| 1001 baseline | signed/DooWop | 123.35 | 0/6323 | 0 | 92.97% | 92.97% | 66.15% | 0.66 | 0.80 |
| 1001 baseline | signed/Soul | 49.87 | 0/17898 | 0 | 85.88% | 85.88% | 88.49% | 0.59 | 0.73 |
| 1001 baseline | signed/Country | 586.34 | 0/12491 | 0 | 99.98% | 99.98% | 93.12% | 0.73 | 0.86 |
| 1001 baseline | signed/Classical | 1.99 | 108/3261 | 887 | 90.95% | 90.95% | 100.00% | 0.90 | 0.96 |
| 1001 baseline | signed/EasyListening | 170.01 | 0/6562 | 0 | 60.00% | 61.71% | 86.47% | 0.66 | 0.79 |
| 1001 baseline | signed/Jazz | 506.51 | 0/6097 | 0 | 89.76% | 89.76% | 59.90% | 0.64 | 0.78 |
| 1001 baseline | signed/Gospel | 350.00 | 0/3569 | 0 | 94.32% | 94.32% | 89.97% | 0.70 | 0.84 |
| 1001 baseline | signed/Comedy | 4.00 | 96/2575 | 306 | 90.92% | 96.69% | 100.00% | 0.78 | 0.89 |
| 1001 baseline | signed/Childrens | 4.30 | 48/1590 | 132 | 93.73% | 97.48% | 100.00% | 0.76 | 0.88 |
| 1001 baseline | signed/TexMex | 0.75 | 325/1312 | 1089 | 0.00% | 0.00% | 100.00% | 1.00 | 1.00 |
| 1001 baseline | unsigned/Folk | 523.28 | 0/14380 | 0 | 94.07% | 94.07% | 86.60% | 0.68 | 0.82 |
| 1001 baseline | unsigned/Country | 587.87 | 0/25746 | 0 | 100.00% | 100.00% | 92.83% | 0.74 | 0.86 |
| 1001 baseline | unsigned/RnB | 565.48 | 0/24326 | 0 | 97.91% | 97.91% | 68.86% | 0.69 | 0.83 |
| 1001 baseline | unsigned/RockAndRoll | 1354.00 | 0/28379 | 0 | 0.00% | 0.00% | 6.82% | 0.31 | 0.41 |
| 1001 baseline | unsigned/Soul | 22.40 | 0/28435 | 0 | 94.01% | 94.01% | 96.26% | 0.64 | 0.78 |
| 1001 baseline | unsigned/Gospel | 350.00 | 0/8147 | 0 | 93.34% | 93.34% | 93.87% | 0.69 | 0.83 |
| 1001 baseline | unsigned/DooWop | 123.56 | 0/14190 | 0 | 95.22% | 95.22% | 69.57% | 0.67 | 0.81 |
| 1001 baseline | unsigned/TraditionalPop | 1238.13 | 0/18835 | 0 | 92.92% | 92.92% | 54.50% | 0.66 | 0.80 |
| 1001 baseline | unsigned/SurfRock | 1358.57 | 0/5254 | 0 | 0.00% | 0.00% | 6.23% | 0.30 | 0.40 |
| 1001 baseline | unsigned/Jazz | 507.39 | 0/12187 | 0 | 87.63% | 87.63% | 63.43% | 0.68 | 0.82 |
| 1001 baseline | unsigned/TeenPop | 178.44 | 0/21074 | 0 | 98.16% | 98.16% | 47.77% | 0.69 | 0.83 |
| 1001 baseline | unsigned/Blues | 403.01 | 0/10232 | 0 | 93.31% | 93.31% | 79.72% | 0.68 | 0.81 |
| 1001 baseline | signed/LatinPop | 0.71 | 456/1571 | 1334 | 0.00% | 0.00% | 100.00% | 1.00 | 1.00 |
| 1001 baseline | signed/SurfRock | 1366.21 | 0/1763 | 0 | 0.00% | 0.00% | 7.73% | 0.35 | 0.45 |
| 1001 baseline | unsigned/ContemporaryFolk | 527.50 | 0/2948 | 0 | 94.84% | 94.84% | 95.11% | 0.69 | 0.82 |
| 1001 baseline | signed/ContemporaryFolk | 532.15 | 0/1048 | 0 | 93.82% | 93.82% | 98.07% | 0.68 | 0.82 |
| 1001 baseline | unsigned/BossaNova | 508.87 | 0/1882 | 0 | 81.15% | 81.15% | 71.64% | 0.69 | 0.82 |
| 1001 baseline | signed/BossaNova | 509.06 | 0/856 | 0 | 81.36% | 81.36% | 74.69% | 0.69 | 0.83 |
| 1001 baseline | unsigned/BritishBeat | 1372.00 | 0/37 | 0 | 0.00% | 0.00% | 36.09% | 0.32 | 0.43 |
| 1001 baseline | unsigned/GarageRock | 1372.31 | 0/212 | 0 | 0.00% | 0.00% | 13.89% | 0.35 | 0.43 |
| 1001 baseline | signed/GarageRock | 1372.67 | 0/110 | 0 | 0.00% | 0.00% | 21.20% | 0.36 | 0.46 |
| 1001 baseline | signed/BritishBeat | 1374.50 | 0/4 | 0 | 0.00% | 0.00% | 100.00% | 0.48 | 0.57 |
| 1002 baseline | unsigned/EasyListening | 40.26 | 0/16127 | 0 | 61.59% | 61.79% | 90.59% | 0.66 | 0.80 |
| 1002 baseline | unsigned/LatinPop | 0.00 | 5492/5492 | 5492 | 0.00% | 0.00% | unobserved | unobserved | unobserved |
| 1002 baseline | unsigned/Classical | 1.17 | 5238/9272 | 5669 | 96.89% | 96.89% | 100.00% | 0.85 | 0.94 |
| 1002 baseline | unsigned/Childrens | 7.79 | 42/6529 | 341 | 90.52% | 99.67% | 100.00% | 0.75 | 0.88 |
| 1002 baseline | unsigned/Comedy | 5.99 | 40/8561 | 466 | 89.19% | 99.66% | 99.65% | 0.75 | 0.87 |
| 1002 baseline | unsigned/TexMex | 0.00 | 3843/3843 | 3843 | 0.00% | 0.00% | unobserved | unobserved | unobserved |
| 1002 baseline | signed/Jazz | 510.25 | 0/6520 | 0 | 93.17% | 93.17% | 45.47% | 0.61 | 0.75 |
| 1002 baseline | signed/RockAndRoll | 1333.14 | 0/18986 | 0 | 0.00% | 0.00% | 5.87% | 0.30 | 0.40 |
| 1002 baseline | signed/Country | 591.92 | 0/12070 | 0 | 99.74% | 99.74% | 91.08% | 0.72 | 0.85 |
| 1002 baseline | signed/RnB | 562.62 | 0/17802 | 0 | 92.31% | 92.31% | 61.91% | 0.63 | 0.77 |
| 1002 baseline | signed/Blues | 403.36 | 0/3830 | 0 | 89.73% | 89.73% | 70.38% | 0.67 | 0.81 |
| 1002 baseline | signed/Soul | 130.78 | 0/16359 | 0 | 74.71% | 75.52% | 90.11% | 0.63 | 0.77 |
| 1002 baseline | signed/TraditionalPop | 1232.10 | 0/12173 | 0 | 94.98% | 94.98% | 54.65% | 0.63 | 0.77 |
| 1002 baseline | signed/TeenPop | 178.66 | 0/13152 | 0 | 93.69% | 93.69% | 57.73% | 0.67 | 0.80 |
| 1002 baseline | signed/DooWop | 121.24 | 0/6925 | 0 | 91.23% | 91.23% | 66.96% | 0.67 | 0.81 |
| 1002 baseline | signed/Folk | 518.50 | 0/7595 | 0 | 80.43% | 80.43% | 82.76% | 0.68 | 0.82 |
| 1002 baseline | signed/EasyListening | 147.28 | 0/5409 | 0 | 64.60% | 65.37% | 82.22% | 0.62 | 0.76 |
| 1002 baseline | signed/Comedy | 6.53 | 72/2742 | 164 | 92.14% | 99.23% | 99.94% | 0.74 | 0.87 |
| 1002 baseline | signed/Gospel | 350.00 | 0/3351 | 0 | 95.66% | 95.66% | 93.10% | 0.69 | 0.84 |
| 1002 baseline | signed/Classical | 0.99 | 1999/3252 | 2156 | 94.82% | 94.82% | 100.00% | 0.82 | 0.93 |
| 1002 baseline | signed/TexMex | 0.00 | 1043/1043 | 1043 | 0.00% | 0.00% | unobserved | unobserved | unobserved |
| 1002 baseline | signed/Childrens | 7.96 | 40/1880 | 84 | 90.68% | 99.39% | 100.00% | 0.76 | 0.88 |
| 1002 baseline | unsigned/TraditionalPop | 1237.64 | 0/19193 | 0 | 97.25% | 97.25% | 59.61% | 0.67 | 0.80 |
| 1002 baseline | unsigned/RnB | 565.02 | 0/24316 | 0 | 96.33% | 96.33% | 61.67% | 0.67 | 0.81 |
| 1002 baseline | unsigned/Jazz | 511.98 | 0/13381 | 0 | 94.56% | 94.56% | 46.50% | 0.66 | 0.80 |
| 1002 baseline | unsigned/Blues | 403.86 | 0/10869 | 0 | 87.87% | 87.87% | 82.06% | 0.71 | 0.84 |
| 1002 baseline | unsigned/Country | 593.69 | 0/26393 | 0 | 99.96% | 99.96% | 90.16% | 0.73 | 0.86 |
| 1002 baseline | unsigned/DooWop | 121.50 | 0/14720 | 0 | 94.77% | 94.77% | 68.78% | 0.68 | 0.82 |
| 1002 baseline | unsigned/Soul | 14.50 | 0/27850 | 0 | 68.69% | 68.75% | 99.61% | 0.70 | 0.84 |
| 1002 baseline | unsigned/Folk | 522.67 | 0/14137 | 0 | 75.00% | 75.00% | 91.90% | 0.69 | 0.83 |
| 1002 baseline | unsigned/RockAndRoll | 1346.51 | 0/28184 | 0 | 0.00% | 0.00% | 6.80% | 0.32 | 0.42 |
| 1002 baseline | unsigned/Gospel | 350.00 | 0/7523 | 0 | 97.05% | 97.05% | 97.12% | 0.71 | 0.85 |
| 1002 baseline | unsigned/TeenPop | 182.16 | 0/22015 | 0 | 92.47% | 92.47% | 55.61% | 0.70 | 0.83 |
| 1002 baseline | unsigned/SurfRock | 1352.51 | 0/4608 | 0 | 0.00% | 0.00% | 6.64% | 0.31 | 0.40 |
| 1002 baseline | signed/LatinPop | 0.00 | 1391/1391 | 1391 | 0.00% | 0.00% | unobserved | unobserved | unobserved |
| 1002 baseline | signed/SurfRock | 1359.21 | 0/1456 | 0 | 0.00% | 0.00% | 7.72% | 0.37 | 0.47 |
| 1002 baseline | unsigned/ContemporaryFolk | 527.35 | 0/2898 | 0 | 81.35% | 81.39% | 93.68% | 0.69 | 0.83 |
| 1002 baseline | signed/ContemporaryFolk | 531.53 | 0/954 | 0 | 93.79% | 93.79% | 96.73% | 0.71 | 0.84 |
| 1002 baseline | unsigned/BossaNova | 515.15 | 0/1749 | 0 | 95.13% | 95.15% | 54.77% | 0.65 | 0.78 |
| 1002 baseline | signed/BossaNova | 515.78 | 0/498 | 0 | 96.44% | 96.44% | 63.44% | 0.64 | 0.79 |
| 1002 baseline | unsigned/GarageRock | 1365.98 | 0/265 | 0 | 0.00% | 0.00% | 12.23% | 0.39 | 0.49 |
| 1002 baseline | signed/GarageRock | 1366.64 | 0/64 | 0 | 0.00% | 0.00% | 24.73% | 0.35 | 0.45 |
| 1002 baseline | signed/BritishBeat | 1367.33 | 0/9 | 0 | 0.00% | 0.00% | 76.67% | 0.48 | 0.57 |
| 1002 baseline | unsigned/BritishBeat | 1366.00 | 0/2 | 0 | 0.00% | 0.00% | 100.00% | 0.67 | 0.67 |

## Original allocation choice
Considered: (1) set size × normalized songwriting ability × genre/cohort writing propensity, with exceptional fully original acts and supplied professional new material; (2) genre-specific thresholds retaining the universal 0/1/2 cap. The user approved option 1 and requested that all-original performances remain rare, with greater availability in Jazz. The implemented draw is persistent per act: 3% among exceptional writers, 12% among composer-led Jazz writers. Others keep at least one cover. A 5000-act regression observed 143 all-original Country acts and 603 Jazz acts; these are fixture rates, not historical measurements or four-year observed shares. Normalized writing strength is clamped between ability 0.15 and 0.85; set size remains 3–5.

Supplied professional new songs are eligible only when an actual unpublished catalogue composition is available; the selected song ID persists into the player commission/recording flow and retains professional credits. Original placeholders remain the established player live-set representation.

## Supply, admission routes and temporal status

| Scene | Family | Count | Origin-year span | Secondary | Traditional lineage |
| --- | --- | --- | --- | --- | --- |
| LatinPop | Latin songbook | 100 | 1920–1963 | TexMex | false |
| TexMex | TexMex songbook | 100 | 1900–1963 | LatinPop | false |
| Classical | Classical works | 120 | 1750–1959 | Classical | false |
| Comedy | Comedy routines | 60 | 1940–1963 | Comedy | false |
| Childrens | Children songs | 80 | 1900–1963 | Childrens | true |
| BossaNova | Brazilian songbook | 140 | 1930–1963 | LatinPop | false |
| ContemporaryFolk | Contemporary folk | 140 | 1958–1963 | Folk | false |
| SurfRock | Surf adaptations | 60 | 1958–1963 | RockAndRoll | false |

All 800 native entries use an independent composition-ID-keyed stream; adding them does not consume existing catalogue/global RNG draws. Titles and credits are synthetic placeholders, not claimed historical songs. Uniform origin-year draws are a provisional supply model; native first-release years are explicitly stamped to that synthetic year, rather than incorrectly treating the first simulated cover as the composition's first release. Established status requires age 15 (or an explicit as-of stamp), adequate durability/circulation and an indexed/released provenance; traditional lineage is separate. New contemporary songbooks cannot be standards before that date. Seeded books and native books are distinct admission traces.

| Run | Route | Admissions |
| --- | --- | --- |
| 1001 baseline | seedFamily | 3570 |
| 1001 baseline | seededRecentHit | 1000 |
| 1001 baseline | professionalCatalogue | 270 |
| 1001 baseline | chartPromoted | 1467 |
| 1002 baseline | seedFamily | 3570 |
| 1002 baseline | seededRecentHit | 1000 |
| 1002 baseline | professionalCatalogue | 270 |
| 1002 baseline | chartPromoted | 1440 |

Admissions are composition/route observations, not exclusive categories: one composition can enter by unpublished, album, locally familiar, supplied-professional and later chart-completion routes. New routes are recorded once per composition/route; legacy chart completion may appear more than once. Initial professional-catalogue admission is identified separately. Primary/secondary tags, origin, route year and record ID accompany every trace. Treatment streams separate recording master dates, first release year, and chart-completion years. Earlier baseline `recordingYears` actually contains completion years from SongRecordingMemory; it cannot reconstruct unobserved recording or first release dates. This is a declared baseline telemetry limit.

### Latin/TexMex and Classical baseline transitions
Retained diagnosis: seed 1001 enabled LatinPop/TexMex first becomes nonempty in April 1962 (one eligible song per act in December 1963); disabled first becomes nonempty in October 1960 (three in December 1963). Classical first becomes nonempty in March 1960 for seed 1001 enabled and July 1962 for seed 1002 enabled. The retained extraction does not identify every particular composition/completed run causing these transitions, so exact retained-world causal attribution stops at that evidence limit. These are observed transitions, not an isolated treatment effect.

The baseline primary-genre pools lack native seeded songbooks. Initially empty pools can gain material through completed top-40 runs; the new route traces identify those chartPromoted compositions. Existing cross-family recording defaults can create those songs before admission. Monthly empty-book counts and exact route/record IDs are in the analysis and CSVs. Admission trace year alone does not prove an exact month; the baseline did not retain first-release/master dates, so any finer attribution must use the linked chart event/master stream rather than inferred timing. Treatment adds native pools at startup and admissions independent of chart completion.

| Run | Population / genre / month | Mean eligible | Empty / sets |
| --- | --- | --- | --- |
| 1001 baseline | unsigned/Classical/1960-01 | 0.00 | 34/34 |
| 1001 baseline | unsigned/Classical/1960-63 | 2.19 | 51/8877 |
| 1001 baseline | unsigned/Comedy/1960-01 | 0.00 | 7/7 |
| 1001 baseline | unsigned/Comedy/1960-63 | 3.59 | 172/8434 |
| 1001 baseline | unsigned/LatinPop/1960-01 | 0.00 | 20/20 |
| 1001 baseline | unsigned/LatinPop/1960-63 | 0.55 | 2504/5616 |
| 1001 baseline | unsigned/TexMex/1960-01 | 0.00 | 4/4 |
| 1001 baseline | unsigned/TexMex/1960-63 | 0.64 | 1304/3672 |
| 1001 baseline | unsigned/Childrens/1960-01 | 0.00 | 6/6 |
| 1001 baseline | unsigned/Childrens/1960-63 | 3.67 | 96/6193 |
| 1001 baseline | signed/Classical/1960-01 | 0.00 | 41/41 |
| 1001 baseline | signed/Classical/1960-63 | 1.99 | 108/3261 |
| 1001 baseline | signed/Comedy/1960-01 | 0.00 | 14/14 |
| 1001 baseline | signed/Comedy/1960-63 | 4.00 | 96/2575 |
| 1001 baseline | signed/Childrens/1960-01 | 0.00 | 4/4 |
| 1001 baseline | signed/Childrens/1960-63 | 4.30 | 48/1590 |
| 1001 baseline | signed/TexMex/1960-01 | 0.00 | 1/1 |
| 1001 baseline | signed/TexMex/1960-63 | 0.75 | 325/1312 |
| 1001 baseline | unsigned/Comedy/1960-02 | 0.00 | 22/22 |
| 1001 baseline | unsigned/LatinPop/1960-02 | 0.00 | 10/10 |
| 1001 baseline | unsigned/Classical/1960-02 | 0.00 | 17/17 |
| 1001 baseline | unsigned/TexMex/1960-02 | 0.00 | 6/6 |
| 1001 baseline | unsigned/Childrens/1960-02 | 0.00 | 13/13 |
| 1001 baseline | unsigned/SurfRock/1960-02 | 1295.00 | 0/4 |
| 1001 baseline | unsigned/SurfRock/1960-63 | 1358.57 | 0/5254 |
| 1001 baseline | signed/Classical/1960-02 | 0.00 | 67/67 |
| 1001 baseline | signed/LatinPop/1960-02 | 0.00 | 15/15 |
| 1001 baseline | signed/LatinPop/1960-63 | 0.71 | 456/1571 |
| 1001 baseline | signed/Comedy/1960-02 | 0.00 | 20/20 |
| 1001 baseline | signed/TexMex/1960-02 | 0.00 | 5/5 |
| 1001 baseline | signed/Childrens/1960-02 | 0.00 | 11/11 |
| 1001 baseline | unsigned/Comedy/1960-03 | 0.00 | 38/38 |
| 1001 baseline | unsigned/LatinPop/1960-03 | 0.00 | 12/12 |
| 1001 baseline | unsigned/Classical/1960-03 | 1.00 | 0/19 |
| 1001 baseline | unsigned/TexMex/1960-03 | 0.00 | 9/9 |
| 1001 baseline | unsigned/Childrens/1960-03 | 0.00 | 18/18 |
| 1001 baseline | unsigned/SurfRock/1960-03 | 1298.00 | 0/10 |
| 1001 baseline | signed/Classical/1960-03 | 1.00 | 0/69 |
| 1001 baseline | signed/LatinPop/1960-03 | 0.00 | 17/17 |
| 1001 baseline | signed/Comedy/1960-03 | 0.00 | 20/20 |
| 1001 baseline | signed/TexMex/1960-03 | 0.00 | 7/7 |
| 1001 baseline | signed/Childrens/1960-03 | 0.00 | 11/11 |
| 1001 baseline | unsigned/Comedy/1960-04 | 0.00 | 46/46 |
| 1001 baseline | unsigned/LatinPop/1960-04 | 0.00 | 19/19 |
| 1001 baseline | unsigned/Classical/1960-04 | 1.00 | 0/21 |
| 1001 baseline | unsigned/TexMex/1960-04 | 0.00 | 11/11 |
| 1001 baseline | unsigned/Childrens/1960-04 | 0.00 | 27/27 |
| 1001 baseline | unsigned/SurfRock/1960-04 | 1304.00 | 0/15 |
| 1001 baseline | signed/Classical/1960-04 | 1.00 | 0/70 |
| 1001 baseline | signed/LatinPop/1960-04 | 0.00 | 18/18 |
| 1001 baseline | signed/Comedy/1960-04 | 0.00 | 21/21 |
| 1001 baseline | signed/TexMex/1960-04 | 0.00 | 7/7 |
| 1001 baseline | signed/Childrens/1960-04 | 0.00 | 11/11 |
| 1001 baseline | unsigned/Comedy/1960-05 | 0.00 | 59/59 |
| 1001 baseline | unsigned/LatinPop/1960-05 | 0.00 | 21/21 |
| 1001 baseline | unsigned/Classical/1960-05 | 1.00 | 0/31 |
| 1001 baseline | unsigned/TexMex/1960-05 | 0.00 | 13/13 |
| 1001 baseline | unsigned/Childrens/1960-05 | 0.00 | 32/32 |
| 1001 baseline | unsigned/SurfRock/1960-05 | 1310.00 | 0/20 |
| 1001 baseline | signed/Classical/1960-05 | 1.00 | 0/68 |
| 1001 baseline | signed/LatinPop/1960-05 | 0.00 | 18/18 |
| 1001 baseline | signed/Comedy/1960-05 | 0.00 | 21/21 |
| 1001 baseline | signed/TexMex/1960-05 | 0.00 | 7/7 |
| 1001 baseline | signed/Childrens/1960-05 | 0.00 | 11/11 |
| 1001 baseline | unsigned/Comedy/1960-06 | 1.00 | 0/69 |
| 1001 baseline | unsigned/LatinPop/1960-06 | 0.00 | 26/26 |
| 1001 baseline | unsigned/Classical/1960-06 | 1.00 | 0/44 |
| 1001 baseline | unsigned/TexMex/1960-06 | 0.00 | 19/19 |
| 1001 baseline | unsigned/Childrens/1960-06 | 1.00 | 0/41 |
| 1001 baseline | unsigned/SurfRock/1960-06 | 1312.00 | 0/24 |
| 1001 baseline | signed/Classical/1960-06 | 1.00 | 0/68 |
| 1001 baseline | signed/Comedy/1960-06 | 1.00 | 0/20 |
| 1001 baseline | signed/LatinPop/1960-06 | 0.00 | 16/16 |
| 1001 baseline | signed/TexMex/1960-06 | 0.00 | 8/8 |
| 1001 baseline | signed/Childrens/1960-06 | 1.00 | 0/11 |
| 1001 baseline | unsigned/Comedy/1960-07 | 1.00 | 0/80 |
| 1001 baseline | unsigned/LatinPop/1960-07 | 0.00 | 35/35 |
| 1001 baseline | unsigned/Classical/1960-07 | 1.00 | 0/46 |
| 1001 baseline | unsigned/TexMex/1960-07 | 0.00 | 21/21 |
| 1001 baseline | unsigned/Childrens/1960-07 | 1.00 | 0/54 |
| 1001 baseline | unsigned/SurfRock/1960-07 | 1316.00 | 0/24 |
| 1001 baseline | signed/Classical/1960-07 | 1.00 | 0/69 |
| 1001 baseline | signed/Comedy/1960-07 | 1.00 | 0/21 |
| 1001 baseline | signed/LatinPop/1960-07 | 0.00 | 15/15 |
| 1001 baseline | signed/TexMex/1960-07 | 0.00 | 8/8 |
| 1001 baseline | signed/Childrens/1960-07 | 1.00 | 0/10 |
| 1001 baseline | signed/SurfRock/1960-07 | 1316.00 | 0/2 |
| 1001 baseline | signed/SurfRock/1960-63 | 1366.21 | 0/1763 |
| 1001 baseline | unsigned/Comedy/1960-08 | 1.00 | 0/86 |
| 1001 baseline | unsigned/LatinPop/1960-08 | 0.00 | 42/42 |
| 1001 baseline | unsigned/Classical/1960-08 | 1.00 | 0/55 |
| 1001 baseline | unsigned/TexMex/1960-08 | 0.00 | 24/24 |
| 1001 baseline | unsigned/Childrens/1960-08 | 1.00 | 0/62 |
| 1001 baseline | unsigned/SurfRock/1960-08 | 1320.00 | 0/27 |
| 1001 baseline | signed/Classical/1960-08 | 1.00 | 0/68 |
| 1001 baseline | signed/Comedy/1960-08 | 1.00 | 0/21 |
| 1001 baseline | signed/LatinPop/1960-08 | 0.00 | 16/16 |
| 1001 baseline | signed/TexMex/1960-08 | 0.00 | 7/7 |
| 1001 baseline | signed/Childrens/1960-08 | 1.00 | 0/10 |
| 1001 baseline | signed/SurfRock/1960-08 | 1320.00 | 0/2 |
| 1001 baseline | unsigned/Classical/1960-09 | 1.00 | 0/67 |
| 1001 baseline | unsigned/Comedy/1960-09 | 1.00 | 0/98 |
| 1001 baseline | unsigned/LatinPop/1960-09 | 0.00 | 52/52 |
| 1001 baseline | unsigned/TexMex/1960-09 | 0.00 | 25/25 |
| 1001 baseline | unsigned/Childrens/1960-09 | 1.00 | 0/68 |
| 1001 baseline | unsigned/SurfRock/1960-09 | 1321.00 | 0/33 |
| 1001 baseline | signed/Classical/1960-09 | 1.00 | 0/66 |
| 1001 baseline | signed/Comedy/1960-09 | 1.00 | 0/20 |
| 1001 baseline | signed/LatinPop/1960-09 | 0.00 | 10/10 |
| 1001 baseline | signed/TexMex/1960-09 | 0.00 | 8/8 |
| 1001 baseline | signed/Childrens/1960-09 | 1.00 | 0/9 |
| 1001 baseline | signed/SurfRock/1960-09 | 1321.00 | 0/2 |
| 1001 baseline | unsigned/Classical/1960-10 | 1.00 | 0/78 |
| 1001 baseline | unsigned/Comedy/1960-10 | 1.00 | 0/112 |
| 1001 baseline | unsigned/LatinPop/1960-10 | 0.00 | 56/56 |
| 1001 baseline | unsigned/TexMex/1960-10 | 0.00 | 30/30 |
| 1001 baseline | unsigned/Childrens/1960-10 | 1.00 | 0/78 |
| 1001 baseline | unsigned/SurfRock/1960-10 | 1324.00 | 0/34 |
| 1001 baseline | signed/Classical/1960-10 | 1.00 | 0/64 |
| 1001 baseline | signed/Comedy/1960-10 | 1.00 | 0/20 |
| 1001 baseline | signed/LatinPop/1960-10 | 0.00 | 11/11 |
| 1001 baseline | signed/TexMex/1960-10 | 0.00 | 7/7 |
| 1001 baseline | signed/Childrens/1960-10 | 1.00 | 0/10 |
| 1001 baseline | signed/SurfRock/1960-10 | 1324.00 | 0/2 |
| 1001 baseline | unsigned/Classical/1960-11 | 2.00 | 0/82 |
| 1001 baseline | unsigned/Comedy/1960-11 | 1.00 | 0/128 |
| 1001 baseline | unsigned/LatinPop/1960-11 | 0.00 | 64/64 |
| 1001 baseline | unsigned/TexMex/1960-11 | 0.00 | 35/35 |
| 1001 baseline | unsigned/Childrens/1960-11 | 1.00 | 0/86 |
| 1001 baseline | unsigned/SurfRock/1960-11 | 1325.00 | 0/36 |
| 1001 baseline | signed/Classical/1960-11 | 2.00 | 0/62 |
| 1001 baseline | signed/Comedy/1960-11 | 1.00 | 0/20 |
| 1001 baseline | signed/LatinPop/1960-11 | 0.00 | 11/11 |
| 1001 baseline | signed/TexMex/1960-11 | 0.00 | 7/7 |
| 1001 baseline | signed/Childrens/1960-11 | 1.00 | 0/10 |
| 1001 baseline | signed/SurfRock/1960-11 | 1325.00 | 0/2 |
| 1001 baseline | unsigned/Classical/1960-12 | 2.00 | 0/93 |
| 1001 baseline | unsigned/Comedy/1960-12 | 1.00 | 0/136 |
| 1001 baseline | unsigned/LatinPop/1960-12 | 0.00 | 69/69 |
| 1001 baseline | unsigned/TexMex/1960-12 | 0.00 | 40/40 |
| 1001 baseline | unsigned/Childrens/1960-12 | 1.00 | 0/90 |
| 1001 baseline | unsigned/SurfRock/1960-12 | 1327.00 | 0/38 |
| 1001 baseline | signed/Classical/1960-12 | 2.00 | 0/58 |
| 1001 baseline | signed/LatinPop/1960-12 | 0.00 | 12/12 |
| 1001 baseline | signed/Comedy/1960-12 | 1.00 | 0/20 |
| 1001 baseline | signed/TexMex/1960-12 | 0.00 | 5/5 |
| 1001 baseline | signed/Childrens/1960-12 | 1.00 | 0/11 |
| 1001 baseline | signed/SurfRock/1960-12 | 1327.00 | 0/2 |
| 1001 baseline | unsigned/Classical/1961-01 | 2.00 | 0/225 |
| 1001 baseline | unsigned/Comedy/1961-01 | 1.00 | 0/167 |
| 1001 baseline | unsigned/LatinPop/1961-01 | 0.00 | 122/122 |
| 1001 baseline | unsigned/TexMex/1961-01 | 0.00 | 48/48 |
| 1001 baseline | unsigned/Childrens/1961-01 | 1.00 | 0/114 |
| 1001 baseline | unsigned/SurfRock/1961-01 | 1329.00 | 0/43 |
| 1001 baseline | signed/Classical/1961-01 | 2.00 | 0/53 |
| 1001 baseline | signed/LatinPop/1961-01 | 0.00 | 14/14 |
| 1001 baseline | signed/TexMex/1961-01 | 0.00 | 8/8 |
| 1001 baseline | signed/Comedy/1961-01 | 1.00 | 0/24 |
| 1001 baseline | signed/Childrens/1961-01 | 1.00 | 0/12 |
| 1001 baseline | signed/SurfRock/1961-01 | 1329.00 | 0/3 |
| 1001 baseline | unsigned/Classical/1961-02 | 2.00 | 0/224 |
| 1001 baseline | unsigned/LatinPop/1961-02 | 0.00 | 123/123 |
| 1001 baseline | unsigned/Comedy/1961-02 | 1.00 | 0/173 |
| 1001 baseline | unsigned/TexMex/1961-02 | 0.00 | 47/47 |
| 1001 baseline | unsigned/Childrens/1961-02 | 1.00 | 0/116 |
| 1001 baseline | unsigned/SurfRock/1961-02 | 1329.00 | 0/50 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-02 | 509.00 | 0/9 |
| 1001 baseline | unsigned/ContemporaryFolk/1960-63 | 527.50 | 0/2948 |
| 1001 baseline | signed/Classical/1961-02 | 2.00 | 0/58 |
| 1001 baseline | signed/Comedy/1961-02 | 1.00 | 0/29 |
| 1001 baseline | signed/LatinPop/1961-02 | 0.00 | 19/19 |
| 1001 baseline | signed/TexMex/1961-02 | 0.00 | 13/13 |
| 1001 baseline | signed/Childrens/1961-02 | 1.00 | 0/13 |
| 1001 baseline | signed/SurfRock/1961-02 | 1329.00 | 0/5 |
| 1001 baseline | unsigned/Classical/1961-03 | 2.00 | 0/233 |
| 1001 baseline | unsigned/LatinPop/1961-03 | 0.00 | 129/129 |
| 1001 baseline | unsigned/Comedy/1961-03 | 1.00 | 0/180 |
| 1001 baseline | unsigned/TexMex/1961-03 | 0.00 | 56/56 |
| 1001 baseline | unsigned/Childrens/1961-03 | 1.00 | 0/124 |
| 1001 baseline | unsigned/SurfRock/1961-03 | 1332.00 | 0/57 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-03 | 509.00 | 0/16 |
| 1001 baseline | signed/Classical/1961-03 | 2.00 | 0/55 |
| 1001 baseline | signed/Comedy/1961-03 | 1.00 | 0/30 |
| 1001 baseline | signed/TexMex/1961-03 | 0.00 | 13/13 |
| 1001 baseline | signed/LatinPop/1961-03 | 0.00 | 16/16 |
| 1001 baseline | signed/Childrens/1961-03 | 1.00 | 0/12 |
| 1001 baseline | signed/SurfRock/1961-03 | 1332.00 | 0/5 |
| 1001 baseline | unsigned/Classical/1961-04 | 2.00 | 0/242 |
| 1001 baseline | unsigned/LatinPop/1961-04 | 0.00 | 134/134 |
| 1001 baseline | unsigned/Comedy/1961-04 | 2.00 | 0/194 |
| 1001 baseline | unsigned/TexMex/1961-04 | 0.00 | 58/58 |
| 1001 baseline | unsigned/Childrens/1961-04 | 2.00 | 0/132 |
| 1001 baseline | unsigned/SurfRock/1961-04 | 1336.00 | 0/69 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-04 | 511.00 | 0/21 |
| 1001 baseline | signed/Classical/1961-04 | 2.00 | 0/55 |
| 1001 baseline | signed/Comedy/1961-04 | 2.00 | 0/31 |
| 1001 baseline | signed/TexMex/1961-04 | 0.00 | 14/14 |
| 1001 baseline | signed/LatinPop/1961-04 | 0.00 | 17/17 |
| 1001 baseline | signed/Childrens/1961-04 | 2.00 | 0/12 |
| 1001 baseline | signed/SurfRock/1961-04 | 1336.00 | 0/5 |
| 1001 baseline | signed/ContemporaryFolk/1961-04 | 511.00 | 0/1 |
| 1001 baseline | signed/ContemporaryFolk/1960-63 | 532.15 | 0/1048 |
| 1001 baseline | unsigned/Classical/1961-05 | 2.00 | 0/248 |
| 1001 baseline | unsigned/LatinPop/1961-05 | 0.00 | 134/134 |
| 1001 baseline | unsigned/Comedy/1961-05 | 2.00 | 0/200 |
| 1001 baseline | unsigned/TexMex/1961-05 | 0.00 | 64/64 |
| 1001 baseline | unsigned/Childrens/1961-05 | 2.00 | 0/141 |
| 1001 baseline | unsigned/SurfRock/1961-05 | 1336.00 | 0/72 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-05 | 512.00 | 0/31 |
| 1001 baseline | signed/Classical/1961-05 | 2.00 | 0/56 |
| 1001 baseline | signed/Comedy/1961-05 | 2.00 | 0/30 |
| 1001 baseline | signed/TexMex/1961-05 | 0.00 | 14/14 |
| 1001 baseline | signed/LatinPop/1961-05 | 0.00 | 18/18 |
| 1001 baseline | signed/Childrens/1961-05 | 2.00 | 0/11 |
| 1001 baseline | signed/SurfRock/1961-05 | 1336.00 | 0/5 |
| 1001 baseline | signed/ContemporaryFolk/1961-05 | 512.00 | 0/1 |
| 1001 baseline | unsigned/Classical/1961-06 | 2.00 | 0/253 |
| 1001 baseline | unsigned/LatinPop/1961-06 | 0.00 | 141/141 |
| 1001 baseline | unsigned/Comedy/1961-06 | 2.00 | 0/211 |
| 1001 baseline | unsigned/TexMex/1961-06 | 0.00 | 70/70 |
| 1001 baseline | unsigned/Childrens/1961-06 | 2.00 | 0/145 |
| 1001 baseline | unsigned/SurfRock/1961-06 | 1339.00 | 0/74 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-06 | 513.00 | 0/39 |
| 1001 baseline | signed/Classical/1961-06 | 2.00 | 0/58 |
| 1001 baseline | signed/Comedy/1961-06 | 2.00 | 0/30 |
| 1001 baseline | signed/TexMex/1961-06 | 0.00 | 14/14 |
| 1001 baseline | signed/LatinPop/1961-06 | 0.00 | 17/17 |
| 1001 baseline | signed/Childrens/1961-06 | 2.00 | 0/11 |
| 1001 baseline | signed/SurfRock/1961-06 | 1339.00 | 0/6 |
| 1001 baseline | signed/ContemporaryFolk/1961-06 | 513.00 | 0/1 |
| 1001 baseline | unsigned/Classical/1961-07 | 2.00 | 0/246 |
| 1001 baseline | unsigned/LatinPop/1961-07 | 0.00 | 141/141 |
| 1001 baseline | unsigned/Comedy/1961-07 | 2.00 | 0/211 |
| 1001 baseline | unsigned/TexMex/1961-07 | 0.00 | 69/69 |
| 1001 baseline | unsigned/Childrens/1961-07 | 2.00 | 0/154 |
| 1001 baseline | unsigned/SurfRock/1961-07 | 1342.00 | 0/88 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-07 | 514.00 | 0/47 |
| 1001 baseline | signed/Classical/1961-07 | 2.00 | 0/61 |
| 1001 baseline | signed/Comedy/1961-07 | 2.00 | 0/32 |
| 1001 baseline | signed/TexMex/1961-07 | 0.00 | 16/16 |
| 1001 baseline | signed/LatinPop/1961-07 | 0.00 | 17/17 |
| 1001 baseline | signed/Childrens/1961-07 | 2.00 | 0/11 |
| 1001 baseline | signed/SurfRock/1961-07 | 1342.00 | 0/6 |
| 1001 baseline | signed/ContemporaryFolk/1961-07 | 514.00 | 0/2 |
| 1001 baseline | unsigned/Classical/1961-08 | 2.00 | 0/246 |
| 1001 baseline | unsigned/LatinPop/1961-08 | 0.00 | 142/142 |
| 1001 baseline | unsigned/Comedy/1961-08 | 2.00 | 0/203 |
| 1001 baseline | unsigned/TexMex/1961-08 | 0.00 | 73/73 |
| 1001 baseline | unsigned/Childrens/1961-08 | 2.00 | 0/147 |
| 1001 baseline | unsigned/SurfRock/1961-08 | 1343.00 | 0/94 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-08 | 514.00 | 0/50 |
| 1001 baseline | signed/Classical/1961-08 | 2.00 | 0/62 |
| 1001 baseline | signed/Comedy/1961-08 | 2.00 | 0/32 |
| 1001 baseline | signed/TexMex/1961-08 | 0.00 | 17/17 |
| 1001 baseline | signed/LatinPop/1961-08 | 0.00 | 18/18 |
| 1001 baseline | signed/Childrens/1961-08 | 2.00 | 0/12 |
| 1001 baseline | signed/SurfRock/1961-08 | 1343.00 | 0/6 |
| 1001 baseline | signed/ContemporaryFolk/1961-08 | 514.00 | 0/2 |
| 1001 baseline | unsigned/Classical/1961-09 | 2.00 | 0/247 |
| 1001 baseline | unsigned/LatinPop/1961-09 | 0.00 | 144/144 |
| 1001 baseline | unsigned/Comedy/1961-09 | 3.00 | 0/201 |
| 1001 baseline | unsigned/TexMex/1961-09 | 0.00 | 72/72 |
| 1001 baseline | unsigned/Childrens/1961-09 | 3.00 | 0/146 |
| 1001 baseline | unsigned/SurfRock/1961-09 | 1345.00 | 0/93 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-09 | 515.00 | 0/54 |
| 1001 baseline | signed/Classical/1961-09 | 2.00 | 0/61 |
| 1001 baseline | signed/Comedy/1961-09 | 3.00 | 0/31 |
| 1001 baseline | signed/TexMex/1961-09 | 0.00 | 18/18 |
| 1001 baseline | signed/LatinPop/1961-09 | 0.00 | 18/18 |
| 1001 baseline | signed/Childrens/1961-09 | 3.00 | 0/13 |
| 1001 baseline | signed/SurfRock/1961-09 | 1345.00 | 0/10 |
| 1001 baseline | signed/ContemporaryFolk/1961-09 | 515.00 | 0/3 |
| 1001 baseline | unsigned/Classical/1961-10 | 2.00 | 0/249 |
| 1001 baseline | unsigned/LatinPop/1961-10 | 0.00 | 148/148 |
| 1001 baseline | unsigned/Comedy/1961-10 | 3.00 | 0/199 |
| 1001 baseline | unsigned/TexMex/1961-10 | 0.00 | 81/81 |
| 1001 baseline | unsigned/Childrens/1961-10 | 3.00 | 0/146 |
| 1001 baseline | unsigned/SurfRock/1961-10 | 1346.00 | 0/93 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-10 | 517.00 | 0/56 |
| 1001 baseline | signed/Classical/1961-10 | 2.00 | 0/63 |
| 1001 baseline | signed/Comedy/1961-10 | 3.00 | 0/35 |
| 1001 baseline | signed/TexMex/1961-10 | 0.00 | 17/17 |
| 1001 baseline | signed/LatinPop/1961-10 | 0.00 | 20/20 |
| 1001 baseline | signed/Childrens/1961-10 | 3.00 | 0/14 |
| 1001 baseline | signed/SurfRock/1961-10 | 1346.00 | 0/12 |
| 1001 baseline | signed/ContemporaryFolk/1961-10 | 517.00 | 0/5 |
| 1001 baseline | unsigned/Classical/1961-11 | 2.00 | 0/256 |
| 1001 baseline | unsigned/LatinPop/1961-11 | 0.00 | 147/147 |
| 1001 baseline | unsigned/Comedy/1961-11 | 3.00 | 0/199 |
| 1001 baseline | unsigned/TexMex/1961-11 | 0.00 | 83/83 |
| 1001 baseline | unsigned/Childrens/1961-11 | 3.00 | 0/148 |
| 1001 baseline | unsigned/SurfRock/1961-11 | 1346.00 | 0/98 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-11 | 518.00 | 0/61 |
| 1001 baseline | signed/Classical/1961-11 | 2.00 | 0/58 |
| 1001 baseline | signed/Comedy/1961-11 | 3.00 | 0/36 |
| 1001 baseline | signed/TexMex/1961-11 | 0.00 | 19/19 |
| 1001 baseline | signed/LatinPop/1961-11 | 0.00 | 22/22 |
| 1001 baseline | signed/Childrens/1961-11 | 3.00 | 0/13 |
| 1001 baseline | signed/SurfRock/1961-11 | 1346.00 | 0/14 |
| 1001 baseline | signed/ContemporaryFolk/1961-11 | 518.00 | 0/5 |
| 1001 baseline | unsigned/Classical/1961-12 | 2.00 | 0/249 |
| 1001 baseline | unsigned/LatinPop/1961-12 | 0.00 | 147/147 |
| 1001 baseline | unsigned/Comedy/1961-12 | 3.00 | 0/199 |
| 1001 baseline | unsigned/TexMex/1961-12 | 0.00 | 84/84 |
| 1001 baseline | unsigned/Childrens/1961-12 | 3.00 | 0/151 |
| 1001 baseline | unsigned/SurfRock/1961-12 | 1347.00 | 0/102 |
| 1001 baseline | unsigned/ContemporaryFolk/1961-12 | 518.00 | 0/65 |
| 1001 baseline | signed/Classical/1961-12 | 2.00 | 0/59 |
| 1001 baseline | signed/Comedy/1961-12 | 3.00 | 0/36 |
| 1001 baseline | signed/TexMex/1961-12 | 0.00 | 19/19 |
| 1001 baseline | signed/LatinPop/1961-12 | 0.00 | 22/22 |
| 1001 baseline | signed/Childrens/1961-12 | 3.00 | 0/12 |
| 1001 baseline | signed/SurfRock/1961-12 | 1347.00 | 0/17 |
| 1001 baseline | signed/ContemporaryFolk/1961-12 | 518.00 | 0/5 |
| 1001 baseline | unsigned/Classical/1962-01 | 2.00 | 0/256 |
| 1001 baseline | unsigned/LatinPop/1962-01 | 0.00 | 141/141 |
| 1001 baseline | unsigned/Comedy/1962-01 | 4.00 | 0/190 |
| 1001 baseline | unsigned/TexMex/1962-01 | 0.00 | 87/87 |
| 1001 baseline | unsigned/Childrens/1962-01 | 4.00 | 0/147 |
| 1001 baseline | unsigned/SurfRock/1962-01 | 1348.00 | 0/108 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-01 | 519.00 | 0/70 |
| 1001 baseline | signed/Classical/1962-01 | 2.00 | 0/58 |
| 1001 baseline | signed/Comedy/1962-01 | 4.00 | 0/46 |
| 1001 baseline | signed/TexMex/1962-01 | 0.00 | 20/20 |
| 1001 baseline | signed/LatinPop/1962-01 | 0.00 | 25/25 |
| 1001 baseline | signed/Childrens/1962-01 | 4.00 | 0/10 |
| 1001 baseline | signed/SurfRock/1962-01 | 1348.00 | 0/19 |
| 1001 baseline | signed/ContemporaryFolk/1962-01 | 519.00 | 0/9 |
| 1001 baseline | unsigned/Classical/1962-02 | 2.00 | 0/255 |
| 1001 baseline | unsigned/Comedy/1962-02 | 4.00 | 0/188 |
| 1001 baseline | unsigned/LatinPop/1962-02 | 0.00 | 142/142 |
| 1001 baseline | unsigned/TexMex/1962-02 | 0.00 | 87/87 |
| 1001 baseline | unsigned/Childrens/1962-02 | 4.00 | 0/143 |
| 1001 baseline | unsigned/SurfRock/1962-02 | 1349.00 | 0/112 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-02 | 520.00 | 0/77 |
| 1001 baseline | unsigned/BossaNova/1962-02 | 508.00 | 0/7 |
| 1001 baseline | unsigned/BossaNova/1960-63 | 508.87 | 0/1882 |
| 1001 baseline | signed/Classical/1962-02 | 2.00 | 0/57 |
| 1001 baseline | signed/Comedy/1962-02 | 4.00 | 0/50 |
| 1001 baseline | signed/TexMex/1962-02 | 0.00 | 23/23 |
| 1001 baseline | signed/LatinPop/1962-02 | 0.00 | 27/27 |
| 1001 baseline | signed/Childrens/1962-02 | 4.00 | 0/16 |
| 1001 baseline | signed/SurfRock/1962-02 | 1349.00 | 0/24 |
| 1001 baseline | signed/ContemporaryFolk/1962-02 | 520.00 | 0/9 |
| 1001 baseline | unsigned/Classical/1962-03 | 2.00 | 0/249 |
| 1001 baseline | unsigned/Comedy/1962-03 | 4.00 | 0/180 |
| 1001 baseline | unsigned/LatinPop/1962-03 | 0.00 | 143/143 |
| 1001 baseline | unsigned/TexMex/1962-03 | 0.00 | 88/88 |
| 1001 baseline | unsigned/Childrens/1962-03 | 4.00 | 0/143 |
| 1001 baseline | unsigned/SurfRock/1962-03 | 1350.00 | 0/117 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-03 | 520.00 | 0/79 |
| 1001 baseline | unsigned/BossaNova/1962-03 | 508.00 | 0/15 |
| 1001 baseline | signed/Classical/1962-03 | 2.00 | 0/60 |
| 1001 baseline | signed/Comedy/1962-03 | 4.00 | 0/55 |
| 1001 baseline | signed/TexMex/1962-03 | 0.00 | 23/23 |
| 1001 baseline | signed/LatinPop/1962-03 | 0.00 | 27/27 |
| 1001 baseline | signed/Childrens/1962-03 | 4.00 | 0/19 |
| 1001 baseline | signed/SurfRock/1962-03 | 1350.00 | 0/24 |
| 1001 baseline | signed/ContemporaryFolk/1962-03 | 520.00 | 0/12 |
| 1001 baseline | unsigned/Classical/1962-04 | 2.00 | 0/243 |
| 1001 baseline | unsigned/Comedy/1962-04 | 4.00 | 0/179 |
| 1001 baseline | unsigned/LatinPop/1962-04 | 1.00 | 0/142 |
| 1001 baseline | unsigned/TexMex/1962-04 | 1.00 | 0/90 |
| 1001 baseline | unsigned/Childrens/1962-04 | 4.00 | 0/139 |
| 1001 baseline | unsigned/SurfRock/1962-04 | 1353.00 | 0/124 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-04 | 520.00 | 0/84 |
| 1001 baseline | unsigned/BossaNova/1962-04 | 508.00 | 0/32 |
| 1001 baseline | signed/Classical/1962-04 | 2.00 | 0/66 |
| 1001 baseline | signed/Comedy/1962-04 | 4.00 | 0/59 |
| 1001 baseline | signed/TexMex/1962-04 | 1.00 | 0/26 |
| 1001 baseline | signed/LatinPop/1962-04 | 1.00 | 0/30 |
| 1001 baseline | signed/Childrens/1962-04 | 4.00 | 0/23 |
| 1001 baseline | signed/SurfRock/1962-04 | 1353.00 | 0/25 |
| 1001 baseline | signed/ContemporaryFolk/1962-04 | 520.00 | 0/15 |
| 1001 baseline | unsigned/Classical/1962-05 | 2.00 | 0/244 |
| 1001 baseline | unsigned/Comedy/1962-05 | 4.00 | 0/176 |
| 1001 baseline | unsigned/LatinPop/1962-05 | 1.00 | 0/138 |
| 1001 baseline | unsigned/TexMex/1962-05 | 1.00 | 0/92 |
| 1001 baseline | unsigned/Childrens/1962-05 | 4.00 | 0/137 |
| 1001 baseline | unsigned/SurfRock/1962-05 | 1355.00 | 0/134 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-05 | 521.00 | 0/95 |
| 1001 baseline | unsigned/BossaNova/1962-05 | 508.00 | 0/42 |
| 1001 baseline | signed/Classical/1962-05 | 2.00 | 0/66 |
| 1001 baseline | signed/Comedy/1962-05 | 4.00 | 0/59 |
| 1001 baseline | signed/TexMex/1962-05 | 1.00 | 0/29 |
| 1001 baseline | signed/LatinPop/1962-05 | 1.00 | 0/32 |
| 1001 baseline | signed/Childrens/1962-05 | 4.00 | 0/25 |
| 1001 baseline | signed/SurfRock/1962-05 | 1355.00 | 0/27 |
| 1001 baseline | signed/ContemporaryFolk/1962-05 | 521.00 | 0/15 |
| 1001 baseline | signed/BossaNova/1962-05 | 508.00 | 0/1 |
| 1001 baseline | signed/BossaNova/1960-63 | 509.06 | 0/856 |
| 1001 baseline | unsigned/Classical/1962-06 | 2.00 | 0/245 |
| 1001 baseline | unsigned/Comedy/1962-06 | 4.00 | 0/182 |
| 1001 baseline | unsigned/LatinPop/1962-06 | 1.00 | 0/139 |
| 1001 baseline | unsigned/Childrens/1962-06 | 4.00 | 0/142 |
| 1001 baseline | unsigned/TexMex/1962-06 | 1.00 | 0/89 |
| 1001 baseline | unsigned/SurfRock/1962-06 | 1357.00 | 0/144 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-06 | 522.00 | 0/105 |
| 1001 baseline | unsigned/BossaNova/1962-06 | 508.00 | 0/52 |
| 1001 baseline | signed/Classical/1962-06 | 2.00 | 0/62 |
| 1001 baseline | signed/TexMex/1962-06 | 1.00 | 0/31 |
| 1001 baseline | signed/Comedy/1962-06 | 4.00 | 0/56 |
| 1001 baseline | signed/LatinPop/1962-06 | 1.00 | 0/30 |
| 1001 baseline | signed/Childrens/1962-06 | 4.00 | 0/26 |
| 1001 baseline | signed/SurfRock/1962-06 | 1357.00 | 0/28 |
| 1001 baseline | signed/ContemporaryFolk/1962-06 | 522.00 | 0/16 |
| 1001 baseline | signed/BossaNova/1962-06 | 508.00 | 0/1 |
| 1001 baseline | unsigned/Classical/1962-07 | 2.00 | 0/156 |
| 1001 baseline | unsigned/Comedy/1962-07 | 4.00 | 0/162 |
| 1001 baseline | unsigned/LatinPop/1962-07 | 1.00 | 0/109 |
| 1001 baseline | unsigned/TexMex/1962-07 | 1.00 | 0/85 |
| 1001 baseline | unsigned/Childrens/1962-07 | 4.00 | 0/122 |
| 1001 baseline | unsigned/SurfRock/1962-07 | 1359.00 | 0/150 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-07 | 523.00 | 0/106 |
| 1001 baseline | unsigned/BossaNova/1962-07 | 508.00 | 0/58 |
| 1001 baseline | signed/Classical/1962-07 | 2.00 | 0/62 |
| 1001 baseline | signed/TexMex/1962-07 | 1.00 | 0/36 |
| 1001 baseline | signed/Comedy/1962-07 | 4.00 | 0/61 |
| 1001 baseline | signed/LatinPop/1962-07 | 1.00 | 0/30 |
| 1001 baseline | signed/Childrens/1962-07 | 4.00 | 0/27 |
| 1001 baseline | signed/SurfRock/1962-07 | 1359.00 | 0/33 |
| 1001 baseline | signed/ContemporaryFolk/1962-07 | 523.00 | 0/24 |
| 1001 baseline | signed/BossaNova/1962-07 | 508.00 | 0/5 |
| 1001 baseline | unsigned/Classical/1962-08 | 2.00 | 0/161 |
| 1001 baseline | unsigned/Comedy/1962-08 | 4.00 | 0/173 |
| 1001 baseline | unsigned/LatinPop/1962-08 | 1.00 | 0/116 |
| 1001 baseline | unsigned/TexMex/1962-08 | 1.00 | 0/94 |
| 1001 baseline | unsigned/Childrens/1962-08 | 4.00 | 0/137 |
| 1001 baseline | unsigned/SurfRock/1962-08 | 1360.00 | 0/155 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-08 | 524.00 | 0/105 |
| 1001 baseline | unsigned/BossaNova/1962-08 | 508.00 | 0/63 |
| 1001 baseline | signed/Classical/1962-08 | 2.00 | 0/60 |
| 1001 baseline | signed/TexMex/1962-08 | 1.00 | 0/35 |
| 1001 baseline | signed/Comedy/1962-08 | 4.00 | 0/64 |
| 1001 baseline | signed/LatinPop/1962-08 | 1.00 | 0/32 |
| 1001 baseline | signed/Childrens/1962-08 | 4.00 | 0/27 |
| 1001 baseline | signed/SurfRock/1962-08 | 1360.00 | 0/34 |
| 1001 baseline | signed/ContemporaryFolk/1962-08 | 524.00 | 0/26 |
| 1001 baseline | signed/BossaNova/1962-08 | 508.00 | 0/10 |
| 1001 baseline | unsigned/Classical/1962-09 | 2.00 | 0/152 |
| 1001 baseline | unsigned/Comedy/1962-09 | 5.00 | 0/175 |
| 1001 baseline | unsigned/LatinPop/1962-09 | 1.00 | 0/121 |
| 1001 baseline | unsigned/TexMex/1962-09 | 1.00 | 0/93 |
| 1001 baseline | unsigned/Childrens/1962-09 | 5.00 | 0/140 |
| 1001 baseline | unsigned/SurfRock/1962-09 | 1361.00 | 0/162 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-09 | 525.00 | 0/109 |
| 1001 baseline | unsigned/BossaNova/1962-09 | 508.00 | 0/62 |
| 1001 baseline | signed/Classical/1962-09 | 2.00 | 0/63 |
| 1001 baseline | signed/TexMex/1962-09 | 1.00 | 0/42 |
| 1001 baseline | signed/Comedy/1962-09 | 5.00 | 0/73 |
| 1001 baseline | signed/LatinPop/1962-09 | 1.00 | 0/40 |
| 1001 baseline | signed/Childrens/1962-09 | 5.00 | 0/38 |
| 1001 baseline | signed/SurfRock/1962-09 | 1361.00 | 0/42 |
| 1001 baseline | signed/ContemporaryFolk/1962-09 | 525.00 | 0/29 |
| 1001 baseline | signed/BossaNova/1962-09 | 508.00 | 0/20 |
| 1001 baseline | unsigned/Classical/1962-10 | 2.00 | 0/161 |
| 1001 baseline | unsigned/Comedy/1962-10 | 5.00 | 0/188 |
| 1001 baseline | unsigned/LatinPop/1962-10 | 1.00 | 0/123 |
| 1001 baseline | unsigned/TexMex/1962-10 | 1.00 | 0/98 |
| 1001 baseline | unsigned/Childrens/1962-10 | 5.00 | 0/137 |
| 1001 baseline | unsigned/SurfRock/1962-10 | 1362.00 | 0/168 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-10 | 525.00 | 0/107 |
| 1001 baseline | unsigned/BossaNova/1962-10 | 508.00 | 0/75 |
| 1001 baseline | signed/Classical/1962-10 | 2.00 | 0/62 |
| 1001 baseline | signed/TexMex/1962-10 | 1.00 | 0/42 |
| 1001 baseline | signed/Comedy/1962-10 | 5.00 | 0/77 |
| 1001 baseline | signed/LatinPop/1962-10 | 1.00 | 0/40 |
| 1001 baseline | signed/Childrens/1962-10 | 5.00 | 0/42 |
| 1001 baseline | signed/SurfRock/1962-10 | 1362.00 | 0/43 |
| 1001 baseline | signed/ContemporaryFolk/1962-10 | 525.00 | 0/32 |
| 1001 baseline | signed/BossaNova/1962-10 | 508.00 | 0/19 |
| 1001 baseline | unsigned/Classical/1962-11 | 2.00 | 0/164 |
| 1001 baseline | unsigned/Comedy/1962-11 | 5.00 | 0/188 |
| 1001 baseline | unsigned/LatinPop/1962-11 | 1.00 | 0/125 |
| 1001 baseline | unsigned/TexMex/1962-11 | 1.00 | 0/97 |
| 1001 baseline | unsigned/Childrens/1962-11 | 5.00 | 0/143 |
| 1001 baseline | unsigned/BossaNova/1962-11 | 508.00 | 0/77 |
| 1001 baseline | unsigned/SurfRock/1962-11 | 1362.00 | 0/173 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-11 | 527.00 | 0/101 |
| 1001 baseline | signed/Classical/1962-11 | 2.00 | 0/65 |
| 1001 baseline | signed/TexMex/1962-11 | 1.00 | 0/43 |
| 1001 baseline | signed/Comedy/1962-11 | 5.00 | 0/83 |
| 1001 baseline | signed/LatinPop/1962-11 | 1.00 | 0/42 |
| 1001 baseline | signed/Childrens/1962-11 | 5.00 | 0/43 |
| 1001 baseline | signed/SurfRock/1962-11 | 1362.00 | 0/47 |
| 1001 baseline | signed/ContemporaryFolk/1962-11 | 527.00 | 0/33 |
| 1001 baseline | signed/BossaNova/1962-11 | 508.00 | 0/25 |
| 1001 baseline | unsigned/Classical/1962-12 | 2.00 | 0/175 |
| 1001 baseline | unsigned/Comedy/1962-12 | 5.00 | 0/199 |
| 1001 baseline | unsigned/LatinPop/1962-12 | 1.00 | 0/120 |
| 1001 baseline | unsigned/TexMex/1962-12 | 1.00 | 0/102 |
| 1001 baseline | unsigned/Childrens/1962-12 | 5.00 | 0/150 |
| 1001 baseline | unsigned/BossaNova/1962-12 | 508.00 | 0/83 |
| 1001 baseline | unsigned/SurfRock/1962-12 | 1365.00 | 0/177 |
| 1001 baseline | unsigned/ContemporaryFolk/1962-12 | 527.00 | 0/98 |
| 1001 baseline | signed/Classical/1962-12 | 2.00 | 0/67 |
| 1001 baseline | signed/TexMex/1962-12 | 1.00 | 0/44 |
| 1001 baseline | signed/Comedy/1962-12 | 5.00 | 0/85 |
| 1001 baseline | signed/LatinPop/1962-12 | 1.00 | 0/50 |
| 1001 baseline | signed/Childrens/1962-12 | 5.00 | 0/50 |
| 1001 baseline | signed/SurfRock/1962-12 | 1365.00 | 0/59 |
| 1001 baseline | signed/ContemporaryFolk/1962-12 | 527.00 | 0/37 |
| 1001 baseline | signed/BossaNova/1962-12 | 508.00 | 0/31 |
| 1001 baseline | unsigned/Classical/1963-01 | 2.00 | 0/176 |
| 1001 baseline | unsigned/Comedy/1963-01 | 5.00 | 0/212 |
| 1001 baseline | unsigned/LatinPop/1963-01 | 1.00 | 0/129 |
| 1001 baseline | unsigned/TexMex/1963-01 | 1.00 | 0/109 |
| 1001 baseline | unsigned/Childrens/1963-01 | 5.00 | 0/154 |
| 1001 baseline | unsigned/BossaNova/1963-01 | 508.00 | 0/86 |
| 1001 baseline | unsigned/SurfRock/1963-01 | 1366.00 | 0/173 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-01 | 528.00 | 0/104 |
| 1001 baseline | signed/Classical/1963-01 | 2.00 | 0/70 |
| 1001 baseline | signed/TexMex/1963-01 | 1.00 | 0/48 |
| 1001 baseline | signed/Comedy/1963-01 | 5.00 | 0/83 |
| 1001 baseline | signed/LatinPop/1963-01 | 1.00 | 0/52 |
| 1001 baseline | signed/Childrens/1963-01 | 5.00 | 0/55 |
| 1001 baseline | signed/SurfRock/1963-01 | 1366.00 | 0/67 |
| 1001 baseline | signed/ContemporaryFolk/1963-01 | 528.00 | 0/37 |
| 1001 baseline | signed/BossaNova/1963-01 | 508.00 | 0/38 |
| 1001 baseline | unsigned/Classical/1963-02 | 2.00 | 0/182 |
| 1001 baseline | unsigned/Comedy/1963-02 | 5.00 | 0/211 |
| 1001 baseline | unsigned/LatinPop/1963-02 | 1.00 | 0/128 |
| 1001 baseline | unsigned/TexMex/1963-02 | 1.00 | 0/112 |
| 1001 baseline | unsigned/Childrens/1963-02 | 5.00 | 0/154 |
| 1001 baseline | unsigned/BossaNova/1963-02 | 508.00 | 0/87 |
| 1001 baseline | unsigned/SurfRock/1963-02 | 1367.00 | 0/171 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-02 | 530.00 | 0/101 |
| 1001 baseline | signed/Classical/1963-02 | 2.00 | 0/74 |
| 1001 baseline | signed/TexMex/1963-02 | 1.00 | 0/46 |
| 1001 baseline | signed/Comedy/1963-02 | 5.00 | 0/87 |
| 1001 baseline | signed/LatinPop/1963-02 | 1.00 | 0/56 |
| 1001 baseline | signed/Childrens/1963-02 | 5.00 | 0/63 |
| 1001 baseline | signed/SurfRock/1963-02 | 1367.00 | 0/77 |
| 1001 baseline | signed/ContemporaryFolk/1963-02 | 530.00 | 0/46 |
| 1001 baseline | signed/BossaNova/1963-02 | 508.00 | 0/44 |
| 1001 baseline | unsigned/Classical/1963-03 | 2.00 | 0/184 |
| 1001 baseline | unsigned/Comedy/1963-03 | 5.00 | 0/208 |
| 1001 baseline | unsigned/LatinPop/1963-03 | 1.00 | 0/135 |
| 1001 baseline | unsigned/TexMex/1963-03 | 1.00 | 0/118 |
| 1001 baseline | unsigned/Childrens/1963-03 | 5.00 | 0/154 |
| 1001 baseline | unsigned/BossaNova/1963-03 | 508.00 | 0/90 |
| 1001 baseline | unsigned/SurfRock/1963-03 | 1367.00 | 0/179 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-03 | 532.00 | 0/104 |
| 1001 baseline | signed/Classical/1963-03 | 2.00 | 0/80 |
| 1001 baseline | signed/TexMex/1963-03 | 1.00 | 0/46 |
| 1001 baseline | signed/Comedy/1963-03 | 5.00 | 0/98 |
| 1001 baseline | signed/LatinPop/1963-03 | 1.00 | 0/61 |
| 1001 baseline | signed/Childrens/1963-03 | 5.00 | 0/71 |
| 1001 baseline | signed/SurfRock/1963-03 | 1367.00 | 0/86 |
| 1001 baseline | signed/ContemporaryFolk/1963-03 | 532.00 | 0/52 |
| 1001 baseline | signed/BossaNova/1963-03 | 508.00 | 0/50 |
| 1001 baseline | unsigned/Classical/1963-04 | 2.00 | 0/176 |
| 1001 baseline | unsigned/Comedy/1963-04 | 5.00 | 0/213 |
| 1001 baseline | unsigned/LatinPop/1963-04 | 1.00 | 0/135 |
| 1001 baseline | unsigned/TexMex/1963-04 | 1.00 | 0/122 |
| 1001 baseline | unsigned/Childrens/1963-04 | 5.00 | 0/158 |
| 1001 baseline | unsigned/BossaNova/1963-04 | 508.00 | 0/89 |
| 1001 baseline | unsigned/SurfRock/1963-04 | 1369.00 | 0/172 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-04 | 532.00 | 0/92 |
| 1001 baseline | signed/Classical/1963-04 | 2.00 | 0/88 |
| 1001 baseline | signed/TexMex/1963-04 | 1.00 | 0/48 |
| 1001 baseline | signed/Comedy/1963-04 | 5.00 | 0/104 |
| 1001 baseline | signed/LatinPop/1963-04 | 1.00 | 0/65 |
| 1001 baseline | signed/Childrens/1963-04 | 5.00 | 0/86 |
| 1001 baseline | signed/SurfRock/1963-04 | 1369.00 | 0/105 |
| 1001 baseline | signed/ContemporaryFolk/1963-04 | 532.00 | 0/65 |
| 1001 baseline | signed/BossaNova/1963-04 | 508.00 | 0/62 |
| 1001 baseline | unsigned/Classical/1963-05 | 3.00 | 0/178 |
| 1001 baseline | unsigned/Comedy/1963-05 | 5.00 | 0/229 |
| 1001 baseline | unsigned/LatinPop/1963-05 | 1.00 | 0/134 |
| 1001 baseline | unsigned/TexMex/1963-05 | 1.00 | 0/124 |
| 1001 baseline | unsigned/SurfRock/1963-05 | 1369.00 | 0/170 |
| 1001 baseline | unsigned/Childrens/1963-05 | 5.00 | 0/164 |
| 1001 baseline | unsigned/BossaNova/1963-05 | 509.00 | 0/98 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-05 | 533.00 | 0/96 |
| 1001 baseline | signed/Classical/1963-05 | 3.00 | 0/89 |
| 1001 baseline | signed/TexMex/1963-05 | 1.00 | 0/52 |
| 1001 baseline | signed/Comedy/1963-05 | 5.00 | 0/106 |
| 1001 baseline | signed/LatinPop/1963-05 | 1.00 | 0/68 |
| 1001 baseline | signed/Childrens/1963-05 | 5.00 | 0/86 |
| 1001 baseline | signed/SurfRock/1963-05 | 1369.00 | 0/108 |
| 1001 baseline | signed/ContemporaryFolk/1963-05 | 533.00 | 0/70 |
| 1001 baseline | signed/BossaNova/1963-05 | 509.00 | 0/64 |
| 1001 baseline | unsigned/Classical/1963-06 | 3.00 | 0/193 |
| 1001 baseline | unsigned/Comedy/1963-06 | 5.00 | 0/237 |
| 1001 baseline | unsigned/LatinPop/1963-06 | 1.00 | 0/141 |
| 1001 baseline | unsigned/TexMex/1963-06 | 1.00 | 0/122 |
| 1001 baseline | unsigned/SurfRock/1963-06 | 1369.00 | 0/175 |
| 1001 baseline | unsigned/Childrens/1963-06 | 5.00 | 0/174 |
| 1001 baseline | unsigned/BossaNova/1963-06 | 509.00 | 0/108 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-06 | 536.00 | 0/96 |
| 1001 baseline | signed/Classical/1963-06 | 3.00 | 0/88 |
| 1001 baseline | signed/TexMex/1963-06 | 1.00 | 0/56 |
| 1001 baseline | signed/Comedy/1963-06 | 5.00 | 0/105 |
| 1001 baseline | signed/LatinPop/1963-06 | 1.00 | 0/71 |
| 1001 baseline | signed/Childrens/1963-06 | 5.00 | 0/89 |
| 1001 baseline | signed/SurfRock/1963-06 | 1369.00 | 0/118 |
| 1001 baseline | signed/ContemporaryFolk/1963-06 | 536.00 | 0/78 |
| 1001 baseline | signed/BossaNova/1963-06 | 509.00 | 0/68 |
| 1001 baseline | unsigned/Classical/1963-07 | 3.00 | 0/286 |
| 1001 baseline | unsigned/Comedy/1963-07 | 5.00 | 0/270 |
| 1001 baseline | unsigned/LatinPop/1963-07 | 1.00 | 0/183 |
| 1001 baseline | unsigned/TexMex/1963-07 | 1.00 | 0/127 |
| 1001 baseline | unsigned/SurfRock/1963-07 | 1370.00 | 0/189 |
| 1001 baseline | unsigned/Childrens/1963-07 | 5.00 | 0/191 |
| 1001 baseline | unsigned/BossaNova/1963-07 | 509.00 | 0/114 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-07 | 537.00 | 0/104 |
| 1001 baseline | signed/TexMex/1963-07 | 1.00 | 0/58 |
| 1001 baseline | signed/Classical/1963-07 | 3.00 | 0/91 |
| 1001 baseline | signed/Comedy/1963-07 | 5.00 | 0/104 |
| 1001 baseline | signed/LatinPop/1963-07 | 1.00 | 0/74 |
| 1001 baseline | signed/Childrens/1963-07 | 5.00 | 0/93 |
| 1001 baseline | signed/SurfRock/1963-07 | 1370.00 | 0/117 |
| 1001 baseline | signed/ContemporaryFolk/1963-07 | 537.00 | 0/74 |
| 1001 baseline | signed/BossaNova/1963-07 | 509.00 | 0/67 |
| 1001 baseline | unsigned/Classical/1963-08 | 3.00 | 0/296 |
| 1001 baseline | unsigned/LatinPop/1963-08 | 1.00 | 0/190 |
| 1001 baseline | unsigned/Comedy/1963-08 | 5.00 | 0/281 |
| 1001 baseline | unsigned/TexMex/1963-08 | 1.00 | 0/134 |
| 1001 baseline | unsigned/SurfRock/1963-08 | 1372.00 | 0/199 |
| 1001 baseline | unsigned/Childrens/1963-08 | 5.00 | 0/204 |
| 1001 baseline | unsigned/BossaNova/1963-08 | 509.00 | 0/126 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-08 | 537.00 | 0/113 |
| 1001 baseline | signed/TexMex/1963-08 | 1.00 | 0/58 |
| 1001 baseline | signed/Classical/1963-08 | 3.00 | 0/91 |
| 1001 baseline | signed/Comedy/1963-08 | 5.00 | 0/101 |
| 1001 baseline | signed/LatinPop/1963-08 | 1.00 | 0/71 |
| 1001 baseline | signed/Childrens/1963-08 | 5.00 | 0/90 |
| 1001 baseline | signed/SurfRock/1963-08 | 1372.00 | 0/113 |
| 1001 baseline | signed/ContemporaryFolk/1963-08 | 537.00 | 0/75 |
| 1001 baseline | signed/BossaNova/1963-08 | 509.00 | 0/67 |
| 1001 baseline | unsigned/Classical/1963-09 | 3.00 | 0/303 |
| 1001 baseline | unsigned/LatinPop/1963-09 | 1.00 | 0/191 |
| 1001 baseline | unsigned/Comedy/1963-09 | 5.00 | 0/287 |
| 1001 baseline | unsigned/BossaNova/1963-09 | 509.00 | 0/130 |
| 1001 baseline | unsigned/TexMex/1963-09 | 1.00 | 0/139 |
| 1001 baseline | unsigned/SurfRock/1963-09 | 1373.00 | 0/209 |
| 1001 baseline | unsigned/Childrens/1963-09 | 5.00 | 0/215 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-09 | 538.00 | 0/124 |
| 1001 baseline | signed/Classical/1963-09 | 3.00 | 0/94 |
| 1001 baseline | signed/Comedy/1963-09 | 5.00 | 0/104 |
| 1001 baseline | signed/LatinPop/1963-09 | 1.00 | 0/70 |
| 1001 baseline | signed/Childrens/1963-09 | 5.00 | 0/86 |
| 1001 baseline | signed/TexMex/1963-09 | 1.00 | 0/56 |
| 1001 baseline | signed/SurfRock/1963-09 | 1373.00 | 0/115 |
| 1001 baseline | signed/ContemporaryFolk/1963-09 | 538.00 | 0/70 |
| 1001 baseline | signed/BossaNova/1963-09 | 509.00 | 0/70 |
| 1001 baseline | unsigned/Classical/1963-10 | 3.00 | 0/309 |
| 1001 baseline | unsigned/LatinPop/1963-10 | 1.00 | 0/193 |
| 1001 baseline | unsigned/Comedy/1963-10 | 5.00 | 0/287 |
| 1001 baseline | unsigned/BossaNova/1963-10 | 509.00 | 0/126 |
| 1001 baseline | unsigned/TexMex/1963-10 | 1.00 | 0/139 |
| 1001 baseline | unsigned/SurfRock/1963-10 | 1373.00 | 0/222 |
| 1001 baseline | unsigned/Childrens/1963-10 | 5.00 | 0/228 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-10 | 538.00 | 0/132 |
| 1001 baseline | signed/Classical/1963-10 | 3.00 | 0/94 |
| 1001 baseline | signed/Comedy/1963-10 | 5.00 | 0/102 |
| 1001 baseline | signed/LatinPop/1963-10 | 1.00 | 0/70 |
| 1001 baseline | signed/Childrens/1963-10 | 5.00 | 0/86 |
| 1001 baseline | signed/TexMex/1963-10 | 1.00 | 0/61 |
| 1001 baseline | signed/SurfRock/1963-10 | 1373.00 | 0/114 |
| 1001 baseline | signed/ContemporaryFolk/1963-10 | 538.00 | 0/68 |
| 1001 baseline | signed/BossaNova/1963-10 | 509.00 | 0/72 |
| 1001 baseline | unsigned/Classical/1963-11 | 3.00 | 0/316 |
| 1001 baseline | unsigned/LatinPop/1963-11 | 1.00 | 0/202 |
| 1001 baseline | unsigned/Comedy/1963-11 | 5.00 | 0/299 |
| 1001 baseline | unsigned/BossaNova/1963-11 | 510.00 | 0/127 |
| 1001 baseline | unsigned/TexMex/1963-11 | 1.00 | 0/141 |
| 1001 baseline | unsigned/SurfRock/1963-11 | 1374.00 | 0/233 |
| 1001 baseline | unsigned/Childrens/1963-11 | 5.00 | 0/237 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-11 | 539.00 | 0/144 |
| 1001 baseline | signed/Classical/1963-11 | 3.00 | 0/92 |
| 1001 baseline | signed/Comedy/1963-11 | 5.00 | 0/98 |
| 1001 baseline | signed/LatinPop/1963-11 | 1.00 | 0/69 |
| 1001 baseline | signed/Childrens/1963-11 | 5.00 | 0/85 |
| 1001 baseline | signed/TexMex/1963-11 | 1.00 | 0/61 |
| 1001 baseline | signed/SurfRock/1963-11 | 1374.00 | 0/115 |
| 1001 baseline | signed/ContemporaryFolk/1963-11 | 539.00 | 0/65 |
| 1001 baseline | signed/BossaNova/1963-11 | 510.00 | 0/71 |
| 1001 baseline | unsigned/Classical/1963-12 | 3.00 | 0/312 |
| 1001 baseline | unsigned/LatinPop/1963-12 | 1.00 | 0/218 |
| 1001 baseline | unsigned/BossaNova/1963-12 | 513.00 | 0/135 |
| 1001 baseline | unsigned/Comedy/1963-12 | 5.00 | 0/302 |
| 1001 baseline | unsigned/TexMex/1963-12 | 1.00 | 0/141 |
| 1001 baseline | unsigned/SurfRock/1963-12 | 1378.00 | 0/240 |
| 1001 baseline | unsigned/Childrens/1963-12 | 5.00 | 0/241 |
| 1001 baseline | unsigned/ContemporaryFolk/1963-12 | 539.00 | 0/153 |
| 1001 baseline | signed/Classical/1963-12 | 3.00 | 0/93 |
| 1001 baseline | signed/Comedy/1963-12 | 5.00 | 0/101 |
| 1001 baseline | signed/LatinPop/1963-12 | 1.00 | 0/62 |
| 1001 baseline | signed/Childrens/1963-12 | 5.00 | 0/89 |
| 1001 baseline | signed/TexMex/1963-12 | 1.00 | 0/69 |
| 1001 baseline | signed/SurfRock/1963-12 | 1378.00 | 0/117 |
| 1001 baseline | signed/ContemporaryFolk/1963-12 | 539.00 | 0/66 |
| 1001 baseline | signed/BossaNova/1963-12 | 513.00 | 0/71 |
| 1002 baseline | unsigned/LatinPop/1960-01 | 0.00 | 29/29 |
| 1002 baseline | unsigned/LatinPop/1960-63 | 0.00 | 5492/5492 |
| 1002 baseline | unsigned/Classical/1960-01 | 0.00 | 47/47 |
| 1002 baseline | unsigned/Classical/1960-63 | 1.17 | 5238/9272 |
| 1002 baseline | unsigned/Childrens/1960-01 | 0.00 | 11/11 |
| 1002 baseline | unsigned/Childrens/1960-63 | 7.79 | 42/6529 |
| 1002 baseline | unsigned/Comedy/1960-01 | 0.00 | 9/9 |
| 1002 baseline | unsigned/Comedy/1960-63 | 5.99 | 40/8561 |
| 1002 baseline | unsigned/TexMex/1960-01 | 0.00 | 4/4 |
| 1002 baseline | unsigned/TexMex/1960-63 | 0.00 | 3843/3843 |
| 1002 baseline | signed/Comedy/1960-01 | 0.00 | 18/18 |
| 1002 baseline | signed/Comedy/1960-63 | 6.53 | 72/2742 |
| 1002 baseline | signed/Classical/1960-01 | 0.00 | 49/49 |
| 1002 baseline | signed/Classical/1960-63 | 0.99 | 1999/3252 |
| 1002 baseline | signed/TexMex/1960-01 | 0.00 | 3/3 |
| 1002 baseline | signed/TexMex/1960-63 | 0.00 | 1043/1043 |
| 1002 baseline | signed/Childrens/1960-01 | 0.00 | 9/9 |
| 1002 baseline | signed/Childrens/1960-63 | 7.96 | 40/1880 |
| 1002 baseline | unsigned/Classical/1960-02 | 0.00 | 29/29 |
| 1002 baseline | unsigned/Childrens/1960-02 | 0.00 | 13/13 |
| 1002 baseline | unsigned/LatinPop/1960-02 | 0.00 | 22/22 |
| 1002 baseline | unsigned/Comedy/1960-02 | 0.00 | 10/10 |
| 1002 baseline | unsigned/TexMex/1960-02 | 0.00 | 7/7 |
| 1002 baseline | unsigned/SurfRock/1960-02 | 1293.00 | 0/1 |
| 1002 baseline | unsigned/SurfRock/1960-63 | 1352.51 | 0/4608 |
| 1002 baseline | signed/LatinPop/1960-02 | 0.00 | 12/12 |
| 1002 baseline | signed/LatinPop/1960-63 | 0.00 | 1391/1391 |
| 1002 baseline | signed/Classical/1960-02 | 0.00 | 76/76 |
| 1002 baseline | signed/Comedy/1960-02 | 0.00 | 27/27 |
| 1002 baseline | signed/Childrens/1960-02 | 0.00 | 15/15 |
| 1002 baseline | signed/TexMex/1960-02 | 0.00 | 4/4 |
| 1002 baseline | signed/SurfRock/1960-02 | 1293.00 | 0/1 |
| 1002 baseline | signed/SurfRock/1960-63 | 1359.21 | 0/1456 |
| 1002 baseline | unsigned/Classical/1960-03 | 0.00 | 36/36 |
| 1002 baseline | unsigned/Childrens/1960-03 | 0.00 | 18/18 |
| 1002 baseline | unsigned/LatinPop/1960-03 | 0.00 | 26/26 |
| 1002 baseline | unsigned/Comedy/1960-03 | 0.00 | 21/21 |
| 1002 baseline | unsigned/TexMex/1960-03 | 0.00 | 13/13 |
| 1002 baseline | unsigned/SurfRock/1960-03 | 1295.00 | 0/4 |
| 1002 baseline | signed/LatinPop/1960-03 | 0.00 | 13/13 |
| 1002 baseline | signed/Classical/1960-03 | 0.00 | 77/77 |
| 1002 baseline | signed/Comedy/1960-03 | 0.00 | 27/27 |
| 1002 baseline | signed/Childrens/1960-03 | 0.00 | 16/16 |
| 1002 baseline | signed/TexMex/1960-03 | 0.00 | 4/4 |
| 1002 baseline | signed/SurfRock/1960-03 | 1295.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1960-04 | 0.00 | 41/41 |
| 1002 baseline | unsigned/Childrens/1960-04 | 1.00 | 0/26 |
| 1002 baseline | unsigned/LatinPop/1960-04 | 0.00 | 30/30 |
| 1002 baseline | unsigned/Comedy/1960-04 | 1.00 | 0/32 |
| 1002 baseline | unsigned/TexMex/1960-04 | 0.00 | 16/16 |
| 1002 baseline | unsigned/SurfRock/1960-04 | 1298.00 | 0/9 |
| 1002 baseline | signed/LatinPop/1960-04 | 0.00 | 14/14 |
| 1002 baseline | signed/Classical/1960-04 | 0.00 | 77/77 |
| 1002 baseline | signed/Comedy/1960-04 | 1.00 | 0/28 |
| 1002 baseline | signed/Childrens/1960-04 | 1.00 | 0/16 |
| 1002 baseline | signed/TexMex/1960-04 | 0.00 | 4/4 |
| 1002 baseline | signed/SurfRock/1960-04 | 1298.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1960-05 | 0.00 | 49/49 |
| 1002 baseline | unsigned/Childrens/1960-05 | 1.00 | 0/36 |
| 1002 baseline | unsigned/LatinPop/1960-05 | 0.00 | 37/37 |
| 1002 baseline | unsigned/Comedy/1960-05 | 1.00 | 0/52 |
| 1002 baseline | unsigned/TexMex/1960-05 | 0.00 | 22/22 |
| 1002 baseline | unsigned/SurfRock/1960-05 | 1301.00 | 0/13 |
| 1002 baseline | signed/LatinPop/1960-05 | 0.00 | 14/14 |
| 1002 baseline | signed/Classical/1960-05 | 0.00 | 77/77 |
| 1002 baseline | signed/Comedy/1960-05 | 1.00 | 0/28 |
| 1002 baseline | signed/Childrens/1960-05 | 1.00 | 0/16 |
| 1002 baseline | signed/TexMex/1960-05 | 0.00 | 4/4 |
| 1002 baseline | signed/SurfRock/1960-05 | 1301.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1960-06 | 0.00 | 54/54 |
| 1002 baseline | unsigned/Childrens/1960-06 | 2.00 | 0/46 |
| 1002 baseline | unsigned/LatinPop/1960-06 | 0.00 | 41/41 |
| 1002 baseline | unsigned/Comedy/1960-06 | 2.00 | 0/61 |
| 1002 baseline | unsigned/TexMex/1960-06 | 0.00 | 26/26 |
| 1002 baseline | unsigned/SurfRock/1960-06 | 1304.00 | 0/14 |
| 1002 baseline | signed/LatinPop/1960-06 | 0.00 | 14/14 |
| 1002 baseline | signed/Classical/1960-06 | 0.00 | 76/76 |
| 1002 baseline | signed/Comedy/1960-06 | 2.00 | 0/28 |
| 1002 baseline | signed/Childrens/1960-06 | 2.00 | 0/16 |
| 1002 baseline | signed/TexMex/1960-06 | 0.00 | 4/4 |
| 1002 baseline | signed/SurfRock/1960-06 | 1304.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1960-07 | 0.00 | 65/65 |
| 1002 baseline | unsigned/Comedy/1960-07 | 2.00 | 0/78 |
| 1002 baseline | unsigned/Childrens/1960-07 | 2.00 | 0/48 |
| 1002 baseline | unsigned/LatinPop/1960-07 | 0.00 | 43/43 |
| 1002 baseline | unsigned/TexMex/1960-07 | 0.00 | 28/28 |
| 1002 baseline | unsigned/SurfRock/1960-07 | 1308.00 | 0/17 |
| 1002 baseline | signed/LatinPop/1960-07 | 0.00 | 16/16 |
| 1002 baseline | signed/Classical/1960-07 | 0.00 | 74/74 |
| 1002 baseline | signed/Childrens/1960-07 | 2.00 | 0/18 |
| 1002 baseline | signed/Comedy/1960-07 | 2.00 | 0/23 |
| 1002 baseline | signed/TexMex/1960-07 | 0.00 | 5/5 |
| 1002 baseline | signed/SurfRock/1960-07 | 1308.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1960-08 | 0.00 | 76/76 |
| 1002 baseline | unsigned/Comedy/1960-08 | 2.00 | 0/90 |
| 1002 baseline | unsigned/Childrens/1960-08 | 2.00 | 0/57 |
| 1002 baseline | unsigned/LatinPop/1960-08 | 0.00 | 47/47 |
| 1002 baseline | unsigned/TexMex/1960-08 | 0.00 | 30/30 |
| 1002 baseline | unsigned/SurfRock/1960-08 | 1310.00 | 0/21 |
| 1002 baseline | signed/LatinPop/1960-08 | 0.00 | 18/18 |
| 1002 baseline | signed/Classical/1960-08 | 0.00 | 72/72 |
| 1002 baseline | signed/Childrens/1960-08 | 2.00 | 0/16 |
| 1002 baseline | signed/Comedy/1960-08 | 2.00 | 0/22 |
| 1002 baseline | signed/TexMex/1960-08 | 0.00 | 5/5 |
| 1002 baseline | signed/SurfRock/1960-08 | 1310.00 | 0/2 |
| 1002 baseline | unsigned/LatinPop/1960-09 | 0.00 | 53/53 |
| 1002 baseline | unsigned/Classical/1960-09 | 0.00 | 87/87 |
| 1002 baseline | unsigned/Comedy/1960-09 | 2.00 | 0/97 |
| 1002 baseline | unsigned/Childrens/1960-09 | 2.00 | 0/64 |
| 1002 baseline | unsigned/TexMex/1960-09 | 0.00 | 36/36 |
| 1002 baseline | unsigned/SurfRock/1960-09 | 1314.00 | 0/22 |
| 1002 baseline | signed/Classical/1960-09 | 0.00 | 67/67 |
| 1002 baseline | signed/LatinPop/1960-09 | 0.00 | 14/14 |
| 1002 baseline | signed/Childrens/1960-09 | 2.00 | 0/14 |
| 1002 baseline | signed/Comedy/1960-09 | 2.00 | 0/22 |
| 1002 baseline | signed/TexMex/1960-09 | 0.00 | 4/4 |
| 1002 baseline | signed/SurfRock/1960-09 | 1314.00 | 0/2 |
| 1002 baseline | unsigned/LatinPop/1960-10 | 0.00 | 59/59 |
| 1002 baseline | unsigned/Classical/1960-10 | 0.00 | 95/95 |
| 1002 baseline | unsigned/Comedy/1960-10 | 2.00 | 0/107 |
| 1002 baseline | unsigned/Childrens/1960-10 | 2.00 | 0/66 |
| 1002 baseline | unsigned/TexMex/1960-10 | 0.00 | 37/37 |
| 1002 baseline | unsigned/SurfRock/1960-10 | 1314.00 | 0/25 |
| 1002 baseline | signed/Classical/1960-10 | 0.00 | 67/67 |
| 1002 baseline | signed/LatinPop/1960-10 | 0.00 | 14/14 |
| 1002 baseline | signed/Childrens/1960-10 | 2.00 | 0/17 |
| 1002 baseline | signed/Comedy/1960-10 | 2.00 | 0/23 |
| 1002 baseline | signed/TexMex/1960-10 | 0.00 | 6/6 |
| 1002 baseline | signed/SurfRock/1960-10 | 1314.00 | 0/2 |
| 1002 baseline | unsigned/LatinPop/1960-11 | 0.00 | 62/62 |
| 1002 baseline | unsigned/Classical/1960-11 | 0.00 | 101/101 |
| 1002 baseline | unsigned/Comedy/1960-11 | 2.00 | 0/120 |
| 1002 baseline | unsigned/Childrens/1960-11 | 2.00 | 0/71 |
| 1002 baseline | unsigned/TexMex/1960-11 | 0.00 | 40/40 |
| 1002 baseline | unsigned/SurfRock/1960-11 | 1315.00 | 0/31 |
| 1002 baseline | signed/Classical/1960-11 | 0.00 | 66/66 |
| 1002 baseline | signed/LatinPop/1960-11 | 0.00 | 13/13 |
| 1002 baseline | signed/Childrens/1960-11 | 2.00 | 0/18 |
| 1002 baseline | signed/Comedy/1960-11 | 2.00 | 0/23 |
| 1002 baseline | signed/TexMex/1960-11 | 0.00 | 6/6 |
| 1002 baseline | signed/SurfRock/1960-11 | 1315.00 | 0/2 |
| 1002 baseline | unsigned/LatinPop/1960-12 | 0.00 | 67/67 |
| 1002 baseline | unsigned/Classical/1960-12 | 0.00 | 109/109 |
| 1002 baseline | unsigned/Comedy/1960-12 | 2.00 | 0/132 |
| 1002 baseline | unsigned/Childrens/1960-12 | 2.00 | 0/83 |
| 1002 baseline | unsigned/TexMex/1960-12 | 0.00 | 48/48 |
| 1002 baseline | unsigned/SurfRock/1960-12 | 1317.00 | 0/39 |
| 1002 baseline | signed/Classical/1960-12 | 0.00 | 66/66 |
| 1002 baseline | signed/Childrens/1960-12 | 2.00 | 0/19 |
| 1002 baseline | signed/Comedy/1960-12 | 2.00 | 0/23 |
| 1002 baseline | signed/TexMex/1960-12 | 0.00 | 6/6 |
| 1002 baseline | signed/LatinPop/1960-12 | 0.00 | 11/11 |
| 1002 baseline | signed/SurfRock/1960-12 | 1317.00 | 0/1 |
| 1002 baseline | unsigned/LatinPop/1961-01 | 0.00 | 104/104 |
| 1002 baseline | unsigned/Classical/1961-01 | 0.00 | 242/242 |
| 1002 baseline | unsigned/Comedy/1961-01 | 2.00 | 0/169 |
| 1002 baseline | unsigned/Childrens/1961-01 | 2.00 | 0/100 |
| 1002 baseline | unsigned/TexMex/1961-01 | 0.00 | 60/60 |
| 1002 baseline | unsigned/SurfRock/1961-01 | 1318.00 | 0/38 |
| 1002 baseline | signed/Classical/1961-01 | 0.00 | 56/56 |
| 1002 baseline | signed/Childrens/1961-01 | 2.00 | 0/27 |
| 1002 baseline | signed/TexMex/1961-01 | 0.00 | 8/8 |
| 1002 baseline | signed/Comedy/1961-01 | 2.00 | 0/29 |
| 1002 baseline | signed/LatinPop/1961-01 | 0.00 | 12/12 |
| 1002 baseline | signed/SurfRock/1961-01 | 1318.00 | 0/3 |
| 1002 baseline | unsigned/LatinPop/1961-02 | 0.00 | 114/114 |
| 1002 baseline | unsigned/Classical/1961-02 | 0.00 | 244/244 |
| 1002 baseline | unsigned/Comedy/1961-02 | 2.00 | 0/170 |
| 1002 baseline | unsigned/Childrens/1961-02 | 2.00 | 0/105 |
| 1002 baseline | unsigned/TexMex/1961-02 | 0.00 | 63/63 |
| 1002 baseline | unsigned/SurfRock/1961-02 | 1319.00 | 0/47 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-02 | 511.00 | 0/4 |
| 1002 baseline | unsigned/ContemporaryFolk/1960-63 | 527.35 | 0/2898 |
| 1002 baseline | signed/Classical/1961-02 | 0.00 | 62/62 |
| 1002 baseline | signed/Childrens/1961-02 | 2.00 | 0/31 |
| 1002 baseline | signed/TexMex/1961-02 | 0.00 | 9/9 |
| 1002 baseline | signed/Comedy/1961-02 | 2.00 | 0/35 |
| 1002 baseline | signed/LatinPop/1961-02 | 0.00 | 12/12 |
| 1002 baseline | signed/SurfRock/1961-02 | 1319.00 | 0/5 |
| 1002 baseline | unsigned/LatinPop/1961-03 | 0.00 | 116/116 |
| 1002 baseline | unsigned/Classical/1961-03 | 0.00 | 246/246 |
| 1002 baseline | unsigned/Comedy/1961-03 | 2.00 | 0/189 |
| 1002 baseline | unsigned/Childrens/1961-03 | 2.00 | 0/113 |
| 1002 baseline | unsigned/TexMex/1961-03 | 0.00 | 72/72 |
| 1002 baseline | unsigned/SurfRock/1961-03 | 1320.00 | 0/50 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-03 | 512.00 | 0/7 |
| 1002 baseline | signed/Classical/1961-03 | 0.00 | 64/64 |
| 1002 baseline | signed/Childrens/1961-03 | 2.00 | 0/32 |
| 1002 baseline | signed/TexMex/1961-03 | 0.00 | 10/10 |
| 1002 baseline | signed/Comedy/1961-03 | 2.00 | 0/32 |
| 1002 baseline | signed/LatinPop/1961-03 | 0.00 | 13/13 |
| 1002 baseline | signed/SurfRock/1961-03 | 1320.00 | 0/6 |
| 1002 baseline | unsigned/LatinPop/1961-04 | 0.00 | 130/130 |
| 1002 baseline | unsigned/Classical/1961-04 | 0.00 | 254/254 |
| 1002 baseline | unsigned/Comedy/1961-04 | 3.00 | 0/199 |
| 1002 baseline | unsigned/Childrens/1961-04 | 3.00 | 0/121 |
| 1002 baseline | unsigned/TexMex/1961-04 | 0.00 | 80/80 |
| 1002 baseline | unsigned/SurfRock/1961-04 | 1327.00 | 0/55 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-04 | 513.00 | 0/13 |
| 1002 baseline | signed/Classical/1961-04 | 0.00 | 63/63 |
| 1002 baseline | signed/Childrens/1961-04 | 3.00 | 0/32 |
| 1002 baseline | signed/TexMex/1961-04 | 0.00 | 9/9 |
| 1002 baseline | signed/Comedy/1961-04 | 3.00 | 0/33 |
| 1002 baseline | signed/LatinPop/1961-04 | 0.00 | 13/13 |
| 1002 baseline | signed/SurfRock/1961-04 | 1327.00 | 0/5 |
| 1002 baseline | unsigned/LatinPop/1961-05 | 0.00 | 134/134 |
| 1002 baseline | unsigned/Classical/1961-05 | 0.00 | 263/263 |
| 1002 baseline | unsigned/Comedy/1961-05 | 4.00 | 0/209 |
| 1002 baseline | unsigned/Childrens/1961-05 | 4.00 | 0/129 |
| 1002 baseline | unsigned/TexMex/1961-05 | 0.00 | 86/86 |
| 1002 baseline | unsigned/SurfRock/1961-05 | 1328.00 | 0/59 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-05 | 513.00 | 0/17 |
| 1002 baseline | signed/Classical/1961-05 | 0.00 | 64/64 |
| 1002 baseline | signed/Childrens/1961-05 | 4.00 | 0/32 |
| 1002 baseline | signed/TexMex/1961-05 | 0.00 | 9/9 |
| 1002 baseline | signed/Comedy/1961-05 | 4.00 | 0/33 |
| 1002 baseline | signed/SurfRock/1961-05 | 1328.00 | 0/5 |
| 1002 baseline | signed/LatinPop/1961-05 | 0.00 | 12/12 |
| 1002 baseline | unsigned/LatinPop/1961-06 | 0.00 | 143/143 |
| 1002 baseline | unsigned/Classical/1961-06 | 0.00 | 271/271 |
| 1002 baseline | unsigned/Comedy/1961-06 | 5.00 | 0/220 |
| 1002 baseline | unsigned/Childrens/1961-06 | 5.00 | 0/139 |
| 1002 baseline | unsigned/TexMex/1961-06 | 0.00 | 91/91 |
| 1002 baseline | unsigned/SurfRock/1961-06 | 1330.00 | 0/63 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-06 | 514.00 | 0/24 |
| 1002 baseline | signed/Classical/1961-06 | 0.00 | 59/59 |
| 1002 baseline | signed/Childrens/1961-06 | 5.00 | 0/29 |
| 1002 baseline | signed/TexMex/1961-06 | 0.00 | 10/10 |
| 1002 baseline | signed/Comedy/1961-06 | 5.00 | 0/33 |
| 1002 baseline | signed/SurfRock/1961-06 | 1330.00 | 0/4 |
| 1002 baseline | signed/LatinPop/1961-06 | 0.00 | 12/12 |
| 1002 baseline | unsigned/LatinPop/1961-07 | 0.00 | 132/132 |
| 1002 baseline | unsigned/Comedy/1961-07 | 5.00 | 0/222 |
| 1002 baseline | unsigned/Childrens/1961-07 | 5.00 | 0/142 |
| 1002 baseline | unsigned/Classical/1961-07 | 0.00 | 258/258 |
| 1002 baseline | unsigned/TexMex/1961-07 | 0.00 | 95/95 |
| 1002 baseline | unsigned/SurfRock/1961-07 | 1333.00 | 0/73 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-07 | 516.00 | 0/33 |
| 1002 baseline | signed/Classical/1961-07 | 0.00 | 63/63 |
| 1002 baseline | signed/Childrens/1961-07 | 5.00 | 0/27 |
| 1002 baseline | signed/TexMex/1961-07 | 0.00 | 11/11 |
| 1002 baseline | signed/Comedy/1961-07 | 5.00 | 0/34 |
| 1002 baseline | signed/SurfRock/1961-07 | 1333.00 | 0/5 |
| 1002 baseline | signed/LatinPop/1961-07 | 0.00 | 11/11 |
| 1002 baseline | unsigned/LatinPop/1961-08 | 0.00 | 131/131 |
| 1002 baseline | unsigned/Classical/1961-08 | 0.00 | 255/255 |
| 1002 baseline | unsigned/Comedy/1961-08 | 5.00 | 0/222 |
| 1002 baseline | unsigned/Childrens/1961-08 | 5.00 | 0/143 |
| 1002 baseline | unsigned/TexMex/1961-08 | 0.00 | 91/91 |
| 1002 baseline | unsigned/SurfRock/1961-08 | 1334.00 | 0/73 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-08 | 517.00 | 0/38 |
| 1002 baseline | signed/Classical/1961-08 | 0.00 | 62/62 |
| 1002 baseline | signed/Childrens/1961-08 | 5.00 | 0/27 |
| 1002 baseline | signed/TexMex/1961-08 | 0.00 | 13/13 |
| 1002 baseline | signed/Comedy/1961-08 | 5.00 | 0/34 |
| 1002 baseline | signed/SurfRock/1961-08 | 1334.00 | 0/6 |
| 1002 baseline | signed/LatinPop/1961-08 | 0.00 | 13/13 |
| 1002 baseline | unsigned/LatinPop/1961-09 | 0.00 | 133/133 |
| 1002 baseline | unsigned/Classical/1961-09 | 0.00 | 254/254 |
| 1002 baseline | unsigned/Comedy/1961-09 | 5.00 | 0/225 |
| 1002 baseline | unsigned/Childrens/1961-09 | 5.00 | 0/150 |
| 1002 baseline | unsigned/TexMex/1961-09 | 0.00 | 90/90 |
| 1002 baseline | unsigned/SurfRock/1961-09 | 1336.00 | 0/75 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-09 | 518.00 | 0/44 |
| 1002 baseline | signed/Classical/1961-09 | 0.00 | 63/63 |
| 1002 baseline | signed/Childrens/1961-09 | 5.00 | 0/25 |
| 1002 baseline | signed/Comedy/1961-09 | 5.00 | 0/35 |
| 1002 baseline | signed/SurfRock/1961-09 | 1336.00 | 0/7 |
| 1002 baseline | signed/LatinPop/1961-09 | 0.00 | 15/15 |
| 1002 baseline | signed/TexMex/1961-09 | 0.00 | 13/13 |
| 1002 baseline | unsigned/LatinPop/1961-10 | 0.00 | 130/130 |
| 1002 baseline | unsigned/Classical/1961-10 | 0.00 | 249/249 |
| 1002 baseline | unsigned/Comedy/1961-10 | 4.00 | 0/220 |
| 1002 baseline | unsigned/Childrens/1961-10 | 6.00 | 0/148 |
| 1002 baseline | unsigned/TexMex/1961-10 | 0.00 | 90/90 |
| 1002 baseline | unsigned/SurfRock/1961-10 | 1338.00 | 0/78 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-10 | 518.00 | 0/55 |
| 1002 baseline | signed/Classical/1961-10 | 0.00 | 64/64 |
| 1002 baseline | signed/Childrens/1961-10 | 6.00 | 0/26 |
| 1002 baseline | signed/Comedy/1961-10 | 4.00 | 0/38 |
| 1002 baseline | signed/SurfRock/1961-10 | 1338.00 | 0/8 |
| 1002 baseline | signed/LatinPop/1961-10 | 0.00 | 15/15 |
| 1002 baseline | signed/TexMex/1961-10 | 0.00 | 14/14 |
| 1002 baseline | signed/ContemporaryFolk/1961-10 | 518.00 | 0/1 |
| 1002 baseline | signed/ContemporaryFolk/1960-63 | 531.53 | 0/954 |
| 1002 baseline | unsigned/LatinPop/1961-11 | 0.00 | 128/128 |
| 1002 baseline | unsigned/Classical/1961-11 | 0.00 | 251/251 |
| 1002 baseline | unsigned/Comedy/1961-11 | 4.00 | 0/218 |
| 1002 baseline | unsigned/Childrens/1961-11 | 6.00 | 0/150 |
| 1002 baseline | unsigned/TexMex/1961-11 | 0.00 | 88/88 |
| 1002 baseline | unsigned/SurfRock/1961-11 | 1339.00 | 0/84 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-11 | 518.00 | 0/61 |
| 1002 baseline | signed/Classical/1961-11 | 0.00 | 65/65 |
| 1002 baseline | signed/Childrens/1961-11 | 6.00 | 0/25 |
| 1002 baseline | signed/Comedy/1961-11 | 4.00 | 0/38 |
| 1002 baseline | signed/SurfRock/1961-11 | 1339.00 | 0/8 |
| 1002 baseline | signed/LatinPop/1961-11 | 0.00 | 16/16 |
| 1002 baseline | signed/TexMex/1961-11 | 0.00 | 14/14 |
| 1002 baseline | signed/ContemporaryFolk/1961-11 | 518.00 | 0/1 |
| 1002 baseline | unsigned/LatinPop/1961-12 | 0.00 | 128/128 |
| 1002 baseline | unsigned/Classical/1961-12 | 0.00 | 248/248 |
| 1002 baseline | unsigned/Comedy/1961-12 | 4.00 | 0/219 |
| 1002 baseline | unsigned/Childrens/1961-12 | 6.00 | 0/147 |
| 1002 baseline | unsigned/TexMex/1961-12 | 0.00 | 92/92 |
| 1002 baseline | unsigned/SurfRock/1961-12 | 1341.00 | 0/89 |
| 1002 baseline | unsigned/ContemporaryFolk/1961-12 | 519.00 | 0/71 |
| 1002 baseline | signed/Classical/1961-12 | 0.00 | 68/68 |
| 1002 baseline | signed/Childrens/1961-12 | 6.00 | 0/25 |
| 1002 baseline | signed/Comedy/1961-12 | 4.00 | 0/40 |
| 1002 baseline | signed/SurfRock/1961-12 | 1341.00 | 0/9 |
| 1002 baseline | signed/LatinPop/1961-12 | 0.00 | 16/16 |
| 1002 baseline | signed/TexMex/1961-12 | 0.00 | 15/15 |
| 1002 baseline | signed/ContemporaryFolk/1961-12 | 519.00 | 0/1 |
| 1002 baseline | unsigned/LatinPop/1962-01 | 0.00 | 130/130 |
| 1002 baseline | unsigned/Classical/1962-01 | 0.00 | 248/248 |
| 1002 baseline | unsigned/Comedy/1962-01 | 4.00 | 0/219 |
| 1002 baseline | unsigned/Childrens/1962-01 | 6.00 | 0/152 |
| 1002 baseline | unsigned/TexMex/1962-01 | 0.00 | 88/88 |
| 1002 baseline | unsigned/SurfRock/1962-01 | 1344.00 | 0/99 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-01 | 520.00 | 0/72 |
| 1002 baseline | signed/Classical/1962-01 | 0.00 | 64/64 |
| 1002 baseline | signed/Childrens/1962-01 | 6.00 | 0/26 |
| 1002 baseline | signed/Comedy/1962-01 | 4.00 | 0/41 |
| 1002 baseline | signed/SurfRock/1962-01 | 1344.00 | 0/9 |
| 1002 baseline | signed/LatinPop/1962-01 | 0.00 | 18/18 |
| 1002 baseline | signed/TexMex/1962-01 | 0.00 | 16/16 |
| 1002 baseline | signed/ContemporaryFolk/1962-01 | 520.00 | 0/5 |
| 1002 baseline | unsigned/LatinPop/1962-02 | 0.00 | 127/127 |
| 1002 baseline | unsigned/Classical/1962-02 | 0.00 | 240/240 |
| 1002 baseline | unsigned/Comedy/1962-02 | 5.00 | 0/207 |
| 1002 baseline | unsigned/Childrens/1962-02 | 7.00 | 0/147 |
| 1002 baseline | unsigned/TexMex/1962-02 | 0.00 | 87/87 |
| 1002 baseline | unsigned/SurfRock/1962-02 | 1346.00 | 0/99 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-02 | 520.00 | 0/74 |
| 1002 baseline | unsigned/BossaNova/1962-02 | 511.00 | 0/8 |
| 1002 baseline | unsigned/BossaNova/1960-63 | 515.15 | 0/1749 |
| 1002 baseline | signed/Classical/1962-02 | 0.00 | 69/69 |
| 1002 baseline | signed/Childrens/1962-02 | 7.00 | 0/33 |
| 1002 baseline | signed/Comedy/1962-02 | 5.00 | 0/48 |
| 1002 baseline | signed/SurfRock/1962-02 | 1346.00 | 0/16 |
| 1002 baseline | signed/LatinPop/1962-02 | 0.00 | 20/20 |
| 1002 baseline | signed/TexMex/1962-02 | 0.00 | 18/18 |
| 1002 baseline | signed/ContemporaryFolk/1962-02 | 520.00 | 0/8 |
| 1002 baseline | signed/BossaNova/1962-02 | 511.00 | 0/1 |
| 1002 baseline | signed/BossaNova/1960-63 | 515.78 | 0/498 |
| 1002 baseline | unsigned/Classical/1962-03 | 0.00 | 235/235 |
| 1002 baseline | unsigned/LatinPop/1962-03 | 0.00 | 126/126 |
| 1002 baseline | unsigned/Childrens/1962-03 | 8.00 | 0/140 |
| 1002 baseline | unsigned/Comedy/1962-03 | 6.00 | 0/213 |
| 1002 baseline | unsigned/TexMex/1962-03 | 0.00 | 85/85 |
| 1002 baseline | unsigned/SurfRock/1962-03 | 1346.00 | 0/104 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-03 | 521.00 | 0/80 |
| 1002 baseline | unsigned/BossaNova/1962-03 | 511.00 | 0/12 |
| 1002 baseline | signed/Classical/1962-03 | 0.00 | 69/69 |
| 1002 baseline | signed/Comedy/1962-03 | 6.00 | 0/53 |
| 1002 baseline | signed/SurfRock/1962-03 | 1346.00 | 0/20 |
| 1002 baseline | signed/LatinPop/1962-03 | 0.00 | 20/20 |
| 1002 baseline | signed/Childrens/1962-03 | 8.00 | 0/38 |
| 1002 baseline | signed/TexMex/1962-03 | 0.00 | 18/18 |
| 1002 baseline | signed/ContemporaryFolk/1962-03 | 521.00 | 0/9 |
| 1002 baseline | unsigned/Classical/1962-04 | 0.00 | 233/233 |
| 1002 baseline | unsigned/LatinPop/1962-04 | 0.00 | 123/123 |
| 1002 baseline | unsigned/Childrens/1962-04 | 8.00 | 0/146 |
| 1002 baseline | unsigned/Comedy/1962-04 | 6.00 | 0/201 |
| 1002 baseline | unsigned/TexMex/1962-04 | 0.00 | 88/88 |
| 1002 baseline | unsigned/SurfRock/1962-04 | 1349.00 | 0/110 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-04 | 521.00 | 0/85 |
| 1002 baseline | unsigned/BossaNova/1962-04 | 511.00 | 0/17 |
| 1002 baseline | signed/Classical/1962-04 | 0.00 | 68/68 |
| 1002 baseline | signed/Comedy/1962-04 | 6.00 | 0/60 |
| 1002 baseline | signed/SurfRock/1962-04 | 1349.00 | 0/21 |
| 1002 baseline | signed/LatinPop/1962-04 | 0.00 | 22/22 |
| 1002 baseline | signed/Childrens/1962-04 | 8.00 | 0/40 |
| 1002 baseline | signed/TexMex/1962-04 | 0.00 | 20/20 |
| 1002 baseline | signed/ContemporaryFolk/1962-04 | 521.00 | 0/12 |
| 1002 baseline | signed/BossaNova/1962-04 | 511.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1962-05 | 0.00 | 230/230 |
| 1002 baseline | unsigned/LatinPop/1962-05 | 0.00 | 128/128 |
| 1002 baseline | unsigned/Childrens/1962-05 | 8.00 | 0/143 |
| 1002 baseline | unsigned/Comedy/1962-05 | 6.00 | 0/198 |
| 1002 baseline | unsigned/TexMex/1962-05 | 0.00 | 86/86 |
| 1002 baseline | unsigned/SurfRock/1962-05 | 1350.00 | 0/106 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-05 | 522.00 | 0/92 |
| 1002 baseline | unsigned/BossaNova/1962-05 | 511.00 | 0/25 |
| 1002 baseline | signed/Classical/1962-05 | 0.00 | 66/66 |
| 1002 baseline | signed/Comedy/1962-05 | 6.00 | 0/62 |
| 1002 baseline | signed/SurfRock/1962-05 | 1350.00 | 0/26 |
| 1002 baseline | signed/LatinPop/1962-05 | 0.00 | 22/22 |
| 1002 baseline | signed/Childrens/1962-05 | 8.00 | 0/41 |
| 1002 baseline | signed/TexMex/1962-05 | 0.00 | 22/22 |
| 1002 baseline | signed/ContemporaryFolk/1962-05 | 522.00 | 0/13 |
| 1002 baseline | signed/BossaNova/1962-05 | 511.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1962-06 | 0.00 | 228/228 |
| 1002 baseline | unsigned/Childrens/1962-06 | 8.00 | 0/149 |
| 1002 baseline | unsigned/Comedy/1962-06 | 6.00 | 0/196 |
| 1002 baseline | unsigned/LatinPop/1962-06 | 0.00 | 126/126 |
| 1002 baseline | unsigned/SurfRock/1962-06 | 1350.00 | 0/123 |
| 1002 baseline | unsigned/TexMex/1962-06 | 0.00 | 90/90 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-06 | 523.00 | 0/99 |
| 1002 baseline | unsigned/BossaNova/1962-06 | 512.00 | 0/35 |
| 1002 baseline | signed/Classical/1962-06 | 0.00 | 66/66 |
| 1002 baseline | signed/LatinPop/1962-06 | 0.00 | 24/24 |
| 1002 baseline | signed/Comedy/1962-06 | 6.00 | 0/62 |
| 1002 baseline | signed/Childrens/1962-06 | 8.00 | 0/39 |
| 1002 baseline | signed/TexMex/1962-06 | 0.00 | 21/21 |
| 1002 baseline | signed/SurfRock/1962-06 | 1350.00 | 0/24 |
| 1002 baseline | signed/ContemporaryFolk/1962-06 | 523.00 | 0/14 |
| 1002 baseline | signed/BossaNova/1962-06 | 512.00 | 0/1 |
| 1002 baseline | unsigned/Classical/1962-07 | 1.00 | 0/146 |
| 1002 baseline | unsigned/Childrens/1962-07 | 9.00 | 0/141 |
| 1002 baseline | unsigned/LatinPop/1962-07 | 0.00 | 115/115 |
| 1002 baseline | unsigned/Comedy/1962-07 | 7.00 | 0/175 |
| 1002 baseline | unsigned/SurfRock/1962-07 | 1350.00 | 0/129 |
| 1002 baseline | unsigned/TexMex/1962-07 | 0.00 | 88/88 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-07 | 523.00 | 0/104 |
| 1002 baseline | unsigned/BossaNova/1962-07 | 512.00 | 0/45 |
| 1002 baseline | signed/Classical/1962-07 | 1.00 | 0/72 |
| 1002 baseline | signed/Comedy/1962-07 | 7.00 | 0/67 |
| 1002 baseline | signed/Childrens/1962-07 | 9.00 | 0/44 |
| 1002 baseline | signed/TexMex/1962-07 | 0.00 | 21/21 |
| 1002 baseline | signed/LatinPop/1962-07 | 0.00 | 27/27 |
| 1002 baseline | signed/SurfRock/1962-07 | 1350.00 | 0/29 |
| 1002 baseline | signed/ContemporaryFolk/1962-07 | 523.00 | 0/19 |
| 1002 baseline | signed/BossaNova/1962-07 | 512.00 | 0/2 |
| 1002 baseline | unsigned/Classical/1962-08 | 1.00 | 0/153 |
| 1002 baseline | unsigned/Childrens/1962-08 | 9.00 | 0/146 |
| 1002 baseline | unsigned/LatinPop/1962-08 | 0.00 | 111/111 |
| 1002 baseline | unsigned/Comedy/1962-08 | 7.00 | 0/178 |
| 1002 baseline | unsigned/SurfRock/1962-08 | 1350.00 | 0/130 |
| 1002 baseline | unsigned/TexMex/1962-08 | 0.00 | 95/95 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-08 | 523.00 | 0/107 |
| 1002 baseline | unsigned/BossaNova/1962-08 | 512.00 | 0/51 |
| 1002 baseline | signed/Classical/1962-08 | 1.00 | 0/72 |
| 1002 baseline | signed/Comedy/1962-08 | 7.00 | 0/67 |
| 1002 baseline | signed/Childrens/1962-08 | 9.00 | 0/45 |
| 1002 baseline | signed/TexMex/1962-08 | 0.00 | 22/22 |
| 1002 baseline | signed/LatinPop/1962-08 | 0.00 | 29/29 |
| 1002 baseline | signed/SurfRock/1962-08 | 1350.00 | 0/35 |
| 1002 baseline | signed/ContemporaryFolk/1962-08 | 523.00 | 0/21 |
| 1002 baseline | signed/BossaNova/1962-08 | 512.00 | 0/2 |
| 1002 baseline | unsigned/Classical/1962-09 | 2.00 | 0/162 |
| 1002 baseline | unsigned/Childrens/1962-09 | 9.00 | 0/148 |
| 1002 baseline | unsigned/LatinPop/1962-09 | 0.00 | 110/110 |
| 1002 baseline | unsigned/Comedy/1962-09 | 7.00 | 0/176 |
| 1002 baseline | unsigned/SurfRock/1962-09 | 1354.00 | 0/132 |
| 1002 baseline | unsigned/TexMex/1962-09 | 0.00 | 91/91 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-09 | 524.00 | 0/110 |
| 1002 baseline | unsigned/BossaNova/1962-09 | 513.00 | 0/52 |
| 1002 baseline | signed/Classical/1962-09 | 2.00 | 0/69 |
| 1002 baseline | signed/Comedy/1962-09 | 7.00 | 0/68 |
| 1002 baseline | signed/Childrens/1962-09 | 9.00 | 0/54 |
| 1002 baseline | signed/LatinPop/1962-09 | 0.00 | 37/37 |
| 1002 baseline | signed/TexMex/1962-09 | 0.00 | 30/30 |
| 1002 baseline | signed/SurfRock/1962-09 | 1354.00 | 0/43 |
| 1002 baseline | signed/ContemporaryFolk/1962-09 | 524.00 | 0/23 |
| 1002 baseline | signed/BossaNova/1962-09 | 513.00 | 0/6 |
| 1002 baseline | unsigned/Classical/1962-10 | 2.00 | 0/170 |
| 1002 baseline | unsigned/Childrens/1962-10 | 9.00 | 0/158 |
| 1002 baseline | unsigned/LatinPop/1962-10 | 0.00 | 110/110 |
| 1002 baseline | unsigned/Comedy/1962-10 | 7.00 | 0/190 |
| 1002 baseline | unsigned/SurfRock/1962-10 | 1355.00 | 0/140 |
| 1002 baseline | unsigned/TexMex/1962-10 | 0.00 | 96/96 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-10 | 524.00 | 0/112 |
| 1002 baseline | unsigned/BossaNova/1962-10 | 513.00 | 0/61 |
| 1002 baseline | signed/Classical/1962-10 | 2.00 | 0/65 |
| 1002 baseline | signed/Comedy/1962-10 | 7.00 | 0/70 |
| 1002 baseline | signed/LatinPop/1962-10 | 0.00 | 36/36 |
| 1002 baseline | signed/Childrens/1962-10 | 9.00 | 0/54 |
| 1002 baseline | signed/TexMex/1962-10 | 0.00 | 31/31 |
| 1002 baseline | signed/SurfRock/1962-10 | 1355.00 | 0/45 |
| 1002 baseline | signed/ContemporaryFolk/1962-10 | 524.00 | 0/25 |
| 1002 baseline | signed/BossaNova/1962-10 | 513.00 | 0/6 |
| 1002 baseline | unsigned/Classical/1962-11 | 2.00 | 0/170 |
| 1002 baseline | unsigned/Childrens/1962-11 | 9.00 | 0/169 |
| 1002 baseline | unsigned/LatinPop/1962-11 | 0.00 | 119/119 |
| 1002 baseline | unsigned/Comedy/1962-11 | 7.00 | 0/199 |
| 1002 baseline | unsigned/SurfRock/1962-11 | 1355.00 | 0/142 |
| 1002 baseline | unsigned/TexMex/1962-11 | 0.00 | 96/96 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-11 | 525.00 | 0/111 |
| 1002 baseline | unsigned/BossaNova/1962-11 | 514.00 | 0/72 |
| 1002 baseline | signed/Classical/1962-11 | 2.00 | 0/66 |
| 1002 baseline | signed/Comedy/1962-11 | 7.00 | 0/71 |
| 1002 baseline | signed/LatinPop/1962-11 | 0.00 | 36/36 |
| 1002 baseline | signed/Childrens/1962-11 | 9.00 | 0/52 |
| 1002 baseline | signed/TexMex/1962-11 | 0.00 | 33/33 |
| 1002 baseline | signed/SurfRock/1962-11 | 1355.00 | 0/48 |
| 1002 baseline | signed/ContemporaryFolk/1962-11 | 525.00 | 0/29 |
| 1002 baseline | signed/BossaNova/1962-11 | 514.00 | 0/11 |
| 1002 baseline | unsigned/Classical/1962-12 | 2.00 | 0/177 |
| 1002 baseline | unsigned/Childrens/1962-12 | 9.00 | 0/177 |
| 1002 baseline | unsigned/LatinPop/1962-12 | 0.00 | 126/126 |
| 1002 baseline | unsigned/Comedy/1962-12 | 7.00 | 0/199 |
| 1002 baseline | unsigned/SurfRock/1962-12 | 1358.00 | 0/142 |
| 1002 baseline | unsigned/TexMex/1962-12 | 0.00 | 90/90 |
| 1002 baseline | unsigned/ContemporaryFolk/1962-12 | 526.00 | 0/110 |
| 1002 baseline | unsigned/BossaNova/1962-12 | 514.00 | 0/74 |
| 1002 baseline | signed/Childrens/1962-12 | 9.00 | 0/53 |
| 1002 baseline | signed/Classical/1962-12 | 2.00 | 0/66 |
| 1002 baseline | signed/Comedy/1962-12 | 7.00 | 0/81 |
| 1002 baseline | signed/LatinPop/1962-12 | 0.00 | 41/41 |
| 1002 baseline | signed/TexMex/1962-12 | 0.00 | 38/38 |
| 1002 baseline | signed/SurfRock/1962-12 | 1358.00 | 0/57 |
| 1002 baseline | signed/ContemporaryFolk/1962-12 | 526.00 | 0/34 |
| 1002 baseline | signed/BossaNova/1962-12 | 514.00 | 0/15 |
| 1002 baseline | unsigned/Classical/1963-01 | 3.00 | 0/187 |
| 1002 baseline | unsigned/Comedy/1963-01 | 7.00 | 0/206 |
| 1002 baseline | unsigned/Childrens/1963-01 | 9.00 | 0/180 |
| 1002 baseline | unsigned/LatinPop/1963-01 | 0.00 | 129/129 |
| 1002 baseline | unsigned/SurfRock/1963-01 | 1358.00 | 0/145 |
| 1002 baseline | unsigned/TexMex/1963-01 | 0.00 | 91/91 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-01 | 526.00 | 0/113 |
| 1002 baseline | unsigned/BossaNova/1963-01 | 514.00 | 0/75 |
| 1002 baseline | signed/Childrens/1963-01 | 9.00 | 0/56 |
| 1002 baseline | signed/Classical/1963-01 | 3.00 | 0/67 |
| 1002 baseline | signed/Comedy/1963-01 | 7.00 | 0/89 |
| 1002 baseline | signed/LatinPop/1963-01 | 0.00 | 43/43 |
| 1002 baseline | signed/TexMex/1963-01 | 0.00 | 38/38 |
| 1002 baseline | signed/SurfRock/1963-01 | 1358.00 | 0/61 |
| 1002 baseline | signed/ContemporaryFolk/1963-01 | 526.00 | 0/39 |
| 1002 baseline | signed/BossaNova/1963-01 | 514.00 | 0/19 |
| 1002 baseline | unsigned/Classical/1963-02 | 3.00 | 0/192 |
| 1002 baseline | unsigned/Comedy/1963-02 | 8.00 | 0/207 |
| 1002 baseline | unsigned/Childrens/1963-02 | 10.00 | 0/182 |
| 1002 baseline | unsigned/LatinPop/1963-02 | 0.00 | 134/134 |
| 1002 baseline | unsigned/SurfRock/1963-02 | 1360.00 | 0/149 |
| 1002 baseline | unsigned/TexMex/1963-02 | 0.00 | 93/93 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-02 | 529.00 | 0/106 |
| 1002 baseline | unsigned/BossaNova/1963-02 | 516.00 | 0/83 |
| 1002 baseline | signed/Childrens/1963-02 | 10.00 | 0/58 |
| 1002 baseline | signed/Classical/1963-02 | 3.00 | 0/64 |
| 1002 baseline | signed/Comedy/1963-02 | 8.00 | 0/99 |
| 1002 baseline | signed/LatinPop/1963-02 | 0.00 | 46/46 |
| 1002 baseline | signed/TexMex/1963-02 | 0.00 | 40/40 |
| 1002 baseline | signed/SurfRock/1963-02 | 1360.00 | 0/68 |
| 1002 baseline | signed/ContemporaryFolk/1963-02 | 529.00 | 0/48 |
| 1002 baseline | signed/BossaNova/1963-02 | 516.00 | 0/21 |
| 1002 baseline | unsigned/LatinPop/1963-03 | 0.00 | 142/142 |
| 1002 baseline | unsigned/Classical/1963-03 | 3.00 | 0/201 |
| 1002 baseline | unsigned/Comedy/1963-03 | 8.00 | 0/203 |
| 1002 baseline | unsigned/Childrens/1963-03 | 10.00 | 0/182 |
| 1002 baseline | unsigned/SurfRock/1963-03 | 1361.00 | 0/156 |
| 1002 baseline | unsigned/TexMex/1963-03 | 0.00 | 97/97 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-03 | 529.00 | 0/101 |
| 1002 baseline | unsigned/BossaNova/1963-03 | 516.00 | 0/87 |
| 1002 baseline | signed/Childrens/1963-03 | 10.00 | 0/64 |
| 1002 baseline | signed/Classical/1963-03 | 3.00 | 0/64 |
| 1002 baseline | signed/Comedy/1963-03 | 8.00 | 0/103 |
| 1002 baseline | signed/LatinPop/1963-03 | 0.00 | 51/51 |
| 1002 baseline | signed/TexMex/1963-03 | 0.00 | 42/42 |
| 1002 baseline | signed/SurfRock/1963-03 | 1361.00 | 0/68 |
| 1002 baseline | signed/ContemporaryFolk/1963-03 | 529.00 | 0/55 |
| 1002 baseline | signed/BossaNova/1963-03 | 516.00 | 0/27 |
| 1002 baseline | unsigned/LatinPop/1963-04 | 0.00 | 141/141 |
| 1002 baseline | unsigned/Classical/1963-04 | 3.00 | 0/207 |
| 1002 baseline | unsigned/Comedy/1963-04 | 8.00 | 0/210 |
| 1002 baseline | unsigned/Childrens/1963-04 | 10.00 | 0/185 |
| 1002 baseline | unsigned/SurfRock/1963-04 | 1361.00 | 0/150 |
| 1002 baseline | unsigned/TexMex/1963-04 | 0.00 | 97/97 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-04 | 531.00 | 0/93 |
| 1002 baseline | unsigned/BossaNova/1963-04 | 516.00 | 0/90 |
| 1002 baseline | signed/Childrens/1963-04 | 10.00 | 0/70 |
| 1002 baseline | signed/Classical/1963-04 | 3.00 | 0/65 |
| 1002 baseline | signed/Comedy/1963-04 | 8.00 | 0/110 |
| 1002 baseline | signed/LatinPop/1963-04 | 0.00 | 56/56 |
| 1002 baseline | signed/TexMex/1963-04 | 0.00 | 48/48 |
| 1002 baseline | signed/SurfRock/1963-04 | 1361.00 | 0/79 |
| 1002 baseline | signed/ContemporaryFolk/1963-04 | 531.00 | 0/66 |
| 1002 baseline | signed/BossaNova/1963-04 | 516.00 | 0/35 |
| 1002 baseline | unsigned/LatinPop/1963-05 | 0.00 | 143/143 |
| 1002 baseline | unsigned/Comedy/1963-05 | 8.00 | 0/205 |
| 1002 baseline | unsigned/Classical/1963-05 | 3.00 | 0/209 |
| 1002 baseline | unsigned/Childrens/1963-05 | 10.00 | 0/195 |
| 1002 baseline | unsigned/SurfRock/1963-05 | 1362.00 | 0/167 |
| 1002 baseline | unsigned/TexMex/1963-05 | 0.00 | 103/103 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-05 | 532.00 | 0/94 |
| 1002 baseline | unsigned/BossaNova/1963-05 | 516.00 | 0/104 |
| 1002 baseline | signed/Classical/1963-05 | 3.00 | 0/66 |
| 1002 baseline | signed/Childrens/1963-05 | 10.00 | 0/72 |
| 1002 baseline | signed/Comedy/1963-05 | 8.00 | 0/115 |
| 1002 baseline | signed/LatinPop/1963-05 | 0.00 | 59/59 |
| 1002 baseline | signed/TexMex/1963-05 | 0.00 | 49/49 |
| 1002 baseline | signed/SurfRock/1963-05 | 1362.00 | 0/80 |
| 1002 baseline | signed/ContemporaryFolk/1963-05 | 532.00 | 0/67 |
| 1002 baseline | signed/BossaNova/1963-05 | 516.00 | 0/36 |
| 1002 baseline | unsigned/LatinPop/1963-06 | 0.00 | 144/144 |
| 1002 baseline | unsigned/Comedy/1963-06 | 9.00 | 0/213 |
| 1002 baseline | unsigned/Classical/1963-06 | 3.00 | 0/212 |
| 1002 baseline | unsigned/Childrens/1963-06 | 11.00 | 0/197 |
| 1002 baseline | unsigned/SurfRock/1963-06 | 1365.00 | 0/163 |
| 1002 baseline | unsigned/TexMex/1963-06 | 0.00 | 109/109 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-06 | 533.00 | 0/95 |
| 1002 baseline | unsigned/BossaNova/1963-06 | 516.00 | 0/114 |
| 1002 baseline | signed/Classical/1963-06 | 3.00 | 0/72 |
| 1002 baseline | signed/Childrens/1963-06 | 11.00 | 0/79 |
| 1002 baseline | signed/Comedy/1963-06 | 9.00 | 0/116 |
| 1002 baseline | signed/LatinPop/1963-06 | 0.00 | 66/66 |
| 1002 baseline | signed/TexMex/1963-06 | 0.00 | 50/50 |
| 1002 baseline | signed/SurfRock/1963-06 | 1365.00 | 0/94 |
| 1002 baseline | signed/ContemporaryFolk/1963-06 | 533.00 | 0/66 |
| 1002 baseline | signed/BossaNova/1963-06 | 516.00 | 0/40 |
| 1002 baseline | unsigned/LatinPop/1963-07 | 0.00 | 177/177 |
| 1002 baseline | unsigned/Comedy/1963-07 | 9.00 | 0/239 |
| 1002 baseline | unsigned/Classical/1963-07 | 3.00 | 0/299 |
| 1002 baseline | unsigned/Childrens/1963-07 | 11.00 | 0/222 |
| 1002 baseline | unsigned/SurfRock/1963-07 | 1366.00 | 0/172 |
| 1002 baseline | unsigned/TexMex/1963-07 | 0.00 | 121/121 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-07 | 536.00 | 0/103 |
| 1002 baseline | unsigned/BossaNova/1963-07 | 516.00 | 0/115 |
| 1002 baseline | signed/Classical/1963-07 | 3.00 | 0/72 |
| 1002 baseline | signed/Childrens/1963-07 | 11.00 | 0/75 |
| 1002 baseline | signed/Comedy/1963-07 | 9.00 | 0/118 |
| 1002 baseline | signed/LatinPop/1963-07 | 0.00 | 69/69 |
| 1002 baseline | signed/TexMex/1963-07 | 0.00 | 50/50 |
| 1002 baseline | signed/SurfRock/1963-07 | 1366.00 | 0/94 |
| 1002 baseline | signed/ContemporaryFolk/1963-07 | 536.00 | 0/66 |
| 1002 baseline | signed/BossaNova/1963-07 | 516.00 | 0/44 |
| 1002 baseline | unsigned/LatinPop/1963-08 | 0.00 | 188/188 |
| 1002 baseline | unsigned/Comedy/1963-08 | 9.00 | 0/251 |
| 1002 baseline | unsigned/Classical/1963-08 | 3.00 | 0/303 |
| 1002 baseline | unsigned/Childrens/1963-08 | 11.00 | 0/230 |
| 1002 baseline | unsigned/SurfRock/1963-08 | 1366.00 | 0/185 |
| 1002 baseline | unsigned/TexMex/1963-08 | 0.00 | 135/135 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-08 | 536.00 | 0/111 |
| 1002 baseline | unsigned/BossaNova/1963-08 | 516.00 | 0/122 |
| 1002 baseline | signed/Classical/1963-08 | 3.00 | 0/71 |
| 1002 baseline | signed/Childrens/1963-08 | 11.00 | 0/75 |
| 1002 baseline | signed/Comedy/1963-08 | 9.00 | 0/115 |
| 1002 baseline | signed/LatinPop/1963-08 | 0.00 | 69/69 |
| 1002 baseline | signed/TexMex/1963-08 | 0.00 | 49/49 |
| 1002 baseline | signed/SurfRock/1963-08 | 1366.00 | 0/92 |
| 1002 baseline | signed/ContemporaryFolk/1963-08 | 536.00 | 0/65 |
| 1002 baseline | signed/BossaNova/1963-08 | 516.00 | 0/43 |
| 1002 baseline | unsigned/LatinPop/1963-09 | 0.00 | 190/190 |
| 1002 baseline | unsigned/Comedy/1963-09 | 9.00 | 0/269 |
| 1002 baseline | unsigned/Classical/1963-09 | 3.00 | 0/304 |
| 1002 baseline | unsigned/Childrens/1963-09 | 11.00 | 0/235 |
| 1002 baseline | unsigned/SurfRock/1963-09 | 1366.00 | 0/204 |
| 1002 baseline | unsigned/TexMex/1963-09 | 0.00 | 142/142 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-09 | 537.00 | 0/122 |
| 1002 baseline | unsigned/BossaNova/1963-09 | 516.00 | 0/124 |
| 1002 baseline | signed/Classical/1963-09 | 3.00 | 0/72 |
| 1002 baseline | signed/Childrens/1963-09 | 11.00 | 0/76 |
| 1002 baseline | signed/Comedy/1963-09 | 9.00 | 0/114 |
| 1002 baseline | signed/LatinPop/1963-09 | 0.00 | 73/73 |
| 1002 baseline | signed/TexMex/1963-09 | 0.00 | 50/50 |
| 1002 baseline | signed/SurfRock/1963-09 | 1366.00 | 0/89 |
| 1002 baseline | signed/ContemporaryFolk/1963-09 | 537.00 | 0/68 |
| 1002 baseline | signed/BossaNova/1963-09 | 516.00 | 0/44 |
| 1002 baseline | unsigned/LatinPop/1963-10 | 0.00 | 201/201 |
| 1002 baseline | unsigned/Comedy/1963-10 | 9.00 | 0/291 |
| 1002 baseline | unsigned/Classical/1963-10 | 3.00 | 0/309 |
| 1002 baseline | unsigned/Childrens/1963-10 | 11.00 | 0/252 |
| 1002 baseline | unsigned/SurfRock/1963-10 | 1366.00 | 0/213 |
| 1002 baseline | unsigned/TexMex/1963-10 | 0.00 | 151/151 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-10 | 537.00 | 0/135 |
| 1002 baseline | unsigned/BossaNova/1963-10 | 516.00 | 0/125 |
| 1002 baseline | signed/Classical/1963-10 | 3.00 | 0/73 |
| 1002 baseline | signed/Childrens/1963-10 | 11.00 | 0/70 |
| 1002 baseline | signed/Comedy/1963-10 | 9.00 | 0/105 |
| 1002 baseline | signed/LatinPop/1963-10 | 0.00 | 69/69 |
| 1002 baseline | signed/TexMex/1963-10 | 0.00 | 50/50 |
| 1002 baseline | signed/SurfRock/1963-10 | 1366.00 | 0/92 |
| 1002 baseline | signed/ContemporaryFolk/1963-10 | 537.00 | 0/67 |
| 1002 baseline | signed/BossaNova/1963-10 | 516.00 | 0/45 |
| 1002 baseline | unsigned/LatinPop/1963-11 | 0.00 | 204/204 |
| 1002 baseline | unsigned/Comedy/1963-11 | 10.00 | 0/305 |
| 1002 baseline | unsigned/Classical/1963-11 | 3.00 | 0/315 |
| 1002 baseline | unsigned/Childrens/1963-11 | 12.00 | 0/262 |
| 1002 baseline | unsigned/SurfRock/1963-11 | 1368.00 | 0/230 |
| 1002 baseline | unsigned/TexMex/1963-11 | 0.00 | 157/157 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-11 | 537.00 | 0/147 |
| 1002 baseline | unsigned/BossaNova/1963-11 | 516.00 | 0/127 |
| 1002 baseline | signed/Classical/1963-11 | 3.00 | 0/75 |
| 1002 baseline | signed/Childrens/1963-11 | 12.00 | 0/67 |
| 1002 baseline | signed/Comedy/1963-11 | 10.00 | 0/103 |
| 1002 baseline | signed/LatinPop/1963-11 | 0.00 | 68/68 |
| 1002 baseline | signed/TexMex/1963-11 | 0.00 | 48/48 |
| 1002 baseline | signed/SurfRock/1963-11 | 1368.00 | 0/89 |
| 1002 baseline | signed/ContemporaryFolk/1963-11 | 537.00 | 0/62 |
| 1002 baseline | signed/BossaNova/1963-11 | 516.00 | 0/49 |
| 1002 baseline | unsigned/LatinPop/1963-12 | 0.00 | 209/209 |
| 1002 baseline | unsigned/Comedy/1963-12 | 10.00 | 0/320 |
| 1002 baseline | unsigned/Childrens/1963-12 | 12.00 | 0/265 |
| 1002 baseline | unsigned/Classical/1963-12 | 3.00 | 0/318 |
| 1002 baseline | unsigned/TexMex/1963-12 | 0.00 | 162/162 |
| 1002 baseline | unsigned/SurfRock/1963-12 | 1370.00 | 0/238 |
| 1002 baseline | unsigned/ContemporaryFolk/1963-12 | 538.00 | 0/155 |
| 1002 baseline | unsigned/BossaNova/1963-12 | 517.00 | 0/131 |
| 1002 baseline | signed/Classical/1963-12 | 3.00 | 0/82 |
| 1002 baseline | signed/Comedy/1963-12 | 10.00 | 0/99 |
| 1002 baseline | signed/LatinPop/1963-12 | 0.00 | 77/77 |
| 1002 baseline | signed/TexMex/1963-12 | 0.00 | 49/49 |
| 1002 baseline | signed/Childrens/1963-12 | 12.00 | 0/71 |
| 1002 baseline | signed/SurfRock/1963-12 | 1370.00 | 0/91 |
| 1002 baseline | signed/ContemporaryFolk/1963-12 | 538.00 | 0/70 |
| 1002 baseline | signed/BossaNova/1963-12 | 517.00 | 0/49 |

## Calibration bands, annual observations and holdout
The bands are unchanged from the directive, stored once in the editable repertoire table, and never read by selection as quotas. Annual and aggregate rows include cohort numerators/denominators. A distance of zero means inside the provisional band, not historical validation. Empty cohorts remain unmeasured. Composer/interpreter and Brazilian-cohort labels are songwriting proxies, not archival labels; instrumental cohorts require known active performers and no vocalist. The starting worlds have no known instrumental cohorts, so they cannot independently validate the instrumental targets.

| Run | Population | Genre / cohort | Period | Numerator / slots | Share | Band | Distance |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 baseline | signed | RnB / mixedContemporary | 1960 | 0/20881 | 0.00% | 5–25% | 5.00% |
| 1001 baseline | signed | RnB / mixedContemporary | 1960-63 | 6802/71855 | 9.47% | 5–25% | 0.00% |
| 1001 baseline | signed | Folk / traditionalRevival | 1960 | 6324/8561 | 73.87% | 50–90% | 0.00% |
| 1001 baseline | signed | Folk / traditionalRevival | 1960-63 | 24575/33686 | 72.95% | 50–90% | 0.00% |
| 1001 baseline | signed | Country / mixedContemporary | 1960 | 5342/9716 | 54.98% | 20–50% | 4.98% |
| 1001 baseline | signed | Country / mixedContemporary | 1960-63 | 33153/49909 | 66.43% | 20–50% | 16.43% |
| 1001 baseline | signed | Jazz / standardsInterpreter | 1960 | 2105/4582 | 45.94% | 60–90% | 14.06% |
| 1001 baseline | signed | Jazz / standardsInterpreter | 1960-63 | 9273/18771 | 49.40% | 60–90% | 10.60% |
| 1001 baseline | signed | Gospel / inheritedRepertoire | 1960 | 1138/2593 | 43.89% | 50–90% | 6.11% |
| 1001 baseline | signed | Gospel / inheritedRepertoire | 1960-63 | 6790/14296 | 47.50% | 50–90% | 2.50% |
| 1001 baseline | signed | Jazz / composerLed | 1960 | 331/1172 | 28.24% | 10–50% | 0.00% |
| 1001 baseline | signed | Jazz / composerLed | 1960-63 | 1864/5656 | 32.96% | 10–50% | 0.00% |
| 1001 baseline | unsigned | Folk / traditionalRevival | 1960 | 1917/2608 | 73.50% | 50–90% | 0.00% |
| 1001 baseline | unsigned | Folk / traditionalRevival | 1960-63 | 43623/57548 | 75.80% | 50–90% | 0.00% |
| 1001 baseline | unsigned | Country / mixedContemporary | 1960 | 3575/6528 | 54.76% | 20–50% | 4.76% |
| 1001 baseline | unsigned | Country / mixedContemporary | 1960-63 | 71561/103088 | 69.42% | 20–50% | 19.42% |
| 1001 baseline | unsigned | RnB / mixedContemporary | 1960 | 0/2379 | 0.00% | 5–25% | 5.00% |
| 1001 baseline | unsigned | RnB / mixedContemporary | 1960-63 | 12848/97089 | 13.23% | 5–25% | 0.00% |
| 1001 baseline | unsigned | Gospel / inheritedRepertoire | 1960 | 1011/2210 | 45.75% | 50–90% | 4.25% |
| 1001 baseline | unsigned | Gospel / inheritedRepertoire | 1960-63 | 16189/32575 | 49.70% | 50–90% | 0.30% |
| 1001 baseline | unsigned | Jazz / standardsInterpreter | 1960 | 836/1850 | 45.19% | 60–90% | 14.81% |
| 1001 baseline | unsigned | Jazz / standardsInterpreter | 1960-63 | 20329/40384 | 50.34% | 60–90% | 9.66% |
| 1001 baseline | unsigned | Jazz / composerLed | 1960 | 130/433 | 30.02% | 10–50% | 0.00% |
| 1001 baseline | unsigned | Jazz / composerLed | 1960-63 | 2944/8082 | 36.43% | 10–50% | 0.00% |
| 1001 baseline | unsigned | Folk / traditionalRevival | 1961 | 14238/18801 | 75.73% | 50–90% | 0.00% |
| 1001 baseline | unsigned | RnB / mixedContemporary | 1961 | 4440/35083 | 12.66% | 5–25% | 0.00% |
| 1001 baseline | unsigned | Country / mixedContemporary | 1961 | 21338/30586 | 69.76% | 20–50% | 19.76% |
| 1001 baseline | unsigned | Jazz / standardsInterpreter | 1961 | 6695/13044 | 51.33% | 60–90% | 8.67% |
| 1001 baseline | unsigned | Jazz / composerLed | 1961 | 888/2483 | 35.76% | 10–50% | 0.00% |
| 1001 baseline | unsigned | Gospel / inheritedRepertoire | 1961 | 4929/9887 | 49.85% | 50–90% | 0.15% |
| 1001 baseline | signed | RnB / mixedContemporary | 1961 | 2570/19594 | 13.12% | 5–25% | 0.00% |
| 1001 baseline | signed | Folk / traditionalRevival | 1961 | 6167/8375 | 73.64% | 50–90% | 0.00% |
| 1001 baseline | signed | Country / mixedContemporary | 1961 | 7075/10371 | 68.22% | 20–50% | 18.22% |
| 1001 baseline | signed | Jazz / standardsInterpreter | 1961 | 2162/4261 | 50.74% | 60–90% | 9.26% |
| 1001 baseline | signed | Gospel / inheritedRepertoire | 1961 | 1371/2754 | 49.78% | 50–90% | 0.22% |
| 1001 baseline | signed | Jazz / composerLed | 1961 | 577/1626 | 35.49% | 10–50% | 0.00% |
| 1001 baseline | unsigned | RnB / mixedContemporary | 1962 | 3230/27078 | 11.93% | 5–25% | 0.00% |
| 1001 baseline | unsigned | Folk / traditionalRevival | 1962 | 12461/16284 | 76.52% | 50–90% | 0.00% |
| 1001 baseline | unsigned | Jazz / standardsInterpreter | 1962 | 6474/11508 | 56.26% | 60–90% | 3.74% |
| 1001 baseline | unsigned | Jazz / composerLed | 1962 | 982/2524 | 38.91% | 10–50% | 0.00% |
| 1001 baseline | unsigned | Country / mixedContemporary | 1962 | 20272/28969 | 69.98% | 20–50% | 19.98% |
| 1001 baseline | unsigned | Gospel / inheritedRepertoire | 1962 | 4276/9023 | 47.39% | 50–90% | 2.61% |
| 1001 baseline | unsigned | ContemporaryFolk / contemporaryInterpreter | 1962 | 3604/4595 | 78.43% | 10–35% | 43.43% |
| 1001 baseline | unsigned | ContemporaryFolk / contemporaryInterpreter | 1960-63 | 7826/10000 | 78.26% | 10–35% | 43.26% |
| 1001 baseline | signed | RnB / mixedContemporary | 1962 | 2187/18004 | 12.15% | 5–25% | 0.00% |
| 1001 baseline | signed | Folk / traditionalRevival | 1962 | 6199/8666 | 71.53% | 50–90% | 0.00% |
| 1001 baseline | signed | Country / mixedContemporary | 1962 | 8964/13108 | 68.39% | 20–50% | 18.39% |
| 1001 baseline | signed | Gospel / inheritedRepertoire | 1962 | 1693/3787 | 44.71% | 50–90% | 5.29% |
| 1001 baseline | signed | Jazz / composerLed | 1962 | 473/1347 | 35.12% | 10–50% | 0.00% |
| 1001 baseline | signed | Jazz / standardsInterpreter | 1962 | 2805/5122 | 54.76% | 60–90% | 5.24% |
| 1001 baseline | signed | ContemporaryFolk / contemporaryInterpreter | 1962 | 783/1028 | 76.17% | 10–35% | 41.17% |
| 1001 baseline | signed | ContemporaryFolk / contemporaryInterpreter | 1960-63 | 3114/4093 | 76.08% | 10–35% | 41.08% |
| 1001 baseline | unsigned | BossaNova / olderSongbook | 1962 | 1094/1964 | 55.70% | 15–40% | 15.70% |
| 1001 baseline | unsigned | BossaNova / olderSongbook | 1960-63 | 3157/6374 | 49.53% | 15–40% | 9.53% |
| 1001 baseline | signed | BossaNova / olderSongbook | 1962 | 219/372 | 58.87% | 15–40% | 18.87% |
| 1001 baseline | signed | BossaNova / olderSongbook | 1960-63 | 1370/2827 | 48.46% | 15–40% | 8.46% |
| 1001 baseline | unsigned | RnB / mixedContemporary | 1963 | 5178/32549 | 15.91% | 5–25% | 0.00% |
| 1001 baseline | unsigned | Folk / traditionalRevival | 1963 | 15007/19855 | 75.58% | 50–90% | 0.00% |
| 1001 baseline | unsigned | Country / mixedContemporary | 1963 | 26376/37005 | 71.28% | 20–50% | 21.28% |
| 1001 baseline | unsigned | Jazz / standardsInterpreter | 1963 | 6324/13982 | 45.23% | 60–90% | 14.77% |
| 1001 baseline | unsigned | Jazz / composerLed | 1963 | 944/2642 | 35.73% | 10–50% | 0.00% |
| 1001 baseline | unsigned | Gospel / inheritedRepertoire | 1963 | 5973/11455 | 52.14% | 50–90% | 0.00% |
| 1001 baseline | unsigned | BossaNova / olderSongbook | 1963 | 2063/4410 | 46.78% | 15–40% | 6.78% |
| 1001 baseline | unsigned | ContemporaryFolk / contemporaryInterpreter | 1963 | 4222/5405 | 78.11% | 10–35% | 43.11% |
| 1001 baseline | signed | RnB / mixedContemporary | 1963 | 2045/13376 | 15.29% | 5–25% | 0.00% |
| 1001 baseline | signed | Folk / traditionalRevival | 1963 | 5885/8084 | 72.80% | 50–90% | 0.00% |
| 1001 baseline | signed | Country / mixedContemporary | 1963 | 11772/16714 | 70.43% | 20–50% | 20.43% |
| 1001 baseline | signed | Gospel / inheritedRepertoire | 1963 | 2588/5162 | 50.14% | 50–90% | 0.00% |
| 1001 baseline | signed | Jazz / composerLed | 1963 | 483/1511 | 31.97% | 10–50% | 0.00% |
| 1001 baseline | signed | Jazz / standardsInterpreter | 1963 | 2201/4806 | 45.80% | 60–90% | 14.20% |
| 1001 baseline | signed | ContemporaryFolk / contemporaryInterpreter | 1963 | 2331/3065 | 76.05% | 10–35% | 41.05% |
| 1001 baseline | signed | BossaNova / olderSongbook | 1963 | 1151/2455 | 46.88% | 15–40% | 6.88% |
| 1002 baseline | signed | Jazz / standardsInterpreter | 1960 | 3155/5526 | 57.09% | 60–90% | 2.91% |
| 1002 baseline | signed | Jazz / standardsInterpreter | 1960-63 | 12254/19558 | 62.65% | 60–90% | 0.00% |
| 1002 baseline | signed | Country / mixedContemporary | 1960 | 4044/9131 | 44.29% | 20–50% | 0.00% |
| 1002 baseline | signed | Country / mixedContemporary | 1960-63 | 22074/48125 | 45.87% | 20–50% | 0.00% |
| 1002 baseline | signed | RnB / mixedContemporary | 1960 | 0/20321 | 0.00% | 5–25% | 5.00% |
| 1002 baseline | signed | RnB / mixedContemporary | 1960-63 | 3331/71070 | 4.69% | 5–25% | 0.31% |
| 1002 baseline | signed | Folk / traditionalRevival | 1960 | 5541/7535 | 73.54% | 50–90% | 0.00% |
| 1002 baseline | signed | Folk / traditionalRevival | 1960-63 | 22503/30422 | 73.97% | 50–90% | 0.00% |
| 1002 baseline | signed | Gospel / inheritedRepertoire | 1960 | 1235/2094 | 58.98% | 50–90% | 0.00% |
| 1002 baseline | signed | Gospel / inheritedRepertoire | 1960-63 | 6734/13361 | 50.40% | 50–90% | 0.00% |
| 1002 baseline | signed | Jazz / composerLed | 1960 | 632/1580 | 40.00% | 10–50% | 0.00% |
| 1002 baseline | signed | Jazz / composerLed | 1960-63 | 2789/6584 | 42.36% | 10–50% | 0.00% |
| 1002 baseline | unsigned | RnB / mixedContemporary | 1960 | 0/2281 | 0.00% | 5–25% | 5.00% |
| 1002 baseline | unsigned | RnB / mixedContemporary | 1960-63 | 4709/97351 | 4.84% | 5–25% | 0.16% |
| 1002 baseline | unsigned | Jazz / standardsInterpreter | 1960 | 1542/2388 | 64.57% | 60–90% | 0.00% |
| 1002 baseline | unsigned | Jazz / standardsInterpreter | 1960-63 | 29265/42506 | 68.85% | 60–90% | 0.00% |
| 1002 baseline | unsigned | Jazz / composerLed | 1960 | 242/555 | 43.60% | 10–50% | 0.00% |
| 1002 baseline | unsigned | Jazz / composerLed | 1960-63 | 4807/11023 | 43.61% | 10–50% | 0.00% |
| 1002 baseline | unsigned | Country / mixedContemporary | 1960 | 3232/7192 | 44.94% | 20–50% | 0.00% |
| 1002 baseline | unsigned | Country / mixedContemporary | 1960-63 | 51631/105543 | 48.92% | 20–50% | 0.00% |
| 1002 baseline | unsigned | Folk / traditionalRevival | 1960 | 2121/2690 | 78.85% | 50–90% | 0.00% |
| 1002 baseline | unsigned | Folk / traditionalRevival | 1960-63 | 42865/56482 | 75.89% | 50–90% | 0.00% |
| 1002 baseline | unsigned | Gospel / inheritedRepertoire | 1960 | 1026/1980 | 51.82% | 50–90% | 0.00% |
| 1002 baseline | unsigned | Gospel / inheritedRepertoire | 1960-63 | 15158/30104 | 50.35% | 50–90% | 0.00% |
| 1002 baseline | unsigned | Country / mixedContemporary | 1961 | 13620/30326 | 44.91% | 20–50% | 0.00% |
| 1002 baseline | unsigned | RnB / mixedContemporary | 1961 | 78/36452 | 0.21% | 5–25% | 4.79% |
| 1002 baseline | unsigned | Folk / traditionalRevival | 1961 | 14674/19125 | 76.73% | 50–90% | 0.00% |
| 1002 baseline | unsigned | Jazz / standardsInterpreter | 1961 | 9284/14177 | 65.49% | 60–90% | 0.00% |
| 1002 baseline | unsigned | Jazz / composerLed | 1961 | 1452/3754 | 38.68% | 10–50% | 0.00% |
| 1002 baseline | unsigned | Gospel / inheritedRepertoire | 1961 | 3882/8608 | 45.10% | 50–90% | 4.90% |
| 1002 baseline | signed | Jazz / standardsInterpreter | 1961 | 2819/4649 | 60.64% | 60–90% | 0.00% |
| 1002 baseline | signed | RnB / mixedContemporary | 1961 | 27/18531 | 0.15% | 5–25% | 4.85% |
| 1002 baseline | signed | Country / mixedContemporary | 1961 | 4356/10406 | 41.86% | 20–50% | 0.00% |
| 1002 baseline | signed | Folk / traditionalRevival | 1961 | 5092/6896 | 73.84% | 50–90% | 0.00% |
| 1002 baseline | signed | Gospel / inheritedRepertoire | 1961 | 1085/2411 | 45.00% | 50–90% | 5.00% |
| 1002 baseline | signed | Jazz / composerLed | 1961 | 536/1471 | 36.44% | 10–50% | 0.00% |
| 1002 baseline | unsigned | Jazz / standardsInterpreter | 1962 | 8424/12155 | 69.30% | 60–90% | 0.00% |
| 1002 baseline | unsigned | Jazz / composerLed | 1962 | 1288/3022 | 42.62% | 10–50% | 0.00% |
| 1002 baseline | unsigned | Country / mixedContemporary | 1962 | 13809/29705 | 46.49% | 20–50% | 0.00% |
| 1002 baseline | unsigned | RnB / mixedContemporary | 1962 | 1495/27610 | 5.41% | 5–25% | 0.00% |
| 1002 baseline | unsigned | Folk / traditionalRevival | 1962 | 11306/15057 | 75.09% | 50–90% | 0.00% |
| 1002 baseline | unsigned | Gospel / inheritedRepertoire | 1962 | 4183/8652 | 48.35% | 50–90% | 1.65% |
| 1002 baseline | unsigned | ContemporaryFolk / contemporaryInterpreter | 1962 | 3546/4653 | 76.21% | 10–35% | 41.21% |
| 1002 baseline | unsigned | ContemporaryFolk / contemporaryInterpreter | 1960-63 | 7730/10181 | 75.93% | 10–35% | 40.93% |
| 1002 baseline | signed | Country / mixedContemporary | 1962 | 5375/12604 | 42.65% | 20–50% | 0.00% |
| 1002 baseline | signed | Folk / traditionalRevival | 1962 | 6031/8218 | 73.39% | 50–90% | 0.00% |
| 1002 baseline | signed | RnB / mixedContemporary | 1962 | 1367/17545 | 7.79% | 5–25% | 0.00% |
| 1002 baseline | signed | Gospel / inheritedRepertoire | 1962 | 1689/3845 | 43.93% | 50–90% | 6.07% |
| 1002 baseline | signed | Jazz / standardsInterpreter | 1962 | 2974/4565 | 65.15% | 60–90% | 0.00% |
| 1002 baseline | signed | Jazz / composerLed | 1962 | 759/1763 | 43.05% | 10–50% | 0.00% |
| 1002 baseline | signed | ContemporaryFolk / contemporaryInterpreter | 1962 | 617/852 | 72.42% | 10–35% | 37.42% |
| 1002 baseline | signed | ContemporaryFolk / contemporaryInterpreter | 1960-63 | 2713/3793 | 71.53% | 10–35% | 36.53% |
| 1002 baseline | unsigned | BossaNova / olderSongbook | 1962 | 1108/1605 | 69.03% | 15–40% | 29.03% |
| 1002 baseline | unsigned | BossaNova / olderSongbook | 1960-63 | 4216/6066 | 69.50% | 15–40% | 29.50% |
| 1002 baseline | signed | BossaNova / olderSongbook | 1962 | 66/106 | 62.26% | 15–40% | 22.26% |
| 1002 baseline | signed | BossaNova / olderSongbook | 1960-63 | 1021/1471 | 69.41% | 15–40% | 29.41% |
| 1002 baseline | unsigned | Jazz / standardsInterpreter | 1963 | 10015/13786 | 72.65% | 60–90% | 0.00% |
| 1002 baseline | unsigned | Jazz / composerLed | 1963 | 1825/3692 | 49.43% | 10–50% | 0.00% |
| 1002 baseline | unsigned | RnB / mixedContemporary | 1963 | 3136/31008 | 10.11% | 5–25% | 0.00% |
| 1002 baseline | unsigned | Country / mixedContemporary | 1963 | 20970/38320 | 54.72% | 20–50% | 4.72% |
| 1002 baseline | unsigned | Folk / traditionalRevival | 1963 | 14764/19610 | 75.29% | 50–90% | 0.00% |
| 1002 baseline | unsigned | Gospel / inheritedRepertoire | 1963 | 6067/10864 | 55.84% | 50–90% | 0.00% |
| 1002 baseline | unsigned | ContemporaryFolk / contemporaryInterpreter | 1963 | 4184/5528 | 75.69% | 10–35% | 40.69% |
| 1002 baseline | unsigned | BossaNova / olderSongbook | 1963 | 3108/4461 | 69.67% | 15–40% | 29.67% |
| 1002 baseline | signed | Country / mixedContemporary | 1963 | 8299/15984 | 51.92% | 20–50% | 1.92% |
| 1002 baseline | signed | Folk / traditionalRevival | 1963 | 5839/7773 | 75.12% | 50–90% | 0.00% |
| 1002 baseline | signed | RnB / mixedContemporary | 1963 | 1937/14673 | 13.20% | 5–25% | 0.00% |
| 1002 baseline | signed | Jazz / standardsInterpreter | 1963 | 3306/4818 | 68.62% | 60–90% | 0.00% |
| 1002 baseline | signed | Jazz / composerLed | 1963 | 862/1770 | 48.70% | 10–50% | 0.00% |
| 1002 baseline | signed | Gospel / inheritedRepertoire | 1963 | 2725/5011 | 54.38% | 50–90% | 0.00% |
| 1002 baseline | signed | ContemporaryFolk / contemporaryInterpreter | 1963 | 2096/2941 | 71.27% | 10–35% | 36.27% |
| 1002 baseline | signed | BossaNova / olderSongbook | 1963 | 955/1365 | 69.96% | 15–40% | 29.96% |

### Unmet and unobserved targets

| Run | Population | Cohort | Share | Distance | Disposition |
| --- | --- | --- | --- | --- | --- |

Existing act data does not reliably distinguish Gospel quartet/choir/songwriter-led ensembles or Country heritage/dance-hall cohorts. Minimal proposed addition for review: an explicit ensemble/repertoire role enum; no invented per-act historical labels were added. Gospel also lacks a sacred/secular repertoire-purpose constraint: related R&B songs remain eligible and their playable shapes can outrank inherited choir/quartet material. The early treatment trace observed many R&B-catalogue and recent R&B selections in Gospel sets. This suggests a representational limit, not proof of a historical share; lowering cross-scene access until a target is reached would conceal that missing repertoire purpose. Proposed review addition: a repertoire-purpose field distinguishing inherited sacred, secular and unrestricted material, with explicit adaptation metadata. Surf shares the existing rare-rock source machinery, which strongly limits inherited material; relaxing that source prior would change an explicitly protected shared studio mechanism. A dedicated live-only source ecology and an instrumental ensemble role would provide a defensible next step if the instrumental band is unmet. No quota was used to hide either constraint.

## Constants and rationale

| Editable number | Value | Rationale |
| --- | --- | --- |
| demandVariation | 0.025 | ±0.025 preserves small first-listen differences |
| identityVariation | 0.02 | ±0.020 retains the authored musical identity |
| exactAccess | 0.22 | 22% latent primary-scene access limits catalogue-wide familiarity |
| secondaryAccess | 0.08 | 8% explicit shared-tag access; draft 18% admitted too much generic Country into Gospel |
| familyAccess | 0.035 | 3.5% adjacent same-family access |
| crossSceneAccess | 0.015 | 1.5% declared cross-scene borrowing |
| preferenceDrift | 0.005 | ±0.005 four-year seasonal drift; draft ±0.025 moved preferences too quickly |
| genrePreferenceWeight | 0.08 | 0.08 follows existing genre drift without overwhelming stable preference |
| hookPreference | 0.06 | 0.06 weak audience hook preference |
| familiarityPreference | 0.08 | 0.08 weak familiar-material preference |
| suitabilityWindow | 0.025 | 0.025 groups near-suitable material while preserving fit separation |
| balladBalance | 0.05 | 0.05 small preference for one slow number after the first song |
| writingFloor | 0.15 | 0.15 begins normalized writing ability |
| exceptionalWriter | 0.85 | 0.85 caps normalized strength and permits rare full-original acts |
| composerLedCutoff | 0.65 | 0.65 existing skill proxy separating stronger writers |
| establishmentAge | 15 | 15-year provisional circulation proxy |
| establishmentDurability | 0.5 | 0.50 excludes weak transient material from automatic age promotion |

| Additional mechanism constant | Value | Rationale |
| --- | --- | --- |
| Writing propensities | Jazz composer .85, interpreter .20; Folk .25; Gospel .30; Easy standards .20/instrumental .60; contemporary Folk .90/early .40; Bossa older .65/contemporary .85; Country .70; Surf .75; other .65 | Provisional cohort allocation; high-songbook cohorts write less without hard old-song quotas. |
| All-original exception | .03, composer Jazz .12 | User-requested rarity; persistent per act, exceptional writers only. |
| New supplied-song slot | (1−ability)×.20 | Occasional supplied fresh material, availability required. |
| Introductory book | 4 songs | Small minimum exposure; independent of requested covers. |
| Genre blending | 12 months | Gradual response to existing genre identity drift; change year is the available timestamp. |
| Season | 48 months; sampled in quarter units | Slow continuous sinusoid; no booking RNG. |
| Local-familiar admission | hook ≥.55 | Moderately memorable released singles can circulate before chart completion. |
| Circulation standard alternative | familiarity ≥.40; ≥3 distinct recorded acts | Proxy for durable interpretation by multiple artists, subject to age and provenance. |
| Native craft/melody/durability | .50–.80 | Competent usable native songs, not uniform masterpieces. |
| Native lyric quality | .45–.75 | Moderate quality spread, instrumental constraints retained. |
| Native hook/rhythm | .40–.75 | Moderate range avoids seed supply determining chart success. |
| Native adaptability | .50–.70 | Borrowable shapes without universal malleability. |
| Native originality | .40–.70 | Mixed synthetic repertoire character. |
| Native familiarity | .25–.60 | No instant universally known catalogue. |
| Native rights cutoff | 1923; explicit lineage always PD | Provisional synthetic rights assignment; rights never determine lineage. |
| Assignment weights/density/form/meter | Full table above | Authored provisional templates; density .40 ordinary vocal, .65 verse/topical, 0 instrumental. |
| New row axes/plasticity/year/pace/mood | Full table above | Author proposal, available for review; no claim of archival measurement. |
| Supply counts/year ranges | Full table above | Native coverage with recent-era compositions eligible only in their origin year. |
| Schema | version 1; additive nullable/zero defaults | Preserves old saves without regenerating their composition truth. |
| Hash constants | FNV-1a plus MurmurHash3 finalizer, top 24 bits | Algorithmic hashing; avalanche prevents adjacent catalogue IDs receiving adjacent draws; not tuning. |

## Verification, scope and remaining limits
Build passes with the existing obsolete-helper and unused-event warnings. Polar data, fit, behavior, recording realization, rock songbook, player perception and repertoire checks pass. The refusal-avoidance compatibility check found a null proposal when polar was disabled; the guard was corrected and the check passes. This conditional does not change enabled treatment behavior. Final invocations bind each run to its assembly and table hashes. The telemetry optimization removes a redundant rerank of the performed set: all four initial live census streams and initial master assignments matched byte for byte (`polar-repertoire-fast-neutrality.json`). Earlier interrupted drafts are excluded from acceptance. Whole-world save/load is byte-identical, including real gzip IO: SAVELOAD_ROUNDTRIP_PASS weeks=2 bytes=113051167 gzipBytes=8546550 ratio=13.2x gzipIo=ok labels=600 artists=7084 unsigned=82 defunct=0 chartWeek=2.

The pre-existing autoload diagnostic for MissingSingletonsTemp.cs appears in headless logs; it did not prevent completed audits or successful checks. The sandboxed Godot launch failed and was discarded; valid runs used the approved normal-host execution path. Existing unrelated uncommitted changes were retained. No studio source-share, chart success, perception, refusal policy, fit plateau or historical-band retuning was performed. Admission routes necessarily affect later source availability; those recording and chart outcomes are observed separately.

Review questions: provisional musical templates and first-year assignments; uniform synthetic native supply; Gospel/Country ensemble-role field; the year-only genre drift timestamp; inherited Surf source constraint; whether measured concentration or churn requires a further mechanism iteration. Cohort proxies and original-allocation priors remain inferred choices. Actual historical archival bands remain unmeasured.

## Source SHA-256 and reproducibility
The manifest compares against the pre-task source snapshot, preserving already-uncommitted work. New files have no pre-task hash. Report/analyzer-only additions are refreshed after result extraction. Symbols are listed by phase above; file references do not rely on shifting line numbers.

| Path | Before SHA-256 | Final SHA-256 |
| --- | --- | --- |
| Data\PolarRepertoireTable.json | new | 56A202FE311705EEA5ACAF0FE22520543668822DD89DD70E8FD3EE5A6BD13A94 |
| Data\PolarSongMetadata.cs | BEE88F4F74CE4935BCCFA407A60FDEC9B0D847C67D786CDBF05EE475B2B8B108 | DD80D01A3550E5D4FF827DC45875B416DF03E4B6DAE8EBCB8271B8FF4E086667 |
| Data\PolarSongProfiles.cs | 649B532C4A5EFDEEB1F658B38C3618ED7BBB00A901BA9DBDAB3676CD24CCEA0E | AF4F15D270B86E2170971E0590591E49FBF5C71A3CC33CCDDD6D552B41646E12 |
| Data\PolarSongTable.json | 880B00DA30EFCAD28FB48486CAD42D72E30022CAD751C2073D4E2F38874D42A2 | 2179DC205FFB490CE9795AB1E730DE2D80E123858A11129762F0D858D4B4B117 |
| Data\SimulatedArtist.cs | D576C5CA9FAE6B38390E4052EA917F415ABB08F75B4548ED94E4D8C91ACC92B4 | 87AB94E2AD2CFC0B651197250269ABEA83632CB2B2E6EB118701EF446285ADEE |
| Data\SongComposition.cs | ACCD1B16C33E1A7F7F9782775C2A9FE7516BEDCE8CB3C66A4C50C1083E035EE0 | 43474243798CF24E0A76C3AC849AFB0E338A94D560D9AD749B7CBC12997291B9 |
| Systems\ChartManager.cs | 31414BACF2E2D915E28A43E9B0E16866567BD6B69F01326873194B8D5598FCE9 | 903E505EDAA2CBDB6805223BA62F5FB9EA3FEAE27F1D9B92F61C0AC760616A04 |
| Systems\CompositionCatalogService.cs | 67A6119B357C8733747AE2AC10D8A4E3AE443D2414D34C23AB3D66AC9BC4025A | A6C3D1C0C09C17D492ABA03AE091F03B876689AFE43CA26B287281A353D9731C |
| Systems\LiveRepertoire.cs | new | 5F66D3BE95A6A3E732F2F72BE9930E36119CDD1E92A3CF9A332222CFA3EE41D9 |
| Systems\PlayerDesk.cs | 7D7612A79FF2EB7CAE82F25537F852307126BEA39D680398FCF8C4C27E9D1D3F | 249B07B4E512C81F15F4F8F3FE275A2101D53CE5854DF0455F8BC76DB44A1424 |
| Systems\PolarActProfileDeriver.cs | C5942708A427BB8613AED512AD74160957C868E040F96B43944213FDB5836D4E | 9F8C17424967BB8C81F455EFC261318A1BC3811F2A0C33A9F6446B32A65695A8 |
| Systems\PolarCoverResolver.cs | 3B118B996247FE2752D9252870C1C5F9D5E84EFB9EC713AD0EFCA8DF72DC740A | 1BE6A8BBBB7D6BC5E79434E68DAB6840AEBAAB99E971B4A01FE88956670A04AC |
| Systems\PolarRepertoireTable.cs | new | 483899C2AFB8DF5DF790B27BD763D25F4FF7205F68E3F3186C0838DDE5F6C1F2 |
| Systems\PolarSongBehavior.cs | FDB6ABDDFCB40D1D474BDC49E703C7F0396FA51310E3019D42CA0B50F9F47359 | 1DF8466226AB493F563072C3C097BBC1D04B96783892894BFB5C113D827B8D52 |
| Systems\PolarSongMetadataService.cs | C52B7CB1A0BE02AD79A249C61712EDB65F008B5C86BBEB95B3A33B8297CCDF44 | 002DBB684FD1E918DF351A10DDD4E30B2890BC36E7F8B5D1F5E88C4AA35A7B61 |
| Systems\RepertoireProvenance.cs | new | EA3FE45C00D8C59696FAD6D11D5AA2FD06BA17D8E686262C5773C6ADB262FB97 |
| Systems\SimulationSeedBootstrap.cs | 1C0234712BDCF61B086F1B5201B732FFB1DE2CD4DE6C0F815520E978D9C54DB7 | 175B096331AAE3D0E9B87F8E2CAF234F5D4EA58AB96C38307FB867145F8FAC0B |
| Systems\SongMaterialApplicationService.cs | F1F11943606634375CB9817DEB8A8EBBE134F754FED298C75579366D049B4679 | EAE3707C9A33A449EB3F2EF14BAA6BC1A31A937BF0168633DA607B597E995422 |
| Systems\SongMaterialSelectionService.cs | B5B27040449C67CE532DEF9B22FC2DC69E0D24D2E2255BD95B1E803645C4DB89 | EB1E53B8F86E1FBC6A509BE28B91DF478BE2BD750353BFE76A6A84B38F88A201 |
| Systems\SongProfileDeriver.cs | E560839BD794AE9E5629DBFAA2419E19346C9E65ACA668DE6761D03D9AB292BA | 048602A23DB49A328F6F1D2BFAA2BD7C33F3A4B7F7D3D8DB1013B5BBF83D6DDE |
| Systems\WorldSaveData.cs | EFD4B8C82582F732A69B7DB76EB66B99F21AE963D9FCA7261A85287AAAD19318 | 942290EE285B83231DC462C9EA9A743BF508641159841C409273AC2F53CC03B0 |
| SimTools\analyze-polar-repertoire.mjs | new | 941E3260FE5CC6533C99BA08270AD9D850C64467864CB4F9876A40372B503A63 |
| SimTools\ChartAuditRunner.PolarDiversity.cs | B96DC222AEDB9BF70C52B04B876ED91A91BA38F024175464F99AB69D82EE8593 | D9E7882B2512A6A0DCA307A5A12AA134453FA5C5C84CF8A881C1BBBD8055F7A9 |
| SimTools\ChartAuditRunner.PolarFollowup.cs | 22E0333599B782E42DE823B2919BC59AB4C8AD1D8BBEAF7E6AE1F17DCD23218D | B7732C6A0D1AAA4BF4C65477F26CDB8A396339F6FD09D324E5D6AA240FDDB546 |
| SimTools\ChartAuditRunner.PolarRepertoire.cs | new | DED2279AEEB340E38CF999FE9FAC24A79911ADB5359D51ED7332BEB20F9ADF98 |
| SimTools\DirectivePolarRepertoire-Codex.md | new | 0C78A78D48A65EA350501EB4CF76B7CDE6FB7378FA1942B3E14B555755297E98 |
| SimTools\draft-polar-repertoire-tables.mjs | new | D6F0821B64E271585C656D9136979DDB91FCE1D55ECD53DFF15C15611DDF547B |
| SimTools\inspect-polar-repertoire-progress.mjs | new | 781E3CEE4030A10F8E110A867CDAC8CAEB1996CF545C5A0F4E86AFDDF03D1957 |
| SimTools\PolarRepertoireChecks.cs | new | B30635A64C153A431F31ECEA8955369E7DD2BF73C087B557C44E1A47E90A10B5 |
| SimTools\PolarRepertoirePhaseProbe.cs | new | D2318B6063A03B6056C8785758D8F6A1D7600251F75E55EE2B8F36461EFB7B2E |
| SimTools\PolarRepertoirePhaseProbe.tscn | new | 5FE62A1ACC9AF6B8224D06B9E8AE37E29F88ACC07E23FA8A2DC4FD950634B6DA |
| SimTools\PolarRockSongbookChecks.cs | A1F5521AB5A3CCAC7A831C4D4A005466193FE4CD8EBE0A2540BBD14E16341EC7 | B058667F4786523E28F9123F32B08D18AAFBEE43069F6B9487F2EEA2A4B310D7 |
| SimTools\run-polar-followup-audit.ps1 | 9496DEB21396E33625854AA66202D74CB18539FB51AB48FE2548F5F9A247834D | 58613547D771CE4F0A4AEA0DD38389D9F8C5BB8E1731206850533399ED9E499D |
| SimTools\SaveLoadRoundTripRunner.cs | BFD34A76659603CB42BA23D0282978B44078B4FD6EAAECCCFA7F52F6EF008A1B | EA4C36BA1B08FD6537399048EFC4567E6D0AEAC5697AE5936BDFDD47DB92D761 |
| SimTools\write-polar-repertoire-report.mjs | new | 2CD7B6514A720AF3D7B6E8772F900ABBB9FCE06464236CA96E3494FB6BBE8BB0 |

Raw evidence: `../SimLogs/polar-repertoire-analysis.json`, `../SimLogs/polar-repertoire-source-manifest.json`, each run invocation JSON, 48-month slot/set streams, admissions and inventory CSVs, both phase-probe streams, and the phase-0 byte-neutrality JSON. Regenerate with `node SimTools/analyze-polar-repertoire.mjs <completed prefixes>` then `node SimTools/write-polar-repertoire-report.mjs`. The analyzer rejects incomplete markers, invalid fits, future songs, malformed CSVs, incorrect weekly counts and missing census months.

