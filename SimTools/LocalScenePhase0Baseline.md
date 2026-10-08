# Local scene Phase 0: baseline and content contract

Prepared October 8, 2026. Implementation work is authorized by the user's request. The design documents' earlier design-only stopping instructions are historical context, not the current task boundary. This phase follows the revised ecosystem directive, not the earlier draft's purge-first sequence.

## Isolation and evidence

Work branch: `codex/local-scene-phase-0`, in its own managed worktree. Parent: `codex/local-scene-ecosystem` at `6ef395e1eefd6531b0ee18dd1a4353e4e90e4525`. Simulation base: `3203a66df703325b22f206d674d9da96cff04f50`.

At audit time, the committed `band-member-simulation` head still equals that simulation base. Its checkout has uncommitted edits to `Data/Musician.cs` and `SimTools/ChartAuditRunner.Resume.cs`, plus `SimTools/fit_solo_intent.py`. Those changes were neither imported nor modified. Recheck these seams before integrating either branch. The source difference against the committed band branch consists of the two scene design documents.

During the initial static preparation, no Godot instance, headless runner, build, editor import, or save load was started. The later authorized runtime gate and its results are recorded below. Static findings are verified against source; do not describe a source hash as an outcome hash.

## Reproducible static baseline

Run from this worktree with PowerShell:

```powershell
./SimTools/AuditLocalSceneBaseline.ps1
```

This reads tracked production C# sources and configuration, hashes their bytes, inventories flags and ownership/geography references, and cross-checks the 31 city IDs against the atlas. It writes `source-baseline.json` and `geography.csv` beneath `SimLogs/local-scene-phase-0/`. Generated artifacts remain ignored, following `SimTools/README.md`. It never launches the game. Its textual reference inventory includes declarations and comments; the owner audit below supplies the semantic classification. New factories or unusual syntax still require review.

The geography CSV is a registry census, not a live act census. The population values in `DistanceModel.AddCity` are unsourced simulation inputs used to seed store counts; they are not retained on `MarketCity` and cannot justify carrying capacity.

## Creation, removal, and contract owners

| Path | Actual baseline behavior | Scene integration requirement |
|---|---|---|
| `ArtistManager.GenerateInitialPool` / `GenerateInitialArtists` | Five type loops feed `GenerateArtist`; returned objects enter the unsigned list. | Place the existing cohort; preserve counts, genre decisions and RNG draw schedule. |
| `MaterializeEnabledInitialUnsignedReserve` | Generates the difference to the enabled target through isolated population RNG; new reserve becomes Latent and is removed from the searchable pool. | Place latent performers without making them contract eligible. |
| `MaterializeRuntimeFormation` | Responsive annual quota; consumes solo formation debt; adds dissolution refill credits; genre/type generation uses population RNG. | Assign each committed formation once; no scene refill clock. Place before geographic recombination eventually uses the base. |
| `GenerateArtist` | Registers an act and its generated people; argument four is a region display string; caller owns unsigned insertion. | Add a typed place seam; never substitute a city ID for the region string. |
| `PlayerDesk.GenerateLocalProspect` | Click-driven factory calls `GenerateArtist`, forces Seeking, and tracks the ID. It does not itself insert into `unsignedArtists`. | Phase 2 must replace this and purge together, using independently budgeted existing acts. Audit registration/search/signing accounting together. |
| `PurgeGeneratedProspects` / `RemoveUnsignedArtist` | Notebook IDs are spared. Removal refuses a nonempty label ID; otherwise only the artist registry entry is removed. Musicians and other references are not reconciled here. | Adopt survivors by ID; retire cleanup ownership. Never sweep pooled people by former act without checking current ownership. |
| `CreateSoloSpinOut` | Constructs an act without generation RNG, joins an existing person, inherits region, registers, adds unsigned, emits event. Band life charges formation debt. | A new act needs its own formation place and inherited working base; preserve person identity and ledger. |
| `BandLifeService.TryRecombine` | Reuses pooled people after a budgeted formation; replaced generated people are unregistered. | Reuse owner; geography is a later eligibility seam, not a second pool. |
| Replacement members / `RegisterMusician` | Person registration outside act generation; `UnregisterMusician` removes unused generated replacements. | Future person geography must cover this path too. |
| `ApplyTerminalExit` / `EndActForBandLife` | Terminal lifecycle/career state and unsigned removal; authoritative act identity remains in registry. Band life handles person consequences. | Keep tombstones, alumni, records and dossiers; scenes project terminal participation changes. |
| Search expiration / latent rotation / pool reconciliation | Mutates contract-search eligibility and searchable pool, not existence. | Performance participation must remain independent of Seeking/Latent. |
| `RestoreArtist` / `RehydrateWorld` | Replaces authoritative registry entries, rebuilds musician index, restores pool IDs and counters/RNG. | Restoration is not a birth. Relink scene IDs after both world and player restoration. |

