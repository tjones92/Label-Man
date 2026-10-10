using System;

/// <summary>A song a person knows from playing it (SimTools/ContactNetworkDirective.md §5.3): cut on a record, or
/// backed in a club. Carried from act to act, so a song can travel with the people who played it.</summary>
[Serializable]
public sealed class CarriedSong {
	public string songId;
	/// <summary>The single they cut it on: the arrangement they learned. Null for an album track or a club set.</summary>
	public string recordId;
	/// <summary>The act they played it with. A song is never "carried" back into the act it came from.</summary>
	public string viaArtistId;
	public int year;
}
