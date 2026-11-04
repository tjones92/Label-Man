# Celebrity fanbase Phase 4 notes: follower demand, advance orders and live draw

Status: built 2026-11-01, off unless named. Design: `CelebrityFanbaseDesign.md` §2.4, §10 Phase 4. This phase enacts
directive amendments §0.1 (a following changes later records' penetration) and §0.2 (follower purchases are audience
behavior, outside the media demand ledger and its caps).

## What is built

| Piece | Where | What it does |
|---|---|---|
| Switches | `MediaFollowerDemand.cs` | `--observe-follower-demand` (shadow) and `--follower-demand` (causal, needs `--causal-media-atmosphere`). Both need `--observe-following`. |
| Launch book | `FollowerLaunch`, `MediaSaveData.FollowerLaunches` | Opened the first time a release is seen (the AI launch, or the first clearing week of any other record) from the act's followers by region: young × 0.35 + adult × 0.10 for a single, young × 0.12 + adult × 0.30 for an LP. Kept only at 100 copies or more. Saved. |
| Release-on-sight intent | `ChartManager.CalculateLiveRegionalSalesWithMarketClearing`, `PrepareFollowerDemand` | Each of the first three clearing weeks adds 50% / 30% / 20% of the region's book to the title's serviceable intent, after the physical limit is known and before the format budgets clear. Never past the title's stock or shelf capacity that week. Fans compete inside the purse: where it binds (most region-weeks) they move units toward their act. Singles and LPs alike. |
| Disappointment | `FollowerLaunchLoss` | A release that disappoints its followers (§2.3) loses that share of weeks two and three. The advance is bought unheard. |
| Advance orders | `StockAdvanceOrders`, both AI launch paths in `CompetitorManager` | Dealers stock the book's three-week intent on top of the launch shelf, in every region the record reached. Never for the player's records: the player's pressing is real. Shows in `initialLaunchStock`. |
| Live draw | `LocalSceneRoomService` (bill scheduling) | Followers in a room's region add 1 in 2,000 of themselves to a bill's expected audience (`LiveNightShare`), up to the room's capacity. Causal only. |
| Live joiners | `NoteLiveCrowd`, `FollowingWeek` | Each performed set's share of the crowd joins the act's following at `cLive` 0.15 (§2.2), young/adult split as a single's buyers of the genre. Shadow and causal. |
| Telemetry | console, `following.csv` | `MEDIA_FOLLOWER_DEMAND` per year: books, asked / granted by format, copies stocked, advance-order sizes (median, p90, max, over 100k), live crowd and joiners, the top three books. `MEDIA_FOLLOWING` gains `joinLive`. A `launch` row per closed book. |
| Screen script | `SimTools/analyze_follower_demand.py` | Repeat-hit share by year, straight Top 40 runs, acts and the top 5%'s share of Top 40 entries, the follower lines. The economy table stays `analyze_media_recognition_screen.py`. |

Not built:
- the bookers' term (§2.4.3) and the player's fan-club report and advance-order line (§2.4.5);
- a live family in salience (§7.1);
- restock reading fan intent: dealers reorder on ordinary demand only, so a book bigger than its advance stock is lost.

## Evidence

**Fixtures** (`MediaAtmosphereChecks`, 279 pass at `--seed=1001`, 9 new):
- arithmetic: 100k young and 20k adult followers ask for 37k copies of a single and 18k of an LP; the book is spent over
  three weeks (sum 1); disappointment cuts weeks two and three, never the first; fans never pass the physical limit; a
  150k regional following brings 75 people to a bill;
- in the world: five shadow weeks open 159 books (84k copies asked, 80k would clear); no book outlives its third week;
  every book matches its record's act and format; shadow adds nobody to a room; books survive a save round trip; two
  causal weeks put 125k fan copies into clearing, never past the stock.

**Shadow gate**, 104 weeks, ref flags (`--aggregate-only`) + `--observe-following --observe-follower-demand`, against
`ref-f50b9a4-<seed>-104`: **98/98 economy CSVs byte-identical on seeds 1001 and 2002** (`fd4obs-<seed>-104`). The
reference also holds eight scene and band-life files this build no longer writes; the Phase 3 gate compared the same 98.

What shadow says fans would do in 1960:

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Books, singles / LPs | 1,789 / 625 | 1,683 / 584 |
| Asked, singles / LPs | 7.8M / 1.4M | 7.7M / 1.1M |
| Through the stock without advance copies | 6.5M / 1.1M | 6.2M / 0.9M |
| Advance copies dealers would stock | 9.3M | 9.1M |
| Single books: median / p90 / max | 0.4k / 7.4k / 213k | 0.4k / 6.7k / 229k |
| Single books over 100k | 14 | 21 |
| Live crowd / live joiners | 1.35M / 87k | 1.35M / 77k |

- Fans ask for about 5% of the year's singles purse (roughly 150M units). Without advance copies, stock turns away
  about a sixth of it, which is why the stock arm is part of the causal switch.
