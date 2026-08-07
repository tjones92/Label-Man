// Systems/Naming/Core/Lexicon.cs
// Tagged word database. Loaded from a grouped JSON file; queried by part-of-speech + tags
// with soft biasing by genre affinity and era. Never throws, never returns "Unknown".

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LabelMan.Naming {

	public sealed class Lexicon {
		private readonly List<WordEntry> _all = new();
		private readonly Dictionary<string, List<WordEntry>> _byPos = new();

		public int Count => _all.Count;
		public IReadOnlyList<WordEntry> All => _all;
		public IEnumerable<string> PartsOfSpeech => _byPos.Keys;

		// ---- JSON shape -------------------------------------------------------
		private sealed class GroupFile {
			public List<Group> groups { get; set; } = new();
			// Optional tombstones (used by the tuner's user-overlay file) that delete matching
			// base words after the groups are merged. Lets the tuner "delete" a curated word
			// without editing the pristine base lexicon.json.
			public List<Removal> remove { get; set; }
		}
		private sealed class Group {
			public string pos { get; set; }
			public List<string> tags { get; set; } = new();
			public List<string> words { get; set; } = new();
			public Dictionary<string, double> genreAffinity { get; set; }
			public int? eraStart { get; set; }
			public int? eraEnd { get; set; }
			public double? weight { get; set; }
			public string source { get; set; } // provenance only
		}
		private sealed class Removal {
			public string pos { get; set; }
			public List<string> tags { get; set; }
			public string word { get; set; }
		}

		private static readonly JsonSerializerOptions JsonOpts = new() {
			PropertyNameCaseInsensitive = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true
		};

		public static Lexicon LoadFromJson(string json) {
			var lex = new Lexicon();
			lex.AppendJson(json);
			return lex;
		}

		/// <summary>Merge additional groups (e.g. a user-overrides file) into this lexicon.</summary>
		public void AppendJson(string json) {
			if (string.IsNullOrWhiteSpace(json)) return;
			var file = JsonSerializer.Deserialize<GroupFile>(json, JsonOpts) ?? new GroupFile();
			foreach (var g in file.groups) {
				if (string.IsNullOrWhiteSpace(g.pos) || g.words == null) continue;
				var tags = new HashSet<string>(g.tags ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
				foreach (var w in g.words) {
					if (string.IsNullOrEmpty(w)) continue;
					Add(new WordEntry {
						Word = w,
						Pos = g.pos,
						Tags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase),
						GenreAffinity = g.genreAffinity,
						EraStart = g.eraStart,
						EraEnd = g.eraEnd,
						Weight = g.weight ?? 1.0
					});
				}
			}
			// Apply tombstones last so a user-overlay file can delete base words.
			if (file.remove != null)
				foreach (var r in file.remove)
					if (!string.IsNullOrEmpty(r.word)) RemoveWord(r.word, r.pos, r.tags);
		}

		public void Add(WordEntry e) {
			_all.Add(e);
			if (!_byPos.TryGetValue(e.Pos, out var list)) { list = new List<WordEntry>(); _byPos[e.Pos] = list; }
			list.Add(e);
		}

		/// <summary>The full candidate pool for a part of speech (empty list if unknown). Exposed so
		/// the Layer-2 template engine can apply ontology filters + mood biasing + genre affinity itself.</summary>
		public IReadOnlyList<WordEntry> Pool(string pos) =>
			_byPos.TryGetValue(pos, out var list) ? list : (IReadOnlyList<WordEntry>)Array.Empty<WordEntry>();

		/// <summary>Collapse redundant entries — same pos, same tag-set, same word ignoring case
		/// (e.g. "Storm" and "storm" both tagged [weather,ominous] from two overlapping files). Keeps
		/// the first-seen casing. Call after all AppendJson, before ClassifyAll. Returns count removed.</summary>
		public int Dedupe() {
			var seen = new HashSet<string>(StringComparer.Ordinal);
			int removed = _all.RemoveAll(e => {
				string tags = e.Tags == null ? "" : string.Join(",", e.Tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
				string key = (e.Pos ?? "").ToLowerInvariant() + "|" + (e.Word ?? "").ToLowerInvariant() + "|" + tags.ToLowerInvariant();
				return !seen.Add(key);
			});
			if (removed > 0) foreach (var list in _byPos.Values) {
				var keep = new HashSet<WordEntry>(_all);
				list.RemoveAll(e => !keep.Contains(e));
			}
			return removed;
		}

		/// <summary>Sort every word's freeform tags onto the ontology's five axes and precompute its
		/// DOMAIN closure bitset. Call once after loading (and after any AppendJson).</summary>
		public void ClassifyAll(TagOntology ontology) {
			if (ontology == null) return;
			foreach (var e in _all) ontology.Classify(e);
		}

		/// <summary>Append a single word at runtime (used by the tuner's "add word" button).</summary>
		public void AddWord(string word, string pos, IEnumerable<string> tags) {
			Add(new WordEntry {
				Word = word, Pos = pos,
				Tags = new HashSet<string>(tags ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase)
			});
		}

		/// <summary>All entries of a pos carrying <em>exactly</em> the given tags (order-independent).
		/// Used by the tuner's dictionary view so the user sees precisely the group a token queries.
		/// Pass an empty/null tag list to match the untagged group.</summary>
		public IReadOnlyList<WordEntry> Entries(string pos, IReadOnlyList<string> tags) {
			if (!_byPos.TryGetValue(pos, out var pool)) return System.Array.Empty<WordEntry>();
			var want = new HashSet<string>(tags ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
			return pool.Where(e => (e.Tags?.Count ?? 0) == want.Count && want.All(e.HasTag)).ToList();
		}

		/// <summary>Remove every entry matching word (+ optional pos/tags scope). Returns the count
		/// removed. Tags, when given, must match exactly; when null, pos-only (or word-only) scope.</summary>
		public int RemoveWord(string word, string pos = null, IReadOnlyList<string> tags = null) {
			if (string.IsNullOrEmpty(word)) return 0;
			HashSet<string> want = tags != null ? new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase) : null;
			bool Match(WordEntry e) =>
				string.Equals(e.Word, word, StringComparison.OrdinalIgnoreCase)
				&& (pos == null || string.Equals(e.Pos, pos, StringComparison.OrdinalIgnoreCase))
				&& (want == null || ((e.Tags?.Count ?? 0) == want.Count && want.All(e.HasTag)));
			int removed = _all.RemoveAll(Match);
			if (removed > 0 && pos != null && _byPos.TryGetValue(pos, out var list)) list.RemoveAll(Match);
			else if (removed > 0) foreach (var list2 in _byPos.Values) list2.RemoveAll(Match);
			return removed;
		}

		/// <summary>Query a word. Filters by pos then tags, biases by genre affinity + era,
		/// and degrades gracefully (relax tags -> pos-only -> any) so it always returns something.</summary>
		public string Query(string pos, IReadOnlyList<string> requiredTags, NamingContext ctx) {
			if (!_byPos.TryGetValue(pos, out var pool) || pool.Count == 0) {
				// Unknown pos: fall back to the whole lexicon rather than emitting a marker.
				pool = _all;
				if (pool.Count == 0) return "";
			}

			IReadOnlyList<WordEntry> candidates = pool;
			if (requiredTags != null && requiredTags.Count > 0) {
				var all = pool.Where(e => requiredTags.All(e.HasTag)).ToList();
				if (all.Count == 0) all = pool.Where(e => requiredTags.Any(e.HasTag)).ToList(); // relax: any tag
				if (all.Count > 0) candidates = all;                                            // else: pos-only
			}

			var chosen = WeightedPick(candidates, ctx);
			return chosen?.Word ?? "";
		}

		/// <summary>The raw candidate list for a query — used by the Markov trainer as a corpus.</summary>
		public List<string> Corpus(string pos, IReadOnlyList<string> requiredTags) {
			if (!_byPos.TryGetValue(pos, out var pool) || pool.Count == 0) return new List<string>();
			IEnumerable<WordEntry> q = pool;
			if (requiredTags != null && requiredTags.Count > 0) {
				var strict = pool.Where(e => requiredTags.All(e.HasTag)).ToList();
				q = strict.Count > 0 ? strict : pool.Where(e => requiredTags.Any(e.HasTag));
			}
			return q.Select(e => e.Word).ToList();
		}

		private WordEntry WeightedPick(IReadOnlyList<WordEntry> candidates, NamingContext ctx) {
			if (candidates.Count == 0) return null;
			double total = 0;
			var weights = new double[candidates.Count];
			for (int i = 0; i < candidates.Count; i++) {
				var e = candidates[i];
				double w = e.Weight <= 0 ? 1.0 : e.Weight;
				if (e.GenreAffinity != null && ctx?.Genre != null &&
					e.GenreAffinity.TryGetValue(ctx.Genre, out var aff))
					w *= Math.Max(0.0001, aff);
				if ((e.EraStart.HasValue || e.EraEnd.HasValue) && ctx != null && ctx.Year > 0) {
					bool inEra = (!e.EraStart.HasValue || ctx.Year >= e.EraStart.Value)
							  && (!e.EraEnd.HasValue || ctx.Year <= e.EraEnd.Value);
					if (!inEra) w *= 0.12; // fade out-of-era words, don't hard-exclude
				}
				weights[i] = w; total += w;
			}
			double roll = (ctx?.Rng?.NextDouble() ?? 0.5) * total;
			for (int i = 0; i < candidates.Count; i++) {
				roll -= weights[i];
				if (roll <= 0) return candidates[i];
			}
			return candidates[candidates.Count - 1];
		}
	}
}
