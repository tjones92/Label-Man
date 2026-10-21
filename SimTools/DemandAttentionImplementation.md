# Demand and attention: first Stage A implementation

October 21, 2026. Implements the first measurement slice of
[DemandAttentionResearch.md](DemandAttentionResearch.md), section 8.
The follow-up adds actual-launch cohorts, tier and quality strata, and explicit
end-of-window reconciliation. Discovery, prospective inventory, and balance changes
remain separate future work.

## Measurement contract

The existing `release-outcomes.csv` columns retain their names and meanings.
Three columns are appended:

| Column | Meaning |
|---|---|
| `sunkMarketingCost` | Attributed launch and follow-up campaign cost. Empty means unknown; zero means attributed zero. |
| `bookedContributionBeforeMarketing` | `lifetimeLabelNet - sunkProductionCost`. |
| `bookedContributionAfterMarketing` | Before-marketing contribution minus attributed marketing; empty when attribution is unknown. |

The appended contribution fields use round-trip float precision. Existing amounts
remain rounded as before. `realizedNet` already subtracts attributed marketing via
`SunkReleaseCost`; it must not be assumed to exclude marketing in current runs.

These are **booked release contributions**, not cash receipts or fully allocated
profit. The settlement books sold copies, uses label revenue after applicable
distribution, royalty, publishing and commercial cost treatment, and applies
manufacturing commitments. Initial pressing joins sunk production cost; later
pressings reduce lifetime label net. Wholesale collection and plant payment can
occur on different dates. Company overhead, tax, ownership distributions and
artist recoupment are not assigned to individual releases by this audit.

`pressingBookSold` is the pressing cost book's cumulative sold-copy count. It is
not a second sales channel or a shipment/collection ledger. `unitsPressed` minus
`pressingBookSold` measures unsold **booked** copies. It does not identify customer
returns, physical warehouse balances, promotional withdrawals or destruction.
Regional shelf stock remains a separate availability mechanism in the default
model. The opt-in physical inventory arm described below reconciles those models.

The existing `market-clearing-weekly.csv` gains `localClearedSingleUnits` and
`localClearedAlbumUnits`. These are observed immediately after local allocation,
before interregional spillover. For each format, the analyzer reports:

- Local budget binding: serviceable intent exceeds the local format budget.
- Local displacement: serviceable intent minus locally cleared units.
- Spillover fulfillment: final cleared units minus locally cleared units.
- Residual displacement: serviceable intent minus final cleared units.

It validates format totals against local, final, spillover and displaced totals,
and rejects existing inventory/allocation violation counters or nonzero deltas.
Serviceable intent has already passed physical stock and outlet constraints.
Raw demand includes a different rounding/jitter boundary; raw minus serviceable
must not be labeled exact physical lost sales. Backorders include decayed prior
weeks, so they are not a count of this week's unfulfilled purchases.

The existing `settlementDelta` CSV field repeats the regional allocation delta;
this is documented in the report and is not presented as an independent financial
reconciliation. The runner's existing settlement booking checks remain in place.

## Running the audit

Build with `dotnet build 'Label Man.sln'`. Run the normal headless audit or, for a
short check using the game economy:

```powershell
./SimTools/RunCorporateEconomyAudit.ps1 -RunTag demand-stage-a -Profile game -Modes @('default') -Seeds @(1001,2002) -Weeks 26
python SimTools/analyze_demand_attention.py SimLogs/demand-stage-a-default-1001-26 SimLogs/demand-stage-a-default-2002-26 --output SimLogs/demand-stage-a/review.json
python -m unittest discover -s SimTools -p test_demand_attention.py
```

Use a new run tag each time. The analyzer requires the new columns and rejects
older schemas rather than guessing their accounting boundary. Generated output
belongs under `SimLogs/`. No new switch, random draw, saved state or economic
behavior is introduced; only summary counters, appended CSV columns and analysis
are added. Existing stream columns retain their order; whole-file hashes of the
two extended streams will change.

Compact calibration mode now retains the small regional clearing and spillover
summaries; larger diagnostic streams remain suppressed. Market-clearing still
requires the existing genre-market system to be enabled (the game scene enables
it); the analyzer rejects empty coverage when it is disabled. Marketing attribution is
conditional on the existing `--release-all-in-economics` arm. Add that flag when
testing that arm; this audit does not turn it on or impute missing campaign costs.

