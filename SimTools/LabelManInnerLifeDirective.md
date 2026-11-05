# Label Man Inner Life Directive — the Lien, the voices, and the drawer of convictions

Status: exploration draft (2026-11-05). Nothing here is built. Numbers marked *provisional* are placeholders until
Phase 0 measures them.

## §0 Charter and invariants

**Goal.** The player is a person, not a cursor over a ledger. Today the Label Man has four instinct scores that are
fixed at founding and speak only on the Rolodex phone and in the band room. This directive gives him a stake (a lien
signed on New Year's Eve 1959 that he must push back one year at a time until 1 January 1970), a voice in every place he does business, a set of beliefs he can adopt and change
over the decade, and a face that ages with the run.

**The blend, in one line.** Disco Elysium's thoughts are about *you*. The Label Man's convictions are about *the
market*, and the market is simulated. So the charts can prove a belief right, or wrong. That is the hook that keeps
this from being a reskin: the inner life argues with a world that doesn't care what you think.

Invariants (each one a gate on every phase):

1. **Player-only, AI-inert.** No conviction, voice, wardrobe item or the Lien itself touches the AI economy. Headless
   audits stay byte-identical, which is how every Rolodex phase was verified
   ([[probe-run-byte-comparison-proves-inertness]]). Every effect lands on a player-only channel that already exists:
   perception error, rapport, `StationAdvocacy`, verb availability, negotiation terms, time costs.
2. **No dice.** The game already chose deterministic reads: an option is gated on an instinct score *and* the fact
   being true, and instinct without the fact is a labelled bluff ([[rolodex-scene-architecture]]). Keep that. No
   visible percentage rolls, no re-rolls, no red/white checks. Asking again on unchanged evidence gives the same
   answer, as the backer already does.
3. **Voices name real state.** A voice line may be *wrong* (a low Ear misreads a record), but it must be wrong
   because of the real perception-fog draw, never invented. Same rule as descriptor text
   ([[descriptor-text-must-name-real-state]]).
4. **Never duplicate sim stats.** A voice reads `Deejay`, `ScoutingPerception`, dossier evidence, scene rooms and
   chart history. It holds no shadow copy.
5. **Trade vernacular, not poetry.** The voices talk like men in the 1960s record business: margins, spins, the
   rack jobber, the jock, the room. They don't have DE's lyrical, cosmic register.

## §1 What exists (audit)

| Piece | Where | State |
|---|---|---|
| Four instincts, 1–5: Ear, Street, Suit, Fixer | `Systems/ExecutiveInstinctProfile.cs` | Set once by `FoundingArchetype`; **never change** |
| Four archetypes | same file | Pawn-Shop Owner, Ex-Musician, Promo Man, Trade Insider |
| `ExecutiveVoice` tag on lines and options | `Systems/RolodexScene.cs:36` | Used in Rolodex calls and band-room reads/verbs only |
| Beat-assembled scenes, ~35 auditable conditions, counter + bluff | `RolodexScene.cs`, `RolodexCall.cs`, `PlayerDesk.Rolodex*.cs` | Live, the best DE-like surface in the game |
| Band-room reads (affair, using, poaching, contract, ego) | `PlayerDesk.BandRoom.cs` ~L419–710 | Live, voice-tagged |
| Venue scouting, polar ear-view cards, direct approach, rival courtship | `ScoutingPerception.cs`, `PlayerDesk.DirectApproach.cs`, `PlayerDesk.RivalCourtship.cs` | Live, **no voices**; fog scales on `scoutingAbility`, not instincts |
| One backer per archetype | `PlayerDesk.Backer.cs` | Live. Author decision: **no family loans**, founding cash *is* the family's money |
| Game over | `PlayerDesk.cs` ~L587 | The bank closes you after 3 months past the credit floor. No end date. |
| Clock | `GameDate.StartDate` = 1960-01-01; `TimeManager.SpendMinutes` | Minute hand exists |
| Character card | OFFICE tab | Text only. **No portrait** of the Label Man. |

So the DE layer is already half-built, but only in two rooms. The gaps are: instincts are static, there's no
long-term stake, scouting and errands are silent, and nobody has a face.

## §2 What we borrow, what we don't

| Borrow | Change | Leave behind |
|---|---|---|
| Skills as voices that interject | Four voices, not 24, each a business lens with a personality | Body/psyche stats, Inland Empire mysticism |
| The Thought Cabinet | **Convictions**: claims about the market that the sim scores as vindicated or contradicted | Thoughts that are purely self-referential |
| Passive checks | Threshold reads gated on instinct + true fact, voiced unprompted | Hidden dice |
| Clothes give stats | Clothes have effects **scoped** to who's looking (genre, region, year) | Flat +1 stat hats that help everywhere |
| Amnesiac mystery backstory | One lost night (31 Dec 1959) and its paperwork; solved through business contacts | Amnesia of the self, the apocalypse |
| A deadline that frames the run | The Lien, extended each December, final on 1970-01-01 | A single-week case |

## §3 The clock: the Lien

### 3.1 Shared backstory: the morning after (author's direction, 2026-11-05)

Every Label Man, whatever his archetype, wakes up the same way:

> 1 January 1960, about noon. Your head is splitting, you're wearing yesterday's shirt and one shoe, and there's
> a lawyer's letter on the mat. Some time after midnight you signed a **lien on everything you own and everything
> the label will own**. It secures a sum you can't believe, and it falls due on **1 January 1961**. The founding
> money is still in the cigar box. Everything else you had (the car, the savings book, the watch, whatever your old
> life left you) is gone, and you don't know where. You don't remember who you were with or who holds the paper.
> The letter says you can apply in December for an extension.

So the game begins: the label you meant to start anyway, now with a fuse on it.

Why it works:

- **A clock every year, not one at the end.** Each December you have to win another year. That gives ten
  deadlines, the first only twelve months out, which is exactly what the empty early game lacks (§6). 1 January
  1970 is the date he stops extending, so the decade still closes as one piece.
- **The same story for all four archetypes.** The archetype's starting cash and instincts are unchanged. Only what
  was lost differs: the Pawn-Shop Owner's stock, the Ex-Musician's guitar, the Promo Man's address book, the Trade
  Insider's pension. It's flavour, plus a few recoverable items (§3.4).
- **It keeps "no family loans".** The family money is still the stake. The lien is the Label Man's own catastrophe.
- **Chaos, not angst.** The Label Man is a bumbling, drunk wannabe who is way over his head. It's a comedy of
  consequences: he got blackout drunk at a party and signed something. The voices argue about what he probably did,
  each in character. The Fixer assumes a card game, the Ear a session that ran late, the Suit insists he'd never sign
  that, the Street thinks there was a woman.

**There's no amnesia** (author, 2026-11-05). He knows exactly who he is. He just can't account for one night and its
paperwork, the way any drunk can't. The mystery is solved with **business evidence** found through contacts (a
dealer who saw him, a jock who heard the story, a bartender's tab, a signature on a second document), never by
recovering memories.

### 3.1a The opening: "What do you remember?"

Before the morning-after scene, the founding screen asks three or four hazy questions about the party. For example:

- Where was it? (a supper club, a DJ's house party, a hotel ballroom, the back room of a bar)
- Who were you drinking with? (a man in a camel coat, a bandleader, a jukebox route man, someone from a Major)
- What were you bragging about? (the label you're about to start, the act you'll sign, money you don't have)
- How did it end? (a card table, a car, a phone call, nothing at all)

The answers **narrow the pool of holder variants** (§3.4), and the run seed picks within what's left. So the player
shapes who they owe without being told they're choosing. The answers are also hazy by design: one of them is
usually *wrong*, and finding out which is part of the mystery. The answers also seed the voices' first theories
and the first clue's location.

### 3.2 The annual extension

- **The lien** *provisional*: a face sum sized in Phase 0 so it can never just be paid off early in the decade.
  Placeholder: $20,000 in 1960 dollars. Interest accrues on the paper.
- **The December sitting.** In the last two weeks of each year the holder's lawyer (the recurring face; the holder
  stays offstage at first) sits down with you. It's a counter scene on the Rolodex architecture. To roll the lien
  to the next 1 January you meet **one of three bars**, his choice of which to stress, read from your real state:
  1. **Pay**: the year's interest plus a slice of principal, from cash on hand.
  2. **Show**: evidence the label is worth more than last year, from facts the sim already holds: a record
     pressed (1960), regional sales, a chart entry, catalogue value, the roster.
  3. **Concede**: give something up, such as a point on a master, a publishing share, a favour (Fixer), an act's
     option, or a recovered item.
  The voices read his position (the Suit sees what he can enforce, the Fixer what he's afraid of, the Street who
  he's been talking to) and open counters gated on instinct + fact, with bluffs.
- **The bar rises.** 1960 asks only for a pressed record or a token payment; by 1968 it expects a real business.
  Each year's terms are written into a ledger, so the 1970 reckoning remembers every concession.
- **Failing a December means partial seizure** (author decision for the working version): he enforces part of the
  lien (takes cash, stock or a master) and extends three months, not twelve. A second failure, or nothing left to
  take, ends the run. The bank's existing credit-floor game-over is kept as a separate way to lose.
- **Each sitting is the year's big scene.** It can offer a conviction (§4), award the year's instinct point
  (§4.3), and drop a clue about the night.

### 3.3 The reckoning, 1 January 1970: endings, not one test

| Outcome | How you reach it | Period anchor |
|---|---|---|
| **Paid clean** | Cash on hand | The independent who made it |
| **Sold to a Major** | A Major's offer for the label or catalogue clears the Lien; the label becomes a subsidiary | Late-decade consolidation, the sim's 1968–69 goal ([[major-share-late-decade-consolidation-goal]]) |
| **Sold the masters** | The Lien is paid from the catalogue; the label keeps its name and nothing else | Catalogue sales |
| **Refinanced** | The holder extends, for a share of the label or a named act | Silent partners |
| **Taken** | The holder takes the catalogue and the lease | The ones who didn't make it |
| **Torn up** | You solved the night and can prove the signature was obtained improperly, or that the holder owes *you* | The mystery's payoff; extremely hard (§3.4) |

The 1968–69 consolidation (Majors absorbing independents) gives the last two years real tension. The "sell"
offers come from the existing absorption machinery, so they're grounded in the sim. **The game ends on 1 January
1970** (author decision): there's no sandbox continuation. After the reckoning comes the epilogue, a card per act,
per conviction and per thing you lost and recovered.

### 3.4 The night of 31 December 1959

An authored mystery, dealt out over the decade. It's optional, but it's the reason the "Torn up" ending exists.

- **Clue sources are real people.** A clue sits on a contact the world already holds: a dealer, a booker, a DJ,
  the plant man, a session player. It's reachable only through existing verbs (calls, legwork, scouting rooms), so
  chasing the night means doing the job.
- **A few seed-keyed variants** (author decision). The night is authored as a handful of variants (who you were
  with, who holds the paper and why). The opening questions (§3.1a) narrow the pool and the seed picks. The holder
  might be a nightclub owner, a jukebox-route boss, a song-plugger who wanted your future publishing, or a man
  fronting for a Major.
- **The holder is a person, not a firm.** It would be fun for the holder to be a real sim entity, but that entity
  could close during the decade, and protecting it from closing would change the AI economy and break invariant 1.
  So the holder is an authored person with **interests** in real sim entities: a stake in an AI label, a jukebox
  route, a rack jobber, a club. If one of his interests closes, he survives it, and the closure becomes a scene and
  a clue ("He lost the Starlite. He'll want the money sooner."). That gives the sim tie without any protection.
- **"Torn up" is extremely hard and takes most of the game** (author decision). The proof is a chain of key clues,
  each gated three ways:
  - **By year.** The decisive clues can't surface before about 1966, so the earliest possible tear-up is the
    December 1968 sitting, and most successful runs reach it in 1969 or at the reckoning itself.
  - **By instinct.** Some clues need a high voice (Street 5 to get the bartender talking, Suit 5 to spot the forged
    witness line).
  - **By recovered item.** At least one link is a lost belonging that has to be found and bought back.
  Missing any link leaves the night explained but the lien standing. Most players should learn the truth and
  still have to pay.
- **The lost things come back.** Some of what vanished turns up (the guitar in a pawn window, the address book with
  a rival) and can be bought back. Each is worth something in a December sitting or a scene: the address book adds
  Rolodex leads, the guitar opens Ex-Musician band-room options.
- **The voices keep theories.** Each voice holds its own working theory of the night, and new clues confirm or
  embarrass them. That's a light, recurring comic thread.

## §4 Convictions: the drawer

The Label Man keeps index cards in the desk drawer. Each card is a conviction.

### 4.1 Lifecycle

1. **Offered.** It comes from an event whose fact is true: a chart week, a conversation, a failure, a trade-paper
   headline, the December sitting, a voice reacting to something you did. Never from a menu.
2. **Mulling.** It takes a drawer slot (start with 2; one more at 1962, 1964 and 1966: *provisional*). Mulling takes
   **dead hours**, the evenings and idle days the early game already has plenty of (§6). Related legwork shortens
   it: mulling "The Beach Is a Fad" goes faster if you spend nights in surf rooms. While it's mulling, a small
   "preoccupied" cost applies (one voice goes quiet).
3. **Held.** It's internalized: a permanent trade-off on player-only channels, always one gain and one cost.
4. **Scored.** Every quarter the trade paper reads the market. Each conviction is a claim with a **predicate on
   sim state** (genre share in your regions, Top-40 share by family, your own release outcomes, and so on). The card
   is marked *vindicated*, *unproven* or *contradicted*.
   - Vindicated: the gain grows, and the claim becomes a *fact* in scenes, so its old bluff option becomes an
     honest counter.
   - Contradicted: the cost grows, and the voice that sponsored it turns on you ("You said guitars were dead. Read
     the Hot 100.").
5. **Renounced.** You can throw a card away. That costs a mulling stretch to clear the slot and leaves a scar
   line the voices remember. You can't take the same card back the same year.

This is the main difference from DE. The same card can be right in 1962 and badly wrong in 1964. A conviction held
too long becomes the reason you missed the Beat boom. Period irony is built into the scoring.

### 4.2 First set (illustrative, a dozen)

| Conviction | Offered when | Gain | Cost | Market predicate |
|---|---|---|---|---|
| **Guitar Groups Are on the Way Out** | Instrumental guitar or rock records fall in your regions, ~1961–63 | Sharper Ear on vocal pop, TeenPop and girl-group material; Suit +1 with Brill-style writers; stronger pitch to Top-40 jocks | Fuzzier Ear on rock-family acts; guitar bands sense it in courtship (trust −) | Rock-family share in your regions, trailing 26 wk vs prior 26 |
| **The Beach Is a Fad** | First surf hit in a region you work | Better Suit reads on act longevity; less time lost courting novelty acts | Surf and hot-rod reads fogged | SurfRock share |
| **Never Pay a Jock** | You refuse payola, or a scandal touches a rival | Integrity counters on the phone; acts' managers trust you; immune to payola heat | Payola verbs locked; greedy DJs cool on you | Your advocacy and spin outcomes vs label average |
| **Every Record Is a Lottery Ticket** | Three releases miss in a row | Cheaper, faster release prep; more singles in the slate | Every per-record Ear read widens | Your hit rate per release |
| **Sign the Song, Not the Singer** | A hit on a cover or outside song | Ear reads songs at a distance (catalogue, demos); publishing terms improve | Weaker reads on performers and live acts | Your charting records with outside songs |
| **The Room Never Lies** | A local room breakout turns into sales | Street reads crowd draw and repeat saturation exactly | Suit discounts what the room can't show (radio-only records) | Breakout conversion in your regions |
| **Radio Is the Only Store** | Advocacy produces a chart entry | Pitch and advocacy stronger | Dealer and rack-jobber relationships cool | Airplay vs retail share of your units |
| **The Majors Will Eat Everybody** | A Major absorbs a label you know | Better reads on absorption offers; sale terms improve in the 1970 reckoning | Majors' A&R treat you as a seller, not a rival | Major collective share |
| **Kids Buy Albums Now** | LP share passes a bar (~1966+) | Ear on LP cohesion; album planning reads | Singles reads fade | LP unit share |
| **Soul Is Where the Money Is** | A soul record of yours, or a rival's, crosses over | Ear and Street on R&B/Soul; better standing with soul DJs | Country and folk reads fog | Soul share and crossovers |
| **I Was One of Them** (Ex-Musician leaning, open to all) | Sitting in with an act | Band-room trust; extra mediation options | Suit −1 on contracts with friends' acts | Your roster retention |
| **Everybody Has a Price** (Promo Man leaning) | A Fixer move works | Fixer options on everyone | Integrity counters gone; scandals hit harder | Payola-ledger outcomes |

Archetype-*leaning* cards are offered more easily to one archetype but are open to all. Archetypes shape the
drawer; they don't own it.

### 4.3 Instinct growth

Instincts stop being frozen. That's the "upgrade" the player feels. They change only through **legible** channels:

- **Convictions** may carry ±1 instinct shifts, for example "Everybody Has a Price": Fixer +1, Ear −1.
- **A successful December sitting** gives one point to raise a single instinct, up to 5. It's earned by a year
  survived. There's no XP bar.
- **6 is a gold record** (author decision): very hard, shown with a gold-record icon on the character card. A point
  alone can't buy it. Each instinct's 6 needs a career feat the sim can verify, plus a point to spend:
  - **Ear 6:** a record you picked and produced reaches #1, or an LP of yours reaches the album Top 10.
  - **Street 6:** a local room breakout of yours goes national.
  - **Suit 6:** a year with label profit above a high bar (*provisional*), with every December met by Pay.
  - **Fixer 6:** advocacy or favours put records on the chart in three regions in one year, with no scandal.
  Most runs should end with one gold record at most.
- No grinding. Repeating an action never raises a stat, so there's no reason to spam calls.

## §5 The voices

Today the four instincts are tags on lines. They become characters with one consistent temperament each:

| Voice | Temperament | Reads | When it lies (low score) |
|---|---|---|---|
| **The Ear** | Snob, romantic, hates hacks | Hook, tightness, song quality, DJ taste | Hears a hit in a mediocre side; it's the fog draw talking |
| **The Street** | Gossip, nose for heat | Room temperature, who's using, who's being courted, regional momentum | Mistakes noise for buzz |
| **The Suit** | Accountant, dry, pessimistic | Costs, terms, reach, what an act will actually earn | Trusts the paper over the people |
| **The Fixer** | Operator, cynical | Greed, suspicion, leverage, who owes whom | Sees an angle that isn't there |

Rules:

- **Unprompted reads.** A voice speaks when its score clears a threshold *and* the fact behind the line is true.
  Nothing new is computed; these are the reads the game already does, now voiced everywhere.
- **Arguments.** When two voices' reads point different ways, both speak and the player chooses. For example, the
  Ear says sign, the Suit says the manager's sheet doesn't work. The choice isn't scored. It's a moment of
  character.
- **Wrong voices are honest about being voices.** A low-score read is tagged with its score, so the player learns
  over time which voice to trust. Convictions and instinct growth change that.

## §6 Filling the early-game empty space

The gap: after the first lacquer and pressing, there are weeks of plant lead time, shipping and waiting for
airplay where the player has little to do. Early Game V2 already built honest groundwork verbs: DJ courtesy calls,
dealer advance orders, pitching the act to a booker. This phase gives them a place and a voice.

**Legwork: the town as a day map.** The founding town gets a handful of places, each a short beat-assembled scene
(same architecture as Rolodex calls) costing hours:

| Place | What happens | Existing system it wraps |
|---|---|---|
| The record shop | See what's moving, talk to the clerk, ask for an advance order | Dealer advance orders, local retail exposure |
| The diner where the jocks eat | Overhear, make a courtesy approach | DJ courtesy, Rolodex discovery |
| The plant floor | Watch your run, chase the date, meet the plant man | Pressing lead time |
| The club / the roadhouse | Listen, sit in, talk to the booker | Venue scouting, booker pitch, direct approach |
| The newsstand | The trade paper's week | Chart and market readings that score convictions |
| The union hall / musicians' bar | Session players, gossip about acts | Session employment, contact network |

- **Nights.** Each evening ends at the desk: mull a conviction, listen to the acetate again (the Ear re-reads it,
  same fog draw), or go out. Dead hours turn into mulling. This is where the empty early game gets filled.
- **The inciting morning.** 1 January 1960 opens on the hangover and the lawyer's letter (§3.1), with the voices
  arguing about the night. The first errand follows. The first December sitting is the year's goal, so even the
  empty weeks have a deadline behind them.
- **Small scenes that cost nothing in the sim.** For example, holding the first box of pressings. These are
  moments, gated on real events.
- **Measure it.** Phase 0 adds an "empty day" metric: player days with no action that has a sim consequence. The
  target is near zero in the first 26 weeks.

## §7 Scouting

Venue scouting is the second most-used player loop and it's silent. Changes:

- **Voices at the venue**, each wrapping a read that already exists:
  - Ear: the hook and tightness bands from the polar ear view ([[polar-ear-view-vs-studio-view]]).
  - Street: crowd draw, repeat-audience saturation, who the regulars came for.
  - Suit: the manager stamp, the likely advance (historical pricing), the roster slot it costs.
  - Fixer: who else is in the room. Spoken only if rival courtship interest is real ("the man at the bar is from a
    Major").
- **Courtship as a counter scene.** The act or its manager raises one grounded objection (the advance, the label's
  record, a rival's offer, the Label Man's own convictions). The player answers with an instinct+fact option or a
  labelled bluff. Same machinery as the Rolodex. This turns rival courtship from a status panel into a
  conversation.
- **Instincts finally reach the fog.** Today the venue fog scales on archetype `scoutingAbility`. Phase 2 makes Ear
  and Street (plus convictions) modulate the existing error band. That's player-only, so the AI market is
  unaffected.
- **Convictions change what you notice.** "Sign the Song, Not the Singer" makes the Ear comment on the song's
  writer; "The Room Never Lies" makes the Street read the room's history.

## §8 Rolodex

The Rolodex is already the model. The work here is to widen it and connect it:

- **Convictions add counters.** "Never Pay a Jock" adds an integrity answer to a greedy DJ. A vindicated
  conviction turns its bluff into fact.
- **Cards beyond DJs**, for people the world already holds: bookers (named in Phase 6 scouting), the jukebox
  operator, the merchant lender, dealers, the plant man, the holder. Each gets one or two verbs routed through
  existing systems. See `SimTools/ContactNetworkDirective.md` before adding a verb.
- **The night thread.** Clues about 31 December 1959 sit on contacts (§3.4). The Street and Fixer pick them up
  on calls and legwork. It's an optional mystery with a scene payoff, and it never touches the AI economy.
- **Voices remember.** Scar lines from renounced convictions, a failed December sitting, a burned DJ. The voices bring
  these up on later calls.

## §9 The Label Man's face

A layered 2D portrait on the OFFICE character card and in scene headers.

- **At founding:** face, build, skin, hair, and starting wardrobe from the archetype (the Pawn-Shop Owner's
  cardigan, the Promo Man's sharkskin suit).
- **Wardrobe is bought with real cash.** A $90 suit in 1960 matters when the first pressing costs about $1.3k.
  Items are period-dated (narrow ties in 1962, Nehru jacket in 1967) and go out of fashion.
- **Clothes have real mechanical effects** (author decision), but **scoped**: each item's effect depends on who's
  looking, where, and what year it is, so there's no universal best outfit. Examples:
  - A 19th-century military jacket in 1967: a clear boost with psychedelic acts, rooms and DJs (courtship trust, the
    Street's reads in those rooms). In 1963 it gets you laughed out of the room, and a Nashville jock or a
    backer reads it as a liability.
  - A sharp mohair suit: eases backers', Majors' and the lawyer's Suit reads at the December sitting; costs trust
    with folk and garage acts.
  - A turtleneck and corduroy (from about 1964): good with folk and coffee-house rooms, flat elsewhere.
  - The watch lost on 31 December 1959, if you buy it back: small, permanent standing with the lawyer.
  Effects land on the same player-only channels as everything else (rapport, courtship trust, read thresholds,
  sitting counters), so the AI economy is untouched.
- **The portrait carries the run.** He ages ten years. Bad months show in his face. The office behind him fills up:
  the gold record, the transistor, the trade-paper clippings of vindicated convictions, the lawyer's letter
  pinned to the wall, and a cocktail napkin from 31 December 1959 with a phone number on it.
- **Scope:** about 6 layer slots (base, hair, eyes and glasses, top, tie/neck, hat) and four age stages. It's an art
  pass of its own, sequenced after the systems.

## §10 Architecture sketch

- New player-only state on `PlayerDesk`: `LienState` (face sum, accrued interest, the yearly extension ledger of bars met and concessions made, clue
  state for the night),
  `ConvictionDrawer` (slots, offered/mulling/held/renounced, score history), `InstinctProfile` made mutable with a
  change ledger, `PortraitState`.
- Conviction predicates read a quarterly `MarketReading` snapshot built from aggregates the sim already computes
  (regional genre share, chart family share, LP unit share, the player's release outcomes). It's read-only and
  computed only when a player exists.
- Effects apply through a single `PlayerModifiers` lookup that existing player-only call sites consult: perception
  error band, rapport/advocacy gains, verb gates, negotiation terms, time costs. A missing lookup returns an exact
  no-op, the same pattern as `StationAdvocacy`.
- Scenes reuse `RolodexScene` beats and conditions. New conditions follow the existing rule: they read only the
  context built once per scene.
- Save fields for all of it. A save/load round trip is gated per phase.

## §11 Phases

| Phase | Content | Gate |
|---|---|---|
| **0** | Measure: player cash trajectories across archetypes (sizes the Lien); the empty-day metric for the first 26 weeks; which reads already exist per surface | Numbers recorded, no code paths changed |
| **1** | The Lien: morning-after opening, December sitting, enforcement on failure, 1970 reckoning with endings | Byte-identical headless; save/load |
| **2** | Voices at venues and in courtship; instincts modulate the venue fog | Byte-identical headless |
| **3** | Conviction drawer: lifecycle, quarterly scoring, the first 6 cards | Same; each card's predicate tested on a real save |
| **4** | Legwork map and nights for the founding town | Empty-day metric near zero |
| **5** | Mutable instincts: New Year point, conviction shifts | Same |
| **6** | Portrait and wardrobe | UI driver check ([[ui-verification-via-temp-driver-scene]]) |
| **7** | Rolodex widening, the night's clues and lost items, remaining cards, epilogue | Playtest |

## §12 Author decisions (2026-11-05)

1. **Backstory:** New Year's Eve 1959 lien, extended each December; no amnesia, just a drunk in over his head (§3.1).
2. **December failure:** partial seizure plus a three-month extension, for the working version (§3.2).
3. **End of game:** the run ends on 1 January 1970. No sandbox continuation (§3.3).
4. **Instinct cap:** 6, shown as a gold record, very hard, feat-gated (§4.3).
5. **Mulling:** takes dead hours; related actions speed it up (§4.1).
6. **Clothes:** real mechanical effects, scoped by genre, region and year (§9).
7. **Holder:** a few seed-keyed variants, narrowed by the opening "What do you remember?" questions; the holder is an
   authored person with interests in real sim entities, so nothing in the sim needs protecting (§3.1a, §3.4).
8. **"Torn up":** extremely hard, gated by year, instinct and recovered item; earliest December 1968 (§3.4).

## §13 Still open

1. The lien's face sum and each year's bar: sized in Phase 0 from measured player cash trajectories.
2. The holder variants: how many (3–5?) and who they are. Needs a writing pass.
3. The gold-record feat bars (especially Suit's profit bar): set after Phase 0.
4. The wardrobe list per year: needs a period and art pass.
