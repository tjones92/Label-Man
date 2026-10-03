# Polar-song directive review and proposed amendments

Reviewed October 2, 2026 against the current working tree and the supplied `DirectiveN-PolarSongSystem-Codex.md`.

Status: design review only. No implementation, simulation, save migration, or calibration was performed. The attached directive is design material, not authorization to execute its phases. Existing local code changes were left untouched. These amendments are a proposed revision of the sketch; they do not constitute approval of new numerical tuning.

## Decisions already settled by the user

- Unrecorded compositions carry `DemoTaxonomy`; it seeds their first recording.
- Plasticity is immutable at composition level. A transformative cover cannot reduce it.
- Meter and form have composition defaults and recording overrides.
- Lyric mode and vocal approach belong to recordings alongside archetype, pace, mood, and genre.
- Implement the minimum act-profile derivation needed for polar fit when implementation is authorized. A wider band/musician redesign is deferred. Individual axis-mapping choices should not become a fresh scope-approval blocker.

Retain the separation of Capability, Identity, and Moment; the ban on player-facing aggregate fit; deterministic resolution; composition-based rights; and staged shadow validation. These are the strongest parts of the sketch.

Specs A, B, and C were not present in the repository or the Downloads search. Consequently, their full formulas, axis definitions, archetype list, and tag mappings remain unverified. Do not invent their missing contents or treat the directive's approximate archetype count as an exact acceptance target.

## 1. Include the tag split, with a compatibility boundary

**Recommendation: include it. The blanket D7 exclusion is unnecessarily broad.**

D7 is historical work, but its runtime systems remain relevant. `Data/Genre.cs` retains canonical genre identities and the mixed `GenreTag` enum. Records actually persist string `genreTagIds`. `Data/GenreMigration.cs` adds legacy scene identities, sorts/deduplicates tags, and preserves the canonical genre migration. Existing D7 handoffs describe both completed work and remaining calibration; their presence does not prove a current scheduling conflict or that every genre issue is closed.

Replace the tag-split non-goal with:

> Typed production, scene, lyric-mode, release, instrumentation, technique, and content classification is in scope where required by polar profiles. Preserve canonical `Genre` values and IDs, existing genre migration, saved tag IDs, and existing market/format/acceptance behavior. Provide an explicit legacy-tag mapping; retain unknown tags and ambiguous metadata without silently assigning new meaning. During migration, keep a legacy compatibility projection for existing consumers. Tag lifecycles, access costs, innovation rewards, and economy effects remain deferred.

Do not convert `Romantic`, `Topical`, or `Protest` blindly into one lyric mode: theme and delivery are different facts. Likewise, an instrumental record needs an explicit vocal-presence rule; a metadata split alone does not resolve that. Verify the detailed mapping against Spec C when supplied.

## 2. Use the existing composition identity; define a durable master identity

`SongComposition.songId` already identifies the composition. `Record.recordId` already identifies a released record/master. `Record.songId` and `originalRecordId` already express part of the intended linkage. Avoid renaming these simply to match the sketch.

The important gap is broader than a new field on `Record`:

- `AlbumTrack` stores song and source-record metadata but is also used for recordings that never become singles.
- An existing recording reused on an album or compilation must keep its master identity and taxonomy.
- A new performance of the same composition must receive a new master identity.
- The player's B-side recording is discarded at shipping; selected B-side facts are copied onto the A-side. A plug-side flip swaps performance attributes without swapping composition identity.
- `SongRecordingMemory` is an outcome summary appended after a chart run, not an authoritative master registry.

The future recon should decide whether a small shared master registry or a reusable recording metadata object best fits these paths. It must cover singles, album-only cuts, B-sides, archived versions, and references to pre-game recordings. A whole album must not acquire one song's polar profile merely because its market representation is a `Record`.

Use `parentRecordingId` to identify the immediate reference performance; retain legacy provenance fields with documented meanings. A compilation or format reissue does not become a cover. Derive a coarse Original/Cover/Traditional/Derived display category from the existing detailed `SongMaterialSource`, rather than creating a competing source authority.

## 3. Make demo, plasticity, and reference selection unambiguous

For unrecorded material, compute provisional plasticity from the demo and composition defaults. On the first committed master, freeze composition plasticity exactly once under the agreed first-recording rule. Save the frozen state explicitly. A preview, cancelled session, or later cover cannot establish or overwrite it. Legacy migration chooses the earliest known master using stable chronology and ID ordering; if none survives, use the demo fallback and record that provenance.

“Once a recording exists, the recording is authoritative” needs a reference rule. Different acts can know different versions. Carry the reference master ID from scouting or repertoire through rehearsal and session selection. Only released/known versions available at the current game date qualify. Never choose the latest version implicitly, consult future chart outcomes, or rely on dictionary order.

Unrecorded demos remain usable even if a master exists that this act has never heard. Pre-game standards need deterministic reference-version metadata rather than fabricated in-game releases.

## 4. Specify the actual transformation before wiring selection

The resolver currently chooses the nearest archetype to the “heard” vector without defining the act's pull. If “heard” means the reference profile, its own archetype is usually nearest, so the core transformation can collapse into retaining the original archetype.

