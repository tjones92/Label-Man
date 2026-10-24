using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Calibration probe (LabelReleaseProgrammeImplementation.md, "Top 10 turnover"), off unless named. Two position
/// feedbacks that reward entering the Top 10:
/// <list type="bullet">
/// <item><c>--chart-exposure-plateau-rank=R</c> replaces CHART_EXPOSURE_PLATEAU_RANK (10) in the rank exposure curve.
/// R above 10 lowers and widens the plateau, so entering the Top 10 buys no more purchase exposure than #R; R below
/// 10 steepens exposure inside the Top 10. The curve is renormalised to mean 1, so total units are unchanged.</item>
/// <item><c>--radio-position-bonus-scale=S</c> scales the Top 10 / Top 40 radio heat bonus (0.25 / 0.10), national
/// and regional alike. S = 0 removes it.</item>
/// </list>
/// Off, both return the authored value bit-for-bit, so a run without either switch is byte-identical.
/// </summary>
public static class ChartEntryFeedbackProbe {
	public const string PlateauPrefix = "--chart-exposure-plateau-rank=";
	public const string RadioBonusPrefix = "--radio-position-bonus-scale=";
	private static bool configured;
	private static int plateauRank = -1;
	private static float radioBonusScale = -1f;
	public static bool PlateauEnabled => plateauRank > 0;
	public static bool RadioBonusEnabled => radioBonusScale >= 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string plateau = Single(args, PlateauPrefix);
		if (plateau != null) {
			if (!int.TryParse(plateau, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank) || rank < 1 || rank > 40)
				throw new ArgumentException(PlateauPrefix + " needs an integer rank in [1, 40].");
			plateauRank = rank;
		}
		string bonus = Single(args, RadioBonusPrefix);
		if (bonus != null) {
			if (!float.TryParse(bonus, NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) || !(scale >= 0f && scale <= 1f))
				throw new ArgumentException(RadioBonusPrefix + " needs a value in [0, 1].");
			radioBonusScale = scale;
		}
		configured = true;
		ChartSimulator.ResetChartExposureMean();
	}

	private static string Single(string[] args, string prefix) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + prefix + " once.");
		return values.Length == 1 ? values[0].Substring(prefix.Length) : null;
	}

	internal static void SetForChecks(int rank, float scale) {
		configured = true; plateauRank = rank; radioBonusScale = scale;
		ChartSimulator.ResetChartExposureMean();
	}

	public static int ApplyPlateau(int authored) => plateauRank > 0 ? plateauRank : authored;
	public static float ApplyRadioBonus(float authored) => radioBonusScale < 0f ? authored : authored * radioBonusScale;
}
