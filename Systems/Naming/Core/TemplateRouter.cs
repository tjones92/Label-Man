// Systems/Naming/Core/TemplateRouter.cs
// Resolves (template-kind, genre) -> a template-set key by walking the genre's PROFILE ancestry
// (Layer-1 taxonomy), then a family/alias fallback, then the generic set, then grammar (null).
// Replaces the old hand-written if/else bucket switch in the adapter (docs D/E): one taxonomy,
// no second grouping to drift from the profile tree. Godot-free.
//
// Resolution, per ancestry rung (self -> base -> base-of-base):
//   1. KindOverride  — a kind-specific redirect OFF the profile tree (AcidRock SONGS lean psych,
//                       but its BAND names lean hardRock). Checked once, before the walk.
//   2. exact         — {kind}.{rung}, e.g. songTitle.DooWop  (case-insensitive set lookup).
//   3. SetSuffix     — {kind}.{alias(rung)} where alias maps a profile id (leaf OR base) to the
//                       template-set suffix its set is actually named with. Base entries are the
//                       family fallback (EarlyRock -> early60s); leaf entries alias a genre whose
//                       set name differs in spelling from its id (SurfRock -> surf).
//   4. generic       — a bare "{kind}" set (albums define "albumTitle"; songs/bands usually don't).
//   5. grammar       — null: the caller expands the raw grammar symbol instead.

using System;
using System.Collections.Generic;

namespace LabelMan.Naming {

	public sealed class TemplateRouter {

		private readonly GenreLibrary _genres;
		private readonly Func<string, bool> _hasSet;   // does a template/grammar set exist for this key?

		// Any profile rung (leaf or base) -> template-set suffix, consulted AFTER the exact
		// {kind}.{rung} lookup fails. Bases give the family fallback; leaf entries alias a genre
		// whose set is named differently from its id (or a legacy enum value with no own profile).
		private static readonly Dictionary<string, string> SetSuffix =
			new(StringComparer.OrdinalIgnoreCase) {
				// --- family bases (the shared ancestry rungs added in Phase F) ---
				["BluesRoot"]     = "blues",
				["EarlyRock"]     = "early60s",
				["AdultPop"]      = "jazz",        // last resort; TradPop/EasyListening/Classical own sets
				["BritishBeat"]   = "british",
				["PsychFamily"]   = "psych",
				["CountryRoot"]   = "country",
				["FolkRoot"]      = "folk",
				["LatinRoot"]     = "latin",
				["CaribbeanRoot"] = "reggae",
				["NoveltyRoot"]   = "comedy",
				["HardRock"]      = "hardRock",
				// --- leaf / legacy aliases (id spelling != set suffix, or no own profile) ---
				["SurfRock"]        = "surf",
				["GarageRock"]      = "garage",
				["SunshinePop"]     = "sunshine",
				["BaroquePop"]      = "sunshine",   // until a bespoke baroquePop set exists
				["PopRock"]         = "sunshine",
				["Funk"]            = "soul",        // funk SONGS override via exact songTitle.funk once authored
				["ProtoPunk"]       = "garage",      // until a bespoke protoPunk set exists
				["Psychedelic"]     = "psych",       // legacy enum value, no own profile
				["Skiffle"]         = "folk",        // legacy
				["SkaRocksteady"]   = "reggae",      // legacy
				["BritishInvasion"] = "british",     // legacy
			};

		// Kind-specific redirects that DON'T lie on the profile ancestry (checked before the walk).
		// Only the genuinely cross-tree cases: where the ancestry would resolve to a different family.
		private static readonly Dictionary<string, string> KindOverride =
			new(StringComparer.OrdinalIgnoreCase) {
				["bandName:AcidRock"]        = "hardRock",   // ancestry would give psych
				["bandName:ProgressiveRock"] = "hardRock",   // ancestry would give psych
				["bandName:RnB"]             = "soul",        // ancestry would give blues; RnB songs stay blues
				["bandName:RockAndRoll"]     = "garageEarly", // ancestry gives no band set
			};

