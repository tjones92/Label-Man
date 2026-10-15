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
| 2 §4.3 | One backer agreement per label, non-family sources, ledger, 1st-of-month payments | `Systems/PlayerDesk.Backer.cs`; gate `SimTools/BackerChecks.*`; UI `BuildBacker` in `PlayerDeskPanel.cs` |
| 4 §7.1 | Paid demos: real unsigned clients, held deposits, producer fee, demo sample | `Systems/PlayerDesk.Demos.cs`; gate `SimTools/DemoChecks.*`; UI `PaidDemos` in `PlayerDeskPanel.SceneRooms.cs` |
| 3 F1-F3 | Room tables, audience saturation, local appeal | `Systems/PlayerDesk.RoomTables.cs`, `PlayerDesk.LocalAppeal.cs`; gate `SimTools/RoomTableChecks.*` |
| 5 §5 | Shop the master: one deal state machine, real buyers that release, lease/sale/sale-with-act, regional pickup | `Systems/PlayerDesk.MasterDeals.cs`; seams in `CompetitorManager.cs`, `RosterManager.cs`, `Data/Record.cs`; gate `SimTools/MasterDealChecks.*`; UI `UI/PlayerDeskPanel.MasterDeals.cs` |
| 5A §7.2 | Place a song: real labels' acts cut the player's songs; advance, hold, settlement at the period mechanical | `Systems/PlayerDesk.SongPlacement.cs`; seams in `CompetitorManager.cs`, `CompositionCatalogService.cs`, `Data/Record.cs`; gate `SimTools/PlacementChecks.*`; UI `BuildSongPlacements` in `UI/PlayerDeskPanel.MasterDeals.cs` |
| 5B F5 | Gig pay is the performers': booking commission off each played set the label booked; residencies are dated nights; tours pay the label nothing | `Systems/LocalSceneRoomService.Bookings.cs` (+ fee split in `LocalSceneRoomService.cs`), `PlayerDesk.BandRoom.cs`, `PlayerDesk.Groundwork.cs`, `PlayerDesk.CashPlanning.cs`; gate `SimTools/GigPayChecks.*` |

Phases 3, 2, 4, 5, 5A and 5B built 2026-10-14 (in that order), see below. Not started: later/deferred items (F6
route convenience, §4.4 partner).

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

## Weak singles sink: counter and route saturation (`SimLogs/ocl-fair3-fairness.md`, 140 runs; committed 95c4467)

Author (2026-10-14): weak records were "probably" too kind. In fair2, 82 of 92 bottom-quartile singles cleared
first-run break-even (204 copies), median 281. The sources found (regional custom pressings of 200–1,000 copies,
"most sold very few") point the other way.

Attribution: a dud's copies came mostly from shop shelves. Pitches placed only ~12 copies, but service top-ups
restocked ~310 at home. A shelf sells `Placed × 0.07 × appeal × decay`, every service resets the decay, and appeal
is linear (a dud at 0.30 sells at 60% of the pace of a strong 0.50). So restocking reopened the counter's full
appetite for any record. Hop tables still gave a dud ~110 copies through their linear 4% take-up floor.

Change (`Systems/PlayerDesk.TableCrowd.cs`, `ProcessTrunkDay`):
- Shop counters and jukebox routes now remember who has bought each title (`PlayerStop.TableCrowds`, now kept
  for every stop kind and saved as before).
- A counter serves 150–500 record-buying regulars and a route 60–200 machines, using the `CrowdSize` trait.
- The share that will ever take the title is `lerp(1%, 25%, t^1.5)`, where `t` is appeal across 0.30–0.80.
- The day's shelf sell-through is thinned by the share of those buyers still left, and never exceeds them; 5% of
  the crowd turns over each week.
- Tables moved to the same convex curve, `lerp(1%, 40%, t^1.5)` (was a linear 4–32%).
- Local airplay still lifts appeal through buzz, so a weak-sounding record the radio is playing still sells.
- Authored values, not sourced. AI-only 52-week seed 1001 matches `ref-c566f24-1001-52` on 84/84 CSVs.

Results (fair2 → fair3):

| hook quartile | singles | median copies | sold ≥ break-even | on a label that closed |
|---|---|---|---|---|
| bottom | 92 | 281 → **169** | 82 → **21 (23%)** | 28 → 39 |
| 2nd | 35 | 393 → 291 | 35 → 26 (74%) | 12 → 12 |
| 3rd | 24 | 529 → 401 | 23 → **24 (100%)** | 0 → 1 |
| top | 3 | 401 → 266 | 3 → 3 | 0 → 0 |

- Hook now moves copies ×1.47 per SD (×1.32 in fair2, ×1.03 before table crowds). Production stays inert (×1.03).
- Closures 26 → 37. Two-single staggered openings still carry most of them (eager 16/40, replay 16/20). One-single
  openings now close 5 of 60, all in New York or LA on day 152. Four were bottom-quartile records. One was a
  53rd-percentile oracle single in New York that sold 216 copies (past break-even), and the $175 overhead still
  closed the label. That's a New York cost question, not a weak-record one.
