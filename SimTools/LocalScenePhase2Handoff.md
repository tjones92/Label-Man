# Local scene Phase 2: persistent population and discovery

Branch: `codex/local-scene-phase-2`. Phase 1 bounded validation is recorded in `LocalScenePhase1Handoff.md`: six fixed/save jobs and all seven two-seed matched 52-week runs pass, including 84 byte-identical economic CSVs per comparison. The latest committed band work through `e0a3a91` is integrated; the primary checkout remains untouched.

Phase 2 implementation `f23400c` and the final availability refinements build with zero errors and six existing warnings. Bounded fixed, save, matched-control and migration/replay validation passes. This acceptance does not include the outstanding paired-seed decade gate.

## Ownership and behavior

`ArtistManager` remains the sole act population/birth owner; `BandLifeService` remains the people and formation-debt/credit owner. Scene history and indexes reference canonical artist IDs. Scene reads have no factory, removal, activation, offer, debt, credit, or quota operation.

`--enable-persistent-scenes` enables identity observation and scene persistence. It requires the existing population lifecycle. It is off by default. Observe-only still means identity only; off preserves stored identity/history but assigns no new scene metadata. Conflicting enable/disable flags and unimplemented recruitment/dynamics flags are rejected.

Each act records resident/visitor/session/touring relationships, observed place, dates, participation level and provenance. Performance participation is separate from `ProspectMarketStatus`: signed residents and latent contract prospects can be heard; an encounter never activates a latent prospect. The existing activation, ownership and cooldown service still decides who may sign. Terminal actors stay in the registry with closed scene history. An existing base change closes the previous resident relationship and creates one current relationship; formation origin is retained.

Satellite identity remains literal while scene catchment may use a playable hub. Owner-supplied dated guest presence preserves origin/base, rejects overlapping distant presence, and removes the actor from its home performance cast while away. No scene visit creates a tour. These are coarse week-level presences, not a validated travel or exact-date booking model.

## Replace creation and deletion together

Under persistence, `ScoutVenue` reads a current local cast of existing IDs. It bypasses both `GenerateLocalProspect` and `PurgeGeneratedProspects`. Every scene's cast advances at the world chart-week boundary whether visited or not. Keys use a separate seed namespace, so observers share one cast and repeat visits do not redraw it or the offered read repertoire. The initial lazy pass is deterministic when the world has not yet ticked.

The cast is a Phase 2 discovery bridge across the existing four room categories, not a claim that real dated gigs or venues have been simulated. Phase 3 owns actual rooms, bills, appearances and work accounting. No performance income or member practice is granted here. The participation probabilities and attention-sized casts are provisional gameplay choices, not historical population facts or innate city quality.

There is no consumer-affinity floor on local cultural supply in the persistent path. Room-family programming still applies. Empty or contract-unavailable casts remain possible; the player sees availability and can return or try another room. There is no fresh-talent fallback.

Existing generated/notebook discoveries are adopted by ID through `ArtistManager`, conserving current registry/person counts and attributes. Seeking acts join the authoritative unsigned pool once; latent/owned/terminal acts do not gain eligibility. Old actors already deleted before saving cannot be reconstructed from absent evidence. The old creation/cleanup path remains available only when persistence is disabled, preserving the off control. Durable scene actors cannot be deleted by that legacy cleanup after switching the feature off.

Player knowledge is an ID-based discovery ledger. Notebook removal does not remove an actor or scene history; writing a name does not add a population hold. Intentional short negotiation holds continue through the existing roster service. A common player signing commit rechecks the canonical object, exclusive owner, activation/cooldown, roster space and affordability after negotiation time is spent, before any payment or contract write.

## Save/replay contract

Save version 6 stores per-act participation inline, world persistence schema/cursor/current casts, ID-based player discoveries and the legacy generated-ID tracking needed for adoption. World and player restore run before scene relinking/migration. Disabled loads preserve stored metadata. The runtime fixed probe exercises real gzip world/player saves and reloads with persistence both off and on, followed by the same cast and canonical links.

