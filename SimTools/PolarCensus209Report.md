# Polar 209-week census and selection diversity

October 3, 2026. Four completed off/on trajectories on development seeds 1001 and 1002. All have 209 weekly rows and 48 monthly censuses (January 1960–December 1963). No repertoire policy, fit threshold, table, royalty, or perception tuning was made. Easy Listening's standards-led direction remains the user's stated preference; implementation decisions remain open.

## Findings from the completed census

Country live selection is strongly dependent on song-ID ties in both seeds. Across all act-month observations, unique selected covers fall from 587 to 45 (1001) and 601 to 62 (1002). The ten most selected songs account for 92.92% and 90.39% of enabled cover slots, versus 3.11% and 3.24% off. Reversing ID order within exact score/genre ties replaces 99.62% and 99.24% of enabled Country cover slots; reversing genre preference replaces none. This confirms the suspected composition concentration and final-ID dependence. It does not establish that songbook category shares would change under another ID order. The top three enabled songs are the same low IDs in both seeds: song_0001401, song_0001402 and song_0001403. Country's enabled cover capability median is 1.0 in both seeds, with 88.08% and 86.88% above .95; these are preliminary truth-fit diagnostics, not a census of player-facing scouting reads.

Easy Listening has two separate mechanisms to review. While the family songbook pool is available, its standard candidates average roughly .64 capability against .80 for recent hits in the enabled trajectories. The demanding TraditionalPop fallback and preliminary ranking described below provide a concrete mapping concern. Later, however, standards disappear from the candidate pool entirely: live BuildLiveSet adds family material only when the genre-exact list has fewer than four entries. Once genre-exact ordinary covers reach that threshold, the family standards are no longer considered. The first monthly snapshot with only ordinary-cover candidates is May 1960 (1001 off), July 1960 (1001 on), April 1961 (1002 off), and June 1960 (1002 on). In those snapshots the exact pools have 6, 4, 5 and 4 songs per act respectively. This observed pool transition and the source rule explain why the off trajectories also become standards-poor, and identify different transition timing behind much of the seed difference. They do not explain why each world produced those covers at different times. Source: [live-pool construction](C:/Project/Label-Man/Systems/PlayerDesk.cs:1442). Secondary EasyListening tags on standards do not make them genre-exact entries; catalogue indexes use primary genre.

Across all four years, ordinary covers account for 96.22% and 96.66% of enabled Easy Listening cover slots; recent-hit covers account for only 2.20% and 2.44%. Thus the long-period problem is broader than recent-hit preference at startup. Enabled live diversity falls to 54 and 59 unique songs, with top-ten shares of 93.44% and 87.40%. ID reversal changes 28.63% and 23.76% of cover slots. Off sets are already concentrated (top-ten shares 71.22% and 81.21%), consistent with the small genre-exact pools. Small per-song demand variance alone would not bring standards back into an excluded pool.

There are no live or recording exclusion events, no recording-unfilled events and no strong recording refusals in either enabled run, despite 113,479 and 109,455 new recording assignments. Avoidance therefore remains unexercised by actual exclusions in these trajectories; only the earlier synthetic fixture demonstrated that branch. Empty-songbook softening remains a material limitation. Live-unfilled event counts exactly match direct all-act missing positions, establishing that these are shortages rather than exclusion counts.

The prior unsigned short-set totals reproduce exactly: 11,488 → 13,078 and 12,778 → 15,811. Every short set in both populations and all four runs has a thin eligible cover pool; none occurs with enough eligible compositions. No future-dated pool entries were observed. The deficit differences are concentrated in changed pool supply, and not in observed refusal avoidance. Act-month denominators differ little within each affected genre, while pool shortages change substantially: for example 1001 LatinPop short sets rise from 2,026/5,626 to 5,051/5,616, and 1002 Classical from 2,514/9,215 to 5,669/9,272. This does not support explaining the entire increase as population-count noise. Why repertoire supply diverged remains open; these evolving worlds are not a fixed-cohort causal experiment.

