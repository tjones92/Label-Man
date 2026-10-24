# Headless chart realism audit

This removable harness runs the real Godot autoload chain. It waits for normal
autoload initialization and prewarming, then advances `TimeManager` one week at
a time. CSV files under `SimLogs/` are scratch output and are gitignored.

**Where output goes.** `SimTools/` holds source: runners, checks, analysis scripts,
directives and hand-written handoffs. Everything a run or an analysis script
*produces* (validation JSON, hash manifests, checkpoints, census CSVs, generated
reports, logs, before-edit snapshots) is written under `SimLogs/` and is not
committed. New JSON under `SimTools/` is gitignored to enforce this.

`--label-release-slate` adds dated single/LP intentions, prepared masters and cash
holds; `--label-release-capacity` adds multiple weekly slots and a conserved label
promotion pool. Both retain cadence and retention. The active working bundle
(`SimLogs/working-bundle/plan.json`) enables both plus `--studio-parent-labels`
by user decision; economy/statistics will be reassessed on this complete base.
The prior bundle is archived alongside it as `mtn5-pair-plan.json`.
`RunLabelReleaseProgrammeAudit.ps1` runs allocation × capacity with studio labels
on and off, explicitly removing those three treatments from the saved working
bundle to reconstruct its untreated control. Its default is one seed, 53 weeks;
longer validation follows candidate review. `analyze_label_release_programme.py --plan SimLogs/TAG/plan.json
--output SimLogs/TAG/review.json` validates the complete screen and reports annual
issues, concentration, conversion, costs and paired factorial effects. See
[LabelReleaseProgrammeImplementation.md](LabelReleaseProgrammeImplementation.md)
for parameters, save v28 and finance boundaries.

Through `SaveLoadRoundTripRunner.tscn`, `--label-release-ownership-check` probes
subsidiary continuity and protection of slate cash from payouts/capital purchases.
`--label-release-ownership-roundtrip --weeks=53` searches up to 53 weeks for a
funded independent with a pending LP and a prepared slate master, forces the
existing absorption path, checks whole-world and gzip save/load, then resumes
through LP delivery under its retained imprint. Use the complete working bundle;
run both studio settings. These are forced integration scenarios, not evidence
on acquisition frequency or historical consolidation calibration.

`--distribution-funding-check` exercises protected advance quotes, atomic
buyout/replacement payments, voluntary exits, financing accounts and stale
player-offer acceptance. `--distribution-funding-roundtrip --weeks=13` stages
a funded incumbent advance and Major buyout in a live world with a prepared
slate master, checks full-world and gzip save/load, then resumes recoupment.
Use the full working bundle with studio labels both off and on. The transaction
is forced for validation; it does not measure late-decade poaching frequency.

`--distribution-ownership-check` checks ownership and distribution paths together,
including direct, nested and mixed dependency loops. Under the programme, offer
selection, signing, replacement and absorption reject a path back to the client.
Acceptance also rechecks whether the client has become a subsidiary, studio
division or a tier that cannot sign. Rejected transactions preserve existing
contracts, claims, cash and prepared work; stale player offers remain pending.
Run with the working bundle and studio labels both off and on.

`--lp-assembly` (with the slate) builds each slate LP as an ordinary, hit-led or
greatest-hits proposition from masters the label controls, shelved expired
masters and new sessions, replacing the compilation lottery (save v30).
`--lp-assembly-check` runs its native assertions; `--lp-assembly-roundtrip
--weeks=26` checks every assembled master against its plan in a live world,
then save/load and resume. Opening acts' pre-1960 singles are seeded as controlled masters;
`--lp-assembly-package-gap=104` spaces packages per act. Telemetry: `*-lp-assembly.csv`. See
[LabelReleaseProgrammeImplementation.md](LabelReleaseProgrammeImplementation.md).

`--slate-work-budget=S` (with the slate and revised capacity) makes a non-Major label's
planned intentions its whole work budget: S singles a month per unit of `releasesPerMonth`,
with no separate preparation or issue draw, and a Struggling label halves its campaign
spend instead of its slots. `--slate-work-budget-check` runs its native assertions;
`analyze_work_budget.py --control TAG:WEEKS --arm NAME=TAG:WEEKS` gives the paired
screen, including the singles purse's rationing factor r. `--slate-work-budget-midtier=SM`
gives MidTiers their own budget. `--slate-going-concern-reserve` tests a work-budget MidTier's slate
reservation, session and issue against tax due within two month-ends, not the full tax-and-service hold.
A budget run writes `*-programme-cash-shortfalls.csv`, one row per refused reservation with its reserve parts.

