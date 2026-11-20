using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Store-report sales score for the Hot 100 (Big5ChartOpportunityInvestigation.md, "Field hit-act follow-ups"), off unless named.
///
/// Billboard's 1960 sales component was compiled from ranked dealer reports market by market, not from a unit count. A
/// record near the top of one market's list scored there however little it sold elsewhere, and a record sold thinly
/// everywhere scored nowhere once it fell off the market lists. The proportional regional bonus
/// (<see cref="RegionalChartAction"/>) could not single out regional sellers, because newcomers' charting records are as
/// concentrated as their near-misses; a rank list is not proportional to the record's own units.
///
/// <c>--chart-regional-rank=B</c>: each region ranks its singles by this week's units. A rank r up to the list depth D earns
/// w(r) = (C(r) - C(D + 1)) / (C(1) - C(D + 1)), C the Carroll (2015) chart-score curve -4357 r / (r + 10) + 4139, so the
/// weight falls to zero just past the list. A record's store score is the sum over regions of q x w(rank), q the region's
/// population share raised to <c>--chart-regional-rank-market-weight=A</c> (default 1) and renormalized; A below 1
/// counts small markets' lists more than their buyers. The score is put in national units by the factor that gives the
/// week's top 100 by units the same total score as units, and sales points become (1 - B) x units + B x that. Computed
/// once per week before ranking and cached on the record, so the chart audit's recomputation stays exact; draws nothing.
/// Off: sales points are national units.
/// </summary>
public static class RegionalChartRank {
	public const string Prefix = "--chart-regional-rank=";
	public const string DepthPrefix = "--chart-regional-rank-depth=";
	public const string MarketWeightPrefix = "--chart-regional-rank-market-weight=";
	public const string CheckSwitch = "--chart-regional-rank-check";
	private const int ScaleChartSize = 100;
	private static bool configured;
	public static bool Enabled { get; private set; }
	public static float Blend { get; private set; }
	public static int Depth { get; private set; } = 50;
	public static float MarketWeight { get; private set; } = 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string One(string prefix) {
			var v = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
			if (v.Length > 1) throw new ArgumentException("Name " + prefix + " once.");
			return v.Length == 1 ? v[0][prefix.Length..] : null;
		}
		float Parse(string text, string prefix, float lo, float hi) {
			if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || !float.IsFinite(v) || v < lo || v > hi)
				throw new ArgumentException($"{prefix} needs a value in [{lo}, {hi}].");
			return v;
		}
		string blend = One(Prefix), depth = One(DepthPrefix), market = One(MarketWeightPrefix);
		if (blend != null) {
			Blend = Parse(blend, Prefix, 0f, 1f);
			if (Blend <= 0f) throw new ArgumentException(Prefix + " needs a blend above 0.");
			Enabled = true;
		}
		if ((depth != null || market != null) && !Enabled) throw new ArgumentException(DepthPrefix + " and " + MarketWeightPrefix + " need " + Prefix);
		if (depth != null) Depth = (int)Parse(depth, DepthPrefix, 5f, 200f);
		if (market != null) MarketWeight = Parse(market, MarketWeightPrefix, 0f, 1f);
		if (Enabled) GD.Print($"CHART_REGIONAL_RANK on blend={Blend.ToString("0.###", CultureInfo.InvariantCulture)} depth={Depth} marketWeight={MarketWeight.ToString("0.###", CultureInfo.InvariantCulture)}");
		configured = true;
	}

	internal static void SetForChecks(bool on, float blend = .5f, int depth = 50, float marketWeight = 1f) {
		configured = true; Enabled = on; Blend = blend; Depth = depth; MarketWeight = marketWeight;
	}

	private static double Carroll(int rank) => -4357d * rank / (rank + 10d) + 4139d;

	/// <summary>List weight of a regional rank: 1 at No. 1, zero from D + 1.</summary>
	internal static double Weight(int rank) {
		if (rank < 1 || rank > Depth) return 0d;
		double floor = Carroll(Depth + 1);
		return Math.Max(0d, (Carroll(rank) - floor) / (Carroll(1) - floor));
	}

	/// <summary>Ranks each region's singles and caches every single's store score in national units. Off: nothing written.</summary>
	public static void UpdateWeek(IEnumerable<RecordRuntimeData> records, MarketRegion[] regions) {
		if (!Enabled || records == null || regions == null) return;
		var singles = records.Where(r => r.baseRecord.format != ReleaseFormat.Album).ToList();
		var score = new Dictionary<RecordRuntimeData, double>(singles.Count);
		foreach (var r in singles) { r.regionalRankUnitsThisWeek = 0f; score[r] = 0d; }

		double totalWeight = 0d;
		var marketWeight = new double[regions.Length];
		for (int i = 0; i < regions.Length; i++) {
			float pop = regions[i].EconomicPopulation;
			marketWeight[i] = pop > 0f ? Math.Pow(pop, MarketWeight) : 0d;
			totalWeight += marketWeight[i];
		}
		if (totalWeight <= 0d) return;

		var list = new List<(int Units, string Id, RecordRuntimeData Record)>(singles.Count);
		for (int i = 0; i < regions.Length; i++) {
			if (marketWeight[i] <= 0d) continue;
			list.Clear();
			foreach (var r in singles)
				if (r.regionalData.TryGetValue(regions[i].regionId, out RegionalRecordData data) && LocalPromotion.Units(data, LocalPromotion.FeedbackGroup.Audience) > 0)
					list.Add((LocalPromotion.Units(data, LocalPromotion.FeedbackGroup.Audience), r.baseRecord.recordId, r));
			list.Sort((a, b) => a.Units != b.Units ? b.Units.CompareTo(a.Units) : string.CompareOrdinal(a.Id, b.Id));
			double q = marketWeight[i] / totalWeight;
			for (int k = 0; k < list.Count && k < Depth; k++) score[list[k].Record] += q * Weight(k + 1);
		}

		// Units per score point: the week's top 100 by units carry the same total either way.
		var top = singles.Where(r => r.unitsThisWeek > 0).OrderByDescending(r => r.unitsThisWeek)
			.ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal).Take(ScaleChartSize).ToList();
		double topScore = top.Sum(r => score[r]);
		if (topScore <= 0d) return;
		double unitsPerPoint = top.Sum(r => (double)r.unitsThisWeek) / topScore;
		foreach (var r in singles) r.regionalRankUnitsThisWeek = (float)(score[r] * unitsPerPoint);
	}

	/// <summary>Sales points: national units, blended with the cached store score when on.</summary>
	public static float SalesPoints(RecordRuntimeData record, float units) =>
		Enabled ? (1f - Blend) * units + Blend * record.regionalRankUnitsThisWeek : units;

	public static void RunChecks() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Chart regional rank: " + why); }
		bool Near(double a, double b, double tol = 1e-3) => Math.Abs(a - b) < tol;
		try {
			SetForChecks(true, .5f, 50);
			Check(Near(Weight(1), 1d) && Weight(51) == 0d && Weight(50) > 0d && Weight(10) > Weight(20), "weight: 1 at No. 1, falling, zero past the list");

			var regions = new[] { ("a", 1f), ("b", 1f), ("c", 2f) }.Select(t => new MarketRegion { regionId = t.Item1, population = t.Item2 }).ToArray();
			RecordRuntimeData Rec(string id, params int[] units) {
				var r = new RecordRuntimeData(new Record { recordId = id, format = ReleaseFormat.Single });
				for (int i = 0; i < units.Length; i++) r.regionalData[regions[i].regionId] = new RegionalRecordData { regionId = regions[i].regionId, unitsSoldThisWeek = units[i] };
				r.unitsThisWeek = units.Sum();
				return r;
			}
			// The even seller tops every list; the local seller is No. 2 in small region a only; the thin seller is No. 3 in a, No. 2 elsewhere.
			var even = Rec("even", 100, 100, 200);
			var local = Rec("local", 60, 0, 0);
			var thin = Rec("thin", 10, 10, 20);
			var all = new[] { even, local, thin };

			SetForChecks(false);
			RegionalChartRank.UpdateWeek(all, regions);
			Check(even.regionalRankUnitsThisWeek == 0f && SalesPoints(local, 60f) == 60f, "off: nothing cached, national units");

			SetForChecks(true, .5f, 50, 1f);
			UpdateWeek(all, regions);
			double w1 = Weight(1), w2 = Weight(2), w3 = Weight(3);
			// Population shares .25/.25/.5: even = 1 x w1; local = .25 w2; thin = .25 w3 + .25 w2 + .5 w2.
			double sEven = w1, sLocal = .25 * w2, sThin = .25 * w3 + .75 * w2;
			double upp = 500d / (sEven + sLocal + sThin);
			Check(Near(even.regionalRankUnitsThisWeek, sEven * upp, .05) && Near(local.regionalRankUnitsThisWeek, sLocal * upp, .05),
				"score: population-weighted list weights in national units");
			Check(Near(even.regionalRankUnitsThisWeek + local.regionalRankUnitsThisWeek + thin.regionalRankUnitsThisWeek, 500d, .05),
				"scale: the top 100 keep their total");
			Check(Near(SalesPoints(local, 60f), .5 * 60 + .5 * local.regionalRankUnitsThisWeek, .01), "blend: (1 - B) units + B score");

			// Market weight 0: every list counts equally, so the small-region seller gains.
			float before = local.regionalRankUnitsThisWeek / even.regionalRankUnitsThisWeek;
			SetForChecks(true, .5f, 50, 0f);
			UpdateWeek(all, regions);
			Check(local.regionalRankUnitsThisWeek / even.regionalRankUnitsThisWeek > before + 1e-4, "market weight below 1 counts a small market's list more");

			// Depth 5: No. 6 scores nothing.
			SetForChecks(true, .5f, 5, 1f);
			Check(Weight(6) == 0d && Weight(5) > 0d, "depth: zero past the list");
			GD.Print("CHART_REGIONAL_RANK_CHECK passed");
		} finally { SetForChecks(false); configured = false; }
	}
}
