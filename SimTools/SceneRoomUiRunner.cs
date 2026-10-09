using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

/// <summary>Render the real A&R board and exercise room selection; never a normal game startup path.</summary>
public partial class SceneRoomUiRunner : Node {
    private static IEnumerable<Node> Descendants(Node node) {
        foreach (Node child in node.GetChildren()) { yield return child; foreach (var other in Descendants(child)) yield return other; }
    }
    public override async void _Ready() {
        try {
            AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
            var desk = PlayerDesk.Instance;
            if (!desk.FoundLabel("Room UI Records", "nashville", out string message)) throw new InvalidOperationException(message);
            TimeManager.Instance.RestoreClock(GameDate.StartDate, 17);
            var panel = Descendants(this).OfType<PlayerDeskPanel>().Single();
            panel.OpenAtTab("A&R");
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var fold = Descendants(this).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && b.Text == "FOLD THE PAPER  ×");
            fold?.EmitSignal(Button.SignalName.Pressed);
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var cards = Descendants(panel).OfType<VenueHandbill>().Where(c => !c.IsQueuedForDeletion()).ToArray();
            if (cards.Length != 6) throw new InvalidOperationException("The board must show six named rooms.");
            foreach (var card in cards) foreach (var label in Descendants(card).OfType<Label>()) {
                if (label.GlobalPosition.Y + label.Size.Y > card.GlobalPosition.Y + card.Size.Y + 1)
                    throw new InvalidOperationException("Room handbill text overflows its paper.");
            }
            cards[1].EmitSignal(Button.SignalName.Pressed);
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            cards = Descendants(panel).OfType<VenueHandbill>().Where(c => !c.IsQueuedForDeletion()).ToArray();
            cards[0].EmitSignal(Button.SignalName.Pressed);
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            bool information = OS.GetCmdlineUserArgs().Contains("--scene-information-ui-check");
            bool sources = OS.GetCmdlineUserArgs().Contains("--scene-source-ui-check");
            if (sources) {
                var office = Descendants(panel).OfType<OptionButton>().Single(b => !b.IsQueuedForDeletion() && b.ItemCount == 9);
                if (office.GetItemText(0) != "London (UK)") throw new InvalidOperationException("Source office identity is missing.");
                var introduce = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("ARRANGE INTRODUCTION"));
                float before = desk.Label.cashReserves;
                introduce.EmitSignal(Button.SignalName.Pressed);
                for (int n=0;n<4;n++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if (desk.Label.cashReserves != before-150 || SceneSourceService.Connection(desk.Label,"gb_london",0)==null ||
                    !Descendants(panel).OfType<Button>().Any(b=>!b.IsQueuedForDeletion() && b.Text.StartsWith("REVIEW DEMOS")))
                    throw new InvalidOperationException("Source introduction button failed to record cost and access.");
            }
            if (information) {
                var tip = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("ASK ") && b.Text.Contains("LISTENING TIP"));
                if (!tip.Disabled) throw new InvalidOperationException("A new player must earn booker familiarity.");
                if (!Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("LOCAL SCENE"))) throw new InvalidOperationException("Local recap is missing.");
            }
            bool ecosystem = OS.GetCmdlineUserArgs().Contains("--scene-ecosystem-ui-check");
            if (ecosystem) {
                float before = desk.Label.cashReserves;
                var fund = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("FUND 13-WEEK"));
                fund.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (SceneEcosystemService.Programs.Count != 1 || desk.Label.cashReserves != before - SceneEcosystemService.ProgramCost)
                    throw new InvalidOperationException("Funding button must create one program and book its real expense.");
            }
            string dir = ProjectSettings.GlobalizePath(sources ? "res://SimLogs/scene7-source-ui-v1" : information ? "res://SimLogs/scene6-information-ui-v1" : ecosystem ? "res://SimLogs/scene5-ecosystem-ui-v1" : "res://SimLogs/scene3-ui-v1"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ui.txt"), string.Join("\n", Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion()).Select(l => l.Text)));
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var image = GetViewport().GetTexture().GetImage();
            if (image.SavePng(Path.Combine(dir, "board.png")) != Error.Ok) throw new InvalidOperationException("Screenshot failed.");
            GD.Print(sources ? "SCENE_SOURCE_UI_PASS rooms=6 sourceIntro=true" : information ? "SCENE_INFORMATION_UI_PASS rooms=6 recap=true earnedTip=true" : ecosystem ? "SCENE_ECOSYSTEM_UI_PASS rooms=6 funding=true layout=true" : "SCENE_ROOM_UI_PASS rooms=6 selection=true layout=true"); GetTree().Quit(0);
        } catch (Exception ex) { GD.PrintErr("SCENE_ROOM_UI_FAILED: " + ex); GetTree().Quit(1); }
    }
}
