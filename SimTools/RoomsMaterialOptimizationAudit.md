# Room and material runtime audit — 2026-10-18

This follows `SimulationRuntimeOptimizationAudit.md`. The reference is the already
optimized game at `1aacdfe`, not the earlier unoptimized Debug build. These changes
retain every artist, room, bill, backing musician, repertoire entry and historical
event. No simulation cadence, eligibility rule, weighting or RNG output changes.

## Findings and implementation

A 20-second CPU trace of fresh LR showed room scheduling in approximately 47% of
sampled CPU stacks, set construction in 22%, material selection in 17% and arrangement
resolution in 11%. These inclusive spans overlap. Temporary LINQ buffer allocation
was prominent. This trace identifies targets; it is not a representative decade census.
The trace's extra duplicate/delimiter CLI tokens were inert to simulation; matched
validation below uses the original LR argument list.

- Reuse act profiles, shrunken market tastes and identical arrangement proposals
  within a single `ChooseMaterial` call. Source builders do not commit performances
  or change the act during that call. The cache is disposed before application and
  cannot retain a stale profile across later career, lineup, song or taste changes.
  Candidate observations and refusal events still fire at their original call sites.
- Select the stable best three room songs and backing musicians with a bounded
  ordered buffer. Evaluate every original candidate/key; retain original tie order,
  scores, references and eligibility. Avoid arrays/sort storage proportional to the
  entire candidate pool for each tiny prefix.
- Reuse the exact UTF-16 hash of the fixed room/night or bill/artist prefix. Append
  each entity ID and apply the original final mix. Seed, namespace, suffix characters
  and floating-point conversion are unchanged. No new random algorithm is involved.
- Index engagements by room for one scheduling pass. Append newly offered
  engagements immediately to both the world list and that room's index. The daily
  engagement set, dates, term rules and booking order remain authoritative.

## Matched measurements

Single sequential runs, same machine and LR flags as the preceding audit. Seconds:

| Workload | Prior simulation window | New window | Prior / new material selection | Prior / new room calendar |
| --- | ---: | ---: | ---: | ---: |
| Fresh LR, seed 1001, 26 weeks | 41.271 | 34.369 | 6.800 / 4.801 | 16.676 / 12.291 |
| 1969 fixture, seed 1001, 4 weeks | 60.243 | 53.423 | 11.397 / 9.371 | 12.813 / 12.557 |

These are 16.7% and 11.3% lower simulation-window times. The freshly repeated traced
LR control measured 42.709 seconds. Whole-process LR time was 67.062 / 59.211 seconds;
the late-world whole process was 196.451 / 211.537 seconds, with large load/save IO
outside the simulation window. Do not infer an IO or full-decade improvement from
the CPU-window result. Calendar work can also occur inside `RoomAdvance`, so a single
calendar span does not capture all room work.

## Identity and tests

- Fresh LR: all 82 non-timing CSVs and the entire saved gzip world match the prior
  optimized build. Evidence: `SimLogs/perf2-opt2-lr-comparison.json`.
- 1969: all 80 non-timing CSVs and the entire saved gzip world match. Evidence:
  `SimLogs/perf2-opt2-1969-comparison.json`.
- Stable-prefix and hash-partition probes: **147/147**. Covers repeated IDs/objects,
  ties, NaN/infinity, prefixes, repeated enumeration, three prefix sizes and UTF-16
  keys including null characters and supplementary characters.
  Evidence: `SimLogs/perf2-fixed-checks.log`.
- Build: zero warnings/errors.

Tested exact-optimization assembly SHA-256:
`0614D2B90E0C33C82FFFA81C82838CA5802B1933970C1B1D69C38AB45E0BDA45`.
The complete-world gzip hashes remain the preceding audit's
`bd8e115300ac7ea860756db3f84a5f87147884896b2c7da8ceac2aa125655a77` (LR) and
`9f99907f9b4ad3c788602dc275e1eab761a1d93fcec777897b29771fe27ce48c` (1969).

The CPU trace is `SimLogs/perf2-room-material.speedscope.json`; its summary is
`SimLogs/perf2-room-material-cpu.json`. Regenerate a compact summary with:

```powershell
python SimTools/analyze_simulation_cpu_trace.py SimLogs/perf2-room-material.speedscope.json `
  --output SimLogs/perf2-room-material-cpu.json
