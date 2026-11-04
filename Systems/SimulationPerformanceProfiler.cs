using System.Diagnostics;

public static class SimulationPerformanceProfiler {
	// Detailed spans; release/material and population substeps are inclusive of their children.
	public enum Phase { RecordState, RegionalDemand, MarketClearing, Restock, Playlists, RegionalState, ChartRanking,
		RevenueMemory, ReleaseDecisions, MaterialSelection, RoomAdvance, RoomCalendar, SceneEcosystem,
		PopulationOwnership, PopulationExits, PopulationFormation, PopulationProspects, SceneWindows, CompetitorMonth }
	// The original coarse spans, kept in their CSV column order.
	private enum Span { SimulateWeek, CalculateLabelRevenue, RecordLookup, AlbumUpdate, DueAlbumProjects, CaptureWeek,
		FreezeSettlement, BookSettlement, SettlementAuditEvent, GenreMomentum, CullDeadRecords, PopulationLifecycle,
		CompetitorWeek, RosterWeek, DailyTalentMarket, LabelLifecycleMonth }

	/// <summary>A span start: timestamp plus this thread's allocated-byte counter. Default when profiling is off.</summary>
	public readonly struct Stamp {
		public readonly long Ticks;
		public readonly long Bytes;
		public Stamp(long ticks, long bytes) { Ticks = ticks; Bytes = bytes; }
	}
	public readonly struct Scope : System.IDisposable {
		private readonly Phase phase;
		private readonly Stamp start;
		public Scope(Phase phase) { this.phase = phase; start = Begin(); }
		public void Dispose() => End(phase, start);
	}
	public static Scope Measure(Phase phase) => new(phase);
	private static readonly long[] phaseTicks = new long[System.Enum.GetValues<Phase>().Length];
	private static readonly long[] phaseBytes = new long[phaseTicks.Length];
	private static readonly long[] spanTicks = new long[System.Enum.GetValues<Span>().Length];
	private static readonly long[] spanBytes = new long[spanTicks.Length];
	public static void End(Phase phase, Stamp start) => Add(phaseTicks, phaseBytes, (int)phase, start);
	public static double[] TakePhaseSecondsAndReset() => TakeSeconds(phaseTicks);
	/// <summary>MB allocated on the calling thread inside each phase (inclusive of nested phases).</summary>
	public static double[] TakePhaseAllocatedMBAndReset() => TakeMB(phaseBytes);
	public static string[] PhaseNames => System.Enum.GetNames<Phase>();
	public static string[] SpanNames => System.Enum.GetNames<Span>();
	public static double[] TakeSpanAllocatedMBAndReset() => TakeMB(spanBytes);

	private static void Add(long[] ticks, long[] bytes, int index, Stamp start) {
		if (start.Ticks == 0L) return;
		ticks[index] += Stopwatch.GetTimestamp() - start.Ticks;
		bytes[index] += System.GC.GetAllocatedBytesForCurrentThread() - start.Bytes;
	}
	private static double[] TakeSeconds(long[] ticks) {
		var seconds = new double[ticks.Length];
		for (int i = 0; i < seconds.Length; i++) { seconds[i] = (double)ticks[i] / Stopwatch.Frequency; ticks[i] = 0L; }
		return seconds;
	}
	private static double[] TakeMB(long[] bytes) {
		var mb = new double[bytes.Length];
		for (int i = 0; i < mb.Length; i++) { mb[i] = bytes[i] / 1048576d; bytes[i] = 0L; }
		return mb;
	}

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

	private static long recordLookups;

	public static Stamp Begin() => Enabled ? new(Stopwatch.GetTimestamp(), System.GC.GetAllocatedBytesForCurrentThread()) : default;
	private static void EndSpan(Span span, Stamp start) => Add(spanTicks, spanBytes, (int)span, start);
	public static void EndSimulateWeek(Stamp start) => EndSpan(Span.SimulateWeek, start);
	public static void EndCalculateLabelRevenue(Stamp start) => EndSpan(Span.CalculateLabelRevenue, start);
	public static void EndRecordLookup(Stamp start) { if (start.Ticks != 0L) { EndSpan(Span.RecordLookup, start); recordLookups++; } }
	public static void EndAlbumUpdate(Stamp start) => EndSpan(Span.AlbumUpdate, start);
	public static void EndDueAlbumProjects(Stamp start) => EndSpan(Span.DueAlbumProjects, start);
	public static void EndCaptureWeek(Stamp start) => EndSpan(Span.CaptureWeek, start);
	public static void EndFreezeSettlement(Stamp start) => EndSpan(Span.FreezeSettlement, start);
	/// <summary>Inclusive of CalculateLabelRevenue, which is reported separately.</summary>
	public static void EndBookSettlement(Stamp start) => EndSpan(Span.BookSettlement, start);
	public static void EndSettlementAuditEvent(Stamp start) => EndSpan(Span.SettlementAuditEvent, start);
	public static void EndGenreMomentum(Stamp start) => EndSpan(Span.GenreMomentum, start);
	public static void EndCullDeadRecords(Stamp start) => EndSpan(Span.CullDeadRecords, start);
	public static void EndPopulationLifecycle(Stamp start) => EndSpan(Span.PopulationLifecycle, start);
	/// <summary>Inclusive of DueAlbumProjects, which is reported separately.</summary>
	public static void EndCompetitorWeek(Stamp start) => EndSpan(Span.CompetitorWeek, start);
	public static void EndRosterWeek(Stamp start) => EndSpan(Span.RosterWeek, start);
	public static void EndDailyTalentMarket(Stamp start) => EndSpan(Span.DailyTalentMarket, start);
	public static void EndLabelLifecycleMonth(Stamp start) => EndSpan(Span.LabelLifecycleMonth, start);

	/// <summary>Resets span seconds only; span allocations are taken by TakeSpanAllocatedMBAndReset.</summary>
	public static Snapshot TakeSnapshotAndReset() {
		double scale = 1d / Stopwatch.Frequency;
		double S(Span span) => spanTicks[(int)span] * scale;
		var snapshot = new Snapshot(
			S(Span.SimulateWeek),
			S(Span.CalculateLabelRevenue),
			S(Span.RecordLookup),
			S(Span.CalculateLabelRevenue) - S(Span.RecordLookup),
			S(Span.AlbumUpdate),
			S(Span.DueAlbumProjects),
			S(Span.CaptureWeek),
			recordLookups,
			S(Span.FreezeSettlement),
			S(Span.BookSettlement),
			S(Span.SettlementAuditEvent),
			S(Span.GenreMomentum),
			S(Span.CullDeadRecords),
			S(Span.PopulationLifecycle),
			S(Span.CompetitorWeek),
			S(Span.RosterWeek),
			S(Span.DailyTalentMarket),
			S(Span.LabelLifecycleMonth));
		System.Array.Clear(spanTicks);
		recordLookups = 0L;
		return snapshot;
	}
}