- The biggest books, 200-230k on one single (Grant and the Daydreams, Biff Sanders), sit inside §9's 100-300k for a solid
  fanbase act. 1960 has no phenomenon, so the 1-2M ceiling isn't tested.
- Live joiners are under 1% of buyer joiners: the rooms are small.

**Historical target for the screen.** From the recovered Hot 100, 63-65% of Top 40 entries in 1962-64 came from acts with
another Top 40 in the two years before. The Phase 3 control (`res3b-t`) is at 32-35% in 1964-66. The top 5% of acts held
24% of history's 1962-64 Top 40 entries, against 15-17% in the control.

## Causal screen, 1963-66 resumes

`fd4-t` / `fd4-t2002`: the Phase 3 flags (`--causal-media-atmosphere --media-recognition --fame-registration
--observe-following --media-residencies --lean-probe`, `--aggregate-only`) plus `--follower-demand`, resuming
`media-p3-1001-1963` and `asc-2002-1963` for 209 weeks. Controls: `res3b-t` / `res3b-t2002`, the same flags without the
switch. Telemetry starts at the resume, so 1963's repeat share has no prior years to look at; read 1965-66.

### What fans did

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Fan copies asked, singles 1963 / 64 / 65 / 66 | 2.9 / 6.9 / 10.9 / 15.2M | 3.3 / 8.5 / 13.1 / 18.0M |
| Fan copies asked, LPs 1963 / 64 / 65 / 66 | 1.5 / 4.0 / 8.1 / 10.4M | 1.4 / 5.1 / 8.5 / 10.6M |
| Share that cleared the stock | 99% | 99% |
| Advance copies stocked, 1966 | 25.7M | 29.0M |
| Largest single book 1964 / 65 / 66 | 193k / 264k / 457k | 201k / 505k / 649k |
| Single books over 100k, 1966 | 24 | 32 |

- With the advance stocked, fans are almost never turned away: 99% of what they ask for reaches clearing.
- Fan demand grows every year because followings do: they are still growing in 1966 (Phase 3: 22M people in 1963 to
  77M in 1966). In 1966 fans ask for about a tenth of the singles purse.
- The largest 1966 books (450-650k) sit between §9's solid-fanbase 100-300k and the phenomenon's 1-2M. Neither world
  has a phenomenon.

### What it did to the charts (§9)

| Repeat-hit share | Seed 1001 control → treatment | Seed 2002 control → treatment | History, 1962-64 |
|---|---|---|---|
| 1964 | 22.1 → 24.1% | 18.9 → 30.0% | 63-65% |
| 1965 | 31.6 → 40.1% | 33.8 → 42.6% | |
| 1966 | 32.7 → 52.3% | 36.3 → 49.2% | |

| | Seed 1001 | Seed 2002 | History |
|---|---|---|---|
| Acts with 5+ straight Top 40 singles | 5 → 13 | 4 → 17 | |
| Longest run | 6 → 12 | 8 → 12 | |
| Top 5% of hit acts' share of Top 40 entries | 14.9 → 18.6% | 16.7 → 22.1% | 24% |
| Acts with a Top 40 entry | 522 → 476 | 527 → 474 | 392 in 1962-64 |

This is the phase's purpose and it works: about half of the gap to history's repeat share closes by 1966, on both seeds.

### Economy (t − c)

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Top 10% acts' slot-weeks (kill +3 pt) | **+3.53 pt, KILL** | +2.48 pt |
| Major-family share 1964 / 65 / 66 | −0.5 / +4.5 / −2.1 | +7.7 / −0.4 / +0.3 |
| CR4 1966 | −5.9 pt | −2.1 pt |
| Singles 1964 / 65 / 66 | −1.4 / −4.1 / −2.7% | −2.3 / −4.2 / −4.0% |
| Albums 1964 / 65 / 66 | +3.5 / +9.7 / +5.5% | +6.5 / +11.2 / +9.7% |
| Genre beyond ±1 pt | Country +2.0, Soul −1.8, EasyListening +1.1 | EasyListening +1.6, RnB −1.4 |
| Closures | 113 → 117 | 115 → 97 |

**Concentration.** §0.4 asks for historical targets in place of "no change", and the kill rule still stands. By the
history measure, concentration is still below 1962-64 (top 5% at 18.6-22.1% against 24%, more distinct hit acts than
history). But seed 1001 crosses the kill line set for the economy, so the rule as written stops adoption.

**Albums are the defect.** The fixed purse that makes fans move units, not create them, binds for singles in 100% of
region-weeks in 1964-66, but for LPs in only 19% (control, seed 1001; 42% with fans). Fans' 8-10M LP copies a year
therefore mostly add units: albums +9M in 1965. Album purchasing then takes capacity singles would have used through
spillover, so cleared singles fall 4-7M, though the singles budget is unchanged. Both unit flags have this one cause.
Major share and CR4 don't move systematically.

## Verdict

