# Artist population market-clearing correction handoff

## Status and authority

This is the authoritative next artist-population pass. It supersedes `ArtistPopulationFreshProspectPreferenceHandoff.md` for implementation and measurement while retaining that file, `ArtistPopulationReleaseCapacityInvestigationHandoff.md`, and `ArtistPopulationLifecycleAudit.md` as evidence.

The current worktree is the failed, unfrozen `d6-market-clearing-gateb-1001` source state. Do not describe it as accepted. Retain the enabled-only 7,000-artist market, isolated reserve generation, calendar-aligned 300-per-complete-year formation, label operating targets, practical telemetry, closed-label protections, contract provenance, and disabled behavior boundary.

This pass is intentionally broader than a one-scalar amendment. The measured failure is a coupled labor-market defect: probabilistic vacancy service, a fresh-talent scoring bias, and repeated performance-comeback recycling reinforce one another. The authorized unit of correction is the coherent market-clearing state model below. Do not reduce it to another scouting multiplier, pool-size increase, or narrowly guarded candidate preference.

The implementation owner may complete the build/probe, 52-week, deterministic 104-week repeat, 260-week, disabled replay, and date-complete 522-week seed-1001 ladder without requesting another micro-handoff, provided each preceding gate passes. Up to two seed-1001 260-week implementation iterations are authorized inside this exact state model when telemetry identifies a correctness error in service recovery, fresh-potential evaluation, or performance-exhaustion transitions. This is not authority for unrelated constant sweeps.

## Decisive diagnosis

The fresh-preference treatment completed correctly. It failed because it intercepted only one small branch of a much larger throughput problem.

| Mature-flow measure | 1960 | 1964 | Change |
|---|---:|---:|---:|
| Average roster | 2,956.4 | 1,887.9 | -36.1% |
| Average release-eligible artists | 1,930.7 | 791.7 | -59.0% |
| End-of-block operating-target gap | 473 | 908 | +92.0% |
| Scouting evaluations | 619 | 2,517 | +306.6% |
| Candidate-score rejections | 97 | 1,243 | +1,181.4% |
| First-time signings | 493 | 456 | -7.5% |
| Free-agent signings | 29 | 814 | +2,706.9% |
| Drops to the free-agent path | 986 | 1,766 | +79.1% |

The 1964 block ended with 404 active labels, an operating target of 2,679, a roster of 1,826, a 908-slot clamped target gap, and 27 empty active labels. The frozen control had only 391 active labels at the same checkpoint, so the release deficit is not explained by too few surviving labels.

Every one of the 2,517 candidate evaluations in 1964 contained at least one never-signed artist, but only 661 contained a never-signed candidate that cleared the inherited `0.30` threshold. There were 1,243 score rejections. Median best-never-signed score fell from `0.4933` in 1960 to `0.2542` in 1964, below the hard threshold. Supply exists; the current evaluator increasingly declares it commercially invisible.

That result follows from the current formula. Never-signed artists have little or no reputation and momentum, then the entire score is multiplied by `0.5 + riskTolerance * 0.5` when reputation is below `0.1`. The evaluator treats the absence of an established career as negative evidence instead of uncertainty about a prospect's potential.

The current fresh guard is also too narrow. It acts only when a third-or-later performance comeback is already the overall winner inside one mixed 4-12 artist slate. Across 260 weeks it applied 408 times, reported 458 `NoQualifyingNeverSigned` fallbacks, and left 8,356 evaluated decisions outside its guarded branch.

Finally, the mature free-agent market is dominated by repeat performance failure. The 1964 block recorded 1,409 performance drops; 929 occurred on contract sequence two or later. Free-agent signings outnumbered first-time signings 814 to 456. The simulation repeatedly spends scarce scouting opportunities returning failed catalogs to rosters while thousands of never-signed artists remain available.

Release selection is not the failed seam. Artist-selection failures are zero. Albums, economics, telemetry size, ownership, pool uniqueness, terminal eligibility, chronology, and closed-label behavior all passed. The loss occurs before a release roll: labels lack enough continuously release-eligible roster lanes.

