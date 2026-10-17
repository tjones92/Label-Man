using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17e), off unless named.
/// --chart-inertia-base=B replaces BASE_INERTIA (0.80) in <see cref="ChartSimulator.GetInertiaPositionCap"/>, the
/// sales-gated cap on how far a charted record may fall in one week.
///
/// At 0.80 the cap holds 6.9-7.2% of Top 10 slot-weeks for records whose raw rank is 11-13, and it keeps 26-35
/// records per five-year seed (about six a year) out of the Top 10 entirely -- they stall at a published #11. It is
/// label-neutral. Both are on the clock that sets Top 10 stay length (the post-peak fall), which is ~15% long.
/// B = 0 removes the cap: every record takes its raw position. The quality override and momentum bonus keep their
/// authored weights; the momentum bonus is still capped at B.
/// </summary>
public static class ChartInertiaProbe {
	public const string SwitchPrefix = "--chart-inertia-base=";
	private static bool configured;
	private static float baseInertia = -1f;
	public static bool Enabled => baseInertia >= 0f;
	public static float BaseInertia => baseInertia;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --chart-inertia-base once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed >= 0f && parsed < 1f))
				throw new ArgumentException("--chart-inertia-base needs a value in [0, 1).");
			baseInertia = parsed;
		}
		configured = true;
	}

	internal static void SetBaseForChecks(float value) { configured = true; baseInertia = value; }

	/// <summary>Returns the authored value bit-for-bit when off, so a run without the switch is byte-identical.</summary>
	public static float Apply(float authored) => baseInertia < 0f ? authored : baseInertia;
}