Not adoptable as built: the kill rule trips on seed 1001 and LP fan demand inflates album units on both seeds. The
mechanism does what the design wants on the measure it was built for (repeat hits, consecutive runs). Candidate fixes,
for the author:
1. **LPs:** make fan LP intent move units rather than add them, either by reallocating within the album intent (as
   singles do implicitly) or by cutting the LP rates (0.12 / 0.30) until the LP purse binds the same way; or leave LPs
   out of Phase 4.
2. **Concentration:** steady-state followings are not reached in four years, so the 1966 effect is still rising. A lower
   single rate, or a following-size saturation in the book (the largest books growing past 450k), would hold the top 10%
   inside +3 pt. Alternatively the author amends the kill rule to the §9 historical target (§0.4), by which the
   treatment is still under history.

## Fix 1: fan LP demand moves LP buyers (2026-11-01)

**Change.** `ChartManager.ApplyFollowerAlbumIntent` → `MediaFollowerDemand.ConserveAlbumIntent`. In causal mode an LP's fan
grant no longer adds to the region's LP intent. After every title's intent is known, each region's LP intent is held at
its pre-fan total: the fans' grants (scaled down if they exceed it) are taken from every LP's own intent in proportion,
including the fan title's. This makes the singles rule ("fans move units, the purse binds") explicit for LPs, whose
purse rarely binds. Singles are unchanged. `MEDIA_FOLLOWER_DEMAND` gains `displacedLPs`. Shadow is untouched. One new
fixture (280 pass at `--seed=1001`).

**Screen.** `fd4lp-t` / `fd4lp-t2002`, the same resumes and flags as `fd4-t`, against `res3b-t` / `res3b-t2002`.

| t − c | Seed 1001: fd4 → fd4lp | Seed 2002: fd4 → fd4lp |
|---|---|---|
| Albums 1964 / 65 / 66 | +3.5 / +9.7 / +5.5% → **+1.1 / +4.3 / +3.2%** | +6.5 / +11.2 / +9.7% → **+6.4 / +2.2 / +1.6%** |
| Singles 1964 / 65 / 66 | −1.4 / −4.1 / −2.7% → −0.5 / −1.8 / −1.6% | −2.3 / −4.2 / −4.0% → −2.3 / −0.8 / −0.7% |
| Top 10% acts' slot-weeks (kill +3 pt) | +3.53 KILL → **+2.30** | +2.48 → +0.36 |
| Repeat-hit share 1966 (control 32.7 / 36.3%) | 52.3 → 49.5% | 49.2 → 48.6% |
| Acts with 5+ straight Top 40 / longest | 13 / 12 → 12 / 8 | 17 / 12 → 15 / 12 |
| Top 5% of hit acts' share of Top 40 entries | 18.6 → 16.9% | 22.1 → 21.8% |
| Major-family share 1963 / 64 / 65 / 66 | −3.2 / −0.5 / +4.5 / −2.1 → **+5.8 / +7.7 / +7.4 / +3.8** | +1.3 / +7.7 / −0.4 / +0.3 → +0.1 / −1.5 / +1.6 / +1.5 |
| Genre beyond ±1 pt (fd4lp) | TeenPop +1.5, RockAndRoll −1.5, Soul −1.0 | RnB −1.0 |
| Closures | 113 → 111 | 115 → 117 |

- The repeat-hit gain survives: about half the gap to history still closes by 1966 on both seeds.
- The kill rule now passes on both seeds.
- Album units fall by half or more but are not neutral. Fan LP copies are fully displaced in-week (`displacedLPs` =
  LP granted, 9-11M in 1966), yet raw LP demand still rises 4-6M a year (seed 1001; seed 2002 1964), with LP releases flat:

  | Seed 1001, control → treatment | Raw LP demand | Serviceable LP | Cleared LP |
  |---|---|---|---|
  | 1964 | 100.3 → 104.4M | 88.4 → 94.0M | 83.8 → 84.7M |
  | 1965 | 107.9 → 112.0M | 95.3 → 100.5M | 92.2 → 96.1M |
  | 1966 | 128.8 → 135.0M | 114.3 → 122.2M | 108.8 → 112.3M |

**Why albums still rise.** LP demand is a buyer pool that exhausts with each title's own regional sales
(`AlbumSimulator.CalculateRegionalSales`, penetration → exhaustion). A displaced LP keeps its unexhausted pool and sells
to it later, so in-week conservation defers other LPs' sales rather than removing them. A second, smaller route: the
dealers' advance copies relieve stock-outs on the fan LP's ordinary demand (the raw-to-serviceable gap narrows 1-2M a
year). Per-record album units aren't written under `--aggregate-only`, so the split between deferral, chart awareness
(a Top 10 LP's awareness floor) and stock relief is from the arithmetic, not measured per title.

**Major share on seed 1001** rises 4-8 pt every year, from 1963, where `fd4-t` was mixed. Seed 2002 doesn't move. One
paired seed can't tell a mechanism from reshuffle ([[three-seed-paired-se-understates-reshuffle]]); it needs a look
before adoption.

**Candidate next steps.**
1. Book displaced LP buyers against the displaced titles' exhaustion (a spent-buyer counter beside
   `rackStoreBuyerUnitsTotal`), so a displaced purchase is gone, not deferred.
