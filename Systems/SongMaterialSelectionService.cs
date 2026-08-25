using System.Collections.Generic;
using Godot;

/// <summary>
/// The material-source decision (Publishing & Cover-Song Phase 1). For each AI release it chooses
/// among artist-written / professional / cover-standard / cover-recent-hit / traditional material,
/// weighted by the decade transition (self-written rises through the 60s; soul/teen/bubblegum stay
/// manufactured late) and by artist/label/economics. Produces a <see cref="SelectedSongMaterial"/>
/// that the application service stamps onto the Record.
///
/// DETERMINISM: this service draws NOTHING from the global GD stream. Every stochastic choice comes
/// from a pure stable hash over (artistId, recordId, year, salt) -- mirrors ScoutingPerception. If it
/// used GD.RandRange it would shift the whole downstream RNG schedule and the material effect could
/// not be isolated from a full reshuffle. Chart outcomes change (material now shapes hook/originality)
/// but the change is attributable and replay-stable. See SimTools/PublishingCoverSongDirective.md.
/// </summary>
public static class SongMaterialSelectionService {
	public static bool Enabled = true;

	// How many catalog songs to sample per source (bounded cost -- never scan the whole catalog).
	private const int CatalogSampleSize = 6;

	public static SelectedSongMaterial ChooseMaterial(
		AILabel label, SimulatedArtist artist, Record record, Genre genre, int year, int chartWeek,
		SongMaterialSource? forcedSource = null
	) {
		// AlbumMaterialPlan (§15): an album slot may dictate the source (pick the song WITHIN it, don't
		// re-roll the source). Empty pool -> fall through to the normal mix so a slot is never starved.
		if (forcedSource.HasValue) {
			MaterialCandidate forced = BuildForSource(forcedSource.Value, label, artist, record, genre, year, chartWeek);
			if (forced?.Material?.Song != null) return forced.Material;
		}
		var candidates = new List<MaterialCandidate>();

		var artistWritten = BuildArtistWritten(label, artist, record, genre, year);
		candidates.Add(artistWritten);
		AddIfPositive(candidates, BuildProfessional(label, artist, record, genre, year));
		AddIfPositive(candidates, BuildStandardCover(label, artist, record, genre, year));
		AddIfPositive(candidates, BuildRecentHitCover(label, artist, record, genre, year, chartWeek));
		AddIfPositive(candidates, BuildTraditional(label, artist, record, genre, year));

		if (candidates.Count == 0) return artistWritten.Material;
		return WeightedPick(candidates, artist, record, year);
	}

	private static void AddIfPositive(List<MaterialCandidate> list, MaterialCandidate c) {
		if (c != null && c.Material?.Song != null && c.Score > 0f) list.Add(c);
	}

	/// <summary>
	/// Builds cover material for one <b>specific</b> song, rather than sampling the source's pool. This
	/// is the player path: an act cuts a particular number out of the set the player heard it play. The
	/// source is inferred from the song (standard / recent hit / traditional) and the same interpretation
	/// and arrangement helpers the AI uses shape the read, so a player cover and an AI cover are the same
	/// object to the chart and the settlement.
	/// </summary>
	public static SelectedSongMaterial BuildCoverForSong(
		SimulatedArtist artist, Record record, SongComposition song, Genre genre, int year
	) {
		if (song == null) return null;
		float familiarity = song.GetFamiliarityForYear(year);
		float artistFit = InterpretationFit(artist, song, genre);
		SongMaterialSource source =
			song.isPublicDomain ? SongMaterialSource.TraditionalPublicDomain
			: song.isTraditional ? SongMaterialSource.AdaptedTraditional
			: song.isStandard ? SongMaterialSource.CoverStandard
			: SongMaterialSource.CoverRecentHit;
		int last = song.recordings.Count - 1;
		return new SelectedSongMaterial {
			Song = song, Source = source, IsCover = true,
			OriginalRecordId = last >= 0 ? song.recordings[last].recordId : null,
			OriginalArtistId = last >= 0 ? song.recordings[last].artistId : null,
			ExpectedHook = Mathf.Clamp(song.commercialHook * 0.8f + artistFit * 0.15f, 0f, 1f),
			ExpectedCompositionQuality = song.compositionQuality, ExpectedLyricQuality = song.lyricQuality,
			FamiliarityAtRelease = familiarity,
			ArrangementOriginality = ArrangementOriginality(artist, record, year, "playercover"),
			ProfessionalPolish = 0f, ArtistIdentityFit = Mathf.Clamp(artistFit * 0.6f, 0f, 1f)
		};
	}

