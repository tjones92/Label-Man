using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Picks descriptive wording for the player-facing reads (scouting impressions, trade-paper blurbs, Rolodex reads).
///
/// Two rules hold every phrase in the Descriptors folder to the simulation:
/// <list type="number">
/// <item>A phrase is only offered when the state it names is true. Each option is a (when, text) pair; a phrase
/// whose fact is false is never a candidate, so an empty pool says nothing rather than something invented.
/// Two phrases may describe the same state (a noisy club, a weak hook) in different words; that is the point.</item>
/// <item>The choice is a pure hash of a caller key (act, record, week...). It draws on no game stream, so reading a
/// card never moves the simulation, and the same card reads the same way when it is reopened.</item>
/// </list>
/// Keys carry the subject's id, so two acts with the same impression usually get different wording.
/// Godot-free: SimTools/DescriptorCensus compiles this folder on its own.
/// </summary>
public static class Phrase {
	public readonly record struct Option(bool When, string Text);

	public static Option When(bool when, string text) => new(when, text);
	public static Option Always(string text) => new(true, text);

	/// <summary>A stable unit in [0, 1) for <paramref name="key"/> (FNV-1a then a 64-bit finaliser, never GetHashCode).</summary>
	public static double Unit(string key) {
		ulong hash = 14695981039346656037UL;
		foreach (char c in "descriptor-v1|" + (key ?? "")) { hash ^= c; hash = unchecked(hash * 1099511628211UL); }
		hash ^= hash >> 33; hash = unchecked(hash * 0xff51afd7ed558ccdUL); hash ^= hash >> 33;
		return (hash >> 11) * (1.0 / (1UL << 53));
	}

	public static int Index(string key, int count) => count <= 0 ? -1 : Math.Min(count - 1, (int)(Unit(key) * count));

	/// <summary>One of the options whose fact holds, or null if none does.</summary>
	public static string Pick(string key, params Option[] options) {
		var live = options.Where(o => o.When && !string.IsNullOrEmpty(o.Text)).ToArray();
		return live.Length == 0 ? null : live[Index(key, live.Length)].Text;
	}

	public static string Pick(string key, params string[] options) =>
		options == null || options.Length == 0 ? null : options[Index(key, options.Length)];

	/// <summary>Up to <paramref name="count"/> distinct live texts, in a stable shuffled order.</summary>
	public static List<string> Some(string key, int count, IEnumerable<string> texts) =>
		texts.Where(t => !string.IsNullOrEmpty(t)).Distinct()
			.Select((t, i) => (t, order: Unit(key + "|" + t)))
			.OrderBy(p => p.order).Take(Math.Max(0, count)).Select(p => p.t).ToList();

	public static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

	/// <summary>"guitar", "guitar and bass", "guitar, bass and drums".</summary>
	public static string List(IReadOnlyList<string> items) => items == null || items.Count == 0 ? ""
		: items.Count == 1 ? items[0]
		: string.Join(", ", items.Take(items.Count - 1)) + " and " + items[items.Count - 1];

	/// <summary>"on the West Coast", "in the Rockies", "in the Deep South": the market regions are all "the" regions,
	/// and a coast is something you are on.</summary>
	public static string InRegion(string region) => string.IsNullOrEmpty(region) ? "" :
		region.EndsWith("Coast", StringComparison.Ordinal) ? "on the " + region : "in the " + region;

	public static string Number(int n) => n switch {
		0 => "no", 1 => "one", 2 => "two", 3 => "three", 4 => "four", 5 => "five", 6 => "six",
		7 => "seven", 8 => "eight", 9 => "nine", 10 => "ten", 11 => "eleven", 12 => "twelve", _ => n.ToString()
	};

	public static string Ordinal(int n) {
		int t = n % 100;
		string suffix = t is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
		return n + suffix;
	}
}
