# Regional economy recovery — 2026-10-16

Status: implemented and measured on two matched 104-week seeds. Small-label revenue is recovered; the model remains opt-in because survival and chart calibration still have residual misses. This follows RegionalSyncHandoff.md; its proposed options are background, not requirements.

Follow-up: [RegionalSurvivalAndChartFollowup.md](RegionalSurvivalAndChartFollowup.md) supersedes the survival interpretation and timing/airplay measurements below. The earlier timing settlement mixed formats, and the earlier airplay residual included chart-survey noise. The follow-up also fixes recognition of acquired distribution and documents the replacement of artificial label-exit rules. The economic tables here are retained as historical results of the earlier build.

## Economic diagnosis

Regional arrival removed two things together: a national launch timetable and revenue from regions outside a label's footprint. Small labels had been paying ordinary production and operating costs with that revenue. A delayed delivery mechanism changes timing but cannot restore an opening audience that has already aged away.

The replacement gives a regional release a concentrated local campaign. The same audience budget can support a strong local business before the record reaches the rest of the country. It reads the record's actual footprint rather than label tier or financial distress.

## Implemented mechanism

- **Sparse retail and regional servicing are separate.** The original finite shop allocation can sell before full regional arrival. The promotion and rotation seeds stay gated. Arrival does not recreate copies already sold, and waiting regions receive no automatic restock.
- **Launch promotion is geographically allocated.** Population-weighted awareness deposits originally assigned to waiting markets are moved into served markets, within their remaining awareness. The cancelled remote deposit is cleared so it cannot be spent again on arrival.
- **The campaign keeps following the current footprint.** Before each live demand pass, the national campaign audience that cannot reach waiting markets is allocated across served markets' remaining audience. This is recomputed from current state each week, without accumulating an extra audience stock. Expansion spreads that budget across more markets.
- **Discovery outside the footprint is local.** In the new campaign mode there is no shared national hearsay blend before arrival. Existing local discovery and neighboring-market propagation can introduce a record there. The national top 40, local breakout, neighbor test and regional rack proof remain the arrival paths.
- **The 100-unit one-stop trigger is removed.** Backorders, regardless of size, do not grant arrival. The trigger constant, branch, method parameter and diagnostic counter have been deleted.

The campaign allocation uses the existing awareness weights. With buying population `P_r`, national awareness `N`, and regional awareness `A_r`:

```
waiting budget B = sum(waiting markets: P_r * 0.4 * N)
served headroom H_r = max(0, 1 - (0.4 * N + 0.6 * A_r))
fill = min(1, B / sum(served markets: P_r * H_r))
extra campaign awareness_r = H_r * fill
```

If all markets are served there is nothing to reallocate. If the served audience saturates, the excess cannot create buyers. Genre acceptance, quality, local inventory, store capacity, and regional purchase clearing determine actual sales. National units continue to be the sum of the seven regional settlements. Finance, COGS, operating costs and bankruptcy rules are unchanged.

## Experiments

All accepted economic runs use 104 weeks, seeds 1001 and 2002, and the real headless Godot autoload chain. The control is `sig5d-SEED-104`, the pre-arrival economy with demand-based local readers. Existing control logs are retained.

- `retail2`: finite retail allocation, existing 0.25 hearsay, no one-stop trigger.
- `focus1`: retail plus launch-only campaign concentration. Its opening recovery did not persist through the full release lifecycle; it is superseded.
- `focus2`: continuing campaign allocation while retaining shared pre-arrival hearsay. Stopped before acceptance when the discovery model was revised; incomplete logs are excluded from evidence.
- `focus3`: the implemented continuing campaign budget with local discovery before arrival.

The regional switches remain opt-in. The command below selects the current regional mechanism; the follow-up describes the new default economic lifecycle. The historical `focus3` table requires its recorded assembly. `--legacy-label-exits` preserves its old mortality policy in the current code while retaining the later distribution correctness fix.

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=104 --seed=1001 --run=regional-economy-1001-104 `
  --enable-genre-market-v2 --enable-artist-population-lifecycle `
  --local-signals-read-demand --regional-arrival `
  --regional-retail-access --regional-campaign-focus
```

Raw outputs and manifests are under ignored `SimLogs/`. Manifests record arguments, seeds and the assembly hash. `retail1` crashed inside the sandbox before engine initialization and is excluded. Accepted engine runs use the reviewed launch outside the sandbox.

## Measurement corrections

`analyze_regional_sync.py` now also reports `soldPeakSpread`. A market with zero lifetime sales has no sales peak; assigning its first logged week as a peak creates false separation. The older no-route arrival arm's apparent >=8-week spread falls from 7.17% to **3.13%** after this correction. The 5d control remains 0.21%. The original `peakSpread` output is retained for comparisons with earlier reports.

`analyze_regional_economy.py` reports both tier at settlement and cohorts grouped by first observed label tier. Promotion must not retroactively remove a successful label's units from the Small-label cohort. Closures are matched by name and closure week to immutable finance-census label IDs; reused names cannot assign a dead label's bankruptcy to a successor. Ambiguous matches fail the audit. The audit also checks regional/national unit identities, inventory movement, and exactly-once settlement booking.

