using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Studio slice 2: named engineers on duty, room acoustics that suit some songs over others, and return
/// visits that remember the act. Run: SimTools/StudioReturnVisitRunner.tscn -- --seed=N</summary>
public partial class StudioReturnVisitRunner : Node {
    public override void _Ready() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        string slot = "studio-return-" + Guid.NewGuid().ToString("N");
        try {
            Check(StudioExperience.Enabled, "Incremental rollout enabled");
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            Check(desk.FoundLabel("Return Studio", "nashville", out string message), "Found: " + message);
            var artist = ArtistManager.Instance.GetAllArtists().First(a => a.members.Count > 2 &&
                a.members.Any(m => m.primaryRole == MusicianRole.Drums) &&
                LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, a.members.Select(m => m.personId), clock.CurrentDate, 540, 1080));
            ChartManager.Instance.GetLabelById(artist.labelId)?.roster.Remove(artist);
            artist.labelId = desk.Label.labelId; artist.artistCreativeControl = false; artist.instrumentalPerformance = false;
            desk.Label.roster.Add(artist);
            var choices = new List<PlayerDesk.MaterialChoice> {
                new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Return Side", Detail = "their own" },
                new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Second Side", Detail = "their own" }
            };
            var tiers = StudioRoomCatalog.Available(desk.Label.homeCityId, desk.Label.homeRegion);
            Check(tiers.Length >= 2, "Nashville offers more than one local room");

            // Rooms and engineers are pure keyed derivations.
            GD.Seed(800); uint next = GD.Randi(); GD.Seed(800);
            var budget = desk.StudioRoomFor(tiers[0], artist.artistId);
            Check(budget.CapabilityVersion == 4 && !string.IsNullOrEmpty(budget.EngineerName) && Json(budget) == Json(desk.StudioRoomFor(tiers[0], artist.artistId)) &&
                GD.Randi() == next, "A room card names its engineer on duty, is stable, and draws no gameplay RNG");
            Check(budget.FamiliarDates == 0 && budget.SetupMinutes == StudioPeople.SetupMinutes(budget, false) && desk.StudioMemory(budget, artist.artistId) == null,
                "A first meeting has no memory and the full setup");

            // Personnel availability: a chief and a second engineer; the chief is away on roughly one day in six and
            // otherwise often booked with the room's regulars, so the room puts the second on unless asked.
            var day = clock.CurrentDate; var staffed = new List<StudioRoomSnapshot>(); int away = 0;
            for (int d = 0; d < 90; d++) {
                var room = StudioRoomCatalog.Local(desk.Label.homeCityId, desk.Label.homeRegion, tiers[0]);
                var date = day; for (int k = 0; k < d; k++) date = date.AddDays(1);
                StudioRoomCatalog.Staff(room, tiers[0], date); staffed.Add(room);
                if (StudioPeople.Engineers(room, tiers[0], date).Single(e => e.Chief).Away) away++;
            }
            var house = staffed.FirstOrDefault(r => r.EngineerHouse); var relief = staffed.FirstOrDefault(r => !r.EngineerHouse);
            Check(house != null && relief != null && house.EngineerId != relief.EngineerId && away is > 5 and < 30 &&
                staffed.Where(r => r.EngineerHouse).Select(r => r.EngineerName).Distinct().Count() == 1 && staffed.Select(r => r.EngineerId).Distinct().Count() == 2,
                $"One chief and one second engineer; the chief away on {away} of 90 days");
            Check(relief.EngineerEar <= .75f - .12f + .0001f && house.EngineerEar >= .45f - .0001f, "A second engineer hears less than the room's range allows its chief");

