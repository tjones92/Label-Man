using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Publishing & Cover-Song layer, Phase 0 (data-only). Owns every SongComposition: the pre-1960
/// standards/catalog seeded before play, the professional writer/publisher pool (rights-metadata
/// only for now), and the artist-original stubs minted per release so every Record has a song
/// biography underneath it.
///
/// Determinism contract: this service draws ONLY from its own private RNG stream (seed-salted, never
/// the global GD stream), and per-release attachment (AttachArtistOriginal) reads already-computed
/// Record fields with NO randomness at all. Therefore Phase 0 is economy-byte-identical: the catalog
/// exists but nothing in the economy reads it yet (settlement still keys off
/// SimulatedArtist.labelOwnsPublishing). See SimTools/PublishingCoverSongDirective.md.
/// </summary>
public static class CompositionCatalogService {
	private static readonly Dictionary<string, SongComposition> songs = new(StringComparer.Ordinal);
	private static readonly Dictionary<Genre, List<SongComposition>> standardsByGenre = new();
	private static readonly Dictionary<Genre, List<SongComposition>> catalogByGenre = new();
	// Curated selectable pools for material selection. Kept SEPARATE from artist-originals: an
	// original is not a cover candidate until it charts (Phase 4). Polluting these with per-release
	// originals would drown the pre-existing material and make selection fall back to ArtistWritten,
	// worsening over the run and corrupting the decade transition curve.
	private static readonly Dictionary<Genre, List<SongComposition>> professionalByGenre = new();
	private static readonly Dictionary<Genre, List<SongComposition>> traditionalByGenre = new();
	private static readonly Dictionary<Genre, List<SongComposition>> coverableHitsByGenre = new();
	// Family-keyed cover pools: early rock covered R&B/blues, soul covered gospel/blues, etc. Selection
	// draws covers/standards/traditional across ADJACENT families (there are no RockAndRoll-primary
	// standards, so a rock act must reach the Blues / R&B songbook). Keyed by GenreFamily.
	private static readonly Dictionary<GenreFamily, List<SongComposition>> standardsByFamily = new();
	private static readonly Dictionary<GenreFamily, List<SongComposition>> traditionalByFamily = new();
	private static readonly Dictionary<GenreFamily, List<SongComposition>> coverableHitsByFamily = new();
	private static readonly List<ProfessionalSongwriter> professionalWriters = new();
	private static readonly List<MusicPublisher> publishers = new();
	// Phase 5: per-person songwriting chart-credit ledger (telemetry-only, keyed by personId). This is the
	// person ROLL-UP; the per-stint ledger below separates credits earned in one act from the next.
	private static readonly Dictionary<string, WriterCreditLedgerEntry> writerLedger = new(StringComparer.Ordinal);
	// Band-member simulation §2.3: the same ledger keyed personId|artistId, so once people move between acts
	// a credit earned in one band is never read as a grievance in the next. Also carries the credit MASS each
	// member holds on the act's originals (written when the credit is, not when a run completes), which is
	// what the CreditAndMoney strain reads as a share.
	private static readonly Dictionary<string, WriterCreditLedgerEntry> writerStintLedger = new(StringComparer.Ordinal);
	// Phase 3b catalog succession: label-controlled compositions indexed by controller label, so a dying
	// label's publishing catalog can pass to a successor instead of silently leaking (covers of its hits
	// then pay the successor). Only artist-originals carry a song-level controller label.
	private static readonly Dictionary<string, List<SongComposition>> songsByControllerLabel = new(StringComparer.Ordinal);
	private static RandomNumberGenerator rng;
	// Titles are drawn from a stream of their OWN, isolated from every stream the economy reads:
	// not GD.Rand, not NameGenerator's naming stream, and not the trait `rng` above. Because song
	// titles never feed a catalog song's traits (only freshly-generated album-track titles are
	// hashed, in CompetitorManager), pulling real titles here is inert to the simulated economy --
	// nothing that charts or settles changes -- while the player stops seeing "Recent DooWop Hit
	// Song 4446" on the stand and on their masters.
	private static RandomNumberGenerator titleRng;
	private static int songCounter;
	private static bool initialized;

	public static bool Initialized => initialized;
	public static int SongCount => songs.Count;
	/// <summary>The publisher houses, for the Rolodex's directory of who plugs what.</summary>
	public static IReadOnlyList<MusicPublisher> Publishers => publishers;
	public static int StandardCount { get { int n = 0; foreach (var kv in standardsByGenre) n += kv.Value.Count; return n; } }

	public static void Initialize(int startYear, IEnumerable<AILabel> labels, ulong seed) {
		CompositionShapeVariation.WorldSeed = seed;
		RepertoireProvenance.Reset();
		LiveRepertoire.Reset();
		PolarSongMetadataService.Reset();
		PolarSongBehavior.ResetTaste();
		PolarSongBehavior.ResetTaste();
		songs.Clear();
		standardsByGenre.Clear();
		catalogByGenre.Clear();
		professionalByGenre.Clear();
		traditionalByGenre.Clear();
		coverableHitsByGenre.Clear();
		standardsByFamily.Clear();
		traditionalByFamily.Clear();
		coverableHitsByFamily.Clear();
		professionalWriters.Clear();
		publishers.Clear();
		writerLedger.Clear();
		writerStintLedger.Clear();
		songsByControllerLabel.Clear();
		songCounter = 0;
		rng = new RandomNumberGenerator {
			Seed = seed ^ 0x736f6e6763617461UL // "songcata" -- private stream, isolated from GD
		};
		titleRng = new RandomNumberGenerator {
			Seed = seed ^ 0x7469746c65727364UL // "titlrsd" -- titles only, isolated from the trait stream
		};
		GeneratePreGameStandards(startYear);
		GeneratePreGameRecentHits(startYear);
		GenerateProfessionalPool(labels, startYear);
		GenerateInitialProfessionalCatalog(startYear);
		if(LiveRepertoire.AuditPhase>=5) {
			GenerateNativeRepertoire(startYear);
			foreach(var song in professionalByGenre.Values.SelectMany(p=>p)) AdmitRepertoire(song,"unpublished",startYear);
		}
		initialized = true;
		GD.Print($"CompositionCatalogService: {songs.Count} songs ({StandardCount} standards), {professionalWriters.Count} pro writers, {publishers.Count} publishers");
	}

	// ---- Pre-game standards / catalog --------------------------------------------------------

	private static void GeneratePreGameStandards(int startYear) {
		GenerateStandardFamily("Tin Pan Alley", Genre.TraditionalPop, Genre.EasyListening, 900, 1900, 1959, .68f, .72f);
		GenerateStandardFamily("Jazz Standard", Genre.Jazz, Genre.TraditionalPop, 500, 1915, 1959, .66f, .70f);
		GenerateStandardFamily("Country Standard", Genre.Country, Genre.Folk, 450, 1920, 1959, .60f, .67f);
		GenerateStandardFamily("Blues Standard", Genre.Blues, Genre.RnB, 400, 1920, 1959, .58f, .68f);
		GenerateStandardFamily("Gospel Standard", Genre.Gospel, Genre.Soul, 350, 1900, 1959, .56f, .70f);
		GenerateStandardFamily("Folk Traditional", Genre.Folk, Genre.Country, 500, 1850, 1959, .50f, .74f, traditional: true);
		GenerateStandardFamily("R&B Catalog", Genre.RnB, Genre.RockAndRoll, 350, 1945, 1959, .58f, .55f);
		// Holiday / evergreen standards: very durable, and tagged so the existing seasonal-tag boost
		// applies to any record cut from them (the tag ids ride onto covering records' genreTagIds).
		GenerateStandardFamily("Christmas Standard", Genre.TraditionalPop, Genre.EasyListening, 120, 1900, 1959, .66f, .90f,
			seasonalTags: new[] { "christmas", "seasonal" });
	}

