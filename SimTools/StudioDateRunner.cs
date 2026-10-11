using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class StudioDateRunner : Node {
    public override async void _Ready() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        IEnumerable<Node> Walk(Node node) { yield return node; foreach (var c in node.GetChildren()) foreach (var n in Walk(c)) yield return n; }
        string slot = "studio-date-" + Guid.NewGuid().ToString("N");
        try {
            Check(StudioExperience.Enabled, "Incremental rollout enabled");
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            Check(desk.FoundLabel("Working Studio", "nashville", out string message), "Found: " + message);
            var artist = ArtistManager.Instance.GetAllArtists().First(a => a.members.Count > 2 &&
                a.members.Any(m => m.primaryRole == MusicianRole.Drums) &&
                LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, a.members.Select(m => m.personId), clock.CurrentDate, 540, 780));
            ChartManager.Instance.GetLabelById(artist.labelId)?.roster.Remove(artist);
            artist.labelId = desk.Label.labelId; artist.artistCreativeControl = false; artist.instrumentalPerformance = false;
            desk.Label.roster.Add(artist);
            var choices = new List<PlayerDesk.MaterialChoice> {
                new() { Kind = PlayerDesk.MaterialKind.Original, Title = "First Side", Detail = "their own" },
                new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Flip Side", Detail = "their own" }
            };
            float cash = desk.Label.cashReserves, debt = artist.unrecoupedAdvance;
            Check(desk.StartSession(artist, choices, PlayerDesk.StudioTier.Budget, 4, out message, overrideRefusal: true), "Book: " + message);
            Check(clock.CurrentHour == 9 && clock.CurrentMinute == 20 && desk.Session.ElapsedMinutes == 20, "Booking spends setup once, not four hours");
            Check(desk.Session.Cuts.All(c => c.Takes.Count == 0 && c.KeptTake == -1), "New date has two uncut, unselected sides");
            Check(desk.Label.cashReserves == cash - desk.Session.Cost && artist.unrecoupedAdvance == debt + desk.Session.Cost, "Room cash and legacy recoupment charged once");
            Check(!LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, desk.Session.ParticipantIds, clock.CurrentDate, 600, 660), "Participants reserved during the paid date");
            Check(!desk.PassTime(1, out _) && clock.CurrentMinute == 20, "Desk waiting cannot spend the running date");
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "Empty date compressed roundtrip: " + message);
            Check(desk.Session.Cuts.Count == 2 && desk.Session.Cuts.All(c => c.Takes.Count == 0) && desk.Session.ElapsedMinutes == 20, "Empty sides, room and clock survive load");

            var main = GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate<UIManager>(); AddChild(main);
            var panel = main.GetNode<PlayerDeskPanel>("PlayerDeskPanel"); panel.OpenAtTab("ROSTER");
            typeof(PlayerDeskPanel).GetField("managingArtistId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(panel, artist.artistId);
            typeof(PlayerDeskPanel).GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(panel, null);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Walk(panel).OfType<Button>().Any(b => b.Text == "CUT A TAKE  ·  12m") && Walk(panel).OfType<Button>().Any(b => b.Text == "FINISH & PRINT MASTERS" && b.Disabled), "Real studio UI exposes work and blocks uncut printing");
            Check(!Walk(panel).OfType<ReadBar>().Any(), "No truth ratings on studio screen");
            var beforeTake = desk.CaptureState(); var takeDate = clock.CurrentDate;
            GD.Seed(700); uint next = GD.Randi(); GD.Seed(700);
            Check(desk.CutStudioTake(0, out message), "Cut: " + message);
            Check(GD.Randi() == next && clock.CurrentMinute == 32 && desk.Session.ElapsedMinutes == 32, "Take spends minutes and no gameplay RNG");
            var freshTake = desk.Session.Cuts[0].Takes[0];
            Check(desk.RestoreState(beforeTake, out message), "Restore fatigue comparator: " + message); clock.RestoreClock(takeDate, 9, 20);
            desk.Session.WorkloadMinutes = 240;
            Check(desk.CutStudioTake(0, out message) && desk.Session.Cuts[0].Takes[0].Vocal < freshTake.Vocal && desk.Session.Cuts[0].Takes[0].Feel < freshTake.Feel,
                "An exhausted lineup loses delivery and feel under the same private draw");
            Check(desk.RestoreState(beforeTake, out message), "Restore rehearsal comparator: " + message); clock.RestoreClock(takeDate, 9, 20);
            desk.Session.Cuts[0].Rehearsals = 2;
            Check(desk.CutStudioTake(0, out message) && desk.Session.Cuts[0].Takes[0].Ensemble > freshTake.Ensemble,
                "Preparation improves coordination under the same private draw");
            Check(desk.RestoreState(beforeTake, out message), "Restore manual continuation: " + message); clock.RestoreClock(takeDate, 9, 20);
            Check(desk.CutStudioTake(0, out message) && Json(desk.Session.Cuts[0].Takes[0]) == Json(freshTake), "Restoring the fresh state reproduces the frozen performance");
            desk.KeepTake(0, 0); string protectedTake = Json(desk.Session.Cuts[0].Takes[0]);
            Check(desk.RehearseStudio(0, out message) && desk.DirectStudio(0, StudioDirection.TightenEnding, out message) && desk.CutStudioTake(0, out message), "Prepare, direct and retry: " + message);
            Check(desk.Session.Cuts[0].KeptTake == 0 && Json(desk.Session.Cuts[0].Takes[0]) == protectedTake, "Rehearsal and retry preserve the selected earlier performance");
            int elapsed = desk.Session.ElapsedMinutes;
            Check(!desk.DirectStudio(0, StudioDirection.TightenEnding, out _) && desk.Session.ElapsedMinutes == elapsed, "Identical direction does not spend time twice");
            float workload = desk.Session.WorkloadMinutes;
            Check(desk.BreakStudio(out _) && desk.Session.WorkloadMinutes < workload && desk.Session.WorkloadMinutes > 0, "Break partially recovers cross-song workload");
            Check(SaveGameService.Save(slot, out message), "Save continuation: " + message);
            Check(desk.CutStudioTake(1, out message), "Next-side performance: " + message);
            string continuation = Json(PendingSessionSaveData.From(desk.Session));
            Check(SaveGameService.Load(slot, out message), "Reload continuation: " + message);
            GD.Seed(99999); _ = GD.Randi();
            Check(desk.CutStudioTake(1, out message) && Json(PendingSessionSaveData.From(desk.Session)) == continuation, "Saved next action yields identical take and evidence despite unrelated RNG");
            Check(desk.Session.WorkloadMinutes > 0 && desk.Session.Cuts[1].Takes[0].TakeId != desk.Session.Cuts[0].Takes[0].TakeId, "Workload crosses songs and take identities stay distinct");
            Check(desk.RunStudioDate(out message) && desk.Session.Cuts[0].KeptTake == 0, "Bounded delegation keeps manual selection: " + message);
            Check(desk.Session.Cuts.All(c => c.KeptTake >= 0), "Delegation provides both sides");
            Check(desk.Session.ElapsedMinutes <= desk.Session.Hours * 60 - 2 * PlayerDesk.StudioFinishMinutesPerSide, "Delegation stays inside the quote and protects finishing");
            float paidCash = desk.Label.cashReserves; int finishStart = clock.CurrentHour * 60 + clock.CurrentMinute;
            int expectedWorked = desk.Session.ElapsedMinutes + 2 * PlayerDesk.StudioFinishMinutesPerSide;
            string roomName = desk.Session.Room.Name;
            Check(desk.PrintSession(out message), "House finish: " + message);
            Check(clock.CurrentHour * 60 + clock.CurrentMinute == finishStart + 20 && desk.Label.cashReserves == paidCash, "Finishing spends quoted minutes and no second room charge");
            Check(desk.Masters.Count == 2 && desk.Masters.All(m => m.StudioHistory?.RoomName == roomName && m.StudioHistory.ElapsedMinutes == expectedWorked), "Both masters retain the date's story");
            string histories = Json(desk.Masters.Select(m => m.StudioHistory).ToArray());
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && histories == Json(desk.Masters.Select(m => m.StudioHistory).ToArray()), "Master histories survive compressed save/load");
            Check(!desk.PrintSession(out _) && desk.Masters.Count == 2, "Finished date cannot print or charge again");
            Check(desk.StartSession(ArtistManager.Instance.GetArtist(artist.artistId), choices, PlayerDesk.StudioTier.Budget, 3, out message, overrideRefusal: true), "Next modest date: " + message);
            paidCash = desk.Label.cashReserves; var people = desk.Session.ParticipantIds.ToArray(); var date = desk.Session.Date; int start = desk.Session.StartMinute;
            desk.ScrapSession();
            Check(desk.Label.cashReserves == paidCash && LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, people, date, start, start + 180), "Abandoning releases reservations without refunding incurred cost");
            GD.Print($"STUDIO_DATE_CHECK_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_DATE_CHECK_FAIL " + ex); GetTree().Quit(1); }
        finally { SaveGameService.Delete(slot); }
    }
}
