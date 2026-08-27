# Directive: Player-Facing Promotion (early game)

**Branch intent.** Give the human player the *other* half of the road: promotion. Today the player
drives records to shops and phones DJs, and those two things happen in different universes -- the
trunk is physical, inventory-bound and full of named people; radio is a free phone call with no
vinyl, no mileage and no cost of goods. This branch makes promoting a record feel exactly like
selling one out of the trunk: **it costs copies, it costs a day, and it costs a drive.**

**Scope.** Player company only. Nothing in the AI economy changes -- not `ApplyReleasePromotion`,
not `GetSeasonalMarketingImpact`, not `ChartSimulator`'s awareness/airplay/survey math, not
`StationNetwork`'s weekly playlist meeting, not genre or integration curves. Every mechanic below
is a player-only surface built on hooks that already exist and that no AI path ever calls
(`ChartManager.AddAwareness` / `AddRadioPlay` / `PlayerSpinNow` / `PlacePayolaCash` /
`PlaceProjectPromo`, `StationAdvocacyService`, `PlayerStop`, `InboundCall`, `PlayerSaveData`).

---

## 0. Ground truth -- what already exists

Read this before proposing anything. The opening game is not greenfield; two full branches
(`rolodex-player-character`, `distribution-expansion`) landed most of the surrounding loop.

**Built and load-bearing:**

- **The Rolodex** (`Systems/PlayerDesk.Rolodex.cs`, `PlayerDesk.RolodexVerbs.cs`, `RolodexCall.cs`,
  `RolodexScene.cs`). Cold-call discovery (`WorkThePhones`), a connect roll grounded in shift /
  influence / rapport, four instinct voices, an approach beat (`PersonalPitch`, `CommercialPitch`
  ad-buy, `RivalPressure`, `OfferPayola`, `AskForFavor`, `AskForIntroduction`), one grounded
  `Objection` (`RolodexScene.cs:48`), a counter beat with bluffs the jock can call, and real writes
  into rapport, `StationAdvocacyService`, and `PayolaLedger`. Outcomes are reported back weekly
  (added / moved up / dropped). **This is the radio half and it is good. Do not rebuild it.**
- **Named retail stops** (`PlayerStopFactory.cs`, `PlayerDesk.PlayerStop`). `StopKind` is
  `{ Shop, Op, OneStop, Venue }`, generated per city on an isolated seed stream, with
  relationship / `OnHand` lots / `OpenBalance` / `PassedRecordIds`. Verbs: `PitchAtStop`,
  `ConsignAtStop`, `ServiceStop`, `WorkTheHopTable`, `VisitOneStopWarehouse`,
  `SellCartonToOneStop`, `ArtistBuyIn`.
- **Inbound calls** (`InboundCall`, `GenerateInboundCalls`, `CheckWeeklyInboundCalls`) --
  the world phones the office when a title has local demand, keyed off real regional velocity.
- **The road and the clock.** `DriveTo` / `Travel` / `CanReach`, `DistanceModel` road miles,
  `HotelNightly`, a minute-hand day, `ActionCosts`, `RequireHome`.
- **The plant.** `OrderPressing` (`PressMinimumOrder = 500`, `PressingCost` = `$38` lacquer +
  `$20` freight + `$0.25`/unit), `PressReorderMinimum = 100`, real turnaround, `PressToFill`,
  `RequestPlantCredit`. **`PressStock` is one undifferentiated pool of sellable copies.**
- **A project promo man** (`HireProjectPromo`, `ChartManager.PlaceProjectPromo`, §7 of the
  distribution directive) -- $25/$50/$75 for 1-3 reporter stations over 2 weeks, routed through
  the `PayolaLedger`'s dormant `IndiePromoter` path so the scandal model applies for free.
- **The launch campaign.** `SetReleaseDate(single, daysOut, marketingBudget)` -> `FireRelease` ->
  `CompetitorManager.ReleasePlayerRecord` -> `ApplyReleasePromotion`, where the budget becomes
  `GetSeasonalMarketingImpact` -> national `awareness`. **This function is shared with every AI
  release. It is frozen.**
- **The chart is a survey, not a census.** `ChartSimulator.cs:210` and `:722`: pre-1973 Billboard
  polled roughly 110 outlets by hand -- 63 stations, 25 one-stops, 22 retailers. This is the single
  most important existing fact in this directive (see §7).
