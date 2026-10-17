using System;

/// <summary>--chart-inertia-base-check: the probe is an identity when off, and lowering the base lets records fall further.</summary>
public static class ChartInertiaProbeChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Chart inertia probe: " + why); }
		float was = ChartInertiaProbe.BaseInertia;
		try {
			ChartInertiaProbe.SetBaseForChecks(-1f);
			Check(!ChartInertiaProbe.Enabled, "an unnamed switch must read as off");
			foreach (float authored in new[] { 0f, 0.5f, 0.8f })
				Check(ChartInertiaProbe.Apply(authored) == authored, "off must return the authored base bit-for-bit");

			var record = new RecordRuntimeData { baseRecord = new Record { hookStrength = 0.8f, productionQuality = 0.8f, danceability = 0.8f },
				unitsThisWeek = 30000, momentum = 0.1f, weeksNegative = 0 };
			int Cap(float b) { ChartInertiaProbe.SetBaseForChecks(b); return ChartSimulator.GetInertiaPositionCap(record, 5, 25); }
			int high = Cap(0.8f), mid = Cap(0.4f), none = Cap(0f);
			Check(high < mid && mid < none, "a lower base must let a falling record drop further");
			Check(none == 25, "base 0 must return the raw position");
			ChartInertiaProbe.SetBaseForChecks(0f);
			Check(ChartSimulator.GetInertiaPositionCap(record, 25, 5) == 5, "a rising record is never held");
		} finally {
			ChartInertiaProbe.SetBaseForChecks(was);
		}
	}
}
