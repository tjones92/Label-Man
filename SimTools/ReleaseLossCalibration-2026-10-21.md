# Release loss rates, B5 and the A2-A4 stack on the corporate economy

**October 21, 2026.** Branch `media-atmosphere` after `9ca7a15` (corporate model plus the safe
release-judgement flags by default). Every run uses the ordinary game profile on seeds 1001 and 2002:
105 weeks for screening, 265 weeks for the closure band. No decade runs. Evidence lives in
`SimLogs/rj1-*` through `SimLogs/rj5-*` (gitignored).

## Historical target

No source gives a measured 1960s share of releases that lost money. What exists is the trade's own
rule of thumb and later, better-scoped figures:

- Record companies have long said that only **one release in ten succeeds**. Osborne traces the
  claim to at least the 1950s and shows it was used rhetorically, never audited
  ([Osborne, "I Am a One in Ten", in *Music by Numbers*, 2021](https://www.academia.edu/6770634/I_Am_a_One_in_Ten_Success_Ratios_in_the_Record_Industry)).
- Denisoff, writing on the pre-1979 market, quotes an industry marketer: "only 10 percent even smelled
  'break even'". He adds that "more recently" 85% of singles and 80% of albums failed to return their
  investment ([Denisoff, *Tarnished Gold*, 1986](https://www.perlego.com/book/3122406/tarnished-gold-record-industry-revisited-pdf)).
- A 1988 UK trade article puts it at about 90% of all records making a loss
  ([Music Technology, Dec 1988](https://www.muzines.co.uk/articles/record-profits/2301)).
- For the LP in the 1950s-60s, the evidence points the other way. LPs were the profitable format, and pop
  LPs were most of Columbia's 1957 dollars ([Keightley, 2004](https://doi.org/10.1177/0163443704042258)).

"Return the investment" in these sources includes recording, artist advances and promotion, and
often allocated overhead. The comparable game measure is all-in release net: production, pressing,
royalties, the corporate per-sale costs and the attributed campaign (A4), but no office overhead.
**Working target: most singles lose money all-in (60-85%). LPs lose money less often than singles,
with no defensible 1960s number.** Neither is a validated historical rate.

## Where the game is

Five-year default (`rj5-def`), releases with final outcomes, before marketing:

| | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Singles losing money | 8.6% of 18,440 | 7.4% of 17,082 |
| LPs losing money | 1.0% of 5,141 | 0.8% of 4,872 |

Counting marketing (A4, `rj5-astack`) barely moves this: singles 8.4% and 9.0%, LPs 0.9%.

**The cause is demand, not cost.** On the 105-week default (seed 1001), a never-charting single
sells a median 8.1k copies at a Small label, 13.1k at a Boutique, 14.9k at an Independent, 18.9k at a
MidTier and 25.6k at a Major. That nets $0.30-0.37 a copy against $350-4,000 of production and
$150-3,000 of campaign. A historical non-charting single sold hundreds to a few thousand. In
`ChartSimulator.CalculateRegionalSales` an uncharted record receives #100's purchase exposure
everywhere, and most Hot 100 entries come from records still climbing after week 6. The flop tail
and the chart-entry mechanism are the same demand.

## Tuning attempts

A new opt-in probe, `--uncharted-single-exposure=F` (`Systems/UnchartedSingleExposure.cs`), scales
the purchase exposure of an AI single with no chart presence from F up to #100's level, by its
regional breakout strength. `--charted-single-exposure-scale` exists for unit renormalization but was
not needed. Off is exact: the first 104 weeks of `rj5-def` match `rj1-base`, built before the probe,
on finance, records, cash and release-strategy streams.

105-week screens, A2-A4 stack in every arm, cells seed 1001 / 2002:

| Shape | Single loss all-in | Distinct Hot 100 acts 1961 | Starting-Small closed |
|---|---:|---:|---:|
| none (`rj1-astack`) | 6.2 / 6.9% | 551 / 543 | 12.1 / 10.0% |
| every uncharted week, F .5 | 11.9 / 12.6% | 454 / 454 | 18.2 / 10.9% |
| every uncharted week, F .25 | 26.7 / 26.6% | 377 / 373 | 23.4 / 13.5% |
| after week 6, F .25 | 18.5 / 18.3% | 455 / 436 | 23.8 / 12.6% |
| after week 6, F .10 | 31.8 / 31.2% | 450 / 436 | 28.1 / 17.8% |
| stalled only, F .25 (kept shape) | 11.0 / 10.3% | 540 / 539 | 17.3 / 9.6% |
| stalled only, F .10 | 12.1 / 11.0% | 564 / 531 | 17.3 / 10.0% |

"Stalled" means uncharted, past week 6, and two weeks past its own sales peak. Only that shape keeps
chart breadth. It saturates near 12%: most of a flop's units come while it is still rising.

At 265 weeks the stalled shape fails the closure band: starting-Small closures are **39.8% / 36.1%**
(default 31.6% / 28.7%). Single losses rise only to 13%. **Rejected for the default.** Small labels
in this economy live on non-charting singles: one seed-1001 window showed 24 charting singles out of 996
for that tier. Taking the flops' profit away kills them before their rare hits can carry them.

**Conclusion.** Within the closure band, no cost or shelf lever reaches the historical loss rate.
It needs a demand pass that moves units from uncharted records to hits: fewer copies for flops, more
for the top of the chart. That also matches this file's earlier note that the #1 sold far below its
historical volume. Small labels also need a survival path that is not flop income, such as regional
hits. That is a chart-calibration change and needs its own decade check.

## A2-A4 stack on the corporate economy

`--single-prior-recalibration --album-prior-recalibration --album-master-costs --release-all-in-economics`,
265 weeks, default vs stack, seed 1001 / 2002:

| Measure | Default | Stack |
|---|---:|---:|
| Starting-Small closed, week 260 | 31.6 / 28.7% | 32.5 / 33.0% |
| 1964 median margin, Major | 13.6 / 12.3% | 13.9 / 13.4% |
| Major-family chart share 1962 | .482 / .450 | **.299 / .328** |
| Major-family chart share 1964 | .549 / .392 | .371 / .363 |
| Distinct Hot 100 acts 1964 | 636 / 622 | 593 / 576 |
| LP unit share 1960 / 1964 | .173 / .295, .175 / .268 | .207 / .313, .202 / .305 |
| Single loss rate | 8.6 / 7.4% (pre-marketing) | 8.4 / 9.0% (all-in) |

The stack stays inside the closure band, moves the 1960 LP unit share toward its ~30% target, and
changes the loss rate by almost nothing. It cuts Major-family chart share by 12-18 points in 1962,
because majors' singles lose most of their overstated prior and their slots go to LPs.
**Not adopted:** that share shift is too large to take without checking it against the Major-share
calibration first.

## B5 attribution

105 weeks, B5 and each part alone against the new default
(`--record-campaign-spend-part=launch|followups|cuts`, a new diagnostic switch). Major-family chart
share, 1960 / 1961:

| Arm | Seed 1001 | Seed 2002 |
|---|---|---|
| default | .513 / .461 | .518 / .462 |
| full B5 | .586 / .509 | .609 / .532 |
| launch factor only | .516 / .491 | .548 / .489 |
| follow-ups only | .604 / .466 | .536 / .553 |
| cuts only | .553 / .481 | .499 / .470 |

Follow-up tranches carry most of the effect, about +5 points averaged. Each tranche multiplies an
already larger Major push by 1.3, whatever it costs, and Majors pass the cash reserve more often:
1.9 follow-ups per Top 40 single against 1.3 for Small labels. The week-6 cut hits 72% of Small
labels' singles against 31% of Majors', but its share effect is small, about +1 point. B5 stays
opt-in. A fix would make a tranche's lift depend on its spend relative to the record's market,
not a flat 1.3.
