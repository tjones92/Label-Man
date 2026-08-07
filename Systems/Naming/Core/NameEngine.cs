// Systems/Naming/Core/NameEngine.cs
// Top-level Core entry point: expands a grammar symbol against a context, splices Markov
// coinages, and enforces uniqueness. Godot-free.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabelMan.Naming {

	public sealed class NameEngine : IWordCoiner {
		public Lexicon Lexicon { get; }
		public GrammarEngine Grammar { get; }

		private readonly Dictionary<string, MarkovModel> _markovCache = new();
		// uniquenessBucket -> set of normalized keys / soundex keys
		private readonly Dictionary<string, HashSet<string>> _used = new();
		private readonly Dictionary<string, HashSet<string>> _usedFuzzy = new();

		public double HybridCoinRate = 0.20; // ~20% of markov slots become invented words
		public int MarkovMinLen = 4, MarkovMaxLen = 11;

		public NameEngine(Lexicon lexicon, Dictionary<string, List<GrammarEngine.Rule>> grammar) {
			Lexicon = lexicon;
			Grammar = new GrammarEngine(grammar, lexicon, this);
		}

		public IEnumerable<string> AvailableSymbols => Grammar.Symbols.OrderBy(s => s);

		/// <summary>Expand a symbol once, no uniqueness enforcement (used by the tuner spin).</summary>
		public string ExpandOnce(string symbol, NamingContext ctx) => Grammar.Expand(symbol, ctx);

		/// <summary>Expand with uniqueness. <paramref name="bucket"/> scopes uniqueness (e.g.
		/// "artist", "label", or "song|Artist Name"). Near-duplicate guard applies when
		/// <paramref name="nearDup"/> is set. Re-rolls a different pattern before giving up.</summary>
		public string Generate(string symbol, NamingContext ctx, string bucket, bool nearDup, int attempts = 40) {
			if (!Grammar.HasSymbol(symbol)) return symbol; // authoring error surfaces visibly
			var exact = Bucket(_used, bucket);
			var fuzzy = nearDup ? Bucket(_usedFuzzy, bucket) : null;

			string last = null;
			for (int i = 0; i < attempts; i++) {
				string candidate = Grammar.Expand(symbol, ctx);
				if (string.IsNullOrWhiteSpace(candidate)) continue;
				last = candidate;
				string key = Normalize(candidate);
				if (key.Length == 0 || exact.Contains(key)) continue;
				if (fuzzy != null) {
					string fk = FuzzyKey(candidate);
					// allow near-dups only after we've tried a while (avoid pathological starvation)
					if (fuzzy.Contains(fk) && i < attempts - 5) continue;
					fuzzy.Add(fk);
				}
				exact.Add(key);
				return candidate;
			}
			// Exhausted the pattern space: disambiguate so we NEVER return an exact duplicate.
			// (Re-rolling a different pattern already failed above; this is the last resort,
			// replacing the old " (City)" mutation with tribute-style suffixes.)
			string basename = last ?? Grammar.Expand(symbol, ctx);
			if (string.IsNullOrWhiteSpace(basename)) return symbol;
			for (int n = 2; n < 1000; n++) {
				string cand = n <= 9 ? $"{basename} {Roman(n)}" : $"{basename} ({n})";
				string k = Normalize(cand);
				if (!exact.Contains(k)) { exact.Add(k); return cand; }
			}
			return basename;
		}

		private static string Roman(int n) => n switch {
			2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", _ => n.ToString()
		};

		public void ResetUniqueness() { _used.Clear(); _usedFuzzy.Clear(); }
		public void ClearMarkovCache() => _markovCache.Clear();

		// ---- IWordCoiner: hybrid Markov splice --------------------------------
		public string Coin(string pos, IReadOnlyList<string> tags, NamingContext ctx) {
			if (!ctx.Rng.Chance(HybridCoinRate)) return null; // 80% -> caller uses a real word
			var model = GetModel(pos, tags);
			if (model == null || !model.IsTrained) return null;
			return model.Generate(ctx.Rng, MarkovMinLen, MarkovMaxLen);
		}

		private MarkovModel GetModel(string pos, IReadOnlyList<string> tags) {
			string key = pos + "|" + (tags == null ? "" : string.Join(",", tags.OrderBy(t => t)));
			if (_markovCache.TryGetValue(key, out var m)) return m;
			var corpus = Lexicon.Corpus(pos, tags);
			if (corpus.Count < 6) { _markovCache[key] = null; return null; } // too small to learn
			m = new MarkovModel(3);
			m.Train(corpus);
			_markovCache[key] = m;
			return m;
		}

		// ---- uniqueness helpers ------------------------------------------------
		private static HashSet<string> Bucket(Dictionary<string, HashSet<string>> d, string b) {
			if (!d.TryGetValue(b, out var set)) { set = new HashSet<string>(); d[b] = set; }
			return set;
		}

		private static string Normalize(string s) {
			var sb = new StringBuilder(s.Length);
			foreach (char c in s.ToLowerInvariant())
				if (char.IsLetterOrDigit(c)) sb.Append(c);
				else if (c == ' ' && sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');
			return sb.ToString().Trim();
		}

		// soundex of the longest word + a coarse length bucket — cheap near-duplicate signal
		private static string FuzzyKey(string s) {
			string longest = s.Split(new[] { ' ', '\'', '-' }, StringSplitOptions.RemoveEmptyEntries)
							   .OrderByDescending(w => w.Length).FirstOrDefault() ?? s;
			int lenBucket = Normalize(s).Length / 4;
			return Soundex(longest) + "#" + lenBucket;
		}

		private static string Soundex(string word) {
			word = new string(word.Where(char.IsLetter).ToArray()).ToUpperInvariant();
			if (word.Length == 0) return "0000";
			char first = word[0];
			var sb = new StringBuilder();
			sb.Append(first);
			char prevCode = Code(first);
			for (int i = 1; i < word.Length && sb.Length < 4; i++) {
				char code = Code(word[i]);
				if (code != '0' && code != prevCode) sb.Append(code);
				prevCode = code;
			}
			while (sb.Length < 4) sb.Append('0');
			return sb.ToString();

			static char Code(char c) => c switch {
				'B' or 'F' or 'P' or 'V' => '1',
				'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => '2',
				'D' or 'T' => '3', 'L' => '4', 'M' or 'N' => '5', 'R' => '6', _ => '0'
			};
		}
	}
}
