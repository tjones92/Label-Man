// Scripts/Data/ContactEnums.cs

public enum ContactType {
	// Creative
	Producer,
	SessionMusician,
	Songwriter,
	
	// Promotion
	DJ_Local,
	DJ_National,
	Journalist_Trade,
	Journalist_Consumer,
	TVBooker,
	Publicist,
	
	// Distribution
	PressingPlant,
	Distributor,
	JukeboxOperator,
	RetailBuyer,
	
	// Business
	Lawyer,
	Banker,
	LabelExecutive,
	
	// Talent
	TalentScout,
	ArtistManager,
	Artist,
	
	// Underground
	MobContact,
	Fixer
}

public enum LabelTier {
	Major,          // The big corporate players (RCA, Columbia, etc.)
	MidTier,        // Large successful indies (Motown, Atlantic in mid-60s)
	Independent,    // Established indies (Sun, Chess)
	Small,          // Local/Regional startups
	Boutique        // Specialized niche labels
}

public enum LabelArchetype {
	CorporateGiant,     // High budget, low risk, broad appeal
	SoulFactory,        // Motown style: polished, assembly line, loyal
	RockRebel,          // Sun style: raw, high risk, high reward
	TeenHitMachine,     // Brill Building style: polished pop, disposable artists
	BluesRoots,         // Chess style: authentic, niche, steady
	CountrySpecialist,  // Nashville style: conservative, loyal audience
	FolkBoutique,       // Vanguard style: political, artistic, low budget
	JazzPrestige,       // Blue Note style: high art, audiophile quality
	GospelPowerhouse,   // Specialty religious market
	RegionalHustler     // Scrappy local label trying to break out
}

public enum LabelStatus {
	Rising,     // Gaining market share/reputation
	Stable,     // Steady operations
	Struggling, // Losing money/reputation
	Dying,      // Near bankruptcy
	Bankrupt,   // Out of business (financial)
	Defunct,    // Closed down (other reasons/bought out)
	Acquired    // Bought by another label
}

public enum ContactCategory {
	Creative,
	Promotion,
	Distribution,
	Business,
	Talent,
	Underground
}

public enum RelationshipTier {
	Burned,
	Cold,
	Acquaintance,
	Friendly,
	Loyal,
	InYourPocket
}

public enum AvailabilityStatus {
	Available,
	Busy,
	OnVacation,
	InJail,
	Deceased,
	ScreeningYou,
	Unknown
}

// Add to ContactEnums.cs or a new Enums file
public enum ArtistType {
	SoloMale,
	SoloFemale,
	Band,
	Duo,
	Trio,
	VocalGroup,
	Unknown
}
