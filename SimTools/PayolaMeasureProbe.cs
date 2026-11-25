using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
/// Measures what each payola action buys and what it risks, on the real reporter panel and the ledger's own rule functions
/// (PayolaLedger.CashIntensity / PromoterIntensity / WeeklyDetectionChance / SeverityRoll / Penalty / ExposureFor). It places
/// nothing in the world: each trial is a Monte Carlo replay of PayolaLedger.Tick's order (decay, expire, detect, deliver, heat)
/// against one station set, so the numbers are the ledger's, not a model of it.
///
/// Reported per action and year: boost-weeks delivered (sum over stations and weeks of the candidacy multiplier's payola term,
/// the same channel and currency as a legal ad buy's advocacy), dollars per boost-week, P(bust), severity mix, expected fine, the
/// trail left on the label. Jack Germain's boys are also run as a decade: one push each December from 1960, the trail carried
/// between pushes, to show how the risk grows with use.
///   --path . SimTools/PayolaMeasureProbe.tscn -- --seed=1001 [--trials=4000] [--home=detroit]
/// Writes SimLogs/payola-measure-&lt;seed&gt;.txt and prints PAYOLA_MEASURE_DONE.
/// </summary>
public partial class PayolaMeasureProbe : Node {
	private readonly StringBuilder log = new();
	private void P(string s) { log.AppendLine(s); GD.Print(s); }

	private int trials = 4000;
	private readonly Random rand = new(20261124);

