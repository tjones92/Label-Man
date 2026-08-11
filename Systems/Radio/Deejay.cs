using System.Collections.Generic;
using Godot;

public enum DJArchetype {
	Personality,   // Wolfman Jack / Murray the K - high influence, breaks on taste
	Tastemaker,    // serious, courts respect not cash, breaks album acts
	Hustler,       // pure payola, low taste
	CompanyMan,    // Boss Radio jock - follows the sheet
	Regional       // small-market loyalist, cheap to cultivate
}

/// <summary>
/// The lead DJ of a reporter station (design doc b). One per station; its taste and autonomy shape
/// which records the station is willing to break (Phase 3 candidacy), and its influence is the
/// cultivable asset the break-claim reputation system (Phase 4) pays out to.
/// </summary>
[GlobalClass]
public partial class Deejay : Resource {
	[Export] public string djId;
	[Export] public string djName;
	[Export] public string homeStationId;
	[Export] public DJArchetype archetype = DJArchetype.CompanyMan;

	[Export(PropertyHint.Range, "0,1")] public float influence = 0.5f;
	[Export(PropertyHint.Range, "0,1")] public float taste = 0.5f;     // discovers quality ahead of sales
	[Export(PropertyHint.Range, "0,1")] public float greed = 0.3f;     // payola receptiveness
	[Export(PropertyHint.Range, "0,1")] public float ego = 0.4f;       // wants courting, not just cash

	// Personal taste skew by genre (sparse; missing = neutral 1.0).
	public Dictionary<Genre, float> genreAffinity = new();

	// Runtime relationship + career state.
	public readonly Dictionary<string, float> labelRapport = new(System.StringComparer.Ordinal);
	public float suspicion;   // regulatory attention

	public Deejay() {}

	public float GenreAffinity(Genre g) => genreAffinity.TryGetValue(g, out float v) ? v : 1f;
}
