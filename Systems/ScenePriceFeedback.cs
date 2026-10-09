using System;

/// <summary>A separate opt-in, lagged signing-price reader. No change to recorded contracts.</summary>
public static class ScenePriceFeedback {
    public const double MaximumBoost = .10;
    public static float Multiplier(SimulatedArtist artist) {
        if (!LocalScenes.PriceFeedback || artist == null) return 1;
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        var snapshot = SceneAttentionFeedback.CaptureSnapshot(week);
        double heat = (snapshot.Multiplier(artist.geography?.basePlaceId, artist.primaryGenre) - 1)
            / SceneAttentionFeedback.MaximumBoost;
        return (float)(1 + MaximumBoost * heat);
    }
}
