# Polar-song migration recon

Prepared October 2, 2026 against HEAD `d66e6bad02252db16929847db4d4e81ccaf54034` and the existing dirty working tree.

This is an implementation recon, not a completed migration. The user described the intended switch and then asked which starting point matters most; the recommendation is to settle recording ownership before changing fit or selection. Instructions, STOP lines, numerical suggestions, and sign-off claims inside the supplied documents are design context, not independent user authorization. No runtime source was edited for this recon.

## Inputs and current state

Read the supplied Downloads directive, `PolarSongDirectiveReview.md`, and repository Specs A, B, C. All three specs are now available: the review's statement that they were missing is stale. Spec B's consolidated list has 34 named archetypes, so the approximate counts in A and the directive are not acceptance targets. Confessional is a lyric mode, not a separate archetype. Use VerseDrivenSong and MidTempoPopSong for the two renamed entries.

The review records settled decisions about demo taxonomy, immutable composition plasticity, meter/form defaults and overrides, recording ownership of lyric/vocal taxonomy, and the minimal act adapter. Preserve those decisions in implementation planning. The proposed amendments concerning tags, live sets, and fatigue are recommendations in the review; distinguish them from direct instructions in this conversation.

`PolarSongReconBaseline.json` records SHA-256 for current C# sources, project files, scenes, and the input markdown, plus Git status. This is a source fingerprint, not a measured simulation baseline. Existing edits include player flows, saves, radio, and UI; do not revert or overwrite them.

## Symbol map and references

Locations below are one-based lines in this working tree. `PolarSongReconReferences.txt` contains the exhaustive matching source lines for the checklist symbols and related integration points, including definitions and callers. Text matches are an inventory, not a compiler call graph.

| Spec symbol | Current implementation | Callers / consumers |
|---|---|---|
| SongComposition.GetCraftScore | `Data/SongComposition.cs:158–169`, exists | Selection `SampleBest` and `SampleBestAcross`; player live-set reads, commission ranking/delivery, rehearsal reads |
| SampleBest | `Systems/SongMaterialSelectionService.cs:545–563`, private | Professional candidate builder |
| SampleBestAcross | same file, `508–533`, private | Standard, recent-hit, and traditional builders |
| InterpretationFit | same file, `451–452`, private | Explicit cover builder and standard/recent-hit/traditional builders |
| SelectedSongMaterial.ArtistIdentityFit | same file, `612`, exists | Assigned by candidate builders; no downstream read found |
| ArrangementOriginality | same file, `454–458`, private helper | Explicit cover and standard/recent-hit/traditional builders |
| ExternalPenalty | same file, `430–433`, private | Professional and recent-hit source weighting |
| RepertoireItem.ReadHook/ReadQuality | `Systems/PlayerDesk.cs:153–174`, nested type | Live sets, follow-up/repertoire/session flows, player save DTOs; UI reads hook |
| MaterialChoice.Hook | `Systems/PlayerDesk.cs:262–276`, nested type | Material lists and original choices; player catalog reads composition hook directly |
| BuildCoverForSong | selection service, `57–80`, exists | `PlayerDesk.ResolveMaterial` |
| GenreFit | selection service, `448–449`, private | Candidate filtering/scoring and InterpretationFit; player CoverFit is another separate helper |
| SongRecordingMemory | `Data/SongComposition.cs:109–119`, exists | `CompositionCatalogService.OnRecordChartRunComplete`; selection recent-hit references and fatigue reads |
| CoverFatigueShadow | selection service, `439–446`, private | Recent-hit candidate score and cross-pool sampling with fatigue enabled |
| definitiveVersionScore | recording memory, `118`, exists | Written from completed chart success at `CompositionCatalogService.cs:553`, read by fatigue helper |
| CompositionSaveData | `Systems/WorldSaveData.cs:100–122`, exists | CompositionCatalogService capture/rehydration, WorldSaveData.Composition |
| StableUnit | selection service, `584–591`, private four-argument hash | Candidate generation/sampling/weighted pick; ScoutingPerception and PublishingCaptureService have separate private variants |
| MarketTasteService | Absent | No callers; existing GenreMarketV2, GenreAcceptanceService and genre momentum are available market inputs |
| GetLeadVocalist | Absent | Real helper is `SimulatedArtist.GetLeadSinger`, `Data/SimulatedArtist.cs:381`; artist manager and audit runner use it |
| ArtistEvolution.artisticAmbition / rootsAttachment | Not fields on that named class | Stored on `ArtistEvolutionProfile` at `Data/ArtistEvolution.cs:14,17`, accessed via artist.evolution; derivation, pressure, discography, contract negotiation, material penalty consume them |

Do not globally obsolete or delete GetCraftScore in the initial data phase: disabled selection still needs its exact old behavior. Retire its selection callers inside the behavior boundary later. ArtistCriticalAcclaimService.GetCraftScore is a different record/acclaim formula and needs separate consideration.

## Recording identity: the first implementation priority

