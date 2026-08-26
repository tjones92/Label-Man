# Directive: Player-Facing Distribution Expansion

**Branch intent.** Deepen how the *human player* sells records — turn the existing trunk-and-house
loop into a felt 1960s independent-distribution career: named local accounts, inbound reorder calls,
commission muscle, wholesale channels, and the cash-timing trap that makes a hit dangerous.

**Scope.** Player company only. Do **not** retune, branch, or extend the AI label economy,
`IndependentDistributorFactory` constants, AI offer gates, AI `DistributionDeal` resolution, chart
attribution, `ChartSimulator` one-stop / rack-jobber math, or market-share math. Every mechanic here
is a *player-only surface*, in the spirit of `AILabel.isPlayerOwned` overrides already in the code
(`GetMonthlyOverhead`, `PlacePlayerLine`, `WorkThisTown`, the player receivable path).

---

## 0. Ground truth — what already exists (read before proposing anything)

The original sketch was written against a handful of scripts and assumes Stage 1 is greenfield. It is
not. **Most of the opening game is already built.** This directive is a set of *additive layers* on
top of a working loop, not a rebuild. Before writing code, know what you're standing on:

**Already implemented (`Systems/PlayerDesk.cs` unless noted):**

- **Founding & solvency.** `FoundingCapital = 800`, roster cap 6, home-office overhead a flat
  `PlayerHomeOfficeOverhead = 75/mo` (`AILabel.GetMonthlyOverhead`, player branch). Founding
  archetype + `ExecutiveInstinctProfile` (`FoundingArchetype`, gates Rolodex). **A credit / failure
  model exists:** `CreditFloor = -3 × monthly overhead` (≈ **−$225** today), and the run ends after
  `MaxMonthsInTheRed = 3` consecutive months below zero (`IsGameOver`, `MonthsOfGraceLeft`).
- **Trunk selling is NOT "leave copies around town."** `WorkThisTown(recordId, quantity)` places a
  **per-town consignment lot** (`ConsignmentLot`: Remaining / Placed / DaysSinceRestock). `ProcessTrunkDay`
  sells a daily slice per town — `appeal = hook·0.45 + production·0.25 + awareness·0.30`, decayed by
  `TrunkDecayPerDay = 0.90^daysSinceRestock`, rolled by luck. Present in town → **cash in hand**;
  away → the shops **hold your cut** (`consignmentOwed`) with a thin daily wire (`WireOwedTrickle`)
  and a lump `CollectFromTown` on return. Trunk units chart (`TakeWeeklyTrunkUnits`) and are booked
  into the weekly settlement.
- **The road.** `DriveTo` / `DriveHome` / `Travel`, real road miles (`DistanceModel.GetRoadMilesBetween`),
  `DriveMph = 64`, `GasPerMile = 0.02` round-trip, reach limited to same region + adjacent regions
  (`CanReach`), `HotelNightly = 9` when away, office/studio locked while on the road (`RequireHome`).
- **Pressing pipeline.** `OrderPressing`, `PressMinimumOrder = 500`, cost ≈ **$183 / 500** (`PressingCost`:
  `$38 lacquer + $20 ship + qty·$0.25`), realistic plant turnaround (mail → plating → queue → ship,
  with seasonal backlog), `PressStock` office inventory, `DeliverArrivedPressings`.
- **Wholesale house distribution.** `PlaceLine(regionId)` → `CompetitorManager.PlacePlayerLine(Label, regionId)`
  attaches the player line to a real `IndependentDistributor` (`AddClient`), seeds `unitsInStores` so the
  regional store engine sells it, and books `Label.wholesaleReceivables` (`WholesaleReceivable`: DueWeek /
  DistributorId / Amount) settled on `paymentTermWeeks` with `reportingHonesty` / `returnAllowance` /
  `reliability`. `GetPlaceableMarkets`, `HasDistributionInRegion`.
- **Radio / DJ / payola.** The whole Rolodex system (`PlayerDesk.Rolodex.cs`, `RolodexEntry`,
  `RolodexCall`, `PayolaLedger`, rapport vs. record-specific advocacy) already delivers the airplay
  half. **Do not duplicate it** — inbound demand (§4) should *read* it, not reinvent it.
