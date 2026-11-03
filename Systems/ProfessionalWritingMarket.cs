using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Finite writing capacity, dated access and paid assignments. All randomness is keyed;
/// quotes use public reputation and workload, never the hidden quality of the song being sold.</summary>
public static class ProfessionalWritingMarket {
	public static readonly bool Enabled = SimulationArguments.Get().Contains("--professional-writing-market");
	public const string PlannedSwitch = "--writing-market-planned-assignments", DeliverySwitch = "--writing-market-delivery-offers";
	public static readonly bool PlannedAssignmentsEnabled = SimulationArguments.Get().Contains(PlannedSwitch);
	public static readonly bool DeliveryOffersEnabled = SimulationArguments.Get().Contains(DeliverySwitch);
	public const string ReminderSwitch = "--writing-market-plan-reminders";
	public static readonly bool PlanRemindersEnabled = SimulationArguments.Get().Contains(ReminderSwitch);
	public static event Action<int, string, string, int, int, string, string, string, string> OnPlanReminderAudit;
	public const int ContractWeeks = 12, BillingWeeks = 4, LicenseHoldWeeks = 12, PriorityWeeks = 2;
	public static ProfessionalWritingMarketState State { get; private set; }
	public static int Week => Math.Max(State?.LastWeek ?? 0, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
	private static IReadOnlyList<ProfessionalSongwriter> Writers => CompositionCatalogService.ProfessionalWriters;
	private static ProfessionalSongwriter Writer(string id) => Writers.FirstOrDefault(w => w.writerId == id);
	private static float Unit(string key) => RepertoireTaxonomy.Unit(BandLife.WorldSeed + "|writing-market|" + key);
	internal static float AccessOrder(string artistId, int week) => Unit("access-order|" + week + "|" + artistId);
	private static bool Active(ProfessionalSongwriter w, int year) => w != null && year >= w.activeStartYear && year <= w.activeEndYear;
	public static string SupplierId(string writerId) {
		var writer = Writer(writerId);
		return writer != null && !string.IsNullOrEmpty(writer.teamPartnerId) && Writer(writer.teamPartnerId)?.teamPartnerId == writerId &&
			string.CompareOrdinal(writer.teamPartnerId, writerId) < 0 ? writer.teamPartnerId : writerId;
	}
	public static string SupplierFor(SongComposition song) => SupplierId(song?.credits.FirstOrDefault(c => c.writerType == WriterEntityType.ProfessionalSongwriter)?.writerId);
	private static IEnumerable<ProfessionalSongwriter> Members(string id) {
		var writer = Writer(id);
		if (writer == null) yield break;
		yield return writer;
		var partner = Writer(writer.teamPartnerId);
		if (partner?.teamPartnerId == id) yield return partner;
	}
	private static bool ActiveSupplier(string id, int year) => Members(id).Any() && Members(id).All(w => Active(w, year));
	private static string PartnershipKey(string label, string artist, string writer) => label + "|" + artist + "|" + writer;
	private static ProfessionalWritingPartnership Partnership(string label, string artist, string writer, bool create = false) {
		if (State == null || writer == null) return null;
		string key = PartnershipKey(label, artist, writer);
		if (!State.Partnerships.TryGetValue(key, out var p) && create)
			State.Partnerships[key] = p = new ProfessionalWritingPartnership { LabelId = label, ArtistId = artist, WriterId = writer };
		return p;
	}
	public static string SupplierName(string id) => string.Join(" & ", Members(SupplierId(id)).Select(w => w.name));
	private static WritingContract Contract(string writer, int week) => State?.Contracts.LastOrDefault(c => c.WriterId == writer && !c.Ended && c.StartWeek <= week && week < c.EndWeek);
	public static WritingContract ContractFor(string writer, string label) {
		var c = Contract(SupplierId(writer), Week);
		return c?.LabelId == label ? c : null;
	}

	internal static void Initialize(int year) {
		if (PlanRemindersEnabled && (!PlannedAssignmentsEnabled || !DeliveryOffersEnabled || !LabelReleaseProgramme.MaterialSearchEnabled))
			throw new InvalidOperationException("Plan reminders require planned assignments, delivery offers and anticipatory material search.");
		if ((PlannedAssignmentsEnabled || DeliveryOffersEnabled) && (!Enabled || !ReleaseJudgement.Selecting) ||
			PlannedAssignmentsEnabled && !LabelReleaseProgramme.AllocationEnabled)
			throw new InvalidOperationException("Writing workflow experiments require the market and perceived/fit repertoire selection; planned assignments also require a release slate.");
		State = Enabled ? new ProfessionalWritingMarketState { PlannedAssignments = PlannedAssignmentsEnabled, DeliveryOffers = DeliveryOffersEnabled,
			PlanReminders = PlanRemindersEnabled, Reminders = PlanRemindersEnabled ? new List<WritingPlanReminder>() : null } : null;
		if (!Enabled) return;
		foreach (var writer in Writers.OrderBy(w => w.writerId, StringComparer.Ordinal)) {
			string id = SupplierId(writer.writerId);
			if (State.Suppliers.ContainsKey(id)) continue;
			State.Suppliers[id] = new WritingSupplier { WriterId = id,
				Reputation = Math.Clamp(.3f + .025f * Math.Max(0, year - writer.activeStartYear) + .25f * Unit(id + "|reputation"), .2f, .8f),
				NextWritingWeek = 1 + (int)(Unit(id + "|opening-capacity") * 5) };
		}
		foreach (var song in CompositionCatalogService.AllSongs.Where(s => s.originKind == SongOriginKind.ProfessionalOffice)) {
			string id = SupplierFor(song);
			if (id != null && State.Suppliers.ContainsKey(id)) State.Demos[song.songId] = new WritingDemo { SongId = song.songId, WriterId = id };
		}
	}

	public static void RequireSaved(ProfessionalWritingMarketState saved) {
		if ((saved != null) != Enabled || saved != null && (saved.Version != 1 || saved.PlannedAssignments != PlannedAssignmentsEnabled || saved.DeliveryOffers != DeliveryOffersEnabled || saved.PlanReminders != PlanRemindersEnabled || PlanRemindersEnabled && saved.Reminders == null))
			throw new InvalidOperationException("Resume with the saved writing-market and workflow switches. Start a new world to change writing-market policy.");
	}
	internal static void Restore(ProfessionalWritingMarketState saved) { RequireSaved(saved); State = saved; }

	private static bool Affordable(AILabel label, float fee) => label?.IsActive == true && float.IsFinite(fee) && fee >= 0 &&
		label.cashReserves - (label.isPlayerOwned ? 0 : EconomicLabelLifecycle.CommitmentReserve(label, false)) >= fee;
	private static void Pay(AILabel label, float fee) {
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.RecordExpense(label, fee, LabelCashKind.OtherOperating);
		else { LabelCompanyAccounts.ChangeCash(label, -fee, LabelCashKind.OtherOperating); label.monthlyExpenses += fee; }
	}
	private static float Rate(string writer, bool commission) {
		if (State == null || writer == null || !State.Suppliers.TryGetValue(writer, out var s)) return float.PositiveInfinity;
		// All inputs are observable career evidence or outstanding demand, independent of the song's true hook.
		int workload = State.Orders.Count(o => o.WriterId == writer && o.Status == WritingOrderStatus.Pending);
		float demand = Math.Min(1, s.Assignments / 20f);
		float fee = (commission ? 100 : 55) * (1 + 2.5f * s.Reputation + .5f * demand + .25f * workload);
		return (float)Math.Ceiling(fee / 5) * 5;
	}
	public static float LicenseFee(SongComposition song, AILabel label, SimulatedArtist artist) {
		if (!Enabled || State == null || song == null) return MaterialPreparationService.CommissionFee;
		if (State.Demos.TryGetValue(song.songId, out var d) && d.LabelId == label?.labelId && d.ArtistId == artist?.artistId &&
			d.Status is WritingDemoStatus.Licensed or WritingDemoStatus.Recorded && (d.Status == WritingDemoStatus.Recorded || d.HoldUntilWeek >= Week)) return 0;
		return Rate(SupplierFor(song), false);
	}
	public static IReadOnlyList<WritingOffer> Offers(AILabel label, SimulatedArtist artist) {
		if (!Enabled || State == null || label == null || artist == null) return Array.Empty<WritingOffer>();
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		return State.Suppliers.Values.Where(s => ActiveSupplier(s.WriterId, year)).Select(s => {
			var c = Contract(s.WriterId, Week);
			var p = Partnership(label.labelId, artist.artistId, s.WriterId);
			float fee = Rate(s.WriterId, true);
			return new WritingOffer { WriterId = s.WriterId, Name = SupplierName(s.WriterId), Reputation = s.Reputation,
				CommissionFee = c?.LabelId == label.labelId ? fee * (c.Kind == WritingContractKind.Exclusive ? .2f : .8f) : fee,
				LicenseFee = Rate(s.WriterId, false), PreferredRetainer = fee * .75f, ExclusiveRetainer = fee * 2.5f,
				DeliveryWeeks = Members(s.WriterId).Average(w => w.reliability) >= .65f ? 2 : 3,
				PreviousAssignments = p?.Assignments ?? 0, PreviousHits = p?.Hits ?? 0,
				Available = s.BusyUntilWeek <= Week && (c == null || c.LabelId == label.labelId || c.Kind != WritingContractKind.Exclusive) };
		}).OrderByDescending(o => Contract(o.WriterId, Week)?.LabelId == label.labelId)
			.ThenByDescending(o => o.PreviousHits * .25f + o.PreviousAssignments * .025f + Fit(o.WriterId, artist.primaryGenre) * .5f + o.Reputation * .2f)
			.ThenBy(o => o.WriterId, StringComparer.Ordinal).ToArray();
	}
	private static float Fit(string writer, Genre genre) => Members(writer).Average(w => w.primaryGenre == genre ? 1f : w.secondaryGenre == genre ? .85f : .4f + .4f * w.versatility);

	public static bool CanLicense(SongComposition song, AILabel label, SimulatedArtist artist = null) {
		if (!Enabled || song?.originKind != SongOriginKind.ProfessionalOffice || State == null) return true;
		if (label == null || !State.Demos.TryGetValue(song.songId, out var d) || d.Status == WritingDemoStatus.Shelved || song.originYear > (TimeManager.Instance?.CurrentDate.year ?? 1960)) return false;
		if (d.Status is WritingDemoStatus.Licensed or WritingDemoStatus.Recorded) {
			if (d.Status == WritingDemoStatus.Recorded || d.HoldUntilWeek >= Week)
				return d.LabelId == label.labelId && (artist == null || d.ArtistId == artist.artistId);
		}
		if (d.AvailableWeek > Week || d.PriorityUntilWeek > Week && d.LabelId != label.labelId ||
			d.ArtistId != null && d.HoldUntilWeek >= Week && artist != null && d.ArtistId != artist.artistId) return false;
		return Affordable(label, LicenseFee(song, label, artist));
	}
	public static IReadOnlyList<SongComposition> Visible(IReadOnlyList<SongComposition> pool, AILabel label, SimulatedArtist artist = null) {
		if (!Enabled || State == null || pool == null) return pool;
		return pool.Where(s => CanLicense(s, label, artist) && (!State.Demos.TryGetValue(s.songId, out var d) || d.Status != WritingDemoStatus.Recorded)).ToArray();
	}
	internal static SongComposition OwnedDelivery(AILabel label, SimulatedArtist artist) {
		if (!Enabled || State == null || label == null || artist == null) return null;
		return State.Demos.Values.Where(d => d.Status == WritingDemoStatus.Licensed && d.LabelId == label.labelId && d.ArtistId == artist.artistId &&
			d.HoldUntilWeek >= Week && d.AvailableWeek <= Week).OrderBy(d => d.AvailableWeek).ThenBy(d => d.SongId, StringComparer.Ordinal)
			.Select(d => CompositionCatalogService.GetSong(d.SongId)).FirstOrDefault();
	}
	public static bool TryLicense(SongComposition song, AILabel label, SimulatedArtist artist, out string message) {
		message = "";
		if (!Enabled || song?.originKind != SongOriginKind.ProfessionalOffice) return true;
		if (artist == null || artist.labelId != label?.labelId || !CanLicense(song, label, artist)) { message = "That song is no longer available to this act."; return false; }
		var existing = State.Demos[song.songId];
		if (existing.LabelId == label.labelId && existing.ArtistId == artist.artistId &&
			(existing.Status == WritingDemoStatus.Recorded || existing.Status == WritingDemoStatus.Licensed && existing.HoldUntilWeek >= Week)) {
			message = "This act already controls the first recording of this song."; return true;
		}
		float fee = LicenseFee(song, label, artist);
		if (fee > 0) Pay(label, fee);
		var demo = State.Demos[song.songId];
		State.Suppliers[demo.WriterId].LicenseFees += fee;
		bool first = demo.Status != WritingDemoStatus.Licensed && demo.Status != WritingDemoStatus.Recorded || demo.HoldUntilWeek < Week;
		demo.LabelId = label.labelId; demo.ArtistId = artist.artistId; demo.FeePaid += fee;
		if (demo.Status != WritingDemoStatus.Recorded) { demo.Status = WritingDemoStatus.Licensed; demo.HoldUntilWeek = Week + LicenseHoldWeeks; }
		if (first) {
			Partnership(label.labelId, artist.artistId, demo.WriterId, true).Assignments++;
			State.Suppliers[demo.WriterId].Assignments++;
		}
		message = $"Licensed \"{song.title}\" for ${fee:N0}; first recording reserved for {LicenseHoldWeeks} weeks. Publishing stays with its current owner.";
		return true;
	}

	public static bool TryCommission(AILabel label, SimulatedArtist artist, string writerId, GameDate date, out WritingOrder order, out string message, string coWriterPersonId = null) {
		order = null; message = "No writing capacity is available right now.";
		if (!Enabled || State == null || artist == null || artist.labelId != label?.labelId ||
			coWriterPersonId != null && artist.members?.Any(m => m.isActive && m.personId == coWriterPersonId) != true ||
			State.Orders.Any(o => o.ArtistId == artist.artistId && o.Status == WritingOrderStatus.Pending)) return false;
		writerId = SupplierId(writerId);
		var offer = Offers(label, artist).FirstOrDefault(o => o.WriterId == writerId);
		if (offer == null || !offer.Available || !ActiveSupplier(writerId, date.AddDays(offer.DeliveryWeeks * 7).year)) return false;
		if (!Affordable(label, offer.CommissionFee)) { message = $"The writing fee is ${offer.CommissionFee:N0}; available cash is too low."; return false; }
		Pay(label, offer.CommissionFee);
		order = new WritingOrder { Id = "writing-order-" + ++State.Sequence, WriterId = writerId, LabelId = label.labelId, ArtistId = artist.artistId,
			Genre = artist.primaryGenre, CoWriterPersonId = coWriterPersonId, StartedWeek = Week, DeliveryWeek = Week + offer.DeliveryWeeks, DueDate = date.AddDays(offer.DeliveryWeeks * 7), Fee = offer.CommissionFee };
		State.Orders.Add(order);
		var supplier = State.Suppliers[writerId]; supplier.BusyUntilWeek = order.DeliveryWeek; supplier.Assignments++;
		supplier.CommissionFees += order.Fee;
		var p = Partnership(label.labelId, artist.artistId, writerId, true); p.Assignments++; p.LastAssignmentWeek = Week;
		message = $"{offer.Name} will deliver a new song for {artist.stageName} by {order.DueDate.ToHeadlineString()} (${order.Fee:N0}). The fee pays for the work; it does not guarantee a usable song.";
		return true;
	}

	// Buying work is a production decision, not a side effect of every unsuccessful demo read.
	// Keep player bookings unrestricted; autonomous clients first use what they already paid for.
	internal static bool CanAiCommission(AILabel label, SimulatedArtist artist) {
		if (!Enabled || State == null || label == null || label.isPlayerOwned || artist == null ||
			OwnedDelivery(label, artist) != null ||
			State.Orders.Any(o => o.ArtistId == artist.artistId && (o.Status == WritingOrderStatus.Pending || Week - o.StartedWeek < 8))) return false;
		return !PlannedAssignmentsEnabled || CommissionPlan(label, artist, 2) != null;
	}
	internal static bool EligibleCommissionPlan(ReleaseSlateItem item, AILabel label, SimulatedArtist artist, int week, int deliveryWeeks) =>
		item != null && label != null && artist != null && item.LabelId == label.labelId && item.ArtistId == artist.artistId &&
		item.ContractSequence == artist.contractSequence && item.Format == ReleaseFormat.Single && LabelReleaseProgramme.IsOpen(item) &&
		item.PreparedRecord == null && item.PreparedWeek == null && CompetitorManager.Instance?.IsPreparingProgrammeItem(item) != true &&
		item.ReadyWeek >= week + deliveryWeeks && item.ReadyWeek <= week + LabelReleaseProgramme.Horizon &&
		item.IntendedWeek >= week + deliveryWeeks &&
		item.MaterialSearch?.State is not (ProgrammeMaterialState.Pending or ProgrammeMaterialState.Ready);
	private static ReleaseSlateItem CommissionPlan(AILabel label, SimulatedArtist artist, int deliveryWeeks) =>
		CompetitorManager.Instance?.GetReleaseSlate().Where(i => EligibleCommissionPlan(i, label, artist, Week, deliveryWeeks))
			.OrderBy(i => i.ReadyWeek).ThenBy(i => i.Id, StringComparer.Ordinal).FirstOrDefault();
	internal static void RememberPlanlessArrival(AILabel label, SimulatedArtist artist, int week) {
		if (!PlanRemindersEnabled || State?.Reminders == null || label == null || artist == null || week <= 0) return;
		if (CompetitorManager.Instance.GetReleaseSlate().Any(i => i.LabelId == label.labelId && i.ArtistId == artist.artistId &&
			i.ContractSequence == artist.contractSequence && i.Format == ReleaseFormat.Single)) return;
		if (State.Reminders.Any(r => r.LabelId == label.labelId && r.ArtistId == artist.artistId && r.ContractSequence == artist.contractSequence && r.ExpiresWeek >= week)) return;
		State.Reminders.Add(new WritingPlanReminder { LabelId = label.labelId, ArtistId = artist.artistId, ContractSequence = artist.contractSequence,
			CreatedWeek = week, ExpiresWeek = week + 8 });
		OnPlanReminderAudit?.Invoke(week, label.labelId, artist.artistId, artist.contractSequence, week, "created", "no-matching-plan", null, null);
	}
	internal static void ReconsiderPlanReminders(AILabel label, GameDate date, int week) {
		if (!PlanRemindersEnabled || State?.Reminders == null || label == null) return;
		foreach (var reminder in State.Reminders.Where(r => r.LabelId == label.labelId && r.CreatedWeek < week && week <= r.ExpiresWeek)
			.OrderBy(r => r.ArtistId, StringComparer.Ordinal).ThenBy(r => r.CreatedWeek).ToArray()) {
			var artist = label.roster.FirstOrDefault(a => a.artistId == reminder.ArtistId);
			if (label.IsActive != true || label.isPlayerOwned || artist == null || artist.labelId != label.labelId || artist.contractSequence != reminder.ContractSequence ||
				!GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist)) {
				State.Reminders.Remove(reminder);
				OnPlanReminderAudit?.Invoke(week, reminder.LabelId, reminder.ArtistId, reminder.ContractSequence, reminder.CreatedWeek, "discarded", "client-unavailable", null, null);
				continue;
			}
			var plan = CompetitorManager.Instance.GetReleaseSlate().Where(i => !i.WritingReminderAttempted && !i.WritingReminderMaterialReviewed && i.MaterialSearch == null &&
				EligibleCommissionPlan(i, label, artist, Week, 2)).OrderBy(i => i.ReadyWeek).ThenBy(i => i.Id, StringComparer.Ordinal).FirstOrDefault();
			if (plan == null) continue;
			State.Reminders.Remove(reminder);
			plan.WritingReminderAttempted = true;
			string reason;
			string orderId = null;
			if (!AiRepertoireService.Acts.TryGetValue(artist.artistId, out var act)) reason = "untracked-repertoire";
			else if (act.Candidates.Count(c => c.Disposition == ShadowDisposition.Active) >= 2) reason = "candidate-count";
			else {
				var offer = Offers(label, artist).FirstOrDefault(o => o.Available);
				if (offer == null) reason = "no-available-writer";
				else if (CommissionPlan(label, artist, offer.DeliveryWeeks)?.Id != plan.Id) reason = "delivery-plan-mismatch";
				else if (TryAiCommission(label, artist, offer.WriterId, date, out reason)) {
					var order = State.Orders.Last(); order.PlanReminder = true; orderId = order.Id;
				}
			}
			OnPlanReminderAudit?.Invoke(week, label.labelId, artist.artistId, reminder.ContractSequence, reminder.CreatedWeek, "attempt", reason, plan.Id, orderId);
		}
	}
	// Read-only audit of the existing AI gates; it does not reserve or book anything.
	internal static (bool owned, bool pending, bool recent, WritingOffer offer, bool reserve, ReleaseSlateItem plan, bool deliveryActive, bool clientMatches) ObserveAiCommission(AILabel label, SimulatedArtist artist, GameDate date) {
		var offer = Offers(label, artist).FirstOrDefault(o => o.Available);
		return (OwnedDelivery(label, artist) != null,
			State.Orders.Any(o => o.ArtistId == artist.artistId && o.Status == WritingOrderStatus.Pending),
			State.Orders.Any(o => o.ArtistId == artist.artistId && Week - o.StartedWeek < 8),
			offer, offer != null && Affordable(label, offer.CommissionFee * 8),
			offer == null ? null : CommissionPlan(label, artist, offer.DeliveryWeeks),
			offer != null && ActiveSupplier(offer.WriterId, date.AddDays(offer.DeliveryWeeks * 7).year),
			label != null && label.IsActive && !label.isPlayerOwned && artist.labelId == label.labelId);
	}
	internal static bool AwaitingCommission(ReleaseSlateItem item) => PlannedAssignmentsEnabled && State != null && item != null &&
		State.Orders.Any(o => o.ReleasePlanId == item.Id && o.Status == WritingOrderStatus.Pending);
	internal static bool AwaitingReminderCommission(ReleaseSlateItem item) => PlanRemindersEnabled && State != null && item != null &&
		State.Orders.Any(o => o.PlanReminder && o.ReleasePlanId == item.Id && o.Status == WritingOrderStatus.Pending);
	internal static bool WasDeliveryOffered(string songId, string artistId) => DeliveryOffersEnabled && State != null &&
		State.Orders.Any(o => o.DeliveryOffered && o.SongId == songId && o.ArtistId == artistId);
	internal static bool TryAiCommission(AILabel label, SimulatedArtist artist, string writerId, GameDate date) =>
		TryAiCommission(label, artist, writerId, date, out _);
	internal static bool TryAiCommission(AILabel label, SimulatedArtist artist, string writerId, GameDate date, out string reason) {
		reason = "invalid-client";
		if (!Enabled || State == null || label == null || label.isPlayerOwned || artist == null) return false;
		reason = "owned-delivery";
		if (OwnedDelivery(label, artist) != null) return false;
		reason = "pending-or-recent-order";
		if (State.Orders.Any(o => o.ArtistId == artist.artistId && (o.Status == WritingOrderStatus.Pending || Week - o.StartedWeek < 8))) return false;
		reason = "no-plan-minimum-delivery";
		if (PlannedAssignmentsEnabled && CommissionPlan(label, artist, 2) == null) return false;
		var offer = Offers(label, artist).FirstOrDefault(o => o.WriterId == SupplierId(writerId) && o.Available);
		reason = "offer-unavailable";
		if (offer == null) return false;
		reason = "cash-reserve";
		if (!Affordable(label, offer.CommissionFee * 8)) return false;
		var plan = PlannedAssignmentsEnabled ? CommissionPlan(label, artist, offer.DeliveryWeeks) : null;
		reason = "no-plan-delivery-time";
		if (PlannedAssignmentsEnabled && plan == null) return false;
		reason = "booking-refused";
		if (!TryCommission(label, artist, writerId, date, out var order, out _)) return false;
		order.ReleasePlanId = plan?.Id;
		reason = "ordered";
		return true;
	}

