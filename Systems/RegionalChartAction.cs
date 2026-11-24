using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Regional action in the Hot 100 (Big5ChartOpportunityInvestigation.md, "Field hit-act follow-ups"), off unless named.
///
/// Billboard's 1960 Hot 100 was compiled from ranked store reports market by market, so a record selling strongly in one
/// market reported like a hit there and charted low nationally on that strength. The sim ranks national units, so a
/// regional seller needs national-sized units to chart. History has about 426 chart events a year by acts without a recent
/// Top 10, 84% of them debuting at 81-100; the sim has about 350, 74% at the bottom. About 150 never-charting newcomer
/// singles a seed had a week in which one region alone sold 40% of the national No. 100's units, with ~90% of their sales
/// there.
///
/// <c>--chart-regional-action=L</c>: a single's chart sales points gain L x (its best region's national-equivalent units -
/// its national units), where national-equivalent = regional units / the region's population share, capped at the
/// record's own units (so regional reports at most double its sales points). An evenly sold record gains nothing.
/// <c>--chart-regional-action-skew=K</c> (default 1): pay only the excess over K x national units, so records whose best
/// region merely runs above its share gain nothing. Reads
/// stored state only, so the chart audit's recomputation stays exact; draws nothing. Off: sales points are national units.
/// </summary>
public static class RegionalChartAction {
	public const string Prefix = "--chart-regional-action=";
	public const string SkewPrefix = "--chart-regional-action-skew=";
	public const string CheckSwitch = "--chart-regional-action-check";
	private static bool configured;
	public static bool Enabled { get; private set; }
	public static float Lambda { get; private set; }
	public static float Skew { get; private set; } = 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + Prefix + " once.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0][Prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || !float.IsFinite(v) || v <= 0f || v > 1f)
				throw new ArgumentException(Prefix + " needs a weight in (0, 1].");
			Enabled = true;
			Lambda = v;
		}
		var skews = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(SkewPrefix, StringComparison.Ordinal)).ToArray();
		if (skews.Length > 1) throw new ArgumentException("Name " + SkewPrefix + " once.");
		if (skews.Length == 1) {
			if (!Enabled) throw new ArgumentException(SkewPrefix + " needs " + Prefix);
			if (!float.TryParse(skews[0][SkewPrefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float k) || !float.IsFinite(k) || k < 1f || k > 20f)
				throw new ArgumentException(SkewPrefix + " needs a multiple in [1, 20].");
			Skew = k;
		}
		if (Enabled) GD.Print($"CHART_REGIONAL_ACTION on lambda={Lambda.ToString("0.###", CultureInfo.InvariantCulture)} skew={Skew.ToString("0.###", CultureInfo.InvariantCulture)}");
		configured = true;
	}

	internal static void SetForChecks(bool on, float lambda = .2f, float skew = 1f) { configured = true; Enabled = on; Lambda = lambda; Skew = skew; }

	/// <summary>The single's sales points: national units, plus the regional-action bonus when on.</summary>
	public static float SalesPoints(RecordRuntimeData record, MarketRegion[] regions) {
		float units = record.unitsThisWeek;
		if (!Enabled || units <= 0f || regions == null) return units;
		double totalPopulation = 0d;
		foreach (var region in regions) if (record.regionalData.ContainsKey(region.regionId)) totalPopulation += region.EconomicPopulation;
		if (totalPopulation <= 0d) return units;
		double best = 0d;
		foreach (var region in regions) {
			if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data) || region.EconomicPopulation <= 0f) continue;
			best = Math.Max(best, LocalPromotion.Units(data, LocalPromotion.FeedbackGroup.Audience) / (region.EconomicPopulation / totalPopulation));
		}
		double bonus = Math.Min(units, Lambda * Math.Max(0d, best - Skew * units));
		return (float)(units + bonus);
	}

	public static void RunChecks() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Chart regional action: " + why); }
		bool Near(double a, double b) => Math.Abs(a - b) < 1e-3;
		var regions = new[] { "a", "b", "c", "d" }.Select(id => new MarketRegion { regionId = id, population = 1f }).ToArray();
		RecordRuntimeData Rec(params int[] units) {
			var r = new RecordRuntimeData(new Record { recordId = "r" + Guid.NewGuid().ToString("N"), format = ReleaseFormat.Single });
			for (int i = 0; i < units.Length; i++) r.regionalData[regions[i].regionId] = new RegionalRecordData { regionId = regions[i].regionId, unitsSoldThisWeek = units[i] };
			r.unitsThisWeek = units.Sum();
			return r;
		}
		try {
			SetForChecks(false);
			Check(RegionalChartAction.SalesPoints(Rec(100, 0, 0, 0), regions) == 100f, "off: national units");
			SetForChecks(true, .2f);
			Check(Near(SalesPoints(Rec(25, 25, 25, 25), regions), 100d), "even sales gain nothing");
			// 100 units all in one of four equal regions: equivalent 400, bonus .2 x 300 = 60.
			Check(Near(SalesPoints(Rec(100, 0, 0, 0), regions), 160d), "a regional seller gains L x (equivalent - national)");
			SetForChecks(true, 1f);
			Check(Near(SalesPoints(Rec(100, 0, 0, 0), regions), 200d), "the bonus is capped at the record's own units");
			Check(SalesPoints(Rec(0, 0, 0, 0), regions) == 0f, "no sales, no points");
			// Skew 3: 100 units all in one of four regions is 4 x national, bonus .5 x (400 - 300) = 50; half in one region
			// (2 x national) gains nothing.
			SetForChecks(true, .5f, 3f);
			Check(Near(SalesPoints(Rec(100, 0, 0, 0), regions), 150d), "skew: only the excess over K x national is paid");
			Check(Near(SalesPoints(Rec(50, 50, 0, 0), regions), 100d), "skew: a record below K x national gains nothing");
			GD.Print("CHART_REGIONAL_ACTION_CHECK passed");
		} finally { SetForChecks(false); configured = false; }
	}
}
