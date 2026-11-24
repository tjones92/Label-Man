# Major Top 10 share: handoff to Fable (2026-11-11)

## Goal

The Big-5 (Columbia, RCA Victor, Capitol, Decca, Mercury) hold far too much of the Hot 100 Top 10 under the reference flag bundle.

| Big-5 Top 10 slot share | 1960 | 1961 | 1962 |
|---|---|---|---|
| History | ~35 | 20.6 | 21.2 |
| Current bundle, seeds 2002 + 1001 mean (major-scale-v1 sg) | 37.0 | 52.9 | 45.4 |

1960 is roughly on history. The excess builds in 1961–62. History also has the Big-5 releasing about 18% of singles; the model's 16% is close, so the problem is how well each Big-5 single converts, not how many they release.

**Scoring rule:** Big-5 = exactly `label_0001`–`label_0005`. Never compare label IDs as strings, because procedural `gen_…` IDs sort below `label_`.

## Reference bundle (adopted)

`SimLogs/writing-calibration/major-scale-bundle-request.json` (186 flags):
- the adopted major-premium bundle,
- `--launch-caps-major-only`,
- `--major-scale-gate=0.75`.

Controls already run:
- `major-scale-v1-{2002,1001}-sg` (156 weeks)
- `indie-access-v2-2002-base` (104 weeks, traced)

Rerun controls only when the code changes.

## What is established

1. **Production is a minor factor.**
   - Record production = 0.4 × label production quality, plus 0.3 × artist studio performance, plus 0.2 × home-region studio, plus noise (`CompetitorManager.GenerateRecordFromArtist`).
   - The tier ranges give the whole observed Big-5/Independent production gap (0.744 vs 0.666).
   - At matched record quality the tiers still split 4–10×. For quality ≥ 0.74, Top 10 weeks per release: Big-5 0.98, Independent 0.22.
   - Quality composition is about 20% of the log gap. Full production parity would be worth about 2–3 points of Big-5 share.
   - Script: `SimTools/analyze_tier_quality_conversion.py`.

2. **Cameo-Parkway is fixed by the scale gate (adopted).**
   - It was the only label clearing the MidTier→Major gate (roster 25 + 16 charting records). It promoted at week 61 in every arm.
   - `--major-scale-gate=R` (`Systems/MajorScaleGate.cs`) requires recent gross ≥ R × the smallest active Major's.
   - Cameo sits at 0.17–0.59 of that bar.
   - Effect: 1962 Major Top 10 share fell from 54.8 to 47.9. Big-5 share unchanged. Cameo's 1962 Top 10 share fell from 8.4% to 3.5%, which may be low against its real 1962 hits.
   - Check: `--major-scale-gate-check`.

3. **Launch stock is mostly follower advance orders, not distribution.**
   - This is the key new finding, from `--launch-stock-trace`.
   - The default args include `--launch-stock-share=0.1` (`Systems/SimulationArguments.cs:24`), so every AI single's first run is 10% of the drawn stock.
   - On top of that, `MediaAtmosphereService.StockAdvanceOrders` adds dealers' advance orders for acts with follower books.
   - Seed 2002, 1960–61 singles:

| Tier | Base run | Advance orders | Share with a follower book | Top 10 weeks per release |
|---|---|---|---|---|
| Big-5 | 5,543 | 25,196 | 80% | 0.486 |
| MidTier | 6,009 | 23,484 | 66% | 0.340 |
| Independent | 3,035 | 1,988 | 37% | 0.053 |
| Boutique | 2,463 | 1,083 | 34% | 0.030 |

   - Distribution (coverage × depth) makes only a 1.8× base-run gap. Follower books make a 12× gap in advance stock.
   - Distance reach factor is about 1.0 everywhere; it is not a factor.
   - Script: `SimTools/analyze_launch_stock_advance_orders.py <run>`, which reads `<run>.log` and `<run>-records.csv`.

4. **A residual without follower books.**
   - Big-5 singles with no follower book still convert at 0.22 Top 10 weeks per release, against 0.047 for Independents (4.7×).
   - Likely carried by campaign impact (0.55 vs 0.35) through launch awareness and radio.
   - Not yet decomposed.

5. **The distributor-access probe is inconclusive and not adopted.**
   - Switch: `--indie-distributor-access=A` (`Systems/IndieDistributorAccess.cs`). For non-Major AI labels it raises uncovered-region launch access from 0.18 to A.
   - Because of the 0.1 launch share, it adds only about 10% of its nominal stock (Independent base run 3.0k → 3.9k).
   - Big-5 Top 10 share, A = 0.6:
     - Seed 2002: 1960 47.2 → 39.8, 1961 55.8 → 47.1.
     - Seed 1001: 1960 26.8 → 30.2, 1961 50.0 → 46.9.
   - Seed 1001 does not replicate the seed-2002 drop. Treat it as reshuffle noise, since any stock change reshuffles the RNG stream.
   - Script: `SimTools/analyze_tier_top10_share.py <ctrl> <arm>`.

## Suggested next steps

1. **Follower books on Big-5 acts.** Why do 80% of Big-5 singles carry a follower book, and why are those books about 25k units? Read the memories `follower-is-die-hard-plus-casual`, `follower-single-copies-clear-at-ten-percent`, `veteran-excess-is-album-heat-via-follower-lps` and `major-top10-doubled-under-reference-flags`. The bundle-era jump in Major share (Nov 1 → Nov 7) coincides with the follower/fanbase flags. A cheap screen is an arm where advance orders are shadow-only (`MediaFollowerDemand.AdvanceStockCausal` off); check how it is switched. Measure Big-5 Top 10 share and whether the advance stock actually sells or just sits on shelves.
2. **Decompose the no-book residual** (Big-5 0.22 vs Independent 0.047): campaign impact → launch awareness and radio, and station adds. `single-launch-attribution.csv` has `launchCampaignImpact`, `initialAwareness` and `initialStock` per single.
3. If distribution still matters after (1), screen the access probe with `--launch-stock-share` in mind, and use two seeds, not one.
4. Decide whether Cameo's 1962 share (3.5%) is too low. If so, look at MidTier roster/slate limits, not the gate.

## Method notes

- `SimTools/run_indie_access_screen.ps1 -Seed N -Arms base,ia -RunPrefix X [-Access A] [-Trace]` runs the reference bundle (104 weeks by default). It refuses existing run names. Run outputs go to `SimLogs/` and are never committed.
- `--launch-stock-trace` prints one `LAUNCH_STOCK_TRACE` line per AI launch to `SimLogs/writing-calibration/<run>.log`. For each region it gives coverage flags (covered/strong/home), reach factor, drawn stock and final stock. It is observe-only: the traced records.csv is byte-identical to the untraced run.
- A single seed moves ±3–4 points of share per year. Confirm anything with two seeds (2002, 1001).
- Python: `$LOCALAPPDATA/Programs/Python/Python314/python.exe`. The bare `python` command is the Store stub.
- Build with `dotnet build "Label Man.csproj" -c Debug` before headless runs, and never rebuild while a run is starting.

## Uncommitted code (this session and the previous one)

- **New:**
  - `Systems/MajorScaleGate.cs`
  - `Systems/IndieDistributorAccess.cs`
  - `Systems/LaunchStockTrace.cs`
  - `SimTools/MajorScaleGateChecks.cs`
  - `SimTools/run_major_scale_gate_screen.ps1`
  - `SimTools/run_indie_access_screen.ps1`
  - `SimTools/analyze_tier_*.py`
  - `SimTools/analyze_launch_stock_*.py`
