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

### 9p. Probe: is the units-only rise panel airplay feeding awareness? (`--station-regional-support-panel-free`, 2026-10-28)

`Big5ShareHandoff.md` lead 1. The floor arm (`ufw085`) lifts the Big-5 share of a top 10 ranked by units by +3.1 (SE
1.5) over the control. The suspected route was `data.awareness += data.radioPlay * 0.12f`, with `radioPlay` carrying the
panel blend.

**Probe.** `=awareness` makes the regional awareness update read the rotation the region would carry with no panel
blend. Chart points still read the blended `radioPlay`. `=sales` also takes the panel out of the breakout media
evidence (`mediaInput`). The panel-free value is tracked, not just read from `tailRadioPlay`. That is because the tail
decays last week's *blended* `radioPlay` (`tail = Lerp(radioPlay × 0.92, target, 0.15)`), so the panel's share carries
forward. `panelExcessRadio` follows it with the same linear decay (× 0.782 on rotation, `STATION_DROP_RESIDUAL` once
dropped). Runtime-only, not saved. **Inert unnamed:** `hs52op1001-SMP-HGRPO-ufw085e` is byte-identical to `-ufw085`
on all 123 CSVs.

A side fact this exposes: through that carry, W .085 is not an 8.5% panel weight. In steady state
`radioPlay ≈ 0.48 × target + 0.30 × panel` (W 0 gives 0.69 × target), so the panel is roughly a third of regional
rotation for a record a few weeks into its life. The time constant is ~3.5 weeks.

**Eight seeds, 1960, Big-5 by label ID** (`SimLogs/hs52op100{1-8}-SMP-HGRPO-{ufpfa,ufpfs}`; floor-arm flags plus the
probe; control is `-sp`; scratchpad `big5.py`):

| Arm | Big-5 Top-10 | Units-only top 10 | Offset | Big-5 share of all units | Airplay | Top-10 artists |
|---|---:|---:|---:|---:|---:|---:|
| Control | 34.7 | 38.7 | −4.0 | 28.2 | 25.5 | 81.8 |
| Floor (W .085) | 39.6 | 41.8 | −2.2 | 29.3 | 27.8 | 74.8 |
| Floor, panel-free awareness | 42.3 | 43.4 | −1.2 | 30.0 | 27.7 | 75.2 |
| Floor, panel-free awareness + breakout | 43.1 | 45.2 | −2.1 | 30.4 | 27.9 | 73.2 |

Paired against the floor arm (SE): panel-free awareness **+2.7 (1.3)** Big-5 Top-10, +1.7 (1.6) units-only, +0.7 (0.6)
unit share. With breakout as well: **+3.5 (1.3)**, +3.5 (1.4), +1.1 (0.6). Airplay is unchanged (±0.1), so chart
points are untouched as designed.

- **The rise does not vanish. It grows.** Taking panel airplay out of awareness moves the Big-5 *up* on every
  measure. Panel-fed awareness therefore leans toward non-Big-5 records. The panel follows each market's sellers,
  including regional sellers the label push does not reach. Without it, regional awareness comes only from the tail,
  which is driven by `radioHeat`, and that favours the pushed records.
- **So radio → awareness is not where the fix lives.** Lead 1's question has an answer: the floor arm's units-only
  rise comes from a route the probe leaves in place. What remains is chart rank feeding sales. Panel airplay seats
  Big-5 records higher, and rank sells through `GetChartExposureWeight`, the Top-10/40 awareness floors, national
  chart-visibility awareness, and the top-20 push hold in `UpdateLabelPush`. That fits §9o's split. The floor's Big-5
  share of *all* units rises only +1.1 (0.5), but its share of a units-ranked top 10 rises +3.1. The extra sales are
  concentrated on records the chart already holds high.
- **Implication for lead 2.** One chart-side change acts on both halves of the lift: the offset directly, and the
  units-only rise through rank feedback. Paying the panel term linearly, outside `AIRPLAY_CONVEXITY`, should be
  screened on both, and on the units-only share specifically. W re-calibration also has to account for the carry
  above, because the panel's effective weight in `radioPlay` is ~3.5× the nominal W.

**Status.** Probe off by default; not for adoption. Lead 1 is closed negative.

### 9q. Screen: the panel paid linearly, outside the airplay convexity (`--station-regional-support-panel-linear=L`, 2026-10-28)

`Big5ShareHandoff.md` lead 2. Chart points raise only the panel-free rotation (the §9p shadow, so the panel is out of
the tail's carry as well) to `AIRPLAY_CONVEXITY`. This week's `ReporterRadioPlay` is paid beside it as `L × panel`,
weighted by reach × population like the rest of airplay. Sales reads (awareness, breakout) keep the blended
`radioPlay` at W .085, so only the chart changes. **Inert unnamed:** `hs52op1001-SMP-HGRPO-ufw085f` is
byte-identical to `-ufw085` on all 123 CSVs.

**Calibration (seed 1001, airplay = pooled top-40 share; control 27.6, floor 27.6):**

| L | 0.001 | 0.03 | 0.06 | 0.1 | **0.15** | 0.2 | 0.5 | 1 | 8 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Airplay | 2.0 | 7.4 | 12.2 | 18.4 | **25.2** | 30.0 | 52.1 | 68.8 | 94.7 |
| Big-5 offset | +0.6 | +0.6 | +1.2 | −0.4 | −1.0 | −0.6 | +1.5 | −5.6 | −0.4 |

- **Under the floor arm, chart airplay is the panel.** With the panel out of the exponent, the tail alone gives 2%
  airplay. The tail is too small at the fifth power (the lerp toward `radioHeat / difficulty` settles well under the
  0.30 reference). This fits §9j, where airplay fell 26.5 → 13.5 as W went 0.13 → 0.092. Whatever pays the chart's
  airplay, its order is the panel's order.
- **A linear payout cannot be tuned near the exponent's.** It pays every region's modest rotation, which the fifth
  power used to crush. That is why L 0.5 already doubles airplay.

**Eight seeds at L 0.15, 1960, Big-5 by label ID** (`SimLogs/hs52op100{1-8}-SMP-HGRPO-lin015`; control `-sp`):

| Arm | Big-5 Top-10 | Units-only top 10 | Offset | Big-5 share of all units | Airplay | Top-10 artists |
|---|---:|---:|---:|---:|---:|---:|
| Control | 34.7 | 38.7 | −4.0 | 28.2 | 25.5 | 81.8 |
| Floor (W .085) | 39.6 | 41.8 | −2.2 | 29.3 | 27.8 | 74.8 |
| Floor, panel linear L .15 | 43.3 | 43.8 | **−0.5** | 30.2 | 25.0 | 75.5 |

Paired against the floor arm (SE): Big-5 Top-10 **+3.8 (1.9)**, offset **+1.7 (0.4)**, units-only +2.0 (1.9). Against
the control: +8.7 (2.1). Per seed the offset is −1.0 / +1.2 / 0.0 / −0.4 / −1.0 / +0.2 / −2.3 / −0.4, and none is below
the floor arm's −2.2 mean. Airplay sits on the guard (25.0) and breadth is unchanged (75.5).

- **Linear payment takes the offset to zero.** A linear payout of a sales-ordered panel gives each record airplay in
  proportion to its regional sales standing. That re-ranks nothing against units. The floor arm's remaining −2.2 most likely came
  from the convexity (not tested directly): it piles points on each region's very top records, so a strong regional seller can pass a
  national seller of the same total. Linearising removes that and leaves airplay purely as reinforcement.
- **The handoff's premise does not hold here.** Compounding of the panel with a pushed record's tail can only work
  where the tail matters, and under the floor arm the tail is ~2% of airplay. The Big-5 lift is not a tail × panel
  interaction. It is the panel's composition (§9k: Majors gain seats in every tier), and no monotone payout of that
  panel can produce an offset against sales.

**Status.** Probe off by default; not for adoption. Lead 2 is closed negative. Together with §9k, §9m, §9n and §9p, every
route that keeps one sales-ordered panel scoring the chart has now failed to hold the Big-5 share. What remains is lead
3 (§9l option (a), the national panel scores the chart, the control by construction), or a non-sales signal *inside
the High tier*, which §9m showed costs the airplay guard.

### 9r. Screen: option (a) with the unsold floor, eight seeds (`--station-regional-support-chart=national`, 2026-10-28)

`Big5ShareHandoff.md` lead 3. This is §9l's option (a) with the §9n floor fix. The regional meeting (`standing`, S 25,
σ 0.5, unsold floor) runs on shadow state and feeds only the playlist export. The national meeting scores the chart
at the calibrated W 0.13 (`--station-regional-support-panel-weight` is unnamed). The national meeting never calls
`Support()`, so the floor reaches only the shadow playlists. Runs `SimLogs/hs52op100{1-8}-SMP-HGRPO-optaf`.

