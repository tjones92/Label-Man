using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Studio: engineers brought in from another room in town, and late-night dates that run past midnight.
/// Every check runs on the real desk and clock.
/// Run: SimTools/StudioNightRunner.tscn -- --seed=N</summary>
public partial class StudioNightRunner : Node {
    public override void _Ready() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value), SaveGameService.TestJsonOptions);
        bool Rejected(PendingSessionSaveData data) { try { data.ToSession(Array.Empty<PlayerDesk.Song>()); return false; } catch (InvalidOperationException) { return true; } }
        try {
            Check(StudioExperience.Enabled, "Incremental rollout enabled");
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            var startDate = clock.CurrentDate;
            Check(desk.FoundLabel("Night Studio", "nashville", out string message), "Found: " + message);
            desk.Label.cashReserves = 80000;
            void At(GameDate date, int hour, int minute = 0) => clock.RestoreClock(date, hour, minute);
            SimulatedArtist Live(SimulatedArtist a) => ArtistManager.Instance.GetArtist(a.artistId);
            bool Free(SimulatedArtist a, GameDate d) => LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, a.members.Select(m => m.personId), d, 0, 1440);
            var band = ArtistManager.Instance.GetAllArtists().Where(a => a.labelId != null && a.lifecycleStatus == ArtistLifecycleStatus.Active && !a.instrumentalPerformance &&
                a.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active) >= 3).OrderBy(a => a.artistId, StringComparer.Ordinal).First(a => Free(a, startDate));
            ChartManager.Instance.GetLabelById(band.labelId)?.roster.Remove(band);
            band.labelId = desk.Label.labelId; band.artistCreativeControl = false; desk.Label.roster.Add(band);
            var one = new List<PlayerDesk.MaterialChoice> { new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Night Side", Detail = "their own" } };

            // ================= Engineers brought in from another room =================
            string budgetId = StudioRoomCatalog.LocalId("nashville", PlayerDesk.StudioTier.Budget), midId = StudioRoomCatalog.LocalId("nashville", PlayerDesk.StudioTier.Mid);
            Check(desk.StudioRoomIds().Contains(budgetId) && desk.StudioRoomIds().Contains(midId), "Nashville has a budget service and a workshop");
            // A first date at the budget service, so the label knows its engineer on duty.
            At(startDate, 9);
            Check(desk.StartSession(Live(band), one, PlayerDesk.StudioTier.Budget, 3, out message, overrideRefusal: true, roomId: budgetId), "First date at the budget service: " + message);
            string guestId = desk.Session.Room.EngineerId, guestName = desk.Session.Room.EngineerName;
            desk.ScrapSession();
            Check(desk.StudioVisits.Any(v => v.EngineerId == guestId), "The label remembers the engineer");
            var host0 = desk.StudioRoom(midId, band.artistId);
            string strangerId = StudioPeople.Engineers(StudioRoomCatalog.ById(StudioRoomCatalog.LocalId("nashville", PlayerDesk.StudioTier.Top), out _), PlayerDesk.StudioTier.Top, startDate)[0].Id;
            Check(desk.StudioGuestEngineer(host0, band.artistId, strangerId, startDate, out _, out _)?.Contains("hasn't worked") == true, "Nobody asks an engineer the label has never worked with");
            Check(desk.StudioGuestEngineers(host0, band.artistId, startDate).Count == 1 && desk.StudioGuestEngineers(host0, band.artistId, startDate).All(g => g.Engineer.Id == guestId),
                "The workshop's card lists the one engineer the label knows from across town");
            var special = desk.StudioRoom(StudioRoomCatalog.SpecialistId("nashville"), band.artistId);
            Check(desk.StudioGuestEngineer(special, band.artistId, guestId, startDate, out _, out _)?.Contains("owner") == true && desk.StudioGuestEngineers(special, band.artistId, startDate).Count == 0,
                "A specialist owner keeps his own board");
            var memphis = desk.StudioRoom(StudioRoomCatalog.LocalId("memphis", PlayerDesk.StudioTier.Budget), band.artistId);
            if (memphis != null) Check(desk.StudioGuestEngineer(memphis, band.artistId, guestId, startDate, out _, out _)?.Contains("Nashville") == true, "An engineer in another town is booked at their own room");
            // Over sixty days: free on quiet days at home, busy otherwise. Pure: asking twice gives the same answer.
            var days = Enumerable.Range(1, 60).Select(d => startDate.AddDays(d)).ToList();
            var answers = days.Select(d => desk.StudioGuestEngineer(desk.StudioRoom(midId, band.artistId, null, d), band.artistId, guestId, d, out _, out _)).ToList();
            Check(answers.SequenceEqual(days.Select(d => desk.StudioGuestEngineer(desk.StudioRoom(midId, band.artistId, null, d), band.artistId, guestId, d, out _, out _))), "Availability is a pure keyed function");
            int freeDays = answers.Count(a => a == null);
            GD.Print($"STUDIO_NIGHT guest {guestName} free on {freeDays} of 60 days; refusals: {string.Join(" | ", answers.Where(a => a != null).Distinct().Take(3))}");
            Check(freeDays > 5 && freeDays < 55, $"The engineer comes across on some days and not others ({freeDays}/60)");
            var freeDay = days[answers.IndexOf(null)];
            var busyDay = days[answers.FindIndex(a => a != null && a.Contains("on the board"))];
            // Booking with the guest.
            At(busyDay, 9);
            float cashBusy = desk.Label.cashReserves;
            Check(!desk.StartSession(Live(band), one, PlayerDesk.StudioTier.Mid, 3, out message, overrideRefusal: true, roomId: midId, engineerId: guestId) && desk.Session == null &&
                desk.Label.cashReserves == cashBusy && message.Contains("on the board"), "On a busy day at home the engineer can't come, and nothing is charged: " + message);
            At(freeDay, 9);
            var offer = desk.StudioOffer(midId, band.artistId, guestId);
            var plain = desk.StudioRoom(midId, band.artistId, null, freeDay);
            var homeRoom = desk.StudioRoom(budgetId, band.artistId, null, freeDay);
            var guest = StudioPeople.Engineers(homeRoom, PlayerDesk.StudioTier.Budget, freeDay).First(e => e.Id == guestId);
            Check(offer.Room.EngineerId == guestId && offer.Room.EngineerHomeRoomId == budgetId && offer.Room.EngineerRequested && offer.Room.EngineerEar == guest.Ear &&
                offer.Room.EngineerHands == guest.Hands && offer.Room.EngineerHabit == guest.Habit, "The guest brings their own ear, hands and habit");
            Check(offer.Room.Intimacy == plain.Intimacy && offer.Room.Liveliness == plain.Liveliness && offer.Room.Control == plain.Control && offer.Room.HourlyRate == plain.HourlyRate,
                "The workshop keeps its own sound and rate");
            Check(offer.Room.EngineerPremium == StudioPeople.GuestFee(homeRoom, guest.Chief) && offer.Room.EngineerPremium > 0, $"The guest's fee is charged by the hour (${offer.Room.EngineerPremium})");
            Check(offer.Room.SetupMinutes == StudioPeople.SetupMinutes(offer.Room, true) + StudioPeople.GuestSetupMinutes && offer.Room.FamiliarDates == 1,
                $"Knows the act (familiar setup) but not the board (+{StudioPeople.GuestSetupMinutes}m): {offer.Room.SetupMinutes}m");
            float before = desk.Label.cashReserves;
            Check(desk.StartSession(Live(band), one, PlayerDesk.StudioTier.Mid, 3, out message, overrideRefusal: true, roomId: midId, engineerId: guestId), "Book the workshop with the guest: " + message);
            var session = desk.Session;
            Check(session.Room.EngineerId == guestId && Math.Abs(before - desk.Label.cashReserves - PlayerDesk.StudioRoomCost(offer.Room, 3)) < .01f &&
                session.ActionLog.Any(l => l.Contains("brought in for the date")) && session.ElapsedMinutes == offer.Room.SetupMinutes,
                "The date has the guest on the board, charges the fee once and spends the longer setup");
            var saved = Clone(desk.CaptureState());
            Check(desk.RestoreState(Clone(saved), out message) && desk.Session.Room.EngineerHomeRoomId == budgetId && desk.Session.Room.EngineerPremium == offer.Room.EngineerPremium,
                "The guest survives a save: " + message);
            var own = PendingSessionSaveData.From(desk.Session); own.Flow.Room.EngineerHomeRoomId = midId;
            var wrong = PendingSessionSaveData.From(desk.Session); wrong.Flow.Room.EngineerHomeRoomId = StudioRoomCatalog.LocalId("nashville", PlayerDesk.StudioTier.Top);
            var away = PendingSessionSaveData.From(desk.Session); away.Flow.Room.EngineerHomeRoomId = StudioRoomCatalog.LocalId("memphis", PlayerDesk.StudioTier.Budget);
            Check(Rejected(own) && Rejected(wrong) && (memphis == null || Rejected(away)), "A saved guest from the host room, the wrong room or another town is rejected");
            Check(desk.CutStudioTake(0, out message), "A pass with the guest: " + message);
            desk.ScrapSession();
            Check(desk.StudioVisits.Any(v => v.EngineerId == guestId && v.RoomId == midId), "The guest's date is remembered at the workshop");
            var nextFree = days.Where((d, i) => answers[i] == null && d > freeDay).FirstOrDefault();
            if (nextFree.year > 0) {
                At(nextFree, 9);
                var again = desk.StudioOffer(midId, band.artistId, guestId);
                Check(again.Room.EngineerId == guestId && again.Room.SetupMinutes == StudioPeople.SetupMinutes(again.Room, true), "Having worked this board once, the guest sets up at the familiar pace");
            }
            var card = RolodexDirectory.Cards(desk, RolodexRole.Engineer).First(c => c.Key == guestId);
            Check(card.Firm == homeRoom.Name, "The Rolodex files the guest under their own room: " + card.Firm);

            // ================= Late-night dates =================
            var evening = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, band.artistId)).FirstOrDefault(o => o != null && o.Local && o.Room.EveningWork && o.Locked == null && !o.Room.Specialist);
            Check(evening != null, "A local room takes evening work");
            var night = days.Last();
            At(night, PlayerDesk.StudioEveningStartHour);
            Check(!desk.StartSession(Live(band), one, evening.Tier, 6, out message, overrideRefusal: true, roomId: evening.Room.Id, options: new StudioBookingOptions { Evening = true }) && desk.Session == null,
                "A plain evening date still ends at the hard stop: " + message);
            Check(!desk.StartSession(Live(band), one, evening.Tier, 3, out message, overrideRefusal: true, roomId: evening.Room.Id, options: new StudioBookingOptions { LateNight = true }) && desk.Session == null,
                "A late night is an evening booking: " + message);
            At(night, PlayerDesk.StudioEveningStartHour, 30);
            Check(!desk.StartSession(Live(band), one, evening.Tier, 8, out message, overrideRefusal: true, roomId: evening.Room.Id, options: new StudioBookingOptions { Evening = true, LateNight = true }) && desk.Session == null,
                "Eight hours from 6:30 runs past 2 a.m.: " + message);
            At(night, PlayerDesk.StudioEveningStartHour);
            before = desk.Label.cashReserves;
            var lateRoom = desk.StudioRoom(evening.Room.Id, band.artistId);
            Check(desk.StartSession(Live(band), one, evening.Tier, 8, out message, overrideRefusal: true, roomId: evening.Room.Id, options: new StudioBookingOptions { Evening = true, LateNight = true }),
                "Book a late night, 6 p.m. to 2 a.m.: " + message);
            session = desk.Session;
            Check(session.Work.LateNight && clock.DayStopMinute == PlayerDesk.StudioLateStopMinute && desk.StudioMinutesLeft(session) == 8 * 60 - session.ElapsedMinutes &&
                Math.Abs(before - desk.Label.cashReserves - Mathf.Round(PlayerDesk.StudioRoomCost(lateRoom, 8) * PlayerDesk.StudioEveningRateFactor)) < 1.01f,
                "The clock runs to 2 a.m. for the date, at the evening rate");
            // The same rehearsal before and after midnight: the small hours tire the band a quarter again.
            int minutes = PlayerDesk.StudioRehearsalMinutesFor(session.Cuts[0]);
            At(night, 22);
            float w0 = session.WorkloadMinutes;
            Check(desk.RehearseStudio(0, out message), "Rehearse at ten: " + message);
            float evening10 = session.WorkloadMinutes - w0;
            At(night, 23, 55);
            Check(desk.RehearseStudio(0, out message) && session.ActionLog.Count(l => l.StartsWith("Midnight came")) == 1, "Rehearse across midnight: " + message);
            At(night, 24, 0);
            w0 = session.WorkloadMinutes;
            Check(desk.RehearseStudio(0, out message), "Rehearse at midnight: " + message);
            float small = session.WorkloadMinutes - w0;
            Check(Math.Abs(small - evening10 - minutes * PlayerDesk.StudioSmallHoursWorkload) < .01f && session.ActionLog.Count(l => l.StartsWith("Midnight came")) == 1,
                $"The small hours tire the band more ({evening10:F1} -> {small:F1} for {minutes}m)");
            Check(clock.GetTimeString() == $"12:{minutes:D2} AM" && clock.CurrentDate == night && !clock.IsDayOver, "Past midnight reads as the small hours of the same working day: " + clock.GetTimeString());
            Check(desk.CutStudioTake(0, out message), "A pass after midnight: " + message);
            saved = Clone(desk.CaptureState());
            int hour = clock.CurrentHour, minute = clock.CurrentMinute;
            Check(desk.RestoreState(Clone(saved), out message) && desk.Session.Work.LateNight && clock.DayStopMinute == PlayerDesk.StudioLateStopMinute, "A late night survives a save: " + message);
            var bad = PendingSessionSaveData.From(desk.Session); bad.Flow.Work.Night = false;
            Check(Rejected(bad), "A saved late night that isn't an evening date is rejected");
            At(night, 25, 30);
            desk.ScrapSession();
            Check(clock.DayStopMinute == clock.HardStopHour * 60 && clock.IsDayOver && clock.MinutesRemainingWithOvertime == 0, "With the date over, nothing else runs at 1:30 a.m.");
            clock.EndDay();
            Check(clock.CurrentDate == night.NextDay() && clock.CurrentHour * 60 + clock.CurrentMinute == 9 * 60 + 30, $"After a 1:30 a.m. finish the next day starts after a night's sleep ({clock.GetTimeString()})");
            At(clock.CurrentDate, clock.HardStopHour);
            var ordinary = clock.CurrentDate;
            clock.EndDay();
            Check(clock.CurrentDate == ordinary.NextDay() && clock.CurrentHour == clock.WorkdayStartHour && clock.CurrentMinute == 0, "An ordinary day still starts at nine");

            GD.Print($"STUDIO_NIGHT_CHECK_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_NIGHT_CHECK_FAIL " + ex); GetTree().Quit(1); }
    }
}
