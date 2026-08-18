using Godot;

/// <summary>
/// The act-level recognition stock: how it is earned at the seams that already fire once
/// per milestone per record, how it decays on the calendar, and the bounded, quality-gated
/// launch profile it would contribute once consumption is switched on.
/// <para>
/// SimTools/CelebrityRecognitionDirective.md. In Phase A this service is WRITE-ONLY at the
/// economy: milestone gains, run-complete gain, standing writers and decay all run under
/// <see cref="ArtistRecognition.Observing"/>, but <see cref="GetLaunchProfile"/> is consumed
/// by nothing. Ceilings and rates are placeholders; the structure is the commitment --
/// milestone gains, diminishing returns, slow calendar decay, standing separated from
/// recognition.
/// </para>
/// </summary>
public static class ArtistRecognitionService {
	// ---- launch ceilings (Phase B+ consumption; computed for audit in Phase A) ----------------
	// Intentionally modest: recognition improves the odds people hear about and can find a
	// release; it never overrides campaign, distribution, genre fit and quality combined. The
	// stock bonus REPLACES the discrete 2.5x careerState switch -- it does not augment it.
	public const float MaxAwarenessLift = 0.115f;
	public const float MaxStockBonus    = 0.30f;
	public const float MaxRadioLift     = 0.075f;

	// ---- milestone gains, credited once per record per milestone at the existing latches ------
	public const float ChartEntryGain = 0.007f;
	public const float Top40Gain      = 0.022f;
	public const float Top10Gain      = 0.045f;
	public const float NumberOneGain  = 0.085f;

	// ---- standing writers ---------------------------------------------------------------------
	public const float LandmarkStandingGain    = 0.12f;   // culturalStanding per unit landmark strength
	public const float LandmarkRecognitionGain = 0.025f;  // a smaller mass-recognition deposit
	public const float InfluenceStandingGain   = 0.008f;  // per unit influence strength, on the SOURCE act

	// ---- calendar decay, per week (not per release) -------------------------------------------
	// A legacy act fades from the mass market even if it never releases again -- the whole point
	// of a stock that is not careerState. Standing is nearly permanent.
	public const float PublicRecognitionDecay = 0.9975f;  // half-life ~277 weeks (~5.3y)
	public const float CulturalStandingDecay  = 0.9992f;  // half-life ~866 weeks

	/// <summary>Cultural standing is not mass awareness, so it contributes only a fraction at launch.</summary>
	public const float StandingLaunchWeight = 0.20f;
	/// <summary>
	/// §8.3: an accomplished maker lifts the act's standing even when they are not the public face
	/// -- the George Martin / Brian Wilson case, where a record launches partly on the maker's
	/// reputation. Small: it is a standing-side nudge, not a second awareness term. Its live reader
	/// is what keeps creativeReputation causal rather than a cosmetic accumulator.
	/// </summary>
	public const float MakerStandingWeight = 0.15f;

	/// <summary>The strongest creative reputation in the current lineup: the act's best maker.</summary>
	private static float BestMakerReputation(SimulatedArtist artist) {
		float best = 0f;
		if (artist?.members == null) return 0f;
		foreach (Musician m in artist.members)
			if (m != null && m.isActive && m.creativeReputation > best) best = m.creativeReputation;
		return best;
	}

	/// <summary>The mass-familiarity value a launch would read: recognition, a slice of standing, and the maker's name.</summary>
	public static float EffectiveRecognition(SimulatedArtist artist) =>
		artist == null ? 0f : Mathf.Clamp(artist.publicRecognition + artist.culturalStanding * StandingLaunchWeight
			+ BestMakerReputation(artist) * MakerStandingWeight, 0f, 1f);

	// ---- gains, with diminishing returns on every deposit --------------------------------------
	// The anti-monopoly core: a #1 does not mint a permanent franchise. Each gain buys less the
	// higher the stock already is, so recognition saturates rather than compounds.
	public static void AddPublicRecognition(SimulatedArtist artist, float rawGain) {
		if (!ArtistRecognition.Observing || artist == null || rawGain <= 0f) return;
		float before = artist.publicRecognition;
		artist.publicRecognition = Mathf.Clamp(before + rawGain * (1f - before), 0f, 1f);
		// Phase E-lite: the act's realized gain reads down to the current lineup.
		MusicianRecognitionService.ShareArtistRecognitionGain(artist, artist.publicRecognition - before);
	}

	public static void AddCulturalStanding(SimulatedArtist artist, float rawGain) {
		if (!ArtistRecognition.Observing || artist == null || rawGain <= 0f) return;
		artist.culturalStanding = Mathf.Clamp(artist.culturalStanding + rawGain * (1f - artist.culturalStanding), 0f, 1f);
	}

	// ---- write-back seams ----------------------------------------------------------------------
	// Milestone credit hangs beside the existing Register* calls, on the same once-per-record
	// latch, so a slot occupied for many weeks is credited once and there is no per-week
	// rich-get-richer term.
	public static void OnChartEntry(SimulatedArtist artist) => AddPublicRecognition(artist, ChartEntryGain);
	public static void OnTop40(SimulatedArtist artist)      => AddPublicRecognition(artist, Top40Gain);
	public static void OnTop10(SimulatedArtist artist)      => AddPublicRecognition(artist, Top10Gain);
	public static void OnNumberOne(SimulatedArtist artist)  => AddPublicRecognition(artist, NumberOneGain);

