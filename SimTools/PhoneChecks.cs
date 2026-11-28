using System;
using System.Linq;
using Godot;

/// <summary>
/// The phone rings once (SimTools/OnboardingTutorialDirective.md §4c, T5 step 3), through the real desk.
///
/// A: a known shop's sold-out call goes on the line for tomorrow or later, never a Sunday, with a morning and an afternoon
///    try; queued twice it's still one call. Nothing is open yet.
/// B: on its day, waiting at the desk through the morning try answers it: it's open demand (THE PHONE, the stop row), it's
///    in the ledger, and no pink slip is made for it.
/// C: a second call, on its day, with him out at the station across both tries (a real 30-minute errand each time): the
///    morning try passes to the afternoon, the afternoon one is gone. No open call. At the shop's counter next, they say so
///    once, and only once.
/// D: a stranger shop that rings while he's out leaves nothing to mention; a station-added call rings once.
/// E: a day he ended at nine, at home, is a day by the phone: the call is answered at dawn.
/// F: a jock's call-back about a mailing: answered, he goes in the Rolodex; missed, he doesn't, and nothing says so.
/// G: the book's WAITING ON names the pre-order calls he set in motion, without a time.
///   --path . SimTools/PhoneChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints PHONE_CHECKS_PASS / _FAIL.
/// </summary>
public partial class PhoneChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			const string city = "detroit";
			Check(desk.FoundLabel("Phone Checks", city, out string message), message);
			desk.DismissLienMorning();
			int Today() => LocalSceneRoomService.Day(clock.CurrentDate);
			void NextDay() {
				clock.EndDay();
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
			}
			PendingRing Ring(string recordId) => desk.PendingRingsForChecks.FirstOrDefault(r => r.RecordId == recordId);
			void ToRingDay(PendingRing r) { while (Today() < r.Day) NextDay(); }
			// Put the clock just before a minute of the ring day without moving it through anything.
			void Before(int minute) => clock.RestoreClock(clock.CurrentDate, (minute - 10) / 60, (minute - 10) % 60);

			var shops = desk.StopsInCity(city).Where(s => s.Kind == PlayerDesk.StopKind.Shop).ToList();
			Check(shops.Count >= 3, $"Detroit has shops ({shops.Count})");
			var station = desk.StopsInCity(city).FirstOrDefault(s => s.Kind == PlayerDesk.StopKind.Station);
			Check(station != null, "Detroit has a station to walk to");
			var known = shops[0]; known.LastVisitWeek = 1;
			var known2 = shops[1]; known2.LastVisitWeek = 1;
			var stranger = shops[2]; stranger.LastVisitWeek = 0;

			// A
			int today = Today();
			desk.QueueRingForChecks(known, "chk_ring_a");
			desk.QueueRingForChecks(known, "chk_ring_a");
			var a = Ring("chk_ring_a");
			Check(desk.PendingRingsForChecks.Count(r => r.RecordId == "chk_ring_a") == 1, "queued twice, one call on the line");
			Check(a.Day > today && a.Day <= today + 7, $"it rings in the coming days, from tomorrow (day {a.Day}, today {today})");
			Check(LocalSceneRoomService.Date(a.Day).DayOfWeek != DayOfWeek.Sunday, "never on a Sunday");
			Check(a.Tries.Count == 2 && a.Tries[0] >= 9 * 60 && a.Tries[0] < 12 * 60 && a.Tries[1] >= 13 * 60 && a.Tries[1] < 17 * 60,
				$"a shop tries in the morning and again in the afternoon ({string.Join(", ", a.Tries.Select(PlayerDesk.ClockText))})");
			Check(!desk.HasOpenCall(known.StopId, "chk_ring_a"), "nothing is open before it rings");

			// B
			ToRingDay(a);
			int answered0 = desk.RingsAnswered, first = a.Tries[0];
			Before(first);
			Check(desk.PassTime(1, out message), "wait an hour at the desk: " + message);
			Check(Ring("chk_ring_a") == null && desk.HasOpenCall(known.StopId, "chk_ring_a") && desk.RingsAnswered == answered0 + 1,
				"waiting at the desk through the ring answers it: the call is open");
			Check(desk.Log.Take(5).Any(l => l.Contains(known.DisplayName) && l.Contains("called")), "the call is in the ledger");
			Check(desk.DeskMessages().All(m => m.Tab != "DISTRIBUTION"), "no pink slip for a shop's call: nobody takes messages");

			// C
			desk.QueueRingForChecks(known2, "chk_ring_c");
			var c = Ring("chk_ring_c");
			ToRingDay(c);
			int missed0 = desk.RingsMissed;
			Before(c.Tries[0]);
			Check(desk.AskWhatsOnSurvey(station.StopId, out message), "walk over to the station (30 minutes, out): " + message);
			Check(Ring("chk_ring_c") is { Tries.Count: 1 } && !desk.HasOpenCall(known2.StopId, "chk_ring_c"), "out at the morning try: it'll try again after lunch");
			Before(c.Tries.Count > 0 ? Ring("chk_ring_c").Tries[0] : 13 * 60);
			Check(desk.AskWhatsOnSurvey(station.StopId, out message), "out again at the afternoon try: " + message);
			Check(Ring("chk_ring_c") == null && !desk.HasOpenCall(known2.StopId, "chk_ring_c") && desk.RingsMissed == missed0 + 1,
				"out at both tries: the call is gone, nothing open");
			Check(desk.MissedRingsForChecks.Any(m => m.StopId == known2.StopId), "the shop remembers it rang");
			NextDay();
			int logBefore = desk.Log.Count;
			desk.TouchStopForChecks(known2);
			Check(desk.Log.Count > 0 && desk.Log[0].Contains("I tried you") && desk.Log[0].Contains(known2.DisplayName),
				"at their counter they say so: " + (desk.Log.Count > 0 ? desk.Log[0] : ""));
			int afterMention = desk.Log.Count;
			desk.TouchStopForChecks(known2);
			Check(desk.Log.Count == afterMention && !desk.MissedRingsForChecks.Any(m => m.StopId == known2.StopId), "and only once");

			// D
			desk.QueueRingForChecks(stranger, "chk_ring_d");
			desk.QueueRingForChecks(known, "chk_ring_d2", PlayerDesk.InboundCallReason.StationAdded);
			Check(Ring("chk_ring_d2").Tries.Count == 1, "a station-added call rings once");
			var d = Ring("chk_ring_d");
			ToRingDay(d);
			Before(d.Tries[0]); desk.AskWhatsOnSurvey(station.StopId, out _);
			if (Ring("chk_ring_d") is { } still) { Before(still.Tries[0]); desk.AskWhatsOnSurvey(station.StopId, out _); }
			Check(Ring("chk_ring_d") == null && !desk.HasOpenCall(stranger.StopId, "chk_ring_d") && !desk.MissedRingsForChecks.Any(m => m.StopId == stranger.StopId),
				"a stranger that rang while he was out just rings the next label: nothing to mention");

			// E
			desk.QueueRingForChecks(shops[0], "chk_ring_e");
			var e = Ring("chk_ring_e");
			while (Today() < e.Day) NextDay();
			Check(clock.CurrentHour * 60 + clock.CurrentMinute < e.Tries[0], "the ring day starts before the first try");
			NextDay();
			Check(Ring("chk_ring_e") == null && desk.HasOpenCall(shops[0].StopId, "chk_ring_e"), "a day he ended at nine, at home, was a day by the phone: answered");

			// F
			var region = ChartManager.Instance.GetRegionById(desk.Label.homeRegion);
			var jocks = ChartManager.Instance.ReporterStationsInRegion(region.regionId).Where(s => s != null && !desk.Rolodex.Any(r => r.stationId == s.stationId)).ToList();
			Check(jocks.Count >= 2, $"two reporter stations in {region.regionName} not in the Rolodex ({jocks.Count})");
			desk.QueueDjRingForChecks(jocks[0].stationId, region.regionId, "chk_ring_f");
			var f = Ring("chk_ring_f");
			Check(f.Tries.Count == 1, "a jock rings once");
			ToRingDay(f); Before(f.Tries[0]);
			Check(desk.PassTime(1, out _) && desk.Rolodex.Any(r => r.stationId == jocks[0].stationId), "answered: the jock is in the Rolodex");
			desk.QueueDjRingForChecks(jocks[1].stationId, region.regionId, "chk_ring_f2");
			var f2 = Ring("chk_ring_f2");
			ToRingDay(f2); Before(f2.Tries[0]);
			int logF = desk.Log.Count;
			desk.AskWhatsOnSurvey(station.StopId, out _);
			Check(Ring("chk_ring_f2") == null && !desk.Rolodex.Any(r => r.stationId == jocks[1].stationId), "missed: the spin is gone, and he's not in the Rolodex");

			// G
			desk.QueueRingForChecks(shops[1], "chk_ring_g", PlayerDesk.InboundCallReason.PreOrder);
			Check(desk.WaitingOn().Any(w => w.Contains("chk_ring_g") && w.Contains("ring")), "WAITING ON names the pre-order calls he set in motion");
			Check(desk.WaitingOn().All(w => !w.Contains(" AM") && !w.Contains(" PM")), "with no times");

			GD.Print($"PHONE_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception ex) {
			GD.PrintErr($"PHONE_CHECKS_FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
			GetTree().Quit(1);
		}
	}
}
