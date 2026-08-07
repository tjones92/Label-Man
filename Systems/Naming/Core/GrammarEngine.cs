// Systems/Naming/Core/GrammarEngine.cs
// Tracery-style expansion engine. Rules are data (grammar.json). A template is literal text
// plus tokens:
//   #symbol#            expand another grammar symbol (recursive, weighted)
//   {pos}  {pos:a,b}    lexicon query for a word of `pos` carrying tags a,b
//   {~pos:a}            hybrid Markov coinage trained on that pool (invented word)
//   {fn:args}           built-in function: number, writtenNumber, ordinal, callsign, letters,
//                       initial, year2, decade
//   (one|two|three)     inline uniform choice (options may contain tokens)
//   $style $genre       inside a tag list, substituted from the context
// Any token may carry .modifiers: .cap .s .trimS .poss .lower .upper  e.g. {groupNoun:psych.trimS}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace LabelMan.Naming {

	public interface IWordCoiner {
		/// <summary>Coin an invented word from the given pool; null if none acceptable.</summary>
		string Coin(string pos, IReadOnlyList<string> tags, NamingContext ctx);
	}

	public sealed class GrammarEngine {
		public sealed class Rule { public double w = 1; public string t = ""; }

		private readonly Dictionary<string, List<Rule>> _symbols;
		private readonly Lexicon _lexicon;
		private readonly IWordCoiner _coiner;
		private readonly Inflection _inflection;   // Layer 6: real morphology for .pl/.ger/.past/... modifiers
		private const int MaxDepth = 25;

		private static readonly HashSet<string> Functions = new(StringComparer.OrdinalIgnoreCase) {
			"number", "writtenNumber", "ordinal", "callsign", "letters", "initial", "year2", "yearFull", "decade"
		};

		/// <summary>True if the token name is a built-in function (not a lexicon pos). Lets the
		/// tuner skip functions when listing the word groups a category queries.</summary>
		public static bool IsFunction(string name) => Functions.Contains(name);

		public GrammarEngine(Dictionary<string, List<Rule>> symbols, Lexicon lexicon, IWordCoiner coiner, Inflection inflection = null) {
			_symbols = symbols ?? new();
			_lexicon = lexicon;
			_coiner = coiner;
			_inflection = inflection;
		}

		public IEnumerable<string> Symbols => _symbols.Keys;
		public bool HasSymbol(string s) => _symbols.ContainsKey(s);

		/// <summary>The raw template strings of a symbol's rules (used by the tuner to discover
		/// which lexicon groups a category queries). Empty if the symbol is unknown.</summary>
		public IReadOnlyList<string> Templates(string symbol) =>
			_symbols.TryGetValue(symbol, out var rules) ? rules.Select(r => r.t).ToList() : System.Array.Empty<string>();

		public static Dictionary<string, List<Rule>> ParseGrammar(string json) {
			using var doc = JsonDocument.Parse(json, new JsonDocumentOptions {
				CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
			});
			var result = new Dictionary<string, List<Rule>>(StringComparer.Ordinal);
			foreach (var sym in doc.RootElement.EnumerateObject()) {
				if (sym.Value.ValueKind != JsonValueKind.Array) continue; // skip "//" comments etc.
				var rules = new List<Rule>();
				foreach (var r in sym.Value.EnumerateArray()) {
					if (r.ValueKind == JsonValueKind.String) {
						rules.Add(new Rule { w = 1, t = r.GetString() });
					} else {
						var rule = new Rule();
						if (r.TryGetProperty("t", out var t)) rule.t = t.GetString();
						if (r.TryGetProperty("w", out var w) && w.TryGetDouble(out var wv)) rule.w = wv;
						rules.Add(rule);
					}
				}
				result[sym.Name] = rules;
			}
			return result;
		}

		public string Expand(string symbol, NamingContext ctx) {
			var sb = new StringBuilder();
			ExpandInto(sb, symbol, ctx, 0);
			return CollapseWhitespace(sb.ToString());
		}

		private void ExpandInto(StringBuilder sb, string symbol, NamingContext ctx, int depth) {
			if (depth > MaxDepth || !_symbols.TryGetValue(symbol, out var rules) || rules.Count == 0) return;
			var rule = PickRule(rules, ctx.Rng);
			Render(sb, rule.t, ctx, depth);
		}

		private static Rule PickRule(List<Rule> rules, IRandom rng) {
			double total = 0; foreach (var r in rules) total += r.w <= 0 ? 1 : r.w;
			double roll = rng.NextDouble() * total;
			foreach (var r in rules) { roll -= (r.w <= 0 ? 1 : r.w); if (roll <= 0) return r; }
			return rules[rules.Count - 1];
		}

		// ---- template renderer -------------------------------------------------
		private void Render(StringBuilder sb, string template, NamingContext ctx, int depth) {
			if (string.IsNullOrEmpty(template)) return;
			int i = 0;
			while (i < template.Length) {
				char ch = template[i];
				if (ch == '#') {
					int end = template.IndexOf('#', i + 1);
					if (end < 0) { sb.Append(ch); i++; continue; }
					string body = template.Substring(i + 1, end - i - 1);
					sb.Append(ExpandSymbolToken(body, ctx, depth));
					i = end + 1;
				} else if (ch == '{') {
					int end = template.IndexOf('}', i + 1);
					if (end < 0) { sb.Append(ch); i++; continue; }
					string body = template.Substring(i + 1, end - i - 1);
					sb.Append(ResolveQuery(body, ctx));
					i = end + 1;
				} else if (ch == '(') {
					int end = MatchParen(template, i);
					if (end < 0) { sb.Append(ch); i++; continue; }
					string body = template.Substring(i + 1, end - i - 1);
					var options = SplitTop(body, '|');
					string choice = options.Count == 0 ? "" : options[ctx.Rng.Next(options.Count)];
					Render(sb, choice, ctx, depth); // options may contain tokens
					i = end + 1;
				} else {
					sb.Append(ch); i++;
				}
			}
		}

		private string ExpandSymbolToken(string body, NamingContext ctx, int depth) {
			var (name, mods) = SplitMods(body);
			var inner = new StringBuilder();
			ExpandInto(inner, name, ctx, depth + 1);
			return ApplyMods(inner.ToString(), mods);
		}

		private string ResolveQuery(string body, NamingContext ctx) {
			var (spec, mods) = SplitMods(body);
			bool markov = spec.StartsWith("~");
			if (markov) spec = spec.Substring(1);

			int colon = spec.IndexOf(':');
			string pos = colon < 0 ? spec : spec.Substring(0, colon);
			string tagStr = colon < 0 ? null : spec.Substring(colon + 1);

			if (Functions.Contains(pos))
				return ApplyMods(ResolveFunction(pos, tagStr, ctx), mods);

			// Untagged token may name a runtime slot (e.g. {artist}, a caller-supplied {city}).
			if (string.IsNullOrEmpty(tagStr) && ctx.Slots != null && ctx.Slots.TryGetValue(pos, out var slot))
				return ApplyMods(slot ?? "", mods);

			var tags = ExpandTags(tagStr, ctx);
			string word;
			if (markov && _coiner != null)
				word = _coiner.Coin(pos, tags, ctx) ?? _lexicon.Query(pos, tags, ctx);
			else
				word = _lexicon.Query(pos, tags, ctx);
			return ApplyMods(word, mods);
		}

		// $style -> primary context style tag; $genre -> genre lowercased; else literal tag
		private static List<string> ExpandTags(string tagStr, NamingContext ctx) {
			if (string.IsNullOrWhiteSpace(tagStr)) return new List<string>();
			var outTags = new List<string>();
			foreach (var raw in tagStr.Split(',')) {
				string t = raw.Trim();
				if (t.Length == 0) continue;
				if (t.Equals("$style", StringComparison.OrdinalIgnoreCase)) {
					if (ctx.StyleTags != null && ctx.StyleTags.Count > 0) outTags.Add(ctx.StyleTags[0]);
				} else if (t.Equals("$genre", StringComparison.OrdinalIgnoreCase)) {
					if (!string.IsNullOrEmpty(ctx.Genre)) outTags.Add(ctx.Genre.ToLowerInvariant());
				} else if (t.StartsWith("$")) {
					if (ctx.TagSets != null && ctx.TagSets.TryGetValue(t.Substring(1), out var set))
						outTags.AddRange(set);
				} else outTags.Add(t);
			}
			return outTags;
		}

		private string ResolveFunction(string fn, string args, NamingContext ctx) {
			var rng = ctx.Rng;
			switch (fn.ToLowerInvariant()) {
				case "number": {
					var (a, b) = ParseRange(args, 100, 999);
					return rng.Range(a, b).ToString();
				}
				case "writtennumber": {
					var (a, b) = ParseRange(args, 1, 12);
					return WrittenNumber(rng.Range(a, b));
				}
				case "ordinal": {
					var (a, b) = ParseRange(args, 1, 13);
					return Ordinal(rng.Range(a, b));
				}
				case "callsign": {
					char p = rng.Chance(0.5) ? 'W' : 'K';
					return p + Letters(rng, 3);
				}
				case "letters": {
					var (a, _) = ParseRange(args, 3, 3);
					return Letters(rng, a);
				}
				case "initial": return "ABCDEFGHJKLMNPRSTVW"[rng.Next(19)].ToString();
				case "year2": return (ctx.Year <= 0 ? 60 : ctx.Year % 100).ToString("D2");
				case "yearfull": return (ctx.Year <= 0 ? 1960 : ctx.Year).ToString();
				case "decade": return $"{(ctx.Year / 10 % 10) * 10}s";
				default: return "";
			}
		}

		// ---- modifiers ---------------------------------------------------------
		private static (string, List<string>) SplitMods(string body) {
			var parts = body.Split('.');
			var mods = new List<string>();
			for (int i = 1; i < parts.Length; i++) mods.Add(parts[i]);
			return (parts[0], mods);
		}

		private string ApplyMods(string s, List<string> mods) {
			if (mods == null) return s;
			foreach (var m in mods) {
				string mod = m.ToLowerInvariant();
				switch (mod) {
					case "cap": s = Capitalize(s); break;
					case "lower": s = s.ToLowerInvariant(); break;
					case "upper": s = s.ToUpperInvariant(); break;
					case "trims": s = s.EndsWith("s") ? s.Substring(0, s.Length - 1) : s; break;
					// morphology: prefer the Layer-6 inflection engine (echoes not echos), else fall back
					case "s": case "pl": s = Morph(s, InflForm.Plural, () => Pluralize(s)); break;
					case "poss": s = Morph(s, InflForm.Possessive, () => s.EndsWith("s") ? s + "'" : s + "'s"); break;
					case "ger": case "ing": s = Morph(s, InflForm.Ger, () => s); break;
					case "past": s = Morph(s, InflForm.Past, () => s); break;
					case "pastpart": case "pp": s = Morph(s, InflForm.PastPart, () => s); break;
					case "3s": s = Morph(s, InflForm.ThirdSing, () => s); break;
					case "comp": s = Morph(s, InflForm.Comparative, () => s); break;
					case "sup": s = Morph(s, InflForm.Superlative, () => s); break;
				}
			}
			return s;
		}

		// Multi-word phrases inflect only their last word (e.g. "blue moon".pl -> "blue moons").
		private string Morph(string s, InflForm form, Func<string> fallback) {
			if (_inflection == null || string.IsNullOrWhiteSpace(s)) return fallback();
			int sp = s.LastIndexOf(' ');
			string head = sp < 0 ? "" : s.Substring(0, sp + 1);
			string tail = sp < 0 ? s : s.Substring(sp + 1);
			return head + _inflection.Inflect(tail, form);
		}

		private static string Capitalize(string s) =>
			string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

		private static string Pluralize(string s) {
			if (string.IsNullOrEmpty(s)) return s;
			char last = char.ToLowerInvariant(s[s.Length - 1]);
			if (s.Length >= 2 && last == 'y' && !"aeiou".Contains(char.ToLowerInvariant(s[s.Length - 2])))
				return s.Substring(0, s.Length - 1) + "ies";
			if (last == 's' || last == 'x' || last == 'z' ||
				s.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
				s.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
				return s + "es";
			return s + "s";
		}

		// ---- small helpers -----------------------------------------------------
		private static string Letters(IRandom rng, int count) {
			const string cons = "BCDFGHJKLMNPQRSTVWXYZ", vowels = "AEIOU";
			var sb = new StringBuilder();
			for (int i = 0; i < count; i++)
				sb.Append(i % 2 == 0 ? cons[rng.Next(cons.Length)] : vowels[rng.Next(vowels.Length)]);
			return sb.ToString();
		}

		private static (int, int) ParseRange(string args, int defA, int defB) {
			if (string.IsNullOrWhiteSpace(args)) return (defA, defB);
			var m = args.Split('-');
			int a = defA, b = defB;
			if (m.Length >= 1) int.TryParse(m[0], out a);
			b = m.Length >= 2 && int.TryParse(m[1], out var bb) ? bb : a;
			return (a, b);
		}

		private static string WrittenNumber(int n) => n switch {
			1 => "One", 2 => "Two", 3 => "Three", 4 => "Four", 5 => "Five", 6 => "Six",
			7 => "Seven", 8 => "Eight", 9 => "Nine", 10 => "Ten", 11 => "Eleven", 12 => "Twelve",
			_ => n.ToString()
		};

		private static string Ordinal(int n) {
			string suffix = (n % 100) switch {
				11 or 12 or 13 => "th",
				_ => (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" }
			};
			return $"{n}{suffix}";
		}

		private static int MatchParen(string s, int open) {
			int depth = 0;
			for (int i = open; i < s.Length; i++) {
				if (s[i] == '(') depth++;
				else if (s[i] == ')') { depth--; if (depth == 0) return i; }
			}
			return -1;
		}

		private static List<string> SplitTop(string s, char sep) {
			var parts = new List<string>(); int depth = 0, start = 0;
			for (int i = 0; i < s.Length; i++) {
				char c = s[i];
				if (c == '(') depth++;
				else if (c == ')') depth--;
				else if (c == sep && depth == 0) { parts.Add(s.Substring(start, i - start)); start = i + 1; }
			}
			parts.Add(s.Substring(start));
			return parts;
		}

		private static string CollapseWhitespace(string s) {
			var sb = new StringBuilder(s.Length);
			bool prevSpace = false;
			foreach (char c in s) {
				bool sp = c == ' ' || c == '\t';
				if (sp) { if (!prevSpace) sb.Append(' '); prevSpace = true; }
				else { sb.Append(c); prevSpace = false; }
			}
			return sb.ToString().Trim();
		}
	}
}
