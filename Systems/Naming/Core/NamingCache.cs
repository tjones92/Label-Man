// Systems/Naming/Core/NamingCache.cs
// Layer 7 — the performance model. Bundles the L0 immutable models (ontology, mood matrix,
// inflection tables, genre library) and the L1 derived caches (filtered pools with prefix-sum
// weighted selection, bounded by era-epoch to keep the year dimension small). Also a Bloom-fronted
// collision registry so uniqueness is O(1) at scale. Caches are pure derivations — never
// serialized; only world state + RNG seed are saved (doc 7 §13). Godot-free.

using System;
using System.Collections.Generic;

namespace LabelMan.Naming {

	/// <summary>L0 bundle: the immutable, load-time models shared read-only across all generation.</summary>
	public sealed class NameModels {
		public TagOntology Ontology { get; }
		public MoodGraph Moods { get; }
		public Inflection Inflection { get; }
		public GenreLibrary Genres { get; }
		public BlendResolver Blend { get; }

		public NameModels(TagOntology ont = null, MoodGraph mood = null, Inflection infl = null, GenreLibrary genres = null) {
			Ontology = ont ?? new TagOntology();
			Moods = mood ?? new MoodGraph();
			Inflection = infl ?? new Inflection();
			Genres = genres ?? new GenreLibrary();
			Blend = new BlendResolver(Moods);
		}

		// Coarse era buckets collapse the 10-year cache dimension to 4 (doc 7 §4.3).
		public static string Epoch(int year) => year switch {
			<= 0 => "any",
			<= 1961 => "early",
			<= 1964 => "midEarly",
			<= 1966 => "mid",
			_ => "late",
		};
	}

	/// <summary>A memoized, prefix-summed candidate pool for one (pos, filter, epoch, locale, genre)
	/// context. First draw computes it (WARM); subsequent draws are an O(log n) binary search on the
	/// prefix-sum array (HOT) — doc 7 §4.</summary>
	public sealed class FilteredPool {
		private readonly WordEntry[] _words;
		private readonly double[] _cum;   // prefix sums
		public double Total { get; }
		public int Count => _words.Length;

		public FilteredPool(List<(WordEntry e, double w)> scored) {
			_words = new WordEntry[scored.Count];
			_cum = new double[scored.Count];
			double run = 0;
			for (int i = 0; i < scored.Count; i++) { _words[i] = scored[i].e; run += scored[i].w; _cum[i] = run; }
			Total = run;
		}

		public WordEntry Pick(double roll01) {
			if (_words.Length == 0) return null;
			double target = roll01 * Total;
			int lo = 0, hi = _cum.Length - 1;
			while (lo < hi) { int mid = (lo + hi) >> 1; if (_cum[mid] < target) lo = mid + 1; else hi = mid; }
			return _words[lo];
		}
	}

	/// <summary>Bounded LRU keyed by context string. Evict prior-epoch contexts at the year tick.</summary>
	public sealed class PoolCache {
		private readonly int _capacity;
		private readonly Dictionary<string, LinkedListNode<(string key, FilteredPool pool)>> _map = new();
		private readonly LinkedList<(string key, FilteredPool pool)> _lru = new();
		public PoolCache(int capacity = 4096) { _capacity = Math.Max(16, capacity); }

		public FilteredPool GetOrBuild(string key, Func<FilteredPool> build) {
			if (_map.TryGetValue(key, out var node)) { _lru.Remove(node); _lru.AddFirst(node); return node.Value.pool; }
			var pool = build();
			var n = new LinkedListNode<(string, FilteredPool)>((key, pool));
			_lru.AddFirst(n); _map[key] = n;
			if (_map.Count > _capacity) {
				var last = _lru.Last; _lru.RemoveLast(); _map.Remove(last.Value.key);
			}
			return pool;
		}
		public void Clear() { _map.Clear(); _lru.Clear(); }
		public int Count => _map.Count;
	}

	/// <summary>Bloom-fronted uniqueness registry. Most generated names are unique, so the Bloom
	/// negative fast-paths without touching the hash set (doc 7 §9). Separate namespaces; cross-
	/// namespace collisions are allowed (a band and a song may share a string).</summary>
	public sealed class CollisionRegistry {
		private sealed class Namespace {
			public readonly HashSet<string> Set = new(StringComparer.Ordinal);
			public ulong[] Bloom;
			public int Bits;
			public Namespace(int expected) { Bits = NextPow2(Math.Max(1024, expected * 10)); Bloom = new ulong[Bits >> 6]; }
		}
		private readonly Dictionary<string, Namespace> _ns = new(StringComparer.Ordinal);
		private readonly int _expected;
		public CollisionRegistry(int expectedPerNamespace = 50000) { _expected = expectedPerNamespace; }

		private Namespace Ns(string bucket) {
			if (!_ns.TryGetValue(bucket, out var n)) { n = new Namespace(_expected); _ns[bucket] = n; }
			return n;
		}

		public bool Contains(string bucket, string normalized) {
			var n = Ns(bucket);
			var (h1, h2) = Hashes(normalized, n.Bits);
			if (!BloomGet(n, h1) || !BloomGet(n, h2)) return false; // definite miss
			return n.Set.Contains(normalized);                       // confirm on Bloom hit
		}

		public void Add(string bucket, string normalized) {
			var n = Ns(bucket);
			var (h1, h2) = Hashes(normalized, n.Bits);
			BloomSet(n, h1); BloomSet(n, h2);
			n.Set.Add(normalized);
		}

		public void Clear() => _ns.Clear();

		private static bool BloomGet(Namespace n, int bit) => (n.Bloom[bit >> 6] & (1UL << (bit & 63))) != 0;
		private static void BloomSet(Namespace n, int bit) => n.Bloom[bit >> 6] |= 1UL << (bit & 63);
		private static (int, int) Hashes(string s, int bits) {
			int mask = bits - 1;
			uint h1 = 2166136261;                     // FNV-1a
			foreach (char c in s) { h1 ^= c; h1 *= 16777619; }
			uint h2 = 5381;                            // djb2
			foreach (char c in s) h2 = ((h2 << 5) + h2) ^ c;
			return ((int)(h1 & mask), (int)(h2 & mask));
		}
		private static int NextPow2(int x) { int p = 1; while (p < x) p <<= 1; return p; }
	}
}
