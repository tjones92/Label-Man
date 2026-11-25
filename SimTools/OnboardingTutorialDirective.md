# Onboarding and the New-Player Tutorial: playtest notes and plan

Status: sketch for discussion (2026-11-25). Nothing here is built. It comes from one scripted playthrough of the real
UI, read against the new-player notes below. Read with `SimTools/LabelManInnerLifeDirective.md` §3.1–3.6 (the
opening and the holders) and §6 (the empty early game).

## 0. What we were told

New-player notes:

1. *"Prioritize a tutorial for the first signing. As a new player I'm encountering a lot of friction in the form of
   'what do I do now? where am I meant to click?'"*
2. *"I don't feel like I have any frame of reference for how valuable time is."* This ties in with the coming
   fatigue mechanic.
3. *"As a player I don't understand what is being asked of me."*
4. *"Leaning into the clickable office as the main 'menu' could be a lot stronger than the dense desk menu. 'OK, I
   need to scout for acts'? Click the door."*

The author adds: after the four "What do you remember?" answers, the label info and the town, the player should get a
short, vague scene that sets up their situation. With Dee-Dee, for example, they should know they're stuck with his
girlfriend as a signee.

## 1. The playthrough

The game was played through the real UI by a temporary driver scene. It pressed buttons by their visible text,
screenshotted after every press and dumped the visible text and tooltips. The setup: NEW GAME, the full intro, the
Sawyer answers (supper club / nobody / "a kid you'd heard once" / the card table), Trade Insider, Detroit, "Motor Mouth
Records". The holder came out as **Sawyer**. After founding, I played like a new player: follow NEXT UP, read the page,
press what looks right.

| # | In-game time | Screen | What happened | Friction |
|---|---|---|---|---|
| 1 | — | Title | NEW GAME | none |
| 2 | — | Intro | The walk, the letter ("SET IT DOWN"), the drawer, the voice, "the maw", back in the drawer | Strong. The voice's speech is typed at 62 chars/s and runs ~7 s before the options appear. |
| 3 | — | LAST NIGHT ×4 | One question to a page; the haze lifts with each answer | Strong. Nothing says the answers matter. That's on purpose, and fine. |
| 4 | — | Background | Four cards, instinct pips, label stat pips | The pips have no scale until you hover. Nothing says what a 3 means or which instinct you'll lean on first. |
| 5 | — | Home town | Map, $900, $80/mo, "11 months of rent in hand", the bank rule | Good. This is the only place the game frames time as money. |
| 6 | — | Name & look | Name, crest | none |
| 7 | Jan 1, 9:00 | The Morning After | Letter with **"Held by: ____"**, cigar box $900, what's gone, the PAY / SHOW / CONCEDE terms, four voices' theories, the 1960–1970 strip | Atmospheric, but it's a wall of rules. **Nothing mentions Connie**, and the player can't tell what to do *today*. |
| 8 | Jan 1, 9:00 | Desk → A&R | GET TO WORK lands on A&R, with the Michigan Roadhouse calendar open (whichever venue was selected) | Dense first screen: 6 venue buttons, a 3-week calendar grid, 3 set rows, booker, room notes, two folded sections. NEXT UP says "Scout an act or review the latest office news." |
| 9 | Jan 1 | NEXT SETS → WAIT | My first "WAIT" press hit the header's **WAIT 1h**. The set's button is also labelled WAIT. | Two meanings of WAIT on one screen. |
| 10 | Jan 1, 12:00 | LISTEN | Rodney Tillman, roadhouse. Notes go into the notebook automatically. | WAIT only gets you there; LISTEN is a second press. The read ("voice struggled… lost the room") gives no sense of whether this act is worth signing. |
| 11 | Jan 1 | TALK → IF THEY'D HEAR AN OFFER | No; ask again after Apr 1 | The card still says "you can ask if they'd hear one. Asking is free." under the refusal. |
| 12 | Jan 1, 18:00 | River Lantern (Dee-Dee's room) → LISTEN → ask | Tito Clark & the Riffs will think it over and answer by Jan 9 | The holder's own room says nothing about Dee-Dee or Connie. |
| 13 | Jan 2, 9:00 | END THE DAY → Morning Paper | Tito says yes overnight. The Hot 100 comes first; our news is fourth. | The paper's masthead reads **JAN 1** on the Saturday Jan 2 edition (check: is it meant to be yesterday's date?). |
| 14 | Jan 2 | TALK → TERMS → contract | Contract sheet: advance, royalty, term, deliverables, publishing, creative control, three promises, cost block | "Wants the reins: *label* creative control" reads backwards, and "Long exclusive term" sits next to a 2-year ask. Nothing says whether $35 / 1.5% is cheap or dear. |
| 15 | Jan 2, 11:00 | TABLE OFFER | Signed. Card: **MMR-002**. NEXT UP: "Manage an act to choose material and cut a record." | The player never met MMR-001 (Connie). |

