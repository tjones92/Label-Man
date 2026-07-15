# Artist population economic-yield investigation handoff

## Mission and authority

Identify and isolate the systemic causes of the remaining Directive 6 economic failure after the label release-capacity repair, without reopening the now-passing release and Album capacity surfaces.

This is the authoritative next-pass handoff for Codex. It supersedes the validation ladder in `ArtistPopulationRosterThroughputNormalizationHandoff.md` after the systemic label-capacity checkpoint, while preserving that handoff, the earlier market-clearing handoffs, and `ArtistPopulationLifecycleAudit.md` as historical evidence.

The current worktree contains an uncommitted but validated removal of calendar-year release growth, D6 fixed probe 53, `SimLogs/.gdignore`, and the matching audit checkpoint. Do not reset or discard those changes. The current source is not a fully accepted Directive 6 candidate because the 1961 economic ratios remain outside `[0.90,1.10]`.

This handoff authorizes:

1. read-only source and artifact attribution;
2. a removable offline analyzer over existing CSV families;
3. narrowly scoped fixed probes or opt-in diagnostic telemetry needed to resolve comparator semantics;
4. one paired 104-week seed-1001 label-lifecycle-disabled diagnostic;
5. if still necessary, one 104-week seed-1001 treatment-only initial-reserve boundary diagnostic; and
6. documentation of the result and a concrete recommendation for the next authorized correction.

It does **not** authorize a decade, later seed, holdout, acceptance-band change, market-demand repair, economic-constant retune, label-lifecycle threshold change, pool-size sweep, or compensating hit-inventory penalty. Stop after the bounded diagnostics and request explicit authority for any behavior-changing repair not already identified as a direct implementation defect.

## Current checkpoint

The systemic label-capacity repair removed `AnnualReleaseGrowthRate` from `CompetitorManager.CalculateWeeklyReleaseChance`. Weekly release opportunity now depends only on explicit `releasesPerMonth`, label status, eligible-artist availability, and optional seasonality, with a final `[0,1]` clamp.

The fresh seed-1001 104-week pair reports:

| Year | Control releases | Enabled releases | Ratio | Control scheduled Albums | Enabled scheduled Albums | Ratio |
|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 4,313 | 4,459 | 1.0339 | 1,090 | 1,181 | 1.0835 |
| 1961 | 3,917 | 3,993 | 1.0194 | 1,307 | 1,250 | 0.9564 |

Both capacity measures are inside `[0.85,1.15]` in each year. Do not change release cadence, Album choice, project scheduling, release cooldowns, or selection priority in this pass.

The remaining 1961 economic ratios are:

| Measure | Enabled / control | Gate |
|---|---:|---:|
| Units | 1.1622 | Fail |
| Gross | 1.1641 | Fail |
| Label net | 1.1709 | Fail |
| Market net | 1.1610 | Fail |

The common movement across units and every revenue surface means the primary excess is upstream sales yield. Gross per unit differs by only about `0.16%`; label-margin differences add less than another `1%`. Do not begin with royalty, price, COGS, skim, advance, overhead, or finance-ledger changes.

## Decisive attribution already established

### Stable release count, higher Single yield

For 1961 Singles:

| Measure | Control | Enabled | Ratio |
|---|---:|---:|---:|
| Releases | 3,917 | 3,993 | 1.0194 |
| Units | 147,214,747 | 170,874,579 | 1.1607 |
| Units per release | 37,584 | 42,794 | 1.1386 |

Mean quality among new 1961 Singles rises from approximately `0.5895` to `0.6069`, or `1.0295x`. `ChartSimulator.CalculateRegionalSales` raises quality to the fourth power. Holding other inputs fixed, that small quality shift implies approximately `1.1233x` demand, close to the observed `1.1386x` per-release yield ratio.

The enabled 1961 A3 decision population also contains 1,024 Q4 Single decisions versus 710 in control. Treat release-quality composition as a first-order cause, not a minor descriptive difference.

### Dropped-control output versus viable enabled replacements

The current control artifacts contain 2,048 1961 A3 decisions with `careerState = Dropped`, 53.3% of all control decisions. Control `records.csv` contains 2,254 active 1961 Single records with `launchCareerState = Dropped`; they contribute 26.84 million units, about 11.9 thousand per record. The enabled artifacts contain no corresponding Dropped launch population.

Enabled NewSigning records contribute 39.89 million more Single units than control. Netting the absent Dropped output against the additional NewSigning output explains about 13.05 million of the 23.66 million Single-unit increase. Higher Rising and Established yield explains most of the balance.

