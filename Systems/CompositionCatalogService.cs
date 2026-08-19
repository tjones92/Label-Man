using System;
using System.Collections.Generic;
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
	// Phase 5: per-person songwriting chart-credit ledger (telemetry-only, keyed by personId).
	private static readonly Dictionary<string, WriterCreditLedgerEntry> writerLedger = new(StringComparer.Ordinal);
	private static RandomNumberGenerator rng;
	private static int songCounter;
	private static bool initialized;

	public static bool Initialized => initialized;
	public static int SongCount => songs.Count;
	public static int StandardCount { get { int n = 0; foreach (var kv in standardsByGenre) n += kv.Value.Count; return n; } }

	public static void Initialize(int startYear, IEnumerable<AILabel> labels, ulong seed) {
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
		songCounter = 0;
		rng = new RandomNumberGenerator {
			Seed = seed ^ 0x736f6e6763617461UL // "songcata" -- private stream, isolated from GD
		};
		GeneratePreGameStandards(startYear);
		GeneratePreGameRecentHits(startYear);
		GenerateProfessionalPool(labels, startYear);
		GenerateInitialProfessionalCatalog(startYear);
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
				title = GenerateSongTitle(family),
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
			songs[song.songId] = song;
			RegisterCoverableHit(song);
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
				title = GenerateSongTitle(family),
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
			Register(song);
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
					title = GenerateSongTitle(pub.publisherName),
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
		bool labelOwns = artist.labelOwnsPublishing;
		if (labelOwns) {
			song.rights.controlType = PublishingControlType.LabelAffiliate;
			song.rights.controllerLabelId = label?.labelId;
		} else {
			song.rights.controlType = PublishingControlType.ArtistControlled;
			song.rights.controllerArtistId = artist.artistId;
		}

		Musician writer = artist.GetMainWriter();
		if (writer != null) {
			song.credits.Add(new SongwriterCredit {
				writerType = WriterEntityType.Musician,
				writerId = writer.personId,
				writerName = writer.FullName,
				share = 1f,
				isArtistMember = true
			});
		} else {
			song.credits.Add(new SongwriterCredit {
				writerType = WriterEntityType.HouseCredit,
				writerName = artist.stageName,
				share = 1f
			});
		}

		Register(song);
		ApplyToRecord(record, song, SongMaterialSource.ArtistWritten, isCover: false,
			originalRecordId: null, originalArtistId: null,
			familiarityAtRelease: 0f, arrangementOriginality: record.originality, professionalPolish: 0f);
	}

	/// <summary>Stamps a song's identity + credit snapshot onto a Record. Pure field copy.</summary>
	public static void ApplyToRecord(
		Record record, SongComposition song, SongMaterialSource source, bool isCover,
		string originalRecordId, string originalArtistId,
		float familiarityAtRelease, float arrangementOriginality, float professionalPolish
	) {
		if (record == null || song == null) return;
		record.songId = song.songId;
		record.songSource = source;
		record.isCover = isCover;
		record.originalRecordId = originalRecordId;
		record.originalArtistId = originalArtistId;
		record.publisherId = song.rights.publisherId;
		record.publishingControllerLabelId = song.rights.controllerLabelId;
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
	}

	// ---- Lookups & helpers -------------------------------------------------------------------

	public static SongComposition GetSong(string songId) =>
		!string.IsNullOrEmpty(songId) && songs.TryGetValue(songId, out var song) ? song : null;

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
		if (artist.labelOwnsPublishing) {
			song.rights.controlType = PublishingControlType.LabelAffiliate;
			song.rights.controllerLabelId = label?.labelId;
		} else {
			song.rights.controlType = PublishingControlType.ArtistControlled;
			song.rights.controllerArtistId = artist.artistId;
		}
		Musician writer = artist.GetMainWriter();
		if (writer != null) {
			song.credits.Add(new SongwriterCredit {
				writerType = WriterEntityType.Musician, writerId = writer.personId,
				writerName = writer.FullName, share = 1f, isArtistMember = true
			});
		} else {
			song.credits.Add(new SongwriterCredit {
				writerType = WriterEntityType.HouseCredit, writerName = artist.stageName, share = 1f
			});
		}
		// Song-only registration: an artist-original is reachable by id but is NOT a selectable cover
		// candidate. It enters coverableHitsByGenre only if it charts (Phase 4).
		songs[song.songId] = song;
		return song;
	}

	private static string NextSongId() => $"song_{++songCounter:D7}";

	private static void Register(SongComposition song) {
		songs[song.songId] = song;
		AddToPool(catalogByGenre, song.primaryGenre, song);
		GenreFamily fam = FamilyOf(song.primaryGenre);
		if (song.isStandard) { AddToPool(standardsByGenre, song.primaryGenre, song); AddToPool(standardsByFamily, fam, song); }
		if (song.originKind == SongOriginKind.ProfessionalOffice) AddToPool(professionalByGenre, song.primaryGenre, song);
		if (song.isPublicDomain || song.isTraditional) { AddToPool(traditionalByGenre, song.primaryGenre, song); AddToPool(traditionalByFamily, fam, song); }
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
			led.creditedRuns++;
			if (song.originKind == SongOriginKind.ArtistOriginal) led.originalCredits++;
			if (top40) led.top40Credits++;
			if (peak == 1) led.number1Credits++;
			led.totalUnits += Mathf.Max(0, record.totalUnitsSold);
			led.bestSuccess = Mathf.Max(led.bestSuccess, successScore);
		}
	}

	/// <summary>Per-person accumulation of songwriting chart credits (Phase 5, telemetry-only).</summary>
	public sealed class WriterCreditLedgerEntry {
		public string personId;
		public string name;
		public int creditedRuns;     // completed chart runs of songs this person is credited on
		public int originalCredits;  // of those, artist-original compositions
		public int top40Credits;
		public int number1Credits;
		public long totalUnits;
		public float bestSuccess;
	}

	public static IReadOnlyCollection<WriterCreditLedgerEntry> WriterCreditLedger => writerLedger.Values;

	/// <summary>Marks a charted song as a live cover candidate for its genre (Phase 4 entry point).</summary>
	public static void RegisterCoverableHit(SongComposition song) {
		if (song == null || !song.isCoverable) return;
		if (!coverableHitsByGenre.TryGetValue(song.primaryGenre, out var pool)) {
			pool = new List<SongComposition>(); coverableHitsByGenre[song.primaryGenre] = pool;
		}
		if (!pool.Contains(song)) pool.Add(song);
		AddToPool(coverableHitsByFamily, FamilyOf(song.primaryGenre), song);
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
	private static string GenerateSongTitle(string family) => $"{family} Song {songCounter}";
}
