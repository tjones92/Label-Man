# Artist population fresh-prospect preference handoff

## Status and authority

This handoff governs the next bounded artist-population pass. It supersedes `ArtistPopulationReleaseCapacityInvestigationHandoff.md` for new implementation and measurement, while retaining that file and `ArtistPopulationDecadeValidationHandoff.md` as historical evidence and stop-state context.

Retain the current enabled-only 7,000-artist initial market as the experimental foundation. It is not an accepted decade candidate. Its one 260-week seed-1001 treatment failed the successful-release floor in blocks 3-5, so do not run 520/522 weeks, seeds 1002/1003, a holdout, or a disabled acceptance replay until a new bounded candidate passes.

The authoritative evidence is the section `Enabled 7,000-artist initial-market experiment (2026-07-14)` in `ArtistPopulationLifecycleAudit.md`.

## Frozen experimental foundation

Retain these boundaries unless a probe demonstrates a correctness defect:

- The disabled route initializes exactly 3,000 artists.
- The enabled route allocates launch rosters from the same first 3,000 artists, then materializes a 4,000-artist unsigned reserve for a 7,000 total.
- Reserve generation uses `ArtistManager`'s isolated population RNG and does not consume the global simulation or `NameGenerator` streams.
- Initial-reserve artists do not emit per-artist event rows.
- A label's enabled operating roster target is its actual launch roster; an initially empty new label receives one bootstrap slot. Hard maximum roster size remains physical capacity, not immediate demand.
- Never-signed artists remain in the labor market; only artists with prior contracts may age into inactivity.
- Enabled discovery prefers the label's region, falls back nationally only when needed, exposes a deterministic 4-12 artist slate, and refreshes on the existing four-week window without another RNG draw.
- One qualifying label receives one scouting draw and at most one signing attempt per live week.
- Closed labels remain observable but consume no scouting RNG, retain no urgency, and cannot attempt or complete a signing.
- The accepted 12-week / `0.25` vacancy urgency persists until the operating target is filled.
- The experienced-comeback contract state, three-current-contract-flop evaluation, 13-week first performance cooldown, and 52-week repeat recovery remain unchanged.
- Candidate score formula and `0.30` threshold, actual affordability, advances, release cadence and selection, Album choice, finance, market, format, genre, regional, and historical rules remain controls.

Do not increase the pool to 10,000 during this pass. The failed 7,000 treatment ended with 2,973 never-signed unsigned artists, so remaining capacity failure is not a shortage of fresh entities.

## Evidence constraining the next correction

The final 7,000 treatment was `d6-pool7000-discovery-middecade-1001`. It completed 260 weeks with all Album blocks, aggregate economics, aggregate format units, telemetry, closed-label behavior, and structural invariants healthy, but failed release blocks 3-5:

| Block/year | Release ratio | Album ratio |
|---|---:|---:|
| 1960 | 1.0202 | 1.1055 |
| 1961 | 0.8919 | 0.8963 |
| 1962 | **0.8199** | 0.8800 |
| 1963 | **0.8041** | 0.9835 |
| 1964 | **0.7193** | 0.9337 |

At week 260:

- active-label operating targets total 2,652 while rosters contain 1,756 artists;
- 2,973 never-signed artists remain unsigned;
- blocks 3-5 live scouting selected 2,676 dropped free agents and only 981 first-time artists;
- candidate-score rejections increased from 97 in block 1 to 1,262 in block 5;
- repeat-contract performance drops reached 865 and 910 in blocks 4 and 5; and
- third-or-later re-signings were 91, 229, and 497 in blocks 3-5.

The larger pool has therefore made a finite repeat-signing guard viable: there is now alternative fresh supply. Pool size itself is not the next variable.

## One authorized causal correction

Implement an enabled-only fresh-prospect preference at the existing candidate-evaluation seam.

Definitions:

- A **never-signed prospect** has `contractSequence == 0` and is not `CareerState.Dropped`.
- A **third-or-later performance comeback** is a dropped candidate whose `lastDropReason` is `Performance` and whose current `contractSequence >= 2`; signing that artist would begin contract three or later.
- A **qualifying fresh prospect** is a never-signed candidate already present in the current discovery slate who passes the unchanged `AILabel` score threshold and whose unchanged calculated advance is currently affordable.

