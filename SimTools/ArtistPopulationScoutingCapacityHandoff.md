# Artist population scouting-capacity diagnosis handoff

## Objective

Add enabled-only, observational label-level vacancy and scouting telemetry, then repeat the accepted seed-1001 104-week `0.20` candidate without changing simulation behavior. The purpose is to distinguish slow vacancy-responsive scouting from candidate scarcity, candidate-score rejection, affordability, and intentionally unused roster capacity before authorizing any further scalar or policy change.

This handoff does **not** authorize `0.25`, a change to `scoutingAbility`, a formation-rate change, a second measurement seed, Gate D, or a 520-week replay.

## Current accepted stopping point

The live-path scouting multiplier was narrowly raised from `0.15` to `0.20`; the disabled path retains `0.15`. Build, `git diff --check`, fixed probes, and the disabled 45-stream replay pass.

The `d6-scout020-gatec-enabled-1001` 104-week candidate remains rejected because 1961 successful releases are `3,803 / 4,810 = 0.7906x`, below the retained `0.85` floor. At least `4,089` releases are required, leaving a deficit of 286. Scheduled Albums pass at `1,531 / 1,600 = 0.9569x`, and the economic and integrity gates pass.

The 1961 roster/scouting flow is:

- `2,066` drops;
- `197` first-time signings;
- `1,536` re-signings;
- `1,966` scouting-gate passes;
- `1,753` signing attempts;
- `213` score rejections;
- `20` affordability rejections;
- zero no-eligible-candidate passes;
- zero release artist-selection failures;
- final roster `2,589`;
- final free-agent pool `993`;
- average empty-roster labels per week `127.7`, with a peak of `187`, out of 600 labels.

The final population row separately reports `120` never-signed unsigned artists, `494` eligible dropped artists, and `394` cooldown-blocked dropped artists. These categories and the roster stream are sampled at different points in the weekly sequence and must not be assumed to reconcile without checking their write boundaries.

## Current diagnosis

The 300-per-year formation rate is not the binding Gate-C constraint demonstrated by this run. Every scouting-gate pass found at least one eligible candidate, and the run ended with a large free-agent pool. Raising formation would add supply to a pool labels already fail to consume quickly enough.

The stronger hypothesis is slow effective scouting cadence. `AILabel.ShouldScoutNewArtist` currently computes:

```text
scoutChance = (1 - rosterFullness) * scoutingAbility
scoutChance *= 0.7 when the roster has a recent hit
scoutChance *= 1.3 when more than 30% of the roster is Declining
gateProbability = scoutChance * 0.20 on the enabled live path
```

For a representative `scoutingAbility = 0.60`, before the recent-hit or Declining adjustments:

| Roster fullness | Weekly pass probability | Geometric expected wait |
|---:|---:|---:|
| 0% | 12.0% | 8.3 weeks |
| 50% | 6.0% | 16.7 weeks |
| 80% | 2.4% | 41.7 weeks |

This arithmetic shows that effective cadence can be slow, but aggregate telemetry cannot prove which individual labels remain vacant, how many slots remain unused, how long vacancies persist, or which branch prevents each label from signing.

Do not interpret this as authority to raise `scoutingAbility`. That property also participates in candidate evaluation and release-quality noise, so changing it would conflate scouting competence with scouting cadence and broaden the behavioral surface.

## Historical-formation interpretation

`F = 0.10`, or approximately 300 runtime formations per year against the initial 3,000-artist cohort, is a modeled renewal rate rather than a demonstrated historical census. Whether it is historically representative is a separate calibration question and cannot be inferred from the Gate-C release deficit.

Any later historical calibration must first define the represented universe: all local acts, all recording acts, acts seeking contracts, commercially viable acts, or acts appearing in release databases. It must also distinguish group formation dates from solo-artist career starts. Do not use raw artist counts whose `begin` field means group formation for groups but birth for people. Historical plausibility may motivate a later research directive; it does not establish that formation restricted this run.

