using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 4 gate (§7.1): paid demos. Clients are real unsigned acts on tonight's bills with the
/// money (scene pay and holdings) to pay; the quote is a real local room's rate on a free day; the deposit is held, never
/// the label's cash; delivery pays only the fee and leaves a bounded demo sample on the act's scouting file; a missed
/// session costs the label the room's half and standing; an act that can't come forfeits half the room from its own
/// deposit; cancelling ahead is free; the books and CASH OPTIONS read it; and nothing changes hands but the fee.
///   --path . SimTools/DemoChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints DEMO_CHECKS_PASS / _FAIL.
/// </summary>
public partial class DemoChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(desk.FoundLabel("Demo Checks", "detroit", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			var label = desk.Label;



			// Run the scene until an unsigned act on tonight's bill could be pitched.
			(SimulatedArtist Act, SceneBill Bill, string Block) pick = default;
			int firstDay = -1, scanned = 0;
			var blocks = new System.Collections.Generic.Dictionary<string, int>();
			var meansSeen = new System.Collections.Generic.List<float>();
			for (int d = 0; d < 120 && pick.Act == null; d++) {
				if (d > 0) clock.EndDay();
				var tonight = desk.DemoProspects("detroit");
				if (tonight.Count == 0) continue;
				int start = tonight.Min(p => p.Bill.StartHour);
				if (clock.CurrentHour < start) desk.PassTime(start - clock.CurrentHour, out _);
				var all = desk.DemoProspects("detroit");
				scanned += all.Count;
				foreach (var p in all) {
					string why = p.Block == null ? "ok" : p.Block.Contains("looking") ? "not looking" : p.Block.Contains("money") ? "no money" : p.Block.Contains("room") ? "no room" : p.Block;
					blocks[why] = blocks.GetValueOrDefault(why) + 1;
					if (why == "no money") meansSeen.Add(desk.DemoClientMeans(p.Act));
				}
				pick = all.FirstOrDefault(p => p.Block == null);
				if (pick.Act != null) firstDay = d;
			}
			GD.Print("blocks: " + string.Join("; ", blocks.Select(kv => $"{kv.Key}={kv.Value}")));
			if (meansSeen.Count > 0) { meansSeen.Sort(); GD.Print($"means of unaffordable act-nights: median ${meansSeen[meansSeen.Count / 2]:F2}, max ${meansSeen[^1]:F2}"); }
			Check(pick.Act != null, $"A real client turns up: {pick.Act?.stageName} on day {firstDay} ({scanned} unsigned act-nights seen)");
			var act = pick.Act;
			Check(string.IsNullOrEmpty(act.labelId) && act.prospectMarketStatus is ProspectMarketStatus.Seeking or ProspectMarketStatus.Latent, "Unsigned and wanting a record");
			float means = desk.DemoClientMeans(act);
			var quote = desk.DemoQuoteFor(act);
			Check(quote != null && quote.Gross <= means * PlayerDesk.DemoAffordableShare + .01f, $"They can afford it: ${quote?.Gross:F2} of ${means:F2}");

			// The quote is a real local room on a free day, at its own rate.
			int today = LocalSceneRoomService.Day(clock.CurrentDate);
			Check(quote.Day >= today + PlayerDesk.DemoLeadDays && quote.Day <= today + PlayerDesk.DemoHorizonDays, $"Dated {quote.Day - today} days out");
			var room = desk.StudioRoom(quote.RoomId, act.artistId, null, LocalSceneRoomService.Date(quote.Day));
			Check(room != null && room.CityId == "detroit" && Near(quote.RoomCost, PlayerDesk.StudioRoomCost(room, PlayerDesk.DemoHours), .02f),
				$"{quote.RoomName}: ${quote.RoomCost:F2} for {quote.Hours}h at the room's own rate");
			Check(Near(quote.DubCost, PlayerDesk.DemoDubCost) && Near(quote.Fee, PlayerDesk.DemoProducerFee) && quote.Gross >= 35f && quote.Gross <= 100f,
				$"Dub ${quote.DubCost}, fee ${quote.Fee}: ${quote.Gross:F2} gross, inside the $35-100 reference");

			// The pitch: an hour at the bar, then their answer.
			float cash0 = label.cashReserves; int hour0 = clock.CurrentHour;
			Check(desk.OfferDemo(act.artistId, out var answer) && clock.CurrentHour == hour0 + PlayerDesk.DemoOfferHours, "The pitch takes an hour: " + answer);
			Check(Near(label.cashReserves, cash0), "No money moves at the pitch, yes or no");
			var job = desk.DemoJobs.FirstOrDefault(j => j.ArtistId == act.artistId);
			if (job == null) {
				Check(desk.DemoRefusedUntil(act.artistId) == today + PlayerDesk.DemoRefusalDays && !desk.OfferDemo(act.artistId, out var again) && again.Contains("said no"),
					"A no stands for a quarter");
				job = desk.StageDemoForChecks(act, quote);   // the rest of the gate needs a booking; stage what a yes books
				GD.Print("note: the pitch was refused, so the booking below is staged");
			}
			Check(job.Status == PlayerDesk.DemoStatus.Booked && Near(job.DepositHeld, quote.Deposit) && Near(label.cashReserves, cash0), $"Booked: ${job.DepositHeld:F2} held, not in the label's cash");
			Check(Near(desk.DemoClientMeans(act), means - job.Deposit, .02f), "Their deposit comes off what they have");
			Check(desk.CashOptions().Any(o => o.Route == "Produce a booked demo" && o.Certainty == PlayerDesk.CashCertainty.Accepted && Near(o.Expected, job.Fee)),
				"CASH OPTIONS lists the booked fee as agreed");
			Check(!desk.ProduceDemo(job.Id, out var early) && early.Contains("booked for"), "Not before its day: " + early);

			// Its day: produce it. The deposit pays the room and the dub; the fee is the label's; the act's file gets a sample.
			while (LocalSceneRoomService.Day(clock.CurrentDate) < job.Day) clock.EndDay();
			Check(job.Status == PlayerDesk.DemoStatus.Booked && Near(job.DepositHeld, job.Deposit), "Still booked, deposit still held, on the morning");
			if (clock.CurrentHour < job.StartHour) desk.PassTime(job.StartHour - clock.CurrentHour, out _);
			float cashDay = label.cashReserves, revenueDay = label.monthlyRevenue, meansDay = desk.DemoClientMeans(act);
			int obsBefore = desk.ScoutingDossiers.FirstOrDefault(d => d.ArtistId == act.artistId)?.Observations.Count ?? 0;
			Check(desk.ProduceDemo(job.Id, out var made) && job.Status == PlayerDesk.DemoStatus.Delivered, "Produced: " + made);
			Check(Near(label.cashReserves - cashDay, job.Fee) && Near(label.monthlyRevenue - revenueDay, job.Fee) && Near(job.DepositHeld, 0f),
				"Only the fee reaches the label, as income; the deposit went to the room");
			Check(clock.CurrentHour == job.StartHour + job.Hours, $"The session took {job.Hours}h");
			var obs = desk.ScoutingDossiers.First(d => d.ArtistId == act.artistId).Observations;
			Check(obs.Count == obsBefore + 1 && obs.Last().Kind == ScoutingEvidenceKind.DemoSample && obs.Last().SourceName == "Your demo session",
				"A demo sample on their scouting file: " + obs.Last().Claim);
			Check(string.IsNullOrEmpty(act.labelId), "No signing, no option: they're still unsigned");
			Check(Near(desk.DemoClientMeans(act), meansDay - job.Fee, .02f), $"Delivery takes the ${job.Fee:F2} fee from them on top of the deposit");
			Check(!desk.ProduceDemo(job.Id, out _), "It can't be produced twice");

			// A missed session: the label refunds the deposit, pays the room's half, loses standing, and they won't come back.
			var missAct = Client(desk, act.artistId) ?? act;
			var missQuote = desk.DemoQuoteFor(missAct) ?? desk.DemoQuoteFor(act);
			var miss = desk.StageDemoForChecks(missAct, missQuote);
			float rep = label.reputation, cashMiss = label.cashReserves;
			while (LocalSceneRoomService.Day(clock.CurrentDate) <= miss.Day) clock.EndDay();
			Check(miss.Status == PlayerDesk.DemoStatus.LabelMissed || miss.Status == PlayerDesk.DemoStatus.ClientCancelled, $"Settled the morning after: {miss.Status}");
			if (miss.Status == PlayerDesk.DemoStatus.LabelMissed) {
				Check(miss.Outcome.Contains($"billed you ${miss.RoomCost * PlayerDesk.DemoNoShowRoomShare:N2}") && Near(miss.DepositHeld, 0f), "Missed: the room's half is the label's, the deposit goes back: " + miss.Outcome);
				Check(Near(rep - label.reputation, Mathf.Min(rep, PlayerDesk.DemoMissedReputation), .0001f), "Missed: standing drops");
				Check(desk.DemoRefusedUntil(missAct.artistId) == miss.Day + PlayerDesk.DemoMissedRefusalDays, "Missed: that act won't book with you for a year");
			}

			// An act that can't come: half the room from its own deposit, nothing owed to the label.
			var goneAct = Client(desk, act.artistId, missAct.artistId) ?? act;
			var gone = desk.StageDemoForChecks(goneAct, desk.DemoQuoteFor(goneAct) ?? missQuote);
			while (LocalSceneRoomService.Day(clock.CurrentDate) < gone.Day) clock.EndDay();
			var status = goneAct.lifecycleStatus;
			goneAct.lifecycleStatus = ArtistLifecycleStatus.Retired;
			if (clock.CurrentHour < gone.StartHour) desk.PassTime(gone.StartHour - clock.CurrentHour, out _);
			float cashGone = label.cashReserves;
			Check(desk.ProduceDemo(gone.Id, out var noShow) && gone.Status == PlayerDesk.DemoStatus.ClientCancelled && Near(label.cashReserves, cashGone), "They can't come: " + noShow);
			goneAct.lifecycleStatus = status;

			// Cancelling a day ahead is free; on the day the room's half is the label's.
			var ahead = desk.StageDemoForChecks(act, desk.DemoQuoteFor(act) ?? missQuote);
			float cashAhead = label.cashReserves;
			Check(desk.CancelDemo(ahead.Id, out var off) && Near(label.cashReserves, cashAhead) && ahead.Status == PlayerDesk.DemoStatus.LabelCancelled, "Called off ahead, free: " + off);
			var late = desk.StageDemoForChecks(act, desk.DemoQuoteFor(act) ?? missQuote);
			while (LocalSceneRoomService.Day(clock.CurrentDate) < late.Day) clock.EndDay();
			float cashLate = label.cashReserves;
			Check(desk.CancelDemo(late.Id, out var lateOff) && Near(cashLate - label.cashReserves, late.RoomCost * PlayerDesk.DemoNoShowRoomShare, .02f), "Called off on the day: " + lateOff);
			Check(desk.DemoJobs.All(j => j.Status == PlayerDesk.DemoStatus.Booked || Near(j.DepositHeld, 0f)), "No deposit is left held on a settled job");

			GD.Print($"DEMO_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PushError($"DEMO_CHECKS_FAIL after {checks} checks: {e}");
			GetTree().Quit(3);
		}
	}

	/// <summary>Another unsigned act working this scene, for staging (not the ones given).</summary>
	private static SimulatedArtist Client(PlayerDesk desk, params string[] not) {
		string scene = LocalScenePersistenceService.SceneIdFor("detroit");
		int week = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate) / 7;
		return ArtistManager.Instance.GetAllArtists().Where(a => string.IsNullOrEmpty(a.labelId) && !not.Contains(a.artistId)
				&& a.lifecycleStatus == ArtistLifecycleStatus.Active && LocalScenePersistenceService.Present(a, scene, week) && desk.DemoQuoteFor(a) != null)
			.OrderBy(a => a.artistId, StringComparer.Ordinal).FirstOrDefault();
	}
}
