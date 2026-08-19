# Publishing & Cover-Song Directive

Branch: `publishing-mechanic` (a fresh branch; the scouting-mechanic work is closed). The publishing
settlement plumbing this extends is already in this branch's baseline — it is **not** on `main`
(HEAD is 31 commits ahead of `main`), so treat that Phase-4 code as inherited baseline, not something
to re-add. Source: a design draft (`publishingdirective.odt`, 2026-08-18) proposing a
composition/publishing layer. This file is the consolidated, code-verified plan. The draft is taken as
**suggestion**; every code reference below was checked against the tree and corrections are called out
inline.

## The goal (design intent)

Model the early-60s industry as material-driven: covers, standards, Brill-office songs, and staff
writers dominate 1960; self-penned material rises through the decade (rock/folk/psych/singer-
songwriter), with acts occasionally *fighting* to write their own hits (the Monkees). The transition
is **not uniform** — soul, teen pop, bubblegum, girl-group keep using manufactured material late.
Holiday standards persist as an evergreen catalog. And beneath it all, model the **publishing
goldmine**: who wrote a song and who controls its publishing drives label economics as much as who
records it.

The clean architectural rule (adopt the draft's framing): **a `Record` is a performance/master; a
`SongComposition` is the underlying song.** Covers, standards, staff songs, and originals are all just
different ways of attaching a Record to a SongComposition. The chart stays record-facing; every record
gains a song biography underneath.

## The reframe — what already exists (do NOT rebuild)

This branch already shipped a **binary publishing economy** in Scouting Phase 4. The new work
*replaces the binary axis with the composition model*, it does not add a second parallel one.

- `SimulatedArtist.labelOwnsPublishing` ([SimulatedArtist.cs:138](Data/SimulatedArtist.cs:138)) — the
  goldmine bit, default label-favorable, flipped by a Visionary manager deal.
- `CompetitorManager.PublishingShareOfGross = 0.11f`
  ([CompetitorManager.cs:13](Systems/CompetitorManager.cs:13)) — the composition slice, a calibration
  guess.
- Settlement already routes it as a **REALLOCATION, not a new stream**
  ([CompetitorManager.cs:966-983](Systems/CompetitorManager.cs:966)): label-owns → the 0.11 is
  informational inside `LabelNet`; artist-owns → the slice comes off `LabelNet`/`recordRevenue` and
  accrues to the artist as non-recoupable royalty. Two settlement columns already exist
  (`PublishingIncome`, `ArtistOwnsPublishing`) on the settlement struct
  ([ChartManager.cs:224-227](Systems/ChartManager.cs:224)) and in the CSV header
  ([ChartAuditRunner.cs:1126](SimTools/ChartAuditRunner.cs:1126)).

**This is the load-bearing correction to the draft.** The draft's §12 proposes a *fresh*
`PublishingSettlementService` on a `ChartManager.CompletedWeekSettlementEntry`, splitting a pool into
writer/publisher halves and adding `entry.LabelPublishingIncome += pool`. Wiring that verbatim would
(a) double-book against the settlement that already runs in `CompetitorManager`, and (b) reintroduce
the exact "add 11%-of-gross to LabelNet" bug the Scouting branch explicitly rejected because it blows
up the calibrated economy. **The composition model must feed the *existing* reallocation** — it
enriches *who the counterparty is* (external publisher vs label affiliate vs artist vs public domain),
not *how much* leaves LabelNet, and never turns reallocation back into addition.

Also already present and reusable:
- `songwritingAbility` is a real, derived field
  ([SimulatedArtist.cs:30](Data/SimulatedArtist.cs:30)), computed from writer-members' creativity
  ([SimulatedArtist.cs:161-163](Data/SimulatedArtist.cs:161)). §7's artist-written path can read it
  directly; do not invent a new one.
- Writer identity exists on `Musician`: `isPrimaryWriter`, `creativity`, `musicalVersatility`,
  `personId`, `creativeReputation` ([Musician.cs:17-45](Data/Musician.cs:17)); `GetMainWriter()`
  ([SimulatedArtist.cs:382](Data/SimulatedArtist.cs:382)). The Lennon/McCartney/Dylan identity surface
  is already half-built.
- `ArtistEvolution.artisticAmbition` / `rootsAttachment`
  ([ArtistEvolution.cs:14-17](Data/ArtistEvolution.cs:14)) exist and drive the "ambitious act resents
  external material" penalty the draft leans on throughout.

## Code-verified findings (checked against the tree)

- **Single injection funnel.** Every AI release is built by
  `GenerateRecordFromArtist(label, artist, year, format)`
  ([CompetitorManager.cs:2972](Systems/CompetitorManager.cs:2972)), called from the singles path
  (:669, :1634) and album path (:1828). Material selection hooks in **here**, in CompetitorManager —
  the draft is right that ChartManager should receive an already-built Record. There is no need to
  touch multiple release sites.
- **Promotion site is real.** `PromoteRecordAI`
  ([ChartManager.cs:747](Systems/ChartManager.cs:747)) computes launch `data.awareness` and
  `data.radioPlay` exactly as the draft's §11 sketches ([ChartManager.cs:785-787](Systems/ChartManager.cs:785)).
  The familiarity-lift insertion is straightforward — but note both lines already draw `GD.RandRange`,
  so any lift must be **added after** the existing draw, never replace it (preserves RNG order).
- **Chart-run completion hook is real** but the draft mis-names it. It is
  `RosterManager.RunCulturalReads(artist, record, label, year)`
  ([RosterManager.cs:1231,1240](Systems/RosterManager.cs:1231)), guarded by
  `RecordRuntimeData.culturalRunCompleted` ([RecordRuntimeData.cs:35](Data/RecordRuntimeData.cs:35)),
  and it already fans out to `ArtistCriticalAcclaimService`, `ArtistEvolutionService`,
  `CulturalMemoryService`, etc. `CompositionCatalogService.OnRecordChartRunComplete` slots in here.
- **Snapshot path is real.** `ChartManager.CreateTrackSnapshot(RecordRuntimeData)`
  ([ChartManager.cs:2195](Systems/ChartManager.cs:2195)) builds `AlbumTrack`
  ([AlbumTrack.cs](Data/AlbumTrack.cs)) from retiring singles. The mirrored song fields must be copied
  here or a retired single loses its song identity when it lands on a later compilation/album.
- **Record already carries the performance axes** the model layers onto: `hookStrength`,
  `productionQuality`, `originality`, `danceability`, `primaryGenre`
  ([Record.cs:29-32](Data/Record.cs:29)). The model adds a *composition* axis beside these; the
  existing generated `hookStrength` becomes the recording/performance variance the draft's §10 blends
  against composition hook.
- **Genre names to reconcile.** The draft's switch tables use `Genre.Motown` (=9, legacy, migrating to
  Soul+tag), `Genre.Psychedelic`/`PsychedelicRock` (both exist, =21/=34), `Genre.SingerSongwriter`
  (=47), `Genre.Bubblegum` (=29), `Genre.SunshinePop` (=24). Audit each `switch` against
  [Genre.cs](Data/Genre.cs) before pasting; several draft arms name legacy values.
