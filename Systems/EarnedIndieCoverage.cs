using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// --earned-indie-coverage=S: restock service and shelf capacity a non-Major AI label's record gets in a region the label
/// has no distribution in, once the record is on the national chart. Off, an uncovered region is served at
/// 0.18 + 0.25 x strength (covered: 0.70 + 0.80 x strength) and shelved to 0.20 of physical capacity (covered:
/// 0.55 + 0.65 x strength), whether or not the record is a hit. Non-Majors cover 3-4 of the 7 regions at first chart
/// entry, and among charted unknown singles Top 10 weeks per single run 0.09 at 1-3 covered regions, 0.30 at 4-5 and
/// 0.96 at 7, with the tiers equal at 7 (SimTools/MajorTop10ShareFableHandoff.md). In 1960 independent distributors in
/// every market ordered a record that was on the charts; this probe moves a charted record's uncovered regions S of the
/// way to covered service and capacity. Restock only: launch stock, awareness and radio are unchanged, and a record
/// that has not charted gets nothing. Draws no random numbers.
/// </summary>
public static class EarnedIndieCoverage {
	public const string Switch = "--earned-indie-coverage";
	/// <summary>Worst national position that earns the service (default: anywhere on the Hot 100).</summary>
	public const string PositionSwitch = "--earned-indie-coverage-position";
	private static bool configured;
	public static float Share { get; private set; }
	public static int Position { get; private set; } = 100;
	public static bool Enabled => Share > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Share = 0f; Position = 100;
		bool positionGiven = false;
		foreach (string arg in arguments ?? Array.Empty<string>()) {
			if (arg.StartsWith(Switch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(Switch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float s) ||
					!float.IsFinite(s) || s < 0f || s > 1f)
					throw new ArgumentException($"{Switch} must be between 0 and 1.");
				Share = s;
			}
			if (arg.StartsWith(PositionSwitch + "=", StringComparison.Ordinal)) {
				if (!int.TryParse(arg.Substring(PositionSwitch.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) ||
					p < 1 || p > 100)
					throw new ArgumentException($"{PositionSwitch} must be between 1 and 100.");
				Position = p; positionGiven = true;
			}
		}
		if (positionGiven && Share <= 0f) throw new ArgumentException($"{PositionSwitch} requires {Switch}.");
		configured = true;
	}

	internal static void SetForChecks(float share, int position = 100) { configured = true; Share = share; Position = position; }

	/// <summary>
	/// How far this record's uncovered regions move toward covered service: <see cref="Share"/> for a non-Major AI label's
	/// record at national position 1..<see cref="Position"/>, otherwise 0.
	/// </summary>
	public static float For(AILabel label, int nationalPosition) =>
		Enabled && label != null && !label.isPlayerOwned && label.tier != LabelTier.Major &&
		nationalPosition >= 1 && nationalPosition <= Position ? Share : 0f;

	/// <summary>An uncovered region's value moved toward the covered one. Returns <paramref name="uncovered"/> exactly at 0.</summary>
	public static float Blend(float uncovered, float covered, float share) =>
		share <= 0f ? uncovered : uncovered + (Math.Max(covered, uncovered) - uncovered) * Math.Min(share, 1f);
}