		public TemplateRouter(GenreLibrary genres, Func<string, bool> hasSet) {
			_genres = genres;
			_hasSet = hasSet ?? (_ => false);
		}

		private static string[] Ancestry(GenreProfile p, string genreId) =>
			p.Ancestry.Length > 0 ? p.Ancestry : new[] { genreId ?? "_neutral" };

		/// <summary>The template-set key to use, or null to fall through to the grammar. kind is one
		/// of "songTitle", "bandName", "albumTitle", "soloAct".</summary>
		public string Resolve(string kind, string genreId) {
			if (string.IsNullOrEmpty(kind)) return null;
			var ancestry = Ancestry(_genres.Get(genreId), genreId);

			// 1. kind-specific override off the tree
			if (KindOverride.TryGetValue($"{kind}:{genreId}", out var ov)) {
				var k = $"{kind}.{ov}";
				if (_hasSet(k)) return k;
			}
			// 2 + 3. exact id then family/alias suffix, per ancestry rung (most specific first)
			foreach (var rung in ancestry) {
				var exact = $"{kind}.{rung}";
				if (_hasSet(exact)) return exact;
				if (SetSuffix.TryGetValue(rung, out var suf)) {
					var k = $"{kind}.{suf}";
					if (_hasSet(k)) return k;
				}
			}
			// 4. generic set (e.g. bare "albumTitle")
			if (_hasSet(kind)) return kind;
			// 5. grammar fallback
			return null;
		}

		/// <summary>Solo-act routing: the two-part key soloAct.&lt;genre&gt;.&lt;strategy&gt;, walked over the
		/// profile ancestry (then the family alias), then a same-strategy default, then the universal
		/// default.realName. Returns null only if even that is absent.</summary>
		public string ResolveSolo(string genreId, string strategy) {
			if (string.IsNullOrEmpty(strategy)) strategy = "realName";
			foreach (var rung in Ancestry(_genres.Get(genreId), genreId)) {
				var k = $"soloAct.{rung}.{strategy}";
				if (_hasSet(k)) return k;
				if (SetSuffix.TryGetValue(rung, out var suf)) { var ka = $"soloAct.{suf}.{strategy}"; if (_hasSet(ka)) return ka; }
			}
			var def = $"soloAct.default.{strategy}";
			if (_hasSet(def)) return def;
			return _hasSet("soloAct.default.realName") ? "soloAct.default.realName" : null;
		}

		/// <summary>Diagnostic: how a genre+kind resolves and via which rung — feeds the load-time
		/// audit that surfaces every genre silently degrading to generic/grammar (doc E §2.4).</summary>
		public RouteInfo Explain(string kind, string genreId) {
			var ancestry = Ancestry(_genres.Get(genreId), genreId);
			if (KindOverride.TryGetValue($"{kind}:{genreId}", out var ov) && _hasSet($"{kind}.{ov}"))
				return new RouteInfo($"{kind}.{ov}", RouteKind.KindOverride);
			foreach (var rung in ancestry) {
				if (_hasSet($"{kind}.{rung}"))
					return new RouteInfo($"{kind}.{rung}",
						rung.Equals(genreId, StringComparison.OrdinalIgnoreCase) ? RouteKind.Exact : RouteKind.Ancestry);
				if (SetSuffix.TryGetValue(rung, out var suf) && _hasSet($"{kind}.{suf}"))
					return new RouteInfo($"{kind}.{suf}", RouteKind.Family);
			}
			if (_hasSet(kind)) return new RouteInfo(kind, RouteKind.Generic);
			return new RouteInfo(null, RouteKind.Grammar);
		}
	}

	public enum RouteKind { Exact, Ancestry, KindOverride, Family, Generic, Grammar }

	public readonly struct RouteInfo {
		public readonly string Key;
		public readonly RouteKind Kind;
		public RouteInfo(string key, RouteKind kind) { Key = key; Kind = kind; }
		public override string ToString() => $"{Kind}: {Key ?? "<grammar>"}";
	}
}
