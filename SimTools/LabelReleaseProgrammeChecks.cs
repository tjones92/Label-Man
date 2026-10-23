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
			if ((item.State == ReleaseSlateState.Issued && item.ActualWeek < LabelReleaseProgramme.EarliestIssueWeek(item)) ||
				(item.State == ReleaseSlateState.AwaitingDelivery && item.LastReviewWeek < LabelReleaseProgramme.EarliestIssueWeek(item)))
				throw new InvalidOperationException("A recorded master must retain its issue lead, including an advance-single launch.");
			if (item.State == ReleaseSlateState.AwaitingDelivery && (item.Format != ReleaseFormat.Album || item.PreparedRecord != null ||
				item.ActualWeek != 0 || item.ReservedProduction != 0 || item.ReservedMarketing != 0 || item.ReservedPressing != 0))
				throw new InvalidOperationException("An awaiting LP must leave delivery and commitments with its existing project.");
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
			var timing = new ReleaseSlateItem { ProposedWeek = 5, IntendedWeek = 8, ReadyWeek = 7, PreparedWeek = 10 };
			Check(LabelReleaseProgramme.EarliestIssueWeek(timing) == 11, "overdue recording cannot issue in the session week");
			timing.PreparedWeek = 6;
			Check(LabelReleaseProgramme.EarliestIssueWeek(timing) == 7, "work prepared early may use next week's idle issue slot");
			timing.PreparedWeek = timing.IntendedWeek = timing.ProposedWeek;
			Check(LabelReleaseProgramme.EarliestIssueWeek(timing) == 5, "the first horizon keeps its opening work-in-hand exception");
			timing.PreparedWeek = 10;
			Check(LabelReleaseProgramme.EarliestIssueWeek(timing) == 11, "postponed opening work cannot reuse the opening exception");
			manager.RescheduleProgrammeItem(timing, 12, ReleaseSlateState.Postponed, "timing-check");
			var timingSave = JsonSerializer.Deserialize<ReleaseSlateItem>(JsonSerializer.Serialize(timing, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(timingSave.PreparedWeek == 10 && LabelReleaseProgramme.EarliestIssueWeek(timingSave) == 11,
				"rescheduling and save/load retain the actual session week");
			timing.PreparedWeek = null;
			var oldTiming = JsonSerializer.Deserialize<ReleaseSlateItem>(JsonSerializer.Serialize(timing, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(oldTiming.PreparedWeek == null && LabelReleaseProgramme.EarliestIssueWeek(oldTiming) == 7,
				"older saves retain their readiness boundary without inventing an actual session date");
			var delayed = new ReleaseSlateItem { Id = "delayed-lp-slot", LabelId = label.labelId, ArtistId = artist.artistId,
				Format = ReleaseFormat.Album, RecordId = "delayed-lp-master", PreparedRecord = new Record { recordId = "delayed-lp-master" },
				PaidProduction = 321, ReservedMarketing = 45, ReservedPressing = 90 };
			state.Items.Add(delayed);
			manager.AwaitProgrammeDelivery(delayed);
			Check(delayed.State == ReleaseSlateState.AwaitingDelivery && delayed.ActualWeek == 0 && delayed.PreparedRecord == null &&
				delayed.ReservedMarketing == 0 && delayed.ReservedPressing == 0 && delayed.PaidProduction == 321,
				"an advance single does not issue the LP or duplicate the project's master and holds");
			var pending = new AlbumProject { currentLabelId = "transferred-owner", terminalState = AlbumProjectTerminalState.PendingAtAuditEnd };
			manager.ReconcileProgrammeDelivery(delayed, pending);
			Check(delayed.State == ReleaseSlateState.AwaitingDelivery, "a transferred pending LP remains on its existing delivery path");
			var pendingSave = JsonSerializer.Deserialize<ReleaseSlateItem>(JsonSerializer.Serialize(delayed, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(pendingSave.State == ReleaseSlateState.AwaitingDelivery && pendingSave.ActualWeek == 0 && pendingSave.PaidProduction == 321,
				"awaiting delivery and its paid cost round-trip without inventing an issue date");
			var deliveryDate = new GameDate(1960, 6, 3);
			manager.CompleteProgrammeDelivery(delayed.RecordId, deliveryDate, singleWeek + 3);
			Check(delayed.State == ReleaseSlateState.Issued && delayed.ActualWeek == singleWeek + 3 && delayed.ActualDate == deliveryDate &&
				state.Clocks[artist.artistId].SingleWeek == singleWeek - 7, "the LP's actual delivery date leaves the single clock alone");
			manager.AwaitProgrammeDelivery(delayed);
			manager.ReconcileProgrammeDelivery(delayed, new AlbumProject { terminalState = AlbumProjectTerminalState.Cancelled });
			Check(delayed.State == ReleaseSlateState.Cancelled && delayed.PaidProduction == 321 && delayed.ActualWeek == 0,
				"cancelled LP delivery retains its paid cost without claiming a release");
			state.Campaigns.Clear();
			var lp = new Record { recordId = "campaign-lp-check", format = ReleaseFormat.Album };
			float lpSpend = label.GetStandardMarketingBudget() * .25f;
			manager.BookProgrammeCampaign(lp, null, label, lpSpend);
			manager.BookProgrammeCampaign(lp, null, label, lpSpend);
			Check(state.Campaigns.Count == 1 && state.Campaigns[0].Spend == lpSpend && label.cashReserves == cash,
				"LP campaign capacity is booked once; the existing project owns its cash payment");
			Check(Math.Abs(manager.ProgrammePushFactor(new RecordRuntimeData { baseRecord = lp }) - .25f) < .001f,
				"delayed LPs receive only their booked share of ongoing label promotion");
			var future = new ReleaseSlateItem { Id = "future-hold", LabelId = "future-promotion-check", Format = ReleaseFormat.Single,
				State = ReleaseSlateState.Scheduled, IntendedWeek = singleWeek + 4, DueWeek = singleWeek + 4,
				ReservedProduction = 15, ReservedMarketing = 70, ReservedPressing = 35, PreparedRecord = lp, PaidProduction = 321 };
			state.Items.Add(future);
			state.Campaigns.Add(new ProgrammeCampaign { LabelId = future.LabelId, RecordId = "future-campaign", Week = singleWeek + 2, Spend = 40 });
			Check(manager.ProgrammePromotionCommittedPeak(future.LabelId) == 110, "future holds and paid campaigns share one overlapping capacity audit");
			manager.RescheduleProgrammeItem(future, singleWeek + 6, ReleaseSlateState.Postponed, "issue-capacity-unavailable");
			Check(manager.ProgrammeCashReserved(future.LabelId) == 0 && future.PreparedRecord == lp && future.PaidProduction == 321 &&
				future.IntendedWeek == singleWeek + 4, "postponement frees all unspent holds while keeping the master, paid cost and original intention");
			Check(manager.ProgrammePromotionCommittedPeak(future.LabelId) == 40, "postponed reservations cannot congest their old or new campaign window");
			future.ReservedMarketing = 70;
			Check(manager.ProgrammePromotionCommittedPeak(future.LabelId) == 70, "a campaign ending on the revised date does not overlap its replacement");
			manager.RescheduleProgrammeItem(future, singleWeek + 3, ReleaseSlateState.Scheduled, "observed-breakout-rush-review");
			Check(manager.ProgrammePromotionCommittedPeak(future.LabelId) == 40 && future.DueWeek == singleWeek + 3,
				"rushing a release also frees its prior holds before readmission");
			var preparedQuote = new ReleaseSlateItem { Id = "prepared-quote", LabelId = label.labelId, Format = ReleaseFormat.Single,
				State = ReleaseSlateState.Scheduled, DueWeek = singleWeek + 2, PreparedRecord = lp };
			state.Items.Add(preparedQuote);
			try {
				label.cashReserves = 1e9f;
				Check(manager.ReserveSlate(label, label.roster.First(), preparedQuote, TimeManager.Instance.CurrentDate, out _),
					"a prepared master can reserve its remaining launch costs");
				Check(preparedQuote.ReservedProduction == 0 && (!LabelPressingEconomics.Enabled || preparedQuote.ReservedPressing > 0),
					"readmission restores pressing without reserving paid production again");
				float hold = preparedQuote.ReservedMarketing + preparedQuote.ReservedPressing;
				float otherReserve = EconomicLabelLifecycle.CommitmentReserve(label, false) - hold;
				label.cashReserves = otherReserve + hold + 1;
				Check(manager.ReserveSlate(label, label.roster.First(), preparedQuote, TimeManager.Instance.CurrentDate, out _),
					"requotes exclude their own cash and promotion holds");
				if (preparedQuote.ReservedPressing > 0) {
					label.cashReserves = otherReserve + preparedQuote.ReservedMarketing + preparedQuote.ReservedPressing * .5f;
					Check(!manager.ReserveSlate(label, label.roster.First(), preparedQuote, TimeManager.Instance.CurrentDate, out _),
						"marketing cash alone cannot fund a prepared master's pressing commitment");
				}
			} finally { label.cashReserves = cash; }
			var idle = new ReleaseSlateItem { LabelId = label.labelId, IntendedWeek = singleWeek + 8, DueWeek = singleWeek + 8,
				State = ReleaseSlateState.Scheduled, PreparedRecord = lp, PaidProduction = 321, PreparedWeek = singleWeek - 1 };
			state.Items.Add(idle);
			state.Campaigns.Clear();
			float pool = label.GetStandardMarketingBudget() * Math.Max(1f, Math.Min(4f, label.releasesPerMonth));
			state.Campaigns.Add(new ProgrammeCampaign { LabelId = label.labelId, RecordId = "saturated-current-campaign", Week = singleWeek, Spend = pool });
			try {
				label.cashReserves = 1e9f;
				Check(manager.ReserveSlate(label, label.roster.First(), idle, TimeManager.Instance.CurrentDate, out _),
					"future promotion can fit while today's pool is full");
				Check(!manager.ReserveSlate(label, label.roster.First(), idle, TimeManager.Instance.CurrentDate, out string congestion, campaignWeek: singleWeek) && congestion == "promotion-congestion",
					"an early launch cannot spend its future booking in today's full pool");
				manager.RescheduleProgrammeItem(idle, idle.DueWeek, ReleaseSlateState.Postponed, "idle-issue-material-economics-funding-or-availability");
				Check(idle.ReservedProduction == 0 && idle.ReservedMarketing == 0 && idle.ReservedPressing == 0 && idle.PreparedRecord == lp &&
					idle.PaidProduction == 321 && idle.PreparedWeek == singleWeek - 1 && idle.DueWeek == singleWeek + 8,
					"failed early issue frees its holds while retaining its original date and paid master");
				Check(!LabelReleaseProgramme.CanUseIdleSlot(idle, singleWeek), "work deferred this week cannot retry in the idle pass");
				Check(LabelReleaseProgramme.CanUseIdleSlot(idle, singleWeek + 1), "deferred work can be reconsidered next week");
				idle.State = ReleaseSlateState.Scheduled;
				Check(LabelReleaseProgramme.CanUseIdleSlot(idle, singleWeek), "another ready future intention may use idle work");
				idle.State = ReleaseSlateState.AwaitingDelivery;
				Check(!LabelReleaseProgramme.CanUseIdleSlot(idle, singleWeek), "a pending LP project cannot be attempted again by the idle pass");
			} finally { label.cashReserves = cash; }
			GD.Print($"LABEL_RELEASE_PROGRAMME_CHECK passed checks={checks} studio={StudioParentLabels.Enabled}");
		} finally { manager.SetProgrammeForChecks(previous); LabelReleaseProgramme.SetForChecks(allocation, capacity); }
	}
}