- Median copies per single by town: 150–228 (was 257–372).
- Caveats: the one-stop carton is still record-blind once a demand call opens it (a dud in a smoke run sold one
  70-copy carton COD). Its returns window was not measured. The oracle's truth proxy (act traits, catalog hook)
  is weak: its singles' median hook percentile was ~0.19, so the top quartile is still only n=3.

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

## Phase 3: real room tables and local appeal (F1-F3), 2026-10-14

Built (author chose Phase 3 next):
- **F1 room tables** (`Systems/PlayerDesk.RoomTables.cs`). With the room calendar on, a record table is a seat at a dated
  bill in one of the town's `SceneRoomCatalog` rooms: club, community hall, roadhouse or coffeehouse. Listening/supper
  rooms and trade appointments put no table out. Permission is the booker's: your own act on tonight's bill brings its
  records to its own show (no cut); otherwise `AskForTableRights` (1h, booker hours) gives a 91-day term at the room's
  cut of the table gross (club 25%, roadhouse 20%, coffeehouse 15%, hall 10%). A no stands for the term unless one of
  your acts plays the room. `WorkRoomTable` sets up an hour before the first set and stays to the end; the sale is read
  off the resolved bill, so a show that doesn't go ahead sells nothing (the hours are still spent). Arriving late sells
  in proportion to the bill staffed. The walk-up `WorkTheHopTable` is refused in room towns; the synthetic Venue stops
  stay as record-hop sites.
- **F2 saturation** on room tables: a pool of ~3x capacity regulars; interest is the shared convex appeal curve (1-40%),
  halved off the room's music; tonight's reach is attendance / pool, doubled when your act played; at most 30% of the
  house and 40 copies a night; buyers remembered per room and title, 35% of the town's other room-table buyers count
  against this room, 5%/week turnover. The record hop now sells only to the hall crowd's interested buyers not yet served
  (it had no memory at all).
- **F3 local appeal** (`Systems/PlayerDesk.LocalAppeal.cs`), one term inside `RecordAppeal`/`TrunkAppeal`, so counters,
  order sizes, shelves, acetate pitches and room tables all read it. Following is derived from existing room history
  (`LocalSceneRoomService.LocalDraw`: each room's attributed arrivals x share that stayed, 8-week half-life from the act's
  last night there), saturated as 1 - exp(-heads/120), worth up to +0.06. Taste is 0.025 x ln(`SceneCityPlacement.Fit`)
  within +-0.04 (size cancels, so sparse cities read neutral). Jointly clamped to [-0.04, +0.08]. Not written to
  awareness or buzz; the existing live-draw recognition channel feeds national recognition, which appeal doesn't read.
- CASH OPTIONS lists the next room-table night as speculative (too late if after the closing check). UI: "SELL AT THE
  ROOMS" on the venues board; the old venue row loses its walk-up button in room towns.
- **Migration:** synthetic Venue stops never held stock, balances or calls (`ValidateStopAction` refuses Venue; calls
  exclude it), so retiring the walk-up table moves nothing. Their crowd memory carries on for hops. No table right is
  inferred from an old save. New state: `PlayerSaveData.RoomTables` (optional; older saves hold none).

All values above are authored, not sourced.

Verification:
- **AI-only identity:** `egl-p3-1001-52` matches `ref-c566f24-1001-52` on 84/84 CSVs, byte for byte.
- `RoomTableChecks` 28/28 at first build (walk-up retired, booker hours, refusal stands, rights lapse, supper rooms refuse, no stock,
  cross-room overlap, taste bounds/direction, draw fades by a half-life, following saturates, joint lift bounds).
- `SaveLoadRoundTripRunner --integration` passes (new `ROOM_TABLE_ROUNDTRIP ok=True`). `CashPlanningChecks` 30/30.
- **Cancelled night, forced** (`RoomTableChecks`, now 49/49). The probe never met one (0 of 501 nights), so the check
  forces it: a checks-only `LocalSceneRoomService.BreakLineupsForChecks` gives every set on a real Detroit club bill a
  departed member after booking, and the clock's own `Resolve` cancels them while the table is up. Result: the bill
  resolves Cancelled with 0 in; the player is told it didn't go ahead; no copies leave the box, no sale, no room cut, no
  buyers remembered; the hours are still spent; the night drops off the table list and can't be worked again. Control:
  dropping one set of three leaves the night Performed on two (38 in), and the table sells within the night's ceiling
  with buyers remembered to match. Not changed: if your own act's set falls through but the rest of the bill plays, a
  table you set up on its strength still sells, at the room's usual cut.

## Live-first vs waiting (`SimLogs/ocl-p3-live.md`, MATRIX=live, 24 runs, 180 days)

Discerning act, every channel worked, seeds 1001-1004 x New York / Detroit / Memphis, `--prep=live` (pitch bookers
during the plant wait) against `none`. Paired runs share a seed, not a world: the player's actions move the shared stream,
so the signed act and songs differ in several pairs (hook percentile .61 vs .48 in 1001 Detroit). Read the direction, not
the size.

