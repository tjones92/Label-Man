// Scripts/Data/Genre.cs

public enum Genre {
	// === PRE-ROCK / TRADITIONAL ===
	TraditionalPop,         // Sinatra, Dean Martin, early 60s holdovers
	EasyListening,          // Herb Alpert, Mantovani, instrumental pop
	Jazz,                   // Dave Brubeck, Miles Davis (charted occasionally)
	Blues,
	
	// === EARLY ROCK ERA (1955-1962) ===
	RockAndRoll,            // Chuck Berry, Little Richard, early Elvis
	DooWop,                 // The Platters, Drifters
	TeenPop,                // Lesley Gore, Fabian, Bobby Vee, teen idols
	
	// === R&B / SOUL CONTINUUM ===
	RnB,                    // Early rhythm & blues
	Soul,                   // Otis Redding, Aretha, Sam Cooke
	Motown,                 // The Motown Sound specifically (Supremes, Temptations)
	Funk,                   // Late 60s James Brown evolution
	
	// === GIRL GROUPS (keeping separate - see notes) ===
	GirlGroup,              // Ronettes, Crystals, Shangri-Las (distinct production style)
	
	// === COUNTRY / FOLK ===
	Country,                // Nashville sound, Patsy Cline
	Folk,                   // Kingston Trio, early Dylan
	FolkRock,               // Byrds, electric Dylan
	CountryRock,            // Late 60s, Gram Parsons direction
	
	// === BRITISH INVASION ===
	BritishInvasion,        // Beatles, DC5, Herman's Hermits (the "sound")
	Skiffle,                // Pre-Beatles British, Lonnie Donegan influence
	
	// === SURF / CALIFORNIA ===
	SurfRock,               // Beach Boys, Jan & Dean, Dick Dale
	
	// === GARAGE / PROTO-PUNK ===
	GarageRock,             // Kingsmen, Sonics, 96 Tears
	ProtoPunk,              // Stooges, MC5, Velvet Underground (late 60s)
	
	// === PSYCHEDELIA / EXPERIMENTAL ===
	Psychedelic,            // Jefferson Airplane, early Pink Floyd
	AcidRock,               // Heavier psych - Hendrix, Cream
	BaroquePop,             // Left Banke, Zombies, orchestral pop
	SunshinePop,            // Association, 5th Dimension, bright harmonies
	ProgressiveRock,        // Very late 60s, Moody Blues, King Crimson
	
	// === BLUES ROCK / HARD ROCK ===
	BluesRock,              // Fleetwood Mac, John Mayall, British blues
	HardRock,               // Led Zeppelin, early Who power
	ProtoMetal,             // Blue Cheer, Iron Butterfly, Black Sabbath edges
	
	// === POP VARIATIONS ===
	Bubblegum,              // 1910 Fruitgum Co, Ohio Express, Archies
	
	// === INTERNATIONAL ===
	BossaNova,              // Stan Getz/Astrud Gilberto crossover
	SkaRocksteady,          // Jamaican sounds entering US consciousness
	
	// === GOSPEL / SPIRITUAL ===
	Gospel                  // Crossover gospel, Edwin Hawkins late 60s
}
