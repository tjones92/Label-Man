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
| **T5 appointment book** (steps 1–2 built) | §4c: the book, signing appointments, times to pick, missing one; then rapport, calls that ring once, the secretary | `PlayerDesk.AppointmentBook.cs`, The Talk, THE BOOK by the clock | `AppointmentBookChecks`, `FirstWeekUiChecks --manager` |
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

**T3 gaps closed (2026-11-26).** `FirstWeekUiChecks` now takes `--holder=NAME` (answers that make that holder the only
fit) and plays on past TAKE INTO STUDIO to a booked date, then back to the desk, where NEXT UP takes over ("Choose takes
and print the masters"). Run on seeds 1001–1008 across all five holders. What the wider runs found and fixed:
- **The studio had no help.** The card is hidden in the studio window, nothing glowed, and BOOK THE ROOM sat five
  sections and six room cards below the songs. Now the booking sheet is a teaching place (`FirstWeekPlace.Studio`): the
  Ear on which two songs, the Suit on leaving the rest of the sheet alone. The glow ticks two songs, then BOOK THE ROOM,
  and scrolls each into view. 3 presses from TAKE INTO STUDIO to a booked date.
- **An evening signing couldn't book.** A date is 3h at least, and after 6 PM in town it won't fit before the 9 PM hard
  stop, but BOOK THE ROOM stayed live and only failed on the press. The sheet now greys it with the reason (book in the
  morning, or an evening rate run past midnight). The card's Fixer says end the day, and the glow goes to the way back,
  then END THE DAY, then the studio in the morning.
- **The same at the table.** At 8 PM a 2h TABLE OFFER (or TABLE AGAIN, RENEW) stayed live and failed on every press.
  The terms sheet now greys it after the hard-stop cutoff, and the card's Fixer sends the player home (NOT NOW, END THE
  DAY), then back to the table in the morning.
- **"Come and hear us again" was a dead end.** The Street said go back, but the glow pointed at END THE DAY, and NEXT
  SETS (top three, new acts before heard ones) never listed the act's next set. The finder now puts an act that asked you
  back first, with a line saying so. The glow walks to its set, its LISTEN, LET THEM GO for anyone else on the bill, TALK
  TO THEM, then the ask. While they're still waiting to be heard again, The Talk's ask line doesn't glow (it only repeats
  the answer); END THE CONVERSATION does.
- A plain no (seeds 1005, 1007) already worked: the glow goes back to the sets and the bot picks somebody else.
- The take sheet after booking stays outside the card (decision 3); it has its own "Next: cut a first pass" line.

**Push-back at booking (2026-11-26).** THE ACT PUSHES BACK had no first-week target: nothing glowed, and SHOW ME only
reopened the studio. Now:
- The Ear teaches it inside the dialog (`PlayerDesk.FirstWeekPushBack`): a no to the song, not to the record; set it aside
  and tick one they'd fight for. When they've turned down everything on the sheet, the Ear says insist or go back to the
  roster for a song they want, and under creative control only the second.
- The glow goes to SET THIS ASIDE while the sheet has a song they haven't turned down, else to INSIST (else, under
  creative control, SET THIS ASIDE and then ‹ BACK to the roster).
- **SET THIS ASIDE unticks only the refused songs.** It used to clear the whole sheet, including the song they were happy
  with. USE THEIR OWN MATERIAL skips their own songs they've already turned down.
- The panel remembers refused songs for the session (`studioRefused`, player-side only) and the song glow never walks back
  to one. The act's answer is still asked fresh at every booking.
- `FirstWeekUiChecks --push-back` ticks two songs and opens the real dialog on the first, then follows the glow. It checks
  the Ear's line, the glow on SET THIS ASIDE, that only the refused song is unticked (the other stays ticked), that the glow
  never walks back to it, and that the date is cut without it. Passes on seed 1002 (49 checks); screenshot
  `SimLogs/first-week-07-push-back.png`. The plain run still passes (2 presses on the sheet, a two-song date).