- **`criticalAcclaim` is still suspect.** The draft's §14 writes `artist.criticalAcclaim` and
  `member.recognition.ApplyCriticalCredit`. Memory [[criticalacclaim-is-a-dead-field]] flagged the
  field as dead; Scouting Phase 5 deferred the Visionary prestige wire for the same reason. Do **not**
  build person-level writer fame on `criticalAcclaim` until it is confirmed live. Route writer prestige
  through the recognition stock that Recognition Phase A built instead ([[recognition-phase-a-implemented]]).
- **Person-level fame has a weak trigger surface.** [[lineup-churn-never-fires]] — no departures /
  solo-splits / reunions fire at runtime, so a credited writer-member's fame currently has nowhere to
  *go* (no solo career spins out of it). §14/Phase 5 is real but its payoff is capped until lineup
  churn exists; scope it as credit-ledger telemetry first, not a fame engine.

## The model (adopt from the draft, condensed)

New types (draft §1, §3, §5 — sound; author the classes largely as written):

- **`SongComposition`** — the song: `songId`, title, primary/secondary genre, `originYear`,
  `originKind`, craft fields (compositionQuality, melodicStrength, lyricQuality, commercialHook,
  adaptability, originality), `standardDurability`, familiarity fields (national/adult/teen +
  regional bias), flags (`isTraditional`, `isPublicDomain`, `isStandard`, `isCoverable`), a
  `SongRightsProfile`, `List<SongwriterCredit>`, and `List<SongRecordingMemory>`. `GetCraftScore()`
  and age-decayed `GetFamiliarityForYear(year)` as sketched (standards decay ~0.992/yr, fads ~0.955/yr).
