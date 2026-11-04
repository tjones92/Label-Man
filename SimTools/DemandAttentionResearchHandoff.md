# Handoff for Astra: audience attention, the demand engine, and historical release losses

2026-10-21. Workspace: `C:/Project/Label-Man` (Windows, PowerShell; Git Bash also available). Branch
`media-atmosphere` at `970d639`.

## Status and authority

The user wants records to **lose money at historically accurate rates without harming smaller labels**. They
suspect the demand model gives every single an unlimited audience ("infinite ears"), and they are open to
overhauling the whole demand engine if historical research supports it. They also want to know whether the
game's 31 cities can carry **per-city attention** so that regional hits happen and pay small labels.

Most of the game is already balanced: genre shares, Small-label closure rates, chart breadth and total
units are accepted calibrations. **The core goal is the loss rate. The rest of the balance must hold.**

This handoff gives evidence, code pointers and a suggested plan. It is not a new source of user permission.
You are authorized to:

- research the historical record, and write up sourced targets;
- measure the current model, using existing runs or new short runs (105 and 265 weeks, seeds 1001 and 2002);
- build **opt-in** prototypes behind new command-line switches, which must be exactly inert when off;
- screen those prototypes on two-seed short runs.

You are **not** authorized to change a default, run a decade A/B, commit, push, or delete evidence. Bring a
written proposal back to the user before any of those. Evidence goes in `SimLogs/` (gitignored).

## Read first

1. [ReleaseLossCalibration-2026-10-21.md](ReleaseLossCalibration-2026-10-21.md): the measured loss rates,
   the historical target and its sources, the tuning attempts that failed and why.
2. `Systems/ChartSimulator.cs`: the single demand model. Read its comment blocks as well as the code; they
   record many measured dead ends.
3. `Systems/ChartManager.cs:1252-1395` (`CalculateLiveRegionalSalesWithMarketClearing`): weekly purchase
   capacity and how it is rationed.
4. [RegionalSyncDesign.md](RegionalSyncDesign.md) and [RegionalAudienceDesign.md](RegionalAudienceDesign.md):
   why the seven regions move in lockstep, and what has been tried.
5. [SmallLabelAttritionAndChartMethod.md](SmallLabelAttritionAndChartMethod.md) and
   [EconomyCashHistoricalReview-2026-10-21.md](EconomyCashHistoricalReview-2026-10-21.md): how the closure
   band was reached and what the cash evidence does and does not show.
6. [LocalSceneHistoricalAtlas.md](LocalSceneHistoricalAtlas.md): historical notes on the 31 cities.
7. [HistoricalLabelTierBalanceResearch.md](HistoricalLabelTierBalanceResearch.md) and
   [SingleHitTailHistoricalCalibrationHandoff.md](SingleHitTailHistoricalCalibrationHandoff.md): earlier
   source work on the market's concentration. The second explains why a direct sales dataset is still missing.

## 1. Where the game is

From the loss-rate write-up (corporate default, five-year runs `rj5-def`):

| | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Singles losing money | 8.6% | 7.4% |
| LPs losing money | 1.0% | 0.8% |
| Starting-Small labels closed by week 260 | 31.6% | 28.7% |

The working historical target is that **most singles lost money all-in (60-85%)**, and LPs less often. That
target rests on the trade's "one in ten" rule of thumb and on Denisoff; neither is an audited 1960s rate.

**Why the medians matter.** About 85% of singles never chart. Whether a release loses money depends on how
many copies a non-charting single sells against its break-even:

| Tier | Break-even, all-in | Median non-charting single now |
|---|---:|---:|
| Small | ~1.5k copies | 8.1k |
| Major | ~20k copies | 25.6k |

Until most non-charting singles sell below break-even, no cost lever moves the loss rate much. The A2-A4
cost stack moved it by less than a point.

**New measurement for this handoff.** On the same runs, singles that never reached the Hot 100 sold
**34-37% of all single units**:

| Run | Singles | Charted | Units to never-charted | Mean units, charted / never |
|---|---:|---:|---:|---:|
| `rj5-def` 1001, 265 wk | 18,746 | 15.6% | 35.7% | 162k / 16.7k |
| `rj5-def` 2002, 265 wk | 17,369 | 16.4% | 33.5% | 175k / 17.3k |

Method: units are `pressingBookSold` from `release-outcomes.csv` (releases with final outcomes). "Charted"
means the record appears with `currentPosition > 0` in `records.csv`, which holds exactly the 100 chart rows
each week. **`first-chart-events.csv` is per label, not per record. Do not use it as the charted set.**

Measured the other way, by week: records sell only **49-58% of single units during the weeks they are on the
Hot 100** (`chartUnits` summed from `genre-decade-shape.csv`, over `singleUnits` from
`decade-annual-rollup.csv`). The share falls across the run, from 55% and 58% in 1960 to 49% and 53% in 1964
(seeds 1001 and 2002). Total single units are 159-172M a year. Note that `marketUnits` in
`genre-decade-shape.csv` includes LPs, so it is the wrong denominator for this.

**Historical comparison (inference, needs checking).** A comment at `Systems/ChartSimulator.cs:749-760`
says a historical #1 sold about 150,000 copies a week and #100 about 7,200. At the Hesbacher rank curve,
that makes the Hot 100 about 3.23M units a week. The same comment puts 148M singles a year about 22% under
real 1960 US volume, which implies about 190M a year, or 3.65M a week. On those figures the Hot 100 took
roughly **88% of all single units in its chart weeks, against the model's 49-58%**. Off-chart sales,
including those of future and former hits, would be around 12% rather than 42-51%. Both figures are
unsourced code comments. Verifying them is the first research task. If they hold, the problem is clear:
**the model sells three to four times too many records off the chart.**

Release volume points the same way. The model releases about 3,600-3,700 singles a year. Billboard reviewed
5,797 singles in 1960, 6,036 in 1961 and 6,690 in 1962 (sources in the hit-tail handoff). So the model has
about 60% of the historical release count selling something like 85-90% of the historical units. Each
release sells too much. Release counts feed supply and genre shares, so treat "more releases competing for
the same attention" as a question for the research, not a lever to pull.

## 2. Why a direct cut broke things

The opt-in probe `--uncharted-single-exposure=F` (`Systems/UnchartedSingleExposure.cs`) cut the purchase
exposure of uncharted singles. Results are in the loss-rate write-up. In short:

- **Chart breadth.** Records enter the Hot 100 by building sales while uncharted. Cutting every uncharted
  week also starved the challengers, costing up to a third of distinct Hot 100 acts.
- **Small-label survival.** Cutting only stalled records kept breadth, but losses saturated near 12% and
  five-year Small closures rose to 36-40%. In this economy Small labels live on non-charting singles: one
  seed-1001 window had 24 charting singles out of 996 for that tier.

Historically, small labels lived on occasional regional hits, and most of them failed. They did not live on
steady flop income. The fix needs both halves: **flops sell less, and small labels get an earned way to
survive.**

## 3. Question 1: are singles handed infinite ears?

From reading the code, largely yes. Attention is not a shared, finite resource except on radio.

- **Each record's demand is computed alone.** `CalculateRegionalSales` (`ChartSimulator.cs:427`) multiplies
  the region's buying population by the record's own awareness and conversion. No term depends on how many
  other records are competing that week.
- **Awareness grows for everyone.** `UpdateAwareness` (`ChartSimulator.cs:1142`) gives every new single a
  launch awareness of 0.04 plus artist heat, then organic growth of `0.012 x quality` a week. The headroom is
  each record's own `1 - awareness`, not a share of a listener's attention. A market with 3,000 live singles
  grows awareness for each as if it were the only one.
- **Saturation is inert.** Potential audience is about 12M per record, scaled by region. The comment at
  `ChartSimulator.cs:246-251` measures a hit's median penetration at its sales peak at 0.3% of that
  audience. Nothing is used up.