- **The act's answer is forced in the check, because no real song is refused.** On seeds 1001–1005 no song on the new
  act's sheet, and none of the 6,574 catalogue standards and coverable hits, trips `MaterialRefusals`. Even with the act
  made a Superstar with full ambition, stretch peaks at 0.25 of capability, and the gate needs 0.9
  (`refusalStretchCapability`). Identity alone would trip at that standing (min 0.33 against a 0.65 bar). So in real play
  the push-back almost never fires for a player's act. `PolarPlayerPerceptionChecks` only trips it with a synthetic song
  and a synthetic act. *Author question:* is a refusal meant to be this rare, or is the stretch half of the rule too
  strict for real songs?

**A counter at the table (2026-11-26).** Seed 1005 on a Kubiak run: the manager countered (SWEETEN IT / TRADE /
PROMISE… / HOLD FIRM / WALK AWAY) and nothing glowed; SHOW ME didn't help. Now:
- On the counter the Suit says it's a price, not a no (`PlayerDesk.FirstWeekCounter`): give them part of what they named
  and put the paper back. A promise is a bill that comes later, and holding firm on a first deal loses the act. For
  publishing or creative control, which can't be split, he says let them have it. For a capability objection, he says it
  isn't the money, but better terms can still carry it. SWEETEN IT glows and is scrolled into view.
- Back on the sheet he names the term they're stuck on, their number and yours ("they asked $60, the paper says $35"),
  and says to move that one about halfway. **That blank glows** until it moves (or the box, for publishing or control),
  then TABLE AGAIN. The glow can now sit on a typed blank: `GlowOn` takes any control, and a blank pulses as a whole.
- `FirstWeekUiChecks` checks the Suit's lines, that SWEETEN IT glows and is on screen, the glowing blank (on screen) or
  box, and TABLE AGAIN after it. When a blank glows, the bot types the halfway number. `--lowball` writes a third of the
  advance on the first offer to draw a money counter. Seed 1005 Kubiak passes (publishing hard line, via the box), and
  seed 1002 `--lowball` passes (advance: $12 offered, $35 asked, $24 typed, signed on TABLE AGAIN). Screenshots
  `SimLogs/first-week-05-counter.png`, `first-week-07-sweeten.png`. Before the last copy and scroll fixes, all 20 runs of
  seeds 1001/1003/1006/1008 × five holders passed; none of them drew a counter.
- Scrolling to a glow that just appeared waits for the watch's next tick (`firstWeekScrollTo`). Scrolling in the same
  pass as a page rebuild did nothing, because the page had no layout yet.

Every phase is player-side only: no sim state, no RNG draws, no AI-read fields, so AI CSVs stay byte-identical
([[probe-run-byte-comparison-proves-inertness]]).

**Measure it.** Add a first-session trace (player-only telemetry): in-game time and presses to the first signing,
tabs opened, presses that changed nothing (dead clicks), and NEXT UP lines shown. Today's scripted baseline is **2
days, ~12 presses**. A human new player is the number that matters, so record one cold playtest before T1 and one
after T3.

**First-session trace built (2026-11-26)** (`UI/FirstSessionTrace.cs`). It starts the first time the desk shows a label
founded in this session (never a loaded one) and stops at the first record set up, game over, or 30 in-game days. It
records every button press in the tree, with the in-game time, the tab, and whether it was the glowing button. Each press
is marked **ok** (the clock moved, the desk changed or the page redrew), **refused** (a red Post-it), or **no visible
change**, and clicks on greyed buttons are kept with their reason. It also keeps the tabs opened, each NEXT UP line shown,
and the first-week lines as they tick. It writes `first-session-<time>-summary.txt` and `-presses.tsv` to
`user://first-session/` (`%APPDATA%\Godot\app_userdata\Label Man\first-session`), rewritten at each milestone, so a quit
or a crash still leaves the numbers. Checks write to `SimLogs/first-session/`. Nothing is saved with the game, read by the
sim or sent anywhere.
- `FirstWeekUiChecks` now checks the trace (it finishes at the first record, marks the signing and the card put away,
  counts presses, and reads GO & WAIT, LISTEN, END THE DAY and TABLE OFFER as ok). Seed 1001: 66 checks pass.
