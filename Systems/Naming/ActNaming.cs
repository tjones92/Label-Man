// Systems/Naming/ActNaming.cs
// Act and label naming POLICY, Godot-free: which name set an act routes to, the demographics its
// person slots read, how a lineup is handed to the Core, and what happens when a name is taken.
// NameGenerator (the Godot adapter) owns the streams and the file I/O and delegates here, and the
// SimTools/NamingCensus harness runs this same class against the real data -- so the census
// measures the code the game runs, not a re-implementation of it.
//
// Every draw here comes from ctx.Rng, which the adapter points at the act or label stream. Neither
// stream feeds anything but names, so nothing in this file can move the simulation.

using System;
using System.Collections.Generic;
using System.Linq;
using LabelMan.Naming;

public sealed class ActNaming {
	public NameEngine Engine { get; }
	public TemplateRouter Router { get; }

	public ActNaming(NameEngine engine, TemplateRouter router) { Engine = engine; Router = router; }

	// ------------------------------------------------------------------ data loading
	public static readonly string[] LexiconFiles = {
		"lexicon.json", "lexicon.ontology.json", "lexicon.ontology.base.json", "lexicon.people.json",
		"lexicon.labels.json", "lexicon.user.json" };

	/// <summary>Build the engine from Data/Naming files. <paramref name="read"/> maps a bare file name
	/// to its text (null if absent). Shared by the game adapter and the census harness.</summary>
	public static ActNaming Load(Func<string, string> read, out NameModels models) {
		models = null;
		string lexJson = read("lexicon.json"), grammarJson = read("grammar.json");
		if (string.IsNullOrEmpty(lexJson) || string.IsNullOrEmpty(grammarJson)) return null;
		var lexicon = Lexicon.LoadFromJson(lexJson);
		foreach (var f in LexiconFiles.Skip(1)) { string j = read(f); if (!string.IsNullOrEmpty(j)) lexicon.AppendJson(j); }
		var ontology = new TagOntology(); ontology.LoadJson(read("ontology.json"));
		var moods = new MoodGraph(); moods.LoadJson(read("moods.json"));
		var inflection = new Inflection(); inflection.LoadJson(read("inflection.json"));
		var genres = new GenreLibrary(); genres.LoadJson(read("genres.json"));
		var diminutives = new Diminutives(); diminutives.LoadJson(read("diminutives.json"));
		models = new NameModels(ontology, moods, inflection, genres, diminutives);
		var engine = new NameEngine(lexicon, GrammarEngine.ParseGrammar(grammarJson), models);
		string templatesJson = read("templates.json");
		if (!string.IsNullOrEmpty(templatesJson))
			foreach (var kv in ConstraintTemplateLoader.Parse(templatesJson)) engine.AddConstraintSet(kv.Key, kv.Value);
		var router = new TemplateRouter(models.Genres, key => engine.HasNameSet(key));
		var naming = new ActNaming(engine, router);
		naming.LoadReserved(read("reserved.json"));
		return naming;
	}

	// ------------------------------------------------------------------ reserved names
	private readonly HashSet<string> _reservedActs = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>reserved.json: famous real acts and labels of the period, never to be minted.</summary>
	public void LoadReserved(string json) {
		if (string.IsNullOrWhiteSpace(json)) return;
		using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
			CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
		if (doc.RootElement.TryGetProperty("acts", out var acts))
			foreach (var a in acts.EnumerateArray()) _reservedActs.Add(ActCore(a.GetString()));
		if (doc.RootElement.TryGetProperty("labels", out var labels))
			ReserveLabelNames(labels.EnumerateArray().Select(x => x.GetString()).ToList());
	}

	private static string ActCore(string n) {
		string s = (n ?? "").Trim().Replace(" & ", " and ");
		return s.StartsWith("The ", StringComparison.OrdinalIgnoreCase) ? s.Substring(4) : s;
	}

