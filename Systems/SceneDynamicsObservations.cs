using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Phase 5 observation: selected resident cohort versus the existing fictional
/// supporting rooms. These ratios are diagnostics, never carrying caps or birth requests.
/// No RNG, scheduling, relocations, contract activation, skill or economic feedback.</summary>
public sealed record SceneDynamicsObservation(string PlaceId, int CalendarWeek, int Year,
    int Residents, int WorkingResidents, int SignedResidents, int SeekingResidents, int LatentResidents,
    int BookableResidents, int PerformanceSlots, int BookedSlots, int BookedActs, int RecurringActs,
    int LabelCount, int OperatingSlotDeficit, double? BookableActsPerWeeklySlot);

public static class SceneDynamicsObservations {
    public static IReadOnlyList<SceneDynamicsObservation> Capture(GameDate date) {
        if (!LocalScenes.Persisting || !LocalScenes.Rooms)
            throw new InvalidOperationException("Scene dynamics observation requires persistence and rooms.");
        int week = LocalSceneRoomService.Day(date) / 7 + 1;
        int year = LocalSceneRoomService.Date(week * 7).year;
        var labels = (ChartManager.Instance?.GetAllLabels() ?? new()).Where(l => l.IsActive)
            .GroupBy(l => LocalScenePersistenceService.SceneIdFor(l.geography?.basePlaceId) ?? "")
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        var results = new List<SceneDynamicsObservation>();
        foreach (var place in ScenePlaceRegistry.All.Where(p => p.CountryCode == "US" && p.Id == p.PlayableCityId)
            .OrderBy(p => p.Id, StringComparer.Ordinal)) {
            var residents = LocalScenePersistenceService.MembersOf(place.Id).Select(ArtistManager.Instance.GetArtist)
                .Where(a => a != null && LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId) == place.Id
                    && a.sceneParticipations?.Any(p => p.relationship == SceneRelationship.Resident && !p.endWeek.HasValue
                        && p.sceneId == place.Id) == true).ToArray();
            var rooms = SceneRoomCatalog.All.Where(r => r.PlaceId == place.Id && r.IsPerformance).ToArray();
            bool Bookable(SimulatedArtist a) => rooms.Any(r => Enumerable.Range(week * 7, 7).Any(day =>
                LocalSceneRoomService.Date(day) <= GameDate.EndDate && SceneLiveEconomics.OpenOn(r, LocalSceneRoomService.Date(day).DayOfWeek)
                && LocalSceneRoomService.Eligible(a, r, day)));
            int bookable = residents.Count(Bookable);
            var capacity = LocalSceneRoomService.ObserveWeekCapacity(place.Id, week);
            var localLabels = labels.GetValueOrDefault(place.Id) ?? Array.Empty<AILabel>();
            int working = residents.Count(a => a.sceneParticipations.Any(p => p.relationship == SceneRelationship.Resident
                && !p.endWeek.HasValue && p.sceneId == place.Id && p.performanceLevel >= ScenePerformanceLevel.Working));
            results.Add(new(place.Id, week, year, residents.Length, working,
                residents.Count(a => !string.IsNullOrEmpty(a.labelId)),
                residents.Count(a => a.prospectMarketStatus == ProspectMarketStatus.Seeking),
                residents.Count(a => a.prospectMarketStatus == ProspectMarketStatus.Latent), bookable,
                capacity.Slots, capacity.BookedSlots, capacity.BookedActs, capacity.RecurringActs,
                localLabels.Length, localLabels.Sum(l => Math.Max(0, l.OperatingRosterTarget - l.CurrentRosterSize)),
                capacity.Slots > 0 ? (double)bookable / capacity.Slots : null));
        }
        return Array.AsReadOnly(results.ToArray());
    }
}
