// Scripts/Data/ContactEnums.cs
// Note: ContactType, ContactCategory, RelationshipTier, AvailabilityStatus were dead legacy enums
// (confirmed by author -- they referenced Contact.cs which is now deleted). Removed in Rolodex Phase 2.

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

/// <summary>Immutable population path for a label's operating roster plan.</summary>
public enum LabelPopulationOrigin { Unspecified, LaunchPopulation, RuntimeFounded }

/// <summary>Why the label's operating roster target was most recently reconciled.</summary>
public enum LabelOperatingTargetReason { Unset, LaunchPopulation, RuntimeBootstrap, OrganicGrowth, PromotionReconciliation, DemotionReconciliation, AcquisitionReconciliation }

public enum ArtistType {
	SoloMale,
	SoloFemale,
	Band,
	Duo,
	Trio,
	VocalGroup,
	Unknown
}
