// Systems/Naming/NameGenerator.cs
// Godot adapter over the plain-C# naming Core (Systems/Naming/Core). Owns a dedicated
// deterministic RNG stream (NEVER GD.Rand), loads the JSON lexicon + grammar, and re-exposes
// the exact public methods the game already calls. Also drives the NameLab tuner.

using System.Collections.Generic;
using System.Linq;
using Godot;
using LabelMan.Naming;

public partial class NameGenerator : Node {

	public static NameGenerator Instance { get; private set; }

	private const string LexiconPath = "res://Data/Naming/lexicon.json";
	private const string UserLexiconPath = "res://Data/Naming/lexicon.user.json";
	private const string OntologyLexiconPath = "res://Data/Naming/lexicon.ontology.json";
	private const string OntologyBaseLexiconPath = "res://Data/Naming/lexicon.ontology.base.json";
	private const string PeopleLexiconPath = "res://Data/Naming/lexicon.people.json";
	private const string GrammarPath = "res://Data/Naming/grammar.json";
	// Optional data-driven overrides for the six-layer models (embedded defaults apply if absent).
	private const string OntologyPath = "res://Data/Naming/ontology.json";
	private const string MoodsPath = "res://Data/Naming/moods.json";
	private const string InflectionPath = "res://Data/Naming/inflection.json";
	private const string GenresPath = "res://Data/Naming/genres.json";
	private const string TemplatesPath = "res://Data/Naming/templates.json";
	private const string DiminutivesPath = "res://Data/Naming/diminutives.json";

	private NameEngine _engine;
	private TemplateRouter _router;   // ancestry-driven (kind,genre)->set resolution (replaces the switch)
	private IRandom _rng;             // dedicated naming stream, isolated from GD.Rand
	// A SECOND naming stream, for the names of acts and people only.
	//
	// Song and album titles are not merely cosmetic: CompetitorManager hashes a track's title in
	// GetDeterministicTrackTraits to derive its hook, production and danceability, so anything
	// that advances the title stream changes album track traits and therefore the chart. Naming
	// acts and band members off _rng would have made "give the bands real names" a calibration
	// change. On its own stream it is provably inert -- nothing else draws from it.
	private IRandom _actRng;

	// A THIRD naming stream, for the pre-game catalog of legacy standards and recent-hit covers
	// (CompositionCatalogService). Same reasoning as _actRng: those ~1,900 titles are generated once
	// at world build, and drawing them off _rng would advance the title stream and move every album
	// track hashed after them. On its own stream, and written into its own near-dup bucket, it is
	// provably inert to the economy -- nothing else draws from it.
	private IRandom _titleRng;

	// Load in _EnterTree (not _Ready): later autoloads (e.g. ChartManager) generate labels during
	// THEIR _EnterTree, which runs before this node's _Ready. Setting Instance without a loaded
	// engine let those callers see Instance != null but _engine == null, so GenerateLabelName fell
	// back to the literal "Records". Autoloads _EnterTree in listed order, and SimulationSeedBootstrap
	// (which sets the naming seed) precedes us, so loading here is both safe and seed-stable.
	public override void _EnterTree() { Instance = this; EnsureLoaded(); }

	public override void _Ready() { EnsureLoaded(); }

	/// <summary>Idempotent load — safe to call from both _EnterTree and _Ready.</summary>
	private void EnsureLoaded() { if (_engine == null) Load(); }

	// ------------------------------------------------------------------ loading
	private void Load() {
		string lexJson = ReadFile(LexiconPath);
		string grammarJson = ReadFile(GrammarPath);
		if (string.IsNullOrEmpty(lexJson) || string.IsNullOrEmpty(grammarJson)) {
			GD.PushError($"NameGenerator: could not load naming data ({LexiconPath} / {GrammarPath}).");
			return;
		}
		var lexicon = Lexicon.LoadFromJson(lexJson);
		string ontJson = ReadFile(OntologyLexiconPath);
		if (!string.IsNullOrEmpty(ontJson)) lexicon.AppendJson(ontJson);   // Layer-3 curated tagged pools
		string ontBaseJson = ReadFile(OntologyBaseLexiconPath);
		if (!string.IsNullOrEmpty(ontBaseJson)) lexicon.AppendJson(ontBaseJson); // Layer-3 base-word re-tagging
		string peopleJson = ReadFile(PeopleLexiconPath);
		if (!string.IsNullOrEmpty(peopleJson)) lexicon.AppendJson(peopleJson);   // central people pool (hispanic/jewish)
		string userJson = ReadFile(UserLexiconPath);
		if (!string.IsNullOrEmpty(userJson)) lexicon.AppendJson(userJson);

		// Build the six-layer model bundle, applying optional JSON overrides atop embedded defaults.
		var ontology = new TagOntology();       ontology.LoadJson(ReadFile(OntologyPath));
		var moods = new MoodGraph();            moods.LoadJson(ReadFile(MoodsPath));
		var inflection = new Inflection();      inflection.LoadJson(ReadFile(InflectionPath));
		var genres = new GenreLibrary();        genres.LoadJson(ReadFile(GenresPath));
		var diminutives = new Diminutives();    diminutives.LoadJson(ReadFile(DiminutivesPath));   // Layer: solo-act grooming
		var models = new NameModels(ontology, moods, inflection, genres, diminutives);

		var grammar = GrammarEngine.ParseGrammar(grammarJson);
		_engine = new NameEngine(lexicon, grammar, models);

		// Optional Layer-2 constraint-template sets (richer, mood-coherent generators).
		string templatesJson = ReadFile(TemplatesPath);
		if (!string.IsNullOrEmpty(templatesJson))
			foreach (var kv in ConstraintTemplateLoader.Parse(templatesJson)) _engine.AddConstraintSet(kv.Key, kv.Value);

		// Ancestry-driven template routing (docs D/E). Existence spans both layers so it can route to
		// constraint sets and grammar-only buckets alike.
		_router = new TemplateRouter(models.Genres, key => _engine.HasNameSet(key));

		_rng = new DeterministicRandom(DeriveSeed());
		_actRng = new DeterministicRandom(DeriveSeed() ^ 0x6163746E616D6573UL); // "actnames"
		_titleRng = new DeterministicRandom(DeriveSeed() ^ 0x636174616C74746CUL); // "cataltl" -- pre-game catalog titles
		var moodIssues = moods.Validate();
		if (moodIssues.Count > 0) GD.PushWarning($"NameGenerator mood matrix: {string.Join("; ", moodIssues)}");
		// Per-genre coherence (doc H #4): flag any resolved profile whose affinity moods are
		// disconnected/isolated at its threshold — those silently deadlock into grammar fallback.
		var genreIssues = new List<string>();
		foreach (var gid in models.Genres.Ids) {
			var gp = models.Genres.Get(gid);
			genreIssues.AddRange(moods.ValidateGenre(gid, gp.MoodAffinity.Keys, gp.MoodThreshold));
		}
		if (genreIssues.Count > 0) GD.PushWarning($"NameGenerator genre mood-coherence:\n  {string.Join("\n  ", genreIssues)}");
		AuditRouting();
		GD.Print($"NameGenerator ready: {lexicon.Count} words, {grammar.Count} symbols, {models.Genres.Ids.Count()} genre profiles.");
	}

