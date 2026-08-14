using Godot;

/// <summary>
/// The album-as-art movement, built as an emergent loop rather than an object. There is no
/// milestone registry and no "art album" type: a landmark record is simply an album with
/// real merit that reached people. The movement is the KNOCK-ON -- the exogenous cohesion
/// ramp becomes partly earned by records that actually happened, and peers who were paying
/// attention remember it.
/// <para>
/// This file owns one bounded global scalar and the rule for what counts. It owns neither
/// the judgement of quality (<see cref="ArtisticMeritService"/>) nor the judgement of
/// reach (<see cref="CulturalRecognitionService"/>) nor the ledger other acts read from
/// (<see cref="CulturalMemoryService"/>). If the loop never starts, the exogenous ramp
/// carries the decade exactly as it does today, which is the phase's own gate: legitimacy
/// pinned at 0 must reproduce the previous phase.
/// </para>
/// </summary>
public static class AlbumLegitimacyService {
	/// <summary>
	/// Landmarks are possible from the start of the simulation. This was 1964, which was
	/// wrong: the album as a serious form was established by jazz well before pop reached for
	/// it — Giant Steps is 1960 — and those records are exactly what made the LP a thing a
	/// rock act could take seriously five years later.
	/// <para>
	/// That chain is now emergent rather than authored: early landmarks accumulate
	/// <see cref="Legitimacy"/>, legitimacy lifts the cohesion ceiling, and the lifted ceiling
	/// is what lets mid-decade pop records become statements. Nothing needs to know the order
	/// in advance. Rarity, not a year gate, is what keeps the early ones scarce.
	/// </para>
	/// </summary>
	public const int LegitimacyStartYear = 1960;
	/// <summary>
	/// How far legitimacy may pull the cohesion ceiling forward. The existing curve remains
	/// the floor AND the shape; legitimacy can bring it forward in time, not rewrite it.
	/// </summary>
	public const float MaxCeilingLift = .25f;
	/// <summary>
	/// A record has to have been an ALBUM to count -- a body of work rather than a smattering
	/// of singles with other songs around it.
	/// <para>
	/// Stated against <see cref="AlbumModel.GetAlbumIntegrity"/> and NOT against
	/// <see cref="Album.thematicCohesion"/>, because thematic cohesion is the concept-album
	/// axis and a landmark album is not the same thing as a concept album -- neither Rubber
	/// Soul nor Pet Sounds was one. Cohesion is also gated by the era ceiling, which pins it
	/// to its 0.08 clamp floor for every artist-made album before 1966; a rule stated against
	/// it therefore could not fire until 1967 no matter how good the record was.
	/// </para>
	/// </summary>
	/// <para>
	/// Sized against the measured distribution, not by intuition. Track qualities on one album
	/// cluster tightly (sd .045), so the mean-against-peak ratio sits at .823 for an ordinary
	/// record: a .72 bar admitted 98% of eligible albums and minted 347 landmarks in two
	/// years. .92 is roughly the 99.3rd percentile.
	/// </para>
	public const float LandmarkIntegrityBar = .92f;
	/// <summary>
	/// A body of work also has to be GOOD. Integrity alone says the record is consistent, not
	/// that it is worth consistently listening to — a uniformly mediocre album scores well on
	/// a ratio.
	/// <para>
	/// TARGET, and these two bars exist to hit it: 25-40 landmarks across the WHOLE DECADE.
	/// Roughly a handful of jazz records before 1965, then three or four a year — 1965 Rubber
	/// Soul and Highway 61; 1966 Revolver, Pet Sounds, Blonde on Blonde; 1967 Sgt. Pepper,
	/// Are You Experienced, The Doors, the Velvet Underground; and so on. Plenty of other
	/// albums are cohesive, ambitious and well reviewed. Almost none of them are landmarks,
	/// and the gap between those two populations is the whole point of the bar.
	/// </para>
	/// </summary>
	public const float LandmarkMeritBar = .78f;
	/// <summary>
	/// And it has to have been doing something. This exists because the merit gate is NOT
	/// independent of the integrity gate the way it was supposed to be: body-of-work feeds
	/// `GetCraft`'s coherence term at 35%, so a consistent record scores well on merit partly
	/// for being consistent — the two gates were reading the same fact twice.
	/// <para>
	/// Originality is the one axis nothing else here derives from, which makes it the gate
	/// that actually distinguishes a landmark from a competently uniform record.
	/// </para>
	/// </summary>
	public const float LandmarkOriginalityBar = .70f;
	/// <summary>
	/// Formats that can be one. A compilation is assembled from sides that already existed and
	/// a live record documents them; neither is a new body of work. Soundtracks are excluded
	/// as a class -- they sit with comedy, children's and classical as an odd entity whose
	/// cultural weight, real as it sometimes was, is not the album-as-art movement.
	/// </summary>
	public static bool IsEligibleFormat(AlbumFormat format) =>
		format is AlbumFormat.Standard or AlbumFormat.Concept;

