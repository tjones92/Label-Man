# Opening-following provenance investigation — 2026-11-06

Synthetic opening following demonstrably sustains standing-budget support. Approximately 7.3M of the original 12.445M followers survive through December 1961 in each control world, accounting for about 45% of the seeded acts' current following. Regional draw/reach is a substantial part of that support; discounting national following scores alone misses much of the mechanism.

The fixed synthetic-history cohort also gains Top 10 representation in 1968 under the original full-marketing treatment in both seeds. That is an association in divergent worlds, not proof that surviving seeded followers caused the later chart gains. The source-specific measurement here ends in 1961.

## Measurement and source boundary

`--observe-opening-following-origin` tags the followers created by `SeedOpeningFollowing` before any live-week flow. Each seed has 75 such acts, with stock spread over all seven regions (525 act-region census rows). The initial totals are 12,444,943.87 in seed 1001 and 12,445,550.36 in seed 2002.

The passive metadata follows original stock through disappointment, controversy, drift, silence, switching, young-cohort ageing, transfer into adults, capacity caps and pruning. New joiners remain untagged. Loss at an adult cap is allocated proportionally between existing adults and ageing transfers; this is a source-accounting convention. Counts are checked against actual stock and for absence of unexplained source growth.

This tracks surviving original people only. It does not track later followers attracted indirectly by those people, prewarmed catalogue buyers, opening recognition, attachment, chart credits or room standing. A lack of this tag therefore does not mean an act was unaffected by prewarm.

The half/zero alternatives subtract surviving tagged young/adult counts **before normalization and clipping**, recompute regional density, record reach and conversion reach, and evolve independent weekly hysteresis. They retain observed sales, TV evidence, lagged peer references and missing-evidence rules. They never supply a budget to the game. Actual follower demand, live draw, programme audiences, paid campaigns and RNG paths remain unchanged by the instrumentation.

The earlier half-score screen differs materially: a saturated following score can remain at one after removing half the underlying opening stock. The new fixtures explicitly test this distinction.

## Persistence and requested envelopes

The results at the last completed observation, December 22, 1961 (1961 counts end on that date):

| Seed / arm | Surviving original stock | Share of these 75 acts' actual following | 1961 available single-launch reads | Standing-table uplifts | Uplifts eliminated with half opening stock | Uplifts eliminated with zero opening stock |
|---|---:|---:|---:|---:|---:|---:|
| 1001 control | 7,318,630 | 44.71% | 174 | 61 | 14 | 28 |
| 1001 treatment | 7,316,029 | 45.92% | 166 | 66 | 14 | 25 |
| 2002 control | 7,306,170 | 44.61% | 185 | 69 | 21 | 29 |
| 2002 treatment | 7,459,286 | 40.02% | 198 | 97 | 24 | 44 |

These denominators cover the fixed 75 seeded acts, not all industry following or all launches. Counts are requested full-table envelopes before existing caps, not spending. An eliminated uplift means the alternative quote is no higher than the observed legacy envelope. A reduced uplift can remain above legacy: in the control worlds, zero opening stock reduces 34 of the 61 uplifts in 1001 and 40 of the 69 in 2002.

Control quotes are standing-table proposals that are not paid or applied in that world. Treatment quotes use the actual full-strength reader. In the treatment worlds, removing opening stock from the diagnostic evidence eliminates 25/66 (37.9%) and 44/97 (45.4%) of those seeded-act 1961 uplifts. The diagnostic itself makes no causal change to the worlds.

## National components versus regional inputs

An exploratory offline partition keeps one input family observed while removing original stock from the other. Each partition has independent hysteresis. Its combined version must reproduce every C# zero-source band/status. These pathways overlap and their counts are not additive causal shares.

| 1961 world, uplifts eliminated | National following components only | Regional draw/reach inputs only | Both |
|---|---:|---:|---:|
| Seed 1001 control, out of 61 | 1 | 22 | 28 |
| Seed 1001 treatment, out of 66 | 2 | 16 | 25 |
| Seed 2002 control, out of 69 | 8 | 16 | 29 |
| Seed 2002 treatment, out of 97 | 4 | 31 | 44 |

The regional partition changes regional draw plus record/conversion reach. It is not a pure geographic-localization intervention: removing stock also lowers regional density. The result identifies an input pathway worth isolating, not the economic outcome of changing that pathway.

## A concrete geography inconsistency

Completed next experiment: [release-level geography replay](StatureOpeningGeographyReplayReview.md). In the fixed RegionalName cohort, tested localization rules generally raise standing and produce no lower launch proposals across the two-year worlds. Top 40 entries dominate that cohort's opening stock; crossover reach remains an explicit interpretation choice.

Follow-up: [targeted geography investigation](StatureOpeningGeographyReview.md) isolates the 20 RegionalName acts in both seeds and tests fixed-total footprint inputs. It finds a smaller budget-sensitive subset than the all-history screen, and shows that concentration can raise regional draw without lowering weighted reach before saturation. Gameplay behavior is unchanged.

`PublicReleaseEntry` stores `markets` and `regionId`. `OpeningCareerService` marks RegionalName histories as regional and fills their geographic fields. `SeedOpeningFollowing` ignores those fields and distributes every qualifying history-derived follower stock to all regions by population. Thus even a history with explicitly regional reach supplies nationwide following density to the standing model.

All 20 RegionalName acts receive that seven-region distribution. In seed 1001 their total is approximately 727,075 followers. This is a concrete mismatch between the stored evidence and its geographic interpretation, though it is only part of the 12.445M opening stock. The seeder also uses the single-format age split and one peak-to-units mapping across historical formats; that is another calibration assumption, not a newly demonstrated cause of the cohort split.

