using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;

/// <summary>Compare startup world, initial appointment order and next RNG draw across cache modes.</summary>
public partial class StartupSnapshotRunner : Node {
    public override void _Ready() {
        try {
            string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions))));
            var args = OS.GetCmdlineUserArgs();
            string tag = args.FirstOrDefault(a => a.StartsWith("--snapshot-tag="))?[15..] ?? "startup";
            if (tag.Any(c => !char.IsLetterOrDigit(c) && c != '-')) throw new ArgumentException("Invalid snapshot tag.");
            var result = new { world = Hash(WorldStateService.Capture()), appointments = Hash(RosterManager.Instance.SceneInitializationAppointments),
                nextRandom = GameRandom.Randi(), artists = ArtistManager.Instance.GetAllArtists().Count, rooms = LocalSceneRoomService.Calendar("nashville", TimeManager.Instance.CurrentDate).Count };
            File.WriteAllText(ProjectSettings.GlobalizePath($"res://SimLogs/{tag}.json"), JsonSerializer.Serialize(result));
            GD.Print("STARTUP_SNAPSHOT " + JsonSerializer.Serialize(result));
            GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr(ex); GetTree().Quit(1); }
    }
}
