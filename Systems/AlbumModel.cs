using System.Collections.Generic;
using System.Linq;
using Godot;

public static class AlbumModel {
	public static float EraWeightStartYear = 1960f;
	public static float EraWeightEndYear = 1968f;
	public static float CohesionRiseStartYear = 1964f;
	public static float CohesionRiseEndYear = 1968f;
	public static float CompilationDeclineStartYear = 1963f;
	public static float CompilationDeclineEndYear = 1969f;
	public static float MaximumCompilationChance = 0.97f;
	// Deliberately vanishing. Tuned against the measured concept count, not by intuition:
	// the target is a handful across 1965-66, not a wave.
	public static float EarlyStatementYear = 1965f;
	public static float EarlyStatementExcellence = 0.55f;
	public static float EarlyStatementRollThreshold = 0.94f;
	public static float EarlyStatementCohesionFloor = 0.80f;

	// Exogenous curve. A future acclaim/legitimacy loop may add a bounded nudge.
	public static float GetAlbumEraWeight(int year) => Mathf.SmoothStep(0f, 1f,
		Mathf.Clamp((year - EraWeightStartYear) / (EraWeightEndYear - EraWeightStartYear), 0f, 1f));

	/// <summary>
	/// LP-RATIO RECALIBRATION (2026-08). Was a step that returned 0 until the album-adoption curve
	/// reached its midpoint (~1964), which zeroed the dedicated album retail channel early and forced
	/// pre-1964 albums to clear inside the single channel under a 2x overlap penalty -- displacing
	/// ~79% of serviceable album demand in 1960 and pinning LP unit share near zero. The LP retail
	/// channel existed in 1960; it was simply smaller. So retail fulfillment is now mature throughout
	/// the period (=1) and the channel's SIZE is carried entirely by its era-scaled capacity share
	/// (the AlbumChannelShareEra convex quadratic in ChartManager), which is the calibrated LP:45 lever.
	/// </summary>
	public static float GetRetailFulfillmentMaturity(int year) => 1f;

	public static float CalculatePooledAppeal(IEnumerable<float> trackQualities, float thematicCohesion, int year) {
		float[] qualities = trackQualities?.Select(quality => Mathf.Clamp(quality, 0f, 1f)).ToArray()
			?? System.Array.Empty<float>();
		if (qualities.Length == 0) return 0f;
		float peakTrack = qualities.Max();
		float meanTrack = qualities.Average();
		float peakWeighted = 0.70f * peakTrack + 0.30f * meanTrack;
		float wholeWeighted = 0.45f * meanTrack + 0.35f * Mathf.Clamp(thematicCohesion, 0f, 1f) + 0.20f * peakTrack;
		return Mathf.Lerp(peakWeighted, wholeWeighted, GetAlbumEraWeight(year));
	}

	public static float GetMaximumAchievableCohesion(int year, float artistTalent, float labelProduction, float luckyRoll) {
		// The exogenous curve is the floor and the shape. Album legitimacy -- records that
		// actually happened and were actually heard -- can pull it forward in time by a
		// bounded amount, never rewrite it, and is exactly 1.0x when that phase is off.
		float era = Mathf.SmoothStep(0.12f, 0.96f,
			Mathf.Clamp((year - CohesionRiseStartYear) / (CohesionRiseEndYear - CohesionRiseStartYear), 0f, 1f))
			* AlbumLegitimacyService.CurrentCeilingMultiplier;
		float excellence = Mathf.Clamp((artistTalent - 0.70f) / 0.30f, 0f, 1f) *
			Mathf.Clamp((labelProduction - 0.70f) / 0.30f, 0f, 1f);
		float baseline = Mathf.Clamp(era * (0.45f + 0.50f * artistTalent + 0.25f * labelProduction), 0.08f, 1f);
		// Rubber Soul (1965) and Pet Sounds (1966) preceded the era ramp. That ramp alone
		// cannot reach the 0.72 statement bar until 1967, and the pre-1965 fluke term it
		// replaces could not either -- its ceiling topped out around 0.55 -- so no concept
		// album was reachable anywhere in the decade before 1967, which the artifacts
		// confirm: zero of them across 1960-66. This opens a deliberately vanishing path
		// from 1965 for near-top talent in a near-top room on the best few percent of rolls.
		bool pioneer = year >= EarlyStatementYear && excellence > EarlyStatementExcellence &&
			luckyRoll > EarlyStatementRollThreshold;
		return pioneer ? Mathf.Clamp(Mathf.Max(baseline, EarlyStatementCohesionFloor), 0.08f, 1f) : baseline;
	}

	/// <summary>
	/// How far each family was actually swept up in the LP revolution. Rock and jazz almost
	/// entirely; soul later and only partly, since it stayed a singles business well past
	/// 1969; bubblegum essentially not at all -- it was manufactured studio product aimed at
	/// children and remained so. Applying one uniform era decline to every genre is what put
	/// psychedelic rock and classical on the same compilation rate.
	/// </summary>
	public static float GetAlbumRevolutionSusceptibility(GenreFamily family) => family switch {
		GenreFamily.Rock or GenreFamily.Folk or GenreFamily.Jazz => 0.80f,
		GenreFamily.RhythmAndSoul or GenreFamily.Blues => 0.55f,
		GenreFamily.Country or GenreFamily.Gospel => 0.45f,
		GenreFamily.Pop => 0.12f,
		_ => 0.30f
	};

	/// <summary>
	/// Probability that an album is assembled from already-released singles plus filler.
	/// Propensity is the catalog's authored SingleOrientation -- a singles-led genre builds
	/// its LPs out of singles -- decayed by the era term above. The model was right about
	/// 1960, where pop and rock LPs genuinely were a hit plus filler; it simply never let
	/// go, holding youth genres at 82-96% through 1969 while the album became the artistic
	/// unit around them, and holding classical there too for want of being on a hardcoded
	/// six-genre adult list.
	/// </summary>
	public static float GetCompilationChance(float singleOrientation, GenreFamily family, int year) {
		float decline = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(
			(year - CompilationDeclineStartYear) / (CompilationDeclineEndYear - CompilationDeclineStartYear), 0f, 1f));
		float retained = 1f - decline * GetAlbumRevolutionSusceptibility(family);
		return Mathf.Clamp(Mathf.Clamp(singleOrientation, 0f, 1f) * retained, 0f, MaximumCompilationChance);
	}

	public static float GetCompilationChance(Genre genre, int year) =>
		GenreCatalog.TryGet(GenreCatalog.MapLegacy(genre, year), out GenreProfile profile)
			? GetCompilationChance(profile.SingleOrientation, profile.Family, year)
			: GetCompilationChance(0.60f, GenreFamily.Pop, year);
}