	// Recent hits from the years just before play (1955-1959): what a 1960-61 act covers as a
	// "contemporary cover" (Pat Boone over Little Richard, etc.). Coverable, moderately familiar,
	// non-standard (they decay). Registered as live cover candidates so CoverRecentHit is non-empty
	// from week one; in-game hits join them via RegisterCoverableHit (Phase 4).
	private static void GenerateRecentHitFamily(string family, Genre primary, Genre secondary, int count, int minYear, int maxYear, float meanQuality) {
		for (int i = 0; i < count; i++) {
			var song = new SongComposition {
				songId = NextSongId(),
				title = GenerateRealTitle(primary),
				primaryGenre = primary,
				secondaryGenre = secondary,
				originYear = rng.RandiRange(minYear, maxYear),
				originKind = SongOriginKind.RecentHit,
				compositionQuality = ClampNormal(meanQuality, .13f),
				melodicStrength = ClampNormal(meanQuality, .13f),
				lyricQuality = ClampNormal(meanQuality - .02f, .14f),
				commercialHook = ClampNormal(meanQuality + .04f, .13f),
				rhythmicAppeal = ClampNormal(.58f, .17f),
				adaptability = ClampNormal(.58f, .17f),
				originality = ClampNormal(.50f, .16f),
				standardDurability = ClampNormal(.30f, .14f),
				nationalFamiliarity = ClampNormal(.52f, .16f),
				adultFamiliarity = ClampNormal(.42f, .18f),
				teenFamiliarity = ClampNormal(.55f, .18f),
				isStandard = false,
				isCoverable = true
			};
			song.rights.controlType = PublishingControlType.ExternalPublisher;
			song.rights.publisherId = "pre_game_publisher";
			song.rights.publisherName = "Legacy Publisher";
			song.credits.Add(new SongwriterCredit { writerType = WriterEntityType.HouseCredit, writerName = "Legacy Writer", share = 1f });
			if(LiveRepertoire.AuditPhase>=1)RepertoireTaxonomy.Assign(song, family, maxYear + 1);
			PolarSongMetadataService.EnsureComposition(song);
			songs[song.songId] = song;
			RegisterCoverableHit(song);
			RepertoireProvenance.Admit(song, "seededRecentHit", family);
		}
	}

	private static void GeneratePreGameRecentHits(int startYear) {
		int a = startYear - 5, b = startYear - 1; // 1955-1959
		GenerateRecentHitFamily("Recent RnR Hit", Genre.RockAndRoll, Genre.RnB, 220, a, b, .62f);
		GenerateRecentHitFamily("Recent R&B Hit", Genre.RnB, Genre.RockAndRoll, 200, a, b, .60f);
		GenerateRecentHitFamily("Recent Pop Hit", Genre.TraditionalPop, Genre.TeenPop, 180, a, b, .60f);
		GenerateRecentHitFamily("Recent Teen Hit", Genre.TeenPop, Genre.TraditionalPop, 160, a, b, .60f);
		GenerateRecentHitFamily("Recent DooWop Hit", Genre.DooWop, Genre.RnB, 120, a, b, .58f);
		GenerateRecentHitFamily("Recent Country Hit", Genre.Country, Genre.Folk, 120, a, b, .58f);
	}

	private static void GenerateStandardFamily(
		string family,
		Genre primary,
		Genre secondary,
		int count,
		int minYear,
		int maxYear,
		float meanQuality,
		float meanDurability,
		bool traditional = false,
		string[] seasonalTags = null
	) {
		for (int i = 0; i < count; i++) {
			var song = new SongComposition {
				songId = NextSongId(),
				title = GenerateRealTitle(primary),
				primaryGenre = primary,
				secondaryGenre = secondary,
				genreTagIds = seasonalTags ?? Array.Empty<string>(),
				originYear = rng.RandiRange(minYear, maxYear),
				originKind = traditional ? SongOriginKind.Traditional : SongOriginKind.PreGameStandard,
				compositionQuality = ClampNormal(meanQuality, .14f),
				melodicStrength = ClampNormal(meanQuality + .04f, .13f),
				lyricQuality = ClampNormal(meanQuality, .15f),
				commercialHook = ClampNormal(meanQuality - .02f, .16f),
				rhythmicAppeal = ClampNormal(.50f, .18f),
				adaptability = ClampNormal(.62f, .18f),
				originality = ClampNormal(.45f, .18f),
				standardDurability = ClampNormal(meanDurability, .16f),
				nationalFamiliarity = ClampNormal(.35f, .20f),
				adultFamiliarity = ClampNormal(.48f, .22f),
				teenFamiliarity = ClampNormal(.18f, .16f),
				isTraditional = traditional,
				isPublicDomain = traditional || rng.Randf() < .18f,
				isStandard = true
			};
			if(LiveRepertoire.AuditPhase>=5)song.establishedYear = traditional ? null : Math.Max(song.originYear + RepertoireProvenance.EstablishmentAge, minYear);
			if (song.isPublicDomain) {
				song.rights.controlType = PublishingControlType.PublicDomain;
				song.rights.writerShare = 0f;
				song.rights.publisherShare = 0f;
				song.credits.Add(new SongwriterCredit {
					writerType = WriterEntityType.PublicDomain,
					writerName = "Traditional",
					share = 1f
				});
			} else {
				song.rights.controlType = PublishingControlType.ExternalPublisher;
				song.rights.publisherId = "pre_game_publisher";
				song.rights.publisherName = "Legacy Publisher";
				song.credits.Add(new SongwriterCredit {
					writerType = WriterEntityType.HouseCredit,
					writerName = "Legacy Writer",
					share = 1f
				});
			}
			if(LiveRepertoire.AuditPhase>=1)RepertoireTaxonomy.Assign(song, family, maxYear + 1);
			// Retain the original Gospel fixture evidence alongside creation-time table authoring.
			if(family == "Gospel Standard" && song.repertoireSeedFamily == family && (!PolarSongTable.AuditLegacyRepair ||
				(SimulationSeedBootstrap.RequestedSeed is 1001 or 1002 && OS.GetCmdlineUserArgs().Contains("--polar-context-fixture"))))
				CompositionShapeVariation.AuthorContext(song, SongContentContext.Sacred, "seeded-authored-fixture:Gospel Standard");
			Register(song);
			RepertoireProvenance.Admit(song, "seedFamily", family);
		}
	}

	// ---- Professional writers / publishers (rights-metadata only in this phase) ---------------

	private static void GenerateProfessionalPool(IEnumerable<AILabel> labels, int startYear) {
		AddPublisher("pub_ny_pop", "New York Pop Factory", PublishingScene.NewYorkPopFactory, null,
			new[] { Genre.TeenPop, Genre.GirlGroup, Genre.TraditionalPop });
		AddPublisher("pub_nashville", "Nashville Publishing", PublishingScene.Nashville, null,
			new[] { Genre.Country, Genre.Folk });
		AddPublisher("pub_la_pop", "Los Angeles Pop", PublishingScene.LosAngelesPop, null,
			new[] { Genre.SunshinePop, Genre.TeenPop, Genre.Bubblegum, Genre.EasyListening });
		AddPublisher("pub_tin_pan", "Legacy Tin Pan Alley", PublishingScene.LegacyTinPanAlley, null,
			new[] { Genre.TraditionalPop, Genre.EasyListening, Genre.Jazz });
		// Label-affiliated in-house shops (Motown/Memphis-style) attach to a label if one exists.
		AddPublisher("pub_detroit", "Detroit In-House", PublishingScene.DetroitInHouse, null,
			new[] { Genre.Motown, Genre.Soul, Genre.RnB });

		// Staff writers, distributed across the scenes. Metadata only: no P&L, no agency yet.
		int writerCount = 120;
		for (int i = 0; i < writerCount; i++) {
			var pub = publishers[rng.RandiRange(0, publishers.Count - 1)];
			var writer = new ProfessionalSongwriter {
				writerId = $"prowriter_{i + 1:D4}",
				name = $"Staff Writer {i + 1}",
				publisherId = pub.publisherId,
				primaryGenre = pub.focusGenres.Length > 0 ? pub.focusGenres[0] : Genre.TraditionalPop,
				secondaryGenre = pub.focusGenres.Length > 1 ? pub.focusGenres[1] : Genre.EasyListening,
				melodyCraft = ClampNormal(.62f, .16f),
				lyricCraft = ClampNormal(.60f, .16f),
				hookCraft = ClampNormal(.66f, .15f),
				commercialInstinct = ClampNormal(.64f, .16f),
				versatility = ClampNormal(.55f, .18f),
				reliability = ClampNormal(.60f, .17f),
				trendSensitivity = ClampNormal(.55f, .18f),
				activeStartYear = startYear - rng.RandiRange(0, 6),
				activeEndYear = startYear + rng.RandiRange(6, 14)
			};
			professionalWriters.Add(writer);
			pub.staffWriterIds.Add(writer.writerId);
		}
	}

