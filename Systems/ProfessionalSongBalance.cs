using System;
using System.Globalization;
using System.Linq;

/// <summary>
/// Opt-in probes (SimTools/ProfessionalSongBalanceExperiment.md). In the finite writing market about 93% of
/// professional single arrivals find no visible demo, while ~60% of newly recorded demos end up only as LP cuts.
/// <list type="bullet">
/// <item><c>--lp-filler-spares-fresh-demos</c>: a non-single album cut may use only a professional demo the act
/// already holds; it never licenses a fresh one, so first recordings stay available to singles. The cut falls
/// through to the ordinary source mix (covers, originals, traditional), as LP filler commonly did.</item>
/// <item><c>--professional-family-pool</c>: an AI act is pitched professional demos from its whole genre family,
/// not only songs whose primary genre equals its own (an office song takes one random focus genre, so a Detroit
/// demo filed as Motown was invisible to every R&amp;B act). Cover pools already fall back to the family.</item>
/// <item><c>--writing-market-capacity=X</c>: professional writers start a new demo X times as often. The per-writer
/// cap of eight unplaced demos is unchanged.</item>
/// <item><c>--writing-market-regional-offices</c>: six label-house offices for the R&amp;B and rock &amp; roll writing
/// the opening world lacks (New York R&amp;B, Chicago, Cincinnati, Memphis, New Orleans, Philadelphia), 74 staff
/// writers and 45 opening titles each.</item>
/// <item><c>--writing-market-extra-writers=N</c>: N more staff writers, dealt uniformly across the opening offices
/// as the original 120 were. They start with no catalogue and write at the ordinary cadence and fees.</item>
/// <item><c>--writing-market-royalty-licences</c>: a first recording of an office demo costs the label no upfront
/// licence fee. Period publishers pitched songs free and were paid by the 2c mechanical, which an AI record already
/// pays through COGS (PeriodAiPublishing). Commissions, retainers and the first-recording hold are unchanged.</item>
/// <item><c>--writing-market-canonical-office-genres</c>: an office song's genre is filed through
/// <see cref="GenreCatalog.MapLegacy"/>. New York Pop files a third of its songs as GirlGroup and Detroit a third as Motown,
/// legacy values no act holds and no family pool reaches, so those demos were never placed and filled their writers'
/// eight-demo caps. The draw is unchanged; only the filed genre is.</item>
/// <item><c>--writing-market-specialist-offices</c>: ten single-genre offices (two TraditionalPop, one TeenPop, one
/// EasyListening, three R&amp;B, three Country), 120 staff writers and 45 opening titles each. With it, an act is pitched professional
/// demos of its own genre first and reaches into its family only when its own shelf is empty, so TeenPop and
/// EasyListening acts no longer draw down the TraditionalPop offices.</item>
/// <item><c>--source-mix-standards-to-recent-hits</c>: in <c>SongMaterialSelectionService.SourceMix</c>, the standards share
/// a non-songbook genre gives up under <c>StandardShareFactor</c> is added to recent-hit covers instead of being spread over
/// every source by normalization. The 1960 anchors match the working central mix (40% covers) only before that factor.</item>
/// <item><c>--source-mix-1960-pro-trims</c> / <c>--tradpop-singles-new-songs</c>: per-genre revisions of the 1960 source-mix
/// anchor (<c>SongMaterialSelectionService.Revised1960</c>): lower professional shares for DooWop, Folk, Blues, Jazz and
/// Classical, and a new-song singles mix for TraditionalPop (its LPs keep the standards-heavy anchor).</item>
/// <item><c>--programme-search-follows-mix</c>: the slate's material search (CompetitorManager.MaterialSearch) samples
/// professional demos for a slot only with probability min(1, genre professional share / 0.30). It picks the best-valued
/// offer regardless of source, and carried half to three-quarters of professional singles, so arrival-mix changes did not
/// reach it.</item>
/// </list>
/// All are off unless named and consume no random draws. The pool extension draws from a private stream and
/// takes its own writer and song ids after the opening catalogue, so the shared catalogue streams are untouched.
/// </summary>
public static class ProfessionalSongBalance {
	public const string LpFillerSwitch = "--lp-filler-spares-fresh-demos";
	public const string CapacityPrefix = "--writing-market-capacity=";
	public const string FamilyPoolSwitch = "--professional-family-pool";
	public const string RegionalOfficesSwitch = "--writing-market-regional-offices";
	public const string ExtraWritersPrefix = "--writing-market-extra-writers=";
	public static readonly bool FamilyPool = SimulationArguments.Get().Contains(FamilyPoolSwitch);
	public static readonly bool LpFillerSparesFreshDemos = SimulationArguments.Get().Contains(LpFillerSwitch);
	public static readonly float WriterCapacity = ParseCapacity(SimulationArguments.Get());
	public static readonly bool RegionalOffices = SimulationArguments.Get().Contains(RegionalOfficesSwitch);
	public static readonly int ExtraWriters = ParseExtraWriters(SimulationArguments.Get());
	public const string RoyaltyLicencesSwitch = "--writing-market-royalty-licences";
	public static readonly bool RoyaltyLicences = SimulationArguments.Get().Contains(RoyaltyLicencesSwitch);
	/// <summary>
	/// --folk-material-mix: Folk, ContemporaryFolk and FolkRock get their own source mixes instead of the Folk family's.
	/// Traditional material stays dominant in Folk (the revival's repertoire) but not in the genres that left it:
	/// contemporary folk sang new songwriters (Baez, Collins, Ochs) and folk rock electrified originals and Dylan-era
	/// songs, with a staff-writer share (Sloan, Bonner/Gordon). FanbaseEfficacy follow-up, 2026-11-10: 19% of 1966-68
	/// FolkRock singles were public-domain songs at quality .53 inherited from the family default.
	/// </summary>
	public const string FolkMaterialMixSwitch = "--folk-material-mix";
	public static readonly bool FolkMaterialMix = SimulationArguments.Get().Contains(FolkMaterialMixSwitch);
	public const string CanonicalOfficeGenresSwitch = "--writing-market-canonical-office-genres";
	public static readonly bool CanonicalOfficeGenres = SimulationArguments.Get().Contains(CanonicalOfficeGenresSwitch);
	public const string SpecialistOfficesSwitch = "--writing-market-specialist-offices";
	public static readonly bool SpecialistOffices = SimulationArguments.Get().Contains(SpecialistOfficesSwitch);
	public const string StandardsToRecentHitsSwitch = "--source-mix-standards-to-recent-hits";
	public static readonly bool StandardsToRecentHits = SimulationArguments.Get().Contains(StandardsToRecentHitsSwitch);
	public const string ProTrims1960Switch = "--source-mix-1960-pro-trims";
	public static readonly bool ProTrims1960 = SimulationArguments.Get().Contains(ProTrims1960Switch);
	public const string TradPopSinglesNewSongsSwitch = "--tradpop-singles-new-songs";
	public static readonly bool TradPopSinglesNewSongs = SimulationArguments.Get().Contains(TradPopSinglesNewSongsSwitch);
	public const string ProgrammeSearchFollowsMixSwitch = "--programme-search-follows-mix";
	public static readonly bool ProgrammeSearchFollowsMix = SimulationArguments.Get().Contains(ProgrammeSearchFollowsMixSwitch);
	internal static bool ExtendsPool => RegionalOffices || ExtraWriters > 0 || SpecialistOffices;