- **Hooks in existing files:**
  - `LabelLifecycleManager.TryPromoteLabel`: scale gate.
  - `ChartSimulator.CalculateInitialRegionalStock`: access.
  - `ChartManager` and `CompetitorManager`: trace emits.
  - `ChartManager` configure block: all three switches.
  - `SaveLoadRoundTripRunner`: `--major-scale-gate-check`.
- **Still uncommitted from the previous session:** `LaunchCapScope`, `EarnedCampaignImpact`, `LaunchStockSpread` and their checks.

All switches are off by default.

## Update 2026-11-11 (evening): advance-stock screen `advance-stock-v1-{2002,1001}-nas`

Arm = reference bundle + `--follower-demand-no-advance-stock` (104 weeks, traced; `run_indie_access_screen.ps1 -Arms nas`). Controls: `indie-access-v2-2002-base`, `major-scale-v1-1001-sg`.

| Big-5 Top 10 share | 1960 | 1961 |
|---|---|---|
| Seed 2002 control → nas | 47.2 → 46.6 | 55.8 → 50.6 |
| Seed 1001 control → nas | 26.8 → 26.8 | 50.0 → 36.9 |
| Mean | 37.0 → 36.7 | 52.9 → 43.8 |

- Launch stock is not what makes stars chart. Seed 2002, Big-5 singles with launch awareness ≥ 0.8: stock 130k → 6.7k, lifetime units 323k → 320k, Top 10 weeks per release 1.79 → 1.86. Restocking covers the demand.
- Big-5 Top 10 weeks per release overall: 0.486 → 0.469 (2002), 0.403 → 0.346 (1001). The 1961 drop is the same sign in both seeds but uneven (−5, −13); mechanism not isolated. Not adopted yet.
- The Top 10 belongs to ~40 big-book acts (seed 2002 control): 50k+ book singles take 46–47% of Top 10 weeks. 1960 Big-5 17 acts / 80 singles / 124 weeks, MidTier 19 / 102 / 142. 1961 Big-5 23 / 116 / 158, MidTier 16 / 58 / 41. The 1961 Big-5 rise is a swap with MidTier stars plus 8 home-grown Big-5 acts crossing 50k.
- Fame-matched tiers still split: awareness ≥ 0.8, Big-5 1.8 Top 10 weeks per release vs MidTier 0.9–1.1 (quality 0.77 vs 0.72). Awareness < 0.2 with equal ~5k launch stock: Big-5 sells 37–57k lifetime units vs 7.5–8k for non-Major, non-MidTier labels. That post-launch gap is the residual in step 2.
- Scratch scripts for the book/awareness tables are not in the repo; logic is tier × book size (trace) and tier × `initialAwareness` band (single-launch-attribution).

## Update 2026-11-11 (late): the no-book residual is regional coverage after chart entry

Unknown singles = launch awareness < 0.2, launched week 1–92, controls `indie-access-v2-2002-base` and `major-scale-v1-1001-sg`. Lifetime units from `release-outcomes.csv` (`records.csv` holds charted weeks only, so the earlier "37–57k vs 7.5k units" figure was on-chart units).

- Top 10 weeks per unknown release, both seeds: Big-5 0.169, MidTier 0.191, Independent 0.043, Small/Boutique 0.014.
- Quality mix: Independents at the Big-5 quality mix would reach 0.068 (about a third of the log gap). No unknown single below quality 0.68 reaches the Top 10 in any tier.
- Hot 100 entry at matched quality is close at the top: q 0.74–0.80 Big-5 18% vs Independent 15%; q ≥ 0.80 23% vs 20%. It splits in the middle band (0.68–0.74: 24% vs 9%).
- After entry, covered regions decide the outcome, not tier. Charted unknown singles, both seeds:

| Covered regions | n | Median peak | Reached Top 40 | Top 10 weeks per single |
|---|---|---|---|---|
| 1–3 | 117 | 82 | 11% | 0.09 |
| 4–5 | 173 | 51 | 38% | 0.30 |
| 6 | 132 | 41 | 50% | 0.58 |
| 7 | 235 | 37 | 51% | 0.96 |

  At 7 regions the tiers are equal: Big-5 0.94, Independent 1.01, MidTier 1.09, Small/Boutique 0.83. Every Big-5 single has 7. Peak radio heat is equal across tiers (0.25–0.31).
- At first chart entry non-Major labels cover 3–4 of 7 regions; 27–51% hold a distribution deal, granting 1–2 regions (`first-chart-events.csv`).
- An uncovered region is penalised four ways: launch awareness and radio (`ChartSimulator.GetRegionalLaunchFactor`, 0.12 + 0.18·reach vs 0.55 + 0.45·reach), launch stock access 0.18, restock service 0.18 + 0.25·strength vs 0.70 + 0.80·strength, and shelf capacity 0.20 vs 0.55 + 0.65·strength (`ChartManager` ~2034–2067). `--indie-distributor-access` touched only launch stock, so it did not test coverage.
- Rough size: if charted unknowns below 7 regions converted like 7-region ones, about 267 Top 10 weeks over two seeds × two years would move (≈13% of slots), roughly −6 points of Big-5 share. Upper bound: coverage correlates with label size.
- Next: a probe that gives a non-Major record that has charted covered-grade restock service and shelf capacity in uncovered regions (earned national access), two seeds.

## Update 2026-11-11 (night): earned-coverage screen `earned-coverage-v1-{2002,1001}-ec`

New switch `--earned-indie-coverage=S` (+ `--earned-indie-coverage-position=P`, default 100), `Systems/EarnedIndieCoverage.cs`, hooks in `ChartManager.RestockHotRecords`: a non-Major AI record at national position 1..P gets its uncovered regions' restock service and shelf capacity moved S of the way to covered. Restock only; no RNG. Off is byte-identical (27-week records.csv vs `indie-access-v2-2002-base`). Arm: `run_indie_access_screen.ps1 -Arms ec [-Coverage S]`. No check runner yet.

| Big-5 share, S = 1 | 1960 Top 10 | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 |
|---|---|---|---|---|
| Seed 2002 control → ec | 47.2 → 35.5 | 55.8 → 47.1 | 45.0 → 40.5 | 40.4 → 35.8 |
| Seed 1001 control → ec | 26.8 → 24.0 | 50.0 → 47.3 | 44.2 → 37.2 | 38.2 → 31.6 |
| Mean Top 10 | 37.0 → 29.8 | 52.9 → 47.2 | | |

- Same sign in both seeds and both years; chart units flat (121–122M a year). Independent Top 10 share: 2002 14.9/15.0 → 26.4/20.4, 1001 21.7/15.8 → 32.8/18.7.
- Mechanism confirmed in part. Charted unknown singles, Top 10 weeks per single by covered regions (control → ec): 1–3 0.09 → 0.08, 4–5 0.30 → 0.49, 6 0.58 → 0.92, 7 0.96 → 0.92. Records at 1–3 regions do not respond, so their limit is demand in uncovered regions (launch awareness/radio factor 0.12 + 0.18·reach), not supply.
- Worth about 6 points at full strength, less than the 1961 excess (47 vs 20.6), and it takes 1960 below history (~35). Not adopted. The 1961 excess still sits with the big-book acts.

## Update 2026-11-12: the 1961 build is release volume; the per-single premium is constant

Both controls, singles launched 1960–61. Script: `SimTools/analyze_big5_premium_decomposition.py <run> <run>` (books from `following.csv` launch rows, so untraced runs work).

**1. 1960 is on history only because the Big-5 release half as many singles.**