	/// <summary>Seed the naming stream deterministically from the sim seed but on a SEPARATE
	/// stream (a fixed mix), so naming never consumes the global GD.Rand sequence.</summary>
	private static ulong DeriveSeed() {
		ulong baseSeed = SimulationSeedBootstrap.RequestedSeed ?? (ulong)System.DateTime.Now.Ticks;
		return baseSeed ^ 0x9E3779B97F4A7C15UL;
	}

	private static string ReadFile(string resPath) {
		if (!FileAccess.FileExists(resPath)) return null;
		using var f = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
		return f?.GetAsText();
	}

	public bool IsReady() => _engine != null;

	// ============================================================ ARTIST / BAND
	public string GenerateArtistName(Genre genre, int year, ArtistType artistType,
									 string regionId = null, LabelArchetype? labelStyle = null) {
		// Callers must gate on IsReady() rather than relying on this fallback: it is the one
		// path in the act-naming family that touches the global stream, and ArtistManager's
		// population path cannot afford a draw from it.
		if (_engine == null) return $"Artist {GD.Randi()}";
		var ctx = MakeContext(genre, year, artistType, _actRng);
		ctx.RegionId = regionId;
		if (labelStyle.HasValue) ctx.LabelArchetype = labelStyle.Value.ToString();

		string symbol = ChooseArtistSymbol(genre, year, artistType, ctx);
		return _engine.Generate(symbol, ctx, "artist", nearDup: true);
	}

	/// <summary>
	/// A person's name, shaded by the music they play.
	/// <para>
	/// The genre argument is what stops a doo-wop group being five men called Smith. It was
	/// hard-coded to <c>{gender, "white"}</c> and <c>{"generic"}</c>, which is the same query for
	/// every act in the game, so the only variation the lexicon could offer was the draw itself.
	/// The ethnicity reads mirror <see cref="GenerateBandMemberName"/>, which had this right
	/// already and has never had a caller.
	/// </para>
	/// </summary>
	public (string firstName, string lastName) GeneratePersonName(bool isMale,
		Genre genre = Genre.TraditionalPop, string bandEthnicity = null) {
		if (_engine == null) return (isMale ? "John" : "Jane", "Doe");
		var ctx = MakeContext(genre, 1963, isMale ? ArtistType.SoloMale : ArtistType.SoloFemale, _actRng);
		bool isBlack = bandEthnicity == "black" || (IsAfricanAmericanGenre(genre) && _actRng.Chance(0.8));
		bool isItalian = bandEthnicity == "italian" || (IsEastCoastGenre(genre) && !isBlack && _actRng.Chance(0.3));
		bool isBritish = bandEthnicity == "british" || IsBritishGenre(genre);
		string surnameTag = isItalian ? (_actRng.Chance(0.5) ? "italian" : "generic")
			: isBritish ? (_actRng.Chance(0.6) ? "british" : "generic") : "generic";
		string first = _engine.Lexicon.Query("firstName",
			new[] { isMale ? "male" : "female", isBlack ? "black" : "white" }, ctx);
		string last = _engine.Lexicon.Query("lastName", new[] { surnameTag }, ctx);
		return (first, last);
	}

	/// <summary>
	/// The ethnicity a whole lineup shares, drawn once per act so its members read as a group
	/// rather than as five independent draws. Consumes the act stream, never the title stream.
	/// </summary>
	public string RollBandEthnicity(Genre genre) {
		if (_engine == null) return null;
		if (IsAfricanAmericanGenre(genre) && _actRng.Chance(0.8)) return "black";
		if (IsBritishGenre(genre)) return "british";
		if (IsEastCoastGenre(genre) && _actRng.Chance(0.3)) return "italian";
		return null;
	}

	private static bool IsBritishGenre(Genre genre) => genre is Genre.BritishInvasion
		or Genre.BritishBeat or Genre.BritishPop or Genre.BritishBlues or Genre.Skiffle;

	/// <summary>Route (genre, year, type) to the artist grammar symbol AND populate the
	/// demographic tag-sets on <paramref name="ctx"/>. Shared by the game and the tuner; all
	/// rolls draw from <c>ctx.Rng</c> so a seeded tuner batch is reproducible.</summary>
	private string ChooseArtistSymbol(Genre genre, int year, ArtistType artistType, NamingContext ctx) {
		return DetermineIfBand(genre, artistType, ctx.Rng)
			? ChooseBandSymbol(genre, year, artistType, ctx)
			: RollSoloDemographics(genre, ctx);
	}

