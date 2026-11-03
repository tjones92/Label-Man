using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class WritingWorkflowChecks {
	public static void Run() {
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
		Check(ProfessionalWritingMarket.Enabled && ReleaseJudgement.Selecting, "Workflow checks require market and repertoire selection");
		var label = ChartManager.Instance.GetAllLabels().First(l => l.IsActive && !l.isPlayerOwned && l.roster.Count > 0);
		label.cashReserves = 10000000;
		var artist = label.roster.First(a => ProfessionalWritingMarket.OwnedDelivery(label, a) == null);
		var date = TimeManager.Instance.CurrentDate;
		int week = ProfessionalWritingMarket.Week;
		AiRepertoireService.Judge(label, artist, date.year, week);
		foreach (var d in ProfessionalWritingMarket.State.Demos.Values.Where(d => d.ArtistId == artist.artistId && d.Status == WritingDemoStatus.Licensed).ToArray())
			ProfessionalWritingMarket.MarkRecorded(CompositionCatalogService.GetSong(d.SongId), artist.artistId);
		foreach (var p in ProfessionalWritingMarket.State.Partnerships.Values.Where(p => p.LabelId == label.labelId)) p.Hits = 0;
		var offer = ProfessionalWritingMarket.Offers(label, artist).First(o => o.Available);
		var plan = new ReleaseSlateItem { Id = "writing-workflow-check", LabelId = label.labelId, ArtistId = artist.artistId,
			ContractSequence = artist.contractSequence, Format = ReleaseFormat.Single, State = ReleaseSlateState.Proposed,
			ProposedWeek = week, ReadyWeek = week + offer.DeliveryWeeks + 1, IntendedWeek = week + offer.DeliveryWeeks + 2,
			DueWeek = week + offer.DeliveryWeeks + 2, IntendedDate = date.AddDays((offer.DeliveryWeeks + 2) * 7) };
		Check(ProfessionalWritingMarket.EligibleCommissionPlan(plan, label, artist, week, offer.DeliveryWeeks), "Real plan with writing lead is eligible");
		plan.ReadyWeek = week + offer.DeliveryWeeks - 1;
		Check(!ProfessionalWritingMarket.EligibleCommissionPlan(plan, label, artist, week, offer.DeliveryWeeks), "Too-late writing cannot book a plan");
		plan.ReadyWeek = week + LabelReleaseProgramme.Horizon + 1;
		Check(!ProfessionalWritingMarket.EligibleCommissionPlan(plan, label, artist, week, offer.DeliveryWeeks), "Distant intentions do not justify immediate writing");
		plan.ReadyWeek = week + offer.DeliveryWeeks + 1;
		plan.ContractSequence++;
		Check(!ProfessionalWritingMarket.EligibleCommissionPlan(plan, label, artist, week, offer.DeliveryWeeks), "Stale artist contract cannot authorize work");
		plan.ContractSequence--;
		plan.PreparedRecord = new Record();
		Check(!ProfessionalWritingMarket.EligibleCommissionPlan(plan, label, artist, week, offer.DeliveryWeeks), "An existing master needs no new commission");
		plan.PreparedRecord = null;
		plan.State = ReleaseSlateState.Cancelled;
		Check(!ProfessionalWritingMarket.EligibleCommissionPlan(plan, label, artist, week, offer.DeliveryWeeks), "Cancelled plan cannot authorize work");
		plan.State = ReleaseSlateState.Proposed;
		var programme = new ReleaseProgrammeState();
		CompetitorManager.Instance.SetProgrammeForChecks(programme);
		float cash = label.cashReserves;
		if (ProfessionalWritingMarket.PlannedAssignmentsEnabled) {
			Check(!ProfessionalWritingMarket.TryAiCommission(label, artist, offer.WriterId, date), "No autonomous purchase without a plan");
			Check(label.cashReserves == cash, "Rejected planless purchase charges nothing");
		}
		programme.Items.Add(plan);
		Check(ProfessionalWritingMarket.TryAiCommission(label, artist, offer.WriterId, date), "Book eligible paid work");
		var order = ProfessionalWritingMarket.State.Orders.Last();
		Check(order.ReleasePlanId == (ProfessionalWritingMarket.PlannedAssignmentsEnabled ? plan.Id : null), "Order links to its actual plan only in planned arm");
		Check(ProfessionalWritingMarket.AwaitingCommission(plan) == ProfessionalWritingMarket.PlannedAssignmentsEnabled, "Linked pending work blocks premature master preparation");
		Check(!AiRepertoireService.OfferWritingDelivery(label, artist, order, date), "Pending work cannot enter the shortlist");
		ProfessionalWritingMarket.Deliver(order.DueDate.AddDays(-1));
		Check(order.Status == WritingOrderStatus.Pending, "Writing retains its real delivery time");
		ProfessionalWritingMarket.Advance(order.DeliveryWeek, order.DueDate);
		Check(order.Status == WritingOrderStatus.Delivered && !ProfessionalWritingMarket.AwaitingCommission(plan), "Delivery releases preparation wait");
		Check(Math.Abs(cash - label.cashReserves - order.Fee) < 1, "Delivery offering does not add a license fee");
		var opts = SaveGameService.TestJsonOptions;
		string saved = JsonSerializer.Serialize(ProfessionalWritingMarket.State, opts);
		var restored = JsonSerializer.Deserialize<ProfessionalWritingMarketState>(saved, opts);
		Check(saved == JsonSerializer.Serialize(restored, opts), "Policy, plan link and offer marker serialize exactly");
		restored.PlannedAssignments = !restored.PlannedAssignments;
		bool refused = false;
		try { ProfessionalWritingMarket.RequireSaved(restored); } catch (InvalidOperationException) { refused = true; }
		Check(refused, "Resume cannot silently change workflow policy");
		ProfessionalWritingMarket.Restore(JsonSerializer.Deserialize<ProfessionalWritingMarketState>(saved, opts));
		order = ProfessionalWritingMarket.State.Orders.Single(o => o.Id == order.Id);
		if (ProfessionalWritingMarket.DeliveryOffersEnabled) {
			Check(order.DeliveryOffered && AiRepertoireService.Acts[artist.artistId].Candidates.Any(c => c.SongId == order.SongId), "Completed assignment enters its intended repertoire");
			int count = AiRepertoireService.Acts[artist.artistId].Candidates.Count;
			Check(!AiRepertoireService.OfferWritingDelivery(label, artist, order, order.DueDate), "Saved marker prevents duplicate offers");
			Check(count == AiRepertoireService.Acts[artist.artistId].Candidates.Count, "Replay creates no candidate or new read");
			var weak = CompositionCatalogService.AllSongs.First(s => s.originKind == SongOriginKind.ProfessionalOffice && ProfessionalWritingMarket.CanLicense(s, label, artist) && s.songId != order.SongId);
			Check(ProfessionalWritingMarket.TryLicense(weak, label, artist, out _), "Create paid weak-demo fixture");
			weak.compositionQuality = weak.commercialHook = weak.lyricQuality = 0;
			var act = AiRepertoireService.Acts[artist.artistId];
			act.Candidates.Clear();
			for (int i = 0; i < ReleaseJudgement.Parameters.ShortlistCap; i++) act.Candidates.Add(new ShadowCandidate {
				Id = "strong-workflow-check-" + i, ArtistId = artist.artistId, Source = SongMaterialSource.ArtistWritten,
				Craft = 1, Hook = 1, Lyric = 1, Originality = 1, EvidenceVersion = 1,
				Readiness = ShadowReadiness.Recordable, Disposition = ShadowDisposition.Active });
			var disappointing = new WritingOrder { Id = "weak-workflow-delivery", ArtistId = artist.artistId, LabelId = label.labelId,
				SongId = weak.songId, WriterId = ProfessionalWritingMarket.SupplierFor(weak), Status = WritingOrderStatus.Delivered };
			ProfessionalWritingMarket.State.Orders.Add(disappointing);
			Check(AiRepertoireService.OfferWritingDelivery(label, artist, disappointing, order.DueDate), "Disappointing work is offered, not silently replaced");
			var candidate = act.Candidates.Single(c => c.SongId == weak.songId);
			Check(candidate.Disposition == ShadowDisposition.Archived, "Ordinary perceived shortlist selection can decline a paid delivery");
			Check(ProfessionalWritingMarket.State.Demos[weak.songId].Status == WritingDemoStatus.Licensed, "A declined offer is not forced into recording");
			Check(ProfessionalWritingMarket.WasDeliveryOffered(weak.songId, artist.artistId), "Rejected delivery remains marked against random reoffering");
		}
		GD.Print("WRITING_WORKFLOW_CHECK_PASS: plan feasibility, paid delayed work, preparation wait, one offer, perceived rejection, no extra fee and saved policy.");
	}
}
