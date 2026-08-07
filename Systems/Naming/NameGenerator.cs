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
	private const string GrammarPath = "res://Data/Naming/grammar.json";

	private NameEngine _engine;
	private IRandom _rng;             // dedicated naming stream, isolated from GD.Rand

	public override void _EnterTree() { Instance = this; }

	public override void _Ready() { Load(); }

	// ------------------------------------------------------------------ loading
	private void Load() {
		string lexJson = ReadFile(LexiconPath);
		string grammarJson = ReadFile(GrammarPath);
		if (string.IsNullOrEmpty(lexJson) || string.IsNullOrEmpty(grammarJson)) {
			GD.PushError($"NameGenerator: could not load naming data ({LexiconPath} / {GrammarPath}).");
			return;
		}
		var lexicon = Lexicon.LoadFromJson(lexJson);
		string userJson = ReadFile(UserLexiconPath);
		if (!string.IsNullOrEmpty(userJson)) lexicon.AppendJson(userJson);

		var grammar = GrammarEngine.ParseGrammar(grammarJson);
		_engine = new NameEngine(lexicon, grammar);
		_rng = new DeterministicRandom(DeriveSeed());
		GD.Print($"NameGenerator ready: {lexicon.Count} words, {grammar.Count} symbols.");
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
		if (_engine == null) return $"Artist {GD.Randi()}";
		var ctx = MakeContext(genre, year, artistType);
		ctx.RegionId = regionId;
		if (labelStyle.HasValue) ctx.LabelArchetype = labelStyle.Value.ToString();

		bool isBand = DetermineIfBand(genre, artistType);
		string symbol = isBand ? ChooseBandSymbol(genre, year, artistType, ctx)
							   : RollSoloDemographics(genre, ctx);
		return _engine.Generate(symbol, ctx, "artist", nearDup: true);
	}

	public (string firstName, string lastName) GeneratePersonName(bool isMale) {
		if (_engine == null) return (isMale ? "John" : "Jane", "Doe");
		var ctx = MakeContext(Genre.TraditionalPop, 1963, isMale ? ArtistType.SoloMale : ArtistType.SoloFemale);
		string gender = isMale ? "male" : "female";
		string first = _engine.Lexicon.Query("firstName", new[] { gender, "white" }, ctx);
		string last = _engine.Lexicon.Query("lastName", new[] { "generic" }, ctx);
		return (first, last);
	}

	private string RollSoloDemographics(Genre genre, NamingContext ctx) {
		bool isFemale = _rng.Chance(GetFemaleChance(genre));
		bool isBlack = IsAfricanAmericanGenre(genre) && _rng.Chance(0.75);
		bool isItalian = IsEastCoastGenre(genre) && !isBlack && _rng.Chance(0.35);
		bool isCountry = genre == Genre.Country || genre == Genre.CountryRock;
		bool isJewish = IsBrillBuildingGenre(genre) && !isBlack && _rng.Chance(0.25);

		string ethnicity = isBlack ? "black" : isCountry ? "country" : "white";
		ctx.TagSets["name"] = new List<string> { isFemale ? "female" : "male", ethnicity };

		string surname;
		if (isCountry) surname = _rng.Chance(0.55) ? "country" : "generic";
		else if (isItalian) surname = _rng.Chance(0.60) ? "italian" : "generic";
		else if (isJewish) surname = _rng.Chance(0.50) ? "jewish" : "generic";
		else surname = "generic";
		ctx.TagSets["surname"] = new List<string> { surname };
		return "soloName";
	}

	private string ChooseBandSymbol(Genre genre, int year, ArtistType artistType, NamingContext ctx) {
		// band-leader demographic for $name patterns
		bool leaderBlack = IsAfricanAmericanGenre(genre) && _rng.Chance(0.8);
		ctx.TagSets["name"] = new List<string> { "male", leaderBlack ? "black" : "white" };
		ctx.TagSets["surname"] = new List<string> { "generic" };

		if (year >= 1967 && IsPsychedelicGenre(genre)) return "bandName.psych";
		if (genre == Genre.BritishInvasion || genre == Genre.BritishBeat || genre == Genre.BritishPop ||
			(year >= 1964 && year <= 1966 && genre == Genre.RockAndRoll && _rng.Chance(0.35)))
			return "bandName.british";
		if (genre == Genre.SurfRock) return "bandName.surf";
		if (genre == Genre.Soul || genre == Genre.RnB || genre == Genre.Motown || genre == Genre.Funk)
			return "bandName.soul";
		if (genre == Genre.DooWop) return "bandName.doowop";
		if (genre == Genre.GirlGroup) return "bandName.girlGroup";
		if (genre == Genre.GarageRock) return year < 1966 ? "bandName.garageEarly" : "bandName.garage";
		if (genre == Genre.Folk || genre == Genre.FolkRock || genre == Genre.ContemporaryFolk) {
			if (artistType == ArtistType.Duo) return "bandName.folkDuo";
			if (artistType == ArtistType.Trio) return "bandName.folkTrio";
			return "bandName.folk";
		}
		if (genre == Genre.Gospel) return "bandName.gospel";
		return "bandName.default";
	}

	private bool DetermineIfBand(Genre genre, ArtistType artistType) {
		if (artistType == ArtistType.SoloMale || artistType == ArtistType.SoloFemale) return false;
		if (artistType == ArtistType.Band || artistType == ArtistType.Duo ||
			artistType == ArtistType.Trio || artistType == ArtistType.VocalGroup) return true;

		float bandChance = genre switch {
			Genre.RockAndRoll => 0.6f, Genre.GarageRock => 0.92f, Genre.Psychedelic => 0.88f,
			Genre.SurfRock => 0.85f, Genre.BritishInvasion => 0.85f, Genre.Soul => 0.4f,
			Genre.RnB => 0.35f, Genre.DooWop => 0.75f, Genre.GirlGroup => 0.98f, Genre.Folk => 0.25f,
			Genre.Country => 0.12f, Genre.Jazz => 0.35f, Genre.TraditionalPop => 0.08f,
			Genre.TeenPop => 0.25f, Genre.Gospel => 0.7f, _ => 0.5f
		};
		return _rng.Chance(bandChance);
	}

	// =============================================================== SONG TITLES
	public string GenerateSongTitle(Genre genre, int year, string artistName = null) {
		if (_engine == null) return $"Untitled {GD.Randi()}";
		var ctx = MakeContext(genre, year, ArtistType.Unknown);
		string symbol = ChooseSongSymbol(genre, year);
		string bucket = "song|" + (artistName ?? "");
		return _engine.Generate(symbol, ctx, bucket, nearDup: false, attempts: 30);
	}

	private string ChooseSongSymbol(Genre genre, int year) {
		if (year >= 1967 && IsPsychedelicGenre(genre)) return "songTitle.psych";
		if (genre == Genre.SurfRock) return "songTitle.surf";
		if (genre == Genre.Soul || genre == Genre.RnB || genre == Genre.Motown || genre == Genre.Funk) return "songTitle.soul";
		if (genre == Genre.Country || genre == Genre.CountryRock) return "songTitle.country";
		if (genre == Genre.DooWop || genre == Genre.GirlGroup || genre == Genre.TeenPop) return "songTitle.early60s";
		if (genre == Genre.Folk || genre == Genre.FolkRock || genre == Genre.ContemporaryFolk) return "songTitle.folk";
		// default routing mirrors the old year-based blend
		if (year < 1964) return "songTitle.early60s";
		if (year < 1967) return _rng.Chance(0.5) ? "songTitle.early60s" : "songTitle.soul";
		return _rng.Next(3) switch { 0 => "songTitle.psych", 1 => "songTitle.soul", _ => "songTitle.early60s" };
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
	public string GenerateAlbumTitle(Genre genre, int year, string artistName, bool isCompilation = false) {
		if (_engine == null) return artistName;
		var ctx = MakeContext(genre, year, ArtistType.Unknown);
		ctx.Slots["artist"] = artistName;
		if (isCompilation) return _engine.ExpandOnce("compilationTitle", ctx);
		double r = _rng.NextDouble();
		if (r < 0.10) return artistName;                          // self-titled
		if (r < 0.30) return _engine.ExpandOnce("albumFormat", ctx);
		return _engine.ExpandOnce("albumTitle", ctx);
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
		var ctx = MakeContext(genre, 1963, ArtistType.Unknown);
		bool isBlack = bandEthnicity == "black" || (IsAfricanAmericanGenre(genre) && _rng.Chance(0.8));
		bool isItalian = bandEthnicity == "italian" || (IsEastCoastGenre(genre) && !isBlack && _rng.Chance(0.3));
		bool isBritish = genre == Genre.BritishInvasion || bandEthnicity == "british";
		string ethnicity = isBlack ? "black" : "white";
		ctx.TagSets["name"] = new List<string> { isFemale ? "female" : "male", ethnicity };
		string surname = isItalian ? (_rng.Chance(0.5) ? "italian" : "generic")
					   : isBritish ? (_rng.Chance(0.6) ? "british" : "generic") : "generic";
		ctx.TagSets["surname"] = new List<string> { surname };
		return _engine.ExpandOnce("bandMember", ctx);
	}

	// ============================================================ TUNER SUPPORT
	/// <summary>Grammar symbols the NameLab tuner can spin.</summary>
	public string[] AvailableSymbols() => _engine == null ? System.Array.Empty<string>()
		: _engine.AvailableSymbols.ToArray();

	/// <summary>Spin N names for a symbol. If <paramref name="seed"/> is given the batch is
	/// reproducible; otherwise the shared naming stream is used.</summary>
	public string[] Spin(string symbol, Genre genre, int year, ArtistType artistType,
						 LabelArchetype archetype, int count, ulong? seed = null) {
		if (_engine == null) return new[] { "(engine not ready)" };
		IRandom rng = seed.HasValue ? new DeterministicRandom(seed.Value) : _rng;
		var results = new string[count];
		for (int i = 0; i < count; i++) {
			var ctx = MakeContext(genre, year, artistType, rng);
			ctx.LabelArchetype = archetype.ToString();
			ctx.Slots["artist"] = "The Spinners";        // placeholder for artist-based symbols
			ctx.Slots["city"] = _engine.Lexicon.Query("city", new[] { "general" }, ctx);
			// give demographic-driven symbols something to work with
			bool female = artistType == ArtistType.SoloFemale;
			ctx.TagSets["name"] = new List<string> { female ? "female" : "male", "white" };
			ctx.TagSets["surname"] = new List<string> { "generic" };
			results[i] = _engine.ExpandOnce(symbol, ctx);
		}
		return results;
	}

	/// <summary>Append a word to the user overrides file and hot-reload so it takes effect.</summary>
	public bool AddWordAndSave(string word, string pos, IEnumerable<string> tags) {
		word = word?.Trim();
		if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(pos)) return false;

		var groups = new List<Godot.Collections.Dictionary>();
		if (FileAccess.FileExists(UserLexiconPath)) {
			string existing = ReadFile(UserLexiconPath);
			var parsed = Json.ParseString(existing);
			if (parsed.VariantType == Variant.Type.Dictionary) {
				var dict = parsed.AsGodotDictionary();
				if (dict.ContainsKey("groups"))
					foreach (var g in dict["groups"].AsGodotArray())
						groups.Add(g.AsGodotDictionary());
			}
		}
		var tagArr = new Godot.Collections.Array();
		foreach (var t in tags ?? Enumerable.Empty<string>()) tagArr.Add(t);
		var wordArr = new Godot.Collections.Array { word };
		groups.Add(new Godot.Collections.Dictionary {
			{ "pos", pos }, { "tags", tagArr }, { "words", wordArr }
		});

		var outGroups = new Godot.Collections.Array();
		foreach (var g in groups) outGroups.Add(g);
		var root = new Godot.Collections.Dictionary { { "groups", outGroups } };
		using (var f = FileAccess.Open(UserLexiconPath, FileAccess.ModeFlags.Write)) {
			if (f == null) return false;
			f.StoreString(Json.Stringify(root, "\t"));
		}
		Reload();
		return true;
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
			or Genre.Motown or Genre.Funk => true,
		_ => false
	};
	private static bool IsEastCoastGenre(Genre g) =>
		g == Genre.DooWop || g == Genre.GirlGroup || g == Genre.TeenPop;
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
