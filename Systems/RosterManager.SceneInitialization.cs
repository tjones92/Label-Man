using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RosterManager {
    // Diagnostic only: desired launch headcount, before SetOperatingRosterTargetFromCurrent.
    internal readonly List<SceneInitializationAppointment> SceneInitializationAppointments = new();
    internal sealed record SceneInitializationAppointment(int Round, string LabelId, string ArtistId, int SlateSize, float TrueQuality, float PerceivedQuality);
    private int sceneInitializationRound;
    internal readonly Dictionary<string, SceneInitializationObservation> SceneInitialization = new(StringComparer.Ordinal);
    internal sealed record SceneInitializationObservation(int Target, int Filled, int AccessiblePreferred,
        int AccessibleSecondary, int AccessibleAny, float MeanQuality, int StrongActs);

    private static int InitializationTierPriority(LabelTier tier) => tier switch {
        LabelTier.Major => 0, LabelTier.MidTier => 1, LabelTier.Independent => 2,
        LabelTier.Boutique => 3, _ => 4
    };

    internal static List<AILabel> SceneInitializationOrder(IEnumerable<AILabel> labels, int round) =>
        labels.OrderBy(l => InitializationTierPriority(l.tier))
            .ThenBy(l => LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
                $"initial-order-v1|{round}|{l.labelId}"))
            .ThenBy(l => l.labelId, StringComparer.Ordinal).ToList();

    private static bool MatchesInitialStyle(Genre[] genres, SimulatedArtist artist) =>
        genres?.Any(g => g == artist.primaryGenre || g == artist.secondaryGenre) == true;

    // Evaluate access BEFORE style fallback. A remote preferred act cannot block a local secondary act.
    internal static List<SimulatedArtist> SceneInitialPool(AILabel label, IEnumerable<SimulatedArtist> supply) {
        var accessible = supply.DistinctBy(a => a.artistId)
            .Where(a => SceneRecruitmentService.CanAccess(label, a, 0))
            .OrderBy(a => a.artistId, StringComparer.Ordinal).ToList();
        var preferred = accessible.Where(a => MatchesInitialStyle(label.preferredGenres, a)).ToList();
        if (preferred.Count > 0) return preferred;
        var secondary = accessible.Where(a => MatchesInitialStyle(label.secondaryGenres, a)).ToList();
        return secondary.Count > 0 ? secondary : accessible;
    }

    private static int DrawSceneInitialTarget(AILabel label) {
        // Same tier fill ranges as the frozen allocation; no capacity or population retuning.
        float fill = label.tier switch {
            LabelTier.Major => (float)GD.RandRange(0.6, 0.85),
            LabelTier.MidTier => (float)GD.RandRange(0.5, 0.75),
            LabelTier.Independent => (float)GD.RandRange(0.4, 0.7),
            LabelTier.Small => (float)GD.RandRange(0.3, 0.6),
            LabelTier.Boutique => (float)GD.RandRange(0.5, 0.8), _ => 0.5f
        };
        return Mathf.RoundToInt(label.maxRosterSize * fill);
    }

    private void InitializeSceneRosters(List<AILabel> labels, int year) {
        SceneInitialization.Clear();
        SceneInitializationAppointments.Clear();
        var targets = new Dictionary<string, int>(StringComparer.Ordinal);
        // Canonical preparation order makes list storage order irrelevant.
        foreach (var label in labels.OrderBy(l => l.labelId, StringComparer.Ordinal)) {
            label.InitializeRoster();
            targets.Add(label.labelId, DrawSceneInitialTarget(label));
        }
        var remaining = labels.Where(l => l.CurrentRosterSize < targets[l.labelId]).ToList();
        for (int round = 0; remaining.Count > 0; round++) {
            sceneInitializationRound = round;
            foreach (var label in SceneInitializationOrder(remaining, round)) {
                var artist = FindArtistForLabel(label, year);
                if (artist == null) { remaining.Remove(label); continue; }
                InitialSignArtist(label, artist, year);
                if (label.CurrentRosterSize >= targets[label.labelId]) remaining.Remove(label);
            }
        }
        var unsigned = ArtistManager.Instance.GetLaunchRosterCandidates();
        foreach (var label in labels) {
            var accessible = unsigned.Where(a => SceneRecruitmentService.CanAccess(label, a, 0)).ToList();
            SceneInitialization[label.labelId] = new(targets[label.labelId], label.CurrentRosterSize,
                accessible.Count(a => MatchesInitialStyle(label.preferredGenres, a)),
                accessible.Count(a => MatchesInitialStyle(label.secondaryGenres, a)), accessible.Count,
                label.roster.Count == 0 ? 0f : label.roster.Average(a => a.CalculateBaseQuality()),
                label.roster.Count(a => a.CalculateBaseQuality() >= .75f));
            label.SetOperatingRosterTargetFromCurrent();
            if (ArtistPopulationLifecycle.Enabled) {
                label.populationOrigin = LabelPopulationOrigin.LaunchPopulation;
                label.operatingRosterTargetReason = LabelOperatingTargetReason.LaunchPopulation;
                label.operatingRosterTargetSource = LabelOperatingTargetReason.LaunchPopulation.ToString();
            }
        }
    }

    private SimulatedArtist FindSceneInitialArtist(AILabel label) {
        var pool = SceneInitialPool(label, ArtistManager.Instance.GetLaunchRosterCandidates()
            .Where(a => !IsHeldForPlayer(a.artistId)));
        // A stable prelaunch read, not a new read on every round. Replace signed slate members
        // from the remaining real pool; repeated queries cannot reroll perception.
        var slate = SceneRecruitmentService.Slate(label, pool, 0, -1,
            GetDiscoverySlateSize(label.scoutingAbility), out _);
        var scored = slate.Select(a => (artist: a, score: ScoreArtistForLabel(a, label)))
            .Where(x => x.score > 0f).OrderByDescending(x => x.score)
            .ThenBy(x => x.artist.artistId, StringComparer.Ordinal).Take(10).ToList();
        if (scored.Count == 0) return null;
        SimulatedArtist Pick(SimulatedArtist artist) {
            SceneInitializationAppointments.Add(new(sceneInitializationRound, label.labelId, artist.artistId, slate.Count,
                artist.CalculateBaseQuality(), ScoutingPerception.PerceivedQuality(artist, label, -1)));
            return artist;
        }
        float roll = (float)GD.RandRange(0f, scored.Sum(x => x.score));
        foreach (var entry in scored) { roll -= entry.score; if (roll <= 0f) return Pick(entry.artist); }
        return Pick(scored[^1].artist);
    }
}
