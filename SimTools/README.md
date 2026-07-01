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

When `--seed` is present, the first autoload applies it before any population
generation. The runner reapplies the same seed after startup/prewarm so both the
initialized world and measured 52-week period are reproducible across processes.
