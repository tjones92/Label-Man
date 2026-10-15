/// <summary>
/// One line of an act's public discography: what the trade papers printed, not a live market record.
/// Written for pre-1960 careers by <see cref="OpeningCareerService"/>; the live-record retirement writer
/// (audit step 7) appends to the same list so old releases stay on the artist's file.
/// </summary>
public sealed class PublicReleaseEntry {
	public int year;
	public string title;
	public ReleaseFormat format;
	/// <summary>Best position on its chart (singles or LPs); 0 never charted.</summary>
	public int peakPosition;
	public int weeksCharted;
	public int weeksInTopTen;
	/// <summary>Regional markets it sold through; a national hit reaches six or seven, a regional seller one or two.</summary>
	public int markets;
	/// <summary>The market a regional seller came out of, when there was one.</summary>
	public string regionId;
	public string labelName;
}

/// <summary>The kind of career an act had already had by January 1960 (SimTools/OpeningCareersDesign.md).</summary>
public enum OpeningCareerArchetype {
	None,
	AdultStandardBearer,
	CurrentHitmaker,
	EstablishedHitmaker,
	FormerStar,
	CriticsFavourite,
	RegionalName
}
