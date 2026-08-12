using Godot;

/// <summary>
/// Phase 4 / design doc c: the break-claim reputation system. Prescience can only be judged
/// retroactively, so this is a two-phase claim/settle:
///   - Claims are STAKED during the playlist decision (StationNetwork.Playlist.cs) when a station
///     moves a still-un-validated, regionally-breaking record into High rotation.
///   - Claims are SETTLED here at the payoff (a genuine national hit that came up through the
///     regions), awarding reputation proportional to how early and how regionally-sourced it was.
/// Reputation feeds reachModifier -> EffectiveReach -> the reporter aggregation, so a good-ear
/// station's picks come to matter more. It NEVER touches radioHeat, chart points or demand.
/// </summary>
public sealed partial class StationNetwork {
	private const float BREAK_CREDIT_TOP10 = 0.05f;
	private const float BREAK_CREDIT_NUMBER_ONE = 0.10f;
	// A record must have genuinely come up through the regions to be "broken" by anyone.
	private const float MIN_REGIONAL_ORIGIN = 0.40f;   // peakRegionalBreakoutStrength gate
	private const int MAX_CLAIM_AGE_WEEKS = 10;        // prescience saturates at a ~10-week early call
	private const float PRESCIENCE_FLOOR = 0.15f;      // even a modest-but-real early call earns something

	/// <summary>
	/// Settle break-claims when a record achieves a national hit. Credit flows ONLY to stations that
	/// committed (High tier) while it was un-validated AND were spinning it in a region where it was
	/// genuinely breaking -- rewards prescience on a regionally-sourced crossover, never participation
	/// in an obvious national smash.
	/// </summary>
	public void CreditStationsOnChartEntry(RecordRuntimeData rec, int week, bool isNumberOne) {
		if (rec?.baseRecord == null) return;

		// GATE 1: was this actually broken from the regions, or manufactured by national push?
		if (rec.peakRegionalBreakoutStrength < MIN_REGIONAL_ORIGIN) return;

		float originQuality = Mathf.Clamp(
			(rec.peakRegionalBreakoutStrength - MIN_REGIONAL_ORIGIN) / (1f - MIN_REGIONAL_ORIGIN), 0f, 1f);
		// Blend in crossover breadth so a broad organic break scores above a single-region fluke.
		originQuality = Mathf.Clamp(originQuality * 0.6f + rec.crossoverCandidateStrength * 0.4f, 0f, 1f);
		if (originQuality <= 0f) return;

		float creditPool = isNumberOne ? BREAK_CREDIT_NUMBER_ONE : BREAK_CREDIT_TOP10;
		string recordId = rec.baseRecord.recordId;

		foreach (RadioStation station in AllStations()) {
			StationRuntime rt = station.rt;
			if (rt == null || !rt.breakClaims.TryGetValue(recordId, out StationRuntime.BreakClaim claim)) continue;
			if (claim.settled) continue;
			claim.settled = true;
			rt.breakClaims[recordId] = claim;   // struct write-back

			// GATE 2: committed while un-validated (re-checked defensively).
			if (claim.chartPosAtFirstHigh != 0 && claim.chartPosAtFirstHigh <= 10) continue;
			// GATE 3: spinning it in a region where it was BREAKING, not just anywhere.
			if (claim.regionalStrengthAtClaim < MIN_REGIONAL_ORIGIN * 0.75f) continue;

			// PRESCIENCE: how early was the commit relative to the payoff?
			int lead = Mathf.Max(0, week - claim.firstHighWeek);
			float prescience = Mathf.Lerp(PRESCIENCE_FLOOR, 1f, Mathf.Clamp(lead / (float)MAX_CLAIM_AGE_WEEKS, 0f, 1f));
			float commitment = Mathf.Clamp(claim.regionalStrengthAtClaim, 0f, 1f);

			float award = creditPool * originQuality * prescience * (0.5f + commitment * 0.5f);
			if (award <= 0f) continue;

			rt.reputation = Mathf.Min(1f, rt.reputation + award);
			rt.reachModifier = 0.85f + rt.reputation * 0.30f;

			// The DJ who broke it shares the acclaim -- makes a good ear a cultivable asset.
			Deejay dj = GetDeejay(station.leadDjId);
			if (dj != null) dj.influence = Mathf.Min(1f, dj.influence + award * 0.5f);
		}
	}

	/// <summary>Prune per-station state for a retired record (keeps the ~63 dictionaries lean over a decade).</summary>
	public void OnRecordRetired(string recordId) {
		if (recordId == null) return;
		foreach (RadioStation s in AllStations()) {
			StationRuntime rt = s.rt;
			if (rt == null) continue;
			rt.breakClaims.Remove(recordId);
			rt.weeksInPlaylist.Remove(recordId);
			rt.droppedOnWeek.Remove(recordId);
			rt.playlist.Remove(recordId);
		}
	}
}