Specify three distinct objects:

1. Reference profile: what the act heard.
2. Proposed arrangement: a bounded pull toward the act's identity, constrained by composition plasticity, interpretive reach, performer capability, arrangement intent, and session resources.
3. Realized recording profile: derived from the resolved new taxonomy, then evaluated independently against the act.

The resolver should take a stable planned master ID and immutable inputs, return a proposal, and perform no world mutation. A separate commit operation creates the master and stores lineage. Previewing a session must not mint IDs or change future hash inputs. Use stable candidate ordering and explicit deterministic tie resolution.

Keep demand and identity distance separate: one describes required performance resources; the other describes stylistic movement. Normalize Stretch as specified, but measure the resolved arrangement's movement rather than an undefined reference-fit quantity. Recompute final fit after resolution. A long stretch can fail or become incoherent, so it is evidence of rearrangement, not automatically proof of good originality.

Covers should retain genre options rather than always taking the act's primary genre. `GenreSupplyService` already selects project genres and `ArtistEvolution` observes those choices. Respect the supplied project's canonical genre context and availability; do not silently overwrite it with the act's center. Individual track variation needs an explicit relationship to album/project market genre, preserving the existing supply accounting.

Resolve lyric mode and vocal approach separately. Current musicians have roles and general skills, not an authored vocal range or lyric-mode range. A minimal capability adapter can approximate delivery suitability; lyric mode also reads interpretation and canonical content. A permitted plea-to-demand change can alter emphasis without inventing new canonical lyrics or credits. Actual rewrites remain outside this directive.

## 5. Keep the act adapter small and explicit

Use current active members, `GetLeadSinger()`, act vocal power, musicianship, cohesion, studio performance, current/formation genres, and existing evolution state. `GetLeadVocalist` is not the current helper name.

Capability derives from performers and real session support. Identity starts from an explicit genre-style prior and bounded adjustments from traits/history. Evolution dispositions such as experimental appetite, roots attachment, and commercial pragmatism are better suited to reach, flexibility, and refusal than to directly declaring every stylistic coordinate. Ambition alone must not mean sophisticated, and age alone must not mean mature.

Persist a mapping/version identifier and distinguish inferred capability from observed capability. Specify fallbacks for solo acts, instrumental acts, missing vocalists, absent evolution state, and missing studio context. Producer craft may support execution but cannot invent a singer or a member's technical ability.

Keep C6's dominant-demand surplus if needed for its original formula, but distinguish it from `WorstAxis`, which should identify the largest relevant unmet demand. The highest demand axis is not necessarily the failing axis. Final definitions await Spec A.

## 6. Replace every relevant material decision, including live sets

The draft names `SampleBest` and `SampleBestAcross`, but the current code has additional decision paths:

- `PlayerDesk.PickCommissionSong` ranks a genre pool by `GetCraftScore()` and familiarity without an act parameter.
- `CoverCatalogFor` orders player choices by commercial hook times coarse genre fit.
- `BuildLiveSet` randomly selects from standards/hit pools independently of the recording selector.
- `SongMaterialSelectionService` also weights source-category candidates after choosing songs within pools.
- Album cuts and promoted singles take separate material paths through `CompetitorManager`.

The integration inventory must cover candidate generation, shortlist ordering, source-category choice, player catalog ordering, commissions, live sets, album cuts, and final session application. Changing only inner-pool ranking can leave global scalar preference in place.

Amend the live-set non-goal: retain set lengths, original/cover counts, and existing composition-count constants, but use polar suitability to choose which eligible covers fill those slots. This is necessary for the stated live-set goal. A failed suitability search should have an explicit deterministic fallback, not an automatic standard.

Use the aggregate fit score only for autonomous internal decisions. Player catalogs can group by known genre/source, filter, and compare perceived components; they should not hide the same aggregate behind an unexplained “best” ordering.

## 7. Give fit an explicit downstream contract

The goal says the three components reach chart/critic behavior separately, but the phases do not specify those consumers. Existing material application blends an expected hook into the record's hook strength and modifies originality and production.

Inventory those bridges before implementation. Capability should inform execution/realization; Identity should inform conviction and interpretive coherence; Moment should read current market relevance. Preserve raw composition traits as ceilings or inputs where specified, without using them to inflate demand axes or restore a global material ranking.

Reuse existing genre-market state for Moment where possible. A new centroid must use information available at that date, avoid feedback loops from current candidate outcomes, and have a neutral fallback if observations are absent. Do not pay the same market preference once through Moment and again through existing genre acceptance without an explicit intended effect.

Add this consumer contract to the shadow phase and validate behavior wiring independently. Historical fixture labels must not be hardcoded exceptions to the general mapping.

## 8. Treat definitive versions as pressure, not permanent deletion

The current `CoverFatigueShadow` dampens selection; it does not retire a composition. The draft's permanent removal would fight the transformation mechanic and exhaust pools over a long game.