**The chart is the control's on all eight seeds.** Every CSV except the playlist export (122/123) is byte-identical to
`-sp` (1001, 1002, 1006-1008), or to §9l's `-opta` (1003-1005), which was already identical to the control. So the Big-5
Top-10 share is 34.7 (history 34.2) and the Big-5 offset is −4.0, by construction. Airplay is 25.5 and top-10 artists
81.8.

**The station shape is the floor arm's** (survey cities, Top40 + FullService, 8-seed means):

| | Same city | Cross city | Cross rank / absent | Uncharted slots | Major share High / Mid / Light |
|---|---:|---:|---|---:|---|
| History 1960 | 69.7 | 40.3 | 53.8 / 17.8 | – | – |
| Floor (W .085, scores the chart) | 68.7 | 50.7 | 60.1 / 19.6 | 7.7 | 41.4 / 38.3 / 33.0 |
| **Option (a) + floor** | **68.7** | **51.9** | **61.4 / 18.0** | 8.0 | 39.0 / 37.1 / 32.4 |
| Option (a), no floor (§9l, 5 seeds) | 72.0 | 40.6 | 46.0 / 35.2 | 32.3 | 25.3 / 23.9 / 27.4 |

**The cost: the playlists shown are not the panel that scores.** Measured against the national meeting's own export
(the `-sp` lists, which are the chart's panel here), per station-week, Top40 + FullService, on the five seeds that
have it (scratchpad `overlap.py`):

| | Option (a) + floor | Option (a), no floor (§9l) |
|---|---:|---:|
| Share of the shown list also on the station's national list | 19.4 | 16.6 |
| Shown ranks 1-11 also on the national list | 33.6 | 25.5 |
| Shown High records also High nationally | 24.6 | 18.4 |
| Big-5 share of High: shown / scoring | **32.7 / 20.5** | 19.2 / 18.4 |

- **Option (a) meets the handoff goal.** The Big-5 share is the control's, and the station shape keeps everything the
  floor bought (same city 68.7, history-shaped mismatch mix). Cross-city overlap stays at ~51 against 40. That is
  the regional-sales lockstep, and no station knob reaches it.
- **The floor widens the gap in what the player sees.** The shown lists now follow each market's sellers, so a
  third of their High slots are Big-5. The size-blind panel that scores the chart gives the Big-5 a fifth. A player
  reading a DJ's sheet sees more Big-5 rotation than the chart is paying for. They agree on a fifth of slots, and on
  a third of the top eleven.
- **This is what §9k-9q add up to.** The control's Big-5 share depends on a scoring panel that is *not* sales-ordered.
  Any one panel that looks like 1960 station sheets is sales-ordered, so it overshoots by ~5. This holds whether it is
  re-weighted (§9j), re-picked (§9m), re-ordered (§9n), kept out of sales (§9p) or re-paid (§9q). Option (a) works
  because it keeps two panels.

**Status.** Off by default. The handoff's goal, Big-5 within ~2 points of the control while keeping the station shape,
is met only by option (a). Adopting it is a game-design choice, not a calibration one: which sheet the player-facing
reads (`SpinTierOf`, Rolodex, courtesy calls, DJ adds, break claims) should follow. In this probe they all still
read the national meeting, so the standing lists exist only in the export.

### 9s. Regional breaks: what history shows, why the model cannot make one, and an offline-tested design (2026-10-28)

**The goal, restated by the author.** Regional radio should drive the simulation the way it historically did: an
indie record gets hot in a town and spreads. Option (a) (§9r) holds the Big-5 share only by keeping regional
stations out of the simulation, so it is not that. This section works offline on existing runs, with no new seeds.

**History** (`SimTools/analyze_regional_break_history.py`). Los Angeles (KFWB + KRLA) and Minneapolis (KDWB) have
weekly top-40 surveys for all of 1960. For each Hot 100 record debuting Feb-Nov: its first week on each city's
survey, its Hot 100 debut, and the gap between the cities. Matching is on title words plus an artist word, within
±26 weeks. Labels come from the 102 labelled 1960 Top-10 records (§17).

| 1960 | n | Leads the Hot 100 ≥ 2 wk | City gap ≥ 3 / ≥ 6 wk | Top 10 in one city before the other has it | One city only |
|---|---:|---:|---|---:|---:|
| Top 10, Big-5 | 20 | **0** | 16.7 / **0** | 44 | 10 |
| Top 10, indie | 54 | **18.5** | 33.3 / **14.6** | 44 | 11 |
| Peak 41-100 | 243 | 16.0 | 58.6 / 24.1 | 7 | 33 (vs 12 in both) |

Big-5 hits reached every market together. Indie hits had a tail of staggered local breaks: about one in six took 6+
weeks to cross from one city to the other, and about one in five was on a local survey 2+ weeks before the Hot 100.
Most records below the top 40 were one-city records. The examples are the familiar ones: "Image of a Girl" (Eldo),
"Mission Bell" (Era), "Stay" (Herald), "A Thousand Stars" (Indigo), "Mule Skinner Blues" (Soma).

**The model** (scratchpad `model_spread.py`, the same metrics). The "city survey" is the union of a city's Top40 +
FullService playlists; the "region" is the region's weekly sales list. Five seeds, 1960, Top-10 indie:

| Top 10, indie | Lead ≥ 2 | Gap ≥ 3 / ≥ 6 | Top 10 in one city first |
|---|---:|---|---:|
| History | 18.5 | 33 / 15 | 44 |
| Control, city playlists (national panel) | 27.6 | 12 / 3.6 | 1.5 |
| Floor arm, city playlists (standing) | 0 | 25 / **0** | 19 |
| Control, regional sales (westcoast vs greatplains) | 0 | 13 / **0** | 5 |

(The control's "lead" is the national meeting seating trial records before they chart, in both cities at once:
gap 0.) Peak 41-100 records are already near history (floor arm: 38% one-city only).

**Why the model cannot make a regional break.** Measured on `ufw085` seed 1001 (scratchpad `diag_local.py`,
`sizing.py`, `decomp.py`):

1. *Regions differ in level, never in timing.* An indie hit's per-capita demand is ~5× higher in its best region than
   its worst. That ratio is flat from 5 weeks before its national debut to 8 after, and the same region leads every
   week for 96% of hits. Indie hits lean more on national awareness than Big-5 hits do: ~35% of their effective
   awareness is regional, against ~55% for Big-5. Regional media input is ~0.05 before debut and rises only after
   the record charts, so radio follows the chart.
2. *A hit is made by conversion, not exposure.* Young non-Big-5 singles in their label's home region, before charting:

   | Eventual national peak | Home demand / wk | Effective awareness | × needed for home top 40 / top 10 |
   |---|---:|---:|---|
   | Top 10 | 2,953 | 0.37 | 0.7 / 2.2 |
   | Never charts | 107 | **0.29** | **20 / 65** |

   93% of the log gap is conversion (22×). Of that, quality is 1.9×; the rest is the kinked `SingleCommercialFit`
   draw (σ 2.5, 15% hit share, floor 0.12, launch weight 1.25), visible as a bimodal inferred fit (flop mode near −1.1,
   connected mode near +2). A flop already has 29% awareness everywhere. Local airplay can at most triple awareness,
   so no local champion can make an ordinary single a local hit. And a single that *does* connect does so in all
   seven regions from week 1, so it goes national at once.
3. *The breakout plumbing is too small to matter.* A regional breakout adds 0.006-0.02 local awareness a week and a
   neighbour test ~0.002-0.006, against a national blend of 0.4 × national awareness (~0.1-0.25 for a hit). And it
   only triggers once a record already sells heavily in that region.

**Design: exposure-gated fit realization.** Keep the draw: most records really did fail to connect, and "fit varies
by record and market" is the research basis (DemandAttentionResearch §8). But a record's connection is *realized*
only in regions where it is exposed (worked and played). Unexposed, it sells at the flop level: its national fit is
capped at the median. Exposure:
- **At launch:** the label's home region, plus each other region with probability `0.3 + 0.7 × label national reach`.
  Big-5 and the large indies (reach ~1) are exposed everywhere, so they connect everywhere at once, as in history.
- **Later:** a neighbour region once the record ranks in a reached region's top 40 (2-week lag); everywhere once it is
  in the national top 40 (1 week).
- **A late region runs its own clock from exposure.** This is essential: on the national clock a late region gets an
  already-aged record and the stagger vanishes.
- Flops are under the cap whatever happens, so they lose nothing. Launch stock and awareness are never held. That is
  the cost that sank the October arrival arms (§6c: Small-tier bankruptcies came from lost out-of-territory launch
  weeks).

**Offline replay** (`SimTools/replay_exposure_gated_fit.py`). This takes each 1960 single's actual regional demand
curve from a run and replays it under an exposure rule, recomputing regional and national (units-only) ranks each
week. With every region exposed at launch, it reproduces the run (no stagger, Big-5 top-10 by units 37.7 vs the run's
39.6 on cleared units). Floor arm, three seeds:

| Seed | Rule | Indie top 10: gap ≥ 3 / ≥ 6 / one city first | Big-5 top 10 gap ≥ 6 | Big-5 share of units top 10 | Indie live demand |
|---|---|---|---:|---:|---:|
| 1001 | all exposed (= run) | 8 / 0 / 2 | 0 | 37.7 | 383M |
| | gated | 44 / 15.4 / 21 | 0 | 41.2 | 340M |
| | gated, 15% of Big-5 connects demoted | 45 / 17.5 / 20 | 0 | **38.1** | 341M |
| 1002 | all exposed | 13 / 0 / 7 | 0 | 35.2 | 378M |
| | gated, demoted | 45 / 11.8 / 29 | 5.9 | **33.8** | 341M |
| 1006 | all exposed | 6 / 0 / 6 | 0 | 40.0 | 391M |
| | gated, demoted | 42 / 13.3 / 33 | 0 | **36.2** | 349M |

Other arms on 1001: the national clock gives no stagger (gap ≥ 6: 0). Gating by reach alone with a narrow spread
(neighbour top 10) gives gap ≥ 6 of 9% but costs indies 21% of demand. Trade-report spread (any region's top 10/20/40
→ everywhere) adds little beyond the national top-40 rule.

- **The stagger has history's shape:** indie hits gap ≥ 6 in 12-18% of cases (history 15), Big-5 hits ~0 (history 0),
  one-city-first 20-33% (history 44).
- **The cost is the familiar one, but now it can be read for what it is.** Gating takes ~10% of indie demand and adds
  +1.7 to +3.5 to the Big-5 units top 10. That demand is the "connected but modest" records that used to sell a
  little in all seven regions and now stay in their home markets, which is history's shape for records below the top
  40. The fix is in *who connects*, not in exposure. Today a stronger launch moves the kink (launch weight 1.25), so
  Big-5 singles connect more often *and* everywhere. Demoting ~15% of Big-5 connects returns the share to within
  ~1-2 points of the baseline on all three seeds. In the game that is a lower launch weight (or a reach-specific
  one), not a new term.
- **Limits of the replay.** It uses raw demand (no stock, no market purse), a units-only national chart, and no
  feedback from rank to demand or from airplay to awareness. History's "leads the Hot 100" cannot be measured, since a
  regional top-40 seller is already a units-only national top-100 record. It is a sizing tool for the rules and the
  rebalance, not a verdict.

**Building it (proposal).** `--regional-fit-exposure`, off by default:
1. Per-region `fitExposedAge` (−1 = not yet). In `SingleCommercialFit.Multiplier`, an unexposed region reads
   `min(nationalLog, cap)` (the regional draw is untouched).
2. Exposure at launch: home region (label home; the act's scene city where 3g knows it) plus a keyed draw
   `< 0.3 + 0.7 × EffectiveNationalReachForRecord`. Later: neighbour region `localSalesRank ≤ 40` (2-week lag),
   national top 40 (1 week). Connected records only (the national draw above the cap), so flops are not touched.
3. On a late exposure, `localIntroAge` takes the exposure age, so 3b's demand-age clock restarts there.
4. Radio is the carrier: in a second step, the neighbour rule becomes a station rule. A station adds a record that
   ranks in an adjacent market's top sellers (the trade-report pick), and exposure is that add. The panel reads
   regional sales (5e floor), so the break shows in the stations a player sees.
5. Rebalance `--single-commercial-fit-launch-weight` against the Big-5 Top-10 benchmark (34.2).

Screen: 8 seeds, 52 weeks. Score with the §9s metrics on city playlists and regional sales, plus Big-5 Top-10 (§9o),
airplay, units, and Small-tier bankruptcies (§6c).

### 9t. Screen: exposure-gated fit realization (`--regional-fit-exposure`, 2026-10-28)

§9s steps 1-3 and 5, built as `RegionalFitExposure`. Step 4 (radio as the carrier) is not built. Per region,
`fitExposureAge` is the record age from which its connection is realized. It defaults to 0 (from launch), so albums,
player records, older saves and switch-off runs are unchanged, and it is saved only when nonzero. An unexposed region
reads the national log capped at `SingleCommercialFit.UnexposedCapLog`, the reference launch's median draw (−1.335, a
factor of 0.26, the flop mode). The regional draw is untouched, and the hit-act fit floor applies after the cap, so
a hit act's follow-up keeps its floor everywhere.
- **Launch:** the label's home region and the act's 3g home region, plus a keyed draw under 0.3 + 0.7 × the label's
  national reach for the record.
- **Spread:** connected records only. A neighbour of an exposed region is exposed 2 weeks after the record is in that
  region's local top 40, and every region the week after it is in the national top 40.
- **Late regions:** `localIntroAge` restarts at the exposure.
- **Requirements:** the switch requires the kinked fit and the 3b local clock, and it computes the local sales ranks
  itself if 3c is off.

Base: the floor arm (`ufw085` flags). Runs `SimLogs/hs52op100{1-8}-SMP-HGRPO-{ufx,ufxl075}`. `ufxl075` adds
`--single-commercial-fit-launch-weight=0.75`. That cuts the modelled Big-5 connect rate 23.3% → 19.6%, the replay's
"15% demoted", while indies move only 16.3% → 15.7%.

**Inert unnamed:** `hs52op1001-SMP-HGRPO-ufw085h` is byte-identical to `-ufw085` on all 123 CSVs. On seed 1001 the
switch holds 32% of launch regions (8,948 of 28,000) and spreads ~420 (neighbour 347, national 71).

**Stagger** (8 seeds, `model_spread.py`; city = LA vs Minneapolis Top40 + FullService, region = westcoast vs
greatplains sales):

| | Indie top 10: gap ≥ 3 / ≥ 6 | One city first | Lead ≥ 2 | Big-5 top 10 gap ≥ 6 | Peak 41-100 gap ≥ 6 |
|---|---|---:|---:|---:|---:|
| History 1960 | 33 / 15 | 44 | 18.5 | 0 | 24 |
| Floor arm, city | 23 / 0.5 | 18 | 0.5 | 0.4 | 2.6 |
| **Gated, city** | **36 / 4.5** | **24** | 2.5 | 0.4 | 8.3 |
| Gated + LW 0.75, city | 34 / 4.3 | 26 | 2.9 | 1.0 | 9.7 |
| Floor arm, region | 17 / 0.5 | 10 | 0.3 | 0 | 0 |
| Gated, region | 24 / 1.1 | 14 | 0.6 | 0.4 | 10.6 |
| Gated + LW 0.75, region | 26 / 2.2 | 11 | 1.1 | 0 | 7.6 |

**Headline and cost** (8 seeds, paired vs the floor arm, SE in brackets; bankruptcies per seed-year):

| | Big-5 Top 10 | Offset | Big-5 unit share | Airplay | Top-10 artists | Bankruptcies |
|---|---|---|---|---|---|---|
| Floor arm (level) | 39.6 | −2.2 | 29.3 | 27.8 | 74.8 | 37.9 |
| Gated | +3.0 (2.3) | +1.6 (0.8) | +3.1 (1.1) | −1.3 (0.2) | −3.6 (1.6) | +3.5 (1.1) |
| Gated + LW 0.75 | +0.6 (2.1) | +0.6 (0.9) | +1.7 (0.8) | −1.0 (0.3) | +1.8 (1.7) | +2.5 (1.6) |

- **The direction is history's, the size is a third of it.** Indie gap ≥ 3 reaches history's level and one-city-first
  moves 18 → 24, but gap ≥ 6 is 4.5 against history's 15 and the replay's 12-18. Big-5 hits stay at ~0 and
  records below the top 40 open up (gap ≥ 6: 2.6 → 8-11).
- **Why it is smaller than the replay: the national rule fires early.** Indie top-10s debut on the Hot 100 in their
  first week and reach the top 40 in a median of 4 weeks (41% within 3) in both arms. The national top-40 rule then
  exposes every region before a 2-week neighbour lag can open a 6-week gap. In the replay the chart was units-only,
  so a record held in most regions charted late. In the game, launch airplay and the reporter panel chart it anyway.
  So in the game the bar for "the trade press has it" is about as early as a record's first chart weeks.
- **The rebalance works as the replay said.** At launch weight 0.75, the Big-5 Top-10 is +0.6 over the floor arm
  (SE 2.1), and breadth is back (+1.8 artists). The Big-5 *unit* share stays +1.7 (SE 0.8): connected-but-modest indie
  records lose the regions they never reach.
- **Bankruptcies rise.** The total is +2.5 to +3.5 per seed-year, almost all Independent tier (25.0 → 27.6 / 28.2),
  and Small stays 0.4-1.5. This is the §6c cost in a milder form: the stock is not held, but connected-but-modest
  records now earn only in their home markets.

**Status.** Off by default. The mechanism is live and history-shaped in direction, but the stagger is a third of the
target, and the binding rule is national exposure at chart top 40. Next, prototype in the replay before more runs:
the national rule gated by the record's *units* rank (the trade-report spread) or by a later chart bar (top 20 / 10),
and the radio carrier (step 4), which spreads exposure through station adds instead of the chart. Then rescreen at
LW 0.75 with bankruptcies scored.

### 9u. The late region must relive the record: replay v2 and the relive rule (`--regional-fit-exposure-relive`, 2026-10-28)

§9t's next step was to prototype a later national bar and the radio carrier in the replay. First the replay was checked
against the built game, and it did not match. Replay v2 is `SimTools/replay_regional_relive.py`, which uses the floor
arm's (`ufw085`) demand, eight seeds, the §9t rules, and LW 0.75 as "demote 0.16".

**Calibration.** Here the game's own runs are scored on the replay's metric (regional raw-demand ranks, westcoast vs
greatplains, game chart):

| Indie top 10 | Gap ≥ 3 / ≥ 6 | One city first | Big-5 units | Big-5 Top 10 | Indie demand |
|---|---|---:|---:|---:|---:|
| Game, floor arm `ufw085` | 12.7 / 0.3 | 5.7 | 27.2 | 39.6 | 376M |
| Game, gated `ufx` | 14.1 / 0.8 | 6.2 | 30.5 | 42.5 | 328M |
| Replay v1 clock (whole curve local), §9t rules | 39.9 / 14.1 | 26.1 | 29.5 | 42.4 | 330M |
| Replay, national clock, §9t rules | 10.6 / 0.0 | 4.0 | 29.7 | 39.9 | 326M |

The cost side of v1 was right, but its stagger was not. In the game a held region enters its top 40 **in the same week
it is exposed** (indie top-10 hits, 704 late regions: median delay 0, found from the jump in conversion). The reason
is that 3b's local clock moves only the post-week-8 decay (`ChartSimulator` demand age). Awareness is national, and
the conversion ramp is the chart exposure weight: the Hesbacher curve on the *national* position, which rises 2.5 log
by week 8, the same as the whole conversion ramp. So a late region gets the record's current rack and jukebox
exposure in full. v1 replayed the whole curve on the local clock, which a game region never does. With a national
clock the replay matches the game on stagger and units. It misses ~2.6 of the game's Big-5 Top-10 rise (there is no
chart → demand feedback), so the Big-5 Top-10 deltas below are understated.

