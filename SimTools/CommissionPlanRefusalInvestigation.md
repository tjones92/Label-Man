# Commission plan refusal follow-up

Status: observational screen; no commissioning or material policy changed. This
uses one matched seed (1001), with and without accessible professional sampling,
for 52 weeks. It does not estimate an intervention's effect.

## Arrival-time reasons

Ordinary professional arrivals that reached and failed the minimum two-week plan
gate are classified once, in this order: no matching single for the same label,
artist and contract; all such plans closed; all remaining plans prepared or in
preparation; all remaining plans have pending or ready material; all remaining
plans lack two-week lead time; all remaining plans lie outside the eight-week
horizon. This priority is an accounting convention, not a causal attribution.
The underlying conditions can overlap, including across different plans. The
JSON includes separate overlapping flags. Initialization events are separate.

| Primary reason | Control | Accessible sampling |
| --- | ---: | ---: |
| No matching plan | 726 | 743 |
| Matching plans all closed | 504 | 497 |
| Preparation started or complete | 84 | 70 |
| Material pending or ready | 88 | 93 |
| Insufficient delivery lead | 38 | 47 |
| Outside horizon | 0 | 0 |
| Total | 1,440 | 1,450 |

These are arrival events, not distinct artists or independent opportunities. A
closed plan may have issued or been cancelled; its presence does not imply a
current production need. Each event first passed the owned-delivery and
pending/recent-order checks and the earlier candidate-count gate. These counts
therefore cannot tell us how often an order would actually be affordable,
available or accepted if the plan gate changed. Pending and recent orders remain
grouped in this screen.

## Subsequent plans and outcomes

For ordinary refusals in weeks 1–26, inspect weekly end-of-week snapshots for the
first plan meeting the existing two-week eligibility rule in weeks 1–8 after the
arrival. Follow that first observed plan until 26 weeks after the arrival. A
snapshot can miss short-lived eligibility, and another plan may also exist. A
later release is not attributed to the refused offer. Repeated events can point
to the same plan; these are event-weighted observations, not unique-plan counts.

| Primary reason | Control: events / eligible / issued | Accessible: events / eligible / issued |
| --- | ---: | ---: |
| No matching plan | 574 / 105 / 85 | 589 / 109 / 84 |
| Matching plans all closed | 167 / 49 / 41 | 172 / 45 / 36 |
| Preparation started or complete | 46 / 12 / 5 | 37 / 15 / 9 |
| Material pending or ready | 49 / 15 / 11 | 52 / 16 / 11 |
| Insufficient delivery lead | 16 / 3 / 2 | 20 / 4 / 3 |

For the no-matching-plan cohort, the first observed eligible plan appeared
about 4.3 weeks later on average, conditional on appearing. Of the later issued
singles, 13 of 85 in control and 6 of 84 under accessible sampling used external
professional material. Other sources dominated. This is descriptive: the
professional material need not have come from the original arrival, and outcomes
are conditioned on observing a suitable plan and then an issue.

## Decision

Do not loosen the minimum lead-time or preparation/material guards on this
screen. Insufficient lead time is a small primary category, and most no-plan
arrivals do not have an observed eligible plan in the next eight weeks. A
short-lived, non-binding reminder to reconsider an arrival when a suitable plan
actually appears is a more targeted hypothesis than commissioning immediately
without a plan. The evidence does not show that such a reminder would lead to an
available writer, an affordable booking, a useful song, or a better outcome.
Before choosing it for a longer controlled test, examine distinct artist/plan
pairs, timing relative to subsequent material selection and the remaining order,
writer and cash guards, and distinguish pending from recent orders. Avoid treating
repeated arrivals as separate potential gains.

## Reproduction and validation

The two runs used `RunWritingMarketCalibration.ps1`, `-Workflow combined`,
`-ConcentrationAudit`, `-MaterialFunnelAudit`, seed 1001 and 52 weeks; treatment
also used `-AccessibleProfessionalSampling`. Manifests and generated JSON are
in `SimLogs/writing-calibration`. Each run matches its corresponding prior
constraint run across all 110 comparable CSVs byte-for-byte. This checks these
runs, not all saves or seeds. The build succeeded with no warnings or errors;
two analyzer fixtures passed. The sandboxed Godot process could not load its
.NET assemblies; the successful runs used the same local engine outside the
sandbox.

```
python SimTools/analyze_commission_plan_refusals.py plan-control-market-1001 plan-treatment-market-1001 --output SimLogs/writing-calibration/commission-plan-refusals.json
```

The analyzer validates endpoint, assembly consistency and the ordered join
between diagnostic rows and the original arrival trace. The diagnostic is
observational and only active with the material-funnel audit. Weekly eligibility
is sampled at a different point in the simulation week from the arrival event.
