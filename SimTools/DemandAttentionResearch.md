# Demand, discovery, and release risk in the 1960s record business

Research and implementation proposal — October 21, 2026

This pass follows the user's request for research and proposals only. The handoff is background, not permission to implement its prototype plan. No simulation code, defaults, or existing research documents were changed; no new simulations were run. Measurements below use existing `rj5-def-default-{1001,2002}-265` logs. Code review refers to checkout `970d639cde839146a450fe1d0ab07192cd647d02`.

## Recommendation

The strongest next direction is **local discovery with uncertain commercial response, coupled to prospective pressing and distribution commitments**. It could produce many inexpensive unsuccessful releases, occasional valuable regional hits, and losses from committing too much stock before demand is known. It also offers a credible survival path for a careful small label.

Do not calibrate that system to an 88% Hot 100 sales share or a supposedly established 60–85% release-loss rate. Neither is supported by the evidence examined here. Do not simply strengthen the quality exponent: in the existing runs, releases above 0.6 runtime quality at launch already almost never lose money before attributed marketing.

The research supports mechanisms more strongly than numerical outcome targets. Consequently, the first proposed implementation phase is a measurement and accounting audit, followed by separate discovery and inventory experiments. The existing genre, chart, volume, and label-survival calibrations should remain regression constraints until a specific historical reason justifies revisiting them.

## 1. Corrections to the starting assumptions

| Starting claim | Research finding | Consequence |
|---|---|---|
| The Hot 100 historically captured about 88% of weekly single sales | The calculation treats a hypothetical chart-score curve as a unit-sales curve and combines it with unverified unit anchors | Withdraw this as a historical target |
| Most singles lost money, specifically 60–85% | A plausible working hypothesis about risk, but no representative 1960s US release-profit dataset was established | Keep any such range explicitly experimental |
| A 30% returns replay produced approximately 66% release failures | The original table measures **starting-Small label cash failures over five years**, not losing releases | It cannot validate a release-loss target |
| Conserving attention will conserve annual units | Exposure and purchases are different quantities; conversion, availability, repeat exposure, and the outside option intervene | Measure both and calibrate separately |
| Good records should reliably survive and weak ones should fail | Distribution access, local fit, working capital, and uncertain demand affect outcomes too | Quality should improve expected chances without guaranteeing profitability |
| City survey evidence establishes a 45–65% region-overlap target | The small station sample supports geographic variation, not that precise region-level band | Treat the band as a design hypothesis |
| About 30% LP unit share in 1960 describes the present baseline | These two runs actually have 17.3% and 17.5% | Resolve the intended benchmark before future comparisons |

The 65.7% counterfactual is explicitly a static five-year cash-failure measure in [SmallLabelAttritionAndChartMethod.md, section 1](SmallLabelAttritionAndChartMethod.md). It uses an older economy, a different failure definition, and no behavioral adaptation. It is neither a measured historical returns rate nor a result from the current model.

### The chart curve is not a sales distribution

The exact coefficients appear in William F. Carroll Jr.'s *Not So Lonely at the Top*, accepted in 2014 and published in 2015. Its page 14 curve, `Score = -4357 × Rank / (Rank + 10) + 4139`, fits hypothetical unanimous polling scores constructed from an account of Billboard's methodology. The article's relevant methodology description concerns the system introduced in June 1973. It does not estimate 1960s physical unit sales by rank. [Carroll, accepted manuscript, pp. 3 and 14](https://img1.wsimg.com/blobby/go/2c2a33c8-8289-4d2f-b0b6-15982c9f5326/downloads/1cfe50g23_15133.pdf).

I did not verify an independent source for either 150,000 weekly copies at #1 or 7,200 at #100. Their ratio closely follows the same score curve, so they cannot be treated as independent corroboration. There is consequently no evidential basis here for assigning 88% of sales to the Hot 100 or dividing the remainder between Bubbling Under and all other records.

This does not prove the current sales tail is correct. It removes a misleading reason to declare it three or four times too large. Keep chart scoring, sales concentration, and chart-driven exposure as separate questions. Changing this existing curve immediately would also disturb an established calibration; the proposal is to correct its evidential status first.

### “Failure” needs a denominator and an accounting definition

