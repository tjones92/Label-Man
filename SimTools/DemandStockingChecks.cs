using System;
using System.Linq;
using Godot;

/// <summary>Commercial-fit and launch-stocking probes: inert when off, keyed and persistent when on.</summary>
public static class DemandStockingChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Demand stocking: " + why); checks++; }
		var label = new AILabel { labelId = "fit-check", marketingPower = .6f, budgetLevel = .5f };
		RecordRuntimeData Launch(string id, float awareness = SingleCommercialFit.LaunchReference, bool player = false,
			ReleaseFormat format = ReleaseFormat.Single, int year = 1961) =>
			new(new Record { recordId = id, format = format, isPlayerOwned = player, releaseDate = new GameDate(year, 3, 1) }) {
				initialLaunchAwareness = awareness };

		SingleCommercialFit.SetForChecks(0f, 0f, 1f, 1001UL);
		Check(SingleCommercialFit.Multiplier(Launch("gen_1"), "northeast", label) == 1f, "off is exactly one");
		LaunchStocking.SetForChecks(1f, false);
		Check(!LaunchStocking.AppliesToLaunch(Launch("gen_1").baseRecord), "off leaves launch stock alone");
		Check(!LaunchStocking.NeedsReorder(Launch("gen_1"), 500, 100f), "off adds no reorders");

		// Kinked shape, no push or launch weights: at the reference launch the national factor has mean one.
		SingleCommercialFit.SetForChecks(2.5f, 0f, 1f, 1001UL, hitShare: .15f, upside: .15f);
		double[] factors = Enumerable.Range(0, 40000).Select(i => (double)SingleCommercialFit.Multiplier(Launch("gen_" + i), "south", label)).ToArray();
		double mean = factors.Average();
		Array.Sort(factors);
		Check(Math.Abs(mean - 1d) < .03d, $"reference launch keeps mean one ({mean:F3})");
		Check(factors[factors.Length / 2] < .35d, "most launches fail to connect");
		// Connected draws sit at or above 1/M (about 3.5x); just under the kink the downside is still close to it.
		double connected = factors.Count(f => f >= 3d) / (double)factors.Length;
		Check(connected > .12 && connected < .2, $"about the hit share connects ({connected:F3})");
		float first = SingleCommercialFit.Multiplier(Launch("gen_7"), "south", label);
		Check(first == SingleCommercialFit.Multiplier(Launch("gen_7"), "south", label), "a record's response is persistent");
		Check(SingleCommercialFit.Multiplier(Launch("gen_7", player: true), "south", label) == 1f, "player records are untouched");
		Check(SingleCommercialFit.Multiplier(Launch("gen_7", year: 1959), "south", label) == 1f, "opening catalog is untouched");

		SingleCommercialFit.SetForChecks(2.5f, .5f, 1f, 1001UL, hitShare: .15f, upside: .15f);
		Check(Enumerable.Range(0, 50).Any(i => SingleCommercialFit.Multiplier(Launch("gen_" + i), "south", label)
			!= SingleCommercialFit.Multiplier(Launch("gen_" + i), "west", label)), "regional response differs by region");

		SingleCommercialFit.SetForChecks(2.5f, 0f, 1f, 1001UL, hitShare: .15f, upside: .15f, launchWeight: 1.25f, pushWeight: 1.25f);
		double Mean(float awareness) => Enumerable.Range(0, 20000).Average(i => SingleCommercialFit.Multiplier(Launch("gen_" + i, awareness), "south", label));
		Check(Mean(.8f) > Mean(.2f) && Mean(.2f) > Mean(.05f), "a stronger launch expects more");
		SingleCommercialFit.SetForChecks(2.5f, 0f, 1f, 1001UL, hitShare: .15f, upside: .15f, floor: .12f);
		Check(Enumerable.Range(0, 20000).Min(i => SingleCommercialFit.Multiplier(Launch("gen_" + i), "south", label)) >= .1199f,
			"the floor bounds a failed launch");
		SingleCommercialFit.SetForChecks(0f, 0f, 1f, 1001UL);

		LaunchStocking.SetForChecks(.1f, true);
		Check(LaunchStocking.AppliesToLaunch(Launch("gen_1").baseRecord), "AI singles take the first-run share");
		Check(!LaunchStocking.AppliesToLaunch(Launch("gen_1", format: ReleaseFormat.Album).baseRecord), "LPs are outside the probe");
		Check(!LaunchStocking.AppliesToLaunch(Launch("gen_1", player: true).baseRecord), "player records are outside the probe");
		Check(LaunchStocking.ScaleLaunch(0) == 0 && LaunchStocking.ScaleLaunch(100) == LaunchStocking.MinimumShelf
			&& LaunchStocking.ScaleLaunch(20000) == 2000, "regional first runs scale, with a small shelf floor");
		Check(LaunchStocking.NeedsReorder(Launch("gen_1"), 1, 1f), "any live backorder is reordered");
		Check(!LaunchStocking.NeedsReorder(Launch("gen_1"), 0, 50f) && !LaunchStocking.NeedsReorder(Launch("gen_1"), 50, 0f),
			"no reorder without both a shortfall and demand");
		Check(!LaunchStocking.NeedsReorder(Launch("gen_1", format: ReleaseFormat.Album), 50, 50f), "LP restock is unchanged");
		LaunchStocking.SetForChecks(1f, false);

		// Early-game routes for AI founders: inert when off, sellers and borrowers are founder-scale firms only.
		AILabel Firm(string id, LabelTier tier, string owner = null) => new AILabel { labelId = id, tier = tier, ownerLabelId = owner,
			roster = new System.Collections.Generic.List<SimulatedArtist> { new SimulatedArtist { artistId = id + "-act" } } };
		AiMasterDeals.SetEnabledForChecks(false);
		AiBackers.SetEnabledForChecks(false);
		Check(!AiMasterDeals.IsSeller(Firm("s0", LabelTier.Small)) && !AiBackers.Eligible(Firm("s0", LabelTier.Small)), "routes are off by default");
		AiMasterDeals.SetEnabledForChecks(true);
		Check(AiMasterDeals.IsSeller(Firm("s1", LabelTier.Small)) && AiMasterDeals.IsSeller(Firm("i1", LabelTier.Independent)),
			"Small to Independent labels can shop masters");
		Check(!AiMasterDeals.IsSeller(Firm("m1", LabelTier.Major)) && !AiMasterDeals.IsSeller(Firm("s2", LabelTier.Small, "parent")),
			"Majors and subsidiaries do not");
		AiBackers.SetEnabledForChecks(true);
		var founder = Firm("s3", LabelTier.Small);
		Check(!AiBackers.Eligible(Firm("i2", LabelTier.Independent)) && AiBackers.Eligible(founder), "only Small and Boutique founders borrow");
		float capacity = PlayerDesk.BackerCapacity(AiBackers.SourceFor(founder));
		Check(!AiBackers.TryBorrow(founder, capacity + 1f, new GameDate(1961, 3, 1)), "a backer never lends beyond his means");
		Check(AiBackers.TryBorrow(founder, 120f, new GameDate(1961, 3, 1)) && founder.aiBackerLoan.Principal == PlayerDesk.BackerMinimumPrincipal
			&& founder.cashReserves == PlayerDesk.BackerMinimumPrincipal, "the smallest loan is the player's minimum, paid in cash");
		Check(founder.aiBackerLoan.FirstPaymentMonth == 1961 * 12 + 3 + PlayerDesk.BackerGraceFirsts, "first installment on the third 1st");
		Check(!AiBackers.TryBorrow(founder, 50f, new GameDate(1961, 4, 1)), "one agreement per label, ever");
		AiMasterDeals.SetEnabledForChecks(false);
		AiBackers.SetEnabledForChecks(false);

		// Majors' off-chart singles: nothing when off; an exact, reach-weighted pool for deal-free AI Majors only.
		AILabel Major(string id, float reach) { var m = Firm(id, LabelTier.Major); m.nationalReach = reach; return m; }
		var majors = new[] { Major("m-a", 1f), Major("m-b", 1f), Major("m-c", .5f), Firm("i-a", LabelTier.Independent),
			Major("m-deal", 1f), Major("m-player", 1f) };
		majors[4].activeDeal = new DistributionDeal();
		majors[5].isPlayerOwned = true;
		MajorOffChartSingles.SetForChecks(0f);
		Check(MajorOffChartSingles.Allocate(majors, 100000).Count == 0, "the off-chart base is empty when off");
		MajorOffChartSingles.SetForChecks(.15f);
		var offChart = MajorOffChartSingles.Allocate(majors, 100001);
		Check(offChart.Values.Sum() == 15000, "the pool is the share of live single units, rounded once");
		Check(offChart.Keys.OrderBy(k => k).SequenceEqual(new[] { "m-a", "m-b", "m-c" }), "only deal-free AI Majors take it");
		Check(offChart["m-a"] == 6000 && offChart["m-b"] == 6000 && offChart["m-c"] == 3000, "the split follows national reach");
		Check(MajorOffChartSingles.Allocate(majors, 0).Count == 0, "no live market, no catalog week");
		Check(MajorOffChartSingles.HeldSingleBudget(115000) == 115000, "without the hold, the single budget is untouched");
		MajorOffChartSingles.SetForChecks(.15f, holdUnits: true);
		Check(MajorOffChartSingles.HeldSingleBudget(115000) == 100000 && MajorOffChartSingles.HeldSingleBudget(0) == 0,
			"the hold takes the share out of the single budget");
		// The momentum window: the base's own net leaves the Rising test over the same three months.
		var catalogMajor = majors[0];
		MajorOffChartSingles.Record(catalogMajor, 100, 63f, 300f);
		Check(Math.Abs(MajorOffChartSingles.CloseMonthForMomentum(catalogMajor) - 300f) < .01f, "a month's base net enters the window");
		Check(MajorOffChartSingles.CloseMonthForMomentum(catalogMajor) == 150f, "an empty month averages in");
		MajorOffChartSingles.CloseMonthForMomentum(catalogMajor);
		Check(MajorOffChartSingles.CloseMonthForMomentum(catalogMajor) == 0f, "the window holds three months");
		MajorOffChartSingles.SetForChecks(0f, holdUnits: true);
		Check(MajorOffChartSingles.HeldSingleBudget(115000) == 115000, "the hold is inert without the base");
		Check(MajorOffChartSingles.CloseMonthForMomentum(catalogMajor) == 0f, "no base, nothing leaves the Rising test");
		MajorOffChartSingles.SetForChecks(0f);
		GD.Print($"DEMAND_STOCKING_CHECK_PASSED checks={checks}");
	}
}
