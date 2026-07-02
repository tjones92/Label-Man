# Capability tier transitions and distribution-deal audit

Measured 2026-07-02 on Godot 4.7 Mono. Short runs are 52 weeks with seeds 1001/1002/1003. The long run is seed 1001 for 520 weeks (1960 through December 1969).

## Phase 0

- `distributionStrength` remains a compatibility alias over `ownedReach + borrowedReach`; capability reads `ownedReach` only.
- Lifecycle remains the runtime tier-mutation path, after monthly competitor finance/status processing.
- The existing deal model and skim/advance route were intact.
- Generated capability ranges under the directive weights were: Small 0.296-0.534 (mean about 0.415), Boutique 0.367-0.634 (0.501), Independent 0.403-0.686 (0.545), MidTier 0.584-0.846 (0.715), Major 0.790-0.996 (0.893). Runway contributes a flat 0.20 at generation, so sustained-time and structural gates carry the differentiation.
- Historical template starting tiers were retained. No template-tier retune was made.

The applied birth targets are 600 (1960), 620 (1961-62), 650 (1963-64), 675 (1965-66), 645 (1967-68), and 625 (1969+), with a six-birth monthly catch-up ceiling. Newborn labels are registered with chart and competitor systems, use viable cash ranges, and can sign artists recycled from closed labels.

## Implemented behavior

- Capability-based quarterly promotion and hysteretic demotion across all five tiers, including Boutique's no-money-only-demotion exception.
- Pull and push distribution offers, weighted distributor selection, asymmetric terms and acceptance, advances, monthly owned-reach reinvestment, exit/renew/absorb resolution, and distributor/client failure cleanup.
- Acquired labels are excluded from revenue, releases, monthly processing, scouting/operating-label queries, and their roster and active records transfer to the acquirer.
- Tunable push probabilities/ramp, skim ranges, masters rate, reinvestment rate/cost, dependency thresholds, and birth curve.
- Deal ledger, label directory, label-name finance fields, capability/reach fields, and annual concentration telemetry.

## Phase attribution and 52-week guardrails

The capability-only rebuilt run shifted annual units by +0.2%, +1.1%, and -2.8% versus the finance-pass baseline. The accepted full mechanic produced:

| Seed | Annual units | Week-52 active records | Operating labels | Indie-family chart share | New Top-100/week |
|---:|---:|---:|---:|---:|---:|
| 1001 | 154,810,982 | 1,693 | 579 | 13.96% | 20.31 |
| 1002 | 158,812,169 | 1,661 | 578 | 8.70% | 20.46 |
| 1003 | 165,617,751 | 1,689 | 580 | 12.44% | 20.48 |

All annual-unit runs remain inside 150-180M. Active records remain below the approximate 1,800 baseline in all three seeds. In the full seed-1001 trace, Independent age-14 charting was 11/1,020 and Boutique was 4/429; charted zombies were zero. Closed Top-40 median life was 9 weeks, one week below the prior 10-13 guard. These two soft misses were reported rather than tuned through unrelated chart levers.

Repeated accepted seed-1001 runs were byte-identical across every emitted CSV.

## Deal lifecycle validation

Forced harness cases pass for exit, renewal, and absorption at dependency values 0.094, 0.493, and 0.925 respectively. The absorption case asserts roster transfer, record roll-up, `Acquired` status, and exclusion from `GetOperatingLabels`.

In the accepted long run:

- Pull deals: 6 signed, average initial reach 0.413, 0 absorbed.
- Push deals: 3 signed, average initial reach 0.749, 1 absorbed (33.3%).
- Push deals therefore deliver both higher day-one reach and a higher eventual absorption rate.

## Long-horizon curve and economy

| Year | C4 | C8 | Indie-family chart share |
|---:|---:|---:|---:|
| 1960 | 36.12% | 53.14% | 13.96% |
| 1962 | 35.03% | 52.79% | 12.86% |
| 1966 | 44.01% | 61.41% | 8.13% |
| 1967 | 36.04% | 55.83% | 13.96% |
| 1968 | 38.97% | 58.52% | 12.31% |
| 1969 | 39.09% | 61.37% | 14.46% |

The curve is noisy, but concentration rises clearly from the 1967 trough through 1969. Other calibration iterations reached indie-family shares above 20% and one reached 28.1%, confirming that the mechanic can close the indie-major gap without destabilizing a 52-week economy.

Decade-scale total market units exceed the 150-180M annual band: the accepted mechanic reaches 218.0M in 1966 and 229.6M in 1969. A lifecycle/deal-disabled 520-week control already reaches 209.0M and 211.0M in those years because the existing annual release-growth system compounds across the decade. The mechanic delta is +4.3% in 1966 and +8.8% in 1969. This is material but not catastrophic; correcting the underlying long-horizon release-growth curve is separate from this directive.

## Historical-label read

`labelName`, archetype, historical flag, initial tier, finance, reach, capability, and deal fields now make the named labels directly queryable. In the accepted long run Motown and Stax remain rising MidTier firms; Cameo-Parkway remains rising MidTier; Vee-Jay and Sun remain rising Independents. Thus telemetry coverage passes, but the specific real-world collapses/absorption are not reproduced in this seed. Motown/Stax also retain the known seed-tier mismatch called out by the directive.
