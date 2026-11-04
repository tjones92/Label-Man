# Timely follow-ups inside the release programme

Current scope: the author selected a fresh **1960–63**, four-seed comparison using the complete `lp-retail-v2` profile,
with two worlds in parallel. The raw-programme v1/v2 experiments below are diagnostics, not evidence about that
calibrated profile. The current targeted candidate is selection priority plus `--programme-follow-up-material`;
the earlier work-queue experiment is retained separately and is not bundled into this candidate.

Experimental implementation, 2026-11-01. The author's scope is timely follow-ups for established acts while preserving
LP plans. This is a scheduling screen, not adoption of the full stature design or a demand recalibration.

`--programme-follow-up-scheduling` requires `--label-release-slate` and is off by default. It changes only the ranking
of eligible acts competing for an existing **single** intention. Existing single clocks, conflicting intentions,
material judgement, preparation, cash, promotion and issue checks still apply. It adds no slots, forces no format
conversion, cancels no LP intention and changes no LP rank, LP spacing, master, project or delivery rule. Scheduling
different singles can still change later cash and market outcomes; preserved LP rules do not promise identical LP units.

The existing allocator already gives recent Top 40 acts quarterly follow-up urgency. Its general waiting score caps at
one after 52 weeks, however, so an overdue act can remain behind a consistently higher-scoring labelmate. This screen
adds one score point per quarter **after** the 13-week follow-up reference, multiplied by current draw:

```
draw = max(recent Top 40 ? 1 : 0, current single audience orders / national leading audience orders)
waitingPriority = max(0, (single gap - 13) / 13) * draw
```

The single gap includes earlier open single intentions, preventing the horizon from repeatedly treating the same act
as overdue. All audience evidence is read at planning time, never projected into the future. Audience orders are a
proxy from the existing young/adult following and existing singles purchase rates, not actual dealer orders for an
unmade record. Their national denominator has a 100-copy floor, so negligible followings do not become full strength
in an empty world. Following contributes only when `MediaFollowerDemand.Causal` is active; observing/shadow media
remains observational. Without causal follower demand this is a chart-follow-up timing screen alone. There are no new
random draws, demand rates, economic budgets, save fields, or default-profile flags.

This deliberately tests overdue pressure rather than changing royalty tables, contracts, career classifications,
recognition writers, retention or other Phase 5 stature readers. Unbounded waiting can eventually outweigh a material
ranking advantage; existing preparation judgement can still hold weak material. That tradeoff requires a screen.

Use `--label-decision-audit` in both matched arms. The choices CSV appends `followUpScheduling`, `waitingPriority`,
`audienceOrders`, and `nationalAudienceOrders`; the selected intention also records its extra priority and audience
proxy in its evidence. `analyze_programme_waits.py <choices.csv>` reports selection episodes by format and recent-hit
cohort, including censored episodes. It follows audit emission order and measures reviewed eligible opportunities,
review-week waits and intended-week spans. It does not impute eligibility between reviews, and selection is a plan,
not an issued follow-up. The recent-hit cohort is classified at each episode's first eligible review; it is not a
fixed matched set of acts across treatments. Join the release slate for actual delivery delays, postponement/cancellation reasons and
preservation of existing LP intentions. Do not interpret selected-only averages without the censoring counts.

Acceptance still requires matched multi-seed screens at the accepted Phase 4 demand rates: repeat hits, distinct
entry and slot-week concentration, actual single follow-up gaps, waiting/censoring, LP intentions and deliveries,
LP units, and label finance. A native check or short smoke run is not historical calibration. No adoption is implied.

Initial verification: `dotnet build --no-restore -v quiet` passes with zero warnings/errors; four wait-analysis tests
pass; `SaveLoadRoundTripRunner.tscn -- --label-release-slate --label-release-capacity
--programme-follow-up-scheduling --label-release-programme-check` passes 52 native checks. Native execution needed to
run outside this environment's sandbox because Godot crashed during sandbox startup; its logs and user-data
directory were redirected into `SimLogs`. The check includes overdue selection pressure, LP exclusion, current
audience arithmetic and the programme's existing finance/clock/persistence invariants.

