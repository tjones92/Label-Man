using System;
using System.Linq;

/// <summary>
/// Verb agreement for an act's name in desk copy. Copy used to hard-code the plural ("David Fields aren't looking",
/// "Gary Wright want to keep playing"), which reads as a bug for the solo singers who are most of a 1960 bill.
///
/// An act takes the singular when it is one person on stage (<see cref="BandLifeService.ConstitutionOf"/> says Solo),
/// or when it is billed under one member's own name ("Kenny Sanders" fronting a backing band: the name is a man, and
/// the band is his). Everything else keeps the plural ("The Texas Players are", "the Donny Anderson Quintet are").
/// An act the registry no longer has falls back to the plural, which is what the copy said before.
/// </summary>
public static class ActGrammar {
	public static bool IsSingular(SimulatedArtist act) {
		if (act == null) return false;
		if (BandLifeService.ConstitutionOf(act) == LineupConstitution.Solo) return true;
		string name = act.stageName?.Trim();
		return !string.IsNullOrEmpty(name) && act.members != null &&
			act.members.Any(m => m != null && string.Equals(m.FullName, name, StringComparison.OrdinalIgnoreCase));
	}

	public static bool IsSingular(string artistId) =>
		!string.IsNullOrEmpty(artistId) && IsSingular(ArtistManager.Instance?.GetArtist(artistId));

	/// <summary>The singular or plural form, by the act.</summary>
	public static string Verb(SimulatedArtist act, string singular, string plural) => IsSingular(act) ? singular : plural;
	public static string Verb(string artistId, string singular, string plural) => IsSingular(artistId) ? singular : plural;
}
