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

## Commercial fit and first-run stocking (opt-in, 2026-10-21)

Two opt-in probes address the verdict above together. Both are off by default.
Off, the multiplier is exactly 1 and no stock or restock path changes. They add no
global random draws or saved state. `SaveLoadRoundTripRunner.tscn --
--demand-stocking-check` passes 19 native checks.

**`SingleCommercialFit`** (`Systems/SingleCommercialFit.cs`) multiplies a live-launched
AI single's purchase conversion by a persistent record response. It does not apply
to opening catalog or player records. A counter RNG keyed by record (national) and
record x region (regional) draws the response. Labels' priors, pressing forecasts and
release judgement never read it. The national factor is *kinked*: only
`hit-share` of reference launches connect. Below the kink the response falls with
`sigma`; above it, it rises only with `upside` (default .15), and `floor` bounds it
from below. Launch awareness (artist recognition, campaign, label standing) and the
label's campaign push both move the kink, weighted by `launch-weight` and
`push-weight`. A stronger launch connects more often and expects more. One
normalizer gives the reference launch (awareness .2, push .35) mean one, and `scale`
renormalizes aggregate units for chart feedback.

**`LaunchStocking`** (`Systems/LaunchStocking.cs`) scales an AI single's launch shelf
allocation by `--launch-stock-share`. It keeps the regional shape, with at least 25 copies in any
reached region. `--sell-through-reorders` lets dealers reorder any AI single that
ran out while buyers were still asking (backorder and raw demand both positive),
charted or not, through the ordinary restock service and capacity limits.

### Shape screen (26 weeks, seeds 1001 / 2002, `SimLogs/dfit-*`)

| Shape | Early-single median | Under 2k | Major-family share | Single units vs control |
|---|---:|---:|---:|---:|
| control | 14.2k / 15.3k | 1.6 / 1.4% | .60 / .62 | 1.00 |
| lognormal sigma 1.5 | 3.3k / 4.3k | 39 / 35% | .41 / .33 | 1.08 / 1.06 |
| kinked sigma 2.5, hit share .15 | 3.8k / 3.7k | 42 / 43% | .51 / .38 | 1.06 / 1.08 |
| same, per-record mean one, launch weight 4 | 6.5k / 6.0k | 31 / 29% | .47 / .40 | 1.00 / 1.04 |
| same, one normalizer, weights 2 / 2 | 5.3k / 6.7k | 34 / 31% | .67 / .71 | 1.06 / 1.07 |
| same, weights 1.5 / 1.5, scale .93 | 5.4k / 6.3k | 34 / 33% | .68 / .64 | 1.05 / 1.07 |

Two shapes were rejected. A symmetric lognormal's long tail makes rare records from
the many small releases outsell Major campaigns: Major chart-unit share fell from
.39-.44 to .18-.30. Normalizing each record to its own mean does the same for weak
launches, which then sell 1/p times more when they connect. Without launch
weighting, the kinked shape also loses Major-family share: in the control nearly
every Major single charts, and a luck-only "connects or not" draw sinks 85% of them.
With one normalizer and weights near 1.25, Major-family share sits within a few
points of control.

Stocking (26 weeks): cutting launch shelves to 10% *without* the fit leaves sales
unchanged when sell-through reorders are on (early median 14.6k / 15.2k). The launch
order is then no longer a demand device. Without reorders, an uncharted single
that sells out its free launch stock can never be restocked, falls below the
retirement floor and leaves: about 180 launched singles per seed in the control.

### Candidate, 105 weeks (`SimLogs/dlong-{ctl,cand,phys,soft}`)

Candidate flags: `--single-commercial-fit=2.5 --single-commercial-fit-hit-share=0.15
--single-commercial-fit-regional=0.5 --single-commercial-fit-scale=0.88
--single-commercial-fit-launch-weight=1.25 --single-commercial-fit-push-weight=1.25
--single-commercial-fit-floor=0.12 --launch-stock-share=0.1 --sell-through-reorders`.
`phys` adds `--physical-pressing-inventory --physical-inventory-shelf-launch
--physical-inventory-early-receipt`. `soft` uses sigma 2.0, hit share .25, floor .15,
scale .9.

