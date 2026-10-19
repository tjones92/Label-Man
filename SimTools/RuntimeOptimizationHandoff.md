# Runtime optimization handoff — 2026-10-18 (updated end of second session)

Branch `media-atmosphere`. Picks up after Codex's `SimulationRuntimeOptimizationAudit.md` and
`RoomsMaterialOptimizationAudit.md`; measurements and methods are in the latter.

Ground rule from the author (2026-10-18): byte-exactness may be given up for speed; the simulation and the
economy tuning may not. Every change below is exact unless marked otherwise, and each non-exact one keeps the
same distribution by construction.

## Done (committed)

| Commit | What | Effect |
| --- | --- | --- |
| `8a12607` | Room/material exact pass (Codex's work plus `Present` and `SetFor` basis) | LR window −16.7% then a further −5%; room calendar −14% |
| `c7b1883` | Opt-in keyed regional-sales RNG pilot (`--regional-sales-rng=keyed-v1`), save v22 only when keyed | Legacy byte-identical; see `RegionalSalesRngPilotAudit.md` |
| `e8fba9b` | Exact incremental caches: standards pools (catalogue change log), person-pool order, argmax replacement | 1969 window 37.4 → 29.3 s (−22%) |
| `9bdd754` | Server GC env for audit runners (`AuditGcEnvironment.ps1`); GC columns in the profile CSV | 1969 −15%, LR −9%, byte-identical |
| `1a1359e` | Indexed lookups, per-town room caches, keyed set lists (details in the commit) | 5-year 581.7 → 414.9 s (−29%) |
| after `1a1359e` | Weekly room presence + planned-move index, `GetLabel` index, media memos, fit constants, set-list rejection sampling, single-pass market residuals | 5-year → 359.6 s (−38% vs HEAD `9bdd754`) |
| third session (uncommitted at writing) | Managed Godot RNG (`Systems/PcgRandom.cs`: `PcgRandom` for every `RandomNumberGenerator`, `GameRandom` for every `GD.Seed/Randf/Randi/RandRange`), cached `MarketRegion.CachedInstanceId`, per-format price/pressing cache in `CompetitorManager` | 5-year 359.6 → 353.2 s (−1.8%); all 80 CSVs byte-identical to `prof5-new8` |

## Third session findings (2026-10-18, late)

- **Calibration is what the real runs use.** The hitcad screen (`SimLogs/hitcad-lr-screen-v1/runs.json`) passed
  `--calibration --small-label-finance` and still wrote ~180 MB of CSVs per 5 years. The "full telemetry explains
  2.5 h" theory below is wrong; the author was right to doubt it.
- **Telemetry is not the cost under calibration.** In `trace4-fresh` every `ChartAuditRunner` handler plus
  `CaptureWeek` is ~2-3% of samples; `captureWeekSeconds` is 4.4 s of 360. The sim-side "Explain" frames are
  simulation (`SceneRecruitmentService.Explain` is the recruitment score), not telemetry.
- **The EventPipe trace over-weights native calls.** It put 23% of samples in Godot interop (9% in the playlist's
  `RandomNumberGenerator.Randf`). Replacing every RNG with a bit-identical managed port left `PlaylistsSeconds`
  unchanged (38.3 → 37.9). Only the price-table fix (Godot `Dictionary<string,float>` marshalled a key per lookup;
  `calculateLabelRevenueSeconds` 13.6 → 9.3) and market clearing (21.3 → 16.7) moved. Confirm any trace-led
  target with a span A/B before investing.
- **Godot RNG facts (proved by `SimTools/GodotRngParityChecks.tscn`, 4.8M draws):** instance `Randf` is
  RandomPCG's two-draw ldexp construction; global `GD.Randf` is ONE draw / `(float)UINT32_MAX`; `GD.RandRange(double,
  double)` is the three-draw `randd`, whose 64-bit significand needs a hand-rounded conversion because .NET's
  `(double)ulong` double-rounds above 2^63. `GameRandom.Seed` also seeds Godot's global so a stray `GD.*` draw stays
  deterministic; new code should call `GameRandom`/`PcgRandom`.
- **Parallel seeds are the decade-wall lever.** 1969 fixture, 4 weeks, calibration: one seed 19.5 s measured;
  two at once 67.2 s and 51.8 s (free RAM hit 0 GB, `competitorWeek` 3.7 → 30 s). Fresh 1960, 52 weeks: one seed
  40.3 s, two at once 47.2 s each. So pairs win early and lose ~3x per job once late worlds page on this 7.2 GB box;
  two seeds back to back beat two in parallel for the late decade. Estimated single-seed calibration decade on the
  current tree: ~21 min (1960-64 measured 353 s; 1965-69 interpolated 2.0 → 4.9 s/week).

## Measuring

- **The 5-year yardstick.** `RunSimulationBenchmark.ps1 -Run <name> -Weeks 260` (fresh seed 1001, calibration
  flags), about 6-10 minutes. The HEAD control is `SimLogs/prof5-base-*` (assembly of `9bdd754`). The profile CSV
  has per-year seconds and per-span allocation MB (`<Phase>AllocMB`, `<Span>AllocMB`); top-level spans cover
  ~93% of wall time. `scratchpad/prof5.py` style summaries: sum each column over the years.
- **Proving exactness when a batch also has a non-exact change.** Gate the non-exact path behind a temporary
  environment switch (this session used `LABELMAN_TMP_EXACT_SETS=1`), run 5 years with it, and hash-compare all
  80 CSVs against `prof5-base` (`compare_simulation_benchmarks.py`). Remove the switch before committing. The
  4-week 1969 window alone missed nothing this session, but it does not exercise the pre-roll or early years.
- **Economy check for a non-exact change.** Compare concentration, rollup units/gross, year-end genre slots and
  tier transitions per year against `prof5-base`. One seed: chart-share deltas of ±8 pts in a year and genre slot
  moves of ±3-7 are reshuffle noise here (two different reshuffles of the same change both showed them, with mixed
  signs). A real tuning shift shows one sign across years.
- **CPU traces.** `dotnet-trace` must attach to the `Godot_v4.7-stable_mono_win64` child, not the
  `_console.exe` wrapper (that hangs). Fresh-run trace: `trace4-fresh.speedscope.json` (60 s inside 1962).
- Only interleaved runs with prebuilt DLLs swapped into `bin/Debug` give trustworthy small deltas; a 5-year
  run's total is stable to a few percent. Never build while a benchmark runs (it refuses a changed assembly).
- Never `json.load` a late world (froze the PC). Stream-hash it without the top-level `Flags` array instead.
- `RunSimulationBenchmark.ps1 -ResumeWorld` needed a PS 5.1 fix (`ConvertFrom-Json` wraps arrays).

## Where 5-year time goes now (`prof5-new8`, 360 s)

| Span | Seconds | Share | Notes |
| --- | --- | --- | --- |
| simulateWeek | 110 | 31% | Playlists 38, RegionalDemand 24, MarketClearing 21, RegionalState 10, RecordState 7 |
| competitorWeek | 71 | 20% | MaterialSelection 36 (album creation; `PolarCoverResolver.Propose` self time) |
| RoomCalendar | 47 | 13% | was 138 at HEAD |
| dailyTalentMarket | 39 | 11% | `SceneRecruitmentService.Slate` per label x act per day |
| populationLifecycle | 26 | 7% | includes SceneWindows 12 (`PreviewCast`) |
| bookSettlement | 16 | 4% | |
| GC pauses | 13 | 4% | |

Not in any span: `MediaAtmosphereService.AfterCompletedWeek` (~10% of a 1962 trace before the media memos;
observe-only today, Phase 4 causal next) and `CompetitorManager.OnMonthChanged` (distribution deals).

## Next targets

1. **Playlists (11%).** 77 reporter stations score every single every week. Visiting only circulating singles
   plus incumbents saved nothing (most singles circulate), so the cost is per-candidate `FormatMatch` /
   `Candidacy`. Precompute per (station format, genre profile) format match once per week.
2. **Album material (10%).** `Propose` self time, `SongProfileDeriver.Derive` (enum `ToString` per lyric mode,
   LINQ `Distinct`), `PolarMaterialFit.Evaluate` allocating `new[] {...}` per call.
3. **Daily talent market (11%).** Per (label, act) `Explain` still does two place lookups and a guest scan. A
   per-day act table (home place, scene, has-guest-rows) shared across labels would make it reference compares.
4. **Regional demand + clearing (13%).** Per record x region; the spillover residual is now one pass. Look at
   `ChartSimulator.CalculateRegionalSales` and allocation (25 GB over 5 years in RegionalDemand alone).
5. **Media layer.** Measure it with its own span first.
6. **Late decade.** These shares are 1960-64. A late window (1969 fixture) is still album-count-bound; re-profile
   there before choosing the next late-game target. A decade run with full telemetry (not calibration) is what
   the author's ~2.5 h figure measures; telemetry capture is a separate cost from the simulation.

## Next session: telemetry and the real decade cost

**Superseded by "Third session findings" above**: the runs are calibration, telemetry is ~2-3%, and parallel
seeds paging late worlds is the measured multiplier. The next lever for the decade wall is the late-world heap
(≈2.2 GB, scales with album count): shrinking it lets two seeds share the machine without paging. Kept below for
the invocation-hunting notes.

The author reports a decade run takes ~2.5 h (2026-10-15). Everything measured above is **calibration mode**
(`--calibration --small-label-finance`), and it does not add up to 2.5 h:

- Calibration decade, estimated from measurements (not run): HEAD `9bdd754` ~35 min (1960-64 measured at
  582 s, 1965-69 interpolated from 3.3 s/week in 1964 to ~7.7 s/week in the 1969 fixture). Current tree ~20-25
  min. The last measured canonical calibration decade was 46.7 min on 2026-10-07 (memory note), so the
  extrapolation is in the right range.
- So the extra ~2 h comes from something these runs exclude. Candidates, in the order to check:
  1. **Full telemetry.** Without `--calibration`, `ChartAuditRunner.CaptureWeek` runs ~13 more per-week
     `Write*` passes that walk all records, labels and rosters (the `!calibrationMode` blocks around
     `ChartAuditRunner.cs:2151-2219`). On 2026-08-06 that was ~21% of a decade, but scenes, band life, media
     and polar telemetry have been added since, and each scales with album or artist count.
  2. **Audit flags.** Band-life observers (`--observe-band-life`, `--enable-lineup-churn=world`,
     `--enable-cowriting`, `--enable-member-axes`), scene audit censuses (`WriteSceneRecruitmentCensus` and
     similar), polar research shadows (`polarSongShadow`, `CapturePolarCensus`), session-supply audits.
  3. **Machine.** Two seeds run at once, or paging: a late world holds ~2.2 GB on a 7.2 GB machine, and the same
     1969 build measured 59 s under memory pressure vs 37 s after a restart.
  4. **World saves** (`--save-world-at-year`, `--save-world-at-end`): late worlds are multi-GB JSON.

### Plan

1. **Find the actual 2.5 h invocation** before measuring anything: ask the author which script or command they
   run for a decade, or read the newest decade run's `*-invocation.json` / console log head. SimLogs is too
   large to search with `find` or `ls` in under 2 minutes; filter by name (`ls SimLogs/<prefix>*`).
2. **Size telemetry cheaply.** Run 52 weeks fresh with the decade's own flags twice, with and without
   `--calibration` (benchmark script: `-Detailed` drops calibration). The profile CSV's `captureWeekSeconds`
   and `wallSeconds` give the split. Then the same on the 1969 fixture (4 weeks) for the late-game ratio.
   `captureWeekSeconds` covers `CaptureWeek` + polar + session supply + census; anything else the runner does
   per week (`MaybeSaveWorld`, annual flushes, `ValidateEmergentSigningFloor`) is outside every span. Add spans
   if the numbers do not close.
3. **Profile `CaptureWeek` itself.** A CPU trace of a full-telemetry run (`dotnet-trace`, attach to the
   `Godot_v4.7-stable_mono_win64` child) shows which writers dominate. Expect the same patterns that dominated
   the simulation: per-week scans of every record and LINQ over growing lists, string formatting of
   floats per cell (`F()`/`Csv()` helpers), and per-record lookups that could be indexed.
4. **Telemetry optimizations are output-format work, not economy work.** They may never change simulation
   state. Prove inertness by hashing the economy CSVs against a run of the same flags; telemetry CSV contents
   must also stay identical unless a column is deliberately dropped. If the author agrees, rarely-read
   per-week CSVs could move to an opt-in flag (ask first; some audits rely on them, e.g. `album-composition.csv`
   is empty under `--calibration`).

### Facts the next session needs

- Exact-proof workflow and the HEAD control: see **Measuring** above. `prof5-base-*` is a calibration control;
  a full-telemetry control must be generated at the same commit with the same flags.
- `--calibration` keeps every calibration metric byte-identical (memory note "calibration mode for fast runs");
  the economy is the same with or without it.
- The canonical decade flags are `--weeks=522 --seed=1001 --enable-genre-market-v2
  --enable-artist-population-lifecycle --calibration`. A decade that finishes in ~7 min is missing the two
  `--enable-*` flags.

## Commands

```bash
# 5-year profile (fresh, seed 1001); output SimLogs/<name>-performance-profile.csv
powershell -NoProfile -ExecutionPolicy Bypass -File SimTools/RunSimulationBenchmark.ps1 -Run <name> -Weeks 260
# 1969 fixture, 4 weeks
powershell -NoProfile -ExecutionPolicy Bypass -File SimTools/RunSimulationBenchmark.ps1 -Run <name> -Weeks 4 -ResumeWorld bms4e-intent90-1001-1969
```

Build before every run: `dotnet build "Label Man.sln" -c Debug`, then `dotnet build-server shutdown` (the
compiler server holds ~1 GB on this 7.2 GB machine). Godot does not rebuild on its own.
