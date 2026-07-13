using System.Text;
using System.Linq;

public static class GenreNameFormatter
{
	public static string Format(Genre genre)
	{
		if (GenreCatalog.TryGet(GenreCatalog.MapLegacy(genre), out GenreProfile profile)) return FormatCanonicalId(profile.Id);
		string name = genre.ToString();
		var result = new StringBuilder();

		foreach (char c in name)
		{
			if (char.IsUpper(c) && result.Length > 0)
				result.Append(' ');
			result.Append(c);
		}

		return result.ToString();
	}

	private static string FormatCanonicalId(string id) => id switch {
		"rnb" => "R&B", "doo-wop" => "Doo-Wop", "rock-and-roll" => "Rock and Roll",
		"bossa-nova" => "Bossa Nova", "tex-mex" => "Tex-Mex", "singer-songwriter" => "Singer-Songwriter",
		"childrens" => "Children's", _ => string.Join(" ", id.Split('-').Select(word => char.ToUpperInvariant(word[0]) + word[1..]))
	};
}
