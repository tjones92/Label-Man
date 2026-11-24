# Veteran chart investigation

Status: the paired Recovery screen was stopped after 1965 at the user's request. Stature readers remain provisional and are not implemented here.

## Existing-run launch comparison

Using `records.csv` chart rows, assign each artist to its first observed Hot 100 year from the run start. Select the distinct singles that occupy any Top 10 slot in 1963 or 1964, then join `single-launch-attribution.csv`, `single-launch-quality.csv`, and `release-campaign-costs.csv` by record ID. Means below are unweighted means per selected record, not per chart week. Both years together contain 1,040 Top 10 slot-weeks, and all of them joined to launch records in both runs. The 1960 cohort is left-censored: it includes acts with careers before the simulation starts. A first observed chart year is not necessarily an act's formation or first release year.

| Metric | 2002: first seen 1960 (96 records) | 2002: first seen 1963–64 (79) | 1001: first seen 1960 (129) | 1001: first seen 1963–64 (64) |
|---|---:|---:|---:|---:|
| Top 10 slot-weeks | 393 | 285 | 586 | 197 |
| Quality | .779 | .796 | .789 | .785 |
| Launch awareness | .744 | .252 | .825 | .229 |
| Effective launch recognition | .747 | .065 | .843 | .036 |
| Initial launch stock | 142,745 | 11,248 | 159,314 | 7,668 |
| Of which follower advance orders | 133,899 | 5,457 | 150,179 | 2,189 |
| Remaining launch stock | 8,846 | 5,791 | 9,135 | 5,479 |
| Launch campaign impact | .579 | .466 | .614 | .454 |
| Launch momentum | .450 | .033 | .534 | .014 |
| Launch reputation | .511 | .059 | .618 | .053 |
| Marketing cost | $287 | $204 | $382 | $243 |
| Major-label share of these slot-weeks | 71% | 39% | 78% | 29% |

This conditions on reaching the Top 10, so it cannot tell whether any gap caused chart success. The follower-ledger join attributes most of the stock difference to advance orders. It uses the closed single launch book's stocked count, treating a missing book as zero; the residual is initial stock minus that count. This remains descriptive and does not show whether those added copies were the binding cause of chart positions. The comparison also mixes label tiers, genres, release timing and careers. Marketing cost is launch cost, not total campaign spending. Reproduce with `node SimTools/analyze_veteran_chart.cjs lpgate-decade-market-2002 branch-end-decade-market-1001`.

Of the 1960 cohort's selected slot-weeks, opening-archetype acts account for 157/393 in seed 2002 and 319/586 in seed 1001; the remainder is `None`. Thus seeded careers are material but do not account for the entire cohort excess. This is not a causal removal experiment, and `None` is not a proof of a procedural debut in 1960.

## Mechanisms checked in code

- With artist recognition on, launch awareness adds momentum, cooled reputation, and cooled recognition; radio adds recognition to quality, campaign impact and payola. The legacy rung-based stock cliff is replaced by a recognition multiplier capped at 1.3×. After normal regional stock and the launch stock scaling, follower advance orders are added; they can make initial stock much larger than that 1.3× multiplier alone explains. See `Data/SimulatedArtist.cs`, `Systems/CompetitorManager.cs`, and `Systems/Media/MediaAtmosphereService.FollowerDemand.cs`.
- Senior cooling **does affect launch access**: after 52 weeks without a Top 40 it reduces reputation and recognition reads at launch and steps down senior rungs. It does not cool momentum itself; its separately enabled half-life handles that. A Top 40 resets the cooling clock, so frequently charting acts retain full launch reads. See `Systems/SeniorCooling.cs` and `Systems/ArtistMomentumHalfLife.cs`.
- The provisional stature proposal could amplify an incumbent advantage if recognition or followers set promotion and contract treatment. It could also help a genuinely new breakout; the data here do not distinguish these. A future directive should shadow-log cohort, rung, stature components and launch decisions before making a reader causal, and measure the age composition of Top 10 slot-weeks as well as overall concentration. No stature mapping is authorized by the provisional handoff.

The old note that the first rung requires an unreachable national milestone is not a complete description of the present configuration. With artist population lifecycle enabled, `HasBreakthroughEvidence` accepts a contract Top 40 **or** the regional-breakout threshold. In these selected Top 10 singles, 45/79 (2002) and 42/64 (1001) of the 1963–64 cohort launched as `NewSigning`, before their later chart success. That shows new-signing status does not prevent a Top 10; it does not measure how many candidates failed to progress or prove that the rung has no effect on their odds. A first-rung funnel should be measured against all eligible launches before changing thresholds.


