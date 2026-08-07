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
		// Layers 1–7 bundle. The lexicon is classified onto the ontology axes at construction so
		// ontology/mood/genre/inflection are all live for the grammar and the constraint templates.
		public NameModels Models { get; }
		public TemplateEngine Templates { get; }

		private readonly Dictionary<string, MarkovModel> _markovCache = new();
		// uniquenessBucket -> set of normalized keys / soundex keys
		private readonly Dictionary<string, HashSet<string>> _used = new();
		private readonly Dictionary<string, HashSet<string>> _usedFuzzy = new();
		// constraint-template sets (Layer 2), keyed by grammar-style symbol; loaded from templates.json
		private readonly Dictionary<string, List<ConstraintTemplate>> _constraintSets = new(StringComparer.Ordinal);

		public double HybridCoinRate = 0.20; // ~20% of markov slots become invented words
		public int MarkovMinLen = 4, MarkovMaxLen = 11;

		public NameEngine(Lexicon lexicon, Dictionary<string, List<GrammarEngine.Rule>> grammar, NameModels models = null) {
			Lexicon = lexicon;
			Models = models ?? new NameModels();
			Lexicon.ClassifyAll(Models.Ontology);                       // Layer 3: sort tags onto axes
			Grammar = new GrammarEngine(grammar, lexicon, this, Models.Inflection);
			Templates = new TemplateEngine(lexicon, Models.Ontology, Models.Moods, Models.Inflection, this);
		}

		public GenreProfile Profile(NamingContext ctx) => Models.Genres.Get(ctx?.Genre);

		/// <summary>Register a constraint-template set (Layer 2) under a symbol the adapter can request.</summary>
		public void AddConstraintSet(string symbol, IEnumerable<ConstraintTemplate> templates) {
			var list = new List<ConstraintTemplate>();
			foreach (var t in templates) { t.Compile(); list.Add(t); }
			_constraintSets[symbol] = list;
		}
		public bool HasConstraintSet(string symbol) => _constraintSets.ContainsKey(symbol);
		public IEnumerable<string> ConstraintSymbols => _constraintSets.Keys;

		/// <summary>The distinct (pos, filter) slot pools a constraint set draws from, with the words
		/// each filter selects — backs the tuner's dictionary panel for constraint categories.</summary>
		public sealed class SlotGroup { public string Pos; public string Label; public List<string> Tags; public List<WordEntry> Words; }
		public IReadOnlyList<SlotGroup> ConstraintSlotGroups(string symbol) {
			if (!_constraintSets.TryGetValue(symbol, out var templates)) return System.Array.Empty<SlotGroup>();
			var seen = new Dictionary<string, SlotGroup>(StringComparer.Ordinal);
			foreach (var t in templates) {
				if (!t.Compiled) t.Compile();
				foreach (var kv in t.Slots) {
					var spec = kv.Value;
					if (string.IsNullOrEmpty(spec.Pos) || spec.Markov) continue;
					string key = spec.Pos + "|" + spec.Filter.Signature;
					if (seen.ContainsKey(key)) continue;
					var words = Lexicon.Pool(spec.Pos)
						.Where(e => spec.Filter.IsEmpty || spec.Filter.Matches(e, Models.Ontology)).ToList();
					seen[key] = new SlotGroup {
						Pos = spec.Pos,
						Tags = spec.Filter.ReferencedTags.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
						Label = spec.Filter.IsEmpty ? spec.Pos : $"{spec.Pos} [{spec.Filter.Label}]",
						Words = words
					};
				}
			}
			return seen.Values.ToList();
		}

		/// <summary>Each entry's exact tag-set for a (pos, word) — so the tuner can tombstone every
		/// variant precisely (the union from TagsForWord matches no single entry).</summary>
		public IReadOnlyList<List<string>> EntryTagSetsForWord(string pos, string word) {
			var seen = new HashSet<string>(StringComparer.Ordinal);
			var outp = new List<List<string>>();
			foreach (var e in Lexicon.Pool(pos))
				if (string.Equals(e.Word, word, StringComparison.OrdinalIgnoreCase)) {
					var tags = (e.Tags ?? Enumerable.Empty<string>()).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
					string key = string.Join(",", tags).ToLowerInvariant();
					if (seen.Add(key)) outp.Add(tags);
				}
			return outp;
		}

		/// <summary>Distinct ontology/style tags stored on a word's entries of a given pos (tuner tag view).</summary>
		public IReadOnlyList<string> TagsForWord(string pos, string word) {
			var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var e in Lexicon.Pool(pos))
				if (string.Equals(e.Word, word, StringComparison.OrdinalIgnoreCase) && e.Tags != null)
					foreach (var tg in e.Tags) tags.Add(tg);
			return tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
		}

		/// <summary>Fill from a constraint-template set: gate + satisfiability prune, weighted pick,
		/// escalating fallback to a simpler template, then the grammar as last resort. Returns null
		/// only if the symbol is unknown.</summary>
		public string FillConstraint(string symbol, NamingContext ctx) {
			if (!_constraintSets.TryGetValue(symbol, out var all)) return null;
			var genre = Models.Genres.Get(ctx.Genre);
			// candidate list: gate + satisfiable, sorted simplest-last for graceful fallback
			var cands = new List<ConstraintTemplate>();
			foreach (var t in all)
				if (Templates.GatePasses(t, ctx, genre) && Templates.SatisfiableFor(t, genre)) cands.Add(t);
			if (cands.Count == 0) return "";
			// weighted pick, then fall through to progressively simpler candidates
			var ordered = SortByWeight(cands, ctx.Rng);
			foreach (var t in ordered) {
				string s = Templates.Fill(t, ctx, genre);
				if (!string.IsNullOrEmpty(s)) return s;
			}
			return "";
		}

		private static List<ConstraintTemplate> SortByWeight(List<ConstraintTemplate> cands, IRandom rng) {
			// weighted shuffle: draw without replacement by weight, so the first pick honors weights
			var pool = new List<ConstraintTemplate>(cands);
			var outp = new List<ConstraintTemplate>(cands.Count);
			while (pool.Count > 0) {
				double total = 0; foreach (var t in pool) total += t.Weight <= 0 ? 1 : t.Weight;
				double roll = rng.NextDouble() * total; int idx = pool.Count - 1;
				for (int i = 0; i < pool.Count; i++) { roll -= pool[i].Weight <= 0 ? 1 : pool[i].Weight; if (roll <= 0) { idx = i; break; } }
				outp.Add(pool[idx]); pool.RemoveAt(idx);
			}
			return outp;
		}

		public IEnumerable<string> AvailableSymbols => Grammar.Symbols.OrderBy(s => s);

		/// <summary>Expand a symbol once, no uniqueness enforcement (used by the tuner spin).</summary>
		public string ExpandOnce(string symbol, NamingContext ctx) => Grammar.Expand(symbol, ctx);

		/// <summary>Constraint-aware single expansion (no uniqueness): a registered constraint set for
		/// the symbol wins, else the grammar. This is what the tuner spins so it matches the game,
		/// which routes through <see cref="Generate"/> (also constraint-aware).</summary>
		public string ExpandRouted(string symbol, NamingContext ctx) {
			if (_constraintSets.ContainsKey(symbol)) {
				string s = FillConstraint(symbol, ctx);
				if (!string.IsNullOrEmpty(s)) return s;
			}
			return Grammar.HasSymbol(symbol) ? Grammar.Expand(symbol, ctx) : symbol;
		}

		/// <summary>Expand with uniqueness. <paramref name="bucket"/> scopes uniqueness (e.g.
		/// "artist", "label", or "song|Artist Name"). Near-duplicate guard applies when
		/// <paramref name="nearDup"/> is set. Re-rolls a different pattern before giving up.</summary>
		public string Generate(string symbol, NamingContext ctx, string bucket, bool nearDup, int attempts = 40) {
			// A registered Layer-2 constraint set takes precedence over a grammar symbol of the same
			// name; the grammar (if it also defines the symbol) remains a per-attempt safety net.
			if (_constraintSets.ContainsKey(symbol)) {
				bool hasGrammar = Grammar.HasSymbol(symbol);
				return GenerateUnique(() => {
					string s = FillConstraint(symbol, ctx);
					return !string.IsNullOrEmpty(s) ? s : (hasGrammar ? Grammar.Expand(symbol, ctx) : s);
				}, symbol, bucket, nearDup, attempts, ctx);
			}
			if (!Grammar.HasSymbol(symbol)) return symbol; // authoring error surfaces visibly
			return GenerateUnique(() => Grammar.Expand(symbol, ctx), symbol, bucket, nearDup, attempts, ctx);
		}

		/// <summary>Shared uniqueness loop: draw candidates from <paramref name="produce"/>, reject
		/// exact (and optionally near-) duplicates within a bucket, disambiguate as a last resort.</summary>
		private string GenerateUnique(Func<string> produce, string symbol, string bucket, bool nearDup, int attempts, NamingContext ctx) {
			var exact = Bucket(_used, bucket);
			var fuzzy = nearDup ? Bucket(_usedFuzzy, bucket) : null;

			string last = null;
			for (int i = 0; i < attempts; i++) {
				string candidate = produce();
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
			string basename = last ?? produce();
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
