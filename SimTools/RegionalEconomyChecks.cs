using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Supply conservation and regional arrival boundaries, independent of a seeded world's outcomes.</summary>
public static class RegionalEconomyChecks {
	public static void Run() {
		bool clock = RegionalAudience.LocalLifecycleClock, radio = RegionalAudience.LocalRadioFatigue,
			home = RegionalAudience.HometownAudience, demand = RegionalAudience.LocalSignalsReadDemand,
			arrival = RegionalAudience.RegionalArrival, retail = RegionalAudience.RegionalRetail,
			focus = RegionalAudience.RegionalCampaignFocus, buildClock = RegionalAudience.RegionalRadioBuildClock;
		try {
			RegionalAudience.ConfigureForProbe(true, signalsReadDemand: true, arrival: true, retail: true);
			var label = new AILabel { labelId = "regional-check", distributionRegions = new[] { "greatlakes" },
				strongRegions = Array.Empty<string>() };
			RecordRuntimeData record = Release();
			int initialStock = record.regionalData.Values.Sum(d => d.unitsInStores);
			RegionalAudience.HoldUntilArrival(record, label, null);
			RegionalRecordData local = record.regionalData["greatlakes"], remote = record.regionalData["westcoast"];
			Require(!local.awaitingArrival && remote.awaitingArrival, "retail stock must not grant regional servicing");
			Require(record.regionalData.Values.Sum(d => d.unitsInStores + d.pendingLaunchStock) == initialStock,
				"staging must conserve every physical copy");
			Require(remote.unitsInStores == 250 && remote.pendingLaunchStock == 0 && remote.awareness == 0f && remote.radioPlay == 0f,
				"a finite shop allocation must be available without a promotion or radio seed");
			remote.unitsInStores -= 90; remote.unitsSoldTotal += 90;
			remote.unitsBackordered = 1_000_000;
			RegionalAudience.TryArrive(record, remote);
			Require(remote.awaitingArrival, "backorders alone must never grant arrival, regardless of volume");
			string saved = JsonSerializer.Serialize(remote, SaveGameService.TestJsonOptions);
			remote = JsonSerializer.Deserialize<RegionalRecordData>(saved, SaveGameService.TestJsonOptions);
			record.regionalData["westcoast"] = remote;
			Require(remote.awaitingArrival && remote.unitsInStores == 160 && remote.pendingLaunchStock == 0,
				"saving must preserve the remaining imported stock and held promotion separately");
			record.weeksSinceRelease = 9;
			remote.neighboringMarketTestStrength = RegionalAudience.ArrivalTestMarketStrength;
			RegionalAudience.TryArrive(record, remote);
			Require(!remote.awaitingArrival && remote.arrivalAge == 9 && remote.LocalAge(9) == 1,
				"regional proof must start a local lifecycle at the actual arrival");
			Require(remote.unitsInStores == 160 && remote.unitsSoldTotal == 90 && remote.awareness > 0f && remote.radioPlay > 0f,
				"arrival must release promotion without recreating copies already sold");
			RegionalAudience.TryArrive(record, remote);
			Require(remote.unitsInStores == 160 && remote.pendingLaunchAwareness == 0f,
				"repeat arrival processing must not duplicate stock or promotion");

			RegionalAudience.ConfigureForProbe(true, arrival: true);
			record = Release();
			RegionalAudience.HoldUntilArrival(record, label, null);
			remote = record.regionalData["westcoast"];
			Require(remote.unitsInStores == 0 && remote.pendingLaunchStock == 250,
				"the retail switch off must retain the held-stock control");
			record.currentPosition = RegionalAudience.ArrivalChartDepth;
			RegionalAudience.TryArrive(record, remote);
			Require(!remote.awaitingArrival && remote.unitsInStores == 250, "national chart proof must still deliver held stock");

			RegionalAudience.ConfigureForProbe(true, arrival: true, retail: true);
			foreach (bool player in new[] { false, true }) {
				record = Release();
				record.baseRecord.isPlayerOwned = player;
				record.baseRecord.format = player ? ReleaseFormat.Single : ReleaseFormat.Album;
				RegionalAudience.HoldUntilArrival(record, label, null);
				Require(record.regionalData.Values.All(d => !d.awaitingArrival && d.unitsInStores == 250 && d.awareness == .1f),
					"player records and albums must retain their existing launch path");
			}
			ProbeCampaignConservation();
			ProbeEarnedDistributionArrival();
			RegionalAudience.ConfigureForProbe(true, arrival: true, radioBuildClock: true);
			Require(RegionalAudience.RadioBuildAge(10, new RegionalRecordData("westcoast") { arrivalAge = 9 }) == 2,
				"a late-serviced market's radio build must use its own arrival age");
			Require(RegionalAudience.RadioBuildAge(10, new RegionalRecordData("greatlakes") { arrivalAge = 1 }) == 10 &&
				RegionalAudience.RadioBuildAge(10, new RegionalRecordData("greatlakes")) == 10,
				"national launch and prewarm records must retain their radio build ages");
			GD.Print("REGIONAL_ECONOMY_CHECK_PASS stock conservation, no backlog arrival, local clocks, save/load, no duplicate seed, control and format boundaries");
		} finally {
			RegionalAudience.ConfigureForProbe(clock, radio, home, demand, arrival, retail, focus, buildClock);
		}
	}

