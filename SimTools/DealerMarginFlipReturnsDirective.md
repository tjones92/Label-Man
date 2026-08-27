# Directive: The Dealer Margin, the Flip, and Returns

**Branch:** `dealer-margin-and-flip` (off `publishing-player-mechanic`).

**Branch intent.** Three known holes in the player-side record business, all of them in the same
neighbourhood of the code (`PlayerDesk`'s sale bookkeeping and the `isPlayerOwned` fork of
`CompetitorManager.CalculateLabelRevenue`), and one of them -- the dealer margin -- is the largest
calibration debt in the player economy. Nothing here is a new system. Every part is a correction to
money and inventory that the game already moves.

- **Part A -- the dealer margin.** The player books retail as label gross. There is no wholesale
  spread anywhere in the trunk path, so a 45 nets the label about $0.87 against a real period figure
  near $0.25. This distorts trunk-versus-distributor, which is the central strategic choice of the
  game.
- **Part B -- the flip.** The B-side is assembled, paired, pressed, shipped, marked `Released`,
  removed from `masters`, and charged its own 2c mechanical -- and then it never charts, never sells,
  and can never break. The flip breaking instead of the plug side was a genuinely period-specific
  event and it is the cheapest missing piece on this list.
- **Part C -- returns.** Deliberately excluded, with a reasoned comment
  (`CompetitorManager.cs:921`) that is *correct about the thing it refuses to do* and silent about
  the thing returns actually were. Every house already carries a `returnAllowance` that nothing
  reads.

**Scope.** Player company only. The AI economy is frozen: `pricePerUnitByFormat`,
`pressingCostPerUnitByFormat`, `GetPricePerUnit`, `DeferWholesaleBillings`, the AI advance
double-pay (see `SimTools/ContractNegotiationDirective.md`), and every non-player branch of
`CalculateLabelRevenue` stay byte-for-byte identical. Proof obligation in section 6.

---

## 0. Ground truth -- what is actually in the code

Read this before proposing anything.

**Prices, today.**

| Constant | Value | Where | What it is used for |
|---|---|---|---|
| `PlayerDesk.SinglePrice` | `0.89` | `PlayerDesk.cs:3489` | Trunk sale gross (`BookSale`), hop-table sale, `HypeTheCount` buy-back cost, a UI "value given away" figure |
| `PlayerDesk.OneStopUnitPrice` | `0.58` | `PlayerDesk.cs:54` | Carton sale to a one-stop -- **the only wholesale price in the game** |
| `PlayerDesk.ArtistBuyInPrice` | `0.50` | `PlayerDesk.cs:3493` | An act buying its own run |
| `pricePerUnitByFormat[Single]` | `0.89` | `CompetitorManager.cs:32` | AI gross **and** the player's distributed units, via `GetPricePerUnit` |
| `PressVinylPerUnit + PressSleeveLabelPerUnit` | `0.25` | `PlayerDesk.cs:431-432` | What the player pays the plant per disc |

`ArtistBuyInPrice = 0.50` sits directly beneath `SinglePrice = 0.89` and is commented as
"a wholesale-grade cut" -- so the code already knows what wholesale looks like and already knows
`0.89` is not it. `OneStopUnitPrice = 0.58` knows it too. The trunk is the outlier.

**How a player unit books today.**

- **Trunk / shop / op / hop table** (`PlayerDesk.BookSale`, `PlayerDesk.cs:3378`):
  `gross = units * 0.89`, minus artist royalty on that gross, minus the runner's commission (if his
  stock), minus both sides' 2c mechanicals. Net to the label: **~$0.805/unit**, against a pressing
  cost of $0.25 already sunk. No dealer keeps anything. The shop that sells the record to a kid at
  89c remits 89c.
- **One-stop carton** (`SellCartonToOneStop`, `PlayerDesk.cs:2459`): `gross = units * 0.58`, same
  royalty/mechanical treatment, COD then net terms. **This one is roughly right.**
- **Distributed units** (`CalculateLabelRevenue`, `CompetitorManager.cs:964`): retail gross
  `units * 0.89`, `pressingCost` forced to `0` for player records (correctly -- the player paid the
  plant up front), minus `marginSkim * grantedShare`, minus royalty, minus mechanicals. Net at a
  0.25 skim: **~$0.58/unit**.

