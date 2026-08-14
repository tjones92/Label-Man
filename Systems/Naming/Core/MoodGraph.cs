// Systems/Naming/Core/MoodGraph.cs
// Layer 4 — Mood compatibility graph. 19 moods in 4 home clusters, a symmetric weighted
// adjacency matrix stored as a flat float[361] (L0 cache form, doc 7 §3.2). mood.match() uses
// MAX-within-pair / MIN-across-pairs: permissive per word, strict per title. Godot-free.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LabelMan.Naming {

	public sealed class MoodGraph {
		// Canonical order — the flat matrix indexes off this.
		public static readonly string[] Moods = {
			"romantic","wistful","melancholy","serene","nostalgic","dreamy",   // TENDER
			"joyful","playful","cheeky","absurd",                              // BRIGHT
			"defiant","aggressive","gritty","restless","ominous",              // HARD
			"elegant","grand","spiritual","earnest"                            // ELEVATED
		};
		public const int N = 19;

		private readonly Dictionary<string, int> _index = new(StringComparer.OrdinalIgnoreCase);
		private readonly float[] _m = new float[N * N];

		public MoodGraph() {
			for (int i = 0; i < N; i++) _index[Moods[i]] = i;
			LoadEmbedded();
		}

		public bool IsMood(string m) => m != null && _index.ContainsKey(m);
		public int IndexOf(string m) => _index.TryGetValue(m, out var i) ? i : -1;

		public float Edge(int a, int b) => (a < 0 || b < 0) ? 0f : _m[a * N + b];
		public float Edge(string a, string b) => Edge(IndexOf(a), IndexOf(b));

		private void Set(int a, int b, float w) { _m[a * N + b] = w; _m[b * N + a] = w; }

		// ---- mood.match: internal coherence of a set of slots ---------------------
		// Each slot carries 0..n moods. Empty slot = wildcard.
		// pairScore = MAX over cross moods; combined = MIN over pairs; a 0.0 edge fails hard.
		// MatchInternalEx discriminates Forbidden (a 0-edge pairing) from BelowThreshold so the
		// engine's retry logic can reroll-slot vs. fall-back-template correctly (doc H #3).
		public MatchOutcome MatchInternalEx(IReadOnlyList<IReadOnlyCollection<string>> slots, double threshold) {
			double combined = 1.0;
			for (int i = 0; i < slots.Count; i++) {
				var a = slots[i];
				if (a == null || a.Count == 0) continue;           // wildcard
				for (int j = i + 1; j < slots.Count; j++) {
					var b = slots[j];
					if (b == null || b.Count == 0) continue;       // wildcard
					double best = 0.0;
					foreach (var ma in a) foreach (var mb in b) { double e = Edge(ma, mb); if (e > best) best = e; }
					if (best <= 0.0) return new MatchOutcome(MatchResult.Forbidden, -1); // forbidden edge present
					if (best < combined) combined = best;
				}
			}
			return combined >= threshold
				? new MatchOutcome(MatchResult.Pass, combined)
				: new MatchOutcome(MatchResult.BelowThreshold, combined);
		}

		/// <summary>Back-compat wrapper: the Pass score, or -1 for Forbidden/BelowThreshold.</summary>
		public double MatchInternal(IReadOnlyList<IReadOnlyCollection<string>> slots, double threshold) {
			var o = MatchInternalEx(slots, threshold);
			return o.Result == MatchResult.Pass ? o.Score : -1;
		}

		// ---- directed match: every slot must agree with a target mood set ---------
		public double MatchDirected(IReadOnlyList<IReadOnlyCollection<string>> slots,
									IReadOnlyCollection<string> target, double threshold) {
			double worst = 1.0;
			foreach (var s in slots) {
				if (s == null || s.Count == 0) continue;
				double best = 0.0;
				foreach (var m in s) foreach (var t in target) { double e = Edge(m, t); if (e > best) best = e; }
				if (best < threshold) return -1;
				if (best < worst) worst = best;
			}
			return worst;
		}

		/// <summary>Draw-time bias multiplier for a candidate word's moods against locked moods.
		/// 0 means incompatible (drop). strictness exponent sharpens the preference (doc 4 §9).</summary>
		public double BiasMultiplier(IReadOnlyCollection<string> candidateMoods,
									 IReadOnlyCollection<string> lockedMoods, double strictness) {
			if (candidateMoods == null || candidateMoods.Count == 0 ||
				lockedMoods == null || lockedMoods.Count == 0) return 1.0; // wildcard
			double best = 0.0;
			foreach (var c in candidateMoods) foreach (var l in lockedMoods) { double e = Edge(c, l); if (e > best) best = e; }
			if (best <= 0.0) return 0.0;
			return Math.Pow(best, Math.Max(0.0, strictness));
		}

		// ---- load-time validation (doc 4 §11) -------------------------------------
		public List<string> Validate() {
			var issues = new List<string>();
			for (int i = 0; i < N; i++) {
				if (Math.Abs(_m[i * N + i] - 1.0f) > 0.001f) issues.Add($"self-edge {Moods[i]} != 1.0");
				for (int j = i + 1; j < N; j++)
					if (Math.Abs(_m[i * N + j] - _m[j * N + i]) > 0.001f)
						issues.Add($"asymmetry {Moods[i]}~{Moods[j]}");
				int strong = 0;
				for (int j = 0; j < N; j++) if (j != i && _m[i * N + j] > 0.4f) strong++;
				if (strong < 3) issues.Add($"{Moods[i]} has <3 edges >0.4 (isolated mood risk)");
			}
			return issues;
		}

		/// <summary>Is a genre's affinity mood set connected above its threshold? (doc 4 §11 #4).
		/// Prevents a genre that can never fill a multi-slot template.</summary>
		public bool IsConnectedAbove(IReadOnlyCollection<string> moods, double threshold) {
			var idx = moods.Select(IndexOf).Where(i => i >= 0).Distinct().ToList();
			if (idx.Count <= 1) return true;
			var seen = new HashSet<int> { idx[0] };
			var stack = new Stack<int>(); stack.Push(idx[0]);
			while (stack.Count > 0) {
				int u = stack.Pop();
				foreach (int v in idx)
					if (!seen.Contains(v) && Edge(u, v) >= threshold) { seen.Add(v); stack.Push(v); }
			}
			return seen.Count == idx.Count;
		}

		/// <summary>A mood adjacent to both a and b above a floor — used by blend connectivity repair.
		/// Excludes the endpoints themselves so a~a=1.0 can't masquerade as a "bridge" (doc H #5).</summary>
		public string FindBridge(string a, string b, double floor = 0.4) {
			string best = null; double bestSum = -1;
			for (int c = 0; c < N; c++) {
				if (Moods[c].Equals(a, StringComparison.OrdinalIgnoreCase) ||
					Moods[c].Equals(b, StringComparison.OrdinalIgnoreCase)) continue;   // don't bridge to an endpoint
				double ea = Edge(Moods[c], a), eb = Edge(Moods[c], b);
				if (ea > floor && eb > floor && ea + eb > bestSum) { bestSum = ea + eb; best = Moods[c]; }
			}
			return best;
		}

		/// <summary>Is a genre's own affinity-mood set internally coherent at its threshold? A genre
		/// whose moods are disconnected (or one is isolated) can never fill a multi-slot title and
		/// degrades to bland grammar fallback. Run per resolved profile at load (doc H #4). Returns
		/// human-readable issues; empty = healthy.</summary>
		public List<string> ValidateGenre(string genreId, IEnumerable<string> affinityMoods, double threshold) {
			var issues = new List<string>();
			var moods = (affinityMoods ?? Enumerable.Empty<string>()).Where(IsMood)
						.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (moods.Count <= 1) return issues;
			if (!IsConnectedAbove(moods, threshold))
				issues.Add($"{genreId}: affinity moods {{{string.Join(",", moods)}}} NOT connected at threshold {threshold:0.##} — multi-slot titles may deadlock");
			foreach (var m in moods) {
				bool hasNeighbor = moods.Any(o => !o.Equals(m, StringComparison.OrdinalIgnoreCase) && Edge(m, o) >= threshold);
				if (!hasNeighbor)
					issues.Add($"{genreId}: mood '{m}' is isolated within its affinity set at threshold {threshold:0.##}");
			}
			return issues;
		}

		public void LoadJson(string json) {
			if (string.IsNullOrWhiteSpace(json)) return;
			using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
			if (!doc.RootElement.TryGetProperty("edges", out var edges)) return;
			foreach (var e in edges.EnumerateArray()) {
				int a = IndexOf(e[0].GetString()), b = IndexOf(e[1].GetString());
				if (a >= 0 && b >= 0) Set(a, b, (float)e[2].GetDouble());
			}
		}

		// Full symmetric matrix (doc 4 §3). Rows in canonical Moods order; strict lower/upper
		// mirror enforced by Set(). One line per row keeps it auditable against the doc.
		private void LoadEmbedded() {
			string[] rows = {
				// rom  wis  mel  ser  nos  dre  joy  pla  che  abs  def  agg  gri  res  omi  ele  gra  spi  ear
				"1.0 .8 .6 .7 .7 .7 .5 .4 .3 .1 .2 .0 .2 .3 .2 .7 .5 .4 .5", // romantic
				".8 1.0 .8 .7 .9 .7 .3 .2 .2 .2 .2 .0 .4 .5 .4 .6 .5 .4 .6", // wistful
				".6 .8 1.0 .6 .8 .6 .1 .1 .1 .1 .3 .1 .5 .4 .6 .5 .5 .5 .6", // melancholy
				".7 .7 .6 1.0 .7 .8 .4 .3 .2 .1 .0 .0 .1 .1 .1 .6 .5 .6 .5", // serene
				".7 .9 .8 .7 1.0 .6 .4 .3 .3 .2 .1 .0 .4 .3 .3 .5 .5 .4 .6", // nostalgic
				".7 .7 .6 .8 .6 1.0 .4 .4 .3 .3 .2 .1 .2 .6 .4 .6 .6 .6 .4", // dreamy
				".5 .3 .1 .4 .4 .4 1.0 .9 .8 .5 .3 .2 .3 .5 .0 .5 .5 .6 .5", // joyful
				".4 .2 .1 .3 .3 .4 .9 1.0 .9 .8 .3 .3 .4 .5 .1 .3 .3 .3 .3", // playful
				".3 .2 .1 .2 .3 .3 .8 .9 1.0 .8 .5 .4 .5 .6 .2 .2 .2 .1 .2", // cheeky
				".1 .2 .1 .1 .2 .3 .5 .8 .8 1.0 .3 .3 .4 .5 .3 .1 .2 .1 .1", // absurd  (absurd~restless bumped .4->.5: gives absurd a foothold in HARD so Comedy/novelty doesn't deadlock — doc H #1)
				".2 .2 .3 .0 .1 .2 .3 .3 .5 .3 1.0 .8 .8 .8 .6 .3 .6 .5 .6", // defiant
				".0 .0 .1 .0 .0 .1 .2 .3 .4 .3 .8 1.0 .8 .7 .7 .1 .5 .2 .3", // aggressive
				".2 .4 .5 .1 .4 .2 .3 .4 .5 .4 .8 .8 1.0 .7 .6 .2 .4 .5 .8", // gritty
				".3 .5 .4 .1 .3 .6 .5 .5 .6 .5 .8 .7 .7 1.0 .6 .3 .5 .4 .6", // restless (idx9 absurd .4->.5, mirrors the row above)
				".2 .4 .6 .1 .3 .4 .0 .1 .2 .3 .6 .7 .6 .6 1.0 .4 .8 .6 .5", // ominous
				".7 .6 .5 .6 .5 .6 .5 .3 .2 .1 .3 .1 .2 .3 .4 1.0 .8 .7 .6", // elegant
				".5 .5 .5 .5 .5 .6 .5 .3 .2 .2 .6 .5 .4 .5 .8 .8 1.0 .8 .7", // grand
				".4 .4 .5 .6 .4 .6 .6 .3 .1 .1 .5 .2 .5 .4 .6 .7 .8 1.0 .8", // spiritual
				".5 .6 .6 .5 .6 .4 .5 .3 .2 .1 .6 .3 .8 .6 .5 .6 .7 .8 1.0", // earnest
			};
			for (int i = 0; i < N; i++) {
				var cells = rows[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				for (int j = 0; j < N; j++) _m[i * N + j] = float.Parse(cells[j], System.Globalization.CultureInfo.InvariantCulture);
			}
		}
	}

	/// <summary>Outcome discriminator for mood.match: a forbidden (0-edge) pairing is distinct from a
	/// merely below-threshold combined score. Lets a caller reroll a slot vs. abandon the template.</summary>
	public enum MatchResult { Forbidden, BelowThreshold, Pass }

	public readonly struct MatchOutcome {
		public readonly MatchResult Result;
		public readonly double Score;
		public MatchOutcome(MatchResult r, double s) { Result = r; Score = s; }
	}
}
