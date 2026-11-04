# Simulation runtime optimization audit — 2026-10-18

The shared game code is faster without changing tuning, release rules, record retirement,
catalogue contents, or RNG draws. These changes apply to the current game and current LR
research configuration. Older bookmarks supply large scaling workloads; they do not stand
in for a newly generated current-version world.

## Measurements

Single-process, sequential runs on the same machine, Godot 4.7 Mono, .NET 8. Timing is
descriptive, not a repeated-trial estimate. All times below are seconds.

| Workload | Records / albums at end | Before simulation window | Final simulation window | Reduction | Before / final whole process |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1963 bookmark, seed 1001, 8 weeks | 8,006 / 3,869 | 50.101 | 31.681 | 36.8% | 117.470 / 81.801 |
| 1969 bookmark, seed 1001, 4 weeks | 13,969 / 9,502 | 80.629 | 60.243 | 25.3% | 245.419 / 196.451 |
| Fresh LR, seed 1001, 26 weeks | 3,262 / 717 | unavailable | 41.271 | — | 204.130 / 67.062 |
| Fresh LR, seed 2002, 26 weeks | 3,206 / 779 | unavailable | 39.143 | — | unavailable / 57.414 |

The original runner did not emit partial-year profiles. The older-world controls add only
timing/end-save infrastructure to the original code. Fresh LR's original assembly produced
no 26-week timing row; its comparison is whole-process time, including startup. That control
was sampled with EventPipe during part of its run, and only the optimized LR run saves an end
world. The approximately 3x process improvement is indicative, not an isolated estimate of
any individual optimization. It must not be extrapolated into a decade completion time.

Room calendar timings vary substantially between runs and between `RoomAdvance` and
`RoomCalendar`, because both paths may ensure the calendar. Intermediate optimized builds
also varied (1969 window 55.042 seconds; final 60.243). No separate speed claim is made for
the last room-eligibility memo. The aggregate improvement above uses the final tested build.

Pending album dispatch was only 0.102 / 0.386 seconds in the two original windows. In the
final 1969 window, material selection still takes 11.397 seconds inside competitor work;
room scheduling, daily room advancement, and population work also remain substantial.
These spans overlap their parent timings and must not be added as independent costs.

## Changes and dependency boundaries

- Enable compiler optimization for the Debug assembly that both editor and headless runs
  load, retaining debug symbols. `-p:Optimize=false` restores unoptimized source stepping.
- Reuse responsive revenue-memory summaries for the same observation list, week and count.
  Every residual/maturity revision invalidates the summary. Ordered float sums are unchanged;
  weak list keys release summaries after world replacement.
- Keep regional genre-demand values for one market-clearing call, avoiding integer-year
  album / fractional-year single cache thrashing. Keys retain the existing genres, region
  identity, exact year/momentum bits and texture switch. The old cache's year transitions
  still occur. No values survive this scope through the new cache.
- Filter and partition the unsigned daily supply snapshot once before label nominations.
  Regional pools preserve snapshot order; discovery, scoring, and ownership commits retain
  their original label-specific order and decisions.
- Cache naming filter/locale membership after dynamic filters resolve. Weights, mood/era
  calculations, subtraction order, and RNG draws still execute as before. The cache is
  bounded to 4,096 entries and cleared with the existing naming caches after lexicon changes.
- Reuse JSON serializer metadata for master fingerprints and evaluate archetype enum names
  once per row lookup. Serialized inputs, SHA fingerprints, and first-match lookup semantics
  stay the same.
- Heapify room candidates and consume the required prefix instead of sorting every act.
  Priority retains descending score, ordinal ID and stable input order. All keyed scores
  are still evaluated eagerly, and the strongest regular still appears first. Each
  enumeration has its own heap.
- Reuse room set membership, keyed original titles, stage lineups, presence and genre-family
  reads within one scheduling call. Scheduling changes bills/reservations/engagements but
  does not change these artist, composition or relocation inputs. Each bill still computes
  its own keyed song order and detached set entries. Presence reuse follows the existing
  startup-cache switch; the public eligibility predicate remains available unchanged.
- Reuse private market-intent scratch objects across clearing passes, resetting every field.
  Trim unused objects and clear the pool on world rehydration. The record-major / region-minor
  evaluation and market-clearing order are unchanged.
- Add detailed phase timers, partial-year profile rows, end-window snapshots, a serial
  benchmark helper, and a complete-output comparison script. Generated evidence stays in
  gitignored `SimLogs/`.

The supplied `SimulationOptimizationReadingList.md` is retained as reference material.
Its dependency-aware reuse and allocation ideas fit this work. Counter-based RNG, agent
aggregation and reduced simulation detail require separate behavior-changing experiments;
they were not applied under this session's identity constraint. Claims in that reading list
are not treated as measured findings about the current checkout.

## Behavior validation