Required selection policy:

1. Generate exactly the same bounded discovery slate in exactly the same order and without another RNG draw.
2. Evaluate the overall best candidate with the existing score formula and `0.30` threshold.
3. If that overall best candidate is a third-or-later performance comeback and the same slate contains at least one qualifying fresh prospect, select the highest-scoring qualifying fresh prospect instead.
4. If no qualifying fresh prospect exists in that slate, allow the existing overall result immediately. This is the finite escape and prevents an empty-roster deadlock.
5. Do not defer a first or second contract, a first comeback, or a departure caused by label closure, contract expiration, voluntary action, or lifecycle reconciliation.
6. Preserve the existing single actual signing attempt. Do not attempt a fresh candidate and then fall back to a comeback in the same label-week.

This is a preference guard, not a score bonus, score penalty, global quota, permanent ban, random coin flip, or changed cooldown. Do not broaden the discovery slate or lower the candidate threshold in the same candidate.

The smallest likely implementation is to let `AILabel` evaluate an optional candidate subset with the existing scoring function, then make the deterministic preference decision in `RosterManager.TrySignNewArtist`. Avoid copying the score formula into `RosterManager`.

## Required telemetry without bloat

Extend the existing enabled-only `label-scouting-vacancy-weekly.csv` row. Do not create another stream and do not emit candidate-level or reserve-artist rows.

Add only the fields needed to prove the policy:

- `neverSignedSlateCount`
- `qualifyingNeverSignedCount`
- `bestNeverSignedScore`
- `thirdPlusPerformanceComebackCount`
- `overallBestContractSequence`
- `freshPreferenceApplied`
- `repeatComebackDeferred`
- `freshPreferenceFallbackReason`

The fallback reason should distinguish at least `OverallBestNotGuarded`, `NoNeverSignedInSlate`, `NoQualifyingNeverSigned`, `FreshAdvanceUnaffordable`, and `FreshPreferred`. Reuse a compact value already available during evaluation; do not add a second evaluation pass solely for telemetry if the production decision already computes it.

Telemetry budgets against the retained 7,000 baseline are hard watch limits:

| Horizon | Retained baseline | Maximum `+5%` |
|---|---:|---:|
| 52 weeks | 23.16 MiB | 24.32 MiB |
| 104 weeks | 48.39 MiB | 50.81 MiB |
| 260 weeks | 137.97 MiB | 144.87 MiB |

The enabled lean family must remain 51 CSV files. Stop and remove the new telemetry shape if it exceeds these limits; do not solve bloat by dropping existing acceptance streams.

## Fixed probes

Extend the D6 suite from 38 probes with focused, RNG-neutral cases:

1. A third-contract performance comeback with the highest overall score loses to a lower-scoring never-signed candidate that still passes `0.30` and is affordable.
2. The highest-scoring qualifying never-signed candidate wins when multiple fresh prospects qualify.
3. A third-contract comeback remains eligible when no never-signed artist is in the slate.
4. A third-contract comeback remains eligible when fresh artists are present but none pass the unchanged score threshold.
5. An unaffordable fresh artist does not create a second signing attempt or an in-week fallback.
6. A first comeback is not guarded.
7. A label-closure departure is not guarded.
8. The disabled route retains its original candidate choice and RNG behavior.
9. Telemetry correctly records applied preference and each finite fallback reason.

Also retain all existing D5 suites and D6 probes 1-38.

## Required measurement sequence

Use seed 1001 only and preserve every existing CSV family.

Suggested run names:

```text
d6-pool7000-fresh-priority-probes-1001
d6-pool7000-fresh-priority-gateb-1001
d6-pool7000-fresh-priority-gatec-1001
d6-pool7000-fresh-priority-gatec-repeat-1001
d6-pool7000-fresh-priority-middecade-1001
```

Sequence:

1. Before editing, reproduce the block 3-5 signing-kind, contract-sequence, score-rejection, target-gap, and repeat-drop counts from the retained 260-week family.
2. Implement only the fresh-prospect preference, telemetry, and fixed probes described above.
3. Run `dotnet build "Label Man.sln" --no-restore`, `git diff --check`, accepted D5 suites, and the expanded D6 suite.
4. Run 52 weeks. Because a third-or-later contract should not exist at launch, investigate any change from the retained 4,400 releases and 1,205 scheduled Albums before proceeding. Do not automatically demand byte identity because added observational columns change hashes.
5. Run 104 weeks and one independent 104-week repeat. Require all 51 suffix-matched CSV streams to be byte-identical between the two new runs.
6. Compare release, Album, annual economy, annual catastrophic, format, population, closed-label, invariant, and telemetry-size gates.
7. Only if steps 3-6 pass, run one 260-week seed-1001 treatment.
8. Compare every 52-week block with weeks 1-260 of `d6-population-decade-control-1001`; do not accept a five-block aggregate in place of per-block results.
9. If the 260-week candidate passes, run the 52-week disabled seed-1001 replay and require all 45 frozen streams to match `d6-fulfillment-emerging-memory-52b-control-1001` by suffix and SHA-256.
10. Append exact source hashes, commands, completion markers, stream manifests, telemetry sizes, policy-use counts, block results, and invariants to `ArtistPopulationLifecycleAudit.md`.

No additional 260-week candidate is authorized in the same pass. Stop on the first hard failure; do not sweep pool size, slate size, score threshold, urgency, guard scope, or cooldown.

## Acceptance boundary before any decade request

Require all of the following:

- calendar formations are `300 / 300 / 300 / 300 / 294` through the partial 260-week endpoint;
- successful releases and scheduled Album projects are each within `[0.85,1.15]` in every 52-week block;
- aggregate individual-format units are within `[0.85,1.15]`;
- aggregate units, gross, label net, and market net remain within the inherited individual-seed band;
- every annual total-units and market-net ratio remains inside `[0.75,1.25]`;
- first-time signing events are nonzero in every block and mature live scouting no longer overwhelmingly depends on repeated performance comebacks;
- the preference applies only to third-or-later performance comebacks and reports nonzero finite fallbacks;
- active roster contraction is materially improved without Album overproduction;
- ownership, duplicate roster/pool, probation, cooldown, terminal, chronology, artist-selection, and closed-label invariants are zero;
- build, `git diff --check`, fixed probes, deterministic 104-week repeat, telemetry budgets, and disabled replay all pass; and
- the exact candidate source and evidence are frozen in the audit.

Only after those conditions pass may a separate authorization be requested for a 522-week seed-1001 control/treatment pair. Seeds 1002/1003 and the holdout remain gated behind a complete seed-1001 decade pass.

## Stop and diagnosis rules

- If 1960 changes materially, first inspect whether the guard accidentally touches first contracts, launch rosters, discovery ordering, or global RNG.
- If releases remain low while the guard rarely applies, inspect the bounded-slate score-rejection seam from existing telemetry; do not widen the slate in the same pass.
- If releases recover but Albums exceed `1.15`, join preference decisions to contract sequence and Album-project identity before changing any Album rule.
- If first-time signings rise but rapid first-contract drops erase the gain, report that cohort outcome; do not weaken probation in the same pass.
- If telemetry exceeds budget, reduce fields or representation, not the retained acceptance streams.
- Any hard gate failure ends the pass. Preserve artifacts and request a new one-variable amendment.

## Current source-state identity

The retained nine-file functional-source manifest is:

```text
9ACDCB2E824D98C9CD77C1A0620823EA6F034FC89AAC499FA23C960A083EA3EC
```

It covers:

```text
Data/AILabel.cs
Data/AlbumProject.cs
Data/SimulatedArtist.cs
Systems/ArtistManager.cs
Systems/ChartManager.cs
Systems/CompetitorManager.cs
Systems/RosterManager.cs
SimTools/ArtistPopulationLifecycleProbeSuite.cs
SimTools/ChartAuditRunner.cs
```

Do not describe the retained 7,000 foundation as accepted or frozen for shipping. It is the preserved starting point for the one fresh-prospect preference experiment above.