## Recovery experiment

The original control is `lpgate-decade-market-2002`, assembly SHA-256 `D22F957DB6E5C91900B39ED8B7B1D40DDC9874112A2396D006F29C60E7861B74`. Its saved request is replayed with the prior `lp-retail-v2` base profile, the same explicit flags and seed, except `--hit-history-signing-recovery` is omitted. Both new runs use assembly `BB1AE1C2EECFCB2D5D0BC7C5210AB867A780786BA683FDB6C53837859F03B576`; the Recovery-on run supplies a same-build control. Source changes since the archived build mean a comparison to the original alone is not a verified same-build ablation. The new media sources are disabled in both replays. Requests and logs are under `SimLogs/writing-calibration/veteran-recovery-{on,off}-market-2002-*`.

### Paired result, seed 2002

Both arms used the same current assembly and the archived control's explicit flags and previous base profile; the only explicit flag difference was Recovery. The processes were deliberately stopped in January 1966 once the full 1965 chart had been written. Each retained year has the expected 100 chart positions and 10 Top 10 positions per week. Error logs were empty before stopping. Because termination interrupted the simulation, the runs have no completion marker and other buffered or end-of-run outputs may be incomplete. Do not treat them as complete decade runs.

| Year | History: 1960 cohort Top 10 | Recovery on | Recovery off | On: 1960 cohort Hot 100 | Off: 1960 cohort Hot 100 |
|---|---:|---:|---:|---:|---:|
| 1961 | 264/520 (50.8%) | 402/520 (77.3%) | 408/520 (78.5%) | 3,149/5,200 (60.6%) | 3,378/5,200 (65.0%) |
| 1962 | 203/520 (39.0%) | 278/520 (53.5%) | 282/520 (54.2%) | 2,106/5,200 (40.5%) | 2,208/5,200 (42.5%) |
| 1963 | 127/520 (24.4%) | 224/520 (43.1%) | 245/520 (47.1%) | 1,785/5,200 (34.3%) | 1,826/5,200 (35.1%) |
| 1964 | 42/520 (8.1%) | 169/520 (32.5%) | 217/520 (41.7%) | 1,647/5,200 (31.7%) | 1,619/5,200 (31.1%) |
| 1965 | not recovered | 190/530 (35.8%) | 221/530 (41.7%) | 1,361/5,300 (25.7%) | 1,459/5,300 (27.5%) |

The one-seed ablation does not support removing Recovery as a solution to the early Top 10 age excess: the old cohort's share is higher with Recovery off in every measured year. This is the net effect in diverging simulated worlds, not the direct contribution of re-signed acts, and it does not establish Recovery's effect on 1968. In 1964 the new 1963–64 cohort's Top 10 weeks also rose (152 to 223), while the 1961–62 cohort lost share. The 1960 cohort's 1964 charting survival was 81/394 on and 73/392 off; weeks per surviving act were 20.3 and 22.2. New 1963–64 acts charting in 1964 averaged 8.9 and 9.4 weeks respectively.

## Recommended next causal screen

Test follower demand/advance orders first, alongside launch awareness and campaign impact, rather than treating the stock difference as a diagnosis by itself. `--follower-demand` changes both advance stocking and the first three weeks' purchase intent, so a whole-feature ablation answers the combined effect only; a second, narrow stocking-only versus demand-only probe would be needed to separate the channels. Preserve the same seed and configuration, assess 1964 and 1968 cohort slot shares and entrants' chart weeks, and monitor repeat hits, market volume and Major share. The launch comparison above is conditioned on successful singles, and neither this comparison nor such a feature ablation by itself validates a historical follower mechanism.

The recovered historical chart counts are reproducible with `node SimTools/analyze_veteran_history.cjs`: 42/520 1964 Top 10 weeks for the 1960 cohort, and 144/520 1968 Top 10 weeks for acts seen in 1960–64. Exact normalized artist-name matching can miss collaborations or naming variations.

The earlier Phase 4 1963–66 resumed-world screen (`CelebrityFanbasePhase4Notes.md`) already found that enabling follower demand raised 1966 repeat-hit share from 32.7% to 52.3% (seed 1001) and 36.3% to 49.2% (2002), closing part of a historical gap. That was a different profile and period, and did not establish this cohort-age outcome. A current-profile ablation would size the mechanism, not justify removing it without protecting repeat hits.

