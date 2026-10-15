using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 2 (§4.3): one backer agreement per label.
///
/// Author decision (2026-10-14): no family loans -- founding capital already is the family's money. So each founding
/// archetype has one non-family source, and every source is someone the world already holds or a named private person:
/// - <b>Ex-Musician:</b> the man who runs a club (or, failing one, a roadhouse) in the home scene, a room the founder used
///   to play. Arrears cost standing with that room's booker: bookings, residencies and table rights get harder there.
/// - <b>Pawn-Shop Owner:</b> a merchant lender who advances against goods, secured on the label's pressed stock. After two
///   missed payments in a row he takes copies on hand at the dealer price against the arrears. No stock, the arrears stand.
/// - <b>Promo Man:</b> the home town's jukebox operator, who wants records for his machines. Arrears cost the label
///   standing on his route (the Op account's relationship).
/// - <b>Trade Insider:</b> a private backer a trade contact introduced. Arrears cost the label trade reputation.
///
/// The ask is a 2h meeting. The backer never hears the hidden hook. He reads a signed act, whether there is anything
/// recorded to play him, and a realistic completion budget (the remaining route to a first release plus two months of
/// overhead, within his own means). His answer is deterministic, so asking again on unchanged evidence can't reroll it; a
/// refusal stands until the evidence changes. An offer can carry altered terms (a smaller sum, or a higher rate in place
/// of the record he hasn't heard) and stays open for two weeks.
///
/// Terms (authored, not sourced): simple interest at the stated annual rate on outstanding principal, accrued actual/365
/// from the day the money is paid over; the first payment falls on the third 1st after signing; twelve monthly principal
/// installments of a twelfth each, the last taking the remainder, each with all interest accrued to that day. Payments are
/// taken on the 1st after the overhead and before the bank's check, in the order interest, then overdue principal, then
/// this month's installment, from cash on hand only: a payment never takes cash below zero, so it can never turn a check
/// red. What isn't paid is arrears and costs standing; no separate game-over. Principal is financing, not revenue;
/// repaying it is not an operating expense; interest is. Balances are kept as a ledger and are recomputable from it.
/// One agreement per label, ever: no top-ups, no second ask once signed.
/// </summary>
public partial class PlayerDesk {
	public enum BackerSource { ClubOwner, MerchantLender, JukeboxOperator, PrivateBacker }
	public enum BackerLedgerKind { Disbursed, InterestAccrued, InterestPaid, PrincipalPaid, Missed, CollateralInterest, CollateralPrincipal, Repaid }

	public const int BackerMeetingHours = 2;
	public const int BackerOfficeFromHour = 9;
	public const int BackerOfficeUntilHour = 17;
	public const float BackerRate = 0.06f;
	public const float BackerAlteredRate = 0.09f;
	public const float BackerMinimumPrincipal = 300f;
	public const float BackerAlteredShare = 0.75f;
	public const int BackerGraceFirsts = 3;
	public const int BackerInstallments = 12;
	public const int BackerOfferDays = 14;
	public const float BackerStandingPerMiss = 0.15f;
	public const float BackerStandingPerPayment = 0.05f;
	/// <summary>At standing 0 the club owner's booker is this much less likely to say yes (subtracted from the chance).</summary>
	public const float BackerRoomPenaltyMax = 0.30f;
	public const int BackerSeizeAfterMisses = 2;
	public const float BackerOperatorRelationshipPerMiss = 0.10f;
	public const float BackerReputationPerMiss = 0.01f;

	public static float BackerCapacity(BackerSource source, SceneRoomKind? roomKind = null) => source switch {
		BackerSource.ClubOwner => roomKind == SceneRoomKind.Club ? 500f : 400f,
		BackerSource.MerchantLender => 600f,
		BackerSource.JukeboxOperator => 500f,
		BackerSource.PrivateBacker => 800f,
		_ => 0f,
	};

	public sealed class BackerLedgerEntry {
		public GameDate Date;
		public BackerLedgerKind Kind;
		public float Amount;
		public int Copies;
		public string RecordId;
	}