| Seed, year | Big-5 singles | Share of launches | Top 10 weeks per single, Big-5 vs field | Premium | Share of Top 10 weeks |
|---|---|---|---|---|---|
| 2002, 1960 | 439 | 10.3% | 0.615 vs 0.081 | 7.6× | 46.6% |
| 2002, 1961 | 627 | 16.3% | 0.396 vs 0.055 | 7.2× | 58.2% |
| 1001, 1960 | 316 | 8.0% | 0.522 vs 0.111 | 4.7× | 29.0% |
| 1001, 1961 | 643 | 16.0% | 0.400 vs 0.079 | 5.1× | 49.0% |

Big-5 launches per quarter ramp 51–67 → 59–91 → 82–133 → 115–139, then hold at ~160 (the slate cap, 5 × 10 a month). Releasing Big-5 acts grow 154 → 221 and 125 → 215 at a flat 2.5–3.0 singles per act. History has the Big-5 at ~18% of singles and 20.6% of the Top 10: a premium near 1.2×, not 5–7×. The target is the per-single premium in both years; 1960 is not a calibrated year.

**2. What the premium is made of (1961, seeds pooled; Big-5 53.2% of launched-single Top 10 weeks on 16.1% of launches).**

- Mix, about 26 points: Big-5 singles sit in the high-awareness, high-quality cells. With the field's quality × awareness mix and its own conversion the Big-5 would hold 27.4%. Awareness mix alone 34.5%, quality mix alone 41.2% (they overlap). Big-5 are 16% of launches but 45–65% of launches at awareness ≥ 0.4 and quality ≥ 0.74.
- Within-cell conversion, about 11 points: at the field's conversion in the same cell the Big-5 would hold 42.6%. The earned-coverage arm (−6) is most of this.
- Label-given launch awareness is small: label reputation (+0.04) and marketing (+0.065) give Big-5 singles +0.107 awareness over non-MidTier labels; removing it is worth about 3 points (53.2 → 49.9).
- Act-given fame is the rest. Mean act momentum at launch: Big-5 0.10–0.14, MidTier 0.11–0.15, other labels 0.005–0.03. Only 2–3% of other labels' launches reach awareness 0.4, against 30–38% at Big-5 and MidTier.

**3. Followers compound at the Big-5.** Follower stock Jan 1960 → Dec 1961: Big-5 3.7M → 14.6M (2002), 2.0M → 12.0M (1001); MidTier 5.2M → 9.3M, 7.7M → 13.0M. Joins are 87–94% from buyers at an equal ~0.09 per unit sold in every tier (`MediaFollowing.CBuy` 0.30), so followers track unit share (Big-5 singles units 39 → 48%, 25 → 46%). Acts with no 1960 Top 40: Big-5 followers ×2.2–3.1, MidTier ×1.2–1.3, others ×1.0. Books are a marker here, not the cause: the nas arm removed the stock without moving star conversion.

**4. Book-holding acts (10k+).** Big-5 acts that held a book in 1960 keep it (24/27, 19/22) and their books grow 77k → 106k, 50k → 81k; MidTier books are flat (63k → 69k, 66k → 75k) and their Top 10 weeks fall 122 → 44, 174 → 117. Twelve to thirteen more Big-5 acts a year cross 10k from below. Tier moves are rare (0–2 acts).

**5. Side finding: stars disband at the first year-end.** Week 54 voluntary disbandment/retirement removes current hit acts in both tiers: seed 2002 five MidTier (books 66–328k, 61 Top 10 weeks in 1960) and three Big-5; seed 1001 four MidTier and two Big-5. Not tier-biased, but it removes the field's biggest non-Big-5 acts.

**Candidate levers, by estimated size.** (a) Fame mix: why non-MidTier labels' acts carry no momentum at launch, and roster growth at the Big-5 (154 → 221 releasing acts). (b) Coverage (built, −6). (c) Production parity (2–3). (d) Label-given awareness (3). (e) Week-54 disbandment of current hit acts.

## Update 2026-11-12 (later): momentum at small labels, and the `ncp` screen

Momentum is a chart-history score: new acts start at 0; +0.02 Top 40, +0.10 Top 10, +0.18 No. 1; −0.05 for a peak of 41–60, −0.10 for 61–100; the bundle adds −0.15 for a never-charted single (`--album-scale-career-milestones`, part b2) and a 52-week half-life.

- Smaller labels start with momentum acts (Q1 1960: 203 Independent, 189 Small/Boutique acts above zero) and lose it. Acts seeded ≥ 0.2, mean momentum at their 1st/2nd/3rd/4th single: Big-5 0.49/0.52/0.51/0.45, MidTier 0.42/0.39/0.36/0.37, Independent 0.30/0.16/0.06/0.04, Small/Boutique 0.30/0.15/0.02/0.00.
- At matched act momentum (0.1–0.4) and quality (0.70–0.78), Hot 100 entry is Big-5 68%, MidTier 64%, Independent 26%, Small/Boutique 21%. Launch awareness 0.56/0.59/0.40/0.34, launch stock 38k/38k/13k/5k, campaign impact 0.55/0.55/0.35/0.27, recognition 0.29/0.29/0.09/0.04. The gap at equal act fame is label-side.
- After a first in-run Top 10, follow-up singles: momentum 0.16/0.09/0.06/0.04, Top 10 weeks per single 0.97/0.47/0.16/0.12, follow-ups per year 5.3/3.1/3.3/2.2.

**Screen `momentum-penalty-v1-{2002,1001}-ncp`** (bundle + `--album-scale-career-milestones-part=b1`: no never-charted penalty; `run_indie_access_screen.ps1 -Arms ncp`).

| Big-5 Top 10 share | 1960 | 1961 | Premium 1960 / 1961 |
|---|---|---|---|
| Seed 2002 control → ncp | 47.2 → 39.8 | 55.8 → 59.0 | 7.6× / 7.2× → 6.4× / 7.2× |
| Seed 1001 control → ncp | 26.8 → 21.1 | 50.0 → 43.3 | 4.7× / 5.1× → 3.9× / 4.8× |
| Mean | 37.0 → 30.5 | 52.9 → 51.2 | |

- Momentum now survives at Independents (0.30/0.24/0.20/0.16 over the first four singles) but their singles still go uncharted 72–83% of the time at matched momentum and quality. Momentum drain is a symptom, not the cause; 1961 is unchanged. Not adopted.
- The binding gap is the label-side launch at equal act fame: campaign impact (marketing awareness and launch radio push), recognition, follower books and coverage.

## Update 2026-11-12 (evening): act-earned campaign impact screen `act-impact-v1-{2002,1001}-aci`

New switch `--act-earned-campaign-impact=C` (+ `--act-earned-campaign-momentum=M`, default 0.2), `Systems/ActEarnedCampaignImpact.cs`: a non-Major AI label's single gets campaign impact label + max(0, C − label) × min(1, act momentum at launch / M), fixed at launch, read at launch marketing awareness, launch radio push, weekly label push and commercial-fit push. Off is byte-identical (27-week records.csv). Per-record share is in memory only (not saved). Arm: `run_indie_access_screen.ps1 -Arms aci [-Impact C]`. No check runner.

| Big-5 Top 10 share, C = 0.55 | 1960 | 1961 | Premium 1960 / 1961 |
|---|---|---|---|
| Seed 2002 control → aci | 47.2 → 43.0 | 55.8 → 47.7 | 7.6× / 7.2× → 6.6× / 4.9× |
| Seed 1001 control → aci | 26.8 → 37.7 | 50.0 → 49.0 | 4.7× / 5.1× → 5.7× / 6.5× |
| Mean | 37.0 → 40.4 | 52.9 → 48.4 | |

