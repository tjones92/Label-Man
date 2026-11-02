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
