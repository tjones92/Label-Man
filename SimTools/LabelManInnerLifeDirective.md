# Label Man Inner Life Directive — the Lien, the voices, and the drawer of convictions

Status: exploration draft (2026-11-05; holders revised 2026-11-24). The Lien is built, core and UI
(`Systems/PlayerDesk.Lien.cs`, `UI/PlayerDeskPanel.Lien.cs`; checked by `SimTools/LienChecks.tscn` and
`SimTools/LienUiChecks.tscn`), and so is getting around without the car (§3.9, Phase 1c:
`Systems/PlayerDesk.Transport.cs`; `SimTools/TransportChecks.tscn`). The holder-specific implementations are
tracked in §3.5 and Phases 1b–1d.
Numbers marked *provisional* are placeholders until Phase 0 measures them.

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
| The Lien (core) | `PlayerDesk.Lien.cs`; `SimTools/LienChecks.tscn` | Built 2026-11: five holders, the sittings, enforcement and the reckoning |
| The Lien (UI) | `UI/PlayerDeskPanel.Lien.cs`; `SimTools/LienUiChecks.tscn` | Built 2026-11-24: THE PAPER on the OFFICE tab, the sitting page, NEXT UP and badge, the morning-after modal, the paper on the game-over page |
| Travel | `PlayerDesk.Travel` → `DriveQuote` (64 mph, 2¢ a mile) | Assumes a car. §3.9 changes that. |
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
- **The same story for all four archetypes.** The archetype's starting cash and instincts are unchanged. Every run
  loses the car (§3.9). Each archetype also loses one signature belonging (§3.8): the Pawn-Shop Owner's stock, the
  Ex-Musician's pedal steel, the Promo Man's address book, the Trade Insider's chart files. The holder takes one or two
  more things (§3.5). They can all be found again.
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

