using Godot;

/// <summary>
/// LAYER 1 of the cultural stack: what a record IS, measured from the record alone.
/// <para>
/// Merit is intrinsic and immutable. It reads originality, coherence, the room it was cut
/// in and what the record was reaching for -- and deliberately reads NO chart position, no
/// units, no airplay and no press. A record that nobody bought has exactly the merit it
/// had the day it was pressed.
/// </para>
/// <para>
/// This separation is the point. The journalism layer, when it arrives, changes how widely
/// a record's merit is KNOWN (<see cref="CulturalRecognitionService"/>) and must never
/// change the merit itself: a magazine discovers a work of art, it does not create one.
/// Keeping the two apart is what lets the Velvet Underground case exist at all -- near-zero
/// recognition sitting on top of high merit, waiting for someone to notice.
/// </para>
/// </summary>
public static class ArtisticMeritService {
	/// <summary>
	/// The bar a record clears before it reads as a critical event at all. Below this a
	/// record is competent product; the trade press has nothing to say about it.
	/// </summary>
	public const float MeritBar = .58f;

	/// <summary>
	/// What the record was reaching for. A concept album is the ceiling case, but it is NOT
	/// the bar: a purpose-made standard LP sits close behind it, because the album-as-art
	/// records of the period were mostly not concept albums. A compilation is at the floor
	/// because it is assembled from sides that already existed.
	/// </summary>
	public static float GetFormatAmbition(ReleaseFormat format, AlbumFormat albumFormat) {
		if (format != ReleaseFormat.Album) return .30f;
		return albumFormat switch {
			AlbumFormat.Concept => 1f,
			AlbumFormat.Standard => .78f,
			AlbumFormat.Live => .40f,
			AlbumFormat.Soundtrack => .35f,
			AlbumFormat.Compilation => .10f,   // a hit plus filler is not a work of art
			_ => .45f
		};
	}

	/// <summary>
	/// Craft as the trade press would have heard it. For a single the album terms are
	/// absent, so production stands in.
	/// <para>
	/// An album's coherence is the BETTER of its body-of-work reading and its thematic
	/// cohesion. Reading cohesion alone made every artist album before 1966 score as
	/// incoherent, because the era ceiling pins that field to its 0.08 clamp floor until then
	/// — so the critics were being handed a decade of albums that had, by construction, no
	/// coherence to hear.
	/// </para>
	/// </summary>
	public static float GetCraft(float originality, float productionQuality, float thematicCohesion,
		bool isAlbum, float labelProductionQuality, float bodyOfWork = 0f) {
		float coherence = isAlbum ? Mathf.Max(bodyOfWork, thematicCohesion) : productionQuality;
		return Mathf.Clamp(.40f * Mathf.Clamp(originality, 0f, 1f) + .35f * Mathf.Clamp(coherence, 0f, 1f) +
			.25f * Mathf.Clamp(labelProductionQuality, 0f, 1f), 0f, 1f);
	}

	/// <summary>
	/// The full intrinsic reading: craft, plus what the record was reaching for. Ambition
	/// without craft earns nothing -- it multiplies rather than adds, so a pretentious
	/// badly-made album scores below a modest well-made one, which is correct.
	/// </summary>
	public static float GetMerit(float craft, float formatAmbition) =>
		Mathf.Clamp(craft * (.72f + .28f * Mathf.Clamp(formatAmbition, 0f, 1f)), 0f, 1f);

	/// <summary>Convenience read straight off a record. Pure; consumes no RNG.</summary>
	public static float Evaluate(Record record, float labelProductionQuality) {
		if (record == null) return 0f;
		bool isAlbum = record.format == ReleaseFormat.Album && record.album != null;
		float craft = GetCraft(record.originality, record.productionQuality,
			record.album?.thematicCohesion ?? 0f, isAlbum, labelProductionQuality,
			record.album?.bodyOfWork ?? 0f);
		return GetMerit(craft, GetFormatAmbition(record.format, record.album?.albumFormat ?? AlbumFormat.Standard));
	}
}
