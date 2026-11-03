# Do smaller labels lose LPs at the profitability gate or after release?

2026-11-03. Follow-up to `CombinedLabelTurnoverInvestigation.md` and
`LpAssemblyTurnoverExperiment.md`. Reads the existing combined and control
worlds (seeds 1001, 2002, 104 weeks); no rules or defaults change.

## Finding

**Both, in different populations, with one shared cause: the release-time
forecast for debut (NewSigning) LPs is about three times too low.**

- The 147 opening identities that close only in the combined worlds mostly
  lose their LPs **before** the profit gate. They fall behind on chart singles
  from the first weeks, run negative monthly cash flow, and the slate's
  commitment reserve (max of overhead and **2× recent monthly burn**) then
  blocks LP cash reservation. This is the hit lottery reshuffling which firms
  get early hits; the rescued cohort shows the mirror image.
- Across all smaller labels, the profit gate is the main LP filter: 8,631 of
  8,676 LP preparation postponements are all-in profit-gate refusals. Debut
  LPs pass only 55% of the time, against 99–100% for Rising, Established and
  Star acts. Finalized debut LPs that passed made money in every case and
  realized about 2.9× their forecast contribution.

## 1. The extra-closing cohort is blocked at cash reservation

LP slots for the 147 identities, up to each firm's anchor week:

| Stage | Control | Combined |
|---|---:|---:|
| Proposed | 691 | 537 |
| Reached preparation (cash reserved) | 511 | **97** |
| Recorded | 499 | 71 |
| Issued | 462 | 46 |
| `insufficient-unreserved-cash` postponement slot-weeks | 607 | **3,493** |

The rescued 80 identities reverse every row (90 → 323 prepared, 57 → 250
issued). The gap is present by week 26 (166 → 37 prepared), before the
receipts collapse described in the turnover note. In combined shortfall
records through week 26, cash on hand is similar (median $2.5k vs $2.7k).
However, the 2×-burn hold is ten times larger (median $3,365 vs $336), and
the LP production reservation is larger ($2,399 vs $1,319).

The burn comes from sales. In weeks 5–13 these firms sell 2.32M charted
single units in the control but 0.15M in the combined world. In the rescued
cohort, sales move from 0.17M to 3.27M. In the first three statement months,
record receipts are $5.51M vs $0.74M, and 136/147 combined firms have negative
net cash flow (79 in control). The profit gate barely reaches this cohort:
40 gate decisions in total across their final 12 weeks.

## 2. Low sellers close more often at the same early sales

Matched identities do not explain the **net** count, because hits move both
ways. Conditioning on smaller labels' weeks 1–13 unit sales does:

| Weeks 1–13 units | Control closures | Combined closures |
|---|---:|---:|
| 1–10k | 83/532 (16%) | **138/538 (26%)** |
| 10–30k | 18/184 (10%) | 19/158 (12%) |
| 30k+ | 1/81 (1%) | 5/105 (5%) |

The early-sales distribution is almost the same in both worlds, so the
combined world does not starve smaller labels of early sales. A firm with a
weak start, however, is more likely to close. The low-seller bin accounts for
about +55 of the +67 net opening-cohort closures. Over the first six months,
low sellers' cash bridges differ only modestly (receipts −$1.1k per firm;
median net flow −$166 → −$896), and their LP preparation counts are similar
(442 vs 406). Over the full run, however, smaller-label LP issues fall from
7,162 to 5,690. In the combined world, 1,921 LP slots are postponed at
preparation, compared with 43 in the control, and 800 expire after cash was
reserved but before recording.

## 3. The gate refuses debut LPs the simulation would pay for

The combined profile adds `--release-all-in-economics`, whose positive-profit
gate runs in `PrepareSlateMasterCore` after cash is reserved and before the
session. The control profile has no such gate. Smaller-label LP gate pass rates
are Debut 55%, Declining 67%, and Rising, Established and Star 99–100%. With
`--lp-assembly` omitted, debut LPs pass 62% of the time.

Refused smaller-label LPs are narrow losses: median forecast −$709 (IQR −$1,041
to −$406) with only ~$120 expected campaign. They should be compared with
finalized LPs released by week 52:

| Group (tier / career / assembly kind) | n | Forecast contribution | Realized | Ratio | Profitable |
|---|---:|---:|---:|---:|---:|
| Smaller / NewSigning / Ordinary | 663 | $4,373 | $11,716 | **2.94** | 100% |
| Smaller / Rising / HitLed | 42 | $12,555 | $12,062 | 1.02 | 100% |
| Smaller / Rising / Ordinary | 8 | $14,070 | $30,184 | 1.76 | 100% |
| Smaller / Established / HitLed | 11 | $17,421 | $18,669 | 1.46 | 100% |
| Other / NewSigning / Ordinary | 20 | $9,357 | $25,599 | 2.74 | 100% |

Only gate-passing LPs are observed, and forecast noise means passers tend to be
the optimistic draws. The population bias is therefore unlikely to be smaller
than this. Hit-led LPs are calibrated, but ordinary new-session LPs, especially
debuts, are not. That matches the caveat in `AlbumPriorRecalibration`: its
coefficients were fitted on `dec-awrw-on-1001-522`, where New/Unsigned bias
remained, and the combined profile adds LP channels such as record club and
rack that the prior did not see. Responsive memory cannot correct this in time:
LPs finalize after about 70 weeks, and firms that are refused never observe one.

This is a statement about the simulation's own economics. It does not show
that 1960 debut LPs were profitable; whether realized LP demand is too generous
is a separate calibration question.

### Minor: assembly path subtracts a credit it never added

With `--lp-assembly`, `projectedAlbumStandaloneNet` is replaced by the raw
assembly prior, which has neither the portfolio credit nor the expected campaign.
`PassesReleaseProfitGate` still subtracts `albumPortfolioCredit`. For smaller
labels the median credit is about $1, and only 83 of 8,846 refusals would pass
without it, so the defect is immaterial here. It should still be fixed with
the forecast work.

## Implication for the next matched test

The LP forecast is the right lever, but the test should correct the debut/ordinary
LP prior rather than remove the gate. Hold everything else in the combined
profile fixed. Track the slate funnel (proposed → prepared → issued) per tier,
low-seller closures, professional single issues and the cover ratio, and realized
LP net for the newly admitted projects. The cohort-1 cash-reserve feedback loop
(2× burn) is a separate mechanism and should not be relaxed in the same arm.

## Reproduction

```powershell
py SimTools/analyze_lp_profit_gate_funnel.py
```
