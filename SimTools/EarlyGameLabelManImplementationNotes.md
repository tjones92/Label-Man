# Early-Game Label Man V2: implementation notes

Branch `early-game-label-man` (fast-forwarded to `origin/main` c566f24 on 2026-10-14; the branch had been two
merge commits behind with no file differences). Directive: `SimTools/EarlyGameLabelManDirectiveV2.md`.

Author decision (2026-10-14): **no family loans.** Founding capital, such as the Ex-Musician's $800, *is* the family
money. A §4.3 backer, if built, must come from a non-family source.

## What is built

| Phase | Content | Files |
|---|---|---|
| 0 | Opening cash-lock probe: scripted, player-present, report-only | `SimTools/OpeningCashLockRunner.*`, `run-opening-cash-lock.sh`, `opening_cash_lock_summary.py` |
| 1A | Release completion preflight (§4.1), CASH OPTIONS card (§4.2), solvency wording (F7) | `Systems/PlayerDesk.CashPlanning.cs`; UI in `PlayerDeskPanel.cs`, `PlayerDeskPanel.StudioWindow.cs` |
| 1A gate | Quotes, prerequisites, due dates and event order on the real clock | `SimTools/CashPlanningChecks.*` |
| 1B §6.1 | DJ courtesy calls: introduce the label, ask what's moving, arrange a listen | `Systems/PlayerDesk.CourtesyCalls.cs`; Rolodex verbs/entry/scene |
| 1B §6.2 | Dealer advance orders off an acetate; shared record appeal; pre-orders held past arrival | `Systems/PlayerDesk.Groundwork.cs`; `PlayerDesk.cs` |
| 1B §6.3 | Pitch an act to a real booker; dated nights placed by the scene scheduler | `Systems/LocalSceneRoomService.Bookings.cs`, `Data/LocalSceneRooms.cs`, `PlayerDesk.Groundwork.cs`; UI `PlayerDeskPanel.SceneRooms.cs` |

Not started: Phase 2 (backer, non-family only), 3 (F1-F3 venue access/saturation), 4 (paid demos),
5/5A (master shopping, song placement), 5B (F5 gig pay), later/deferred items.

## Design choices worth knowing

- **Preflight never blocks.** `CompletionWarning(spend, covers)` names the first unfunded stage of the cheapest
  route (sign → record → press). It says "you cannot afford to press any of the singles you have ready" only
  when the gap is pressing an assembled title. A spend that pays for a stage (`covers`) drops it, so nothing is
  reserved twice. Stock on hand, runs at the plant and receivables qualify the line. Overhead falling before a
  run would arrive is named only when it takes cash below zero.
- **Solvency timing (F7).** Overhead is taken on the 1st and the bank checks straight after.
  `NextSolvencyCheck` and `ClosureCheckIfNothingChanges` forecast it; `CashPlanningChecks` advances the real clock
  and confirms the forecast matches what `OnMonthChanged` does, for a red check, a reset and a ceiling closure.
  The reset rule is unchanged (the 15-day alternative stays deferred).
- **CASH OPTIONS can be empty**, and then says why. Routes are read from the same gates the verbs enforce:
  balances owed (with gas and range), wholesale paper (wait or factor), called-in orders (COD on delivery),
  stock on hand (a pitch, not a promise), artist buy-in, master deals, plant credit, booked engagements. A
  route paying on or after the closing check is marked too late.
- **Acetate counter pitch (§6.2)** uses the existing pitch formula. `RecordAppeal` now reads an unreleased master
  (hook and production, no buzz) instead of the neutral fallback. Released records read exactly as before. A yes
  is a `PreOrder` inbound call capped at the existing pre-order bands, COD on delivery. A no is a stop pass (the
  same pass the released 45 would meet). Ordering a run, or plant credit, holds every pre-order on that title
  until two weeks past the vinyl's arrival; an order never shortens.
- **Courtesy calls (§6.1)** resolve without a roll. The introduction gains rapport once (+0.025). "What's moving"
  is a stable, imperfect read of the station's real playlist (a leading sound and one record on heavy rotation,
  never a ranking), refreshed at most every 4 weeks. "Arrange a listen" reuses the existing soft appointment.
  Putting the acetate in his hands keeps it (+0.04); never turning up costs rapport through the existing expiry.
  New enum values are appended after `HangUp`, so saved ordinals don't move.
- **Booker pitch (§6.3)** writes a dated `SceneBooking` for a night after the announced calendar (up to 6 weeks
  out). When that week is scheduled, the booked act is placed after the room's closing regular, under every
  ordinary rule. It displaces nothing already announced. If it can't be billed, or the set doesn't go ahead, the
  booking is cancelled with a reason. Only a played set counts. The label takes no money (F5 commissions come
  later). A no stands for a quarter unless the act plays the room meanwhile. AI-only worlds hold no bookings, so
  the scheduler path is unchanged for them.