	private static void AddPublisher(string id, string name, PublishingScene scene, string affiliateLabelId, Genre[] focus) {
		publishers.Add(new MusicPublisher {
			publisherId = id,
			publisherName = name,
			affiliateLabelId = affiliateLabelId,
			scene = scene,
			focusGenres = focus ?? Array.Empty<Genre>(),
			catalogQuality = ClampNormal(.62f, .12f),
			songPluggerSkill = ClampNormal(.60f, .14f),
			commercialAggression = ClampNormal(.58f, .16f),
			artistFriendly = ClampNormal(.45f, .18f),
			buyoutWillingness = ClampNormal(.40f, .18f)
		});
	}

	// A modest office catalog available for professional/staff selection in Phase 1. Inert here.
	private static void GenerateInitialProfessionalCatalog(int startYear) {
		foreach (var pub in publishers) {
			int titles = pub.scene == PublishingScene.NewYorkPopFactory ? 90 : 45;
			for (int i = 0; i < titles; i++) {
				Genre primary = pub.focusGenres.Length > 0 ? pub.focusGenres[rng.RandiRange(0, pub.focusGenres.Length - 1)] : Genre.TraditionalPop;
				var song = new SongComposition {
					songId = NextSongId(),
					title = GenerateRealTitle(primary),
					primaryGenre = primary,
					secondaryGenre = pub.focusGenres.Length > 1 ? pub.focusGenres[1] : primary,
					originYear = startYear - rng.RandiRange(0, 3),
					originKind = SongOriginKind.ProfessionalOffice,
					compositionQuality = ClampNormal(.62f, .15f),
					melodicStrength = ClampNormal(.62f, .15f),
					lyricQuality = ClampNormal(.56f, .16f),
					commercialHook = ClampNormal(.68f, .15f),
					rhythmicAppeal = ClampNormal(.58f, .17f),
					adaptability = ClampNormal(.55f, .18f),
					originality = ClampNormal(.42f, .16f),
					standardDurability = ClampNormal(.35f, .16f),
					nationalFamiliarity = 0f,
					isStandard = false
				};
				song.rights.controlType = PublishingControlType.ExternalPublisher;
				song.rights.publisherId = pub.publisherId;
				song.rights.publisherName = pub.publisherName;
				song.credits.Add(new SongwriterCredit {
					writerType = WriterEntityType.ProfessionalSongwriter,
					writerId = pub.staffWriterIds.Count > 0 ? pub.staffWriterIds[rng.RandiRange(0, pub.staffWriterIds.Count - 1)] : null,
					writerName = "Staff Writer",
					share = 1f
				});
				CompositionShapeVariation.AuthorContext(song,pub.scene==PublishingScene.ChurchGospel?SongContentContext.Sacred:SongContentContext.Secular,
					"procedural-publishing-office:"+pub.publisherId+":"+pub.scene);
				Register(song);
				pub.catalogSongIds.Add(song.songId);
			}
		}
	}

	// ---- Per-release attachment (Phase 0: artist-original stub, ZERO randomness) --------------

	/// <summary>
	/// Mints an artist-original SongComposition from a Record's already-computed attributes and
	/// stamps the song identity + credit snapshot onto the Record. Reads existing fields only -- no
	/// RNG, no GD-stream touch -- so this is inert to the simulated economy. Later phases replace the
	/// unconditional "artist original" with real material selection.
	/// </summary>
	public static void AttachArtistOriginal(Record record, SimulatedArtist artist, AILabel label, int year) {
		if (record == null || artist == null) return;

		var song = new SongComposition {
			songId = $"song_orig_{record.recordId}",
			title = record.title,
			primaryGenre = record.primaryGenre,
			secondaryGenre = record.secondaryGenre,
			originYear = year,
			originKind = SongOriginKind.ArtistOriginal,
			originArtistId = artist.artistId,
			// Composition axis derived from the record's realized attributes (no new randomness).
			compositionQuality = record.hookStrength,
			melodicStrength = record.hookStrength,
			lyricQuality = record.originality,
			commercialHook = record.hookStrength,
			rhythmicAppeal = record.danceability,
			adaptability = Mathf.Clamp(record.originality * 0.6f + 0.2f, 0f, 1f),
			originality = record.originality,
			standardDurability = 0f,
			nationalFamiliarity = 0f,
			isStandard = false,
			isTraditional = false,
			isPublicDomain = false
		};

		// Credits: the act's writer-members if we can identify them, else a house credit.
		AuthorOriginalContext(song,artist);
		bool labelOwns = artist.labelOwnsPublishing;
		if (labelOwns) {
			song.rights.controlType = PublishingControlType.LabelAffiliate;
			song.rights.controllerLabelId = label?.labelId;
		} else {
			song.rights.controlType = PublishingControlType.ArtistControlled;
			song.rights.controllerArtistId = artist.artistId;
		}

		// Credits: a writing team from the act's writers (co-writing on), or the legacy first-writer credit.
		// Either way a house credit stands in when the act has no writer. Credits only -- no trait moves.
		CowritingService.CreditOriginal(song, artist, record.recordId, year);

		Register(song);
		IndexControllerLabel(song);
		ApplyToRecord(record, song, SongMaterialSource.ArtistWritten, isCover: false,
			originalRecordId: null, originalArtistId: null,
			familiarityAtRelease: 0f, arrangementOriginality: record.originality, professionalPolish: 0f);
	}

	/// <summary>Stamps a song's identity + credit snapshot onto a Record. Pure field copy.</summary>
	public static void ApplyToRecord(
		Record record, SongComposition song, SongMaterialSource source, bool isCover,
		string originalRecordId, string originalArtistId,
		float familiarityAtRelease, float arrangementOriginality, float professionalPolish, PolarArrangementProposal proposal = null
	) {
		if (record == null || song == null) return;
		record.songId = song.songId;
		record.songSource = source;
		record.isCover = isCover;
		record.originalRecordId = originalRecordId;
		record.originalArtistId = originalArtistId;
		record.publisherId = song.rights.publisherId;
		record.publishingControllerLabelId = song.rights.controllerLabelId;
		record.publishingControllerArtistId = song.rights.controllerArtistId;
		record.publishingControl = song.rights.controlType;

		int n = song.credits.Count;
		record.songwriterIds = new string[n];
		record.songwriterNames = new string[n];
		record.songwriterTypes = new WriterEntityType[n];
		record.songwriterShares = new float[n];
		for (int i = 0; i < n; i++) {
			record.songwriterIds[i] = song.credits[i].writerId;
			record.songwriterNames[i] = song.credits[i].writerName;
			record.songwriterTypes[i] = song.credits[i].writerType;
			record.songwriterShares[i] = song.credits[i].share;
		}

		record.compositionQuality = song.compositionQuality;
		record.compositionHook = song.commercialHook;
		record.lyricQuality = song.lyricQuality;
		record.songFamiliarityAtRelease = familiarityAtRelease;
		record.standardDurability = song.standardDurability;
		record.arrangementOriginality = arrangementOriginality;
		record.professionalPolish = professionalPolish;

		// Seasonal / holiday tags ride onto the record so the existing seasonal-tag boost applies.
		if (song.genreTagIds != null && song.genreTagIds.Length > 0) {
			record.genreTagIds = MergeTags(record.genreTagIds, song.genreTagIds);
		}
		PolarSongMetadataService.RegisterRecord(record, song, TimeManager.Instance?.CurrentDate ?? default, proposal: proposal);
		AdmitUnpublished(song,TimeManager.Instance?.CurrentDate.year??song.originYear,record.masterId);
	}

