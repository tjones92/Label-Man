using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The trade-paper voice on chart cards, record cards, act files and label files. This class only gathers what the
/// simulation actually holds about the subject (chart run, the record's traits, its song's writers and taxonomy, the
/// title's content tags, the act's lineup and history, the label's tier and hits) into fact objects; the wording lives
/// in TradePressPhrases, where every sentence is gated on the fact it names.
///
/// Opening a card is a UI act, so nothing here draws from a game stream: wording is a hash of the subject (and, for
/// records, the week), so a card reads the same way if it is opened twice in a week and never moves the simulation.
/// </summary>
public static class JournalisticDescriptor {

	// === RECORDS ===

	/// <summary>
	/// The record's blurb. <paramref name="listName"/> and <paramref name="listRank"/> place the record on a genre or regional
	/// list instead of the Hot 100: the bullet, anchor, run length and peak are Hot 100 facts, and a country record at #1 on
	/// its own list can be #2 on the Hot 100 or off it altogether (position 0).
	/// </summary>
	public static string DescribeRecord(RecordRuntimeData record, string listName = null, int listRank = 0) =>
		record?.baseRecord == null ? "" : TradePressPhrases.DescribeRecord(FactsFor(record, listName, listRank));

	internal static RecordFacts FactsFor(RecordRuntimeData record, string listName = null, int listRank = 0) {
		var r = record.baseRecord;
		SimulatedArtist ArtistOf(string id) => string.IsNullOrEmpty(id) ? null : ArtistManager.Instance?.GetArtist(id);
		var artist = ArtistOf(r.artistId);
		var song = CompositionCatalogService.GetSong(r.songId);
		var taxonomy = PolarSongMetadataService.Get(r.PlugMasterId)?.taxonomy ?? PolarSongMetadataService.Get(r.masterId)?.taxonomy;
		var source = r.songSource;
		bool cover = r.isCover || source is SongMaterialSource.CoverRecentHit or SongMaterialSource.CoverCatalogSong;
		var writers = new List<string>();
		for (int i = 0; i < (r.songwriterNames?.Length ?? 0); i++) {
			var type = i < (r.songwriterTypes?.Length ?? 0) ? r.songwriterTypes[i] : WriterEntityType.Unknown;
			if (type is WriterEntityType.Traditional or WriterEntityType.PublicDomain or WriterEntityType.HouseCredit) continue;
			string name = r.songwriterNames[i];
			// Publishing offices credit unnamed staff as "Staff Writer" / "Staff Writer 3": a placeholder, not a person.
			if (string.IsNullOrWhiteSpace(name) || name.StartsWith("Staff Writer", System.StringComparison.Ordinal)) continue;
			if (!writers.Contains(name)) writers.Add(name);
		}
		string First<T>(IEnumerable<T> values, T unknown) where T : struct =>
			values?.Where(v => !EqualityComparer<T>.Default.Equals(v, unknown)).Select(v => v.ToString()).FirstOrDefault();
		var form = taxonomy?.formOverride ?? song?.defaultForm;
		var meter = taxonomy?.meterOverride ?? song?.defaultMeter;
		return new RecordFacts {
			Key = $"{r.recordId}|{record.weeksSinceRelease}",
			Title = r.title, ArtistName = r.artistName, LabelName = ChartManager.Instance?.GetLabelName(record.releaseLabelId ?? r.labelId),
			Band = artist != null && artist.type is ArtistType.Band or ArtistType.Duo or ArtistType.Trio or ArtistType.VocalGroup,
			Album = r.format == ReleaseFormat.Album,
			Position = listName != null ? listRank : record.currentPosition, ListName = listName,
			Bullet = record.isBullet, Anchor = record.isAnchor, WeeksOnChart = record.weeksOnChart, Peak = record.peakPosition,
			WeeksInTopTen = record.weeksInTopTen,
			Hook = r.hookStrength, Production = r.productionQuality, Originality = r.originality, Danceability = r.danceability,
			EarlierReleases = artist != null ? System.Math.Max(0, artist.totalReleases - 1) : 0,
			Cover = cover, Standard = source == SongMaterialSource.CoverStandard,
			Traditional = source is SongMaterialSource.TraditionalPublicDomain or SongMaterialSource.AdaptedTraditional,
			Adapted = source == SongMaterialSource.AdaptedTraditional,
			WrittenByAct = !cover && source is SongMaterialSource.ArtistWritten or SongMaterialSource.ArtistCowrittenWithProfessional,
			CoWritten = !cover && source == SongMaterialSource.ArtistCowrittenWithProfessional,
			ProfessionalSong = !cover && source is SongMaterialSource.LabelStaffWriter or SongMaterialSource.ExternalProfessional,
			OriginalArtist = cover ? ArtistOf(r.originalArtistId)?.stageName
				?? (string.IsNullOrEmpty(r.originalRecordId) ? null : ChartManager.Instance?.GetRecordRuntimeData(r.originalRecordId)?.baseRecord?.artistName) : null,
			Publisher = string.IsNullOrWhiteSpace(song?.rights?.publisherName) ? null : song.rights.publisherName,
			Writers = writers,
			Form = form is null or SongForm.Unknown ? null : form.ToString(),
			Meter = meter is null or SongMeter.Unknown or SongMeter.FourFour ? null : meter.ToString(),
			VocalApproach = First(taxonomy?.vocalApproaches, SongVocalApproach.Unknown),
			LyricMode = First(taxonomy?.lyricModes?.Where(m => m != SongLyricMode.Instrumental), SongLyricMode.Unknown),
			ProductionStyle = taxonomy == null || taxonomy.production == SongProduction.Unknown ? null : taxonomy.production.ToString(),
			Instrumental = taxonomy?.vocalPresence == SongVocalPresence.Instrumental,
			DurationSeconds = taxonomy?.durationSeconds,
			ContentTags = song?.contentTagIds?.ToList() ?? new List<string>(),
			RadioHeat = record.radioHeat, WordOfMouth = record.wordOfMouth, RegionalBreakouts = record.regionalBreakoutCount,
			UnitsThisWeek = record.unitsThisWeek, UnitsLastWeek = record.unitsPreviousWeek,
			GrammyNominee = record.isGrammyNominated, GrammyWinner = record.isGrammyWinner,
			Genre = GenreNameFormatter.Format(r.primaryGenre),
			SecondGenre = r.secondaryGenre != r.primaryGenre ? GenreNameFormatter.Format(r.secondaryGenre) : null
		};
	}

