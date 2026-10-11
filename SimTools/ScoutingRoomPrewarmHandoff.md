# Scouting continuation: room history before day one (prewarm)

October 10, 2026. Continues the contract-outlook snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. Primary gameplay sources and other checkouts are unchanged. Changes are uncommitted. **The prewarm ships off by default** pending this review.

## Why

The contract outlook reads an act's working life from the room ledger (`LocalSceneRoomService.ObserveActWork`). At Jan 1 1960 the ledger is empty, so acts that have notionally gigged for years read as brand new: in Nashville (seed 1001), mean work was 0.02 and 5 of 241 acts were working.

## What it does

`LocalSceneRoomService.Prewarm` (new partial `Systems/LocalSceneRoomService.Prewarm.cs`) runs at world creation, just before the first `EnsureCalendar`. It runs the room owner's own `Schedule` over the 52 weeks before the start date. The rules are the same: rooms, open nights, slots, family fit, regular priority, the keyed per-term draw, one room per person per day, and house-band picks.

**Stand-in for the past.** The world as it stands on the start date stands in for the recent past: who is in town, lineups, and the rooms' families. Participation rows start at week 0 only because that is when they were observed, not when the acts formed. One honest limit applies: an act does not play a year before its `formedYear`.

**What a historical set settles.** Each set settles exactly one fact: `SceneRoomStanding.PriorShows`. Nothing else is written:
- no performance outcome;
- no attendance, receipts or fee;
- no stage hours or work account, so no fatigue, growth or member wealth;
- no contacts, house-band standing or repertoire influence;
- no `CompletedPerformances`.

The historical bills are discarded. Term engagements are also discarded, because a term ends at the start date (the term index now uses floor division for negative days; days ≥ 0 are unchanged). So no historical engagement carries into the live calendar.

