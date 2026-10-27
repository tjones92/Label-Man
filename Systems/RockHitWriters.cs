using System;
using Godot;

/// <summary>
/// Calibration probe (SimTools/ReleaseJudgementDirective.md, 2026-10-19 genre hook audit), off unless named.
/// --rock-hit-writers gives rock &amp; roll and vocal-group records the professional hit writers they had.
///
/// No publishing office focused on RockAndRoll or DooWop, so their professional pool was empty and R&amp;R's 5%
/// professional prior (the rock-family default) was redistributed to self-writing and covers. 1956-63 rock &amp; roll
/// ran heavily on staff writers (the Hill &amp; Range / Leiber-Stoller / Aldon pattern). Under the switch: one more
/// NY-scale office with RockAndRoll, DooWop and RnB focus; an R&amp;R source anchor that starts professional-led and
/// hands over to self-writing by 1969; and an R&amp;R availability that follows the Brill peak. Read once from the
/// command line, so it holds before the catalogue is generated.
/// </summary>
public static class RockHitWriters {
	public const string Switch = "--rock-hit-writers";
	public static readonly bool Enabled = Array.IndexOf(SimulationArguments.Get(), Switch) >= 0;

	public static readonly Genre[] OfficeFocus = { Genre.RockAndRoll, Genre.DooWop, Genre.RnB };

	/// <summary><c>--rock-hit-writers-share=S</c> scales R&amp;R's professional anchor share (1960 .35, 1969 .15); the source mix
	/// renormalises, so the rest goes to the other sources in proportion. Default 1. The office's catalogue is unchanged.</summary>
	public const string SharePrefix = "--rock-hit-writers-share=";
	public static readonly float ProShareScale = ReadShareScale();

	private static float ReadShareScale() {
		string arg = Array.Find(SimulationArguments.Get(), a => a.StartsWith(SharePrefix, StringComparison.Ordinal));
		if (arg == null) return 1f;
		if (!Enabled) throw new ArgumentException(SharePrefix + " needs " + Switch + ".");
		if (!float.TryParse(arg.Substring(SharePrefix.Length), System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out float s) || !(s >= 0f && s <= 2f))
			throw new ArgumentException(SharePrefix + " needs a value in [0, 2].");
		return s;
	}

	/// <summary>R&amp;R professional availability: high through the Brill years, easing as bands write their own.</summary>
	public static float RockAndRollAvailability(float smoothedLateShift) => Mathf.Clamp(0.60f - 0.30f * smoothedLateShift, 0f, 1f);
}