- **Regional buzz is already blended honestly** (`PlayerDesk.RegionalBuzz`): 40/60 national/regional
  awareness plus an undiluted read of `ChartManager.ReporterAirplay`, so cultivated station play
  really does move trunk sell-through.

**Genuinely NOT built -- this is the work:**

1. **A promo copy.** There is no such object. Vinyl is either sellable stock or it does not exist.
   A Rolodex pitch costs minutes and nothing else -- the jock is talked into playing a record he
   has never physically been sent. (§3)
2. **A station you can walk into.** `PlayerStopFactory` generates no station stop. Radio is
   phone-only, so a promo day and a selling day are different days in different systems. (§4)
3. **The mailing.** No way to service twenty stations at once, badly, from the office. (§5)
4. **The trades.** Zero references to Billboard/Cash Box as a player-facing object anywhere in
   `Systems` or `UI`. No review desk, no trade ad, no breakout listing. (§6)
5. **Reporting outlets.** The survey exists in the chart math but no `PlayerStop` knows it reports
   to anybody, so the loop a 1960 label actually gamed is invisible. (§7)
6. **The record hop.** `Venue` stops sell records off a table; no DJ MCs, no act appears. The
   artist is not a promotional asset at all. (§8)
7. **Servicing a moving record.** No "it's number four in Pittsburgh" verb -- the second-market
   pitch that is the entire small-hit game. (§10)

---

## 1. The history this is modelling

**What actually went out the door with a first 45 in 1960.** A label pressed 500-1,000. That run
was not all merchandise. A serious first release put **100-300 copies out free** as promo/DJ stock
-- some labels cut a separate white-label "DJ COPY -- NOT FOR SALE" pressing, most just stamped or
colour-swapped the label. Those copies are the promotion budget: the cash cost is trivial (vinyl is
a quarter a unit) and the real cost is that they are copies you can never sell.

Where they went, roughly in order of how well it worked:

- **Hand-delivered to the jock.** Driving the record to the station, sitting in the lobby, catching
  him going into his shift. Expensive in the only currency a one-man label has -- the day -- and by
  far the most effective thing available.
- **Mailed to a list.** A record mailer, four cents of postage, a typed one-page letter. A 100-piece
  mailing cost maybe ten dollars and an evening of typing, and most of it went in the bin: a record
  from a label nobody has heard of, with nobody following up, is not a promotion, it is a donation.
  It mattered because it was the only way to reach a city you could not drive to.
- **The trade review desks.** Billboard, Cash Box, Record World. Sending a copy cost postage. A
  "Spotlight" / four-star / "Best Bet" pick was not a consumer event at all -- almost no record
  buyer read Billboard. It was a **distributor-facing** event: it told one-stops, houses, rack
  buyers and programme directors that a record existed and was worth a phone call.
- **Trade advertising.** A paid ad in the trades was the same signal with your money behind it:
  "this label is going to work this record, stock it." Again: distributors and jocks, not kids.
- **Tip sheets.** Bill Gavin's Record Report (from 1958) and its imitators -- subscription
  newsletters read by programme directors. A Gavin mention was a national radio event you could not
  buy directly.
- **The station survey.** Every Top 40 station printed a weekly survey and gave stacks of it to
  record shops. Getting on it as a "Pick Hit" or "Hit Bound" sold records in that city that week.
  The survey was compiled from the station's own read plus **reports from a handful of dealers and
  one-stops** -- and the trade charts were built the same way, from the same kind of reports.
- **Hyping the count.** Because the survey and the chart were both compiled from a small number of
  reporting outlets, the notorious hustle was to make those specific outlets report movement: buy
  your own record out of the reporting shop, get kids to call the request line, make sure the
  reporting dealer has stock and a reason to say it is moving. Cheap, universal, and ruinous if the
  dealer worked out what you were doing.
- **Record hops.** A DJ MC'd a teen dance; your act appeared for gas money or nothing; the jock kept
  the gate; you sold 45s at the door. After the payola hearings (Freed fired November 1959, House
  hearings February 1960, the federal anti-payola amendment signed that September) this was **the**
  legal currency of a DJ relationship, and 1960-62 is exactly when cash was most dangerous.
- **In-store.** Window streamers, counter cards, browser-box dividers, and the act turning up to
  sign copies on a Saturday. Cheap print, and a dealer who put your card up ordered more.

