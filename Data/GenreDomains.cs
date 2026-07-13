using System.Collections.Generic;
using System.Linq;

/// <summary>Explicit gameplay enumeration domains. Never enumerate the serialized enum directly.</summary>
public static class GenreDomains {
	// Original 0..32 serialization order. This is the exact disabled-domain count and order.
	private static readonly Genre[] Legacy = {
		Genre.TraditionalPop, Genre.EasyListening, Genre.Jazz, Genre.Blues, Genre.RockAndRoll, Genre.DooWop, Genre.TeenPop,
		Genre.RnB, Genre.Soul, Genre.Motown, Genre.Funk, Genre.GirlGroup, Genre.Country, Genre.Folk, Genre.FolkRock,
		Genre.CountryRock, Genre.BritishInvasion, Genre.Skiffle, Genre.SurfRock, Genre.GarageRock, Genre.ProtoPunk,
		Genre.Psychedelic, Genre.AcidRock, Genre.BaroquePop, Genre.SunshinePop, Genre.ProgressiveRock, Genre.BluesRock,
		Genre.HardRock, Genre.ProtoMetal, Genre.Bubblegum, Genre.BossaNova, Genre.SkaRocksteady, Genre.Gospel
	};
	private static readonly Genre[] Canonical = GenreCatalog.All.Select(profile => profile.Genre).ToArray();
	public static IReadOnlyList<Genre> LegacyDomain => Legacy;
	public static IReadOnlyList<Genre> CanonicalDomain => Canonical;
	public static IReadOnlyList<Genre> Current => GenreMarketV2.Enabled ? Canonical : Legacy;
}