Recording diversity behaves differently from live selection: enabled Country recording assignments use 1,092 and 1,125 unique cover compositions. Do not generalize the live-set collapse to all recordings or a durable AI repertoire store.

## Validation

All four processes exited successfully. Analysis checks passed for 209 weekly rows, all 48 months, per-act selected-cover counts, duplicate covers, finite bounded fit values, category partitions, legacy unsigned census agreement, and identical assembly/song-table/perception-table fingerprints. No invalid committed fit was recorded. The audit's two-week enabled smoke run retained byte-identical output in all 78 comparable existing CSV streams; the refusal stream was excluded because it now includes extra signed-roster census observations. The build passed with four pre-existing warnings. [Run fingerprints and smoke evidence](C:/Project/Label-Man/SimTools/PolarCensus209Hashes.json) preserve the baseline and final artifact hashes.

## Measurement

The legacy live census still covers eligible unsigned acts. Diversity streams cover those same sets plus all label rosters, deduplicated by artist ID. Each act uses the existing seed/year/month/artist private RNG key. Originals are placeholders and excluded from composition diversity. Traditional covers use the existing traditional/public-domain flags, then standards use the standard flag; recent hits use the existing RecentHit origin category and ordinary covers are the remainder. No new age threshold is introduced. Counts are monthly opportunities, not unique persistent repertoires. Off/on worlds evolve independently; comparisons are two trajectories per seed, not fixed-cohort treatment effects or independent annual replications.

Unique songs count compositions. Top-10 share and HHI use cover-slot counts; effective songs = 1/HHI. Max song reach is distinct acts selecting the most widely shared song divided by distinct acts observed. Annual/four-year metrics include repeated monthly selections.

Tie counterfactuals apply only to enabled non-rock live ranking. Exact scores use float equality with no tolerance. Candidate tie counts describe selected covers whose preliminary score is shared by another eligible candidate (before refusal filtering); song-ID ties additionally require the same exact-genre flag. Reverse-ID swaps the final ID ordering inside exact score/genre groups, applies the same resolved refusal gate, and counts replacements in set membership. Genre reversal separately reverses genre preference inside exact score ties. ID dependence establishes composition-choice dependence; it does not alone establish that category shares would change under another tie order, since counterfactual category counts are not recorded. Every audited base ordering must reproduce the actual selected cover set or the run fails. Near-boundary gaps describe the unfiltered preliminary ranked pool; ≤0.0001 is a diagnostic reporting band, not a selection rule. Rock uses bounded source sampling, so these counterfactuals are not applicable there.

Candidate fit rows are preliminary cover fits grouped by category, not resolved recording fits. Recording assignment rows cover new single/album-track assignments after audit subscription, excluding startup inventory and reuse/promotions. AI empty-songbook softening remains unchanged. Signed-roster sets are extra read-only audit observations, not simulated live engagements.

## Country and Easy Listening, all acts, 1960–63

| Seed / mode | Genre | Act-month sets | Cover slots | Unique songs | Top-10 share | Effective songs | Song-ID tie slots | Reverse-ID replacements | Max song reach | Songbook share of all slots |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 off | Country | 38032 | 115777 | 587 | 3.11% | 572.1 | n/a | n/a | 21.59% | 59.34% |
| 1001 off | EasyListening | 22661 | 68289 | 1093 | 71.22% | 15.6 | n/a | n/a | 95.68% | 1.69% |
| 1001 on | Country | 38237 | 116390 | 45 | 92.92% | 6.3 | 116378 | 115949 | 91.64% | 75.94% |
| 1001 on | EasyListening | 22786 | 68507 | 54 | 93.44% | 8.7 | 34101 | 19612 | 98.45% | 1.19% |
| 1002 off | Country | 38300 | 115682 | 601 | 3.24% | 576.0 | n/a | n/a | 21.99% | 58.26% |
| 1002 off | EasyListening | 21556 | 65997 | 1400 | 81.21% | 10.6 | n/a | n/a | 98.75% | 10.43% |
| 1002 on | Country | 38463 | 116086 | 62 | 90.39% | 7.0 | 115958 | 115200 | 73.93% | 75.29% |
| 1002 on | EasyListening | 21536 | 65820 | 59 | 87.40% | 9.4 | 41017 | 15636 | 99.32% | 0.69% |

