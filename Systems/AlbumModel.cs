using System.Collections.Generic;
using System.Linq;
using Godot;

public static class AlbumModel {
	public static float EraWeightStartYear = 1960f;
	public static float EraWeightEndYear = 1968f;
	public static float CohesionRiseStartYear = 1964f;
	public static float CohesionRiseEndYear = 1968f;

	// Exogenous curve. A future acclaim/legitimacy loop may add a bounded nudge.
	public static float GetAlbumEraWeight(int year) => Mathf.SmoothStep(0f, 1f,
		Mathf.Clamp((year - EraWeightStartYear) / (EraWeightEndYear - EraWeightStartYear), 0f, 1f));

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
		float era = Mathf.SmoothStep(0.12f, 0.96f,
			Mathf.Clamp((year - CohesionRiseStartYear) / (CohesionRiseEndYear - CohesionRiseStartYear), 0f, 1f));
		float excellence = Mathf.Clamp((artistTalent - 0.70f) / 0.30f, 0f, 1f) *
			Mathf.Clamp((labelProduction - 0.70f) / 0.30f, 0f, 1f);
		float fluke = year < 1965 && excellence > 0.75f && luckyRoll > 0.985f ? 0.55f * excellence : 0f;
		return Mathf.Clamp(era * (0.45f + 0.50f * artistTalent + 0.25f * labelProduction) + fluke, 0.08f, 1f);
	}
}