**The B-side, today.** `AssembleSingle` (`PlayerDesk.cs:3212`) pairs two `Master`s into a
`PlannedRelease { Master, BSide }`. `FireRelease` (`PlayerDesk.cs:3497`) ships **only the A-side's
`Record`** through `ReleasePlayerRecord`; the B-side is marked `Released`, dropped from `masters`,
its `recordId` remembered in `shippedBSideRecordIds` for one repertoire line, and four song-control
fields snapshotted onto the A-side (`Record.cs:72-75`) so `MechanicalRoyaltyService` can charge the
flip its 2c. **Its `Record` object is then unreachable.** Its hook, production, danceability,
originality, genre, title and `songId` die with it. Nothing in `ChartSimulator`, `StationNetwork`,
`ChartManager` or the trade press has ever heard of it.

**Returns, today.** `IndependentDistributor.returnAllowance` is generated per house at 0.10-0.35
(`IndependentDistributorFactory.cs:115`), written to the audit CSV, asserted by one probe, and
**read by no economic path**. The refusal is at `CompetitorManager.cs:921`: the settlement bills on
units *sold*, so charging a return allowance against it would take the same loss twice. That
reasoning is sound. It is also not what returns were.

---

## 1. The history

### 1.1 The price ladder on a 45, early 1960s

A single carried a suggested list price of **98c** (discount chains advertised 89c; the label's
books did not care what the chain did). Down the ladder, as the trade quoted it:

| Who pays | Roughly | Share of list |
|---|---|---|
| The kid, at list | $0.98 | 100% |
| A dealer buying from a one-stop or distributor | ~$0.63 | ~64% |
| A jukebox operator, through a one-stop | ~$0.58-0.60 | ~60% |
| A one-stop buying from the distributor/label | ~$0.50-0.53 | ~52% |
| **The label, from its distributor** | **~$0.44-0.47** | **~46%** |

Against that ~45c receipt the label paid roughly **11-15c** to press (vinyl, label, plain sleeve,
in runs of 500-1,000), **4c** in statutory mechanicals on a two-sided disc, and **3-5c** in artist
royalty. **Label net on a distributed single: about 25c.** That is the number the whole indie
business ran on, and it is why the trunk existed: a box of 45s sold straight to a dealer at 63c
skipped the distributor's 18c entirely and roughly doubled the margin -- at the cost of gas, days,
consignment risk, and never reaching a market you did not personally drive to.

The pressing figure matters as much as the price figure. A custom plant's quote for 500 45s with
labels and sleeves ran to roughly **$130** all in, mastering and metal parts included.

### 1.2 The flip

Radio played what it wanted. A DJ who was sent a record and did not care for the plug side turned
it over, and if the flip pulled phones, the trade sheets said so, the counter reports followed, and
the label reversed the sides on the next pressing and re-serviced. "Rock Around the Clock" shipped
as the B-side of "Thirteen Women". "Hound Dog" and "Don't Be Cruel" both charted off one disc. "I
Saw Her Standing There" was the American flip of "I Want to Hold Your Hand". This was not a rare
freak event; it was a standing risk and a standing second chance on every 45 a label shipped, and
it is *the* mechanical reason a B-side selection was a real decision instead of filler.

Two properties matter for the model:

1. **A flip rescues; it rarely doubles.** The flip broke most often when the plug side stalled --
   a jock with nothing to lose turns the record over. A record already climbing was left alone.
2. **The unit is the disc, not the side.** A flipping record does not sell twice. The same piece
   of vinyl sells on a different reason. Any implementation that adds units for the second side is
   wrong.

### 1.3 Returns

Returns were not a haircut on sales. They were a **claim against shipments**, and they were the
distributor's weapon:

- A partial return privilege was standard (often ~10%, negotiated up, occasionally a 100%
  "guaranteed sale" on a record the label was pushing hard).
- Returns arrived **late** -- weeks or months after the money was spent -- and they arrived
  **against the invoice**. A distributor who owed the label for a record that died did not write a
  cheque; it sent the records back and cleared the debt.
