using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>The world-start room pre-roll: history in the room ledger only. Run on a world created WITHOUT the prewarm
/// (the control), so the live calendar it produced can be compared with one opened after a pre-roll in this process.</summary>
public static class SceneRoomPrewarmChecks {
    public static void Run() {
        if (!LocalScenes.Rooms) throw new InvalidOperationException("Scene rooms must be enabled for these checks.");
        if (LocalScenes.RoomPrewarm) throw new InvalidOperationException("Run on a control world (with --disable-scene-room-prewarm).");
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCENE_ROOM_PREWARM_CHECK_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        string[] args = OS.GetCmdlineUserArgs();
        // The prewarm is on by default, so the control world is created with the explicit off flag.
        string[] defaults = args.Where(a => a != "--disable-scene-room-prewarm").ToArray();
        var date = TimeManager.Instance.CurrentDate;
        int today = LocalSceneRoomService.Day(date);
        var am = ArtistManager.Instance;
        SceneRoomSaveData Snapshot() { var w = new WorldSaveData(); LocalSceneRoomService.CaptureWorld(w); return w.SceneRooms; }
        void RestoreRooms(SceneRoomSaveData saved) { LocalSceneRoomService.BeginRestore(saved); LocalSceneRoomService.CompleteRestore(); }
        // What AI recruitment and the rest of the economy read about acts: none of it may move.
        string ActFacts() => Json(am.GetAllArtists().OrderBy(a => a.artistId, StringComparer.Ordinal).Select(a => new {
            a.artistId, a.reputation, a.momentum, a.careerState, a.labelId, a.prospectMarketStatus, a.isActive,
            Members = a.members.Select(m => new { m.personId, m.fatigue, m.technicalSkill, m.lifeState }) }));
        if (!PlayerDesk.Instance.FoundLabel("Prewarm Check", "nashville", out string founded)) throw new InvalidOperationException(founded);
        var control = Snapshot();
        string facts = ActFacts();
        try {
            Check(today == 0 && control.PriorWeeks == 0 && control.Standing.All(s => s.PriorShows == 0), "control world starts with no pre-rolled history");

            // 1. Flags follow the repository's convention.
            bool Rejects(params string[] extra) { try { LocalScenes.Configure(defaults.Concat(extra)); return false; } catch (ArgumentException) { return true; } }
            Check(Rejects("--enable-scene-room-prewarm", "--disable-scene-room-prewarm"), "enable and disable together are refused");
            Check(Rejects("--enable-scene-room-prewarm", "--disable-scene-rooms"), "the prewarm requires scene rooms");
            Check(Rejects("--enable-scene-room-prewarm", "--scene-room-prewarm-weeks=0"), "a zero-week pre-roll is refused");
            LocalScenes.Configure(defaults);
            Check(LocalScenes.RoomPrewarm && LocalScenes.RoomPrewarmWeeks == LocalScenes.DefaultRoomPrewarmWeeks, "on by default with rooms, 52 weeks");
            LocalScenes.Configure(defaults.Append("--disable-scene-rooms"));
            Check(!LocalScenes.RoomPrewarm, "off without scene rooms");

            // 2. A pre-roll from a fresh calendar, with the global and population streams pinned.
            LocalScenes.Configure(defaults.Append("--enable-scene-room-prewarm"));
            Check(LocalScenes.RoomPrewarm && LocalScenes.RoomPrewarmWeeks == LocalScenes.DefaultRoomPrewarmWeeks, "enabled with the default 52 weeks");
            ulong population = WorldStateService.Capture().PopulationRngState;
            GD.Seed(4417); float next = GD.Randf(); GD.Seed(4417);
            var started = DateTime.UtcNow;
            RestoreRooms(new SceneRoomSaveData());
            LocalSceneRoomService.Prewarm(date);
            double seconds = (DateTime.UtcNow - started).TotalSeconds;
            Check(GD.Randf() == next, "the pre-roll consumes no global RNG");
            Check(WorldStateService.Capture().PopulationRngState == population, "the pre-roll consumes no population RNG");
            var warmed = Snapshot();
            Check(warmed.PriorWeeks == 52 && warmed.Standing.Count > 0 && warmed.Standing.All(s => s.Shows == 0 && s.PriorShows > 0 && s.LastDay < 0),
                "history is recorded as prior shows only, dated before the start");
            Check(warmed.Bills.Count == 0 && warmed.Engagements.Count == 0 && warmed.Work.Count == 0 && warmed.HouseStanding.Count == 0 &&
                warmed.CompletedPerformances == 0 && warmed.LastScheduledWeek == -1 && warmed.LastResolvedDay == -1,
                "no bills, engagements, pay, hours, house-band standing or completed performances carry");
            Check(warmed.Standing.All(s => SceneRoomCatalog.Get(s.RoomId)?.IsPerformance == true), "only performance rooms build standing");
            Check(warmed.Standing.All(s => am.GetArtist(s.ArtistId) is { } a && a.formedYear <= 1959), "no act plays a year before it formed");
            Check(warmed.Standing.All(s => SceneRoomCatalog.Get(s.RoomId).Families.Contains(GenreCatalog.Get(GenreCatalog.MapLegacy(am.GetArtist(s.ArtistId).primaryGenre, 1960)).Family)),
                "history follows the rooms' own programming");
            int maxNights = SceneRoomCatalog.All.Max(r => Math.Max(r.Nights.Count, 5));
            Check(warmed.Standing.All(s => s.PriorShows <= 52 * maxNights), "no more shows than nights the room was open");
            Check(ActFacts() == facts, "reputation, momentum, careers, labels, prospects, fatigue and skill are untouched");

            // 3. The live calendar is the control's, bill for bill: history is not a live draw and no engagement carries.
            LocalSceneRoomService.EnsureCalendar(date);
            var live = Snapshot();
            Check(Json(live.Bills) == Json(control.Bills) && Json(live.Engagements) == Json(control.Engagements) && Json(live.Work) == Json(control.Work),
                "the opening calendar, audiences, engagements and work accounts match the control exactly");
            Check(live.Standing.Count == warmed.Standing.Count && live.PriorWeeks == 52, "the live calendar keeps the history");

            // 4. The readers see it.
            var veteran = live.Standing.OrderByDescending(s => s.PriorShows).ThenBy(s => s.ArtistId, StringComparer.Ordinal).First();
            Check(LocalSceneRoomService.ShowsAt(veteran.RoomId, veteran.ArtistId) == veteran.PriorShows, "the booker's record includes the history");
            Check(LocalSceneRoomService.ObserveActWork(veteran.ArtistId, today).LifetimeShows ==
                live.Standing.Where(s => s.ArtistId == veteran.ArtistId).Sum(s => s.PriorShows), "the contract outlook reads lifetime shows including history");

            // 5. Deterministic: a second pre-roll from a fresh calendar is identical.
            RestoreRooms(new SceneRoomSaveData());
            LocalSceneRoomService.Prewarm(date);
            Check(Json(Snapshot().Standing) == Json(warmed.Standing), "the pre-roll is deterministic");

            // 6. World creation only: never on a running, loaded or legacy calendar.
            RestoreRooms(live);
            LocalSceneRoomService.Prewarm(date);
            Check(Json(Snapshot()) == Json(live), "a running calendar is never pre-rolled twice");
            var legacy = LocalSceneRoomService.Copy(control);
            RestoreRooms(legacy);
            LocalSceneRoomService.Prewarm(date);
            Check(Json(Snapshot()) == Json(control), "a legacy calendar gains no invented history");
            var legacyJson = JsonSerializer.Serialize(live, SaveGameService.TestJsonOptions)
                .Replace("\"PriorShows\":", "\"_PriorShows\":").Replace("\"PriorWeeks\":", "\"_PriorWeeks\":");
            var older = JsonSerializer.Deserialize<SceneRoomSaveData>(legacyJson, SaveGameService.TestJsonOptions);
            Check(older.PriorWeeks == 0 && older.Standing.All(s => s.PriorShows == 0), "older saves without the fields restore with no history");

            // 7. Save/restore keeps it; malformed history is refused before mutation.
            RestoreRooms(live);
            string slot = "Prewarm " + Guid.NewGuid().ToString("N");
            try {
                Check(SaveGameService.Save(slot, out string message) && SaveGameService.Load(slot, out message), "compressed save/load: " + message);
                Check(Json(Snapshot().Standing) == Json(live.Standing) && Snapshot().PriorWeeks == 52, "history survives save and load");
            } finally { SaveGameService.Delete(slot); }
            foreach (var corrupt in new Action<SceneRoomSaveData>[] { s => s.PriorWeeks = -1, s => s.Standing[0].PriorShows = -3 }) {
                var invalid = LocalSceneRoomService.Copy(live); corrupt(invalid);
                string before = Json(Snapshot());
                bool refused = false;
                try { LocalSceneRoomService.BeginRestore(invalid); } catch (InvalidOperationException) { refused = true; }
                Check(refused && Json(Snapshot()) == before, "malformed history refused before mutation");
            }

            // 8. The off control does nothing.
            LocalScenes.Configure(args.Append("--disable-scene-room-prewarm"));
            RestoreRooms(new SceneRoomSaveData());
            LocalSceneRoomService.Prewarm(date);
            Check(Json(Snapshot()) == Json(new SceneRoomSaveData()), "--disable-scene-room-prewarm leaves a fresh calendar empty");

            var acts = live.Standing.GroupBy(s => s.ArtistId).Select(g => g.Sum(s => s.PriorShows)).OrderBy(x => x).ToArray();
            GD.Print($"SCENE_ROOM_PREWARM acts={acts.Length} rows={live.Standing.Count} medianShows={acts[acts.Length / 2]} maxShows={acts[^1]} " +
                $"atLeast12={acts.Count(x => x >= 12)} seconds={seconds:0.00}");
        } finally {
            LocalScenes.Configure(args);
            RestoreRooms(control);
        }
        Check(Json(Snapshot()) == Json(control) && ActFacts() == facts, "the check leaves the world as it found it");
        GD.Print($"SCENE_ROOM_PREWARM_CHECK_PASS checks={checks}");
    }
}
