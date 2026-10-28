# Why the regions move in lockstep (audit step 3, follow-up design)

**Status: investigation done 2026-10-15; 5a–5d built behind switches (off by default), first seed-1001 checks in §6c.** Follows
[RegionalAudienceDesign.md](RegionalAudienceDesign.md), whose sub-steps 3a, 3b, 3c, 3f and 3g are built and on, and none
of which moved the timing metrics. This document finds out why and proposes the change that would.

Tools (all in `SimTools/`): `analyze_regional_separability.py` (new; two-way decomposition below),
`analyze_regional_sync.py`, `analyze_regional_guards.py`. Probe switches (diagnostic only, never scene defaults) live
in `RegionalAudience`: `--probe-sync-no-floors`, `--probe-sync-no-blend`, `--probe-sync-regional-awareness-only`
(both), `--probe-sync-late-uncovered`.

## 1. The finding in one paragraph

Every single launches in all seven regions in week 1. Every region then runs the same linear dynamics, driven by one
national heat series through fixed regional gains, and 40% of every region's awareness is the national stock itself.
A system like that can give regions different **levels** but cannot give them different **timing**. Regional demand
is 99.8% `region gain × common week shape`. The small regional variation the metrics did show (the 1-week peak
spread, 13% of top-11 lists differing) comes almost entirely from **stockouts**, after demand. So the local state
built in 3a–3g has been reading inventory noise. Two things hold the regions together, and each is enough on its own:
**the simultaneous launch** and **the national awareness blend**. Removing either alone does almost nothing. Removing
both breaks the lockstep (local share of weekly change 1.25% → 26%). The design below replaces both with national
information that **arrives region by region**, and keeps the national audience that independents depend on.

## 2. Measurement: demand is separable

For each single sold in all seven regions for at least 8 weeks (≥ 20k units), each log series `X[r, t]` is fitted as
`μ + α_r + β_t + ε`: a region gain, a common week shape, and an interaction. Only `ε` can move a record's timing or
rank in one region relative to another. Weighted by units, from `local3g-1001-104` (current HEAD behaviour, 3,444 records):

| Stage | Region gain α | Common week β | Interaction ε | Share of demand ε carried |
|---|---:|---:|---:|---:|
| Raw demand `lnD` | 28.0% | 71.8% | **0.20%** | 100% |
| Awareness (aware / potential) | 5.9% | 91.4% | 2.7% | 87% |
| …baseline (effective) awareness | 9.0% | 86.6% | 4.4% | 81% |
| …chart signal | 0.1% | 99.7% | 0.2% | 6% |
| Intrinsic conversion | 0.7% | 99.3% | 0.02% | 13% |
| Potential audience | 100% | 0% | 0% | — |
| **Settled units** `lnCleared` | 22.9% | 73.9% | **3.2%** | — |
| …settled ÷ demand (supply) | | | 50.3% of its variance; ε var **0.090** vs demand's **0.005** | −52% |
| …inventory fulfilment | | | 59.3%; ε var 0.091 | −54% |
| …market rationing | | | 7.2%; ε var 0.002 | +3% |

The control (`ref-781a445`, before any of step 3) is the same: demand interaction 0.13%, supply interaction var 0.091.

- **Raw-demand peaks are simultaneous.** Median spread of a record's regional peak week is **0 weeks** on raw demand
  (0.96% spread ≥ 4 weeks), against 1 week on settled units (7.0% ≥ 4). The 1-week figure the design tracked is a
  stock artefact.
- **Stockouts are the dominant regional signal.** One third of record-region-weeks are stock-bound (fulfilment < 0.98;
  7–8% below 0.5). Their region × week variance is **18×** the demand interaction, and it is *negatively*
  correlated with it: a region runs short exactly when its demand is locally high. Every local reader built so far —
  `localPeakUnits`, `localSalesRank`, the 3c support ratio, the 3f sales standing, 30% of breakout volume — reads
  **cleared units**, so each mostly measures restock timing.
- **Arrival is simultaneous.** Every single has demand in all 7 regions in week 1; each region reaches 10% of its own
  peak demand at mean age 1.18–1.31; across a record's regions that arrival spread has median **0** and is never
  ≥ 4 weeks. So 3a's `localIntroAge` is week 1 everywhere, and 3b's local clock is the national clock **by
  construction**. That is why 3b was inert, not a tuning miss.

## 3. Mechanism: why the model can only produce lockstep

Reading `ChartSimulator.CalculateRegionalSales` and `ChartManager.UpdateRecordRegionalData`, every term in a single's
regional demand is one of three kinds:

| Kind | Terms | Effect on timing |
|---|---|---|
| **Common time series** (one value per record-week) | release ramp and radio build (record age), national rank → chart exposure, chart signal, awareness floors; momentum; national `radioHeat` (radio signal, rotation target); the 0.4 × national awareness blend; demand-age decay (local since 3b, but local age = record age because arrival is week 1) | Sets β_t. Identical everywhere. |
| **Static regional gain** (fixed per record × region) | population and buying share, genre acceptance (annual), launch factor (strong 1.35 / covered 0.55–1.0 / uncovered 0.12–0.30), radio difficulty, genre radio opportunity, sentiment, distribution difficulty, potential audience | Sets α_r. A level, never a time. |
| **Linear filter with identical constants** | regional radio `Lerp(0.92·radio, heat/difficulty × genreRadio × build, 0.15)`; regional awareness `0.92·aw + 0.12·radio` | A linear time-invariant filter of a common input times a static gain returns the same shape, scaled. Only the launch initial condition (∝ launch factor) adds a transient, gone in ~2–3 weeks. |

Only four terms are region-local **and** time-varying: market exhaustion (weak: conversion interaction 0.02%), stock and
shelf capacity (large, but after demand), local breakout discovery for uncharted records, and the 3c/3g local radio
terms (weight 0.35 on a common base target). A local feedback loop can amplify a regional difference, but only one
that already exists. With a simultaneous start and 40% shared awareness, there is none to amplify.

The reporter panel (`StationNetwork.Playlist`) is the model's only discrete add process. But its candidacy multiplies
the **national** sales-support ratio, **national** `radioHeat` (heat pull) and the **national** top-40 trade pick.
Only format, DJ taste noise and freshness are local, so stations in every region add a record in the same week too.

## 4. Probes: which synchronizer is load-bearing

Seed 1001, 52 weeks, each against `sync-ctl-1001-52` (HEAD `a5ec776`). Metrics are as in `analyze_regional_sync.py`;
"raw peak" is the peak spread on raw demand.

| Arm | Demand ε | Local share of weekly change | Top-11 overlap | Settled spread ≥4 / ≥8 | Raw peak ≥4 / ≥8 | Units | Major share | Closures / bankr. |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Control | 0.23% | 1.25% | 86.6% | 10.6% / 0.29% | 1.4% / 0.1% | 152.5M | 55.7% | 76 / 16 |
| No floors | 0.22% | 1.23% | 84.9% | 10.4% / 0.00% | 0.9% / 0.1% | 152.0M | 56.3% | 74 / 17 |
| No national blend | 1.20% | 6.74% | 86.5% | 10.7% / 0.18% | 20.0% / 1.0% | 141.7M | 60.7% | 91 / 25 |
| Both (regional only) | 1.07% | 6.53% | 80.3% | 16.2% / 0.73% | 22.2% / 1.9% | 135.0M | 68.9% | 85 / 25 |
| Late uncovered (blend kept) | 0.34% | 1.61% | 87.5% | 9.3% / 0.15% | 1.6% / 0.0% | 152.1M | 54.0% | 88 / 22 |
| **Regional only + late uncovered** | **4.90%** | **26.3%** | **78.9%** | **25.1% / 0.92%** | **37.8% / 20.0%** | 134.3M | 70.7% | 97 / 28 |

"Late uncovered": a single launches with zero regional awareness and rotation in regions its label does not ship to.

Read:

1. **The floors are not a synchronizer.** They bind on only 0.6% of rows but carry 17% of all demand and 36% of
   charted-record demand (the hits live on them), which is why the first design underrated them. But removing them
   changes nothing, because a hit's effective awareness is already nearly common without them. 3e stays a semantic
   cleanup.
2. **The national blend synchronizes demand timing**: removing it multiplies demand interaction 5× and spreads
   raw-demand peaks ≥4 weeks from 1.4% to 20%. Settled peaks and overlap barely move, because ranks are set by
   levels (98% common), and settled peaks are blurred by stock.
3. **A late start does nothing while the blend stays.** A region with zero local awareness still gets 0.4 × national
   from week 1, and national heat fills its rotation within ~1 week. "Uncovered" today means *smaller*, never *later*.
4. **Together they break the lockstep.** Arrival spread goes from median 0 to **4 weeks**; Rockies arrive at mean
   age 3.1 against East Coast 1.9.
5. **The cost is real and is the design constraint.** Taking the national blend away everywhere costs 7–12% of single
   units and hands Majors 5–15 points of share: the national stock is what gives an independent's record an audience
   outside the regions it ships to. The fix must keep that audience, and only change **when** it reaches each region.

## 5. Design

Principle: **national information reaches a region through that region's own channels, and only once the record is
there.** A record is "there" when it is shipped, serviced to stations, or picked up by a one-stop or rack jobber. A
national campaign that ships everywhere stays simultaneous, as it historically was. The independents' regional break
— local first, neighbours next, national only if it moves — becomes possible.

### 5a. Regional arrival as an event (`RegionalAudience.RegionalArrival`)

- New saved per-region field `arrivalAge` (−1 = not arrived). Arrival at launch (age 1) in every region where the
  label ships the record (`HasDistributionInRegionForRecord`), its strong regions, and the act's home region (3g).
- Elsewhere the record **arrives** the week any existing local-access path fires, all already causal and draw-free:
  the neighbour-market test (`neighboringMarketTestStrength ≥ 0.08`, the gate `ChartManager` already counts as a
  test market), the region's own breakout score crossing the discovery gate in `CalculateRegionalSales` (`> 0.24`),
  rack-jobber access (`GetRackJobberAccess > 0`), or the record entering the national
  top 40 (one-stops racked the national list; a lag of `ArrivalLagWeeks = 1` stands for shipping).
- Before arrival: no launch stock, launch awareness or launch rotation in that region (today it gets them at 0.12–0.30
  scale). After arrival, the launch seed is applied at arrival, scaled as today, and stock restocks normally.
- `localIntroAge` (3a) becomes `arrivalAge` where set, so 3b's local clock finally has a local start.

### 5b. The national blend is gated by arrival, not removed

`effectiveAwareness = arrived ? 0.4·national + 0.6·regional : 0.6·regional`. Before arrival a region sees only what its
own breakout and neighbour deposits gave it. After arrival the national stock reaches it as it does today. Because
covered regions are unchanged, the probe's −7% volume and +5 pts Major share should mostly not recur. The loss is
bounded by the indie volume that today sells in regions the label does not ship to, before the record has proven itself.

### 5c. Rotation targets are gated by arrival

A region's rotation target (`targetRegionalRadio`, the reporter panel's candidacy) is 0 before arrival: a station
cannot programme a record it has not been serviced. Without this, national heat fills a not-yet-arrived region's
rotation in a week (probe 3), and arrival becomes an artefact of the radio lerp, not an event.

### 5d. Local signals read demand, not inventory

`localPeakUnits`, `localSalesRank`, the 3c support ratio and the 3f sales standing switch from cleared units to
**serviceable intent** (`serviceableIntentThisWeek`, before the stock cap), so they measure buyers, not restocks.
Breakout volume keeps its 70/30 raw/fulfilled split. This is a correctness fix independent of 5a–5c, and it should
ship first: on today's data it would remove most of the measured "local" signal (the 18× stock term) and expose the
true baseline.

### 5e. Reporter candidacy reads the region's own sales (optional, after 5a–5d)

In `StationNetwork.Candidacy`, `salesSupport` uses the station's **region's** support ratio (demand-based, per 5d)
instead of the record's national one. `heatPull` and the top-40 trade pick stay national: they are the bounded
national information channel. This changes RNG consumption (candidacy noise is drawn per candidate), so it gets its
own short check.

### What is deliberately not done

