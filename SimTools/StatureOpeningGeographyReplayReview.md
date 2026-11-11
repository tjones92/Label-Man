# Release-level opening geography replay — 2026-11-06

Follow-up: [national standing for Star and above](StatureRegionalSensitivityReview.md)
tests regional-route dependence and an offline national-route requirement for
the higher tiers, following the user's policy decision.

Concentrating the RegionalName cohort's surviving opening followers generally **raises** standing and proposed launch budgets in these frozen worlds. None of the tested geography rules lowers an available launch proposal in either year. The seeder does discard geographic information, but localization is not a demonstrated remedy for standing-budget inflation.

The release census also sharpens the interpretation problem: 20 Top 40 entries supply 84.48% and 82.83% of this cohort's opening stock. Treating those entries as national crossovers substantially reduces the geography sensitivity. Their chart peaks and the generator's crossover comment do not establish which regions actually bought them.

## Comparison and scope

The [protocol](StatureOpeningGeographyProtocol.md) was written before inspecting the new release census and replay results. All four worlds run 104 weeks: seeds 1001 and 2002, legacy-budget control and original full-marketing treatment. Geography alternatives run offline and never supply gameplay inputs or budgets.

Only the fixed 20 RegionalName acts per seed are reallocated. Actual non-source followers, national following scores, observed sales/TV, peer references and the calibration stay fixed. Each alternative has independent hysteresis. The rules are:

- **Home only:** all qualifying named-origin release contributions go to their origin. This is a concentration stress scenario that ignores additional market counts.
- **Home + market count:** enumerate all footprints of the stored size containing the named origin, allocate by population within each, and average the allocations uniformly. This produces an averaged input scenario, not an inferred territory or an expectation of nonlinear standing outcomes.
- **Crossovers national:** apply the preceding rule to other releases, while distributing positive Top 40 releases nationally. This tests an interpretation of crossover; it is not a finding that every such release sold in all seven regions.

Origin fields contain display names such as `Deep South`; the export retains those strings and the replay resolves both region names and IDs. Count-only histories outside the targeted cohort remain observed.

## What the releases contain

| Seed | Qualifying RegionalName releases | Opening followers | Top 40 entries | Followers attributed to Top 40 entries | Share |
|---|---:|---:|---:|---:|---:|
| 1001 | 50 | 727,075 | 20 | 614,250 | 84.48% |
| 2002 | 57 | 819,018 | 20 | 678,375 | 82.83% |

Every qualifying RegionalName release has a named origin and one to three markets, including the Top 40 entries. Their one/two/three-market counts are 7/8/5 in seed 1001 and 7/6/7 in seed 2002. The Top 40 entries include two albums in seed 1001 and three in seed 2002. The existing single-format age split and peak-to-units mapping are preserved throughout this comparison.

`ShapeTail` ensures an Established act has a recent hit and describes the RegionalName case as a crossover. `Finish` then assigns regional market counts to all of that archetype's releases. This creates an interpretation gap between chart performance, origin and selling territory. A Top 40 peak alone does not prove seven-region reach, and an origin alone does not mean home-only sales.

The full qualifying release censuses contain 666 and 693 entries respectively. Of these, 616 and 636 have count-only geography and are outside this reallocation. Release contributions reconstruct the original seed census within the declared float tolerance, and paired opening/release censuses match exactly.

## Launch proposals

Counts below are **higher full-table proposals relative to the observed standing proposal**, before existing caps or spending. Available reads cover only these 20 acts' single-launch decisions in 1961. A higher proposal need not exceed the legacy envelope. Control proposals are diagnostic and unpaid.

| Seed / arm | Available reads | Higher: home only | Higher: home + count | Higher: crossovers national |
|---|---:|---:|---:|---:|
| 1001 control | 13 | 13 | 11 | 2 |
| 1001 treatment | 25 | 19 | 13 | 5 |
| 2002 control | 34 | 20 | 18 | 9 |
| 2002 treatment | 40 | 22 | 21 | 8 |

All corresponding lower-proposal counts are zero, including in 1960. The 1961 treatment changes involve 15/12/4 distinct acts in seed 1001 and 13/13/7 in seed 2002, in the same rule order.

For example, Jim Pierce (`artist_00672`, seed 2002) has an observed proposal of 1,127.53 on September 2, 1960 in both arms. Home-only and home-plus-count relocation each produce an Established-band proposal of 3,382.59. The earlier source-removal diagnostic leaves that observed quote unchanged. Source removal and geographic relocation answer different questions.

These comparisons hold completed-world observations fixed. They do not measure additional paid exposure, subsequent audience growth, chart success, or a world initialized with different geography.