## First matched screen

Seed 1001, 53 weeks from `media-p3-1001-1963` (1963-01-04 through 1964-01-10). Both arms use the Phase 4 reproduction
flags plus `--label-release-slate --label-release-capacity --label-decision-audit`; only treatment adds the new flag.
This is not the ordinary-game profile or a replay of the original no-programme Phase 4 control. Both processes exit
zero, print completion markers, and contain no error/exception markers. Manifest and assembly hash:
`SimLogs/followup-v1-manifest.json`; analysis: `SimLogs/followup-v1-report.json` and its adjacent summary script.

| Measure | Control | Treatment |
|---|---:|---:|
| Opening LP slots with identical act allocation | 427 | 427 (all identical) |
| Recent-hit eligibility episodes selected / censored | 429 / 51 | 452 / 58 |
| Mean review weeks to selection, selected recent-hit episodes | 2.112 | 1.810 |
| Mean opportunities to selection, selected recent-hit episodes | 4.851 | 4.531 |
| Issued single intentions | 3,850 | 3,840 |
| Issued LP intentions | 1,798 | 1,811 |
| Observed repeat-single issue gap, all acts, mean / median weeks | 14.83 / 12 | 14.65 / 11 |
| Observed issue gap ending in a hit-follow-up intention, mean / median weeks | 17.76 / 16 | 18.16 / 16 |
| LP units in the 53-week window | 67,826,272 | 68,809,184 (+1.45%) |
| Single units in the 53-week window | 165,394,110 | 164,675,214 (-0.43%) |

The first screen improves selected recent-hit planning waits and preserves every opening LP allocation without
reducing aggregate LP deliveries. It does **not** establish improved actual hit-follow-up timing: those observed gaps
are unchanged at the median and slightly longer on average, on different samples (108 control, 98 treatment).
The recent-hit episode censoring share also rises from 10.6% to 11.4%. All-act selected planning waits rise slightly
(7.76 to 7.85 weeks). These are descriptive changing cohorts; actual gaps exclude the predecessor before the audit
and follow-ups after its end. A larger screen should distinguish allocation from preparation and delivery delays,
using matched acts and censoring, before choosing any further scheduling change. No demand increase or default
adoption follows from this result.

## Pipeline follow-up and delivery experiment

The author approved tracing matched acts, changing the delayed stage, then a four-seed 209-week screen with complete
chart history. `analyze_programme_pipeline.py` fixes the cohort to artist IDs that are Established/Star/Superstar and
eligible in both opening planning reviews. In v1 that is 110 acts. It retains cancelled and unfinished intentions,
separates future intentions beyond the endpoint, records proposal/master/issue weeks and estimates the opening
spacing clock from the first audited gap. Clock-derived eligibility is a spacing bound, not evidence of uninterrupted
artist availability; clocks whose eligibility predates the run are explicitly left-censored. The historical single
clock seeded by the programme is not a reconstructed missing discography. No opening act in these old resumes was
classified hot by the newly enabled cadence clock, so the fixed cohort uses career standing rather than a newly
formed, post-treatment hot cohort.

For these same 110 acts, v1 control/treatment have 551/561 intentions, 395/400 issued, and 68/70 cancelled. Mean master
lateness relative to the intended session week is 1.666/1.628 weeks. The leading logged postponement pressure is
preparation capacity (812/839 distinct item-weeks), followed by promotion congestion (394/417). These counts include
repeat postponements, not independent records or additive causes of delay. Moving selection earlier alone did little
to these stages. Full traces are in `SimLogs/followup-v1-pipeline.json`.

`--programme-follow-up-delivery` therefore requires `--programme-follow-up-scheduling` and changes precedence among
existing single work. Once an act's actual single clock reaches 13 weeks, current draw supplies work priority, with
the same overdue pressure thereafter. The mixed work list retains every LP's exact position; only single items move
between single positions. Main-queue work must have reached its preparation date (unrecorded) or due date (prepared)
before receiving this precedence. Idle-slot work retains the existing early-work permission. Every capacity, material,
rights, cash, promotion, master lead and spacing check remains in force. No new work slot, refund, demand rate, LP
override or save field is introduced. This tests the queue diagnosis; it does not assume promotion shortages disappear.

