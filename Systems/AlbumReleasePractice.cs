using System;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Research candidate: separate an advance LP campaign from a hit subsequently
/// collected on an LP, or a cut extracted after the LP succeeds. See
/// SimTools/AlbumReleasePracticeResearch.md for evidence and uncertainty.
/// Commission probabilities are modelling priors, NOT measured historical rates.
/// </summary>
public static class AlbumReleasePractice {
	public const string Switch = "--album-release-practice";
	public static readonly bool Enabled = OS.GetCmdlineUserArgs().Contains(Switch);
	public static readonly float CampaignScale = ReadScale();
	public static bool IsLive => Enabled && GenreMarketV2.Enabled && CompetitorManager.Instance?.AlbumPracticeLiveTick == true;
	public static readonly bool SeparateSinglesCalendar = !OS.GetCmdlineUserArgs().Contains("--album-practice-coupled-calendar");
	public static readonly bool HitLedAlbums = !OS.GetCmdlineUserArgs().Contains("--album-practice-album-first");

	internal static bool NeedsSingleAudience(Genre genre, int year, int priorSingles, float roll) {
		float orientation = GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year)).SingleOrientation;
		return orientation >= .6f && priorSingles == 0 && roll < orientation;
	}

	private static float ReadScale() {
		string arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--album-promo-campaign-scale=", StringComparison.Ordinal));
		if (arg == null) return 1f;
		if (!float.TryParse(arg.Split('=')[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) ||
			!float.IsFinite(scale) || scale < 0f || scale > 3f)
			throw new ArgumentException("Album promo campaign scale must be finite and between 0 and 3.");
		return scale;
	}

	// Deliberately no annual or label-tier multipliers: available sources establish
	// different routes, not precise genre-year percentages or a Major entitlement.
	internal static float AdvanceCommissionProbability(Genre genre, int year, float scale = 1f) {
		GenreProfile profile = GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year));
		float institution = profile.Family switch {
			GenreFamily.Classical or GenreFamily.NonMusic => 0f,
			GenreFamily.Jazz or GenreFamily.Gospel => .25f,
			GenreFamily.Blues or GenreFamily.Folk => .5f,
			_ => 1f
		};
		return Mathf.Clamp(.15f * profile.SingleOrientation * institution * scale, 0f, .45f);
	}

	internal static bool CanAdvance(Genre genre, AlbumFormat format, int year) =>
		format is AlbumFormat.Standard or AlbumFormat.Concept && AdvanceCommissionProbability(genre, year) > 0f;

	internal static bool CanExtract(Genre genre, int year, int ageWeeks, int peakPosition, bool alreadyExtracted) =>
		GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year)).Family is not (GenreFamily.Classical or GenreFamily.NonMusic) &&
		ageWeeks >= 8 && ageWeeks <= 52 && peakPosition > 0 && peakPosition <= 40 && !alreadyExtracted;

	internal static float SinglesProgrammeProbability(Genre genre, int year) =>
		Mathf.Clamp(GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year)).SingleOrientation, 0f, 1f);

	internal static float Roll(string domain, string labelId, string artistId, string recordId, int week) {
		uint hash = 2166136261u;
		foreach (char value in $"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|album-practice-v1|{domain}|{labelId}|{artistId}|{recordId}|{week}") {
			hash ^= value; hash *= 16777619u;
		}
		return (hash >> 8) / 16777216f;
	}

	internal static float IncrementalContribution(float singleContributionBeforeCost, float preparationCost,
		float campaignCost, float lpLift, float cannibalization, float delayCost, float opportunityCost = 0f) =>
		singleContributionBeforeCost - Mathf.Max(0f, preparationCost) - Mathf.Max(0f, campaignCost) +
		Mathf.Max(0f, lpLift) - Mathf.Max(0f, cannibalization) - Mathf.Max(0f, delayCost) - Mathf.Max(0f, opportunityCost);
}