	// ---- Lookups & helpers -------------------------------------------------------------------

	public static SongComposition GetSong(string songId) =>
		!string.IsNullOrEmpty(songId) && songs.TryGetValue(songId, out var song) ? song : null;
	internal static IReadOnlyCollection<SongComposition> AllSongs => songs.Values;

	private static readonly List<SongComposition> emptySongs = new();

	/// <summary>All catalog songs whose PRIMARY genre matches (standards, professional, and hits).</summary>
	public static IReadOnlyList<SongComposition> GetCatalogForGenre(Genre genre) =>
		catalogByGenre.TryGetValue(genre, out var list) ? list : emptySongs;

	/// <summary>Standards (pre-game or promoted) whose primary genre matches.</summary>
	public static IReadOnlyList<SongComposition> GetStandardsForGenre(Genre genre) =>
		standardsByGenre.TryGetValue(genre, out var list) ? list : emptySongs;

	/// <summary>Professional (office/staff) catalog songs for a genre -- curated, no artist-originals.</summary>
	public static IReadOnlyList<SongComposition> GetProfessionalForGenre(Genre genre) =>
		professionalByGenre.TryGetValue(genre, out var list) ? list : emptySongs;

	/// <summary>Traditional / public-domain songs for a genre.</summary>
	public static IReadOnlyList<SongComposition> GetTraditionalForGenre(Genre genre) =>
		traditionalByGenre.TryGetValue(genre, out var list) ? list : emptySongs;

	/// <summary>In-game (or catalog) hits that have become coverable for a genre (Phase 4-fed).</summary>
	public static IReadOnlyList<SongComposition> GetCoverableHitsForGenre(Genre genre) =>
		coverableHitsByGenre.TryGetValue(genre, out var list) ? list : emptySongs;

	/// <summary>Standards belonging to a whole family (cross-genre cover source).</summary>
	public static IReadOnlyList<SongComposition> GetStandardsForFamily(GenreFamily family) =>
		standardsByFamily.TryGetValue(family, out var list) ? list : emptySongs;

	/// <summary>Traditional / public-domain songs belonging to a whole family.</summary>
	public static IReadOnlyList<SongComposition> GetTraditionalForFamily(GenreFamily family) =>
		traditionalByFamily.TryGetValue(family, out var list) ? list : emptySongs;

	/// <summary>Coverable hits belonging to a whole family (contemporary cross-genre covers).</summary>
	public static IReadOnlyList<SongComposition> GetCoverableHitsForFamily(GenreFamily family) =>
		coverableHitsByFamily.TryGetValue(family, out var list) ? list : emptySongs;

	/// <summary>
	/// Mints and registers a fresh artist-original song. Callers pass composition attributes derived
	/// deterministically (no GD RNG) from the artist/record; the song id is stable per record so a
	/// replay reproduces it. Used by the material-selection service's artist-written branch.
	/// </summary>
	public static SongComposition CreateArtistOriginal(
		Record record, SimulatedArtist artist, AILabel label, Genre genre, int year,
		float compositionQuality, float commercialHook, float lyricQuality, float originality
	) {
		var song = new SongComposition {
			songId = $"song_orig_{record.recordId}",
			title = record.title,
			primaryGenre = genre,
			secondaryGenre = record.secondaryGenre,
			originYear = year,
			originKind = SongOriginKind.ArtistOriginal,
			compositionQuality = compositionQuality,
			melodicStrength = compositionQuality,
			lyricQuality = lyricQuality,
			commercialHook = commercialHook,
			rhythmicAppeal = record.danceability,
			adaptability = Mathf.Clamp(originality * 0.6f + 0.2f, 0f, 1f),
			originality = originality,
			standardDurability = 0f,
			nationalFamiliarity = 0f,
			isStandard = false
		};
		AuthorOriginalContext(song,artist);
		if (artist.labelOwnsPublishing) {
			song.rights.controlType = PublishingControlType.LabelAffiliate;
			song.rights.controllerLabelId = label?.labelId;
		} else {
			song.rights.controlType = PublishingControlType.ArtistControlled;
			song.rights.controllerArtistId = artist.artistId;
		}
		CowritingService.CreditOriginal(song, artist, record.recordId, year);
		// Song-only registration: an artist-original is reachable by id but is NOT a selectable cover
		// candidate. It enters coverableHitsByGenre only if it charts (Phase 4).
		PolarSongMetadataService.EnsureComposition(song);
		songs[song.songId] = song;
		IndexControllerLabel(song);
		return song;
	}

	private static string NextSongId() => $"song_{++songCounter:D7}";
	private static void AuthorOriginalContext(SongComposition song,SimulatedArtist artist) {
		song.originArtistId=artist.artistId;
		CompositionShapeVariation.AuthorContext(song,artist.primaryGenre==Genre.Gospel||song.primaryGenre==Genre.Gospel?SongContentContext.Sacred:SongContentContext.Secular,
			"procedural-artist-original:"+artist.artistId+":"+artist.primaryGenre);
	}

	private static void Register(SongComposition song) {
		PolarSongMetadataService.EnsureComposition(song);
		songs[song.songId] = song;
		AddToPool(catalogByGenre, song.primaryGenre, song);
		GenreFamily fam = FamilyOf(song.primaryGenre);
		if(song.isStandard || song.isTraditional) LiveRepertoire.Index(song);
		if (song.isStandard) { AddToPool(standardsByGenre, song.primaryGenre, song); AddToPool(standardsByFamily, fam, song); }
		if (song.originKind == SongOriginKind.ProfessionalOffice) AddToPool(professionalByGenre, song.primaryGenre, song);
		if (song.originKind == SongOriginKind.ProfessionalOffice) RepertoireProvenance.Admit(song, "professionalCatalogue");
		if (song.isTraditional || LiveRepertoire.AuditPhase<5&&song.isPublicDomain) { AddToPool(traditionalByGenre, song.primaryGenre, song); AddToPool(traditionalByFamily, fam, song); }
	}

	private static GenreFamily FamilyOf(Genre g) => GenreCatalog.TryGet(g, out var p) ? p.Family : GenreFamily.Pop;

	private static void AddToPool<TKey>(Dictionary<TKey, List<SongComposition>> pool, TKey key, SongComposition song) {
		if (!pool.TryGetValue(key, out var list)) { list = new List<SongComposition>(); pool[key] = list; }
		list.Add(song);
	}

	// Phase 4 kill-switch. When off, chart runs append no song memory and in-game hits never become
	// coverable, so the recent-hit cover pool stays the pre-game 1955-59 set (Phase 1 behavior).
	public static bool ChartMemoryEnabled = true;

