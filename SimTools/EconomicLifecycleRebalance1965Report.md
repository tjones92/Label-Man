# Economic and lifecycle rebalance through 1965

## Outcome

The accepted source completed the strict seed-1001 1965 gate:

```text
run=d6-economic-lifecycle-rebalance-through-1965-1001
weeks=314
exit=0
completion=CHART_AUDIT_COMPLETE
```

The catastrophic stream is header-only. Both D5 probe groups and D6 fixed
probes 1-69 passed. The final source also passes:

```text
git diff --check
dotnet build "Label Man.sln" --no-restore
```

The build has only the inherited unused
`ChartManager.OnGenreMomentumChanged` warning.

## Root cause

The Single-lane repair redistributed the economy rather than merely changing
aggregate Single yield. In the rejected predecessor's completed 1965:

- Major Single units were 22.58M versus 83.59M control.
- Major Album units were 0.72M versus 4.80M control.
- Every non-Major tier was above control, hiding the Major collapse in aggregate
  Single units.
- Major format decisions flipped from 233 Album / 68 Single in control to
  82 Album / 179 Single.

Three mechanisms reinforced each other:

1. Bounded Single discovery removed the former multiplicative hit feedback, but
   retained anti-concentration tier scalars that heavily penalized Majors.
2. Lane-specific responsive memory treated a promo-plus-Album project as two
   independent release opportunities and admitted only half of its component
   value at the format gate.
3. The broader lower-tier revenue distribution protected too many marginal
   labels from financial closure, inflating active population and participation.

Album scheduling/drop counts therefore looked superficially close while the
high-yield Major Album and promo ecosystem had disappeared.

## Implemented repair

### Competitive label exit

The enabled quarterly review now:

- begins at 6 operating months for launch labels and 9 for runtime founders;
- uses a 0.08 stable base probability;
- treats one recent chart as partial evidence (0.35 multiplier) and two recent
  charts as the safe harbor;
- retains profitability and runway protection without making either absolute;
- applies additional MidTier/Independent competition pressure;
- remains deterministic and isolated from Godot's global RNG.

The existing evidence-gated Independent-to-MidTier promotion remains intact.

### Tier-aware realized demand

The live Single and Album paths now have separate tier-allocation scalars. These
restore national-label promotion/distribution leverage after bounded discovery
while reducing the inflated lower-tier unit footprint. The disabled path keeps
the legacy scalars and behavior.

### Album project economics

Promo projects retain 75% rather than 50% of component project value at the
eligibility gate. Major and, more weakly, MidTier Album programs also receive an
era-dependent portfolio-commitment multiplier. This represents a multi-release
LP program's longer horizon without contaminating orphan-Single, promo-Single,
Album-component, or total-project memory lanes.

## Completed-1965 economics

| Metric | Candidate | Control | Ratio |
| --- | ---: | ---: | ---: |
| Single units | 159,174,388 | 164,692,558 | 0.966494 |
| Album units | 10,711,437 | 11,129,114 | 0.962470 |
| Total units | 169,885,825 | 175,821,672 | 0.966239 |
| Gross | $184,296,722 | $190,463,837 | 0.967621 |
| Label net | $105,581,334 | $104,522,383 | 1.010131 |
| Market net | $105,581,334 | $104,843,478 | 1.007038 |
| Successful releases | 2,938 | 3,336 | 0.880695 |
| Logged Album decisions | 1,706 | 1,834 | 0.930207 |
| Completed Album drops | 1,624 | 1,780 | 0.912360 |

Album units improved from the rejected predecessor's 0.705294x to 0.962470x.

### Album units by tier

| Tier | Candidate | Control | Ratio |
| --- | ---: | ---: | ---: |
| Major | 5,257,864 | 4,873,214 | 1.078930 |
| MidTier | 3,965,678 | 3,999,598 | 0.991519 |
| Independent | 899,433 | 1,442,547 | 0.623508 |
| Boutique | 395,309 | 594,491 | 0.664953 |
| Small | 193,153 | 219,264 | 0.880914 |

The previous systemic Major Album collapse is repaired. The remaining mix is
concentrated toward Major/MidTier and away from the lower tiers, while aggregate
Album units and economics are close to control.

## Lifecycle and participation

| 1965 metric | Candidate | Control | Ratio |
| --- | ---: | ---: | ---: |
| Mean active labels | 264.06 | 338.75 | 0.7795 |
| Release participants | 252 | 254 | 0.9921 |
| Release decisions | 2,926 | 3,327 | 0.8795 |
| Decisions / mean active | 11.08 | 9.82 | 1.1283 |
| Decisions / participant | 11.61 | 13.10 | 0.8865 |

MidTier averaged 63.83 active labels versus 55.51 control. Its absolute count
gap fell from +24.32 in the predecessor to +8.32. Its share is still high
(24.17% versus 16.39%) because total active population contracted below control.

The active-count miss is concentrated in launch population: 172.11 candidate
versus 260.72 control. Runtime-founded population is modestly high at 91.94
versus 78.04. Despite the lower raw population, distinct release participation
is almost exact.

## Rejected timing follow-up

Moving the launch competitive-review minimum from 6 to 9 months was tested as:

```text
d6-economic-lifecycle-rebalance-launch9-through-1965-1001
```

It was rejected at the completed-1964 catastrophic gate:

```text
metric=scheduledAlbumProjects
candidate=885
control=1322
ratio=0.669440
band=[0.70,1.30]
```

That change was reverted. The retained source is the strict-pass source above.

## Decision

The inherited Album, Single, total-unit, release, and economic gates are repaired.
The raw active-count difference is not an acceptance defect. The control's
additional nonparticipating labels are an artifact of the pre-lifecycle behavior
that the lifecycle redesign intentionally removed. The economically meaningful
comparison is release participation: 252 candidate labels versus 254 control,
with the accepted release, format, unit, and finance gates above.

A follow-up dormant-catalog-shell experiment was therefore rejected and fully
reverted. Reintroducing non-operating labels merely to reproduce the control's
338.75 raw count would restore the obsolete behavior without improving market
participation. Future lifecycle checks should treat active-label count as a
descriptive diagnostic, not a control-ratio target; participation, release
throughput, tier composition, and economic output remain authoritative.
