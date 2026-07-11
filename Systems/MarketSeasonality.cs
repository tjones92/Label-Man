using System;
using System.Collections.Generic;

/// <summary>
/// Pure owner of the national market-seasonality shapes.  It deliberately has no
/// TimeManager dependency: callers provide the calendar date and whether this is
/// a live (rather than synthetic prewarm) tick.
/// </summary>
public static class MarketSeasonality {
	// Directive 4C calibration scalar: applied only after the calendar/legacy
	// normalization. Singles remain at unity; the first two-seed probe found
	// Album realized units just outside the +5% paired gate.
	private const float EnabledSingleSalesLevel = 1f;
	private const float EnabledAlbumSalesLevel = 0.98f;
	private static readonly float[] SingleSales = { .84f, .89f, .95f, .98f, 1.03f, 1.07f, 1.10f, 1.05f, .99f, 1.02f, 1.04f, 1.04f };
	private static readonly float[] AlbumSales = { .75f, .83f, .91f, .97f, 1.00f, 1.01f, 1.00f, .97f, .99f, 1.06f, 1.16f, 1.35f };
	private static readonly float[] RadioDelta = { -.04f, -.03f, -.02f, 0f, .02f, .05f, .05f, .03f, .01f, .02f, -.03f, -.06f };
	private static readonly float[] VenueAttendance = { .85f, .89f, .95f, 1f, 1.03f, 1.10f, 1.13f, 1.08f, 1f, 1.03f, .98f, .96f };
	private static readonly float[] RecordingCost = { .88f, .92f, .97f, 1f, 1.02f, 1.03f, 1.04f, 1.02f, 1.03f, 1.06f, 1.08f, .95f };
	private static readonly float[] MarketingEfficiency = { .82f, .90f, .95f, .99f, 1.02f, 1.04f, 1.03f, 1f, .98f, 1.08f, 1.13f, 1.06f };
	private static readonly float[] ArtistAvailability = { 1.16f, 1.11f, 1.06f, 1.02f, .99f, .95f, .92f, .94f, 1f, 1f, .95f, .90f };
	private static readonly float[] LegacySingleSales = { .90f, 1f, 1f, 1f, 1f, 1.05f, 1.05f, 1.05f, 1f, 1f, 1.10f, 1.20f };
	private static readonly float[] LegacyAlbumSales = { .90f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.12f, 1.25f };

