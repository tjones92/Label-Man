using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 5B gate (F5): gig money belongs to the performers. A tour pays the label nothing; a
/// residency is a booker's yes turned into dated nights on the room's real bills; the label's commission comes out of the
/// act's fee once, only for a set actually played, and is collected the morning after. A night that falls through pays
/// nothing. On the real clock in Detroit.
///   --path . SimTools/GigPayChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints GIG_PAY_CHECKS_PASS / _FAIL.
/// </summary>
public partial class GigPayChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(LocalScenes.Rooms, "Room calendar is on with the canonical flags");
			Check(desk.FoundLabel("Gig Pay Checks", "detroit", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			var label = desk.Label;
			label.cashReserves = 5000f;
			var rooms = SceneRoomCatalog.ForPlace("detroit").Where(r => r.IsPerformance).ToList();
			// Three weeks of bills, so the town's acts have played its rooms.
			var billed = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < 21; i++) {
				foreach (var b in LocalSceneRoomService.Calendar("detroit", clock.CurrentDate)) foreach (var s in b.Appearances) billed.Add(s.ArtistId);
				clock.EndDay();
			}
			int year = clock.CurrentDate.year, today = LocalSceneRoomService.Day(clock.CurrentDate);

			// Unsigned acts on Detroit's bills that have played a room here at least twice: the ones a booker would give a run.
			var local = billed.Select(id => ArtistManager.Instance.GetArtist(id))
				.Where(a => a != null && string.IsNullOrEmpty(a.labelId) && LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId) == rooms[0].PlaceId).ToList();
			GD.Print($"  billed acts {billed.Count}, room place '{rooms[0].PlaceId}', local unsigned {local.Count}");
			var candidates = local
				.Where(a => rooms.Any(r => LocalSceneRoomService.Eligible(a, r, today) && LocalSceneRoomService.ShowsAt(r.Id, a.artistId) >= PlayerDesk.ResidencyMinShows))
				.OrderBy(a => a.artistId, StringComparer.Ordinal).Take(25).ToList();
			Check(candidates.Count >= 2, $"{candidates.Count} of {local.Count} unsigned Detroit acts have played a room twice " +
				$"(eligible somewhere: {local.Count(a => rooms.Any(r => LocalSceneRoomService.Eligible(a, r, today)))}, any shows: {local.Count(a => rooms.Any(r => LocalSceneRoomService.ShowsAt(r.Id, a.artistId) > 0))})");

			// A tour: road load and momentum, no label money.
			var touring = candidates[^1];
			Sign(desk, touring, year);
			float revenue = label.monthlyRevenue, momentum = touring.momentum;
			Check(desk.BookRoad(touring, 0, 4, out var toured) && toured.Contains("theirs"), "Tour booked: " + toured);
			for (int i = 0; i < 8; i++) clock.EndDay();
			Check(Near(label.monthlyRevenue, revenue) && touring.momentum > momentum && desk.RoadBookingFor(touring.artistId).WeeksRemaining < 4,
				$"A week on the road: momentum {momentum:F3} -> {touring.momentum:F3}, no label income");
			Check(!desk.CashOptions().Any(o => o.Route == "Booked engagements" && o.Action.StartsWith(touring.stageName)), "CASH OPTIONS lists no tour money");

			// A residency: ask until a booker says yes (each act is its own ask; a no stands for that act).
			Check(desk.PassTime(desk.HoursUntil(PlayerDesk.BookerFromHour), out _), "Wait for the bookers");
			SimulatedArtist act = null; PlayerDesk.RoadBooking road = null; int asks = 0;
			foreach (var c in candidates.Take(candidates.Count - 1)) {
				if (clock.CurrentHour >= 22) { clock.EndDay(); desk.PassTime(desk.HoursUntil(PlayerDesk.BookerFromHour), out _); }
				Sign(desk, c, year);
				asks++;
				if (!desk.BookRoad(c, 8, 0, out var said)) { GD.Print("  (no ask: " + said + ")"); continue; }
				if (desk.RoadBookingFor(c.artistId)?.ResidencyRoomId != null) { act = c; road = desk.RoadBookingFor(c.artistId); GD.Print("  " + said); break; }
			}
			Check(act != null, $"A booker gave a residency after {asks} ask(s)");
			var room = SceneRoomCatalog.Get(road.ResidencyRoomId);
			var nights = LocalSceneRoomService.Bookings.Where(b => b.ArtistId == act.artistId && b.RoomId == room.Id && b.Residency).OrderBy(b => b.Day).ToList();
			Check(nights.Count > 0 && nights.Count <= 8 * PlayerDesk.ResidencyNightsPerWeek && nights.All(b => b.LabelId == label.labelId && b.Status == SceneBillStatus.Scheduled),
				$"{nights.Count} dated residency nights at {room.Name}");
			Check(nights[0].Day >= LocalSceneRoomService.FirstBookableDay - 7 && nights.All(b => SceneLiveEconomics.OpenOn(room, LocalSceneRoomService.Date(b.Day).DayOfWeek))
				&& nights.GroupBy(b => b.Day / 7).All(g => g.Count() <= PlayerDesk.ResidencyNightsPerWeek),
				"Past the announced calendar, only on nights the room opens, at most three a week");
			var row = desk.CashOptions().FirstOrDefault(o => o.Route == "Booked engagements" && o.Action.StartsWith(act.stageName));
			Check(row != null && row.Expected > 0f && row.Certainty == PlayerDesk.CashCertainty.Accepted, $"CASH OPTIONS: {row?.Action}, about ${row?.Expected:F2}, {row?.Why}");

			// Play the nights; every played one pays 10% of the act's fee once, the morning after; the band splits the rest.
			int played = 0, cancelled = 0;
			for (int d = 0; d < 70 && played < 2; d++) {
				int day = LocalSceneRoomService.Day(clock.CurrentDate);
				var tonight = LocalSceneRoomService.Bookings.FirstOrDefault(b => b.ArtistId == act.artistId && b.RoomId == room.Id && b.Day == day && b.Status == SceneBillStatus.Scheduled);
				if (tonight == null) { clock.EndDay(); continue; }
				var slots = LocalSceneRoomService.Calendar("detroit", clock.CurrentDate).Where(b => b.Day == day)
					.SelectMany(b => b.Appearances.Where(s => s.ArtistId == act.artistId)).ToList();
				var people = slots.SelectMany(s => s.PersonIds.Concat(s.BackingPersonIds ?? new())).Distinct().ToList();
				float realizedBefore = people.Sum(p => LocalSceneRoomService.Realized(act.artistId, p, year).Pay);
				float cash = label.cashReserves;
				bool first = clock.CurrentDate.day == 1 || clock.CurrentDate.AddDays(1).day == 1;
				clock.EndDay();
				var after = LocalSceneRoomService.Bookings.First(b => b.ArtistId == act.artistId && b.RoomId == room.Id && b.Day == day);
				var bill = LocalSceneRoomService.Calendar("detroit", LocalSceneRoomService.Date(day)).FirstOrDefault(b => b.Day == day && b.RoomId == room.Id)
					?? LocalSceneRoomService.LastCompletedBill(room.Id, act.artistId, day + 1, 0);
				if (after.Status != SceneBillStatus.Performed) {
					cancelled++;
					Check(after.Commission == 0f && !after.CommissionCollected, $"Night {LocalSceneRoomService.Date(day).ToShortString()} fell through ({after.Note}): no commission");
					continue;
				}
				var slot = bill?.Appearances.FirstOrDefault(s => s.ArtistId == act.artistId);
				Check(slot != null && slot.Fee > 0f, $"Played {LocalSceneRoomService.Date(day).ToShortString()}: the act's fee ${slot?.Fee:F2}");
				Check(Near(after.Commission, (float)Math.Round(slot.Fee * LocalSceneRoomService.BookingCommissionRate, 2)) && after.CommissionCollected,
					$"Commission ${after.Commission:F2} = {LocalSceneRoomService.BookingCommissionRate:P0} of the fee, collected");
				float fees = LocalSceneRoomService.Calendar("detroit", LocalSceneRoomService.Date(day)).Where(b => b.Day == day)
					.SelectMany(b => b.Appearances.Where(s => s.ArtistId == act.artistId && s.Status == SceneBillStatus.Performed)).Sum(s => s.Fee);
				float realizedAfter = people.Sum(p => LocalSceneRoomService.Realized(act.artistId, p, year).Pay);
				Check(Near(realizedAfter - realizedBefore, fees - after.Commission, .02f),
					$"The band's pay rose ${realizedAfter - realizedBefore:F2}: their ${fees:F2} in fees less the commission, never counted twice");
				if (!first) Check(Near(label.cashReserves - cash, after.Commission), $"Label cash +${label.cashReserves - cash:F2} at dawn");
				played++;
			}
			Check(played >= 1, $"{played} residency night(s) played, {cancelled} fell through");

			// A night that falls through: the clock's own resolution cancels it and the label is paid nothing.
			var next = LocalSceneRoomService.Bookings.Where(b => b.ArtistId == act.artistId && b.RoomId == room.Id && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.Day).FirstOrDefault();
			if (next != null) {
				while (LocalSceneRoomService.Day(clock.CurrentDate) < next.Day) clock.EndDay();
				var bill = LocalSceneRoomService.Calendar("detroit", clock.CurrentDate).FirstOrDefault(b => b.Day == next.Day && b.RoomId == room.Id && b.Appearances.Any(s => s.ArtistId == act.artistId));
				if (bill != null) {
					int sets = bill.Appearances.Count(s => s.Status == SceneBillStatus.Scheduled);
					LocalSceneRoomService.BreakLineupsForChecks(bill.Id, sets);
					float cash = label.cashReserves;
					clock.EndDay();
					var gone = LocalSceneRoomService.Bookings.First(b => b.ArtistId == act.artistId && b.RoomId == room.Id && b.Day == next.Day);
					Check(gone.Status == SceneBillStatus.Cancelled && gone.Commission == 0f && !gone.CommissionCollected, $"Forced fall-through: cancelled ({gone.Note}), no commission");
				}
			}

			// Legacy: a residency with no room behind it (booked before rooms gated it) holds no nights and pays nothing.
			Check(!LocalSceneRoomService.Bookings.Any(b => b.ArtistId == touring.artistId), "The touring act holds no booked nights");

			GD.Print($"GIG_PAY_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PushError($"GIG_PAY_CHECKS_FAIL after {checks} checks: {e}");
			GetTree().Quit(3);
		}
	}

	private static void Sign(PlayerDesk desk, SimulatedArtist act, int year) {
		if (act.labelId == desk.Label.labelId) return;
		desk.Label.SignArtist(act, year, desk.Label.GenerateTermSheet(act, year));
		ArtistManager.Instance.SignArtist(act, desk.Label.labelId, year);
	}
}