	public static bool TryContract(AILabel label, string writerId, WritingContractKind kind, out string message) {
		message = "This supplier cannot take that contract.";
		writerId = SupplierId(writerId);
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		if (!Enabled || State == null || writerId == null || kind is not (WritingContractKind.Preferred or WritingContractKind.Exclusive) || label == null ||
			!State.Suppliers.ContainsKey(writerId) || !ActiveSupplier(writerId, year) || Contract(writerId, Week) != null ||
			State.Contracts.Count(c => c.LabelId == label.labelId && !c.Ended && c.EndWeek > Week) >= 2) return false;
		// Reserve only a minority of world capacity; one exclusive supplier per label.
		if (kind == WritingContractKind.Exclusive && (State.Contracts.Any(c => c.LabelId == label.labelId && !c.Ended && c.EndWeek > Week && c.Kind == kind) ||
			State.Contracts.Count(c => !c.Ended && c.EndWeek > Week && c.Kind == kind) >= Math.Max(1, State.Suppliers.Count / 4))) return false;
		float fee = Rate(writerId, true) * (kind == WritingContractKind.Preferred ? .75f : 2.5f);
		if (!Affordable(label, fee)) { message = $"The retainer is ${fee:N0} every four weeks; available cash is too low."; return false; }
		Pay(label, fee);
		State.Suppliers[writerId].RetainerFees += fee;
		State.Contracts.Add(new WritingContract { WriterId = writerId, LabelId = label.labelId, Kind = kind, StartWeek = Week,
			EndWeek = Week + ContractWeeks, NextPaymentWeek = Week + BillingWeeks, Retainer = fee });
		message = $"{SupplierName(writerId)}: {kind} for {ContractWeeks} weeks, ${fee:N0} every four weeks. " +
			(kind == WritingContractKind.Preferred ? "Two-week first refusal on new demos; discounted assignments." : "New writing capacity reserved; assignments still require delivery time.") + " Existing songs and publishing rights are excluded.";
		return true;
	}

