# The Label Man's Fatigue: design

Status: design accepted (2026-11-26, §7 decisions taken as recommended). It answers the onboarding plan's open hook
(`SimTools/OnboardingTutorialDirective.md` §2B, §3.4, decision 5) and the new-player note *"I don't feel like I have any
frame of reference for how valuable time is."* Read with `SimTools/LabelManInnerLifeDirective.md` §0 (invariants), §3.1
(the hangover), §4.1 (mulling takes dead hours) and §6 (the empty early game). The F0 baseline and F1 implementation
are in §§6.1–6.2.

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

Fatigue is **hours owed**: the sleep he's behind on. It uses the same unit as the action costs in onboarding §3.4.
The player save stores it as a nonnegative integer in minutes, so short actions accumulate at the same rate as a long wait.

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
| By 6 PM ("knock off early") | 5 |
| After 6 PM through 11 PM | 2 |
| Past 11 PM | The two-hour recovery falls by a minute for every minute later, reaching zero at 1 AM |
| And the day was quiet: 3h or less spent, ended by 6 PM | 2 more |

It never goes below 0, and it's capped at 12 so a long bender can't dig a hole the player can't climb out of.

**What that gives, in rhythms:**
- 9 to 6 every day: always 0. A player who never stays late never meets the mechanic, except on the first morning.
- Three hours of overtime every night: +1 a day net, so **three nights out, then one night in.** That's the rhythm to
  teach.
- One studio night to 1 AM, from fresh: 3 OT + 4h × 2 = 11 owed, and nothing comes off. The 8h rule wakes him at
  9 AM, and being spent sleeps him till noon. Two quiet days clear it (11 → 4 → 0).

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

That's the whole lesson: what the state is, what made it, what clears it. WORN leaves the first signing path open,
and checks are off. `FirstWeekUiChecks` should take the same presses and days with fatigue on and off.

## 3. What the player sees

- **The hour strip** (onboarding §3.4): twelve blocks for the day, nine regular and three overtime, filled as hours
  are spent. When he's slept in, the lost morning blocks are struck through. Late-night blocks appear only when
  something runs past 9.
- **The state word** next to the clock: FRESH, WORN or SPENT. Its tooltip shows how many evenings ended in overtime
  and how many nights ended past 9 PM in the last seven completed days,
  what it costs today, and what clears it ("end the day by 6 tonight and you'll wake fresh").
- **A tonight preview.** An action that would end past 6 PM, or past 9, says what tomorrow will be: "Waits till
  8 PM · you'll wake worn." Only when the state would change, so the buttons don't fill up with it.
- **The morning line,** at the top of the desk on a WORN or SPENT day, in character: "You slept till eleven. The
  Lantern's last set is still ringing in your ears."
- Later, the Label Man's face (inner-life §9) carries it in his eyes.

## 4. The night

Today the player's world closes at 9 PM, and the rooms are clamped to it. Fatigue makes it possible to open the night
honestly. Past 9 PM costs 2 an hour, so it's a choice with a price, not a free extra session:

- **Late sets:** a room's sets after 9 PM can be heard (clamp to `room.CloseHour`, not `HardStopHour`, for the
  player's own visit). Rooms with late bills are where the R&B and jazz scenes really happened.
- **A drink with the booker or the act** after the set. It's a small regard gain on the existing booker and
  act-regard channels, and it costs 1 owed per drink. It's the business, and it's the problem.

This is a scope expansion with sim-adjacent edges (which bills exist after 9 PM), so it's its own phase (F4), and the
core doesn't depend on it.

## 5. Inertness and the bots

- **Player-only.** The save carries the owed minutes, the waking state, time spent today and the last finish minute.
  A save without fatigue loads at 0, as FRESH. Nothing AI-read, so the headless AI audits stay byte-identical
  ([[probe-run-byte-comparison-proves-inertness]]).
- **But the player bots change.** A Spent day that sleeps in, or a signature it blocks, changes what a player bot
  does. Player actions draw from the shared RNG (record hops, window cards), so a bot run's AI CSVs will move. That's
  expected and not a leak. Give the mechanic a switch (`--disable-label-man-fatigue`) so `OpeningCashLockRunner` and
  the empty-day metric can be compared with and without it.

