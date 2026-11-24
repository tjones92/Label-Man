using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/Top10TurnoverFindings.md §11.7, #1 reign length), off unless named.
/// A region's rotation follows Lerp(radioPlay × hold, target, lerp) with hold 0.92 and lerp 0.15. That carries 0.78 of
/// last week forward (about a 4.6-week time constant) and settles at lerp / (1 − hold·(1 − lerp)) ≈ 0.69 of the target.
/// The slow carry is what keeps airplay up after a record's sales peak, but it also makes airplay arrive two to three
/// weeks after sales. A record that sells its way to #1 at release age 7 then gains a doubling of airplay while it
/// sits there, and holds #1 for four weeks or more.
/// <c>--regional-radio-rise-lerp=L</c> changes only the rising weeks. While the authored settle level (ratio × target)
/// is above this week's rotation, rotation moves toward that same level at rate L. The level a record settles at is
/// unchanged and falling weeks keep the authored hold. L is at least 0.25, above the authored rise rate (1 − 0.78 =
/// 0.218), so a rising week is never slower than today. No random draws, so the probe is byte-identical when off.
/// </summary>
public static class RegionalRadioRise {
	public const string Switch = "--regional-radio-rise-lerp=";
	private static bool configured;
	private static float riseLerp = -1f;
	public static bool Enabled => riseLerp > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(Switch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + Switch.TrimEnd('=') + " once.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0].Substring(Switch.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
				|| !float.IsFinite(v) || v < 0.25f || v > 1f)
				throw new ArgumentException(Switch.TrimEnd('=') + " needs a value in [0.25, 1].");
			riseLerp = v;
			GD.Print(FormattableString.Invariant($"REGIONAL_RADIO_RISE lerp={riseLerp}"));
		}
		configured = true;
	}

	internal static void SetForChecks(float value) { configured = true; riseLerp = value; }

	/// <summary>The level the authored update settles at for a fixed target: lerp / (1 − hold·(1 − lerp)).</summary>
	public static float SettleRatio(float hold, float lerp) => lerp / (1f - hold * (1f - lerp));

	/// <summary>This week's rotation for a region still carrying the record. Off, or in a falling week, it is the
	/// authored Lerp(current × hold, target, lerp), computed exactly as before.</summary>
	public static float Next(float current, float target, float hold, float lerp) {
		float authored = Mathf.Lerp(current * hold, target, lerp);
		if (!Enabled) return authored;
		float settle = SettleRatio(hold, lerp) * target;
		if (settle <= current) return authored;
		return Mathf.Max(authored, Mathf.Lerp(current, settle, riseLerp));
	}
}
