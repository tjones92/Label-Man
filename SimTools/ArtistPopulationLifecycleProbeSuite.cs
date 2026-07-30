using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Directive 6 fixed-input probes. They deliberately use detached artists and
/// labels so they do not perturb an audit world's object graph.
///
/// They are NOT RNG-neutral, despite what this comment claimed until it was
/// measured: several probes reach helpers that draw from the global stream
/// (<see cref="RosterManager.InitializeRuntimeRosterForProbe"/> consumes the
/// legacy capacity draw, for one). A 52-week run with the flag and without it
/// diverge in 1960 â€” album units 2,271,329 against 2,426,185 on seed 1001. Never
/// pass --artist-population-lifecycle-probes to a run being compared against a
/// control; probe runs and comparison runs stay separate, exactly as they must
/// for --genre-market-v2-probes.
///
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
		ProbeRuntimeLabelOrganicGrowth();                           // 61
		ProbeRuntimeFoundedOperatingProfiles();                     // 62
		ProbeDailyTalentMarketScheduling();                         // 63
		ProbeCatastrophicFailFastBoundaries();                       // 64
		ProbeCatastrophicControlParsing();                           // 65
		ProbeAlbumMonotonicPenetration();                            // 66
		ProbeAlbumFormatClearingBudget();                             // 67
		ProbeMidTierPromotionBoundary();                               // 68
		ProbeCompetitiveLabelExitBoundary();                            // 69
		ProbeVacancyDenominatedHiringDemand();                           // 70
		ProbeExperiencedTalentReservoir();                                // 71
		ProbeEarnedNationalReachBoundaries();                                // 72
		ProbeEarnedReachDemandScale();                                          // 73
		ProbePersistentRegionalDealEvidence();                                      // 74
		ProbeRetiredLabelEvidenceLookback();                                        // 75
		ProbeWeeklyAwarenessAgeDecay();                                              // 76
		ProbeReleaseImprintIdentity();                                                // 77
		ProbeRegionScaledBreakoutEvidence();                                           // 78
		ProbePerSongDistributionScope();                                                // 79
		ProbePhysicalDistributionGovernsShelfStock();                                   // 80
		ProbeSeededLargeFirmPopulation();                                                // 81
		ProbeBreakoutEvidenceRewardsConstrainedDemand();                                 // 82
		ProbeArtistReleaseHistoryCountsOnce();                                           // 83
		ProbePromoCannibalizationChargedOnce();                                          // 84
		ProbePromoRecruitmentMatchesDiversionTerms();                                    // 85
		ProbeLoweredLocalTractionAdmitsStrandedBand();                                   // 86
		ProbeConsolidationGate();                                                        // 87
		ProbeSubsidiaryAbsorptionRetainsLabel();                                         // 88
		ProbeDependentHitmakerArchetype();                                               // 89
		results.Add("D6 fixed probes 1-89 passed (contract/cooldown/calendar formation/identity/lifecycle/roster normalization/discovery lanes/performance exhaustion/label release capacity/economic-yield diagnostics/prospect participation/runtime-label bootstrap, organic growth, deterministic runtime operating profiles, daily talent-market scheduling, catastrophic fail-fast semantics, schema-bound control parsing, Album monotonic penetration, market-wide Album format clearing, evidence-gated MidTier promotion, bounded competitive label exit, vacancy-denominated hiring demand, the experienced talent reservoir, earned national-reach boundaries, the earned-reach Single-demand scale, persistent home-region distribution evidence, retired-record lookback, weekly awareness aging, immutable release-imprint identity, region-scaled regional breakout evidence, per-song distribution-deal scope, physically distributed shelf stock, a historically scaled seeded large-firm population, breakout evidence that credits demand a label cannot fulfil, single-counted artist project history, promo cannibalization charged once against the Album-component projection, promo recruitment on the same base terms as diversion, a lowered LocalTraction activation that admits the stranded breakout band, a late-decade major-consolidation gate scoped to Major acquirers absorbing charted independents inside the window and cap, and a subsidiary absorption that folds borrowed reach into permanent owned reach, unions the parent's regions and rolls ownership up while the label keeps operating and charting, and a minority dependent-hitmaker archetype among runtime Independents that charts strongly but keeps low owned reach so it stays an absorption target)");
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
		Require(runtime.CurrentRosterSize == 0 && runtime.OperatingRosterTarget == 1 && runtime.operatingRosterTargetSource == "RuntimeBootstrap",
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

	private static void ProbeRuntimeLabelOrganicGrowth() {
		Require(LabelLifecycleManager.GetRosterCapacityForTier(LabelTier.Small) == 5 &&
			LabelLifecycleManager.GetRosterCapacityForTier(LabelTier.Boutique) == 8 &&
			LabelLifecycleManager.GetRosterCapacityForTier(LabelTier.Independent) == 12 &&
			LabelLifecycleManager.GetRosterCapacityForTier(LabelTier.MidTier) == 25 &&
			LabelLifecycleManager.GetRosterCapacityForTier(LabelTier.Major) == 50,
			"61a canonical lifecycle tier capacities are exactly 5/8/12/25/50");
		int smallDraws = 0; int smallState = 0;
		Action<int, int> smallDraw = (minimum, maximum) => { smallDraws++; smallState = (smallState * 31) + minimum + maximum; };
		AILabel smallRuntime = NewScoutingLabel(); smallRuntime.tier = LabelTier.Small; smallRuntime.populationOrigin = LabelPopulationOrigin.RuntimeFounded;
		RosterManager.ConsumeLegacyRuntimeCapacityAlignmentDrawForProbe(smallRuntime, smallDraw);
		Require(smallDraws == 1 && smallState == 13,
			"61b runtime Small birth consumes exactly the legacy 3..10 compatibility draw");
		int independentDraws = 0; int independentState = 0;
		Action<int, int> independentDraw = (minimum, maximum) => { independentDraws++; independentState = (independentState * 31) + minimum + maximum; };
		AILabel independentRuntime = NewScoutingLabel(); independentRuntime.tier = LabelTier.Independent; independentRuntime.populationOrigin = LabelPopulationOrigin.RuntimeFounded;
		RosterManager.ConsumeLegacyRuntimeCapacityAlignmentDrawForProbe(independentRuntime, independentDraw);
		Require(independentDraws == 1 && independentState == 26,
			"61c runtime Independent birth consumes exactly the legacy 8..18 compatibility draw");

		AILabel launch = NewScoutingLabel(12); launch.roster.Add(NewArtist("launch")); launch.SetOperatingRosterTargetFromCurrent();
		launch.populationOrigin = LabelPopulationOrigin.LaunchPopulation;
		Require(launch.populationOrigin == LabelPopulationOrigin.LaunchPopulation && launch.OperatingRosterTarget == 1,
			"61d launch labels retain launch origin and their populated operating baseline");

		Require(!LabelLifecycleManager.IsOrganicGrowthEligibleOrigin(launch) &&
			LabelLifecycleManager.GetOrganicGrowthBlockingReason(launch, 4, 4, 13) == "NotGrowthEligible",
			"61d2 a launch Independent stays frozen at the roster it opened with");
		foreach (LabelTier flat in new[] { LabelTier.Small, LabelTier.Boutique, LabelTier.Independent }) {
			launch.tier = flat;
			Require(!LabelLifecycleManager.IsOrganicGrowthEligibleOrigin(launch),
				$"61d3 launch {flat} appetite is deliberately flat across the decade");
		}
		foreach (LabelTier upper in new[] { LabelTier.Major, LabelTier.MidTier }) {
			launch.tier = upper;
			Require(LabelLifecycleManager.IsOrganicGrowthEligibleOrigin(launch) &&
				LabelLifecycleManager.GetOrganicGrowthBlockingReason(launch, 4, 4, 13) != "NotGrowthEligible",
				$"61d4 launch {upper} appetite can be earned rather than pinned at its 1960 roster");
		}
		launch.tier = LabelTier.Major; launch.status = LabelStatus.Stable; launch.lastMonthlyProfit = 100f;
		launch.consecutiveLossMonths = 0; launch.cashReserves = launch.GetMonthlyOverhead() * 6f;
		Require(LabelLifecycleManager.TryAuthorizeOrganicGrowthForProbe(launch, 4, 4, 13) && launch.OperatingRosterTarget == 2 &&
			LabelLifecycleManager.GetOrganicGrowthBlockingReason(launch, 4, 4, 26) == "OperatingTargetUnfilled",
			"61d5 an upper-tier launch label earns exactly one slot per quarter and must fill it before the next");

		AILabel runtime = NewScoutingLabel(5);
		runtime.populationOrigin = LabelPopulationOrigin.RuntimeFounded;
		runtime.roster.Add(NewArtist("runtime-1"));
		runtime.SetOperatingRosterTarget(1, LabelOperatingTargetReason.RuntimeBootstrap, 10);
		runtime.status = LabelStatus.Stable; runtime.lastMonthlyProfit = 100f; runtime.consecutiveLossMonths = 0; runtime.cashReserves = runtime.GetMonthlyOverhead() * 6f;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(runtime, 0, 1, 13) == "Eligible" &&
			LabelLifecycleManager.TryAuthorizeOrganicGrowthForProbe(runtime, 0, 1, 13) && runtime.OperatingRosterTarget == 2 &&
			runtime.organicRosterTargetGrowthCount == 1 && runtime.CurrentRosterSize == 1,
			"61e a filled, profitable founder with a recent release gains exactly one emergence slot without requiring a chart hit");
		Require(!LabelLifecycleManager.TryAuthorizeOrganicGrowthForProbe(runtime, 0, 1, 13) && runtime.lastOrganicGrowthBlockingReason == "AlreadyReviewedThisQuarter",
			"61f a quarterly pass cannot grant a second organic target decision");

		runtime.roster.Add(NewArtist("runtime-2"));
		runtime.cashReserves = runtime.GetMonthlyOverhead() * 6f;
		Require(LabelLifecycleManager.TryAuthorizeOrganicGrowthForProbe(runtime, 0, 1, 26) && runtime.OperatingRosterTarget == 3 &&
			runtime.organicRosterTargetGrowthCount == 2 && runtime.lastOrganicRosterTargetGrowthWeek == 26,
			"61g release-backed emergence can mature a founder to the three-lane operating floor");
		runtime.roster.Add(NewArtist("runtime-3"));
		runtime.cashReserves = runtime.GetMonthlyOverhead() * 6f;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(runtime, 0, 1, 39) == "NoRecentCharting",
			"61h growth beyond the emergence floor still requires demonstrated chart success");

		AILabel blocked = NewScoutingLabel(5); blocked.populationOrigin = LabelPopulationOrigin.RuntimeFounded;
		blocked.SetOperatingRosterTarget(2, LabelOperatingTargetReason.RuntimeBootstrap, 0); blocked.roster.Add(NewArtist("blocked"));
		blocked.status = LabelStatus.Stable; blocked.lastMonthlyProfit = 100f; blocked.cashReserves = blocked.GetMonthlyOverhead() * 6f;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(blocked, 1, 1, 13) == "OperatingTargetUnfilled", "61i unfilled targets cannot grow");
		blocked.roster.Add(NewArtist("blocked-2")); blocked.status = LabelStatus.Struggling;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(blocked, 1, 1, 13) == "UnhealthyStatus", "61j distressed labels cannot grow");
		blocked.status = LabelStatus.Stable; blocked.lastMonthlyProfit = -1f;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(blocked, 1, 1, 13) == "NotProfitable", "61k loss-making labels cannot grow");
		blocked.lastMonthlyProfit = 100f; blocked.cashReserves = 0f;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(blocked, 1, 1, 13) == "InsufficientRunway", "61l under-runway labels cannot grow");
		blocked.cashReserves = blocked.GetMonthlyOverhead() * 6f;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(blocked, 0, 0, 13) == "NoRecentRelease", "61m emergence cannot grow without demonstrated release activity");
		blocked.maxRosterSize = 2;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(blocked, 1, 1, 13) == "HardCapacityFull", "61n hard-full labels cannot grow");
		blocked.status = LabelStatus.Acquired;
		Require(LabelLifecycleManager.GetOrganicGrowthBlockingReason(blocked, 1, 1, 13) == "InactiveLabel", "61o acquired labels cannot grow");

		AILabel acquired = NewScoutingLabel(5); acquired.populationOrigin = LabelPopulationOrigin.RuntimeFounded;
		acquired.SetOperatingRosterTarget(1, LabelOperatingTargetReason.RuntimeBootstrap, 0);
		acquired.roster.Add(NewArtist("acquired-1")); acquired.roster.Add(NewArtist("acquired-2")); acquired.roster.Add(NewArtist("acquired-3"));
		LabelLifecycleManager.ReconcileAcquisitionRosterTargetForProbe(acquired, 26);
		Require(acquired.OperatingRosterTarget == 3 && acquired.maxRosterSize >= 3 && acquired.operatingRosterTargetReason == LabelOperatingTargetReason.AcquisitionReconciliation &&
			!RosterManager.CanAttemptMarketClearingSigning(acquired.CurrentRosterSize, acquired.OperatingRosterTarget),
			"61p acquisition reconciliation recognizes transferred roster without creating a vacancy");
		runtime.monthsActive = 18;
		Require(LabelLifecycleManager.IsRuntimeFounderInEmergenceRunway(runtime),
			"61q runtime founders retain normal release status through their eighteen-month emergence runway");
		runtime.monthsActive = 19;
		Require(!LabelLifecycleManager.IsRuntimeFounderInEmergenceRunway(runtime) &&
			!LabelLifecycleManager.IsRuntimeFounderInEmergenceRunway(launch),
			"61r emergence protection ends after eighteen months and never applies to the launch population");
	}

	private static void ProbeRuntimeFoundedOperatingProfiles() {
		var smallArchetypes = new HashSet<LabelArchetype>();
		var independentArchetypes = new HashSet<LabelArchetype>();
		for (int index = 0; index < 160; index++) {
			AILabel small = NewRuntimeProfileProbeLabel("small-" + index, LabelTier.Small);
			AILabel independent = NewRuntimeProfileProbeLabel("independent-" + index, LabelTier.Independent);
			RuntimeLabelProfileFactory.Initialize(small, null, 17 + index, new GameDate(1964, 4, 1), 1001UL);
			RuntimeLabelProfileFactory.Initialize(independent, null, 17 + index, new GameDate(1964, 4, 1), 1001UL);
			smallArchetypes.Add(small.archetype); independentArchetypes.Add(independent.archetype);
			Require(RuntimeLabelProfileFactory.IsValidRuntimePair(small.tier, small.archetype) && RuntimeLabelProfileFactory.IsValidRuntimePair(independent.tier, independent.archetype) &&
				ProfileWithinEnvelope(small) && ProfileWithinEnvelope(independent) && RuntimeLabelProfileFactory.HasCompleteOperatingProfile(small) && RuntimeLabelProfileFactory.HasCompleteOperatingProfile(independent),
				"62a every runtime profile has a tier-valid archetype and nonzero fields within the canonical envelope");
		}
		Require(smallArchetypes.SetEquals(new[] { LabelArchetype.RegionalHustler, LabelArchetype.RockRebel, LabelArchetype.BluesRoots, LabelArchetype.CountrySpecialist, LabelArchetype.GospelPowerhouse }) &&
			independentArchetypes.SetEquals(new[] { LabelArchetype.SoulFactory, LabelArchetype.RockRebel, LabelArchetype.BluesRoots, LabelArchetype.CountrySpecialist, LabelArchetype.TeenHitMachine, LabelArchetype.GospelPowerhouse, LabelArchetype.RegionalHustler }),
			"62b only the allowed Small and Independent archetypes are reachable; corporate, folk, and jazz profiles are excluded");

		AILabel first = NewRuntimeProfileProbeLabel("stable-profile", LabelTier.Independent);
		AILabel repeat = NewRuntimeProfileProbeLabel("stable-profile", LabelTier.Independent);
		RuntimeLabelProfileFactory.Initialize(first, null, 91, new GameDate(1965, 2, 1), 1001UL);
		RuntimeLabelProfileFactory.Initialize(repeat, null, 91, new GameDate(1965, 2, 1), 1001UL);
		Require(ProfileFingerprint(first) == ProfileFingerprint(repeat) && first.foundedYear == 1965 && first.monthsActive == 0 && first.totalReleases == 0 &&
			first.top40Hits == 0 && first.numberOneHits == 0 && first.momentumScore == 0f && first.consecutiveLossMonths == 0 &&
			!string.IsNullOrEmpty(first.homeRegion) && !string.IsNullOrEmpty(first.homeCityId) && !string.IsNullOrEmpty(first.homeCityAssignmentSource),
			"62c stable seed/identity/week inputs reproduce the profile while founding history and canonical geography reset exactly at birth");
		AILabel changed = NewRuntimeProfileProbeLabel("changed-profile", LabelTier.Independent);
		RuntimeLabelProfileFactory.Initialize(changed, null, 91, new GameDate(1965, 2, 1), 1001UL);
		Require(ProfileFingerprint(first) != ProfileFingerprint(changed), "62d a changed stable identity yields a different isolated profile without global-RNG participation");
		RosterManager.InitializeRuntimeRosterForProbe(first);
		Require(first.CurrentRosterSize == 0 && first.OperatingRosterTarget == 1 && first.maxRosterSize == 12 &&
			CompetitorManager.CalculateLabelReleaseCapacityChance(first.releasesPerMonth, first.status, 1) > 0f,
			"62e production profile initialization leaves the runtime roster empty at target one with canonical capacity and positive signed-artist release chance");
		AILabel westCoast = NewRuntimeProfileProbeLabel("west-coast-profile", LabelTier.Independent);
		westCoast.headquartersCity = "San Francisco";
		RuntimeLabelProfileFactory.Initialize(westCoast, null, 92, new GameDate(1965, 2, 1), 1001UL);
		Require(westCoast.homeRegion == "westcoast" && westCoast.homeCityId == "san_francisco" &&
			westCoast.distributionRegions.SequenceEqual(new[] { "westcoast" }),
			"62f runtime geography resolves from the canonical headquarters city and includes a functioning home-market distribution path");
		Require(LabelLifecycleManager.SelectRuntimeFoundingTier(.24f, .25f) == LabelTier.Small &&
			LabelLifecycleManager.SelectRuntimeFoundingTier(.25f, .25f) == LabelTier.Independent,
			"62g runtime founding preserves a bounded Small tail while making Independent the dominant entry tier");
	}

	private static void ProbeDailyTalentMarketScheduling() {
		var offsets = new HashSet<int>();
		for (int index = 0; index < 512; index++) offsets.Add(RosterManager.GetDailyScoutingOffsetForProbe(1001UL, "daily-probe-" + index, 1));
		Require(offsets.SetEquals(Enumerable.Range(0, 7)), "63a stable daily vacancy schedules cover all seven offsets without global RNG");
		GameDate opened = new(1960, 1, 1);
		GameDate first = RosterManager.GetInitialDailyScoutingDateForProbe(1001UL, "daily-spacing", 2, opened);
		Require(first >= opened && first <= opened.AddDays(6) && first.AddDays(7).AddDays(-7) == first,
			"63b an unfilled vacancy is scheduled within seven days and then exactly weekly");
		GameDate protectedDate = RosterManager.GetInitialDailyScoutingDateForProbe(1001UL, "runtime-birth", 1, opened, 1);
		Require(RosterManager.GetCalendarChartWeekForProbe(protectedDate) > 1,
			"63c runtime founders are advanced beyond their birth chart week before service");
		var weekdays = new HashSet<DayOfWeek>();
		for (int index = 0; index < 512; index++) weekdays.Add(RosterManager.GetInitialDailyScoutingDateForProbe(1001UL, "weekday-probe-" + index, 1, opened).DayOfWeek);
		Require(weekdays.SetEquals(Enum.GetValues<DayOfWeek>()) && weekdays.Contains(DayOfWeek.Saturday) && weekdays.Contains(DayOfWeek.Sunday),
			"63d daily appointment dates cover all weekdays including Saturday and Sunday");
	}

	private static void ProbeCatastrophicFailFastBoundaries() {
		Require(!ChartAuditRunner.IsInvalidFailFastFinanceValueForProbe(-116.453125d) &&
			!ChartAuditRunner.IsInvalidFailFastFinanceValueForProbe(0d) &&
			ChartAuditRunner.IsInvalidFailFastFinanceValueForProbe(double.NaN) &&
			ChartAuditRunner.IsInvalidFailFastFinanceValueForProbe(double.PositiveInfinity),
			"64a finite terminal debt is valid finance state while NaN and infinity remain catastrophic");
		Require(!ChartAuditRunner.ShouldValidateCompletedFailFastYear(1960, 1960) &&
			ChartAuditRunner.ShouldValidateCompletedFailFastYear(1960, 1961) &&
			ChartAuditRunner.ShouldValidateCompletedFailFastYear(1969, 1970),
			"64b completed-year comparison fires on a real calendar transition rather than an arbitrary 52-week multiple");
		Require(!ChartAuditRunner.IsCatastrophicFailFastRatioForProbe(.70d, 1d) &&
			!ChartAuditRunner.IsCatastrophicFailFastRatioForProbe(1.30d, 1d) &&
			ChartAuditRunner.IsCatastrophicFailFastRatioForProbe(.699999d, 1d) &&
			ChartAuditRunner.IsCatastrophicFailFastRatioForProbe(1.300001d, 1d) &&
			!ChartAuditRunner.IsCatastrophicFailFastRatioForProbe(1d, 0d),
			"64c catastrophic ratios preserve inclusive 0.70/1.30 boundaries and the explicit zero-denominator non-abort");
		Require(ChartAuditRunner.FormatCompletedYearRatioStateForProbe(1963, 999d / 1446d)
			.StartsWith("completedYear=1963 ratio=0.690871 ", StringComparison.Ordinal),
			"64d fail-fast state names the completed year instead of implying the new checkpoint year");
		AILabel runtime = NewScoutingLabel(); runtime.populationOrigin = LabelPopulationOrigin.RuntimeFounded; runtime.runtimeBirthWeek = 18;
		AILabel launch = NewScoutingLabel(); launch.populationOrigin = LabelPopulationOrigin.LaunchPopulation;
		Require(ChartAuditRunner.IsRuntimeBirthWeekSigningViolationForProbe("signing", runtime, 18) &&
			ChartAuditRunner.IsRuntimeBirthWeekSigningViolationForProbe("re-signing", runtime, 17) &&
			!ChartAuditRunner.IsRuntimeBirthWeekSigningViolationForProbe("signing", runtime, 19) &&
			!ChartAuditRunner.IsRuntimeBirthWeekSigningViolationForProbe("formation", runtime, 18) &&
			!ChartAuditRunner.IsRuntimeBirthWeekSigningViolationForProbe("signing", launch, 18),
			"64e birth-week protection validates signing events only and cannot misclassify later roster transfers");
	}

	private static void ProbeCatastrophicControlParsing() {
		string[] releases = {
			"successfulReleases,week,year",
			"10,1,1960",
			"20,2,1960"
		};
		var seasonality = new List<string> { "albumProjectsScheduled,year,successfulReleases,month" };
		for (int month = 1; month <= 12; month++) {
			int successful = month == 1 ? 10 : month == 2 ? 20 : 0;
			seasonality.Add($"1,1960,{successful},{month}");
		}
		var albumProjects = new List<string> { "scheduledWeek,projectId" };
		for (int project = 1; project <= 12; project++) albumProjects.Add($"{(project <= 6 ? 1 : 2)},project-{project}");
		string[] revenue = {
			"marketNet,labelNet,gross,totalMarketUnits,releaseFormat,labelTier,year,period",
			"80,70,100,1234,\"All\",\"All\",1960,annual"
		};
		(int parsedReleases, int albums, long units) = ChartAuditRunner.ParseCatastrophicFailFastControlForProbe(releases, seasonality, albumProjects, revenue, 1960);
		Require(parsedReleases == 30 && albums == 12 && units == 1234,
			"65a control parsing binds by required header name and remains correct under reordered columns and quoted fields");

		RequireThrows<InvalidDataException>(() => ChartAuditRunner.ParseCatastrophicFailFastControlForProbe(
			new[] { "week,year", "1,1960" }, seasonality, albumProjects, revenue, 1960),
			"65b missing required control columns fail closed before simulation");
		RequireThrows<InvalidDataException>(() => ChartAuditRunner.ParseCatastrophicFailFastControlForProbe(
			new[] { "successfulReleases,week,year", "oops,1,1960" }, seasonality, albumProjects, revenue, 1960),
			"65c malformed numeric control fields fail closed before simulation");
		var mismatchedSeasonality = seasonality.ToList();
		mismatchedSeasonality[1] = "1,1960,9,1";
		RequireThrows<InvalidDataException>(() => ChartAuditRunner.ParseCatastrophicFailFastControlForProbe(
			releases, mismatchedSeasonality, albumProjects, revenue, 1960),
			"65d independently sourced annual release totals must reconcile before simulation");
		RequireThrows<InvalidDataException>(() => ChartAuditRunner.ParseCatastrophicFailFastControlForProbe(
			releases, seasonality.Take(12), albumProjects, revenue, 1960),
			"65e incomplete calendar-month coverage fails closed before simulation");
		var unmappedProjects = albumProjects.ToList(); unmappedProjects[1] = "99,project-1";
		RequireThrows<InvalidDataException>(() => ChartAuditRunner.ParseCatastrophicFailFastControlForProbe(
			releases, seasonality, unmappedProjects, revenue, 1960),
			"65f Album projects must map through an authoritative chart-week year before simulation");
	}

	private static void ProbeAlbumMonotonicPenetration() {
		var data = new RegionalRecordData("probe-region");
		float first = AlbumSimulator.CalculateEffectiveRegionalPenetration(data, 100, 1_000f, true);
		float firstExhaustion = AlbumSimulator.CalculateAlbumExhaustion(first);
		Require(first == .1f && data.albumPeakEffectivePenetration == .1f,
			"66a first live Album penetration observation uses the existing observed value");
		float grownPool = AlbumSimulator.CalculateEffectiveRegionalPenetration(data, 100, 2_000f, true);
		float grownPoolExhaustion = AlbumSimulator.CalculateAlbumExhaustion(grownPool);
		Require(grownPool == first && grownPoolExhaustion <= firstExhaustion && data.albumPeakEffectivePenetration == first,
			"66b buyer-pool growth cannot reduce effective penetration or raise exhaustion headroom");
		float higherSales = AlbumSimulator.CalculateEffectiveRegionalPenetration(data, 300, 2_000f, true);
		float higherSalesExhaustion = AlbumSimulator.CalculateAlbumExhaustion(higherSales);
		Require(higherSales == .15f && data.albumPeakEffectivePenetration == .15f && higherSalesExhaustion >= .15f,
			"66c cumulative Album sales increase the stored peak while exhaustion retains its floor");
		float disabled = AlbumSimulator.CalculateEffectiveRegionalPenetration(data, 100, 2_000f, false);
		Require(disabled == .05f && data.albumPeakEffectivePenetration == .15f &&
			AlbumSimulator.CalculateRawDemandBeforeCannibalization(2_000f, .5f, .02f) == 2f * AlbumSimulator.CalculateRawDemandBeforeCannibalization(1_000f, .5f, .02f),
			"66d disabled/prewarm penetration remains stateless and buyer-pool growth still directly multiplies raw Album demand without RNG participation");
	}

	private static void ProbeAlbumFormatClearingBudget() {
		ChartManager.FormatClearingBudget noAlbum = ChartManager.CalculateFormatClearingBudget(1_200, 0, 1_000);
		Require(noAlbum.Single == 1_000 && noAlbum.Album == 0 && noAlbum.EffectiveAlbum == 0f,
			"67a a market without Album intent retains the full common Single capacity");
		ChartManager.FormatClearingBudget crowded = ChartManager.CalculateFormatClearingBudget(1_200, 100, 1_000);
		int legacyAlbumShare = (int)Math.Round(1_000f * 100f / 1_300f);
		float unpressuredOverlap = 1_000f * 100f / 1_100f;
		Require(crowded.Album > 0 && crowded.Album < legacyAlbumShare &&
			crowded.EffectiveAlbum < unpressuredOverlap &&
			crowded.Single + crowded.Album <= 1_000,
			"67b shared-pool overlap pressure gives Album intent a smaller bounded format budget than cloned common clearing");
		ChartManager.FormatClearingBudget doubled = ChartManager.CalculateFormatClearingBudget(1_200, 200, 1_000);
		Require(doubled.Album > crowded.Album && doubled.Album < crowded.Album * 2,
			"67c additional Album intent increases its budget sublinearly");
		ChartManager.FormatClearingBudget mature = ChartManager.CalculateFormatClearingBudget(1_200, 100, 1_000,
			ChartManager.CalculateAlbumIntentOverlapPressure(1f), albumChannelMaturity: 1f, albumChannelCapacity: 80);
		Require(mature.Single == 1_000 && mature.EffectiveAlbum == 100f && mature.Album == 80 &&
			ChartManager.CalculateAlbumIntentOverlapPressure(0f) == 2f,
			"67d the established retail transition exposes separate bounded Single and Album channels without changing early pressure");
		Require(ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Album, true, 100, 20f,
			ageWeeks: 155, hasCharted: false, weeksSinceLastCharted: 155, automaticAgeWeeks: 156, chartGraceWeeks: 26) &&
			!ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Album, true, 100, 20f,
				ageWeeks: 156, hasCharted: false, weeksSinceLastCharted: 156, automaticAgeWeeks: 156, chartGraceWeeks: 26) &&
			ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Album, true, 100, 20f,
				ageWeeks: 220, hasCharted: true, weeksSinceLastCharted: 25, automaticAgeWeeks: 156, chartGraceWeeks: 26) &&
			!ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Album, true, 100, 20f,
				ageWeeks: 220, hasCharted: true, weeksSinceLastCharted: 26, automaticAgeWeeks: 156, chartGraceWeeks: 26),
			"67e automatic uncharted Album replenishment closes after the three-year establishment or post-chart grace window");
	}

	private static AILabel NewRuntimeProfileProbeLabel(string id, LabelTier tier) => new() {
		labelId = id, labelName = id, tier = tier, headquartersCity = "New York", status = LabelStatus.Stable,
		populationOrigin = LabelPopulationOrigin.RuntimeFounded, roster = new List<SimulatedArtist>()
	};

	private static bool ProfileWithinEnvelope(AILabel label) {
		(bool small, float budgetMin, float budgetMax, float marketingMin, float marketingMax, float reachMin, float reachMax, float nationalMin, float nationalMax, float scoutingMin, float scoutingMax, float productionMin, float productionMax, float cadenceMin, float cadenceMax) =
			label.tier == LabelTier.Small ? (true, .10f, .40f, .18f, .56f, .12f, .42f, .07f, .30f, .34f, .84f, .28f, .80f, .20f, .80f) :
			(false, .28f, .62f, .30f, .72f, .28f, .62f, .18f, .50f, .44f, .91f, .40f, .91f, .50f, 1.50f);
		return label.budgetLevel >= budgetMin && label.budgetLevel <= budgetMax && label.marketingPower >= marketingMin && label.marketingPower <= marketingMax &&
			label.ownedReach >= reachMin && label.ownedReach <= reachMax && label.nationalReach >= nationalMin && label.nationalReach <= nationalMax &&
			label.scoutingAbility >= scoutingMin && label.scoutingAbility <= scoutingMax && label.productionQuality >= productionMin && label.productionQuality <= productionMax &&
			label.releasesPerMonth >= cadenceMin && label.releasesPerMonth <= cadenceMax;
	}

	private static string ProfileFingerprint(AILabel label) => string.Join("|", label.archetype, label.homeRegion, label.homeCityId,
		label.budgetLevel, label.scoutingAbility, label.productionQuality, label.marketingPower, label.ownedReach, label.nationalReach,
		label.riskTolerance, label.artistLoyalty, label.payolaWillingness, label.releasesPerMonth);

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
		Require(artist.performanceDropCount == 2 && artist.lastDropReason == ArtistDropReason.PerformanceExhaustion &&
			artist.prospectMarketStatus == ProspectMarketStatus.Latent && artist.prospectMarketSpellCount == 1 && pool.Count == 0,
			"32a a second performance departure leaves the active market for the reservoir, charged one search spell");
		Require(!ArtistManager.IsProspectSearchEligibleForProbe(artist) && !ArtistManager.IsEligibleForPopulationSigningForProbe(artist, 1964) &&
			artist.lifecycleStatus == ArtistLifecycleStatus.Active,
			"32b the exhausted career is held rather than destroyed: unsearchable while reserved, but still a live career");
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
		// Formation answers unmet hiring demand. It must stay at the base rate while the
		// prospect market can still cover openings, or the early decade moves.
		Require(ArtistManager.CalculateResponsiveAnnualFormationTarget(24, 35, 3628) == 300 &&
			ArtistManager.CalculateResponsiveAnnualFormationTarget(0, 0, 0) == 300 &&
			ArtistManager.CalculateResponsiveAnnualFormationTarget(197, 6, 0) > 1000 &&
			ArtistManager.CalculateResponsiveAnnualFormationTarget(197, 6, 0) <= 1200 &&
			ArtistManager.CalculateResponsiveAnnualFormationTarget(100000, 0, 0) == 1200 &&
			ArtistManager.CalculateResponsiveAnnualFormationTarget(200, 50, 0) <
				ArtistManager.CalculateResponsiveAnnualFormationTarget(200, 10, 0),
			"9 responsive formation is inert while prospect supply covers openings, rises monotonically as it does not, and is bounded");
		float responsiveCarry = 0f; int responsiveFormed = 0;
		for (GameDate date = new(1968, 1, 1); date.year == 1968; date = date.NextDay()) {
			if (!date.IsFriday) continue;
			responsiveFormed += ArtistManager.CalculateCalendarFormationCount(ref responsiveCarry, responsiveFormed, 1200);
		}
		Require(responsiveFormed == 1200, "9 calendar formation quota is exact at the responsive ceiling");
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
		SimulatedArtist agedGroup = NewArtist(); agedGroup.type = ArtistType.Band;
		agedGroup.members.Add(new Musician("group-lead", "Lead", "Probe", true, 1920) { isLeadVocalist = true });
		SimulatedArtist agedSolo = NewArtist(); agedSolo.type = ArtistType.SoloMale;
		agedSolo.members.Add(new Musician("solo-lead", "Lead", "Probe", true, 1920) { isLeadVocalist = true });
		Require(ArtistManager.ClassifyTerminalLifecycleForProbe(agedGroup, 1960) == ArtistLifecycleStatus.Disbanded &&
			ArtistManager.ClassifyTerminalLifecycleForProbe(agedSolo, 1960) == ArtistLifecycleStatus.Retired,
			"15a aged-out acts classify as group disbandment and solo retirement");

		SimulatedArtist youngGroup = NewArtist(); youngGroup.type = ArtistType.Band;
		youngGroup.members.Add(new Musician("young-group-lead", "Lead", "Probe", true, 1938) { isLeadVocalist = true });
		SimulatedArtist youngSolo = NewArtist(); youngSolo.type = ArtistType.SoloMale;
		youngSolo.members.Add(new Musician("young-solo-lead", "Lead", "Probe", true, 1938) { isLeadVocalist = true });
		Require(!ArtistManager.IsTerminalExitEarned(youngGroup, 1960) && !ArtistManager.IsTerminalExitEarned(youngSolo, 1960) &&
			ArtistManager.ClassifyTerminalLifecycleForProbe(youngGroup, 1960) == ArtistLifecycleStatus.Inactive,
			"15b a young band is spared on exactly the same terms as a young solo act");

		youngGroup.prospectMarketSpellCount = 2; youngSolo.prospectMarketSpellCount = 3;
		Require(!ArtistManager.IsTerminalExitEarned(youngGroup, 1960) && ArtistManager.IsTerminalExitEarned(youngSolo, 1960) &&
			ArtistManager.ClassifyTerminalLifecycleForProbe(youngSolo, 1960) == ArtistLifecycleStatus.Retired,
			"15c repeated rejection earns a terminal exit at the third completed search spell, not the second");
	}

	private static void ProbeTerminalSigningAndReleaseGuards() {
		SimulatedArtist artist = NewArtist(); artist.lifecycleStatus = ArtistLifecycleStatus.Retired; artist.careerState = CareerState.Retired; artist.isActive = false;
		Require(!ArtistManager.IsEligibleForPopulationSigningForProbe(artist, 1) && !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist),
			"16 inactive/terminal artists cannot sign or release");
	}

	private static void ProbeVacancyDenominatedHiringDemand() {
		AILabel major = NewScoutingLabel(40); major.tier = LabelTier.Major;
		major.SetOperatingRosterTarget(9, LabelOperatingTargetReason.LaunchPopulation, 0);
		major.roster.Add(NewArtist("major-1"));
		AILabel small = NewScoutingLabel(5); small.tier = LabelTier.Small;
		small.SetOperatingRosterTarget(1, LabelOperatingTargetReason.RuntimeBootstrap, 0);
		AILabel filled = NewScoutingLabel(5); filled.roster.Add(NewArtist("filled-1")); filled.SetOperatingRosterTargetFromCurrent();
		AILabel unaffordable = NewScoutingLabel(5, 0f);
		unaffordable.SetOperatingRosterTarget(4, LabelOperatingTargetReason.RuntimeBootstrap, 0);
		Require(ArtistManager.GetAffordableHiringVacancies(major) == 8 && ArtistManager.GetAffordableHiringVacancies(small) == 1 &&
			ArtistManager.GetAffordableHiringVacancies(filled) == 0 && ArtistManager.GetAffordableHiringVacancies(unaffordable) == 0,
			"70a hiring demand counts unfilled slots, so eight vacancies no longer read as the same demand as one");
		Require(ArtistManager.CalculateProspectActivationCount(20, 0,
				ArtistManager.GetAffordableHiringVacancies(major) + ArtistManager.GetAffordableHiringVacancies(small)) == 9,
			"70b the activation budget is the slot count the market can actually pay for");
	}

	private static void ProbeExperiencedTalentReservoir() {
		SimulatedArtist veteran = NewArtist("veteran"); veteran.careerState = CareerState.Dropped; veteran.contractSequence = 2;
		veteran.prospectMarketStatus = ProspectMarketStatus.Seeking; veteran.prospectSeekingWeeks = 77;
		veteran.members.Add(new Musician("veteran-lead", "Lead", "Probe", true, 1938) { isLeadVocalist = true });
		Require(ArtistManager.AdvanceProspectSearchWeekForProbe(veteran) && veteran.prospectMarketStatus == ProspectMarketStatus.Latent &&
			veteran.prospectMarketSpellCount == 1 && veteran.lifecycleStatus == ArtistLifecycleStatus.Active && veteran.isActive,
			"71a a prior-contract career completes search spells instead of running an inactivity clock it cannot return from");
		Require(!ArtistManager.IsProspectSearchEligibleForProbe(veteran) && !ArtistManager.IsTerminalExitEarned(veteran, 1964),
			"71b reserved talent is held off the market until activation returns it, and one spell does not end a career");
		veteran.prospectMarketStatus = ProspectMarketStatus.Seeking; veteran.prospectSeekingWeeks = 0;
		Require(ArtistManager.IsProspectSearchEligibleForProbe(veteran) && ArtistManager.IsEligibleForPopulationSigningForProbe(veteran, 500),
			"71c activation returns an experienced free agent to the searchable market on the same terms as a first-timer");

		// Rotation is what stops the reservoir being a one-way pen: without it, once
		// seeking saturates the vacancy budget nothing is ever activated, so nothing is
		// ever seen by scouting and nothing can ever complete another spell either.
		Require(ArtistManager.GetLatentRotationWeeksForProbe() > 0 &&
			!ArtistManager.ShouldRotateLatentProspectForProbe(ArtistManager.GetLatentRotationWeeksForProbe() - 1) &&
			ArtistManager.ShouldRotateLatentProspectForProbe(ArtistManager.GetLatentRotationWeeksForProbe()),
			"71d a rested latent act returns to the market on its own clock rather than waiting to be sent for");
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
		SimulatedArtist exhausted = NewArtist("no-third"); exhausted.performanceDropCount = 2; exhausted.careerState = CareerState.Dropped;
		exhausted.lastDropReason = ArtistDropReason.PerformanceExhaustion; exhausted.prospectMarketStatus = ProspectMarketStatus.Latent;
		Require(!ArtistManager.IsEligibleForPopulationSigningForProbe(exhausted, 500),
			"46 exhausted artists cannot walk into a third comeback: only activation can offer them back to the market");
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

	private static void ProbeMidTierPromotionBoundary() {
		AILabel candidate = NewScoutingLabel(12, 100000f);
		candidate.tier = LabelTier.Independent;
		candidate.status = LabelStatus.Stable;
		candidate.monthsActive = 19;
		candidate.sustainedCapabilityQuarters = 4;
		candidate.ownedReach = .55f;
		candidate.nationalReach = .50f;
		candidate.marketingPower = .60f;
		candidate.lastMonthlyProfit = 1000f;
		candidate.consecutiveLossMonths = 0;
		for (int index = 0; index < 6; index++) candidate.roster.Add(NewArtist($"mid-promotion-{index}"));

		Require(LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 2),
			"68a a mature, scaled, charting, profitable Independent with runway qualifies for MidTier");
		candidate.monthsActive = 18;
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 2),
			"68b the former second-quarter capability-only promotion wave is blocked by operating age");
		candidate.monthsActive = 19; candidate.sustainedCapabilityQuarters = 3;
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 2),
			"68c fewer than four sustained capability quarters cannot promote");
		candidate.sustainedCapabilityQuarters = 4;
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 1),
			"68d capability and reach without two recent charting records cannot promote");
		candidate.roster.RemoveAt(candidate.roster.Count - 1);
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 2),
			"68e fewer than six rostered artists cannot promote into the large-independent tier");
		candidate.roster.Add(NewArtist("mid-promotion-restored")); candidate.lastMonthlyProfit = -1f;
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 2),
			"68f an unprofitable Independent cannot promote");
		candidate.lastMonthlyProfit = 1000f; candidate.cashReserves = candidate.GetMonthlyOverhead() * 5f;
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 2),
			"68g fewer than six months of runway cannot promote");

		// Section 28: the dependent-hitmaker route. A label with low owned reach and high
		// distributor dependency -- which the organic route rejects -- still reaches MidTier on a
		// strong sustained chart-and-roster footprint (Stax/A&M on a major's P&D deal).
		candidate.ownedReach = .28f;
		candidate.activeDeal = new DistributionDeal { reachGranted = .60f };
		while (candidate.roster.Count < 8) candidate.roster.Add(NewArtist($"mid-dep-{candidate.roster.Count}"));
		candidate.cashReserves = candidate.GetMonthlyOverhead() * 6f;
		Require(candidate.DistributionDependency >= 0.35f && candidate.ownedReach < 0.50f,
			"68h setup: the dependent-hitmaker candidate is genuinely low-reach and high-dependency");
		Require(LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 4),
			"68i a distributor-dependent hitmaker with a strong chart-and-roster footprint promotes without owning national reach");
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 3),
			"68j the dependent route needs the stronger sustained charting bar, not the base two records");
		candidate.roster.RemoveAt(candidate.roster.Count - 1);
		Require(!LabelLifecycleManager.IsIndependentReadyForMidTier(candidate, 4),
			"68k the dependent route needs the larger roster footprint as well");
	}

	private static void ProbeCompetitiveLabelExitBoundary() {
		AILabel candidate = NewScoutingLabel(6, 100000f);
		candidate.populationOrigin = LabelPopulationOrigin.LaunchPopulation;
		candidate.tier = LabelTier.Independent;
		candidate.status = LabelStatus.Stable;
		candidate.monthsActive = 9;
		candidate.lastMonthlyProfit = -1f;
		candidate.consecutiveLossMonths = 1;
		float weakChance = LabelLifecycleManager.GetCompetitiveExitChance(candidate, 0);
		float oneChartChance = LabelLifecycleManager.GetCompetitiveExitChance(candidate, 1);
		Require(weakChance > 0f && oneChartChance > 0f && oneChartChance < weakChance &&
			LabelLifecycleManager.GetCompetitiveExitChance(candidate, 2) == 0f,
			"69a mature zero/one-chart labels face graduated review while two recent charts are the safe harbor");

		candidate.lastMonthlyProfit = 100f;
		float profitableChance = LabelLifecycleManager.GetCompetitiveExitChance(candidate, 0);
		Require(profitableChance > 0f && profitableChance < weakChance,
			"69b positive monthly profit reduces but does not erase no-demand competitive pressure");

		candidate.status = LabelStatus.Dying;
		candidate.lastMonthlyProfit = -1f;
		candidate.cashReserves = candidate.GetMonthlyOverhead() * 5f;
		float distressedChance = LabelLifecycleManager.GetCompetitiveExitChance(candidate, 0);
		Require(distressedChance > weakChance && distressedChance <= .50f,
			"69c status and runway raise competitive exit pressure within the hard cap");

		candidate.tier = LabelTier.Major;
		Require(LabelLifecycleManager.GetCompetitiveExitChance(candidate, 0) == 0f,
			"69d Majors remain exempt from the marginal-label competition review");
		candidate.tier = LabelTier.Independent;
		candidate.populationOrigin = LabelPopulationOrigin.RuntimeFounded;
		candidate.monthsActive = 17;
		Require(LabelLifecycleManager.GetCompetitiveExitChance(candidate, 0) == 0f,
			"69e a runtime founder retains an eighteen-month market-entry runway");
		candidate.monthsActive = 18;
		Require(LabelLifecycleManager.GetCompetitiveExitChance(candidate, 0) > 0f,
			"69f competitive review begins after the runtime market-entry runway");

		float first = LabelLifecycleManager.GetCompetitiveExitRoll(1001UL, "label_probe", 1961, 3);
		float repeat = LabelLifecycleManager.GetCompetitiveExitRoll(1001UL, "label_probe", 1961, 3);
		float nextQuarter = LabelLifecycleManager.GetCompetitiveExitRoll(1001UL, "label_probe", 1961, 6);
		Require(first >= 0f && first < 1f && first == repeat && first != nextQuarter,
			"69g competitive review uses a deterministic isolated quarterly roll");
	}

	private static void ProbeEarnedNationalReachBoundaries() {
		Require(Math.Abs(CompetitorManager.CalculateNationalReachAfterSelfBuiltGain(.20f, .008f, .70f) - .208f) < .000001f &&
			Math.Abs(CompetitorManager.CalculateNationalReachAfterSelfBuiltGain(.699f, .008f, .70f) - .70f) < .000001f &&
			Math.Abs(CompetitorManager.CalculateNationalReachAfterSelfBuiltGain(.40f, -.10f, .70f) - .40f) < .000001f &&
			Math.Abs(CompetitorManager.CalculateNationalReachAfterSelfBuiltGain(.80f, .008f, .70f) - .80f) < .000001f,
			"72a self-built national reach grows by the configured monthly step, respects its ceiling, and cannot regress");
		Require(Math.Abs(CompetitorManager.CalculateNationalReachAfterCompletedDeal(.20f, .40f, .25f, .80f) - .30f) < .000001f &&
			Math.Abs(CompetitorManager.CalculateNationalReachAfterCompletedDeal(.78f, .80f, .25f, .80f) - .80f) < .000001f &&
			Math.Abs(CompetitorManager.CalculateNationalReachAfterCompletedDeal(.35f, -.50f, .25f, .80f) - .35f) < .000001f,
			"72b a completed deal retains a bounded fraction of granted reach without allowing a negative grant to reduce reach");
		var client = new AILabel {
			nationalReach = .20f,
			activeDeal = new DistributionDeal { reachGranted = .45f }
		};
		Require(Math.Abs(client.effectiveNationalReach - .65f) < .000001f,
			"72c an active distributor adds temporary national reach without mutating the client's permanent field");
		client.activeDeal = null;
		Require(Math.Abs(client.effectiveNationalReach - .20f) < .000001f,
			"72d borrowed national reach ends with the deal while permanent reach remains");
		client.distributionRegions = new[] { "eastcoast" };
		string[] granted = CompetitorManager.GetGrantedDistributionRegions(client,
			new[] { "eastcoast", "westcoast", "deepsouth", "westcoast" });
		Require(granted.SequenceEqual(new[] { "westcoast", "deepsouth" }),
			"72e a distribution deal grants the distributor's full new network rather than intersecting it with the client's existing strong market");
	}

	private static void ProbeEarnedReachDemandScale() {
		float regional = ChartSimulator.CalculateLiveLabelDemandScale(.20f, .10f);
		float established = ChartSimulator.CalculateLiveLabelDemandScale(.55f, .50f);
		float national = ChartSimulator.CalculateLiveLabelDemandScale(.90f, .90f);
		Require(Math.Abs(regional - .595f) < .000001f && Math.Abs(established - .9275f) < .000001f &&
			Math.Abs(national - 1.20f) < .000001f && regional < established && established < national,
			"73 live Single demand rises continuously with earned distribution and national reach while respecting the national-label ceiling");
	}

	private static void ProbePersistentRegionalDealEvidence() {
		var record = new RecordRuntimeData(new Record {
			recordId = "regional_deal_probe",
			labelId = "regional_label",
			format = ReleaseFormat.Single,
			hookStrength = .45f,
			productionQuality = .45f,
			danceability = .45f
		});
		record.peakRegionalBreakoutStrength = .25f;
		record.regionalData["eastcoast"] = new RegionalRecordData("eastcoast") {
			peakBreakoutScore = .23f,
			unitsSoldThisWeek = 0
		};
		record.regionalData["westcoast"] = new RegionalRecordData("westcoast") {
			peakBreakoutScore = .25f,
			unitsSoldThisWeek = 0
		};
		CompetitorManager.RegionalDealEvidence evidence = CompetitorManager.EvaluateRegionalDealEvidence(
			new[] { record }, "regional_label", new[] { "eastcoast" }, .24f);
		var highNationalReachClient = new AILabel { labelId = "regional_label", nationalReach = .85f };
		Require(evidence.HasPersistentRegionalTraction &&
			Math.Abs(evidence.BestStrongRegionPeak - .23f) < .000001f &&
			Math.Abs(evidence.BestAnyRegionPeak - .25f) < .000001f &&
			!evidence.PassesLegacyQualityAndCurrentSalesGate &&
			CompetitorManager.IsPullDealTrigger(highNationalReachClient, evidence),
			"74 persistent observed LocalTraction in any market survives a zero-sales processing week, does not require a static launch-time strong region, and is not closed by an arbitrary national-reach scalar");

		record.regionalData["westcoast"].peakBreakoutScore = .23f;
		evidence = CompetitorManager.EvaluateRegionalDealEvidence(
			new[] { record }, "regional_label", new[] { "eastcoast" }, .24f);
		Require(!evidence.HasPersistentRegionalTraction,
			"74b sub-LocalTraction noise cannot qualify for distribution evidence in any region");
	}

	private static void ProbeRetiredLabelEvidenceLookback() {
		var history = new[] {
			new CompetitorManager.LabelRecordHistoryEntry(48, charted: true, top40: true),
			new CompetitorManager.LabelRecordHistoryEntry(47, charted: true, top40: false),
			new CompetitorManager.LabelRecordHistoryEntry(90, charted: false, top40: false)
		};
		Require(CompetitorManager.CountRecentRetiredRecordEvidence(history, 100, 52,
				requireCharted: true, requireTop40: false) == 1 &&
			CompetitorManager.CountRecentRetiredRecordEvidence(history, 100, 52,
				requireCharted: false, requireTop40: true) == 1 &&
			CompetitorManager.CountRecentRetiredRecordEvidence(history, 100, 52,
				requireCharted: false, requireTop40: false) == 2,
			"75 a retired record remains visible through the inclusive 52-week label-evidence window and expires after it");
	}

	private static void ProbeWeeklyAwarenessAgeDecay() {
		float prePeak = ChartSimulator.ApplyWeeklyAwarenessAgeDecay(.80f, 8);
		float ageNine = ChartSimulator.ApplyWeeklyAwarenessAgeDecay(.80f, 9);
		float ageEighteen = ChartSimulator.ApplyWeeklyAwarenessAgeDecay(.80f, 18);
		float repeated = .80f;
		for (int age = 9; age <= 18; age++)
			repeated = ChartSimulator.ApplyWeeklyAwarenessAgeDecay(repeated, age);
		Require(Math.Abs(prePeak - .80f) < .000001f &&
			Math.Abs(ageNine - .76f) < .000001f &&
			Math.Abs(ageEighteen - .76f) < .000001f &&
			Math.Abs(repeated - (.80f * MathF.Pow(.95f, 10f))) < .000001f,
			"76 awareness receives one post-peak decay step per elapsed week rather than a triangular age exponent");
	}

	private static void ProbeReleaseImprintIdentity() {
		var source = new Record { recordId = "imprint_probe", labelId = "original_imprint" };
		var runtime = new RecordRuntimeData(source);
		source.labelId = "acquiring_owner";
		Require(runtime.releaseLabelId == "original_imprint" && runtime.baseRecord.labelId == "acquiring_owner",
			"77 acquisition may transfer operating ownership without rewriting the immutable release-imprint identity");
	}

	private static void ProbeRegionScaledBreakoutEvidence() {
		// East Coast and Deep South as authored in chart_manager.tscn.
		var eastCoast = new MarketRegion {
			regionId = "eastcoast", population = 52.2f, urbanization = .70f, averageIncome = 1.15f, youthPercentage = .32f
		};
		var deepSouth = new MarketRegion {
			regionId = "deepsouth", population = 15.0f, urbanization = .48f, averageIncome = .78f, youthPercentage = .40f
		};
		float reference = eastCoast.GetRecordBuyingPopulation();
		float eastScale = ChartManager.CalculateRegionalDemandScale(eastCoast.GetRecordBuyingPopulation(), reference);
		float southScale = ChartManager.CalculateRegionalDemandScale(deepSouth.GetRecordBuyingPopulation(), reference);
		Require(Math.Abs(eastScale - 1f) < .000001f && southScale > .21f && southScale < .25f,
			"78 the largest authored market keeps its existing calibration while a roughly quarter-sized market scales to its own buying population");

		// The same share of each local market must yield the same evidence.
		float eastVolume = ChartManager.CalculateBreakoutVolumeInput(3500f, 3000f, eastScale);
		float southVolume = ChartManager.CalculateBreakoutVolumeInput(3500f * southScale, 3000f * southScale, southScale);
		Require(Math.Abs(eastVolume - southVolume) < .000001f,
			"78b equal per-capita regional performance produces equal breakout volume evidence regardless of market size");

		// The historical defect: a genuine Deep South regional hit scored as noise.
		float southHitUnderFlatThresholds = ChartManager.CalculateBreakoutVolumeInput(900f, 800f, 1f);
		float southHitUnderRegionScale = ChartManager.CalculateBreakoutVolumeInput(900f, 800f, southScale);
		Require(southHitUnderFlatThresholds < .25f && southHitUnderRegionScale > .75f &&
			southHitUnderRegionScale > southHitUnderFlatThresholds,
			"78c a regional hit in a smaller market is no longer scored against the largest market's absolute unit thresholds");

		// A degenerate region must not divide by zero or change previous behavior.
		Require(Math.Abs(ChartManager.CalculateRegionalDemandScale(0f, reference) - 1f) < .000001f &&
			Math.Abs(ChartManager.CalculateRegionalDemandScale(reference, 0f) - 1f) < .000001f,
			"78d an unauthored or degenerate region falls back to the unscaled thresholds");
	}

	private static void ProbePerSongDistributionScope() {
		var label = new AILabel {
			labelId = "scoped_deal_label",
			homeRegion = "deepsouth",
			nationalReach = .20f,
			distributionRegions = new[] { "deepsouth" }
		};
		label.distributionStrength = .30f;
		label.activeDeal = new DistributionDeal {
			distributorId = "national_distributor",
			reachGranted = .50f,
			grantedRegions = new[] { "eastcoast", "greatlakes", "westcoast" },
			signedWeek = 40,
			termWeeks = 78
		};
		label.activeDeal.Cover("breakout_single");

		// The record that earned the contract rides the distributor's network.
		Require(label.RecordCoveredByActiveDeal("breakout_single") &&
			label.HasDistributionInRegionForRecord("eastcoast", "breakout_single") &&
			Math.Abs(label.BorrowedReachForRecord("breakout_single") - .50f) < .000001f &&
			Math.Abs(label.EffectiveNationalReachForRecord("breakout_single") - .70f) < .000001f,
			"79 the record whose regional breakout earned a distribution deal receives the distributor's regions and borrowed reach");

		// A back-catalog record released before the deal does not.
		Require(!label.RecordCoveredByActiveDeal("older_catalog_single") &&
			!label.HasDistributionInRegionForRecord("eastcoast", "older_catalog_single") &&
			label.BorrowedReachForRecord("older_catalog_single") == 0f &&
			Math.Abs(label.EffectiveNationalReachForRecord("older_catalog_single") - .20f) < .000001f,
			"79b a deal does not retroactively push the label's existing catalog into the distributor's network");

		// The label's own regions still serve every record, deal or not.
		Require(label.HasDistributionInRegionForRecord("deepsouth", "older_catalog_single") &&
			Math.Abs(label.DistributionStrengthForRecord("older_catalog_single") - .30f) < .000001f,
			"79c owned distribution continues to serve records the contract does not carry");

		// Output released during the term joins the deal.
		label.activeDeal.Cover("released_during_term");
		Require(label.HasDistributionInRegionForRecord("greatlakes", "released_during_term") &&
			Math.Abs(label.EffectiveNationalReachForRecord("released_during_term") - .70f) < .000001f,
			"79d output released while the contract runs goes out through the distributor's network");

		// Termination removes borrowed capability from every record at once.
		label.activeDeal = null;
		Require(!label.HasDistributionInRegionForRecord("eastcoast", "breakout_single") &&
			label.BorrowedReachForRecord("breakout_single") == 0f,
			"79e ending the contract withdraws the distributor's network from the records it carried");
	}

	private static void ProbePhysicalDistributionGovernsShelfStock() {
		var major = new AILabel {
			labelId = "national_major", homeRegion = "eastcoast",
			strongRegions = new[] { "eastcoast" },
			distributionRegions = new[] { "eastcoast", "greatlakes", "deepsouth" }
		};
		major.distributionStrength = .88f;
		var small = new AILabel {
			labelId = "regional_small", homeRegion = "deepsouth",
			strongRegions = new[] { "deepsouth" },
			distributionRegions = new[] { "deepsouth" }
		};
		small.distributionStrength = .26f;

		int majorCovered = ChartSimulator.CalculateInitialRegionalStock(major, "greatlakes", 1f, 1f, "rec");
		int smallUncovered = ChartSimulator.CalculateInitialRegionalStock(small, "greatlakes", 1f, 1f, "rec");
		int smallHome = ChartSimulator.CalculateInitialRegionalStock(small, "deepsouth", 1f, 1f, "rec");

		Require(majorCovered > smallUncovered * 3,
			"80 shelf stock in a region a label does not distribute into is a small fraction of a national label's covered shelf");
		Require(smallHome > smallUncovered,
			"80b a label's own strong home market receives deeper shelf stock than a market it cannot reach");
	}

	private static void ProbeSeededLargeFirmPopulation() {
		var labels = AILabelFactory.GenerateAllLabels(600);
		int majors = labels.Count(label => label.tier == LabelTier.Major);
		int midTier = labels.Count(label => label.tier == LabelTier.MidTier);
		int independents = labels.Count(label => label.tier == LabelTier.Independent);

		// The 1960 market had roughly eight corporate majors and on the order of
		// twenty to twenty-five national independents. The former draw produced about
		// 13 and 98 respectively, which took 85% of chart entries.
		Require(majors >= 4 && majors <= 14,
			"81 the seeded population carries a corporate-major count in the historical range rather than an inflated one");
		Require(midTier >= 10 && midTier <= 40,
			"81b the seeded population carries a national-independent count in the historical range rather than four times it");
		Require(independents > midTier * 3,
			"81c regional independents outnumber national independents in the seeded 1960 market");

		// Motown and Stax must earn MidTier rather than begin there in January 1960.
		AILabel motown = labels.FirstOrDefault(label => label.labelName != null && label.labelName.StartsWith("Motown", StringComparison.Ordinal));
		AILabel stax = labels.FirstOrDefault(label => label.labelName != null && label.labelName.StartsWith("Stax", StringComparison.Ordinal));
		Require(motown != null && motown.tier == LabelTier.Independent &&
			stax != null && stax.tier == LabelTier.Independent,
			"81d firms that were months old or trading under an earlier name in 1960 start below the mature large-independent tier");

		// Every seeded label must satisfy the operating-profile contract.
		Require(labels.All(label => label.riskTolerance > 0f && label.artistLoyalty > 0f),
			"81e every seeded launch label carries a complete operating profile");
	}

	private static void ProbeBreakoutEvidenceRewardsConstrainedDemand() {
		// Two records identical except that one is selling out where its label cannot
		// restock. Under the former form unmetInput was multiplied by volumeInput, so a
		// supply-constrained record -- which has low fulfilled volume by construction --
		// had its own proof cancelled. Backordered demand must now raise evidence.
		float noBackorder = ChartManager.CalculateBreakoutEvidence(
			.45f, .5f, .5f, .4f, .15f, .8f, .7f, unmetInput: 0f);
		float soldOut = ChartManager.CalculateBreakoutEvidence(
			.45f, .5f, .5f, .4f, .15f, .8f, .7f, unmetInput: 1f);
		Require(soldOut > noBackorder && soldOut - noBackorder > .05f,
			"82 proven demand a label cannot fulfil raises regional breakout evidence instead of being cancelled by low fulfilled volume");

		// The envelope must not become a general subsidy: a high-volume incumbent keeps
		// its prior calibration while the low-volume tail is the part that is relieved.
		float incumbentEnvelope = .70f + .30f * .98f;
		float tailEnvelope = .70f + .30f * .61f;
		Require(Math.Abs(incumbentEnvelope - .994f) < .001f && incumbentEnvelope > .99f,
			"82b a high-volume incumbent's evidence envelope is left effectively unchanged");
		Require(tailEnvelope > .88f && tailEnvelope < .89f && tailEnvelope < incumbentEnvelope,
			"82c the low-volume tail is relieved without overtaking high-volume records");

		// Volume must still dominate: it holds the largest single weight and the envelope.
		float lowVolume = ChartManager.CalculateBreakoutEvidence(.10f, .5f, .5f, .4f, .15f, .8f, .7f, .5f);
		float highVolume = ChartManager.CalculateBreakoutEvidence(.90f, .5f, .5f, .4f, .15f, .8f, .7f, .5f);
		Require(highVolume > lowVolume * 1.4f,
			"82d volume remains the dominant breakout input after the envelope is narrowed");

		// Weights are a partition, so a saturated record scores exactly 1.
		Require(Math.Abs(ChartManager.CalculateBreakoutEvidence(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f) - 1f) < .000001f,
			"82e the breakout input weights remain a partition of unity");
	}

	private static void ProbeArtistReleaseHistoryCountsOnce() {
		SimulatedArtist artist = NewArtist("release-history");
		CompetitorManager.RecordArtistRelease(artist, "rec_a", ReleaseFormat.Single);
		Require(artist.releaseHistory.Count == 1 && artist.totalReleases == 1 &&
			artist.releasedSingleIds.Contains("rec_a") && artist.weeksSinceLastRelease == 0,
			"83 one live release appends exactly one project-history entry");

		CompetitorManager.RecordArtistRelease(artist, "rec_b", ReleaseFormat.Single);
		CompetitorManager.RecordArtistRelease(artist, "rec_c", ReleaseFormat.Single);
		Require(artist.releaseHistory.Count == 3,
			"83b three releases reach the GenreSupplyService project-history cap in three, not two");

		// Guard the boundary the double count actually moved.
		Require(Math.Min(2, 3) * .03f < Math.Min(3, 3) * .03f,
			"83c project-identity retention still distinguishes a second release from a third");
	}

	private static void ProbePromoCannibalizationChargedOnce() {
		// A label with no Album-component evidence still carries the whole modelled
		// diversion: nothing has been priced in for it yet, so 1960 is unmoved.
		Require(Math.Abs(CompetitorManager.CalculateChargedPromoCannibalization(100000f, 0f) - 100000f) < .001f,
			"84 an Album-component lane with no evidence charges the full modelled promo cannibalization");

		// A label whose projection is fully memory-driven has the diversion inside that
		// projection already, so charging it again would be the double count.
		Require(CompetitorManager.CalculateChargedPromoCannibalization(100000f, 1f) == 0f,
			"84b a fully confident Album-component projection charges no additional cannibalization");

		// The relief is monotone in confidence and never exceeds the modelled loss, so it
		// is a reallocation between two accountings of one effect, not a new subsidy.
		float low = CompetitorManager.CalculateChargedPromoCannibalization(100000f, .2f);
		float high = CompetitorManager.CalculateChargedPromoCannibalization(100000f, .7f);
		Require(high < low && low < 100000f && high > 0f,
			"84c charged cannibalization falls monotonically as Album-component confidence rises");

		// Confidence outside [0,1] and a negative modelled loss must not invent a credit.
		Require(CompetitorManager.CalculateChargedPromoCannibalization(100000f, 1.4f) == 0f &&
			Math.Abs(CompetitorManager.CalculateChargedPromoCannibalization(100000f, -.3f) - 100000f) < .001f &&
			CompetitorManager.CalculateChargedPromoCannibalization(-5000f, .5f) == 0f,
			"84d out-of-range confidence and negative modelled loss are clamped rather than inverted");

		// The measured failure this repairs: a Major's 1969 inputs. Charged in full the
		// promo strategy is not viable and the Album ships with no Single; charged once
		// against a half-confident component projection it survives, which is what keeps
		// the Singles pipeline alive as the LP share rises.
		const float promoTerms = 3685f + 52751f + 68956f;
		const float modelledLoss = 141465f;
		Require(promoTerms - modelledLoss < 0f,
			"84e the measured 1969 Major promo proposition is non-viable when cannibalization is charged twice");
		Require(promoTerms - CompetitorManager.CalculateChargedPromoCannibalization(modelledLoss, .51f) > 0f,
			"84f the same proposition is viable once the component projection's share is not charged again");
	}

	private static void ProbePromoRecruitmentMatchesDiversionTerms() {
		// Recruitment (CalculatePromoAlbumSynergyGain) and diversion are the two sides of
		// the promo Single's Album-unit effect. Diversion is substitutionK (1.00) * album
		// demand * shelf overlap (0.60); recruitment is PromoAlbumConversionK * album
		// demand * awareness headroom. With the base conversion now at or above substitutionK,
		// recruitment exceeds diversion at real awareness headroom yet stays dilutive at the floor.
		const float albumDemand = .60f, singleUnits = 1000f, margin = 10f;
		float diverted = Math.Min(1.00f * albumDemand, .60f) * .60f * singleUnits * margin;
		float recruitUnknownAct = CompetitorManager.CalculatePromoAlbumSynergyGain(albumDemand, 1f, singleUnits, margin);
		float recruitEstablishedAct = CompetitorManager.CalculatePromoAlbumSynergyGain(albumDemand, 0f, singleUnits, margin);
		Require(recruitUnknownAct > diverted,
			"85 a hit promo Single for an unknown act now recruits more Album units than it diverts");
		Require(recruitEstablishedAct < diverted,
			"85b an established act's promo Single stays mildly dilutive, preserving the awareness-gated crossover rather than a flat subsidy");
		// The former 0.50 base put recruitment strictly below diversion at every headroom.
		Require(recruitUnknownAct > .50f * albumDemand * 1f * singleUnits * margin,
			"85c raising the base conversion to at least substitutionK lifts recruitment above the former negative-definite half-measure");
	}

	private static void ProbeLoweredLocalTractionAdmitsStrandedBand() {
		// LocalTraction, and the discovery basin it opens, began at 0.24, stranding the
		// 0.18-0.24 breakout band: above the 0.18 collapse floor so not dying, below the
		// activation so earning no reinforcement. Lowering activation to 0.20 admits the
		// upper part of that band to the discovery ramp.
		Require(ChartManager.CalculateBreakoutDiscoveryStrength(0.21f) > 0f &&
			ChartManager.CalculateBreakoutDiscoveryStrength(0.20f) == 0f,
			"86 a record just above the lowered 0.20 LocalTraction activation now earns self-reinforcing discovery");
		Require(ChartManager.CalculateBreakoutDiscoveryStrength(0.19f) == 0f,
			"86b a record below the activation earns none, so the sub-collapse-floor population is unchanged");
		// The ramp keeps its 0.40-wide shape: monotone, zero at the anchor, saturating a
		// full 0.40 above it, so a RegionalBreakout-strength incumbent earns the same
		// reinforcement it already did (and is separately capped at runtime).
		Require(ChartManager.CalculateBreakoutDiscoveryStrength(0.40f) > ChartManager.CalculateBreakoutDiscoveryStrength(0.30f) &&
			Math.Abs(ChartManager.CalculateBreakoutDiscoveryStrength(0.60f) - 1f) < .000001f,
			"86c the discovery-strength ramp stays monotone and saturates a fixed 0.40 above the activation");
	}

	private static void ProbeConsolidationGate() {
		// The late-decade major-consolidation gate is split from its random roll so it can be
		// asserted directly. Arguments below use the shipped defaults: start year 1966, cap 40,
		// requireCharted on, allowNationalMidTier off. A Major absorbing a charted independent
		// inside the window and under the cap is the one eligible shape.
		Require(CompetitorManager.IsConsolidationEligible(1968, 1966,
			LabelTier.Major, false, LabelTier.Independent, true, true, false, 5, 40),
			"87 a Major absorbing a charted independent inside the window and under the cap is eligible");

		// Before the window nothing consolidates, which preserves the calibrated early-decade
		// major share and keeps pre-1966 realizations unperturbed by the lever.
		Require(!CompetitorManager.IsConsolidationEligible(1965, 1966,
			LabelTier.Major, false, LabelTier.Independent, true, true, false, 0, 40),
			"87b no absorption fires before the consolidation start year");

		// Only a Major -- or, when the flag is on, a genuinely national MidTier -- may acquire.
		// A standalone MidTier or Independent absorbing is the wrong-tier noise the old ungated
		// path produced (indie-on-indie, even small-on-major).
		Require(!CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.MidTier, false, LabelTier.Independent, true, true, false, 0, 40) &&
			!CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.Independent, true, LabelTier.Boutique, true, true, false, 0, 40),
			"87c a MidTier (national flag off) or Independent acquirer is not an eligible consolidator");
		Require(CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.MidTier, true, LabelTier.Independent, true, true, true, 0, 40) &&
			!CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.MidTier, false, LabelTier.Independent, true, true, true, 0, 40),
			"87d a national MidTier acquires only when the flag is on and it is genuinely national");

		// Section 28: the historically dominant consolidation was majors absorbing high-volume
		// MidTier labels (WB->Atlantic), so a MidTier client IS an eligible target. Only a Major
		// client -- a peer, not an acquisition target -- is excluded.
		Require(CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.Major, false, LabelTier.MidTier, true, true, false, 0, 40) &&
			!CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.Major, false, LabelTier.Major, true, true, false, 0, 40),
			"87e a Major can absorb a MidTier client (WB->Atlantic) but never another Major");

		// Majors bought success: an uncharted client is ineligible while requireCharted holds,
		// and relaxing that flag admits it.
		Require(!CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.Major, false, LabelTier.Independent, false, true, false, 0, 40) &&
			CompetitorManager.IsConsolidationEligible(1968, 1966,
				LabelTier.Major, false, LabelTier.Independent, false, false, false, 0, 40),
			"87f a client must have charted when requireCharted is set, and need not when it is cleared");

		// The decade cap bounds the wave so it cannot crush the independent imprint tail that
		// breadth and the section 1 tier guardrail require.
		Require(!CompetitorManager.IsConsolidationEligible(1969, 1966,
				LabelTier.Major, false, LabelTier.Independent, true, true, false, 40, 40),
			"87g absorption stops once the decade cap is reached");
	}

	private static void ProbeSubsidiaryAbsorptionRetainsLabel() {
		// Subsidiary model (section 24): absorption does not shut the label down. It folds the
		// terminated deal's borrowed reach into permanent owned reach, unions the parent's
		// distribution regions in so national coverage persists, and rolls ownership up to the
		// parent -- while the label keeps its operational status, roster and release imprint so
		// it keeps charting as a Major-owned subsidiary.
		AILabel parent = new() {
			labelId = "major-parent", tier = LabelTier.Major,
			distributionRegions = new[] { "westcoast", "greatlakes" }, roster = new List<SimulatedArtist>()
		};
		AILabel client = new() {
			labelId = "indie-sub", tier = LabelTier.Independent, status = LabelStatus.Rising,
			ownedReach = 0.10f, distributionRegions = new[] { "eastcoast" },
			roster = new List<SimulatedArtist> { NewArtist("sub-artist") },
			activeDeal = new DistributionDeal { distributorId = "major-parent", reachGranted = 0.50f }
		};

		CompetitorManager.ApplySubsidiaryAbsorption(client, parent);

		Require(client.IsSubsidiary && client.ownerLabelId == "major-parent",
			"88 an absorbed label is marked a subsidiary of its acquiring parent");
		Require(client.activeDeal == null && Math.Abs(client.ownedReach - 0.60f) < 0.0001f,
			"88b the terminated deal's borrowed reach is folded into permanent owned reach");
		Require(client.distributionRegions.Length == 3 && client.distributionRegions.Contains("eastcoast") &&
			client.distributionRegions.Contains("westcoast") && client.distributionRegions.Contains("greatlakes"),
			"88c the parent's distribution regions are unioned into the subsidiary's own");
		Require(client.IsActive && client.status == LabelStatus.Rising && client.CurrentRosterSize == 1,
			"88d the subsidiary keeps operating -- status, roster and imprint retained, not shut down");
	}

	private static void ProbeDependentHitmakerArchetype() {
		// Section 27: a minority of runtime Independents are dependent "Stax" hitmakers -- strong
		// production but low owned reach, so they chart through a major's network and stay
		// absorbable -- while the rest of the dependent population is unchanged.
		int flagged = 0; const int total = 200;
		for (int i = 0; i < total; i++) {
			AILabel l = NewRuntimeProfileProbeLabel("dh-" + i, LabelTier.Independent);
			RuntimeLabelProfileFactory.Initialize(l, null, 30 + i, new GameDate(1963, 6, 1), 4242UL);
			if (l.distributionDependentHitmaker) {
				flagged++;
				Require(l.productionQuality >= 0.60f && l.ownedReach <= 0.40f && RuntimeLabelProfileFactory.HasCompleteOperatingProfile(l),
					"89 a dependent hitmaker has strong production and low owned reach within a complete profile");
			}
		}
		Require(flagged > 0 && flagged < total,
			"89b dependent hitmakers are a nonempty minority of runtime Independents, neither all nor none");

		AILabel a = NewRuntimeProfileProbeLabel("dh-fixed", LabelTier.Independent);
		AILabel b = NewRuntimeProfileProbeLabel("dh-fixed", LabelTier.Independent);
		RuntimeLabelProfileFactory.Initialize(a, null, 55, new GameDate(1963, 6, 1), 4242UL);
		RuntimeLabelProfileFactory.Initialize(b, null, 55, new GameDate(1963, 6, 1), 4242UL);
		Require(a.distributionDependentHitmaker == b.distributionDependentHitmaker,
			"89c the dependent-hitmaker roll is deterministic for a fixed seed and identity");

		AILabel small = NewRuntimeProfileProbeLabel("dh-small", LabelTier.Small);
		RuntimeLabelProfileFactory.Initialize(small, null, 55, new GameDate(1963, 6, 1), 4242UL);
		Require(!small.distributionDependentHitmaker,
			"89d the dependent-hitmaker archetype is confined to the Independent tier");
	}


	private static void Require(bool condition, string message) {
		if (!condition) throw new InvalidOperationException("D6 probe failed: " + message);
	}

	private static void RequireThrows<TException>(Action action, string message) where TException : Exception {
		try { action(); }
		catch (TException) { return; }
		throw new InvalidOperationException("D6 probe failed: " + message);
	}
}
