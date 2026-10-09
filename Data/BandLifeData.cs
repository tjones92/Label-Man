using System;
using System.Collections.Generic;

// ============================================================================================
// BAND-MEMBER SIMULATION DATA (SimTools/BandMemberSimulationDirective.md)
//
// Plain serializable records. Everything here is sparse: an act carries no relations, alumni or
// partnerships until something actually happens to it, so a quiet band costs nothing and a run with
// the feature off allocates nothing.
// ============================================================================================

/// <summary>Why two people in an act are pulling apart. Prose and telemetry read the mix; the departure
/// stage reads the dominant one. Money and credit share one cause because, once co-writing exists, they read
/// the same ledger (directive §2.10).</summary>
public enum StrainCause { CreditAndMoney, Spotlight, Direction, Reliability, Romance, Burnout, Outsider }

/// <summary>How far a member's grievance has gone. Each stage is visible: the member complains, then states a
/// demand, then goes (directive §4.6).</summary>
public enum DepartureStage { Content, Brewing, Ultimatum }

/// <summary>What a departure actually was. Busted onward are the life exits (directive §16); new values go at the
/// end, since saves and the annual CSV carry these by position.</summary>
public enum DepartureKind {
	None, SoloCareer, Acrimony, Fired, Service, Death, LifeEvent, StudioOnly, Dissolution,
	Busted, WalkedOut, Breakdown, Injured, DayJob, Family, School, Church, SessionWork
}

/// <summary>Where a person's life stands, independent of which act they are in.
/// Drafted, Jailed and Injured are temporary absences: <c>lifeStateUntilYear</c> is the year the person comes back.</summary>
public enum MemberLifeState { Active, Drafted, StudioOnly, Retired, Deceased, Jailed, Injured }

/// <summary>What kind of lineup an act is. Derived, never stored (directive §4.3): it changes what a
/// departure MEANS without adding a field anyone has to keep in sync.</summary>
public enum LineupConstitution { Band, LeaderAndSidemen, NameOwned, Duo, Solo }

/// <summary>Which life channel a death came through (directive §4.8.2). Telemetry and prose read it.</summary>
public enum DeathChannel { None, Travel, Illness, Misadventure, Substance }

/// <summary>
/// One edge between two people in the same act. Allocated on first strain and dropped when both strain and
/// rivalry fall below epsilon. <c>rivalry</c> is productive creative tension and is NOT strain: the band's
/// best records and its eventual split come from the same cause, but they are different numbers.
/// </summary>
[Serializable]
public sealed class MemberRelation {
	public string personA, personB;   // ordinal order, A < B
	public float strain;              // 0 fine .. 1 one of them is leaving
	public float rivalry;             // productive creative tension -- NOT strain
	public float[] causeWeight = new float[7]; // per StrainCause, relaxed with strain
	public bool secret;               // an affair the wronged party hasn't discovered (directive §4.9)
	public string secretAgainstPersonId; // whose partner it was
	public int lastEventYear;

	public StrainCause DominantCause {
		get {
			int best = 0;
			if (causeWeight == null) return StrainCause.Direction;
			for (int i = 1; i < causeWeight.Length; i++) if (causeWeight[i] > causeWeight[best]) best = i;
			return (StrainCause)best;
		}
	}

	public bool Involves(string personId) => personA == personId || personB == personId;
	public string Other(string personId) => personA == personId ? personB : personA;
}

/// <summary>The old act's memory of one stint. The Musician object itself moves on (directive §2.20): a
/// person lives in exactly one place, so a past membership is a frozen record, never a second live copy.</summary>
[Serializable]
public sealed class AlumniRecord {
	public string personId;
	public string name;
	public MusicianRole role;
	public int joinedYear;
	public int leftYear;
	public string reason;
	public DepartureKind departureKind;
	public bool wasLeadVocalist;
	public bool wasWriter;
}

/// <summary>A sparse partner reference (directive §4.9). The partner may be a Musician (in this act or
/// another) or a name only. Allocated only when an event needs one. Gender-neutral: prose reads the record.</summary>
[Serializable]
public sealed class MusicianPartner {
	public string name;
	public string personId;     // null unless the partner is a musician in the world
	public bool isMale;
	public int sinceYear;
	public PartnerState state;
	public bool sitsInOnSessions; // the partner-as-outsider channel
}

public enum PartnerState { Seeing, Married, Separated, Divorced }

/// <summary>Two writers in one act and how they have written together. A <c>pact</c> is the Lennon-McCartney
/// convention: both are credited equally on anything either one writes (directive §4.14 layer 1).</summary>
[Serializable]
public sealed class WritingPartnership {
	public string personA, personB;  // ordinal order
	public int coCredits;            // songs credited to both
	public int coCreditsAtYearStart; // rivalry reads this year's delta
	public bool pact;
	public int pactYear;
	public bool pactConcededByLabel; // the player conceded it in the Band Room
}

/// <summary>A person between acts (directive §4.1). Only people with a career to continue enter the pool.</summary>
[Serializable]
public sealed class PooledPerson {
	public Musician person;
	public string lastArtistId;
	public string lastStageName;
	public Genre lastGenre;
	public string homeRegion;
	public int sinceYear;
	public DepartureKind leftAs;
}