## Objective

Restore active labels to a stable talent-service level and keep them there without changing release cadence, release selection, Album choice, finance, market demand, format weights, genre keyframes, or historical inputs.

The corrected system must:

1. clear deep and persistent label vacancies on a bounded schedule;
2. evaluate never-signed artists on potential rather than established-career evidence;
3. stop indefinite performance-comeback recycling;
4. preserve affordability and hard roster capacity;
5. keep release, Album, format, and economic outputs inside the inherited bands; and
6. remain completely dormant on the disabled path.

## Authorized state-model correction

### 1. Replace probabilistic urgency with a headcount-service obligation

Keep `operatingRosterTarget` as the normal headcount commitment. Set a newly created active label's bootstrap target to `min(3, maxRosterSize)`, not one, because `CalculateWeeklyReleaseChance` does not reach full artist-availability contribution until three artists are release eligible.

For each active label, compute at the weekly scouting boundary:

```text
headcountDeficit = max(0, OperatingRosterTarget - rosterSize)
requiredReleaseLanes = min(3, maxRosterSize)
releaseLaneDeficit = max(0, requiredReleaseLanes - releaseEligibleArtistCount)
serviceDeficit = headcountDeficit
```

Use the following service modes:

- `Normal`: `serviceDeficit == 0`. Preserve the ordinary stochastic scouting gate.
- `Watch`: `serviceDeficit == 1` and the deficit is younger than four consecutive weeks. Preserve the ordinary gate.
- `Recovery`: the label is empty, `serviceDeficit >= 2`, or any service deficit has persisted for four weeks. Give the label one candidate evaluation and at most one actual signing attempt every live week until the deficit clears.

Recovery bypasses the random scouting gate; it is not a probability floor. Do not consume a decorative scouting RNG draw on a guaranteed recovery evaluation. Enabled-run RNG divergence is expected and must be captured in the source manifest. Closed labels, labels at their operating target, and labels unable to afford the existing tier estimated advance do not enter recovery evaluation.

`OperatingRosterTarget` is a hard hiring stop for this policy. Never sign above it merely because current roster artists are cooling down between releases. `releaseLaneDeficit` remains observational telemetry: it may explain release throughput, but it does not create a vacancy, enter Recovery, widen discovery, or authorize another contract. Recovery ends as soon as `headcountDeficit` is zero.

One active label still receives at most one evaluation and one actual signing attempt per week. The correction changes service certainty, not in-week attempt multiplicity.

### 2. Split discovery into fresh-potential and experienced-production lanes

Do not keep one mixed slate and then add another narrow preference guard. Enumerate two deterministic lanes from the same eligible regional pool and the same four-week discovery window:

- `FreshPotential`: `contractSequence == 0`, never performance-dropped, active, unsigned, genre-available, and signable.
- `ExperiencedProduction`: every other eligible free agent not career-exhausted.

Each lane receives the existing scouting-ability-derived slate size. Stable label/artist hashing, regional preference, national fallback, genre availability, cooldown eligibility, and affordability remain deterministic and RNG-neutral.

If a Recovery evaluation finds no viable fresh candidate in the initial regional lane, widen only that evaluation to a deterministic national fresh slate of up to four times the normal lane size. This is an explicit market-clearing search, not a permanent enlargement of every slate. Do not add another RNG draw.

Evaluate experienced artists with the existing production score. Evaluate never-signed artists with a prospect-potential score derived from the existing formula:

```text
potentialScore = baseQuality * scoutingAbility * 2
potentialScore += unchanged preferred/secondary/off-genre adjustment
potentialScore *= unchanged high-cost adjustment when applicable
```

Do not add momentum or reputation bonuses to a never-signed prospect, and do not apply the low-reputation multiplier. Lack of a prior career is neither a bonus nor a penalty. This is a separate evidence model, not a lowered version of the experienced score.