- **The bot's baseline has moved.** Seed 1001 now takes **33 presses (8 on the glow) and 9 in-game days** to a booked
  date. The 8-press, 1-day figure above predates the bot choosing its act by its Ear (b5d9c22): it now hears several sets
  before it asks. That's the bot's choice, not the card's. A run without a seed reached its first set by one GO & WAIT
  that skipped **seven days** (Jan 1 → Jan 8). Worth checking whether NEXT SETS should offer a set a week out to a new
  player at all.
- "No visible change" means the trace saw no clock move, desk change or node added or removed within two frames. A press
  that only reopens the section that's already open (the bot's FIND A SET / NEXT SETS) counts, which is right for a
  dead click. A purely cosmetic press would count too, so read the list rather than trusting the number blind.
- Next: the cold playtest itself. Play a NEW GAME through to a first record, then send the two files.

## 4a. First cold playtest (2026-11-26)

The author played a cold NEW GAME on the T3 build. What came up, and what was done:

| Finding | Done |
|---|---|
| FIND A SET glowed over the finder it had already opened. Pressing it did nothing. | Choosing who to hear, **every listed set glows** and the player picks (dd92a7e). |
| The yellow glow vanished on TALK TO THEM's oxblood | The glow is now a **pulsing blue pencil ring** around the control as well as the tint (dd92a7e) |
| The Street already knew Buzz Carroll's residency on a hungover first morning | The Street passes on room talk only **once a set has been heard**. Before that, a residency shows as the room's own billing (dd92a7e). |
| "I'll sit down with Douglas Banks. Not before Jan 11." Where? | The greyed line now says when and where. The real fix is the appointment book (§4c). |
| The trace recorded nothing | The session loaded a save, and the trace only followed a founding. It now follows a loaded run still on its first week, marked resumed. |
| A 7-day skip hung for ~20 s | Not looked at yet (day-end sim cost) |

### 4b. The voices as pop-overs (proposal, T6)

The teaching lines are inline text on the page today. The player reads past them. Instead, the voice line of the moment
becomes a **slip of paper that pops over the page, with a pencil arrow at the glowing control**. It's the voices
desperately trying to steer a body that doesn't know what it's doing.

- **One slip at a time,** from the voice whose job it is (decision 2), anchored beside the glow, with a hand-drawn arrow
  to it. When several things glow (the sets), the arrow points at the group.
- **It never blocks.** It doesn't take the mouse, apart from a small × that puts it away until the glow moves. It comes
  back when the next thing to press changes.
- **The tone is panicked, not instructive.** The voices know the Label Man is out of his depth:
  - Street, day one: *"You have no clue how this all works, do you? We need to sign a fuckin' act! Pick a room."*
  - Ear, at a set: *"Shut up and listen. That's the whole job tonight."*
  - Suit, on the first contract while worn: *"Look at you, hungover. You can't even put a proposal together. Take the
    standard paper and don't touch anything."* (This is where the fatigue mechanic gets introduced, once F1 exists.)
  - Fixer, on a wait: *"Go home. They'll call. Nobody calls back a man who's standing in the doorway."*
- Inline teaching stays only where nothing glows (a page with no next press), so the line is never lost.
- Same rules as now: every line names real state, and the off switch is I KNOW THE WAY.

**T6 built (2026-11-26)** (`UI/PlayerDeskPanel.FirstWeekSlip.cs`; the lines are `PlayerDesk.FirstWeekNudge`):
- A paper slip on its own canvas layer (61, above the modals) beside whatever glows, with a pencil-blue arrow to each
  glowing control in view (up to four). If the glow is scrolled out of sight (the terms sheet's TABLE OFFER sits below
  the fold), the slip sits just inside that edge of the page and the arrow points straight off it.
- One line per thing to press, from the voice whose job it is, in the panicked register: about thirty lines across the
  first week, including the Street's day-one line above and the Suit's "Look at you, hungover…" at TABLE OFFER, which
  shows only on Jan 1–2 (the real morning after) until fatigue takes it over. The lines claim no read, so they speak at
  any instinct.
