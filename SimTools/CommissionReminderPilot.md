# Plan-reminder pilot

Status: opt-in experimental policy, not promoted to a default. This pilot tests
a narrow follow-up to `CommissionReminderFeasibilityScreen.md`; it does not
establish a career-balance improvement.

## Policy

`--writing-market-plan-reminders` requires the combined planned-assignment and
delivery-offer market workflow and anticipatory material search. An ordinary
professional arrival that reached the minimum-delivery plan gate and found no
matching single plan for its current label, artist and contract creates one
reminder. Repeated arrivals do not extend its eight-week lifetime. Initialization
arrivals do not create reminders.

At the label's weekly programme pass, after planning and before material review,
the reminder can try once on the first eligible, unprepared single for that
contract that has not already had a material review. It rechecks active candidate
count, current writer availability, the selected writer's delivery timing and
actual plan, and the existing owned-delivery, order, reserve and booking guards.
If the selected plan fails those checks, the attempt is consumed. An invalid
client is discarded; a reminder without an eligible plan expires after eight
weeks. The reminder does not reserve a writer until an actual commission is
booked. It does not require the professional song pool still to be empty, so the
pilot can buy new work even when a licensable song has since appeared.

A booked order is linked to that plan. The existing pending-commission guard
prevents preparation, and the pilot also holds off a competing material search
while its commission is pending. On delivery or cancellation, normal material
review resumes. Delivery offers and ordinary perceived selection remain in force;
no song is forced into a release. The saved market records the policy and pending
reminders; order and plan markers preserve one-time behavior. A save made with a
different reminder setting is rejected, so use a fresh world to switch the pilot.
The disabled policy leaves its new save fields absent.

## Matched 104-week results

Controls and pilot share the combined writing workflow, normal sampling and all
other flags. Seed 3003 is an additional seed beyond the two earlier screens. The
pilot made 114, 99 and 110 directly attributed orders, respectively; those are
not the same as the increase in total market orders.

| Seed | Total orders control → pilot | All writing fees control → pilot | Reminder orders | Mature reminder deliveries | Linked single used reminder song within 26 weeks |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 574 → 667 | $474,964 → $492,598 | 114 | 96 | 54 |
| 2002 | 629 → 644 | $487,932 → $486,610 | 99 | 88 | 51 |
| 3003 | 568 → 679 | $465,903 → $489,775 | 110 | 94 | 58 |

Across the pilot runs, 163 of 278 deliveries with a complete 26-week window were
used by their linked single. All 163 had a complete 13-week chart window from
issue, and 15 reached the Top 40 during it. These are descriptive outcomes for
pilot orders, not incremental hits or a comparison of like-for-like plans. The
323 directly attributed orders cost $92,815 in commission fees; total market
writing fees changed by much less because other orders and licenses also changed.
The whole-world order increases were 93, 15 and 111, showing that marked pilot
orders cannot be equated with incremental orders.

In the final 52 weeks, external-professional single launches changed from
282 to 301, 276 to 268, and 248 to 282. All single launches changed from 3,114
to 3,062, 3,022 to 3,038, and 2,933 to 3,049, respectively. These are matched
aggregate differences, not proof of what any individual commission displaced.

| Seed | Complete post-hit cohort with 1–3 further Top 40 titles, control → pilot | Fixed top-30 act chart-slot share, control → pilot | Failed labels, control → pilot |
| --- | ---: | ---: | ---: |
| 1001 | 44/113 (38.9%) → 37/115 (32.2%) | 39.4% → 40.2% | 114 → 131 |
| 2002 | 33/110 (30.0%) → 34/100 (34.0%) | 42.9% → 39.5% | 106 → 98 |
| 3003 | 40/111 (36.0%) → 32/113 (28.3%) | 40.1% → 41.8% | 122 → 113 |

Cohorts and retrospective leaders can differ across arms; small samples and
changed world trajectories limit interpretation. The career middle improved in
one seed and worsened in two; fixed top-30 concentration also improved in one
and worsened in two. Label-failure changes were mixed. Do not infer a general
causal benefit from the direct-use rate or these three worlds.

## Decision

Keep the reminder opt-in. The pilot demonstrates that it can route additional
paid work to plans and that many mature deliveries reach their linked singles.
It does not show a repeatable improvement in release volume, career breadth or
concentration, and it increased writing fees in two seeds. Do not promote it as a
default or a career-balance fix on this evidence. A later-decade holdout and a
more targeted mechanism hypothesis are still needed before a policy decision.

## Validation and reproduction

All six runs completed 104 weeks on assembly
`511E9709FE20879CE4636C7FFE39F9C62E9CE825DC8EB01A110041711D221AC5`.
The analyzer verifies matched flags and assembly, complete endpoints, reminder
lifetimes, no repeated plan attempts, event/order/plan reconciliation, no
preceding material review, no competing material review during the pending
commission, commission ledger reconciliation, material provenance, and the
existing no-early-preparation invariant. Career cohorts require 52 weeks after
the first observed non-opening hit; reminder deliveries require 26 weeks; linked
song chart outcomes require 13 weeks from issue. The 13-week chart window here
includes the issue week and twelve following weeks.

Build completed with zero warnings or errors. The native reminder fixture passed
bounded deduplication, save-policy mismatch, material-review exclusion,
one-time booking, pending-order refusal, expiry and marker persistence. A
13-week full-world save/serialize/load/resume round trip passed with the pilot
active, as did the 13-week pilot smoke audit. Nine commission/reminder analyzer
fixtures and two existing material-constraint fixtures passed. These checks are
bounded evidence, not a proof of every save or world. With the pilot off, the
1001 and 2002 controls each match the earlier combined-workflow run across all
108 comparable CSVs byte-for-byte.

Run `SimTools/RunWritingMarketCalibration.ps1` with `-Arm market -Workflow combined
-Weeks 104 -ConcentrationAudit -MaterialFunnelAudit` on seeds 1001, 2002 and
3003, adding `-PlanReminders` only for the treatment. Use:

```
python SimTools/analyze_reminder_pilot.py plan-pilot-control-market-1001 plan-pilot-treatment-market-1001 plan-pilot-control-market-2002 plan-pilot-treatment-market-2002 plan-pilot-control-market-3003 plan-pilot-treatment-market-3003 --output SimLogs/writing-calibration/plan-reminder-pilot-results.json
```

Manifests, logs and the full machine-readable results are in
`SimLogs/writing-calibration`; raw audit CSVs use the run prefixes under
`SimLogs`. Godot ran outside the sandbox because its .NET assemblies would not
load inside it. The later-decade holdout has not been run.
