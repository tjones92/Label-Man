using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Proven-act chart add (Big5ChartOpportunityInvestigation.md, "Field hit-act follow-ups"), off unless named.
///
/// History's proven acts climbed once charted: of chart events by an act with a Top 10 in the prior 52 weeks, 60.5%
/// reached the Top 40 against 38.1% for everyone else (1961-63). The sim has no such premium (42% against 47%): its
/// proven-act advantage is all at entry (launch awareness, stock, connection odds), and a record below 40 earns almost
/// no chart visibility (about 0.0014 a week at No. 60 against 0.025 inside the Top 40).
///
/// <c>--proven-act-chart-add=V</c>: a single whose act was proven at launch, while it charts at 41-100, earns chart
/// visibility <c>V</c> in the weekly awareness growth instead of the low-chart value, as if Top 40 programmers and
/// dealers picked up a known act's charting record on the act's name. Label-blind and post-entry only: entry, launch
/// and the Top 40 itself are untouched. "Proven at launch" is the hit-act fit floor's saved launch capture (an act Top 10
/// within 52 weeks before release; <see cref="HitActFitSupport"/>), so the record's own later Top 10 never counts and
/// nothing new is saved. Off: the authored visibility exactly, nothing drawn or written.
/// </summary>
public static class ProvenActChartAdd {
	public const string Prefix = "--proven-act-chart-add=";
	public const string CheckSwitch = "--proven-act-chart-add-check";
	private static bool configured;
	public static bool Enabled { get; private set; }
	public static float Visibility { get; private set; }
	public static int AppliedRecordWeeks { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + Prefix + " once.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0][Prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
				|| !float.IsFinite(v) || v <= 0f || v > .12f)
				throw new ArgumentException(Prefix + " needs a weekly visibility in (0, 0.12].");
			if (!HitActFitSupport.Enabled)
				throw new ArgumentException(Prefix + " reads the hit-act fit floor's launch capture (--hit-act-fit-floor).");
			Enabled = true;
			Visibility = v;
			GD.Print($"PROVEN_ACT_CHART_ADD on visibility={v.ToString("0.###", CultureInfo.InvariantCulture)}");
		}
		configured = true;
	}

	internal static void SetForChecks(bool on, float visibility = .025f) {
		configured = true; Enabled = on; Visibility = visibility; AppliedRecordWeeks = 0;
	}

	/// <summary>Whether the record is a single whose act was proven at launch.</summary>
	internal static bool ProvenAtLaunch(RecordRuntimeData record) =>
		record?.baseRecord != null && record.baseRecord.format == ReleaseFormat.Single && record.hitActFitFloorAtLaunch is > 0f;

	/// <summary>The chart visibility the awareness update reads: the authored value, or the add for a proven act's single
	/// charting at 41-100.</summary>
	public static float Apply(RecordRuntimeData record, float visibility) {
		if (!Enabled || record.currentPosition is not (> 40 and <= 100) || !ProvenAtLaunch(record)) return visibility;
		AppliedRecordWeeks++;
		return Math.Max(visibility, Visibility);
	}

	/// <summary>A cumulative console line at each year end while on.</summary>
	public static void ReportForWeek(int week) {
		if (!Enabled || week <= 0 || week % 52 != 0) return;
		GD.Print($"PROVEN_ACT_CHART_ADD week={week} appliedRecordWeeks={AppliedRecordWeeks}");
	}

	public static void RunChecks() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Proven-act chart add: " + why); }
		RecordRuntimeData Rec(ReleaseFormat format, float? floor, int position) => new RecordRuntimeData {
			baseRecord = new Record { recordId = "r" + Guid.NewGuid().ToString("N"), format = format }, hitActFitFloorAtLaunch = floor, currentPosition = position };
		try {
			SetForChecks(false);
			Check(Apply(Rec(ReleaseFormat.Single, .5f, 60), .001f) == .001f, "off: authored visibility");
			SetForChecks(true, .025f);
			Check(Apply(Rec(ReleaseFormat.Single, .5f, 60), .001f) == .025f, "proven single at 60 earns the add");
			Check(Apply(Rec(ReleaseFormat.Single, .5f, 100), 0f) == .025f, "proven single at 100 earns the add");
			Check(Apply(Rec(ReleaseFormat.Single, .5f, 40), .025f) == .025f && Apply(Rec(ReleaseFormat.Single, .5f, 5), .12f) == .12f,
				"Top 40 keeps its own visibility");
			Check(Apply(Rec(ReleaseFormat.Single, .5f, 0), 0f) == 0f, "uncharted: nothing (post-entry only)");
			Check(Apply(Rec(ReleaseFormat.Single, 0f, 60), .001f) == .001f && Apply(Rec(ReleaseFormat.Single, null, 60), .001f) == .001f,
				"not proven at launch: nothing");
			Check(Apply(Rec(ReleaseFormat.Album, .5f, 60), .001f) == .001f, "albums: nothing");
			Check(AppliedRecordWeeks == 2, "counts applied weeks");
			GD.Print("PROVEN_ACT_CHART_ADD_CHECK passed");
		} finally { SetForChecks(false); configured = false; }
	}
}
