# Stature marketing mechanisms and weaker intervention — 2026-11-06

The subsequent source-specific census, two-year persistence/standing measurements and corrected synthetic-history cohorts are in [StatureOpeningOriginReview.md](StatureOpeningOriginReview.md).

The halfway blend reduced short-window chart displacement for the fixed opening cohort in both seeds, but did not consistently reduce paid launch spending. Keep it experimental. The existing decade traces do not identify following as the general cause of the biggest 1964 uplifts: several large winners retain their standing without its direct magnitude contribution, and some winners had no launch-envelope uplift. Synthetic opening history and prewarm remain plausible amplifiers, with important provenance limits in the retained data.

## Experiments completed

The new `--stature-marketing-blend=0.5` option applies `legacy + 0.5 * (standingBudget - legacy)` to eligible requested envelopes. Zero returns legacy exactly; one preserves the full reader. The switch requires the marketing reader, validates a finite weight in [0, 1], and is saved/restored. Older saves default to full strength. Ordinary funding, reserves and capacity gates still apply. `proposedBudget` continues to report the full standing-table value; `requestedBudget` reports the applied blend.

Two prespecified seeds, 1001 and 2002, each ran independent control/half and control/full pairs for 39 weeks, through September 30, 1960. July 1 calibration activation leaves 13 active chart weeks. All eight runs reached their exact completion markers with empty stderr. The pairs used the same assembly, calibration, C# content hashes and economic arguments. The 147 CSVs from the repeated controls matched byte for byte for each seed. All four arms per seed had identical week-zero charts and identical chart rows through week 26. This verifies a deterministic shared startup and pre-treatment chart path; it is not a saved mid-decade state/RNG restoration experiment.

| Seed / measure | Legacy | Half | Full |
|---|---:|---:|---:|
| 1001: active-window Top 10 slot-weeks, fixed week-zero chart acts | 50 | 46 | 44 |
| 2002: same measure | 22 | 19 | 16 |
| 1001: emitted launch marketing payments, whole run | $685,783 | $691,602 | $687,384 |
| 2002: same measure | $627,728 | $628,583 | $629,779 |
| 1001: paid follow-up tranches, whole run | $56,706 | $57,058 | $57,410 |
| 2002: same measure | $40,176 | $37,580 | $39,759 |
| 1001: LP units, whole run | 21,999,375 | 22,048,203 | 21,957,394 |
| 2002: same measure | 22,023,199 | 21,989,910 | 22,043,347 |

Each fixed cohort contains 97 acts. The active window has 130 Top 10 slots. Half closes half the full-reader cohort displacement in each seed, but the sample and window are too small to infer decade stability or historical improvement. Half increases launch payments by 0.85% in 1001 versus full's 0.23%; in 2002 those increases are 0.14% and 0.33%. Half therefore dampens the local request substitution without guaranteeing monotone spending effects once schedules, funding and later state diverge. About 93% of changed reads are still increases under half strength.

These runs change marketing only. The following-input screen below is offline; no follower-demand, live-draw or programme-audience mechanism was disabled.

## Fixed cohorts in the existing decades

The audit verifies identical initial shadow rows for 7,000 opening acts and identical complete week-zero chart rows across each original pair. Runtime acts are not matched by generated IDs. The largest five opening-act winners and losers in each of 1964 and 1968 are selected retrospectively by Top 10 slot-week change; the detailed file retains both successes exclusive to control and successes exclusive to treatment.

| Fixed week-zero chart cohort | Control | Treatment | Difference |
|---|---:|---:|---:|
| 1001, 1964 | 87 | 117 | +30 |
| 2002, 1964 | 102 | 36 | -66 |
| 1001, 1968 | 58 | 44 | -14 |
| 2002, 1968 | 25 | 17 | -8 |

These counts use the same acts in both arms, independent of later first-observed chart years. The 1964 seed split survives that correction. The direction changes over time in seed 1001. The machine-readable report also separates other opening acts and each arm's runtime population.

