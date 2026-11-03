using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit A3: refit LP affinity by career without multiplying production cost or changing demand.
/// Five nonnegative least-squares coefficients, career ordered, fitted by calibrate_album_prior.py
/// to every completed LP launched through week 364 of dec-awrw-on-1001-522 (22,702 LPs).
/// All four training/holdout cohorts are complete at that cutoff; week 418 is censored for LPs.
/// Quality, reach, period exposure, actual dealer margin and compilation cost remain separate.
/// Independent holdouts improve proven-act forecasts; New/Unsigned and period bias remain.
/// This forecasts the simulation's economics; it does not establish realistic debut-LP demand.
/// </summary>
public static class AlbumPriorRecalibration {
	public const string Switch = "--album-prior-recalibration";
	// Opt-in probe (SimTools/LpProfitGateFunnelInvestigation.md): finalized NewSigning ordinary LPs realized ~2.9x
	// their forecast contribution in the combined profile. Scales NewSigning affinity only, capped at Rising's so
	// the career order cannot invert. 1 leaves the fit unchanged.
	public const string DebutScaleSwitch = "--album-prior-debut-scale=";
	private static bool enabled, configured;
	public static bool Enabled => enabled;
	public static float DebutScale { get; private set; } = 1f;
	internal static float MaximumDebutScale => Affinity[1] / Affinity[0];
	public static bool IsLive => enabled && GenreMarketV2.Enabled &&
		(ChartManager.Instance?.IsGenreMarketV2Live == true || CompetitorManager.Instance?.AlbumPracticeLiveTick == true);
	internal const float HitCoefficient = 1.062010614f;
	private static readonly float[] Affinity = { 1.107276574f, 2.282491535f, 3.870309897f, 4.881536394f };

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (enabled && (!GenreMarketV2.Enabled || !LabelPressingEconomics.Enabled || !LabelPressingEconomics.PeriodAlbumCosts))
			throw new ArgumentException(Switch + " requires the live genre market, dealer pricing and period LP costs.");
		string debut = (arguments ?? Array.Empty<string>()).SingleOrDefault(a => a.StartsWith(DebutScaleSwitch, StringComparison.Ordinal));
		if (debut != null) {
			if (!enabled) throw new ArgumentException(DebutScaleSwitch + " requires " + Switch + ".");
			if (!float.TryParse(debut[DebutScaleSwitch.Length..], System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float scale) || !float.IsFinite(scale) ||
					scale < 1f || scale > MaximumDebutScale + .0001f)
				throw new ArgumentException(DebutScaleSwitch + $" requires a scale between 1 and {MaximumDebutScale:0.###}.");
			DebutScale = Math.Min(scale, MaximumDebutScale);
		}
		configured = true;
	}

	internal static void SetForChecks(bool value, float debutScale = 1f) { configured = true; enabled = value; DebutScale = debutScale; }
	internal static float CareerAffinity(CareerState state) => state switch {
		CareerState.Rising => Affinity[1], CareerState.Established => Affinity[2],
		CareerState.Star => Affinity[3], CareerState.Superstar => Affinity[3] * 1.25f,
		CareerState.NewSigning => Affinity[0] * DebutScale,
		_ => Affinity[0]
	};
}
