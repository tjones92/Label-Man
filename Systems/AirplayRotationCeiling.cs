using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17), off unless named.
/// --airplay-rotation-ceiling=C soft-caps a region's own rotation at about C before
/// <see cref="ChartSimulator.CalculateChartPoints"/> raises it to the fifth power.
///
/// The published Top 10 runs #1 at 6.4x #10 against Hesbacher's 1.91x. Units are nearly flat across the Top 10;
/// airplay is 17-24x, so the gaps between ranks are up to five times Hesbacher's at the top. Peaks per rank follow
/// those gaps, so records pile up at #1-3 and too few peak at #7-10, which is the whole Top 10 duration excess
/// (stays per peak band already match history). Heavy rotation physically saturated -- every record in a station's
/// top ten was on the hourly cycle -- so the probe bends rotation over above C and leaves it alone well below.
/// Only the chart-points payout reads it: radio heat, regional radioPlay, awareness and demand are untouched.
/// </summary>
public static class AirplayRotationCeiling {
	public const string SwitchPrefix = "--airplay-rotation-ceiling=";
	// Sharpness of the soft minimum x / (1 + (x/C)^m)^(1/m). At m = 8 rotation at 0.7C loses 0.7% (3.4% of airplay
	// after the fifth power), at C it is 0.917C, and above 1.5C it is within 0.5% of C.
	public const float Sharpness = 8f;
	private static bool configured;
	private static float ceiling;
	public static float Ceiling => ceiling;
	public static bool Enabled => ceiling > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --airplay-rotation-ceiling once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed > 0f && parsed <= 1f))
				throw new ArgumentException("--airplay-rotation-ceiling needs a rotation level in (0, 1].");
			ceiling = parsed;
		}
		configured = true;
	}

	internal static void SetCeilingForChecks(float value) { configured = true; ceiling = value; }

	/// <summary>Identity when off, so a run without the switch is byte-identical to one built before it.</summary>
	public static float Apply(float ownRotation) =>
		ceiling <= 0f || ownRotation <= 0f
			? ownRotation
			: ownRotation / Mathf.Pow(1f + Mathf.Pow(ownRotation / ceiling, Sharpness), 1f / Sharpness);
}