Production searches for `new SimulatedArtist` found the generic generator and solo spin-out constructors in `ArtistManager`. Probe generation (`GenerateRuntimeArtistForProbeCore`) also calls the generator but is not an independent live factory. Runtime label founding recruits existing supply through the roster owner; it must not become a scene factory.

Commercial terms and ownership are separate calls: `AILabel.SignArtist` applies the deal/roster terms; `ArtistManager.SignArtist` reconciles label ownership, contract sequence and unsigned state. Cover every paired caller:

- `RosterManager.InitialSignArtist`: launch roster allocation.
- Daily talent-market winners, notebook rival resolutions, and legacy weekly recruitment in `RosterManager`.
- `PlayerDesk.ContractNegotiation`: player contract commit, then removal from generated cleanup tracking.
- `PlayerDesk.BandRoom` and `BandLifeService`: solo spin-out signing.

Drops also have enabled/legacy paths: `RosterManager` review/terminal reconciliation, `LabelLifecycleManager` closure, and player contract expiry feed `ArtistManager.DropArtist`, sometimes paired with `AILabel.DropArtist`. A dropped act can re-enter the market without relocating. Signing never deletes a scene resident.

## Discovery and knowledge seams

`ScoutVenue` spends time, purges, clears the slate, randomly chooses bill size, samples regional unsigned acts, filters them by consumer affinity and venue family, tops up via factory, and builds noisy live sets. None of these constitute persistent bookings. Phase 1 must leave these readers unchanged; Phase 2/3 must transition them coherently.

`EnsureRepresentedActOnFirstMeet` also mutates a selected existing act's manager/name from a visit. Move represented-act guarantees to independently generated/scheduled opportunities when persistent observation is enabled. Replacing only factory/purge would still leave visit-driven world mutation.

Notebook persistence restores acts by ID where possible, falls back to the serialized artist, and re-adds unsigned/dropped notebook IDs to `generatedProspectIds`. This restore-time reacquisition of purge ownership must be retired in Phase 2. `RestoreArtist` removes the restored act from unsigned supply, so fallback restoration needs an owner-mediated eligibility reconciliation, not direct list surgery. `Prospect.CityId` / notebook `CityId` prove an encounter location only.

`RosterManager.SetPlayerHold` / `ClearPlayerHold` / `ClearAllPlayerHolds` own temporary negotiation exclusion. Player notebook updates expire holds and restore them from dated save data. Keep these dates and checks; a notebook entry must not confer permanent immunity from rival signing or breakup.

Initial AI rosters use preferred genres, a whole-pool fallback, quality/risk scoring and random weighting without city distance. Enabled daily nominations use regional fresh/experienced pools, then `NationalFreshRecovery`. Legacy weekly selection and notebook rival paths also need the common access contract in Phase 4. A scoring-only geography patch leaves discovery and opening rosters wrong.

## Geography and save contract

The existing registry has 31 US playable cities in seven distribution regions. Aliases include Oakland to San Francisco, Hollywood/Pasadena to Los Angeles, Indianapolis to Cincinnati, Milwaukee to Chicago and Jackson to Memphis. They are distribution lookups; do not reuse them as hometown evidence. Six UK fallback names are London, Liverpool, Manchester, Birmingham, Glasgow and Bristol; they currently resolve label distribution to US hubs.

`GetRoadMilesBetween` returns zero when either city is unknown. Preserve economic distribution behavior in Phase 1, but the new place travel API must represent unknown/international routes explicitly rather than accept that zero as feasible travel.

Act `homeRegion` uses display strings (for example `Deep South`); label regions use IDs (`deepsouth`). The Phase 1 adapter must map both ways centrally. Economic readers retain their current values. `ArtistPublicProfile.homeCity` aliases `homeRegion`; `JournalisticDescriptor.DescribeArtist` explicitly appends “region.” Consumers include `ArtistDetailPanel`; do not switch only the scouting card.

Current save version is 4. World artists exclude player-owned acts. `WorldStateService.Apply` restores world artists, labels, lifecycle, records, economy, roster caches, composition and band life; `SaveGameService.Load` then calls `PlayerDesk.RestoreState`. Scene indices need a post-player success hook, with failed restores not committing partial migration. Check the next available version when implementing; do not assume v5 remains unused.

