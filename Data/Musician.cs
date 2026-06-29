using System;
using Godot;

// Kept as plain C# class since it's generated at runtime
[Serializable]
public class Musician {
	public string personId;
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

	public bool WouldConsiderSoloCareer(int yearsInGroup, int groupHits) {
		if (!isLeadVocalist && stagePresence < 0.7f) return false;
		float soloUrge = ambition * 0.4f + ego * 0.3f + stagePresence * 0.2f;
		float groupTies = loyalty * 0.5f + (yearsInGroup * 0.05f);
		float successFactor = groupHits > 5 ? 0.2f : 0f;
		return (soloUrge + successFactor) > (groupTies + 0.3f);
	}
}

public enum MusicianRole {
	LeadVocals, BackingVocals, LeadGuitar, RhythmGuitar, Bass, Drums, Piano, Organ, 
	Saxophone, Trumpet, Harmonica, Violin, Percussion, MultiInstrumentalist
}
