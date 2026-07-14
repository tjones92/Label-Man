# Dropped-artist roster lifecycle repair handoff

## Objective

Repair the enabled Genre Market V2 artist lifecycle so a terminal `Dropped` career transition is reflected atomically in label ownership, roster membership, the unsigned/free-agent pool, release eligibility, and later re-signing. Preserve the current specialist fulfillment and emerging-project memory candidate, the disabled byte-exact boundary, and release capacity. Do not launch a 520-week run until this repair passes the fixed, disabled, 52-week, and 104-week gates below.

## Correct interpretation of the defect

The `Dropped` status **does** register in `SimulatedArtist.careerState`. The missing operation is the corresponding label/roster transition.

Current failure chain:

1. `SimulatedArtist.UpdateCareerState` can change a signed artist directly to `CareerState.Dropped` after the relevant flop threshold.
2. `RosterManager.RecordChartRunComplete` subsequently calls `AILabel.ShouldDropArtist`.
3. `AILabel.ShouldDropArtist` has no unconditional `Dropped` case. Its ordered-state tests do not match `Dropped`, so the method normally returns false.
4. `AILabel.DropArtist` and `ArtistManager.DropArtist` are therefore not called.
5. The artist remains in the label roster with the old `labelId`, remains `isActive`, and is accepted by `GenreSupplyService.IsEligibleExistingArtistForRelease`.
6. The format fork does not recognize `Dropped` as a valid career band and serializes it through `New/Unsigned (unexpected-state fallback)`.

This is not a display-only problem. In the frozen seed-1001 decade, `5,932 / 6,166` 1969 release decisions (`96.20%`) use artists whose career state is `Dropped`. The current `d6-fulfillment-emerging-memory-52b-enabled-1001` checkpoint already has `192 / 4,215` such decisions (`4.56%`) in 1960. The older decade shows the defect accelerating sharply: `54.14%` in 1961, `83.71%` in 1962, and more than `91%` from 1963 onward.

## Intended re-sign behavior already present

A correctly processed dropped artist can be re-signed:

- `ArtistManager.DropArtist` clears `labelId`, sets `careerState = Dropped`, and adds the active artist to `unsignedArtists`.
- `ArtistManager.GetUnsignedArtists` and `GetUnsignedByGenre` deliberately include active artists in either `Unsigned` or `Dropped` state.
- `RosterManager.TrySignNewArtist` selects from that pool on the enabled path.
- `AILabel.SignArtist` adds the artist to the new roster; `ArtistManager.SignArtist` assigns the new `labelId`, resets the state to `NewSigning`, removes the artist from `unsignedArtists`, and records the event.

The repair must make the missing transition reach this established path. It must also make pool insertion and ownership reconciliation idempotent so an artist cannot appear twice in `unsignedArtists`, in two rosters, or in a roster and the free-agent pool simultaneously.

## Current candidate to preserve

The working tree contains the uncommitted D6 specialist fulfillment and emerging-project memory repair on top of commit `67567f5`. Preserve those edits and their evidence:

- conserved fixed-texture redistribution of specialist launch stock;
- specialist uncharted replenishment after physical backorders;
- `ProjectPrior` memory scope for non-retained genres introduced in 1966 or later;
- all fixed probes currently passing;
- disabled 52-week replay byte-identical across all 45 CSV streams;
- enabled 52-week ratios: units `1.0318x`, gross `1.0361x`, label net `1.0320x`, market net `1.0402x`, Single units `1.0305x`, Album units `1.1337x`;
- TexMex fulfilled share uniquely highest in Southwest at `.0543%`.

Do not retune or rewrite the specialist texture, stock budget, uncharted restock service, project-memory rule, format noise, Album priors, historical keyframes, supply weights, finance, charts, distance, seasonality, or release-growth constants while repairing roster lifecycle.

## Compatibility boundary

Disabled behavior must remain byte-identical. The legacy disabled path currently contains the lifecycle defect, so either:

1. gate the corrected ownership/release-eligibility behavior behind the live Genre Market V2 boundary, leaving disabled execution unchanged; or
2. obtain explicit authorization to replace the disabled baseline before making a global lifecycle change.

Use option 1 unless a new directive explicitly authorizes option 2. Do not add, remove, or reorder disabled RNG calls. Do not change the set or headers of the 45 disabled CSV streams.

## Required implementation properties

### 1. Atomic enabled-path terminal transition

Provide one authoritative operation for a signed active artist whose updated state is `Dropped`:

- capture the owning label before clearing `labelId`;
- remove the artist from that label roster exactly once;
- clear label ownership;
- retain `careerState = Dropped`;
- keep `isActive = true` so the artist is a valid free agent rather than retired;
- add the artist to `ArtistManager`'s unsigned pool exactly once;
- record one coherent career event;
- do not consume RNG merely to recognize a terminal state.

The natural first call site is `RosterManager.RecordChartRunComplete`, immediately after `UpdateAfterChartRun` returns a `Dropped` state and before probabilistic `ShouldDropArtist` handling. Monthly roster review should also reconcile any already-leaked terminal roster member on the enabled path.

### 2. Release-selection safety invariant

Even with atomic cleanup, add a deterministic enabled-path guard so `Dropped`, `Disbanded`, and `Retired` artists cannot reach release selection or the format fork while rostered. This is a safety invariant, not a substitute for moving them into the free-agent pool.

