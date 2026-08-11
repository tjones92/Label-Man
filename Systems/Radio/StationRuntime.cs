using System.Collections.Generic;

public enum SpinTier { None, Light, Mid, High }

/// <summary>
/// All mutable per-station state (design doc b). Lives on the ~63 station objects, NOT on the
/// ~3,500 RegionalRecordData instances - the many-to-many playlist relationship is materialized
/// only station->record (sparse), never record->station.
///
/// PHASE NOTE: break-claim tracking (design doc c) is added in Phase 4; the base rotation +
/// relationship + reputation state is here from Phase 1 so roster generation and the aggregation
/// have somewhere to hang their weather.
/// </summary>
public sealed class StationRuntime {
	// recordId -> current tier. Sparse: only records this station actually plays (~35 entries).
	public readonly Dictionary<string, SpinTier> playlist = new(System.StringComparer.Ordinal);
	// recordId -> weeks the record has been in THIS station's rotation (drives burn).
	public readonly Dictionary<string, int> weeksInPlaylist = new(System.StringComparer.Ordinal);
	// recordId -> week this station last dropped it (drives re-add hysteresis).
	public readonly Dictionary<string, int> droppedOnWeek = new(System.StringComparer.Ordinal);

	// Relationships (the cultivation surface).
	public readonly Dictionary<string, float> labelRapport = new(System.StringComparer.Ordinal);
	public readonly Dictionary<string, float> artistLoyalty = new(System.StringComparer.Ordinal);

	// Lifecycle reputation.
	public float reputation = 0.5f;
	public float reachModifier = 1f;   // derived from reputation; multiplies effective reach
	public float scandalHeat;          // payola exposure, decays

	public float Rapport(string labelId) =>
		labelId != null && labelRapport.TryGetValue(labelId, out float v) ? v : 0f;
	public float Loyalty(string artistId) =>
		artistId != null && artistLoyalty.TryGetValue(artistId, out float v) ? v : 0f;
	public SpinTier TierOf(string recordId) =>
		recordId != null && playlist.TryGetValue(recordId, out SpinTier t) ? t : SpinTier.None;
}