	/// <summary>Build professional material for a SPECIFIC pre-commissioned song, so a song the player
	/// commissioned and had delivered records as the professional song it is -- not re-sampled blind at the
	/// studio. Mirrors <see cref="BuildProfessional"/>'s expectations for a fixed composition. Pure: no GD
	/// draw, so it is safe to call from the player desk without perturbing the AI RNG schedule.</summary>
	public static SelectedSongMaterial BuildProfessionalForSong(
		LabelTier tier, SimulatedArtist artist, Record record, SongComposition song, Genre genre, int year
	) {
		if (song == null) return null;
		float labelAccess = tier switch {
			LabelTier.Major => 0.90f, LabelTier.MidTier => 0.65f, LabelTier.Boutique => 0.50f,
			LabelTier.Independent => 0.38f, LabelTier.Small => 0.25f, _ => 0.35f
		};
		float polish = Mathf.Clamp(song.compositionQuality * 0.7f + labelAccess * 0.3f, 0f, 1f);
		float hook = Mathf.Clamp(song.commercialHook * 0.85f + polish * 0.10f, 0f, 1f);
		return new SelectedSongMaterial {
			Song = song, Source = SongMaterialSource.ExternalProfessional, IsCover = false,
			ExpectedHook = hook, ExpectedCompositionQuality = song.compositionQuality, ExpectedLyricQuality = song.lyricQuality,
			FamiliarityAtRelease = 0f, ArrangementOriginality = Mathf.Clamp(song.originality * 0.65f, 0f, 1f),
			ProfessionalPolish = polish, ArtistIdentityFit = 0.30f
		};
	}

	// Build only the candidate for one dictated source (AlbumMaterialPlan forced-source path).
	private static MaterialCandidate BuildForSource(
		SongMaterialSource src, AILabel label, SimulatedArtist artist, Record record, Genre genre, int year, int chartWeek
	) => src switch {
		SongMaterialSource.ArtistWritten => BuildArtistWritten(label, artist, record, genre, year),
		SongMaterialSource.ExternalProfessional or SongMaterialSource.LabelStaffWriter
			or SongMaterialSource.ArtistCowrittenWithProfessional => BuildProfessional(label, artist, record, genre, year),
		SongMaterialSource.CoverStandard or SongMaterialSource.CoverCatalogSong => BuildStandardCover(label, artist, record, genre, year),
		SongMaterialSource.CoverRecentHit => BuildRecentHitCover(label, artist, record, genre, year, chartWeek),
		SongMaterialSource.TraditionalPublicDomain or SongMaterialSource.AdaptedTraditional => BuildTraditional(label, artist, record, genre, year),
		_ => null
	};

	/// <summary>The calibrated source-mix shares for a genre/year (Anchor prior, normalized), exposed so
	/// AlbumMaterialPlan builds an album's whole-LP plan from the same decade curve the singles follow.</summary>
	public static SourceShares GetSourceMixShares(Genre genre, int year) {
		float aw = SourceMix(SongMaterialSource.ArtistWritten, genre, year);
		float pro = SourceMix(SongMaterialSource.ExternalProfessional, genre, year);
		float std = SourceMix(SongMaterialSource.CoverStandard, genre, year);
		float hit = SourceMix(SongMaterialSource.CoverRecentHit, genre, year);
		float trad = SourceMix(SongMaterialSource.TraditionalPublicDomain, genre, year);
		float sum = aw + pro + std + hit + trad;
		if (sum <= 0f) return new SourceShares { Aw = 1f };
		return new SourceShares { Aw = aw / sum, Pro = pro / sum, Std = std / sum, Hit = hit / sum, Trad = trad / sum };
	}

	// ---- Candidate builders ------------------------------------------------------------------

	private static MaterialCandidate BuildArtistWritten(
		AILabel label, SimulatedArtist artist, Record record, Genre genre, int year
	) {
		float eraWeight = GetArtistWrittenEraWeight(genre, year);
		float writerCraft = ArtistWriterCraft(artist);
		float noise = (StableUnit(artist.artistId, record.recordId, year, "artwrite") - 0.5f) * 0.10f;
		float compositionQuality = Mathf.Clamp(writerCraft * 0.80f + eraWeight * 0.10f + noise, 0f, 1f);
		float hook = Mathf.Clamp(compositionQuality * 0.75f + record.hookStrength * 0.15f, 0f, 1f);
		float lyric = Mathf.Clamp(writerCraft * 0.70f + eraWeight * 0.10f, 0f, 1f);
		float originality = Mathf.Clamp(artist.members.Count > 0 ? artist.members[0].creativity * 0.6f + 0.2f : 0.4f, 0f, 1f);

		SongComposition song = CompositionCatalogService.CreateArtistOriginal(
			record, artist, label, genre, year, compositionQuality, hook, lyric, originality);

		var material = new SelectedSongMaterial {
			Song = song, Source = SongMaterialSource.ArtistWritten, IsCover = false,
			ExpectedHook = hook, ExpectedCompositionQuality = compositionQuality, ExpectedLyricQuality = lyric,
			FamiliarityAtRelease = 0f, ArrangementOriginality = originality, ProfessionalPolish = 0f,
			ArtistIdentityFit = Mathf.Clamp(0.45f + writerCraft * 0.40f + eraWeight * 0.15f, 0f, 1f)
		};
		// Source-mix prior x craft. Ambitious/roots acts tilt toward self-writing via the external
		// penalty on the other candidates, and a self-contained band writes more competitively.
		float score = SourceMix(SongMaterialSource.ArtistWritten, genre, year) * (0.55f + 0.45f * writerCraft);
		return new MaterialCandidate { Material = material, Score = Mathf.Max(score, 0.001f) };
	}