Do not change the shared legacy predicate unconditionally if that would alter disabled execution. Prefer an explicit live-path predicate or reconciliation pass. A terminal-state candidate reaching `DecideRelease` should be treated as an invariant failure in fixed probes, not mapped to the New/Unsigned career band.

### 3. Preserve release opportunity through roster turnover

Simply filtering terminal artists will hollow out rosters and collapse the release ladder. The repair must exercise the existing free-agent/scouting path and keep labels capable of replacing dropped acts.

Requirements:

- a dropped artist becomes available to other labels through the existing unsigned pool;
- a re-signed dropped artist becomes `NewSigning`, has exactly one new owner, and leaves the pool;
- the old label may sign a replacement under the existing enabled supply/lifecycle rules;
- no immediate synthetic release is created to hide a missing roster;
- no release roll, project, or finance entry is fabricated;
- successful releases and scheduled Album projects must remain within the inherited `[0.85,1.15]` control bands.

If the existing scouting cadence cannot replace enough roster capacity, diagnose that separately before changing its frequency. Do not compensate by weakening drop thresholds or re-admitting terminal artists to release selection.

### 4. Do not conflate terminal states

- `Dropped`: active free agent; eligible for later re-signing.
- `Disbanded` / `Retired`: terminal and not signable unless a separate future reunion mechanic is authorized.
- `Unsigned`: never signed or returned to an unsigned state by an explicitly authored path.

The current enum conflates career and contract concepts, but this repair should use the existing schema unless a schema migration is separately authorized.

## Fixed probes required before simulation

Add deterministic probes covering all of the following without changing disabled/prewarm output:

1. A signed artist crosses the flop threshold, becomes `Dropped`, leaves the old roster, clears `labelId`, and appears exactly once in the unsigned pool.
2. Repeating lifecycle reconciliation is idempotent: no duplicate pool entry or duplicate career event.
3. A rostered `Dropped`, `Disbanded`, or `Retired` artist is ineligible for an enabled release and cannot reach `DecideRelease`.
4. The same legacy/disabled setup retains its exact prior eligibility and RNG behavior.
5. A dropped active artist is visible to enabled signing candidates when its genre is available for new supply.
6. Re-signing removes the artist from the pool, adds it to exactly one roster, assigns the new `labelId`, and resets the state to `NewSigning`.
7. A retired or disbanded artist is not returned by unsigned/signing queries.
8. Roster ownership invariant: every active signed artist has one matching roster owner; every free agent has no label owner; no artist belongs to both sets.
9. Terminal states never use `New/Unsigned (unexpected-state fallback)` at the enabled format fork.
10. Existing specialist-stock, uncharted-service, emerging-memory, supply-selection, and disabled-neutrality probes remain unchanged and pass.

## Validation ladder

### Gate A: build and fixed suite

- `dotnet build "Label Man.sln" --no-restore` passes with no new warning.
- The full Genre Market V2 fixed probe suite passes.
- `git diff --check` passes.

### Gate B: disabled replay

Repeat the current seed-1001 disabled 52-week control. All 45 CSV streams must remain byte-identical to `d6-fulfillment-emerging-memory-52b-control-1001`. Any disabled difference is a hard stop.

### Gate C: enabled 52-week full telemetry

Run seed 1001 with full telemetry and compare against the current D6 enabled/control pair. Require:

- zero release decisions with `Dropped`, `Disbanded`, or `Retired` career state;
- zero enabled format decisions using the unexpected career-state fallback;
- ownership/pool invariant violations equal zero;
- nonzero drop-to-pool transitions and internally reconciled re-sign transitions;
- successful releases, scheduled Album projects, format units, and Album decision share inside inherited gates;
- units, gross, label net, and market net inside `[0.90,1.10]`;
- TexMex remains uniquely Southwest-highest after fulfillment;
- the D6 stock and `ProjectPrior` probes remain satisfied.

### Gate D: enabled 104-week checkpoint

A 52-week run is insufficient because the prior leak rises from roughly `5%` in 1960 to `54%` in 1961. Before any 520-week run, require a seed-1001 104-week enabled checkpoint, with a matching disabled/control comparison if one is not already available. Apply the same terminal-state, ownership, release/project, format, and economic gates in both years. Report annual roster sizes, drops, pool size, signings/re-signings, release attempts, successful releases, and selection failures by label tier.

If release volume collapses, the pool grows without re-signing, or labels repeatedly re-sign the same just-dropped act, stop and diagnose roster replenishment. Do not continue to 520 weeks and do not mask the failure with release-growth, finance, format, or historical-demand tuning.

## Explicit stop conditions

- No seed 1002, seed 1003, holdout, or 520-week run is authorized by this handoff.
- Do not tune the current stock or emerging-memory repairs from lifecycle results.
- Do not globally remove Dropped artists without confirming free-agent insertion and replacement capacity.
- Do not accept a lower release count merely because terminal decisions disappear.
- Do not change disabled hashes or silently freeze new baselines.

## Completion handoff

When Gates A-D pass, append the exact implementation map, probe results, disabled hashes, 52/104-week commands, roster-flow counts, economic/format comparisons, and remaining limitations to `SimTools/GenreMarketV2Audit.md`. Only then request authorization for a fresh 520-week seed-1001 measurement candidate.
