# The first signing: audit and proposal (2026-11-25)

Scope: everything from "go hear somebody" to the signed card. That means the A&R tab, the act notebook card, the
act conversation, the terms sheet and the T3 first-week card on top of them. The evidence is the code plus the ten
screenshots `SimTools/FirstWeekUiChecks` took of a real Sawyer run (`SimLogs/first-week-01..10-*.png`).

## 1. What a new player goes through today

| Step | What's on screen | Who speaks |
|---|---|---|
| Find a set | NEXT SETS list. Each row: act, room, time, admission, GO & WAIT | Nobody |
| Listen | Press LISTEN. A toast: *"Heard 1 act at Michigan Roadhouse. Evidence saved automatically. You can discuss terms now or choose a question for another date."* | The system |
| The read | The act card opens on a four-row table (Vocals / Playing / Material / Stage presence) and three grey hints | Nobody (it's a form) |
| Talk to them | A modal, **CONVERSATION**, with six equal buttons, each followed by a date, hours and fee | Nobody. The modal says *"this is conversation, not more listening"* |
| The answer | A sticky note, then a line in the Morning Paper | The newspaper |
| Terms | The carbon-copy contract sheet with ± steppers. Their ask comes as a toast: *"Eager to deal. $25, 1.3%…"* | The system |
| Signed | "The First Name on the Label" card: *Floyd folds his copy into his jacket. "So when do we cut?"* | **The act, for the first time** |

The only line an act speaks in the whole flow comes after the contract is signed.

## 2. Findings

### 2.1 The voices never reach the signing loop
`grep` finds **no** reference to `ExecutiveInstinctProfile`, `TheEar..TheFixer` or `ExecutiveVoice` in any
scouting, direct-approach, negotiation or signing file (Systems or UI). The inner-life directive already lists this:
§1 rates venue scouting *"Live, **no voices**"*, and §7 specifies the fix, which hasn't been built.

Compare the Rolodex (`PageCall`, `PlayerDesk.Rolodex.cs` `PassiveReads`, `RolodexVerbs.cs`):
- The contact opens the scene with a line of his own.
- Each of the four voices gives a **passive read** gated on the instinct score: under 3 you get nothing, 3 gets a hint,
  4 a clear read, 5 a deep read. Every read names a real fact.
- What the player can say is a **line of script** (`RenderOption`), coloured by its voice and marked with that voice's
  linocut. Its cost sits underneath in small italic.
- Some options exist only because a voice is strong enough (Street ≥3 unlocks rival pressure, Fixer unlocks the
  envelope). A bluff is always labelled.
- Pushback is answered with counters.

Scouting has the facts these reads would need (`ScoutingPerception`, the room impression, rival interest, the
contract outlook) but nobody says them.

### 2.2 The conversation is a menu of appointments
`OpenScoutingConversation` lists IF THEY'D HEAR AN OFFER, PAST LABEL DECISIONS, ASK FOR REHEARSAL, ASK FOR DEMO,
ASK FOR RETURN MEETING and ASK FOR THEIR NUMBER. They're six identical boxed buttons, and the notes beside them are
logistics ("Jan 2, 1960 · 3h · $54 when you go"). Every verb is real and worth keeping. None of them is said to
anybody, and the act never answers in the modal. The modal closes and a toast reports what happened.

### 2.3 The most important moment has no stage
Hearing an act is the decision point of the game's second loop, and today it's a toast followed by a table. The table
is honest (the fog draw, the room effect), but it reads like a lab report. Readers say what they noticed. A table
just lists it.

### 2.4 The act card does three jobs at once
After a listen the card shows about 11 controls: the header toggle, **TALK TO THEM · MAKE AN OFFER**, WATCH, PURSUE,
ARCHIVE, ROOM IMPRESSION, SOURCES & COVERAGE, FULL NOTES & HISTORY, the PLAN ANOTHER LISTEN fold (a dropdown plus a
set preview with its own GO & WAIT) and the MORE fold, which holds two more folds (booker questions, other acts).
Earlier playtests already cut it down (049: one primary verb, quiet links). What's left mixes three different jobs:
- **evidence**: what you heard
- **the relationship**: where the deal stands, what to say next
- **filing**: watch, pursue, archive, sources

The relationship verbs are split between the card (plan another listen, ask a booker) and the modal (rehearsal,
demo, number).

### 2.5 Three act lists and their duplicate verbs are still in the page
`ScoutingActsPage` still draws `ProspectCard` (Slate acts without a dossier: HEAR ANOTHER ANNOUNCED SET, ASK ABOUT
THIS ROOM'S BOOKING HISTORY, ASK IF THEY'D HEAR AN OFFER / DISCUSS TERMS) and the old `WatchNote` pad (BRING BACK TO
THE PAD, HOLD THEM — HANDSHAKE, REMOVE). It does this alongside the dossier notebook, and the pre-rooms venue
handbills are still kept behind `LocalScenes.Rooms`. With the default flags these rarely show. But they keep alias
names alive for the same action:

| Action | Names in the UI |
|---|---|
| Ask if they'd hear an offer | IF THEY'D HEAR AN OFFER · ASK IF THEY'D HEAR AN OFFER |
| Open terms | TERMS — MAKE AN OFFER · DISCUSS TERMS |
| Sign | TABLE OFFER · OFFER CONTRACT |

T3's glow-target table (`FirstWeekTargets`) has to list both names for each, which shows the problem. The **handshake
hold** is a real mechanic, and it lives only on the legacy pad.

### 2.6 The copy speaks like a system
"Evidence saved automatically." "This is conversation, not more listening." "No hold on the act." "Consumed sessions
are not refunded." "A substantial conversation creates no simultaneous listening evidence." These are rules written
down for the developer. The facts are fine; the speaker is wrong.

### 2.7 T3 teaches buttons, and its glow picks the act
- The voices on the card describe the UI: *"FIND A SET on the A&R tab… GO & WAIT takes you there; press LISTEN…"*
  It's button help wearing a linocut.
- The Hear step glows the first GO & WAIT on screen, which is the soonest set. The check proves what this does: it
  followed the glow for 8 presses and signed **Floyd Wilson**. Floyd was simply listed first, and his read was poor
  ("the songs were the weak link", "rarely gripping"). The tutorial walked a new player into signing a weak act
  without them ever making a decision. That's the railroading you described, and it teaches the wrong lesson about
  the game's central skill.

### 2.8 Almost nobody on a bill can be signed (measured 2026-11-25)
Playtest complaint: "it's always not looking, playing locally, or signed." `SimTools/SigningAvailabilityProbe`
(seed 1001, Detroit founding, 56 days, every bill in all 31 scene towns, 9,660 bill slots) labels each act the way its
card would:

| | Bill slots | Distinct acts |
|---|---|---|
| Under contract ("signed to …") | **55.6%** | 46.3% |
| Not looking for a deal (can be asked) | 43.9% | 52.3% |
| On the market now (can talk terms) | **0.4%** | 1.4% |
| Not taking offers right now | ~0% | ~0% |

- It's flat across the eight weeks (asking rises from 0% to 1.1% by week 8) and holds in every room kind. Residency
  acts are 67% signed; announced bookings 49%. Towns range from Portland (21% signed) to Miami (74%). Detroit is 64%.
- So an act heard is a flat no **56%** of the time. The rest go through the ask: a keyed 65% think it over, 35% "keep
  playing locally this season". Per bill slot that's ~29% think it over and ~15% "playing locally". One heard act in
  four gets as far as "thinking it over", and only after an 8-day wait.
- Acts that think it over come back reliably: the weekly activation pass takes them first, capped by open roster
  slots. Once back, other labels can hear they're free. How often a rival takes them before you do wasn't measured.
- The "on the market" path barely exists in the opening, so every first signing runs through the ask.
- Options (author's call): (a) UI only: make who's free visible before the hour is spent (sort or flag unsigned acts in
  FIND A SET; have the Street say which rooms have free acts). (b) Player-only odds: the 65% is an uncalibrated design
  input, and it could lean on something real (the room, a second listen). (c) World: book more unsigned acts onto
  announced bills. That changes the AI economy and needs its own screen.
- **Decided 2026-11-25:** both (b) and (c), widened into a redesign of who wants a deal and why. See
  `SigningRealismBreakdown.md`.

## 3. Proposal

**The principle: the signing loop becomes three scenes built on the Rolodex's beat machinery, and the notebook
becomes a quiet filing cabinet.** The scenes are **The Set** (Ear and Street), **The Talk** (Street and Fixer, with
the act's own spokesperson) and **The Table** (Suit and Fixer, with whoever negotiates for them). In each one, the
voices say what the existing reads already know.

The voice rules from the inner-life directive still apply: voices name real state, and they don't roll dice or
duplicate stats.

### 3.1 Declutter first (UI only, no new content)
1. **One act list.** Fold the `ProspectCard` and `WatchNote` paths into the dossier card, or delete them if they
   can't be reached with the default flags. Move **HOLD THEM — HANDSHAKE** into The Talk as a line ("Give me a week
   before you talk to anybody else"). Use one name per action.
2. **The act card gets three zones:**
   - *Header*: name, genre, and one status sentence spoken by someone. For example, the Street: "Thinking it over.
     They'll call by the 9th."
   - *What you heard*: the latest voice reads from the set, with the four-row table one fold down as the record.
   - *One primary verb* whose label follows the state:
     - GO HEAR THEM (no evidence; names their next date)
     - TALK TO THEM
     - WAITING ON THEIR ANSWER (Jan 9), shown disabled
     - SIT DOWN ON TERMS
3. **Filing is a stamp, not a toolbar.** Watch, pursue and archive become a small rubber-stamp control in the
   card's corner. ROOM IMPRESSION, SOURCES & COVERAGE and FULL NOTES merge into a single NOTES fold.
4. **Relationship verbs move into The Talk.** Plan another listen, ask a booker, rehearsal, demo, return meeting and
   number all become things you *say* there. The card stops carrying them.
5. **Copy pass.** Rewrite every toast and tooltip in this loop in a speaker's voice. Keep the facts and the costs.

### 3.2 The Set (presentation only)
When LISTEN resolves, a short scene replaces the toast. It can be a modal or a page takeover, matching PageCall.
1. **The room** (narration): from `ScoutingAudience.RoomImpression` and the room catalog. "Nine people, most of them
   at the bar."
2. **The set**: the songs heard, ♪ title (source).
3. **Voice reads**, using the same `Read(voice, score, hint, clear, deep)` helper and wrapping existing outputs only:
   - *Ear*: the vocals, playing and material bands from `ScoutingPerception`, including the room effect ("a roadhouse
     eats a voice").
   - *Street*: the crowd draw, repeat audience, who the regulars came for.
   - *Suit*: only when the ask or a manager is known. What they'll cost, and the roster slot.
   - *Fixer*: only when rival interest is real. "The man at the bar is from a Major."
4. **The act comes off stage.** Two verbs: **TALK TO THEM** (opens The Talk) or **LET THEM GO** (the notes are
   filed).

Because the reads wrap the fog draw, a weak Ear *misreads* honestly. That's the point of the instincts.

### 3.3 The Talk (presentation only, reusing `CallLine`, `CallOption`, `RenderTranscript`, `RenderOption`)
- **Opening**: their spokesperson, a real member (`members`, `meeting.SourceName`), speaks a line chosen by their
  deal state: not looking, looking, already courted, or said no this season.
- **Passive reads**: Street on what they want (local or national), and Fixer on rival heat if it's real.
- **What you say**: each line is mapped onto an existing verb, with the cost in italic underneath as on a call.

  | Line | Existing verb | Voice |
  |---|---|---|
  | "Would you hear an offer from us?" | `ApproachToSign` | none |
  | "Who've you dealt with before?" | `AskScoutingBusinessHistory` | Suit |
  | "Can I hear you somewhere quieter?" | rehearsal | Ear |
  | "Put two songs on tape for me." | demo | Ear |
  | "How do I reach you?" | `AskActForNumber` | Street |
  | "When are you on next?" | the next set | Street |
  | "Give me a week." | the handshake hold | Fixer |

- **Their answer comes back in the transcript.** The Morning Paper still carries the callback.

### 3.4 The Table (presentation only)
Keep the carbon sheet; it's the best-looking thing in the loop. Add a speaker across from it:
- Their manager, or the act, states the ask as a line instead of a toast.
- *Suit*: reads beside the fields, wrapping `ContractOutlookLines`. "$25 is fair for a roadhouse act nobody's heard
  of."
- *Fixer*: whether a rival offer they mention is real.
- `NegotiationScene` counters become lines from across the table.

### 3.5 T3, rebuilt on the scenes
- **The card shrinks to the checklist and SHOW ME.** The teaching moves into the scenes as *first-time lines*: the
  first time each beat plays, its voice explains it in character. That's the theatrical version. The voice is in
  the room when the thing happens, not in a banner above the tabs.
- **Voices talk about the task, never the button.** Button names appear only behind SHOW ME. For example, the Ear on
  Hear: "Nobody signs an act off a handbill. Pick a room tonight and I'll tell you what I hear."
- **No glow on an act.** On the Hear step, glow the FIND A SET view, never a row. Instead, the voice comments on two
  or three of today's sets from real state: the room's character and the genre against the town. Then the player
  picks.
- **After the set, the voice gives an honest verdict.** If the read is weak, it says so: "Songs were the weak link.
  Hear somebody else before you ask." Asking is still allowed. The player decides; the voice isn't a gate.

### 3.6 Later (outside this pass)
- **Courtship objections and counters** (§7 bullet 2). The act raises a grounded objection and the player answers.
  This changes approach outcomes, so it needs its own screen.
- **Instincts in the fog** (§7 bullet 3). A player-only perception change, with its own check.

### 3.7 Built: the declutter pass (2026-11-25)
- **The open act card** is now: WHAT YOU'VE HEARD, one primary verb, one quiet row, and a single MORE fold.
  - The primary verb is **TALK TO THEM**, or **SIT DOWN ON TERMS** once they'll hear an offer, which goes straight to
    the contract.
  - The quiet row has a **FILED: …** stamp (a small menu: Watch, Pursue, Archive) and **NOTES**.
  - MORE (next listen, who else is interested, ask a booker, other acts) has subheads instead of nested folds.
- **One NOTES sheet** replaces FULL NOTES & HISTORY, ROOM IMPRESSION and SOURCES & COVERAGE.
- **One name per action:** ASK IF THEY'D HEAR AN OFFER, SIT DOWN ON TERMS, TABLE OFFER. This covers the conversation,
  the old prospect card (still used for office demo submissions) and the Pushover contract form. T3's glow targets
  shrank to match.
- **The GO HEAR THEM state was dropped.** Every dossier already holds a heard set, so it would never show.
- **Copy pass:** the listen, wait, notes and back-to-the-table toasts and the meeting text are now spoken plainly
  ("You caught Floyd Wilson at Michigan Roadhouse. It's all in your notebook.").
- **Found and fixed:** opening an act's conversation changed the save (`NumberRefusals` went from null to `[]` in a
  read). The lookup no longer creates the list.
- **Found, not touched:**
  - The **handshake hold** is unreachable in new games. It needs a `WatchNote`, and nothing in the current UI creates
    one.
  - The **old pad** list renders only for older saves.
  - Reviving the hold belongs with The Talk (§3.3): it changes whether rivals can sign an act, so it needs its own
    check.
- **Checks:**
  - `ScoutingUiChecks` was already failing before this pass (venue handbills became inline room buttons). It's
    repointed and passes.
  - `SceneRoomUiRunner`'s button names were updated, but the runner wasn't run.

### 3.8 Built: the T3 voice lines and The Set (2026-11-25)
- **T3 card lines rewritten in character** (`PlayerDesk.FirstWeek.cs`). Each voice talks about the work in its own
  temperament (§5 of the inner-life directive: Ear the snob, Street the gossip, Suit the dry accountant, Fixer the
  operator) and never names a button; the glow and SHOW ME point at those. Every line keeps its facts (asking is free,
  the advance comes back out of the royalty, answers come within a week). A voice under 3 teaches in its own blunt
  register and claims nothing ("Don't ask me who's good. I can tell you who was loud.").
- **The Set** (`PlayerDesk.TheSet.cs`, `PlayerDeskPanel.TheSet.cs`). After LISTEN (all three listen buttons) the A&R
  page becomes the room, the way the Band Room takes its page:
  - The room as narration (kind, how full), the songs heard with ♪ and *their own* / *a cover*, or a note that you came
    in partway.
  - Voice reads, gated like the Rolodex (under 3 silent, 3 hint, 5 deep):
    - *Ear*: a verdict from the fogged reads, then the best and worst of them, then one concrete detail (an original
      worth taping, all covers, a singer who is the act). A second Ear line, from 4, names the room's blind spot, but
      only when it falls on a *weak* read.
    - *Street*: who stayed, who came in for them, calls for songs by name. Only on a paying crowd you watched long
      enough to see it.
    - *Suit*: only with a figure: under paper with a label, or their stated ask against the room's usual, your cash,
      and your roster.
    - *Fixer*: only when word of a rival's interest or offer has actually reached you.
  - While the first-week card is on Hear/Ask, the Ear adds an honest teaching verdict at any instinct: "spoken for",
    "I can't tell you if they're any good", "X was the weak link, hear somebody else", "I can't judge X in a room like
    that", or "go and talk to them now".
  - The act comes off the stand, with who talks for them. **TALK TO THEM** (or **SIT DOWN ON TERMS**, same rule as the
    card) or **LET THEM GO**. Several acts on one bill come one after another.
  - The card and the scene share one standing/primary-verb helper (`ScoutingActStanding`, `PressScoutingPrimary`).
  - T3's Ask step also glows SIT DOWN ON TERMS, for an act that's already shopping.
- **Check:** `SimTools/TheSetChecks.tscn` (`--set-city=` picks the town). Builds every first-day scene at all voices
  1 / 3 / 5: silence under 3, one read per voice, the deep read built on the hint, the teaching line at any instinct
  and only while the card is up, the same scene built twice; then stages it on the A&R page and lets the act go.
  Passes in Nashville, Detroit and Chicago.
- **Not yet seen in a check:** the Suit's ask line and the Fixer. Nearly every act heard on day one is already
  signed, and rival word takes days to arrive.

### 3.9 Built: The Table (2026-11-26)
- **Somebody sits across the sheet** (`PlayerDesk.TheTable.cs`, `PlayerDeskPanel.TheTable.cs`). The manager when the
  act has one ("Sol Kramer, their personal manager, sits down across the table from you, for Wilson 'n' the Gems"),
  else the member who talks for them (the same `SpeakerFor` as The Talk). A renewal opens on the old paper running out.
  It's drawn above the carbon sheet on all three contract screens: the easy signing, the negotiation and the renewal.
- **The ask is a line, not a toast.** Each manager kind says it in its own manner: the hustler wants to shake on it
  tonight, the shark wants it up front and the paper short, the Svengali keeps the reins and a longer term, the
  visionary keeps the publishing. Every term is in the line: advance, points, years, sides owed, publishing, last word.
  The "Their ask: …" lines and the DemandSummary toast are gone; the toast now says who you sit down with. A renewal's
  remembered grievance (a cheap signing, a broken promise) follows as narration.
- **Royalties are written as the form types them** ("3.25%", not "3.2%").
- **Voices**, gated like the Rolodex:
  - *Suit*: the advance against what acts from that kind of room ask (3); your cash and the roster slot (4); at 5, the
    term they'll fight for, from the same weights the acceptance check uses, or the hard line on publishing, or that an
    eager hustler takes less than he asks. A renewal compares the new points with the old paper's.
  - *Fixer*: real rival paper or asking-around that has reached you (shared with The Set and The Talk). When the act
    says it believes a bigger label could want it and no offer stands behind that, the Fixer calls it a hope.
- **What they say back comes across the table.** Objections, counters and the hold-firm narration are split from the
  talk's log into spoken lines and narration in the same transcript. HE COUNTERS became WHAT DO YOU SAY. Objection lines
  no longer have the manager say his own name ("Sol Kramer wants more money"); hold-firm names who is across the table.
- **Check:** `SimTools/TheTableChecks.tscn`: 43 checks, Nashville, seed 1001. The speaker and ask, every manager kind ×
  publishing × control carrying every term in its manner, silence at 1, Suit 3/4/5 building on each other, the Fixer's
  hope line, a member speaking when there's no manager, building the scene twice the same with save and world hashes
  unchanged, the objection said by the manager, and the page drawing it without the old lines.
- **Regression (seed 1001):** pass: `--scouting-terms-check` 42, `--scouting-contract-outlook-check` 63,
  `--scouting-session-promise-check` 65, `--scouting-contract-continuity-check` 50, `--scouting-offer-planning-check` 27,
  `--scouting-appointment-check`, `--signing-moment-check` 20, `TheTalkChecks` 126, `TheSetChecks` 91,
  `FirstWeekUiChecks` 18, `ConnieChecks` 49. `--scouting-rival-courtship-check` and
  `--scouting-credible-offers-check` failed the same at 5cd69f3 without this change. Both were fixtures that step 8's
  reshuffled bills broke, not game faults, and both now pass (94 and 78):
  - *The "unknown" act had a past.* `PrepareAct` cleared the act's career but not `careerStateBeforeDrop` or
    `top40Hits`, and pricing counts the best career an act has had, so a major priced it at $926, not $68. It now
    clears both.
  - *The search for a winning offer stopped too low.* A label nobody has heard of has to pay well over a better-placed
    rival's money: $100 against $70. The search ceiling went from 1.2× to 2× the rival's advance.
  - *Against a renewal poacher, money alone can't win.* The advance term saturates at 1.5× the bigger paper, and the
    fixture's act is ambitious (0.95), so at the poacher's −0.30 capability edge no advance wins. With all three
    promises (session, trade ad, wholesale line), $100 does. The fixture now adds them when money alone can't win.
    Worth knowing as design: an ambitious act leaves a small label for a well-placed one at any price unless the
    label promises work.
  - *The booker is asked in the room.* The fixture asked from across town; it now goes to the room first.

  The SaveLoadRoundTripRunner checks
  run out of memory when the box is short of commit memory (they serialize the whole world to one string); rerun
  them alone.
- **Not reached in the check:** the hold-firm narration (holding firm on the shark's first objection, he walked, as an
  impatient act does), and the Fixer on real paper (no rival offer on day one).

### 3.10 Built: T3 on the scenes (2026-11-26)
- **The card is the checklist, SHOW ME and I KNOW THE WAY.** Its voice lines are gone.
- **Each line is taught where its work happens** (`FirstWeekLinesAt`), in the same words as before (`FirstWeekHow`,
  `FirstWeekAside`):
  - *The set finder*: hearing, asking, and waiting for an answer, with any aside (Connie on a Sawyer run).
  - *The Talk*: asking, and the visit a yes still needs (Sunday dinner, the manager, the holdout's set), as a read in
    the transcript.
  - *The Table*: the Suit's arithmetic and how to give at the table, as reads in the transcript. Opening the table ticks
    "put a deal on the table", so both lines are taught there.
  - *The roster*: the first record.
  - *The Set* keeps its own teaching verdict (§3.8).
- **No glow on an act.** On the hearing line FIND A SET (then NEXT SETS) glows, never a row. The one exception is the
  LISTEN of the set you walked to, which is marked `first_week_here`: the player has already chosen it.
- **Tonight's rooms, from real state** (`FirstWeekSetNotes`):
  - *Ear*: what each of up to three rooms lets you hear ("… is a roadhouse: you'll see whether they can hold a bar, and
    the voice fights the noise."), then "Pick one."
  - *Street*: whether each act's genre is among the region's top three by affinity, or is negative there.
  - Under 3 the Street says it can't tell what the town buys.
- **Check: `FirstWeekUiChecks`, Detroit: seeds 1001 (36 checks), 2002 (37) and 3003 (52).** Also passing after this
  step: `TheSetChecks` 91, `TheTalkChecks` 126, `TheTableChecks` 43, `ConnieChecks` 49, `LeadsChecks`,
  `ScoutingUiChecks` 72, `SignedClockChecks` 39, `DispositionChecks` 92, `--scouting-runway-check` 212,
  `--scouting-appointment-check`. The bot chooses instead of following the glow:
  - It hears sets in the finder's order and talks to the first act whose Ear verdict says "go and talk to them now",
    letting the rest go.
  - It checks that no set row glows until you've walked to it, then that the LISTEN there glows.
  - It checks that the card carries no teaching line on any step, and that each voice teaches in its place (Ear at the
    finder and on the roster, Street in The Talk, Suit at The Table).
  - It checks that the act signed is the one it chose. Detroit, seed 1001: Lamont Turner, after one set ("Nothing
    wrong with them…"), signed in 9 days with 8 presses on a glow.
- **Found by making the bot choose, and fixed.** Each was a dead end a new player would have hit:
  - *After a no, the card glowed END THE DAY forever* while its voice said "go hear somebody else". When every act asked
    has said no (`FirstWeekNeedsAnotherAct`), it now points at END THE CONVERSATION and the finder, and at The Talk's
    ask only for an act nobody has asked yet, never at the act that said no.
  - *The Set's Ear verdict stopped after the first ask*, so hearing somebody else came with no verdict. It now plays
    while the card is still finding an act (`FirstWeekLooking`: hearing, asking, or after a no), and so does the glow
    on the LISTEN of the set you walked to.
  - *NEXT SETS kept offering the act that had just turned you down.* A resident plays nightly, and in seed 3003 the
    bot heard Marianne Spencer six times. The list now puts acts you haven't heard first, acts you've heard next, and
    acts that told you no (with nothing changed since) last, one row per act. Those rows say "They told you no, and
    nothing's changed for them since."
- **What a new player meets in the opening, Detroit:**
  - Seed 1001: one ask, a yes, signed in 9 days.
  - Seed 2002: one no (a residency act), then a yes, signed in 3 days.
  - Seed 3003: three residency acts ("We've got Motor City Room steady, and it pays") said no, and the fourth ask
    signed on day 16.

  Sawyer's Street is 2, so nobody warns that a resident is comfortable. That's the instinct doing its job, but three
  noes in a row is a long first fortnight. The bills-are-mostly-signed finding (§2.8) shows up here as "the act the Ear
  likes holds a steady room".

### 3.11 Built: instincts in the fog (2026-11-26)
The courtship objections in §3.6 were built as SigningRealismBreakdown step 9 (§16). This is the other half: the fog.
- **Who heard what.** The set's error band used the label's archetype `scoutingAbility`, fixed at founding. Now the
  **Ear** sets it for voice, playing and material (and the song-by-song reads), and the **Street** sets it for stage
  presence: how the crowd takes the act. It applies to sets, rehearsals and demos (`EarNoise`, `StreetNoise` in
  `PlayerDesk.Instincts.cs`; `ScenePerformanceService.InterpretAt`). Hired listeners keep the old ability formula.
- **The instinct is the main part of the fog** (author, 2026-11-26: a first cut that only swapped the archetype number
  for the instinct moved a read 0.01–0.06, which is negligible). `InstinctNoise`: 1 → .45, 2 → .33, 3 → .22, 4 → .13,
  5 → .06, 6 → .03. That replaces the old 0.08 floor plus 0.20 × (1 − ability). Partial coverage and the room's blind
  spots still add their own error for everyone.
- **What it's worth** (`FogInstinctChecks`, seed 1001): every one of the 2,888 performances on the first day's bills
  in every scene town, read at full coverage in its own room by 24 blind spots. A read lands in the wrong word band
  (under .48 / .48–.72 / from .72) this often:

  | Instinct | 1 | 2 | 3 | 4 | 5 | 6 |
  |---|---|---|---|---|---|---|
  | Music, wrong band | 37% | 30% | 21% | 14% | 8% | 5% |
  | Music, mean error | .148 | .112 | .078 | .050 | .027 | .018 |
  | Stage presence, wrong band | 37% | 29% | 21% | 14% | 7% | 5% |

  The old formula ran from about 21% (Ear 1, noise .22) to 14% (Ear 5, .13), which a player couldn't feel. Ear 3 now
  reads about as Ear 1 did. The Pawn-Shop Owner's tin ear misreads one set in three; the Ex-Musician's, one in twelve.
- **The blind spot keeps its direction.** The keyed bias (per label and axis) and event error are unchanged, so a low
  Ear is wrong the same way again and again, and a December point shrinks that without moving it. Notes already
  taken are never rewritten.
- **Still on the archetype number:** the old slate's `ReadQuality` / `ReadConfidence` (`ScoutingPerception`), the
  polar ear view, and the negotiation fog band. Convictions (inner-life §7) don't exist yet, so they don't reach the fog.
- **Check: `SimTools/FogInstinctChecks.tscn`** (`--set-city=`). It covers:
  - the fog narrows at each point, and the population rates above fall at each point; Ear 1 is at least 25% wrong,
    Ear 5 at most 10%, and Street 1 at least 20 points worse than Street 5;
  - a hired listener reads exactly as before;
  - sets heard at Ear 2 / Street 5 store exactly that read, and raising instincts later leaves the notes alone;
  - per heard set and axis, each point is never further off and keeps the error's sign;
  - the Ear never moves stage presence and the Street never moves the music.

  Passes, seed 1001: Nashville 374 checks, Detroit 355.
- **Regression (seed 1001 unless noted):** `TheSetChecks` 91 (Nashville) and 96 (Detroit), `TheTalkChecks` 126,
  `TheTableChecks` 43, `FirstWeekUiChecks` 36 / 37 / 52 (seeds 1001 / 2002 / 3003; the bot still signs Lamont Turner,
  Timmy Bishop and Skip Calloway), `ScoutingUiChecks` 72, `DispositionChecks` 92, `ConnieChecks` 49.
- **AI byte-comparison for this pass (§4).** 104 weeks, seed 1001, `--enable-genre-market-v2
  --enable-artist-population-lifecycle --aggregate-only`. HEAD (§3.4, §3.5 and this) was compared against step 8's
  run (`sig8on-1001-104` vs `sigaudit-1001-104`, and again at the steeper fog scale, `sigfog-1001-104`): **all 100 CSVs byte-identical.** Steps 1–7 were compared in
  SigningRealismBreakdown §14, so the whole presentation pass is now confirmed inert for the AI.

### 3.12 Built: the Fixer's bluff, the first check at a named moment (2026-11-26)
**Why.** The author wants an instinct you've invested in to buy real swings, the way a Disco Elysium check does, not
just a slightly better read. The inner-life directive's principle 2 is amended to match: deterministic stays the
default, and a few named moments may resolve as a visible check. The first is the bluff step 9 left greyed because
it "would need a draw" (SigningRealismBreakdown §16, "Not done").

**The machinery** (`PlayerDesk.Checks.cs`, for every later check):
- **The roll:** 2d6 + instinct + modifiers taken from real state, against a named difficulty: Easy 8, Medium 10,
  Challenging 12, Formidable 13, Legendary 14, Heroic 15. Double 1 always fails and double 6 always passes.
- **Keyed dice:** `LocalSceneIdentityService.AssignmentUnit` on the check's state, never the shared RNG. The same
  situation rolls the same, so a reload doesn't reroll.
- **White checks:** a failed one shuts on that state (`BluffFailedKey`) and reopens when the state changes.
- **Odds before you commit:** the small print names the instinct, the modifiers, the difficulty and the odds, plus a
  few words in the Fixer's manner: "They'll buy it", "Better than even", "Coin toss, near enough", "Long shot",
  "Forget it".
- **Not in the early signing:** `ChecksOpen` waits for your first record. The first signing runs on reads alone, and
  `FirstWeekUiChecks` never meets a check.

**The bluff** (`PlayerDesk.Objections.cs`, The Talk).
- **When it's offered:** an objection's true answer is greyed because its fact isn't true. The same line is then
  offered beneath it, marked as a bluff in rust, with the Fixer's linocut.
- **Difficulty**, by how checkable the lie is:

  | Lie | Difficulty |
  |---|---|
  | "I've been to hear you twice" (Trust) | Easy |
  | Your songs; you'd record here; your own songs, to a comfortable act | Medium |
  | "You're the best player up there"; "ask our acts, we've paid them" | Challenging |
  | "I told your folks at their table"; "you'd have more fun on a record" | Formidable |

  Job, Engagement and Local have no bluff: the first is always true, and the other two need only a record out.
- **Modifier:** regard. They trust you +2, they like you +1, they don't know you −1, you let them down −2.
- **Pass:** the objection is answered as if it were true, and the step is marked `Bluffed`. Nothing comes of that
  yet; it's there for a later "found out" beat.
- **Fail:** they say what gave you away ("Name one act of yours that's seen a royalty."). The line shuts on that
  state, and regard takes a −1 `CaughtBluffing` for half a year, which also makes the next try harder. Waiting, or
  signing without them, stays open.
- **What reopens it:** another Fixer point, another night of theirs heard, or a change in regard. The point a failed
  bluff costs doesn't count, or failing would reopen it straight away.

**Odds of passing** (regard 0), so investing shows:

| Difficulty | Fixer 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Easy 8 | 58% | 72% | 83% | 92% | 97% | 97% |
| Medium 10 | 28% | 42% | 58% | 72% | 83% | 92% |
| Challenging 12 | 8% | 17% | 28% | 42% | 58% | 72% |
| Formidable 13 | 3% | 8% | 17% | 28% | 42% | 58% |

The Promo Man (Fixer 5) talks round a holdout who trusts the label on "labels cheat" 83% of the time (5 + 2 against 12).
The Ex-Musician (Fixer 2) manages 8% with a stranger (2 − 1). Being caught costs regard, so the bet has a downside.

**Check: `SimTools/BluffCheckChecks.tscn`** (`--set-city=`, seed 1001). The fixture is an agreed yes with a holdout
raising "labels cheat", on an unsigned act heard on day one. It covers:
- the dice: odds are exact against Disco Elysium's table, boxcars and snake eyes override the sum, and the keyed dice
  are fair (6,000 keys);
- no check, Talk option or answer before the first record;
- once open: Challenging on the Fixer, and The Talk shows it under the greyed truth with its odds;
- same state, same key; each Fixer level its own state, with rising odds;
- a failing Fixer shuts it and costs a regard point, it's shut on the same state, the save carries it, and a later
  Fixer point reopens it and lands.

Results: Nashville 28 checks (Carolyn Fields: fails at Fixer 1–4, passes at 5), Portland 28 (Frankie Myers: passes
only at 6). Detroit has no unsigned act heard on day one (§2.8), so the fixture can't run there.
- **Regression (seed 1001 unless noted), all unchanged:** `TheSetChecks` 91 / 96, `TheTalkChecks` 126, `TheTableChecks` 43,
  `FirstWeekUiChecks` 36 / 37 / 52 (seeds 1001 / 2002 / 3003), `ScoutingUiChecks` 72, `DispositionChecks` 92,
  `ConnieChecks` 49. **AI byte-comparison:** `sigbluff-1001-104` against `sig8on-1001-104`, all 100 CSVs byte-identical.

**Not done:**
- An introductory check to teach a new player. The directive allows one, but none is placed.
- Red checks. Contract buyouts get their own directive.
- A bluff that landed being found out later.
- Checks at the other named moments: Street turning a "no", Suit at the table, an Ear passive in the room.

## 4. Order of work

| # | Piece | Touches the sim? | Check |
|---|---|---|---|
| 1 | Declutter + copy pass (§3.1) | No | `ScoutingUiChecks`, `FirstWeekUiChecks` (target names shrink to one each) |
| 2 | The Set (§3.2) — **built** (§3.8) | No (reads existing outputs) | `TheSetChecks`: Ear 1 / 3 / 5 lines |
| 3 | The Talk (§3.3) — **built** (SigningRealismBreakdown.md §12) | No (existing verbs) | Every old modal verb is reachable as a line |
| 4 | The Table (§3.4) — **built** (§3.9) | No | `TheTableChecks`; `ScoutingTermsChecks` |
| 5 | T3 on the scenes (§3.5) — **built** (§3.10) | No | Re-run `FirstWeekUiChecks`; the bot must **choose** an act (e.g. by the best Ear read) instead of following a glow |
| 6 | Objections, fog (§3.6): objections **built** (SigningRealismBreakdown §16), fog **built** (§3.11) | Player-only outcomes | Own screen; `FogInstinctChecks` |
| 7 | Checks at named moments: the Fixer's bluff **built** (§3.12) | Player-only outcomes | `BluffCheckChecks` |

Steps 1 to 5 are presentation only, so AI CSVs should stay byte-identical. **Confirmed** (§3.11 and
SigningRealismBreakdown §14).

## 5. Decisions for the author

**Decided 2026-11-25:** all three as recommended. The Talk takes over the A&R page; reads stay gated but T3's
first-time teaching lines speak at any instinct; the four-row table sits one fold under the voice reads. Work starts
with §3.1 (declutter).

1. **Should The Talk be a modal, or take over the A&R page the way a call takes over the Rolodex?** Recommendation:
   take over the page. It's consistent, and the transcript gets room.
2. **How do gated voices work in week one?** Instinct ranges go down to 1: the Pawn-Shop Owner's Ear is 1 and the
   Promo Man's is 2. Under the Rolodex rule (below 3 means silence), those players get no Ear read on their first
   set. Recommendation: keep reads gated, since that's what the instincts mean, but exempt T3's *first-time
   teaching* lines. A low voice teaches the step in its own blunt register ("I can't tell you if he's any good. Ask
   somebody who can.") without inventing a read.
3. **Should the four-row table stay visible by default after a set,** or sit one fold under the voice reads?
   Recommendation: one fold under, for players who want the record.