	public override void _Ready() {
		try {
			var args = OS.GetCmdlineUserArgs();
			trials = int.TryParse(args.FirstOrDefault(a => a.StartsWith("--trials="))?[9..], out int t) ? t : 4000;
			string home = args.FirstOrDefault(a => a.StartsWith("--home="))?[7..] ?? "detroit";
			string seed = args.FirstOrDefault(a => a.StartsWith("--seed="))?[7..] ?? "1001";
			var chart = ChartManager.Instance;
			var all = chart.AllReporterStations.Where(s => s != null).ToList();
			P($"Payola measure, seed {seed}, {trials} trials per cell, home {home}");
			P($"Reporter stations: {all.Count} in {all.Select(s => s.regionId).Distinct().Count()} regions");
			foreach (var g in all.GroupBy(s => s.regionId).OrderBy(g => g.Key))
				P($"  {g.Key,-22} {g.Count(),2} reporters  susceptibility {g.Average(s => s.payolaSusceptibility):F2}  integrityRisk {g.Average(s => s.integrityRisk):F2}  greed {g.Average(s => chart.GetDeejay(s.leadDjId)?.greed ?? .3f):F2}");
			string region = DistanceModel.GetCityById(home).parentRegionId;
			var homeStations = all.Where(s => s.regionId == region).OrderBy(s => s.stationId, StringComparer.Ordinal).ToList();
			P($"Home region {region}: {homeStations.Count} reporters");
			P("");

			int[] years = { 1960, 1962, 1964, 1966, 1968 };
			P("Legal reference (no risk): Rolodex ad buys, advocacy boost x weeks on one station");
			foreach (var (name, cost, boost, weeks) in new[] { ("Spot Buy", 150f, .08f, 2), ("Promo Package", 400f, .15f, 3), ("Full Sponsorship", 800f, .24f, 4) })
				P($"  {name,-18} ${cost,5:N0}  {boost * weeks,5:F2} boost-weeks  ${cost / (boost * weeks),6:N0} per boost-week");
			P("");

			P("action                         year  boost-wk  $/boost-wk  P(bust)  warn/fine/ban/region   E[fine]  fine|bust  trail  risk$/boost-wk");
			foreach (int y in years) {
				foreach (var (tier, budget) in new[] { ("Envelope", 200f), ("Package", 500f), ("Heavy Package", 1000f) })
					Row($"{tier} ${budget:N0}", y, budget, () => CashSet(homeStations, chart, budget, y));
				foreach (var (tier, cost, count, eff) in new[] { ("Promo S", 25f, 1, .30f), ("Promo M", 50f, 2, .45f), ("Promo L", 75f, 3, .60f) })
					Row($"Project {tier} ${cost:N0}", y, cost, () => PromoSet(homeStations, cost, count, eff));
				foreach (int use in new[] { 0, 2, 4 })
					Row($"Jack's boys, use {use + 1}", y, PlayerDesk.GermainNetworkFee, () => JackSet(homeStations, use));
				P("");
			}

			P("Jack's boys as a decade: one push each December from 1960, the trail carried between them (no other payola)");
			JackDecade(homeStations, 0f);
			P("...and with his name on two songs a year and his fees on the books (PlayerDesk.Germain trail)");
			JackDecade(homeStations, 2 * PlayerDesk.GermainNameExposure + PlayerDesk.GermainFeeExposure);
			P($"His name: {PlayerDesk.GermainCutInShare:P0} of the writer's half of the {MechanicalRoyaltyService.RatePerCopy * 100f:F0}c mechanical = " +
				$"{PlayerDesk.GermainCutInShare * MechanicalRoyaltyService.RatePerCopy * 50f:F2}c a copy, ${PlayerDesk.GermainCutInShare * MechanicalRoyaltyService.RatePerCopy * 0.5f * 10000f:N0} per 10,000 copies.");

			string path = ProjectSettings.GlobalizePath($"res://SimLogs/payola-measure-{seed}.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, log.ToString());
			GD.Print("PAYOLA_MEASURE_DONE " + path);
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("PAYOLA_MEASURE_FAIL: " + e);
			GetTree().Quit(1);
		}
	}

	// ---- arrangement sets: what one action places ----

	private sealed record Spec(RadioStation Station, PayolaMethod Method, float Intensity, float Decay, int Weeks, float Paid, float Discretion, float Mob);

	private List<Spec> CashSet(List<RadioStation> stations, ChartManager chart, float budget, int year) {
		var s = stations[rand.Next(stations.Count)];
		float heat = RadioEra.RegulatoryHeat(year, 12);
		float i = PayolaLedger.CashIntensity(budget, heat, chart.GetDeejay(s.leadDjId)?.greed ?? .3f, s.payolaSusceptibility);
		return new() { new Spec(s, PayolaMethod.Cash, i, .35f, 3, budget, 0f, 0f) };
	}

	private List<Spec> PromoSet(List<RadioStation> stations, float cost, int count, float tierEff) {
		var picks = stations.OrderBy(_ => rand.Next()).Take(count).ToList();
		var list = new List<Spec>();
		foreach (var s in picks) {
			float eff = Mathf.Clamp(tierEff * (.6f + s.payolaSusceptibility * .8f), .1f, .9f);
			list.Add(new Spec(s, PayolaMethod.IndiePromoter, PayolaLedger.PromoterIntensity(eff), .15f, PlayerDesk.ProjectPromoWeeks, cost / picks.Count, .5f, .1f));
		}
		return list;
	}

	private List<Spec> JackSet(List<RadioStation> stations, int priorUses) {
		float disc = Mathf.Max(PlayerDesk.GermainDiscretionFloor, PlayerDesk.GermainNetworkDiscretion - PlayerDesk.GermainDiscretionLostPerUse * priorUses);
		var picks = stations.Take(PlayerDesk.GermainNetworkStationCount(stations.Count)).ToList();
		return picks.Select(s => new Spec(s, PayolaMethod.IndiePromoter, PayolaLedger.PromoterIntensity(PlayerDesk.GermainNetworkEffectiveness), .15f,
			PlayerDesk.GermainNetworkWeeks, PlayerDesk.GermainNetworkFee / picks.Count, disc, PlayerDesk.GermainNetworkMob)).ToList();
	}

	// ---- one replay of PayolaLedger.Tick for a set ----

	private sealed class Outcome { public float boostWeeks; public bool bust; public ScandalSeverity sev; public float fine; public float trailAfter; }

	/// <summary>Replays the ledger week by week. <paramref name="exposure"/> is the label's trail when the set is placed.</summary>
	private Outcome Replay(List<Spec> set, int year, float exposure) {
		var o = new Outcome();
		var intensity = set.Select(s => s.Intensity).ToArray();
		var heat = set.Select(_ => 0f).ToArray();
		var exposed = new bool[set.Count];
		foreach (var s in set) exposure = Mathf.Min(PayolaLedger.EXPOSURE_CAP, exposure + PayolaLedger.ExposureFor(s.Method, s.Discretion, s.Paid));
		int maxWeeks = set.Max(s => s.Weeks);
		for (int w = 1; w <= maxWeeks && !o.bust; w++) {
			float era = RadioEra.RegulatoryHeat(year + (11 + w) / 12, ((11 + w) % 12) + 1);   // placed in December
			exposure *= PayolaLedger.EXPOSURE_WEEKLY_KEEP;
			for (int k = 0; k < set.Count; k++) heat[k] *= PayolaLedger.STATION_HEAT_WEEKLY_KEEP;
			bool any = false;
			for (int k = 0; k < set.Count; k++) {
				intensity[k] *= 1f - set[k].Decay;
				if (w >= set[k].Weeks || intensity[k] < .02f) { exposed[k] = true; continue; }
				any = true;
			}
			if (!any) break;
			for (int k = 0; k < set.Count && !o.bust; k++) {
				if (exposed[k]) continue;
				var s = set[k];
				float chance = PayolaLedger.WeeklyDetectionChance(s.Method, s.Discretion, era, s.Station.integrityRisk, intensity[k], heat[k], exposure);
				if (rand.NextDouble() < chance) {
					o.bust = true;
					o.sev = PayolaLedger.SeverityOf(PayolaLedger.SeverityRoll(era, intensity[k], s.Mob, exposure));
					o.fine = PayolaLedger.Penalty(o.sev, s.Paid);
					exposure = Mathf.Min(PayolaLedger.EXPOSURE_CAP, exposure + .2f + .1f * (int)o.sev);
				}
			}
			if (o.bust) break;   // the bust week delivers nothing (the boost cache drops exposed arrangements), and a network goes dark together
			for (int k = 0; k < set.Count; k++) {
				if (exposed[k]) continue;
				o.boostWeeks += Mathf.Min(1.5f, intensity[k]);
				heat[k] = Mathf.Min(1f, heat[k] + intensity[k] * PayolaLedger.STATION_HEAT_PER_WEEK);
			}
		}
		o.trailAfter = exposure;
		return o;
	}

	private void Row(string name, int year, float cost, Func<List<Spec>> make) {
		float boost = 0, fine = 0; int busts = 0; var sev = new int[4]; float trail = 0;
		for (int i = 0; i < trials; i++) {
			var o = Replay(make(), year, 0f);
			boost += o.boostWeeks; trail += o.trailAfter;
			if (o.bust) { busts++; sev[(int)o.sev]++; fine += o.fine; }
		}
		float n = trials, bw = boost / n, ef = fine / n;
		string mix = busts == 0 ? "-" : string.Join("/", sev.Select(c => $"{100f * c / busts:F0}"));
		P($"{name,-30} {year}  {bw,7:F2}  {cost / Math.Max(.001f, bw),9:N0}  {100f * busts / n,6:F1}%  {mix,-20} {ef,8:N0}  {(busts == 0 ? 0 : fine / busts),8:N0}  {trail / n,5:F2}  {(cost + ef) / Math.Max(.001f, bw),8:N0}");
	}

	private void JackDecade(List<RadioStation> stations, float extraTrailPerYear) {
		var bustByUse = new int[10]; var anyByUse = new int[10]; var fineByUse = new float[10]; var trailAt = new float[10];
		for (int i = 0; i < trials; i++) {
			float exposure = 0f; bool ever = false;
			for (int use = 0; use < 10; use++) {
				int year = 1960 + use;
				exposure = Mathf.Min(PayolaLedger.EXPOSURE_CAP, exposure + extraTrailPerYear);
				trailAt[use] += exposure;
				var o = Replay(JackSet(stations, use), year, exposure);
				if (o.bust) { bustByUse[use]++; fineByUse[use] += o.fine; ever = true; }
				if (ever) anyByUse[use]++;
				exposure = o.trailAfter * Mathf.Pow(PayolaLedger.EXPOSURE_WEEKLY_KEEP, 52 - PlayerDesk.GermainNetworkWeeks);
			}
		}
		P("  push  Dec   discretion  trail-at-push  P(bust)  cum P(any)  E[fine]");
		for (int use = 0; use < 10; use++) {
			float disc = Mathf.Max(PlayerDesk.GermainDiscretionFloor, PlayerDesk.GermainNetworkDiscretion - PlayerDesk.GermainDiscretionLostPerUse * use);
			P($"  {use + 1,4}  {1960 + use}  {disc,9:F2}  {trailAt[use] / trials,12:F2}  {100f * bustByUse[use] / trials,6:F1}%  {100f * anyByUse[use] / trials,8:F1}%  {fineByUse[use] / trials,7:N0}");
		}
		P("");
	}
}