	/// <summary>The signed agreement. Balances live in the running fields and are recomputable from <see cref="Ledger"/>.</summary>
	public sealed class BackerAgreement {
		public BackerSource Source;
		public string BackerName;
		/// <summary>The room (club owner) or stop (operator) behind the backer; null for a private person.</summary>
		public string CounterpartyId;
		public float Principal;
		public float Rate;
		public GameDate Signed;
		public GameDate FirstDue;
		public int Installments = BackerInstallments;
		public GameDate LastAccrual;
		public float PrincipalPaid, InterestAccrued, InterestPaid;
		public int Missed, ConsecutiveMissed;
		public float Standing = 1f;
		public bool Repaid;
		public List<BackerLedgerEntry> Ledger = new();

		public float Outstanding => R2(Principal - PrincipalPaid);
		public float InterestDue => R2(InterestAccrued - InterestPaid);
		public float Installment => R2(Principal / Installments);

		/// <summary>The due dates: the first, then each following 1st, <see cref="Installments"/> in all.</summary>
		public IEnumerable<GameDate> DueDates() {
			var d = FirstDue;
			for (int i = 0; i < Installments; i++, d = FirstOfNextMonth(d)) yield return d;
		}

		/// <summary>Principal the schedule calls for on or before <paramref name="date"/>.</summary>
		public float ScheduledBy(GameDate date) {
			int due = DueDates().Count(d => d <= date);
			return due >= Installments ? Principal : R2(Installment * due);
		}

		public float PrincipalArrears(GameDate date) => Mathf.Max(0f, R2(ScheduledBy(date) - PrincipalPaid));
		public GameDate? NextDue(GameDate after) => Repaid ? null : DueDates().Where(d => d > after).Cast<GameDate?>().FirstOrDefault()
			?? (Outstanding > 0.005f ? FirstOfNextMonth(after) : null);

		public BackerAgreement Copy() {
			var c = (BackerAgreement)MemberwiseClone();
			c.Ledger = new List<BackerLedgerEntry>(Ledger);
			return c;
		}

		/// <summary>Balances rebuilt from the ledger alone: (principal paid, interest accrued, interest paid, missed).</summary>
		public (float PrincipalPaid, float InterestAccrued, float InterestPaid, int Missed) FromLedger() => (
			R2(Ledger.Where(e => e.Kind is BackerLedgerKind.PrincipalPaid or BackerLedgerKind.CollateralPrincipal).Sum(e => e.Amount)),
			R2(Ledger.Where(e => e.Kind == BackerLedgerKind.InterestAccrued).Sum(e => e.Amount)),
			R2(Ledger.Where(e => e.Kind is BackerLedgerKind.InterestPaid or BackerLedgerKind.CollateralInterest).Sum(e => e.Amount)),
			Ledger.Count(e => e.Kind == BackerLedgerKind.Missed));

		public bool LedgerAgrees() {
			var l = FromLedger();
			float disbursed = R2(Ledger.Where(e => e.Kind == BackerLedgerKind.Disbursed).Sum(e => e.Amount));
			return Math.Abs(disbursed - Principal) < 0.011f && Math.Abs(l.PrincipalPaid - PrincipalPaid) < 0.011f
				&& Math.Abs(l.InterestAccrued - InterestAccrued) < 0.011f && Math.Abs(l.InterestPaid - InterestPaid) < 0.011f && l.Missed == Missed;
		}

		internal void Add(GameDate date, BackerLedgerKind kind, float amount, int copies = 0, string recordId = null) =>
			Ledger.Add(new BackerLedgerEntry { Date = date, Kind = kind, Amount = R2(amount), Copies = copies, RecordId = recordId });

