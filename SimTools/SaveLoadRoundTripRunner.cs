using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Headless verification for the full-world save (player-loop-cleanup). Runs the generated AI world forward
/// a few weeks, then round-trips the world snapshot: Capture -> serialize (A) -> deserialize -> Apply
/// (rehydrate in place) -> Capture -> serialize (B). A byte-identical A == B proves the capture/rehydrate
/// pair is lossless for every world section implemented so far -- any field that is captured but not restored
/// (or vice versa) shows up as a diff. This is the plan's "round-trip byte-identity" check.
///
/// Launch (mirrors ChartAuditRunner), e.g.:
///   Godot_console.exe --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- \
///     --weeks=26 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
///
/// Exit code 0 = PASS, 1 = FAIL (diff printed), 3 = error.
/// </summary>
public partial class SaveLoadRoundTripRunner : Node {
	public override void _Ready() {
		try {
			int weeks = 26;
			bool integration = false;
			string inspectSlot = null;
			foreach (string arg in OS.GetCmdlineUserArgs()) {
				if (arg.StartsWith("--weeks=", StringComparison.Ordinal)) weeks = int.Parse(arg["--weeks=".Length..]);
				else if (arg == "--integration") integration = true;
				else if (arg.StartsWith("--inspect-slot=", StringComparison.Ordinal)) inspectSlot = arg["--inspect-slot=".Length..];
			}

			if (TimeManager.Instance == null || ChartManager.Instance == null)
				throw new InvalidOperationException("TimeManager and ChartManager autoloads must be available.");

			// Inspecting a real save loads it over the freshly generated world; it must NOT be run forward first.
			if (inspectSlot != null) { RunInspect(inspectSlot); return; }

			for (int w = 0; w < weeks && !TimeManager.Instance.IsGameOver; w++) AdvanceOneChartWeek();

			if (integration) { RunIntegration(weeks); return; }

			JsonSerializerOptions opts = SaveGameService.TestJsonOptions;

			WorldSaveData cap1 = WorldStateService.Capture();
			string a = JsonSerializer.Serialize(cap1, opts);

			WorldSaveData restored = JsonSerializer.Deserialize<WorldSaveData>(a, opts);
			WorldStateService.Apply(restored, TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);

			WorldSaveData cap2 = WorldStateService.Capture();
			string b = JsonSerializer.Serialize(cap2, opts);

			if (a == b) {
				// Also exercise the real gzip + Godot FileAccess buffer IO path end-to-end, and report the
				// compressed size (the actual on-disk save size for this world).
				string gzipStatus = VerifyGzipFileIo(cap1, opts, out long compressedBytes);
				GD.Print($"SAVELOAD_ROUNDTRIP_PASS weeks={weeks} bytes={a.Length} gzipBytes={compressedBytes} " +
					$"ratio={(a.Length / (double)Math.Max(1, compressedBytes)):F1}x gzipIo={gzipStatus} " +
					$"labels={cap1.Labels.Count} artists={cap1.Artists.Count} unsigned={cap1.UnsignedArtistIds.Count} " +
					$"defunct={cap1.DefunctLabelIds.Count} chartWeek={cap1.ChartWeek}");
				GetTree().Quit(gzipStatus == "ok" ? 0 : 2);
			} else {
				int i = 0;
				while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
				string dir = OS.GetUserDataDir();
				System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "roundtrip_a.json"), a);
				System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "roundtrip_b.json"), b);
				GD.Print($"SAVELOAD_ROUNDTRIP_FAIL weeks={weeks} lenA={a.Length} lenB={b.Length} firstDiff={i} dumped={dir}");
				GetTree().Quit(1);
			}
		} catch (Exception exception) {
			GD.PushError("SAVELOAD_ROUNDTRIP_ERROR: " + exception);
			GetTree().Quit(3);
		}
	}

	/// <summary>Mirrors SaveGameService's gzip file IO: stream a world through GZipStream into a buffer, store it
	/// with Godot FileAccess, read it back, decompress, and confirm it deserializes+reserializes to the same
	/// JSON. Returns "ok" or a failure detail, and the compressed byte count.</summary>
	private static string VerifyGzipFileIo(WorldSaveData world, JsonSerializerOptions opts, out long compressedBytes) {
		compressedBytes = 0;
		try {
			string expected = JsonSerializer.Serialize(world, opts);
			byte[] payload;
			using (var buffer = new System.IO.MemoryStream()) {
				using (var gz = new System.IO.Compression.GZipStream(buffer, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
					JsonSerializer.Serialize(gz, world, opts);
				payload = buffer.ToArray();
			}
			compressedBytes = payload.LongLength;

			const string path = "user://saves/_roundtrip_probe.json";
			DirAccess.MakeDirRecursiveAbsolute("user://saves");
			using (Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write)) {
				if (file == null) return "open-write-failed";
				file.StoreBuffer(payload);
			}
			byte[] readBack;
			using (Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read)) {
				if (file == null) return "open-read-failed";
				readBack = file.GetBuffer((long)file.GetLength());
			}
			string actual;
			using (var input = new System.IO.MemoryStream(readBack))
			using (var gz = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress)) {
				WorldSaveData restored = JsonSerializer.Deserialize<WorldSaveData>(gz, opts);
				actual = JsonSerializer.Serialize(restored, opts);
			}
			Godot.DirAccess.RemoveAbsolute(path);
			return actual == expected ? "ok" : "content-mismatch";
		} catch (Exception e) {
			return "error:" + e.GetType().Name;
		}
	}

	/// <summary>End-to-end shipping-flow test: found a player label, save to disk (gzip), perturb the live world
	/// (advance a week, spend the player's cash), load from disk, and confirm both the AI world and the player
	/// came back to the save-time state -- proving WorldStateService.Apply + PlayerDesk.RestoreState + the gzip
	/// file path all cooperate.</summary>
	private void RunIntegration(int weeks) {
		const string slot = "_integration_probe";
		try {
			// Found a player label in the first available city.
			var cities = DistanceModel.GetCities();
			string cityId = cities.Count > 0 ? cities[0].cityId : null;
			if (cityId == null || PlayerDesk.Instance == null) { GD.Print("SAVELOAD_INTEGRATION_FAIL reason=no-city-or-desk"); GetTree().Quit(3); return; }
			if (!PlayerDesk.Instance.FoundLabel("Probe Records", cityId, out string founded)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=found:{founded}"); GetTree().Quit(3); return; }

			// Distribution-expansion Stage 1 sanity check: the named-stop roster generates without
			// crashing, with a legible (not hundreds-of-shops) count, and is stable across repeat calls
			// in the same session (EnsureStops caches rather than regenerating).
			var homeStops = PlayerDesk.Instance.StopsInCity(cityId).ToList();
			int shopCount = homeStops.Count(s => s.Kind == PlayerDesk.StopKind.Shop);
			int opCount = homeStops.Count(s => s.Kind == PlayerDesk.StopKind.Op);
			var homeStops2 = PlayerDesk.Instance.StopsInCity(cityId).ToList();
			bool stableIds = homeStops.Select(s => s.StopId).SequenceEqual(homeStops2.Select(s => s.StopId));
			bool uniqueNames = homeStops.Select(s => s.DisplayName).Distinct(StringComparer.Ordinal).Count() == homeStops.Count;
			GD.Print($"STOPS_CHECK city={cityId} shops={shopCount} ops={opCount} stableIds={stableIds} uniqueNames={uniqueNames} " +
				$"sample=\"{string.Join("\", \"", homeStops.Take(3).Select(s => s.DisplayName))}\"");
			if (shopCount < 6 || opCount < 1 || opCount > 3 || !stableIds || !uniqueNames) {
				GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=stops-check shops={shopCount} ops={opCount} stableIds={stableIds} uniqueNames={uniqueNames}");
				GetTree().Quit(1);
				return;
			}

			// Hand-mutate one stop's state directly (bypassing the write/cut/press chain a real Pitch/
			// Consign call would need) to stress-test PlayerDesk's StopState capture/restore -- otherwise
			// this probe's stops are never touched and the new save path round-trips an empty list.
			PlayerDesk.PlayerStop probeStop = homeStops[0];
			probeStop.Relationship = 0.62f;
			probeStop.LastVisitWeek = ChartManager.Instance.GetCurrentChartWeek();
			probeStop.OpenBalance = 17.5f;
			probeStop.OnHand["probe_record_1"] = new PlayerDesk.ConsignmentLot {
				Remaining = 12, Placed = 20, DaysSinceRestock = 3, ConsignmentTerms = true
			};
			// Sticky-refusal / once-a-day-approach addendum: LastApproachDate and PassedRecordIds are
			// PlayerStop-only mutable state just like the fields above -- exercise their round-trip too.
			probeStop.LastApproachDate = TimeManager.Instance.CurrentDate;
			probeStop.PassedRecordIds.Add("probe_record_2");

			// §6 one-stop: not guaranteed to exist in the home city, so search the whole map for one and
			// exercise its Unlocked/Trusted round-trip the same way as the fields above.
			PlayerDesk.PlayerStop probeOneStop = cities
				.SelectMany(c => PlayerDesk.Instance.StopsInCity(c.cityId))
				.FirstOrDefault(s => s.Kind == PlayerDesk.StopKind.OneStop);
			if (probeOneStop != null) { probeOneStop.OneStopUnlocked = true; probeOneStop.OneStopTrusted = true; }

			// Distribution-expansion §7 (people): unlock + hire a commission runner, assign him a route
			// stop, and hand-mutate his carton/familiarity directly (same pattern as the stop mutation
			// above) to stress-test CaptureState/RestoreState for the whole PlayerRunner + the
			// ConsignmentLot.RunnerSourced flag it sets on a lot.
			PlayerDesk.Instance.DebugUnlockRunner(cityId);
			if (!PlayerDesk.Instance.HireRunner(out string runnerHireMsg)) {
				GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=runner-hire:{runnerHireMsg}");
				GetTree().Quit(1);
				return;
			}
			PlayerDesk.PlayerStop routeStop = homeStops.FirstOrDefault(s => s.Kind != PlayerDesk.StopKind.OneStop && s.StopId != probeStop.StopId) ?? homeStops[0];
			if (!PlayerDesk.Instance.AssignRunnerStop(routeStop.StopId, true, out string assignMsg)) {
				GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=runner-assign:{assignMsg}");
				GetTree().Quit(1);
				return;
			}
			PlayerDesk.Instance.Runner.CartonRecordId = "probe_record_3";
			PlayerDesk.Instance.Runner.CartonRemaining = 42;
			PlayerDesk.Instance.Runner.Familiarity[routeStop.StopId] = 0.37f;
			routeStop.OnHand["probe_record_3"] = new PlayerDesk.ConsignmentLot { Remaining = 5, Placed = 5, RunnerSourced = true };

			// Distribution-expansion §8 (factor the paper): a real WholesaleReceivable, hand-added the
			// same way a house line would book one, to prove it (a) survives save/load at all -- Label
			// isn't captured through PlayerSaveData's own DTOs, so this also stress-tests whatever path
			// DOES carry AILabel state -- and (b) factors correctly when asked.
			int receivableDueWeek = ChartManager.Instance.GetCurrentChartWeek() + 12;
			PlayerDesk.Instance.Label.wholesaleReceivables.Add(new WholesaleReceivable(receivableDueWeek, "probe_house_1", 200f));
			PlayerDesk.Instance.Label.outstandingWholesaleReceivables += 200f;

			// Distribution-expansion §11 (plant credit): hand-construct an outstanding credit (bypassing
			// the real backlog-eligibility gate, same spirit as DebugUnlockRunner) to prove PlantCredit
			// round-trips through CaptureState/RestoreState.
			int creditDueWeek = ChartManager.Instance.GetCurrentChartWeek() + PlayerDesk.PlantCreditTermWeeks;
			PlayerDesk.Instance.DebugSetPlantCredit(new PlayerDesk.PlantCredit { RecordId = "probe_record_5", Amount = 313f, DueWeek = creditDueWeek });

			// §7 project promo is ephemeral (not persisted -- same as the existing Rolodex payola calls),
			// so it has no CaptureState/RestoreState surface to round-trip. Smoke-test the ChartManager/
			// PayolaLedger wiring directly instead of building a full release just to exercise it.
			var promoStations = ChartManager.Instance.ReporterStationsInRegion(DistanceModel.GetCityById(cityId)?.parentRegionId ?? "");
			bool promoOk = true;
			if (promoStations.Count > 0) {
				var placed = ChartManager.Instance.PlaceProjectPromo("probe_record_4", PlayerDesk.Instance.Label.labelId,
					new[] { promoStations[0].stationId }, 0.5f, 0.5f, 0.1f, ChartManager.Instance.GetCurrentChartWeek(),
					TimeManager.Instance.CurrentDate.year, TimeManager.Instance.CurrentDate.month, 2);
				promoOk = placed != null && placed.Count == 1;
				GD.Print($"PROJECT_PROMO_SMOKE stations={promoStations.Count} placed={placed?.Count ?? 0} ok={promoOk}");
			}

			// Distribution-expansion §4 (inbound demand): the answering-service purchase is a real
			// AILabel field folded into GetMonthlyOverhead (LabelSaveData.HasAnsweringService), not
			// PlayerDesk-only state -- exercise its round-trip alongside the stop mutation above.
			if (!PlayerDesk.Instance.PurchaseAnsweringService(out string answeringMsg)) {
				GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=answering-service:{answeringMsg}");
				GetTree().Quit(1);
				return;
			}

			// Distribution-expansion §9 (late exits): mark one-stop exposure and stage a master
			// sale/lease directly (same bypass spirit as DebugSetPlantCredit -- driving the real gate
			// would mean releasing an actual record with real regional radioPlay) to prove
			// soldMasterRecordIds/leasedMasterExpiryWeek/oneStopKnownRecordIds round-trip. Also stage an
			// unsigned P&D offer on the desk (PendingDistributionOffer) and a genuinely SIGNED deal via
			// CompetitorManager.SignDistributionDeal -- the signed deal travels on Label.ActiveDeal,
			// excluded from the full-world save's generic AILabel capture, so it's the one most likely
			// to silently vanish on load if LabelSaveData ever drifts.
			PlayerDesk.Instance.DebugMarkOneStopKnown("probe_record_6");
			int leaseExpiryWeek = ChartManager.Instance.GetCurrentChartWeek() + PlayerDesk.MasterLeaseTermWeeks;
			PlayerDesk.Instance.DebugSetMasterSold("probe_record_7");
			PlayerDesk.Instance.DebugSetMasterLeased("probe_record_8", leaseExpiryWeek);

			var pendingOfferStage = new DistributionDeal {
				distributorId = "probe_distributor_1", reachGranted = 0.4f, grantedRegions = new[] { "probe_region_1" },
				marginSkim = 0.3f, ownsMasters = false, advance = 500f, unrecoupedAdvance = 500f,
				signedWeek = ChartManager.Instance.GetCurrentChartWeek(), termWeeks = 78, origin = DealOrigin.LabelSought
			};
			PlayerDesk.Instance.DebugSetPendingDistributionOffer(pendingOfferStage);

			var signedDealStage = new DistributionDeal {
				distributorId = "probe_distributor_2", reachGranted = 0.6f, grantedRegions = new[] { "probe_region_2" },
				marginSkim = 0.35f, ownsMasters = true, advance = 800f, unrecoupedAdvance = 800f,
				signedWeek = ChartManager.Instance.GetCurrentChartWeek(), termWeeks = 104, origin = DealOrigin.DistributorCourted
			};
			CompetitorManager.Instance.SignDistributionDeal(PlayerDesk.Instance.Label, signedDealStage);

			// Save-time snapshot of world + player.
			int week0 = ChartManager.Instance.GetCurrentChartWeek();
			int labels0 = ChartManager.Instance.GetAllLabels().Count;
			int artists0 = ArtistManager.Instance.GetAllArtists().Count;
			AILabel sample0 = ChartManager.Instance.GetAllLabels().FirstOrDefault(l => !l.isPlayerOwned);
			string sampleId = sample0?.labelId; float sampleCash0 = sample0?.cashReserves ?? 0f;
			string playerName0 = PlayerDesk.Instance.Label.labelName; float playerCash0 = PlayerDesk.Instance.Label.cashReserves;

			if (!SaveGameService.Save(slot, out string saveMsg)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=save:{saveMsg}"); GetTree().Quit(1); return; }

			// Perturb: advance a week (moves the whole world) and drain the player's cash.
			AdvanceOneChartWeek();
			PlayerDesk.Instance.Label.cashReserves = -99999f;
			int weekPerturbed = ChartManager.Instance.GetCurrentChartWeek();

			if (!SaveGameService.Load(slot, out string loadMsg)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=load:{loadMsg}"); GetTree().Quit(1); return; }

			// Verify everything is back to the save-time state.
			int week1 = ChartManager.Instance.GetCurrentChartWeek();
			int labels1 = ChartManager.Instance.GetAllLabels().Count;
			int artists1 = ArtistManager.Instance.GetAllArtists().Count;
			float sampleCash1 = ChartManager.Instance.GetLabelById(sampleId)?.cashReserves ?? float.NaN;
			string playerName1 = PlayerDesk.Instance.Label?.labelName;
			float playerCash1 = PlayerDesk.Instance.Label?.cashReserves ?? float.NaN;

			// The hand-mutated stop above: identity regenerates fresh (same StopId, since it's a pure
			// function of the unchanged world seed), so the mutable state should have round-tripped
			// through StopState onto that same stop.
			PlayerDesk.PlayerStop probeStop1 = PlayerDesk.Instance.StopsInCity(cityId).FirstOrDefault(s => s.StopId == probeStop.StopId);
			bool stopOk = probeStop1 != null
				&& Math.Abs(probeStop1.Relationship - 0.62f) < 0.001f
				&& Math.Abs(probeStop1.OpenBalance - 17.5f) < 0.01f
				&& probeStop1.OnHand.TryGetValue("probe_record_1", out PlayerDesk.ConsignmentLot probeLot)
				&& probeLot.Remaining == 12 && probeLot.Placed == 20 && probeLot.DaysSinceRestock == 3 && probeLot.ConsignmentTerms
				&& probeStop1.LastApproachDate == TimeManager.Instance.CurrentDate
				&& probeStop1.PassedRecordIds.Contains("probe_record_2");
			GD.Print($"STOPS_ROUNDTRIP stopId={probeStop.StopId} found={probeStop1 != null} " +
				$"relationship={probeStop1?.Relationship} openBalance={probeStop1?.OpenBalance} " +
				$"lastApproach={probeStop1?.LastApproachDate} passed={probeStop1?.PassedRecordIds.Count} ok={stopOk}");

			bool oneStopOk = true;
			if (probeOneStop != null) {
				PlayerDesk.PlayerStop probeOneStop1 = PlayerDesk.Instance.StopsInCity(probeOneStop.CityId)
					.FirstOrDefault(s => s.StopId == probeOneStop.StopId);
				oneStopOk = probeOneStop1 != null && probeOneStop1.OneStopUnlocked && probeOneStop1.OneStopTrusted;
				GD.Print($"ONESTOP_ROUNDTRIP stopId={probeOneStop.StopId} found={probeOneStop1 != null} " +
					$"unlocked={probeOneStop1?.OneStopUnlocked} trusted={probeOneStop1?.OneStopTrusted} ok={oneStopOk}");
			}

			bool answeringServiceOk = PlayerDesk.Instance.HasAnsweringService;
			GD.Print($"ANSWERING_SERVICE_ROUNDTRIP ok={answeringServiceOk}");

			PlayerDesk.PlayerRunner runner1 = PlayerDesk.Instance.Runner;
			PlayerDesk.PlayerStop routeStop1 = PlayerDesk.Instance.StopsInCity(cityId).FirstOrDefault(s => s.StopId == routeStop.StopId);
			PlayerDesk.ConsignmentLot runnerLot = null;
			bool runnerLotOk = routeStop1 != null && routeStop1.OnHand.TryGetValue("probe_record_3", out runnerLot)
				&& runnerLot.RunnerSourced && runnerLot.Remaining == 5;
			bool runnerOk = PlayerDesk.Instance.RunnerUnlocked && runner1 != null
				&& runner1.CartonRecordId == "probe_record_3" && runner1.CartonRemaining == 42
				&& runner1.RouteStopIds.Contains(routeStop.StopId)
				&& runner1.Familiarity.TryGetValue(routeStop.StopId, out float fam) && Math.Abs(fam - 0.37f) < 0.001f
				&& runnerLotOk;
			GD.Print($"RUNNER_ROUNDTRIP unlocked={PlayerDesk.Instance.RunnerUnlocked} hasRunner={runner1 != null} " +
				$"cartonRecord={runner1?.CartonRecordId} cartonQty={runner1?.CartonRemaining} " +
				$"onRoute={runner1?.RouteStopIds.Contains(routeStop.StopId)} lotRunnerSourced={runnerLot?.RunnerSourced} ok={runnerOk}");

			var creditOwed1 = PlayerDesk.Instance.PlantCreditOwed;
			bool plantCreditOk = creditOwed1.HasValue && creditOwed1.Value.RecordId == "probe_record_5"
				&& Math.Abs(creditOwed1.Value.Amount - 313f) < 0.01f && creditOwed1.Value.WeeksAway == PlayerDesk.PlantCreditTermWeeks;
			GD.Print($"PLANT_CREDIT_ROUNDTRIP found={creditOwed1.HasValue} recordId={creditOwed1?.RecordId} " +
				$"amount={creditOwed1?.Amount} weeksAway={creditOwed1?.WeeksAway} ok={plantCreditOk}");

			var invoices1 = PlayerDesk.Instance.OutstandingInvoices().ToList();
			bool receivableOk = invoices1.Any(inv => Math.Abs(inv.Amount - 200f) < 0.01f);
			GD.Print($"RECEIVABLE_ROUNDTRIP count={invoices1.Count} found200={receivableOk}");
			bool factorOk = false;
			if (receivableOk) {
				int idx = invoices1.FindIndex(inv => Math.Abs(inv.Amount - 200f) < 0.01f);
				float rate = PlayerDesk.Instance.FactorRatePreview(idx);
				float cashBefore = PlayerDesk.Instance.Label.cashReserves;
				float owedBefore = PlayerDesk.Instance.Label.outstandingWholesaleReceivables;
				if (!PlayerDesk.Instance.FactorReceivable(idx, out string factorMsg)) {
					GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=factor:{factorMsg}");
					GetTree().Quit(1);
					return;
				}
				float cashAfter = PlayerDesk.Instance.Label.cashReserves;
				float owedAfter = PlayerDesk.Instance.Label.outstandingWholesaleReceivables;
				float expectedCash = 200f * rate;
				factorOk = Math.Abs((cashAfter - cashBefore) - expectedCash) < 0.01f && Math.Abs((owedBefore - owedAfter) - 200f) < 0.01f
					&& !PlayerDesk.Instance.OutstandingInvoices().Any(inv => Math.Abs(inv.Amount - 200f) < 0.01f);
				GD.Print($"FACTOR_CHECK rate={rate:F2} cashDelta={cashAfter - cashBefore:F2} expectedCash={expectedCash:F2} owedDelta={owedBefore - owedAfter:F2} ok={factorOk}");
			}

			// Distribution-expansion §9: one-stop exposure, master sold/leased state, the unsigned offer
			// on the desk, and the SIGNED deal on Label.ActiveDeal should all have round-tripped.
			bool oneStopKnownOk = PlayerDesk.Instance.IsOneStopKnown("probe_record_6");
			bool masterSoldOk = PlayerDesk.Instance.IsMasterOut("probe_record_7");
			bool masterLeasedOk = PlayerDesk.Instance.IsMasterOut("probe_record_8");
			GD.Print($"MASTER_DEAL_ROUNDTRIP oneStopKnown={oneStopKnownOk} sold={masterSoldOk} leased={masterLeasedOk}");

			DistributionDeal pendingOffer1 = PlayerDesk.Instance.PendingDistributionOffer;
			bool pendingOfferOk = pendingOffer1 != null && pendingOffer1.distributorId == "probe_distributor_1"
				&& Math.Abs(pendingOffer1.advance - 500f) < 0.01f && pendingOffer1.termWeeks == 78
				&& pendingOffer1.origin == DealOrigin.LabelSought && !pendingOffer1.ownsMasters;
			GD.Print($"PENDING_OFFER_ROUNDTRIP found={pendingOffer1 != null} distributor={pendingOffer1?.distributorId} ok={pendingOfferOk}");

			DistributionDeal activeDeal1 = PlayerDesk.Instance.Label?.activeDeal;
			bool activeDealOk = activeDeal1 != null && activeDeal1.distributorId == "probe_distributor_2"
				&& Math.Abs(activeDeal1.advance - 800f) < 0.01f && activeDeal1.termWeeks == 104
				&& activeDeal1.origin == DealOrigin.DistributorCourted && activeDeal1.ownsMasters;
			GD.Print($"ACTIVE_DEAL_ROUNDTRIP found={activeDeal1 != null} distributor={activeDeal1?.distributorId} ownsMasters={activeDeal1?.ownsMasters} ok={activeDealOk}");

			// Advance one week past the load. This is the exact path that regressed: the post-load settlement
			// rejects as out-of-order if CompetitorManager's lastBookedSettlementId wasn't restored in step with
			// the chart week. A throw here surfaces via the outer catch as SAVELOAD_INTEGRATION_ERROR.
			AdvanceOneChartWeek();
			int week2 = ChartManager.Instance.GetCurrentChartWeek();

			// Re-save after the post-load week. New AI records minted this week collide with restored ids and the
			// record-capture ToDictionary throws unless generatedRecordCounter was restored -- the exact overwrite
			// path that regressed. A throw surfaces via the outer catch as SAVELOAD_INTEGRATION_ERROR.
			if (!SaveGameService.Save(slot, out string resaveMsg)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=resave:{resaveMsg}"); GetTree().Quit(1); return; }

			var fails = new System.Collections.Generic.List<string>();
			if (week1 != week0) fails.Add($"week {week0}->{week1} (perturbed {weekPerturbed})");
			if (week2 != week1 + 1) fails.Add($"postLoadWeek {week1}->{week2} (expected +1)");
			if (labels1 != labels0) fails.Add($"labels {labels0}->{labels1}");
			if (artists1 != artists0) fails.Add($"artists {artists0}->{artists1}");
			if (Math.Abs(sampleCash1 - sampleCash0) > 0.01f) fails.Add($"aiLabelCash {sampleCash0}->{sampleCash1}");
			if (playerName1 != playerName0) fails.Add($"playerName {playerName0}->{playerName1}");
			if (Math.Abs(playerCash1 - playerCash0) > 0.01f) fails.Add($"playerCash {playerCash0}->{playerCash1}");
			if (!stopOk) fails.Add($"stopState stopId={probeStop.StopId} found={probeStop1 != null}");
			if (!oneStopOk) fails.Add($"oneStopState stopId={probeOneStop?.StopId}");
			if (!answeringServiceOk) fails.Add("answeringService lost on round-trip");
			if (!runnerOk) fails.Add($"runnerState stopId={routeStop.StopId}");
			if (!promoOk) fails.Add("projectPromoSmoke");
			if (!plantCreditOk) fails.Add("plantCreditState");
			if (!receivableOk) fails.Add("wholesaleReceivable lost on round-trip");
			else if (!factorOk) fails.Add("factorReceivable math");
			if (!oneStopKnownOk) fails.Add("oneStopKnownRecordIds lost on round-trip");
			if (!masterSoldOk) fails.Add("soldMasterRecordIds lost on round-trip");
			if (!masterLeasedOk) fails.Add("leasedMasterExpiryWeek lost on round-trip");
			if (!pendingOfferOk) fails.Add("pendingDistributionOffer lost on round-trip");
			if (!activeDealOk) fails.Add("player Label.activeDeal lost on round-trip");

			SaveGameService.Delete(slot);
			if (fails.Count == 0) {
				GD.Print($"SAVELOAD_INTEGRATION_PASS weeks={weeks} week={week0} labels={labels0} artists={artists0} " +
					$"aiLabelCash={sampleCash0:F1} player={playerName0} playerCash={playerCash0:F1}");
				GetTree().Quit(0);
			} else {
				GD.Print("SAVELOAD_INTEGRATION_FAIL " + string.Join(" | ", fails));
				GetTree().Quit(1);
			}
		} catch (Exception e) {
			GD.PushError("SAVELOAD_INTEGRATION_ERROR: " + e);
			try { SaveGameService.Delete(slot); } catch { }
			GetTree().Quit(3);
		}
	}

	/// <summary>
	/// Loads a real save from disk and prints what the desk actually holds afterwards: the discography, and
	/// the town-stock readout the DISTRIBUTION screen draws from. A stock line whose title is still a bare
	/// record id ("player_2") is the symptom this exists to catch -- a record the dead-stock cull deleted out
	/// from under the references that point at it. Fails on any such line; otherwise diagnostic only.
	/// </summary>
	private void RunInspect(string slot) {
		if (PlayerDesk.Instance == null) { GD.Print("SAVELOAD_INSPECT_ERROR reason=no-desk"); GetTree().Quit(3); return; }
		if (!SaveGameService.Load(slot, out string loadMsg)) {
			GD.Print($"SAVELOAD_INSPECT_FAIL slot={slot} reason=load:{loadMsg}");
			GetTree().Quit(1);
			return;
		}
		PlayerDesk desk = PlayerDesk.Instance;
		GD.Print($"SAVELOAD_INSPECT slot={slot} message=\"{loadMsg}\"");
		int titles = 0;
		foreach (RecordRuntimeData record in desk.ReleasedRecords) {
			titles++;
			GD.Print($"  DISCOGRAPHY {record.baseRecord.recordId} \"{record.baseRecord.title}\" by {record.baseRecord.artistName}" +
				$" released={record.baseRecord.releaseDate} weeks={record.weeksSinceRelease} units={record.totalUnitsSold}");
		}

		int unresolved = 0;
		foreach ((string cityName, string stopName, string title, int remaining) in desk.StopStock()) {
			bool dangling = System.Text.RegularExpressions.Regex.IsMatch(title, @"^player_\d+$");
			if (dangling) unresolved++;
			GD.Print($"  STOCK {cityName} — {stopName} \"{title}\" remaining={remaining} dangling={dangling}");
		}
		GD.Print(unresolved == 0
			? $"SAVELOAD_INSPECT_PASS slot={slot} discography={titles} danglingStockRefs=0"
			: $"SAVELOAD_INSPECT_FAIL slot={slot} discography={titles} danglingStockRefs={unresolved}");
		GetTree().Quit(unresolved == 0 ? 0 : 1);
	}

	private static void AdvanceOneChartWeek() {
		int start = ChartManager.Instance.GetCurrentChartWeek();
		while (ChartManager.Instance.GetCurrentChartWeek() == start && !TimeManager.Instance.IsGameOver)
			TimeManager.Instance.DebugAdvanceWeek();
	}
}