## 6. Phases

| Phase | Scope | Check |
|---|---|---|
| **F0 measure** (measured in §6.1) | From the existing bot runs (`OpeningCashLockRunner`, `SimLogs/empty-1001-*` style), count overtime hours and nights past 9 PM per week for the busy and plain players. Test recovery values against ~1 day in 4 WORN and rarely SPENT. | A table per archetype × town |
| **F1 core** | The owed hours, the three states fixed at waking, sleeping in on SPENT, the first morning at 4, the save fields, the switch | `LabelManFatigueChecks` covers rhythms, save round-trip and older saves. `FirstWeekUiChecks` compares on and off. |
| **F2 the edge** | Fog steps, the check modifier, the Spent signature block with the Suit's line, the quiet voice | Checks for each, plus a UI driver run that ends a day at 1 AM in the studio and shows the next morning |
| **F3 the strip** | The hour strip, the state word and tooltip, the tonight preview, the morning line, the first-week lines in §2.4 (this is onboarding T4) | UI driver screenshots |
| **F4 the night** | §4: late sets, a drink with the booker or the act | `LabelManFatigueChecks` covers the late calendar, night rate, both companions and save round-trip. |

### 6.1 F0 baseline, 2026-11-27

The archived `empty-1001-*` action traces give a lower bound on overtime. The median busy run logged at least
4.44 overtime hours per week, compared with 0.40 for plain runs. Those traces record whole hours after logged actions
and omit the end-of-day clock. To measure actual exposure, `OpeningCashLockRunner` now records each day's start and
end in minutes and counts completed intercity bus legs of at least four hours.

The rerun uses seed 1001 for 182 days per case, with `--enable-genre-market-v2` and
`--enable-artist-population-lifecycle`. Busy uses `--policy=discerning --road=residency --sell=working --backer=ask --demos=work`.
Plain uses `--policy=frugal` and the runner defaults. Inputs are `SimLogs/fatigue-f0-1001-*-opening.json`.

The replay charges the measured portions of 6–9 PM and after 9 PM, counts the bus legs, then applies the cap and
the original overnight recovery (zero after 9 PM). It carries fractional hours as owed minutes and assigns each
morning's state against the thresholds in §2.2. It follows each bot's recorded choices throughout the run, so the
projections exclude changes in behavior from sleeping in, missed opportunities or a signature block.

All 24 runs recorded 182 days each. Median weekly overtime was **2.91h busy** and **0.53h plain**. Neither profile
recorded a night past 9 PM, and the plain runs took zero long bus legs. The table shows projected morning counts with
the F1 starting values, including the scripted WORN morning on January 1.

| Archetype | Town | Busy overtime h/week | Plain overtime h/week | Busy long bus legs | Busy WORN / SPENT days | Plain WORN / SPENT days |
|---|---|---:|---:|---:|---:|---:|
| ExMusician | Detroit | 8.13 | 0.69 | 10 | 29 / 4 | 2 / 0 |
| ExMusician | Memphis | 0.97 | 0.51 | 0 | 3 / 0 | 2 / 0 |
| ExMusician | New York | 2.76 | 0.60 | 8 | 3 / 0 | 2 / 0 |
| PawnShopOwner | Detroit | 7.67 | 0.50 | 10 | 28 / 8 | 2 / 0 |
| PawnShopOwner | Memphis | 0.97 | 0.51 | 0 | 3 / 0 | 2 / 0 |
| PawnShopOwner | New York | 4.76 | 0.54 | 10 | 9 / 0 | 2 / 0 |
| PromoMan | Detroit | 8.08 | 0.69 | 12 | 29 / 23 | 2 / 0 |
| PromoMan | Memphis | 0.97 | 0.51 | 0 | 3 / 0 | 2 / 0 |
| PromoMan | New York | 2.37 | 0.60 | 6 | 6 / 0 | 2 / 0 |
| TradeInsider | Detroit | 7.40 | 0.50 | 8 | 28 / 24 | 2 / 0 |
| TradeInsider | Memphis | 0.97 | 0.51 | 0 | 3 / 0 | 2 / 0 |
| TradeInsider | New York | 3.06 | 0.60 | 8 | 4 / 0 | 2 / 0 |

