# Artist population release-capacity investigation handoff

## Current status

This is an investigation handoff, not authorization to resume the decade ladder. The calendar-year formation defect is corrected in source, but no release-capacity candidate has passed the bounded seed-1001 gates. The current worktree contains the last failed experimental candidate and is intentionally unfrozen.

Do not run 520 or 522 weeks, seeds 1002/1003, or a holdout during this pass. Preserve all existing CSV families. A future date-complete decade must use 522 Friday ticks; the old 520-tick commands are obsolete.

## Retain unless disproved

- Calendar formation is owned by `GameDate.year`: reset fractional carry at a year boundary and cap each complete year at 300. Do not restore December or terminal-horizon special cases.
- The disabled route must remain behaviorally frozen and must not emit enabled-only population streams.
- Closed labels remain observable but cannot consume scouting RNG, sign artists, or retain urgency state.
- One qualifying label gets one scouting draw and at most one signing attempt per live week.
- Candidate enumeration, score ordering, actual affordability, release selection, finance, formats, and market rules should be treated as controls unless evidence identifies a direct correctness defect.

## Source state to audit first

- `Data/AILabel.cs` accepts an explicit minimum candidate score.
- `Systems/RosterManager.cs` currently applies a zero post-launch non-empty score floor, best-candidate empty-roster recovery, the accepted `0.25` urgency probability, and an experimental post-launch urgency carry that subtracts one 12-week interval after a successful signing.
- `Systems/ArtistManager.cs` contains the calendar formation quota and the existing free-agent contract reset.
- `SimTools/ChartAuditRunner.cs` adds enabled-only candidate-score policy fields to label vacancy telemetry.
- `SimTools/ArtistPopulationLifecycleProbeSuite.cs` covers the calendar quota, score policies, and current urgency state, but passing probes do not imply a capacity pass.

The urgency-carry candidate is failed, not a baseline to defend. Replace or remove its post-launch score/urgency behavior if the next causal amendment makes it unnecessary.

## Evidence that constrains the next pass

| Candidate | Release ratios, 52-week blocks 1-5 | Album ratios, blocks 1-5 |
|---|---|---|
| zero score floor + reset, urgency `0.30` | 0.9631 / 0.8827 / 0.8432 / 0.7776 / 0.7385 | 1.1495 / 1.0150 / 1.0465 / 1.1041 / 1.0853 |
| zero score floor + persistent urgency `0.25` | 0.9631 / 0.9158 / 0.9047 / 0.9782 / 1.0086 | 1.1495 / 1.0362 / 1.1542 / 1.3742 / 1.3074 |
| zero score floor + one-interval carry `0.25` | 0.9631 / 0.9075 / 0.8382 / 0.7728 / 0.7285 | 1.1495 / 1.0400 / 1.0455 / 1.0862 / 1.0246 |

The persistent endpoint proves roster/release capacity can be restored, but repeated signings overproduce Album projects. A scalar urgency sweep is therefore not an adequate next investigation.

In the final bounded run, annual performance drops were 806, 1,576, 1,713, 1,643, and 1,597 for 1960-1964. Performance drops after a prior re-signing were 0, 265, 1,172, 1,364, and 1,411. Label closures add roughly 425-596 departures per mature year. Release artist-selection failures remain zero.

## Authorized liberties

The next pass may make a coherent enabled-only lifecycle correction after establishing it with telemetry and probes. In particular, it may:

- add observational fields for pre-drop career state, prior contract count, signing-to-project timing, per-label open slots, and drop-to-re-sign-to-drop cycles;
- preserve and restore an artist's appropriate pre-drop career tier on re-signing instead of forcing every experienced free agent through `NewSigning`, if evidence supports that model;
- distinguish a genuinely new contract probation from an experienced-artist comeback policy;
- amend performance-drop or re-sign state transitions when they are the demonstrated source of pathological churn;
- remove or replace the experimental post-launch zero score floor and urgency carry;
- adjust the enabled-only vacancy policy as part of the same causal correction; and
- run seed-1001 treatment diagnostics up to 260 weeks, beginning with 52/104-week checkpoints.

These liberties do not authorize tuning release cadence, Album choice rules, finance, market demand, format weights, genre keyframes, regional routing, or historical inputs to hide an upstream roster-state defect. Avoid broad constant sweeps. Prefer one explainable state-model change, then measure it.

## Required investigation sequence

1. Reproduce the final run's re-sign/drop cohorts from `artist-population-events.csv` and join them to Album-project identity/timing. Determine whether experienced free agents forced to `NewSigning` account for the excess Album share in the persistent-urgency endpoint.
2. Add the smallest telemetry/probe seam needed to distinguish new prospects, first contracts, experienced free agents, closure departures, and performance re-sign cycles.
3. Implement one coherent state correction. Remove obsolete experimental policy if that correction supersedes it.
4. Build, run D5 plus D6 probes, and run a 52-week treatment. Preserve the exact 1960 release/Album result unless the new model has an evidenced reason to change it.
5. Run 104 weeks and an independent deterministic 104-week repeat. Only then run one 260-week seed-1001 treatment checkpoint.
6. Compare every 52-week block against `d6-population-decade-control-1001` weeks 1-260. Do not rely on the five-year aggregate alone.
7. After a candidate passes, rerun the 52-week disabled seed-1001 replay and require all 45 frozen streams to match before proposing a 522-week checkpoint.

## Pre-decade acceptance boundary

Before requesting a 522-week seed-1001 pair, require:

- each complete calendar year has 300 formations; the partial 260-tick 1964 block has 294;
- successful releases and scheduled Album projects are each within `[0.85,1.15]` in every completed 52-week block;
- the inherited economic and format bands pass;
- ownership, duplicate roster/pool, probation, cooldown, terminal, chronology, and release-selection invariants are zero;
- no closed label scouts or signs;
- first-time signings remain nonzero and the population does not depend solely on recycling dropped artists;
- build, `git diff --check`, fixed probes, deterministic 104-week repeat, and disabled replay all pass; and
- the exact candidate source state, commands, run names, completion markers, and hashes are appended to `ArtistPopulationLifecycleAudit.md`.

Only after all of those conditions pass may a separate pass request authorization for a 522-week seed-1001 control/treatment pair. Seeds 1002/1003 and the holdout remain gated behind a complete seed-1001 pass.