- **The only shared limit is proportional.** Market clearing gives each region a weekly purchase capacity
  (buying population x 1.34, plus an LP channel) and, when demand exceeds it, scales every record's demand
  by the same factor (`AllocateProportionalLocal`, `ChartManager.cs:1511`). A proportional ration keeps the
  shape of demand, so it cannot make sales more top-heavy. **Whether it binds at all is unmeasured.**
  `market-clearing-weekly.csv` is written only outside `--calibration`, and every loss-rate run used
  `--calibration`, so those files are header-only.
- **Radio is the only finite-slot channel.** Station playlists, the reporting panel, station drops and the
  airplay convexity (`AIRPLAY_CONVEXITY = 5`) behave like scarce attention. About 58% of chart points come
  from airplay, but radio feeds sales only through `radioHeat` in the discovery term.
- **Uncharted records get #100's exposure.** `GetChartExposureWeight(0)` treats an uncharted record as the
  bottom of the chart, and the uncharted discovery signal is 0.40-0.95 of #100's.

**Working hypothesis.** A period buyer had a limited budget of attention: a station played 30-40 current
records, a jukebox held a fixed number of slots, a store racked a limited number of titles, and a teenager
bought a few singles a month. If discovery is rationed through finite slots like these, allocated
competitively by appeal, demand becomes top-heavy on its own. Weak records go unheard and sell very little,
strong ones take the slots, and ordering among uncharted records is preserved because the same appeal ranks
them. Total units can be held, because the budget is the conserved quantity. Test this hypothesis; do not
assume it.

## 4. Question 2: can the cities carry regional attention?

The pieces exist but the AI economy does not use them.

- `Systems/DistanceModel.cs` defines **31 cities in 7 regions**, each with a parent region, a hub flag and a
  distribution tier.
- **AI demand runs only at region level.** Cities are used for radio station names
  (`StationNetwork.PickStationCityName`, `Systems/Radio/StationNetwork.cs:443`), for distances, and for the
  player's costs. `Data/CityProfile.cs` says explicitly that the AI economy does not read it.
- **Regions agree far too much.** Sixty-five cached station surveys (WLS, KRLA, KDWB, Oct 1963-Feb 1964) show
  a 40-45% overlap between cities' top records. The model's regions overlap 82-91%, with no distance
  gradient (`SimTools/analyze_survey_overlap.py`, `analyze_regional_sync.py`). A defensible region-level target
  is 45-65%, with neighbouring regions above coast-to-coast pairs.
- **Regional arrival attempts hit a catch-22.** A region a record has not reached has no stock and no
  awareness, so any trigger that reads that region's own sales never fires. Every arrival variant tried so far
  raised Small-label bankruptcies (see `RegionalSyncDesign.md` section 6c).

Historically, a regional hit was born in a city: a local DJ, a local distributor, a local scene. A city that
has its own finite attention (its stations' playlists, its stores' racks) would let a small label's record
win locally without winning nationally. That is the regional hit small labels need, and it would also lower
overlap. Questions to answer:

- Can regional hits be **earned** this way, so that a good small-label record wins its home city and a weak
  one does not? Closures must come from weak records, not from the mechanism.
- What does it cost to run? Regional demand is computed per record per region per week. Thirty-one cities
  would be about 4.4 times that work if done naively, and a decade run already takes about 2.5 hours. A
  cheaper design keeps sales and clearing at region level and puts only discovery, the attention state, at
  city level, perhaps only for records that are not yet national. `SimulationPerformanceProfiler` reports the
  `RegionalDemand` and `MarketClearing` phases separately.
- Does it fix the lockstep without breaking national reconciliation? Region totals must still sum to
  national sales.

## 5. Question 3: historical research to undertake

Separate measured fact from inference in everything you write, as the loss-rate write-up does. For each
target give the source, what it measures, and a confidence. Questions, in priority order:

1. **How single units were distributed.** Total US single units per year, 1960-69 (RIAA shipments, trade
   estimates). Weekly sales by chart rank, especially #1 and #100: check the code's 150,000 and 7,200 and
   find their source. What share of units went to Hot 100 records, to "Bubbling Under" records and to the
   rest.
