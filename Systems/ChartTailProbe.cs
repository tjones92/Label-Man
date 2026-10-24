using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (LabelReleaseProgrammeImplementation.md, "Post-peak tail probe"), off unless named.
/// <c>--post-peak-demand-decay=D</c> multiplies a charted single's regional conversion by
/// D^(weeksSincePeakUnits - G) once it is more than G weeks past its sales peak; <c>--post-peak-demand-grace=G</c>
/// sets G (default 3, needs the decay switch). <c>--post-peak-demand-decay-top=T</c> gives a record that peaked at #1
/// the rate T instead, interpolated linearly to D at <c>--post-peak-demand-top-rank=R</c> + 1 (default R = 3), so #2
/// to #R sit between.
///
/// On the working bundle a Top 10 record's chart points fall a steady ~0.8x a week after its peak, so records that
/// peaked at #4-10 are still #19-37 eight weeks later with 3-5x the #100 cutoff, where history's are off the chart.
/// History's tracks the model for about four weeks and then accelerates. The term is keyed to the record's own
/// sales peak, so it is exactly 1 while a record is still setting weekly highs, and only to records that have
/// charted (peakPosition > 0), so a slow regional breakout that has not reached the Hot 100 keeps its clock.
///
/// A single rate made #1 records leave too early (4.1 Top 10 weeks after the peak against history's 4.9, 2.1 weeks
/// at #1 against 2.6): history's biggest hits faded more slowly than its #4-10 peakers. Without the top switch the
/// rate is D exactly, so tail-only runs are unchanged.
/// Off, the multiply is skipped entirely, so a run without the switch is byte-identical.
/// </summary>
public static class ChartTailProbe {
	public const string DecayPrefix = "--post-peak-demand-decay=";
	public const string GracePrefix = "--post-peak-demand-grace=";
	public const string TopPrefix = "--post-peak-demand-decay-top=";
	public const string TopRankPrefix = "--post-peak-demand-top-rank=";
	public const int DefaultGraceWeeks = 3;
	public const int DefaultTopRank = 3;
	private static bool configured;
	private static float decay = -1f;
	private static int graceWeeks = DefaultGraceWeeks;
	private static float topDecay = -1f;
	private static int topRank = DefaultTopRank;
	public static bool Enabled => decay > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string rate = Single(args, DecayPrefix);
		if (rate != null) {
			if (!float.TryParse(rate, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed > 0f && parsed < 1f))
				throw new ArgumentException(DecayPrefix + " needs a value in (0, 1).");
			decay = parsed;
		}
		string grace = Single(args, GracePrefix);
		if (grace != null) {
			if (rate == null) throw new ArgumentException(GracePrefix + " needs " + DecayPrefix + ".");
			if (!int.TryParse(grace, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weeks) || weeks < 0 || weeks > 26)
				throw new ArgumentException(GracePrefix + " needs an integer in [0, 26].");
			graceWeeks = weeks;
		}
		string top = Single(args, TopPrefix), topRankRaw = Single(args, TopRankPrefix);
		if ((top != null || topRankRaw != null) && rate == null) throw new ArgumentException(TopPrefix + " and " + TopRankPrefix + " need " + DecayPrefix + ".");
		if (topRankRaw != null && top == null) throw new ArgumentException(TopRankPrefix + " needs " + TopPrefix + ".");
		if (top != null) {
			if (!float.TryParse(top, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed > 0f && parsed < 1f))
				throw new ArgumentException(TopPrefix + " needs a value in (0, 1).");
			topDecay = parsed;
		}
		if (topRankRaw != null) {
			if (!int.TryParse(topRankRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank) || rank < 1 || rank > 40)
				throw new ArgumentException(TopRankPrefix + " needs an integer in [1, 40].");
			topRank = rank;
		}
		configured = true;
	}

	private static string Single(string[] args, string prefix) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + prefix + " once.");
		return values.Length == 1 ? values[0].Substring(prefix.Length) : null;
	}

	internal static void SetForChecks(float rate, int grace = DefaultGraceWeeks, float top = -1f, int rank = DefaultTopRank) {
		configured = true; decay = rate; graceWeeks = grace; topDecay = top; topRank = rank;
	}

	/// <summary>The weekly rate for a record whose best position is <paramref name="peakPosition"/>.</summary>
	internal static float Rate(int peakPosition) =>
		topDecay <= 0f || peakPosition > topRank ? decay : topDecay + (decay - topDecay) * (peakPosition - 1) / (float)topRank;

	/// <summary>The conversion multiplier for a single; 1 for uncharted records and inside the grace.</summary>
	public static float Multiplier(int peakPosition, int weeksSincePeakUnits) =>
		!Enabled || peakPosition <= 0 || weeksSincePeakUnits <= graceWeeks ? 1f : Mathf.Pow(Rate(peakPosition), weeksSincePeakUnits - graceWeeks);
}
