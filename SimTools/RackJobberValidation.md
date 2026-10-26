# Rack historical validation

The subsequent own-master budget/substitution/shared-shelf implementation and
its separate screen are in [BudgetLpProducts.md](BudgetLpProducts.md). This report
retains the completed front-line-only validation and its historical conclusions.

**Completed 26 October 2026:** the frozen assembly hash was verified unchanged.
The restarted returns worlds and both six-year growth worlds passed under
`rkj-returns-v2` and `rkj-growth-v1`. Partial output from the
original `rkj-stress-v1` returns worlds remains excluded. The reusable default
and overlap runs have a completed-only manifest at `rkj-stress-completed-v1`.
Completed seed comparisons, overlap results, checks and interim analysis are
preserved. Combined annual, paired and seed summaries are at
`SimLogs/rkj-returns-v2/review/`. The original restart handoff remains recorded
in `SimLogs/rkj-validation-checks/restart-handoff.json` for provenance.

25 October 2026. Continues
[RackJobberChannelDesign.md](RackJobberChannelDesign.md) and [BudgetLpV2.md](BudgetLpV2.md).
The front-line shopper scale stays at 0.026; budget sales remain absent.

## Questions and experiments

- Robustness: paired control/racks on seeds 1002 and 1003, 106 weeks, against
  seed 1001's archived fit. These are independent worlds, not confidence intervals.
- Overlap: `--rack-shared-exhaustion`, seed 1001. Both store and rack exhaustion
  read store plus rack cumulative purchases of the same title/region. Each still
  uses its own buyer-pool denominator. Stores see prior rack purchases; the rack
  pass also sees that week's already-cleared store purchases. Both accounting
  ledgers remain separate. This is an extreme shared-purchase sensitivity, not
  identification of unique people, cross-title budgets, or the actual overlap.
- Returns: `--rack-returns=0.1|0.2`, seed 1001. Units are consumer sell-through,
  already excluding unsold copies. Returned share r of shipments implies
  `sold × r/(1-r)` extra copies. Charge their pressing, packaging and outward
  regional freight as full label write-offs. No salvage or later resale, no
  royalty on an unsold copy, no fictitious sale/refund, no distributor rebate,
  and no additional chart units. It is a manufacturing-cost stress, not a
  model of receivable timing, return lags, reverse freight, or reusable stock.
- Concentration: distinguish all-store Major LP share, Major share of store
  units on the racked assortment, Major share of rack sales, and rack title-week
  share. Present tier shares separately from owner-family Hot 100 measures.
- Growth: compare older-series years 1961–65 without fitting annual volume.
  Front-line model racks cannot be level-matched to all-rack historical units
  until budget products exist. Revised-series years stay separate.

Default behavior stays unchanged. New flags require `--rack-jobbers`. Return
rates must be finite in [0,1). As with other scenario flags, resume with the same
flags; no new saved state is introduced by these sensitivities.

## Reporting and calendar

`RunRackJobberProbe.ps1` now accepts `overlap`, `returns10`, `returns20`, up to
530 weeks, and `-Compact`. Compact removes only the LP demand, capacity, and
follow-up diagnostic flags, replacing them with `--rack-validation-audit`:

- `rack-store-weekly.csv`: all-store LP units by tier, live LP count and charted
  launch identities, observed at the booked, pre-retirement settlement.
- `rack-club-weekly.csv`: change in cumulative consumer club units, excluding
  licensor duplicates, assigned to the same settlement calendar.
- `rack-finance-weekly.csv`: sold rack units, gross billings, sold manufacturing/freight
  basis and extra return write-offs, by current label and tier. All rack runs
  write this stream; it is not saved gameplay state.

Existing rack region/title rows use the simulation date. Frozen store settlements
use that date plus seven days. The analyzer now maps rack weeks to the settlement
year. For archived seed 1001, 1961 rack units become 36.226M, compared with 36.237M
in the original report. This changes timing, not underlying sales. Archived club
annual files retain their original booking-year convention; new compact runs
provide club weekly units on the settlement boundary too. A partial final year
is explicitly marked and never annualized. Company-monthly economics retain
their accounting calendar, so annual cash/earnings and settlement-year units
are related diagnostics, not a per-unit annual accounting identity.

