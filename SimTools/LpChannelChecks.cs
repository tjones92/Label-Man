using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class LpChannelChecks {
	public static void Run() {
		int count = 0;
		void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException("LP channels: " + name); count++; }
		RecordRuntimeData Album(string id) => new(new Record { recordId = id, labelId = "owner", artistId = "act",
			format = ReleaseFormat.Album, primaryGenre = Genre.TraditionalPop, hookStrength = .8f, productionQuality = .8f });
		RackJobberChannel.Candidate Candidate(string id, int stores, float demand) => new() {
			Record = Album(id), StoreUnits = stores, Demand = demand, AutonomousDemand = demand, Tier = RackJobberChannel.TierHit, Rank = 1 };
		try {
			BudgetLpChannel.ResetForChecks(); LpChannelExperiment.ResetForChecks();
			foreach (string arg in new[] { "--rack-mixed-assortment", "--rack-buyer-access", "--rack-store-choice=0.5", "--rack-store-choice=NaN", "--rack-store-choice=-1", "--rack-store-choice=1.1" }) {
				bool refused = false;
				try { LpChannelExperiment.Configure(new[] { arg }); } catch (ArgumentException) { refused = true; }
				Check(refused, "invalid or orphan treatment " + arg); LpChannelExperiment.ResetForChecks();
			}
			Check(LpChannelExperiment.CatalogueSlots(25) == 25, "baseline retains the original hard assortment split");
			LpChannelExperiment.ResetForChecks(mixed: true);
			Check(LpChannelExperiment.CatalogueSlots(25) == 50 && LpChannelExperiment.CatalogueSlots(0) == 50,
				"wider assortment preserves the 50-title front-line catalogue without adding shelf units");
			Check(LpChannelExperiment.Plan(100, .4f, .5f, .5) == 10 && LpChannelExperiment.Plan(100, 1, 1, 0) == 0,
				"choice uses reach and display, and zero is exactly off");
			Check(LpChannelExperiment.Plan(100, 0, 1, 1) == 0 && LpChannelExperiment.Plan(100, 1, 0, 1) == 0 &&
				LpChannelExperiment.Plan(0, 1, 1, 1) == 0, "no coverage, no display or no store buyers means no route");
			Check(LpChannelExperiment.Plan(100, .4f, .1f, .5, true) == 20 &&
				LpChannelExperiment.Plan(100, .4f, 1, .5, true) == 20,
				"known-buyer access retains reach and choice but is independent of discovery exposure");
			Check(LpChannelExperiment.Plan(100, 0, 1, 1, true) == 0 && LpChannelExperiment.Plan(0, 1, 1, 1, true) == 0 &&
				LpChannelExperiment.Plan(100, 1, 1, 0, true) == 0 && LpChannelExperiment.Plan(100, 1, 0, 1, true) == 100,
				"access cannot create buyers or bypass coverage or the choice coefficient");
			Check(LpChannelExperiment.Fulfilled(374, 374.195573, 100) <= 100 && LpChannelExperiment.Fulfilled(12, 12, 12) == 12 &&
				LpChannelExperiment.Fulfilled(0, 100, 50) == 0, "fulfilled cohorts conserve exact pure routes and never remove unfulfilled buyers");
			LpChannelExperiment.ResetForChecks(mixed: true, share: .5);
			var front = new List<RackJobberChannel.Candidate> { Candidate("a", 1000, 100), Candidate("b", 500, 50) };
			foreach (var candidate in front) LpChannelExperiment.Plan("r", candidate, 1961);
			int planned = front.Sum(c => c.StoreChoice);
			Check(planned > 0 && planned <= 1500 && front.All(c => c.StoreChoice <= c.StoreUnits), "front-line cohort is bounded by actual title buyers");
			var budget = new List<BudgetLpChannel.Offer> { new() { Product = new() { Id = "budget", Genre = Genre.TraditionalPop },
				Demand = 300, SwitchDemand = 150, StoreLimit = 1500 } };
			LpChannelExperiment.ReserveBudgetBuyers("r", budget, front);
			Check(budget[0].StoreLimit == 1500 - planned && Math.Abs((budget[0].Demand - budget[0].SwitchDemand) - 150) < 1e-10,
				"budget uses the remaining cohort and retains its original incremental buyers");
			foreach (var candidate in front) candidate.Demand += candidate.StoreChoice;
			BudgetLpChannel.AllocateShared(front, budget, 200);
		LpChannelExperiment.CompleteFront("r", front);
			int routed = front.Sum(c => LpChannelExperiment.FrontSwitches("r", c.Record.baseRecord.recordId));
			Check(front.Sum(c => c.Units) + budget.Sum(o => o.Units) == 200 && routed <= front.Sum(c => c.Units),
				"both products and routed buyers consume the one shared shelf");
			Check(front.Sum(c => LpChannelExperiment.BudgetEligible("r", c.Record.baseRecord.recordId,
				c.StoreUnits - LpChannelExperiment.FrontSwitches("r", c.Record.baseRecord.recordId))) == 1500 - planned,
				"budget excludes front-line preferences even when those preferences cannot all clear");
			LpChannelExperiment.Record(1, 1961, "r", 1500, 0, 1500 - routed);
			Check(LpChannelExperiment.Rows.Single().FrontSwitched == routed, "regional conservation trace reconciles");
			foreach (var candidate in front) candidate.Units = 0;
			LpChannelExperiment.CompleteFront("r", front);
			Check(front.All(c => LpChannelExperiment.FrontSwitches("r", c.Record.baseRecord.recordId) == 0), "empty shelf leaves all routed preferences in stores");
			LpChannelExperiment.BeginWeek();
			var tiny = Enumerable.Range(0, 50).Select(i => Candidate("tiny" + i, 1000, 0)).ToList();
			foreach (var candidate in tiny) { LpChannelExperiment.Plan("tiny", candidate, 1961); candidate.AutonomousDemand = .001f; candidate.Units = candidate.StoreChoice; }
			LpChannelExperiment.CompleteFront("tiny", tiny);
			Check(tiny.Sum(c => c.Units - LpChannelExperiment.FrontSwitches("tiny", c.Record.baseRecord.recordId)) == 1,
				"regional cohort rounding avoids inventing one autonomous buyer per title");
			foreach (var candidate in tiny) candidate.AutonomousDemand = 0;
			LpChannelExperiment.CompleteFront("tiny", tiny);
			Check(tiny.All(c => c.Units == LpChannelExperiment.FrontSwitches("tiny", c.Record.baseRecord.recordId)),
				"zero autonomous front-line demand preserves every pure-switch copy");
			BudgetLpChannel.ResetForChecks(true);
			BudgetLpChannel.Prepare(new[] { Album("save") }, _ => new AILabel { labelId = "owner", tier = LabelTier.Major }, 1);
			var state = BudgetLpChannel.Capture();
			Check(state.MixedAssortment && state.RackChoice == .5 && SaveGameService.WritingVersion == 33, "treatment settings persist in v33");
			string saved = JsonSerializer.Serialize(state);
			BudgetLpChannel.Restore(JsonSerializer.Deserialize<BudgetLpState>(saved));
			Check(saved == JsonSerializer.Serialize(BudgetLpChannel.Capture()), "treatment state round-trips canonically");
			LpChannelExperiment.ResetForChecks();
			bool mismatch = false;
			try { BudgetLpChannel.Restore(state); } catch (InvalidOperationException) { mismatch = true; }
			Check(mismatch, "resume rejects changed treatment settings");
			LpChannelExperiment.ResetForChecks(mixed: true, share: .5, access: true);
			BudgetLpChannel.ResetForChecks(true);
			BudgetLpChannel.Prepare(new[] { Album("access-save") }, _ => new AILabel { labelId = "owner", tier = LabelTier.Major }, 1);
			var accessState = BudgetLpChannel.Capture();
			Check(accessState.RackBuyerAccess && SaveGameService.WritingVersion == 34, "buyer access persists in v34");
			string accessSaved = JsonSerializer.Serialize(accessState);
			BudgetLpChannel.Restore(JsonSerializer.Deserialize<BudgetLpState>(accessSaved));
			Check(accessSaved == JsonSerializer.Serialize(BudgetLpChannel.Capture()), "buyer-access state round-trips canonically");
			bool accessMismatch = false;
			try { BudgetLpChannel.Restore(state); } catch (InvalidOperationException) { accessMismatch = true; }
			Check(accessMismatch, "old display-choice saves cannot resume silently with buyer access");
			var accessFront = new List<RackJobberChannel.Candidate> { Candidate("access", 1000, 0) };
			accessFront[0].Tier = RackJobberChannel.TierCatalogue;
			LpChannelExperiment.BeginWeek();
			LpChannelExperiment.Plan("access", accessFront[0], 1961);
			Check(accessFront[0].StoreChoice == LpChannelExperiment.Plan(1000, RackJobberChannel.Reach(1961), 1, .5),
				"stocked catalogue titles use access without their display penalty");
			accessFront[0].Demand = accessFront[0].StoreChoice;
			BudgetLpChannel.AllocateShared(accessFront, new List<BudgetLpChannel.Offer>(), 10);
			LpChannelExperiment.CompleteFront("access", accessFront);
			LpChannelExperiment.Record(1, 1961, "access", 1000, 0, 990);
			Check(LpChannelExperiment.FrontSwitches("access", "access") == 10,
				"access removes only fulfilled buyers when shelf capacity binds");
			var unavailable = Candidate("unavailable", 1000, 0); unavailable.Tier = RackJobberChannel.TierNone;
			LpChannelExperiment.Plan("access", unavailable, 1961);
			Check(unavailable.StoreChoice == 0, "buyer access cannot route an unstocked title");
			LpChannelExperiment.ResetForChecks(mixed: true, share: .5);
			accessMismatch = false;
			try { BudgetLpChannel.Restore(accessState); } catch (InvalidOperationException) { accessMismatch = true; }
			Check(accessMismatch, "access saves require buyer access on resume");
			LpChannelExperiment.ResetForChecks(share: .5); RackJobberChannel.SetForChecks(true);
			var depletion = new RegionalRecordData("r") { unitsSoldTotal = 80, rackUnitsTotal = 60, rackStoreBuyerUnitsTotal = 20 };
			Check(RackJobberChannel.ExhaustionUnits(depletion, false) == 100 && RackJobberChannel.ExhaustionUnits(depletion, true) == 40,
				"routed buyers retain store-origin depletion and do not consume the autonomous rack pool");
			Check(Math.Abs(AlbumSimulator.CalculateEffectiveRegionalPenetration(depletion, 80, 100, false) - 1) < 1e-7,
				"original store audience is unchanged when its twenty purchases move to racks");
			var restored = JsonSerializer.Deserialize<RegionalRecordSaveData>(JsonSerializer.Serialize(RegionalRecordSaveData.From(depletion))).ToRegional();
			Check(restored.rackStoreBuyerUnitsTotal == 20 && restored.rackUnitsTotal == 60 && restored.unitsSoldTotal == 80,
				"saved routed purchase history restores without changing channel accounting");
			Check(!JsonSerializer.Serialize(RegionalRecordSaveData.From(new RegionalRecordData("plain"))).Contains("rackStoreBuyers"),
				"ordinary regional saves omit experimental purchase history");
			LpChannelExperiment.ResetForChecks(); bool historyRefused = false;
			try { RegionalRecordSaveData.From(depletion).ToRegional(); } catch (InvalidOperationException) { historyRefused = true; }
			Check(historyRefused, "routed history requires its channel-choice setting on resume");
			LpChannelExperiment.ResetForChecks(share: .5);
			RackJobberChannel.SetForChecks(true, shared: true);
			Check(RackJobberChannel.ExhaustionUnits(depletion, true) == 140 &&
				Math.Abs(AlbumSimulator.CalculateEffectiveRegionalPenetration(depletion, 80, 100, false) - 1.4) < 1e-6,
				"shared exhaustion counts each routed purchase once");
			var old = new BudgetLpState();
			Check(!JsonSerializer.Serialize(old).Contains("RackChoice") && !JsonSerializer.Serialize(old).Contains("MixedAssortment") &&
				!JsonSerializer.Serialize(old).Contains("RackBuyerAccess"), "off settings leave the old v32 JSON unchanged");
		} finally { BudgetLpChannel.ResetForChecks(); LpChannelExperiment.ResetForChecks(); RackJobberChannel.ResetForChecks(); }
		Godot.GD.Print($"LP_CHANNEL_CHECKS_OK {count}");
	}
}