- **Source taxonomy** — `enum SongMaterialSource` {ArtistWritten, ArtistCowrittenWithProfessional,
  LabelStaffWriter, ExternalProfessional, CoverRecentHit, CoverCatalogSong, CoverStandard,
  TraditionalPublicDomain, AdaptedTraditional}, `enum WriterEntityType`, `enum PublishingControlType`
  {PublicDomain, ExternalPublisher, LabelAffiliate, ArtistControlled, LabelBuyout, SharedControl},
  `enum SongOriginKind`, `enum PublishingScene` (LegacyTinPanAlley, NewYorkPopFactory [Brill
  abstraction], Nashville, DetroitInHouse, MemphisSoul, LosAngelesPop, IndependentFolk, ChurchGospel,
  LabelAffiliate).
- **`ProfessionalSongwriter`** and **`MusicPublisher`** (staff/office writers who are not artists;
  publishers with an optional `affiliateLabelId`, a `PublishingScene`, focus genres, plugger skill,
  buyout willingness, and catalog lists).

Record-side additions (draft §2) — `songId`, `songSource`, `isCover`, `originalRecordId`/
`originalArtistId`, `publisherId`, `publishingControllerLabelId`, `publishingControl`, songwriter
id/name/type/share arrays, and composition snapshot floats (`compositionQuality`, `compositionHook`,
`lyricQuality`, `songFamiliarityAtRelease`, `standardDurability`, `arrangementOriginality`,
`professionalPolish`). Mirror the important ones onto `AlbumTrack` and copy them in
`CreateTrackSnapshot`.

**Holiday / evergreen standards** (called out by the user): seed a holiday family in the pre-game
catalog with very high `standardDurability` and a slow-decay familiarity curve, and **tag each song
with the existing `GenreTag.Christmas` / `GenreTag.Halloween` / `GenreTag.Seasonal`**
([Genre.cs:86](Data/Genre.cs:86)) carried on `Record.genreTagIds`
([Record.cs:28](Data/Record.cs:28)). The seasonality system is already built to boost these tags —
holiday is therefore **not** a stretch goal: tag the family correctly at authoring time and the demand
bump rides the existing seasonal mechanism. Records that cover a holiday song must inherit the song's
seasonal tag(s) onto `genreTagIds` in the application step.

## Phased plan (ordered low → high determinism/calibration risk)

Mirrors the draft's §18 but re-gated against *this* branch's economy.

### Phase 0 — Catalog + attachment, data-only (draft Phase 1) — IMPLEMENTED
`Data/SongComposition.cs` (model types + enums + `ProfessionalSongwriter`/`MusicPublisher`),
`Systems/CompositionCatalogService.cs` (static service, own seed-salted RNG stream
`seed ^ 0x736f6e6763617461`). Initialized in `CompetitorManager.Initialize` **before**
`PopulateInitialRecords` ([CompetitorManager.cs:420](Systems/CompetitorManager.cs:420)); a decade seed
mints **3840 songs (3570 standards incl. a 120-song holiday family tagged `christmas`/`seasonal`,
270 professional catalog), 120 staff writers, 5 publishers**. Composition/publishing fields added to
`Record` (`songwriterTypes` kept non-`[Export]` — Godot GD0102 rejects a custom-enum array) and
mirrored on `AlbumTrack` + copied through `CreateTrackSnapshot`. `AttachArtistOriginal` is called at
the single funnel `GenerateRecordFromArtist` ([CompetitorManager.cs:2972](Systems/CompetitorManager.cs:2972)),
minting an artist-original stub from the record's already-computed fields (**zero RNG**) and stamping
song identity + credits + `PublishingControlType` (LabelAffiliate when `labelOwnsPublishing`, else
ArtistControlled). **Gate MET:** 52-week A/B (seed 1001, canonical flags) — **76/76 economy CSVs
byte-identical** (settlement, records, label-finance, market-revenue, …). Telemetry CSV (draft §17)
deliberately deferred so this checkpoint proves pure inertness; it is the first task of Phase 1.