	/// <summary>
	/// Publishing &amp; Cover-Song Phase 4. A completed chart run feeds back into the song: it appends a
	/// <see cref="SongRecordingMemory"/>, raises the song's national familiarity (saturating, by hit
	/// size), and a top-40 peak makes the song <c>isCoverable</c> -- so an in-game hit becomes a future
	/// cover candidate for the Phase-1 recent-hit builder. This closes the loop that lets the LATE decade
	/// cover the EARLY decade's own hits. No RNG: a pure function of the record's realized chart outcome.
	/// Idempotent per record via RunCulturalReads' culturalRunCompleted guard.
	/// </summary>
	public static void OnRecordChartRunComplete(RecordRuntimeData record, int year) {
		if (!ChartMemoryEnabled || record?.baseRecord == null) return;
		SongComposition song = GetSong(record.baseRecord.songId);
		if (song == null) return;

		int peak = record.peakPosition;
		bool top40 = peak >= 1 && peak <= 40;
		// #1 -> ~1.0, #40 -> ~0.025, unranked -> 0.
		float peakStrength = top40 ? Mathf.Clamp((41 - peak) / 40f, 0f, 1f) : 0f;
		float unitStrength = 1f - Mathf.Exp(-Mathf.Max(0, record.totalUnitsSold) / 200000f);
		float successScore = Mathf.Clamp(peakStrength * 0.6f + unitStrength * 0.4f, 0f, 1f);

		song.recordings.Add(new SongRecordingMemory {
			recordId = record.baseRecord.recordId,
			artistId = record.baseRecord.artistId,
			artistName = record.baseRecord.artistName,
			year = year,
			peakPosition = peak,
			weeksOnChart = record.weeksOnChart,
			units = record.totalUnitsSold,
			definitiveVersionScore = successScore
		});

		// Familiarity rises toward saturation with hit size (a #1 imprints far more than a #38).
		float lift = Mathf.Max(peakStrength, unitStrength * 0.5f) * 0.25f;
		song.nationalFamiliarity = Mathf.Clamp(song.nationalFamiliarity + (1f - song.nationalFamiliarity) * lift, 0f, 1f);

		// A top-40 hit becomes a live cover candidate. Artist-originals were registered song-only; this
		// is what promotes them into the recent-hit pool for future covers.
		if (top40) {
			song.isCoverable = true;
			RegisterCoverableHit(song);
			RepertoireProvenance.Admit(song, "chartPromoted", year: year, recordId: record.baseRecord.recordId);
		}

		// Phase 5 (scoped to credit telemetry -- see [[lineup-churn-never-fires]]: no solo career spins
		// out yet, so this is a ledger, not a fame engine). Credit each writer-member for the run. No
		// dependence on the dead criticalAcclaim field; prestige routing is deferred to the recognition
		// stock. Pure accumulation, no economy or chart feedback.
		foreach (SongwriterCredit credit in song.credits) {
			if (credit.writerType != WriterEntityType.Musician || string.IsNullOrEmpty(credit.writerId)) continue;
			if (!writerLedger.TryGetValue(credit.writerId, out WriterCreditLedgerEntry led)) {
				led = new WriterCreditLedgerEntry { personId = credit.writerId, name = credit.writerName };
				writerLedger[credit.writerId] = led;
			}
			AccrueRun(led, song, top40, peak, record.totalUnitsSold, successScore);
			// The stint: credits on an act's own original belong to the act the song came from.
			if (credit.isArtistMember && !string.IsNullOrEmpty(song.originArtistId))
				AccrueRun(StintEntry(credit.writerId, song.originArtistId, credit.writerName), song, top40, peak,
					record.totalUnitsSold, successScore);
		}
	}

	private static void AccrueRun(WriterCreditLedgerEntry led, SongComposition song, bool top40, int peak, int units,
		float successScore) {
		led.creditedRuns++;
		if (song.originKind == SongOriginKind.ArtistOriginal) led.originalCredits++;
		if (top40) led.top40Credits++;
		if (peak == 1) led.number1Credits++;
		led.totalUnits += Mathf.Max(0, units);
		led.bestSuccess = Mathf.Max(led.bestSuccess, successScore);
	}

	private static WriterCreditLedgerEntry StintEntry(string personId, string artistId, string name) {
		string key = StintKey(personId, artistId);
		if (!writerStintLedger.TryGetValue(key, out WriterCreditLedgerEntry entry)) {
			entry = new WriterCreditLedgerEntry { personId = personId, artistId = artistId, name = name };
			writerStintLedger[key] = entry;
		}
		return entry;
	}

	public static string StintKey(string personId, string artistId) => personId + "|" + artistId;

	/// <summary>
	/// Records who holds the credit on a freshly credited artist original, per stint. Called wherever an
	/// original's credits are written. Pure accumulation into a ledger nothing in the economy reads.
	/// </summary>
	internal static void RecordOriginalCredits(SongComposition song, SimulatedArtist artist) {
		if (song?.credits == null || artist == null) return;
		int memberWriters = 0;
		foreach (SongwriterCredit credit in song.credits)
			if (credit.isArtistMember && !string.IsNullOrEmpty(credit.writerId)) memberWriters++;
		foreach (SongwriterCredit credit in song.credits) {
			if (!credit.isArtistMember || string.IsNullOrEmpty(credit.writerId)) continue;
			WriterCreditLedgerEntry entry = StintEntry(credit.writerId, artist.artistId, credit.writerName);
			entry.creditMass += credit.share;
			entry.songs++;
			if (memberWriters > 1) entry.coWrittenSongs++;
		}
	}

	/// <summary>One member's credited share of an act's originals: their credit mass over the act's total.
	/// Zero for a member with no credits; zero everywhere for an act with no credited originals.</summary>
	public static float GetStintCreditShare(string personId, SimulatedArtist artist) {
		if (artist?.members == null || string.IsNullOrEmpty(personId)) return 0f;
		float mine = 0f, total = 0f;
		foreach (Musician m in artist.members) {
			if (m == null) continue;
			float mass = writerStintLedger.TryGetValue(StintKey(m.personId, artist.artistId), out var e) ? e.creditMass : 0f;
			total += mass;
			if (m.personId == personId) mine = mass;
		}
		if (artist.alumni != null)
			foreach (AlumniRecord a in artist.alumni)
				if (writerStintLedger.TryGetValue(StintKey(a.personId, artist.artistId), out var e)) total += e.creditMass;
		return total > 0f ? mine / total : 0f;
	}

	public static WriterCreditLedgerEntry GetStint(string personId, string artistId) =>
		writerStintLedger.TryGetValue(StintKey(personId, artistId), out var e) ? e : null;

	/// <summary>Whether a person holds any writer credit anywhere -- the pool's "career to continue" test.</summary>
	public static bool HasAnyWriterCredit(string personId) {
		if (string.IsNullOrEmpty(personId)) return false;
		if (writerLedger.ContainsKey(personId)) return true;
		foreach (WriterCreditLedgerEntry e in writerStintLedger.Values) if (e.personId == personId && e.songs > 0) return true;
		return false;
	}

	/// <summary>Per-person accumulation of songwriting chart credits (Phase 5, telemetry-only). The same type
	/// serves the per-stint ledger, where <see cref="artistId"/> is set and the credit-mass fields are filled.</summary>
	public sealed class WriterCreditLedgerEntry {
		public string personId;
		public string name;
		public string artistId;      // set on stint entries only
		public int creditedRuns;     // completed chart runs of songs this person is credited on
		public int originalCredits;  // of those, artist-original compositions
		public int top40Credits;
		public int number1Credits;
		public long totalUnits;
		public float bestSuccess;
		public float creditMass;     // stint only: sum of credit shares across the act's originals
		public int songs;            // stint only: originals credited in this act
		public int coWrittenSongs;   // stint only: of those, credited to more than one member
	}

	public static IReadOnlyCollection<WriterCreditLedgerEntry> WriterCreditLedger => writerLedger.Values;
	public static IReadOnlyCollection<WriterCreditLedgerEntry> WriterStintLedger => writerStintLedger.Values;

	// Index an artist-original whose publishing a label controls, so catalog succession can find it.
	private static void IndexControllerLabel(SongComposition song) {
		string labelId = song?.rights?.controllerLabelId;
		if (string.IsNullOrEmpty(labelId)) return;
		if (!songsByControllerLabel.TryGetValue(labelId, out var list)) {
			list = new List<SongComposition>(); songsByControllerLabel[labelId] = list;
		}
		list.Add(song);
	}

