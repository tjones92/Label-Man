using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PlayerDesk {
    private readonly Dictionary<string, SceneDiscovery> sceneDiscoveries = new(StringComparer.Ordinal);
    public IReadOnlyList<SceneDiscovery> SceneDiscoveries => Array.AsReadOnly(sceneDiscoveries.Values.OrderBy(d => d.ArtistId, StringComparer.Ordinal).ToArray());

    private void AdoptLegacyDiscoveries() {
        foreach (string id in generatedProspectIds.ToArray()) ArtistManager.Instance?.AdoptScoutingDiscovery(id);
        foreach (var act in notebook.Select(n => n.Artist).Concat(slate.Select(p => p.Artist)).Where(a => a != null).Distinct())
            ArtistManager.Instance?.AdoptScoutingDiscovery(act.artistId);
        generatedProspectIds.Clear(); // The scene population, not this slate, now owns their lifetime.
    }
    private void RememberSceneDiscovery(string id, string place, ScoutingVenue venue, int week) {
        if (!sceneDiscoveries.TryGetValue(id, out var discovery)) sceneDiscoveries[id] = discovery = new SceneDiscovery {
            ArtistId = id, FirstPlaceId = place, FirstWeek = week
        };
        discovery.LastPlaceId = place; discovery.LastWeek = week; discovery.Venue = (int)venue;
    }
    private bool ScoutPersistentScene(ScoutingVenue venue, out string message) {
        AdoptLegacyDiscoveries();
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
        string placeId = CurrentCityId;
        // Resolve the world cast before spending time. It exists independently of this observation.
        var cast = LocalScenePersistenceService.Encounter(placeId, venue, week, year);
        Spend(ScoutHours);
        slate.Clear();
        SlateDate = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
        foreach (SimulatedArtist artist in cast) {
            float noise = ScoutingReadNoise(Label.scoutingAbility);
            var prospect = new Prospect { Artist = artist, Venue = venue, CityId = placeId,
                ReadQuality = ScoutingPerception.PerceivedQuality(artist, Label, 0),
                ReadConfidence = Mathf.Clamp(Label.scoutingAbility, 0, 1),
                AskingAdvance = VenueAdvanceAsk(artist, venue, AskScaleFor(placeId)),
                Note = DescribeProspect(artist, Label, noise) };
            // Re-reading a cast cannot reroll the act's offered repertoire or consume the world's RNG.
            ulong readSeed = (ulong)(LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
                $"scene-read-v1|{placeId}|{(int)venue}|{week}|{artist.artistId}") * 9007199254740992.0);
            using var localDraws = new RandomNumberGenerator { Seed = readSeed };
            BuildLiveSet(prospect, artist, year, noise, localDraws, sceneRead: true);
            prospect.Rough = ReadRough(prospect, notebook.FirstOrDefault(n => n.Artist?.artistId == artist.artistId));
            slate.Add(prospect);
            RememberSceneDiscovery(artist.artistId, placeId, venue, week);
        }
        message = slate.Count == 0 ? "No local acts in this room's current cast. Try another room or return another week." :
            $"Heard {slate.Count} local {(slate.Count == 1 ? "act" : "acts")}. Some may already have a deal or be content to play locally.";
        Note($"Worked {VenueName(venue)} in {CurrentCity?.name ?? placeId}: {message}");
        Changed?.Invoke();
        return true;
    }
    public static bool SceneDealAvailable(Prospect prospect) => prospect?.Artist != null &&
        ReferenceEquals(ArtistManager.Instance?.GetArtist(prospect.Artist.artistId), prospect.Artist) &&
        ArtistManager.Instance.IsEligibleForPopulationSigning(prospect.Artist, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
    public static string SceneDealDescription(Prospect prospect) {
        if (prospect?.Artist == null || !ReferenceEquals(ArtistManager.Instance?.GetArtist(prospect.Artist.artistId), prospect.Artist))
            return "entry needs updating";
        if (!string.IsNullOrEmpty(prospect.Artist.labelId)) return "under contract to " +
            (ChartManager.Instance?.GetLabelName(prospect.Artist.labelId) ?? "another label");
        if (prospect.Artist.prospectMarketStatus == ProspectMarketStatus.Latent) return "not looking for a record deal";
        return SceneDealAvailable(prospect) ? $"asking ${prospect.AskingAdvance:N0}" : "not taking record offers right now";
    }
    /// <summary>Commit checks use the canonical object and the existing activation/cooldown owner.</summary>
    private bool CanCommitSceneSigning(Prospect prospect, out string message) {
        message = "";
        if (prospect?.Artist == null || !ReferenceEquals(ArtistManager.Instance?.GetArtist(prospect.Artist.artistId), prospect.Artist)) {
            message = "That discovery is no longer current. Return to the act's entry."; return false;
        }
        if (!string.IsNullOrEmpty(prospect.Artist.labelId)) { message = "Somebody signed them first."; return false; }
        if (ArtistManager.Instance?.IsEligibleForPopulationSigning(prospect.Artist, ChartManager.Instance?.GetCurrentChartWeek() ?? 0) != true) {
            message = "They're not taking offers right now."; return false;
        }
        if (Label?.HasRosterSpace != true) { message = "Roster is full."; return false; }
        return true;
    }
}
