// Systems/Naming/Core/MarkovModel.cs
// Order-3 letter Markov model with backoff to order-2/1, generate-and-score, no dead-ends.
// Used ONLY to coin invented words (obscure nouns, surnames, label coinages), never whole names.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LabelMan.Naming {

	public sealed class MarkovModel {
		private const char Start = '^';
		private const char End = '$';
		private readonly int _maxOrder;
		// order -> (context -> next chars)
		private readonly Dictionary<int, Dictionary<string, List<char>>> _tables = new();

		public MarkovModel(int maxOrder = 3) {
			_maxOrder = Math.Max(1, maxOrder);
			for (int o = 1; o <= _maxOrder; o++) _tables[o] = new Dictionary<string, List<char>>();
		}

		public void Train(IEnumerable<string> words) {
			foreach (var raw in words) {
				if (string.IsNullOrWhiteSpace(raw)) continue;
				string w = new string(Start, _maxOrder) + raw.Trim().ToLowerInvariant() + End;
				for (int i = _maxOrder; i < w.Length; i++) {
					char next = w[i];
					for (int o = 1; o <= _maxOrder; o++) {
						string ctx = w.Substring(i - o, o);
						var tbl = _tables[o];
						if (!tbl.TryGetValue(ctx, out var list)) { list = new List<char>(); tbl[ctx] = list; }
						list.Add(next);
					}
				}
			}
		}

		public bool IsTrained => _tables[1].Count > 0;

		/// <summary>Generate up to <paramref name="candidateCount"/> words and return the best-scoring
		/// one within [minLen,maxLen]. Returns null if nothing acceptable was produced.</summary>
		public string Generate(IRandom rng, int minLen, int maxLen, int candidateCount = 8) {
			string best = null; double bestScore = double.NegativeInfinity;
			for (int c = 0; c < candidateCount; c++) {
				string w = BuildOne(rng, maxLen);
				if (w == null || w.Length < minLen || w.Length > maxLen) continue;
				double s = Score(w);
				if (s <= 0) continue; // rejected (triple consonant, no vowel, etc.)
				if (s > bestScore) { bestScore = s; best = w; }
			}
			return best == null ? null : Capitalize(best);
		}

		private string BuildOne(IRandom rng, int maxLen) {
			var sb = new System.Text.StringBuilder();
			string history = new string(Start, _maxOrder);
			int guard = maxLen + 4;
			while (sb.Length < guard) {
				char next = NextChar(rng, history);
				if (next == End || next == '\0') break;
				sb.Append(next);
				history = (history + next);
				if (history.Length > _maxOrder) history = history.Substring(history.Length - _maxOrder);
			}
			return sb.Length == 0 ? null : sb.ToString();
		}

		// Order backoff: try highest order context, fall back to shorter ones.
		private char NextChar(IRandom rng, string history) {
			for (int o = Math.Min(_maxOrder, history.Length); o >= 1; o--) {
				string ctx = history.Substring(history.Length - o);
				if (_tables[o].TryGetValue(ctx, out var list) && list.Count > 0)
					return list[rng.Next(list.Count)];
			}
			return End;
		}

		// ---- scoring: reward pronounceable, punish junk. <= 0 means reject. -------------
		private static readonly HashSet<char> Vowels = new() { 'a', 'e', 'i', 'o', 'u', 'y' };

		private static double Score(string w) {
			if (string.IsNullOrEmpty(w)) return 0;
			int vowels = 0, maxConsonantRun = 0, run = 0;
			foreach (char ch in w) {
				if (Vowels.Contains(ch)) { vowels++; run = 0; }
				else { run++; if (run > maxConsonantRun) maxConsonantRun = run; }
			}
			if (vowels == 0) return 0;                 // must be sayable
			if (maxConsonantRun >= 3) return 0;        // no "schtr" clusters
			if (End.ToString() == w || w.Contains(Start)) return 0;
			int syllables = CountVowelGroups(w);
			double vowelBalance = 1.0 - Math.Abs((vowels / (double)w.Length) - 0.42); // prefer ~42% vowels
			return 1.0 + vowelBalance + Math.Min(syllables, 4) * 0.05;
		}

		private static int CountVowelGroups(string w) {
			int groups = 0; bool inVowel = false;
			foreach (char ch in w) {
				bool v = Vowels.Contains(ch);
				if (v && !inVowel) groups++;
				inVowel = v;
			}
			return groups;
		}

		private static string Capitalize(string w) =>
			string.IsNullOrEmpty(w) ? w : char.ToUpperInvariant(w[0]) + w.Substring(1);
	}
}
