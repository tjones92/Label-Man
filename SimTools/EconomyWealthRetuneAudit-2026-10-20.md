# Economy retune: 1960-64 net and cash

**Latest direction:** before further cash reduction, the user requested a
historical assessment of whether the additional surplus is reasonable. See
[EconomyCashHistoricalReview-2026-10-21.md](EconomyCashHistoricalReview-2026-10-21.md).
The parity experiments below remain opt-in research; the +/-15% interpretation
is not a historical target. No new cash-reduction experiment is scheduled from
that tolerance alone.

This follow-up expands the target of
[the pressing-commitment retune](EconomyRetunePressingAudit-2026-10-20.md).
The user now wants single/album net and label cash near the original economy
over 1960-64, allowing differences caused by inventory commitments and behavior.
The existing 20-35% starting-Small closure band remains a constraint. No decade
run is requested for this pass; a later branch audit will test the full decade.

The working interpretation of "around" is within 15% of the original
equal-weight seed means for cumulative single contribution, cumulative LP
contribution, and active-label median cash in each current tier. Per-seed values,
annual values, promotions, closures and chart diagnostics remain visible rather
than forcing individual labels to reproduce their former balances.

All historical A/B/C corrections remain enabled. Prices, royalties, publishing,
physical plant tariffs and insolvency rules are preserved. No cash clamp or
mortality quota is added.

## Complete five-year comparison

Week 260 is December 25, 1964, the final chart week of the year. The original
unbroken runs record 1964 as weeks 209-260. The earlier analysis unnecessarily
excluded the final annual row as incomplete; the new comparison includes all
five chart years, 1960 through 1964.

`compare_economy_wealth.py` compares candidate runs with original same-seed
`decaboff` runs. It reports ratios of equal-weight seed means, using each seed's
active-label median for cash. Composition effects from promotions and different
surviving labels are part of that diagnostic, not within-label counterfactuals.

The previous pressing-only retune over all five years gives single net +10.5%,
LP net +41.9%, and median cash increases of Small +90.9%, Boutique +30.4%,
Independent +43.6%, MidTier +33.2%, Major +17.6%, on seeds 1001/2002.

## Purchase-volume experiment

`--album-purchase-volume=` scales the LP purchase budget after the existing
format allocation. The withheld amount also leaves total regional purchasing
capacity, so it cannot be reassigned to Singles or exported to another region.
Ranking and proportions within the LP allocation continue to use actual demand.
Every fulfilled copy still pays its full period price, manufacturing, royalty
and publishing. The live LP prior uses the same volume multiplier.

The control value is 1.0. This is a calibration assumption about aggregate
purchase volume, not a measured historical estimate or a discount to receipts.
Prewarm and the original omitted-parameter behavior are preserved.

LP orders can use separate assumptions through
`--pressing-album-initial-forecast-share=` and `--pressing-album-repress-weeks=`;
when omitted they inherit the shared pressing parameters. This lets the retune
balance lower sales with less cash tied up in LP stock, retaining stronger
commitments than the original 0.5-forecast/three-week settings.

The first pilot tests LP volume 0.75, single pressing 1.5 forecasts/twelve weeks,
and LP pressing one forecast/eight weeks, on seed 2002. It completes with:

| Measure against original, seed 2002 | 0.75 LP volume pilot |
|---|---:|
| Cumulative single contribution | +11.0% |
| Cumulative LP contribution | +11.3% |
| Small / Boutique / Independent current-tier median cash | +15.9% / +17.8% / +11.6% |
| MidTier / Major current-tier median cash | +42.9% / +5.1% |
| Total active-label cash | +8.0% |
| Starting-Small closure | 21.3% |

The MidTier median includes a composition effect: the surviving original MidTier
cohort's median is only 2.0% above its original-economy counterpart, while the
current-tier population is 34 rather than 37 firms. This is why current-tier
and starting-cohort cash are both retained. It is not grounds to hide a higher
current-tier result or to impose a cash clamp.

The next candidate tests LP volume 0.70 with the same pressing settings, on
matched seeds 1001/2002. Both runs complete: single contribution +10.7%, LP
contribution +3.1%, cash Small +48.0%, Boutique -2.2%, Independent +5.3%,
MidTier -18.1%, Major -17.0% (ratios of two-seed means). Closure is 28.6% and
21.3%. The residual Small cash and lower upper-tier medians are not solved by
another uniform LP volume cut.

## Profit-funded owner-manager compensation

The next experiment adds explicit owner-manager remuneration as an operating
cost. This is a model calibration assumption, not a sourced wage quote. It
does not change per-copy receipts or record contribution, and it is logged
separately in `*-operator-pay.csv`.

`--label-operator-monthly-pay=` is a maximum, default zero. At each month end,
after ordinary overhead, an AI firm pays the minimum of this cap, half its
positive operating cash surplus, and cash above six months of ordinary overhead
plus outstanding plant payables. A loss-making month or a firm without that
cash buffer pays nothing. The same rule applies across all AI tiers; the player's
personal cash is exempt. Payment is deducted once from cash and monthly expenses
and is included in the monthly financial history before status review and
reinvestment. The existing bankruptcy rule is untouched. No new saved fields
are needed: cash and monthly expenses already persist; the configured cap is
retained in audit invocation/world metadata like other experiment flags.

The paired pilot uses LP volume 0.725, Single pressing 1.5 forecasts/twelve weeks,
LP pressing one forecast/eight weeks, and a $400 monthly compensation cap.
The paired pilot completes with single contribution +10.6%, LP contribution
+7.0%, and current-tier median cash Small +23.0%, Boutique +4.5%, Independent
+8.5%, MidTier -2.0% and Major -11.0%, against original equal-weight seed means.
Starting-Small closures are 34.2% and 30.9%. It does not satisfy the working cash
parity test, and it is not adopted. The source also contains an unvalidated
refinement that protects another two months of overhead from the surplus
before calculating pay. Neither this refinement nor the pilot establishes a
historically missing wage expense; further tuning is suspended for the
historical review above.

## Checks

The budget/format-specific pressing check passes six native probes; pressing
commitment and B/C checks pass 17 and 49 respectively. The existing pressing
check also passes. With the volume parameter omitted, a new 13-week control
reproduces all 7,800 finance rows of the prior pressing-only configuration,
together with release, career and deal streams. Eight Python tests check cohort
completion, the inclusion of 1964, missing-year rejection and ratio-of-means
aggregation while retaining individual seed outliers.
The owner-manager payment check passes eight probes for exact-off behavior,
cash/expense/telemetry agreement, positive surplus, runway, plant bills, player
separation and RNG. A further 13-week flag-off control reproduces the same
7,800 finance rows after this feature is added.

Generated evidence is written under `SimLogs/economy-wealth-*`.
