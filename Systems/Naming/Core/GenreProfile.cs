// Systems/Naming/Core/GenreProfile.cs
// Layer 1 — the Genre Parameter Block. Prose genre sheets become data: a numeric voice vector
// (drives the post-processor pipeline), tag affinities (ambient semantic lean), a mood threshold,
// orthography/locale, and a 1960-69 era curve. Profiles form an inheritance tree (doc 1 §8) so a
// new genre declares only its deltas. Resolved profiles are frozen (immutable) for the HOT path.
// Godot-free.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LabelMan.Naming {

	/// <summary>The 11 voice scalars (0..1). Dense fixed-index array so the HOT path reads a slot
	/// by index, and blends can lerp the whole vector in one loop (doc 7 §3.3).</summary>
	public struct VoiceVector {
		public double NicknameDensity, PunctuationIntensity, TitleLengthBias, ArchaismLevel,
					  TheProbability, MononymProbability, NumeralPreference, ApostropheDropRate,
					  ParentheticalRate, FirstPersonBias, MoodStrictness;

		public static readonly string[] Dims = {
			"nicknameDensity","punctuationIntensity","titleLengthBias","archaismLevel","theProbability",
			"mononymProbability","numeralPreference","apostropheDropRate","parentheticalRate",
			"firstPersonBias","moodStrictness"
		};

		public double this[int i] {
			get => i switch {
				0 => NicknameDensity, 1 => PunctuationIntensity, 2 => TitleLengthBias, 3 => ArchaismLevel,
				4 => TheProbability, 5 => MononymProbability, 6 => NumeralPreference, 7 => ApostropheDropRate,
				8 => ParentheticalRate, 9 => FirstPersonBias, 10 => MoodStrictness, _ => 0 };
			set { switch (i) {
				case 0: NicknameDensity = value; break; case 1: PunctuationIntensity = value; break;
				case 2: TitleLengthBias = value; break; case 3: ArchaismLevel = value; break;
				case 4: TheProbability = value; break; case 5: MononymProbability = value; break;
				case 6: NumeralPreference = value; break; case 7: ApostropheDropRate = value; break;
				case 8: ParentheticalRate = value; break; case 9: FirstPersonBias = value; break;
				case 10: MoodStrictness = value; break; } }
		}

		public static int DimIndex(string name) => Array.FindIndex(Dims, d => d.Equals(name, StringComparison.OrdinalIgnoreCase));

		public VoiceVector Clamped() {
			var v = this;
			for (int i = 0; i < Dims.Length; i++) v[i] = Math.Clamp(v[i], 0, 1);
			return v;
		}

		// All-NaN probe: a seed lambda writes only the dims it cares about, so we can detect exactly
		// which dims were set (for correct partial inheritance) rather than guessing from values.
		public static VoiceVector NaN() {
			var v = new VoiceVector();
			for (int i = 0; i < Dims.Length; i++) v[i] = double.NaN;
			return v;
		}

		// A neutral middle profile — the safe default for any genre with no authored sheet.
		public static VoiceVector Neutral() => new VoiceVector {
			NicknameDensity = 0.2, PunctuationIntensity = 0.3, TitleLengthBias = 0.4, ArchaismLevel = 0.2,
			TheProbability = 0.5, MononymProbability = 0.1, NumeralPreference = 0.2, ApostropheDropRate = 0.2,
			ParentheticalRate = 0.1, FirstPersonBias = 0.15, MoodStrictness = 1.0
		};
	}

	public sealed class GenreProfile {
		public string Id;
		public string Extends;                 // base profile id, resolved by GenreLibrary
		public VoiceVector Voice = VoiceVector.Neutral();
		// Which fields this raw (pre-resolution) profile explicitly set — drives partial inheritance.
		internal bool _voiceSet, _moodThresholdSet, _eraSet;
		internal HashSet<int> _voiceDims;
		public Dictionary<string, double> DomainAffinity = new(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, double> MoodAffinity = new(StringComparer.OrdinalIgnoreCase);
		public HashSet<string> Suppress = new(StringComparer.OrdinalIgnoreCase);
		public double MoodThreshold = 0.35;
		public Locale Orthography = Locale.Neutral;
		public bool Diacritics;
		public double[] EraCurve = Enumerable.Repeat(1.0, 10).ToArray(); // 1960..1969 relative prevalence

		public double EraWeight(int year) {
			int i = Math.Clamp(year - 1960, 0, 9);
			return EraCurve[i];
		}

		public GenreProfile Clone() => new GenreProfile {
			Id = Id, Extends = Extends, Voice = Voice,
			DomainAffinity = new(DomainAffinity, StringComparer.OrdinalIgnoreCase),
			MoodAffinity = new(MoodAffinity, StringComparer.OrdinalIgnoreCase),
			Suppress = new(Suppress, StringComparer.OrdinalIgnoreCase),
			MoodThreshold = MoodThreshold, Orthography = Orthography, Diacritics = Diacritics,
			EraCurve = (double[])EraCurve.Clone()
		};

		/// <summary>Effective affinity multiplier for a word under this genre: domain-weight product,
		/// mood-weight product, hard 0 if any of the word's domains/moods is suppressed. 1.0 = neutral.</summary>
		public double AffinityFor(WordEntry e) {
			double w = 1.0;
			if (e.Tags != null)
				foreach (var t in e.Tags) {
					if (Suppress.Contains(t)) return 0.0;
					if (DomainAffinity.TryGetValue(t, out var dw)) w *= dw;
				}
			if (e.Moods != null)
				foreach (var m in e.Moods) {
					if (Suppress.Contains(m)) return 0.0;
					if (MoodAffinity.TryGetValue(m, out var mw)) w *= mw;
				}
			return w;
		}
	}

	/// <summary>Loads genre profiles, resolves the inheritance tree, and hands out frozen profiles.
	/// Unknown genres degrade to a neutral default rather than throwing.</summary>
	public sealed class GenreLibrary {
		private readonly Dictionary<string, GenreProfile> _raw = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, GenreProfile> _resolved = new(StringComparer.OrdinalIgnoreCase);

		public IEnumerable<string> Ids => _resolved.Keys;

		public GenreLibrary() { SeedBases(); ResolveAll(); }

		public GenreProfile Get(string id) {
			if (id != null && _resolved.TryGetValue(id, out var p)) return p;
			return _neutral;
		}
		private static readonly GenreProfile _neutral = new GenreProfile { Id = "_neutral" };

		public void Add(GenreProfile p) { _raw[p.Id] = p; }

		/// <summary>Load/override genre profiles from JSON, then re-resolve the inheritance tree. A
		/// profile only overrides the fields it names, so a JSON sheet can tweak one voice dim.</summary>
		public void LoadJson(string json) {
			if (string.IsNullOrWhiteSpace(json)) return;
			using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
			if (!doc.RootElement.TryGetProperty("genres", out var arr)) return;
			foreach (var g in arr.EnumerateArray()) {
				var p = _raw.TryGetValue(g.GetProperty("id").GetString(), out var existing) ? existing : new GenreProfile { Id = g.GetProperty("id").GetString() };
				if (g.TryGetProperty("extends", out var ex)) p.Extends = ex.GetString();
				if (g.TryGetProperty("voice", out var voice)) {
					p._voiceDims ??= new HashSet<int>(); p._voiceSet = true;
					foreach (var vd in voice.EnumerateObject()) {
						int di = VoiceVector.DimIndex(vd.Name);
						if (di >= 0) { p.Voice[di] = vd.Value.GetDouble(); p._voiceDims.Add(di); }
					}
				}
				if (g.TryGetProperty("domainAffinity", out var da)) foreach (var kv in da.EnumerateObject()) p.DomainAffinity[kv.Name] = kv.Value.GetDouble();
				if (g.TryGetProperty("moodAffinity", out var ma)) foreach (var kv in ma.EnumerateObject()) p.MoodAffinity[kv.Name] = kv.Value.GetDouble();
				if (g.TryGetProperty("suppress", out var su)) foreach (var s in su.EnumerateArray()) p.Suppress.Add(s.GetString());
				if (g.TryGetProperty("moodThreshold", out var mt)) { p.MoodThreshold = mt.GetDouble(); p._moodThresholdSet = true; }
				if (g.TryGetProperty("orthography", out var or) && Enum.TryParse<Locale>(or.GetString(), true, out var loc)) p.Orthography = loc;
				if (g.TryGetProperty("eraCurve", out var ec)) { p.EraCurve = ec.EnumerateArray().Select(x => x.GetDouble()).ToArray(); p._eraSet = true; }
				_raw[p.Id] = p;
			}
			ResolveAll();
		}

		private void ResolveAll() {
			_resolved.Clear();
			foreach (var id in _raw.Keys.ToList()) Resolve(id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		}

		private GenreProfile Resolve(string id, HashSet<string> stack) {
			if (_resolved.TryGetValue(id, out var done)) return done;
			if (!_raw.TryGetValue(id, out var raw)) return _neutral;
			if (!stack.Add(id)) return raw; // cycle guard

			GenreProfile baseP = string.IsNullOrEmpty(raw.Extends) ? null : Resolve(raw.Extends, stack).Clone();
			GenreProfile r = baseP ?? new GenreProfile();
			r.Id = raw.Id; r.Extends = raw.Extends;
			// deltas override base
			r.Voice = raw._voiceSet ? MergeVoice(r.Voice, raw.Voice, raw._voiceDims) : r.Voice;
			foreach (var kv in raw.DomainAffinity) r.DomainAffinity[kv.Key] = kv.Value;
			foreach (var kv in raw.MoodAffinity) r.MoodAffinity[kv.Key] = kv.Value;
			foreach (var s in raw.Suppress) r.Suppress.Add(s);
			if (raw._moodThresholdSet) r.MoodThreshold = raw.MoodThreshold;
			if (raw.Orthography != Locale.Neutral) r.Orthography = raw.Orthography;
			if (raw.Diacritics) r.Diacritics = true;
			if (raw._eraSet) r.EraCurve = (double[])raw.EraCurve.Clone();
			r.Voice = r.Voice.Clamped();
			_resolved[id] = r;
			stack.Remove(id);
			return r;
		}

		private static VoiceVector MergeVoice(VoiceVector baseV, VoiceVector delta, HashSet<int> setDims) {
			if (setDims == null) return delta;                 // full replacement
			for (int i = 0; i < VoiceVector.Dims.Length; i++) if (setDims.Contains(i)) baseV[i] = delta[i];
			return baseV;
		}

		// Base profiles + a few anchored leaves (doc 1 §8, §9). JSON extends/overrides these.
		private void SeedBases() {
			GenreProfile G(string id, string ext = null) { var p = new GenreProfile { Id = id, Extends = ext }; _raw[id] = p; return p; }

			// --- BluesRoot family ---
			var blues = G("BluesRoot");
			blues.SetVoice(v => { v.NicknameDensity = 0.9; v.ApostropheDropRate = 0.9; v.MononymProbability = 0.7;
								   v.TheProbability = 0.4; v.PunctuationIntensity = 0.3; v.ArchaismLevel = 0.3; return v; });
			blues.DomainAffinity["grit"] = 3; blues.DomainAffinity["travel"] = 2.5; blues.DomainAffinity["fate"] = 2;
			blues.DomainAffinity["vice"] = 2; blues.DomainAffinity["romance"] = 1.5;
			blues.MoodAffinity["gritty"] = 3; blues.MoodAffinity["restless"] = 2; blues.MoodAffinity["melancholy"] = 2;
			blues.Suppress.UnionWith(new[] { "candy", "luxury" });
			blues.MoodThreshold = 0.35;
			G("Blues", "BluesRoot");
			G("RnB", "BluesRoot").SetAff(d => { d["party"] = 2; d["dance"] = 1.5; });
			G("BluesRock", "BluesRoot").SetVoice(v => { v.PunctuationIntensity = 0.6; return v; });
			G("BritishBlues", "BluesRock").SetOrtho(Locale.UK);

			// --- AdultPop family ---
			var adult = G("AdultPop");
			adult.SetVoice(v => { v.NicknameDensity = 0.05; v.ArchaismLevel = 0.4; v.TitleLengthBias = 0.4;
								   v.PunctuationIntensity = 0.15; v.MoodStrictness = 1.4; return v; });
			adult.MoodAffinity["romantic"] = 2; adult.MoodAffinity["wistful"] = 2; adult.MoodAffinity["elegant"] = 2;
			adult.MoodThreshold = 0.55;
			G("TraditionalPop", "AdultPop");
			G("EasyListening", "AdultPop").SetVoice(v => { v.ParentheticalRate = 0.4; return v; });
			G("Jazz", "AdultPop").SetVoice(v => { v.ArchaismLevel = 0.2; v.TitleLengthBias = 0.3; v.MoodStrictness = 0.8; return v; })
								  .SetThreshold(0.35);
			G("Classical", "AdultPop").SetVoice(v => { v.TitleLengthBias = 0.9; v.ArchaismLevel = 0.7; v.PunctuationIntensity = 0.05; return v; });

			// --- British collective ---
			var brit = G("BritishBeat");
			brit.SetVoice(v => { v.TheProbability = 1.0; v.PunctuationIntensity = 0.8; v.NicknameDensity = 0.1;
								  v.TitleLengthBias = 0.3; return v; });
			brit.MoodAffinity["cheeky"] = 2; brit.MoodAffinity["restless"] = 2; brit.MoodThreshold = 0.35;
			G("BritishPop", "BritishBeat");
			G("BritishInvasion", "BritishBeat");

			// --- Psych family ---
			var psych = G("PsychFamily");
			psych.SetVoice(v => { v.ArchaismLevel = 0.5; v.TitleLengthBias = 0.6; v.MoodStrictness = 0.6; return v; });
			psych.DomainAffinity["mystical"] = 4; psych.DomainAffinity["flora"] = 3; psych.DomainAffinity["celestial"] = 2;
			psych.DomainAffinity["cosmic"] = 2;
			psych.MoodAffinity["dreamy"] = 4; psych.MoodAffinity["restless"] = 2;
			psych.Suppress.UnionWith(new[] { "grit", "conflict" });
			psych.MoodThreshold = 0.25;
			G("PsychedelicRock", "PsychFamily"); G("AcidRock", "PsychFamily");
			G("PsychedelicPop", "PsychFamily").SetVoice(v => { v.ArchaismLevel = 0.2; v.TitleLengthBias = 0.35; return v; });
			G("ProgressiveRock", "PsychFamily").SetVoice(v => { v.ArchaismLevel = 0.8; v.TitleLengthBias = 0.95; v.MoodStrictness = 1.2; return v; })
				.SetAff(d => { d["cosmic"] = 3; d["mythic"] = 3; d["conflict"] = 1.5; }, m => { m["grand"] = 4; m["ominous"] = 2; })
				.SetThreshold(0.4);

			// --- other anchored leaves (doc 1 §9) ---
			G("Country").SetVoice(v => { v.NicknameDensity = 0.4; v.PunctuationIntensity = 0.5; v.TitleLengthBias = 0.45;
										  v.ApostropheDropRate = 0.6; return v; })
				.SetAff(d => { d["rural"] = 3; d["travel"] = 2; d["vice"] = 1.5; }, m => { m["earnest"] = 2; m["gritty"] = 2; })
				.SetThreshold(0.35);
			G("Soul").SetVoice(v => { v.NicknameDensity = 0.4; v.PunctuationIntensity = 0.5; v.TheProbability = 0.4;
									   v.ApostropheDropRate = 0.4; return v; })
				.SetAff(d => { d["romance"] = 3; d["emotion"] = 2; d["body"] = 2; }, m => { m["earnest"] = 3; m["romantic"] = 2; })
				.SetThreshold(0.45);
			G("Funk").SetVoice(v => { v.NicknameDensity = 0.7; v.PunctuationIntensity = 0.6; v.MononymProbability = 0.6;
									   v.ApostropheDropRate = 0.7; return v; })
				.SetAff(d => { d["party"] = 3; d["dance"] = 3; d["mechanical"] = 2; }, m => { m["defiant"] = 2; m["cheeky"] = 2; });
			G("GarageRock").SetVoice(v => { v.TheProbability = 0.95; v.PunctuationIntensity = 0.9; v.TitleLengthBias = 0.2; v.MoodStrictness = 0.4; return v; })
				.SetAff(null, m => { m["cheeky"] = 2; m["restless"] = 2; m["aggressive"] = 1.5; }).SetThreshold(0.2);
			G("Bubblegum").SetVoice(v => { v.PunctuationIntensity = 0.95; v.TheProbability = 0.7; v.MoodStrictness = 0.4; return v; })
				.SetAff(d => { d["candy"] = 4; d["nonsense"] = 2; }, null).SetThreshold(0.2);
			G("SurfRock").SetVoice(v => { v.TheProbability = 0.9; v.TitleLengthBias = 0.2; return v; })
				.SetAff(d => { d["nautical"] = 4; d["vehicle"] = 3; d["dance"] = 2; }, null)
				.SetSuppress("faith","mythic","luxury","protest");
			G("Gospel").SetAff(d => { d["faith"] = 4; d["mythic"] = 2; d["emotion"] = 2; }, m => { m["spiritual"] = 4; m["earnest"] = 3; })
				.SetSuppress("vice","grit","party","candy").SetThreshold(0.45);
			G("Reggae").SetVoice(v => { v.NicknameDensity = 0.6; v.MononymProbability = 0.6; v.TheProbability = 0.6; return v; })
				.SetAff(d => { d["faith"] = 3; d["mythic"] = 3; d["protest"] = 2; d["identity"] = 2; }, m => { m["spiritual"] = 3; m["defiant"] = 2; })
				.SetSuppress("luxury","candy").SetOrtho(Locale.Jamaican).SetThreshold(0.35);
			G("Folk").SetVoice(v => { v.TheProbability = 0.0; v.NicknameDensity = 0.1; v.ApostropheDropRate = 0.2; v.TitleLengthBias = 0.5; return v; })
				.SetAff(d => { d["protest"] = 2; d["rural"] = 2; d["travel"] = 1.5; }, m => { m["earnest"] = 3; m["wistful"] = 2; })
				.SetSuppress("luxury","candy").SetThreshold(0.45);
			G("DooWop").SetVoice(v => { v.TheProbability = 0.95; return v; })
				.SetAff(d => { d["celestial"] = 3; d["gem"] = 2.5; d["romance"] = 3; d["nonsense"] = 2; }, m => { m["romantic"] = 3; m["dreamy"] = 2; })
				.SetSuppress("grit","vice","conflict").SetThreshold(0.4);
			G("HardRock").SetVoice(v => { v.PunctuationIntensity = 0.85; v.TitleLengthBias = 0.25; v.ArchaismLevel = 0.3; return v; })
				.SetAff(d => { d["conflict"] = 2; d["grit"] = 2; }, m => { m["aggressive"] = 3; m["defiant"] = 2; m["ominous"] = 2; }).SetThreshold(0.3);
			G("ProtoMetal", "HardRock").SetAff(null, m => { m["ominous"] = 3; m["grand"] = 2; });
			G("SingerSongwriter", "Folk").SetVoice(v => { v.PunctuationIntensity = 0.3; v.FirstPersonBias = 0.4; return v; });
			G("BossaNova", "AdultPop").SetOrtho(Locale.Portuguese).SetAff(d => { d["nautical"] = 2; d["flora"] = 2; }, m => { m["serene"] = 2; m["wistful"] = 2; });
		}
	}

	// Fluent seeding helpers keep SeedBases readable. Mark which fields the raw profile actually
	// set, so inheritance overrides only those (a leaf that touches 2 voice dims inherits the other 9).
	public static class GenreProfileSeedExt {
		public static GenreProfile SetVoice(this GenreProfile p, Func<VoiceVector, VoiceVector> f) {
			var after = f(VoiceVector.NaN());          // lambda writes only the dims it cares about
			p._voiceDims ??= new HashSet<int>();
			for (int i = 0; i < VoiceVector.Dims.Length; i++)
				if (!double.IsNaN(after[i])) { p.Voice[i] = after[i]; p._voiceDims.Add(i); }
			p._voiceSet = true; return p;
		}
		public static GenreProfile SetAff(this GenreProfile p, Action<Dictionary<string,double>> dom, Action<Dictionary<string,double>> mood = null) {
			dom?.Invoke(p.DomainAffinity); mood?.Invoke(p.MoodAffinity); return p;
		}
		public static GenreProfile SetSuppress(this GenreProfile p, params string[] tags) { p.Suppress.UnionWith(tags); return p; }
		public static GenreProfile SetThreshold(this GenreProfile p, double t) { p.MoodThreshold = t; p._moodThresholdSet = true; return p; }
		public static GenreProfile SetOrtho(this GenreProfile p, Locale l) { p.Orthography = l; if (l != Locale.Neutral) p.Diacritics = l is Locale.Portuguese or Locale.Spanish; return p; }
	}
}
