using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 1A (§4.1-4.2, F7 wording): release-completion preflight and the CASH OPTIONS card.
/// Read-only throughout: no state, no randomness, no cash, no clock. It reads the same gates the verbs enforce, so
/// an option shown as executable is one the desk would accept now; it never manufactures demand or a buyer.
/// </summary>
public partial class PlayerDesk {
	// ── Solvency timing (F7): checked on the 1st, after the month's bills ───────────────────────────────

	/// <summary>The rule as one sentence, for tooltips, the books and closure text.</summary>
	public const string SolvencyRuleText =
		"The bank checks your cash on the 1st of each month, after that month's overhead is paid. " +
		"Three checks in a row below $0 close the label; so does any check below the overdraft ceiling. " +
		"Any check at or above $0 resets the count.";

	/// <summary>The next solvency check: its date, the overhead taken just before it, and what cash would read then if
	/// nothing else came in or went out. Overhead uses today's rate.</summary>
	public (GameDate Date, int Days, float Overhead, float CashAfter, bool WouldClose, string Outcome) NextSolvencyCheck(float cashNow) {
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		var first = FirstOfNextMonth(today);
		float overhead = Label?.GetMonthlyOverhead() ?? 0f, after = cashNow - overhead;
		bool close = after < CreditFloor || after < 0f && monthsInTheRed + 1 >= MaxMonthsInTheRed;
		string outcome = after < CreditFloor ? "below the overdraft ceiling: the label closes"
			: after < 0f ? (close ? $"red check {monthsInTheRed + 1} of {MaxMonthsInTheRed}: the label closes" : $"red check {monthsInTheRed + 1} of {MaxMonthsInTheRed}")
			: monthsInTheRed > 0 ? "in the black: the red count resets" : "in the black";
		return (first, DaysBetween(today, first), overhead, after, close, outcome);
	}

	/// <summary>The last date money can still count toward survival: the check that would close the label if cash stays
	/// where it is (past it, nothing arriving helps). Null while no check in the visible horizon would close it.</summary>
	public GameDate? ClosureCheckIfNothingChanges() {
		if (Label == null) return null;
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		float cash = Label.cashReserves, overhead = Label.GetMonthlyOverhead();
		int red = monthsInTheRed;
		var check = FirstOfNextMonth(today);
		for (int i = 0; i < 6; i++, check = FirstOfNextMonth(check)) {
			cash -= overhead;
			if (cash < CreditFloor) return check;
			if (cash < 0f) { if (++red >= MaxMonthsInTheRed) return check; } else red = 0;
		}
		return null;
	}

	private static GameDate FirstOfNextMonth(GameDate d) => d.month == 12 ? new GameDate(d.year + 1, 1, 1) : new GameDate(d.year, d.month + 1, 1);
	private static int DaysBetween(GameDate a, GameDate b) =>
		(int)(new DateTime(b.year, b.month, b.day) - new DateTime(a.year, a.month, a.day)).TotalDays;
	private static GameDate AddDays(GameDate d, int days) { var t = new DateTime(d.year, d.month, d.day).AddDays(days); return new GameDate(t.Year, t.Month, t.Day); }

	// ── §4.1 Release completion preflight ─────────────────────────────────────────────────────────────

	/// <summary>The plant's quoted turnaround for a first run: the forecast plans on the slow end so an overhead date
	/// is not hidden by an optimistic arrival.</summary>
	public const int CompletionPlantDaysForecast = 30;

	public enum CompletionStageKind { Signing, Studio, Pressing }

	/// <summary>One unfunded step on the cheapest credible route to a first deliverable single. Cost is a current quote
	/// where the desk has one and an estimate otherwise (<see cref="Estimate"/>).</summary>
	public sealed record CompletionStage(CompletionStageKind Kind, string Name, float Cost, bool Estimate, string Note);