	/// <summary>Is this a famous real act's name, outright or as the backing band ("X and the Drifters")?</summary>
	public bool IsReservedAct(string name) {
		string core = ActCore(name);
		if (_reservedActs.Contains(core)) return true;
		foreach (var sep in new[] { " and the ", " 'n' the ", " with the " }) {
			int i = core.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
			if (i > 0 && _reservedActs.Contains(core.Substring(i + sep.Length))) return true;
		}
		return false;
	}

	// ------------------------------------------------------------------ context
	public NamingContext MakeContext(Genre genre, int year, ArtistType type, IRandom rng) => new NamingContext {
		Genre = genre.ToString(), Year = year, ArtistType = type.ToString(),
		StyleTags = new List<string> { StyleTagFor(genre) },
		TagSets = new Dictionary<string, List<string>>(), Slots = new Dictionary<string, string>(), Rng = rng
	};

	// ================================================================== ACTS
	/// <summary>
	/// An act's name. With a <paramref name="lineup"/> (front person first) every person in the name is
	/// a member and every claim the wording makes -- head count, sex, kinship, a backing band -- is true
	/// of the lineup. Exact names are unique across ALL genres; near-duplicates are policed per genre.
	/// </summary>
	public string GenerateActName(Genre genre, int year, ArtistType type, Lineup lineup, IRandom rng,
								  string regionId = null, string labelArchetype = null) {
		var ctx = MakeContext(genre, year, type, rng);
		ctx.RegionId = regionId;
		ctx.LabelArchetype = labelArchetype;
		if (lineup != null && lineup.Size > 0) ctx.Lineup = lineup;
		bool solo = type == ArtistType.SoloMale || type == ArtistType.SoloFemale || ctx.Lineup?.Size == 1;
		string symbol = ChooseArtistSymbol(genre, year, type, ctx);
		var l = ctx.Lineup;
		var opts = new NameEngine.UniqueOptions {
			Finalize = ActNamePolish.Run,
			ExactBucket = "artist",
			// Safety net over the whole finished name: a word drawn from a pool can still make a claim
			// ("Chester Hairston Quintet" for four men, from an ensemble-noun pool), and a group noun can
			// be a famous act's name. Either rejects the candidate and the loop draws again.
			Accept = n => !IsReservedAct(n) && (l == null || (CastClaim.FromLiteral(StripPeople(n, l), !solo).Admits(l) && !RepeatsAFirstName(n, l))),
			Variants = b => ActVariants(b, ctx.Lineup, solo),
		};
		// A real-name solo template can only ever produce the one name, so retrying it 40 times is
		// 40 identical fills; a few tries, then the variants.
		return Engine.Generate(symbol, ctx, "artist|" + genre, nearDup: !solo, attempts: solo ? 6 : 40, opts);
	}

	// Person names can collide with claim words (a member surnamed "Brothers" or first-named "Duo"),
	// so the finished-name check reads the text with the members' own names taken out.
	private static string StripPeople(string name, Lineup l) {
		var drop = new HashSet<string>(l.People.SelectMany(p => new[] { p.First, p.Last }), StringComparer.OrdinalIgnoreCase);
		return string.Join(" ", name.Split(' ').Where(w => !drop.Contains(w.TrimEnd('\'', 's')) && !drop.Contains(w)));
	}

	// "Judith, Brent and Brent": two members can share a first name, but a billing that names them both
	// by it reads as a typo.
	private static bool RepeatsAFirstName(string name, Lineup l) {
		var firsts = new HashSet<string>(l.People.Select(p => p.First), StringComparer.OrdinalIgnoreCase);
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var w in name.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
			if (firsts.Contains(w) && !seen.Add(w)) return true;
		return false;
	}

	// ------------------------------------------------------------------ people
	private HashSet<string> _stageOnlyFirst;

