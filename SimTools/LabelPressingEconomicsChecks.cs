using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Fixed checks for LabelPressingEconomics (SaveLoadRoundTripRunner --label-pressing-check).</summary>
public static class LabelPressingEconomicsChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Label pressing economics: " + why); }
		LabelPressingEconomics.SetPeriodSingleCostsForChecks(false);
		LabelPressingEconomics.SetPeriodAlbumCostsForChecks(false);
		LabelPressingEconomics.SetEnabledForChecks(false);
		Check(LabelPressingEconomics.InitialRunReserve(new AILabel(), ReleaseFormat.Single, 0.30f) == 0f,
			"switched off, a release reserves nothing for pressing");
		LabelPressingEconomics.SetEnabledForChecks(true);
		// Live releases by default: dated after the run's first day, so only `catalog` below is opening stock.
		RecordRuntimeData Fresh(ReleaseFormat format, int age = 1, string id = null, bool openingCatalog = false) =>
			new(new Record { recordId = id ?? "check-" + format, format = format,
				releaseDate = openingCatalog ? new GameDate(1959, 11, 7) : new GameDate(1961, 3, 4) }) { weeksSinceRelease = age };

		Check(Mathf.IsEqualApprox(LabelPressingEconomics.LabelPrice(ReleaseFormat.Single, 0.89f), PlayerDesk.DealerPrice),
			"an AI single books the dealer price the player's shop sales book");
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.LabelPrice(ReleaseFormat.Album, 3.98f), 3.98f * 0.63f / 0.98f),
			"an LP books the same dealer share of list");

		var label = new AILabel { labelId = "check-label", tier = LabelTier.Small };
		var flop = Fresh(ReleaseFormat.Single);
		var week = LabelPressingEconomics.Press(flop, label, 40);
		Check(week.InitialRun == LabelPressingEconomics.MinimumRun && week.Repressed == 0 && flop.unitsPressed == 500,
			"a label with no history presses the plant minimum and nothing more for a slow first week");
		LabelPressingEconomics.Press(flop, label, 60);
		Check(flop.unitsPressed == 500 && flop.pressingBookSold == 100, "stock already pressed covers later sales");
		LabelPressingEconomics.Learn(label, flop);
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.ForecastUnits(label, ReleaseFormat.Single), 100f, 0.01f),
			"the first retirement sets the forecast");

		var hit = Fresh(ReleaseFormat.Single);
		LabelPressingEconomics.Press(hit, label, 400);
		week = LabelPressingEconomics.Press(hit, label, 2000);
		Check(week.InitialRun == 0 && week.Repressed == 6000 && hit.unitsPressed == 6500 && hit.unitsPressed >= hit.pressingBookSold,
			"a record outselling its run is re-pressed in lots of three weeks' current sales");
		LabelPressingEconomics.Learn(label, hit);
		float blended = MathF.Exp(MathF.Log(101f) + 0.3f * (MathF.Log(2401f) - MathF.Log(101f))) - 1f;
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.ForecastUnits(label, ReleaseFormat.Single), blended, 0.5f),
			"the forecast is a geometric memory, so one hit moves it only partway");
		Check(LabelPressingEconomics.ForecastUnits(label, ReleaseFormat.Album) == 0f, "formats keep separate forecasts");

		label.pressingForecastSingleLogUnits = MathF.Log(1f + 8000f);
		var planned = Fresh(ReleaseFormat.Single);
		week = LabelPressingEconomics.Press(planned, label, 300);
		Check(week.InitialRun == 4000 && planned.unitsPressed == 4000, "the initial run is half the label's own forecast");

		Check(Mathf.IsEqualApprox(LabelPressingEconomics.InitialRunReserve(label, ReleaseFormat.Single, 0.30f), 1200f) &&
			Mathf.IsEqualApprox(LabelPressingEconomics.InitialRunReserve(new AILabel(), ReleaseFormat.Album, 1f), 500f) &&
			LabelPressingEconomics.InitialRunReserve(new AILabel { isPlayerOwned = true }, ReleaseFormat.Single, 0.30f) == 0f,
			"a release decision reserves its expected initial run (plant minimum without history; never the player)");

		var lateSeller = Fresh(ReleaseFormat.Single, age: 30);
		week = LabelPressingEconomics.Press(lateSeller, label, 120);
		Check(week.InitialRun == 0 && week.Repressed == 500, "a live record first selling late pays no fresh initial run, only what it now sells");

		int carriedFree = 0, phases = 0;
		var seenStock = new System.Collections.Generic.HashSet<int>();
		for (int i = 0; i < 40; i++) {
			var catalog = Fresh(ReleaseFormat.Single, age: 30, id: "catalog-" + i, openingCatalog: true);
			week = LabelPressingEconomics.Press(catalog, label, 400);
			if (week.InitialRun == 0 && week.Repressed == 0 && catalog.unitsPressed >= 400 && catalog.unitsPressed < 400 + 1200)
				carriedFree++;
			if (seenStock.Add(catalog.unitsPressed)) phases++;
			var again = Fresh(ReleaseFormat.Single, age: 30, id: "catalog-" + i, openingCatalog: true);
			LabelPressingEconomics.Press(again, label, 400);
			Check(again.unitsPressed == catalog.unitsPressed, "a catalog record's carried stock is stable across runs");
		}
		Check(carriedFree == 40, "opening catalog was pressed before 1960: its first week bills nothing, and it carries under one lot");
		Check(phases >= 30, "catalog records stand at different points of the re-press cycle, so they do not re-press together");

		var player = new AILabel { labelId = "player", isPlayerOwned = true };
		LabelPressingEconomics.Learn(player, hit);
		Check(player.pressingForecastSingleLogUnits == 0f, "the player's pressing stays with PlayerDesk");

		var restoredLabel = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(label, SaveGameService.TestJsonOptions),
			SaveGameService.TestJsonOptions);
		Check(restoredLabel.pressingForecastSingleLogUnits == label.pressingForecastSingleLogUnits,
			"the label's forecast survives saves");
		var restoredRecord = RuntimeRecordSaveData.From(hit).ToRuntime();
		Check(restoredRecord.pressingBookOpened && restoredRecord.unitsPressed == hit.unitsPressed &&
			restoredRecord.pressingBookSold == hit.pressingBookSold, "the pressing book survives saves");
		var oldLabel = JsonSerializer.Deserialize<AILabel>("{\"cashReserves\":1000,\"tier\":3}", SaveGameService.TestJsonOptions);
		Check(oldLabel.pressingForecastSingleLogUnits == 0f && oldLabel.pressingForecastAlbumLogUnits == 0f,
			"old saves load with no forecast");

		GD.Seed(9113); float expected = GD.Randf(); GD.Seed(9113);
		LabelPressingEconomics.Press(Fresh(ReleaseFormat.Album), label, 900);
		Check(GD.Randf() == expected, "pressing consumes no random draws");

		// --plant-credit (§11): the plant bills on PlantTermWeeks terms.
		var cashOnly = new AILabel { labelId = "cash-only", tier = LabelTier.Small };
		LabelPressingEconomics.SetPlantCreditForChecks(false);
		Check(LabelPressingEconomics.SettlePlantAccount(cashOnly, 300f, 10) == 300f && cashOnly.plantPayables.Count == 0 &&
			cashOnly.outstandingPlantPayables == 0f, "without plant credit the whole bill is paid the week it is billed");
		LabelPressingEconomics.SetPlantCreditForChecks(true);
		var onTerms = new AILabel { labelId = "on-terms", tier = LabelTier.Small };
		Check(LabelPressingEconomics.SettlePlantAccount(onTerms, 1000f, 10) == 0f && Mathf.IsEqualApprox(onTerms.outstandingPlantPayables, 1000f),
			"on credit a new bill is owed, not paid");
		Check(LabelPressingEconomics.SettlePlantAccount(onTerms, 200f, 12) == 0f && Mathf.IsEqualApprox(onTerms.outstandingPlantPayables, 1200f),
			"nothing is paid before its term runs out");
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.SettlePlantAccount(onTerms, 0f, 10 + LabelPressingEconomics.PlantTermWeeks), 1000f) &&
			Mathf.IsEqualApprox(onTerms.outstandingPlantPayables, 200f) && onTerms.plantPayables.Count == 1,
			"a bill is paid in full the week its term runs out, and later bills stay owed");
		Check(EconomicLabelLifecycle.CommitmentReserve(onTerms, false) -
			Math.Max(onTerms.GetMonthlyOverhead(), 0f) == 200f, "cash owed to the plant is held out of new commitments");
		var restoredTerms = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(onTerms, SaveGameService.TestJsonOptions),
			SaveGameService.TestJsonOptions);
		Check(restoredTerms.plantPayables.Count == 1 && restoredTerms.plantPayables[0].DueWeek == 12 + LabelPressingEconomics.PlantTermWeeks &&
			Mathf.IsEqualApprox(restoredTerms.plantPayables[0].Amount, 200f) && Mathf.IsEqualApprox(restoredTerms.outstandingPlantPayables, 200f),
			"plant payables survive saves");
		Check(oldLabel.plantPayables != null && oldLabel.plantPayables.Count == 0 && oldLabel.outstandingPlantPayables == 0f,
			"old saves load owing the plant nothing");
		Check(LabelPressingEconomics.SettlePlantAccount(new AILabel { isPlayerOwned = true }, 50f, 10) == 50f,
			"the player's plant stays with PlayerDesk");
		LabelPressingEconomics.SetPlantCreditForChecks(false);
		Check(EconomicLabelLifecycle.CommitmentReserve(onTerms, false) == Math.Max(onTerms.GetMonthlyOverhead(), 0f),
			"switched off, payables never enter the reserve");

		// Period-cost experiment: a physical invoice must match the existing player quote;
		// the old bundled composition allowance is instead paid only on sold copies.
		LabelPressingEconomics.SetPeriodSingleCostsForChecks(true);
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.UnitPressingCost(ReleaseFormat.Single, .30f), .14f) &&
			Mathf.IsEqualApprox(LabelPressingEconomics.SingleCompositionCost, .04f), "single physical and composition costs are separate");
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.PlantBill(ReleaseFormat.Single, 500, false, .30f), 128f) &&
			Mathf.IsEqualApprox(LabelPressingEconomics.PlantBill(ReleaseFormat.Single, 500, true, .30f), 90f) &&
			LabelPressingEconomics.PlantBill(ReleaseFormat.Single, 0, true, .30f) == 0f,
			"first run pays setup and shipping, repress reuses stampers, and no order pays nothing");
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.InitialRunReserve(label, ReleaseFormat.Single, .30f), 618f) &&
			LabelPressingEconomics.InitialRunReserve(new AILabel { isPlayerOwned = true }, ReleaseFormat.Single, .30f) == 0f,
			"initial reserve uses the same full invoice as settlement, without reserving royalties on unsold stock");
		Check(LabelPressingEconomics.PlantBill(ReleaseFormat.Album, 500, false, .95f) == 475f,
			"the single correction leaves other format costs alone");
		LabelPressingEconomics.SetPeriodAlbumCostsForChecks(true);
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.UnitPressingCost(ReleaseFormat.Album, .95f), .40f) &&
			Mathf.IsEqualApprox(LabelPressingEconomics.PlantBill(ReleaseFormat.Album, 500, false, .50f), 308f),
			"the paired LP correction uses the period pressing quote plus caller's jacket cost and order fees");
		var tenTracks = new Record { format = ReleaseFormat.Album, album = new Album { nonSingleTracks = new AlbumTrack[10] } };
		Check(Mathf.IsEqualApprox(LabelPressingEconomics.CompositionUnitCost(tenTracks), .20f) &&
			Mathf.IsEqualApprox(LabelPressingEconomics.CompositionUnitCost(new Record { format = ReleaseFormat.Album }), .22f),
			"album composition allowance follows actual tracks, with the generator's mean for missing catalog snapshots");
		LabelPressingEconomics.SetPeriodAlbumCostsForChecks(false);
		LabelPressingEconomics.SetPeriodSingleCostsForChecks(false);

		// --small-home-territory (§11): factory Small labels start in their home region only, with every draw still made.
		var territoryChecks = SmallLabelHomeTerritoryChecks();
		GD.Print("LABEL_PRESSING_CHECK_PASS dealer prices, plant minimum, re-press lots, geometric forecast, catalog, player boundary, saves, RNG, plant credit, "
			+ territoryChecks);
	}

	private static string SmallLabelHomeTerritoryChecks() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Small home territory: " + why); }
		SmallLabelHomeTerritory.SetEnabledForChecks(false);
		GD.Seed(4401);
		var before = AILabelFactory.GenerateAllLabels(600);
		float nextBefore = GD.Randf();
		SmallLabelHomeTerritory.SetEnabledForChecks(true);
		GD.Seed(4401);
		var after = AILabelFactory.GenerateAllLabels(600);
		float nextAfter = GD.Randf();
		SmallLabelHomeTerritory.SetEnabledForChecks(false);
		Check(nextBefore == nextAfter && before.Count == after.Count, "the factory makes the same draws with the switch on");
		int small = 0, widened = 0;
		for (int i = 0; i < before.Count; i++) {
			AILabel off = before[i], on = after[i];
			Check(off.labelId == on.labelId && off.tier == on.tier && off.cashReserves == on.cashReserves && off.homeRegion == on.homeRegion,
				"every label keeps its identity, tier and cash");
			if (on.tier != LabelTier.Small) {
				Check(SameRegions(off.distributionRegions, on.distributionRegions) && SameRegions(off.strongRegions, on.strongRegions),
					"other tiers keep their territory");
				continue;
			}
			small++;
			if (off.distributionRegions.Length > 1 || off.strongRegions.Length > 1) widened++;
			Check(on.distributionRegions.Length == 1 && on.distributionRegions[0] == on.homeRegion &&
				on.strongRegions.Length == 1 && on.strongRegions[0] == on.homeRegion, "a Small label covers and is strong only at home");
		}
		Check(small > 100 && widened > small / 3, "the check sees many Small labels the old factory widened");
		return $"small home territory ({widened}/{small} Small narrowed)";
	}

	private static bool SameRegions(string[] a, string[] b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>());
}