## Following sensitivity and trajectory evidence

For every available budget read of the selected acts, the audit reconstructs the observed band from its logged normalized components, regional evidence, reach and hysteresis boundary, rejecting mismatches. It then scales the two national following scores and regional following draw by 0.5 or zero. Sales scores, TV scores, reach, peer references and observed boundaries remain frozen. This is a local magnitude screen, not a sequential shadow replay. In particular, halving a clipped normalized score does not mean halving follower headcounts. Following-derived reach and any follower-origin sales remain in the evidence; the screen cannot attribute their origins or rule out earlier feedback.

| Selected acts' available 1964 single-launch reads | Control 1001 | Treatment 1001 | Control 2002 | Treatment 2002 |
|---|---:|---:|---:|---:|
| Reads | 53 | 40 | 50 | 50 |
| Baseline standing-table uplifts | 18 | 17 | 12 | 26 |
| Lower band after zeroing following magnitude | 12 | 5 | 5 | 4 |
| Baseline uplifts completely removed | 4 | 2 | 3 | 0 |

The half and zero screens produce the same aggregate counts in these particular cells. Most selected treatment uplifts survive. These are outcome-selected acts and repeated available reads, not a population effect estimate.

The detailed traces connect dated standing snapshots and route support to legacy/proposed/requested budgets, within-arm record identities, emitted launch payments, paid follow-up events, issued slate events, record-level chart response and following trajectories. Same-day budget reads are candidate links: existing logs do not carry a unique decision-event ID connecting every quote to its payment. Charted units measure chart-row sales, not all audience purchases. Payment joins use launch-quality and issued-slate identities as well as chart identities, so off-chart releases are included where identifiable.

Some instructive 1964 cases:

- **Janet and the Rebels (`artist_01672`, seed 1001):** Top 10 weeks rise 3 → 25. Both worlds issue five singles, while joined launch marketing payments across formats fall $1,012 → $346. The treatment's three uplifted single-launch reads all retain their bands when following magnitude is zeroed. On May 1, the request rises $335.81 → $559.69; normalized sales are 1.0 versus following 0.601, and record-route support clears the retained Superstar exit boundary. Its first observed decision-input/opportunity divergence is already November 11, 1960. A 1964 budget quote cannot explain the entire subsequent career difference.
- **Georgie Payne (`artist_00007`, seed 1001):** Top 10 weeks rise 2 → 23 with three → four singles. Neither arm has a 1964 launch-table uplift: both already read Superstar. Treatment also pays a $681 follow-up tranche. This gain does not require a contemporaneous legacy-to-standing launch uplift.
- **`artist_02638`, seed 1001:** Top 10 weeks fall 23 → 1 despite treatment uplifting all four single-launch reads in 1964. Joined launch payments across formats are identical at $3,155. Zeroing direct following magnitude does not lower those treatment launch bands. The first observed decision divergence is September 9, 1960. Higher requested envelopes did not prevent the loss.
- **`artist_01585`, seed 2002:** Top 10 weeks rise 0 → 24, with one → seven singles. All seven treatment launch reads uplift, and all retain their bands after zeroing following magnitude. The January 24 request is $5,776.52 → $7,220.65; normalized sales are 1.0 versus following 0.483. Record-route support alone clears the Superstar boundary. First observed decision divergence is November 25, 1960. This act is absent from the week-zero chart and first following log, so the biggest winner is not confined to that visibly privileged initial group.
- **The Record Hops (`artist_01538`, seed 2002):** Top 10 weeks fall 27 → 4. Control issues five singles in 1964; treatment issues none, so there is no 1964 treatment launch quote to interpret as a marketing cut. Earlier treatment launches include many uplifts. First observed decision divergence is August 26, 1960. Release opportunity and prior trajectory are central to this loss.
- **Rufus Vaughn and the Herders (`artist_02589`, seed 2002):** Top 10 weeks fall 17 → 1 despite two → four singles and four treatment launch uplifts. Only one of those uplifted launch bands falls under the zero-following screen, and its uplift is not completely removed.

