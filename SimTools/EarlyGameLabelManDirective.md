# Early-Game Label Man Directive — the opening month, the cash lock, and the trunk

Branch: `early-game-label-man` (cut from `codex/studio-experience-expansion` @ 5f85ba3, 2026-10-14).
Status: **directive only — no code written.**

This directive covers three things the author asked for together:

1. Can a new label still **cash-lock** in the opening months, the way Opus's playtest label did before
   the UI overhaul? (Answer: yes. The UI now warns about it, but the lock itself is still there. See §1.)
2. **What does a label man do in the 14–30 days between cutting a master and getting the vinyl back?**
   There should be real work here, but no day jobs. (§2 is the history; §4–§8 are the verbs.)
3. Is the **trunk-selling** simulation high-fidelity now that cities have real music scenes?
   (Partly. The shop side is strong. The venue side is still synthetic and ignores the scene. See §9.)

Sources: the Opus first-five-years playtest (`C:\Project\Label-Man-playtest\Playtest\PlaytestLog.md`),
`~/Downloads/Playtest-Action-Backlog.md` (P1.7, P1.8, P2.4), the code on this branch, and the
research cited in §2 and §12.

---

## §0 Charter and invariants

1. **Player-only.** AI-only runs never instantiate the player, so they must stay byte-identical.
   Prove it with the probe-run hash, as earlier player-side directives did: 52 weeks, seed 1001,
   `--enable-genre-market-v2 --enable-artist-population-lifecycle`, comparing every CSV in SimLogs.
2. **No day jobs.** The author rejects "go work a part-time job." It may be realistic, but it breaks
   the fantasy of a founder devoted to the label. Every verb here must be **label work**: it has to
   move the label's position (cash, relationships, market knowledge, or the act's name) inside the
   record trade. The test: *would a 1960 trade paper write it up as the label's business?*
3. **No free money.** Every cash source must cost something real and certain: repayment on a fixed
   schedule, part of the future (a master, a share of net, publishing), or hours that could have
   gone elsewhere. The plant-credit rule applies: the lender collects on schedule whether the record
   sells or not (`SettlePlantCreditIfDue`, `PlayerDesk.cs`).
4. **No new buzz meter.** Read the state that already exists:
   - `regionalData` / `ReporterAirplay` for airplay and awareness
   - `LocalSceneRoomService` bills and room history for live work
   - `SceneCityPlacement` for a city's genre strength
   - `PlayerStop.Relationship` / `BuyerNerve` / `AudienceLean` for accounts
5. **Waiting must pay off later.** The best wait-month verbs plant something that only grows once
   the vinyl lands: a following in a room, a shop's advance order, a DJ who has already heard the
   acetate. Cash-now verbs should exist, but each should trade away some of the future.
6. **Ground each mechanic in a documented period practice** and cite it in the implementation log,
   the same way the studio-expansion branch does.

---

## §1 Audit: what the opening looks like in code today

### 1.1 Opening arithmetic

| Line item | Where | New York (Ex-Musician) | Detroit | Billings |
|---|---|---|---|---|
| Founding capital | `FoundingArchetypeData` | $800 (Pawn $1,400, Insider $900) | same | same |
| Monthly overhead | `$35 + CityProfile.Rent` | **$175** | $80 | $53 |
| Signing reserve (2× overhead) | `AILabel.CanAffordToSign`, `OfferPlanning.cs` | $350 | $160 | $106 |
| Credit floor (−3× overhead) | `PlayerDesk.CreditFloor` | −$525 | −$240 | −$159 |
| Budget room, 3h minimum | `SessionCost` (rate scales with `CityProfile.Studio`) | ~$60 | ~$48 | ~$36 |
| First pressing (500) | `PressingCost` | $128 | $128 | $128 |
| Acetate | `CutAcetate` | $20 | $20 | $20 |
| Plant lead time | mail 1–4 + plate 3–5 + queue 7–14 + ship 3–7 | **14–30 days** | | |

The cheapest first record in New York (one club act, a budget room and a 500 run) costs about
$250–320. That leaves roughly $500, and $175 of it goes out on the 1st with nothing sold yet. **One
record is survivable anywhere.** Most real players won't stop at one.

Since the playtest, city profiles raised New York's overhead from $75 to $175. The studio-expansion
branch also added optional costs to every session: engineers, session players, backing singers,
overdubs, tape stock, evening rates. So the playtest's path to $32 is *easier* to reach now, not
harder.

### 1.2 Lock taxonomy

