using System;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 5 gate (§5): one master-deal state machine. Buyers are real labels with the sound and
/// the cash; a call gives public answers only; a posted tape is answered weeks later if anyone listens; a visit needs the
/// town, the hours and a copy, and an appointment gets you seen; the ear never moves on unchanged evidence, so a pass
/// stands; signing pays from the buyer's own cash and takes the title out of every player channel; the buyer releases an
/// unreleased master itself (one runtime record, tagged, not the player's); its units accrue the royalty, recouping the
/// advance first, and the lease leaves the act's royalty to the player, settled on the statement; the lease reverts with
/// its chart history; a buyer that never releases leaves the advance and returns the master; a breakout brings at most
/// one call per change of evidence; a pickup takes the stock at cost, and a sale with the act moves the act.
///   --path . SimTools/MasterDealChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints MASTER_DEAL_CHECKS_PASS / _FAIL.
/// </summary>
public partial class MasterDealChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(desk.FoundLabel("Master Checks", "detroit", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			var label = desk.Label;
			label.cashReserves = 5000f;
			int Today() => LocalSceneRoomService.Day(clock.CurrentDate);

			// An act in the sound of a nearby label with room and money: the buyer we'll work with.
			var reach = desk.ReachableCities().Select(c => c.cityId).Append(label.homeCityId).ToHashSet();
			int year = clock.CurrentDate.year;
			SimulatedArtist act = null;
			foreach (var l in CompetitorManager.Instance.GetOperatingLabels().Where(l => !l.isPlayerOwned && l.IsActive && reach.Contains(l.homeCityId ?? "")
					&& l.tier is LabelTier.Independent or LabelTier.MidTier && l.CurrentRosterSize < l.maxRosterSize && l.CanAffordToSign(5000f)
					&& l.preferredGenres?.Length > 0).OrderBy(l => l.labelId, StringComparer.Ordinal)) {
				act = ArtistManager.Instance.GetAllArtists().FirstOrDefault(a => string.IsNullOrEmpty(a.labelId) && a.isActive && a.members?.Count > 0
					&& a.primaryGenre == l.preferredGenres[0]);
				if (act != null) break;
			}
			Check(act != null, $"An unsigned act in a nearby label's sound: {act?.stageName} ({act?.primaryGenre})");
			act.labelId = label.labelId; label.roster.Add(act);
			act.unrecoupedAdvance = 100f;
			act.royaltyRate = .05f;   // a hand-signed act has no contract rate of its own
			float actEarned0 = act.totalRoyaltyEarnings;
			var strong = desk.StageShelfMasterForChecks(act, "chk_strong", "Strong Side", .95f, .95f);
			var weak = desk.StageShelfMasterForChecks(act, "chk_weak", "Weak Side", .05f, .05f);
			var plain = desk.StageShelfMasterForChecks(act, "chk_plain", "Plain Side", .50f, .40f);

			// Buyers: real, active AI labels whose sound fits, never the player; unblocked first.
			var buyers = desk.MasterBuyersFor("chk_strong", int.MaxValue);
			Check(buyers.Count > 0 && buyers.All(b => !b.Label.isPlayerOwned && b.Label.IsActive), $"{buyers.Count} real buyers in its sound");
			Check(buyers.SkipWhile(b => b.Block == null).All(b => b.Block != null), "Labels that can pay come before ones that can't");
			var x = buyers.FirstOrDefault(b => b.Block == null && b.Reachable && b.Label.tier is LabelTier.Independent or LabelTier.MidTier
				&& b.Label.CurrentRosterSize < b.Label.maxRosterSize && b.Label.CanAffordToSign(5000f)
				&& desk.MasterReadForChecks(b.Label, "chk_strong", false) >= PlayerDesk.MasterBarForChecks(b.Label, b.OwnSound));
			Check(x != null, $"A reachable buyer that would hear the strong side: {x?.Label.labelName} ({x?.Label.tier}, {x?.CityName})");
			var X = x.Label;
			Check(desk.MasterReadForChecks(X, "chk_weak", false) < PlayerDesk.MasterBarForChecks(X, x.OwnSound), "...and would not hear the weak side");
			Check(Near(desk.MasterReadForChecks(X, "chk_strong", false), desk.MasterReadForChecks(X, "chk_strong", false), 0f), "The ear is a fixed read, not a reroll");
			var y = buyers.First(b => b.Block == null && b.Label != X);

			// Calls: office hours, the toll, public answers only, no repeat within four weeks.
			if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) {
				int h = clock.CurrentHour;
				Check(!desk.CallMasterBuyer(X.labelId, "chk_strong", out var early) && early.Contains("a.m.") && clock.CurrentHour == h, "Before office hours, refused free: " + early);
				Check(desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _), "Wait for the office to open");
			}
			float cash = label.cashReserves; int hour = clock.CurrentHour; float toll = desk.MasterCallToll(X);
			Check(desk.CallMasterBuyer(X.labelId, "chk_strong", out var called), "Call about the strong side: " + called);
			Check(Near(cash - label.cashReserves, toll) && clock.CurrentHour == hour + PlayerDesk.MasterCallHours, $"It costs the ${toll:F2} toll and an hour");
			var callA = desk.MasterCalls.Last();
			Check(callA.Welcome && callA.AppointmentDay is int a && a >= Today() + 3 && a <= Today() + 7, $"A welcome: send a tape, or come in on day {callA.AppointmentDay}");
			hour = clock.CurrentHour;
			Check(!desk.CallMasterBuyer(X.labelId, "chk_strong", out var again) && again.Contains("spoke") && clock.CurrentHour == hour, "A second call inside four weeks is refused free: " + again);
			Check(desk.CallMasterBuyer(X.labelId, "chk_weak", out _) && desk.MasterCalls.Last().Welcome, "A call about the weak side is just as welcome: the phone doesn't hear the record");
			var callB = desk.MasterCalls.Last();

			// Post: the dub and the postage, answered weeks later; one tape in flight per buyer and title.
			cash = label.cashReserves;
			Check(desk.PostMasterTape(y.Label.labelId, "chk_plain", out var posted), "Post a tape of the plain side to another label: " + posted);
			var post = desk.MasterPostings.Last();
			Check(Near(cash - label.cashReserves, PlayerDesk.MasterTapeDub + PlayerDesk.MasterPostage) && !post.Solicited
				&& post.AnswerDay >= Today() + 14 && post.AnswerDay <= Today() + 28, $"${PlayerDesk.MasterTapeDub + PlayerDesk.MasterPostage:F2}, unsolicited, answer on day {post.AnswerDay}");
			hour = clock.CurrentHour;
			Check(!desk.PostMasterTape(y.Label.labelId, "chk_plain", out var twice) && twice.Contains("on its way") && clock.CurrentHour == hour, "A second tape while one is in the post is refused: " + twice);

			Check(desk.CutAcetate("chk_strong", out _) && desk.CutAcetate("chk_weak", out _), "Cut acetates of both sides to play him");

			// Visit on the appointment days: in their town, in their hours, with a copy.
			int firstDay = Math.Min(callA.AppointmentDay.Value, callB.AppointmentDay.Value);
			if (desk.CurrentCityId != x.CityId) { clock.EndDay(); Check(desk.DriveTo(x.CityId, out var drove), $"Drive to {x.CityName} ahead of the appointment: {drove}"); }
			while (Today() < firstDay) clock.EndDay();
			if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _);
			hour = clock.CurrentHour;
			Check(!desk.VisitMasterBuyer(X.labelId, "chk_plain", out var noCopy) && noCopy.Contains("acetate") && clock.CurrentHour == hour, "No acetate of the plain side: refused free: " + noCopy);

			MasterDealOffer offer = null;
			for (int guard = 0; guard < 6 && (offer == null || desk.MasterPasses.All(p => p.RecordId != "chk_weak")); guard++) {
				if (Today() == callA.AppointmentDay && offer == null) {
					if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _);
					hour = clock.CurrentHour;
					Check(desk.VisitMasterBuyer(X.labelId, "chk_strong", out var heard) && clock.CurrentHour == hour + PlayerDesk.MasterVisitHours, "Play the strong side on the appointment (2h): " + heard);
					offer = desk.MasterOffers.FirstOrDefault(o => o.RecordId == "chk_strong" && o.BuyerLabelId == X.labelId);
				}
				if (Today() == callB.AppointmentDay && desk.MasterPasses.All(p => p.RecordId != "chk_weak")) {
					if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _);
					Check(desk.VisitMasterBuyer(X.labelId, "chk_weak", out var pass), "Play the weak side on its appointment: " + pass);
				}
				if (offer == null || desk.MasterPasses.All(p => p.RecordId != "chk_weak")) clock.EndDay();
			}
			Check(offer != null && offer.Route == MasterRoute.Visit && offer.ExpiresDay == Today() - (Today() - offer.MadeDay) + PlayerDesk.MasterOfferDays,
				$"An offer on the spot, open {PlayerDesk.MasterOfferDays} days: {offer?.Answer}");
			var lease = offer.Terms.First(t => t.Kind == MasterDealKind.Lease);
			Check(lease.Advance >= PlayerDesk.MasterLeaseAdvanceMin && lease.Advance <= PlayerDesk.MasterLeaseAdvanceMax
				&& (Near(lease.RoyaltyRate, PlayerDesk.MasterLeaseRate) || Near(lease.RoyaltyRate, PlayerDesk.MasterLeaseRateStrong))
				&& lease.TermWeeks == PlayerDesk.MasterLeaseWeeks && lease.ReleaseWithinDays == PlayerDesk.MasterReleaseWithinDays,
				$"Lease: ${lease.Advance} against {lease.RoyaltyRate:P0} of list, {lease.TermWeeks} weeks, release within {lease.ReleaseWithinDays} days");
			Check(!offer.Answer.Contains("0."), "The answer is words, never the hidden score: " + offer.Answer);
			Check(offer.Terms.All(t => t.Kind != MasterDealKind.SaleWithAct), "No sale with the act on an unreleased master");
			var weakPass = desk.MasterPasses.Last(p => p.RecordId == "chk_weak");
			Check(weakPass.BuyerLabelId == X.labelId && weakPass.Evidence == "unreleased", "The weak side was passed on: " + weakPass.Reason);
			hour = clock.CurrentHour;
			Check(!desk.VisitMasterBuyer(X.labelId, "chk_weak", out var stands) && stands.Contains("passed") && clock.CurrentHour == hour, "The pass stands on unchanged evidence, free: " + stands);
			var row = desk.CashOptions().FirstOrDefault(o => o.Route == "Master offer");
			Check(row != null && row.Certainty == PlayerDesk.CashCertainty.Accepted && row.Executable && Near(row.Expected, offer.Terms.Max(t => t.Advance)),
				"CASH OPTIONS: the open offer, agreed if you sign, signable here in their town");

			// Sign the lease from the office: the advance comes out of the buyer's cash; the master leaves the shelf.
			if (!desk.AtHome) Check(desk.DriveHome(out _), "Drive home");
			if (clock.CurrentHour + PlayerDesk.MasterSignHours > 22) { clock.EndDay(); }
			cash = label.cashReserves; float revenue = label.monthlyRevenue, buyerCash = X.cashReserves;
			int leaseIndex = offer.Terms.FindIndex(t => t.Kind == MasterDealKind.Lease);
			Check(desk.AcceptMasterOffer(offer.Id, leaseIndex, out var signed), "Sign the lease: " + signed);
			var deal = desk.MasterDeals.Last();
			Check(Near(label.cashReserves - cash, lease.Advance) && Near(label.monthlyRevenue - revenue, lease.Advance) && Near(buyerCash - X.cashReserves, lease.Advance),
				$"${lease.Advance} paid over today, out of {X.labelName}'s own cash");
			Check(deal.Status == MasterDealStatus.AwaitingRelease && desk.HoldsMasterForChecks(deal.Id) && !desk.Masters.Contains(strong) && desk.IsMasterOut("chk_strong"),
				$"Held for their release on day {deal.ReleaseDay} (by day {deal.ReleaseByDay}); off your shelf and out");
			Check(desk.ShoppableMaster("chk_strong").Block != null && !desk.CutAcetate("chk_strong", out _) && desk.MasterOffers.All(o => o.RecordId != "chk_strong"),
				"Nothing more to shop, cut or sign on it");
			Check(deal.Ledger.Count == 1 && deal.Ledger[0].Kind == MasterLedgerKind.Advance && deal.LedgerAgrees(), "One ledger line: the advance");

			// The buyer releases it itself: one runtime record, tagged, on their books.
			for (int i = 0; i < 40 && deal.Status == MasterDealStatus.AwaitingRelease; i++) clock.EndDay();
			var runtime = ChartManager.Instance.GetRecordRuntimeData("chk_strong");
			Check(deal.Status == MasterDealStatus.Out && deal.ReleasedDay == deal.ReleaseDay && !desk.HoldsMasterForChecks(deal.Id), $"{X.labelName} put it out on day {deal.ReleasedDay}");
			Check(runtime != null && runtime.baseRecord.labelId == X.labelId && !runtime.baseRecord.isPlayerOwned && runtime.baseRecord.playerMasterDealId == deal.Id,
				"Their record, tagged with the contract");
			Check(ChartManager.Instance.GetAllRecords().Count(r => r.baseRecord?.recordId == "chk_strong") == 1 && desk.ReleasedRecords.All(r => r.baseRecord.recordId != "chk_strong"),
				"One chart entry, and not in your catalogue");
			Check(deal.ExpiresDay == deal.ReleasedDay + 7 * PlayerDesk.MasterLeaseWeeks && deal.NextStatementDay == deal.ReleasedDay + PlayerDesk.MasterStatementDays, "52 weeks from their release; first statement a quarter in");

			// Their sales accrue your royalty, recouping the advance first; the act's royalty isn't theirs to pay.
			for (int i = 0; i < 28; i++) clock.EndDay();
			Check(deal.Units > 0 && deal.Units <= runtime.totalUnitsSold && deal.Units >= runtime.totalUnitsSold - runtime.unitsThisWeek,
				$"{deal.Units} units billed of {runtime.totalUnitsSold} sold (a week's lag at most)");
			Check(Near(deal.Earned, deal.Units * lease.CentsPerUnit, .05f) && Near(deal.Recouped, Mathf.Min(lease.Advance, deal.Earned)) && deal.LedgerAgrees(),
				$"${deal.Earned:F2} earned at {lease.CentsPerUnit * 100f:F1}c, ${deal.Recouped:F2} against the advance; ledger agrees");
			Check(Near(act.totalRoyaltyEarnings, actEarned0) && Near(act.unrecoupedAdvance, 100f) && deal.ActUnitsUnsettled == deal.Units,
				"The buyer paid the act nothing; you owe it on those units, on the statement");

			// The statement: what's owed is paid over; the act's royalty is settled against its advance.
			deal.NextStatementDay = Today();
			cash = label.cashReserves;
			float owed = deal.OwedNow, actDue = deal.ActUnitsUnsettled * PlayerDesk.RoyaltyPerUnit(act);
			desk.ProcessMasterDealsForChecks();
			Check(Near(deal.Paid, deal.Payable) && deal.ActUnitsUnsettled == 0, $"Statement: ${owed:F2} paid over (recouped advance pays nothing more)");
			Check(Near(deal.ActRecouped, Mathf.Min(100f, actDue), .02f) && Near(act.unrecoupedAdvance, 100f - deal.ActRecouped, .02f)
				&& Near(deal.ActPaid, Mathf.Max(0f, actDue - 100f), .02f) && Near(label.cashReserves - cash, owed - deal.ActPaid, .02f),
				$"The act's ${actDue:F2}: ${deal.ActRecouped:F2} against its advance, ${deal.ActPaid:F2} paid; ledger agrees={deal.LedgerAgrees()}");

			// Reversion: the record comes home with its chart history, off their shelves.
			deal.ExpiresDay = Today();
			int weeksOut = runtime.weeksSinceRelease;
			desk.ProcessMasterDealsForChecks();
			Check(deal.Status == MasterDealStatus.Reverted && runtime.baseRecord.labelId == label.labelId && runtime.baseRecord.isPlayerOwned && runtime.baseRecord.playerMasterDealId == null,
				"The lease ran out: the record is yours again");
			Check(desk.ReleasedRecords.Any(r => r.baseRecord.recordId == "chk_strong") && runtime.weeksSinceRelease == weeksOut
				&& runtime.regionalData.Values.All(rd => rd.unitsInStores == 0), "Same record, same history, their copies withdrawn (you have no lines)");
			Check(!desk.IsMasterOut("chk_strong") && desk.ShoppableMaster("chk_strong").Block == null && deal.Ledger.Last().Kind == MasterLedgerKind.Reverted && deal.LedgerAgrees(),
				"Yours to press, sell or shop again; ledger agrees");

			// A buyer that never releases: the master comes back and the advance is kept.
			var z = buyers.First(b => b.Block == null && b.Label != X).Label;
			desk.StageMasterOfferForChecks(new MasterDealOffer { Id = "chk_offer_weak", BuyerLabelId = z.labelId, BuyerName = z.labelName, RecordId = "chk_weak",
				Title = "Weak Side", ArtistId = act.artistId, Route = MasterRoute.Post, MadeDay = Today(), ExpiresDay = Today() + 21, Read = .5f,
				Terms = { new MasterDealTerms { Kind = MasterDealKind.Lease, Advance = 150f, RoyaltyRate = .07f, TermWeeks = 52, ReleaseWithinDays = 42 } } });
			Check(desk.AcceptMasterOffer("chk_offer_weak", 0, out var weakSigned), "Lease the weak side to another label: " + weakSigned);
			var failed = desk.MasterDeals.Last();
			cash = label.cashReserves;
			failed.ReleaseDay = Today() + 10; failed.ReleaseByDay = Today();
			desk.ProcessMasterDealsForChecks();
			Check(failed.Status == MasterDealStatus.Failed && desk.Masters.Contains(weak) && !desk.IsMasterOut("chk_weak") && Near(label.cashReserves, cash)
				&& failed.Ledger.Select(e => e.Kind).SequenceEqual(new[] { MasterLedgerKind.Advance, MasterLedgerKind.Failed }) && failed.LedgerAgrees(),
				"Not out by the deadline: back on your shelf, advance kept");

			// A breakout: at most one call per change of evidence, from a buyer that would hear a record.
			var region = runtime.regionalData.Values.First();
			region.breakoutStage = RegionalBreakoutStage.RegionalBreakout;
			Check(desk.MasterEvidence("chk_strong") == "released|breakouts=1", "The evidence changed: released, one breakout");
			desk.ProcessMasterDealsForChecks();
			var inbound = desk.MasterOffers.Where(o => o.RecordId == "chk_strong" && o.Route == MasterRoute.Inbound).ToList();
			Check(inbound.Count <= 1 && inbound.All(o => desk.MasterReadForChecks(CompetitorManager.Instance.GetLabel(o.BuyerLabelId), "chk_strong", false) >= 0f),
				inbound.Count == 1 ? $"PHONE: {inbound[0].BuyerName} called: {string.Join("; ", inbound[0].Terms.Select(t => t.Kind))}" : "No call this time (a fixed draw; likely, not guaranteed)");
			desk.ProcessMasterDealsForChecks();
			Check(desk.MasterOffers.Count(o => o.RecordId == "chk_strong" && o.Route == MasterRoute.Inbound) == inbound.Count, "No second call on the same evidence");
			foreach (var o in inbound) desk.DeclineMasterOffer(o.Id, out _);

			// A pickup lease: the record changes hands now, with the stock at cost and the calls.
			desk.DebugSetPressStock("chk_strong", 120, 20, 500, 128f);
			var shop = desk.StopsInCity(label.homeCityId).First(s => s.Kind != PlayerDesk.StopKind.Venue);
			desk.StageFieldStockForChecks("chk_strong", shop, 30);
			var (pickPass, pick) = desk.EvaluateMasterForChecks(X, "chk_strong", MasterRoute.Visit);
			bool xAccess = SceneRecruitmentService.CanAccess(X, act, ChartManager.Instance.GetCurrentChartWeek());
			Check(pick != null && pick.Terms.Take(2).Select(t => t.Kind).SequenceEqual(new[] { MasterDealKind.Lease, MasterDealKind.Sale })
				&& pick.Terms.Any(t => t.Kind == MasterDealKind.SaleWithAct) == xAccess,
				$"With a breakout, a lease and a sale{(xAccess ? " and the act" : "; not the act, they don't work its scene")}: {pickPass ?? OfferText(pick)}");
			Check(pick.Terms.All(t => t.ReleaseWithinDays == 0) && pick.Terms[1].Advance == Mathf.Round(pick.Terms[0].Advance * PlayerDesk.MasterSaleMultiple / 25f) * 25f && Near(pick.Terms[1].RoyaltyRate, 0f),
				"Already out, so no release deadline; the sale is 2.5x the lease advance, with no royalty");
			desk.StageMasterOfferForChecks(pick);
			var (copies, stockValue) = desk.MasterStockValue("chk_strong");
			Check(copies == 170 && Near(stockValue, 170 * PlayerDesk.MasterStockPerCopy), $"170 copies (office, promo, a shop) worth ${stockValue:F2} at cost");
			cash = label.cashReserves; buyerCash = X.cashReserves;
			Check(desk.AcceptMasterOffer(pick.Id, 0, out var picked), "Sign the pickup lease: " + picked);
			var pickup = desk.MasterDeals.Last();
			Check(Near(label.cashReserves - cash, pick.Terms[0].Advance + stockValue) && Near(buyerCash - X.cashReserves, pick.Terms[0].Advance + stockValue)
				&& pickup.StockCopies == 170 && pickup.Ledger.Any(e => e.Kind == MasterLedgerKind.Stock && Near(e.Amount, stockValue)), "Advance plus the stock, from their cash");
			Check(desk.StockFor("chk_strong") == null && !shop.OnHand.ContainsKey("chk_strong") && desk.OpenCallsForChecks("chk_strong") == 0,
				"Your copies and the shop's lot are theirs; their salesmen take the calls");
			Check(pickup.Status == MasterDealStatus.Out && pickup.PickedUp && runtime.baseRecord.labelId == X.labelId && runtime.baseRecord.playerMasterDealId == pickup.Id,
				"Theirs from today, the same record");

			// The buyer goes under: the lease reverts early.
			var status = X.status;
			X.status = LabelStatus.Defunct;
			desk.ProcessMasterDealsForChecks();
			Check(pickup.Status == MasterDealStatus.Reverted && pickup.EndNote.Contains("went under") && runtime.baseRecord.labelId == label.labelId,
				"Their label folded: the master comes home");
			X.status = status;

			// Sale with the act: the act signs with them on their own terms; you keep a producer's royalty.
			int week = ChartManager.Instance.GetCurrentChartWeek();
			var (W, withAct) = CompetitorManager.Instance.GetOperatingLabels().Where(l => !l.isPlayerOwned && l.IsActive && SceneRecruitmentService.CanAccess(l, act, week))
				.OrderBy(l => l.labelId, StringComparer.Ordinal)
				.Select(l => (l, desk.EvaluateMasterForChecks(l, "chk_strong", MasterRoute.Visit).Offer))
				.FirstOrDefault(p => p.Offer?.Terms.Any(t => t.Kind == MasterDealKind.SaleWithAct) == true);
			Check(W != null, $"A label that works {act.stageName}'s scene would take the act too: {W?.labelName} ({W?.tier})");
			int actIndex = withAct.Terms.FindIndex(t => t.Kind == MasterDealKind.SaleWithAct);
			Check(withAct.Terms[actIndex].ActTerms is { } actTerms && actTerms.Advance > 0f, $"With the act: {withAct.Terms[actIndex].Why}");
			Check(!SceneRecruitmentService.CanAccess(X, act, week) || xAccess, "Only a label that could recruit in the act's scene offers to take it");
			desk.StageMasterOfferForChecks(withAct);
			X = W;
			buyerCash = X.cashReserves;
			float actAdvance = withAct.Terms[actIndex].ActTerms.Value.Advance;
			Check(desk.AcceptMasterOffer(withAct.Id, actIndex, out var soldWithAct), "Sell it with the act: " + soldWithAct);
			var sale = desk.MasterDeals.Last();
			Check(act.labelId == X.labelId && X.roster.Contains(act) && !desk.Roster.Contains(act), $"{act.stageName} is on {X.labelName}'s roster now");
			Check(sale.Status == MasterDealStatus.Permanent && runtime.baseRecord.labelId == X.labelId && runtime.baseRecord.playerMasterDealId == sale.Id
				&& buyerCash - X.cashReserves >= withAct.Terms[actIndex].Advance + actAdvance - .01f, "Sold for good; they paid your advance and the act's");
			float rec0 = sale.Recouped;
			var wk = desk.SettleMasterDealWeekForChecks(sale.Id, act, 100, 5f, 1f, 6f);
			Check(Near(wk.ArtistPayment, 5f) && Near(wk.ArtistRecouped, 1f) && Near(wk.Expense, 6f + Mathf.Max(0f, 100 * withAct.Terms[actIndex].CentsPerUnit - (sale.Recouped - rec0)), .02f)
				&& Near(sale.Recouped - rec0, Mathf.Min(sale.Terms.Advance, 100 * withAct.Terms[actIndex].CentsPerUnit), .02f) && sale.LedgerAgrees(),
				$"Their act's royalty runs on their contract; your producer royalty recoups your advance first (pay {wk.ArtistPayment:F2} rec {wk.ArtistRecouped:F2} exp {wk.Expense:F2} recouped +{sale.Recouped - rec0:F2} ledger {sale.LedgerAgrees()} units {sale.Units})");

			// The two other contracts, settled by hand: a lease recoups then pays; a sale pays the act, not you.
			desk.StageMasterDealForChecks(new MasterDeal { Id = "chk_lease", BuyerLabelId = X.labelId, RecordId = "chk_x1", Title = "x", ArtistId = act.artistId,
				Status = MasterDealStatus.Out, Terms = new MasterDealTerms { Kind = MasterDealKind.Lease, Advance = 10f, RoyaltyRate = .07f, TermWeeks = 52 } });
			var w1 = desk.SettleMasterDealWeekForChecks("chk_lease", act, 200, 9f, 2f, 9f);
			var w2 = desk.SettleMasterDealWeekForChecks("chk_lease", act, 100, 9f, 2f, 9f);
			var l2 = desk.MasterDeals.First(d => d.Id == "chk_lease");
			Check(Near(w1.Expense, 200 * .07f * PlayerDesk.ListPrice - 10f, .02f) && Near(w1.ArtistPayment, 0f) && Near(w2.Expense, 100 * .07f * PlayerDesk.ListPrice, .02f)
				&& Near(l2.Recouped, 10f) && l2.ActUnitsUnsettled == 300 && l2.LedgerAgrees(), $"Lease: the first $10 recoups, then {w2.Expense:F2} is owed; the act's 300 units wait for the statement");
			desk.StageMasterDealForChecks(new MasterDeal { Id = "chk_sale", BuyerLabelId = X.labelId, RecordId = "chk_x2", Title = "y", ArtistId = act.artistId,
				Status = MasterDealStatus.Permanent, Terms = new MasterDealTerms { Kind = MasterDealKind.Sale, Advance = 400f } });
			var w3 = desk.SettleMasterDealWeekForChecks("chk_sale", act, 100, 9f, 2f, 9f);
			var s2 = desk.MasterDeals.First(d => d.Id == "chk_sale");
			Check(Near(w3.ArtistPayment, 100 * PlayerDesk.RoyaltyPerUnit(act)) && Near(w3.ArtistRecouped, 0f) && Near(w3.Expense, w3.ArtistPayment)
				&& s2.Units == 100 && Near(s2.Earned, 0f) && s2.LedgerAgrees(), "Sale: they pay the act at its rate, never against your advance; you're owed nothing");

			// The posted tape: answered by letter, or never.
			while (Today() < post.AnswerDay + 1) clock.EndDay();
			var after = desk.MasterPostings.FirstOrDefault(p => p == post);
			bool letter = desk.MasterOffers.Any(o => o.BuyerLabelId == y.Label.labelId && o.RecordId == "chk_plain") || desk.MasterPasses.Any(p => p.BuyerLabelId == y.Label.labelId && p.RecordId == "chk_plain");
			Check(after != null ? after.NoReply && !letter : letter, after != null ? "No word back on the posted tape; another waits out the quiet" : "A letter came back on the posted tape");
			if (after != null) {
				bool reposted = desk.PostMasterTape(y.Label.labelId, "chk_plain", out var quiet);
				Check(!reposted && quiet.Contains("never answered"), "Reposting is refused until the quiet passes: " + quiet);
			}

			Check(desk.MasterDeals.All(d => d.LedgerAgrees()), "Every deal's ledger agrees");
			GD.Print($"MASTER_DEAL_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr($"MASTER_DEAL_CHECKS_FAIL after {checks}: {e.Message}\n{e.StackTrace}");
			GetTree().Quit(1);
		}
	}

	private static string OfferText(MasterDealOffer o) => o == null ? "none" : string.Join("; ", o.Terms.Select(t => $"{t.Kind} ${t.Advance}"));
}
