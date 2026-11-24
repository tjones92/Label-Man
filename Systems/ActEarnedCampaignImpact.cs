using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Calibration probe (SimTools/MajorTop10ShareFableHandoff.md, 2026-11-12), off unless named.
/// --act-earned-campaign-impact=C lets a non-Major AI label put Major-grade promotion behind a single by an act with a
/// hit record.
///
/// A label's campaign impact is a founding draw (Big-5 and MidTier about 0.55, Independents 0.35, Small/Boutique 0.27)
/// and is the same for every act on the label. At matched act momentum (0.1-0.4) and quality (0.70-0.78), a single
/// reaches the Hot 100 68% of the time at a Big-5 label and 26% at an Independent, so a small label's hit act loses its
/// momentum within two or three singles. In the period a small label with a proven act concentrated what it had on
/// that act's next record.
///
/// At launch a single's share is min(1, act momentum / <see cref="FullMomentum"/>): momentum moves only on chart
/// results, so an act with no hit record earns nothing. The single's campaign impact becomes
/// label + max(0, C - label) x share wherever the label's impact is read for that record: launch marketing awareness,
/// launch radio push, the weekly label push and the commercial-fit push. A label at or above C is unchanged, as is any
/// Major and the player. The share is fixed at launch, so a record's own chart run cannot raise its push.
///
/// Probe state: the per-record share is held in memory and is not saved, so a resumed world loses it. Draws no RNG.
/// </summary>
public static class ActEarnedCampaignImpact {
	public const string SwitchPrefix = "--act-earned-campaign-impact=";
	/// <summary>Act momentum at which the full lift applies (default 0.2: about one Top 10 single and a Top 40).</summary>
	public const string MomentumPrefix = "--act-earned-campaign-momentum=";
	private static bool configured;
	private static readonly Dictionary<string, float> launchShare = new(StringComparer.Ordinal);
	public static float Ceiling { get; private set; }
	public static float FullMomentum { get; private set; } = 0.2f;
	public static bool Enabled => Ceiling > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string[] args = (arguments ?? Array.Empty<string>()).ToArray();
		string c = args.LastOrDefault(a => a.StartsWith(SwitchPrefix, StringComparison.Ordinal));
		if (c != null) {
			if (!float.TryParse(c.Substring(SwitchPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ||
				!(parsed > 0f && parsed <= 1f))
				throw new ArgumentException(SwitchPrefix + " needs a ceiling in (0, 1].");
			Ceiling = parsed;
		}
		string m = args.LastOrDefault(a => a.StartsWith(MomentumPrefix, StringComparison.Ordinal));
		if (m != null) {
			if (!Enabled) throw new ArgumentException(MomentumPrefix + " requires " + SwitchPrefix);
			if (!float.TryParse(m.Substring(MomentumPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ||
				!(parsed > 0f && parsed <= 1f))
				throw new ArgumentException(MomentumPrefix + " needs a momentum in (0, 1].");
			FullMomentum = parsed;
		}
		configured = true;
	}

	internal static void SetForChecks(float ceiling, float fullMomentum = 0.2f) {
		configured = true; Ceiling = ceiling; FullMomentum = fullMomentum; launchShare.Clear();
	}

	/// <summary>The share an act's hit record earns: 0 at no momentum, 1 at <see cref="FullMomentum"/> or more.</summary>
	public static float ShareFor(float momentum) => Math.Clamp(momentum / FullMomentum, 0f, 1f);

	/// <summary>Fix a single's share at its launch. Off, for albums, Majors and the player: nothing is recorded.</summary>
	public static void NoteLaunch(Record record, AILabel label, SimulatedArtist artist) {
		if (!Enabled || record == null || label == null || artist == null || string.IsNullOrEmpty(record.recordId)) return;
		if (record.format != ReleaseFormat.Single || record.isPlayerOwned || label.isPlayerOwned || label.tier == LabelTier.Major) return;
		float share = ShareFor(artist.momentum);
		if (share > 0f) launchShare[record.recordId] = share;
	}

	/// <summary>The record's campaign impact. Identity when off or for a record with no earned share.</summary>
	public static float Apply(float labelImpact, string recordId) {
		if (!Enabled || recordId == null || labelImpact >= Ceiling || !launchShare.TryGetValue(recordId, out float share)) return labelImpact;
		return labelImpact + (Ceiling - labelImpact) * share;
	}
}