- Dead consignment stock in a shop was the same thing at small scale: your money, in vinyl, on a
  shelf in a town you have not driven back to.

So returns belong to **shipments and receivables**, never to sell-through. The existing refusal at
`CompetitorManager.cs:921` is right that they cannot be charged against units sold; it is wrong to
conclude from that that they cannot be modelled at all.

---

## 2. Part A -- the dealer margin

### 2.1 The rule

**`SinglePrice` becomes a list price and stops being a receipt.** Every player sale books at the
price of the channel it went through, and only a sale to an actual human being at a table books at
list.

Introduce one player-side ladder next to the existing constants:

```csharp
// The period's 45 ladder (see SimTools/DealerMarginFlipReturnsDirective.md section 1.1). ListPrice is
// what the kid pays; every other line is what somebody in the trade pays the label. AI records keep
// CompetitorManager.pricePerUnitByFormat, which is a frozen calibration constant, not a price.
public const float ListPrice        = 0.98f;  // suggested list on a 45
public const float DealerPrice      = 0.63f;  // a shop buying direct from the label
public const float OperatorPrice    = 0.58f;  // a jukebox op, through the one-stop trade
public const float OneStopUnitPrice = 0.52f;  // was 0.58 -- a one-stop buys BELOW dealer, then marks up
public const float ArtistBuyInPrice = 0.50f;  // unchanged; sits just above what a distributor would pay
```

Channel routing, keyed off the `StopKind` that is already on every `PlayerStop`:

| Channel | Books at | Why |
|---|---|---|
| `StopKind.Venue` (hop table, church, dance) | `ListPrice` | No dealer in the room. The label *is* the retailer. |
| `StopKind.Shop` (consignment and firm) | `DealerPrice` | The shop's margin is the 35c it keeps. |
| `StopKind.Op` (jukebox operator) | `OperatorPrice` | Ops bought a hair under dealer. |
| `StopKind.OneStop` (carton) | `OneStopUnitPrice` | It has to resell to dealers and ops. |
| Artist buy-in | `ArtistBuyInPrice` | Unchanged. |
| Distributed units (`CalculateLabelRevenue`) | `DealerPrice`, then `marginSkim` | See 2.2. |

### 2.2 The distributor path needs no new constant

The settlement's job is to book what the **label** receives on a unit a dealer sold. Book the
player's gross at `DealerPrice` and let the existing `marginSkim` be the distributor's cut of it,
exactly as it already is:

```
0.63 dealer price x (1 - 0.28 skim) = 0.45 to the label
```

The generated `marginSkim` band is already 0.20-0.35 (`ChartAuditRunner.cs:986`, the
`IndependentDistributor` deals, `PlayerDeskPanel.cs:1736`), so applying it to dealer price lands the
label between **41c and 50c** -- the historical band, with no new number and no double count. This
is a **one-line fork inside the existing `isPlayerOwned` branch** at `CompetitorManager.cs:985`,
next to the `pressingCost = 0f` fork that is already there.

### 2.3 The pressing correction lands in the same commit

`PressVinylPerUnit = 0.22` + `PressSleeveLabelPerUnit = 0.03` puts a 500-run at
`38 + 20 + 125 = $183`, about 40% above the period quote. Today that is invisible, because the
player is also being paid 89c a disc. Correct one without the other and the early game inverts.

```csharp
public const float PressVinylPerUnit       = 0.12f;  // was 0.22
public const float PressSleeveLabelPerUnit = 0.02f;  // was 0.03
// PressLacquerSetup 38 + PressShipping 20 unchanged -> 500 discs for $128, the period quote.
```

**These two changes are ONE change.** Shipping the dealer margin without the pressing correction is
a ~50% cut to the player's early-game margin and a different game. Do not split them across
commits; do not measure them separately.

### 2.4 The royalty base

Today the act's royalty is a percentage of *whatever the label collected on that unit*: 4.45c on a
trunk sale, 2.9c on a one-stop carton, for the same record on the same day. Period contracts paid a
percentage of **suggested list**, conventionally on 90% of records sold (the "breakage" clause),
independent of channel.