2. Stock only the fans' copies for LPs (advance stock that ordinary demand can't draw).
3. Accept +2-4% albums against the §9 measures and move to the concentration and Major-share checks.

## Fix 2: a buyer lost to a fan title is gone, not deferred (2026-11-01)

**Change.** `RegionalRecordData.fanDisplacedBuyerUnitsTotal`, beside `rackStoreBuyerUnitsTotal`. After clearing,
`ChartManager.BookFanDisplacedBuyers` books each LP's displaced intent (Fix 1) against its own buyer cohort, at the
share of the region's LP intent that cleared that week. Where the LP purse binds, part of the intent was never going to
sell. `AlbumSimulator.CalculateEffectiveRegionalPenetration` and the budget-LP path add it to penetration, so the
exhaustion curve treats a lost purchase like a sale. It's saved only when nonzero, and written only under
`--follower-demand`. `MEDIA_FOLLOWER_DEMAND` gains `spentLPs`. One new fixture (281 pass at `--seed=1001`).

**Screen.** `fd4sp-t` / `fd4sp-t2002`, the same resumes and flags, against `res3b-t` / `res3b-t2002`. `spentLPs` is
95-99% of `displacedLPs` (9.1M / 9.8M in 1966).

| t − c | Seed 1001: fd4 → fd4lp → **fd4sp** | Seed 2002: fd4 → fd4lp → **fd4sp** |
|---|---|---|
| Albums 1963 / 64 / 65 / 66 | +0.9 / +3.5 / +9.7 / +5.5% → −0.1 / +1.1 / +4.3 / +3.2% → **−1.4 / +0.1 / +3.1 / +1.9%** | +0.6 / +6.5 / +11.2 / +9.7% → −0.3 / +6.4 / +2.2 / +1.6% → **+2.5 / +2.5 / −0.1 / +0.6%** |
| Singles 1964 / 65 / 66 | −1.4 / −4.1 / −2.7% → −0.5 / −1.8 / −1.6% → **−0.0 / −1.3 / −0.9%** | −2.3 / −4.2 / −4.0% → −2.3 / −0.8 / −0.7% → **−0.9 / −0.0 / −0.2%** |
| Top 10% slot-weeks (kill +3 pt) | +3.53 → +2.30 → **+3.03, KILL** | +2.48 → +0.36 → **+1.79** |
| Repeat-hit share 1966 (control 32.7 / 36.3%) | 52.3 → 49.5 → **50.5%** | 49.2 → 48.6 → **47.4%** |
| Acts with 5+ straight Top 40 | 13 → 12 → **16** | 17 → 15 → **20** |
| Top 5% of hit acts' share of Top 40 entries (history 24%) | 18.6 → 16.9 → **19.0%** | 22.1 → 21.8 → **21.2%** |
| Major-family share 1963 / 64 / 65 / 66 | **+0.8 / +9.0 / +2.5 / +4.9** | **+6.0 / +3.7 / +1.7 / +1.7** |
| Genre beyond ±1 pt | Soul −1.7, TeenPop +1.4, RockAndRoll −1.2, RnB −1.0 | Soul +2.5, TraditionalPop −2.3 |
| Closures | 113 → 117 → 111 → **146** | 115 → 97 → 117 → **93** |

- **Albums are inside the noise.** Seed 2002 moves +2.5% in 1963, when fans' LP copies are only 1.5M, and seed 1001
  moves −1.4%. The four-year mean of the treatment is +0.9% albums and −0.4% singles. The 1965 +3.1% on seed 1001
  remains the largest cell. Raw LP demand no longer climbs year on year (1966: +1.0M and −1.6M, against +6.2M in fd4lp).
- **Repeat hits hold.** About half the gap to history's 63-65% still closes by 1966, and consecutive runs are up on
  both seeds.
- **The kill rule is at the edge on seed 1001.** It is +3.03 pt here and +2.30 pt in fd4lp. Between those runs the only
  change is LP exhaustion, which barely touches singles. So the 0.7 pt swing is reshuffle, and one paired seed can't
  place the effect on either side of +3 pt.
- **Closures and genre move by reshuffle-sized amounts in opposite directions.** Closures are +33 on seed 1001 and −22 on
  seed 2002; Soul is −1.7 and +2.5. None of these is a consistent direction.
- **Major share is now positive on both seeds** (+0.8 to +9.0 pt), smaller on 2002. Across the three variants seed 1001
  has gone mixed → +4-8 → +1-9. This is the open economy question.

**Verdict.** The LP defect is fixed. Fans now move LP buyers the way the purse makes them move singles, and a lost sale
stays lost. Adoption now turns on two questions for the author:
- concentration at the kill line on seed 1001, against a §9 historical target the treatment is still under;
- the Major-share rise, which needs more seeds or a mechanism trace before it can be called real.