**And when it starts to move.** The problem inverts. You now need promo copies in twenty cities you
have never driven to, and each unfollowed-up mailing is money in the bin. The trades become the
lever, because a **regional breakout listing** is what makes distributors, one-stops and majors
phone *you*. The stations that added it have to be *kept* -- fed news, told it is #4 in Pittsburgh,
or they cut it and never re-add. And all of that promo spend is cash out this week against paper
that is twelve to eighteen weeks away.

---

## 2. Design law

**Promotion is inventory, a day, and a name.** Every verb below spends at least one of: promo
copies out of a real press run, hours off the minute hand, miles on the road. Cash is the *least*
important input and mostly buys reach you could otherwise have earned by driving.

**Invariants.**

1. **A copy is a copy.** Promo stock comes out of the same pressing run the trunk sells. Giving 150
   away out of 500 is a real decision with a real opportunity cost, and it is the promotion budget.
2. **Nothing gets played that nobody has been sent.** A jock who has never been serviced with the
   record cannot add it. This is the mechanic that makes vinyl and radio one system.
3. **Distance costs the same for promotion as for selling.** A station in another city is reached
   by driving there, or badly by mail. There is no free national anything.
4. **The trades talk to the trade, not to the public.** A review or an ad moves distributors,
   one-stops, houses and programme directors -- it feeds `InboundCall` generation and station
   candidacy. It is never a direct units multiplier.
5. **Proof is geographic** (inherited from the distribution directive §1.3). A Pittsburgh survey
   position is the ammunition for a Cleveland call, and the game must let the player *say so*.
6. **No new fixed weekly cost** the player can reach before the channel it serves has paid, and do
   not soften `CreditFloor` (-$225) to allow one. Every price in §11 is a one-off or per-copy.
7. **Promotion cannot manufacture a hit.** Every lever here is bounded, feeds channels that already
   saturate, and works only on top of a record with a real hook. The one thing promotion buys that
   nothing else can is the *chance to be heard at all* in a market.

---

## 3. Phase 1 -- the promo copy

The foundation. Nothing else in this directive works without it.

### 3.1 Split the press run

`PressStock` gains a second pool:

```
PressStock
  Remaining        // sellable, as today
  PromoRemaining   // NEW: free goods. Cannot be sold, ever.
  TotalPressed, TotalSpent
```

`OrderPressing(recordId, quantity, promoCount, ...)` -- the player nominates how much of the run is
struck as promo stock. Cost is unchanged (`PressingCost` already prices the whole run); what changes
is how much of it can ever be sold. Suggested UI default on a first run: **120 of 500**, with the
panel spelling out the trade in plain money ("120 copies = ~$107 of sales you are giving away").

Two guards:
- Promo count is capped at ~35% of the run. A label that presses 500 and strikes 400 promos is not
  playing the game, it is exploiting a free-goods channel.
- A repress (`PressReorderMinimum = 100`) can be struck all-promo. This is how you service a second
  market on a record that is already moving, and it should be an obvious, cheap move.

`WorkTheHopTable`, `PitchAtStop`, `ConsignAtStop`, `ServiceStop`, `SellCartonToOneStop`,
`ArtistBuyIn` and `HandCartonToRunner` all keep drawing from `Remaining` only. Every verb in this
directive draws from `PromoRemaining` only. **Neither pool ever converts into the other.**

### 3.2 Servicing state

```
RecordServicing            // player-only, per (recordId, stationId)
  stationId, recordId
  week                     // when it landed
  conviction               // 0-1: mailed cold ~0.2, hand-delivered ~0.75, hop/appearance ~1.0
  source                   // Mailed, HandDelivered, Hop, Trade
```

Held on `PlayerDesk`, persisted in `PlayerSaveData`. Servicing **decays**: a copy sent nine months
ago is in a stack somewhere. Age it out after ~16 weeks, or when the record's regional stations
latch dropped (`RegionalRecordData.stationsDropped`).

### 3.3 The new objection

Add `Objection.NotServiced` to `RolodexScene.cs:48` and rank it **first** in the severity ordering
-- it is the most basic thing that can be wrong with a pitch. He raises it whenever no servicing row
exists for (record, his station):

> "I can't put on a record I haven't got, friend. Send me one."

Counters, using the existing two-part instinct+fact gate:
- `PressIt` -- he agrees to listen if you get one to him. Writes nothing; the call is a dead end
  until you actually send it, which is the lesson.
