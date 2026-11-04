# Budget LP products, substitution and shared shelves

26 October 2026. Continues [BudgetLpV2.md](BudgetLpV2.md). This is an opt-in
economic/channel experiment. It does not change the game defaults or infer a
budget market size from the difference between total model and historical sales.

## Products and ownership

`--budget-lps` requires `--rack-jobbers` and the live genre-market-v2 clearing
path. Clubs are independent; their catalogue weights and licences grant no
budget rights. The saved v32 ledger contains separate budget products:

- A known own master retains its source record ID, original artist, owner,
  repertoire genre, frozen quality and lifetime store-sales appeal. Its budget
  release occurs no earlier than 52 weeks after the original release. Retirement
  remembers the product and remaining holdback; it never creates another issue.
- Opening synthetic baskets have an owner/genre identity, unknown titles and no
  invented source master or artist. Their quality and appeal follow the opening
  live catalogue mix. These stand for assumed older own repertoire, rather than
  recovering missing pre-1960 titles. They are an explicit opening-stock
  sensitivity and can overlap economically with represented live repertoire.

Acquisition moves ownership while preserving product identity, release date
and regional purchase depletion. Closed and player-owned labels cannot offer
products. A product's budget sales never become source-record sales, chart
points, release outcomes or new-record production. Known-master budget purchases
do enter that master's later store/rack buyer exhaustion. Synthetic baskets
cannot be assigned to particular original records.

The ledger is optional in old saves. Budget saves require the budget switch and
the saved budget price, shopper scale, substitution and shelf turnover on resume.
World capture/restore includes the product ledger and per-region depletion;
week-local offers and telemetry are reconstructed.

## Buyers and one physical shelf

Each region offers at most 25 budget products, ranked by decaying source appeal
with stable identity ties. Repertoire, quality and prior budget purchases weight
the bargain pool's choice between these products. It is one regional pool,
divided between offers, rather than an independent full pool for every title.
The scale starts at .08 of population, weighted by rack reach and income.
Conversion uses .095, a bounded price response and the existing seasonality.
These are scenario assumptions, not observed buyer counts or estimated demand.

Store substitution is a weekly choice between a cleared AI store purchase and
an available same-genre cheap product. Potential switching is:

`cleared same-genre stores × substitution × quality/depletion availability × (1 − budget price / $3)`.

The initial substitution coefficient is .25; `--budget-substitution=0` turns off
the direct weekly store choice while retaining known-master exhaustion and shared
physical capacity. A budget copy's fulfilled share determines
its integer number of switchers. Only those fulfilled switchers leave the store
allocation, one for one. Unfulfilled budget demand removes no store purchase.
Store reductions are proportional by record, stable by identity. Store inventory
is consumed only by the remaining sales. Freed purchasing capacity is not
reassigned to singles or a second spillover pass. This is an anonymous weekly
choice model, not identification of unique people or a lifetime cross-title purse.
Paired world differences also include later cash, release and survival feedback;
the explicit switched-buyer stream measures the direct weekly transfer separately.

Budget offers occupy catalogue slots in the existing 75-slot assortment:
25 front-line hits, up to 50 minus the budget offers in front-line catalogue,
and the budget offers. Front-line and budget demand then enter **one** integer
largest-remainder allocation against the region's existing shelf. Both physical
unit capacity and assortment slots are shared. Front-line rack displacement is
therefore explicit competition for shelf/assortment; direct rack-buyer preference
switching is not separately identified. Store observations used to select the
front-line assortment are the pre-switch cleared intentions.

The default shelf stays at 30 LPs/rack/week with the existing rack-count/reach
curve and frozen .026 front-line shopper scale. `--rack-turnover=45` is a separate
capacity sensitivity, not an adopted correction to the historical rack count.
The existing shelf cannot carry the all-rack historical target in 1961 even with
unlimited demand. National spare capacity also cannot move across regions/weeks.

## Own-master economics

The reference scenario price is $1.75; `--budget-price=` accepts an explicit price.
The trial $1.25 case lost about $0.077 per copy under the existing pressing cost
before returns. The final implementation declines offerings with nonpositive
expected contribution, rather than supplying that loss indefinitely. Prices,
55% trade receipts and 5% artist royalty on trade receipts are assumptions.