The updated build passes with no warnings/errors. Native programme checks pass 56 cases, including LP position
preservation, unavailable future work, zero-priority stability and quarterly timing. Eight Python tests cover selection
episodes, fixed opening cohorts, cancelled/unfinished work, early issue and LP-campaign singles fulfilling a follow-up. `RunProgrammeFollowUpScreen.ps1` runs four matched seeds
(1001/2002/3003/4004), programme-only control versus selection-plus-delivery treatment, 209 weeks each, sequentially.
It holds the assembly hash fixed and records world hashes, exact flags, completion status and annual checkpoints.
Both arms retain the accepted follower rates and add `--chart-history`. `analyze_programme_screen.py` writes its own
report directory and leaves the accepted Phase 4 calibration artifacts intact. The screen is not the game-default
profile; it uses the Phase 4 media flags with the programme in both arms. Results must be assessed before adoption.

After the screen began, the author requested two worlds in parallel. `RunProgrammeFollowUpParallel.ps1` adopts the
running world without restarting it and keeps at most two active, each new world with its own user-data directory.
The serial launcher's existing other-Godot guard stops its queue after its current world completes; this is an
orchestration handoff, not a failed simulation. The parallel manifest is `SimLogs/followup-v2/parallel-plan.json`;
the serial manifest retains the original adopted world's arguments. The model assembly and experimental flags stay
fixed. Future resumes reject a changed assembly/window/seed set.

## Profile correction and fresh-world material test

The first raw-programme v2 control completed 209 weeks on seed 1001, but its 81.67% repeat share, 34.02% leading-act
entry share and 58.11% leading-act slot-weeks exposed an important setup error: adding bare slate/capacity switches
to the stripped media world omitted the calibrated programme's work budgets, material judgement, turnover and retail
policy. That control is a bridge diagnostic, not the existing LP programme's baseline. Remaining raw runs were
stopped cleanly (treatment 1001 after 20 weeks, control 2002 after 35). No four-seed v2 result exists.

The loader correctly rejected applying the complete current profile to the old snapshots: their saved LP demand
scale is incompatible. This check was retained. The author chose fresh 1960–63 worlds instead of generating new
1963 warm starts. The new historical comparison uses 1963 repeat share and the 1961–63 entry/slot-week window:
64.14%, 24.06% and 44.46%, respectively, on the same as-printed historical credits. The earlier 1966 / 1962–64 figures
remain a different reference, not a direct before/after comparison.

The full-profile native checks passed 57 cases before the material change; a two-week world round-trip and resumed
play also passed, including real gzip I/O. The national audience maximum is now cached once per weekly programme
pass (with programme-state identity invalidation on restore), since following updates after that pass. This removes
repeated label-by-label scans without changing the inputs or random draws.

A fresh 13-week v3 full-profile pair completed in parallel. Its fixed opening cohort has 46 established acts. Each
arm issued 61 single intentions, with no cancelled prepared master. Main postponements were material holds (62/64
distinct item-weeks), then cash (10/10); preparation-capacity refusals were absent for this cohort. Merely reprioritizing
work did not change those shipments. Files: `SimLogs/followup-v3-fresh-preflight/pipeline.json` and `parallel-plan.json`.

The targeted `--programme-follow-up-material` experiment therefore requires selection scheduling and project
judgement. When an allocated single can be prepared for its intended date and the act's actual single clock reaches
the quarterly reference at the ordinary one-week master lead, a current recent-hit act—or an Established-or-higher
act with a causal follower single order proxy at least the existing 100-copy book floor—may choose its own session
instead of continuing to hold a weaker heard shortlist until the window's final weeks. It uses the same routine
session and session costs as the existing late-window fallback. Heard material still passes the unchanged read
comparison, and a pending paid search is allowed to deliver before this early decision. LPs, work capacity, rights,
cash, promotion, spacing and master lead are unchanged. The project audit records `ownSessionTiming` as
`follow-up-due` or `window-close` without adding saved model fields.

