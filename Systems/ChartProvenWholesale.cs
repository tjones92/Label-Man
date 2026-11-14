using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// --chart-proven-wholesale=N: a record on the national chart at position 1..N is proof for a wholesale house in any
/// market. Off, a house takes a label's line only where one of its records broke out in that market (regional breakout
/// evidence), and a label adds one market at a time on a 35% monthly roll. A market the record has not reached has no
/// sales and so can never show evidence, which left a national hit on an independent label unserved in most of the
/// country (SimTools/MajorTop10ShareFableHandoff.md). In 1960 independent distributors read the trade charts and called
/// the label for a record that was selling nationally. With the switch on, a chart-proven AI label's line is taken that
/// month (or week, with --chart-proven-wholesale-weekly) by an open house in each uncovered market (up to --chart-proven-wholesale-markets=M, default all), through the
/// ordinary placement: house capacity, reach credit, wholesale receivables and returns. The player's
/// PlacePlayerLine reads the same proof. Draws no global random numbers.
/// </summary>
public static class ChartProvenWholesale {
	public const string Switch = "--chart-proven-wholesale";
	public const string MarketsSwitch = "--chart-proven-wholesale-markets";
	/// <summary>Check the houses' call at every week end, not only at the label's monthly close.</summary>
	public const string WeeklySwitch = "--chart-proven-wholesale-weekly";
	private static bool configured;
	/// <summary>Worst national position that counts as proof; 0 when off.</summary>
	public static int Position { get; private set; }
	/// <summary>Most markets a chart-proven label places in one month.</summary>
	public static int Markets { get; private set; } = int.MaxValue;
	public static bool Weekly { get; private set; }
	public static bool Enabled => Position > 0;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Position = 0; Markets = int.MaxValue; Weekly = false;
		bool marketsGiven = false;
		foreach (string arg in arguments ?? Array.Empty<string>()) {
			if (arg.StartsWith(Switch + "=", StringComparison.Ordinal)) {
				if (!int.TryParse(arg.Substring(Switch.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ||
					n < 1 || n > 100)
					throw new ArgumentException($"{Switch} must be a chart position from 1 to 100.");
				Position = n;
			}
			if (arg == WeeklySwitch) Weekly = true;
			if (arg.StartsWith(MarketsSwitch + "=", StringComparison.Ordinal)) {
				if (!int.TryParse(arg.Substring(MarketsSwitch.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ||
					m < 1)
					throw new ArgumentException($"{MarketsSwitch} must be at least 1.");
				Markets = m; marketsGiven = true;
			}
		}
		if (marketsGiven && Position <= 0) throw new ArgumentException($"{MarketsSwitch} requires {Switch}.");
		if (Weekly && Position <= 0) throw new ArgumentException($"{WeeklySwitch} requires {Switch}.");
		if (Enabled) Godot.GD.Print($"CHART_PROVEN_WHOLESALE on position={Position} markets={(Markets == int.MaxValue ? "all" : Markets.ToString(CultureInfo.InvariantCulture))} weekly={Weekly}");
		configured = true;
	}

	internal static void SetForChecks(int position, int markets = int.MaxValue, bool weekly = false) { configured = true; Position = position; Markets = markets; Weekly = weekly; }

	/// <summary>Whether a national position counts as proof. False when off.</summary>
	public static bool Qualifies(int nationalPosition) => Enabled && nationalPosition >= 1 && nationalPosition <= Position;

	/// <summary>Whether any of these records is on the national chart inside the proof depth. False when off.</summary>
	public static bool IsChartProven(IEnumerable<RecordRuntimeData> records) {
		if (!Enabled || records == null) return false;
		foreach (RecordRuntimeData record in records)
			if (record != null && Qualifies(record.currentPosition)) return true;
		return false;
	}
}