- The seeds disagree; no reliable effect on share. Not adopted.
- It works mechanically for Independents only. Matched momentum 0.1–0.4 and quality 0.70–0.78: Independent campaign impact 0.35 → 0.52, Hot 100 entry 26% → 40%, Top 10 4% → 6%; Small/Boutique impact 0.27 → 0.51 with entry unchanged at 21%.
- Launch awareness barely moves (Independent 0.40 → 0.41, Big-5 0.53): the marketing term is BudgetToImpact(budget, tier) × impact × 0.35 and small labels spend about a third of the budget ($109 vs $334 a single), so awareness is budget-bound, not impact-bound. Recognition (0.29 vs 0.09), launch stock (36k vs 12k / 4k) and coverage are untouched by this switch.

**Where this leaves the premium.** Four single-lever screens, mean Big-5 Top 10 share 1960 / 1961 against control 37.0 / 52.9: no advance stock 36.7 / 43.8; earned coverage 29.8 / 47.2; no never-charted penalty 30.5 / 51.2; act-earned impact 40.4 / 48.4. None moves 1961 more than 9 points, and two-seed spread is ±5. The premium is spread across coverage, marketing budget, recognition, follower stock and production; no single switch carries it.

## Update 2026-11-11 (session 3): combined arm `combo-v1-{2002,1001}-ec_aci_nas`, and the label-footprint audit

Arm = bundle + `--earned-indie-coverage=1` + `--act-earned-campaign-impact=0.55` + `--follower-demand-no-advance-stock` (104 weeks). `run_indie_access_screen.ps1` now takes arms joined by `_` (`-Arms ec_aci_nas`) and `-Extra` raw flags. New scorer: `SimTools/analyze_big5_share_ladder.py <run> ...` (Big-5 share at Top 10 / Top 40 / Hot 100, premium, Hot 100 entry rate of Independent + Boutique + Small launches, chart units).

| Big-5 slot share | 1960 Top 10 | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 | 1961 premium | 1961 small-label Hot 100 entry |
|---|---|---|---|---|---|---|
| Seed 2002 control → combined | 47.2 → 33.4 | 55.8 → 47.7 | 45.0 → 39.9 | 40.4 → 36.9 | 7.2× → 5.5× | 6.6% → 6.3% |
| Seed 1001 control → combined | 26.8 → 38.1 | 50.0 → 53.8 | 44.2 → 41.4 | 38.2 → 37.9 | 5.6× → 6.7× | 6.6% → 6.6% |
| Mean Top 10 | 37.0 → 35.8 | 52.9 → 50.8 | | | | |

- The three levers do not add up. The combined arm is no better than earned coverage alone (1961 Hot 100 share 35.8 / 31.6 with `ec` only), and seed 1001 goes up at the Top 10. Not adopted.
- Small-label Hot 100 entry does not move in any arm (6.3–7.4% in 1961 against 28–34% for the Big-5). Chart units are fixed at 121–122M a year, so the chart is a zero-sum purse and the field's entry rate is the thing to move.