            // Same song, different rooms: acoustics suit some songs more than others.
            StudioSongStructure Shape(float power, float lyric, float ensemble) => new() { DurationSeconds = 160, PowerDemand = power, LyricDemand = lyric, EnsembleDemand = ensemble, FeelDemand = .5f,
                Sections = new() { new() { Kind = StudioSectionKind.Verse, Label = "the first verse" } } };
            var ballad = Shape(.35f, .85f, .3f); var stomper = Shape(.85f, .3f, .8f);
            StudioRoomSnapshot Room(PlayerDesk.StudioTier t) {
                var r = StudioRoomCatalog.Local(desk.Label.homeCityId, desk.Label.homeRegion, t); StudioRoomCatalog.Staff(r, t, day);
                r.EngineerHands = .5f; r.EngineerHabit = StudioEngineerHabit.ListensHard; return r;
            }
            var close = Room(PlayerDesk.StudioTier.Budget); var big = Room(PlayerDesk.StudioTier.Top);
            var balladClose = StudioPeople.RoomEffect(close, ballad); var balladBig = StudioPeople.RoomEffect(big, ballad);
            var stompClose = StudioPeople.RoomEffect(close, stomper); var stompBig = StudioPeople.RoomEffect(big, stomper);
            Check(balladClose.Vocal > balladBig.Vocal && stompBig.Feel > stompClose.Feel && stompClose.Capture < balladClose.Capture,
                $"The close room flatters a ballad's vocal, the lively room lifts a stomper, and loud songs spill in the close room ({balladClose.Vocal:F3}/{balladBig.Vocal:F3}, {stompBig.Feel:F3}/{stompClose.Feel:F3})");
            Check(new[] { balladClose, balladBig, stompClose, stompBig }.All(e => Math.Abs(e.Vocal) <= .04f && Math.Abs(e.Feel) <= .04f && e.Capture is >= -.05f and <= .04f),
                "Room effects are bounded to a few hundredths");
            var legacy = StudioRoomCatalog.Local(desk.Label.homeCityId, desk.Label.homeRegion, tiers[0]);
            Check(StudioPeople.RoomEffect(legacy, stomper) == (0f, 0f, 0f) && PlayerDesk.StudioEngineerExpertise(legacy) == .65f && StudioPeople.SongInRoom(legacy, stomper, "x") == null,
                "Pre-v3 rooms keep the original behaviour: no acoustics, the original engineer read");
            Check(StudioPeople.SongInRoom(close, ballad, "Ballad") == StudioPeople.SongInRoom(close, ballad, "Ballad") && StudioPeople.SongInRoom(close, ballad, "Ballad").Contains("\"Ballad\""),
                "The engineer's opinion of a song in the room is stable and attributed");