	/// <summary>
	/// A real person's legal first and last name, shaded by the music they play. Stage-only first names
	/// (mononyms like "Cher", which the solo templates use as billing) are redrawn: they are billing,
	/// not what a bass player is called.
	/// </summary>
	public (string first, string last) PersonName(bool isMale, Genre genre, string bandEthnicity, IRandom rng) {
		_stageOnlyFirst ??= new HashSet<string>(Engine.Lexicon.Pool("firstName").Where(e => e.HasTag("stageMononym")).Select(e => e.Word)
			.Except(Engine.Lexicon.Pool("firstName").Where(e => !e.HasTag("stageMononym") && (e.HasTag("white") || e.HasTag("black"))).Select(e => e.Word)),
			StringComparer.Ordinal);
		var ctx = MakeContext(genre, 1963, isMale ? ArtistType.SoloMale : ArtistType.SoloFemale, rng);
		bool isBlack = bandEthnicity == "black" || (IsAfricanAmericanGenre(genre) && rng.Chance(0.8));
		bool isItalian = bandEthnicity == "italian" || (IsEastCoastGenre(genre) && !isBlack && rng.Chance(0.3));
		bool isBritish = bandEthnicity == "british" || IsBritishGenre(genre);
		bool isCountry = genre is Genre.Country or Genre.CountryRock;
		string surnameTag = isItalian ? (rng.Chance(0.5) ? "italian" : "generic")
			: isBritish ? (rng.Chance(0.6) ? "british" : "generic")
			: isCountry ? (rng.Chance(0.35) ? "country" : "generic") : "generic";
		var firstTags = new[] { isMale ? "male" : "female", isBlack ? "black" : "white" };
		string first = Engine.Lexicon.Query("firstName", firstTags, ctx);
		for (int i = 0; i < 4 && _stageOnlyFirst.Contains(first); i++) first = Engine.Lexicon.Query("firstName", firstTags, ctx);
		string last = Engine.Lexicon.Query("lastName", new[] { surnameTag }, ctx);
		return (first, last);
	}

	/// <summary>The ethnicity a whole lineup shares, drawn once per act so its members read as a group.</summary>
	public static string RollBandEthnicity(Genre genre, IRandom rng) {
		if (IsAfricanAmericanGenre(genre) && rng.Chance(0.8)) return "black";
		if (IsBritishGenre(genre)) return "british";
		if (IsEastCoastGenre(genre) && rng.Chance(0.3)) return "italian";
		return null;
	}

	/// <summary>Reserve act names that already exist (a loaded world) so new acts never repeat them.</summary>
	public void ReserveActNames(IEnumerable<string> names) => Engine.Reserve("artist", names);

	// Last-resort billing for a taken name, before Roman numerals: the way a real act told itself
	// apart from a same-named one was to put the front person's name on it.
	private static IEnumerable<string> ActVariants(string baseName, Lineup l, bool solo) {
		var p = l?.Leader;
		if (p == null) yield break;
		if (solo) {
			// Deterministic (string.GetHashCode is randomized per process).
			int h = 0; foreach (char c in p.First + p.Last) h = h * 31 + c;
			const string initials = "ABCDEFGHJKLMNPRSTW";
			yield return $"{p.First} {initials[(h & 0x7fffffff) % initials.Length]}. {p.Last}";
			yield return $"{p.First} {initials[((h >> 5) & 0x7fffffff) % initials.Length]}. {p.Last}";
			if (!p.Female) { yield return $"Little {p.First} {p.Last}"; yield return $"{p.First} {p.Last} Jr."; }
			yield break;
		}
		string core = baseName.StartsWith("The ", StringComparison.Ordinal) ? baseName.Substring(4) : null;
		if (core != null && l.Size >= 3) {
			yield return $"{p.First} and the {core}";
			yield return $"{p.First} {p.Last} and the {core}";
		}
		if (core != null) yield return $"{p.Last}'s {core}";
	}

	public string ChooseArtistSymbol(Genre genre, int year, ArtistType artistType, NamingContext ctx) {
		if (ctx.Lineup?.Size == 1 && artistType is not (ArtistType.SoloMale or ArtistType.SoloFemale))
			artistType = ctx.Lineup.Leader.Female ? ArtistType.SoloFemale : ArtistType.SoloMale;
		return DetermineIfBand(genre, artistType, ctx.Rng)
			? ChooseBandSymbol(genre, year, artistType, ctx)
			: RollSoloDemographics(genre, ctx);
	}