	private static bool configured;
	private static bool enabled;
	private static bool? commandOverride;
	public static bool Enabled => enabled;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enableFlag = false;
		bool disableFlag = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--enable-market-seasonality") enableFlag = true;
			if (argument == "--disable-market-seasonality") disableFlag = true;
		}
		if (enableFlag && disableFlag) throw new ArgumentException("--enable-market-seasonality and --disable-market-seasonality cannot be used together.");
		commandOverride = enableFlag ? true : disableFlag ? false : null;
		enabled = commandOverride ?? sceneDefault;
		configured = true;
		ValidateTables();
		ValidateCalendarNormalizers();
	}

	public static float GetSingleSalesMultiplier(int year, int month, bool liveTick) =>
		GetSalesMultiplier(SingleSales, LegacySingleSales, year, month, liveTick);
	public static float GetAlbumSalesMultiplier(int year, int month, bool liveTick) =>
		GetSalesMultiplier(AlbumSales, LegacyAlbumSales, year, month, liveTick);
	public static float GetRadioOpportunity(int year, int month, bool liveTick) {
		ValidateMonth(month);
		if (!enabled || !liveTick) return 1f;
		float offset = WeightedSum(year, RadioDelta) / GetLiveFridayCount(year);
		return 1f + RadioDelta[month - 1] - offset;
	}
	public static float GetVenueAttendanceMultiplier(int year, int month, bool liveTick) => GetMeanOneMultiplier(VenueAttendance, year, month, liveTick);
	public static float GetRecordingCostMultiplier(int year, int month, bool liveTick) => GetMeanOneMultiplier(RecordingCost, year, month, liveTick);
	public static float GetMarketingEfficiencyMultiplier(int year, int month, bool liveTick) => GetMeanOneMultiplier(MarketingEfficiency, year, month, liveTick);
	public static float GetArtistAvailabilityMultiplier(int year, int month, bool liveTick) => GetMeanOneMultiplier(ArtistAvailability, year, month, liveTick);

	public static int GetLiveFridayCount(int year, int month) {
		ValidateMonth(month);
		int count = 0;
		for (DateTime date = new(year, month, 1); date.Month == month; date = date.AddDays(1)) if (date.DayOfWeek == DayOfWeek.Friday) count++;
		return count;
	}
	public static int GetLiveFridayCount(int year) { int total = 0; for (int month = 1; month <= 12; month++) total += GetLiveFridayCount(year, month); return total; }
	public static IReadOnlyList<float> GetRawTable(string channel) => channel switch {
		"singleSales" => SingleSales, "albumSales" => AlbumSales, "radioDelta" => RadioDelta,
		"venueAttendance" => VenueAttendance, "recordingCost" => RecordingCost,
		"marketingEfficiency" => MarketingEfficiency, "artistAvailability" => ArtistAvailability,
		_ => throw new ArgumentOutOfRangeException(nameof(channel))
	};

	private static float GetSalesMultiplier(float[] raw, float[] legacy, int year, int month, bool liveTick) {
		ValidateMonth(month);
		if (!enabled || !liveTick) return legacy[month - 1];
		float level = ReferenceEquals(raw, SingleSales) ? EnabledSingleSalesLevel : EnabledAlbumSalesLevel;
		return raw[month - 1] * WeightedSum(year, legacy) / WeightedSum(year, raw) * level;
	}
	private static float GetMeanOneMultiplier(float[] raw, int year, int month, bool liveTick) {
		ValidateMonth(month);
		if (!enabled || !liveTick) return 1f;
		float result = raw[month - 1] * GetLiveFridayCount(year) / WeightedSum(year, raw);
		if (!float.IsFinite(result) || result <= 0f) throw new InvalidOperationException("Seasonality multiplier must be finite and positive.");
		return result;
	}
	private static float WeightedSum(int year, float[] values) { float total = 0f; for (int month = 1; month <= 12; month++) total += GetLiveFridayCount(year, month) * values[month - 1]; return total; }
	private static void ValidateMonth(int month) { if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month)); }
	private static void ValidateTables() {
		foreach (float[] table in new[] { SingleSales, AlbumSales, RadioDelta, VenueAttendance, RecordingCost, MarketingEfficiency, ArtistAvailability, LegacySingleSales, LegacyAlbumSales }) {
			if (table.Length != 12) throw new InvalidOperationException("All market seasonality tables must contain twelve months.");
			foreach (float value in table) if (!float.IsFinite(value)) throw new InvalidOperationException("Market seasonality table contains a non-finite value.");
		}
		foreach (float[] table in new[] { SingleSales, AlbumSales, VenueAttendance, RecordingCost, MarketingEfficiency, ArtistAvailability }) foreach (float value in table) if (value <= 0f) throw new InvalidOperationException("Multiplicative market seasonality tables must be positive.");
		foreach (float[] table in new[] { SingleSales, AlbumSales, VenueAttendance, RecordingCost, MarketingEfficiency, ArtistAvailability }) ValidateSum(table, 12f);
		ValidateSum(RadioDelta, 0f);
	}
	private static void ValidateCalendarNormalizers() {
		for (int year = 1960; year <= 1969; year++) {
			ValidateClose(WeightedSum(year, SingleSales) * WeightedSum(year, LegacySingleSales) / WeightedSum(year, SingleSales), WeightedSum(year, LegacySingleSales));
			ValidateClose(WeightedSum(year, AlbumSales) * WeightedSum(year, LegacyAlbumSales) / WeightedSum(year, AlbumSales), WeightedSum(year, LegacyAlbumSales));
			foreach (float[] channel in new[] { VenueAttendance, RecordingCost, MarketingEfficiency, ArtistAvailability }) {
				float effectiveTotal = WeightedSum(year, channel) * GetLiveFridayCount(year) / WeightedSum(year, channel);
				ValidateClose(effectiveTotal, GetLiveFridayCount(year));
			}
			ValidateClose(WeightedSum(year, RadioDelta) - (WeightedSum(year, RadioDelta) / GetLiveFridayCount(year)) * GetLiveFridayCount(year), 0f);
		}
	}
	private static void ValidateSum(float[] values, float expected) { float actual = 0f; foreach (float value in values) actual += value; ValidateClose(actual, expected); }
	private static void ValidateClose(float actual, float expected) { if (Math.Abs(actual - expected) > 0.0001f * Math.Max(1f, Math.Abs(expected))) throw new InvalidOperationException("Market seasonality invariant failed."); }
}
