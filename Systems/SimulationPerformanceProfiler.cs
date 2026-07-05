using System.Diagnostics;

public static class SimulationPerformanceProfiler {
	public readonly record struct Snapshot(
		double SimulateWeekSeconds,
		double CalculateLabelRevenueSeconds,
		double RecordLookupSeconds,
		double RevenueArithmeticSeconds,
		double AlbumUpdateSeconds,
		double DueAlbumProjectsSeconds,
		double CaptureWeekSeconds,
		long RecordLookups);

	public static bool Enabled { get; set; }

	private static long simulateWeekTicks;
	private static long calculateLabelRevenueTicks;
	private static long recordLookupTicks;
	private static long albumUpdateTicks;
	private static long dueAlbumProjectsTicks;
	private static long captureWeekTicks;
	private static long recordLookups;

	public static long Begin() => Enabled ? Stopwatch.GetTimestamp() : 0L;
	private static long Elapsed(long start) => start == 0L ? 0L : Stopwatch.GetTimestamp() - start;
	public static void EndSimulateWeek(long start) => simulateWeekTicks += Elapsed(start);
	public static void EndCalculateLabelRevenue(long start) => calculateLabelRevenueTicks += Elapsed(start);
	public static void EndRecordLookup(long start) { if (start != 0L) { recordLookupTicks += Elapsed(start); recordLookups++; } }
	public static void EndAlbumUpdate(long start) => albumUpdateTicks += Elapsed(start);
	public static void EndDueAlbumProjects(long start) => dueAlbumProjectsTicks += Elapsed(start);
	public static void EndCaptureWeek(long start) => captureWeekTicks += Elapsed(start);

	public static Snapshot TakeSnapshotAndReset() {
		double scale = 1d / Stopwatch.Frequency;
		var snapshot = new Snapshot(
			simulateWeekTicks * scale,
			calculateLabelRevenueTicks * scale,
			recordLookupTicks * scale,
			(calculateLabelRevenueTicks - recordLookupTicks) * scale,
			albumUpdateTicks * scale,
			dueAlbumProjectsTicks * scale,
			captureWeekTicks * scale,
			recordLookups);
		simulateWeekTicks = calculateLabelRevenueTicks = recordLookupTicks = albumUpdateTicks = 0L;
		dueAlbumProjectsTicks = captureWeekTicks = recordLookups = 0L;
		return snapshot;
	}
}