	private static MaterialCandidate BuildProfessional(
		AILabel label, SimulatedArtist artist, Record record, Genre genre, int year
	) {
		float availability = GetProfessionalMaterialAvailability(genre, year);
		if (availability <= 0.01f) return null;
		SongComposition song = SampleBest(CompositionCatalogService.GetProfessionalForGenre(genre),
			artist, record, year, "pro", null);
		if (song == null) return null;

		float labelAccess = label.tier switch {
			LabelTier.Major => 0.90f, LabelTier.MidTier => 0.65f, LabelTier.Boutique => 0.50f,
			LabelTier.Independent => 0.38f, LabelTier.Small => 0.25f, _ => 0.35f
		};
		float artistNeedsSong = Mathf.Clamp(1f - artist.songwritingAbility, 0f, 1f);
		float commercialNeed = CommercialNeed(artist.careerState);
		float polish = Mathf.Clamp(song.compositionQuality * 0.7f + labelAccess * 0.3f, 0f, 1f);
		float hook = Mathf.Clamp(song.commercialHook * 0.85f + polish * 0.10f, 0f, 1f);

		// Better labels/needier acts get slightly more of the professional share; ambitious acts less.
		float q = Mathf.Clamp(song.commercialHook * 0.5f + song.compositionQuality * 0.2f + labelAccess * 0.15f
			+ artistNeedsSong * 0.1f + commercialNeed * 0.05f, 0f, 1f);
		float score = SourceMix(SongMaterialSource.ExternalProfessional, genre, year)
			* (0.55f + 0.45f * q) * (1f - ExternalPenalty(artist));

		var material = new SelectedSongMaterial {
			Song = song,
			Source = SongMaterialSource.ExternalProfessional, IsCover = false,
			ExpectedHook = hook, ExpectedCompositionQuality = song.compositionQuality, ExpectedLyricQuality = song.lyricQuality,
			FamiliarityAtRelease = 0f, ArrangementOriginality = Mathf.Clamp(song.originality * 0.65f, 0f, 1f),
			ProfessionalPolish = polish, ArtistIdentityFit = 0.30f
		};
		return new MaterialCandidate { Material = material, Score = score };
	}

	private static MaterialCandidate BuildStandardCover(
		AILabel label, SimulatedArtist artist, Record record, Genre genre, int year
	) {
		float pref = GetCoverStandardPreference(genre, year);
		if (pref <= 0.01f) return null;
		SongComposition song = SampleBestAcross(artist, record, year, "std",
			StandardPoolsFor(genre));
		if (song == null) return null;

		float familiarity = song.GetFamiliarityForYear(year);
		float artistFit = InterpretationFit(artist, song, genre);
		float q = Mathf.Clamp(song.compositionQuality * 0.4f + song.standardDurability * 0.25f
			+ familiarity * 0.2f + artistFit * 0.15f, 0f, 1f);
		float score = SourceMix(SongMaterialSource.CoverStandard, genre, year) * (0.55f + 0.45f * q);

		var material = new SelectedSongMaterial {
			Song = song, Source = SongMaterialSource.CoverStandard, IsCover = true,
			ExpectedHook = Mathf.Clamp(song.commercialHook * 0.8f + artistFit * 0.15f, 0f, 1f),
			ExpectedCompositionQuality = song.compositionQuality, ExpectedLyricQuality = song.lyricQuality,
			FamiliarityAtRelease = familiarity,
			ArrangementOriginality = ArrangementOriginality(artist, record, year, "stdarr"),
			ProfessionalPolish = 0f, ArtistIdentityFit = Mathf.Clamp(artistFit * 0.6f, 0f, 1f)
		};
		return new MaterialCandidate { Material = material, Score = score };
	}

