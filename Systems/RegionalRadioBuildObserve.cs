using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

/// <summary>
/// Observation only (SimTools/Top10TurnoverFindings.md §11.9): <c>--regional-radio-build-observe</c> writes one row per
/// single, region and week to SimLogs/&lt;run&gt;-regional-radio.csv, so a replay can separate the per-region build clock
/// (<c>--regional-radio-build-clock</c>: a region that is serviced late restarts the six-week radio build ramp at its
/// own arrival) from the rotation lag and from target growth. A row is written while the record sat in last week's
/// Hot 100, and for every week of a region that arrived after launch (arrival age above 1), from its arrival on, up to
/// release age 52. Reads settled state only and makes no draws, so a run is byte-identical with it on or off.
/// </summary>
public static class RegionalRadioBuildObserve {
	public const string Switch = "--regional-radio-build-observe";
	private const int MaxAge = 52;
	private static bool configured, enabled;
	private static string output;
	private static StreamWriter writer;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		configured = true;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = args.Contains(Switch);
		if (!enabled) return;
		string run = args.FirstOrDefault(a => a.StartsWith("--run=", StringComparison.Ordinal))?.Substring(6);
		if (string.IsNullOrEmpty(run) || Path.GetFileName(run) != run)
			throw new ArgumentException(Switch + " requires a plain --run name.");
		output = Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), run + "-regional-radio.csv");
		GD.Print("REGIONAL_RADIO_BUILD_OBSERVE " + output);
	}

	/// <summary>Whether this region's week is logged: the record charted last week, or the region arrived late.</summary>
	public static bool Wants(RecordRuntimeData record, RegionalRecordData data) =>
		enabled && record.weeksSinceRelease <= MaxAge && !data.awaitingArrival &&
		(record.currentPosition >= 1 && record.currentPosition <= 100 || data.arrivalAge > 1);

	private readonly struct Pending {
		public readonly int Week, Year, BuildAge, Position, Age;
		public readonly RecordRuntimeData Record;
		public readonly MarketRegion Region;
		public readonly RegionalRecordData Data;
		public readonly float Build, Heat, Reach, Target, Start, Tail, PanelWeight, Reporter, Step3;
		public Pending(int week, int year, RecordRuntimeData record, MarketRegion region, RegionalRecordData data, int buildAge,
			float build, float heat, float reach, float target, float start, float tail, float panelWeight, float reporter) {
			Week = week; Year = year; Record = record; Region = region; Data = data; BuildAge = buildAge;
			Position = record.currentPosition; Age = record.weeksSinceRelease;
			Build = build; Heat = heat; Reach = reach; Target = target; Start = start; Tail = tail;
			PanelWeight = panelWeight; Reporter = reporter; Step3 = data.radioPlay;
		}
	}
	private static readonly List<Pending> pending = new();

	/// <summary>Holds this region's update until <see cref="Flush"/>, which runs after breakout discovery and before the
	/// chart points are read, so the row carries the rotation the chart actually paid.</summary>
	/// <param name="startPlay">The region's rotation entering this week's update (after any arrival seed or event add).</param>
	/// <param name="heatTarget">The rotation target before the build weight, arrival and city reach.</param>
	/// <param name="cityReach">The city-reach fraction applied to the target (1 when it is not applied).</param>
	/// <param name="target">The target the rotation update used.</param>
	public static void Row(int week, int year, RecordRuntimeData record, MarketRegion region, RegionalRecordData data,
		int buildAge, float radioBuild, float heatTarget, float cityReach, float target, float startPlay, float tail,
		float panelWeight, float reporter) =>
		pending.Add(new Pending(week, year, record, region, data, buildAge, radioBuild, heatTarget, cityReach, target,
			startPlay, tail, panelWeight, reporter));

	/// <summary>Writes the week's rows: step3Play is the rotation the regional update left, radioPlay the rotation at
	/// chart time (after breakout discovery), airplay that region's points before the era weight.</summary>
	public static void Flush() {
		if (pending.Count == 0) return;
		if (writer == null) {
			writer = new StreamWriter(output);
			writer.WriteLine("week,year,recordId,region,age,position,arrivalAge,buildAge,radioBuild,heatTarget,cityReach,target," +
				"startPlay,tail,panelWeight,reporter,step3Play,radioPlay,dropped,access,scale,airplay");
		}
		foreach (var p in pending) {
			var data = p.Data;
			float access = data.genreRadioOpportunityThisWeek > 0f ? data.genreRadioOpportunityThisWeek : 1f;
			float scale = p.Region.media != null ? p.Region.media.radioReach * p.Region.EconomicPopulation : 0f;
			writer.WriteLine(string.Join(",",
				p.Week.ToString(CultureInfo.InvariantCulture), p.Year.ToString(CultureInfo.InvariantCulture),
				p.Record.baseRecord.recordId, p.Region.regionId,
				p.Age.ToString(CultureInfo.InvariantCulture), p.Position.ToString(CultureInfo.InvariantCulture),
				data.arrivalAge.ToString(CultureInfo.InvariantCulture), p.BuildAge.ToString(CultureInfo.InvariantCulture),
				F(p.Build), F(p.Heat), F(p.Reach), F(p.Target), F(p.Start), F(p.Tail), F(p.PanelWeight), F(p.Reporter),
				F(p.Step3), F(data.radioPlay), data.stationsDropped ? "1" : "0", F(access), F(scale),
				F(ChartSimulator.RegionAirplayPoints(data, p.Region))));
		}
		pending.Clear();
		writer.Flush();
	}

	private static string F(float v) => v.ToString("G7", CultureInfo.InvariantCulture);
}