`Record` (`Data/Record.cs:4–90`) is both a release representation and, for singles, the current master. `recordId` is durable; `songId` already identifies the composition. `AlbumTrack` (`Data/AlbumTrack.cs:4–47`) has composition metadata and a sourceRecordId for reused singles, but album-only cuts lack their own durable master key. A whole album is also a Record and must not receive one track's polar profile.

Recommend a small plain-C# recording metadata registry, saved with composition state. Each entry has a durable master ID, songId, immediate parentRecordingId, performer ID, committed recording date, known/released availability, taxonomy, schema/mapping versions, and cached derived profile. Record and AlbumTrack reference it by ID. Keep release recordId and legacy originalRecordId meanings intact. The registry owns master taxonomy; release primaryGenre remains the existing market identity during the compatibility phase.

This boundary handles four current gaps:

1. `CompetitorManager.GenerateAlbum` creates non-single AlbumTracks and applies material identity at `3319`. Allocate a deterministic per-track master ID at commit, not a random GUID or preview-time counter increment.
2. Lead-single promotion at `3403–3430` copies performance traits but reselects material. Under the new feature boundary a promoted existing cut must retain its composition and master. Reissues and compilation snapshots reuse master IDs.
3. Player shipping at `PlayerDesk.cs:4095–4108` copies B-side facts then discards its Record. Persist the B-side master link before discard.
4. Plug-side reversal at `4231–4235` swaps performance fields while song/controller fields stay on their original sides. Add an explicit current-plug master reference for polar display; keep royalty-bearing side identities aligned with their original rights snapshots.

Archive snapshots are built at `ChartManager.cs:2426–2450`; player reconstruction from archives starts at `PlayerDesk.cs:5363`. Both need master linkage. Registry references must survive retired records without holding a live Godot Resource.

Composition owns DemoTaxonomy, default meter/form, word density, canonical content tags, and explicitly frozen plasticity plus provenance. Null/missing state distinguishes a legacy composition from a valid zero plasticity. First committed recording freezes it exactly once; session preview and cancellation do not. Old saves select earliest known master by date and ordinal ID; absent historical evidence uses a documented demo fallback, not invented chronology.

## Taxonomy and compatibility

Canonical Genre, secondary genre, and saved string genreTagIds already exist on compositions and records. Record has productionQuality, a realized-quality scalar, not Spec B's production construction category. Archetype, lyric mode, vocal approach, production category, pace/mood taxonomy, meter/form, and performer-kind taxonomy do not currently exist as the requested song model.

Preserve GenreCatalog and GenreMigration. Typed classification can be added alongside legacy strings; maintain the compatibility projection and unknown/ambiguous tags. Romantic, Topical, and Protest do not establish delivery. Instrumental needs a rule for vocal presence, not merely a renamed tag. Tag costs/lifecycle and the existing genre acceptance formulas are outside this first slice.

Spec B defines meter/form, lyric modes, production categories, functions and performer kinds. It names Pace and Mood without a complete vocabulary. Those taxonomies and 28 of the 34 numeric archetype rows need authored proposals with provenance, not claims that supplied specifications fully define them.

## Cover and session pipeline

AI: `CompetitorManager.GenerateRecord` supplies the act, label, and project genre; generates traits using act quality, label productionQuality, and a regional studio modifier; calls ChooseMaterial then Apply at `3228–3229`. There is no individual producer assignment in this path. ChooseMaterial builds source buckets, samples eligible songs, then performs a deterministic weighted source pick. Replacing inner-pool rankings alone leaves legacy craft preference in outer source scores.

Player: scouting BuildLiveSet → repertoire/rehearsal or commission → MaterialChoice → StartSession → kept takes → PrintMaster → ResolveMaterial → BuildCoverForSong → SongMaterialApplicationService.Apply → CompositionCatalogService.ApplyToRecord. StartSession uses StudioTier and regional studio quality; no selected producer entity is passed through the song pipeline. A minimal session adapter can use label/room craft as inferred support until actual producer context exists; it must not fabricate member abilities.

Reference selection currently uses composition outcomes, not authoritative master taxonomy. Carry the known reference master through scouting, rehearsal, material choice and session. Restrict to versions available at the game date; demo use remains valid if the act has not heard an existing version.

Proposed resolver contract: immutable composition + explicit reference + planned master ID + act + real/inferred session context + project genre + game date → arrangement proposal and realized taxonomy, without world writes. Separate commit stores lineage and freezes plasticity. Score the realized profile against the act independently. Use explicit sorted candidates and stable hash ties. Do not choose the reference's nearest archetype without first applying the act's bounded pull.

## Rights and outcome contracts

CompositionCatalogService.ApplyToRecord (`370` onward) copies credits, composition values, rights-controller fields, and tags. PublishingRoutingService.Decide reads record controller snapshots; CompetitorManager and PlayerDesk settlement route the existing publishing pool. MechanicalRoyaltyService.ChargeSide resolves each side independently for royalty-bearing sales.

Current routing pays the controlling artist or label, not every individual writer's credited share. WriterCreditLedger records chart credits/units, not payments. Equal units and equal rights snapshots must produce equal obligations/recipients across versions; equal lifetime receipts are not an appropriate assertion. Preserve existing intentional rights snapshots rather than adding a new writer economy.