The existing all-outcome summaries separate format, format/genre and format/memory eligibility,
show zero/under-500/under-1000 booked-sold counts, and use only known marketing
observations for the after-marketing
loss denominator. If launch-campaign telemetry is available, coverage reports how
many observed launches have not finalized. That is a partial observed cohort,
not a census of all releases or proof of eventual outcomes. No final outcomes
produces an empty outcome summary, not a zero loss rate.
Opening/prewarm catalog is included and may have costs outside the observed
window. Memory eligibility provides a separate stratum, not a launch-cohort
definition. When the pressing book is disabled its zero counts cannot establish
zero physical sales.

## Actual-launch cohorts

Two new streams are always available, including compact runs:

- `demand-release-census.csv`: one row per distinct observed record, containing
  its observation kind, audit week, format, genre, observed tier, actual launch
  tier and runtime quality, and final Active/Retired/Missing status. Retirement
  audit week is explicit. Only actual launches have launch tier/quality populated.
- `demand-observation-window.csv`: one row with start/end audit weeks, a normal
  completion marker and opening/launch/active/retired/missing counts. Partial or
  stopped runs are not accepted as completed cohort evidence.

Opening records are the active catalog after initialization or world restoration,
before the first observed tick. They are excluded from launch cohorts even if
their original release date falls in the same calendar year. Observation of new
launches uses `ChartManager.ReleaseRecord`, after registration and initial AI
promotion. A linked LP is observed at its actual drop, not when its project is
scheduled. Orphan, promo and extracted singles, standalone and linked LPs, and
other actual release paths all use that seam. Unreleased project commitments do
not enter the market-entry denominator.

The census stores only scalar snapshots, never runtime object references. It
consumes no random draws, changes no save schema and is not persisted into worlds.
A resumed run starts a new observation window: loaded records are Opening;
absolute audit-week cutoffs must fall inside that window. It does not reconstruct
their original launch attributes from later state. For opening catalog,
`observedTier` is the current observed tier and `launchTier` is empty.

The analyzer checks each census retirement against exactly one finalized outcome,
rejects missing/repeated identities and invalid dates, and reconciles all census
counts with the window. Extra live identities that bypass the observer are counted
as missing. Clearing observations must cover every observed week with a consistent
regional panel and match the census window.

Launch-only groups separate format, launch tier, genre, runtime-quality band and
13-week launch cohort. The quality bands match the research: at most .4, then
(.4,.5], (.5,.6], (.6,.7], and above .7. LP quality is its existing pooled appeal;
single quality is the existing hook/production/danceability weighted value. The
two formats remain separate. These are observed runtime attributes, not a label's
estimate or foreknowledge of commercial response.

Each group counts launches, active records, finalized outcomes and known/unknown
marketing among finalized outcomes. It shows finalized-only loss rates explicitly;
`cohortLifetimeLossBeforeMarketingShare` is null until every selected launch in
that group has finalized. The after-marketing lifetime rate also requires known
marketing for every selected launch. No unfinished record is imputed as profitable
or unsuccessful.

To observe early launches for longer without admitting new launches to the cohort:

```powershell
python SimTools/analyze_demand_attention.py SimLogs/demand-cohort-followup-default-1001-105 SimLogs/demand-cohort-followup-default-2002-105 --launch-through-week 13 --output SimLogs/demand-cohort-followup/early-cohort-review.json
```

This selects launches through week 13 and follows their outcomes through week
105. Census and outcome coverage outside the selection are still reconciled.
Omitting the cutoff selects all launches in the observation window. Older Stage A
runs remain readable with `launchCohorts: null`; a cutoff requires the new census.

## Remaining Stage A work

Still needed before discovery/inventory calibration: screen and calibrate the new
physical inventory arm, add evidence-based return/promo/damage policies, and
attribute collections, obligations, overhead and contract recoupment with explicit
boundaries. Then screen the independent discovery and prospective-inventory arms
on the proposed 105-week horizons. These short verification runs do not establish
lifetime profitability, five-year survival or historical calibration.

