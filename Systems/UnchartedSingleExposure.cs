using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Release-economics probe: purchase exposure of an AI single with no chart presence (neither Hot 100 nor
/// bubbling under). Off, such a record gets #100's exposure everywhere, so a never-charting single sells
/// 8k-25k copies and almost never loses money. Historically a record off the charts was stocked mainly where
/// it was breaking; "only one in ten succeeds" was the trade's own rule of thumb. When on, a stalled
/// uncharted record's exposure runs from the floor (no regional breakout) up to #100's level (a full
/// regional breakout), and an optional scale on charted exposure can return the lost units to records that chart.
/// Both values are tuning assumptions, not measured historical rates. Off (the default) is exactly 1.
/// At a .25 floor it fails the five-year closure band; see SimTools/ReleaseLossCalibration-2026-10-21.md.
/// </summary>
public static class UnchartedSingleExposure {
	public const string FloorPrefix = "--uncharted-single-exposure=";
	public const string ChartedPrefix = "--charted-single-exposure-scale=";
	private static bool configured;
	public static float Floor { get; private set; } = 1f;
	public static float ChartedScale { get; private set; } = 1f;
	public static bool Enabled => Floor < 1f || ChartedScale != 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Floor = Read(args, FloorPrefix, 1f, 0f, 1f);
		ChartedScale = Read(args, ChartedPrefix, 1f, .5f, 3f);
		configured = true;
	}

	internal static void SetForChecks(float floor, float chartedScale) { configured = true; Floor = floor; ChartedScale = chartedScale; }

	// Only a record that has stalled loses shelf space: shipped on initial orders, it keeps its launch
	// window and any climb, and dealers return it once its sales are falling without chart or regional
	// support. Two rejected shapes, two 105-week seeds each: cutting every uncharted week starved chart
	// challengers (distinct Hot 100 acts -32% at a 0.25 floor), and cutting every uncharted week after
	// week 6 still cost ~18%, because most Hot 100 entries are records still climbing after week 6.
	public const int LaunchWindowWeeks = 6;
	public const int StalledWeeks = 2;

	internal static float Multiplier(int internalChartPosition, float regionalDiscovery, int weeksSinceRelease, int weeksSincePeakUnits) {
		if (!Enabled) return 1f;
		if (internalChartPosition > 0) return ChartedScale;
		if (weeksSinceRelease < LaunchWindowWeeks || weeksSincePeakUnits < StalledWeeks) return 1f;
		return Floor + (1f - Floor) * Math.Clamp(regionalDiscovery, 0f, 1f);
	}

	private static float Read(string[] args, string prefix, float fallback, float min, float max) {
		string raw = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length);
		if (raw == null) return fallback;
		if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value < min || value > max)
			throw new ArgumentException($"{prefix} takes a number from {min} to {max}.");
		return value;
	}
}