	private string RollSoloDemographics(Genre genre, NamingContext ctx) {
		var rng = ctx.Rng;
		// An explicit SoloMale/SoloFemale selection FORCES the gender; only Unknown rolls by genre.
		bool isFemale = ctx.ArtistType == ArtistType.SoloFemale.ToString() ? true
					  : ctx.ArtistType == ArtistType.SoloMale.ToString() ? false
					  : rng.Chance(GetFemaleChance(genre));
		bool isBlack = IsAfricanAmericanGenre(genre) && rng.Chance(0.75);
		bool isHispanic = IsLatinGenre(genre) && !isBlack && rng.Chance(0.6);
		bool isItalian = IsEastCoastGenre(genre) && !isBlack && !isHispanic && rng.Chance(0.35);
		bool isCountry = genre == Genre.Country || genre == Genre.CountryRock;
		bool isJewish = IsBrillBuildingGenre(genre) && !isBlack && !isHispanic && rng.Chance(0.25);

		// firstName ethnicity draws from the central people pool (white/black/hispanic/jewish/country).
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

		// Solo strategy layer (doc B): pick a name strategy weighted by genre (nickname-dominant for
		// blues/funk, honorific for gospel, credited for easy/classical…), then route to the
		// soloAct.<genre>.<strategy> set. The set's $name/$surname/$gender slots bind to the demographic
		// rolled just above. Falls back to the legacy soloName grammar only if nothing resolves.
		string strategy = PickSoloStrategy(genre, rng);
		return _router?.ResolveSolo(genre.ToString(), strategy) ?? "soloName";
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
		// band-leader demographic for $name patterns
		bool leaderBlack = IsAfricanAmericanGenre(genre) && rng.Chance(0.8);
		ctx.TagSets["name"] = new List<string> { "male", leaderBlack ? "black" : "white" };
		ctx.TagSets["surname"] = new List<string> { "generic" };

		// --- genuinely contextual routing (year / artist-type) the genre router can't express ---
		if (year >= 1967 && IsPsychedelicGenre(genre)) return "bandName.psych";      // late-60s psych era pull
		if (year >= 1964 && year <= 1966 && genre == Genre.RockAndRoll && rng.Chance(0.35))
			return "bandName.british";                                              // Merseybeat wave
		if (genre == Genre.GarageRock && year < 1966) return "bandName.garageEarly"; // pre-fuzz garage
		if (genre == Genre.Folk || genre == Genre.FolkRock || genre == Genre.ContemporaryFolk) {
			if (artistType == ArtistType.Duo) return "bandName.folkDuo";
			if (artistType == ArtistType.Trio) return "bandName.folkTrio";
		}

		// --- genre -> set via the ancestry-walking router (replaces the old bucket switch) ---
		return _router.Resolve("bandName", genre.ToString()) ?? "bandName.default";
	}

	/// <summary>Load-time audit (doc E §2.4): report every genre×kind that degrades to the generic set
	/// or grammar, turning "silently bland genre" into a visible, shrinking authoring checklist.</summary>
	private void AuditRouting() {
		if (_router == null) return;
		string[] kinds = { "songTitle", "bandName" };
		var lines = new List<string>();
		foreach (Genre g in System.Enum.GetValues(typeof(Genre)))
			foreach (var kind in kinds) {
				var info = _router.Explain(kind, g.ToString());
				if (info.Kind == RouteKind.Grammar) lines.Add($"{g}.{kind} -> GRAMMAR (no set, no family)");
				else if (info.Kind == RouteKind.Generic) lines.Add($"{g}.{kind} -> generic '{info.Key}'");
			}
		if (lines.Count > 0) GD.Print($"[naming routing audit] {lines.Count} genre/kind(s) below family:\n  " + string.Join("\n  ", lines));
		else GD.Print("[naming routing audit] every genre reaches a bespoke or family set for song + band.");
	}

