// The immutable creative+commercial fingerprint of an externally-originated album -- a film
// soundtrack, a stage-cast recording, or a film-song tie-in. Minted once by ExternalMediaService
// when a label accepts an RFP and never mutated afterward. Its stats are deliberately CORRELATED at
// generation (a high-prestige blockbuster is NOT also a mass-youth smash) so the category cannot
// collapse into a single archetype. See SimTools/D7SoundtrackCastAlbumHandoff.md §3.2.

// The medium a soundtrack was lifted from. The film/cast difference lives entirely here + in the
// demand params -- there is deliberately NO CastAlbum AlbumFormat value (handoff §3.1).
public enum ExternalMediaSourceType {
	FilmScore,  // instrumental/orchestral film score -> Classical / EasyListening
	FilmSong,   // song-driven film (beach/rock pictures) -> RockAndRoll / SurfRock / FolkRock
	StageCast   // Broadway/stage cast recording -> TraditionalPop / Comedy; the multi-year tail case
}

public sealed class ExternalMediaProfile {
	public ExternalMediaSourceType sourceType;

	// --- Demand shape (0..1 unless noted) ---
	public float sourcePopularity;    // launch awareness the film/show arrives with (premiere buzz)
	public float castStarDraw;        // initial momentum from a marquee cast/name
	public float studioPromotion;     // multiplies the label's own marketing spend (~0.8..2.0)
	public float boxOfficeTrajectory; // 0 = flop (dies ~3wk) .. 1 = blockbuster (40-60wk sustain)
	public float awardsPrestige;      // drives the Q1-next-year awards-season resurrection spike
	public float criticalPrestige;    // critical standing; anti-correlated with youthAppeal
	public float youthAppeal;         // pull on Youth/MainstreamAM; anti-correlated with prestige

	// --- Economics ---
	public float licenseSkim;         // studio's cut of gross (0.60..0.80 on blockbuster deals)
	public float upfrontLicenseFee;   // advance paid at acceptance; gates small labels off the big tier

	public bool isBlockbuster;        // rare (0-3/decade cap); the anti-monoculture guardrail lives here
}
