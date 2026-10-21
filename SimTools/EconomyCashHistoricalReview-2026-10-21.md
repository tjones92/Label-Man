# Historical context for label cash, 1960-64

Research requested after the release-judgement audit and pressing retune. This
review uses existing runs; it adds no economic charges, adopts no defaults and
runs no additional decade simulation.

**Assessment:** the corrections justify some additional profit. The evidence
does not justify returning cash mechanically to the old economy, but it also
does not establish that the present balances are realistic bank cash. The
pressing candidate still leaves most established firms very liquid relative to
their turnover. Treat that as an unresolved model question, especially for the
upper tiers, rather than declaring cash validated because closures pass.

The earlier +/-15% comparison with original cash was a working interpretation
of the user's parity request. It is not a historical cash band. Further volume
or owner-pay calibration solely to satisfy it is on hold during this review.

## Period evidence and its limits

### 1. Stereo could generate a real increase in contribution

The February 1961 *HiFi/Stereo Review* discussion of record retailing describes
the $1 stereo premium. It reports Everest's contention that the stereo/mono
production-cost difference was negligible, and notes that most companies still
charged the premium. This is contemporary reporting of an industry argument,
not an independently audited cost study.

**Implication:** C2 can legitimately increase the label's contribution. Adding
an equal compensating manufacturing expense merely to undo the premium would
need separate evidence. The same accounting principle applies to B4/B6:
correcting an overstated expense can legitimately increase retained funds.

