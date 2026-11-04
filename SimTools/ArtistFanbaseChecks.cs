using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ArtistFanbaseChecks {
	public static void Run() {
		int n = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Artist fanbase: " + why); n++; }
		bool Near(double x, double y) => Math.Abs(x - y) < .001;
		FanReleaseRegion Cohort() => new() { initial = new() { ["Youth"] = 600, ["AdultMOR"] = 400 } };
		var rng = GameRandom.State;
		try {
			Check(Near(HitActRenewalFinance.Reserve(500, 300, 100), 300), "renewal reserve preserves non-tax operating/slate holds and near-term tax");
			Check(HitActRenewalFinance.Eligible(true, false, true, 0, 1, 52), "profitable recent-hit renewal at boundary is eligible");
			Check(!HitActRenewalFinance.Eligible(true, false, true, -1, 1, 10), "distressed cash flow gets no renewal relaxation");
			Check(!HitActRenewalFinance.Eligible(true, true, true, 1, 1, 10), "player negotiations retain their own rules");
			Check(!HitActRenewalFinance.Eligible(false, false, true, 1, 1, 10), "inactive labels cannot be rescued by this gate");
			Check(!HitActRenewalFinance.Eligible(true, false, false, 1, 1, 10), "ordinary labels retain original reserve policy");
			Check(!HitActRenewalFinance.Eligible(true, false, true, 1, 0, 10), "cold contracts get no renewal relaxation");
			Check(!HitActRenewalFinance.Eligible(true, false, true, 1, 1, 53), "stale hits get no permanent renewal protection");
			Check(!HitActRenewalFinance.Eligible(true, false, true, 1, 1, -1), "future hit evidence cannot authorize a renewal");
			ArtistFanbase.SetForChecks(true, .25);
			var empty = new FanReleaseRegion();
			Check(ArtistFanbase.Blend(empty, 123.5f, 1e6, .7, .5, .12, 1, 0) == 123.5f, "no fans preserves original demand exactly");
			var a = Cohort();
			float raw = ArtistFanbase.Blend(a, 1000, 10000, .7, .5, .12, 1, 0);
			Check(Near(a.anonymousRaw, 900), "the launch cohort replaces its population share of anonymous demand");
			Check(Near(raw, a.anonymousRaw + a.fanRaw), "raw components reconcile");
			Check(a.fanRaw > 0 && a.fanRaw < 1000, "return intent bounded by the remaining fan cohort");
			double attributed = ArtistFanbase.SettleCohort(a, 100);
			Check(attributed > 0 && attributed < 100, "actual fan purchases are a share of cleared units");
			Check(Near(attributed, a.purchased.Values.Sum()), "cohort depletion equals attributed purchases");
			Check(a.initial.Values.Sum() == 1000, "immutable launch pool is not refilled by settlement");
			for (int i = 0; i < 200; i++) {
				ArtistFanbase.Blend(a, 0, 10000, 1, 1, 1, 1, 0);
				ArtistFanbase.SettleCohort(a, 1e6);
				Check(a.purchased.All(p => p.Value >= 0 && p.Value <= a.initial[p.Key] + 1e-8), "no title can sell repeatedly to the same launch cohort");
			}
			Check(Near(a.purchased.Values.Sum(), 1000), "a fully bought title exhausts its returning cohort");
			Check(ArtistFanbase.Blend(a, 1000, 10000, .8, .8, 1, 1, 0) == 900, "exhausted fans are not put back into anonymous demand");
			var noStock = Cohort(); ArtistFanbase.Blend(noStock, 1000, 10000, .7, .5, 1, 1, 0);
			Check(ArtistFanbase.SettleCohort(noStock, 0) == 0 && noStock.purchased.Values.Sum() == 0, "unfulfilled intent neither depletes nor experiences a purchase");
			var full = Cohort(); var shared = Cohort();
			ArtistFanbase.Blend(full, 1000, 10000, .7, .5, .12, 1, 0);
			ArtistFanbase.Blend(shared, 1000, 10000, .7, .5, .12, .5, 0);
			Check(Near(shared.fanRaw * 2, full.fanRaw), "concurrent titles share returning attention");
			var poor = Cohort(); ArtistFanbase.Blend(poor, 1000, 10000, .2, .5, .12, 1, 0);
			Check(poor.fanRaw < full.fanRaw, "fans still respond to material");
			var late = Cohort(); ArtistFanbase.Blend(late, 1000, 10000, .7, .5, .12, 1, 8);
			Check(Near(late.fanRaw * 2, full.fanRaw), "attention to a title cools while artist attachment can persist");
			var data = new ArtistFanbaseData { regions = new() { ["test"] = new() { ["Youth"] = 1000 } } };
			ArtistFanbase.Decay(data, 104); Check(Near(ArtistFanbase.Heads(data), 500), "calendar retention uses elapsed weeks");
			ArtistFanbase.Decay(data, 104); Check(Near(ArtistFanbase.Heads(data), 500), "duplicate calendar reads do not decay again");
			var record = new RecordRuntimeData(new Record { recordId = "fan-check", artistId = "fan-act", format = ReleaseFormat.Single,
				releaseDate = new GameDate(1961, 3, 1) }) { fanAudience = new() { launchWeek = 60, regions = new() { ["test"] = full } } };
			var opts = SaveGameService.TestJsonOptions;
			string json = JsonSerializer.Serialize(record, opts);
			var restored = JsonSerializer.Deserialize<RecordRuntimeData>(json, opts);
			Check(restored.fanAudience.regions["test"].initial["Youth"] == 600, "record launch cohort survives world JSON");
			Check(restored.fanAudience.regions["test"].purchased.Count == full.purchased.Count, "ownership/depletion survives JSON");
			Check(!json.Contains("anonymousRaw") && !json.Contains("pending"), "transient demand attribution is excluded from save state");
			Check(RuntimeRecordSaveData.From(record).ToRuntime().fanAudience.launchWeek == 60, "transfer/player DTO preserves the optional launch pool");
			var act = new SimulatedArtist { artistId = "fan-act", fanbase = data };
			var act2 = JsonSerializer.Deserialize<SimulatedArtist>(JsonSerializer.Serialize(act, opts), opts);
			Check(Near(ArtistFanbase.Heads(act2.fanbase), 500), "artist stock survives world/player JSON");
			ArtistFanbase.SetForChecks(false);
			Check(ArtistFanbase.Demand(record, null, .7f, .5f, 123f, null) == 123f, "off ignores all saved fan state");
			Check(GameRandom.State == rng, "no audience arithmetic consumes global RNG");
			GD.Print($"ARTIST_FANBASE_CHECK passed checks={n}");
		} finally { ArtistFanbase.SetForChecks(false); }
	}
	public static void CheckLiveState() {
		if (!ArtistFanbase.Enabled) return;
		var artists = ArtistManager.Instance.GetAllArtists().Where(a => a.fanbase != null).ToArray();
		var records = ChartManager.Instance.GetAllRecords().Where(r => r.fanAudience != null).ToArray();
		if (!artists.Any(a => a.fanbase.recruitedHeads > 0) || !records.Any(r => r.fanAudience.regions.Count > 0))
			throw new InvalidOperationException("Fanbase round trip has no acquired audience/launch snapshot.");
		if (artists.Any(a => a.fanbase.regions.Values.Any(r => r.Values.Any(v => !double.IsFinite(v) || v < 0))) ||
			records.Any(r => r.fanAudience.regions.Values.Any(c => c.purchased.Any(p => p.Value < 0 || p.Value > c.initial[p.Key] + .0001))))
			throw new InvalidOperationException("Invalid audience or title depletion state.");
		GD.Print($"ARTIST_FANBASE_LIVE passed artists={artists.Length} launches={records.Length} heads={artists.Sum(a => ArtistFanbase.Heads(a.fanbase)):F0}");
	}
}
