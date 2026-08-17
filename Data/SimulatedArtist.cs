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
	public ArtistCohort cohort = ArtistCohort.InitialLegacy;
	// Participation in the enabled first-contract labor market is intentionally
	// distinct from lifecycle, career, and ownership state.
	public ProspectMarketStatus prospectMarketStatus = ProspectMarketStatus.NotProspect;
	public ProspectMarketStatus prospectMarketStatusBeforeContract = ProspectMarketStatus.NotProspect;
	public int prospectSeekingWeeks;
	public int prospectLatentWeeks;
	public int prospectMarketSpellCount;
	public Genre formationPrimaryGenre;
	public Genre formationSecondaryGenre;

	public float vocalPower;
	public float musicianship;
	public float songwritingAbility;
	public float livePerformance;
	public float studioPerformance;
	public float groupCohesion;

	public CareerState careerState = CareerState.Unsigned;
	public string labelId;
	public bool isPlayerOwned;
	public int signedYear;
	public bool isActive = true;
	public string disbandReason;
	public ArtistLifecycleStatus lifecycleStatus = ArtistLifecycleStatus.Active;
	public ArtistDropReason lastDropReason = ArtistDropReason.Voluntary;
	public int lastPerformanceDropWeek = -1;
	public int performanceDropCount;
	public bool usesRepeatPerformanceRecovery;
	public int weeksContinuouslyUnowned;

	// These counters intentionally coexist with lifetime career counters.  They
	// are reset only by a free-agent signing, never by a same-label renewal.
	public int contractSequence;
	public int contractStartWeek = -1;
	public int contractTop40Hits;
	public int contractConsecutiveFlops;
	public int contractCompletedChartRuns;
	// Evidence a label can actually see short of a national hit. A first-contract act
	// reaches the Top 40 on 1.87% of its releases, so a probation window keyed only on
	// Top 40 is a bar 96% of signings cannot clear; charting anywhere on the Hot 100
	// (8.3%) and breaking out in a regional market (11.9%) are the observable results
	// that historically kept an act employed and carried a new sound to national notice.
	public int contractChartedRecords;
	public int contractRegionalBreakouts;
	public CareerState careerStateBeforeDrop = CareerState.Unsigned;
	public CareerState contractEntryCareerState = CareerState.Unsigned;
	public bool contractUsesExperiencedComebackPolicy;
	// Captured before a performance departure mutates careerState to Dropped, so
	// event telemetry retains the predicate that actually authorized the exit.
	public ArtistPerformanceEvaluationMode lastPerformanceEvaluationMode = ArtistPerformanceEvaluationMode.NormalCareer;
	public int lastRequiredPerformanceCompletedRuns;
	public int lastRequiredPerformanceConsecutiveFlops;
	public bool lastContractProbationPending;
	// A probation window is a minimum runway, not a coin flip. At the measured
	// first-contract Top 40 rate a two-release window terminated 96% of first
	// contracts and ended 90% of all careers, which is why no runtime-formed artist
	// reached Star in a decade. Four releases with no commercial evidence of any kind
	// is a genuinely failed act; the window restarts whenever evidence appears
	// (see CompleteChartRun), so one regional hit buys another four sides rather than
	// permanent immunity. The comeback window stays one release longer than the
	// first-contract window, preserving the authored ordering.
	public const int FirstContractFlopThreshold = 4;
	public const int ExperiencedComebackFlopThreshold = 5;
	// Two regional breakouts are the "a few regional hits brought the sound to
	// national attention" route onto the ladder, parallel to a single national Top 40.
	public const int RegionalBreakoutPromotionThreshold = 2;

	public float momentum;
	public float reputation;
	public float criticalAcclaim;

	public int totalReleases;
	public int charted;
	public int top40Hits;
	public int top10Hits;
	public int numberOnes;
	public int weeksAtNumberOne;
	public int regionalBreakouts;
	public int consecutiveHits;
	public int consecutiveFlops;
	public int totalUnitsSold;

	public int weeksSinceLastRelease = 999;
	public List<string> releaseHistory = new List<string>();
	public List<string> releasedSingleIds = new List<string>();

	public float royaltyRate;
	public float unrecoupedAdvance;
	public float totalRoyaltyEarnings;
	public int contractExpiresYear;
	public int contractLength;
	// Year granularity was tolerable at four-to-seven-year terms and is not at one-to-two:
	// a term signed in November otherwise ran two months. The week is authoritative when
	// it is set; contractExpiresYear survives for display and for legacy paths.
	public int contractExpiresWeek = -1;
	// A 1960s first deal was commonly written in sides, not in years — the Beatles' was
	// six songs across three singles in year one. Whichever obligation matures first ends
	// the term, so a prolific act works its deal out early and a slow one runs the clock.
	public int contractReleases;
	public int contractSinglesObligation;

	public List<string> careerEvents = new List<string>();

	// Career-arc state. Null unless artist evolution is observing or enabled, so the
	// default path allocates nothing extra across a 22.5k-artist registry.
	public ArtistEvolutionProfile evolution;

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

	public void UpdateAfterChartRun(int peakPosition, int weeksOnChart, int unitsSold, bool creditCurrentContract = true,
		int regionalBreakoutMarkets = 0) {
		if (peakPosition > 0 && peakPosition <= 100) RegisterChartEntry();
		if (peakPosition > 0 && peakPosition <= 40) RegisterTop40Hit(creditCurrentContract);
		if (peakPosition > 0 && peakPosition <= 10) RegisterTop10Hit();
		if (peakPosition == 1) RegisterNumberOne();
		CompleteChartRun(peakPosition, weeksOnChart, unitsSold, creditCurrentContract, regionalBreakoutMarkets);
	}

	public void RegisterChartEntry() {
		charted++;
		UpdateCareerState();
	}

	public void RegisterTop40Hit(bool creditCurrentContract = true) {
		top40Hits++;
		consecutiveHits++;
		consecutiveFlops = 0;
		momentum = Mathf.Clamp(momentum + 0.02f, 0f, 1f);
		reputation = Mathf.Clamp(reputation + 0.01f, 0f, 1f);
		if (ArtistPopulationLifecycle.Enabled && IsContractEvaluationPending() && creditCurrentContract) {
			contractTop40Hits++;
			contractConsecutiveFlops = 0;
		}
		UpdateCareerState();
	}

	public void RegisterTop10Hit() {
		top10Hits++;
		momentum = Mathf.Clamp(momentum + 0.10f, 0f, 1f);
		reputation = Mathf.Clamp(reputation + 0.02f, 0f, 1f);
		UpdateCareerState();
	}

	public void RegisterNumberOne() {
		numberOnes++;
		momentum = Mathf.Clamp(momentum + 0.18f, 0f, 1f);
		UpdateCareerState();
	}

	public void CompleteChartRun(int peakPosition, int weeksOnChart, int unitsSold, bool creditCurrentContract = true,
		int regionalBreakoutMarkets = 0) {
		totalUnitsSold += unitsSold;
		bool charted = peakPosition > 0 && peakPosition <= 100;
		bool brokeOutRegionally = regionalBreakoutMarkets > 0;
		if (brokeOutRegionally) regionalBreakouts++;
		// The lifetime streak keeps its authored peak-60 definition because the normal
		// career path in AILabel.ShouldDropArtist is calibrated against it. The contract
		// streak asks the narrower question probation actually turns on: did this record
		// show the label anything at all?
		if (peakPosition == 0 || peakPosition > 60) {
			consecutiveFlops++;
			consecutiveHits = 0;
		}
		if (ArtistPopulationLifecycle.Enabled && IsContractEvaluationPending() && creditCurrentContract) {
			if (charted) contractChartedRecords++;
			if (brokeOutRegionally) contractRegionalBreakouts++;
			if (charted || brokeOutRegionally) contractConsecutiveFlops = 0;
			else contractConsecutiveFlops++;
			contractCompletedChartRuns++;
		}
		if (peakPosition > 40) {
			float penalty = peakPosition <= 60 ? -0.05f : peakPosition <= 100 ? -0.10f : -0.15f;
			momentum = Mathf.Clamp(momentum + penalty, 0f, 1f);
		}
		reputation = Mathf.Clamp(reputation - 0.005f, 0f, 1f);
		UpdateCareerState();
	}

	private void UpdateCareerState() {
		CareerState priorState = careerState;
		CareerState nextState;
		if (ArtistPopulationLifecycle.Enabled && ShouldDepartForCurrentContractPerformance()) {
			nextState = CareerState.Dropped;
		} else nextState = careerState switch {
			CareerState.Unsigned => careerState,
			CareerState.NewSigning when HasBreakthroughEvidence() => CareerState.Rising,
			CareerState.NewSigning when !ArtistPopulationLifecycle.Enabled && consecutiveFlops >= 2 => CareerState.Dropped,
			CareerState.Rising when top10Hits >= 2 || top40Hits >= 3 => CareerState.Established,
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
		// Pending contracts may leave only through their own current-contract
		// evidence. Normal careers retain the established state-transition rules.
		if (ArtistPopulationLifecycle.Enabled && IsContractPerformanceProbationPending() &&
			nextState == CareerState.Dropped && !ShouldDepartForCurrentContractPerformance()) nextState = priorState;
		if (nextState == CareerState.Dropped && priorState != CareerState.Dropped) {
			if (ArtistPopulationLifecycle.Enabled) {
				lastPerformanceEvaluationMode = GetPerformanceEvaluationMode();
				lastRequiredPerformanceCompletedRuns = RequiredPerformanceCompletedRuns;
				lastRequiredPerformanceConsecutiveFlops = RequiredPerformanceConsecutiveFlops;
				lastContractProbationPending = IsContractPerformanceProbationPending();
			}
			careerStateBeforeDrop = priorState;
		}
		careerState = nextState;
	}

	/// <summary>
	/// The first rung of the ladder. Every rung above it is a national chart outcome,
	/// and the chart applies absolute unit bars to a distribution whose level is
	/// genre-scaled, so a rung keyed only on Top 40 is unreachable for any genre whose
	/// records launch small — an emergent genre signs nothing but new acts and can
	/// therefore never grow one. A regional breakout is ordered the same way across
	/// genres (r = 0.70 against Top 40 rate) without being an absolute national bar:
	/// it separates Soul from Sunshine Pop by 2.4x where Top 40 separates them by 12.6x.
	/// </summary>
	public bool HasBreakthroughEvidence() => ArtistPopulationLifecycle.Enabled
		? contractTop40Hits >= 1 || contractRegionalBreakouts >= RegionalBreakoutPromotionThreshold
		: top40Hits >= 1;

	public bool IsExperiencedComebackContract() => contractUsesExperiencedComebackPolicy;
	public bool IsExperiencedComebackEvaluationPending() => IsExperiencedComebackContract() &&
		contractTop40Hits == 0 && careerState != CareerState.Dropped;
	public ArtistPerformanceEvaluationMode GetPerformanceEvaluationMode() {
		if (IsExperiencedComebackEvaluationPending()) return ArtistPerformanceEvaluationMode.ExperiencedComebackProbation;
		if (careerState == CareerState.NewSigning && !IsExperiencedComebackContract()) return ArtistPerformanceEvaluationMode.FirstContractProbation;
		return ArtistPerformanceEvaluationMode.NormalCareer;
	}
	public bool IsContractPerformanceProbationPending() =>
		GetPerformanceEvaluationMode() != ArtistPerformanceEvaluationMode.NormalCareer;
	private bool IsContractEvaluationPending() => IsContractPerformanceProbationPending();
	public int RequiredPerformanceCompletedRuns => GetPerformanceEvaluationMode() switch {
		ArtistPerformanceEvaluationMode.FirstContractProbation => FirstContractFlopThreshold,
		ArtistPerformanceEvaluationMode.ExperiencedComebackProbation => ExperiencedComebackFlopThreshold,
		_ => 0
	};
	public int RequiredPerformanceConsecutiveFlops => RequiredPerformanceCompletedRuns;
	/// <summary>
	/// An act that has paid its advance and production costs back is earning for the
	/// label, and a label does not drop a paying act for chart position. This is a
	/// standing exemption rather than window evidence because recoupment is a balance,
	/// not an event.
	/// <para>
	/// Both clauses are load-bearing. unrecoupedAdvance is reset to the new advance at
	/// every signing and charged again for each production, so it is a per-contract
	/// balance; totalRoyaltyEarnings only moves once that balance is clear, so requiring
	/// it prevents an unset or not-yet-charged balance from reading as profitability.
	/// </para>
	/// </summary>
	public bool HasRecoupedCurrentContract() => unrecoupedAdvance <= 0f && totalRoyaltyEarnings > 0f;
	public bool ShouldDepartForCurrentContractPerformance() => ArtistPopulationLifecycle.Enabled &&
		IsContractPerformanceProbationPending() && contractTop40Hits == 0 &&
		!HasRecoupedCurrentContract() &&
		contractCompletedChartRuns >= RequiredPerformanceCompletedRuns &&
		contractConsecutiveFlops >= RequiredPerformanceConsecutiveFlops;

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

public enum ArtistLifecycleStatus { Active, Inactive, Retired, Disbanded }
public enum ArtistPerformanceEvaluationMode { FirstContractProbation, ExperiencedComebackProbation, NormalCareer }
public enum ArtistDropReason { Performance, PerformanceExhaustion, ContractExpired, LabelClosure, Financial, Voluntary, LifecycleReconciliation }
public enum ArtistCohort { InitialLegacy, EnabledInitialReserve, RuntimeFormation }
public enum ProspectMarketStatus { NotProspect, Latent, Seeking }
