# MidTier promotion test: 1964 label survival and participation review

## Scope

This review follows the preserved seed-1001 strict-gate stop from:

```text
d6-midtier-promotion-gate-through-1965-1001
```

against the retained control:

```text
d6-transition-envelope-decade-control-1001
```

It investigates the completed-1964 active-label and release-participation excess
only. It does not reinterpret the missing 1965 result and does not change the
new MidTier promotion evidence gates, Album policy, genre policy, market
clearing, or inherited acceptance bands.

The reusable offline analyzer is:

```text
SimTools/analyze-label-survival-participation.mjs
```

## Result

The strict test did not reach a completed-1965 row. Its first failure was the
inherited completed-year release gate: `4,367 / 3,330 = 1.311411x`, above the
inclusive `1.30x` ceiling.

This was not caused by excessive release cadence or release-roll success:

| 1964 metric | Control | Candidate | Candidate / control |
| --- | ---: | ---: | ---: |
| Mean active labels | 347.19 | 476.62 | 1.3728x |
| Distinct release participants | 264 | 443 | 1.6780x |
| Release decisions | 3,328 | 4,382 | 1.3167x |
| Decisions per mean active label | 9.59 | 9.19 | 0.9592x |
| Decisions per participant | 12.61 | 9.89 | 0.7847x |
| Capacity release success | 99.82% | 99.84% | +0.02 percentage points |

The candidate generated more releases because many more labels survived and
many more labels had at least one release-capable artist. The typical
participating label was less active, not more active: the participant
release-decision median was seven candidate versus nine control, and the
interquartile ranges were 3-13 versus 6-17.

## Active-label excess by origin

| Origin | Control mean active | Candidate mean active | Gap | Share of total gap |
| --- | ---: | ---: | ---: | ---: |
| Launch population | 269.54 | 354.25 | +84.71 | 65.45% |
| Runtime founded | 77.65 | 122.37 | +44.72 | 34.55% |
| **All labels** | **347.19** | **476.62** | **+129.42** | **100.00%** |

The launch-population gap was largely established before the MidTier promotion
policy under test could matter:

| Year | Control launch closures | Candidate launch closures | Difference |
| --- | ---: | ---: | ---: |
| 1960 | 36 | 16 | -20 |
| 1961 | 216 | 152 | -64 |
| 1962 | 45 | 41 | -4 |
| 1963 | 26 | 27 | +1 |
| 1964 | 13 | 14 | +1 |

The candidate therefore accumulated exactly 84 fewer launch closures in
1960-1961. Its 1964 mean launch-label excess was 84.71. Later-year closure
rates were nearly the same, so the 1964 launch excess is inherited early
survival, not a new 1964 promotion or death-rate event.

The first-year participation pattern explains how the survival divergence
started. In 1960 the two runs had nearly the same mean active population
(`595.35` candidate versus `591.54` control), and candidate launch labels made
only 2.9% more release decisions (`4,503` versus `4,377`). But those decisions
were distributed across 493 launch labels rather than 402. More of the launch
population therefore received release opportunities and revenue before the
financial closure wave of 1960-1961.

The production closure path is financial. Monthly status is driven by net
income, cash reserves, and consecutive loss months in
`CompetitorManager.UpdateLabelStatus`; `LabelLifecycleManager` then closes
bankrupt or qualifying dying labels. The artifacts support the causal sequence
`broader release participation -> broader revenue access -> fewer early
financial closures`, although they do not contain a dedicated counterfactual
closure-reason stream.

## Runtime-founded asymmetry

Both runs founded 72 runtime labels in 1964, so birth count did not create the
within-year gap.

| 1964 runtime metric | Control | Candidate |
| --- | ---: | ---: |
| Mean active | 77.65 | 122.37 |
| Active at start | 78 | 116 |
| Active at end | 78 | 130 |
| Founded | 72 | 72 |
| Closed | 73 | 59 |
| Release participants | 0 | 96 |
| Release decisions | 0 | 174 |

The zero control participation is a structural onboarding asymmetry:

1. Runtime labels enter with an empty roster.
2. With artist-population lifecycle enabled, a runtime label receives an
   operating target of one and is serviced by the daily talent market.
3. In the disabled control, the runtime initializer creates roster capacity but
   does not assign an artist. Refill is left to the frozen legacy scouting path:
   one global 8% weekly roll followed by a maximum of three label attempts.
4. Weekly releases skip every empty-roster label.

Thus runtime labels in control can remain financially active but cannot
participate in releases unless the heavily throttled legacy scout happens to
reach and fill them. None reached release participation in 1964. Candidate
runtime labels both survived better and 96 became release participants.

This mechanism contributed 44.72 of the 129.42 mean-active gap (34.55%), but 96
of the 179 distinct-participant gap (53.63%). It is a release-participation
asymmetry as much as a survival difference.

## Tier location

| Tier | Control mean active | Candidate mean active | Active gap | Control participants | Candidate participants | Decision gap |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Major | 7.00 | 6.42 | -0.58 | 7 | 7 | -53 |
| MidTier | 57.02 | 86.08 | +29.06 | 57 | 88 | +646 |
| Independent | 85.52 | 162.00 | +76.48 | 76 | 161 | +309 |
| Small | 135.15 | 154.08 | +18.93 | 61 | 126 | +153 |
| Boutique | 62.50 | 68.04 | +5.54 | 63 | 65 | -1 |

The largest active-label excess is Independent, not MidTier. Independent and
Small also account for most of the extra participating labels. This supports
the original recommendation not to weaken or retune the new
Independent-to-MidTier evidence gate from this result.

Tier participant counts can overlap when a label changes tier during the year;
the all-tier distinct total remains 264 control and 443 candidate.

## Interpretation and next decision

The failed strict gate is an upstream population-distribution result:

- early candidate talent-market coverage spread roughly the same 1960 release
  volume across many more launch labels, preventing a large early closure wave;
- that survivor advantage persisted into 1964;
- enabled runtime labels had a viable one-artist onboarding path while control
  runtime labels remained empty and nonparticipating; and
- almost-perfect release success merely converted the enlarged participating
  population into successful releases.

The next calibration, if authorized, should target the overall label
survival/participation surface rather than MidTier classification. It should
separate two questions before changing production behavior:

1. whether early launch-label talent-market service should be bounded so 1960
   release participation does not prevent 84 additional 1960-1961 closures; and
2. whether runtime-founded release participation belongs in the inherited
   control comparison as modeled, or needs an explicit population-neutral
   guardrail in the enabled path.

No production correction is implemented by this review. Any next experiment
should preserve the MidTier evidence gates and the protected Album/genre
surfaces, change one population/participation mechanism at a time, and rerun the
same completed-1964 release ceiling before spending another 1965 test.
