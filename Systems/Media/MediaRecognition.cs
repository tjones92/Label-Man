using System;
using System.Collections.Generic;

/// <summary>
/// Media → act recognition (SimTools/MediaRecognitionAdapterDesign.md), off unless named.
/// <para>
/// A TV airing makes the act's name known to the people who saw it for the first time in a while. Each act keeps a
/// decaying stock of people who have seen it recently (<see cref="MediaActTally.RecognitionReach"/>, same clock as
/// recognition); an airing deposits <see cref="K"/> × the growth of that stock as a share of the population, times a
/// performance factor, up to <see cref="AnnualCap"/> a calendar year. A weekly regular stops earning; an act back after
/// years earns again. Familiarity, not esteem: a disaster makes a face known too, so no deposit is negative.
/// </para>
/// <list type="bullet">
/// <item><c>--observe-media-recognition</c> (shadow): the reach stock and the would-be gain are media-owned telemetry
/// (<see cref="MediaActTally.ShadowRecognition"/>); <see cref="SimulatedArtist.publicRecognition"/> is never touched,
/// so every economy CSV must match the control.</item>
/// </list>
/// A causal switch does not exist yet; it waits on the shadow pass. Draws no RNG.
/// </summary>
public static class MediaRecognition {
	public const string ShadowSwitch = "--observe-media-recognition";

	/// <summary>A first ordinary Sullivan spot (≈0.14 of the population) deposits about one Top 40 milestone (0.022).</summary>
	public const float K = 0.15f;
	/// <summary>At most about a Top 10 plus a Top 40's worth of familiarity a year from television.</summary>
	public const float AnnualCap = 0.06f;

	private static bool configured;
	private static bool shadow;
	public static bool Shadow => shadow && MediaAtmosphere.Observing;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == ShadowSwitch) shadow = true;
			if (argument == "--media-recognition")
				throw new ArgumentException("--media-recognition: the causal writer waits on the shadow pass (MediaRecognitionAdapterDesign.md).");
		}
		configured = true;
	}

	internal static void SetForChecks(bool on) { shadow = on; configured = true; }

	/// <summary>The performance factor: a bad night still makes the name known, just less of it.</summary>
	public static float PerformanceFactor(MediaPerformanceResult result) => result?.Outcome switch {
		PerformanceOutcome.Breakdown => 0.5f,
		PerformanceOutcome.Weak => 0.7f,
		PerformanceOutcome.Engaging => 1.15f,
		PerformanceOutcome.Electrifying => 1.35f,
		_ => 1f,
	};

	/// <summary>
	/// One airing's raw gain before the act's diminishing returns: K × growth of the recently-seen stock as a share of
	/// the population × performance, clipped to what is left of the year's cap. Returns the new reach stock too.
	/// </summary>
	public static (float Gain, float Clipped, double Seen) Deposit(double seen, double freshReach, double population,
		MediaPerformanceResult performance, float depositedThisYear) {
		if (population <= 0 || freshReach <= 0) return (0f, 0f, seen);
		double after = BroadcastExposure.CombineReach(seen, freshReach, population);
		float raw = (float)(K * Math.Max(0.0, after - seen) / population) * PerformanceFactor(performance);
		float room = Math.Max(0f, AnnualCap - depositedThisYear);
		float gain = Math.Min(raw, room);
		return (gain, raw - gain, after);
	}
}
