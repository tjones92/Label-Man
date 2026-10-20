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
	private static bool enabled, configured;
	public static bool Enabled => enabled;
	public static bool IsLive => enabled && GenreMarketV2.Enabled &&
		(ChartManager.Instance?.IsGenreMarketV2Live == true || CompetitorManager.Instance?.AlbumPracticeLiveTick == true);
	internal const float HitCoefficient = 1.062010614f;
	private static readonly float[] Affinity = { 1.107276574f, 2.282491535f, 3.870309897f, 4.881536394f };

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (enabled && (!GenreMarketV2.Enabled || !LabelPressingEconomics.Enabled || !LabelPressingEconomics.PeriodAlbumCosts))
			throw new ArgumentException(Switch + " requires the live genre market, dealer pricing and period LP costs.");
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }
	internal static float CareerAffinity(CareerState state) => state switch {
		CareerState.Rising => Affinity[1], CareerState.Established => Affinity[2],
		CareerState.Star => Affinity[3], CareerState.Superstar => Affinity[3] * 1.25f,
		_ => Affinity[0]
	};
}
