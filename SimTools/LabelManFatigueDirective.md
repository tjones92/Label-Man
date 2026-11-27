# The Label Man's Fatigue: design

Status: design accepted (2026-11-26; §7 decisions taken as recommended). Nothing here is built. It answers the onboarding plan's open hook
(`SimTools/OnboardingTutorialDirective.md` §2B, §3.4, decision 5) and the new-player note *"I don't feel like I have any
frame of reference for how valuable time is."* Read with `SimTools/LabelManInnerLifeDirective.md` §0 (invariants), §3.1
(the hangover), §4.1 (mulling takes dead hours) and §6 (the empty early game). Numbers marked *provisional* wait on
the F0 measurement.

This is the **Label Man's** fatigue. It isn't the musicians' road fatigue (`BandMemberSimulationDirective.md` §4.16)
or a band's tiredness in the studio (`StudioExperienceExpansionDirective.md` §6). Those are sim state. This is
player-only. In code it's `LabelManFatigue`, so the names don't collide.

## 0. The idea in one line

**The record business happens at night, and the night is paid for the next morning.** A working day from 9 to 6 is
free. Overtime and nights out wear him down a little. Past the 9 PM stop they wear him down a lot. Ending the day
early, or taking a quiet day, wears off. Being worn costs him a sharp read, a steady hand at the table and, when he's
spent, the morning itself.

What it's for:

1. **Time gets a felt price.** An hour after 6 PM costs more than an hour at noon, and the player can see the bill.
   That's the missing frame of reference.
2. **The quiet days get a job.** About half a busy player's days have no label work (inner-life §6, measured
   2026-11-25). Fatigue makes a quiet day a recovery day, so it's worth something. Mulling a conviction at the desk
   (§4.1) is the natural thing to do on a night in.
3. **It's the hangover's long tail.** The game opens on a blackout night and a signature he can't remember. The
   comedy writes itself. The Suit won't let him sign anything while he's spent, because the last time he signed
   something tired, it was the lien.

## 1. What exists

| Piece | Where | What it does now |
|---|---|---|
| The working day | `TimeManager`: `workDayStartHour` 9, `workDayEndHour` 18, `maxOvertimeHours` 3 | 9h regular, 3h overtime, hard stop at 9 PM |
| Overtime | `CanAffordHours(h, allowOvertime)`, `SpendMinutes(m, allowOvertime = true)`, `IsOvertime` | Free. Some verbs refuse it (the bus: the last bus leaves at 6). |
| Past the hard stop | `LateStopProbe`, `DayStopMinute` | Only a booked late-night studio date runs past 9 PM |
| Sleeping it off | `AdvanceToNextDayCore`: `wake = max(9:00, ended − 24h + SleepHoursAfterLateNight)` | A night past midnight starts the next day 8h after it ended. The only cost of a late night today. |
| Room hours | `LocalSceneRoomService.Conversations.cs:96` clamps appointments to `min(room.CloseHour, HardStopHour)` | The player never sees a room after 9 PM |
| Every day is a working day | `IsWorkDay` exists but the desk doesn't gate on it; Sunday is for families | No day off is built in |
| Checks | `PlayerDesk.Checks.cs`: 2d6 + instinct + real-state modifiers | Has a slot for a named modifier |
| Read fog | `ScoutingPerception`, steep on instincts (signing audit §3.11) | Has a player-only error channel |

So the clock already has the right three bands (day, overtime, the night). It just charges nothing for the second and
third.

## 2. The model

### 2.1 One number, in hours

Fatigue is **hours owed**: the sleep he's behind on. One unit, the same unit as every action button (onboarding §3.4
asked for the unit to be settled first; this settles it). It's an integer on the player save, 0 and up.

**Adding to it** (as the hours are spent, *provisional*):

