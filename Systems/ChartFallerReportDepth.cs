using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Survey reporting depth for singles past their sales peak (Big5ChartOpportunityInvestigation.md, "Where the chart
/// bottom's slots go"), off unless named.
///
/// In the 1961-63 Hot 100 a record left the chart from about No. 50 whatever its peak: the median last position of a
/// completed run is 48-52 for every peak band from No. 1 to No. 40, and a record past its peak at 41-60 is gone the next
/// week 59% of the time. The chart was a survey of about 110 outlets' reports (63 stations, 47 dealers and one-stops),
/// and a station or dealer stopped reporting a single once it had dropped off their own list or the label stopped working
/// it. The lower half of the chart therefore held records still rising or breaking somewhere. The sim ranks on national
/// units and airplay, so a falling record slides on its residual sales (97.5% of its points at 61-100) down to about
/// No. 80 before it leaves, and spends 46% of all slot-weeks past its peak against history's 30%.
///
/// <c>--chart-faller-report-depth=D</c>: after the week's points are drawn, a single that ranks below No. D on points and
/// is at least G weeks past its own weekly sales peak is not reported. It is dropped from the week's ranking, so it is off
/// the Hot 100 and Bubbling Under alike, and everyone below it moves up. <c>--chart-faller-report-grace=G</c> (default 2;
/// needs the depth switch) sets G. Records ranked 1..D, and records below D still at or near their sales peak, are untouched.
/// Points are not changed, so the audit's recomputation stays exact. Draws nothing. Off: the ranking is returned as given.
/// </summary>
public static class ChartFallerReportDepth {
	public const string Prefix = "--chart-faller-report-depth=";
	public const string GracePrefix = "--chart-faller-report-grace=";
	public const string CheckSwitch = "--chart-faller-report-depth-check";
	private static bool configured;
	public static bool Enabled { get; private set; }
	public static int Depth { get; private set; }
	public static int Grace { get; private set; } = 2;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string One(string prefix) {
			var v = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
			if (v.Length > 1) throw new ArgumentException("Name " + prefix + " once.");
			return v.Length == 1 ? v[0][prefix.Length..] : null;
		}
		int Parse(string text, string prefix, int lo, int hi) {
			if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) || v < lo || v > hi)
				throw new ArgumentException($"{prefix} needs a whole number in [{lo}, {hi}].");
			return v;
		}
		string depth = One(Prefix), grace = One(GracePrefix);
		if (depth != null) {
			Depth = Parse(depth, Prefix, 10, 99);
			Enabled = true;
		}
		if (grace != null && !Enabled) throw new ArgumentException(GracePrefix + " needs " + Prefix);
		if (grace != null) Grace = Parse(grace, GracePrefix, 1, 26);
		if (Enabled) GD.Print($"CHART_FALLER_REPORT_DEPTH on depth={Depth} grace={Grace}");
		configured = true;
	}

	internal static void SetForChecks(bool on, int depth = 50, int grace = 2) {
		configured = true; Enabled = on; Depth = depth; Grace = grace;
	}

	/// <summary>True when a single at this points rank, this many weeks past its sales peak, goes unreported.</summary>
	internal static bool Unreported(int rawPosition, int weeksSincePeakUnits) =>
		Enabled && rawPosition > Depth && weeksSincePeakUnits >= Grace;

	/// <summary>The points ranking without unreported fallers. Off: the same list.</summary>
	public static List<RecordRuntimeData> Filter(List<RecordRuntimeData> rawRanking) {
		if (!Enabled || rawRanking == null) return rawRanking;
		var kept = new List<RecordRuntimeData>(rawRanking.Count);
		for (int i = 0; i < rawRanking.Count; i++)
			if (!Unreported(i + 1, rawRanking[i].weeksSincePeakUnits)) kept.Add(rawRanking[i]);
		return kept;
	}

	public static void RunChecks() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Chart faller report depth: " + why); }
		try {
			RecordRuntimeData Rec(string id, int weeksPast) =>
				new RecordRuntimeData(new Record { recordId = id, format = ReleaseFormat.Single }) { weeksSincePeakUnits = weeksPast };
			// Ranked by points: a falling hit at No. 1, rising No. 2, a faller at No. 3 (inside D 3), a faller at No. 4,
			// a record one week past its peak at No. 5, a rising record at No. 6.
			var ranking = new List<RecordRuntimeData> { Rec("hit", 6), Rec("up", 0), Rec("inside", 4), Rec("out", 2), Rec("fresh", 1), Rec("low", 0) };

			SetForChecks(false);
			Check(ReferenceEquals(Filter(ranking), ranking), "off: the ranking is returned unchanged");
			Check(!Unreported(99, 50), "off: nothing is unreported");

			SetForChecks(true, 3, 2);
			var kept = Filter(ranking).Select(r => r.baseRecord.recordId).ToArray();
			Check(kept.SequenceEqual(new[] { "hit", "up", "inside", "fresh", "low" }),
				"depth 3, grace 2: only the faller below No. 3 goes; order is kept");
			Check(ranking.Count == 6, "the input list is not modified");

			SetForChecks(true, 3, 1);
			kept = Filter(ranking).Select(r => r.baseRecord.recordId).ToArray();
			Check(kept.SequenceEqual(new[] { "hit", "up", "inside", "low" }), "grace 1: a record one week past its peak goes too");

			SetForChecks(true, 3, 2);
			Check(!Unreported(3, 9) && Unreported(4, 2) && !Unreported(4, 1), "rank and grace bounds");

			var args = new[] { Prefix + "50", GracePrefix + "3" };
			configured = false; Enabled = false; Configure(args);
			Check(Enabled && Depth == 50 && Grace == 3, "configure: depth and grace parse");
			bool threw = false;
			configured = false; Enabled = false; Grace = 2;
			try { Configure(new[] { GracePrefix + "3" }); } catch (ArgumentException) { threw = true; }
			Check(threw, "configure: grace without depth is refused");
			GD.Print("CHART_FALLER_REPORT_DEPTH_CHECK passed");
		} finally { SetForChecks(false); Grace = 2; Depth = 0; configured = false; }
	}
}
