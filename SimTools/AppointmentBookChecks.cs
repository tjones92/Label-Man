using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The appointment book (SimTools/OnboardingTutorialDirective.md §4c, T5 step 1), through the real desk.
///
/// A: a heard act with a shark for a manager says yes; the sit-down comes with two proposed times (weekday office hours, or
///    the club booker's open nights), the same ones on a second read, and nothing booked. The book lists it as a time to
///    pick; The Talk offers both times as lines. Picking one writes it in the book. Before the day the sit-down refuses,
///    naming the time, and costs nothing.
/// B: on the day, letting the clock pass the hour misses it: the manager proposes two new times (a note says so), the yes
///    still holds. Booked again and kept: pressed in the morning, the Label Man goes there, waits for the hour, and the
///    hour-long sit-down ends an hour after the start. Terms open.
/// C: a second act stood up twice: the second miss ends the yes (let lapse). The day turning over the hour counts too.
/// D: a family with a minor: one missed Sunday dinner ends the yes.
/// Rapport (T5 step 2), along the way: a woman's name reads as a woman; the shark's miss costs a point and puts his price up
/// at the table, the kept sit-down wins one back; the second act's two misses leave its manager cool; a manager of two acts
/// is one man; the archetype rules (how many misses, what each costs, a visionary's patience, the cap on holding firm).
/// The book never puts a badge or a reminder anywhere; that's a UI matter, checked by FirstWeekUiChecks.
///   --path . SimTools/AppointmentBookChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints APPOINTMENT_BOOK_CHECKS_PASS / _FAIL.
/// </summary>
public partial class AppointmentBookChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
			const string city = "detroit";
			Check(desk.FoundLabel("Book Checks", city, out string message), message);
			desk.DismissLienMorning();
			int Today() => LocalSceneRoomService.Day(clock.CurrentDate);
			int Now() => clock.CurrentHour * 60 + clock.CurrentMinute;
			void NextDay() {
				clock.EndDay();
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
			}
			SimulatedArtist HearNew(int days) {
				for (int d = 0; d < days; d++) {
					int day = Today();
					foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList())
						foreach (var slot in bill.Appearances.Where(s => s.EndHour <= clock.HardStopHour && s.StartHour >= clock.CurrentHour).OrderBy(s => s.StartHour)) {
							var a = am.GetArtist(slot.ArtistId);
							if (a == null || !string.IsNullOrEmpty(a.labelId) || a.prospectMarketStatus != ProspectMarketStatus.Latent ||
								desk.DirectApproaches.Any(x => x.ArtistId == a.artistId)) continue;
							clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
							if (desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out _) && desk.CanAskDirectApproach(a)) return a;
						}
					NextDay();
				}
				return null;
			}
			// The act's frictions, set through its real fields: hungry, of age (or with one minor), and a shark managing.
			void Shape(SimulatedArtist a, bool minor) {
				var people = a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).OrderBy(m => m.personId, StringComparer.Ordinal).ToList();
				int y = clock.CurrentDate.year;
				foreach (var m in people) { m.ambition = .95f; if (m.birthYear > 0 && m.GetAge(y) < ActDisposition.MinorAge) m.birthYear = y - 24; }
				if (minor) people[0].birthYear = y - 17;
				a.manager = ManagerArchetype.Shark; a.managerName ??= "Sol Kramer";
				var r = desk.ReadDisposition(a);
				Check(r.Kind == DealDisposition.Hungry && r.DemandingManager && r.Holdout == null && (r.Minors.Count > 0) == minor,
					$"{a.stageName}: hungry, a shark managing{(minor ? ", one member under 21" : "")}");
			}
			ScoutingDirectApproach Ask(SimulatedArtist a) {
				desk.OpenScoutingDossier(a.artistId, out _);
				var p = desk.Slate.First(x => x.Artist.artistId == a.artistId);
				Check(!desk.ApproachToSign(p, out message) && desk.Agreement(a) is { } yes && yes.Steps.Count > 0, $"{a.stageName} says yes, with steps: {message}");
				return desk.Agreement(a);
			}
			// Let the clock run past a minute of today, an hour at a time, through the desk's own wait.
			void WaitPast(int minute) { while (Now() <= minute && desk.PassTime(1, out _)) { } }

			// ---- A: two times, picked in The Talk, written in the book ----
			var a1 = HearNew(14);
			Check(a1 != null, $"heard an act to ask: {a1?.stageName}");
			Shape(a1, minor: false);
			// A woman managing an act reads as a woman (playtest 2026-11-27: Denise Patterson was "him").
			string ownName = a1.managerName;
			a1.managerName = "Denise Patterson";
			var denise = ManagerIdentity.Read(a1, Today());
			Check(denise.Name != "Denise Patterson" || denise.Female && denise.Him == "her" && !denise.Place.Contains("his "),
				$"Denise Patterson ({denise.Kind}) reads as a woman: {denise.He}/{denise.Him}, at {denise.Place}");
			a1.managerName = "Sol Kramer";
			Check(!ManagerIdentity.Read(a1, Today()).Female, "Sol Kramer reads as a man");
			a1.managerName = ownName;
			int day0 = Today();
			var yes1 = Ask(a1);
			var sit = yes1.Steps.Single(s => s.Kind == ScoutingApproachStepKind.Manager);
			var who = ManagerIdentity.Read(a1, day0);
			Check(PlayerDesk.NeedsATime(sit) && sit.Offered.Count == 2 && sit.Minute == null && sit.Day == sit.Offered[0].Day &&
				sit.Offered.All(t => t.Day > day0 && t.Minute >= clock.WorkdayStartHour * 60 && t.Minute + 60 <= clock.HardStopHour * 60) &&
				sit.Offered[1].Day > sit.Offered[0].Day,
				$"the sit-down ({who.Kind}) comes with two times on two days, inside the working day: {PlayerDesk.OfferedText(sit)}");
			if (who.Kind is not (ManagerKind.ClubBooker or ManagerKind.Family))
				Check(sit.Offered.All(t => !LocalSceneRoomService.Date(t.Day).IsWeekend), "a manager at his office proposes weekdays");
			var again = desk.StepsFor(desk.ReadDisposition(a1), a1, day0).Single(s => s.Kind == ScoutingApproachStepKind.Manager);
			Check(again.Offered.Select(t => (t.Day, t.Minute)).SequenceEqual(sit.Offered.Select(t => (t.Day, t.Minute))), "the same yes on the same day proposes the same times");
			Check(message.Contains("Pick one") && message.Contains(PlayerDesk.WhenText(sit.Offered[0].Day, sit.Offered[0].Minute)), "the yes names both times: " + message);
			var pickEntry = desk.AppointmentBook().SingleOrDefault(e => e.ArtistId == a1.artistId);
			Check(pickEntry?.Kind == BookEntryKind.PickATime && pickEntry.Offered.Count == 2, "the book lists it as a time still to pick");
			var talk = desk.BuildTalkScene(a1.artistId);
			var lines = talk.Options.Where(o => o.Verb is TalkVerb.BookFirst or TalkVerb.BookSecond).ToList();
			Check(lines.Count == 2 && lines.All(o => o.Enabled && o.Line.EndsWith("suits me.") && o.Cost.Contains("nobody will remind you")) && !talk.Options.Any(o => o.Verb == TalkVerb.Step),
				"The Talk offers both times as lines, and no sit-down until one is picked: " + string.Join(" / ", lines.Select(o => o.Line)));
			int before = Now();
			Check(!desk.TakeApproachStep(a1.artistId, ScoutingApproachStepKind.Manager, out message) && message.StartsWith("Pick a time first") && Now() == before,
				"no sit-down without a time: " + message);
			Check(desk.SayInTalk(a1.artistId, TalkVerb.BookFirst, out message, out _) && sit.Minute == sit.Offered[0].Minute && sit.Day == sit.Offered[0].Day,
				"picked in The Talk: " + message);
			var booked = desk.AppointmentBook().Single(e => e.ArtistId == a1.artistId);
			Check(booked.Kind == BookEntryKind.SitDown && booked.Day == sit.Day && booked.Minute == sit.Minute && booked.Hours == PlayerDesk.ManagerSitDownHours &&
				booked.What.Contains(sit.Who) && !booked.CanGo, $"the book has it: {booked.What}, {PlayerDesk.WhenText(booked.Day, booked.Minute.Value)}, at {booked.Place}");
			Check(!desk.TakeApproachStep(a1.artistId, ScoutingApproachStepKind.Manager, out message) && message.Contains(PlayerDesk.ClockText(sit.Minute.Value)) && Now() == before,
				"before the day, the sit-down refuses and names the time: " + message);
			Check(!desk.BuildTalkScene(a1.artistId).Options.Any(o => o.Verb is TalkVerb.BookFirst or TalkVerb.BookSecond), "once picked, the times are no longer lines");

			// ---- B: the day comes, the hour passes: missed. Booked again, and kept ----
			while (Today() < sit.Day) NextDay();
			Check(desk.AppointmentBook().Single(e => e.ArtistId == a1.artistId).CanGo == (Now() <= sit.Minute), "on the day, before the hour, the book can send him there");
			int firstStart = sit.Minute.Value;
			WaitPast(firstStart);
			Check(sit.Missed == 1 && PlayerDesk.NeedsATime(sit) && sit.Offered.All(t => t.Day >= Today() + 2) && desk.Agreement(a1) != null,
				$"the hour passed with him at the desk: missed once, two new times ({PlayerDesk.OfferedText(sit)}), and the yes still holds");
			Check(desk.Log.Take(6).Any(l => l.Contains("never came") && l.Contains(sit.Who) && l.Contains("You owe me")), "a note under the door, in a shark's words: " + desk.Log[0]);
			var r1 = desk.ReadRapport(a1);
			Check(r1.Misses == 1 && r1.Score == -1 && r1.Level == RapportLevel.Cool && PlayerDesk.RapportReservation(r1) > PlayerDesk.SharkMissPremium,
				$"rapport with {r1.Name}: the miss costs a point ({r1.Score}, {r1.Level}), and a shark puts his price up ({PlayerDesk.RapportReservation(r1):0.00})");
			Check(desk.BookApproachStep(a1.artistId, ScoutingApproachStepKind.Manager, 0, out message), "booked again: " + message);
			while (Today() < sit.Day) NextDay();
			Check(Now() < sit.Minute, "on the new day, it's morning before the hour");
			Check(desk.TakeApproachStep(a1.artistId, ScoutingApproachStepKind.Manager, out message) && sit.Done && sit.DoneDay == Today() &&
				Now() == sit.Minute + PlayerDesk.ManagerSitDownHours * 60, $"kept: he went there, waited for {PlayerDesk.ClockText(sit.Minute.Value)} and sat down for the hour: {message}");
			r1 = desk.ReadRapport(a1);
			Check(r1.Score == 0 && r1.Misses == 1 && r1.Entries.Any(e => e.Kind == RapportSourceKind.KeptSitDown) &&
				Mathf.IsEqualApprox(PlayerDesk.RapportReservation(r1), PlayerDesk.SharkMissPremium) && PlayerDesk.RapportTableLine(r1, ManagerIdentity.Read(a1, Today())).Contains("You owe me"),
				$"kept: rapport back to even ({r1.Score}), but the shark still means to bill the afternoon: {PlayerDesk.RapportTableLine(r1, ManagerIdentity.Read(a1, Today()))}");
			Check(desk.RapportSummary(a1.artistId) is null == (PlayerDesk.InstinctScore(desk.InstinctProfile, ExecutiveVoice.Fixer) < 3),
				"the act's card names the rapport only for a Fixer of 3+: " + (desk.RapportSummary(a1.artistId) ?? "(no read)"));
			// The table: his price is up and he says why.
			var p1 = desk.Slate.First(x => x.Artist.artistId == a1.artistId);
			desk.ApproachToSign(p1, out message);
			if (p1.Talk != null)
				Check(p1.Talk.outlookStated.Any(l => l.Contains("You owe me")), $"at the table the shark says the afternoon is on the bill ({message}; patience {p1.Talk.patienceMax}, stage {p1.Talk.stage}): " + string.Join(" / ", p1.Talk.outlookStated));
			else GD.Print($"note: {a1.stageName} signs without a table ({p1.Posture}); the shark's line wasn't seen there");
			Check((desk.AgreedToSign(a1) || a1.labelId == desk.Label.labelId) && !desk.AppointmentBook().Any(e => e.ArtistId == a1.artistId), $"the steps are done and the book is clear of it ({desk.AgreedToSign(a1)} {yes1.Status} label={a1.labelId} {a1.lifecycleStatus} {string.Join(",", yes1.Steps.Select(s => s.Kind + ":" + s.Done))}; {string.Join(" / ", desk.AppointmentBook().Where(e => e.ArtistId == a1.artistId).Select(e => e.Kind + " " + e.What))})");

			// ---- C: stood up twice, the yes ends. The day turning counts as the hour passing ----
			NextDay();
			var a2 = HearNew(14);
			if (a2 == null) GD.Print("note: no second act heard inside 14 days; Part C not exercised");
			else {
				Shape(a2, minor: false);
				var yes2 = Ask(a2);
				var s2 = yes2.Steps.Single(s => s.Kind == ScoutingApproachStepKind.Manager);
				Check(desk.BookApproachStep(a2.artistId, s2.Kind, 1, out message) && s2.Day == s2.Offered[1].Day, "the second time picked: " + message);
				for (int until = s2.Day.Value; Today() <= until;) NextDay();
				Check(s2.Missed == 1 && PlayerDesk.NeedsATime(s2) && yes2.Status == ScoutingApproachStatus.Agreed, "the day turned over the hour: missed once, new times");
				Check(desk.BookApproachStep(a2.artistId, s2.Kind, 0, out message), "booked again: " + message);
				int regardBefore = desk.ReadRegard(a2).Score;
				for (int until = s2.Day.Value; Today() <= until;) NextDay();
				Check(yes2.Status == ScoutingApproachStatus.Lapsed && yes2.LetLapse && desk.Agreement(a2) == null && desk.ReadRegard(a2).Score < regardBefore,
					$"stood up twice: the yes is off, let lapse, and {a2.stageName} remember it (regard {regardBefore} → {desk.ReadRegard(a2).Score})");
				Check(desk.Log.Take(6).Any(l => l.Contains("shopping")), "the shark shops the act: " + desk.Log[0]);
				var r2 = desk.ReadRapport(a2);
				Check(r2.Misses == 2 && r2.Level == RapportLevel.Cool && PlayerDesk.RapportPatience(r2) == -1,
					$"two misses leave {r2.Name} cool ({r2.Score}) and a round shorter at the table");
				// One man, two acts: give a2 a1's manager and, where he reads as the same kind of man, he remembers both.
				string a2Name = a2.managerName;
				a2.managerName = a1.managerName;
				var m1 = ManagerIdentity.Read(a1, Today()); var m2 = ManagerIdentity.Read(a2, Today());
				if (PlayerDesk.ManagerKey(m1) == PlayerDesk.ManagerKey(m2)) {
					var shared = desk.ReadRapport(m2);
					Check(shared.Entries.Any(e => e.ArtistId == a1.artistId), $"{m2.Name} manages both acts and remembers the sit-down for {a1.stageName}");
				} else {
					GD.Print($"note: the two acts read as different kinds of manager ({m1.Kind}, {m2.Kind}); the shared key is checked on the person");
					var same = new ManagerRead { Kind = m1.Kind, Archetype = m1.Archetype, Name = " " + m1.Name.ToUpperInvariant() };
					Check(desk.ReadRapport(same).Entries.Any(e => e.ArtistId == a1.artistId), $"rapport is keyed on the person: {m1.Name} remembers {a1.stageName}'s sit-down whichever act he's managing");
				}
				a2.managerName = a2Name;
			}

			// ---- Rapport's archetype rules ----
			Check(PlayerDesk.MissesBeforeLapseFor(ManagerArchetype.LocalHustler) == 3 && PlayerDesk.MissesBeforeLapseFor(ManagerArchetype.Visionary) == 3 &&
				PlayerDesk.MissesBeforeLapseFor(ManagerArchetype.Shark) == 2 && PlayerDesk.MissesBeforeLapseFor(ManagerArchetype.Svengali) == 2,
				"a hustler or a visionary waits out three misses, a shark or a svengali two");
			Check(PlayerDesk.MissPoints(ManagerArchetype.Svengali, 1) == -3 && PlayerDesk.MissPoints(ManagerArchetype.LocalHustler, 1) == -1 &&
				PlayerDesk.MissPoints(ManagerArchetype.Visionary, 2) == -2, "a svengali takes the first miss personally (-3); a hustler shrugs (-1); a visionary's second costs -2");
			var vis = new RapportRead { Archetype = ManagerArchetype.Visionary, Misses = 2, Score = -3, Level = RapportLevel.Sour };
			Check(PlayerDesk.RapportPatience(vis) == -4 && PlayerDesk.RapportReservation(vis) > 0, "a sour visionary with two misses: four rounds shorter, and harder");
			var close = new RapportRead { Archetype = ManagerArchetype.LocalHustler, Score = 3, Level = RapportLevel.Close };
			Check(PlayerDesk.RapportPatience(close) == 2 && PlayerDesk.RapportReservation(close) < 0, "a manager in your corner: two more rounds, and he comes further");

			// ---- D: a family doesn't set a place twice ----
			var a3 = HearNew(14);
			if (a3 == null) GD.Print("note: no third act heard inside 14 days; Part D not exercised");
			else {
				Shape(a3, minor: true);
				var yes3 = Ask(a3);
				var dinner = yes3.Steps.Single(s => s.Kind == ScoutingApproachStepKind.Parents);
				Check(dinner.Offered.All(t => LocalSceneRoomService.Date(t.Day).DayOfWeek == DayOfWeek.Sunday && t.Minute == PlayerDesk.SundayDinnerHour * 60),
					"the family offers Sunday dinner after church, on two Sundays: " + PlayerDesk.OfferedText(dinner));
				Check(desk.BuildTalkScene(a3.artistId).Options.Any(o => o.Verb == TalkVerb.BookFirst && o.Step == ScoutingApproachStepKind.Parents), "the family's times come first in The Talk");
				Check(desk.BookApproachStep(a3.artistId, dinner.Kind, 0, out message), "Sunday dinner booked: " + message);
				while (Today() < dinner.Day) NextDay();
				WaitPast(dinner.Minute.Value);
				Check(yes3.Status == ScoutingApproachStatus.Lapsed && yes3.LetLapse && dinner.Missed == 1 && desk.Log[0].Contains("Sunday dinner"),
					"one missed Sunday dinner and the yes is off: " + desk.Log[0]);
			}
			GD.Print($"APPOINTMENT_BOOK_CHECKS_PASS ({checks} checks)");
		} catch (Exception e) {
			GD.PrintErr("APPOINTMENT_BOOK_CHECKS_FAIL: " + e.Message + "\n" + e.StackTrace);
		}
		GetTree().Quit();
	}
}