	/// <summary>
	/// Phase 3b catalog succession: reassign every composition a dying/absorbed label controlled to a
	/// successor (an acquirer, or a surviving major that buys the catalog). Covers of those songs then
	/// pay the successor's publishing receipts instead of leaking to a defunct firm. If there is no
	/// successor the catalog is orphaned (left as-is; it will leak). Deterministic, no RNG.
	/// </summary>
	public static void TransferCatalogControl(string fromLabelId, string toLabelId) {
		if (string.IsNullOrEmpty(fromLabelId) || string.IsNullOrEmpty(toLabelId) || fromLabelId == toLabelId) return;
		if (!songsByControllerLabel.TryGetValue(fromLabelId, out var moving) || moving.Count == 0) return;
		foreach (SongComposition song in moving) {
			if (song.rights != null) {
				song.rights.controllerLabelId = toLabelId;
				// LabelAffiliate becomes a buyout under the successor: it acquired the catalog outright.
				if (song.rights.controlType == PublishingControlType.LabelAffiliate)
					song.rights.controlType = PublishingControlType.LabelBuyout;
			}
		}
		if (!songsByControllerLabel.TryGetValue(toLabelId, out var dest)) {
			dest = new List<SongComposition>(); songsByControllerLabel[toLabelId] = dest;
		}
		dest.AddRange(moving);
		songsByControllerLabel.Remove(fromLabelId);
	}

	/// <summary>Marks a charted song as a live cover candidate for its genre (Phase 4 entry point).</summary>
	public static void RegisterCoverableHit(SongComposition song) {
		if (song == null || !song.isCoverable) return;
		LiveRepertoire.Index(song);
		if (!coverableHitsByGenre.TryGetValue(song.primaryGenre, out var pool)) {
			pool = new List<SongComposition>(); coverableHitsByGenre[song.primaryGenre] = pool;
		}
		if (!pool.Contains(song)) pool.Add(song);
		if(LiveRepertoire.AuditPhase<5){AddToPool(coverableHitsByFamily,FamilyOf(song.primaryGenre),song);return;}
		var family=FamilyOf(song.primaryGenre);
		if(!coverableHitsByFamily.TryGetValue(family,out var familyPool)) coverableHitsByFamily[family]=familyPool=new();
		if(!familyPool.Contains(song))familyPool.Add(song);
	}

	// Phase 5 admissions are independent of chart outcome. Routes persist on compositions and indexes already serialize.
	public static void AdmitRepertoire(SongComposition song,string route,int year,string recordId=null) {
		if(song==null||!song.isCoverable)return;
		if(!song.repertoireAdmissionRoutes.Contains(route)) {
			song.repertoireAdmissionRoutes.Add(route);RegisterCoverableHit(song);
			RepertoireProvenance.Admit(song,route,song.repertoireSeedFamily,year,recordId);
		}
	}
	public static void RecordRepertoireRelease(SongComposition song,int year,string recordId,bool album) {
		if(song==null||LiveRepertoire.AuditPhase<5)return;
		if(song.repertoireFirstReleaseYear==0)song.repertoireFirstReleaseYear=year;
		if(album)AdmitRepertoire(song,"albumRepertoire",year,recordId);
		else if(song.commercialHook>=.55f)AdmitRepertoire(song,"locallyFamiliar",year,recordId);
		if(song.originKind is SongOriginKind.ProfessionalOffice or SongOriginKind.LabelStaff)AdmitRepertoire(song,"suppliedProfessional",year,recordId);
	}
	public static void AdmitUnpublished(SongComposition song,int year,string masterId) {
		if(LiveRepertoire.AuditPhase>=5 && song?.originKind is SongOriginKind.ArtistOriginal or SongOriginKind.ProfessionalOffice or SongOriginKind.LabelStaff)
			AdmitRepertoire(song,"unpublished",year,masterId);
	}
	public static void OnRecordReleased(Record record) {
		if(LiveRepertoire.AuditPhase<5||record==null)return;
		int year=record.releaseDate.year>0?record.releaseDate.year:TimeManager.Instance?.CurrentDate.year??0;
		if(record.format==ReleaseFormat.Album && record.album!=null) {
			if(record.album.albumFormat==AlbumFormat.Soundtrack&&record.album.externalMedia!=null)AdmitExternalMediaAlbum(record,year);
			foreach(var track in record.album.GetAllTracks())if(track!=null)RecordRepertoireRelease(GetSong(track.songId),year,track.masterId,true);
		}else RecordRepertoireRelease(GetSong(record.songId),year,record.recordId,false);
	}
	// Album cuts are masters inside the album, never separately released Records. Keep the
	// existing theme ID/shape and the album's pooled appeal, license and release economics.
	internal static void AdmitExternalMediaAlbum(Record record,int year) {
		var theme=AdmitExternalMediaTheme(record,year);
		if(theme==null||record.album.GetAllTracks().Length>0)return;
		int count=record.album.runtimeMinutes>0?Math.Clamp((int)Math.Round(record.album.runtimeMinutes/3f),8,16):12;
		var tracks=new AlbumTrack[count];
		for(int i=0;i<count;i++) {
			var song=i==0?theme:CreateExternalMediaComposition(record,year,i);
			var track=new AlbumTrack {masterId=$"{record.recordId}:media:{i:D2}",title=song.title,genre=record.primaryGenre,
				quality=record.album.pooledAppeal,hookStrength=record.hookStrength,productionQuality=record.productionQuality,
				danceability=record.danceability,isReleasedSingle=false,releaseDate=record.releaseDate};
			SongMaterialApplicationService.ApplyIdentityToAlbumTrack(track,new SelectedSongMaterial {
				Song=song,Source=SongMaterialSource.ExternalProfessional,IsCover=false,FamiliarityAtRelease=song.GetFamiliarityForYear(year)});
			PolarSongMetadataService.RegisterTrack(track,song,track.masterId,record.artistId,record.releaseDate);
			tracks[i]=track;
		}
		record.album.nonSingleTracks=tracks;
	}
	// The representative theme remains compatible with albums/savegames from the previous pass.
	internal static SongComposition AdmitExternalMediaTheme(Record record,int year) {
		return CreateExternalMediaComposition(record,year,0);
	}
	private static SongComposition CreateExternalMediaComposition(Record record,int year,int cut) {
		if(record?.album?.externalMedia==null||record.album.albumFormat!=AlbumFormat.Soundtrack||string.IsNullOrEmpty(record.recordId))return null;
		string id=cut==0?"song_media_theme_"+record.recordId:$"song_media_cut_{record.recordId}_{cut:D2}";
		if(songs.TryGetValue(id,out var existing))return existing;
		float U(string salt)=>RepertoireTaxonomy.Unit(id+"|"+salt);
		var media=record.album.externalMedia;
		bool instrumental=media.sourceType==ExternalMediaSourceType.FilmScore;
		var song=new SongComposition {songId=id,title=record.title+(cut==0?" (theme)":instrumental?$" (cue {cut+1})":$" (song {cut+1})"),primaryGenre=Genre.EasyListening,secondaryGenre=Genre.TraditionalPop,
			originYear=year,originKind=cut==0?SongOriginKind.ExternalMediaTheme:SongOriginKind.ExternalMediaComposition,repertoireFirstReleaseYear=year,
			externalMediaSourceRecordId=record.recordId,externalMediaSourceType=media.sourceType,
			compositionQuality=.5f+media.criticalPrestige*.3f,melodicStrength=.55f+U("melody")*.25f,lyricQuality=instrumental?0:.5f+U("lyric")*.25f,
			commercialHook=.45f+media.sourcePopularity*.3f,rhythmicAppeal=.4f+U("rhythm")*.2f,adaptability=.7f,
			originality=.45f+U("originality")*.2f,standardDurability=.5f+media.criticalPrestige*.25f,
			nationalFamiliarity=media.sourcePopularity,adultFamiliarity=media.sourcePopularity,teenFamiliarity=media.youthAppeal*.5f,isCoverable=true};
		RepertoireTaxonomy.Assign(song,instrumental?"Screen instrumental":"Stage and film songs",year);
		song.rights.controlType=PublishingControlType.ExternalPublisher;
		song.rights.publisherId="external_media_publisher";song.rights.publisherName="Screen & Stage Publisher";
		song.credits.Add(new SongwriterCredit {writerType=WriterEntityType.HouseCredit,writerName="Screen & Stage Composer",share=1});
		Register(song);AdmitRepertoire(song,cut==0?"externalMediaTheme":"externalMediaAlbumCut",year,record.recordId);
		return song;
	}
	private static void GenerateNativeRepertoire(int startYear) {
		// A separate keyed stream: extending supply never consumes the established catalogue or global RNG.
		foreach(var scene in PolarRepertoireTable.Current.Supply)for(int i=0;i<scene.Count;i++) {
			string id=$"song_native_{scene.Genre}_{i:D4}";
			float U(string salt)=>RepertoireTaxonomy.Unit(id+"|"+salt);
			int year=scene.FromYear+(int)(U("year")*(scene.ToYear-scene.FromYear+1));
			var song=new SongComposition {songId=id,title=$"{scene.Family} {i+1}",primaryGenre=Enum.Parse<Genre>(scene.Genre),secondaryGenre=Enum.Parse<Genre>(scene.Secondary),
				originYear=year,originKind=scene.Traditional?SongOriginKind.Traditional:SongOriginKind.PreGameCatalog,
				compositionQuality=.50f+U("craft")*.30f,melodicStrength=.50f+U("melody")*.30f,lyricQuality=.45f+U("lyric")*.30f,
				commercialHook=.40f+U("hook")*.35f,rhythmicAppeal=.40f+U("rhythm")*.35f,adaptability=.50f+U("adaptability")*.20f,
				originality=.40f+U("originality")*.30f,standardDurability=.50f+U("durability")*.30f,nationalFamiliarity=.25f+U("familiarity")*.35f,
				isTraditional=scene.Traditional,isPublicDomain=scene.Traditional||year<1923,isStandard=scene.Family!="Comedy routines"&&!scene.Traditional&&(scene.EstablishedYear??year+RepertoireProvenance.EstablishmentAge)<=startYear,isCoverable=true,
				repertoireFirstReleaseYear=year,establishedYear=scene.Traditional||scene.Family=="Comedy routines"?null:scene.EstablishedYear??year+RepertoireProvenance.EstablishmentAge};
			RepertoireTaxonomy.Assign(song,scene.Family,Math.Max(startYear,year));
			if(song.isPublicDomain){song.rights.controlType=PublishingControlType.PublicDomain;song.rights.writerShare=song.rights.publisherShare=0;}
			else {song.rights.controlType=PublishingControlType.ExternalPublisher;song.rights.publisherId="native_songbook";song.rights.publisherName="Independent Repertoire";}
			song.credits.Add(new SongwriterCredit {writerType=scene.Traditional?WriterEntityType.Traditional:WriterEntityType.HouseCredit,writerName=scene.Traditional?"Traditional":"Catalogue Writer",share=1});
			Register(song);RegisterCoverableHit(song);RepertoireProvenance.Admit(song,"seedFamily",scene.Family,startYear);
		}
	}
	internal static void ApplyRepertoireAuditPhase(int phase,int startYear) {
		if(phase!=LiveRepertoire.AuditPhase+1)throw new InvalidOperationException("Causal probe phases must advance sequentially");
		LiveRepertoire.AuditPhase=phase;
		if(phase==1)foreach(var a in RepertoireProvenance.Admissions.Where(a=>a.Route is "seedFamily" or "seededRecentHit").ToArray())
			RepertoireTaxonomy.Assign(GetSong(a.SongId),a.Family,startYear);
		if(phase==5) {
			traditionalByGenre.Clear();traditionalByFamily.Clear();
			foreach(var s in songs.Values) {
				if(s.isTraditional){AddToPool(traditionalByGenre,s.primaryGenre,s);AddToPool(traditionalByFamily,FamilyOf(s.primaryGenre),s);}
				else if(s.isStandard)s.establishedYear=s.originYear+RepertoireProvenance.EstablishmentAge;
			}
			GenerateNativeRepertoire(startYear);
			foreach(var song in professionalByGenre.Values.SelectMany(p=>p))AdmitRepertoire(song,"unpublished",startYear);
		}
	}

