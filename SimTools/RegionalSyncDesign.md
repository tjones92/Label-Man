# Why the regions move in lockstep (audit step 3, follow-up design)

**Status: investigation done 2026-10-15; design proposed, awaiting approval.** Follows
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