- **Tables are small and earned.** 501 table nights across 24 runs sold 10-130 copies a run (room cut $2-17). Tables no
  longer lead the channel mix (counters and service do). A dud's table goes dry: 1003 Memphis (hook pct .03) worked 102
  nights, 89 sold nothing. 0 cancelled nights, 0 invariant violations.
- **Live-first helps a little, in the right direction.** Following in the home town was higher in 10/12 pairs, table
  copies in 9/12, final cash in 8/12 (median +$14); units ahead in only 6/12 (median +20), inside the stream-drift noise.
  Acts in the waiting plan already build some following from the scene's own rotation (Memphis 1001 none: 0.69).
- **New York still runs red** on both plans (final -$213 to -$326; one waiting run closed day 152). F3 doesn't change
  the New York overhead question.
- Booker acceptance stays easy (bookings fell through 2 of 26); table rights were won about 60% of asks.

## Phase 2: one backer agreement (§4.3), 2026-10-14

Built (`Systems/PlayerDesk.Backer.cs`). Author decisions: no family money; the Ex-Musician's backer is a club owner from
the home scene; the Pawn-Shop Owner's pledge is the label's pressed stock.

- **Sources, all from the world or a named private person:** Ex-Musician, the man who runs a home-scene club (roadhouse
  if none), up to $500 ($400 roadhouse). Pawn-Shop Owner, a merchant lender against pressed stock, $600. Promo Man, the
  home town's jukebox operator (a real Op account), $500. Trade Insider, a private backer a trade contact introduced, $800.
  Private names are keyed (`SceneRoomCharacter.PersonName`) and draw on no stream.