	// === CHART MOVEMENT ===

	public static string GetChartMovementComment(RecordRuntimeData record) =>
		TradePressPhrases.Movement($"{record.baseRecord?.recordId}|{record.weeksSinceRelease}|move", record.lastWeekPosition, record.currentPosition);

	/// <summary>The same remark for a place on a genre list. <paramref name="lastRank"/> is 0 for a new entry and -1 where the list
	/// keeps no week-on-week memory (regional action), which has nothing to say about movement.</summary>
	public static string GetListMovementComment(RecordRuntimeData record, int rank, int lastRank) =>
		lastRank < 0 ? "" : TradePressPhrases.Movement($"{record.baseRecord?.recordId}|{record.weeksSinceRelease}|list", lastRank, rank);

	/// <summary>Where the record is known, from each region's awareness; empty when no region knows it.</summary>
	public static string GetRegionalPerformanceHint(RecordRuntimeData record, List<MarketRegion> regions) {
		var known = regions.Where(region => record.regionalData.ContainsKey(region.regionId))
			.Select(region => (region.regionName, record.regionalData[region.regionId].awareness)).ToList();
		return TradePressPhrases.Regions($"{record.baseRecord?.recordId}|{record.weeksSinceRelease}|regions", known);
	}

	// === ARTISTS ===

	public static string DescribeArtist(ArtistPublicProfile artist) {
		var active = artist.personnel.Where(p => p.isActive).ToList();
		int guitars = active.Count(p => p.role is MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar);
		var parts = new List<string>();
		foreach (var p in active) {
			string part = p.role switch {
				MusicianRole.LeadVocals or MusicianRole.BackingVocals => "vocals",
				MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar => guitars > 1 ? Phrase.Number(guitars) + " guitars" : "guitar",
				MusicianRole.Bass => "bass", MusicianRole.Drums => "drums", MusicianRole.Piano => "piano", MusicianRole.Organ => "organ",
				MusicianRole.Saxophone => "sax", MusicianRole.Trumpet => "trumpet", MusicianRole.Harmonica => "harmonica",
				MusicianRole.Violin => "violin", MusicianRole.Percussion => "percussion", _ => null
			};
			if (part != null && !parts.Contains(part)) parts.Add(part);
		}
		string origin = !string.IsNullOrEmpty(artist.homeCity) ? artist.homeCity :
			!string.IsNullOrEmpty(artist.baseCity) || string.IsNullOrEmpty(artist.homeRegion) ? null : $"the {artist.homeRegion} region";
		return TradePressPhrases.DescribeArtist(new ArtistFacts {
			Key = artist.artistId, Name = artist.name,
			Genre = GenreNameFormatter.Format(artist.primaryGenre),
			SecondGenre = artist.secondaryGenre != artist.primaryGenre ? GenreNameFormatter.Format(artist.secondaryGenre) : null,
			Origin = origin, Base = string.IsNullOrEmpty(artist.baseCity) ? null : artist.baseCity,
			BaseInferred = artist.baseEvidence == PlaceEvidence.Inferred, Band = artist.isBand,
			// A solo act is a "singer" unless the file shows them playing an instrument and not singing.
			Singer = active.Count == 0 || active.Any(p => p.role is MusicianRole.LeadVocals or MusicianRole.BackingVocals),
			FormedYear = artist.formedYear, Signed = !string.IsNullOrEmpty(artist.labelId), LabelName = artist.labelName,
			Charted = artist.totalCharted, Top40 = artist.top40Hits, Top10 = artist.top10Hits, NumberOnes = artist.numberOneHits,
			Highest = artist.highestPosition, WeeksOnChart = artist.totalWeeksOnChart, Releases = artist.totalRecordsReleased,
			Members = active.Count, FoundersRemaining = active.Count(p => p.isFoundingMember),
			Departed = artist.personnel.Count(p => !p.isActive),
			Parts = artist.isBand ? parts : parts.Where(p => p != "vocals").ToList()
		});
	}

	// === LABELS ===

	public static string DescribeLabel(AILabel label) => TradePressPhrases.DescribeLabel(new LabelFacts {
		Key = label.labelId, Name = label.labelName, City = string.IsNullOrEmpty(label.headquartersCity) ? null : label.headquartersCity,
		Founder = string.IsNullOrEmpty(label.founderName) ? null : label.founderName, Tier = label.tier.ToString(),
		FoundedYear = label.foundedYear, Roster = label.roster?.Count(a => a != null) ?? 0, Releases = label.totalReleases,
		Top40 = label.top40Hits, NumberOnes = label.numberOneHits, RiskTolerance = label.riskTolerance,
		Genres = (label.preferredGenres ?? System.Array.Empty<Genre>()).Select(g => GenreNameFormatter.Format(g).ToLowerInvariant()).ToList()
	});
}