		/// <summary>
		/// The 1st-of-month settlement, applied to <paramref name="cash"/> (already net of overhead). Accrues interest to the
		/// day; from the first due date on, pays interest, then overdue principal, then this month's installment, from cash
		/// above zero only. Returns what was paid. Used for the real settlement and, on a copy, for the forecasts, so the
		/// two can't disagree.
		/// </summary>
		public (float Paid, bool MissedNow) Settle(GameDate date, ref float cash) {
			if (Repaid) return (0f, false);
			int days = DaysBetween(LastAccrual, date);
			if (days > 0) {
				float interest = R2(Outstanding * Rate * days / 365f);
				if (interest > 0f) { InterestAccrued = R2(InterestAccrued + interest); Add(date, BackerLedgerKind.InterestAccrued, interest); }
				LastAccrual = date;
			}
			if (date < FirstDue) return (0f, false);
			float paid = 0f;
			float payInterest = R2(Mathf.Min(Mathf.Max(0f, cash), InterestDue));
			if (payInterest > 0f) { cash -= payInterest; paid += payInterest; InterestPaid = R2(InterestPaid + payInterest); Add(date, BackerLedgerKind.InterestPaid, payInterest); }
			float dueP = PrincipalArrears(date);
			float payPrincipal = R2(Mathf.Min(Mathf.Max(0f, cash), dueP));
			if (payPrincipal > 0f) { cash -= payPrincipal; paid += payPrincipal; PrincipalPaid = R2(PrincipalPaid + payPrincipal); Add(date, BackerLedgerKind.PrincipalPaid, payPrincipal); }
			bool missed = dueP - payPrincipal > 0.005f || InterestDue > 0.005f;
			if (missed) {
				Missed++; ConsecutiveMissed++;
				Standing = Mathf.Max(0f, Standing - BackerStandingPerMiss);
				Add(date, BackerLedgerKind.Missed, R2(dueP - payPrincipal + InterestDue));
			} else if (dueP > 0f) {
				ConsecutiveMissed = 0;
				Standing = Mathf.Min(1f, Standing + BackerStandingPerPayment);
			}
			CloseIfRepaid(date);
			return (paid, missed);
		}

		internal void CloseIfRepaid(GameDate date) {
			if (Repaid || Outstanding > 0.005f || InterestDue > 0.005f) return;
			Repaid = true;
			Add(date, BackerLedgerKind.Repaid, 0f);
		}
	}

	/// <summary>One set of terms the backer will sign, with the answer that earns it.</summary>
	public sealed record BackerTerms(string Answer, float Principal, float Rate, string Why);

	public sealed class BackerOffer {
		public GameDate Made;
		public GameDate Expires;
		public List<BackerTerms> Terms = new();
		public string Objection;
	}

	/// <summary>Who would back this label, where to find him, and what he'd want to see. Null when nobody is reachable.</summary>
	public sealed record BackerProspect(BackerSource Source, string Name, string Title, string CounterpartyId, float Capacity,
		string Where, string Hours, string Wants);

	private BackerAgreement backer;
	private BackerOffer backerOffer;
	/// <summary>A refusal and the evidence it was given on: it stands until <see cref="BackerEvidence"/> changes.</summary>
	private (int Day, string Evidence, string Reason)? backerRefusal;

	public BackerAgreement Backer => backer;
	public BackerOffer PendingBackerOffer => backerOffer != null && backerOffer.Expires >= BackerToday ? backerOffer : null;
	public (int Day, string Evidence, string Reason)? BackerRefusal => backerRefusal;

	private static GameDate BackerToday => TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
	private static float R2(float v) => Mathf.Round(v * 100f) / 100f;

	/// <summary>The backer this founder can approach, from the world's own rooms and accounts.</summary>
	public BackerProspect BackerProspectFor() {
		if (Label == null) return null;
		string home = Label.homeCityId;
		switch (Archetype) {
			case FoundingArchetype.ExMusician: {
				string scene = LocalScenePersistenceService.SceneIdFor(home);
				var room = scene == null ? null : SceneRoomCatalog.ForPlace(scene).Where(r => r.Kind is SceneRoomKind.Club or SceneRoomKind.Roadhouse)
					.OrderBy(r => r.Kind == SceneRoomKind.Club ? 0 : 1).ThenBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault();
				if (room == null) return null;
				return new(BackerSource.ClubOwner, room.ContactName, $"who runs {room.Name}, a room you used to play", room.Id,
					BackerCapacity(BackerSource.ClubOwner, room.Kind), $"at {room.Name}", $"from {BookerFromHour - 12} p.m.",
					"a signed act; he knows you, not the act");
			}
			case FoundingArchetype.PromoMan: {
				var op = StopsInCity(home).Where(s => s.Kind == StopKind.Op).OrderBy(s => s.StopId, StringComparer.Ordinal).FirstOrDefault();
				if (op == null) return null;
				return new(BackerSource.JukeboxOperator, op.DisplayName, "the jukebox operator whose route you know", op.StopId,
					BackerCapacity(BackerSource.JukeboxOperator), "at the office", $"{BackerOfficeFromHour} a.m. to {BackerOfficeUntilHour - 12} p.m.",
					"a signed act and something recorded he can picture in his machines");
			}
			case FoundingArchetype.PawnShopOwner:
				return new(BackerSource.MerchantLender, SceneRoomCharacter.PersonName($"backer|lender|{home}"), "a merchant lender who advances against goods",
					null, BackerCapacity(BackerSource.MerchantLender), "at the office", $"{BackerOfficeFromHour} a.m. to {BackerOfficeUntilHour - 12} p.m.",
					"a signed act; your pressed stock stands as security");
			default:
				return new(BackerSource.PrivateBacker, SceneRoomCharacter.PersonName($"backer|private|{home}"), "a private backer a trade contact put you onto",
					null, BackerCapacity(BackerSource.PrivateBacker), "at the office", $"{BackerOfficeFromHour} a.m. to {BackerOfficeUntilHour - 12} p.m.",
					"a signed act and a realistic budget");
		}
	}