Phase 1 conceptual fields:

| Entity | Contract |
|---|---|
| Place | Stable ID; country; coordinates with source; optional US market mapping; optional playable catchment; aliases classified by purpose. |
| Act | Immutable formation place when known; mutable base place; dated move history; assignment provenance/confidence; compatibility market display preserved. |
| Musician | Personal origin when known; base and moves; stable existing person ID. |
| Label | True HQ place distinct from US distribution proxy; provenance. |
| Institution | Exact place/catchment; institution type; historical or fictional; supported effective date/range; community links. |

Assignment runs after existing generation without global/population RNG draws. It uses a separate keyed namespace, stable sorted place IDs, world seed, entity ID and assignment purpose/version. Legacy region-only saves get deterministic inferred bases spread within their region; unknown formation/origin remains unknown. Never derive origin from contract HQ or a scout's encounter. Saves reference existing act/person IDs; scenes never serialize duplicate artist ownership.

## Feature and control matrix

These are reserved design names; Phase 0 does not register runtime flags.

| Mode | Proposed flags | Allowed work | Required dependency |
|---|---|---|---|
| Scene off | No scene flags | Existing behavior; no scene RNG/state/ticks | None |
| Identity observation, Phase 1 | `--observe-local-scenes` | Place identity, assignment and diagnostics; no candidate/economic readers switched | None beyond existing generation; test enabled and legacy lifecycle |
| Persistent world, Phase 2/3 | `--enable-local-scenes` | Membership, world updates and later persistent bookings/knowledge | Enabled artist population lifecycle and genre market; observation implied |
| Geographic recruitment, Phase 4 | `--enable-scene-recruitment` | Initial/runtime reachable discovery, access provenance, common ownership checks | Persistent world |
| Dynamic feedback, Phase 5 | `--enable-scene-dynamics` | Bounded opportunity/movement/institution feedback | Persistent world; recruitment required for evaluated integrated mode |
| Explicit off | `--disable-local-scenes` | Freeze compatible persisted scene dynamics; never delete artists or rerun initialization | Reject combined observation/enable/dependent flags |

Reject contradictory flags and missing dependencies explicitly. Saved scene state must survive a disabled load. Do not change production defaults before the corresponding gate passes. Observation may write new geographic metadata and diagnostic output, but must not change source economic inputs, supply eligibility, candidate order, counters, schedules or RNG state.

`chart_manager.tscn` overrides genre market, population lifecycle, evolution, recognition, managers, canopy, cowriting, member axes and roster lineup churn to true. Distance is also enabled on its node. C# field defaults alone are not a control. World churn, team writing, musician growth, Polar member axes and member identity are CLI features at this base. Record the complete resolved switches, seed, executable version, revision and source hashes for every later run.

The concurrent branch's accepted run configuration/outcome artifacts were not supplied or imported. Establish that configuration with the user before a new runtime comparator; do not invent accepted thresholds from comments or old reports.

## First-slice authorship contract

All 31 IDs already have atlas entries; basic differentiated content is due by Phase 3. Phase 1 ships identity/institution types, not numerical scene weights. Rich first slices can start with New Orleans, Miami and Nashville to exercise multiple communities within one existing commercial region; this is a coverage strategy, not a historical ranking.

Each authored claim must carry claim ID, exact fields affected, source URL/title and locator, consultation date, applicable years/date precision, geographic boundary, confidence, and evidence versus design status. Population also requires source year and boundary. Unsupported quantities remain unset; simulation coefficients live in separately labeled parameters. Historical and fictional institutions must be explicit. Profiles support overlapping subscenes and distinguish activity, opportunity, audience, export visibility and transient heat. Existing canonical genre chronology remains authoritative; style descriptors can represent earlier roots.

Sources rechecked October 8, 2026:

| Claim ID / place | Evidence verified | Allowed fields and unresolved work |
|---|---|---|
| `no-traditional-jazz-continuity` / New Orleans | Preservation Hall's own history describes gallery sessions, community musicians, and a 1963 touring band; dated images include 1964, 1965 and 1967. [Institution history](https://www.preservationhall.com/about/). | Supports continuing traditional-jazz institutions and separate visitor/tour participation. Does not establish a 1960s population count, genre share, capacity, fees, or exact opening day; source those before named scheduling. Current performer counts are not period counts. |
| `miami-youth-rnb` / Miami | HistoryMiami's Teen Miami exhibition identifies local garage bands and R&B performers in the 1960s. [Museum exhibition](https://historymiami.org/exhibition/teen-miami/). | Supports multiple youth/R&B opportunity paths, not percentages or venue calendars. Cuban, hotel, radio and distributor paths retain the atlas's separate research backlog. |
| `nashville-rnb` / Nashville | The Country Music Hall of Fame describes its R&B exhibition as covering 1945–1970 and Nashville's R&B scene. [Museum exhibition](https://www.countrymusichalloffame.org/night-train-to-nashville). | Supports an R&B community alongside country; exact rooms, dates, audience/access and cross-scene sessions require the underlying exhibit and period evidence. |