The material candidate build is clean and passes 61 full-profile native checks. Eight Python tests cover the analysis,
including LP-campaign singles satisfying a follow-up. The intended final launch is:

```
SimTools/RunProgrammeFollowUpParallel.ps1 -RunTag followup-v4-fresh -Weeks 209 -DefaultProfile -FreshWorld -MaterialDeadline
```

This holds the complete existing LP profile and accepted causal media/follower rates in both arms; only treatment
adds selection scheduling and the material deadline. The separate `--programme-follow-up-delivery` queue flag is off.
`analyze_programme_screen.py --tag followup-v4-fresh --comparison-year 1963` produces the matching historical table,
seed-paired changes, LP outcomes, finance and fixed-cohort traces. No adoption is implied by native or short checks.

The fresh v4 13-week check completed in both arms. On the same 46 opening established acts, material-hold
postponements fell 62 -> 56 item-weeks, issued single intentions rose 61 -> 62, and cancelled prepared masters stayed
at zero. This supports exercising the candidate in the full screen, not adopting it. The full fresh 209-week matrix
uses tag `followup-v4-fresh`, four seeds, `-DefaultProfile -FreshWorld -MaterialDeadline`, and at most two active worlds.

## Completed four-seed screen and disposition

All eight fresh 209-week worlds completed successfully with the same assembly and full LP profile. Complete chart
histories were validated. No further scheduling worlds are planned, and the candidate remains off by default.
These figures supersede interim two- and three-seed readouts.

| Measure | Matched history | Control | Material candidate |
|---|---:|---:|---:|
| Repeat hits, 1963 | 64.14% | 60.78% | 61.28% |
| Top 5% acts' new Top 40 entry share, 1961–63 | 24.06% | 36.33% | 34.72% |
| Top 10% charting acts' slot-weeks, 1961–63 | 44.46% | 49.33% | 49.19% |

Repeat-hit changes by seed are +10.08, -5.17, -3.64 and +0.74 percentage points: the mean +0.50-point gain is not
consistent across seeds. Entry concentration falls in every seed but remains substantially above history. The
earlier interim rise in slot concentration does not persist in the four-seed mean.

For 188 matched opening established acts, the first due-follow-up wait capped at 52 weeks falls from 2.91 to 2.12
weeks; actors with no issue within 52 weeks fall from five to three. Mean preparation lateness across seeds falls
from 1.11 to 0.88 weeks. The pooled observed repeat-release gap barely changes (10.32 to 10.33 weeks). These are
small operational improvements without a reliable chart-career gain.

Aggregate 1960–63 LP units fall 0.96%; individual seed/year changes range from -6.46% to +4.68%. Issued LP intentions
total 44,660 versus 44,665, while single intentions total 60,254 versus 60,535. LP capacity and finance rules remain
in force; maximum cash reconciliation error is 2.33e-10. This fresh screen does not inherit the old resume's opening
LP-plan comparison.

The concentration investigation finds a thin repeat-career middle and excess transient lower-chart acts. Using a
fixed 81 leading acts, slot shares are 43.95%/43.91%, close to history's 44.46%; the model's broader charting population
makes its percentile group larger. Entry concentration remains excessive even at fixed headcount. Most observed
new-hit acts already receive prompt follow-up releases, so the next useful trace is conversion and continuity of
support after a hit, alongside ordinary six-to-twelve-week allocation and predecessor timing. Keep LP commitments
and demand calibration fixed; these observations do not isolate a causal reader or justify a blanket demand change.

Evidence: `SimLogs/followup-v4-fresh/parallel-plan.json`, `analysis/report.json`, `analysis/chart-pairs.csv`,
`analysis/economic-pairs.csv` and `concentration/`. See `ProgrammeConcentrationInvestigation.md` for methods and
diagnostics. The final candidate build passed 61 native programme checks and eight Python analysis tests.