Recommendation: distinguish successful reinterpretation, commercial success, and cultural definitiveness. Predictive arrangement fit alone cannot make a recording definitively owned before release. Feed an idempotent outcome update when evidence exists; apply bounded fatigue, cooldown, or version/act-specific shadow. Preserve the possibility of later eras and different acts revisiting the composition. Permanent song-wide retirement would be a separate design choice.

Define the current `SongRecordingMemory.definitiveVersionScore` and any new master-level score as one synchronized fact, not two independently updated authorities.

## 9. Preserve rights provenance without broadening the economy

Current `ApplyToRecord` already copies composition credits and controller metadata onto recordings. Publishing routing and mechanical charges consume controller snapshots. Credits describe writers, but present routing is not a general payment ledger for every individual credited writer share.

Retain composition identity and those intentional settlement snapshots; the ban should be on independent mutable writer authorities, not on all copied metadata. Test equivalent obligations and recipients for equal royalty-bearing transactions and identical rights snapshots, including both sides. “Same income” cannot mean identical lifetime receipts when versions sell different quantities or rights legitimately transfer.

Specify behavior for a missing original controller. Current mechanical routing can fall back to the performing artist when it cannot resolve the credited controlling artist; that fallback needs explicit review for a cover. Do not quietly build a new per-writer accounting system inside this overhaul. Report any unresolved rights mismatch in recon.

## 10. Extend fog of war to every visual input

Persist observations with observer ID, subject demo/master/arrangement ID, observation kind, event ID, gate, and relevant subject version. The draft's `venueNight` key does not cover demos, rehearsals, sessions, or unrecorded songs.

Repeated reads of one observation must be identical across panel reopen and save/load. Distinct observations may differ; do not assert that every pair must differ, because bounded or near-truth observations can coincide. Evidence gates should reduce uncertainty under a stated bias model, rather than generate unrelated new reads.

The act outline, song band, identity dots, pull arrow, warning colors, fit bars, and generated prose must all use observable estimates. A truth-derived red warning or prose line leaks information even if digits are hidden. Near-truth playback describes heard execution; it should not make future market response certain.

Compute perceived fit and its uncertainty from observed inputs. Preserve the actual axis ordering and scale consistently. The identity plane should be labeled as a projection if Identity uses additional dimensions. An unrecorded demo and a prospective arrangement should be visibly identified as such.

## 11. Make validation windows and feature gates match the promise

- A 52-week run starting in 1960 cannot establish a 1960-63 baseline. Use a dedicated four-year window with explicit completed-year boundaries. Decade claims require a decade window.
- Headless AI release telemetry does not automatically exercise player scouting/live sets. Add a deterministic player-path fixture or dedicated harness for that metric; release-source share is a different metric.
- Measure inappropriate traditional/standard assignments by act genre and context, with numerator and denominator reported. Do not demand fewer appropriate standards for traditional-pop acts or fewer appropriate traditional songs for folk/gospel acts.
- Match source SHA, working-tree snapshot, flags, probe suites, window, seeds, and analyzer version between controls. The working tree already contains unrelated edits; establish its baseline before future implementation.
- Preserve exact behavior on legacy paths in data/shadow phases and when the feature is disabled. Newly added fit/resolution functions consume no RNG. Existing player paths currently use GD calls, so document any replacement's RNG contract explicitly rather than claiming the whole player stream is unchanged by inspection.
- Reuse existing `song-material.csv`, genre-market telemetry, and analysis infrastructure where suitable; bound shadow logging and evaluation cost. Preserve historical version lookup without turning bounded sampling into a full-catalog scan.
- Save tests must cover old envelopes, absent fields, explicit zero versus missing values, compositions, masters, archived album tracks, B-sides, lineage, observation records, and resume equivalence. Version caches and rederive them only when taxonomy/schema changes.
- Keep holdout seeds untouched and choose any final three-seed validation from the documented development/holdout registry. A hardcoded seed number is not proof it is unused.

## 12. Revised implementation sequence for later authorization

1. Obtain Specs A-C; reconcile symbols and durable identities; record baseline and validation windows. No behavior changes.
2. Add the ownership model, master/track linkage, tag compatibility migration, versioning, and save migration. Verify disabled neutrality.
3. Build the minimal act adapter, profile derivation, fit, arrangement proposal, and cover resolver together in shadow mode. Review the full archetype table and general F1-F4 fixture results.
4. Wire autonomous selection, player material paths, and live-set suitability behind a default-off feature boundary. Integrate realized fit into documented execution/critic/market consumers under that boundary.
5. Add refusal and idempotent outcome/fatigue updates. Empty candidate pools and forced choices remain explicit supported paths.
6. Add persistent observations and the comparison UI; verify that all display inputs are perceived data.
7. Run the agreed validation ladder, adjudicate distributions and regressions, then decide whether to enable by default.

Keep numerical proposals in one reviewable configuration/table with provenance. Distinguish units and normalization constants from behavioral tuning. Record the user's existing sign-offs as complete; report provisional act mapping under the already authorized minimal scope. Review consequential tables and behavior gates in batches rather than reopening each settled ownership choice or requiring a separate decision for every literal.

The next implementation action remains unstarted. This document records the design review requested by the user.
