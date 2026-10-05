# Gospel live repertoire repair

October 5, 2026. Development seeds 1001 and 1002; two fresh 91-week trajectories, ending September 29, 1961, with 21 monthly observations each.

The user request authorizes implementation and the 65–70% inherited / <=2% secular live-set target. Gemini’s attached text and the previous directive report are background documents, not new instructions. The target is a game calibration supplied by the user; the unsourced Gemini estimate is not treated as a measured historical rate.

## Implementation

Only Gospel has a configured affinity in Data/PolarRepertoireTable.json. Its inherited source probability is 0.77, with a stable per-act spread of +/-0.10 from the saved repertoire seed. This probability applies to the remaining cover slots after an act’s own originals have been reserved; it is not a guaranteed fraction of every short set. Other genres default to the existing selector.

The live selector chooses inherited versus contemporary material before choosing a song within that source. Each source uses the existing fit bands, personal song preferences, ballad balance and refusal rules. A source with no eligible candidates falls back to the other source. This prevents a growing catalogue of other acts’ originals from gaining more chances solely through its size. A deterministic check expands contemporary candidates from one to fifty and verifies that the selected source stays unchanged.

The Gospel book remains 350 compositions, and the access algorithm is unchanged. The retained later-frame median remains 77 accessible book songs per act. No shape offsets, fit formulas, market centers, recording source weights or population rules were retuned.

Creation-time context authoring now covers repertoire families, publishing-office songs, both artist-original creation paths, and live original stubs. Gospel acts’ originals are sacred even when their project genre differs. The table explicitly distinguishes sacred hymn forms from secular Christmas forms. Covers retain the original composition context and rights. Player repertoire saves retain the new context field.

Autonomous Gospel live selection excludes explicitly secular or mixed material. Older/imported unknowns stay unknown and usable; they are not relabelled sacred. Therefore the secular guarantee below applies to the fully tagged fresh worlds, not to unidentified material in an older save. Player-selected covers keep their own context.

## Fresh results

| Seed | Observed live slots | Inherited | Inherited share | Secular | Unknown |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 14070 | 9667 | 68.71% | 0 | 0 |
| 1002 | 11949 | 8010 | 67.03% | 0 | 0 |

Both full-window inherited averages meet 65–70%, and both secular shares are 0%. These are repertoire observations, not counts of actual bookings or songs newly learned each day.

| Quarter | Seed 1001 all | Seed 1002 all |
| --- | ---: | ---: |
| 1960-Q1 | 66.98% | 65.02% |
| 1960-Q2 | 70.94% | 67.91% |
| 1960-Q3 | 71.61% | 67.05% |
| 1960-Q4 | 69.15% | 67.92% |
| 1961-Q1 | 68.34% | 67.72% |
| 1961-Q2 | 67.91% | 66.27% |
| 1961-Q3 | 68.08% | 66.88% |

Seed 1001 Q2 and Q3 1960 exceed the upper target (70.94% and 71.61%). Twelve of fourteen all-act quarter cells are inside the band. Signed/unsigned cells have additional small-sample outliers; the full breakdown is in GospelLiveRepairValidation.json. The first fixed starting frame for seed 1002 is also below the band (63.80%, 163 slots). These exceptions remain visible; no hard per-set quota or retrospective band change was used.

## Concentration and working repertoire

| Seed | Distinct inherited songs | Top-10 share now | Retained v2 top-10 share | Median distinct songs per act across observations | P90 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 249 | 14.63% | 19.28% | 5 | 7 |
| 1002 | 229 | 26.90% | 35.36% | 5 | 7 |

The per-act median top-10 share is 100%: a typical act repeatedly uses a handful of standards. Across acts the book remains much broader. Seed 1002 still has appreciable concentration, so this is a measure to retain, not a claim that a universal concentration ceiling has been established. Retained versus fresh trajectory comparisons are descriptive because resume boundaries differ.

A smooth score-plus-preference alternative was tested and rejected. In the retained later frames it raised top-10 share from 15.65% to 27.27% for seed 1001 and from 24.81% to 34.63% for seed 1002. Smooth ranking alone also lowered inherited share. Normal play therefore keeps the existing within-source bands. AuditSmoothRanking is only an ablation hook.

## Shape v1/v2 source fit comparison

The unchanged prior fixed-frame CSVs are split by source in GospelSourceFitComparison.json. Values below are means over Gospel act/candidate pairs, not unique songs or independent historical observations. ArtistOriginal here means an admitted original available as another act’s cover; an act’s own live original stubs are preallocated and have no contested fit read.

| Later-frame seed | Source | Capability v1 -> v2 | Moment v1 -> v2 |
| --- | --- | ---: | ---: |
| 1001 | Gospel Standard | 91.68 -> 91.72 | 83.32 -> 82.64 |
| 1001 | ArtistOriginal | 87.69 -> 87.32 | 83.08 -> 82.67 |
| 1002 | Gospel Standard | 91.62 -> 91.42 | 87.91 -> 87.07 |
| 1002 | ArtistOriginal | 87.94 -> 87.57 | 86.29 -> 85.53 |

At startup every source’s moment remains 0.5. Later, both standards and originals lose some moment fit. The hypothesis that only standards shift away while originals are unaffected is not supported by these means. Capability changes are small. Full means and descriptive >=0.8-bin shares, including frozen versus resolved readings, remain in the source-fit artifact.

