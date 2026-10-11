using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;

public static class ScoutingMarketHistoryChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_MARKET_HISTORY_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        // Compare every saved world field except the new read-only history; normalization never mutates the world.
        string Digest(string json) {
            using var doc = JsonDocument.Parse(json); using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) {
                void Write(JsonElement element) {
                    if (element.ValueKind == JsonValueKind.Object) {
                        writer.WriteStartObject();
                        foreach (var property in element.EnumerateObject()) if (property.Name != "sceneMarketHistory") { writer.WritePropertyName(property.Name); Write(property.Value); }
                        writer.WriteEndObject();
                    } else if (element.ValueKind == JsonValueKind.Array) { writer.WriteStartArray(); foreach (var child in element.EnumerateArray()) Write(child); writer.WriteEndArray(); }
                    else element.WriteTo(writer);
                }
                Write(doc.RootElement);
            }
            return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
        }
        var clock = TimeManager.Instance; var desk = PlayerDesk.Instance;
        Check(desk.FoundLabel("Market History Checks", "nashville", out _), "found label");
        // Isolate daily clearing from the Friday weekly economy and its independent runtime caches.
        clock.RestoreClock(GameDate.StartDate.AddDays(1), 10);
        ChartManager.Instance.RestoreChartWeek(1); // Enable the established live daily market in this controlled fixture.
        var supply = ArtistManager.Instance.GetUnsignedArtists().First();
        foreach (var label in ChartManager.Instance.GetAllLabels().Where(l => !l.isPlayerOwned && l.IsActive).Take(4)) {
            label.maxRosterSize = Math.Max(label.maxRosterSize, label.CurrentRosterSize + 3);
            label.SetOperatingRosterTarget(label.CurrentRosterSize + 3, LabelOperatingTargetReason.OrganicGrowth, 0);
            label.cashReserves = 100000; label.preferredGenres = Enum.GetValues<Genre>(); label.scoutingAbility = 1;
            label.geography.basePlaceId = supply.geography.basePlaceId;
            label.SetNextScoutingDate(clock.CurrentDate.AddDays(1));
        }
        var date = clock.CurrentDate; ulong seed = LocalSceneIdentityService.KeyedSeed;
        string baseline = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
        void Restore(bool enabled) {
            LocalScenes.Configure(enabled ? Array.Empty<string>() : new[] { "--disable-scouting-market-history" });
            WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(baseline, SaveGameService.TestJsonOptions), date, seed, deferSceneRelink: true);
            Check(desk.RestoreState(JsonSerializer.Deserialize<PlayerSaveData>(player, SaveGameService.TestJsonOptions), out _), "matched player restored");
            LocalSceneIdentityService.CompleteRestore(); GD.Seed(77123);
        }
        Restore(false); clock.EndDay();
        string controlJson = Json(WorldStateService.Capture()); string control = Digest(controlJson); string controlPlayer = Json(desk.CaptureState()); uint controlRng = GD.Randi();
        Check(ArtistManager.Instance.GetAllArtists().All(a => a.sceneMarketHistory == null || a.sceneMarketHistory.Count == 0), "off control records no future history");
        Restore(true); clock.EndDay();
        var events = ArtistManager.Instance.GetAllArtists().SelectMany(a => a.sceneMarketHistory ?? new()).ToArray();
        Check(events.Length > 0 && events.Any(h => h.Kind == SceneMarketDecisionKind.Signed), "real daily-market commit invokes the new owner instrumentation");
        Check(events.All(h => h.Day == LocalSceneRoomService.Day(date.AddDays(1))), "real daily events have exact commit date");
        string treatmentJson = Json(WorldStateService.Capture());
        if (Digest(treatmentJson) != control) { File.WriteAllText(Godot.ProjectSettings.GlobalizePath("res://SimLogs/business-v1-market-control.json"), controlJson); File.WriteAllText(Godot.ProjectSettings.GlobalizePath("res://SimLogs/business-v1-market-treatment.json"), treatmentJson); }
        Check(Digest(treatmentJson) == control, "all pre-existing world fields identical in matched on/off clearing");
        Check(Json(desk.CaptureState()) == controlPlayer && GD.Randi() == controlRng, "player state and RNG continuation identical under recording control");
        Check(desk.ScoutingDossiers.Count == 0, "actual market recording never grants player knowledge");
        LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCOUTING_MARKET_HISTORY_CHECK_PASS checks={checks} events={events.Length} normalizedWorld={control}");
    }
}
