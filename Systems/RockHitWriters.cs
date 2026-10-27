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

	/// <summary>R&amp;R professional availability: high through the Brill years, easing as bands write their own.</summary>
	public static float RockAndRollAvailability(float smoothedLateShift) => Mathf.Clamp(0.60f - 0.30f * smoothedLateShift, 0f, 1f);
}
