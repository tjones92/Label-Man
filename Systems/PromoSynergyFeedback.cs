using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit A5: test the promo-single album credit against what linked LPs actually gained.
///
/// The format fork credits a promo project with an awareness lift and a recruitment
/// synergy (PromoAlbumConversionK), and charges modelled diversion. No memory lane
/// observed either album-side term. When live, each linked LP carries a counterfactual
/// awareness path without the promo's launch bonus, so its finalized record says how
/// many album units the promo recruited (actual raw demand minus counterfactual) and
/// how many it suppressed. Recency-weighted market ratios of realized to modelled
/// dollars, by original career cohort and shrunk toward the pooled ratio and then
/// toward 1, scale the two credits. With no finalized evidence both scales are 1.
///
/// Not measured: the promo's launch stock multiplier (a flop's 0.85 floor), and
/// second-order chart, word-of-mouth and penetration feedback from the extra sales.
/// The modelled credit is tested, not replaced, and catalogue-practice projects keep
/// their own standalone LP proposition.
/// </summary>
public static class PromoSynergyFeedback {
	public const string Switch = "--promo-synergy-feedback";
	internal const float HalfLifeWeeks = 52f;
	internal const float ConfidenceK = 10f;
	internal const float MaxRatio = 2f;
	internal const int CareerCohorts = 5;
	private static bool configured, enabled;
	private static bool? liveForChecks;
	public static bool Enabled => enabled;
	public static bool IsLive => liveForChecks ?? (enabled && GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
	public static bool HasState => enabled ||
		CompetitorManager.Instance?.GetAlbumProjects().Any(p => p.promoFeedback != null) == true ||
		ChartManager.Instance?.GetAllRecords().Any(r => r.promoShadowDemand.HasValue) == true;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (enabled && !GenreMarketV2.Enabled) throw new ArgumentException(Switch + " requires the live genre market.");
		configured = true;
	}

	internal static void SetForChecks(bool value, bool? live = null) { configured = true; enabled = value; liveForChecks = live; }

	internal static float RecencyWeight(int currentWeek, int finalWeek) =>
		MathF.Pow(.5f, Math.Max(0, currentWeek - finalWeek) / HalfLifeWeeks);

	/// <summary>Realized/modelled ratio, shrunk toward <paramref name="anchor"/> by effective evidence.</summary>
	internal static float Shrink(double realized, double modelled, double weight, float anchor) {
		if (weight <= 0d || modelled <= 0d) return anchor;
		float ratio = Math.Clamp((float)(realized / modelled), 0f, MaxRatio);
		float confidence = (float)(weight / (weight + ConfidenceK));
		return anchor + (ratio - anchor) * confidence;
	}

	/// <summary>Recruitment and diversion scales for one career cohort, from finalized evidence in creation order.</summary>
	internal static (float Recruitment, float Cannibalization, float Weight) Estimate(
		IEnumerable<PromoSynergyEvidence> evidence, int currentWeek, int careerBand) {
		double allW = 0, allRealR = 0, allModR = 0, allRealC = 0, allModC = 0;
		double bandW = 0, bandRealR = 0, bandModR = 0, bandRealC = 0, bandModC = 0;
		foreach (PromoSynergyEvidence e in evidence) {
			if (e?.finalWeek == null) continue;
			double w = RecencyWeight(currentWeek, e.finalWeek.Value);
			double realR = e.RealizedRecruitment, realC = e.RealizedSuppression;
			allW += w; allRealR += w * realR; allModR += w * e.creditedRecruitment;
			allRealC += w * realC; allModC += w * e.modeledCannibalization;
			if (e.careerBand != careerBand) continue;
			bandW += w; bandRealR += w * realR; bandModR += w * e.creditedRecruitment;
			bandRealC += w * realC; bandModC += w * e.modeledCannibalization;
		}
		float pooledR = Shrink(allRealR, allModR, allW, 1f), pooledC = Shrink(allRealC, allModC, allW, 1f);
		return (Shrink(bandRealR, bandModR, bandW, pooledR), Shrink(bandRealC, bandModC, bandW, pooledC), (float)bandW);
	}
}

/// <summary>Decision-time credit and finalized album-side outcome of one promo project (A5).</summary>
public sealed class PromoSynergyEvidence {
	public int careerBand;
	// Unscaled model terms; the learned ratio is realized / these, so it never compounds.
	public float creditedRecruitment;
	public float modeledCannibalization;
	public float albumMarginPerUnit;
	public float recruitmentScale = 1f;
	public float cannibalizationScale = 1f;
	public int? finalWeek;
	public double? realizedRecruitmentUnits;
	public double? realizedSuppressedUnits;
	[System.Text.Json.Serialization.JsonIgnore]
	public double RealizedRecruitment => (realizedRecruitmentUnits ?? 0d) * albumMarginPerUnit;
	[System.Text.Json.Serialization.JsonIgnore]
	public double RealizedSuppression => (realizedSuppressedUnits ?? 0d) * albumMarginPerUnit;
}
