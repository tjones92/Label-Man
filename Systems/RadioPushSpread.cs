using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §10), off unless named.
/// --radio-push-spread=F compresses the weekly label push that <c>UpdateRadioHeat</c> reads toward <see cref="Pivot"/>.
///
/// 1960 Top 10 CR4 runs 37.85% over five seeds against a historical 28%. The excess is a Big-5 conversion premium: at
/// equal quality, launch stock and launch awareness a Major record reaches the Top 10 three to five times as often.
/// Push from <see cref="ChartSimulator.GetCampaignImpact"/> is ~0.79 for a Major against ~0.36 for an Independent,
/// worth 1.31x radio heat at q = .75 and ~3.8x airplay under the fifth-power exponent. The probe narrows only that
/// spread: <c>push' = Pivot + F * (push - Pivot)</c>. Launch awareness, launch stock, album and promo reads of
/// GetCampaignImpact are untouched, so the probe isolates the one diagnosed channel.
/// </summary>
public static class RadioPushSpread {
	public const string SwitchPrefix = "--radio-push-spread=";
	// Hot 100 entry-weighted mean push from the ApplyTierStats midpoints and the 1960 tier mix of chart entries
	// (Major .79, MidTier .53, Independent .36, Boutique .32, Small .22). Pivoting there keeps the average charting
	// record's heat level roughly where it is, so the probe moves the tier spread rather than overall airplay.
	public const float Pivot = 0.48f;
	private static bool configured;
	private static float factor = 1f;
	public static float Factor => factor;
	public static bool Enabled => factor < 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --radio-push-spread once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed >= 0f && parsed <= 1f))
				throw new ArgumentException("--radio-push-spread needs a factor in [0, 1].");
			factor = parsed;
		}
		configured = true;
	}

	internal static void SetFactorForChecks(float value) { configured = true; factor = value; }

	/// <summary>Identity when off, so a run without the switch is byte-identical to one built before it.</summary>
	public static float Apply(float push) =>
		factor >= 1f ? push : Mathf.Clamp(Pivot + factor * (push - Pivot), 0f, 1f);
}