2. **What a non-charting single sold.** Typical first pressing runs by label type, reorder behaviour, and
   regional sales of records that never charted nationally. Look at trade press (Billboard, Cash Box, Record
   World on worldradiohistory.com), label histories, distributor and one-stop accounts.
3. **Break-even and how money was lost.** Session costs, pressing cost per unit, artist and mechanical
   royalties, distributor margins, promotion and DJ costs, and the copies needed to break even for a major
   and for an independent. **Returns are a key question.** Singles were often sold with generous return
   privileges, so a flop could lose money through overpressing and returns, not only through low sales. An
   earlier static replay found that 30% returns on top of dealer pricing took the failure rate to about 66%.
   Today only about 4% of pressed copies go unsold. Check `Systems/LabelPressingEconomics.cs` and the
   `--pressing-initial-forecast-share` and `--pressing-repress-weeks` switches. Establish whether historical
   losses came mostly from small sales or from pressing ahead of demand.
4. **How small labels survived.** Regional hits, leasing masters to larger labels, publishing income,
   studios and side businesses, and how often they folded. This decides what the survival path should be.
5. **Attention capacity**, to size any budget: current records on a Top 40 playlist, stations per market,
   jukeboxes and slots per jukebox, titles a record store or rack carried, and teenage record spending a
   week. Also how quickly a record spread from one city to the next.
6. **Regional hits.** Documented cases of records that were big in one city or region and minor nationally,
   with sales where available. The cached city surveys are a starting sample. Widening them (WABC or WMCA,
   KHJ, a Southern station, a summer window) was already recommended.

Write the results to a new `SimTools/DemandAttentionResearch.md`.

## 6. What must not move

These are accepted results. Report every one of them for every arm you screen.

- **Genre market shares and year-end slots.** Total share error is about 18 points. Shares follow eligible
  supply closely. A demand change that is not genre-neutral will show up here.
- **Hot 100 breadth and dynamics:** distinct acts per year, debut positions, chart life, #1 tenure. Chart
  slot-weeks are fixed at 52,100 a decade by the hundred slots, so records charting and mean chart life
  trade off against each other.
- **Total single units**, and the LP unit share (about 30% in 1960).
- **Starting-Small five-year closures within 20-35%** (default 31.6% and 28.7%). Closures must be earned by
  weak records, never imposed by a quota or by an unrealistic wall. Before tuning any sell-through lever,
  stratify outcomes by the record's true quality and check that a good record from a careful label still
  has a fair chance.
- **Major-family chart share.** It has a calibration of its own and moves easily (the A2-A4 stack cost
  12-18 points in 1962).
- **Genre acceptance inside the airplay exponent is load-bearing.** Dividing it out inverts the chart.

Engineering rules:

- New behaviour goes behind an opt-in switch, read the way existing switches are (see
  `UnchartedSingleExposure.cs` for a small example). With the switch off, prove the run is byte-identical to a
  control on the finance, records and release-strategy streams.
- **One extra RNG draw reshuffles every later stream.** Use keyed draws rather than the shared sequence, and
  compare runs through the ledgers rather than aggregate error.
- V3 is a protected baseline.

## 7. Dead ends already measured

Do not repeat these without a new reason:

- A flat cut to uncharted exposure, at any strength (loses chart breadth).
- Cutting only stalled uncharted records (saturates near 12% losses and kills Small labels).
- Cost-side levers alone (the A2-A4 stack moved losses by less than a point).
- Tuning saturation or age decay to shape the sales curve (they are not what drives it).
- Regional arrival triggers that read a held region's own sales (unreachable), and the hearsay and one-stop
  variants (more bankruptcies, uniform lag rather than regional timing).
- A convex release ramp, tried twice, and a linear airplay term (both documented in `ChartSimulator.cs`).

## 8. Suggested plan

**Phase 0: research.** Answer section 5 and write `DemandAttentionResearch.md`, with a target and a
confidence for each metric in section 9. Stop and report if the evidence contradicts the premise, for
example if non-charting singles historically did take about a third of units.

**Phase 1: measure the model.** Run a default 105-week pair **without `--calibration`**, so that
`market-clearing-weekly.csv` and `single-demand-stages.csv` are written (the latter is large, about 1.8 GB a
decade). Establish:

- whether and how often the regional purchase capacity binds (`rationingFactor`);
- where a non-charting single's units come from: by week of life, region, label tier and awareness source;
- how concentrated weekly sales are among live uncharted singles (top-decile share, Gini);
- how much of a Small label's income comes from non-charters, and from its home region.

**Phase 2: design.** Write up options with what each predicts and what it risks, and recommend one. The
candidates suggested so far, which are not exhaustive:

- **A finite attention budget per market**, allocated competitively by appeal and replacing per-record
  awareness growth for uncharted records. Keep the budget conserved so total units hold.
- **City-level discovery**: each city's stations and stores have finite slots, and records win cities before
  regions. Sales and clearing stay at region level.
- **Reshaped uncharted demand**: a steeper quality exponent on conversion, or uncharted exposure scaled by
  appeal rather than cut flat. This is the smallest change, but it does not touch airplay.
- **Pressing and returns risk**, if the research says that is how money was lost.
- **Moving the lost units to hits**, since the #1 currently sells far below its historical volume (the
  code's own note: 28,838 against about 150,000 before the exposure curve; about 90,000 after).

Get the user's agreement on the design before building a large overhaul.

**Phase 3: prototype and screen.** Build behind a switch. Screen on seeds 1001 and 2002 at 105 weeks for the
chart and loss metrics, then 265 weeks for closures. Use `SimTools/RunCorporateEconomyAudit.ps1` with
`-Profile game -Modes default -Weeks 105` (or 265) and `-ExtraFlags` for your switch, or the console command:

```powershell
$godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=105 --run=<name> --seed=1001 --calibration --small-label-finance
```

Build with `dotnet build 'Label Man.sln'` first, since headless Godot loads `bin/Debug`. Reuse the existing
controls (`SimLogs/rj1-base`, `SimLogs/rj5-def`) instead of rerunning them. Analyse with pandas through
`%LOCALAPPDATA%\Programs\Python\Python314\python.exe`; the bare `python` command is a Store stub.

**Phase 4: report back** with the results table, the recommended arm and what a decade check would need to
cover. The user decides on any decade run and on adoption.

## 9. Metrics for every screen

| Metric | Source | Direction |
|---|---|---|
| Loss share, singles and LPs | `release-outcomes.csv` `realizedNet`; all-in with marketing via `analyze_release_economics.py` | toward the researched target |
| Non-charting single median sales, by tier | `release-outcomes.csv` joined to `records.csv` | down, mostly below break-even |
| Share of single units sold in chart weeks | `genre-decade-shape.csv` `chartUnits` / rollup `singleUnits` | up, toward the researched share |
| Units share to never-charting singles | as in section 1 | down, toward the researched share |
| Distinct Hot 100 acts per year | `chart-breadth.csv` `hot100Acts` | hold |
| Starting-Small five-year closures | the audit runner's `review.json` | hold within 20-35% |
| Total single units, LP unit share | `decade-annual-rollup.csv`, `format-mix.csv` | hold |
| #1 weekly sales | `records.csv` (position 1 rows) | up, toward the researched level |
| Genre share and slot error | `genre-decade-shape.csv`, `year-end-hot100.csv` | hold |
| Major-family chart share | `concentration.csv` `majorFamilyChartShare` | hold |
| Regional top-11 overlap (if you touch regions) | `analyze_regional_sync.py` | down, toward 45-65% |

Several per-week files (`market-clearing-weekly.csv`, `genre-market-weekly.csv`, `weeks.csv`) are empty under
`--calibration`. The sources above are all written in calibration mode.

Two-seed short runs are noisy: pop-genre shares move 3-4 points between seeds on their own. Report both
seeds, not just the mean, and treat small deltas as noise.

## 10. Closure report

Return the research document, the measurement results from Phase 1, the design note, every screen's metrics
table (both seeds), the byte-identity proof for each switch when off, the exact commands and the assembly
hash. State plainly which numbers are measured history, which are inference, and which targets remain
unverified.