## Opt-in physical inventory implementation

`--physical-pressing-inventory` enables a prospective **AI-only** inventory arm;
discovery and the default economy remain unchanged. It requires the pressing book
and genre-market settlement. It is a prototype, not a newly accepted calibration.

- Before a new release's first live sales tick, the label receives its existing
  forecast-sized initial pressing, even if it sells zero copies. Requested shelf
  and held-for-arrival allocations share those copies proportionally, with stable
  largest-remainder rounding. There is no free national shelf allocation.
- Store replenishment draws only from the warehouse. A shortfall orders at least
  the existing minimum/repress-cover lot, enlarged for the combined regional
  request. Production arrives at the start of the following chart week. Stores
  can receive it in that week's replenishment pass, after sales; it can sell from
  the next sales pass. This one-week plant lead time is an assumption, not a
  sourced historical term. No new random draws are introduced.
- Held regional launch stock is already dispatched/reserved. Arrival moves it to
  shelves without manufacturing or invoicing it again. Backorders and existing
  restock eligibility continue to govern requests; missing copies cannot sell.
- The existing settlement invoices initial receipts and later receipts once,
  including zero-sales weeks. Existing plant credit handles cash due dates.
  Orders are unreceived commitments, not stock or invoiced plant payables.
  Retirement waits for committed runs to arrive and be billed before writing off
  the remaining warehouse, shelf and held copies. It cannot erase an unpaid run.
- Explicit operations distinguish saleable unsold dealer returns, damaged dealer
  returns and warehouse promo withdrawals. Saleable returns transfer existing
  copies back to the warehouse. Damage, promos and terminal write-offs consume
  copies without a second manufacturing charge or invented retail sale/refund.
  Corporate inventory value is reduced for physical withdrawals as well. There
  are no new automatic return/damage/promo percentages; those policies need
  evidence and calibration. Consumer refunds and distributor revenue credits
  are not modeled by the unsold-dealer-return operation.
- Opening/prewarm catalog retains existing shelf/held stock as an explicit paid
  opening balance. Adopting an old cost-book save uses an explicit opening balance
  sufficient for its existing stock and booked sales; it is not current production
  or proof that the old model conserved copies. Old opening excess is retained in
  the warehouse. Future releases receive no such adoption credit.
- All inventory counters, unbilled receipts and pending orders persist. The
  regional save adapter now also preserves held arrival stock and arrival state.
  Resume physical worlds with the flag enabled. AI-to-player master transfers
  are rejected before mutation because PlayerDesk has a separate inventory
  ledger; integrating that handoff remains separate work. AI ownership replanning
  redistributes existing units rather than creating stock.

The compact audit adds `physical-inventory.csv` only when enabled, with one row
per initialized record per observed week, including its terminal retirement row.
Counters are cumulative; warehouse/shelves/held/productionOrdered are balances.
`productionReceived` excludes opening stock; orders do not count as receipts.
The runtime and analyzer enforce exact unit identities:

```
openingUnits + productionReceived = unitsPressed
unitsPressed = bookSold + warehouse + shelves + heldForArrival
             + promotionalWithdrawals + damagedCopies + terminalWriteOffs
shipments - saleableReturns - heldReturns - damagedCopies - bookSold
          - distributedWriteOffs = shelves + heldForArrival
```

The analyzer checks weekly continuity, nonnegative integral counts, receipt
billing completion, order dates, terminal balances, census retirement dates and
the final pressing/sold outcome counts. Older/default runs report
`physicalInventory: null`. In this arm the legacy outcome's
`unitsPressed - pressingBookSold` includes withdrawn/destroyed copies; it is not
remaining saleable stock. Use the physical ledger's separate balances.

Run the fixed native checks with `SaveLoadRoundTripRunner.tscn --
--physical-inventory-check`. Run the Python suites with
`python -m unittest discover -s SimTools -p 'test_*inventory.py'` and the existing
`test_demand_attention.py` suite. For a short simulation:

