// Scripts/Systems/Naming/NameGenerator.cs

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class NameGenerator : Node {
	
	public static NameGenerator Instance { get; private set; }
	
	private NameDatabase db = new NameDatabase();
	
	// Markov Chain instances for procedural generation
	private MarkovChain _firstNameChain;
	private MarkovChain _lastNameChain;
	private MarkovChain _bandNameChain;
	
	public override void _EnterTree() {
		Instance = this;
	}
	
	public override void _Ready() {
		if (db != null) {
			db.Initialize();
			
			// Train Markov Chains using your authentic 1960s data
			_firstNameChain = new MarkovChain(2); // Order 2 = looks at 2 previous letters
			_firstNameChain.Train(db.maleNamesWhite);
			_firstNameChain.Train(db.femaleNamesWhite);
			_firstNameChain.Train(db.maleNamesBlack);
			_firstNameChain.Train(db.femaleNamesBlack);
			_firstNameChain.Train(db.maleNamesCountry);
			_firstNameChain.Train(db.femaleNamesCountry);

			_lastNameChain = new MarkovChain(2);
			_lastNameChain.Train(db.lastNamesGeneric);
			_lastNameChain.Train(db.lastNamesItalian);
			_lastNameChain.Train(db.lastNamesCountry);
			_lastNameChain.Train(db.lastNamesBritish);
			_lastNameChain.Train(db.lastNamesJewish);

			_bandNameChain = new MarkovChain(3); // Order 3 for longer group words
			_bandNameChain.Train(db.groupNounsBirds);
			_bandNameChain.Train(db.groupNounsGems);
			_bandNameChain.Train(db.groupNounsMusical);
			_bandNameChain.Train(db.groupNounsBritish);
			_bandNameChain.Train(db.groupNounsPsych);
		}
	}

	// Helper methods to mimic Unity's Random (exclusive max for ints)
	private int RandInt(int min, int maxExclusive) => GD.RandRange(min, maxExclusive - 1);
	private float Randf() => GD.Randf();
	
	// ========================================================================
	// MARKOV CHAIN GENERATION EXAMPLES
	// ========================================================================
	
	public string GenerateMarkovPersonName() {
		if (_firstNameChain == null) return "John Doe";
		string first = _firstNameChain.Generate(3, 8);
		string last = _lastNameChain.Generate(3, 10);
		return $"{first} {last}";
	}

	public string GenerateMarkovBandName() {
		if (_bandNameChain == null) return "The Unknowns";
		string word = _bandNameChain.Generate(4, 10);
		return $"The {word}s";
	}
	
	// ========================================================================
	// ARTIST/BAND NAME GENERATION
	// ========================================================================
	
	public string GenerateArtistName(Genre genre, int year, ArtistType artistType, 
									  string regionId = null, LabelArchetype? labelStyle = null) {
		
		for (int attempts = 0; attempts < 50; attempts++) {
			string name = GenerateArtistNameInternal(genre, year, artistType, regionId, labelStyle);
			if (db.TryRegisterArtistName(name)) {
				return name;
			}
		}
		
		// Fallback: add regional uniquifier
		string baseName = GenerateArtistNameInternal(genre, year, artistType, regionId, labelStyle);
		string city = db.GetRandom(db.citiesGeneral);
		string uniqueName = $"{baseName} ({city})";
		db.TryRegisterArtistName(uniqueName);
		return uniqueName;
	}

	public (string firstName, string lastName) GeneratePersonName(bool isMale) {
		string firstName = isMale 
			? maleFirstNames[RandInt(0, maleFirstNames.Length)]
			: femaleFirstNames[RandInt(0, femaleFirstNames.Length)];
			
		string lastName = lastNames[RandInt(0, lastNames.Length)];
		
		return (firstName, lastName);
	}

	private static readonly string[] maleFirstNames = {
		"James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph",
		"Thomas", "Charles", "Eddie", "Bobby", "Johnny", "Billy", "Tommy", "Jerry",
		"Ray", "Sam", "Carl", "Earl", "Marvin", "Curtis", "Otis", "Jackie", "Smokey"
	};

	private static readonly string[] femaleFirstNames = {
		"Mary", "Patricia", "Linda", "Barbara", "Elizabeth", "Susan", "Dorothy", "Nancy",
		"Diana", "Martha", "Gladys", "Aretha", "Etta", "Tina", "Ronnie", "Darlene",
		"Betty", "Peggy", "Connie", "Brenda", "Dionne", "Patti", "LaVern"
	};

	private static readonly string[] lastNames = {
		"Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Wilson",
		"Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin",
		"Thompson", "Robinson", "Clark", "Lewis", "Lee", "Walker", "Hall", "Young", "King"
	};
	
	private string GenerateArtistNameInternal(Genre genre, int year, ArtistType artistType,
											   string regionId, LabelArchetype? labelStyle) {
		bool isBand = DetermineIfBand(genre, artistType);
		
		if (isBand) {
			return GenerateBandName(genre, year, artistType, labelStyle);
		} else {
			return GenerateSoloName(genre, year, regionId);
		}
	}
	
	public bool IsReady() {
		return db != null;
	}
	
	private bool DetermineIfBand(Genre genre, ArtistType artistType) {
		if (artistType == ArtistType.SoloMale || artistType == ArtistType.SoloFemale) {
			return false;
		}
		if (artistType == ArtistType.Band || artistType == ArtistType.Duo || 
			artistType == ArtistType.Trio || artistType == ArtistType.VocalGroup) {
			return true;
		}
		
		float bandChance = genre switch {
			Genre.RockAndRoll => 0.6f,
			Genre.GarageRock => 0.92f,
			Genre.Psychedelic => 0.88f,
			Genre.SurfRock => 0.85f,
			Genre.BritishInvasion => 0.85f,
			Genre.Soul => 0.4f,
			Genre.RnB => 0.35f,
			Genre.DooWop => 0.75f,
			Genre.GirlGroup => 0.98f,
			Genre.Folk => 0.25f,
			Genre.Country => 0.12f,
			Genre.Jazz => 0.35f,
			Genre.TraditionalPop => 0.08f,
			Genre.TeenPop => 0.25f,
			Genre.Gospel => 0.7f,
			_ => 0.5f
		};
		
		return Randf() < bandChance;
	}
	
	// ========================================================================
	// SOLO ARTIST NAMES
	// ========================================================================
	
	private string GenerateSoloName(Genre genre, int year, string regionId) {
		bool isFemale = Randf() < GetFemaleChance(genre);
		bool isBlack = IsAfricanAmericanGenre(genre) && Randf() < 0.75f;
		bool isItalian = IsEastCoastGenre(genre) && !isBlack && Randf() < 0.35f;
		bool isCountry = IsCountryGenre(genre);
		bool isJewish = IsBrillBuildingGenre(genre) && !isBlack && Randf() < 0.25f;
		
		string firstName = SelectFirstName(isFemale, isBlack, isCountry);
		string lastName = SelectLastName(isBlack, isItalian, isCountry, isJewish);
		
		return ApplySoloNamePattern(firstName, lastName, genre, year, isBlack);
	}
	
	private string SelectFirstName(bool isFemale, bool isBlack, bool isCountry) {
		if (isFemale) {
			if (isBlack) return db.GetRandom(db.femaleNamesBlack);
			if (isCountry) return db.GetRandom(db.femaleNamesCountry);
			return db.GetRandom(db.femaleNamesWhite);
		} else {
			if (isBlack) return db.GetRandom(db.maleNamesBlack);
			if (isCountry) return db.GetRandom(db.maleNamesCountry);
			return db.GetRandom(db.maleNamesWhite);
		}
	}
	
	private string SelectLastName(bool isBlack, bool isItalian, bool isCountry, bool isJewish) {
		if (isCountry) return db.GetWeighted(db.lastNamesCountry, db.lastNamesGeneric, 0.55f);
		if (isItalian) return db.GetWeighted(db.lastNamesItalian, db.lastNamesGeneric, 0.6f);
		if (isJewish) return db.GetWeighted(db.lastNamesJewish, db.lastNamesGeneric, 0.5f);
		return db.GetRandom(db.lastNamesGeneric);
	}
	
	private string ApplySoloNamePattern(string first, string last, Genre genre, int year, bool isBlack) {
		int roll = RandInt(0, 100);
		
		if (roll < 55) return $"{first} {last}";
		
		if (roll < 65 && (IsAfricanAmericanGenre(genre) || genre == Genre.RockAndRoll)) {
			string prefix = db.GetRandom(db.nicknamesPrefixes);
			if (Randf() < 0.5f) return $"{prefix} {first}";
			else return $"{prefix} {first} {last}";
		}
		
		if (roll < 70 && isBlack) {
			string title = db.GetRandom(db.nicknamesTitles);
			return $"{title} {first}";
		}
		
		if (roll < 75 && (genre == Genre.TeenPop || year >= 1966)) {
			return first;
		}
		
		if (roll < 82 && (genre == Genre.RnB || genre == Genre.Country || genre == Genre.RockAndRoll)) {
			string nickname = db.GetRandom(db.nicknamesQuoted);
			return $"{first} \"{nickname}\" {last}";
		}
		
		if (roll < 87 && isBlack && Randf() < 0.3f) {
			char initial1 = first[0];
			char initial2 = "ABCDEFGHJKLMNPRSTVW"[RandInt(0, 19)];
			return $"{initial1}.{initial2}. {last}";
		}
		
		if (roll < 92 && Randf() < 0.15f) {
			string suffix = Randf() < 0.8f ? "Jr." : "III";
			return $"{first} {last} {suffix}";
		}
		
		if (roll < 97 && genre == Genre.Country) {
			string secondFirst = SelectFirstName(first.EndsWith("y") || first.EndsWith("ie"), false, true);
			return $"{first} {secondFirst} {last}";
		}
		
		return $"{first} {last}";
	}
	
	// ========================================================================
	// BAND NAMES
	// ========================================================================
	
	private string GenerateBandName(Genre genre, int year, ArtistType artistType, LabelArchetype? labelStyle) {
		if (year >= 1967 && IsPsychedelicGenre(genre)) return GeneratePsychBandName(year);
		if (genre == Genre.BritishInvasion || (year >= 1964 && year <= 1966 && genre == Genre.RockAndRoll && Randf() < 0.35f)) return GenerateBritishBandName();
		if (genre == Genre.SurfRock) return GenerateSurfBandName();
		if (genre == Genre.Soul || genre == Genre.RnB) return GenerateSoulGroupName(artistType);
		if (genre == Genre.DooWop) return GenerateDooWopGroupName();
		if (genre == Genre.GirlGroup) return GenerateGirlGroupName();
		if (genre == Genre.GarageRock) return GenerateGarageBandName(year);
		if (genre == Genre.Folk) return GenerateFolkGroupName(artistType);
		if (genre == Genre.Gospel) return GenerateGospelGroupName();
		return GenerateDefaultBandName(year);
	}
	
	private string GeneratePsychBandName(int year) {
		int pattern = RandInt(0, 14);
		return pattern switch {
			0 => $"The {db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)}",
			1 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.groupNounsPsych)}",
			2 => $"{db.GetRandom(db.lastNamesGeneric)} {db.GetRandom(db.groupNounsPsych)}",
			3 => db.GetRandom(db.nounsPsych),
			4 => $"The {db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)} {db.GetRandom(db.groupNounsPsych)}",
			5 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.groupNounsAnimals).TrimEnd('s')}",
			6 => $"{db.GetRandom(db.maleNamesBlack)} and the {db.GetRandom(db.adjectivesSoul)} {db.GetRandom(db.nounsSoul)}",
			7 => $"The {db.GetRandom(db.nounsPsych)}s",
			8 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsEarly60s)}",
			9 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.groupNounsPsych)}",
			10 => $"The {GetOrdinal(RandInt(3, 14))} {db.GetRandom(db.nounsPsych)}",
			11 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.citiesGeneral)} {db.GetRandom(db.groupNounsPsych)}",
			12 => db.GetRandom(db.groupNounsPsych),
			13 => $"The {db.GetRandom(db.maleNamesWhite)} {db.GetRandom(db.lastNamesGeneric)} {db.GetRandom(db.groupNounsPsych)}",
			_ => $"The {db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)}"
		};
	}
	
	private string GenerateBritishBandName() {
		int pattern = RandInt(0, 10);
		return pattern switch {
			0 => $"The {db.GetRandom(db.groupNounsBritish)}",
			1 => $"The {db.GetRandom(db.groupNounsAnimals)}",
			2 => $"{db.GetRandom(db.maleNamesWhite)} and the {db.GetRandom(db.groupNounsBritish)}",
			3 => $"{db.GetRandom(db.maleNamesWhite)} {db.GetRandom(db.lastNamesBritish)} and the {db.GetRandom(db.groupNounsBritish)}",
			4 => $"{db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.groupNounsBritish)}",
			5 => db.GetRandom(db.groupNounsBritish),
			6 => $"The {db.GetRandom(db.citiesBritish)} {db.GetRandom(db.groupNounsBritish)}",
			7 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.groupNounsAnimals)}",
			8 => $"{db.GetRandom(db.lastNamesBritish)}'s {db.GetRandom(db.groupNounsBritish)}",
			9 => $"The {GetWrittenNumber(RandInt(3, 8))} {db.GetRandom(db.groupNounsBritish)}",
			_ => $"The {db.GetRandom(db.groupNounsBritish)}"
		};
	}
	
	private string GenerateSurfBandName() {
		int pattern = RandInt(0, 8);
		return pattern switch {
			0 => $"The {db.GetRandom(db.groupNounsSurf)}",
			1 => $"The {db.GetRandom(db.adjectivesSurf)} {db.GetRandom(db.nounsSurf)}s",
			2 => $"{db.GetRandom(db.maleNamesWhite)} {db.GetRandom(db.lastNamesGeneric)} and the {db.GetRandom(db.groupNounsMusical)}",
			3 => $"The {db.GetRandom(db.citiesSurf)} {db.GetRandom(db.groupNounsSurf)}",
			4 => $"{db.GetRandom(db.maleNamesWhite)} and {db.GetRandom(db.maleNamesWhite)}",
			5 => $"The {db.GetRandom(db.nounsSurf)}s",
			6 => $"The {db.GetRandom(db.adjectivesSurf)} {db.GetRandom(db.groupNounsSurf)}",
			7 => $"The {db.GetRandom(db.citiesSurf)} {db.GetRandom(db.groupNounsSurf)}",
			_ => $"The {db.GetRandom(db.groupNounsSurf)}"
		};
	}
	
	private string GenerateSoulGroupName(ArtistType artistType) {
		int pattern = RandInt(0, 10);
		if (artistType == ArtistType.VocalGroup) {
			return pattern switch {
				0 => $"The {db.GetRandom(db.groupNounsMusical)}",
				1 => $"The {db.GetRandom(db.adjectivesSoul)} {db.GetRandom(db.groupNounsMusical)}",
				2 => $"The {db.GetRandom(db.groupNounsGems)}",
				3 => $"The {db.GetRandom(db.groupNounsRoyalty)}",
				4 => $"The {GetWrittenNumber(RandInt(3, 6))} {db.GetRandom(db.groupNounsRoyalty)}",
				5 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.groupNounsRoyalty)}",
				_ => $"The {db.GetRandom(db.groupNounsMusical)}"
			};
		}
		return pattern switch {
			0 => $"{db.GetRandom(db.femaleNamesBlack)} {db.GetRandom(db.lastNamesGeneric)} and the {db.GetRandom(db.groupNounsMusical)}",
			1 => $"{db.GetRandom(db.maleNamesBlack)} {db.GetRandom(db.lastNamesGeneric)} and the {db.GetRandom(db.groupNounsMusical)}",
			2 => $"The {db.GetRandom(db.groupNounsGems)}",
			3 => $"The {db.GetRandom(db.groupNounsMusical)}",
			4 => $"{db.GetRandom(db.maleNamesBlack)} and the {db.GetRandom(db.adjectivesSoul)} {db.GetRandom(db.groupNounsMusical)}",
			5 => Randf() < 0.5f ? $"{db.GetRandom(db.maleNamesBlack)} {db.GetRandom(db.lastNamesGeneric)} and His {db.GetRandom(db.groupNounsMusical)}" : $"{db.GetRandom(db.femaleNamesBlack)} {db.GetRandom(db.lastNamesGeneric)} and Her {db.GetRandom(db.groupNounsMusical)}",
			6 => $"The {GetWrittenNumber(RandInt(3, 6))} {db.GetRandom(db.groupNounsMusical)}",
			7 => $"The {db.GetRandom(db.citiesGeneral)} {db.GetRandom(db.groupNounsMusical)}",
			8 => $"The {db.GetRandom(db.groupNounsMusical)}",
			9 => $"The {db.GetRandom(db.adjectivesUniversal)}ettes",
			_ => $"The {db.GetRandom(db.groupNounsMusical)}"
		};
	}
	
	private string GenerateDooWopGroupName() {
		int pattern = RandInt(0, 8);
		return pattern switch {
			0 => $"The {db.GetRandom(db.groupNounsBirds)}",
			1 => $"The {db.GetRandom(db.groupNounsMusical)}",
			2 => $"The {db.GetRandom(db.groupNounsGems)}",
			3 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.groupNounsBirds)}",
			4 => $"The {GetWrittenNumber(RandInt(3, 6))} {db.GetRandom(db.groupNounsRoyalty)}",
			5 => $"The {db.GetRandom(new[]{"Blue", "Golden", "Silver", "Scarlet", "Midnight"})} {db.GetRandom(db.groupNounsBirds)}",
			6 => $"The {db.GetRandom(db.groupNounsRoyalty)}",
			7 => $"The {db.GetRandom(db.lastNamesGeneric)} Brothers",
			_ => $"The {db.GetRandom(db.groupNounsBirds)}"
		};
	}
	
	private string GenerateGirlGroupName() {
		int pattern = RandInt(0, 10);
		return pattern switch {
			0 => $"The {db.GetRandom(db.femaleNamesWhite)}s",
			1 => $"The {db.GetRandom(db.femaleNamesBlack)}s",
			2 => $"The {db.GetRandom(db.groupNounsGems)}",
			3 => $"{db.GetRandom(db.femaleNamesBlack)} and the {db.GetRandom(db.groupNounsMusical)}",
			4 => $"The {db.GetRandom(db.adjectivesUniversal)}ettes",
			5 => $"The {db.GetRandom(db.nounsUniversal)}ettes",
			6 => $"The {db.GetRandom(db.adjectivesEarly60s)} {db.GetRandom(db.groupNounsGems)}",
			7 => $"The {GetWrittenNumber(RandInt(2, 5))} {db.GetRandom(db.groupNounsGems)}",
			8 => $"The {db.GetRandom(new[]{"Golden", "Silver", "Blue", "Scarlet", "Velvet"})} Girls",
			9 => $"The {db.GetRandom(db.citiesGeneral)} Girls",
			_ => $"The {db.GetRandom(db.groupNounsGems)}"
		};
	}
	
	private string GenerateGarageBandName(int year) {
		int pattern = RandInt(0, 12);
		if (year < 1966) {
			return pattern switch {
				0 => $"The {db.GetRandom(db.groupNounsAnimals)}",
				1 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.groupNounsAnimals)}",
				2 => $"{db.GetRandom(db.maleNamesWhite)} and the {db.GetRandom(db.groupNounsBritish)}",
				3 => $"The {db.GetRandom(db.groupNounsBritish)}",
				4 => $"The {db.GetRandom(db.citiesGeneral)} {db.GetRandom(db.groupNounsBritish)}",
				5 => $"The {GetWrittenNumber(RandInt(4, 7))} {db.GetRandom(db.groupNounsBritish)}",
				_ => $"The {db.GetRandom(db.groupNounsBritish)}"
			};
		}
		return pattern switch {
			0 => $"The {db.GetRandom(new[]{"Dark", "Black", "Shadow", "Night", "Grim", "Wild", "Mad", "Savage"})} {db.GetRandom(db.groupNounsBritish)}",
			1 => $"The {db.GetRandom(new[]{"Spiders", "Rats", "Snakes", "Scorpions", "Vampires", "Zombies", "Ghouls"})}",
			2 => $"The {db.GetRandom(new[]{"Outcasts", "Rejects", "Loners", "Strangers", "Misfits", "Freaks", "Losers"})}",
			3 => $"The {db.GetRandom(new[]{"Count", "Lords", "Knights", "Sons", "Children", "Slaves"})} of {db.GetRandom(db.nounsPsych)}",
			4 => $"{db.GetRandom(new[]{"Electric", "Chocolate", "Velvet", "Iron", "Plastic", "Chrome"})} {db.GetRandom(db.groupNounsAnimals).TrimEnd('s')}",
			5 => $"The {db.GetRandom(db.citiesGeneral)} {db.GetRandom(db.groupNounsBritish)}",
			6 => $"{db.GetRandom(db.maleNamesWhite)} and the {db.GetRandom(db.groupNounsBritish)}",
			7 => $"The {db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.groupNounsBritish)}",
			8 => $"The {db.GetRandom(db.groupNounsBritish)}",
			9 => $"The {db.GetRandom(db.lastNamesGeneric)} {db.GetRandom(db.groupNounsBritish)}",
			10 => $"The {db.GetRandom(new[]{"Leather", "Velvet", "Vinyl", "Nylon", "Suede", "Denim"})} {db.GetRandom(db.groupNounsBritish)}",
			11 => $"{db.GetRandom(new[]{"Question Mark", "Zero", "Nobody", "X", "Johnny Blade", "Roky"})} and the {db.GetRandom(db.groupNounsBritish)}",
			_ => $"The {db.GetRandom(db.groupNounsBritish)}"
		};
	}
	
	private string GenerateFolkGroupName(ArtistType artistType) {
		int pattern = RandInt(0, 10);
		if (artistType == ArtistType.Duo) {
			return pattern switch {
				0 => $"{db.GetRandom(db.maleNamesWhite)} and {db.GetRandom(db.maleNamesWhite)}",
				1 => $"{db.GetRandom(db.femaleNamesWhite)} and {db.GetRandom(db.maleNamesWhite)}",
				2 => $"{db.GetRandom(db.lastNamesGeneric)} and {db.GetRandom(db.lastNamesGeneric)}",
				3 => $"The {db.GetRandom(db.lastNamesGeneric)} {db.GetRandom(new[]{"Brothers", "Sisters", "Twins", "Duo"})}",
				_ => $"{db.GetRandom(db.maleNamesWhite)} and {db.GetRandom(db.femaleNamesWhite)}"
			};
		}
		if (artistType == ArtistType.Trio) {
			return pattern switch {
				0 => $"{db.GetRandom(db.maleNamesWhite)}, {db.GetRandom(db.maleNamesWhite)} and {db.GetRandom(db.femaleNamesWhite)}",
				1 => $"The {db.GetRandom(db.lastNamesGeneric)} Trio",
				2 => $"The {db.GetRandom(db.adjectivesUniversal)} Trio",
				3 => $"The {db.GetRandom(db.nounsUniversal)} Three",
				_ => $"The {db.GetRandom(db.lastNamesGeneric)} Trio"
			};
		}
		return pattern switch {
			0 => $"The {db.GetRandom(db.citiesGeneral)} {db.GetRandom(new[]{"Singers", "Ramblers", "Travelers", "Wanderers", "Minstrels"})}",
			1 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(new[]{"Balladeers", "Troubadours", "Folksingers", "Minstrels", "Wayfinders"})}",
			2 => $"The {GetWrittenNumber(RandInt(3, 6))} {db.GetRandom(new[]{"Singers", "Voices", "Harmonizers", "Crooners"})}",
			3 => $"The {db.GetRandom(db.lastNamesGeneric)} Singers",
			4 => $"The {db.GetRandom(new[]{"Mountain", "Valley", "River", "Forest", "Prairie", "Highland"})} {db.GetRandom(new[]{"Boys", "Girls", "Folk", "Singers", "Voices"})}",
			5 => $"The {db.GetRandom(db.lastNamesGeneric)} {db.GetRandom(new[]{"Brothers", "Sisters", "Family"})}",
			6 => $"The New {db.GetRandom(new[]{"Christy", "Lost City", "Cumberland", "Limelight", "Greenbriar"})} {db.GetRandom(new[]{"Minstrels", "Ramblers", "Singers", "Boys"})}",
			7 => $"The {db.GetRandom(db.statesAndRegions)} {db.GetRandom(new[]{"Ramblers", "Travelers", "Boys", "Singers"})}",
			8 => $"The {db.GetRandom(new[]{"Weavers", "Seekers", "Pilgrims", "Wayfarers", "Journeymen", "Highwaymen"})}",
			9 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(new[]{"Brothers", "Sisters", "Family", "Clan"})}",
			_ => $"The {db.GetRandom(db.lastNamesGeneric)} Singers"
		};
	}
	
	private string GenerateGospelGroupName() {
		int pattern = RandInt(0, 12);
		return pattern switch {
			0 => $"The {db.GetRandom(new[]{"Mighty", "Glorious", "Heavenly", "Divine", "Sacred", "Blessed"})} Gospel {db.GetRandom(new[]{"Singers", "Choir", "Voices", "Harmonizers"})}",
			1 => $"The {GetWrittenNumber(RandInt(3, 6))} {db.GetRandom(new[]{"Angels", "Saints", "Disciples", "Apostles", "Prophets", "Witnesses"})}",
			2 => $"{db.GetRandom(db.maleNamesBlack)} {db.GetRandom(db.lastNamesGeneric)} and the {db.GetRandom(new[]{"Gospel Stars", "Spirituals", "Voices of Praise", "Heavenly Choir"})}",
			3 => $"The {db.GetRandom(db.citiesGeneral)} Gospel {db.GetRandom(new[]{"Singers", "Choir", "Jubilee Singers"})}",
			4 => $"The {db.GetRandom(new[]{"Soul", "Spirit", "Faith", "Grace", "Glory", "Praise", "Jubilee"})} {db.GetRandom(new[]{"Stirrers", "Travelers", "Harmonizers", "Singers"})}",
			5 => $"The {db.GetRandom(db.lastNamesGeneric)} {db.GetRandom(new[]{"Family", "Brothers", "Sisters", "Singers"})}",
			6 => $"The {db.GetRandom(new[]{"Golden", "Silver", "Heavenly", "Angelic", "Divine"})} {db.GetRandom(new[]{"Gate", "Star", "Light", "Voice", "Crown"})} {db.GetRandom(new[]{"Quartet", "Singers", "Jubileers"})}",
			7 => $"{db.GetRandom(new[]{"Reverend", "Brother", "Sister", "Elder", "Deacon"})} {db.GetRandom(db.lastNamesGeneric)}'s {db.GetRandom(new[]{"Gospel Train", "Spiritual Caravan", "Heavenly Choir"})}",
			8 => $"The {db.GetRandom(db.citiesGeneral)} Community Choir",
			9 => $"The {db.GetRandom(new[]{"Sensational", "Original", "Famous", "Mighty"})} {GetWrittenNumber(RandInt(3, 6))}",
			10 => $"The {db.GetRandom(new[]{"Singing", "Praising", "Rejoicing", "Testifying"})} {db.GetRandom(new[]{"Disciples", "Saints", "Witnesses", "Pilgrims"})}",
			11 => $"The {db.GetRandom(new[]{"Caravans", "Pilgrim Travelers", "Highway QCs", "Violinaires", "Swanee Quintet", "Swan Silvertones", "Dixie Hummingbirds"})}",
			_ => $"The {db.GetRandom(db.lastNamesGeneric)} Gospel Singers"
		};
	}
	
	private string GenerateDefaultBandName(int year) {
		int pattern = RandInt(0, 10);
		return pattern switch {
			0 => $"The {db.GetRandom(db.groupNounsMusical)}",
			1 => $"The {db.GetRandom(db.groupNounsBirds)}",
			2 => $"{db.GetRandom(db.maleNamesWhite)} and the {db.GetRandom(db.groupNounsMusical)}",
			3 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.groupNounsMusical)}",
			4 => $"The {db.GetRandom(db.groupNounsAnimals)}",
			5 => $"The {GetWrittenNumber(RandInt(3, 6))} {db.GetRandom(db.groupNounsRoyalty)}",
			6 => $"The {db.GetRandom(db.citiesGeneral)} {db.GetRandom(db.groupNounsMusical)}",
			7 => $"{db.GetRandom(db.maleNamesWhite)}'s {db.GetRandom(db.groupNounsMusical)}",
			8 => $"The {db.GetRandom(db.groupNounsGems)}",
			9 => $"The {db.GetRandom(db.lastNamesGeneric)} Brothers",
			_ => $"The {db.GetRandom(db.groupNounsMusical)}"
		};
	}
	
	// ========================================================================
	// SONG TITLE GENERATION
	// ========================================================================
	
	public string GenerateSongTitle(Genre genre, int year, string artistName = null) {
		for (int attempts = 0; attempts < 30; attempts++) {
			string title = GenerateSongTitleInternal(genre, year);
			if (db.TryRegisterSongTitle(title, artistName ?? "")) {
				return title;
			}
		}
		string baseTitle = GenerateSongTitleInternal(genre, year);
		return $"{baseTitle} '{ year % 100:D2}";
	}
	
	private string GenerateSongTitleInternal(Genre genre, int year) {
		if (year >= 1967 && IsPsychedelicGenre(genre)) return GeneratePsychSongTitle();
		if (genre == Genre.SurfRock) return GenerateSurfSongTitle();
		if (genre == Genre.Soul || genre == Genre.RnB) return GenerateSoulSongTitle();
		if (genre == Genre.Country) return GenerateCountrySongTitle();
		if (genre == Genre.DooWop || genre == Genre.GirlGroup || genre == Genre.TeenPop) return GenerateEarly60sSongTitle();
		if (genre == Genre.Folk) return GenerateFolkSongTitle();
		return GenerateDefaultSongTitle(year);
	}
	
	private string GenerateEarly60sSongTitle() {
		int pattern = RandInt(0, 20);
		return pattern switch {
			0 => $"{db.GetRandom(db.phrasesEarly60sOpeners)} {db.GetRandom(db.verbsUniversal)} {db.GetRandom(new[]{"Me", "You", "Her", "Him", "Us"})}",
			1 => Randf() < 0.5f ? db.GetRandom(db.maleNamesWhite) : db.GetRandom(db.femaleNamesWhite),
			2 => $"{db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.nounsUniversal)}",
			3 => $"The {db.GetRandom(db.nounsEarly60s)}",
			4 => $"{db.GetRandom(db.verbsAction)}in'",
			5 => $"My {db.GetRandom(db.adjectivesEarly60s)} {db.GetRandom(db.nounsEarly60s)}",
			6 => $"{db.GetRandom(db.exclamationsEarly60s)} {db.GetRandom(db.femaleNamesWhite)}!",
			7 => $"{db.GetRandom(db.phrasesEarly60sOpeners)} {db.GetRandom(db.nounsEarly60s)}",
			8 => $"{db.GetRandom(new[]{"Last", "This", "One", "That", "Every", "Lonely"})} {db.GetRandom(new[]{"Night", "Day", "Time", "Summer", "Christmas", "Kiss"})}",
			9 => db.GetRandom(new[]{"Sha La La", "Da Doo Ron Ron", "Doo Wah Diddy", "Shoop Shoop", "Rama Lama Ding Dong", "Be Bop A Lula", "Tutti Frutti"}),
			10 => $"{db.GetRandom(db.nounsEarly60s)} Of {db.GetRandom(db.nounsUniversal)}",
			11 => $"{db.GetRandom(db.verbsAction)}in' In The {db.GetRandom(new[]{"Street", "Rain", "Moonlight", "Dark", "Sun"})}",
			12 => $"The {db.GetRandom(db.nounsUniversal)} {db.GetRandom(db.verbsUniversal)}s",
			13 => $"{db.GetRandom(new[]{"A Thousand", "A Hundred", "A Million", "Ten Thousand"})} {db.GetRandom(db.nounsUniversal)}s",
			14 => $"Will You {db.GetRandom(db.verbsUniversal)} Me {db.GetRandom(new[]{"Tomorrow", "Tonight", "Forever", "Again", "Always"})}",
			15 => $"{db.GetRandom(new[]{"His", "Her", "Your", "My"})} {db.GetRandom(db.nounsEarly60s)}",
			16 => $"At The {db.GetRandom(new[]{"Hop", "Dance", "Party", "Prom", "Beach", "Corner"})}",
			17 => $"{db.GetRandom(db.adjectivesEarly60s)} To {db.GetRandom(new[]{"You", "Love", "Her", "Him"})}",
			18 => $"{db.GetRandom(new[]{"Please", "Oh Please", "Baby Please", "I Beg You"})} {db.GetRandom(new[]{"Stay", "Don't Go", "Come Back", "Love Me", "Hold Me"})}",
			19 => $"{db.GetRandom(new[]{"Letter", "Message", "Note", "Memo", "Word"})} To My {db.GetRandom(new[]{"Baby", "Darling", "Love", "Heart", "Angel"})}",
			_ => $"{db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.nounsUniversal)}"
		};
	}
	
	private string GenerateSurfSongTitle() {
		int pattern = RandInt(0, 15);
		return pattern switch {
			0 => $"{db.GetRandom(new[]{"Surfin'", "Ridin'", "Cruisin'", "Drivin'", "Racin'"})} {db.GetRandom(db.citiesSurf)}",
			1 => db.GetRandom(db.nounsSurf),
			2 => $"The {db.GetRandom(db.adjectivesSurf)} {db.GetRandom(db.nounsSurf)}",
			3 => db.GetRandom(new[]{"409", "Shut Down", "Little Deuce Coupe", "Drag City", "Hot Rod", "T-Bird", "GTO", "Stingray"}),
			4 => $"{db.GetRandom(new[]{"Wipe", "Walk", "Ride", "Catch", "Hit", "Shoot"})} {db.GetRandom(new[]{"Out", "Away", "On", "The Curl", "The Pier", "The Wave"})}",
			5 => $"{db.GetRandom(db.citiesSurf)} {db.GetRandom(new[]{"Girls", "Boys", "Sun", "Dreams", "Summer"})}",
			6 => $"{db.GetRandom(new[]{"Summer", "Beach", "Sun", "Surf", "Wave"})} {db.GetRandom(new[]{"Time", "Party", "Fun", "Love", "Days"})}",
			7 => $"{db.GetRandom(db.adjectivesSurf)} {db.GetRandom(db.nounsSurf)}",
			8 => db.GetRandom(db.citiesSurf),
			9 => $"Surfin' {db.GetRandom(new[]{"USA", "Safari", "Bird", "Sunset", "Summer"})}",
			10 => $"{db.GetRandom(new[]{"Beach", "Surf", "Summer", "Pool", "Luau"})} {db.GetRandom(new[]{"Party", "Stomp", "Blast", "Bash", "Jam"})}",
			11 => $"Let's {db.GetRandom(new[]{"Go Trippin'", "Surf", "Dance", "Party", "Cruise", "Race"})}",
			12 => $"{db.GetRandom(new[]{"Drag", "Shut", "Rev", "Burn", "Speed"})} {db.GetRandom(new[]{"City", "Down", "Up", "Out", "Away"})}",
			13 => db.GetRandom(db.instrumentalNouns),
			14 => $"{db.GetRandom(db.instrumentalAdjectives)} {db.GetRandom(db.instrumentalNouns)}",
			_ => $"{db.GetRandom(db.adjectivesSurf)} {db.GetRandom(db.nounsSurf)}"
		};
	}
	
	private string GenerateSoulSongTitle() {
		int pattern = RandInt(0, 20);
		return pattern switch {
			0 => $"{db.GetRandom(db.phrasesSoulOpeners)} {db.GetRandom(db.verbsUniversal)} {db.GetRandom(new[]{"Your Love", "Your Lovin'", "You", "Me", "It"})}",
			1 => $"{db.GetRandom(db.nounsSoul)} {db.GetRandom(new[]{"Man", "Woman", "Lady", "Brother", "Sister", "Child"})}",
			2 => $"{db.GetRandom(db.verbsUniversal)}in' In The {db.GetRandom(new[]{"Shadows", "Midnight", "Street", "Rain"})} Of {db.GetRandom(db.nounsSoul)}",
			3 => db.GetRandom(db.nounsSoul),
			4 => $"What {db.GetRandom(new[]{"Becomes Of", "Happened To", "About", "Good Is"})} {db.GetRandom(new[]{"The Broken Hearted", "Our Love", "My Heart", "True Love"})}",
			5 => $"{db.GetRandom(db.exclamationsLate60s)}",
			6 => $"{db.GetRandom(new[]{"Hold", "Keep", "Walk", "Move", "Get"})} On{db.GetRandom(new[]{", I'm Comin'", "!", " To Love", " Brother", " Tight"})}",
			7 => $"{db.GetRandom(db.adjectivesSoul)} {db.GetRandom(db.nounsSoul)} {db.GetRandom(new[]{"Music", "Love", "Life", "Man", "Woman"})}",
			8 => $"I {db.GetRandom(db.verbsUniversal)} {db.GetRandom(new[]{"It Through The Grapevine", "You", "Your Love", "The Music", "A Change"})}",
			9 => $"Ain't {db.GetRandom(new[]{"No", "Too", "Nothin'", "Nobody"})} {db.GetRandom(db.nounsSoul)} {db.GetRandom(new[]{"High Enough", "Good Enough", "Strong Enough", "Gonna Stop Me"})}",
			10 => db.GetRandom(new[]{"Think", "Respect", "Try", "Wait", "Stop", "Dance", "Move", "Groove"}),
			11 => $"My {db.GetRandom(new[]{"Girl", "Guy", "Man", "Woman", "Baby", "Love", "Everything"})}",
			12 => $"{db.GetRandom(db.verbsAction)}in' {db.GetRandom(new[]{"In The Street", "On The Ceiling", "To The Music", "All Night Long"})}",
			13 => $"{RandInt(100, 999)}-{RandInt(1000, 9999)}",
			14 => $"When A {db.GetRandom(new[]{"Man", "Woman", "Heart", "Soul"})} {db.GetRandom(db.verbsUniversal)}s {db.GetRandom(new[]{"A Woman", "A Man", "True Love", "Too Much"})}",
			15 => $"Chain Of {db.GetRandom(new[]{"Fools", "Love", "Hearts", "Dreams", "Pain"})}",
			16 => $"{db.GetRandom(new[]{"Please", "Baby", "Darling"})}, {db.GetRandom(new[]{"Please", "Baby", "Darling"})}, {db.GetRandom(new[]{"Please", "Please", "Baby"})}",
			17 => $"I'm Gonna {db.GetRandom(new[]{"Make You Love Me", "Get You", "Find A Way", "Be There", "Hold On"})}",
			18 => $"({db.GetRandom(db.verbsAction)}in' On) The {db.GetRandom(db.nounsSoul)} Of {db.GetRandom(new[]{"The Bay", "Love", "Life", "My Heart"})}",
			19 => $"Just My {db.GetRandom(new[]{"Imagination", "Luck", "Heart", "Soul", "Dream"})} ({db.GetRandom(new[]{"Running Away", "Slipping Away", "Fading Fast"})})",
			_ => $"{db.GetRandom(db.adjectivesSoul)} {db.GetRandom(db.nounsSoul)}"
		};
	}
	
	private string GenerateCountrySongTitle() {
		int pattern = RandInt(0, 20);
		return pattern switch {
			0 => $"{db.GetRandom(db.phrasesCountryOpeners)} {db.GetRandom(db.nounsCountry)}",
			1 => $"{db.GetRandom(db.adjectivesCountry)} {db.GetRandom(db.nounsCountry)}",
			2 => $"{db.GetRandom(db.citiesCountry)} {db.GetRandom(new[]{"Blues", "Moon", "Woman", "Man", "Train", "Memories"})}",
			3 => $"{db.GetRandom(db.verbsUniversal)}in' {db.GetRandom(new[]{"After Midnight", "The Floor", "The Line", "Away", "Alone"})}",
			4 => $"Your {db.GetRandom(db.adjectivesCountry)} {db.GetRandom(new[]{"Heart", "Arms", "Eyes", "Love", "Ways"})}",
			5 => $"{db.GetRandom(db.citiesCountry)} {db.GetRandom(new[]{"Prison Blues", "Bound", "Highway", "Nights", "Moon"})}",
			6 => $"{db.GetRandom(new[]{"There Stands The", "Pour Me Another", "Set 'Em Up", "One More"})} {db.GetRandom(db.nounsCountry)}",
			7 => $"{db.GetRandom(new[]{"Mama", "Daddy", "Papa", "Grandma", "Brother"})} {db.GetRandom(new[]{"Tried", "Said", "Told Me", "Knows Best", "Don't Know"})}",
			8 => $"{db.GetRandom(new[]{"He", "She", "I", "You"})} {db.GetRandom(new[]{"Stopped Loving", "Started Crying", "Quit", "Never Stopped"})} {db.GetRandom(new[]{"Her", "Him", "Me", "You"})} {db.GetRandom(new[]{"Today", "Yesterday", "Last Night", "Again"})}",
			9 => $"{db.GetRandom(db.nounsCountry)} On The {db.GetRandom(new[]{"Wall", "Floor", "Table", "Road", "Hill"})}",
			10 => $"{db.GetRandom(new[]{"Act", "Love", "Sing", "Cry", "Stand"})} {db.GetRandom(new[]{"Naturally", "Alone", "Forever", "Again", "True"})}",
			11 => $"Don't {db.GetRandom(new[]{"Come Home", "Come Back", "Leave Me", "Walk Away"})} {db.GetRandom(new[]{"A-Drinkin'", "A-Cryin'", "Again", "Alone"})}",
			12 => $"{db.GetRandom(new[]{"Sixteen", "Forty", "Twenty", "Hundred"})} {db.GetRandom(new[]{"Tons", "Miles", "Years", "Tears", "Days"})}",
			13 => $"{db.GetRandom(new[]{"King", "Queen", "Prince", "Lord"})} Of The {db.GetRandom(db.nounsCountry)}",
			14 => $"{db.GetRandom(new[]{"Hurtin'", "Cryin'", "Leavin'", "Lovin'", "Prayin'", "Wishin'"})}",
			15 => $"Behind {db.GetRandom(new[]{"Closed Doors", "The Wheel", "Bars", "Those Eyes", "Every Cloud"})}",
			16 => $"Long {db.GetRandom(new[]{"Black", "Lonesome", "Cold", "Dusty", "Empty"})} {db.GetRandom(db.nounsCountry)}",
			17 => $"{db.GetRandom(db.statesAndRegions)} {db.GetRandom(new[]{"Waltz", "Woman", "Man", "Rain", "Blue"})}",
			18 => $"If You've Got The {db.GetRandom(new[]{"Money", "Time", "Heart", "Love", "Nerve"})}",
			19 => $"{db.GetRandom(new[]{"Hello", "Goodbye", "Farewell", "So Long"})} {db.GetRandom(db.nounsCountry)}",
			_ => $"{db.GetRandom(db.adjectivesCountry)} {db.GetRandom(db.nounsCountry)}"
		};
	}
	
	private string GeneratePsychSongTitle() {
		int pattern = RandInt(0, 20);
		return pattern switch {
			0 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)}",
			1 => $"{db.GetRandom(new[]{"Eight", "Five", "Seven", "Ten", "Thousand"})} {db.GetRandom(new[]{"Miles High", "Miles Low", "Days", "Nights", "Years"})}",
			2 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(new[]{"Fields", "Skies", "Dreams", "Meadows"})} Forever",
			3 => $"{db.GetRandom(db.phrasesPsychOpeners)} {db.GetRandom(db.nounsPsych)}",
			4 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)}",
			5 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)} {db.GetRandom(db.groupNounsPsych)}",
			6 => $"{db.GetRandom(db.phrasesPsychStructures)} {db.GetRandom(db.nounsPsych)}",
			7 => $"{db.GetRandom(db.femaleNamesWhite)} In The {db.GetRandom(db.nounsPsych)} With {db.GetRandom(db.groupNounsGems)}",
			8 => $"The {db.GetRandom(db.nounsPsych)}",
			9 => $"{db.GetRandom(db.phrasesPsychOpeners)} {db.GetRandom(new[]{"Never Knows", "Always Comes", "Waits For No One", "Is Today"})}",
			10 => $"I Am The {db.GetRandom(db.nounsPsych)}",
			11 => $"{db.GetRandom(db.nounsPsych)} And {db.GetRandom(db.nounsPsych)}",
			12 => $"{db.GetRandom(new[]{"Season", "Time", "Age", "Day", "Night"})} Of The {db.GetRandom(db.nounsPsych)}",
			13 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(new[]{"Mystery", "Magical", "Cosmic", "Astral"})} {db.GetRandom(new[]{"Tour", "Trip", "Journey", "Voyage"})}",
			14 => $"{db.GetRandom(db.nounsPsych)} Is A {db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)}",
			15 => $"{RandInt(100, 2001)} {db.GetRandom(new[]{"Light Years", "Miles", "Days", "Nights"})} From {db.GetRandom(new[]{"Home", "Here", "Love", "You"})}",
			16 => $"She {db.GetRandom(db.verbsUniversal)}s She {db.GetRandom(db.verbsUniversal)}s",
			17 => $"{db.GetRandom(new[]{"Good", "Beautiful", "Lovely", "Strange"})} {db.GetRandom(new[]{"Morning", "Day", "Night", "World", "People"})}",
			18 => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(new[]{"Mind", "Consciousness", "Awareness", "Vision", "Dream"})}",
			19 => $"Are You {db.GetRandom(new[]{"Experienced", "Ready", "Aware", "Alive", "Dreaming"})}",
			_ => $"{db.GetRandom(db.adjectivesPsych)} {db.GetRandom(db.nounsPsych)}"
		};
	}
	
	private string GenerateFolkSongTitle() {
		int pattern = RandInt(0, 15);
		return pattern switch {
			0 => $"The {db.GetRandom(db.nounsUniversal)} {db.GetRandom(new[]{"They Are A-Changin'", "Keep On", "Will Come", "Have Gone"})}",
			1 => $"{db.GetRandom(db.verbsUniversal)}in' In The {db.GetRandom(db.nounsUniversal)}",
			2 => $"Where Have All The {db.GetRandom(db.nounsUniversal)}s Gone",
			3 => $"This {db.GetRandom(db.nounsUniversal)} Is {db.GetRandom(new[]{"Your", "My", "Our"})} {db.GetRandom(db.nounsUniversal)}",
			4 => $"If I Had A {db.GetRandom(db.nounsUniversal)}",
			5 => $"The {db.GetRandom(new[]{"Sound", "Voice", "Echo", "Song"})} Of {db.GetRandom(db.nounsUniversal)}",
			6 => $"{db.GetRandom(new[]{"Both", "All", "Every", "Either"})} {db.GetRandom(new[]{"Sides", "Ways", "Roads", "Paths"})} {db.GetRandom(new[]{"Now", "Then", "Before", "After"})}",
			7 => $"{db.GetRandom(db.maleNamesWhite)} {db.GetRandom(db.lastNamesGeneric)}'s {db.GetRandom(new[]{"Blues", "Ballad", "Song", "Dream", "Story"})}",
			8 => $"{db.GetRandom(new[]{"Turn", "Walk", "Run", "Come", "Go"})}! {db.GetRandom(new[]{"Turn", "Walk", "Run", "Come", "Go"})}! {db.GetRandom(new[]{"Turn", "Walk", "Run", "Come", "Go"})}!",
			9 => $"Mr. {db.GetRandom(new[]{"Tambourine", "Spaceman", "Sandman", "Moonlight", "Sunshine"})} Man",
			10 => $"{db.GetRandom(new[]{"Early", "Late", "Cold", "Warm"})} {db.GetRandom(new[]{"Morning", "Evening", "Night", "Autumn"})} {db.GetRandom(db.nounsUniversal)}",
			11 => $"{db.GetRandom(new[]{"500", "1000", "100"})} {db.GetRandom(new[]{"Miles", "Days", "Years", "Nights"})} {db.GetRandom(new[]{"Away", "From Home", "To Go", "Behind"})}",
			12 => $"{db.GetRandom(db.maleNamesWhite)} {db.GetRandom(db.verbsUniversal)} The {db.GetRandom(db.nounsUniversal)}",
			13 => $"We Shall {db.GetRandom(new[]{"Overcome", "Not Be Moved", "Rise", "Prevail", "Endure"})}",
			14 => $"{db.GetRandom(db.adjectivesUniversal)} On My {db.GetRandom(new[]{"Mind", "Heart", "Soul", "Eyes"})}",
			_ => $"{db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.nounsUniversal)}"
		};
	}
	
	private string GenerateDefaultSongTitle(int year) {
		if (year < 1964) return GenerateEarly60sSongTitle();
		else if (year < 1967) return Randf() < 0.5f ? GenerateEarly60sSongTitle() : GenerateSoulSongTitle();
		else {
			int roll = RandInt(0, 3);
			return roll switch {
				0 => GeneratePsychSongTitle(),
				1 => GenerateSoulSongTitle(),
				_ => GenerateEarly60sSongTitle()
			};
		}
	}
	
	// ========================================================================
	// ALBUM TITLE GENERATION
	// ========================================================================
	
	public string GenerateAlbumTitle(Genre genre, int year, string artistName, bool isCompilation = false) {
		if (isCompilation) return GenerateCompilationTitle(artistName);
		int pattern = RandInt(0, 12);
		return pattern switch {
			0 => artistName,
			1 => string.Format(db.GetRandom(db.albumFormats), artistName),
			2 => $"{GetGenreAdjective(genre)} {GetGenreNoun(genre)}",
			3 => $"{GetGenreNoun(genre)} Of {GetGenreNoun(genre)}",
			4 => $"{db.GetRandom(db.verbsUniversal)}ing {GetGenreNoun(genre)}",
			5 => $"From {db.GetRandom(genre == Genre.Country ? db.citiesCountry : genre == Genre.SurfRock ? db.citiesSurf : db.citiesGeneral)}",
			6 => GetFirstWord(artistName) != artistName ? $"{GetGenreAdjective(genre)} {GetFirstWord(artistName)}" : artistName,
			7 => db.GetRandom(db.albumThemes),
			8 => $"{db.GetRandom(new[]{"Midnight", "Morning", "Summer", "Winter", "Autumn", "Evening"})} {GetGenreNoun(genre)}",
			9 => $"{db.GetRandom(new[]{"In", "At", "Live From", "Direct From"})} {db.GetRandom(genre == Genre.BritishInvasion ? db.citiesBritish : db.citiesGeneral)}",
			10 => $"{db.GetRandom(new[]{"Blue", "Golden", "Silver", "Black", "White", "Red"})} {GetGenreNoun(genre)}",
			11 => $"{db.GetRandom(new[]{"One", "Two", "Three", "Seven", "Twelve"})} {GetGenreNoun(genre)}s",
			_ => artistName
		};
	}
	
	private string GenerateCompilationTitle(string artistName) {
		int pattern = RandInt(0, 8);
		return pattern switch {
			0 => $"The Best Of {artistName}",
			1 => $"{artistName}'s Greatest Hits",
			2 => $"Golden Hits Of {artistName}",
			3 => $"The Very Best Of {artistName}",
			4 => $"Anthology: {artistName}",
			5 => $"{artistName}: The Collection",
			6 => $"More Hits From {artistName}",
			7 => $"The {artistName} Story",
			_ => $"The Best Of {artistName}"
		};
	}
	
	private string GetGenreAdjective(Genre genre) => genre switch {
		Genre.Psychedelic => db.GetRandom(db.adjectivesPsych),
		Genre.SurfRock => db.GetRandom(db.adjectivesSurf),
		Genre.Soul or Genre.RnB => db.GetRandom(db.adjectivesSoul),
		Genre.Country => db.GetRandom(db.adjectivesCountry),
		_ => db.GetRandom(db.adjectivesUniversal)
	};
	
	private string GetGenreNoun(Genre genre) => genre switch {
		Genre.Psychedelic => db.GetRandom(db.nounsPsych),
		Genre.SurfRock => db.GetRandom(db.nounsSurf),
		Genre.Soul or Genre.RnB => db.GetRandom(db.nounsSoul),
		Genre.Country => db.GetRandom(db.nounsCountry),
		Genre.GirlGroup or Genre.DooWop or Genre.TeenPop => db.GetRandom(db.nounsEarly60s),
		_ => db.GetRandom(db.nounsUniversal)
	};
	
	private string GetFirstWord(string input) {
		if (string.IsNullOrEmpty(input)) return input;
		int spaceIndex = input.IndexOf(' ');
		return spaceIndex > 0 ? input.Substring(0, spaceIndex) : input;
	}
	
	// ========================================================================
	// INSTRUMENTAL TITLE GENERATION
	// ========================================================================
	
	public string GenerateInstrumentalTitle(Genre genre, int year) {
		int pattern = RandInt(0, 10);
		return pattern switch {
			0 => db.GetRandom(db.instrumentalNouns),
			1 => $"{db.GetRandom(db.instrumentalAdjectives)} {db.GetRandom(db.instrumentalNouns)}",
			2 => $"{db.GetRandom(db.instrumentalPlaces)} {db.GetRandom(db.instrumentalNouns)}",
			3 => $"The {db.GetRandom(db.instrumentalNouns)}",
			4 => $"{db.GetRandom(db.verbsAction)} {db.GetRandom(new[]{"Don't Run", "Away", "On", "Down", "Right"})}",
			5 => db.GetRandom(new[]{"Apache", "Tequila", "Bongo", "Safari", "Caravan", "Exodus", "Telstar", "Wipeout"}),
			6 => $"{db.GetRandom(db.instrumentalPlaces)} {db.GetRandom(db.instrumentalNouns)}",
			7 => $"Theme From {db.GetRandom(new[]{"A Summer Place", "The Apartment", "Exodus", "The Untouchables", "Rebel Without A Cause"})}",
            8 => RandInt(100, 999).ToString(),
			9 => $"{db.GetRandom(db.instrumentalAdjectives)} {db.GetRandom(db.instrumentalPlaces)}",
            _ => db.GetRandom(db.instrumentalNouns)
        };
    }
    
    // ========================================================================
    // HELPER METHODS - Genre Detection
    // ========================================================================
    
    private float GetFemaleChance(Genre genre) => genre switch {
        Genre.GirlGroup => 1.0f, Genre.Soul => 0.35f, Genre.RnB => 0.3f, Genre.Country => 0.25f,
        Genre.TeenPop => 0.3f, Genre.TraditionalPop => 0.35f, Genre.Folk => 0.35f, Genre.Gospel => 0.4f,
        Genre.DooWop => 0.15f, Genre.RockAndRoll => 0.1f, Genre.SurfRock => 0.05f, Genre.GarageRock => 0.05f,
        Genre.Psychedelic => 0.1f, Genre.BritishInvasion => 0.08f, _ => 0.2f
    };
    
    private bool IsAfricanAmericanGenre(Genre genre) => genre switch {
        Genre.Soul => true, Genre.RnB => true, Genre.DooWop => true, Genre.GirlGroup => true,
        Genre.Gospel => true, Genre.Jazz => true, _ => false
    };
    
    private bool IsEastCoastGenre(Genre genre) => genre switch {
        Genre.DooWop => true, Genre.GirlGroup => true, Genre.TeenPop => true, _ => false
    };
    
    private bool IsCountryGenre(Genre genre) => genre == Genre.Country;
    
    private bool IsBrillBuildingGenre(Genre genre) => genre switch {
        Genre.GirlGroup => true, Genre.TeenPop => true, Genre.DooWop => true,
        Genre.TraditionalPop => true, _ => false
    };
    
    private bool IsPsychedelicGenre(Genre genre) => genre switch {
        Genre.Psychedelic => true, Genre.GarageRock => true, Genre.Folk => true, _ => false
    };
    
    // ========================================================================
    // HELPER METHODS - String Utilities
    // ========================================================================
    
    private string GetOrdinal(int number) {
        string suffix = (number % 100) switch {
			11 or 12 or 13 => "th",
            _ => (number % 10) switch {
				1 => "st", 2 => "nd", 3 => "rd", _ => "th"
            }
        };
		return $"{number}{suffix}";
    }
    
    private string GetWrittenNumber(int number) => number switch {
		1 => "One", 2 => "Two", 3 => "Three", 4 => "Four", 5 => "Five", 6 => "Six",
		7 => "Seven", 8 => "Eight", 9 => "Nine", 10 => "Ten", 11 => "Eleven", 12 => "Twelve",
        _ => number.ToString()
    };
    
    // ========================================================================
    // LABEL NAME GENERATION
    // ========================================================================
    
   public string GenerateLabelName(LabelArchetype archetype) => archetype switch {
        LabelArchetype.SoulFactory => GenerateMotownStyleLabelName(),
        LabelArchetype.BluesRoots => GenerateAtlanticStyleLabelName(),
        LabelArchetype.GospelPowerhouse => GenerateMotownStyleLabelName(),
        LabelArchetype.RockRebel => GenerateSunStyleLabelName(),
        LabelArchetype.CountrySpecialist => GenerateSunStyleLabelName(),
        LabelArchetype.CorporateGiant => GenerateMajorLabelName(),
        LabelArchetype.TeenHitMachine => GenerateMajorLabelName(),
        LabelArchetype.FolkBoutique => GenerateIndieLabelName(),
        LabelArchetype.JazzPrestige => GenerateIndieLabelName(),
        LabelArchetype.RegionalHustler => GenerateIndieLabelName(),
        _ => GenerateGenericLabelName()
    };
    
    private string GenerateMotownStyleLabelName() {
        int pattern = RandInt(0, 6);
        return pattern switch {
			0 => $"{db.GetRandom(db.citiesGeneral)} Sound",
			1 => $"{db.GetRandom(db.nounsSoul)} Records",
			2 => $"{db.GetRandom(db.adjectivesSoul)} Records",
			3 => $"{db.GetRandom(db.groupNounsGems)} Records",
            4 => db.GetRandom(db.nounsSoul),
			5 => $"{db.GetRandom(db.citiesGeneral)} Records",
			_ => "Soul Records"
        };
    }
    
    private string GenerateAtlanticStyleLabelName() {
        int pattern = RandInt(0, 5);
        return pattern switch {
			0 => $"{db.GetRandom(new[]{"Atlantic", "Pacific", "Continental", "National", "Imperial"})} Records",
			1 => $"{db.GetRandom(db.groupNounsRoyalty)} Records",
			2 => $"{db.GetRandom(new[]{"Stax", "Volt", "Chess", "Duke", "Ace"})} Records",
			3 => $"{db.GetRandom(db.adjectivesSoul)} Sound",
			4 => $"{db.GetRandom(db.citiesGeneral)} Sound",
			_ => "Rhythm Records"
        };
    }
    
    private string GenerateSunStyleLabelName() {
        int pattern = RandInt(0, 5);
        return pattern switch {
			0 => $"{db.GetRandom(new[]{"Sun", "Moon", "Star", "Mercury", "Saturn"})} Records",
			1 => $"{db.GetRandom(db.citiesCountry)} Records",
			2 => $"{db.GetRandom(new[]{"Starday", "Monument", "Plantation", "Hickory"})} Records",
			3 => $"{db.GetRandom(db.statesAndRegions)} Records",
			4 => $"{db.GetRandom(db.adjectivesCountry)} Records",
			_ => "Hillbilly Records"
        };
    }
    
    private string GenerateIndieLabelName() {
        int pattern = RandInt(0, 6);
        return pattern switch {
			0 => $"{db.GetRandom(db.groupNounsBritish)} Records",
			1 => $"{db.GetRandom(db.adjectivesPsych)} Sound",
			2 => $"{db.GetRandom(db.nounsPsych)} Records",
			3 => $"{db.GetRandom(db.citiesGeneral)} Underground",
			4 => $"{db.GetRandom(db.lastNamesGeneric)} Records",
            5 => db.GetRandom(db.groupNounsPsych),
			_ => "Independent Records"
        };
    }
    
    private string GenerateMajorLabelName() {
        int pattern = RandInt(0, 5);
        return pattern switch {
			0 => $"{db.GetRandom(new[]{"Columbia", "Capitol", "RCA", "Decca", "Mercury"})} Records",
			1 => $"{db.GetRandom(new[]{"American", "National", "United", "Republic", "Liberty"})} Records",
			2 => $"{db.GetRandom(db.lastNamesGeneric)} Brothers",
			3 => $"{db.GetRandom(new[]{"Victor", "Brunswick", "Parlophone", "HMV"})}",
			4 => $"{db.GetRandom(db.groupNounsRoyalty)} Records",
			_ => "Major Records"
        };
    }
    
    private string GenerateBritishLabelName() {
        int pattern = RandInt(0, 5);
        return pattern switch {
			0 => $"{db.GetRandom(db.citiesBritish)} Records",
			1 => $"{db.GetRandom(db.groupNounsBritish)} Records",
			2 => $"{db.GetRandom(new[]{"Parlophone", "Deram", "Immediate", "Track", "Reaction"})}",
			3 => $"{db.GetRandom(db.lastNamesBritish)} Records",
			4 => $"{db.GetRandom(db.adjectivesUniversal)} Sound",
			_ => "British Records"
        };
    }
    
    private string GenerateGenericLabelName() {
        int pattern = RandInt(0, 6);
        return pattern switch {
			0 => $"{db.GetRandom(db.lastNamesGeneric)} Records",
			1 => $"{db.GetRandom(db.citiesGeneral)} Records",
			2 => $"{db.GetRandom(db.nounsUniversal)} Records",
			3 => $"{db.GetRandom(db.adjectivesUniversal)} Records",
			4 => $"{db.GetRandom(db.groupNounsMusical)} Records",
			5 => $"{db.GetRandom(db.lastNamesGeneric)} & {db.GetRandom(db.lastNamesGeneric)}",
			_ => "Generic Records"
        };
    }
    
    // ========================================================================
    // VENUE NAME GENERATION
    // ========================================================================
    
    public string GenerateVenueName(VenueType venueType, string city = null) {
        city ??= db.GetRandom(db.citiesGeneral);
        return venueType switch {
            VenueType.SmallClub => GenerateSmallClubName(city),
            VenueType.Theater => GenerateTheaterName(city),
            VenueType.Arena => GenerateArenaName(city),
            VenueType.Stadium => GenerateStadiumName(city),
            VenueType.CoffeHouse => GenerateCoffeeHouseName(city),
            VenueType.HonkyTonk => GenerateHonkyTonkName(city),
            VenueType.JukeJoint => GenerateJukeJointName(city),
            _ => GenerateGenericVenueName(city)
        };
    }
    
    private string GenerateSmallClubName(string city) {
        int pattern = RandInt(0, 8);
        return pattern switch {
			0 => $"The {db.GetRandom(db.groupNounsAnimals).TrimEnd('s')} Club",
			1 => $"{db.GetRandom(db.lastNamesGeneric)}'s",
			2 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(new[]{"Door", "Room", "Spot", "Corner", "Place"})}",
			3 => $"Club {db.GetRandom(db.nounsUniversal)}",
			4 => $"The {db.GetRandom(new[]{"Cavern", "Cellar", "Basement", "Underground", "Hideaway"})}",
			5 => $"{city} {db.GetRandom(new[]{"A-Go-Go", "Discotheque", "Club", "Lounge"})}",
			6 => $"The {db.GetRandom(db.groupNounsBritish).TrimEnd('s')} Den",
			7 => $"{db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.lastNamesGeneric)}'s",
			_ => $"The {db.GetRandom(db.nounsUniversal)} Club"
        };
    }
    
    private string GenerateTheaterName(string city) {
        int pattern = RandInt(0, 6);
        return pattern switch {
			0 => $"The {city} {db.GetRandom(new[]{"Apollo", "Paramount", "Orpheum", "Rialto", "Majestic"})}",
			1 => $"The {db.GetRandom(db.adjectivesUniversal)} Theater",
			2 => $"{db.GetRandom(db.lastNamesGeneric)} Auditorium",
			3 => $"The {db.GetRandom(db.groupNounsRoyalty).TrimEnd('s')} Theater",
			4 => $"{city} Ballroom",
			5 => $"The {db.GetRandom(new[]{"Grand", "Royal", "Imperial", "Palace"})} {city}",
			_ => $"{city} Theater"
        };
    }
    
    private string GenerateArenaName(string city) {
        int pattern = RandInt(0, 5);
        return pattern switch {
			0 => $"{city} {db.GetRandom(new[]{"Arena", "Coliseum", "Civic Center", "Sports Arena"})}",
			1 => $"The {city} {db.GetRandom(new[]{"Garden", "Forum", "Dome", "Pavilion"})}",
			2 => $"{db.GetRandom(db.lastNamesGeneric)} Arena",
			3 => $"{city} Convention Center",
			4 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(new[]{"Arena", "Center", "Hall"})}",
			_ => $"{city} Arena"
        };
    }
    
    private string GenerateStadiumName(string city) {
        int pattern = RandInt(0, 4);
        return pattern switch {
			0 => $"{city} Stadium",
			1 => $"{db.GetRandom(db.lastNamesGeneric)} Field",
			2 => $"The {city} Bowl",
			3 => $"{city} Municipal Stadium",
			_ => $"{city} Stadium"
        };
    }
    
    private string GenerateCoffeeHouseName(string city) {
        int pattern = RandInt(0, 6);
        return pattern switch {
			0 => $"The {db.GetRandom(new[]{"Gaslight", "Bitter End", "Troubadour", "Exit", "Gate"})}",
			1 => $"Café {db.GetRandom(db.nounsUniversal)}",
			2 => $"The {db.GetRandom(db.adjectivesPsych)} {db.GetRandom(new[]{"Bean", "Cup", "Brew", "Grind"})}",
			3 => $"{db.GetRandom(db.lastNamesGeneric)}'s Coffee House",
			4 => $"The {db.GetRandom(new[]{"Folklore", "Hootenanny", "Sing-Along", "Open Mic"})} Café",
			5 => $"The {db.GetRandom(db.groupNounsBritish).TrimEnd('s')} Coffee House",
			_ => "The Coffee House"
        };
    }
    
    private string GenerateHonkyTonkName(string city) {
        int pattern = RandInt(0, 6);
        return pattern switch {
			0 => $"{db.GetRandom(db.lastNamesCountry)}'s {db.GetRandom(new[]{"Honky Tonk", "Roadhouse", "Saloon", "Bar & Grill"})}",
			1 => $"The {db.GetRandom(db.adjectivesCountry)} {db.GetRandom(new[]{"Cowboy", "Rodeo", "Ranch", "Trail"})}",
			2 => $"The {db.GetRandom(new[]{"Broken Spoke", "Rusty Spur", "Silver Saddle", "Golden Horseshoe"})}",
			3 => $"{db.GetRandom(db.statesAndRegions)} {db.GetRandom(new[]{"Dancehall", "Opry", "Jamboree"})}",
			4 => $"The {db.GetRandom(db.nounsCountry)} {db.GetRandom(new[]{"Lounge", "Tavern", "Inn"})}",
			5 => $"{db.GetRandom(db.maleNamesCountry)}'s Place",
			_ => "The Honky Tonk"
        };
    }
    
    private string GenerateJukeJointName(string city) {
        int pattern = RandInt(0, 6);
        return pattern switch {
			0 => $"{db.GetRandom(db.maleNamesBlack)}'s {db.GetRandom(new[]{"Juke Joint", "Blues Club", "Lounge", "Place"})}",
			1 => $"The {db.GetRandom(db.adjectivesSoul)} {db.GetRandom(new[]{"Room", "Spot", "Corner", "Joint"})}",
			2 => $"{db.GetRandom(new[]{"Club", "Café", "Lounge"})} {db.GetRandom(db.nounsSoul)}",
			3 => $"The {db.GetRandom(db.groupNounsRoyalty).TrimEnd('s')}'s Den",
			4 => $"{db.GetRandom(db.citiesGeneral)} Blues Club",
			5 => $"The {db.GetRandom(new[]{"Chitlin'", "Soul", "Rhythm", "Blues"})} Circuit",
			_ => "The Juke Joint"
		};
	}
	
	private string GenerateGenericVenueName(string city) => $"The {city} {db.GetRandom(new[]{"Club", "Hall", "Room", "Lounge", "Theater"})}";
	
	// ========================================================================
	// RADIO STATION NAME GENERATION
	// ========================================================================
	
	public string GenerateRadioStationName(string city = null) {
		city ??= db.GetRandom(db.citiesGeneral);
		string prefix = Randf() < 0.5f ? "W" : "K";
		string letters = prefix + GetRandomLetters(3);
		int pattern = RandInt(0, 6);
		return pattern switch {
			0 => $"{letters} Radio",
			1 => $"{letters} {city}",
			2 => $"{letters} - The {db.GetRandom(new[]{"Sound", "Beat", "Voice", "Pulse", "Heart"})} of {city}",
			3 => $"{letters} {db.GetRandom(new[]{"Boss", "Big", "Super", "Mighty", "Fabulous"})} Radio",
			4 => $"{letters}-FM",
			5 => $"{letters} {RandInt(55, 99) * 10}",
			_ => letters
		};
	}
	
	private string GetRandomLetters(int count) {
		const string consonants = "BCDFGHJKLMNPQRSTVWXYZ";
		const string vowels = "AEIOU";
		string result = "";
		for (int i = 0; i < count; i++) {
			result += i % 2 == 0 ? consonants[RandInt(0, consonants.Length)] : vowels[RandInt(0, vowels.Length)];
		}
		return result;
	}
	
	// ========================================================================
	// MUSIC PUBLICATION NAME GENERATION
	// ========================================================================
	
	public string GeneratePublicationName() {
		int pattern = RandInt(0, 8);
		return pattern switch {
			0 => $"{db.GetRandom(new[]{"Hit", "Pop", "Rock", "Teen", "Music", "Beat", "Sound"})} {db.GetRandom(new[]{"Parade", "Weekly", "Monthly", "Magazine", "World"})}",
			1 => $"The {db.GetRandom(db.nounsUniversal)} {db.GetRandom(new[]{"Times", "Post", "Herald", "Tribune"})}",
			2 => $"{db.GetRandom(new[]{"Billboard", "Cashbox", "Record", "Disc", "Melody"})} {db.GetRandom(new[]{"World", "Mirror", "Maker", "Weekly"})}",
			3 => $"{db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(new[]{"Sounds", "Beats", "Notes", "Tunes"})}",
			4 => $"The {db.GetRandom(new[]{"Top", "Hot", "Cool", "Hip"})} {db.GetRandom(new[]{"40", "50", "100"})}",
			5 => $"{db.GetRandom(new[]{"Teen", "Young", "Mod", "Groovy"})} {db.GetRandom(new[]{"Scene", "Life", "Beat", "Wave"})}",
			6 => $"{db.GetRandom(db.citiesGeneral)} {db.GetRandom(new[]{"Sound", "Scene", "Music"})} Magazine",
			7 => $"The {db.GetRandom(new[]{"Rolling", "Crawling", "Running", "Flying"})} {db.GetRandom(db.groupNounsAnimals).TrimEnd('s')}",
			_ => "Music Magazine"
		};
	}
	
	// ========================================================================
	// TOUR NAME GENERATION
	// ========================================================================
	
	public string GenerateTourName(string artistName, int year, Genre genre) {
		int pattern = RandInt(0, 10);
		return pattern switch {
			0 => $"The {artistName} {year} Tour",
			1 => $"{artistName} Live '{year % 100:D2}",
			2 => $"The {GetGenreAdjective(genre)} {GetGenreNoun(genre)} Tour",
			3 => $"{artistName}: {db.GetRandom(new[]{"On The Road", "Live In Concert", "In Person", "The Tour"})}",
			4 => $"The {db.GetRandom(db.adjectivesUniversal)} {artistName} Tour",
			5 => $"{db.GetRandom(new[]{"Summer", "Fall", "Winter", "Spring"})} '{year % 100:D2} with {artistName}",
			6 => $"The {GetGenreNoun(genre)} Tour featuring {artistName}",
			7 => $"{artistName} - {db.GetRandom(new[]{"Coast to Coast", "Across America", "World Tour", "USA Tour"})}",
			8 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.nounsUniversal)} Revue",
			9 => $"{db.GetRandom(new[]{"Caravan", "Cavalcade", "Revue", "Spectacular"})} of Stars '{year % 100:D2}",
			_ => $"{artistName} Tour '{year % 100:D2}"
		};
	}
	
	// ========================================================================
	// MUSIC AWARD NAME GENERATION
	// ========================================================================
	
	public string GenerateAwardName() {
		int pattern = RandInt(0, 6);
		return pattern switch {
			0 => $"The {db.GetRandom(db.lastNamesGeneric)} Award",
			1 => $"The {db.GetRandom(db.adjectivesUniversal)} {db.GetRandom(db.nounsUniversal)} Award",
			2 => $"The {db.GetRandom(db.citiesGeneral)} Music Award",
			3 => $"The {db.GetRandom(new[]{"Gold", "Silver", "Platinum", "Diamond"})} {db.GetRandom(new[]{"Record", "Disc", "Note", "Star"})}",
			4 => $"Best {db.GetRandom(new[]{"New Artist", "Single", "Album", "Group", "Male Vocalist", "Female Vocalist"})}",
			5 => $"The {db.GetRandom(db.groupNounsRoyalty).TrimEnd('s')}'s Award of Excellence",
			_ => "Music Award"
		};
	}
	
	// ========================================================================
	// SONGWRITER/PRODUCER CREDIT GENERATION
	// ========================================================================
	
	public string GenerateSongwriterName(Genre genre, bool isTeam = false) {
		if (isTeam) return $"{db.GetRandom(db.lastNamesGeneric)} & {db.GetRandom(db.lastNamesGeneric)}";
		bool isFemale = Randf() < 0.3f;
		bool isJewish = IsBrillBuildingGenre(genre) && Randf() < 0.4f;
		string firstName = isFemale ? db.GetRandom(db.femaleNamesWhite) : db.GetRandom(db.maleNamesWhite);
		string lastName = isJewish ? db.GetRandom(db.lastNamesJewish) : db.GetRandom(db.lastNamesGeneric);
		return $"{firstName} {lastName}";
	}
	
	public string GenerateProducerName(Genre genre) {
		bool usesTitle = Randf() < 0.15f;
		string firstName = db.GetRandom(db.maleNamesWhite);
		string lastName = db.GetRandom(db.lastNamesGeneric);
		if (usesTitle) return $"{db.GetRandom(new[]{"Phil", "Joe", "Jerry", "Bob", "Sam"})} \"{db.GetRandom(db.nicknamesQuoted)}\" {lastName}";
		return $"{firstName} {lastName}";
	}
	
	// ========================================================================
	// FAN CLUB NAME GENERATION
	// ========================================================================
	
	public string GenerateFanClubName(string artistName) {
		int pattern = RandInt(0, 6);
		return pattern switch {
			0 => $"The Official {artistName} Fan Club",
			1 => $"{artistName}'s {db.GetRandom(db.groupNounsRoyalty)}",
			2 => $"The {artistName} Appreciation Society",
			3 => $"{GetFirstWord(artistName)}'s {db.GetRandom(new[]{"Army", "Nation", "World", "Kingdom", "Legion"})}",
			4 => $"We Love {artistName} Club",
			5 => $"The {artistName} {db.GetRandom(db.groupNounsAnimals)}",
			_ => $"{artistName} Fan Club"
		};
	}
	
	// ========================================================================
	// BAND MEMBER NAME GENERATION
	// ========================================================================
	
	public string GenerateBandMemberName(Genre genre, bool isFemale, string bandEthnicity = null) {
		bool isBlack = bandEthnicity == "black" || (IsAfricanAmericanGenre(genre) && Randf() < 0.8f);
		bool isItalian = bandEthnicity == "italian" || (IsEastCoastGenre(genre) && !isBlack && Randf() < 0.3f);
		bool isBritish = genre == Genre.BritishInvasion || bandEthnicity == "british";
		string firstName = SelectBandMemberFirstName(isFemale, isBlack, isBritish);
		string lastName = SelectBandMemberLastName(isBlack, isItalian, isBritish);
		return $"{firstName} {lastName}";
	}
	
	private string SelectBandMemberFirstName(bool isFemale, bool isBlack, bool isBritish) {
		if (isFemale) {
			if (isBlack) return db.GetRandom(db.femaleNamesBlack);
			return db.GetRandom(db.femaleNamesWhite);
		} else {
			if (isBlack) return db.GetRandom(db.maleNamesBlack);
			if (isBritish) return db.GetWeighted(new[]{"John", "Paul", "George", "Peter", "Roger", "Keith", "Mick", "Eric", "Jeff", "Graham", "Brian", "Ray", "Dave"}, db.maleNamesWhite, 0.4f);
			return db.GetRandom(db.maleNamesWhite);
		}
	}
	
	private string SelectBandMemberLastName(bool isBlack, bool isItalian, bool isBritish) {
		if (isItalian) return db.GetWeighted(db.lastNamesItalian, db.lastNamesGeneric, 0.5f);
		if (isBritish) return db.GetWeighted(db.lastNamesBritish, db.lastNamesGeneric, 0.6f);
		return db.GetRandom(db.lastNamesGeneric);
	}
	
	// ========================================================================
	// SINGLE B-SIDE TITLE GENERATION
	// ========================================================================
	
	public string GenerateBSideTitle(Genre genre, int year, string aSideTitle) {
		int pattern = RandInt(0, 5);
		return pattern switch {
			0 => Randf() < 0.3f ? $"{aSideTitle} (Instrumental)" : GenerateInstrumentalTitle(genre, year),
			1 => GenerateSongTitleInternal(genre, year),
			2 => GenerateRelatedBSide(genre, year),
			3 => GenerateSongTitleInternal(genre, year),
			4 => GenerateSongTitleInternal(genre, year),
			_ => GenerateSongTitleInternal(genre, year)
		};
	}
	
	private string GenerateRelatedBSide(Genre genre, int year) {
		int pattern = RandInt(0, 5);
		return pattern switch {
			0 => $"The Other Side Of {db.GetRandom(db.nounsUniversal)}",
			1 => $"{db.GetRandom(db.verbsUniversal)}ing {db.GetRandom(new[]{"Alone", "Again", "Away", "Forever"})}",
			2 => $"What {db.GetRandom(new[]{"Happened", "Became", "About"})} {db.GetRandom(new[]{"To Us", "To Love", "To Me", "To You"})}",
			3 => $"Just {db.GetRandom(new[]{"A Dream", "A Memory", "A Fool", "A Man", "A Woman"})}",
			4 => $"Don't {db.GetRandom(db.verbsUniversal)} {db.GetRandom(new[]{"Me", "Away", "Now", "Yet"})}",
			_ => GenerateSongTitleInternal(genre, year)
		};
	}
}