            // A first date in the room: the room effect reaches the actual pass.
            Check(desk.StartSession(artist, choices, tiers[0], 4, out message, overrideRefusal: true), "Book first date: " + message);
            var session = desk.Session;
            Check(session.Room.CapabilityVersion == 4 && session.ElapsedMinutes == session.Room.SetupMinutes && session.ActionLog.Any(l => l.StartsWith("Engineer: ")),
                "The booked room snapshots the engineer and spends that room's setup");
            var before = desk.CaptureState(); var date0 = clock.CurrentDate; int h0 = clock.CurrentHour, m0 = clock.CurrentMinute;
            Check(desk.CutStudioTake(0, out message), "Cut: " + message);
            var withRoom = session.Cuts[0].Takes[0];
            var effect = StudioPeople.RoomEffect(session.Room, session.Cuts[0].Structure);
            Check(desk.RestoreState(before, out message), "Restore: " + message); clock.RestoreClock(date0, h0, m0);
            desk.Session.Room.CapabilityVersion = 2;
            Check(desk.CutStudioTake(0, out message), "Cut without acoustics: " + message);
            var without = desk.Session.Cuts[0].Takes[0];
            bool Near(float a, float b, float d) => Math.Abs(a - b - d) < .0001f || a is 0 or 1 || b is 0 or 1;
            Check(Near(withRoom.Vocal, without.Vocal, effect.Vocal) && Near(withRoom.Feel, without.Feel, effect.Feel) && Near(withRoom.Capture, without.Capture, effect.Capture),
                "Under identical draws the pass differs by exactly the room and engineer effect");
            Check(desk.RestoreState(before, out message), "Restore again: " + message); clock.RestoreClock(date0, h0, m0);
            session = desk.Session;
            Check(desk.CutStudioTake(0, out message) && desk.CutStudioTake(0, out message) && desk.CutStudioTake(1, out message), "Work the date: " + message);
            desk.KeepTake(0, 1); desk.KeepTake(1, 0);
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "Roundtrip v3 room: " + message);
            Check(desk.Session.Room.EngineerName == session.Room.EngineerName && desk.Session.Room.CapabilityVersion == 4, $"Engineer and acoustics survive save/load");
            var badRoom = PendingSessionSaveData.From(desk.Session); badRoom.Flow.Room.EngineerEar = 5;
            bool rejected = false; try { badRoom.ToSession(Array.Empty<PlayerDesk.Song>()); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "A saved room with an impossible engineer is rejected");
            string flaggedBefore = string.Join(",", desk.Session.Cuts.SelectMany(c => c.Takes.Where(t => t.TroubleSection >= 0).Select(t => c.Structure.Sections[t.TroubleSection].Kind)));
            string engineerId = desk.Session.Room.EngineerId;
            Check(desk.PrintSession(out message), "Print first date: " + message);
            Check(desk.StudioVisits.Count == 1 && desk.StudioVisits[0].Printed && desk.StudioVisits[0].Passes == 3 && desk.StudioVisits[0].EngineerId == engineerId,
                "The finished date is remembered with its engineer and passes");
            Check(desk.StudioVisits[0].FlaggedParts.Sum() == flaggedBefore.Split(',', StringSplitOptions.RemoveEmptyEntries).Length, "The memory counts the parts heard to fail");
            Check(RolodexDirectory.Cards(desk, RolodexRole.Engineer).Count == 1 && RolodexDirectory.Cards(desk, RolodexRole.Engineer)[0].Name == desk.StudioVisits[0].EngineerName,
                "The engineer is filed in the Rolodex");

            // Return visit, same day and room: same engineer, quicker setup, closer read, remembered trouble.
            var again = desk.StudioRoomFor(tiers[0], artist.artistId);
            string memory = desk.StudioMemory(again, artist.artistId);
            Check(again.EngineerId == engineerId && again.FamiliarDates == 1 && again.SetupMinutes < StudioPeople.SetupMinutes(again, false) &&
                PlayerDesk.StudioEngineerExpertise(again) > PlayerDesk.StudioEngineerExpertise(budget) - .0001f && memory != null && memory.Contains(again.EngineerName.Split(' ')[0]),
                "A return visit with the same engineer sets up faster and reads the tape more closely: " + memory);
            Check(!memory.Contains(" he ") && !memory.Contains(" she "), "The memory names the engineer without guessing pronouns");
            var other = desk.StudioRoomFor(tiers[1], artist.artistId);
            Check(other.FamiliarDates == 0 && desk.StudioMemory(other, artist.artistId) == null, "Familiarity belongs to the engineer, not to every room");
            var choicesB = new List<PlayerDesk.MaterialChoice> { new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Third Side", Detail = "their own" } };
            Check(desk.StartSession(artist, choicesB, tiers[0], 2, out message, overrideRefusal: true), "Book return date: " + message);
            Check(desk.Session.ElapsedMinutes == again.SetupMinutes && desk.Session.Room.FamiliarDates == 1, "The return date spends the familiar setup");
            desk.ScrapSession(); desk.ScrapSession();
            Check(desk.StudioVisits.Count == 2 && !desk.StudioVisits[1].Printed && desk.StudioVisits[1].Passes == 0, "An abandoned date is remembered once, as abandoned");

            // History persists and is validated.
            string history = Json(desk.StudioVisits);
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && Json(desk.StudioVisits) == history, "Studio history survives save/load");
            var damaged = desk.CaptureState(); damaged.StudioVisits[0].FlaggedParts = new List<int> { 1 };
            Check(!desk.RestoreState(damaged, out _) && Json(desk.StudioVisits) == history, "Damaged studio history is refused without changing the desk");
            var older = desk.CaptureState(); older.StudioVisits = null;
            Check(desk.RestoreState(older, out message) && desk.StudioVisits.Count == 0 && desk.StudioMemory(desk.StudioRoomFor(tiers[0], artist.artistId), artist.artistId) == null,
                "Saves from before studio history load as first meetings");
            GD.Print($"STUDIO_RETURN_CHECK_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_RETURN_CHECK_FAIL " + ex); GetTree().Quit(1); }
        finally { SaveGameService.Delete(slot); }
    }
}