**The national bar does not bind** (national clock, demote 0.16):

| Rule | Indie gap ≥ 3 / ≥ 6 | One city first | Big-5 Top 10 | Indie demand |
|---|---|---:|---:|---:|
| §9t (chart top 40) | 13.9 / 0.5 | 4.6 | 38.9 | 327M |
| Chart top 20 / top 10 / top 5 | 18-20 / 0.5 | 11-12 | 37.6-38.1 | 326M |
| Units top 20 / top 10, or no national rule | 18-21 / 0.2 | 9-12 | 35.6 | 326M |
| Chart top 10, neighbour top 20 | 33.9 / 3.2 | 18.6 | 38.1 | 314M |
| Chart top 10, neighbour top 10, lag 3 | 43.1 / 10.5 | 25.3 | 39.5 | 302M |
| Chart top 10, radio top 10, 1-3 adds/wk | 33-35 / 3.7-4.6 | 20-23 | 38.4-38.6 | 307M |
| Chart top 10, no neighbour rule | 43.4 / 14.1 | 25.2 | 40.2 | 298M |

With the national rule removed, the neighbour rule still spreads a hit to every region before any national bar
matters. §9t's reading that the national rule fires early was wrong. A slower chain (tighter neighbour or radio rank,
longer lag) does open a stagger, but only by holding sales: each point of gap ≥ 6 costs ~2.5M of indie demand. The
radio carrier is that kind of chain. A region's Top-40 stations take ~7 new records a week, and capacity hardly binds,
since 2 or 3 adds a week give the same result. What matters is the rank bar.