## Unfilled positions and refusal accounting

The previous checkpoint off-side live-unfilled zeros were **not measured**: `ReportLiveFill` emits that event only with polar selection enabled. They did not establish an off/on difference. This census directly measures requested minus filled positions in both modes. Short sets (<3 songs) and missing requested positions (3–5 target) have different definitions.

| Seed / mode | Unsigned short sets | Unsigned missing positions | All-act short sets | All-act missing positions | Strong recording refusals | Recording exclusions | Recording unfilled |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1001 off | 11488 | 37920 | 14570 | 48416 | not evaluated | not evaluated | not evaluated |
| 1001 on | 13078 | 40739 | 16826 | 52305 | 0 | 0 | 0 |
| 1002 off | 12778 | 41814 | 16768 | 54654 | not evaluated | not evaluated | not evaluated |
| 1002 on | 15811 | 50192 | 20649 | 65091 | 0 | 0 | 0 |

The expanded refusal event stream includes both unsigned and signed census observations. The analyzer separates live and recording events by the planned ID prefix; the table above reports recording exclusions only. Direct set deficits are the comparable off/on live measure.

## Artifacts and reproduction

[Full monthly/annual diversity table](C:/Project/Label-Man/SimLogs/polar-census209-diversity.csv) and [analysis, fit/category totals, assignments, validation and input hashes](C:/Project/Label-Man/SimLogs/polar-census209-analysis.json). Raw run prefixes are `polar-census209-{off,on}-{1001,1002}` under SimLogs. Reproduce with `node SimTools/analyze-polar-census209.mjs`. The run helper uses `-Weeks 209 -Diversity`; existing prefixes cannot be overwritten.

Decisions remain open. This census does not select a repertoire target or explain an axis mismatch causally, and it does not replace the retained decade economics.

## Easy Listening candidate fits, all acts, 1960–63

| Seed / mode | Category | Candidate opportunities | Mean capability | Mean identity | Selected covers |
|---|---|---:|---:|---:|---:|
| 1001 off | standard | 575856 | 0.642567 | 0.999903 | 1265 |
| 1001 off | traditional | 125904 | 0.642816 | 0.999680 | 263 |
| 1001 off | recentHit | 233920 | 0.799238 | 0.956828 | 506 |
| 1001 off | ordinaryCover | 310423 | 0.843634 | 0.979145 | 66255 |
| 1001 on | standard | 927396 | 0.643467 | 0.999927 | 851 |
| 1001 on | traditional | 202764 | 0.645407 | 0.999921 | 233 |
| 1001 on | recentHit | 376720 | 0.799672 | 0.948175 | 1505 |
| 1001 on | ordinaryCover | 228677 | 0.871732 | 0.992469 | 65918 |
| 1002 off | standard | 3383325 | 0.650441 | 0.995530 | 7146 |
| 1002 off | traditional | 799695 | 0.674104 | 0.960222 | 1843 |
| 1002 off | recentHit | 1394340 | 0.783807 | 0.938393 | 3069 |
| 1002 off | ordinaryCover | 230202 | 0.809564 | 0.972179 | 53939 |
| 1002 on | standard | 712800 | 0.636691 | 0.999734 | 137 |
| 1002 on | traditional | 168480 | 0.640435 | 0.999571 | 460 |
| 1002 on | recentHit | 293760 | 0.795765 | 0.942647 | 1604 |
| 1002 on | ordinaryCover | 270914 | 0.853296 | 0.994848 | 63619 |

## Unsigned short-set attribution, 1960–63

Thin pools have fewer eligible songs than requested cover positions; this includes empty pools. The last column tests whether a short set occurred despite enough eligible cover compositions.

