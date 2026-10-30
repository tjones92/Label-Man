using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
/// Regional sync 5e (SimTools/RegionalSyncDesign.md §9g): what a reporter station reads as a record's sales support.
/// Off, it is the national ratio (this week's units / the record's peak week), which says where a record is in its own
/// life but not how big it is. <c>--station-regional-support=ratio</c> reads the same ratio from the station's own
/// region (local units / local peak, 3a/5d signals). <c>=standing</c> also weights it by the record's rank on the
/// region's sales list, so stations in one market follow that market's best sellers. This week's settled values, as
/// the national ratio is; no draws, and the candidacy noise is drawn before support is read.
/// </summary>
public static class StationRegionalSupport {
	public enum SupportMode { National, Ratio, Standing }
	private const string Prefix = "--station-regional-support=";
	private const string RankScalePrefix = "--station-regional-support-rank-scale=";
	private const string PanelWeightPrefix = "--station-regional-support-panel-weight=";
	private const string RankNoisePrefix = "--station-regional-support-rank-noise=";
	private const string BlendPrefix = "--station-regional-support-blend=";
	private const string ChartPrefix = "--station-regional-support-chart=";
	private const string UnsoldFloorFlag = "--station-regional-support-unsold-floor";
	private const string PlateauPrefix = "--station-regional-support-rank-plateau=";
	private const string PanelFreePrefix = "--station-regional-support-panel-free=";
	private const string PanelLinearPrefix = "--station-regional-support-panel-linear=";
	/// <summary>Standing weight: exp(-(rank - 1) / scale), floored; an unranked record reads the floor.</summary>
	public const float DefaultStandingRankScale = 25f, StandingFloor = .02f;
	private static bool configured;
	private static float? panelWeightOverride;
	public static SupportMode Mode { get; private set; }
	public static bool Enabled => Mode != SupportMode.National;
	/// <summary>§9h calibration: a wider scale lets stations in one market differ more on which sellers they play.</summary>
	public static float StandingRankScale { get; private set; } = DefaultStandingRankScale;
	/// <summary>§9i: each station reads a record's local rank as rank × exp(sigma × z), z a keyed normal fixed per
	/// station and record (the programmer's own view of it), so stations in one market share the sellers but order them
	/// differently. 0 (off) unless named; no RNG stream is touched.</summary>
	public static float StandingRankNoise { get; private set; }
	/// <summary>§9j: how much of the standing weight a station reads, support = ratio × lerp(1, standing, k). 1 is full
	/// standing; lower keeps support partly size-blind, as the national ratio was. Skipped (exactly full standing)
	/// unless named.</summary>
	public static float StandingBlend { get; private set; } = 1f;
	/// <summary>§9l option (a): the stations' playlists read regional support, but the reporter panel that feeds chart
	/// points is a separate national-support meeting on the stations' own state and draws, so the chart is the control's.
	/// The regional playlists run beside it on shadow state and their own draws, and feed only the playlist export.
	/// Off unless <c>--station-regional-support-chart=national</c> is named with a regional mode.</summary>
	public static bool ChartReadsNational { get; private set; }
	/// <summary>§9m: under <c>standing</c>, a record with no sales yet in the station's region reads the standing floor.
	/// Unnamed it reads the national ratio, which is 1.0 in a record's launch week, so stations seat records that sell
	/// nowhere above all but a market's top few sellers.</summary>
	public static bool UnsoldReadsFloor { get; private set; }
	/// <summary>§9m: standing reads 1 for a market's top K sellers and decays past them, exp(-(rank - K) / scale), so
	/// stations agree on which records are the market's sellers but order them by where each is in its own life (the
	/// size-blind ratio), as the national meeting did. 0 (off, decay from rank 1) unless named.</summary>
	public static int StandingRankPlateau { get; private set; }
	/// <summary>Big-5 handoff lead 1: panel airplay stays in chart points but not in the regional awareness update
	/// (<c>=awareness</c>), or not in awareness nor the breakout media evidence (<c>=sales</c>). Those reads take the
	/// rotation the region would carry with no panel blend, today's or carried in the tail. Off unless named.</summary>
	public static bool PanelFreeAwareness { get; private set; }
	public static bool PanelFreeBreakout { get; private set; }
	/// <summary>Big-5 handoff lead 2: chart points raise only the panel-free rotation to <c>AIRPLAY_CONVEXITY</c> and
	/// pay this week's reporter panel linearly, L × panel, beside it, so a panel that agrees with a pushed record's high
	/// tail is no longer compounded. Sales reads (awareness, breakout) keep the blended radioPlay. 0 (off) unless
	/// <c>--station-regional-support-panel-linear=L</c> is named.</summary>
	public static float PanelLinearWeight { get; private set; }
	public static bool PanelLinear => PanelLinearWeight > 0f;
	/// <summary>Whether ChartManager must track the panel's share of radioPlay (panelExcessRadio, panelRadioPlay).</summary>
	public static bool TracksPanelExcess => PanelFreeAwareness || PanelLinear;