```powershell
./SimTools/RunCorporateEconomyAudit.ps1 -RunTag physical-inventory-check -Profile game -Modes @('default') -Seeds @(1001,2002) -Weeks 26 -ExtraFlags @('--physical-pressing-inventory')
python SimTools/analyze_demand_attention.py SimLogs/physical-inventory-check-default-1001-26 SimLogs/physical-inventory-check-default-2002-26 --output SimLogs/physical-inventory-check/review.json
```

## Verification

- `dotnet build 'Label Man.sln' --no-restore`: succeeded with zero warnings/errors.
- Eight analyzer tests pass, covering rationing versus spillover, missing versus
  zero marketing, unfinished observed campaigns, no finalized observations,
  duplicate rows, nonfinite amounts, broken identities and incompatible schemas.
- `demand-stage-a-final`, game profile, seeds 1001/2002, 26 weeks: both native
  audits completed. The new analyzer reconciled 182 region-weeks per seed and
  417/385 finalized release rows. Singles' local budgets bound in 64.3%/62.1%
  of region-weeks; albums' budgets never bound in this short window.
- Compared with the earlier same-seed `demand-stage-a-native` runs before
  retaining compact clearing rows, all 89 other CSV streams per seed are byte
  identical. This verifies the compact-output change, not equivalence to the
  original research checkout or save/load identity.

Evidence: `SimLogs/demand-stage-a-final/review.json`, its `runs.json`, and
`compact-stream-comparison-{1001,2002}.json`. The finalized rows in these short
default runs have unknown marketing attribution; the before/after-marketing
identity and denominator are covered by the analyzer tests, not by a mature
all-in simulation cohort. Godot crashed at startup inside the sandbox; successful
native runs used execution outside it. No historical acceptance claim follows
from this verification.

### Cohort follow-up verification

The expanded suite has 16 passing tests, adding actual-launch selection,
tier/quality bins, incomplete-cohort null rates, matched retirement dates,
completed-window validation, consistent weekly region coverage, absolute audit
weeks after resume, and unknown marketing in an otherwise completed cohort.
The rebuilt game compiles without warnings or errors.

`demand-cohort-smoke`, seeds 1001/2002 at 26 weeks, reconciles the new census and
all retirement outcomes. All **91 pre-existing CSV streams per seed** are byte
identical to `demand-stage-a-final`. The only additional streams are the census
and its observation window. Evidence is in
`SimLogs/demand-cohort-smoke/stream-comparison.json` and `early-cohort-review.json`.

`demand-cohort-followup`, game defaults, seeds 1001/2002 at 105 weeks, completed
and reconciled with zero missing records. Selecting launches through audit week
13 gives:

| Seed | Early singles finalized/launched | Early LPs finalized/launched | Active early singles/LPs |
|---|---:|---:|---:|
| 1001 | 1,341 / 1,341 | 263 / 299 | 0 / 36 |
| 2002 | 1,250 / 1,254 | 243 / 278 | 4 / 35 |

Only the first seed's early-single cohort is complete: its booked
before-marketing loss rate is 2.68%. The other whole-format cohort lifetime
rates remain null. Marketing attribution is unknown for these default-run
outcomes, so none has an after-marketing lifetime rate. These observations
validate cohort accounting; they do not establish a historical loss target,
five-year survival, fully allocated profit or the behavior of a discovery arm.

Evidence: `SimLogs/demand-cohort-followup/runs.json` and
`SimLogs/demand-cohort-followup/early-cohort-review.json`. Census observation is
not saved; save/load behavioral identity has not been separately established in
this follow-up.
The final guard for live identities absent from the census was rebuilt and
checked with `demand-cohort-guard`, seed 1001 at one week; native execution and
the analyzer both passed (`SimLogs/demand-cohort-guard/review.json`).

### Physical inventory screen

`physical-inventory-smoke` failed: prewarm restock shipped before inventory
opened. After the fix, `physical-inventory-v2` (flag on) and
`physical-inventory-control` (flag off, same assembly `162A0EEB…`), game
profile, seeds 1001/2002 at 26 weeks, both completed; the analyzer reconciled
every unit identity with zero deltas. Eight inventory and 16 analyzer tests pass.

The arm is not behavior-neutral. Against control (1001 / 2002):