**L1. Hard lock (terminal).** All of these hold at once:
- cash is below the cheapest press run (a first run of any assembled single, $128, or a repress);
- no sellable stock anywhere (office `PressStock.Remaining` plus every stop's lot);
- no `PressOrder` in flight;
- no stop `OpenBalance` and no wholesale receivable.

From there, *no player verb produces cash*:
- tables, shops, the one-stop and the act buy-in all need pressed stock;
- `SellMaster` / `LeaseMaster` need a one-stop to know the title *and* regional radio;
- `RequestPlantCredit` needs ≥250 copies of open call demand;
- `PursueDistributionDeal` needs regional evidence.

The only inflow left is the band room's residency (`RunRoadBookings`, $22/week to the label, about
$95/month, gated on `BandLife.RosterChurn`). That covers a Billings office but not a New York one
($175). **A New York hard lock is certain death in about four months.** Overhead pushes the label
past the credit floor, or it hits three consecutive red month-starts.

How to get there: sign two acts, book two sessions, and assemble a second single before pressing
the first. The UI's `WarnIfUnderOverhead` warns on the session and on the pressing. It does not
warn on signing, scouting appointment fees or studio extras. And it never says the deadly part:
*"after this you cannot afford to press anything."*

**L2. Plant-wait drought (structural, happens every game).** From the master until the vinyl lands,
14–30 days, no verb turns anything into money. `WhileYouWait` (`UI/PlayerDeskPanel.cs`) lists
everything the desk can honestly offer right now:
- work the phones,
- cut or play an acetate,
- work up songs,
- scout.

Playtest: *"11 of 13 days read 'a quiet day at the office.'"*

**L3. Stockout drought.** A first-run sellout plus a 14–30-day repress leaves no income. Meanwhile
reorders expire (`CallExpiryWeeks = 3`) and the reporter panel drops the record for lack of sales.
The playtest lost WAVY this way (Feb 8–29).