- No regional taste multiplier and no random per-region delay. Every arrival comes from an existing causal path
  (distribution, neighbour test, breakout, rack jobber, chart), audit §7.
- The national awareness ceiling, recognition and the album path are untouched. Albums are not shown to be
  synchronized for the same reason and are out of scope until measured.

## 6. Order, switches, checks

1. **5d** alone (`LocalSignalsReadDemand`), 104-week check vs HEAD. Expected: guards unchanged (it only re-reads
   inputs to 3b/3c/3f), sync metrics on settled units unchanged, local readers no longer track stockouts.
2. **5a + 5b + 5c** together (`RegionalArrival`): the probes show the pieces only work jointly. 104-week check vs (1).
3. **5e** separately.
4. Every check reports **both** settled and raw-demand peak spread (add the raw spread to `analyze_regional_sync.py`),
   plus arrival spread.

Guards (unchanged from the first design, plus one): single units ±3%, airplay share of top-40 points ±3 pts,
Hesbacher #10/#1 and #100/#1, #1 count and tenure, genre share error, closures and bankruptcies, and **Major-family
share within ±3 pts of the control**. That last one is the probe's warning: if 5b costs independents too much, the
first lever is `ArrivalLagWeeks` and the top-40 arrival trigger (earlier national pickup), never a demand multiplier.

Targets: as in the first design (local share of weekly change ≥ 10%, top-11 overlap 70–85%, settled peak spread median
3–5 weeks, ≥ 8 weeks 5–15%). The probe that broke the lockstep (26%, 78.9%, median 2, 0.9%) shows the overlap and
change targets are reachable. The ≥ 8-week settled spread is still under target even there, so it may need 5e or be
the wrong target for a 1960s single's 8–10-week life.

## 6b. As built (2026-10-15)

Switches (scene exports on `ChartManager`, off by default; CLI `--[no-]local-signals-read-demand`,
`--[no-]regional-arrival`):

- **5d** `LocalSignalsReadDemand`: `RegionalAudience.LocalSignalUnits` returns the week's **raw demand**
  (`rawDemandThisWeek`), not `serviceableIntentThisWeek`. Correction to 5d above: serviceable intent is already
  capped by `unitsInStores`, so it *is* the inventory-fulfilment term (54% of the settled interaction); only raw
  demand is before the stock cap. Readers switched: local intro and peak (3a), the region's sales list (3c), the 3c
  support ratio and local heat, and the 3f sales standing (soft and hard paths). Breakout volume is unchanged (70/30).