	private static MaterialCandidate BuildRecentHitCover(
		AILabel label, SimulatedArtist artist, Record record, Genre genre, int year, int chartWeek
	) {
		// Coverable hits: pre-game recent hits (1955-59) plus in-game hits (Phase 4), across adjacent
		// families so a rock act can cover a recent R&B hit.
		SongComposition song = SampleBestAcross(artist, record, year, "hit", HitPoolsFor(genre), applyFatigue: true);
		if (song == null) return null;

		float familiarity = song.GetFamiliarityForYear(year);
		float artistFit = InterpretationFit(artist, song, genre);
		float q = Mathf.Clamp(song.commercialHook * 0.45f + familiarity * 0.35f + artistFit * 0.2f, 0f, 1f);
		// Cover fatigue + definitive-version shadow are applied INSIDE the sampler (which hit gets covered),
		// NOT here -- the source-mix prior must still govern the recent-hit-cover SHARE, or the whole bucket
		// collapses as pre-game hits accumulate recordings. Fatigue only spreads covers off worn / definitive
		// songs and damps the 3rd/4th cover of a given one.
		float score = SourceMix(SongMaterialSource.CoverRecentHit, genre, year)
			* (0.55f + 0.45f * q) * (1f - ExternalPenalty(artist));

		var material = new SelectedSongMaterial {
			Song = song, Source = SongMaterialSource.CoverRecentHit, IsCover = true,
			OriginalRecordId = song.recordings.Count > 0 ? song.recordings[song.recordings.Count - 1].recordId : null,
			OriginalArtistId = song.recordings.Count > 0 ? song.recordings[song.recordings.Count - 1].artistId : null,
			ExpectedHook = Mathf.Clamp(song.commercialHook * 0.85f + artistFit * 0.10f, 0f, 1f),
			ExpectedCompositionQuality = song.compositionQuality, ExpectedLyricQuality = song.lyricQuality,
			FamiliarityAtRelease = familiarity,
			ArrangementOriginality = ArrangementOriginality(artist, record, year, "hitarr"),
			ProfessionalPolish = 0f, ArtistIdentityFit = Mathf.Clamp(artistFit * 0.5f, 0f, 1f)
		};
		return new MaterialCandidate { Material = material, Score = score };
	}

	private static MaterialCandidate BuildTraditional(
		AILabel label, SimulatedArtist artist, Record record, Genre genre, int year
	) {
		SongComposition song = SampleBestAcross(artist, record, year, "trad",
			TraditionalPoolsFor(genre));
		if (song == null) return null;

		float artistFit = InterpretationFit(artist, song, genre);
		bool pd = song.isPublicDomain;
		float q = Mathf.Clamp(song.compositionQuality * 0.4f + song.standardDurability * 0.2f + artistFit * 0.25f, 0f, 1f);
		float score = SourceMix(pd ? SongMaterialSource.TraditionalPublicDomain : SongMaterialSource.AdaptedTraditional, genre, year)
			* (0.55f + 0.45f * q);
		var material = new SelectedSongMaterial {
			Song = song, IsCover = true,
			Source = pd ? SongMaterialSource.TraditionalPublicDomain : SongMaterialSource.AdaptedTraditional,
			ExpectedHook = Mathf.Clamp(song.commercialHook * 0.75f + artistFit * 0.15f, 0f, 1f),
			ExpectedCompositionQuality = song.compositionQuality, ExpectedLyricQuality = song.lyricQuality,
			FamiliarityAtRelease = song.GetFamiliarityForYear(year),
			ArrangementOriginality = ArrangementOriginality(artist, record, year, "tradarr"),
			ProfessionalPolish = 0f, ArtistIdentityFit = Mathf.Clamp(artistFit * 0.55f, 0f, 1f)
		};
		return new MaterialCandidate { Material = material, Score = score };
	}

	// ---- Era / genre weight functions (the decade transition) --------------------------------

	private static float SmoothYear(float start, float end, int year) {
		float t = Mathf.Clamp((year - start) / Mathf.Max(0.001f, end - start), 0f, 1f);
		return t * t * (3f - 2f * t);
	}

	public static float GetArtistWrittenEraWeight(Genre genre, int year) {
		float rise = SmoothYear(1962f, 1968f, year);
		float genreMultiplier = genre switch {
			Genre.Folk or Genre.FolkRock or Genre.SingerSongwriter or Genre.ContemporaryFolk => 1.15f,
			Genre.RockAndRoll or Genre.GarageRock or Genre.PsychedelicRock or Genre.Psychedelic
				or Genre.AcidRock or Genre.BluesRock => 1.05f,
			Genre.Country => 0.55f,
			Genre.Soul or Genre.Motown or Genre.RnB => 0.45f,
			Genre.TeenPop or Genre.TraditionalPop or Genre.EasyListening or Genre.GirlGroup or Genre.Bubblegum => 0.25f,
			_ => 0.50f
		};
		return Mathf.Clamp((0.18f + 0.62f * rise) * genreMultiplier, 0f, 1f);
	}

