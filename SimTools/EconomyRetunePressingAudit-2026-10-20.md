# Economy retune: pressing commitments

Follow-up to [the release-judgement audit](EconomyReleaseJudgementAudit-2026-10-20.md).
The pre-retune work is committed on `media-atmosphere` as `0dc5845`.

**Expanded target:** the subsequent user request requires 1960-64 net and cash
near the original economy as well. That work is recorded in
[EconomyWealthRetuneAudit-2026-10-20.md](EconomyWealthRetuneAudit-2026-10-20.md).
The acceptance below applies to the earlier closure-only target.

The subsequent historical cash review is recorded in
[EconomyCashHistoricalReview-2026-10-21.md](EconomyCashHistoricalReview-2026-10-21.md).
It supports preserving the historical corrections and evaluating liquidity
against business scale; the original cash medians are not historical targets.

## Target and scope

The user confirmed that **20-35% five-year financial closure of starting-Small
firms is the hard target**. Cash and margins are diagnostic; they need not return
to the old, historically incorrect economy. Promotion is a separate outcome,
and closures after promotion are reported separately from financial exits before
first promotion. No mortality probability or quota is introduced.

Every retune arm enables the full A1-A5, B1-B7 and C1-C4 stack used by Claude's
decade A/B, including period publishing, royalty tiers and stereo list prices.
Physical unit pressing costs retain their existing period quotes. The experiment
changes the inventory committed and reserved for, not its unit tariff.

## Mechanism

The original pressing book commits half the label's geometric lifetime-sales
forecast at launch and reorders three weeks of current sales. On the corrected
seed-2002 baseline, memory-eligible retired records leave only 4.4% of pressed
singles and 3.1% of pressed LPs unsold by week 260.

Two optional parameters now expose the existing model assumptions:

- `--pressing-initial-forecast-share=`: 0.5-2; default 0.5.
- `--pressing-repress-weeks=`: 3-26; default 3.

The release reserve and actual initial plant invoice use the same calculation.
Unsold stock remains paid inventory. Later orders use that stock before charging
another run; plant credit retains its normal terms. Opening catalogue inventory
is held fixed across arms, rather than granting extra free stock when a new-order
parameter increases. Actual stock and sales counters use the existing save schema.
The parameters consume no random draws and do not directly alter demand.

These are calibration assumptions, **not measured historical order sizes or
return rates**. The resulting unsold fraction is a model outcome, not an extra
percentage expense charged on top of the pressing book.

## Exploration

The exploratory `economy-retune-p1-commitment-2002-260` arm used one forecast and
eight weeks per repress. Closure rose from 35/230 (15.2%) to 44/230 (19.1%), still
below target. Unsold stock rose to 22.0% of singles and 11.2% of LPs; starting-Small
survivor median cash was $189.8k versus $190.2k. This arm also scaled free opening
stock with the new lot size. That comparison issue was corrected before validation;
the exploratory arm is excluded from acceptance evidence.

The stronger candidate commits **1.5 forecasts initially and twelve weeks per
repress**, with opening stock fixed. Validation settings were frozen before
running seeds 2002, 1001 and the additional seed 3003.

## Validation

**Accepted for the five-year closure target:** all three completed 260-week runs
are in band. Equal-weight mean closure is **29.7%**. Seed 3003 is an additional
validation seed, not a matched before/after comparison; no original-pressing
control was run for it. Its 34.1% result is near the upper edge of the band.

| Starting-Small five-year outcome | Seed 1001 | Seed 2002 | Seed 3003 |
|---|---:|---:|---:|
| Starting firms | 231 | 230 | 232 |
| Corrected stack, original pressing: closures | 52 / 22.5% | 35 / 15.2% | not run |
| Retuned pressing: closures | **68 / 29.4%** | **59 / 25.7%** | **79 / 34.1%** |
| Retuned closures before first promotion | 67 | 58 | 79 |
| Retuned firms ever promoted | 59 | 71 | 73 |
| Original -> retuned survivor median cash | $143.8k -> $121.5k | $190.2k -> $162.6k | retuned $163.8k |

Survivor medians describe different surviving sets, not a matched-label cash
counterfactual. Retired, memory-eligible records leave 38.3% / 38.1% of pressed
singles and 22.9% / 22.2% of pressed LPs unsold (seeds 1001 / 2002). The all-in
single loss share rises from 8.7% / 8.3% to 21.0% / 20.1%; LP loss share rises
from about 1.2% to 6.8% / 6.1%. These describe retirements observed by week 260,
not a fully matured cohort of every release launched during the five years.