	// ========================================================================
	// FULL-WORLD SAVE -- the composition catalogue. Songs/writers/publishers/ledger serialize whole; the
	// by-genre/by-family pools and the controller-label index travel as song-id lists (they share SongComposition
	// objects with `songs` and are not a pure function of it, so they are relinked, not rebuilt). RNG preserved.
	// ========================================================================

	public static void CaptureWorld(WorldSaveData w) {
		foreach (var song in songs.Values) PolarSongMetadataService.EnsureComposition(song);
		PolarSongMetadataService.MigrateRecords((w.Records ?? new()).Where(r => r != null).Select(r => r.baseRecord), w.RetiredTrackArchive);
		var c = new CompositionSaveData {
			ShapeVariationVersion = CompositionShapeVariation.ActiveVersion,
			ShapeVariationWorldSeed = CompositionShapeVariation.WorldSeed,
			RepertoireSchemaVersion = 2,
			UsePolarFitSelection = PolarSongBehavior.UsePolarFitSelection,
			PolarTaste = PolarSongBehavior.CaptureTaste(),
			PolarMasters = PolarSongMetadataService.Capture(),
			Songs = new Dictionary<string, SongComposition>(songs),
			StandardsByGenre = GenrePoolToIds(standardsByGenre),
			CatalogByGenre = GenrePoolToIds(catalogByGenre),
			ProfessionalByGenre = GenrePoolToIds(professionalByGenre),
			TraditionalByGenre = GenrePoolToIds(traditionalByGenre),
			CoverableHitsByGenre = GenrePoolToIds(coverableHitsByGenre),
			StandardsByFamily = FamilyPoolToIds(standardsByFamily),
			TraditionalByFamily = FamilyPoolToIds(traditionalByFamily),
			CoverableHitsByFamily = FamilyPoolToIds(coverableHitsByFamily),
			SongsByControllerLabel = songsByControllerLabel.ToDictionary(kv => kv.Key, kv => kv.Value.Select(s => s.songId).ToList()),
			ProfessionalWriters = professionalWriters.ToList(),
			Publishers = publishers.ToList(),
			WriterLedger = new Dictionary<string, WriterCreditLedgerEntry>(writerLedger),
			WriterStintLedger = new Dictionary<string, WriterCreditLedgerEntry>(writerStintLedger),
			SongCounter = songCounter
		};
		if (rng != null && titleRng != null) {
			c.HasRng = true;
			c.RngSeed = rng.Seed; c.RngState = rng.State;
			c.TitleRngSeed = titleRng.Seed; c.TitleRngState = titleRng.State;
		}
		w.Composition = c;
	}

