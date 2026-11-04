# Handoff: the regional break → neighbour → national pipeline (2026-10-29)

**Goal (the author's).** Regional radio should drive the simulation as it did historically: an indie record gets hot
in one town, spreads to its neighbours, and goes national. Background is `RegionalSyncDesign.md` §9s-9u. All work
below is opt-in, on `media-atmosphere`, and uncommitted at the time of writing.

## Where it stands

The timing of the stagger is now close to history for indie hits. The causal story behind it is not built: a record
still hits because of its launch fit draw, and the national chart sees it at the same time as its home town does.

| 8 seeds, 1960, city = LA vs Minneapolis Top40 + FullService | Indie top 10: gap ≥ 3 / ≥ 6 | One city first | Lead ≥ 2 | Big-5 hits gap ≥ 6 | Peak 41-100 gap ≥ 6 | Big-5 Top 10 vs floor | Top-10 artists vs floor |
|---|---|---:|---:|---:|---:|---|---|
| History | 33 / 15 | 44 | 18.5 | 0 | 24 | (34.2 absolute) | – |
| Floor arm `ufw085` (no gating) | 23 / 0.5 | 18 | 0.5 | 0.4 | 2.6 | level 39.6 | level 74.8 |
| §9t gated, LW 0.75 `ufxl075` | 34 / 4.3 | 26 | 2.9 | 1.0 | 9.7 | +0.6 (2.1) | +1.8 (1.7) |
| **Relive + awareness, base 0.3, LW 0.25 `ufxal025`** | **41 / 14.4** | **36** | 1.7 | 2.9 | 9.8 | +1.3 (0.8) | **+5.2 (1.2)** |
| **Same, base 0.35 `ufxa35l025`** | 42 / 11.8 | 35 | 1.6 | 2.7 | 8.4 | **−2.1 (2.6)** | **+6.0 (1.5)** |

Bankruptcies per seed-year: floor 37.9, `ufxl075` 40.4, `ufxal025` 38.6, `ufxa35l025` 37.2.

**What is built** (each off by default):
- `--regional-fit-exposure` (§9t). A connected single realizes its fit only in regions where it is exposed.
  Unexposed regions read the median draw. Exposure at launch is the label's and act's home plus a keyed draw under
  base + 0.7 × reach. It spreads to a neighbour of a region where the record is in the local top 40 (2-week lag), and
  to every region once it is in the national top 40 (1 week).
- `--regional-fit-exposure-relive` (§9u). In a late region, the chart exposure weight and the release ramp read the
  record's position and age at the region's local age.
- `--regional-fit-exposure-relive-awareness` (§9u). In a late region, the national awareness blend, the top-10 /
  top-40 awareness floors and the chart visibility also read the local age.
- `--regional-fit-exposure-launch-base=B` (default 0.3).
- `--single-commercial-fit-launch-weight` already existed; LW 0.25 is the screened value (the profile uses 1.25, and
  §9t screened 0.75).

## Is the ~3% Big-5 hit stagger a problem? No.

- **Size.** In `ufxal025`, 6 Big-5 top-10 hits over 8 seed-years staggered ≥ 6 weeks (0.75 a year, out of ~26). They
  are ~1% of Big-5 units and 0.4% of all units, and they still sold. In `ufxa35l025` it is 0.6 a year and 0.2% of
  units.
- **History cannot rule it out.** History's 0 is 0 of 20 records. At a true rate of 3%, seeing 0 of 20 has a 54%
  chance; only rates above ~14% are excluded.
- **Where it comes from.** Big-5 labels with national reach below 1 still hold a few regions at launch
  (0.3 + 0.7 × reach). If it ever matters, set Big-5 launch exposure to 1. It is not worth a run now.

## What is left, in order of importance

### 1. The local break must come before the national chart (lead ≥ 2: ~1.7% vs history 18.5%)
In 1960, about one indie hit in five was on a local survey 2+ weeks before the Hot 100. In the model, hits debut
nationally at a median age of 2-3 weeks, because the home region and the regions exposed at launch sell enough to
chart at once. National success runs alongside the local break instead of following from it. This is the gap a
player would notice first.
- **Tested 2026-10-29 (§9v): a breadth-aware chart does not work.** Soft weights, hard lists and reporter thresholds
  give lead 0-1.5 in replay. The regional top 40s hold only ~75 distinct records a week, so every local seller already
  charts, and hits chart at age 2 before they are local sellers anywhere. Station adds follow the debut (median 0 stations
  the week before). Only fewer launch regions move the lead (home-only: 8-10, at −28% indie demand). Next: stations that
  add from local sales before the trades, or a slower indie launch (author's call).
- **Stations reading local sales sooner cap at ~9% (§9w).** Even an oracle station adding the week a record is a local
  new-release top-20 seller gives 8.6%. 18.5% needs LA / Minneapolis stations to add ~40% of eventual indie hits at
  age 1, before sales, so stations can lead only through release-week picks that find hits: the DJ hears the fit draw,
  or the pick earns the connection (item 2). Author's call.
- **Built and screened (§9x; open work in `DjEarPickHandoff.md`): release-week DJ picks that hear the fit.** `--station-picks=2 --station-picks-score=fit
  --station-picks-age=2 --station-picks-fit-noise=0.5 --station-picks-fit-bar=2.4 --station-picks-fit-unserviced` (`djb24u`)
  gives indie lead ≥ 2 of 18.9 (history 18.5) and Big-5 3.0 (0). Still open: records below the top 10 lead ~2× history
  in every arm, the never-chart share of new city records goes 13% → 22%, and Big-5 Top 10 is +2.5 (SE 2.8) at four seeds.
- **(Superseded) Lead: a breadth-aware national chart.** The 1960 Hot 100 was a survey of ~110 outlets across the country (63
  stations, 25 one-stops, 22 retailers; see `ChartSimulator` near the Hesbacher notes). A record selling in one
  region reached few reporters, so it sat in bubbling-under until it spread. Weight a record's chart points by its
  regional coverage, for example by the share of regions where it ranks in the local top 40, or by survey reporters
  drawn per region, before ranking.
- **Risk.** Indie hits chart later, so they lose chart exposure weight, top-40 awareness floors and the national
  exposure rule for longer. Indie demand falls and the Big-5 share rises (the §9t mechanism, but larger). Expect to
  rebalance with launch base or LW again.
- **How to test.** Replay v2 already models a chart (`--nat chart`: game units and airplay scaled by the replay's
  demand ratio). Add a coverage term to that score and measure lead ≥ 2 directly. Note the replay limit in §9s: a
  regional top-40 seller is already a units-only national top-100 record, so the lead can only be read with the
  breadth term in place.

### 2. Regional success should cause the hit (today the launch fit draw decides)
Whether a single is a hit is set by its kinked commercial fit draw at launch (σ 2.5, 15% hit share). Exposure gating
only delays where a predestined hit shows up. A record that did not draw a connection cannot break anywhere. §9s
measured that local airplay can at most triple awareness, against the 20-65× a flop would need to reach a local top
40 / top 10. So "a sleeper breaks in Cleveland and goes national" cannot happen.
- **Lead: a locally earned connection.** Part of a record's realized fit in a region would rise with local
  exposure: heavy rotation there, a station champion (the rolodex DJ / advocacy channel), or a strong home scene.
  The existing spread rules would then carry the earned connection outward. Records that connect only through local
  earning would be history's regional breakouts.
- **This is a design decision for the author.** It changes what commercial fit means (drawn vs earned), it touches
  the player's levers (DJ relations, promo), and it interacts with `HitActFitSupport`. Agree the rule before
  modelling it.
- **How to test.** Replay v2 can only demote records (send a connected record to the flop level). Promoting a flop
  needs a connected demand curve for it: scale its flop curve by the fit factor its draw would need, and check that
  against connected records with a similar launch.

### 3. Records below the top 40 barely stagger (peak 41-100 gap ≥ 6: 8-10% vs history 24%)
Below the top 40 the model already has history's one-city share (~40% of these records reach only one of the two
cities; history 33 one-only to 12 both). But those that reach both cities reach them together. This is the texture
of the station lists a player reads, since most playlist entries are records like these. So it matters for
believability more than for any share metric.
- **Likely cause (untested).** The spread rules and relive act only on connected records, and the records that reach
  both cities are mostly the ones exposed in both at launch. A flop or a capped record reaches a second city
  through national awareness, which has no local clock.
- **First step: measure.** In `ufxal025`, split peak 41-100 records that reach both cities by connected / capped and
  by launch-exposed / late, using the conversion-step detector from §9u's diagnosis. Fix #2 would likely open this
  up as well.

### 4. Radio is not the carrier yet (§9s step 4)
The neighbour spread is a sales-rank rule, not a station decision. The replay showed that a station-add chain (a
region adds records that rank in an adjacent market's top sellers, 1-3 adds a week) gives the same numbers as the
rank rule, because add capacity never binds (a region's Top-40 stations take ~7 new records a week). So this is a
presentation feature: the player should see a station add a record *because* it is hot next door, and the exposure
should follow that add.
- **Lead.** Let the 5e floor stations add a neighbour-market seller (the trade-report pick), and make the region's
  exposure the week of that add instead of the rank rule. Keep the rank rule as the fallback for regions with no
  Top-40 reporter.
- **Low numerical risk.** Byte-compare with the rule off, and expect stagger within noise of `ufxal025`.

### 5. The remaining Big-5 cost, and which arm to carry
`ufxal025` matches the stagger at Big-5 Top-10 +1.3 (SE 0.8), the one borderline cost. `ufxa35l025` is neutral or
better on every cost, but its stagger is 11.8 and noisier across seeds (5-19). Indie live demand is down in both
(289-295M against 320M gated and 376M ungated), though bankruptcies are not up: the lost demand is in regions the
record never reaches.
- **Decide with the author.** LW 0.25 is a global change to the fit draw. It shrinks the launch-awareness edge for
  every label, which is part of the Big-5 entry edge (memory: Big-5 entry edge is campaign impact). It needs a decade
  A/B before it becomes a default, and the decade A/B belongs at the end of the audit.
- Items 1 and 2 will move the Big-5 cost again, so do not tune it further until they land.

## Ruled out (do not repeat)
- **A later national bar** (chart top 20 / 10 / 5, units top 20 / 10, or no national rule at all). It never binds:
  the neighbour rule spreads a hit to every region first (§9u).
- **Tighter neighbour or radio chains as the stagger source.** They work only by holding sales: ~2.5M of indie
  demand per point of gap ≥ 6 (§9u).
- **A generic late-region ramp** (log floor phased out over weeks since exposure). It gives the late region a record
  past its national peak, so the region never gets a peak, and it costs demand (§9u).
- **Replay v1's local clock as a forecast.** It replays the whole demand curve locally, which no game region does,
  and it overstates the stagger ~15× (§9u).
- **Raw Hesbacher in the replay.** The game's chart exposure is Hesbacher^0.62 with a top-10 plateau (4.4×, not 21×).
  Use `--expo 0.62`, the default (§9u).

## How to measure
- **Stagger:** `SimTools/analyze_regional_stagger.py SEEDS TAG [--source region]`. City = LA vs Minneapolis Top40 +
  FullService first appearance. Region = westcoast vs greatplains cleared-unit ranks. Indie top 10 = non-Big-5 peak
  ≤ 10, debut weeks 7-46. The history counterpart is `analyze_regional_break_history.py`. For per-seed values read
  column `gapGE6` of the "top 10, indie" row: n is ~45-55 per seed, so one seed swings ±8.
- **Headline:** `SimTools/score_big5_handoff.py SEEDS ufw085 TAG...` (paired Big-5 Top-10, offset, unit share,
  airplay, top-10 artists against the first two tags). **Bankruptcies:** `SimTools/score_bankruptcies.py SEEDS
  TAG...` (reads `SimLogs/regprobe/*-console.log`).
- **Replay v2 (forecast before any seeds):** `SimTools/replay_regional_relive.py SEEDS --clock local --nat chart --p0 B
  --demote D --demote_indie Di` replays the built relive + awareness rule on the floor arm's demand. Map LW to the
  demotes as in its docstring (LW 0.25 = 0.28 / 0.073). Replay → game offsets, measured on five arms: stagger × 0.85,
  Big-5 units +1.5, Big-5 Top-10 +0.8, indie demand × ~0.91, and city gap ≥ 6 ≈ 1.12 × regional. `--clock national`
  is the §9t game.
- **Runs:** `SimTools/run_regional_fit_screen.sh LW "seed tag flags..." ...` (52 weeks, ~8 min each, 3 in
  parallel; profile in `regional_fit_profile_flags.txt`). Floor-arm flags: `--station-regional-support=standing
  --station-regional-support-rank-noise=0.5 --station-regional-support-unsold-floor
  --station-regional-support-panel-weight=0.085`. The candidate adds `--regional-fit-exposure
  --regional-fit-exposure-relive --regional-fit-exposure-relive-awareness [--regional-fit-exposure-launch-base=0.35]`
  at LW 0.25.
- **Before every screen:** `dotnet build`, then check the first finished seed for exceptions and the
  `REGIONAL_FIT_EXPOSURE` counters in the console log (≈ 4,000 launches; held ≈ a third at base 0.3) before the rest
  run. The console's `REGIONAL_FIT_EXPOSURE on` line echoes base, relive and awareness, but not LW: check the process
  command line for that.
