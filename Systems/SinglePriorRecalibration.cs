using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Audit A2: a current-economy standalone-single prior, opt-in with --single-prior-recalibration.
/// Continuous in quality, nondecreasing in quality and career, without the old Q3 Established/Star inversion.
/// Only the live genre-market route uses it; disabled and prewarm decisions retain the frozen prior.
///
/// Fitted by SimTools/calibrate_single_prior.py to 19,897 finalized standalone launches through week 418 of
/// dec-awrw-on-1001-522. All launches in that mature cohort have finals. The target is arithmetic mean
/// (realizedNet + release-time production cost) / (reach x genre factor), at the current booked-price margin.
/// It includes pressing losses and excludes marketing, matching the outcome ledger; A4 handles marketing.
/// Twelve quality bins have extra resolution around the chart-entry region. Weighted isotonic regression
/// constrains both axes; empty cells use the pooled quality-bin mean at weight one. Linear interpolation
/// connects pooled mean qualities, with endpoint clamping. Sparse low-quality Star cells remain borrowed.
///
/// The independent reference seed improves substantially. Alternate album-practice policies still cause
/// underprediction for proven acts; this is an experimental prior, not a claim that A3 is resolved.
/// Declining acts retain the existing New/Unsigned fallback; they were excluded from the fit.
/// </summary>
public static class SinglePriorRecalibration {
	public const string Switch = "--single-prior-recalibration";
	// $0.63 dealer - $0.14 pressing - $0.04 composition - 5% of the $0.89 AI list price.
	// The table is ALREADY fitted at this margin. Reprice only relative to it; never apply the old counter-price discount again.
	internal const float ReferenceMargin = .4055f;
	private static bool configured, enabled;
	public static bool Enabled => enabled;

	private static readonly float[] QualityKnots = {
		.341612f, .426715f, .477602f, .525452f, .574851f, .612453f,
		.637196f, .661500f, .687267f, .721600f, .771398f, .842257f
	};
	// Rows are ascending quality knots; columns are New/Unsigned, Rising, Established, Star/Superstar.
	private static readonly float[,] NormalizedContribution = {
		{ 5511.967f, 7872.482f, 7872.482f, 7872.482f },
		{ 5832.356f, 7872.482f, 7872.482f, 7872.482f },
		{ 6381.240f, 10599.810f, 12944.387f, 12944.387f },
		{ 7317.786f, 10599.810f, 16343.484f, 16343.484f },
		{ 7686.986f, 10599.810f, 18784.868f, 18784.868f },
		{ 7686.986f, 13157.435f, 18784.868f, 29811.657f },
		{ 7686.986f, 13157.435f, 18784.868f, 29811.657f },
		{ 8363.023f, 13157.435f, 18784.868f, 29811.657f },
		{ 8824.771f, 14680.102f, 23391.449f, 29811.657f },
		{ 9830.316f, 14680.102f, 23391.449f, 44798.246f },
		{ 10984.538f, 17536.924f, 23517.412f, 44798.246f },
		{ 13792.148f, 18855.377f, 33703.963f, 46402.180f }
	};

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (enabled && (!GenreMarketV2.Enabled || !LabelPressingEconomics.Enabled || !LabelPressingEconomics.PeriodSingleCosts))
			throw new ArgumentException(Switch + " requires the live genre market, dealer pricing and period single pressing costs.");
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	internal static float Contribution(float quality, int careerBand) {
		int band = Math.Clamp(careerBand, 0, 3);
		if (quality <= QualityKnots[0]) return NormalizedContribution[0, band];
		for (int upper = 1; upper < QualityKnots.Length; upper++) {
			if (quality > QualityKnots[upper]) continue;
			float fraction = (quality - QualityKnots[upper - 1]) / (QualityKnots[upper] - QualityKnots[upper - 1]);
			return Mathf.Lerp(NormalizedContribution[upper - 1, band], NormalizedContribution[upper, band], fraction);
		}
		return NormalizedContribution[QualityKnots.Length - 1, band];
	}

	internal static float EstimateNet(float quality, int careerBand, float reach, float genreFactor,
		float productionCost, float currentMargin) =>
		Contribution(quality, careerBand) * reach * genreFactor * Mathf.Max(0f, currentMargin) / ReferenceMargin - productionCost;
}
