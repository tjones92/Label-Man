# Scouting continuation: bills that rotate

October 11, 2026. Continues the room-prewarm snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. Primary gameplay sources and other checkouts are unchanged. Changes are uncommitted. **Rotation is on by default** with scene rooms; `--disable-scene-room-rotation` restores the old rule byte for byte.

## Why

The prewarm slice found that the room booker drew its cast once per 91-day term (`engagement|room|term|artist`). Every room booked the same two acts every night for a quarter. A Nashville club with 122 eligible acts used 6 of them in a year. That is not a scene, and it gives a scouting player nothing to see.

## What the record says

Sources are cited in the rotation partial's header comment.

- **Bills had several acts.** A Cavern evening on 25 July 1961 carried four groups: the Blue Genes, the Beatles, Gerry and the Pacemakers and the Remo Four. A Liverpool Jazz Society afternoon on 15 March 1961 had the Beatles, Gerry and the Pacemakers and Rory Storm's band alternating over five hours. A 1961 all-nighter had six acts.
- **Casts changed nightly.** One Cavern lunchtime week ran the Beatles on Monday, Wednesday and Friday, the Big Three on Tuesday, and Derry and the Seniors on Thursday.
- **Favoured acts worked constantly, beside a changing cast.** The Beatles played at least 155 lunchtime and 125 evening Cavern shows from 1961 to 1963. They played 31 Aintree Institute dates in a year, and sometimes two or three venues in a day. Mersey Beat counts roughly 350 groups regularly working Merseyside.
- **Rooms kept residents.** The King Kasuals had a residency at Club Del Morocco, Nashville, in 1962. Austin's Skyline Club had a house band backing touring singers. Café Wha? had an MC and regulars on the afternoon shift.

I found no US source giving club-by-club booking formats for 1960s Nashville or Texas. The US side rests on residencies, house bands and hootenanny practice, not on dated bills.

## What it does

`Systems/LocalSceneRoomService.Rotation.cs`, plus small hooks in `Schedule`:

- **Bill size by kind of room.** Clubs, roadhouses, coffeehouses and community programs book three acts a night. A listening (supper) room books two: a featured act and one other. A funded program still adds one set; when that runs past the room's usual close, the bill's `EndHour` extends.
- **Regulars.** Each performance room names up to two regulars (`RegularsPerRoom`). Regulars are the booker's strongest working residents by booking weight, and the room's own history with an act counts. They hold the existing term engagement and are re-chosen each term, so a rising act can take a lapsed regular's place.
  - One regular closes each night, so a regular is on every bill once a room has one.
  - Regulars can also take support slots.
  - Trade listening appointments hold no residencies.
