# Scouting continuation: a working week

October 11, 2026. Continues the room-rotation snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. It closes both "not modeled" items in `ScoutingRoomRotationHandoff.md`. Changes are uncommitted. Primary gameplay sources and other checkouts are unchanged.

## What it does

`Systems/LocalSceneRoomService.WorkingWeek.cs` adds two rules, with small hooks in `Schedule`. Both are on by default with room rotation, and both turn off with `--disable-scene-room-rotation`.

### Day jobs: weekend-only acts

- **Day-job acts** play on Friday, Saturday and Sunday.
  - The exception is the coffeehouse open stage. Hootenannies ran on weeknights for whoever turned up.
- **Full-time acts** take weeknight work. An act is full-time on any of these routes:
  - **Traction:** a chart entry, or a career past NewSigning (Rising or above).
  - **A standing room engagement.** The residency is the job.
  - **Enough room work to live on:** at least `ProShows = 26` live room shows, counted across every room. That is half a year of weekly gigs. A weekend act reaches it in about three months.
  - **Gone pro:** every active member has ambition ≥ 0.6 and is aged 25 or under. This is the Cavern-era Beatles route: unsigned, young, quit their jobs anyway.
  - **Wealth:** a member's wealth Norm is at least 0.45, the band-life StudioOnly threshold.
- **A contract alone is not enough.** The first cut counted signed acts as full-time, which made 55% of performers full-time. The AI world signs many small local acts, and those acts mostly kept their jobs.
- **Live shows only.** Pre-rolled history (`PriorShows`) is not counted, the same as the booker's own count, so a prewarm never moves a live bill. The prewarm check caught this.

### Double rooms: two rooms in one day

A full-time act can work a second room in its town the same day, under these limits:
- at most `MaxRoomsPerDay = 2` rooms;
- `TravelHours = 1` clear between the end of one set and the start of the other;
- every other set the act's people have that day must be with this act, as members;
- house-band backers stay in one room;
- a player who crosses town to a second room is taken off the house-band list for that day.

**What limits doubling.** Room hours and genre families decide who can double at all:
- afternoon rooms are community halls and roadhouses (12–15), plus listening rooms (14–17);
- evening rooms are clubs and coffeehouses (17–20);
- so in practice the doubles are blues acts (roadhouse plus club, Wednesday and Friday) and jazz acts (listening room plus coffeehouse, Saturday).

A Cavern lunchtime-plus-evening beat group can't double, because each city has one rock room. That is a catalog limit.

## Measured

### Room clock, 12 weeks, all rooms (`--scene-room-rotation-check`)

| | s1001 | s2002 |
|---|---|---|
| acts that played | 2,658 | 2,621 |
| full-time acts | 783 (29%) | 819 (31%) |
| full-time route: gone pro / live-show bar / engagement / traction | 480 / 110 / 221 / 167 | 522 / 117 / 218 / 177 |
| doubled act-days | 119 of 13,287 (0.9%), 31 acts | 101 of 13,123 (0.8%), 23 acts |
| club acts per bill, weekday / weekend | 3.32 / 3.40 | 3.27 / 3.30 |
| Nashville club: distinct line-ups / nights | 59 / 60 | 59 / 60 |

The routes overlap, so they don't sum to the full-time total.

With the stricter traction-only rule, only 15% of acts were full-time, and Nashville weekday line-ups repeated (41 distinct line-ups in 60 nights). The user asked for a more liberal rule.

### AI economy, 52 weeks, compared with `rot-on2-*-52`

- **Off control** (`--disable-scene-day-jobs --disable-scene-double-rooms`, s1001): all 84 shared CSVs are byte-identical to `rot-on2-1001-52`. The six other files are scene-room audit outputs that only `rot-on2` wrote.
- **On:** first-time signings went 134→135 and 110→109. First-chart events went 249→259 and 258→261.
  - 1960 genre-share error went 43.8→40.0 and 26.2→27.9.
  - Soul's 1960 share went 20.05→18.34 and 12.60→12.43.
  - At this length all of these are noise.
- **No decade run.** The user deferred decade validation to the end of the directive.

## Flags (`Systems/LocalScenes.cs`)

| Flag | Effect |
|---|---|
| `--disable-scene-day-jobs` | Turns off the weekend-only rule. |
| `--disable-scene-double-rooms` | Turns off the second room. |
| `--enable-scene-day-jobs`, `--enable-scene-double-rooms` | Require rotation; each conflicts with its disable flag. |

## Save/restore

- No schema change.
- `actShows` is a derived index rebuilt from `Standing` on restore. It is never saved.

## Verification

- **Build:** 0 warnings and 0 errors.
- **`--scene-room-rotation-check`** passes at seeds 1001 and 2002, with these new checks:
  - flags;
  - at most two rooms per person per day;
  - a second room is the same act, with members only, in the same town, with the travel gap;
  - only full-time acts double;
  - day-job acts play only on weekends and at coffeehouses;
  - doubling stays at no more than 1 in 20 act-days;
  - weekend line-ups at least 80% distinct, all nights at least 50%.
- **Fixture changes**, each an intended behavior change:
  - the rotation check's 80% distinct-line-up test now applies to weekend nights only;
  - `LocalSceneRoomChecks` changes "one room per person per day" to "one city, and at most two rooms when double rooms are on."
- **Regression:** `sh SimLogs/room-rotation-regression.sh ww` passed 24 of 26 checks. The two failures were the prewarm check and the scene-room check, both described above. After the fixes, both re-ran and pass, along with the two rotation checks.

## Portable delta

`SimTools/ScoutingWorkingWeek.patch` covers five files: four changed and one new (`WorkingWeek.cs`).
- Applied with `git -c core.autocrlf=false apply` to the baselines in `SimLogs/working-week-baseline/*.orig`, it reproduces every file byte for byte.
- Generate it outside the repo. `git diff --no-index` inside the repo normalizes CRLF.

## Open

- **Calibration constants:** `ProShows` 26, `ProAmbition` 0.6, `ProMaxAge` 25, `TravelHours` 1.
- **Ratchet:** the live-show bar never decays, so over a decade more acts become full-time. Watch the full-time share in the end-of-directive decade run.
- **Catalog limit:** one room per kind per city, so most genres can't double.
