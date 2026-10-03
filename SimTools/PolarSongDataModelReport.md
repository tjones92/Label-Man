# Polar song: first data-model slice

October 2, 2026. Branch: `codex/polar-song-data-model`. Checkpoint: `e1dcbae`, pushed to `origin/dealer-margin-and-flip` before implementation.

## Implemented

- Added the 34 Spec B archetypes, lyric modes, vocal approaches, meter/form, production, function, performer kinds, vocal-presence state, and a versioned taxonomy object. Missing legacy evidence is Unknown/null; no numerical archetype table or arbitrary genre-to-archetype inference was introduced.
- Added composition demo taxonomy, meter/form defaults, nullable word density and plasticity, canonical content tags, first-committed-master linkage and explicit frozen-plasticity provenance. A recording receives a copy of the demo taxonomy. The freeze API requires that first committed master and refuses later recordings or uncommitted preview IDs, including after save/load. Zero is a valid value distinct from missing.
- Added a plain-C# master registry separate from release Records. Single master IDs initially reuse record IDs; album-only tracks get the existing deterministic per-track key. Album releases have no one-song profile. Existing single snapshots/compilations reference their master; new performances receive separate entries and retain legacy reference provenance as parentRecordingId.
- Added A-side and B-side master links and a plug-master accessor. Shipping preserves the B-side link before its Record is discarded. Side reversal changes the accessor through the existing side flag, without changing rights-bearing side identities. Archived track snapshots and player reconstruction carry master IDs.
- Added non-destructive classification of known legacy tag IDs into instrumentation, production, scene and release arrays, including both enum-style and saved canonical spellings. Legacy genreTagIds remain untouched. Theme/delivery ambiguities, instrumental metadata and unknown strings stay unclassified.
- Persisted the registry under CompositionSaveData; world entities retain master fields through the existing field serialization contract. Player RecordSaveData explicitly maps both master IDs. Save envelope version is now 3; older versions remain accepted. Legacy records/tracks/archives get deterministic linkage at capture/rehydration; surviving release chronology plus ordinal ID determines the inferred first master. Shelf/player records are reconciled after player restoration.

The sole new numeric configuration constant is `PolarSongConstants.SchemaVersion = 1`, a schema identifier rather than behavioral tuning. Canonical Genre values, migration, song IDs, credits, controller snapshots, royalty routing, quality formulas, selection, chart behavior and RNG draw order were preserved.

## Validation

Build: `dotnet build "Label Man.csproj" --no-restore` passed with the existing unused-event warning in ChartManager. An elevated build resolved the NuGet.Config access limitation from recon.

`SaveLoadRoundTripRunner --weeks=0 --seed=1001 --polar-song-data-check` passed. It checks demo-copy isolation, single/album-only IDs, reused and archived masters, cover lineage, first-master freeze/zero/missing semantics, cancellation without a committed master, unchanged composition rights, ambiguous and canonical tag IDs, album exclusion, both-side DTO serialization, old player record fields, deterministic old album keys, world serialization/rehydration, and repeated-load taxonomy.

An additional legacy flipped-disc probe verifies that the original A and B titles/genres are recovered from the correct side fields when master metadata is absent.

`SaveLoadRoundTripRunner --weeks=2 --seed=1001 --integration` passed the existing real player/world gzip save/load integration, including inventory, servicing, runner routes, deals, plant credit and pending offers.

Controls were built from checkpoint e1dcbae and run before data changes. Candidates were rebuilt from this slice, with the same default flags, seeds and 52-week window. No holdout or three-seed final ladder was used. These are one-year behavior-neutral checks, not the unmeasured 1960–63 live-set baseline.

Run comparison with `node SimTools/analyze-polar-data-neutrality.mjs`. The analyzer requires completion markers, matching CSV stream families, and exact whole-file bytes; it does not use numerical tolerances. Evidence is written to ignored `SimLogs/polar-data-neutrality.json`, with control/candidate logs and CSVs beside it.

| Development seed | Window | Existing CSV streams | Comparison |
|---|---|---|---|
| 1001 | 52 weeks, through December 30, 1960 | 79 | Byte-identical |
| 1002 | 52 weeks, through December 30, 1960 | 79 | Byte-identical |

## Deliberate boundaries for the next slice

Plasticity remains null for legacy/generated songs whose archetype/form evidence has not yet been derived. This slice supplies the ownership and one-time freeze mechanism; it does not invent the numerical table necessary to call it in ordinary gameplay. There is no fit, cached polar profile, arrangement resolver, refusal, observation model or polar UI yet.

Existing lead-single promotion still reselects material, so it currently represents a separate legacy performance/master. Changing that path to reuse the album cut's composition/master would alter selection outcomes and belongs behind the next behavior boundary. Ordinary reused-single/compilation snapshots already retain linkage here.

Historical reference taxonomy and reliable heard-version selection remain part of shadow derivation/resolution. Existing parentRecordingId preserves originalRecordId provenance; it must not be interpreted as a fully verified historical reference chain when the legacy parent is missing. Missing compositions in old player-only envelopes cannot be reconstructed from incomplete writer/controller snapshots by this slice.

The rights-controller fallback identified in recon remains unchanged. No claim of new writer-payment equivalence or rights-routing correction is made. Current credits/controller snapshots remain authoritative for the existing settlement behavior.

Typed tag classification is a compatibility adapter, not tag lifecycle/economy. Mood and pace use nullable authored strings until their vocabulary is reviewed. Taxonomy schema versioning is present; profile cache versioning will accompany the actual deriver.

Source SHA-256 values for all modified runtime/probe/analyzer files are recorded in `PolarSongDataModelHashes.json`; this report is excluded from that manifest to avoid a self-referential digest.