The new Recovery-on replay's `records.csv` rows for 1960–63 match the archived control byte for byte (33,044 rows); this is a partial parity check, not a claim that every output or later year matches.

## Follower launch books across all single launches

The stock gap is not confined to records selected for reaching the Top 10. The table describes **all** 1964 single launches in the two archived decade runs, grouped by the artist's first observed Hot 100 year across the available run. It joins the closed follower launch book by record ID and counts an absent book as zero; that includes launches below the 100-copy book floor and potentially other missing books. Means are per launch, including zero-book launches. Cohort assignment uses the whole observed chart history, so this is not a launch-time classification. `Other/unobserved` includes artists first charting after 1964 and those never observed charting. The 1960 cohort remains left-censored.

| Seed and cohort | Launches | Closed books | Mean initial stock | Mean stocked advance | Mean asked | Mean granted |
|---|---:|---:|---:|---:|---:|---:|
| 2002, 1960 | 411 | 411 | 60,420 | 53,773 | 53,534 | 51,737 |
| 2002, 1961–62 | 384 | 384 | 30,580 | 24,884 | 24,788 | 23,177 |
| 2002, 1963–64 | 690 | 564 | 12,327 | 6,887 | 6,839 | 6,434 |
| 2002, other/unobserved | 2,207 | 614 | 3,925 | 190 | 189 | 184 |
| 1001, 1960 | 405 | 405 | 75,369 | 68,497 | 68,334 | 65,716 |
| 1001, 1961–62 | 438 | 438 | 29,237 | 23,390 | 23,314 | 21,982 |
| 1001, 1963–64 | 754 | 613 | 11,244 | 5,727 | 5,698 | 5,465 |
| 1001, other/unobserved | 2,285 | 601 | 3,972 | 235 | 234 | 229 |

The granted amount is intent admitted by physical headroom **before** market clearing, not observed incremental sales. The large grant-to-ask ratio therefore does not establish that advance stock was necessary, or that every granted unit sold. Stock and demand are both derived from the same launch book; stocked copies need not equal asked units because of region reach, rounding and later disappointment. Differences between cohorts remain descriptive and include different label tiers, release opportunities and underlying artist histories. Reproduce with `python SimTools/analyze_veteran_follower.py lpgate-decade-market-2002` and `python SimTools/analyze_veteran_follower.py branch-end-decade-market-1001`.

The whole-feature replays `veteran-follower-{on,off}-market-2002` both completed 522 weeks with completion markers and empty error logs. Their requests use assembly SHA-256 `BB1AE1C2EECFCB2D5D0BC7C5210AB867A780786BA683FDB6C53837859F03B576`; aside from run name, their only explicit flag difference is `--follower-demand`. The switch also affects live draw and follow-up programme decisions, in addition to advance stock and release intent. A stock-only versus intent-only experiment still requires isolated controls; the existing switch cannot supply those estimates.

### Paired screen, seed 2002

The follower-off and follower-on runs each wrote all 52 1964 chart weeks (5,200 Hot 100 and 520 Top 10 slots). The follower-on run's 1960–64 `records.csv` rows match the earlier Recovery-on replay row for row (40,559 rows); the table uses that same-build replay, whose 1964 rows therefore also match the completed full-decade control.

| 1964 measure | Follower on | Follower off |
|---|---:|---:|
| 1960 cohort Top 10 slot-weeks | 169/520 (32.5%) | 80/520 (15.4%) |
| 1963–64 cohort Top 10 slot-weeks | 152/520 (29.2%) | 316/520 (60.8%) |
| 1960 cohort charting acts / mean chart weeks | 81 / 20.33 | 47 / 13.74 |
| 1963–64 cohort charting acts / mean chart weeks | 271 / 8.89 | 357 / 9.98 |
| Top 40 record entries with another act entry in previous 104 weeks | 152/275 (55.3%) | 112/268 (41.8%) |
| Major-label Top 10 slot-weeks | 323/520 (62.1%) | 304/520 (58.5%) |

An entry is a distinct record's first observed Top 40 week; a repeat requires a different record by the same act entering in the preceding 104 weeks. The 1960 opening chart is left-censored, affecting earlier repeat calculations. The on/off difference is the combined effect of the switch in diverging worlds, not the causal effect of stocking alone, or an estimate for another seed. It is consistent with follower demand materially contributing to the early-cohort excess, while removing the whole feature harms the repeat-hit objective. The historical 1964 old-cohort share is 8.1%; the off run remains above it and also has substantially fewer surviving old acts. Reproduce via `python SimTools/analyze_veteran_follower_screen.py veteran-recovery-on-market-2002 veteran-follower-off-market-2002`; ignore incomplete current years.