```

## Second room pass (Present and set pools)

A fresh 25-second trace of the build above still put `Schedule` at 42.8% of sampled CPU:
`LocalScenePersistenceService.Present` 9.6% (it materialized the act's current scene rows
per act/room/day check) and the `SetFor` pool build 11.4% (each act's
genre-plus-family standards concatenated, filtered and deduplicated afresh each week).

- `Present` now makes two allocation-free passes over the same rows with the same
  predicates and evaluation order.
- `SetFor` takes a per-pass basis per (genre, family, year): catalogue order, first copy
  of each songId, `originYear` already applied. Each act then applies only
  `EligibleLive`. Filtering after deduplication keeps the same songs only when a
  repeated songId is the same object (true here: `AddToPool` adds one object to both
  lists), so any other catalogue, or a null songId, returns no basis and the original
  per-act filter runs. `EligibleLive` and `PolarActProfileDeriver.Derive` are pure, so
  fewer calls change nothing. The basis is cleared each pass and rechecked against list
  counts.

After: `Present` 1.2%, pool build 4.1%, `Schedule` 35.1% of sampled CPU
(`SimLogs/perf4-room-cpu.json`, trace `perf4-room.speedscope.json`).

Identity: fresh LR seed 1001, all 82 CSVs match `perf2-final-legacy-lr1001`; the saved
world matches except `Flags`, which records the duplicated CLI tokens of the earlier
launcher. 1969 fixture: all 80 CSVs and the whole world match `perf2-opt2-1969`.

Timing: single sequential runs on this machine varied 32-44 s for identical code, so
old (`AFE1AACE…`, rebuilt bit-identically) and new (`D127D8E7…`) assemblies were
swapped into `bin/Debug` and run interleaved, three pairs, first pair discarded as warm-up:

| Fresh LR, seed 1001, 26 weeks | Old | New |
| --- | ---: | ---: |
| Simulation window | 32.19 / 32.60 | 30.68 / 31.12 (30.82 warm-up) |
| Room calendar | 11.27 / 11.28 | 9.65 / 9.66 (9.82 warm-up) |
| Competitor week (untouched control) | 8.02 / 8.11 | 8.09 / 8.11 |

Room calendar about 14% lower, simulation window about 5%. Remaining room targets in the
trace: the set-list `StableSmallSelection.Take` (6%), house-band `Pick` (6.4%; per-musician
string keys and contact-edge lookups) and member eligibility building (~8.7%).

## Late-world pass (standards pools, person pool)

The 1969 fixture costs about 10x per week what fresh LR does, so the next trace ran there.
`StandardPoolsFor` alone was 21.6% of sampled CPU. Every AI cover choice re-tested
establishment for every song in its families' standards-plus-hits union (thousands by 1969),
then sampled about a dozen. `PersonPool.Ordered()` re-sorted the whole pool by person id per
call. The house-band `Players` called it once per town per week, and each band replacement
hire called it again.

- **Standards pools** are kept per family (plus Easy Listening's media-cut access) between
  calls. Year, establishment rule and audit flags key the cache. The merged union is
  append-only until `MergedStandards` replaces it. Per-song inputs change only at writes
  that now log the song in `CompositionCatalogService` (recording plus familiarity,
  first-release year, admission route, taxonomy assignment). Resets, loads, migrations and
  audit-phase changes advance an epoch. A call re-tests only logged and newly appended songs,
  and re-filters in merged order only when one enters or leaves.
- **Person pool order** is a sorted array, edited by binary-search insert or remove on each
  join or hire (copy-on-write, so a running enumeration keeps its snapshot). It is rebuilt
  after expiry, reset or load. Enumeration stays deferred, as `OrderBy` was.
- **Replacement search** is one argmax pass over that order; first maximum equals the old
  `OrderByDescending(score).ThenBy(id).First()`.
- `--verify-runtime-caches` recomputes both caches from scratch on every read and throws on
  any difference. It passed on the 1969 window (including departures) and on a 60-week LR
  crossing 1960 to 1961.

Identity: 1969 all 80 CSVs, LR all 82 CSVs and both saved worlds match the previous build.
Worlds are compared by streaming SHA-256 of the decompressed JSON without the `Flags`
array. Never `json.load` a late world: a 1969 world is several GB as JSON.

Timing, 1969 fixture, 4 weeks, three interleaved pairs (HEAD `c7b1883` vs this pass):

| Seconds | HEAD | This pass |
| --- | ---: | ---: |
| Simulation window | 37.19 / 37.47 / 37.58 | 29.07 / 29.09 / 29.77 |
| Competitor week | 10.49 / 10.63 / 13.59 | 5.33 / 5.36 / 5.49 |
| Material selection | 7.42 / 7.46 / 10.43 | 2.46 / 2.47 / 2.54 |

About 22% off the late-world window. Earlier 1969 windows in this file (53-60 s) were
measured under heavy memory pressure; after a restart the same build measured 37 s.

The subsequent RNG pilot has its own explicit behavior boundary and audit. Its
experimental results must not be mixed with these byte-identical measurements.
