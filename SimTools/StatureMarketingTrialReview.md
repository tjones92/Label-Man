# Independent stature marketing screen, 2026-11-06

The subsequent completed two-seed decade analysis is in [StatureMarketingDecadeReview.md](StatureMarketingDecadeReview.md). It supersedes the pending decade questions below where measured, while retaining this one-year screen as the disabled/shadow neutrality evidence.

The two-seed, 52-week implementation screen passed. Shadow calculation and budget logging preserved all 144 common CSV outputs byte for byte in each seed. The marketing-only treatment applied the existing table to available standing and retained the legacy budget for unavailable reads. The experimental mapping remains predominantly upward. This screen does not select that calibration or justify default adoption.

## Configuration and reproducibility

`run_stature_marketing_trial.ps1` ran disabled, shadow-control and marketing-treatment arms for seeds 1001 and 2002. All six reached the exact 52-week completion marker with empty stderr. No arm enabled the programme album reader or career display. The reference flags came from `veteran-follower-on-market-2002-request.json`, with the previous `lp-retail-v2` profile expanded using the resolver's explicit-key override rule. The LP assembly gate fix, programme follow-up scheduling and delivery, recognition, following, reserves and capacity settings remained in the paired configuration.

- Assembly SHA-256: `EAAA467D9521327C7ABBCA84214699CB9AC417FE62F7A2877DD9D0284682D59E`.
- Calibration SHA-256: `80546B52E1CE0902FF44692FF642B45FCC6DA5A63D48663B9CDB295DB6450F19`.
- Source revision: `5e6adddf348c2a5ffc92c721734867340d7b6f18`, with uncommitted implementation present. Requests retain hashes of the current C# source files, explicit and effective arguments, assembly, engine and calibration. This is not a clean-revision provenance claim.
- Run prefix: `stature-marketing-trial-v1`. Requests, logs, output manifests, neutrality results and summaries are under `SimLogs/writing-calibration`; CSVs are under `SimLogs`.

The calibration becomes effective July 1, 1960. The 52-week runs therefore exercise approximately six months of active treatment. Annual totals below include the earlier common period.

## Budget rules and neutrality

`--observe-stature-marketing` enables a read-only audit trace independently of the causal switch. The trace appends `proposedBudget` to the existing legacy/requested envelope columns. A shadow observer computes the proposal but returns the exact legacy budget. Ordinary disabled games without a listener retain the original fast path.

`summarize_stature_marketing_trial.py` checks each read's nonnegative finite envelopes, available-snapshot age of zero through seven days, existing career-table ratio, unavailable fallback and actual requested value. It also checks arm configuration, completion/error logs, output fingerprints, exact observation-only schema additions and disabled/control parity. Summaries group repeated reads by year, context, legacy career and read status.

| Observation | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Byte-identical shared CSVs, disabled vs shadow | 144 | 144 |
| Shadow budget reads | 98,641 | 94,313 |
| Shadow proposals differing from legacy | 12,054 | 11,228 |
| Shadow requested budgets changed | 0 | 0 |
| Treatment budget reads | 98,342 | 94,335 |
| Treatment reads with available standing | 26,951 | 27,371 |
| Treatment requested budgets changed | 12,553 | 11,948 |
| Higher / lower requested budgets | 11,743 / 810 | 11,163 / 785 |
| Distinct artists with changed treatment reads | 520 | 542 |

Changed reads are 12.8% and 12.7% of all treatment reads, and 46.6% and 43.7% of available reads. Increases account for about 93.5% and 93.4% of changes. These denominators include forecasts, passed-over opportunities, reservations, launches and later campaign decisions; they are not unique investments. Context breakdowns are retained in the JSON summaries. Changes in both directions occurred naturally in both seeds.

## Observed economic tradeoffs

Each cell shows control → treatment. These are totals within each simulated world, not direct effects on matched acts.

| Metric | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Emitted launch campaign marketing costs | $951,577 → $954,082 (+0.26%) | $858,052 → $867,980 (+1.16%) |
| Paid follow-up campaign spend | $67,461 → $61,369 (−9.03%) | $49,883 → $42,744 (−14.31%) |
| Emitted launch production costs | $10,174,988 → $10,099,600 (−0.74%) | $9,718,142 → $9,656,036 (−0.64%) |
| Single `Issued` slate events | 3,414 → 3,387 | 3,380 → 3,380 |
| Album `Issued` slate events | 1,613 → 1,612 | 1,694 → 1,677 |
| Annual single units | 122,580,979 → 122,580,979 | 121,667,949 → 121,667,949 |
| Annual album units | 39,885,281 → 39,572,492 (−0.78%) | 38,646,047 → 38,532,559 (−0.29%) |

Launch-cost telemetry and later follow-up payments are separate event streams; these figures are not a combined all-in accounting measure. Slate states are events rather than unique releases. Identical aggregate single volume does not establish identical records, cohorts or careers. The small LP-volume reduction and lower follow-up spend have the same sign across these two seeds, but neither is an adoption threshold.

## Focused verification and remaining decision

The Debug build passed with zero warnings/errors. `STATURE_MARKETING_CHECK passed checks=32` covers the real table, upward/downward and missing/timing/ownership reads, actual reservations and paid tranches, cash/reserve/capacity refusals, persistence, and shadow artist-state/RNG neutrality. `STATURE_GAME_READER_CHECK passed checks=12` also passed; both had empty stderr. Four Python summary tests passed, including future/stale evidence, invalid values/table ratios, and accidental application in shadow mode. The six real runs exercise the runner and summary end to end.

Retain the marketing reader as an independent experimental opt-in. The mapping's strong upward asymmetry and the spending/LP tradeoffs warrant full-decade evaluation before adoption. The 1964/1968 cohort measures, fixed-lookback repeat-hit share, long-run label survival and tier distributions, route/cohort trajectories, and follower-feedback sensitivity are not established by this screen. Common-output parity also does not prove equality of every latent field or longer-run neutrality. The previously recorded marketing-only save/load smoke and focused persistence fixtures remain the save-continuity evidence; this audit screen adds no new save format.

To reproduce with a fresh prefix after building:

```powershell
./SimTools/run_stature_marketing_trial.ps1 -RunPrefix stature-marketing-trial-v2 -Weeks 52
```

The runner refuses existing run outputs. `-Weeks 522` supports the later decade screen without changing the independent arm definitions.

For the subsequent causal decade evaluation, use `run_stature_marketing_decade.ps1`: it runs only control and treatment per seed, with two seed workers concurrently. Each worker uses the trial runner's `-CausalOnly -CompressOutputs` options. This is four decades rather than six. Its summaries explicitly report that disabled/shadow neutrality was not repeated at decade duration; the completed one-year neutrality results above remain separate evidence.