**L4. Red-ink sampling.** `monthsInTheRed` checks cash only on the 1st (`OnMonthChanged`). A label
that spends all of February in the black but is red on Feb 1 and again on Mar 1 counts *two
consecutive* months. The code is correct, but the rule is invisible and harsh (playtest: *"the
countdown doesn't reset"*).

### 1.3 Fixed since the playtest (do not redo)

- NEXT UP adapts to affordability (`NextUpStep`).
- `WarnIfUnderOverhead` on pressing and studio.
- `WhileYouWait` list.
- Low-stock warning (`LowStockWarningThreshold = 75`).
- Fixed-height red-ink chip, "IN THE RED n/3".
- Pressing-arrival announcement and skip stops.
- Release date defaults to arrival + 1.
- A $0 campaign no longer blocks dating on negative cash.
- Interruptible skips.
- Pushover and lowball negotiation floors.
- Acetate on a Rolodex call (`RolodexApproach.PlayTheAcetate`).

### 1.4 Acetates already reach DJs (the author asked)

Yes, there are two routes:
- **In person.** `CutAcetate` ($20, 1h, one per master), then `DeliverAcetateToStation` at a
  station in the current region. This services the record at conviction 0.9 (`HandDelivered`). With
  hook ≥ 0.72 it also raises **pre-order calls** from shops in that DJ's town (`GeneratePreOrders`).
- **On a call.** `RolodexApproach.PlayTheAcetate`, once per DJ per record. It gets his honest ear
  and services nothing.

Both are good. **The gap is the courtesy call.** A DJ reached with nothing on the table still gets
*"You have nothing out to talk about"* (`PlayerDesk.Rolodex.cs`), and the only option is HANG UP.
§6.1 fixes this.

---

## §2 Historical record: what small 1950s–60s label owners actually did

Two patterns repeat across many founders. **Money came from family, partners and lease deals, not
wages.** And most label owners also ran *another counter inside the trade*: a shop, a club, a
studio, a booking office, a jukebox route, or a songwriting desk.

| Who | Activity | What it maps to |
|---|---|---|
| **Berry Gordy** (Tamla, 1959) | Borrowed **$800 from the family savings fund** (the Ber-Berry Co-op): the family voted on it, the loan was cut back from what he first asked, and the paper references 6% interest. | A **backer loan** with a pitch scene, terms and repayment (§4.1) |
| Gordy (1958) | Leased "Got a Job" (Miracles) to End Records and got a famous ~$3 royalty check. Smokey's response: start your own label. | **Shopping a master** is cash now, and the trap is how little comes back later (§5) |
| Gordy (1959) | "Come to Me" sold locally on Tamla, gained Detroit airplay, then was **licensed to United Artists** for national release (sources disagree on the money). Licensed "Bad Girl" to Chess because he lacked national distribution. Later: *"a great deal of the profit was lost"* leasing. | **Local break, then national pickup** is a bigger deal than a pre-release lease (§5.3) |
| Gordy (1957–59) | Wrote hits for Jackie Wilson ("Reet Petite", "Lonely Teardrops") on a three-way split with little pay. | **Song placement** with other labels' acts (§7.2) |
| Gordy and Raynoma Liles (1958) | **Rayber Music Writing Co.**: about $100 bought an aspiring singer a basement session backed by the Rayber Voices, giving them a demo to shop. | A **demo service** for unsigned locals (§7.1) |
| **Sam Phillips** (Memphis Recording Service) | "We record anything — anywhere — anytime": $2 a side for walk-ins, weddings and funerals to cover $150 rent. Leased masters to Chess and RPM ("Rocket 88") before founding Sun. | Demo / custom service (§7.1); leasing masters (§5) |
| **Florence Greenberg** (Tiara, 1958) | The Shirelles' "I Met Him on a Sunday" went to Decca for about $4,000 (lease vs. sale disputed). She used it to start Scepter. | **Sell the first record to fund the label** (§5) |
| **Vee-Jay** (Carter and Bracken, 1953) | Borrowed $500 to found the label (a pawnbroker as lender is often repeated but **unverified**). Ran Vivian's Record Shop; Carter was a DJ. | Backer loan (§4); storefront (§8) |
| **Estelle Axton** (Satellite/Stax) | Mortgaged her house and joined as an equal partner with $2,500. Opened the Satellite Record Shop in the lobby for cash flow, calling it "a workshop for Stax Records" and studying what made records sell. | **Partner equity** (§4.2); **storefront as market research** (§8) |
| **Leonard and Phil Chess** | "Thousands of miles, days and days… car full of records to drop off to every distributor, every disc jockey." Trade was mostly jukeboxes, taverns and shops. | The trunk and DJ work the game already models |
| **Don Robey** (Duke/Peacock) | Owned the Bronze Peacock club and ran the **Buffalo Booking Agency**, which put his acts on the road. | **Booking your own acts** into real rooms for commission (§6.3, §9 F5) |
| **Huey Meaux** | Found acts in clubs and roadhouses, recorded, pressed, pushed regional radio. Once a single broke regionally, **licensed the master and the song** to bigger labels. | Licence after a regional break (§5.3) |
| **Syd Nathan, Herman Lubinsky, Stan Lewis, Johnny Vincent, Bobby Robinson** | All ran record shops (Lewis and Vincent also ran jukebox routes or distribution) before or alongside their labels. Nathan got his first hillbilly/race stock as repayment of a $6 jukebox loan. | Storefront (§8); jukebox ops as early buyers (exists) |

**What the record does not support:**
- Bank loans to record startups. Gordy turned down outside loan terms and went to his family.
- Distributors advancing cash to an unknown label before it had a record moving.

Do not add either.

**Thin evidence, flagged:**
- Exact distributor payment terms in 1960. One secondary source says 90 days, but nothing primary.
- The wholesale price an independent got for a 45. The existing ladder in
  `DealerMarginFlipReturnsDirective.md` stays.
- Pressing-plant credit practices.
- Record-hop economics (who paid whom).

Keep these as authored estimates and label them that way in comments.

---

## §3 Design shape: the wait month is a calendar of groundwork

During the first master's plant wait, the player should be choosing between five kinds of label
work. Each one pays off differently:

| Kind | Cash now? | Pays off when the vinyl lands? | Section |
|---|---|---|---|
| **Backing**: family/partner money | yes | repayment or equity drag later | §4 |
| **Shop the master**: lease or sell to a bigger label | yes | no, you've traded the record away | §5 |
| **Groundwork**: DJs, shops, rooms, the act on bills | no | **yes**: pre-orders, rapport, a following | §6 |
| **The counter**: demo sessions, song placement | small | partly: scouting reads, publishing income | §7 |
| **Storefront** (optional, author's call) | trickle | yes: market read and a home counter | §8 |

When L1 or L3 fires, a **WAY OUT** card (§4.4) lists whichever of these are actually available
right then.

---

## §4 Phase 2: Backers (money from people, not wages)

### 4.1 The backer loan (one per label, early)

- **Source by archetype.** It's the same mechanic with different flavor and terms:
  - Ex-Musician: the family fund (Gordy).
  - Pawn-Shop Owner: borrow against the back room's goods.
  - Promo Man: a club owner or jock he's done favours for.
  - Trade Insider: a distributor's salesman who "knows a guy."
- **The ask is a meeting** (2h, home office), built on the Rolodex beat grammar: approach, one
  objection, grounded-or-bluff answer, roll. Odds are driven by real evidence:
  - a printed master or acetate (hook as the player read it, never the true value);
  - a signed act;
  - for a second ask, whether the first loan is being repaid.

  The family "vote" is the objection beat.
- **Terms.** Principal is $300–800 (scaled off archetype and city overhead, never above founding
  capital), at about 6% simple interest. Monthly repayments start in month 3, over 12 months, and
  are booked on the 1st next to overhead, so the red-ink count sees them.
- **Default.** A missed payment is not game over. It costs the backer relationship (no second ask
  ever), and for the family fund a morale line in the Morning Paper. The money still has to be
  repaid: arrears accrue and are taken first from any later cash windfall (master deal, one-stop
  COD).
- **Save:** `PlayerSaveData.Backer { Source, Principal, Rate, RepaidToDate, NextDueMonth, Arrears, Standing }`.

### 4.2 Silent partner (mid-early game, optional)

The Axton model: cash for equity. It unlocks after the first release, when there's a record to show.
- A local partner (club owner, shop owner, or relative) puts in $1,500–3,000.
- In return they take a fixed share (25–50%) of **positive** monthly net, forever, or until bought
  out at a multiple.
- No repayment schedule, so it can't sink you, but it drains every good month. One partner per label.

### 4.3 Explicitly not included

Banks, loan sharks (they'd turn the mob/payola heat system into a lending system, which is a
separate design), gambling, and selling office furniture.

### 4.4 The WAY OUT card

This replaces the generic red-ink chip text when L1, L3 or cash < overhead holds. It lists only
**live** verbs, in a fixed order, each with its price:
- tables (needs stock),
- artist buy-in,
- backer ask,
- shop the master,
- demo sessions,
- residency.

Each row tab-links to its control. This is the backlog item P1.7, which the playtest asked for
twice. Also add a **hard-lock preflight** to the warning modal: when a spend would put cash below
`PressingCost` of every unpressed assembled single and no stock or press order exists, say so in
plain words ("After this you can't afford to press anything you've cut").

---

## §5 Phase 3: Shop the master (the producer's road)

**Today:** `SellMaster` / `LeaseMaster` exist (`PlayerDesk.cs`, "LATE EXITS"). They are priced as a
multiple of `lifetimeLabelNet`, so a pre-release master is worth the $150 floor, and they are gated
on a one-stop knowing the title plus regional radio. That's a late exit. The period also had an
**early** version, and it was the standard bridge for a label with no money.

### 5.1 The A&R sit-down

- Requires a trip to a city with a national label's A&R office: New York, Chicago, Los Angeles, or
  Nashville (from `CompetitorManager` labels with `LabelTier.Major` / `MidTier` and a home city). It
  costs a day plus the drive. The master or acetate must be in hand.
- The counterparty is a **real AI label**: one with genre fit and roster room
  (`FindRivalToPlayer`-style selection). Its interest reads the record's *true* hook and production
  through a fogged A&R taste, so the player can't "test" quality for free. Expect many passes.
- Offers come in two shapes:
  - **Lease.** A small advance ($150–600 pre-release) plus an override royalty (2–4% of the
    buyer's units, paid quarterly, slowly). The master comes back after N years.
  - **Sale.** Larger cash, no future.

  Either way, the act stays on the player's roster. That's the Gordy/UA shape: UA picked up the
  record and later Marv Johnson's follow-ups, and Motown kept growing.
- **The trap must be visible.** The override is paid on the AI label's settlement and arrives
  tiny and late (the $3.19 lesson). The record charts under someone else's name. Show what the
  record has earned *them* on the record card.

### 5.2 AI-inertness note

A leased record released by an AI label does mutate the AI world, but only when a player exists, so
the AI-only byte-identical proof still holds. **Do not let leased masters enter
`CompetitorManager`'s automatic loops.** Release them through one explicit `ChartManager.ReleaseRecord`
path tagged `leasedFromPlayer`, so a probe can count them.

### 5.3 The national pickup (post-release)

Once a released record shows regional breakout (`peakBreakoutScore` past the house proof gate), the
same A&R office **calls the player** through the existing `InboundCall` seam. The offer is priced
off regional evidence, not `lifetimeLabelNet`, so it is much larger. This is the "Come to Me"
moment and should outrank a pre-release lease. Merge it with the existing `MasterDealEligible` path
rather than keeping two master-deal brains.

---

## §6 Phase 1 (do first): Groundwork, the wait month that pays later

This is the heart of the directive and the cheapest to build, because almost everything exists.

### 6.1 DJ courtesy calls with something to say

Replace the HANG-UP-only branch (`!c.HasRecord`) with three low-stakes approaches. Each is
10–20 minutes, has a cooldown per DJ per week, and gives a small, bounded rapport gain (below a
won pitch):

- **Introduce the label.** Rapport tick plus a line of his taste in his voice. **STREET** reads
  whether it landed.
- **Ask what's moving.** A market read. It returns the top two genre families on *his* station's
  playlist this week and one he's tired of. This is the real `StationNetwork` playlist state,
  rendered as dialogue, and it feeds the studio preview's "Moment" evidence. **EAR** tags it as an
  insight.
- **Ask when to bring it by.** It sets his `callbackDate`/`callbackHour` so the release-week hand
  delivery isn't a "WAIT FOR HIM" lottery.

Persona-stat explanations go on hover tooltips, per the author's preference.

### 6.2 Pre-sell the shops with the acetate

`GeneratePreOrders` already exists, but it fires only through a DJ. Add a **Play it for him** verb at
Shop and Op stops (needs an acetate in hand, the one daily approach, about 1h):
- The dealer listens and may write an **advance order**: a reserved quantity, COD on delivery.
  Strangers get `PreOrderQty` stocked from `PreOrderShopMin..Max`.
- The roll uses `PitchEarPull`'s appeal band plus the existing segregation access split
  (`RetailAccessFactor`).
- On release day, delivering the order pays cash at once, which shortens L2 by the size of the
  advance orders.
- A pass works like `PassedRecordIds` today: it sticks until real evidence.

### 6.3 Put your act on a real bill (live groundwork)

This uses the scene system that already runs nightly 3-act bills in every city
(`LocalSceneRoomService.Rotation`) and has bookers with contact ids (`SceneRoomProfile.ContactId`,
`AskSceneBooker`).

- **Pitch your act to a booker** (1h, during room hours, booker known via
  `IntroduceSceneBooker`/`KnowsSceneBooker`). If accepted, the act is slotted into that room's
  rotation for N nights over the next weeks. Acceptance reads the act's live-set quality, the
  room's genre families, and the booker relationship.
- **Effects, all existing state:**
  - Room history: shows count toward the full-time and traction routes.
  - Member live income goes to the members (`SceneLiveEconomics.MemberLiveIncome`), **not** the
    label.
  - A per-city **following** for the act: a bounded scalar on the player's side, keyed
    (artistId, cityId) and fed by performed nights and attendance.
- **Payoff when the vinyl lands:**
  1. The following lifts `TrunkAppeal` in that city (§9 F3).
  2. On a night your act plays, the room's door table sells the act's record at list, sized by the
     night's attendance (§9 F1).
  3. A DJ who MCs or attends sees the crowd. This reuses `RecordHop`'s advocacy gate with a smaller
     effect.
- **The label's cut.** Only if the player booked it, and then only as a booking commission (10%,
  Robey's agency). See §9 F5 for retiring the flat $22/week residency.

### 6.4 Acceptance for Phase 1

On a scripted opening (New York and Detroit, seeds 1001/1002, one act, one single), the player can
spend ≥70% of the plant-wait days' hours on verbs that change some saved state other than "time
passed." At least one of pre-orders, advance orders, or a room following is non-zero on release
day for a competent policy.

---

## §7 Phase 4: The counter, selling services from inside the trade

### 7.1 Demo sessions for unsigned locals (Rayber / Memphis Recording Service)

- **Who comes.** Each week the city's unsigned acts (real `SimulatedArtist`s from the scene, which
  have ambition and wealth) may book **a paid demo session** at the label. Demand is drawn from
  scene acts with wealth ≥ a threshold and no label, and scales with the city's `Studio` value and
  the label's room renown. Pull it from the existing scene population. Never invent clients.
- **Economics.** The client pays a flat fee (about $35–100 by city; NYC is highest). The label pays
  the room hours at its normal rate (`StudioHourlyRate`), so the margin is the fee minus the room.
  Optionally, a 100–300 custom pressing billed at plant cost plus a margin. The player spends the
  session hours, but the studio-expansion machinery already handles the date.
- **Why it isn't a day job.** It's the label's own studio work, and the player **hears the act
  play**. The demo yields a scouting read (`Prospect.Learned` quality) on someone the scouting rooms
  might never surface. Sometimes the best client becomes the next signing. That's the Elvis $3.98
  story, without naming Elvis.
- **Ceiling.** Demand-limited (a few clients a month) so it covers part of overhead but never
  replaces selling records. Size it by closed form: in NYC, expected monthly margin should sit at
  30–60% of overhead with diligent play, not 100%.

### 7.2 Song placement (plugging your own catalog)

- Applies when a signed act writes originals and the label holds publishing
  (`publishingControl = LabelAffiliate/LabelBuyout`).
- **Plug the song** at a national A&R office (the same trip as §5.1) or to a `MusicPublisher` with
  `songPluggerSkill`. An AI act may cut a cover.
- **Pays:** a small advance on placement ($25–100), then mechanicals on the AI record's units via
  `MechanicalRoyaltyService` (the label as publisher), plus the publisher's half of performance
  money if that's modelled. It's slow, small and certain to be late, which is the Jackie Wilson
  lesson.
- **Cost:** the song is now someone else's record too. If the AI cover charts first, the
  publishing directive's cover-threat logic applies to the player's own act.
- Gate: must not change any AI material-choice draw in AI-only runs. Placement is injected only
  via a player-created candidate.

---

## §8 Phase 6 (author decision): The storefront

Seven of the founders in §2 ran a record shop alongside their labels. Axton called hers the label's
workshop. It's label work by the period's own definition, but it is a big feature and the closest
thing here to "a second job," so it needs **the author's call before building**.

If built:
- Rent a counter (+$40–90/month by city). It sells all records, not just yours, at retail margin,
  driven by local `marketUnits`.
- Your own singles sell there at list with no dealer cut.
- It produces a weekly **WHAT MOVED** read: top genre families and titles in this neighbourhood,
  shown on the studio preview as "Moment" evidence (§6.1 shares the plumbing).
- It must be staffed: either the player's hours, or a clerk ($25–35/week), which makes it a
  mid-game hire under the solvency rule in `DistributionExpansionDirective.md`.

---

## §9 Trunk fidelity review

The shop-and-op model is strong. These are all period-true and well tested:
- named accounts with `BuyerNerve` and `AudienceLean`,
- the segregation-era *split* access,
- COD vs. consignment,
- inbound calls, the runner, one-stops and returns,
- the dealer price ladder.

The gaps are in how the trunk meets the **city**.

**F1. Venue stops are synthetic (HIGH).** `PlayerStopFactory` invents up to 4 "church & hop tables"
per city from region weights. They have no nights, hours, capacity, admission or community, and no
connection to the scene rooms that now run nightly bills.
- **Change:** project Venue stops from `SceneRoomCatalog` the way Station stops are projected from
  the reporter panel. Clubs, roadhouses, coffeehouses and community halls become door tables.
  `CommunityHall` covers the church side.
- Table sales then read that night's real bill:
  - open nights (`SceneLiveEconomics.OpenOn`),
  - attendance (`Capacity` × the night's draw),
  - the room's genre families vs. the record's genre,
  - **whether your act is on the bill.**
- Keep a `VenueTakeCeiling`. Old saves: map an existing Venue stop to the nearest room by community,
  or drop it with its lot returned to office stock.

**F2. Tables never saturate (HIGH).** Playtest: four tables made 25–30 copies a day, "every day,
indefinitely," about $600–800/month, and that became the dominant, boring routine.
- **Change:** each room keeps a buyer pool for a title, about attendance × an interest share.
  Copies sold come out of it, and it refills only as the room's audience turns over (weekly, faster
  on rotation-heavy rooms).
- First nights sell well; the fifth visit that week sells little. Fridays and Saturdays outsell
  Tuesdays.
- Size the monthly closed form so diligent table work covers about 1–1.5× overhead in a mid-size
  city: a real lifeline, not an engine.

**F3. Counter appeal is blind to the town (MEDIUM).** `TrunkAppeal` is
`hook·0.45 + production·0.25 + buzz·0.30 + windowCard`. Detroit and Billings shops read a Country
side identically, apart from airplay.
- **Change:** add a bounded **local taste** term from `SceneCityPlacement`'s implied genre share
  for that city (normalised against the national mean), and a **following** term from §6.3.
- Both are additive and capped (max ±0.08 to appeal), the same discipline as
  `TrunkReporterAirplayLift`.
- Both also feed `EarForRecord`, so the order size and sell-through stay in agreement.

**F4. Metro shops bought through distributors (LOW, fidelity note).** In 1960 a hub-city shop mostly
ordered through its independent distributor or one-stop. Hand-selling direct was a hometown and
small-town practice. Today the player can work 14 New York counters direct on day one.
- Don't remove this; it's the game's opening loop.
- Once the player has a house line placed in that region (`PlaceLine`), a shop there **may** say
  "get it to me through [distributor]." The sale is still booked, but as a wholesale receivable at
  distributor price.
- This makes the line a real channel shift rather than a parallel one. Defer until the line is
  commonly reached in playtests.

**F5. Residency pays the label (MEDIUM, fidelity).** `RunRoadBookings` books $22/week (residency) or
$30/week (tour) straight into `Label.cashReserves`. In the period, gig money belonged to the act.
A label owner saw some of it only as booker or manager (Robey's Buffalo agency, Gordy's later
management arm).
- **Change:** with §6.3 in place, route residency and tour pay to members (the existing
  `MemberLiveIncome` path). The label takes a 10% booking commission on engagements it booked.
- Keep `BookRoad` as the road-load control for `BandLifeService`.
- **This lowers an existing L1 escape**, so it ships with §4.4's WAY OUT card and §7.1, never alone.

**F6. One-visit-a-day per account holds, but a day's route is still about 20 clicks (LOW, UX).**
Backlog P1.8's "work the route" batch is still open. Fold it into the F1 rebuild: the city day sheet
gets a **WORK THE ROUTE (n hours)** plan. It runs the player's chosen verb per account kind in
order and stops at the hour budget. It uses the same functions, so it adds no new math.

**F7. Red-ink sampling (LOW, rules).** Keep "consecutive month-starts," but show the rule where the
count lives ("checked on the 1st of each month"). Also count a label as recovered if it spent ≥15
days of the prior month in the black. That fixes the playtest's "the countdown doesn't reset"
without softening the run's end.

---

## §10 Phase 0 (before any of the above): Measure the lock

Build `SimTools/OpeningCashLockRunner` (headless, player present, scripted policies). It **reports
only**; it never fixes anything.

**Matrix:**
- 4 archetypes × 5 towns (NYC, LA, Detroit, Memphis, Billings) × seeds 1001/1002.
- Four policies:
  - **frugal:** 1 club act, budget room, 500 run, tables.
  - **eager:** 2 acts, mid room, 2 singles.
  - **playtest replay:** a $350 advance plus two sessions.
  - **studio-extras:** session player plus engineer.

**Report per run:**
- the day L1 is first true (if ever) and whether it was escapable;
- L2 idle hours (hours on verbs that changed nothing saved);
- L3 stockout days;
- the game-over month;
- cash at days 30/60/90.

**Baseline expectation to confirm or refute:** L1 is reachable under *eager* in New York and Los
Angeles and not under *frugal* anywhere. Write the numbers into this doc before Phase 1 code lands.
Re-run after every phase. Acceptance: `frugal` never L1, `eager` L1 rate halves after Phases 1–2,
and no policy can reach a state where WAY OUT lists nothing.

Fixture rule: anchor scripted asks and costs to the constants, never to literals.

---

## §11 Build order and gates

| Phase | Content | Depends on | Gate |
|---|---|---|---|
| 0 | Lock probe (§10) | n/a | numbers recorded here |
| 1 | Groundwork: courtesy calls, shop advance orders, act on the bill (§6) | scene rooms | §6.4; AI byte-identical |
| 2 | Backers + WAY OUT + hard-lock preflight (§4) | Phase 0 | lock probe; save round-trip |
| 3 | Venue projection + table saturation + local taste (§9 F1–F3) | Phase 1 | table closed form ≈1–1.5× overhead; old-save migration |
| 4 | Demo sessions + song placement (§7) | studio expansion | margin 30–60% of overhead; AI material draws unchanged |
| 5 | Shop the master + national pickup (§5) | Phase 2 | leased-master count probe; one master-deal brain |
| 5b | F5 residency → commission | Phases 1, 2, 4 | lock probe shows no new L1 |
| 6 | Storefront (§8) | author's go | n/a |

Every phase must:
- prove AI-inertness with the 52-week CSV hash;
- pass `SaveLoadRoundTripRunner --integration`;
- extend `--inspect-slot` reads where new state is saved;
- build `-c Debug` before headless runs.

---

## §12 Sources

- [Berry Gordy, Wikipedia](https://en.wikipedia.org/wiki/Berry_Gordy) ($800 family loan; UA pickup)
- [Motown, Wikipedia](https://en.wikipedia.org/wiki/Motown) (Tamla regional release, UA licensing, "profit was lost")
- [West Grand Blog: 20 Milestones in Motown History](https://www.adampwhite.com/westgrandblog/2018/1/11/20-milestones-in-motown-history) (6% interest on the family loan)
- [Michigan Rock and Roll Legends: The Birth of Motown](https://michiganrockandrolllegends.com/blog/403-ch-10-the-birth-of-motown) (Ber-Berry Co-op mechanics)
- [Classic Motown: Smokey Robinson's 'Job' of a Lifetime](https://classic.motown.com/story/smokey-robinsons-job-of-a-lifetime/) and [American Songwriter](https://americansongwriter.com/5-fascinating-berry-gordy-facts/) (End lease, $3.19 check)
- [Bad Girl (The Miracles song), Wikipedia](https://en.wikipedia.org/wiki/Bad_Girl_(The_Miracles_song)) (Chess licence)
- [Gwen Gordy Fuqua, Wikipedia](https://en.wikipedia.org/wiki/Gwen_Gordy_Fuqua) (Jackie Wilson songwriting split)
- [Soulful Detroit: Rayber Music Writing Company](https://soulfuldetroit.com/web01-soulfuldetroit/motown2.htm) ($100 demo sessions; secondary)
- [Encyclopedia.com: Sam Phillips](https://www.encyclopedia.com/people/literature-and-arts/music-popular-and-jazz-biographies/sam-phillips) ($2/side service, leased masters)
- [Stereophile: Sam Phillips obituary](https://stereophile.com/content/sam-phillips-recording-pioneer-dead-80) (motto)
- [The Scepter/Wand Story, BSN Pubs](https://bsnpubs.com/scepter/scepterstory.html) and [Florence Greenberg, Wikipedia](https://en.wikipedia.org/wiki/Florence_Greenberg) (Tiara → Decca $4,000 → Scepter)
- [Vivian Carter, Wikipedia](https://en.wikipedia.org/wiki/Vivian_Carter) and [Vee-Jay Records, Wikipedia](https://en.wikipedia.org/wiki/Vee-Jay_Records) ($500 borrowed; record shop)
- [Stax Records, Wikipedia](https://EN.wikipedia.com/wiki/Stax/Volt) and [Stax Museum 1957–1968](https://staxmuseum.com/1957-1968/) (Axton mortgage, Satellite Record Shop)
- [NPR / WBEZ: Phil Chess obituary](https://www.wbez.org/arts/2016/10/20/phil-chess-co-founder-of-chess-records-dies-at-95) (Cohodas on the road trips)
- [Don Robey, Wikipedia](https://en.wikipedia.org/wiki/Don_Robey) (Bronze Peacock, Buffalo Booking Agency)
- [The Rag Blog: Huey P. Meaux](https://www.theragblog.com/ivan-koop-kuper-huey-p-meaux-was-the-crazy-cajun-2/) (break regionally, then license)
- [Syd Nathan, Wikipedia](https://en.wikipedia.org/wiki/Syd_Nathan), [Herman Lubinsky, Wikipedia](https://en.wikipedia.org/wiki/Herman_Lubinsky), [Jewel/Paula story, BSN Pubs](https://www.bsnpubs.com/gulfcoast/jewel-paulastory.html), [Johnny Vincent, Wikipedia](https://en.wikipedia.org/wiki/Johnny_Vincent), [Bobby Robinson, Wikipedia](https://en.wikipedia.org/wiki/Bobby_Robinson_(music_producer)) (shop-owner founders)
- [History of Rock: Independent Record Companies](https://history-of-rock.com/independent.htm) (90-day distributor terms; secondary, thin)

## §13 Open questions for the author

1. **Storefront (§8):** in or out? It's the best-attested pattern in the research, and also the
   closest to a "second job."
2. **Backer default (§4.1):** is "arrears plus lost standing, no game over" right, or should the
   family fund be able to pull the label's founding stake?
3. **F5 residency:** OK to move live pay to the act (more faithful, and it removes a small existing
   lifeline), provided it ships together with the WAY OUT card and demo sessions?
4. **Demo sessions (§7.1):** are paying unsigned clients acceptable as "label work"? The research
   says yes (Rayber, Phillips), but it does turn the studio into a service desk for a few hours a
   week.
5. **Ex-Musician session playing:** the playtest suggested it. I've left it **out** as a day job
   under §0.2. Confirm.