| Measure | v2 / control |
|---|---|
| Cleared units, all | 0.77 / 0.78 |
| Cleared singles | 0.77 / 0.76 |
| Cleared albums | 0.77 / 0.88 |
| Spillover cleared | 0.19 / 0.07 |
| Physical backorders | 2.25 / 2.16 |
| Finalized outcomes (retirements) | 914 vs 417 / 909 vs 385 |
| Booked before-marketing loss share | 40.5% vs 4.1% / 37.1% vs 3.6% |
| Label closures in console | 19 vs 12 / 16 vs 8 |

Shortfalls concentrate in new singles' weeks 1-4, where the warehouse is empty
in about 93% of record-weeks. Every shortfall row has an empty warehouse and an
order already pending. Two mechanisms cause it. First, receipts arrive at the
start of a week but replenishment runs after sales, so the effective lead time is
two sales weeks. Second, the launch allocation ships the whole initial pressing,
and the cover lot is three weeks of *sold* units, which a stockout has already
suppressed. `unfilledShipmentRequests` counts the same backlog again on each
weekly request; it is not a count of distinct lost copies.

The higher loss share is not yet evidence of better historical calibration. It
comes from an unsourced lead time and a sales-sized reorder rule. Before
calibration, the reorder signal and receipt timing must be screened separately.

#### Reorder and receipt-timing variants

Two screening switches require `--physical-pressing-inventory` and are off by
default. `--physical-inventory-demand-lot` sizes the cover lot from this week's
sold copies plus accumulated shortfall. `--physical-inventory-early-receipt`
ships a receipt to reached regions' backlogs during `BeginWeek`, before sales.
It allocates pro rata with largest-remainder rounding and never exceeds a
region's backlog. Neither switch adds a random draw or saved field. Both runs
(`physical-inventory-demandlot`, `physical-inventory-earlyreceipt`; seeds
1001/2002, 26 weeks) passed native execution and every analyzer identity.

| Arm (1001 / 2002) | Cleared vs control | Retired outcomes | Loss share | Closures |
|---|---|---|---|---|
| control | 1.00 / 1.00 | 417 / 385 | 4.1% / 3.6% | 12 / 8 |
| v2 | 0.77 / 0.78 | 914 / 909 | 40.5% / 37.1% | 19 / 16 |
| demand lot | 0.81 / 0.82 | 949 / 883 | 36.2% / 36.9% | 14 / 17 |
| early receipt | 0.90 / 0.86 | 988 / 929 | 38.1% / 33.4% | 16 / 16 |

Early receipt recovers about half of the lost units. Demand-sized lots recover
about one sixth. Neither changes retirement or loss. Retired opening records
are similar in all arms (207-237). Retired *launched* singles rise from 180
(control, seed 1001) to 696-759, and they retire younger (median 15-16 weeks
versus 20).

The dominant mechanism is launch stock. `InitialCopies` is
`max(500, 0.5 x label forecast)`, and 78% of launched singles receive exactly
500 copies. In the default model this figure prices the plant bill only;
launch shelves are stocked separately and without charge. In this arm it is
the only launch stock. `AllocateLaunch` scales shelf requests down to it
without recording the cut as unfilled. A quarter of retired launched singles
sell exactly that pressing (224 of 244 small sellers pressed 500, mostly
Independent/Small/Boutique). Uncharted low-demand records then fail the restock
gates, sit below the retirement floor for five weeks, and retire with the plant
bill unrecovered.

The arm therefore cannot be calibrated until launch pressing is chosen as a
stocking decision rather than a reused cost proxy. The likely forms are either
the launch shelf request or a demand-based forecast. That choice, not lead
time, determines most of the gap from control.

#### Shelf-launch pressing

`--physical-inventory-shelf-launch` presses
`max(InitialCopies, launch shelf + held request)`. Shelves receive their full
request, and any forecast excess stays in the warehouse.
`physical-inventory-shelflaunch` ran it with early receipt on seeds 1001/2002
at 26 weeks. Both runs passed, and the analyzer reconciled every identity.

