using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Real autoload + UI checks for the first studio and Esc-menu slice.</summary>
public partial class StudioFoundationRunner : Node {
    public override async void _Ready() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        IEnumerable<Node> Walk(Node node) { yield return node; foreach (var child in node.GetChildren()) foreach (var n in Walk(child)) yield return n; }
        try {
            var main = GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate<UIManager>();
            AddChild(main);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var office = main.GetNode<PlayerDeskPanel>("PlayerDeskPanel");
            Check(!Walk(office).OfType<Button>().Any(b => b.Text == "SAVE / LOAD"), "Save/load no longer appears in office chrome");
            main._UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            var menu = main.GetNode<PlayerDeskPanel>("GameMenu");
            Check(menu.Visible && main.isUIOpen, "Esc opens game menu from founding");
            Check(Walk(menu).OfType<Button>().Any(b => b.Text == "EXIT GAME"), "Exit remains available before a label or save exists");
            var modal = PaperModal.Open(menu, "TEST CONFIRMATION");
            main._UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Check(menu.Visible && PaperModal.IsOpen, "Menu respects nested confirmation");
            modal._UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            main._UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Check(!menu.Visible && office.Visible && main.isUIOpen, "Resume restores the previous office view");
            main.OnClick_CloseAll();
            main.OpenGameMenu(); menu.ClosePanel();
            Check(!main.isUIOpen && !office.Visible, "Resume from desktop restores desktop input state");

            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            Check(desk.FoundLabel("Studio Foundation", "nashville", out string message), "Found label: " + message);
            var artist = ArtistManager.Instance.GetAllArtists().First(a => a.members.Count > 0);
            artist.labelId = desk.Label.labelId; artist.artistCreativeControl = false;
			desk.Label.roster.Add(artist);
            clock.RestoreClock(clock.CurrentDate, 9);
            var choices = new List<PlayerDesk.MaterialChoice> {
                new() { Kind = PlayerDesk.MaterialKind.Original, Title = "First Side", Detail = "their own" },
                new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Flip Side", Detail = "their own" }
            };
            Check(desk.StartSession(artist, choices, PlayerDesk.StudioTier.Budget, 4, out message, overrideRefusal: true), "Book: " + message);
            var session = desk.Session;
            Check(session.Cuts.All(c => c.KeptTake == -1 && c.Takes.Count == 2), "Manual date has paid takes and no selected default");
            office.OpenAtTab("ROSTER");
            main.OpenStudio(artist.artistId);
            var studio = main.GetNode<PlayerDeskPanel>("StudioWindow");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Walk(studio).OfType<Button>().Any(b => b.Text == "PRINT MASTERS" && b.Disabled), "Actual studio UI blocks printing until every side is selected");
            Check(!Walk(studio).OfType<ReadBar>().Any() && Walk(studio).OfType<Label>().Any(l => l.Text.StartsWith("Engineer: ")),
                "Actual studio UI displays attributed notes with no raw take bars");
            float cash = desk.Label.cashReserves; int hour = clock.CurrentHour; int masters = desk.Masters.Count;
            Check(!desk.PrintSession(out _) && desk.Session == session && desk.Masters.Count == masters, "Unselected print is refused without losing the date");
            desk.KeepTake(0, -1); desk.KeepTake(0, 99);
            Check(session.Cuts[0].KeptTake == -1, "Invalid selection does not choose a take");
            string evidence = Json(PendingSessionSaveData.From(session));
            GameRandom.Seed(314); uint expected = GameRandom.Randi(); GameRandom.Seed(314);
            StudioTakeEvidence.Observe(session, session.Cuts[0], 0, .9f, .9f);
            Check(GameRandom.Randi() == expected && Json(PendingSessionSaveData.From(session)) == evidence, "Observation is frozen and does not consume gameplay RNG");
            var restored = JsonSerializer.Deserialize<PendingSessionSaveData>(evidence, SaveGameService.TestJsonOptions).ToSession(desk.Songs);
            Check(restored.Cuts.All(c => c.KeptTake == -1) && Json(PendingSessionSaveData.From(restored)) == evidence, "Unselected date and all evidence round-trip");
            var legacy = JsonSerializer.Deserialize<PendingSessionSaveData>(evidence, SaveGameService.TestJsonOptions);
            legacy.ManualSelection = false; legacy.SessionId = null;
            legacy.Cuts[0].KeptTake = 1; legacy.Cuts[1].KeptTake = 0;
            foreach (var take in legacy.Cuts.SelectMany(c => c.Takes)) take.ProducerNote = take.EngineerNote = take.PerformerNote = null;
            var migrated = legacy.ToSession(desk.Songs);
            Check(migrated.Cuts[0].KeptTake == 1 && migrated.Cuts[1].KeptTake == 0 && migrated.Cost == session.Cost &&
                migrated.Cuts[0].Takes[1].Hook == session.Cuts[0].Takes[1].Hook, "Legacy paid takes, cost and selections survive without reroll");
            int pick = StudioTakeEvidence.Choose(session.Cuts[0].Takes);
            var probe = session.Cuts[0].Takes.Select(t => new PlayerDesk.SessionTake { Hook = 1 - t.Hook, Production = 1 - t.Production,
                ProducerNote = t.ProducerNote, EngineerNote = t.EngineerNote }).ToArray();
            Check(StudioTakeEvidence.Choose(probe) == pick, "Delegation uses evidence rather than hidden truth");
            string slot = "studio-check-" + Guid.NewGuid().ToString("N");
            try {
                Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "Compressed save/load: " + message);
                Check(Json(PendingSessionSaveData.From(desk.Session)) == evidence, "Real load preserves unselected performances and frozen notes");
            } finally { SaveGameService.Delete(slot); }
            desk.KeepTake(0, 0); desk.DelegateTakeSelection();
            Check(desk.Session.Cuts[0].KeptTake == 0 && desk.Session.Cuts[1].KeptTake >= 0, "Delegation preserves the player's earlier choice");
            Check(desk.PrintSession(out message) && desk.Masters.Count == masters + 2, "Two-side masters finish: " + message);
            Check(desk.Label.cashReserves == cash && clock.CurrentHour == hour, "Selection and printing do not charge time or cash again");
            Check(!desk.PrintSession(out _) && desk.Masters.Count == masters + 2, "Printing is idempotent");
            GD.Print($"STUDIO_FOUNDATION_CHECK_PASS checks={checks}");
            GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_FOUNDATION_CHECK_FAIL " + ex); GetTree().Quit(1); }
    }
}