| Seed 1001 / 2002 | Control | Candidate | Candidate, physical | Soft |
|---|---:|---:|---:|---:|
| Early-launched single, median units | 14.5k / 15.6k | 8.3k / 8.3k | 8.1k / 9.0k | 11.1k / 11.6k |
| p10 units | 4.9k / 5.5k | 650 / 550 | 610 / 625 | 1.1k / 1.0k |
| Under 2k | 1.6 / 1.4% | 26 / 27% | 24 / 24% | 18 / 17% |
| Early cohort (launched by week 13) loss share, before marketing | 2.7 / 2.5% | 40.7 / 41.2% | 41.2 / 36.6% | 27.8 / 26.5% |
| Early cohort under 500 copies | 8 / 3 | 156 / 171 | 148 / 128 | 85 / 90 |
| Median copies pressed, retired launched single | 17.1k / 17.6k | 3.8k / 3.7k | 5.8k / 6.1k | 8.0k / 8.3k |
| Median launch stock, charted singles | 50k / 49k | 4.7k / 4.4k | 4.7k / 4.4k | 4.9k / 4.8k |
| Single units vs control | 1.00 | 1.00 / 1.03 | 1.02 / 1.03 | 1.00 / 1.02 |
| LP units vs control | 1.00 | 1.05 / 0.88 | 0.88 / 0.83 | 0.99 / 0.95 |
| Chart-week share of single units | 54 / 56% | 72 / 77% | 75 / 77% | 67 / 70% |
| Mean #1 weekly units | 84k / 77k | 107k / 149k | 126k / 157k | 109k / 124k |
| Major-family chart share | .51 / .52 | .53 / .58 | .55 / .52 | .52 / .61 |
| Distinct Hot 100 acts | 915 / 906 | 867 / 826 | 808 / 790 | 867 / 844 |
| Starting-Small closed by week 105 | 13.0 / 9.1% | 26.0 / 19.1% | 28.6 / 21.7% | 21.2 / 16.5% |
| Starting Boutique / Independent closed | 20 / 4%, 19 / 10% | 33 / 12%, 30 / 18% | 31 / 19%, 31 / 25% | 23 / 17%, 26 / 15% |

What it fixes: the missing bottom (a quarter of singles under 2k, p10 in the
hundreds), first runs of a few thousand copies instead of 50k shelves, pressing per
release down ~4.5x, a loss share in the historical direction, and the hit tail.
The #1's weekly units and the chart's share of single units both rise, and aggregate
single units hold. (The ~150k #1 and ~88% chart-week share once used as targets here are
not historical evidence; DemandAttentionResearch.md withdraws both.) The physical arm reconciles
every unit identity (`SimLogs/dlong-phys/review.json`). Its backorders are about
1.5x the candidate's, and it is not needed to reach the result.

