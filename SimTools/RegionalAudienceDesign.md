# Regional audience causality (audit step 3)

**Status: approved 2026-10-15 with the recommended values; 3a, 3f, 3b, 3c, 3g built. 3b, 3c, 3g on: none moves
the timing metrics (see the progress file). Why, and the proposed fix: [RegionalSyncDesign.md](RegionalSyncDesign.md).** Remedies [CelebrityEconomyAudit-2026-10-15.md](CelebrityEconomyAudit-2026-10-15.md)
§3 and §4. Progress and the decade watch list are in [CelebrityAuditProgress.md](CelebrityAuditProgress.md).

Audit acceptance for this step: *local peaks occur at different times; a national ranking decline does not erase
simultaneous local success; national sales still reconcile.*

## What actually synchronizes the regions (measured, not inferred)

Decomposition of `single-demand-stages.csv` from the audit's fresh seed-1001 run (13 weeks, 14,530 record-weeks
present in all seven regions). Each record's log demand in a region is split into the record's cross-region mean
(**common**) and that region's deviation from it (**local**). Script: `scratchpad/sync_decomp.py`, to be moved into
`SimTools/` as the step's metric.

| Share of… carried by | Common | Local |
|---|---:|---:|
| Record-to-record spread of demand within a region (what sets the regional ranking) | **98.3%** | 1.7% |
| …of which intrinsic conversion | 69.9% | 0.1% |
| …of which awareness (aware buyers / potential) | 28.4% | 1.6% |
| Week-to-week **change** in a record's regional demand (what sets timing) | **99.3%** | 0.7% |
| …of which intrinsic conversion | 72.9% | 0.0% |
| …of which awareness | 26.4% | 0.6% |

Peak timing, mid-decade `media-p3-hot36` (3,255 records over 50k units): the spread between a record's earliest and
latest regional peak week is **median 1 week**; 19% peak in the same week everywhere, 9.5% spread ≥4 weeks, 1.3% ≥8.

Three conclusions change the audit's list of suspects:

1. **The awareness floors are not the problem.** Only 0.9% / 0.6% of record-region-weeks sit on the 0.70 / 0.40
   floor. Removing them is a semantic fix, not the lever. *(Correction, RegionalSyncDesign §4: by units the floors
   carry 17% of demand and 36% of charted demand. The conclusion still holds: a no-floors probe changes nothing.)*
2. **Conversion is the synchronizer, and its movement is entirely record-wide.** It carries ~73% of every week's
   change and has *zero* local variation. Its moving parts are the release ramp and the post-week-8 age decay (both
   keyed to `record.weeksSinceRelease`) and the Hesbacher rank exposure (keyed to national rank). Quality and label
   scale are common too, but those are legitimately national — a good record is good everywhere.
3. **Awareness is mostly national by construction.** `effectiveAwareness = 0.4·national + 0.6·regional`, and regional
   awareness is fed by regional radio, which lerps toward the single national `radioHeat`. Station drops are rolled
   per region but against the *national* support ratio and weeks-since-peak.

So "a record rising in the West while fading in the East" is impossible today for a structural reason: every clock
that turns a record over is one national clock.

## Principle

Give each region **its own lifecycle state** — when the record arrived there, how it is selling there against its
own local best, and whether local stations still carry it — and make the clocks that turn a record over read that
state. National chart standing stays, as a **bounded information channel** into local state (programmers read the
trades, one-stops rack the national list), not as the clock itself. National totals remain the sum of local results.

A record launched everywhere at once by a national campaign should behave almost as it does now; the change should
bite where a region discovers a record late, or keeps buying it after the rest of the country has moved on.

## Design

### 3a. Local lifecycle state + metric (no behaviour change)

New fields on `RegionalRecordData` (saved; save-version bump):

| Field | Meaning |
|---|---|
| `localIntroWeek` | First chart week this region had the record "in play": local awareness ≥ `LocalIntroAwareness` (start 0.08) **or** local units ≥ the region-scaled demand floor already used by breakout (`150 × regionScale`). Launch regions with stock + campaign awareness are introduced in week 1. |
| `localPeakUnits`, `weeksSinceLocalPeak` | Running local maximum and clock, the regional twin of `peakWeeklyUnits` / `weeksSincePeakUnits`. |
| `LocalAge` (derived) | `currentChartWeek − localIntroWeek + 1`, or 0 if not introduced. |

Plus `SimTools/analyze_regional_sync.py`: common/local split of level and change, regional peak-week spread,
top-11 overlap, and the media "all qualifying regions → none" transition count. Byte-identical check against the
reference control (fields are written, nothing reads them).

### 3b. Local lifecycle clocks (the main lever)

- The post-8-week `DEMAND_AGE_DECAY_RATE` term in `CalculateRegionalSales` reads **`LocalAge`** instead of
  `record.weeksSinceRelease`. A region introduced in week 1 gets the identical curve.
- `GetReleaseRampWeight` **stays on record age**. It represents pressing and shipping build-up, which is
  record-wide, and resetting it at local introduction would dip a late region's sales just as it discovers the
  record. Launch-week volume is therefore unchanged.
