# Small-label attrition, chart method and regional timing — 2026-10-16

**Share follow-up:** [MajorFamilyShareInvestigation.md](MajorFamilyShareInvestigation.md) compares the original pre-2-p.m.
and pre-arrival compositions with the adopted run. It separates opening-year MidTier sales, tier reclassification and
Independent failures; the +7.9-point 1962 old-home comparison is predominantly a reclassification movement.

**Latest result (§12):** corrected period pressing costs plus home-only Small territory produce **24.8%** five-year
starting-Small closures (seed 2002), against **44.3%** with the old costs. The four agreed switches, paired period costs
and home-only territory now default on. Historical off-by-default descriptions below refer to the older assemblies.

Follow-up to `RegionalSurvivalAndChartFollowup.md`. All observations are read from existing logs
(`SimLogs/economic-holdout-new-2002-260-*`, the seed-2002 five-year economic-policy run) and from
cached radio surveys. No simulation was run and no game code was changed.

## 1. Why only 6.1% of starting Small labels close in five years

The economic exit rule is not too lenient. A label fails when cash stays negative across two monthly
reviews, and Small overhead is a real fixed cost of $150 + $30/artist ([AILabel.cs:791](../Data/AILabel.cs)).
The problem is that **almost nothing a Small label releases loses money**, so cash only accumulates:

| Starting Small cohort (n=230) | 1960 | 1962 | 1964 |
|---|---:|---:|---:|
| Median cash (week 1: $4,806) | $27,561 | $121,808 | $264,741 |
| Median cash ÷ monthly overhead (week 1: 21) | 118 | 383 | 766 |
| Survivors with positive recent cash flow | 194/222 | 204/218 | 210/216 |

Per release, for singles from labels in the starting Small cohort:

- Median lifetime units are **10,493**. Even p10 is 2,677, and only 3.2% of in-run records sell under 1,000.
- **95% never chart.** The median release is a non-charting single that still sells ten thousand copies.
- Label net is **$0.44 per unit** (median), so the median single clears $4,095 after its ~$360 production cost.
- **Only 1.7% of 4,283 retired singles failed to recover production cost** (`realizedNet ≤ 0`). That
  figure excludes marketing and overhead, but those are small next to a $4k contribution.

Across the cohort over five years, label net is **54% of booked retail gross**. The distribution skim
is only 6.1% of gross after manufacturing.

### The two mechanisms Codex flagged are both real

1. **AI singles book near retail.** [CompetitorManager.cs:1044](../Systems/CompetitorManager.cs) books
   `GetPricePerUnit` = $0.89 for every AI unit. The player's singles book `PlayerDesk.DealerPrice` = $0.63.
   The project's own ladder (`PlayerDesk.cs` ~5089) puts what a distributor nets on a dealer-price unit
   at ~$0.41–0.50. So an AI label is booking the dealer's and distributor's margin as its own revenue,
   and then paying a skim of only ~6% on top.
2. **Manufacturing follows copies sold.** `CalculateRegionalCogs` charges pressing on
   `unitsSoldThisWeek`, with no pressing run, no unsold stock and no returns. The main period risk
   (pressing and shipping, then eating returns 90 days later) cannot occur.

### Static sensitivity (no behavioural response)

`analyze_small_label_cash_counterfactual.py` replays each starting-Small label's weekly cash path with
a different per-unit price and with pressing charged on shipped copies (`sold / (1 − returns)`). The
royalty stays on list price and the per-week skim rate is kept. "Fail" means five or more consecutive
negative weeks while still active.

| Label price per single | Returns | Static 5-yr failure | Median end cash |
|---:|---:|---:|---:|
| $0.89 (current) | 0% | 1.3% | $230k |
| $0.63 (dealer) | 0% | 18.7% | $78k |
| $0.45 (via distributor) | 0% | 90.9% | −$19k |
| $0.89 | 30% | 3.9% | $165k |
| $0.63 | 30% | 65.7% | $11k |
| $0.45 | 30% | 96.5% | −$86k |

The response is steep. The price lever is roughly ten times stronger than the returns lever, and the
two compound. These are upper-bound-ish readings: a live run would let `CommitmentReserve` cut new
releases as cash tightens, so failures would land lower and later. Realistic prices *and* realistic
returns together would wipe out the sector at today's unit volumes. That says the per-release unit
distribution (a 10.5k-copy median non-charting single) is part of the same miscalibration. Prices
cannot be corrected without revisiting volume.

### Global ripple — do not change the price in isolation

`pricePerUnitByFormat` feeds every AI tier. Format choice is margin-driven (memory:
album-creation-is-margin-driven), so cutting the Single price while leaving the Album's $3.98 would
shift the format mix toward albums. Any change needs the Album price treated the same way (dealer /
distributor price for the LP), and a decade A/B against market share, Major share and the format mix.

## 2. Target band for five-year Small-label closure

No representative closure rate for 1960s US labels was found, by Codex or in this pass. The closest
defensible anchors are general business demography:

- Modern US *new* establishments: ~49–51% survive five years, and that includes voluntary closures and
  sales ([BLS-based tabulation](https://startbusinessbystate.com/business-survival-rates-by-state);
  older [BLS research](https://www.bls.gov/osmr/research-papers/2006/pdf/st060040.pdf): 44% survive
  four years).
- The starting Small cohort is mixed-age, not newborn, which lowers the hazard. A hit-driven trade with
  90-day terms, returns and unreliable distributor payment raises it.

**Proposed band: 20–35% of starting Small labels close for financial reasons within five years,
front-loaded into years 1–2.** Acquisitions and promotions out of Small are counted separately as
non-failures. Discontinued imprints and dormancy remain unmodeled and are not folded in. The old
63.9% exceeds even total new-business churn and is too high. 6.1% is below any business-demography
analogue and is too low. Treat this band as a design judgment, not a measured historical rate. It
should be reached through prices, pressing risk and volume, never through a mortality quota.

## 3. Chart method

**Verified:** from the chart dated **May 11, 1968**, Billboard tabulated the **top 50 of the Hot 100 on
sales alone**. Positions 51–100 kept sales plus airplay, and the whole chart returned to combined
sales + airplay the week of **June 9, 1973**
([Billboard Chart Rewind](https://billboardchartrewind.wordpress.com/2020/05/14/its-all-about-the-sales/);
also noted in [this analysis](https://pappubahry.substack.com/p/statistical-oddities-in-the-billboard)).
These are secondary sources; a Billboard issue scan would make this primary.

The model does the opposite. `GetAirplayEraWeight` ([ChartSimulator.cs:1327](../Systems/ChartSimulator.cs))
ramps airplay from 0.60 in 1960 to its **maximum 1.00 in 1968**, applied to all 100 positions.

**How much airplay already decides the top** (five-year run, `1 − units / unsampled points`):

| Year | #1 | #1–10 | #11–40 | All top 40 | Median record-week |
|---|---:|---:|---:|---:|---:|
| 1960 | 87% | 71% | 21% | 55% | 15% |
| 1962 | 85% | 69% | 22% | 54% | 14% |
| 1964 | 90% | 73% | 22% | 57% | 12% |

The code comment at [ChartSimulator.cs:143](../Systems/ChartSimulator.cs) says the coefficient was tuned
so that airplay was ~36% of a number one's points at 1960. It is now 87%, so the top of the chart is an
airplay chart. The ~10–15% "airplay share" guard used in the regional work is a median over all top-40
record-weeks, where the long sales-only tail hides this. Airplay is also front-loaded: 59% of points in
release weeks 1–4, falling to 2% at week 13+.

Codex also reports that the "110 outlets: 63 stations, 25 one-stops, 22 retailers" scheme cited in the
survey code describes the post-1973 system. I have not independently verified that date. If it holds,
the survey-panel comment needs re-sourcing, but the noise mechanism itself can stay.

**Implications:**

1. For 1968-05-11 onward, rank positions 1–50 by sales points and positions 51–100 by combined points.
   Put this behind a switch. Because rank feeds exposure, the awareness floors, shelf bonus and the
   rack channel, this changes 1968–69 sales and genre slots, not just labels. It needs a two-seed
   decade A/B (memory: airplay-acceptance-is-load-bearing; airplay is the only non-units chart channel).
2. The 0.60 → 1.00 ramp has no source behind its direction. Before re-tuning it, find out why airplay
   now carries 87% of #1 rather than the designed 36%. Candidates since that calibration are regional
   radio, local heat and the reporter panel. The `--regional-radio-build-clock` result already showed
   airplay is sensitive here.

## 4. Regional timing and overlap: the miss is *what* sells, not *when*

Historical sample: WLS (Chicago), KRLA (Los Angeles) and KDWB (Minneapolis), 65 surveys,
Oct 1963–Feb 1964 (`research_regional_chart_examples.py`, Oldiesloon transcriptions).

**Peak timing** for the eight selected hits, as the spread of first peak dates across the three stations:

| Record | Spread | Peak ranks WLS / KRLA / KDWB |
|---|---:|---|
| Louie Louie | 0 wk | 2 / 1 / 2 |
| Sugar Shack | 1 | 1 / 1 / 1 |
| Wonderful Summer | 3 | 1 / 19 / 12 |
| I Want to Hold Your Hand | 3 | 1 / 1 / 1 |
| There! I've Said It Again | 3 | 1 / 5 / 4 |
| Drip Drop | 3 | 3 / 17 / 5 |
| Surfin' Bird | 5 | 3 / 12 / 1 |
| Dominique | 6 | 1 / 5 / 3 |

Median spread is **3 weeks**, and **0 of 8 records peak 8+ weeks apart**. The model's settled median is
2 weeks with 3–4% at ≥8 weeks, so timing is already close. The handoff's 5–15% at ≥8 weeks target
has no support in this sample; given 8–10-week chart lives, 0–5% looks right.

**Top-11 overlap**, same week, pairwise, measured with `analyze_survey_overlap.py` over 59 station pairs.
Model regions are those whose hub city is the station's city (`DistanceModel.cs`).

| Station pair (model region pair) | Historical median | Historical mean | Model mean |
|---|---:|---:|---:|
| WLS – KDWB (Great Lakes – Great Plains) | 50% | 50% | 89.7% |
| KRLA – KDWB (West Coast – Great Plains) | 36% | 40% | 89.4% |
| WLS – KRLA (Great Lakes – West Coast) | 27% | 29% | 90.2% |
| **All** | **45%** | **40%** | **88.6%** (median 91%) |

*Correction, 2026-10-16:* the first version of this table read 36% median / 31% mean. That was
understated by a parse bug. KRLA's 1964 surveys are table pages that parse as `lastWeek | TITLE | artist`,
and the script keyed each row on its first word, which was the last-week rank number. So no 1964 KRLA row
could ever match. The script now drops numeric (and `TIE`) columns, matches on the first four significant title
words, and normalizes four transcription variants. Spot checks of the remaining misses show genuinely
different records. For example, the WLS (Nov 22) and KRLA (Nov 23) lists share only Sugar Shack and Deep Purple.

Model figures come from `economic-holdout-new-2002-260`: all five years, all records, regional `finalCleared`,
all 21 pairs of the 7 regions (`analyze_regional_sync.py`, now with `top11OverlapMedian` and `top11OverlapByPair`).
Every model pair falls in **85.8–91.3%**. The lowest pairs involve the Rockies, the smallest region, so the
low end is probably small-sample noise rather than geography. The model has **no distance gradient**:
Chicago–LA is no lower than Chicago–Minneapolis. Historically the coast-to-coast pair shares about half
as much as the Midwest pair. A singles-only cut, restricted to retired records and to Oct–Feb weeks for the
three matched pairs, reads the same: 82% mean, 91% median.

The handoff target of 70–85% is itself far above this sample. Local lists in 1963 shared well under half of
their top 11. Peak *ranks* diverge just as much (Wonderful Summer #1 in Chicago, #19 in LA).

Caveats: station surveys mix sales, requests and playlist policy, which lowers overlap compared with
regional sales. These are three city lists, while the model's regions aggregate several markets, which
raises overlap. The sample is one winter. Even so, the gap from 40–45% to 89–91% is too large for those
caveats to close. The missing gradient is a separate miss, and the caveats cannot explain it.

**Implication:** the regional model already staggers *when* the same hits arrive. It does not let
regions prefer *different* records. That points at the cross-region common terms named in
`RegionalSyncDesign.md`: the 0.4 national awareness blend, national radioHeat and national rank
exposure. It also points at local repertoire, meaning local acts on local labels selling mostly at
home. That connects back to §1: the Small sector is meant to be regional, but it sells 10k-copy
non-charting singles everywhere. Before re-targeting, widen the sample (WABC/WMCA, KHJ, a Southern
station; a summer window). A provisional band is **45–65% region-level overlap**, sitting above the 40–45%
station figure to allow for aggregation. Adjacent regions should overlap visibly more than coast-to-coast
pairs. Peak timing stays at a **median spread of 2–4 weeks, with 0–5% at ≥8 weeks**. A regional model that
lets areas prefer different records also needs the divergence to grow with distance.

## 5. Implemented: dealer price + pressing book (`--dealer-price-pressing`)

`Systems/LabelPressingEconomics.cs`. Off by default; every caller gates on it. Requires nothing else, but was
measured on the regional + economic-exit configuration below.

- **Price.** AI Singles book `PlayerDesk.DealerPrice` ($0.63) instead of $0.89. Albums (and EPs) book the same
  dealer share of list (0.63/0.98, so an LP books $2.56 instead of $3.98). The existing skim stays the
  distributor's cut. Artist royalty and the publishing pool stay on the old list basis.
- **Pressing book.** This is kept separate from shelf stock. Stores hold a median 25k copies of a Small single
  that sells 10k: that stock is a demand device and cannot double as a pressing run. When a record first
  ships, the label presses an initial run of `max(500, 0.5 × forecast)`. The forecast is a geometric memory
  (α 0.3) of the label's own lifetime copies per retired release, per format. Represses go in lots of
  `max(500, 3 weeks of current sales)` whenever sales catch up. The plant bill is paid in cash at once, never
  deferred with wholesale billings. The initial run joins sunk production cost; represses come out of the
  record's net. Copies pressed but never sold are the returns loss.
- **Decisions.** A release must keep its expected initial run in reserve, as well as production and
  marketing (orphan singles; album projects reserve both the promo and the LP run). The single prior's
  contribution table is rescaled by the new/old per-unit margin (~0.52). The album prior re-prices itself at
  the dealer price.
- **Telemetry.** New appended columns: `pressingCost` (settlement), `weeklyPressing` (label-finance),
  `unitsPressed` and `pressingBookSold` (release-outcomes). New record fields are saved; label fields ride the
  whole-object world save.
- Assumptions with no measured period source: the 500-copy plant minimum, the 0.5 initial-run share, and the
  three-week repress lot.

**Validation.** `--label-pressing-check` and `--economic-label-check` pass. A 26-week switch-off replay of seed
2002 is **byte-identical** to `economic-holdout-new-2002-260` on every preexisting column of records,
label-finance, both settlement files and release-outcomes. It was run twice, before and after the reservation
change. So Codex's economic-policy runs are the matched controls. No engine errors occurred, and all runs
printed `CHART_AUDIT_COMPLETE`.

### Results (control = economic exits, switch off; treatment = switch on)

Starting-Small cohort closed (Bankrupt/Defunct), cumulative:

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| Control 2002 | 3.5% | 5.2% | 5.2% | 5.7% | **6.1%** |
| Treatment 2002 | 13.5% | 21.7% | 30.4% | 35.7% | **38.7%** |
| Control 1001 / 3003 | 7.4% / 7.8% | 10.8% / 12.9% | | | |
| Treatment 1001 / 3003 | 14.7% / 18.1% | **27.7% / 34.5%** | | | |

Other starting tiers, treatment vs control: Independent 8.1% vs 0% after five years (6.0–7.2% at two
years), Boutique 5.5% vs 0%. Majors and MidTier: none. New entrants rise from 72 to 190 over five years, and
55 of them close. Births respond to the vacancies, so the market churns rather than shrinking.

Seed 2002, five years:

| | Control | Treatment |
|---|---:|---:|
| Starting-Small survivors still Small / promoted | 105 / 111 | 72 / 69 |
| Survivor median cash, year 5 | $264,741 | $68,269 |
| Starting-Small retired singles losing money (`realizedNet ≤ 0`) | 1.7% | 15.1% |
| Median realized net per single / per album | $4,095 / $21,664 | $1,512 / $8,850 |
| Unsold share of pressed copies (all retired records) | n/a | 3.8% |

Returns overhang is small: about 4% of pressed copies go unsold. **Nearly all of the effect comes from the
price.** The pressing book matters for timing: plant bills land up front while wholesale receipts arrive
about 90 days later. Analysis of the live runs shows two closure populations. One is tiny shells that started
on ~$1.5k, sold ~2k copies in their life and bled overhead. The other is growing Independents and Boutiques
that were profitable on the books but out of cash, with 12-week pressing of ~$26k against $17k sitting in
receivables. Budgeting the initial run barely changed early closures (32/35/47 vs 32/36/44 by Oct 1960).

**Guards.** Units are flat: −1.0% to −0.1% a year, all formats. Album unit share is −1.8 / −1.8 / −1.1 /
−1.2 / −0.4 pts. Small-tier unit share is 7.7 / 5.8 / 4.1 / 3.3 / 2.8% vs 7.6 / 8.4 / 5.1 / 4.2 / 3.3%.
**Major-family chart share** moves +11.4 / +4.2 / +2.9 / −6.1 / −5.6 pts (seed 2002), and +5.9 / −2.1
(1001) and +6.1 / +6.8 (3003). It breaches the ±3-pt guard in most years, and is worst in 1960. Number ones,
genre gap and airplay share stay within seed noise.

**Reading.** The mechanism moves five-year Small attrition from 6% to 39%, a little above the proposed
20–35% band. The shape is right: front-loaded and decelerating, a few Independents fail, and no Major or
MidTier fails. It does this without a quota or a chart rule. The year-1 spike in both closures and Major chart
share points at the 1960 seed: starting cash, rosters and advances were sized for the old 54%-of-gross
margin, and the world meets the new prices all at once in January 1960. Before any re-tuning, decide whether
that opening shock is acceptable or should be absorbed, for example by re-seeding opening cash at the new
margins or ramping the switch in. Do not lower the price to reach a target number.

Runs: `SimLogs/pressing2-{2002-260,1001-104,3003-104}` and `pressing2-off-2002-26`, with
`*-vs-control.json` and `*-guards.txt`; assembly hash in `pressing2-runs-assembly.txt`. The `pressing-*` runs
(no reservation) were stopped at ~week 43 and are kept only for the early-closure comparison.

### Opening shock, first pass: catalogue pre-pressed (2026-10-16)

There were two suspected seeding artifacts. One was fixed and the other turned out not to exist.

- **Week-one catalogue pressing (fixed).** Catalogue records skipped the initial run but still pressed a full repress
  lot (three weeks of sales) when their book opened in week one. That is **1.32–1.38× week-one gross** for Majors,
  MidTiers, Indies and Boutiques, against ~0.45 at steady state. A record released on or before 1960-01-01 now opens with
  that week's copies already pressed, plus part of a pre-1960 lot. Its position in the lot cycle is a stable hash of
  the record id (FNV-1a + fmix64; no random draws), so the first represses spread out. Live records that sell
  late keep the old behaviour. `--label-pressing-check` covers both cases.
- **Q4-1959 receivables (not applicable).** Wholesale deferral applies only in a label's independent-distribution
  regions, and **no label has one before week 5**. The count goes from 0 to 89 labels at week 8, 255 at week 13 and 390 at week 52. No 1959
  wholesale billing exists to carry in. The receivables ramp in 1960 is the cash cost of joining a house during the
  1960 land rush, and it is the same in the control. Seeding it would mean inventing pre-1960 wholesale relationships,
  which is a design decision, not a seeding fix.

Seed 2002, first 52 weeks. Control is `economic-holdout-new-2002-260`, then `pressing2-2002-260`, then `pressing3-2002-52`
(this fix, otherwise the same assembly; hash in `pressing3-runs-assembly.txt`).

| 1960 | Control | Pressing2 | Pressing3 |
|---|---:|---:|---:|
| Starting Small closed by wk 13 / 26 / 39 / 52 | 0 / 0.9 / 2.6 / 3.5% | 0.4 / 3.0 / 8.7 / **13.5%** | 0.9 / 3.9 / 7.8 / **10.9%** |
| Starting Independent closed, wk 52 | 0% | 3.6% | 3.6% |
| Week-one pressing ÷ gross (non-Small tiers) | — | 1.32–1.38 | 0.01–0.04 |
| Major-family chart share | 50.5% | 61.9% (+11.4) | 58.6% (**+8.1**) |
| C4 | 25.4% | 32.5% | 27.1% |
| Starting-Small median cash, wk 52 | $24,825 | $6,307 | $5,963 |

**Reading.** The week-one bill was real and is gone. It accounts for most of the C4 jump, about 30% of the
Major-share excess and a fifth of the 1960 Small closures. This is one seed: the 1960 Major-share excess already ran
+5.9 to +11.4 across the three pressing2 seeds, so the 3.3-point drop is inside seed spread. Most of the "opening
shock" is not a seeding artifact:

- **Small closures are not front-loaded artifacts.** They land in weeks 13–52, long after week one. The new year-1
  rate (10.9%) is only modestly above pressing2's years 2–3 (+8.2, +8.7 pts), which is the front-loading the
  proposed band asks for. Starting-Small labels hold almost no seeded catalogue (week-one gross is $1.5k across all
  230), so all their 1960 pressing is initial runs on new releases against ramping sales: 0.79× gross in weeks 2–6.
  This is the same in both runs. It is how this sector starts, not a bill carried in from 1959.
- **Major share is still +8 pts in 1960**, which breaches the ±3-pt guard. See below.

#### What carries the remaining +8 pts (seed 2002, control vs pressing3)

`majorFamilyChartShare` is a units share: chart-week units over the year, rolled up to each record's year-end owner,
with Major and MidTier counted together. Chart *entries* hardly move (owner Major-family 304 → 299). The gain is
in units per charted record, and it belongs to **MidTier, not the Majors**:

| 1960 chart-unit share | Control | Pressing3 |
|---|---:|---:|
| Major | 32.6% | 31.7% |
| MidTier | 18.0% | **26.9%** |
| Independent / Boutique / Small | 32.3 / 14.9 / 2.3% | 28.4 / 12.2 / 0.7% |

- **Not a few hits.** MidTier records outside its top five go from 14.0% to 21.6% of chart units; its top five add only 1.3 pts.
- **Not launch inputs or exposure.** MidTier releases fall (482 → 451). Release quality (0.613 → 0.619), launch stock,
  launch awareness, radioHeat (+1.6%) and awareness (+0.6%) are unchanged. Yet weekly units in weeks 1–10 rise 42%.
- **It is demand per record.** MidTier raw demand rises 40.8M → 52.9M while every other tier's falls (Small −24%,
  Independent −11%). Conversion rises 19%, through the intrinsic quality factor (0.165 → 0.178), and is concentrated in
  orphan singles and TeenPop (9.3M → 17.9M).
- **Not talent acquisition.** MidTier signs fewer first-time acts (68 → 39). 51.0M of its 52.9M demand comes from artists
  who were MidTier in the control too. Those same artists' demand rises 41%, and broadly: 111 gain, 84 lose.

Reading: with launch inputs and exposure unchanged, this looks mostly like per-record draws (quality, and titles
feeding hook/production) reshuffled by the cash change, on top of a real but smaller effect. Indie-family releases
fall ~10% under the switch, which leaves fewer competitors for the same demand. One seed cannot separate the two. The
pressing2 excess was positive on all three seeds (+11.4 / +5.9 / +6.1), so some part is systematic. The tier
shares also swing hard between two runs that differ only in the catalogue fix (pressing2 → pressing3: Major −5.6 pts,
MidTier +2.3), so ±5 pts per tier per run is the noise scale here. Resolving it needs either seeds or a
common-random-numbers design that keeps record draws fixed across arms.

## 6. Survey: how AI labels are generated (2026-10-16)

This section asks whether game-like seeding drives the effects above. Code: `AILabelFactory` (the 1960 world: named
templates plus a procedural tier draw); `LabelGenerator.GenerateSingleLabel`, then `RuntimeLabelProfileFactory` (runtime
births, Small or Independent only); `RosterManager.InitialSignArtist`; `CompetitorManager.PopulateInitialRecords`;
`AILabel.GetMonthlyOverhead`; `ChartSimulator.CalculateInitialRegionalStock`.

| Item | What the code does | Assessment |
|---|---|---|
| **Distribution coverage ramp** | The factory gives a Small label its home region plus 0–2 more. In 1960, **64% of starting Small labels sign a Major/MidTier distribution deal** (149 deals, median reachGranted 0.22), and most also join a wholesale house (median one region by week 52). Median coverage of a new Small release climbs from **2 regions in Q1 to 5 in Q4** (Boutique 3 → 6, Independent 3 → 6). Non-charting Small singles are covered in a median 4 of 7 regions and sell **63.5% of copies outside the home region** (Majors 71.6%). | **The strongest gamification found.** Within a year a storefront label has near-national distribution. A two-thirds-a-year rate of micro labels landing a Major distribution deal is far above any period norm; distributed indies were a select group. This feeds the 10k-copy median Small single (§1) and the 87–91% regional overlap (§4). |
| Out-of-territory stock | An uncovered region still gets shelf stock at `access 0.18` (~500–600 copies). Under `--regional-arrival` its demand waits for arrival. | Minor. At most ~2.7% of Small units sell beyond the record's covered-region count (a lower bound that assumes the best-selling regions are the covered ones); spillover is ~9% for every tier. |
| Starting cash | Flat uniform draw by tier: Small $1k–8k, Boutique $3k–15k, Independent $5k–20k, MidTier $15k–60k, Major $50k–200k. Nothing ties it to roster, catalogue or age. | Small runs 4–35 months of its ~$230 overhead. A 1960 one-man label could start on a few hundred dollars, so the top of the Small range is generous. It is not the cause of "never lose money": the control's Small median grew $4.8k → $265k whatever it started with. Under the switch the low end is what dies (the ~$1.5k shells), so lowering cash raises closures without fixing the margin. |
| Opening catalogue | Initial records are quota'd by tier weight × roster (Small weight 1), and prewarm drops records with no chart and under 500 units. | Starting Small labels have almost nothing on sale (week-one gross $1.5k across all 230), so for them 1960 is a cold start. This is defensible for micro labels, but it means every Small label begins at once. |
| Tier mix | 600 labels at start: 230 Small, 197 Independent, 146 Boutique, 20 MidTier, 7 Major. The procedural draw is 0.2% Major / 3.3% MidTier / 33% Independent / 43% Small / 20.5% Boutique. | Independent is nearly as common as Small. The factory's own comment says 1960 had ~20–25 *national* independents, and an "Independent" here has 1–3 extra distribution regions. Micro labels should far outnumber regional independents. Design call. |
| Capability envelopes | Small scouting 0.34–0.84 and production 0.28–0.80 overlap the Major range (0.68–0.96 / 0.72–0.98) by intent ("a focused small label can make a great record"). | Measured quality factor: Small 0.159 vs Major 0.194. Every tier faces the same `potentialAudience` per region, so a Small single differs from a Major's mainly in awareness and stock. Intended, but combined with the out-of-territory stock it lets Small output look national. |
| Seeded fame | `InitialSignArtist` is tier-blind: ~12% of seeded acts start Rising with 1–3 top-40 hits and ~4.5% Established. The star canopy is Major/MidTier only. | Inert for Small: 97.5% of 1960 Small launches are NewSigning, 2.5% Established, 0 Rising. |
| Overhead / costs | Tier constants: Small $150 + $30 per artist a month, production $350, marketing $150 × career multiplier × marketingPower. | Plausible as fixed costs. Not a lever on the issues above. |
| Runtime births | Small or Independent only. Cash from `LabelGenerator` (the same Small/Independent ranges); stats from `RuntimeLabelProfileFactory`. 12% of Independent births are "dependent hitmakers" with cash capped at two months of overhead. | Fine. Latent trap: `LabelGenerator.InitializeFinancials` also has *inverted* Major ($5k–15k) and MidTier ($500–2,000) rows. They are unreachable today because births are never those tiers, but would misfire if that changed. |

**Implication.** Price and pressing (§5) fixed whether a Small release can lose money. What remains is how many copies a Small
release sells, and where. Easy distribution access, mostly Major/MidTier deals plus wholesale houses, is what turns
a micro label into a near-national one within a year. It is also the lever shared with the regional-overlap miss.
Next: read the deal-seeking and acceptance gates for Small clients, find which condition makes 64% a year
pass, and measure a tighter gate against Small per-release units, Small closure, the home-region share and
`top11OverlapByPair`. Memory notes that cheap independent reach already invalidated the MidTier promotion gate, so
the same ease of access may be the common cause.

## 7. Why 64% of starting Small labels sign a P&D deal in 1960 (2026-10-16)

Read from `economic-holdout-new-2002-260` (`-distribution-offer-attempts.csv`, `-deal-ledger.csv`,
`-breakout-funnel.csv`). No code was changed for this section.

*Correction to §6:* these are not mostly Major/MidTier deals. Of the 152 deals that starting-Small labels sign in
1960, **103 are with Independent-tier labels**: an Independent with `ownedReach ≥ 0.65` qualifies as a distributor in
`IsEligibleDistributor`. 43 are with MidTiers and **6 with Majors**. All 152 come through the pull route
(`LabelSought`). The median deal grants reach 0.21 with a $205 advance; 33% carry no advance. In total, 396 P&D
deals are signed across all tiers in 1960.

### The funnel (monthly, `TryGenerateDistributionOffer`)

| Gate | Condition | Starting-Small pass rate, 1960 |
|---|---|---:|
| Evidence | any record's `peakBreakoutScore ≥ 0.20` in **any** region (`IsPullDealTrigger`) | **205 of 230 labels (89%)**, median first week 13 |
| Pull chance | `monthlyPullOfferProbability` 0.40, rolled again **every month** | 39.8% of evidence-months |
| Distributor | `SelectDistributor`: capacity, cash and an uncovered region | 54 of 313 rolls fail |
| Acceptance | `ShouldAcceptDeal`: 0.20 base, +0.20 label-sought, +0.35 if cash-pressured | 59% |

The binding condition is the **evidence gate**. The two probability gates do not filter, they only delay. A label
that misses the 40% roll or gets rejected simply tries again next month, and the evidence never expires because
`peakBreakoutScore` is a running maximum. A label with evidence by week 13 gets about nine monthly tries in 1960,
and 1 − (1 − 0.40 × 0.59)⁹ ≈ 91%. The gap from 89% × 91% down to the observed 66% is late evidence (a quarter
of labels first qualify after week 18, so they get fewer tries) and distributor misses.

### Why the evidence gate passes nearly everyone

`peakBreakoutScore ≥ 0.20` was meant to mean "this record is breaking out in a market". It is reached by **697 of 941
(74%) Small records released in 1960**, usually in release week 3. Decomposing `CalculateBreakoutEvidence` at the week
each record first crosses 0.20 (mean weighted terms, before the volume multiplier):

| Term (weight) | Contribution |
|---|---:|
| velocity (0.15) | 0.121 |
| sustained growth (0.09) | 0.070 |
| quality (0.08) | 0.051 |
| volume (0.30) | 0.048 |
| genre fit (0.08) | 0.046 |
| awareness (0.12) | 0.031 |
| media (0.10) / unmet demand (0.08) | 0.005 / 0.000 |

**87% of the evidence at the crossing is not volume.** Median regional demand that week is 388 copies, and 218 are
sold. Velocity is *relative* growth over a 150-copy floor, so the ordinary launch ramp of any release (weeks 2–3
rising off week 1) scores as velocity and sustained growth. Quality and genre fit are static. Together, the ramp and
the static terms carry a typical non-charting single over the bar. Recomputing the raw evidence without the velocity
and sustained terms leaves 142 of 230 labels over 0.20 at some point. Also dropping quality and genre fit leaves 93.
(These are upper bounds: raw evidence, not the lagged `breakoutScore`.)

The bar was lowered 0.24 → 0.20 to match `LocalTractionActivationScore` (see `ChartManager.cs` ~166 and
`CompetitorManager.cs` ~215). That served the discovery basin for chart breadth, but it also made the deal gate a
"typical release" bar.

### Historical comparison: how common was a Motown-style deal?

- **Motown's deal was a per-record lease of a proven regional hit, not a label contract.** Tamla 101, Marv Johnson's
  "Come to Me" (January 1959), sold in Detroit first. Gordy then leased it to United Artists for national release, and UA
  also took Johnson's contract. Gordy kept Michigan distribution
  ([Michigan Rock and Roll Legends](https://michiganrockandrolllegends.com/hall-of-fame/legendary-mi-songs/133-come-to-me),
  [Wikipedia](https://en.wikipedia.org/wiki/Come_to_Me_(Marv_Johnson_song))). "Money (That's What I Want)" went the same
  way, Tamla → Anna → Chess national distribution
  ([Wikipedia](https://en.wikipedia.org/wiki/Money_(That%27s_What_I_Want))). The trigger was demand Tamla could not
  meet, and the record was the unit of the deal.
- **The normal channel for a micro label was the regional independent distributor, city by city.** Pickups by larger
  labels were common as a *mechanism*: a small label sold or leased a master once a hit outgrew it, for example Renown
  selling "What It Was Was Football" to Capitol ([NCpedia](https://www.ncpedia.org/recording-industry-part-3-independe)).
  Leasing was a standard practice, but no source found quantifies its rate.
- **Scale.** In 1960 only **55 labels** placed a record in the top 20
  ([Deacon Street](https://www.deaconstreet.com/ElvisEra/1960/1960recordlabels.html)), against a population of
  hundreds to low thousands of active labels. A national pickup requires a regional hit first, so it is an event for a
  few dozen records a year, not for most micro labels.

**Estimate, not a measured rate:** a national P&D deal or master lease was something a **few percent of micro labels
a year** achieved, perhaps up to ~10% in a hot genre year. 66% a year is an order of magnitude high. Treat the target
band as **5–15% of starting-Small labels in 1960**, i.e. roughly 10–35 deals rather than 152. That is a design
judgment anchored on the 55-label top-20 count and the per-record nature of the documented cases.

### What a fix would look like (not implemented)

1. Make the evidence a regional **hit**, not a ramp. Require volume, for example `volumeInput ≥ ~0.5` (about 1,800 × region scale
   copies a week of demand) in the evidence region, or gate on the `RegionalBreakout` stage (0.40 with two
   traction weeks), not on `LocalTraction`. Decouple `regionalBreakoutDealThreshold` from
   `LocalTractionActivationScore`, so the discovery basin keeps its 0.20 entry.
2. Make the offer per record and perishable. Read the *current* `breakoutScore` or a recent window, not the running
   peak, so a label cannot spend a year retrying on one old ramp. This matches the history: the deal is for the record
   that is selling now.
3. Measure as in §6: Small per-release units, Small closure, home-region share, `top11OverlapByPair`, plus the
   breadth guards. The memory notes show Major distribution was the chart-access gate for independents, so check
   chart breadth before and after.

## 8. Opening finances (2026-10-16)

### Where opening cash comes from

| Path | Code | Before | Read by |
|---|---|---|---|
| 1960 world, all tiers | `AILabelFactory.ApplyTierStats` | flat uniform per tier | everything |
| Runtime births (Small/Independent) | `LabelGenerator.InitializeFinancials` | its own copy of the Small/Independent rows, plus inverted unreachable Major ($5k–15k) and MidTier ($500–2,000) rows | everything |
| Dependent hitmakers (12% of Independent births) | `RuntimeLabelProfileFactory` | capped at 2× monthly overhead | unchanged |

Opening cash against each tier's own 1960 scale (seed 2002, week 1 vs that year's gross):

| Tier | Median opening cash | Median 1960 gross (control / dealer price) | Cash ÷ monthly gross | Ever below $0 in 1960 (dealer price) |
|---|---:|---:|---:|---:|
| Major | $258k | $14.5M / $10.1M | 0.3 | 0% |
| MidTier | $40.6k | $2.0M / $1.6M | 0.3 | 0% |
| Independent | $12.9k | $441k / $256k | 0.4–0.6 | 14% |
| Boutique | $8.9k | $243k / $169k | 0.4–0.7 | 10% |
| Small | $4.8k | $60k / $34k | 0.9–1.7 | 18% |

Documented founding stakes: **Vee-Jay $500** (1953, a pawnbroker loan, all spent on the first sessions;
[NPR](https://www.npr.org/2007/08/21/13774728/the-success-and-undoing-of-vee-jay-records)), **Tamla $800** (1959,
the Gordy family fund; [Wikipedia](https://en.wikipedia.org/wiki/Berry_Gordy)), **Satellite/Stax $2,500** (1958,
Estelle Axton's mortgage; [Wikipedia](https://en.wikipedia.org/wiki/Stax_Records)), and **Atlantic $12,500** (1947,
$10,000 from a family dentist plus Abramson's $2,500; [Wikipedia](https://en.wikipedia.org/wiki/Atlantic_Records)).
The first three are micro labels; Atlantic was capitalized as a national independent. The model's Small median of
$4.5k sits above every micro-label stake, and its $8k top end is close to Atlantic's founding.

### Tried: Small opening cash log-uniform $400–5,000 (reverted)

The trial drew Small cash log-uniformly from $400 to $5,000 (median ≈ $1,400, p10 $515, p90 $3,900), so the three
micro-label anchors sat in the bulk. Every other tier was unchanged, and births used the same table. Three 52-week runs
used the regional + economic-exit flags (assembly `cash1-runs-assembly.txt`):

| 1960, starting-Small cohort | Control prices: before → trial (2002) | Dealer-price pressing: before → trial (2002) | Pressing, seed 1001: before\* → trial |
|---|---:|---:|---:|
| Opening cash median | $4,806 → $1,579 | $4,875 → $1,646 | $4,378 → $1,337 |
| Closed by wk 26 | 0.9% → **24.3%** | 3.9% → **30.9%** | 4.8% → 36.8% |
| Closed by wk 52 | 3.5% → **35.7%** | 10.9% → **47.0%** | 14.7% → **55.0%** |
| P&D deals in year 1 | 66% → 50% | 64% → 43% | 61% → 35% |
| Runtime births / closed by wk 52 | n/a → 37 / 4 | 12 / 0 → 51 / 3 | 15 / 1 → 59 / 5 |

\* `pressing2-1001-104`, which predates the catalogue pre-press fix. Before = `economic-holdout-new-2002-260`,
`pressing3-2002-52`.

**Why so many close, at control prices too.** Year-1 closure by opening cash (control prices, seed 2002):

| Opening cash | n | Closed | Median releases | Median 1960 gross |
|---|---:|---:|---:|---:|
| ≤ $800 | 50 | 84% | 0 | $0 |
| $800–1,500 | 57 | 51% | 0 | $934 |
| $1,500–2,500 | 54 | 20% | 3 | $36.6k |
| > $2,500 | 69 | 0% | 3–4 | $64k |

**98% of the closed labels never released a record.** They close in a median week 22, with no receivables
outstanding. A first release needs roughly $1,300–1,500 up front: the act's advance, the 2×-overhead signing reserve
(`CommitmentReserve`), production ($350), marketing and one month's reserve. Below that, the label sits paying
$150 + $30/artist a month in overhead until it fails two monthly reviews. These closures are imposed by the model's
cost structure, not earned on bad records (memory: closures-must-be-earned).

**The history this collides with.** Vee-Jay spent its whole $500 on the first sessions. Tamla's $800 paid for a
session and a pressing. These founders had near-zero fixed cost (a record shop, a house, a day job) and ran on
pressing-plant and distributor credit. The model gives an AI Small label a fixed monthly overhead, no trade credit,
a reserve requirement on top of the release cost, and two months to cure negative cash. So a realistic opening
balance can only be adopted together with that cost and credit structure.

**Decision: Small stays at $1,000–8,000.** `Systems/LabelStartingFinances.cs` reproduces every tier's former
uniform table value-for-value (asserted in `--economic-label-check`). What remains is the consolidation: one table
for the 1960 world and for births.

**What would make realistic cash work (not implemented):**
1. Small fixed overhead near zero until the first release, or only while records are on sale. Today it is $150 +
   $30/artist from week 1.
2. Plant credit: the pressing book's plant bill on 30–60-day terms instead of cash at once (§5 pays at once).
3. Let a founding label commit its whole stake to the first release. Exempt a label with no catalogue from the
   signing/production reserve, or size the reserve on the first session only.

With those, $400–5,000 opening cash becomes testable against the 20–35% five-year band without a die-off of
never-released shells.

### Factory cleanup

- `LabelGenerator`: removed `GenerateHistoricalMajors`, `CreateHistoricalMajor`, `CreateHistoricalIndie` and
  `GenerateLabels` (no callers; their Major cash was $5k–15k). Removed the empty `ApplyArchetypeStats` shell and the
  inverted Major/MidTier finance rows. A non-birth tier now throws.
- With the lifecycle on (the scene default and every current run), `RuntimeLabelProfileFactory` overwrote the
  archetype, genres, founding year, months active and track record that `GenerateSingleLabel` drew.
  `GenerateSingleLabel(..., legacyIdentity)` now draws those only for the lifecycle-off path, in their original order.
  **This removes 2–4 shared-stream draws per runtime birth**, so on the enabled path every draw after the first runtime
  birth is reshuffled. Comparisons against older runs are seed-level, not paired (memory: one conversion
  reshuffles the RNG stream).
- `AILabelFactory`: procedural `foundedYear` was drawn from 1948–1962 in a 1960 world, which the label panel showed
  as "Founded 1962". It is now 1948–1960. Display-only, same single draw.

- `AILabelFactory` and `LabelGenerator` now share `LabelStartingFinances` for opening cash (same values).

### Verification of the cleanup (values unchanged)

`cleanup1-2002-52` (assembly `cleanup1-runs-assembly.txt`) uses the `pressing3-2002-52` flags on the cleaned-up
code. `-label-finance.csv` is **identical for weeks 1–21** and first differs in **week 22, the first runtime birth**,
as the removed birth draws predict. After that, year-1 outcomes sit within reshuffle noise: starting-Small closed
10.9% → 10.4%, P&D deals 64% → 60%, births 12 → 14. Starting Boutique closures go 0 → 3 labels and Independent 7 →
9. `--economic-label-check` and `--label-pressing-check` pass. Paired comparisons against runs older than this
assembly hold only up to the first birth.

## 9. Implemented: regional-hit deal gate (`--regional-hit-deal-gate`) (2026-10-16)

`Systems/RegionalHitDealGate.cs`. Off by default; every caller gates on it. This is the §7 fix, applied to the P&D pull route
only (`EvaluateRegionalDealEvidence`, which the AI pull trigger and the player's `PursueDistributionDeal` share).

- **Decoupled bar.** A record-region is deal evidence only while its *current* `breakoutScore ≥ 0.40` (the RegionalBreakout
  stage's own score) **and** `breakoutVolumeInput ≥ 0.50` (about 1,800 × region scale copies a week of demand).
  `LocalTractionActivationScore` and the discovery basin keep 0.20. `regionalBreakoutDealThreshold` still serves wholesale
  placement, trade press and media.
- **Recent, not peak.** The breakout pass stamps the record age of each qualifying week (`RegionalRecordData.dealEvidenceAge`,
  saved). Evidence lapses after 4 weeks, one monthly check interval. Offline sizing on `cleanup1-2002-52` showed a strict
  current-week read finds 76 starting-Small labels at check weeks, and the 4-week window finds 89, because some breakouts fall between two checks.
- **The deal covers the hit.** The earning record is the qualifying record with the highest current score, not the label's
  biggest historical peak. With no recent hit, courting falls back to the old strongest-record rule.
- Assumptions: the 0.40 / 0.50 bars and the 4-week window. Offline, the volume bar is nearly redundant at score 0.40
  (77 → 76 labels). It is kept as an explicit guard in case the score's static terms rise.

**Validation.** `--regional-hit-deal-check` (new, `SaveLoadRoundTripRunner`), `--label-pressing-check` and
`--economic-label-check` pass. A 26-week switch-off replay (`gateoff-2002-26`) is **byte-identical** to `cleanup1-2002-52` on
all 54 weekly CSVs. The only differences are the two end-of-run snapshots (`live-records-snapshot`, the retirement flush). So
`cleanup1-2002-52` is the seed-2002 control. Seeds 1001 and 3003 got fresh switch-off controls on the same assembly
(`gate1-runs-assembly.txt`). All runs use the `cleanup1` flags (economic exits + `--dealer-price-pressing`), 52 weeks.

### Results, 1960, three seeds (control → treatment, 2002 / 1001 / 3003)

| | 2002 | 1001 | 3003 |
|---|---:|---:|---:|
| **Starting Small signing a P&D deal** | 60.4 → **13.9%** | 59.3 → **10.4%** | 57.8 → **14.2%** |
| Small labels with pull evidence | 83.9 → 33.5% | 80.1 → 26.8% | 80.2 → 26.7% |
| P&D deals signed, all clients | 396 → 163 | 412 → 156 | 389 → 163 |
| **Wholesale placements, all labels** | 779 → **1,200** | 758 → **1,127** | 840 → **1,204** |
| Wholesale placements, starting Small | 227 → 398 | 244 → 376 | 250 → 405 |
| Covered regions at wk 6, Small releases Q1 / Q4 | 2 / 5 → 2 / 4 | 2 / 5 → 2 / 5 | 2 / 5 → 2 / 4 |
| Small in-run singles | 760 → 715 | 727 → 649 | 725 → 627 |
| Small single units, median / p90 | 8,699 / 32.4k → 8,617 / 29.0k | 8,037 / 27.1k → 8,905 / 28.6k | 8,221 / 29.8k → 8,119 / 27.0k |
| Copies outside home region | 62.9 → 61.6% | 63.4 → 62.6% | 63.9 → 60.8% |
| top-11 overlap, mean (pair min–max) | .883 → .863 (.83–.90) | .849 → .863 (.83–.91) | .868 → .879 (.83–.90) |
| **Starting Small closed, wk 52** | 10.4 → 13.9% | 16.0 → 22.1% | 15.9 → 20.3% |
| Starting Boutique / Independent closed | 2.1 / 4.6 → 0.7 / 6.6% | 2.3 / 2.9 → 3.1 / 1.9% | 4.0 / 4.6 → 8.0 / 8.8% |
| Chart entries / firms charting | 742 / 263 → 717 / 252 | 804 / 260 → 766 / 243 | 744 / 258 → 725 / 246 |
| Major-family chart share | 60.9 → 58.9 (−2.0) | 54.4 → 61.1 (**+6.7**) | 53.8 → 57.9 (**+4.1**) |
| C4 | 29.1 → 27.6 | 24.2 → 27.1 | 24.3 → 31.1 |

Single units, number ones, genre gap, airplay share and mean chart life stay within seed noise
(`analyze_regional_guards.py`).

**Reading.**

1. **The gate does its job.** P&D signings by starting-Small labels land in the 5–15% band on all three seeds, and they now
   go mostly to Major and MidTier distributors rather than to qualifying Independents.
2. **Wholesale houses replace the P&D deals, and the access ramp barely moves.** Placements rise 44–58%. Coverage of a Small
   release still climbs from 2 regions to 4–5 within the year. Per-release units, the home-region share and top-11 overlap
   do not change. Wholesale placement keeps the old evidence (`GetProvenBreakoutRegions`: running peak ≥ 0.20, plus adjacent
   regions), so §6's "easy distribution" now runs through the houses. This partly matches the history (§7: the regional
   independent distributor was the normal channel for a micro label). What does not match is reaching near-national
   coverage in under a year.
3. **The overlap miss is not a distribution effect.** Cutting P&D by 60% leaves top-11 overlap at 86–88% with no distance
   gradient. That retires the §6 idea that distribution access is the lever shared with the §4 miss. The cross-region
   common terms named in §4 (national awareness blend, national radioHeat, national rank exposure) are what is left.
4. **Closures rise 3.5–6.1 pts, and they are earned on cash timing.** Most labels that close only under the gate held a P&D
   deal in the control (14 of 18 / 18 of 23 / 17 of 23). Under the gate they placed with a house instead (14 / 19 / 17), and
   only 3–5 never released. Wholesale receipts arrive about 90 days after billing, while the §5 plant bill is paid at once, and
   these labels also lose the pull advance. Fewer Small releases (−6 to −14%) follow from the tighter cash.
5. **Guards.** Chart breadth falls slightly: entries −19 to −38, firms charting −11 to −17. Major-family share breaches ±3
   on two seeds (+6.7, +4.1; −2.0 on 2002), mean +2.9, and C4 rises on two. §5 put the noise scale at about ±5 pts per run, so
   this is suggestive, not established. The direction makes sense: fewer independents ride a Major or MidTier network to the chart.

**Next lever, not implemented.** The remaining ease of access is the speed of wholesale placement, not the P&D gate. Gating
houses on the regional-hit bar would contradict the history, because houses are the right channel for an unproven local
record. The candidates are slower spread instead: a placement per region only after the record has sold there for some
weeks, or no neighbour hop without a hit in the source region. Measure against covered regions by quarter, the home-region
share and Small closure. Keep this switch on in that test, so the two routes do not substitute for each other again.

Runs: `SimLogs/gate1-{2002,1001,3003}-52`, `gatectl-{1001,3003}-52`, `gateoff-2002-26`; assembly hash in
`gate1-runs-assembly.txt`.

## 10. Implemented: slower wholesale spread (`--slow-wholesale-spread`) (2026-10-16)

`Systems/WholesaleSpreadGate.cs`. Off by default; every caller gates on it. This is the §9 "next lever". Both arms run with
`--regional-hit-deal-gate`, so the P&D and wholesale routes cannot substitute for each other again.

**What carried the spread.** Under the deal gate, **304 of 398** 1960 wholesale placements by starting-Small labels
(`gate1-2002-52`) were *neighbour hops*. `SelectIndependentDistributionTarget` hops to a bordering market whenever no
proven market is placeable. A Small label proven only at home, a market it already covers, therefore hopped outward on every
35% monthly roll, into markets where nothing of its had sold.

**The rule.** Houses are still the right channel for an unproven local record, so placement is paced, not gated on a hit:

- **Direct placement** in a market needs a record holding LocalTraction there now: `tractionWeeks ≥ 3`. That counter rises
  while the current score is ≥ 0.20 and falls by one otherwise, so the proof fades with the record. The running peak no
  longer counts.
- **A neighbour hop** needs a regional hit in the *source* market now: `RegionalHitDealGate.IsRecent`, meaning current score
  ≥ 0.40, volume ≥ 0.50, within 4 weeks. The breakout pass now stamps `dealEvidenceAge` when either switch is on. The deal
  gate still reads it only while its own switch is on.
- Offline sizing on `gate1-2002-52` kept 37 of the 94 direct placements and 25 of the 304 hops, before any behavioural
  response. The 3-week bar is a model choice; 4 weeks would keep 25 of 94.
- The player's house proof gate (`HasProvenBreakoutIn`) reads the same proven set, so under the switch it also uses the
  sustained bar.

**Validation.** `--regional-hit-deal-check` gains the slow-spread cases and passes. A 26-week switch-off replay
(`spreadoff-2002-26`, deal gate on) is **byte-identical** to `gate1-2002-52` on all 54 weekly CSVs, apart from the same two
end-of-run snapshots as before. The 1001 and 3003 controls are `gate1-*-52` from the §9 assembly. The switch-off path is
the same code on every seed, but only seed 2002 was replayed. Treatment assembly: `spread1-runs-assembly.txt`.

### Results, 1960 (deal gate on in both arms; control → treatment, 2002 / 1001 / 3003)

| | 2002 | 1001 | 3003 |
|---|---:|---:|---:|
| **Wholesale placements, starting Small** | 398 → **108** | 376 → **97** | 405 → **109** |
| direct / neighbour hop | 94 / 304 → 65 / 43 | 73 / 303 → 56 / 41 | 89 / 316 → 65 / 44 |
| Starting Small with any house | 75.7 → 35.2% | 70.1 → 31.6% | 76.3 → 33.6% |
| Wholesale placements, all labels | 1,200 → 534 | 1,127 → 494 | 1,204 → 537 |
| Starting Small signing P&D | 13.9 → 20.9% | 10.4 → 13.4% | 14.2 → 12.1% |
| **Covered regions at wk 6, Small releases Q1 / Q2 / Q3 / Q4** | 2/3/4/4 → **2/2/3/3** | 2/3/4/5 → **2/2/3/3** | 2/3/4/4 → **2/2/3/3** |
| **Copies outside home region** | 61.6 → 59.6% | 62.6 → 55.7% | 60.8 → 55.4% |
| Small single units, median / p90 | 8,617 / 29.0k → 8,102 / 28.0k | 8,905 / 28.6k → 7,217 / 25.2k | 8,119 / 27.0k → 7,040 / 24.4k |
| Small in-run singles | 715 → 751 | 649 → 738 | 627 → 705 |
| **Starting Small closed, wk 52** | 13.9 → **9.6%** | 22.1 → **14.7%** | 20.3 → **15.5%** |
| Starting Boutique / Independent closed | 0.7 / 6.6 → 1.4 / 2.0% | 3.1 / 1.9 → 2.3 / 2.4% | 8.0 / 8.8 → 4.0 / 5.5% |
| top-11 overlap, mean (pair min–max) | .863 → .878 (.86–.93) | .863 → .868 (.84–.90) | .879 → .870 (.85–.90) |
| Chart entries / firms charting | 717 / 252 → 736 / 250 | 766 / 243 → 771 / 242 | 725 / 246 → 736 / 244 |
| Small chart entries | 35 → 33 | 31 → 21 | 29 → 20 |
| Major-family chart share | 58.9 → 58.9 (0.0) | 61.1 → 55.3 (−5.8) | 57.9 → 58.2 (+0.3) |
| C4 | 27.6 → 29.1 | 27.1 → 27.3 | 31.1 → 32.5 |

Single units, number ones, genre gap, airplay share and mean chart life stay within seed noise
(`analyze_regional_guards.py`).

**Reading.**

1. **This is the lever that moves access.** With both switches on, a Small release's coverage ramps 2 → 3 regions over 1960
   instead of 2 → 4–5. Placements fall 73% and hops 86%. About a third of starting-Small labels hold any house by year end,
   against three quarters before. It is the first change in this arc to move the §6 coverage ramp and the home-region share
   (−2 to −7 pts).
2. **Units per release fall, but less than coverage.** The median falls 6–19% and p90 3–12%. Total single units are
   unchanged, so the copies move to larger labels. A Small single still sells 7–8k copies and 55% of them outside the home
   region. The §6 reasons for that remain: the factory's 0–2 extra starting regions, and out-of-territory stock.
3. **Closures fall back to about the no-gate level.** Starting-Small closure falls 4–7 pts, to 9.6 / 14.7 / 15.5%. The
   switch-off control *without* either gate (§9) had 10.4 / 16.0 / 15.9%. This confirms the §9 mechanism: those closures came
   from wholesale's 90-day receipts against an up-front plant bill. Fewer house placements mean fewer such squeezes, and
   Small releases rise 5–14%. The §5 band (20–35% over five years) cannot be judged from one year.
4. **P&D picks up a little of the slack.** On seed 2002 P&D signing goes to 20.9%, above the §9 band; the other two seeds stay
   in it. A label without houses has more uncovered regions to offer a distributor, and a lower `ownedReach` passes
   `ShouldAcceptDeal`'s +0.05 reach test more easily. One seed is too few to call this a breach.
5. **Overlap still does not move.** Top-11 overlap stays at 87–88% with no distance gradient, although Small coverage and
   home share both moved. Together with §9, this confirms the §4 miss sits in the cross-region common terms (the national
   awareness blend, national radioHeat and national rank exposure), not in distribution.
6. **Guards hold.** Chart entries rise 5–19, and firms charting move by at most −2. Small chart entries fall on two seeds
   (−10, −9). Major-family share is flat on two seeds and −5.8 on 1001, inside the ±5-point per-run noise from §5. C4 rises
   0.2–1.5.

**Not done.** Both switches need a five-year run to read Small closure against the §2 band. They also need a decade A/B
against market share and the Major-share guards (memory: decade-ab-only-at-audit-end). The 3-traction-week bar and the
reuse of the deal gate's 0.40/0.50 hit bar for hops have no period source.

Runs: `SimLogs/spread1-{2002,1001,3003}-52`, `spreadoff-2002-26`; controls `gate1-*-52`.

## 11. Plant credit, Small home territory, and the five-year A/B (2026-10-16)

Two more switches, both off by default:

- **`--plant-credit`** (requires `--dealer-price-pressing`; `LabelPressingEconomics.SettlePlantAccount`). The plant bills on
  `PlantTermWeeks` = 4 terms ("net 30") instead of cash with the order. Bills become `AILabel.plantPayables` and are paid in full
  the week they fall due. Cash still owed to the plant is added to `CommitmentReserve`, so it is never spent on a new release
  or a signing. New label-finance columns: `weeklyPlantPaid`, `outstandingPlantPayables`. The 4-week term is a model choice.
- **`--small-home-territory`** (`Systems/SmallLabelHomeTerritory.cs`). Factory Small labels start with only their home region,
  for both distribution and strong regions. Before, they got 0–2 extra regions and a 40% chance of a strong neighbour. Every
  factory draw is still made, so the other labels in the 1960 world generate identically. Runtime births were already home-only.

**Validation.** `--label-pressing-check` covers terms, the reserve, saves, the player boundary and the factory. In the factory
check, 198 of 250 Small labels narrowed, and every other label was unchanged. `--economic-label-check` and
`--regional-hit-deal-check` pass. A 26-week switch-off replay (`plantoff-2002-26`, with the deal gate and slow spread on) is
identical to `spread1-2002-52` on all 53 weekly CSVs and on the label tables. As before, only the two end-of-run snapshots
differ.

### Five-year A/B, seed 2002, with attribution arms

Every arm carries the regional stack and economic exits. Control = today's defaults (`ab5ctl`). `press` = `--dealer-price-pressing`.
`nohome` = press + deal gate + slow spread + plant credit. `trt` = nohome + Small home territory. Assembly:
`ab5-runs-assembly.txt`. The seed-1001 pair was stopped at about week 40: one seed already ruled the full bundle out, and the
attribution arms were the better use of the run time.

| Seed 2002 | ctl | press | nohome | trt |
|---|---:|---:|---:|---:|
| **Starting Small closed, cumulative 1960 / 61 / 62 / 63 / 64** | 3.5 / 4.3 / 5.7 / 6.1 / **6.5** | 10.4 / 21.7 / 27.8 / 36.1 / **39.6** | 7.8 / 17.8 / 30.0 / 33.5 / **38.7** | 9.1 / 19.1 / 32.6 / 40.9 / **44.3** |
| Starting Boutique / Independent closed, 5 yr | 0 / 0 | 8.9 / 11.2 | 5.5 / 4.6 | 5.5 / 3.6 |
| Small signing P&D, 1960 (% of alive) | 66.1 | 60.4 | **16.1** | 28.7 |
| Small signing P&D, 1961–64 | 28.5 / 22.5 / 21.7 / 19.4 | 21.5 / 21.2 / 17.1 / 17.7 | 8.0 / 9.2 / 6.9 / 10.5 | 13.4 / 11.4 / 3.9 / 9.0 |
| Covered regions at wk 6, Small singles 1960 / 61 / 62–64 | 4 / 5 / 5 | 3 / 5 / 5–6 | 3 / 3 / 3–4 | **1** / 3 / 4 |
| Copies outside home region, 1960 / 1964 | 67.5 / 72.2% | 64.4 / 69.8% | 59.4 / 68.1% | **46.7** / 65.3% |
| Small single median units 1960 / 1964 | 11.1k / 7.5k | 11.5k / 8.3k | 9.6k / 6.8k | 10.0k / 7.4k |
| Starting-Small retired singles losing money | 1.9% | 12.8% | 16.8% | 18.0% |
| Major-family chart share 1960 (vs ctl) | 50.5 | 60.9 (+10.4) | 55.1 (**+4.6**) | 63.6 (**+13.1**) |
| Major-family chart share 1961–64 (vs ctl) | 58.3 / 51.5 / 49.1 / 53.7 | +2.2 / +1.5 / −1.3 / 0.0 | −9.1 / −5.1 / −0.3 / −2.3 | −5.2 / −7.3 / +3.1 / −6.0 |
| Firms charting 1960 / 1964 | 276 / 313 | 263 / 280 | 247 / 279 | 248 / 294 |
| Small chart entries 1960 / 61 / 62 | 46 / 42 / 19 | 43 / 24 / 13 | 26 / 19 / 5 | 35 / 30 / 15 |
| Genre gap 1964 | .467 | .412 | .396 | .404 |
| Top-11 overlap mean | .889 | .895 | .884 | .885 |

Units are flat in every arm (+1–2% single units). Album share is −1 to −2 pts, and number ones, airplay share and chart life
are within seed noise (`ab5-2002-attribution-guards.txt`).

**Who closes (trt vs ctl, `closure_diag`).** In the control, 10 of the 15 closures never released a record. In the treatment, 54
of 102 closed labels had 3 or more records, with a median lifetime gross of $13.6k. They are not being drained by pressing
waste: unsold stock is 3.5% of what they pressed. Pressing runs ~46% of dealer-price gross for survivors and closed labels
alike. They are not caught by cash timing either. Receivables at the last solvent month were a median $0, and only 14 still owed
the plant. They close on volume: their singles sold a median 6.0k copies against survivors' 9.6k. These are earned closures
(memory: closures-must-be-earned).

**Reading (one seed; about ±5 pts per tier per run).**

1. **Pressing carries the attrition.** By itself it reaches 39.6%, which matches pressing2's 38.7% on the older assembly. It
   is above the band even without the other switches. The doc rule still stands: do not lower the price to hit a number.
2. **The two gates plus plant credit are close to neutral on Small closure** (39.6 → 38.7%). They do what they were for. P&D
   signing falls into the §7 band in every year (16.1 / 8.0 / 9.2 / 6.9 / 10.5%), wholesale hops fall, and the coverage ramp
   slows to 3 → 4 regions. Independent and Boutique five-year closure falls from 11.2 / 8.9% to 4.6 / 5.5%. The 1960
   Major-family excess drops from +10.4 to +4.6. The cost is chart breadth: Small chart entries and firms charting fall.
   Plant credit cannot be separated from the gates in these arms. Its 4-week float is small (median $2.3k owed), and the
   closure diagnosis says timing is not what binds.
3. **Home territory is net harmful and should not be defaulted.** It moves 1960 coverage to 1 region and the home share up by
   13 pts, but by 1961 its coverage matches `nohome`. Meanwhile it adds +5.6 pts of closure, pushes 1960 P&D back above the
   band (16.1 → 28.7%, since a home-only label has more uncovered regions to offer), and adds +8.5 pts to 1960 Major share. The
   switch stays in the code, off.
4. **Overlap is unmoved in every arm**, which confirms §9/§10.

**Default candidate:** pressing + deal gate + slow spread + plant credit (`nohome`). Small five-year closure is 38.7%, about 4
pts above the band. That is inside single-seed noise of the band top, but it sits on the high side, and it comes from the
price rather than the gates. The remaining excess is a volume question: closed labels sell 6k-copy singles. §1 already said
price cannot be corrected without revisiting volume.

Runs: `SimLogs/ab5{ctl,press,nohome,trt}-2002-260`, `plantoff-2002-26`; tables `ab5-2002-attribution.txt`,
`ab5-2002-attribution-guards.txt`, `ab5-2002-vs-control.txt`.

## 12. Cost-structure investigation and defaults (Codex, 2026-10-16)

The user accepted dealer-price pressing, the regional-hit deal gate, slow wholesale spread and plant credit as defaults.
These now default on. The positive flags remain accepted for existing run recipes. Explicit comparison switches are
`--no-dealer-price-pressing`, `--no-regional-hit-deal-gate`, `--no-slow-wholesale-spread` and `--no-plant-credit`.
After the validation below, paired period pressing costs and Small home territory also default on. Their comparison
switches are `--legacy-pressing-costs` and `--no-small-home-territory`; `--legacy-single-costs` remains an alias for the
old bundled costs. Conflicting explicit options are rejected.

### Two different closure measures

Section 2 says that promotion out of Small is a separate non-failure outcome. The old analyzer nevertheless counts a
starting-Small label's later bankruptcy after promotion in its Small closure percentage. Both are useful measures, but
they answer different questions. The existing all-firm cohort rows are retained; a new row reports financial exit
**before first promotion**, with promotions reported separately. An eventual demotion does not undo the first successful
exit from Small for this competing-outcomes measure.

For the old seed-2002 runs:

| Five-year outcome, starting Small n=230 | nohome | home territory |
|---|---:|---:|
| Firm closed at any later tier (old table) | 89 / 38.7% | 102 / 44.3% |
| Closed after having been promoted | 3 | 13 |
| Financial closure before first promotion | **86 / 37.4%** | **89 / 38.7%** |

Thus the home flag's excess is 13 firms on the original cohort measure but three on the documented Small-exit measure.
It is not a clean experiment in the units of the same 13 marginal labels: 39 firms close only in the home run, 26 only
in nohome, and 63 in both. The shared RNG diverges after different operating decisions. Nor does a 6k-vs-9.6k comparison
by itself validate the underlying cost assumptions.

### A plant-cost mismatch, independently of the desired attrition band

The AI charges a flat **30c per single pressed**, including every re-press. The player already pays 12c vinyl + 2c
sleeve/label, $38 first-run setup and $20 shipping per order. The AI's old number is described in code as bundled COGS,
including an implicit composition allowance; it is not a sourced physical plant quote.

Primary period evidence: [Billboard, March 30, 1959, "Pinch-Penny Processes: How Mean Can You Get?"](
https://www.worldradiohistory.com/Archive-All-Music/Billboard/50s/1959/Billboard%201959-03-30.pdf)
reports standard 45 pressing at **11–13c in quantities around 5,000**. This supports checking the physical cost before
raising label sales to compensate for a 30c invoice. It does not measure the player's exact small-order setup/shipping
fees. Those are existing model assumptions, reused without fitting them to closure percentages.

The initial `--period-single-costs` experiment tests the existing player plant quote for AI singles, including setup on first run, reused
stampers on re-presses, and shipping on each order. The release reserve uses the same complete invoice. A separate
**4c allowance per sold two-sided single** preserves two 2c composition costs from the old bundled COGS, without
charging composition costs on unsold vinyl or putting them on plant credit. Existing publishing-control routing is
unchanged. Other formats' costs, dealer prices, sales demand, opening cash and the insolvency rule are unchanged.
`--legacy-single-costs` identifies the old flat-cost comparison.

`RunSmallCostPilot.ps1` records assembly hashes, exact arguments, run durations and completion status. Start with 13-week
full-telemetry arms before extending candidates. `--calibration --small-label-finance` retains weekly cohort finance
in the fast telemetry mode for later runs; it changes only audit output. Do not infer a five-year rate from a short run.

Diagnostics: `analyze_small_cost_structure.py` decomposes the old runs, and `analyze_small_cost_pilots.py` compares matched
horizons while retaining both closure definitions. Static cost relief is labelled an upper bound, excludes setup and
shipping and ignores collection timing; it is not a survival forecast.

### Short-run rejection of the singles-only correction

All three 13-week runs completed, and the new four-default control was identical to the old `ab5nohome` run on finance,
records, national/regional settlements and outcomes. The fast 52-week control also reproduced all **31,281** weekly
finance rows exactly. Cost and insolvency fixed checks pass; the deal-gate check now explicitly disables slow spread
when checking the both-off path rather than relying on its former default.

Seed 2002, 52 weeks, four accepted switches on: the singles-only quote lowers Small closures **7.8% -> 6.1%** and raises
Major-family chart share **55.1% -> 56.8%**, but reduces album units **38.7M -> 31.7M (-18.1%)**. Album decision share
also falls sharply. This is a reason to stop the remaining singles-only arms before a long run, not to raise single
pressing costs until the old format mix returns. The incomplete `smallcost-year1-v1-home-2002-52` arm is not evidence.

The paired experiment is `--period-pressing-costs`: the single correction plus a **40c base LP pressing quote**, the
existing per-unit jacket/packaging cost and the same explicit order setup/shipping assumptions. Its composition
allowance is **2c per track sold**; missing catalog track snapshots and the decision prior use the generator's mean
of 11 tracks (22c). The album prior uses the corrected physical + composition cost too.

Primary evidence: [Billboard, June 20, 1960, "Trend to 'Bigness' Keys Indie Worry"](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1960/Billboard%201960-06-20.pdf)
reports ordinary outsourced vinyl LP pressing around **40c**, basic plant costs around **30–33c**, and RCA's competitive
summer quote at **28–30c with 120-day credit**. The model uses the ordinary quote; it does not assume that every small
client qualifies for RCA's offer. LP setup/shipping fees remain model assumptions. This corrects both formats' physical
costs from the same price level while keeping their composition allowances out of the plant bill.

Another observed fragility is separate from this cost experiment: Small-to-Independent promotion has no affordability
gate. In the old home-territory run `label_0228` is promoted at week 113 with **-$2,432** and closes at week 118. That
does not establish a new historical mortality rate. It reinforces the need to keep first promotion and subsequent
business failure separate in the report, and to audit any later expansion-cost repair independently.

### Paired-cost one-year validation (before five-year runs)

Both 13-week paired-cost smoke tests and the paired single/LP invoice checks pass. The compact audit also retains
charted-single rows and chart-life rows, allowing the existing airplay and turnover guards to run without logging every
off-chart record.

| 1960 | Seed 2002 | Seed 1001 |
|---|---:|---:|
| Old costs, four accepted switches: Small closures | 7.8% | 12.1% |
| Paired period costs, home flag off | **5.7%** | **8.2%** |
| Paired period costs, home flag on | **7.8%** | **10.4%** |
| Major-family chart share change, costs only | +4.3 pp | -2.0 pp |
| Album unit-share change, costs only | -0.3 pp | -2.5 pp |
| Single units change, costs only | +0.7% | +1.7% |
| Top-40 median airplay share change, costs only | +2.9 pp | -0.3 pp |

The average Major-family change is +1.2 points. These comparisons support proceeding to the long validation; they do
not establish a five-year attrition rate. Prices, opening cash, sales demand, release eligibility, promotion policy and
the insolvency rule have not been relaxed. The seed-2002 retired Small single median falls from 8.6k to 7.7k copies under
the cost-only change, so improved survival does not depend on raising Small sales.

Distribution is still imperfect: first-year starting-Small P&D signings rise from **16.1% / 16.5%** to **21.3% / 19.5%**
under costs only; home territory reaches **29.1%** on seed 2002. More viable releases create more qualifying hits and
offers. This remains above section 7's provisional deal-frequency band and is reported separately from financial
survival; the cost quotes are not fitted to either percentage.

Completed short runs: `smallcost-smoke-v1-*`, `smallcost-paired-smoke-v1-*`, the two completed
`smallcost-year1-v1-*` arms, `smallcost-paired-year1-v1-*`, and `smallcost-paired-year1-holdout-v1-*`.
Manifests, guard reports and the corrected cohort-definition regression are under `SimLogs/small-cost-investigation/`
and each run-tag directory. `smallcost-fiveyear-v1` extends the paired candidate on seed 2002 with home territory both
off and on, using the same assembly and explicit cost/territory flags.

### Final five-year result and adopted defaults

Both arms completed 260 weeks with native exit code 0, `CHART_AUDIT_COMPLETE`, and no engine errors. At the user's
request they ran concurrently after the first arm had started. Each took about 30 minutes; they share assembly
`E682FC70E82EED1BEEEF18249557F7571D6F07B8737C89240B4864122A7054A7`. The first 52 weeks of the long nohome arm reproduce
all 31,263 finance rows of its short counterpart exactly.

| Starting Small n=230 | Old costs, nohome | Old costs, home | Period costs, nohome | Period costs, home |
|---|---:|---:|---:|---:|
| Closures: years 1 / 2 / 3 / 4 / 5 | 7.8 / 17.8 / 30.0 / 33.5 / 38.7% | 9.1 / 19.1 / 32.6 / 40.9 / 44.3% | 5.7 / 13.0 / 15.2 / 18.3 / **18.7%** | 7.8 / 15.2 / 21.3 / 22.6 / **24.8%** |
| Closed firms | 89 | 102 | 43 | 57 |
| Closure before first promotion | 37.4% | 38.7% | 18.7% | **24.8%** |
| Survivors promoted out of Small | 60 | 62 | 83 | 79 |
| Retired Small single median units | 8,940 | 9,226 | 9,070 | 8,674 |
| Retired Small singles losing money | 16.8% | 18.0% | 10.0% | 10.5% |
| Pressing spend / gross, cohort | 46.0% | 45.8% | 22.5% | 22.4% |

**Decision:** the four agreed switches stay default on; the paired single/LP cost correction and home-only starting
territory become defaults too. Without the home flag, corrected costs land slightly below the design band. One
multi-state home region is the historically coherent starting footprint, and it now lands inside the band with the
same sourced cost schedule. No mortality threshold, cash grant, demand floor or closure quota was used to obtain this.
The new home result also front-loads failure: 35 of its 57 closures (61%) happen in years 1–2. None of the new runs'
closed starting-Small firms had first promoted, so the all-firm and competing-outcomes measures now agree.

The original "earned" interpretation was incomplete. Low-volume records can genuinely fail, but their cash outcomes
were being evaluated against inflated bundled plant invoices. Separating a physical plant quote from a composition
allowance lets a modest local record support its firm. The home flag still carries risk: it adds 14 closures / 6.1 points
under corrected costs. That is a real consequence of narrower access; the resulting level is 24.8%, not 44.3%.

Five-year guards: compared with the corresponding old-home arm, the adopted home configuration changes annual
Major-family share by **-1.1 / -0.8 / +7.9 / -0.8 / -1.2 points**. The 1962 +7.9-point observation breaches the old
per-year guard, despite a five-year mean change of only +0.8 points, and remains a macro-calibration concern; this is
not an all-guards-pass claim. Chart life is 7.57 versus 7.61 weeks. Chart entries are
763 / 766 / 797 / 772 / 795, versus 748 / 782 / 771 / 787 / 780. Single units change by +2.0 / -0.9 / -0.9 / -0.2 /
-0.2%; album unit share changes by -1.5 / +0.9 / +0.8 / +0.2 / +0.2 points. The concentration, airplay, units, genre and format
reports are retained in `SimLogs/small-cost-investigation/fiveyear-final-guards.txt` and `fiveyear-final.json`.
These are five-year results on one seed, supported by two-seed one-year direction checks, not a two-seed decade audit.
Deal frequency and the ungated Small promotion rule described above remain separate calibration issues.

`SimLogs/smallcost-fiveyear-v1/final-runs.json` is the combined completion manifest. It records that the sequential
controller prevented a duplicate home launch after concurrent dispatch; the corrected process-handle monitor records
the home's successful native completion. These orchestration notes do not represent engine failures.

For comparisons on the final assembly:

| Configuration | Additional options beyond the regional/economic stack |
|---|---|
| Adopted defaults | none (explicit `--period-pressing-costs --small-home-territory` is equivalent) |
| Period costs, home off | `--no-small-home-territory` |
| Original four-switch nohome bundle | `--legacy-pressing-costs --no-small-home-territory` |
| Original four-switch home bundle | `--legacy-pressing-costs --small-home-territory` |
| Pre-four-switch control | `--no-dealer-price-pressing --no-plant-credit --no-regional-hit-deal-gate --no-slow-wholesale-spread --no-small-home-territory` |

The final default and legacy comparison paths both pass fresh 13-week replay checks against the matching accepted
short-run artifacts. The default's finance, charted-record, outcome, geography and deal-ledger files are byte-identical
to the tested paired-home configuration. The legacy comparison's finance, outcomes, geography and deal ledger are
byte-identical to the old-cost nohome smoke run, and its charted records match the corresponding subset of that full
telemetry run. The invoice, economic-exit and deal-gate fixed checks pass on the final defaults; the build has zero
warnings and zero errors. `test_small_cost_analysis.py` also passes its promotion/demotion/acquisition definition
regression. Replay evidence: `final-default-replay.json` and `final-legacy-replay.json` in the investigation log folder.

## Reproduction

```text
python SimTools/analyze_historical_calibration_snapshot.py SimLogs/economic-holdout-new-2002-260
python SimTools/analyze_small_label_cash_counterfactual.py SimLogs/economic-holdout-new-2002-260
python SimTools/research_regional_chart_examples.py            # cached pages; --download to fetch
python SimTools/analyze_survey_overlap.py
python SimTools/analyze_pressing_economics.py SimLogs/economic-holdout-new-2002-260 SimLogs/pressing2-2002-260
python SimTools/analyze_regional_guards.py SimLogs/economic-holdout-new-2002-260 SimLogs/pressing2-2002-260
python SimTools/analyze_small_label_deal_funnel.py SimLogs/economic-holdout-new-2002-260        # §7 funnel
python SimTools/analyze_breakout_evidence_terms.py SimLogs/economic-holdout-new-2002-260        # §7 terms (~500 MB read)
python SimTools/analyze_opening_cash_year1.py SimLogs/pressing3-2002-52 SimLogs/cash1-2002-52 SimLogs/cleanup1-2002-52   # §8
python SimTools/analyze_small_closure_by_opening_cash.py SimLogs/cash1-off-2002-52              # §8 cash bands
python SimTools/analyze_deal_gate.py SimLogs/cleanup1-2002-52 SimLogs/gate1-2002-52 SimLogs/gatectl-1001-52 SimLogs/gate1-1001-52   # §9
python SimTools/analyze_deal_gate.py SimLogs/gate1-2002-52 SimLogs/spread1-2002-52 SimLogs/gate1-1001-52 SimLogs/spread1-1001-52   # §10
python SimTools/analyze_five_year_ab.py SimLogs/ab5ctl-2002-260 SimLogs/ab5press-2002-260 SimLogs/ab5nohome-2002-260 SimLogs/ab5trt-2002-260   # §11
```

Treatment runs use the economic-label reproduction flags from `RegionalSurvivalAndChartFollowup.md` plus
`--dealer-price-pressing`. Fixed checks: `SaveLoadRoundTripRunner.tscn` with `--label-pressing-check`.

Per-release unit/net figures in §1 came from a one-off pandas pass joining `-records.csv`
(max `totalUnitsSold` per record) to `-release-outcomes.csv` for the starting-Small cohort.