What it breaks: **starting-Small closures double at two years.** Five-year control is
29-32% against a 20-35% band, so this would overshoot. Boutique and Independent
closures also rise. Softening the shape (`soft`) recovers only a few points: the
closures come from flop income disappearing at all, not from how deep the flops go.
This is the same finding as the loss-calibration audit. Distinct Hot 100 acts fall
5-9%. LP units fall in three of four non-control runs (promo synergy reads promo
singles' chart peaks; unconfirmed).

**Not adopted.** Before any default change this needs a small-label survival path
that is not flop income (cost per release scaled to expected sales, a regional-hit
route, or release judgement that learns the new distribution), an LP-unit check,
and a 265-week closure run. No historical acceptance claim follows from these short
screens.

### Small-label survival paths (opt-in, 2026-10-21)

**Why the candidate closes labels.** Every closure is a bankruptcy (cash below zero at
two month-ends). A closed starting-Small label made a median 1-2 releases in its life,
whose best record sold 134-925 copies. Survivors made about 10. Its first single flops,
cash falls below the release reserve, and the ~$200 a month of fixed overhead
(premises ~$110, retainers ~$55, plus payroll) runs it out. A Small single costs ~$486
including its 500-copy first run, which is period-plausible. The issue is ruin, not
price: about 1 in 10 Small launches connects, and ~$2k of opening cash buys 4-5 tries.

Three opt-in routes were built. All are off by default, and 27 native checks pass
(`--demand-stocking-check`).

- **`--home-operated-micro-labels`** (`LabelCorporateModel`). A founder-run Small,
  Boutique or Independent firm with no paid staff works from home, shop or car: no
  leased suite and no standing retainers. Hiring anyone leases an office as before, and
  a suite already leased is paid until its annual review.
- **`--ai-master-deals`** (`Systems/AiMasterDeals.cs`). The player's master routes
  (EarlyGameLabelManDirectiveV2 §2, §5), with the same buyer ear, bars, prices and
  terms, open to AI Small/Boutique/Independent sellers and bigger AI buyers.
  *Shop the master* (Gordy's Miracles masters leased to End): a label that can pay a
  session but not pressing and promotion cuts the single and offers it to up to four
  fitting offices. The first that hears it offers a $150-600 lease against 7-8% of list
  for 52 weeks, or a sale at 2.5x when it likes it a lot; a cash-short seller takes the
  sale. *Regional pickup* (Tamla's "Come to Me" to United Artists, $3,000 against
  royalties): a new breakout market may bring a call, using the player's chance and one
  keyed draw per evidence state. The lease is priced from the evidence (up to $1,500),
  and the live record changes hands with its chart history. Small and Boutique sellers,
  and Independents without three months' overhead, let it go. Lease statements are
  quarterly, and the master comes home at the end of the term. Artists stay on the
  seller's roster and publishing is never included. Telemetry is `AI_MASTER_DEAL`
  console lines.
- **`--ai-backers`** (`Systems/AiBackers.cs`). The player's one backer agreement (§4.3;
  Gordy's $800 Ber-Berry loan at 6%) for Small and Boutique founders. Each label's source
  is a keyed draw over the player's four, with capacities of $400-800. When a recorded
  single can't be pressed and promoted, the founder asks once. Terms are the player's:
  6% simple interest, twelve installments from the third 1st after signing, paid from
  cash on hand only, with arrears. It is tried before shopping the master, since
  borrowing keeps the master. Telemetry is `AI_BACKER` console lines.

105 weeks, seeds 1001 / 2002, candidate flags as above. Closures are of starting labels.
`own .5` adds `--single-commercial-fit-own-normalization=0.5`, which normalizes half of
each launch's own mean in log space.

| Arm | Small closed | Boutique | Independent | Small survivor median cash |
|---|---:|---:|---:|---:|
| control | 13.0 / 9.1% | 20.3 / 18.5% | 3.9 / 9.6% | $6.9k / $7.4k |
| control + home | 0.0 / 0.0% | 11.7 / 12.3% | 8.7 / 11.2% | $7.1k / $3.4k |
| control + master deals | 9.5 / 7.8% | 17.2 / 15.1% | 11.1 / 10.2% | $6.7k / $7.6k |
| candidate | 26.0 / 19.1% | 32.8 / 30.1% | 11.6 / 17.8% | $7.0k / $7.2k |
| + home | 0.4 / 0.0% | 20.3 / 13.7% | 14.5 / 16.8% | **$458 / $1.9k** |
| + master deals | 22.5 / 12.2% | 32.0 / 28.8% | 13.0 / 17.8% | $6.6k / $7.7k |
| + master deals + backers | 24.7 / 14.3% | 29.7 / 28.8% | 19.3 / 19.8% | $6.4k / $6.3k |
| + master deals + backers, own .5 | 18.2 / 12.2% | 28.1 / 24.0% | 16.9 / 19.3% | $7.2k / $6.8k |

Deal volume per two years: 345-452 regional pickups (almost all leases), 8-33
pre-release masters shopped, 16-41 leases reverting. Backers: 34-46 signed, 8-9 repaid.

Readings:

- **Home operation:** fixed overhead accounts for *all* of the control's Small
  closures. Removing it under the candidate leaves zombies: median survivor cash of a
  few hundred dollars, and 23-30% of survivors with no release in a year.
- **Master deals:** they help, but mostly labels that already hold a breaking record.
  Zombies have nothing to sell and can't fund a session to shop.
- **Backers:** a loan mostly pays for one more flop.
- **The larger driver is the fit's push weighting.** With one reference normalizer it
  moves a fifth to a third of Small, Boutique and Independent units to Majors (Major
  units +59 / +62%). `own .5` removes that transfer (Major +8 / +3%), but Major-family
  chart share falls to .41 / .43 against .51 / .52. Small-launched units double, partly
  because pickups carry records to bigger labels, whose push then lifts the same fit
  draw.

Every setting so far trades Major-family chart share against small-label survival. A
choice between them, or a third lever, is a calibration decision; none of these arms
is adopted.

### What the shares should be, and which part of the closure rise is error (2026-10-21)

**History, 1960-64.** Top 10 weekly slots from the project's reconstructions
(`SimLogs/historical-top10-<year>/records_<year>_grouped.csv`), grouped by owner. Big-5 =
Columbia (with Epic), RCA, Capitol/EMI, Decca/MCA, Mercury (with Philips and Smash); film and
broadcast majors = MGM, ABC-Paramount, Paramount/Dot, Warner, United Artists and Colpix.

| Year | Big-5 | Film/broadcast majors | Independents | of which 20+ slot national indies | of which firms under 10 slots |
|---|---:|---:|---:|---:|---:|
| 1960 | 35.6% | 21.2% | 43.3% | 5.4% | 23.7% |
| 1961 | 22.1% | 16.9% | 61.0% | 17.9% | 20.2% |
| 1962 | 26.0% | 16.7% | 57.3% | 15.8% | 25.8% |
| 1963 | 38.8% | 17.3% | 43.8% | 9.4% | 17.9% |
| 1964 | 34.4% | 11.9% | 53.7% | 23.7% | 17.5% |

Sales: the FTC's 1960 figures give the same five companies 54.4% of all record dollars and 61.9% of
LP dollars, which implies about **32% of singles dollars** (24-37% for a 70-80% LP mix). There is no
sourced split of singles sales below that level, and no sourced closure rate for small labels. The
20-35% five-year band is a design judgment.

**Model, first two years (seed 1001 / 2002; Major tier = the Big-5 plus EMI and Decca UK).**

| | Control | Candidate + routes | Candidate + routes, own .5 |
|---|---:|---:|---:|
| Major share of Top 10 slots, 1960 | 40.6 / 47.1% | 43.8 / 44.8% | 40.2 / 39.0% |
| Major share of Top 10 slots, 1961 | 34.6 / 29.0% | **52.7 / 41.9%** | 36.2 / 26.2% |
| Major share of single units | 21.8 / 20.4% | **33.8 / 31.0%** | 23.4 / 20.6% |
| Small share of single units | 7.1 / 7.0% | 7.4 / 8.5% | 13.6 / 13.9% |
| Top 10 slots held by firms under 10 slots, 1961 | 45.4 / 51.7% | 27.1 / 36.9% | 40.8 / 49.4% |

Readings:

1. **The control already gives Majors too much chart and too little sales.** Its Top 10 share is
   5-13 points over the Big-5 history, while its singles unit share (~21%) sits below the FTC
   residual (~32%). Majors earn singles sales only through chart hits. History's majors also sold
   steady catalog, country, standards and specialty singles off the chart. The model lacks that
   base, so matching their sales share would require overstating their hits.
2. **The candidate reaches the FTC sales share by doing exactly that.** Its Major units match ~32%,
   but its 1961 Top 10 share is 42-53% against 22% historically. The 20-35% loss of Boutique and
   Independent units that goes with it is **error**: it comes from the push-weighted fit, not from
   any historical shift. The film/broadcast majors (12-21% of history's Top 10) have no counterpart
   in the model.
3. **The Small-label rise is mostly the natural part.** Small labels' total unit share is unchanged
   under the candidate (7-8.5%); the tier's income is simply concentrated in fewer labels, as a
   hit-driven market should do. But all of the control's Small closures, and the zombie state when
   it is removed, come from ~$200 a month of fixed overhead that period micro-labels did not carry.
   Closure counts also cannot separate a bankruptcy from a founder who stopped releasing.
4. **Missing mechanisms**, in order of evidence: (a) a non-chart Major singles base (catalog and
   specialty volume) so sales share does not require chart dominance; (b) a film/broadcast major
   tier or equivalent second rank; (c) a dormancy exit distinct from bankruptcy, with
   founder-scale fixed costs; (d) a measured Hot 100 entry split. The recovered Hot 100
   (`SimLogs/historical-hot100-1960-64`) has no labels yet.

### Majors' off-chart singles base (opt-in, 2026-10-21)

**Evidence.** No source splits the Majors' singles sales between charted and uncharted titles, so the size
of this base is a calibration choice, not a measurement. The channels are well documented: catalog reissue
lines (RCA's Gold Standard series from the mid-1950s, Columbia's Hall of Fame), country sold through country
outlets and operators' country boxes (Billboard's C&W charts were built from those operators, not from the
pop survey), standards and MOR for adult buyers, and children's, polka, Latin and sacred lines. Jukebox
operators bought about 47M records in 1959 (Billboard, 1960-05-09) and programmed them by location rather than
from the Hot 100. Rachlin's industry series gives 600M singles for 1960. That differs from the unverified
~190M used above, and neither was produced by the same survey. The target is the FTC residual (~32% of
singles dollars for the Big-5), with Top 10 share held near the 1960-64 history (22-39%).

**`--major-offchart-singles=<share>`** (`Systems/MajorOffChartSingles.cs`, booked in
`CompetitorManager.SettleOffChartSingles`). Each week, every active AI Major without a distribution deal
sells a share of that week's settled live single units, split by national reach with largest-remainder
rounding. The copies are booked at the ordinary AI single terms: price, plant cost, mechanicals, the default
5% royalty on list, own-reach skim and corporate selling costs. They are pressed as they sell, never
deferred to a wholesale house, and belong to no record. So they never enter chart points, release outcomes,
pressing forecasts or release priors. Volume is added on top of the cleared market and does not compete for
the regional single budget. The off-chart base adds no random draws and no saved state. The audit writes
cumulative per-label totals to `offchart-singles.csv`; a resumed world restarts them from zero. Off, nothing
is allocated, and `doc-inert` (the push-weighted candidate rerun with the new build) is byte-identical to
`dlong-candmdb` in `records.csv` on both seeds. The native checks now number 32, and all pass
(`--demand-stocking-check`).

**Screen** (105 weeks, seeds 1001 / 2002). Arms: `doc-nopush` is the candidate with master deals and
backers but with `--single-commercial-fit-push-weight` dropped. `doc-np-oc15` adds
`--major-offchart-singles=0.15`. `doc-ctl-oc12` is the control plus `0.12`. Shares use labels' initial
tier, as in the table above.

| Seed 1001 / 2002 | Control | Control + base .12 | Candidate, push 1.25 | Push dropped | Push dropped + base .15 |
|---|---:|---:|---:|---:|---:|
| Major share of single units, incl. off-chart | 21.8 / 20.4% | 32.9 / 29.6% | 33.8 / 31.0% | 21.5 / 25.4% | **34.5 / 29.6%** |
| of which live records | 21.8 / 20.4% | 24.9 / 21.2% | 33.8 / 31.0% | 21.5 / 25.4% | 24.7 / 19.0% |
| Major Top 10 share, 1960 | 40.6 / 47.1% | 47.7 / 43.3% | 43.8 / 44.8% | 37.7 / 37.5% | 40.8 / 31.9% |
| Major Top 10 share, 1961 (history 22.1%) | 34.6 / 29.0% | 46.2 / 32.5% | 52.7 / 41.9% | 31.0 / 41.5% | **38.8 / 27.1%** |
| Chart-week units / all single units | 54 / 56% | 49 / 50% | 75 / 76% | 73 / 76% | 64 / 65% |
| Mean #1 weekly units | 84k / 77k | 69k / 84k | 127k / 133k | 103k / 129k | 104k / 120k |
| Starting Small closed | 13.0 / 9.1% | 14.7 / 9.6% | 24.7 / 14.3% | 22.1 / 13.9% | 20.8 / 10.4% |
| Starting Boutique closed | 20.3 / 18.5% | 20.3 / 16.4% | 29.7 / 28.8% | 21.1 / 31.5% | 27.3 / 24.7% |
| Starting Independent closed | 3.9 / 9.6% | 7.7 / 7.6% | 19.3 / 19.8% | 19.3 / 18.3% | 15.9 / 15.7% |
| Major single launches | 554 / 427 | 598 / 440 | — | 562 / 449 | 599 / 445 |

Readings:

1. **The decoupling works.** Without the push weight and with a .15 base, Majors reach the FTC residual
   (34.5 / 29.6%) while their 1961 Top 10 share falls 14 points below the push-weighted candidate. Seed 2002
   reaches the historical level; seed 1001 is still 17 points over it. About 50M off-chart copies over two
   years, roughly 6M per Major, carry the difference.
2. **Dropping the push weight alone neither raises Major sales nor protects small labels.** Majors stay at
   21-25% of units, and Small/Independent closures barely move from the push-weighted candidate. Both readings
   are within seed noise. The closure rise belongs to the fit's flop distribution (see "Small-label survival
   paths"), not to the transfer to Majors.
3. **Side effects.** The base gives Majors more cash, and they release 3-8% more singles. In 1001 that lifts
   Major Top 10 share in both base arms (control 34.6 → 46.2% in 1961), so part of the chart transfer returns
   through cash. Seed 2002 does not show it. Off-chart volume is additive: total single units rise 15%, and the
   chart's share of units falls to 64-65%. There is no sourced target for that share
   (DemandAttentionResearch.md withdrew the ~88% figure), but an unheld rise in total units is still a
   distortion. Renormalizing (the fit's `scale`,
   or taking the base out of the regional single budget) is the next calibration step. Boutique labels' unit
   share collapses from 13-16% to 3-4% in every fit arm, with or without the base, and needs separate
   investigation.
4. **Single-seed differences between arms are noise-sized.** Turning the base on reshuffles the RNG stream
   (one conversion reshuffles the RNG stream), and the two seeds disagree in sign on several rows. Adoption
   needs three or more seeds and a 265-week closure run.

Not adopted. Next: hold total single units (renormalize, or carry the base inside the regional budget),
re-screen on three seeds, and decide whether to scale the base by catalog depth (opening catalog and
lifetime hit units) instead of equal reach. Equal reach treats every Major's catalog as the same size.

#### Holding total single units (three seeds)

`--major-offchart-singles-hold-units` (requires the base) takes the off-chart copies out of the same
buyers. A first version scaled every live AI single's purchase conversion by 1 / (1 + share) (`doc-np-oc15h`).
It changed nothing: live single units are **budget-bound**. In `doc-nopush` seed 1001, the regional single
budget binds in 93% of region-weeks (control 90%), and serviceable single intent is 668M against a 297M budget.
A demand cut is absorbed by rationing. The hold now shrinks each region's single budget by 1 / (1 + share), and
its purchase capacity by the same copies. Spillover therefore cannot reclaim them; this mirrors how LP budget
retrenchment is already handled (`ChartManager` market clearing). 35 native checks pass.

105 weeks, seeds 1001 / 2002 / 3003 (`doc-ctl3` and `doc-np3` add seed 3003 to the earlier arms). Push weight
dropped in every arm except the control. The cells show seed values, then the mean.

| | Control | Push dropped | + base .15, conversion hold | + base .15, budget hold |
|---|---:|---:|---:|---:|
| Live + off-chart single units (M) | 330 | 334 | 381 | **339** |
| Major share of single units, incl. off-chart | 21.8 / 20.4 / 22.4 → 21.5% | 21.5 / 25.4 / 22.2 → 23.0% | → 36.1% | 37.9 / 30.8 / 34.0 → **34.3%** |
| of which live records | 21.5% | 23.0% | 26.5% | 24.4% |
| Major Top 10 share, 1960 (history 35.6%) | 43.7% | 37.4% | 40.1% | 45.8 / 33.5 / 32.1 → 37.1% |
| Major Top 10 share, 1961 (history 22.1%) | 32.5% | 31.0 / 41.5 / 25.4 → 32.6% | 40.6% | 49.4 / 30.6 / 38.8 → **39.6%** |
| Chart-week units / all single units | 56% | 75% | 65% | 65% |
| Starting Small / Boutique / Independent closed | 12 / 19 / 7% | 19 / 26 / 18% | 16 / 23 / 20% | 19 / 26 / 14% |

The chart-week share is a model diagnostic only: DemandAttentionResearch.md withdrew the ~88% target.

Readings:

1. **The budget hold holds units.** Total single units are within 2% of the push-dropped run. With the hold,
   live single units fall 12% and the off-chart base supplies about 44M copies. Majors reach 34% of single units
   (2 points over the FTC residual; a share near .12 should land on it).
2. **The chart transfer partly comes back through cash.** Majors' 1961 Top 10 share rises 7 points on the mean
   (1001 +18, 2002 −11, 3003 +13). The base's cash is spent: Major releases rise 4-8% (singles
   562/449/487 → 599/438/508; LPs up 2-16%), and campaign spending rises 3-13%. The largest rise in both
   spending and chart share is in seed 1001. The mean still sits 13 points below the push-weighted
   candidate's 1961 share (47.3%, two seeds), but 18 points above history.
3. **Small labels.** Independent closures fall 4 points against push-dropped; Small and Boutique closures do
   not move. As before, the fit's flop distribution, not the transfer to Majors, sets them.
4. **Boutique unit share** stays at 3-4% in every fit arm against 13-16% in the control; that is unrelated to
   this base.

Not adopted. Open choice: let catalog income fund Major A&R and promotion (it historically did, and the chart
feedback then needs its own brake), or book the base outside the cash that release and campaign budgets read.
Either way, re-screen at a share near .12.

#### Where the Majors' catalog cash goes (trace, 2026-10-21)

**Decisions that read a Major's cash or revenue** (AI release, campaign and growth paths):

| Decision | What it reads | Binds for a Major? |
|---|---|---|
| Weekly release chance (`CalculateLabelReleaseCapacityChance`) | status multiplier: Rising 1.2, Stable 1.0. Under `EconomicLabelLifecycle`, Rising means trailing monthly cash flow > 2x overhead | **Yes**: the only direct cash-to-volume link |
| Organic roster-target growth (`LabelLifecycleManager`) | Stable/Rising, last month profitable, 6 months' runway, plus charting | Weakly: Majors pass almost always |
| Single and LP funding (`TryRelease…`, `TryClampFirstEventCost`, promo extraction) | cash above commitment reserve + first run | No: reserve floors bind only cash-poor labels |
| Launch campaign size (`GetMarketingBudget`) | tier x career state x marketingPower; cash only through the reserve clamp | No |
| Follow-up tranches (`ProcessRecordCampaigns`) | cash above reserve months | No |
| Staff hiring (`ReviewOrganization`) | cash above protected + 3 months' payroll | No: coverage is ~.93-.95 in both arms |
| Production upgrades, distribution reinvestment | recent cash flow, profit | Inactive: Majors' capital spending is zero in both arms |
| Owner distributions | 65% of undistributed earnings each quarter for a corporate division, to an owner outside the model | Yes: a sink |

**Measured** (initial-tier Majors, 105 weeks, means of seeds 1001/2002/3003; `doc-np3` and `doc-nopush`
against `doc-np-oc15b`):

| | Push dropped | + base .15, budget hold | Change |
|---|---:|---:|---:|
| Off-chart net after selling costs | — | $8.64M | +8.64M |
| Tax paid | $19.6M | $24.4M | +4.78M (55%) |
| Owner transfers out | $9.9M | $11.9M | +2.03M (23%) |
| End cash | $8.4M | $10.0M | +1.63M (19%) |
| Recording + campaigns + advances | $6.08M | $6.46M | +0.40M (5%) |
| Payroll | $6.56M | $6.85M | +0.29M (3%) |
| Rising share of Major label-weeks | 50.9% | 61.5% | +10.6 pts |
| Single / LP launches | 499 / 247 | 515 / 271 | +3.1% / +9.9% |
| Campaign spend per launch | $2,654 | $2,688 | +1.3% |
| Organic roster-target increases | 45.7 | 48.3 | +2.7 |

The percentages are shares of the off-chart net. The columns do not close exactly, because live
revenue also changed between arms.

Readings:

1. **About 5% of the catalog money reaches A&R and promotion.** The rest is taxed, paid to the outside
   owner or held as cash, which is roughly what a period corporate division did with it.
2. **The volume link is the Rising flag.** Catalog cash flow lifts Majors over the 2x-overhead bar more often
   (+10.6 points). That predicts about +2% release opportunities; organic roster growth adds about another
   2%. Observed singles rose 3%. LPs rose 10%, more than this explains, and are not yet traced. Campaign
   spend per launch is flat: campaign spend rises only with volume.
3. **The +7-point Top 10 rise is within noise.** Across the three push-dropped seeds, Major 1961 Top 10 share
   ranges 25-42% (SD about 8 points). The standard error of a three-seed mean difference is about 6.5 points,
   and one seed moved the other way (−11). The rise in Majors' live unit share (23.0 → 24.4%) is likewise about
   one standard error. The trace found no mechanism large enough to produce 7 points: about 3% more singles
   at unchanged campaign size.

Implication: no sink is needed to stop catalog cash from buying chart share. If anything is changed, the
Rising flag is the place: it reads total cash flow, so a profitable back catalog counts as A&R momentum.
Settling the chart effect needs more seeds, not a sink.

#### Rising decoupled, base .12, six seeds (2026-10-21)

The status review's Rising bar (trailing three-month cash flow above twice overhead, x1.2 release chance)
now excludes the base. `MajorOffChartSingles` keeps each label's base net, after selling costs and before
tax, in the same three-month window, and `EconomicLabelLifecycle.ReviewMonth` subtracts its mean before the
Rising test. Solvency tests (Dying, Struggling) still count it. Subtracting pre-tax net removes slightly more
than the base adds to cash flow. The window is not saved: on a resumed world, the base counts toward Rising
until the window refills. Under the legacy lifecycle the probe is not applied. 39 native checks pass. The
release-capacity reasoning is recorded for the release-judgement slate in ReleaseJudgementDirective.md §6.

`doc-np-oc12r` (push dropped, `--major-offchart-singles=0.12 --major-offchart-singles-hold-units`), seeds
1001-6006, against the push-dropped arm (`doc-nopush`, `doc-np3`, `doc-np6`) and control (`dlong-ctl`,
`doc-ctl3`, `doc-ctl6`). Means of six seeds; paired difference against push-dropped, with its standard error.

| | Control | Push dropped | + base .12 (decoupled) | Paired difference |
|---|---:|---:|---:|---:|
| Total single units (M) | 331 | 335 | 340 (303 live + 36 base) | +1.3% |
| Major share of single units (FTC residual ~32%) | 23.6% | 23.2% | **31.9%** | +8.7 pts |
| of which live records | 23.6% | 23.2% | 23.7% | +0.6 ± 1.7 |
| Major Top 10 share, 1960 (history 35.6%) | 47.5% | 39.0% | 38.6% | −0.4 ± 1.4 |
| Major Top 10 share, 1961 (history 22.1%) | 39.3% | 36.0% | 39.2% | +3.2 ± 4.0 |
| Rising share of Major label-weeks | — | 51.4% | 45.7% | |
| Major single / LP launches | — | 486 / 253 | 495 / 264 | +1.9% / +4.3% |
| Major campaign spend per launch | — | $2,727 | $2,693 | |
| Starting Small / Boutique / Independent closed | 11.3 / 17.0 / 9.5% | 18.0 / 24.2 / 19.8% | 17.7 / 25.6 / 20.0% | |
| Mean #1 weekly units | 87k | 127k | 106k | |

Per-seed 1961 differences: −1.2, −15.2, +8.4, +9.1, +10.2, +7.9 points.

Readings:

1. **The base does what it was built for.** Majors reach the FTC residual (31.9%) while their live records
   keep the same unit share as without the base. Total single units hold within 1.3%. Majors' 1960 Top 10
   share is unchanged. The 1961 share is +3.2 points, under one standard error. Four of six seeds rise,
   so a small residual chart effect cannot be ruled out.
2. **Decoupling Rising worked.** Majors' Rising share now sits below the push-dropped arm, because the hold
   takes about 10% of live single revenue. Release volume rises only 2% (singles) and 4% (LPs), against
   3% and 10% at .15 without decoupling. Campaign spend per launch is flat.
3. **Small-label closures are unchanged by the base.** All of the rise over control (+7 / +8 / +10 points)
   comes with the commercial-fit candidate, as found before.
4. **Majors' chart excess is a baseline problem, not this base.** Push dropped, Majors hold 36-39% of 1961
   Top 10 slots against a 22% history, and the control is the same. Seeds 4004/5005 run 40-52% in every
   arm (known concentrated worlds). The film/broadcast-major second rank, which is missing, and the
   Big-5's own push premium are the remaining routes.
5. **The hold lowers chart-record units.** Hit records sell about 10% fewer copies: the #1 falls from 127k
   to 106k. The chart's share of all units is 67% (a diagnostic; there is no sourced target).

Candidate for the commercial-fit bundle: push weight dropped, `--major-offchart-singles=0.12
--major-offchart-singles-hold-units`. Not adopted. The bundle's small-label closures, Boutique unit share
(3-4% in every fit arm against 13% in control) and a 265-week run remain open.
