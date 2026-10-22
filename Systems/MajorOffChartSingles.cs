using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Demand probe: the Majors' off-chart singles base. History's majors sold steady 45s that no pop survey
/// counted: catalog reissue lines (RCA's Gold Standard series, Columbia's Hall of Fame), country sold through
/// country outlets and operators' country boxes, standards and MOR for adult buyers and location jukeboxes,
/// and specialty lines (children's, polka, Latin, sacred). Operators alone bought about 47M records in 1959
/// (Billboard, 1960-05-09), programmed by location rather than by the Hot 100. Off, a Major earns singles units
/// only from live records, so matching the FTC's 1960 residual (~32% of singles dollars for the Big-5) would
/// need Majors to dominate the chart.
///
/// When on, each active AI Major without a distribution deal sells a weekly volume equal to a share of the
/// week's settled live single units, split by national reach. The units are booked through the label's
/// accounts at the ordinary single price and costs, but belong to no record: they never enter chart points,
/// release outcomes or any label's priors. The share is a calibration assumption; no sourced split of the
/// Majors' singles sales between charted and uncharted titles exists. No random draws and no saved state;
/// the cumulative totals are audit telemetry only.
/// </summary>
public static class MajorOffChartSingles {
	public const string SharePrefix = "--major-offchart-singles=";
	/// <summary>Takes the base out of the same buyers. Live single sales are budget-bound (the regional single
	/// budget binds in ~90% of region-weeks, with serviceable intent about twice the budget), so scaling demand
	/// moves nothing. The hold shrinks each region's single budget, and its purchase capacity, by 1 / (1 + share):
	/// the base's copies then replace live copies instead of adding to them.</summary>
	public const string HoldFlag = "--major-offchart-singles-hold-units";
	private static bool configured;
	public static float Share { get; private set; }
	public static bool HoldUnits { get; private set; }
	public static bool Enabled => Share > 0f;

	public sealed class Totals {
		public string LabelId, Tier;
		public long Units;
		public double Gross, Net;
	}
	private static readonly Dictionary<string, Totals> totals = new(StringComparer.Ordinal);
	public static IEnumerable<Totals> Cumulative => totals.Values.OrderBy(t => t.LabelId, StringComparer.Ordinal);

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string raw = args.FirstOrDefault(a => a.StartsWith(SharePrefix, StringComparison.Ordinal))?.Substring(SharePrefix.Length);
		float share = 0f;
		if (raw != null && (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out share) || share < 0f || share > .5f))
			throw new ArgumentException(SharePrefix + " takes a number from 0 to 0.5.");
		Share = share;
		HoldUnits = args.Contains(HoldFlag);
		if (HoldUnits && !Enabled) throw new ArgumentException(HoldFlag + " requires " + SharePrefix + ".");
		configured = true;
	}

	internal static void SetForChecks(float share, bool holdUnits = false) {
		configured = true; Share = share; HoldUnits = holdUnits; totals.Clear(); monthNet.Clear(); recentMonthNets.Clear();
	}

	/// <summary>A region's single budget for live records once the base has taken its share of the buyers.</summary>
	public static int HeldSingleBudget(int budget) =>
		Enabled && HoldUnits && budget > 0 ? (int)Math.Round(budget / (1d + Share)) : budget;

	public static bool Eligible(AILabel label) =>
		label != null && !label.isPlayerOwned && label.IsActive && label.tier == LabelTier.Major && label.activeDeal == null;

	/// <summary>This week's off-chart units per eligible label, from the week's settled live single units.
	/// Largest-remainder rounding keeps the total exact and the split stable by label ID.</summary>
	public static Dictionary<string, int> Allocate(IEnumerable<AILabel> labels, long liveSingleUnits) {
		var result = new Dictionary<string, int>(StringComparer.Ordinal);
		if (!Enabled || liveSingleUnits <= 0) return result;
		var eligible = labels.Where(Eligible).OrderBy(l => l.labelId, StringComparer.Ordinal)
			.Select(l => (l.labelId, weight: (double)Math.Clamp(l.effectiveNationalReach, 0f, 1f))).Where(e => e.weight > 0).ToList();
		double totalWeight = eligible.Sum(e => e.weight);
		if (totalWeight <= 0) return result;
		long pool = (long)Math.Round(liveSingleUnits * (double)Share);
		var exact = eligible.Select(e => (e.labelId, value: pool * e.weight / totalWeight)).ToList();
		long assigned = 0;
		foreach (var (id, value) in exact) { int floor = (int)Math.Floor(value); result[id] = floor; assigned += floor; }
		foreach (var (id, _) in exact.OrderByDescending(e => e.value - Math.Floor(e.value)).ThenBy(e => e.labelId, StringComparer.Ordinal)) {
			if (assigned >= pool) break;
			result[id]++; assigned++;
		}
		return result;
	}

	public static void Record(AILabel label, int units, double gross, double net) {
		if (!totals.TryGetValue(label.labelId, out var row)) totals[label.labelId] = row = new Totals { LabelId = label.labelId };
		row.Tier = label.tier.ToString();
		row.Units += units; row.Gross += gross; row.Net += net;
		monthNet[label.labelId] = monthNet.GetValueOrDefault(label.labelId) + net;
	}

	// The status review's "Rising" bar reads trailing cash flow as release momentum. A back catalog is steady
	// division profit, not momentum: a period Major's singles schedule followed its A&R organization, not its
	// catalog receipts (ReleaseJudgementDirective §6). The base's own net is therefore kept in the same
	// three-month window and taken out of that one test; solvency still counts it. Not saved: a resumed
	// world counts the base toward Rising until the window refills.
	private static readonly Dictionary<string, double> monthNet = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, List<double>> recentMonthNets = new(StringComparer.Ordinal);
	public const int MomentumWindowMonths = 3;

	/// <summary>Closes this label's month (once per status review) and returns the base's trailing mean monthly
	/// net, over the same window as <see cref="AILabel.RecentMonthlyCashFlow"/>. Zero when the base is off.</summary>
	public static float CloseMonthForMomentum(AILabel label) {
		if (!Enabled || label == null) return 0f;
		if (!recentMonthNets.TryGetValue(label.labelId, out var window)) recentMonthNets[label.labelId] = window = new List<double>();
		window.Add(monthNet.GetValueOrDefault(label.labelId));
		monthNet[label.labelId] = 0d;
		if (window.Count > MomentumWindowMonths) window.RemoveAt(0);
		return (float)window.Average();
	}
}