	/// <summary>
	/// Families that can produce one. Comedy, children's records and classical are the same
	/// odd entity as the soundtrack: they sell as albums, they are sometimes culturally large,
	/// and they are not participants in the album-as-art movement this loop models.
	/// <para>
	/// Measured consequence of not having this: SIX of the first twelve landmarks a run
	/// produced were children's records. Novelty and children's material is uniformly pitched
	/// by construction, and a body-of-work reading is a consistency ratio, so it rates them
	/// highly for exactly the wrong reason.
	/// </para>
	/// </summary>
	public static bool IsEligibleFamily(GenreFamily family) =>
		family is not (GenreFamily.NonMusic or GenreFamily.Classical);
	/// <summary>
	/// ...and its merit has to have reached somebody. Recognition, not chart position:
	/// the bar is deliberately stated against <see cref="CulturalRecognitionService"/> so
	/// that a press channel can clear it for a record that never charted at all.
	/// </summary>
	public const float LandmarkRecognitionBar = .45f;
	/// <summary>
	/// Sized so ~30 landmarks across a decade carry legitimacy to roughly 0.6-0.8 by 1969 —
	/// a movement that builds and is nearly complete, never one that arrives complete.
	/// <para>
	/// This value briefly went to .02 while the integrity bar was admitting 347 landmarks in
	/// two years and saturating legitimacy at 1.0 by 1965. That was the bar being wrong, not
	/// this rate; with the bar fixed the original value is right. The loop is self-limiting
	/// anyway — earliness is `1 − Legitimacy`, so each landmark is worth less than the last.
	/// </para>
	/// </summary>
	public const float ContributionPerLandmark = .06f;
	/// <summary>
	/// A landmark still counts for something once the movement is everywhere -- less, but
	/// never nothing. The floor exists because the alternative measured out at exactly zero.
	/// </summary>
	public const float MinimumEarliness = .15f;

	public static float Legitimacy { get; private set; }
	public static long LandmarkCount { get; private set; }

	/// <summary>
	/// How much of the earliness premium a landmark still carries. Measured against the
	/// MOVEMENT, not the calendar.
	/// <para>
	/// This was a year ramp expiring in 1969, and it was anti-phased with the thing it was
	/// scoring. The cohesion ceiling does not admit statement albums in volume until 1967
	/// (albums over the bar ran 32, 33, 564, 1441, 1610 across 1965-69), while the calendar
	/// premium paid 0.8 in 1965 and exactly 0.0 in 1969 -- so the decade's 136 landmark
	/// albums of 1969 were worth literally nothing and 1968's averaged 0.13. The premium was
	/// being spent in the years that structurally could not produce the record it was meant
	/// to reward.
	/// </para>
	/// <para>
	/// Against legitimacy it self-scales: the first act to make one of these is early
	/// whenever they do it, and the premium decays as the movement actually happens rather
	/// than as the calendar advances. It is also the same quantity that lifts the cohesion
	/// ceiling, so the two move together -- getting easier to make and less remarkable to
	/// have made are one process.
	/// </para>
	/// </summary>
	public static float GetEarliness(int year) => year < LegitimacyStartYear ? 0f :
		Mathf.Max(MinimumEarliness, 1f - Mathf.Clamp(Legitimacy, 0f, 1f));

	public static bool IsLandmark(int year, float bodyOfWork, float merit, float recognition) =>
		year >= LegitimacyStartYear && bodyOfWork >= LandmarkIntegrityBar &&
		merit >= LandmarkMeritBar && recognition >= LandmarkRecognitionBar;