	public static float DemoRead(SongComposition song, AILabel label, string axis = "craft") {
		float truth = axis == "hook" ? song.commercialHook : song.GetCraftScore();
		float band = .18f - .12f * Math.Clamp(label.scoutingAbility, 0, 1);
		return Math.Clamp(truth + (Unit(label.labelId + "|" + song.songId + "|" + axis) * 2 - 1) * band, 0, 1);
	}
	internal static bool NeedsFirstRecordingLicense(SongComposition song) => Enabled && State != null && song != null &&
		State.Demos.TryGetValue(song.songId, out var d) && !d.Released;
	internal static void MarkReleased(string songId) {
		if (Enabled && State != null && songId != null && State.Demos.TryGetValue(songId, out var d)) d.Released = true;
	}
	private static void EndContract(WritingContract contract, int week) {
		contract.Ended = true;
		foreach (var d in State.Demos.Values.Where(d => d.WriterId == contract.WriterId && d.LabelId == contract.LabelId && d.Status == WritingDemoStatus.Available))
			d.PriorityUntilWeek = Math.Min(d.PriorityUntilWeek, week);
	}
	internal static void MarkRecorded(SongComposition song, string artistId) {
		if (!Enabled || State == null || song == null || !State.Demos.TryGetValue(song.songId, out var d) ||
			d.Status != WritingDemoStatus.Licensed || artistId != null && d.ArtistId != artistId) return;
		d.Status = WritingDemoStatus.Recorded;
		Partnership(d.LabelId, d.ArtistId, d.WriterId, true).Recordings++;
	}
	internal static void ObserveChart(IEnumerable<RecordRuntimeData> chart) {
		if (!Enabled || State == null) return;
		foreach (var r in chart.Where(r => r.currentPosition >= 1 && r.currentPosition <= 40)) {
			var record = r.baseRecord;
			string writer = SupplierFor(CompositionCatalogService.GetSong(record.songId));
			if (writer == null || !State.Suppliers.TryGetValue(writer, out var s) || !State.ObservedHits.Add(record.recordId)) continue;
			s.Hits++; s.Reputation = Math.Min(1, s.Reputation + .025f);
			Partnership(record.labelId, record.artistId, writer, true).Hits++;
		}
	}