	private bool HasRecordedWork => masters.Count > 0 || acetateCopies.Values.Any(n => n > 0) || ReleasedRecords.Any();

	/// <summary>What the backer reads, as a signature: a refusal stands while this is unchanged.</summary>
	public string BackerEvidence() => $"act={Roster.Any()}|work={HasRecordedWork}";

	/// <summary>The completion budget a backer would fund: the remaining route plus two months of overhead, in fifties.</summary>
	public float BackerNeed() {
		float route = PreviewReleaseCompletion()?.RemainingCost ?? 0f;
		float need = route + 2f * (Label?.GetMonthlyOverhead() ?? 0f);
		return Mathf.Ceil(need / 50f) * 50f;
	}

	/// <summary>The backer's answer on the evidence as it stands: a refusal reason, or the terms he'd sign. Pure.</summary>
	public (string Refusal, BackerOffer Offer) EvaluateBacker(BackerProspect p) {
		if (p == null) return ("Nobody you know is in a position to back you.", null);
		if (!Roster.Any()) return ($"{p.Name}: \"Back what? Come and see me when you've got an act signed.\"", null);
		if (p.Source == BackerSource.JukeboxOperator && !HasRecordedWork)
			return ($"{p.Name}: \"I put records in machines. Come back when you've cut something.\"", null);

		float full = Mathf.Clamp(BackerNeed(), BackerMinimumPrincipal, p.Capacity);
		var offer = new BackerOffer { Made = BackerToday, Expires = AddDays(BackerToday, BackerOfferDays) };
		if (HasRecordedWork) {
			offer.Terms.Add(new("Play him the record", full, BackerRate, $"He hears what you've cut and puts up the budget: ${full:N0}."));
		} else {
			offer.Objection = $"{p.Name}: \"I haven't heard a note of it.\"";
			float altered = Mathf.Max(BackerMinimumPrincipal, Mathf.Floor(full * BackerAlteredShare / 50f) * 50f);
			if (altered > BackerMinimumPrincipal)
				offer.Terms.Add(new("Offer him a better rate", altered, BackerAlteredRate, $"Unheard, he'll go to ${altered:N0} at {BackerAlteredRate:P0} a year."));
		}
		if (full > BackerMinimumPrincipal || offer.Terms.Count == 0)
			offer.Terms.Add(new("Take a smaller sum", BackerMinimumPrincipal, BackerRate, $"${BackerMinimumPrincipal:N0} at {BackerRate:P0}: enough to keep going, less to pay back."));
		return (null, offer);
	}

	/// <summary>Why a meeting can't happen now, or null. Pure; the same gate the verb enforces.</summary>
	public string BackerMeetingBlock(BackerProspect p) {
		if (Label == null) return "You don't have a label yet.";
		if (backer != null) return backer.Repaid ? $"You've already had {backer.BackerName}'s money and paid it back. One backer is all you get." : $"You already owe {backer.BackerName}.";
		if (p == null) return "Nobody you know is in a position to back you.";
		if (PendingBackerOffer != null) return $"{p.Name}'s offer is open until {PendingBackerOffer.Expires.ToShortString()}.";
		if (backerRefusal is { } r && r.Evidence == BackerEvidence()) return $"{p.Name} said no on {LocalSceneRoomService.Date(r.Day).ToShortString()}. Nothing's changed since.";
		var clock = TimeManager.Instance;
		if (clock == null) return "No clock.";
		if (p.Source == BackerSource.ClubOwner) {
			var room = SceneRoomCatalog.Get(p.CounterpartyId);
			if (LocalScenePersistenceService.SceneIdFor(CurrentCityId) != room.PlaceId) return $"{p.Name} is at {room.Name}; you have to be in town.";
			if (clock.CurrentHour < BookerFromHour || clock.CurrentHour + BackerMeetingHours > room.CloseHour) return $"{p.Name} is in from {BookerFromHour - 12} p.m.";
		} else {
			if (!AtHome) return $"{p.Name} comes to the office; you have to be home.";
			if (clock.CurrentHour < BackerOfficeFromHour || clock.CurrentHour + BackerMeetingHours > BackerOfficeUntilHour)
				return $"{p.Name} sees people from {BackerOfficeFromHour} a.m. to {BackerOfficeUntilHour - 12} p.m.";
		}
		return null;
	}