	/// <summary>
	/// The era term with legitimacy applied. Clamped to at most <see cref="MaxCeilingLift"/>
	/// above the exogenous curve and never below it, so the authored ramp is a floor.
	/// </summary>
	public static float ApplyToEraTerm(float exogenousEra, float legitimacy) =>
		Mathf.Clamp(exogenousEra * (1f + MaxCeilingLift * Mathf.Clamp(legitimacy, 0f, 1f)),
			exogenousEra, exogenousEra * (1f + MaxCeilingLift));

	/// <summary>Reads the live global so <see cref="AlbumModel"/> stays free of feature switches.</summary>
	public static float CurrentCeilingMultiplier =>
		ArtistEvolution.AlbumLegitimacyEnabled ? 1f + MaxCeilingLift * Mathf.Clamp(Legitimacy, 0f, 1f) : 1f;

	/// <summary>
	/// Offered every week the album is on the chart, and it publishes at most once -- at the
	/// moment the record is actually RECOGNISED, which is while it is climbing.
	/// <para>
	/// This used to hang off the chart-run-COMPLETE hook, i.e. retirement, which for an album
	/// is roughly 94 weeks after release (a ~42-week chart life plus a 52-week tolerance).
	/// Sgt. Pepper was a landmark within weeks and the acts who answered it did so that
	/// summer, not two years later. Worse, the latency made the whole channel unobservable:
	/// across a full decade run not one album's completion hook ever fired in time to matter,
	/// so the album-as-art loop had never once executed.
	/// </para>
	/// <para>
	/// Reception is read as RECOGNITION rather than as chart position, which is the modular
	/// seam: today recognition comes from the chart and from the act's own standing, and when
	/// the trade press exists it will come from there too. A record the critics carried and
	/// the public ignored becomes a landmark through the same door, with nothing here changed.
	/// </para>
	/// </summary>
	public static void OnAlbumChartWeek(SimulatedArtist artist, RecordRuntimeData record, int year) =>
		TryPublishLandmark(artist, record, year);

	/// <summary>
	/// Last chance, at retirement, for an album that never charted. Its merit is unchanged;
	/// only a press deposit could have given it the standing to qualify, which is exactly the
	/// case this call exists to keep reachable once journalism is writing.
	/// </summary>
	public static void OnAlbumChartRunComplete(SimulatedArtist artist, RecordRuntimeData record, AILabel label, int year) =>
		TryPublishLandmark(artist, record, year);

	private static void TryPublishLandmark(SimulatedArtist artist, RecordRuntimeData record, int year) {
		if (!ArtistEvolution.AlbumLegitimacyEnabled || artist == null || record == null) return;
		if (record.landmarkPublished) return;
		Album album = record.baseRecord?.album;
		if (album == null || record.baseRecord.format != ReleaseFormat.Album) return;
		// Cheapest gates first: both are fixed at pressing and most albums fail them outright,
		// so the weekly offer costs a compare and a float test for the great majority of the
		// chart. Note that having singles on it is NOT disqualifying -- Rubber Soul and Pet
		// Sounds both carried them. Integrity asks whether the REST of the record stands up.
		if (year < LegitimacyStartYear || !IsEligibleFormat(album.albumFormat)) return;
		if (!IsEligibleFamily(GenreCatalog.Get(GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, year)).Family)) return;
		if (album.bodyOfWork < LandmarkIntegrityBar || album.artisticMerit < LandmarkMeritBar) return;
		if (record.baseRecord.originality < LandmarkOriginalityBar) return;

		(float recognition, _) = CulturalRecognitionService.Consume(record.baseRecord.recordId,
			record.peakPosition, Mathf.Max(artist.reputation, artist.criticalAcclaim));
		if (recognition < LandmarkRecognitionBar) return;

		record.landmarkPublished = true;
		float merit = album.artisticMerit;
		float strength = Mathf.Clamp(merit * recognition * GetEarliness(year) *
			CulturalMemoryService.LandmarkInfluenceWeight, 0f, 1f);
		Legitimacy = Mathf.Clamp(Legitimacy + ContributionPerLandmark * strength, 0f, 1f);
		LandmarkCount++;
		CulturalMemoryService.Publish(artist.artistId, artist.labelId,
			GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, year), year,
			CulturalEventType.LandmarkAlbum, merit, recognition, strength);
	}

	internal static void ResetForProbe() {
		Legitimacy = 0f;
		LandmarkCount = 0;
	}

	internal static void SetLegitimacyForProbe(float value) => Legitimacy = Mathf.Clamp(value, 0f, 1f);
}
