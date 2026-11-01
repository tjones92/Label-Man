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
/// <item><c>--media-recognition</c> (causal): the same deposit goes through
/// <see cref="ArtistRecognitionService.AddPublicRecognition"/>, with its diminishing returns and member share, so it reaches
/// the launch caps and the standing pool. <see cref="MediaActTally.ShadowRecognition"/> then holds the decaying part of the
/// real stock that television earned, for telemetry. Requires artist recognition and the causal media mode
/// (<see cref="MediaAtmosphere.CausalSwitch"/>), since observe mode promises no economic write (§12.1).</item>
/// </list>
/// Draws no RNG.
/// </summary>
public static class MediaRecognition {
	public const string ShadowSwitch = "--observe-media-recognition";
	public const string CausalSwitch = "--media-recognition";

	/// <summary>A first ordinary Sullivan spot (≈0.14 of the population) deposits about one Top 40 milestone (0.022).</summary>
	public const float K = 0.15f;
	/// <summary>At most about a Top 10 plus a Top 40's worth of familiarity a year from television.</summary>
	public const float AnnualCap = 0.06f;

	private static bool configured;
	private static bool shadow;
	private static bool causal;
	/// <summary>The would-be gain is telemetry only.</summary>
	public static bool Shadow => shadow && !causal && MediaAtmosphere.Observing;
	/// <summary>Deposits are written to the act's recognition.</summary>
	public static bool Causal => causal && MediaAtmosphere.Causal && ArtistRecognition.Enabled;
	/// <summary>Either mode: the reach stock is kept and MEDIA_RECOGNITION is printed.</summary>
	public static bool Active => Shadow || Causal;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == ShadowSwitch) shadow = true;
			if (argument == CausalSwitch) causal = true;
		}
		if (causal && shadow)
			throw new ArgumentException($"{CausalSwitch} and {ShadowSwitch} cannot be used together; the causal run prints the same telemetry.");
		if (causal && !ArtistRecognition.Enabled)
			throw new ArgumentException($"{CausalSwitch} requires artist recognition to be enabled.");
		if (causal && !MediaAtmosphere.Causal)
			throw new ArgumentException($"{CausalSwitch} is an economic writer; it requires {MediaAtmosphere.CausalSwitch}.");
		configured = true;
	}

	internal static void SetForChecks(bool on, bool writeCausal = false) { shadow = on && !writeCausal; causal = on && writeCausal; configured = true; }

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
