// Systems/Naming/Core/TemplateEngine.cs
// Layer 2 — the Template Constraint DSL, plus the Layer 1 §5 post-processor pipeline.
// A template has typed, addressable slots (%pos#k:inflect%), per-slot ontology filters, a
// constraint algebra (distinct / same / mood.match / register bounds) with failure modes, and a
// load-time gate. Slot filling is mood-biased so constraints are a safety net, not the bottleneck
// (doc 4 §9). Godot-free.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabelMan.Naming {

	// ---- semantic filter: tag[a|b & c & !d] over the ontology ----------------------
	public sealed class DomainFilter {
		private readonly List<string[]> _andOfOr = new();   // each entry: OR-alternatives, all must be satisfied
		private readonly List<string> _exclude = new();
		public bool IsEmpty => _andOfOr.Count == 0 && _exclude.Count == 0;
		private string _sig;
		/// <summary>Stable cache signature for the filtered-pool cache key.</summary>
		public string Signature => _sig ??= string.Join("&", _andOfOr.Select(g => string.Join("|", g))) + "!" + string.Join(",", _exclude);
		/// <summary>Human-readable tag label for the tuner dictionary, e.g. "celestial|gem, !aggressive".</summary>
		public string Label => string.Join(", ", _andOfOr.Select(g => string.Join("|", g)).Concat(_exclude.Select(x => "!" + x)));
		/// <summary>Every tag this filter references (for the tuner), across all AND/OR groups + excludes.</summary>
		public IEnumerable<string> ReferencedTags => _andOfOr.SelectMany(g => g).Concat(_exclude);

		public static DomainFilter Parse(string expr) {
			var f = new DomainFilter();
			if (string.IsNullOrWhiteSpace(expr)) return f;
			foreach (var rawGroup in expr.Split(new[] { '&', ',' }, StringSplitOptions.RemoveEmptyEntries)) {
				string g = rawGroup.Trim();
				if (g.Length == 0) continue;
				if (g.StartsWith("!")) { f._exclude.Add(g.Substring(1).Trim()); continue; }
				f._andOfOr.Add(g.Split('|').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray());
			}
			return f;
		}

		public bool Matches(WordEntry e, TagOntology ont) {
			foreach (var t in _exclude) if (TagHit(e, t, ont)) return false;
			foreach (var group in _andOfOr) {
				bool any = false;
				foreach (var alt in group) if (TagHit(e, alt, ont)) { any = true; break; }
				if (!any) return false;
			}
			return true;
		}

		private static bool TagHit(WordEntry e, string tag, TagOntology ont) {
			var dm = ont?.DomainMatch(e, tag);
			if (dm.HasValue) return dm.Value;                          // domain node: use closure
			if (e.HasTag(tag)) return true;                            // exact freeform tag
			return e.Moods != null && e.Moods.Contains(tag);           // mood name
		}
	}

	public enum FailMode { Reroll, RerollAll, Fallback, Collapse }

	public sealed class SlotSpec {
		public string Pos;
		public DomainFilter Filter = DomainFilter.Parse(null);
		public InflForm? Inflect;
		public bool Optional;
		public List<string> Transforms;      // slot-local post-ops (e.g. "diacritics")
		public bool Markov;                  // coin an invented word at this slot
	}

	public abstract class Constraint {
		public FailMode OnFail = FailMode.Reroll;
		public int RerollBudget = 3;
		/// <summary>Return the slot refs that violate; empty = satisfied.</summary>
		public abstract IReadOnlyList<string> Violations(FillState s);
		/// <summary>Some constraints repair deterministically (e.g. reduplication copies a slot)
		/// rather than relying on random reroll. Return true if the state was fixed in place.</summary>
		public virtual bool TryRepair(FillState s) => false;
	}

	public sealed class DistinctConstraint : Constraint {
		public string[] Slots;
		public bool Stem;
		public override IReadOnlyList<string> Violations(FillState s) {
			var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var bad = new List<string>();
			foreach (var sr in Slots) {
				if (!s.Lemma.TryGetValue(sr, out var w) || w == null) continue;
				string key = Stem ? Stemize(w) : w.ToLowerInvariant();
				if (seen.ContainsKey(key)) bad.Add(sr); else seen[key] = sr;
			}
			return bad;
		}
		private static string Stemize(string w) {
			w = w.ToLowerInvariant();
			foreach (var suf in new[] { "ing", "ed", "es", "s" }) if (w.Length > suf.Length + 2 && w.EndsWith(suf)) return w.Substring(0, w.Length - suf.Length);
			return w;
		}
	}

	public sealed class SameConstraint : Constraint {          // reduplication (Bubblegum, DooWop)
		public string[] Slots;
		public override IReadOnlyList<string> Violations(FillState s) {
			string first = null;
			var bad = new List<string>();
			foreach (var sr in Slots) {
				if (!s.Lemma.TryGetValue(sr, out var w) || w == null) continue;
				if (first == null) first = w;
				else if (!string.Equals(first, w, StringComparison.OrdinalIgnoreCase)) bad.Add(sr);
			}
			return bad;
		}
		// reduplication: copy the anchor slot (lemma + surface + moods) onto the rest
		public override bool TryRepair(FillState s) {
			if (Slots.Length == 0 || !s.Lemma.TryGetValue(Slots[0], out var anchorLemma)) return false;
			s.Surface.TryGetValue(Slots[0], out var anchorSurface);
			s.Moods.TryGetValue(Slots[0], out var anchorMoods);
			s.Register.TryGetValue(Slots[0], out var anchorReg);
			for (int i = 1; i < Slots.Length; i++) {
				s.Lemma[Slots[i]] = anchorLemma; s.Surface[Slots[i]] = anchorSurface;
				s.Moods[Slots[i]] = anchorMoods; s.Register[Slots[i]] = anchorReg;
			}
			return true;
		}
	}

	public sealed class MoodConstraint : Constraint {
		public string[] Target;         // null => internal coherence; else directed
		public double? Threshold;       // null => genre threshold
		public MoodGraph Graph;
		public override IReadOnlyList<string> Violations(FillState s) {
			double thr = Threshold ?? s.MoodThreshold;
			var slots = s.OrderedRefs.Select(r => (IReadOnlyCollection<string>)(s.Moods.TryGetValue(r, out var m) ? m : null)).ToList();
			double score = Target == null ? Graph.MatchInternal(slots, thr) : Graph.MatchDirected(slots, Target, thr);
			if (score >= 0) return Array.Empty<string>();
			// blame the last-drawn non-wildcard slot (cheapest reroll)
			for (int i = s.OrderedRefs.Count - 1; i >= 0; i--) {
				var r = s.OrderedRefs[i];
				if (s.Moods.TryGetValue(r, out var m) && m != null && m.Count > 0) return new[] { r };
			}
			return s.OrderedRefs.Count > 0 ? new[] { s.OrderedRefs[^1] } : Array.Empty<string>();
		}
	}

	public sealed class RegisterConstraint : Constraint {
		public int Min = -1, Max = 6;
		public override IReadOnlyList<string> Violations(FillState s) {
			var bad = new List<string>();
			foreach (var r in s.OrderedRefs)
				if (s.Register.TryGetValue(r, out var reg) && reg >= 0 && (reg < Min || reg > Max)) bad.Add(r);
			return bad;
		}
	}

	// mutable state during one fill attempt
	public sealed class FillState {
		public readonly Dictionary<string, string> Lemma = new(StringComparer.OrdinalIgnoreCase);   // pre-inflection
		public readonly Dictionary<string, string> Surface = new(StringComparer.OrdinalIgnoreCase);  // post-inflection
		public readonly Dictionary<string, HashSet<string>> Moods = new(StringComparer.OrdinalIgnoreCase);
		public readonly Dictionary<string, int> Register = new(StringComparer.OrdinalIgnoreCase);
		public List<string> OrderedRefs = new();
		public double MoodThreshold;
	}

	public sealed class ConstraintTemplate {
		public string Id;
		public string Type;                 // band | solo | song | album
		public string Pattern;              // literal text + %pos#k:inflect% slots
		public double Weight = 1.0;
		public int MinWords = 1, MaxWords = 12;
		public string[] Requires;           // genre must have these pos pools non-empty after filter
		public string[] Forbids;
		public int GateMinYear = 0, GateMaxYear = 9999;
		public Locale? GateOrthography;
		public string GateVoiceDim; public double GateVoiceMin = double.NegativeInfinity;
		public Dictionary<string, SlotSpec> Slots = new(StringComparer.OrdinalIgnoreCase);
		public List<Constraint> Constraints = new();
		public List<string> Transforms = new();
		public FailMode OnFail = FailMode.Reroll;

		private List<(string lit, string slotRef)> _parsed; // literal-before-slot pairs

		/// <summary>Parse the pattern into literal/slot segments and infer default SlotSpecs.</summary>
		public void Compile() {
			_parsed = new();
			int i = 0; var lit = new StringBuilder();
			while (i < Pattern.Length) {
				if (Pattern[i] == '%') {
					int end = Pattern.IndexOf('%', i + 1);
					if (end < 0) { lit.Append(Pattern[i]); i++; continue; }
					string body = Pattern.Substring(i + 1, end - i - 1);
					string slotRef = NormalizeRef(body, out var pos, out var infl);
					if (!Slots.TryGetValue(slotRef, out var spec)) { spec = new SlotSpec { Pos = pos }; Slots[slotRef] = spec; }
					if (spec.Pos == null) spec.Pos = pos;
					if (!spec.Inflect.HasValue && infl.HasValue) spec.Inflect = infl;
					_parsed.Add((lit.ToString(), slotRef));
					lit.Clear();
					i = end + 1;
				} else { lit.Append(Pattern[i]); i++; }
			}
			if (lit.Length > 0) _parsed.Add((lit.ToString(), null));
		}

		// "%verb#1:ger%" -> ref "verb#1", pos "verb", inflect ger
		private static string NormalizeRef(string body, out string pos, out InflForm? infl) {
			infl = null;
			int colon = body.IndexOf(':');
			string head = colon < 0 ? body : body.Substring(0, colon);
			string inflStr = colon < 0 ? null : body.Substring(colon + 1);
			int hash = head.IndexOf('#');
			pos = hash < 0 ? head : head.Substring(0, hash);
			string slotRef = hash < 0 ? head + "#1" : head;      // bare %noun% == %noun#1%
			if (inflStr != null) { var tmp = new Inflection(); if (tmp.TryParseForm(inflStr, out var f)) infl = f; }
			return slotRef;
		}

		public IReadOnlyList<(string lit, string slotRef)> Segments => _parsed;
		public bool Compiled => _parsed != null;
	}

	/// <summary>Parses templates.json into constraint-template sets keyed by grammar-style symbol.</summary>
	public static class ConstraintTemplateLoader {
		public static Dictionary<string, List<ConstraintTemplate>> Parse(string json) {
			var result = new Dictionary<string, List<ConstraintTemplate>>(StringComparer.Ordinal);
			if (string.IsNullOrWhiteSpace(json)) return result;
			using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
			foreach (var sym in doc.RootElement.EnumerateObject()) {
				if (sym.Value.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
				var list = new List<ConstraintTemplate>();
				foreach (var t in sym.Value.EnumerateArray()) list.Add(ParseOne(t));
				result[sym.Name] = list;
			}
			return result;
		}

		private static ConstraintTemplate ParseOne(System.Text.Json.JsonElement t) {
			var ct = new ConstraintTemplate {
				Id = Str(t, "id"), Type = Str(t, "type"), Pattern = Str(t, "pattern"),
				Weight = Dbl(t, "weight", 1.0),
			};
			if (t.TryGetProperty("words", out var w) && w.GetArrayLength() == 2) { ct.MinWords = w[0].GetInt32(); ct.MaxWords = w[1].GetInt32(); }
			if (t.TryGetProperty("requires", out var rq)) ct.Requires = rq.EnumerateArray().Select(x => x.GetString()).ToArray();
			if (t.TryGetProperty("forbids", out var fb)) ct.Forbids = fb.EnumerateArray().Select(x => x.GetString()).ToArray();
			if (t.TryGetProperty("gate", out var gate)) {
				ct.GateMinYear = (int)Dbl(gate, "minYear", 0); ct.GateMaxYear = (int)Dbl(gate, "maxYear", 9999);
				if (gate.TryGetProperty("orthography", out var or) && Enum.TryParse<Locale>(or.GetString(), true, out var loc)) ct.GateOrthography = loc;
				if (gate.TryGetProperty("voiceDim", out var vd)) { ct.GateVoiceDim = vd.GetString(); ct.GateVoiceMin = Dbl(gate, "voiceMin", double.NegativeInfinity); }
			}
			if (t.TryGetProperty("slots", out var slots))
				foreach (var s in slots.EnumerateObject()) {
					var spec = new SlotSpec { Pos = Str(s.Value, "pos") };
					if (s.Value.TryGetProperty("filter", out var f)) spec.Filter = DomainFilter.Parse(f.GetString());
					if (s.Value.TryGetProperty("inflect", out var infl)) { var tmp = new Inflection(); if (tmp.TryParseForm(infl.GetString(), out var form)) spec.Inflect = form; }
					spec.Optional = Bool(s.Value, "optional"); spec.Markov = Bool(s.Value, "markov");
					if (s.Value.TryGetProperty("transforms", out var tr)) spec.Transforms = tr.EnumerateArray().Select(x => x.GetString()).ToList();
					ct.Slots[s.Name] = spec;
				}
			if (t.TryGetProperty("constraints", out var cs))
				foreach (var c in cs.EnumerateArray()) { var parsed = ParseConstraint(c); if (parsed != null) ct.Constraints.Add(parsed); }
			if (t.TryGetProperty("transform", out var xf)) ct.Transforms = xf.EnumerateArray().Select(x => x.GetString()).ToList();
			ct.Compile();
			return ct;
		}

		private static Constraint ParseConstraint(System.Text.Json.JsonElement c) {
			string type = Str(c, "type");
			string[] cslots = c.TryGetProperty("slots", out var sl) ? sl.EnumerateArray().Select(x => x.GetString()).ToArray() : Array.Empty<string>();
			Constraint made = type?.ToLowerInvariant() switch {
				"distinct" => new DistinctConstraint { Slots = cslots, Stem = Bool(c, "stem") },
				"same" => new SameConstraint { Slots = cslots },
				"moodinternal" => new MoodConstraint { Threshold = c.TryGetProperty("threshold", out var th) ? th.GetDouble() : (double?)null },
				"mooddirected" => new MoodConstraint { Target = c.GetProperty("target").EnumerateArray().Select(x => x.GetString()).ToArray(),
													   Threshold = c.TryGetProperty("threshold", out var th2) ? th2.GetDouble() : (double?)null },
				"registermin" => new RegisterConstraint { Min = (int)Dbl(c, "value", -1) },
				"registermax" => new RegisterConstraint { Max = (int)Dbl(c, "value", 6) },
				_ => null,
			};
			if (made != null) {
				if (c.TryGetProperty("onFail", out var of) && Enum.TryParse<FailMode>(of.GetString(), true, out var fm)) made.OnFail = fm;
				made.RerollBudget = (int)Dbl(c, "budget", 3);
			}
			return made;
		}

		private static string Str(System.Text.Json.JsonElement e, string k) => e.TryGetProperty(k, out var v) ? v.GetString() : null;
		private static double Dbl(System.Text.Json.JsonElement e, string k, double d) => e.TryGetProperty(k, out var v) && v.TryGetDouble(out var x) ? x : d;
		private static bool Bool(System.Text.Json.JsonElement e, string k) => e.TryGetProperty(k, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.True;
	}

	public sealed class TemplateEngine {
		private readonly Lexicon _lex;
		private readonly TagOntology _ont;
		private readonly MoodGraph _mood;
		private readonly Inflection _infl;
		private readonly IWordCoiner _coiner;
		private readonly PoolCache _pools = new();   // Layer 7 L1: memoized prefix-sum pools

		public TemplateEngine(Lexicon lex, TagOntology ont, MoodGraph mood, Inflection infl, IWordCoiner coiner = null) {
			_lex = lex; _ont = ont; _mood = mood; _infl = infl; _coiner = coiner;
		}

		/// <summary>Evict derived pools — call at a sim year-tick after the lexicon changes (doc 7 §12).</summary>
		public void ClearCaches() => _pools.Clear();

		/// <summary>Fill a template against a genre profile. Returns null if it cannot satisfy its
		/// constraints within budget (caller falls back to a simpler template).</summary>
		public string Fill(ConstraintTemplate t, NamingContext ctx, GenreProfile genre, int maxAttempts = 6) {
			if (!t.Compiled) t.Compile();
			for (int attempt = 0; attempt < maxAttempts; attempt++) {
				var st = new FillState { MoodThreshold = genre.MoodThreshold };
				bool ok = true;
				// left-to-right, biasing each draw by moods locked so far
				var locked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var (_, slotRef) in t.Segments) {
					if (slotRef == null) continue;
					if (!DrawSlot(t, slotRef, ctx, genre, locked, st)) { ok = false; break; }
					if (st.Moods.TryGetValue(slotRef, out var m) && m != null) foreach (var x in m) locked.Add(x);
					if (!st.OrderedRefs.Contains(slotRef)) st.OrderedRefs.Add(slotRef);
				}
				if (!ok) continue;
				if (!EnforceConstraints(t, ctx, genre, st)) continue;
				string assembled = Assemble(t, st);
				assembled = PostProcessor.Run(assembled, genre, ctx, st, t.Type);
				int wc = assembled.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
				if (wc < t.MinWords || wc > t.MaxWords) continue;
				return assembled;
			}
			return null;
		}

		private bool DrawSlot(ConstraintTemplate t, string slotRef, NamingContext ctx, GenreProfile genre,
							  HashSet<string> locked, FillState st) {
			var spec = t.Slots[slotRef];
			if (spec.Optional && ctx.Rng.Chance(0.5)) { st.Lemma[slotRef] = null; st.Surface[slotRef] = ""; return true; }

			if (spec.Markov && _coiner != null) {
				string coined = _coiner.Coin(spec.Pos, null, ctx);
				if (coined != null) { st.Lemma[slotRef] = coined; st.Surface[slotRef] = coined; st.Moods[slotRef] = null; st.Register[slotRef] = -1; return true; }
			}

			var pool = _lex.Pool(spec.Pos);
			if (pool.Count == 0) { if (spec.Optional) { st.Surface[slotRef] = ""; return true; } return false; }

			// FAST PATH (Layer 7): with no locked moods, mood bias is uniform (1.0), so the pool is a
			// pure function of (pos, filter, epoch, orthography, genre) — memoize its prefix sums.
			WordEntry pick = null;
			if (locked.Count == 0) {
				string key = spec.Pos + "|" + spec.Filter.Signature + "|" + NameModels.Epoch(ctx.Year) + "|" + (int)genre.Orthography + "|" + genre.Id;
				var fp = _pools.GetOrBuild(key, () => BuildPool(pool, spec, genre, ctx.Year));
				if (fp.Count > 0) pick = fp.Pick(ctx.Rng.NextDouble());
			}

			// weighted candidate list (biased path, or fallback when the cached pool was empty)
			double total = 0; var cands = new List<(WordEntry e, double w)>();
			if (pick == null)
			foreach (var e in pool) {
				if (!spec.Filter.IsEmpty && !spec.Filter.Matches(e, _ont)) continue;
				if (!TagOntology.LocaleEligible(e.LocaleClass, genre.Orthography)) continue; // keep non-English out of non-matching genres
				double aff = genre.AffinityFor(e);
				if (aff <= 0) continue;                               // suppressed
				double bias = _mood.BiasMultiplier(e.Moods, locked, genre.Voice.MoodStrictness);
				if (bias <= 0) continue;                              // forbidden mood pairing
				double era = (e.EraClass != null && !TagOntology.EraEligible(e.EraClass, ctx.Year)) ? 0.12 : 1.0;
				double w = (e.Weight <= 0 ? 1 : e.Weight) * aff * bias * era;
				if (w <= 0) continue;
				cands.Add((e, w)); total += w;
			}
			if (pick == null && cands.Count == 0) {
				// relax mood/filter rather than fail hard: fall back to any affinity-positive word
				foreach (var e in pool) { double aff = genre.AffinityFor(e); if (aff > 0) { cands.Add((e, aff)); total += aff; } }
				if (cands.Count == 0) { if (spec.Optional) { st.Surface[slotRef] = ""; return true; } return false; }
			}
			if (pick == null) {
				double roll = ctx.Rng.NextDouble() * total; pick = cands[^1].e;
				foreach (var (e, w) in cands) { roll -= w; if (roll <= 0) { pick = e; break; } }
			}

			st.Lemma[slotRef] = pick.Word;
			st.Moods[slotRef] = pick.Moods;
			st.Register[slotRef] = pick.Register;
			// inflection (contextual for dual-form pasts by genre+mood tags)
			string surface = pick.Word;
			if (spec.Inflect.HasValue) {
				var moodCtx = new List<string>(); if (pick.Moods != null) moodCtx.AddRange(pick.Moods);
				if (genre.Orthography == Locale.US) moodCtx.Add("us"); else if (genre.Orthography == Locale.UK) moodCtx.Add("uk");
				surface = _infl.InflectContextual(pick.Word, spec.Inflect.Value, genre.Orthography, moodCtx);
			}
			st.Surface[slotRef] = surface;
			return true;
		}

		// Score a pool with filter + genre affinity + era (no mood bias — valid only when nothing is
		// locked). Feeds the prefix-sum FilteredPool cache.
		private FilteredPool BuildPool(IReadOnlyList<WordEntry> pool, SlotSpec spec, GenreProfile genre, int year) {
			var scored = new List<(WordEntry, double)>();
			foreach (var e in pool) {
				if (!spec.Filter.IsEmpty && !spec.Filter.Matches(e, _ont)) continue;
				if (!TagOntology.LocaleEligible(e.LocaleClass, genre.Orthography)) continue;
				double aff = genre.AffinityFor(e);
				if (aff <= 0) continue;
				double era = (e.EraClass != null && !TagOntology.EraEligible(e.EraClass, year)) ? 0.12 : 1.0;
				scored.Add((e, (e.Weight <= 0 ? 1 : e.Weight) * aff * era));
			}
			return new FilteredPool(scored);
		}

		private bool EnforceConstraints(ConstraintTemplate t, NamingContext ctx, GenreProfile genre, FillState st) {
			foreach (var c in t.Constraints) {
				if (c is MoodConstraint mc && mc.Graph == null) mc.Graph = _mood;
				int budget = c.RerollBudget;
				while (true) {
					var bad = c.Violations(st);
					if (bad.Count == 0) break;
					if (c.TryRepair(st)) continue;                      // deterministic fix (e.g. reduplication)
					if (budget-- <= 0) return false;
					if (c.OnFail == FailMode.RerollAll) return false;   // caller re-attempts whole template
					var locked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (var r in st.OrderedRefs) if (!bad.Contains(r) && st.Moods.TryGetValue(r, out var m) && m != null) foreach (var x in m) locked.Add(x);
					foreach (var r in bad) if (!DrawSlot(t, r, ctx, genre, locked, st)) return false;
				}
			}
			return true;
		}

		private static string Assemble(ConstraintTemplate t, FillState st) {
			var sb = new StringBuilder();
			foreach (var (lit, slotRef) in t.Segments) {
				sb.Append(lit);
				if (slotRef != null && st.Surface.TryGetValue(slotRef, out var s)) sb.Append(s);
			}
			return sb.ToString();
		}

		// ---- load-time validation (doc 2 §10, doc 3 §11) --------------------------
		public bool SatisfiableFor(ConstraintTemplate t, GenreProfile genre, int minPoolDepth = 4) {
			if (!t.Compiled) t.Compile();
			if (t.Requires != null) foreach (var pos in t.Requires) if (_lex.Pool(pos).Count == 0) return false;
			if (t.Forbids != null) foreach (var pos in t.Forbids) if (_lex.Pool(pos).Count > 0) return false;
			// every slot must have a non-trivial pool after filter + genre suppression
			foreach (var kv in t.Slots) {
				var spec = kv.Value;
				if (spec.Markov || spec.Optional) continue;
				int depth = 0;
				foreach (var e in _lex.Pool(spec.Pos)) {
					if (!spec.Filter.IsEmpty && !spec.Filter.Matches(e, _ont)) continue;
					if (genre.AffinityFor(e) <= 0) continue;
					depth++; if (depth >= minPoolDepth) break;
				}
				if (depth == 0) return false;                          // empty pool -> template invalid here
			}
			// distinctness needs >= slot-count candidates
			foreach (var c in t.Constraints)
				if (c is DistinctConstraint dc) {
					var spec = t.Slots[dc.Slots[0]];
					int depth = _lex.Pool(spec.Pos).Count(e => spec.Filter.IsEmpty || spec.Filter.Matches(e, _ont));
					if (depth < dc.Slots.Length) return false;
				}
			return true;
		}

		public bool GatePasses(ConstraintTemplate t, NamingContext ctx, GenreProfile genre) {
			if (ctx.Year > 0 && (ctx.Year < t.GateMinYear || ctx.Year > t.GateMaxYear)) return false;
			if (t.GateOrthography.HasValue && genre.Orthography != t.GateOrthography.Value) return false;
			if (t.GateVoiceDim != null) {
				int di = VoiceVector.DimIndex(t.GateVoiceDim);
				if (di >= 0 && genre.Voice[di] < t.GateVoiceMin) return false;
			}
			return true;
		}
	}

	// ---- Layer 1 §5: the ordered post-processor pipeline --------------------------
	public static class PostProcessor {
		public static string Run(string s, GenreProfile genre, NamingContext ctx, FillState st, string outputType = null) {
			if (string.IsNullOrEmpty(s)) return s;
			var v = genre.Voice; var rng = ctx.Rng;
			s = ApostropheDrop(s, v.ApostropheDropRate, rng);
			s = Numerals(s, v.NumeralPreference, rng);
			s = TitleCase(s);
			// Act names (band/solo) don't take !/? punctuation — only titles do.
			bool isActName = outputType == "band" || outputType == "solo";
			if (!isActName) s = Punctuation(s, v.PunctuationIntensity, rng);
			return CollapseSpaces(s);
		}

		// contextual: only -ing gerunds and "and" -> "'n'" (doc 1 §5 rule 1)
		private static string ApostropheDrop(string s, double rate, IRandom rng) {
			if (rate <= 0) return s;
			var words = s.Split(' ');
			for (int i = 0; i < words.Length; i++) {
				string w = words[i];
				if (w.Length > 4 && w.EndsWith("ing", StringComparison.OrdinalIgnoreCase) && rng.Chance(rate))
					words[i] = w.Substring(0, w.Length - 1) + "'";                // running -> runnin'
				else if (w.Equals("and", StringComparison.OrdinalIgnoreCase) && rng.Chance(rate * 0.6))
					words[i] = "'n'";
			}
			return string.Join(" ", words);
		}

		private static readonly string[] Ones = { "zero","one","two","three","four","five","six","seven","eight","nine","ten","eleven","twelve" };
		private static string Numerals(string s, double pref, IRandom rng) {
			// prob of DIGITS = pref; when low, spell small numbers out
			var words = s.Split(' ');
			for (int i = 0; i < words.Length; i++) {
				string w = words[i];
				if (int.TryParse(w, out var n) && n >= 0 && n <= 12 && !rng.Chance(pref))
					words[i] = Cap(Ones[n]);
			}
			return string.Join(" ", words);
		}

		private static string TitleCase(string s) {
			var words = s.Split(' ');
			var small = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a","an","the","and","but","or","for","nor","of","in","on","at","to","by","with","from" };
			for (int i = 0; i < words.Length; i++) {
				if (words[i].Length == 0) continue;
				bool first = i == 0 || i == words.Length - 1;
				words[i] = (!first && small.Contains(words[i])) ? words[i].ToLowerInvariant() : Cap(words[i]);
			}
			return string.Join(" ", words);
		}

		private static string Punctuation(string s, double intensity, IRandom rng) {
			if (intensity > 0.6 && rng.Chance((intensity - 0.6) * 1.2)) return s + "!";
			if (intensity > 0.4 && rng.Chance((intensity - 0.4) * 0.3)) return s + "?";
			return s;
		}

		private static string Cap(string w) => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w.Substring(1);
		private static string CollapseSpaces(string s) {
			var sb = new StringBuilder(s.Length); bool sp = false;
			foreach (char c in s) { if (c == ' ') { if (!sp) sb.Append(' '); sp = true; } else { sb.Append(c); sp = false; } }
			return sb.ToString().Trim();
		}
	}
}