**The headline:** the mechanics are fast. A first signing took **two in-game days and about a dozen presses**. What
slows a new player is that **the path is invisible**. Every step was findable only because I was reading button text
in a dump. Find a set, WAIT, LISTEN, TALK, ask, end the day, TERMS, TABLE OFFER: nothing on screen hands you this
sequence. The game doesn't need a new verb for the first signing. It needs to show the six it has, in order, once.

## 2. Findings

### A. "What do I do now? Where do I click?"

- **NEXT UP is the only guide, and it's too generic at the moment that matters.** On day one it says "Scout an act or
  review the latest office news". That names two jobs, and the second one doesn't exist yet. It's driven by
  `NextUpStep` (`UI/PlayerDeskPanel.cs:611`). Because Connie fills a roster slot, the `CurrentRosterSize == 0` branch
  ("Hear an announced set…") **never fires on a Sawyer run**. The Sawyer player gets the vaguer line.
- **The first desk screen is the densest screen in the game.** GET TO WORK drops you into A&R with a venue's full
  calendar open, not into a short list that says "here are three sets you can hear today".
- **Each step needs a second press nobody mentions:** WAIT then LISTEN; TALK then IF THEY'D HEAR; TALK then TERMS then
  TABLE OFFER.
- **Reads have no yardstick.** "The voice struggled to hold the songs" is good fog-of-war writing, but a new player
  can't tell whether a mediocre read is normal for a roadhouse act or a reason to walk away. The voices (§5 of the
  inner-life directive) are the natural people to say so.

### B. No frame of reference for time

- Time costs are written three ways: "180min wait", "2h", "fits today's remaining hours". The day's budget appears
  only in the clock widget ("IN 9:00 OUT 6:00 · O.T. to 9:00 · 9h left") and in a tooltip.
- Nothing says what an hour is *worth*. A 3-hour wait for a 1-hour set looks free ("Waiting charges no admission").
  What it really costs is the other things you didn't do, and the game never names them.
