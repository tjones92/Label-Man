# Artist population roster-throughput normalization handoff

## Mission and authority

Correct the systemic roster-retention expansion exposed by `d6-headcount-recovery-gateb-r2-1001`, then restart the gated artist-population ladder.

This is the authoritative next-pass handoff for Codex. It supersedes `ArtistPopulationHeadcountRecoveryHandoff.md` and `ArtistPopulationMarketClearingHandoff.md` where they differ. Preserve those documents and `ArtistPopulationLifecycleAudit.md` as historical evidence.

The current worktree is the unaccepted H2 source with headcount-only Recovery, separate fresh-potential discovery, a unified three-flop performance predicate, and an empty-label bootstrap target of three. Do not reset the worktree. Correct only the state-model seams identified below before measuring again.

This handoff authorizes implementation, fixed probes, a corrected 52-week treatment, deterministic 104-week repeat, 260-week checkpoint, disabled replay, date-complete 522-week seed-1001 treatment, and later seeds after seed 1001 passes. Stop on a real hard gate failure, but do not request another micro-handoff after every passing checkpoint.

## Gate-metric correction

The H2 audit compared unlike release measures. `3,896` is the treatment's retired-record count from `release-outcomes.csv`; `4,313` is the control's successful-release count from `release-capacity.csv`. That comparison cannot establish a release pass.

Use the established initiation-capacity measure consistently:

```text
successful releases = sum(release-capacity.successfulReleases)
```

For the first 52 weeks:

| Measure | H2 | Control | Ratio | Gate |
|---|---:|---:|---:|---|
| Successful releases | 5,079 | 4,313 | 1.1776 | Fail |
| Scheduled Album projects | 1,384 | 1,090 | 1.2697 | Fail |
| Retired release-outcome rows | 3,896 | 3,373 | 1.1551 | Fail, diagnostic only |

`release-outcomes.csv` remains valuable for realized economics and retirement cohorts, but its row count is not the successful-release capacity gate. Never compare its row count to `release-capacity.successfulReleases` again.

## Systemic finding

Albums are not being selected at an anomalously higher rate than the retained 7,000-market candidate. The whole release-decision surface expanded.

| Measure | H2 headcount Recovery | Prior fresh-priority candidate | Change |
|---|---:|---:|---:|
| Average roster | 3,441.2 | 2,956.4 | +16.4% |
| Average release-eligible artists | 2,224.1 | 1,930.7 | +15.2% |
| Successful releases / format decisions | 5,079 | 4,400 | +15.4% |
| Scheduled Albums | 1,384 | 1,205 | +14.9% |
| Album share of format decisions | 27.25% | 27.39% | -0.14 pp |
| First-time signings | 590 | 493 | +19.7% |
| Performance departures | 141 | 851 | -83.4% |
| Ending roster | 3,355 | 2,573 | +30.4% |

Conditional Album choice also remained stable. NewSigning Album share was 25.17% in H2 versus 25.48% in the prior candidate. The extra NewSigning Albums came from 4,740 NewSigning decisions instead of 4,062, not from a new format preference.

Do not change `DecideRelease`, Single/Album priors, format tilts, revenue memory, Album scheduling, or Album thresholds. The failure is excess roster/release opportunity upstream.

Two state changes created that excess.

### Empty-label bootstrap expansion

At week 1, 173 of 600 labels had no initialized artist. H2 assigned each empty label a target of three:

```text
roster = 3,000
operating targets = 3,519
```

The prior candidate used a one-artist bootstrap:

```text
roster = 2,998
operating targets = 3,173
```

The `+346` target difference is exactly two extra slots for 173 empty labels. A release-availability constant was incorrectly converted into permanent headcount demand.

### Performance-state over-retention

The unified enabled predicate now requires three current-contract completed runs and three current-contract consecutive flops for every performance departure. In 1960 this reduced performance departures from 851 to 141.

The predicate also replaced normal career-state departure too broadly:

- contract counters advance only while an artist is `NewSigning` or an experienced comeback is pending;
- a current-contract Top-40 clears that pending state;
- `AILabel.ShouldDropArtist` nevertheless returns only the contract predicate for every enabled artist; and
- `SimulatedArtist.UpdateCareerState` suppresses any `Dropped` transition that does not satisfy the same pending-contract predicate.

As a result, a proven Rising/Established/Declining artist can become immune to ordinary performance departure after clearing probation. Contract probation and normal career decline are separate concepts and must not share one universal predicate.

## Fresh-potential scouting status

The potential model is already implemented and working. `AILabel.EvaluateFreshPotential` scores never-signed acts from base quality, scouting ability, genre fit, and the existing high-cost adjustment. It deliberately omits momentum/reputation bonuses and the low-reputation penalty.

This is not a synthetic quality bonus; it treats missing career evidence as unknown rather than bad. H2 recorded:

- 590 `FreshPotential` selections;
- 1 `ExperiencedProduction` selection; and
- zero candidate-score rejections.

Retain this model. Once first-contract departures and genuine headcount vacancies are restored, it supplies fresh replacements without forcing labels back into repeated comeback recycling.