| Arm (1001 / 2002) | Cleared vs control | Backorders (M) | Retired | Retired launched singles | Loss share | Launched-single loss | Closures |
|---|---|---|---|---|---|---|---|
| control | 1.00 / 1.00 | 18.7 / 19.2 | 417 / 385 | 180 / 167 | 4.1% / 3.6% | 9.4% / 8.4% | 12 / 8 |
| early receipt | 0.90 / 0.86 | 35.8 / 34.3 | 988 / 929 | 759 / 722 | 38.1% / 33.4% | 49.5% / 42.9% | 16 / 16 |
| shelf launch + early receipt | 0.95 / 0.91 | 22.3 / 22.3 | 409 / 377 | 190 / 160 | 12.0% / 9.8% | 25.8% / 23.1% | 22 / 20 |

Retirement count and age (median 19-20 weeks) return to control. Units sold
reach 91-95% of control. The remaining loss share comes from pressed copies
that do not sell: sell-through of new releases' pressings falls from 75-79% to
62%. Closures rise above every other arm because labels now pay to manufacture
the whole launch stock.

The median launch request is **18,600-19,800 copies per release**. The
cost book's forecast pressing is 500. The default model stocks those shelves
without a plant bill, so most of a release's launch distribution is never
manufactured or paid for. This likely contributes to the default model's
~4% booked loss share; see the earlier loss-rate audit. Whether ~19k is a
plausible 1960 launch order, especially for Independent/Small labels, is the
next calibration question. It determines whether the request or the forecast
should change.

#### Launch order versus period evidence

Model launch request per single, by actual launch tier (shelf-launch run, both
seeds, 26 weeks):

| Tier | n | p10 | Median | p90 |
|---|---:|---:|---:|---:|
| Small | 831 | 8.0k | 10.7k | 15.1k |
| Boutique | 685 | 14.4k | 20.0k | 29.9k |
| Independent | 1,631 | 17.1k | 24.0k | 36.5k |
| MidTier | 172 | 28.5k | 39.6k | 52.0k |
| Major | 203 | 58.0k | 69.3k | 84.4k |

Period evidence found (no representative sample exists; confidence low):

- First runs of small/regional singles: custom pressings of 200-1,000 copies
  (EarlyGameLabelManImplementationNotes); a 1967 participant account of an
  initial 500, of which 300 were promos, followed by a reorder (research §3).
- The 1959 Billboard manufacturing quote is priced at 5,000-copy quantities.
  That is a price break, not a typical order (research §4).
- Secondary histories: independents profited on records selling "a few
  thousand" copies.
- Strong *regional successes*, as lifetime sales, not launch orders:
  "Louie Louie" (1961) ~15k in the Northwest; Rufus and Carla Thomas (1960)
  ~40k regional; 1966 regional hits 25-50k each.
- Superstar majors: RCA shipped 1.3M copies of "Stuck On You" (1960) to
  wholesalers. That is the ceiling, not a typical major release.
- Aggregate: ~190M singles (1960, unverified) over 5,797 reviewed singles is
  a mean near 33k per title, which is dominated by hits.

**Verdict.** The model's Small, Boutique and Independent launch requests
(11-24k median) exceed every sourced *lifetime* figure for non-hit small-label
singles. They resemble strong regional successes, not first runs. The
period pattern is a first run of a few hundred to a few thousand copies, then
reorders. MidTier and Major medians (40-70k) are plausible only for established
acts, not as a typical release.

The request is not a stand-alone error. Shelf stock is a demand device
(SmallLabelAttritionAndChartMethod §5), and the model's demand matches the
request. Singles launched in weeks 0-4 sold a median of 13.6k / 15.4k copies by
week 26; only ~1% sold under 2,000 and 9-12% under 5,000. The per-title mean
(~43k, top 10% = 64-68% of units) is in line with the period aggregate, but the
bottom of the distribution is missing. Cutting the launch order alone
reproduces v2: lost sales and early retirement. Pressing the request
reproduces shelf launch: paid stock for median sales that are already too
high. A period-plausible arm needs both changes together: decouple
shelf presence from the copies in stores, and move the median non-hit single
toward low thousands, leaving the hit tail and aggregate units intact. Then a
forecast-sized first run (500 to a few thousand) plus reorders becomes the
stocking rule. This is the same demand floor identified by the loss-rate
audit, now seen from the inventory side.