Richard Osborne's research examines the industry's recurring “one in ten” claim and its uncertain empirical foundations. I could inspect indexed text but not retrieve the complete repository PDF in this pass. The earlier project's Denisoff citation is a later retrospective source, not a newly verified 1960s release ledger. Neither establishes a common accounting boundary, representative label sample, or fixed observation horizon. [Osborne, repository manuscript](https://repository.mdx.ac.uk/download/b61e93c61442e786c576d18db55c458e05562221b8712b6e95506ff465bc2209/176623/Richard%20Osborne-I_Am_a_One_in_Ten.pdf); [existing source review](ReleaseLossCalibration-2026-10-21.md).

For future work, distinguish:

- **Commercial disappointment:** missing an expectation or failing to chart.
- **Release contribution loss:** receipts less directly attributable production, manufacturing, royalties, distribution, and promotion costs.
- **Fully allocated loss:** contribution less an explicitly stated share of overhead.
- **Artist unrecouped balance:** a contractual account that need not equal label profit.
- **Label insolvency:** inability to meet obligations when due, even with potentially valuable assets or profitable eventual sales.

Likewise, aggregate LP profitability does not establish that most individual LP releases made money. A few large successes can support an otherwise loss-making portfolio. This pass supplies no defensible numerical 1960s LP loss target or Small-label mortality rate. The 20–35% closure band remains a gameplay constraint.

## 2. Market size: useful contemporary evidence, incompatible vintages

The table below deliberately preserves publication vintages. Values are millions of US single units, not dollars. They are period trade estimates, not a harmonized audited series.

| Calendar year | Earlier Billboard directory estimates | Revised 1969–70 directory |
|---|---:|---:|
| 1960 | Not verified here | — |
| 1961 | 182 | — |
| 1962 | 210 | — |
| 1963 | 222 | — |
| 1964 | 226 | — |
| 1965 | 247 | — |
| 1966 | **275** | **183** |
| 1967 | — | 187 |
| 1968 | — | 183 |
| 1969 | Not verified here | — |

The 1961–64 numbers are in the [1965–66 directory, printed p. 16](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1965/Billboard%201965-08-07-International-Buyers-Guide.pdf). Its 1964 channels are 101 million non-rack retail, 71 million rack, and 54 million jukebox purchases. The [1967–68 directory](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1967/Billboard%201967-08-19-II-International-Buyers-Guide.pdf) extends the earlier estimates through 1966. These tables were located through indexed document text; their full scans were not successfully downloaded this pass.