## Trace: where seed 1001's Major-share rise comes from (2026-11-01)

`majorFamilyChartShare` is the Major + MidTier share of charted single units, by each label's year-end tier (after
acquisitions). So it moves when units move and when labels change tier. `SimTools/analyze_family_share_trace.py` rebuilds
it from `label-sales-weekly.csv` and `label-finance.csv`; the rebuild matches `concentration.csv` to the third decimal.
Pairs `fd4sp-t` / `fd4sp-t2002` against `res3b-t` / `res3b-t2002`.

**1. The rise is tier changes, not Majors selling more.** Scoring the treatment's sales on the control's year-end tiers
removes all of it:

| Family share, t − c | 1963 | 1964 | 1965 | 1966 |
|---|---|---|---|---|
| Seed 1001, own tiers | +0.8 | +9.0 | +2.5 | +4.8 |
| Seed 1001, treatment sales on control tiers | −2.3 | −6.2 | −4.1 | −3.0 |
| Seed 2002, own tiers | +6.0 | +3.7 | +1.7 | +1.8 |
| Seed 2002, treatment sales on control tiers | +0.2 | +0.5 | −0.8 | −3.8 |

The units that move in are all Independent → MidTier promotions that happened only in the treatment (seed 1001: 6-17 pt
a year; seed 2002: 3-6 pt). The reverse, MidTier → Independent only in the treatment, takes back 0.2-3.5 pt. The fixed-tier
row overstates the fall: a promoted label sells more because it is MidTier, so its units are scored as Independent here.
Its sign isn't evidence that fans move units to independents.

**2. The Major tier itself doesn't move consistently.** Major-only share is +1.6 to +4.9 pt on seed 1001 and −1.8 to
−3.7 pt on seed 2002. What both seeds share is more MidTier firms at year end (seed 1001: 26-39 → 29-51; seed 2002:
22-32 → 29-37) and MidTier share up on seed 2002 every year (+3.6 to +9.7).

**3. Promotions rise, and demotions too.** Independent → MidTier over 1963-66: 55 → 72 (seed 1001), 35 → 50 (seed 2002).
MidTier → Independent: 41 → 48 and 27 → 40. Of the labels promoted in only one run, 30 vs 20 (seed 1001) and 15 vs 8
(seed 2002) are on the treatment side.

**4. Fans don't carry the promoted labels.** Labels promoted only in the treatment hold 3.0% (seed 1001) and 1.3%
(seed 2002) of all fan units. The largest carriers were promoted at the gate's edge: `recentChartingRecords` 5-7
against the 5-record bar on the dependent route. Once promoted, their chart share jumps (Diamonds 0.0 → 1.6%, Arms
0.3 → 1.5%, Rhinestones 0.3 → 1.7%), because MidTier status brings the label more resources. Seed 2002's biggest year
(+6.0 pt) is 1963, when fans asked for the fewest copies, and is carried by labels with 0-7k fan units.

**5. No systematic route into the gate was found.**
- *Charting records:* fans add 35-77 a year from 1964 on (seed 1001), but they land at Majors (+25 to +52 a year) and
  MidTiers. Independents' charting records are flat or down on both seeds, so the gate's chart count isn't raised
  where it binds.
- *Profit:* the share of Independent label-months in profit is within 1 pt of the control on both seeds.
- *Advance copies:* not free margin; pressing is charged on copies sold (`LabelPressingEconomics.Press`).

**Reading.** The Major-share rise is MidTier headcount. It comes from a knife-edge promotion gate whose effect on the
measure is amplified after promotion, and it doesn't follow fan demand: the promoted labels have few fans, and the
largest year comes with the least fan demand. The one part common to both seeds is more Independent ↔ MidTier churn.
With two seeds that is suggestive, not shown. Fixed-tier sales show no Major gain from fans. Taken alone, the measure
can't screen this phase, because it swings several points with label reclassification.

**What would settle it.** A third and fourth seed pair, read on fixed-tier shares and promotion counts rather than
`majorFamilyChartShare`; or the same pairs with tier changes frozen at the resume, if a switch for that exists.

## Four seeds (2026-11-01)

New 1963 worlds `fdw-3003-1963` and `fdw-4004-1963`, saved with the same flags as `asc-2002-1963` (the two decade flags,
157 weeks). Pairs `res3b-t<seed>` / `fd4sp-t<seed>`, same flags as the seed 1001 and 2002 pairs. Seed 4004 is a
concentrated world ([[seeds-4004-5005-are-concentrated-worlds]]); the pairing absorbs that.

