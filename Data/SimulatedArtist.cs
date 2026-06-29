using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[Serializable]
public class SimulatedArtist {
	public string artistId;
	public string stageName;
	public ArtistType type;
	public List<Musician> members = new List<Musician>();

	public Genre primaryGenre;
	public Genre secondaryGenre;
	public string homeRegion;
	public int formedYear;

	public float vocalPower;
	public float musicianship;
	public float songwritingAbility;
	public float livePerformance;
	public float studioPerformance;
	public float groupCohesion;

	public CareerState careerState = CareerState.Unsigned;
	public string labelId;
	public int signedYear;
	public bool isActive = true;
	public string disbandReason;

	public float momentum;
	public float reputation;
	public float criticalAcclaim;

	public int totalReleases;
	public int charted;
	public int top40Hits;
	public int top10Hits;
	public int numberOnes;
	public int weeksAtNumberOne;
	public int consecutiveHits;
	public int consecutiveFlops;
	public int totalUnitsSold;

	public int weeksSinceLastRelease = 999;
	public List<string> releaseHistory = new List<string>();

	public float royaltyRate;
	public float unrecoupedAdvance;
	public int contractExpiresYear;
	public int contractLength;

	public List<string> careerEvents = new List<string>();

	public void RecalculateStats() {
		if (members.Count == 0) return;
		var activeMembers = members.Where(m => m.isActive).ToList();
		if (activeMembers.Count == 0) { isActive = false; return; }

		var leadVocalists = activeMembers.Where(m => m.isLeadVocalist).ToList();
		if (leadVocalists.Count > 0) {
			vocalPower = leadVocalists.Max(m => m.technicalSkill * 0.6f + m.stagePresence * 0.4f);
		} else {
			vocalPower = activeMembers.Max(m => m.technicalSkill) * 0.7f;
		}

		musicianship = activeMembers.Average(m => m.technicalSkill);

		var writers = activeMembers.Where(m => m.isPrimaryWriter).ToList();
		if (writers.Count > 0) songwritingAbility = writers.Average(m => m.creativity);
		else songwritingAbility = activeMembers.Max(m => m.creativity) * 0.8f;

		float avgPresence = activeMembers.Average(m => m.stagePresence);
		float bandTightness = 1f - activeMembers.Average(m => Mathf.Abs(m.technicalSkill - musicianship));
		livePerformance = avgPresence * 0.6f + bandTightness * 0.4f;

		studioPerformance = activeMembers.Average(m => m.technicalSkill * 0.4f + m.studioEfficiency * 0.4f + m.reliability * 0.2f);

		float avgEgo = activeMembers.Average(m => m.ego);
		float avgLoyalty = activeMembers.Average(m => m.loyalty);
		float avgTemperament = activeMembers.Average(m => m.temperament);
		groupCohesion = (1f - avgEgo) * 0.35f + avgLoyalty * 0.35f + avgTemperament * 0.3f;
	}

	public float CalculateBaseQuality() {
		float talent = (vocalPower * 0.3f) + (musicianship * 0.25f) + (songwritingAbility * 0.35f) + (studioPerformance * 0.1f);
		float cohesionBonus = groupCohesion * 0.15f;
		return Mathf.Clamp(talent + cohesionBonus, 0f, 1f);
	}

	public float CalculateRecordQuality() {
		float baseQuality = CalculateBaseQuality();
		float varianceRange = (1f - groupCohesion) * 0.2f;
		float variance = (float)GD.RandRange(-varianceRange, varianceRange);
		float luck = (float)GD.RandRange(-0.08f, 0.08f);
		return Mathf.Clamp(baseQuality + variance + luck, 0f, 1f);
	}