The analyzer checks cash reconciliation, regional shelf caps, title/region unit
identity and (when present) rack finance unit identity. It writes annual and
paired tables. Combine only matching new assembly hashes; pairs must share seed,
duration and year coverage. Raw observations are unchanged.

The older `closedAll` count includes Bankrupt, Defunct and Acquired labels.
New reports also separate those statuses. Treating all three as insolvency
would confuse shutdown acquisitions with business failure. Hot 100 C4 uses a
year-end owner-family roll-up; rack Major share uses the record's label tier
at sale. They answer different concentration questions.

## Historical evidence

The reviewed material did not verify a common returns-accounting method for
the historical outlet-unit estimates. The model reports consumer sell-through;
these tests therefore size label cost, and do not multiply the historical unit
targets by .9 or .8. Treat that measurement qualification separately from the
chosen shipment-return rate.

The existing period sources support racks selecting proven sellers and having
high returns, but do not identify a national Major share or a rack/store customer
overlap fraction. A contemporary second trade paper also reports Capitol's
criticism of high returns and weak catalogue support:
[Music Business, 13 March 1965](https://www.worldradiohistory.com/Archive-All-Music/Music-Business/Music-Business-1965-03-13.pdf).
The 10%/20% rates are scenarios, not historical estimates.

Later budget mix should not automatically hold 1962's 21% dollar share fixed:
[Billboard's NARM survey, 12 March 1966](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1966/BB-1966-03-12.pdf)
reports 1965 budget LP dollars of $27.98M against $164.58M of LP dollars (17%).
The survey's member coverage and changes in discounting prevent reading a direct
national unit-share curve from those dollars. Search-index extraction, not a new
visual audit of the scans, was available in this review.

## Initial finding from archived seed 1001

1961 Major LP shares: all stores 48.86%; store sales of titles in the rack
assortment 81.28%; rack sales 79.20%; rack title-weeks 78.38%. Most of the bias
appears at assortment selection; the rack conversion does not further raise the
Major share of that selected store-sales mix. This does not establish historical
accuracy. No numerical national rack-by-label benchmark has been found.

For context, Major-tier labels take 82.78% of charted LP units and 79.11% of
charted LP title-weeks in the same rack-enabled world. Racks' 79.20% sales share
is closer to the charted LP market than to the whole store catalogue. The
relevant question is how broad a rack's assortment should be; these observations
do not independently establish that its tier mix should be flattened.

By rack tier, seed 1001's 1961 Major share is 85.36% on hits (14.550M units)
and 75.06% on catalogue (21.676M units), on the aligned settlement calendar.
Both tiers contribute to the overall concentration; it is not confined to hits.

### Reach-curve correction

The actual curve is linear from 0.30 in 1960 to 1.0 in 1969:
1961 = .377778, 1962 = .455556, 1965 = .688889. It grows 1.2059x in
1961–62 and **1.8235x in 1961–65**, compared with historical all-rack units'
1.2667x and 1.7667x. The original design's 1.62x arithmetic was wrong. This
authored reach trajectory is not a sales forecast: assortment, quality, buyer
exhaustion, population/income and later budget mix still determine volume.

Growth comparisons also need the missing budget mix qualification. As an
illustration only, applying member budget dollar shares of 21% then 17% to
national 1961/1965 rack units, holding assumed front-line price at $3 and budget
price at $1.25 or $1.75, implies front-line growth of roughly 1.9x. This is not
an empirical front-line series: the member/national and year substitutions and
fixed prices are assumptions. Do not tune the model to this calculation.

## Verification before the longer screen

Correction to the original v1 concentration discussion: the .322–.373 range
quoted there belongs to the **no-club** controls. The relevant club-enabled
1961 controls on seeds 1001–1003 are .356274, .311471 and .308554. Seed 1001's
rack C4 (.267866) is below that observed range. This alone neither establishes
a general decline nor supports dismissing it as within the observed control
range; the new matched rack seeds are needed.

- Build: no warnings or errors.
- 50 native rack checks, including switch validation, shared numerators, store
  exhaustion and return-to-shipment arithmetic.
- Four 13-week smoke worlds (control, default racks, overlap, 20% returns), seed
  1001: all completed; company cash, shelf and rack finance reconciled.
- Default rack replay through week 12: label sales, company-monthly, company cash,
  label finance and rack region/title rows match archived `rkj-v1` exactly.
- Rack-off control replay through week 12: label sales and all three company/
  finance streams match archived `rkj-v0-control` exactly.
- Seeds 1002 and 1003 additionally match their archived `rcl-v2-clubs` controls
  through week 52 in label sales, company-monthly, company cash and label finance
  (`control-year-replay.json`).
- The completed 106-week controls also match those four full archived files
  byte for byte on both seeds (`control-full-replay.json`, eight SHA-256 pairs).
- The new default rack arm also matches all six full 106-week archived v1
  sales/company/finance/rack streams byte for byte (`racks-full-replay.json`).
- A full 13-week world save/load with shared exhaustion and 20% returns passed,
  including compressed disk I/O and resumed simulation. Native/replay/save
  evidence is under `SimLogs/rkj-validation-checks/`.

Longer manifests: `rkj-robust-v1` (seeds 1002–1003, control/racks, 106 weeks),
`rkj-stress-v1` (seed 1001, default/overlap/10%/20%, 106 weeks), and `rkj-growth-v1`
(seed 1001, control/racks, 318 weeks). These share a frozen assembly; their check
arrays are empty because verification ran separately as listed above. A planned
arm is not a result until its manifest says it passed.

## Frozen-scale additional seeds: completed

All four `rkj-robust-v1` runs passed, including cash, shelf, finance units and
return-cost reconciliation. Results are settlement-year 1961; seed 1001 uses
the archived fit with its rack weeks aligned to the same calendar.

| Seed | Store LPs, control → racks (M) | Rack LPs (M) | Major rack share | Hot 100 C4, control → racks | Inactive labels, control → racks |
|---|---:|---:|---:|---:|---:|
| 1001 | 74.526 → 76.873 | 36.226 | 79.20% | .356274 → .267866 | 78 → 63 |
| 1002 | 73.028 → 74.391 | 35.406 | 88.91% | .311471 → .330160 | 83 → 83 |
| 1003 | 74.236 → 73.043 | 34.779 | 77.37% | .308554 → .302644 | 77 → 88 |

The front-line volume is fairly stable across seeds (34.8–36.2M), but two new
seeds sit below the assumed 36–42M range. Keep this as a modest downward-volume
qualification, not a reason to refit before budget and overlap are resolved.
The large C4 decline and the improved survival in the fitted seed do not repeat
consistently. In seed 1003 the additional 11 inactive labels are Defunct, not
Acquired. No general survival benefit follows from the initial result.

The assortment bias does repeat: Major share of selected titles' store units is
90.17% on seed 1002 and 74.80% on seed 1003, versus their rack unit shares of
88.91% and 77.37%. Conversion changes those selected shares only a few percentage
points; selection explains most of the large difference from the overall store
Major shares of 54.03% and 45.60%.

Chart-crowding indicators remain mild: LP entrants change 251→246 and 244→245;
mean live LPs change +2.04% and +0.34%. Compact output does not retain LP title
chart trajectories, so these checks do not establish unchanged first-spell
duration. Terminal Defunct status also does not identify the reason for closure;
the separate status counts should not be read as a bankruptcy-event census.

### Club calendar qualification exposed by the weekly audit

The new aligned club stream records 44.135M in settlement-year 1961, while the
existing authored/booking-year club total is 45M. A raw 53rd week of 1960 sells
zero by the club rule; that tick falls in the first settlement week of 1961.
The following raw-year club ticks straddle the next settlement boundary. This
is a timing/counting difference, not a volume reduction from racks; it occurs
in both controls and treatments. New all-channel totals use the common
settlement calendar, and should not silently combine the old 45M booking-year
total with differently dated store and rack sales. The 106-week grand totals
are conserved. Do not tune club volume to remove this one-week discrepancy.

## Buyer overlap: completed stress test

Seed 1001, 106 weeks, default scale, settlement-year 1961:

| Units, millions | Separate exhaustion | Shared exhaustion |
|---|---:|---:|
| Store LP | 76.873 | 71.819 |
| Rack LP | 36.226 | 23.999 |
| Clubs/mail | 44.135 | 44.135 |
| Total LP | 157.234 | 139.952 |
| Singles | 136.642 | 137.763 |

Sharing purchase exhaustion cuts racks by 33.75%, stores by 6.57%, and total
LPs by 17.282M (10.99%). The control's store excess shrinks, but rack front-line
units fall well below the exploratory target. This is the largest measured
mechanism sensitivity so far, comparable with the missing budget market itself.
It shows that the fit depends on separate exhaustion; it does not measure actual
shopper overlap. Changing the shopper scale to repair this result would be a
new joint calibration, not validation of the existing one.

Both worlds passed shelf, finance-unit and cash checks. The test includes the
subsequent release/economic reshuffle, so its changes are not additive with
budget, return or other scenario effects. The common numerator and different
pool denominators are an explicit stress abstraction, not person-level demand.

## Returns: frozen-state cost sizing

The default seed-1001 arm records 1961 rack gross billings of $97.056M and
sold-copy manufacturing/outward freight of $18.040M. Holding that sales mix
fixed, a 10% shipment return rate costs an extra $2.004M; a 20% rate costs
$4.510M. These are 2.07% and 4.65% of rack gross billings, before tax and
economic feedback. They are charges on unsold copies, not lost consumer units
or additional artist royalties. The live 10%/20% runs test the subsequent
release/company reshuffle separately; these frozen cost numbers are not a
prediction of their final profits.

## Returns: completed live screen

Both fresh 106-week runs passed. The combined analysis at
`SimLogs/rkj-returns-v2/review/` includes the completed-only stress manifest and
the additional seed checks. Cash, shelf, region/title units, finance units and
declared return-cost arithmetic reconcile. Seed 1001, settlement-year 1961:

| Metric | Default racks | 10% returns | 20% returns |
|---|---:|---:|---:|
| Store LPs (M) | 76.873 | 77.711 | 78.010 |
| Rack LPs (M) | 36.226 | 34.836 | 34.365 |
| Total LPs including clubs (M) | 157.234 | 156.682 | 156.510 |
| Rack gross billings ($M) | 97.056 | 94.619 | 92.422 |
| Extra return write-offs ($M) | 0 | 1.941 | 4.281 |
| All-tier pretax earnings ($M) | 141.423 | 140.354 | 136.665 |
| Major pretax earnings ($M) | 88.357 | 89.472 | 85.097 |
| MidTier pretax earnings ($M) | 15.562 | 11.243 | 13.320 |
| Hot 100 C4 | .267866 | .307145 | .292173 |
| Inactive labels at year end | 63 | 68 | 63 |
| Distinct launched LP chart entrants | 247 | 246 | 251 |

Total LP volume falls by 0.552M / 0.724M (0.35% / 0.46%), much less than
the shared-exhaustion stress. Store sales rise while rack sales fall. These
are whole-world responses to costs charged from the start, including release
and assortment changes; they do not estimate direct consumer substitution.
The realized return costs differ from frozen-mix sizing because sales and mix
change. Their ratio to rack gross is 2.05% / 4.63%.

The Major/MidTier earnings and closure responses are not monotonic in the
scenario rate. Inactive labels are all recorded as Defunct in these year-end
snapshots; this is not a count of bankruptcy events. These single-seed scenarios
establish implementation behavior and cost sensitivity, not an empirical return
rate, a repeatable survival effect, or a reason to recalibrate shopper scale.
All-tier pretax earnings fall by $1.069M / $4.758M. This aggregate includes the
whole record business, so its delta is not an isolated rack margin estimate.

## Later-year growth: completed

Both seed-1001 worlds completed 318 weeks, covering complete settlement years
1961–65 and a partial 1966, which is excluded here. No parameter was refitted.
The first 106 weeks match the shorter archived runs in 13 weekly streams
(4 control, 9 racks), 4,081,398 rows, including the compact store/club/finance
diagnostics. Evidence: `SimLogs/rkj-validation-checks/growth-early-replay.json`.
All full-run cash, shelf, units and return-cost checks passed.

Units in millions; historical racks include budget LPs, model racks do not:

| Year | Stores control | Stores with racks | Historical stores | Model front-line racks | Historical all racks | Model total LPs | Historical total LPs |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1961 | 74.526 | 76.873 | 68 | 36.226 | 60 | 157.234 | 173 |
| 1962 | 76.910 | 81.486 | 70 | 42.736 | 76 | 169.222 | 191 |
| 1963 | 82.067 | 85.858 | 66 | 48.842 | 79 | 175.777 | 186 |
| 1964 | 85.198 | 87.717 | 68 | 60.710 | 86 | 186.485 | 192 |
| 1965 | 84.018 | 85.502 | 76 | 60.597 | 106 | 192.830 | 228 |

The 1961–65 endpoints alone look reasonably close in proportional growth:
stores with racks grow 1.112x versus historical 1.118x, and front-line racks
grow 1.673x versus historical all-rack growth of 1.767x. The rack comparison
remains a product-mix qualification, not a validated front-line growth target.
Annual paths reveal more: stores overshoot by about 19.9M / 19.7M in 1963/64,
and racks plateau in 1965 while historical all-rack units rise 23.3%.
The control also overshoots stores in those years, so this is not solely a
rack-channel problem. No single missing-budget number explains the annual net
LP gaps: 15.8M, 21.8M, 10.2M, 5.5M and 35.2M.

The rack plateau is not a binding national shelf limit in the current front-line
world. In 1965 all region-weeks are below shelf; desired demand is 60.597M and
cleared units 60.597M. Hit sales fall from 27.490M in 1964 to 22.958M in 1965;
catalogue rises from 33.221M to 37.640M. That locates the plateau on the
demand/assortment side, without identifying which demand input causes it.

Capacity matters once budgets are added: total shelf is 98.891M in 1965,
leaving 38.294M unused after front-line sales, versus a 45.403M gap to the
historical all-rack volume. The current national ceiling itself is 7.109M
below 106M. In 1961 the aligned shelf is 53.110M, also below the 60M all-rack
reference. Spare capacity must be shared across product lines, and national
headroom does not guarantee the right regional/week capacity. A capacity
change needs its own justification; do not duplicate the shelf for budgets.

### Major concentration over time

Major rack shares rise from 79.20% in 1961 to 85.45%, 89.79%, 95.18%, then
92.05% in 1965. The selected assortment's store-unit shares are 81.28%, 87.87%,
88.60%, 93.16%, 94.30%; whole-store Major shares are only 48.86–55.44%.
Selection remains the dominant source of rack concentration, and the later-year
concentration is stronger than the first-year fit suggested. No historical
national rack-by-label benchmark has been established, so this flags assortment
breadth for review rather than supporting a new arbitrary tier-share cap.
These Major-tier shares are distinct from owner-family Hot 100 concentration.

## Implications for budget LP v2

1. Keep the default front-line calibration frozen while adding the missing
   product. Three seeds support similar front-line volume; they do not establish
   repeatable concentration or survival improvements.
2. Resolve budget/store/rack buyer substitution explicitly. Shared exhaustion is
   the strongest measured demand sensitivity (17.3M total LPs in 1961); assuming
   every budget buyer is additional would bypass this uncertainty.
3. Use the separate budget-product ledger described in `BudgetLpV2.md`, with
   source provenance, eligibility/holdback, ownership and saved exhaustion.
   The current supply-only audit does not book budget sales or identify masters.
4. Allocate front-line and budget demand against one regional rack shelf. Review
   the capacity assumptions separately, since the existing ceiling cannot reach
   historical all-rack units even with unlimited demand.
5. Carry budget manufacturing, receipts and return costs explicitly. The tested
   10%/20% front-line return scenarios are not measured budget return rates.
6. Recheck the annual channel path and selected assortment after v2, rather than
   fitting one 1961 net gap. The store excess and 1965 rack plateau are remaining
   calibration questions alongside the absent budget product.

This closes the five requested checks within their stated scope. LP chart spell
duration, empirical buyer overlap/return rates, a historical rack-label share
benchmark and a full decade comparison remain unestablished. Defaults are
unchanged. Budget-LP sales were absent in these validation arms; the later
product experiment is documented separately above.