## Required correction

### 1. Restore a one-artist empty-label bootstrap

After initial roster allocation:

```text
if CurrentRosterSize > 0:
    OperatingRosterTarget = CurrentRosterSize
else:
    OperatingRosterTarget = min(1, maxRosterSize)
```

`SetOperatingRosterTargetFromCurrent()` already clamps an empty roster to one. Remove the subsequent override to `min(3, maxRosterSize)`.

This rule applies whenever the simulation establishes a label's initial operating target. It does not prevent a later separately modeled label-growth system; none is authorized in this pass.

Continue to enforce:

- `serviceDeficit = max(0, OperatingRosterTarget - rosterSize)`;
- release-lane deficit is observational only;
- no Recovery or Normal signing at or above operating target; and
- one evaluation and one actual signing attempt per active label-week.

### 2. Separate probation from normal career decline

Create one authoritative classification for enabled performance evaluation:

```text
FirstContractProbation
ExperiencedComebackProbation
NormalCareer
```

Use state, not a guessed contract-sequence range:

- `ExperiencedComebackProbation` when `IsExperiencedComebackEvaluationPending()` is true.
- `FirstContractProbation` when `careerState == NewSigning` and the contract is not an experienced comeback.
- `NormalCareer` otherwise.

#### First-contract probation

A first-contract NewSigning performance departure requires:

```text
contractTop40Hits == 0
contractCompletedChartRuns >= 2
contractConsecutiveFlops >= 2
```

All evidence must belong to the current contract. Stale prior-contract records remain excluded.

#### Experienced-comeback probation

An experienced comeback performance departure requires:

```text
contractTop40Hits == 0
contractCompletedChartRuns >= 3
contractConsecutiveFlops >= 3
```

Retain the restored pre-drop career tier and the existing 13-week first performance cooldown. A second career performance departure still transitions atomically to `PerformanceExhaustion` and never re-enters the unsigned pool.

#### Normal career

Once a current-contract Top-40 clears probation, ordinary Rising/Established/Declining career progression and performance departure must work again.

Scope the enabled drop-suppression guard only to an artist whose contract probation is still pending. Do not suppress a normal-career `Declining -> Dropped` transition merely because the artist no longer has pending contract counters.

In `AILabel.ShouldDropArtist`:

- pending first-contract or comeback probation uses only the appropriate current-contract predicate;
- normal-career artists use the existing state-aware monthly review behavior; and
- Superstar protection remains unchanged.

Do not allow lifetime flop history to bypass pending probation. Conversely, do not allow completed probation to create permanent performance immunity.

### 3. Retain career finality

`performanceDropCount` remains the career-level counter:

- first performance departure: structured Performance drop, 13-week cooldown, one comeback allowed;
- second performance departure: structured `PerformanceExhaustion`, inactive, removed from roster and pool; and
- closure, expiration, voluntary departure, and reconciliation do not increment performance failure.

Use `performanceDropCount`, not raw `contractSequence`, for exhaustion.

## Telemetry

There is no telemetry size, percentage-growth, or exact stream-count gate. Record sizes and hashes for reproducibility and use common sense to avoid catastrophic row multiplication.

Existing streams are sufficient if extended with compact structured values. Prove at least:

- operating target source: populated launch roster or one-artist bootstrap;
- performance evaluation mode;
- required completed runs and consecutive flops;
- current-contract counters at departure;
- whether probation was pending or cleared;
- normal-career performance departures;
- first performance departure versus exhaustion; and
- market-clearing attempts at/above target.

Do not emit candidate-level or reserve-artist rows.

## Fixed probes

Retain accepted D5 suites and D6 probes 1-49. Add production-helper coverage for:

1. a populated label's operating target equals initialized roster headcount;
2. an empty initialized label receives target one, never three;
3. release eligibility cannot alter the target or service deficit;
4. a first-contract artist does not depart after one current-contract flop;
5. a first-contract artist departs after two completed current-contract consecutive flops;
6. a stale prior-contract result cannot satisfy first-contract probation;
7. an experienced comeback remains protected until three current-contract completed flops;
8. an experienced comeback departs on the third current-contract flop;
9. a current-contract Top-40 clears either probation mode;
10. after probation clears, a Rising artist can decline through normal career rules;
11. after probation clears, a Declining artist can depart through normal career rules;
12. monthly review reads contract evidence only while probation is pending;
13. monthly review uses normal state-aware behavior after probation clears;
14. a first performance departure remains comeback-eligible after cooldown;
15. a second performance departure becomes `PerformanceExhaustion` and is absent from roster and pool;
16. non-performance departures do not exhaust a career;
17. fresh-potential scoring still omits career-evidence penalties and selects signable fresh supply;
18. one-attempt, operating-target, closed-label, ownership, terminal, and RNG invariants remain intact; and
19. the disabled route retains exact behavior, RNG order, headers, and stream set.

Do not add probe-only policy logic.

## Validation ladder

Use seed 1001 until the seed-1001 decade passes. Preserve all run families.

Suggested run family:

```text
d6-roster-normalization-probes-1001
d6-roster-normalization-gateb-1001
d6-roster-normalization-gatec-1001
d6-roster-normalization-gatec-repeat-1001
d6-roster-normalization-middecade-1001
d6-roster-normalization-disabled-1001
d6-roster-normalization-decade-1001
```

### N0 - reproduce the diagnosis

Before editing, reproduce:

- 5,079 successful H2 releases from `release-capacity.csv`;
- 1,384 scheduled Album projects;
- 3,896 treatment versus 3,373 control retired outcome rows;
- week-1 roster 3,000, target 3,519, and 173 empty labels;
- average roster 3,441.2 and release-eligible artists 2,224.1;
- 141 performance departures;
- 590 fresh and 1 experienced market selections; and
- conditional Album shares by career state.

Any discrepancy must be resolved before source changes.

### N1 - build and probes

Run `dotnet build "Label Man.sln" --no-restore`, `git diff --check`, accepted D5 probes, and the expanded D6 suite. Record the exact source manifest used by the run.

### N2 - corrected 52-week treatment

Run one enabled 52-week seed-1001 treatment.

Hard gates:

- successful releases from `release-capacity.csv` inside `[0.85,1.15]` of 4,313;
- scheduled Album projects inside `[0.85,1.15]` of 1,090;
- 300 formations;
- inherited economy and format bands;
- week-1 empty-label bootstrap target exactly one;
- no market-clearing signing at/above operating target;
- first-contract and comeback departures contain their required current-contract evidence;
- normal-career departure is not blocked by probation logic;
- no exhausted artist is rostered, pooled, signed, or released; and
- all ownership, duplicate, cooldown, terminal, chronology, closed-label, hard-cap, and release-selection invariants are zero.

Report:

- successful-release and Album ratios;
- total decisions and Album share;
- average and ending roster;
- average release-eligible artists;
- operating targets and empty labels at weeks 1 and 52;
- signings by lane;
- departures by evaluation mode and reason;
- format decisions and Album share by career state; and
- comparable retired-outcome counts, clearly labeled diagnostic only.

The prior 7,000-market Gate B result—4,400 successful releases and 1,205 Albums—is a useful causal reference, not a required exact replay.

### N3 - deterministic 104-week repeat

Only after N2 passes, run a 104-week treatment and independent repeat. Require identical stream sets and byte-identical suffix-matched CSVs. Require both annual release and Album ratios, economic/format gates, performance-evaluation evidence, exhaustion finality, and all invariants.

### N4 - 260-week maturity checkpoint

Only after N3 passes, run one 260-week treatment and compare every 52-week block with weeks 1-260 of `d6-population-decade-control-1001`.

Require:

- formations `300 / 300 / 300 / 300 / 294`;
- successful releases and scheduled Albums each inside `[0.85,1.15]` in every block;
- aggregate format units and inherited economic bands;
- headcount gap at most 10% of active operating targets;
- at least 95% of active labels nonempty;
- first-time signings nonzero in every block;
- fresh potential remains materially used;
- zero third-or-later performance comebacks;
- all second performance failures exit through exhaustion; and
- every structural and behavioral invariant remains zero.

If mature releases fall while genuine target gaps persist, diagnose Recovery cadence and affordability using the existing telemetry. Do not restore a three-artist empty bootstrap, weaken first-contract evidence below two results, raise formation, or modify release/Album rules.

One evidence-driven correction is authorized for a direct implementation defect in this state model, followed by a complete N1-N4 rerun. This is not authority for a scalar sweep.

### N5 - disabled replay

After N4 passes, run the disabled 52-week aggregate replay. Require all 45 frozen streams to match `d6-fulfillment-emerging-memory-52b-control-1001` by suffix and SHA-256, with no enabled-only population stream.

### N6 - date-complete decade and later seeds

After N5 passes, run the 522-Friday seed-1001 treatment. Do not use 520 weeks. Apply the same per-calendar-year, aggregate, lifecycle, and integrity gates against the authoritative control boundary.

If seed 1001 passes, freeze source and proceed sequentially to seeds 1002 and 1003, then the defined holdout. Do not change source between seeds.

## Closed surfaces

Do not tune:

- release cadence, release growth, release cooldown, selection, or priority;
- Single/Album priors, format tilts, revenue memory, Album scheduling, or format rules;
- pool size above 7,000 or annual formation above 300;
- fresh-potential formula, except to correct a demonstrated implementation mismatch with this handoff;
- advances, affordability, finance, label lifecycle, market demand, sales, genre, regional, or historical inputs;
- acceptance bands; or
- disabled behavior.

The correction is roster-throughput normalization: one-artist empty bootstrap, state-specific contract probation, restored normal career decline, and retained fresh replacement supply.

## Completion record

Append exact commands, source manifests, completion markers, stream hashes, gate tables, cohort evidence, and stop/accept decisions to `ArtistPopulationLifecycleAudit.md` after each gate.

The target behavior is a labor market that develops unproven acts, releases first-contract failures on current evidence, gives experienced artists one finite comeback, permits proven careers to decline normally, and fills only genuine roster vacancies.