## Authored values (not historical claims)

Courtesy rapport +0.025 / kept listen +0.04; moving-read refresh 4 weeks; booker accept chance
0.10 + 0.45·live + 0.25·reputation + 0.20·min(1, shows/4), clamped 0.05–0.90; booker hours from 2 p.m.; booking
horizon 42 days; booker refusal 91 days; completion forecast assumes 30 plant days. The CASH OPTIONS order value
uses the existing `DealerPrice` ($0.63).

Historical anchors used here are only the kinds of transaction (§2): dealer and DJ hand delivery, regional
promotion, booking alongside recording. None of the numbers above is sourced.

## Verification status

- **AI-only byte identity, Phases 1A+1B:** a 52-week seed-1001 run (`egl-1ab-1001-52`, canonical flags) matches
  `SimLogs/ref-c566f24-1001-52` on all 84 CSVs, byte for byte. The reference is c566f24 plus only the
  never-instantiated probe runner.
- **Phase 1A gate:** `CashPlanningChecks` passes 26/26. It covers stage order and quotes for a fresh label, the
  $128 first run, covered-stage deduplication, warnings naming the right stage, an honest empty CASH OPTIONS, and,
  on the real clock, a red check, a reset that restarts the forecast, and a ceiling closure, each matching
  `OnMonthChanged`.
- **Probe equivalence across DLLs:** a finished baseline config re-run on the Phase 1A/1B DLL matched all 1,029
  actions and every daily cash row. The only difference was the new, null guidance field. Baseline runs from either
  DLL are therefore comparable.
- **Save/load:** `SaveLoadRoundTripRunner --integration` was extended to stage and check the Rolodex courtesy fields,
  a scene booking and a booker pitch. **Passes** (`EARLY_GAME_ROUNDTRIP ... ok=True`, `SAVELOAD_INTEGRATION_PASS`).
- **Phase 1B preparation probes (§6.4)** (`--prep=dealer|radio|live` vs `none`): **not yet run.**

## Phase 0 baseline (`SimLogs/ocl-base-summary.md`, 170 runs, 180 days, pre-title-fix build)

- **No opening ever hit L1 or an unfunded completion path.** Every run that signed an act cut, pressed and released
  a single (release on day 21–42, median 31). The original directive's "certain death in about four months" cash
  lock did not occur under any scripted policy, archetype or town.
- **33 closures, all on the 1st of May or June (days 121/152), all in New York, LA or Detroit.** All are three red
  checks with stock unsold: the record didn't move at the home-town counters (median 62 copies placed COD by
  pitching), and overhead drained cash. Frugal openings never closed; replay (the $350 advance) and eager closed
  most. Pawn Shop's $1,400 never closed.
- **Two residencies (the §10 fixture) kept every New York and LA eager run open** that otherwise closed on day 121.
  That's the flat $22/week label inflow F5 is meant to correct, so it must not become the balancing crutch.
- **Billings released nothing in all 32 runs.** This is a probe-policy gap, not a game result: the clubs-only
  scouting never met Billings' roadhouse and coffeehouse acts. The policy now rotates room kinds; the quality batch
  uses the fixed policy.