| t − c | 1001 | 2002 | 3003 | 4004 | Mean |
|---|---|---|---|---|---|
| **Top 10% slot-weeks (kill +3 pt)** | +3.03 KILL | +1.79 | +3.15 KILL | +4.17 KILL | **+3.04** |
| Repeat-hit share 1966 (pt) | +17.8 | +11.1 | +15.7 | +10.9 | +13.9 |
| Repeat-hit share 1966, treatment | 50.5% | 47.4% | 50.8% | 50.2% | 49.7% (history 63-65%) |
| Acts with 5+ straight Top 40 (control → t) | 5 → 16 | 4 → 20 | 6 → 20 | 8 → 21 | |
| Top 5% of hit acts' share of entries, treatment | 19.0% | 21.2% | 20.4% | 18.9% | 19.9% (history 24%) |
| Albums 1963 / 64 / 65 / 66 (%) | −1.4 / +0.1 / +3.1 / +1.9 | +2.5 / +2.5 / −0.1 / +0.6 | +2.1 / +3.1 / −4.3 / +1.7 | −5.1 / −0.8 / −2.6 / −2.1 | −0.5 / +1.2 / −1.0 / +0.5 |
| Singles 1964 / 65 / 66 (%) | −0.0 / −1.3 / −0.9 | −0.9 / −0.0 / −0.2 | −0.7 / +1.2 / −0.6 | +0.2 / +0.7 / +0.5 | −0.4 / +0.2 / −0.3 |
| Family share, own tiers 1963 / 64 / 65 / 66 (pt) | +0.8 / +9.0 / +2.5 / +4.8 | +6.0 / +3.7 / +1.7 / +1.8 | +1.6 / +5.1 / −1.2 / +6.0 | +0.5 / +4.6 / +2.5 / +0.7 | +2.2 / +5.6 / +1.4 / +3.3 |
| Family share, control tiers (pt) | −2.3 / −6.2 / −4.1 / −3.0 | +0.2 / +0.5 / −0.8 / −3.8 | +4.4 / +1.1 / −2.0 / −1.9 | +1.1 / +1.5 / −0.6 / −1.4 | +0.9 / −0.8 / −1.9 / −2.5 |
| Major tier alone (pt) | +1.6 / +4.8 / +1.9 / +4.9 | −3.7 / −1.9 / −3.0 / −1.8 | +6.4 / +5.3 / +4.4 / +1.3 | −0.8 / +0.7 / +2.1 / +1.5 | +0.9 / +2.2 / +1.4 / +1.5 |
| Indie → MidTier promotions, 1963-66 | 55 → 72 | 35 → 50 | 57 → 76 | 62 → 56 | |
| MidTier → Indie demotions | 41 → 48 | 27 → 40 | 35 → 48 | 38 → 35 | |
| Fan units at labels promoted only in t | 3.0% | 1.3% | 2.8% | 0.8% | |
| TeenPop / RnB slot-week share (pt) | +1.4 / −1.0 | +0.7 / −0.5 | +1.5 / −2.0 | +1.1 / −1.0 | +1.2 / −1.1 |
| Closures | 113 → 146 | 115 → 93 | 215 → 208 | 208 → 250 | +11 |

**What is now settled.**
- **Albums and singles are neutral in the four-seed mean.** The means are within ±1.2% every year, with positive and
  negative effects across seeds. Seed 4004's album cells are all negative; neutrality is a mean result. The LP fixes hold.
- **Repeat hits are the phase's real effect.** They are up on every seed, closing about half the gap to history by
  1966. Consecutive runs rise on every seed (roughly 2.6-5 times as many acts with 5+ straight hits).
- **Concentration is real and trips the kill rule.** It trips on three seeds of four, mean +3.04 pt. It isn't
  reshuffle. Yet the top 5%'s share of Top 40 entries stays below history on every seed (18.9-21.2% against 24%). The
  rule and §9's historical target now disagree. That is the §0.4 decision the design left to the author.
- **TeenPop gains and RnB loses on all four seeds** (about 1 pt each). That's consistent with followings being young.

**Major share, on four seeds.**
- The headline family share rises on every seed in 1964 and 1966 (mean +1.4 to +5.6 pt).
- Promotions are up on three seeds of four, and so are demotions, so more Independent ↔ MidTier churn is now the likelier
  reading.
- The labels promoted only in the treatment still hold 1-3% of fan units. So it's the churn, not fans buying from those
  labels.
- The Major tier alone rises modestly on three seeds (mean +0.9 to +2.2 pt; seed 2002 falls). That matches fans'
  extra charting records landing mostly at Majors, whose rosters hold more followed acts.
- On control tiers, family share is down on every seed in 1966. That row is biased low by promotion amplification
  (see the trace above).

Net: fans give the Majors a small real gain, about 1-2 pt. The rest of the headline rise comes from MidTier churn.

## Author decision and follow-up investigation (2026-11-01)

The author accepted the current Phase 4 calibration for the next stage and explicitly amended adoption to historical
calibration. **The original +3 pt top-10% slot-week kill rule failed** (three seeds of four, mean +3.04 pt); it has not
been passed or dismissed as noise. Slot-week concentration remains a separate diagnostic, because a low share of
distinct hit entries does not establish that chart occupancy is healthy.

