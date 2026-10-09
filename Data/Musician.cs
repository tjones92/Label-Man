using System;
using Godot;

// Kept as plain C# class since it's generated at runtime
[Serializable]
public class Musician {
	public string personId;
	// Phase 1 observation metadata. Existing economic identity fields remain authoritative.
	public GeographicIdentity geography;
	public string firstName;
	public string lastName;
	public string FullName => $"{firstName} {lastName}";
	public bool isMale;
	public int birthYear;

	public MusicianRole primaryRole;
	public MusicianRole secondaryRole;
	public bool isLeadVocalist;
	public bool isPrimaryWriter;
	public bool isBandLeader;

	public float technicalSkill;
	public float creativity;
	public float musicalVersatility;
	public float stagePresence;
	public float studioEfficiency;

	public float ego;
	public float ambition;
	public float reliability;
	public float loyalty;
	public float temperament;

	public bool isFoundingMember;
	public int joinedYear;
	public bool isActive = true;
	public string reasonLeft;

	// Person-level recognition (SimTools/CelebrityRecognitionDirective.md §8.2, "E-lite"). A fraction
	// of each act-recognition gain reads down to the active members, weighted by visibility, so the
	// front person accrues a name and a sideman does not. These are write-only accumulators: honest
	// biography that stays inert until presentation (§9). Untouched unless recognition is observing.
	// Deliberately NOT adding actAssociation/irreplaceability -- those are dead until lineup churn
	// exists ([[lineup-churn-never-fires]]) and would be pure declared-and-never-read fields.
	public float personalRecognition;   // mass familiarity with THIS person
	public float liveReputation;        // the performer's name: leads / high stagePresence
	public float creativeReputation;    // the maker's name: writers / high creativity

	// ---- Band-member simulation (SimTools/BandMemberSimulationDirective.md) ---------------------------
	// Every field below names its reader in the directive's §4.2 table. All default to zero/null, so an old
	// save and a run with the feature off carry nothing meaningful here.

	// Phase 1b -- split axes, keyed and ANCHORED on technicalSkill, which stays the stored, authoritative
	// value. Nothing existing reads these until a later phase switches a reader (5b: PolarActProfileDeriver;
	// 5: RecalculateStats through growth). axesVersion 0 means "not generated yet".
	public int axesVersion;
	public float instrumentalSkill;
	public float vocalPower;
	public float vocalControl;
	public float diction;
	public float ceilingInstrumental;   // growth origin -> ceiling; scouting reads tells, never this
	public float ceilingVocal;
	public float developmentRate;       // how fast work turns into skill
	public float formationInstrumental; // the growth origin, set once
	public float formationVocalPower;
	public float formationVocalControl;
	public float formationDiction;
	public float formationTechnical;    // technicalSkill at formation: growth moves the live value from here
	public float sightReading;          // read by the session-hire price and outcome (Band Room)

	// Phase 2 -- life state, written at the year boundary by BandLifeService.
	public float roadYears;             // accumulated road load; decline, Burnout, romance and road hazards
	public float substanceLoad;         // 0..1; reliability drift, Reliability strain, bust and death hazards
	public int substanceHeavyYears;     // years spent above the death-hazard threshold
	public MusicianPartner partner;     // sparse
	public bool hasChildren;            // draft-exempt; road reluctance
	public MemberLifeState lifeState;
	public int lifeStateUntilYear;      // Drafted: the year he comes home
	public int deathYear;
	public DeathChannel deathChannel;

	// Membership facts for the CURRENT act (reset on joining a new one; see BandLifeService.JoinAct).
	public DepartureStage departureStage;
	public int stageEnteredYear;
	public int brewingYears;
	public StrainCause stageCause;
	public string stageAgainstPersonId; // the other side of the worst edge, when there is one
	// Observe-only counterfactual (Phase 2): the year this person WOULD have left. Measured as gone from then
	// on so a would-be departure is counted once, while the economy's lineup is left exactly as it was.
	public int observedGoneYear;

	// Phase 5 -- growth and personality drift (closed form; see MemberGrowthService).
	public float effectiveHours;
	public bool traitsBaselined;
	public float generatedEgo, generatedLoyalty, generatedTemperament, generatedReliability;

	// Parameterless ctor for save/load deserialization (System.Text.Json). The population always builds
	// members through the ctor below; this exists only so a saved member can be rehydrated field-by-field.
	public Musician() { isActive = true; }

	public Musician(string id, string first, string last, bool male, int birthYear) {
		this.personId = id;
		this.firstName = first;
		this.lastName = last;
		this.isMale = male;
		this.birthYear = birthYear;
		this.isActive = true;
	}

	public int GetAge(int currentYear) => currentYear - birthYear;

	public float GetOverallTalent() => (technicalSkill * 0.4f) + (creativity * 0.3f) + (stagePresence * 0.3f);

	public float GetDramaRisk() => (ego * 0.3f) + (ambition * 0.25f) + ((1f - loyalty) * 0.25f) + ((1f - temperament) * 0.2f);

	// Solo intent (BandMemberSimulationDirective §12-13). Success, not tenure alone, sets the urge to leave: each year
	// in a group that has had top-40 hits adds to it (Ruffin, Medley, Durham left after years of hits). The old form
	// added every year to the ties instead and passed ~2-4% of the leads who already had the fame and the spotlight.
	// Sized offline on bms2-obs-1001: of the 91 groups with 5+ top-40 hits, 17.6% then hold a member who clears
	// intent, the fame bar and the spotlight at once, against the reference set's 17.6% of 91 top-40 groups.
	public const float SoloSuccessPullPerYear = 0.05f;
	public const int SoloSuccessPullMaxYears = 6;
	public const float SoloSuccessHitsForFullPull = 2f;
	public const float SoloTiesBase = 0.15f;

	public bool WouldConsiderSoloCareer(int yearsInGroup, int groupHits) {
		if (!isLeadVocalist && stagePresence < 0.7f) return false;
		float soloUrge = ambition * 0.4f + ego * 0.3f + stagePresence * 0.2f;
		float success = Math.Min(1f, Math.Max(0, groupHits) / SoloSuccessHitsForFullPull);
		float successPull = SoloSuccessPullPerYear * Math.Min(yearsInGroup + 1, SoloSuccessPullMaxYears) * success;
		float groupTies = loyalty * 0.5f;
		return (soloUrge + successPull) > (groupTies + SoloTiesBase);
	}
}

public enum MusicianRole {
	LeadVocals, BackingVocals, LeadGuitar, RhythmGuitar, Bass, Drums, Piano, Organ, 
	Saxophone, Trumpet, Harmonica, Violin, Percussion, MultiInstrumentalist
}