**What does work is the late region reliving the record's rise.** Same §9t spread rules, demote 0.16:

| Late region | Indie gap ≥ 3 / ≥ 6 | One city first | Big-5 hits gap ≥ 6 | Peak 41-100 gap ≥ 6 | Big-5 units | Big-5 Top 10 | Indie demand |
|---|---|---:|---:|---:|---:|---:|---:|
| History 1960 | 33 / 15 | 44 | 0 | 24 | | 34.2 (§9o) | |
| National clock (= game) | 13.9 / 0.5 | 4.6 | 0 | 0 | 27.5 | 38.9 | 327M |
| Awareness local, conversion national | 24.2 / 2.2 | 10.0 | 0 | 0 | 28.2 | 39.8 | 312M |
| Conversion local, awareness national | 39.6 / 7.8 | 24.0 | 2.1 | 14.3 | 27.6 | 39.6 | 331M |
| Generic ramp on weeks since exposure (floor 0.1-0.4, 4-8 wk) | 24-39 / 1.2-5.5 | 10-24 | ≤ 1.7 | ≤ 2.4 | 27.5-27.7 | 39.0-39.4 | 304-317M |
| **Relive** (the game rule) | **45.0 / 19.7** | **28.1** | 3.8 | 6.5 | 28.2 | 40.8 | 323M |
| **Relive, launch base 0.5** | **27.8 / 12.0** | 14.4 | **0.4** | 4.8 | **27.0** | 40.2 | **348M** |

- **Relive:** in a late region, the chart exposure weight reads the national position the record held at the region's
  local age (weeks since exposure), and the release ramp runs on that age. Awareness, the chart floors and the fit are
  unchanged. History's story is that the racks, jukebox operators and one-stops in a market stock a record as it
  breaks *there*. A generic ramp fails because it hands the late region a record whose national position may already
  be past its peak, so that region never gets a peak.
- It holds on every seed: indie gap ≥ 6 is 14-26% on each of the eight (relive, base 0.3).
- The stagger is close to free, because the late region still gets its whole lifecycle (indie demand −1% against the
  national clock). The costs are Big-5 hits staggering (3.8% gap ≥ 6, against history's 0) and Big-5 Top-10 +1.9 in
  the replay (+3-4 likely in the game, given the missing feedback). Staggered indie hits peak lower nationally.
- Launch base 0.5 (each other region exposed at launch with chance 0.5 + 0.7 × reach) trades some stagger (gap ≥ 6:
  12 vs history 15; one-city-first: 14 vs 44) for Big-5 hits back at ~0, Big-5 units at the floor level, and half the
  gating's indie demand loss recovered (348M vs 323M, against the floor arm's 376M). That should cut the §9t
  Independent-tier bankruptcy cost.
- Further demotion (0.25 / 0.30) moves the replay's Big-5 Top-10 by less than a point.

**Built:** `--regional-fit-exposure-relive` (requires `--regional-fit-exposure`). `RegionalFitExposure.NotePosition`
logs the internal position each week's demand reads (`RecordRuntimeData.fitRelivePositions`, saved when non-null).
`ReliveAge` and `PositionAt` feed `GetChartExposureWeight` and `GetReleaseRampWeight` in `CalculateRegionalSales`.
Also `--regional-fit-exposure-launch-base=B` (default 0.3). Off, both are identity.