	public static void Deliver(GameDate date) {
		if (!Enabled || State == null) return;
		foreach (var order in State.Orders.Where(o => o.Status == WritingOrderStatus.Pending && o.DueDate <= date).ToArray()) {
			var artist = ArtistManager.Instance?.GetArtist(order.ArtistId);
			var label = ChartManager.Instance?.GetLabelById(order.LabelId);
			if (label?.IsActive != true || artist?.labelId != order.LabelId || !ActiveSupplier(order.WriterId, date.year)) { order.Status = WritingOrderStatus.Cancelled; continue; }
			var p = Partnership(order.LabelId, order.ArtistId, order.WriterId);
			var song = CompositionCatalogService.WriteOfficeSong(Writer(order.WriterId).publisherId, new[] { order.Genre }, date.year,
				order.WriterId, true, Math.Min(1, (p?.Recordings ?? 0) / 5f), artist.members?.FirstOrDefault(m => m.personId == order.CoWriterPersonId));
			order.SongId = song.songId; order.Status = WritingOrderStatus.Delivered;
			State.Demos[song.songId] = new WritingDemo { SongId = song.songId, WriterId = order.WriterId, LabelId = order.LabelId, ArtistId = order.ArtistId,
				Status = WritingDemoStatus.Licensed, AvailableWeek = order.DeliveryWeek, HoldUntilWeek = order.DeliveryWeek + LicenseHoldWeeks, FeePaid = order.Fee };
			if (DeliveryOffersEnabled && !label.isPlayerOwned) AiRepertoireService.OfferWritingDelivery(label, artist, order, date);
		}
	}

