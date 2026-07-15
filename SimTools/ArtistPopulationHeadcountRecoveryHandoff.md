# Artist population headcount-recovery handoff

## Mission

Correct the failed market-clearing M2 implementation by making roster Recovery headcount-only, then carry the candidate through the complete gated validation ladder without returning for another micro-amendment after every passing checkpoint.

This handoff is the authoritative next-pass instruction for Codex. It supersedes `ArtistPopulationMarketClearingHandoff.md` where the two differ. Retain that file, `ArtistPopulationLifecycleAudit.md`, and all prior run families as historical evidence.

The current worktree contains the unaccepted source used by `d6-market-clearing-gateb-1001`. Preserve it as the starting point; do not reset to the earlier fresh-priority candidate.

## What happened

The first market-clearing implementation passed build and all 47 fixed probes, then completed its 52-week treatment.

| Measure | Treatment | Control | Ratio | Result |
|---|---:|---:|---:|---|
| Authoritative successful releases | 4,141 | 4,313 | 0.9601 | Pass |
| Scheduled Album projects | 1,326 | 1,090 | 1.2165 | Fail |

Telemetry size is not a failure and is not an acceptance gate.

The Album failure came from a specific implementation mistake. `serviceDeficit` included instantaneous `releaseLaneDeficit`. Normal artist release cooldown therefore looked like missing staff, even when a label was already at its operating target. Recovery then signed above target to create temporary release lanes.

That behavior caused:

- 2,981 first-time signings instead of 493 in the prior 52-week candidate;
- an ending roster of 5,169 instead of 2,573;
- 910 label-closure departures instead of 134; and
- 1,190 Album projects scheduled from `NewSigning` state, versus 1,035 previously.

The release result already passed. Do not increase release cadence or solve this by changing Album rules. Remove the over-hiring cause.

## Required correction

### Headcount is the only Recovery deficit

At the live weekly scouting boundary, calculate:

```text
headcountDeficit = max(0, OperatingRosterTarget - rosterSize)
requiredReleaseLanes = min(3, maxRosterSize)
releaseLaneDeficit = max(0, requiredReleaseLanes - releaseEligibleArtistCount)
serviceDeficit = headcountDeficit
```

`releaseLaneDeficit` remains telemetry only. It must not:

- enter or extend Recovery;
- age the service deficit;
- bypass the random scouting gate;
- widen discovery;
- create a signing attempt;
- increase an operating target; or
- authorize a signing above target.

### Operating target is the hiring ceiling

For market-clearing scouting:

```text
mayHire = rosterSize < OperatingRosterTarget
```

Do not sign when `rosterSize >= OperatingRosterTarget`, even if fewer than three artists are currently release eligible. Remove `recoveryRosterCeilingByLabelId` and any temporary release-lane buffer above target if they have no remaining purpose.

The hard `maxRosterSize` invariant remains, but `OperatingRosterTarget` is the tighter hiring boundary for this path.

### Preserve the service schedule

Retain the current service modes, now driven only by headcount:

- `Normal`: no headcount deficit;
- `Watch`: one missing artist for fewer than four consecutive weeks; and
- `Recovery`: empty roster, at least two missing artists, or any headcount deficit persisting for four weeks.

Recovery still gives one deterministic candidate evaluation and at most one actual signing attempt per active label-week. It bypasses the scouting roll and consumes no decorative RNG draw. It ends as soon as headcount reaches the operating target.

### Retain the corrected talent and career model

Do not revert or weaken:

- the enabled-only 7,000-artist market with post-launch isolated reserve generation;
- calendar-aligned 300-per-complete-year formation;
- separate deterministic `FreshPotential` and `ExperiencedProduction` lanes;
- the fresh-potential score that does not punish missing reputation or momentum;
- deterministic regional discovery and bounded national fresh recovery;
- affordability and one-attempt-per-label-week rules;
- three completed current-contract chart runs plus three current-contract consecutive flops for a performance departure;
- stale-record contract provenance isolation;
- one 13-week performance-drop comeback opportunity;
- structured `PerformanceExhaustion` after the second career performance departure;
- closed-label scouting exclusion;
- ownership, pool, terminal, chronology, and release-selection safeguards; or
- the fully frozen disabled route.

Remove only the current third-plus fresh-preference implementation if any obsolete code remains alongside the separate-lane resolver. Do not run two candidate preference systems.

## Telemetry policy

There is no telemetry byte-size limit, percentage-growth limit, or exact stream-count gate.

Record stream sizes and hashes for reproducibility. Use common sense to avoid catastrophic growth: prefer one row per label-week and aggregate cohort rows; avoid per-candidate and reserve-artist rows unless a concrete later diagnosis requires them.

Retain the current service/lane fields. They must prove at minimum:

- headcount and release-lane deficits separately;
- service mode and age;
- whether the scouting gate was bypassed;
- fresh and experienced lane sizes and best scores;
- selected lane and recovery fallback;
- roster size and operating target before the decision;
- whether a signing was attempted and succeeded; and
- the structured failure reason.

Add or retain a direct invariant counter for any market-clearing signing attempted at or above `OperatingRosterTarget`. It must remain zero.

## Fixed probes

Keep all accepted D5 probes and all current D6 probes. Amend or add focused tests proving:

1. `serviceDeficit` equals `headcountDeficit`, regardless of release eligibility.
2. A label at operating target with zero release-eligible artists remains `Normal` and cannot evaluate or sign.
3. `releaseLaneDeficit` is still reported accurately as telemetry.
4. A one-artist headcount deficit follows Watch for weeks one through three and Recovery on week four.
5. An empty roster or two-artist deficit enters Recovery immediately.
6. Recovery ends exactly when roster size reaches operating target.
7. No market-clearing signing occurs at or above operating target.
8. No temporary recovery ceiling above target remains active.
9. Recovery still performs at most one evaluation and one signing attempt per label-week.
10. Fresh-potential scoring, lane separation, national widening, actual affordability, and experienced fallback remain unchanged.
11. Three-current-contract-flop evidence, Top-40 clearance, stale-record isolation, first comeback, second performance exhaustion, and non-performance departure behavior remain unchanged.
12. The disabled path retains its exact behavior, RNG order, headers, and stream set.

Probes must exercise production helpers. Do not satisfy them with parallel probe-only logic.

## Validation ladder

Use seed 1001 until the seed-1001 decade passes. Preserve every run family and never overwrite earlier evidence.

Suggested corrected run names:

```text
d6-headcount-recovery-probes-1001
d6-headcount-recovery-gateb-1001
d6-headcount-recovery-gatec-1001
d6-headcount-recovery-gatec-repeat-1001
d6-headcount-recovery-middecade-1001
d6-headcount-recovery-disabled-1001
d6-headcount-recovery-decade-1001
```

### H0 - source review

Before editing, trace every use of:

```text
releaseLaneDeficit
serviceDeficit
recoveryRosterCeilingByLabelId
OperatingRosterTarget
HasOperatingRosterSpace
```

Confirm that all behavior-producing release-lane uses are removed while observational uses remain. Record the exact starting functional-source manifest.

### H1 - build and probes

Run:

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check
```

Then run the accepted D5 and full D6 fixed-probe command with the corrected probe run name. Require successful completion markers and no new warning.

### H2 - corrected 52-week treatment

Run the enabled 52-week seed-1001 treatment.

Hard requirements:

- successful releases inside `[0.85,1.15]` of 4,313;
- scheduled Album projects inside `[0.85,1.15]` of 1,090;
- 300 formations in 1960;
- inherited annual economic and format gates pass;
- no signing at or above operating target;
- no release-lane-triggered Recovery;
- no hard-roster, ownership, duplicate, probation, cooldown, terminal, chronology, closed-label, or release-selection violation; and
- build/probe source is exactly the source used by the treatment.

Report first-time signings, free-agent signings, ending roster, operating target, label-closure departures, Recovery label-weeks, and Album projects by career state at schedule. Investigate if first-time signings or ending roster remain remotely close to 2,981 or 5,169; that would mean the over-hiring seam still exists.

Telemetry volume is recorded but cannot fail H2.

### H3 - 104 weeks and deterministic repeat

Only after H2 passes, run one 104-week treatment and one independent repeat.

Require:

- identical stream sets;
- every suffix-matched CSV byte-identical;
- both annual release and Album ratios inside `[0.85,1.15]`;
- inherited economic, format, population, and integrity gates; and
- nonzero, correctly isolated performance exhaustion once enough history exists.

### H4 - 260-week maturity checkpoint

Only after H3 passes, run one 260-week treatment and compare each 52-week block with weeks 1-260 of `d6-population-decade-control-1001`.

Require:

- formations `300 / 300 / 300 / 300 / 294`;
- releases and scheduled Albums each inside `[0.85,1.15]` in every block;
- aggregate individual-format units inside `[0.85,1.15]`;
- inherited aggregate and annual economic bands;
- final aggregate headcount gap no more than 10% of active-label operating targets;
- at least 95% of active labels nonempty;
- first-time signings nonzero in every block and at least as numerous as performance-comeback signings in blocks 3-5;
- zero third-or-later performance comebacks;
- every second performance departure exits through `PerformanceExhaustion` rather than the unsigned pool; and
- all structural and behavioral invariants remain zero.

Report release eligibility per active label as diagnosis only. It is not a staffing target or independent acceptance gate.

If H4 exposes a direct implementation defect in headcount Recovery, fresh-potential evaluation, or performance exhaustion, one evidence-driven source correction and one repeated H2-H4 ladder are authorized. Do not use that authority for a scalar sweep.

### H5 - disabled replay

After H4 passes, run the disabled 52-week aggregate replay. Require all 45 frozen streams to match `d6-fulfillment-emerging-memory-52b-control-1001` by suffix and SHA-256, with no enabled-only population stream.

### H6 - date-complete decade and later seeds

After H5 passes, run the date-complete 522-Friday seed-1001 treatment. Never use the obsolete 520-week horizon. Compare with the retained authoritative seed-1001 control or rerun its paired control if the boundary cannot be established.

Apply the same per-calendar-year, aggregate, population, market-clearing, and integrity gates. If seed 1001 passes, freeze the exact source and proceed sequentially to seeds 1002 and 1003, then the defined holdout. Do not edit source between seeds.

## Controls that remain closed

Do not change these to force a pass:

- pool size above 7,000 or annual formation above 300;
- release cadence, annual release growth, release cooldown, selection, or priority;
- Album choice, project scheduling, track reuse, or format rules;
- advances, affordability, reserves, overhead, royalties, label finance, or label lifecycle thresholds;
- market demand, sales, genre keyframes, supply weights, adjacency, regional economics, or historical inputs;
- acceptance bands; or
- the disabled behavior boundary.

The correction is headcount-only Recovery. If Albums remain high after the over-hiring defect is actually removed, join projects to contract entry state, selected lane, Recovery state, and signing week before proposing another change. Do not tune Album logic.

## Completion record

Append exact source manifests, commands, completion markers, hashes, result tables, invariants, and the stop/accept decision to `ArtistPopulationLifecycleAudit.md` after every gate.

The desired outcome is a stable talent market that clears genuine roster vacancies, does not hire against temporary release cooldown, does not recycle failed careers indefinitely, and preserves the accepted release/Album/economic envelope.