```
royaltyPerUnit = artist.royaltyRate * ListPrice * RoyaltyBaseFraction   // 0.90f
```

At the default 5% that is a flat **4.41c/unit** everywhere -- within a tenth of a cent of today's
trunk figure, so the act's headline income barely moves while the *channel* distortion disappears.
Apply it in `BookSale`, `SellCartonToOneStop`, and the player fork of `CalculateLabelRevenue`.
Recoupment behaviour (`Mathf.Min(unrecoupedAdvance, accrued)`) is unchanged.

### 2.5 What this does to the player's economy

Per unit, 5% royalty, two-sided mechanical, 500-run amortisation folded into the pressing line, and
a 0.25 skim in **both** columns so the distributor line is a like-for-like comparison:

| | Today | After Part A |
|---|---|---|
| Trunk gross (shop) | 0.890 | 0.630 |
| Artist royalty | 0.045 | 0.044 |
| Mechanicals (2 sides) | 0.040 | 0.040 |
| Pressing (500-run, all in) | 0.366 | 0.256 |
| **Trunk contribution / unit** | **0.439** | **0.290** |
| Distributor gross to label | 0.668 | 0.473 |
| **Distributor contribution / unit** | **0.217** | **0.132** |
| Trunk : distributor ratio | 2.02x | 2.20x |
| **Break-even on a 500 run, trunk only** | **227 units** | **235 units** |

The headline: **the pressing correction very nearly pays for the dealer margin on the trunk path**
-- break-even on a first run moves by eight copies -- while **the distributor path takes the whole
correction**, down about 40%. That is precisely the debt being repaid, and it is the historically
correct shape: driving the boxes yourself was worth roughly twice the distributor, and the
distributor was worth about a dime a record.

Expect to re-look at, but do not pre-emptively tune: `PlantCreditDemandThreshold`, advance sizes in
the contract mini-game, `OverdraftMonths` / `MaxMonthsInTheRed`, and starting cash. Change none of
them in the same commit; measure first (section 6).

### 2.6 Phases

- **A1.** Add the ladder constants; rename `SinglePrice` -> `ListPrice` with the channel table
  above; add `ChannelPrice(StopKind)`. Route `BookSale` through it. `WorkTheHopTable` and the record
  hop (both `Venue`) resolve to `ListPrice`; `ProcessTrunkDay`'s per-lot sell-through resolves to
  the stop's own kind.
- **A2.** `SellCartonToOneStop` -> `OneStopUnitPrice = 0.52`.
- **A3.** The `isPlayerOwned` fork in `CalculateLabelRevenue`: gross at `DealerPrice`, skim
  unchanged. AI path untouched.
- **A4.** The pressing correction. Same commit as A1-A3.
- **A5.** The royalty base (`ListPrice * 0.90`), all three call sites.
- **A6.** UI: `PlayerDeskPanel.cs:1477` prices a promo giveaway at retail -- a promo copy costs the
  label its **pressing cost**, and the sales it forgoes are dealer-price sales. Show both, or show
  cost. The distribution readout should say what a channel pays per unit; the whole point of the
  branch is that the player can now *see* why the trunk is worth the drive.

---

## 3. Part B -- the flip

### 3.1 What has to survive the pressing

`FireRelease` already snapshots four B-side fields onto the surviving A-side `Record`. Extend that
snapshot to everything the sim reads off a record, so the flip is a complete second identity riding
on one disc:

```csharp
// Record.cs, next to the existing bSide* publishing block
[Export] public string bSideTitle;
[Export] public Genre  bSidePrimaryGenre;
[Export] public float  bSideHookStrength;
[Export] public float  bSideProductionQuality;
[Export] public float  bSideDanceability;
[Export] public float  bSideOriginality;
[Export] public bool   bSideIsPlugSide;   // true once the sides have been reversed
```

Everything a record's performance is computed from lives on `Record`
(`hookStrength`/`productionQuality` at `ChartManager.cs:770`, `:860`,
`CompetitorManager.cs:1781`, `PlayerDesk.cs:3343`), and `RecordRuntimeData` holds `baseRecord` by
reference, so **a flip is a field swap on the live `Record` and nothing downstream needs to know**.
Chart position, weeks on, peak and the regional book all belong to the disc and correctly carry
across.

