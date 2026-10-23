using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class LabelReleaseProgrammeChecks {
	public static void CheckLiveState() {
		var manager = CompetitorManager.Instance;
		var state = manager.ProgrammeState;
		if (state == null) throw new InvalidOperationException("Enabled programme has no saved state.");
		if (state.Items.Select(i => i.Id).Distinct(StringComparer.Ordinal).Count() != state.Items.Count)
			throw new InvalidOperationException("Duplicate dated slate slot.");
		foreach (var item in state.Items) {
			if (item.State is ReleaseSlateState.Ready or ReleaseSlateState.Scheduled && item.PreparedRecord == null)
				throw new InvalidOperationException("A ready slate item must carry its recorded master.");
			if (item.PreparedRecord != null && (item.PreparedRecord.recordId != item.RecordId || item.ReservedProduction != 0))
				throw new InvalidOperationException("Prepared master identity or prepaid production hold changed.");
			if (item.PaidProduction < 0 || item.ReservedProduction < 0 || item.ReservedMarketing < 0 || item.ReservedPressing < 0)
				throw new InvalidOperationException("Negative programme resource.");
			if (item.State == ReleaseSlateState.Issued && manager.GetLabel(item.LabelId) is { } label) {
				var issued = ChartManager.Instance.GetRecordRuntimeData(item.RecordId);
				// Later initial-pressing invoices also enter sunkProductionCost through the existing plant ledger.
				if (issued != null && issued.baseRecord.labelId == label.labelId && issued.sunkProductionCost + .01f < item.PaidProduction)
					throw new InvalidOperationException($"Issued master lost its prepaid production cost: slot={item.Id} record={item.RecordId} format={item.Format} sunk={issued.sunkProductionCost} paid={item.PaidProduction} role={issued.baseRecord.projectRole}");
			}
		}
		GD.Print($"LABEL_RELEASE_PROGRAMME_LIVE passed items={state.Items.Count} prepared={state.Items.Count(i => i.PreparedRecord != null)}");
	}

	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Release programme: " + message); checks++; }
		var manager = CompetitorManager.Instance;
		bool allocation = LabelReleaseProgramme.AllocationEnabled, capacity = LabelReleaseProgramme.CapacityEnabled;
		var previous = manager.ProgrammeState;
		try {
			LabelReleaseProgramme.SetForChecks(false, false); manager.SetProgrammeForChecks(null);
			Check(!LabelReleaseProgramme.HasState, "off path has no state");
			var clean = new CompetitorSaveData();
			Check(!JsonSerializer.Serialize(clean, SaveGameService.TestJsonOptions).Contains("ReleaseProgramme"), "off schema omits the new field");
			GameRandom.Seed(7712); float expected = GameRandom.Randf(); GameRandom.Seed(7712);
			double roll = LabelReleaseProgramme.Roll("label", "act", "1960-02-01:single:0", 5, 1);
			Check(expected == GameRandom.Randf(), "keyed decisions do not advance global RNG");
			Check(roll == LabelReleaseProgramme.Roll("label", "act", "1960-02-01:single:0", 5, 1), "same semantic identity replays");
			Check(roll != LabelReleaseProgramme.Roll("label", "other-act", "1960-02-01:single:0", 5, 1), "act identity separates decisions");
			var namedA = new AILabel { labelName = "Hickory Records", isHistorical = true, homeCityId = "nashville", foundedYear = 1955 };
			var namedB = new AILabel { labelName = "Hickory Records", isHistorical = false, homeCityId = "nashville", foundedYear = 1955 };
			Check(LabelReleaseProgramme.LabelIdentity(namedA) != LabelReleaseProgramme.LabelIdentity(namedB), "a procedural namesake must not share the historical firm's dated slot");
			Check(LabelReleaseProgramme.Slots(2.5f, .2) == 3 && LabelReleaseProgramme.Slots(2.5f, .8) == 2 && LabelReleaseProgramme.Slots(0, 0) == 0,
				"capacity supports zero and several slots without a carry bank");
			LabelReleaseProgramme.SetForChecks(true, true);
			var state = new ReleaseProgrammeState(); manager.SetProgrammeForChecks(state);
			var artist = new SimulatedArtist { artistId = "programme-check-act", weeksSinceLastRelease = 20 };
			manager.ObserveProgrammeRelease(artist, "single-check", ReleaseFormat.Single);
			int singleWeek = state.Clocks[artist.artistId].SingleWeek;
			state.Clocks[artist.artistId].SingleWeek -= 7;
			manager.ObserveProgrammeRelease(artist, "lp-check", ReleaseFormat.Album);
			Check(state.Clocks[artist.artistId].SingleWeek == singleWeek - 7 && state.Clocks[artist.artistId].AlbumWeek == singleWeek, "LP delivery leaves the single clock untouched");
			var label = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned && l.IsActive);
			float cash = label.cashReserves;
			float baseReserve = EconomicLabelLifecycle.CommitmentReserve(label, false, includeProgramme: false);
			var item = new ReleaseSlateItem { Id = "programme-check-slot", LabelId = label.labelId, ArtistId = artist.artistId, Format = ReleaseFormat.Single,
				State = ReleaseSlateState.InPreparation, ReservedProduction = 123, ReservedMarketing = 45, IntendedWeek = 19, DueWeek = 20,
				IntendedDate = new GameDate(1960, 5, 13), Source = "hit-follow-up", Evidence = "observed-hit", Reason = "reserved" };
			state.Items.Add(item);
			Check(manager.ProgrammeCashReserved(label.labelId) == 168, "preparation holds production and promotion cash");
			Check(Math.Abs(EconomicLabelLifecycle.CommitmentReserve(label, false) - baseReserve - 168) < .1,
				"existing finance reserve sees programme holds");
			Check(EconomicLabelLifecycle.CommitmentReserve(label, false, includeProgramme: false) == baseReserve && label.cashReserves == cash,
				"the parent reserve excludes holds; planning creates no payment or funding");
			bool studioEnabled = StudioParentLabels.Enabled;
			try {
				StudioParentLabels.SetEnabledForChecks(true);
				var studio = new AILabel { labelId = "programme-studio-check", studioParent = StudioParentLabels.Mgm,
					tier = LabelTier.MidTier, status = LabelStatus.Stable, homeCityId = "new_york" };
				LabelCompanyAccounts.Open(studio);
				studio.cashReserves = EconomicLabelLifecycle.CommitmentReserve(studio, false, includeProgramme: false);
				var market = new AILabel { labelId = "programme-market-check", tier = LabelTier.Major };
				LabelCompanyAccounts.Open(market); market.company.lastStatement = new LabelCompanyStatement { recordGross = 1e6 };
				state.Items.Add(new ReleaseSlateItem { LabelId = studio.labelId, State = ReleaseSlateState.InPreparation, ReservedProduction = 5000, ReservedMarketing = 1000 });
				Check(StudioParentLabels.SupportMonth(studio, new[] { market, studio }, new GameDate(1960, 2, 1)) == 0,
					"an actual parent support review cannot fund a planned launch indirectly");
				state.Items.RemoveAt(state.Items.Count - 1);
			} finally { StudioParentLabels.SetEnabledForChecks(studioEnabled); }
			item.State = ReleaseSlateState.Cancelled;
			Check(manager.ProgrammeCashReserved(label.labelId) == 0, "cancellation frees the hold");
			item.State = ReleaseSlateState.Postponed; item.MissedFollowUp = true;
			state.PlannedThrough[label.labelId] = 22;
			state.Campaigns.Add(new ProgrammeCampaign { LabelId = label.labelId, RecordId = "check-record", Week = singleWeek, Spend = 50, Share = .25f });
			var runtime = new RecordRuntimeData { baseRecord = new Record { recordId = "check-record" } };
			Check(manager.ProgrammePushFactor(runtime) == .25f, "booked campaign divides ongoing label push");
			string json = JsonSerializer.Serialize(state, SaveGameService.TestJsonOptions);
			var restored = JsonSerializer.Deserialize<ReleaseProgrammeState>(json, SaveGameService.TestJsonOptions);
			Check(JsonSerializer.Serialize(restored, SaveGameService.TestJsonOptions) == json, "dates, reservations, clocks and bounded ledger round-trip");
			Check(restored.Items.Single().MissedFollowUp && restored.PlannedThrough[label.labelId] == 22, "misses and planning cursor persist");
			Check(SaveGameService.WritingVersion == 28, "programme state requires save v28");
			GD.Print($"LABEL_RELEASE_PROGRAMME_CHECK passed checks={checks} studio={StudioParentLabels.Enabled}");
		} finally { manager.SetProgrammeForChecks(previous); LabelReleaseProgramme.SetForChecks(allocation, capacity); }
	}
}