| Seed / mode | Genre | Sets | Short sets | Empty eligible pools | Thin eligible pools | Missing positions | Short sets with enough pool |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 1001 off | Classical | 9089 | 4532 | 93 | 6239 | 13010 | 0 |
| 1001 off | Comedy | 8439 | 2222 | 1219 | 3391 | 7609 | 0 |
| 1001 off | LatinPop | 5626 | 2026 | 235 | 4082 | 7437 | 0 |
| 1001 off | TexMex | 3707 | 1250 | 131 | 2647 | 4782 | 0 |
| 1001 off | Childrens | 6154 | 1458 | 781 | 2331 | 5082 | 0 |
| 1001 on | Classical | 8877 | 2207 | 51 | 5440 | 8906 | 0 |
| 1001 on | Comedy | 8434 | 1509 | 172 | 2498 | 4884 | 0 |
| 1001 on | LatinPop | 5616 | 5051 | 2504 | 5448 | 14398 | 0 |
| 1001 on | TexMex | 3672 | 3313 | 1304 | 3541 | 9194 | 0 |
| 1001 on | Childrens | 6193 | 998 | 96 | 1695 | 3297 | 0 |
| 1002 off | LatinPop | 5531 | 5337 | 4910 | 5465 | 15904 | 0 |
| 1002 off | Classical | 9215 | 2514 | 763 | 5496 | 9999 | 0 |
| 1002 off | Childrens | 6504 | 449 | 150 | 982 | 1818 | 0 |
| 1002 off | Comedy | 8393 | 636 | 185 | 1170 | 2317 | 0 |
| 1002 off | TexMex | 3948 | 3842 | 3480 | 3919 | 11732 | 0 |
| 1002 on | LatinPop | 5492 | 5492 | 5492 | 5492 | 16297 | 0 |
| 1002 on | Classical | 9272 | 5669 | 5238 | 7094 | 18919 | 0 |
| 1002 on | Childrens | 6529 | 341 | 42 | 687 | 1231 | 0 |
| 1002 on | Comedy | 8561 | 466 | 40 | 1114 | 1872 | 0 |
| 1002 on | TexMex | 3843 | 3843 | 3843 | 3843 | 11858 | 0 |

## Selected cover fit distribution, all acts, 1960–63

| Seed / mode | Genre | Capability p10 / p50 / p90 | Identity p10 / p50 / p90 | Capability > .95 |
|---|---|---|---|---:|
| 1001 off | EasyListening | 0.661971 / 0.880923 / 0.955688 | 0.905907 / 1.000000 / 1.000000 | 12.81% |
| 1001 off | Country | 0.583236 / 0.994186 / 1.000000 | 0.882415 / 1.000000 / 1.000000 | 68.47% |
| 1001 on | EasyListening | 0.776083 / 0.942774 / 0.999468 | 0.941742 / 1.000000 / 1.000000 | 45.57% |
| 1001 on | Country | 0.937344 / 1.000000 / 1.000000 | 0.988707 / 1.000000 / 1.000000 | 88.08% |
| 1002 off | EasyListening | 0.589669 / 0.854897 / 0.954738 | 0.894131 / 1.000000 / 1.000000 | 11.29% |
| 1002 off | Country | 0.656739 / 0.996569 / 1.000000 | 0.887777 / 1.000000 / 1.000000 | 71.31% |
| 1002 on | EasyListening | 0.759114 / 0.931552 / 0.998840 | 0.977671 / 1.000000 / 1.000000 | 40.88% |
| 1002 on | Country | 0.932525 / 1.000000 / 1.000000 | 0.987430 / 1.000000 / 1.000000 | 86.88% |

## Recording assignments (209-week event window)

These include album-only cuts and exclude startup inventory and reused/promoted performances. Recording uses bounded source sampling; live tie counterfactuals do not apply to it. The assignment window includes the final early-1964 week, while monthly live snapshots stop in December 1963.