- **The ask** is a 2h meeting in his hours (club owner from 2 p.m. at his room; the others 9-5 at the office). He reads a
  signed act, whether anything is recorded (master, acetate or release), and the completion budget (remaining route + two
  months' overhead, in fifties, floored at $300 and capped by his means). He never reads the hidden hook. The answer is
  deterministic, so asking again on unchanged evidence can't reroll it; a no stands until the evidence changes. Unheard,
  he objects ("I haven't heard a note of it") and the terms change: a smaller sum ($300 at 6%), or 75% of the budget at 9%.
  With a record to play him, the full budget at 6%. An offer stays open 14 days. The operator also wants something
  recorded. Room fit was tried for the club owner and dropped: it made the backer depend on which club the catalog lists
  first (the New York smoke run was refused 111 days running).
- **Terms (authored):** simple interest on outstanding principal, actual/365 from the day the money is paid; first
  payment on the third 1st after signing; 12 installments of a twelfth plus all accrued interest. Taken on the 1st after
  the overhead and before the bank's check, interest first, then overdue principal, then this month's, **from cash above
  zero only**, so a payment can never turn a check red. What isn't paid is arrears: standing -0.15 per miss, +0.05 per
  on-time payment. Consequences by source: the club owner's booker subtracts up to 0.30 x (1 - standing) from bookings,
  residencies and table rights at his room; the operator's account loses 0.10 relationship per miss; the private backer
  costs 0.01 label reputation per miss; the merchant lender, after two misses in a row, takes copies on hand at the dealer
  price ($0.63) against the arrears (no cash, no sale, no chart); with no stock the arrears stand. No separate game-over.
  Principal is financing (cash, not revenue); interest is an expense; early repayment any time. One agreement per label,
  ever. Windfall collection is not built.
- **Ledger:** every disbursement, accrual, payment, miss and seizure is a ledger line; `LedgerAgrees()` recomputes the
  balances from it. Saved as `PlayerSaveData.Backer` (optional; older saves hold none) with the offer and standing refusal.
- **Forecasts:** `NextSolvencyCheck` (now also returns `BackerPayment`) and `ClosureCheckIfNothingChanges` run the same
  `Settle` on a copy, so the forecast and the clock can't disagree. CASH OPTIONS lists the ask (blocked, with what he
  wants, until he'd say yes; speculative, up to his means, after), or the open offer as agreed. UI: BACKER in the books.

Verification:
- **AI-only identity:** `egl-p2-1001-52` matches `ref-c566f24-1001-52` on 84/84 CSVs.
- `BackerChecks` 49/49 on the real clock: sources; hours; a no stands and is free to repeat; an act changes the evidence;
  the objection and its terms; principal not booked as revenue; first payment on the third 1st; forecast = clock
  ($29.49); interest before principal; a miss keeps the check black and costs standing, read by his booker (table rights
  .200 -> .155) and no other room; catching up; early payoff; no second backer; the full budget once a record exists;
  the lender's seizure (176 copies clear $110.75) and a seizure with no stock.
- `SaveLoadRoundTripRunner --integration` passes (`BACKER_ROUNDTRIP ok=True`). `CashPlanningChecks` 30/30 (the fresh-label
  card is no longer empty: its only line is the blocked backer ask). `RoomTableChecks` 49/49.
- `--inspect-slot` prints the agreement and whether its ledger agrees.

## Backer matrix (`SimLogs/ocl-p2-backer.md`, MATRIX=backer, stopped at 27 pairs)

Ex-Musician, every channel worked, New York / LA / Detroit x {eager, replay staggered; discerning one-single} x seeds
1001-1003, `--backer=ask` (meet as soon as he'd say yes, sign the largest terms) against `none`. Seed 1004 was stopped
part-way (direction was already clear). Paired runs share a seed, not a world.

- **Closures 15 -> 8. No label closed with a backer that survived without one.** 27/27 ledgers agree.
- **It mostly buys time.** Of the 7 closures avoided, one (Detroit 1001 replay, +$18) ended solvent; the other six ended
  day 180 with negative cash (-$35 to -$245) and arrears. All 8 remaining closures moved from day 121 to 152. 10 of the
  19 surviving ask runs end open but negative or in arrears, so a longer horizon would close some of them.
- **Arrears are the norm where the label runs red:** every closure-prone cell missed payments (often all three it
  reached), because payments come only from cash above zero. Detroit eager/discerning paid on time.
- New York borrows at 9% ($350, unheard) because the act is signed before anything is recorded; elsewhere $300 at 6%.
- Reading: the backer is an option with a cost, not a rescue. Whether repayment "from cash above zero only" is too soft
  (a red label never pays and loses only standing) is the author's call: the alternative is letting a payment take cash
  below zero, which would make it a closure accelerator.

## Phase 4: paid demos (§7.1), 2026-10-14

Built (`Systems/PlayerDesk.Demos.cs`; UI "PAID DEMOS" on the scene board). Paid production in a hired room: the label
owns no studio.

- **Clients are real acts:** unsigned, on a bill in your town tonight, a seeking or latent prospect, with the money.
  Their money is read, never written: their people's realized scene pay this year and last
  (`LocalSceneRoomService.Realized`), their holdings (`Musician.wealth`), and, for a day-job act (not
  `LocalSceneRoomService.FullTime`), day-job savings of $2 a member a week up to half a year's worth (author decision,
  2026-10-14: the first build read room pay alone and 270 of 271 Detroit act-nights in 120 days could not pay; median $4).
  Less what they've spent on demos with you in the last half-year. A demo can cost at most two-thirds of it.
- **The ask** is in person between sets (1h, from the bill's start). Chance 0.20 + 0.25 seeking (0.10 latent) +
  0.25 x label reputation + 0.15 if your acts' people know theirs + 0.10 if you've heard them - 0.30 x (gross/means
  above a third), clamped 0.05-0.85. A no stands 91 days. (First weights had -0.40 x gross/means, which double-counted
  the affordability gate: 2 bookings from 37 pitches in 90 days.)
- **The quote:** the cheapest free local room (studio sheet rooms, rates and booked-solid calendar; specialist rooms
  excluded) for 3h from 10 a.m. on the first day 3-14 days out when none of the act's people has a bill and you have no
  other demo, + a $20 acetate dub + the $20 producer fee. Detroit: $54 + 20 + 20 = $94.
- **Money:** the room and the dub are the deposit, paid by the client on agreement and **held**, never label cash. On
  delivery the deposit pays the room and the cutting room and the client pays the fee: the label's only income
  (booked as revenue). The label never advances a session.
- **Delivery:** the founder produces it at the room (3h). The act takes an acetate of its own song: no master,
  publishing, option or signing. The label gets a `DemoSample` observation on the act's scouting file ("Your demo
  session", keyed noise like the scouting demo).
- **Cancellations:** the act can't come (broken up, nobody left, left town) -> the room keeps half its fee from their
  deposit, the rest goes back, nothing to the label. The founder misses it -> full deposit back, the label pays the
  room's half, reputation -0.01, that act refuses for a year. Called off a day or more ahead -> free; on the day ->
  the room's half is the label's. Settled the morning after (`OnDayStarted`).
- Saved as `PlayerSaveData.Demos` (optional). CASH OPTIONS lists booked demos (agreed, fee on the day) and tonight's
  first pitchable act (speculative).
- Authored, not sourced: the fee, dub, session length, lead days, savings rate and cap, acceptance weights, the
  two-thirds rule and the room's half. Historical anchor is only the kind of service (custom recording, Rayber demos).

Verification:
- **AI-only identity:** `egl-p4-1001-52` matches `ref-c566f24-1001-52` on 84/84 CSVs.
- `DemoChecks` 30/30 on the real clock (Detroit): a real client appears (day 0 after savings; day 22 before); quote at
  the room's own rate, in the $35-100 reference; the pitch costs an hour and no money; a no stands; the deposit is held,
  not cash, and comes off the client's means; CASH OPTIONS lists the fee; not before its day; delivery pays only the
  fee as revenue, takes 3h, leaves a demo sample and no signing; a missed session (refund, room's half, reputation,
  a year's refusal); an act that broke up (half the room from its deposit, nothing owed); free and same-day cancels.
- `SaveLoadRoundTripRunner --integration` passes (`DEMO_ROUNDTRIP ok=True`). CashPlanning 30/30, Backer 49/49,
  RoomTable 49/49.

## Demo matrix (`SimLogs/ocl-p4-demos.md`, MATRIX=demo, 16 runs, 180 days)

Discerning one-single, every channel worked, `--demos=work` (produce each booked demo; pitch up to two of tonight's
acts after the day's work) against `none`, New York / LA / Detroit / Memphis x seeds 1001-1002.

- **$62/month in fees on average** (147 delivered of 152 booked from 498 pitches; 4 missed). New York $54-98/month =
  **31-56% of its $175 overhead**, inside the §7.1 30-60% diagnostic. Detroit $30-78 (38-97% of $80), Memphis $44-71
  (66-106% of $67), LA $61 (49% of $125).
- **It moves the label's cash a lot:** final cash rose in all 8 pairs (New York -267 -> +32 and -213 -> +303). Units
  were not hurt (stream noise either way). No closures in either arm.
- **Supply binds, not demand:** most unsigned act-nights can't pay (duos and solos hold too little), many aren't on
  yet when pitched, and refusals stand. Every client is a real act with real money.
- **Cost in time:** about 3.3 pitches (1h each) per booking plus the 3h session: roughly $3 an hour of the founder's
  day, worked every evening the probe could.
- Reading: demos do not need guaranteed clients to matter; worked every night, they cover about half of New York's
  overhead. Whether that is too generous an escape (directive: "they need not ... rescue every lock") is the author's
  call; the levers are the fee, the savings cap and the acceptance weights, not an income ceiling.

## Phase 5: shop the master (§5), 2026-10-14

Built (`Systems/PlayerDesk.MasterDeals.cs`; UI "SHOP THE MASTER" in the books). Author decision (2026-10-14): three
contracts, **Lease, Sale, and Sale with the act**, in one deal state machine. They replace the old cash-only
`SellMaster`/`LeaseMaster` buttons, which had no buyer behind them (the title just went dark). Legacy sold/leased ids in
older saves still read as out; nothing new writes them.

- **Buyers are real AI labels:** active, the record's genre in their lists (own sound) or the same family (near it, bar
  +0.04), and able to pay an advance today (`CanAffordToSign`). Their offices are their home towns.
- **Access routes** (§5.1). A *call* from the office (1h, 9-5, toll $0.50 + $0.004/mile) gets public answers only: not
  our kind of record, not buying, or "send a tape" plus an appointment 3-7 days out when the office is in driving
  range. A repeat call inside 28 days is refused free. A *posted tape* (1h, $5 dub + $1.50 postage) is answered 14-28
  days later, if anyone plays it: 90% after a call asked for it, 40% unsolicited. No reply means another tape waits 91
  days. A *visit* (2h, in their town, in their hours, with an acetate, or a pressed copy for a released title) is
  always heard on an appointment day. A walk-in is seen in half the weeks (a fixed draw per buyer and week). Driving
  rules apply, so a far office can only be called or posted.
- **The ear** reads hook x0.60 + production x0.40, plus the buyer's own noise (the AI's scouting-ability range,
  x1.4 off a tape), plus regional evidence for a released title (+0.04 per breakout market up to +0.12, plus copies
  sold/5,000 up to +0.08). The noise is a fixed draw per buyer and title, so **a pass stands until the evidence
  changes** (the record comes out, or breaks in another market). A different buyer is a different ear. Bars by tier:
  Major .66, MidTier .60, Independent .54, Small/Boutique .50. Answers are words ("I hear a record"), never the score.
- **Contracts** (authored; §5.2's $150-600 is the lease reference):
  - *Lease:* exclusive, national, 52 weeks from the buyer's release (or takeover). An unreleased master must be out
    within 42 days of signing, or the rights revert and the advance is kept. The advance is $150-600 by interest and
    tier; on a breakout it is the larger of that and $150 per market + $0.10 per copy sold, capped at $1,500. It is
    against 7% of list (8% when the buyer likes it a lot) per payable unit, recouped first, with quarterly statements.
    The act stays, and the player owes it its royalty on the buyer's units, settled on each statement against its
    advance. At term the record comes home with its chart history; the buyer's shop copies are withdrawn.
  - *Sale:* the master is the buyer's for good, at 2.5x the lease advance, not recoupable, no royalty. Offered when the
    buyer likes it a lot, or on a breakout. The buyer pays the act's royalty on its sales directly, at the act's rate.
    The player's advance to the act recoups only from royalties the player pays.
  - *Sale with the act:* released titles with a breakout, offered by an Independent or bigger with roster room **and
    scene access to the act** (the same `SceneRecruitmentService.CanAccess` test every rival signing needs). The master
    is sold and the act signs with the buyer on that label's own terms (`RivalOfferTerms`). The player keeps a
    producer's 3% of list on that master, against 1.5x the lease advance, with quarterly statements. The act's
    unrecouped balance with the player is left behind.
  - Payable units are the units the buyer's own settlement bills (AI returns aren't modelled). Publishing is never part
    of a master deal (that is 5A's job).
- **Stock** (§5.2 "existing stock, pressings in flight"): on signing the buyer takes the player's pressed copies (office,
  promo and shop lots) at $0.14 each, reimburses runs at the plant, and its salesmen take over open calls and
  pre-orders. There is no player sell-off: the title leaves every player channel that day. That is what prevents double
  sales: the trunk never sells a title the buyer is billing.
- **The buyer releases it itself** (§5.4): `CompetitorManager.ReleaseLicensedPlayerMaster` follows the AI's own single
  path. The buyer funds the pressing (its COGS) and a launch campaign on its own marketing rule, sized by what it heard,
  with no production cost (the player paid for the session). The record is tagged `Record.playerMasterDealId`. A
  regional pickup instead moves the live record (`TransferRecordToLabel`): one runtime record per master throughout, so
  there is never a second chart entry. AI competitor loops can't re-release it, because a lease's act isn't on their
  roster.
- **Settlement:** one branch in the AI weekly settlement, taken only for tagged records (`SettleMasterDealWeek`),
  replaces the royalty leg. Lease: the buyer bears the royalty after recoupment and no artist royalty. Sale: the act's
  royalty at its rate, no recoupment. With the act: the buyer's own artist terms, plus the producer royalty after
  recoupment. Accrued money is paid on the quarterly statement at dawn. Every advance, stock payment, week's units,
  recoupment, statement and act settlement is a ledger line, and `LedgerAgrees()` recomputes the balances.
- **Regional pickup** (§5.3): when a released single's breakout count changes, the best-fitting buyer that would hear a
  record may phone. The chance is 0.35 + 0.15 per extra market + 0.30 x interest, capped at 0.85, on a fixed draw per
  evidence: likely, not guaranteed, and one call per change of evidence. The call is an ordinary offer, open 14 days
  (letters 21).
- **Failure paths:** a buyer that goes under, or misses the release deadline, returns an unreleased master to the shelf
  (advance kept). A lease reverts early if its buyer folds. A record the buyer's cull already retired comes back as
  rights only ("it had already run its course"); reissues aren't built.
- CASH OPTIONS lists open offers (agreed, signable from the office or at theirs) and each shoppable title (speculative,
  from $150, a call, tape or visit away). Saved as `PlayerSaveData.MasterDeals` (optional). The buyer's record travels
  in the world save with its tag; `--inspect-slot` prints every deal and whether its ledger agrees.
- **Shared-world effects** in player-present games (§0.1, intended): buyer cash (advances, stock, royalties, campaign),
  buyer catalogue and chart entries, buyer roster (sale with the act), and the AI RNG stream (the buyer's release
  promotion draws). All values above are authored, not sourced. The historical anchors are only the kinds of transaction
  (the Miracles lease to End, Tamla's regional break, and the UA pickup with Marv Johnson's signing).

Verification:
- **AI-only identity:** `egl-p5-1001-52` and, on the final DLL, `egl-p5b-1001-52` match `ref-c566f24-1001-52` on
  84/84 CSVs, byte for byte.
- `SaveLoadRoundTripRunner --integration --weeks=26` passes (new `MASTER_SHOP_ROUNDTRIP ok=True`: a lease with its
  ledger, a held master, an offer with act terms, a pass, a posted tape). CashPlanning 30/30, Backer 49/49, RoomTable
  49/49, Demo 30/30. Note: at `--weeks=4` the backer row fails because the perturbation week crosses a 1st and settles
  the staged loan object in place; that is a fixture artifact, and 26 weeks is the documented setting.
- `MasterDealChecks` 69/69 on the real clock (Detroit, a Soul act). The gate covers:
  - Buyers are real labels, ranked; calls (hours, toll, public answers, no repeat); posted tapes (cost, timing, one in
    flight, the letter); the no-copy refusal.
  - An appointment visit: Atlantic (New York) offers a $600 lease at 8% and a $1,500 sale for the strong side and passes
    on the weak one. The pass stands, free. CASH OPTIONS shows the offer.
  - The lease: $600 comes out of Atlantic's cash and the master is held. Atlantic releases it on day 21 (tagged, one
    chart entry, not in the player's catalogue) and bills 12,475 units in four weeks ($978.05 at 7.8c, $600 recouped).
    The buyer pays the act nothing. The statement pays $378.05 and settles the act's $550.15 ($100 against its advance,
    $450.15 paid). Reversion brings the same record home.
  - A missed release deadline returns the master, advance kept. A breakout brings at most one call per change of
    evidence (Baltimore Sound phoned).
  - The pickup lease takes 170 copies at cost, a shop lot and the open calls. The buyer folding reverts the lease.
  - Sale with the act goes to Mercury, which works the act's scene (Atlantic doesn't, so it never offered it), and moves
    the act onto Mercury's roster. Hand-settled lease and sale weeks check recoupment and who pays the act. Every ledger
    agrees.
