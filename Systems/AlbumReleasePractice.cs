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
	public static readonly float CampaignScale = ReadScale("--album-promo-campaign-scale=", 1f);
	// Late-decade LP-led singles, opt-in: --album-practice-late-advance=K. Inert unless named; 1960-64 never change.
	public static readonly float LateAdvanceScale = ReadScale("--album-practice-late-advance=", 0f);
	public static bool IsLive => Enabled && GenreMarketV2.Enabled && CompetitorManager.Instance?.AlbumPracticeLiveTick == true;
	public static readonly bool SeparateSinglesCalendar = !OS.GetCmdlineUserArgs().Contains("--album-practice-coupled-calendar");
	public static readonly bool HitLedAlbums = !OS.GetCmdlineUserArgs().Contains("--album-practice-album-first");

	internal static bool NeedsSingleAudience(Genre genre, int year, int priorSingles, float roll) {
		float orientation = GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year)).SingleOrientation;
		return orientation >= .6f && priorSingles == 0 && roll < orientation;
	}

	private static float ReadScale(string prefix, float absent) {
		string arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
		if (arg == null) return absent;
		if (!float.TryParse(arg.Split('=')[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) ||
			!float.IsFinite(scale) || scale < 0f || scale > 3f)
			throw new ArgumentException($"{prefix.TrimEnd('=')} must be finite and between 0 and 3.");
		return scale;
	}

	// 0 through 1964, 1 from 1967. SimTools/LateDecadeSingleSourcing.md: in a dated census of the Billboard
	// year-end top 20, singles issued with or ahead of an LP that has its own title are 10% of 1963 hits,
	// 17.5% of 1965-66 and 22% of 1967-69, so about half the change has happened by 1965-66.
	internal static float LateDecadeWeight(int year) => Mathf.Clamp((year - 1964) / 3f, 0f, 1f);

	// Deliberately no label-tier multiplier: the late census puts Big-5 and independent hits at 57% and
	// 46% LP-sourced, about one SE apart, so no Major entitlement. The era term is opt-in. From 1965 it
	// ramps the prior to lateScale times its institutional value and drops the dependence on singles
	// orientation, because the late adult-pop hits (Mauriat, Montenegro, Mancini, Alpert, O. C. Smith) are
	// LP-sourced. Jazz/Gospel/Blues/Folk keep their institution factor, and Classical/NonMusic stay at zero.
	internal static float AdvanceCommissionProbability(Genre genre, int year, float scale = 1f, float? lateScale = null) {
		GenreProfile profile = GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year));
		float institution = profile.Family switch {
			GenreFamily.Classical or GenreFamily.NonMusic => 0f,
			GenreFamily.Jazz or GenreFamily.Gospel => .25f,
			GenreFamily.Blues or GenreFamily.Folk => .5f,
			_ => 1f
		};
		float late = lateScale ?? LateAdvanceScale;
		float weight = late > 0f ? LateDecadeWeight(year) : 0f;
		float orientation = Mathf.Lerp(profile.SingleOrientation, 1f, weight);
		return Mathf.Clamp(.15f * orientation * institution * scale * Mathf.Lerp(1f, late, weight), 0f, .45f);
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