- A region not yet introduced is not decayed at all (local age 0). Its demand is below the introduction floor by
  definition, so the change cannot inflate pre-introduction volume beyond a small amount; the short check measures
  it.
- National `ApplyWeeklyAwarenessAgeDecay` and radio-build stay on record age for now (national stock, one clock is
  correct there).

Expected effect: late-discovering regions (neighbour tests, rack-jobber breakouts, independents' home-region
launches) get their own climb instead of inheriting a record already at week 12's decay. This is the "local
surprise" acceptance scenario.

### 3c. Local radio fatigue and local heat

- **Station drops** (`UpdateRecordRegionalData`): `GetStationDropChance` is fed the **local** support ratio
  (`unitsSoldThisWeek / localPeakUnits`) and **local** `weeksSinceLocalPeak`. Same hazard function, same constants.
  This is the cleanest causal fix in the step: stations in a region drop a record because it stopped selling *there*.
- **Regional radio target**: today `target = record.radioHeat / difficulty × genreRadio × build`. Replace
  `record.radioHeat` with `localHeat = radioHeat × (1 − λ) + localEvidenceHeat × λ`, where `localEvidenceHeat` is
  the same `UpdateRadioHeat` formula evaluated on local inputs (local support/fatigue clock, local breakout score in
  place of national rank bonus). Start λ = 0.35; national heat keeps the majority (trade-paper influence was real).
- **Airplay convexity guard.** Airplay points apply `AIRPLAY_CONVEXITY` (5th power) **per region**, so any increase
  in regional spread raises total airplay points by Jensen's inequality even at constant mean rotation. 3c ships with
  a measured check of the airplay share of chart points (memory: airplay is ~45% of a hit's points) and, if it moves
  more than ±3 pts, a population-weighted renormalization of the regional targets back to the national mean — so the
  change redistributes rotation across regions without adding any.

### 3d. Rank exposure: national and local

`GetChartExposureWeight(nationalRank)` is retail/jukebox racking. Racks were filled from the national list *and*
from what was moving locally. Replace with a geometric blend
`exposure = nationalExposure^(1−μ) × localExposure^μ`, where `localExposure` uses the same Hesbacher shape on the
record's **rank in this region's last-week sales** (cheap: seven sorts of live singles per week), normalized to mean 1
like the national term so total units do not move. Start μ = 0.30.

This is the riskiest sub-step: the Hesbacher curve (#1/#10/#100) was fitted with feedback through rank, and
`chart-slot-weeks identity` / #1 tenure respond to anything that changes rank feedback. While regions agree (today
94% top-11 overlap) the blend is nearly the national curve; it only diverges after 3b/3c create real local
differences. Ship it **after** 3b/3c are measured, and re-run `SimTools/curve.py` on its short check.

### 3e. Awareness floors → bounded information deposit

The top-10/top-40 floors become a weekly deposit into **regional** awareness headroom
(`AddWithinHeadroom`, as step 1 did for discovery): national top-10 adds up to 0.05/wk, top-40 up to 0.02/wk,
scaled by the region's radio reach. Small by measurement (≤1.5% of rows today); it is here so a national chart
position *informs* a region instead of overwriting it. The 0.4/0.6 national/regional awareness blend is left alone
in this step and listed as the next lever if 3b–3d undershoot.

### 3f. Fan sales evidence: soft local score with hysteresis (media, observe-only)

Media is observe-only, so this is economy-inert and can land first.
- Rename the diagnostic now: `SellingRegions` → `TopSellerRegions` (it counts top-11 ranks, not selling regions).
- Replace the hard rank cut with a per-act, per-region **local sales standing**: EMA (half-life 3 weeks) of the
  act's summed singles units as a share of the region's single units, mapped through a soft curve. A region
  **enters** "selling" at standing ≥ the current #8–9 equivalent and **leaves** only below the #16–18 equivalent
  (hysteresis). Sales evidence and `selling` read the standing.
- Result to require: in the mid-decade replay, "7 → 0 qualifying regions in one week" transitions drop from 185 to
  near zero; acts still losing all regions over several weeks is fine.

### 3g. Hometown audience: a local seed for local state (built 2026-10-15; no sync effect, kept on)

**Measured reason (3b, 3c).** With local clocks and local radio both on, the local share of weekly demand change is
1.2% (control 0.4%) and the median peak-week spread is still 1 week. Each region now runs on its own state, but
every input to that state is common, so the regions settle on the same ranks and peak in the same week. A local
feedback loop amplifies a local difference; nothing creates one. 3d (local rack exposure) has the same problem: it
would rank on lists that are already 87% identical.

**What historically seeded the difference** was mostly people, not noise: an act's home town (local gigs, local
DJs championing local talent, the local paper), and the label's home and strong markets. The model already records
`SimulatedArtist.homeRegion`, but no market reads it. The label side is already causal through
`GetRegionalLaunchFactor` (strong 1.35, covered 0.55–1.0, uncovered 0.12–0.30).