These are descriptive trajectories in divergent worlds. The first recorded difference in opportunities/inputs is not necessarily the first causal state or RNG difference.

## What prewarm could be amplifying

The relevant startup source files match their content hashes in the original decade request. There are several separate mechanisms:

1. **Synthetic career history → seeded following.** `SeedOpeningFollowing` converts five years of public-history peaks into followers, with annual discounting, then allocates them to every region by population. This gives qualifying incumbents immediate geographically broad evidence. Following enters both record and conversion routes and regional draw/reach. It can therefore help the same synthetic heritage support a standing budget later.
2. **Seeded catalogue → chart prewarm → career/recognition credits.** Initial records get seeded awareness, radio and stock, then eight synthetic chart weeks. The replay explicitly credits chart entries, Top 40/10 hits and number ones through the ordinary artist handlers. LP catalogue awareness has an 0.85 floor. These are inherited opportunities and credentials before a live entrant has accumulated equivalent evidence. The old blanket national-stock shortcut has already been replaced by the ordinary regional shelf calculation; do not attribute these results to that removed behavior.
3. **Opening attachment.** Recent synthetic hits also seed teen attachment for CurrentHitmaker acts. This can feed later audience response independently of standing's direct following magnitude. It remains unchanged in this work.
4. **Room-history prewarm.** This seeds prior shows and room standing without immediate pay, attendance or audience gains. Because room standing can matter to later bookings, it should be distinguished from catalogue and following seeding rather than treated as an audience purchase grant.

The first following telemetry is emitted after the first completed live week. Each seed has 98 logged acts at that boundary, totaling approximately 12.44M and 12.45M followers. Those totals include opening stock plus early joins/losses, subject to the log floor; they are not exact synthetic seed totals or source lineage. Comparing this fixed first-observed group, 1964 Top 10 weeks are 161 → 162 in seed 1001 and 141 → 107 in seed 2002. The much larger seed-1001 week-zero cohort gain therefore is not simply a blanket gain for every act with initially visible following.

The evidence supports **prewarm as a plausible structural amplifier**, not an established causal explanation for the seed disagreement or proof that inherited audiences are inherently unfair. Neither turning off all followers nor deleting prewarm would isolate the standing-input mechanism: both would alter demand, credits, scheduling and RNG-sensitive paths together.

## Assessment and artifacts

Half strength is a useful experimental intensity setting. It reduces fixed-cohort displacement in this short screen while preserving reductions, but does not resolve the upward asymmetry or show stable economic improvement. The selected trajectories do not justify a broad causal following intervention yet. The next targeted prewarm test would need a startup provenance census and source-specific standing evidence carried through decay, with actual audience mechanisms preserved; the existing logs cannot recover those descendants exactly.

Build: zero errors/warnings. Headless marketing checks: 35 passed, including funding/payment and blend save/load. Python regression checks: eight passed across the marketing summary and magnitude-screen tests. Python compilation and PowerShell parsing passed. No defaults were changed to enable the experimental reader.

- Detailed decade evidence: `SimLogs/writing-calibration/stature-marketing-mechanisms-v5.json` (includes the parent report digest and enrichment digest).
- Short-window comparison and verification: `SimLogs/writing-calibration/stature-marketing-intensity-v1.json`.
- Eight run requests, output manifests, completion logs and four paired summaries: prefixes `stature-marketing-half-v1` and `stature-marketing-full-v1` under `SimLogs/writing-calibration`.
- New assembly SHA-256: `FF42C7539F18CA4AE97C183E7B2B9048A747852AB983707BDA9805489351963C`.
- Calibration unchanged: `80546B52E1CE0902FF44692FF642B45FCC6DA5A63D48663B9CDB295DB6450F19`.
