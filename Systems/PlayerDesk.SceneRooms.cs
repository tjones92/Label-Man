using System;
using System.Linq;
using Godot;

public partial class PlayerDesk {
    public bool ScoutSceneRoom(string roomId, out string message) {
        if (!LocalScenes.Rooms) { message = "The room calendar is disabled."; return false; }
        var room = SceneRoomCatalog.Get(roomId);
        if (room?.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) {
            message = "You need to be in this room's city."; return false;
        }
        if (!Require(ScoutHours, out message)) return false;
        var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
        int hour = TimeManager.Instance?.CurrentHour ?? 9;
        var bill = LocalSceneRoomService.CurrentBill(roomId, date, hour);
        if (bill == null) { message = "There is no set or listening appointment you can catch now. Check this room's calendar."; return false; }
        var cast = LocalSceneRoomService.Hear(bill, hour);
        if (cast.Count == 0) { message = "The announced bill cannot go ahead. No new act has been substituted."; return false; }
        float admission = SceneLiveEconomics.Admission(room, date.year);
        if (Label.cashReserves < admission) { message = "You cannot cover admission."; return false; }
        AdoptLegacyDiscoveries();
        Label.cashReserves -= admission;
        Spend(ScoutHours); // The clock resolves shared work, irrespective of who observes it.
        slate.Clear(); SlateDate = date;
        foreach (var artist in cast) {
            var slot = bill.Appearances.Single(s => s.ArtistId == artist.artistId);
            float noise = ScoutingReadNoise(Label.scoutingAbility);
            var prospect = new Prospect { Artist = artist, Venue = room.Category, CityId = CurrentCityId,
                SceneRoomId = roomId, SceneBillId = bill.Id,
                ReadQuality = ScoutingPerception.PerceivedQuality(artist, Label, 0),
                ReadConfidence = Mathf.Clamp(Label.scoutingAbility, 0, 1),
                AskingAdvance = VenueAdvanceAsk(artist, room.Category, AskScaleFor(CurrentCityId)),
                Note = DescribeProspect(artist, Label, noise) + $" Heard the {slot.Role} at {room.Name}." };
            prospect.LiveSet.AddRange(ReadSceneRoomSet(artist, bill));
            prospect.HeardCount = Math.Min(2, prospect.LiveSet.Count);
            prospect.Rough = ReadRough(prospect, notebook.FirstOrDefault(n => n.Artist?.artistId == artist.artistId));
            slate.Add(prospect);
            RememberRoomEncounter(artist.artistId, room, bill, date);
        }
        RememberSceneContact(room, bill);
        message = $"Heard {slate.Count} {(slate.Count == 1 ? "act" : "acts")} at {room.Name}. The booker handles this room's announced bills; record-deal availability is separate.";
        Note(message); Changed?.Invoke(); return true;
    }
    private System.Collections.Generic.List<RepertoireItem> ReadSceneRoomSet(SimulatedArtist artist, SceneBill bill) {
        var set = new System.Collections.Generic.List<RepertoireItem>();
        float noise = ScoutingReadNoise(Label.scoutingAbility);
        foreach (var song in bill.Appearances.Single(s => s.ArtistId == artist.artistId).Set) {
                var canonical = string.IsNullOrEmpty(song.SongId) ? null : CompositionCatalogService.GetSong(song.SongId);
                float readNoise = (float)(LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
                    $"room-read-v1|{Label.labelId}|{bill.Id}|{artist.artistId}|{song.SongId ?? song.Title}") * 2 - 1) * noise;
                set.Add(new RepertoireItem { Title = song.Title, SongId = song.SongId, Genre = song.Genre,
                    IsOriginal = song.Original, ContentContext = song.ContentContext, SourceTag = song.Original ? "their own" : "standard",
                    ReadHook = Mathf.Clamp((canonical?.commercialHook ?? artist.songwritingAbility) + readNoise, 0, 1),
                    ReadQuality = Mathf.Clamp((canonical?.GetCraftScore() ?? artist.songwritingAbility) + readNoise, 0, 1) });
        }
        return set;
    }
    private void RememberRoomEncounter(string artistId, SceneRoomProfile room, SceneBill bill, GameDate date) {
        RememberSceneDiscovery(artistId, CurrentCityId, room.Category, ChartManager.Instance.GetCurrentChartWeek());
        var knowledge = sceneDiscoveries[artistId];
        knowledge.RoomId = room.Id; knowledge.BillId = bill.Id; knowledge.ContactId = room.ContactId;
        knowledge.LastDay = LocalSceneRoomService.Day(date);
    }
    private bool ScoutScheduledCategory(ScoutingVenue category, out string message) {
        var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
        int hour = TimeManager.Instance?.CurrentHour ?? 9;
        var room = SceneRoomCatalog.ForPlace(CurrentCityId).FirstOrDefault(r => r.Category == category &&
            LocalSceneRoomService.CurrentBill(r.Id, date, hour) != null);
        if (room == null) { message = "No announced bill in this kind of room is open now. Check the named rooms' calendars."; return false; }
        return ScoutSceneRoom(room.Id, out message);
    }
}
