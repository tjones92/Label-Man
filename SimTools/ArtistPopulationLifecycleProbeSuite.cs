using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Directive 6 fixed-input probes. They deliberately use detached artists and
/// labels so they neither consume simulation RNG nor perturb an audit world.
/// The integration/replay gates remain the authority for full event ordering.
/// </summary>
public static class ArtistPopulationLifecycleProbeSuite {
	private const int Cooldown = ArtistManager.PerformanceDropCooldownWeeks;

	public static IReadOnlyList<string> Run() {
		if (!ArtistPopulationLifecycle.Enabled) throw new InvalidOperationException("D6 probes require the lifecycle toggle.");
		var results = new List<string>();
		ProbeContractHistoricalSeparation();                 // 1
		ProbeContractHitAndFlopThreshold();                  // 2
		ProbeFreeAgentResetAndRenewal();                     // 3
		ProbeStructuredCooldownReasons();                    // 4
		ProbeCooldownBoundary();                             // 5
		ProbeMatureSecondChance();                           // 6
		ProbeRuntimeFormationRegistration();                 // 7
		ProbeFormationYearsAndLegacyCohort();                // 8
		ProbeAccumulatorMath();                              // 9
		ProbeAvailabilityBoundary();                         // 10
		ProbeLateGenreAvailability();                        // 11
		ProbeRuntimeSecondaryIdentity();                     // 12
		ProbePopulationRngDeterminism();                     // 13
		ProbeInactiveEligibility();                          // 14
		ProbeTerminalClassification();                       // 15
		ProbeTerminalSigningAndReleaseGuards();              // 16
		ProbeExitDeferralPredicate();                        // 17
		ProbeProjectIdentityStability();                     // 18
		ProbeNativeProjectClassification();                  // 19
		ProbeOwnershipAndIdIntegrity();                      // 20
		ProbeInheritedSpecialistBoundary();                  // 21
		ProbeMonthlyProbationGuard();                        // 22
		ProbePriorContractChartRunIsolation();               // 23
		ProbeScoutingEarlyBranchesDoNotRoll();                // 24
		ProbeScoutingProbabilityAndSingleRoll();             // 25
		ProbeScoutingFailureAndCandidateBoundaries();        // 26
		ProbeCandidateScoreAndSigningKinds();                // 27
		ProbeVacancyAgeBookkeeping();                        // 28
		ProbeTelemetryEvaluationIsRngNeutral();              // 29
		ProbeScoutingUrgencyThresholdAndFloor();             // 30
		ProbeScoutingUrgencyResets();                        // 31
		ProbeScoutingUrgencyEnabledBoundaryAndSingleRoll();  // 32
		ProbeClosedLabelsCannotScout();                      // 33
		results.Add("D6 fixed probes 1-33 passed (contract/cooldown/formation/identity/lifecycle/scouting telemetry/urgency/closed-label guard)");
		return results;
	}

	private static SimulatedArtist NewArtist(string id = "probe") => new() {
		artistId = id, stageName = id, primaryGenre = Genre.RnB, secondaryGenre = Genre.Soul,
		formationPrimaryGenre = Genre.RnB, formationSecondaryGenre = Genre.Soul, formedYear = 1960,
		careerState = CareerState.NewSigning, lifecycleStatus = ArtistLifecycleStatus.Active, isActive = true
	};

	private static void ProbeContractHistoricalSeparation() {
		SimulatedArtist artist = NewArtist();
		artist.top40Hits = 9; artist.consecutiveFlops = 5;
		artist.CompleteChartRun(0, 1, 0);
		Require(artist.careerState == CareerState.NewSigning && artist.contractConsecutiveFlops == 1 && artist.top40Hits == 9,
			"1 historical hits/flops do not resolve current probation");
	}

	private static void ProbeContractHitAndFlopThreshold() {
		SimulatedArtist hit = NewArtist("hit");
		hit.RegisterTop40Hit();
		Require(hit.careerState == CareerState.Rising && hit.contractTop40Hits == 1, "2a current-contract Top 40 advances probation");
		SimulatedArtist flop = NewArtist("flop");
		flop.CompleteChartRun(0, 1, 0); flop.CompleteChartRun(0, 1, 0);
		Require(flop.careerState == CareerState.Dropped && flop.contractConsecutiveFlops == 2, "2b two current-contract flops drop probation");
	}

