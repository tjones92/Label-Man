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
		private sealed class GroupFile { public List<Group> groups { get; set; } = new(); }
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
		}

		public void Add(WordEntry e) {
			_all.Add(e);
			if (!_byPos.TryGetValue(e.Pos, out var list)) { list = new List<WordEntry>(); _byPos[e.Pos] = list; }
			list.Add(e);
		}

		/// <summary>Append a single word at runtime (used by the tuner's "add word" button).</summary>
		public void AddWord(string word, string pos, IEnumerable<string> tags) {
			Add(new WordEntry {
				Word = word, Pos = pos,
				Tags = new HashSet<string>(tags ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase)
			});
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
