using System;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 3 gate (F1-F3) for the parts a scripted opening can't force: the walk-up table is
/// retired where rooms sell, table rights need the booker's hours and a no stands, a supper room puts no table out, a
/// night needs a bill, the following is read from real room draw and fades, and the local appeal lift stays in bounds.
/// Sales themselves are exercised by the OpeningCashLockRunner (--prep=live, --sell=working).
///   --path . SimTools/RoomTableChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints ROOM_TABLE_CHECKS_PASS / _FAIL.
/// </summary>
public partial class RoomTableChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(LocalScenes.Rooms, "Room calendar is on with the canonical flags");
			Check(desk.FoundLabel("Room Table Checks", "detroit", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			Check(PlayerDesk.RoomTablesActive("detroit"), "Detroit's rooms sell records");

			// F1: the walk-up table is retired; the hall stays a hop site.
			var hall = desk.StopsInCity("detroit").FirstOrDefault(s => s.Kind == PlayerDesk.StopKind.Venue);
			if (hall != null) Check(!desk.WorkTheHopTable(hall.StopId, "any", out var walkUp) && walkUp.Contains("without a show"), "Walk-up table refused: " + walkUp);

			var rooms = SceneRoomCatalog.ForPlace("detroit");
			var club = rooms.First(r => r.Kind == SceneRoomKind.Club);
			var listening = rooms.First(r => r.Kind == SceneRoomKind.ListeningRoom);
			Check(!PlayerDesk.RoomSellsRecords(listening) && !PlayerDesk.RoomSellsRecords(rooms.First(r => r.Kind == SceneRoomKind.TradeEvent)), "Supper rooms and trade appointments put no table out");
			Check(rooms.Count(PlayerDesk.RoomSellsRecords) == 4, "Club, hall, roadhouse and coffeehouse sell");
			Check(PlayerDesk.RoomTableCut(SceneRoomKind.CommunityHall) < PlayerDesk.RoomTableCut(SceneRoomKind.Club), "A hall takes less than a club");

			// Rights: the booker's hours, in person.
			int hourBefore = clock.CurrentHour;
			Check(!desk.AskForTableRights(club.Id, out var early) && early.Contains("p.m.") && clock.CurrentHour == hourBefore, "Morning ask refused without spending time: " + early);
			Check(!desk.AskForTableRights(listening.Id, out var supper) && supper.Contains("doesn't put"), "Listening room refuses a table: " + supper);
			Check(desk.PassTime(desk.HoursUntil(PlayerDesk.BookerFromHour), out _) && clock.CurrentHour == PlayerDesk.BookerFromHour, "Wait for the booker");
			float chance = desk.TableRightsChance(club);
			Check(chance >= .05f && chance <= .85f && Math.Abs(chance - .20f) < .001f, $"A label that never played the club starts at the base chance ({chance:F2})");
			Check(desk.AskForTableRights(club.Id, out var answer) && clock.CurrentHour == PlayerDesk.BookerFromHour + PlayerDesk.TableRightsHours, "Ask takes an hour: " + answer);
			var asked = desk.LastTableRightsAsk(club.Id);
			Check(asked != null && Math.Abs(asked.Cut - PlayerDesk.RoomTableCut(club.Kind)) < .001f, "The answer and its cut are recorded");
			Check(!desk.AskForTableRights(club.Id, out var again) && (asked.Granted ? again.Contains("already have") : again.Contains("said no")), "Asking again: " + again);
			Check(asked.Granted == (desk.TableRightsAt(club.Id) != null), "Rights read back as answered");

			// A refusal stands; a staged grant is current until its term ends.
			int today = LocalSceneRoomService.Day(clock.CurrentDate);
			desk.StageRoomTableForChecks(club.Id, false, today, .25f, null, 0f, clock.CurrentDate);
			bool refusedAgain = !desk.AskForTableRights(club.Id, out var stands);
			Check(desk.TableRightsAt(club.Id) == null && refusedAgain && stands.Contains("said no"), "A no stands: " + stands);
			desk.StageRoomTableForChecks(club.Id, true, today - PlayerDesk.TableRightsTermDays - 1, .25f, null, 0f, clock.CurrentDate);
			Check(desk.TableRightsAt(club.Id) == null, "Rights lapse after a term");
			desk.StageRoomTableForChecks(club.Id, true, today, .25f, null, 0f, clock.CurrentDate);
			Check(desk.TableRightsAt(club.Id) != null && desk.RoomTableNights("detroit").All(n => n.OwnAct || n.Rights != null), "Granted rights list the club's nights");
			Check(!desk.WorkRoomTable(club.Id, "no-such-record", out var noStock) && noStock.Contains("pressed on hand"), "No stock, no table: " + noStock);

			// F2: buyers already served at one room count partly against the town's other rooms.
			var hallRoom = rooms.First(r => r.Kind == SceneRoomKind.CommunityHall);
			int leftFresh = desk.RoomTableBuyersLeft(hallRoom.Id, "probe_record");
			desk.StageRoomTableForChecks(club.Id, true, today, .25f, "probe_record", 50f, clock.CurrentDate);
			int leftAfter = desk.RoomTableBuyersLeft(hallRoom.Id, "probe_record");
			Check(leftAfter <= leftFresh && leftFresh - leftAfter <= Mathf.CeilToInt(PlayerDesk.RoomTableOverlapShare * 50f) + 1, $"Overlap: the hall's buyers left {leftFresh} -> {leftAfter} after 50 sold at the club");

			// F3 taste: bounded, neutral off the table, and pointing the right way.
			Check(PlayerDesk.LocalTasteLift(Genre.Country, "atlantis", 1960) == 0f, "Unknown city has no taste");
			float nashville = PlayerDesk.LocalTasteLift(Genre.Country, "nashville", 1960), newYork = PlayerDesk.LocalTasteLift(Genre.Country, "new_york", 1960);
			Check(nashville > 0f && nashville <= PlayerDesk.TasteLiftMax && newYork <= nashville, $"Country leans Nashville ({nashville:F3}) over New York ({newYork:F3})");
			Check(Math.Abs(PlayerDesk.LocalTasteLift(Genre.TraditionalPop, "billings", 1960)) < 1e-6f, "A sparse city with no specialism reads neutral");

			// F3 following: read from real room draw once sets have played, and it fades.
			for (int i = 0; i < 10; i++) clock.EndDay();
			int day = LocalSceneRoomService.Day(clock.CurrentDate);
			var drawn = ArtistManager.Instance.GetAllArtists()
				.Where(a => a.geography?.basePlaceId != null)
				.Select(a => (a, place: LocalScenePersistenceService.SceneIdFor(a.geography.basePlaceId)))
				.Where(x => x.place != null)
				.Select(x => (x.a, x.place, heads: LocalSceneRoomService.LocalDraw(x.a.artistId, x.place, day, PlayerDesk.FollowingHalfLifeDays)))
				.Where(x => x.place != null && x.heads > 0f).OrderByDescending(x => x.heads).FirstOrDefault();
			Check(drawn.a != null, "Some act has a live draw after ten days of bills");
			float later = LocalSceneRoomService.LocalDraw(drawn.a.artistId, drawn.place, day + (int)PlayerDesk.FollowingHalfLifeDays, PlayerDesk.FollowingHalfLifeDays);
			Check(later < drawn.heads * .51f, $"Draw fades by a half-life ({drawn.heads:F1} -> {later:F1})");
			float following = desk.LocalFollowing(drawn.a.artistId, drawn.place);
			Check(following > 0f && following < 1f, $"Following saturates below 1 ({following:F3})");
			Check(desk.LocalFollowing(drawn.a.artistId, "atlantis") == 0f, "No following in a town it never played");
			float lift = desk.LocalAppealLift(drawn.a.artistId, drawn.a.primaryGenre, drawn.place);
			Check(lift >= PlayerDesk.LocalLiftMin && lift <= PlayerDesk.LocalLiftMax, $"Joint lift within bounds ({lift:F3})");

			CancelledNight(desk, clock);

			GD.Print($"ROOM_TABLE_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PushError($"ROOM_TABLE_CHECKS_FAIL after {checks} checks: {e}");
			GetTree().Quit(3);
		}
	}

	/// <summary>
	/// F1 "do not award sales from a scheduled show that later cancels". The probe never met one (0 of 501 nights), so
	/// force it: change every set's lineup on a real club night, as if a member left after booking, and let the clock's own
	/// resolution cancel the bill while the player is working the table. Then the control: drop only one set of a
	/// multi-act night, which still goes ahead on the rest.
	/// </summary>
	private void CancelledNight(PlayerDesk desk, TimeManager clock) {
		var label = desk.Label;
		var act = ArtistManager.Instance.GetAllArtists().First(a => a.labelId != label.labelId);
		var record = new Record {
			recordId = "room_table_cancel_probe", title = "Cancel Probe", artistId = act.artistId, artistName = act.stageName,
			format = ReleaseFormat.Single, primaryGenre = act.primaryGenre,
			hookStrength = .65f, productionQuality = .5f, originality = .5f, danceability = .5f,
		};
		Check(CompetitorManager.Instance.ReleasePlayerRecord(label, act, record, 0f, 0f, clock.CurrentDate), "A probe single is released on the label");
		desk.DebugSetPressStock(record.recordId, 200, 0, 200, 0f);

		SceneBill Tonight(int minActs) {
			for (int d = 0; d < 21; d++) {
				if (d > 0) clock.EndDay();
				int day = LocalSceneRoomService.Day(clock.CurrentDate);
				var bill = LocalSceneRoomService.Calendar("detroit", clock.CurrentDate)
					.Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled && b.Appearances.Count(a => a.Status == SceneBillStatus.Scheduled) >= minActs
						&& PlayerDesk.RoomSellsRecords(SceneRoomCatalog.Get(b.RoomId))
						&& (b.StartHour - 1) * 60 >= clock.CurrentHour * 60 + clock.CurrentMinute && b.EndHour * 60 <= clock.DayStopMinute)
					.OrderBy(b => b.StartHour).ThenBy(b => b.RoomId, StringComparer.Ordinal).FirstOrDefault();
				if (bill != null) return bill;
			}
			return null;
		}

		// 1. Every set falls through: the bill cancels while the table is up.
		var night = Tonight(1);
		Check(night != null, "A selling room has a bill within three weeks");
		var room = SceneRoomCatalog.Get(night.RoomId);
		desk.StageRoomTableForChecks(room.Id, true, night.Day, PlayerDesk.RoomTableCut(room.Kind), null, 0f, clock.CurrentDate);
		Check(desk.PassTime(desk.HoursUntil(night.StartHour - 1), out _) && clock.CurrentHour == night.StartHour - 1, $"Wait for set-up at {room.Name}");
		int sets = night.Appearances.Count(a => a.Status == SceneBillStatus.Scheduled);
		Check(LocalSceneRoomService.BreakLineupsForChecks(night.Id, sets) == sets, $"All {sets} set(s) lose a member after booking");
		Check(desk.RoomTableNights("detroit").Any(n => n.Bill.Id == night.Id), "The night still reads as scheduled before it resolves");

		float cash = label.cashReserves, expenses = label.monthlyExpenses;
		int stock = desk.StockFor(record.recordId).Remaining;
		var rec = desk.ReleasedRecords.First(r => r.baseRecord.recordId == record.recordId);
		float net = rec.lifetimeLabelNet;
		Check(desk.WorkRoomTable(room.Id, record.recordId, out var cancelled), "Working the table is accepted: " + cancelled);
		var after = LocalSceneRoomService.Bill(night.Id);
		Check(after.Status == SceneBillStatus.Cancelled && after.Attendance == 0 && after.Appearances.All(a => a.Status == SceneBillStatus.Cancelled),
			$"The clock cancelled the bill ({after.Status}, {after.Attendance} in): {after.Response}");
		Check(cancelled.Contains("didn't go ahead"), "The player is told: " + cancelled);
		Check(desk.StockFor(record.recordId).Remaining == stock, "No copies left the box");
		Check(Math.Abs(label.cashReserves - cash) < .001f && Math.Abs(label.monthlyExpenses - expenses) < .001f && Math.Abs(rec.lifetimeLabelNet - net) < .001f,
			"No sale booked and no room cut taken");
		Check(desk.RoomTableCrowdForChecks(room.Id, record.recordId) < 0f, "No buyers remembered at the room");
		Check(clock.CurrentHour == night.EndHour, $"The hours were still spent (clock {clock.CurrentHour}:00, bill end {night.EndHour}:00)");
		Check(!desk.RoomTableNights("detroit").Any(n => n.Bill.Id == night.Id), "A cancelled night drops off the table list");
		Check(!desk.WorkRoomTable(room.Id, record.recordId, out var again) && again.Contains("no bill"), "It can't be worked again: " + again);

		// 2. Control: one set of several drops; the night goes ahead on the rest and the table sells to who came.
		clock.EndDay();
		var partial = Tonight(2);
		if (partial == null) { GD.Print("skip: no multi-act selling night within three weeks for the partial control"); return; }
		var partRoom = SceneRoomCatalog.Get(partial.RoomId);
		desk.StageRoomTableForChecks(partRoom.Id, true, partial.Day, PlayerDesk.RoomTableCut(partRoom.Kind), null, 0f, clock.CurrentDate);
		Check(desk.PassTime(desk.HoursUntil(partial.StartHour - 1), out _), $"Wait for set-up at {partRoom.Name}");
		int partSets = partial.Appearances.Count(a => a.Status == SceneBillStatus.Scheduled);
		Check(LocalSceneRoomService.BreakLineupsForChecks(partial.Id, 1) == 1, $"One of {partSets} sets loses a member");
		int stockBefore = desk.StockFor(record.recordId).Remaining;
		Check(desk.WorkRoomTable(partRoom.Id, record.recordId, out var went), "Working the table: " + went);
		var partAfter = LocalSceneRoomService.Bill(partial.Id);
		Check(partAfter.Status == SceneBillStatus.Performed && partAfter.Appearances.Count(a => a.Status == SceneBillStatus.Cancelled) == 1,
			$"The night went ahead on {partSets - 1} set(s) ({partAfter.Attendance} in)");
		Check(!went.Contains("didn't go ahead"), "The player isn't told it was cancelled: " + went);
		int sold = stockBefore - desk.StockFor(record.recordId).Remaining;
		Check(sold >= 0 && sold <= Math.Min(PlayerDesk.RoomTableNightCeiling, (int)(partAfter.Attendance * PlayerDesk.RoomTableShareOfHouse)),
			$"The table sold {sold} within the night's ceiling");
		Check(sold == 0 || Math.Abs(desk.RoomTableCrowdForChecks(partRoom.Id, record.recordId) - sold) < .001f, "Buyers remembered match the copies sold");
	}
}