The separate source choice addresses the selection interaction without changing valid song shapes to force a target. Within-source v2 differences still influence which particular songs an act picks. The fixed-frame sacred-tagging fixtures show inherited share falling under v2 from 63.53% to 57.85% (1001) and from 56.62% to 47.41% (1002); adding the source affinity reaches 69.32% and 68.10%. Those fixture baselines include the new context restriction and should not be substituted for the old report’s unrestricted v1/v2 totals.

Older retained snapshots lack authorship context. The diagnostic probe explicitly annotates procedural fixtures and creates v2 offsets on probe-only copies of stored v1 truth. These annotations are not a save migration or evidence about historical lyrics. Fresh fully tagged trajectories are the primary acceptance evidence.

## Cohorts and other genres

standardsLed and traditionalRevival are behavioral labels: LiveRepertoire.WritingPropensity uses them to set original counts (0.20 and 0.25 respectively). They are also used for reporting; they do not directly add a standards scoring bonus. Gospel inheritedRepertoire currently sets writing propensity to 0.30. This repair leaves those values and the rare all-original exception unchanged.

Folk, Country, Blues, Easy Listening and Classical were included in the recheck. The retained-frame panels match slot-for-slot between v2 baseline and source affinity for every non-Gospel act. No other genre affinity was added. Fresh bounded-panel results are below; these aggregate different cohorts and are not full-population historical pass/fail tests.

| Genre | Seed 1001 slots / inherited | Seed 1002 slots / inherited |
| --- | ---: | ---: |
| Folk | 662 / 42.60% | 672 / 46.43% |
| Country | 644 / 15.99% | 673 / 34.18% |
| Blues | 633 / 23.70% | 664 / 29.67% |
| EasyListening | 652 / 32.21% | 681 / 39.06% |
| Classical | 674 / 49.55% | 673 / 37.15% |

The pre-existing Folk and Easy Listening inherited-share misses remain separate work. Their neutral selector was checked, not retuned to hide those misses.

## January pool jump and exits

The event logs resolve the previously source-unknown unsigned arrivals: all 66 (seed 1001) and 60 (seed 1002) are EnabledInitialReserve acts returned by prospect-rotated on December 23, 1960. The 52-week latent rotation causes the wave, which first appears in January’s monthly census. The whole Gospel arrival totals are 72 and 66, including six and five runtime formations and one additional rotated seed-1002 act already signed by observation. These are arrivals into the observed pool/roster, not lifetime births.

The jump is broad: Rock and Roll has 674/637 arrivals, Folk 234/229, Country 261/283, Classical 127/123 and Easy Listening 196/194. Full source/cohort/date breakdowns are in GospelPopulationJump.json.

The first Gospel prospect-search-expired events are June 30, 1961, visible in July’s census. AdvanceProspectSearchWeekForProbe expires a spell after 78 Seeking weeks and returns the act to Latent while lifecycle status remains Active. This is a search-spell rule, not a calendar-formation tenure rule or automatic retirement. Formerly contracted acts can also enter reserve after 78 continuously unowned weeks with no live record/pending project. Terminal exit additionally requires prior contract history and either three completed spells or a lead member aged at least 35, with no live record/pending project. ArtistManager.cs was investigated but not changed.

## Verification and reproduction

Final build succeeds with zero errors and four existing warnings. Both development seeds pass GospelPreferenceChecks plus the existing Directive 3 shape, constraints, save/load, cover/scouting, refusal, player-perception and cache checks. New checks include composition and live-item context save/load, old unknown defaults, retained cover rights/context, unchanged 350-song book, stable access, deterministic picks, act spread, neutral genres and independence from contemporary catalogue size.

The two sandboxed Godot launches crashed before initialization; the installed-runtime runs completed. Fresh trajectories completed 91 weeks each, with checkpoint round-trip checks on resumes. Later source edits preserve unknown compatibility and save the live-item context field; they do not change the fully tagged fresh worlds’ choices. Each slice retains its actual assembly/table fingerprint. No hold-out seed was run.

Build with `dotnet build --no-restore -v quiet`. Run the installed Godot .NET console with `--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --gospel-preference-check` (repeat seed 1002). Use `--polar-directive3-check` for existing regressions.

For fresh trajectories, run `./SimTools/run-polar-directive3-trajectories.ps1 -Seeds 1001 -RunTag YOUR-FRESH-TAG` and repeat for seed 1002 with a different tag. The analyzer currently reads the completed gospel-live-1001 and gospel-live-1002 manifests; update those paths to analyze a new run. `node SimTools/analyze-gospel-source-fit.mjs` and `node SimTools/analyze-gospel-population-jump.mjs` analyze retained evidence. `node SimTools/analyze-gospel-live-repair.mjs` requires complete 91-week manifests, and `node SimTools/write-gospel-live-repair-report.mjs` writes this report.

GospelLiveRepairValidation.json contains full scoped quarterly results, fixed-frame ablations, source fits, population trace, invocation records and final source fingerprints. Earlier gospel-pref-* additive-bonus experiments and gospel-source-start-* 0.75-spread experiments are superseded; their artifacts are retained.
