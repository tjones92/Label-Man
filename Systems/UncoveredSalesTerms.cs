using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// --uncovered-sales-terms=W: an AI label's sales in a market it has no distribution in (no own branch, no wholesale
/// line, no P&amp;D grant for the record) are paid on one-stop terms -- billed at --uncovered-sales-honesty=H (default
/// 0.85) of the units' revenue and collected W weeks later -- instead of landing as cash that week at full value.
/// In 1960 every route to a store in a market without your own distributor went through a one-stop, rack jobber or a
/// distributor's sub-account, and all of them paid on 30-120 day terms and under-reported. Off, an uncovered market
/// paid better than a wholesale house (12-18 weeks at reporting honesty ~0.77), so taking a house line for a hit cost
/// the label money (SimTools/MajorTop10ShareFableHandoff.md, chart-proven wholesale). The receivable goes on the
/// ordinary ledger (label.wholesaleReceivables, distributor id "onestop_&lt;region&gt;"), so the receivable-backed
/// overdraft, factoring, tax and the corporate accounts read it unchanged; there is no house behind it, so it is
/// collected in full at maturity. The player's uncovered sales settle through the trunk, not here. Draws no random numbers.
/// </summary>
public static class UncoveredSalesTerms {
	public const string Switch = "--uncovered-sales-terms";
	public const string HonestySwitch = "--uncovered-sales-honesty";
	public const string DistributorPrefix = "onestop_";
	private static bool configured;
	/// <summary>Payment term in weeks; 0 when off.</summary>
	public static int Weeks { get; private set; }
	/// <summary>Share of the units' revenue the one-stop admits to and pays.</summary>
	public static float Honesty { get; private set; } = .85f;
	public static bool Enabled => Weeks > 0;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Weeks = 0; Honesty = .85f;
		bool honestyGiven = false;
		foreach (string arg in arguments ?? Array.Empty<string>()) {
			if (arg.StartsWith(Switch + "=", StringComparison.Ordinal)) {
				if (!int.TryParse(arg.Substring(Switch.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) ||
					w < 1 || w > 52)
					throw new ArgumentException($"{Switch} must be a whole number of weeks from 1 to 52.");
				Weeks = w;
			}
			if (arg.StartsWith(HonestySwitch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(HonestySwitch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float h) ||
					!float.IsFinite(h) || h <= 0f || h > 1f)
					throw new ArgumentException($"{HonestySwitch} must be above 0 and at most 1.");
				Honesty = h; honestyGiven = true;
			}
		}
		if (honestyGiven && Weeks <= 0) throw new ArgumentException($"{HonestySwitch} requires {Switch}.");
		if (Enabled) Godot.GD.Print($"UNCOVERED_SALES_TERMS on weeks={Weeks} honesty={Honesty.ToString("F2", CultureInfo.InvariantCulture)}");
		configured = true;
	}

	internal static void SetForChecks(int weeks, float honesty = .85f) { configured = true; Weeks = weeks; Honesty = honesty; }

	/// <summary>Whether this label's uncovered sales go on terms. False when off and for the player.</summary>
	public static bool AppliesTo(AILabel label) => Enabled && label != null && !label.isPlayerOwned;

	/// <summary>
	/// Splits this week's revenue for a label by unit share. Returns (billed, collectable) per uncovered region: billed is the
	/// revenue the units carried, collectable = billed x Honesty. Regions are uncovered when
	/// <paramref name="isCovered"/>(regionId, recordId) is false. Empty when off, for the player, or with no units.
	/// </summary>
	public static Dictionary<string, (float Billed, float Collectable)> Split(AILabel label, float weeklyRevenue,
		IEnumerable<(string RecordId, string RegionId, int Units)> clearedUnits, Func<string, string, bool> isCovered) {
		var result = new Dictionary<string, (float Billed, float Collectable)>(StringComparer.Ordinal);
		if (!AppliesTo(label) || weeklyRevenue <= 0f || clearedUnits == null) return result;
		long total = 0;
		var uncovered = new Dictionary<string, long>(StringComparer.Ordinal);
		foreach (var (recordId, regionId, units) in clearedUnits) {
			if (units <= 0) continue;
			total += units;
			if (isCovered(regionId, recordId)) continue;
			uncovered[regionId] = uncovered.GetValueOrDefault(regionId) + units;
		}
		if (total <= 0) return result;
		foreach (var pair in uncovered) {
			float billed = weeklyRevenue * (pair.Value / (float)total);
			if (billed > 0f) result[pair.Key] = (billed, billed * Honesty);
		}
		return result;
	}
}