Original plan text follows.

Author the model types. Stand up `CompositionCatalogService`, initialized in
`CompetitorManager.Initialize` **before** `PopulateInitialRecords`
([CompetitorManager.cs:398,421](Systems/CompetitorManager.cs:398)) with its **own** RNG stream
(`seed ^ 0x736f6e6763617461` — a separate stream, so it cannot perturb the global `GD` schedule).
Generate the pre-1960 standards families (Tin Pan Alley, jazz, country songbook, blues/R&B, gospel,
folk-traditional, holiday evergreen) plus a professional-writer/publisher pool. Attach a `SongComposition`
to every `Record` built in `GenerateRecordFromArtist` (initially: fabricate an "artist original" stub
so every record has a `songId`) and copy song fields through `CreateTrackSnapshot`. Wire release +
outcome telemetry (draft §17). **Do not** alter hook, awareness, radio, originality, or economics.
- **Gate:** economy byte-identical except the new CSV columns. Verify with the byte-comparison probe
  discipline ([[probe-run-byte-comparison-proves-inertness]]). Catalog RNG on its own stream ⇒ launch
  roster + population schedule unchanged.

### Phase 1 — Material selection (draft §6-9) — IN PROGRESS
Built: `Systems/SongMaterialSelectionService.cs` (pure stable-hash, never GD — FNV-1a over
artist|record|year|salt, mirroring ScoutingPerception), `Systems/SongMaterialApplicationService.cs`
(blends composition hook into the record's performance hook by source-authority; lowers originality
for covers), catalog query + `CreateArtistOriginal` helpers, and a `song-material.csv` telemetry
writer in ChartAuditRunner (one row per single at first observation, joined to `records.csv` by
recordId). Selection replaces the Phase-0 unconditional stub at `GenerateRecordFromArtist`, behind
`SongMaterialSelectionService.Enabled`.

**Two structural findings from the first 104-week run (seed 1001):**
1. **Pool-pollution bug (FIXED).** `CreateArtistOriginal` registered every artist-written song into
   `catalogByGenre`, so the selectable pool filled with originals over the run; `SampleBest`'s bounded
   6-draw then almost never hit a professional/cover song → fell back to ArtistWritten, and it got
   *worse over time* (would corrupt the decade curve). Fix: curated pools kept SEPARATE from
   artist-originals (`professionalByGenre`/`traditionalByGenre`/`coverableHitsByGenre`; standards
   already separate). Originals are song-only (reachable by id, not selectable) until they chart
   (Phase 4 calls `RegisterCoverableHit`). This is a [[market-share-is-supply-bound]] instance: the
   gate only bites where curated supply exists.
