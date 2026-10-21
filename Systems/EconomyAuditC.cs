using System;
using System.Collections.Generic;
using System.Linq;

// Economy audit section C (SimTools/EconomyReleaseJudgementAudit-2026-10-20.md). Each item is its own
// opt-in switch, off by default and byte-identical when off.

/// <summary>
/// C1: an album project's drop-week campaign keeps the same cash reserve as every other launch (the
/// commitment reserve, or a month's overhead, plus the LP's initial pressing run) instead of a month's
/// overhead alone. The LP still drops; only its campaign is clamped.
/// </summary>
public static class AlbumLaunchReserve {
	public const string Switch = "--album-launch-reserve";
	private static bool configured, enabled;
	public static bool Enabled => enabled;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}
	internal static void SetForChecks(bool value) { configured = true; enabled = value; }
	internal static float ClampCampaign(float planned, float cash, float reserve) => Math.Min(planned, Math.Max(0f, cash - reserve) * .8f);
}

/// <summary>
/// C2: AI records list at period prices. A 45 lists at the player's $0.98 (PlayerDesk.ListPrice), not
/// $0.89, so the act's royalty and the composition pool are figured on the same list as the player's.
/// A stereo LP lists $1 above mono ($4.98 against $3.98), the period premium; every LP is stereo from
/// 1968, when the mono line was dropped at the stereo price. With dealer pricing on, the single's booked
/// dealer price is unchanged ($0.63); the LP's moves with its list. Forecasts keep the mono LP list.
/// </summary>
public static class PeriodListPrices {
	public const string Switch = "--period-list-prices";
	internal const float StereoPremium = 1f;
	private static bool configured, enabled;
	public static bool Enabled => enabled;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}
	internal static void SetForChecks(bool value) { configured = true; enabled = value; }
	/// <summary>Format list price; the authored table's value when off.</summary>
	internal static float FormatList(ReleaseFormat format, float authored) =>
		enabled && format == ReleaseFormat.Single ? PlayerDesk.ListPrice : authored;
	/// <summary>A record's own list price at settlement.</summary>
	internal static float RecordList(Record record, float formatList) =>
		enabled && record?.format == ReleaseFormat.Album && record.album?.isStereo == true ? formatList + StereoPremium : formatList;
}

/// <summary>
/// C3: the HQ studio modifier applies once. GenerateRecordFromArtist blends it into base quality (18%)
/// and then multiplies by it again, so across the authored regions (0.83-1.15) an act of base quality
/// 0.60 records at 0.53 or 0.80 by where its label is headquartered. When on, only the blend remains,
/// in the record generator and in the repertoire forecast that mirrors it.
/// </summary>
public static class SingleStudioModifier {
	public const string Switch = "--single-studio-modifier";
	private static bool configured, enabled;
	public static bool Enabled => enabled;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}
	internal static void SetForChecks(bool value) { configured = true; enabled = value; }
	internal static float BaseQuality(float artistQuality, float studioMod) {
		float blend = artistQuality * .82f + studioMod * .18f;
		return enabled ? blend : blend * studioMod;
	}
}

/// <summary>
/// C4: a record's own chart run no longer counts toward its act's heat. CalculateArtistHeat summed every
/// charting record by the act, including the one being updated, so a record's position fed its own radio
/// target twice, on top of the radio position bonus. When on (live market only, so prewarm is unchanged),
/// the record's own position and peak contributions, and its share of the hit bonus, are taken out of the
/// base heat it reads. Other probes that substitute heat (ArtistHeatAttribution) are unaffected.
/// </summary>
public static class SelfExcludedArtistHeat {
	public const string Switch = "--self-excluded-artist-heat";
	private static bool configured, enabled;
	public static bool Enabled => enabled;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}
	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	/// <summary>One record's contribution to its act's heat, before the hit bonus, and whether it counts as a hit.</summary>
	internal static (float Heat, int Hit) Contribution(int position, int peak) {
		float heat = position > 0 && position <= 10 ? .3f : position > 0 && position <= 40 ? .15f : position > 0 ? .05f : 0f;
		if (peak > 0 && peak <= 10) return (heat + .1f, 1);
		if (peak > 0 && peak <= 40) return (heat + .05f, 1);
		return (heat, 0);
	}

	/// <summary>The act's heat as CalculateArtistHeat computes it, from its summed parts.</summary>
	internal static float Heat(float sum, int hits) => Math.Clamp(sum + Math.Min(hits * .05f, .3f), 0f, 1f);

	internal static float Excluding(float sum, int hits, int position, int peak) {
		var own = Contribution(position, peak);
		return Heat(sum - own.Heat, hits - own.Hit);
	}
}
