using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A counterfactual opportunity lead, never an approved or funded move.
/// Road distance and travel days are game-map estimates. No RNG, births, offers,
/// schedule writes, origin changes, or saved advancing sequence.</summary>
public sealed record SceneMoveObservation(string ArtistId, string OriginPlaceId, string FromPlaceId,
    string ToPlaceId, Genre Genre, int Year, int Window, double RoadMiles, int TravelDays,
    double OpportunityRatio, string Reason, string UnresolvedRequirements);

public static class SceneDynamicsMobility {
    public const double MaximumRoadMiles = 350;
    public const double MinimumOpportunityRatio = 1.5;
    public const int WindowWeeks = 13;
    public static SceneMoveObservation Evaluate(string artistId, string origin, string source,
        Genre genre, int year, int calendarWeek, bool committed) {
        if (committed || string.IsNullOrEmpty(artistId) || calendarWeek < 0 || year < 1960 || year > 1969)
            return null;
        var home = ScenePlaceRegistry.Get(source);
        if (home?.CountryCode != "US" || home.Id != home.PlayableCityId) return null;
        double baseline = SceneCityPlacement.Weight(source, genre, year);
        if (!(baseline > 0)) return null;
        var candidates = ScenePlaceRegistry.All.Where(p => p.CountryCode == "US" && p.Id == p.PlayableCityId && p.Id != source)
            .Select(p => (Place: p, Ratio: SceneCityPlacement.Weight(p.Id, genre, year) / baseline))
            .Where(p => p.Ratio >= MinimumOpportunityRatio)
            .Select(p => (p.Place, p.Ratio, Miles: ScenePlaceRegistry.TryDomesticRoadMiles(source, p.Place.Id, out double miles) ? miles : double.NaN))
            .Where(p => double.IsFinite(p.Miles) && p.Miles > 0 && p.Miles <= MaximumRoadMiles)
            .OrderByDescending(p => p.Ratio).ThenBy(p => p.Miles).ThenBy(p => p.Place.Id, StringComparer.Ordinal).ToArray();
        if (candidates.Length == 0) return null;
        var target = candidates[0];
        return new(artistId, origin, source, target.Place.Id, genre, year, calendarWeek / WindowWeeks,
            target.Miles, Math.Max(1, (int)Math.Ceiling(target.Miles / 250)), target.Ratio,
            "Existing genre opportunity prior; no label vacancy or named-room capacity input",
            "Consent; relocation funds; housing; full employment opportunity; access barriers; member agreement");
    }

    public static IReadOnlyList<SceneMoveObservation> Capture(GameDate date) {
        if (!LocalScenes.Persisting || !LocalScenes.Rooms)
            throw new InvalidOperationException("Movement observation requires persistence and rooms.");
        int week = LocalSceneRoomService.Day(date) / 7 + 1;
        int year = LocalSceneRoomService.Date(week * 7).year;
        var artists = ArtistManager.Instance.GetAllArtists().ToArray();
        // A person active in more than one act cannot be relocated independently.
        var shared = artists.Where(a => a.isActive && a.lifecycleStatus == ArtistLifecycleStatus.Active)
            .SelectMany(a => a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active)
                .Where(m => !string.IsNullOrEmpty(m.personId)).Select(m => (a.artistId, m.personId))).Distinct()
            .GroupBy(p => p.personId).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
        var proposals = new List<SceneMoveObservation>();
        foreach (var a in artists.Where(a => a.isActive && a.lifecycleStatus == ArtistLifecycleStatus.Active
            && a.careerState is not (CareerState.Disbanded or CareerState.Retired)
            && string.IsNullOrEmpty(a.labelId) && a.prospectMarketStatus == ProspectMarketStatus.Seeking
            && GenreSupplyService.IsAvailableForNewSupply(a.primaryGenre, year)
            && a.members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active))) {
            bool committed = a.members.Any(m => shared.Contains(m.personId))
                || LocalSceneRoomService.HasFutureCommitment(a.artistId, LocalSceneRoomService.Day(date))
                || a.sceneParticipations?.Any(p => p.relationship is not (SceneRelationship.Resident or SceneRelationship.Alumnus)
                    && (!p.endWeek.HasValue || p.endWeek.Value >= week)) == true
                // Conservative year-level cooldown where legacy move precision is uncertain.
                || a.geography?.moves?.Any(m => m.year >= year - 1) == true;
            var proposal = Evaluate(a.artistId, a.geography?.originPlaceId, a.geography?.basePlaceId,
                a.primaryGenre, year, week, committed);
            if (proposal != null) proposals.Add(proposal);
        }
        // Bounded observer attention, not a movement quota. Same quarterly window keeps
        // identity stable; repeated rows are observations, never repeated event impulses.
        return Array.AsReadOnly(proposals.GroupBy(p => p.FromPlaceId).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.OrderBy(p => LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
                $"mobility-observation-v1|{p.Window}|{p.ArtistId}"))
                .ThenBy(p => p.ArtistId, StringComparer.Ordinal).First()).ToArray());
    }
}
