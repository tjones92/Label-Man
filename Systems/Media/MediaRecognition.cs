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

	/// <summary>
	/// Celebrity fanbase Phase 2 (SimTools/CelebrityFanbaseDesign.md §3-§4): with either mode above, an airing deposits the
	/// people who saw the act fresh and registered it, with no yearly cap; mainstream, specialist and local-daily items
	/// deposit their registered readers once per root event; a public controversy deposits notoriety; and fan and
	/// specialist readers join followings. Without it the capped adapter runs as before.
	/// </summary>
	public const string RegistrationSwitch = "--fame-registration";

	/// <summary>Mainstream readers who register an act from one item (design §4: circulation × 0.2).</summary>
	public const float MainstreamRegistration = 0.2f;
	/// <summary>A local daily's music page is read by few of its subscribers: the first clipping, small.</summary>
	public const float LocalDailyRegistration = 0.05f;
	/// <summary>A national defiance (severity 0.6) makes the act about as known as a Top 40 hit does (0.022): notoriety is familiarity.</summary>
	public const float NotorietyGain = 0.035f;
	/// <summary>An airing worth a Top 10 on its own (raw, before diminishing returns): a television breakout (design §9).</summary>
	public const float BreakoutGain = ArtistRecognitionService.Top10Gain;

	private static bool configured;
	private static bool shadow;
	private static bool causal;
	private static bool registration;
	/// <summary>Phase 2 writers: registration in place of the cap, press and controversy deposits.</summary>
	public static bool Registration => registration && Active;
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
			if (argument == RegistrationSwitch) registration = true;
		}
		if (registration && !causal && !shadow)
			throw new ArgumentException($"{RegistrationSwitch} changes the recognition adapter; name {ShadowSwitch} or {CausalSwitch} with it.");
		if (causal && shadow)
			throw new ArgumentException($"{CausalSwitch} and {ShadowSwitch} cannot be used together; the causal run prints the same telemetry.");
		if (causal && !ArtistRecognition.Enabled)
			throw new ArgumentException($"{CausalSwitch} requires artist recognition to be enabled.");
		if (causal && !MediaAtmosphere.Causal)
			throw new ArgumentException($"{CausalSwitch} is an economic writer; it requires {MediaAtmosphere.CausalSwitch}.");
		configured = true;
	}

	internal static void SetForChecks(bool on, bool writeCausal = false, bool withRegistration = false) {
		shadow = on && !writeCausal; causal = on && writeCausal; registration = on && withRegistration; configured = true;
	}

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

	/// <summary>
	/// Phase 2 (design §3.1): the people an airing registers, with no yearly cap. Fresh people are the growth of the act's
	/// recently-seen stock, so a return to the same audience adds almost nothing; each of them remembers the act at r0 ×
	/// audience fit (weighted by the airing's young and adult viewers) × performance × novelty × attention. Returns the
	/// registered people and the new reach stock.
	/// </summary>
	public static (double Registered, double Seen) Registered(double seen, double freshReach, double population, double youngShare,
		double adultShare, Genre genre, MediaPerformanceResult performance, int nationalDatesBefore, float attention) {
		if (population <= 0 || freshReach <= 0) return (0, seen);
		double after = BroadcastExposure.CombineReach(seen, freshReach, population);
		double fresh = Math.Max(0.0, after - seen);
		if (!MediaFans.EventHype)
			return (MediaMemberFame.RegisteredViewers(fresh, youngShare, adultShare, genre, performance, nationalDatesBefore, attention), after);
		double registered = fresh * MediaFollowing.R0 * RegistrationFit * MediaFollowing.RegistrationPerformance(performance)
			* MediaFollowing.Novelty(nationalDatesBefore) * attention;
		return (registered, after);
	}

	/// <summary>
	/// Phase 2c experiment (design §3.4.2, `--event-hype` only; screened, not adopted): familiarity ignores taste, so act
	/// registration reads one constant fit. It moved Soul +1.4 to +2.7 points on both screen seeds. Without the switch,
	/// registration reads <see cref="MediaFollowing.AudienceFit"/> as Phase 2 was screened.
	/// </summary>
	public const float RegistrationFit = 0.6f;

	/// <summary>
	/// Readers an item registers for the act (design §4): mainstream circulation × 0.2, specialist circulation × r0 × the
	/// paper's fit, a local daily's × 0.05. Teen fan magazines write followings and faces (§5), not the act's name; trades
	/// are business evidence; critics write standing, which §12.4 hasn't opened, so all three register nobody here.
	/// </summary>
	public static double RegisteredReaders(PressOutlet outlet, Genre genre) {
		if (outlet == null) return 0;
		double readers = Math.Max(0f, outlet.ReadersMillions) * 1e6 * Math.Clamp(outlet.Fit(genre), 0f, 1f);
		return outlet.Family switch {
			PressFamily.Mainstream => readers * MainstreamRegistration,
			PressFamily.Specialist => readers * MediaFollowing.R0,
			PressFamily.LocalDaily => readers * LocalDailyRegistration,
			_ => 0,
		};
	}

	/// <summary>
	/// What a story adds once its root has already been credited (§4, directive §12.4): a story and its retellings count once,
	/// at the strength of the widest outlet that ran it. Returns the extra people and the root's new credit.
	/// </summary>
	public static (double Extra, double Credit) RootBudget(double credited, double registered) =>
		registered > credited ? (registered - credited, registered) : (0, credited);
}

/// <summary>People already credited to one root event's recognition (§4 root-event budget). Media-owned.</summary>
public sealed class FameRoot {
	public int Day;
	public double People;
}