The [1969–70 directory, printed pp. 9–11, accessible transcription](https://device.report/m/89f9b18cd728245667bdd25c46561b0e56c109653914cb67e221b8282a66cfd1) describes revisions using several industry sources. Its 1966 single total is about one third below the earlier estimate. For 1966–68 it reports jukebox purchases of 54, 57, and 57 million and LP totals of 180, 192, and 196 million. [Original directory](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1969/Billboard%201969-08-30-Buyer.pdf).

**Interpretation:** do not splice 247 million in 1965 onto 183 million in 1966 and simulate a collapse. First identify what the revision changed. Nor should these estimates silently replace the accepted game totals: their inclusion of operator purchases, catalog, specialty markets, and other categories must match the simulation's coverage. The original ~190 million estimate for 1960 remains unverified here.

A second useful contemporary denominator is Billboard's number of singles reviewed: 5,797 in 1960, 6,036 in 1961, and 6,690 in 1962. These count reviews, not a complete census of unique domestic commercial releases, and give no sales or profit distribution. [1963–64 directory, printed p. 18](https://device.report/m/2c5f2b887d781390d09827e54f62cf75e67e290d1e060cb96fe613af84e3fa38).

These numbers justify investigating release supply and market coverage together. They do not, by themselves, justify adding thousands of releases or dividing annual units by reviewed titles to obtain a typical record's lifetime sales.

## 3. Local successes, small pressings, and distribution

| Case | Evidence | What it can establish |
|---|---|---|
| Rufus and Carla Thomas, “Cause I Love You,” 1960 | The Stax Museum reports 40,000 regional copies and subsequent Atlantic interest; its account describes a master-leasing relationship | A regional success could create meaningful revenue and unlock a larger partner |
| Rockin' Robin Roberts, “Louie Louie,” 1961 | HistoryLink reports nearly 15,000 Northwest copies within weeks, with Seattle radio exposure | National chart prominence is not necessary for substantial local demand |
| The Sonics, “The Witch,” 1964 | HistoryLink reports approximately 29,000 Seattle–Tacoma sales and differing acceptance by local stations | A strong local record could encounter programming barriers even within its home market |
| The Individuals, Virginia, 1967 | A participant account describes an initial 500 copies, 300 allocated to radio promotion, local store sales, and a reorder | Pressed, promoted, and sold copies must be separate quantities; a small initial batch need not be the whole release |

Sources: [Stax Museum](https://staxmuseum.com/1957-1968/); [HistoryLink, Rockin' Robin Roberts](https://www.historylink.org/File/9217); [HistoryLink, the Sonics](https://www.historylink.org/File/8844); [The Individuals, including Ronnie Couch's correction](https://garagehangover.com/individuals/).

These are retrospective institutional, oral-history-based, or participant accounts. Confidence is moderate for the individual examples and low for extrapolating population parameters. The cases do **not** establish a median first pressing, median non-charting sales, or the prevalence of regional success. They also do not collectively verify “never Hot 100” status across every possible reissue and chart attribution. Treat the numerical examples as scenario fixtures, not a random sample of flops.

The local mechanism is nevertheless persuasive. A label can test a record with a few outlets, receive evidence, reorder, and either retain a regional business or negotiate wider distribution. That is different from every release automatically selling several thousand copies throughout its allotted territory.

The Stax account also describes the Satellite Record Shop supporting the premises and supplying market knowledge. This supports optional, specifically modeled side businesses. It does not support giving every small label a generic income subsidy. Publishing, studio, retail, and performance receipts belong to the entities and contracts that actually earn them; artist touring revenue should not automatically become label revenue. [Stax Museum](https://staxmuseum.com/1957-1968/).

Local success should not guarantee national diffusion. A contemporary June 1965 report relays Liberty A&R executive Don Blocker's observation that tighter playlists could inhibit the spread of regional hits between cities. That is evidence for selective diffusion and gatekeepers, not a universal distance-based timetable. [Billboard, June 5, 1965, “Regional Hits Losing Spread Appeal: Blocker”](https://www.worldradiohistory.com/Archive-Billboard/60s/1965/Billboard%201965-06-05.pdf).

## 4. Costs, returns, and the missing timing of risk

The strongest evidence here establishes commercial practices, with weaker evidence for representative magnitudes.

| Item | Evidence and qualification | Proposed use |
|---|---|---|
| 45 manufacturing | A transcription of Billboard's March 30, 1959 pressing report gives roughly 11–13 cents per standard 45 at 5,000-copy quantities | Check existing unit-cost assumptions at that scale; do not apply this price automatically to a 500-copy run |
| LP manufacturing | Billboard's June 20, 1960 report discusses approximately 40-cent outside pressing, lower basic costs around 30–33 cents, and an RCA offer around 28–30 cents with credit | Preserve scale and supplier differences; these are manufacturing costs, not complete release budgets |
| Return privileges | A September 1960 report describes a Los Angeles distributor offering full guarantees on singles; a November report describes Capitol exchange privileges up to a specified quantity per title | Represent contract-specific exposure; “100% exchange” is a privilege, not a measured 100% return rate |
| Sessions, promotion, royalties, distribution | Existing project assumptions and individual historical examples are available, but no representative tier-specific complete cost stack was verified here | Retain transparent assumptions until comparable invoices or accounts can anchor them |

Sources: [1959 report transcription and reproduction](https://microgroove.jp/2024/10/how-records-were-are-manufactured-4/) (the relevant text identifies March 30, 1959; the surrounding page contains other dates and links); [Billboard, June 20, 1960, p. 1](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1960/Billboard%201960-06-20.pdf); [Billboard, September 5, 1960, p. 4](https://retrocdn.net/images/3/38/Billboard_US_1960-09-05.pdf); [Billboard, November 21, 1960, p. 22](https://retrocdn.net/images/a/ac/Billboard_US_1960-11-21.pdf).

The 1959 figure was checked in the reproduction/transcription, not independently against a newly downloaded original scan. The June 1960 issue is also present in the project's existing PDF cache. Returns articles were checked through indexed primary-source text. No representative 1960s US singles return percentage was established, and no source examined quantifies whether low sales or excess stock caused most losses.

The accounting distinction matters. Unsold warehouse stock, free promotional copies, retailer returns, distributor credits, and bad debts are different events. A reported 4% difference between pressed and sold copies measures none of them separately. A flat returns percentage applied as an extra manufacturing surcharge could double-count losses already charged to inventory.

A useful future break-even statement would be:

> Net collected receipts must cover recording and campaign commitments, manufacturing of **all** copies, royalties and contract expenses, and any separately stated overhead allocation.

If a forecast commits to a fixed pressing batch, dividing production cost by per-unit margin is insufficient. Promos and leftovers already consume cash, invoices may be collectible later, and exchanged copies may remain saleable. Conversely, once an item has been expensed, writing it off again must not create a second manufacturing loss.

The current project already contains plant credit, wholesale receivables, distribution advances, and publishing mechanisms. The proposal is to reconcile and extend their timing and allocation, not to add duplicate versions of them.

## 5. Attention is finite, but not a single pool of teenage buyers

Physical record purchases by jukebox operators are economically significant in the contemporary estimates above. They create two separate effects: an operator purchases a disc, then many listeners may hear it. A play is not another record sale, and coin receipts are not automatically label income. One-stop, distributor, and retail transactions must likewise not count the same disc repeatedly as final demand.

The historical directory also estimates approximately 435,000 jukeboxes and 49 million jukebox single purchases in 1962. Their ratio is about 113 singles per box annually, or 2.2 weekly. This is an aggregate arithmetic check, not a uniform purchasing schedule or a count of unique new titles. [1963–64 directory](https://device.report/m/2c5f2b887d781390d09827e54f62cf75e67e290d1e060cb96fe613af84e3fa38).

There is no defensible universal capacity constant from this pass for shop assortments, stations per city, or teenagers' monthly single purchases. “Top 40” should not become a literal forty-slot rule for every station; survey length, playlist size, spin concentration, and new-record auditions are distinct. The cached station surveys establish ranked lists, not complete exposure logs. Jukebox slots also require care because one physical disc usually contains two selectable sides.

**Proposed distinction:** ration the weekly flow of auditions, programming, stocking decisions, and listener exposure. Do not force remembered awareness across all records to sum to one. A person can know hundreds of songs while hearing only a limited number this week.

### What the cached regional surveys actually say

I reran the existing overlap analyzer against its cached inputs, without fetching a new survey panel. It parsed 63 charts and formed 59 pairs dated within three days:

| Station pair | Pairs | Mean overlap | Median overlap |
|---|---:|---:|---:|
| WLS–KRLA | 18 | 29.3% | 27.3% |
| WLS–KDWB | 20 | 50.0% | 50.0% |
| KRLA–KDWB | 21 | 39.8% | 36.4% |
| All pairs | 59 | 40.1% | 45.5% |

These are outputs of [analyze_survey_overlap.py](analyze_survey_overlap.py), not a cleaned historical panel. Its heuristic title matching can accept a shared significant word or two shared words, and does not require artist identity. The sample covers three stations and one winter, with repeated observations of the same markets. It is too small to estimate a general distance law. Station-to-station overlap is also not directly comparable with the simulation's aggregates over seven large regions.

The inherited model result of 82–91% region overlap therefore remains a useful warning, not a directly comparable error of forty percentage points. Before setting a quantitative target, construct canonical recording identities, review ambiguous matches, and add more stations, seasons, and formats. A future panel should compare both station lists and explicitly aggregated markets.

## 6. New measurements from the existing model

These results describe finalized single outcomes. They do not include every still-active release's eventual sales. “Loss” below means `realizedNet < 0` in the existing outcome ledger, **before separately attributed marketing**. It is not the all-in historical measure discussed earlier.

| Measure | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Finalized singles, including opening catalog | 18,746 | 17,369 |
| Finalized singles linked to launches during the run | 18,440 | 17,082 |
| Loss rate, linked new releases | 8.57% | 7.45% |
| Ever Hot 100, all finalized singles | 15.64% | 16.43% |
| Units from never-charted records, all finalized singles | 35.68% | 33.51% |
| Pressed but not sold, all finalized single copies | 3.98% | 3.72% |
| New launches without a final outcome at cutoff | 2,742 / 21,182 | 2,461 / 19,543 |
| Annual single units, 1960 → 1964 | 158.9m → 167.5m | 158.9m → 172.3m |
| Units sold in Hot 100 weeks / all single units, 1960 → 1964 | 55.42% → 49.11% | 57.47% → 53.36% |
| LP share of combined single + LP units, 1960 → 1964 | 17.26% → 29.53% | 17.47% → 26.77% |

About 13% of new launches remain unfinalized. That is material censoring: a final-outcome sample is not automatically representative of the complete release cohort. Future comparisons should include fixed launch-year cohorts with equal follow-up, disclose unfinished outcomes, and separate inherited catalog from in-run launches.

Lifetime “never charted” sales share and weekly “off chart” sales share are different statistics. A future hit's pre-chart sales belong to the latter, not the former. Neither should be compared with a historical statistic using the other definition.

### Non-charting releases by tier at launch

| Tier | Median units, 1001 / 2002 | Loss rate, 1001 / 2002 |
|---|---:|---:|
| Small | 9,020 / 9,178 | 4.65% / 3.46% |
| Boutique | 14,320 / 16,139 | 6.88% / 6.86% |
| Independent | 15,148 / 16,007 | 9.80% / 8.69% |
| MidTier | 20,063 / 21,718 | 20.37% / 21.72% |
| Major | 24,202 / 24,394 | 36.68% / 32.99% |

These medians differ from the handoff's shorter-run examples because the window and matched cohort differ. They reinforce a model-specific finding: Small labels seldom lose on finalized non-charting releases. They do not establish the historical median that should replace 9,000 units.

### Quality is already a very strong discriminator

| Runtime quality at launch | Finalized releases, 1001 / 2002 | Loss rate, 1001 / 2002 | Median units, 1001 / 2002 |
|---|---:|---:|---:|
| ≤ 0.4 | 464 / 398 | 87.28% / 84.17% | 1,905 / 2,271 |
| > 0.4 to 0.5 | 2,460 / 2,200 | 30.28% / 27.36% | 5,687 / 6,119 |
| > 0.5 to 0.6 | 5,843 / 5,714 | 6.23% / 4.97% | 11,914 / 12,678 |
| > 0.6 to 0.7 | 6,664 / 5,985 | 0.96% / 0.80% | 24,620 / 26,189 |
| > 0.7 | 3,009 / 2,785 | 0.07% / 0.11% | 60,373 / 68,028 |

This is descriptive, pooled across tiers, genres, and strategies; it is not a controlled causal estimate. Nevertheless, simply making quality more powerful appears poorly directed. A better historical question is whether good records sometimes receive insufficient exposure, encounter a poor market fit, or carry unwise financial commitments, while careful labels can limit the cost of discovering that.

### Reproduction and limits

The numeric output is saved in `SimLogs/demand-attention-research-2026-10-21/existing-run-diagnostics.json`. For each seed:

1. Filter `release-outcomes.csv` to singles. A release's units are `pressingBookSold`.
2. Construct the charted set from `records.csv` rows with `currentPosition > 0`. Do not use the per-label first-chart file.
3. Join `recordId` to `singleId` in `single-launch-quality.csv`, validating one-to-one matches. Use launch tier and `runtimeQuality` for the stratified tables; the separate `diagQuality` field is not the quantity binned here.
4. Calculate loss frequencies from negative `realizedNet`; calculate unsold fraction as `(sum(unitsPressed) − sum(pressingBookSold)) / sum(unitsPressed)`.
5. For annual chart-week shares, sum `records.csv` single chart units by year and divide by `singleUnits` in `decade-annual-rollup.csv`. Calculate LP share as `albumUnits / (albumUnits + singleUnits)`.

The prior five-year Small closure rates, 31.6% / 28.7%, and approximate genre error of 18 points are inherited from the existing research, not independently remeasured in this pass. LP loss rates likewise remain the earlier report's results. No counterfactual effectiveness is claimed from these diagnostics.

## 7. What the code supports—and what it does not

The handoff correctly identifies broad-based demand generation. [ChartSimulator.cs](../Systems/ChartSimulator.cs) computes each record's regional demand independently, initializes awareness for each release, and gives organic awareness growth individual headroom. Uncharted records inherit bottom-of-chart exposure plus a discovery modifier. These mechanisms can grant extensive reach before a specific outlet or listener has selected the record.

However, remembered awareness is not itself a conserved resource. The problem is unconditional additions to exposure and purchase opportunity. Replacing awareness with a zero-sum pool would solve the wrong problem.

[ChartManager.cs](../Systems/ChartManager.cs) already applies regional stock constraints and format-specific market clearing. Proportional scaling of a fixed demand vector preserves that vector's concentration, although a change to the inputs can change concentration. The existing calibration logs do not establish how often the budget binds. Instrument that before introducing another purchase cap. Preserve the single/LP distinction and stock reconciliation.

[LabelPressingEconomics.cs](../Systems/LabelPressingEconomics.cs) forecasts an initial run, then increments `pressingBookSold` from actual sales and adds repress lots until booked pressings cover those sales. This is an accounting replenishment mechanism, not independently scheduled production with uncertain future demand and lead times. There are separate regional stock constraints, so it would be wrong to say the model has no inventory constraint at all. The proposed audit must establish one coherent relationship between physical availability and this cost book.

The city building blocks exist in [DistanceModel.cs](../Systems/DistanceModel.cs) and radio station metadata. [CityProfile.cs](../Data/CityProfile.cs) explicitly limits its current use to the player-facing cost model. It should not silently become an AI audience-population database. Existing aliases and nearby cities also make overlapping catchments a real double-counting risk.

Regional success already has a financing route through [RegionalHitDealGate.cs](../Systems/RegionalHitDealGate.cs). The player also has master-lease and sale concepts in [PlayerDesk.MasterDeals.cs](../Systems/PlayerDesk.MasterDeals.cs). Extend or reconcile those systems where appropriate; do not build a second disconnected deal economy.

## 8. Proposed implementation, in stages

Everything in this section is a proposal. No classes, switches, or parameters described here were added.

### Stage A: establish economic and historical comparability

Record separate release contribution, attributed marketing, overhead allocation, cash collections, and contract recoupment. Record initial stock, promotional stock, shipments, consumer/operator sell-through, returns, and write-offs distinctly. Resolve whether a sale in each existing ledger means shipment, retail sale, or cash collection.

For physical inventory, the conceptual identity is:

> Available inventory = prior inventory + production received + saleable returns − shipments − promotional withdrawals − destruction.

Transfers between warehouses and distributors do not manufacture new units. A customer return can reverse revenue or create a credit without charging manufacturing twice. Add separate receivables and obligations so that profitable eventual sales can still create a working-capital problem.

Do not tune a target before those definitions reconcile. A simultaneous rise in “losses” caused only by changing the accounting boundary would not demonstrate more historical behavior.

### Stage B: city discovery, regional sales

Keep the seven-region sales and clearing engine. Add a smaller city layer that determines which records get tested, heard, requested, or stocked. Aggregate city effects into each existing parent region using disjoint population/catchment weights, including a residual area for people outside the named cities.

Use two decisions:

1. **Admission:** an outlet hears or tests a candidate. Capacity is limited, and relationships, promotion, local relevance, or evidence elsewhere affect access.
2. **Continuation:** observed response determines whether the record retains exposure and earns replenishment or more outlets.

A single normalized score over every active record is insufficient: it can still grant every release a little demand, while a deterministic “best quality wins” rule gives gatekeepers impossible knowledge. Include bounded opportunities for new candidates and allow records to fail to acquire meaningful exposure. Those opportunities are trials, not guaranteed sales or subsidized hits.

Use existing radio playlists as the authoritative radio allocation. A new city system should consume their exposure signal rather than award the same radio attention twice. Retail and jukebox decisions can be separate channels because their purchasers and incentives differ. Shared listening and local word of mouth may amplify an exposure that actually happened.

Persist awareness as memory. Limit new exposure, allow attention to fade or shift, and prevent repeated hearing from implying unlimited unique buyers. Include a no-purchase outcome. National hits should continue to consume local attention; optimize their shared contribution rather than exempting them from competition.

Quality should raise expected listener response holding other factors fixed. Commercial fit should also vary by record and market, remain persistent enough to be learnable, and have correlated components across markets. Avoid independent random noise every week, arbitrary genre penalties, or access to future chart status. Key any new stochastic state by stable identifiers so it does not perturb the existing global random stream.

### Stage C: let discovery precede stock, and stock precede sales

An unfamiliar city can hear a promotional copy or receive evidence of a success elsewhere before local retail stock exists. That exposure may trigger an order. Sales then require delivered stock. This breaks the existing arrival deadlock without inventing purchases in a market with no inventory.

Diffusion should follow station relationships, distributors, format compatibility, and local response as well as distance. A useful local record can travel to a compatible distant market while nearby outlets decline it. Preserve genre acceptance inside the existing airplay exponent; changing it would introduce a different, already known calibration problem.

### Stage D: make pressing a decision made before the outcome

Commit an initial batch before the relevant demand is observed. Choose quantities from the label's information, access, cash, and past forecast errors. Reorders use evidence available at the decision date and arrive after a lead time. Promotional copies and returns have explicit destinations and contract treatment.

The key change is the information boundary, not an arbitrary increase in costs. A cautious small label should be able to lose a limited investment on a failed test. A large campaign can lose substantially more through excess commitments. A successful local test can fund or justify a second pressing, subject to credit and delivery time.

Do not assume a 500-copy minimum or a fixed returns percentage is universal. Use the documented small-run example and larger-run cost quotation as different scenario checks. Fit representative behavior only when better evidence exists.

### Stage E: preserve a credible small-label portfolio

Measure the economics of a regional success independently of national chart admission. A 10,000–40,000-copy local-success scenario should be possible when territory, response, and distribution justify it; that range is an illustrative case envelope, not a required rate or median.

Retain existing distribution-finance gates, but assess them against economically meaningful local evidence and current cash commitments. Distinguish P&D finance from a master lease or outright sale. Advances come from counterparties and have repayment or rights consequences; they are not free survival grants. A reissued or licensed master needs one sales identity so its units are not counted twice.

As an illustration, nine releases losing $300 each and one earning $6,000 leave $3,300 before overhead. A 90% release-loss rate can coexist with label survival; the timing of the hit and bills determines whether that label actually survives. These numbers are illustrative, not historical calibration. The relevant design problem is the joint distribution of losses, wins, costs, and timing—not the loss percentage alone.

A fair system should reward sound decisions in expectation. It should not guarantee that every good record succeeds or that every insolvency can be attributed to poor music. Historical uncertainty and working-capital exposure are part of the proposed economics, with the accepted closure band acting as a playability constraint.

### Engineering scope and performance

| Existing area | Proposed responsibility |
|---|---|
| `ChartSimulator.UpdateAwareness` and regional demand | Consume earned exposure and persistent response; retain controlled purchase conversion |
| `Systems/Radio/StationNetwork.cs` | Supply actual station exposure and candidate-test evidence |
| City metadata and a proposed discovery-state component | Represent catchments, channel opportunities, local adoption, and sparse active record–city state |
| `ChartManager.CalculateLiveRegionalSalesWithMarketClearing` | Aggregate city influence into existing regional demand; retain format budgets, stock caps, and national reconciliation |
| `LabelPressingEconomics` and existing inventory/cash systems | Reconcile booked manufacturing with prospective orders, deliveries, returns, and settlement |
| Regional deal gate, wholesale finance, master deals | Convert demonstrated demand into contractual opportunities with counterparty accounting |
| Diagnostics | Expose why a release was unheard, rejected, unavailable, sold, or financially unsuccessful |

Avoid recomputing the complete demand model for every record in all 31 cities. A plausible design is the existing seven-region calculation plus sparse city adoption updates, bounded trial candidates, and a cached contribution from widely established records. Measure the candidate-maintenance cost too: a supposedly sparse design can still scan the entire catalog while selecting candidates.

Prune dormant state carefully, retain reproducibility across save/load, and version new saved data. A national record remains a local competitor even if its exposure can be calculated once and distributed. Performance claims require profiling; no speedup or fixed overhead is asserted here.

## 9. Proposed experiments and decision criteria

First resolve the measurement boundary and missing market-clearing telemetry. Then screen a small component comparison: unchanged control, discovery only, prospective inventory only, and the combination. Each arm should be opt-in and independently interpretable. These are future runs, not work performed in this pass.

Use 105 weeks on seeds 1001 and 2002 to detect broken accounting, chart entry, genre composition, and pathological cash behavior. That horizon cannot establish five-year survival or complete lifetime outcomes. Advance only promising arms to 265 weeks, with comparable early launch cohorts. Do not change defaults or launch a decade calibration merely because a short run reaches a desired loss percentage.

Required reporting for every future arm:

- Release loss by tier, genre, quality, and launch cohort, with contribution and all-in definitions side by side; unfinished outcomes explicitly counted.
- Sales distribution including zero, under 500, under 1,000, and below release-specific break-even; initial pressing, promo allocation, reorder, return, and write-off distributions.
- First exposure, first adoption, first order, first delivered stock, and first sale, so failed discovery and failed fulfillment are distinguishable.
- Weekly off-chart share and lifetime never-charted share separately; national and regional reconciliation; clearing pressure and unmet demand by format.
- Distinct chart acts, debut positions, chart lives, #1 tenure, major-family chart share, genre shares, year-end slots, annual single units, and LP unit share.
- Starting-Small closure, cash troughs, debt, concentration of profits in successful releases, and the role of local hits and deals.
- Regional overlap with clearly matched aggregation units, and runtime/memory/log-volume costs.

The disabled feature must preserve the existing finance, record, and release-strategy streams exactly. New state and random draws must be inert when off. Prefer aggregated telemetry and a reproducible sample of record histories to emitting every record–city–week row.

**Advance a proposal when** it reconciles physical units and money, creates economically understandable failures and local successes, preserves access for emerging records, and stays within the accepted broader balance. **Reject or revise it when** it reaches a loss target by starving all new releases, granting hidden subsidies, forcing demand into chart incumbents, changing genre treatment, or counting the same loss twice.

## 10. What remains uncertain, and how to research it

| Question | Current confidence | Evidence needed next |
|---|---|---|
| Representative 1960s single and LP release-loss rates | Low; no numerical target established | Release-level label/distributor accounts including unsuccessful issues, explicit cost definitions, and follow-up horizons |
| Sales at Hot 100 ranks and the chart's share of all units | Low; current anchors unverified | Dated unit-sales reports matched to chart weeks and physical release identities; distinguish chart sides from double-sided discs |
| Median non-charting sales and first pressing by label type | Low; examples only | Complete catalog samples linked to pressing orders, reorders, invoices, and returns—not selected hit recollections |
| Annual single units | Moderate for what the period tables print; low comparability between vintages | Reconcile the large 1966 revision and market coverage; establish 1960 and 1969 using the same definitions |
| Returns privileges and manufacturing scale differences | Stronger evidence for existence than incidence | Contracts plus realized shipments, credits, returns, and manufacturing invoices by format and year |
| Regional discovery and substantial local successes | Moderate, supported by named cases and surveys | Broader matched station panels and comparable unsuccessful releases |
| Store, jukebox, and listener attention capacities | Partial evidence; no universal constants | Outlet surveys, actual catalogs/playlists, operator purchasing records, and period US household purchasing studies |
| Label closure and sustainable portfolios | No representative mortality target | Cohort directories with entry/exit dates, mergers and inactivity distinguished from insolvency |

For a manageable next archival effort, sample early, middle, and late decade windows and several business types: a local label, a distributor-backed independent, and a major. Retain non-pop specialty records and catalog in the market accounting even when the principal chart is the Hot 100. Separate domestic units from exports, free samples, and intra-channel transfers.

The central unresolved question is not simply how much to cut off-chart demand. It is **which releases acquired a real opportunity to sell, what they cost before that opportunity was known, and how the occasional success financed the next attempt**. That is the historical structure the proposed model should represent.

## Evidence retained

- `SimLogs/demand-attention-research-2026-10-21/existing-run-diagnostics.json`: measurements from the two existing runs.
- `SimLogs/demand-attention-research-2026-10-21/input-manifest.json`: hashes of the principal input CSVs and source-checkout identifier.
- `SimLogs/demand-attention-research-2026-10-21/carroll.pdf`, `carroll.txt`, and `carroll-p14.png`: downloaded paper, extracted text, and visually inspected curve page.
- Existing project research and source caches were read without alteration. The evidence directory is gitignored; this report is the reviewable research deliverable.

Source-access limitations are stated next to the affected claims. Unretrieved scans, incomplete historical coverage, and retrospective case accounts have not been promoted into verified population parameters.
