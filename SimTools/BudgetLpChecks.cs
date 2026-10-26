using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class BudgetLpChecks {
	public static void Run() {
		int count = 0;
		void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException("Budget LP: " + name); count++; }
		bool Refuses(Action action) { try { action(); return false; } catch (InvalidOperationException) { return true; } }
		var label = new AILabel { labelId = "owner", tier = LabelTier.Major };
		var buyer = new AILabel { labelId = "buyer", tier = LabelTier.Major };
		AILabel Owner(string id) => id == label.labelId ? label : id == buyer.labelId ? buyer : null;
		RecordRuntimeData Album(string id, int age = 52) => new(new Record { recordId = id, labelId = "owner", artistId = "act",
			format = ReleaseFormat.Album, primaryGenre = Genre.TraditionalPop, hookStrength = .8f, productionQuality = .8f }) {
			weeksSinceRelease = age, totalUnitsSold = 1000, awareness = .4f };
		try {
			BudgetLpChannel.ResetForChecks();
			Check(BudgetLpChannel.Capture() == null, "off saves omit the ledger");
			foreach (string value in new[] { "NaN", "-1", "1.1", "x" }) {
				bool rejects = false;
				try { BudgetLpChannel.Configure(new[] { "--budget-substitution=" + value }); } catch (ArgumentException) { rejects = true; }
				Check(rejects, "invalid or orphan scenario " + value);
			}
			BudgetLpChannel.ResetForChecks(true);
			Check(BudgetLpChannel.HasMargin(.51) && !BudgetLpChannel.HasMargin(2), "offer gate includes manufacturing, sleeve, mechanicals and artist royalty");
			var record = Album("r", 20);
			BudgetLpChannel.Remember(record, 10);
			var product = BudgetLpChannel.State.Products.Values.Single();
			Check(product.ReleaseWeek == 42 && product.SourceId == "r" && product.Provenance == "OwnMaster", "retirement retains source and remaining holdback");
			BudgetLpChannel.Remember(record, 11);
			Check(BudgetLpChannel.State.Products.Count == 1 && product.ReleaseWeek == 42, "migration never reissues a product");
			var excluded = Album("player"); excluded.baseRecord.isPlayerOwned = true; BudgetLpChannel.Remember(excluded, 12);
			excluded = Album("single"); excluded.baseRecord.format = ReleaseFormat.Single; BudgetLpChannel.Remember(excluded, 12);
			excluded = Album("zero"); excluded.totalUnitsSold = 0; BudgetLpChannel.Remember(excluded, 12);
			Check(BudgetLpChannel.State.Products.Count == 1, "player, singles and unsold masters excluded");
			var region = new MarketRegion { regionId = "region", population = 1, averageIncome = 1 };
			var stores = new Dictionary<Genre, int> { [Genre.TraditionalPop] = 10000 };
			Check(BudgetLpChannel.Offers(region, 41, 1961, 6, false, Owner, stores).Count == 0, "holdback excludes week before release");
			var offers = BudgetLpChannel.Offers(region, 42, 1961, 6, false, Owner, stores);
			Check(offers.Count == 1 && offers[0].SwitchDemand > 0 && offers[0].Demand >= offers[0].SwitchDemand, "own product enters with separate new and switching buyers");
			double firstDemand = offers[0].Demand;
			var front = new List<RackJobberChannel.Candidate> { new() { Record = Album("front"), Demand = 10000 } };
			BudgetLpChannel.AllocateShared(front, offers, 100);
			BudgetLpChannel.Complete(region, 42, 1961, 100, front.Sum(c => c.Units));
			Check(front.Sum(c => c.Units) + offers.Sum(o => o.Units) == 100, "one shelf caps both lines together");
			Check(offers.All(o => o.Switched <= o.Units && o.Switched <= o.SwitchDemand), "only sold copies transfer buyers");
			Check(record.totalUnitsSold == 1000 && record.rackUnitsThisWeek == 0 && record.unitsThisWeek == 0, "budget leaves source accounting and chart units untouched");
			Check(product.RegionUnits[region.regionId] == offers.Sum(o => o.Units), "separate purchase depletion advances");
			Check(BudgetLpChannel.SourcePurchases("r", region.regionId) == offers.Sum(o => o.Units) &&
				BudgetLpChannel.SourcePurchases("unrelated", region.regionId) == 0, "only the identifiable master's full-price buyers share its depletion");
			product.RegionUnits[region.regionId] = 100000000;
			Check(BudgetLpChannel.Offers(region, 43, 1961, 6, false, Owner, stores)[0].Demand < firstDemand, "budget purchases exhaust future demand");
			var noShelf = BudgetLpChannel.Offers(region, 43, 1961, 6, false, Owner, stores);
			BudgetLpChannel.AllocateShared(front, noShelf, 0); BudgetLpChannel.Complete(region, 43, 1961, 0, 0);
			Check(noShelf.All(o => o.Units == 0 && o.Switched == 0) && front.All(c => c.Units == 0), "no shelf means no sales or substitutions");
			BudgetLpChannel.OnAcquired("owner", "buyer");
			Check(product.OwnerId == "buyer" && product.SourceId == "r" && product.RegionUnits[region.regionId] == 100000000, "acquisition preserves provenance, holdback and exhaustion");
			string saved = JsonSerializer.Serialize(BudgetLpChannel.Capture());
			BudgetLpChannel.Restore(JsonSerializer.Deserialize<BudgetLpState>(saved));
			Check(JsonSerializer.Serialize(BudgetLpChannel.Capture()) == saved, "canonical saved product state round-trips");
			var state = BudgetLpChannel.Capture(); state.Products.Values.First().RegionUnits.Clear();
			Check(BudgetLpChannel.State.Products.Values.First().RegionUnits.Count == 1, "capture has no mutable aliases");
			state = BudgetLpChannel.Capture(); state.Substitution = .9;
			Check(Refuses(() => BudgetLpChannel.Restore(state)), "resume refuses different choice parameters");
			BudgetLpChannel.ResetForChecks();
			Check(Refuses(() => BudgetLpChannel.Restore(JsonSerializer.Deserialize<BudgetLpState>(saved))), "resume requires budget switch");
			BudgetLpChannel.ResetForChecks(true);
			BudgetLpChannel.Prepare(new[] { Album("live", 51), Album("aged", 52) }, Owner, 50);
			Check(BudgetLpChannel.State.Products.Count == 2 && BudgetLpChannel.State.Products.Values.Count(p => p.Provenance == "OwnMaster") == 1,
				"opening basket and exactly aged live source remain explicit");
			var opening = BudgetLpChannel.State.Products.Values.Single(p => p.Provenance == "OpeningSyntheticBasket");
			Check(opening.SourceId == "" && opening.ArtistId == "", "synthetic basket invents no master or artist");
			BudgetLpChannel.Prepare(new[] { Album("live", 51), Album("aged", 52) }, Owner, 51);
			Check(BudgetLpChannel.State.Products.Count == 2, "opening initialization and live migration idempotent");
			// Integer allocation is order-independent and under-demand sales do not fill a spare shelf.
			var a = new BudgetLpChannel.Offer { Product = new() { Id = "a" }, Demand = 10.6 };
			var b = new BudgetLpChannel.Offer { Product = new() { Id = "b" }, Demand = 10.6 };
			BudgetLpChannel.AllocateShared(new(), new() { b, a }, 100);
			Check(a.Units == 11 && b.Units == 10, "largest remainders floor total demand and break ties by product id");
			BudgetLpChannel.AllocateShared(new(), new() { a, b }, 7);
			Check(a.Units == 4 && b.Units == 3, "stable constrained allocation");
			a.Switched = 4; b.Switched = 3;
			BudgetLpChannel.BoundSwitchers(new() { b, a }, 7);
			Check(a.Switched == 4 && b.Switched == 3, "nonbinding buyer bound preserves the calibrated allocation");
			BudgetLpChannel.BoundSwitchers(new() { b, a }, 5);
			Check(a.Switched + b.Switched == 5 && a.Units + b.Units == 5 && a.Switched <= 4 && b.Switched <= 3,
				"rounded high-overlap cohorts cannot exceed actual store buyers");
			BudgetLpChannel.BoundSwitchers(new() { a, b }, 0);
			Check(a.Switched == 0 && b.Switched == 0 && a.Units == 0 && b.Units == 0,
				"zero available store buyers removes unfulfilled copies instead of inventing new buyers");
			Check(BudgetLpChannel.FulfilledSwitchers(new() { Demand = 12.7, SwitchDemand = 12.7, Units = 12 }) == 12,
				"pure-switch cohorts preserve every fulfilled buyer despite floating-point cancellation");
			BudgetLpChannel.ResetForChecks(true, scale: 0, substitution: 1);
			BudgetLpChannel.Prepare(new[] { Album("live") }, Owner, 50);
			var onlySwitchers = BudgetLpChannel.Offers(region, 50, 1961, 6, false, Owner, stores);
			BudgetLpChannel.AllocateShared(new(), onlySwitchers, 10000);
			BudgetLpChannel.Complete(region, 50, 1961, 10000, 0);
			Check(onlySwitchers.Sum(o => o.Units) > 0 && onlySwitchers.All(o => o.Units == o.Switched),
				"zero-incremental-buyer integration conserves every budget copy");
			BudgetLpChannel.ResetForChecks(true, scale: 0, substitution: 0);
			BudgetLpChannel.Prepare(new[] { Album("live") }, Owner, 50);
			Check(BudgetLpChannel.Offers(region, 50, 1961, 6, false, Owner, stores).All(o => o.Demand == 0), "zero buyer sources create no sales");
		} finally { BudgetLpChannel.ResetForChecks(); }
		Godot.GD.Print($"BUDGET_LP_CHECKS_OK {count}");
	}
}
