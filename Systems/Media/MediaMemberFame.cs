using System;
using System.Collections.Generic;

/// <summary>The media-earned part of one member's personal recognition (CelebrityFanbaseDesign.md §5). Media-owned.</summary>
public sealed class MemberFameEntry {
	public string ArtistId, Name;
	/// <summary>
	/// Shadow: the would-be personal recognition television and photographs earned, beside the real stock. Causal: the part
	/// of the real stock they earned. Decays on recognition's clock either way.
	/// </summary>
	public float Earned;
	/// <summary>This calendar year's applied gain by source, for MEDIA_MEMBER_FAME.</summary>
	public int Year;
	public float TvYear, PressYear;
}

/// <summary>
/// An office promise of the camera (§5, audit finding 10): the label puts a member in front of the lens. Billing lasts while
/// the act is still billed under the name it was given; a solo side lasts until <see cref="UntilDay"/>.
/// </summary>
public sealed class CameraPromise {
	public string ArtistId, StageName;
	public bool Billing;
	public int FromDay, UntilDay = -1;
}

/// <summary>
/// Celebrity fanbase Phase 1 (SimTools/CelebrityFanbaseDesign.md §5): the faces the camera and the photographers found
/// become known by name. Each airing of a group deposits its registered viewers (§3.1) into the members'
/// <see cref="Musician.personalRecognition"/> by camera share; each photo session deposits its registered readers into the
/// member photographed, three times over for a cover. This is extra to the act's own recognition and its member share:
/// it is the fans who know the drummer's name. Groups only; a solo act's name is already its person's.
/// <list type="bullet">
/// <item><c>--observe-member-fame</c> (shadow): deposits land in a media-owned ledger (<see cref="MemberFameEntry.Earned"/>);
/// personal recognition is never touched, so every economy CSV must match the control.</item>
/// <item><c>--media-member-fame</c> (causal): the same deposits are written to personal recognition with its diminishing
/// returns, and the band room's SpotlightThem and SoloSingleYes promise the camera instead of minting fame in the office.
/// Requires artist recognition and the causal media mode. Amends directive §12.4 (design §0.3).</item>
/// </list>
/// Draws no RNG beyond the keyed camera shares the spotlight already reads.
/// </summary>
public static class MediaMemberFame {
	public const string ShadowSwitch = "--observe-member-fame";
	public const string CausalSwitch = "--media-member-fame";

	/// <summary>A cover puts the face in front of every reader; a feature's pin-up inside reaches about a third of them (§4).</summary>
	public const float CoverWeight = 3f;
	/// <summary>
	/// Camera weight a promise adds (MediaFans.CameraShares): about a lead singer's pull again, so the promised member
	/// usually leads the frame without owning it.
	/// </summary>
	public const float PromiseCameraWeight = 1f;
	/// <summary>A solo side keeps the member in front of the camera for a year.</summary>
	public const int SoloPromiseDays = 365;
	/// <summary>
	/// The camera's favorite takes most of a night's fame: shares are raised to this power and renormalized. At 1 the
	/// camera spread fame more evenly than the chart's member attribution (a typical lead 36% against 44%), so television
	/// diluted the front person and cut solo spinouts 17 → 9 on the 1963-66 resume (Phase 1 notes, author's decision
	/// 2026-11-01). At 2 the same lead takes about half.
	/// </summary>
	public const float CameraFameExponent = 2f;

	/// <summary>Camera shares sharpened by <see cref="CameraFameExponent"/>, still summing to one.</summary>
	public static List<(Musician Member, float Weight)> FameShares(List<(Musician Member, float Weight)> camera) {
		var list = new List<(Musician Member, float Weight)>(camera.Count);
		float total = 0f;
		foreach (var (member, share) in camera) {
			float w = (float)Math.Pow(Math.Max(0f, share), CameraFameExponent);
			list.Add((member, w));
			total += w;
		}
		if (total <= 0f) return list;
		for (int i = 0; i < list.Count; i++) list[i] = (list[i].Member, list[i].Weight / total);
		return list;
	}

	private static bool configured, shadow, causal;
	public static bool Shadow => shadow && !causal && MediaAtmosphere.Observing;
	public static bool Causal => causal && MediaAtmosphere.Causal && ArtistRecognition.Enabled;
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

	/// <summary>
	/// The people who saw an airing fresh and remember the act (§3.1): fresh reach × r0 × performance × novelty × attention ×
	/// audience fit, the fit weighted by the airing's young and adult viewers.
	/// </summary>
	public static double RegisteredViewers(double fresh, double youngShare, double adultShare, Genre genre,
		MediaPerformanceResult performance, int nationalDatesBefore, float attention) {
		if (fresh <= 0) return 0;
		double fit = youngShare * MediaFollowing.AudienceFit(genre, young: true) + adultShare * MediaFollowing.AudienceFit(genre, young: false);
		return fresh * MediaFollowing.R0 * MediaFollowing.RegistrationPerformance(performance) * MediaFollowing.Novelty(nationalDatesBefore) * attention * fit;
	}

	/// <summary>The readers a photo session registers: circulation × r0 × the outlet's fit for the act, a cover counted three times.</summary>
	public static double RegisteredReaders(double readers, float fit, bool cover) =>
		Math.Max(0.0, readers) * MediaFollowing.R0 * Math.Clamp(fit, 0f, 1f) * (cover ? CoverWeight : 1f);

	/// <summary>stock + raw × (1 − stock): the act's diminishing returns, applied to a person.</summary>
	public static float Gain(float stock, float raw) => Math.Clamp(stock + Math.Max(0f, raw) * (1f - stock), 0f, 1f);
}
