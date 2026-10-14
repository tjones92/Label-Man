# Headless chart realism audit

This removable harness runs the real Godot autoload chain. It waits for normal
autoload initialization and prewarming, then advances `TimeManager` one week at
a time. CSV files under `SimLogs/` are scratch output and are gitignored.

**Where output goes.** `SimTools/` holds source: runners, checks, analysis scripts,
directives and hand-written handoffs. Everything a run or an analysis script
*produces* (validation JSON, hash manifests, checkpoints, census CSVs, generated
reports, logs, before-edit snapshots) is written under `SimLogs/` and is not
committed. New JSON under `SimTools/` is gitignored to enforce this.

Example (PowerShell):

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=baseline-1 --seed=1001
```

For long pool-stability runs, add `--aggregate-only` to omit the large
per-record stream while retaining weekly stock/flow and lifecycle telemetry.

When `--seed` is present, the first autoload applies it before any population
generation. The runner reapplies the same seed after startup/prewarm so both the
initialized world and measured 52-week period are reproducible across processes.

The runner also writes `*-label-finance.csv` with weekly gross, COGS,
distribution skim/income, artist royalty, net revenue, cash, and status. Use
`--force-distribution-deal` for the dormant deal-routing assertion or
`--disable-label-lifecycle` to isolate finance changes from lifecycle processing.

Scene Phase 4 recruitment is opt-in with `--enable-scene-recruitment`. It requires
persistent scenes, artist population lifecycle and genre market; observe-only or
explicit contradictory disable modes fail before scene configuration changes.
Rooms keep their existing development default. Distribution deals do not grant
national A&R access. Legacy contracts and places remain unchanged.

`RunSceneRecruitmentChecks.ps1` runs two seeded fixed probes, two 26-week world/gzip
roundtrips and the existing room compatibility checks serially. `-FixedOnly`
omits the long roundtrips. `RunSceneRecruitmentAudit.ps1` runs a 52-week recruitment
off control plus two enabled seeds. Both helpers require a fresh successful build,
refuse an active game and write hashes, arguments, logs and results to `SimLogs`.
Use a new `-RunTag` for every invocation.

`--scene-recruitment-audit` writes contract-time route evidence and label access/
vacancy censuses. `analyze_scene_recruitment.py MANIFEST --baseline-root ROOT`
compares against the frozen Phase 3 `scene3-matched-v1-rooms` outputs, checks all
84 economic CSVs for the off control and all nine integrity counters. Enabled
economic changes are descriptive; no new acceptance tolerance is inferred. Use
the bundled Python runtime when the system Python launcher has no installed runtime.
