using System;
using System.Collections.Generic;

/// <summary>--label-hold-capacity-check: off is inert, and on, only a label's records beyond the capacity lose the hold.</summary>
public static class LabelHoldCapacityChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Label hold capacity: " + why); }
		int was = LabelHoldCapacity.Capacity;
		RecordRuntimeData Single(string id, string labelId, int position, int weeksBeforeIncrement, float momentum = 0f) =>
			new RecordRuntimeData {
				baseRecord = new Record { recordId = id, labelId = labelId, format = ReleaseFormat.Single,
					hookStrength = 0.8f, productionQuality = 0.8f, danceability = 0.8f },
				currentPosition = position, weeksSinceRelease = weeksBeforeIncrement, momentum = momentum };
		var label = new AILabel { marketingPower = 0.9f, budgetLevel = 0.9f };
		float Push(RecordRuntimeData r) { ChartSimulator.UpdateLabelPushForChecks(r, label); return r.currentLabelPush; }
		try {
			// Release-age taper is unchanged by the extraction.
			float[] taper = { 1f, 1f, 0.9f, 0.9f, 0.6f, 0.6f, 0.3f, 0.3f, 0.1f, 0.1f };
			for (int w = 0; w < taper.Length; w++)
				Check(ChartSimulator.GetReleaseAgePushFactor(w) == taper[w], $"release-age factor changed at week {w}");

			// Four held records for one label (weeks 9 after the increment), one for another, plus a young record and #25.
			var records = new List<RecordRuntimeData> {
				Single("a", "L1", 3, 8), Single("b", "L1", 12, 8), Single("c", "L1", 7, 8), Single("d", "L1", 18, 8, momentum: 0.3f),
				Single("e", "L2", 15, 8), Single("young", "L1", 2, 0), Single("low", "L1", 25, 8),
				new RecordRuntimeData { baseRecord = new Record { recordId = "lp", labelId = "L1", format = ReleaseFormat.Album },
					currentPosition = 1, weeksSinceRelease = 8 } };

			LabelHoldCapacity.SetCapacityForChecks(0);
			LabelHoldCapacity.MarkForChecks(records);
			Check(!LabelHoldCapacity.Enabled, "an unnamed switch must read as off");
			foreach (var r in records) Check(!LabelHoldCapacity.IsOverflow(r), "off must mark nothing");

			LabelHoldCapacity.SetCapacityForChecks(2);
			LabelHoldCapacity.MarkForChecks(records);
			var marked = new HashSet<string>();
			foreach (var r in records) if (LabelHoldCapacity.IsOverflow(r)) marked.Add(r.baseRecord.recordId);
			// L1 holds a (#3), c (#7), b (#12), d (#18): the two best-placed keep it.
			Check(marked.SetEquals(new[] { "b", "d" }), "only L1's third and fourth best-placed held records overflow, got " + string.Join(",", marked));

			foreach (var r in records) r.weeksSinceRelease++;   // the weekly loop increments before UpdateLabelPush
			float full = Push(records[0]);
			Check(Math.Abs(full - ChartSimulator.GetCampaignImpact(label) * 0.85f) < 1e-6f, "a kept record retains the 0.85 floor");
			Check(Math.Abs(Push(records[1]) - ChartSimulator.GetCampaignImpact(label) * 0.1f) < 1e-6f, "an overflow record takes its release-age factor");
			Check(Math.Abs(Push(records[3]) - ChartSimulator.GetCampaignImpact(label) * 0.7f) < 1e-6f, "an overflow record with momentum keeps the momentum floor");
			Check(Math.Abs(Push(records[4]) - full) < 1e-6f, "another label's single held record is untouched");
			Check(Push(records[5]) == ChartSimulator.GetCampaignImpact(label), "a young record keeps its release push and is not counted");

			LabelHoldCapacity.SetCapacityForChecks(0);
			Check(Math.Abs(Push(records[1]) - full) < 1e-6f, "turning the switch off restores the hold");
		} finally {
			LabelHoldCapacity.SetCapacityForChecks(was);
		}
	}
}