	/// <summary>§9h calibration: the reporter panel's weight in regional radioPlay (the calibrated constant unless
	/// <c>--station-regional-support-panel-weight</c> is named). Standing concentrates panel spins on the records
	/// already selling, which the airplay convexity turns into chart points; a lower weight returns airplay's share
	/// to the guard.</summary>
	public static float PanelWeight(float calibrated) => panelWeightOverride ?? calibrated;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string a in arguments ?? Array.Empty<string>()) {
			if (a.StartsWith(RankScalePrefix, StringComparison.Ordinal)) {
				StandingRankScale = PositiveFloat(a, RankScalePrefix);
				continue;
			}
			if (a.StartsWith(RankNoisePrefix, StringComparison.Ordinal)) {
				StandingRankNoise = PositiveFloat(a, RankNoisePrefix);
				continue;
			}
			if (a.StartsWith(BlendPrefix, StringComparison.Ordinal)) {
				StandingBlend = Mathf.Clamp(PositiveFloat(a, BlendPrefix), 0f, 1f);
				continue;
			}
			if (a.StartsWith(PlateauPrefix, StringComparison.Ordinal)) {
				StandingRankPlateau = (int)PositiveFloat(a, PlateauPrefix);
				continue;
			}
			if (a.StartsWith(PanelFreePrefix, StringComparison.Ordinal)) {
				PanelFreeAwareness = true;
				PanelFreeBreakout = a.Substring(PanelFreePrefix.Length) switch {
					"awareness" => false,
					"sales" => true,
					_ => throw new ArgumentException(PanelFreePrefix + " takes awareness or sales.")
				};
				continue;
			}
			if (a.StartsWith(PanelLinearPrefix, StringComparison.Ordinal)) {
				PanelLinearWeight = PositiveFloat(a, PanelLinearPrefix);
				continue;
			}
			if (a == UnsoldFloorFlag) {
				UnsoldReadsFloor = true;
				continue;
			}
			if (a.StartsWith(ChartPrefix, StringComparison.Ordinal)) {
				ChartReadsNational = a.Substring(ChartPrefix.Length) switch {
					"national" => true,
					"stations" => false,
					_ => throw new ArgumentException(ChartPrefix + " takes national or stations.")
				};
				continue;
			}
			if (a.StartsWith(PanelWeightPrefix, StringComparison.Ordinal)) {
				panelWeightOverride = Mathf.Clamp(PositiveFloat(a, PanelWeightPrefix), 0f, 1f);
				continue;
			}
			if (!a.StartsWith(Prefix, StringComparison.Ordinal)) continue;
			Mode = a.Substring(Prefix.Length) switch {
				"ratio" => SupportMode.Ratio,
				"standing" => SupportMode.Standing,
				"national" => SupportMode.National,
				_ => throw new ArgumentException(Prefix + " takes ratio, standing or national.")
			};
		}
		if (ChartReadsNational && Mode == SupportMode.National)
			throw new ArgumentException(ChartPrefix + "national needs --station-regional-support=ratio or standing.");
		configured = true;
	}

	private static float PositiveFloat(string argument, string prefix) {
		if (!float.TryParse(argument.Substring(prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value <= 0f)
			throw new ArgumentException(prefix + " takes a positive number.");
		return value;
	}

	internal static void SetForChecks(SupportMode mode) { configured = true; Mode = mode; }

	/// <summary>The support a station in <paramref name="regionId"/> reads; the national ratio when off or before any
	/// local sales.</summary>
	public static float Support(RecordRuntimeData record, float nationalSupport, string regionId, string stationId) {
		if (!Enabled || record?.regionalData == null) return nationalSupport;
		bool unsoldFloor = UnsoldReadsFloor && Mode == SupportMode.Standing;
		if (!record.regionalData.TryGetValue(regionId, out RegionalRecordData data)) return unsoldFloor ? StandingFloor : nationalSupport;
		if (data.localPeakUnits <= 0) return unsoldFloor ? StandingFloor : nationalSupport;
		float ratio = Mathf.Clamp(RegionalAudience.LocalSignalUnits(data) / (float)data.localPeakUnits, 0f, 1f);
		if (Mode == SupportMode.Ratio) return ratio;
		float standing = StandingFloor;
		if (data.localSalesRank > 0) {
			float rank = data.localSalesRank;
			if (StandingRankNoise > 0f)
				rank *= Mathf.Exp(StandingRankNoise * BandLife.Normal("station-rank|" + stationId + "|" + record.baseRecord.recordId));
			standing = StandingRankPlateau > 0
				? Mathf.Max(StandingFloor, Mathf.Exp(-Mathf.Max(0f, rank - StandingRankPlateau) / StandingRankScale))
				: Mathf.Max(StandingFloor, Mathf.Exp(-(rank - 1f) / StandingRankScale));
		}
		if (StandingBlend < 1f) standing = Mathf.Lerp(1f, standing, StandingBlend);
		return ratio * standing;
	}
}