- Found and fixed by the gate: two deals on one title in one day shared an id, so the buyer's settlement booked against
  the dead lease. Ids now carry a saved running ordinal.
- **Not yet run:** an opening-probe matrix with a master-shopping policy (how often an opening finds a buyer, and what
  it is worth against overhead).

## Phase 5A: place a song (§7.2), 2026-10-14

Built (`Systems/PlayerDesk.SongPlacement.cs`; UI "PLACE A SONG" in the books, after SHOP THE MASTER; seams in
`CompetitorManager.cs`, `CompositionCatalogService.SongsControlledBy`, `Data/Record.cs`).

- **What can be placed:** a composition whose publishing the player's label controls (LabelAffiliate/LabelBuyout with the
  label as controller, i.e. its own act's song under label-owned publishing), with a master the player cut of it (shelf,
  released, held or out on a master deal) to play from. Songs the act kept, commissioned songs, standards and covers are
  refused. Publishing is separate from the master: a sold or leased master leaves its song placeable.
- **Buyers are real AI labels** with an act on the roster in the song's sound (its genre = own sound; same family = near,
  bar +0.04) that can find a $25 advance. A route-to-route copy of the master-shopping contacts: call (1h, toll, public
  answers, appointment if in driving range, no repeat inside 28 days), posted tape ($5 + $1.50, answered in 14-28 days, 90%
  solicited / 40% not, 91-day quiet), visit (2h, their town and hours, an acetate or a pressed copy). The walk-in front desk
  is shared with master visits.
