// Systems/Naming/Core/NamingPrimitives.cs
// Godot-free core: RNG abstraction, word entries, and the generation context.
// NOTHING in this namespace may reference Godot.

using System;
using System.Collections.Generic;

namespace LabelMan.Naming {

	/// <summary>Deterministic randomness abstraction. The game adapter and the tuner
	/// each supply a concrete implementation seeded on a stream separate from GD.Rand.</summary>
	public interface IRandom {
		/// <summary>Uniform int in [0, maxExclusive).</summary>
		int Next(int maxExclusive);
		/// <summary>Uniform double in [0, 1).</summary>
		double NextDouble();
	}

	public sealed class DeterministicRandom : IRandom {
		private readonly Random _rng;
		public DeterministicRandom(int seed) { _rng = new Random(seed); }
		public DeterministicRandom(ulong seed)
			: this(unchecked((int)(seed ^ (seed >> 32)))) { }
		public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : _rng.Next(maxExclusive);
		public double NextDouble() => _rng.NextDouble();
	}

	public static class RandomExtensions {
		public static bool Chance(this IRandom rng, double p) => rng.NextDouble() < p;

		/// <summary>Inclusive integer range [min, max].</summary>
		public static int Range(this IRandom rng, int min, int max) {
			if (max < min) (min, max) = (max, min);
			return min + rng.Next(max - min + 1);
		}

		public static T Pick<T>(this IRandom rng, IReadOnlyList<T> list)
			=> list[rng.Next(list.Count)];
	}

	/// <summary>One lexicon word plus metadata. Words inherit tags from their group at load.
	/// The ontology (Layer 3) classifies the freeform <see cref="Tags"/> into the five axes below
	/// at load time — no separate tagging pass in the data files.</summary>
	public sealed class WordEntry {
		public string Word;
		public string Pos;
		public HashSet<string> Tags;
		public Dictionary<string, double> GenreAffinity; // genre name -> weight, optional
		public int? EraStart;
		public int? EraEnd;
		public double Weight = 1.0;

		// ---- ontology axes (populated by TagOntology.Classify at load; null/default if unclassified) ----
		public Bitset DomainBits;          // DOMAIN closure (self + ancestors) for O(1) tree-filter
		public HashSet<string> Moods;      // MOOD axis (intersection of Tags with the 19 moods)
		public int Register = -1;          // REGISTER 0..5, -1 = unset
		public string EraClass;            // ERA idiom bucket (early60s/mid60s/late60s/emerging:YYYY)
		public string LocaleClass;         // LOCALE (US/UK/Portuguese/Spanish/Jamaican), null = Neutral

		public bool HasTag(string t) => Tags != null && Tags.Contains(t);
	}

	/// <summary>Fixed-width bitset over ulong words. Used for DOMAIN-tag closures so a tree filter
	/// becomes a single bitwise-AND (doc 7 §3.1) instead of a runtime tree walk.</summary>
	public sealed class Bitset {
		private readonly ulong[] _w;
		public Bitset(int bits) { _w = new ulong[(Math.Max(1, bits) + 63) >> 6]; }
		private Bitset(ulong[] w) { _w = w; }
		public void Set(int i) { _w[i >> 6] |= 1UL << (i & 63); }
		public bool Get(int i) => (_w[i >> 6] & (1UL << (i & 63))) != 0;
		public void OrWith(Bitset o) { for (int i = 0; i < _w.Length; i++) _w[i] |= o._w[i]; }
		public bool Intersects(Bitset o) {
			int n = Math.Min(_w.Length, o._w.Length);
			for (int i = 0; i < n; i++) if ((_w[i] & o._w[i]) != 0) return true;
			return false;
		}
		public bool IsEmpty() { foreach (var x in _w) if (x != 0) return false; return true; }
		public Bitset Clone() => new Bitset((ulong[])_w.Clone());
	}

	/// <summary>The single parameter object threaded through every generation call.
	/// Uses plain strings so the Core stays decoupled from the game's enums.</summary>
	public sealed class NamingContext {
		public string Genre;                 // e.g. "Psychedelic"
		public int Year;
		public string ArtistType;            // e.g. "SoloFemale", "Band"
		public string RegionId;
		public string LabelArchetype;        // e.g. "SoulFactory"
		/// <summary>Style tags injected for <c>$style</c> in lexicon queries (the adapter maps
		/// the game's 51 genres down to a handful of naming styles: psych, soul, country...).</summary>
		public List<string> StyleTags = new();
		/// <summary>Named tag-sets addressable in grammar tag lists via <c>$key</c> (e.g. the
		/// adapter puts the rolled demographic tags under "name" and "surname").</summary>
		public Dictionary<string, List<string>> TagSets = new();
		/// <summary>Runtime literal values addressable as an untagged token, e.g. <c>{artist}</c>
		/// or a caller-supplied <c>{city}</c>. Only consulted when the token carries no tags.</summary>
		public Dictionary<string, string> Slots = new();
		public IRandom Rng;
		public Dictionary<string, string> Extras; // future fields without touching call sites

		public NamingContext Clone() => new NamingContext {
			Genre = Genre, Year = Year, ArtistType = ArtistType, RegionId = RegionId,
			LabelArchetype = LabelArchetype, StyleTags = new List<string>(StyleTags),
			TagSets = TagSets, Slots = Slots, Rng = Rng, Extras = Extras
		};
	}
}
