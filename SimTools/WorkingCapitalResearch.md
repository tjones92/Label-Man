# Working capital: distributor terms, the hit squeeze and a proposed experiment

October 22, 2026. Follows the `bcs-v1` founder-compensation screen
([BoutiqueLabelResearch.md](BoutiqueLabelResearch.md)), whose remaining
Boutique bankruptcies were mostly owed far more than they were short. Research
and proposal only; no simulation behaviour changes here.

## Recommendation

Do **not** shorten distributor terms or add generic credit against receivables.
The game's 12–18 week wholesale terms match the period source, and the failing
firms are mostly one-hit labels. The period evidence treats them as genuinely
exposed, not mis-modelled. Screen instead an opt-in **distress master sale**: a
cash-short independent that owns a live hit can sell or lease it to a larger
firm, at a buyer's evidence-based price, before its second negative month-end.
That is the period's documented escape (Sun → RCA). It is earned, it costs the
label its hit, and Vee-Jay's ending stays possible when no buyer pays enough.
A Billboard-faithful "hot label collects faster" rule is a cheap secondary
arm, but the data says it is nearly inert for Boutiques.

## What the sources establish

| Source | Evidence | Implication |
|---|---|---|
| Billboard, 14 March 1964, p. 1, "Collections Are Worse Than Ever" ([scan](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1964/Billboard%201964-03-14.pdf)) | Collections a "long-time problem"; some firms wait four to six months. A leading chain account moved from a 90-day policy to 100–120 days. Volume accounts hold about 80% of the business. Hot labels are paid more promptly. Per manufacturers, one or two hits do not make a hot label. One firm held back shipments of a coming hit to make accounts pay. | Terms of 90–120+ days are period-correct. Collection speed depends on a **sustained** flow of product (leverage), not a single hit. Primary trade source, 1964, describing a condition it calls long-standing. |
| Oxford American, "How Jukeboxes Made Memphis Music" ([link](https://oxfordamerican.org/magazine/issue-127-winter-2024/how-jukeboxes-made-memphis-music)); Memphis historical marker ([HMdb](https://www.hmdb.org/m.asp?m=149041)) | Buster Williams pressed about a thousand records on credit for new labels (Sun, Chess, Stax), repaid "if the label sold". His jukebox routes paid his distributorship, which paid his plant. | Plant credit existed and could follow collections. But this is a vertically integrated operator who controlled the cash route, not a general plant term. |
| Vee-Jay/Ace history ([Historical Text Archive](https://historicaltextarchive.org/sections/article/404-vee-jay-records-ace-records)) | Vee-Jay was undercapitalized and could not fund pressing fast enough. The article calls this the independent's Achilles heel. Ace's distributors withheld payment once they believed Ace was closing. Vee-Jay went bankrupt in 1966 despite major hits. | Hit-driven cash failure was real. Collections can **worsen** for a label seen as failing. Secondary source; some dates uncertain. |
| Sun → RCA, November 1955 ([Country Music Hall of Fame](https://countrymusichalloffame.org/hall-of-fame/sam-phillips); [PMA](https://pmamagazine.org/from-memphis-to-mainstream-how-rca-made-elvis-presley-the-king-of-rock-n-roll/); [Ultimate Classic Rock](https://ultimateclassicrock.com/elvis-presley-leaves-sun-records/)) | Sun's cash flow was never solid. Sources describe debt and pressure; RCA paid $35,000 for Presley's contract, and Phillips paid debts and reinvested. | The documented escape for a cash-short indie with a valuable hit asset was a sale to a larger firm. The breakdown of the $35k differs between sources. |

**Not found:** a period source for factoring record receivables or for bank
lines secured on distributor invoices. These may have existed, but the
experiment does not rely on them. There is also no source for the game's
"net 30 = four weeks" plant term; the code already labels it an assumption.

## What the game does now

- Wholesale receivables mature 12–18 weeks after billing
  (`IndependentDistributorFactory.PaymentTermWeeksFor`, "90–120 day terms").
  Unreliable houses settle short, not late. That is consistent with Billboard.
- Plant credit is on by default at four weeks (`LabelPressingEconomics.PlantTermWeeks`).
- Bankruptcy is two consecutive negative month-ends; receivables are never
  spendable (`EconomicLabelLifecycle.ReviewMonth`).
- AI backers lend only toward production (`AiBackers.TryBorrow`). AI master
  deals shop unreleased singles and lease regional breakouts priced up to about
  $1,500; nothing responds to an overdraft.

The structural squeeze: when a hit ramps, a label pays the plant within four
weeks for copies that distributors pay for 12–18 weeks later.

## The squeeze in `bcs-v1` (founder compensation on, 3 seeds)

`analyze_working_capital.py` (run from the repository root after
`analyze_boutique_closures.py`). A "squeeze" bankruptcy has receivables at exit
of at least twice the overdraft.

| Remaining Boutique bankruptcies (78) | Squeeze (54) | Other (24) |
|---|---|---|
| Median overdraft at exit | $11.4k | $1.5k |
| Median receivables at exit | $73.3k | $0 |
| Median plant payments, last 26 weeks | $44.1k | $1.3k |
| Median gross, last 26 weeks | $212k | $3.4k |
| Hot 100 record in the last 8 weeks | 40 | 1 |
| Top 40 record in the last 26 weeks | 31 | 1 |
| Median exit week | 142 | 70 |

The "other" group is ordinary failure: no sales, no receivables. The squeeze
group is labels dying during or just after a hit. Distinct Hot 100 records in
the 52 weeks before exit: 0 for 8 of the 54, exactly 1 for 31, 2 for 13, and
3 or more for 2.

By Billboard's own description, very few of these are "hot labels." Only 14
have two or more charting records plus one charting now; only 2 have three or
more. Most are one-hit indies, the population the 1964 article says lacked
collection leverage.

The pattern is not Boutique-specific, and it predates the compensation change.
Squeeze bankruptcies are 10–14% of every starting independent tier in both arms
(control/pay: Small 75/81 of 693, Independent 86/78 of 621, Boutique 41/54
of 399). Rescuing them all would take Boutique closures from 20.6% to about 7%
and Small from 21.6% to about 10%. Both are below the 15–30% design prior,
and they would erase a failure the period sources describe. A blanket fix
would impose survival rather than earn it.

## Proposed experiment: `--distress-master-sales`

Opt-in, AI-only, independently owned Small/Boutique/Independent sellers.
Requires `--ai-master-deals`. Control: the existing `bcs-v1-pay` runs, reused.

1. **Trigger.** At the monthly review where an eligible label's cash is
   negative and it is still within the existing one-month grace
   (`insolventMonthEnds == 1`). No forecast; only an actual overdraft.
2. **Asset.** Owned masters with Hot 100 presence or live sales in the last 8
   weeks, strongest trailing label-net first. No catalog, publishing or roster
   is sold; the artist stays signed, as in the existing deals.
3. **Buyers.** The existing `AiMasterDeals` buyer search (larger tier, genre
   fit, `MaxApproaches`). A buyer pays only from cash above its own
   commitment reserve.
4. **Price.** Use the buyer's valuation of the record's expected remaining label
   net from trailing sales evidence and the existing decay. Discount it as a
   buyer's market, as an explicit scenario coefficient. Never price at "what the
   seller needs."
   The seller accepts the best offer only if it clears the overdraft plus one
   month of mandatory overhead. Otherwise no sale, and the ordinary second
   negative month-end bankrupts it (Vee-Jay).
5. **Accounting.** Receivables billed before the sale stay with the seller.
   Future sales and their receivables go to the buyer. Book the price once as
   a master-sale receipt (not operating revenue, not founder-pay surplus, which
   already excludes non-operating cash). The seller's plant payables stay its
   own.
6. **Determinism.** No new random draws, so buyer order is the existing
   deterministic ranking. Any new state must round-trip through save/load. A
   switch-off replay must be byte-identical to `bcs-v1-pay`.

**Report:** distress sales per tier and seed; price ÷ overdraft; buyer tier;
seller survival 6 and 12 months after the sale (did it only delay closure?).
Also: closures by tier and cause, squeeze vs other; founder income; total and
Major/MidTier unit shares with chart-unit attribution, since this moves hits to
larger firms and Major share already rose 2.4 ± 1.6 points under compensation.
Plus the usual chart and genre guards against a reshuffle-noise reference.

**Expectation, not a target:** only squeeze firms with a live, valuable hit
qualify. Somewhere between a handful and a few dozen pooled Boutique sales
seems plausible. Closures should stay inside 15–30%; falling below 15% means the
price or trigger is too generous. No quota or per-seed tuning.

### Secondary arm: `--hot-label-collections` (optional)

Billboard's leverage rule: while a label has a Hot 100 record **and** at least
three distinct charting records in the trailing 52 weeks, receivables from
houses carrying it mature at billing + 8 weeks instead of 12–18. Pure timing:
amounts, reliability and write-offs are unchanged. The billing week is
recoverable as due week − house term, so no new save field is needed. The
threshold comes from the source ("one or two hits do not make a hot label");
the 8 weeks is a scenario value. It would reach only 2 of the 54 Boutique
squeezes, so it is mainly a check on Independents. The Ace-style converse
(houses withholding from a failing label) is real but would only add
closures; leave it for later.

### Not proposed

- Shorter or uniform distributor terms: they match the source.
- Credit secured on receivables (factor or bank line): no period source found.
- Longer plant terms for all labels: the source is one integrated operator. It
  would rescue one-hit squeezes wholesale, the overshoot above.

## Implemented: `--distress-master-sales`

`AiMasterDeals.TryDistressSale`, called from `CompetitorManager.ProcessLabelMonth`
after owner pay, operator pay and reinvestment decisions, before the month
closes. It requires `--ai-master-deals` and the economic label lifecycle.

- **Trigger:** an AI, independently owned Small/Boutique/Independent label whose
  last month-end was negative and whose cash is still negative
  (`insolventMonthEnds >= 1`). Need = overdraft + one month of mandatory overhead.
- **Asset:** owned live singles not on any lease, most valuable first; sale only
  (no lease). The artist stays signed; earlier receivables stay with the seller.
- **Price:** current weekly units × observed week-on-week carry (capped at 0.9;
  a climbing or flat record is valued at the cap) ÷ (1 − carry) × realized label
  net per unit × 0.5. The 0.9 cap and 0.5 discount are scenario coefficients.
  Realized net per unit on squeezed labels' hits is the same as other indie
  hits (about $0.26), so the price is not depressed by repress timing.
- **Buyer:** the existing buyer list (larger tier, sound fit, at most four
  offices), the existing ear bar, and `CanAffordToSign(price)`. Booked through
  `PayMasterAdvance` (Licensing, as existing master sales). That makes it a
  taxable gain on an already expensed recording, and the proceeds are excluded
  from the cash flow `ReviewMonth` reads. No new random draws or save fields.

`RunDistressSaleAudit.ps1` ran 7 native checks (including
`DistressMasterSaleChecks`, 21 assertions) and a 13-week round trip, all passing.
The switch-off seed-1001 replay is byte-identical to `bcs-v1-pay` in all 95 CSV
streams. Three 265-week worlds then ran with the switch on (`dms-v1-sale`):
exit 0, empty error logs, cash bridges reconcile. Analysis:
`analyze_boutique_closures.py` (`control=bcs-v1-pay`, `pay=dms-v1-sale`),
`analyze_boutique_compensation.py --tag dms-v1-sale --control bcs-v1-pay
--details SimLogs/dms-v1/closures --output SimLogs/dms-v1/screen.json`, and
`analyze_distress_sales.py`, all from the repository root.

## Screen result (`dms-v1`, 3 seeds): nearly inert

**13 sales in three five-year worlds** (4 / 3 / 6). Sellers' current tiers:
8 Independent, 4 Small, **1 Boutique**. Buyers: 8 Major, 5 MidTier. Median price
$2.6k against a median need of $1.9k; the largest are $44–58k for Top 20
singles. At the sale, sellers held a median 20× their overdraft in
receivables. 11 of 13 sellers were alive at the end; of the 10 with a year
observed, one closed within 52 weeks.

**Why so few.** There are about 65–70 squeeze bankruptcies per seed. At the review
that closes them, the median squeezed label needs $15–19k, while its best
single's forced-sale value is about $2k. Roughly 30 of 66 have no single still
selling, and only 3–5 per seed clear the bar (9–11 even at full price). By the
second negative month-end the hit has faded, and its value is already in
receivables, which this design does not sell. Sun sold an artist contract
(future records), not the tail of a single.

| Paired, sale − control (3 seeds) | Control | Distress sales | Difference |
|---|---|---|---|
| Starting Boutique closed | 20.6% | 17.3% | −3.3 ± 2.9 |
| Specialist proxy closed | 17.2% | 11.4% | −5.9 ± 3.9 |
| Starting Small closed | 21.6% | 21.9% | +0.3 ± 1.4 |
| Starting Independent closed | 26.0% | 25.9% | −0.15 ± 0.15 |
| Major share incl. off-chart | 33.0% | 31.3% | −1.6 ± 0.4 |
| Live single units | 767.6M | 770.3M | +2.6M ± 1.1M |

**The Boutique and Major rows are reshuffle noise, not effects.** There is one
Boutique sale in all three worlds, at week 214 of seed 3003, yet that seed's
Boutique closures move 21.6% → 13.6%. Genre shares are identical until the first
sale (weeks 87–113), then diverge by the usual reshuffle amount. Sales move
hits *to* Majors, yet Major share falls in all three seeds. Under `bcs-v1` it
rose 2.4 points (−0.2, +5.3, +1.9); here it falls 1.6 (−0.9, −2.2, −1.8). With
only three diffs, a paired SE understates reshuffle variance. Treat per-seed
swings of a few points in Boutique closures or Major share as noise unless a
mechanism ledger accounts for them. That also widens the honest uncertainty on
`bcs-v1`'s −15 points, though that result has its own mechanism evidence.

**Verdict:** mechanically sound and historically faithful, but it does not reach
the squeeze. **Adopted into the working bundle** (2026-10-22, user decision) on
historical-accuracy grounds: a forced sale of a live master is a route a
cash-short indie had, and it costs nothing measurable. It models a narrower
escape than Sun → RCA (a single's tail, not an artist contract), and its 0.5
discount and 0.9 carry are scenario values. It is not a game default until the
bundle's decade A/B. `dms-v1-sale` is the control for later screens.

## Found during the screen: tax is paid on uncollected sales

One seller (`label_0350`, seed 1001, week 92) sold a Top 30 single for $15,250
against a $9,114 need. It still closed that month: the quarterly corporate tax
payment in the same close took **$25,086**. Pretax earnings that month were
$38k, most of it receivables growth.

`LabelCompanyTax` accrues tax on management earnings, including the change in
receivables, and pays the provision in cash every quarter of the **current** year
(`PayQuarter`, months 3/6/9/12). `analyze_tax_timing.py` on the control
(`bcs-v1-pay`): in **79 of 213** squeeze bankruptcies (37%), and 98 (46%) at the
half-overdraft level, cash tax paid in the final six months is at least the
overdraft at exit. Never in the 141 other bankruptcies. Squeezed firms show a
median $47–54k pretax over those six months against $66–77k of receivables
growth. They pay tax in cash on sales whose cash arrives 12–18 weeks later.

**Period evidence (moderate).** Before the Revenue Act of 1964, a corporation
paid current-year installments only on half its liability *above $100,000*
(September and December). The rest came after the year ended. The 1964 Act
phased current payment in through 1970, and the Tax Adjustment Act of 1966
accelerated that phase-in ([Tax Policy Center, major enacted tax
legislation](https://taxpolicycenter.org/sites/default/files/statistics/pdf/major_bills_description.pdf);
prior-law detail from search excerpts of [Duke Law Journal 1964](https://scholarship.law.duke.edu/cgi/viewcontent.cgi?article=1940&context=dlj)
and [Washington Law Review 1964](https://digitalcommons.law.uw.edu/cgi/viewcontent.cgi?article=1442&context=wlr),
whose full text was not retrievable here). The post-year-end months are
commonly given as March 15 and June 15, but that is not verified from a source
here. Every Label Man indie's liability is far below $100,000, so in 1960–63
none of it would have been due during the year it was earned.

## Implemented: `--prior-year-corporate-tax`

`LabelCompanyTax.PayPriorYearSchedule`, used by `PayQuarter` when the switch is
on (requires the corporate income tax). Accrual, brackets, loss pools and the
liability are unchanged; only cash timing changes. Half of the year's liability
above $100,000 is paid in the current year: 25% of the year-to-date excess in
September, then the December top-up. The remainder becomes `priorYearTaxDue`
at the December close and is paid half in March and the rest in June. An
overpaid year is refunded in March. Three new saved fields on
`LabelCompanyState`; no random draws. All years use the pre-1964 rule: the 1964
and 1966 Acts' faster schedule for the over-$100k share is not phased in. That
affects Majors' timing from 1964 on, not indies. The commitment reserve still
holds back tax owed, so firms keep budgeting for it. A firm that closes before
paying still owes it.

`RunTaxTimingAudit.ps1` (bundle = `dms-v1-sale`, i.e. distress sales included):
8 native checks passed, including `TaxTimingChecks` (17 assertions), plus the
13-week round trip with the switch on. The switch-off seed-1001 replay is
byte-identical to `dms-v1-sale` in all 95 CSV streams. Three 265-week worlds
(`ttx-v1-tax`) ran: exit 0, empty error logs, cash bridges reconcile. Analysis
as above with `control=dms-v1-sale`, `pay=ttx-v1-tax` into `SimLogs/ttx-v1/`.

## Screen result (`ttx-v1`, 3 seeds): removes the tax squeeze

**Mechanism ledger (all starting Small/Boutique/Independent, pooled):**

| | Control (`dms-v1-sale`) | Prior-year tax |
|---|---|---|
| Squeeze bankruptcies (receivables ≥ 2× overdraft) | 193 | **138** |
| …of which 6-month tax paid ≥ exit overdraft | 63 | **4** |
| Other bankruptcies | 151 | 142 |

The 55 fewer squeeze bankruptcies are the tax-driven ones; ordinary failures
are unchanged. This is an effect with its own ledger, not reshuffle noise.

| Paired, tax − control (3 seeds) | Control | Prior-year tax | Difference |
|---|---|---|---|
| Starting Independent closed | 25.9% | 20.4% | **−5.5 ± 1.1** (26.1→18.4, 23.9→19.3, 27.6→23.5) |
| Starting Small closed | 21.9% | 19.6% | −2.3 ± 3.1 |
| Starting Boutique closed | 17.3% | 13.7% | −3.6 ± 4.3 (15.6→13.3, 22.6→11.0, 13.6→16.8) |
| Specialist proxy closed | 11.4% | 9.6% | −1.7 ± 5.8 |
| Live single units | 770.3M | 766.7M | −3.6M ± 1.5M (−0.5%) |
| Unit share: Independent / Small | 39.7 / 9.1% | 41.7 / 7.9% | +2.0 ± 1.0 / −1.2 ± 0.6 |
| Major share incl. off-chart | 31.3% | 29.9% | −1.4 ± 1.0 (−3.1, −1.5, +0.3) |

Chart guards are unchanged: mean chart life 9.6–9.8 weeks, annual Hot 100
acts, 1964 LP share (+0.6 to +2.6 points). Genre L1 shifts are reshuffle-sized.
Owner income is flat (survivors' median founder salary −$13 ± 8).

**End cash must be read net of deferred tax.** The run ends in January 1965,
when the 1964 liability is not yet due. Major end cash shows +$9.3M (+64%), but
net of tax payable it is $15.9M vs $15.5M. Boutique survivors' median cash
shows +$25k; net of tax it is +$6.4k ($47.2k vs $40.7k).

**Assessment.** This is historically correct timing that removes a specific,
unhistorical failure mechanism, and the strongest effect lands where the
mechanism predicts (Independents, which carry larger receivables). The
cumulative bundle now puts starting Boutiques at **13.5% pooled** (54/399) and
the specialist proxy at 9.6%. Both are below the low-confidence 15–30% design
prior, though Boutique per-seed swings of ±4–8 points are within reshuffle
noise. Do not tune toward the prior (no quota); confirm in the bundle's decade
A/B. If Boutique closures stay below ~15% there, look first at what keeps weak
specialists alive (e.g. dormancy rules), not at re-imposing tax timing.
**Adopted into the working bundle** (2026-10-22, user decision). It is not a
game default until the bundle's decade A/B. `ttx-v1-tax` is the control for
later screens; its bundle is the `RunTaxTimingAudit.ps1` list plus
`--prior-year-corporate-tax`.
