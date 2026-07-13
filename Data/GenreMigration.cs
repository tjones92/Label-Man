using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Idempotent legacy-to-canonical migration used by save readers and enabled-path release creation.</summary>
public static class GenreMigration {
	public const int CurrentSchemaVersion = 1;

	public static void Canonicalize(Record record) {
		if (record == null) throw new ArgumentNullException(nameof(record));
		if (record.genreSchemaVersion >= CurrentSchemaVersion && !string.IsNullOrWhiteSpace(record.primaryGenreId)) return;
		int? year = record.releaseDate.year > 0 ? record.releaseDate.year : null;
		Genre originalPrimary = record.primaryGenre;
		var tags = new HashSet<string>(record.genreTagIds ?? Array.Empty<string>(), StringComparer.Ordinal);
		switch (originalPrimary) {
			case Genre.Motown: tags.Add("motown"); break;
			case Genre.GirlGroup: tags.Add("girl-group"); break;
			case Genre.BritishInvasion: tags.Add("british"); break;
			case Genre.Skiffle: tags.Add("skiffle"); tags.Add("british"); break;
			case Genre.SkaRocksteady: tags.Add("jamaican"); break;
		}
		Canonicalize(ref record.primaryGenre, ref record.secondaryGenre, year);
		record.primaryGenreId = GenreCatalog.Get(record.primaryGenre).Id;
		record.secondaryGenreId = GenreCatalog.TryGet(record.secondaryGenre, out GenreProfile secondary) ? secondary.Id : string.Empty;
		record.genreTagIds = tags.OrderBy(tag => tag, StringComparer.Ordinal).ToArray();
		record.genreSchemaVersion = CurrentSchemaVersion;
	}

	/// <summary>Canonicalizes an identity pair without adding metadata or consuming randomness.</summary>
	public static void Canonicalize(ref Genre primary, ref Genre secondary, int? year = null) {
		Genre originalPrimary = primary;
		Genre originalSecondary = secondary;
		primary = originalPrimary == Genre.GirlGroup
			? originalSecondary is Genre.Soul or Genre.RnB ? Genre.Soul : Genre.TeenPop
			: GenreCatalog.MapLegacy(originalPrimary, year);
		secondary = GenreCatalog.MapLegacy(originalSecondary, year);
	}
}