The atlas remains provisional for all unsupported quantitative fields. Before Phase 3 content acceptance, source several hearing opportunities, recording/distribution pathways, recurring-cast constraints, and visit paths for each city; use marked fictional supporting institutions where appropriate. No city ability bonuses or pre-scripted chart winners.

## Coordinated runtime gate and next phase

Phase 0 static preparation is complete once the audit passes and production hashes match the parent. At the initial stopping point, runtime baseline acceptance was pending a user-coordinated test window; the later authorization and completed results are recorded below. The user explicitly requires stopping before any in-game test; this includes headless Godot probes, startup census and save/replay runs.

The next executable gate needs a runtime act/person census by region, cohort, seeking status and ownership; exact resolved flags and seeds; replay/economic output hashes; and accepted control/noise registration. Census counts must reconcile against the shared registries, not only the unsigned shelf. No fixed population/finance bounds are claimed here.

After that gate, Phase 1 implements the place registry, compatibility adapter, act/person/HQ identity and deterministic assignment/migration, plus post-restore relinking and fixed probes. Its acceptance requires economic control preservation with scene-off and identity observation under matched flags. Phase 2 then replaces slate factories/cleanup and adopts surviving prospects. Do not remove purge in Phase 1 or recalibrate formation to compensate for local shortages.

## Runtime gate registration (authorized October 8, 2026)

The user authorized continuing with tests. No other Godot process was active at preflight. The isolated engine import completed resource caching but returned native teardown code `0xC0000374`; record it as an import failure, not a simulation pass. The separately built runtime assembly passed compilation with five existing warnings. Only subsequently completed runtime runs count as evidence.

Baseline family: `scene0-control-v1`. Three 52-week runs (seeds 1001, 1001 repeat, 2002), then two eight-week reloads of the same seed-1001 world. Every job runs serially and refuses an active engine before launch. Artifacts and the early world files stay in this worktree's ignored `SimLogs`, with no player save slots used. Early snapshots are chart week 1 (January 8), not the untouched launch state. There is no scene treatment yet.

Fixed gates: zero duplicate/dangling act IDs, roster ownership mismatches, unsigned ownership conflicts, duplicate active person assignments or pooled/assigned person conflicts in the captured world; byte-identical repeated CSVs except wall-time performance telemetry; identical canonical repeated world snapshots; successful engine exit, completion marker and exact requested week counts. Reload compares two resumes of the same world, because global RNG is reseeded on load. It does not compare a resume to an uninterrupted run.

Resolved baseline intent: genre market, artist population lifecycle, evolution, artist recognition, managers, canopy, cowriting and member axes enabled; band-life observation and world churn enabled; member fame share 0.45; team writing, growth, growth shadow, Polar member axes and member identity off. Other commercial/repertoire defaults remain exactly those of the hashed source/config. Seeds are 1001 and 2002. Flags, executable/assembly hashes and actual exit/week results are in the run manifest. This corresponds to the documented refill-world configuration plus explicit scene defaults, not a newer concurrent-branch candidate.

The v1 invocation used the unrecognized `--enable-seed-star-canopy` spelling. Canopy nevertheless ran through the hashed `chart_manager.tscn` true default, confirmed by its startup diagnostic (6 Superstars / 24 Stars). Every v1 comparator uses the same effective configuration and argument spelling. The helper now uses the recognized `--seed-star-canopy` for future families; do not quietly rewrite saved v1 flags or rename v1 as an explicit-flag run.

Census coverage is authoritative world acts, roster IDs, unsigned IDs, their saved people and the band-life pool. The private musician registry is not fully serialized, so this does not prove absence of orphan musician entries created by old purge behavior. Phase 2 requires a direct registry/cache reconciliation probe. No player label is founded in these controls; player notebook/generated-prospect adoption and two-stage restore remain Phase 1/2 integration scenarios.

For subsequent observe-only identity changes, same-seed economic drift tolerance is zero against the matched source control. For recruitment/dynamics, these two early-year seeds are descriptive controls, not a decade noise floor or permission to reuse old scalar thresholds. Shared economic changes still owe the registered two-seed decade A/B under the integrated band's accepted flags.

