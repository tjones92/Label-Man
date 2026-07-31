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
		long RecordLookups,
		// Post-SimulateWeek spans. Handoff 35.3 left 39% of the decade run
		// unattributed because every one of these sits after SimulateWeek returns.
		// FreezeSettlement, BookSettlement, SettlementAuditEvent, GenreMomentum and
		// CullDeadRecords together cover the whole of OnWeekEnded.
		double FreezeSettlementSeconds,
		double BookSettlementSeconds,
		double SettlementAuditEventSeconds,
		double GenreMomentumSeconds,
		double CullDeadRecordsSeconds,
		double PopulationLifecycleSeconds,
		// ChartManager is only one of three OnWeekEnded subscribers, and the label
		// lifecycle runs off OnMonthChanged. These cover the rest of the tick.
		double CompetitorWeekSeconds,
		double RosterWeekSeconds,
		double DailyTalentMarketSeconds,
		double LabelLifecycleMonthSeconds);

	public static bool Enabled { get; set; }

	private static long simulateWeekTicks;
	private static long calculateLabelRevenueTicks;
	private static long recordLookupTicks;
	private static long albumUpdateTicks;
	private static long dueAlbumProjectsTicks;
	private static long captureWeekTicks;
	private static long recordLookups;
	private static long freezeSettlementTicks;
	private static long bookSettlementTicks;
	private static long settlementAuditEventTicks;
	private static long genreMomentumTicks;
	private static long cullDeadRecordsTicks;
	private static long populationLifecycleTicks;
	private static long competitorWeekTicks;
	private static long rosterWeekTicks;
	private static long dailyTalentMarketTicks;
	private static long labelLifecycleMonthTicks;

	public static long Begin() => Enabled ? Stopwatch.GetTimestamp() : 0L;
	private static long Elapsed(long start) => start == 0L ? 0L : Stopwatch.GetTimestamp() - start;
	public static void EndSimulateWeek(long start) => simulateWeekTicks += Elapsed(start);
	public static void EndCalculateLabelRevenue(long start) => calculateLabelRevenueTicks += Elapsed(start);
	public static void EndRecordLookup(long start) { if (start != 0L) { recordLookupTicks += Elapsed(start); recordLookups++; } }
	public static void EndAlbumUpdate(long start) => albumUpdateTicks += Elapsed(start);
	public static void EndDueAlbumProjects(long start) => dueAlbumProjectsTicks += Elapsed(start);
	public static void EndCaptureWeek(long start) => captureWeekTicks += Elapsed(start);
	public static void EndFreezeSettlement(long start) => freezeSettlementTicks += Elapsed(start);
	/// <summary>Inclusive of CalculateLabelRevenue, which is reported separately.</summary>
	public static void EndBookSettlement(long start) => bookSettlementTicks += Elapsed(start);
	public static void EndSettlementAuditEvent(long start) => settlementAuditEventTicks += Elapsed(start);
	public static void EndGenreMomentum(long start) => genreMomentumTicks += Elapsed(start);
	public static void EndCullDeadRecords(long start) => cullDeadRecordsTicks += Elapsed(start);
	public static void EndPopulationLifecycle(long start) => populationLifecycleTicks += Elapsed(start);
	/// <summary>Inclusive of DueAlbumProjects, which is reported separately.</summary>
	public static void EndCompetitorWeek(long start) => competitorWeekTicks += Elapsed(start);
	public static void EndRosterWeek(long start) => rosterWeekTicks += Elapsed(start);
	public static void EndDailyTalentMarket(long start) => dailyTalentMarketTicks += Elapsed(start);
	public static void EndLabelLifecycleMonth(long start) => labelLifecycleMonthTicks += Elapsed(start);

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
			recordLookups,
			freezeSettlementTicks * scale,
			bookSettlementTicks * scale,
			settlementAuditEventTicks * scale,
			genreMomentumTicks * scale,
			cullDeadRecordsTicks * scale,
			populationLifecycleTicks * scale,
			competitorWeekTicks * scale,
			rosterWeekTicks * scale,
			dailyTalentMarketTicks * scale,
			labelLifecycleMonthTicks * scale);
		simulateWeekTicks = calculateLabelRevenueTicks = recordLookupTicks = albumUpdateTicks = 0L;
		dueAlbumProjectsTicks = captureWeekTicks = recordLookups = 0L;
		freezeSettlementTicks = bookSettlementTicks = settlementAuditEventTicks = 0L;
		genreMomentumTicks = cullDeadRecordsTicks = populationLifecycleTicks = 0L;
		competitorWeekTicks = rosterWeekTicks = dailyTalentMarketTicks = labelLifecycleMonthTicks = 0L;
		return snapshot;
	}
}