	/// <summary>The genre an office song is filed under; the legacy value itself when the canonical switch is off.</summary>
	internal static Genre FileGenre(Genre drawn) => CanonicalOfficeGenres ? GenreCatalog.MapLegacy(drawn, 1960) : drawn;

	/// <summary>Under specialist offices, the act's own-genre demos when any are visible, else the whole (family) pool.</summary>
	internal static System.Collections.Generic.IReadOnlyList<SongComposition> OwnGenreFirst(
		System.Collections.Generic.IReadOnlyList<SongComposition> pool, Genre genre) {
		if (!SpecialistOffices || pool.Count == 0) return pool;
		var own = pool.Where(s => s != null && s.primaryGenre == genre).ToArray();
		return own.Length > 0 && own.Length < pool.Count ? own : pool;
	}
	// Set only while GenerateAlbum chooses a non-single cut's material.
	internal static bool ChoosingAlbumFiller;

	// Label-house publishing of 1960-62: Atlantic/Leiber-Stoller, Chess-Arc and Vee-Jay, King-Lois, Stax-East and Hi,
	// Minit/Imperial, Cameo-Parkway. Staffs are ballpark working-writer counts, not rosters.
	internal static readonly (string Id, string Name, PublishingScene Scene, Genre[] Focus, int Writers)[] RegionalOfficeTable = {
		("pub_ny_rnb", "New York R&B House", PublishingScene.NewYorkPopFactory, new[] { Genre.RnB, Genre.RockAndRoll, Genre.DooWop }, 18),
		("pub_chicago", "Chicago R&B House", PublishingScene.LabelAffiliate, new[] { Genre.RnB, Genre.DooWop, Genre.Blues }, 14),
		("pub_cincinnati", "Cincinnati Label House", PublishingScene.LabelAffiliate, new[] { Genre.RnB, Genre.RockAndRoll, Genre.Country }, 10),
		("pub_memphis", "Memphis House", PublishingScene.MemphisSoul, new[] { Genre.Soul, Genre.RnB, Genre.RockAndRoll }, 12),
		("pub_new_orleans", "New Orleans House", PublishingScene.LabelAffiliate, new[] { Genre.RnB, Genre.RockAndRoll }, 8),
		("pub_philadelphia", "Philadelphia Teen House", PublishingScene.LabelAffiliate, new[] { Genre.TeenPop, Genre.RockAndRoll, Genre.DooWop }, 12),
	};

