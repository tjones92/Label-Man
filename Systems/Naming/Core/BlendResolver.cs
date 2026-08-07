// Systems/Naming/Core/BlendResolver.cs
// Layer 5 — blend resolution. Produces a coherent third voice from two parents, not mud:
//   scalars lerp, sets union, categoricals defer to dominance (doc 5 §3). A few dimensions need
// max/min instead of lerp (drawl retention, hook length) via BlendPolicy. Mood sets get a
// connectivity repair (lower threshold, else inject a bridge mood) so distant blends stay coherent
// or are cleanly rejected. Static blends resolve once (cache as a GenreProfile); dynamic blends and
// succession (mix = f(year)) reuse the same math. Godot-free.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LabelMan.Naming {

	public enum Dominance { PrimaryLeads, Balanced, SecondaryLeads }
	public enum MergeRule { Lerp, Max, Min, PrimaryOnly, SecondaryOnly }

	public sealed class BlendPolicy {
		public Dictionary<int, MergeRule> VoiceOverrides = new();   // voice dim index -> rule
		public double MoodFloor = 0.15;                             // below this even a bridge can't save it
		public double LateSkew = 0.15;                              // hybrids emerge later than both parents
		public MergeRule Default = MergeRule.Lerp;
	}

	public sealed class BlendResolver {
		private readonly MoodGraph _mood;
		public BlendResolver(MoodGraph mood) { _mood = mood; }

		/// <summary>Resolve a blend into a frozen GenreProfile, or null if musically incoherent
		/// (caller falls back to pure primary — doc 5 §14).</summary>
		public GenreProfile Resolve(GenreProfile primary, GenreProfile secondary, double mix,
									Dominance dominance, int year = 0, BlendPolicy policy = null) {
			policy ??= new BlendPolicy();
			mix = Math.Clamp(mix, 0, 1);
			var r = new GenreProfile { Id = $"{primary.Id}+{secondary.Id}@{mix:0.00}" };

			// voice — per-dim rule
			for (int i = 0; i < VoiceVector.Dims.Length; i++) {
				double a = primary.Voice[i], b = secondary.Voice[i];
				r.Voice[i] = (policy.VoiceOverrides.TryGetValue(i, out var rule) ? rule : policy.Default) switch {
					MergeRule.Max => Math.Max(a, b),
					MergeRule.Min => Math.Min(a, b),
					MergeRule.PrimaryOnly => a,
					MergeRule.SecondaryOnly => b,
					_ => a * (1 - mix) + b * mix,
				};
			}
			r.Voice = r.Voice.Clamped();

			// affinities — weighted union
			r.DomainAffinity = WeightedUnion(primary.DomainAffinity, secondary.DomainAffinity, mix);
			var moodW = WeightedUnion(primary.MoodAffinity, secondary.MoodAffinity, mix);

			// suppression — union (a blend inherits both parents' taboos)
			r.Suppress = new HashSet<string>(primary.Suppress, StringComparer.OrdinalIgnoreCase);
			r.Suppress.UnionWith(secondary.Suppress);

			// mood threshold + connectivity repair
			double threshold = primary.MoodThreshold * (1 - mix) + secondary.MoodThreshold * mix;
			var moods = moodW.Keys.ToList();
			if (!_mood.IsConnectedAbove(moods, threshold)) {
				double lowered = HighestConnectedThreshold(moods, threshold);
				if (lowered >= policy.MoodFloor) threshold = lowered;
				else {
					string bridge = _mood.FindBridge(DominantMood(primary.MoodAffinity), DominantMood(secondary.MoodAffinity), 0.4);
					if (bridge != null) { moodW[bridge] = (primary.MoodThreshold + secondary.MoodThreshold) * 0.5; threshold = policy.MoodFloor; }
					else return null;                               // parents musically alien -> reject
				}
			}
			r.MoodAffinity = moodW; r.MoodThreshold = threshold;

			// categoricals — dominance winner
			bool secWins = dominance == Dominance.SecondaryLeads;
			r.Orthography = secWins ? secondary.Orthography : primary.Orthography;
			if (r.Orthography == Locale.Neutral) r.Orthography = secWins ? primary.Orthography : secondary.Orthography;
			r.Diacritics = primary.Diacritics || secondary.Diacritics;

			// era curve — intersect (min) with a late-skew emergence bonus (hybrids are late)
			r.EraCurve = new double[10];
			for (int y = 0; y < 10; y++) {
				double bonus = 1.0 + policy.LateSkew * (y / 9.0);
				r.EraCurve[y] = Math.Min(primary.EraCurve[y], secondary.EraCurve[y]) * bonus;
			}
			Normalize(r.EraCurve);

			// final validator: mood set must be connected above the (possibly repaired) threshold
			if (!_mood.IsConnectedAbove(r.MoodAffinity.Keys.ToList(), r.MoodThreshold)) return null;
			return r;
		}

		/// <summary>Succession: mix rises smoothly across a window (Ska->Rocksteady->Reggae, doc 5 §11).</summary>
		public GenreProfile ResolveSuccession(GenreProfile from, GenreProfile to, int year,
											  int windowStart, int windowEnd, BlendPolicy policy = null) {
			double mix = Smoothstep(windowStart, windowEnd, year);
			return Resolve(from, to, mix, Dominance.Balanced, year, policy) ?? from;
		}

		public static double Smoothstep(double a, double b, double x) {
			if (b <= a) return x >= b ? 1 : 0;
			double t = Math.Clamp((x - a) / (b - a), 0, 1);
			return t * t * (3 - 2 * t);
		}

		private static Dictionary<string, double> WeightedUnion(Dictionary<string, double> a, Dictionary<string, double> b, double mix) {
			var m = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
			foreach (var kv in a) m[kv.Key] = kv.Value * (1 - mix);
			foreach (var kv in b) m[kv.Key] = (m.TryGetValue(kv.Key, out var w) ? w : 0) + kv.Value * mix;
			return m;
		}

		private static string DominantMood(Dictionary<string, double> moodAff) =>
			moodAff.Count == 0 ? null : moodAff.OrderByDescending(kv => kv.Value).First().Key;

		private double HighestConnectedThreshold(IReadOnlyCollection<string> moods, double start) {
			for (double t = start; t >= 0.1; t -= 0.05) if (_mood.IsConnectedAbove(moods, t)) return t;
			return 0.0;
		}

		private static void Normalize(double[] c) {
			double sum = c.Sum(); if (sum <= 0) { for (int i = 0; i < c.Length; i++) c[i] = 1.0; return; }
			double mean = sum / c.Length; for (int i = 0; i < c.Length; i++) c[i] /= mean; // mean-normalize to ~1.0 scale
		}

		/// <summary>Template interleave (doc 5 §8): categorical, so merge two weighted menus by
		/// dominance share rather than averaging — every emitted title is a clean single-parent shape.</summary>
		public static List<(T item, double w)> InterleaveTemplates<T>(
				IReadOnlyList<(T item, double w)> primary, IReadOnlyList<(T item, double w)> secondary,
				Dominance dominance, double mix, Func<T, bool> secondarySatisfiable = null) {
			(double ps, double ss) = dominance switch {
				Dominance.PrimaryLeads => (0.75, 0.25),
				Dominance.SecondaryLeads => (0.25, 0.75),
				_ => (1 - mix, mix),
			};
			var pool = new List<(T, double)>();
			foreach (var (item, w) in primary) pool.Add((item, w * ps));
			foreach (var (item, w) in secondary) if (secondarySatisfiable == null || secondarySatisfiable(item)) pool.Add((item, w * ss));
			double tot = pool.Sum(x => x.Item2);
			return tot <= 0 ? pool : pool.Select(x => (x.Item1, x.Item2 / tot)).ToList();
		}
	}
}