	public static float GetProfessionalMaterialAvailability(Genre genre, int year) {
		float brillPeak = Mathf.Exp(-Mathf.Pow((year - 1963.0f) / 3.2f, 2f));
		float lateBubblegum = SmoothYear(1966f, 1969f, year) * 0.35f;
		return genre switch {
			Genre.TeenPop or Genre.GirlGroup => Mathf.Clamp(0.45f + 0.42f * brillPeak + lateBubblegum, 0f, 1f),
			Genre.Bubblegum => Mathf.Clamp(0.40f + 0.30f * brillPeak + lateBubblegum, 0f, 1f),
			Genre.TraditionalPop or Genre.EasyListening => Mathf.Clamp(0.72f - 0.20f * SmoothYear(1964f, 1969f, year), 0f, 1f),
			Genre.Soul or Genre.Motown => Mathf.Clamp(0.45f + 0.25f * SmoothYear(1960f, 1966f, year), 0f, 1f),
			Genre.Country => 0.70f,
			Genre.RnB => 0.45f,
			Genre.RockAndRoll or Genre.GarageRock or Genre.PsychedelicRock or Genre.Psychedelic =>
				Mathf.Clamp(0.28f - 0.12f * SmoothYear(1964f, 1968f, year), 0f, 1f),
			_ => 0.35f
		};
	}

	public static float GetCoverStandardPreference(Genre genre, int year) {
		float decline = SmoothYear(1960f, 1967f, year);
		return genre switch {
			Genre.TraditionalPop or Genre.EasyListening or Genre.Jazz => Mathf.Clamp(0.75f - 0.20f * decline, 0f, 1f),
			Genre.Country or Genre.Gospel or Genre.Folk or Genre.Blues => Mathf.Clamp(0.55f - 0.10f * decline, 0f, 1f),
			Genre.RnB or Genre.Soul => Mathf.Clamp(0.35f - 0.12f * decline, 0f, 1f),
			Genre.RockAndRoll or Genre.BluesRock => Mathf.Clamp(0.38f - 0.08f * decline, 0f, 1f),
			_ => Mathf.Clamp(0.25f - 0.10f * decline, 0f, 1f)
		};
	}

	// ---- Source-mix prior (the calibratable decade transition) -------------------------------
	// Target share of each source, per genre, interpolated 1960->1969. Candidate scores are this
	// weight times a mild quality factor, so the realized mix tracks these targets (modulated by
	// catalog availability -- an empty pool redistributes its share to the others). Order:
	// {ArtistWritten, Professional/staff, CoverStandard, CoverRecentHit, Traditional/PD}.
	// 1960 design targets (genre-averaged): AW .22, Pro .30, Std .20, Hit .18, Trad .10; TeenPop skews
	// majority-staff; folk/jazz carry more self-written and traditional; rock covers recent R&B/blues.
	private struct Mix { public float Aw, Pro, Std, Hit, Trad; public Mix(float a,float p,float s,float h,float t){Aw=a;Pro=p;Std=s;Hit=h;Trad=t;} }

	private static Mix Anchor1960(Genre g) => g switch {
		Genre.TeenPop        => new Mix(.06f, .56f, .14f, .14f, .10f),
		Genre.Bubblegum      => new Mix(.06f, .56f, .14f, .14f, .10f),
		Genre.GirlGroup      => new Mix(.08f, .54f, .14f, .14f, .10f),
		Genre.TraditionalPop => new Mix(.10f, .28f, .40f, .10f, .12f),
		Genre.EasyListening  => new Mix(.10f, .28f, .40f, .08f, .14f),
		Genre.Jazz           => new Mix(.30f, .14f, .40f, .06f, .10f),
		Genre.Gospel         => new Mix(.15f, .16f, .26f, .06f, .37f),
		Genre.Folk           => new Mix(.34f, .06f, .18f, .10f, .32f),
		Genre.Country        => new Mix(.22f, .36f, .22f, .10f, .10f),
		_ => GenreCatalog.TryGet(g, out var p) ? p.Family switch {
			GenreFamily.Rock          => new Mix(.32f, .05f, .18f, .27f, .18f),
			GenreFamily.RhythmAndSoul => new Mix(.18f, .40f, .16f, .16f, .10f),
			GenreFamily.Blues         => new Mix(.30f, .08f, .20f, .22f, .20f),
			GenreFamily.Pop           => new Mix(.12f, .42f, .24f, .12f, .10f),
			GenreFamily.Country       => new Mix(.22f, .36f, .22f, .10f, .10f),
			GenreFamily.Folk          => new Mix(.34f, .06f, .18f, .10f, .32f),
			GenreFamily.Jazz          => new Mix(.30f, .14f, .40f, .06f, .10f),
			GenreFamily.Gospel        => new Mix(.15f, .16f, .26f, .06f, .37f),
			_                         => new Mix(.20f, .30f, .20f, .18f, .12f)
		} : new Mix(.20f, .30f, .20f, .18f, .12f)
	};

