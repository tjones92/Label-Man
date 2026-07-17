# Historical regional scouting validation handoff

## Objective and authority

Validate the newly authorized historical scouting policy with exactly one enabled 104-week seed-1001 checkpoint and, if that checkpoint is structurally sound, return directly to the stopped R3 seed-1001 decade.

This is a validation ladder, not a calibration loop. Do not add fixed probes, an enabled repeat, a fresh control, exploratory scalar runs, seeds 1002/1003, or a holdout. Do not modify source between the 104-week checkpoint and the decade. The retained `d6-transition-envelope-decade-control-1001` remains the authorized control.

## Preserved history

The prior hardened R3 attempt, `d6-daily-market-failfast-decade-enabled-r2-1001`, correctly aborted at week 470 / January 3, 1969 after the completed 1968 release gate observed 2,045 enabled successful releases versus 3,314 control, ratio `0.617079`, below the catastrophic `0.70` floor. No final-decade gate was adjudicable.

The owner then authorized a production scouting-policy change. `Systems/RosterManager.cs` now:

1. uses the available regional pool for a regional discovery pass even when that pool is smaller than the desired slate;
2. considers national fresh supply only after the regional fresh slate yields no affordable candidate clearing the established `0.30` floor;
3. selects among qualifying affordable candidates instead of taking the deterministic global maximum; and
4. uses an isolated SplitMix-style stream seeded by simulation seed, label, vacancy generation, persisted appointment ordinal, and lane/scope domain. `scoutingAbility` changes the strength of the score bias but never guarantees the top candidate.

The production-source hash at authorization is:

```text
Systems/RosterManager.cs=21B13D09CEB69A7991350210A4B156B1CB0D459A69A912C521B3988F950A5EDB
```

The implementation checkpoint is already complete and must not be repeated unless this source hash or build inputs change:

- `git diff --check` passed;
- `dotnet build "Label Man.sln" --no-restore` passed with only the inherited unused `ChartManager.OnGenreMomentumChanged` warning; and
- `d6-historical-scouting-disabled-52-1001` completed and matched all 45/45 CSV streams of `d6-transition-envelope-disabled-52-1001` byte-for-byte by suffix and SHA-256.

This disabled equality proves that the new isolated selection stream does not perturb the frozen dual-disabled path. It does not prove enabled outcomes or collision reduction.

## Execution freeze

Before starting, confirm the `RosterManager.cs` hash above and run `git diff --check`. If either fails, stop; do not substitute a new source manifest inside this ladder. Do not rerun the build or disabled replay when the recorded inputs are unchanged.

Use the same Godot console executable and workspace used by the completed R2/R3 ladder. The known post-completion `MissingSingletonsTemp.cs` autoload diagnostic remains non-fatal when the requested completion or catastrophic-abort marker and flushed artifacts are present.

## V1 — one enabled 104-week checkpoint

Run exactly one enabled seed-1001 checkpoint:

```powershell
& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-historical-regional-scouting-enabled-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Do not add `--lean-probe`, `--aggregate-only`, probe switches, or other feature/configuration arguments.

### V1 go/no-go checks

Proceed to V2 only if all of the following hold:

- the process emits `CHART_AUDIT_COMPLETE run=d6-historical-regional-scouting-enabled-104-1001 weeks=104` and no catastrophic-abort marker;
- the catastrophic stream is header-only;
- ownership conflicts, duplicate roster/pool membership, terminal artists rostered or release-eligible, runtime birth-week signing violations, operating-target overshoots, hard-capacity overshoots, and impossible/non-finite counters remain zero;
- daily appointment accepted offers reconcile exactly to daily aggregate accepted offers, and detail collision losses reconcile exactly to daily aggregate collision losses;
- runtime-founded labels have nonzero due appointments and nonzero accepted offers, with no signing in their birth week; and
- successful releases and scheduled Albums for both 1960 and 1961 remain inside the catastrophic `[0.70,1.30]` band against the retained control.

The retained control denominators for the two checkpoint years are:

| Year | Successful releases | Scheduled Albums |
|---:|---:|---:|
| 1960 | 4,298 | 1,083 |
| 1961 | 3,880 | 1,257 |

Assign Album projects to years by joining `album-projects.csv.scheduledWeek` to the authoritative `release-capacity.csv` week/year map, matching the hardened control-loader semantics. Do not use a naive sales-date or seasonality-year sum.

Report the ordinary inherited release, Album, format, and economic bands at V1, but an ordinary miss that remains inside the emergency `[0.70,1.30]` envelope is diagnostic and does not by itself block V2. This checkpoint exists to catch structural breakage or obvious early collapse; the requested question is whether the scouting policy repairs late-decade staffing and release capacity.

### Required V1 scouting measurements

Measure, without adding telemetry or changing source:

- due labels, nominations, unique nominated artists, accepted offers, collision artists, collision offers, and collision losers;
- runtime-founded appointments, accepted offers, ending rostered labels/artists, and any runtime-founded release outcomes;
- first-time signings versus re-signings; and
- vacancy/empty-label state at week 104, split by launch versus runtime label origin where existing streams permit it.

For collision context only, compare the new 104-week aggregates with the accepted pre-change M4 family `d6-daily-market-enabled-104-1001`:

| Measure | Pre-change M4 |
|---|---:|
| Due labels | 8,027 |
| Nominations | 6,248 |
| Unique nominated artists / accepted offers | 2,134 |
| Collision artists | 897 |
| Collision offers | 5,011 |
| Collision losers | 4,114 |
| Runtime appointments | 151 |
| Runtime accepted offers | 81 |

Collision movement is diagnostic, not a gate. Do not claim reduction unless the observed counts and normalized rates support it. Do not reject a staffing improvement merely because one raw collision count increases with a larger nomination volume; report collision offers per nomination, losers per nomination, and unique nominations per nomination.

If V1 fails a structural, reconciliation, runtime-entry, completion, or catastrophic requirement, stop and append the exact evidence to `ArtistPopulationLifecycleAudit.md`. Do not tune the selection weights, floor, slate size, cadence, formation, releases, Albums, or finance under this handoff.

## V2 — return to R3 decade

If V1 passes, immediately launch one new enabled 522-week seed-1001 fail-fast decade from the exact same source and configuration:

```powershell
& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-historical-regional-scouting-failfast-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Do not generate a replacement control. The already-passed 45/45 disabled boundary and unchanged retained-control exception authorize this comparison.

The hardened fail-fast semantics remain unchanged:

- structural corruption, impossible state, or non-finite checked values abort immediately;
- a completed calendar year aborts only when an annual treatment/control ratio leaves inclusive `[0.70,1.30]`;
- ordinary inherited-band misses inside the emergency band continue to the next year;
- a catastrophic stop must flush artifacts and emit `CHART_AUDIT_ABORTED_CATASTROPHIC`; and
- normal completion requires `CHART_AUDIT_COMPLETE ... weeks=522`.

If V2 aborts, record the first genuine failure and stop. Do not restart R3 from the same source. If V2 completes, adjudicate every inherited annual and decade gate, with explicit attention to:

- 1968 and 1969 successful releases;
- 1968 and 1969 scheduled Album projects;
- runtime-founded active, rostered, release-eligible, and releasing-label contribution;
- organic-growth events and their blockers;
- Single-yield ratios, which remain a separate deferred surface and must not be explained away by scouting; and
- all ownership, lifecycle, chronology, finance, capacity, format, and economic invariants.

Also report decade collision rates and candidate-choice concentration against the pre-change daily-market artifacts where comparable. These are measurements, not assumed benefits and not permission to retune scouting in the same pass.

## Terminal boundary

Stop after the first V1/V2 hard failure or after V2 completes and is fully analyzed. Do not run an enabled repeat, seeds 1002/1003, a holdout, a new control, the deferred Single-yield correction, or another behavioral candidate.

Append V1 and, if reached, V2 to `ArtistPopulationLifecycleAudit.md` with the exact command, source-integrity evidence, completion/abort marker, control denominators, adjudicable ratios, scouting/collision measurements, and explicit statement of what remains unadjudicable. Any production, telemetry, control, acceptance, or configuration change requires a new owner instruction.
