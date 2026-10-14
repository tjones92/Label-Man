using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Live discovery feedback from last week's committed contracts. Pure, bounded
/// weighting within existing access and slate budgets; no prices, births or movement.
/// The weekly cache is disposable and rebuilt from saved ledgers after restore.</summary>
public sealed class SceneAttentionFeedbackSnapshot {
    public int InputWeek { get; }
    private readonly Dictionary<(string Place, Genre Genre), double> heat;
    public SceneAttentionFeedbackSnapshot(IEnumerable<SceneRecruitmentRecord> records, int recruitmentWeek) {
        InputWeek = recruitmentWeek - 1;
        heat = InputWeek < 0 ? new() : SceneDynamicsAttention.Project(records, InputWeek).Evidence
            .Where(e => e.SigningGenre.HasValue)
            .GroupBy(e => (e.PlaceId, e.SigningGenre.Value))
            .ToDictionary(g => g.Key, g => SceneDynamicsAttention.BoundedHeat(g.Sum(e => e.DecayedImpulse)));
    }
    public double Multiplier(string placeId, Genre genre) {
        string scene = LocalScenePersistenceService.SceneIdFor(placeId);
        return scene == null ? 1 : 1 + SceneAttentionFeedback.MaximumBoost * heat.GetValueOrDefault((scene, genre));
    }
}

public readonly record struct SceneFeedbackDiagnostics(int SlateCalls, long BoostedCandidateObservations,
    long BoostedSelectionObservations, int ChangedSlates, double MaximumMultiplier);

public static class SceneAttentionFeedback {
    public const double MaximumBoost = .25; // gameplay prior, not a historical measurement
    private static int cachedWeek = int.MinValue;
    private static SceneAttentionFeedbackSnapshot cached;
    private static int calls, changed;
    private static long candidates, selections;
    private static double maximum = 1;
    public static void Reset() {
        cachedWeek = int.MinValue; cached = null;
        calls = changed = 0; candidates = selections = 0; maximum = 1;
    }
    public static SceneAttentionFeedbackSnapshot Capture(int week) {
        if (!LocalScenes.AttentionFeedback) throw new InvalidOperationException("Live attention feedback is disabled.");
        return CaptureSnapshot(week);
    }
    internal static SceneAttentionFeedbackSnapshot CaptureSnapshot(int week) {
        if (cached == null || cachedWeek != week) {
            cached = new(ArtistManager.Instance.GetAllArtists().SelectMany(a => a.sceneRecruitmentHistory ?? new()), week);
            cachedWeek = week;
        }
        return cached;
    }
    public static void Observe(IEnumerable<double> candidateMultipliers, IEnumerable<double> selectedMultipliers,
        IEnumerable<string> baseline, IEnumerable<string> selected) {
        if (!LocalScenes.AttentionFeedbackAudit) return;
        var values = candidateMultipliers.ToArray();
        calls++; candidates += values.LongCount(v => v > 1);
        selections += selectedMultipliers.LongCount(v => v > 1);
        if (!baseline.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(selected.OrderBy(id => id, StringComparer.Ordinal))) changed++;
        maximum = Math.Max(maximum, values.DefaultIfEmpty(1).Max());
    }
    // Query observations, not unique acts or events; diagnostics never affect decisions.
    public static SceneFeedbackDiagnostics DrainDiagnostics() {
        var result = new SceneFeedbackDiagnostics(calls, candidates, selections, changed, maximum);
        calls = changed = 0; candidates = selections = 0; maximum = 1;
        return result;
    }
}

