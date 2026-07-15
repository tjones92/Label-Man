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
		ProbeCalendarFormationQuota();                       // 9
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
		ProbeMarketClearingServiceModes();                   // 30
		ProbeMarketClearingLaneScores();                     // 31
		ProbeMarketClearingPerformanceExhaustion();          // 32
		ProbeClosedLabelsCannotScout();                      // 33
		ProbeExperiencedComebackStateModel();                  // 34
		ProbeInitialTalentMarketBoundary();                    // 35
		ProbeOperatingRosterTarget();                           // 36
		ProbeNeverSignedInactivityBoundary();                   // 37
		ProbeBoundedScoutingDiscovery();                        // 38
		ProbeFreshPotentialNoCareerPenalty();                    // 39
		ProbeFreshPotentialThreshold();                          // 40
		ProbePerformanceContractScope();                         // 41
		ProbePerformanceTop40Clearance();                        // 42
		ProbeFirstPerformanceDeparture();                        // 43
		ProbeSecondPerformanceDepartureExhaustion();             // 44
		ProbeNonPerformanceDepartureDoesNotExhaust();            // 45
		ProbeNoThirdComebackSigning();                           // 46
		ProbeMarketClearingTelemetryFields();                    // 47
		ProbeHeadcountOnlyRecovery();                            // 48
		ProbeHeadcountRecoveryBoundaries();                      // 49
		ProbeRosterThroughputBootstrap();                        // 50
		ProbeFirstContractProbationThreshold();                  // 51
		ProbeNormalCareerAfterProbation();                       // 52
		ProbeLabelReleaseCapacityBoundary();                     // 53
		ProbeEconomicYieldDiagnosticBoundaries();                 // 54
		ProbeRotatingProspectParticipation();                     // 55
		ProbeRuntimeLabelBootstrapInitialization();               // 56
		ProbeRuntimeLabelRecoveryBoundary();                       // 57
		ProbeRuntimeSigningTransitionClassification();             // 58
		ProbeRuntimeFirstContractClassification();                 // 59
		ProbeRuntimeBootstrapCannotBurst();                        // 60
		results.Add("D6 fixed probes 1-60 passed (contract/cooldown/calendar formation/identity/lifecycle/roster normalization/discovery lanes/performance exhaustion/label release capacity/economic-yield diagnostics/prospect participation/runtime-label bootstrap)");
		return results;
	}

	private static SimulatedArtist NewArtist(string id = "probe") => new() {
		artistId = id, stageName = id, primaryGenre = Genre.RnB, secondaryGenre = Genre.Soul,
		formationPrimaryGenre = Genre.RnB, formationSecondaryGenre = Genre.Soul, formedYear = 1960,
		careerState = CareerState.NewSigning, lifecycleStatus = ArtistLifecycleStatus.Active, isActive = true
	};

	private static void ProbeEconomicYieldDiagnosticBoundaries() {
		Require(ArtistPopulationLifecycle.ShouldMaterializeInitialReserveFor(true, false) &&
			!ArtistPopulationLifecycle.ShouldMaterializeInitialReserveFor(true, true) &&
			!ArtistPopulationLifecycle.ShouldMaterializeInitialReserveFor(false, false) &&
			!ArtistPopulationLifecycle.ShouldMaterializeInitialReserveFor(false, true),
			"54 diagnostic reserve boundary preserves enabled 7,000 default, suppresses only the opt-in enabled reserve, and leaves disabled independent");
	}

	private static void ProbeRotatingProspectParticipation() {
		Require(Enum.GetValues<ArtistCohort>().Contains(ArtistCohort.EnabledInitialReserve) &&
			ArtistManager.CalculateProspectActivationCount(4, 3, 0) == 0 &&
			ArtistManager.CalculateProspectActivationCount(4, 3, 3) == 0 &&
			ArtistManager.CalculateProspectActivationCount(4, 3, 5) == 2 &&
			ArtistManager.CalculateProspectActivationCount(1, 0, 4) == 1,
			"55a reserve cohort and vacancy-minus-seeker exposure budget honor zero, exact, under-supplied, and latent-exhausted boundaries");
		SimulatedArtist first = NewArtist("first"); first.careerState = CareerState.Unsigned; first.prospectMarketStatus = ProspectMarketStatus.Latent; first.prospectMarketSpellCount = 0; first.vocalPower = .05f;
		SimulatedArtist repeat = NewArtist("repeat"); repeat.careerState = CareerState.Unsigned; repeat.prospectMarketStatus = ProspectMarketStatus.Latent; repeat.prospectMarketSpellCount = 1; repeat.vocalPower = .99f;
		SimulatedArtist second = NewArtist("second"); second.careerState = CareerState.Unsigned; second.prospectMarketStatus = ProspectMarketStatus.Latent; second.prospectMarketSpellCount = 0; second.vocalPower = .99f;
		string[] ordered = ArtistManager.OrderLatentProspects(new[] { repeat, second, first }).Select(artist => artist.artistId).ToArray();
		Require(ordered[0] != "repeat" && ordered[1] != "repeat" && ordered.SequenceEqual(ArtistManager.OrderLatentProspects(new[] { first, second, repeat }).Select(artist => artist.artistId)),
			"55b deterministic activation is quality-blind and serves never-exposed prospects before repeat spells");
		SimulatedArtist seeking = NewArtist("seeking"); seeking.careerState = CareerState.Unsigned; seeking.prospectMarketStatus = ProspectMarketStatus.Seeking; seeking.prospectSeekingWeeks = 76;
		Require(!ArtistManager.AdvanceProspectSearchWeekForProbe(seeking) && seeking.prospectMarketStatus == ProspectMarketStatus.Seeking && seeking.prospectSeekingWeeks == 77 &&
			ArtistManager.AdvanceProspectSearchWeekForProbe(seeking) && seeking.prospectMarketStatus == ProspectMarketStatus.Latent && seeking.prospectSeekingWeeks == 0 && seeking.prospectMarketSpellCount == 1 && seeking.lifecycleStatus == ArtistLifecycleStatus.Active && seeking.careerState == CareerState.Unsigned,
			"55c prospect search stays searchable through week 77 and rotates to latent at week 78 without lifecycle mutation");
		var pool = new List<SimulatedArtist> { seeking }; seeking.prospectMarketStatus = ProspectMarketStatus.Seeking;
		ArtistManager.ReconcileSignedArtistForProbe(seeking, pool, "label", 1960);
		Require(seeking.prospectMarketStatus == ProspectMarketStatus.NotProspect && seeking.prospectMarketStatusBeforeContract == ProspectMarketStatus.Seeking && !pool.Contains(seeking),
			"55d first signing atomically exits prospect participation and records the pre-contract status");
	}

	private static void ProbeRuntimeLabelBootstrapInitialization() {
		AILabel runtime = NewScoutingLabel(); runtime.maxRosterSize = 4;
		RosterManager.InitializeRuntimeRosterForProbe(runtime);
		Require(runtime.CurrentRosterSize == 0 && runtime.OperatingRosterTarget == 1 && runtime.operatingRosterTargetSource == "OneArtistBootstrap",
			"56 runtime label initialization remains empty with the one-artist bootstrap target and consumes no launch population");
	}

	private static void ProbeRuntimeLabelRecoveryBoundary() {
		var empty = RosterManager.GetTalentServiceSnapshotForProbe(0, 1, 4, 0, 0);
		Require(empty.HeadcountDeficit == 1 && empty.ServiceMode == "Recovery" && RosterManager.CanAttemptMarketClearingSigning(0, 1),
			"57 an empty runtime label enters Recovery at its next scouting boundary with exactly one operating vacancy");
	}

	private static void ProbeRuntimeSigningTransitionClassification() {
		SimulatedArtist comeback = NewArtist("runtime-comeback");
		comeback.contractSequence = 1; comeback.careerState = CareerState.Dropped; comeback.careerStateBeforeDrop = CareerState.Rising;
		comeback.lastDropReason = ArtistDropReason.Performance; comeback.performanceDropCount = 1;
		var pool = new List<SimulatedArtist> { comeback };
		ArtistManager.SigningTransition transition = ArtistManager.ReconcileSignedArtistForProbe(comeback, pool, "runtime", 1961);
		Require(transition.IsReSigning && transition.PriorContractSequence == 1 && transition.WasDroppedFreeAgent &&
			comeback.contractSequence == 2 && comeback.careerState == CareerState.Rising && comeback.IsExperiencedComebackContract() &&
			comeback.performanceDropCount == 1 && pool.Count == 0,
			"58 runtime prior-contract signing preserves comeback history and uses the authoritative repeat-signing transition");
	}

	private static void ProbeRuntimeFirstContractClassification() {
		SimulatedArtist prospect = NewArtist("runtime-prospect");
		prospect.careerState = CareerState.Unsigned; prospect.prospectMarketStatus = ProspectMarketStatus.Seeking;
		var pool = new List<SimulatedArtist> { prospect };
		ArtistManager.SigningTransition transition = ArtistManager.ReconcileSignedArtistForProbe(prospect, pool, "runtime", 1961);
		Require(!transition.IsReSigning && transition.WasFirstContractProspect && transition.PriorContractSequence == 0 &&
			prospect.contractSequence == 1 && prospect.careerState == CareerState.NewSigning && !prospect.IsExperiencedComebackContract(),
			"59 a Seeking first-contract prospect remains a first-contract probation case");
	}

	private static void ProbeRuntimeBootstrapCannotBurst() {
		AILabel runtime = NewScoutingLabel(); runtime.maxRosterSize = 7;
		RosterManager.InitializeRuntimeRosterForProbe(runtime);
		Require(runtime.OperatingRosterTarget == 1 && !RosterManager.CanAttemptMarketClearingSigning(1, runtime.OperatingRosterTarget),
			"60 one successful runtime bootstrap signing closes the only operating vacancy, preventing a birth-week bulk burst");
	}

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
		flop.CompleteChartRun(0, 1, 0);
		Require(flop.careerState == CareerState.NewSigning && flop.contractConsecutiveFlops == 1, "2b one current-contract flop retains first-contract probation");
		flop.CompleteChartRun(0, 1, 0);
		Require(flop.careerState == CareerState.Dropped && flop.contractCompletedChartRuns == 2, "2c two current-contract flops depart a first contract");
	}

	private static void ProbeMarketClearingServiceModes() {
		Require(RosterManager.GetTalentServiceModeForProbe(3, 3, 6, 3, 0) == "Normal" &&
			RosterManager.GetTalentServiceModeForProbe(2, 3, 6, 3, 0) == "Watch" &&
			RosterManager.GetTalentServiceModeForProbe(2, 3, 6, 3, 3) == "Recovery" &&
			RosterManager.GetTalentServiceModeForProbe(0, 3, 6, 0, 0) == "Recovery",
			"30 service modes preserve normal/watch and recover deep or persistent deficits");
	}

	private static void ProbeMarketClearingLaneScores() {
		AILabel label = NewScoutingLabel(); label.scoutingAbility = .5f; label.riskTolerance = 0f; label.cashReserves = 100000f;
		SimulatedArtist fresh = NewArtist("fresh"); fresh.reputation = 0f; fresh.momentum = 0f;
		SimulatedArtist experienced = NewArtist("experienced"); experienced.careerState = CareerState.Dropped; experienced.contractSequence = 1;
		Require(label.EvaluateFreshPotential(new List<SimulatedArtist> { fresh }).BestCandidateScore > 0f &&
			label.EvaluateSigning(new List<SimulatedArtist> { experienced }).BestCandidateScore > 0f,
			"31 fresh and experienced lanes score independently");
	}

	private static void ProbeMarketClearingPerformanceExhaustion() {
		SimulatedArtist artist = NewArtist("exhaustion"); artist.performanceDropCount = 1; artist.labelId = "owner";
		AILabel owner = NewScoutingLabel(); owner.labelId = "owner"; owner.roster.Add(artist); var pool = new List<SimulatedArtist>();
		ArtistManager.ReconcileDroppedArtistForProbe(artist, owner, pool, 1964, ArtistDropReason.Performance);
		Require(artist.performanceDropCount == 2 && artist.lifecycleStatus == ArtistLifecycleStatus.Inactive && !artist.isActive && pool.Count == 0,
			"32 second performance departure exhausts the career atomically");
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
		Require(ArtistManager.GetPerformanceDropCooldownWeeks(artist) == Cooldown,
			"5 the first performance departure keeps the existing cooldown");
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

	private static void ProbeCalendarFormationQuota() {
		foreach (int year in new[] { 1960, 1961, 1965 }) {
			float carry = 0f; int formed = 0;
			for (GameDate date = new(year, 1, 1); date.year == year; date = date.NextDay()) {
				if (!date.IsFriday) continue;
				formed += ArtistManager.CalculateCalendarFormationCount(ref carry, formed);
			}
			Require(formed == 300, $"9 calendar formation quota is exact in {year}");
		}
		float partialCarry = 0f; int partialFormed = 0;
		for (GameDate date = new(1969, 1, 1); date <= new GameDate(1969, 12, 12); date = date.NextDay()) {
			if (date.IsFriday) partialFormed += ArtistManager.CalculateCalendarFormationCount(ref partialCarry, partialFormed);
		}
		Require(partialFormed == 288 && partialCarry >= 0f && partialCarry < 1f,
			"9 partial 1969 checkpoint reports only formations earned by its 50 processed Fridays");
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
			"30 urgency starts on week twelve at the accepted enabled-path floor");
	}

	private static void ProbeScoutingUrgencyResets() {
		Require(RosterManager.FinalizeScoutingUrgencyAge(true, true, 13) == 13 &&
			RosterManager.FinalizeScoutingUrgencyAge(false, false, 13) == 0 &&
			RosterManager.FinalizeScoutingUrgencyAge(true, false, 13) == 13,
			"31 a signing retains urgency while target vacancies remain, and reaching target resets it");
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

	private static void ProbeExperiencedComebackStateModel() {
		SimulatedArtist launchArtist = NewArtist("launch-comeback");
		launchArtist.careerState = CareerState.Dropped;
		launchArtist.careerStateBeforeDrop = CareerState.Rising;
		launchArtist.contractSequence = 1;
		var launchPool = new List<SimulatedArtist> { launchArtist };
		ArtistManager.ReconcileSignedArtistForProbe(launchArtist, launchPool, "launch", 1960);
		Require(launchArtist.careerState == CareerState.Rising && launchArtist.contractSequence == 2 &&
			launchArtist.IsExperiencedComebackContract(),
			"34a every performance comeback uses the current-contract evidence model");

		SimulatedArtist artist = NewArtist("experienced-comeback");
		var pool = new List<SimulatedArtist> { artist };
		ArtistManager.ReconcileSignedArtistForProbe(artist, pool, "first", 1960);
		artist.CompleteChartRun(0, 1, 0);
		artist.CompleteChartRun(0, 1, 0);
		Require(artist.careerState == CareerState.Dropped && artist.careerStateBeforeDrop == CareerState.NewSigning,
			"34b first-contract probation retains the state that preceded its performance drop");

		ArtistManager.ReconcileSignedArtistForProbe(artist, pool, "comeback", 1961);
		Require(artist.contractSequence == 2 && artist.careerState == CareerState.NewSigning &&
			artist.contractEntryCareerState == CareerState.NewSigning && artist.IsExperiencedComebackEvaluationPending(),
			"34c a first-contract flop keeps its pre-drop presentation tier under the experienced-comeback policy");
		AILabel label = NewScoutingLabel();
		Require(!label.ShouldDropArtist(artist), "34d monthly review cannot bypass the experienced-comeback evidence window");
		artist.CompleteChartRun(0, 1, 0);
		artist.CompleteChartRun(0, 1, 0);
		Require(artist.careerState == CareerState.NewSigning && artist.contractConsecutiveFlops == 2,
			"34e an experienced comeback survives two current-contract flops");
		artist.CompleteChartRun(0, 1, 0);
		Require(artist.careerState == CareerState.Dropped && artist.careerStateBeforeDrop == CareerState.NewSigning,
			"34f the third current-contract flop drops an unresolved comeback");

		ArtistManager.ReconcileSignedArtistForProbe(artist, pool, "comeback-hit", 1962);
		artist.RegisterTop40Hit();
		Require(artist.careerState == CareerState.Rising && artist.contractTop40Hits == 1 &&
			!artist.IsExperiencedComebackEvaluationPending(),
			"34g a current-contract hit resolves comeback evaluation using preserved career history");
	}

	private static void ProbeInitialTalentMarketBoundary() {
		Require(ArtistManager.GetDefaultInitialPoolSizeForPath(false) == ArtistManager.LegacyInitialPoolSize &&
			ArtistManager.GetDefaultInitialPoolSizeForPath(true) == ArtistManager.EnabledLifecycleInitialPoolSize,
			"35a disabled initialization stays at 3,000 while the enabled talent market targets 7,000");
		Require(ArtistManager.Instance != null &&
			ArtistManager.Instance.GetAllArtists().Count == ArtistManager.EnabledLifecycleInitialPoolSize,
			"35b the enabled fixed-probe scene materializes the configured 7,000-artist initial market");
	}

	private static void ProbeOperatingRosterTarget() {
		AILabel label = NewScoutingLabel();
		label.maxRosterSize = 10;
		label.roster = new List<SimulatedArtist> { NewArtist("target-1"), NewArtist("target-2"), NewArtist("target-3"), NewArtist("target-4") };
		label.SetOperatingRosterTargetFromCurrent();
		AILabel.ScoutingGateEvaluation operatingGate = label.PreviewScoutingGate(useOperatingRosterTarget: true);
		Require(label.OperatingRosterTarget == 4 && !label.HasOperatingRosterSpace && label.HasRosterSpace &&
			operatingGate.FailureReason == "RosterFull" && operatingGate.MaxRosterSize == 4,
			"36a the soft operating target blocks expansion while preserving hard roster capacity");
		label.roster.Clear();
		label.SetOperatingRosterTargetFromCurrent();
		Require(label.OperatingRosterTarget == 1 && label.HasOperatingRosterSpace,
			"36b an initially empty operating label receives one bootstrap roster slot");
	}

	private static void ProbeNeverSignedInactivityBoundary() {
		SimulatedArtist prospect = NewArtist("unsigned-prospect");
		prospect.weeksContinuouslyUnowned = 500;
		SimulatedArtist veteran = NewArtist("unsigned-veteran");
		veteran.contractSequence = 1;
		veteran.weeksContinuouslyUnowned = 78;
		Require(!ArtistManager.HasPriorContractForInactivityExit(prospect) &&
			ArtistManager.HasPriorContractForInactivityExit(veteran),
			"37 never-signed prospects remain in the labor market while prior-contract careers can age into inactivity");
	}

	private static void ProbeBoundedScoutingDiscovery() {
		ulong first = RosterManager.GetStableDiscoveryKey("label", "artist", 4);
		ulong repeat = RosterManager.GetStableDiscoveryKey("label", "artist", 4);
		ulong refreshed = RosterManager.GetStableDiscoveryKey("label", "artist", 5);
		Require(RosterManager.GetDiscoverySlateSize(0f) == RosterManager.MinimumDiscoverySlateSize &&
			RosterManager.GetDiscoverySlateSize(1f) == RosterManager.MaximumDiscoverySlateSize &&
			first == repeat && first != refreshed,
			"38 discovery visibility is bounded by scouting ability, deterministic, and refreshes without RNG");
	}

	private static SimulatedArtist NewThirdPlusPerformanceComeback(string id, float quality) {
		SimulatedArtist artist = NewScoringCandidate(id, quality, CareerState.Dropped);
		artist.contractSequence = 2;
		artist.lastDropReason = ArtistDropReason.Performance;
		return artist;
	}

	private static RosterManager.FreshProspectPreferenceDecision EvaluateFreshPreference(AILabel label,
		params SimulatedArtist[] candidates) => RosterManager.SelectFreshProspectCandidate(label,
		label.EvaluateSigning(candidates.ToList()), true);

	private static void ProbeFreshProspectPreferenceApplied() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist comeback = NewThirdPlusPerformanceComeback("guarded-comeback", .9f);
		SimulatedArtist fresh = NewScoringCandidate("fresh", .6f);
		var decision = EvaluateFreshPreference(label, comeback, fresh);
		Require(decision.SelectedCandidate == fresh && decision.FreshPreferenceApplied && decision.RepeatComebackDeferred &&
			decision.FallbackReason == "FreshPreferred", "39 a guarded third-contract performance comeback yields to an affordable qualifying fresh prospect");
	}

	private static void ProbeHighestQualifyingFreshProspectWins() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist comeback = NewThirdPlusPerformanceComeback("multiple-comeback", .9f);
		SimulatedArtist lowerFresh = NewScoringCandidate("lower-fresh", .55f);
		SimulatedArtist higherFresh = NewScoringCandidate("higher-fresh", .7f);
		var decision = EvaluateFreshPreference(label, comeback, lowerFresh, higherFresh);
		Require(decision.SelectedCandidate == higherFresh && decision.QualifyingNeverSignedCount == 2,
			"40 highest-scoring qualifying fresh prospect wins within the unchanged slate");
	}

	private static void ProbeFreshPreferenceNoNeverSignedFallback() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist comeback = NewThirdPlusPerformanceComeback("no-fresh", .9f);
		var decision = EvaluateFreshPreference(label, comeback);
		Require(decision.SelectedCandidate == comeback && !decision.FreshPreferenceApplied && decision.FallbackReason == "NoNeverSignedInSlate",
			"41 guarded comeback remains eligible when its slate has no never-signed prospect");
	}

	private static void ProbeFreshPreferenceScoreFallback() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist comeback = NewThirdPlusPerformanceComeback("score-fallback", .9f);
		SimulatedArtist lowFresh = NewScoringCandidate("low-fresh", .1f);
		lowFresh.primaryGenre = Genre.Blues;
		var decision = EvaluateFreshPreference(label, comeback, lowFresh);
		Require(decision.SelectedCandidate == comeback && decision.QualifyingNeverSignedCount == 0 &&
			decision.FallbackReason == "NoQualifyingNeverSigned", "42 guarded comeback remains eligible when fresh prospects fail the existing score threshold");
	}

	private static void ProbeFreshPreferenceUnaffordableFallback() {
		AILabel label = NewScoutingLabel(cash: 1000f);
		SimulatedArtist comeback = NewThirdPlusPerformanceComeback("advance-fallback", .9f);
		SimulatedArtist fresh = NewScoringCandidate("unaffordable-fresh", .6f);
		var decision = EvaluateFreshPreference(label, comeback, fresh);
		Require(decision.SelectedCandidate == comeback && !decision.FreshPreferenceApplied && decision.FallbackReason == "FreshAdvanceUnaffordable",
			"43 unaffordable fresh prospect preserves one selected comeback attempt without an in-week fallback");
	}

	private static void ProbeFirstComebackIsNotGuarded() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist firstComeback = NewThirdPlusPerformanceComeback("first-comeback", .9f);
		firstComeback.contractSequence = 1;
		SimulatedArtist fresh = NewScoringCandidate("first-comeback-fresh", .6f);
		var decision = EvaluateFreshPreference(label, firstComeback, fresh);
		Require(decision.SelectedCandidate == firstComeback && decision.FallbackReason == "OverallBestNotGuarded",
			"44 first performance comeback is not guarded");
	}

	private static void ProbeNonPerformanceDepartureIsNotGuarded() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist closureDeparture = NewThirdPlusPerformanceComeback("closure-departure", .9f);
		closureDeparture.lastDropReason = ArtistDropReason.LabelClosure;
		SimulatedArtist fresh = NewScoringCandidate("closure-fresh", .6f);
		var decision = EvaluateFreshPreference(label, closureDeparture, fresh);
		Require(decision.SelectedCandidate == closureDeparture && decision.FallbackReason == "OverallBestNotGuarded",
			"45 label-closure departure is not guarded");
	}

	private static void ProbeDisabledFreshPreferenceBoundary() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist comeback = NewThirdPlusPerformanceComeback("disabled-comeback", .9f);
		SimulatedArtist fresh = NewScoringCandidate("disabled-fresh", .6f);
		AILabel.SigningEvaluation evaluation = label.EvaluateSigning(new List<SimulatedArtist> { comeback, fresh });
		var decision = RosterManager.SelectFreshProspectCandidate(label, evaluation, false);
		Require(decision.SelectedCandidate == evaluation.BestCandidate && !decision.FreshPreferenceApplied &&
			decision.FallbackReason == "OverallBestNotGuarded", "46 disabled path retains the original candidate choice without a policy RNG draw");
	}

	private static void ProbeFreshPreferenceTelemetry() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist comeback = NewThirdPlusPerformanceComeback("telemetry-comeback", .9f);
		SimulatedArtist fresh = NewScoringCandidate("telemetry-fresh", .6f);
		var applied = EvaluateFreshPreference(label, comeback, fresh);
		var noFresh = EvaluateFreshPreference(label, comeback);
		SimulatedArtist lowFresh = NewScoringCandidate("telemetry-low-fresh", .1f); lowFresh.primaryGenre = Genre.Blues;
		var noQualifying = EvaluateFreshPreference(label, comeback, lowFresh);
		AILabel lowCashLabel = NewScoutingLabel(cash: 1000f);
		var unaffordable = EvaluateFreshPreference(lowCashLabel, comeback, fresh);
		Require(applied.NeverSignedSlateCount == 1 && applied.QualifyingNeverSignedCount == 1 && applied.BestNeverSignedScore.HasValue &&
			applied.ThirdPlusPerformanceComebackCount == 1 && applied.OverallBestContractSequence == 2 &&
			applied.FallbackReason == "FreshPreferred" && noFresh.FallbackReason == "NoNeverSignedInSlate" &&
			noQualifying.FallbackReason == "NoQualifyingNeverSigned" && unaffordable.FallbackReason == "FreshAdvanceUnaffordable",
			"47 fresh-preference telemetry records policy use and all finite fallback reasons");
	}

	private static void ProbeFreshPotentialNoCareerPenalty() {
		AILabel label = NewScoutingLabel();
		SimulatedArtist plain = NewScoringCandidate("plain", .6f); plain.reputation = 0f; plain.momentum = 0f;
		SimulatedArtist decorated = NewScoringCandidate("decorated", .6f); decorated.reputation = .9f; decorated.momentum = .9f;
		float plainScore = label.EvaluateFreshPotential(new List<SimulatedArtist> { plain }).BestCandidateScore.Value;
		float decoratedScore = label.EvaluateFreshPotential(new List<SimulatedArtist> { decorated }).BestCandidateScore.Value;
		Require(Math.Abs(plainScore - decoratedScore) < .0001f, "39 fresh potential ignores reputation and momentum evidence");
	}

	private static void ProbeFreshPotentialThreshold() {
		AILabel label = NewScoutingLabel(); SimulatedArtist prospect = NewScoringCandidate("threshold", .6f);
		Require(label.EvaluateFreshPotential(new List<SimulatedArtist> { prospect }).BestCandidate != null,
			"40 fresh potential retains the normal qualifying threshold");
	}

	private static void ProbePerformanceContractScope() {
		SimulatedArtist artist = NewArtist("scope"); artist.consecutiveFlops = 99;
		artist.CompleteChartRun(0, 1, 0, false); artist.CompleteChartRun(0, 1, 0, false);
		Require(artist.careerState != CareerState.Dropped && artist.contractCompletedChartRuns == 0,
			"41 stale career evidence cannot cause a current-contract departure");
	}

	private static void ProbePerformanceTop40Clearance() {
		SimulatedArtist artist = NewArtist("clearance"); artist.RegisterTop40Hit();
		artist.CompleteChartRun(0, 1, 0);
		Require(artist.careerState == CareerState.Rising && artist.contractTop40Hits == 1 &&
			!artist.IsContractPerformanceProbationPending(),
			"42 a current-contract Top 40 result clears performance probation");
	}

	private static void ProbeFirstPerformanceDeparture() {
		SimulatedArtist artist = NewArtist("first-drop"); artist.labelId = "owner"; AILabel owner = NewScoutingLabel(); owner.labelId = "owner"; owner.roster.Add(artist);
		var pool = new List<SimulatedArtist>(); ArtistManager.ReconcileDroppedArtistForProbe(artist, owner, pool, 1964, ArtistDropReason.Performance);
		Require(artist.performanceDropCount == 1 && artist.lastDropReason == ArtistDropReason.Performance && artist.isActive && pool.Contains(artist),
			"43 first performance departure enters the existing cooldown and remains comeback eligible");
	}

	private static void ProbeSecondPerformanceDepartureExhaustion() => ProbeMarketClearingPerformanceExhaustion();

	private static void ProbeNonPerformanceDepartureDoesNotExhaust() {
		SimulatedArtist artist = NewArtist("expiry"); artist.performanceDropCount = 1; artist.labelId = "owner"; AILabel owner = NewScoutingLabel(); owner.labelId = "owner"; owner.roster.Add(artist);
		var pool = new List<SimulatedArtist>(); ArtistManager.ReconcileDroppedArtistForProbe(artist, owner, pool, 1964, ArtistDropReason.ContractExpired);
		Require(artist.performanceDropCount == 1 && artist.isActive && pool.Contains(artist), "45 non-performance departures do not exhaust a career");
	}

	private static void ProbeNoThirdComebackSigning() {
		SimulatedArtist exhausted = NewArtist("no-third"); exhausted.performanceDropCount = 2; exhausted.isActive = false; exhausted.lifecycleStatus = ArtistLifecycleStatus.Inactive; exhausted.careerState = CareerState.Retired;
		Require(!ArtistManager.IsEligibleUnsignedCandidateForProbe(exhausted), "46 exhausted artists are not signable for a third comeback");
	}

	private static void ProbeMarketClearingTelemetryFields() {
		var observation = new RosterManager.LabelScoutingVacancyObservation { ServiceMode = "Recovery", FreshDiscoveryScope = "National", RecoveryFailureReason = "FreshRecoveryQualified" };
		Require(observation.ServiceMode == "Recovery" && observation.FreshDiscoveryScope == "National" && observation.RecoveryFailureReason == "FreshRecoveryQualified",
			"47 market-clearing telemetry records the production service and discovery branches");
	}

	private static void ProbeHeadcountOnlyRecovery() {
		var atTargetWithoutReleaseLanes = RosterManager.GetTalentServiceSnapshotForProbe(3, 3, 6, 0, 0);
		Require(atTargetWithoutReleaseLanes.HeadcountDeficit == 0 && atTargetWithoutReleaseLanes.ReleaseLaneDeficit == 3 &&
			atTargetWithoutReleaseLanes.ServiceDeficit == 0 && atTargetWithoutReleaseLanes.ServiceMode == "Normal" &&
			!RosterManager.CanAttemptMarketClearingSigning(3, 3),
			"48 release-lane telemetry cannot create Recovery or a signing at the operating target");
	}

	private static void ProbeHeadcountRecoveryBoundaries() {
		var watch1 = RosterManager.GetTalentServiceSnapshotForProbe(2, 3, 6, 3, 0);
		var watch3 = RosterManager.GetTalentServiceSnapshotForProbe(2, 3, 6, 3, 2);
		var recovery4 = RosterManager.GetTalentServiceSnapshotForProbe(2, 3, 6, 3, 3);
		var deep = RosterManager.GetTalentServiceSnapshotForProbe(1, 3, 6, 3, 0);
		Require(watch1.ServiceMode == "Watch" && watch3.ServiceMode == "Watch" && recovery4.ServiceMode == "Recovery" &&
			deep.ServiceMode == "Recovery" && RosterManager.GetTalentServiceModeForProbe(3, 3, 6, 0, 9) == "Normal" &&
			RosterManager.CanAttemptMarketClearingSigning(2, 3),
			"49 headcount Recovery follows watch timing, clears exactly at target, and never uses a temporary ceiling");
	}

	private static void ProbeRosterThroughputBootstrap() {
		AILabel populated = NewScoutingLabel();
		populated.roster.AddRange(new[] { NewArtist("launch-1"), NewArtist("launch-2") });
		populated.SetOperatingRosterTargetFromCurrent();
		AILabel empty = NewScoutingLabel(); empty.SetOperatingRosterTargetFromCurrent();
		var noLaneVacancy = RosterManager.GetTalentServiceSnapshotForProbe(2, 2, 4, 0, 0);
		Require(populated.OperatingRosterTarget == 2 && populated.operatingRosterTargetSource == "PopulatedLaunchRoster" &&
			empty.OperatingRosterTarget == 1 && empty.operatingRosterTargetSource == "OneArtistBootstrap" &&
			noLaneVacancy.ServiceDeficit == 0 && !RosterManager.CanAttemptMarketClearingSigning(2, 2),
			"50 operating targets retain initialized headcount or exactly one empty-label bootstrap; release eligibility cannot create a vacancy");
	}

	private static void ProbeFirstContractProbationThreshold() {
		SimulatedArtist first = NewArtist("first-contract");
		first.CompleteChartRun(0, 1, 0);
		Require(first.careerState == CareerState.NewSigning && first.GetPerformanceEvaluationMode() == ArtistPerformanceEvaluationMode.FirstContractProbation,
			"51a a first-contract artist survives one current-contract flop");
		first.CompleteChartRun(0, 1, 0);
		Require(first.careerState == CareerState.Dropped && first.contractCompletedChartRuns == 2 && first.contractConsecutiveFlops == 2,
			"51b a first-contract artist drops on two completed current-contract flops");

		SimulatedArtist stale = NewArtist("stale-contract"); stale.contractSequence = 2;
		stale.CompleteChartRun(0, 1, 0, false); stale.CompleteChartRun(0, 1, 0, false);
		Require(stale.careerState == CareerState.NewSigning && stale.contractCompletedChartRuns == 0,
			"51c stale prior-contract evidence cannot satisfy first-contract probation");
	}

	private static void ProbeNormalCareerAfterProbation() {
		SimulatedArtist artist = NewArtist("normal-career");
		artist.RegisterTop40Hit();
		artist.CompleteChartRun(0, 1, 0); artist.CompleteChartRun(0, 1, 0);
		Require(artist.careerState == CareerState.Declining && artist.GetPerformanceEvaluationMode() == ArtistPerformanceEvaluationMode.NormalCareer &&
			!artist.IsContractPerformanceProbationPending(),
			"52a a current-contract Top 40 clears probation and permits normal Rising-to-Declining progression");
		artist.CompleteChartRun(0, 1, 0);
		Require(artist.careerState == CareerState.Dropped && !artist.ShouldDepartForCurrentContractPerformance(),
			"52b a cleared probation does not suppress normal Declining performance departure");
	}

	private static void ProbeLabelReleaseCapacityBoundary() {
		float stable = CompetitorManager.CalculateLabelReleaseCapacityChance(2f, LabelStatus.Stable, 3);
		float scarce = CompetitorManager.CalculateLabelReleaseCapacityChance(2f, LabelStatus.Stable, 1);
		float seasonallyBusy = CompetitorManager.CalculateLabelReleaseCapacityChance(4f, LabelStatus.Rising, 3, 1.5f);
		float closed = CompetitorManager.CalculateLabelReleaseCapacityChance(4f, LabelStatus.Defunct, 3, 1.5f);
		float noRosterCapacity = CompetitorManager.CalculateLabelReleaseCapacityChance(4f, LabelStatus.Stable, 0);
		Require(Math.Abs(stable - 0.5f) < .000001f && Math.Abs(scarce - (1f / 6f)) < .000001f &&
			Math.Abs(seasonallyBusy - 1f) < .000001f && closed == 0f && noRosterCapacity == 0f,
			"53 label cadence derives only from explicit monthly capacity, status, availability, and bounded seasonality");
	}

	private static void Require(bool condition, string message) {
		if (!condition) throw new InvalidOperationException("D6 probe failed: " + message);
	}
}