In 1965's 530 Top 10 slots, the 1960 cohort occupies 135/530 (25.5%) off versus 190/530 (35.8%) on; the 1960–64 cohort occupies 351/530 (66.2%) off versus 434/530 (81.9%) on. Repeat Top 40 entries are 126/278 (45.3%) off versus 154/267 (57.7%) on. The full-decade follower-on replay's 1960–65 chart rows match the earlier Recovery-on replay row for row (47,828 rows).

In 1966's 520 Top 10 slots, the 1960–64 cohort occupies 381/520 (73.3%) on versus 233/520 (44.8%) off; repeat Top 40 entries are 159/276 (57.6%) on versus 126/288 (43.8%) off. The 1960 cohort alone holds 161/520 (31.0%) on and 69/520 (13.3%) off.

In 1967, the 1960–64 cohort occupies 366/520 (70.4%) on versus 246/520 (47.3%) off; repeat entries are 149/273 (54.6%) on versus 132/274 (48.2%) off. Each arm has 5,200 Hot 100 and 520 Top 10 slots for that year.

In 1968, the 1960–64 cohort occupies 329/520 (63.3%) on versus 163/520 (31.3%) off, against 144/520 (27.7%) in recovered history. Repeat entries are 163/291 (56.0%) on versus 126/286 (44.1%) off. Acts first observed in 1967–68 occupy 105/520 (20.2%) Top 10 slots on versus 286/520 (55.0%) off; those charting in 1968 average 8.77 versus 9.85 Hot 100 weeks per act. Both arms have all 5,200 Hot 100 and 520 Top 10 slots. This is a one-seed combined-feature ablation, not a stock-only result or proof that the off configuration is historically right overall.

The completed annual rollups report 127,973,716 single units in both arms in both 1964 and 1968. Album units are 66.68M on versus 73.61M off in 1964 (+10.4% off), and 55.56M versus 57.46M in 1968 (+3.4% off). The concentration output's Major **family chart-unit** share is 65.1% on versus 60.9% off in 1964, and 63.8% versus 64.2% in 1968; this is not total-market share. Thus the improvement in cohort balance comes with materially lower repeat entries and an album-volume change, not a free calibration gain. Compare individual channels and another seed before considering a policy change.

## Isolated follower channels through 1964

Seed 2002 ran for 261 weeks with both follower channels, with advance stock disabled, and with purchase intent disabled. The ablations each add one switch to the same resolved profile. All three workers exited zero with exact completion markers and empty error logs. The analyzer checked matching build and source provenance, the hashes of the CSVs it consumed, and every Hot 100 position in all 52 weeks of 1964. The January 1, 1965 tally closes 1964. The chart rows, market-clearing rows and following exports through 1962 match the earlier interrupted runs for each arm.

On the first week's release of James Jackson's “Gina” (`gen_438`), advance orders add 97,319 copies to the base shelf stock of 8,268. The stock ablation leaves the 8,268 base copies and admits 21,481 units of follower intent across the launch window. The intent ablation keeps 105,587 initial shelf copies and admits zero follower intent. A grant records pre-clearing intent admitted by inventory. The 1964 clearing ledger separately attributes 14.11M LP buyer reallocations to follower demand in the control, 10.51M with advance stock disabled, and zero with purchase intent disabled.

| 1964 measure | Both channels | Advance stock disabled | Purchase intent disabled |
|---|---:|---:|---:|
| 1960 cohort Top 10 slot-weeks | 225/520 (43.3%) | 216/520 (41.5%) | 98/520 (18.8%) |
| 1963–64 cohort Top 10 slot-weeks | 187/520 (36.0%) | 179/520 (34.4%) | 278/520 (53.5%) |
| Repeat Top 40 entries within 104 weeks | 143/264 (54.2%) | 141/266 (53.0%) | 117/272 (43.0%) |
| Major-label Top 10 slot-weeks | 294/520 (56.5%) | 231/520 (44.4%) | 313/520 (60.2%) |
| Single units | 127,973,716 | 127,973,716 | 127,973,716 |
| Album units | 66.90M | 71.56M (+7.0%) | 72.47M (+8.3%) |

