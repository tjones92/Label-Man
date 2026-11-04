using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17aa-§17ab), off unless named.
///
/// History's hit acts converted a Hot 100 entry into a Top 10 at the same rate whatever their label (Big-5 15.96%,
/// everyone else 15.93%, 1961-64); the Big-5's per-act edge was cadence, not height. The sim's Big-5 hit acts convert
/// at 1.38x the rest, carried by radio heat from the weekly label push in the radio target (§17i). The hit-act
/// cadence lane (§17y) gives Big-5 hit acts history's cadence and so exposes that premium (§17z).
///
/// <c>--known-act-airplay[=P]</c>: for a single whose act was hot when it launched (a Top 40 within the lane's
/// <see cref="HitActCadence.HotWindowWeeks"/> weeks, on the same clock), while the single is on the Hot 100, the weekly
/// label push that <c>UpdateRadioHeat</c> reads uses campaign impact <c>P</c> in place of the label's own. The
/// release-age taper and the top-20 hold still apply, so only the label's strength is replaced: programmers play a
/// known act's charting record on the act's name, not on the label's promotion budget. <c>P</c> defaults to
/// <see cref="DefaultPush"/>, the mean raw campaign impact of hot Hot 100 entrants in V1 (1961-64, five seeds), so
/// the population's average push is kept and only the label spread is removed.
///
/// Before entry, and for every record by an act that was not hot, label push is untouched: that is history's channel
/// for breaking a new act. Sales (the release ramp), awareness, launch stock and launch radio reads of campaign
/// impact are untouched too. Market-side and label-blind, so the player's records are included. Off: the radio push
/// is <c>currentLabelPush</c> exactly and nothing is drawn or written.
/// </summary>
public static class KnownActAirplay {
	public const string Switch = "--known-act-airplay";
	public const float DefaultPush = 0.60f;
	private static bool configured;
	private static bool enabled;
	private static float push = DefaultPush;
	public static bool Enabled => enabled;
	public static float Push => push;

	public static int AppliedRecordWeeks { get; private set; }
	public static int HotLaunches { get; private set; }
	public static int ColdLaunches { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		(enabled, push) = Parse(arguments);
		configured = true;
	}

	internal static (bool on, float push) Parse(IEnumerable<string> arguments) {
		var values = (arguments ?? Array.Empty<string>())
			.Where(arg => arg == Switch || arg.StartsWith(Switch + "=", StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --known-act-airplay once.");
		if (values.Length == 0) return (false, DefaultPush);
		if (values[0] == Switch) return (true, DefaultPush);
		string raw = values[0].Substring(Switch.Length + 1);
		if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed >= 0f && parsed <= 1f))
			throw new ArgumentException("--known-act-airplay=P needs a campaign impact in [0, 1].");
		return (true, parsed);
	}

	internal static void SetForChecks(bool on, float value = DefaultPush) {
		configured = true; enabled = on; push = value;
		AppliedRecordWeeks = HotLaunches = ColdLaunches = 0;
	}

	private static int CurrentWeek => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;

	/// <summary>
	/// Whether the record's act was hot at launch. Decided once, on the record's first weekly update with the switch
	/// on, which comes before the record can credit its own Top 40 (that credit lands when its chart peak reaches 40).
	/// </summary>
	internal static bool HotAtLaunch(RecordRuntimeData record, SimulatedArtist artist, int week) {
		if (record.knownActHotAtLaunch < 0) {
			bool hot = artist != null && HitActCadence.WeeksSinceTop40(artist, week) <= HitActCadence.HotWindowWeeks;
			record.knownActHotAtLaunch = (sbyte)(hot ? 1 : 0);
			if (hot) HotLaunches++; else ColdLaunches++;
		}
		return record.knownActHotAtLaunch == 1;
	}

	/// <summary>
	/// Written in <c>UpdateLabelPush</c> once the week factor is known: the radio push for a hot act's charting single,
	/// or -1 (read <c>currentLabelPush</c>). Only called while the switch is on.
	/// </summary>
	public static float RadioPush(RecordRuntimeData record, float weekFactor) =>
		RadioPush(record, weekFactor, ArtistManager.Instance?.GetArtist(record.baseRecord?.artistId ?? ""), CurrentWeek);

	internal static float RadioPush(RecordRuntimeData record, float weekFactor, SimulatedArtist artist, int week) {
		if (!enabled || record?.baseRecord == null || record.baseRecord.format != ReleaseFormat.Single) return -1f;
		if (!HotAtLaunch(record, artist, week)) return -1f;
		if (record.currentPosition is not (> 0 and <= 100)) return -1f;
		AppliedRecordWeeks++;
		return Mathf.Clamp(push * weekFactor, 0f, 1f);
	}

	/// <summary>The push <c>UpdateRadioHeat</c> reads: the label's own unless the switch wrote a known-act value.</summary>
	public static float ForRadio(RecordRuntimeData record) =>
		record.knownActRadioPush >= 0f ? record.knownActRadioPush : record.currentLabelPush;

	/// <summary>A cumulative console line at each year end while on (the console is not a compared CSV).</summary>
	public static void ReportForWeek(int week) {
		if (!enabled || week <= 0 || week % 52 != 0) return;
		GD.Print($"KNOWN_ACT_AIRPLAY week={week} push={push.ToString("0.###", CultureInfo.InvariantCulture)} " +
			$"hotLaunches={HotLaunches} coldLaunches={ColdLaunches} appliedRecordWeeks={AppliedRecordWeeks}");
	}
}