### 3.2 The trigger

Weekly, player records only, while the record is alive (released, not retired). One roll per
record per week, in the same weekly pass that already reads servicing and advocacy:

```
flipPressure =
      w1 * max(0, bSideHook - plugHook)          // the flip is simply the better side
    + w2 * stallTerm                             // plug side serviced but going nowhere
    + w3 * servicedStationCount / panelSize      // somebody has to physically have the disc
    + w4 * genreFitDelta                         // the flip suits the station's format better
```

- **`stallTerm` is the load-bearing one.** It should be near zero for a record that is climbing and
  at its maximum for a record that has been serviced to real stations for 2-5 weeks and has not
  charted. That is both the history (1.2) and the gameplay: the flip is a second chance, not a
  bonus.
- **No servicing, no flip.** Gate hard on `IsServiced` -- the same invariant the promo directive
  already enforces ("nothing gets played that nobody has been sent").
- Cap it. A flip should be a **notable event, not a weekly coin toss**: target on the order of
  5-12% of released singles over a full run, concentrated in the ones that stalled.

### 3.3 The three outcomes

1. **Nothing.** The overwhelming default.
2. **Split action.** Both sides get play in different markets. Model as a bounded lift on the
   record's effective appeal (`max(plug, flip)` plus a small premium, capped), a trade-press line,
   and **no extra units beyond what that appeal earns**. Historically this was the good problem:
   two markets, one disc, and a decision about which side to push.
3. **The sides reverse.** Swap the plug and flip field sets on the `Record`, set
   `bSideIsPlugSide`, and let everything downstream re-read. Consequences that must be real:
   - The **title changes** on the chart, on the shop shelf, in the trades, and in every Rolodex log
     line written from here on. This is the moment the player feels it.
   - Existing **servicing and advocacy** rows carry over -- the jock has the disc; he just turned it
     over.
   - A **re-press decision**: the run in the office and on the shelves still has the old side
     plugged. Offer a re-press at repress cost (no lacquer fee --
     `PressingCost(qty, isRepress: true)` already does this) that reverses the printed labels, and
     let the player decline and live with it.

### 3.4 Player agency

The flip is not only weather. Two verbs, both cheap because the machinery exists:

- **Work the flip** (Rolodex): pitch the B-side to a jock the record is already serviced at. Writes
  a flip-specific advocacy row rather than the A-side's. Reuses `StationAdvocacyService` and the
  existing call loop wholesale.
- **Reverse the sides** (desk): the label's own decision, at the cost of a re-press and a
  re-service. The correct move once the trade sheets have already told you.

And one honest consequence: `AssembleSingle` picking a B-side becomes a **real** decision. A cheap
throwaway on the flip is a wasted second chance; a strong flip is a hedge that occasionally costs
you the A-side you believed in.

### 3.5 Invariants

1. **No new units.** The flip re-bases *why* a disc sells. It never multiplies demand. Any
   implementation where a split-action record outsells the sum of its parts is wrong.
2. **AI records are structurally untouched.** No AI path writes `bSideSongId` or any `bSide*` field,
   so every flip predicate is unreachable for an AI label -- the same guarantee
   `MechanicalRoyaltyService` already relies on. Assert it in the probe; do not merely assume it.
3. **Mechanicals do not change.** Both compositions were already charged on every copy sold, and
   that is correct whichever side the jock plays. A flip must not re-charge or double-charge.
4. **One flip per record.** A disc reversing sides twice is a bug, not a comeback.

---

## 4. Part C -- returns

Returns attach to **shipments and receivables**. Three channels, in ascending order of cost to
build. R1 is the one that earns its place.

### R1 -- dead consignment comes back (build this first)

A `ConsignmentLot` that has not sold in weeks sits in `stop.OnHand` forever and nothing ever asks
for it back. That is the player's capital, stranded, invisible.

- After a dead interval (no units off that lot in ~6-8 weeks, relationship-gated -- a good account
  is patient, a cold one is not), the shop wants it gone: an `InboundCall` reason
  (`ReturnRequest`), or a line on the next visit.
