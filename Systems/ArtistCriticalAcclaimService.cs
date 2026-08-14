using Godot;

/// <summary>
/// Gives <see cref="SimulatedArtist.criticalAcclaim"/> a writer. Until this existed the
/// field was declared and referenced nowhere: always 0f, so any formula multiplying by
/// it was multiplying by zero.
/// <para>
/// Deliberately READ-ONLY TO THE ECONOMY. Acclaim touches no units, no advance, no
/// signing decision and no chart point. It is a narrative and pressure signal, and the
/// gate for the phase that introduces it is that the economy is byte-comparable. Whether
/// an acclaimed act eventually earns better rooms and better terms -- historically it did --
/// is a real economic edge and belongs to its own directive with its own gates.
/// </para>
/// </summary>
public static class ArtistCriticalAcclaimService {
	/// <summary>
	/// A reputation with critics is sticky but not permanent. Applied per completed chart
	/// run rather than per week, so an act that stops releasing holds its standing and an
	/// act releasing constantly is judged constantly.
	/// </summary>
	public const float RetentionPerRelease = .94f;
	/// <summary>
	/// The bar a record clears before it reads as a critical event at all. Owned by
	/// <see cref="ArtisticMeritService"/> now that merit has its own home; kept as an alias
	/// so there is exactly one number and callers need not know which file it lives in.
	/// </summary>
	public const float CraftBar = ArtisticMeritService.MeritBar;
	/// <summary>
	/// The acclaimed-but-didn't-sell case is the interesting one and the one the model had
	/// no way to express. A high-craft record that missed commercially earns MORE critical
	/// standing than the same record that sold, which is the Pet Sounds shape.
	/// </summary>
	public const float UnderratedBonus = .45f;
	/// <summary>
	/// The cap on what CRAFT alone earns from one record. The underrated bonus rides on top
	/// of it, so the true per-release ceiling is <see cref="MaxTotalGainPerRelease"/> -- if
	/// the cap were applied after the bonus it would clip the bonus to nothing at exactly the
	/// high-craft records the bonus exists to distinguish.
	/// </summary>
	public const float MaxGainPerRelease = .22f;
	public const float MaxTotalGainPerRelease = MaxGainPerRelease * (1f + UnderratedBonus);

	/// <summary>
	/// Craft as the trade press would have heard it. Delegates to the merit layer so the
	/// critics and the landmark rule are demonstrably reading the same record.
	/// </summary>
	public static float GetCraftScore(float originality, float productionQuality, float thematicCohesion,
		bool isAlbum, float labelProductionQuality) =>
		ArtisticMeritService.GetCraft(originality, productionQuality, thematicCohesion, isAlbum, labelProductionQuality);

	/// <summary>How loudly the public answered. 0 for a record that never charted.</summary>
	public static float GetCommercialScore(int peakPosition) =>
		CulturalRecognitionService.GetCommercialRecognition(peakPosition);

	/// <summary>
	/// The per-release delta. Bounded on both sides: a run of anonymous product erodes
	/// standing slowly, and no single record can mint a critical reputation outright.
	/// </summary>
	public static float GetAcclaimDelta(float craft, float commercial) {
		float above = craft - CraftBar;
		if (above <= 0f) return above * .35f;   // erosion is gentler than the climb
		// Scaled so a record exactly at the bar earns nothing and a perfect one earns the cap.
		float earned = Mathf.Min(MaxGainPerRelease, above / Mathf.Max(.0001f, 1f - CraftBar) * MaxGainPerRelease);
		float unrecognized = Mathf.Clamp(1f - commercial, 0f, 1f);
		return earned * (1f + UnderratedBonus * unrecognized);
	}

	public static float Apply(float priorAcclaim, float craft, float commercial) =>
		Mathf.Clamp(priorAcclaim * RetentionPerRelease + GetAcclaimDelta(craft, commercial), 0f, 1f);

	/// <summary>
	/// Called once per completed chart run, from the same place the commercial outcome
	/// lands on the artist. Consumes no RNG.
	/// </summary>
	public static void OnChartRunComplete(SimulatedArtist artist, RecordRuntimeData record, AILabel label) {
		if (!ArtistEvolution.Observing || artist == null || record?.baseRecord == null) return;
		Record baseRecord = record.baseRecord;
		bool isAlbum = baseRecord.format == ReleaseFormat.Album;
		float craft = GetCraftScore(baseRecord.originality, baseRecord.productionQuality,
			baseRecord.album?.thematicCohesion ?? 0f, isAlbum && baseRecord.album != null,
			label?.productionQuality ?? .5f);
		artist.criticalAcclaim = Apply(artist.criticalAcclaim, craft, GetCommercialScore(record.peakPosition));
	}
}