The comparison script checks file sets, empty streams and SHA-256 of every non-timing CSV.
Timing profiles are the sole excluded CSV. Complete gzip equality proves complete serialized
world equality; if compressed bytes differ, the script compares decompressed contents.

| Check | Result | Evidence under `SimLogs/` |
| --- | --- | --- |
| Original vs final, 1963 | All 80 CSVs and entire saved world identical | `perf-final-1963-comparison.json` |
| Original vs final, 1969 | All 80 CSVs and entire saved world identical | `perf-final-1969-comparison.json` |
| Original vs final, fresh LR 1001 | All 82 CSVs identical | `perf-lr-final-1001-comparison.json` |
| Preceding optimized vs final, fresh LR 1001 | All 82 CSVs and entire saved world identical | `perf-lr-final-1001-world-comparison.json` |
| Archived LR 260-week research vs final 26-week runs, 1001 and 2002 | Exact prefixes of all seven checked streams, 14/14 | `perf-lr-reference-prefix-validation.json` |
| Room ordering probes | 90/90, including ties, NaN/infinity, duplicate IDs/references, regular precedence, prefixes and repeated enumeration | `perf-order-check-console.log` |
| Naming replay | 1,200 paired cached/original outputs and remaining RNG state identical | `perf-final-naming-checks.log` |
| Game build | 0 warnings, 0 errors | `dotnet build 'Label Man.sln' --no-restore -v minimal` |

The seven archived-LR streams are records, release strategy, calibration decisions, release
outcomes, format-memory revisions, label finance and artist-population events. End-window
summaries are not prefix-comparable. Seed 2002 has this prefix validation, not a full-world
comparison against a newly rerun original. The original fresh LR run had no end snapshot;
the LR world comparison is between optimized stages, not against the original build.

The full naming suite is **148/150**, with two existing profile-inheritance assertions
failing: `Blues inherits BluesRoot nickname 0.9` and
`BritishBlues inherits nickname .9 from root`. Running the original Core and test source
from HEAD gives **146/148**, failing exactly those two assertions
(`perf-naming-baseline-check.log`). Both added replay assertions pass. No genre configuration
was changed to accommodate those tests.

Final tested assembly SHA-256, reproduced by the final successful build:
`252600C598FB22E4DADEBE0BDB3E0FE0F70AC875F0AD1EFD160A9B7601CFEA7C`.
The instrumented original bookmark control is
`49CED48FFFAD0F69ACEB997816FE19BC1A240E5B5B356A0FBE26054E3E0770AC`;
the original fresh LR control is
`E45238C8EF42184E7CA258CA9D8A1B4DC76E19FE289A3BA2C6AB9196F9632175`.

Matching complete-world gzip SHA-256 values:

- 1963: `a93ae777a0ce4a2b3a8e3bb0fc09689ef6232a472d923b95691f86f59f582a14`
- 1969: `9f99907f9b4ad3c788602dc275e1eab761a1d93fcec777897b29771fe27ce48c`
- LR optimized stages: `bd8e115300ac7ea860756db3f84a5f87147884896b2c7da8ceac2aa125655a77`

## Reproduction and continuation

Start from a successful build and an idle game/editor. Use new output names. The helper
records assembly hash, arguments, process time and completion status in an invocation
manifest, and refuses output overwrite, concurrent game processes or a changing assembly.

```powershell
./SimTools/RunSimulationBenchmark.ps1 -Run next-1969 -Weeks 4 `
  -ResumeWorld bms4e-intent90-1001-1969 -SaveWorld next-end-1969
python SimTools/compare_simulation_benchmarks.py SimLogs/perf-profile-1969 SimLogs/next-1969 `
  --before-world SimLogs/worlds/perf-control-1969.world.json.gz `
  --after-world SimLogs/worlds/next-end-1969.world.json.gz `
  --output SimLogs/next-1969-comparison.json
```

The helper restores bookmark flags and verifies that its seed matches `-Seed` (default
1001). A resumed control and treatment must start from the same file: load reseeds the
global RNG, so a resumed window is not the original uninterrupted trajectory.

The old fixture dates/versions are 1963-01-04 / v21 (`media-p3-1001-1963`) and
1969-01-03 / v4 (`bms4e-intent90-1001-1969`). No save-schema migration was introduced.

Fresh LR flags were read from `SimLogs/hitcad-lr-screen-v1/runs.json`:

```text
--enable-genre-market-v2 --enable-artist-population-lifecycle
--local-signals-read-demand --regional-arrival --regional-retail-access
--regional-campaign-focus --economic-label-exits --artist-momentum-half-life=52
--launch-push-saturation=0.5 --seed-head-templates --head-hit-attribution
--hit-act-cadence --hit-act-retention
```

Use these with `-ExtraArgs` for fresh research runs. Current-version midyear LR bookmarks
are now available as `perf-lr-final-midyear-1001`, `perf-lr-opt3-midyear-1001` and
`perf-lr-1960-midyear-1001`; their saved contents agree. They preserve this configuration
and provide a current-version starting point for subsequent profiling.