**Built (2026-11-24):** `UI/Office/OfficeIntro.cs` plays the morning on NEW GAME. He staggers in with a hangover
(blurred, double-vision view and a vignette that follows the edges of the frame) and finds the lawyer's letter on the
desk ("Congrats on the office!", $20,000, holder unnamed because it isn't picked yet). He sits, opens the drawer, takes
out the revolver and turns it toward himself; a voice stops him with a two-option choice, and he puts it back. Then the
founding pages open on **LAST NIGHT**, these four questions, ahead of background, town and name. ESC skips straight to
them.

Before the morning-after scene, the founding screen asks three or four hazy questions about the party. For example:

- Where was it? (a supper club, a DJ's house party, a hotel ballroom, the back room of a bar)
- Who were you drinking with? (a man in a camel coat, a disc jockey, a jukebox route man, someone from a Major)
- What were you bragging about? (the label you're about to start, the act you'll sign, money you don't have, the
  hits you can pick)
- How did it end? (a card table, a car, a phone call, nothing at all)

The answers **narrow the pool of holder variants** (§3.4), and the run seed picks within what's left. So the player
shapes who they owe without being told they're choosing. The answers are also hazy by design: one of them is
usually *wrong*, and finding out which is part of the mystery. The answers also seed the voices' first theories
and the first clue's location.

### 3.2 The annual extension

- **The lien**: a $20,000 face sum in 1960 dollars (author decision). Simple interest accrues at the holder's rate
  (0–8%; Kubiak's compounds; §3.7). **The paper forbids prepayment**, so a hit in 1960 can't end the run; full payment
  is accepted only at the reckoning (built).
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
- **The bar rises.** 1960 asks only for a pressed record or a token payment, and every holder accepts a record out
  as the 1960 Show, whatever he stresses (built). By 1968 it expects a real business.
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

Floria (§3.5 H4) forces a buy-out at the 1968 sitting unless you get out another way, so with him "Sold to a Major"
can come before the reckoning.

The 1968–69 consolidation (Majors absorbing independents) gives the last two years real tension. The "sell"
offers come from the existing absorption machinery, so they're grounded in the sim. **The game ends on 1 January
1970** (author decision): there's no sandbox continuation. After the reckoning comes the epilogue, a card per act,
per conviction and per thing you lost and recovered.

### 3.4 The night of 31 December 1959

An authored mystery, dealt out over the decade. It's optional, but it's the reason the "Torn up" ending exists.

- **A comedy of two fools.** Each lien exists because the Label Man was drunk *and* the holder is bad at his job.
  Every holder made one big blunder that leaves the paper weak, and finding that blunder is the tear-up. Each holder
  is also afraid of the player in one particular way, and shows it by overreacting.
- **Clue sources are real people.** A clue sits on a contact the world already holds: a dealer, a booker, a DJ,
  the plant man, a session player. It's reachable only through existing verbs (calls, legwork, scouting rooms), so
  chasing the night means doing the job.
- **Five seed-keyed variants** (§3.5). The opening questions (§3.1a) narrow the pool and the seed picks.
- **The holder is a person, not a firm.** He's an authored person with **interests** in real sim entities: a room,
  a jukebox route, a publisher, a Major, a promoter network. If one of his interests closes, he survives it, and the
  closure becomes a scene and a clue. That gives the sim tie without protecting anything (invariant 1).
- **Handicap and goal.** Each holder gives one *handicap*, on a player-only channel from the first week, and one
  *goal*: what he pushes at each December, which isn't money. He'll always take money, but each holder makes money
  awkward in his own way (§3.7).
- **Two items per holder.** Each holder has a **belonging** (something of yours, with a perk through one Sense that
  applies as soon as you get it back, and which can be offered as a Concede) and a piece of **evidence** (a link in
  the tear-up chain, with no other use). Sometimes one holds the other: the deck is in the car. On top of those, the
  **car** is gone in every run (§3.9) and each archetype loses one **signature belonging** (§3.8).
- **Evidence has to be read before it counts.** A found evidence item looks ordinary ("a deck of cards in the trunk")
  until the right Sense reads it at 3+. Before that you can sell it, concede it or throw it out; the voices only warn
  you if they're good enough, and a bluff label marks the risk. This is the Label Man's bungling turned into a
  mechanic. A lost evidence item can come back once, at a much higher price.
- **Every link you find eases the sitting.** Each link found (an item, a reading, a witness) gives the lawyer one less
  thing to push and gives you one honest counter at every later sitting. So the hunt pays off from the first year,
  and only the last link ends the paper.
- **Torn up is hard, and gated three ways:** by **year** (the last link only, §3.7), by **Sense** (a 6 is a gold
  record, §4.3), and by **item**. Missing any link leaves the night explained but the lien standing. Most players
  should learn the truth and still have to pay. No route needs two 6s: most runs reach only one gold record.
- **The voices keep theories.** Each voice holds its own working theory of the night, and new clues confirm or
  embarrass them. That's a light, recurring comic thread.

**Every Sense leads at least one tear-up:**

| Sense | Leads | Assists |
|---|---|---|
| Ear | Winters (the party tape); Sawyer (make Connie a Star) | Germain (spotting the ledger among the 78s) |
| Street | Sawyer (the drummer); Floria (the other signer) | Kubiak (which jukebox); Germain (the jock who'll talk) |
| Suit | Kubiak (the notary); Winters (the bank ledger); Floria (the matching signature) | |
| Fixer | Germain (the black ledger); Floria (the other signer, alternative) | Kubiak (buying his paper cheap) |

Four holders can be torn up with one Sense at 6 plus items, where a second Sense at 4–5 saves time or money but isn't
required. Floria needs a 6 *and* a second Sense at 4+; that's what makes him the hardest.

| Holder | Pressure | Tear-up | Earliest tear-up | Feel |
|---|---|---|---|---|
| Clyde Winters | Low | Moderate | Dec 1965 | The friendly one; a mystery run |
| Walter Kubiak | Money | Moderate–hard | Dec 1966 | An economy run |
| Dee-Dee Sawyer | Roster | Hard | Dec 1966 (Connie route: Dec 1965) | An A&R run |
| Jack Germain | Scandal | Hard | Dec 1966 | A temptation run |
| Gene Floria | Survival | Very hard, combo | Dec 1968 | The boss fight |

### 3.5 The five holders

Names are placeholders. The lawyers are the ones in `PlayerDesk.Lien.cs`. Rates are Phase 1 values from §3.7.

#### H1. Dee-Dee Sawyer, the room owner (4%, stresses Concede)

- **Who.** Owns the hometown's most popular room. That's a real room from the scene catalogue: the biggest
  performance room in the home scene (`LienState.Venue`; in Detroit, the River Lantern). He also has a stake in its
  bookers. Loud, vain, and generous when he's winning. He believes he's a shrewd card player, but he has never won a
  hand in his life except one. The room's booker is a separate man, who is also the Ex-Musician's backer (§1). He
  runs the room, Dee-Dee owns it.
- **Why he wants a label.** His girlfriend, **Connie Boursaw**, sings at the room and wants a record. His last plan for
  that was one of his own men starting a label for the room's acts. The man took Dee-Dee's loan and left town, and
  Dee-Dee was stuck with the note.
- **What happened.** In the back-room card game you heard about the man who'd run off and made a drunk, heroic offer
  to "take the whole thing off his hands" if you lost the next hand. You thought you were betting to *win* a label.
  You lost. Halloran wrote the note over to you before sunrise, and Connie's contract came with it.
- **His blunder.** Dee-Dee didn't mark the deck. His dealer, a session drummer called **Sonny Pike**, owed him money
  and marked it so Dee-Dee would win and be in a good mood. Dee-Dee has bragged about "the hand I won off the record
  man" for years, which is exactly why Sonny is too frightened to talk.
- **Handicap: Connie.** She's on your roster from day one under Dee-Dee's paper. The game opens with the Suit reading
  it to you:
  - A five-year term, with the option held by *Dee-Dee*, not you.
  - A high royalty.
  - A guaranteed session every year.
  - **No release clause: the label can't drop her** (author decision). The drop verb is disabled for her, with
    Halloran's reason.

  She's an underwhelming act, and she holds a roster slot and a yearly session cost all decade. She leaves only
  through a tear-up, by reaching Star (below), or at the reckoning. **Booking gate built.** For a player-requested
  night or residency at Sawyer's room, Connie must already have a release dated in each year the engagement covers.
  Connie can be pitched herself at any time the ordinary booking rules allow. The booker explains the restriction,
  and residency searches can move on to another room with enough standing.
- **Goal: Connie's career.** His asks escalate. Missing one isn't a failed sitting, but in a year she hasn't moved, Pay
  costs **1.5×**. Each year she does move, he hands back a belonging.

  | Sitting | He asks for (*provisional*) |
  |---|---|
  | 1960 | A record by Connie, out |
  | 1961–62 | Connie on a regional chart |
  | 1963–64 | Connie in the Hot 100 |
  | 1965–66 | Connie in the Top 40, or Rising |
  | 1967–69 | Connie at **Star** (a #1). He *talks* about Superstar, but it's never a bar. |

  Star means one #1 and Superstar four, which is rare for a strong act and very rare for a weak one, so Superstar is
  only colour. Artist identity is frozen ([[artist-identity-is-frozen]]), so Connie can't get better herself; your
  choices around her can. Better songs (Ear), a better producer, and the room as her home room for a local breakout
  (Street) are what raise her odds. That's what makes Dee-Dee an A&R run.
- **Items.**

  | Item | Kind | Where | How | Perk |
  |---|---|---|---|---|
  | **Your car** | Belonging (transport, §3.9), holds evidence | The lot behind the room; Dee-Dee drives it | He returns it the first year Connie charts, or you buy it back (high) | Transport. **The marked deck** is under the spare; Street 3+ reads the marks. |
  | **Your watch** | Belonging (Suit) | On Dee-Dee's wrist. He wears it to the sittings. | A Concede trade, or Connie in the Hot 100 | +standing with every lawyer at every sitting |

- **Torn up: the deck plus one witness.**
  - **Street route:** Sonny Pike drifts through the session-employment pool and leaves town in 1966. Find him with
    Street 6; out of Dee-Dee's reach, he talks.
  - **Ear route:** Connie reaches Star. She's outgrown Dee-Dee and leaves him; she was at the table and saw Sonny
    shuffle. From 1965. You make the act he forced on you good enough to testify against him.
- **Sim interests.** The room and its bookers. If the room's draw falls, his asks come a year early.
- **Voices.** The Fixer respects him and is wrong about him. The Suit hates every concession. The Ear can't stand
  Connie, and softens only if she finds the right song.

#### H2. Walter Kubiak, the jukebox route boss (8%, compounding; stresses Pay)

- **Who.** Runs a small, struggling route and holds a share in a one-stop. Loud and always counting. He's sure
  jukeboxes will "explode" in the sixties. They won't, and the Suit knows it.
- **Why he wants a label.** Cheap records for his machines, and exclusive jukebox rights to everything you press. His
  route doesn't reach far, and he expects your hits to fix that.
- **What happened.** He sat down next to you at the bar and talked jukeboxes for three hours. You signed his
  "standard operator's agreement", believing you'd gone in with him on a little route in town. In fact you took over
  the last man's debt, and Walter filed your label's incorporation papers himself, with clause 14 in them.
- **His blunder.** He uses the same form every time, notarized by the same man, **Leon Marsh**, whose commission lapsed
  in 1959. Walter is too cheap to pay for a new notary. His lawyer also wrote a no-prepayment clause in by mistake,
  and Walter is furious about it.
- **Handicap: money.** The highest rate, and interest left unpaid when the lien rolls over goes onto the face sum
  (built). He also holds the jukebox rights in your home region, so your records play only on his weak route there.
  **Built 2026-11-24** (`Systems/PlayerDesk.Kubiak.cs`, `SimTools/KubiakChecks.tscn`) on the player's own operator
  accounts, so the AI economy is untouched: a home-town operator reaches **half** its machines for your records (its
  route crowd and its suggested order), every year he holds the paper; away operators and shops are free. The home
  town's operator list says why. Values *provisional*.
- **Goal: cash.** Show counts only if it's records on his route. Concede he takes only if it's something he can sell.
- **Items.**

  | Item | Kind | Where | How | Perk |
  |---|---|---|---|---|
  | **Your car** | Belonging (transport, §3.9) | His yard, "as security" | Buy it back; Walter sells anything, and the price drops in his bad years | Transport. (The Pawn-Shop Owner's stock is in the same warehouse, §3.8.) |
  | **Your carbon copy** | Evidence | The coin mechanism of a jukebox on his route. You fed it to a machine at 2 a.m., thinking it was a dollar. | Find the machine that has rejected dimes since 1960. Street 4+ hears which bar complains; without it, it's many hours of legwork. | — |

- **Torn up.** Marsh's register at the county clerk (Suit 6, from 1966, after Marsh is caught notarizing on a lapsed
  commission), plus the carbon copy.
- **The Fixer's side door** (not a tear-up). By the mid-decade his route is failing and he owes the one-stop. Fixer 5
  can get the one-stop to take your paper as part payment of his debt, then sell it to you at a discount. That's
  "Refinanced", but cheap.
- **Sim interests.** A jukebox Op account and a one-stop or rack jobber.
- **Voices.** The Suit understands him and pities his books. The Street knows every bar on his route. The Fixer thinks
  he can be played, and is usually wrong: Walter's too dumb to be played.

#### H3. Clyde Winters, the song-plugger (0%, stresses Show)

- **Who.** A plugger on salary for a big publisher, desperate for a catalogue of his own. Charming, quick, and broke.
- **What happened.** At a disc jockey's house party you bragged you could always pick the Top 10. Clyde offered to front
  you $20,000 "for that ear". You laughed, said "what the hell", and signed. He never paid it, **because he never had
  it.**
- **His blunder.** The whole lien is for money that never existed. He charges no interest, because an interest claim
  would make a lawyer ask where the principal went. He sweats every December.
- **Handicap.** He took your address book and has spent the year calling your contacts as you, borrowing money,
  pitching songs and making promises. Your hometown Rolodex starts cold, and some cards start burned. He also presses
  his own songs (which aren't very good) on your acts. Recording one counts as a Concede, at a real cost to the
  record.
  **Cold and burned cards built 2026-11-24** (`Systems/PlayerDesk.Winters.cs`, `SimTools/WintersChecks.tscn`). At
  founding Clyde has worked the home region's most influential reporter jocks, half of them up to six (Detroit: 6 of
  11). All of them think you owe them one (a 50% "you burned me before" objection on asks), and the top third (at
  least one; Detroit: 2) also stop taking your word (Personal Pitch closed). The marks live on the lien, not on the
  card's own flags: a card says so in its log when it goes into the book, and the card shows a line naming "somebody"
  until Clyde is identified. Turning down his first move burns the biggest cold card. The book comes back after a Show
  from the 1961 sitting on (a year of real sales), and only Clyde's marks thaw; a burn the Label Man earned himself
  stays.
  **The other ways back, the leads and the stub** (built 2026-11-24, same file). Once Clyde is named, THE PAPER says the
  book is in his desk at the publisher's office in town and offers his secretary: **Street 3+** takes her to lunch (3h),
  **Fixer 3+** sends flowers and a $40 envelope (1h). However it comes back, the book's numbers become **Rolodex leads**:
  every jock Clyde worked who isn't in the book yet, as a name with a home number (HeardOf, hours known). A Promo Man's
  book (his signature belonging, §3.8) holds every reporter in the home region (Detroit: 10 leads), and those start at
  +0.05 rapport. **The cheque stub** is in the back: Suit 3+ reads it on the spot, otherwise it waits on the desk for a
  Suit that can. Read, it's the first link of the Suit route, and an **honest counter at every later Winters sitting**:
  when the copies fall short of Show, laying it on the desk rolls the lien as Show at −0.1 net standing (*provisional*).
  The book comes off the "gone since the night" list.
- **Goal: hits**, because hits are copyrights. His Concedes are publishing shares. In good years he's almost friendly.
- **Items.**

  | Item | Kind | Where | How | Perk |
  |---|---|---|---|---|
  | **Your address book** (Promo Man: his signature one, §3.8) | Belonging (Street), holds evidence | His desk at the publisher | Fixer or Street with his secretary, or he gives it back after a year you Show well | Thaws the burned cards and adds Rolodex leads. **The cheque stub** for the "advance" is inside; Suit 3+ reads it. |
  | **Your tape recorder** (Ex-Musician: **your 1926 pedal steel**) | Belonging (Ear), holds evidence | His apartment; he never listened to it | Buy it back, or trade a publishing share | Recorder: replay a venue set at the desk (same fog draw). Pedal steel: band-room sit-ins. **The party reel** is on the machine, or in the steel's case. |

- **Torn up: two routes.**
  - **Suit:** the bank's ledger for 31 December 1959 shows no $20,000 leaving his account (Suit 6, through a bank
    contact), plus the cheque stub.
  - **Ear:** you taped the party, because you'd brought the recorder to tape the host's records. In four hours of
    noise, Clyde says "the cash'll be there Monday". Picking his voice out takes **Ear 6**, because the host DJ only
    believes what you heard from a proven ear. The host then confirms it.

  Both routes have a floor of **1965**: the host DJ works in another city until he comes back to town.
- **Sim interests.** Publishing, and a stake in an AI MidTier label's publishing arm.
- **Voices.** The Ear likes him: he knows a song. The Suit has suspected the advance from day one. The Fixer wants to
  steal his writers.

#### H4. Gene Floria, the Major's front (3%, stresses Show; refuses money from 1966)

- **Who.** A soft-spoken man in a camel coat from the regional office of the Major with the largest share in your
  region (a real sim entity). He's on the firm's blacklist after botching a subsidiary deal, and needs a grown, cheap
  independent to deliver to New York in 1968–69.
- **What happened.** You wandered into a hotel ballroom party without knowing it was the Major's, and pretended to own
  a label so you wouldn't be thrown out. Gene took you for exactly the rube you were. He said "lots of new label owners"
  were signing that night and called it a "pressing credit line". You thanked him.
- **His blunder.** Every signer that night was witnessed by the same man, his brother-in-law **Delbert Mays**, who
  isn't a lawyer, isn't with the firm, and signs his name the same way every time.
- **Handicap: the clock.** From the December 1966 sitting he won't take money, though the reckoning still must (built).
  At the 1968 sitting (1969 if your Show has been weak), his Major offers to buy you, and the paper lets him force it.
  His Show bar is the steepest, because he's fattening you up.
- **Ways out of the buy-out:**
  1. **Tear it up** (below).
  2. **Too big to swallow.** The firm turns Floria down if your label's value passes a bar. The bar is set from
     Phase 0 valuations (author decision) and should be reachable only by a real national label. Floria's own Show
     demands push you towards it.
  3. **A better buyer.** A rival Major or MidTier outbids him. You still sell, but on your terms: "Sold to a Major",
     not "Taken".
  4. **Pay at the reckoning,** if the buy-out was put off.
  5. **He gets fired.** If his Major's share falls below a bar, his successor finds the paper in a drawer and treats it
     as an ordinary lien. You can't plan on it, but the Street tells you when it's close.

  The buy-out must be a player-only path. The Major's AI behaviour stays byte-identical.

  **Built.** The paper binds Floria to the leading Major family by sales in the label's home region at founding.
  His Show bar asks for 1.5× the copies or two Top 40 records once chart evidence counts. A weak 1968 Show
  leaves the ordinary sitting in place that year and moves the purchase review to December 1969. If his firm
  proceeds, its funded offer is 65% of the valuation, capped by its available cash. A funded rival Major or
  MidTier can offer more after the label has a Top 40 record, provided the offer clears the debt. A rival sale
  ends the run as **Sold to a Major**. The firm's compulsory purchase ends it as **Taken**, including when the
  player lets the deadline pass. Both routes transfer the imprint as a subsidiary. If Floria's firm exits or its
  share falls below a quarter of its founding share, the successor accepts the ordinary Pay bar.

  **Valuation.** Twice the positive net contribution of the last 52 weeks' issues whose masters the label can
  convey. The firm passes at **$1.25m** if at least four regions each account for at least 1,000 copies and 5% of
  the measured regional sales. In the existing seed-1001 snapshots dated January 3 and January 31, 1969
  (`bms4e-intent90-1001-1969` and `perf-control-1969`), the 430 active Independents in each run had p95 values
  of $1.16m and $1.14m, and p99 values of $1.57m and $1.52m. The 84 active MidTiers had medians near $1.98m in
  both. These are AI catalogue comparables. The player retains past releases, so the player valuation can
  include eligible sides after they stop selling. The full-decade player cash check remains open.
- **Items.**

  | Item | Kind | Where | How | Perk |
  |---|---|---|---|---|
  | **Your good overcoat** | Belonging (wardrobe, §9), holds evidence | Left in the hotel cloakroom, sold unclaimed, now in a pawn shop | Street on the pawn rounds (1962+), then buy it | Easier Suit reads at sittings and with backers. **The invitation card** is in the pocket. |
  | **Your briefcase** | Belonging (Suit), holds evidence | Floria's office at the regional branch; he forgot he took it | Fixer 4+ (his secretary) or Suit 4+ (a records request) | Founding papers: cheaper legal work. **Your carbon of the "credit line"**, with Delbert's signature, is inside. |

- **Torn up (combo):** the invitation card, plus your carbon, plus **another signer** (an AI independent's founder;
  **Street 6** or **Fixer 6**), plus **Suit 4+** to lay the signatures side by side. The other signer surfaces only
  once the Major moves on him, so the earliest is **December 1968**.
- **Sim interests.** The Major itself. If it absorbs another independent, the Street hears it was another signer. If
  its share falls, Floria's patience thins and his job gets shaky.
- **Voices.** The Suit is afraid of him. The Fixer can't get a read on him. The Street finds him first.

#### H5. Jack Germain, the payola jock (6%, stresses Concede)

- **Who.** Until November 1959, the hottest disc jockey in the region. He was fired in the payola sweep, and his name is
  in the trade papers. He's a big personality, can't stop talking, and is sure he'll be back on the air by spring.
  The game opens at the crackdown peak (`RadioEra.RegulatoryHeat` 0.90 in 1960, falling to 0.55 by 1965).
- **Why he wants a label.** In the order he'd admit it: legitimate income (the grand jury wants to know where his money
  came from, so he needs to be a paid "promotion consultant"); a label to run his old jocks for, as an independent
  promotion outfit; and his book, which he thinks you have. You do; you just don't know it.
- **What happened.** New Year's Eve was his farewell broadcast, and you were in the booth with him at midnight, drunk.
  He put you on the air, and you announced a label that didn't exist and "its first big record". Back at his house he
  said "every label needs a promotion man", and you signed a "consulting retainer and loan agreement" on the back of
  the station's programme log. Before he passed out he gave you his **black ledger** ("keep this safe, kid, nobody
  looks for anything at a label that doesn't exist"). You lost it.
- **His blunder.** He wrote down every bribe he ever took, because he was afraid of being cheated, and then gave the only
  copy to a drunk stranger.
- **Handicap: heat.** When one of Jack's jocks is caught working your label, the rest of that network push goes dark.
  The scandal costs reputation and can get the jock removed. Each consulting fee adds 0.05 to the label's exposure
  on top of the trail left by the station arrangements. Exposure increases the chance and severity of a bust and
  retains 97% of its value each week. The ledger saves the arrangements, promoters, exposure, station heat and RNG.
- **Concede: his name.** Jack takes 25% of the writer's share on each of the next two originals with a credited
  writer. At the 2¢ statutory mechanical, that's 0.25¢ per copy ($25 per 10,000 copies). Each cut-in adds 0.06
  exposure and costs each credited writer 0.12 trust, plus 0.08 band strain, regardless of Ear.
- **Concede: his boys.** A $300 consulting fee buys an eight-week push on half the home region's reporters, rounded
  up and capped at six. Network effectiveness is 0.80 and mob connection is 0.70. Discretion starts at 0.55 and
  falls by 0.12 after each use, with a floor of 0.05.
- **Measured.** `PayolaMeasureProbe.tscn`, seed 1001, Detroit, 3,000 trials per cell. All seven regions had 11
  reporters. Jack's first 1960 push delivered 11.89 boost-weeks at $25 per boost-week before penalties, compared
  with $57 for Project Promo L and $833 for Full Sponsorship. The first push's bust rate was 15.1%, with $196 in
  expected penalties and $1,300 per bust. In the annual December replay, the chance of at least one bust was
  58.2% by December 1963 and 93.1% by December 1969. With the additional exposure from two cut-ins and the fee each
  year, those figures were 64.3% and 95.2%. The probe writes `SimLogs/payola-measure-1001.txt`.
- **Items.**

  | Item | Kind | Where | How | Perk |
  |---|---|---|---|---|
  | **The black ledger** | Evidence | Filed under "L" in the station's record library, between two 78s. When the station clears out its 78s (seeded, 1961–64), it ends up in a used-record bin. | The Street hears the station's 78s went to a shop; the Ear notices one "record" is too thick. Jack is hunting it too. | — |
  | **Your chequebook** | Belonging (Street) | Jack has it. He wrote six $50 cheques to jocks in your label's name, and they all bounced, because the founding money is in the cigar box. | He returns it for one concession, or Fixer 3+ talks it back | The six jocks start cold. Make the cheques good ($300) and they become **your** sources: Street reads on his network. |

- **Holding the ledger freezes him.** Built. After Tolliver identifies Jack, the office paper points to Jack's
  real station. Ask about the 78s at the station, follow the clerk's lead to its used-record shop after the
  seeded 1961–64 clearance, and bring the book back to the office. Street and Ear shorten their respective
  errands. Fixer 3 can read the book. When Jack hears you have read it, Tolliver stops offering concessions and
  Jack drops pending cut-ins. Existing credits and station arrangements keep their terms. Pay, Show and the
  deadline stay in force.
- **Torn up:** the ledger, plus **Fixer 6** (to use it without it blowing up on you, since your bounced cheques are in
  it too), plus **one of his jocks** who'll talk once the payola rules catch up with him (Street 4+, from 1966). Jack
  tears the paper up himself rather than see the book reach a grand jury.
- **Sim interests.** His `IndiePromoter` network, and the regulatory heat. He gets braver as it falls.
- **Voices.** The Fixer loves him and is unreliable about him. The Suit wants nothing to do with him. The Ear hates
  every record he ever plugged. The Street knows which jocks he owns.

### 3.6 Mapping the opening answers (built)

Each answer adds +1 to the holders it fits:

| Question | Answer | Sawyer | Kubiak | Winters | Floria | Germain |
|---|---|:-:|:-:|:-:|:-:|:-:|
| Where was it? | a supper club | + | | | | |
| | a DJ's house party | | | + | | + |
| | a hotel ballroom | | | | + | |
| | the back room of a bar | + | + | | | |
| Who were you drinking with? | a man in a camel coat | | | | + | |
| | a disc jockey | | | + | | + |
| | a jukebox route man | | + | | | |
| | someone from a Major | | | + | + | |
| What were you bragging about? | the label you're about to start | | + | | + | |
| | the act you'll sign | + | | | | + |
| | money you don't have | + | + | | | |
| | the hits you can pick | | | + | | + |
| How did it end? | a card table | + | | | | |
| | a car | | + | | | + |
| | a phone call | | | + | + | |
| | nothing at all | | | | | |

Rules:

- **Balanced:** each holder fits exactly five answers, so no holder is favoured before the player answers.
- The **pool** is every holder whose score is within 1 of the top. The pick within the pool is a **stable hash** of
  the run seed and the answers, weighted 2:1 toward the top scorers. It's keyed, not drawn from the shared RNG
  stream, so it can't reshuffle the world ([[one-conversion-reshuffles-the-rng-stream]]).
- The answers the chosen holder doesn't fit are the **wrong memories**. The voices treat them as facts at first and
  are embarrassed when a clue contradicts one. A player who answers "nothing at all" everywhere gets a uniform pick
  and no wrong memories, just an honest blank.
- The variant is stored on `LienState` at founding and never changes. Saved ordinals keep the first draft's slots
  (Dace → Sawyer, Fennell → Winters, Ainsley → Floria, Lark → Germain).

### 3.7 Timing and money (analysis, 2026-11-24)

**When can a player tear it up?** 1966 is right as the date the *last* link becomes available, and far too late as
the date the hunt starts. Without a year floor, a smart player could tear up early:
- An Ex-Musician with a #1 in 1961 already has Ear 6.
- A Promo Man needs one December point and a three-region advocacy year for Fixer 6.

Against Winters, that's a tear-up in 1962, followed by seven years with no clock. So the run has three acts:

| Act | Years | What the player does |
|---|---|---|
| 1. Who | 1960–62 | Learn who holds the paper and which memories were wrong. Find the first item. |
| 2. What | 1963–65 | Recover belongings, read the evidence, find the witness. Each link eases the sitting (§3.4). |
| 3. Proof | 1966–69 | The last link opens. Tear it up at a December sitting. |

The floors apply to the last link only: Dec 1965 for Winters and Connie, Dec 1966 for most, Dec 1968 for Floria.
Every tear-up has a **sting**, so an early win doesn't empty the late game:
- Dee-Dee bans your acts from the room.
- Kubiak sues for the jukebox rights.
- Winters is ruined, and you can hire him.
- Germain names your label to the grand jury out of spite.
- Floria's firm comes after your acts openly (player-only courtship pressure).

The 1968–69 consolidation hits whether you're free or not. The epilogue rewards how early and how cleanly you got out.

**Is $20,000 plus interest a threat?** On its own, no, not for a player who's doing well. Measured on two partial
decade runs (AI labels that opened Small, `stature-marketing-decade-v2` seeds 1001/2002; the player opens with
$800–1,400, below these):

| Small-label cash | p25 | p50 | p75 | p90 |
|---|---|---|---|---|
| Dec 1960 | $2.0–5.0k | $6.0–10.0k | $14.5–21.0k | $22.4–34.6k |
| Dec 1961 | $4.7–7.5k | $10.8–15.3k | $19.8–26.7k | $30.7–39.9k |

Phase 0's median survivor holds $28–42k by 1969. Run through the slice schedule, each holder's rate gives this Pay
bar:

| Holder | Rate | Dec 1960 | Dec 1964 | Dec 1968 | Owed at the reckoning |
|---|---|---|---|---|---|
| Winters | 0% | $0 | $1,000 | $2,500 | $10,250 |
| Floria | 3% | $600 | $1,555 | $2,882 | $10,558 |
| Sawyer | 4% (×1.5 if Connie stalls) | $800 | $1,740 | $3,010 | $10,660 |
| Germain | 6% | $1,200 | $2,110 | $3,265 | $10,865 |
| Kubiak | 8%, compounding | $1,600 | $2,480 | $3,520 | $11,070 (≥$13.7k after one unpaid year) |

What this shows:
- **The rate barely moves the reckoning** ($10.2–11.1k for every holder), and the median 1969 survivor pays that
  easily. The rate shapes the yearly bill, not the ending.
- **The p90 player holds more than $20k by the end of 1961.** If early payoff were allowed, a hit in 1960 would end the
  run. The paper forbids prepayment, which is already built: `PayInFull` exists only at the reckoning.
- **Year one is evened out.** Every holder takes "a record out" as the 1960 Show (built), so Kubiak's $1,600 isn't a
  first-year wall for a player who started with $800.
- **The threat comes from what money can't buy:** Connie's career (Sawyer), compounding if you don't pay (Kubiak),
  publishing taken in Concedes (Winters), the buy-out with no money accepted from 1966 (Floria), and scandal exposure
  (Germain).
- **Phase 0 still owes:** full-decade player p75/p90 cash. Floria's threshold uses the late-decade catalogue valuations in §3.5 H4. If p90
  clears $20k by 1962 in the player's own runs, the Pay slice should scale with the holder's desperation (his sim
  interest going badly), never with the player's cash.

### 3.8 Signature belongings: one per archetype

Every archetype loses one thing that matters to *it*. Where it ends up depends on the holder; with a holder who
doesn't keep it, it's scattered and found through legwork.

| Archetype | Signature belonging | Perk | With | Otherwise |
|---|---|---|---|---|
| Ex-Musician | Your 1926 pedal steel | Ear: band-room sit-in options | Winters | A pawn-shop window (Street) |
| Pawn-Shop Owner | The back room of stock from the shop | Suit: a one-time cash value when sold, and the instruments make cheaper session dates | Kubiak's warehouse | Auctioned to a junk dealer (Suit traces it) |
| Promo Man | Your address book, with every jock's home number | Fixer: DJ Rolodex leads, and warmer first calls | Winters (he's been using it) | A rival label's office (Fixer) |
| Trade Insider | Ten years of chart files | Suit: sharper quarterly market readings (§4.1 scoring and the newsstand) | Floria (the Major's man wanted them) | The trade paper's local stringer (Street) |

### 3.9 Getting around without a car

The car is gone in every run. That's a real early-game problem: getting around town and hand-carrying records to
other towns.

| Holder | Where the car is | Getting it back |
|---|---|---|
| Sawyer | Behind the room. Dee-Dee drives it. | The first year Connie charts, or buy it back |
| Kubiak | His yard, "as security" | Buy it back; cheaper in his bad years |
| Winters | Towed from outside the DJ's house to the city impound. Fees grow monthly, and it's auctioned after a year. | Pay the impound (Suit reads the fee) |
| Floria | With the hotel valet; the garage sold it unclaimed to a used-car lot | Buy it back from the lot (Street finds it) |
| Germain | The station lot, where the manager won't release it to anybody connected to Jack | Prove you aren't (Fixer), or the manager leaves |

**Four ways to get around** (built 2026-11-24, `Systems/PlayerDesk.Transport.cs`; every figure *provisional* until Phase 0b):

1. **The bus** (the default): 42 mph plus an hour at the depot on every intercity leg, 2.5¢ a mile, and **no overtime**: the
   last bus leaves when the working day ends (6 p.m.), so a long leg that won't make it is refused. Every errand in town
   costs half an hour and 30¢. Out of town he carries **two cartons (100 copies) a trip**; the count resets when he walks
   back into the office.
2. **Taxi:** 60 mph, 22¢ a mile, and **no cabbie goes past 75 miles**. In town an errand costs $1.50 and no time. Three
   cartons a trip. Pick bus or cab at the office, before you set out.
3. **Buy a car** at the lot in town (2h): a **used 1955–57 sedan $750** (a 1958 dealer ad had a 1955 Chevrolet at $1,145) (58 mph, 2.5¢ a mile: slower and thirstier than the
   old one), or a **new car $2,000** (64 mph, 2¢: the old car's numbers). Against a first pressing of about $1.3k and
   founding cash of $800–1,400, the choice is real: press now and take the bus, or drive now and press later. A car of
   either kind ends the errand tax and the carton limit.
4. **Get yours back**, per holder (2h, at the office; everything it takes is in town):

   | Holder | Price | Condition |
   |---|---|---|
   | Sawyer | $1,100 ("high") | None. Free the first year Connie charts (built, 1b). |
   | Kubiak | $700; $450 from 1964 (his route is failing) | None |
   | Winters | The pound's fee: $60 tow + $20 a month; **auctioned after 12 months** (gone for good) | Suit 3+ reads the fee up front; anyone can still pay what the window says |
   | Floria | $800 | Street 3+ knows which lot; otherwise the lot's advertisement turns up from 1962 (stands in for the legwork map, Phase 4) |
   | Germain | $0 with Fixer 3+ (one phone call); otherwise $120 storage from 1964, when the manager's gone | Blocked until then |

   Getting it back restores the old car's speed and gas, and (later phases) is where Sawyer's marked deck lives.

**Hook.** Every trip now goes through `PlayerDesk.Travel` → `TravelQuote(from, to)`, which returns (hours, cost) for the mode
he's in; `DriveQuote` is still the car's own quote, and the band's studio trips still use it (the band drives its own van).
Errands in town (`PitchAtStop`, `ConsignAtStop`, `ServiceStop`, the hop table, the acetate pitch) go through
`RequireErrand`/`SpendErrand`; stock placement away from home reads `CarryLeft`. State lives on `LienState` (`Car`,
`CarRecovered`, `Mode`, `TripCopies`; an older save keeps the car it had), and a desk with no lien keeps the car and quotes
exactly what it always did. All of it is player-only. UI: the GETTING AROUND block at the top of the travel section
(`UI/PlayerDeskPanel.Transport.cs`). Checked by `SimTools/TransportChecks.tscn`.

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
| ~~The diner where the jocks eat~~ | ~~Overhear, make a courtesy approach~~ (dropped 2026-11-24: incremental, adds a place but no stakes) | DJ courtesy, Rolodex discovery |
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

### 6.1 The first single: the holder shows his hand (author direction, 2026-11-24)

The wait isn't short of verbs. Hops, station stops, record shops, courtesy calls, mailing and flips all exist. It's
short of **stakes**. A real independent spends the weeks between the lacquer and the first spin feeling that
something is about to happen or go wrong. So the wait gets events that carry a clock, not more places to visit.

**The spine.** Every holder's 1960 Show is "a record out" (§3.7), so the first single is the one thing all five
care about. During the wait, the holder starts circling the record in his own way, and that move is how the Label
Man learns who holds the paper. Act 1, "Who" (§3.7), happens in the first months, not at the December sitting.

- **Today:** the holder is named only when the first sitting opens (`OpenLienSitting` sets
  `LienState.HolderIdentified`) or after an extension or seizure. **Change:** the holder's first move sets it.
- **Trigger:** the player's first single goes to the plant. The holder is already fixed at founding (§3.6). His move
  lands while the record is at the plant (*provisional*: a few days after the order, never after the street date).
- **One move, one real choice.** Each move is an offer on a channel that already exists or is already built for
  that holder. Accepting is logged on the lien ledger as a concession, and the December sitting reads it like any
  other Concede. Built: it fully meets that year's Concede, and eases the money by half the year's interest
  (*provisional*). Refusing has the
  consequence the holder's existing handicap already produces, plus a voice line. Nothing is invented.
- **The voices' first theories get tested.** The Fixer guessed cards, the Ear a late session, the Suit "I'd never
  sign that", the Street a woman (§3.1). Whoever turns up confirms one theory or embarrasses another, and the
  winning voice gloats for a week.

**Built 2026-11-24 (Phase 1e)**, as `Systems/PlayerDesk.HolderMove.cs`, `UI/PlayerDeskPanel.HolderMove.cs` and
`SimTools/HolderMoveChecks.tscn` (140 checks). What was built, against the design table below:
- **The trigger.** Ordering the first run of the first single (by `OrderPressing` or the plant's credit run) stages
  the move two days out, and never on or after the run's arrival. The move lands at dawn and waits on the desk as a
  full-screen sheet. Answering it, or letting the run arrive first, sets `HolderIdentified`. A lapsed move counts
  as refused, except that Floria's favour stands because nobody asked for it back.
- **The lien.** Every answer goes on the lien ledger (`LienLedgerKind.HolderMove`). Accepting raises the lawyer's
  standing by 0.1 and refusing lowers it by 0.05 (*provisional*).
- **The December sitting reads it** (built 2026-11-24). That year's sitting opens on the answer, in the lawyer's
  voice. A taken favour is that year's **Concede**: a "Remind him…" option on the CONCEDE card rolls the lien, and he
  books it against **half the interest** owed at the sitting (*provisional*); the till pays the other half if it can.
  Floria's favour counts even if the move lapsed, because nobody sent it back. It counts once (`LienState.MoveCredited`).
  At the 1960 rates that's about $390 for Sawyer, $780 for Kubiak, $580 for Germain, $290 for Floria and nothing for
  Winters (0%). A refusal or a lapse gets a line and nothing more: its price is the holder's handicap, already paid
  during the year. The Suit (2+) prices the favour; the Fixer (3+) warns after a refusal.
- **The voices.** The voice whose morning answer fits the holder gloats, and needles one whose answer doesn't.
- **Each holder:**
  - **Sawyer.** He's named from the first morning, because Connie's contract carries his name: `SeedConnie` sets
    `HolderIdentified`, and the morning after shows the contract. His move still fires; for him it's the in-person
    visit, not a reveal. Accepting waives his Connie booking gate for the year, and it's a **promise** (built
    2026-11-24): Connie's record has to be out within **8 weeks** of your single's street date (*provisional*). Out first
    or in time, the favour stands. Not out by then, and the next dawn he takes the waiver back, the lawyer's standing
    drops 0.15, and the December sitting reads the launch as a debt, not a favour: no favour Concede. So taking his room
    means funding a session for the weakest act on the roster in the leanest weeks of the year.
  - **Kubiak.** Accepting takes min(200, half the saleable run) off the run on delivery, at the plant's price, paid
    in cash. The copies go on his machines the day the run comes in: that single gets a +0.04 appeal lift at the home
    town's counters, fading to nothing over 12 weeks (*provisional*; `PlayerDesk.Kubiak.cs`). Refusing
    shuts the home city's jukebox operators to your records for the rest of the year.
  - **Winters.** He takes the flip's *publishing*. It's too late to put his song on a record that's already at the
    plant. The option is disabled when the flip's publishing isn't the label's. Refusing burns the biggest of his
    cold hometown cards (§3.5 H3).
  - **Germain.** Accepting takes the $300 fee and 0.05 exposure up front. His network is placed the day the single
    ships.
  - **Floria.** The plant's queue is the player's own (`pressOrders`), so moving the run up is player-only. Accepting
    keeps the earlier date, and turning it down restores the original date.

| Holder | The move | Accept | Refuse | How it names him |
|---|---|---|---|---|
| **Sawyer** | Dee-Dee walks into the office: the launch is at *his* room, and Connie's record comes out first or the same week. | Connie's session goes ahead of or beside yours. Her release also opens the room's booking gate (1b). | You launch elsewhere. The room stays shut to your acts until Connie has a 1960 release (built). | Connie's paper already sits on the desk at founding, so the Suit half-knows. This is where Dee-Dee confirms it in person, wearing your watch. |
| **Kubiak** | The plant man shows you a claim on 200 copies of your run "at cost, for the route", on an operator's form with clause 14. | 200 copies come off the top of the run at cost and go onto his machines (home-region jukebox exposure, scoped to the player's label; check before building, as in H2). | His jukebox rights stand and nothing goes on any machine in the home region this year. | His name is on the form. **First clue:** Suit 3+ notices the notary stamp (Leon Marsh) and files it. |
| **Winters** | Clyde arrives with a lead sheet for the B-side: "the publisher's expecting it." | His song goes on the flip. That's a publishing share (his Concede), at a real cost if the record is ever flipped. | He tells his publisher you're cutting it anyway, and one of your hometown cards cools. | He mentions a jock whose home number only your address book holds. The Street catches it. |
| **Germain** | A phone call in the dead week before the street date: his boys can have it on the air by Friday for $300. | The built "his boys" Concede (§3.5 H5), offered before December: eight weeks, plus 0.05 exposure. | Nothing, except a long, friendly monologue. | It's the voice from the radio. The Street places it at the farewell broadcast. Setting `HolderIdentified` here opens the ledger trail early, but the 78s clearance is still seeded 1961–64. |
| **Floria** | The plant moves your run up a week, and the plant man says the Major's regional office "took care of it". | Nothing to accept: the favour is done, and he logs it. It counts toward his steep Show bar and he mentions it at the sitting. | You can insist on the original date, which costs the week. | A camel-coat man's card is left with the plant man. Check first that the player's pressing date is a player-only quote, not a shared plant queue (invariant 1). |

**Around the spine (candidates, not yet decided).** These beats fill the rest of the wait. Each one reads state the
sim already holds.

- **The first spin.** Get a dub to one jock, then sit at the desk that night with the radio. He plays it or he
  doesn't, according to the real advocacy and spin outcome. The next morning the shop clerk says whether anyone
  asked for it. The Ear hears what AM compression does to the mix.
- **The loop.** The distributor won't take it without airplay, the jock won't play it unless it's in the shops, and
  the shop won't order without the distributor. Show the deadlock as a small board of who's waiting on whom, with one
  way through per voice: Street hand-carries 100 copies into the jock's city; Fixer leans on the jock; Suit takes
  consignment with the one-stop.
- **The Run.** When the cartons arrive: a multi-day bus trip through the region with two cartons, the last bus at
  6 p.m., and a short scene at each stop (§3.9 makes this possible). He comes home to news on the desk.
- **The flip.** When a real flip outcome fires: "they're playing the other side in Cleveland." Re-service, re-label
  or hold.
- **The hit that breaks you.** A breakout brings a reorder the label can't fund: the plant wants cash, and the
  distributor pays in 60–90 days. The backer, Kubiak's one-stop, Germain and the merchant lender each offer a way out
  at a price.
- **The bragging comes due.** The founding "What were you bragging about?" answer becomes a public promise. "Its
  first big record" on Jack's broadcast means kids are asking shops for a record that doesn't exist yet. "The act
  you'll sign" is waiting to hear from you. "The hits you can pick" brings a trade stringer who wants your pick in
  print, which could be the first conviction offer.

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
| **0b** | Measure for the holders (§3.7). Floria's late-decade valuation comparables are recorded in §3.5 H4. Full-decade p75/p90 player cash is pending. | Numbers recorded |
| **1** | The Lien: morning-after opening, December sitting, enforcement on failure, 1970 reckoning with endings. **Core built**: the five holders, per-holder rates, Kubiak compounding, Floria refusing money from 1966, every holder taking a record out as the 1960 Show, Sawyer's room from the scene catalogue, the §3.6 mapping, save fields, the founding questions, and the sitting UI (2026-11-24). | Byte-identical headless; save/load; `LienChecks`; `LienUiChecks` |
| **1b** | **Connie built 2026-11-24** (`Systems/PlayerDesk.Connie.cs`, `ArtistManager.Keyed.cs`, `SimTools/ConnieChecks.tscn`): seeded from a private keyed stream with Dee-Dee's paper (5 years, his option, 15% royalty, 5 sides, no advance). He renews her paper at dawn until Star, charges 1.5× Pay when she stalls, returns the car when she first charts, and gates nights and residencies for other player acts on a Connie release in each performance year. **Germain Concedes and payola exposure built** (`Systems/PlayerDesk.Germain.cs`, `Systems/Radio/PayolaLedger.cs`, `SimTools/GermainChecks.tscn`). *His name:* 25% of the writer share on the next two originals with a credited writer, paid through mechanicals, plug-side performances and AI-label cuts. Each cut-in adds 0.06 exposure, costs each credited writer 0.12 trust, and adds 0.08 band strain. *His boys:* $300 for eight weeks on half of the home reporters, rounded up to a maximum of six. Discretion starts at 0.55, falls by 0.12 per use, and bottoms out at 0.05. Each fee adds 0.05 exposure. A bust ends the push across the network. Under the Detroit calibration the first 1960 push delivered 11.89 boost-weeks with a 15.1% bust rate (§3.5 H5). The station and record-shop trail for Jack's black ledger is built. Fixer 3 reads it and freezes concessions and pending cut-ins. **Pending:** Sawyer's watch (wardrobe phase). | Byte-identical headless; save/load; `ConnieChecks`; `GermainChecks` |
| **1c** | Getting around (§3.9): transport mode on the travel quote and errands, carton limit, car purchase, the car per holder. **Built 2026-11-24**, UI included; Sawyer's car return is built in 1b. | Byte-identical headless; save/load; `TransportChecks` |
| **1d** | Floria's buy-out (§3.5 H4) is built as a player-only path, with a funded rival offer, valuation waiver and successor. The separate tear-up investigation remains future work. | Same; the Major's AI behaviour unchanged |
| **1e** | The holder's first move on the first single (§6.1): the move per holder, `HolderIdentified` set by it, and the accept/refuse result on the lien ledger. **Built 2026-11-24** (`PlayerDesk.HolderMove.cs`, UI sheet). The December sitting reads the answer (a favour is that year's Concede, against half the interest). Kubiak's route exposure, Winters' hometown cards and Sawyer's promise (Connie's record within 8 weeks) are built (§3.5 H2, H3, §6.1). | Byte-identical headless; save/load; `HolderMoveChecks` |
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
8. **"Torn up":** extremely hard, gated by year, instinct and recovered item (§3.4). Revised 2026-11-24: the year floor
   is on the last link only. That's Dec 1965–66 for most holders and Dec 1968 for Floria (§3.7).

9. **Lien size:** $20,000 face sum (1960 dollars). Phase 0 still measures player cash trajectories, to set each
   December's bar and to confirm $20k can't be cleared early but can be met by a real national run.
10. **Holder variants:** start with 5. Who they are needs a writing pass.

**Author decisions (2026-11-24)**

11. **The five holders:** Dee-Dee Sawyer, Walter Kubiak, Clyde Winters, Gene Floria and Jack Germain (the payola jock),
    with two items each, a tear-up route that each Sense leads, and incompetent holders opposite a bungling Label Man
    (§3.4–3.5).
12. **Sawyer's room** is the hometown's real most popular room from the scene catalogue, not an invented one. His
    girlfriend is **Connie Boursaw**, and her contract **can't be dropped** (§3.5 H1).
13. **Floria's "too big to swallow" bar** is set from Phase 0 valuations (§3.5 H4, Phase 0b).
14. **Jack's cut-ins** always cost standing with the song's writer, whatever the Label Man's ear (§3.5 H5).
15. **Rates** were run against Phase 0 cash (§3.7): 0 / 3 / 4 / 6 / 8%, with Kubiak's compounding.
16. **Every archetype** has a signature belonging (§3.8).
17. **No car at the start:** public transport, taxi, buy one, or get yours back (§3.9).

## §13 Still open

1. The gold-record feat bars (especially Suit's profit bar): separate design pass.
2. The wardrobe list per year: separate design pass.
3. ~~Car prices, fares and carton limits for §3.9~~ Set provisionally 2026-11-24 (§3.9). Phase 0b still owes the check
   against the player's own cash: is $750 the right price for the used sedan (about 94% of the Ex-Musician's $800 start) next to a first pressing?
4. Each holder's tear-up "sting" (§3.7): needs a writing pass.
5. **AI covers of player sides** (author: "it should be a thing", 2026-11-24). Today a player original becomes an AI
   cover candidate only once it peaks in the national Top 40: `CompositionCatalogService` marks it `isCoverable` and
   calls `RegisterCoverableHit`, and the material selector can then draw it as a `CoverRecentHit`. So covers *after*
   the hit already exist. What doesn't exist is the period **cover race**: a bigger label cuts a regional breakout
   before it crosses over, and the original has to beat it to the shops. If the trigger reads only player sides, it is
   inert headless. If it applies to every label, it changes the AI economy and needs its own decade A/B. That belongs in
   the publishing and cover-song directive, not here. Once it exists, §6.1 can add "the cover race" as a beat.