Before Phase 5, investigate whether repeat hits and leading acts' entry share can move closer to history. First audit
the measurement: the original report dates entries from closed lifecycles, omits records still active at the endpoint,
and compares a four-year simulated entry concentration with a three-year historical concentration. The optional
`--chart-history` audit switch retains the opening chart and every weekly Top 100 in `records.csv`, including under
`--calibration`; it changes telemetry only. `analyze_follower_calibration.py` uses first actual Top 40 observations,
an actual prior-104-week Top 40 lookback, and equal three-year concentration windows. Opening carryovers seed the
lookback but do not count as new entries. Historical artist credits remain as printed, without A/B-side merging.

Recomputing the recovered history with that definition gives repeat shares of 62.9%, 64.1%, and 64.7% in 1962-64,
and top-5% entry concentration of 23.8% across those years. Model replay and any candidate screen must use complete
weekly chart observations before selecting a new calibration. The author will do the player-residency playtest
before the final branch decade; that validation is still pending.

### Measurement audit and sensitivity test

The first two complete chart replays (`fd4audit-base1001`, `fd4audit-base2002`) reproduce the original annual singles
and LP units and label concentration. Calibration telemetry omits some diagnostic aggregates (for example compilation
freshness); those omissions are not economic changes. The corrected 1966 repeat shares are 58.8% and 47.8%, rather
than the original lifecycle report's 50.5% and 47.4%. Equal three-year entry concentration (1964-66) is 19.0% and 21.4%,
against history's 23.8%. Actual top-10% Top 100 slot-week shares are 43.0% and 41.2%, against history's 42.9%.
Mean observed Top 40 occupancy per title in that window is 8.1 and 7.8 weeks, against history's 6.8. There is room in
entry concentration, but indiscriminately lengthening hits would move another measure away from history.

The retrospective leading 5% of hit acts have 6.4 and 6.9 Top 40 entries each over three years, against history's 10.5.
Their mature 1966 follow-up launches already reach the Top 40 within 13 weeks about 95% of the time (only launches
with a full 13-week outcome window are counted). Stronger demand has little direct room to create more hits for these
leaders. Release frequency and audience warm-up therefore need to be distinguished from failed chart conversion.
Leader selection is descriptive and uses the whole window, not a matched causal cohort.

The resume worlds' saved flags contain no `--observe-following`: following starts at zero in 1963. A final branch run
with the stock active from 1960 will have a different warm-up. Do not force these cold resumes to the historical
benchmark by assuming the 1966 state is an equilibrium.

An experimental `--follower-single-scale=1.5` tests the demand hypothesis without changing LP rates, follower growth
constants, or AI scheduling. It changes the singles rates from 0.35 / 0.10 to 0.525 / 0.15 and scales launch stock with
the book. The parameter accepts finite values from 0 to 2, requires follower demand or its shadow, and defaults to 1:
**the accepted calibration is unchanged**. Shadow release telemetry uses the same rate. The fixture suite passes
282 checks, including singles scaling with invariant LP intent; the Python measurement regressions pass 4 checks.

Use `analyze_follower_calibration.py RUN... --pair BASE TREATMENT` for complete run comparisons. Outputs are in
`SimLogs/follower-calibration`: annual entry shares, three-year concentration, fixed follow-up windows, mature launch
book outcomes, retrospective leader releases, and paired annual economics. `granted` in the original follower ledger
means admitted through the physical limit **before purse clearing**; it is not an attribution of settled fan purchases.

### Complete chart audit: four seeds

All four `fd4audit-base<seed>` replays complete 209 weeks and reproduce the corresponding `fd4sp-t<seed>` annual
sales (units and gross, both formats) and all label-concentration columns exactly. Seed 1001's original tag is
`fd4sp-t`, without the suffix. Each new history contains the opening 100 records and exactly 100 unique chart positions
and titles per completed week. The three-year comparison is 1964-66 in the model and 1962-64 in the recovered history;
it is a period reference, not a claim to have observed 1966 history.

| Complete weekly chart measure | 1001 | 2002 | 3003 | 4004 | Seed mean | History |
|---|---:|---:|---:|---:|---:|---:|
| Repeat-hit share, 1966 | 58.8% | 47.8% | 49.2% | 54.5% | 52.6% | 62.9-64.7% |
| Top 5% of hit acts' share of entries, three years | 19.0% | 21.4% | 20.5% | 19.5% | 20.1% | 23.8% |
| Top 10% of charting acts' Top 100 slot-weeks, three years | 43.0% | 41.2% | 41.8% | 41.3% | 41.8% | 42.9% |
| Observed Top 40 weeks per title in the three-year window | 8.11 | 7.80 | 8.22 | 8.13 | 8.07 | 6.81 |