	private string RollSoloDemographics(Genre genre, NamingContext ctx) {
		var rng = ctx.Rng;
		var person = ctx.Lineup?.Leader;
		// An explicit SoloMale/SoloFemale selection FORCES the gender; only Unknown rolls by genre.
		bool isFemale = person != null ? person.Female
					  : ctx.ArtistType == ArtistType.SoloFemale.ToString() ? true
					  : ctx.ArtistType == ArtistType.SoloMale.ToString() ? false
					  : rng.Chance(GetFemaleChance(genre));
		bool isBlack = IsAfricanAmericanGenre(genre) && rng.Chance(0.75);
		bool isHispanic = IsLatinGenre(genre) && !isBlack && rng.Chance(0.6);
		bool isItalian = IsEastCoastGenre(genre) && !isBlack && !isHispanic && rng.Chance(0.35);
		bool isCountry = genre == Genre.Country || genre == Genre.CountryRock;
		bool isJewish = IsBrillBuildingGenre(genre) && !isBlack && !isHispanic && rng.Chance(0.25);

		string ethnicity = isBlack ? "black" : isHispanic ? "hispanic" : isJewish ? "jewish"
						 : isCountry ? "country" : "white";
		ctx.TagSets["name"] = new List<string> { isFemale ? "female" : "male", ethnicity };

		string surname;
		if (isHispanic) surname = "hispanic";
		else if (isCountry) surname = rng.Chance(0.55) ? "country" : "generic";
		else if (isItalian) surname = rng.Chance(0.60) ? "italian" : "generic";
		else if (isJewish) surname = rng.Chance(0.50) ? "jewish" : "generic";
		else surname = "generic";
		ctx.TagSets["surname"] = new List<string> { surname };

		string strategy = PickSoloStrategy(genre, rng);
		return Router?.ResolveSolo(genre.ToString(), strategy) ?? "soloName";
	}

	/// <summary>Weighted solo-act name strategy per genre (doc B §2): realName / nickname / mononym /
	/// honorific / credited. Encodes "nickname density scales with grit" as sampling weights.</summary>
	private static string PickSoloStrategy(Genre g, IRandom rng) {
		(double rn, double nk, double mo, double ho, double cr) w = g switch {
			Genre.DooWop => (0.87, 0.0, 0.0, 0.13, 0.0),
			Genre.Blues or Genre.BluesRock or Genre.BritishBlues => (0.58, 0.28, 0.09, 0.0, 0.05),
			Genre.Jazz => (0.80, 0.20, 0.0, 0.0, 0.0),
			Genre.Soul or Genre.RnB or Genre.Motown => (0.92, 0.0, 0.0, 0.08, 0.0),
			Genre.Gospel => (0.4, 0.05, 0.0, 0.55, 0.0),
			Genre.Country or Genre.CountryRock or Genre.RootsRock => (0.85, 0.13, 0.0, 0.0, 0.02),
			Genre.Folk or Genre.ContemporaryFolk => (0.9, 0.05, 0.05, 0.0, 0.0),
			Genre.SingerSongwriter => (0.95, 0.0, 0.05, 0.0, 0.0),
			Genre.TeenPop => (0.90, 0.10, 0.0, 0.0, 0.0),
			Genre.TraditionalPop => (0.95, 0.0, 0.0, 0.0, 0.05),
			Genre.EasyListening => (0.35, 0.05, 0.1, 0.0, 0.5),
			Genre.Funk => (0.3, 0.55, 0.1, 0.05, 0.0),
			Genre.RockAndRoll => (0.85, 0.10, 0.0, 0.0, 0.05),
			Genre.Classical => (0.5, 0.0, 0.0, 0.0, 0.5),
			Genre.Comedy => (0.3, 0.6, 0.1, 0.0, 0.0),
			Genre.LatinPop or Genre.Boogaloo or Genre.TexMex or Genre.BossaNova => (0.6, 0.15, 0.1, 0.15, 0.0),
			Genre.Reggae or Genre.Ska or Genre.Rocksteady or Genre.SkaRocksteady => (0.5, 0.35, 0.1, 0.05, 0.0),
			_ => (0.7, 0.15, 0.05, 0.05, 0.05)
		};
		double roll = rng.NextDouble() * (w.rn + w.nk + w.mo + w.ho + w.cr);
		if ((roll -= w.rn) < 0) return "realName";
		if ((roll -= w.nk) < 0) return "nickname";
		if ((roll -= w.mo) < 0) return "mononym";
		if ((roll -= w.ho) < 0) return "honorific";
		return "credited";
	}

