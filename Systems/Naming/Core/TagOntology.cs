// Systems/Naming/Core/TagOntology.cs
// Layer 3 — the semantic backbone. Five axes let one tagged word serve every genre:
//   DOMAIN   hierarchical tree (a filter for a parent matches all descendants) — precomputed to
//            per-word closure bitsets so a filter is one bitwise-AND, not a tree walk (doc 7 §3.1).
//   MOOD     flat set (the 19 mood-graph values).
//   REGISTER single ordered value slang<plain<poetic<ornate<archaic<formal.
//   ERA      idiom bucket; ERA/LOCALE gate draws so decade-drift & orthography are emergent.
//   LOCALE   single culture/orthography value.
// The ontology reads a word's EXISTING freeform tags and sorts them onto these axes at load —
// there is no second tagging pass in the data files. Godot-free.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LabelMan.Naming {

	public sealed class TagOntology {
		// DOMAIN tree: parent -> children (doc 3 §3). ROOT is implicit.
		private static readonly Dictionary<string, string[]> DefaultTree = new(StringComparer.OrdinalIgnoreCase) {
			["nature"]   = new[] { "celestial", "nautical", "weather", "terrain", "flora", "fauna" },
			["human"]    = new[] { "body", "romance", "kin", "emotion", "identity" },
			["place"]    = new[] { "urban", "rural", "regional", "domestic", "mythic" },
			["motion"]   = new[] { "travel", "dance", "vehicle" },
			["time"]     = new[] { "diurnal", "seasonal", "temporal" },
			["spirit"]   = new[] { "faith", "cosmic", "mystical" },
			["material"] = new[] { "gem", "luxury", "grit", "candy", "mechanical" },
			["social"]   = new[] { "party", "conflict", "vice", "protest" },
			["abstract"] = new[] { "virtue", "fate", "nonsense" },
		};

		private static readonly string[] Registers = { "slang", "plain", "poetic", "ornate", "archaic", "formal" };
		private static readonly HashSet<string> Locales = new(StringComparer.OrdinalIgnoreCase)
			{ "us", "uk", "portuguese", "spanish", "jamaican" };
		// ERA idioms; "emerging:1968"-style are matched by prefix.
		private static readonly HashSet<string> EraClasses = new(StringComparer.OrdinalIgnoreCase)
			{ "timeless", "early60s", "mid60s", "late60s" };

		private readonly Dictionary<string, int> _domainIndex = new(StringComparer.OrdinalIgnoreCase);
		private readonly List<string> _domainNodes = new();
		private Bitset[] _ancestorsInclusive;      // node -> self + all ancestors
		private readonly Dictionary<string, string> _parent = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> _moodNames;

		public int DomainNodeCount => _domainNodes.Count;
		public bool IsDomainTag(string t) => t != null && _domainIndex.ContainsKey(t);
		public bool IsMoodTag(string t) => t != null && _moodNames.Contains(t);

		public TagOntology(Dictionary<string, string[]> tree = null, IEnumerable<string> moodNames = null) {
			_moodNames = new HashSet<string>(moodNames ?? MoodGraph.Moods, StringComparer.OrdinalIgnoreCase);
			BuildTree(tree ?? DefaultTree);
		}

		private void BuildTree(Dictionary<string, string[]> tree) {
			// collect all nodes (parents + children)
			void Reg(string n) { if (!_domainIndex.ContainsKey(n)) { _domainIndex[n] = _domainNodes.Count; _domainNodes.Add(n); } }
			foreach (var kv in tree) { Reg(kv.Key); foreach (var c in kv.Value) { Reg(c); _parent[c] = kv.Key; } }

			_ancestorsInclusive = new Bitset[_domainNodes.Count];
			for (int i = 0; i < _domainNodes.Count; i++) {
				var bs = new Bitset(_domainNodes.Count);
				string cur = _domainNodes[i];
				while (cur != null) {
					bs.Set(_domainIndex[cur]);
					_parent.TryGetValue(cur, out cur);
				}
				_ancestorsInclusive[i] = bs;
			}
		}

		/// <summary>Sort a word's freeform tags onto the five axes and precompute its DOMAIN closure.
		/// Idempotent; leaves non-ontology tags (e.g. style tags "psych"/"soul") untouched in Tags.</summary>
		public void Classify(WordEntry e) {
			if (e.Tags == null) { e.DomainBits = new Bitset(_domainNodes.Count); return; }
			var dom = new Bitset(_domainNodes.Count);
			HashSet<string> moods = null;
			foreach (var t in e.Tags) {
				if (_domainIndex.TryGetValue(t, out var di)) { dom.OrWith(_ancestorsInclusive[di]); continue; }
				if (_moodNames.Contains(t)) { (moods ??= new(StringComparer.OrdinalIgnoreCase)).Add(t); continue; }
				int ri = Array.FindIndex(Registers, r => r.Equals(t, StringComparison.OrdinalIgnoreCase));
				if (ri >= 0) { e.Register = ri; continue; }
				if (Locales.Contains(t)) { e.LocaleClass = t; continue; }
				if (EraClasses.Contains(t) || t.StartsWith("emerging:", StringComparison.OrdinalIgnoreCase))
					e.EraClass = t;
			}
			e.DomainBits = dom;
			e.Moods = moods;
		}

		/// <summary>Does the word satisfy a single DOMAIN filter tag (parent tags match descendants)?
		/// Returns null when <paramref name="tag"/> is not a domain node, so callers fall back to
		/// exact-tag matching for non-ontology tags.</summary>
		public bool? DomainMatch(WordEntry e, string tag) {
			if (!_domainIndex.TryGetValue(tag, out var idx)) return null;
			return e.DomainBits != null && e.DomainBits.Get(idx);
		}

		/// <summary>Register drawn from a genre archaism target as a soft gaussian pick (doc 3 §5).</summary>
		public static int DrawRegisterCenter(double archaismLevel) =>
			(int)Math.Round(Math.Clamp(archaismLevel, 0, 1) * 5);

		/// <summary>ERA gate: is a word idiomatic in the given year? timeless/unset always true;
		/// emerging:YYYY has a hard floor; the coarse buckets map to year windows (doc 3 §6).</summary>
		public static bool EraEligible(string eraClass, int year) {
			if (string.IsNullOrEmpty(eraClass) || eraClass.Equals("timeless", StringComparison.OrdinalIgnoreCase))
				return true;
			if (year <= 0) return true;
			if (eraClass.StartsWith("emerging:", StringComparison.OrdinalIgnoreCase))
				return int.TryParse(eraClass.Substring(9), out var y0) ? year >= y0 : true;
			return eraClass.ToLowerInvariant() switch {
				"early60s" => year <= 1963,
				"mid60s"   => year >= 1962 && year <= 1966,
				"late60s"  => year >= 1966,
				_ => true
			};
		}

		public void LoadJson(string json) {
			if (string.IsNullOrWhiteSpace(json)) return;
			using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
			if (!doc.RootElement.TryGetProperty("tree", out var tree)) return;
			var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			foreach (var node in tree.EnumerateObject())
				map[node.Name] = node.Value.EnumerateArray().Select(x => x.GetString()).ToArray();
			_domainIndex.Clear(); _domainNodes.Clear(); _parent.Clear();
			BuildTree(map);
		}
	}
}