	private static void ProbeEarnedDistributionArrival() {
		RegionalAudience.ConfigureForProbe(true, arrival: true, retail: true);
		var label = new AILabel { distributionRegions = new[] { "greatlakes" }, strongRegions = Array.Empty<string>() };
		RecordRuntimeData record = Release();
		RegionalAudience.HoldUntilArrival(record, label, null);
		var remote = record.regionalData["westcoast"];
		remote.unitsBackordered = 1_000_000;
		RegionalAudience.TryArrive(record, remote, label);
		Require(remote.awaitingArrival, "backorders must not manufacture distribution access");
		label.independentDistributionRegions.Add("westcoast");
		record.weeksSinceRelease = 5;
		RegionalAudience.TryArrive(record, remote, label);
		Require(!remote.awaitingArrival && remote.arrivalAge == 5 && remote.unitsInStores == 250,
			"an earned wholesale line must service a waiting record without duplicating imported stock");

		label.independentDistributionRegions.Clear();
		label.activeDeal = new DistributionDeal { grantedRegions = new[] { "westcoast" } };
		record = Release();
		RegionalAudience.HoldUntilArrival(record, label, null);
		remote = record.regionalData["westcoast"];
		label.activeDeal.Cover("some-other-record");
		RegionalAudience.TryArrive(record, remote, label);
		Require(remote.awaitingArrival, "a distribution contract must not carry an excluded catalog record");
		label.activeDeal.Cover(record.baseRecord.recordId);
		RegionalAudience.TryArrive(record, remote, label);
		Require(!remote.awaitingArrival, "a newly covered release must use its contracted regional access");

		record = Release();
		label.activeDeal = null;
		RegionalAudience.HoldUntilArrival(record, label, null);
		remote = record.regionalData["westcoast"];
		label.distributionRegions = new[] { "greatlakes", "westcoast" };
		remote.awareness = .2f; remote.radioPlay = .1f; // the caller's replacement promotion seed
		RegionalAudience.HoldUntilArrival(record, label, null);
		Require(!remote.awaitingArrival && remote.pendingLaunchAwareness == 0f && remote.pendingLaunchRadio == 0f && remote.pendingLaunchStock == 0,
			"reseeded promotion must not retain a stale held state after gaining access");
	}

	private static void ProbeCampaignConservation() {
		var regions = new[] {
			new MarketRegion { regionId = "greatlakes", population = 20, youthPercentage = .2f, averageIncome = 3000 },
			new MarketRegion { regionId = "westcoast", population = 10, youthPercentage = .2f, averageIncome = 3000 }
		};
		RecordRuntimeData record = Release();
		record.awareness = .2f;
		float baseline = regions.Sum(r => r.GetRecordBuyingPopulation() * (.4f * record.awareness + .6f * record.regionalData[r.regionId].awareness));
		RegionalRecordData remote = record.regionalData["westcoast"];
		remote.awaitingArrival = true; remote.pendingLaunchAwareness = remote.awareness; remote.awareness = 0f;
		RegionalAudience.ConcentrateLaunchAudience(record, regions);
		RegionalAudience.ConfigureForProbe(true, arrival: true, campaignFocus: true);
		RegionalAudience.PrepareCampaignAudience(record, regions);
		float focused = regions.Sum(r => r.GetRecordBuyingPopulation() * (r.regionId == "westcoast"
			? .4f * record.awareness * RegionalAudience.HearsayShare
			: .4f * record.awareness + .6f * record.regionalData[r.regionId].awareness + record.regionalData[r.regionId].campaignAwarenessThisWeek));
		Require(Math.Abs(focused - baseline) < 1f, "local campaign allocation must conserve the population-weighted launch audience");
		Require(record.regionalData["greatlakes"].awareness > .1f && remote.awareness == 0f && remote.pendingLaunchAwareness == 0f,
			"only served markets receive the redirected campaign; the cancelled deposit must not arrive twice");
		float localCampaign = record.regionalData["greatlakes"].campaignAwarenessThisWeek;
		RegionalAudience.PrepareCampaignAudience(record, regions);
		Require(record.regionalData["greatlakes"].campaignAwarenessThisWeek == localCampaign,
			"repeated demand preparation must not accumulate a second campaign budget");
		remote.awaitingArrival = false;
		RegionalAudience.PrepareCampaignAudience(record, regions);
		Require(record.regionalData.Values.All(d => d.campaignAwarenessThisWeek == 0f),
			"a completed rollout must stop concentrating the national budget");
		record = Release();
		RegionalAudience.ConcentrateLaunchAudience(record, regions);
		Require(record.regionalData.Values.All(d => d.awareness == .1f), "nationwide campaigns must remain unchanged");
		record.regionalData["greatlakes"].awareness = .99f;
		remote = record.regionalData["westcoast"];
		remote.awaitingArrival = true; remote.awareness = 0f; remote.pendingLaunchAwareness = 1f;
		RegionalAudience.ConcentrateLaunchAudience(record, regions);
		Require(record.regionalData["greatlakes"].awareness <= 1f, "local campaign focus must respect the finite audience");
		foreach (var region in regions) region.Dispose();
	}

	private static RecordRuntimeData Release() => new(new Record { recordId = "regional-check-single" }) {
		weeksSinceRelease = 1,
		regionalData = new Dictionary<string, RegionalRecordData> {
			["greatlakes"] = new("greatlakes") { unitsInStores = 250, awareness = .1f, radioPlay = .2f },
			["westcoast"] = new("westcoast") { unitsInStores = 250, awareness = .1f, radioPlay = .2f }
		}
	};
	private static void Require(bool condition, string message) {
		if (!condition) throw new InvalidOperationException("Regional economy: " + message);
	}
}