	private string ChooseBandSymbol(Genre genre, int year, ArtistType artistType, NamingContext ctx) {
		var rng = ctx.Rng;
		var leader = ctx.Lineup?.Leader;
		// Band-leader demographic for $name / $gender. With a lineup the person slots bind to real
		// members, so only the leader's sex still matters (honorific/nickname gender agreement).
		bool leaderBlack = IsAfricanAmericanGenre(genre) && rng.Chance(0.8);
		bool leaderFemale = leader?.Female ?? false;
		ctx.TagSets["name"] = new List<string> { leaderFemale ? "female" : "male", leaderBlack ? "black" : "white" };
		ctx.TagSets["surname"] = new List<string> { "generic" };
		int size = ctx.Lineup?.Size ?? 0;

		// A two-person act is billed as two people far more often than as a group noun (Sam & Dave,
		// Mickey & Sylvia, Jan & Dean, the Everly Brothers).
		if ((artistType == ArtistType.Duo || size == 2) && Engine.HasNameSet("bandName.duo") && rng.Chance(0.6))
			return "bandName.duo";

		// --- genuinely contextual routing (year / artist-type) the genre router can't express ---
		if (year >= 1967 && IsPsychedelicGenre(genre)) return "bandName.psych";
		if (year >= 1964 && year <= 1966 && genre == Genre.RockAndRoll && rng.Chance(0.35))
			return "bandName.british";
		if (genre == Genre.GarageRock && year < 1966) return "bandName.garageEarly";
		if (genre == Genre.Folk || genre == Genre.FolkRock || genre == Genre.ContemporaryFolk) {
			if (artistType == ArtistType.Duo || size == 2) return "bandName.folkDuo";
			if (artistType == ArtistType.Trio || size == 3) return "bandName.folkTrio";
		}
		return Router.Resolve("bandName", genre.ToString()) ?? "bandName.default";
	}

	private static bool DetermineIfBand(Genre genre, ArtistType artistType, IRandom rng) {
		if (artistType == ArtistType.SoloMale || artistType == ArtistType.SoloFemale) return false;
		if (artistType == ArtistType.Band || artistType == ArtistType.Duo ||
			artistType == ArtistType.Trio || artistType == ArtistType.VocalGroup) return true;
		float bandChance = genre switch {
			Genre.RockAndRoll => 0.4f, Genre.GarageRock => 0.92f, Genre.Psychedelic => 0.88f,
			Genre.SurfRock => 0.85f, Genre.BritishInvasion => 0.85f, Genre.Soul => 0.4f,
			Genre.RnB => 0.35f, Genre.DooWop => 0.88f, Genre.GirlGroup => 0.98f, Genre.Folk => 0.25f,
			Genre.Country => 0.12f, Genre.Jazz => 0.35f, Genre.TraditionalPop => 0.08f,
			Genre.TeenPop => 0.12f, Genre.Gospel => 0.7f, _ => 0.5f
		};
		return rng.Chance(bandChance);
	}