- **Material.** Covers rehearsed over days, commissioning ("see it, then cut it", `CommissionFee = 150`,
  7-day delivery), the standards-mix lever (`SongMaterialSelectionService.StandardShareFactor`).
- **One-stops and rack jobbers already exist AI-side** in `ChartSimulator` (`GetRackJobberAccess`,
  `GetRackJobberEraWeight`, `GetRackJobberShelfMultiplier`; one-stop weekly reorder in the store engine)
  and in the authored data (`DistributionNetwork.hasOneStopDistributors`, `.departmentStoreCount`).
  The player-facing versions in §6 are **new surfaces over the same fiction** — they must not touch
  those functions or their calibration.

**Genuinely NOT built (this is the actual work):**

1. **Named individual accounts.** Today the finest grain is the *city* (one consignment lot per
   record per city). There is no shop / op / jock / one-stop as a person-shaped account with its own
   relationship. (§3)
2. **Inbound demand — "they called me."** Trunk is 100% player-push; nothing phones the office when a
   record is moving. This is the seam the branch owner cares about most, and the fix for the
   "200 units and dying" failure. (§4)
3. **A proof gate on the player's house line.** `PlacePlayerLine` has **no evidence requirement** —
   `provenInRegion` is hardcoded `false`, so the player can place a line in any region with an open
   house on day one. The AI path gates on `GetProvenBreakoutRegions` / `regionalBreakoutDealThreshold`;
   the player bypasses it. The catch-22 the sketch describes is real, unbuilt work. (§5)
4. **One-stop / rack as *player counterparties*** distinct from the house line. (§6)
5. **The people layer** — commission runner, project promo, answering service, secretary. `ManagerSystem`
   is about *artist* managers, not label staff; there is no existing home for this. (§7)
6. **The first-hit squeeze as a designed beat.** The parts exist (receivables, terms, reliability,
   plant-paid-up-front); the drama (factor / lease / P&D / starve) is not surfaced. (§8)

---

## 1. Design law

Every stage is the same verb with more leverage: **get records onto shelves and into ears.** What
changes is who drives, who pays you, how long you wait, and how much of the record you still own.

**Five invariants.**

1. **The player pays the plant** until a P&D deal or a master lease/sale says otherwise. One-stops and
   houses are *customers*, not patrons. (Already true: `PressingCost` is charged up front; the house
   line only sells, it never funds.)
2. **Cash speed gets worse as volume gets better.** COD trunk (`present` sale) → shops-hold-your-cut
   (`consignmentOwed`) → one-stop paper → 12–18 week house paper (`paymentTermWeeks`) → rack returns
   *after* you already spent the money. The receivable clock already exists; make it bite.
3. **Proof is geographic.** Pittsburgh units do not impress a Dallas house. This is exactly
   `RegionalRecordData.peakBreakoutScore` per `regionId` — use it (§5), don't invent a national bullet.
4. **A valid run can stay regional through 1969.** The decade *punishes* that choice as ops and
   mom-and-pops shrink; the skill tree must not forbid it. Drive availability off year + authored
   flags, never a locked ladder.