Screen: 8 seeds, 52 weeks, floor arm + `--regional-fit-exposure`, LW 0.75. Runs `hs52op100{1-8}-SMP-HGRPO-ufxr`
(relive) and `-ufxr5` (relive, launch base 0.5).

**Screen result** (8 seeds, all at LW 0.75; `ufxl075` is §9t's gated arm). Stagger (`model_spread.py`; city = LA vs
Minneapolis Top40 + FullService, region = westcoast vs greatplains cleared units):

| | Indie top 10, city: gap ≥ 3 / ≥ 6 | One city first | Indie, region: gap ≥ 6 / one first | Big-5 top 10 gap ≥ 6 (city) | Peak 41-100 gap ≥ 6 (city) |
|---|---|---:|---|---:|---:|
| History 1960 | 33 / 15 | 44 | | 0 | 24 |
| Floor arm `ufw085` | 23 / 0.5 | 18 | 0.5 / 10 | 0.4 | 2.6 |
| §9t gated `ufxl075` | 34 / 4.3 | 26 | 2.2 / 11 | 1.0 | 9.7 |
| **Relive `ufxr`** | 36 / **9.3** | **33** | **7.0** / 24 | 2.9 | 9.2 |
| Relive, base 0.5 `ufxr5` | 34 / 5.6 | 24 | 5.4 / 18 | 1.0 | 4.9 |

Per seed, indie city gap ≥ 6, ufxl075 / ufxr / ufxr5: 4/13/7, 12/0/13, 2/7/2, 0/14/5, 6/16/6, 2/10/9, 6/5/0, 0/10/3.
Paired against ufxl075: relive +5.2 (SE ~2.8), base 0.5 +1.5 (SE ~1.3).

Headline (paired vs the floor arm, SE; bankruptcies per seed-year):

| | Big-5 Top 10 | Offset | Big-5 unit share | Airplay | Top-10 artists | Bankruptcies (Indep.) | Indie live demand |
|---|---|---|---|---|---|---|---:|
| Floor arm (level) | 39.6 | −2.2 | 29.3 | 27.8 | 74.8 | 37.9 (25.0) | 376M |
| §9t gated, LW 0.75 | +0.6 (2.1) | +0.6 (0.9) | +1.7 (0.8) | −1.0 (0.3) | +1.8 (1.7) | 40.4 (28.2) | 320M |
| Relive | +1.9 (1.8) | +0.6 (0.7) | +2.5 (0.7) | −0.4 (0.3) | −1.0 (2.0) | 40.1 (27.1) | 300M |
| **Relive, base 0.5** | **−0.6 (1.2)** | +0.5 (0.5) | **+0.4 (0.4)** | −0.2 (0.4) | **+3.2 (1.9)** | **38.6 (25.8)** | **329M** |

- **The relive rule works in the game, but at about half the replay's size.** Relive doubles the indie city stagger
  (gap ≥ 6: 4.3 → 9.3, history 15; one-city-first: 26 → 33, history 44), and on regional sales it triples (2.2 → 7.0).
  The replay said 19.7, and its cost was smaller: indie demand −6% against §9t vs the replay's −1%. The diagnosis below
  shows the gap was the replay's error (raw Hesbacher), not chart feedback. Big-5 hits stagger slightly (2.9, history 0).
- **Launch base 0.5 is the cleaner arm.** Every cost metric is at or better than the floor arm: Big-5 Top-10 −0.6,
  unit share +0.4, breadth +3.2 artists. Bankruptcies are +0.7 against §9t's +2.5, and indie demand is above §9t. It
  keeps a modest stagger (city gap ≥ 6: 5.6, region 5.4 against the floor's 0.5), but its paired gain over §9t on the
  city metric is within noise.
- **Status.** Both off by default. Relive is the mechanism the stagger needs, and launch base 0.5 is the
  cost-neutral setting. Neither reaches history's 15 / 44. The diagnosis follows.

**Diagnosis: why the game gave half the replay** (`ufxr` vs the replay, eight seeds, indie top-10 hits, regional
measure). The case mix is the same: the later city is held at launch in 34% of pairs (replay 38%), and connected
indie singles carry ~2 held regions in both, top-10 hits included, so there is no selection against held records. The
whole shortfall sits inside that case. There the game's gap averages 4.0 weeks against the replay's 5.6, with game
gaps peaking at 3 weeks and the replay's at 6.
- **Exposure timing is not it.** The spread rules fire ~1.4 weeks after the early city's top-40 entry in both (the game
  runs ~0.6 weeks earlier throughout, which cancels in the gap). The game's exposure week was fitted from the step in
  a held region's conversion relative to its launch-exposed regions, net of the relive path. The step is +2.7 log,
  the expected fit-minus-cap jump. 16% of game cases show no step and reach the top 40 without exposure (gap 2.6, none
  ≥ 6).
- **The ramp after exposure is the gap:** ~2.5 weeks to the local top 40 in the game against ~4.2 in the replay.
- **Cause: the replay's relive used raw Hesbacher (21×, 3.05 log).** The game's chart exposure weight is the Hesbacher
  ratio ^0.62 with a top-10 plateau (`CHART_EXPOSURE_EXPONENT`, `CHART_EXPOSURE_PLATEAU_RANK`): 4.4×, or 1.5 log. With
  the game's function, the replay's relive gives indie gap ≥ 6 / one-city-first **8.6 / 24** (game 7.0 / 24), and at
  launch base 0.5 **4.5 / 13.5** (game 5.4 / 18). The replay is now calibrated to the built rule
  (`replay_regional_relive.py` defaults `--expo 0.62`; `--expo -1` gives the raw first cut). Chart feedback is a
  minor term: it adds ~3 points of indie demand cost (game −6% vs replay −3%).
- **Conversion-side reliving has nothing more to give.** The game's relive already resets the whole conversion ramp
  (exposure + release ramp ≈ the measured +2.5 log), and it matches the replay's "conversion local" arm (7.8).

