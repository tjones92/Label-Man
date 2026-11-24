using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (major-top10 premium screen, 2026-11-11), off unless named.
/// --launch-stock-spread=F compresses the distribution depth an AI record's LAUNCH stock reads above <see cref="Knee"/>.
///
/// Under the full bundle the Majors issue a historical share of singles (15% in 1961, history ~18%) but each converts to
/// the Top 40 at 2.6x the field's rate against history's ~1.2x. At matched quality a Major newcomer ships 1.8x the initial
/// stock and carries 1.34x the launch awareness; peak radio heat is no higher. The awareness half already has
/// <see cref="LaunchPushSaturation"/>; this is the stock half: <c>depth' = Knee + F * (depth - Knee)</c> above the knee,
/// unchanged below it, read only by <see cref="ChartSimulator.CalculateInitialRegionalStock"/> -- every initial shelf:
/// launches, the 1960 prewarm and a master transfer's reseed. Coverage (which regions a record reaches), reorders, rack
/// access and weekly distribution are untouched. One-sided, so records whose distribution
/// depth is at or below the knee (most MidTiers, Independents, Small, Boutique, the player) launch with identical stock.
/// </summary>
public static class LaunchStockSpread {
	public const string SwitchPrefix = "--launch-stock-spread=";
	// The same knee as launch saturation: just above the MidTier mean, so regional-label depth is left alone.
	public const float Knee = 0.55f;
	private static bool configured;
	private static float factor = 1f;
	public static float Factor => factor;
	public static bool Enabled => factor < 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --launch-stock-spread once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed >= 0f && parsed <= 1f))
				throw new ArgumentException("--launch-stock-spread needs a factor in [0, 1].");
			factor = parsed;
		}
		configured = true;
	}

	internal static void SetFactorForChecks(float value) { configured = true; factor = value; }

	/// <summary>Identity when off, at or below the knee, or for the player, so a run without the switch is byte-identical.</summary>
	public static float Apply(float depth, bool playerOwned) =>
		factor >= 1f || playerOwned || depth <= Knee ? depth : Mathf.Clamp(Knee + factor * (depth - Knee), 0f, 1f);
}
