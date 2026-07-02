# Finance reconciliation and distribution scaffolding audit

Measured: 52 weeks, seeds 1001 / 1002 / 1003.

## Phase 0 findings

- `ChartManager` creates the live 600-label population with `AILabelFactory.GenerateAllLabels(600)` and passes that exact list to `CompetitorManager.Initialize`.
- Before this pass, `LabelLifecycleManager.InitializeLabels` and `ProcessMonth` had no callers. Its list stayed empty, so there was no live double-write or execution-order winner: the two populations were effectively disjoint, with lifecycle completely inert.
- `AILabelFactory` is the canonical runtime generator and emits Major, MidTier, Independent, Boutique, and Small. The inactive `LabelGenerator.GenerateLabels` emits only MidTier, Independent, and Small and uses substantially lower starting cash.
- `distributionStrength` had seven reads and two generator writers. Regional coverage had six independently implemented checks across four files. The former was safe to split into owned and borrowed reach; the latter was consolidated behind `AILabel.HasDistributionInRegion`.
- Label revenue already excluded artist royalty before and after recoupment. The actual missing leg was an artist earnings ledger after recoupment.

Lifecycle is now attached to the exact list owned by `CompetitorManager`. Autoload subscription order makes `CompetitorManager` process monthly finance/status first and `LabelLifecycleManager` subsequently read that state for health, death, and unchanged tier-transition logic.

The legacy lifecycle birth targets remain 100-160 labels versus the live population of 600, so births remain naturally suppressed. That scale mismatch is reported rather than silently retuned in this finance/scaffolding pass.

## Economy results

`finance-only` disables the newly revived lifecycle loop in the audit harness, isolating COGS plus finance plumbing from lifecycle behavior. `final` enables it. The reviewer-corrected regional change adds no multiplier: for generated labels without deals the shared helper is semantically identical to the former array checks, so there is no separate coverage-penalty delta.

| Run | Seed | Annual units | Week-52 active records | Operating labels | Bankrupt | Defunct (bankruptcy death) |
|---|---:|---:|---:|---:|---:|---:|
| Baseline | 1001 | 156,510,097 | 1,847 | 583 | 17 | 0 |
| Baseline | 1002 | 162,851,295 | 1,793 | 572 | 28 | 0 |
| Baseline | 1003 | 174,521,020 | 1,923 | 584 | 16 | 0 |
| Finance only | 1001 | 153,482,968 | 1,675 | 580 | 20 | 0 |
| Finance only | 1002 | 160,122,449 | 1,785 | 574 | 26 | 0 |
| Finance only | 1003 | 168,880,770 | 1,737 | 582 | 18 | 0 |
| Final | 1001 | 151,941,003 | 1,713 | 583 | 15 | 2 |
| Final | 1002 | 160,681,737 | 1,706 | 569 | 29 | 2 |
| Final | 1003 | 172,705,004 | 1,799 | 588 | 12 | 0 |

Finance-only annual volume changed by -1.9%, -1.7%, and -3.2% from baseline. Final annual volume remains inside the 150-180M target range in all seeds. Week-52 active records are below the approximate 1,800 target in seeds 1001/1002 and effectively on target in seed 1003; no calibration constants were changed in response.

Final operating-label counts by tier:

| Seed | Major | MidTier | Independent | Boutique | Small |
|---:|---:|---:|---:|---:|---:|
| 1001 | 10 | 86 | 160 | 115 | 212 |
| 1002 | 8 | 89 | 164 | 95 | 213 |
| 1003 | 9 | 98 | 158 | 109 | 214 |

## Accounting and deal verification

- Retail price remains `$0.89`; pressing cost is an exported `$0.30` per unit. Label net is post-COGS gross minus retail-based artist royalty and post-COGS distribution skim.
- The artist royalty basis intentionally remains retail, preserving the existing contract convention. Recoupment is clamped at zero and royalty beyond recoupment accumulates in `SimulatedArtist.totalRoyaltyEarnings`; this adds observability but does not further change label economics.
- A four-week forced-deal run (`finance-forced-deal`) verified that granted regions become covered, every client skim is credited to the selected distributor, and the deal advance recoups by exactly the routed skim. The test client's advance moved from 5,000 to 267.3743 after routed skims of 1,055.038, 1,004.77, 920.3999, and 1,752.418.
- `DistributionDeal` follows the existing runtime-data convention: a serializable plain C# class held as a non-exported field on the `AILabel` resource, like its `SimulatedArtist` roster. The project has no save/load system requiring Godot resource serialization.

## Reproducibility

Two independent final seed-1001 runs were byte-identical across weekly, lifecycle, breakout, retirement, tier-volume, and label-finance CSV outputs.