- **`OfferToBringIt`** (new) -- available only when the station's city is inside `CanReach`. Creates
  a soft appointment: a flagged, time-limited bonus on the `DropOffAtStation` visit (§4), and the
  jock expects you. Standing him up costs rapport.
- `FixerSweeten` / `SuitUnderwrite` stay available and stay *worse* -- money at a man who has never
  heard the record is exactly the 1960 mistake.

`conviction` then modifies the pitch roll everywhere the call already computes odds: a hand-delivered
copy is worth a real bonus, a cold-mailed one is worth almost nothing beyond clearing the gate.

---

## 4. Phase 2 -- the station is a stop on the day sheet

**This is the change that delivers the branch's stated goal.** Right now a promo day and a selling
day are different systems. Make them the same day sheet.

Add `StopKind.Station`. Unlike Shop/Op/Venue, these are **not invented** by `PlayerStopFactory` --
they are the real reporter stations the sim already runs, projected into the stop layer:

```
foreach reporter station in ChartManager.ReporterStationsInRegion(regionId)
    -> a PlayerStop { Kind = Station, StopId = "station_" + stationId, CityId = <station's city> }
```

A `Station` stop holds **no `OnHand` lot and no `OpenBalance`** -- it never sells anything. What it
holds is a link to the `RadioStation` / `Deejay` and to the Rolodex entry, if one exists yet.

Verbs at a station stop (all require being in the city, all cost hours off the minute hand):

| Verb | Cost | Effect |
|---|---|---|
| **Drop off a copy** | 1h, 1-2 promo copies | Servicing at `conviction ~ 0.75`. Small rapport tick. If the jock is not yet in the Rolodex, this is a **discovery** -- you met him, you have his card now. |
| **Wait for him** | 3h, 1-2 promo copies | Servicing at `conviction ~ 0.9`, a bigger rapport tick, and the pitch scene opens **in person** -- the existing `RolodexCall` beats, with the connect roll skipped (you are standing there) and a bonus to the roll. The expensive, good version. |
| **Leave it with the receptionist** | 15 min, 1 promo copy | Servicing at `conviction ~ 0.35`. No rapport, no discovery. What you do when you are behind schedule. |
| **Ask what's on the survey** | 30 min, free | A read: the station's current playlist tiers for records you care about, and which local dealers report to it (§7). Free information that is the setup for everything else. |

Design consequences, all of them wanted:
- Driving to Cleveland is now one trip that services jocks *and* stocks shops. The day sheet becomes
  a real route-planning problem: six shops, an op, and two stations, and you have ten hours.
- Cold discovery stops being phone-only. `WorkThePhones` remains the way to find a jock in a city
  you cannot reach; the lobby is the way to find one you can.
- The `RivalPressure` approach gets teeth, because two stations in one town are now two stops you
  visited in one afternoon.

---

## 5. Phase 3 -- the mailing

The office-bound, cheap, weak version of §4. This is the only way to touch a market you cannot drive
to, and it should feel like what it was.

```
MailPromoCopies(recordId, regionId, count)
```

- **Home only.** Costs `ActionCosts.Planning` (2h) for up to ~25 pieces, plus an hour per further 25
  -- typing labels and stuffing envelopes is the cost.
- Consumes `count` promo copies; charges `MailerCostPerCopy` (~$0.14: a record mailer plus period
  postage). A 50-piece mailing is about $7 -- trivial cash, 50 copies you cannot sell, and half a day.
- Picks up to `count` reporter stations in `regionId` that are not already serviced.
- **Most of it lands in the bin.** Roll a landing chance per station, ~20-35%, raised by
  `Label.reputation`, by whether the record has *any* charted history, by a trade review (§6), and
  by whether a Rolodex relationship with that station already exists. A station that does not land
  gets no servicing row at all and the copy is gone.
- A station that *does* land gets `conviction ~ 0.2` -- enough to clear `Objection.NotServiced` and
  nothing more. A small chance the jock's card enters the Rolodex unprompted, which is the
  historically correct payoff: sometimes somebody actually calls you.

This is deliberately a bad deal per copy and an unbeatable deal per mile. That tension is the point.

---

## 6. Phase 4 -- the trades

A new player-only surface, `TradePress`, living on the OFFICE tab. Three objects, one of which
cannot be bought.