- **The ear** reads the song, not the recording: craft score x0.5 + composition hook x0.5, with the buyer's fixed noise
  (x1.4 off a tape) plus the evidence of the player's own record of it if it is out (breakouts, copies). A pass stands until
  that evidence changes. Bars: Major .62, MidTier .58, Independent .54, smaller .50. Answers are words.
- **Contract (authored):** an advance of $25-100 by interest and tier, paid from the buyer's cash on signing; a 182-day hold
  during which the song can't be pitched elsewhere; no cut in the hold = lapse, advance kept. After a cut the song may be
  pitched to other labels again, never twice to the same one.
- **The cut** is injected in `GenerateRecordFromArtist` after the AI's own `ChooseMaterial`: when the label holds a song
  its releasing act can sing and the act was about to record an outside song (not `ArtistWritten`/co-written), the placed
  song is cut instead (`BuildProfessionalForSong`, or `BuildCoverForSong` when the player's record of it is out, so it is a
  cover with `originalRecordId` = the player's record and the trades notice sees it). `StampPlacedCut` restores the song's
  rights after `Apply` (so `PublishingCaptureService` can't take the player's song), gives the record the song's title and
  the `playerPlacementId` tag. The cut counts only once the record is on the chart; an AI single dropped for want of cash
  leaves the song held.
- **Settlement:** no new money. The song's income is the existing publishing slice (`PublishingShareOfGross`, 11% of the
  buyer's retail gross; cut to the 2c mechanical since, see below) that live routing already transfers to the controlling label. For a tagged record the transfer
  branch calls `SettlePlacementWeek`: the advance recoups first (that part stays in the buyer's net, since it paid the
  advance), and the rest accrues on the placement instead of the week's `weeklyPublishingReceipts`, paid on a quarterly
  statement from the cut's release. No 2c mechanical is charged (an AI record's composition cost is that slice), so nothing
  is charged twice. Under label-owned publishing the label keeps the writer's half too, the same convention as its own
  sales and the AI routing, so no separate writer line is paid. With `--disable-publishing-routing` a placement earns only
  its advance. When the record leaves the chart or the buyer folds, a final statement settles and the placement ends.
