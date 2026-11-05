/// <summary>"1 song" / "3 songs" for desk copy, in place of the programmer's "song(s)". The noun may be a short
/// phrase ("red check", "more playlist meeting"); only its last word is inflected. Regular English plurals only --
/// every noun the desk counts is regular.</summary>
public static class Plural {
	public static string Of(long n, string noun) => $"{n:N0} {(n == 1 ? noun : Inflect(noun))}";

	public static string Inflect(string noun) {
		if (string.IsNullOrEmpty(noun)) return noun;
		char last = noun[^1], prev = noun.Length > 1 ? noun[^2] : ' ';
		if (last == 'y' && "aeiou".IndexOf(prev) < 0) return noun[..^1] + "ies";
		if (last is 's' or 'x' or 'z' || noun.EndsWith("ch") || noun.EndsWith("sh")) return noun + "es";
		return noun + "s";
	}
}