	/// <summary>
	/// Credited once at run completion from facts already on the record, and deliberately keyed
	/// to SUSTAIN and BREADTH (weeks on chart, weeks in the top ten, regional breadth) rather
	/// than to peak position -- peak is already paid by the milestone gains, so this cannot
	/// double-count them. This is the Temptations tier: three or four solid runs accumulate into
	/// durable recognition without any single one being iconic.
	/// </summary>
	public static void OnChartRunComplete(SimulatedArtist artist, RecordRuntimeData record) {
		if (!ArtistRecognition.Observing || artist == null || record == null) return;
		if (record.peakPosition <= 0 || record.peakPosition > 100) return;   // never charted -> no sustain to reward
		float sustain = 0.020f * Mathf.Clamp(record.weeksOnChart / 40f, 0f, 1f);
		float topTen  = 0.030f * Mathf.Clamp(record.weeksInTopTen / 12f, 0f, 1f);
		float breadth = 0.020f * Mathf.Clamp(record.regionalBreakoutCount / 6f, 0f, 1f);
		AddPublicRecognition(artist, sustain + topTen + breadth);
	}

	// ---- calendar decay ------------------------------------------------------------------------
	/// <summary>
	/// One idempotent weekly pass over the registry. The per-artist week guard makes a second
	/// call in the same week a no-op, so it is safe on both the weekly boundary and any live-tick
	/// reconciliation. Draws no RNG.
	/// </summary>
	public static void DecayRegistryForWeek(int week) {
		if (!ArtistRecognition.Observing) return;
		ArtistManager artists = ArtistManager.Instance;
		if (artists == null) return;
		foreach (SimulatedArtist artist in artists.GetAllArtists()) {
			if (artist == null || artist.recognitionLastUpdatedWeek == week) continue;
			artist.recognitionLastUpdatedWeek = week;
			if (artist.publicRecognition > 0f) artist.publicRecognition *= PublicRecognitionDecay;
			if (artist.culturalStanding > 0f) artist.culturalStanding *= CulturalStandingDecay;
		}
	}

	// ---- launch profile (Phase A: audited, not consumed) --------------------------------------
	/// <summary>
	/// The bounded lifts recognition contributes to a launch. Awareness is live in Phase B (as a
	/// national scalar, region-weighted downstream by the same regionStrength the base awareness
	/// pays); stock and radio are computed for audit but consumed only in Phases C and D. A
	/// per-record quality gate and the softer region curve (directive §3) fold in with the
	/// per-region stock/radio plumbing of those phases.
	/// </summary>
	public static ArtistRecognitionLaunchProfile GetLaunchProfile(SimulatedArtist artist, RecordRuntimeData runtime) {
		if (artist == null) return default;
		float effective = EffectiveRecognition(artist);
		return new ArtistRecognitionLaunchProfile(
			artist.publicRecognition, artist.culturalStanding, effective,
			effective * MaxAwarenessLift,
			1f + effective * MaxStockBonus,
			effective * MaxRadioLift);
	}

	/// <summary>
	/// Snapshot the act's recognition at release and stamp the record's audit block. Write-only:
	/// it never mutates a demand quantity. This is what lets the A/B read chart-slot-weeks by
	/// launch-recognition decile against the launchCareerState control.
	/// </summary>
	public static void RecordLaunchAudit(SimulatedArtist artist, RecordRuntimeData runtime) {
		if (!ArtistRecognition.Observing || artist == null || runtime == null) return;
		artist.recognitionAtLastRelease = artist.publicRecognition;
		ArtistRecognitionLaunchProfile profile = GetLaunchProfile(artist, runtime);
		runtime.launchArtistRecognition = profile.PublicRecognition;
		runtime.launchCulturalStanding = profile.CulturalStanding;
		runtime.launchEffectiveRecognition = profile.EffectiveRecognition;
		runtime.launchRecognitionAwarenessLift = profile.AwarenessLift;
		runtime.launchRecognitionStockMultiplier = profile.StockMultiplier;
		runtime.launchRecognitionRadioLift = profile.RadioLift;
	}
}

/// <summary>The bounded launch lifts recognition contributes, computed once per release.</summary>
public readonly struct ArtistRecognitionLaunchProfile {
	public readonly float PublicRecognition;
	public readonly float CulturalStanding;
	public readonly float EffectiveRecognition;
	public readonly float AwarenessLift;
	public readonly float StockMultiplier;
	public readonly float RadioLift;

	public ArtistRecognitionLaunchProfile(float publicRecognition, float culturalStanding, float effectiveRecognition,
		float awarenessLift, float stockMultiplier, float radioLift) {
		PublicRecognition = publicRecognition;
		CulturalStanding = culturalStanding;
		EffectiveRecognition = effectiveRecognition;
		AwarenessLift = awarenessLift;
		StockMultiplier = stockMultiplier;
		RadioLift = radioLift;
	}
}