Proposal: make the artist's home region a real audience, through the channels that already have to earn sales
(awareness and local radio), never as a conversion multiplier (audit §7: no hidden demand factors).
- **Launch:** a single's awareness in the act's home region gets a headroom deposit of `0.06 + 0.10 × localStanding`,
  where `localStanding` is the act's live following in its home region's towns. It reuses the player's
  `LocalFollowing` arithmetic: `1 − exp(−LocalSceneRoomService.LocalDraw(artist, city, day, 56) / 120)`, the
  highest across the region's cities. It is 0 for an act that has not played there. Static and draw-free.
- **Live following in any region**, not only home: the same following term feeds that region's launch awareness,
  so an act that tours a region's rooms has an audience waiting there.
- **Local radio:** in 3c's `localHeat`, the home region adds `+0.10 × (1 − nationalFame)`, where nationalFame is
  `EffectiveRecognition`. Local DJs champion an unknown local act; a national star gets no extra hometown push.
- **No new draws.** The home region already exists, and the renown term reads saved state.

Expected: a record by an act with a home market climbs there first, crosses the breakout and neighbour-test gates
earlier, and reaches other regions later through the existing propagation. That is the "local surprise"
scenario, and it is also what 3b's local clock needs in order to bite. Risks: home-region concentration
(NYC/LA host the most acts, so they gain the most; see the specialist-cities memory), and Major share, since
Majors sign more acts from big markets. Mapping `homeRegion` names to market region ids needs checking.

**Rejected alternative:** a keyed per-record × region "taste" multiplier. It would hit the overlap target by
construction, but it is the "unrelated regional randomness" the audit warns against.

If 3g also fails to move the timing metrics, the honest conclusion is that the target band needs staggered
launches. Independents historically broke a record in one city before shipping nationally, and that is a
distribution-policy change outside this step.

## RNG, switches, order

- All new state is deterministic; no new draws. **Correction (3c):** the drop roll is gated by
  `stationDropChance > 0` as well as `IsStationDropCandidate`, so a per-region chance changes how many rolls are
  made and 3c reshuffles the shared stream. 3d adds sorts, not draws.
- One switch per sub-step on `ChartManager` (`LocalLifecycleClock`, `LocalRadioFatigue`, `LocalRankExposure`,
  `RegionalAwarenessDeposit`) plus `MediaFans.SoftSalesStanding`; scene defaults flip on as each passes its short
  check; `--no-…` controls for the decade A/B.
- Order: **3a → 3f → 3b → 3c → 3d → 3e**. Each gets a 104-week seed-1001 catastrophe check against the previous
  arm (memory: one change at a time; short deltas can invert, so the decade A/B at the end is the verdict).

## Targets and guards

| Metric | Today | Target (judgement — no historical regional benchmark is fitted) |
|---|---:|---:|
| Common share of weekly demand change | 99.3% | ≤ 90% |
| Regional peak-week spread, 50k+ records, median | 1 wk | 3–5 wk |
| Share with spread ≥ 8 wk | 1.3% | 5–15% |
| Mean top-11 overlap between regions | 94% | 70–85% |
| Media 7 → 0 in one week (mid-decade) | 185 | < 20 |
| Settlement / reconciliation deltas | 0 / 0 | 0 / 0 (hard) |

Guards on every short check: total single units (±3%), airplay share of chart points (±3 pts), Hesbacher ratios
#10/#1 and #100/#1, #1 count and tenure, genre share error, Major-family share, closures/bankruptcies.

## What it will move (decade watch list)

- **Independents and regional genres should gain** — a home-region hit keeps its own clock instead of decaying on
  the national one. Expect Country / RnB / Gospel regional share up, which interacts with the genre chart guard (§6,
  step 6) and with "share is supply-bound": the guard may claw it back.
- **#1 tenure and chart turnover**: local clocks lengthen total life for records discovered late; slot-weeks are
  fixed (52,100), so longer lives mean fewer distinct charting records.
- **Major-family share**: national campaigns are unaffected by design, so if Major share moves it is a signal the
  blend parameters are too strong.

## Out of scope here

- Per-region **recognition** (artist fame stays one national stock; step 4 decides whether it splits).
- The national 0.60 awareness ceiling (deferred in step 1).
- Catalog revival / renewed demand for an old record (§17 — step 5 builds on 3b's local clock).
- Album regional clocks: albums have their own catalog-decay path; singles first, albums only if the metric shows
  album regions equally synchronized.

## Decisions for the author

1. **Scope of the national channel.** λ = 0.35 (radio) and μ = 0.30 (rack) keep national standing as the majority
   influence. Recommended as starting values; the alternative is a bolder 0.5 that will move calibration more.
2. **Targets.** The table's target band is a judgement, not a fitted benchmark. Accept it, or name a reference (e.g.
   sampled 1960s station surveys) to fit against.
3. **3d (local rank exposure) at all?** It touches the Hesbacher fit. Recommended: build it last and keep it only if
   3b+3c miss the timing targets.
