using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Three calibration probes on label-side advantages the Majors hold at every seam (SimTools/MajorTop10ShareFableHandoff.md).
/// All off unless named; none draws a random number.
///
/// --label-demand-parity=P: the live purchase rate carries 0.45 + 0.55 x distribution strength + 0.35 x national reach,
/// clamped to 0.55-1.20 (ChartSimulator.CalculateLiveLabelDemandScale), so a listener who already knows a record is about
/// 45% likelier to buy it on a Major (1.12-1.20) than on an Independent (0.67-0.97) and 80% likelier than on a Small label.
/// Distribution already bounds launch stock, restock service, shelf capacity and regional arrival, so this counts it a
/// second time as demand. P moves every label's scale that share of the way to 1.
///
/// --regional-arrival-chart-depth=N: a held region takes a single once it is inside the national top N (40 when off).
/// One-stops and independent distributors ordered off the whole Hot 100, not the top 40.
///
/// --regional-fit-exposure-national-rank=N: a connected single becomes fit-exposed in every region once it is inside the
/// national top N (40 when off; RegionalFitExposure.NationalRank). The same claim as the arrival depth, on the other gate.
///
/// --covered-launch-parity=F: in a region a label ships to, launch awareness and radio carry 0.55 + 0.45 x national reach
/// (Major about 0.95, Independent about 0.70), so a label's national reach weakens it in its own territory. F moves that
/// factor that share of the way to 1 for a non-Major AI label; strong regions (1.35) and uncovered regions are unchanged.
///
/// --major-craft-parity: a Major's founding scouting and production draws use the MidTier envelope. The Majors' studios
/// were the best equipped, but record production here is 40% of a single's quality, and in the 1960 singles market the
/// independents' records were not the worse made.
/// </summary>
public static class LabelFootprintParity {
	public const string DemandSwitch = "--label-demand-parity";
	public const string ArrivalSwitch = "--regional-arrival-chart-depth";
	public const string CraftSwitch = "--major-craft-parity";
	public const string CoveredLaunchSwitch = "--covered-launch-parity";
	public const string FitRankSwitch = "--regional-fit-exposure-national-rank";
	public static int FitExposureNationalRank { get; private set; } = RegionalFitExposure.NationalRank;
	public static float CoveredLaunchParity { get; private set; }
	private static bool configured;
	public static float DemandParity { get; private set; }
	public static int ArrivalChartDepth { get; private set; } = RegionalAudience.ArrivalChartDepth;

	/// <summary>Read at world generation, before the chart manager configures anything.</summary>
	public static readonly bool CraftParity = Array.IndexOf(SimulationArguments.Get(), CraftSwitch) >= 0;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		DemandParity = 0f; CoveredLaunchParity = 0f; ArrivalChartDepth = RegionalAudience.ArrivalChartDepth;
		FitExposureNationalRank = RegionalFitExposure.NationalRank;
		foreach (string arg in arguments ?? Array.Empty<string>()) {
			if (arg.StartsWith(DemandSwitch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(DemandSwitch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float p) ||
					!float.IsFinite(p) || p < 0f || p > 1f)
					throw new ArgumentException($"{DemandSwitch} must be between 0 and 1.");
				DemandParity = p;
			}
			if (arg.StartsWith(CoveredLaunchSwitch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(CoveredLaunchSwitch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
					!float.IsFinite(f) || f < 0f || f > 1f)
					throw new ArgumentException($"{CoveredLaunchSwitch} must be between 0 and 1.");
				CoveredLaunchParity = f;
			}
			if (arg.StartsWith(FitRankSwitch + "=", StringComparison.Ordinal)) {
				if (!int.TryParse(arg.Substring(FitRankSwitch.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank) ||
					rank < 1 || rank > 100)
					throw new ArgumentException($"{FitRankSwitch} must be between 1 and 100.");
				FitExposureNationalRank = rank;
			}
			if (arg.StartsWith(ArrivalSwitch + "=", StringComparison.Ordinal)) {
				if (!int.TryParse(arg.Substring(ArrivalSwitch.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ||
					n < 1 || n > 100)
					throw new ArgumentException($"{ArrivalSwitch} must be between 1 and 100.");
				ArrivalChartDepth = n;
			}
		}
		configured = true;
	}

	internal static void SetForChecks(float demandParity, int arrivalChartDepth = RegionalAudience.ArrivalChartDepth,
		float coveredLaunchParity = 0f, int fitExposureNationalRank = RegionalFitExposure.NationalRank) {
		configured = true; DemandParity = demandParity; ArrivalChartDepth = arrivalChartDepth;
		CoveredLaunchParity = coveredLaunchParity; FitExposureNationalRank = fitExposureNationalRank;
	}

	/// <summary>A covered region's launch factor for this label. Returns <paramref name="factor"/> exactly when off.</summary>
	public static float CoveredLaunch(float factor, AILabel label) =>
		CoveredLaunchParity <= 0f || label == null || label.isPlayerOwned || label.tier == LabelTier.Major || factor >= 1f
			? factor : factor + (1f - factor) * CoveredLaunchParity;

	/// <summary>The label demand scale moved toward 1. Returns <paramref name="scale"/> exactly when off.</summary>
	public static float Demand(float scale) => DemandParity <= 0f ? scale : scale + (1f - scale) * DemandParity;
}