- **Caveats** (author's point, 2026-10-14): a closure here may be a weak act or song meeting a small 1960 label's
  reality, not an unfair market. The baseline policies pick the cheapest act and the first two songs, sell only at
  home, and were dealt traits by the placeholder title bug. The quality batch (frugal vs discerning, true
  hook-quartile table) is what separates earned closures from imposed ones. Nothing is tuned before that.
- No stockouts; no invariant violations.

## Quality batch: earned or imposed closures? (`SimLogs/ocl-q-summary.md`, 40 runs, post-title-fix)

Frugal (cheapest act, first songs) against discerning (best act it can *read* within budget, best-heard songs) ×
five towns × seeds 1001–1004, ExMusician, one single, venue rotation on.

- **No closures in 180 days** for either policy. One act and one single is survivable everywhere; New York and LA
  run red but don't reach three red checks.
- **Sales follow true quality.** Median copies sold: bottom hook quartile 33, second 125, third 110. Discerning's
  picks sold 2–5× frugal's. The market discriminates, which is the earned side of the author's test.
- **The test isn't complete yet:** no player record reached the top hook quartile (best: 68th percentile), and
  production sat at the 3rd–42nd percentile because budget rooms cap it. Whether a genuinely strong record is
  treated fairly is still open.
- **Nothing broke even on its first run in six months** (best 196 copies vs 204). The probe sells only at home-town
  counters: no radio, travel or distribution. That channel is too narrow to call demand unrealistic from this.
- **Billings:** sparse talent; the one reachable act took a rival's $285 paper over our at-ask offer. That's the
  market working, not a probe or game bug (a player can outbid).

## Fairness batch: every channel worked (`SimLogs/ocl-fair-fairness.md`, 100 runs, 180 days)

Author's corrections (2026-10-14) that this batch answers. (1) The playtest's lock was the *follow-up* pressing: the
first run sold out, the repress took the cash, and the second single couldn't be pressed. The earlier probe never
reached that state, because eager pressed both singles at once while cash was high and no run ever sold out.
(2) Budget production should hamper sound, not gate public reception. (3) Residencies should open options, not be
automatic or prop a label up.

Probe additions: `--sell=working` services reorders, clears dead lots (`AcceptReturn`), works hop tables, opens the
one-stop, drops promos at stations, takes a weekly trip to one of the three nearest towns and represses a sold-out
title. `--sequence=staggered` cuts the second session a week after the first single is out (the playtest's order).
`--policy=oracle` picks act and songs by probe truth. The fixture is ExMusician × 5 towns × seeds 1001–1004 ×
{frugal, discerning, oracle} batch + {eager, replay} staggered.

- **The home-only probe badly undersold.** With every channel worked, median copies per single run 608–774 by
  town, against a best of 196 before. Hop tables are 77% of units (median per run).
- **The market does not discriminate by quality any more.** Every hook quartile sold about 70% of what it
  pressed, and all 105 singles beat first-run break-even. Hook explains ×1.03 copies per SD, and production
  explains nothing (×0.93, noise). Sales are capped by how much the label presses and can afford to repress, not by
  how good the record is. So a strong record isn't treated unfairly; **a weak record is treated too kindly.**
- **Why:** `WorkTheHopTable` sells `lerp(3,12,relationship)×nerve×ear` copies cash-at-list with no roll, once per
  table per day, and nothing depletes the crowd. The same table was worked a median 22 times per run (max 46).
  The `ear` term (0.55–2.50) scales each visit's size but never stops a visit from selling. Fixing this is the
  precondition for any fairness reading. Production weight (0.25 of trunk appeal) is not the problem.
- **The follow-up-pressing lock reproduces as a repress lock, in New York and LA only.** All 7 closures were
  staggered two-single openings there (3 eager, 4 replay), on day 121 or 152. Each spent weeks sold out with demand
  open and cash short of the repress (unfunded repress up to 14 weeks in a run). Only one run (2002 LA eager, day
  54) left an assembled single unpressable. One-single openings never closed (0 of 60).
- **Caveats:** in replay, the second session of a one-act label prints one master (material exhausted), so its
  second single never ships. A player would write or commission songs. The repress rule is the probe's own (open
  calls, or half a run sold). The sales volume is inflated by the table channel above, so the cash path is
  optimistic for weak records.
- **Residencies:** `BookRoad` (`PlayerDesk.BandRoom.cs`) has no room, booker, quality check or refusal. Any signed
  act gets $22/week for an hour's work. That is the automatic acceptance the author rejects.

## Table crowds + booker-gated residencies (`SimLogs/ocl-fair2-fairness.md`, 140 runs, 180 days)

Built (author request, 2026-10-14):
- **Hop-table crowd saturation** (`Systems/PlayerDesk.TableCrowd.cs`). Each venue stop has a fixed crowd of 80–260
  regulars (a third `WithCharacter` trait, drawn after the first two, so neither earlier trait moves). The
  record's appeal sets the share that will ever buy it: 4% at appeal 0.30, up to 32% at 0.80. Buyers already
  served are remembered per title, and 5% of the crowd turns over each week. A visit sells the table's usual size
  times the share of interested regulars still left. When none are left it sells nothing and says so. Airplay
  widens the crowd through the buzz term in appeal. Saved per stop (`PlayerStopSaveData.TableCrowds`).
- **Residencies are a booker's yes** (`BookRoad`, `PlayerDesk.BandRoom.cs`). A residency now needs a
  performance room in the act's own scene, suited to its music, where the act has already played 2 sets. The
  player must be in town during the booker's hours. Acceptance is the §6.3 chance × 0.6, and a refusal stands for
  91 days unless the act plays the room again. The flat $22 is gone: the label earns a 10% booking commission on 3
  nights a week of the room's expected night pay (`SceneLiveEconomics.ExpectedPlayerNightPay`). A residency booked
  before this has no room behind it and pays the label nothing (F5 direction). Tours are unchanged ($30/week,
  still F5's to fix). CASH OPTIONS reads the commission.
- **Verification:** AI-only 52-week seed 1001 matches `ref-c566f24-1001-52` on 84/84 CSVs. Save/load integration
  passes (new `TABLE_CROWD_ROUNDTRIP ok=True`). Band-life checks pass 43/43, with the residency check rewritten:
  a residency is refused before the act has played a room, and a tour still books.

Results:
- **Quality now discriminates.** Hook moves copies ×1.32 per SD (was ×1.03), R² 0.50. Median copies run 277 /
  393 / 529 / 401 by hook quartile (the top quartile has only n=3). Every closure was on a label whose singles
  sat in the bottom two quartiles (40 of them; 0 in the 3rd or top). Production stays inert (×0.94).
  Median copies per single fell from ~650 to ~330. Tables still lead the channel mix, but 71% of visits sold
  nothing once the crowd had the record.
- **Weak records still mostly break even:** 82 of 92 bottom-quartile singles passed 204 copies in six months of
  full hustle. Whether that is too kind is the author's call, not a probe finding.
- **Closures are now the careless second spend.** 26 of 140 runs closed, all two-single staggered openings (eager
  14/40, replay 12/20), mostly New York and LA on day 121. Almost none sold out (repress count 0). They died of
  overhead with stock unsold. One-single openings closed 0 of 80.
- **Residencies no longer prop a label up.** 36 won from 99 asks across 40 runs (first won around day 30), after
  63 booked nights were played. The median commission is **$0.78/week** (range $0.49–$16.76), because realized
  night pay per player is small. Residency arms closed exactly as often as no-residency (eager 7/20 both ways).
  The residency now opens the room and the act's name, not cash. If the author wants it to "help open options"
  in cash terms too, the lever is the scene's night pay, not the commission rate.
- **New trap found (not fixed):** a founder away from home pays the $9 motel into debt, but the drive home is
  refused for $2 of gas once cash is negative. One smoke run sat stranded in Philadelphia for 30 days until the
  credit line closed it. The probe now only travels with the round trip + $50 in hand.

## §6.4 preparation probes (`ocl-p-*`, discerning, New York and Detroit)

- **Dealer-first:** 14–21 acetate pitches → 2–3 advance orders per town, all delivered COD on arrival, none lapsed
  (0 invariant violations). Sales modestly up (106 vs 96, 127 vs 114 copies).
- **Live-first:** 3 bookings won and 3 played per town; none fell through. No sales lift yet. That's expected:
  nothing converts a played set into local buyers until F3. Booker acceptance came out 6/6, worth watching for
  being too easy.
- **Radio-first:** the first pass connected 0 of 21 calls. That was a probe fault (it dialled a graveyard jock at 9
  a.m. and found no second contact). Fixed to call inside each card's window. Now: 48–57 dials, 10–18 connected, 4
  listens arranged and 1 acetate hand-delivered per run. Courtesy calls run out ("nothing new to say" up to 8×), so
  repetition earns nothing. No sales lift: one acetate reaches one station. In Detroit 1001 the radio contact
  turned the single over, so the weaker side became the plug side (existing flip mechanic), and it sold 91 vs 96.
  The probe also arranged four listens while holding one acetate. The game allows that and will cost rapport when
  three expire, which is fair: a player shouldn't promise four jocks one disc.
- **Gate reading (§6.4):** the three plans create distinct opportunities (counter orders, played nights, radio
  relationships). No duplicate demand, no lapsed orders, no unresolved bookings, and repetition is unrewarded. Cash
  after delivery is not materially better than waiting yet. Orders are small and played nights feed no local buyers
  until F3. That's the expected state for this phase, not a reason to inflate the rewards.

## "Unrecorded original" title fix (separate bug, found by the probe)

Scene bills named every act's own song "Unrecorded original". The placeholder survived signing and recording onto
released singles, and titles hash into track traits. Fixed with `NameGenerator.KeyedSongTitle`: a pure function of
a key and the world seed that draws from no title stream and records nothing in uniqueness buckets. It is used by
`LocalSceneRoomService.OriginalTitle` in the scene set builder and in `BuildLiveSet`'s scene-read path. Unrecorded
repertoire items in old saves are renamed on load. This **changes player-present outcomes**, because a real title
means different traits. So baseline runs from before the fix must not be mixed with runs after it. **AI-only
identity verified:** `egl-title-1001-52` matches `ref-c566f24-1001-52` on all 84 CSVs, so the keyed titles move no AI
stream.

## Stranded-away fix (drive home on the tab)

Found by the ocl-fair2 probe: out of town on negative cash, `ChargeHotelIfAway` kept booking $9 motel nights into debt while `Travel` refused the ~$2 drive home ("You're $391 short of the $2 gas"), until the credit line closed the label. `Travel` now skips the cash gate when the destination is home: gas books on the tab like the motel, and the monthly solvency check still applies. Outbound trips stay gated on cash. `CashPlanning`'s "collect balances" row no longer marks the home town unaffordable. `CashPlanningChecks` covers it (30 checks pass). Player-only. **AI-only identity verified:** `dh-fix-1001-52` matches `ref-c566f24-1001-52` on all 84 CSVs, and `SaveLoadRoundTripRunner --integration` passes.