**Audit of label-side terms (what a label's size buys at each seam).**

| Seam | Code | Major vs Independent | Status in the bundle |
|---|---|---|---|
| Weekly radio push | `ChartSimulator.UpdateLabelPush` | campaign impact × 0.4 into radio target | compressed to 10% of its spread (`--radio-push-spread=0.1`): near label-blind |
| Launch campaign impact | `LaunchPushSaturation` | 0.55 vs 0.35 | Majors capped at 0.55 |
| **Purchase rate** | `ChartSimulator.CalculateLiveLabelDemandScale` (line 744) | 0.45 + 0.55·distribution + 0.35·reach, clamp 0.55–1.20: Major 1.12–1.20, Independent 0.67–0.97, Small 0.55–0.79 | **untouched by any screen** |
| Regional arrival | `RegionalAudience.HoldUntilArrival` / `TryArrive` | a single has zero awareness, radio and stock in every region its label does not ship to until it reaches the national Top 40 (or a neighbour test fires); Majors ship to all 7 | untouched |
| Launch awareness and radio per region | `ChartSimulator.GetRegionalLaunchFactor` | covered 0.55 + 0.45·reach (Major ~0.95, Independent ~0.70), strong 1.35, uncovered 0.12 + 0.18·reach | untouched |
| Restock service and shelf | `ChartManager.RestockHotRecords` | uncovered 0.18 + 0.25·strength, shelf 0.20 | `--earned-indie-coverage` (not adopted) |
| Record production | `AILabelFactory.ApplyTierStats` | founding draw 0.72–0.98 vs 0.40–0.91; 40% of record production, 30% of quality, quality enters demand at the 4th power | untouched |
| Singles ration | `SinglesRationExponent` (`--singles-ration-exponent=1.35`) | units ∝ demand^1.35 on a 6–10× oversubscribed purse: every demand-side edge above is raised to the 1.35 power | adopted for No. 1 sales |

The purchase-rate term counts distribution a second time: it already bounds launch stock, restock, shelf and arrival, and here it also makes a listener who knows the record likelier to buy it. The pre-V2 path did the opposite (Major × 0.60). Purchase rate (1.46×) and production (≈1.14× through quality⁴) compound to about 2× units at equal awareness and coverage once the ration exponent is applied.

**New switches (`Systems/LabelFootprintParity.cs`, all off by default, no RNG; off is byte-identical for 27 weeks, `parity-v1-off-2002-base` vs `indie-access-v2-2002-base`).**
- `--label-demand-parity=P` (arm `dp`): moves every label's purchase-rate scale P of the way to 1.
- `--regional-arrival-chart-depth=N` (arm `ad`, default 100 in the script): a held region takes a single inside the national top N instead of the top 40.
- `--major-craft-parity` (arm `cp`): a Major's founding scouting and production draws use the MidTier envelope. Changes the world from week 0.
- `--covered-launch-parity=F` (arm `cl`): a non-Major's covered-region launch factor (0.55 + 0.45·reach) moves F of the way to 1. Built, not yet screened.
- `--regional-fit-exposure-national-rank=N` (arm `fn`): a connected single is fit-exposed everywhere inside the national top N instead of the top 40.
- Arm `fx` = the existing `--regional-fit-exposure-launch-base=1`: every region fit-exposed at launch for every label.

### The hit lottery is tier-fair; realization is not

`SingleCommercialFit` (profile: sigma 2.5, hit share 0.15, launch weight 1.25, floor 0.12) draws one national response per single. A single connects with probability 1 − Φ(1.036 − 1.25 × (launch awareness − 0.2)): 15% at awareness 0.2, 22% at 0.4, 32% at 0.65. A single that does not connect is cut to 0.12–0.6 of its demand and never reaches the Top 40 (0 of the sampled flops in `single-demand-stages.csv`; every Top 10 week belongs to a connected single). Script: `SimTools/analyze_fit_lottery_by_tier.py <run> ...` (`--sample` for the captured fit).

Control runs, singles launched weeks 1–92:

| Tier | Launch share | Mean connect chance | Share of expected connecting singles | Top 40 singles per expected connect (2002 / 1001) | Share of Top 10 weeks (2002 / 1001) |
|---|---|---|---|---|---|
| Big-5 | 12–13% | 0.21–0.22 | 15–17% | 0.95 / 0.95 | 51 / 38 |
| MidTier | 10–11% | 0.23–0.24 | 14–16% | 0.73 / 0.79 | 28 / 34 |
| Independent | 34–35% | 0.15 | 31–32% | 0.25 / 0.29 | 16 / 18 |
| Small + Boutique | 42% | 0.14 | 36% | 0.15 / 0.15 | 6 / 6 |

- If every connecting single were realized alike, the Big-5 would hold 15–17% of the Top 40 and Top 10, which is the historical figure. The awareness tilt on the lottery is small (0.22 vs 0.15).
- The premium is realization: nearly every connecting Big-5 single reaches the Top 40; 70–85% of connecting small-label singles do not. About 480 singles reach the Top 40 against about 1,180 expected connects, so parity would be near 0.40 for every tier.
- What stands between a connected small-label single and the Top 40: fit exposure at launch only in the home regions plus each other region with chance 0.3 + 0.7 × reach (`RegionalFitExposure.Launch`; Major 0.83–1.0, Independent 0.43–0.65, Small 0.35–0.51), spreading only through a local top 40 or the national top 40; the arrival hold; the purchase-rate scale; uncovered restock.
- Follow-ups after an act's first in-run Top 10 (`SimTools/analyze_hit_followups_by_tier.py`): Hot 100 entry Big-5 71–72%, MidTier 61–71%, Independent 28–35%, Small/Boutique 25–26%. Independent follow-ups ship 26k copies at launch awareness 0.40 and 72% never chart, selling a median 2.3k. Top 10 weeks per hit act: Big-5 6.1–8.0, Independent 0.4–0.8.

### Screens `parity-v1-{2002,1001}-<arm>` (104 weeks; scorer `analyze_big5_share_ladder.py`)

| Arm | 1960 Top 10 | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 | 1961 premium | Top 40 per expected connect: Big-5 / Independent / Small+Boutique |
|---|---|---|---|---|---|---|
| Control | 47.2 / 26.8 | 55.8 / 50.0 | 45.0 / 44.2 | 40.4 / 38.2 | 7.2× / 5.6× | 0.95 / 0.25–0.29 / 0.15 |
| `ec_aci_nas` | 33.4 / 38.1 | 47.7 / 53.8 | 39.9 / 41.4 | 36.9 / 37.9 | 5.5× / 6.7× | |
| `dp` | 26.6 / 23.0 | 45.8 / 41.2 | 34.8 / 32.6 | 30.5 / 29.5 | 4.3× / 5.2× | 0.70 / 0.32 / 0.23 (2002) |
| `ad` | 39.8 / 24.3 | 56.7 / 34.4 | 44.4 / 31.6 | 36.9 / 29.1 | 7.4× / 3.1× | 0.83 / 0.36 / 0.15 (1001) |
| `ec_aci_nas_dp` | 24.5 / 20.6 | 36.9 / 32.7 | 29.9 / 27.5 | 28.2 / 25.9 | 4.2× / 2.8× | |
| `ec_dp_ad_cp` | 27.0 / 20.0 | 35.0 / 25.6 | 27.4 / 24.0 | 26.2 / 23.1 | 3.4× / 2.2× | 0.65–0.73 / 0.33–0.34 / 0.25 |
| History | ~35 | 20.6 | | | ~1.2× | |

(Each cell is seed 2002 / seed 1001.)

- `dp` is the first single lever with the same sign on both seeds at every rung and in both years (1961 Hot 100 share −9.9 / −8.7). With it, the earlier levers add: `ec_aci_nas_dp` is 18 points under control at the 1961 Top 10.
- `ad` alone is uneven at the Top 10 (+1 / −16); it lowers the Hot 100 share on both seeds (−3.5 / −9.1).
- `ec_dp_ad_cp` is the lowest arm so far: 1961 Top 10 mean 30.3 (control 52.9), Hot 100 24.7 (39.3). 1960 falls to 23.5, under the ~35 estimate for history; see the earlier note that 1960 is not a calibrated year.
- Chart units stay at 119–122M a year in every arm. Side effects of `dp`, seed 2002: Big-5 singles units 48M → 28M (1960) and 62M → 48M (1961); Big-5 gross −7% and pretax −8 to −10% (LPs carry them); Independent singles units +30–47%; active label counts unchanged in 1960, Independents 217 → 235 in 1961.
- The Top 10 is star-driven: the top five acts hold about 20% of slot-weeks and one Big-5 act held 9% of 1961 in `ec_aci_nas` seed 1001. Judge levers on Top 40 / Hot 100 share and realization, not on the Top 10 alone.

### Opening fit exposure, and leave-one-out from the full set (`parity-v1-*`, same session)

Full set = `ec_dp_ad_cp_fx`: earned coverage 1 + demand parity 1 + arrival depth 100 + Major craft parity + fit-exposure launch base 1. Each cell is seed 2002 / seed 1001; (mean).

| Arm | 1960 Top 10 | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 | 1961 premium |
|---|---|---|---|---|---|
| Control | 47.2 / 26.8 (37.0) | 55.8 / 50.0 (52.9) | 45.0 / 44.2 | 40.4 / 38.2 | 7.2× / 5.6× |
| `ec_dp_ad_cp` (no fit exposure change) | 27.0 / 20.0 | 35.0 / 25.6 (30.3) | 27.4 / 24.0 | 26.2 / 23.1 | 3.4× / 2.2× |
| `ec_dp_ad_cp_fn` (fit exposure inside Hot 100) | 29.2 / 10.8 | 32.3 / 26.0 (29.2) | 26.5 / 22.8 | 28.9 / 21.0 | 3.3× / 2.3× |
| **`ec_dp_ad_cp_fx` (full set)** | 24.9 / 12.1 (18.5) | **28.1 / 21.7 (24.9)** | 25.8 / 21.8 | 28.2 / 22.2 | 2.8× / 1.9× |
| Full minus `cp` | 31.7 / 18.7 | 37.5 / 24.0 (30.8) | 30.6 / 21.5 | 29.2 / 20.9 | 3.9× / 1.8× |
| Full minus `ad` | 23.4 / 12.6 | 34.2 / 21.5 (27.9) | 30.0 / 21.9 | 28.2 / 22.9 | 4.1× / 1.8× |
| Full minus `ec` | 29.4 / 22.1 | 29.4 / 29.4 (29.4) | 26.5 / 24.5 | 25.4 / 24.1 | 2.1× / 2.7× |
| **Full minus `dp`** | 34.5 / 36.0 | **47.5 / 39.6 (43.6)** | 40.0 / 34.9 | 37.5 / 32.1 | 4.5× / 3.3× |
| History | ~35 | 20.6 | | | ~1.2× |

- **The purchase-rate scale is the carrier.** Removing `dp` from the full set gives back 19 points of 1961 Top 10 share, 14 of Top 40 and 10 of Hot 100, on both seeds. Removing any other piece moves the Top 10 by 3–6 points and the Top 40 / Hot 100 by 0–3, which is inside two-seed noise.
- The full set reaches 24.9 at the 1961 Top 10 (history 20.6); seed 1001 sits at 21–24 in four of the five arms that keep `dp`. Realization under the full set (Top 40 singles per expected connect): Big-5 0.71 / 0.76, MidTier 0.70 / 0.73, Independent 0.42 / 0.38, Small + Boutique 0.23 / 0.27.
- 1960 falls to 18.5, well under the ~35 estimate. The Big-5 launch only 7–9% of 1960 singles (history ~18%), so their 1960 share was being held up by the per-single premium; with the premium cut, the 1960 shortfall is release volume and opening rosters, not conversion.
- Genre mix of the 1961 Top 10 under the full set, seed 2002: teen pop 27%, rock and roll 18%, traditional pop 14% (control 26%), R&B 13%, doo-wop 10%, soul 6%, country 5% (control 10%).
- What is left of the Big-5 share under the full set (1961, share of all Top 10 slots): home-grown acts 10.0 / 11.5, adult standard-bearers 10.2 / 5.4, other opening stars 7.9 / 4.8. Adult genres (traditional pop, easy listening, country, folk, jazz) hold 23–25% of slots and the Big-5 hold 11–14 points of that.
- `fx` removes the regional stagger at launch for every label, which the §9s exposure work was built to create; `fn` keeps it and is 4 points weaker at the Top 10. Check that the regional guards (same-city / cross-city overlap, local lead) survive before adopting `fx`.
- Check runner: `--label-footprint-parity-check` on `SimTools/SaveLoadRoundTripRunner.tscn` passes (`SimTools/LabelFootprintParityChecks.cs`). Off is byte-identical for 27 weeks on both builds (`parity-v1-off`, `parity-v2-off`).
- The control runs `indie-access-v2-2002-base`, `major-scale-v1-{2002,1001}-sg` and the `ec` / `nas` / `ncp` / `aci` single-lever runs were deleted on 2026-11-11 to free disk; their numbers are in the tables above. A 104-week run writes about 0.8 GB; pass `-Extra --lean-probe` (about 0.12 GB, drops `single-demand-stages` and five other dumps the scorers here do not read).

**Minimal set and covered-launch parity (lean runs).**

| Arm | 1960 Top 10 | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 | 1961 premium |
|---|---|---|---|---|---|
| `dp_fx` (demand parity + open fit exposure only) | 23.6 / 22.6 | 39.4 / 38.1 (38.8) | 32.2 / 31.3 | 30.6 / 28.8 | 3.8× / 3.4× |
| Full set + `cl` | 30.6 / 15.7 | 37.1 / 23.1 (30.1) | 32.7 / 20.0 | 31.5 / 20.8 | 3.4× / 1.9× |

- `dp_fx` is about `dp` alone less 5 points. The three small pieces are not individually resolvable but they add: leave-one-out costs of `cp` 5.9, `ad` 3.0 and `ec` 4.5 sum to 13.4, and the full set is 13.9 under `dp_fx`. So the full set is `dp` (about 10–19 points depending on what else is on) plus about 14 from coverage, arrival and craft together plus about 5 from `fx`.
- `cl` adds nothing (seed 2002 +9, seed 1001 +1.4 at the Top 10; realization unchanged at Independent 0.43 / 0.40). Not part of the set. Seed 2002 moving 9 points on an inert lever is the size of single-seed Top 10 noise.

**Recommendation.** Adopt `--label-demand-parity=1` first: it is the one lever that is large, same-signed on both seeds at every rung, and removes a double count rather than adding a handicap. Then decide on `ec` + `ad` + `cp` as a group (14 points together, each a defensible period claim, `cp` changes Majors' record quality from week 0), and on `fx` only after the regional guards are checked. Nothing is adopted yet; `major-scale-bundle-request.json` is unchanged.

**Next levers.** (1) Adult standard-bearers' singles (5–10 points of 1961 Top 10 slots at the Big-5 even under the full set; see the adult60 notes). (2) Big-5 1960 release volume and opening rosters, now that 1960 sits under history. (3) Realization of connecting small-label singles under quality 0.68 (2–6% reach the Top 40; half of small-label launches are below that line because label production is a founding draw). (4) A decade A/B of the adopted set on one paired seed, with closures and Major finances.

**Controls back on disk, and the dose (lean runs `parity-v2-{2002,1001}-{base,dp}`, `dp` at `-Parity 0.5`).** The lean controls reproduce the deleted ones exactly at every rung (47.2 / 55.8 and 26.8 / 50.0). Use `parity-v2-*-base` as the control from here.

| Demand parity | 1960 Top 10 | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 | 1961 premium |
|---|---|---|---|---|---|
| 0 (control) | 47.2 / 26.8 (37.0) | 55.8 / 50.0 (52.9) | 45.0 / 44.2 | 40.4 / 38.2 | 7.2× / 5.6× |
| 0.5 | 36.0 / 33.8 (34.9) | 44.8 / 43.1 (44.0) | 34.3 / 34.6 | 32.0 / 29.8 | 5.1× / 4.7× |
| 1 | 26.6 / 23.0 (24.8) | 45.8 / 41.2 (43.5) | 34.8 / 32.6 | 30.5 / 29.5 | 4.3× / 5.2× |

Half strength already gives the whole 1961 effect and leaves 1960 on the ~35 estimate; full strength only pushes 1960 down further. Realization at 0.5, seed 2002: Big-5 0.80, Independent 0.33, Small + Boutique 0.23 (control 0.95 / 0.25 / 0.15). The dose is not linear, so tune P inside the full set rather than alone.

## Update 2026-11-11: `--label-demand-parity=1` adopted into the reference bundle

Author's decision: adopt the flat label purchase-rate scale at full strength (P = 1), not the 0.5 dose.

- `SimLogs/writing-calibration/major-scale-bundle-request.json` now has 187 flags; the last one is `--label-demand-parity=1`. The previous 186-flag bundle is kept as `major-scale-bundle-request.pre-dp-2026-11-11.json`.
- Expected result (two-seed mean, from the `dp` screen): Big-5 Top 10 share 1960 24.8, 1961 43.5 (premium 4.3× / 5.2×). 1960 sits under the ~35 estimate; 1960 is not a calibrated year (Big-5 release volume).
- **New control:** `parity-v1-{2002,1001}-dp` (104 weeks, full output, still on disk) is the new bundle's base arm. `parity-v2-*-base` is now the pre-dp control.
- **Screen arms:** `run_indie_access_screen.ps1` reads this bundle, so every arm now carries dp = 1. Do not pass the `dp` arm on top (it would add a second `--label-demand-parity`). To test a different dose, pass `-Extra --label-demand-parity=P` only after checking which duplicate the argument parser keeps.
- Still open: `ec` + `ad` + `cp` as a group (about 14 points more), and `fx` after the regional guards are checked. Side effects to watch in a decade A/B: Big-5 singles units roughly halve, Big-5 gross −7%, pretax −8 to −10%.

## Update 2026-11-11: regional guards for `fx`, and the case for `ec` + `ad` + `cp`

All on existing 104-week runs, both seeds pooled, 1960. Stagger script: a copy of `SimTools/analyze_regional_stagger.py` with the prefix set to `parity-v1-{seed}-{arm}` (the repo copy hard-codes `hs52op…`). Station overlap: `SimTools/analyze_station_playlist_overlap.py <prefix> <prefix> --year 1960`. Economy: `SimTools/analyze_regional_guards.py`.

**City stagger, LA vs Minneapolis, national Top 10 hits on non-Big-5 labels (history in brackets):**

| Arm | n | City lead ≥ 2 wk (18.5) | City gap ≥ 3 wk (33) | City gap ≥ 6 wk (15) |
|---|---|---|---|---|
| `v2-base` (pre-dp control) | 132 | 14.4 | 50.0 | 23.4 |
| `dp` (new control) | 135 | 23.0 | 44.6 | 19.2 |
| `dp_fx` | 160 | **8.1** | 41.8 | **9.6** |
| `ec_dp_ad_cp` | 139 | 15.1 | 53.8 | 24.2 |
| `ec_dp_ad_cp_fx` | 161 | **7.5** | 46.7 | **9.2** |

**Station top-11 overlap, 1960 (history same city 69.7, cross city 40.3):** `dp` 72.5 / 38.0 and 71.6 / 39.5; `dp_fx` 71.5 / 42.5 and 74.4 / 43.8; `ec_dp_ad_cp` 70.7 / 38.8 and 71.6 / 41.1. `fx` raises cross-city overlap by 4 points and cuts cross-city timing mismatches 16 → 13 (history 14.3).

- **`fx` fails the stagger guard.** It halves the city lead and the 6-week city gap for non-Big-5 hits, on both arm pairs, to about half of history. That undoes the §9s–9u exposure work, which existed to create this stagger. Station overlap stays near history. **Do not adopt `fx`.** It was worth about 5 points of 1961 Big-5 Top 10 share; `fn` keeps the stagger and is about 4 points weaker.
- **`ec` + `ad` + `cp` passes the guards.** Against `dp`: stagger within noise (lead 23 → 15, nearer history; 6-week gap 19 → 24), station overlap unchanged, single units unchanged, airplay share of Top 40 points unchanged (0.13–0.16), closures 160 → 180 and 180 → 175.
- **Size of `ec` + `ad` + `cp` over `dp`, same sign on both seeds at every rung:** 1961 Big-5 Top 10 45.8 / 41.2 → 35.0 / 25.6; Top 40 34.8 / 32.6 → 27.4 / 24.0; Hot 100 30.5 / 29.5 → 26.2 / 23.1; Major-family chart share 1961 65.7 / 67.2 → 56.2 / 57.8; C4 32–34 → 26.
- **Recommendation:** adopt `ec` and `ad`, hold `cp`. `ec` (a charting indie record gets national restock and shelf) and `ad` (a held region takes a single on the Hot 100, not only the Top 40) stand for indie distributors picking up a record that is selling; both fit the period. `cp` instead takes away the Majors' production and scouting edge from week 0. The 1960 Majors did have better studios and A&R. The production study put full parity at only 2–3 points, so `cp`'s 5.9 leave-one-out points come from somewhere other than history. Before adopting, screen `ec_ad` on the new bundle (dp is already in it) on both seeds, using `--lean-probe`. No run has `ec` + `ad` + `dp` without `cp` or `fx`. `ec` also has no check runner yet.

## Update 2026-11-11: `ec` + `ad` on the dp bundle, and chart-proven wholesale lines (`cw`)

All 104 weeks, lean, current bundle (dp = 1 in every arm). Control `parity-v1-{2002,1001}-dp`. Cells are seed 2002 / seed 1001.

| Arm | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 | Bankruptcies (104 wk) | Singles launched 1961 |
|---|---|---|---|---|---|
| Control (dp) | 45.8 / 41.2 (43.5) | 34.8 / 32.6 | 30.5 / 29.5 | 124 / 141 | 3,986 / 3,845 |
| `ec_ad` (`ecad-v1`) | 32.7 / 38.7 (35.7) | 27.3 / 30.4 | 26.1 / 27.6 | 123 / 133 | |
| `cw` depth 40, monthly (`cpw-v1`) | 55.0 / 32.3 (43.7) | 40.0 / 27.2 | 34.4 / 24.7 | 169 / 182 | |
| `cw` depth 100, monthly (`cpw-v2`) | 39.8 / 42.1 (41.0) | 34.3 / 33.2 | 31.6 / 30.9 | 191 / 201 | |
| `cw` depth 100, weekly (`cpw-v3`) | 55.0 / 28.5 (41.8) | 42.9 / 24.2 | 36.9 / 25.1 | 198 / 231 | 3,427 / 3,123 |

**Historical review of `ec` and `ad`.** `ad` (a region the label does not ship to receives the record's held launch package once it is on the Hot 100) fits the period, since DJs and distributors read the Hot 100 and Bubbling Under. It creates no coverage. Depth 60–75 would be easier to defend than 100. `ec` reaches the right outcome but goes around the distribution layer: covered-grade restock with no house, capacity, margin or receivables, for AI labels only. The rack-jobber restock path that was meant to carry a proven record into uncovered markets is worth 0.30 × 0.50 of covered service in 1960, below the plain uncovered rate, so in 1960–61 it never helps.

**`--chart-proven-wholesale=N` (built; `Systems/ChartProvenWholesale.cs`, hook in `CompetitorManager.PursueIndependentDistribution`, arm `cw`, `-WholesaleDepth N`).** A label with a record at national 1..N counts as proven in every market. An open house in each uncovered market takes its line that month, or that week with `--chart-proven-wholesale-weekly`, skipping the 35% monthly roll. Optional cap `--chart-proven-wholesale-markets=M`. Placement goes through the ordinary route (house capacity, reach credit, receivables). The player's `HasProvenBreakoutIn` / `PlacePlayerLine` reads the same proof. Check `--chart-proven-wholesale-check` passes. Off is byte-identical for 27 weeks (`cpw-off`, `cpw-off2`), including after the weekly refactor. Not adopted.

- **Lines fire and land in time.** Placements rise 632 → 867 (depth 40) and 1,351–1,464 (depth 100 weekly). About 300 land within 4 weeks of a label's first Top 40 week, against about 55 in the control.
- **At depth 40 it cannot help a record get into the Top 40**, because the proof only exists once the record is there. Small-label realization barely moves.
- **At depth 100 weekly, conversion improves:** Small + Boutique Top 10 weeks per expected connect 0.26 → 0.41 and 0.13 → 0.46. But the field launches 12–19% fewer singles and bankruptcies rise 60–64%. Labels with three or more lines go bankrupt at 36–44%, against 16–19% in the control. Big-5 share does not fall (mean 1961 Top 10 41.8, Top 40 33.6, Hot 100 31.0).
- **Why taking a line hurts a label: payment asymmetry in `CompetitorManager.DeferWholesaleBillings`.** A market the label does not cover books its sales as cash that week at full value. A house market books them as a receivable due in 12–18 weeks, collected at the house's reporting honesty (mean 0.77). So for a hit already selling in uncovered markets, a house line turns full immediate cash into 77% paid four months later, while pressing bills for the bigger runs come due weekly. That is a stylised version of the real independent-label "hit can kill you" squeeze. Here it is made worse by the uncovered channel paying instantly and in full, which no 1960 channel did.

**Options (author's call):** (A) make uncovered-market sales pay on one-stop / rack terms too, which removes the asymmetry for every label and reshapes small-label finance, so it needs its own screen. (B) cover the timing gap for chart hits with factoring or plant credit on the existing ledgers (`--hit-follow-up-finance` already factors receivables but only for follow-up slots, and is not in the bundle). (C) use `ec` + `ad` as a calibration stand-in, labelled as such. `cw` as built should not be adopted alone.

## Update 2026-11-13: chart-proven lines with financing, and one-stop terms (`ut`)

All 104 weeks, lean, current bundle (dp = 1). Second control `gamey-ro-{2002,1001}-x` = bundle + `--receivable-backed-overdraft=0.75` (from the game-y audit). Cells seed 2002 / seed 1001, 1961 Big-5 share.

| Arm (all with overdraft 0.75) | Top 10 | Top 40 | Hot 100 | Bankruptcies | Independent singles launched (weeks 1–92) |
|---|---|---|---|---|---|
| Overdraft alone (`gamey-ro`) | 46.0 / 36.7 | 33.8 / 27.2 | 31.4 / 24.2 | 112 / 121 | 2,670 / 2,575 |
| + `cw` (`cpwro-v1`) | 42.7 / 32.9 | 34.1 / 31.0 | 32.2 / 30.7 | 123 / 139 | 2,163 / 2,006 |
| + `ut` (`utro-v1`) | 38.5 / 30.4 | 29.3 / 23.3 | 27.6 / 22.1 | 110 / 131 | 2,524 / 2,417 |
| + `cw` + `ut` (`utro-v1`) | 46.9 / 34.2 | 38.3 / 25.7 | 34.5 / 26.0 | 122 / 151 | 2,213 / 1,896 |
| + `ut` + factoring (`utff-v1`) | 38.3 / 38.8 | 32.6 / 29.4 | 29.7 / 27.5 | 109 / 135 | 2,646 / 2,522 |
| + `cw` + `ut` + factoring (`utff-v1`) | 29.0 / 35.6 | 28.6 / 32.5 | 27.2 / 29.7 | 122 / 144 | 2,304 / 2,147 |

`cw` = `--chart-proven-wholesale=100 --chart-proven-wholesale-weekly`. Factoring = new `--hit-follow-up-factoring`.

**New switches (off by default; off byte-identical for 27 weeks, `cpw-off3`, `cpw-off4`):**
- `--uncovered-sales-terms=W` (+ `--uncovered-sales-honesty=H`, default 0.85; `Systems/UncoveredSalesTerms.cs`; hook `CompetitorManager.DeferUncoveredBillings`; arm `ut`, `-TermWeeks`, default 10). An AI label's sales in a market it has no distribution in become a receivable `onestop_<region>` billed at H and due in W weeks. Same ledger as the houses, so the overdraft, factoring, tax and accounts read it. Collected in full at maturity. Player untouched (trunk). Check `--uncovered-sales-terms-check` passes.
- `--hit-follow-up-factoring` (`LabelReleaseProgramme`, `CompetitorManager.HitFollowUpFactoringApplies`): the funding half of `--hit-follow-up-finance` only. A cash-short release by a non-Major act with a recent Top 40 may factor the label's receivables, with no extra slots. The bundle's `--slate-breakout-planning` refuses `--hit-follow-up-finance`; this one runs beside it. No check runner.
- Plant credit was already on (`LabelPressingEconomics`, default unless `--no-plant-credit`).

**Findings.**
- **The overdraft absorbs the `cw` bankruptcy wave** (198 / 231 → 123 / 139). Small-label conversion rises: Independent Top 40 per expected connect 0.30 → 0.40 and 0.36 → 0.42; Small + Boutique Top 10 weeks per expected connect 0.30 → 0.44 and 0.18 → 0.39.
- **`cw` costs releases even when bankruptcies are flat.** Independent launches fall 13–22% in every `cw` arm. Programme cash shortfalls rise 55–60% (`programme-cash-shortfalls.csv`), concentrated on Rising hit labels: median receivables $3.8–5.3k, tax-and-service hold $2–3.4k, commercial hold $2–2.8k, nothing factored. Factoring recovers only part (−13 to −15% remaining). The tax-and-service hold on income not yet collected is the next suspect ([[tax-paid-on-uncollected-receivables]]).
- **`ut` alone lowered Big-5 share on both seeds at every rung (−4 to −7.5 at the Top 10)**, but adding factoring, which barely touches non-hit labels, takes seed 1001 back up (30.4 → 38.8). Treat the `ut` effect as unreplicated: single-seed swings of 5–8 points between arms that differ in small finance details are reshuffle noise at this effect size.
- **No arm in this chain moves Big-5 share with the same sign on both seeds at every rung.** The best mean is `cw_ut` + factoring (1961 Top 10 32.3 vs 41.4), carried by seed 2002 alone.
- Regional stagger guard passes in every arm (non-Big-5 Top 10 city lead ≥ 2 wk 21–32%, 6-week gap 19–27%).

**Where this leaves it.** `ec` + `ad` remains the only footprint lever that lowered Big-5 share on both seeds at every rung (1961 Top 40 34.8 / 32.6 → 27.3 / 30.4). The financing pieces (overdraft, plant credit, one-stop terms, factoring) are defensible on their own as historical economics, and the overdraft is already marked adoptable by the game-y audit. They should be judged on finance and survival metrics, not on Big-5 share. If `cw` is wanted as the period-correct replacement for `ec`, the release-funding drop must be fixed first, and the screen needs 4+ seeds to resolve a 3–5 point effect.

## Update 2026-11-13 (later): one-stop terms + overdraft adopted; release-funding fix screened on four seeds

**Bundle now 189 flags:** `--uncovered-sales-terms=10` and `--receivable-backed-overdraft=0.75` added (author's decision). Previous bundle `major-scale-bundle-request.pre-ut-ro-2026-11-13.json`. New control `fund-v1-{2002,1001,3003,6006}-base`. Seeds 4004/5005 skipped (concentrated worlds).

**New switches (`Systems/ProgrammeFundingRelief.cs`, off by default; arms `gc`, `of`; check `--programme-funding-relief-check` passes; `--slate-work-budget-check` still passes; off byte-identical for 27 weeks, `cpw-off5`):**
- `--slate-going-concern-reserve-all-tiers` (needs `--slate-going-concern-reserve`): the going-concern release reserve (hold only tax due within two month-ends, no commercial hold) applies to every work-budget label, not only MidTiers. Before this, Independent / Small / Boutique releases reserved tax on booked but uncollected income plus 2 × recent commercial spend.
- `--programme-overdraft-funding` (needs `--receivable-backed-overdraft`): a release by an act with a recent Top 40 may draw unused overdraft headroom (0.75 × receivables less the overdraft already drawn), all or nothing.
- `run_indie_access_screen.ps1` gained `-WholesaleWeekly` (weekly flag for the `cw` arm only).

**Four-seed means (per-seed in the scorer output; `cw` = `--chart-proven-wholesale=100` weekly):**

| Arm | 1960 Top 10 | 1961 Top 10 | 1961 Top 40 | 1961 Hot 100 | Independent singles 1961 | All singles 1961 | Bankruptcies | Non-Major gross ($M, 2 yr) |
|---|---|---|---|---|---|---|---|---|
| `base` | 27.5 | 39.9 | 30.2 | 26.7 | 1,471 | 3,826 | 117.8 | 301 |
| `gc_of` | 26.6 | 37.9 | 29.8 | 27.1 | 1,580 | 3,996 | 122.5 | 314 |
| `cw_gc_of` | 25.0 | 42.5 | 33.7 | 30.5 | 1,475 | 3,760 | 123.0 | 304 |

- **The fix restores releases.** `gc_of` lifts Independent singles 7.4% and all singles 4.4%, which more than offsets the ~5% that one-stop terms cost. Non-Major gross is up 4%, tax paid up 3%, bankruptcies +4.7 a seed (4%). Big-5 share is flat: 1961 Top 10 per seed −7.9 / −2.1 / +7.9 / −5.6, Top 40 and Hot 100 within ±3. Stagger guard unchanged (non-Big-5 Top 10 city lead ≥ 2 wk 20.7%, 6-week gap 23.0%).
- **Chart-proof lines are now rejected.** With funding fixed, `cw_gc_of` brings Independent launches back to base (1,475 vs 1,471), but Big-5 share rises: Hot 100 up on all four seeds (+3.2 / +8.2 / +1.3 / +2.4), Top 40 up on three. The lost slots are MidTier's, not the Independents': mean 1961 Hot 100 slot-weeks a seed Big-5 1,390 → 1,587, MidTier 1,152 → 944, promoted Majors 70 → 12, Independent 1,596 → 1,654. MidTier launches fall 8% (339 → 313). The lines give Independents national coverage that crowds the MidTiers, and the Big-5 pick up what MidTiers lose. Mechanism not traced further.
- **Recommendation:** adopt `--slate-going-concern-reserve-all-tiers` and `--programme-overdraft-funding` as the companion to one-stop terms. They restore releases at a small bankruptcy cost, and the change is historically sound: no one reserved tax on income not yet received, and labels pressed follow-ups on credit. Their separate contributions are not split. Drop `cw`. For Big-5 share, `ec` + `ad` is still the only lever that moved every rung on both earlier seeds; rescreen it on the new bundle on four seeds before deciding.

**2026-11-13 (end):** `--slate-going-concern-reserve-all-tiers` and `--programme-overdraft-funding` adopted. The bundle is now 191 flags (old: `.pre-gc-of-2026-11-13.json`); control `fund-v1-*-gc_of`. Root-cause work continues in `SimTools/MajorShareRootCauseHandoff.md`.