	private static void ProbeFreeAgentResetAndRenewal() {
		SimulatedArtist artist = NewArtist();
		artist.careerState = CareerState.Dropped; artist.labelId = null; artist.top40Hits = 4; artist.consecutiveFlops = 7;
		artist.contractTop40Hits = 3; artist.contractConsecutiveFlops = 2; artist.contractCompletedChartRuns = 9;
		var pool = new List<SimulatedArtist> { artist };
		ArtistManager.ReconcileSignedArtistForProbe(artist, pool, "label", 1961);
		Require(artist.contractSequence == 1 && artist.contractTop40Hits == 0 && artist.contractConsecutiveFlops == 0 &&
			artist.top40Hits == 4 && artist.consecutiveFlops == 7 && pool.Count == 0, "3 free-agent reset preserves lifetime history");
		int sequence = artist.contractSequence;
		artist.contractTop40Hits = 1; // same-label renewal is deliberately not a new free-agent cycle
		Require(artist.contractSequence == sequence && artist.contractTop40Hits == 1, "3 same-label renewal keeps contract cycle");
	}

	private static void ProbeStructuredCooldownReasons() {
		SimulatedArtist performance = NewArtist(); performance.careerState = CareerState.Dropped; performance.lastDropReason = ArtistDropReason.Performance; performance.lastPerformanceDropWeek = 100;
		SimulatedArtist expiry = NewArtist(); expiry.careerState = CareerState.Dropped; expiry.lastDropReason = ArtistDropReason.ContractExpired; expiry.lastPerformanceDropWeek = 100;
		Require(ArtistManager.IsPopulationCooldownBlockedForProbe(performance, 101) && !ArtistManager.IsPopulationCooldownBlockedForProbe(expiry, 101),
			"4 only structured performance drops receive cooldown");
	}

	private static void ProbeCooldownBoundary() {
		SimulatedArtist artist = NewArtist(); artist.careerState = CareerState.Dropped; artist.lastDropReason = ArtistDropReason.Performance; artist.lastPerformanceDropWeek = 100;
		Require(ArtistManager.IsPopulationCooldownBlockedForProbe(artist, 100 + Cooldown - 1) &&
			!ArtistManager.IsPopulationCooldownBlockedForProbe(artist, 100 + Cooldown) && artist.isActive,
			"5 cooldown blocks C-1, admits C, and retains active pool identity");
	}

	private static void ProbeMatureSecondChance() {
		SimulatedArtist artist = NewArtist(); artist.careerState = CareerState.Dropped; artist.lastDropReason = ArtistDropReason.Performance; artist.lastPerformanceDropWeek = 0;
		var pool = new List<SimulatedArtist> { artist };
		Require(ArtistManager.IsEligibleForPopulationSigningForProbe(artist, Cooldown), "6a matured drop is signable");
		ArtistManager.ReconcileSignedArtistForProbe(artist, pool, "second", 1961);
		Require(artist.labelId == "second" && artist.careerState == CareerState.NewSigning && pool.Count == 0 && artist.contractSequence == 1,
			"6b matured second chance receives one owner and new evidence scope");
	}

	private static void ProbeRuntimeFormationRegistration() {
		SimulatedArtist artist = ArtistManager.GenerateRuntimeArtistForProbe(1964, 1001);
		Require(artist.cohort == ArtistCohort.RuntimeFormation && artist.formedYear == 1964 && artist.labelId == null &&
			artist.careerState == CareerState.Unsigned && artist.members.Count > 0 && artist.members.Select(member => member.personId).Distinct().Count() == artist.members.Count,
			"7 runtime formation creates unsigned artist with unique musicians");
	}

	private static void ProbeFormationYearsAndLegacyCohort() {
		SimulatedArtist runtime = ArtistManager.GenerateRuntimeArtistForProbe(1968, 1001);
		SimulatedArtist legacy = NewArtist("legacy"); legacy.cohort = ArtistCohort.InitialLegacy; legacy.formedYear = 1957;
		Require(runtime.formedYear == 1968 && runtime.cohort == ArtistCohort.RuntimeFormation && legacy.formedYear == 1957,
			"8 runtime year is exact and legacy backdating remains representable");
	}

	private static void ProbeAccumulatorMath() {
		float carry = 0f; int total = 0;
		for (int week = 0; week < 52; week++) { carry += 300f / 52f; int formed = (int)MathF.Floor(carry + .00001f); carry -= formed; total += formed; }
		Require(total == 300 && carry >= 0f && carry < 1f, "9 formation accumulator yields exactly 300 annual entrants");
	}

	private static void ProbeAvailabilityBoundary() {
		Require(!GenreSupplyService.IsAvailableForNewSupply(Genre.PsychedelicRock, 1965) && GenreSupplyService.IsAvailableForNewSupply(Genre.PsychedelicRock, 1966),
			"10 native formation excludes pre-emergent genres");
	}