- Accepting returns the units to `PressStock.Remaining`, less a small scuff/sleeve loss (~5%)
  written off as damaged goods.
- **No money moves.** Nothing was ever booked on unsold consignment stock; that is the whole point
  of consignment and the reason this is nearly free to implement.
- Refusing costs relationship, and the shop will not take a fresh lot until it is cleared.

This is the strongest of the three for gameplay: it makes over-placing a record a real mistake, and
it makes the drive back to Dayton about recovering your own vinyl and not only collecting cash.

### R2 -- one-stop carton returns

`SellCartonToOneStop` is a **firm** sale. Period one-stops bought firm and still expected a return
privilege on a record that died.

- Grant the one-stop a return privilege of `returnAllowance` (reuse the house band, 0.10-0.35) on
  the carton, exercisable within a window (~8-12 weeks).
- On exercise: units back into inventory (less scuff), and the credit lands **against the
  outstanding `WholesaleReceivable` first** and only then as cash -- the historical behaviour, and
  it reuses `Label.wholesaleReceivables` with no new ledger.
- Reverse gross, royalty (against `totalRoyaltyEarnings`, restoring `unrecoupedAdvance` to the
  extent it was recouped on those units) and the mechanical on the returned units.
- **Chart credit does not rewrite history.** Units already swept into a settled week stand -- the
  survey reported what it reported. Subtract returned units from the *current* week's
  `weeklyTrunkUnits` tally instead, floored at zero. State this asymmetry in the comment; without
  it, a carton sale plus an immediate return is a free chart hype.

### R3 -- the distributor's returns reserve (the real one)

This is what the comment at `CompetitorManager.cs:921` was reaching for and correctly refused to
implement as a haircut on sales. Model it as a **hold on cash, not a second charge on units**:

- When the player's billings are deferred (`DeferWholesaleBillings`), the house withholds a
  **returns reserve** of `returnAllowance * billed` alongside the existing `reportingHonesty`
  discount, booked as a separate reserve balance rather than a receivable.
- The reserve is **released** to the player, on the house's normal terms, if the record is still
  selling when the window closes; it is **forfeited** (write-off, via `lifetimeWholesaleWriteOffs`)
  if the record has died.
- No unit is charged twice: the billing is still on units sold, and the reserve is a timing and
  survival claim on money the label has earned but has not been given.
- **Player-only.** `DeferWholesaleBillings` is shared with AI labels; the reserve branch must be
  gated on `label.isPlayerOwned` and proven inert for AI (section 6).

Build order: **R1, then R3, then R2.** R2 has the most subtle chart interaction and the least
gameplay per line of code.

---

## 5. Adjacent inaccuracies found while in this code

Not part of the three headline items. Listed with a recommendation each; take them or log them, but
do not let them expand the branch.

1. **The artist buy-in owes mechanicals.** `ArtistBuyIn` (`PlayerDesk.cs:2588`) sells 50-100 copies
   for cash and charges no mechanical, while every other sale channel does. The publisher is owed
   2c per side per copy *sold*, and these are sold. One `MechanicalRoyaltyService.ChargeSide` call
   per side. **Fix in Part A.**
2. **`HypeTheCount` eats the full list price with no offsetting receipt.**
   (`PlayerDesk.ReportingOutlets.cs:117`.) The player pays retail through kids and cousins --
   correct -- but the copies came off the shop's consignment lot, so the shop owes the label its
   dealer price on them exactly as it would on a real sale. Today the label eats 89c a copy; it
   should eat the spread (~35c) and book a normal dealer-price sale. Under Part A this roughly
   halves the cost of the move, so **re-check the hype economics after the ladder lands**, not
   before.
3. **Royalty base is channel-dependent.** Covered as A5. Flagged separately because it is a
   *correctness* bug independent of the ladder: today the act is paid less for the same record
   because of how the label chose to move it.
4. **Pressing runs ~40% over the period quote.** Covered as A4.
5. **Jukebox-operator purchases enter the chart through the retail survey.** Ops take consignment
   lots through `ProcessTrunkDay` like a shop and their units land in `weeklyTrunkUnits`. In period,
   a record's jukebox exposure reached the chart through the **jukebox-play component**, not the
   store survey; an op's purchase is a wholesale buy that then generates *plays*. Low priority (the
   chart model is a survey abstraction and the units are real either way), but if `Op` ever gets its
   own chart contribution it belongs on the airplay side, not the sales side. **Log it.**