### 6.1 Send it to the review desk
Free but for one promo copy and postage; `ActionCosts.Paperwork`. One submission per record. Resolve
after 1-2 weeks into an outcome weighted by the record's real `hookStrength` / `productionQuality` /
genre fit against the year, plus a small term for label standing:

| Outcome | Effect |
|---|---|
| **Spotlight / Pick** (rare) | Big multiplier on `InboundCall` generation for ~4 weeks (one-stops, houses, out-of-region shops), a real bonus on mailing landing chance (§5), a grounded new Rolodex counter ("Cash Box picked it"), and a bonus to house-visit acceptance (distribution directive §5). |
| **Four-star / Best Bet** | The same, smaller. |
| **Two-line mention** | A modest mailing-landing bonus and a usable, weak talking point. |
| **Nothing** | Nothing. Most records got nothing. |

### 6.2 Buy a trade ad
Three tiers -- **$75 / $250 / $600** (quarter-page / half-page / full-page). Unlike the rest of §11,
these are **not** scaled to $800 of founding capital -- they are what the space actually cost. A
weekly trade paper at Billboard/Cash Box circulation (a few tens of thousands, all industry --
distributors, one-stops, jocks, other labels, not consumers) ran roughly this range for display
space in the early 1960s: a small quarter-page or "trade note" in the tens of dollars, a half-page
in the low hundreds, a full page pushing toward a thousand. Exact contemporary rate cards were not
independently verifiable during authoring (the primary archives are paywalled/OCR-garbled), so
treat these three numbers as the era-grounded estimate, not a cited rate card -- revise if a real
rate card surfaces, but do not re-derive them from `FoundingCapital` again.

The consequence is deliberate and sharper than the old scaled figures: a full page is most of an
$800 label's cash on hand in one move, not a bounded "genuine gamble" tier -- it is a bet-the-label
move that should feel financially reckless even before the record's odds are considered, exactly as
it would have been for a real 1960 one-man label. Reaching for it because there's a hot record
should be a real decision, not month-one furniture.

An ad is **not consumer advertising and must never be modelled as one.** For 3-5 weeks it:
- multiplies `InboundCall` probability, weighted toward `OneStopTest`, `HouseInterest` and
  `AdjacentCity` reasons -- the trade-reading counterparties;
- adds a small bonus to cold Rolodex connect rolls (the switchboard has seen the name) and to
  `CommercialPitch`;
- adds a bounded lift to the mailing landing chance in every region.

It adds **zero** direct awareness and **zero** direct units. If the record is a stiff, the $400 is
simply gone, and that is the honest historical outcome.

### 6.3 The regional breakout listing -- not for sale
When a player record crosses the breakout evidence bar the model already computes for a region
(`RegionalRecordData.peakBreakoutScore` against `regionalBreakoutDealThreshold`, the same bar
`CompetitorManager.GetProvenBreakoutRegions` applies), it appears in the trades' breakout column.

This is the loudest inbound generator in the game and it costs nothing, because it cannot be bought.
It is the payoff moment for a small hit: the office phone starts ringing from cities the player has
never driven to, out-of-region stations become mailable with a real story attached, and it is the
in-fiction reason a house or a major suddenly knows the label's name. Surface it as a readable
weekly trade page -- other labels' breakouts included, drawn from the same live data -- because a
player who can read the column can see a rival coming.

---

## 7. Phase 5 -- reporting outlets and the hype

The chart is already a survey of ~110 outlets (`ChartSimulator.cs:210`). Make the player able to see
and touch that fact locally.

### 7.1 Who reports
Mark a small subset of stops as reporting, derived deterministically from authored data on the
existing isolated stop seed stream: `PlayerStop.ReportsToStationIds` (a shop that phones its numbers
to the local Top 40 survey) and `PlayerStop.ReportsToTrades` (the one-stop, and the one or two
biggest dealers in a hub). Keep it **small and legible** -- one or two per city; the whole point is
that they are identifiable.

The "Ask what's on the survey" verb (§4) and a shop conversation both reveal who reports. That is
the information the early game is actually about.

### 7.2 The honest verb: get the report right
Free, relationship-gated, at a reporting shop: **ask the dealer to put it on his report.** He will
if (a) he actually has stock, (b) it is actually moving off his counter, and (c) the relationship is
warm. All three are numbers that already exist on the stop. Effect: a bounded, player-only nudge to
the local station's candidacy for that record (through `StationAdvocacyService`, exactly as a won
Rolodex call does) plus a small chance of a trade-listing mention. It is a real, legal, load-bearing
reason to care about *which* six shops you serviced.