**F1 starting values.** Ending by 6 PM clears **5 hours owed**, up from 4. Ending later through 9 PM clears 2,
and a quiet day still adds 2 to the early recovery. On the busy runs this moves WORN from 5.4% to **6.3%** and
SPENT from 5.8% to **2.7%**, excluding January 1. Six hours of early recovery projects 6.2% WORN and 1.6% SPENT.
Cutting overtime recovery to one hour with the original early recovery projects 4.9% WORN and 12.1% SPENT.
The plain runs project 0.6% WORN and zero SPENT with the chosen values.

**The one-in-four WORN target is still open.** Detroit's busy runs account for all SPENT mornings and still
project 8.1% SPENT after the change. The Memphis bots in both profiles never signed, and the recording policy
books morning studio time. These runs support tuning recovery from overtime and bus travel. Tuning the after-9 PM
charge needs runs with late studio dates or player traces that include them.

```text
python SimTools/analyze_fatigue_baseline.py "SimLogs/fatigue-f0-1001-*-opening.json"
```

### 6.2 F1 core

The desk charges fatigue on actual clock advances and completed intercity bus legs. The day turn applies recovery and
fixes the new waking state before the morning runs. A SPENT morning uses the later of the existing eight-hour sleep
floor and the fatigue wake time. The fatigue wake time uses one hour for each whole hour owed over five, up to noon.

`--disable-label-man-fatigue` uses the usual clock, starts FRESH and omits fatigue from the player save. Older saves
load FRESH at zero. A mid-day save preserves the state from that morning and the exact debt at the time of the save.

Seed 1001 passed `LabelManFatigueChecks` (31 on, 11 off), `StudioNightRunner` (41 in each mode) and
`TransportChecks` (100). `FirstWeekUiChecks` passed all 80 checks in each mode, with 12 presses on a glow and nine
in-game days to the signing checkpoint. Four player-free chart weeks produced 94 byte-identical CSVs across the two modes.

The live comparison used six paired 182-day openings with the F0 policies. The runs used TradeInsider in Detroit,
PawnShopOwner in New York and ExMusician in Memphis, each busy and plain. The six switch-off runs matched the F0
action and daily reports. With fatigue enabled, Detroit busy had 25 WORN mornings and 19 SPENT mornings after
January 1, losing 36 hours to later starts. New York busy had eight WORN mornings and Memphis busy had two.
Each plain run had one WORN morning after January 1. SPENT occurred only in the Detroit busy run. The one-in-four
WORN target remains open. Raw player reports are under `SimLogs/fatigue-f1-1001-*-opening.json`.

### 6.3 F2 player effects

WORN and SPENT add one and two steps of uncertainty to the player's new hearings. Live reads use the existing
instinct noise steps. Scouting quality and Polar ear reads widen by 20% per step, with fatigue-specific keys to keep
each read repeatable. A saved set's hook read stays with the evidence from that hearing.

The Talk shows Tired −1 or Spent −2 in check odds once checks open. SPENT closes the artist signing and renewal
actions before they spend time. The Suit gives the reason on the terms sheet. The least trusted voice is chosen at
waking and stays quiet through the day, including across a save. Ties follow the displayed instinct order.

The overnight desk driver passed 44 checks, including the noon clock, the quiet voice notice and the disabled table
button after a 1 AM studio finish. The disabled control passed 19 checks. The first-week tutorial passed all 80
checks with 12 presses on a glow and nine in-game days to signing. The four-week player-free audit matched F1
byte for byte across all 94 CSVs.

### 6.4 F3 desk clock

The office HUD, folder and studio use the hour strip. It fills down to the minute, separates regular and overtime
hours, strikes through the time slept away, and extends past 9 PM as time is spent in a late date. The waking
minute is saved so the marks stay put after a reload. The state appears beside the strip. Its tooltip explains
the day's effects and projects the result of turning in now. The office and folder show a morning line while
WORN or SPENT.