	private static Mix Anchor1969(Genre g) => g switch {
		Genre.TeenPop        => new Mix(.20f, .55f, .04f, .16f, .05f),
		Genre.Bubblegum      => new Mix(.15f, .60f, .04f, .16f, .05f),
		Genre.GirlGroup      => new Mix(.25f, .45f, .06f, .18f, .06f),
		Genre.TraditionalPop => new Mix(.20f, .28f, .34f, .08f, .10f),
		Genre.EasyListening  => new Mix(.18f, .28f, .38f, .06f, .10f),
		Genre.Jazz           => new Mix(.45f, .12f, .30f, .06f, .07f),
		Genre.Gospel         => new Mix(.35f, .20f, .15f, .08f, .22f),
		Genre.Folk           => new Mix(.68f, .05f, .06f, .09f, .12f),
		Genre.Country        => new Mix(.45f, .35f, .10f, .06f, .04f),
		_ => GenreCatalog.TryGet(g, out var p) ? p.Family switch {
			GenreFamily.Rock          => new Mix(.76f, .05f, .04f, .12f, .03f),
			GenreFamily.RhythmAndSoul => new Mix(.42f, .38f, .06f, .10f, .04f),
			GenreFamily.Blues         => new Mix(.55f, .08f, .10f, .17f, .10f),
			GenreFamily.Pop           => new Mix(.30f, .40f, .10f, .14f, .06f),
			GenreFamily.Country       => new Mix(.45f, .35f, .10f, .06f, .04f),
			GenreFamily.Folk          => new Mix(.68f, .05f, .06f, .09f, .12f),
			GenreFamily.Jazz          => new Mix(.45f, .12f, .30f, .06f, .07f),
			GenreFamily.Gospel        => new Mix(.35f, .20f, .15f, .08f, .22f),
			_                         => new Mix(.50f, .25f, .10f, .10f, .05f)
		} : new Mix(.50f, .25f, .10f, .10f, .05f)
	};

	// Standards (the pre-war songbook -- distinct from covers of recent hits) are a roots-genre habit.
	// Jazz, classical, gospel, folk and the Tin Pan Alley pop that IS the songbook keep leaning on them;
	// the contemporary genres -- r&b and soul most of all -- were original- and recent-hit-driven and
	// should reach for standards far less. This scales the CoverStandard share down for everyone outside
	// the songbook genres (recent-hit COVERS are untouched); the freed share flows to self-written and
	// recent-hit material through the normalization in GetSourceMixShares.
	private static float StandardShareFactor(Genre genre) {
		switch (genre) {
			case Genre.Jazz: case Genre.Classical: case Genre.Gospel: case Genre.Folk:
			case Genre.TraditionalPop: case Genre.EasyListening:
				return 1f;                       // the songbook genres -- standards belong here
			case Genre.RnB: case Genre.Soul:
				return 0.15f;                    // heavily reduced -- these were not a standards market
		}
		if (GenreCatalog.TryGet(genre, out var p)) {
			switch (p.Family) {
				case GenreFamily.Jazz: case GenreFamily.Classical:
				case GenreFamily.Gospel: case GenreFamily.Folk:
					return 1f;
				case GenreFamily.RhythmAndSoul:
					return 0.15f;
			}
		}
		return 0.40f;                            // rock, pop, country, blues, teen pop -- far less standard-driven
	}

	// The transition mostly runs 1962-1968 (Brill decline, self-writing rise).
	private static float SourceMix(SongMaterialSource source, Genre genre, int year) {
		Mix a = Anchor1960(genre), b = Anchor1969(genre);
		float t = SmoothYear(1962f, 1968f, year);
		float Lerp(float x, float y) => Mathf.Lerp(x, y, t);
		return source switch {
			SongMaterialSource.ArtistWritten => Lerp(a.Aw, b.Aw),
			SongMaterialSource.ExternalProfessional or SongMaterialSource.LabelStaffWriter or SongMaterialSource.ArtistCowrittenWithProfessional => Lerp(a.Pro, b.Pro),
			SongMaterialSource.CoverStandard or SongMaterialSource.CoverCatalogSong => Lerp(a.Std, b.Std) * StandardShareFactor(genre),
			SongMaterialSource.CoverRecentHit => Lerp(a.Hit, b.Hit),
			SongMaterialSource.TraditionalPublicDomain or SongMaterialSource.AdaptedTraditional => Lerp(a.Trad, b.Trad),
			_ => 0f
		};
	}

	// ---- Scoring helpers ---------------------------------------------------------------------

	private static float ArtistWriterCraft(SimulatedArtist artist) {
		float best = 0f;
		if (artist.members != null) foreach (var m in artist.members) if (m.creativity > best) best = m.creativity;
		return Mathf.Clamp(artist.songwritingAbility * 0.6f + best * 0.4f, 0f, 1f);
	}

	private static float CommercialNeed(CareerState state) => state switch {
		CareerState.NewSigning => 0.65f, CareerState.Rising => 0.55f, CareerState.Established => 0.35f,
		CareerState.Declining => 0.70f, CareerState.Star or CareerState.Superstar => 0.25f, _ => 0.45f
	};

	// Ambitious / roots-attached self-contained acts resent external material.
	private static float ExternalPenalty(SimulatedArtist artist) {
		if (artist.evolution == null) return 0f;
		return Mathf.Clamp(artist.evolution.artisticAmbition * 0.08f + artist.evolution.rootsAttachment * 0.04f, 0f, 0.4f);
	}