### 7.3 The dishonest verb: hype the count
A `TheFixer`-gated verb, visible only above an instinct bar, at a reporting stop: **buy your own
record off the counter.**

Model it honestly, because the sim already supports the honest version and a fake number would be
unforgivable here:
- The player pays `SinglePrice` (**$0.89**) per copy, N copies, cash now.
- Those copies leave the shop's `OnHand` lot as genuinely sold. The shop's sell-through is real, so
  its report is real. **No number anywhere in the sim is falsified.**
- **The label books no revenue on them.** You are buying through kids, cousins and a third party;
  the label eats the full list price. Net cost is $0.89/copy -- 25 copies is about $22 -- and the
  lot now needs restocking out of stock you paid the plant for.
- The units *do* count, because they were bought. This self-balances: against a market where a chart
  slot costs on the order of half a million units nationally, hyping is economically hopeless as a
  chart strategy and only ever works as a **local survey** play. That is exactly what it was.
- **Detection.** A dealer notices the same man buying twelve copies. Roll against relationship and
  the era (`media.payolaSusceptibility`, worst in 1960-61 with the hearings live). Getting caught
  burns the stop (permanent `PassedRecordIds` plus a relationship floor) *and* the station it
  reports to (reuse the existing `payolaBurned` / `professionallyBurned` channel-burn pattern). A
  burned reporting dealer in your home town is a genuinely bad week.

> **Known model quirk, flagged not fixed:** `BookSale` books trunk gross at full retail
> (`SinglePrice = 0.89`, `PlayerDesk.cs:3231`) with no dealer margin, so a naive "buy it back and
> re-book the sale" implementation would cost the player only the artist royalty and be nearly free.
> That is why the verb above books **no** revenue. Do not route hype copies through `BookSale`, and
> do not "fix" the retail/wholesale spread on this branch -- the player economy is calibrated as it
> stands.

---

## 8. Phase 6 -- the record hop

The legal answer to payola, the era-correct DJ relationship, and the first time the roster is a
promotional asset rather than a cost centre.

Unlock: a Rolodex jock at `DiscoveryState.Trusted` or with rapport over a bar, in a city with a
`Venue` stop. New approach on the existing call, `RolodexApproach.AskForHop`, or a verb at the venue.

- **He MCs, your act appears.** Costs the act a night and the player a day and the drive; a token
  fee ($0-25) or nothing. The jock keeps the gate -- that is his payment, and it is why it is legal.
- **You sell at the table.** Reuse `WorkTheHopTable` wholesale, with a real multiplier: a hop with
  the act on the bill moves several times what a table on its own does.
- **He has now watched a room react.** This is the payoff: a large rapport gain, servicing at
  `conviction = 1.0`, and a strong, *earned* advocacy write -- the biggest legal advocacy in the
  game, and unlike payola it cannot be busted.
