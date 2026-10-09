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
            string dir = ProjectSettings.GlobalizePath("res://SimLogs/scene3-ui-v1"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ui.txt"), string.Join("\n", Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion()).Select(l => l.Text)));
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var image = GetViewport().GetTexture().GetImage();
            if (image.SavePng(Path.Combine(dir, "board.png")) != Error.Ok) throw new InvalidOperationException("Screenshot failed.");
            GD.Print("SCENE_ROOM_UI_PASS rooms=6 selection=true layout=true"); GetTree().Quit(0);
        } catch (Exception ex) { GD.PrintErr("SCENE_ROOM_UI_FAILED: " + ex); GetTree().Quit(1); }
    }
}
