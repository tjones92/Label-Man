# Headless chart realism audit

This removable harness runs the real Godot autoload chain. It waits for normal
autoload initialization and prewarming, then advances `TimeManager` one week at
a time. CSV files under `SimLogs/` are scratch output and are gitignored.

Example (PowerShell):

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=baseline-1 --seed=1001
```

For long pool-stability runs, add `--aggregate-only` to omit the large
per-record stream while retaining weekly stock/flow and lifecycle telemetry.

The seed is applied after the normal startup/prewarm because the runner scene is
created after autoloads. Separate processes therefore provide independent
startup populations; the explicit seed makes the measured 52-week portion
repeatable given that initialized state.
