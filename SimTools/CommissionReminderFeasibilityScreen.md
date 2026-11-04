# Commission reminder feasibility screen

Status: observational follow-up to `CommissionPlanRefusalInvestigation.md`.
No reminder was implemented and no commissioning policy changed. These are
matched 52-week runs with seeds 1001 and 2002, with and without accessible
professional sampling. They do not estimate the effect of a reminder.

## Method

Take ordinary arrivals in weeks 1–26 that failed the minimum-delivery plan gate.
For each, find the first eligible two-week plan in the next eight weekly
end-of-week snapshots. When several arrivals map to the same label, artist and
plan, count one pair and use its earliest observed eligible snapshot, with the
first associated refusal as the reason. This is not a count of every later plan;
a single act may contribute different plans. A suitable plan that appears and
vanishes between snapshots is not observed. The 26-week release window is
measured from the associated refusal.

At that snapshot the audit reads the existing owned-delivery, pending and recent
order, writer availability, eight-fee reserve, actual writer delivery-time plan,
delivery-year supplier activity and client checks. It picks the first available
writer using the same offer ordering as a professional arrival. It also records
the active repertoire count, for the existing fewer-than-two candidate gate.
These checks do not reserve the writer, check whether a fresh professional arrival
would find an existing song, or prove that a commission would be useful. Current
conditions at a weekly snapshot may differ from the conditions when a real
reminder would execute.

## Distinct opportunities and gates

| Arm, seed | Mature refusal events | Events with an observed plan | Distinct label/artist/plan pairs | Pass observed booking checks | Pass checks and candidate count |
| --- | ---: | ---: | ---: | ---: | ---: |
| Control 1001 | 852 | 184 | 166 | 144 | 129 |
| Control 2002 | 723 | 145 | 137 | 120 | 114 |
| Accessible 1001 | 870 | 189 | 167 | 138 | 125 |
| Accessible 2002 | 746 | 150 | 140 | 125 | 115 |

There were respectively 630, 554, 636 and 566 distinct label/artist identities
in the mature refusal events; 159, 129, 161 and 134 of those identities had an
observed eligible plan. Deduplication reduces the event count but does not make
these pairs independent: an artist can have multiple plans and arms are matched.
The candidate count was available for every pair. At each snapshot the writer's
actual delivery-time plan was the same plan as the observed pair.

Among all distinct pairs, the first failing observed booking checks were:

| Arm, seed | Reserve | Pending order | Owned delivery | Recent order only | No available writer or delivery-time plan |
| --- | ---: | ---: | ---: | ---: | ---: |
| Control 1001 | 18 | 3 | 1 | 0 | 0 |
| Control 2002 | 15 | 2 | 0 | 0 | 0 |
| Accessible 1001 | 24 | 4 | 1 | 0 | 0 |
| Accessible 2002 | 14 | 1 | 0 | 0 | 0 |

Pending and recent flags overlap: every pending case was also recent; there
were no recent-only cases at these snapshots. These are ordered first-failure
counts, not the prevalence of each condition among every arrival. Reserve is a
real remaining constraint, not a reason to remove the reserve guard. No writer
availability, delivery-time plan, delivery-year activity or client check failed
first in this sample.

For the narrower original no-matching-plan reason, 105, 72, 109 and 79 events
mapped respectively to 91, 69, 92 and 75 pairs. Of those pairs, 74, 55, 74 and
59 passed both the observed booking checks and candidate count. None had a
material-funnel review or search start recorded by the snapshot week. For all
reasons combined, only 3, 5, 2 and 4 pairs respectively had a recorded material
review by that week; week-level logs do not resolve ordering inside a week.

The observed pairs subsequently issued within the common refusal-based window
in 129/166, 113/137, 127/167 and 115/140 cases. Those releases occurred without
a reminder and are not evidence that a commission would improve them.

## Decision and next step

A limited reminder is worth an independent pilot: the no-matching-plan subgroup
contains a meaningful number of distinct plans that appear before recorded
material review and pass the observed gates. The data do not establish an
incremental order, usable song, release, profit or chart benefit. The measured
pass count is at most a proxy for triggers in this sampled cohort, not an upper
bound on all opportunities, because weekly snapshots can miss eligibility.

A narrow pilot could remember an ordinary no-matching-plan refusal for at most
eight weeks, once per label/artist/contract and plan; when a matching plan appears
before material work, reconsider through the existing candidate, writer, order,
cash and actual-delivery guards. It should expire without commissioning if those
conditions fail and must not reserve a writer or buy work without a plan. Define
when the reminder runs within the week, avoid duplicate bookings, retain normal
save/resume behavior, and measure incremental orders, material sources, plan
outcomes, costs and displacement against matched controls. This proposal is not
implemented or validated here. Use the 104-week matched controls, complete chart
and career windows, additional seeds and later-decade holdout required by the
prior screen before drawing policy conclusions.

## Reproduction and validation

The four `reminder-candidate-{control,treatment}-market-{1001,2002}` runs used
`RunWritingMarketCalibration.ps1`, 52 weeks, combined workflow, concentration
and material-funnel audits; treatment also used accessible sampling. Each run
matched its corresponding prior `constraint` run byte-for-byte across all 110
comparable CSVs. This is bounded evidence of observer non-interference, not a
general proof. Build succeeded without warnings or errors; five commission
analyzer fixture tests passed. Runs used the local Godot engine outside the
sandbox because its .NET assemblies did not load inside it.

```
python SimTools/analyze_commission_reminders.py reminder-candidate-control-market-1001 reminder-candidate-control-market-2002 reminder-candidate-treatment-market-1001 reminder-candidate-treatment-market-2002 --output SimLogs/writing-calibration/commission-reminder-screen.json
```

The analyzer rejects incomplete endpoints, mixed assembly hashes, inconsistent
arrival/diagnostic ordering and missing or duplicate gate snapshots. The JSON,
run manifests and raw CSVs retain the per-reason and overlapping observations.