This is also a comparator-integrity question. Current source defines `Dropped`, `Disbanded`, and `Retired` as terminal release states and routes live Genre Market release eligibility through the terminal guard, yet the inherited control artifacts contain thousands of Dropped decisions. Determine whether this is:

- an expected legacy compatibility behavior;
- a stale or differently configured control lineage;
- event-order behavior between chart completion, roster reconciliation, and release selection;
- pending-project telemetry whose state is being interpreted as launch state; or
- a current implementation or telemetry defect.

Do not assume the answer and do not silently replace the accepted disabled compatibility boundary.

### The enabled initial reserve dominates replacement supply

The enabled path preserves the original 3,000-artist launch allocation, then materializes an isolated-RNG reserve to reach 7,000 artists. In 1961, new Single releases split as follows:

| Artist source | New Singles | Share | 1961 units from new Singles |
|---|---:|---:|---:|
| Original 3,000 | 1,635 | 41.0% | 101.998 million |
| Enabled initial reserve | 2,104 | 52.7% | 49.508 million |
| Runtime formation | 254 | 6.4% | 6.086 million |

Runtime formation is not the dominant economic driver. The post-roster reserve, fresh-potential discovery, and lifecycle replacement semantics collectively replace low-yield legacy slots with commercially viable new contracts.

Do not increase the pool above 7,000. Do not describe the reserve as economy-neutral; the prior audit already rejected that assumption.

### No market-wide buyer-budget clearing

Single sales give each record the region's full potential-buyer population, then apply record-specific awareness, conversion, saturation, inventory, and chart visibility. Album sales similarly give each Album its genre buyer pool and apply per-record exhaustion. There is no market-wide budget that reallocates a bounded number of purchases among simultaneous records.

Consequently, replacing weak releases with stronger releases increases aggregate units instead of primarily redistributing a stable market total. This is a structural candidate for a future correction, but market-wide demand clearing is outside this handoff's implementation authority.

### Career and label feedback compound the initial difference

Artist success increases momentum, reputation, career state, release priority, launch awareness, marketing, and initial stock. Label revenue then affects status, survival, cash runway, distribution opportunity, and later release capacity.

At week 104 the artifacts report:

| Label state | Control | Enabled |
|---|---:|---:|
| Active labels | 426 | 495 |
| Rising labels | 235 | 265 |
| Defunct labels | 254 | 187 |

The enabled/control unit ratio reaches `1.2191x` in weeks 92-104. This late rise is consistent with compounding rather than a one-time price or ledger offset.

### Albums amplify the same composition effect

The enabled 1961 path schedules fewer Albums but sells approximately `1.210x` Album units. Released-Album yield is approximately `1.2446x`.

Mean pooled appeal changes include:

| Album format | Control | Enabled |
|---|---:|---:|
| Standard | 0.5766 | 0.6195 |
| Compilation | 0.5346 | 0.5487 |

Album demand raises pooled appeal to the `2.5` power. Existing compilation inventory is already bounded to the four newest resolvable Singles and applies independent age and reuse decay. Enabled mean freshness is higher (`0.8268` versus `0.7272`) because its selected material is newer and less reused. Do not add another hit-inventory penalty, compilation quota, veteran penalty, or format tilt in this investigation.

## Required work

### E0 - Freeze and reproduce the attribution

Before editing behavior, reproduce the tables above directly from:

```text
SimLogs/systemic-label-capacity-control-104-1001-*.csv
SimLogs/systemic-label-capacity-enabled-104-1001-*.csv
```

At minimum, independently verify:

- annual release, scheduled-Album, format-unit, gross, label-net, and market-net ratios;
- Single units per release;
- new-1961 Single quality by artist source;
- A3 decisions by career state and quality quartile;
- 1961 Single units by launch career state;
- enabled artist-source shares using original, reserve, and runtime ID/cohort boundaries;
- Album pooled appeal and freshness by format;
- label status counts at week 104; and
- 13-week economic ratios, including weeks 92-104.

Resolve any discrepancy before implementing diagnostics.

### E1 - Build a removable offline attribution analyzer

Prefer an offline analyzer under `SimTools/` over adding high-volume runtime telemetry. It may read existing CSV families and emit a Markdown or CSV report under ignored `SimLogs/`.

The analyzer must accept control and treatment run prefixes and report, where existing streams permit:

```text
year
format
artistSource = Original3000 | EnabledInitialReserve | RuntimeFormation
releaseCohort = Carryover | CurrentYear
launchCareerState
labelTier
qualityQuartile
recordCount / decisionCount
units
unitsPerRecord
meanQuality
meanInitialAwareness
meanInitialStock
meanPerceivedQualityMultiplier
gross
labelNet
marketNet
```

Also report:

- quarterly/13-week unit and gross ratios;
- active/Rising/Dying/Defunct label counts at the same boundaries;
- Album release count, units per released Album, pooled appeal by format, reuse counts, and freshness;
- a reconciliation proving that reported format units equal `market-revenue.csv`; and
- a clear warning where an attribution is unavailable because existing telemetry does not carry a per-record field.

Do not infer runtime cohort solely from `formedYear`; use explicit cohort/ID boundaries and document the 3,000/7,000 construction. Do not modify frozen CSVs.

### E2 - Resolve the Dropped-control comparator anomaly

Trace these exact seams:

1. `AILabel.GetArtistForRelease` and its eligibility predicate;
2. `GenreSupplyService.IsEligibleExistingArtistForRelease` versus `IsEligibleExistingArtistForEnabledRelease`;
3. `SimulatedArtist.UpdateCareerState` when a chart run changes state to Dropped;
4. `ArtistManager.OnRecordLeftChart` and `RosterManager.RecordChartRunComplete`;
5. `RosterManager.TransitionDroppedArtist` on enabled and disabled paths;
6. `TimeManager.OnWeekEnded` subscriber order among chart, competitor, roster, and lifecycle owners;
7. pending Album scheduling versus drop-time release; and
8. the exact write-time semantics of `a3-economic-decisions.careerState` and `records.launchCareerState`.

Add deterministic production-helper probes if the source lacks coverage for a discovered branch. An opt-in console trace or diagnostic-only file is permitted only if it is dormant by default and cannot alter the disabled replay's 45 frozen streams, headers, values, RNG order, or stream set.

Required outcome: classify the Dropped rows as expected compatibility behavior, stale comparator lineage, telemetry semantics, or an implementation defect. Record exact code evidence.

If correcting the cause would alter the accepted disabled replay, stop and document the required Directive amendment. Do not repair disabled behavior under this handoff. If it is an enabled-path or diagnostic-only correctness defect, fix it, add probes, and restart E0-E2 before any simulation diagnostic.

### E3 - Paired label-lifecycle-disabled 104-week diagnostic

After E0-E2 are complete and the source builds, run exactly one seed-1001 104-week control/treatment pair with label lifecycle processing disabled in both arms. Keep Genre Market V2, artist-population treatment state, distribution deals, release rules, Albums, market demand, finance arithmetic, and all other inputs unchanged.

Suggested run family:

```text
d6-economic-yield-label-freeze-control-104-1001
d6-economic-yield-label-freeze-enabled-104-1001
```

This is a causal diagnostic, not an acceptance replay. Report the same annual and 13-week attribution tables as E0, plus label counts proving the lifecycle owner was actually disabled.

Interpretation:

- If disabling label lifecycle removes at least half of the 1961 excess above `1.0`, label survival/status is the dominant amplifier.
- If the 1961 ratio remains above `1.10`, the upstream artist/release-quality and uncapped buyer-budget mechanisms remain sufficient to fail acceptance.
- In either case, do not tune label death, birth, promotion, demotion, status, overhead, or bankruptcy thresholds in this pass.

Stop immediately if either diagnostic arm accidentally changes release or market configuration beyond the intended label-lifecycle switch.

### E4 - Conditional initial-reserve boundary diagnostic

Run this step only if E3 leaves the 1961 unit or gross ratio above `1.10`, or if E3 cannot distinguish the reserve from career-state replacement.

Add a diagnostic-only command-line override that suppresses `MaterializeEnabledInitialUnsignedReserve` while leaving:

- the original 3,000 launch artists and roster allocation unchanged;
- annual runtime formation at 300;
- the enabled lifecycle, contract, cooldown, inactivity, and terminal rules active;
- fresh-potential and experienced discovery logic unchanged;
- the global and population RNG boundaries explicit; and
- the default enabled 7,000-market path unchanged when the override is absent.

Reject contradictory reserve flags. Add fixed probes proving default 7,000 behavior, diagnostic 3,000 behavior, disabled neutrality, and no global RNG consumption by the suppressed reserve.

Run one treatment-only 104-week seed-1001 diagnostic:

```text
d6-economic-yield-no-reserve-enabled-104-1001
```

Compare it with the existing systemic-capacity control and treatment, but do not treat it as an acceptance candidate. Report:

- release and scheduled-Album ratios;
- economic ratios;
- first-time versus repeat signings;
- original versus runtime release shares;
- mean release quality and quality quartiles;
- roster, vacancy, and release-eligible counts; and
- every structural invariant.

Interpretation:

- Economics inside band with capacity below band confirms a reserve-mediated capacity/yield tension.
- Economics still above band shows that terminal filtering, survivor concentration, and market demand amplification are sufficient without the reserve.
- Capacity and economics both inside band identifies the reserve as a candidate scalar, but does not authorize permanently removing it; mature supply and chronology requirements still require a separate decision.

Do not test 4,000, 5,000, 6,000, 8,000, 10,000, or any other pool size. This is one boundary diagnostic, not a sweep.

## Fixed probes and verification

Retain accepted D5 suites and D6 probes 1-53. Add only probes demanded by E1-E4, including at least:

1. artist-source classification at IDs 3,000/3,001 and 7,000/7,001;
2. carryover versus current-year release classification;
3. quality quartile boundaries used by the analyzer;
4. terminal-state eligibility under each explicit live/disabled predicate;
5. chart-run completion followed by the correct roster/pool transition;
6. pending Album state semantics at schedule and drop;
7. diagnostic reserve suppression leaves the original 3,000 intact;
8. default enabled reserve still materializes to 7,000;
9. disabled configuration never materializes or evaluates the reserve override; and
10. diagnostic code consumes no behavior-producing RNG unless the selected diagnostic intentionally changes the enabled treatment.

For every source change:

- run `dotnet build "Label Man.sln" --no-restore`;
- run `git diff --check`;
- run accepted D5 and the complete D6 fixed suites;
- record the source manifest; and
- preserve the known non-fatal `MissingSingletonsTemp.cs` post-completion diagnostic without treating it as a new failure.

Do not add probe-only behavior branches.

## Decision record required at completion

Append a new checkpoint to `ArtistPopulationLifecycleAudit.md` containing:

- exact artifact prefixes and commands;
- analyzer command and reconciliation results;
- source and stream manifests;
- the Dropped-control classification with code evidence;
- baseline, label-lifecycle-disabled, and conditional no-reserve attribution tables;
- release quality, career state, artist source, label tier, and Album appeal decompositions;
- how much of the economic excess is initial composition versus label feedback;
- whether the accepted control is a coherent economic counterfactual or only a compatibility boundary;
- the exact stop decision; and
- one recommended next correction with its required authority.

The final recommendation must choose among these outcomes rather than combining them speculatively:

1. **Comparator amendment:** the accepted disabled control is structurally depressed by legacy terminal-release behavior, requiring a separately defined shadow economic comparator while retaining byte-exact compatibility control.
2. **Reserve/labor-market amendment:** the 7,000 reserve and fresh-selection model create the binding quality/capacity tradeoff, requiring an explicitly authorized supply-policy change.
3. **Market-clearing amendment:** artist and label behavior is coherent, but independent per-record buyer pools convert improved talent quality into unbounded aggregate market volume, requiring a bounded market-demand/substitution design.
4. **Direct defect repair:** a concrete implementation error, rather than an intended model difference, explains the excess and can be repaired without changing frozen behavior.

Do not launch a decade, later seed, or holdout until a subsequent authorized candidate passes seed-1001 52/104-week capacity, economic, format, lifecycle, determinism, and disabled-boundary gates.

## Closed surfaces

Do not tune or change:

- `releasesPerMonth`, release cadence, release probability, release growth, release cooldown, or release priority;
- Single/Album priors, format tilts, revenue memory, project timing, Album thresholds, or Album chart rules;
- hit-inventory count, age decay, reuse decay, compilation quota, or veteran priority;
- prices, pressing costs, packaging costs, royalties, advances, skim, overhead, or finance reconciliation;
- label lifecycle thresholds or distribution-deal terms;
- quality exponent, purchase rate, awareness, chart visibility, saturation, exhaustion, restock, or buyer-pool formulas;
- genre keyframes, supply weights, acceptance, regional routing, momentum, or historical inputs;
- annual formation, inactivity, cooldown, probation, exhaustion, or operating-target rules;
- the default enabled pool size or disabled 3,000-artist boundary, except for the one opt-in E4 diagnostic seam;
- acceptance bands; or
- frozen disabled streams and RNG behavior.

The purpose of this handoff is causal isolation. The next implementation should fix the mechanism actually demonstrated by these bounded diagnostics, not force the ratios through an unrelated penalty.