6. **`OneStopUnitPrice = 0.58` is a dealer price wearing a one-stop's name.** Corrected in A2 to
   0.52; a one-stop that pays 58c cannot resell to a dealer at 63c and survive.
7. **The `ArtistBuyInPrice` comment goes stale either way.** It currently justifies 0.50 as "well
   above the ~$0.37-0.40/disc pressing cost", which stops being true once pressing is corrected, and
   it describes 0.50 as "a wholesale-grade cut of the $0.89 trunk/retail price" while the shop next
   door pays the full 89c. Rewrite it against the ladder: the act pays a hair above what a
   distributor pays and well under a dealer.
8. **The AI advance double-pay stands.** (`CompetitorManager.cs:1014-1021`.) Known, documented,
   deliberately unfixed because the AI economy is calibrated against it. **Nothing in this branch
   touches it.** Re-stated here only so nobody "fixes" it while they are in the function for A3.

---

## 6. Verification

**The AI economy must be byte-identical.** Every change above is either inside an `isPlayerOwned`
fork or inside `PlayerDesk`, which no AI path calls.

1. **Probe-run byte comparison.** Run the canonical decade configuration before and after and hash
   the CSVs:
   ```
   Godot_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --run --weeks=520 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --calibration
   ```
   Identical hashes on every calibration CSV, or the change is not player-only. (Build `-c Debug`
   first; headless loads `bin/Debug`. Redirect to a file -- do not pipe a long run.)
2. **A player-economy probe**, in the pattern of `--mechanical-royalty-check`
   (`SaveLoadRoundTripRunner.cs:28`):
   - `--channel-pricing-check`: one unit sold through each of the five channels; assert the exact
     gross, royalty, mechanical and net against the table in 2.5, and assert that the royalty per
     unit is identical across all five.
   - `--flip-check`: assert the `bSide*` fields are populated at `FireRelease`, that a forced flip
     swaps the identity in place while preserving `currentPosition` / `weeksOnChart` /
     `peakPosition`, that a record can flip at most once, and that **no AI record in the run has a
     non-empty `bSideSongId`** (the structural inertness claim).
   - `--returns-check`: R1 returns units without moving money; R2 credits against the receivable
     before cash and never drives the current week's trunk tally negative; R3's reserve is released
     or forfeited exactly once and never touches a non-player label.

   Make the fixtures **relational** -- anchor them to the constants, not to hard-coded cents, or the
   next tuning pass inverts them silently.
3. **Save round-trip.** Every new field (`bSide*` musical block, `bSideIsPlugSide`, lot dead-age,
   reserve balances, return windows) must round-trip. `SaveLoadRoundTripRunner` proves this by
   byte-identity; a field captured but not restored shows up as a diff.
4. **A real player run.** `--inspect-slot=NAME` against a save carried through a first release, a
   first carton, a first distributed week and a stalled record, because none of this is visible from
   an AI-only run.

## 7. Invariants

1. **The AI economy does not move.** Not one number, not one branch. Proven by hash, not by reading.
2. **Retail is what a person pays.** No trade counterparty in the game ever remits list.
3. **No channel change invents or destroys a unit.** Part A moves money only.
4. **The flip never adds demand** (3.5.1).
5. **Returns never charge a unit twice** -- they are claims on shipments and cash, never a haircut
   on sell-through. That is the entire content of the refusal at `CompetitorManager.cs:921`, and it
   survives this branch intact.
6. **The dealer margin and the pressing correction ship together** (2.3).

## 8. Order of work

1. Part A, A1-A5 in one commit, plus item 5.1 (buy-in mechanicals). Measure. Hash the AI run.
2. Part A6 (UI) and the economy re-look from 2.5 -- separately, with numbers in hand.
3. Part B, the flip: snapshot -> trigger -> outcomes -> agency, in that order. The snapshot alone is
   inert and safe to land first.
4. Part C, R1. Then R3. Then R2 if it still looks worth it.