- **Regional awareness** through `ChartManager.AddAwareness` on the region, bounded and modest.
- **It can go badly.** A weak act in front of a room is a rapport *loss*. Gate the outcome on real
  act numbers (`cohesion`, live-set quality, the record's hook) so booking a hop for a bad act is a
  mistake the player can make.

Era weighting: hops are strongest 1960-63, fade through the mid-decade, and are largely gone by
1967-68 -- the same shape as `PlayerStopFactory.JukeboxEraCurve`. Author it as a second keyframed
curve next to that one so the two read together.

---

## 9. Phase 7 -- the counter and the window

Small, cheap, and the reason a serviced dealer outsells an unserviced one.

- **Window cards / counter display** -- a per-city print buy (~$8-20) plus a promo copy or two, or a
  free-goods sweetener at an individual shop. Effect: a bounded lift on that stop's sell-through
  appeal term and a relationship tick. Modest, cheap, and stackable across a route.
- **In-store appearance** -- the act signs copies on a Saturday. Costs the act's day and the
  player's; a real spike in that stop's `Placed` lot and a jump in the stop's relationship, plus a
  small regional awareness write. Requires an act with some local standing, so it is the *second*
  thing you do in a town, not the first.

---

## 10. Phase 8 -- servicing a record that is moving

The small-hit loop. Everything above is the cold-start; this is what changes when it works.

- **New Rolodex counter: `SuitSurvey`** -- "It's number four on the WAMO survey." Instinct-gated on
  `TheSuit`, fact-gated on a real out-of-region position (the station-survey read from §4, or the
  §6.3 breakout listing). Available as an explicit bluff when the fact is not there, and a jock with
  reach can and should check. This is the single most valuable counter in the game because it is the
  only one that converts a win in one city into a win in another.
- **Second-market mailing converts.** The §5 landing chance rises materially once a review, an ad,
  or a breakout listing exists -- a mailing with a story behind it is a different object from a
  mailing without one.
- **Keeping what you have.** `RegionalRecordData.stationsDropped` is a **one-way latch by design** --
  a station that cuts a record never re-adds it. Surface that: a station whose spin tier is sliding
  should be visible on the Rolodex card *before* it drops, so that "drive back to Cleveland this
  week or lose the market" is a decision the player gets to make and can lose.
- **The promo squeeze.** Servicing twenty new cities means a repress struck all-promo, paid to the
  plant up front, while the house paper from the breakout is twelve to eighteen weeks out
  (`WholesaleReceivable`, `paymentTermWeeks`). The factor / master-lease / P&D buttons from the
  distribution directive §8-§9 are already there. Promo spend is the thing that walks the player
  into that trap, which is exactly what it did.

---

## 11. Prices and costs

All player-only. Every one is a one-off or per-copy; **no new recurring cost** (invariant 6).

| Thing | Cost | Also spends |
|---|---|---|
| Promo copies | vinyl only (~$0.25 ea, already in `PressingCost`) | the sale you will never make |
| Mailer + postage | ~$0.14 / copy | 2h per ~25 pieces, home only |
| Drop off a copy | -- | 1h + 1-2 promo copies + the drive |
| Wait for him | -- | 3h + 1-2 promo copies + the drive |
| Trade review submission | ~$0.15 postage | 1h + 1 promo copy |
| Trade ad | $75 / $250 / $600 (era rate, not budget-scaled -- see §6.2) | 1h |
| Window cards, per city | $8-20 | 1h + 1-2 promo copies |
| Record hop | $0-25 | the act's night + the player's day |
| In-store appearance | $0 | the act's day + the player's day |
| Hype the count | $0.89 / copy, unrecovered | the copies leave the lot; detection risk |
| Project promo man (**exists**) | $25 / $50 / $75 | 2h |
| Ad buy through a jock (**exists**) | $150 / $400 / $800 | in-call minutes |
| Payola (**exists**) | $200 / $500 / $1000 | in-call minutes + the ledger |

Sanity check against `FoundingCapital = 800` and `CreditFloor = -225`: a complete, competent first
campaign -- 500 pressed with 120 promo ($183), a 50-piece mailing ($7), a review submission ($0.15),
a $75 quarter-page trade ad, window cards in two towns ($30), and gas -- is about **$305**, leaving
the player solvent with stock in the trunk. A player who instead buys the $600 full-page ad and a
$500 envelope on a debut by an unknown act is broke, and *should* be -- the full-page tier alone is
most of the founding capital in one line item. That is the intended lesson and the credit model
already enforces it.

**On the release-screen campaign budget.** `SetReleaseDate`'s `marketingBudget` currently buys
national awareness through the shared `ApplyReleasePromotion` path. Leave that function alone. What
should change is the *player-facing framing*: default the field to $0 and present it as what an
$800 label could actually buy at launch -- shipping samples and a trade announcement -- so that the
awareness a player record earns comes from the verbs above, not from a slider. This is a UI and
default-value change only; no shared math is touched.

---

## 12. Decade weighting

Drive off year plus authored flags; change no AI curve.

| Years | Promotion looks like |
|---|---|
| 1960-62 | Hand-delivered copies, hops, black radio, the trade review desk. Payola is at maximum heat -- the hearings are live and the federal amendment lands September 1960. Hops are the currency. |
| 1963-65 | Top 40 tightens; the trade breakout column matters more; mailing lists get longer and convert worse; the survey is king. |
| 1966-67 | Hops crater with the jukebox route; FM underground appears where `media.hasFMUnderground` is authored and wants a different pitch entirely (album cuts, not a 45 with a hook). |
| 1968-69 | Album priority; a 45-only promo campaign is a hobby, same as the distribution directive says of a 45-only trunk. |

Integration gating on *which* stations will see the player is already the
`MarketRegion.IntegrationProgressCurve` / `GetEraIntegration(year)` the distribution directive uses
for stops. Reuse it here for station stops; do not author a second curve.

---

## 13. Unlock order

1. Promo stock split + `Objection.NotServiced` (§3). Nothing else lands without these.
2. Station stops on the day sheet + drop-off / wait-for-him (§4). The core feel.
3. The mailing (§5) -- the bad, necessary reach.
4. Trade review desk + the breakout listing (§6.1, §6.3). Free; the first payoff.
5. Reporting outlets and the honest report verb (§7.1-7.2).
6. Trade ads (§6.2) -- once there is something worth announcing.
7. Record hops (§8) -- gated on a real relationship.
8. Counter display and in-store appearances (§9).
9. `SuitSurvey` and the second-market loop (§10).
10. Hype the count (§7.3) -- Fixer-gated, late to appear, worst-in-1960.

---

## 14. Implementation wall

**Do**
- New **player-only** types: `RecordServicing`, `TradeSubmission`, `TradeAd`, `StopKind.Station`,
  `PressStock.PromoRemaining`, reporting flags on `PlayerStop`. Persist every one in
  `PlayerSaveData` (`SaveGameService.cs`) with the same From/To pattern the stop / call / runner
  state already uses -- and remember `RebuildRadioForLoad` **wipes** runtime panel state, so
  servicing rows must be snapshotted the way cultivated rapport already is (the snapshot/restore
  pass in `PlayerDesk.RolodexVerbs.cs`).
- Route every effect through hooks no AI path calls: `ChartManager.AddAwareness`, `AddRadioPlay`,
  `PlayerSpinNow`, `StationAdvocacyService`, `PlacePayolaCash`, `PlaceProjectPromo`, `InboundCall`
  generation, `PlayerStop`.
- Generate station stops by **projecting** `ChartManager.ReporterStationsInRegion` -- read-only. Any
  new random draw runs on the isolated `PlayerStopFactory` stream or on a player-action-only path
  (the `WorkThePhones` / `NameGenerator` precedent: never called from the weekly sim).
- Extend the existing `Objection` / `CallCounter` ladders rather than adding a parallel scene.
- UI: promo pool on the plant ticket, station stops on the city day sheet, a trade page on OFFICE,
  a servicing column on the Rolodex card.

**Do not**
- Touch `ApplyReleasePromotion`, `GetSeasonalMarketingImpact`, `ChartSimulator.UpdateAwareness` /
  `UpdateRadioHeat` / `GetCampaignImpact` / `GetReleaseRampWeight`, the Hesbacher survey math,
  `REPORTER_PANEL_WEIGHT`, `StationNetwork`'s AI playlist meeting, `IndependentDistributorFactory`,
  or any genre / integration / demand curve. If a player lever seems to need one of these, it is
  the wrong lever.
- Falsify a number to make a mechanic work. §7.3 exists in its exact form because the honest version
  was available.
- Let a trade review or a trade ad add units or consumer awareness directly (invariant 4).
- Re-add a record to a station that dropped it. The latch is one-way for a measured reason
  (`RegionalRecordData.cs:30-39` -- returns to #1 are already the chart's largest defect).
- Convert promo stock into sellable stock, or the reverse.
- Introduce a recurring cost, or soften `CreditFloor` to afford one.
- Rebuild the Rolodex. It is the radio half and it works.

**Verification.**
1. **AI inertness by byte comparison.** Run the canonical decade probe with
   `--enable-genre-market-v2 --enable-artist-population-lifecycle` before and after, same seed,
   and hash the emitted CSVs. They must be **identical** -- no player exists in a headless run, so
   every line of this branch is unreachable there. Any diff means something leaked into shared code.
   Redirect to a file; do not pipe a long headless run (it hangs). Build `-c Debug` first --
   headless loads `bin/Debug`.
2. **Save round-trip.** `SaveLoadRoundTripRunner --inspect-slot=<name>` over a save carrying promo
   stock, servicing rows, an open trade submission and a live ad, to prove none of it is wiped by
   `RebuildRadioForLoad` or lost in the From/To pattern.
3. **A played hour.** The real test is a 1960 start driven by hand: press 500/120, service the two
   home-town stations on foot, mail the region, submit to the review desk, and see whether the first
   record's fate feels like it was decided by decisions rather than by dice.