	public sealed record ReleaseCompletionPlan(float Cash, float SpendNow, IReadOnlyList<CompletionStage> Remaining,
		IReadOnlyList<(GameDate Date, float Overhead)> OverheadBeforeArrival, GameDate LikelyArrival,
		int SellableStock, int PressingsInFlight, float Receivables, string ReadyTitle) {
		public float RemainingCost => Remaining.Sum(s => s.Cost);
		public float CashAfter => Cash - SpendNow;
		/// <summary>Stock on hand, a run in flight or money owed: the warning is qualified, not absolute.</summary>
		public bool HasIncome => SellableStock > 0 || PressingsInFlight > 0 || Receivables > 0.5f;
		public bool Unfunded => Remaining.Count > 0 && CashAfter < RemainingCost;
		/// <summary>The first stage the cash after this spend cannot reach, or null when the route is funded.</summary>
		public CompletionStage FirstUnfunded {
			get {
				float left = CashAfter;
				foreach (var s in Remaining) { if (left < s.Cost) return s; left -= s.Cost; }
				return null;
			}
		}
	}

	/// <summary>
	/// The remaining minimum route to a first deliverable single, read from what the label holds now: an assembled
	/// single needs only its cheapest legal run; two printed masters need a first run; a console session needs nothing
	/// more from the studio; a signed act needs the cheapest bookable local date; no act needs a signing too. Pressings
	/// in flight, stock and receivables are reported alongside so the warning can be qualified (§4.1).
	/// </summary>
	public ReleaseCompletionPlan PreviewReleaseCompletion(float spendNow = 0f, CompletionStageKind? covers = null) {
		if (Label == null || !float.IsFinite(spendNow)) return null;
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		var ownTitles = planned.Select(p => p.Master?.Record?.recordId).Concat(ReleasedRecords.Select(r => r.baseRecord?.recordId))
			.Where(id => id != null && !IsMasterOut(id)).Distinct(StringComparer.Ordinal).ToList();
		int sellable = ownTitles.Sum(id => StockFor(id)?.Remaining ?? 0);
		var inFlight = pressOrders.Where(o => ownTitles.Contains(o.RecordId)).ToList();
		float receivables = EnsureStops().Values.Sum(s => Math.Max(0f, s.OpenBalance)) + (Label.wholesaleReceivables?.Sum(r => r.Amount) ?? 0f);

		var stages = new List<CompletionStage>();
		string readyTitle = null;
		// Assembled and not yet pressed: the cheapest legal run of any of them.
		var unpressed = planned.Where(p => p.Master?.Record != null && !IsMasterOut(p.Master.Record.recordId) &&
			!HasBeenPressed(p.Master.Record.recordId) && PressingOrderFor(p.Master.Record.recordId) == null)
			.Select(p => (Id: p.Master.Record.recordId, Cost: PressingCost(MinimumPressRun(p.Master.Record.recordId))))
			.OrderBy(x => x.Cost).ToList();
		bool anyPressed = ownTitles.Any(id => HasBeenPressed(id) || PressingOrderFor(id) != null);
		if (unpressed.Count > 0) {
			readyTitle = TitleForRecord(unpressed[0].Id);
			stages.Add(new(CompletionStageKind.Pressing, $"Press \"{readyTitle}\"", unpressed[0].Cost, false,
				$"first run of {PressMinimumOrder:N0}: ${PressLacquerSetup:N0} lacquers, ${PressShipping:N0} shipping, ${PressVinylPerUnit + PressSleeveLabelPerUnit:F2} a copy"));
		} else if (!anyPressed) {
			bool twoMasters = masters.Count >= 2, onConsole = pendingSession != null;
			if (!twoMasters && !onConsole) {
				var act = Roster.FirstOrDefault();
				if (act == null)
					stages.Add(new(CompletionStageKind.Signing, "Sign an act", 0f, true, "the advance is whatever you agree; the signing reserve must still be left after it"));
				if (covers != CompletionStageKind.Studio) {   // the spend being previewed IS the date: skip quoting rooms
					var quote = CheapestLocalStudioQuote(act);
					stages.Add(new(CompletionStageKind.Studio, "Record an A and a B side", quote.Cost, quote.Estimate, quote.Note));
				}
			}
			stages.Add(new(CompletionStageKind.Pressing, "Press a first run", PressingCost(PressMinimumOrder), false,
				$"the plant's minimum of {PressMinimumOrder:N0}; assembling the single costs nothing"));
		}

		GameDate arrival = inFlight.Count > 0 && stages.Count == 0 ? inFlight.Min(o => o.Arrives) : AddDays(today, CompletionPlantDaysForecast);
		var overhead = new List<(GameDate, float)>();
		for (var check = FirstOfNextMonth(today); check <= arrival; check = FirstOfNextMonth(check)) overhead.Add((check, Label.GetMonthlyOverhead()));
		if (covers.HasValue) stages.RemoveAll(s => s.Kind == covers.Value);
		return new ReleaseCompletionPlan(Label.cashReserves, spendNow, stages.AsReadOnly(), overhead.AsReadOnly(), arrival,
			sellable, inFlight.Count, receivables, readyTitle);
	}

