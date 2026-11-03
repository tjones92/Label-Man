# Commission refusals and material-search capacity

Status: observational follow-up to `ProfessionalMaterialFunnelScreen.md`. No
policy was promoted or experimentally changed. The accessible-sampling switch
remains opt-in. The findings do not measure the causal effect of removing a guard.

## Commission-arrival refusals

The observer now reports the first failing guard in `TryAiCommission` on an
unsuccessful professional repertoire arrival. The existing candidate-count
gate is earlier and is reported separately. Counts below are ordinary arrival
events across all 52 weeks; the same act can appear more than once. They are not
unique plans, the complete set of commissioning opportunities, or estimates of
profitable missed orders. Initialization arrivals are in the JSON separately.

| Arm, seed | Candidate-count gate | No eligible plan, minimum 2-week delivery | Pending/recent order | Owned delivery | Eight-fee reserve | No plan for actual delivery | Ordered |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Control 1001 | 1,250 | 1,440 | 44 | 24 | 16 | 7 | 94 |
| Control 2002 | 1,337 | 1,261 | 30 | 23 | 12 | 11 | 135 |
| Accessible 1001 | 1,252 | 1,450 | 38 | 21 | 18 | 6 | 106 |
| Accessible 2002 | 1,382 | 1,264 | 25 | 18 | 14 | 8 | 116 |

The first plan gate dominates the classified refusals, far ahead of the cash
reserve. It requires an open, matching single plan in a suitable preparation and
release window, not already prepared or being prepared, and without pending or
ready material search. This trace does not distinguish absence of any plan from
failure of those individual conditions. Pending and recent orders remain grouped;
the final booking check also remains grouped. Counts are conditional on earlier
gates, so they do not reveal overlapping reasons or the effect of changing one.

## Capacity and subsequent outcomes

These are distinct single intentions proposed through week 26, whose *first*
observed material review selected a professional song but failed the label
capacity guard. Each outcome is followed through 26 weeks after proposal, not
26 weeks after the block. A song may change on later reviews; a later start or
issue need not result from the original offer. Blocker counts describe pending
searches at the block and can count the same search for multiple plans.

| Arm, seed | Blocked plans | Later started any search | Later started same song | Issued any single | Issued same song | No issue by window |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Control 1001 | 40 | 16 | 5 | 27 | 4 | 13 |
| Control 2002 | 46 | 21 | 6 | 34 | 6 | 12 |
| Accessible 1001 | 112 | 54 | 19 | 74 | 14 | 38 |
| Accessible 2002 | 132 | 64 | 24 | 107 | 15 | 25 |

All these blocked plans had a preparation recorded during their window; not all
issued. In the treatment, the 43 same-song starts waited 96 weeks in total after
the first block, about 2.2 weeks per start. This average excludes songs that never
started and is not a general estimate of queue delay. Among 488 blocking-search
observations in treatment, 373 were cover searches and 115 professional;
control had 151 cover and 21 professional among 172. This does not establish
that cover searches were inappropriate or that a capacity increase is beneficial.
The JSON includes outcome counts by career, genre and craft band; small cells
should not be treated as reliable subgroup effects.

## Decision and next investigation

Do not relax the cash reserve or the capacity guard based on these data. The next
observer should decompose the minimum-delivery plan gate at the arrival time:
no matching plan, closed or already prepared/in preparation, material already
pending or ready, too little delivery lead time, or outside the horizon. Record
whether a suitable single appears shortly afterwards and its actual outcome;
retain the original ordered decision and distinguish mutually exclusive primary
reasons from overlapping conditions. Also distinguish pending from recent orders.
That would tell us whether an arrival is simply mistimed relative to a real plan
or whether autonomous commissions are mostly being prevented without a production
need. Only then choose a narrow independent intervention. Any such change needs
matched 104-week controls, complete 13-week chart and 52-week career windows,
additional seeds and a later-decade holdout as described in the previous screen.

## Reproduction and validation

The four `constraint-{control,treatment}-market-{1001,2002}` runs completed 52
weeks on one assembly, with combined workflow, funnel and concentration audits;
treatment additionally used accessible sampling. Each matches the corresponding
previous `sample-screen` run byte-for-byte across 108 comparable CSV files (the
expanded observer files are excluded). A 13-week observer-off/on pair matched all
105 comparable CSVs; its off run also matched the earlier observer-off build on
all 105. This is bounded evidence, not proof for all worlds or saves. Build
succeeded with zero warnings and errors; four material analyzer fixture tests
passed. The raw output and exact run flags/assembly hashes are in
`SimLogs/writing-calibration`. `material-constraints.json` is generated by:

```
python SimTools/analyze_material_constraints.py constraint-control-market-1001 constraint-control-market-2002 constraint-treatment-market-1001 constraint-treatment-market-2002 --weeks 52 --output SimLogs/writing-calibration/material-constraints.json
```

The analyzer rejects incomplete endpoints and mixed assembly hashes. The screen
is not a complete 52-week post-hit career cohort or later-decade test.