Source: [HiFi/Stereo Review, February 1961](https://manuals.plus/m/0f90b8289b8e207b12f54499fc5e94424c452e1af81c99ef7e7629f474a365cb).

### 2. Successful firms earned substantial profits, with considerable variation

Contemporary company announcements provide useful scale checks:

| Firm and reporting period | Reported net sales | Reported net income | Income / sales, calculated |
|---|---:|---:|---:|
| Capitol, fiscal year ended June 30, 1964 | $57,460,133 | $1,952,533 | 3.4% |
| Liberty, fiscal year ended January 31, 1962 | $7,361,713.92 | $646,365.34, after tax | 8.8% |
| Cameo-Parkway, six months ended June 30, 1962 | $3,896,341 | $530,431 | 13.6% |

Sources: [Cash Box, October 3, 1964, p. 7](https://www.worldradiohistory.com/Archive-All-Music/Cash-Box/60s/1964/CB-1964-10-03.pdf),
[Cash Box, April 28, 1962, Liberty acquisition report, mirrored text](https://manuals.plus/m/7fa814edcbd1cddcc3d93cc03c637675dd9768503f380a78ec7a518bf448b357),
[Cash Box, September 1, 1962](https://www.worldradiohistory.com/Archive-All-Music/Cash-Box/60s/1962/CB-1962-09-01.pdf).

These are reported company results, not a representative industry sample.
Capitol's reporting scope included an electronics business it exited during
the year. Fiscal periods differ, and the simulation's record receipts and
per-record contribution do not have identical accounting scope. In particular,
the game's `singleNet` and `albumNet` are **contribution before recording,
campaigns and overhead**, not the net income in this table.

**Implication:** profitable independent labels and accumulated reserves are
plausible. These figures cannot be converted into a universal cash limit, nor
can they validate 50-60% company net margins. A proper comparison needs annual
company earnings after all modeled operating costs, with tax treatment and
reporting scope identified.

### 3. Sales were often financed and returnable

Billboard's February 6, 1961 dealer-program summary lists Liberty's extra
discount, full exchange privilege and deferred payment schedule, alongside
30/60/90-day terms and exchange arrangements at other suppliers. Cash Box's
September 22, 1962 Liberty program describes a 15% invoice discount, a 100%
exchange privilege and payments spread across November, December and January.

Sources: [Billboard, February 6, 1961, p. 50](https://retrocdn.net/images/5/5e/Billboard_US_1961-02-06.pdf),
[Cash Box, September 22, 1962](https://retrocdn.net/images/1/1f/CashBox_US_1962-09-22.pdf).

These are specific promotional programs, not evidence that every shipment had
the same discount, maturity or return rate. An exchange privilege is also not
an observed percentage of unsold stock.

**Implication:** enough capital to finance several months of stock and
collections is sensible. Sales, receivables, inventory and spendable cash must
remain distinct. The simulation already has wholesale receivables, plant bills
and paid inventory; these must not be charged a second time. We should audit
how much remains genuinely available after obligations rather than treating
all higher balances as wasteful surplus.

### 4. Surpluses did not all remain in the label's bank account

Cameo-Parkway declared its sixth consecutive quarterly dividend in early 1964.
Decca's 1963 report described quarterly dividends and a new Canadian pressing
plant. Decca's consolidated earnings included Universal Pictures and are not a
record-label-only profit benchmark.

Sources: [Cash Box, February 8, 1964](https://www.worldradiohistory.com/Archive-All-Music/Cash-Box/60s/1964/CB-1964-02-08.pdf),
[Cash Box, March 21, 1964, Decca annual-report coverage, mirrored text](https://device.report/m/21ef1c9aa6be51d9e6fe9b865fe6bae452cd12712eb847df8a14df8d99f4812c).

**Implication:** historically, profitable firms could distribute profits or
convert funds into assets. The AI monthly settlement has ordinary overhead and
some distribution reinvestment, but no separate income-tax or dividend
settlement. Its overhead is an aggregate, so staff/owner compensation may
already be partly represented; this research does not establish a missing
salary amount. The experimental owner-pay feature is off by default and is
not a historically calibrated remedy.

### 5. Healthy aggregate earnings could coexist with serious firm distress

Cash Box reported Riverside closing its offices amid financial difficulty in
July 1964, with creditors including a factor seeking settlement. A later report
records Cameo-Parkway losing $103,478 in the first half of 1964, following its
strong 1962 half-year earnings above.

Sources: [Cash Box, July 18, 1964](https://retrocdn.net/images/5/54/CashBox_US_1964-07-18.pdf),
[Cash Box, December 11, 1965, retrospectively reporting the 1964 loss](https://www.worldradiohistory.com/Archive-All-Music/Cash-Box/60s/1965/CB-1965-12-11.pdf).

**Implication:** firm-level volatility and reliance on credit matter alongside
the closure count. A model can close the weakest quarter of entrants while
making almost every established survivor financially invulnerable. The
20-35% starting-Small closure band remains a game-design target, not an
empirically measured 1960s mortality rate or proof that survivor wealth is right.

## What the existing simulation actually holds

The following uses weeks 1-260 from matched seeds 1001/2002. Each cash value is
the equal-weight mean of the two seed-level **active, current-tier medians**.
Promotions, exits and different survivors affect these values; they are not
within-label counterfactuals. All amounts are nominal simulated period dollars.

"Corrected" is the full audit stack with original pressing. "Pressing candidate"
is the closure-tested 1.5-forecast/twelve-week commitment configuration, without
the subsequent experimental LP volume cut or owner pay.

| Current tier | Original cash | Corrected cash | Pressing candidate cash | Candidate cash / 1964 gross, months |
|---|---:|---:|---:|---:|
| Small | $44,365 | $105,127 | $84,677 | 13.8 |
| Boutique | $358,512 | $617,369 | $467,455 | 21.2 |
| Independent | $355,778 | $573,309 | $510,882 | 17.4 |
| MidTier | $2,523,975 | $3,385,241 | $3,362,011 | 20.0 |
| Major | $24,864,008 | $31,928,523 | $29,250,700 | 30.5 |

The last column first computes each firm's cash divided by its own 1964 gross
times twelve, then takes the median within seed/tier and the mean across seeds.
It is not the ratio of the cash median to the gross median. Gross here is the
booked record-receipts field, not company-wide consolidated sales or expenses.
Months of gross is a turnover scale, **not a runway estimate or historical
target**.

Subtracting outstanding plant bills leaves candidate cash medians of $82,293,
$453,349, $501,135, $3,330,459 and $29,032,465 respectively. This is only a partial
liability adjustment: it excludes future release commitments, contract
obligations and any real-world taxes or distributions not modeled. Nevertheless,
existing unpaid plant bills do not explain most of these cash stocks. Paid
inventory has already consumed cash; wholesale receivables are separate assets,
not an amount to subtract from the cash account.

In the pressing candidate, 56% of current Small firms, 87% of Boutiques, 69% of
Independents, 88% of MidTiers and 100% of Majors hold cash net of plant bills
greater than their own entire 1964 gross (equal-weight means of seed fractions).
Every active MidTier and Major has positive recent monthly cash flow at the
endpoint. Fixed-overhead-only coverage is much larger still, but excludes
variable production and marketing and must not be described as full operating
runway.

One scale warning: simulated Capitol on seed 2002 has $56.9m cash against
$22.2m of 1964 booked record gross. Historical Capitol reported $57.5m net sales
and $2.0m annual net income for its different fiscal/reporting scope. This is
not a comparison of historical bank balances, but it makes the interpretation
of simulated cash as literal discretionary bank cash particularly doubtful.
It does not establish a specific corrective charge.

The experimental 0.70 LP-volume arm reduces nominal balances, yet cash still
amounts to approximately 14.2 / 20.7 / 17.4 / 19.3 / 29.4 months of each firm's
1964 gross across the five tiers. Lowering sales can reduce dollars without
resolving cash accumulation relative to operating scale. That is a reason to
benchmark LP volume independently, rather than use it as a generic cash sink.

Reproduce the read-only comparison with:

```text
python -I SimTools/audit_economy_cash_context.py
```

Generated detail: `SimLogs/economy-cash-historical-context.json`. Inputs are the
original `decaboff`, corrected `decabon`, `economy-retune-p2-larger` and
experimental `economy-wealth-p2-volume` finance streams, seeds 1001/2002.

## Recommendation

Keep B4, B6 and C2. Keep the closure-tested pressing candidate as the current
reference for evaluating the corrected economy. Do not adopt an LP-demand cut
or owner-pay charge solely to restore old cash medians.

Separate three questions before another economic change:

1. Are annual unit sales, per-record contribution and **whole-firm operating
   earnings** plausible against period evidence with comparable scope?
2. Does cash represent realistic available liquidity after modeled obligations,
   or retained business funds in an abstraction that omits some uses of profit?
3. Does the distribution of liquidity preserve consequential financing,
   reinvestment and release choices for established firms as well as entrants?

The additional profits are not inherently ahistorical or bad for play. The
current cash stocks remain unvalidated and look generous, especially at the
top. This review found no representative 1960-64 label bank-cash cross-section;
it supports these diagnostic conclusions, not a historically proven dollar cap
or a blanket declaration that every tier's present median is reasonable.
