using System;
using System.Collections.Generic;
using System.Linq;

public sealed record SceneAttentionEvidence(string EventId, string ArtistId, string LabelId,
    int SigningWeek, string PlaceId, Genre? SigningGenre, double Attribution,
    double DecayedImpulse, string Provenance);
public sealed record SceneAttentionObservation(string PlaceId, int Week, int ContributingEvents,
    double SigningHeat, double RawDecayedImpulse);
public sealed record SceneAttentionSnapshot(IReadOnlyList<SceneAttentionObservation> Cities,
    IReadOnlyList<SceneAttentionEvidence> Evidence);

/// <summary>Observation-only projection from committed contracts, not a news claim or
/// an economic reader. Initial allocation is not an event. Attribution has a single
/// bounded budget across working base, origin and HQ; missing places retain no credit.
/// Repeated reads/reloads cannot double-award an impulse because there are no writes.</summary>
public static class SceneDynamicsAttention {
    public const double HalfLifeWeeks = 8;
    public const double SigningImpulse = .05;
    public const int HorizonWeeks = 104;
    public static double Decay(double impulse, int elapsedWeeks) => elapsedWeeks < 0 ? 0 :
        Math.Max(0, impulse) * Math.Pow(.5, elapsedWeeks / HalfLifeWeeks);
    public static double BoundedHeat(double impulse) => 1 - Math.Exp(-Math.Max(0, impulse));

    public static SceneAttentionSnapshot Project(IEnumerable<SceneRecruitmentRecord> records, int week) {
        if (week < 0) throw new ArgumentOutOfRangeException(nameof(week));
        var evidence = new List<SceneAttentionEvidence>();
        // Ledger identity is artist + label + week + commit phase. Two distinct labels
        // remain distinct events; exact duplicate rows are suppressed. This ledger lacks
        // daily contract ordinals, so same-pair same-week repeats cannot be distinguished.
        string Key(SceneRecruitmentRecord r) => $"{r.ArtistId}|{r.LabelId}|{r.Week}|{r.Phase}";
        foreach (var r in (records ?? Array.Empty<SceneRecruitmentRecord>())
            .Where(r => r != null && r.Phase is "DailyMarket" or "WeeklyMarket" or "PlayerRival")
            .Where(r => !string.IsNullOrWhiteSpace(r.ArtistId) && !string.IsNullOrWhiteSpace(r.LabelId)
                && r.Week >= 0 && r.Week <= week && week - r.Week <= HorizonWeeks)
            .OrderBy(Key, StringComparer.Ordinal).ThenBy(r => r.BasePlaceId, StringComparer.Ordinal)
            .ThenBy(r => r.OriginPlaceId, StringComparer.Ordinal).ThenBy(r => r.HqPlaceId, StringComparer.Ordinal)
            .ThenBy(r => r.SigningGenre).GroupBy(Key, StringComparer.Ordinal).Select(g => g.First())) {
            var claims = new[] { (Place: r.BasePlaceId, Share: .6), (Place: r.OriginPlaceId, Share: .2), (Place: r.HqPlaceId, Share: .2) };
            foreach (var claim in claims.Where(c => ScenePlaceRegistry.Get(c.Place) != null)
                .GroupBy(c => LocalScenePersistenceService.SceneIdFor(c.Place), StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)) {
                double share = claim.Sum(c => c.Share);
                evidence.Add(new(Key(r), r.ArtistId, r.LabelId, r.Week, claim.Key, r.SigningGenre, share,
                    Decay(SigningImpulse * share, week - r.Week),
                    "Committed contract ledger; base 0.6, origin 0.2, HQ 0.2; unknown historical genre stays unknown"));
            }
        }
        var groups = evidence.GroupBy(e => e.PlaceId).ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        var cities = ScenePlaceRegistry.All.Where(p => p.CountryCode == "US" && p.Id == p.PlayableCityId)
            .OrderBy(p => p.Id, StringComparer.Ordinal).Select(p => {
                var items = groups.GetValueOrDefault(p.Id) ?? Array.Empty<SceneAttentionEvidence>();
                double raw = items.Sum(e => e.DecayedImpulse);
                return new SceneAttentionObservation(p.Id, week, items.Length, BoundedHeat(raw), raw);
            }).ToArray();
        return new(Array.AsReadOnly(cities), Array.AsReadOnly(evidence.ToArray()));
    }
    public static SceneAttentionSnapshot Capture(int week) {
        if (!LocalScenes.Persisting) throw new InvalidOperationException("Attention observation requires persistence.");
        return Project(ArtistManager.Instance.GetAllArtists().SelectMany(a => a.sceneRecruitmentHistory ?? new()), week);
    }
}