## Validation

- Build: zero warnings and errors.
- Fixed checks: physical stock conservation; saving imported stock separately from held promotion; arrival without duplicate stock; no arrival from one million backorders; actual local arrival clocks; population-weighted campaign conservation; campaign preparation without accumulation; expansion releasing the concentration; saturation; album and player boundaries.
- Disabled 52-week run: exact weekly row agreement with the first 52 weeks of `sig5d-1001-104` for records, label finance, national and regional settlements, demand stages, concentration and year-end chart. The annual rollup's retrospective decision/completion statistics depend on the final run horizon; its weekly economic inputs agree.
- Full-world roundtrip: the final continuing-budget model passed at 13 weeks, including compressed save I/O (`regional-economy-v3-roundtrip-console.log`).
- Both final runs completed all 104 weeks. Both economic audits report zero national/regional unit mismatches, zero invalid inventory movements, and zero duplicate or missing settlement bookings.

## Results

Small cohorts below use each label's first observed tier, so later promotions retain their earlier Small-label identity. Net revenue is the actual settled Single label net, after the existing cost/royalty/distribution calculations; it is not a cash grant. All label counts include runtime-founded labels observed during the run.

| Metric, 104 weeks | Control 1001 | New 1001 | Control 2002 | New 2002 |
|---|---:|---:|---:|---:|
| National Single units | 307.22M | 305.28M (-0.63%) | 312.05M | 313.34M (+0.41%) |
| Small-cohort Single units | 22.98M | 23.00M (+0.07%) | 23.29M | 22.76M (-2.27%) |
| Small-cohort Single net | $9.158M | $9.528M (+4.03%) | $9.424M | $9.421M (-0.04%) |
| Small seller median units | 73,217 | 85,424 | 77,738 | 81,828 |
| Small seller median Single net | $31,744 | $35,475 (+11.75%) | $32,540 | $34,490 (+5.99%) |
| Small sellers observed | 203 | 202 | 213 | 205 |
| Small seller bankruptcies | 9 | 8 | 10 | 14 |
| Small non-seller bankruptcies | 26 | 24 | 24 | 26 |
| Small surviving sellers | 150 | 148 | 149 | 141 |
| Small survivor median cash | $74,039 | $74,113 | $92,768 | $85,979 |
| All closures / bankruptcies | 153 / 51 | 159 / 51 | 156 / 44 | 161 / 54 |
| Local share of weekly demand change | 0.94% | 46.69% | 0.95% | 46.25% |
| Settled peak spread, positive-sale markets only, median | 1 week | 2 weeks | 1 week | 2 weeks |
| Actual settled peak spread >=8 weeks | 0.21% | 3.81% | 0.42% | 2.72% |
| Regional top-11 overlap | 87.79% | 85.57% | 89.27% | 86.88% |

The demand-change decomposition uses record-weeks with positive demand in all seven markets. Its final samples are 100,091 / 92,725 record-weeks; it describes those observations, while the settled metrics include the broader sales population. The zero-sale-market correction is used for all actual settled peak figures above.

The two partial approaches did not recover the Small-label middle: finite retail alone gave median units of 55,895 / 43,201; launch-only concentration gave 54,868 / 63,774. Continuing allocation is the change that restores the revenue distribution. Small-cohort total net is +4.0% / flat, and median seller net is +11.8% / +6.0%, with substantially more regional demand movement than control.

## Acceptance limits

This is a measured replacement for the lost regional-label revenue mechanism, not a claim that every guard has passed:

- **Revenue and national volume:** recovered on both seeds. National units stay within the original +/-3% guard, including each annual observation.
- **Survival:** the earlier repeatable +12 bankrupt Single sellers on each seed has been reduced to -1 / +4 within the Small cohort. Seed 2002 still has four extra bankrupt Small sellers and ten extra bankruptcies overall. Its surviving Small seller count is lower and median surviving cash is 7.3% lower. The model does not grant solvency or protect an individual label from a changed market.
- **Regional timing:** actual sales diverge, but the old median 3-5 week and 5-15% >=8-week targets remain unmet. Top-11 overlap is closer to, but still above, the 70-85% target. Empty markets are excluded from the timing claim.
- **Chart calibration:** Major-family chart share moves by -5.8 to +5.3 points across the four seed/year comparisons, outside the +/-3 point guard. Top-40 airplay share rises by 1.0-4.9 points, exceeding its +3-point guard in three comparisons. The full Hesbacher, genre, concentration and turnover results are retained in `SimLogs/regional-economy-final-guards.txt`.

Keep this opt-in while deciding the remaining chart calibration and validating survival over a longer holdout. The mechanism should be retained: regional labels can earn locally from the same campaign audience, with national reach arising through actual regional diffusion. Additional work should follow the residual survival cases and local radio/chart feedback, rather than reintroducing a sales threshold that guarantees distribution.

Final evidence: `focus3-runs.json`, `regional-economy-final-controls.json`, `regional-economy-final-treatment.json`, both `focus3-SEED-104-sync.json` files, `regional-economy-final-guards.txt`, `regional-economy-off-comparison.json`, and the final fixed-check/roundtrip logs under `SimLogs/`.