## Authorized implementation: observational telemetry only

Add one new enabled-only CSV, preferably `*-label-scouting-vacancy-weekly.csv`. Do not add fields to or change ordering in any frozen stream. Do not create the stream when artist population lifecycle is disabled.

Record one row per live label per week with, at minimum:

```text
week,year,labelId,labelTier,
maxRosterSize,rosterSize,unusedRosterSlots,isEmptyRoster,
consecutiveVacancyWeeks,consecutiveEmptyWeeks,
scoutingAbility,rosterFullness,
hasRecentHit,recentHitFactor,decliningArtistCount,decliningFactor,
estimatedAdvance,canAffordEstimatedAdvance,
computedScoutProbability,scoutRandomRoll,scoutingGatePassed,
eligibleCandidateCount,bestCandidateScore,
signingAttempted,signingSucceeded,signingKind,
failureReason
```

Allowed `failureReason` values should distinguish at least:

- `RosterFull`;
- `EstimatedAdvanceUnaffordable`;
- `ScoutingRandomGate`;
- `NoEligibleCandidate`;
- `CandidateScore`;
- `ActualAdvanceUnaffordable`;
- `SignedFirstTime`;
- `SignedFreeAgent`.

Blank or explicit not-applicable values are acceptable for fields not reached by a branch. Keep the vocabulary structured; do not parse log-message strings.

### RNG and behavior neutrality

- Capture the existing scouting random roll. Do not make a second RNG call for telemetry.
- Compute and store the probability once; do not recompute it through a helper that consumes RNG.
- Candidate enumeration and scoring must occur exactly where they occur now and in the same order.
- Do not enumerate candidates merely to populate fields when the existing scouting gate did not pass.
- Vacancy-age bookkeeping must not affect candidate order, label order, lifecycle state, or RNG order.
- Prefer telemetry-owned dictionaries keyed by `labelId` for consecutive vacancy and empty-roster weeks; do not add gameplay state unless required.
- The existing `0.20` enabled and `0.15` disabled values remain unchanged.

`maxRosterSize - rosterSize` is unused hard-cap capacity, not automatically proof that the label intends to fill every slot. Preserve that distinction in the audit and analysis.

## Required fixed probes

Add deterministic probes that establish:

1. Full rosters emit `RosterFull` without consuming a scouting RNG roll.
2. An unaffordable estimated advance emits `EstimatedAdvanceUnaffordable` without consuming a scouting RNG roll.
3. A vacant affordable label records the exact existing probability and the single existing random roll.
4. A failed roll emits `ScoutingRandomGate` and does not enumerate candidates.
5. A passing roll with no eligible candidates emits `NoEligibleCandidate`.
6. A passing roll whose best candidate is below threshold emits `CandidateScore` and records the evaluated best score.
7. Successful first-time and free-agent signings are distinguished.
8. Consecutive vacancy and empty-roster ages increment and reset correctly.
9. Telemetry collection consumes no additional RNG and does not alter signing results.
10. The disabled route creates no new population/scouting-vacancy stream and preserves the accepted boundary.

Existing D5 probes and D6 probes 1-23 must continue to pass.

## Validation ladder

### Gate O1 - build and probes

- `dotnet build "Label Man.sln" --no-restore` passes with no new warning.
- `git diff --check` passes.
- Accepted D5 probes and all D6 probes pass.

### Gate O2 - disabled replay

Repeat the seed-1001 disabled 52-week aggregate replay. Require all 45 accepted CSV streams to remain byte-identical, with no population or label-scouting-vacancy stream.

Any disabled difference is a hard stop. Do not freeze a new baseline.

### Gate O3 - enabled observational repeat

Repeat seed 1001 for 104 weeks with the same `0.20` behavior and full telemetry. Compare every pre-existing enabled CSV against `d6-scout020-gatec-enabled-1001`.

Requirements:

- every pre-existing enabled stream is byte-identical;
- the only added output is the new enabled-only observational stream;
- release, Album, economic, formation, probation, cooldown, ownership, terminal, and roster-flow results are unchanged;
- the new stream contains exactly one row per live label per observed week, subject only to explicitly documented label creation/closure boundaries;
- weekly aggregates from the new stream reconcile to `roster-lifecycle.csv` for empty labels, scouting passes, signing attempts, score rejections, affordability rejections, first-time signings, and re-signings.

Do not describe O3 as a new behavioral Gate-C candidate. It is an observational replay of the rejected `0.20` candidate.

## Required analysis

Report, overall and by label tier:

- unused-slot count and unused-slot-week totals;
- vacancy-age and empty-roster-age p50/p75/p90/p95/max;
- share of labels and slot-weeks vacant for at least 4, 8, 13, 26, and 52 weeks;
- scouting probability distribution during vacant label-weeks;
- pass rate and expected-versus-realized pass count;
- failure-reason counts and rates;
- time from first vacancy to next successful signing;
- time from empty roster to next successful signing;
- correlation or grouped comparison by `scoutingAbility`, tier, fullness, and recent-hit state;
- candidate-score rejection concentration by tier and preferred/new-supply genre where available;
- number of labels that remain empty despite eligible candidates and affordable advances;
- number of labels that repeatedly pass scouting but still remain materially under capacity.

The analysis must separate label-weeks from slot-weeks. One label with ten unused slots is not equivalent to one label with one unused slot.

## Decision rules for the next directive

### A. Random scouting gate is binding

Use this conclusion only if long vacancy durations and unused-slot weeks are predominantly associated with `ScoutingRandomGate`, while no-eligible, score, and affordability failures remain secondary.

If supported, recommend opening the vacancy-response policy in a new amendment. Prefer evaluating vacancy-age urgency or a bounded cadence floor over changing `scoutingAbility`, because ability is also a quality parameter. Do not implement the new policy under this handoff.

### B. Candidate supply is binding

Use this conclusion only if `NoEligibleCandidate` is material or candidate failures concentrate in specific era/genre/tier cells despite repeated scouting passes. Distinguish absolute pool exhaustion from genre availability, cooldown, and candidate-quality mismatch.

Only this result may justify requesting formation-rate or formation-mix authority. Do not assume that a larger aggregate pool repairs a filtered supply mismatch.

### C. Candidate scoring is binding

If passing labels commonly have candidates but reject the best score, report whether failures arise from low base quality, genre mismatch, reputation/risk treatment, or label scouting ability. Do not change the `0.3` threshold or scoring weights without a separate directive.

### D. Affordability is binding

If estimated or actual advance affordability is material, report it separately. Do not tune advances, reserves, overhead, royalties, or finance rules under Directive 6.

### E. Hard-cap capacity is intentionally unused

If labels behave consistently with a roster target below `maxRosterSize`, identify the evidence and recommend adding an explicit target-roster concept rather than treating every unused hard-cap slot as a vacancy. Do not infer intent merely from persistent emptiness.

## Explicit stop conditions

- Do not change `0.20` to `0.25`.
- Do not change `scoutingAbility` values or distributions.
- Do not change `F = 0.10` or the 300-per-year formation accumulator.
- Do not change attempts per passing label, candidate scoring, affordability, drop thresholds, cooldown, exit horizons, release rules, format, finance, or genre constants.
- Do not run seed 1002, seed 1003, a holdout, Gate D, or a 520-week replay.
- Do not accept the `0.20` candidate because observational telemetry becomes available.
- Stop on any changed pre-existing enabled stream; telemetry must be behavior-neutral.

## Completion deliverable

Append the exact implementation map, probe command/results, disabled hash comparison, enabled stream comparison, vacancy-duration tables, unused-slot analysis, failure taxonomy, and next-surface recommendation to `SimTools/ArtistPopulationLifecycleAudit.md`.

End with one of the decision-rule conclusions above and request explicit authorization for any behavioral follow-up. Until then, Gate C remains failed and Directive 6 remains incomplete.
