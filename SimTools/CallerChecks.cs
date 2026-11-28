using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// The ringing phone, a booker's open set, a rival's tip and the note left in the door (PlayerDesk.Callers; onboarding §4c,
/// T5 step 3 follow-on), through the real desk in Detroit.
///
/// A: a shop's call rings while he waits at the desk (the desk's own WAIT): the wait stops at the minute it rings, the phone
///    is ringing and nothing is open yet. It survives a save and load. Picking up takes five minutes and shows the shop and
///    what they want; the call is open. A script's wait (no stop) still picks up as it goes.
/// B: let it ring: any other move of the clock lets that try go (the shop tries again after lunch); let the second ring out
///    and the day end, and the call is missed.
/// C: a booker he's met, with a set on the announced calendar come open (the billed act lost its players), rings about it a
///    day or more before the night, in the afternoon, if the label has an act who could work it. Picked up, the set is
///    offered (a pink slip and THE PHONE list it); putting the act in puts it on that night's bill at that hour with a
///    booking. Once per open set. A booker's call nobody answers leaves no offer.
/// D: a rival's look at an act he's heard: a booker whose room has had them rings the next day. Picked up, the act's leads
///    name him whatever the Fixer; missed, nothing.
/// E: a manager whose yes waits on a time comes by a day or two before the first time offered: out, a note in the door,
///    which names the times and can be read (it's gone after); in, a knock that stops the wait; opened, a time can be
///    picked there. A knock left unanswered leaves a note. A sit-down missed leaves the manager's new times in the door.
///   --path . SimTools/CallerChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints CALLER_CHECKS_PASS / _FAIL.
/// </summary>
public partial class CallerChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
			const string city = "detroit";
			Check(LocalScenes.Rooms, "the room calendar is on with the canonical flags");
			Check(desk.FoundLabel("Caller Checks", city, out string message), message);
			desk.DismissLienMorning();
			desk.Label.cashReserves = 5000f;
			int Today() => LocalSceneRoomService.Day(clock.CurrentDate);
			int Now() => clock.CurrentHour * 60 + clock.CurrentMinute;
			void NextDay() {
				clock.EndDay();
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
			}
			PendingRing Ring(Func<PendingRing, bool> which) => desk.PendingRingsForChecks.FirstOrDefault(which);
			void ToDay(int day) { while (Today() < day) NextDay(); }
			void Before(int minute) => clock.RestoreClock(clock.CurrentDate, (minute - 10) / 60, (minute - 10) % 60);
			bool WaitAtDesk(int hours, out string said) => desk.PassTime(hours, out said, stopForCallers: true);

			var shops = desk.StopsInCity(city).Where(s => s.Kind == PlayerDesk.StopKind.Shop).ToList();
			var station = desk.StopsInCity(city).First(s => s.Kind == PlayerDesk.StopKind.Station);
			shops[0].LastVisitWeek = 1; shops[1].LastVisitWeek = 1;
			void GoOut() => desk.AskWhatsOnSurvey(station.StopId, out _);

			// A
			desk.QueueRingForChecks(shops[0], "chk_call_a");
			var a = Ring(r => r.RecordId == "chk_call_a");
			ToDay(a.Day);
			int first = a.Tries[0];
			Before(first);
			Check(WaitAtDesk(3, out message) && Now() == first, $"the desk's wait stops at the minute it rings ({PlayerDesk.ClockText(Now())}): {message}");
			Check(desk.Calling == PlayerDesk.CallerAt.Phone && !desk.HasOpenCall(shops[0].StopId, "chk_call_a"), "the phone is ringing, and nothing's open yet");
			var json = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
			var saved = JsonSerializer.Deserialize<PlayerSaveData>(json, SaveGameService.TestJsonOptions);
			Check(saved.PendingRings.Count(r => r.Ringing) == 1, "a save holds the ringing phone");
			Check(desk.RestoreState(saved, out message) && desk.Calling == PlayerDesk.CallerAt.Phone, "and a load is still ringing: " + message);
			int answered0 = desk.RingsAnswered;
			var card = desk.PickUp(out message);
			Check(card != null && !card.AtTheDoor && card.From.Contains(shops[0].DisplayName) && card.Said.Length > 10,
				$"picked up: {card?.From}: {card?.Said}");
			Check(Now() == first + PlayerDesk.PickUpMinutes && desk.Calling == PlayerDesk.CallerAt.None, "picking up took five minutes; it's quiet now");
			Check(desk.HasOpenCall(shops[0].StopId, "chk_call_a") && desk.RingsAnswered == answered0 + 1, "the call is open demand");
			desk.QueueRingForChecks(shops[0], "chk_call_a2");
			var a2 = Ring(r => r.RecordId == "chk_call_a2");
			ToDay(a2.Day); Before(a2.Tries[0]);
			Check(desk.PassTime(1, out _) && desk.Calling == PlayerDesk.CallerAt.None && desk.HasOpenCall(shops[0].StopId, "chk_call_a2"),
				"a script's wait doesn't stop: it picks up as it goes");

			// B
			desk.QueueRingForChecks(shops[1], "chk_call_b");
			var b = Ring(r => r.RecordId == "chk_call_b");
			ToDay(b.Day); Before(b.Tries[0]);
			int afternoon = b.Tries[1], missed0 = desk.RingsMissed;
			Check(WaitAtDesk(3, out _) && desk.Calling == PlayerDesk.CallerAt.Phone, "the second shop rings");
			desk.PassTime(1, out _);
			Check(desk.Calling == PlayerDesk.CallerAt.None && Ring(r => r.RecordId == "chk_call_b") is { Tries.Count: 1 } left && left.Tries[0] == afternoon,
				"let it ring: that try's gone, they'll try after lunch");
			Before(afternoon);
			Check(WaitAtDesk(3, out _) && desk.Calling == PlayerDesk.CallerAt.Phone, "it rings again in the afternoon");
			NextDay();
			Check(desk.Calling == PlayerDesk.CallerAt.None && Ring(r => r.RecordId == "chk_call_b") == null && !desk.HasOpenCall(shops[1].StopId, "chk_call_b")
				&& desk.RingsMissed == missed0 + 1 && desk.MissedRingsForChecks.Any(m => m.StopId == shops[1].StopId),
				"left ringing to the day's end: missed, and the shop remembers");

			// Three weeks of bills, so the town's acts have played its rooms.
			var billed = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < 14; i++) {
				foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate)) foreach (var s in bill.Appearances) billed.Add(s.ArtistId);
				NextDay();
			}
			int today = Today(), year = clock.CurrentDate.year;
			var rooms = SceneRoomCatalog.ForPlace(city).Where(r => r.IsPerformance).OrderBy(r => r.Id, StringComparer.Ordinal).ToList();

			// C: a set comes open three nights out in a room whose booker he knows, and the label has an act who could work it.
			LocalSceneRoomService.OpenSet? made = null; SimulatedArtist ours = null; SceneRoomProfile room = null;
			foreach (var r in rooms) {
				var bill = LocalSceneRoomService.Calendar(city, clock.CurrentDate).FirstOrDefault(x => x.RoomId == r.Id && x.Day == today + 3
					&& x.Status == SceneBillStatus.Scheduled && x.Appearances.Count > 0);
				if (bill == null) continue;
				var act = billed.Select(id => am.GetArtist(id)).Where(x => x != null && string.IsNullOrEmpty(x.labelId) && bill.Appearances.All(s => s.ArtistId != x.artistId)
					&& LocalSceneRoomService.Eligible(x, r, bill.Day) && !LocalSceneRoomService.PlayingThatDay(x, bill.Day)).OrderBy(x => x.artistId, StringComparer.Ordinal).FirstOrDefault();
				if (act == null) continue;
				var dropped = am.GetArtist(bill.Appearances[0].ArtistId);
				if (dropped.members.Any(m => act.members.Any(o => o.personId == m.personId))) continue;
				foreach (var m in dropped.members) m.isActive = false;   // the billed act lost its players: it can't play the night
				desk.Label.SignArtist(act, year, desk.Label.GenerateTermSheet(act, year));
				am.SignArtist(act, desk.Label.labelId, year);
				desk.MeetBookerForCheck(r.Id);
				made = new LocalSceneRoomService.OpenSet(bill.Id, r.Id, bill.Day, bill.Appearances[0].StartHour, dropped.artistId);
				ours = act; room = r;
				break;
			}
			Check(made != null, $"a set three nights out comes open at {room?.Name}, and {ours?.stageName} could work it");
			Check(LocalSceneRoomService.OpenSets(room.Id, today, today + 5).Contains(made.Value), "the booker's calendar shows the open set");
			desk.QueueBookerCallsForChecks();
			var slot = Ring(r => r.Kind == PlayerDesk.RingKind.BookerSlot && r.RoomId == room.Id);
			Check(slot != null && slot.Day > today && slot.Day < slot.Night && slot.Tries.Count == 1 && slot.Tries[0] >= 14 * 60 && slot.Tries[0] < 17 * 60,
				$"he rings once, a day or more before the night, in the afternoon (day {slot?.Day}, night {slot?.Night}, {PlayerDesk.ClockText(slot?.Tries[0] ?? 0)})");
			desk.QueueBookerCallsForChecks();
			Check(desk.PendingRingsForChecks.Count(r => r.Kind == PlayerDesk.RingKind.BookerSlot && r.RoomId == room.Id) == 1, "once per open set");
			ToDay(slot.Day); Before(slot.Tries[0]);
			Check(WaitAtDesk(3, out _) && desk.Calling == PlayerDesk.CallerAt.Phone, "the booker's call rings at the desk");
			card = desk.PickUp(out _);
			Check(card.OfferId != null && card.From.Contains(room.ContactName) && card.Said.Contains("today"), $"picked up: {card.From}: {card.Said}");
			Check(desk.DeskMessages().Any(m => m.From == room.ContactName), "a pink slip says to tell him today");
			var acts = desk.ActsForOffer(card.OfferId);
			Check(acts.Any(x => x.artistId == ours.artistId), $"{ours.stageName} can take it ({string.Join(", ", acts.Select(x => x.stageName))})");
			Check(desk.TakeBookerOffer(card.OfferId, ours.artistId, onTheLine: true, out message), "put them in: " + message);
			var night = LocalSceneRoomService.Bill(made.Value.BillId);
			Check(night.Appearances.Any(s => s.ArtistId == ours.artistId && s.StartHour == made.Value.Hour && s.Status == SceneBillStatus.Scheduled && s.Set.Count > 0)
				&& night.Appearances.All(s => s.ArtistId != made.Value.DroppedArtistId), $"on the bill at {PlayerDesk.ClockText(made.Value.Hour * 60)} in the dropped act's place, with a set");
			Check(LocalSceneRoomService.Bookings.Any(x => x.ArtistId == ours.artistId && x.Day == made.Value.Day && x.Billed && x.LabelId == desk.Label.labelId),
				"with a booking, so the commission is taken as for any booked night");
			Check(desk.BookerOffers.Count == 0 && desk.DeskMessages().All(m => m.From != room.ContactName), "the offer and the slip are gone");
			ToDay(made.Value.Day + 1);
			var played = LocalSceneRoomService.Bookings.First(x => x.ArtistId == ours.artistId && x.Day == made.Value.Day);
			Check(played.Status == SceneBillStatus.Performed, $"they played the night: {played.Note}");

			// D: a rival's tip about an act he's heard.
			var heard = billed.Select(id => am.GetArtist(id)).Where(x => x != null && string.IsNullOrEmpty(x.labelId)).Select(x => (act: x,
				r: rooms.FirstOrDefault(rm => LocalSceneRoomService.ShowsAt(rm.Id, x.artistId) > 0))).Where(x => x.r != null).OrderBy(x => x.act.artistId, StringComparer.Ordinal).Take(2).ToList();
			Check(heard.Count == 2, "two free acts that have played a Detroit room");
			string rival = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned).labelId;
			foreach (var (act, r) in heard) { desk.ScoutingEvidenceForChecks(act.artistId); desk.MeetBookerForCheck(r.Id); desk.AddRivalLookForCheck(act.artistId, rival, Today()); }
			desk.QueueRivalTipForChecks(heard[0].act, rival);
			var tip = Ring(r => r.Kind == PlayerDesk.RingKind.RivalTip && r.ArtistId == heard[0].act.artistId);
			Check(tip != null && tip.Day == Today() + 1 && tip.Tries.Count == 1, "a booker will ring tomorrow with the tip");
			ToDay(tip.Day); Before(tip.Tries[0]);
			Check(WaitAtDesk(3, out _), "the tip rings");
			card = desk.PickUp(out _);
			string rivalName = ChartManager.Instance.GetLabelName(rival);
			Check(card.Said.Contains(rivalName) && card.Said.Contains(heard[0].act.stageName), $"picked up: {card.From}: {card.Said}");
			var leads = desk.LeadsFor(heard[0].act.artistId, heard[0].act.geography.basePlaceId, Today());
			Check(leads.Any(l => l.Text.Contains(rivalName)), "the act's leads now say a rival was asking: " + string.Join(" / ", leads.Select(l => $"{l.Source}: {l.Text}")));
			desk.QueueRivalTipForChecks(heard[1].act, rival);
			var tip2 = Ring(r => r.Kind == PlayerDesk.RingKind.RivalTip && r.ArtistId == heard[1].act.artistId);
			ToDay(tip2.Day); Before(tip2.Tries[0]);
			GoOut();
			Check(Ring(r => r == tip2) == null && desk.BookerOffers.Count == 0, "a tip that rang while he was out is gone");

			// E: a manager whose yes waits on a time.
			SimulatedArtist agreed = null;
			for (int d = 0; d < 14 && agreed == null; d++) {
				int day = Today();
				foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(x => x.Day == day && x.Status == SceneBillStatus.Scheduled).OrderBy(x => x.StartHour).ToList()) {
					foreach (var s in bill.Appearances.Where(s => s.EndHour <= clock.HardStopHour && s.StartHour >= clock.CurrentHour).OrderBy(s => s.StartHour)) {
						var x = am.GetArtist(s.ArtistId);
						if (x == null || !string.IsNullOrEmpty(x.labelId) || x.prospectMarketStatus != ProspectMarketStatus.Latent || desk.DirectApproaches.Any(q => q.ArtistId == x.artistId)) continue;
						clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), s.StartHour);
						if (!desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, s), out _) || !desk.CanAskDirectApproach(x)) continue;
						foreach (var m in x.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active)) { m.ambition = .95f; if (m.birthYear > 0 && m.GetAge(year) < ActDisposition.MinorAge) m.birthYear = year - 24; }
						x.manager = ManagerArchetype.Shark; x.managerName ??= "Sol Kramer";
						desk.OpenScoutingDossier(x.artistId, out _);
						var p = desk.Slate.FirstOrDefault(q => q.Artist.artistId == x.artistId);
						if (p == null) continue;
						desk.ApproachToSign(p, out _);
						if (desk.StepNeedingATime(x) is { Kind: ScoutingApproachStepKind.Manager }) { agreed = x; break; }
					}
					if (agreed != null) break;
				}
				if (agreed == null) NextDay();
			}
			Check(agreed != null, $"{agreed?.stageName}'s manager said yes and is waiting on a time");
			var step = desk.StepNeedingATime(agreed);
			for (int i = 0; i < 10 && Ring(r => r.Kind == PlayerDesk.RingKind.Visit && r.ArtistId == agreed.artistId) == null && step.Offered[0].Day - Today() > 1; i++) NextDay();
			var visit = Ring(r => r.Kind == PlayerDesk.RingKind.Visit && r.ArtistId == agreed.artistId);
			Check(visit != null && visit.Day == Today() && step.Offered[0].Day - Today() is >= 1 and <= 2,
				$"he comes by a day or two before the first time ({PlayerDesk.WhenText(step.Offered[0].Day, step.Offered[0].Minute)}), at {PlayerDesk.ClockText(visit?.Tries[0] ?? 0)}");
			Before(visit.Tries[0]); GoOut();
			var note = desk.DoorNotes.LastOrDefault(n => n.ArtistId == agreed.artistId);
			Check(note != null && note.Text.Contains(PlayerDesk.WhenText(step.Offered[0].Day, step.Offered[0].Minute)), $"out: a note in the door: \"{note?.Text}\"");
			Check(desk.ReadDoorNote(note.Id) != null && desk.DoorNotes.All(n => n.Id != note.Id), "read, and it's out of the door");
			desk.QueueVisitForChecks(agreed.artistId, Now() + 30);
			Check(WaitAtDesk(1, out message) && desk.Calling == PlayerDesk.CallerAt.Door, "a knock stops the wait: " + message);
			int notes0 = desk.DoorNotes.Count;
			desk.PassTime(1, out _);
			Check(desk.Calling == PlayerDesk.CallerAt.None && desk.DoorNotes.Count == notes0 + 1, "a knock left unanswered leaves a note");
			desk.QueueVisitForChecks(agreed.artistId, Now() + 30);
			WaitAtDesk(1, out _);
			card = desk.PickUp(out _);
			Check(card.AtTheDoor && card.ArtistId == agreed.artistId && card.Said.Contains("sit down"), $"opened the door: {card.From}: {card.Said}");
			int pick = step.Offered.FindIndex(t => t.Day > Today() || t.Day == Today() && t.Minute > Now());
			Check(desk.BookApproachStep(agreed.artistId, ScoutingApproachStepKind.Manager, pick, out message), "a time picked at the door: " + message);
			var booked = desk.Agreement(agreed).Steps.First(s => s.Kind == ScoutingApproachStepKind.Manager);
			ToDay(booked.Day.Value);
			while ((Today() < booked.Day || Now() <= booked.Minute) && desk.PassTime(1, out _)) { }
			if (Today() == booked.Day && Now() <= booked.Minute) NextDay();
			var stood = desk.DoorNotes.LastOrDefault(n => n.ArtistId == agreed.artistId);
			Check(stood != null && stood.From == booked.Who && stood.Text.Contains("I can see you"), $"stood up, the new times are in the door: \"{stood?.Text}\"");

			GD.Print($"CALLER_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception ex) {
			GD.PrintErr($"CALLER_CHECKS_FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
			GetTree().Quit(1);
		}
	}
}
