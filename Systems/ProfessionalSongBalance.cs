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
	internal static bool ExtendsPool => RegionalOffices || ExtraWriters > 0;
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
		if (!RegionalOffices || !RegionalOfficeTable.Any(o => o.Id == publisherId)) return 0;
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