The stock ablation gave the 1960 cohort 75 fewer Top 10 slots in 1962, three fewer in 1963 and nine fewer in 1964. Its 1964 gains went to the 1961–62 cohort, which added 17 slots. This arm comes closest to preserving repeat entries, but offers limited progress on 1964 turnover and remains far above the 8.1% historical share for the 1960 cohort. The intent ablation delivered stronger turnover at the cost of 26 repeat entries and an 11.2 percentage-point drop in their share. Neither arm cleared both objectives in this one-seed screen. The 1968 benchmark and pooled 1965–69 share await a package trial.

[Verified comparison](../SimLogs/writing-calibration/veteran-follower-channels-261w-v2-2002-analysis.json).

## Turnover with repeat entries as a guardrail

This read-only analysis uses the two completed stature-decade controls, with both follower channels active and causal stature marketing disabled. It verifies the consumed chart and launch exports against their manifests. The historical comparisons use the recovered weekly Hot 100 through 1964 and weekly Top 10 in 1968. Historical artist joins use normalized names and remain sensitive to aliases and collaborations. Cohorts use first observed Hot 100 year, with the same opening-chart convention as the earlier screens.

### Which acts repeat

Among first observed Top 40 entries in 1964, a repeat has an earlier entry by the same act in the preceding 104 weeks. The second number counts repeat entries that also reach the Top 10 before year-end.

| Cohort | Recovered history | Seed 1001 | Seed 2002 |
|---|---:|---:|---:|
| First charted in 1960 | 52 repeats / 6 reach Top 10 | 71 / 40 | 58 / 35 |
| First charted in 1961–62 | 57 / 20 | 51 / 27 | 49 / 24 |
| First charted in 1963–64 | 87 / 27 | 29 / 11 | 36 / 16 |

The newest cohort has 270 charting acts in each control against 239 in the recovered chart. It has far fewer repeat Top 40 entries despite that breadth. Among its repeats, 38–44% reach the Top 10 within 1964, compared with 31% historically. The 1960 cohort converts 56–60% against history's 12%. A check on total repeat entries alone would miss this difference in who follows up and how high those records peak.

### How long records stay in the Top 10

| Year and cohort | History, records / weeks per record | Seed 1001 | Seed 2002 |
|---|---:|---:|---:|
| 1964, first charted in 1960 | 13 / 3.23 | 54 / 4.07 | 50 / 4.50 |
| 1964, first charted in 1963–64 | 72 / 5.07 | 47 / 3.70 | 60 / 3.12 |
| 1968, first charted in 1960–64 | 30 / 4.80 | 79 / 4.23 | 83 / 4.04 |
| 1968, first charted after 1964 | 75 / 5.01 | 63 / 2.95 | 59 / 3.14 |

These counts include titles carried in from the previous year. By 1968, the older cohort's individual Top 10 runs are already shorter than history's. Its excess comes from placing many more records. The newer cohort places fewer records and spends about two fewer weeks in the Top 10 per record. A general increase in post-peak decay would apply to both groups.

### Where newer follow-ups lose ground

The launch funnel uses the act's observed history strictly before release. A recent hit is a prior Top 40 entry in the last 52 weeks. Outcomes count entries within 26 weeks of launch. Across 1968, later-cohort acts with a recent hit issued 220 and 250 singles, versus 250 and 247 for the earlier cohort. Only 13.2–14.4% of later-cohort launches reached the Top 40 versus 29.6–30.8% of earlier-cohort launches. Among the releases that made the Top 40, 50–59% of later-cohort singles reached the Top 10 versus 58% of earlier-cohort singles.

The access gap persists within the recorded quality band from 0.70 through just below 0.80. The later cohort sends 12 of 87 and 12 of 109 launches to the Top 40, versus 32 of 111 and 32 of 122 for the earlier cohort. Across all recent-hit launches, the later cohort starts with 0.39–0.40 mean awareness versus 0.63–0.66. These are descriptive groups with different artists, labels, genres, campaigns and local coverage. The quality band does not isolate awareness as the cause.

The first follow-up to trace is a newer hit act's next single before it reaches the Top 40. The exports point to initial exposure, first regional support and early promotion as places to check. By 1968 both cohorts are already releasing comparable numbers of recent-hit singles. A separate test of Top 10 tenure should focus on why newer hits fade quickly once they get there. Both screens should retain the two follower channels and count repeat entries overall and by cohort.

Reproduce with `python SimTools/analyze_veteran_turnover.py --output SimLogs/writing-calibration/veteran-turnover-angles-v3.json`. [Analysis output](../SimLogs/writing-calibration/veteran-turnover-angles-v3.json).