- CASH OPTIONS: open offers (agreed), statements owed (owed, on the statement date), placeable songs (speculative, from
  $25). Saved as `PlayerSaveData.SongPlacements` (optional). `--inspect-slot` prints placements and ledger agreement.
- Not built: a route to a cash-holding publisher/plugger (the catalogue's publishers hold no cash in the sim, so a
  publisher deal would be money from nowhere); writer payouts on placements (see above); a probe matrix.

Verification:
- **AI-only identity:** `egl-p5a-1001-52` matches `ref-c566f24-1001-52` on 84/84 CSVs, byte for byte.
- `PlacementChecks` 57/57 on the real clock (Detroit act, Mercury in Chicago): what can and can't be placed; real buyers;
  calls, tape, visit, refusals free; the strong song offered $100 and the weak one passed (the pass stands); CASH OPTIONS;
  signing pays from Mercury's cash and holds the song; a self-writing act's single keeps to its own song; Mercury's act cut
  it 9 days after signing in a real AI session, tagged, the player's publishing intact, not a cover; units accrue at the
  2c mechanical (9.8c a copy at the original 11% slice), the advance recoups first, nothing reaches weekly receipts, the statement pays the rest;
  hand-settled recoupment; a folded buyer ends it with a final statement; a dropped single leaves the hold; an uncut hold
  lapses with the advance kept; a cut of a released song is a cover of the player's record; every ledger agrees.
- `SaveLoadRoundTripRunner --integration --weeks=26` passes (new `SONG_PLACEMENT_ROUNDTRIP ok=True`). CashPlanning 30/30,
  Backer 49/49, RoomTable 49/49, Demo 30/30, MasterDeal 69/69.

**Placed songs earn the period mechanical (author question, 2026-10-14).** The gate's fixture song (craft .95) became a
Mercury hit: 263,224 copies in four weeks. At the AI economy's 11% composition pool that paid the player $25,770, about
9.8c a copy. The 1909 Act's compulsory rate was 2c a song per copy sold, unchanged until 1978, and the game already charges
the player exactly that (`MechanicalRoyaltyService.RatePerCopy`) when it sells someone else's song. So the 11% was about
five times the period rate, and believability was the issue, not that a hit pays.

