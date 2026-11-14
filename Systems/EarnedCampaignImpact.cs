using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (major-premium-v1 follow-up, 2026-11-11), off unless named.
/// --earned-campaign-impact=C lets an AI label that keeps charting earn national promotion capacity.
///
/// A label's campaign impact (marketing x budget, <see cref="ChartSimulator.GetCampaignImpact"/>) is drawn once at founding
/// and never moves: neither a run of hits nor promotion to MidTier changes it. Reach and coverage already grow through the
/// distribution mechanic (independent distributors, P&D deals, wholesale houses); promotion power did not. History's
/// Majors lost Top 10 share after 1960 because national independents rose (Liberty, Parkway, ABC-Paramount, Vee-Jay,
/// Motown; HistoricalLabelTierBalanceResearch.md section 17k). Under the launch caps a Major release converts no better
/// than a MidTier one; the remaining excess is an Independent field whose newcomers enter the Hot 100 at 7%.
///
/// Monthly, a label's earned share s moves <see cref="MonthlyRate"/> of the way toward min(1, Hot 100 records in the last
/// 52 weeks / <see cref="RecordsForFullShare"/>), so it rises with sustained charting and fades when the label goes cold.
/// Impact = founding + max(0, C - founding) x s: founding is the floor, and a label already at or above C (the Majors) is
/// unchanged. It reads chart records only; reach, coverage and cash are untouched. The player's label never earns.
/// </summary>
public static class EarnedCampaignImpact {
	public const string SwitchPrefix = "--earned-campaign-impact=";
	// Evidence for the full share, in Hot 100 records over 52 weeks. Default: the MidTier -> Major gate's Major-level volume
	// (16). major-earned-v1 (seed 2002, 1961): only 1 non-Major reaches 16 (best Independent 8; 7 Independents >= 4), so at
	// 16 the median Independent launch impact moved .299 -> .308. --earned-campaign-records=R sets a national-independent bar.
	public const string RecordsPrefix = "--earned-campaign-records=";
	public static float RecordsForFullShare { get; private set; } = 16f;
	// A quarter of the gap a month: about half a year to most of the way, and the same to fade.
	public const float MonthlyRate = 0.25f;
	private static bool configured;
	public static float Ceiling { get; private set; }
	public static bool Enabled => Ceiling > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --earned-campaign-impact once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed > 0f && parsed <= 1f))
				throw new ArgumentException("--earned-campaign-impact needs a ceiling in (0, 1].");
			Ceiling = parsed;
		}
		var records = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(RecordsPrefix, StringComparison.Ordinal)).ToArray();
		if (records.Length > 1) throw new ArgumentException("Name --earned-campaign-records once.");
		if (records.Length == 1) {
			if (!Enabled) throw new ArgumentException(RecordsPrefix + " requires " + SwitchPrefix);
			if (!float.TryParse(records[0].Substring(RecordsPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ||
				!(r >= 1f && r <= 52f))
				throw new ArgumentException("--earned-campaign-records needs a record count in [1, 52].");
			RecordsForFullShare = r;
		}
		configured = true;
	}

	internal static void SetCeilingForChecks(float value) { configured = true; Ceiling = value; }
	internal static void SetRecordsForChecks(float value) { configured = true; RecordsForFullShare = value; }

	/// <summary>The monthly step. Off, or for the player: nothing is written.</summary>
	public static void Update(AILabel label, int recentChartingRecords) {
		if (!Enabled || label == null || label.isPlayerOwned) return;
		float target = Mathf.Clamp(recentChartingRecords / RecordsForFullShare, 0f, 1f);
		label.earnedCampaignShare = Mathf.Clamp(label.earnedCampaignShare + MonthlyRate * (target - label.earnedCampaignShare), 0f, 1f);
	}

	/// <summary>Identity when off or with nothing earned, so a run without the switch is byte-identical.</summary>
	public static float Apply(float foundingImpact, AILabel label) {
		if (!Enabled || label == null || label.isPlayerOwned || label.earnedCampaignShare <= 0f || foundingImpact >= Ceiling) return foundingImpact;
		return foundingImpact + (Ceiling - foundingImpact) * label.earnedCampaignShare;
	}
}
