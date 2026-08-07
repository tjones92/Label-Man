// Systems/Naming/Core/Inflection.cs
// Layer 6 — Irregular inflection engine. Lemma-first: one base word, forms computed on demand
// via table -> pattern-class -> regular rule -> orthographic normalizer, always terminating in
// valid output. Deterministic and memoized. Godot-free.
//
// Boundary (per design doc 6 §11): this layer is PURE MORPHOLOGY. Apostrophe-drop (runnin'),
// casing, and semantic choices live in the post-processor / grammar, never here.

using System;
using System.Collections.Generic;
using System.Text;

namespace LabelMan.Naming {

	public enum InflForm {
		Base, Past, PastPart, Ger, ThirdSing,
		Singular, Plural, Possessive, PluralPossessive,
		Comparative, Superlative
	}

	public enum Locale { Neutral, US, UK, Portuguese, Spanish, Jamaican }

	/// <summary>Morphology engine. Resolution order for every request: irregular table ->
	/// pattern-class -> regular rule -> normalizer -> cache. Never crashes, never returns empty.</summary>
	public sealed class Inflection {
		// lemma -> (past, pastPart). Only deviating forms listed; ger/3s fall through to rules.
		private readonly Dictionary<string, (string past, string pastPart)> _verbs = new(StringComparer.OrdinalIgnoreCase);
		// lemma -> plural. Only irregulars.
		private readonly Dictionary<string, string> _plurals = new(StringComparer.OrdinalIgnoreCase);
		// -o nouns: lemma -> "-es" | "-s". Domain default fills the rest.
		private readonly Dictionary<string, string> _oPlural = new(StringComparer.OrdinalIgnoreCase);
		// dual-form verbs: lemma -> forms with tag sets, selected by genre/mood/locale.
		private readonly Dictionary<string, List<DualForm>> _dual = new(StringComparer.OrdinalIgnoreCase);
		// variant pairs: lemma -> (usForm, ukForm) for burnt/burned etc.
		private readonly Dictionary<string, (string us, string uk)> _variant = new(StringComparer.OrdinalIgnoreCase);
		// irregular comparatives: lemma -> (comp, sup).
		private readonly Dictionary<string, (string comp, string sup)> _adjIrregular = new(StringComparer.OrdinalIgnoreCase);
		// gerund exceptions that break the rules (singe->singeing).
		private readonly Dictionary<string, string> _gerExceptions = new(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, string> _cache = new(StringComparer.Ordinal);
		// Lemmas that fell through all tables to a rule and looked irregular-suspicious (audit).
		private readonly HashSet<string> _audit = new(StringComparer.OrdinalIgnoreCase);
		public IReadOnlyCollection<string> AuditLog => _audit;

		private sealed class DualForm { public string form; public HashSet<string> tags; }

		private static readonly HashSet<char> Vowels = new() { 'a', 'e', 'i', 'o', 'u' };

		public Inflection() { SeedDefaults(); }

		// ---------------------------------------------------------------- public API
		public string Inflect(string lemma, InflForm form, Locale locale = Locale.Neutral, string styleFlags = null) {
			if (string.IsNullOrEmpty(lemma)) return lemma;
			string key = lemma + "|" + (int)form + "|" + (int)locale + "|" + (styleFlags ?? "");
			if (_cache.TryGetValue(key, out var hit)) return hit;
			string result = Resolve(lemma, form, locale, styleFlags);
			_cache[key] = result;
			return result;
		}

		/// <summary>Dual-form pasts (shine->shone/shined) resolved by genre + mood context tags.</summary>
		public string InflectContextual(string lemma, InflForm form, Locale locale, IReadOnlyCollection<string> context) {
			if ((form == InflForm.Past || form == InflForm.PastPart) && _dual.TryGetValue(lemma, out var forms)) {
				string best = null; int bestScore = -1;
				foreach (var f in forms) {
					int score = 0;
					if (context != null) foreach (var t in context) if (f.tags.Contains(t)) score++;
					if (score > bestScore) { bestScore = score; best = f.form; }
				}
				if (best != null) return best;
			}
			return Inflect(lemma, form, locale, null);
		}

		public bool TryParseForm(string mod, out InflForm form) {
			form = InflForm.Base;
			switch (mod.ToLowerInvariant()) {
				case "past": form = InflForm.Past; return true;
				case "pastpart": case "pp": form = InflForm.PastPart; return true;
				case "ger": case "ing": form = InflForm.Ger; return true;
				case "3s": case "thirdsing": form = InflForm.ThirdSing; return true;
				case "pl": case "plural": form = InflForm.Plural; return true;
				case "poss": form = InflForm.Possessive; return true;
				case "pluralposs": form = InflForm.PluralPossessive; return true;
				case "comp": case "comparative": form = InflForm.Comparative; return true;
				case "sup": case "superlative": form = InflForm.Superlative; return true;
				default: return false;
			}
		}

		// ---------------------------------------------------------------- resolution
		private string Resolve(string lemma, InflForm form, Locale locale, string style) {
			switch (form) {
				case InflForm.Base: case InflForm.Singular: return lemma;
				case InflForm.Past: return VerbPast(lemma, locale, style, pastPart: false);
				case InflForm.PastPart: return VerbPast(lemma, locale, style, pastPart: true);
				case InflForm.Ger: return Gerund(lemma);
				case InflForm.ThirdSing: return ThirdSing(lemma);
				case InflForm.Plural: return Plural(lemma, locale);
				case InflForm.Possessive: return Possessive(lemma, style);
				case InflForm.PluralPossessive: return PluralPossessive(lemma, locale);
				case InflForm.Comparative: return Comparative(lemma);
				case InflForm.Superlative: return Superlative(lemma);
				default: return lemma;
			}
		}

		private string VerbPast(string lemma, Locale locale, string style, bool pastPart) {
			// Variant (burnt/burned) — locale/register selects the -t or -ed form.
			if (_variant.TryGetValue(lemma, out var v)) {
				bool preferT = locale == Locale.UK || (style != null && style.Contains("poetic"));
				return preferT ? v.uk : v.us;
			}
			if (_verbs.TryGetValue(lemma, out var f))
				return pastPart ? f.pastPart : f.past;
			// Regular: +ed with normalizer (love->loved, cry->cried, stop->stopped).
			return RegularEd(lemma);
		}

		private string RegularEd(string lemma) {
			string l = lemma.ToLowerInvariant();
			if (l.EndsWith("e")) return lemma + "d";                       // love -> loved
			if (EndsConsonantY(l)) return lemma.Substring(0, lemma.Length - 1) + "ied"; // cry -> cried
			if (NeedsDoubling(l)) return lemma + lemma[lemma.Length - 1] + "ed";        // stop -> stopped
			return lemma + "ed";
		}

		private string Gerund(string lemma) {
			if (_gerExceptions.TryGetValue(lemma, out var g)) return g;
			string l = lemma.ToLowerInvariant();
			if (l.EndsWith("ie")) return lemma.Substring(0, lemma.Length - 2) + "ying"; // die -> dying
			if (l.EndsWith("ee") || l.EndsWith("ye") || l.EndsWith("oe")) return lemma + "ing"; // see -> seeing
			if (l.EndsWith("e")) return lemma.Substring(0, lemma.Length - 1) + "ing";   // ride -> riding
			if (NeedsDoubling(l)) return lemma + lemma[lemma.Length - 1] + "ing";       // run -> running
			return lemma + "ing";                                                       // cry -> crying, play -> playing
		}

		private string ThirdSing(string lemma) {
			string l = lemma.ToLowerInvariant();
			if (l == "be") return "is";
			if (l == "have") return "has";
			if (l == "go" || l == "do") return lemma + "es";
			if (l.EndsWith("s") || l.EndsWith("ss") || l.EndsWith("sh") || l.EndsWith("ch") ||
				l.EndsWith("x") || l.EndsWith("z")) return lemma + "es";
			if (EndsConsonantY(l)) return lemma.Substring(0, lemma.Length - 1) + "ies";
			return lemma + "s";
		}

		private string Plural(string lemma, Locale locale) {
			if (_plurals.TryGetValue(lemma, out var p)) return MatchCase(lemma, p);
			string l = lemma.ToLowerInvariant();

			// -o plural: explicit class, else music/loanword default -s, else -es.
			if (l.EndsWith("o")) {
				if (_oPlural.TryGetValue(lemma, out var cls)) return lemma + (cls == "-s" ? "s" : "es");
				_audit.Add(lemma);          // -o noun with no class -> log for authoring
				return lemma + "es";
			}
			// -f / -fe -> -ves handled by irregular table; remaining regular rules:
			if (EndsConsonantY(l)) return lemma.Substring(0, lemma.Length - 1) + "ies";  // lady -> ladies
			if (l.EndsWith("s") || l.EndsWith("ss") || l.EndsWith("sh") || l.EndsWith("ch") ||
				l.EndsWith("x") || l.EndsWith("z")) return lemma + "es";
			return lemma + "s";
		}

		private string Possessive(string lemma, string style) {
			if (lemma.EndsWith("s") || lemma.EndsWith("S")) {
				bool formal = style != null && style.Contains("formal");
				return formal ? lemma + "'" : lemma + "'s";       // James' (formal) / James's
			}
			return lemma + "'s";
		}

		private string PluralPossessive(string lemma, Locale locale) {
			string pl = Plural(lemma, locale);
			return pl.EndsWith("s") ? pl + "'" : pl + "'s";       // girls' / children's
		}

		private string Comparative(string lemma) {
			if (_adjIrregular.TryGetValue(lemma, out var a)) return a.comp;
			string l = lemma.ToLowerInvariant();
			int syl = SyllableEstimate(l);
			if (syl >= 2 && !EndsConsonantY(l)) return "more " + lemma;
			if (EndsConsonantY(l)) return lemma.Substring(0, lemma.Length - 1) + "ier"; // lonely -> lonelier
			if (l.EndsWith("e")) return lemma + "r";               // late -> later
			if (NeedsDoubling(l)) return lemma + lemma[lemma.Length - 1] + "er";        // hot -> hotter
			return lemma + "er";
		}

		private string Superlative(string lemma) {
			if (_adjIrregular.TryGetValue(lemma, out var a)) return a.sup;
			string l = lemma.ToLowerInvariant();
			int syl = SyllableEstimate(l);
			if (syl >= 2 && !EndsConsonantY(l)) return "most " + lemma;
			if (EndsConsonantY(l)) return lemma.Substring(0, lemma.Length - 1) + "iest";
			if (l.EndsWith("e")) return lemma + "st";
			if (NeedsDoubling(l)) return lemma + lemma[lemma.Length - 1] + "est";
			return lemma + "est";
		}

		// ---------------------------------------------------------------- morphology helpers
		private static bool EndsConsonantY(string l) =>
			l.Length >= 2 && l[l.Length - 1] == 'y' && !Vowels.Contains(l[l.Length - 2]);

		// Disyllables that double only because their FINAL syllable is stressed (be-GIN, for-GET).
		// Unstressed-final disyllables (WAN-der, TRAV-el) must NOT double — hence the explicit set.
		private static readonly HashSet<string> StressFinalDouble = new(StringComparer.OrdinalIgnoreCase) {
			"begin","forget","prefer","refer","occur","admit","permit","control","patrol","rebel",
			"submit","commit","regret","upset","propel","expel","compel","deter","forbid","allot","omit"
		};

		// CVC with a stressed final syllable -> double. Monosyllables always; disyllables only when
		// their final syllable carries stress (table above); longer words never (US convention).
		private static bool NeedsDoubling(string l) {
			if (l.Length < 2) return false;
			char c1 = l[l.Length - 1];
			if ("wxy".IndexOf(c1) >= 0) return false;                 // never double w,x,y
			if (Vowels.Contains(c1)) return false;                    // must end in consonant
			char v = l[l.Length - 2];
			if (!Vowels.Contains(v)) return false;                    // penult must be a vowel
			if (l.Length >= 3 && Vowels.Contains(l[l.Length - 3])) return false; // VV before C: no double (rain->raining)
			int syl = SyllableEstimate(l);
			if (syl == 1) return true;                                // run, sit, stop
			if (syl == 2) return StressFinalDouble.Contains(l);       // begin -> yes; wander/travel -> no
			return false;
		}

		private static int SyllableEstimate(string l) {
			int groups = 0; bool inV = false;
			foreach (char ch in l) {
				bool v = Vowels.Contains(ch) || ch == 'y';
				if (v && !inV) groups++;
				inV = v;
			}
			return Math.Max(1, groups);
		}

		private static string MatchCase(string source, string produced) =>
			source.Length > 0 && char.IsUpper(source[0]) && produced.Length > 0
				? char.ToUpperInvariant(produced[0]) + produced.Substring(1) : produced;

		// ---------------------------------------------------------------- data
		public void LoadJson(string json) {
			if (string.IsNullOrWhiteSpace(json)) return;
			using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
			var root = doc.RootElement;
			if (root.TryGetProperty("verbs", out var vs))
				foreach (var v in vs.EnumerateObject()) {
					var arr = v.Value;
					string past = arr[0].GetString();
					string pp = arr.GetArrayLength() > 1 ? arr[1].GetString() : past;
					_verbs[v.Name] = (past, pp);
				}
			if (root.TryGetProperty("plurals", out var pl))
				foreach (var p in pl.EnumerateObject()) _plurals[p.Name] = p.Value.GetString();
			if (root.TryGetProperty("oPlural", out var op))
				foreach (var o in op.EnumerateObject()) _oPlural[o.Name] = o.Value.GetString();
			if (root.TryGetProperty("variants", out var va))
				foreach (var v in va.EnumerateObject())
					_variant[v.Name] = (v.Value[0].GetString(), v.Value[1].GetString());
			if (root.TryGetProperty("adjectives", out var aj))
				foreach (var a in aj.EnumerateObject())
					_adjIrregular[a.Name] = (a.Value[0].GetString(), a.Value[1].GetString());
			if (root.TryGetProperty("gerundExceptions", out var ge))
				foreach (var g in ge.EnumerateObject()) _gerExceptions[g.Name] = g.Value.GetString();
			if (root.TryGetProperty("dualForms", out var df))
				foreach (var d in df.EnumerateObject()) {
					var list = new List<DualForm>();
					foreach (var entry in d.Value.EnumerateArray()) {
						var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						if (entry.TryGetProperty("tags", out var t))
							foreach (var tag in t.EnumerateArray()) tags.Add(tag.GetString());
						list.Add(new DualForm { form = entry.GetProperty("form").GetString(), tags = tags });
					}
					_dual[d.Name] = list;
				}
			_cache.Clear();
		}

		// Embedded high-frequency 60s-title tables (doc 6 §3, §5). JSON can extend/override.
		private void SeedDefaults() {
			void V(string lemma, string past, string pp) => _verbs[lemma] = (past, pp);
			V("run","ran","run"); V("shake","shook","shaken"); V("ride","rode","ridden");
			V("fly","flew","flown"); V("break","broke","broken"); V("go","went","gone");
			V("come","came","come"); V("see","saw","seen"); V("sing","sang","sung");
			V("ring","rang","rung"); V("bring","brought","brought"); V("sink","sank","sunk");
			V("drink","drank","drunk"); V("swim","swam","swum"); V("begin","began","begun");
			V("give","gave","given"); V("take","took","taken"); V("make","made","made");
			V("feel","felt","felt"); V("keep","kept","kept"); V("sleep","slept","slept");
			V("weep","wept","wept"); V("creep","crept","crept"); V("leave","left","left");
			V("lose","lost","lost"); V("find","found","found"); V("hold","held","held");
			V("tell","told","told"); V("sell","sold","sold"); V("fall","fell","fallen");
			V("know","knew","known"); V("grow","grew","grown"); V("throw","threw","thrown");
			V("blow","blew","blown"); V("draw","drew","drawn"); V("fight","fought","fought");
			V("catch","caught","caught"); V("teach","taught","taught"); V("stand","stood","stood");
			V("understand","understood","understood"); V("mean","meant","meant"); V("hear","heard","heard");
			V("say","said","said"); V("pay","paid","paid"); V("lay","laid","laid");
			V("buy","bought","bought"); V("think","thought","thought"); V("wear","wore","worn");
			V("tear","tore","torn"); V("swear","swore","sworn"); V("speak","spoke","spoken");
			V("steal","stole","stolen"); V("freeze","froze","frozen"); V("choose","chose","chosen");
			V("rise","rose","risen"); V("drive","drove","driven"); V("write","wrote","written");
			V("hide","hid","hidden"); V("bite","bit","bitten"); V("light","lit","lit");
			V("meet","met","met"); V("lead","led","led"); V("bleed","bled","bled");
			V("feed","fed","fed"); V("read","read","read"); V("build","built","built");
			V("send","sent","sent"); V("spend","spent","spent"); V("lend","lent","lent");
			V("set","set","set"); V("let","let","let"); V("put","put","put"); V("cut","cut","cut");
			V("hit","hit","hit"); V("hurt","hurt","hurt"); V("cost","cost","cost");

			// dual-form pasts, selected by genre/mood/locale tags
			_dual["shine"] = new List<DualForm> {
				new() { form = "shone",  tags = new(StringComparer.OrdinalIgnoreCase){"dreamy","elegant","light"} },
				new() { form = "shined", tags = new(StringComparer.OrdinalIgnoreCase){"gritty","plain","polish"} },
			};
			_dual["hang"] = new List<DualForm> {
				new() { form = "hung",   tags = new(StringComparer.OrdinalIgnoreCase){"suspended","plain"} },
				new() { form = "hanged", tags = new(StringComparer.OrdinalIgnoreCase){"gallows","ominous"} },
			};
			_dual["dive"] = new List<DualForm> {
				new() { form = "dove",  tags = new(StringComparer.OrdinalIgnoreCase){"us"} },
				new() { form = "dived", tags = new(StringComparer.OrdinalIgnoreCase){"uk","formal"} },
			};

			// variant pairs (us / uk)
			void Var(string lemma, string us, string uk) => _variant[lemma] = (us, uk);
			Var("burn","burned","burnt"); Var("learn","learned","learnt"); Var("dream","dreamed","dreamt");
			Var("spell","spelled","spelt"); Var("spill","spilled","spilt"); Var("smell","smelled","smelt");
			Var("kneel","kneeled","knelt"); Var("leap","leaped","leapt");

			// irregular plurals
			void P(string s, string p) => _plurals[s] = p;
			P("knife","knives"); P("leaf","leaves"); P("wolf","wolves"); P("life","lives");
			P("thief","thieves"); P("half","halves"); P("wife","wives"); P("shelf","shelves");
			P("wharf","wharves"); P("man","men"); P("woman","women"); P("child","children");
			P("foot","feet"); P("tooth","teeth"); P("goose","geese"); P("mouse","mice");
			P("louse","lice"); P("person","people"); P("sheep","sheep"); P("deer","deer");
			P("fish","fish"); P("series","series"); P("species","species"); P("aircraft","aircraft");
			P("cactus","cacti"); P("radius","radii"); P("nucleus","nuclei"); P("crisis","crises");
			P("oasis","oases"); P("thesis","theses"); P("analysis","analyses"); P("phenomenon","phenomena");
			P("criterion","criteria"); P("echo","echoes"); P("hero","heroes"); P("potato","potatoes");
			P("tomato","tomatoes"); P("torpedo","torpedoes"); P("volcano","volcanoes");

			// -o plural classes (music/loanwords keep -s)
			foreach (var w in new[] { "piano","solo","photo","radio","studio","tango","disco","soprano","banjo" })
				_oPlural[w] = "-s";
			foreach (var w in new[] { "echo","hero","potato","tomato","torpedo","volcano" })
				_oPlural[w] = "-es";

			// gerund exceptions
			_gerExceptions["singe"] = "singeing"; _gerExceptions["dye"] = "dyeing"; _gerExceptions["be"] = "being";

			// irregular adjectives
			_adjIrregular["good"] = ("better","best"); _adjIrregular["bad"] = ("worse","worst");
			_adjIrregular["far"] = ("further","furthest"); _adjIrregular["little"] = ("less","least");
			_adjIrregular["much"] = ("more","most"); _adjIrregular["many"] = ("more","most");
		}
	}
}