// ========================================================================
// MARKOV CHAIN ENGINE
// Learns letter sequences from input data to generate new, authentic words
// ========================================================================
public class MarkovChain {
	private Dictionary<string, List<char>> chain = new Dictionary<string, List<char>>();
	private int order;
	private System.Random rng = new System.Random();

	public MarkovChain(int order = 2) {
		this.order = order;
	}

	public void Train(IEnumerable<string> words) {
		foreach (var word in words) {
			// Pad with spaces so it knows where words start and end
			string padded = new string(' ', order) + word.ToLower() + ' ';
			for (int i = 0; i <= padded.Length - order - 1; i++) {
				string key = padded.Substring(i, order);
				char nextChar = padded[i + order];

				if (!chain.ContainsKey(key)) {
					chain[key] = new List<char>();
				}
				chain[key].Add(nextChar);
			}
		}
	}

	public string Generate(int minLength = 4, int maxLength = 10) {
		string key = new string(' ', order);
		string result = "";
		int attempts = 0;

		// Try a few times to get a word that fits length constraints
		while (attempts < 20) {
			result = "";
			key = new string(' ', order);

			while (result.Length < maxLength + order) {
				if (!chain.ContainsKey(key)) break;

				var possible = chain[key];
				char next = possible[rng.Next(possible.Count)];

				if (next == ' ') break; // End of word

				result += next;
				key = key.Substring(1) + next;
			}

			if (result.Length >= minLength && result.Length <= maxLength) {
				// Capitalize first letter
				return char.ToUpper(result[0]) + result.Substring(1);
			}
			attempts++;
		}

		// Fallback if constraints weren't met
		if (result.Length == 0) return "Unknown";
		return char.ToUpper(result[0]) + result.Substring(1);
	}
}

// ========================================================================
// SUPPORTING ENUMS
// ========================================================================
public enum VenueType {
    SmallClub, Theater, Arena, Stadium, CoffeHouse, HonkyTonk, JukeJoint, FairGround, RecordingStudio
}
