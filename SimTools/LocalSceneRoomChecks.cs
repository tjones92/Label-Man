using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class LocalSceneRoomChecks {
    public static void Run() {
        if (!LocalScenes.Rooms) throw new InvalidOperationException("Scene rooms must be enabled for these checks.");
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCENE_ROOM_CHECK_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var date = TimeManager.Instance.CurrentDate;
        var original = WorldStateService.Capture();
        var am = ArtistManager.Instance;
        SceneRoomSaveData Snapshot() { var w = new WorldSaveData(); LocalSceneRoomService.CaptureWorld(w); return w.SceneRooms; }
        void RestoreRooms(SceneRoomSaveData saved) { LocalSceneRoomService.BeginRestore(saved); LocalSceneRoomService.CompleteRestore(); }
        try {
            Check(SceneRoomCatalog.All.Count == 186, "six stable supporting rooms per playable city");
            Check(SceneRoomCatalog.All.Select(r => r.PlaceId).Distinct().Count() == 31, "all 31 cities covered");
            Check(SceneRoomCatalog.All.Select(r => r.Id).Distinct().Count() == 186, "unique room IDs");
            Check(SceneRoomCatalog.All.Select(r => r.ContactId).Distinct().Count() == 186, "persistent booker identity separate from musician population");
            Check(SceneRoomCatalog.All.All(r => r.Fictional && r.Source.Length > 0), "fictional supporting institutions and source/uncertainty provenance explicit");
            Check(SceneRoomCatalog.All.Where(r => r.Kind == SceneRoomKind.Coffeehouse || r.Kind == SceneRoomKind.CommunityHall).All(r => r.Admission == 0), "community/acoustic access needs no alcohol purchase");
            Check(SceneRoomCatalog.Get("new_orleans:listening").Programming(1969).Contains("traditional jazz"), "New Orleans jazz survives late pop attention changes");
            Check(SceneRoomCatalog.Get("san_francisco:acoustic").FromYear == 1960, "early Bay folk activity exists");
            Check(SceneRoomCatalog.Get("nashville:club").Families.Contains(GenreFamily.RhythmAndSoul), "Nashville is not country-only");
            Check(SceneRoomCatalog.Get("miami:community").Families.Contains(GenreFamily.Latin), "Miami includes Spanish-language performance programming");
            Check(SceneRoomCatalog.Get("san_antonio:community").Families.Contains(GenreFamily.Latin), "conjunto programming has a distinct community room");
            Check(SceneRoomCatalog.Get("billings:club") != null && SceneRoomCatalog.Get("omaha:listening") != null, "smaller/less-documented places retain conservative opportunities");
            Check(SceneRoomCatalog.Get("detroit:club").Programming(1969).Contains("soul"), "Detroit later electric programming preserves soul");
            Check(SceneRoomCatalog.ForPlace("oakland").Count == 6 && ScenePlaceRegistry.Get("oakland").Id == "oakland", "catchment room access does not rewrite satellite identity");
            Check(SceneRoomCatalog.ForPlace("gb_london").Count == 0, "foreign sources do not receive fictitious domestic rooms");

            var before = Snapshot();
            var calendar = LocalSceneRoomService.Calendar("nashville", date);
            Check(calendar.Count > 0 && calendar.Any(b => b.Appearances.Count > 0), "calendar populated independently of player visits");
            Check(Json(before) == Json(Snapshot()), "opening a current calendar does not redraw bookings");
            calendar[0].Appearances.Clear();
            Check(Json(before) == Json(Snapshot()), "returned calendars cannot mutate the shared world");
            Check(before.Bills.Select(b => b.Id).Distinct().Count() == before.Bills.Count, "one bill per room/date");
            Check(before.Bills.All(b => b.Day >= 0 && b.Day < 14), "initial rolling horizon bounded to two weeks");
            Check(before.Bills.All(b => b.Appearances.All(s => am.GetArtist(s.ArtistId) != null)), "bill IDs resolve canonical acts");
            var reservations = before.Bills.SelectMany(b => b.Appearances.SelectMany(s => s.PersonIds.Select(p => new { Key = $"{b.Day}|{s.StartHour}|{p}", DayPerson = $"{b.Day}|{p}", b.RoomId })));
            Check(reservations.Select(r => r.Key).Distinct().Count() == reservations.Count(), "no overlapping shared-musician slots");
            Check(reservations.GroupBy(r => r.DayPerson).All(g => g.Select(r => r.RoomId).Distinct().Count() == 1), "a person cannot teleport between rooms/cities in a day");
            Check(before.Bills.All(b => b.Appearances.All(s => s.PersonIds.Distinct().Count() == s.PersonIds.Count)), "person IDs deduplicated within a show");
            Check(before.Bills.All(b => b.Appearances.All(s => s.Set.All(t => t.SongId == null || CompositionCatalogService.GetSong(t.SongId) != null))), "shared repertoire references existing compositions");
            Check(before.Engagements.Count > 0 && before.Engagements.All(e => e.ThroughDay >= e.StartDay), "dated recurring engagements exist");
            Check(before.Bills.Any(b => b.Appearances.Any(s => !string.IsNullOrEmpty(am.GetArtist(s.ArtistId).labelId))), "signed residents remain publicly hearable");
            Check(before.Bills.Any(b => b.Appearances.Any(s => am.GetArtist(s.ArtistId).prospectMarketStatus == ProspectMarketStatus.Latent)), "latent contract prospects can work publicly");
            ulong populationBefore = original.PopulationRngState;
            GD.Seed(812334); float next = GD.Randf(); GD.Seed(812334);
            _ = LocalSceneRoomService.Calendar("miami", date);
            Check(GD.Randf() == next, "calendar reads preserve global RNG");
            Check(WorldStateService.Capture().PopulationRngState == populationBefore, "scene calendar does not consume population RNG");
            int artistCount = am.GetAllArtists().Count();
            var first = before.Bills.First(b => b.Day == 0 && b.Appearances.Count > 0 && SceneRoomCatalog.Get(b.RoomId).IsPerformance);
            var room = SceneRoomCatalog.Get(first.RoomId);
            var heard = LocalSceneRoomService.Hear(first, first.StartHour);
            Check(heard.Count > 0 && Snapshot().CompletedPerformances == before.CompletedPerformances, "hearing a bill does not resolve extra work");
            Check(LocalSceneRoomService.CurrentBill(first.RoomId, date, first.EndHour) == null, "closed bills cannot be observed as current");
            Check(LocalSceneRoomService.CurrentBill(first.RoomId, date.AddDays(2), first.StartHour) == null, "room hours alone do not invent tomorrow's performance");
            LocalSceneRoomService.Advance(date, 24);
            var completed = Snapshot();
            Check(completed.CompletedPerformances > before.CompletedPerformances, "clock resolves unvisited performances");
            Check(completed.Work.Any(w => w.StageHours > 0), "real hours and performer IDs enter the work ledger");
            Check(completed.Bills.Where(b => b.Day == 0).All(b => b.Status != SceneBillStatus.Scheduled), "completed-day bills resolve exactly once");
            LocalSceneRoomService.Advance(date, 24);
            Check(Json(completed) == Json(Snapshot()), "duplicate clock callback adds no gigs, fees or practice");
            Check(am.GetAllArtists().Count() == artistCount, "performances do not create or delete acts");
            Check(WorldStateService.Capture().ArtistIdCounter == original.ArtistIdCounter && WorldStateService.Capture().MusicianIdCounter == original.MusicianIdCounter, "performance does not spend birth counters");
            Check(completed.Bills.All(b => b.Attendance <= SceneRoomCatalog.Get(b.RoomId).Capacity && b.Attendance >= 0), "attendance bounded by each physical room");
            Check(completed.Bills.Where(b => SceneRoomCatalog.Get(b.RoomId).Kind == SceneRoomKind.TradeEvent).All(b => b.GrossReceipts == 0), "trade listening appointments do not pay stage receipts");
            var worker = completed.Work.First();
            LocalSceneRoomService.AttributeBudget(worker.ArtistId, worker.PersonId, worker.Year, 450);
            var credited = Snapshot().Work.First(w => w.ArtistId == worker.ArtistId && w.PersonId == worker.PersonId && w.Year == worker.Year);
            Check(credited.AttributedHours + credited.BackgroundHours == 450 && credited.AttributedHours <= credited.StageHours, "actual hours replace corresponding synthetic allowance without double counting");
            LocalSceneRoomService.AttributeRoadBudget(worker.ArtistId, worker.PersonId, worker.Year, 225);
            credited = Snapshot().Work.First(w => w.ArtistId == worker.ArtistId && w.PersonId == worker.PersonId && w.Year == worker.Year);
            Check(credited.AttributedRoadHours + credited.BackgroundRoadHours == 225, "fatigue allowance reconciles actual and background work");
            LocalSceneRoomService.AttributeBudget(worker.ArtistId, worker.PersonId, worker.Year, 0);
            credited = Snapshot().Work.First(w => w.ArtistId == worker.ArtistId && w.PersonId == worker.PersonId && w.Year == worker.Year);
            Check(credited.AttributedHours == 0 && credited.UnbudgetedHours == credited.StageHours, "unbudgeted activity is exposed, never silently granted as bonus practice");

            RestoreRooms(before);
            var cancelledBill = before.Bills.First(b => b.Day == 0 && b.Appearances.Count > 0 && SceneRoomCatalog.Get(b.RoomId).IsPerformance);
            var cancelledAct = am.GetArtist(cancelledBill.Appearances[0].ArtistId);
            var oldLife = cancelledAct.lifecycleStatus;
            cancelledAct.lifecycleStatus = ArtistLifecycleStatus.Retired;
            LocalSceneRoomService.Advance(date, 24);
            var cancelledSlot = Snapshot().Bills.First(b => b.Id == cancelledBill.Id).Appearances[0];
            Check(cancelledSlot.Status == SceneBillStatus.Cancelled && cancelledSlot.Fee == 0, "terminal career cancels the booked slot without substitution/payment");
            cancelledAct.lifecycleStatus = oldLife;
            RestoreRooms(before);
            var member = cancelledAct.members.First(m => m.isActive && m.lifeState == MemberLifeState.Active);
            member.isActive = false;
            LocalSceneRoomService.Advance(date, 24);
            Check(Snapshot().Bills.First(b => b.Id == cancelledBill.Id).Appearances[0].Status == SceneBillStatus.Cancelled, "changed lineup cancels old personnel commitments");
            member.isActive = true;
            RestoreRooms(before);

            string frozen = Json(before);
            LocalScenes.Configure(new[] { "--disable-scene-rooms" });
            LocalSceneRoomService.Advance(date.AddDays(40), 24);
            Check(Json(Snapshot()) == frozen && LocalSceneRoomService.Calendar("nashville", date).Count == 0, "rooms-off preserves existing calendars but generates no work or observation");
            LocalScenes.Configure(Array.Empty<string>());
            LocalSceneRoomService.Advance(date.AddDays(40), 9);
            Check(Snapshot().CompletedPerformances == before.CompletedPerformances, "re-enabling after a gap does not backpay frozen performances");
            RestoreRooms(before);
            _ = LocalSceneRoomService.Calendar(room.PlaceId, date.AddDays(40));
            LocalSceneRoomService.Advance(date.AddDays(40), 9);
            Check(Snapshot().CompletedPerformances == before.CompletedPerformances, "opening the calendar after a pause cannot bypass the no-backpay rule");
            RestoreRooms(null);
            LocalSceneRoomService.EnsureCalendar(date.AddDays(40));
            var migrated = Snapshot();
            Check(migrated.CompletedPerformances == 0 && migrated.Work.Count == 0 && migrated.Bills.All(b => b.Day >= 40), "legacy worlds gain a future calendar, no invented past gigs");
            RestoreRooms(before);
            bool futureRejected = false;
            try { WorldStateService.Apply(new WorldSaveData { SceneRooms = new SceneRoomSaveData { SchemaVersion = 99 } }, date, SimulationSeedBootstrap.RequestedSeed); }
            catch (InvalidOperationException) { futureRejected = true; }
            Check(futureRejected && am.GetAllArtists().Count() == artistCount && Json(Snapshot()) == frozen, "future schema rejected before world mutation");
            var bad = LocalSceneRoomService.Copy(before); bad.Bills.Add(LocalSceneRoomService.Copy(bad.Bills[0]));
            bool badRejected = false;
            try { LocalSceneRoomService.ValidateRestore(bad); } catch (InvalidOperationException) { badRejected = true; }
            Check(badRejected, "duplicate saved booking IDs rejected");
            LocalScenes.Configure(new[] { "--observe-local-scenes" });
            Check(!LocalScenes.Rooms && !LocalScenes.Persisting, "observe mode never enables bookings");
            LocalScenes.Configure(Array.Empty<string>());
            bool rejected = false;
            try { LocalScenes.Configure(new[] { "--enable-scene-rooms", "--disable-scene-rooms" }); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "contradictory room flags rejected");
            rejected = false;
            try { LocalScenes.Configure(new[] { "--enable-scene-rooms", "--disable-local-scenes" }); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "explicit rooms require persistence");
            LocalScenes.Configure(Array.Empty<string>());

            // Real player/world gzip seam, including dated discovery and notebook source IDs.
            var desk = PlayerDesk.Instance;
            Check(desk.FoundLabel("Room Probe Records", room.PlaceId, out _), "player label founded in booked city");
            TimeManager.Instance.RestoreClock(date, first.StartHour);
            RestoreRooms(before);
            float cash = desk.Label.cashReserves;
            Check(desk.ScoutSceneRoom(first.RoomId, out _), "player hears the actual named bill");
            Check(desk.Label.cashReserves == cash - room.Admission, "room admission charged once per visit");
            string read = Json(desk.Slate.Select(p => new { p.Artist.artistId, p.LiveSet }).ToList());
            string billsAfter = Json(Snapshot());
            TimeManager.Instance.RestoreClock(date, first.StartHour);
            GD.Seed(554433); next = GD.Randf(); GD.Seed(554433);
            Check(desk.ScoutSceneRoom(first.RoomId, out _), "repeat observation of booked bill succeeds");
            Check(read == Json(desk.Slate.Select(p => new { p.Artist.artistId, p.LiveSet }).ToList()) && billsAfter == Json(Snapshot()), "repeat observation preserves shared sets and work ledger");
            Check(GD.Randf() == next, "room scouting does not alter global RNG");
            Check(desk.SceneDiscoveries.All(d => d.RoomId == first.RoomId && d.BillId == first.Id && d.ContactId == room.ContactId), "knowledge stores actual room/bill/contact provenance");
            var prospect = desk.Slate.First();
            Check(desk.AddToNotebook(prospect, out _), "notebook remembers named encounter");
            string slotName = "Room probe " + Guid.NewGuid().ToString("N");
            try {
                var saved = Snapshot();
                Check(SaveGameService.Save(slotName, out _), "actual gzip player/world save writes calendar");
                LocalScenes.Configure(new[] { "--disable-local-scenes" });
                Check(SaveGameService.Load(slotName, out _) && Json(Snapshot()) == Json(saved), "disabled real load preserves calendar/work state");
                LocalScenes.Configure(Array.Empty<string>());
                Check(SaveGameService.Load(slotName, out _) && Json(Snapshot()) == Json(saved), "enabled real load preserves exact calendar/work state");
                Check(desk.CaptureState().Notebook.Single().SceneRoomId == first.RoomId, "notebook source survives gzip reload");
                var savedWorld = WorldStateService.Capture();
                var worldJson = Json(savedWorld);
                WorldStateService.Apply(LocalSceneRoomService.Copy(savedWorld), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
                Check(Json(WorldStateService.Capture()) == worldJson, "full world round trip preserves rooms and canonical links");
                LocalSceneRoomService.Advance(date, 24); string replayA = Json(Snapshot());
                WorldStateService.Apply(LocalSceneRoomService.Copy(savedWorld), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
                LocalSceneRoomService.Advance(date, 24);
                Check(Json(Snapshot()) == replayA, "restored performance resolution replays exactly");
            } finally { SaveGameService.Delete(slotName); }
            var laterBill = before.Bills.FirstOrDefault(b => b.Day > first.Day && b.RoomId == first.RoomId && b.Appearances.Any(a => a.ArtistId == prospect.Artist.artistId));
            Check(laterBill != null, "recurring act has another announced bill for a real follow-up");
            RestoreRooms(before);
            TimeManager.Instance.RestoreClock(LocalSceneRoomService.Date(laterBill.Day), laterBill.StartHour);
            var laterSlot = laterBill.Appearances.Single(a => a.ArtistId == prospect.Artist.artistId);
            cash = desk.Label.cashReserves;
            Check(desk.FollowUp(prospect, out _), "follow-up attends the act's next actual bill");
            Check(prospect.SceneBillId == laterBill.Id && prospect.LiveSet.Select(s => s.SongId).SequenceEqual(laterSlot.Set.Select(s => s.SongId)), "follow-up reads the new shared set, not a stale prior set");
            Check(desk.Label.cashReserves == cash - room.Admission && desk.SceneDiscoveries.Single(d => d.ArtistId == prospect.Artist.artistId).BillId == laterBill.Id,
                "follow-up admission and knowledge belong to the actual encounter");
            Check(desk.CaptureState().Notebook.Single().SceneBillId == laterBill.Id, "follow-up updates notebook bill provenance");
            GD.Print($"SCENE_ROOM_CHECK_PASS checks={checks}");
        } finally {
            LocalScenes.Configure(Array.Empty<string>());
            // Probe processes quit immediately; this restores the world without fabricating legacy history.
            WorldStateService.Apply(original, date, SimulationSeedBootstrap.RequestedSeed);
        }
    }
}