- **A fresh support cast every night.** The draw is a weighted sample keyed to room and night (Efraimidis–Spirakis on the room owner's keyed draw). The weight is `(1 + 2·livePerformance + 2·reputation) × (1 + 6·min(1, shows here / 4))`. Standing compounds, so a room builds a core of acts it keeps rebooking, and new faces still get nights.
- **Running order.** The bill fills from the closing set back, then runs back to back from the opening hour. Roles are opening, middle and closing.
- **The crowd is the room's.** Past two acts, more sets don't draw more people. The acts' standing counts as their average scaled to two acts, which keeps the live-cal fill. `SceneLiveEconomics.ActsPerBill` replaces the fixed two-act door split in a member's expected night.
- **Unchanged:**
  - one room per person per day;
  - no global or population RNG;
  - the live calendar counts live shows only, so the prewarm stays inert;
  - house bands, pay terms and the work ledger.

## Measured effect

**One club, 12 weeks of the room clock** (`--scene-room-rotation-check`):

| Nashville club | old rule | rotation, seed 1001 | rotation, seed 2002 |
|---|---|---|---|
| distinct acts | 6 in a year (pre-roll) | 89 in 12 weeks | 84 in 12 weeks |
| distinct line-ups / nights | ~1 per term | 60 / 60 | 60 / 60 |
| nights for the two regulars | every night | 32, 28 | 31, 30 |
| support acts rebooked ≥3× (from the top-10 list) | – | ≥8 | ≥8 |

**A year, nationally** (52-week ChartAuditRunner with `--scene-room-audit`, 1960):

| | off s1001 | on s1001 | off s2002 | on s2002 |
|---|---|---|---|---|
| acts that worked a room | 1,182 | 5,369 | 1,168 | 5,329 |
| median stage hours per act | 104 | 16 | 104 | 16 |
| sets | 42,276 | 57,831 | 41,720 | 57,301 |
| fill | 0.78 | 0.70 | 0.78 | 0.70 |
| act pay | $834k | $803k | $841k | $794k |

Fill of 0.70 is inside live-cal-v1's measured 0.69–0.75. Roadhouse pay rises about 43%, because its flat fee is per player per set. Club pay falls about 12%.

**Contract outlook, new Nashville label, week 0, prewarm on** (`--scouting-contract-outlook-survey=nashville`):

| | old rule | rotation |
|---|---|---|
| unsigned acts with room history | 21 / 241 | 152 / 241 |
| booked in the coming days | 8 | 35 |
| club acts with history | 6 | 83 |
| mean work | 0.05 | 0.11 |
| Pushover→Firm | 47 | 59 |

With the prewarm off, rotation alone moves booked acts from 8 to 35. The outlook's "working" bar (≥0.45) is still met by only 8, because it rewards an engagement (+0.40) and rotation gives engagements to two acts per room. Whether the outlook should read rotating work more generously is a separate calibration.

## AI-behavior impact: real, through house bands

- **Off control is inert.** With `--disable-scene-room-rotation`, all 84 CSVs of a 52-week seed-1001 run hash identical to `prewarm-ref-1001-52`. This was checked twice, before and after the final weights.
- **On is not inert, by design.** It changes 70 of 84 CSVs at 52 weeks.
- **The channel is house bands, and only house bands.** With `--disable-scene-house-bands` on both arms, rotation on and off produce 82 of 84 identical CSVs. The two that differ are cash in `label-finance` and `label-operating-target-events`: a few dollars on two labels, with no decision change. Room growth credit is not a within-year channel; on and off are identical with it disabled.
  - Three acts a night means more backed sets, spread over many more solo singers.
  - More backed sets feed contact links (`ContactNetworkService.Link`), repertoire influence (`RepertoireInfluenceService.OnBacked`) and backers' hours and pay. The contact-network systems built on top of those read them.
- **52 weeks:** signings fell 141→134 (s1001) and 122→110 (s2002), and first-chart events fell 262→249 and 269→258.

**Decade, paired seeds** (`--weeks=522 --enable-genre-market-v2 --enable-artist-population-lifecycle --calibration`). The control is `ref-scoutsnap-<seed>-522`: the same build with `--disable-scene-room-rotation`, byte-identical to the base and reusable as the snapshot's reference.

| 1960–69 | s1001 control | s1001 rotation | s2002 control | s2002 rotation |
|---|---|---|---|---|
| genre-share sumAbsErr | 406.9 | 465.7 (+58.8) | 364.3 | 365.2 (+0.9) |
| year-end slot error | 547 | 587 (+40) | 511 | 519 (+8) |
| signings | 21,080 | 21,342 (+1.2%) | 19,816 | 19,995 (+0.9%) |
| drops | 16,263 | 16,472 | 14,618 | 15,064 |
| first-chart events | 636 | 660 (+3.8%) | 690 | 653 (−5.4%) |
| album unit share 1969 | 55.4 | 55.4 | 55.4 | 55.4 |
| owner-Major share 1969 | 43.0 | 46.2 | 38.9 | 37.0 |
| acts in scope 1968 | 20,858 | 20,788 | 21,413 | 21,357 |

**Verdict: one consistent effect, Soul; the rest is noise.**

- **The 52-week signing dip doesn't persist.** Over the decade, signings are +1.2% and +0.9%.
- **Noise, on these two seeds:**
  - first-chart events, which go +3.8% on one seed and −5.4% on the other;
  - owner-Major share;
  - population.
  - Album share and acts in scope barely move.
- **Mean of two seeds:** genre-share error +29.9, slot error +24. Seed 1001 carries almost all of it; seed 2002 is neutral (+0.9), with Country improving by 10.1.
- **Soul rises on both seeds:** share error +28.4 and +15.9.
  - Seed 1001: Soul's share is 1–5 points higher in every year.
  - Seed 2002: it is 2–5 points higher from 1963 to 1967 and lower in 1960 and 1969.
  - This is the house-band channel. R&B clubs now run three acts a night, and many of them are solo singers backed by local players. More backed sets mean more repertoire carried and more club contacts for R&B records.
- **Options:**
  1. Accept the change and re-trim Soul's genre baseline. The `sqrt(target/current)` rule applies, since share goes as baseline squared.
  2. Hold house-band volume near its old level, for example by backing only the closing set or capping backed sets per night.
  3. Leave it and treat Soul's drift as the cost of a realistic scene.

  None of these is applied here. This is a calibration decision, not a mechanism defect.

## Flags (`Systems/LocalScenes.cs`)

- **On by default** with scene rooms.
- `--disable-scene-room-rotation` is the control; it restores the two-act, per-term bill exactly.
- `--enable-scene-room-rotation` requires rooms and conflicts with the disable flag.

## Save/restore

- No schema change.
- Bills, engagements and standing keep their shapes.
- An older save resumes under whichever rule is configured. Its existing bills stand until they are played; new weeks are booked by the configured rule.

## Verification

- **Build:** zero warnings and zero errors.
- **New check:** `--scene-room-rotation-check` passes 836 checks at seeds 1001 and 2002. Most of them are per-bill invariants. It covers:
  - flags;
  - bill sizes by kind;
  - running order and roles;
  - residency meaning exactly an engagement;
  - the two-act crowd;
  - determinism of a fresh calendar;
  - 84 days of the room clock with no global or population RNG;
  - no double-booked person and one room per person per day;
  - at least 25 distinct acts and at least 80% distinct line-ups;
  - a regular on at least 70% of bills and closing at least 60%;
  - a rebooked support core;
  - bounded, working-resident regulars;
  - no trade residencies;
  - the off control.
- **Regression:** `sh SimLogs/room-rotation-regression.sh on` passes all 26 checks:
  - rotation ×2, prewarm 29, ecosystem 51;
  - contract outlook 63 ×2, session promise 65 ×2, contract continuity 50 ×2;
  - offer planning 27, business 34, experience 131, terms 42, meeting 43, appointment 59;
  - opening 544, commitment planning 19, founding history 40, audience 37, comparison 32, market history 9;
  - listener 40, hired listener 43, scene rooms 83, scene information 43.
- **Fixture changes, each an intended behavior change:**
  - The ecosystem check's program-slot assertion reads the base slot count instead of 2.
  - The scene-room check accepts no engagements on day 0 under rotation. That case doesn't occur now, since regulars are named at the first booking, but the guard stays.
- **Not run:**
  - the rendered UI checks: no UI changed, but bills now list three acts;
  - a human playthrough.

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-rotation-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --enable-scene-room-prewarm --scouting-contract-outlook-survey=nashville
--headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --seed=1001 --disable-scene-room-rotation --run=rot-off2-1001-52
sh SimLogs/room-rotation-regression.sh on
```

## Portable delta

`SimTools/ScoutingRoomRotation.patch` is a ten-file delta against the prewarm snapshot: eight files changed and two new. The new files are `Systems/LocalSceneRoomService.Rotation.cs` and `SimTools/SceneRoomRotationChecks.cs`. Applied with `git -c core.autocrlf=false apply` to the baselines in `SimLogs/room-rotation-baseline/*.orig`, it reproduces every file byte for byte, line endings included. `LocalSceneRoomService.cs` is mixed CRLF/LF, and its nine LF lines were kept.

## Open design questions

The constants are first values:

- `RegularsPerRoom` 2;
- the weight shape: live 2, reputation 2, standing ×6 saturating at 4 shows;
- three acts for every non-listening room. Coffeehouse hoots historically ran many short turns. Sets are whole hours here, so a hoot would need sub-hour sets.

Not modeled:

- **Two rooms in one night.** The Beatles did lunchtime, evening and a third room in one day. The one-room-per-day rule stays until intracity travel is modeled.
- **A day-job cap.** Most local acts worked weekends only. Nothing limits an unsigned act's nights except the draw.