	/// <summary>
	/// Whether this act should be a family act, and if so make it one: every member takes the front
	/// person's surname. Kin billing ("The Hart Brothers") is only ever offered to acts this says yes
	/// to, so the name is true by construction. Rates follow the period: family harmony was common in
	/// country, gospel and folk, rare in rock.
	/// </summary>
	public static bool RollFamily(Genre genre, Lineup lineup, IRandom rng) {
		if (lineup == null || lineup.Size < 2) return false;
		double p = genre switch {
			Genre.Country or Genre.Gospel => 0.22,
			Genre.Folk or Genre.ContemporaryFolk => 0.14,
			Genre.DooWop or Genre.RnB or Genre.Soul or Genre.Motown or Genre.GirlGroup => 0.07,
			Genre.EasyListening or Genre.TraditionalPop or Genre.SunshinePop => 0.08,
			Genre.SurfRock or Genre.TeenPop => 0.05,
			_ => 0.03
		};
		if (lineup.Size == 2) p *= 1.6;   // sibling duos were the commonest family act of all
		if (!rng.Chance(p)) return false;
		string surname = lineup.Leader.Last;
		foreach (var m in lineup.People) m.Last = surname;
		lineup.Family = true;
		return true;
	}

	// ================================================================== LABELS
	private readonly HashSet<string> _labelCores = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Keep generation away from names that exist: the historical labels (so there is never
	/// a procedural "Capitol Records" beside the real Capitol) and labels already in a loaded world.
	/// Matching is on the core word, so "Liberty" also blocks "Liberty Sound".</summary>
	public void ReserveLabelNames(IEnumerable<string> names) {
		foreach (var n in names) { string c = LabelCore(n); if (c.Length > 0) _labelCores.Add(c); }
		Engine.Reserve("label", names);
	}

	private static readonly string[] LabelSuffixes = {
		"records", "record", "recording co.", "recording company", "recordings", "sound", "sounds", "music",
		"disc", "discs", "productions", "inc.", "label", "corp.", "corporation", "company", "co.", "uk"
	};

	/// <summary>"Comet Recording Co." -> "comet". The identity two label names collide on.</summary>
	public static string LabelCore(string name) {
		if (string.IsNullOrWhiteSpace(name)) return "";
		string s = name.Trim().ToLowerInvariant();
		bool changed = true;
		while (changed) {
			changed = false;
			foreach (var suf in LabelSuffixes)
				if (s.EndsWith(" " + suf, StringComparison.Ordinal) && s.Length > suf.Length + 1) { s = s.Substring(0, s.Length - suf.Length - 1).TrimEnd(); changed = true; }
		}
		if (s.StartsWith("the ", StringComparison.Ordinal)) s = s.Substring(4);
		return s;
	}

	/// <summary>
	/// A label name in the archetype's voice. <paramref name="founderSurname"/> and
	/// <paramref name="city"/> fill the {founder} / {hq} slots when known, so a runtime-founded label
	/// can be named after the person who founded it -- the commonest real pattern of the period.
	/// </summary>
	public string GenerateLabelName(LabelArchetype archetype, IRandom rng, string founderSurname = null, string city = null, int year = 1962) {
		var ctx = MakeContext(Genre.TraditionalPop, year, ArtistType.Unknown, rng);
		ctx.LabelArchetype = archetype.ToString();
		ctx.Slots["founder"] = !string.IsNullOrWhiteSpace(founderSurname) ? founderSurname
			: Engine.Lexicon.Query("lastName", new[] { "generic" }, ctx);
		ctx.Slots["hq"] = !string.IsNullOrWhiteSpace(city) ? city : Engine.Lexicon.Query("city", new[] { "general" }, ctx);
		string symbol = archetype switch {
			LabelArchetype.SoulFactory => "label.soulFactory",
			LabelArchetype.GospelPowerhouse => "label.gospel",
			LabelArchetype.BluesRoots => "label.bluesRoots",
			LabelArchetype.RockRebel => "label.rockRebel",
			LabelArchetype.CountrySpecialist => "label.country",
			LabelArchetype.CorporateGiant => "label.major",
			LabelArchetype.TeenHitMachine => "label.teen",
			LabelArchetype.FolkBoutique => "label.folk",
			LabelArchetype.JazzPrestige => "label.jazz",
			LabelArchetype.RegionalHustler => "label.indie",
			_ => "label.generic"
		};
		if (!Engine.HasNameSet(symbol)) symbol = "label.generic";
		string hq = ctx.Slots["hq"];
		var opts = new NameEngine.UniqueOptions {
			Finalize = ActNamePolish.Run,
			Accept = n => !_labelCores.Contains(LabelCore(n)),
			Variants = b => new[] { $"{hq} {LabelCore(b).ToTitle()} Records", $"{ctx.Slots["founder"]}-{LabelCore(b).ToTitle()} Records" },
		};
		string name = Engine.Generate(symbol, ctx, "label", nearDup: true, attempts: 40, opts);
		_labelCores.Add(LabelCore(name));
		return name;
	}

	// ================================================================== maps
	public static string StyleTagFor(Genre g) => g switch {
		Genre.Psychedelic or Genre.AcidRock or Genre.PsychedelicRock or Genre.PsychedelicPop
			or Genre.ProgressiveRock or Genre.BaroquePop => "psych",
		Genre.Soul or Genre.RnB or Genre.Motown or Genre.Funk or Genre.Boogaloo => "soul",
		Genre.Country or Genre.CountryRock or Genre.TexMex => "country",
		Genre.SurfRock => "surf",
		Genre.BritishInvasion or Genre.BritishPop or Genre.BritishBeat or Genre.BritishBlues
			or Genre.Skiffle => "british",
		Genre.DooWop or Genre.GirlGroup or Genre.TeenPop or Genre.Bubblegum or Genre.TraditionalPop => "early60s",
		Genre.Folk or Genre.FolkRock or Genre.ContemporaryFolk or Genre.SingerSongwriter => "folk",
		Genre.Gospel => "gospel",
		_ => "universal"
	};

	public static bool IsAfricanAmericanGenre(Genre g) => g switch {
		Genre.Soul or Genre.RnB or Genre.DooWop or Genre.GirlGroup or Genre.Gospel or Genre.Jazz
			or Genre.Motown or Genre.Funk or Genre.Blues => true,
		_ => false
	};
	public static bool IsEastCoastGenre(Genre g) => g == Genre.DooWop || g == Genre.GirlGroup || g == Genre.TeenPop;
	public static bool IsLatinGenre(Genre g) => g == Genre.LatinPop || g == Genre.Boogaloo || g == Genre.TexMex;
	public static bool IsBrillBuildingGenre(Genre g) => g switch {
		Genre.GirlGroup or Genre.TeenPop or Genre.DooWop or Genre.TraditionalPop => true, _ => false
	};
	public static bool IsBritishGenre(Genre genre) => genre is Genre.BritishInvasion
		or Genre.BritishBeat or Genre.BritishPop or Genre.BritishBlues or Genre.Skiffle;
	public static bool IsPsychedelicGenre(Genre g) => g switch {
		Genre.Psychedelic or Genre.GarageRock or Genre.Folk or Genre.AcidRock
			or Genre.PsychedelicRock or Genre.PsychedelicPop => true,
		_ => false
	};
	public static float GetFemaleChance(Genre g) => g switch {
		Genre.GirlGroup => 1.0f, Genre.Soul => 0.35f, Genre.RnB => 0.3f, Genre.Country => 0.25f,
		Genre.TeenPop => 0.3f, Genre.TraditionalPop => 0.35f, Genre.Folk => 0.35f, Genre.Gospel => 0.4f,
		Genre.DooWop => 0.15f, Genre.RockAndRoll => 0.1f, Genre.SurfRock => 0.05f, Genre.GarageRock => 0.05f,
		Genre.Psychedelic => 0.1f, Genre.BritishInvasion => 0.08f, _ => 0.2f
	};
}

internal static class ActNamingStringExt {
	public static string ToTitle(this string s) =>
		string.Join(" ", (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));
}
