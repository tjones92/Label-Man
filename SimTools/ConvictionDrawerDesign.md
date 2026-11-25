# The Conviction Drawer: design (inner-life directive §4, Phase 3)

Status: design for approval (2026-11-25). Nothing here is built. Every number marked *provisional* is a
placeholder until the phase that builds it measures it. Read with `SimTools/LabelManInnerLifeDirective.md` §0, §4,
§5, §6 and §10–11; this doc settles what §4 left open and doesn't repeat it.

## 0. The short version

- The Label Man keeps index cards in the desk drawer. A card is a **claim about the market** ("Radio Is the Only
  Store"). It's **offered** by a real event, **mulled** in dead hours, **held** for one gain and one cost, and
  **scored** every quarter by a trade-paper reading of real sim aggregates: *vindicated*, *unproven* or
  *contradicted*.
- Every effect is a multiplier or a gate on a lever the player already pulls and the AI never touches: a Rolodex
  counter, rapport, a shop or operator relationship, the scouting fog, an errand's hours. No new sim stat, no new
  roll, nothing the AI economy reads.
- Each card owns **one counter line** on the phone. While the card is unproven it's a labelled bluff. Vindicated,
  it's an honest counter. Contradicted, it's withdrawn and the sponsoring voice says why.
- The first six cards can all be offered in the 1960 selling months, which is where the empty days are (§6 "Measure
  it"). That's on purpose: the drawer is the first system that has something to say in May 1960.
- **What it can't do:** it doesn't give a busy player a new action with sim consequences on a day the sell loop has
  run dry. It turns those days into progress and gives them decisions. Section 7 has the honest numbers.

## 1. The card

A card is static authored data plus a small runtime row. The data:

| Field | Meaning |
|---|---|
| `Id`, `Title` | Stable id (`radio-only-store`), the card's name |
| `Sponsor` | The voice that argues for it (Ear, Street, Suit, Fixer). It speaks the offer and the verdicts. |
| `Opponent` | The voice that argues against it. It gets the last word when the card is contradicted. |
| `Trigger` | The real event that offers it (§2.1) |
| `MullHours` | Hours of dead time to internalize it (§2.3) |
| `Legwork` | The verbs whose hours also count toward mulling |
| `Gain`, `Cost` | One each, each a `PlayerModifiers` entry (§4) |
| `Counter` | The one Rolodex counter the card owns (§4.3) |
| `Predicate` | A pure function of the quarterly `MarketReading` returning a value and a verdict (§3) |
| `Leaning` | Optional archetype that gets the offer at a lower bar (directive §4.2) |

The runtime row (`ConvictionCard` on a new `ConvictionDrawer`, saved): `State` (Offered, Mulling, Held, Renouncing),
`HoursMulled`, `OfferedOn`, `HeldOn`, the verdict history (one entry per quarter: date, value, verdict), and
`ProvenAwarded` (§2.6). The drawer also keeps the scar list and a per-card "not before" date for re-offers.

## 2. Lifecycle

### 2.1 Offered: from which events

A card is offered by a fact that is true, at the moment it becomes true, and it's spoken by the sponsor voice in the
place it happened (the call, the shop, the morning paper). Never from a menu.

Rules:

- **One offer on the blotter at a time.** It waits seven days, then lapses. A lapsed or declined card can't be
  offered again for 26 weeks (*provisional*), and then only by its trigger firing again.
- **One new offer per 14 days at most** (*provisional*). If two triggers fire inside that window, the first one
  wins and the second is simply not offered; it'll fire again if its fact stays true. No queue, so nothing gets
  offered about a fact that's stopped being true.
- **Deterministic.** The first eligible trigger in game order wins. No draw, no weights. The same play makes the
  same offers.
- **The December sitting** can offer one card a year, from the cards whose trigger fact was true that year and
  which aren't held. The lawyer doesn't offer it; the sponsor voice does, on the walk home. (Phase 3c: the first
  six cards don't need it.)
- **Leaning.** An archetype's leaning card fires on a softer version of its trigger (named per card below). Open to
  all; just easier for one.

Accepting puts it in a free slot as Mulling. With no free slot, the offer says so and the player can accept only by
renouncing something (§2.5) or let it lapse.

### 2.2 Slots

Two at founding, one more on 1 January 1962, 1964 and 1966 (the directive's numbers, kept). Held and mulling cards
both use a slot; a renouncing card keeps its slot until the renouncing is done. Five slots by 1966 against the full
dozen means the player is choosing all decade.

*Trade-off:* slots by year rather than by feat keeps the drawer off the instinct ladder, so a bad 1961 doesn't also
shrink the inner life. The cost is that a slot isn't earned. I'd keep it; the December point already covers
earning.

### 2.3 Mulling: what it costs in dead hours

**Dead hours are the hours a day ends with.** When the player ends the day, the hours left between the clock and
the hard stop (`TimeManager.HardStopHour`, 21:00) are dead. A day ended at 2 p.m. gives 7. A day with nothing done
gives 12. A night that ran past midnight gives none. Every mulling card gets all of them (mulling runs in
parallel; the slots are the limit).

**Legwork counts too.** Hours spent on a card's `Legwork` verbs also go to that card, at the same rate, while the
work happens. That's the directive's "related legwork shortens it", and it means the player who's working the
right part of the business doesn't have to stop to think.

**Floor.** A card can't finish in fewer than 7 calendar days from acceptance, however many hours it gets. A
conviction takes a stretch, not an afternoon.

**Lengths** (*provisional*): 40 hours for a card about the player's own channel, 56 for a card about a market
share, 72 for a card about luck. A busy player in the 1960 selling months has about 45 empty days (~540 dead hours)
plus partial evenings, so a 40-hour card finishes in two to three weeks, and two 56-hour cards in parallel finish
in about a month. Phase 3b measures it with the busy bot and resizes so that a busy player finishes a card in 3–5
calendar weeks.

**The day-end button says it.** "End the day (7h left): mulling *Radio Is the Only Store*, 22 of 40." That's the
whole point of the mechanic: ending the day early stops being a waste and becomes a choice.

*Trade-off considered:* an explicit MULL verb at the desk (spend 3h, get 3h) is more legible but adds a click that
does nothing a day-end can't, and it punishes the busy player who never has a free evening. Converting the day's
leftover hours does the same thing without a button. The evening at the desk (§6 Nights: re-listen to the
acetate, go out) is Phase 4's, and it would feed the same counter.

**Preoccupied.** While a card is mulling, its **sponsor voice's passive reads go quiet** on calls and in the band
room: `PassiveReads` (`PlayerDesk.Rolodex.cs` ~L563) and the band-room reads skip that voice. Options it gates stay;
only the unprompted read is lost. The voice is busy arguing the card with you. That's a real cost (information)
and needs no new channel. When two mulling cards share a sponsor, the voice is quiet once, not twice.

### 2.4 Held

When the hours are in, the card is held. Its gain and cost apply from the next action. The sponsor gets a one-line
"I told you" the first morning, whatever the market says yet. The opponent gets one line too. Held cards are
scored every quarter (§3), and the verdict scales the effects (§4.2).

### 2.5 Renounced: and the scar

- **Renouncing takes half the card's mull hours** (dead hours and legwork as above), during which the card sits in
  its slot as *Renouncing*. Gain and cost stop the moment renouncing starts, so the slot is just blocked.
- **The scar.** A renounced card leaves a scar row: card, date, and the verdict it had when you threw it away. The
  voices quote scars, one line per scar, in two places only: the sponsor on the first call where the card's counter
  *would* have been honest, and the opponent at the next December sitting's walk home. A scar never changes a
  number.
- **No take-backs this year.** The same card can't be offered again until the next 1 January, and then only by
  its trigger.
- **Renouncing a vindicated card** is allowed and is the drawer's sharpest regret: its scar line is the sponsor's
  "You had it right."

### 2.6 Instinct shifts: what convictions write to the ledger

`PlayerDesk.Instincts.cs` keeps a change ledger for later channels. Convictions write to it in one case only:

- **Proven.** A card held through **four vindicated quarterly readings** (not necessarily consecutive) raises its
  sponsor instinct by one, up to 5, once per card per run: `InstinctChange { Why = "Conviction proven: Radio Is the
  Only Store" }`. It's a year of the market agreeing with you, which is the same scale as the December point.
  Nothing a player repeats can earn it (directive §4.3, "no grinding").

Not proposed for the first six: shifts at adoption ("Everybody Has a Price: Fixer +1, Ear −1") and losses on
contradiction. Both are possible later on the same ledger; adoption shifts need a reversal rule on renounce, and
contradiction losses punish the period irony the drawer exists to show. Open question 3.

## 3. Scoring: the quarterly `MarketReading`

### 3.1 When and where it's built

- **Built on the player's desk only.** It's a field on `PlayerDesk`, so a headless AI run (no desk) never builds
  it. It reads, never writes, and never draws a `GameRandom` value.
- **Weekly accumulation, quarterly close.** The desk's weekly settlement (where `WeekBooks` is inserted,
  `PlayerDesk.cs` ~L5755) adds the week to running accumulators. The first settlement on or after 1 January, 1 April,
  1 July and 1 October closes the quarter into a `MarketReading`, scores every held card against it, and resets the
  accumulators. Readings are kept for eight quarters (the predicates look back at most four), and saved.
- Phase 3a's first check is ordering: the settlement must run after the week's regional sales and airplay are final.

### 3.2 What it reads

All of these are existing fields. Nothing is shadowed: the reading keeps sums over a quarter, which the sim
doesn't keep, and the sim never reads them back.

| Accumulator | Built from | Used by |
|---|---|---|
| `WorkedRegions` | The home region, plus every region where this quarter the player placed a lot, visited a stop, held a `StationAdvocacy` row or sold a copy | Shares (C3, C4) |
| `FamilyUnits[region][family]` | Σ over records with `regionalData[r].unitsSoldThisWeek > 0` of those units, by `GenreCatalog.Get(MapLegacy(primaryGenre)).Family`. All labels. | C3, C4 |
| `MyRegionWeeks` | One row per (player single, region, week) with a copy on hand: wholesale units `unitsSoldThisWeek`, trunk units booked there (`BookSale`), `aired = radioPlay ≥ 0.05` (*provisional*), `stocked = unitsInStores > 0 or a player lot with Remaining > 0` | C1 |
| `LotUnits[kind]`, `LotCopyWeeks[kind]` | Trunk units booked per `StopKind` (in `BookSale`), and Σ `Remaining` over each kind's lots at settlement | C5 |
| `AdvocacyClosed` | The player's `StationAdvocacy` rows that expired this quarter: count, and how many have `everPlayed` | C2 |
| `Cohort12` | Each single, any label, the week its `weeksSinceRelease` reaches 12: (its quality, the same `recordQuality` the Rolodex context reads; its `totalUnitsSold`) | C6 |
| `Heat` | `RadioEra.RegulatoryHeat(year, month)` at the close | C2's voice lines only |

**Cost.** `FamilyUnits` is the only one that scans other labels' records, once a week. Phase 3a times it on a busy
1966 save ([[perf-hub]]); if there's already a per-region genre total in `GenreMarketV2` or `RegionalGenreTaste`
that the sim computes anyway, read that instead, and if the scan costs more than a few milliseconds a week, restrict
it to records with `weeksSinceRelease ≤ 52`.

### 3.3 Verdicts

Each predicate returns a value and one of three verdicts. Every card has a **minimum evidence** bar; below it, the
verdict is *unproven* whatever the value. Each quarter is scored fresh: the latest reading sets the effects, and the
card's history (V U V C…) is printed on its face. No hysteresis in v1; if verdicts flicker in the 3a measurement,
the fix is a band, not a memory.

**Thresholds come from measurement, not taste.** For every market predicate, Phase 3a replays the reading over
the existing decade telemetry (two seeds, `SimLogs`) and records the quarter-to-quarter spread. The vindicate and
contradict bars are then set at least two standard deviations of that noise apart from "no change", or at the
numbers below, whichever is wider ([[vacuum-filling-vs-sampling-variance]]). A card whose value never leaves the
band over the decade is a dud, and gets a different predicate before it ships.

## 4. Effects

### 4.1 One lookup

Every effect goes through `PlayerModifiers`, a static lookup on the desk that the player's own call sites consult,
exactly like `StationAdvocacy` (directive §10). With no desk, or no held card, every query returns the identity
(×1, +0, no gate). The table of channels the first six use:

| Channel | Where it lives today | Player-only? |
|---|---|---|
| Rolodex counter options and weights | `CounterOptions`, `CounterWeight` (`PlayerDesk.RolodexVerbs.cs` ~L336, ~L609) | Yes |
| Rapport gained on a call | `ApplyRapport` (`RolodexVerbs.cs` ~L1024) | Yes |
| Shop / operator / one-stop relationship | `TouchStop` (`PlayerDesk.cs` ~L3341), `PlayerStop.Relationship` | Yes |
| Verb gates (payola) | `PayolaMinFixer`, the `OfferPayola` and `FixerSweeten` options | Yes |
| Scouting fog | `ScoutingReadNoise`, `DescribeProspect` noise and `ReadConfidence` at the desk's call sites (`PlayerDesk.cs` ~L1378, ~L1575, ~L1716) | Yes, **with one change**: see below |
| Errand and prep hours | `StopVisitHours`, `ScheduleHours`, the plant visit hours | Yes |
| Voice passive reads | `PassiveReads`, the band-room reads | Yes |

**The fog needs one shared-code touch.** `ScoutingPerception.PerceivedQuality` is a pure function the AI also
calls, and its band comes from `label.scoutingAbility`. Scaling the player's fog means giving it an optional
`bandScale = 1f` argument that only the desk's call sites pass. The AI's calls are unchanged, so a headless audit is
byte-identical, but it's the one place where the drawer edits a file the AI uses, and its gate is a byte compare
([[probe-run-byte-comparison-proves-inertness]]). Never write `Label.scoutingAbility` itself: that's a sim stat.

### 4.2 How the verdict scales them

| Verdict | Gain | Cost | The card's counter |
|---|---|---|---|
| Held, no reading yet, or unproven | as written | as written | a labelled bluff (he can call it, same catch roll as every bluff) |
| Vindicated | 1.5× the gain's distance from identity | as written | an **honest counter**, with its `CounterWeight` |
| Contradicted | as written | 1.5× the cost's distance from identity | withdrawn; the opponent voice says why, with the number |

"Distance from identity" means a ×0.8 fog becomes ×0.7 and a +0.04 becomes +0.06. Everything is clamped to the
lever's existing range.

### 4.3 Bluff to honest counter

Every card owns one `CallCounter`. The rule is the directive's ("the claim becomes a fact in scenes") made
general: a card adds a counter the player wouldn't otherwise have. Before the market has spoken, saying it on the
phone is a claim you can't back, so it's offered with `isBluff = true` and the existing catch roll. Once the
trade paper has printed it, it's a fact the jock can look up, so it's offered grounded. Contradicted, the paper has
printed the opposite and you can't say it. The vindicated counter's weight is set to sit with the existing honest
counters it resembles (0.16–0.22), never above `CallInFavor` (0.40).

The counter's sub-label quotes the reading ("True: the trades have it at 4 to 1 in your regions"), so the player
can see what made it honest.

### 4.4 No new roll

The drawer adds no roll anywhere. Two existing hidden rolls change odds (a shop's counter roll through
relationship, a bluff's catch roll by being skipped when the counter is honest). That's the same as any other
relationship or counter in the game. Nothing visible is a percentage.

## 5. The first six cards

Each is worked through: offer, hours, gain, cost, counter, predicate, and the voice lines that must name real
state. Five of six can be offered in the 1960 selling months, February to June; the sixth (C3) from April 1960 on
the trade paper. All numbers *provisional*.

### C1. Radio Is the Only Store

| | |
|---|---|
| **Sponsor / opponent** | Fixer / Suit |
| **Offered** | A shop passes on your single with "come back once it's played somewhere" (`CounterPassReason`'s third reason, `PlayerDesk.RecordShops.cs:60`), for the **second** time on the same title within 14 days. The busy bot logged 857 refusals in 12 runs; this fires in the first fortnight of selling. *Leaning (Promo Man):* the first such pass. |
| **The offer** | Fixer: "Second counter this week that wants to hear it on the radio first. Stop carrying boxes. Get it on the air and they'll call you." |
| **Mull** | 40h. Legwork: Rolodex calls to jocks, station stops, record hops. |
| **Gain** | Rapport from a won pitch or a played acetate ×1.25 (`ApplyRapport`). |
| **Cost** | Relationship gained at shops and one-stops ×0.6 (`TouchStop`). Dealers can tell when you think they don't matter. |
| **Counter** | `RadioMakesTheRack` on `NoSalesSupport` ("Radio follows the racks here"): "The racks follow you, not the other way round. Put it on and the counter numbers come to you." Weight 0.18. |
| **Predicate** | Over `MyRegionWeeks` with `stocked`: `A` = units per aired region-week, `N` = units per unaired region-week. `R = A / max(N, 0.5)`. |
| **Verdicts** | Vindicated R ≥ 4; contradicted R ≤ 1.5; unproven otherwise. Minimum evidence: ≥ 3 aired and ≥ 6 unaired stocked region-weeks. |
| **Verdict lines** | Vindicated, Fixer: "Four to one. Stocked towns with a spin sold four copies for every one in towns without." Contradicted, Suit: "Towns without a spin sold as well as towns with one. It was the counters that moved it." The numbers are the reading's. |

*Why it's worth holding in 1960:* the busy player's selling months are spent pitching counters that won't take it.
The card buys the phone, at the counter's expense, and asks the market whether that was right.

### C2. Never Pay a Jock

| | |
|---|---|
| **Sponsor / opponent** | Suit / Fixer |
| **Offered** | A jock raises `WhatsInItForMe`, the Fixer's `FixerSweeten` is on the table, and the player ends the pushback any other way. Or the player refuses Jack Germain's first move (§6.1), or a bust lands on the player's own payola ledger. 1960 is the crackdown peak (`RegulatoryHeat` 0.90), so the Suit has a case. |
| **The offer** | Suit: "He wanted an envelope and you didn't give him one. Good. They're subpoenaing logbooks in Philadelphia this month." (The heat line needs `Heat ≥ 0.7`; below it, the Suit drops the subpoena clause.) |
| **Mull** | 40h. Legwork: Rolodex calls. |
| **Gain** | Counter `CleanRecord` on `ManagerHeat` and `WhatsInItForMe`: "Nobody's paid you to play this, and nobody will. That's a record you can put on in daylight." |
| **Cost** | Payola locked: `OfferPayola` and `FixerSweeten` are not offered, and **Germain's "his boys" Concede** isn't on the CONCEDE card at the sitting (you can still renounce first). |
| **Counter rule** | This card's counter is the gain itself, so it follows its own rule: honest while unproven if `Heat ≥ 0.7` (the crackdown is a fact in 1960–61), a bluff otherwise; honest when vindicated; withdrawn when contradicted. Weight 0.18. |
| **Predicate** | `p = everPlayed / closed` over `AdvocacyClosed`. |
| **Verdicts** | Vindicated p ≥ 0.5; contradicted p ≤ 0.2. Minimum evidence: 4 closed rows in the quarter. |
| **Verdict lines** | Vindicated, Suit: "Six stations took a look this quarter and four of them played it. Nobody got an envelope." Contradicted, Fixer: "Five looks, no spins. Clean hands, empty log." |

*Period irony:* the heat falls to 0.55 by 1965, so the free honest counter goes away mid-decade even if nothing else
changes, and the card has to earn it from the advocacy numbers.

### C3. Rock and Roll Is Finished

| | |
|---|---|
| **Sponsor / opponent** | Suit / Street |
| **Offered** | From April 1960, the first Monday the trade column's Hot 100 top three (`GatherTradeNews`) has no Rock-family record, **and** your worked regions' Rock-family share fell from the last closed quarter to this one so far. Elvis is in the army, Holly is dead, Little Richard is preaching: every trade man said this in 1960. |
| **The offer** | Suit: "Not a guitar in the top three. The kids have gone back to ballads. Rock and roll was a fad and the fad's over." |
| **Mull** | 56h. Legwork: venue scouting, record-shop visits. |
| **Gain** | Scouting fog on **Pop-family** acts ×0.8 (narrower). |
| **Cost** | Scouting fog on **Rock-family** acts ×1.25 (wider). The directive also has "guitar bands sense it in courtship (trust −)", but there's no courtship trust channel yet: courtship as a counter scene is Phase 2 (§7). Add it when Phase 2 lands, not before. |
| **Counter** | `SuitTheBalladsBack` on `NoLocalAudience` for a Pop-family record: "Rock's down every quarter. The kids are buying the ballads again." Weight 0.16. |
| **Predicate** | `S_q = Σ_{r∈Worked} FamilyUnits[r][Rock] / Σ_{r∈Worked} Σ_f FamilyUnits[r][f]`. Value: `S_q / S_{q−1} − 1` (in 1961 on, compare with the same quarter a year earlier, to take out seasonality). |
| **Verdicts** | Vindicated ≤ −10%; contradicted ≥ +10% (or ±2σ of the measured noise, wider wins). Minimum evidence: 2,000 units in the worked regions that quarter. |
| **Verdict lines** | Contradicted, Street: "Rock's 31% of what sold in your regions this quarter. It was 26. Somebody's buying guitars." |

*Period irony:* this is the card the directive means by "the reason you missed the Beat boom". Whether the sim's
Rock share actually turns up, and when, is for 3a to measure, not to assume. If Rock share in the sim falls all
decade, this card is simply right, and a different family is the one to make wrong.

### C4. This Town Is Turning

| | |
|---|---|
| **Sponsor / opponent** | Street / Suit |
| **Offered** | The Street's existing bluff gets caught: a jock calls "The city's turning. You just can't hear it from inside the building." (`StreetBluff`, `RolodexVerbs.cs` ~L394). Or a home-town shop passes with "that trade hasn't found its way to this side of town yet". The card is written about **one family**: the family of the record on the call or at the counter, fixed at the offer ("This Town Is Turning to Rhythm & Soul"). |
| **The offer** | Street, after being caught: "He's wrong. Maybe not this week. But it's turning, and when it does I want it on paper that I said so." |
| **Mull** | 56h. Legwork: home-town venue scouting and home-town shop visits. |
| **Gain** | Scouting fog on that family's acts in the home region ×0.8. |
| **Cost** | Scouting fog on every act outside the home region ×1.2. You stop looking out of town. |
| **Counter** | The card's own fact converts the old bluff: in the home region, for that family, `StreetScene` is offered **grounded** when the card is vindicated even if this week's `regionalGenreMomentum ≤ 0.08`, with sub-label "True: the trades have the town moving to it." While unproven it stays the existing `StreetBluff`. |
| **Predicate** | As C3, for the card's family, home region only: `S_q(F, home) / S_{q−1}(F, home) − 1`. |
| **Verdicts** | Vindicated ≥ +10%; contradicted ≤ −10% (or ±2σ). Minimum evidence: 1,000 units in the home region that quarter. |
| **Verdict lines** | Vindicated, Street: "Soul was a fifth of what this town bought last quarter. It's a quarter now. Told you." |

*Why this one:* it's the cleanest case of the directive's mechanic, an **existing** bluff turned honest by the
market. It also gives the caught bluff, which today only costs rapport, a second life.

### C5. The Jukebox Is the Real Chart

| | |
|---|---|
| **Sponsor / opponent** | Street / Suit |
| **Offered** | An operator (`StopKind.Op`) takes your single, and in the next 14 days its lot sells more copies than the shop lots in the same city put together. Kubiak's runs get a pointed version (his half-reach is on the operator list), but it fires in every run. *Leaning (Pawn-Shop Owner):* the first time an operator takes the record at all. |
| **The offer** | Street: "The bars moved more of it in a fortnight than the shops did. Kids don't buy records. They feed nickels." |
| **Mull** | 40h. Legwork: operator visits. |
| **Gain** | Relationship gained at operators ×1.5 (`TouchStop`). |
| **Cost** | Rapport from Rolodex calls ×0.85 (`ApplyRapport`). The jocks hear you're a route man's label. |
| **Counter** | `StreetTheMachines` on `NoSalesSupport`: "It's on every machine on the east side. Ask the bartenders what they're changing the dimes for." Weight 0.16. |
| **Predicate** | `T_k = LotUnits[k] / LotCopyWeeks[k]` for operators and shops. Value `T_op / T_shop`. Copies on hand, not copies placed, so where you put the stock doesn't decide it. |
| **Verdicts** | Vindicated ≥ 1.25; contradicted ≤ 0.8. Minimum evidence: 100 copy-weeks at each kind. |
| **Verdict lines** | Contradicted, Suit: "A copy on a machine sold a third as fast as a copy in a shop this quarter. The routes are drying up." |

*Period irony, built in already:* operator routes shrink with the decade (`PlayerStopFactory.JukeboxEraWeight`), so
this card should be vindicated early and contradicted by the mid-sixties. Kubiak believes it; the Suit knows
better (directive H2). It's the drawer's clearest "right in 1961, wrong in 1965".

### C6. Every Record Is a Lottery Ticket

| | |
|---|---|
| **Sponsor / opponent** | Fixer / Ear |
| **Offered** | Your single reaches eight weeks past its street date without reaching `LocalTraction` in any region. For the busy bot's first single that lands in April or May 1960. *Leaning (Pawn-Shop Owner):* six weeks. |
| **The offer** | Fixer: "Eight weeks and not a town picked it up. It's a lottery, kid. Buy more tickets." The Ear, at once: "It's a lottery if you can't hear." |
| **Mull** | 72h. Legwork: plant visits, studio sessions. |
| **Gain** | Release prep hours ×0.5: scheduling the release (`ScheduleHours`), plant visits, the lacquer approval. Get the next one out. |
| **Cost** | Per-record Ear reads need one more point: every Ear-gated read about a record (the acetate line, `EarChorus`, `EarConcede`, the passive Ear read on a call, the acetate re-read) checks `TheEar − 1`. That's a scoped read through `PlayerModifiers`, not a ledger change. |
| **Counter** | `FixerNumbersGame` on `UnknownArtist`: "Nobody knew anybody's name before the record. That's how it works." Weight 0.14. |
| **Predicate** | Spearman ρ between quality and units over `Cohort12` for the trailing four quarters, **all labels' singles**: the claim is about the business, not just you. |
| **Verdicts** | Vindicated ρ ≤ 0.25; contradicted ρ ≥ 0.5 (*provisional*; 3a sets them from the decade distribution of quarterly ρ). Minimum evidence: 40 singles. |
| **Verdict lines** | Contradicted, Ear: "The better record outsold the worse one about three times in four. Some lottery." (The "three in four" is the concordance the ρ implies, printed in words.) |

*Note:* if 3a finds ρ never moves across the decade, this card is a fixed fact and the thresholds shouldn't be
tuned to make it swing. Then it's honest as a card that's simply right or simply wrong for the whole run, and that's
acceptable for one card in six.

### 5.7 Coverage check

| Card | 1960 selling months? | Gain channel | Cost channel | Counter |
|---|---|---|---|---|
| C1 Radio | Yes, first fortnight | rapport | shop relationship | new, NoSalesSupport |
| C2 Never Pay | Yes | counter | verb gate (+ Germain concede) | new, ManagerHeat / WIIFM |
| C3 Rock | From April | fog | fog | new, NoLocalAudience |
| C4 Town | Yes | fog | fog | **existing bluff converted** |
| C5 Jukebox | Yes | operator relationship | rapport | new, NoSalesSupport |
| C6 Lottery | April–May | time | Ear read threshold | new, UnknownArtist |

Every voice sponsors at least one card and opposes at least one.

### 5.8 Cards in the directive's dozen without a clean channel

Flagged for the later cards, not the first six:

- **Any "courtship trust" effect** (Guitar Groups, Never Pay a Jock's "acts' managers trust you"): courtship has
  rival-interest state but no trust lever the player argues against until Phase 2 builds courtship as a counter
  scene (§7).
- **Never Pay a Jock, "immune to payola heat"** (directive table): no. Exposure is a real ledger, and immunity would
  falsify it. C2 maps it to the verb lock instead.
- **Kids Buy Albums Now, "album planning reads"**: there's no player-facing LP planning read to sharpen yet.
- **The Majors Will Eat Everybody, "sale terms improve in the 1970 reckoning"**: the buy-out offer (65% of
  valuation) is a player-only path and could take a modifier, but it's an ending number, and a card shouldn't move
  an ending by a percentage without the author's say. Needs a decision.
- **I Was One of Them, "Suit −1 on contracts with friends' acts"**: negotiation terms exist; "friends' acts" needs a
  definition from band-room state.
- **Guitar Groups Are on the Way Out, "Suit +1 with Brill-style writers"**: the professional-writing market has
  terms, but "Brill-style" isn't a sim category; map it to Pop-family writers.

## 6. The UI surface

- **The drawer lives on the OFFICE tab**, under the character card, beside the instinct points. It's a row of
  index cards, one per slot, with empty slots shown as blank cards ("A slot opens in 1962").
- **Card face:** title; sponsor voice's colour; a stamp (MULLING 22/40h, HELD, VINDICATED, UNPROVEN,
  CONTRADICTED, RENOUNCING 8/20h); the gain and cost in plain words with the real figure ("Shops warm to you 40%
  slower"); the verdict history as small stamps; and the last reading's number in the sponsor's words. Low Suit
  (1–2) sees the reading in words only ("down a bit"); Suit 3+ sees the figure. That's presentation, not a fog.
- **The offer** arrives as a card on the blotter: a small sheet over the desk, like the holder's move but lighter,
  with ADOPT and LEAVE IT, the gain and cost spelled out, and how many hours it'll take. NEXT UP lists "A card on
  the blotter (6 days)".
- **Mulling** shows on the day-end button (§2.3) and on the card. A finished card gets a morning line from the
  sponsor.
- **Scoring is announced in the morning paper.** The quarter's first paper carries a **THE QUARTER** block in the
  trade column (`TakeTradeNews`): one line per held card, written as trade copy with the real figure ("Rock and
  roll's share of the Great Lakes counters: 26%, down from 31%"), then the voice's verdict line. Vindicated cards
  are kept as **clippings** on the card face, and later on the office wall (§9). When Phase 4's newsstand exists,
  the block moves there and the paper keeps a one-line pointer.
- **The Trade Insider's chart files** (§3.8): once recovered, the reading also prints the same quarter a year
  earlier and a mid-quarter **early read** at week six. The early read is labelled as such and never scores a card.
  That's the "sharper quarterly market readings" perk, made honest.

## 7. How much of the empty time it fills

The measurement (§6): a busy scripted player has a median **59 empty days** of 182 in the first 26 weeks, plus
about **35 side-only days**, and in Detroit and New York **40–55** of the empty days are in the selling months
(months 2–6). On those days the bot had either been turned down at the counter or had nothing left to try.

What the drawer does in the first 26 weeks, for that player:

| | Estimate (*provisional*) |
|---|---|
| Offers landing in the selling months | 4–5 (C1, C2, C4, C5, C6; C3 from April), at most one per 14 days |
| Decisions they create | 4–5 adopt-or-leave choices, ~1–2 forced trade-offs (two slots), 1 quarterly paper with verdicts (1 April), a second on 1 July just past week 26 |
| Empty days that carry mulling progress | ~20–25 of the ~45–55 in the selling months (with 2 slots and 40–72h cards) |
| Empty days that gain a **sim-consequential** action | ~0–5. Only indirectly: C1 and C5 shift relationships so some later pitches land; C6's halved prep can bring a second record a few days sooner (the two-record bot ran 44–52 empty days against 59). |

Said plainly:

- **By the metric's own definition, the drawer fills almost none of it.** An empty day is one with no action that
  has a sim consequence, and mulling has a player-only consequence. The drawer turns dead time into progress toward
  something the player chose, gives the selling months a handful of decisions, and puts a verdict in the paper. It
  doesn't create work.
- **It does nothing for the side-only days.** Those are already demo and booker work.
- **It can't fix "nothing left to try".** 355 of the bot's empty days had no next move because every nearby account
  had passed or was stocked. That's the sell loop's shape, and it's §6.1's job (the Run, the flip, the hit that
  breaks you) and Phase 4's (the legwork map). The drawer should be measured as what it is.
- **Recommendation for the metric:** add a **mulling** day class to `analyze_empty_days.py`, reported next to empty
  and side-only, and never counted as filled. Then the target stays honest and the drawer's share is visible.

## 8. Invariants and gate checks

The four invariants (directive §0), as they apply here:

1. **Player-only, AI-inert.** The drawer and the reading live on `PlayerDesk`. `PlayerModifiers` returns the
   identity without a desk. The one shared-code touch is the optional `bandScale` on `ScoutingPerception`.
2. **No dice.** Offers, mulling and scoring are deterministic. No new roll anywhere; existing hidden rolls only see
   changed odds.
3. **Voices name real state.** Every offer line names the event that fired it; every verdict line prints the
   reading's own number. A voice can be wrong only where its fog is real (C3/C4 fog).
4. **Never duplicate sim stats.** The reading holds quarterly sums the sim doesn't keep and never reads. Instinct
   changes go only through the existing ledger.

Gates, phase by phase:

| Phase | Builds | Gate checks |
|---|---|---|
| **3a** The reading | `MarketReading` accumulators and quarterly close; predicates as pure functions; no cards yet | (1) **Headless byte-identical** AI audit vs before. (2) **Player-run byte-identical:** a scripted player run with the reading on matches the run without it, byte for byte (the reading draws no `GameRandom`; count the calls before and after). (3) **Save/load mid-quarter** gives the same closed reading. (4) **Each predicate on a real save** (directive §11): run all six over a 1960, 1963 and 1966 save with the save-inspection probe and print value and verdict. (5) **Decade spread:** replay the predicates over the existing decade telemetry, two seeds, and record the quarter-to-quarter noise for the threshold rule in §3.3. (6) **Timing** of the `FamilyUnits` scan on a 1966 save. |
| **3b** Lifecycle | Offers, slots, mulling, held, renounce, scars; effects still identity | (1) `ConvictionChecks.tscn`: each trigger fires only on its real fact, the same play gives the same offer, the 14-day and 26-week rules hold, dead-hours arithmetic (a day ended at 14:00 gives 7h; past midnight gives 0), the 7-day floor, renounce timing, no re-offer the same year. (2) **Empty drawer is inert:** a player run with the drawer present but no card adopted is byte-identical to the 3a run. (3) Save/load at every state. |
| **3c** Effects | `PlayerModifiers` entries for the six cards, the six counters, the verdict scaling, the instinct-ledger write | (1) Every modifier is the exact identity when absent (unit check per entry). (2) Headless byte-identical again, including the `ScoutingPerception` byte compare. (3) Each card's gain and cost moves its lever by the stated factor and nothing else (a held-card run diffs only in that lever's call sites). (4) Counter state follows verdict: bluff, honest, withdrawn. (5) `InstinctChecks` extended: four vindicated readings write exactly one ledger row, capped at 5, once per card. |
| **3d** UI | Drawer on OFFICE, blotter offer, day-end line, THE QUARTER in the paper | UI driver check ([[ui-verification-via-temp-driver-scene]]): offer → adopt → mull → held → quarterly paper → renounce, with the figures on screen matching the reading. |
| **3e** Re-measure | The busy bot adopts the first offer in a free slot | `analyze_empty_days.py` with the mulling class; report empty, side-only and mulling days against the 59 / 35 baseline. The target for this phase is the mulling share, not the empty count. |

## 9. Open questions for the author

1. **Mulling in parallel or one at a time?** Proposed: parallel, slots are the limit. One at a time makes cards
   slower and the slot cheaper.
2. **Should mulling days count as "filled"** in the empty-day metric? Proposed: no, a separate class.
3. **Instinct shifts:** only "proven" (+1 to the sponsor after four vindicated readings)? Or also shifts at adoption
   (the directive's "Everybody Has a Price: Fixer +1, Ear −1")?
4. **Never Pay a Jock locks Germain's "his boys" Concede.** Acceptable as the card's cost in a Germain run, or too
   strong against the lien?
5. **Order against Phase 2.** Two cards (C3, C4) and several later ones lean on the scouting fog, and Phase 2 is
   where instincts first reach that fog. Build the drawer first with the `bandScale` hook, or Phase 2 first?
6. **Thresholds by measured noise** (§3.3): agreed that a card that never leaves its band gets a new predicate
   rather than a tuned bar?
7. **The December sitting's offer**: in Phase 3, or with the later cards?