A targeted next policy experiment should preserve real audience behavior and compare how standing treats known regional opening evidence. Simply concentrating followers geographically is not guaranteed to lower bands: it can raise local density and the regional maximum while reducing population-weighted reach. That candidate needs its own input and budget checks before causal interpretation.

## Fixed history cohorts in the retained decades

The new census separates the former 98-act first-observed-following group into 75 synthetic-history acts and 23 other acts visible after the first live week. All 23 are present on the week-zero chart in each seed. Only five synthetic-history acts are on that chart in seed 1001, and seven in seed 2002. The startup career-log IDs and archetypes match the census exactly in each original decade arm.

| Fixed cohort / year | Seed 1001, control → treatment Top 10 weeks | Seed 2002, control → treatment Top 10 weeks |
|---|---:|---:|
| Synthetic-history 75, 1964 | 77 → 82 (+5) | 122 → 93 (-29) |
| Synthetic-history 75, 1968 | 63 → 100 (+37) | 37 → 70 (+33) |
| First-week-only 23, 1964 | 84 → 80 (-4) | 19 → 14 (-5) |
| First-week-only 23, 1968 | 50 → 22 (-28) | 20 → 11 (-9) |

Each year has 520 Top 10 slot-weeks. The original week-zero chart cohort loses 14 and eight slots respectively in 1968, while the synthetic-history cohort gains 37 and 33. These are different fixed groups: a startup-chart cohort cannot stand in for inherited career history. First observed charting after 1960 is also not proof of a new career when synthetic history already exists.

For example, 23 synthetic-history acts in the seed-1001 control first appear on the observed chart after 1960, and eight never appear. The corresponding treatment counts are 11 and 21. Seed 2002 has 13 and 14 in control, and 12 and 15 in treatment. Their inherited public histories already exist regardless of those observed chart years. Never-charted counts and later cohort slot gains measure different margins.

Janet and the Rebels (`artist_01672`, seed 1001) is not in the synthetic-history seed census. It belongs to the first-week-only group and has a prewarmed chart record. Georgie Payne (`artist_00007`) is in the synthetic-history census, starting with approximately 588,410 followers. This distinguishes two bootstrap channels that the first following log mixed together. It does not establish which channel caused their subsequent careers.

Two 1968 winners illustrate why source membership alone is insufficient. Robin Carr (`artist_01023`, seed 1001) gains 26 Top 10 weeks and starts with about 73,327 synthetic-history followers, but all seven treatment launch uplifts measured in 1960–1961 survive zeroing that source. Bo Pugh (`artist_00144`, seed 2002) gains 19 weeks and starts with about 207,483 such followers; nine of his ten treatment launch uplifts in those two years disappear in the zero-source diagnostic. Bo is a useful candidate for a later isolated causal test. The current measurements do not establish that those early requests became additional paid exposure or caused the 1968 gain.

The fixed seeded cohort has 10 → 12 distinct Top 10 acts in 1968 in seed 1001 and seven → ten in seed 2002. Leading individual changes, including losses, are retained alongside the cohort totals in the cohort artifact.

## Validation and reproducibility

The 39-week seed-1001 observation pair matches all 147 original CSVs byte for byte in **both** control and full-treatment arms against `stature-marketing-full-v1`. Three new CSVs contain census, weekly source/standing, and budget sensitivities. This is separate short-window instrumentation neutrality evidence; it does not claim an uninstrumented two-year pair was rerun.

All four 104-week worlds reached exact completion markers with empty stderr, archived and verified 150 CSVs each, and passed paired summaries. Requests match the paired assembly, engine, calibration, C# source hashes and economic settings. Every seeded-act neutral reconstruction, stock-bound check and chronological source observation passed at runtime and in analysis.

The source fixtures pass 12 checks, including source survival, ageing, caps, untagged joins, serialization, clipping, reach reconstruction and no RNG consumption. The existing marketing fixtures pass 35 checks. The six new Python analysis tests and six marketing-summary tests pass. Build: zero errors/warnings. The sensitivity ledgers are fresh-run diagnostics; save metadata is round-tripped, but continuation of the alternative standing histories across saved runs is not implemented or claimed.

Protocol: [StatureOpeningOriginProtocol.md](StatureOpeningOriginProtocol.md). The two-year seeds, duration and primary measurements were fixed before results. The route partition was added after the short smoke results and before inspecting two-year results.

- Two-year run prefix: `stature-origin-2y-v1`, seeds 1001 and 2002, control/full treatment, 104 weeks.
- Instrumentation-neutrality prefix: `stature-origin-smoke-v1`, seed 1001, 39 weeks.
- Assembly SHA-256: `D761002765A1917402A2A96A5A2F4AE4E8F04D498319AF734C70C47A10F2D7E8`.
- Calibration SHA-256 unchanged: `80546B52E1CE0902FF44692FF642B45FCC6DA5A63D48663B9CDB295DB6450F19`.
- Requests, source hashes, output manifests and completion/error logs are under `SimLogs/writing-calibration`. Diagnostic CSVs are archived under `SimLogs`.
- Primary source/budget results: `SimLogs/writing-calibration/stature-origin-2y-v1-analysis.json`.
- Exploratory route partition: `SimLogs/writing-calibration/stature-origin-2y-v1-routes.json`.
- Fixed cohorts and individual chart contributions in original decades: `SimLogs/writing-calibration/stature-origin-2y-v2-decade-cohorts.json`.
- Short observation-neutrality report: `SimLogs/writing-calibration/stature-origin-smoke-v1-analysis.json`.