Current casts are bounded to one world week (45 distinct identity/catchment scenes at catalog v1, four categories each); actor history stores significant joins/exits rather than daily snapshots. There is no second act/person copy inside a scene. Membership is indexed by scene and rebuilt from authoritative actor metadata on restore. A weekly population reconciliation is allowed; there is no all-artists-per-venue-per-day scan.

## Diagnostics and reproducibility

- `--local-scene-persistence-check` dispatches fixed probes in `SaveLoadRoundTripRunner`, with `--enable-persistent-scenes` and the normal band/population flags.
- `--local-scene-persistence-audit` emits separate start/end participation CSVs and checks duplicate residents, terminal residents and canonical active person references.
- `RunLocalScenePersistenceChecks.ps1` packages serial two-seed fixed tests and 26-week world/gzip round trips with hashes, logs and completion markers.
- `compare_local_scene_controls.py <manifest> --persistence` compares all existing CSVs and normalized early world snapshots, excluding only the explicit new scene metadata/diagnostic fields. It supports frozen control runs recorded by name in a manifest.
- The pre-existing `MissingSingletonsTemp.cs`/`Rolodex` class-name autoload diagnostic occurs in both control and treatment; it remains outside this scene change.

Broad economic acceptance still owes the established paired-seed decade gate. Bounded equality is not a claim of decade acceptance. Persistence remains opt-in while that broader gate is pending; Phase 3 is not part of this implementation.

## Completed bounded validation

- `SimLogs/scene2-fixed-v2/runs.json`: both seeds 1001 and 2002 pass all 45 fixed checks. Checks include canonical adoption, residence/history, guest presence, repeat scouting, unchanged global RNG, latent/signed availability, stale signing rejection, future-schema rejection before mutation, and actual player/world gzip reloads with persistence off/on. Two additional 26-week runs pass exact world/gzip round trips.
- `SimLogs/scene2-matched-v1/comparison.json`: all five 52-week comparisons pass. Off controls for both seeds, persistent treatments for both seeds and a repeated seed 1001 match all 84 existing economic CSVs byte for byte against the frozen `e0a3a91` controls. Early world snapshots match after excluding only the explicitly listed scene metadata. Each enabled end census has 9,200 residents and zero duplicate residents, terminal residents or missing canonical active people.
- `SimLogs/scene2-replay-v1/comparison.json`: two eight-week continuations from a frozen legacy world match all 88 CSVs, including scene diagnostics, and the entire decompressed migrated world exactly. All 7,042 legacy acts survive. No unknown legacy hometown becomes an invented origin; all legacy bases resolve. All 180 current cast windows are unique and every cast ID resolves. The initial native weekly formation budget adds 42 acts, without a scene-owned birth source.
- `SimLogs/scene2-replay-v1/integrity.json`: every recorded Phase 2 weekly population/labor integrity metric has maximum zero across matched and replay runs (ownership conflicts, duplicate roster/pool/seeking entries, terminal roster/release eligibility, latent unsigned entries, missing seeking entries and contract-status conflicts).

The matched family uses the core `f23400c` implementation, recorded by its source/assembly contract. Final fixed checks, 26-week round trips and legacy replay use the final assembly with UI availability, early stale-offer rejection and unknown-hometown wording refinements. Those refinements do not change the headless native economic path. Run manifests preserve exact flags, hashes, completion markers and exit codes.

Testing was serial, with process and free-memory checks before each launch; no test remains running. The primary checkout subsequently advanced to `01ee660`; that later band change is outside this frozen comparison and has not been merged into the scene branch. The primary checkout was not edited.

The next phase is actual scene rooms, bills, appearances and work accounting. Phase 3 has not started. The broader decade gate remains open before default enablement or a claim of long-horizon economic acceptance.