	private bool DetermineIfBand(Genre genre, ArtistType artistType, IRandom rng) {
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

	// =============================================================== SONG TITLES
	/// <param name="artistKey">
	/// Identifies the ACT for near-duplicate bookkeeping, so one band does not keep writing the
	/// same song. Pass a stable id, never a display name.
	/// <para>
	/// It used to be handed <c>stageName</c>, which quietly made the act's NAME an input to its
	/// song titles — and titles are hashed by
	/// <c>CompetitorManager.GetDeterministicTrackTraits</c> into hook, production and
	/// danceability. So renaming the bands moved the chart: measured, giving acts real names
	/// changed `genre-decade-shape`, `format-mix` and `year-end-hot100` while leaving
	/// `album-composition` byte-identical, which is exactly the signature of a title-derived
	/// trait shift. Keyed on the id, cosmetic naming work is free again.
	/// </para>
	/// </param>
	public string GenerateSongTitle(Genre genre, int year, string artistKey = null) {
		if (_engine == null) return $"Untitled {GD.Randi()}";
		var ctx = MakeContext(genre, year, ArtistType.Unknown);
		string symbol = ChooseSongSymbol(genre, year, ctx.Rng);
		string bucket = "song|" + (artistKey ?? "");
		return _engine.Generate(symbol, ctx, bucket, nearDup: false, attempts: 30);
	}

	/// <summary>
	/// A song title for a pre-game catalog composition (legacy standard / recent-hit cover). Same
	/// engine and routing as <see cref="GenerateSongTitle"/>, but drawn from the dedicated catalog
	/// stream (<c>_titleRng</c>) and written into a dedicated near-dup bucket that no runtime caller
	/// touches -- so seeding ~1,900 catalog titles at world build never advances the runtime title
	/// stream the album-track hash reads. Returns null if the engine isn't up (headless tools without
	/// the naming autoload), so callers can fall back.
	/// </summary>
	public string GenerateCatalogTitle(Genre genre, int year) {
		if (_engine == null) return null;
		var ctx = MakeContext(genre, year, ArtistType.Unknown, _titleRng);
		string symbol = ChooseSongSymbol(genre, year, ctx.Rng);
		return _engine.Generate(symbol, ctx, "song|catalog", nearDup: false, attempts: 30);
	}

	private string ChooseSongSymbol(Genre genre, int year, IRandom rng) {
		// Ancestry-driven routing (docs D/E) replaces the old genre->bucket switch. Each genre reaches
		// its most specific set, then its family, then a set-less genre falls to the era blend below.
		string key = _router.Resolve("songTitle", genre.ToString());
		if (key != null) return key;
		// Ultimate fallback for a genre with no set and no family rung: the legacy era-appropriate blend.
		if (year < 1964) return "songTitle.early60s";
		if (year < 1967) return rng.Chance(0.5) ? "songTitle.early60s" : "songTitle.soul";
		return rng.Next(3) switch { 0 => "songTitle.psych", 1 => "songTitle.soul", _ => "songTitle.early60s" };
	}

	public string GenerateBSideTitle(Genre genre, int year, string aSideTitle) {
		if (_engine == null) return GenerateSongTitle(genre, year);
		int roll = _rng.Next(5);
		if (roll == 0)
			return _rng.Chance(0.3) ? $"{aSideTitle} (Instrumental)" : GenerateInstrumentalTitle(genre, year);
		if (roll == 2) {
			var ctx = MakeContext(genre, year, ArtistType.Unknown);
			return _engine.ExpandOnce("bSide", ctx);
		}
		return GenerateSongTitle(genre, year);
	}

	// ================================================================== ALBUMS
	/// <summary>Genre-flavored album title. <paramref name="leadSingle"/> (the album's designated hit
	/// track, generated before the title) binds the %leadSingle% slot so soul/country/garage LPs can be
	/// named after their single; omit it and those templates drop gracefully. %selfTitle% binds the
	/// act's own name. Routes to albumTitle.&lt;genre&gt; via the ancestry walk, else the generic set.</summary>
	/// <param name="artistKey">
	/// The near-duplicate bucket key, separated from <paramref name="artistName"/> because this
	/// method uses that one value for two jobs: it fills ctx.Slots and is returned verbatim on the
	/// self-titled branch (DISPLAY), and it keys the near-dup bucket (IDENTITY). A display name is
	/// the wrong thing to key on -- two acts can carry the same name, and the whole population
	/// shared one placeholder name until the GenerateStageName gate was fixed -- so callers pass
	/// artistId here, matching GenerateSongTitle, while artistName keeps filling the slots.
	/// Defaults to artistName so existing callers are unchanged.
	/// </param>
	public string GenerateAlbumTitle(Genre genre, int year, string artistName, bool isCompilation = false, string leadSingle = null, string artistKey = null) {
		if (_engine == null) return artistName;
		var ctx = MakeContext(genre, year, ArtistType.Unknown);
		ctx.Slots["artist"] = artistName;
		ctx.Slots["selfTitle"] = artistName;
		if (!string.IsNullOrWhiteSpace(leadSingle)) { ctx.Slots["leadSingle"] = leadSingle; ctx.Slots["titleTrack"] = leadSingle; }
		if (isCompilation) return _engine.ExpandOnce("compilationTitle", ctx);
		double r = _rng.NextDouble();
		if (r < 0.08) return artistName;                          // self-titled (rare; the sets also self-title)
		if (r < 0.20) return _engine.ExpandRouted("albumFormat", ctx);
		string key = _router?.Resolve("albumTitle", genre.ToString()) ?? "albumTitle";
		return _engine.Generate(key, ctx, "album|" + (artistKey ?? artistName), nearDup: false, attempts: 30);
	}

	public string GenerateInstrumentalTitle(Genre genre, int year) {
		if (_engine == null) return "Instrumental";
		return _engine.ExpandOnce("instrumentalTitle", MakeContext(genre, year, ArtistType.Unknown));
	}

	// ================================================================== LABELS
	public string GenerateLabelName(LabelArchetype archetype) {
		if (_engine == null) return "Records";
		var ctx = MakeContext(Genre.TraditionalPop, 1962, ArtistType.Unknown);
		ctx.LabelArchetype = archetype.ToString();
		string symbol = archetype switch {
			LabelArchetype.SoulFactory or LabelArchetype.GospelPowerhouse => "label.soulFactory",
			LabelArchetype.BluesRoots => "label.bluesRoots",
			LabelArchetype.RockRebel or LabelArchetype.CountrySpecialist => "label.rockRebel",
			LabelArchetype.CorporateGiant or LabelArchetype.TeenHitMachine => "label.major",
			LabelArchetype.FolkBoutique or LabelArchetype.JazzPrestige or LabelArchetype.RegionalHustler => "label.indie",
			_ => "label.generic"
		};
		return _engine.Generate(symbol, ctx, "label", nearDup: true);
	}

	// ================================================================== VENUES
	public string GenerateVenueName(VenueType venueType, string city = null) {
		if (_engine == null) return "The Club";
		var ctx = MakeContext(Genre.TraditionalPop, 1963, ArtistType.Unknown);
		ctx.Slots["city"] = city ?? _engine.Lexicon.Query("city", new[] { "general" }, ctx);
		string symbol = venueType switch {
			VenueType.SmallClub => "venue.smallClub", VenueType.Theater => "venue.theater",
			VenueType.Arena => "venue.arena", VenueType.Stadium => "venue.stadium",
			VenueType.CoffeHouse => "venue.coffeeHouse", VenueType.HonkyTonk => "venue.honkyTonk",
			VenueType.JukeJoint => "venue.jukeJoint", _ => "venue.generic"
		};
		return _engine.ExpandOnce(symbol, ctx);
	}

	// =========================================================== MISC ENTITIES
	public string GenerateRadioStationName(string city = null) {
		if (_engine == null) return "WXYZ Radio";
		var ctx = MakeContext(Genre.TraditionalPop, 1963, ArtistType.Unknown);
		ctx.Slots["city"] = city ?? _engine.Lexicon.Query("city", new[] { "general" }, ctx);
		return _engine.ExpandOnce("radioStation", ctx);
	}

	public string GeneratePublicationName() {
		if (_engine == null) return "Music Magazine";
		return _engine.ExpandOnce("publication", MakeContext(Genre.TraditionalPop, 1963, ArtistType.Unknown));
	}

	public string GenerateTourName(string artistName, int year, Genre genre) {
		if (_engine == null) return $"{artistName} Tour";
		var ctx = MakeContext(genre, year, ArtistType.Unknown);
		ctx.Slots["artist"] = artistName;
		return _engine.ExpandOnce("tour", ctx);
	}

	public string GenerateAwardName() {
		if (_engine == null) return "Music Award";
		return _engine.ExpandOnce("award", MakeContext(Genre.TraditionalPop, 1963, ArtistType.Unknown));
	}

	public string GenerateFanClubName(string artistName) {
		if (_engine == null) return $"{artistName} Fan Club";
		var ctx = MakeContext(Genre.TraditionalPop, 1963, ArtistType.Unknown);
		ctx.Slots["artist"] = artistName;
		return _engine.ExpandOnce("fanClub", ctx);
	}

	public string GenerateSongwriterName(Genre genre, bool isTeam = false) {
		if (_engine == null) return "Songwriter";
		var ctx = MakeContext(genre, 1963, ArtistType.Unknown);
		if (isTeam) return _engine.ExpandOnce("songwriterTeam", ctx);
		bool isFemale = _rng.Chance(0.3);
		bool isJewish = IsBrillBuildingGenre(genre) && _rng.Chance(0.4);
		ctx.TagSets["name"] = new List<string> { isFemale ? "female" : "male", "white" };
		ctx.TagSets["surname"] = new List<string> { isJewish ? "jewish" : "generic" };
		return _engine.ExpandOnce("songwriter", ctx);
	}

	public string GenerateProducerName(Genre genre) {
		if (_engine == null) return "Producer";
		var ctx = MakeContext(genre, 1963, ArtistType.Unknown);
		ctx.TagSets["name"] = new List<string> { "male", "white" };
		ctx.TagSets["surname"] = new List<string> { "generic" };
		return _engine.ExpandOnce("producer", ctx);
	}

	public string GenerateBandMemberName(Genre genre, bool isFemale, string bandEthnicity = null) {
		if (_engine == null) return "Member";
		var ctx = MakeContext(genre, 1963, ArtistType.Unknown, _actRng);
		bool isBlack = bandEthnicity == "black" || (IsAfricanAmericanGenre(genre) && _actRng.Chance(0.8));
		bool isItalian = bandEthnicity == "italian" || (IsEastCoastGenre(genre) && !isBlack && _actRng.Chance(0.3));
		bool isBritish = genre == Genre.BritishInvasion || bandEthnicity == "british";
		string ethnicity = isBlack ? "black" : "white";
		ctx.TagSets["name"] = new List<string> { isFemale ? "female" : "male", ethnicity };
		string surname = isItalian ? (_actRng.Chance(0.5) ? "italian" : "generic")
					   : isBritish ? (_actRng.Chance(0.6) ? "british" : "generic") : "generic";
		ctx.TagSets["surname"] = new List<string> { surname };
		return _engine.ExpandOnce("bandMember", ctx);
	}

	// ============================================================ TUNER SUPPORT

	// Synthetic "routed" categories the tuner prepends to the raw grammar symbols. Selecting one
	// runs the game's genre->symbol routing, so the Genre/Type menus actually drive the output.
	public const string CatArtist = "» Artist name (by genre)";
	public const string CatSong   = "» Song title (by genre)";
	public const string CatAlbum  = "» Album title (by genre)";
	public const string CatPerson = "» Person (people pool)";

	/// <summary>Routed meta-categories, listed first in the tuner ahead of the raw grammar symbols.</summary>
	public string[] RoutedCategories() => new[] { CatArtist, CatSong, CatAlbum, CatPerson };

	/// <summary>Raw grammar symbols the NameLab tuner can spin directly.</summary>
	public string[] AvailableSymbols() => _engine == null ? System.Array.Empty<string>()
		: _engine.AvailableSymbols.ToArray();

	/// <summary>Layer-2 constraint-template sets (from templates.json) the tuner can spin.</summary>
	public string[] ConstraintCategories() => _engine == null ? System.Array.Empty<string>()
		: _engine.ConstraintSymbols.OrderBy(s => s).ToArray();

	/// <summary>Spin N names for a category (a routed meta-category or a raw grammar symbol).
	/// A given <paramref name="seed"/> makes the batch reproducible; blank uses the shared stream.
	/// <paramref name="artistName"/> fills {artist} for artist-based symbols (blank auto-generates a
	/// fresh band name per spin). <paramref name="coinSurname"/> uses a Markov-coined surname for the
	/// Person pool.</summary>
	public string[] Spin(string category, Genre genre, int year, ArtistType artistType,
						 LabelArchetype archetype, string artistName, bool coinSurname, int count, ulong? seed = null) {
		if (_engine == null) return new[] { "(engine not ready)" };
		IRandom rng = seed.HasValue ? new DeterministicRandom(seed.Value) : _rng;
		var results = new string[count];
		for (int i = 0; i < count; i++) {
			var ctx = MakeContext(genre, year, artistType, rng);
			ctx.LabelArchetype = archetype.ToString();
			ctx.Slots["city"] = _engine.Lexicon.Query("city", new[] { "general" }, ctx);
			// default demographics for symbols reading $name/$surname (routing overrides below).
			ctx.TagSets["name"] = new List<string> { artistType == ArtistType.SoloFemale ? "female" : "male", "white" };
			ctx.TagSets["surname"] = new List<string> { "generic" };
			results[i] = SpinOne(category, genre, year, artistType, artistName, coinSurname, ctx);
		}
		return results;
	}

	private string SpinOne(string category, Genre genre, int year, ArtistType type,
						   string artistName, bool coinSurname, NamingContext ctx) {
		// Layer-2 constraint-template set (from templates.json)
		if (_engine.HasConstraintSet(category)) {
			ctx.Slots["artist"] = ResolveArtist(artistName, genre, year, ctx);
			string s = _engine.FillConstraint(category, ctx);
			return string.IsNullOrEmpty(s) ? "(no satisfiable template for this genre/year)" : s;
		}
		switch (category) {
			case CatArtist:
				return _engine.ExpandRouted(ChooseArtistSymbol(genre, year, type, ctx), ctx);
			case CatSong:
				return _engine.ExpandRouted(ChooseSongSymbol(genre, year, ctx.Rng), ctx);
			case CatAlbum: {
				ctx.Slots["artist"] = ResolveArtist(artistName, genre, year, ctx);
				ctx.Slots["selfTitle"] = ctx.Slots["artist"];           // %leadSingle% unavailable here -> those templates drop
				double r = ctx.Rng.NextDouble();
				if (r < 0.10) return ctx.Slots["artist"];               // self-titled
				if (r < 0.30) return _engine.ExpandRouted("albumFormat", ctx);
				return _engine.ExpandRouted(_router?.Resolve("albumTitle", genre.ToString()) ?? "albumTitle", ctx);
			}
			case CatPerson: {
				// Central people pool: show variety across the four ethnicity categories. An explicit
				// SoloMale/SoloFemale still forces gender; ethnicity rotates so the pool is visible.
				string g = type == ArtistType.SoloFemale ? "female" : type == ArtistType.SoloMale ? "male"
						 : (ctx.Rng.Chance(0.5) ? "female" : "male");
				string eth = new[] { "white", "black", "hispanic", "jewish" }[ctx.Rng.Next(4)];
				ctx.TagSets["name"] = new List<string> { g, eth };
				ctx.TagSets["surname"] = new List<string> {
					eth == "hispanic" ? "hispanic" : eth == "jewish" ? "jewish" : "generic" };
				return _engine.ExpandOnce(coinSurname ? "personName.coined" : "personName", ctx);
			}
			default:
				ctx.Slots["artist"] = ResolveArtist(artistName, genre, year, ctx);
				return _engine.ExpandRouted(category, ctx);
		}
	}

	/// <summary>The band/artist name to drop into {artist}: the user-supplied name if any, else a
	/// fresh routed band name so album/tour/fan-club spins vary instead of repeating one constant.</summary>
	private string ResolveArtist(string artistName, Genre genre, int year, NamingContext ctx) {
		if (!string.IsNullOrWhiteSpace(artistName)) return artistName.Trim();
		var bctx = ctx.Clone();
		bctx.TagSets = new Dictionary<string, List<string>>(ctx.TagSets); // isolate demographic rolls
		return _engine.ExpandRouted(ChooseArtistSymbol(genre, year, ArtistType.Band, bctx), bctx);
	}

	// ------------------------------------------------------- ENGINE INSPECTOR (tuner)
	/// <summary>Read-only view of a genre's resolved profile (Layer 1) for the tuner's inspector.</summary>
	public sealed class GenreProfileView {
		public string Id, Extends, Orthography;
		public double MoodThreshold;
		public (string dim, double val)[] Voice;
		public (string tag, double w)[] DomainAffinity;
		public (string mood, double w)[] MoodAffinity;
		public string[] Suppress;
		public double[] EraCurve;
	}

	/// <summary>Resolve and expose the genre profile powering a genre (voice vector, affinities,
	/// mood threshold, orthography, era curve). Edit these in genres.json + Reload to change them.</summary>
	public GenreProfileView InspectGenre(Genre genre) {
		if (_engine == null) return null;
		var p = _engine.Models.Genres.Get(genre.ToString());
		var voice = new (string, double)[VoiceVector.Dims.Length];
		for (int i = 0; i < VoiceVector.Dims.Length; i++) voice[i] = (VoiceVector.Dims[i], p.Voice[i]);
		return new GenreProfileView {
			Id = p.Id, Extends = p.Extends, Orthography = p.Orthography.ToString(), MoodThreshold = p.MoodThreshold,
			Voice = voice,
			DomainAffinity = p.DomainAffinity.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToArray(),
			MoodAffinity = p.MoodAffinity.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToArray(),
			Suppress = p.Suppress.OrderBy(s => s).ToArray(),
			EraCurve = p.EraCurve
		};
	}

	/// <summary>Inflection-engine (Layer 6) tester: all forms of a lemma for the tuner.</summary>
	public string[] InflectForms(string word) {
		word = word?.Trim();
		if (_engine == null || string.IsNullOrEmpty(word)) return System.Array.Empty<string>();
		var inf = _engine.Models.Inflection;
		return new[] {
			$"plural:  {inf.Inflect(word, InflForm.Plural)}",
			$"gerund:  {inf.Inflect(word, InflForm.Ger)}",
			$"past:    {inf.Inflect(word, InflForm.Past)}",
			$"3rd sg:  {inf.Inflect(word, InflForm.ThirdSing)}",
			$"poss:    {inf.Inflect(word, InflForm.Possessive)}",
			$"comp:    {inf.Inflect(word, InflForm.Comparative)}",
			$"super:   {inf.Inflect(word, InflForm.Superlative)}",
		};
	}

	// ----------------------------------------------------------- DICTIONARY VIEW
	/// <summary>One editable lexicon group a category queries.</summary>
	public sealed class LexGroupView {
		public string Pos;
		public string[] Tags;
		public string Label;          // e.g. "noun [psych]"
		public WordView[] Words;
	}
	public sealed class WordView { public string Word; public int? EraStart; public int? EraEnd; public string[] Tags; }

	/// <summary>The distinct lexicon groups (pos + resolved tags) the given category queries,
	/// each with its current word list — the backing data for the tuner's dictionary panel.</summary>
	public LexGroupView[] GroupsForCategory(string category, Genre genre, int year, ArtistType type) {
		if (_engine == null) return System.Array.Empty<LexGroupView>();
		var ctx = MakeContext(genre, year, type, new DeterministicRandom(1UL));
		ctx.TagSets["name"] = new List<string> { type == ArtistType.SoloFemale ? "female" : "male", "white" };
		ctx.TagSets["surname"] = new List<string> { "generic" };

		var seen = new Dictionary<string, LexGroupView>();
		foreach (var sym in SymbolsForCategory(category, genre, year, type, ctx)) {
			// Constraint-routed symbol: show the ontology-filtered slot pools the templates actually
			// draw from (with each word's stored axis tags), not the underlying grammar symbol's pools.
			if (_engine.HasConstraintSet(sym)) {
				foreach (var sg in _engine.ConstraintSlotGroups(sym)) {
					string key = sg.Label;
					if (seen.ContainsKey(key)) continue;
					var words = sg.Words
						.Select(e => new WordView { Word = e.Word, EraStart = e.EraStart, EraEnd = e.EraEnd,
													Tags = _engine.TagsForWord(sg.Pos, e.Word).ToArray() })
						.OrderBy(w => w.Word, System.StringComparer.OrdinalIgnoreCase).ToArray();
					seen[key] = new LexGroupView { Pos = sg.Pos, Tags = sg.Tags.ToArray(), Label = sg.Label, Words = words };
				}
				continue;
			}
			foreach (var tmpl in _engine.Grammar.Templates(sym))
				foreach (var (pos, tags) in ExtractGroups(tmpl, ctx)) {
					string key = pos + "|" + string.Join(",", tags.OrderBy(t => t, System.StringComparer.OrdinalIgnoreCase));
					if (seen.ContainsKey(key)) continue;
					var words = _engine.Lexicon.Entries(pos, tags)
						.Select(e => new WordView { Word = e.Word, EraStart = e.EraStart, EraEnd = e.EraEnd, Tags = tags.ToArray() })
						.OrderBy(w => w.Word, System.StringComparer.OrdinalIgnoreCase).ToArray();
					seen[key] = new LexGroupView {
						Pos = pos, Tags = tags.ToArray(),
						Label = tags.Count == 0 ? pos : $"{pos} [{string.Join(",", tags)}]",
						Words = words
					};
				}
		}
		return seen.Values.OrderBy(g => g.Label, System.StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private string[] SymbolsForCategory(string category, Genre genre, int year, ArtistType type, NamingContext probe) {
		switch (category) {
			case CatArtist: return new[] { ChooseArtistSymbol(genre, year, type, probe) };
			case CatSong:   return new[] { ChooseSongSymbol(genre, year, probe.Rng) };
			case CatAlbum:  return new[] { "albumTitle", "albumFormat" };
			case CatPerson: return new[] { "personName" };
			default:        return new[] { category };
		}
	}

	/// <summary>Pull the lexicon queries out of a grammar template: {[~]pos:tags[.mods]}. Skips
	/// built-in functions and untagged slots ({artist}, {city}). Mirrors GrammarEngine tag rules.</summary>
	private IEnumerable<(string pos, List<string> tags)> ExtractGroups(string tmpl, NamingContext ctx) {
		var outp = new List<(string, List<string>)>();
		if (string.IsNullOrEmpty(tmpl)) return outp;
		for (int i = 0; i < tmpl.Length; ) {
			if (tmpl[i] != '{') { i++; continue; }
			int end = tmpl.IndexOf('}', i + 1);
			if (end < 0) break;
			string spec = tmpl.Substring(i + 1, end - i - 1).Split('.')[0]; // drop modifiers
			i = end + 1;
			if (spec.StartsWith("~")) spec = spec.Substring(1);
			int colon = spec.IndexOf(':');
			if (colon < 0) continue;                                        // slot or function, not a group
			string pos = spec.Substring(0, colon);
			if (pos.Length == 0 || GrammarEngine.IsFunction(pos)) continue;
			outp.Add((pos, ResolveTags(spec.Substring(colon + 1), ctx)));
		}
		return outp;
	}

	private static List<string> ResolveTags(string tagStr, NamingContext ctx) {
		var outTags = new List<string>();
		if (string.IsNullOrWhiteSpace(tagStr)) return outTags;
		foreach (var raw in tagStr.Split(',')) {
			string t = raw.Trim();
			if (t.Length == 0) continue;
			if (t.Equals("$style", System.StringComparison.OrdinalIgnoreCase)) {
				if (ctx.StyleTags?.Count > 0) outTags.Add(ctx.StyleTags[0]);
			} else if (t.Equals("$genre", System.StringComparison.OrdinalIgnoreCase)) {
				if (!string.IsNullOrEmpty(ctx.Genre)) outTags.Add(ctx.Genre.ToLowerInvariant());
			} else if (t.StartsWith("$")) {
				if (ctx.TagSets != null && ctx.TagSets.TryGetValue(t.Substring(1), out var set)) outTags.AddRange(set);
			} else outTags.Add(t);
		}
		return outTags;
	}

	// ------------------------------------------------------- DICTIONARY PERSIST
	// All tuner edits live in lexicon.user.json (an overlay) so the curated base lexicon.json
	// stays pristine. Adds/edits append groups; deletes leave tombstones the loader applies last.
	private sealed class UserFileDto {
		public List<UserGroupDto> groups { get; set; } = new();
		public List<UserRemovalDto> remove { get; set; } = new();
	}
	private sealed class UserGroupDto {
		public string pos { get; set; }
		public List<string> tags { get; set; } = new();
		public List<string> words { get; set; } = new();
		public int? eraStart { get; set; }
		public int? eraEnd { get; set; }
	}
	private sealed class UserRemovalDto {
		public string pos { get; set; }
		public List<string> tags { get; set; } = new();
		public string word { get; set; }
	}

	private static readonly System.Text.Json.JsonSerializerOptions UserJsonOpts = new() {
		PropertyNameCaseInsensitive = true,
		WriteIndented = true,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>Add a word to the overlay (era optional) and hot-reload. Un-tombstones it if it
	/// was previously deleted. Words with an era window form their own group.</summary>
	public bool AddWord(string word, string pos, IEnumerable<string> tags, int? eraStart = null, int? eraEnd = null) {
		word = word?.Trim();
		if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(pos)) return false;
		var tagList = NormalizeTags(tags);
		var f = LoadUserFile();
		f.remove.RemoveAll(r => Eq(r.word, word) && Eq(r.pos, pos) && SameTags(r.tags, tagList));
		var g = f.groups.FirstOrDefault(x => Eq(x.pos, pos) && SameTags(x.tags, tagList)
											 && x.eraStart == eraStart && x.eraEnd == eraEnd);
		if (g == null) { g = new UserGroupDto { pos = pos, tags = tagList, eraStart = eraStart, eraEnd = eraEnd }; f.groups.Add(g); }
		if (!g.words.Any(w => Eq(w, word))) g.words.Add(word);
		return SaveAndReload(f);
	}

	/// <summary>Delete a word (from any scope-matching overlay group, plus a tombstone that also
	/// hides a base-lexicon word) and hot-reload.</summary>
	public bool DeleteWord(string word, string pos, IEnumerable<string> tags) {
		word = word?.Trim();
		if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(pos)) return false;
		var tagList = NormalizeTags(tags);
		var f = LoadUserFile();
		foreach (var g in f.groups.Where(x => Eq(x.pos, pos) && SameTags(x.tags, tagList)))
			g.words.RemoveAll(w => Eq(w, word));
		f.groups.RemoveAll(g => g.words.Count == 0);
		if (!f.remove.Any(r => Eq(r.word, word) && Eq(r.pos, pos) && SameTags(r.tags, tagList)))
			f.remove.Add(new UserRemovalDto { pos = pos, tags = tagList, word = word });
		return SaveAndReload(f);
	}

	/// <summary>Rename a word within a group (delete old + add new). Era editing of base words is
	/// not supported via the overlay; pass era to re-attach it to the renamed word.</summary>
	public bool EditWord(string pos, IEnumerable<string> tags, string oldWord, string newWord,
						 int? eraStart = null, int? eraEnd = null) {
		newWord = newWord?.Trim();
		if (string.IsNullOrEmpty(newWord)) return false;
		var tagList = NormalizeTags(tags);
		DeleteWord(oldWord, pos, tagList);
		return AddWord(newWord, pos, tagList, eraStart, eraEnd);
	}

	/// <summary>Remove EVERY entry of a (pos, word) — base + overlay, all tag-sets — in one save.
	/// Used by the tuner's Delete so a word shown under a constraint filter is fully removed (the
	/// old per-exact-tag delete missed base entries whose tag-set differed from the filter).</summary>
	public bool DeleteWordEverywhere(string pos, string word) {
		word = word?.Trim();
		if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(pos) || _engine == null) return false;
		var existing = _engine.EntryTagSetsForWord(pos, word);
		var f = LoadUserFile();
		foreach (var g in f.groups.Where(x => Eq(x.pos, pos))) g.words.RemoveAll(w => Eq(w, word));
		f.groups.RemoveAll(g => g.words.Count == 0);
		foreach (var ts in existing)
			if (!f.remove.Any(r => Eq(r.word, word) && Eq(r.pos, pos) && SameTags(r.tags, ts)))
				f.remove.Add(new UserRemovalDto { pos = pos, tags = ts, word = word });
		if (existing.Count == 0 && !f.remove.Any(r => Eq(r.word, word) && Eq(r.pos, pos)))
			f.remove.Add(new UserRemovalDto { pos = pos, tags = new List<string>(), word = word });
		return SaveAndReload(f);
	}

	/// <summary>Re-classify a word: remove all its existing entries for this pos and add ONE entry
	/// with the new axis tags. Idempotent and duplicate-free (fixes the retag-duplicates bug).</summary>
	public bool RetagWord(string pos, string word, IEnumerable<string> newTags, int? eraStart = null, int? eraEnd = null) {
		word = word?.Trim();
		if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(pos) || _engine == null) return false;
		var newList = NormalizeTags(newTags);
		var existing = _engine.EntryTagSetsForWord(pos, word);
		var f = LoadUserFile();
		foreach (var g in f.groups.Where(x => Eq(x.pos, pos))) g.words.RemoveAll(w => Eq(w, word));
		f.groups.RemoveAll(g => g.words.Count == 0);
		foreach (var ts in existing) {
			if (SameTags(ts, newList)) continue;                       // keep (will re-add) the target set
			if (!f.remove.Any(r => Eq(r.word, word) && Eq(r.pos, pos) && SameTags(r.tags, ts)))
				f.remove.Add(new UserRemovalDto { pos = pos, tags = ts, word = word });
		}
		f.remove.RemoveAll(r => Eq(r.word, word) && Eq(r.pos, pos) && SameTags(r.tags, newList));
		var ng = f.groups.FirstOrDefault(x => Eq(x.pos, pos) && SameTags(x.tags, newList)
											&& x.eraStart == eraStart && x.eraEnd == eraEnd);
		if (ng == null) { ng = new UserGroupDto { pos = pos, tags = newList, eraStart = eraStart, eraEnd = eraEnd }; f.groups.Add(ng); }
		if (!ng.words.Any(w => Eq(w, word))) ng.words.Add(word);
		return SaveAndReload(f);
	}

