using System;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 2 gate (§4.3): one backer agreement. Sources come from the world (no family money);
/// a refusal stands on unchanged evidence; an offer's terms answer the objection; the principal is financing; payments
/// fall on the 1st after the overhead and before the bank's check, in the stated order, from cash above zero only, so a
/// payment can't turn a check red; the forecast matches what the clock does; a miss is arrears and lost standing, read
/// by the club owner's booker; the merchant lender takes pledged stock after two misses; early repayment closes it;
/// the ledger always recomputes the balances; and there is no second agreement.
///   --path . SimTools/BackerChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints BACKER_CHECKS_PASS / _FAIL.
/// </summary>
public partial class BackerChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(desk.FoundLabel("Backer Checks", "detroit", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			var label = desk.Label;

			// Sources: the Ex-Musician's backer is the man who runs a real home-scene club, not family.
			var p = desk.BackerProspectFor();
			Check(p != null && p.Source == PlayerDesk.BackerSource.ClubOwner && SceneRoomCatalog.Get(p.CounterpartyId)?.Kind == SceneRoomKind.Club,
				$"Ex-Musician's backer runs a home club: {p?.Name}, {p?.Title}");
			var room = SceneRoomCatalog.Get(p.CounterpartyId);
			Check(p.Name == room.ContactName && Near(p.Capacity, 500f), "He is that room's man, good for $500 at most");

			// Hours and place: the club owner is in from 2 p.m.; a morning ask costs nothing.
			int hour = clock.CurrentHour;
			Check(!desk.MeetBacker(out var early) && early.Contains("p.m.") && clock.CurrentHour == hour, "Morning meeting refused without spending time: " + early);
			Check(desk.PassTime(desk.HoursUntil(PlayerDesk.BookerFromHour), out _), "Wait for the afternoon");

			// No act: a no, and it stands until the evidence changes.
			var row = desk.CashOptions().FirstOrDefault(o => o.Route == "Ask a backer");
			Check(row != null && !row.Executable && row.Certainty == PlayerDesk.CashCertainty.Blocked && row.Why.Contains("signed act"),
				"CASH OPTIONS lists the ask, blocked until there's an act: " + row?.Why);
			Check(desk.MeetBacker(out var noAct) && noAct.Contains("act signed") && clock.CurrentHour == PlayerDesk.BookerFromHour + PlayerDesk.BackerMeetingHours,
				"The meeting takes 2h and he wants an act: " + noAct);
			Check(desk.Backer == null && desk.PendingBackerOffer == null && Near(label.cashReserves, 800f), "No money on a no");
			hour = clock.CurrentHour;
			Check(!desk.MeetBacker(out var stands) && stands.Contains("said no") && clock.CurrentHour == hour, "Asking again on the same evidence is refused, free: " + stands);
			row = desk.CashOptions().First(o => o.Route == "Ask a backer");
			Check(!row.Executable && row.Certainty == PlayerDesk.CashCertainty.Blocked, "CASH OPTIONS shows the standing no as blocked");

			// Sign an act: the evidence changed, so he'll hear you again. He backs the founder he knows, whatever the music.
			int year = clock.CurrentDate.year;
			var act = ArtistManager.Instance.GetAllArtists().First(a => string.IsNullOrEmpty(a.labelId)
				&& !room.Families.Contains(GenreCatalog.Get(GenreCatalog.MapLegacy(a.primaryGenre, year)).Family));
			act.labelId = label.labelId; label.roster.Add(act);
			row = desk.CashOptions().First(o => o.Route == "Ask a backer");
			Check(row.Executable && row.Certainty == PlayerDesk.CashCertainty.Speculative && Near(row.Expected, 500f), "With an act, the ask is live: speculative, up to his means");
			Check(desk.MeetBacker(out var heard), "Meeting again once an act is signed: " + heard);
			var offer = desk.PendingBackerOffer;
			Check(offer != null && offer.Objection != null && offer.Objection.Contains("haven't heard"), "Unheard, he objects: " + offer?.Objection);
			Check(offer.Terms.All(t => t.Answer != "Play him the record") && offer.Terms.All(t => t.Principal >= PlayerDesk.BackerMinimumPrincipal && t.Principal <= p.Capacity),
				"No full-budget terms without a record; every sum within his means: " + string.Join("; ", offer.Terms.Select(t => $"{t.Answer} ${t.Principal}@{t.Rate:P0}")));
			Check(offer.Terms.Any(t => t.Answer == "Take a smaller sum" && Near(t.Principal, 300f) && Near(t.Rate, PlayerDesk.BackerRate)), "He'll put up the smaller sum");
			Check(desk.CashOptions().Any(o => o.Route == "Backer's offer" && o.Certainty == PlayerDesk.CashCertainty.Accepted && o.Executable), "CASH OPTIONS shows the open offer as agreed");

			// Sign: principal is financing, paid over today.
			float cash0 = label.cashReserves, revenue0 = label.monthlyRevenue;
			int small = offer.Terms.FindIndex(t => t.Answer == "Take a smaller sum");
			Check(desk.AcceptBackerTerms(small, out var signed), "Sign the smaller sum: " + signed);
			var loan = desk.Backer;
			Check(Near(label.cashReserves - cash0, 300f) && Near(label.monthlyRevenue, revenue0), "Cash +$300, and none of it booked as revenue");
			Check(loan.Ledger.Count == 1 && loan.Ledger[0].Kind == PlayerDesk.BackerLedgerKind.Disbursed && loan.LedgerAgrees(), "One ledger line: the disbursement");
			var f1 = PlayerDesk.NextFirstForChecks(clock.CurrentDate);
			Check(loan.FirstDue.Equals(PlayerDesk.NextFirstForChecks(PlayerDesk.NextFirstForChecks(f1))), $"First payment on the third 1st: {loan.FirstDue}");
			Check(!desk.MeetBacker(out var second) && second.Contains("already owe") && desk.CashOptions().All(o => o.Route != "Ask a backer"), "No second ask: " + second);

			// Grace months: interest accrues, nothing is taken.
			label.cashReserves = 2000f;
			ToNextFirst(clock);
			Check(loan.Ledger.Count(e => e.Kind == PlayerDesk.BackerLedgerKind.InterestAccrued) == 1 && Near(loan.PrincipalPaid, 0f) && Near(loan.InterestPaid, 0f),
				$"First 1st: {loan.InterestAccrued:F2} interest accrued, nothing paid");
			ToNextFirst(clock);
			Check(Near(loan.PrincipalPaid, 0f), "Second 1st: still nothing paid");

			// The first payment: forecast before, the real clock after, same number; taken after overhead, before the check.
			(GameDate Date, int Days, float Overhead, float CashAfter, bool WouldClose, string Outcome, float BackerPayment) forecast = default;
			int days = DaysBetween(loan.Signed, loan.FirstDue);
			ToNextFirst(clock, () => { label.cashReserves = 2000f; forecast = desk.NextSolvencyCheck(label.cashReserves); });
			float paidThatDay = loan.Ledger.Where(e => e.Date.Equals(loan.FirstDue) && e.Kind is PlayerDesk.BackerLedgerKind.InterestPaid or PlayerDesk.BackerLedgerKind.PrincipalPaid).Sum(e => e.Amount);
			Check(Near(forecast.BackerPayment, paidThatDay), $"Forecast payment {forecast.BackerPayment:F2} matches the clock's {paidThatDay:F2}");
			Check(Near(loan.PrincipalPaid, 25f) && Near(loan.InterestPaid, loan.InterestAccrued) && Near(loan.InterestAccrued, 300f * PlayerDesk.BackerRate * days / 365f, .03f),
				$"Paid a twelfth ($25) and {loan.InterestPaid:F2} interest on {days} days at 6%");
			int order = loan.Ledger.FindIndex(e => e.Date.Equals(loan.FirstDue) && e.Kind == PlayerDesk.BackerLedgerKind.InterestPaid);
			Check(order >= 0 && order < loan.Ledger.FindIndex(e => e.Date.Equals(loan.FirstDue) && e.Kind == PlayerDesk.BackerLedgerKind.PrincipalPaid), "Interest is paid before principal");
			Check(loan.Missed == 0 && loan.LedgerAgrees() && Near(loan.Standing, 1f), "On time, standing full, ledger agrees");

			// A miss: nothing above zero after the overhead. Arrears and standing, never a red check of its own.
			var dry = forecast;
			var missDate = PlayerDesk.NextFirstForChecks(clock.CurrentDate);
			ToNextFirst(clock, () => { label.cashReserves = label.GetMonthlyOverhead(); dry = desk.NextSolvencyCheck(label.cashReserves); });
			Check(Near(dry.BackerPayment, 0f) && dry.Outcome == "in the black", "Forecast: no payment, and the check stays black");
			Check(desk.MonthsInTheRed == 0 && label.cashReserves >= -0.01f, $"The check stayed black (cash {label.cashReserves:F2})");
			Check(loan.Missed == 1 && loan.ConsecutiveMissed == 1 && Near(loan.Standing, 1f - PlayerDesk.BackerStandingPerMiss) && Near(loan.PrincipalArrears(clock.CurrentDate), 25f),
				$"One miss: $25 overdue, standing {loan.Standing:P0}");
			Check(loan.Ledger.Any(e => e.Date.Equals(missDate) && e.Kind == PlayerDesk.BackerLedgerKind.Missed) && loan.LedgerAgrees(), "The miss is on the ledger");
			float penalty = desk.BackerRoomPenalty(room.Id);
			Check(Near(penalty, PlayerDesk.BackerRoomPenaltyMax * PlayerDesk.BackerStandingPerMiss, .001f) && Near(desk.TableRightsChance(room), .20f - penalty, .001f),
				$"His booker reads it: table rights {desk.TableRightsChance(room):F3} (was .200)");
			Check(desk.BackerRoomPenalty(SceneRoomCatalog.ForPlace(room.PlaceId).First(r => r.Id != room.Id).Id) == 0f, "Other rooms don't");

			// Back in funds: interest, the overdue installment, then this month's.
			ToNextFirst(clock, () => label.cashReserves = 2000f);
			Check(Near(loan.PrincipalPaid, 75f) && loan.ConsecutiveMissed == 0 && Near(loan.InterestDue, 0f) && Near(loan.Standing, 1f - PlayerDesk.BackerStandingPerMiss + PlayerDesk.BackerStandingPerPayment),
				$"Caught up: $75 principal paid, standing {loan.Standing:P0}");

			// Pay it off early: interest to the day, then principal; no second agreement after.
			Check(desk.RepayBacker(10000f, out var payoff) && loan.Repaid && Near(loan.Outstanding, 0f) && Near(loan.InterestDue, 0f) && loan.LedgerAgrees(), "Paid off early: " + payoff);
			Check(desk.BackerRoomPenalty(room.Id) == 0f, "A paid-off backer puts no weight on the booker");
			int ledgerLines = loan.Ledger.Count;
			ToNextFirst(clock);
			Check(loan.Ledger.Count == ledgerLines, "Nothing more is taken on the 1st");
			Check(!desk.MeetBacker(out var again) && again.Contains("One backer"), "One backer per label: " + again);

			// The other sources.
			desk.SetArchetypeForChecks(FoundingArchetype.PromoMan);
			var op = desk.BackerProspectFor();
			Check(op?.Source == PlayerDesk.BackerSource.JukeboxOperator && desk.StopsInCity("detroit").Any(s => s.StopId == op.CounterpartyId && s.Kind == PlayerDesk.StopKind.Op),
				$"Promo Man's backer is the home jukebox operator: {op?.Name}");
			desk.SetArchetypeForChecks(FoundingArchetype.TradeInsider);
			var priv = desk.BackerProspectFor();
			Check(priv?.Source == PlayerDesk.BackerSource.PrivateBacker && Near(priv.Capacity, 800f) && priv.Name == desk.BackerProspectFor().Name, $"Trade Insider's private backer, stable name: {priv?.Name}");
			Pawn(desk, clock, label);

			// With a record to play him, he puts up the budget: need, within his means. Last, since a released record sells.
			desk.SetArchetypeForChecks(FoundingArchetype.ExMusician);
			var rec = new Record { recordId = "backer_probe", title = "Backer Probe", artistId = act.artistId, artistName = act.stageName,
				format = ReleaseFormat.Single, primaryGenre = act.primaryGenre, hookStrength = .5f, productionQuality = .5f, originality = .5f, danceability = .5f };
			Check(CompetitorManager.Instance.ReleasePlayerRecord(label, act, rec, 0f, 0f, clock.CurrentDate), "A record to play him");
			var heardOffer = desk.EvaluateBacker(p).Offer;
			Check(heardOffer.Objection == null && heardOffer.Terms.Any(t => t.Answer == "Play him the record" && Near(t.Principal, Mathf.Clamp(desk.BackerNeed(), 300f, p.Capacity))),
				$"Heard, he funds the budget: {string.Join("; ", heardOffer.Terms.Select(t => $"{t.Answer} ${t.Principal}"))} (need ${desk.BackerNeed()})");

			GD.Print($"BACKER_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PushError($"BACKER_CHECKS_FAIL after {checks} checks: {e}");
			GetTree().Quit(3);
		}
	}

	/// <summary>The merchant lender: two misses in a row and he takes pledged stock at the dealer price; none on hand, the
	/// arrears stand. A seizure moves no cash and books no sale.</summary>
	private void Pawn(PlayerDesk desk, TimeManager clock, AILabel label) {
		desk.SetArchetypeForChecks(FoundingArchetype.PawnShopOwner);
		var p = desk.BackerProspectFor();
		Check(p?.Source == PlayerDesk.BackerSource.MerchantLender && p.Wants.Contains("pressed stock"), $"Pawn-Shop Owner's backer lends against stock: {p?.Name}");
		// Signed long enough ago that the next 1st is the first payment.
		var d = clock.CurrentDate;
		var signed = d.month > 2 ? new GameDate(d.year, d.month - 2, 15) : new GameDate(d.year - 1, d.month + 10, 15);
		var loan = PlayerDesk.NewAgreementForChecks(PlayerDesk.BackerSource.MerchantLender, p.Name, null, 600f, PlayerDesk.BackerRate, signed);
		Check(loan.FirstDue.Equals(PlayerDesk.NextFirstForChecks(d)), $"Staged agreement's first payment is the next 1st ({loan.FirstDue})");
		desk.StageBackerForChecks(loan);
		desk.DebugSetPressStock("pawn_pledge", 200, 0, 200, 0f);

		void Dry() => label.cashReserves = label.GetMonthlyOverhead();
		ToNextFirst(clock, Dry);
		Check(loan.ConsecutiveMissed == 1 && desk.StockFor("pawn_pledge").Remaining == 200, "One miss: he waits");
		ToNextFirst(clock, Dry);
		var seized = loan.Ledger.Where(e => e.Kind is PlayerDesk.BackerLedgerKind.CollateralInterest or PlayerDesk.BackerLedgerKind.CollateralPrincipal).ToList();
		int copies = seized.Sum(e => e.Copies);
		Check(seized.Count > 0 && 200 - desk.StockFor("pawn_pledge").Remaining == copies && copies > 0, $"Second miss: he took {copies} copies");
		Check(Near(seized.Sum(e => e.Amount), Mathf.Min(copies * PlayerDesk.DealerPrice, seized.Sum(e => e.Amount)), .02f) && Near(loan.PrincipalArrears(clock.CurrentDate) + loan.InterestDue, 0f, .02f),
			$"At ${PlayerDesk.DealerPrice:F2} a copy they clear the arrears ({seized.Sum(e => e.Amount):F2})");
		Check(label.cashReserves >= -0.01f && label.cashReserves <= 0.01f && loan.ConsecutiveMissed == 0 && loan.LedgerAgrees(), "No cash moved; the count resets; the ledger agrees");

		// No stock on hand: the arrears stand.
		desk.DebugSetPressStock("pawn_pledge", 0, 0, 200, 0f);
		ToNextFirst(clock, Dry);
		ToNextFirst(clock, Dry);
		Check(loan.ConsecutiveMissed == 2 && loan.PrincipalArrears(clock.CurrentDate) > 0f && loan.LedgerAgrees(), $"Nothing to take: ${loan.PrincipalArrears(clock.CurrentDate):F2} overdue stands");
	}

	/// <summary>End days until the clock turns over to the next 1st. <paramref name="beforeLastDay"/> runs just before
	/// the day that crosses into it.</summary>
	private static void ToNextFirst(TimeManager clock, Action beforeLastDay = null) {
		var target = PlayerDesk.NextFirstForChecks(clock.CurrentDate);
		while (!clock.CurrentDate.Equals(target)) {
			if (beforeLastDay != null && PlayerDesk.NextFirstForChecks(clock.CurrentDate).Equals(target) && DaysBetween(clock.CurrentDate, target) == 1) beforeLastDay();
			clock.EndDay();
		}
	}

	private static int DaysBetween(GameDate a, GameDate b) =>
		(int)(new DateTime(b.year, b.month, b.day) - new DateTime(a.year, a.month, a.day)).TotalDays;
}
