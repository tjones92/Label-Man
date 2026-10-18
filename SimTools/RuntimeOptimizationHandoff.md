# Runtime optimization handoff — 2026-10-18

Branch `media-atmosphere`. Picks up after Codex's `SimulationRuntimeOptimizationAudit.md` and
`RoomsMaterialOptimizationAudit.md`; measurements and methods are in the latter.

## Done (committed)

| Commit | What | Effect |
| --- | --- | --- |
| `8a12607` | Room/material exact pass (Codex's work plus `Present` and `SetFor` basis) | LR window −16.7% then a further −5%; room calendar −14% |
| `c7b1883` | Opt-in keyed regional-sales RNG pilot (`--regional-sales-rng=keyed-v1`), save v22 only when keyed | Legacy byte-identical; see `RegionalSalesRngPilotAudit.md` |
| `e8fba9b` | Exact incremental caches: standards pools (catalogue change log), person-pool order, argmax replacement | 1969 window 37.4 → 29.3 s (−22%) |

All exact: every CSV plus the saved world (minus its `Flags` array) matches on LR seed 1001
and the 1969 fixture. `--verify-runtime-caches` recomputes the caches from scratch and throws on any
difference. It passed on 1969 and on a 60-week LR crossing a year boundary.

## Uncommitted at pause

- `SimTools/ChartAuditRunner.cs`: profile CSV gains `gcPauseSeconds, gen0/1/2Collections,
  allocatedMB, heapMB, committedMB` (timing file only; excluded from identity).
- `SimTools/AuditGcEnvironment.ps1` plus a dot-source line after the `param` block of 26 runners
  (all `Start-Process` runners and `RunSimulationBenchmark.ps1`). It sets
  `DOTNET_gcServer=1 DOTNET_GCHeapCount=4`. Opt out with `LABELMAN_WORKSTATION_GC=1`. Verified: the
  benchmark ran 43 gen0 collections in 8 weeks under server GC.
- This file.

## Key findings

- **GC is ~16% of a late window.** 1969 allocates ~1.8 GB per simulated week on a ~2 GB live heap.
  Server GC (4 heaps) cut 1969 −15% (3 interleaved pairs) and LR −9%, byte-identical. Committed
  memory +190 MB late, +600 MB early. Godot's editor binary ignores the project runtimeconfig
  (it reads `GodotSharp/Api/Debug/GodotPlugins.runtimeconfig.json` in the install), so the
  `<ServerGarbageCollection>` csproj property does nothing for audits. Hence the env vars.
  Ad-hoc launches (bash scripts, Codex one-offs) must set the two env vars themselves.
- **Memory pressure dominates timing on this 7.2 GB machine.** The same 1969 build measured
  59 s under pressure and 37 s after a restart. Only interleaved runs with prebuilt DLLs
  swapped into `bin/Debug` are trustworthy. Discard the first run of a batch.
- **Never `json.load` a late world.** That froze the PC. Compare worlds by streaming SHA-256 of
  the decompressed JSON without the top-level `Flags` array (it sits at byte ~90).
- Per-week cost by fixture on the current build: 1960 1.1 s (717 albums), 1963 3.0 s (3,869),
  1969 7.3 s (9,502). Wall time tracks album count about linearly. The 1963 fixture lacks
  band-life flags, so its comparison with 1969 is confounded.
- The keyed RNG pilot cannot show A/B variance gains yet: `gen_N`/`artist_N` IDs are creation
  counters and sales draws are unconditional (see its audit).

## Next targets (1969 trace `SimLogs/perf11-1969trace-cpu.json`, pre person-pool fix)

1. **Heap and allocations.** Find what fills the 2 GB live heap and the 1.8 GB/week of allocation.
   This needs `dotnet-gcdump` (not installed; ask before downloading), or per-phase
   `GC.GetAllocatedBytesForCurrentThread()` deltas in `SimulationPerformanceProfiler`. A smaller heap
   helps GC, paging and the player's game, which runs workstation GC.
2. **Album creation:** `TryInitiateAlbumProject` 17% of the 1969 window, mostly
   `GenerateAlbum` → per-track `ChooseMaterial` (7–9%; now diffuse: `PolarCoverResolver.Propose`,
   `PolarSongBehavior.Reference`) and `ApplyIdentityToAlbumTrack` (2.8%).
3. **Room `Schedule`:** 20% of the 1969 window. Set-list `StableSmallSelection.Take` hashes every
   pool song per bill. Also house-band `Pick` per-musician string keys and member eligibility.
4. **Year-boundary only:** `HasAnyWriterCredit` scans the whole writer-stint ledger per departing
   member (~16% of a window containing a year boundary). Exact fix: index personIds with
   `songs > 0`. About 90 s per decade.
5. **Non-exact options (need economy-bound validation; the author requires no loss of player-
   facing historical fidelity):** reduced update cadence for deep-catalog off-chart albums
   (9,302 of 9,502 in 1969 are off-chart, none inert). Not started; measure first.

## Commands

```bash
# 1969 fixture, 4 weeks (args as in ab69/gcvar scripts; resume world bms4e-intent90-1001-1969)
powershell -NoProfile -File SimTools/RunSimulationBenchmark.ps1 -Run <name> -Weeks 8
```

Build before every run: `dotnet build "Label Man.sln" -c Debug`. Godot does not rebuild on its own.
