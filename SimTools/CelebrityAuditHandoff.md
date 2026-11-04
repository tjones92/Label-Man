# Celebrity system audit: handoff

For an agent auditing how fame works in Label Man. Written 2026-10-15 on branch `media-atmosphere`, after the Phase 3
mid-decade fan check (`SimTools/MediaAtmospherePhase3Notes.md`, "Mid-decade check"). Everything here was read from code
or measured on seed 1001; re-verify line numbers before citing them.

## There are four fame systems, and they mostly do not read each other

| System | Where | What it holds | Who reads it |
|---|---|---|---|
| Career ladder | `Data/SimulatedArtist.cs` `CareerState` (Unsigned … Superstar … Retired), `ArtistManager`, `RosterManager` | Discrete rung, chart-driven | Album planning stature (`CompetitorManager.CalculateAlbumPriorNet`), `AILabel.GetMarketingBudget`, the audit's stature multiplier. Launch reads it only when recognition is off. |
| Seeded star canopy | `Systems/StarCanopy.cs`, `RosterManager` `StampStar` | 6 Superstars + 24 Stars stamped onto Major/MidTier rosters in 1960, with synthetic hit histories | Whatever reads careerState and the stamped counters. On by default (`chart_manager.tscn` `seedStarCanopyEnabled = true`). |
| Recognition stock | `Systems/ArtistRecognition.cs`, `ArtistRecognitionService.cs`, `MusicianRecognitionService.cs`; directive `SimTools/CelebrityRecognitionDirective.md` | `publicRecognition`, `culturalStanding` per act; `personalRecognition` / `liveReputation` / `creativeReputation` per musician | Single and album launch (awareness, stock, radio lift), on by default (`artistRecognitionEnabled = true`). UI STANDING section. |
| Media fans | `Systems/Media/MediaFans.cs`, `MediaAtmosphereService.Fans.cs` | Teen attachment by region, fan episodes, member spotlight | Media only, observe-only by design: booking interest, hype cap, fan press leads |

`Systems/CulturalRecognitionService.cs` is a separate record-level layer (how widely a record's merit is known). Its
`Press` channel says "Nothing writes this yet -- the journalism seam", and media's press items still don't write it.

## Known seams worth auditing

1. **Media is fame-blind.** Nothing under `Systems/Media/` reads `careerState`, `publicRecognition` or the canopy.
   National TV books only from this week's singles chart (`MediaAtmosphereService.Candidates`: top 25 for Sullivan, 30-70
   for the others). Real 1960-63 Sullivan bills were mostly adult pop and crooners, Broadway, opera and comedy, booked on
   standing fame. Teen and rock acts were about 18 appearances in a 45-episode season (CTVA 1961-62 guide). In the sim a
   Superstar with no current top-25 single can't get on Sullivan.
2. **Fans start at zero in 1960, seeded stars included.** No seeded teen idol has any prior attachment.
3. **Fan episodes are short and lag the record.** Attachment's ceiling is about 0.435: a region's No. 1 seller every week
   at full youth fit. Top-10 regional sales across the 7 regions move together, so an act drops from 7 selling regions
   to 0 in one week when its record slides from about #11 to #15 nationally. Sensations last 2-3 weeks. No act reached
   cultural phenomenon in 1963-66: four straight extraordinary weeks never coincide with hot attachment and two evidence
   families. That is structural, not a threshold.
4. **Fame inputs vs. outputs.** Recognition feeds launch, so check for double counting wherever careerState still
   multiplies something that recognition also lifts (directive §0, §3). Memory notes say Phase C removed the careerState
   launch-stock switches only under `ArtistRecognition.Enabled`.
5. **Person-level fame transfer is unverified.** Lineup churn used to never fire. The band-member simulation since then
   does produce solo spinouts (about 19% of 5+-hit groups lose a member to a solo career). Nobody has checked whether
   a departing member's `personalRecognition` carries into the new act, or whether the old act keeps it.
6. **`controversy` is a cosmetic record field.** It's random, and only UI and prose read it. Media Phase 2 adds its own
   `Controversies` list. They are not the same thing.

## How to measure cheaply

- Build `dotnet build "Label Man.csproj" -c Debug` before every headless run (Godot loads `.godot/mono/temp/bin/Debug`).
- Console exe: `C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe`.
- Required flags: `--enable-genre-market-v2 --enable-artist-population-lifecycle` (add `--aggregate-only` for speed).
- Mid-decade world: `SimLogs/worlds/media-p3-1001-1963.world.json.gz` (seed 1001, 1/4/1963). Resume it with
  `--resume-world=media-p3-1001-1963 --seed=1001 --weeks=209` (about 40 min solo, 52 min two-wide). Two resumes of one
  world are byte-identical. The global stream is reseeded on load, so the control is another resume of the same world.
- `--media-fan-diagnostics` prints `MEDIA_FANS_WEEK` (per act per week: regional attachment, selling regions, evidence
  families, national exposure, extraordinary, chart position). `--media-fan-hot=X` overrides the hot threshold.
  Offline replay of the episode rules from those lines matched the live run exactly at 0.38. The script is in the
  shared notes folder (`replay_fans.py`, outside the repo); it is ~80 lines and easy to rewrite from
  `MediaFans.Level/Advance`. The default hot threshold is now 0.36 (was 0.45, which no act could reach).
- Year-end lines: `MEDIA_FANS_YEAR`, `MEDIA_AI`, `MEDIA_YEAR` print only after Dec 25.
- Saved worlds carry media state (`WorldSaveData.Media`), so fan state can be read straight from the gzip JSON
  (`World.Media.Fans`).
- Pipe nothing. Redirect run output to a file and launch in the background. A piped long run hangs.