Selection policy:

1. In Normal and Watch mode, apply the unchanged `0.30` threshold to each lane's appropriate score and choose the highest-scoring affordable qualifier.
2. In Recovery mode, prefer the highest-scoring affordable fresh prospect that clears `0.30`.
3. If no fresh prospect clears `0.30`, accept the highest positive-scoring affordable fresh prospect from the widened recovery lane.
4. Only when no positive affordable fresh prospect exists may Recovery select the highest-scoring eligible experienced candidate.
5. Never attempt a fresh artist and then fall back to an experienced artist in the same label-week.

Remove the current third-plus-only `SelectFreshProspectCandidate` policy after the new lane resolver is covered by probes. Its telemetry may be replaced in place; do not leave two overlapping preference systems active.

### 3. Make performance failure contract-scoped and career-finite

Replace the asymmetric two-flop new-signing rule and three-flop experienced-comeback rule with one current-contract evidence rule for the enabled lifecycle:

- a Top-40 result under the current contract clears pending performance probation;
- a performance departure requires at least three completed chart runs under the current contract and three current-contract consecutive flops;
- stale records from prior contracts never satisfy or change those counters; and
- monthly review and chart callbacks must call the same authoritative predicate.

After an artist's first performance departure, keep the existing 13-week cooldown and allow one comeback. Preserve pre-drop career tier on that comeback as the current source does.

After an artist's second performance departure across their career (`performanceDropCount` becomes two), transition them to `Inactive` with a structured `PerformanceExhaustion` reason. Remove them atomically from roster and unsigned-pool membership while preserving registry, history, public profile, release history, and eventual retirement/disbandment behavior. They are not eligible for another label contract.

Label closure, contract expiration, voluntary departure, and lifecycle reconciliation do not increment the performance-failure count and do not by themselves exhaust a career. Use `performanceDropCount`, not raw `contractSequence`, to determine exhaustion.

This creates a clear career topology:

```text
first contract -> first performance drop -> one cooldown -> one comeback
one comeback -> second performance drop -> inactive career exit
```

It intentionally eliminates third-or-later performance comebacks. This is a lifecycle rule, not a scouting preference.

## Required telemetry

There is no telemetry byte-size or stream-count acceptance gate. Capture enough structured evidence to diagnose the model. Use common sense to avoid catastrophic row multiplication: prefer extending weekly aggregate rows and do not emit candidate-level or reserve-artist rows unless a later diagnosis genuinely requires them.

Add or replace fields in `label-scouting-vacancy-weekly.csv` sufficient to prove:

```text
releaseEligibleArtistCount
requiredReleaseLanes
headcountDeficit
releaseLaneDeficit
serviceDeficit
serviceDeficitAge
serviceMode
scoutingGateBypassed
freshLaneCount
experiencedLaneCount
freshDiscoveryScope
bestFreshPotentialScore
bestExperiencedProductionScore
selectedLane
recoveryThresholdFallbackUsed
recoveryFailureReason
```

Structured recovery failure reasons must distinguish at least:

- `HardRosterFull`;
- `EstimatedAdvanceUnaffordable`;
- `NoFreshRegionalCandidate`;
- `NoFreshNationalCandidate`;
- `NoPositiveFreshPotential`;
- `NoEligibleExperiencedCandidate`;
- `ActualAdvanceUnaffordable`;
- `FreshThresholdQualified`;
- `FreshRecoveryQualified`;
- `ExperiencedFallback`; and
- `ServiceSatisfied`.

Extend `artist-population-events.csv` only as needed to distinguish first performance departure, comeback signing, second performance departure, and `PerformanceExhaustion`. Do not emit reserve-artist rows.

Report service-level results by label-week and slot-week. Aggregate counts alone are insufficient.

## Fixed probes

Retain all accepted D5 probes and D6 probes 1-47. Replace obsolete third-plus preference expectations only after equivalent new lane coverage exists.

