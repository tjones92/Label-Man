using Godot;

/// <summary>
/// The fog of war for talent scouting. Converts an artist's TRUE base quality into what a given
/// label PERCEIVES, with error inversely correlated to <see cref="AILabel.scoutingAbility"/>.
///
/// Reuses the same noise band already used for release perception in ChartManager.ReleaseRecord
/// (Lerp(0.30, 0.10, scoutingAbility)), so a label misjudges artists and records consistently:
/// a bad scout is bad at both.
///
/// DETERMINISM: this is a PURE stable-hash function, never a GD.RandRange call. RosterManager's
/// daily talent market is obsessively deterministic (byte-identical replays, preserved RNG call
/// order). A random draw here would desync every replay and shift the RNG schedule. The read is
/// stable per (label, artist, discovery-window): the same scout gets the same read of the same
/// artist within a 4-week window, so re-evaluation cannot launder the fog. The window advances
/// (DiscoveryRefreshWindowWeeks), so a scout revisiting a region gets a fresh (still noisy) read
/// over time - a good scout's reads converge on truth, a bad scout's stay scattered.
///
/// SCOPE: fog only the LIVE daily/monthly-market read sites. The frozen launch-roster allocation
/// (PopulateInitialRoster -> ScoreArtistForLabel) stays omniscient - fogging it would reshape the
/// seeded 1960 industry and break calibration.
/// </summary>
public static class ScoutingPerception {
	// Matches ChartManager.ReleaseRecord's release-perception band.
	private const float NoiseMax = 0.30f;   // scoutingAbility 0 -> +/-0.30 error
	private const float NoiseMin = 0.10f;   // scoutingAbility 1 -> +/-0.10 error

	/// <summary>
	/// What <paramref name="label"/> perceives <paramref name="artist"/>'s latent quality to be.
	/// Only the latent talent (CalculateBaseQuality) is fogged; an artist's observable career
	/// record (momentum, reputation - public chart knowledge) is read without fog by callers.
	/// </summary>
	public static float PerceivedQuality(SimulatedArtist artist, AILabel label, int discoveryWindow) {
		float trueQuality = artist.CalculateBaseQuality();
		float band = PerceptionBand(label);
		// Stable signed offset in [-band, +band], deterministic per (label, artist, window).
		float unit = StableUnit(label.labelId, artist.artistId, discoveryWindow);   // [0,1)
		float offset = (unit * 2f - 1f) * band;
		return Mathf.Clamp(trueQuality + offset, 0f, 1f);
	}

	/// <summary>
	/// The bracket the label's read sits inside, for the player-facing progressive reveal:
	/// a wide range for a poorly-scouted artist, a tight one for a well-scouted one.
	/// </summary>
	public static (float Low, float High) PerceivedRange(SimulatedArtist artist, AILabel label, int discoveryWindow) {
		float perceived = PerceivedQuality(artist, label, discoveryWindow);
		float band = PerceptionBand(label);
		return (Mathf.Clamp(perceived - band, 0f, 1f), Mathf.Clamp(perceived + band, 0f, 1f));
	}

	/// <summary>How confident the label should be in its read - drives whether it "scouts deeper".</summary>
	public static float PerceptionConfidence(AILabel label) => Mathf.Clamp(label.scoutingAbility, 0f, 1f);

	private static float PerceptionBand(AILabel label) =>
		Mathf.Lerp(NoiseMax, NoiseMin, Mathf.Clamp(label.scoutingAbility, 0f, 1f));

	/// <summary>
	/// FNV-1a over (label, artist, window) with a perception salt, folded to [0,1). Mirrors
	/// RosterManager.GetStableDiscoveryKey's hashing style; the distinct salt keeps the perception
	/// draw statistically independent of the discovery-slate ordering that key drives.
	/// </summary>
	private static float StableUnit(string labelId, string artistId, int window) {
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		ulong hash = offset;
		foreach (char value in $"{labelId}|{artistId}|{window}|ScoutPerceptionV1") { hash ^= value; hash *= prime; }
		// Top 24 bits -> [0,1). 2^24 = 16777216.
		return (hash >> 40) * (1f / 16777216f);
	}
}