| Hour spent | Adds |
|---|---|
| 9 AM – 6 PM | 0 |
| 6 PM – 9 PM (overtime) | 1 an hour |
| After 9 PM (the night: the late studio date now, late sets if §4 is built) | 2 an hour |
| An intercity leg of 4h or more, by bus | 1 a leg (the cab and the car don't) |

**Taking it off** (overnight, at END THE DAY, by when the day ended):

| Day ended | Takes off |
|---|---|
| By 6 PM ("knock off early") | 4 |
| By 9 PM | 2 |
| Past 9 PM | 0 (the night's own sleep is already the 8h wake rule) |
| And the day was quiet: 3h or less spent, ended by 6 PM | 2 more |

It never goes below 0, and it's capped at 12 so a long bender can't dig a hole the player can't climb out of.

**What that gives, in rhythms:**
- 9 to 6 every day: always 0. A player who never stays late never meets the mechanic, except on the first morning.
- Three hours of overtime every night: +1 a day net, so **three nights out, then one night in.** That's the rhythm to
  teach.
- One studio night to 1 AM, from fresh: 3 OT + 4h × 2 = 11 owed, and nothing comes off. The 8h rule wakes him at
  9 AM, and being spent sleeps him till noon. Two quiet days clear it (11 → 5 → 0).

### 2.2 How it reads: three states, fixed at waking

The number stays under the hood. What the player sees is a **state word, fixed for the day when he wakes:**

| State | Owed at waking | What it costs today |
|---|---|---|
| **FRESH** | 0–2 | nothing |
| **WORN** | 3–5 | his reads are a step foggier; checks take −1 ("Tired"); the morning line says so |
| **SPENT** | 6+ | reads two steps foggier; checks −2; he **sleeps in** one hour per point over 5 (up to noon); the voice he trusts least goes quiet; the Suit won't table a contract or sign a deal |

Fixing the state at waking matters. The player always knows what kind of day he's having, the state can't flip in the
middle of a negotiation, and tonight's cost shows up as tomorrow's state. The desk can preview it: "Stay for the 8 PM
set and you'll wake worn."

### 2.3 Where the effects land (all player-only)

Every effect goes through a channel that already exists (inner-life invariant 1):

- **The read fog.** A Worn or Spent day adds one or two steps to the player's perception error on reads made that day
  (sets heard, acetates re-heard, polar ear-view cards). The fog stays keyed and deterministic. Fatigue is one more
  term in the key, so hearing the same set fresh another night can read differently, which is honest.
- **Checks.** A named modifier, "Tired −1" or "Spent −2", shown before you roll like the other modifiers. Checks are
  off in the first signing, so this never hits the tutorial.
- **The table.** On a Spent day TABLE OFFER, TABLE AGAIN and the signing buttons are greyed with the Suit's reason,
  the same way the 8 PM hard-stop cutoff greys them now (onboarding T3 gaps). Talking, listening and asking are
  still allowed. Only the signature is blocked.
- **The morning.** A Spent day starts late. The day budget is the obvious cost, and it's the one a new player feels.
- **A voice.** The voice with the lowest instinct goes quiet for the day (the same "preoccupied" cost mulling uses,
  §4.1). The others notice, in character.

**Things it never does:** touch the sim, the AI, any act, money or the lien. It has no game over and no hospital, and
it never makes a choice for the player. It also never gates a verb a fresh 9-to-6 player has, apart from the Spent
signature block.

### 2.4 The first morning

1 January starts **WORN at 4 owed**. The intro already plays the hangover (blur, double vision), so this is the same
morning, carried onto the desk. The first-week card gets one line, from the Fixer, the first time the clock passes
6 PM on day one:

> "You're still running on last night. Go home at six and tomorrow you'll have a head on you. Stay out and you'll be
> paying for New Year's all week."

And the Ear, on the first read, while he's worn: *"Half of what you think you heard is the hangover. Hear them again
when you can see straight."*

That's the whole lesson: what the state is, what made it, what clears it. It costs the first signing nothing, because
WORN doesn't block anything in that path, and checks are off. The `FirstWeekUiChecks` bot (8 presses, 1 day) has to
stay the same; F1 re-runs it.

## 3. What the player sees

- **The hour strip** (onboarding §3.4): twelve blocks for the day, nine regular and three overtime, filled as hours
  are spent. When he's slept in, the lost morning blocks are struck through. Late-night blocks appear only when
  something runs past 9.
- **The state word** next to the clock: FRESH, WORN or SPENT. Its tooltip says why ("three nights out since Monday"),
  what it costs today, and what clears it ("end the day by 6 tonight and you'll wake fresh").
- **A tonight preview.** An action that would end past 6 PM, or past 9, says what tomorrow will be: "Waits till
  8 PM · you'll wake worn." Only when the state would change, so the buttons don't fill up with it.
- **The morning line,** at the top of the desk on a WORN or SPENT day, in character: "You slept till eleven. The
  Lantern's last set is still ringing in your ears."
- Later, the Label Man's face (inner-life §9) carries it in his eyes.

## 4. The night (optional, needs an author decision)

Today the player's world closes at 9 PM, and the rooms are clamped to it. Fatigue makes it possible to open the night
honestly. Past 9 PM costs 2 an hour, so it's a choice with a price, not a free extra session:

- **Late sets:** a room's sets after 9 PM can be heard (clamp to `room.CloseHour`, not `HardStopHour`, for the
  player's own visit). Rooms with late bills are where the R&B and jazz scenes really happened.
- **A drink with the booker or the act** after the set. It's a small regard gain on the existing booker and
  act-regard channels, and it costs 1 owed per drink. It's the business, and it's the problem.

This is a scope expansion with sim-adjacent edges (which bills exist after 9 PM), so it's its own phase (F4), and the
core doesn't depend on it.

## 5. Inertness and the bots

- **Player-only.** One saved integer and a saved "day ended at" minute on the player save. A save without them loads
  at 0, as FRESH. Nothing AI-read, so the headless AI audits stay byte-identical
  ([[probe-run-byte-comparison-proves-inertness]]).
- **But the player bots change.** A Spent day that sleeps in, or a signature it blocks, changes what a player bot
  does. Player actions draw from the shared RNG (record hops, window cards), so a bot run's AI CSVs will move. That's
  expected and not a leak. Give the mechanic a switch (`--disable-label-man-fatigue`) so `OpeningCashLockRunner` and
  the empty-day metric can be compared with and without it.

## 6. Phases

| Phase | Scope | Check |
|---|---|---|
| **F0 measure** | From the existing bot runs (`OpeningCashLockRunner`, `SimLogs/empty-1001-*` style), count overtime hours and nights past 9 PM per week for the busy and plain players. Tune the §2.1 numbers so a busy bot spends ~1 day in 4 WORN and rarely SPENT. | A table per archetype × town |
| **F1 core** | The owed hours, the three states fixed at waking, sleeping in on SPENT, the first morning at 4, the save fields, the switch | `LabelManFatigueChecks`: rhythms in §2.1, save round-trip, old save loads FRESH; `FirstWeekUiChecks` still 8 presses, 1 day |
| **F2 the edge** | Fog steps, the check modifier, the Spent signature block with the Suit's line, the quiet voice | Checks for each, plus a UI driver run that ends a day at 1 AM in the studio and shows the next morning |
| **F3 the strip** | The hour strip, the state word and tooltip, the tonight preview, the morning line, the first-week lines in §2.4 (this is onboarding T4) | UI driver screenshots |
| **F4 the night** | §4: late sets, a drink with the booker | Author decision first |

## 7. Author decisions (accepted as recommended, 2026-11-26)

All five recommendations below were accepted as written: overtime wears him 1 an hour; SPENT blocks the signature;
the night (§4) opens, but after F1–F3; no archetype differences in F1; the bender is held as a later story beat.

1. **Does overtime wear him at all, or only the night?** The recommendation is a little (1 an hour). Otherwise most
   scouting, which happens in the evening, is free, and the player still has no reason to value an evening.
2. **Does SPENT block the signature?** The recommendation is yes. It's the clearest cost, and it's the joke.
3. **Open the night (§4)?** Recommended, but after F1–F3.
4. **Do archetypes differ?** For example, the Promo Man is used to jocks' hours, so the night costs him 1 an hour,
   not 2. The recommendation is no in F1, and maybe later as a conviction ("I Was One of Them" or a new card about
   the night).
5. **The bender.** At the 12 cap, a blackout morning: he wakes and finds he agreed to something. It's a strong echo of
   the opening and a natural red-check moment, but it's a story beat, not part of the mechanic. Hold it.