	private UserFileDto LoadUserFile() {
		string json = ReadFile(UserLexiconPath);
		if (string.IsNullOrWhiteSpace(json)) return new UserFileDto();
		try { return System.Text.Json.JsonSerializer.Deserialize<UserFileDto>(json, UserJsonOpts) ?? new UserFileDto(); }
		catch { return new UserFileDto(); }
	}

	private bool SaveAndReload(UserFileDto f) {
		string json = System.Text.Json.JsonSerializer.Serialize(f, UserJsonOpts);
		using (var fa = FileAccess.Open(UserLexiconPath, FileAccess.ModeFlags.Write)) {
			if (fa == null) return false;
			fa.StoreString(json);
		}
		Reload();
		return true;
	}

	private static List<string> NormalizeTags(IEnumerable<string> tags) =>
		(tags ?? Enumerable.Empty<string>()).Select(t => t?.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
	private static bool Eq(string a, string b) => string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
	private static bool SameTags(List<string> a, IEnumerable<string> b) {
		var sa = new HashSet<string>(a ?? new List<string>(), System.StringComparer.OrdinalIgnoreCase);
		return sa.SetEquals(new HashSet<string>(b ?? Enumerable.Empty<string>(), System.StringComparer.OrdinalIgnoreCase));
	}

	public void Reload() => Load();

	// ============================================================ CONTEXT + MAPS
	private NamingContext MakeContext(Genre genre, int year, ArtistType type, IRandom rng = null) {
		return new NamingContext {
			Genre = genre.ToString(),
			Year = year,
			ArtistType = type.ToString(),
			StyleTags = new List<string> { StyleTagFor(genre) },
			TagSets = new Dictionary<string, List<string>>(),
			Slots = new Dictionary<string, string>(),
			Rng = rng ?? _rng
		};
	}

	private static string StyleTagFor(Genre g) => g switch {
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

	// ---- genre predicates (ported from the old generator) ----
	private static bool IsAfricanAmericanGenre(Genre g) => g switch {
		Genre.Soul or Genre.RnB or Genre.DooWop or Genre.GirlGroup or Genre.Gospel or Genre.Jazz
			or Genre.Motown or Genre.Funk or Genre.Blues => true,
		_ => false
	};
	private static bool IsEastCoastGenre(Genre g) =>
		g == Genre.DooWop || g == Genre.GirlGroup || g == Genre.TeenPop;
	private static bool IsLatinGenre(Genre g) =>
		g == Genre.LatinPop || g == Genre.Boogaloo || g == Genre.TexMex;
	private static bool IsBrillBuildingGenre(Genre g) => g switch {
		Genre.GirlGroup or Genre.TeenPop or Genre.DooWop or Genre.TraditionalPop => true, _ => false
	};
	private static bool IsPsychedelicGenre(Genre g) => g switch {
		Genre.Psychedelic or Genre.GarageRock or Genre.Folk or Genre.AcidRock
			or Genre.PsychedelicRock or Genre.PsychedelicPop => true,
		_ => false
	};
	private static float GetFemaleChance(Genre g) => g switch {
		Genre.GirlGroup => 1.0f, Genre.Soul => 0.35f, Genre.RnB => 0.3f, Genre.Country => 0.25f,
		Genre.TeenPop => 0.3f, Genre.TraditionalPop => 0.35f, Genre.Folk => 0.35f, Genre.Gospel => 0.4f,
		Genre.DooWop => 0.15f, Genre.RockAndRoll => 0.1f, Genre.SurfRock => 0.05f, Genre.GarageRock => 0.05f,
		Genre.Psychedelic => 0.1f, Genre.BritishInvasion => 0.08f, _ => 0.2f
	};
}

// ========================================================================
// SUPPORTING ENUMS
// ========================================================================
public enum VenueType {
	SmallClub, Theater, Arena, Stadium, CoffeHouse, HonkyTonk, JukeJoint, FairGround, RecordingStudio
}