5. **You should not go bankrupt in the first few months absent radical error.** *(New — the branch
   owner's explicit constraint.)* The failure model already carries the player: −$225 credit floor,
   3 months of grace. The job here is to keep the *early* economy inside that envelope. Concretely:
   - A cold shop taking 0–5 copies, a stiff record, one over-pressed run — none of those alone should
     cross the floor. The floor is crossed by *stacking* bad calls (over-press + salaried hire before
     paper + advances you can't recoup), which is the intended lesson, not a rng death.
   - **No new fixed weekly cost may be introduced at a tier the player can reach before the channel it
     serves has paid.** A $35–55/week secretary is ≈ $150–220/mo — two-to-three times the entire
     current home-office overhead, and it alone blows the −$225 floor. That is *correct*: it makes the
     secretary a mid-game object (§7), and the credit model already enforces it. Honor it; don't
     soften the floor to allow early salaries.

---

## 2. Player channel vocabulary

One enum for every sale. UI, ledgers, and the conflict rule all speak it. Map each to what exists:

| Channel | What it is | Codebase status |
|---|---|---|
| `DirectTrunk` | You (or a runner) standing in the shop / at the hop | **Built** — `WorkThisTown` / `ConsignmentLot` |
| `StoreAccount` | A standing dealer, sometimes net-30 | New — a `PlayerStop` of kind Shop with terms (§3) |
| `JukeboxOp` | Operator pull, a route of machines | New — `PlayerStop` kind Op; the first big carton (§3) |
| `OneStop` | Metro wholesale, first real carton, faster than a house | New player counterparty (§6) |
| `RackJobber` | Chain/discount racks, later decade, return bomb | New player counterparty (§6) |
| `IndieHouse` | Existing `IndependentDistributor`, one `regionId` | **Built** — `PlaceLine`; §5 adds the proof gate |
| `MajorPD` | Existing `DistributionDeal` — late, separate surface | Exists AI-side; player front door only (§9) |

**Conflict rule.** When an `IndieHouse` carries the player in a `regionId` (`Label.independentDistributionRegions`),
new `DirectTrunk` / `StoreAccount` pitches in that region are politically closed, or cost a real
relationship hit with the house. Radio/promo stay the player's. Direct-to-one-stop after a house is on
the line is a *visible* gray-area choice, not a silent multiplier — the house can bury you (drop the
line, dump stock) for it. Store this as a **player-contract-row modifier**, never a flag bolted onto
the shared `IndependentDistributor` (see the shared-house caution, §10).

---

## 3. The named-stop layer (deepening Stage 1)

**Do not abolish `WorkThisTown` — refine its grain.** Today a city is one lot; make a city a small,
legible set of *named accounts* the lot is composed of. This is the single biggest feel upgrade and it
reuses the existing sell-through math per account instead of per city.

### 3.1 Derive stops from authored data (player-only, dedicated seed stream)

For each `MarketCity` the player can reach, generate a stop list on an isolated seed namespace (follow
`IndependentDistributorFactory`'s isolation discipline — **do not edit that factory**). Correct data
sources (the sketch mislocated several):

| Stop kind | Source (verified) | Role |
|---|---|---|
| Record shop | `MarketCity.distribution.recordStoreCount`, `.distributionTier`, `.isRegionalHub` | Primary trunk target |
| Jukebox operator | `MarketRegion.media.jukeboxCount` + early-decade weight | First real volume spike |
| Disc jockey / station | `MarketRegion.media.hasTop40Stations / hasRnBStations / hasCountryStations / hasFMUnderground` | Airplay, **not** units — route through the Rolodex |
| One-stop counter | `MarketCity.distribution.hasOneStopDistributors` | Locked as a customer until inbound demand exists (§6) |
| Department / rack buyer | `MarketCity.distribution.departmentStoreCount` + year | Late, proof-gated (§6) |
| Hop / club / church table | genre + `MarketRegion.churchNetworkStrength` (default 0.25) + `MarketRegion.youthPercentage` + `media.concertVenueCount` | Retail-at-the-door; early soul/gospel/teen |

**Keep counts small and legible.** A 1960 hometown start ≈ 6–12 shops you can actually meet, 1–3 ops,
2–4 jocks, 0–1 one-stop; a hub has more. **Do not instantiate hundreds of shops because
`recordStoreCount` is 400** — that number is coverage math for the store engine, not a playable roster.
The player meets a representative named set; the rest of the city's retail is precisely what one-stops
and houses reach on the player's behalf. Names in the period idiom (family shop, "Record Mart", "the
one-stop in the back of the appliance store"). Stable IDs, persisted in `PlayerSaveData`.

### 3.2 Stop record (player-only)

```
PlayerStop
  stopId, displayName
  cityId, regionId
  kind            // Shop, Op, Jock, OneStop, Rack, Venue
  relationship    // 0–1, slow to earn, slower to repair — ELIGIBILITY, not a damage stat
  lastVisitWeek
  onHand[recordId]        // replaces/refines the city-level ConsignmentLot
  standingPull            // optional weekly/biweekly expected copies (accounts only)
  terms                   // COD default; net-30 is a reward
  openBalance             // this stop's slice of consignmentOwed
  willCall                // eligible to generate an InboundCall (§4)
  notes / lastRumor
```

Jocks hold **servicing** (promo copies, familiarity, spins via the Rolodex), never sellable stock. Ops
hold boxes and a route. Migrate the existing per-city `ConsignmentLot` / `consignmentOwed` into per-stop
`onHand` / `openBalance`; the daily sell-through (`appeal · decay · luck`) runs per stop, so a hot shop
and a dead one in the same town no longer move in lockstep.

### 3.3 The verbs at a stop

A call day is still spent in **one** `MarketCity` (drive range from last night's stop). The day is a
handful of stops, not a town-wide sprinkle. At a shop or op:

| Verb | Effect |
|---|---|
| Pitch / sell | They take N copies COD (or refuse). Relationship tick. `onHand` rises. |
| Consign | They take N, pay when sold. Worse cash, callback-eligible (`willCall`). |
| Service | Replace sold stock, collect `openBalance`, take a reorder. The standing-account verb. |
| Leave a DJ copy | Jock only. No units — routes to the Rolodex (a cue, a rumor, a request). |
| Work the hop table | Venue. Retail cash (~list), tiny volume, story. |

**Refusal is common and correct.** A cold shop's first visit is 0–5 copies, or "leave three on
consignment, don't come back unless it plays." **Jukebox ops are the first handshake that moves 20–40
copies** — weight them hard 1960–63; one op order should match a week of shop-by-shop nickels.
**Artist buy-in is a first-class verb:** the act takes 50–100 at a discount, cash now — a one-stop with
legs. (Artists already exist on the roster; this is a new action, not a new type.)

---

## 4. Inbound demand — "they called me" (the seam)

This is the heart of the branch. Today, a record that is "a little bit on the radio" sells only to the
three shops the player can physically reach, then dies — the classic 200-units failure. Fix it by
letting **the world phone the office** when a title has local demand above what's on counters.

### 4.1 Drive intensity off real regional velocity, not a national chart bullet

The signal already exists: `RecordRuntimeData.regionalData[regionId]` carries `unitsInStores`,
`peakBreakoutScore`, and week-over-week movement, and the Rolodex/airplay system knows spins per market.
Read those; do **not** author a parallel "buzz meter." If you ever surface one number, it must be a
composed, inspectable sum of {regional velocity, weeks of airplay, unfilled demand, trade mention}.

| Local state | What the phone does |
|---|---|
| Vanity / no airplay | Silence. You go to them. |
| Light rotation, stocked badly | A couple of sold-out calls from shops you know **plus at least one new-name request** — so the player *sees* the missed demand instead of a flat 200-unit stall. |
| In the shops listeners use | Standing pulls rise; ops bump; a stranger shop calls; a one-stop test carton (§6 unlock ping). |
| Local turntable hit | The office is a problem: multiple cities, press-to-fill, someone offers to take it off your hands, a house may court (§5). |

### 4.2 The call is an object, not a toast

```
InboundCall
  week, stopId, recordId
  requestedQty
  reason        // SoldOut, Requests, StationAdded, OpRoute, OneStopTest, AdjacentCity, HouseInterest
  expiresWeek   // they fill it from someone else, or forget
  termsHint     // COD / net-30 / consignment
```

Call sources, in the order they should appear as a record heats up: (1) shops you already stocked, low
`onHand`, demand still there; (2) ops on a `standingPull`; (3) a shop you've **never visited**, only if
airplay + regional velocity are real — the request loop; (4) the one-stop test carton (the §6 unlock,
not an automatic deal); (5) an adjacent-city shop/op — the first hint the record wants a second day of
driving; (6) a house — late, `DealOrigin.DistributorCourted` flavor, never on a stiff.

The player answers with **stock and a body** (drive it) or a bus shipment once that's unlocked. An
unanswered call decays relationship and burns proof (`expiresWeek`). An answered call is the
highest-trust relationship tick in the game.

### 4.3 Missed-call pressure is the honest argument for staff

If the player is on the road all week, they **miss** calls — do not solve this with omniscient
voicemail on day one. Missing the one-stop's first carton because you were in Toledo is the lesson and
the first real reason to buy an **answering service** ($5–10/mo — trivial against the $75 overhead, so
it's an early, affordable unlock) and, much later, a secretary (§7).

---

## 5. The house line — add the proof gate (fixing `PlacePlayerLine`)

`PlaceLine` works, but `PlacePlayerLine` currently takes any open house with no evidence. Make the
catch-22 real, using the machinery the AI already uses.

### 5.1 The gate

Before a house takes the player's line in a `regionId`, require the **same regional breakout evidence
the AI path demands**: a player record with `regionalData[regionId].peakBreakoutScore >=
regionalBreakoutDealThreshold` (see `CompetitorManager.GetProvenBreakoutRegions`). Pull vs. push,
mirroring `DealOrigin`:

- **Below threshold** → the player may still *visit the warehouse* (`LabelSought`): an easy no, or a
  yes on worse terms (thinner skim to you, COD not net-30, back-of-the-pile priority).
- **At/above threshold** → the house *courts* (`DistributorCourted`): better terms, and a genuine
  local breakout can draw more than one house call in a month.

Keep the existing side effects (`AddClient`, `independentDistributionRegions`, `unitsInStores` seed,
`WholesaleReceivable` on `paymentTermWeeks`) — just stop granting them unconditionally, and set
`provenInRegion` truthfully in the telemetry instead of a hardcoded `false`.

### 5.2 The folder (diegetic, inspectable)

What the player brings to the warehouse visit, all from data that already exists: regional units vs.
that region's buying population (`MarketRegion.GetRecordBuyingPopulation`), which stations and how many
weeks (Rolodex), the `InboundCall` log as proof, and ability to supply (cash + plant — they won't get
stiffed on a hit they'd be creating). **Capacity is almost never the "no"** (`clientCapacity` is
generous by design, per `IndependentDistributor.cs`); the "no" is "I don't hear it" or "you can't press
it if I get it on."

### 5.3 Negotiation surface — four knobs, not a contract RPG

Price/skim (`marginSkim`), free goods, priority (hot pile vs. box in the back — a player-row modifier),
territorial exclusivity vs. keeping the one-stop. Reuse the `ContractTalk` mini-game shell from the
Rolodex/negotiation work rather than a new UI. **No advances here** — upfront cash is for master-lease /
`DistributionDeal` only.

---

## 6. One-stop and rack — player counterparties (new, but not new fiction)

Both already exist AI-side in `ChartSimulator`. Add *player-facing* counterparties that **do not touch**
`GetRackJobberAccess/EraWeight/ShelfMultiplier` or the store engine's one-stop reorder math.

**One-stop** — first wholesale, still intimate. Available where `distribution.hasOneStopDistributors`.
Historically one-stops grew up serving **jukebox operators** (one place to buy every label's 45s), so
the natural unlock is an op or dealer they already serve asking for the record → surfaced as an
`InboundCall(OneStopTest)` → then a warehouse visit. You sell a carton; they scatter it to shops/ops you
never meet (metro multiplier, not whole-region coverage). Terms: pay faster than a house (COD if you're
nobody, else net 30–45), modest returns, you still pay the plant. Feel: the first 50–200 that move at
once, the first customers you can't name. **Not an `IndependentDistributor`** — a lighter player-only
counterparty; remains available after a house (the house often feeds one-stops).

**Rack jobber** — late-decade, proof-gated on `departmentStoreCount` + year + a proven title. Volume,
ugly price, ugly returns, increasingly LP-oriented. Unknown trunkers do not get Woolworth. This is a
second-half-of-the-decade way to die on a hit (a return bomb after you've spent the money — invariant 2).
**Not an `IndependentDistributor`.**

---

## 7. People — contractors first, payroll later

A company with $800 and a salary is already dead (invariant 5, and the −$225 credit floor proves it).
First-year muscle eats what it kills.

- **The player (unpaid):** A&R, driving, DJ stops, shipping, books. The opening game as it stands.
- **Commission trunk runner** — *no weekly nut.* Unlock on persistent reorders in one city, or inbound
  calls in two cities the same week. Pay 8–15% of *his collections* (or a nickel a copy), paid when the
  shop pays. Assign a route (a list of `PlayerStop`s) + a carton; he runs the same sell/consign/service
  outcomes at a worse starting conversion that rises on *his* accounts, and can receive calls on his
  stops. Fire him by not handing him stock.
- **Project promo** — *not an employee.* $25–75 (or a point on the record) to work one city / a few
  stations for 1–2 weeks. Creates spins and rumors (feeds §4 through the Rolodex), sells no units.
  Payola-adjacent spend is a **risky project line item**, temperature from `media.payolaSusceptibility`
  — and note the 1959–60 payola scandal makes 1960 the *worst* year to be brazen: getting burned
  **freezes a market**, it is not a fun buff.
- **Answering service** — $5–10/mo, captures `InboundCall`s while you're on the road. The real first
  "secretary," and cheap enough to be an early buy against $75 overhead.
- **Secretary ($35–55/week)** — a **second-tier** hire, unlockable only when missed calls / shipping
  paperwork demonstrably cost more than she does. She logs calls, types packing slips, chases a late
  one-stop; she does not sell records and does not replace a runner. Her cost (~$150–220/mo) is designed
  to be unaffordable until receivables are flowing — do not undercut that.
- **Bookkeeper / stock driver** — appear with `WholesaleReceivable` volume and inventory leaving the
  sedan. Later still.

**Rule:** if a hire's pay is due before the channel it serves has paid you, it is a late object wearing
an early name. Out of scope to invent RPG classes for staff-promo/second-salesman/lawyer this pass.

---

## 8. The first-hit squeeze (make the existing clock bite)

The parts are in place: the plant is paid up front (`PressingCost`), houses pay on `paymentTermWeeks`
(90–120 days) via `WholesaleReceivable`, and `reliability < 1` means some money never arrives. Turn
that into a **designed beat**, not an accident:

Record breaks → house reorders hard → plant wants cash *this* week → house paper is 12–18 weeks out →
some never comes. The player's buttons: **starve the hit** (under-press and lose shelf), **factor the
paper** (sell the receivable at 70–85¢ now), **take a master-lease / P&D** (§9), or **die famous**.
If this squeeze can't happen, the receivable model is decoration. Surface the receivable clock and the
factor/lease buttons on the settlement screen where `Outstanding` already lives.

---

## 9. Late exits (surface the front door only)

These already have types AI-side; this pass needs player buttons, **not** an AI deal brain rewrite.

- **Lease / sell the master** — on the table once a station *and* a one-stop both know the title. An
  honorable success for an $800 company.
- **`DistributionDeal` P&D** — they press, an advance exists, `marginSkim` / `ownsMasters` / priority /
  `Dropped` / `Poached` / `Graduated` already designed. Tempting the week the receivable clock is about
  to kill you.
- **Own distribution arm** — one `regionId`, expensive, political, late; you become a house and
  established houses retaliate. Not a 1964 unlock.
- **Stay regional** — supported, stronger where indie + R&B infrastructure is authored
  (`hasIndieDistribution`, `hasRnBStations`, high `blackPopulation`).

---

## 10. Decade weighting — reuse the curves that exist

Same company, different country. Drive off year + authored flags; **do not change AI demand curves.**
Crucially, tie the social changes to the model's **existing** curves, not new multipliers:

- **Integration opening white retail/radio to black lines (1963–65 → peak 1967)** is already the
  `MarketRegion.IntegrationProgressCurve` (big steps 1964 / 1967). Gate which stops and stations *see*
  the player off `GetEraIntegration(year)` — a visible access change, not a hidden buff.
- **Singles → LP.** The model already has the LP mature by 1960 (`albumDemandRiseStartYear = 1957`) and
  ramping via `GetAlbumDemandEraProgress`. So "a 45-only trunk label is a hobby by 1969" is right, but
  the album market is *not* a late-decade surprise here — weight rack/LP availability off the era
  progress that's already computed.
- **Jukebox weight** is an explicit down-curve on `JukeboxOp` across the decade; ops crater by 1966–67.
- **`hasFMUnderground`** adds a new jock type where authored (1967+); it does not buff Top-40 promo.
- Office rent and rack temptations should come online in the years they historically did.

| Years | Player world |
|---|---|
| 1960–62 | Trunk, ops, black radio, mom-and-pops, one-stops, 45s. Racks barely care. |
| 1963–65 | First house makes sense; Top-40 and trade lists matter; pure trunking stops scaling; integration opens white retail per the curve. |
| 1966–67 | Racks and LPs; ops crater; returns abused; FM underground where authored. |
| 1968–69 | Album priority, chain retail, majors hunting; a 45-only trunk label is a hobby. |

---

## 11. Unlock order (doors that open, not chapters that close)

1. Named trunk stops + COD + the honest cold-shop refusal (§3, refines existing `WorkThisTown`).
2. First reorder / first op / artist box — relationships become assets.
3. **Inbound calls + missed-call pressure + answering service** (§4) — the core seam.
4. Commission runner and/or project promo (§7) — cost vs. coverage, still no salary.
5. One-stop will see you (§6) — first wholesale, still fast-ish cash.
6. Press-to-fill; then plant credit (a mid-game gun, not a tutorial crutch).
7. **First house pitch, only with regional proof** (§5) — 90–120-day paper, channel conflict.
8. Second region; receivables as a portfolio; the first-hit squeeze (§8).
9. Racks, year- and proof-gated (§6).
10. Master lease / major interest / own arm (§9).

A breakout is a shortcut into Stage-4 problems (supply, paper, circling buyers) while the company is
still a Stage-2 organism. That mismatch is the point — not a skip to the credits.

---

## 12. Implementation wall

**Do**
- New **player-only** types: `PlayerStop`, `InboundCall`, route assignments, commission ledgers,
  one-stop / rack counterparties, a player proof log. Persist all in `PlayerSaveData`
  (`SaveGameService.cs`) with the same From/To pattern the repertoire/rehearsal saves already use.
- Derive stop graphs from existing exports on a **dedicated seed namespace**, honoring
  `IndependentDistributorFactory`'s isolation without editing it.
- Migrate the city-level `ConsignmentLot` / `consignmentOwed` into per-`PlayerStop` `onHand` / `openBalance`.
- Add the **proof gate** to `PlacePlayerLine`; set `provenInRegion` truthfully.
- Reuse: `WholesaleReceivable` (already player-wired), `ContractTalk` for house negotiation, the Rolodex
  for all airplay, the credit/`IsGameOver` model for failure.
- UI (`UI/PlayerDeskPanel.cs`): city day-sheet, stop conversation, office call list, plant ticket,
  runner route, receivable clock.

**Do not**
- Edit AI deal logic, `IndependentDistributorFactory` house counts/capacities/reliability curves,
  chart/demand calibration, `ChartSimulator` rack/one-stop math, or genre/integration math "to help the
  player tutorial."
- Pay indie-house or one-stop sales as if they were trunk COD (respect `paymentTermWeeks`).
- Instant-cover a region because a house said yes to one title — coverage is `coveredRecordIds` (the
  breaking record plus what you release while the deal is live), exactly as `DistributionDeal` already
  models it.
- Give the office omniscience — if nobody is home, the `InboundCall` expires.
- Introduce any fixed weekly cost the player can reach before the channel it serves has paid
  (invariant 5); do not soften `CreditFloor` to allow it.
- Invent a mystic buzz meter (§4.1).

**Shared-house caution.** The player and AI labels may share an `IndependentDistributor`. Do not add
player-priority hacks that starve AI clients, and do not let AI logic start refusing the player because
a new flag was bolted onto `IndependentDistributor`. Attention/priority for the player is a
**player-facing modifier** (free goods, relationship, whether the title is moving) stored on the
player's contract row, never on the shared house object.