- The × puts it away until the glow moves on. It never takes the mouse otherwise, and the first-session trace counts the
  × as ok, not a dead click. It's built once and moved on the watch's tick.
- **Changed from the sketch above:** the longer teaching lines stay on the page as reference. The slip is the nudge,
  the page is the explanation. Moving all the teaching into slips can follow if the page lines still go unread.
- `FirstWeekUiChecks` (seed 1001, 76 checks): the Street's slip is up beside the sets on day one with an arrow per set
  in view, the × puts it away, it comes back with the Ear's line at LISTEN, and every glow with a line has its slip up,
  including TABLE OFFER below the fold. Screenshots: `SimLogs/first-week-02-finder.png`, `-10-terms.png`.
- Not seen in play: the hungover line (seed 1001 reaches the table on Jan 10), the studio's song and push-back slips, and
  a counter's SWEETEN IT and blank. A known rough edge: beside the set rows the slip covers the end of the card's
  billing line.

### 4c. The appointment book (proposal, T5)

A sit-down with a manager is the first time the player has **somewhere to be at a certain time**. That's the first moment
the clock means something, and today it's a greyed line in a conversation. It should be an appointment.

*Revised 2026-11-26 with the author.* The book, the secretary, missed calls and manager rapport are one design. The
trade-off it exists for: **do you hit the road to sell while calls you're waiting on might come in?**

#### The book is his memory, and only that

- **An object on the desk**, a desk diary (so it fits T2's office-as-menu). Each entry has a day, a time, a place, who,
  how long and what it's for.
- **The other side proposes a time, and you pick one.** "Banks can see you Monday at 3 or Tuesday at 10, at his office
  on Woodward." That's the first trade-off between hours.
- **It also lists what he's waiting on:** the jocks he mailed a record to, the shop that's nearly sold out, the act that's
  thinking it over. These are things he set in motion himself, so the risk of going out is known even though the call
  isn't.
- **No reminders and no clash warnings.** The book doesn't speak. He has to look at it. The desk never says "It's 2:30,
  Banks at 3," and a road trip that runs over an appointment isn't flagged. The first-week card teaches the book once,
  on the first appointment (the Street: *"Write it down. Nobody in this office is going to remind you."*). That's a
  lesson, not a reminder.
- **Being there.** At the appointment's hour he has to be in town and not out on something else (a set, an errand, the
  plant, the road). If he isn't, he's missed it.
- **What goes in it,** in order of building: the manager sit-down and Sunday dinner (signing), the holdout's next set,
  studio dates and demo bookings (already timed), Rolodex call-backs ("call Ed back at 3", a skip stop today), station
  visits, the December sitting.
- **Fatigue ties in.** A SPENT morning sleeps in, so a 10 AM appointment after a late night is a real risk.

#### Missed calls: the phone rings once

**Today** an implied answering service takes everything. `PlayerDesk.DeskMessages()` turns every shop reorder, station
add, request, pre-order and due call-back into a pink "while you were out" slip (`InboundCall`, open about a week), and
mailed-record call-backs, master and song offers and signed-act calls stop a skip "at the office"
(`FlagSkipStop(..., officeOnly: true)`). Nothing is ever missed, so there's nothing for a secretary to save.

**Proposed:** a call arrives at an hour of the day, keyed on the caller and the day so the same day replays the same.
If he's in the office at that hour, it's answered. If not, it's gone, and **most callers leave nothing.** The player
should fear that the important DJ call came while he was out.

| Who | Without a secretary, if he's out |
|---|---|
| A jock calling back about a mailed record | Rings once. The spin is gone, and he never knows. |
| A station adding the record | Gone. He may see it later on the survey, too late to work it. |
| A shop sold out or getting requests | Tries twice (morning, afternoon), then buys from whoever answered. The reorder is lost. |
| A booker with a cancellation slot, or a rival's tip | Rings once and goes to the next name on the list |
| **Rare visitors leave a note:** the holder, the lawyer, the bank, an act whose yes is waiting on you | A note under the door, or the man himself waiting on the step |
| Letters (master and song offers, the trade paper, contracts) | Always arrive. The mail doesn't need anyone in. |

