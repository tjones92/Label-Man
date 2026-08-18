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
	private static readonly List<ProfessionalSongwriter> professionalWriters = new();
	private static readonly List<MusicPublisher> publishers = new();
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
		professionalWriters.Clear();
		publishers.Clear();
		songCounter = 0;
		rng = new RandomNumberGenerator {
			Seed = seed ^ 0x736f6e6763617461UL // "songcata" -- private stream, isolated from GD
		};
		GeneratePreGameStandards(startYear);
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

	private static string NextSongId() => $"song_{++songCounter:D7}";

	private static void Register(SongComposition song) {
		songs[song.songId] = song;
		if (!catalogByGenre.TryGetValue(song.primaryGenre, out var list)) {
			list = new List<SongComposition>();
			catalogByGenre[song.primaryGenre] = list;
		}
		list.Add(song);
		if (song.isStandard) {
			if (!standardsByGenre.TryGetValue(song.primaryGenre, out var standards)) {
				standards = new List<SongComposition>();
				standardsByGenre[song.primaryGenre] = standards;
			}
			standards.Add(song);
		}
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