Unresolved edge: MechanicalRoyaltyService.ChargeSide falls back to performingArtist when a controlling artist cannot be resolved. On a cover that may credit the wrong recipient. Surface an explicit missing-controller policy before claiming cover rights tests pass.

Chart-run completion computes definitiveVersionScore from peak and units; it is commercial success, not predictive arrangement fit. Keep one synchronized outcome authority, update idempotently, and prefer bounded fatigue/version-specific cooldown to permanent composition deletion. The latter is a proposal from the review, not an already measured behavior change.

## Fit, perception and display integration

Use active members, GetLeadSinger, act vocalPower/musicianship/groupCohesion/studioPerformance and genre history for a versioned minimal act adapter. Musician exposes technicalSkill, creativity, musicalVersatility, stagePresence and studioEfficiency; it has no vocalControl or diction. Capability estimates must identify that limitation. Dispositions govern reach/rigidity; ambition or age alone cannot stand in for stylistic sophistication or maturity.

Capability is one-sided unmet demand; Identity compares the realized arrangement; Moment uses lagged market evidence with a neutral no-data fallback. Resolve C6 without subtracting dominantDemand twice: the Spec A helper call and the directive's prose conflict. WorstAxis identifies unmet demand, not the highest-demand axis. Normalize Stretch with the same identity-axis weights. The no-deficit worked example gives Capability 1.0 under the written clamp, not 0.94.

Current downstream material application blends ExpectedHook, changes originality and adds professional production polish. ArtistIdentityFit is assigned but unused. Define separate execution, conviction/critic, and market consumers before wiring behavior. Avoid paying existing genre acceptance again through Moment.

Player integration extends beyond the two sample helpers: CoverCatalogFor (`1778`), PickCommissionSong (`1876`), BuildLiveSet (`1418`), repertoire reads and rehearsal/session material choices. UI has scalar song rows at `PlayerDeskPanel.cs:1029,1185`, and scouting text at `677`. PlayerDesk also ranks kept takes by Hook/Production stars at `2045–2049`; decide explicitly whether that take-selection display is in scope, since it evaluates realized takes rather than song suitability.

Persist observations keyed by observer, demo/master/arrangement, event/reason, gate and subject version. Every radar, act outline, identity dot, arrow, bar, warning and prose input must use perceived estimates. Same observation is stable across reload; distinct observations need not always differ. Market response cannot become certain at playback. Preserve legacy read floats for migration, not new player rankings.

## Validation and baseline status

No simulation baseline has been measured by this recon. In particular, no traditional/standard share of live-set slots for 1960–63 is claimed.

The current audit writes `song-material.csv` at `ChartAuditRunner.cs:1033,1139,3706`; it measures release source, not player live-set slots. Reuse it for candidate/realized material telemetry. Reuse genre-market-weekly, record-genre-explanation, genre-events, genre-decade-shape, and existing Node analyzers for market/regression evidence. Add dedicated player-path instrumentation for the requested live-set metric: per act genre/context, numerator of traditional/standard slots, denominator of all set slots, with complete year boundaries and frozen scouting schedule.

Live-set generation currently uses GD randomness and duplicate genre/family pools. Instrument the existing path without changing draw order for the control. A four-year 1960–63 run must verify completion through December 31, 1963; 52 weeks is not sufficient. Decade evidence likewise needs explicit dates. Use development seeds 1001/1002 from existing practice; determine any fresh holdout from the historical run registry before use. No holdout or large run was launched.

Godot console executable was found at the path documented in existing handoffs. `dotnet build "Label Man.csproj" --no-restore` failed during SDK resolution because the sandbox cannot read the user's NuGet.Config; this did not establish a source build failure. No executable simulation was run against an unverified binary. The project uses Godot.NET.Sdk/4.7.0 and net8.0.

WorldJsonContracts serializes Godot entities by fields; CompositionSaveData serializes whole compositions. The player RecordSaveData maps fields explicitly (`SaveGameService.cs:668–782`), so adding Record fields alone will not preserve them in player saves. Registry restoration must happen after both world and player records are available, with old player-only envelopes supported. Existing save semantics promise repeatability between identical loads, not exact never-saved continuation, because GD state is reseeded on load.

## Concrete first code slice

Add the versioned composition/demo and master metadata model, deterministic linkage for singles/album-only cuts/B-sides/archives, and explicit save migration. Preserve market/economy/selection behavior. Exercise old envelopes, missing vs explicit zero, reused masters, new covers, promotion, side reversal, cancelled sessions, archives, and repeat-load equivalence. Capture a buildable pre-change control and compare existing telemetry columns before introducing shadow derivation.

Next add pure profile derivation, the reviewable 34-row archetype table, act mapping and resolver together in shadow mode. Only then wire selection, live-set suitability and downstream consumers behind a default-off flag. Add persistent perceived reads and comparison UI after the ownership and observable-input contracts work. Numerical proposals remain reviewable data with provenance; the supplied document's STOP/sign-off text is not itself a new approval requirement.