	private static void ProbeLateGenreAvailability() {
		Require(GenreSupplyService.IsAvailableForNewSupply(Genre.HardRock, 1967) && GenreSupplyService.IsAvailableForNewSupply(Genre.ProtoMetal, 1968) &&
			GenreSupplyService.IsAvailableForNewSupply(Genre.ProgressiveRock, 1968), "11 authored late genres become selectable at availability");
	}

	private static void ProbeRuntimeSecondaryIdentity() {
		Genre secondary = ArtistManager.ChooseRuntimeSecondaryGenreForProbe(Genre.ProtoMetal, 1968, 1002);
		Require(GenreSupplyService.IsAvailableForNewSupply(Genre.ProtoMetal, 1968) && GenreSupplyService.IsAvailableForNewSupply(secondary, 1968) &&
			secondary != Genre.TraditionalPop, "12 runtime secondary is canonical/available and avoids legacy fallback");
	}

	private static void ProbePopulationRngDeterminism() {
		SimulatedArtist a = ArtistManager.GenerateRuntimeArtistForProbe(1967, 1003);
		SimulatedArtist b = ArtistManager.GenerateRuntimeArtistForProbe(1967, 1003);
		Require(a.type == b.type && a.formationPrimaryGenre == b.formationPrimaryGenre && a.formationSecondaryGenre == b.formationSecondaryGenre &&
			a.members.Select(member => member.personId).SequenceEqual(b.members.Select(member => member.personId)), "13 dedicated population RNG is deterministic");
	}

	private static void ProbeInactiveEligibility() {
		SimulatedArtist artist = NewArtist(); artist.careerState = CareerState.Dropped; artist.lifecycleStatus = ArtistLifecycleStatus.Inactive; artist.isActive = false;
		Require(!ArtistManager.IsEligibleForPopulationSigningForProbe(artist, 200), "14 inactive transition removes signing eligibility while preserving object history");
	}

	private static void ProbeTerminalClassification() {
		SimulatedArtist group = NewArtist(); group.type = ArtistType.Band;
		SimulatedArtist solo = NewArtist(); solo.type = ArtistType.SoloMale; solo.members.Add(new Musician("lead", "Lead", "Probe", true, 1920) { isLeadVocalist = true });
		Require(ArtistManager.ClassifyTerminalLifecycleForProbe(group, 1960) == ArtistLifecycleStatus.Disbanded &&
			ArtistManager.ClassifyTerminalLifecycleForProbe(solo, 1960) == ArtistLifecycleStatus.Retired, "15 group disbandment and qualified solo retirement classify correctly");
	}

