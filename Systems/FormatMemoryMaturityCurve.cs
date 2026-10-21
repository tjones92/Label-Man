using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Audit A1 (SimTools/EconomyReleaseJudgementAudit-2026-10-20.md): the share of a release's lifetime label net
/// (revenue before the one-time production cost) that it has earned by a given age. Responsive format memory
/// divides net-to-date by this to project a provisional outcome. The frozen route uses a linear
/// <c>(age + 1) / 52</c> (Album) or <c>/ 20</c> (Single); album sales are front-loaded, so at age 13 that
/// projected 2.3x the final outcome on every release and pushed the late decade toward LPs.
/// <para>
/// Off by default (<c>--format-memory-maturity-curve</c> turns it on). With it off,
/// <see cref="CompetitorManager"/> evaluates the original linear expression and every run is byte-identical.
/// </para>
/// <para>
/// Knots are medians of lifetimeLabelNet(age) / lifetimeLabelNet(final) over memory-eligible releases of the
/// reference decade run <c>dec-awrw-on-1001-522</c> (format-memory-revisions joined to release-outcomes;
/// lifetimeLabelNet(age) = realizedNetToDate + sunkProductionCost). Interquartile ranges are tight:
/// <list type="bullet">
/// <item>Album (AlbumComponent lane): age 13 0.534 (IQR 0.497-0.569, n 30,368; linear said 0.269),
/// age 26 0.807 (0.782-0.830; linear 0.519), age 52 0.972 (0.966-0.979; linear 1.0). Median final age 75.</item>
/// <item>Single (OrphanSingle / PromoSingle, knot = n-weighted pool): age 13 0.740 / 0.758 -> 0.750 (IQR ~0.70-0.79,
/// n 21,634 / 36,486; linear 0.70), age 26 0.971 / 0.974 -> 0.973 (linear 1.0). Median final age 33.</item>
/// </list>
/// Between knots the curve is linear, from (age -1, 0) and closing at 1.0 near the median final age. The
/// memory only revises at ages 13, 26, 52 and retirement, so interpolation matters only for the other
/// component of an album project, read at whatever age it happens to be. Soundtracks and cast albums decay
/// far more slowly and stay under-projected by this curve; they are a small share of albums.
/// </para>
/// </summary>
public static class FormatMemoryMaturityCurve {
	public const string Argument = "--format-memory-maturity-curve";

	private static bool configured;
	private static bool enabled;

	public static bool Enabled => enabled;

	private static readonly (float Age, float Fraction)[] AlbumKnots =
		{ (-1f, 0f), (13f, .534f), (26f, .807f), (52f, .972f), (78f, 1f) };
	private static readonly (float Age, float Fraction)[] SingleKnots =
		{ (-1f, 0f), (13f, .750f), (26f, .973f), (34f, 1f) };

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>())
			if (argument == Argument) enabled = true;
		if (LabelCorporateModel.FullBundleRequested(arguments)) enabled = true;
		configured = true;
	}

	/// <summary>Fraction of lifetime label net earned by <paramref name="age"/> weeks after release, clamped to [.05, 1].</summary>
	public static float Fraction(bool album, float age) {
		(float Age, float Fraction)[] knots = album ? AlbumKnots : SingleKnots;
		float fraction = 1f;
		for (int i = 1; i < knots.Length; i++) {
			if (age > knots[i].Age) continue;
			(float a0, float f0) = knots[i - 1];
			(float a1, float f1) = knots[i];
			fraction = f0 + (f1 - f0) * (Mathf.Max(age, a0) - a0) / (a1 - a0);
			break;
		}
		return Mathf.Clamp(fraction, .05f, 1f);
	}

	internal static bool CaptureSwitch() => enabled;

	internal static void ConfigureForProbe(bool enable) {
		configured = true;
		enabled = enable;
	}
}
