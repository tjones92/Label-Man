// Systems/Naming/Core/Lineup.cs
// The people an act's name is allowed to talk about. Godot-free.
//
// Before this, an act's name was drawn blind: "Sonny 'n' Buck" for a five-piece with no Sonny
// and no Buck, "Marlon Burke Trio" for a quartet, "The Hart Brothers" for three unrelated men.
// Measured on a seed-1001 decade (branch-end-decade-market-1001-musician-recognition.csv): 1,304
// of the 1,343 band names that contained a first name named nobody in the band.
//
// With a Lineup on the context, every person slot in a band/solo name BINDS to a real member
// instead of drawing from the lexicon, and every word in the template that makes a claim about
// the lineup -- its size (Duo, Trio, Quartet, "The Five ..."), its sex (Brothers, Sisters, Boys,
// "and His Orchestra"), kinship (Brothers, Family, Twins), or a backing band ("X and the Ys") --
// is checked against it. A template whose claim is false is not eligible, exactly like a template
// whose word pool is empty. No lineup (the tuner, legacy callers) means no binding and no gating.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LabelMan.Naming {

	public sealed class LineupPerson {
		public string First;
		public string Last;
		public bool Female;
		public LineupPerson() { }
		public LineupPerson(string first, string last, bool female) { First = first; Last = last; Female = female; }
		public override string ToString() => $"{First} {Last}";
	}

	public class Lineup {
		/// <summary>Front person first (the bandleader / lead singer), then the rest in billing order.</summary>
		public List<LineupPerson> People = new();
		/// <summary>The members are kin and share the front person's surname (the adapter made it so).</summary>
		public bool Family;
		public int Size => People.Count;
		public bool AllMale => People.Count > 0 && People.All(p => !p.Female);
		public bool AllFemale => People.Count > 0 && People.All(p => p.Female);
		public LineupPerson Leader => People.Count > 0 ? People[0] : null;

		// Grammar-render cursor: grammar tokens carry no index, so {firstName} tokens bind to
		// successive members in the order they render. Reset at the start of every expansion.
		internal int NextPerson;
		internal int LastPerson = -1;
		internal bool LastSurnameUsed = true;
		internal void ResetCursor() { NextPerson = 0; LastPerson = -1; LastSurnameUsed = true; }

		/// <summary>The member a person slot binds to. Index 0 is the front person.</summary>
		public LineupPerson At(int index) => index >= 0 && index < People.Count ? People[index] : null;

		private static readonly string[] SizeWords = { null, "Solo", "Duo", "Trio", "Quartet", "Quintet", "Sextet", "Septet", "Octet", "Nonet" };
		private static readonly string[] CountWords = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve" };

		/// <summary>"Trio" for three people. Null outside 2..9.</summary>
		public static string EnsembleWord(int size) => size >= 2 && size < SizeWords.Length ? SizeWords[size] : null;
		/// <summary>"Three" for three people. Null outside 1..12.</summary>
		public static string CountWord(int size) => size >= 1 && size < CountWords.Length ? CountWords[size] : null;
		public static int CountFromWord(string w) {
			if (string.IsNullOrEmpty(w)) return -1;
			for (int i = 0; i < CountWords.Length; i++) if (string.Equals(CountWords[i], w, StringComparison.OrdinalIgnoreCase)) return i;
			return int.TryParse(w, out var n) ? n : -1;
		}
	}

	/// <summary>
	/// What a name's literal wording claims about the act. Inferred from the template text so new
	/// templates are covered without per-template authoring; a template may still state it
	/// explicitly with a "cast" object, which wins.
	/// </summary>
	public sealed class CastClaim {
		public int MinSize = 1, MaxSize = int.MaxValue;
		public bool Family;           // members are kin (Brothers, Sisters, Family, Twins...)
		public string AllGender;      // "male"/"female": every member (Brothers, Boys / Sisters, Girls)
		public string LeaderGender;   // "male"/"female": the front person ("and His ...", "and Her ...")
		public bool IsEmpty => MinSize <= 1 && MaxSize == int.MaxValue && !Family && AllGender == null && LeaderGender == null;

		public bool Admits(Lineup l) {
			if (l == null) return true;
			if (l.Size < MinSize || l.Size > MaxSize) return false;
			if (Family && !l.Family) return false;
			if (AllGender == "male" && !l.AllMale) return false;
			if (AllGender == "female" && !l.AllFemale) return false;
			if (LeaderGender == "male" && (l.Leader == null || l.Leader.Female)) return false;
			if (LeaderGender == "female" && (l.Leader == null || !l.Leader.Female)) return false;
			return true;
		}

		public void Merge(CastClaim o) {
			if (o == null) return;
			MinSize = Math.Max(MinSize, o.MinSize); MaxSize = Math.Min(MaxSize, o.MaxSize);
			Family |= o.Family; AllGender ??= o.AllGender; LeaderGender ??= o.LeaderGender;
		}

		private static readonly Dictionary<string, (int size, string gender, bool family)> Words = new(StringComparer.OrdinalIgnoreCase) {
			["duo"] = (2, null, false), ["twosome"] = (2, null, false), ["twins"] = (2, null, true),
			["trio"] = (3, null, false), ["threesome"] = (3, null, false),
			["quartet"] = (4, null, false), ["quintet"] = (5, null, false), ["sextet"] = (6, null, false),
			["septet"] = (7, null, false), ["octet"] = (8, null, false),
			["brothers"] = (0, "male", true), ["sisters"] = (0, "female", true),
			["family"] = (0, null, true), ["clan"] = (0, null, true),
			["boys"] = (0, "male", false), ["men"] = (0, "male", false), ["fellows"] = (0, "male", false), ["playboys"] = (0, "male", false),
			["girls"] = (0, "female", false), ["ladies"] = (0, "female", false), ["sweethearts"] = (0, "female", false),
		};

		/// <summary>
		/// Claims made by literal text (no slot contents). <paramref name="isBand"/> distinguishes a
		/// band template -- where "X and the Ys" means X plus at least two bandmates -- from a solo
		/// billing like "X and His Orchestra", whose orchestra is hired, not in the lineup.
		/// </summary>
		public static CastClaim FromLiteral(string text, bool isBand) {
			var c = new CastClaim();
			if (string.IsNullOrEmpty(text)) return c;
			string t = " " + text.ToLowerInvariant() + " ";
			if (t.Contains(" and his ") || t.Contains(" & his ")) c.LeaderGender = "male";
			if (t.Contains(" and her ") || t.Contains(" & her ")) c.LeaderGender = "female";
			if (isBand && (t.Contains(" and the ") || t.Contains(" & the ") || t.Contains(" 'n' the ") || t.Contains(" with the ")
						   || t.Contains(" and his ") || t.Contains(" and her ")))
				c.MinSize = Math.Max(c.MinSize, 3);
			foreach (var raw in t.Split(new[] { ' ', ',', '.', '(', ')', '|', '-' }, StringSplitOptions.RemoveEmptyEntries)) {
				if (!Words.TryGetValue(raw, out var w)) continue;
				if (w.size > 0) { c.MinSize = Math.Max(c.MinSize, w.size); c.MaxSize = Math.Min(c.MaxSize, w.size); }
				if (w.gender != null) c.AllGender = w.gender;
				if (w.family) c.Family = true;
			}
			// "The Three Suns", "The Five Keys": a written count that precedes the noun is a head count.
			var words = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i + 1 < words.Length; i++)
				if (words[i] == "the") { int n = Lineup.CountFromWord(words[i + 1]); if (n >= 2 && n <= 12) { c.MinSize = Math.Max(c.MinSize, n); c.MaxSize = Math.Min(c.MaxSize, n); } }
			return c;
		}

		/// <summary>True when a name's literal text is nothing but connectors between people ("X & Y",
		/// "X, Y and Z"): that billing claims the act IS those people, so it caps the head count.</summary>
		public static bool IsPersonsOnly(string literal) {
			if (literal == null) return true;
			foreach (var w in literal.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
				if (!(w == "&" || w == "'n'" || string.Equals(w, "and", StringComparison.OrdinalIgnoreCase))) return false;
			return true;
		}

		/// <summary>Claims made by ONE chosen alternative of a grammar "(a|b|c)" group, used to filter
		/// the options at render time ("The Hart (Brothers|Sisters|Family)" for three women).</summary>
		public static bool OptionAdmits(string option, Lineup l, bool isBand) => l == null || FromLiteral(option, isBand).AdmitsIgnoringBacking(l);
		private bool AdmitsIgnoringBacking(Lineup l) {
			if (Family && !l.Family) return false;
			if (AllGender == "male" && !l.AllMale) return false;
			if (AllGender == "female" && !l.AllFemale) return false;
			if (MaxSize != int.MaxValue && (l.Size < MinSize || l.Size > MaxSize)) return false;
			return true;
		}
	}

	/// <summary>Light final pass for act and label names. Never applied to song or album titles,
	/// whose exact strings are hashed into track traits.</summary>
	public static class ActNamePolish {
		public static string Run(string s) {
			if (string.IsNullOrWhiteSpace(s)) return s;
			// "The the Ozarks Players": a lexicon entry that carries its own article ("the Ozarks")
			// dropped into a "The %noun% ..." frame. Collapse any doubled article.
			var words = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			for (int i = 0; i + 1 < words.Count; i++)
				if (IsThe(words[i]) && IsThe(words[i + 1])) { words.RemoveAt(i + 1); i--; }
			if (words.Count > 0 && IsThe(words[0])) words[0] = "The";
			// A trailing article ("Records the") or connector is always an authoring slip.
			while (words.Count > 1 && (IsThe(words[^1]) || words[^1] == "and" || words[^1] == "&" || words[^1] == "'n'")) words.RemoveAt(words.Count - 1);
			return string.Join(" ", words);
		}
		private static bool IsThe(string w) => string.Equals(w, "the", StringComparison.OrdinalIgnoreCase);
	}
}
