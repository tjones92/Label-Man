using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class RackJobberChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("Rack jobbers: " + name); checks++; }
		RecordRuntimeData Album(string id, string labelId, int storeUnits, int position = 0, int weeksSincePeak = 0,
			Genre genre = Genre.TraditionalPop, string region = "r1", bool player = false) {
			var record = new RecordRuntimeData(new Record {
				recordId = id, labelId = labelId, format = ReleaseFormat.Album, primaryGenre = genre, isPlayerOwned = player,
				hookStrength = .7f, productionQuality = .7f, danceability = .5f
			}) { currentPosition = position, weeksSincePeakUnits = weeksSincePeak, awareness = .2f, wordOfMouth = .3f };
			record.regionalData[region] = new RegionalRecordData(region) { unitsSoldThisWeek = storeUnits, unitsSoldTotal = storeUnits * 3 };
			return record;
		}
		RackJobberChannel.Candidate Cand(string id, int units, int tier = 0, bool pastPeak = false) =>
			new() { Record = Album(id, "l", units), StoreUnits = units, Tier = tier, PastPeak = pastPeak };
		try {
			// Switches.
			bool Rejects(params string[] args) {
				try { RackJobberChannel.ResetForChecks(); RackJobberChannel.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			Check(Rejects(RackJobberChannel.ScaleSwitch + "0.2"), "the scale needs the channel");
			Check(Rejects(RackJobberChannel.SharedSwitch), "shared exhaustion needs the channel");
			Check(Rejects(RackJobberChannel.ReturnsSwitch + "0.1"), "returns need the channel");
			foreach (string rate in new[] { "-0.1", "1", "NaN", "x" })
				Check(Rejects(RackJobberChannel.Switch, RackJobberChannel.ReturnsSwitch + rate), "invalid return rate " + rate);
			Check(!Rejects(RackJobberChannel.Switch, RackJobberChannel.ReturnsSwitch + "0.2", RackJobberChannel.SharedSwitch) &&
				RackJobberChannel.SharedExhaustion && RackJobberChannel.ReturnRate == .2f, "sensitivity switches configure");
			Check(Rejects(RackJobberChannel.Switch, RackJobberChannel.ScaleSwitch + "0"), "nonpositive scale");
			Check(Rejects(RackJobberChannel.Switch, RackJobberChannel.ScaleSwitch + "x"), "unparsable scale");
			Check(!Rejects(RackJobberChannel.Switch, RackJobberChannel.ScaleSwitch + "0.25") && RackJobberChannel.Enabled &&
				RackJobberChannel.ShopperScale == .25f, "scale configures");
			Check(!Rejects() && !RackJobberChannel.Enabled && RackJobberChannel.ShopperScale == RackJobberChannel.DefaultShopperScale, "default off");
			var sharedData = new RegionalRecordData("r1") { unitsSoldTotal = 100, rackUnitsTotal = 60 };
			RackJobberChannel.SetForChecks(true);
			Check(RackJobberChannel.ExhaustionUnits(sharedData, true) == 60 && RackJobberChannel.ExhaustionUnits(sharedData, false) == 100 &&
				RackJobberChannel.ReturnWriteOff(100f) == 0f, "default retains separate exhaustion and zero write-off");
			RackJobberChannel.SetForChecks(true, shared: true, returns: .2f);
			Check(RackJobberChannel.ExhaustionUnits(sharedData, true) == 160 && RackJobberChannel.ExhaustionUnits(sharedData, false) == 160,
				"shared numerator includes both ledgers");
			Check(Math.Abs(AlbumSimulator.CalculateEffectiveRegionalPenetration(sharedData, 100, 100f, false) - 1.6f) < .0001f,
				"store exhaustion includes prior racks in shared arm");
			Check(Math.Abs(RackJobberChannel.ReturnWriteOff(100f) - 25f) < .0001f, "20 percent of shipments means 25 percent extra copies per sale");
			Check(sharedData.unitsSoldTotal == 100 && sharedData.rackUnitsTotal == 60, "sharing does not mutate accounting ledgers");
			RackJobberChannel.SetForChecks(true, returns: .1f);
			Check(Math.Abs(RackJobberChannel.ReturnWriteOff(90f) - 10f) < .0001f && RackJobberChannel.ReturnWriteOff(0f) == 0f,
				"10 percent write-off and zero sales");

			// Tiers: top N by store units, ties by id; zero sales never racked.
			var tied = new List<RackJobberChannel.Candidate> { Cand("b", 10), Cand("a", 10), Cand("c", 5), Cand("z", 0) };
			RackJobberChannel.AssignTiers(tied, 2, 0);
			Check(tied[1].Tier == RackJobberChannel.TierHit && tied[1].Rank == 1 && tied[0].Rank == 2 && tied[2].Tier == 0 && tied[3].Tier == 0,
				"rank order and ties");
			// Hysteresis: an incumbent ranked inside 1.25 N stays; a newcomer is not admitted past N.
			var hold = Enumerable.Range(0, 6).Select(i => Cand("t" + i, 100 - i, tier: i == 4 ? RackJobberChannel.TierHit : 0)).ToList();
			RackJobberChannel.AssignTiers(hold, 4, 0);
			Check(hold[4].Tier == RackJobberChannel.TierHit && hold.Count(c => c.Tier == RackJobberChannel.TierHit) == 4 &&
				hold[3].Tier == 0 && hold[0].Tier == RackJobberChannel.TierHit, "incumbent kept inside 1.25 N");
			var drop = Enumerable.Range(0, 8).Select(i => Cand("d" + i, 100 - i, tier: i == 7 ? RackJobberChannel.TierHit : 0)).ToList();
			RackJobberChannel.AssignTiers(drop, 4, 0);
			Check(drop[7].Tier == 0 && drop.Take(4).All(c => c.Tier == RackJobberChannel.TierHit), "incumbent outside 1.25 N leaves");
			// Catalogue tier takes only past-peak titles not on the hit tier.
			var cat = new List<RackJobberChannel.Candidate> { Cand("h", 50), Cand("p1", 40, pastPeak: true), Cand("n", 30), Cand("p2", 20, pastPeak: true) };
			RackJobberChannel.AssignTiers(cat, 1, 5);
			Check(cat[0].Tier == RackJobberChannel.TierHit && cat[1].Tier == RackJobberChannel.TierCatalogue && cat[1].Rank == 1 &&
				cat[2].Tier == 0 && cat[3].Tier == RackJobberChannel.TierCatalogue && cat[3].Rank == 2, "catalogue tier is past peak only");
			var again = cat.Select(c => Cand(c.Record.baseRecord.recordId, c.StoreUnits, 0, c.PastPeak)).ToList();
			RackJobberChannel.AssignTiers(again, 1, 5);
			Check(again.Select(c => c.Tier).SequenceEqual(cat.Select(c => c.Tier)), "tiers deterministic");

			// Allocation: exact under demand, capped by shelf, stable by id.
			var alloc = new List<RackJobberChannel.Candidate> { Cand("x", 1), Cand("y", 1), Cand("w", 1) };
			alloc[0].Demand = 10.6f; alloc[1].Demand = 10.6f; alloc[2].Demand = 0f;
			RackJobberChannel.AllocateUnits(alloc, 1000);
			Check(alloc.Sum(c => c.Units) == 21 && alloc[2].Units == 0 && alloc[0].Units == 11 && alloc[1].Units == 10, "floor of demand, ties by id");
			RackJobberChannel.AllocateUnits(alloc, 7);
			Check(alloc.Sum(c => c.Units) == 7, "shelf caps units exactly");
			RackJobberChannel.AllocateUnits(alloc, 0);
			Check(alloc.Sum(c => c.Units) == 0, "no shelf, no units");

			// Shapes.
			Check(RackJobberChannel.Exposure(RackJobberChannel.TierHit, 1, 0f) > RackJobberChannel.Exposure(RackJobberChannel.TierHit, 11, 0f) &&
				RackJobberChannel.Exposure(RackJobberChannel.TierHit, 11, 0f) > RackJobberChannel.Exposure(RackJobberChannel.TierCatalogue, 1, 0f) &&
				RackJobberChannel.Exposure(RackJobberChannel.TierNone, 1, 1f) == 0f, "exposure order");
			Check(RackJobberChannel.Exposure(RackJobberChannel.TierCatalogue, 1, .9f) == .9f, "awareness above the display");
			Check(AlbumSimulator.CalculateAlbumExhaustion(0f) > AlbumSimulator.CalculateAlbumExhaustion(.5f) &&
				AlbumSimulator.CalculateAlbumExhaustion(.5f) >= AlbumSimulator.CalculateAlbumExhaustion(5f), "exhaustion monotone");
			foreach (Genre genre in new[] { Genre.TraditionalPop, Genre.RockAndRoll, Genre.Country, Genre.Jazz }) {
				float weight = RackJobberChannel.GenreWeight(Album("g", "l", 1, genre: genre), 1961);
				Check(weight >= 0f && weight <= 1f, "genre weight bounded " + genre);
			}
			Check(RackJobberChannel.Reach(1962) > RackJobberChannel.Reach(1961) && RackJobberChannel.Reach(1969) == 1f, "reach rises");
			var region = new MarketRegion { regionId = "r1", population = 10f, averageIncome = .81f };
			RackJobberChannel.SetForChecks(true, .1f);
			float poolLow = RackJobberChannel.ShopperPool(region, 1961);
			RackJobberChannel.SetForChecks(true, .2f);
			Check(Math.Abs(RackJobberChannel.ShopperPool(region, 1961) - 2f * poolLow) < 1f && poolLow > 0f, "pool linear in scale");
			Check(RackJobberChannel.Shelf(region, 1962) > RackJobberChannel.Shelf(region, 1961), "shelf grows with reach");

			// Weekly pass. Off: nothing written.
			AILabel Label(string id, params string[] regions) => new() { labelId = id, labelName = id, tier = LabelTier.Major,
				distributionRegions = regions };
			var labels = new Dictionary<string, AILabel> { ["ship"] = Label("ship", "r1"), ["none"] = Label("none") };
			List<RecordRuntimeData> World() => new() {
				Album("s1", "ship", 500), Album("s2", "ship", 300, weeksSincePeak: 3),
				Album("p1", "none", 400, position: 20), Album("u1", "none", 900),
				Album("pl", "ship", 800, player: true),
				new RecordRuntimeData(new Record { recordId = "single", labelId = "ship", format = ReleaseFormat.Single })
			};
			var regions = new List<MarketRegion> { region };
			RackJobberChannel.SetForChecks(false);
			var off = World();
			RackJobberChannel.ClearWeek(off, regions, id => labels.GetValueOrDefault(id), 1, 1961, 6, false);
			Check(off.All(r => r.rackUnitsThisWeek == 0 && r.regionalData.Values.All(d => d.rackUnitsTotal == 0 && d.rackTier == 0)) &&
				RackJobberChannel.RegionRows.Count == 0, "off writes nothing");

			RackJobberChannel.SetForChecks(true, 1f);
			var on = World();
			var storeBefore = on.Select(r => (r.unitsThisWeek, r.totalUnitsSold, r.regionalData.Values.Sum(d => d.unitsSoldThisWeek),
				r.regionalData.Values.Sum(d => d.unitsSoldTotal), r.regionalData.Values.Sum(d => d.unitsInStores))).ToList();
			RackJobberChannel.ClearWeek(on, regions, id => labels.GetValueOrDefault(id), 1, 1961, 6, false);
			RecordRuntimeData Get(string id) => on.First(r => r.baseRecord.recordId == id);
			Check(Get("s1").regionalData["r1"].rackTier == RackJobberChannel.TierHit && Get("s1").rackUnitsThisWeek > 0, "shipped title racked");
			Check(Get("p1").regionalData["r1"].rackTier == RackJobberChannel.TierHit && Get("p1").rackUnitsThisWeek > 0, "proven unshipped title racked");
			Check(Get("u1").regionalData["r1"].rackTier == 0 && Get("u1").rackUnitsThisWeek == 0, "unproven unshipped title never racked");
			Check(Get("pl").rackUnitsThisWeek == 0 && Get("single").rackUnitsThisWeek == 0, "player records and singles excluded");
			Check(on.All(r => r.rackUnitsThisWeek == r.regionalData.Values.Sum(d => d.rackUnitsThisWeek) &&
				r.regionalData.Values.All(d => d.rackUnitsTotal == d.rackUnitsThisWeek)), "record and region units agree");
			Check(on.Select(r => (r.unitsThisWeek, r.totalUnitsSold, r.regionalData.Values.Sum(d => d.unitsSoldThisWeek),
				r.regionalData.Values.Sum(d => d.unitsSoldTotal), r.regionalData.Values.Sum(d => d.unitsInStores))).SequenceEqual(storeBefore),
				"store sales, store exhaustion and stock untouched");
			var row = RackJobberChannel.RegionRows.Single();
			Check(row.Units == on.Sum(r => r.rackUnitsThisWeek) && row.Units <= row.Shelf && row.UnshippedUnits == Get("p1").rackUnitsThisWeek,
				"region row reconciles");
			Check(RackJobberChannel.TitleRows.Sum(t => t.Units) == row.Units, "title rows reconcile");
			// Exhaustion: identical state with a larger ledger sells less.
			int firstWeek = Get("s1").rackUnitsThisWeek;
			Get("s1").regionalData["r1"].rackUnitsTotal = 10_000_000;
			RackJobberChannel.ClearWeek(on, regions, id => labels.GetValueOrDefault(id), 2, 1961, 6, false);
			Check(Get("s1").rackUnitsThisWeek < firstWeek, "rack ledger exhausts demand");
			Check(Get("s2").regionalData["r1"].rackUnitsThisWeek == Get("s2").rackUnitsThisWeek, "week-local units reset");

			// Save: null when untouched, round-trips when set, refuses to resume without the channel.
			var plain = RegionalRecordSaveData.From(new RegionalRecordData("r1") { unitsSoldTotal = 5 });
			Check(plain.rackUnits == null && plain.rackTier == null && !JsonSerializer.Serialize(plain).Contains("rack"), "flag-off save unchanged");
			var saved = RegionalRecordSaveData.From(new RegionalRecordData("r1") { rackUnitsTotal = 1234, rackTier = RackJobberChannel.TierCatalogue });
			var restored = JsonSerializer.Deserialize<RegionalRecordSaveData>(JsonSerializer.Serialize(saved)).ToRegional();
			Check(restored.rackUnitsTotal == 1234 && restored.rackTier == RackJobberChannel.TierCatalogue && restored.rackUnitsThisWeek == 0, "rack ledger round-trips");
			RackJobberChannel.SetForChecks(false);
			bool refused;
			try { saved.ToRegional(); refused = false; } catch (InvalidOperationException) { refused = true; }
			Check(refused, "resume requires the channel");
		}
		finally { RackJobberChannel.ResetForChecks(); }
		Godot.GD.Print($"RACK_JOBBER_CHECKS_OK {checks}");
	}
}
