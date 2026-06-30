using System.Text;

public static class GenreNameFormatter
{
	public static string Format(Genre genre)
	{
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
}