The first demand/attention measurement slice appends explicit before/after-marketing
contribution fields to release outcomes and local single/LP clearing totals to
market telemetry. `analyze_demand_attention.py PREFIX ... --output SimLogs/REPORT.json`
validates their accounting identities and reports format rationing and finalized
release losses. Actual-release census streams now separate opening catalog from
new single and LP launches, including delayed LP drops. Use `--launch-through-week N`
to follow a comparable early launch cohort through the full run; active records
remain counted and lifetime loss rates remain unknown until the cohort completes.
See [DemandAttentionImplementation.md](DemandAttentionImplementation.md)
for definitions, limitations and remaining Stage A work.

The AI company model is enabled by default. It replaces aggregate overhead with
department staffing and premises, pays commercial channel costs and record
excise, accrues income tax, routes ownership payouts, and charges for production
upgrades. The full bundle also selects the corrected advance recovery, royalty
tiers, mechanical publishing and period prices. Use `--no-label-corporate-model`
for the legacy economy; `--label-corporate-model` explicitly selects the full
bundle. Individual research arms remain available.
`RunCorporateEconomyAudit.ps1` runs matched arms;
`analyze_corporate_economy.py` validates monthly cash and company earnings.
Run 53 weeks for a complete accounting year, including the December close.
See [LabelCorporateModelImplementation.md](LabelCorporateModelImplementation.md)
for cost boundaries, historical sources, assumptions, individual switches and
calibration results. Native financial checks use `--label-company-check` with
`SaveLoadRoundTripRunner.tscn`.

Validate the actual game defaults with
`./SimTools/RunCorporateEconomyAudit.ps1 -RunTag NAME -Profile game
-Modes @('default') -Seeds @(1001,2002,3003,4004,5005) -Weeks 265`.
The runner's `audit` profile (its default) retains the broader research stack
and 1.5-forecast/twelve-week pressing scenario; use `game` for adoption checks.
Starting-Small closures use the equal-weight multi-seed mean against the 20–35%
design band, with individual outliers disclosed in the calibration record.

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

Economy-audit retuning keeps the full A1-A5/B1-B7/C1-C4 correction stack enabled.
`RunEconomyRetune.ps1 -RunTag NAME -Seeds 1001,2002,3003 -Modes larger -Weeks 260`
runs serial, unbroken five-year worlds and records arguments, assembly hash and
native completion in `SimLogs/NAME/runs.json`. `control` retains the original
pressing assumptions; `commitment` tests one lifetime forecast at launch and
eight weeks per repress; `larger` tests 1.5 forecasts and twelve weeks.
These are inventory-risk model assumptions, not sourced plant tariffs.
The individual controls are `--pressing-initial-forecast-share=` (0.5-2,
default 0.5) and `--pressing-repress-weeks=` (3-26, default 3).
They affect new orders and their cash reserves, leaving opening stock fixed.
Resumed experiments must pass the same parameters; the actual pressing book
continues through normal world serialization.

`analyze_economy_retune.py PREFIX ... --weeks 260 --review-manifest
SimLogs/NAME/runs.json --output SimLogs/NAME/review.json` refuses incomplete
cohorts, repeated seeds and mixed configurations. Its acceptance target is
20-35% financial closure of starting-Small firms at five years, for each of at
least three seeds. It also reports exits before first promotion, inventory,
all-in release outcomes, cash and chart diagnostics. Cash and margins are
diagnostic by the user's October 20 decision. `--pressing-commitment-check`
through `SaveLoadRoundTripRunner.tscn` probes reserves, invoices, saved stock,
opening inventory and RNG; `test_economy_retune_analysis.py` guards the review.

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

`--promo-synergy-feedback` is the opt-in A5 test of the promo-single album credit.
Each linked LP carries a counterfactual without the promo's launch awareness bonus
or cannibalization. At retirement, recruited (actual minus counterfactual raw
demand) and suppressed units are compared with the credit the project was
scheduled on. Recency-weighted market ratios by career cohort, shrunk toward the
pooled ratio and then toward 1, scale the awareness-lift/synergy credit and the
modelled diversion. Evidence-carrying worlds require save v26.
`--promo-synergy-feedback-check` covers the estimator, counterfactual identity,
finalization and serialization. The runs add `*-promo-synergy-decisions.csv` and
`*-promo-synergy-outcomes.csv`; `analyze_promo_synergy.py RUN --control OFF`
checks their identities and compares strategy mix and chart breadth.

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