- **5a–5c** `RegionalArrival`: new `RegionalRecordData.awaitingArrival`, `arrivalAge`, `pendingLaunch{Awareness,
  Radio,Stock}` (saved with the full world; player saves never hold one because player records are exempt).
  `RegionalAudience.HoldUntilArrival` runs after each live single launch (AI labels and the chart's own launcher):
  arrival at once where the label ships, in its strong regions and in the act's home region; elsewhere the launch
  seed already drawn is held, so the launch RNG stream is unchanged. `TryArrive` runs each week after the breakout
  pass and before restock. Triggers in order: national top 40 on the latest chart, neighbour test ≥ 0.08, own
  breakout > 0.24, rack-jobber access on **regional proof** (`GetRackJobberAccess(0, peakBreakoutScore)`; the
  chart half of rack access is the top-40 trigger, otherwise any Hot 100 entry would ship everywhere). The held
  seed lands on arrival, and `localIntroAge` takes the arrival age.
  Before arrival: effective awareness is `0.6 × regional` with no chart floors (5b), no restock, a zero rotation
  target, and no reporter station can add the record (5c). Albums and player records are never held.
  The 1-week lag is structural: the pass reads last week's chart and the seed reaches demand the week after.
- `analyze_regional_sync.py` now also reports `rawTiming`: raw-demand peak spread, arrival spread (first week at
  10% of the region's own peak) and mean arrival age by region. Console line `REGIONAL_ARRIVAL` counts triggers.

## 6c. First checks (seed 1001, 2026-10-15)

Inertness: switches off, 52 weeks (`arr-off-1001-52`) is byte-identical to `sync-ctl-1001-52` on
`year-end-hot100`, `decade-annual-rollup` and `concentration`.

104 weeks against `local3g-1001-104` (HEAD behaviour). `sig5d` = 5d alone; `arr` = 5d + 5a–5c.

| Metric | Control | 5d | 5d + arrival |
|---|---:|---:|---:|
| Local share of weekly change | 1.30% | 0.94% | **29.8%** |
| Local share of level | 3.5% | 1.8% | 3.6% |
| Raw-demand peak spread ≥4 / ≥8 | 0.9% / 0% | 0.5% / 0% | **40.8% / 14.7%** |
| Arrival spread ≥4 / ≥8 | 0.07% / 0% | 0.04% / 0% | **36.6% / 23.2%** |
| Settled peak spread median, ≥4 / ≥8 | 1, 9.1% / 0.28% | 1, 8.7% / 0.21% | **2, 23.4% / 7.2%** |
| Top-11 overlap | 88.2% | 87.8% | 86.2% |
| Mean arrival age, East Coast … Rockies | 1.49 … 1.71 | 1.48 … 1.70 | 1.83 … 3.05 |
| Single units 1960 / 1961 | 152.5M / 154.7M | +0.1% / −0.1% | −1.6% / +0.1% |
| Major share 1960 / 1961 | 55.7 / 50.0 | 51.6 / 55.0 | 53.3 / 57.3 |
| Airplay share of top-40 pts 1960 / 1961 | 17.0 / 17.4 | 15.6 / 15.3 | 17.9 / 17.6 |
| #10/#1 units 1960 / 1961 | 0.52 / 0.64 | 0.65 / 0.62 | 0.58 / 0.66 |
| Charted records / mean weeks on chart | 1063 / 7.54 | 1048 / 7.70 | 977 / 8.16 |
| Closures / bankruptcies | 148 / 44 | 153 / 51 | **177 / 65** |

Arrival triggers (`REGIONAL_ARRIVAL`, 104 weeks): 47,292 region-launches arrived at launch, **22,575 held**; of
those only **1,750 arrived** (7.8%): neighbour test 1,596, national top 40 154, own breakout **0**, rack on regional
proof **0**; mean arrival age 7.8 weeks.

Read:

1. **The lockstep is broken.** Every timing target the first design set is met or close: local share of change 30%
   (≥ 10%), settled ≥ 8-week spread 7.2% (5–15%). Overlap 86.2% is just above the 70–85% band; settled median 2
   against 3–5.
2. **5d is inert on the guards** as expected, but it moved Major share −4 / +5 pts by itself. That is the single-seed
   noise on this guard over two years, so the Major guard cannot be read on one seed. Against 5d, arrival moves
   Major +1.7 / +2.4 (inside ±3), units −1.7% / +0.3%, airplay +2.3 pts.
3. **Two of the four triggers are dead by construction.** A held region has no stock and almost no awareness, so its
   own breakout evidence, which is built mostly on its own volume, can never cross 0.24, and rack proof reads the
   same score. Only the neighbour test (driven by *other* regions' breakouts) and the national chart can deliver a
   record. So 92% of held region-launches never get the record. The unit cost is small because those regions sold
   little anyway (launch factor 0.12–0.30, stock access 0.18), but it is where the closure and bankruptcy rise
   (+24 / +14 against 5d) most likely comes from: independents lose their thin out-of-territory tail.
4. **Chart arrival is rare (154)**: most top-40 records come from labels that already ship everywhere.

Breakout volume already reads raw demand (70%), so "read unserved demand" is not a fix by itself. Measured on
`arr-1001-104`, first 12 weeks: 21,301 of 74,235 record-regions never cleared a unit. Their peak raw demand is
**0 at the median and 90th percentile, 4.3 units at the 99th**, and their baseline awareness is 0. The breakout volume
floor is 150 × region scale. Before arrival there is no unserved demand to read, because 5b removed the national
blend, and that blend is the only source of demand in a held region.

Open, for the author: a held region needs *some* pre-arrival demand before any of its own triggers can fire. The
candidate is a hearsay share of the national blend before arrival (`0.6·regional + h·0.4·national`, small `h`):
people hear of the record and ask for it, nothing is in the shops, so the demand piles up as backorders and feeds
the region's breakout volume and unmet-demand inputs until it arrives. It re-couples **raw** timing a little, but
settled sales stay at zero until arrival. The alternative is to accept neighbours and the chart as the only routes.
Either way, run a second seed before any scene default flips.

### Hearsay share, h = 0.25 (`arr3-1001-104`, built as `RegionalAudience.HearsayShare`)

Before arrival a held region's effective awareness is `0.6·regional + 0.25·0.4·national`. With no shelf stock, that
demand is all unserved (backorders).

| Metric | 5d | Arrival | Arrival + hearsay |
|---|---:|---:|---:|
| Held region-launches / arrived | — | 22,575 / 1,750 (7.8%) | 22,000 / 1,869 (8.5%) |
| …via chart / test / own breakout / rack | — | 154 / 1,596 / 0 / 0 | 40 / 854 / **975** / 0 |
| Held-region peak raw demand p50 / p90 / p99 (12 wk) | — | 0 / 0 / 4 | 27 / 117 / 287 |
| Local share of weekly change (raw demand) | 0.94% | 29.8% | 6.7% |
| Local share of level | 1.8% | 3.6% | 15.3% |
| Settled peak spread median, ≥4 / ≥8 | 1, 8.7% / 0.21% | 2, 23.4% / 7.2% | 2, 20.3% / 3.1% |
| Top-11 overlap | 87.8% | 86.2% | 86.8% |
| Single units 1960 / 1961 | 152.7M / 154.5M | −1.7% / +0.3% | −1.8% / −0.4% |
| Major share 1960 / 1961 | 51.6 / 55.0 | 53.3 / 57.3 | 48.0 / 51.5 |
| Closures / bankruptcies | 153 / 51 | 177 / 65 | 183 / 68 |

Read:

1. **The own-breakout route now works** (975 arrivals, from zero), but it mostly *replaces* the neighbour-test and
   chart arrivals instead of adding to them. The arrival rate goes from 7.8% to only 8.5%. Held demand sits well under
   the breakout volume floor (150 × region scale) for most records, so only the strongest reach the 0.24 gate.
2. **The raw-demand change metric falls back to 6.7%** partly by construction. Held regions now carry a common-shaped
   hearsay demand series that they did not have before, and the decomposition reads raw demand. The settled metrics
   are the fairer read: the ≥ 8-week spread halves (7.2% to 3.1%) and stays inside the ≥ 4-week band.
3. **No relief on closures or bankruptcies** (183 / 68), so the hearsay route is not what independents were missing.
   Their out-of-territory tail is still mostly never delivered. Major share swings ±4 pts between arms, the same
   size as 5d's own swing, so it is noise on one seed.

### One-stop order trigger (`arr4`, seeds 1001 and 2002)

A fifth trigger, checked last: a held region gets the record once its unfilled requests (`unitsBackordered`) reach
`OneStopOrderUnits` (100) × its demand scale. Hearsay stays at 0.25. Each seed is compared with its own 5d-only control.

| Metric | 5d 1001 | Arrival 1001 | 5d 2002 | Arrival 2002 |
|---|---:|---:|---:|---:|
| Held / arrived (one-stop share) | — | 22,617 / 16,500 (100%) | — | 20,561 / 15,000 (100%) |
| Mean arrival age of held regions | — | 4.6 wk | — | 4.7 wk |
| Local share of weekly change | 0.94% | **20.2%** | 0.95% | **20.3%** |
| Local share of level | 1.8% | 9.3% | 1.8% | 9.2% |
| Arrival spread ≥4 wk | 0.04% | 8.2% | 0% | 8.1% |
| Settled peak spread median, ≥4 / ≥8 | 1, 8.7% / 0.21% | 1, 10.8% / 0.82% | 1, 8.0% / 0.42% | 1, 10.5% / 0.21% |
| Top-11 overlap | 87.8% | 89.2% | 89.3% | 88.2% |
| Single units 1960 / 1961 | 152.7M / 154.5M | −1.2% / +0.3% | 154.3M / 157.8M | −0.3% / −0.8% |
| Major share 1960 / 1961 | 51.6 / 55.0 | 55.7 / 50.4 | 62.9 / 56.8 | 62.5 / 51.3 |
| Airplay share of top-40 pts 1960 / 1961 | 15.6 / 15.3 | 17.9 / 18.5 | 15.8 / 17.5 | 18.2 / 15.9 |
| Charted records | 1048 | 1081 | 1052 | 1029 |
| Closures / bankruptcies | 153 / 51 | 171 / 68 | 156 / 44 | 163 / 58 |

Read:

1. **Delivery is fixed.** 73% of held region-launches now arrive (from 8%), almost all by one-stop order, and
   the other triggers no longer get the chance: the order bar is reached first, at about week 4.
2. **The two seeds agree.** Local share of change is 20.2% / 20.3%, and arrival ages rise the same way, so the
   change is structural.
3. **But the lockstep is broken only in demand, not in the settled sales the targets are written on.** The ≥ 8-week
   settled spread is 0.8% / 0.2%, against 7.2% for the no-route arm and a 5–15% target. Overlap goes *up* (89%). A
   fixed ~3-week delay for nearly every held region is a uniform lag: it shifts the uncovered regions together
   instead of letting each one find the record on its own evidence. The order bar is too low, and its trade-off
   against the no-route arm is now bracketed: no route gives timing without delivery, a 100-unit bar gives delivery
   without timing.
4. **Bankruptcies rise on both seeds** (+17, +14; closures +18, +7) in every arrival arm so far. That is now a
   consistent sign, not noise. The likely source: independents lose the first weeks of their out-of-territory sales,
   where the launch stock used to sell. Major share is mixed (±5) and not readable.

### Who goes bankrupt (`analyze_arrival_bankruptcies.py`, arr4 against 5d, both seeds)

| | 5d 1001 | Arrival 1001 | 5d 2002 | Arrival 2002 |
|---|---:|---:|---:|---:|
| Bankrupt labels | 51 | 70 | 44 | 58 |
| …that never sold a single | 36 | 43 | 32 | 34 |
| …that sold singles | **15** | **27** | **12** | **24** |
| Bankrupt sellers in the Small tier | 9 | 23 | 10 | 18 |
| Small-tier single units (sellers) | 13.6M | 11.7M (−14%) | 14.8M | 10.9M (−26%) |
| Small-tier median units per label | 66.1k | 41.0k (−38%) | 65.2k | 46.5k (−29%) |
| Independent-tier single units | 142.5M | 145.2M (+2%) | 126.4M | 137.0M (+8%) |
| Boutique-tier single units | 30.6M | 32.7M (+7%) | 41.4M | 43.5M (+5%) |

1. **The whole excess is Small-tier labels that sell singles: +12 bankruptcies on each seed.** Labels that never
   sold a single go bankrupt at about the same rate in every arm (+7, +2), so they are not the effect.
2. **It is lost revenue, not stranded stock.** COGS is booked per unit sold (`CalculateRegionalCogs`), so held launch
   stock costs a label nothing. The Small tier loses 14–26% of its single units and about a third of the median
   label's, and its out-of-home share falls. The Independent and Boutique tiers *gain* (+2–8%): the volume moves up
   to them rather than to the Majors.
3. **Coverage alone does not pick the victims.** Bankrupt sellers sit at a median of 2 covered regions in every arm,
   and the rise falls in different coverage bins on each seed (≤ 1 and 4–7 regions on 1001, 2–4 on 2002). What
   they share is size: a Small label's median life is about 65k units over two years, and the out-of-territory
   launch weeks are a large share of that.

So the order bar is not the main lever for this cost. A Small label's held region gets the record a few weeks late
and the launch weeks are gone, at any bar. Options: arrival at launch where the label has a one-stop or independent
distributor line (the Small tier's real channel), or keep the launch-week sales in held regions and gate only the
blend and rotation. Both switches stay off.

## 7. Risks

- **Independents' reach.** The main risk, measured above. 5b keeps the national stock wherever the record is, and
  the arrival triggers are the independents' historical routes (one city first, then the neighbours). The guard is
  Major share.
- **#1 tenure and slot-weeks.** Staggered regions mean longer national lives for regional breakouts. Slot-weeks are
  fixed (52,100), so expect fewer distinct charting records; watch the `chart-slot-weeks identity`.
- **Two-seed noise.** Single-seed short runs move closures by ±10 between identical arms; the verdict is the decade
  A/B at the end of step 3, as planned.

## 8. Decisions for the author

1. **Accept that uncovered regions get the record later, not just less of it.** That is the core of the change, and
   it costs some indie volume before arrival. Alternative: keep the current simultaneous launch and accept lockstep
   timing as a known limitation.
2. **Arrival triggers.** Recommended: the four existing paths plus national top-40 with a 1-week lag. A national
   top-10 auto-arrival would be safer for volume and weaker for the "regional break" story.
3. **5e (local reporter support)** now, or only if 5a–5d undershoot the overlap target. Recommended: only if needed.

## 9. 1960 station-survey benchmark and what the overlap is made of (2026-10-27)

**The model number moved.** Since 2026-10-21 the working bundle carries `--single-commercial-fit-regional=0.5`
(`SingleCommercialFit.Multiplier`, an independent keyed lognormal per record × region). In the office-opening runs
(`SimLogs/hs52op100{1,2}-SMP-HGRPO`) 1960 top-11 overlap between model regions is **63.8% / 60.4%**, not ~89%, and that
draw carries all of the record × region demand interaction among the national top 30. It has no geography: adjacent
regions overlap 64.4%, non-adjacent 63.2%.

**History, widened.** `SimTools/research_station_surveys.py` caches 200 1960 surveys from oldiesloon.com and
musicradio77.com: KFWB 53 and KRLA 52 (Los Angeles), KDWB 50 and WDGY 7 (Minneapolis), WABC 26 (New York, Jul–Dec),
WLS 12 (Chicago). No Southern station: ARSA holds them, but browsing its stations needs a member login.
`SimTools/analyze_station_survey_overlap.py` scores them (surveys within 3 days):

| Top-11 overlap, 1960 | Pairs | Mean |
|---|---:|---:|
| Same city, two stations (KFWB–KRLA, KDWB–WDGY) | 58 | **69.7%** |
| Cross city, station pairs | 245 | **40.3%** |
| Cross city, city lists (stations averaged by rank) | 132 | 42.4% |

No distance gradient in 1960: Chicago–Minneapolis (355 mi) 39%, Los Angeles–New York (2,451 mi) 40%; correlation
with miles −0.18 on six pairs, Chicago thin. The 1963–64 sample did show one (Chicago–Minneapolis 50%, Chicago–LA 27%).
So distance is not a 1960 target; at most a weak later one.

**The composition matters more than the level.** Each top-11 record missing from the other list, classified by where
it is in the other market (one station per city, KFWB/KDWB/WABC/WLS; same-city row is KFWB vs KRLA):

| Mismatched slot is, in the other list… | Cross city | Same city | Model regions, 1001 / 1002 |
|---|---:|---:|---:|
| in the top 40 the same week (**rank order**) | 53.8% | 82.9% | 90.8% / 89.2% |
| top 11 within ±6 weeks (**timing**) | 14.3% | 6.9% | 1.6% / 2.6% |
| top 40 within ±6 weeks only | 14.1% | 2.5% | 5.0% / 4.8% |
| absent within ±6 weeks (**repertoire**) | 17.8% | 7.8% | 2.5% / 3.3% |

- **Same-city disagreement is station noise,** ~3.3 of 11 slots, nearly all rank order. A model region is a sales
  aggregate, so it should carry none of it.
- **Net of that floor, cross-city lists differ by ~3.3 slots, and only ~25% of that is rank order;** ~20% is timing,
  ~25% top-40-only, ~30% repertoire. As a rough additive reading, a regional aggregate's top-11 overlap would sit near
  **65–75%**, made mostly of *different records and different weeks*, not reshuffled ranks.
- **The model has the level by accident and the composition wrong:** ~90% of its mismatches are rank order, the
  signature of an i.i.d. taste draw. It has almost no timing and no regional repertoire.
- **The repertoire exclusives read as regional genre taste and local labels:** R&B and dance records in New York and
  Los Angeles ("Finger Poppin' Time", King; "Hully Gully", Arvee), country-western and country-pop in Minneapolis
  ("Stranger from Durango", "My Shoes Keep Walkin' Back to You"), Chicago instrumentals and standards ("Rat Race",
  "Wabash Blues", "Hucklebuck"). Local breaks then spread: "Muleskinner Blues" (Soma, Minneapolis) was KDWB #1 before
  it reached LA #3 and New York #4. Classification is by title words; spelling variants can inflate "absent" a little
  (one was fixed: Muleskinner/Mule Skinner).

**What this means for the levers.**
1. **Remove the regional draw** (`--single-commercial-fit-regional=0`), keeping the national fit. Expected: overlap back
   toward ~89%, which shows how much of the level it was supplying.
2. **Timing** comes from arrival: the national blend and rotation gated by arrival (5a–5c). The open cost is the
   Small-tier bankruptcies (§6c), so arrival must give a label's own one-stop/independent lines the record at launch.
3. **Repertoire** comes from regional genre taste on single demand and radio (R&B, country, polka/standards by
   region), plus home-region and local-label breaks that spread to neighbours. Genre acceptance carries only 0.3% of
   the record × region interaction today, so its regional spread is the place to look.
4. **National radio heat** feeds every region's rotation target; once a region's own taste and arrival decide its
   adds, the remaining rank-order difference should come from them, not from a draw.

Score each step on the mismatch composition (`model_mismatch` logic, to fold into `analyze_regional_sync.py`), with
the target band rank ≤ 40%, timing ≥ 15%, repertoire ≥ 20%, level 65–75%. The old 45–65% band was set on station
lists, so it double-counts station noise.

### 9a. Probe: the regional commercial-fit draw removed (2026-10-27)

HGRPO with `--single-commercial-fit-regional=0` against the HGRPO control, seeds 1001-1002, 52 weeks, every other flag
the same (`SimLogs/hs52op100{1,2}-SMP-HGRPO-noreg`, consoles in `SimLogs/regprobe/`). Omitting the flag is not "off":
it defaults to half the national sigma (1.25). The fit draws are keyed, so the opening worlds match.
Scored with `SimTools/analyze_regional_mismatch.py` (all seven regions, 1960):

| | Overlap | Adjacent / non-adj. | Rank | Timing | Top-40 only | Absent |
|---|---:|---:|---:|---:|---:|---:|
| Control 1001 / 1002 | 63.8 / 60.4 | 64.4–63.2 / 60.8–60.0 | 89.4 / 88.4 | 2.0 / 2.4 | 5.7 / 5.7 | 2.8 / 3.5 |
| No regional draw 1001 / 1002 | **87.1 / 86.6** | 87.3–87.0 / 86.8–86.5 | 97.3 / 97.0 | 1.8 / 2.6 | 0.9 / 0.4 | **0.0 / 0.0** |
| History target (regional aggregate) | ~65-75 | none in 1960 | ≤ 40 | ≥ 15 | ~25 | ≥ 20 |

Guards (1960, control → probe, 1001 / 1002):

| | 1001 | 1002 |
|---|---|---|
| Units (records.csv) | 121.7M → 121.8M | 121.8M → 121.8M |
| Major share of units | 33.0 → 30.1 | 36.7 → 36.0 |
| Major share of Top 10 slots | 38.7 → 34.6 | 45.8 → 41.3 |
| Median units #1 / #10 / #100 | 74.4k / 43.3k / 4.05k → 56.3k / 37.9k / 4.00k | 65.0k / 42.2k / 3.79k → 65.3k / 44.3k / 3.75k |
| Distinct Top 10 artists | 84 → 81 | 76 → 93 |
| Single launches / insolvencies | 6,487 / 30 → 6,576 / 31 | 6,417 / 49 → 6,423 / 47 |

- **The draw was the whole level.** Without it overlap returns to the October lockstep (~87%), no record is ever
  absent from another region's top 40, and the rest is rank shuffle. Timing (~2%) is the same in both arms: the draw
  never supplied any.
- **It also handed Majors ~4 points of Top 10 slots on both seeds.** A sum over seven independent regional draws
  favours records with many draws above the line, which the widest-launched records get. Market units are unchanged.
- **Keep the draw for now.** Turning it off alone moves away from history (87% vs ~70%). It comes out in the same
  change that brings in timing and repertoire, and that change carries the Major-share guard.

### 9b. Regional genre taste today: three stacked flatteners (2026-10-27)

Measured on `hs52op1001-SMP-HGRPO-noreg` (1960, single demand stages joined to record genre) and from the code.

| Genre (national unit share) | Raw acceptance, max/min over regions | Demand multiplier, max/min | Realized regional unit index, max/min |
|---|---:|---:|---:|
| TeenPop (25%), R&R (16%), TradPop (11%), Doo-Wop (5%), Jazz (2%) | 1.00–1.01 | 1.00 | 1.07–1.11 |
| R&B (13%), Soul (4%), Blues (2%), Gospel (1%) | 1.04–1.07 | 1.02–1.04 | 1.11–1.25 |
| Country (11%) | **1.54** | 1.18 | 1.20 |

Only Country (and Tex-Mex) varies, through hand-authored `regionalFactor`s (Deep South 1.38, Great Plains 1.33,
Southwest 1.64, elsewhere 0.85). R&B is *lowest* in the Deep South (0.39 vs 0.41). The realized index spread is mostly
the 0.4 national blend's leftovers and stock, not taste.

Why, in order of where the information is lost:

1. **Segment capacities are nearly uniform** (`SegmentCapacityModel.Create`). Constant terms dominate: UrbanRnB runs
   2.2% (Plains, Rockies) to 4.8% (Deep South) of a region's buyers, CountryWestern 9.8–13.2%, MainstreamAM 30–33%
   everywhere. Even read proportionally, a genre's regional segment mass varies at most 1.07× (TeenPop) to 1.19×
   (Blues, R&B).
2. **Per-region centering cancels the segment mix exactly.** `Evaluate` routes each segment by
   `1 + 1.25 × (reach − regionMean)`, where `regionMean` is the region's own capacity-weighted reach; weighted by the
   same capacities it sums to 1 in every region (the 0.25 floor aside). So acceptance ignores who lives in a region.
   Centering on the national mean instead would leave ≤ 1.02× (offline, `scratchpad/segment_offline.py`), because of (1).
3. **The demand transfer compresses what survives.** `GetEnabledSingleDemandMultiplier` = `(0.6 + 0.5a) ×
   smoothstep(0, 0.5, a)`: above a = 0.5, a 1.5× acceptance difference becomes ~1.18× demand (Country's case).

**Size of the target.** A record that is top 11 in one region and outside another's top 40 needs roughly a 3× relative
demand difference (median units at #10 vs #40 differ ~3–4×). History puts ~1 such record in every top-11 pair
comparison (§9: ~30% of ~3.3 net mismatches). The 1960 exclusives are specialist records: R&B/dance in New York and
Los Angeles, country-western in Minneapolis, instrumentals/standards in Chicago. Mainstream teen and R&R records travel.
So the lever must be **strong and narrow**: multi-fold for specialist genres, near 1 for mainstream pop.

**Lever design, city-based (`RegionalGenreTaste`, `--regional-genre-taste=K`, off by default; revised 2026-10-27).**
Taste lives in places, not regions: Miami's Latin audience is real, but it is not the Deep South's.

- **(1) City audience profiles feed region taste.** Each of the 32 `DistanceModel` cities gets a 1960 metro
  population and three specialist audience shares: Black (UrbanRnB), country-western listening (CountryWestern) and
  Latin (RegionalLatin, effective-dated: Miami's rises with Cuban emigration after 1959). Each region gets a
  "rest of region" remainder for its towns and farms: population = region population − its cities, Black share
  derived so the region total matches `MarketRegion.blackPopulation` (floored), country and Latin shares authored.
  Every other segment is uniform (intensity 1).
  - Place mass for a genre: `m_p(g) = Σ_s w_gs × a_ps / ā_s`, with `w_gs` the genre's live `SegmentWeights` and `ā_s`
    the population-weighted national mean of segment s. By construction the national population-weighted mean of
    `m_p(g)` is 1 for every genre.
  - Region taste: `T_r(g)` = population-weighted mean of `m_p(g)` over the region's places, raised to `K` and
    renormalized so its national population-weighted mean is again 1. Secondary genre blends at 0.2, as acceptance does.
  - It multiplies single conversion after `GetEnabledSingleDemandMultiplier`, so the transfer does not compress it and
    the 0.4 national awareness blend does not dilute it. National genre totals hold to first order; acceptance,
    keyframes and the authored Country `regionalFactor`s are untouched (CountryWestern here is listening share by
    place, which the regional factor does not see, so the two are not the same term).
- **(2) City taste in station candidacy.** A reporter station's candidacy is multiplied by its own city's
  `m_p(g)^K` (same national normalizer), so a Miami station adds a Latin record that Atlanta's do not; that reaches
  Deep South demand only through the station's share of the region's panel. Candidacy draws its noise before the
  product, so the switch moves no draws.
- **(3) Later, own step:** home-city launch deposits that spread to nearby cities by distance (timing as well as
  repertoire).
- **Not done:** city-level demand markets. Stock, retail and settlement are regional; 7 → 32 markets would multiply
  demand work ~4-5×.

**Fixed inputs, no draws.** Every term is a region attribute already in `chart_manager.tscn`. Keep the commercial-fit
regional draw (decision 2026-10-27).

**Screen.** 52 weeks, seeds 1001-1002 against the HGRPO control, `K` ∈ {0.5, 1} (R&B's place mass already spans ~6× between the Deep South remainder and the Plains at K = 1). Score with
`analyze_regional_mismatch.py`: absent share ≥ 20% with rank share falling, overlap 65–75%. Guards: genre share error
(national genre totals must hold), Major share of Top 10 slots ±3, single units ±3%, R&B and Country Hot 100 counts.
Also report the realized regional genre index; R&B should now be highest where `blackPopulation` is.

### 9c. Screen: city-based regional genre taste, K = 0.5 and 1 (2026-10-27)

`Systems/RegionalGenreTaste.cs`, `--regional-genre-taste=K`. HGRPO (commercial-fit regional draw kept) against the
HGRPO control, seeds 1001-1002, 52 weeks (`SimLogs/hs52op100{1,2}-SMP-HGRPO-t05`, `-t1`; consoles in
`SimLogs/regprobe/`). **Off is inert:** `hs52op1001-SMP-HGRPO-toff` is byte-identical to the control on all 122 CSVs.

**The taste lands in sales** (realized regional unit index, 1960, seed 1001, control → K = 1):

| | Deep South | East Coast | Great Lakes | Great Plains | Rockies | Southwest | West Coast |
|---|---:|---:|---:|---:|---:|---:|---:|
| R&B | 0.93 → **1.53** | 1.03 → 1.07 | 1.07 → 1.09 | 0.93 → 0.65 | 0.91 → **0.50** | 0.91 → 0.83 | 0.96 → 0.81 |
| Country | 1.10 → 1.07 | 0.96 → 0.83 | 0.99 → 0.98 | 0.98 → 1.20 | 1.04 → 1.43 | 1.21 → **1.50** | 0.94 → 0.88 |

**But it barely moves what the regions disagree about** (`analyze_regional_mismatch.py`, all regions):

| | Overlap | Rank | Timing | Top-40 only | Absent |
|---|---:|---:|---:|---:|---:|
| Control 1001 / 1002 | 63.8 / 60.4 | 89.4 / 88.4 | 2.0 / 2.4 | 5.7 / 5.7 | 2.8 / 3.5 |
| K = 0.5 | 59.0 / 61.0 | 87.9 / 89.2 | 1.9 / 1.4 | 6.6 / 5.9 | 3.6 / 3.5 |
| K = 1 | 59.1 / 60.7 | 86.5 / 87.5 | 1.9 / 2.0 | 6.5 / 6.0 | **5.1 / 4.6** |
| History target | ~65-75 | ≤ 40 | ≥ 15 | ~25 | ≥ 20 |

Guards (control → K = 0.5 → K = 1):

| | 1001 | 1002 |
|---|---|---|
| Single units | 121.7 → 121.5 → 121.2M | 121.8 → 121.7 → 121.6M |
| Major share of Top 10 slots | 38.7 → 36.2 → 35.6 | 45.8 → 45.0 → **53.8** |
| Median units at #1 | 74k → 78k → 87k | 65k → 69k → **94k** |
| Insolvencies | 30 → 36 → 40 | 49 → 53 → 51 |
| National share: Country / TeenPop | 10.6 / 27.8 → 11.6 / 23.3 → 10.8 / 25.0 | 11.2 / 23.1 → 12.5 / 22.4 → 13.4 / 21.9 |

- **Genre taste is the wrong grain for repertoire.** It moves whole genres together, so a region's list reorders
  between genres while the same R&B and country records still reach every region's top 40. At K = 1 the absent share
  rises ~1-2 points of a 20-point gap; rank order stays ~87%.
- **National totals hold only roughly.** Country gains 0.2-2.2 points nationally and TeenPop loses 0.7-4.5 on all
  four arms: concentrated demand pushes specialist records over chart thresholds, and the chart's convex exposure
  returns that nationally. First-order normalization does not see it.
- **K = 1 on seed 1002 concentrates the head** (Major Top 10 slots +8, #1 units +45%); seed 1001 moves the other way.
  Single-seed, but a reason not to go above K = 0.5 without a guard.
- **Insolvencies rise on all four arms** (+2 to +10).

**Verdict.** Keep the switch off by default. The history list in §9 points at the missing mechanism: the exclusives
are *specific records* tied to places (Soma in Minneapolis, King and Arvee R&B in New York and Los Angeles), not
genres. That is part 3, home-city launches spreading to neighbours by distance, together with the label's own
territory. Genre taste at K ≤ 0.5 is realistic texture to carry alongside it, with the normalizer reweighted by each
region's single buyers rather than population.

### 9d. City reach: home-city launches spreading by distance (`--city-reach`, 2026-10-27)

`Systems/CityReach.cs`. Where a single is in play, at city grain, over the 38 places of §9b (31 cities, seven
remainders located at their region hub).

- **Launch.** Every place in regions the label ships the record to or is strong in (the label's territory), plus the
  home city and every place within 250 road miles of it. Home city: the act's scene city, else the label's home
  city, else the hub of the act's home region.
- **Spread, weekly.** Each reached place pulls on unreached ones by its region's sales standing for the record
  (local rank ≤ 10: 1, ≤ 25: 0.5, ≤ 40: 0.2) × `exp(−miles / 400)`; a place is reached when its accumulated exposure
  reaches 1. Minneapolis at #1-10 reaches Chicago (~410 road miles) in ~3 weeks and Los Angeles practically never.
  A national top-40 record reaches everywhere (one-stops racked the national list).
- **Effect.** Region reach r = population share of its places reached. The national awareness blend becomes
  `0.4 × national × (0.25 + 0.75 r)`, the chart floors scale by r, and the regional rotation target by r. A station
  cannot add a record its own city has not reached.
- **Kept, unlike 5a-5c:** launch stock and launch awareness are never held, so a Small label's out-of-territory
  launch-week sales survive (the cause of the arrival arm's bankruptcies).
- Runtime-only state (probe switch; not saved). A record more than 52 weeks old, or without state, reads as fully
  reached. No draws.

### 9e. Screen: city reach, alone and with taste K = 0.5 (2026-10-28)

`hs52op100{1,2}-SMP-HGRPO-cr` (city reach) and `-cr05` (city reach + taste 0.5) against the HGRPO control, 52 weeks.
**Off is inert:** `hs52op1001-SMP-HGRPO-toff2` (this build, both switches off) is byte-identical to the control on
all 122 CSVs.

| | Overlap | Rank | Timing | Top-40 only | Absent | Major unit share | Major Top 10 slots | Insolvencies |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Control 1001 / 1002 | 63.8 / 60.4 | 89.4 / 88.4 | 2.0 / 2.4 | 5.7 / 5.7 | 2.8 / 3.5 | 33.0 / 36.7 | 38.7 / 45.8 | 30 / 49 |
| Taste 0.5 | 59.0 / 61.0 | 87.9 / 89.2 | 1.9 / 1.4 | 6.6 / 5.9 | 3.6 / 3.5 | 31.9 / 38.0 | 36.2 / 45.0 | 36 / 53 |
| City reach | 61.6 / 60.1 | 89.5 / 87.8 | 3.5 / 3.0 | 5.0 / 6.4 | 2.1 / 2.8 | **38.7 / 45.6** | 42.5 / 51.3 | 40 / 48 |
| City reach + taste 0.5 | 60.3 / 61.9 | 86.3 / 88.8 | 3.3 / 2.6 | 7.5 / 6.2 | 2.9 / 2.4 | 35.3 / 45.3 | 40.2 / 54.0 | 32 / 53 |

- **City reach localizes small records, not hits.** Records whose sales concentrate ≥ 2× in one region rise from
  1,604 to 2,418 (seed 1001; 1,506 → 2,274 on 1002), but the records that ever make a regional top 11 shift toward
  Majors (85 → 107 and 93 → 117; Independents 50 → 31 and 34 → 32). Cutting out-of-territory reach thins the indies'
  volume, and a regional top 11 is volume-bound. **Major unit share +6 to +9 points.** Not adoptable as built.
- **The §9 target was set on the wrong unit.** History's "absent" share is measured between *single-city station
  lists*. A Minneapolis exclusive is ~10% of the Great Plains' buyers and is diluted in a regional sales aggregate,
  so subtracting same-city noise does not convert station-list differences into a regional-aggregate target; it
  overstates what a model region should show. The like-for-like comparison is the model's own city stations'
  playlists (the reporter panel, city-level since `PickStationCityName`) against the station surveys.

**Next.** Export each reporter station's weekly playlist (High/Mid tiers, station city), then score model station
pairs with the §9 classification against KFWB/KDWB/WABC/WLS, including same-city pairs. Only then decide which of
taste, city reach or neither is needed. Both switches stay off; taste stays in the code as an inert option.

### 9f. Model station playlists against the 1960 surveys (2026-10-28)

`ChartAuditRunner --station-playlists` writes `<run>-station-playlists.csv`: each reporter station's weekly playlist in
seating (candidacy) order, with its city (`StationNetwork.PlaylistSink`, a read-only hook). On the HGRPO control
(`hs52op100{1,2}-SMP-HGRPO-sp`) every other CSV is byte-identical to the control (122/122).
`SimTools/analyze_station_playlist_overlap.py` scores station pairs with the §9 classification: ranks 1-11 are the top
11, the whole 35-slot playlist stands in for a survey's top 40.

In 1960 only 14 of 63 reporters are Top 40 format; Chicago's and Los Angeles's are all FullService. So pop stations
are Top40 + FullService (50 stations; 12 in the four survey cities).

| 1960 top 11 | Overlap | Rank | Timing | Top-40 only | Absent |
|---|---:|---:|---:|---:|---:|
| **History, cross city** | **40.3** | 53.8 | 14.3 | 14.1 | 17.8 |
| Model, cross city, survey cities (1001 / 1002) | **40.4 / 36.6** | 26.0 / 25.3 | 10.9 / 8.7 | 31.9 / 28.0 | 31.3 / 38.0 |
| **History, same city** | **69.7** | 82.9 | 6.9 | 2.5 | 7.8 |
| Model, same city, survey cities | **52.5 / 48.9** | 37.7 / 38.7 | 11.4 / 11.1 | 36.3 / 33.2 | 14.7 / 17.0 |
| Model, all 25 cities: cross / same | 39.2 / 45.5 (1001) | 26.0 / 30.7 | 10.1 / 10.6 | 29.1 / 32.2 | 34.7 / 26.4 |

- **At the station level the model already has the cross-city level** (~40%), and timing (9-11%) is near history's 14%.
  The regional-sales gap of §9-9e was partly a unit mismatch: the right comparison was always stations.
- **But stations are idiosyncratic where history's were not.** Two stations in the same city overlap ~50%, not ~70%,
  and their differences are other records (top-40-only and absent ~50% of mismatches), not order. Cross-city, the model
  has too little rank-order disagreement (26% vs 54%) and too many records one station never plays (31-38% vs 18%).
- **Reading:** history's stations in one market played the same hits, ordered by that market's sales; cross-city
  differences were mostly order and timing on a shared list. The model's stations choose records independently of
  their market (national sales support, national heat pull, per-station DJ taste and format noise), so they disagree
  everywhere about which records, while regional *sales* (which those choices barely feed) stay near-identical.
- **Implication for the levers.** Neither regional genre taste nor city reach addresses this. The matching change is
  5e: station candidacy reads its own region's (or city's) sales support instead of the national ratio, so stations in
  one market converge on that market's records, and cross-city differences come from regional sales. Paired with it,
  how much station-level noise (DJ taste, format) is right should be checked against same-city overlap (~70%).

### 9g. Screen: 5e, station candidacy reads regional sales (`--station-regional-support`, 2026-10-28)

`Systems/Radio/StationRegionalSupport.cs`. The national "sales support" a station reads is `unitsThisWeek /
peakWeeklyUnits`: where a record is in its own life, not how big it is (a small record at its peak reads 1.0, like
the #1). Two modes: `ratio` (5e as designed: the region's units / the region's peak) and `standing` (that ratio ×
`exp(−(local sales rank − 1) / 25)`, floor 0.02, so stations follow their market's best sellers). Seeds 1001-1002,
52 weeks, playlist export on (`hs52op100{1,2}-SMP-HGRPO-e5r`, `-e5s`). **Off is inert:** `hs52op1001-SMP-HGRPO-sp2`
(this build, switch off) is byte-identical to `-sp` on all 123 CSVs, station playlists included.

Station pairs, Top40 + FullService, four survey cities (1001 / 1002):

| | Same-city overlap | Cross-city overlap | Rank | Timing | Top-40 only | Absent |
|---|---:|---:|---:|---:|---:|---:|
| History 1960 | **69.7** | **40.3** | 53.8 | 14.3 | 14.1 | 17.8 |
| Control | 52.5 / 48.9 | 40.4 / 36.6 | 26.0 / 25.3 | 10.9 / 8.7 | 31.9 / 28.0 | 31.3 / 38.0 |
| `ratio` | 51.1 / 47.4 | 31.7 / 31.3 | 21.3 / 22.7 | 14.4 / 13.0 | 33.7 / 28.3 | 30.5 / 36.0 |
| `standing` | **91.2 / 88.7** | **45.4 / 43.4** | **49.1 / 52.9** | 5.1 / 4.7 | **10.1 / 9.0** | 35.7 / 33.4 |

(cross-city composition columns; all 25 cities: `standing` same city 81.9 / 80.9, cross city 39.6 / 38.3.)

Guards (control → `ratio` → `standing`):

| | 1001 | 1002 |
|---|---|---|
| Airplay share of top-40 points (1 − units / unsampled points) | 27.6 → – → **50.2** | 23.2 → – → **49.8** |
| Single units | 121.7 → 121.5 → 121.6M | 121.8 → 121.9 → 121.6M |
| Major share of Top 10 slots | 38.7 → 33.5 → 43.3 | 45.8 → 43.8 → 47.3 |
| Median units #10 | 43.3k → 39.4k → 35.4k | 42.2k → 43.7k → 41.5k |
| Insolvencies | 30 → 30 → 40 | 49 → 50 → 51 |
| Regional-sales overlap (§9 metric) | 63.8 → – → 62.9 | 60.4 → – → 59.5 |

- **`ratio` does nothing useful:** a lifecycle ratio read locally is still a lifecycle ratio. Same city unchanged,
  cross city falls to ~31%.
- **`standing` fixes the station picture's shape:** same-city stations now share their market's hits (overshooting
  70% to ~90%), cross-city overlap stays at history's level, and the mismatch is now mostly order (49-53% vs 54%)
  with top-40-only at history's 9-10%. Timing falls (5% vs 14%) and absent stays high (33-36% vs 18%).
- **But it doubles airplay's share of chart points** (~25% → ~50%), far outside the ±3-point guard: stations that
  follow sales concentrate panel spins on the records already selling, and the panel's points add to those
  records. Regional sales are unchanged (overlap 63/60 → 63/60), so the stations still do not feed back into
  regional demand. Not adoptable as built.

**Next, if pursued:** `standing` needs the panel's chart-point weight renormalized so airplay's share returns to
the guard (the panel weight is a calibrated constant, memory: radio network w = 0.13), and the rank scale loosened
(25 → ~10-15 more noise or a wider scale) to bring same-city overlap from ~90% to ~70%. Both are calibration on top
of a mechanism that now has the right shape at station level.

### 9h. Calibrating 5e `standing`: panel weight and rank scale (2026-10-28)

Two probe knobs on the 5e switch, both unchanged unless named: `--station-regional-support-rank-scale=S` (the standing
scale, default 25) and `--station-regional-support-panel-weight=W` (the reporter panel's weight in regional
`radioPlay`, `Lerp(tail, reporter, W)`; the calibrated constant `REPORTER_PANEL_WEIGHT = 0.13` otherwise). Same flags
as §9g, 52 weeks, `SimLogs/hs52op100{1,2}-SMP-HGRPO-c-*`. Airplay share is the pooled top-40 share,
1 − Σunits / Σunsampled points (the §9g numbers; the median in `analyze_regional_guards.py` reads 2.7 → 16.3 on the
same runs). Overlap columns are survey cities, Top40 + FullService.

**Rank scale alone (W = 0.13, seed 1001)** barely moves same-city overlap until it is very wide: every selling single
in a region is ranked (hundreds), so even S = 100 cuts deep into the tail.

| S | Airplay | Same city | Cross city | Rank | Top-40 only | Absent |
|---:|---:|---:|---:|---:|---:|---:|
| 25 (§9g) | 50.2 | 91.2 | 45.4 | 49.1 | 10.1 | 35.7 |
| 50 | 51.4 | 88.4 | 49.1 | 55.7 | 9.7 | 29.6 |
| 100 | 50.4 | 85.9 | 52.7 | 58.0 | 13.9 | 22.4 |
| 300 | 45.9 | 82.4 | 55.0 | 52.5 | 23.4 | 17.6 |
| 1000 | 40.5 | 74.5 | 46.8 | 34.2 | 31.9 | 23.9 |

**Panel weight (S = 25, seed 1001):** W 0.05 → 9.7, 0.085 → 24.7, 0.09 → 28.4, 0.13 → 50.2. Steep (the airplay
convexity), and it leaves the playlists' shape alone (same city 90.9 at W = 0.05).

**Calibrated arms, both seeds (1001 / 1002):**

| | Airplay (control 27.6 / 23.2) | Same city | Cross city | Rank | Top-40 only | Absent |
|---|---:|---:|---:|---:|---:|---:|
| History 1960 | – | 69.7 | 40.3 | 53.8 | 14.1 | 17.8 |
| Control | 27.6 / 23.2 | 52.5 / 48.9 | 40.4 / 36.6 | 26.0 / 25.3 | 31.9 / 28.0 | 31.3 / 38.0 |
| A: S = 25, W = 0.085 | **24.7 / 23.9** | 89.8 / 89.1 | 45.8 / 43.7 | **49.0 / 51.6** | **10.8 / 9.8** | 35.4 / 33.3 |
| B: S = 1000, W = 0.095 | **25.7 / 22.8** | **75.6 / 73.1** | 49.1 / 47.2 | 39.0 / 34.1 | 26.4 / 31.4 | **24.4 / 24.2** |

Other guards (control → A → B, 1001 / 1002): single units 121.7 → 121.9 → 121.8M / 121.8 → 121.7 → 121.7M; Major
share of Top 10 slots 38.7 → 46.2 → 30.6 / 45.8 → 51.5 → 43.5; median units #10 43.3k → 39.4k → 41.3k / 42.2k →
37.6k → 43.8k; median units #1 74k → 88k → 83k / 65k → 93k → 88k; insolvencies 30 → 36 → 38 / 49 → 54 → 55.

- **The weight renormalization works:** W ≈ 0.085-0.095 puts airplay's share back inside ±3 points on both seeds,
  at either scale (a wider scale spreads panel spins, so it needs slightly more weight).
- **The rank scale cannot reach ~70% same-city without losing what `standing` fixed.** Widening it to S = 1000
  brings same-city to 73-76%, but it does so by letting more of the tail back in. The extra same-city mismatches are
  other records, not order (top-40-only back to control's ~30% vs history's 14%; rank share 34-39% vs 54%).
  Cross-city overlap rises to 47-49% (history 40). S = 25 keeps the history-shaped mismatch (rank ~50%, top-40-only
  ~10%) but same-city stays ~90%.
- **Both arms raise #1 units** (+15-40%): with less airplay in the score, #1 is more often the top seller. Arm A
  also lifts Major Top-10 slots (+6-8 pts on both seeds) and lowers #10 units ~10%. Arm B is mixed on Majors
  (−8 / −2). Two seeds cannot settle the Major guard (§5d note).
- **Reading:** same-city stations read one signal (their region's sales). The scale only changes how much of the
  tail that signal admits, so it cannot make two same-city stations disagree on *order* while sharing a list.
  History's same-city 70% with 83% of mismatches as order wants station-level ordering noise on a shared list
  (DJ taste or a per-station draw on the standing rank), not a wider scale.

**Status.** Both knobs stay off by default (unnamed, `hs52op1001-SMP-HGRPO-sp3` is byte-identical to `-sp2` on all 123 CSVs). If 5e is pursued: take arm A's scale (25) with W ≈ 0.085 as the
airplay-calibrated base. Then test per-station rank noise, which the same-city target needs. Check #1 units and
Major Top-10 slots on more seeds before any adoption.

### 9i. Screen: per-station rank noise on `standing` (2026-10-28)

`--station-regional-support-rank-noise=σ`: each station reads a record's local sales rank as rank × exp(σ z), with z a
keyed normal (`BandLife.Normal("station-rank|station|record")`). It is fixed per station and record (the programmer's
own view of it) and touches no RNG stream. Stations in one market share the sellers but order them differently, which
is what §9h says the same-city target needs. Unnamed it is inert: `hs52op1001-SMP-HGRPO-c-w085b` (this build,
S = 25, W = 0.085, no noise) is byte-identical to `-c-w085` on all 123 CSVs. Runs `SimLogs/hs52op100{1,2,3}-SMP-HGRPO-n*`.

**σ sweep (S = 25, W = 0.085, seed 1001; survey cities, Top40 + FullService):**

| σ | Airplay | Same city | Same-city rank / top-40 / absent | Cross city | Cross rank / top-40 / absent |
|---:|---:|---:|---|---:|---|
| History | (27.6) | 69.7 | 82.9 / 2.5 / 7.8 | 40.3 | 53.8 / 14.1 / 17.8 |
| 0 (§9h A) | 24.7 | 89.8 | 63.6 / 10.9 / 24.4 | 45.8 | 49.0 / 10.8 / 35.4 |
| 0.4 (W 0.09) | 25.1 | 74.8 | 75.8 / 11.1 / 12.0 | 42.9 | 46.8 / 11.6 / 35.9 |
| **0.5** | 22.6 | **72.7** | 71.7 / 13.2 / 12.4 | **42.0** | 44.7 / 12.5 / 36.7 |
| 1.0 | 17.1 | 52.8 | 49.3 / 16.2 / 29.2 | 34.0 | 33.8 / 13.6 / 46.6 |
| 1.5 | 12.8 | 41.8 | 31.7 / 13.5 / 49.1 | 24.8 | 23.8 / 12.1 / 58.4 |

The noise spreads panel spins, so airplay falls with σ, and W has to rise to hold the guard. At σ = 0.5 the treated
arms read ~24% airplay on every seed while the controls span 23-29, so one weight fits all seeds: **W = 0.092**.

**Calibrated arm, σ = 0.5, W = 0.092, three seeds (1001 / 1002 / 1003):**

| | Control | Arm |
|---|---|---|
| Airplay share (pooled, guard ±3) | 27.6 / 23.2 / 28.8 | **25.6 / 24.9 / 26.6** |
| Same city (survey cities; history 69.7) | 52.5 / 48.9 / – | **72.0 / 72.2 / 73.0** |
| Same-city mismatch rank / top-40 / absent (83 / 2.5 / 7.8) | 37.7 / 36.3 / 14.7 (1001) | 71.4-73.7 / 10.2-13.5 / 12.7-15.3 |
| Cross city (history 40.3) | 40.4 / 36.6 / – | **41.3 / 42.1 / 39.6** |
| Cross mismatch rank / top-40 / absent (54 / 14 / 18) | 26.0 / 31.9 / 31.3 (1001) | 45.6-46.8 / 11.1-12.7 / 36.1-36.8 |
| Single units | 121.7 / 121.8 / 121.3M | 121.6 / 121.4 / 120.9M |
| **Major share of Top 10 slots** | 38.7 / 45.8 / 30.4 | **44.2 / 45.2 / 50.0** |
| Major unit share | 33.0 / 36.7 / 29.9 | 32.8 / 36.4 / 36.3 |
| Median units #1 | 74k / 65k / 71k | 84k / 80k / 84k |
| Median units #10 | 43.3k / 42.2k / 38.6k | 39.3k / 39.7k / 38.3k |
| Distinct top-10 artists | 84 / 76 / 84 | 70 / 73 / 71 |
| Insolvencies | 30 / 49 / 37 | 33 / 47 / 41 |

- **The station picture is now history-shaped on every axis but one.** Same-city overlap is 72-73% with mismatches
  that are mostly order. Cross-city overlap is 40-42% with top-40-only at history's level. Airplay and units are inside
  their guards on all three seeds. The remaining miss is cross-city **absent** (36% vs 18%): records one city never
  plays. That is regional sales (the §9 regional-sales overlap, unchanged by 5e), not station behaviour.
- **But the chart concentrates.** Major Top-10 slots go +5.5 / −0.6 / +19.6 (mean +8). #1 units rise 14-23%, and
  distinct top-10 artists fall 84/76/84 → 70/73/71. The §9h arms without noise moved the same way (+6-8 Major slots),
  and `standing` at W = 0.13 less so (+4.6 / +1.5). So the concentration comes from `standing` plus the lower panel
  weight, not from the noise.
- **Reading (hypothesis, untested):** the national lifecycle ratio was size-blind (a small record at its peak read
  1.0), so panel airplay gave small sellers a chart lift independent of their sales. Under `standing`, airplay
  follows each market's best sellers, so it adds to the records already selling, and Majors sell most. The airplay
  *share* of points is restored, but it is no longer an independent signal.

**Status.** Off by default; not adoptable while the Major and top-10-breadth guards move this much. The station
shape is solved (S = 25, W = 0.092, σ = 0.5). Next, if pursued, test whether airplay can stay partly size-blind: e.g. blend the
national ratio back into support (support = ratio × lerp(1, standing, k)), and measure Major Top-10 slots and
distinct top-10 artists on seeds 1001-1005 against same-city overlap.

### 9j. Screen: blending standing back toward size-blind support (2026-10-28)

`--station-regional-support-blend=k`: support = ratio × lerp(1, standing, k). k = 1 is full standing, and lower k
keeps support partly size-blind, as the national ratio was (the §9i hypothesis for the Major lift). Unnamed it is
skipped: `hs52op1001-SMP-HGRPO-n05w092b` (this build, no blend) is byte-identical to `-n05w092` (123/123). All arms
use S = 25, W = 0.092 and σ = 0.5, over five seeds 1001-1005 (`SimLogs/hs52op100{1-5}-SMP-HGRPO-{n05w092,k075,k05}`). A
fourth arm, `natw092`, keeps national support and cuts only the panel weight, to attribute the lift. Scored by the
scratchpad script `score5.py` (1960; pooled top-40 airplay; survey cities, Top40 + FullService). Δ is paired against
each seed's HGRPO control (SE over seeds).

| Arm (5-seed mean) | Airplay | Major Top-10 slots Δ (SE) | Top-10 artists Δ | #1 units | Same city | Cross city | Cross rank / absent |
|---|---:|---|---:|---:|---:|---:|---|
| Control | 26.5 | 38.5 | 81.4 | 64k | – | – | – |
| k = 1 | 26.1 | +6.6 (3.9) | −8.0 | 80k | 71.6 | 40.8 | 45.9 / 36.4 |
| k = 0.75 | 26.2 | +10.1 (3.2) | −7.2 | 80k | 71.0 | 41.5 | 45.7 / 33.6 |
| k = 0.5 | 22.8 | +8.5 (3.7) | −6.8 | 80k | 66.9 | 39.7 | 36.3 / 35.0 |
| National, W = 0.092 only | 13.5 | +2.1 (3.4) | −2.2 | 83k | 52.3 | 38.3 | 26.8 / 36.3 |

Per seed, Major Top-10 slots Δ: k = 1 +5.5 / −0.6 / +19.6 / +10.4 / −1.7; k = 0.75 +1.1 / +20.7 / +8.1 / +10.4 / +10.2;
k = 0.5 +5.9 / +9.2 / +22.1 / +4.4 / +0.8; national +W −8.7 / +0.2 / +12.3 / +4.6 / +2.3. Units and insolvencies are
flat in every arm.

- **The blend does not undo the concentration.** Down to k = 0.5, Major Top-10 slots stay +7-10 and top-10 breadth
  stays −7. Meanwhile the station shape starts to go: same city 67%, cross-city rank share 36% vs history's 54, and
  airplay below the guard (22.8).
- **This is not a clean refutation of the size-blind reading.** Candidacy raises support to the power 1 + (1 − autonomy) × 2
  (up to 3), so even k = 0.5 leaves a tail record at a 0.5 floor up to ~8× below a market's top seller after the
  exponent. A blend loose enough to flatten that would give back the station shape §9i won.
- **The panel-weight cut alone is not the cause.** National support at W = 0.092 moves Majors +2.1 (within one SE)
  and breadth −2.2, with airplay halved. The lift needs `standing` and the lower weight together: the panel then
  stacks on each market's best sellers.
- **#1 units rise ~+16k in every arm, the national one included.** That part comes from the lower panel weight
  (less panel airplay to lift non-top sellers into #1), not from `standing`.

**Status.** All knobs off by default; 5e is not adoptable. The station shape (S 25, σ 0.5, W 0.092) costs +7-10 Major
Top-10 slots and −7 distinct top-10 artists on five seeds, and blending support toward size-blind does not buy
it back before the shape degrades. Options: (a) accept 5e as a station-level picture only, feeding the playlist
export but not `radioPlay` (W → the national panel for chart points); (b) look for the concentration in the
panel aggregation (`ReporterRadioPlay`, tier spins) rather than in candidacy.

### 9k. Where the Major lift sits: panel composition, not the aggregation (2026-10-28)

No new runs. The §9j runs (control `hs52op100{1-5}-SMP-HGRPO[-sp]`, `standing` = `-n05w092`, `natW` = `-natw092`; S 25,
σ 0.5, W 0.092; 1960) re-read with two scratchpad scripts. `panel_tiers.py` rebuilds `ReporterRadioPlay` from the
playlist export: per region, Σ spinWeight × Hot100ReportingWeight / Σ ReportingWeight. Station reach is not exported,
so stations count equally. `panel_vs_sales.py` re-ranks each week's chart top 100 by units alone. That gives the top
10 that sales would produce in the same run, so the comparison is paired within the run and avoids the seed reshuffle.
Control playlists exist only for 1001-1002. `natW` has the same national support, so it stands in for the control's
panel on all five seeds (its tier shares match the control's on 1001-1002 within ~2 points).

**The panel's Major share, by tier** (share of format-weighted slots held by Major records; 5-seed means):

| | High | Mid | Light | Panel spin mass | Records per region-week with panel ≥ 0.8 / ≥ 0.5 | Spearman(panel, units) |
|---|---:|---:|---:|---:|---:|---:|
| Control (1001 / 1002) | 22.7 / 24.8 | 16.6 / 18.9 | 14.6 / 17.6 | 18.7 / 21.0 | 3.4 / 31.7 | 0.19 |
| `natW` | 22.6 | 16.4 | 15.0 | 18.7 | 3.7 / 29.9 | 0.22 |
| `standing` | 27.8 | 27.9 | 29.5 | 28.2 | **20.3 / 70.6** | **0.75** |

(Major unit share is 31.7 in the control. The panel ≥ 0.8/0.5 counts are summed over regions and divided by 52 weeks.)

- **The tier spins do not create the tilt.** Under `standing`, Majors gain seats in every tier, and they gain more in
  Mid and Light (+11, +14) than in High (+5). The tier weights and the reach-weighted mean only pass on what candidacy
  seated. Under national support, Majors held 19% of panel spins against 32% of units, so the panel leaned against
  Majors. Under `standing` they hold 28%, close to their sales share.
- **What the aggregation does add is concentration.** Stations in one market now agree, so the same spin mass lands
  on fewer records. Record-regions at panel ≥ 0.8 rise from ~3.5 to ~20 a week. Panel airplay for the sales top 10
  doubles (0.22 → 0.43), and for ranks 41-100 it stays flat (0.04). That is the mean of agreeing stations, not the
  tier weights.
- **The payout then stacks on Majors.** Majors' share of airplay *points* moves 24.8 → **37.9** (Δ +13.1, SE 3.3).
  That is above their unit share (33.6), and it rises more than their share of panel mass did (+9.5). The panel term
  enters `Lerp(tail, panel, W)` before the fifth power. So agreeing panel spins add to records whose tail is already
  high (the pushed records), and the exponent compounds them. In the top 10, airplay points per unit go from Major
  0.5× non-Major (control) to parity (0.85×).

**Splitting the Major Top-10 lift** (Major share of Top-10 slots; offset = actual − sales-only, in the same run; Δ paired
against the control, SE over 5 seeds):

| | Actual | Top 10 by units alone | Airplay offset | Δ actual | Δ sales-only | Δ offset |
|---|---:|---:|---:|---|---|---|
| Control | 38.5 | 44.5 | −6.0 (1.3) | | | |
| `standing` | 45.2 | 47.6 | −2.4 (0.8) | +6.7 (3.9) | +3.1 (3.5) | **+3.5 (0.9)** |
| `natW` | 40.7 | 43.3 | −2.7 (0.6) | +2.2 (3.4) | −1.1 (3.6) | **+3.3 (1.0)** |

- **In the control, airplay holds Majors 6 points below their sales top 10.** That offset is the size-blind national
  panel lifting non-Major records (air per unit 0.94 vs 0.36 in the top 10 on 1001).
- **Both arms lose the same ~3.5 points of offset, by different routes.** `natW` has half the airplay (13.5% of
  points). `standing` keeps airplay at 26%, but airplay now follows sales, so it no longer offsets them. The offset is
  the low-noise part of the lift (SE ~1).
- **The rest of `standing`'s headline (+3.1) is in the sales-only top 10, at noise level.** Per seed it runs
  +2.5 / −5.0 / +15.4 / +4.8 / −2.1. It could be panel airplay feeding awareness and then sales (Major unit share is
  +5-6 on 1003-1004), or it could be the reshuffle. Five seeds cannot tell them apart. The same split explains `natW`:
  its smaller headline (+2.2) is the same offset loss plus a sales-only draw of −1.1.
- **This corrects §9j's attribution.** On the offset, the panel-weight cut alone loses as much as `standing` does.
  §9j's "national W 0.092 moves Majors +2.1, within one SE" read a headline whose noise comes from the sales ranking.

**Reading.** The Major lift is not in `ReporterRadioPlay` or the tier spins. Any monotone aggregation of a
sales-ordered panel gives a sales-ordered signal. The control's Major share depends on the panel being *anti*-sales
(size-blind support seated non-Major records at their own peaks), and at W 0.13 that bought ~6 Top-10 points.
`standing` makes the panel agree with each market's sales, which is what the station shape needs. A concave cap on
panel airplay before the lerp would undo the consensus concentration, but not the composition, which is the larger
part (Mid and Light seats). It would also give back the airplay share that W was calibrated to.

**Status.** Nothing changed in code. If the station shape is kept, the chart needs a non-sales airplay signal to hold
the control's offset. Candidates: §9j option (a), where the playlists keep `standing` but chart points read the
national panel; or a source of independent picks inside candidacy (DJ taste, advocacy, payola), which is what
historically broke non-Major records. Use the paired offset, not the headline Major Top-10 Δ, to screen either one.

### 9l. Screen: option (a), standing playlists beside a national chart panel (`--station-regional-support-chart=national`, 2026-10-28)

Each reporter now holds two meetings a week. The **national** meeting runs on the station's own state (`station.rt`)
and the network's RNG, exactly as with 5e off. It feeds `ReporterRadioPlay`, break claims and reputation at the
calibrated W 0.13. The **regional** meeting (here `standing`, S 25, σ 0.5) runs on shadow state (per-station
`StationRuntime`, pruned on retirement like the real one) with its own RNG stream. Only the playlist export reads it.
The flag needs a regional mode; `=stations` (the default) keeps §9g-9j behaviour. Runs `SimLogs/hs52op100{1-5}-SMP-HGRPO-opta`.

**Inert unnamed:** `hs52op1001-SMP-HGRPO-n05w092c` (this build, §9i arm) is byte-identical to `-n05w092` on all 123
CSVs. `performance-profile.csv` is wall time and differs in every pair here.

**The chart is the control's, byte for byte.** On all five seeds every CSV except the playlist export is identical to
the HGRPO control (122/122). So the §9k paired airplay offset is the control's −6.0, Δ 0.0 by construction. The same
holds for Major Top-10 38.5, top-10 artists 81.4, airplay 26.5%, #1 units 64k and insolvencies 35.2.

**The station shape survives on the control world** (survey cities, Top40 + FullService, 5-seed means):

| | Same city | Cross city | Cross rank / absent |
|---|---:|---:|---|
| History 1960 | 69.7 | 40.3 | 53.8 / 17.8 |
| §9i arm (`n05w092`, standing feeds the chart) | 71.6 | 40.8 | 45.9 / 36.4 |
| **Option (a)** | **72.0** (70.9-72.8) | **40.6** | 46.0 / 35.2 |

**The cost: the playlists people see are not the panel that scores.** On the two seeds with control playlists
(1001 / 1002):

| Per station-week, Top40 + FullService | 1001 | 1002 |
|---|---:|---:|
| Share of the standing list also on the station's national list | 16.4 | 16.7 |
| Top-11 overlap, standing vs national | 18.5 | 19.2 |
| Standing High records also High nationally | 18.3 | 18.6 |
| National list: slots on records in the chart top 40 (High tier) | 23 (71) | 23 (70) |
| Standing list: slots on records in the chart top 40 (High tier) | 52 (62) | 52 (58) |

- The two panels share about one record in six. Their High tiers both lean on charting records. The national
  meeting fills Mid and Light with records off the top 40, at their own sales peaks: the size-blind lift §9k
  inferred, seen directly in the playlists. That off-chart rotation is what holds Majors 6 Top-10 points below their
  sales ranking.
- **In this probe every player-facing read still sees the national meeting:** `SpinTierOf`, Rolodex, courtesy
  calls, the DJ add-on-the-phone path, and reputation through break claims. So the standing playlists exist only in
  the export. Adopting (a) in the game means choosing which sheet the player sees. If it is the standing sheet, a
  DJ's visible rotation would barely overlap what moves the chart, and a record "on his sheet" would usually get no
  chart help from it.

**Status.** Off by default. Option (a) is chart-safe by construction, and it keeps the station shape (72 / 41). It
does this by running two panels that agree on about 17% of records. It works as an observe-only station picture,
like the media-atmosphere export. It is not a model of what radio did to the chart. The other route keeps one panel:
make the regional meeting itself carry non-sales picks (DJ taste, advocacy, payola), so that it recovers the
control's −6 offset. Screen that on the paired offset.

### 9m. Screen: non-sales picks in the regional meeting, and a size-blind order inside the sellers (2026-10-28)

The goal was to recover the control's −6 airplay offset (§9k) under `standing`. All arms use S 25, σ 0.5,
W 0.092 (`SimLogs/hs52op100{1-5}-SMP-HGRPO-*`). They are scored by the scratchpad script `score_offset.py`: offset =
Major Top-10 share minus the share in a top 10 ranked by units in the same run. **Unnamed, every new knob is inert:**
`-n05w092d` (this build) is byte-identical to `-n05w092` on all 123 CSVs.

**Advocacy and payola cannot be screened headless.** Both terms are player-only (`ActiveAdvocacyLookup`,
`ActivePayolaLookup`, discretionary picks), so they read neutral in every AI run. An AI analogue would be new label
behaviour with a cost, not a probe of the meeting. What the meeting can carry is the DJ's taste.

**What the control's off-chart rotation is** (1001 / 1002, Top40 + FullService). 66% of national slots are on
uncharted records: 3-4 weeks old, ~250 units a week, quality ~0.78, ~10% Major, and 7-10% go on to the top 40. These
are new releases on trial. `standing`'s uncharted slots (30%) are different: week-old records selling ~9 units,
quality 0.62, that almost never chart, and they hold 7% of High. **This is a defect in `Support()`:** a record with no
sales yet in the region falls back to the national ratio, which is 1.0 in its launch week, and so outranks all but a
market's top few sellers. `--station-regional-support-unsold-floor` makes it read the standing floor instead.

**Probe knobs.** `--station-picks=N` reserves N slots in a tier (`--station-picks-tier=light|mid|high`) for records at
most `--station-picks-age` weeks old (default 6). The slots are filled from records the standing ranking left out, by
one of two scores (`--station-picks-score`). `trial` is the national meeting's own candidacy (size-blind support,
heat, the DJ's read). `ear` is the DJ's read of quality and genre, with no sales or heat term. Both scores use the same
draw as candidacy, so the RNG stream is untouched. `--station-regional-support-rank-plateau=K` sets standing to 1 for a
market's top K sellers and decays it past them. Stations then agree on which records are the market's sellers, but
order them by lifecycle ratio, as the national meeting did.

**Seed 1001 sweep** (offset: control −6.2, `standing` −3.1):

| Arm | Offset | Airplay | Same city | Cross city | Cross rank / absent | Major share of High |
|---|---:|---:|---:|---:|---|---:|
| Control | −6.2 | 27.6 | 52.5 | 40.4 | 26.0 / 31.3 | 22.8 |
| `standing` (§9i) | −3.1 | 25.6 | 72.0 | 41.3 | 45.6 / 36.2 | 28.8 |
| Unsold floor | −2.3 | 32.4 | 69.5 | 52.8 | 58.6 / 21.3 | 35.1 |
| Floor + trial, 8 / 15 Light | −1.9 / 0.0 | 29.5 / 27.5 | 62.9 / 60.5 | 47.1 / 47.2 | | 39.8 / 41.8 |
| Floor + trial, 6 Mid | −1.9 | 26.8 | 63.3 | 49.8 | | 41.4 |
| Floor + ear, 8 Light / 6 Mid | −1.3 / −2.3 | 29.3 / 26.6 | 61.9 / 61.1 | 47.8 / 46.5 | | 40.1 / 36.9 |
| Trial, 8 Light (no floor) | −1.2 | 22.2 | 68.2 | 37.9 | | 22.3 |
| Floor + ear / trial, 3 High | +0.8 / −0.6 | 20.7 / 21.6 | 52.8 / 57.3 | 39.2 / 43.0 | | 33.4 / 31.6 |
| Plateau K 25 / 40 | −5.4 / −3.8 | 25.9 / 25.2 | 63.7 / 66.7 | 44.8 / 49.4 | 46.6 / 28.1, 45.8 / 25.7 | 24.3 / 33.0 |
| Floor + plateau K 25 / 40 | −3.5 / −0.4 | 28.0 / 26.3 | 66.2 / 68.0 | 49.7 / 49.7 | | 33.5 / 36.0 |

- **No non-sales pick recovers the offset; every one moves it toward zero.** The picks look like the control's
  rotation (`trial` Light picks are ~18% Major, like the control's Light at 14%). But they sit outside the High tier,
  and High is where the charting records are. Taking Light or Mid slots for picks pushes the sellers up a tier, so
  High becomes *more* Major (37-42%). Picks in High take High slots from the sellers. They go to young records, which
  rarely chart, so airplay drops below the guard (21%) and the offset still does not return.
- **The offset is set by which charting records get High.** The control's size-blind order put non-Major hits at their
  peak level with Major ones. Only the plateau restores that (Major share of High 24.3 vs the control's 22.8).
- **The unsold floor fixes a real defect but costs the guard.** Airplay rises to 32 and cross city to 53. The junk
  slots were holding cross-city overlap and airplay share down; with them gone, absent falls to 21% (history 18).

**Plateau, five seeds** (paired against the control; SE over seeds):

| Arm (5-seed mean) | Offset | Δ offset (SE) | Major Top-10 Δ (SE) | Top-10 artists | Airplay | #1 units | Same city | Cross city | Cross rank / absent |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| Control | −6.0 | | | 81.4 | 26.5 | 64k | – | – | – |
| `standing` (§9i) | −2.4 | +3.5 (0.9) | +6.7 (3.9) | 73.4 | 26.1 | 80k | 71.6 | 40.8 | 45.9 / 36.4 |
| **Plateau K 25** | **−5.4** | **+0.6 (1.9)** | +3.3 (2.5) | **79.8** | 26.5 | 79k | 65.4 | 47.1 | 46.2 / 28.7 |
| Plateau K 15 | −3.5 | +2.4 (0.8) | +6.9 (2.0) | 75.6 | 26.8 | 78k | 66.0 | 45.6 | 47.3 / 30.4 |

Per seed, offset at K 25: −5.4 / −6.0 / −4.4 / −3.1 / −8.1 (control −6.2 / −8.1 / −8.7 / −5.6 / −1.3). Paired against
`standing`, K 25 recovers 3.0 points of offset (SE 1.2). It is better or equal on all five seeds (−2.3 / −2.3 / 0.0 / −3.1 /
−7.1). Seed 1005, where the control's offset is only −1.3, carries most of the spread against the control.

- **K 25 recovers most of the offset, and with it most of the breadth.** Top-10 artists are 79.8 against the
  control's 81.4 and standing's 73.4. Airplay sits on the guard with no change to W. The Major Top-10 headline falls
  to +3.3, inside 1.5 SE, and the rest is in the noisy sales-only ranking (§9k).
- **It costs some station shape.** Same city falls to 65 (history 70; standing 72). Cross city rises to 47 (history
  40), because the market's top-25 set is near-identical across regions, the regional-sales lockstep of §9. The
  mismatch mix improves (absent 29 vs 36, history 18; rank 46, history 54).
- **K 15 is too tight** (offset −3.5): a top-15 plateau still lets size order most of High's competition. #1 units stay
  ~+15k in every arm; that is the lower W (§9j), not the order.
- **Reading.** What the national meeting did for the chart was not "non-sales picks". It was a *size-blind order among
  the records that are selling*. A non-sales channel outside the sellers cannot reach the chart at all. A flat
  standing over the market's top 25 keeps stations on the market's sellers, and it gives back the order that held
  Majors below their sales ranking.

**Status.** All knobs off by default; nothing adopted. The candidate is `standing` with plateau K 25 (S 25, σ 0.5,
W 0.092). It holds the offset at about the control's level, breadth within 2 artists and airplay on the guard, at the
cost of same city 65 / cross city 47. Before adoption: run more seeds (the offset SE is 1.9 against the control),
try K 20-30 with σ (cross-city overlap is now the miss), and decide whether the unsold-floor defect should be fixed
in the same change. Fixing it moves airplay to ~28-32, so it needs a W re-calibration.

### 9n. Tuning the plateau, and the unsold-floor decision (2026-10-28)

The work went in three steps. First, a shape screen on seed 1001 crossed K 20/25/30 with rank noise 0.25/0.5/0.75, plus
the unsold floor at W 0.085 and 0.08. Second, eight seeds (1001-1008) for the chosen arms, with new controls (`-sp`) and
`standing` (`-n05w092`) arms for 1006-1008. Third, to finish the 2×2, the floor without the plateau. Runs are
`SimLogs/hs52op100{1-8}-SMP-HGRPO-{k*n*,p25,ufw085,ufk25w088}`; scoring is as in §9m.

**Shape screen (seed 1001; same city / cross city, history 69.7 / 40.3):**

| | σ 0.25 | σ 0.5 | σ 0.75 |
|---|---|---|---|
| K 20 | 75.8 / 49.2 | 64.2 / 46.8 | 56.3 / 41.1 (airplay 22.8) |
| K 25 | 75.5 / 53.4 | 63.7 / 44.8 | 58.7 / 43.4 (22.9) |
| K 30 | 76.0 / 51.6 | 65.6 / 47.9 | 56.6 / 41.7 (21.2) |

- **Noise moves same city and cross city together; K barely moves either.** No σ reaches both 70 and 40. σ 0.75
  reaches history's cross city only by dropping same city to 57, and airplay falls below the guard. σ 0.5 is the best
  compromise. K 20-30 differ by one seed's noise, so K stays at 25.
- **The floor puts airplay on the guard between W 0.085 and 0.092** (25.2 / 28.0 at K 25). W 0.088 is used with the
  plateau, 0.085 without.

**Eight seeds, the 2×2 of plateau × floor** (means; Δ paired against the control, SE over 8 seeds):

| Arm | Offset | Δ offset (SE) | Major Top-10 Δ | Top-10 artists | Airplay | Same city | Cross city | Cross rank / absent |
|---|---:|---|---|---:|---:|---:|---:|---|
| History 1960 | | | | | | 69.7 | 40.3 | 53.8 / 17.8 |
| Control | −5.5 | | | 81.8 | 25.5 | 52.0 | 39.3 | 26.9 / 35.7 |
| `standing` (§9i, W .092) | −2.2 | +3.3 (0.7) | +6.9 (2.5) | 74.0 | 25.7 | 71.9 | 40.7 | 45.9 / 36.2 |
| Plateau K 25 (W .092) | −4.5 | +1.0 (1.2) | +4.4 (1.8) | 79.5 | 26.2 | 65.9 | 47.3 | 46.3 / 28.4 |
| Floor (W .085) | −3.1 | +2.4 (0.9) | +6.2 (1.6) | 74.8 | 27.8 | **68.7** | 50.7 | **60.1 / 19.6** |
| Floor + plateau K 25 (W .088) | −2.9 | +2.6 (0.9) | +6.3 (2.5) | **80.5** | 25.8 | 66.2 | 49.3 | 52.4 / 23.0 |

Paired effects on the offset (SE): the plateau without the floor gives **−2.3 (0.9)**; the plateau with the floor
**+0.2 (0.9)**. The floor without the plateau gives −0.9 (0.7); the floor with the plateau +1.7 (0.7).

- **The plateau's offset gain depends on the defect.** Without the floor it recovers 2.3 of the 3.3 points `standing`
  lost. With the floor it recovers none. The junk records (week-old, unsold, ~1% Major, reading 1.0) push sellers out
  of panel slots. Under a flat top-25 standing, the sellers they push out are disproportionately Major. That is a
  side effect of a bug, not a mechanism.
- **The plateau's breadth gain does not depend on the defect.** Top-10 artists recover to ~80 with or without the
  floor (standing 74, floor alone 74.8, control 81.8).
- **The floor alone gives the best station picture of any arm.** Same city is 68.7 (history 69.7), and the
  cross-city mismatch is history-shaped (rank 60 vs 54, absent 19.6 vs 17.8). Its offset is no worse than `standing`
  (−3.1 vs −2.2). Cross-city overlap stays ~50 against 40 in every floor arm. That is the regional-sales lockstep (§9),
  which no station knob reaches.
- **Decision: fix the unsold fallback.** A station reading full support for a record that has sold nothing in its
  market is wrong. Keeping it would mean leaning on that bug for 1.7 points of offset. With the fix, W is ~0.085
  without the plateau (airplay 27.8, +2.3 over the control, inside ±3; ~0.082 would centre it) and 0.088 with it
  (25.8).
- **With the defect fixed, the offset is still open.** The best arms sit at Δ +2.4-2.6 against the control's −5.5.
  Neither non-sales picks (§9m) nor a size-blind order inside the sellers recovers it. The plateau is still worth
  keeping for breadth (+5.7 artists over floor alone, SE not computed). It costs same city 66 vs 69 and moves cross
  city 51 → 49.

**Status.** Knobs still off by default; nothing adopted or committed. The candidate 5e configuration is now
`standing` + unsold floor, S 25, σ 0.5, with or without plateau K 25 (W 0.085 / 0.088). It is history-shaped on
every station axis except cross-city overlap (+10). It holds airplay, units and breadth (with the plateau). It still
gives up ~2.5 points of the control's −5.5 Major offset. Open: whether that remaining offset is a target at all. The
control's offset comes from a size-blind panel spending two-thirds of its slots on uncharted trial records (§9m). The
historical Major share of the 1960 Top 10 is the benchmark that decides it.

### 9o. Is the remaining offset a target? The 1960 Big-5 Top-10 benchmark (2026-10-28)

**History.** In 1960 the Big-5 operating labels (RCA Victor, Columbia, Capitol, Decca, Mercury) held **34.2%** of
Top-10 slot-weeks: 178 of 520 (`HistoricalLabelTierBalanceResearch.md` §17l, from the recovered weekly Hot 100s).

**Like for like in the model.** The runtime `Major` tier is not the Big-5. It also includes promoted fictional majors
(Beacon, Running Records, …), which hold ~4-6% of Top-10 slots. The Big-5 are `label_0001`-`label_0005` on every seed;
names vary by seed ("Mercury" / "Mercury Music"), so match on ID. Eight seeds (1001-1008), 1960, the §9n arms:

| Arm | Big-5 Top-10 slots (SE; seed range) | vs history 34.2 | Δ vs control (SE) | Big-5 in a top 10 by units | Big-5 airplay offset (SE) | Whole Major tier |
|---|---|---:|---|---:|---|---:|
| Control | **34.7** (2.4; 26.0-46.0) | **+0.5** | | 38.7 | −4.0 (0.5) | 39.2 |
| `standing` (§9i) | 41.5 (3.1; 26.2-51.5) | +7.3 | +6.9 (2.7) | 43.4 | −1.8 (0.7) | 46.1 |
| Plateau K 25 | 39.7 (2.6; 30.4-49.0) | +5.5 | +5.0 (2.1) | 43.5 | −3.8 (1.0) | 43.6 |
| Floor, W .085 | 39.6 (2.4; 29.4-51.0) | +5.4 | +4.9 (1.0) | 41.8 | −2.2 (0.3) | 45.3 |
| Floor + plateau, W .088 | 39.4 (2.7; 28.1-53.1) | +5.2 | +4.8 (2.4) | 41.4 | −1.9 (0.6) | 45.5 |

- **The control is on history.** The Big-5 hold 34.7% of Top-10 slots against 34.2%. Its airplay offset (−4.0 on the
  Big-5, SE 0.5) is part of how it gets there: a top 10 ranked by units alone would be 38.7%.
- **Every 5e arm overshoots by ~5 points.** The floor arms sit at 39.4-39.6. Paired against the control, the floor
  alone is +4.9 (SE 1.0). Even the plateau, which kept most of the offset with the junk present (§9n), is +5.0.
- **So the remaining offset is a target.** 5e should hold the control's Big-5 share. The ~2 points of offset the
  floor arms lose, plus a slightly higher units-only share, are what carries them above history. History's 1960 is a
  single draw. Its own sampling noise is a few points (the 1961 bootstrap SD is 3.7, §17), so +5 is about 1.5 of
  those. But the control already matches, and the 5e lift is measured paired at 5 SE. It also points the wrong way
  for the next years: history's Big-5 fall to 20.6 / 21.2 / 26.0 / 18.7% in 1961-64, and the sim already runs high
  there (§17k; memory: Big-5 collective share drives CR4).

**Status.** 5e (`standing` + floor) stays unadopted until it holds the Big-5 Top-10 share within ~2 points of the
control. Measure on the Big-5 by label ID, not the runtime Major tier. Station-side options are now exhausted: non-sales
picks (§9m) and a size-blind order inside the sellers (§9n, with the floor) both fail. What remains is the
chart-points side, option (a) of §9l, or accepting 5e as an observe-only station picture.
