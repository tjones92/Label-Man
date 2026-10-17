using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17i), off unless named.
/// --label-hold-capacity=H limits how many records per label keep the top-20 push hold in
/// <c>ChartSimulator.UpdateLabelPush</c> each week.
///
/// The hold floors weekly push at 0.85 for any single at positions 1-20, so a label pays its full campaign to every
/// record it has there at once. In the two seeds that fail the CR4 band one major holds 3+ of the Top 10 in 14-32% of
/// weeks against 8.9% historically (1960-64). A label's promotion staff was finite: with the switch on, only its H
/// best-placed held records keep the floor. The rest lose the top-20 hold and take whatever the release-age and
/// momentum rules give them. Reads no seed, tier, year or label identity, and draws no random numbers.
/// </summary>
public static class LabelHoldCapacity {
	public const string SwitchPrefix = "--label-hold-capacity=";
	private static bool configured;
	private static int capacity;
	private static readonly HashSet<string> overflow = new(StringComparer.Ordinal);
	private static int yearHeldRecordWeeks, yearOverflowRecordWeeks;
	public static bool Enabled => capacity > 0;
	public static int Capacity => capacity;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --label-hold-capacity once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed < 1 || parsed > 20)
				throw new ArgumentException("--label-hold-capacity needs a whole number of records in [1, 20].");
			capacity = parsed;
			GD.Print($"LABEL_HOLD_CAPACITY enabled capacity={capacity}");
		}
		configured = true;
	}

	internal static void SetCapacityForChecks(int value) { configured = true; capacity = value; overflow.Clear(); }

	/// <summary>
	/// Whether UpdateLabelPush would apply the top-20 floor to this record this week. It reads the position going
	/// into the week; <paramref name="weeksSinceRelease"/> is the value after the week's increment.
	/// </summary>
	internal static bool IsHeld(int position, int weeksSinceRelease) =>
		position > 0 && position <= 20 && ChartSimulator.GetReleaseAgePushFactor(weeksSinceRelease) < 0.85f;

	/// <summary>
	/// Ranks each label's held singles by position and marks every one beyond the capacity. Call once per week before
	/// the record update loop, which increments weeksSinceRelease before UpdateLabelPush reads it.
	/// </summary>
	public static void BeginWeek(IEnumerable<RecordRuntimeData> records, int chartWeek) {
		overflow.Clear();
		if (!Enabled) return;
		var held = new Dictionary<string, List<RecordRuntimeData>>(StringComparer.Ordinal);
		foreach (var record in records) {
			if (record?.baseRecord == null || record.baseRecord.format == ReleaseFormat.Album) continue;
			if (string.IsNullOrEmpty(record.baseRecord.labelId)) continue;
			if (!IsHeld(record.currentPosition, record.weeksSinceRelease + 1)) continue;
			if (!held.TryGetValue(record.baseRecord.labelId, out var list)) held[record.baseRecord.labelId] = list = new List<RecordRuntimeData>();
			list.Add(record);
		}
		foreach (var list in held.Values) {
			yearHeldRecordWeeks += list.Count;
			if (list.Count <= capacity) continue;
			// Positions are unique on the published chart, so the order is total and deterministic.
			list.Sort((a, b) => a.currentPosition.CompareTo(b.currentPosition));
			for (int i = capacity; i < list.Count; i++) overflow.Add(list[i].baseRecord.recordId);
			yearOverflowRecordWeeks += list.Count - capacity;
		}
		if (chartWeek > 0 && chartWeek % 52 == 0) {
			GD.Print($"LABEL_HOLD_CAPACITY week={chartWeek} capacity={capacity} heldRecordWeeks={yearHeldRecordWeeks} overflowRecordWeeks={yearOverflowRecordWeeks}");
			yearHeldRecordWeeks = yearOverflowRecordWeeks = 0;
		}
	}

	/// <summary>True only when the switch is on and this record is beyond its label's capacity this week.</summary>
	public static bool IsOverflow(RecordRuntimeData record) =>
		overflow.Count > 0 && record?.baseRecord?.recordId != null && overflow.Contains(record.baseRecord.recordId);

	internal static void MarkForChecks(IEnumerable<RecordRuntimeData> records) => BeginWeek(records, 0);
}