- The only time-as-money frame is on the founding town page ("11 months of rent in hand"), and it never comes back.
- **Hook for fatigue:** the fatigue design isn't written yet. Whatever it becomes, onboarding needs from it (a) a
  visible daily budget the player spends, (b) a cost in that budget on every action button, and (c) a first-week
  moment where running out *matters* (you can't make the 6 pm set because you burned the afternoon). See §3.4.

### C. "I don't understand what is being asked of me"

- The game *has* a clear goal for 1960: get a record pressed and out by December (the Show bar). It also has a clear
  holder goal: "Dee-Dee wants a record by Connie, out." **Both are buried.** The first is in the Morning After's rules
  block. The second is only on the OFFICE tab under THE PAPER, which a new player has no reason to open.
- Nobody tells the player **what a label does** in order: sign, choose songs, record, press, ship, sell, get played.
  NEXT UP reveals one step at a time, so the player never sees the whole road or where they are on it.
- The Morning After explains *consequences* (miss one and he seizes, miss twice and he takes the label) before the
  player knows what the job is.

### D. The office as the menu

What the office has now (`UIManager.BuildDeskProps`, `UI/UIManager.cs:795`): six hover-only hotspots (phone →
ROLODEX, two stacks → CATALOG, typewriter → LEDGER, checkbook → FINANCES, the pad → A&R) and an "OPEN THE OFFICE"
button. The **door is painted but isn't a hotspot**, and you can't see what's clickable until you hover. The real menu
is the nine-tab folder.

The note is right: the office already maps onto the game's verbs. It just doesn't say so.

| Object | Verb it should mean | Today |
|---|---|---|
| **The door** | Go out: scout, town errands, rooms, travel | not clickable |
| The pad / notebook | Your acts: the notebook, then the roster | A&R tab |
| The phone | Calls: DJs, bookers, the acts' numbers | ROLODEX |
| The stacks | Your records: masters, singles, stock | CATALOG |
| The typewriter | The ledger: what happened | LEDGER |
| The checkbook | Money | FINANCES |
| The flip calendar | Time: end the day, skip to an event, see the December sitting | not clickable (SKIP DAYS… is a desk button) |
| The drawer | The lien papers now; the Conviction Drawer later | OFFICE tab, THE PAPER |
| The clock / window | Wait, wait for evening | desk header buttons |

### E. The holder scene, and a contradiction in the mystery

- The Morning After deliberately shows **"Held by: ____"**, but the same desk names the holder everywhere else:
  Connie's roster card says "Dee-Dee Sawyer's paper: his option, no release clause", and the OFFICE tab says "Dee-Dee
  wants: A record by Connie, out." So the mystery of *who* is spoiled by the UI on day one, and **the player isn't
  told the one thing they need**: there's a singer on your roster you didn't choose, and the man who owns your lien
  wants her record out.
- The directive's three acts make "Who" the 1960–62 mystery (§3.7). That conflicts with the author's direction above
  (tell the player they're stuck with Connie). Proposal: **reveal who, keep how.** The player learns on day one who
  holds the paper and what he wants. The mystery is what happened at the table, which memories are wrong, and the
  holder's blunder (the tear-up). The voices' theories already argue about the *how*, so they keep their comedy.
  *Needs an author decision* (§6).

### F. Small copy and consistency bugs found on the way

1. "Held by: ____" vs the holder named on the roster and the OFFICE tab (E above).
2. After a refusal, the act card still reads "They aren't looking for a deal, but you can ask if they'd hear one.
   Asking is free." The header correctly says "ask again after Apr 1".
3. Two kinds of **WAIT** button on one screen: the header's time-skips and the set row's "go and wait for this set".
   Suggest renaming the set row's to **GO · WAIT 3h** or **CATCH THIS SET**.
4. The Morning Paper masthead read "SATURDAY EDITION · JAN 1, 1960" on Jan 2. Check whether that's meant to be the
   date of the news.
5. Tito's card said "talk to them after a set you've heard that day", yet TERMS — MAKE AN OFFER was live from the
   desk the next morning. Either the line or the gate is wrong.
6. "Wants the reins: label creative control, $35, 1.5%. Long exclusive term." "Wants the reins" plus *label* control
   reads backwards, and "long" sits beside a 2-year ask.
7. The first signing is catalogued **MMR-002**. MMR-001 (Connie) was never introduced.
8. Tooltip jargon a new player can't parse: "Waiting advances ordinary expenses and other labels' decisions."
9. GET TO WORK opens A&R on whichever venue was last selected (the Michigan Roadhouse calendar), not on a short list
   of today's sets.

## 3. Proposals

### 3.1 The first morning: a holder scene after founding (author request)

Between OPEN THE DOORS and the Morning After's rules, play a short, vague, **holder-specific** vignette: two or three
beats, one choice at most, nothing that touches the sim. Its job is to answer "what's my situation?" in story terms
before the rules page answers it in legal terms.

Each holder gets one, built from facts the run already holds (`LienState.Holder`, `Venue`, Connie's record):

- **Sawyer.** A knock. A young woman with a garment bag and a sheet-music folder: Connie Boursaw. "Dee-Dee said you'd
  be expecting me." She has a copy of a contract you don't remember signing. The voices react: the Street ("there's
  always a woman", now half right and embarrassed), the Suit (reads the paper: five years, his option, no release
  clause), the Ear ("can she sing?"). The scene ends on the River Lantern's matchbook in her hand: *the room is
  Dee-Dee's.* So the player leaves knowing who holds the paper, that Connie is theirs, and where to find both.
- **Kubiak.** A route man's boy pulls the office jukebox's coin box and leaves a receipt.
- **Winters.** A friendly note and a box of song sheets "to get you started."
- **Floria.** A Major's car idling outside; nobody gets out.
- **Germain.** A jock on the radio says your name between records.

One sentence of direction per holder, sketched here only; each is a short authored beat. This **replaces** "Held by:
____" with a name (§2E) and moves the PAY / SHOW / CONCEDE rules to after the scene, or behind a "Read the letter"
fold, so the first thing a player reads isn't a rules block.

### 3.2 The first-signing tutorial: "The first week"

A **guided first week** that rides the real verbs, not a separate sandbox. Its spine is a short **to-do card**
pinned to the office (on the pad, or slipped under the phone), written in the Label Man's own hand. Each line ticks
off from real state, the way NEXT UP already reads state:

1. ☐ *Go out and hear somebody.* (The door glows. Clicking it opens a short "today's sets" list, not the venue
   calendar.)
2. ☐ *Write down what you heard.* (The first LISTEN opens the notebook with one voice explaining the read: "a
   roadhouse eats a voice; don't judge the singer by this room alone.")
3. ☐ *Ask if they'd hear an offer.* (The first TALK highlights the one ask, and says plainly that a no is normal.)
4. ☐ *Sleep on it.* (END THE DAY, and the Morning Paper tells you who answered. While you wait, the card points at
   Connie: "Dee-Dee's girl needs songs.")
5. ☐ *Put a deal on the table.* (The first contract sheet gets the Suit's one-paragraph primer: what the advance and
   royalty are, what's normal for an unknown act in 1960, and "the standard paper is fine for a first deal.")
6. ☐ *Sign them.* The signing card then hands off to the next card: *Cut a record.*

Rules:

- **Teach through the voices, not a narrator.** The four voices already exist and already have opinions. The
  tutorial is them talking the Label Man through his first week, which fits the hangover comedy. Each voice teaches
  the system it reads: Ear the listening reads, Street the rooms and who's moving, Suit the contract and money, Fixer
  the bookers and numbers.
- **One highlight at a time.** Highlight the next thing to press, never the whole screen.
- **Never block.** Any step can be done out of order or skipped. The card just ticks. A "No help, I've done this"
  toggle on the NEW GAME / background page turns it off.
- **The Sawyer run doubles up on purpose.** Connie is already signed, so the card's "cut a record" step can point at
  her if the player hasn't signed anyone. That also teaches the 1960 Show bar ("a record out by December").
- **It's NEXT UP, grown up.** `NextUpStep` already knows the state machine. The first-week card is its first six
  steps, with more voice and the whole list visible, so the player sees the road ahead. Once the first-week card is
  finished, NEXT UP takes over as it does now.

Later cards, same shape, once the first one works: *Cut a record* (manage, songbook, studio), *Press and ship*,
*Get it played*.

### 3.3 The office as the main menu

Make the room the primary navigation and demote the folder to "the files":

- **Every verb object is clickable, and visibly so.** Give each a faint at-rest cue (a lit edge, a slip of paper)
  for the first week, then hover-only. Add the **door** (go out: today's sets, the town, travel), the **calendar**
  (time: end the day, skip days, the December sitting) and the **drawer** (the paper; later, convictions).
- **Clicking an object opens its job, not a tab.** The door opens a small "where to?" card: today's sets, the record
  shops, the plant, another town. It doesn't open the A&R folder with a venue calendar. The folder tabs stay for
  players who want everything.
- **The to-do card from §3.2 lives on the desk**, so the room is also where you see what to do next.
- **Message slips already exist** (`MessageSlips`, opening a tab). Lean on them: "Tito's side called: they'll hear an
  offer" as a slip by the phone is clearer than the fourth line of the Morning Paper.

### 3.4 A frame of reference for time (and the fatigue hook)

- **One unit, everywhere:** hours of the Label Man's day, written as "3h" on every action button, the same way the
  contract sheet writes "Meeting 2h · fits today's remaining hours".
- **Show the day as a budget.** The clock already knows 9h regular + 3h overtime. Draw it as a strip of hour blocks
  that fill as you spend them (walking, waiting, listening, talking), so a 3-hour wait *looks* like a third of a day.
- **Name the opportunity cost once.** On the first long wait the Suit or the Street says what else three hours buys:
  "That's the afternoon. You could have heard the Coffeehouse set and still made the Lantern."
- **Bring back the money clock.** "11 months of rent in hand" from the town page belongs on the desk header next to
  cash, and the December sitting's countdown ("366 days to run") belongs on the flip calendar.
- **Fatigue (design to come):** if fatigue is the hangover's long tail, the first week is its tutorial. The Label Man
  starts the run worn down (the intro already plays the hangover), the day budget is shorter until he rests, and
  overtime costs tomorrow's hours. That gives time a felt value from day one without a lecture. The fatigue design
  should settle the unit (hours lost? a stamina strip?) before §3.2's copy is written.

### 3.5 Say what's being asked: a three-line goal

Pin three lines to the desk, always visible and always true to state:

- **This year:** a record pressed and out by Dec 18 (Mr. Halloran sits down). *Dee-Dee wants it to be Connie's.*
- **This week:** sign an act / cut a record / get it pressed (from the first-week card).
- **Today:** the next action (from NEXT UP).

The year line comes from the lien (`LienSittingDue`, the Show bar, `LienProfile`). The week line comes from the
tutorial card, and today's from `NextUpStep`. Nothing new is computed; it's three existing facts in one place.

## 4. Phases

| Phase | Scope | Touches | Check |
|---|---|---|---|
| **T0 copy** | §2F fixes 1–6, 8, 9 ("Held by" filled in per decision 1); NEXT UP's day-one line on a Sawyer run | UI strings, `NextUpStep` | Re-run the playtest driver to a first signing |
| **T1 holder scene** (built) | §3.1 Sawyer first, then the other four; the mystery reframed as *why* | `PlayerDesk.FirstMorning.cs`, `PlayerDeskPanel.FirstMorning.cs`, Morning After order | UI driver screenshots per holder |
| **T3 first-week card** (built) | §3.2 through an act signed and a first record set up (decision 3); each voice teaches its task (decision 2); highlight, off toggle | a tutorial state on `PlayerDesk` (player-only, saved), a card widget | Playtest metric below |
| **T4 time** | §3.4 hour strip, one-unit costs, money clock; fatigue hooks once that mechanic exists | desk header, action buttons | Playtest |
| **T2 office menu (last)** | §3.3 and decision 4: door, calendar, drawer hotspots; objects as the way in, tabs as the way across | `UIManager.BuildDeskProps`, a small door card | UI driver |

**T0 built (2026-11-25), uncommitted.** Re-played through the real UI on a fresh Sawyer founding:
- The Morning After's letter names the holder whenever the run already identifies him (Sawyer from day one), and a
  new **UNDER THE LETTER** slip tells the player about Connie. The narration had always said it, but the page only
  rendered its first and last lines. Naming Kubiak, Winters, Floria and Germain at founding waits for their T1 scenes
  (today `HolderIdentified` is set at their first move).
- NEXT UP's day one on a Sawyer run: "Go and hear an announced set: open FIND A SET."
- A&R opens on NEXT SETS. The set row's button is **GO & WAIT**, and the NEXT SETS help text now names "VENUES &
  CALENDARS".
- The act card follows the last answer ("They said no for this season…" / "They're thinking over your interest…");
  a Ready act reads "will hear an offer · open their card to talk terms".
- The paper's masthead carries the day it lands; a multi-day paper puts the covered dates on its heading.
- The Svengali ask is rewritten (string only; this run's acts were unmanaged, so it wasn't seen in play), and the
  "Waiting advances ordinary expenses…" tooltips now say the bills keep running and rivals keep moving.
- Not exercised in play: the "said no" hint (both acts asked said yes). Noticed in passing: an unmanaged act's button
  reads OFFER CONTRACT, a managed one's TABLE OFFER.
- `SimTools/ScoutingUiChecks` was already stale: it pressed "VENUES & DATES" (renamed in 83fe92d) and still expects the
  old venue handbills. The press is fixed; it now fails at the handbill assertion (line 59), which predates this work.

**T1 built (2026-11-25).** A **knock at the door** plays before the Morning After, once per session while the morning is
pending (`PlayerDesk.FirstMorning.cs`, `UI/PlayerDeskPanel.FirstMorning.cs`):
- One scene per holder from facts the run already holds: Connie at the door with her carbon and the River Lantern's
  matchbook; Kubiak's boy emptying the office jukebox ("against account"); Winters' carton of plugs from his publisher
  with a friendly note; Floria's sedan with company plates and a card naming his Major's regional office; a dedication
  from Jack Germain read by the morning man on a home-region station. Up to three voices react (instinct 3+, or the
  player's strongest if none). No choice and no sim effect.
- A pinned **WHERE YOU STAND** slip says it plainly ("Walter Kubiak holds your paper. He wants money: 8% a year, and what
  you don't pay goes on the sum."), then "Why he wanted your paper at all, you've no idea." READ THE LETTER opens the
  Morning After, whose letter now names every holder.
- **Named vs met.** `LienHolderIdentified` is now always true with a lien. The saved `HolderIdentified` flag means *met*
  (`HolderShownHand`: the first move answered or lapsed, or a sitting opened) and still gates what follows from meeting
  him: Clyde's secretary and book lead, the card line naming Clyde, Jack's ledger station. No new save field; Connie no
  longer marks Sawyer met at founding, and the first move's gate exempts Sawyer so older saves still get his.
- The inner-life directive's Act 1 is now "Why" (§3.7 table, §6.1 note).
- Checks: `HolderMoveChecks` (262, first-morning lines for all five), `LienUiChecks` (58, windowed screenshot
  `SimLogs/lien-ui-first-morning.png`), `WintersChecks`, `GermainChecks`, `KubiakChecks`, `ConnieChecks`, `LienChecks`,
  `HolderMoveUiChecks` all pass. Only Sawyer's scene was seen rendered; the other four were read from the check log.
  No AI byte-comparison was run (nothing here is read by the sim).

**T3 built (2026-11-25).** The **first-week card** takes NEXT UP's place under the desk header until it's done
(`PlayerDesk.FirstWeek.cs`, `UI/PlayerDeskPanel.FirstWeek.cs`):
- Six lines, all visible, each ticked from real state so any can be done out of order: *Go out and hear somebody*
  (a heard set in the notebook), *Ask if they'd hear an offer* (any direct approach), *Sleep on it* (an approach
  answered yes), *Put a deal on the table* (a terms discussion opened, the one saved flag), *Sign them* (an act of the
  player's own on the roster; Connie doesn't count), *Set up a first record* (a session booked, or anything past it;
  Connie's record counts). A later line ticks the ones it implies.
- The line you're on is taught by its voice (decision 2): the Ear how to hear a set and, once you have, what a read
  is worth ("a roadhouse eats a voice"); the Street how to ask, and that a no is normal and only for the season; the
  Fixer the wait; the Suit the advance, the royalty and that the standard paper is fine; the Ear the studio. On a
  Sawyer run the Street points at Connie while you wait, and again if you have nobody of your own at the record step.
- **One glow at a time:** the next button to press pulses, modal buttons first, rechecked twice a second (the act's
  conversation opens without a refresh). Studio buttons carry their act, so after a signing the glow is on the player's
  own act, not Connie's. **SHOW ME** opens the tab for the step; **I KNOW THE WAY** puts the card away for good, and
  NEXT UP returns. The off switch is on the card, not on the NEW GAME page as §3.2 sketched.
- Saved as `FirstWeekSaveData` (Off, Done, TermsOpened) on the player save. An older save gets a fresh card that ticks
  itself off from the run, so a run past its first record never sees it.
- **Measured:** `SimTools/FirstWeekUiChecks` plays a fresh Sawyer founding in Detroit by pressing only the glowing
  button: **8 presses and 1 in-game day to a first signing** (the scripted baseline in §1 was ~12 presses and 2 days,
  found by reading button text). No SHOW ME was needed. Screenshots: `SimLogs/first-week-*.png`.
- **Not exercised:** the studio step past TAKE INTO STUDIO (choosing songs and booking hours; the card is hidden in the
  studio window, as NEXT UP is); a run where the ask is declined; a non-Sawyer run in the UI. A human cold playtest is
  still the number that matters (§4 "Measure it").
- Also: UI checks that end a day through the real desk now set `PlayerDesk.AutosaveOffForChecks`. An earlier run of
  this check wrote `Autosave 2` into the shared saves folder.

Every phase is player-side only: no sim state, no RNG draws, no AI-read fields, so AI CSVs stay byte-identical
([[probe-run-byte-comparison-proves-inertness]]).

**Measure it.** Add a first-session trace (player-only telemetry): in-game time and presses to the first signing,
tabs opened, presses that changed nothing (dead clicks), and NEXT UP lines shown. Today's scripted baseline is **2
days, ~12 presses**. A human new player is the number that matters, so record one cold playtest before T1 and one
after T3.

## 5. Author decisions (2026-11-25)

1. **The holder is named on day one.** Each holder bends the playthrough toward something specific (Connie's record,
   Kubiak's money, Floria's survival), and the player needs to know that at once. Hiding the name alone isn't a
   mystery that pays off. **The early mystery is *why*:** why does this man want this particular thing from you?
   The letter, the first-morning scene (§3.1) and the desk all name him. This replaces the inner-life directive's Act
   1 "Who" (§3.7) with "Why"; that directive needs the matching edit when T1 is built.
2. **Each voice teaches its own task:** the Ear teaches hearing a set, the Street the rooms and who's moving, the Suit
   the contract and the money, and the Fixer the bookers, numbers and favours. No separate mentor.
3. **The first-week card ends with an act on the roster and a first record set up** (material chosen and studio time
   booked), not with the record out.
4. **The office as primary comes last.** Open question, with a first answer: *could the game manage without the
   tabs?* Mostly, yes. Seven of the nine tabs have a natural object: A&R (the door, then the pad), ROSTER (the pad),
   CATALOG (the stacks), FINANCES (the checkbook), LEDGER (the typewriter), ROLODEX (the phone) and BAND ROOM (a back
   door). Two have no object: **DISTRIBUTION**, which is really "the road" (a map on the wall, or the door's "another
   town" choice), and **OFFICE**, which is a mixed bag of staff, services, the paper and project promo (split it: the
   paper goes in the drawer, staff and services go on a noticeboard). What the tabs give that objects don't is
   **switching mid-task**: from the contract sheet to the books and back. So the likely end state is objects as the
   way in, and a slim strip of tabs kept *inside* the open folder as the way across. That's for T2, last.
5. **Fatigue is its own mechanic,** designed separately. §3.4's hooks wait on it.

## 6. Method note (for re-running the playtest)

The playthrough used a throwaway driver scene (since deleted) that instantiates `MainMenu.tscn`, plays NEW GAME, and
then follows a command file (`press <text>`, `tab <name>`, `wait <s>`), screenshotting and dumping visible text and
tooltips after every command. The save it left, `onboard-playtest`, sits in the shared saves folder, on Jan 1 with the
label just founded. Computer use couldn't drive the Godot window: it only resolves apps that have a Start-menu entry.
