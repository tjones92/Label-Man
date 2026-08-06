// Scripts/Data/Genre.cs

public enum Genre {
	// === PRE-ROCK / TRADITIONAL ===
	TraditionalPop = 0,     // Sinatra, Dean Martin, early 60s holdovers
	EasyListening = 1,      // Herb Alpert, Mantovani, instrumental pop
	Jazz = 2,               // Dave Brubeck, Miles Davis (charted occasionally)
	Blues = 3,
	
	// === EARLY ROCK ERA (1955-1962) ===
	RockAndRoll = 4,        // Chuck Berry, Little Richard, early Elvis
	DooWop = 5,             // The Platters, Drifters
	TeenPop = 6,            // Lesley Gore, Fabian, Bobby Vee, teen idols
	
	// === R&B / SOUL CONTINUUM ===
	RnB = 7,                // Early rhythm & blues
	Soul = 8,               // Otis Redding, Aretha, Sam Cooke
	Motown = 9,             // Legacy value; migrates to Soul + Motown tag
	Funk = 10,              // Late 60s James Brown evolution
	
	// === GIRL GROUPS (keeping separate - see notes) ===
	GirlGroup = 11,         // Legacy value; migrates to canonical genre + GirlGroup tag
	
	// === COUNTRY / FOLK ===
	Country = 12,           // Nashville sound, Patsy Cline
	Folk = 13,              // Kingston Trio, early Dylan
	FolkRock = 14,          // Byrds, electric Dylan
	CountryRock = 15,       // Late 60s, Gram Parsons direction
	
	// === BRITISH INVASION ===
	BritishInvasion = 16,   // Legacy value; migrates to BritishBeat + British tag
	Skiffle = 17,           // Legacy value; migrates to Folk + Skiffle/British tags
	
	// === SURF / CALIFORNIA ===
	SurfRock = 18,          // Beach Boys, Jan & Dean, Dick Dale
	
	// === GARAGE / PROTO-PUNK ===
	GarageRock = 19,        // Kingsmen, Sonics, 96 Tears
	ProtoPunk = 20,         // Stooges, MC5, Velvet Underground (late 60s)
	
	// === PSYCHEDELIA / EXPERIMENTAL ===
	Psychedelic = 21,       // Legacy value; migrates to PsychedelicRock
	AcidRock = 22,          // Heavier psych - Hendrix, Cream
	BaroquePop = 23,        // Left Banke, Zombies, orchestral pop
	SunshinePop = 24,       // Association, 5th Dimension, bright harmonies
	ProgressiveRock = 25,   // Very late 60s, Moody Blues, King Crimson
	
	// === BLUES ROCK / HARD ROCK ===
	BluesRock = 26,         // Fleetwood Mac, John Mayall, British blues
	HardRock = 27,          // Led Zeppelin, early Who power
	ProtoMetal = 28,        // Blue Cheer, Iron Butterfly, Black Sabbath edges
	
	// === POP VARIATIONS ===
	Bubblegum = 29,         // 1910 Fruitgum Co, Ohio Express, Archies
	
	// === INTERNATIONAL ===
	BossaNova = 30,         // Stan Getz/Astrud Gilberto crossover
	SkaRocksteady = 31,     // Legacy value; migrates by release year
	
	// === GOSPEL / SPIRITUAL ===
	Gospel = 32,            // Crossover gospel, Edwin Hawkins late 60s

	// Canonical Directive 5 identities. Legacy values above are never reordered.
	BritishPop = 33,
	PsychedelicRock = 34,
	BritishBeat = 35,
	BritishBlues = 36,
	Classical = 37,
	Boogaloo = 38,
	TexMex = 39,
	LatinPop = 40,
	Ska = 41,
	Rocksteady = 42,
	Reggae = 43,
	Comedy = 44,
	Childrens = 45,
	ContemporaryFolk = 46,
	SingerSongwriter = 47,
	PopRock = 48,           // Neil Diamond, Three Dog Night, Abbey Road-era Beatles (~1967/68)
	RootsRock = 49,         // CCR, The Band, roots-era Dylan (~1968)
	PsychedelicPop = 50     // Pet Sounds, Donovan (~1966)
}

public enum GenreFamily { Pop, Rock, RhythmAndSoul, Gospel, Country, Folk, Jazz, Blues, Classical, Latin, Caribbean, NonMusic }
public enum GenreLifecycleState { PreEmergent, Emerging, Established, Declining, Legacy }
public enum GenreTag { Motown, GirlGroup, British, Skiffle, Jamaican, Rockabilly, Seasonal, Christmas, Halloween, Summer, Romantic, Novelty, Topical, Instrumental, Orchestral, WallOfSound, HornSection, LoFi, Protest }