## Concurrent branch drift during the runtime window

The band checkout advanced after the initial audit and is now clean. Its head at this recheck is `d1cc91b`, after `38aeb7d` (solo intent rework and yearly saved worlds), `8bdb8ca` (member fame default 0.45 to 0.90 with saved-slice metadata), and `d1cc91b` (world churn enabled by default). Nine files differ from the frozen simulation base, including musician intent, band-life service, chart configuration, member recognition, and the resume harness.

These commits were not imported into this worktree while controls were running. The measured baseline remains `3203a66` production code, explicitly world churn and fame 0.45. It does not validate the new solo-intent implementation or fame-0.90 default. Before Phase 1 integration, align to the then-accepted committed band state and establish controls under that state; do not silently compare its outcomes to this frozen older control.

The latest band save harness records the actual member-fame slice independently of CLI flags and supports annual world snapshots. Preserve that metadata through future scene save/version work. A region/place migration must not rescale fame, change solo intent, erase formation debt/credit, or bypass new world-churn defaults. The upcoming data additions need to extend the current musician representation, not restore an older copy of it.


## Completed Phase 0 runtime results

All five jobs exited 0, wrote completion markers, and produced exactly 52 / 52 / 52 / 8 / 8 measured weeks. No engine process remained after the series. The existing `MissingSingletonsTemp` autoload diagnostic occurred, but it did not prevent these completed runs. No production simulation source or configuration changed relative to the scene parent.

| Gate | Result |
|---|---|
| Seed 1001 fresh-run repeat | All 84 CSV files byte-identical; no mismatches. |
| Seed 1001 saved-world repeat | All 84 CSV files byte-identical across two independent eight-week resumes. |
| Canonical early world, 1001 repeat | Identical SHA-256. |
| Snapshot ownership/person checks | Zero violations in all three captured worlds. |
| Weekly roster/search integrity | All nine monitored maximum conflict/duplicate/terminal counts zero on both seeds. |
| Runtime/build provenance | Engine and assembly SHA-256, arguments, revision, exit codes and requested/actual weeks recorded in `runs.json`. |
| Static geography/source gate | 31 unique playable IDs match the atlas; production sources remain unchanged. |

| Census / control measure | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Chart-week-1 active acts | 7,042 | 7,042 |
| Chart-week-1 owned acts | 2,968 | 2,924 |
| Chart-week-1 searchable unsigned shelf | 74 | 118 |
| Chart-week-1 unique saved people | 21,299 | 21,271 |
| Chart-week-1 labels | 600 | 600 |
| HQ-match / international / unmapped projections | 586 / 4 / 10 | 588 / 4 / 8 |
| Week-52 registry acts | 9,200 | 9,200 |
| Runtime formations in 1960 | 2,200 | 2,200 |
| Week-52 active rostered acts | 2,935 | 2,834 |
| Week-52 experienced free agents | 213 | 261 |
| Week-52 seeking prospects | 6,052 | 6,105 |
| Week-52 affordable hiring vacancies | 0 | 1 |
| 1960 single units | 156,371,074 | 157,516,319 |
| 1960 album units | 40,675,959 | 36,986,994 |

These counts describe the frozen base, not a historical census. Regions and source/proxy HQ assignments are enumerated honestly; acts currently have no canonical formation/base place fields. Full city assignment begins in Phase 1. The early census includes one formation tick, and the yearly economy includes 52 measured weeks after the normal prewarm.

Artifacts: `SimLogs/scene0-control-v1/runs.json`, `analysis.json`, `census.csv`, `label-geography.csv`, per-run console/error logs, the 84 CSV families for each comparator, and `SimLogs/worlds/scene0-control-v1-w0-*.world.json.gz`. Static hashes and city registry CSV are in `SimLogs/local-scene-phase-0/`. All generated artifacts remain ignored.

Reusable tools:

```powershell
./SimTools/AuditLocalSceneBaseline.ps1
# A new runtime family requires an authorized test window and a successful fresh build:
./SimTools/RunLocalSceneBaseline.ps1 -RunTag scene0-control-v2
py -3 SimTools/analyze_local_scene_baseline.py scene0-control-v2
```

Phase 0 is complete for the frozen scene branch. The next phase is identity observation. It must first align with the now-committed band changes and register a control from that integrated revision, then add canonical places, region adapters, geography provenance and migration/relinking. This phase supplies a proven baseline procedure and ownership/content contract; it does not waive that integration comparison or the later two-seed decade gates.