	// Damps a recent-hit cover candidate by how worn the song is: each remembered recording adds
	// fatigue, and the most definitive prior version casts a shadow. Returns ~1.0 for a fresh song and
	// falls toward ~0.3 for a much-covered / definitively-owned one. Pure read of the song's memory.
	private static float CoverFatigueShadow(SongComposition song) {
		int worn = song.recordings?.Count ?? 0;
		float fatigue = 1f / (1f + 0.5f * worn);
		float bestDefinitive = 0f;
		if (song.recordings != null)
			foreach (var rec in song.recordings) if (rec.definitiveVersionScore > bestDefinitive) bestDefinitive = rec.definitiveVersionScore;
		float shadow = 1f - 0.4f * bestDefinitive;
		return Mathf.Clamp(fatigue * shadow, 0.1f, 1f);
	}

	private static float GenreFit(SongComposition song, Genre genre) =>
		song.primaryGenre == genre ? 1f : song.secondaryGenre == genre ? 0.6f : 0.3f;

	private static float InterpretationFit(SimulatedArtist artist, SongComposition song, Genre genre) =>
		Mathf.Clamp(artist.CalculateBaseQuality() * 0.55f + GenreFit(song, genre) * 0.30f + song.adaptability * 0.15f, 0f, 1f);

	private static float ArrangementOriginality(SimulatedArtist artist, Record record, int year, string salt) {
		float baseVal = record.originality * 0.5f + (artist.members.Count > 0 ? artist.members[0].creativity * 0.3f : 0.15f);
		float noise = (StableUnit(artist.artistId, record.recordId, year, salt) - 0.5f) * 0.2f;
		return Mathf.Clamp(baseVal + noise, 0f, 1f);
	}

	// ---- Cross-genre cover sourcing ----------------------------------------------------------
	// Which families a genre can draw cover/standard/traditional material from. Early rock covered
	// R&B/blues; soul covered gospel/blues; pop drew the Tin Pan Alley / jazz songbook; etc. There are
	// no RockAndRoll-primary standards, so without this a rock act is starved into all-self-written.
	private static readonly GenreFamily[] RockSrc = { GenreFamily.Rock, GenreFamily.Blues, GenreFamily.RhythmAndSoul };
	private static readonly GenreFamily[] SoulSrc = { GenreFamily.RhythmAndSoul, GenreFamily.Blues, GenreFamily.Gospel, GenreFamily.Pop };
	private static readonly GenreFamily[] PopSrc = { GenreFamily.Pop, GenreFamily.Jazz };
	private static readonly GenreFamily[] CountrySrc = { GenreFamily.Country, GenreFamily.Folk, GenreFamily.Pop };
	private static readonly GenreFamily[] FolkSrc = { GenreFamily.Folk, GenreFamily.Country, GenreFamily.Blues };
	private static readonly GenreFamily[] BluesSrc = { GenreFamily.Blues, GenreFamily.RhythmAndSoul };
	private static readonly GenreFamily[] JazzSrc = { GenreFamily.Jazz, GenreFamily.Pop };
	private static readonly GenreFamily[] GospelSrc = { GenreFamily.Gospel, GenreFamily.RhythmAndSoul };

	private static GenreFamily[] CoverSourceFamilies(Genre genre) {
		GenreFamily fam = GenreCatalog.TryGet(genre, out var p) ? p.Family : GenreFamily.Pop;
		return fam switch {
			GenreFamily.Rock => RockSrc,
			GenreFamily.RhythmAndSoul => SoulSrc,
			GenreFamily.Pop => PopSrc,
			GenreFamily.Country => CountrySrc,
			GenreFamily.Folk => FolkSrc,
			GenreFamily.Blues => BluesSrc,
			GenreFamily.Jazz => JazzSrc,
			GenreFamily.Gospel => GospelSrc,
			_ => PopSrc
		};
	}

	private static IReadOnlyList<SongComposition>[] StandardPoolsFor(Genre genre) {
		var fams = CoverSourceFamilies(genre);
		var pools = new IReadOnlyList<SongComposition>[fams.Length];
		for (int i = 0; i < fams.Length; i++) pools[i] = CompositionCatalogService.GetStandardsForFamily(fams[i]);
		return pools;
	}

	private static IReadOnlyList<SongComposition>[] TraditionalPoolsFor(Genre genre) {
		var fams = CoverSourceFamilies(genre);
		var pools = new IReadOnlyList<SongComposition>[fams.Length];
		for (int i = 0; i < fams.Length; i++) pools[i] = CompositionCatalogService.GetTraditionalForFamily(fams[i]);
		return pools;
	}