	internal static void Advance(int week, GameDate date) {
		if (!Enabled || State == null || week <= State.LastWeek) return;
		State.LastWeek = week;
		if (PlanRemindersEnabled) foreach (var reminder in State.Reminders.Where(r => week > r.ExpiresWeek).ToArray()) {
			State.Reminders.Remove(reminder);
			OnPlanReminderAudit?.Invoke(week, reminder.LabelId, reminder.ArtistId, reminder.ContractSequence, reminder.CreatedWeek, "expired", "no-eligible-plan", null, null);
		}
		foreach (var c in State.Contracts.Where(c => !c.Ended).ToArray()) {
			var label = ChartManager.Instance?.GetLabelById(c.LabelId);
			if (week >= c.EndWeek || label?.IsActive != true || !ActiveSupplier(c.WriterId, date.year)) { EndContract(c, week); continue; }
			if (week >= c.NextPaymentWeek) {
				if (!Affordable(label, c.Retainer)) EndContract(c, week);
				else { Pay(label, c.Retainer); State.Suppliers[c.WriterId].RetainerFees += c.Retainer; c.NextPaymentWeek += BillingWeeks; }
			}
		}
		Deliver(date);
		if (DeliveryOffersEnabled) foreach (var o in State.Orders.Where(o => o.Status == WritingOrderStatus.Delivered && !o.DeliveryOffered).ToArray())
			AiRepertoireService.OfferWritingDelivery(ChartManager.Instance?.GetLabelById(o.LabelId), ArtistManager.Instance?.GetArtist(o.ArtistId), o, date);
		foreach (var d in State.Demos.Values.Where(d => d.Status == WritingDemoStatus.Licensed && d.HoldUntilWeek < week)) {
			d.Status = WritingDemoStatus.Available; d.LabelId = null; d.ArtistId = null; d.PriorityUntilWeek = 0;
		}
		foreach (var s in State.Suppliers.Values.OrderBy(s => s.WriterId, StringComparer.Ordinal)) {
			if (!ActiveSupplier(s.WriterId, date.year) || week < s.NextWritingWeek || week <= s.BusyUntilWeek) continue;
			s.NextWritingWeek = week + 4 + (int)(Unit(s.WriterId + "|cadence") * 3);
			if (State.Demos.Values.Count(d => d.WriterId == s.WriterId && d.Status == WritingDemoStatus.Available) >= 8) continue;
			var writer = Writer(s.WriterId);
			var pub = CompositionCatalogService.Publishers.First(p => p.publisherId == writer.publisherId);
			var song = CompositionCatalogService.WriteOfficeSong(pub.publisherId, pub.focusGenres, date.year, s.WriterId, true);
			var contract = Contract(s.WriterId, week);
			float selfRead = song.GetCraftScore() + (Unit(song.songId + "|self-read") * 2 - 1) * .12f;
			State.Demos[song.songId] = new WritingDemo { SongId = song.songId, WriterId = s.WriterId, AvailableWeek = week,
				Status = selfRead < .47f ? WritingDemoStatus.Shelved : WritingDemoStatus.Available,
				LabelId = contract?.LabelId, PriorityUntilWeek = contract == null ? 0 : contract.Kind == WritingContractKind.Exclusive ? contract.EndWeek : week + PriorityWeeks };
		}
		RunReturningClients(date);
	}

