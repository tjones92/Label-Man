// Systems/Naming/Core/Selection.cs
// Grammatical selection for title slots. Mood and domain tags say what a word is ABOUT; nothing said
// how it can be USED, so a template like "You %verb% Me" happily produced "You Sing Me", and
// "%noun% down the %noun%" produced "Mercy Down the Redemption". This layer adds:
//   * WordFeatures — Data/Naming/features.json, applied at load as g* tags on lexicon entries
//     (gObjPerson, gAbstract, gMass, gProper, gArticle, gAdverbial, gExclaim, gGerund, gPlural, gPath);
//   * TitleSelection — checks a drawn word against the literal text around its slot, the same way
//     Lineup.cs infers cast claims from a template's literal text (no per-template authoring);
//   * TitlePolish — whole-title fixes: doubled articles, an act-name/verb agreement ("The Calloways
//     Sings" -> "Sing"), and a "?" only on a question.
// Godot-free.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LabelMan.Naming {

	public sealed class WordFeatures {
		// pos -> word (lower) -> feature tags
		private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _map = new(StringComparer.OrdinalIgnoreCase);

		public static readonly string[] All = {
			"gObjPerson", "gAbstract", "gMass", "gProper", "gArticle", "gAdverbial", "gExclaim", "gGerund", "gPlural", "gPath" };

		private static string TagFor(string feature) => feature.ToLowerInvariant() switch {
			"objperson" => "gObjPerson", "abstract" => "gAbstract", "mass" => "gMass", "proper" => "gProper",
			"article" => "gArticle", "adverbial" => "gAdverbial", "exclaim" => "gExclaim", "gerund" => "gGerund",
			"plural" => "gPlural", "path" => "gPath", _ => null };

		public void LoadJson(string json) {
			if (string.IsNullOrWhiteSpace(json)) return;
			using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
			foreach (var pos in doc.RootElement.EnumerateObject()) {
				if (pos.Value.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
				if (!_map.TryGetValue(pos.Name, out var words)) _map[pos.Name] = words = new(StringComparer.OrdinalIgnoreCase);
				foreach (var feat in pos.Value.EnumerateObject()) {
					string tag = TagFor(feat.Name);
					if (tag == null || feat.Value.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
					foreach (var w in feat.Value.EnumerateArray()) {
						string word = w.GetString();
						if (string.IsNullOrWhiteSpace(word)) continue;
						if (!words.TryGetValue(word, out var set)) words[word] = set = new(StringComparer.OrdinalIgnoreCase);
						set.Add(tag);
					}
				}
			}
		}

		/// <summary>Add feature tags to every entry. Automatic: a word written "the X" is gArticle, a
		/// place name without its article is gProper. Returns the number of entries tagged.</summary>
		public int Apply(Lexicon lex) {
			int n = 0;
			foreach (var e in lex.All) {
				if (e.Word == null) continue;
				bool hit = false;
				if (e.Word.StartsWith("the ", StringComparison.OrdinalIgnoreCase)) { Add(e, "gArticle"); hit = true; }
				else if (string.Equals(e.Pos, "place", StringComparison.OrdinalIgnoreCase)) { Add(e, "gProper"); hit = true; }
				if (_map.TryGetValue(e.Pos, out var words) && words.TryGetValue(e.Word, out var set)) {
					foreach (var t in set) Add(e, t);
					hit = true;
				}
				if (hit) n++;
			}
			return n;
		}

		private static void Add(WordEntry e, string tag) {
			e.Tags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			e.Tags.Add(tag);
		}

		public bool Has(string pos, string word, string tag) =>
			word != null && _map.TryGetValue(pos, out var words) && words.TryGetValue(word, out var s) && s.Contains(tag);
	}

	/// <summary>Does a drawn word fit the literal text either side of its slot?</summary>
	public static class TitleSelection {
		/// <summary>Master switch for the grammar layer (selection, plural guard, -ing and "?" rules, TitlePolish).
		/// Off reproduces the pre-layer engine exactly; the title census uses it for its baseline.</summary>
		public static bool Enabled = true;
		private static readonly string[] ObjectPronouns = { "me", "you", "her", "him", "us", "them" };
		private static readonly string[] NoArticle = { "gProper", "gArticle", "gAdverbial", "gExclaim", "gGerund" };
		// a spatial preposition + "the": its object must be a thing you can be in/on/under, not an idea
		private static readonly Regex SpatialThe = new(@"\b(down|up|along|across|through|over|under|beneath|beyond|beside|behind|around|past|into|onto|on|by|near|toward|towards|before|after)\s+the\s*$", RegexOptions.IgnoreCase);
		private static readonly Regex SpatialAfter = new(@"^\s+(down|up|along|across|through|over|under|beneath|beyond|beside|behind|around|past|on|by|near|before|after)\s+the\b", RegexOptions.IgnoreCase);
		private static readonly Regex PathThe =new(@"\b(down|up|along)\s+the\s*$", RegexOptions.IgnoreCase);
		private static readonly Regex TheEnd = new(@"\bthe\s*$", RegexOptions.IgnoreCase);
		private static readonly Regex AEnd = new(@"\b(a|an)\s*$", RegexOptions.IgnoreCase);
		// a bare preposition (no article) right before the slot
		private static readonly Regex BarePrep = new(@"\b(of|at|in|on|to|from|into|onto|for|with|across|through|over|under|beyond|beside|down|up|along|near)\s*$", RegexOptions.IgnoreCase);

		public static bool IsNounLike(string pos) =>
			string.Equals(pos, "noun", StringComparison.OrdinalIgnoreCase) || string.Equals(pos, "place", StringComparison.OrdinalIgnoreCase);

		/// <param name="left">Everything assembled before the slot (literal text and earlier slots).</param>
		/// <param name="right">The literal text right after the slot, up to the next slot.</param>
		/// <param name="pluralized">The slot is inflected to a plural.</param>
		public static bool Admits(string pos, WordEntry e, string left, string right, bool pluralized) {
			if (e == null) return true;
			left ??= ""; right ??= "";
			if (string.Equals(pos, "verb", StringComparison.OrdinalIgnoreCase)) {
				// "You %verb% Me", "Don't %verb% Me", "Please %verb% Me, Baby": the verb takes a person object.
				string next = FirstWord(right);
				if (next != null && ObjectPronouns.Contains(next) && !e.HasTag("gObjPerson")) return false;
				return true;
			}
			if (!IsNounLike(pos)) return true;
			// "Honor Beside the Barn", "Odyssey Under the Ranch": the thing placed beside/under something must
			// be a thing too, not an idea ("Midnight on the Bayou", "Freight Down the Highway" are fine).
			if (SpatialAfter.IsMatch(right) && (e.HasTag("gAbstract") || e.HasTag("gAdverbial")) && !pluralized) return false;
			// "at the %place% Jazz Festival", "the %noun% Blues": a word followed by more of the noun phrase
			// is a modifier, and the article belongs to the head ("the Newport Jazz Festival").
			if (right.Length > 1 && right[0] == ' ' && char.IsUpper(right[1])) return true;
			bool any(params string[] tags) => tags.Any(e.HasTag);
			if (TheEnd.IsMatch(left)) {
				// a word written with its own article ("the Riverside Theater") is fine: TitlePolish drops the doubled "the"
				if (any(NoArticle.Where(t => t != "gArticle").ToArray())) return false;
				if (SpatialThe.IsMatch(left) && e.HasTag("gAbstract") && !pluralized) return false;
				if (PathThe.IsMatch(left) && !e.HasTag("gPath")) return false;
				return true;
			}
			if (AEnd.IsMatch(left)) return !any(NoArticle) && !e.HasTag("gPlural") && !pluralized;
			if (BarePrep.IsMatch(left)) {
				// "Songs of Shack", "Live at Alley", "Grace on Farm": a singular count noun needs an article.
				if (pluralized || any("gPlural", "gMass", "gAbstract", "gProper", "gArticle", "gAdverbial", "gExclaim", "gGerund")) return true;
				return false;
			}
			return true;
		}

		private static string FirstWord(string s) {
			s = s.TrimStart(' ', ',');
			int i = 0; while (i < s.Length && (char.IsLetter(s[i]) || s[i] == '\'')) i++;
			return i == 0 ? null : s.Substring(0, i).ToLowerInvariant();
		}
	}

	/// <summary>Whole-title fixes that need the finished string.</summary>
	public static class TitlePolish {
		private static readonly Regex DoubleArticle = new(@"\b(the|a|an)\s+the\b", RegexOptions.IgnoreCase);
		private static readonly Regex QuestionStart = new(@"^(\(.*?\)\s*)?(will|where|why|do|does|did|can|can't|could|what|who|whose|how|is|isn't|are|aren't|was|were|have|has|would|wouldn't|should|won't|don't you|ain't|am|shall|may|when)\b", RegexOptions.IgnoreCase);
		private static readonly Dictionary<string, string> VerbBase = new(StringComparer.OrdinalIgnoreCase) {
			["Sings"] = "Sing", ["Plays"] = "Play", ["Swings"] = "Swing", ["Rocks"] = "Rock", ["Goes"] = "Go",
			["Meets"] = "Meet", ["Salutes"] = "Salute", ["Celebrates"] = "Celebrate", ["Presents"] = "Present",
			["Conducts"] = "Conduct", ["Is"] = "Are", ["Has"] = "Have", ["Does"] = "Do", ["Remembers"] = "Remember",
			["Twists"] = "Twist", ["Strikes"] = "Strike", ["Returns"] = "Return", ["Dances"] = "Dance" };

		/// <summary>Fixes for any song or album title: doubled articles; "?" only on a question.</summary>
		public static string Title(string s) {
			if (string.IsNullOrWhiteSpace(s)) return s;
			s = DoubleArticle.Replace(s, m => m.Value.Substring(0, m.Groups[1].Length));
			if (s.EndsWith("?") && !IsQuestion(s)) s = s.TrimEnd('?', ' ');
			return s;
		}

		public static bool IsQuestion(string s) => QuestionStart.IsMatch(s ?? "");

		/// <summary>Is an act's billing grammatically plural ("The Calloways", "Dale and the Rockets")?</summary>
		public static bool IsPluralAct(string act) {
			if (string.IsNullOrWhiteSpace(act)) return false;
			string a = act.Trim();
			if (Regex.IsMatch(a, @"\b(and|&|'n')\b", RegexOptions.IgnoreCase)) return true;
			string last = a.Split(' ').Last();
			if (Regex.IsMatch(last, @"^(Trio|Quartet|Quintet|Sextet|Duo|Orchestra|Band|Combo|Ensemble|Choir|Chorus|Company|Family)$", RegexOptions.IgnoreCase)) return false;
			return a.StartsWith("The ", StringComparison.OrdinalIgnoreCase) && last.EndsWith("s") && !last.EndsWith("ss");
		}

		/// <summary>Album fixes on top of <see cref="Title"/>: the verb after the act's name agrees with it,
		/// and a pronoun referring to the act matches its number. A title that is exactly the act's name or
		/// the lead single keeps that text verbatim (no "!" or "?" bolted on).</summary>
		public static string Album(string s, string act, string leadSingle) {
			if (string.IsNullOrWhiteSpace(s)) return s;
			foreach (var bound in new[] { act, leadSingle }) {
				if (string.IsNullOrWhiteSpace(bound)) continue;
				string t = s.TrimEnd('!', '?');
				if (string.Equals(t, bound, StringComparison.Ordinal)) return bound;
			}
			s = Title(s);
			if (string.IsNullOrWhiteSpace(act)) return s;
			int i = s.IndexOf(act, StringComparison.OrdinalIgnoreCase);
			if (i < 0) return s;
			// "Here's The Dean Brothers" -> "Here's the Dean Brothers": the act's article is lower-case mid-title.
			if (i > 0 && act.StartsWith("The ", StringComparison.Ordinal)) {
				s = s.Substring(0, i) + "the " + s.Substring(i + 4);
				act = "the " + act.Substring(4);
				s = Title(s);
				i = s.IndexOf(act, StringComparison.OrdinalIgnoreCase);
				if (i < 0) return s;
			}
			int after = i + act.Length;
			string rest = s.Substring(after);
			bool plural = IsPluralAct(act);
			var m = Regex.Match(rest, @"^ ([A-Z][a-z]+)\b");
			if (plural && m.Success && VerbBase.TryGetValue(m.Groups[1].Value, out var bas))
				rest = " " + bas + rest.Substring(m.Length);
			rest = plural ? Regex.Replace(rest, @"\b(His|Her)\b", "Their") : Regex.Replace(rest, @"\bTheir\b", "the");
			return s.Substring(0, after) + rest;
		}
	}
}