	private static IReadOnlyList<SongComposition>[] HitPoolsFor(Genre genre) {
		var fams = CoverSourceFamilies(genre);
		var pools = new IReadOnlyList<SongComposition>[fams.Length];
		for (int i = 0; i < fams.Length; i++) pools[i] = CompositionCatalogService.GetCoverableHitsForFamily(fams[i]);
		return pools;
	}

	// Deterministically sample across several pools (a genre's adjacent-family cover sources) and
	// return the best-scoring song -- bounded cost, never a full-catalog scan.
	private static SongComposition SampleBestAcross(
		SimulatedArtist artist, Record record, int year, string salt, IReadOnlyList<SongComposition>[] pools,
		bool applyFatigue = false
	) {
		int total = 0;
		foreach (var p in pools) total += p?.Count ?? 0;
		if (total == 0) return null;
		SongComposition best = null;
		float bestScore = -1f;
		int take = Mathf.Min(CatalogSampleSize, total);
		for (int i = 0; i < take; i++) {
			float u = StableUnit(artist.artistId, record.recordId, year, $"{salt}{i}");
			int gidx = (int)(u * total) % total;
			SongComposition s = ResolveGlobal(pools, gidx);
			if (s == null) continue;
			float score = s.GetCraftScore() + s.GetFamiliarityForYear(year) * 0.2f;
			// Recent-hit covers: prefer the less-worn song so covers spread across many hits and the
			// 3rd/4th cover of one hit (or a definitive #1) is avoided -- without cutting the bucket share.
			if (applyFatigue) score *= CoverFatigueShadow(s);
			if (score > bestScore) { bestScore = score; best = s; }
		}
		return best;
	}

	private static SongComposition ResolveGlobal(IReadOnlyList<SongComposition>[] pools, int gidx) {
		foreach (var p in pools) {
			int c = p?.Count ?? 0;
			if (gidx < c) return p[gidx];
			gidx -= c;
		}
		return null;
	}

	// Deterministically sample up to CatalogSampleSize songs from a list and return the best-scoring
	// by craft*fit -- bounded cost, never a full-catalog scan.
	private static SongComposition SampleBest(
		IReadOnlyList<SongComposition> pool, SimulatedArtist artist, Record record, int year,
		string salt, System.Func<SongComposition, bool> filter
	) {
		if (pool == null || pool.Count == 0) return null;
		SongComposition best = null;
		float bestScore = -1f;
		int take = Mathf.Min(CatalogSampleSize, pool.Count);
		for (int i = 0; i < take; i++) {
			float u = StableUnit(artist.artistId, record.recordId, year, $"{salt}{i}");
			int idx = (int)(u * pool.Count) % pool.Count;
			SongComposition s = pool[idx];
			if (filter != null && !filter(s)) continue;
			float score = s.GetCraftScore() + s.GetFamiliarityForYear(year) * 0.2f;
			if (score > bestScore) { bestScore = score; best = s; }
		}
		return best;
	}

	// ---- Deterministic weighted pick ---------------------------------------------------------

	private static SelectedSongMaterial WeightedPick(
		List<MaterialCandidate> candidates, SimulatedArtist artist, Record record, int year
	) {
		float total = 0f;
		foreach (var c in candidates) total += c.Score;
		if (total <= 0f) return candidates[0].Material;
		float roll = StableUnit(artist.artistId, record.recordId, year, "pick") * total;
		float acc = 0f;
		foreach (var c in candidates) {
			acc += c.Score;
			if (roll <= acc) return c.Material;
		}
		return candidates[candidates.Count - 1].Material;
	}

	// FNV-1a over (artist, record, year, salt) folded to [0,1). Same style as ScoutingPerception;
	// the record id makes each release an independent draw, so successive releases can diverge.
	private static float StableUnit(string artistId, string recordId, int year, string salt) {
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		ulong hash = offset;
		foreach (char value in $"{artistId}|{recordId}|{year}|{salt}|SongMaterialV1") { hash ^= value; hash *= prime; }
		return (hash >> 40) * (1f / 16777216f);
	}
}

/// <summary>Normalized source-mix shares {artist-written, professional, standard, recent-hit, traditional}
/// for a genre/year -- the calibrated decade prior, shared by singles selection and AlbumMaterialPlan.</summary>
public struct SourceShares {
	public float Aw, Pro, Std, Hit, Trad;
}

/// <summary>The outcome of a material decision: which song, from what source, with expected traits.</summary>
public sealed class SelectedSongMaterial {
	public SongComposition Song;
	public SongMaterialSource Source;
	public bool IsCover;
	public string OriginalRecordId;
	public string OriginalArtistId;
	public float ExpectedHook;
	public float ExpectedCompositionQuality;
	public float ExpectedLyricQuality;
	public float FamiliarityAtRelease;
	public float ArrangementOriginality;
	public float ProfessionalPolish;
	public float ArtistIdentityFit;
}

internal sealed class MaterialCandidate {
	public SelectedSongMaterial Material;
	public float Score;
}