The remaining gap is distinct hits by established acts, with only about one point of mean headroom in top-10% slot-week
concentration relative to this historical period. The model averages 743.5 new Top 40 titles across 457.5 hit acts in
the window; history has 879 titles across 391 acts. More acts share fewer hits. These absolute measures do not
recompute the original Phase 4 versus no-follower treatment effects: the old no-follower controls have no complete
weekly trace. The old kill failure remains recorded on its original method.

The existing `--hit-act-cadence` mechanism is a separate scheduling sensitivity: due recent hit acts take the label's
release opportunity ahead of the general lottery, and a lane release is a single. It also applies the existing
same-act radio-sharing/predecessor-cut behavior. Capacity is unchanged, but the format mix can change, so LP sales
must be checked. This is not a Phase 5 stature implementation and does not enable `--hit-act-retention`.

### Completed candidate screens and disposition

Ten 209-week worlds completed: four baseline replays, four `fd4audit-high<seed>` runs at singles scale 1.5, and two
`fd4audit-cad<seed>` runs (1001/2002) at the accepted scale 1 plus `--hit-act-cadence`. All processes exit 0, with empty
error logs. No decade run or player playtest was performed. These use the original media-screen flag bundle, not the
ordinary-game `--default-simulation-profile` (which already includes other release and audience mechanisms).

| Measure | Baseline, four seeds | Singles scale 1.5, four seeds | Baseline, seeds 1001/2002 | Cadence, seeds 1001/2002 | History |
|---|---:|---:|---:|---:|---:|
| Repeat-hit share 1966 | 52.6% | 57.4% | 53.3% | 61.4% | 62.9-64.7% |
| Top 5% share of distinct entries, three years | 20.1% | 19.9% | 20.2% | 26.1% | 23.8% |
| Top 10% Top 100 slot-weeks, three years | 41.8% | 42.7% | 42.1% | 47.2% | 42.9% |
| Observed Top 40 weeks per title, three-year window | 8.07 | 7.67 | 7.96 | 7.33 | 6.81 |

**Stronger singles demand is not adopted.** The repeat-share changes by seed are −1.5, +11.0, +5.9, +4.1 pt. Mean
repeat hits and turnover improve, but top-5% entry concentration does not. Against the original no-follower controls,
four-seed album-unit changes in 1963/64/65/66 are +0.7/+1.0/+1.0/+3.3%, and singles −0.2/−0.4/−0.4/−1.3%. In 1966
LPs are +2.8% against the accepted Phase 4 baseline itself. LP rates were unchanged; this screen does not isolate the
feedback/stock route responsible for that drift. It does establish that increasing the singles rate fails to retain
the accepted unit neutrality and does not solve the leading-acts entry-share gap. The new parameter remains opt-in
at its screened experimental value; the default is still 1.

**The blanket cadence bundle is not adopted for this media screen.** It raises distinct hit concentration beyond the
historical reference and raises slot-week concentration to 46.0-48.4%. Against the matched Phase 4 baselines, mean
LP-unit changes in 1963/64/65/66 are −3.2/−20.1/−22.9/−23.5%; singles +1.1/+7.6/+8.5/+10.0%. The single-for-LP
override is a direct format-mix change, so the chart gains are economically expensive. This verdict concerns adding
the flag to these resumes, not its use in the separately calibrated ordinary-game release bundle.

**Next implementation target.** Keep the accepted Phase 4 demand calibration. The evidence supports investigating
release selection and timing within a format programme: followed acts should get timely singles without taking away
their planned LPs or creating extra label capacity. Phase 5's stature-to-AI work is the natural place to implement and
screen that allocation, with explicit observations of eligible acts waiting for a release opportunity. Mapping stature
only to budgets or minimum cooldowns would leave the selection lottery unresolved. A broad fan-rate increase should
not be used to compensate for missing AI readers or the resumes' zero-following warm-up.

The adoption amendment stands: historical calibration governs; the original kill rule failed. The Phase 4 default
and the ordinary-game profile have not been changed by these trials. Final branch decade validation and the author's
player-residency playtest remain pending.

**Reproduction.** The common audit flags are `--enable-genre-market-v2 --enable-artist-population-lifecycle
--causal-media-atmosphere --media-recognition --fame-registration --observe-following --media-residencies
--follower-demand --calibration --chart-history`, `--weeks=209`, and the corresponding seed. Resumes are
`media-p3-1001-1963`, `asc-2002-1963`, `fdw-3003-1963`, and `fdw-4004-1963`. Add
`--follower-single-scale=1.5` for `high` runs, or `--hit-act-cadence` for the two `cad` runs. Baselines use scale 1.
The four baseline sales/concentration replays exactly match commit `4e2338a`'s original Phase 4 artifacts. The later
screen assembly SHA256 is `FA8AE651151AF2E558A7AB16994D3B79E163658D834A0E3898EF5410A9AC630A`.
`SimLogs/follower-calibration/final-report.txt`, `chart-pairs.csv`, `economic-pairs.csv`, and
`no-follower-economics.csv` hold the results; `sources.json` records the analyzed inputs and their SHA256 hashes.