	public void UpdateAfterChartRun(int peakPosition, int weeksOnChart, int unitsSold) {
		totalUnitsSold += unitsSold;
		if (peakPosition > 0 && peakPosition <= 100) charted++;

		bool wasHit = peakPosition > 0 && peakPosition <= 40;
		bool wasFlop = peakPosition == 0 || peakPosition > 60;

		if (peakPosition == 1) numberOnes++;
		if (peakPosition > 0 && peakPosition <= 10) top10Hits++;
		if (wasHit) { top40Hits++; consecutiveHits++; consecutiveFlops = 0; }
		else if (wasFlop) { consecutiveFlops++; consecutiveHits = 0; }

		float momentumDelta = peakPosition switch {
			1 => 0.30f, <= 5 => 0.20f, <= 10 => 0.12f, <= 20 => 0.06f, <= 40 => 0.02f,
			<= 60 => -0.05f, <= 100 => -0.10f, _ => -0.15f
		};
		momentum = Mathf.Clamp(momentum + momentumDelta, 0f, 1f);

		if (peakPosition <= 10) reputation = Mathf.Clamp(reputation + 0.03f, 0f, 1f);
		else if (peakPosition <= 40) reputation = Mathf.Clamp(reputation + 0.01f, 0f, 1f);
		reputation = Mathf.Clamp(reputation - 0.005f, 0f, 1f);

		UpdateCareerState();
	}

	private void UpdateCareerState() {
		careerState = careerState switch {
			CareerState.Unsigned => careerState,
			CareerState.NewSigning when top40Hits >= 1 => CareerState.Rising,
			CareerState.NewSigning when consecutiveFlops >= 2 => CareerState.Dropped,
			CareerState.Rising when top10Hits >= 2 => CareerState.Established,
			CareerState.Rising when consecutiveFlops >= 2 => CareerState.Declining,
			CareerState.Established when consecutiveHits >= 3 && numberOnes >= 1 => CareerState.Star,
			CareerState.Established when consecutiveFlops >= 3 => CareerState.Declining,
			CareerState.Star when numberOnes >= 4 && consecutiveHits >= 4 => CareerState.Superstar,
			CareerState.Star when consecutiveFlops >= 2 => CareerState.Established,
			CareerState.Superstar when consecutiveFlops >= 3 => CareerState.Star,
			CareerState.Declining when top40Hits > 0 && consecutiveHits >= 1 => CareerState.Established,
			CareerState.Declining when consecutiveFlops >= 3 => CareerState.Dropped,
			_ => careerState
		};
	}

	public float GetNewReleaseAwarenessBonus() {
		return (momentum * 0.5f) + (reputation * 0.3f) + (careerState switch {
			CareerState.Superstar => 0.25f, CareerState.Star => 0.15f, CareerState.Established => 0.08f,
			CareerState.Rising => 0.04f, _ => 0f
		});
	}

	public float GetCareerPriority() {
		return careerState switch {
			CareerState.Superstar => 1.0f, CareerState.Star => 0.85f, CareerState.Established => 0.7f,
			CareerState.Rising => 0.6f, CareerState.NewSigning => 0.4f, CareerState.Declining => 0.25f, _ => 0.1f
		};
	}

	public void AddMember(Musician musician, int year, bool isFounder = false) {
		musician.isFoundingMember = isFounder;
		musician.joinedYear = year;
		musician.isActive = true;
		members.Add(musician);
		RecalculateStats();
	}

	public void RemoveMember(Musician musician, string reason, int year) {
		musician.isActive = false;
		musician.reasonLeft = reason;
		careerEvents.Add($"{year}: {musician.FullName} left ({reason})");
		RecalculateStats();
	}

	public List<Musician> GetActiveMembers() => members.Where(m => m.isActive).ToList();
	public Musician GetLeadSinger() => members.FirstOrDefault(m => m.isActive && m.isLeadVocalist);
	public Musician GetMainWriter() => members.FirstOrDefault(m => m.isActive && m.isPrimaryWriter);
}

public enum CareerState {
	Unsigned, NewSigning, Rising, Established, Star, Superstar, Declining, Dropped, Disbanded, Retired
}