	/// <summary>§4.3: a 2h meeting with the backer. A no is recorded against the evidence it was given on; a yes leaves an
	/// offer open for two weeks with one or more sets of terms, each tied to the answer that earned it.</summary>
	public bool MeetBacker(out string message) {
		var p = BackerProspectFor();
		message = BackerMeetingBlock(p);
		if (message != null) return false;
		if (!Require(BackerMeetingHours, out message)) return false;
		Spend(BackerMeetingHours);
		var (refusal, offer) = EvaluateBacker(p);
		if (refusal != null) {
			backerRefusal = (LocalSceneRoomService.Day(BackerToday), BackerEvidence(), refusal);
			backerOffer = null;
			message = refusal;
		} else {
			backerOffer = offer;
			backerRefusal = null;
			message = $"Sat down with {p.Name}, {p.Title}. " + (offer.Objection != null ? offer.Objection + " " : "") +
				string.Join(" ", offer.Terms.Select(t => $"{t.Answer}: ${t.Principal:N0} at {t.Rate:P0}.")) +
				$" The offer stands until {offer.Expires.ToShortString()}.";
		}
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Sign one of the open offer's terms. The principal is paid over today as financing (not revenue).</summary>
	public bool AcceptBackerTerms(int index, out string message) {
		var offer = PendingBackerOffer;
		var p = BackerProspectFor();
		if (backer != null) { message = "You already have a backer."; return false; }
		if (offer == null || p == null) { message = "No offer is open; meet the backer first."; return false; }
		if (index < 0 || index >= offer.Terms.Count) { message = "Pick one of his terms."; return false; }
		var t = offer.Terms[index];
		var today = BackerToday;
		var first = today;
		for (int i = 0; i < BackerGraceFirsts; i++) first = FirstOfNextMonth(first);
		backer = new BackerAgreement {
			Source = p.Source, BackerName = p.Name, CounterpartyId = p.CounterpartyId, Principal = t.Principal, Rate = t.Rate,
			Signed = today, FirstDue = first, LastAccrual = today,
		};
		backer.Add(today, BackerLedgerKind.Disbursed, t.Principal);
		Label.cashReserves += t.Principal;
		backerOffer = null;
		message = $"Shook on it with {p.Name}: ${t.Principal:N0} at {t.Rate:P0} a year. Twelve payments of ${backer.Installment:N0} plus interest, the first on {first.ToShortString()}, taken on the 1st after the overhead.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Pay the backer early, in person or by cheque: interest first, then overdue principal, then principal ahead of
	/// schedule (which covers the next installments). Never more than is owed or than cash on hand.</summary>
	public bool RepayBacker(float amount, out string message) {
		if (backer == null || backer.Repaid) { message = "You don't owe a backer anything."; return false; }
		if (Label.cashReserves <= 0f) { message = "There's nothing in the till to pay him with."; return false; }
		var today = BackerToday;
		// Bring interest up to today first so the payment clears what is actually owed.
		int days = DaysBetween(backer.LastAccrual, today);
		if (days > 0) {
			float interest = R2(backer.Outstanding * backer.Rate * days / 365f);
			if (interest > 0f) { backer.InterestAccrued = R2(backer.InterestAccrued + interest); backer.Add(today, BackerLedgerKind.InterestAccrued, interest); }
			backer.LastAccrual = today;
		}
		float pay = R2(Mathf.Min(Mathf.Min(amount, Label.cashReserves), backer.Outstanding + backer.InterestDue));
		if (pay <= 0f) { message = "Nothing to pay."; return false; }
		float toInterest = R2(Mathf.Min(pay, backer.InterestDue)), toPrincipal = R2(pay - toInterest);
		if (toInterest > 0f) { backer.InterestPaid = R2(backer.InterestPaid + toInterest); backer.Add(today, BackerLedgerKind.InterestPaid, toInterest); Label.monthlyExpenses += toInterest; }
		if (toPrincipal > 0f) { backer.PrincipalPaid = R2(backer.PrincipalPaid + toPrincipal); backer.Add(today, BackerLedgerKind.PrincipalPaid, toPrincipal); }
		Label.cashReserves -= pay;
		if (backer.PrincipalArrears(today) <= 0.005f) backer.ConsecutiveMissed = 0;
		backer.CloseIfRepaid(today);
		message = backer.Repaid ? $"Paid {backer.BackerName} the last ${pay:N2}. You're square." : $"Paid {backer.BackerName} ${pay:N2}; ${backer.Outstanding:N2} still owed.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>The 1st-of-month settlement, run after the overhead and before the bank's check (OnMonthChanged).</summary>
	private void SettleBacker(GameDate date) {
		if (backer == null || backer.Repaid) return;
		float cash = Label.cashReserves, interestBefore = backer.InterestPaid;
		var (paid, missed) = backer.Settle(date, ref cash);
		Label.cashReserves = cash;
		Label.monthlyExpenses += R2(backer.InterestPaid - interestBefore);
		if (paid > 0f) Note($"Paid {backer.BackerName} ${paid:N2} on the 1st{(backer.Repaid ? "; that clears it." : $" (${backer.Outstanding:N2} still owed).")}");
		if (!missed) return;
		float arrears = R2(backer.PrincipalArrears(date) + backer.InterestDue);
		Note($"Couldn't make {backer.BackerName}'s payment: ${arrears:N2} in arrears. {BackerMissConsequence()}");
		switch (backer.Source) {
			case BackerSource.JukeboxOperator:
				if (EnsureStops().TryGetValue(backer.CounterpartyId ?? "", out var op)) op.Relationship = Mathf.Max(0f, op.Relationship - BackerOperatorRelationshipPerMiss);
				break;
			case BackerSource.PrivateBacker:
				Label.reputation = Mathf.Max(0f, Label.reputation - BackerReputationPerMiss);
				break;
			case BackerSource.MerchantLender:
				if (backer.ConsecutiveMissed >= BackerSeizeAfterMisses) SeizeCollateral(date);
				break;
		}
	}

	private string BackerMissConsequence() => backer.Source switch {
		BackerSource.ClubOwner => $"Word gets to the booker at {SceneRoomCatalog.Get(backer.CounterpartyId)?.Name ?? "his room"}.",
		BackerSource.JukeboxOperator => "He'll remember it on his route.",
		BackerSource.PrivateBacker => "The trade hears about it.",
		BackerSource.MerchantLender => backer.ConsecutiveMissed >= BackerSeizeAfterMisses ? "He's coming for the pledged stock." : "Miss another and he takes pledged stock.",
		_ => "",
	};

	/// <summary>The merchant lender takes copies on hand at the dealer price against the arrears, largest stock first.
	/// Stock leaves the books as security realised, not a sale: no units, no chart.</summary>
	private void SeizeCollateral(GameDate date) {
		float arrears = R2(backer.InterestDue + backer.PrincipalArrears(date));
		int need = Mathf.CeilToInt(arrears / DealerPrice - 0.0001f);
		int took = 0;
		foreach (var (id, stock) in inventory.Where(kv => kv.Value.Remaining > 0).OrderByDescending(kv => kv.Value.Remaining).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList()) {
			if (need <= 0) break;
			int copies = Math.Min(need, stock.Remaining);
			stock.Remaining -= copies;
			need -= copies; took += copies;
			float value = R2(Mathf.Min(copies * DealerPrice, R2(backer.InterestDue + backer.PrincipalArrears(date))));
			float toInterest = R2(Mathf.Min(value, backer.InterestDue)), toPrincipal = R2(value - toInterest);
			if (toInterest > 0f) { backer.InterestPaid = R2(backer.InterestPaid + toInterest); backer.Add(date, BackerLedgerKind.CollateralInterest, toInterest, copies, id); }
			if (toPrincipal > 0f) { backer.PrincipalPaid = R2(backer.PrincipalPaid + toPrincipal); backer.Add(date, BackerLedgerKind.CollateralPrincipal, toPrincipal, toInterest > 0f ? 0 : copies, id); }
		}
		if (took == 0) { Note($"{backer.BackerName} came for the pledged stock, but there was none on hand. The arrears stand."); return; }
		backer.ConsecutiveMissed = 0;
		backer.CloseIfRepaid(date);
		Note($"{backer.BackerName} took {took:N0} copies of your pressed stock against the arrears (${DealerPrice:F2} a copy).");
	}

	/// <summary>What the next 1st's settlement would take from cash after overhead, on a copy of the agreement.</summary>
	public float ForecastBackerPayment(GameDate check, float cashAfterOverhead) {
		if (backer == null || backer.Repaid) return 0f;
		float cash = cashAfterOverhead;
		return backer.Copy().Settle(check, ref cash).Paid;
	}

	/// <summary>Club owner's arrears read at his own room's booker: subtracted from bookings, residencies and table rights.</summary>
	public float BackerRoomPenalty(string roomId) =>
		backer != null && !backer.Repaid && backer.Source == BackerSource.ClubOwner && backer.CounterpartyId == roomId
			? BackerRoomPenaltyMax * (1f - backer.Standing) : 0f;

	// ── Checks ──────────────────────────────────────────────────────────────────────────────────────

	/// <summary>Checks only: set a founding archetype after founding, so one world can test every source.</summary>
	internal void SetArchetypeForChecks(FoundingArchetype archetype) { Archetype = archetype; backer = null; backerOffer = null; backerRefusal = null; }

	/// <summary>Checks and the save round-trip only: put an agreement (and an offer) in place without a meeting.</summary>
	internal void StageBackerForChecks(BackerAgreement agreement, BackerOffer offer = null) { backer = agreement; backerOffer = offer; }

	/// <summary>Checks only: a fresh agreement as <see cref="AcceptBackerTerms"/> would sign it, without touching cash.</summary>
	internal static BackerAgreement NewAgreementForChecks(BackerSource source, string name, string counterparty, float principal, float rate, GameDate signed) {
		var first = signed;
		for (int i = 0; i < BackerGraceFirsts; i++) first = FirstOfNextMonth(first);
		var a = new BackerAgreement { Source = source, BackerName = name, CounterpartyId = counterparty, Principal = principal, Rate = rate,
			Signed = signed, FirstDue = first, LastAccrual = signed };
		a.Add(signed, BackerLedgerKind.Disbursed, principal);
		return a;
	}

	internal static GameDate NextFirstForChecks(GameDate d) => FirstOfNextMonth(d);

	// ── Save ────────────────────────────────────────────────────────────────────────────────────────

	private BackerSaveData CaptureBacker() {
		if (backer == null && backerOffer == null && backerRefusal == null) return null;
		var data = new BackerSaveData();
		if (backer != null) {
			data.HasAgreement = true;
			data.Source = (int)backer.Source; data.BackerName = backer.BackerName; data.CounterpartyId = backer.CounterpartyId;
			data.Principal = backer.Principal; data.Rate = backer.Rate; data.Installments = backer.Installments;
			data.Signed = DateSave.From(backer.Signed); data.FirstDue = DateSave.From(backer.FirstDue); data.LastAccrual = DateSave.From(backer.LastAccrual);
			data.PrincipalPaid = backer.PrincipalPaid; data.InterestAccrued = backer.InterestAccrued; data.InterestPaid = backer.InterestPaid;
			data.Missed = backer.Missed; data.ConsecutiveMissed = backer.ConsecutiveMissed; data.Standing = backer.Standing; data.Repaid = backer.Repaid;
			data.Ledger = backer.Ledger.Select(e => new BackerLedgerSaveData { Date = DateSave.From(e.Date), Kind = (int)e.Kind, Amount = e.Amount, Copies = e.Copies, RecordId = e.RecordId }).ToList();
		}
		if (backerOffer != null) {
			data.OfferMade = DateSave.From(backerOffer.Made); data.OfferExpires = DateSave.From(backerOffer.Expires); data.OfferObjection = backerOffer.Objection;
			data.OfferTerms = backerOffer.Terms.Select(t => new BackerTermsSaveData { Answer = t.Answer, Principal = t.Principal, Rate = t.Rate, Why = t.Why }).ToList();
		}
		if (backerRefusal is { } r) { data.HasRefusal = true; data.RefusalDay = r.Day; data.RefusalEvidence = r.Evidence; data.RefusalReason = r.Reason; }
		return data;
	}

	private void RestoreBacker(BackerSaveData data) {
		backer = null; backerOffer = null; backerRefusal = null;
		if (data == null) return;
		if (data.HasAgreement && data.Signed?.Valid == true) {
			backer = new BackerAgreement {
				Source = (BackerSource)data.Source, BackerName = data.BackerName, CounterpartyId = data.CounterpartyId,
				Principal = data.Principal, Rate = data.Rate, Installments = data.Installments > 0 ? data.Installments : BackerInstallments,
				Signed = data.Signed.ToDate(), FirstDue = data.FirstDue.ToDate(), LastAccrual = data.LastAccrual.ToDate(),
				PrincipalPaid = data.PrincipalPaid, InterestAccrued = data.InterestAccrued, InterestPaid = data.InterestPaid,
				Missed = data.Missed, ConsecutiveMissed = data.ConsecutiveMissed, Standing = data.Standing, Repaid = data.Repaid,
				Ledger = (data.Ledger ?? new()).Where(e => e?.Date?.Valid == true).Select(e => new BackerLedgerEntry {
					Date = e.Date.ToDate(), Kind = (BackerLedgerKind)e.Kind, Amount = e.Amount, Copies = e.Copies, RecordId = e.RecordId }).ToList(),
			};
			if (!backer.LedgerAgrees()) GD.PushWarning($"Backer ledger disagrees with saved balances for {backer.BackerName}; keeping the saved balances.");
		}
		if (data.OfferMade?.Valid == true && data.OfferTerms?.Count > 0)
			backerOffer = new BackerOffer { Made = data.OfferMade.ToDate(), Expires = data.OfferExpires.ToDate(), Objection = data.OfferObjection,
				Terms = data.OfferTerms.Select(t => new BackerTerms(t.Answer, t.Principal, t.Rate, t.Why)).ToList() };
		if (data.HasRefusal) backerRefusal = (data.RefusalDay, data.RefusalEvidence, data.RefusalReason);
	}
}

/// <summary>A calendar date as saved. Invalid (year 0) when absent.</summary>
public sealed class DateSave {
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public bool Valid => Year > 0 && Month > 0 && Day > 0;
	public static DateSave From(GameDate d) => new() { Year = d.year, Month = d.month, Day = d.day };
	public GameDate ToDate() => new(Year, Month, Day);
}

/// <summary>Early-game Phase 2: the backer agreement, its ledger, an open offer and a standing refusal. Absent in older saves.</summary>
public sealed class BackerSaveData {
	public bool HasAgreement { get; set; }
	public int Source { get; set; }
	public string BackerName { get; set; }
	public string CounterpartyId { get; set; }
	public float Principal { get; set; }
	/// <summary>Annual simple rate on outstanding principal, accrued actual/365.</summary>
	public float Rate { get; set; }
	public int Installments { get; set; }
	public DateSave Signed { get; set; }
	public DateSave FirstDue { get; set; }
	public DateSave LastAccrual { get; set; }
	public float PrincipalPaid { get; set; }
	public float InterestAccrued { get; set; }
	public float InterestPaid { get; set; }
	public int Missed { get; set; }
	public int ConsecutiveMissed { get; set; }
	public float Standing { get; set; }
	public bool Repaid { get; set; }
	public List<BackerLedgerSaveData> Ledger { get; set; } = new();
	public DateSave OfferMade { get; set; }
	public DateSave OfferExpires { get; set; }
	public string OfferObjection { get; set; }
	public List<BackerTermsSaveData> OfferTerms { get; set; }
	public bool HasRefusal { get; set; }
	public int RefusalDay { get; set; }
	public string RefusalEvidence { get; set; }
	public string RefusalReason { get; set; }
}

public sealed class BackerLedgerSaveData {
	public DateSave Date { get; set; }
	public int Kind { get; set; }
	public float Amount { get; set; }
	public int Copies { get; set; }
	public string RecordId { get; set; }
}

public sealed class BackerTermsSaveData {
	public string Answer { get; set; }
	public float Principal { get; set; }
	public float Rate { get; set; }
	public string Why { get; set; }
}