	internal static int MigrateComedyRepertoire(IEnumerable<SongComposition> catalogue) {
		int changed=0;
		foreach(var song in catalogue.Where(RepertoireProvenance.ComedyRoutine))if(song.establishedYear.HasValue||song.isStandard) {
			song.establishedYear=null;song.isStandard=false;changed++;
		}
		return changed;
	}
	public static void RehydrateWorld(WorldSaveData w) {
		CompositionSaveData c = w.Composition;
		if (c == null) return;
		CompositionShapeVariation.ActiveVersion = CompositionShapeVariation.ResolveVersion(OS.GetCmdlineUserArgs(), c.ShapeVariationVersion);
		CompositionShapeVariation.WorldSeed = c.ShapeVariationWorldSeed ?? (c.HasRng ? c.RngSeed ^ 0x736f6e6763617461UL : CompositionShapeVariation.WorldSeed);
		PolarSongBehavior.UsePolarFitSelection = c.UsePolarFitSelection;
		PolarSongBehavior.RestoreTaste(c.PolarTaste);

		songs.Clear();
		foreach (var kv in c.Songs ?? new Dictionary<string, SongComposition>()) if (kv.Value != null) songs[kv.Key] = kv.Value;
		if(c.RepertoireSchemaVersion<2){MigrateComedyRepertoire(songs.Values);c.RepertoireSchemaVersion=2;}
		PolarSongMetadataService.Restore(c.PolarMasters);
		foreach (var song in songs.Values) PolarSongMetadataService.EnsureComposition(song);
		PolarSongMetadataService.MigrateRecords((w.Records ?? new()).Where(r => r != null).Select(r => r.baseRecord), w.RetiredTrackArchive);

		RebuildGenrePool(standardsByGenre, c.StandardsByGenre);
		RebuildGenrePool(catalogByGenre, c.CatalogByGenre);
		RebuildGenrePool(professionalByGenre, c.ProfessionalByGenre);
		RebuildGenrePool(traditionalByGenre, c.TraditionalByGenre);
		RebuildGenrePool(coverableHitsByGenre, c.CoverableHitsByGenre);
		RebuildFamilyPool(standardsByFamily, c.StandardsByFamily);
		RebuildFamilyPool(traditionalByFamily, c.TraditionalByFamily);
		RebuildFamilyPool(coverableHitsByFamily, c.CoverableHitsByFamily);
		LiveRepertoire.Reset();
		foreach(var song in standardsByGenre.Values.Concat(coverableHitsByGenre.Values).SelectMany(p=>p).DistinctBy(s=>s.songId)) LiveRepertoire.Index(song);

		songsByControllerLabel.Clear();
		foreach (var kv in c.SongsByControllerLabel ?? new Dictionary<string, List<string>>())
			songsByControllerLabel[kv.Key] = ResolveSongs(kv.Value);

		professionalWriters.Clear(); professionalWriters.AddRange(c.ProfessionalWriters ?? new List<ProfessionalSongwriter>());
		publishers.Clear(); publishers.AddRange(c.Publishers ?? new List<MusicPublisher>());
		writerLedger.Clear();
		foreach (var kv in c.WriterLedger ?? new Dictionary<string, WriterCreditLedgerEntry>()) writerLedger[kv.Key] = kv.Value;
		writerStintLedger.Clear();
		foreach (var kv in c.WriterStintLedger ?? new Dictionary<string, WriterCreditLedgerEntry>()) writerStintLedger[kv.Key] = kv.Value;

		songCounter = c.SongCounter;
		if (c.HasRng) {
			rng = new RandomNumberGenerator { Seed = c.RngSeed }; rng.State = c.RngState;
			titleRng = new RandomNumberGenerator { Seed = c.TitleRngSeed }; titleRng.State = c.TitleRngState;
		}
	}

	private static Dictionary<int, List<string>> GenrePoolToIds(Dictionary<Genre, List<SongComposition>> pool) =>
		pool.ToDictionary(kv => (int)kv.Key, kv => kv.Value.Select(s => s.songId).ToList());
	private static Dictionary<int, List<string>> FamilyPoolToIds(Dictionary<GenreFamily, List<SongComposition>> pool) =>
		pool.ToDictionary(kv => (int)kv.Key, kv => kv.Value.Select(s => s.songId).ToList());
	private static void RebuildGenrePool(Dictionary<Genre, List<SongComposition>> pool, Dictionary<int, List<string>> saved) {
		pool.Clear();
		foreach (var kv in saved ?? new Dictionary<int, List<string>>()) pool[(Genre)kv.Key] = ResolveSongs(kv.Value);
	}
	private static void RebuildFamilyPool(Dictionary<GenreFamily, List<SongComposition>> pool, Dictionary<int, List<string>> saved) {
		pool.Clear();
		foreach (var kv in saved ?? new Dictionary<int, List<string>>()) pool[(GenreFamily)kv.Key] = ResolveSongs(kv.Value);
	}
	private static List<SongComposition> ResolveSongs(List<string> ids) {
		var list = new List<SongComposition>();
		foreach (string id in ids ?? new List<string>()) if (id != null && songs.TryGetValue(id, out SongComposition s)) list.Add(s);
		return list;
	}

	private static string[] MergeTags(string[] existing, string[] incoming) {
		var set = new List<string>(existing ?? Array.Empty<string>());
		foreach (var tag in incoming) {
			if (!set.Contains(tag)) set.Add(tag);
		}
		return set.ToArray();
	}

	private static float ClampNormal(float mean, float stdDev) {
		float u1 = Mathf.Max(.000001f, rng.Randf());
		float u2 = rng.Randf();
		float normal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.Pi * u2);
		return Mathf.Clamp(mean + normal * stdDev, 0f, 1f);
	}

	// Placeholder titling; replaced by naming v2 in a later pass.
	// A period, genre-tinted 45 title. In the running game this comes from the full naming engine on
	// its own dedicated catalog stream + bucket (NameGenerator.GenerateCatalogTitle), which is provably
	// inert to the economy -- so these legacy songs read exactly like every runtime title. The word-bank
	// below is only a fallback for headless tools that run the catalog without the naming autoload.
	private static string GenerateRealTitle(Genre genre) {
		string engineTitle = NameGenerator.Instance?.GenerateCatalogTitle(genre, 1959);
		if (!string.IsNullOrWhiteSpace(engineTitle)) return engineTitle;
		string style = genre switch {
			Genre.Country or Genre.Folk or Genre.TexMex => "country",
			Genre.RnB or Genre.Soul or Genre.Motown or Genre.Blues or Genre.Gospel or Genre.DooWop => "soul",
			Genre.Jazz or Genre.TraditionalPop or Genre.EasyListening => "pop",
			_ => "rock"
		};
		return style switch {
			"country" => Pick(CountryTitles),
			"soul"    => Pick(SoulTitles),
			"pop"     => Pick(PopTitles),
			_         => Pick(RockTitles)
		};
	}

	// Two-part titles ("{opener} {closer}") give a few hundred plausible combinations per style off a
	// short bank -- enough that repeats read like the era's real title churn rather than a bug.
	private static string Pick((string[] Openers, string[] Closers) bank) =>
		$"{bank.Openers[titleRng.RandiRange(0, bank.Openers.Length - 1)]} " +
		$"{bank.Closers[titleRng.RandiRange(0, bank.Closers.Length - 1)]}";

	private static readonly (string[] Openers, string[] Closers) PopTitles = (
		new[] { "Moonlight", "Autumn", "Stardust", "Blue", "Sweet", "My Foolish", "The Nearness of", "Dream",
			"Someone to", "Till", "Because of", "A Kiss to", "Stella by", "September", "Lover's", "The Song Is" },
		new[] { "Serenade", "Leaves", "Melody", "Moon", "Lorraine", "Heart", "You", "a Little Dream", "Watch Over Me",
			"the End of Time", "Build a Dream", "Remember", "Starlight", "in the Rain", "Reverie", "Ended" });

	private static readonly (string[] Openers, string[] Closers) SoulTitles = (
		new[] { "Since I Met", "That's the Way", "Ain't That", "I'll Be", "Rockin'", "Good Lovin'", "Baby, Don't",
			"Do You", "Come On", "Have Mercy", "Fever for", "Trouble in", "Shake, Rattle and", "Let the Good Times",
			"Money", "Nobody but" },
		new[] { "You", "Love Goes", "a Shame", "Around", "Tonight", "Baby", "Leave Me", "Love Me", "Over Here",
			"Miss Clawdy", "My Baby", "Mind", "Roll", "Roll On", "Honey", "Me" });

	private static readonly (string[] Openers, string[] Closers) CountryTitles = (
		new[] { "Lonesome", "Honky Tonk", "Your Cheatin'", "I Walk", "Wild Side of", "Six Days on",
			"Half as", "Cold, Cold", "There Stands", "Waltz Across", "The Wild Side of", "Faded", "Ramblin'",
			"Big River", "Green, Green Grass of", "Blue Kentucky" },
		new[] { "Whistle", "Angel", "Heart", "the Line", "Life", "the Road", "Much", "Heart Again", "the Glass",
			"Texas", "Love", "Love", "Man", "Rising", "Home", "Girl" });

	private static readonly (string[] Openers, string[] Closers) RockTitles = (
		new[] { "Rock Around", "Long Tall", "Splish", "Runnin'", "Twistin' the", "Summertime", "Wake Up",
			"Whole Lotta", "Great Balls of", "Peggy", "Get a", "Rave", "Party", "Teenage", "Sea of",
			"Somethin' Else" },
		new[] { "the Clock", "Sally", "Splash", "Bear", "Night Away", "Blues", "Little Susie", "Shakin'",
			"Fire", "Sue", "Job", "On", "Doll", "Heaven", "Love", "Tonight" });
}
