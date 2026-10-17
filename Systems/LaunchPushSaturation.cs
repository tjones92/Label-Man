using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17n), off unless named.
/// --launch-push-saturation=F saturates the campaign impact a record's LAUNCH reads above <see cref="Knee"/>.
///
/// The Big-5 enter the Hot 100 on 50% of their singles against 22% for MidTier and 12% for Independents, at equal
/// career state and in every genre. Quality, launch stock, launch awareness and campaign impact reproduce the newcomer
/// gap exactly, and campaign impact is the largest of the four. The edge then compounds on the shared career ladder
/// into a home-grown senior roster. The probe caps what a national campaign buys at launch:
/// <c>impact' = Knee + F * (impact - Knee)</c> above the knee, unchanged below it. Only the launch radio push and the
/// launch marketing-awareness term read it; weekly push, the top-20 hold, launch stock and distribution are untouched.
/// One-sided on purpose, so labels below the knee (Independents, Small, Boutique, most MidTiers, the player) launch
/// with bit-identical inputs.
/// </summary>
public static class LaunchPushSaturation {
	public const string SwitchPrefix = "--launch-push-saturation=";
	// Just above the MidTier mean campaign impact (0.52), so regional-label scale is left alone.
	public const float Knee = 0.55f;
	private static bool configured;
	private static float factor = 1f;
	public static float Factor => factor;
	public static bool Enabled => factor < 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --launch-push-saturation once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed >= 0f && parsed <= 1f))
				throw new ArgumentException("--launch-push-saturation needs a factor in [0, 1].");
			factor = parsed;
		}
		configured = true;
	}

	internal static void SetFactorForChecks(float value) { configured = true; factor = value; }

	/// <summary>Identity when off or at or below the knee, so a run without the switch is byte-identical.</summary>
	public static float Apply(float impact) =>
		factor >= 1f || impact <= Knee ? impact : Mathf.Clamp(Knee + factor * (impact - Knee), 0f, 1f);
}