- **Some misses surface later, most never do.** Now and then the caller mentions it the next time you meet ("I tried you
  Tuesday. Gave it to Chess."), so the player learns that it happens without being told every time.
- **The selling months are where it bites.** That's when he's on the road (the empty-day metric's gap, inner-life §6)
  and when reorders and jocks call. So the secretary pays off exactly when a successful player starts travelling.

#### The secretary (office expansion, about 1961)

Hiring her (a weekly wage, *provisional*, period clerical pay) turns each gap above into a strength:

- **Presence.** She answers every call and takes the message, so the pink slips come back, and now they're earned. A
  visitor is kept waiting or booked in.
- **Scheduling.** She fixes appointment times around his trips ("You're in Toledo Tuesday. I told Banks Thursday at
  10.") and catches clashes.
- **Reminders.** The timed nudge the book lacks: "It's 2:30. Banks at 3, on Woodward."
- **Saving a miss.** Once per appointment, she calls ahead and moves it before he misses it, so the manager's rapport
  isn't hit.
- **Inertness.** All player-side. Calls that are answered or missed change the player's own sales and airplay, as any
  player choice does. The AI economy doesn't read whether he was in.
- **No answering service** (author, 2026-11-26). Nothing sits between an empty office and the secretary. A cheap service
  would soften the fear of the missed call, which is the point.

#### Manager rapport

Managers are the people who run the act's career and its contracts, so the label carries **rapport with each manager**.

- **What exists:** a manager is only a description worked out from the act (`ManagerIdentity.Read`: family, club booker,
  deejay, songwriter, agency, jukebox operator, personal manager, hustler), with nothing stored about him. His archetype
  (`ManagerProfile`: Hustler, Shark, Svengali, Visionary) is the deal posture the AI reads. **Rapport is new, player-only
  state** on top of that.
- **Keyed on the person,** not the act. A club booker who manages two acts carries the same rapport for both, and a
  manager who liked working with you calls first with his next act.
- **What moves it:** kept appointments, promises kept or broken, advances paid on time, a record that does well for his
  act, how hard you pushed at the table, and missed appointments.
- **What it moves,** always in the player's own talks: patience at the table, how far he'll come toward you, how often
  he objects, the renewal ask, whether he listens to a rival's offer, and whether he brings you his next act. The AI's
  term sheets don't change.
- **Who can read it:** the Fixer (who owes whom). With Fixer 3+ the act card names where you stand ("Banks: warm"). Below
  that it's a guess, with the usual fog.

**A missed appointment, by who he is:**

| Manager | First miss | Again |
|---|---|---|
| Hustler | Shrugs, reschedules. Rapport dips a little. | Cools |
| Shark | Uses it: "You owe me." His price goes up at the table. | The price climbs again, and he shops the act |
| Svengali | Takes it personally. A big rapport loss. | The act's yes lapses |
| Visionary | Cools. Less patience at the table. | He's out of patience. Terms harden. |
| Family (Sunday dinner) | Close to fatal. A parent has to sign, and you didn't come. | — |

A club booker or a deejay as manager also loses face in his own room or station, and that carries over to booking and
airplay through the existing booker and jock regard (*later*).

#### Build order (T5)

1. The book, with signing appointments (sit-down, Sunday dinner), times to pick from, and missing one.
2. Manager rapport: store, read and move it, and apply the miss costs above.
3. Calls that ring at an hour, and the answering service taken out. This is the biggest change to the selling loop, so
   it gets an empty-day and sales comparison on the player bots (`OpeningCashLockRunner`) before and after.
4. The secretary as a hire in the OFFICE.

Inertness for all four: player-side and saved, with headless AI audits byte-identical. Player-bot runs will move,
because what a player sells changes.

**T5 step 1 built (2026-11-27): the book, with signing appointments** (`PlayerDesk.AppointmentBook.cs`,
`UI/PlayerDeskPanel.AppointmentBook.cs`):
- **Two times to pick from.** When a yes comes with a visit, their side proposes two (`ProposedTimes`, keyed on the act and
  the day, no RNG draw): a family offers Sunday dinner at 1 PM on two Sundays; a club booker two of his room's open nights,
  an hour before the doors; anyone else two weekdays at office hours (10, 11, 2, 3 or 4). The yes names both. The Talk
  offers them as lines ("Wednesday, Jan 6, at 4 PM suits me."), family first. Picking is free, works from anywhere, and
  writes it in the book. Until a time is picked the card's verb is TALK TO THEM and the visit can't be taken.
- **Being there.** On the day, the visit (from the act's card, The Talk or the book's GO THERE) goes there and waits for the
  hour, then takes its hours. Before the day it refuses and names the time. It has to be in the act's town.
- **Missing one.** Any clock move past a booked start without the visit done is a miss (`SweepMissedAppointments`, on the
  hour hand and at dawn). A family doesn't set a place twice: the yes ends. A manager leaves a note with two new times, two
  days on or later; a second miss ends the yes. A yes ended this way counts against regard on the day it ended
  (`RegardSourceKind.StoodThemUp`, -2, "We said yes, and then you stood us up."). The archetype costs in the table above
  wait for rapport (step 2).
- **The book** is THE BOOK, by the desk clock: soonest first, by day. It lists the visits (booked, or still waiting on a
  time, with the two times as buttons), rehearsals, demos and meetings arranged with acts, paid demo jobs, the studio date,
  a holdout's set, and the lien sitting's window. It has no badge, no count and no reminder. Nothing stops a skip for it.
- **The first week teaches it once.** The Street's slip at the two times ("Pick one and write it down. Nobody in this
  office is going to remind you."); on the day the glow goes to THE BOOK, not to the visit, then GO THERE; before the day,
  to END THE DAY. The Suit's and the Street's teaching lines for a pending visit say the same.
- **Older saves:** a step without offered times keeps "on or after its day", with no hour to miss.
- **Not built yet:** the book as an object on the desk (the header button stands in until T2 adds desk hotspots); misses
  aren't flagged on a road trip (by design). The archetype miss costs and rapport are step 2, below.
- Checks: `AppointmentBookChecks` (34: two times, deterministic, picked in The Talk, refused before the day, missed by the
  desk's own wait and by the day turning, new times, kept on the second booking, second miss ends the yes and drops regard,
  a missed Sunday dinner ends it). `DispositionChecks` (97, now books its family and manager visits),
  `TheTalkChecks` (126), `BluffCheckChecks` (28), `SignedClockChecks` (39) pass. `FirstWeekUiChecks --manager` (new: a
  shark manages the act the bot picks) walks the whole thing by the glow on seed 1001: both times picked, the book opened
  on each day, both visits kept, signed in 15 glow presses and 9 days (91 checks). Screenshots
  `SimLogs/first-week-04-book-times.png`, `-11-book-open.png`.
- Rough edges seen: GO THERE in the morning spends the hours up to the meeting (a 4 PM sit-down burns the day), which is
  the trade-off, but the first week's glow sent a new player there at 9 AM (fixed below). The slip beside GO THERE covers
  the entry's text. A woman managing an act could read "him" (Denise Patterson; fixed below).

**After step 1 (2026-11-27): the morning before a sit-down, and women managers.**
- **GO THERE at 9 AM.** The first week's glow sent a new player to GO THERE first thing, and a 4 PM sit-down ate the day.
  Now, while a visit later today is more than an hour off (`FirstWeekVisitLater`, `VisitLeaveMinutes` = 60), the hours
  are his. Every listed set that's over before it's time to leave glows (each set button carries `set_ends`), with the
  right WAIT (3h, or 1h when less than three hours are left). The Suit's slip: *"Banks isn't till 4. The day's yours till
  3: hear a set that's done by then. Don't sit in a waiting room all day."* Only in the last hour does the glow go to THE
  BOOK, then GO THERE. GO THERE itself now shows what going early costs: **GO THERE · WAIT 7h**. The book still never
  reminds; the glow is first-week teaching, as before.
- **Women managers.** A generated manager's sex was never stored with the name, so Denise Patterson was "him".
  `ManagerIdentity` now reads it off the first name (`NameGenerator.IsFemaleFirstName`: tagged female in the lexicon and
  never male; a lookup, no draw, so older saves read right too). Club bookers' names get the same read. The places and
  lines that said "his" (his office, his publisher's, the diner where he does his business, the Suit's and Street's
  teaching lines, The Talk's friction) follow it. A step saved before this keeps the sex it was saved with.

**T5 step 2 built (2026-11-27): manager rapport** (`PlayerDesk.ManagerRapport.cs`):
- **A ledger, keyed on the person** (`ManagerKey`: what he is and his name, so a club booker who manages two acts is one
  man). Stored, player-only, on the scouting knowledge (`Rapport`, null on older saves). Stored lines: a kept sit-down +1
  (twice at most; a family's Sunday dinner counts when the family is the manager), a missed one by archetype, holding firm
  at the table -1 (-2 at most), talks pushed until the act walked -1, and signing within 5% of his ask +1. **Read, not
  stored:** a promise to his act kept +1 or broken -2 (`ContractCommitments`), and a Top 40 record by his act on this
  label +1 (twice at most). Bad turns are remembered for half a year, good ones for two years. Levels: Sour (-3 or less),
  Cool, Neutral, Warm (1-2), Close (3+).
- **The miss costs, by archetype** (the §4c table): a hustler shrugs (-1, no hard feelings in the note) and gives up only
  at the third miss; a shark writes "You owe me an afternoon. It'll be on the bill." (-1), his price at the table rises
  +0.05 on the reservation for every miss remembered, and at the second he shops the act (the yes ends); a svengali takes
  it personally (-3) and at the second tells the act you aren't serious (the yes ends); a visionary cools (-1, then -2),
  has one round less patience for every miss, and gives up at the third. A family that's the manager is unchanged: one
  miss ends it.
- **What it moves, in the player's own talks only,** applied when the table opens (a signing, a renewal, a rival's paper
  turning an easy renewal into talks): patience (Sour -2, Cool -1, Warm +1, Close +2 rounds) and how far he'll come
  (reservation +0.06 / +0.03 / -0.02 / -0.04). He says why at the table ("You owe me for the afternoon I sat waiting. It's
  on the bill." / "You turned up when you said you would. That counts with me."). The AI's term sheets don't change.
- **Who can read it:** the Fixer at 3+, on the act's card ("The Fixer: Carl Toliver: even (kept a sit-down, missed a
  sit-down)"). Below that, only what the manager says at the table.
- **Not built yet:** the renewal ask, listening to a rival's offer, bringing you his next act, advances paid on time
  (there's no late advance to read), and a booker or deejay losing face in his room or station.
- Checks: `AppointmentBookChecks` (now 47): Denise Patterson reads as a woman and Sol Kramer as a man; the shark's miss
  note, -1 and Cool, his price up; kept, back to even but still billing the afternoon, and saying so at the table; the
  card's Fixer read; the second act's shark shops it after two misses and is a round shorter; the key is the person; the
  archetype rules. `FirstWeekUiChecks --manager` now checks that a visit later today never glows GO THERE or THE BOOK,
  that a glowing set is over before it's time to leave, and that the hours before the visit were left free.

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
5. **Fatigue is its own mechanic,** designed separately. §3.4's hooks wait on it. *Designed 2026-11-26:*
   `SimTools/LabelManFatigueDirective.md`. The unit is hours owed; a 9-to-6 day is free, overtime and the night cost;
   the state (FRESH / WORN / SPENT) is fixed at waking; Jan 1 starts WORN. T4 is its phase F3.

## 6. Method note (for re-running the playtest)

The playthrough used a throwaway driver scene (since deleted) that instantiates `MainMenu.tscn`, plays NEW GAME, and
then follows a command file (`press <text>`, `tab <name>`, `wait <s>`), screenshotting and dumping visible text and
tooltips after every command. The save it left, `onboard-playtest`, sits in the shared saves folder, on Jan 1 with the
label just founded. Computer use couldn't drive the Godot window: it only resolves apps that have a Start-menu entry.