Now, when the song's controller is the player's label, the transfer out of an AI record's publishing pool is cut to 2c per
unit (`CompetitorManager`, the transfer branch). The rest of the pool stays in the buyer's net, so no money appears or
vanishes. This covers placements and organic AI covers of a player-controlled song alike. AI-to-AI routing is unchanged.
The same hit now pays about $5,265. Still not modelled: performance (BMI/ASCAP) income on airplay, which was real money
for a hit's publisher, and paying the writer's half to the act (the game's label-owned-publishing convention keeps it
with the label, as on the label's own sales). `PlacementChecks` 57/57 with the per-copy check now at 2c.

## Phase 5B: gig pay belongs to the performers (F5), 2026-10-14

Built:
- **Booking commission off the played set.** When a set the player's label booked (a §6.3 night or a residency night) is
  played and its fee paid, `LocalSceneRoomService.TakeBookingCommission` sets 10% of the act's fee on the booking, and the
  band splits only the rest (`Resolve`'s fee loop). So the commission is deducted once from the act's gross, and no
  second copy of the fee exists. The label collects it the next morning (`CollectBookingCommissions`, dawn) and gets a note.
  A set that never plays, whether it was cancelled, not billed or fell through, pays nothing. Saved on `SceneBooking`
  (`Residency`, `Commission`, `CommissionCollected`; optional, older saves read zero).
- **A residency is its nights.** `BookRoad` still needs the booker's yes at a room the act has played twice. It now writes
  dated `SceneBooking`s: the room's first 3 open nights in each calendar week, from the first night past the announced
  calendar (or after the act's last booked night there). Each night is billed under every ordinary rule, so it can fall
  through. `ResidencyWeeklyCommission` is now only the estimate shown to the player.
- **Tours pay the label nothing.** The flat $30/week is gone. A tour is road load and momentum; the label books no dates
  it could bill. The weekly $ inflow in `RunRoadBookings` is removed for residencies too.
- CASH OPTIONS lists the label's booked nights by act and room (accepted, with the estimated commission, cash the morning
  after the first night), from `BookedNightsAhead`. Booker and residency wording, and the roster-card tooltips, say the
  door is the band's.
- **Legacy:** a residency booked before this change has no nights behind it, so it keeps its road load and pays nothing
  further (the same treatment as pre-room residencies).
- Authored, not sourced: the 10% rate (§6.3 says it is not a documented Robey rate) and 3 nights a week.
- Not changed: `MemberLiveIncome`/`SceneLiveEconomics.MemberLiveIncome` reads realized `FeeShare`, which is now net of
  commission. So the members' estimated live income falls by exactly what the label took, and nothing is counted twice.
- Observation, not changed: community halls and roadhouses open noon-3 p.m., so their booker's window (from 2 p.m.) is one
  hour.

Verification:
- **AI-only identity:** `egl-p5b-1001-52` matches `ref-c566f24-1001-52` on 84/84 CSVs, byte for byte. The fee split
  subtracts 0 when no bookings exist, which is exact in floating point.
- `GigPayChecks` 22/22 on the real clock (Detroit). A tour adds momentum and no label income, and CASH OPTIONS shows no
  tour money. A residency (won on the 6th ask) wrote 24 dated nights, on open nights, at most 3 a week, past the calendar.
  CASH OPTIONS lists them. Played nights: fees $14.38 and $15.12, commissions $1.44 and $1.51. The band's realized pay rose
  by fee less commission, and label cash rose by the commission at dawn. A forced fall-through was cancelled with no
  commission.
- `SaveLoadRoundTripRunner --integration --weeks=26` passes (the booking round-trip now covers the new fields).
  CashPlanning 30/30, Placement 57/57, RoomTable 49/49, Backer 49/49, Demo 30/30, MasterDeal 69/69, BandLife 43/43.

## Gig matrix: residency with the flat inflow removed (`SimLogs/ocl-p5b-gig.md`, MATRIX=gig, 48 runs, 180 days)

ExMusician, every channel worked, seeds 1001-1003 x New York / LA / Detroit / Memphis x {discerning one-single batch,
eager two-single staggered} x {no road, residency}. Paired runs share a seed, not a world.

- **No new locks.** Closures are 6/24 in both arms, and they are the same six cells: eager staggered in New York and LA,
  all on day 121, as in fair3. The one-single discerning opening closed 0 of 24. Sensible openings stay viable with no
  flat live income.
- **Residencies are earned and small.** 22 of 24 residency runs won one (30 yes from 81 asks). 536 nights were booked
  and 474 played (62 fell through). Commission per run had a median of $17 (range $1-$154). That is about **$2.45 per
  residency week** (median), against $67-175 of monthly overhead. It opens the room, the following and table nights;
  it doesn't carry the label.
- Residency was ahead on final cash in 18/24 pairs (median +$27), partly through commission and partly through more played
  nights in the home town (units up in 15/24, within stream noise).
- 0 probe failures, 0 invariant violations.
