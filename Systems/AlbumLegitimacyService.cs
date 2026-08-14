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
	/// <summary>Hard zero before this year. Nothing had happened yet that could be leaned on.</summary>
	public const int LegitimacyStartYear = 1964;
	/// <summary>
	/// How far legitimacy may pull the cohesion ceiling forward. The existing curve remains
	/// the floor AND the shape; legitimacy can bring it forward in time, not rewrite it.
	/// </summary>
	public const float MaxCeilingLift = .25f;
	/// <summary>A record has to have actually hung together to count.</summary>
	public const float LandmarkCohesionBar = .72f;
	/// <summary>
	/// ...and its merit has to have reached somebody. Recognition, not chart position:
	/// the bar is deliberately stated against <see cref="CulturalRecognitionService"/> so
	/// that a press channel can clear it for a record that never charted at all.
	/// </summary>
	public const float LandmarkRecognitionBar = .45f;
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

	public static bool IsLandmark(int year, float thematicCohesion, float recognition) =>
		year >= LegitimacyStartYear && thematicCohesion >= LandmarkCohesionBar && recognition >= LandmarkRecognitionBar;

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
		// Cheapest gate first: cohesion is fixed at pressing and most albums fail it outright,
		// so the weekly offer costs one float compare for the great majority of the chart.
		if (year < LegitimacyStartYear || album.thematicCohesion < LandmarkCohesionBar) return;

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
