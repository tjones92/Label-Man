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
        async System.Threading.Tasks.Task Screenshot(string name) {
            if (DisplayServer.GetName() == "headless" || !OS.GetCmdlineUserArgs().Contains("--studio-ui-capture")) return;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/studio-window-" + name + ".png"));
        }
        string slot = "studio-date-" + Guid.NewGuid().ToString("N");
        bool invalidShapeRejected;
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
            int setup = desk.Session.Room.SetupMinutes;
            Check(setup is >= 10 and <= 20 && clock.CurrentHour == 9 && clock.CurrentMinute == setup && desk.Session.ElapsedMinutes == setup, "Booking spends the room's setup once, not four hours");
            Check(desk.Session.Cuts.All(c => c.Takes.Count == 0 && c.KeptTake == -1), "New date has two uncut, unselected sides");
            string uncutDate = Json(PendingSessionSaveData.From(desk.Session));
            Check(!desk.FinishStudioSide(0, StudioFinish.SoftenTop, out _) && Json(PendingSessionSaveData.From(desk.Session)) == uncutDate,
                "Processing requires a kept source and cannot spend an uncut date");
            Check(desk.Label.cashReserves == cash - desk.Session.Cost && artist.unrecoupedAdvance == debt + desk.Session.Cost, "Room cash and legacy recoupment charged once");
            Check(!LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, desk.Session.ParticipantIds, clock.CurrentDate, 600, 660), "Participants reserved during the paid date");
            Check(!desk.PassTime(1, out _) && clock.CurrentMinute == setup, "Desk waiting cannot spend the running date");
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "Empty date compressed roundtrip: " + message);
            Check(desk.Session.Cuts.Count == 2 && desk.Session.Cuts.All(c => c.Takes.Count == 0) && desk.Session.ElapsedMinutes == setup, "Empty sides, room and clock survive load");

            var main = GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate<UIManager>(); AddChild(main);
            var office = main.GetNode<PlayerDeskPanel>("PlayerDeskPanel"); office.OpenAtTab("ROSTER");
            Check(Walk(office).OfType<Button>().Any(b => b.Text == "RETURN TO STUDIO") && !Walk(office).OfType<Button>().Any(b => b.Text.StartsWith("CUT A TAKE", StringComparison.Ordinal)), "Roster provides a studio entry point without inline studio controls");
            Walk(office).OfType<Button>().First(b => b.Text == "RETURN TO STUDIO").EmitSignal(Button.SignalName.Pressed);
            var panel = main.GetNode<PlayerDeskPanel>("StudioWindow");
            Check(panel.Visible && !office.Visible, "Roster entry opens a separate studio window");
            string beforeMenu = Json(PendingSessionSaveData.From(desk.Session));
            main.OpenGameMenu();
            Check(!office.Visible && !panel.Visible && main.GetNode<PlayerDeskPanel>("GameMenu").Visible, "Save/load hides office and studio, including raised tabs and unread badges");
            await Screenshot("menu");
            main.GetNode<PlayerDeskPanel>("GameMenu").ClosePanel();
            Check(panel.Visible && !office.Visible && beforeMenu == Json(PendingSessionSaveData.From(desk.Session)), "Resume restores the studio without spending work or changing takes");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Walk(panel).OfType<Button>().Any(b => b.Text == $"CUT A TAKE  ·  {PlayerDesk.StudioPassMinutes(desk.Session.Cuts[0])}m") && Walk(panel).OfType<Button>().Any(b => b.Text == "FINISH & PRINT MASTERS" && b.Disabled), "Real studio UI exposes work and blocks uncut printing");
            Check(!Walk(panel).OfType<ReadBar>().Any(), "No truth ratings on studio screen");
            await Screenshot("recording");
            beforeMenu = Json(PendingSessionSaveData.From(desk.Session));
            Walk(panel).OfType<Button>().Single(b => b.Text == "? How a studio date works").EmitSignal(Button.SignalName.Pressed);
            var studioHelp = panel.GetChildren().OfType<PaperModal>().Single(m => !m.IsQueuedForDeletion());
            main._UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Check(panel.Visible && studioHelp.Visible && !main.GetNode<PlayerDeskPanel>("GameMenu").Visible, "Studio help retains Escape priority over the game menu");
            studioHelp.Cancel();
            Check(beforeMenu == Json(PendingSessionSaveData.From(desk.Session)), "Help overlays neither spend time nor change the date");
            Walk(panel).OfType<Button>().Single(b => b.Text.StartsWith("B · ")).EmitSignal(Button.SignalName.Pressed);
            Check(Walk(panel).OfType<Label>().Any(l => l.Text.StartsWith("\"Flip Side\"")) && !Walk(panel).OfType<Label>().Any(l => l.Text.StartsWith("\"First Side\"")), "Studio focuses work on the chosen song");
            Walk(panel).OfType<Button>().Single(b => b.Text.StartsWith("A · ")).EmitSignal(Button.SignalName.Pressed);
            var beforeTake = desk.CaptureState(); var takeDate = clock.CurrentDate;
            GameRandom.Seed(700); uint next = GameRandom.Randi(); GameRandom.Seed(700);
            Check(desk.CutStudioTake(0, out message), "Cut: " + message);
            int firstPass = PlayerDesk.StudioPassMinutes(desk.Session.Cuts[0]);
            Check(GameRandom.Randi() == next && clock.CurrentMinute == setup + firstPass && desk.Session.ElapsedMinutes == setup + firstPass, "Take spends the song's pass minutes and no gameplay RNG");
            var freshTake = desk.Session.Cuts[0].Takes[0];
            Check(desk.RestoreState(beforeTake, out message), "Restore fatigue comparator: " + message); clock.RestoreClock(takeDate, 9, setup);
            desk.Session.WorkloadMinutes = 240;
            Check(desk.CutStudioTake(0, out message) && desk.Session.Cuts[0].Takes[0].Vocal < freshTake.Vocal && desk.Session.Cuts[0].Takes[0].Feel < freshTake.Feel,
                "An exhausted lineup loses delivery and feel under the same private draw");
            Check(desk.RestoreState(beforeTake, out message), "Restore rehearsal comparator: " + message); clock.RestoreClock(takeDate, 9, setup);
            desk.Session.Cuts[0].Rehearsals = 2;
            Check(desk.CutStudioTake(0, out message) && desk.Session.Cuts[0].Takes[0].Ensemble > freshTake.Ensemble,
                "Preparation improves coordination under the same private draw");
            Check(desk.RestoreState(beforeTake, out message), "Restore manual continuation: " + message); clock.RestoreClock(takeDate, 9, setup);
            Check(desk.CutStudioTake(0, out message) && Json(desk.Session.Cuts[0].Takes[0]) == Json(freshTake), "Restoring the fresh state reproduces the frozen performance");

            // Song structure, located reads and targeted rehearsal.
            var shape = desk.Session.Cuts[0].Structure;
            GameRandom.Seed(705); next = GameRandom.Randi(); GameRandom.Seed(705);
            string shapeJson = Json(StudioSongStructures.For(choices[0], artist));
            Check(shape != null && shape.Valid && shapeJson == Json(shape) && GameRandom.Randi() == next, "Song structure is a stable pure derivation drawing no gameplay randomness");
            Check(shape.Inferred && shape.Sections[^1].Kind == StudioSectionKind.Ending && shape.Sections.Count(x => x.Final) == 1 &&
                Json(desk.Session.Cuts[1].Structure) != shapeJson, "Originals take a genre-default shape with one final part, distinct per song");
            Check(freshTake.Minutes == firstPass && firstPass == Math.Clamp((int)Math.Round(2 * shape.DurationSeconds / 60.0 + 6), 9, 25) &&
                freshTake.ProducerDetail != null && freshTake.TroubleSection >= -1 && freshTake.TroubleSection < shape.Sections.Count,
                "A pass costs twice the song plus a reset and records a located producer read");
            var catalog = Enum.GetValues<Genre>().SelectMany(CompositionCatalogService.GetCatalogForGenre).Distinct().ToList();
            var twelveBar = catalog.FirstOrDefault(c => c.defaultForm == SongForm.TwelveBar && c.demoTaxonomy?.vocalPresence != SongVocalPresence.Instrumental);
            var twelveShape = twelveBar == null ? null : StudioSongStructures.For(new PlayerDesk.MaterialChoice { Kind = PlayerDesk.MaterialKind.LiveCover, SongId = twelveBar.songId, Title = twelveBar.title }, artist);
            Check(twelveShape is { Form: SongForm.TwelveBar, Inferred: false } && twelveShape.Sections.Any(x => x.Kind == StudioSectionKind.Break),
                "A catalog cover keeps its saved form and archetype (twelve-bar with a solo)");
            var lengths = catalog.Select(c => StudioSongStructures.For(new PlayerDesk.MaterialChoice { Kind = PlayerDesk.MaterialKind.LiveCover, SongId = c.songId, Title = c.title }, artist))
                .GroupBy(x => x.Archetype).ToDictionary(g => g.Key, g => g.Average(x => x.DurationSeconds));
            double ballad = lengths.GetValueOrDefault(SongArchetype.SlowBallad), pop = lengths.GetValueOrDefault(SongArchetype.BrightPopNumber);
            Check(ballad > pop + 15 &&
                pop >= 130 && pop <= 160 && lengths.Values.All(v => v >= 60 && v <= 420), $"Lengths follow the archetype (ballad {ballad:F0}s, pop {pop:F0}s)");
            Check(StudioSongStructures.PassMinutes(new StudioSongStructure { DurationSeconds = 300 }) > StudioSongStructures.PassMinutes(new StudioSongStructure { DurationSeconds = 140 }),
                "A long song costs more time per pass than a short single");
            var heardNotes = new HashSet<string>();
            for (int trial = 0; trial < 6; trial++) {
                Check(desk.RestoreState(beforeTake, out message), "Restore note comparator: " + message); clock.RestoreClock(takeDate, 9, setup);
                desk.Session.ActionNumber += trial * 7;
                Check(desk.CutStudioTake(0, out message), "Note trial: " + message);
                heardNotes.Add(desk.Session.Cuts[0].Takes[0].ProducerDetail);
            }
            GD.Print($"STUDIO_SHAPE {shape.Form} {shape.Archetype} {StudioSongStructures.Length(shape.DurationSeconds)} pass={firstPass}m: {string.Join(" > ", shape.Sections.Select(x => x.Label))}");
            foreach (string heard in heardNotes) GD.Print("STUDIO_NOTE " + heard);
            Check(heardNotes.Count >= 2, "Located reads differ between passes rather than repeating one phrase");
            Check(desk.RestoreState(beforeTake, out message), "Restore part comparator: " + message); clock.RestoreClock(takeDate, 9, setup);
            var part = shape.Parts.First(k => k != StudioSectionKind.Intro);
            string noPart = Json(PendingSessionSaveData.From(desk.Session));
            GameRandom.Seed(706); next = GameRandom.Randi(); GameRandom.Seed(706);
            Check(!desk.RehearseStudioPart(0, (StudioSectionKind)99, out _) && !desk.RehearseStudioPart(5, part, out _) &&
                noPart == Json(PendingSessionSaveData.From(desk.Session)), "Unknown parts and sides cost nothing");
            int partStart = desk.Session.ElapsedMinutes, partAction = desk.Session.ActionNumber; float partWork = desk.Session.WorkloadMinutes;
            Check(desk.RehearseStudioPart(0, part, out message) && desk.Session.ElapsedMinutes == partStart + PlayerDesk.StudioPartRehearsalMinutes &&
                desk.Session.Cuts[0].PartRehearsals[(int)part] == 1 && desk.Session.WorkloadMinutes > partWork && GameRandom.Randi() == next,
                "Working one part spends ten minutes, tires the band and draws no gameplay RNG: " + message);
            desk.Session.ElapsedMinutes = partStart; desk.Session.ActionNumber = partAction; desk.Session.WorkloadMinutes = partWork; clock.RestoreClock(takeDate, 9, setup);
            Check(desk.CutStudioTake(0, out message), "Part-rehearsed comparator: " + message);
            var practisedTake = desk.Session.Cuts[0].Takes[0];
            float Lift(StudioAspect a) => StudioSongStructures.Weight(part, a) * .04f;
            Check(practisedTake.Ensemble >= freshTake.Ensemble && practisedTake.Feel >= freshTake.Feel && practisedTake.Vocal >= freshTake.Vocal &&
                (practisedTake.Ensemble > freshTake.Ensemble || practisedTake.Feel > freshTake.Feel || practisedTake.Vocal > freshTake.Vocal) &&
                practisedTake.Ensemble - freshTake.Ensemble <= Lift(StudioAspect.Ensemble) + .00001f, "Working a part lifts what it demands, within bounds, under identical draws");
            if (freshTake.TroubleSection >= 0) {
                var flaggedKind = shape.Sections[freshTake.TroubleSection].Kind;
                var drilled = new List<int>(new int[Enum.GetValues<StudioSectionKind>().Length]); drilled[(int)flaggedKind] = 20;
                var relocated = StudioSongStructures.Locate(shape, freshTake, drilled, StudioDirection.None, 0, desk.Session.ProducerCraft, freshTake.TakeId);
                Check(shape.Sections[relocated.Section].Kind != flaggedKind, "A thoroughly drilled part stops being where the pass is heard to fail");
            }
            string shapedDate = Json(PendingSessionSaveData.From(desk.Session));
            var preShape = System.Text.Json.Nodes.JsonNode.Parse(shapedDate);
            foreach (var oldCut in preShape["Cuts"].AsArray()) {
                oldCut.AsObject().Remove("Structure"); oldCut.AsObject().Remove("PartRehearsals");
                foreach (var oldTake in oldCut["Takes"].AsArray()) foreach (string field in new[] { "TroubleSection", "ProducerDetail", "EngineerDetail", "Minutes" }) oldTake.AsObject().Remove(field);
            }
            var preShapeDate = JsonSerializer.Deserialize<PendingSessionSaveData>(preShape.ToJsonString(), SaveGameService.TestJsonOptions).ToSession(Array.Empty<PlayerDesk.Song>());
            Check(Json(preShapeDate.Cuts.Select(c => c.Structure).ToArray()) == Json(desk.Session.Cuts.Select(c => c.Structure).ToArray()) &&
                preShapeDate.Cuts.All(c => c.PartRehearsals.All(n => n == 0)) && preShapeDate.Cuts[0].Takes.All(t => t.TroubleSection == -1 && t.ProducerDetail == null && t.Minutes == 0) &&
                preShapeDate.ElapsedMinutes == desk.Session.ElapsedMinutes, "Pre-structure dates derive the same shape and gain no located reads after the fact");
            var badShape = PendingSessionSaveData.From(desk.Session); badShape.Cuts[0].Takes[0].TroubleSection = 99;
            invalidShapeRejected = false;
            try { badShape.ToSession(Array.Empty<PlayerDesk.Song>()); } catch (InvalidOperationException) { invalidShapeRejected = true; }
            Check(invalidShapeRejected, "A saved read pointing outside the song's structure is rejected");
            Check(desk.RestoreState(beforeTake, out message), "Restore after structure checks: " + message); clock.RestoreClock(takeDate, 9, setup);
            Check(desk.CutStudioTake(0, out message) && Json(desk.Session.Cuts[0].Takes[0]) == Json(freshTake), "Structure checks leave the frozen first performance reproducible");
            typeof(PlayerDeskPanel).GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(panel, null);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var shownTake = desk.Session.Cuts[0].Takes[0];
            Check(Walk(panel).OfType<Button>().Any(b => b.Text == "REHEARSE…") &&
                Walk(panel).OfType<Label>().Any(l => l.Text.StartsWith("Song map (", StringComparison.Ordinal)) &&
                Walk(panel).OfType<Label>().Any(l => l.Text == "Producer: " + shownTake.ProducerNote + " " + shownTake.ProducerDetail) &&
                (shownTake.TroubleSection < 0) == !Walk(panel).OfType<Button>().Any(b => b.Text.StartsWith("REHEARSE THE ", StringComparison.Ordinal)),
                "Real console shows the song map, the located producer read, and a direct rehearsal only for a flagged part");
            await Screenshot("take-notes");
            desk.KeepTake(0, 0); string protectedTake = Json(desk.Session.Cuts[0].Takes[0]);
            Check(desk.RehearseStudio(0, out message) && desk.DirectStudio(0, StudioDirection.TightenEnding, out message) && desk.CutStudioTake(0, out message), "Prepare, direct and retry: " + message);
            Check(desk.Session.Cuts[0].KeptTake == 0 && Json(desk.Session.Cuts[0].Takes[0]) == protectedTake, "Rehearsal and retry preserve the selected earlier performance");
            int elapsed = desk.Session.ElapsedMinutes;
            Check(!desk.DirectStudio(0, StudioDirection.TightenEnding, out _) && desk.Session.ElapsedMinutes == elapsed, "Identical direction does not spend time twice");
            float workload = desk.Session.WorkloadMinutes;
            Check(desk.BreakStudio(out _) && desk.Session.WorkloadMinutes < workload && desk.Session.WorkloadMinutes > 0, "Break partially recovers cross-song workload");
            string unchanged = Json(PendingSessionSaveData.From(desk.Session));
            var olderDate = System.Text.Json.Nodes.JsonNode.Parse(unchanged);
            foreach (var oldCut in olderDate["Cuts"].AsArray()) {
                oldCut.AsObject().Remove("Arrangement"); oldCut.AsObject().Remove("ArrangementRevision");
                foreach (var oldTake in oldCut["Takes"].AsArray()) {
                    oldTake.AsObject().Remove("Arrangement"); oldTake.AsObject().Remove("ArrangementRevision");
                }
            }
            var migrated = JsonSerializer.Deserialize<PendingSessionSaveData>(olderDate.ToJsonString(), SaveGameService.TestJsonOptions).ToSession(Array.Empty<PlayerDesk.Song>());
            Check(Json(PendingSessionSaveData.From(migrated)) == unchanged, "Pre-arrangement incremental saves keep paid work, selections and frozen performances with the live default");
            var malformed = PendingSessionSaveData.From(desk.Session); malformed.Cuts[0].Arrangement = (StudioArrangement)99;
            bool invalidSaveRejected = false;
            try { malformed.ToSession(Array.Empty<PlayerDesk.Song>()); } catch (InvalidOperationException) { invalidSaveRejected = true; }
            Check(invalidSaveRejected, "Unknown saved arrangements are rejected instead of silently changing performances");
            GameRandom.Seed(701); next = GameRandom.Randi(); GameRandom.Seed(701);
            string proposal = desk.StudioArrangementProposal(0, StudioArrangement.HalfTime);
            Check(proposal.Contains("Kept take 1 stays safe") && proposal == desk.StudioArrangementProposal(0, StudioArrangement.HalfTime) &&
                unchanged == Json(PendingSessionSaveData.From(desk.Session)) && GameRandom.Randi() == next, "Repeated proposals are stable and change neither date nor gameplay RNG");
            var currentArtist = ArtistManager.Instance.GetArtist(artist.artistId);
            currentArtist.artistCreativeControl = true; float tension = desk.Session.Tension; desk.Session.Tension = .8f;
            string refused = Json(PendingSessionSaveData.From(desk.Session));
            Check(!desk.ArrangeStudio(0, StudioArrangement.HalfTime, out _) && refused == Json(PendingSessionSaveData.From(desk.Session)), "Creative-control refusal costs nothing and protects performances");
            currentArtist.artistCreativeControl = false; desk.Session.Tension = tension;
            int bookedStart = desk.Session.StartMinute;
            desk.Session.StartMinute = clock.CurrentHour * 60 + clock.CurrentMinute - desk.Session.Hours * 60 + 30;
            unchanged = Json(PendingSessionSaveData.From(desk.Session));
            Check(!desk.ArrangeStudio(0, StudioArrangement.HalfTime, out _) && unchanged == Json(PendingSessionSaveData.From(desk.Session)),
                "Preparation cannot consume the time needed to hear the alternative and finish");
            desk.Session.StartMinute = bookedStart;
            int beforeArrangement = desk.Session.ElapsedMinutes; float paidArrangementCash = desk.Label.cashReserves;
            Check(desk.ArrangeStudio(0, StudioArrangement.SimplerRhythm, out message) && desk.Session.ElapsedMinutes == beforeArrangement + 20 &&
                desk.Label.cashReserves == paidArrangementCash && desk.Session.Cuts[0].Rehearsals == 0, "Alternative spends preparation and resets arrangement-specific rehearsal, with no extra fee");
            unchanged = Json(PendingSessionSaveData.From(desk.Session));
            Check(!desk.ArrangeStudio(0, StudioArrangement.SimplerRhythm, out _) && !desk.ArrangeStudio(0, (StudioArrangement)99, out _) &&
                unchanged == Json(PendingSessionSaveData.From(desk.Session)), "Repeated and invalid arrangements do not consume work");
            Check(desk.CutStudioTake(0, out message) && desk.Session.Cuts[0].Takes.Last().Arrangement == StudioArrangement.SimplerRhythm &&
                desk.Session.Cuts[0].Takes.Last().ArrangementRevision == 1 && Json(desk.Session.Cuts[0].Takes[0]) == protectedTake && desk.Session.Cuts[0].KeptTake == 0,
                "Trying the new part records a separate revision and leaves the kept live take untouched");
            Check(desk.ArrangeStudio(1, StudioArrangement.HalfTime, out message), "Prepare half-time flip: " + message);
            Check(SaveGameService.Save(slot, out message), "Save continuation: " + message);
            // Compare identical actions under identical private draws, varying only the prepared part.
            desk.Session.Cuts[1].Arrangement = StudioArrangement.Live;
            Check(desk.CutStudioTake(1, out message), "Live arrangement comparator: " + message);
            float liveEnsemble = desk.Session.Cuts[1].Takes[0].Ensemble;
            Check(SaveGameService.Load(slot, out message), "Restore prepared alternative: " + message);
            Check(desk.CutStudioTake(1, out message), "Next-side performance: " + message);
            Check(Math.Abs(desk.Session.Cuts[1].Takes[0].Ensemble - (liveEnsemble - .025f)) < .00001f, "The changed arrangement affects the actual captured performance under identical draws");
            string continuation = Json(PendingSessionSaveData.From(desk.Session));
            Check(SaveGameService.Load(slot, out message), "Reload continuation: " + message);
            GameRandom.Seed(99999); _ = GameRandom.Randi();
            Check(desk.CutStudioTake(1, out message) && Json(PendingSessionSaveData.From(desk.Session)) == continuation, "Saved next action yields identical take and evidence despite unrelated RNG");
            Check(desk.Session.WorkloadMinutes > 0 && desk.Session.Cuts[1].Takes[0].TakeId != desk.Session.Cuts[0].Takes[0].TakeId, "Workload crosses songs and take identities stay distinct");
            Check(desk.Session.Cuts[0].Arrangement == StudioArrangement.SimplerRhythm && desk.Session.Cuts[1].Takes[0].Arrangement == StudioArrangement.HalfTime,
                "Prepared and recorded arrangements survive compressed save/load and continuation");
            Check(desk.RunStudioDate(out message) && desk.Session.Cuts[0].KeptTake == 0, "Bounded delegation keeps manual selection: " + message);
            Check(desk.Session.Cuts.All(c => c.KeptTake >= 0), "Delegation provides both sides");
            await Screenshot("finishing");
            Check(desk.Session.ElapsedMinutes <= desk.Session.Hours * 60 - 2 * PlayerDesk.StudioFinishMinutesPerSide, "Delegation stays inside the quote and protects finishing");
            string sourceBeforeFinish = Json(desk.Session.Cuts[0].Takes[0]);
            string finishSourceId = desk.Session.Cuts[0].Takes[0].TakeId;
            unchanged = Json(PendingSessionSaveData.From(desk.Session));
            GameRandom.Seed(702); next = GameRandom.Randi(); GameRandom.Seed(702);
            string finishProposal = desk.StudioFinishProposal(0, StudioFinish.SoftenTop);
            Check(finishProposal.Contains("every instrument together") && finishProposal == desk.StudioFinishProposal(0, StudioFinish.SoftenTop) &&
                unchanged == Json(PendingSessionSaveData.From(desk.Session)) && GameRandom.Randi() == next, "Finish proposals are free, stable and describe mono source limitations");
            Check(PlayerDesk.StudioFinishes(desk.Session.Room).Contains(StudioFinish.SoftenTop) &&
                !PlayerDesk.StudioFinishes(desk.Session.Room).Contains(StudioFinish.GentleCompression) &&
                !desk.FinishStudioSide(0, StudioFinish.GentleCompression, out _) && !desk.FinishStudioSide(0, (StudioFinish)99, out _) &&
                unchanged == Json(PendingSessionSaveData.From(desk.Session)), "Only the booked room's supported whole-tape operations can be resolved");
            int beforeFinish = desk.Session.ElapsedMinutes; float cashBeforeFinish = desk.Label.cashReserves;
            workload = desk.Session.WorkloadMinutes;
            GameRandom.Seed(703); next = GameRandom.Randi(); GameRandom.Seed(703);
            Check(desk.FinishStudioSide(0, StudioFinish.SoftenTop, out message) && desk.Session.ElapsedMinutes == beforeFinish + 8 &&
                desk.Label.cashReserves == cashBeforeFinish && desk.Session.WorkloadMinutes == workload && GameRandom.Randi() == next,
                "Whole-tape processing spends eight quoted minutes once, without another wage, band pass or gameplay RNG draw");
            string processed = Json(PlayerDesk.SelectedStudioFinish(desk.Session.Cuts[0]));
            unchanged = Json(PendingSessionSaveData.From(desk.Session));
            Check(!desk.FinishStudioSide(0, StudioFinish.SoftenTop, out _) && unchanged == Json(PendingSessionSaveData.From(desk.Session)) &&
                Json(desk.Session.Cuts[0].Takes[0]) == sourceBeforeFinish, "Repeated finish does no work and the source performance and listening notes stay frozen");
            desk.KeepTake(0, 1);
            Check(desk.Session.Cuts[0].Finish == StudioFinish.House && PlayerDesk.SelectedStudioFinish(desk.Session.Cuts[0]) == null && desk.Session.Cuts[0].FinishWork.Count == 1,
                "Changing the selected source restores the included house finish while retaining the processed alternative");
            desk.KeepTake(0, 0);
            beforeFinish = desk.Session.ElapsedMinutes;
            Check(desk.FinishStudioSide(0, StudioFinish.SoftenTop, out _) && desk.Session.ElapsedMinutes == beforeFinish &&
                Json(PlayerDesk.SelectedStudioFinish(desk.Session.Cuts[0])) == processed, "Returning to a retained processed source needs no repeat work or new observation");
            typeof(PlayerDeskPanel).GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(panel, null);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Walk(panel).OfType<Button>().Any(b => b.Text == PlayerDesk.FinishName(StudioFinish.SoftenTop)) &&
                !Walk(panel).OfType<Button>().Any(b => b.Text == PlayerDesk.FinishName(StudioFinish.GentleCompression)) && !Walk(panel).OfType<ReadBar>().Any(),
                "Real console exposes supported finishes and no source truth bars");
            malformed = PendingSessionSaveData.From(desk.Session); malformed.Cuts[0].FinishWork[0].TakeId = "missing-source";
            invalidSaveRejected = false;
            try { malformed.ToSession(Array.Empty<PlayerDesk.Song>()); } catch (InvalidOperationException) { invalidSaveRejected = true; }
            Check(invalidSaveRejected, "A processed save cannot point at a missing source performance");
            var preFinishSave = System.Text.Json.Nodes.JsonNode.Parse(Json(PendingSessionSaveData.From(desk.Session)));
            foreach (var oldCut in preFinishSave["Cuts"].AsArray()) { oldCut.AsObject().Remove("Finish"); oldCut.AsObject().Remove("FinishWork"); }
            preFinishSave["Flow"]["Room"].AsObject().Remove("SupportsToneFilter"); preFinishSave["Flow"]["Room"].AsObject().Remove("SupportsCompression");
            var olderFinishDate = JsonSerializer.Deserialize<PendingSessionSaveData>(preFinishSave.ToJsonString(), SaveGameService.TestJsonOptions).ToSession(Array.Empty<PlayerDesk.Song>());
            Check(olderFinishDate.Cuts.All(c => c.Finish == StudioFinish.House && c.FinishWork.Count == 0) &&
                olderFinishDate.ElapsedMinutes == desk.Session.ElapsedMinutes && olderFinishDate.Cost == desk.Session.Cost &&
                Json(olderFinishDate.Cuts.Select(c => c.Takes).ToArray()) == Json(desk.Session.Cuts.Select(c => c.Takes).ToArray()) &&
                PlayerDesk.StudioFinishes(olderFinishDate.Room).SequenceEqual(new[] { StudioFinish.House }),
                "Older dates default to their included house finish without inventing equipment or changing paid takes");
            bookedStart = desk.Session.StartMinute;
            desk.Session.StartMinute = clock.CurrentHour * 60 + clock.CurrentMinute - desk.Session.Hours * 60 + 20;
            unchanged = Json(PendingSessionSaveData.From(desk.Session));
            Check(!desk.FinishStudioSide(1, StudioFinish.SoftenTop, out _) && unchanged == Json(PendingSessionSaveData.From(desk.Session)),
                "Optional processing cannot consume the final mastering reservation");
            Check(desk.FinishStudioSide(0, StudioFinish.House, out _) && desk.Session.ElapsedMinutes == beforeFinish,
                "The included source finish stays available when no optional work fits");
            Check(desk.FinishStudioSide(0, StudioFinish.SoftenTop, out _) && desk.Session.ElapsedMinutes == beforeFinish,
                "Already retained processing can be selected without using the final reservation");
            desk.Session.StartMinute = bookedStart;
            float paidCash = desk.Label.cashReserves; int finishStart = clock.CurrentHour * 60 + clock.CurrentMinute;
            int expectedWorked = desk.Session.ElapsedMinutes + 2 * PlayerDesk.StudioFinishMinutesPerSide;
            string roomName = desk.Session.Room.Name;
            Check(SaveGameService.Save(slot, out message), "Save selected performance comparator: " + message);
            GameRandom.Seed(888);
            Check(desk.PrintSession(out message), "House finish: " + message);
            float halfTimeDance = desk.Masters[1].Record.danceability;
            float processedHook = desk.Masters[0].Record.hookStrength, processedProduction = desk.Masters[0].Record.productionQuality;
            Check(desk.Masters[0].StudioHistory.FinishWork?.TakeId == finishSourceId && Json(desk.Masters[0].StudioHistory.FinishWork) == processed,
                "The master retains the actual processing source identity");
            Check(SaveGameService.Load(slot, out message), "Restore print comparator: " + message);
            desk.Session.Cuts[1].Takes[desk.Session.Cuts[1].KeptTake].Arrangement = StudioArrangement.Live;
            desk.Session.Cuts[0].Finish = StudioFinish.House;
            GameRandom.Seed(888);
            Check(desk.PrintSession(out message) && Math.Abs(halfTimeDance - desk.Masters[1].Record.danceability * .70f) < .00001f,
                "Half-time changes the finished recording's movement after shared material realization");
            Check(Math.Abs(processedHook - Math.Clamp(desk.Masters[0].Record.hookStrength - .012f, 0, 1)) < .00001f &&
                Math.Abs(processedProduction - Math.Clamp(desk.Masters[0].Record.productionQuality + .015f, 0, 1)) < .00001f,
                "Retained processing changes the printed master once after shared realization, with its dynamics tradeoff");
            Check(SaveGameService.Load(slot, out message), "Restore final kept alternatives: " + message);
            GameRandom.Seed(888);
            Check(desk.PrintSession(out message), "Finish final alternatives: " + message);
            Check(clock.CurrentHour * 60 + clock.CurrentMinute == finishStart + 20 && desk.Label.cashReserves == paidCash, "Finishing spends quoted minutes and no second room charge");
            Check(desk.Masters.Count == 2 && desk.Masters.All(m => m.StudioHistory?.RoomName == roomName && m.StudioHistory.ElapsedMinutes == expectedWorked), "Both masters retain the date's story");
            Check(desk.Masters.All(m => m.StudioHistory.DurationSeconds >= 60 && m.StudioHistory.Form != SongForm.Unknown &&
                (PolarSongMetadataService.Get(m.Record.masterId)?.taxonomy == null || PolarSongMetadataService.Get(m.Record.masterId).taxonomy.durationSeconds == m.StudioHistory.DurationSeconds)),
                "Masters keep the performed length and form, and the registered master stores its length");
            Check(desk.Masters[0].StudioHistory.Arrangement == StudioArrangement.Live && desk.Masters[1].StudioHistory.Arrangement == StudioArrangement.HalfTime &&
                desk.Masters[1].StudioHistory.ArrangementRevision == 1, "Masters record the selected arrangement, including an earlier live performance after an experiment");
            string histories = Json(desk.Masters.Select(m => m.StudioHistory).ToArray());
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && histories == Json(desk.Masters.Select(m => m.StudioHistory).ToArray()), "Master histories survive compressed save/load");
            Check(!desk.PrintSession(out _) && desk.Masters.Count == 2, "Finished date cannot print or charge again");
            Check(desk.StartSession(ArtistManager.Instance.GetArtist(artist.artistId), choices, PlayerDesk.StudioTier.Mid, 3, out message, overrideRefusal: true), "Next controlled-room date: " + message);
            Check(desk.Session.Room.SupportsCompression && desk.CutStudioTake(0, out message), "Controlled room can capture a source for compression: " + message);
            desk.KeepTake(0, 0); string compressionSource = Json(desk.Session.Cuts[0].Takes[0]);
            Check(desk.FinishStudioSide(0, StudioFinish.GentleCompression, out message) &&
                PlayerDesk.SelectedStudioFinish(desk.Session.Cuts[0])?.Finish == StudioFinish.GentleCompression &&
                Json(desk.Session.Cuts[0].Takes[0]) == compressionSource, "Supported compression retains the original tape and a distinct whole-tape treatment");
            processed = Json(PendingSessionSaveData.From(desk.Session));
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && processed == Json(PendingSessionSaveData.From(desk.Session)),
                "Compression choice, saved equipment, results and source binding survive compressed roundtrip");
            paidCash = desk.Label.cashReserves; var people = desk.Session.ParticipantIds.ToArray(); var date = desk.Session.Date; int start = desk.Session.StartMinute;
            desk.ScrapSession();
            Check(desk.Label.cashReserves == paidCash && LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, people, date, start, start + 180), "Abandoning releases reservations without refunding incurred cost");
            var planningSave = desk.CaptureState();
            foreach (string title in new[] { "Ready For The Room", "After Midnight" }) planningSave.Songs.Add(SongSaveData.From(new PlayerDesk.Song {
                SongId = "studio-ui:" + title, Title = title, ArtistId = artist.artistId, Hook = .5f, Originality = .5f, Danceability = .5f, Written = clock.CurrentDate
            }));
            Check(desk.RestoreState(planningSave, out message), "Prepared material for the booking interface: " + message);
            main.OpenStudio(artist.artistId);
            Check(Walk(panel).OfType<Button>().Any(b => b.Text == "BOOK THE ROOM" && b.Disabled) && !Walk(office).OfType<Button>().Any(b => b.Text == "BOOK THE ROOM"), "Booking lives only in the studio and requires explicit song selection");
            await Screenshot("planning");
            var planBefore = desk.CaptureState(); planBefore.PolarObservations = null;
            string planState = Json(planBefore);
            var materialBox = Walk(panel).OfType<CheckBox>().First(); materialBox.ButtonPressed = true;
            GameRandom.Seed(704); next = GameRandom.Randi(); GameRandom.Seed(704);
            Walk(panel).OfType<Button>().Single(b => b.Text == "COMPARE MATERIAL…").EmitSignal(Button.SignalName.Pressed);
            var planAfter = desk.CaptureState(); planAfter.PolarObservations = null;
            Check(Walk(panel).OfType<CheckBox>().Any(b => b.ButtonPressed) && Json(planAfter) == planState && GameRandom.Randi() == next, "Optional comparison preserves the plan, cash, clock and gameplay RNG while retaining its listening evidence");
            string compared = Json(desk.CaptureState());
            Walk(panel).OfType<Button>().Single(b => b.Text == "HIDE MATERIAL COMPARISON").EmitSignal(Button.SignalName.Pressed);
            Walk(panel).OfType<Button>().Single(b => b.Text == "COMPARE MATERIAL…").EmitSignal(Button.SignalName.Pressed);
            Check(compared == Json(desk.CaptureState()), "Repeated material comparison cannot reroll its cached evidence");
            panel.ClosePanel();
            Check(office.Visible && !panel.Visible && main.isUIOpen, "Closing the studio returns to the prior roster view");
            main.OpenGameMenu();
            Check(!office.Visible && !Walk(office).OfType<TabStamp>().Any(s => s.IsVisibleInTree()) && !panel.Visible,
                "Office tabs and unread badges are hidden immediately when save/load opens");
            await Screenshot("office-menu");
            main.GetNode<PlayerDeskPanel>("GameMenu").ClosePanel();
            Check(office.Visible && !panel.Visible && main.isUIOpen, "Resume restores the prior office rather than reopening the studio");
            GD.Print($"STUDIO_DATE_CHECK_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_DATE_CHECK_FAIL " + ex); GetTree().Quit(1); }
        finally { SaveGameService.Delete(slot); }
    }
}