Add deterministic, RNG-audited probes for at least:

1. a satisfied label stays on the ordinary stochastic route;
2. a one-slot deficit remains Watch for weeks one through three;
3. the fourth consecutive deficit week enters Recovery;
4. an empty or two-slot-deficit label enters Recovery immediately;
5. Recovery bypasses the scouting roll and performs exactly one evaluation;
6. a closed, hard-full, or estimated-unaffordable label never enters candidate evaluation;
7. a release-lane deficit is observable but cannot enter Recovery or authorize signing above operating target;
8. fresh and experienced candidates enter separate deterministic lanes;
9. a fresh prospect receives no momentum/reputation bonus and no low-reputation penalty;
10. Normal mode retains the `0.30` threshold;
11. Recovery selects a threshold-passing fresh prospect before an experienced artist;
12. Recovery selects the highest positive fresh prospect when none reaches `0.30`;
13. Recovery widens regional fresh discovery nationally without another RNG draw;
14. Recovery uses an experienced fallback only when no positive affordable fresh prospect exists;
15. one failed actual affordability check does not create an in-week second attempt;
16. current-contract evidence ignores stale prior-contract records;
17. fewer than three current-contract completed flops cannot cause a performance departure;
18. a current-contract Top-40 result clears pending probation;
19. the first performance departure receives the existing cooldown and remains comeback-eligible;
20. the second performance departure emits `PerformanceExhaustion`, becomes inactive, and leaves both roster and pool;
21. closure and expiration departures do not increment performance exhaustion;
22. no third performance comeback is signable;
23. disabled scouting, scoring, drop behavior, RNG order, headers, and stream set remain byte-frozen; and
24. every new telemetry field reports the exact production branch without additional enumeration or RNG.

## Validation ladder

### Gate M0 - retained evidence reproduction

Before editing, reproduce from `d6-pool7000-fresh-priority-middecade-1001`:

- annual average roster and release-eligible counts;
- annual end target gap and empty active labels;
- gate passes, random misses, score rejections, first-time signings, and free-agent signings;
- best-fresh score distributions and threshold-qualified counts;
- performance drops by `performanceDropCount`, contract sequence, and prior departure reason; and
- active-label counts against the frozen control.

Any discrepancy with the diagnosis above must be resolved before implementation.

### Gate M1 - build and probes

- `dotnet build "Label Man.sln" --no-restore` passes with no new warning.
- `git diff --check` passes.
- Accepted D5 probes and the full expanded D6 suite pass.
- Source and telemetry manifests are recorded.

### Gate M2 - 52 weeks, seed 1001

Run the enabled 7,000-market treatment for 52 weeks. The result need not preserve the failed candidate's exact 4,400 releases and 1,205 Albums because the new contract evidence and recovery service are intentionally live in 1960.

Require release and Album ratios inside `[0.85,1.15]`, inherited economic and format bands, correct 300 formation, and zero structural invariants. Record telemetry size for reproducibility, not acceptance.

### Gate M3 - 104 weeks and deterministic repeat

Run one 104-week treatment and one independent repeat. Require the same stream set in both runs and every suffix-matched CSV to be byte-identical. Require both annual release and Album ratios inside `[0.85,1.15]` and all inherited gates.

Also require that Recovery is exercised, fresh-potential fallback is exercised, performance exhaustion is nonzero once enough career history exists, and no exhausted artist is rostered, pooled, released, or re-signed.

### Gate M4 - 260-week market-clearing checkpoint

Run seed 1001 for 260 Friday ticks and compare each 52-week block with weeks 1-260 of `d6-population-decade-control-1001`.

Hard requirements:

- formations are `300 / 300 / 300 / 300 / 294`;
- successful releases and scheduled Albums are each inside `[0.85,1.15]` for every block;
- aggregate individual-format units remain inside `[0.85,1.15]`;
- aggregate units, gross, label net, and market net pass inherited bands;
- each annual total-units and market-net ratio remains inside `[0.75,1.25]`;
- final aggregate headcount gap is at most 10% of active-label operating target;
- no market-clearing signing occurs at or above the label's operating target;
- at least 95% of active labels are nonempty;
- mean release-eligible artists per active label is reported in blocks 3-5 but is not itself a hiring target or acceptance gate;
- p90 age of a deep (`serviceDeficit >= 2`) affordable vacancy is at most eight weeks;
- Recovery candidate-score rejection is zero after national widening; failures must be affordability or true no-positive-candidate cases;
- first-time signings are nonzero in every block and are at least as numerous as performance-comeback signings in blocks 3-5;
- third-or-later performance-comeback signings are zero;
- second performance departures produce exhaustion rather than pool entries; and
- ownership, duplicate membership, probation, cooldown, terminal, chronology, artist-selection, closed-label, hard-cap, and one-attempt invariants are zero.

If release volume passes but Albums exceed `1.15`, first join Album projects to selected lane, contract entry state, and service mode. Do not alter Album rules. Correct a mistaken contract-entry or project-identity transition if demonstrated. If both release and Album volume rise proportionally above the band, audit recovery termination and hard-cap enforcement before touching any downstream rule.

One source-correctness iteration after the first M4 result is allowed when the evidence identifies an implementation defect in this specified model. Do not use that authority to sweep thresholds, pool size, release rules, or economics.

### Gate M5 - disabled replay

After M4 passes, run the 52-week disabled aggregate replay and require all 45 frozen streams to match `d6-fulfillment-emerging-memory-52b-control-1001` by suffix and SHA-256. No enabled-only stream may be emitted.

### Gate M6 - date-complete seed-1001 decade

After M5 passes, run a 522-Friday enabled treatment. Use the retained frozen seed-1001 control if its source and command boundary remain authoritative; otherwise rerun the paired control explicitly. Do not use obsolete 520-week commands.

Require the same per-calendar-year and aggregate gates, 300 formations in every complete calendar year, deterministic completion markers, and market-clearing invariants. Report roster service, telemetry volume, and career exhaustion by year, not only at endpoint; telemetry volume is observational unless it is plainly catastrophic.

If seed 1001 passes M6, the handoff authorizes sequential seeds 1002 and 1003 using the same frozen source and gates, followed by the already defined holdout. Stop on a hard failure, but do not reopen implementation between seeds without invalidating the multi-seed candidate.

## Surfaces that remain controls

Do not tune these to force acceptance:

- initial enabled pool above 7,000 or annual formation above 300;
- `releasesPerMonth`, annual release growth, artist release cooldown, release selection, or release priority;
- Album choice, project scheduling, track reuse, or format weights;
- market demand, sales, finance, advances, royalties, overhead, or label lifecycle thresholds;
- genre availability, keyframes, adjacency, supply weights, or regional economics;
- historical inputs; or
- acceptance bands.

Affordability remains real. Recovery may improve discovery and evaluate on schedule; it may not give a label money or waive the actual advance.

## Completion record

Append to `ArtistPopulationLifecycleAudit.md`:

- exact source-state manifest before and after the correction;
- commands, completion markers, run names, stream counts, sizes, and hashes;
- reproduced diagnosis tables;
- service-mode, lane-selection, widening, affordability, and exhaustion counts;
- per-block release, Album, format, economy, roster-service, and invariant tables;
- deterministic-repeat and disabled-replay comparisons; and
- the exact stop or acceptance decision.

Suggested run family:

```text
d6-market-clearing-probes-1001
d6-market-clearing-gateb-1001
d6-market-clearing-gatec-1001
d6-market-clearing-gatec-repeat-1001
d6-market-clearing-middecade-1001
d6-market-clearing-disabled-1001
d6-market-clearing-decade-1001
```

The intended outcome is not merely a release ratio above `0.85`. The market must demonstrate that active labels can reliably discover, develop, replace, and retire talent without accumulating vacancy debt or endlessly recycling the same failed contracts.