## Bands and mechanism

Each world has 1,020 selected standing observations in 1961. Higher-band counts under home-only / home-plus-count / crossovers-national are:

| Seed / arm | Home only | Home + count | Crossovers national |
|---|---:|---:|---:|
| 1001 control | 1,007 | 903 | 293 |
| 1001 treatment | 930 | 809 | 293 |
| 2002 control | 809 | 664 | 238 |
| 2002 treatment | 820 | 704 | 275 |

There are no lower bands in 1961. In 1960, each rule has one lower observation: Earl Williams (`artist_00233`), seed-2002 treatment, September 2. Observed Star becomes Established under all three alternatives. This is one shared act/week, not three distinct cases; it does not lower a measured launch proposal.

The regional route uses maximum regional density. At draw 1, its current Star entry support is `0.5 + 0.5 × recordReach`. With entry threshold 0.515904, approximately 3.18% record reach suffices to enter Star. The Star exit threshold, 0.412723, is below the support floor of 0.5 at draw 1. An isolated evaluator check confirms entry at reach 0.032 and retention at reach 0.02913. This explains a strong concentration pathway without establishing that it causes every changed observation.

Reach can decrease while a band increases. For example, home-only conversion reach falls in 204 seed-1001 treatment observations in 1961, yet the alternative has 930 higher bands and no lower bands that year. The artifact retains every input change and weekly band, including decreases in regional draw under some rules.

## Accounting limits and correction direction

Every observed young source cohort is conserved nationally and reallocated using that cohort's initial release mixture. The adult pool uses the initial adult release mixture, including for later ageing transfers. Adult provenance does not retain release-year identity, so this is an explicit mixing convention. Home-only allocation is unaffected by that mixture convention when an act's releases share one origin; the other rules can depend on it.

Relocating observed survivors holds their actual losses fixed. It does not simulate the region-dependent losses, saturation and joins of a differently initialized world. Diagnostic counts receive no additional audience capacity cap. Missing evidence remains unknown, and neutral reconstruction reproduces every selected C# band/status and regional input. National following scores are used unchanged.

A geography correction should separate chart scope, origin and reached regions in the stored history, then allocate contributions per release before aggregating followers. The missing destination identities and crossover semantics need an explicit generation rule. These results also identify the regional standing route's concentration sensitivity as a separate calibration question. The current experiments do not select a gameplay rule or justify changing the economic reader.

## Validation and reproduction

- Build: zero errors and warnings. Headless source/export fixtures: 14 checks passed. Python geography and route fixtures: 20 tests passed.
- The 39-week seed-1001 pair matches all 150 original CSVs in each arm against `stature-origin-smoke-v1`, with exactly two geography exports added.
- All four 104-week worlds have exact completion markers, empty error logs, verified archives of 152 CSVs, and passing paired summaries. They match all **600 original CSVs** against their corresponding `stature-origin-2y-v1` worlds. This is observation neutrality for the new exports, separate from the original policy comparison.
- All four requests share assembly, engine, calibration and C# content hashes. Assembly SHA-256: `2D797EB25410C13E7368074BFAFDC39AE91924966B45DD06DB255E8E6429D776`. Calibration remains `80546B52E1CE0902FF44692FF642B45FCC6DA5A63D48663B9CDB295DB6450F19`.
- The replay validates conserved national age totals, paired censuses, chronological regional coverage, every neutral band/status and regional input, and every neutral available launch proposal. The final source observation is December 22, 1961.

Run `SimTools/run_stature_marketing_trial.ps1` with a fresh prefix, for example `-RunPrefix stature-geography-2y-v2 -Weeks 104 -Seeds @(1001,2002) -CausalOnly -OpeningGeography -CompressOutputs`, to reproduce simulations. Replay that prefix with `SimTools/replay_opening_geography.py --prefix stature-geography-2y-v2 --output SimLogs/writing-calibration/stature-geography-2y-v2-analysis.json`. Existing run names and report files are protected from overwrite.

Primary results: [two-year replay](../SimLogs/writing-calibration/stature-geography-2y-v1-analysis.json). Neutrality reports: [seed 1001](../SimLogs/writing-calibration/stature-geography-2y-v1-1001-neutrality.json), [seed 2002](../SimLogs/writing-calibration/stature-geography-2y-v1-2002-neutrality.json), [short pair](../SimLogs/writing-calibration/stature-geography-smoke-v1-neutrality.json). Short replay: [current analysis](../SimLogs/writing-calibration/stature-geography-smoke-v2-analysis.json). Requests, output manifests, paired summaries and completion/error logs remain under `SimLogs/writing-calibration`; all CSV archives are under `SimLogs`.
