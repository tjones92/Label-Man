using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public partial class ChartAuditRunner {
	private StreamWriter professionalWritingSuppliers, professionalWritingOrders, professionalWritingPartnerships;
	private StreamWriter professionalArrivalWriter;
	private StreamWriter commissionPlanWriter, commissionEligibleWriter;
	private StreamWriter commissionReminderWriter;
	private StreamWriter planReminderPilotWriter;
	private readonly Dictionary<string, List<int>> commissionRefusals = new();
	private void OpenProfessionalWritingOutputs(string outputDirectory) {
		OpenWritingConcentrationOutputs(outputDirectory);
		if (SimulationArguments.Get().Contains("--material-funnel-audit")) {
			professionalArrivalWriter = CreateWriter(Path.Combine(outputDirectory, runName + "-professional-arrivals.csv"));
			professionalArrivalWriter.WriteLine("week,labelId,artistId,career,genre,ability,craft,provenance,activeCandidates,outcome");
			AiRepertoireService.OnProfessionalArrivalAudit += WriteProfessionalArrival;
			commissionPlanWriter = CreateWriter(Path.Combine(outputDirectory, runName + "-commission-plan-diagnostic.csv"));
			commissionPlanWriter.WriteLine("week,labelId,artistId,primary,closed,preparing,material,lead,horizon");
			commissionEligibleWriter = CreateWriter(Path.Combine(outputDirectory, runName + "-commission-eligible-plans.csv"));
			commissionEligibleWriter.WriteLine("week,labelId,artistId,slotId");
			commissionReminderWriter = CreateWriter(Path.Combine(outputDirectory, runName + "-commission-reminder-gates.csv"));
			commissionReminderWriter.WriteLine("week,marketWeek,labelId,artistId,slotId,owned,pending,recent,writerId,deliveryWeeks,reserve,selectedPlanId,deliveryActive,clientMatches,activeCandidates");
		}
		if (!ProfessionalWritingMarket.Enabled || !SimulationArguments.Get().Contains("--professional-writing-market-audit")) return;
		if (ProfessionalWritingMarket.PlanRemindersEnabled) {
			planReminderPilotWriter = CreateWriter(Path.Combine(outputDirectory, runName + "-plan-reminder-events.csv"));
			planReminderPilotWriter.WriteLine("week,labelId,artistId,contractSequence,createdWeek,action,reason,slotId,orderId");
			ProfessionalWritingMarket.OnPlanReminderAudit += WritePlanReminderPilot;
		}
		professionalWritingSuppliers = CreateWriter(Path.Combine(outputDirectory, runName + "-writing-suppliers.csv"));
		professionalWritingOrders = CreateWriter(Path.Combine(outputDirectory, runName + "-writing-orders.csv"));
		professionalWritingPartnerships = CreateWriter(Path.Combine(outputDirectory, runName + "-writing-partnerships.csv"));
		professionalWritingSuppliers.WriteLine("week,writerId,reputation,assignments,hits,pending,available,shelved,licensed,recorded,contract,labelId,retainer,busyUntilWeek,nextWritingWeek,commissionFees,licenseFees,retainerFees");
		professionalWritingOrders.WriteLine("week,id,writerId,labelId,artistId,startedWeek,deliveryWeek,status,songId,fee,releasePlanId,deliveryOffered");
		professionalWritingPartnerships.WriteLine("week,writerId,labelId,artistId,assignments,recordings,hits,lastAssignmentWeek");
	}
	private void WritePlanReminderPilot(int week, string label, string artist, int contract, int created, string action, string reason, string slot, string order) =>
		planReminderPilotWriter.WriteLine(string.Join(",", week, Csv(label), Csv(artist), contract, created, Csv(action), Csv(reason), Csv(slot), Csv(order)));
	private void WriteProfessionalArrival(int week, AILabel label, SimulatedArtist artist, string provenance, int active, string outcome) {
		professionalArrivalWriter.WriteLine(string.Join(",", week, Csv(label.labelId), Csv(artist.artistId), artist.careerState,
			artist.primaryGenre, DF(artist.songwritingAbility), DF(SongMaterialSelectionService.ArtistWriterCraft(artist)), Csv(provenance), active, outcome));
		if (outcome != "commission-refused:no-plan-minimum-delivery") return;
		if (provenance == "arrival" && week > 0) {
			string key = label.labelId + "\0" + artist.artistId;
			if (!commissionRefusals.TryGetValue(key, out var dates)) commissionRefusals[key] = dates = new List<int>();
			dates.Add(week);
		}
		var plans = CompetitorManager.Instance.GetReleaseSlate().Where(i => i.LabelId == label.labelId && i.ArtistId == artist.artistId &&
			i.ContractSequence == artist.contractSequence && i.Format == ReleaseFormat.Single).ToList();
		bool Closed(ReleaseSlateItem i) => !LabelReleaseProgramme.IsOpen(i);
		bool Preparing(ReleaseSlateItem i) => i.PreparedRecord != null || i.PreparedWeek != null || CompetitorManager.Instance.IsPreparingProgrammeItem(i);
		bool Material(ReleaseSlateItem i) => i.MaterialSearch?.State is ProgrammeMaterialState.Pending or ProgrammeMaterialState.Ready;
		bool Lead(ReleaseSlateItem i) => i.ReadyWeek < week + 2 || i.IntendedWeek < week + 2;
		bool Horizon(ReleaseSlateItem i) => i.ReadyWeek > week + LabelReleaseProgramme.Horizon;
		var remaining = plans.Where(i => !Closed(i)).ToList();
		string primary = plans.Count == 0 ? "no-matching-plan" : remaining.Count == 0 ? "closed" : null;
		if (primary == null) { remaining = remaining.Where(i => !Preparing(i)).ToList(); if (remaining.Count == 0) primary = "preparing-or-prepared"; }
		if (primary == null) { remaining = remaining.Where(i => !Material(i)).ToList(); if (remaining.Count == 0) primary = "material-pending-or-ready"; }
		if (primary == null) { remaining = remaining.Where(i => !Lead(i)).ToList(); if (remaining.Count == 0) primary = "insufficient-lead"; }
		if (primary == null) { remaining = remaining.Where(i => !Horizon(i)).ToList(); primary = remaining.Count == 0 ? "outside-horizon" : "unexpected-eligible"; }
		commissionPlanWriter.WriteLine(string.Join(",", week, Csv(label.labelId), Csv(artist.artistId), primary,
			plans.Any(Closed) ? 1 : 0, plans.Any(Preparing) ? 1 : 0, plans.Any(Material) ? 1 : 0, plans.Any(Lead) ? 1 : 0, plans.Any(Horizon) ? 1 : 0));
	}
	private void CaptureProfessionalWritingWeek(int week) {
		CaptureWritingConcentrationWeek(week);
		var audited = new Dictionary<string, (bool owned, bool pending, bool recent, WritingOffer offer, bool reserve, ReleaseSlateItem plan, bool deliveryActive, bool clientMatches)>();
		if (commissionEligibleWriter != null) foreach (var item in CompetitorManager.Instance.GetReleaseSlate()) {
			var label = CompetitorManager.Instance.GetLabel(item.LabelId);
			var artist = label?.roster.Find(a => a.artistId == item.ArtistId);
			if (ProfessionalWritingMarket.EligibleCommissionPlan(item, label, artist, week, 2)) {
				commissionEligibleWriter.WriteLine(string.Join(",", week, Csv(item.LabelId), Csv(item.ArtistId), Csv(item.Id)));
				string key = item.LabelId + "\0" + item.ArtistId;
				if (ProfessionalWritingMarket.Enabled && commissionRefusals.TryGetValue(key, out var refused) && refused.Any(w => week > w && week <= w + 8)) {
					if (!audited.TryGetValue(key, out var a)) audited[key] = a = ProfessionalWritingMarket.ObserveAiCommission(label, artist, TimeManager.Instance.CurrentDate);
					int active = AiRepertoireService.Acts.TryGetValue(artist.artistId, out var repertoire) ?
						repertoire.Candidates.Count(c => c.Disposition == ShadowDisposition.Active) : -1;
					commissionReminderWriter.WriteLine(string.Join(",", week, ProfessionalWritingMarket.Week, Csv(item.LabelId), Csv(item.ArtistId), Csv(item.Id),
						a.owned ? 1 : 0, a.pending ? 1 : 0, a.recent ? 1 : 0, Csv(a.offer?.WriterId), a.offer?.DeliveryWeeks ?? 0,
						a.reserve ? 1 : 0, Csv(a.plan?.Id), a.deliveryActive ? 1 : 0, a.clientMatches ? 1 : 0, active));
				}
			}
		}
		if (professionalWritingSuppliers == null) return;
		var state = ProfessionalWritingMarket.State;
		var demos = state.Demos.Values.ToLookup(d => d.WriterId);
		var pending = state.Orders.Where(o => o.Status == WritingOrderStatus.Pending).ToLookup(o => o.WriterId);
		foreach (var s in state.Suppliers.Values.OrderBy(s => s.WriterId, StringComparer.Ordinal)) {
			var contract = state.Contracts.LastOrDefault(c => c.WriterId == s.WriterId && !c.Ended && c.EndWeek > state.LastWeek);
			professionalWritingSuppliers.WriteLine(string.Join(",", week, Csv(s.WriterId), DF(s.Reputation), s.Assignments, s.Hits, pending[s.WriterId].Count(),
				demos[s.WriterId].Count(d => d.Status == WritingDemoStatus.Available), demos[s.WriterId].Count(d => d.Status == WritingDemoStatus.Shelved),
				demos[s.WriterId].Count(d => d.Status == WritingDemoStatus.Licensed), demos[s.WriterId].Count(d => d.Status == WritingDemoStatus.Recorded),
				contract?.Kind.ToString() ?? "Freelance", Csv(contract?.LabelId), DF(contract?.Retainer ?? 0), s.BusyUntilWeek, s.NextWritingWeek,
				DF(s.CommissionFees), DF(s.LicenseFees), DF(s.RetainerFees)));
		}
		foreach (var o in state.Orders.OrderBy(o => o.Id, StringComparer.Ordinal))
			professionalWritingOrders.WriteLine(string.Join(",", week, Csv(o.Id), Csv(o.WriterId), Csv(o.LabelId), Csv(o.ArtistId), o.StartedWeek, o.DeliveryWeek, o.Status, Csv(o.SongId), DF(o.Fee), Csv(o.ReleasePlanId), o.DeliveryOffered));
		foreach (var p in state.Partnerships.Values.OrderBy(p => p.WriterId).ThenBy(p => p.LabelId).ThenBy(p => p.ArtistId))
			professionalWritingPartnerships.WriteLine(string.Join(",", week, Csv(p.WriterId), Csv(p.LabelId), Csv(p.ArtistId), p.Assignments, p.Recordings, p.Hits, p.LastAssignmentWeek));
	}
	private void CloseProfessionalWritingOutputs() {
		ProfessionalWritingMarket.OnPlanReminderAudit -= WritePlanReminderPilot;
		planReminderPilotWriter?.Dispose(); planReminderPilotWriter = null;
		AiRepertoireService.OnProfessionalArrivalAudit -= WriteProfessionalArrival;
		professionalArrivalWriter?.Dispose(); professionalArrivalWriter = null;
		commissionPlanWriter?.Dispose(); commissionPlanWriter = null;
		commissionEligibleWriter?.Dispose(); commissionEligibleWriter = null;
		commissionReminderWriter?.Dispose(); commissionReminderWriter = null;
		commissionRefusals.Clear();
		CloseWritingConcentrationOutputs();
		professionalWritingSuppliers?.Dispose(); professionalWritingSuppliers = null;
		professionalWritingOrders?.Dispose(); professionalWritingOrders = null;
		professionalWritingPartnerships?.Dispose(); professionalWritingPartnerships = null;
	}
}