	private static void RunReturningClients(GameDate date) {
		foreach (var p in State.Partnerships.Values.Where(p => p.Hits > 0 && Week - p.LastAssignmentWeek >= 8).OrderBy(p => p.LastAssignmentWeek).ThenBy(p => p.ArtistId, StringComparer.Ordinal).ToArray()) {
			var label = ChartManager.Instance?.GetLabelById(p.LabelId);
			var artist = ArtistManager.Instance?.GetArtist(p.ArtistId);
			if (label?.IsActive != true || label.isPlayerOwned || artist?.labelId != p.LabelId || !CanAiCommission(label, artist)) continue;
			if (State.Demos.Values.Any(d => d.ArtistId == p.ArtistId && d.Status == WritingDemoStatus.Licensed && d.HoldUntilWeek >= Week)) continue;
			var offer = Offers(label, artist).FirstOrDefault(o => o.WriterId == p.WriterId);
			if (offer == null || !offer.Available || label.cashReserves < offer.CommissionFee * 8) continue;
			if (PlannedAssignmentsEnabled && CommissionPlan(label, artist, offer.DeliveryWeeks) == null) continue;
			if (p.Assignments >= 3 && Contract(p.WriterId, Week) == null && label.cashReserves > offer.ExclusiveRetainer * 12)
				TryContract(label, p.WriterId, p.Hits >= 3 ? WritingContractKind.Exclusive : WritingContractKind.Preferred, out _);
			TryAiCommission(label, artist, p.WriterId, date);
		}
	}
}
