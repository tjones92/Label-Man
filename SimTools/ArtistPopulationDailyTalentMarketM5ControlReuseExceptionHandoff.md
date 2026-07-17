# Daily talent-market M6 hardened fail-fast resume handoff

## Authority and preserved history

The owner-approved M5 status remains:

```text
M5: PASS_BY_OWNER_APPROVED_NORMALIZED_CONTROL_REUSE_EXCEPTION
```

The historical 36/45 raw-prefix result is not rewritten. The nine non-prefix-safe terminal/rollup streams remain the documented exception, and `d6-transition-envelope-decade-control-1001` remains the only authorized seed-1001 comparison control.

Preserve both stopped M6-family attempts:

- `d6-daily-market-failfast-decade-enabled-1001` aborted at week 18 because the original validator incorrectly rejected Sultans Records' legal finite terminal debt. Classify it as `M6_ATTEMPT_1: FALSE_POSITIVE_FAIL_FAST_ABORT`; no annual or decade gate is adjudicable.
- `d6-daily-market-failfast-finance-regression-18-1001` stopped in `_Ready()` before week 1 and created no run artifacts because the first repaired loader read positional CSV fields incorrectly. Classify it as `R2_ATTEMPT_1: CONTROL_LOADER_FAILURE`; it is not a simulation or gameplay result.

## Hardened repair now in source

The repair is confined to `SimTools/ChartAuditRunner.cs` fail-fast/control-loading behavior and `SimTools/ArtistPopulationLifecycleProbeSuite.cs` probes. No production finance, label lifecycle, daily market, artist lifecycle, demand, release, Album, format, RNG, retained control, or acceptance band changed.

The validator now:

1. accepts finite terminal debt while still rejecting NaN or infinity in all checked finance fields;
2. validates completed years at captured calendar-year transitions rather than arbitrary `week % 52` boundaries;
3. detects runtime birth-week signing from authoritative signing/re-signing events, avoiding acquisition-transfer false positives;
4. keeps immediate ownership, terminal-roster, lifecycle, operating-target, hard-capacity, impossible-count, and reconciliation checks;
5. retains the inclusive catastrophic annual ratio band `[0.70,1.30]` and the explicit zero-denominator behavior;
6. parses control CSVs by exact header names, never numeric field positions;
7. handles BOMs, quoted fields, and doubled quotes, and rejects missing/duplicate headers, malformed row widths, malformed or non-finite values, and duplicate keys;
8. requires complete 1960-1969 control coverage, including 12 seasonality months and one annual `All`/`All` revenue row per year;
9. derives release years from `release-capacity.csv`'s authoritative `week,year` mapping and joins Album projects through `scheduledWeek`; and
10. reconciles whole-control release and scheduled-Album totals against the independently produced seasonality stream before any simulation may start.

The Album denominator deliberately does not use a naive seasonality-year sum. The final chart week is captured on January 1, 1970 while its sales date is December 31, 1969, so such a sum would incorrectly move week 522 into 1969. Joining projects to the authoritative chart-week year preserves the established annual gate semantics.

## Completed verification

The following checks are complete and must not be rerun unless source/build inputs change:

- `git diff --check` passed.
- `dotnet build "Label Man.sln" --no-restore` passed with only the inherited unused `ChartManager.OnGenreMomentumChanged` warning.
- The complete one-week harness exited 0 with both accepted D5 lines, `D6 fixed probes 1-65 passed`, and `CHART_AUDIT_COMPLETE run=d6-failfast-control-loader-probes-1001 weeks=1`.
- Probe 65 proved reordered headers and quoted fields parse correctly and that missing columns, malformed numerics, incomplete month coverage, unknown project weeks, and cross-file release mismatches fail closed before simulation.
- A real-control no-simulation preflight loaded `d6-transition-envelope-decade-control-1001`, formed complete annual rows for 1960-1969, and emitted:

```text
CHART_AUDIT_CONTROL_PREFLIGHT_COMPLETE control=d6-transition-envelope-decade-control-1001 years=1960-1969
```

The preflight formed the established denominators, including releases/Albums of `4298/1083` for 1960, `3314/2653` for 1968, and `3303/2802` for 1969. It did not execute requested chart weeks or create an R2/R3 artifact family.

Exact verification commands:

```powershell
dotnet build "Label Man.sln" --no-restore

& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-failfast-control-loader-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes

& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --catastrophic-control-preflight --gate-control-run=d6-transition-envelope-decade-control-1001
```

The known post-completion `MissingSingletonsTemp.cs` autoload diagnostic and ObjectDB leak warning remain non-fatal.

## Resume ladder

Do not modify source while executing this ladder. The next action is R2; do not repeat the already-passed probe or preflight unless integrity has changed.

### R2 - bounded week-18 regression retry

Run one enabled 18-week seed-1001 fail-fast regression against the retained control, using a new prefix so the prior pre-week-1 attempt remains unambiguous:

```powershell
& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=18 --run=d6-daily-market-failfast-finance-regression-r2-18-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Require all of the following:

- `CHART_AUDIT_COMPLETE run=d6-daily-market-failfast-finance-regression-r2-18-1001 weeks=18` and no catastrophic-abort marker;
- a header-only catastrophic stream;
- Sultans Records at week 18 is `Defunct` with the expected finite `-116.453125` balance;
- prefix-safe weekly/event streams through week 18 agree with the accepted M4 deterministic family; and
- no ownership, capacity, birth-week, terminal-roster, chronology, or daily-market reconciliation violation.

Terminal snapshots from the 18-week process are not raw prefixes of longer runs and must not be compared as such.

### R3 - resumed M6 decade

Only after R2 passes, launch exactly one new enabled 522-week seed-1001 fail-fast decade. Preserve attempt 1 and use a new prefix:

```powershell
& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-daily-market-failfast-decade-enabled-r2-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Do not add `--lean-probe`, `--aggregate-only`, probe switches, or extra feature/configuration arguments.

The fail-fast remains catastrophic-only:

- genuine structural corruption or non-finite/impossible numeric state aborts immediately;
- completed calendar years abort only outside `[0.70,1.30]`;
- ordinary inherited-band misses inside that emergency band continue;
- an abort must flush artifacts and emit `CHART_AUDIT_ABORTED_CATASTROPHIC`; and
- normal completion requires `CHART_AUDIT_COMPLETE ... weeks=522`.

If R3 completes, adjudicate every inherited annual and decade gate, including the known 1968/1969 release, scheduled-Album, and deferred Single-yield surfaces. This repair changes no final gate.

## Stop and fallback boundary

Stop at the first genuine R2 or R3 gameplay/catastrophic failure, or after R3 completes and is analyzed. Do not run seeds 1002/1003, a holdout, a replacement control, the deferred Single-yield correction, or another behavioral candidate.

If the hardened loader/preflight itself produces another false failure on unchanged, preflight-passing inputs, do not patch it a third time in this ladder. Record the exact evidence, drop the optional fail-fast wrapper for this candidate, and request owner authorization to run the same R2/R3 simulation configuration without `--catastrophic-fail-fast` and `--gate-control-run`. Genuine simulator exceptions or structural violations are not covered by that fallback and must still stop the ladder.

Append the resumed outcome to `ArtistPopulationLifecycleAudit.md`, including the exact command, source-integrity evidence, completion/abort marker, and every adjudicable gate. Any production, control, or acceptance change requires a new explicit handoff.