2. **Cross-genre cover supply gap (FIXED).** Pools are keyed strictly on PRIMARY genre, and
   the seed catalog has no RockAndRoll/DooWop/etc. standards or professional songs — so those genres
   are forced to ArtistWritten by starvation (first run: RockAndRoll 96.8% self-written in 1960). But
   early rock was substantially **covers of R&B/blues material** (Pat Boone over Little Richard) plus a
   self-writing wing (Berry, Holly, Little Richard co-writes) and a manufactured wing (Elvis — who
   didn't write; his co-credits were publishing cut-ins — Fabian, Avalon). So rock's artist-written
   share should sit *above* teen-pop/trad-pop and *below* folk (the era-weight ×1.05 already does
   this), but the covers it *doesn't* self-write must come from adjacent families. Fix: make
   cover/standard/traditional selection **genre-adjacency-aware** using `GenreCatalog.Get(genre).Family`
   ([Genre.cs:84](Data/Genre.cs:84) `GenreFamily` {Pop, Rock, RhythmAndSoul, Blues, …}) — e.g. a Rock
   act draws covers from {Rock, Blues, RhythmAndSoul}. Author a small cover-source adjacency map.
   DONE: family-keyed pools (`standardsByFamily`/`traditionalByFamily`/`coverableHitsByFamily`) +
   `CoverSourceFamilies(genre)` adjacency map; cover/standard/traditional/recent-hit builders sample
   across adjacent families. After the fix rock reads 13% self / 86% cover in 1960 (was 97% self).

**Calibration (owner targets, 2026-08-18).** 1960 genre-averaged mix should be: self-written **~22%**
(small bands, jazz players like Miles), staff/professional **~30%** (with **TeenPop skewing majority
staff** — more manufactured than R&B), standards **~20%**, contemporary/recent-hit covers **~15–20%**,
public-domain/traditional **~10%** (classical/folk/holiday/children's). To hit these directly the
selection was re-architected around an explicit **source-mix prior**: `Anchor1960(genre)` /
`Anchor1969(genre)` give the target share of each source, interpolated by `SmoothYear(1962,1968)`;
a candidate's score is `mixWeight × (0.55 + 0.45·quality)`, so the realized mix tracks the prior
(modulated by catalog availability — an empty pool redistributes its share). This separates *how much
of each source* (calibratable) from *which specific song* (quality/fit pick). A **pre-game recent-hits
pool** (1955–59, `SongOriginKind.RecentHit`, registered coverable) was added so `CoverRecentHit` is
non-empty from week one (it was 0% — there were no hits to cover yet). Iterating the anchors on
104-week runs until 1960 lands near target; the anchors ARE the decade curve.

**Phase 2 bundled into the pre-decade work (owner decision).** Phase 1's real gate is the decade
transition, so Phase 2 (the launch-familiarity lift) is folded in before the single decade run — one
run validates both material mix and familiarity effect. Phase 2 stays behind
`SongLaunchService.LaunchInputEnabled` so it can still be A/B'd cheaply on a short run without
re-running the decade.

**Promo-single coverage hole (FIXED).** The only runtime single path that bypassed the funnel was
`CreatePromoSingleFromAlbum` (a promo single lifted from an album cut) — it built a `new Record` with
no material selection, so **38% of chart singles carried no `songId`** (invisible in telemetry, which
skips song-less rows). Threaded `artist`/`label`/`year` into it and ran the same selection (pure
stable-hash, no GD draw → album-project RNG stream unchanged). Coverage 62% → **100%** (0 of 9655
singles missing on a 104-week run), mix unchanged. Still open (draft §15, deferred): individual
**AlbumTrack**s carry no per-track song, so a retired album cut has no composition origin.

**Validated calibration (seed 1001, 104wk, p1l).** 1960 realized mix vs owner targets: self **20.6%**
(t22), staff **26.3%** (t30), standard **22.7%** (t20), recent-hit **16.2%** (t18), trad **14.2%**
(t10) — all within ~5pp. Runtime releases obey the prior: launch (wk≤4) vs runtime (wk≥5) buckets are
near-identical. TeenPop is majority-staff.

**DECADE GATE — PASSED (seed 1001, 522wk, full telemetry, run `p2-decade`, 2026-08-18).** The one long
run that gates Phases 1+2. Singles source mix by year (self / staff / standard / recent-hit / trad, %):
1960 `20.6/26.3/22.7/16.2/14.2` → 1969 `52.8/15.9/12.5/11.8/7.0`. Self-penned material rises
monotonically to a healthy majority; Brill/staff, standards, and traditional all decline — the intended
transition. **Genre non-uniformity holds**: rock/folk/psych/singer-songwriter reach self-written
majorities by 1969 (RockAndRoll 32.6%→67.2%, Folk 34.7%→55.6%, FolkRock 68.2%, GarageRock 82.8%,
PsychedelicRock 83.3%, SingerSongwriter 73.6%), while soul/teen/bubblegum/trad-pop stay manufactured
late (TeenPop 7.6%→31.5%, Soul 19.1%→40.0%, TraditionalPop 12.5%→17.5%, Bubblegum 19.4%). Economy stays
healthy (singleNet flat ~7.4–8.1e7/yr, album market grows into the LP era, no mass insolvency); **no
Established genre is starved of releases** in 1969. Genre balance is historically plausible (1960
TeenPop/TradPop/RnB/RockAndRoll → 1965 British-Invasion rise → 1969 Soul-dominant).

**Per-track album song identity — IMPLEMENTED (draft §15, the second owed item).** Fresh non-single
album cuts (`GenerateAlbum`'s `nonSingleTracks`) now run the same pure-stable-hash material selection
as singles, keyed on a per-track synthetic id (`{albumRecordId}_t{i}`), stamping song identity onto each
`AlbumTrack` via `SongMaterialApplicationService.ApplyIdentityToAlbumTrack`. **Identity only** — it does
NOT blend the track's quality/hook/production/danceability, so album `pooledAppeal` and promo-single
selection stay byte-identical (no GD draws; RNG schedule untouched). The promo trackRef mirrors its
lifted single's identity. New `album-composition.csv` columns (`originalTracksWithSong`,
`originalSelfWrittenShare`, `originalStaffShare`, `originalCoverShare`) make it observable. **Verified:
0 songId mismatches across 40,604 album rows** on the decade run — every album cut carries a composition
origin. Album filler follows the singles transition: selfShare 0.195 (1960) → 0.526 (1969), coverShare
0.535 → 0.314. The full `AlbumMaterialPlan` (whole-LP cohesion/lead-single-source correlation) remains
the genuinely deferred, larger piece.

Original plan text follows.

Add `SongMaterialSelectionService.ChooseMaterial(...)` and its candidate builders (artist-written,
professional, standard-cover, recent-hit-cover, traditional), scored by the era/genre weight functions
(`GetArtistWrittenEraWeight`, `GetProfessionalMaterialAvailability`, `GetCoverStandardPreference`) —
these encode the whole decade transition and the genre non-uniformity (soul/teen/bubblegum keep
manufactured material late). Call it inside `GenerateRecordFromArtist`; apply via
`SongMaterialApplicationService` which blends composition hook into the record's existing performance
`hookStrength` and lowers `originality` for covers.
- **⚠ DETERMINISM.** `ChooseMaterial` must **not** draw from the global `GD` stream. Selection noise
  must be a **pure stable hash** over (artistId, songId, chartWeek) — the exact discipline Scouting
  Phase 1 used for perception ([ScoutingPerception](Systems/ScoutingPerception.cs)). If instead it
  draws RNG per release, it reseeds the entire downstream schedule (the Scouting Phase 3 hazard) and
  every replay seed changes. Prefer the stable-hash route so this phase can be measured against
  Phase 0 as a clean A/B.
- **Gate:** chart changes expected, **no** publishing money yet. Measure the decade curves (self-
  written / professional / cover-standard share by year, #1s by source) against the design targets.
  Confirm no emergent genre is starved of material (the Scouting Phase 3 caution).

### Phase 2 — Small launch familiarity input (draft §11) — IMPLEMENTED (bundled with Phase 1)
`Systems/SongLaunchService.cs`: bounded awareness (≤ +0.08, recent-hit > catalog > standard, originals
0) + radio lift, pure function of release-time fields (no RNG), added in `PromoteRecordAI`
([ChartManager.cs:787](Systems/ChartManager.cs:787)) AFTER the existing `GD.RandRange` launch draws so
the stream is unchanged. Kill-switch `SongLaunchService.LaunchInputEnabled`. Validated on the same
decade run as Phase 1.

Original plan text follows.

In `PromoteRecordAI`, add a **bounded** awareness + radio lift for familiar material via a
`SongLaunchService` — capped (≤ ~0.08 awareness), covers/standards only, *after* the existing
`GD.RandRange` draws. Familiar songs help *launch* awareness; they are not a weekly chart steroid. Keep
`LaunchInputEnabled` as a kill-switch.
- **Gate:** measure chart concentration and cover-song success rate. A known song should launch better
  but still lose to fatigue, the definitive-version shadow, and identity mismatch — verify covers are
  not auto-powerful.

### Phase 3 — Publishing routing on the EXISTING reallocation (draft §12, corrected)
Replace the binary `labelOwnsPublishing` decision inside the current settlement
([CompetitorManager.cs:966-983](Systems/CompetitorManager.cs:966)) with `PublishingControlType`-driven
routing off the record's attached `SongComposition.rights`. Public-domain → no composition sink (label
keeps the informational slice); external publisher → the slice leaks out (external income, off
LabelNet); label affiliate/buyout for *this* label → stays; artist-controlled → accrues to artist;
shared → split. **Keep it a reallocation of the existing `PublishingShareOfGross` pool — never add to
LabelNet.** Sub-phase exactly as the draft advises: (a) telemetry-only (populate the richer counterparty
fields, LabelNet unchanged), then (b) flip the routing live.
- **Gate:** label profitability and tier balance hold. External-publishing leakage should differ by
  label tier (majors capture more via affiliates) without any tier going insolvent. Re-derive `0.11`
  from measured profitability rather than trusting it — it directly touches bankruptcy.

### Phase 4 — Song memory + covers-of-hits become real (draft §13)
`CompositionCatalogService.OnRecordChartRunComplete`, called from `RunCulturalReads`
([RosterManager.cs:1231](Systems/RosterManager.cs:1231)): a completed run appends a
`SongRecordingMemory`, a hit raises the song's `nationalFamiliarity` (saturating), and a **top-40 peak
makes it `isCoverable`** — so an in-game hit becomes a future cover candidate for the Phase-1
recent-hit builder. This closes the loop that makes the *late* decade able to cover the *early*
decade's own hits.
- **Gate:** covers-of-in-game-hits appear and cluster behind big hits; cover fatigue and the
  definitive-version shadow visibly suppress the 3rd/4th cover of the same song.

### Phase 5 — Person-level songwriting credit (draft §14) — SCOPE DOWN
Credit actual writer-members for artist-written songs into the musician career ledger. Route any
prestige through the **recognition stock** ([[recognition-phase-a-implemented]]), **not**
`artist.criticalAcclaim` ([[criticalacclaim-is-a-dead-field]]). Land this as **credit telemetry
first** — its fame payoff is capped until [[lineup-churn-never-fires]] is solved (no solo careers spin
out of a star writer yet).
- **Gate:** writer credits accumulate correctly per person; no dependence on dead fields.

## Album behavior (draft §15)

Albums must not pick one source for the whole LP unless concept/self-contained. Add an
`AlbumMaterialPlan` (counts of originals / professional / covers / standards / traditional +
cohesion target + lead-single source). Early-decade LPs skew single + standards + covers + filler;
late-decade rock/folk/psych skew artist-written + cohesive. Preserve each track's `songId`/`songSource`
through the snapshot path. Fits alongside Phase 1 but can trail it — singles carry the decade signal;
albums are the polish pass.

## Determinism & calibration flags (the ones that will bite)

1. **Catalog generation on its own RNG stream** (Phase 0) — never the global `GD` stream, or the 1960
   population reseeds.
2. **Material selection must be a pure stable hash** (Phase 1), not `GD.RandRange` — otherwise every
   replay seed shifts and Phase 1 can't be A/B'd against Phase 0. This is the single biggest hazard;
   it is the Scouting Phase 3 reseed trap.
3. **Launch lift added *after* the existing `PromoteRecordAI` RNG draws** (Phase 2), never replacing
   them — preserves RNG order.
4. **Publishing stays a reallocation of the existing 0.11 pool** (Phase 3) — the draft's `+= pool` to
   LabelNet is the rejected economy-inflating pattern; do not resurrect it. Re-derive 0.11; it touches
   bankruptcy.
5. **No writer fame on `criticalAcclaim`** (Phase 5) — dead field; use the recognition stock.
6. **A valid decade A/B run requires the canonical flags** ([[canonical-decade-run-flags]]:
   `--enable-genre-market-v2 --enable-artist-population-lifecycle`) and Godot must be built
   `-c Debug` before every headless run ([[godot-headless-loads-bin-debug]]).

## Open questions to settle before Phase 1

- **Selection stability vs. variety.** A pure stable hash makes a given act's material choice
  deterministic per week — good for replays, but does it over-lock an artist into one source? May need
  the hash seeded on (artist, releaseCount) so successive releases can diverge.
- ~~Holiday seasonality~~ **(RESOLVED)** — reuse the existing seasonal-tag boost; tag the holiday
  catalog family with `GenreTag.Christmas`/`Seasonal` and inherit those tags onto covering records. No
  new demand-model work.
- ~~Professional-writer economy depth~~ **(RESOLVED)** — rights-metadata only for now (staff
  writers/publishers are just the counterparty on the song's rights); flesh out their agency/P&L later.
- **Re-derive `PublishingShareOfGross`** from measured label profitability once Phase 3 telemetry
  exists, rather than shipping the 0.11 guess into live routing.
```