**Who sees it.** History is read only by:
- `ObserveActWork` (the contract outlook's lifetime shows);
- `ShowsAt` (the booker's "booking history" answer).

**Kept out of the live draw.** The live bill's expected audience still counts live `Shows` only. If history counted toward audience, it would change attendance and fees, which the directive rules out "merely for history".

**One pass only.** It runs only on a fresh calendar: no scheduled week, no standing, no bills, no engagements, `PriorWeeks` 0. A loaded, running or legacy world never gains invented history.

**RNG.** Draws are the room owner's keyed draws. No global or population RNG is consumed (checked).

## Flags (`Systems/LocalScenes.cs`)

- `--enable-scene-room-prewarm` turns it on. It requires scene rooms and conflicts with the disable flag.
- `--disable-scene-room-prewarm` is the explicit off control.
- `--scene-room-prewarm-weeks=N` sets the length (1–520, default 52).
- Default is **off**. Turning it on by default is a one-line change (`RoomPrewarm = Rooms && !disablePrewarm`).

## Save/restore

- `SceneRoomStanding.PriorShows` and `SceneRoomSaveData.PriorWeeks` are optional JSON fields. Older saves restore with zero history. The room schema stays 1 and the save envelope stays v13.
- Validation rejects negative `PriorWeeks`, `PriorShows` or `Shows` before mutation.
- Prewarmed rows carry a negative `LastDay` (dated before the start). Nothing reads `LastDay` for standing.

## AI-behavior impact: none, demonstrated

- **52-week ChartAuditRunner, seed 1001.** All 84 output CSVs are byte-identical (SHA-256) to a reference run on the pre-change build, in both configurations:
  - prewarm off on the new build;
  - prewarm on (the log confirms `pre-rolled 52 weeks of room history -- 910 acts, 37854 prior shows`).

  Covered: concentration, deal ledger, labor market, appointments, population events, genre market, settlement, and the rest.
- **In-process A/B (`--scene-room-prewarm-check`).** After a pre-roll, the opening live calendar matches the control world exactly. That covers bills, sets, house-band picks, expected audiences, engagements and work accounts.
- **Act fields AI recruitment reads.** Reputation, momentum, career state, label, prospect status, member fatigue and skill are unchanged.
- **By construction, AI cannot see it.** No AI path reads `PriorShows`. `HasFutureCommitment`, which gates AI-sponsored relocation, sees no carried engagement. So the paired-seed long-run economic acceptance is not triggered, and reputation and momentum are not written.

## Measured effect (Nashville, new label, week 0)

| | seed 1001 off | 13 wk | 26 wk | **52 wk** | 104 wk | seed 2002 off | **2002 52 wk** |
|---|---|---|---|---|---|---|---|
| acts with room history | 0 | 5 | 10 | **21** | 41 | 0 | **20** |
| mean work | 0.02 | 0.03 | 0.04 | **0.05** | 0.07 | 0.02 | **0.04** |
| working (≥0.45) | 5 | 6 | 6 | **6** | 6 | 5 | **5** |
| Pushover→Pushover | 114 | 112 | 109 | **103** | 96 | 116 | **108** |
| Pushover→Firm | 36 | 38 | 41 | **47** | 54 | 34 | **42** |

Nationally the 52-week pre-roll gives 910 (seed 1001) and 935 (seed 2002) acts a room history. The median is 26 prior shows; nearly all of them pass the outlook's 12-show saturation. It costs about 4.1 s at world creation, after caching the start-date cast per room and year (11 s uncached). Saves grow by about 1,000 standing rows (an estimate, not measured).

## The finding that matters more

The prewarm is honest and inert, but it cannot make most acts look established. The **room owner's capacity and rotation** are the binding constraint, not the empty ledger.

Nashville has 5 performance rooms with 2 slots each, open 1–5 nights, against 19–122 eligible local acts per room. The booking draw is keyed per 91-day term (`engagement|room|term|artist`), so each room books the same ~2 acts every night for a quarter. A year of history therefore reaches ~5 new acts per room per year, and 21 of 241 acts in all.

Showing "working" acts on day one needs one or more of:
- more slots or rooms;
- a rotation that spreads nights across more acts;
- an outlook work measure that is not room-ledger-only.

All three change the live economy. Room bookings feed stage hours (fatigue and growth), fees (member wealth) and `HasFutureCommitment` (AI relocation). So they need the paired-seed long-run acceptance. They are deliberately not in this slice.

## Recommendation

Safe to enable by default: player-side only, proven AI-inert over 52 weeks, legacy-safe, about 4 s at startup. The payoff is modest (+6 to +11 acts that talk terms in Nashville). Decide it together with the capacity question above.

If the design wants history to count toward a room's draw (audience and fees), make that a separate, economically validated change.

## Verification

- **Build:** zero warnings and zero errors.
- **New check:** `--scene-room-prewarm-check` passes 29 checks at seeds 1001 and 2002. It covers:
  - flag conventions;
  - no global or population RNG;
  - prior shows only;
  - no bills, engagements, pay, hours, house-band standing or performances;
  - performance rooms only;
  - the formed-year gate and family fit;
  - AI-read act fields untouched;
  - the live calendar equal to the control;
  - readers including the history;
  - determinism;
  - no second pre-roll and no legacy pre-roll;
  - older JSON without the fields;
  - a compressed save/load round trip;
  - two malformed saves rejected before mutation;
  - the off control;
  - the world restored.
- **Regression:** `SimLogs/room-prewarm-regression.sh` (a copy of `contract-outlook-regression.sh` that takes extra flags) passes all 22 checks twice, with the prewarm off (default) and on:
  - contract outlook 63 ×2, session promise 65 ×2, contract continuity 50 ×2;
  - offer planning 27, business 34, experience 131, terms 42, meeting 43, appointment 59;
  - opening 544, commitment planning 19, founding history 40, audience 37, comparison 32, market history 9;
  - listener 40, hired listener 43, scene rooms 83, scene information 43.

  The appointment check prints `SCOUTING_APPOINTMENT_OK`, which the inherited grep pattern does not match; its logs show it passing. The market-history check's normalized world hash differs between on and off, as expected: the world snapshot includes room standing.
- **Not run:** the rendered UI checks (no UI changed; the booker's history answer only reports a larger count) and a human playthrough.

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-prewarm-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --enable-scene-room-prewarm --scouting-contract-outlook-survey=nashville
--headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --seed=1001 --enable-scene-room-prewarm --run=prewarm-on-1001-52
sh SimLogs/room-prewarm-regression.sh on --enable-scene-room-prewarm
```

## Portable delta

`SimTools/ScoutingRoomPrewarm.patch` is a nine-file delta (seven changed, two new) against the contract-outlook snapshot. Baselines are in `SimLogs/room-prewarm-baseline/*.orig`. `git apply` on the reconstructed baseline reproduces every file byte for byte, line endings included. Several touched files are LF or mixed, not CRLF, and each edit kept the endings it found.

Markers and hashes are in `SimLogs/room-prewarm-validation.json`.
