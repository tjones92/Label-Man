using Godot;

// Publishing & Cover-Song §15 -- AlbumMaterialPlan. Gives an album a COHERENT whole-LP material strategy
// instead of rolling every track independently: a high-cohesion statement record concentrates on one
// source (late rock/folk -> artist-written; early pop -> professional/standards), while a hits-and-filler
// record stays spread. Built from the same calibrated Anchor1960/Anchor1969 mix the singles follow, so the
// aggregate decade curve is preserved; cohesion only reshapes WITHIN an album.
//
// Economically inert: non-single album tracks never settle (only the album RECORD and its promo single
// route publishing), so this changes track biographies/telemetry, not the goldmine economics. Deterministic
// (stable hash, no GD) -- it consumes no RNG, so the album build's RNG schedule is unchanged.

/// <summary>The per-LP material strategy: how many tracks come from each source, how cohesive the record
/// is, and which source frames the lead single.</summary>
public struct AlbumMaterialPlan {
	public int Originals;       // artist-written
	public int Professional;    // staff / external-professional
	public int Covers;          // recent-hit covers
	public int Standards;       // pre-game standards
	public int Traditional;     // public-domain / traditional
	public float Cohesion;
	public SongMaterialSource LeadSingleSource;

	public int TotalPlanned => Originals + Professional + Covers + Standards + Traditional;
}

public static class AlbumMaterialPlanner {
	public static bool Enabled = true;

	// How hard maximum cohesion concentrates the LP onto its dominant source. Below 1 so even a full
	// statement album keeps a little variety (a stray cover), which is realistic.
	private const float MaxConcentration = 0.70f;

	/// <summary>Build an album's material plan: the genre/year source prior, pulled toward the dominant
	/// source in proportion to the album's thematic cohesion, allocated across <paramref name="trackCount"/>
	/// slots by largest-remainder (deterministic).</summary>
	public static AlbumMaterialPlan Plan(Genre genre, int year, float thematicCohesion, int trackCount) {
		SourceShares s = SongMaterialSelectionService.GetSourceMixShares(genre, year);
		// Order: 0 originals, 1 professional, 2 standards, 3 covers(recent-hit), 4 traditional.
		float[] share = { s.Aw, s.Pro, s.Std, s.Hit, s.Trad };
		int dom = 0;
		for (int i = 1; i < share.Length; i++) if (share[i] > share[dom]) dom = i;

		float pull = Mathf.Clamp(thematicCohesion, 0f, 1f) * MaxConcentration;
		float sum = 0f;
		var conc = new float[share.Length];
		for (int i = 0; i < share.Length; i++) { conc[i] = Mathf.Lerp(share[i], i == dom ? 1f : 0f, pull); sum += conc[i]; }
		if (sum <= 0f) { conc[dom] = 1f; sum = 1f; }
		for (int i = 0; i < share.Length; i++) conc[i] /= sum;

		int[] counts = LargestRemainder(conc, Mathf.Max(0, trackCount));
		return new AlbumMaterialPlan {
			Originals = counts[0], Professional = counts[1], Standards = counts[2],
			Covers = counts[3], Traditional = counts[4],
			Cohesion = Mathf.Clamp(thematicCohesion, 0f, 1f),
			LeadSingleSource = SlotSource(dom)
		};
	}

	/// <summary>Expand a plan into a per-slot source list, deterministically ordered by a stable hash so
	/// the same sources are not always front-loaded (replay-stable, no GD).</summary>
	public static SongMaterialSource[] ExpandSlots(AlbumMaterialPlan plan, string artistId, string albumKey) {
		int n = plan.TotalPlanned;
		var slots = new SongMaterialSource[n];
		int w = 0;
		void Fill(int count, int sourceIndex) { for (int i = 0; i < count; i++) slots[w++] = SlotSource(sourceIndex); }
		Fill(plan.Originals, 0); Fill(plan.Professional, 1); Fill(plan.Standards, 2);
		Fill(plan.Covers, 3); Fill(plan.Traditional, 4);
		// Deterministic shuffle: sort indices by a stable hash of (artist|album|slot).
		var order = new int[n];
		for (int i = 0; i < n; i++) order[i] = i;
		System.Array.Sort(order, (a, b) => StableKey(artistId, albumKey, a).CompareTo(StableKey(artistId, albumKey, b)));
		var shuffled = new SongMaterialSource[n];
		for (int i = 0; i < n; i++) shuffled[i] = slots[order[i]];
		return shuffled;
	}

	private static SongMaterialSource SlotSource(int i) => i switch {
		0 => SongMaterialSource.ArtistWritten,
		1 => SongMaterialSource.ExternalProfessional,
		2 => SongMaterialSource.CoverStandard,
		3 => SongMaterialSource.CoverRecentHit,
		_ => SongMaterialSource.TraditionalPublicDomain
	};

	// Largest-remainder apportionment of `total` slots across shares (sums exactly to total).
	private static int[] LargestRemainder(float[] shares, int total) {
		var counts = new int[shares.Length];
		if (total <= 0) return counts;
		var frac = new float[shares.Length];
		int assigned = 0;
		for (int i = 0; i < shares.Length; i++) {
			float exact = shares[i] * total;
			counts[i] = (int)exact;
			frac[i] = exact - counts[i];
			assigned += counts[i];
		}
		// Hand out the remaining slots to the largest fractional remainders.
		for (int r = assigned; r < total; r++) {
			int best = 0;
			for (int i = 1; i < frac.Length; i++) if (frac[i] > frac[best]) best = i;
			counts[best]++;
			frac[best] = -1f;
		}
		return counts;
	}

	private static uint StableKey(string artistId, string albumKey, int slot) {
		uint hash = 2166136261u;
		foreach (char c in $"{artistId}|{albumKey}|{slot}|AlbumPlanV1") { hash ^= c; hash *= 16777619u; }
		return hash;
	}
}