	/// <summary>The cheapest date the home sheet would sell for an A and a B side: the cheapest bookable local room at the
	/// minimum hours. Without an act (or with the incremental studio off) it falls back to the budget-room rate.</summary>
	private (float Cost, bool Estimate, string Note) CheapestLocalStudioQuote(SimulatedArtist act) {
		if (act != null && StudioExperience.Enabled) {
			var best = StudioRoomIds().Select(id => StudioOffer(id, act.artistId)).Where(o => o != null && o.Locked == null && o.Local)
				.OrderBy(o => StudioRoomCost(o.Room, MinSessionHours)).FirstOrDefault();
			if (best != null)
				return (StudioRoomCost(best.Room, MinSessionHours), false,
					$"{best.Room.Name}, {MinSessionHours}h at ${best.Room.HourlyRate + best.Room.EngineerPremium:N0} an hour; players, singers and extra hours are extra");
		}
		return (SessionCost(StudioTier.Budget, MinSessionHours), true, $"home budget room, {MinSessionHours}h; the actual room quote may differ");
	}

	/// <summary>
	/// The §4.1 warning line for a spend, or null when the route stays funded (or nothing is left to fund). It names the
	/// unfunded stage, and says so plainly when that stage is pressing a single already assembled. Stock, a run in
	/// flight and receivables qualify it. Overhead falling before the likely arrival is named. It never blocks a spend.
	/// </summary>
	public string CompletionWarning(float spendNow, CompletionStageKind? covers = null) {
		var plan = PreviewReleaseCompletion(spendNow, covers);
		if (plan == null || plan.Remaining.Count == 0) return null;
		var gap = plan.FirstUnfunded;
		float overheadDue = plan.OverheadBeforeArrival.Sum(o => o.Overhead);
		bool overheadBites = gap == null && overheadDue > 0f && plan.CashAfter - plan.RemainingCost < overheadDue;
		if (gap == null && !overheadBites) return null;
		string line = gap == null
			? $"After this, the route to a first release is paid for, but ${overheadDue:N0} of overhead falls due before a run would arrive ({string.Join(", ", plan.OverheadBeforeArrival.Select(o => o.Date.ToShortString()))})."
			: gap.Kind == CompletionStageKind.Pressing && plan.ReadyTitle != null
				? $"After this, you cannot afford to press any of the singles you have ready (cheapest run ${gap.Cost:N0}, cash after ${plan.CashAfter:N0})."
				: $"After this, {gap.Name.ToLowerInvariant()} is unfunded: the route still needs ${plan.RemainingCost:N0} ({string.Join(" + ", plan.Remaining.Select(s => $"{s.Name.ToLowerInvariant()} ${s.Cost:N0}{(s.Estimate ? " est." : "")}"))}), cash after ${plan.CashAfter:N0}.";
		if (plan.HasIncome)
			line += $" Already working for you: {string.Join(", ", new[] {
				plan.SellableStock > 0 ? $"{plan.SellableStock:N0} copies on hand" : null,
				plan.PressingsInFlight > 0 ? $"{plan.PressingsInFlight} run(s) at the plant" : null,
				plan.Receivables > 0.5f ? $"${plan.Receivables:N0} owed to you" : null }.Where(s => s != null))}.";
		return line;
	}

	// ── §4.2 Cash options ───────────────────────────────────────────────────────────────────────────

	public enum CashCertainty { Owed, Accepted, Speculative, Blocked }

	/// <summary>One route to cash, as the card shows it. <see cref="Executable"/> means the desk would accept the action
	/// now (prerequisites met, affordable, in reach, and paying before the closing check); the money itself may still
	/// depend on a buyer, as <see cref="Certainty"/> says.</summary>
	public sealed record CashOption(string Route, string Action, string Prerequisite, float Cost, float Expected,
		GameDate? CashDate, CashCertainty Certainty, bool Executable, string Why);

	/// <summary>
	/// The CASH OPTIONS card (§4.2): every current route with its prerequisites, cost, expected cash, date and certainty.
	/// It can be empty -- that is the honest answer for an exhausted business. Speculative routes (a cold pitch, a
	/// distribution pitch) are never shown as assured; a route that pays only after the closing check is marked too late.
	/// </summary>
	public IReadOnlyList<CashOption> CashOptions() {
		var list = new List<CashOption>();
		if (Label == null) return list;
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		GameDate? closing = ClosureCheckIfNothingChanges();
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		string home = Label.homeCityId;
		bool TooLate(GameDate? when) => closing.HasValue && when.HasValue && when.Value >= closing.Value;
		string Late(GameDate? when) => TooLate(when) ? $" Too late: the {closing.Value.ToShortString()} check comes first." : "";
		(int Hours, float Gas) Trip(string cityId) => cityId == CurrentCityId ? (0, 0f) : DriveQuote(CurrentCityId ?? home, cityId);

		// Money already owed at the counters: collected by turning up in that town.
		foreach (var town in EnsureStops().Values.Where(s => s.OpenBalance > 0.5f).GroupBy(s => s.CityId)) {
			float owed = town.Sum(s => s.OpenBalance);
			var trip = Trip(town.Key);
			bool reach = trip.Hours == 0 || CanReachCity(town.Key);
			bool afford = town.Key == home || Label.cashReserves >= trip.Gas; // the drive home always goes, on the tab
			var when = trip.Hours > MaxStudioDriveHours ? AddDays(today, 1) : today;
			list.Add(new("Collect account balances", $"Work or revisit {CityName(town.Key)}", trip.Hours == 0 ? "in town" : $"{trip.Hours}h drive",
				trip.Gas, owed, when, CashCertainty.Owed, reach && afford && !TooLate(when),
				(!reach ? "Out of driving range." : !afford ? $"Gas ${trip.Gas:N0} is more than you have." : "Dealers settle what they owe when you call.") + Late(when)));
		}

		// Wholesale paper: due on its own date, or sold now at a discount.
		var ordered = OrderedReceivables();
		for (int i = 0; i < ordered.Count; i++) {
			var r = ordered[i].Receivable;
			var due = AddDays(today, Math.Max(0, r.DueWeek - week) * 7);
			float rate = FactorRatePreview(i);
			list.Add(new("Wholesale invoice", "Wait for the due date", HouseFor(r.DistributorId)?.distributorName ?? "a house", 0f, r.Amount, due,
				CashCertainty.Owed, !TooLate(due), "Houses can short-pay or hold back against returns." + Late(due)));
			list.Add(new("Factor an invoice", "Sell the invoice now", "outstanding wholesale paper", r.Amount * (1f - rate), r.Amount * rate, today,
				CashCertainty.Accepted, true, $"{rate:P0} on the dollar, today."));
		}

		// Orders already placed: dealers asking for copies, COD when delivered.
		foreach (var group in inboundCalls.GroupBy(c => c.RecordId)) {
			int onHand = StockFor(group.Key)?.Remaining ?? 0;
			int asked = group.Sum(c => c.RequestedQty);
			var expires = AddDays(today, Math.Max(0, group.Min(c => c.ExpiresWeek) - week) * 7);
			bool stocked = onHand > 0 && !IsMasterOut(group.Key);
			list.Add(new("Deliver called-in orders", $"Deliver \"{TitleForRecord(group.Key)}\"", $"{group.Count()} account(s) asked for {asked:N0}",
				0f, Math.Min(asked, onHand) * DealerPrice, today, CashCertainty.Accepted, stocked,
				stocked ? $"Dealers asked; COD or consignment on delivery. Calls lapse around {expires.ToShortString()}."
					: onHand <= 0 ? "Nothing pressed on hand to deliver." : "The master is out."));
		}

		// Stock on hand, sold by pitching at counters: real, but every dealer can pass.
		foreach (string id in planned.Select(p => p.Master?.Record?.recordId).Concat(ReleasedRecords.Select(r => r.baseRecord?.recordId))
			.Where(id => id != null).Distinct(StringComparer.Ordinal)) {
			int onHand = StockFor(id)?.Remaining ?? 0;
			if (onHand <= 0 || IsMasterOut(id)) continue;
			int accounts = StopsInCity(CurrentCityId ?? home).Count(s => s.Kind is StopKind.Shop or StopKind.Op && s.LastApproachDate != today && !s.PassedRecordIds.Contains(id));
			list.Add(new("Sell stock on hand", $"Pitch \"{TitleForRecord(id)}\" at the counters", $"{onHand:N0} copies on hand, {accounts} account(s) here not yet tried today",
				0f, 0f, today, CashCertainty.Speculative, accounts > 0, accounts > 0 ? "A buyer can take a handful or pass; nothing is promised." : "Every account here has been worked today or passed."));
		}

		// Act buy-in: cash today at a deep discount, where an act has enough of its own single on hand.
		foreach (var (artist, recordId, title, onHand) in BuyInEligible()) {
			int take = Math.Min(ArtistBuyInMax, onHand);
			list.Add(new("Artist buy-in", $"{artist.stageName} buys \"{title}\"", $"{onHand:N0} on hand (minimum {ArtistBuyInMin})", 0f, take * ArtistBuyInPrice,
				today, CashCertainty.Accepted, AtHome, AtHome ? $"${ArtistBuyInPrice:F2} a copy, cash today; no chart credit." : "Back at the office only."));
		}

		// Existing late exits (§1.2): master deals and the plant's credit run.
		foreach (var rec in ReleasedRecords) {
			string id = rec.baseRecord?.recordId;
			if (id == null || IsMasterOut(id)) continue;
			if (MasterDealEligible(id))
				list.Add(new("Lease or sell the master", $"\"{TitleForRecord(id)}\"", "airplay and a one-stop that knows it", 0f, MasterLeaseValue(id), today,
					CashCertainty.Accepted, true, $"Lease ${MasterLeaseValue(id):N0} or sell ${MasterSaleValue(id):N0}; the title leaves your own channels."));
			if (PlantCreditEligible(id))
				list.Add(new("Plant credit run", $"\"{TitleForRecord(id)}\"", $"open calls for {OpenCallDemand(id):N0}", 0f, 0f, null, CashCertainty.Accepted, AtHome,
					$"{PlantCreditQuantity:N0} copies with no cash down; ${PressingCost(PlantCreditQuantity, HasBeenPressed(id)):N0} is due in {PlantCreditTermWeeks} weeks. Stock, not cash."));
		}

		// Engagements the label booked: a residency's booking commission, a tour's net (F5 will move tour money to the performers).
		foreach (var act in Roster) {
			var booking = RoadBookingFor(act.artistId);
			if (booking == null || booking.WeeksRemaining <= 0) continue;
			bool residency = booking.ResidencyWeeks >= booking.TourWeeks;
			float weekly = residency ? booking.ResidencyWeeklyCommission : TourWeeklyNet;
			if (weekly <= 0f) continue;   // a residency with no room behind it pays the label nothing
			var next = AddDays(today, 7 - (DaysBetween(GameDate.StartDate, today) % 7));
			list.Add(new("Booked engagements", $"{act.stageName}: {booking.WeeksRemaining} week(s) left", residency ? "residency" : "on the road",
				0f, weekly * booking.WeeksRemaining, next, CashCertainty.Accepted, !TooLate(next), $"${weekly:N0} a week as each week settles." + Late(next)));
		}
		return list.AsReadOnly();
	}

	private bool CanReachCity(string cityId) {
		var from = DistanceModel.GetCityById(CurrentCityId ?? Label?.homeCityId); var to = DistanceModel.GetCityById(cityId);
		return from != null && to != null && CanReach(from, to);
	}
}