Each own-master sale books separate receipts, the existing album pressing cost,
an assumed 11-cent simple sleeve, eleven tracks of period mechanicals, and artist
royalty. Unknown artists in opening baskets still incur royalty expense. The
rack return scenario charges extra unsold manufacturing/sleeve copies; no artist
royalty or consumer sales are added for these copies. Known artists receive their
budget royalty without recouping an unrelated current-label advance. Budget cash
joins existing company bookkeeping and tax/P&L accounting.

This slice covers own-master products. It does not reuse the club's 17.8-cent
licence terms. Cross-label budget licensing, separate licence expiry and budget
manufacturing stock/receivable timing remain future work. There is no new fixed
reissue/session count, and pressing is charged on fulfilled volume, as in the
existing rack abstraction. The margin gate is not a full label pricing strategy.

## Reproduction and checks

Build first, then choose a fresh tag:

```powershell
dotnet build --no-restore
./SimTools/RunRackJobberProbe.ps1 -RunTag blp-v3-screen -Compact -Weeks 106 `
  -Arms racks,budget,budgetadd,budgetturnover45,budgetprice125 -Seeds 1001
python SimTools/analyze_budget_lp_products.py --tag blp-v3-screen
python SimTools/plot_budget_lp_products.py --tag blp-v3-screen --year 1961
```

The runner executes native rack and budget checks, the off replay, and real
world/gzip save/load/resume checks before the worlds. It freezes the assembly
hash. The named arms retain identical base flags, seed and duration. The budget
arm uses substitution .25, the sensitivity without weekly switches uses zero, the capacity
sensitivity raises only turnover to 45, and the low-price arm uses $1.25 with
the same economic gate. These are scenarios; none is accepted calibration.

The analyzer checks combined shelf use, total assortment slots, regional/product
units, booked financial units, saved product depletion, release holdback, zero
switchers in that arm and company cash reconciliation. It maps rack and
budget rows onto the store settlement calendar, reports observed weeks, never
annualizes partial years, and keeps store and all-rack historical errors separate.
Outputs and generated evidence remain under `SimLogs/`.
While a matrix is running, `--completed-only` writes explicitly provisional
files from fully completed worlds. It never reads unfinished worlds or replaces
the final report. The plot requires a completed matrix and complete calendar year.

`budgetfullswitch` is an additional conservation stress: shopper scale zero,
substitution coefficient one. It requires nonzero budget sales and every sold
budget copy to match a removed store purchase; it is not a volume-fit arm.

Telemetry: `budget-region-weekly`, `budget-product-weekly`,
`budget-finance-weekly`, `budget-product-catalogue`. The existing rack streams
continue to mean front-line racks; sum the explicit budget stream for all racks.
`shelfBound` includes both demand streams in budget worlds. Regional demand,
spillover and purchasing budgets describe the initial store clearing; final store
sales reflect the subsequent fulfilled budget choices. Exported/imported capacity
is not reclaimed when those buyers switch channels.

## Validation results

The first, superseded 13-week $1.25 smoke matrix completed under `blp-v3-smoke`:
all four worlds reconciled shared capacity, product/financial units and cash.
Relative to front-line racks alone, the budget arm reduced stores by .152M and
increased all racks by 2.223M; .145M store purchases explicitly switched. Higher
turnover made no difference in this early window, when the shelf had spare room.
Budget contribution was negative $255,636, motivating the margin gate and the
$1.75 reference scenario. These partial-1960 results are implementation evidence,
not a 1961 or later-year volume fit. They use the earlier assembly and are not
combined with the final screen.

### Completed 1961 screen

All five `blp-v3-screen` worlds completed 106 weeks with seed 1001, the same
assembly, base bundle and calendar. The 1962 tail has only two weeks and is
excluded from historical comparisons. Units below are millions:

| Scenario | Stores | Front-line racks | Budget racks | All racks | Direct store switches |
|---|---:|---:|---:|---:|---:|
| Front-line only | 76.873 | 36.226 | 0 | 36.226 | 0 |
| Budget reference, weekly substitution .25 | 74.441 | 26.561 | 16.928 | 43.489 | 1.800 |
| Weekly substitution zero | 75.983 | 26.429 | 15.676 | 42.105 | 0 |
| Budget reference, turnover 45 | 73.911 | 25.509 | 17.241 | 42.750 | 1.836 |
| $1.25 with contribution gate | 76.873 | 36.226 | 0 | 36.226 | 0 |
| Historical outlet reference | 68 | — | — | 60 | — |

The reference reduces the store excess from 8.873M to 6.441M and the all-rack
shortfall from 23.774M to 16.511M. Its 16.928M budget sales increase total LPs by
only 4.831M: front-line rack sales fall 9.666M and stores fall 2.432M. The latter
includes the explicit 1.800M weekly transfer and subsequent world feedback.
Thus a net national shortfall remains an unsuitable budget-market-size measure.

Front-line catalogue racks fall from 21.676M to 10.880M; hits rise from 14.550M
to 15.680M. Shared assortment matters strongly under the assumed 75 total slots
and 25 budget offers. Before physical unit rationing, reference front-line plus
budget demand is only 44.331M, versus 43.489M sold. Removing every same-state
unit cap could therefore recover at most .842M, leaving a 15.669M rack gap.
Higher turnover alone cannot supply the missing demand/assortment in that state.
Its separate world has different subsequent cash, releases and assortment; the
lower all-rack result is not a claim that a larger shelf reduces sales for an
unchanged set of offers.

Reference unused national shelf is 9.621M, against a 53.110M ceiling; turnover
45 raises that ceiling to 79.665M but leaves 36.914M unused in its own world.
Headroom cannot transfer across weeks or regions. Known own masters supply
16.647M reference budget units and synthetic baskets .281M. Own-master budget
contribution is positive $3.121M under the stated assumptions. Clubs remain at
44.135M on this settlement-year convention in every arm.

Every completed world passes shelf, assortment, product/financial/depletion and
cash checks. The front-line baseline's six full sales/company/cash/finance/rack
CSV streams match archived `rkj-v1` byte for byte through all 106 weeks. The
unprofitable $1.25 arm matches those same six streams of the new baseline byte
for byte. The flag-off check matches 75 archived weekly streams through week 12.
Eight supply-observer checks also pass.

The long matrix used assembly `E5CD69B5A5CED6CF7BF0A03E33733716985A2479F3BC6ED8E7B1C6077BC694C5`
(retained as `SimLogs/blp-v3-screen/frozen-matrix.dll`). A final boundary guard
caps rounded switch cohorts at actual store buyers and removes unfulfilled
copies. The long worlds already passed the existing runtime assertion of that
same bound, so the guard is inactive on their allocations. Its final build and
short native/save/load/replay/conservation screen are kept separately under
`blp-v3-final2`; the two assembly hashes are not combined into one matrix.
The first final stress (`blp-v3-final`) exposed 78 pure-switch copies left
unmatched by floating-point cancellation in `floor(n × demand / demand)`.
Pure-switch cohorts now preserve the fulfilled integer exactly before applying
the cohort bound. Positive-incremental-pool scenarios retain their original
allocation formula; final replay checks compare them to the long worlds.

Final validation passes on assembly
`A7AA861CED2DBCB4CB6C3B79A91CCF629F3BBBD5FE00C80F00D6C67879860963`:

- Build: zero warnings/errors; 34 budget and 50 rack native checks pass.
- Both 13-week world/gzip save/load/resume checks pass; the preserved supply
  observer's eight checks also pass.
- All six final 13-week worlds complete and reconcile. The stress sells 92,320
  budget copies and removes exactly 92,320 store purchases, with no new bargain
  shoppers.
- Final ordinary scenarios replay the long worlds through week 12 exactly:
  75 weekly streams for front-line-only and 78 for each budget scenario. The
  final off replay matches 75 archived streams. Evidence:
  `SimLogs/blp-v3-final2/guard-replay.json` and `budget-review.json`.

This is one seed and a 1961 channel screen, not validation of the 1962–65 path or
an accepted national calibration. The remaining store excess, rack shortfall and
large catalogue displacement call for testing assortment and buyer-overlap
assumptions, plus independently specified licensed budget supply, before
choosing volume parameters. Game defaults and the frozen front-line scale stay unchanged.