| Seed / mode | Act genre | Cover assignments | Unique cover songs | Total assignments | Mean resolved capability | Capability > .95 |
|---|---|---:|---:|---:|---:|---:|
| 1001 off | Country | 2499 | 1070 | 8282 | not evaluated | n/a |
| 1001 off | EasyListening | 4673 | 1005 | 7234 | not evaluated | n/a |
| 1001 on | Country | 2758 | 1092 | 9036 | 0.959837 | 79.89% |
| 1001 on | EasyListening | 4687 | 1130 | 7250 | 0.940973 | 60.28% |
| 1002 off | Country | 2687 | 1059 | 8823 | not evaluated | n/a |
| 1002 off | EasyListening | 4368 | 987 | 6734 | not evaluated | n/a |
| 1002 on | Country | 2741 | 1125 | 8951 | 0.955588 | 76.82% |
| 1002 on | EasyListening | 4165 | 1112 | 6417 | 0.940525 | 55.32% |

## January 1960 position reconciliation

| Seed / mode | Unsigned missing positions | Unsigned short sets | All-act missing positions | All-act short sets |
|---|---:|---:|---:|---:|
| 1001 off | 222 | 71 | 402 | 131 |
| 1001 on | 222 | 71 | 402 | 131 |
| 1002 off | 327 | 100 | 559 | 179 |
| 1002 on | 327 | 100 | 559 | 179 |

## Similar song shapes: source diagnosis

The user observed several songs for one act sharing roughly the same shape with different intensities. The source supports a concrete mechanism: generated standards and pre-game hits vary composition quality, melodic strength, lyric quality, hook, rhythmic appeal, adaptability and originality, but do not populate explicit polar archetypes, word density, meter/form or lyric/vocal modifiers. EnsureComposition supplies genre/secondary-genre/tags only. SongProfileDeriver chooses the primary genre fallback for an unknown archetype, clones its axis vector and applies a small set of categorical modifiers. Those varied craft/hook fields and secondary genre do not shape the polar axes. Same fallback plus same effective modifiers therefore yields identical truth axes, regardless of song ID. Not every song in a genre must be identical: explicit archetypes, modifiers and completed reference-master taxonomies can distinguish them.

The widget draws supplied observation bands without access to truth. PolarPlayerPerception applies deterministic per-subject axis uncertainty, and PolarCoverResolver pulls arrangements toward the same act and resolves them to authored archetypes. These can explain some visible intensity/band differences on top of shared templates. The specific songs from the user's scouting session were not captured, so their precise displayed differences are not reconstructed. No artificial jitter, taxonomy assignment or profile-generation change was made during these census runs.

Source references: [catalogue generators](C:/Project/Label-Man/Systems/CompositionCatalogService.cs:92), [composition defaults](C:/Project/Label-Man/Systems/PolarSongMetadataService.cs:44), [profile derivation](C:/Project/Label-Man/Systems/SongProfileDeriver.cs:7), [observed bands](C:/Project/Label-Man/Systems/PolarPlayerPerception.cs:95), [arrangement resolution](C:/Project/Label-Man/Systems/PolarCoverResolver.cs:7), [widget rendering](C:/Project/Label-Man/UI/PolarComparisonWidget.cs:12).

The Easy Listening concern has a specific mapping mechanism to review: legacy Traditional Pop standards use the LushStandard fallback, while legacy Teen Pop hits use BrightPopNumber. Their vocal-nuance demands are .90 versus .35, lyric-delivery demands .55 versus .25, and ensemble demands .70 versus .60. These are existing table values, not proposed settings. The generated standard catalogue labels Tin Pan Alley/Christmas material TraditionalPop with EasyListening secondary genre, but derivation chooses the primary-genre fallback. Non-rock live ordering scores the reference/demo profile before its resolved arrangement; the resolved proposal is used for the refusal gate. Thus an adaptable standard can be ranked against its demanding fallback before an act-appropriate arrangement is considered. This identifies concrete mechanisms behind the observed ordering; it does not prove which table, metadata or ranking remedy is appropriate. The current census was completed without changing any of them.

## User direction for the follow-up

The user wants small variation around archetype templates so individual songs are not static copies. Treat each template as a typical shape and give each composition small persistent differences, created once and retained across scouting, reopening and save/load. The variation should preserve musical meaning and constraints such as explicitly instrumental vocal demands. The magnitude, distribution, axis relationships and interaction with reinterpretation remain implementation choices for review after this baseline census. No per-song variation was added during these runs. Easy Listening should retain the user's standards-led repertoire direction.