Booked contribution margins fall approximately 7-9 percentage points across
tiers on both seeds. This is contribution after plant bills, before production,
marketing and overhead, **not operating profit**. Cash remains much higher than
in the old all-switches-off economy; the confirmed target does not require those
old cash levels.

### Cash and net against the original economy

A matched comparison against the original all-switches-off economy shows that
the retune removes much of the single-net increase, but leaves substantial LP
and cash increases. The following are increases over that original economy,
using seeds 1001 and 2002:

| Measure | Corrected stack, original pressing | Retuned pressing |
|---|---:|---:|
| Single net, cumulative 1960-63 | +35.0% | **+9.6%** |
| Album net, cumulative 1960-63 | +56.6% | **+43.7%** |
| Small median cash, week 260 | +137.0% | **+90.9%** |
| Boutique median cash, week 260 | +72.2% | **+30.4%** |
| Independent median cash, week 260 | +61.1% | **+43.6%** |
| MidTier median cash, week 260 | +34.1% | **+33.2%** |
| Major median cash, week 260 | +28.4% | **+17.6%** |

Net ratios use equal-weight seed means of cumulative booked contribution for
the completed calendar years 1960-63. Cash ratios use equal-weight means of
each seed's active-label median by **current tier** at week 260. Promotions and
different surviving sets affect those medians; these are not within-label
counterfactuals. Seed 3003 has no matched original-economy control and is excluded.
Generated values are retained in
`SimLogs/economy-retune-p2/cash-and-net-comparison.json`.

These do **not** replace the original audit's **1969** single-net +37% and
album-net +73% figures. The retuned worlds stop in December 1964, so their 1969
net and cash have not been measured. At the available matched horizons, closure
is calibrated and overall wealth remains elevated, especially LP contribution
and Small-label cash.

### Chart and career diagnostics

This pass compared 1960-63 for annual errors. The follow-up confirmed that week
260 contains the final chart week of 1964, and includes 1964 in the expanded
five-year net/cash comparison. The earlier exclusion of that row as incomplete
was unnecessary.

| Retuned minus corrected baseline, 1960-63 | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Genre-share sumAbsErr | -4.0 | +0.6 |
| Year-end Hot 100 slot error | +24 | -10 |
| LP unit share | essentially unchanged | -0.2 to -0.9 pp |
| Owner-Major entry share | +2.6 to +3.7 pp | -1.2 to +1.4 pp |
| Week-260 Star + Superstar | 36 -> 33 | 32 -> 34 |

The closure target passes on all three seeds; the matched diagnostic changes are mixed.
This is not evidence that every chart measure improves. It does not replace the
original unbroken decade A/B with a retuned decade A/B, and no new production
defaults are adopted on this five-year evidence.

The final run manifest and review are generated under
`SimLogs/economy-retune-p2/`; generated outputs are not committed.
The review returns `IN_BAND` for assembly
`E79CEAB66606E02B3F290A4160B40C2CA5D7614D74183C21BF32F3A3F442B4AD`.

The runner records one assembly hash, exact arguments, native exit codes and
completion markers. Runs are serial and unbroken. The analyzer rejects missing
weeks, incomplete final cohorts, repeated seeds and mixed configurations. It
requires each of three distinct seeds to satisfy the five-year band.

The new 17-probe native check and the existing pressing check pass, including
invoice/reserve agreement, carried inventory, saves, opening stock and RNG.
The existing B/C native checks pass all 49 probes. Five Python review tests pass.
An omitted-parameter 13-week control reproduces the baseline's 7,800 finance rows
exactly, together with releases, all-in outcomes, career and distribution events.
Campaign events match through week 12: their clock stamp precedes the next
advance, so a short run does not execute the following week's campaign pass.

## Reproduction

```powershell
SimTools/RunEconomyRetune.ps1 -RunTag NEW-NAME -Seeds 1001,2002,3003 -Modes larger -Weeks 260
```

`control`, `commitment` and `larger` respectively select 0.5/3, 1/8 and 1.5/12.
Use the bundled Python runtime with:

```text
python -I SimTools/analyze_economy_retune.py PREFIX1 PREFIX2 PREFIX3 --weeks 260 --review-manifest SimLogs/NEW-NAME/runs.json --output SimLogs/NEW-NAME/review.json
```

Game defaults remain the original pressing settings. This is an explicit retune
of the all-corrections audit configuration, not a default rollout of the audit
switches. Resume comparisons must pass the same parameters recorded in their
world metadata.