	// Single-genre houses of 1960-62: Chappell/Famous (Broadway and film), Robbins-Feist-Miller, Aldon and Hill & Range,
	// a mood/instrumental catalogue, and the R&B publishers the label houses above miss (Lion/Duke-Peacock in Houston,
	// Venice/Specialty and Modern in Los Angeles, Fire/Fury and Old Town in Harlem). Staffs are ballpark.
	internal static readonly (string Id, string Name, PublishingScene Scene, Genre[] Focus, int Writers)[] SpecialistOfficeTable = {
		("pub_broadway_film", "Broadway & Film Music House", PublishingScene.LegacyTinPanAlley, new[] { Genre.TraditionalPop }, 16),
		("pub_big_three", "Big Three Pop House", PublishingScene.LegacyTinPanAlley, new[] { Genre.TraditionalPop }, 14),
		("pub_brill_teen", "Brill Teen House", PublishingScene.NewYorkPopFactory, new[] { Genre.TeenPop }, 16),
		("pub_mood_music", "Mood & Instrumental House", PublishingScene.LegacyTinPanAlley, new[] { Genre.EasyListening }, 10),
		("pub_houston_rnb", "Houston R&B House", PublishingScene.LabelAffiliate, new[] { Genre.RnB }, 10),
		("pub_la_rnb", "Los Angeles R&B House", PublishingScene.LabelAffiliate, new[] { Genre.RnB }, 10),
		("pub_harlem_rnb", "Harlem R&B House", PublishingScene.LabelAffiliate, new[] { Genre.RnB }, 10),
		// Country: Acuff-Rose; Tree and Cedarwood; Central Songs (Capitol, Bakersfield/Hollywood).
		("pub_acuff_rose", "Acuff-Rose Country House", PublishingScene.Nashville, new[] { Genre.Country }, 14),
		("pub_tree_cedarwood", "Tree & Cedarwood House", PublishingScene.Nashville, new[] { Genre.Country }, 12),
		("pub_bakersfield", "Bakersfield Country House", PublishingScene.LabelAffiliate, new[] { Genre.Country }, 8),
	};

	/// <summary>Offices the extension adds, in creation order.</summary>
	internal static System.Collections.Generic.IEnumerable<(string Id, string Name, PublishingScene Scene, Genre[] Focus, int Writers)> AddedOffices() {
		if (RegionalOffices) foreach (var o in RegionalOfficeTable) yield return o;
		if (SpecialistOffices) foreach (var o in SpecialistOfficeTable) yield return o;
	}

	internal static int ParseExtraWriters(System.Collections.Generic.IEnumerable<string> arguments) {
		string value = (arguments ?? Array.Empty<string>()).SingleOrDefault(a => a.StartsWith(ExtraWritersPrefix, StringComparison.Ordinal));
		if (value == null) return 0;
		if (!int.TryParse(value[ExtraWritersPrefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 0 || n > 1000)
			throw new ArgumentException(ExtraWritersPrefix + " requires a writer count between 0 and 1000.");
		return n;
	}

	/// <summary>Salt for a live office song's keyed stream. Zero for the opening offices, whose songs keep their
	/// serial-only key; an added office gets its own, so its serial-n song does not repeat another office's traits.</summary>
	internal static ulong OfficeSalt(string publisherId) {
		if (!AddedOffices().Any(o => o.Id == publisherId)) return 0;
		ulong h = 14695981039346656037UL;
		foreach (char c in publisherId) { h ^= c; h *= 1099511628211UL; }
		return h;
	}

	internal static float ParseCapacity(System.Collections.Generic.IEnumerable<string> arguments) {
		string value = (arguments ?? Array.Empty<string>()).SingleOrDefault(a => a.StartsWith(CapacityPrefix, StringComparison.Ordinal));
		if (value == null) return 1f;
		if (!float.TryParse(value[CapacityPrefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
			!float.IsFinite(x) || x < 1f || x > 8f)
			throw new ArgumentException(CapacityPrefix + " requires a multiplier between 1 and 8.");
		return x;
	}

	internal static bool SparesFreshDemos => LpFillerSparesFreshDemos && ChoosingAlbumFiller;

	/// <summary>Weeks until a writer's next demo: the original 4-6 week cadence divided by the capacity, at least one.</summary>
	internal static int CadenceWeeks(float unit) {
		int legacy = 4 + (int)(unit * 3);
		return WriterCapacity == 1f ? legacy : Math.Max(1, (int)Math.Round(legacy / WriterCapacity));
	}
}