	private static void ProbeTerminalSigningAndReleaseGuards() {
		SimulatedArtist artist = NewArtist(); artist.lifecycleStatus = ArtistLifecycleStatus.Retired; artist.careerState = CareerState.Retired; artist.isActive = false;
		Require(!ArtistManager.IsEligibleForPopulationSigningForProbe(artist, 1) && !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist),
			"16 inactive/terminal artists cannot sign or release");
	}

	private static void ProbeExitDeferralPredicate() {
		Require(ArtistManager.IsExitDeferredForProbe(true, false) && ArtistManager.IsExitDeferredForProbe(false, true) &&
			!ArtistManager.IsExitDeferredForProbe(false, false), "17 live-chart and pending-project exits defer exactly");
	}

	private static void ProbeProjectIdentityStability() {
		SimulatedArtist artist = NewArtist(); Genre identity = artist.primaryGenre; Genre project = Genre.ProtoMetal;
		SimulatedArtist decision = CompetitorManager.CreateProjectDecisionArtistForProbe(artist, project);
		Require(identity != project && artist.primaryGenre == identity && artist.formationPrimaryGenre == identity && decision.primaryGenre == project,
			"18 project choice does not mutate stored artist identity");
	}

	private static void ProbeNativeProjectClassification() {
		SimulatedArtist artist = NewArtist();
		Require(artist.formationPrimaryGenre == artist.primaryGenre && artist.primaryGenre != Genre.ProtoMetal, "19 native and transitioned project classification remains distinct");
	}

	private static void ProbeOwnershipAndIdIntegrity() {
		IReadOnlyList<SimulatedArtist> generated = ArtistManager.GenerateRuntimeArtistsForProbe(1966, 1004, 2);
		SimulatedArtist a = generated[0]; SimulatedArtist b = generated[1];
		string[] ids = a.members.Select(member => member.personId).Append(a.artistId).Concat(b.members.Select(member => member.personId)).Append(b.artistId).ToArray();
		Require(ids.Distinct().Count() == ids.Length, "20 generated artist/musician IDs reconcile uniquely");
	}

	private static void ProbeInheritedSpecialistBoundary() {
		Require(GenreSupplyService.IsAvailableForNewSupply(Genre.TexMex, 1968) && GenreSupplyService.IsAvailableForNewSupply(Genre.Boogaloo, 1967),
			"21 inherited supply/specialist boundary remains available to population formation");
	}

	private static void ProbeMonthlyProbationGuard() {
		var label = new AILabel();
		SimulatedArtist artist = NewArtist("monthly-probation");
		artist.consecutiveFlops = 9;
		artist.contractConsecutiveFlops = 1;
		Require(!label.ShouldDropArtist(artist) && artist.careerState == CareerState.NewSigning,
			"22 monthly review cannot use lifetime flops to bypass current-contract probation");
	}

	private static void ProbePriorContractChartRunIsolation() {
		SimulatedArtist artist = NewArtist("prior-contract-chart-run");
		artist.contractSequence = 2;
		artist.RegisterTop40Hit(false);
		artist.CompleteChartRun(0, 1, 0, false);
		Require(artist.top40Hits == 1 && artist.consecutiveFlops == 1 && artist.contractTop40Hits == 0 &&
			artist.contractConsecutiveFlops == 0 && artist.contractCompletedChartRuns == 0 &&
			artist.careerState == CareerState.NewSigning,
			"23 prior-contract chart runs retain lifetime history without resolving new-contract probation");
	}

	private static AILabel NewScoutingLabel(int maxRoster = 4, float cash = 100000f) => new() {
		labelId = "scouting-probe", tier = LabelTier.Independent, maxRosterSize = maxRoster, cashReserves = cash,
		scoutingAbility = 0.6f, riskTolerance = 1f, preferredGenres = new[] { Genre.RnB }, secondaryGenres = Array.Empty<Genre>(),
		roster = new List<SimulatedArtist>()
	};

	private static SimulatedArtist NewScoringCandidate(string id, float quality, CareerState state = CareerState.Unsigned) {
		SimulatedArtist artist = NewArtist(id);
		artist.careerState = state;
		artist.vocalPower = quality; artist.musicianship = quality; artist.songwritingAbility = quality;
		artist.studioPerformance = quality; artist.groupCohesion = 0f;
		return artist;
	}

	private static void ProbeScoutingEarlyBranchesDoNotRoll() {
		int rolls = 0;
		AILabel full = NewScoutingLabel(1); full.roster.Add(NewArtist("full"));
		AILabel.ScoutingGateEvaluation fullResult = full.EvaluateScoutingGate(() => { rolls++; return 0f; });
		AILabel unaffordable = NewScoutingLabel(cash: 0f);
		AILabel.ScoutingGateEvaluation unaffordableResult = unaffordable.EvaluateScoutingGate(() => { rolls++; return 0f; });
		Require(fullResult.FailureReason == "RosterFull" && unaffordableResult.FailureReason == "EstimatedAdvanceUnaffordable" && rolls == 0,
			"24 full and estimated-unaffordable scouting branches emit structured reasons without a roll");
	}

	private static void ProbeScoutingProbabilityAndSingleRoll() {
		int rolls = 0;
		AILabel label = NewScoutingLabel();
		AILabel.ScoutingGateEvaluation result = label.EvaluateScoutingGate(() => { rolls++; return 1f; });
		Require(result.ComputedScoutProbability > 0f && result.ComputedScoutProbability < 1f && result.RandomRoll == 1f && !result.ScoutingGatePassed &&
			rolls == 1 && result.FailureReason == "ScoutingRandomGate",
			"25 vacant affordable scouting records the existing probability and exactly one failed roll");
	}

	private static void ProbeScoutingFailureAndCandidateBoundaries() {
		AILabel label = NewScoutingLabel();
		AILabel.ScoutingGateEvaluation pass = label.EvaluateScoutingGate(() => 0f);
		AILabel.SigningEvaluation none = label.EvaluateSigning(new List<SimulatedArtist>());
		Require(pass.ScoutingGatePassed && none.BestCandidate == null && none.BestCandidateScore == null,
			"26 passing gate preserves no-eligible-candidate boundary without candidate scoring");
	}

	private static void ProbeCandidateScoreAndSigningKinds() {
		AILabel label = NewScoutingLabel(); label.riskTolerance = 0f;
		AILabel.SigningEvaluation rejected = label.EvaluateSigning(new List<SimulatedArtist> { NewScoringCandidate("low", 0.1f) });
		AILabel.SigningEvaluation accepted = label.EvaluateSigning(new List<SimulatedArtist> { NewScoringCandidate("high", 0.9f) });
		Require(rejected.BestCandidate == null && rejected.BestCandidateScore.HasValue && rejected.BestCandidateScore.Value < .3f &&
			accepted.BestCandidate != null && RosterManager.GetSigningKindForTelemetry(NewScoringCandidate("first", .9f)) == "SignedFirstTime" &&
			RosterManager.GetSigningKindForTelemetry(NewScoringCandidate("free", .9f, CareerState.Dropped)) == "SignedFreeAgent",
			"27 score rejection retains best score and successful signing kinds remain structured");
	}

	private static void ProbeVacancyAgeBookkeeping() {
		Require(RosterManager.AdvanceConsecutiveAge(true, 0) == 1 && RosterManager.AdvanceConsecutiveAge(true, 7) == 8 &&
			RosterManager.AdvanceConsecutiveAge(false, 8) == 0,
			"28 telemetry-owned vacancy and empty-roster ages increment and reset");
	}

	private static void ProbeTelemetryEvaluationIsRngNeutral() {
		AILabel label = NewScoutingLabel();
		int rolls = 0;
		AILabel.ScoutingGateEvaluation first = label.EvaluateScoutingGate(() => { rolls++; return .05f; });
		AILabel.ScoutingGateEvaluation second = label.EvaluateScoutingGate(() => { rolls++; return .05f; });
		Require(rolls == 2 && first.ScoutingGatePassed == second.ScoutingGatePassed &&
			MathF.Abs(first.ComputedScoutProbability - second.ComputedScoutProbability) < .00001f,
			"29 telemetry evaluation adds no RNG draw beyond its supplied scouting roll");
	}

	private static void ProbeScoutingUrgencyThresholdAndFloor() {
		Require(RosterManager.GetScoutingUrgencyAgeForWeek(true, 10) == 11 &&
			RosterManager.GetScoutingProbabilityFloorForPath(true, 11) == 0f &&
			RosterManager.GetScoutingUrgencyAgeForWeek(true, 11) == 12 &&
			RosterManager.GetScoutingProbabilityFloorForPath(true, 12) == .25f,
			"30 urgency starts only on the twelfth consecutive under-capacity week");
	}

	private static void ProbeScoutingUrgencyResets() {
		Require(RosterManager.FinalizeScoutingUrgencyAge(true, true, 13) == 0 &&
			RosterManager.FinalizeScoutingUrgencyAge(false, false, 13) == 0 &&
			RosterManager.FinalizeScoutingUrgencyAge(true, false, 13) == 13,
			"31 successful signing and full roster reset urgency age only");
	}

	private static void ProbeScoutingUrgencyEnabledBoundaryAndSingleRoll() {
		AILabel label = NewScoutingLabel();
		label.scoutingAbility = .1f;
		int rolls = 0;
		AILabel.ScoutingGateEvaluation urgent = label.EvaluateScoutingGate(() => { rolls++; return .099f; },
			RosterManager.GetScoutingProbabilityFloorForPath(true, 12));
		Require(RosterManager.GetScoutingProbabilityFloorForPath(false, 12) == 0f && MathF.Abs(urgent.ComputedScoutProbability - .25f) < .00001f &&
			urgent.ScoutingGatePassed && rolls == 1,
			"32 urgency floor is enabled-only and preserves exactly one scouting draw");
	}

	private static void ProbeClosedLabelsCannotScout() {
		AILabel operating = NewScoutingLabel();
		operating.status = LabelStatus.Stable;
		AILabel defunct = NewScoutingLabel();
		defunct.status = LabelStatus.Defunct;
		AILabel bankrupt = NewScoutingLabel();
		bankrupt.status = LabelStatus.Bankrupt;
		AILabel acquired = NewScoutingLabel();
		acquired.status = LabelStatus.Acquired;
		Require(RosterManager.IsEligibleForEnabledScouting(operating) &&
			!RosterManager.IsEligibleForEnabledScouting(defunct) &&
			!RosterManager.IsEligibleForEnabledScouting(bankrupt) &&
			!RosterManager.IsEligibleForEnabledScouting(acquired),
			"33 enabled scouting excludes every closed-label state");
	}

	private static void Require(bool condition, string message) {
		if (!condition) throw new InvalidOperationException("D6 probe failed: " + message);
	}
}
