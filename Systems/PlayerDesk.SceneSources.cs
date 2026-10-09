using System;
using System.Linq;
using Godot;

public partial class PlayerDesk {
    private bool FollowUpSourceDemo(Prospect prospect, out string message) {
        message = "The submission or its introduction is no longer current.";
        if (SceneSourceService.Connection(Label, prospect.Artist.geography?.basePlaceId, ChartManager.Instance.GetCurrentChartWeek())?.Id != prospect.SourceConnectionId ||
            !ReferenceEquals(ArtistManager.Instance.GetArtist(prospect.Artist.artistId), prospect.Artist) || !SceneDealAvailable(prospect)) return false;
        if (prospect.FollowedUp) { message = "You already reviewed the full submission."; return true; }
        if (!Require(FollowUpHours, out message)) return false;
        Spend(FollowUpHours); prospect.HeardCount = prospect.LiveSet.Count; prospect.FollowedUp = true;
        prospect.ReadConfidence = Mathf.Clamp(prospect.ReadConfidence + .15f, 0, 1);
        var watched = notebook.FirstOrDefault(n => n.Artist?.artistId == prospect.Artist.artistId);
        if (watched != null) {
            watched.HeardCount = prospect.HeardCount; watched.FollowedUp = true;
            watched.ReadConfidence = prospect.ReadConfidence; watched.SourceConnectionId = prospect.SourceConnectionId;
            watched.LastSeen = TimeManager.Instance.CurrentDate;
        }
        message = "Reviewed the full demo submission. A live visit, travel and master licensing would be separate arrangements.";
        Note(message); Changed?.Invoke(); return true;
    }
    public bool ArrangeSourceIntroduction(string placeId, out string message) {
        if (!Require(2, out message)) return false;
        if (!SceneSourceService.ArrangeIntroduction(Label, placeId, out message)) return false;
        Spend(2); Note(message); Changed?.Invoke(); return true;
    }
    public bool ReviewSourceDemos(string placeId, out string message) {
        message = "Arrange a current introduction to this source office first.";
        int week = ChartManager.Instance.GetCurrentChartWeek();
        var connection = SceneSourceService.Connection(Label, placeId, week);
        if (connection == null || !Require(ScoutHours, out message)) return false;
        var candidates = ArtistManager.Instance.GetUnsignedArtists().Where(a => a.geography?.basePlaceId == placeId);
        var cast = SceneRecruitmentService.Slate(Label, candidates, week, week / 4, 4, out _);
        if (cast.Count == 0) { message = "This office has no available submissions right now. No substitute act has been created."; return false; }
        AdoptLegacyDiscoveries();
        var date = TimeManager.Instance.CurrentDate;
        Spend(ScoutHours); slate.Clear(); SlateDate = date;
        foreach (var artist in cast) {
            var prospect = new Prospect { Artist = artist, Venue = ScoutingVenue.IndustryMeets, CityId = CurrentCityId,
                SourceConnectionId = connection.Id,
                ReadQuality = ScoutingPerception.PerceivedQuality(artist, Label, 0), ReadConfidence = Mathf.Clamp(Label.scoutingAbility, 0, 1),
                AskingAdvance = VenueAdvanceAsk(artist, ScoutingVenue.IndustryMeets, AskScaleFor(CurrentCityId)),
                Note = $"Demo submitted through the {ScenePlaceRegistry.Get(placeId).Name} office. The act remains based there; no live appearance or master rights are implied." };
            using var draws = new RandomNumberGenerator { Seed = (ulong)(LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
                $"source-demo-read-v1|{Label.labelId}|{artist.artistId}|{week / 4}") * 9007199254740992.0) };
            BuildLiveSet(prospect, artist, date.year, ScoutingReadNoise(Label.scoutingAbility), draws, sceneRead: true);
            foreach (var song in prospect.LiveSet) song.SourceTag = "submitted demo";
            prospect.HeardCount = Math.Min(2, prospect.LiveSet.Count);
            prospect.Rough = ReadRough(prospect, notebook.FirstOrDefault(n => n.Artist?.artistId == artist.artistId));
            slate.Add(prospect);
            RememberSceneDiscovery(artist.artistId, CurrentCityId, ScoutingVenue.IndustryMeets, week);
        }
        message = $"Reviewed {cast.Count} submissions from {ScenePlaceRegistry.Get(placeId).Name}. Record offers use the usual ownership and availability checks.";
        Note(message); Changed?.Invoke(); return true;
    }
}
