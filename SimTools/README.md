# Headless chart realism audit

This removable harness runs the real Godot autoload chain. It waits for normal
autoload initialization and prewarming, then advances `TimeManager` one week at
a time. CSV files under `SimLogs/` are scratch output and are gitignored.

**Where output goes.** `SimTools/` holds source: runners, checks, analysis scripts,
directives and hand-written handoffs. Everything a run or an analysis script
*produces* (validation JSON, hash manifests, checkpoints, census CSVs, generated
reports, logs, before-edit snapshots) is written under `SimLogs/` and is not
committed. New JSON under `SimTools/` is gitignored to enforce this.

The adopted 1960 concentration review uses **weekly Top 10 slots**, with bands
of **23-33% for the leading four firms** and **47-57% for the leading eight**.
`analyze_label_tier_historical_benchmarks.py PREFIX ... --review-manifest MANIFEST
--output SimLogs/REPORT.json` reviews equal-weight means from at least three
distinct seeds in one completed configuration. Supply the `runs.json` emitted by
`RunSmallCostPilot.ps1` and exactly its run prefixes. The manifest records the
assembly and flags; partial years, repeated seeds and mixed configurations are
rejected. Out-of-band means produce advisory `historicalWarnings`, not a native
simulation failure. Two seeds remain `INSUFFICIENT_SEEDS`. These are design review
bands, not historical confidence intervals. The ownership grouping remains an
operating-label proxy, so the analysis refuses runs containing absorptions.
The existing `c4ChartShare` / `c8ChartShare` fields continue to measure chart
**units** and are not used for this review. See `HistoricalLabelTierBalanceResearch.md`
for sources and interpretation.

Example (PowerShell):

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=baseline-1 --seed=1001
```

For short performance windows, `RunSimulationBenchmark.ps1 -Run NAME -Weeks 8
-ResumeWorld WORLD` restores the bookmarked world's own flags, records its assembly
hash and process time, and writes phase timings even below 52 weeks. Omit
`-ResumeWorld` for a fresh world; use `-ExtraArgs` for a research configuration
and `-SaveWorld NAME` to retain the final state. `-Detailed` retains the normal
diagnostic streams. Use a fresh run and output-world name each time. World
bookmarks live in `SimLogs/worlds`; their creation flags and date are retained.

`compare_simulation_benchmarks.py SimLogs/BEFORE SimLogs/AFTER` checks every
non-timing CSV, including empty/missing streams. Add `--before-world PATH
--after-world PATH` to also hash and compare the complete decompressed saves.
Pass `--output SimLogs/RESULT.json` to retain evidence. A resumed control must
resume the same world as its treatment: loading reseeds the global RNG, so its
continuation is not the original uninterrupted trajectory. Older bookmarks
provide scaling workloads; fresh runs with the current research flags establish
current-version identity.

The editor/headless Debug assembly now enables compiler optimization while
retaining symbols. `dotnet build 'Label Man.sln' -p:Optimize=false` restores
unoptimized debugging when source stepping requires it. The build option does
not change any simulation settings.

For long pool-stability runs, add `--aggregate-only` to omit the large
per-record stream while retaining weekly stock/flow and lifecycle telemetry.

When `--seed` is present, the first autoload applies it before any population
generation. The runner reapplies the same seed after startup/prewarm so both the
initialized world and measured 52-week period are reproducible across processes.

Dealer-price pressing, plant credit, the regional-hit deal gate and slower
wholesale spread now default on. Their old comparison paths use
`--no-dealer-price-pressing`, `--no-plant-credit`,
`--no-regional-hit-deal-gate` and `--no-slow-wholesale-spread`.
Sourced single/LP pressing costs and home-only Small starting territory also
default on after the five-year validation. `--legacy-pressing-costs` (alias
`--legacy-single-costs`) retains the old bundled costs, and
`--no-small-home-territory` restores the former extra starting regions. See
`SmallLabelAttritionAndChartMethod.md` for assumptions and results.

`RunSmallCostPilot.ps1` starts with short matched cost/territory comparisons.
Its `-Compact` option uses `--calibration --small-label-finance`: weekly label
finance, charted singles and chart-life rows stay available while the large
diagnostic streams are suppressed. Both all-firm starting-cohort closures and
financial exits before first promotion are reported by
`analyze_small_cost_pilots.py`; these are different outcomes.

The runner also writes `*-label-finance.csv` with weekly gross, COGS,
distribution skim/income, artist royalty, net revenue, cash, and status. Use
`--force-distribution-deal` for the dormant deal-routing assertion or
`--disable-label-lifecycle` to isolate finance changes from lifecycle processing.

With artist population lifecycle enabled, `--mid-tier-promotion-audit` adds
`*-mid-tier-promotion-gates.csv`: every Independent's exact quarterly review,
including unsuccessful reviews, before promotion resets its capability streak.
It records charting, roster, operating months, reach, dependency, the streak,
status, losses, profit and runway, together with the actual eligibility result.
`analyze_mid_tier_promotions.py PREFIX ... --output SimLogs/REPORT.json` streams
these rows, counts overlapping blockers, and checks that eligible decisions match
the Independent-to-MidTier transitions. `--mid-tier-promotion-audit-check` through
`SaveLoadRoundTripRunner.tscn` verifies observation before the streak reset.

`--single-prior-recalibration` is the opt-in A2 prior for live genre-market
decisions with dealer pricing and period single costs. It replaces the stale
quartiles with a continuous quality curve that is monotone in quality and career.
`--single-prior-recalibration-check` through `SaveLoadRoundTripRunner.tscn`
checks the decision seam, continuity, career ordering and price/cost accounting.
`calibrate_single_prior.py TRAINING --holdout HOLDOUT --output SimLogs/FIT.json`
reproduces the curve using completed standalone releases, with a default launch
cutoff of week 418 for the 522-week source runs. It uses the Python standard
library and refuses incomplete fitting/evaluation cohorts. See the A2 follow-up
in `EconomyReleaseJudgementAudit-2026-10-20.md` for source runs and limitations.

`--career-banded-format-memory` is the opt-in memory portion of A3. Live format
decisions read observations from their own career cohort, using immutable launch
or project-scheduling context. Cohorts 0-3 are New/Unsigned, Rising, Established
and Star/Superstar; cohort 4 separates legacy fallback states such as Declining.
An unsupported cohort keeps the prior with zero correction confidence. Old
untagged history is not assigned a band from an artist's later career state.
Tagged worlds require save v24, including when the switch is later turned off.
The off path for worlds that never used this switch retains its existing schema.
`--career-banded-format-memory-check` checks grouping, caching and serialization.
The optional `*-format-memory-career-revisions.csv` also works in calibration
mode. `analyze_career_format_memory.py PREFIX ... --output SimLogs/REPORT.json`
checks stable tags and summarizes observed groups without loading whole worlds.
Album-prior stature calibration and debut-LP market economics remain A3 work.

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