Wait, scouting, travel, contract and studio previews compare tomorrow's state if you use the quoted time with
tomorrow's state if you turn in now. The studio quote says when it is estimating the full booked date. The first
long desk wait, opening overtime and first WORN hearing have one-shot lines saved with the tutorial. The desk
shows cash runway in months of rent, and the flip calendar counts down to the opening of the sitting window.

The rendered overnight desk run passed 54 checks. It verified the four late blocks at 1 AM and the three
crossed-out blocks at noon. The fatigue-off control passed 24 checks, and the first-week tutorial passed 80
checks with 12 presses on a glow and nine in-game days to signing.

### 6.5 F4 the night

In a player run with fatigue enabled, newly scheduled club bills and Saturday listening-room bills start at 8 PM.
They can run past 9 PM, and the rooms close at midnight on those nights. Existing announced bills keep their
posted times. A&R's calendar and itinerary use those hours when you go, wait and listen. The ordinary desk
cutoff is still 9 PM. Waiting and listening past that point cost two owed minutes per minute.

After a set, a drink with the booker or an available act member takes 30 minutes and adds one owed hour on top
of the cost of time. The booker remembers meeting you and gains one point of familiarity, or the act gains
one point of regard. The choice persists in the notebook so one set cannot be used for a second drink.

### 6.6 F4 balance and clock tooltip

The control and late-scouting runs use seed 1001 for 84 days per case, with TradeInsider in Detroit, PawnShopOwner
in New York, and ExMusician in Memphis. They use the F0 busy policy. After ordinary work, the late-scouting
profiles go to the first affordable local set ending after 9 PM, up to their weekly limit, and take a drink with
the booker if enabled and available. The results exclude the scripted January 1 WORN morning.

| Scouting policy | Late visits, before → tuned | WORN mornings, before → tuned | SPENT mornings, before → tuned | Morning hours lost, before → tuned |
|---|---:|---:|---:|---:|
| Ordinary work control | 0 | 12 | 0 | 0 |
| Up to one late set a week | 33 → 33 | 40 → 45 | 16 → 6 | 37 → 10 |
| Up to one late set and drink a week | 33 → 33 | 13 → 41 | 47 → 15 | 110 → 36 |
| Up to two late sets and drinks a week | 64 → 66 | 24 → 70 | 114 → 76 | 282 → 204 |

Each row covers 249 mornings across the three cities. The control is the original recovery rule. It had no
late finishes. All measured scout visits finished at 10 PM without a drink or 10:30 PM with one. The weekly
drink profile went from 18.9% SPENT to 6.0%, with 4 SPENT mornings in Detroit, 11 in New York, and none in
Memphis under the tuned rule. The twice-weekly drink profile reached 28.1% WORN and 30.5% SPENT. This pass
reduces the cost of an ordinary night out. The one-in-four WORN target with SPENT rare remains open, especially
for repeated outings. The profiles keep scouting when WORN or SPENT and cover one seed and opening-period
club hours. Reports are under `SimLogs/fatigue-balance-1001-*-opening.json` and `SimLogs/fatigue-tuned-1001-*-opening.json`.

Ending at 9:01 PM previously forfeited all two hours of overtime recovery. Recovery now stays at two hours
through 11 PM, then falls minute for minute until 1 AM. From zero debt, finishing at 10:30 PM now leaves four
hours owed the next morning. An 11:30 PM finish leaves six and a half hours and a 10 AM start. The 1 AM studio
case still leaves eleven hours owed and a noon start. A drink after a 10 PM set leaves five hours owed from
zero. A drink after an 11 PM set leaves seven and a half.

The clock tooltip counts overtime finishes and nights past 9 PM over the last seven completed days. The player
save carries the history. Saves from before the history field use the previous finish when it was recorded.

Example run, using the Godot console executable from the project directory:

```text
Godot --headless res://SimTools/OpeningCashLockRunner.tscn -- --seed=1001 --days=84 --archetype=TradeInsider --town=detroit --policy=discerning --road=residency --sell=working --backer=ask --demos=work --enable-genre-market-v2 --enable-artist-population-lifecycle --late-scout=1 --late-drink=booker --run=fatigue-tuned-1001-detroit-weekly-drink
```

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