**What is left: awareness.** A late region still gets the national blend (0.4 × record awareness) and the national
top-10 / top-40 awareness floors (0.7 / 0.4) at full strength from the week it is exposed. With awareness also on the
local clock (the replay's full local clock, which is now the target rule):

| Late region | Indie gap ≥ 3 / ≥ 6 | One city first | Big-5 hits gap ≥ 6 | Peak 41-100 gap ≥ 6 | Big-5 units | Big-5 Top 10 | Indie demand |
|---|---|---:|---:|---:|---:|---:|---:|
| National clock (§9t) | 13.9 / 0.5 | 4.6 | 0 | 0 | 27.5 | 38.9 | 327M |
| Relive (built, game function) | 41.8 / 8.6 | 24.1 | 2.9 | 5.3 | 27.7 | 39.4 | 318M |
| Relive + awareness | 42.9 / **18.9** | 26.0 | 3.3 | 29.0 | 27.8 | 40.6 | 330M |
| Relive + awareness, base 0.5 | 28.6 / 10.2 | 15.3 | 0.9 | 13.8 | 26.9 | 39.9 | **351M** |

In history, Minneapolis did not know an LA indie hit before its own stations and racks had it. A game rule would log
`record.awareness` by age beside `fitRelivePositions`. In a late region, the national blend and the chart floors would
read the record's awareness and position at the region's local age. Before any build, discount the replay by the
feedback term (indie demand −3% more in the game, Big-5 Top-10 understated by ~1-2.6).

**Built and screened: awareness relive** (`--regional-fit-exposure-relive-awareness`, requires relive, 2026-10-28). The
record's national awareness is logged by age beside the positions (`fitReliveAwareness`, saved when non-null). In a
late region, the national blend, the top-10 / top-40 awareness floors and the chart visibility read the awareness and
position at the region's local age. The region's own accumulated awareness (the 0.6 share) is not relived. Runs
`hs52op100{1-8}-SMP-HGRPO-ufxa` (base 0.3) and `-ufxa5` (base 0.5), LW 0.75, eight seeds, all exit 0, no exceptions.

| Indie top 10 | City gap ≥ 3 / ≥ 6 | City one first | Region gap ≥ 6 / one first | Big-5 hits gap ≥ 6 (city) | Peak 41-100 gap ≥ 6 (city) |
|---|---|---:|---|---:|---:|
| History 1960 | 33 / 15 | 44 | | 0 | 24 |
| §9t gated `ufxl075` | 34 / 4.3 | 26 | 2.2 / 11 | 1.0 | 9.7 |
| Relive `ufxr` | 36 / 9.3 | 33 | 7.0 / 24 | 2.9 | 9.2 |
| **Relive + awareness `ufxa`** | **44 / 16.1** | **41** | **15.4 / 30** | 2.8 | 7.0 |
| Relive + awareness, base 0.5 `ufxa5` | 32 / 7.7 | 26 | 7.0 / 19 | 0.0 | 4.3 |
| (Replay prediction, base 0.3 / 0.5) | | | 18.9 / 26 · 10.2 / 15 | | |

Per seed, indie city gap ≥ 6, `ufxa`: 11, 18, 14, 21, 26, 13, 13, 15. Every seed is ≥ 11, and the paired gain over
`ufxl075` is +12 (SE ~2). For `ufxa5`: 4, 10, 2, 16, 5, 7, 9, 9 (+3.6 vs `ufxl075`).

| Paired vs floor arm (SE) | Big-5 Top 10 | Offset | Big-5 unit share | Airplay | Top-10 artists | Bankruptcies (Indep.) | Indie live demand |
|---|---|---|---|---|---|---|---:|
| §9t gated | +0.6 (2.1) | +0.6 (0.9) | +1.7 (0.8) | −1.0 (0.3) | +1.8 (1.7) | 40.4 (28.2) | 320M |
| Relive + awareness | +1.9 (1.8) | +0.8 (0.7) | **+2.4 (0.7)** | −0.1 (0.3) | −2.1 (1.7) | 38.6 (26.5) | 299M |
| Relive + awareness, base 0.5 | +0.9 (1.5) | +0.2 (0.8) | +1.2 (0.6) | −0.3 (0.4) | +1.4 (1.8) | 39.9 (25.4) | 323M |

Against `ufxl075`, `ufxa` loses 3.9 Top-10 artists (SE 1.2); `ufxa5` loses 0.4 (SE 0.8).

- **History's indie stagger, reached for the first time.** Base 0.3 gives indie top-10 gap ≥ 6 of 16 (history 15) and
  one-city-first of 41 (44). The replay predicted it (regional 18.9 vs 15.4 built; one-first 26 vs 30), so replay v2
  now forecasts the built rules. Gap ≥ 3 overshoots (44 vs 33).
- **Its costs** are Big-5 unit share +2.4 over the floor arm (significant), Big-5 Top-10 +1.9 (within noise),
  breadth −3.9 artists against §9t, and indie demand −7% against §9t. Bankruptcies do not rise (38.6, the floor arm
  37.9, §9t 40.4). Big-5 hits still stagger a little (2.8, history 0). Records below the top 40 do not open up (7.0,
  history 24).
- **Base 0.5** halves the stagger (7.7 / 26), is near cost-neutral, and has no Big-5 stagger.
- **Status.** Off by default. Relive + awareness at base 0.3 is the first arm on history's indie shape. What remains
  is its Big-5 cost (unit share +2.4, Top-10 +1.9, breadth): staggered indie hits sell less nationally, and the
  Big-5 picks up the share. The candidates are an intermediate launch base (0.4) or a lower launch weight, run
  through replay v2 before any seeds.

**Replay grid: launch base × launch weight** (relive + awareness = the replay's full local clock, eight seeds,
2026-10-28). LW enters the replay as a share of connected singles that never connect, read from the fit formula. A
record connects (draws above the median) when z > −LW × (launch awareness − 0.2), so P(hit) = Φ(LW (a − 0.2) − kink).
On 1960 launches (Big-5 median awareness 0.32, indie 0.17), relative to LW 1.25 that gives Big-5 / indie hits:
LW 0.75 −16% / −4% (reproduces §9t's 23.3 → 19.6 and 16.3 → 15.7), 0.5 −22% / −6%, 0.25 −28% / −7%, 0 −34% / −9%.
Lowering LW cuts Big-5 hits and also, less, indie hits (`--demote` / `--demote_indie`). A first grid that demoted the
Big-5 alone, at steps past LW 0, overstated the gain.

Calibration: replay base 0.3 / LW 0.75 against the built `ufxa` on the regional raw-demand measure gives 17.0 / 25.4 /
28.2 / 40.7 vs 14.4 / 28.4 / 29.7 / 41.5 (gap ≥ 6 / one first / Big-5 units / Big-5 Top 10). The game is stagger
× 0.85, units +1.5, Top-10 +0.8, and the city gap ≥ 6 is ~1.12 × regional. Projected game values, floor arm at Big-5
units 27.2, Top-10 39.6, history city gap ≥ 6 at 15:

| Base / LW | Replay gap ≥ 6 / one first | Big-5 hits gap ≥ 6 | Replay Big-5 units / Top 10 | **Projected game: city gap ≥ 6** | **Big-5 units vs floor** | **Big-5 Top 10 vs floor** | Indie demand (replay) |
|---|---|---:|---|---:|---:|---:|---:|
| 0.3 / 0.75 (= `ufxa`, built) | 17.0 / 25.4 | 3.4 | 28.2 / 40.7 | 16.1 (actual) | +2.5 (actual) | +1.9 (actual) | 324M |
| 0.3 / 0.5 | 16.0 / 24.8 | 2.1 | 27.8 / 40.4 | ~15.2 | +2.1 | +1.6 | 321M |
| **0.3 / 0.25** | 15.7 / 24.6 | 2.2 | 27.1 / 39.5 | **~14.9** | **+1.4** | **+0.7** | 319M |
| 0.3 / 0 | 15.0 / 23.2 | 1.8 | 26.7 / 39.4 | ~14.3 | +1.0 | +0.6 | 317M |
| 0.35 / 0.25 | 13.9 / 21.2 | 1.3 | 27.0 / 39.2 | ~13.2 | +1.3 | +0.4 | 324M |
| 0.35 / 0 | 12.5 / 19.7 | 1.8 | 26.5 / 39.0 | ~11.9 | +0.8 | +0.2 | 321M |
| 0.4 / 0 | 12.0 / 19.1 | 1.4 | 26.3 / 38.7 | ~11.4 | +0.6 | −0.1 | 326M |

- **Launch base sets the stagger. LW sets the Big-5 cost.** Going from LW 0.75 to 0.25 costs ~8% of the stagger and
  takes ~1.1 off Big-5 units and ~1.2 off Big-5 Top-10.
- **No setting is fully neutral at history's stagger.** At base 0.3 / LW 0.25 the projection is city gap ≥ 6 ~15,
  Big-5 Top-10 +0.7 (the benchmark metric; §9t's accepted gated arm was +0.6), and Big-5 unit share +1.4 (`ufxa`:
  +2.5). Buying the last point of unit share costs about 2 points of stagger per point (base 0.35-0.4).
- LW 0 removes the launch-awareness term from the fit altogether, which is a design change (the Big-5 entry edge is
  campaign impact; the launch weight carries part of it). LW 0.25 keeps it.
- Indie demand does not recover with lower LW (it moves −2 to −3%). The gating's indie loss is a launch-base effect.

Candidate screen (not run): relive + awareness, base 0.3, LW 0.25, plus base 0.35 / LW 0.25 as the cheaper bracket.

**Screen: relive + awareness at LW 0.25** (2026-10-28). Runs `hs52op100{1-8}-SMP-HGRPO-ufxal025` (base 0.3) and
`-ufxa35l025` (base 0.35), eight seeds, all exit 0, no exceptions.

| Indie top 10 (city) | Gap ≥ 3 / ≥ 6 | One city first | Region gap ≥ 6 / one first | Big-5 hits gap ≥ 6 | Peak 41-100 gap ≥ 6 |
|---|---|---:|---|---:|---:|
| History 1960 | 33 / 15 | 44 | | 0 | 24 |
| §9t gated `ufxl075` | 34 / 4.3 | 26 | 2.2 / 11 | 1.0 | 9.7 |
| `ufxa` (base 0.3, LW 0.75) | 44 / 16.1 | 41 | 15.4 / 30 | 2.8 | 7.0 |
| **`ufxal025` (base 0.3, LW 0.25)** | **41 / 14.4** | **36** | 14.9 / 30 | 2.9 | 9.8 |
| **`ufxa35l025` (base 0.35, LW 0.25)** | **42 / 11.8** | **35** | 12.6 / 28 | 2.7 | 8.4 |
| (Projected from replay grid) | ~15 · ~13 | | | | |

Per seed, indie city gap ≥ 6: `ufxal025` 17, 17, 8, 16, 15, 18, 7, 19; `ufxa35l025` 6, 19, 8, 5, 15, 17, 10, 14.

| Paired vs floor arm (SE) | Big-5 Top 10 | Offset | Big-5 unit share | Airplay | Top-10 artists | Bankruptcies (Indep.) | Indie live demand |
|---|---|---|---|---|---|---|---:|
| §9t gated `ufxl075` | +0.6 (2.1) | +0.6 (0.9) | +1.7 (0.8) | −1.0 (0.3) | +1.8 (1.7) | 40.4 (28.2) | 320M |
| `ufxa` | +1.9 (1.8) | +0.8 (0.7) | +2.4 (0.7) | −0.1 (0.3) | −2.1 (1.7) | 38.6 (26.5) | 299M |
| **`ufxal025`** | +1.3 (0.8) | +0.8 (1.1) | +1.1 (0.6) | −0.3 (0.4) | **+5.2 (1.2)** | 38.6 (25.2) | 289M |
| **`ufxa35l025`** | **−2.1 (2.6)** | −0.5 (0.4) | **−0.1 (1.1)** | −0.4 (0.4) | **+6.0 (1.5)** | **37.2 (25.2)** | 295M |

(Floor arm: Big-5 Top 10 39.6, unit share 29.3, 74.8 artists, 37.9 bankruptcies, 376M.)

- **The replay grid held.** Base 0.3 / LW 0.25 came in at city gap ≥ 6 of 14.4 (projected ~15), Big-5 units +1.1
  (+1.4) and Big-5 Top-10 +1.3 (+0.7, within SE). Base 0.35 came in at 11.8 (~13).
- **LW 0.25 buys back the Big-5 cost and breadth.** Against `ufxa`, Big-5 unit share is −1.3 and the Top-10 has
  +7 artists (+5.2 over the floor arm, SE 1.2). Bankruptcies stay at or below the floor arm's level.
- **`ufxal025` is the history-shaped candidate.** Gap ≥ 6 is 14.4 / 15, one-city-first 36 / 44, and gap ≥ 3 overshoots
  (41 / 33). Big-5 Top-10 is +1.3 (SE 0.8), the only cost left, and borderline.
- **`ufxa35l025` is the cost-neutral one.** Big-5 Top-10 is −2.1, unit share −0.1, bankruptcies 37.2 (floor 37.9).
  Its stagger is 11.8 / 15 and one-city-first 35 / 44, and it is noisier across seeds (5-19).
- **Still off history:** Big-5 hits stagger ~3% (history 0), and records below the top 40 do not open up (8-10,
  history 24). Indie live demand is 289-295M, against 320M (§9t) and 376M (floor arm), yet bankruptcies are not
  higher. The demand lost is in the regions that never reach the record, which history's shape asks for.
- **Not yet:** the local break before the national chart (lead ≥ 2: ~1% vs history 18.5), and regional success
  causing a hit (whether a record hits is still its launch fit draw). These are the next design questions; both can
  be screened in replay v2 first.

### 9v. Replay: a breadth-aware national chart does not open the local lead (2026-10-29)

Handoff item 1 proposed weighting chart points by regional coverage so a record selling in one region waits in
bubbling-under. Tested in replay v2 (`replay_regional_relive.py`, new options `--breadth G --bk K --bq Q --bfloor F`:
score × (Σ panel share × min(1, (K / local rank)^Q))^G; `--bthresh T`: records listed in regions under T of the panel
rank below every record that clears it; `--unexp F`: indie demand in unexposed regions × F). Panel share per region =
its share of demand (eastcoast .37, greatlakes .24, westcoast .15, the rest .02-.08). Base arm = `ufxal025` in replay
(`--clock relive --nat chart --p0 .3 --demote .28 --demote_indie .073`), eight seeds.

| Replay arm | Indie top-10 lead ≥ 2 | Region gap ≥ 6 | Peak 41-100 gap ≥ 6 | Regional top-10 on national that week | Indie demand |
|---|---:|---:|---:|---:|---:|
| History 1960 (city lists) | 18.5 | (city 15) | 24 | 85-88 | |
| Base | 1.1 | 8.0 | 7.8 | 100 | 304M |
| Soft breadth G 0.5-2, Q 1-3 | 0.0-1.1 | 6.7-7.6 | 5.7-8.7 | 100 | 300-303M |
| Hard list (Q 50), K 20-40 | 0.0 | 4.7-6.6 | 5.2-10.3 | 100 | 301M |
| Threshold T .25-.5 | 0.7-1.5 | 5.9-7.3 | 7.1-8.7 | 100 | 301-304M |
| Unexposed indie demand × .1-.5 | 1.4 | 7.9 | 7.7 | 100 | 294-298M |
| Launch exposure p .1 + .4 reach | 2.5 | 10.4 | 14.4 | 100 | 261M |
| Launch exposure p 0 + .2 reach (+ soft G 1) | 3.8 (4.4) | 11.5 | 30.3 | 100 | 234M |
| Home only (+ soft G 1) | 7.7 (10.2) | 12.2 | 53.1 | 99.9 | 220M |

Why no chart rule can do it:
- **Every local seller is already a national record.** The seven regional top 40s hold only ~75 distinct records a
  week (each on ~3.7 lists), fewer than the 100 national slots. Any coverage weight or threshold leaves slots that are
  filled by score, and a regional top-10 seller always has the score: 100% chart nationally that week, against 85-88%
  of LA / Minneapolis survey top-10 entries in 1960 (`analyze_city_list_vs_hot100_history.py`: 75% of all survey
  entries on the Hot 100 that week, 65% of ranks 21-40; game cities `ufxal025` 84-88%, top 10 99.5%).
- **Hits chart before they are local sellers anywhere.** Indie top-10 hits debut at a median age of 2. At debut 207 of
  443 are in no region's top 40; the first regional top 40 comes at age 2-4 (median 3). A breadth weight can at best
  hold the debut to the first listing (lead 0-1), which is what the hard list does: lead exactly 0.
- **Station adds follow the national chart.** In the game (`analyze_hit_station_adds.py ufxal025`), an indie top-10 hit
  is on a median of 0 Top40/FullService stations the week before its debut, 2 at debut and 9 a week later (Big-5: 0, 2,
  12). City-level overlap is already historical (Minneapolis top 30 also in LA: 65% vs history 72%), and the 50
  stations list ~126 distinct records a week, ~40 of them off the national chart. So a station-reporter chart would be
  selective, but it would see a hit no earlier than the national chart does.
- **Unexposed regions do not matter; launch breadth does.** Cutting indie demand in unexposed regions changes nothing
  (they sell little). The lead moves only with the number of regions exposed at launch (indie mean 4.2 at base 0.3 +
  0.7 reach, 2.4 at .1 + .4, 1.4 at 0 + .2), and even home-only launches give 8-10 against 18.5, at a 28% indie demand
  cost and a peak 41-100 stagger of 53 (history 24).

Conclusion: the lead is not a chart-weighting problem. A hit has to *sell locally first*, for weeks, while the rest of
the country has not heard it, and in the model a hit's first two weeks already sell nationally out of the launch
regions. Leads for a next step, both outside the chart: (a) local stations adding records from local one-stop sales
before the trades (the station path leading the national chart rather than following it), and (b) a slower launch for
indie records: home plus few regions, with spread through the neighbour rule, costed against indie demand. (b) is a
change to how indie distribution works at launch, and the cost is large enough that it is the author's call.

### 9w. Replay: how early can stations add a hit? (2026-10-29)

Lead (a) from §9v: stations add records from local sales before the trades do. Ceiling test in replay v2
(`replay_onestop_station_adds.py`, `ufxal025` replay arm, eight seeds, 443 indie top-10 hits): a station in westcoast
or greatplains adds any record up to 6 weeks old once its local units rank among those new releases is ≤ B (the
one-stop's "moving" report), the next week (lag 1) or the same week (lag 0, an oracle).

| Station rule | Added before debut | Lead ≥ 2 | Never added | Adds per region-week | Adds that never chart |
|---|---:|---:|---:|---:|---:|
| History 1960 | | 18.5 | | | ~13% of entries |
| New-release top 10, lag 1 | 1.4 | 0.2 | 16 | 3.6 | 2% |
| New-release top 20, lag 1 | 8.6 | 2.7 | 11 | 6.3 | 8% |
| New-release top 40, lag 1 | 21.2 | 9.0 | 7 | 11.0 | 25% |
| New-release top 20, lag 0 (oracle) | 43.6 | 8.6 | 11 | 6.3 | 8% |
| Top 20 with launch exposure 0 + .2 reach | 13.7 | 7.8 | 21 | 6.1 | 32% |
| Home-region stations add every home-label release at age 1 | | ≤ 8.8 | | | |

- **A sales-reading station caps at ~9%.** The local sales that make a record a new-release leader are the same sales
  that chart it nationally at age 2 (debut age: 1: 23, 2: 215, 3: 67, 4: 77, 5+: 61). Reading them a week sooner (the
  oracle) does not help: an add at the debut week is lead 0.
- **History's 18.5% needs adds before any sales.** 46% of indie hits debut at age 3 or later, so lead ≥ 2 at 18.5%
  needs LA or Minneapolis stations to add about 40% of eventual indie hits at age 1, before they sell. Home stations
  playing home labels reach at most 8.8%, because only 18.5% of indie hits come from westcoast or greatplains labels.
- So stations can lead sales only through **non-sales picks at release** (DJ ear, promo man, payola), and those picks
  must find hits more often than chance. Either the DJ hears the record's connection (the pick reads a noisy view of the
  fit draw: stations forecast hits, and fit stays drawn), or the pick makes the connection (a locally earned fit,
  handoff item 2). These are different meanings of commercial fit, so the choice is the author's.

### 9x. Screen: release-week DJ picks that hear the fit (`--station-picks-score=fit`, 2026-10-29)

The author chose the "DJ hears the fit" reading of §9w: fit stays drawn, and programmers forecast it. Built on the §9m pick
block (`StationPicks`): `--station-picks-score=fit` ranks records up to `--station-picks-age` weeks old by
exp(national log draw + S × keyed normal per station and record), `--station-picks-fit-noise=S`, only among records
the label serviced to the station's region at release (the §9t launch footprint). `--station-picks-fit-bar=B` admits only
records heard B log units above the connection line (a hit sits ~2.6 above it). `--station-picks-fit-unserviced`
picks only records held in some region at launch. Replay first (`replay_dj_ear_picks.py`): 4 picks per region-week at
noise 1, launch footprint only, gives indie lead ≥ 2 of 18.1 and Big-5 8.3. City-list precision scorer:
`score_city_list_precision.py`.

Runs `hs52op100{1-4}-SMP-HGRPO-<tag>`, `ufxal025` flags plus the picks, age 2, Light tier. All exit 0. 1005-1008 of
the first three arms were stopped unfinished. Paired against `ufxal025`, four seeds (SE).

| City LA / Minneapolis | Indie top-10 lead ≥ 2 | Big-5 top-10 lead ≥ 2 | All debuts lead ≥ 2 | Peak 41-100 lead ≥ 2 | Indie gap ≥ 6 | Peak 41-100 gap ≥ 6 | LA new / week, never chart | Big-5 Top 10 | Top-10 artists | Bankr. |
|---|---:|---:|---:|---:|---:|---:|---|---|---|---:|
| History 1960 | 18.5 | 0 (of 20) | 14.5 | 16 | 15 | 24 | 15.8, ~10% | | | |
| `ufxal025` | 1.6 | 0 | 8.7 | 12.9 | 14.6 | 9.0 | 10.1, 13% | | | 41.5 |
| `djf2n1` 2 slots, S 1 | 15.9 | 10.5 | 28.1 | 32.6 | 23.5 | 24.0 | 12.5, 23% | +3.0 (2.4) | −2.2 (2.5) | 37.2 |
| `djf1n1` 1 slot, S 1 | 7.0 | 7.3 | 21.2 | 25.6 | 18.9 | 17.9 | 11.3, 19% | −0.9 (2.8) | −4.0 (0.7) | 38.5 |
| `djf2n05` 2 slots, S .5 | 19.8 | 11.9 | 28.9 | 32.4 | 18.5 | 19.3 | 12.1, 20% | +4.9 (2.2) | −3.8 (2.5) | 37.8 |
| `djb2` + bar 2 | 16.9 | 2.8 | 26.8 | 29.4 | 22.2 | 17.9 | 12.1, 22% | +3.4 (3.4) | −1.0 (3.5) | 36.8 |
| `djb2u` + bar 2, unserviced | 11.0 | 0.9 | 26.3 | 31.4 | 20.8 | 18.7 | 12.4, 22% | +2.9 (2.2) | −4.0 (2.3) | 37.2 |
| **`djb24u` + bar 2.4, unserviced** | **18.9** | **3.0** | 27.5 | 31.4 | 21.5 | 16.5 | 12.2, 22% | +2.5 (2.8) | −3.5 (5.0) | 40.0 |

- **The indie lead is solved.** `djb24u` gives 18.9 against 18.5, with a Big-5 lead of 3.0 (history 0 of 20). The bar is
  what removes the Big-5 lead (11.9 → 2.8); the unserviced switch adds little.
- **Records below the top 10 lead about twice as often as history** in every arm (peak 41-100: 26-33 vs 16; all
  debuts 21-29 vs 14.5). Noise, the bar and the unserviced switch do not move it. Picks mostly list records early that
  the sales ranking would have listed later anyway (LA takes only ~2 more new records a week). Those ~2 are almost all
  records that never chart, so the never-chart share goes 13% → 22%.
- **Why precision cannot fix the ratio.** A hit and a record peaking 41-100 are both connected draws. What separates
  them is mostly the label's launch and push, which a DJ hearing only the draw ignores. In history the city lead was
  about as common for 41-100 records (16) as for indie hits (18.5); here picks give the low records the lead about 1.7×
  as often as the hits.
- **Costs.** Big-5 Top 10 +2.5 to +4.9 in every arm with two slots (1-2 SE at four seeds). Even with Big-5 records barely
  picked (`djb2u`) it is +2.9, so the lift does not come from Big-5 picks. A likely route is the pick block taking Light
  slots away from the sales ranking (airplay share −1.7 to −2.4 in every arm). Not tested. Top-10 artists −1 to −4.
  Bankruptcies are down 1.5-5.

**Extra pick slots** (`--station-picks-extra`: the block is added on top of the Light tier, and re-adds keep the tier's
slack; 2026-10-29). Same four seeds, `djb24u` flags plus the switch.

| | Indie top-10 lead ≥ 2 | Big-5 lead ≥ 2 | All debuts | Peak 41-100 lead ≥ 2 | Indie gap ≥ 6 | LA new / wk, never | Big-5 Top 10 | Big-5 Top 10 units-only | Top-10 artists | Bankr. |
|---|---:|---:|---:|---:|---:|---|---|---|---|---:|
| `djb24u` (reserved, 2) | 18.9 | 3.0 | 27.5 | 31.4 | 21.5 | 12.2, 22% | +2.5 (2.8) | +1.2 (3.6) | −3.5 (5.0) | 40.0 |
| `djb24ux` (extra, 2) | 13.7 | 3.6 | 25.6 | 31.0 | 19.3 | 12.5, 24% | −0.5 (1.4) | −1.1 (2.3) | −2.5 (2.5) | 41.0 |
| `djb24ux1` (extra, 1) | 10.0 | 3.4 | 18.6 | 22.8 | 18.0 | 11.2, 19% | +5.5 (1.8) | +6.0 (2.0) | −4.2 (3.1) | 38.2 |

- **The displacement hypothesis is not supported.** Taking the reservation away removes the Big-5 cost at two slots, but
  one extra slot, which displaces nothing and adds fewer picks, gives the largest cost of any arm (+5.5). The units-only
  Big-5 Top 10 moves the same way (+6.0), so the swing is in sales, not chart weighting. At four seeds the Big-5 cost of
  these arms (−0.5 to +5.5) is within reshuffle noise and cannot be attributed to a pick rule. It needs eight seeds.
- **The low-peak ratio does not change.** Peak 41-100 lead per point of indie lead is 1.7-2.3 in every arm (history
  0.86). One slot brings the low-peak lead near history (22.8 vs 16) only by halving the indie lead (10.0).
- **The indie lead drops with extra slots** (18.9 → 13.7, about 1.5 binomial SE on ~180 hits), so `djb24u` stays the
  candidate.
