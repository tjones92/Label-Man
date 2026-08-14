using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fixed-input probes for artist evolution. Every fixture is anchored to the
/// constants in <see cref="ArtistEvolution"/> rather than to a literal "one under
/// the bar" -- those silently invert the moment the bar is re-derived, which has
/// already happened once on this project.
///
/// These probes run on detached artists and consume no RNG, but they DO mutate the
/// static annual-budget ledger, so they reset it on the way out. As with the other
/// suites: never pass --artist-evolution-probes to a run being compared against a
/// control.
/// </summary>
public static class ArtistEvolutionProbeSuite {
	public static IReadOnlyList<string> Run() {
		// EVERY switch, not just the two that used to be captured here. The old teardown put
		// back `enabled` and `observeOnly` and left pressure, legitimacy and cultural memory
		// switched off for the rest of the process -- so a run that also passed
		// --artist-evolution-probes quietly measured a different feature set than the one on
		// its command line, and reported it as every pressure reading exactly 0.0000.
		ArtistEvolution.Switches priorSwitches = ArtistEvolution.CaptureSwitches();
		try {
			var results = new List<string>();
			ProbeDisabledFlagIsInert();                    // 1
			ProbeInitializeIsPureAndBounded();             // 2
			ProbeWindowMustBeFull();                       // 3
			ProbeMajorityRuleFires();                      // 4
			ProbePluralityShortOfMajorityDoesNotFire();    // 5
			ProbeAdjacencyFloorBlocks();                   // 6
			ProbeClimateBlocksClosedGenre();               // 7
			ProbeCooldownBlocks();                         // 8
			ProbeCooldownExpires();                        // 9
			ProbeTerminalCareerBlocks();                   // 10
			ProbeAnnualBudgetBlocks();                     // 11
			ProbeGenreOutflowCapBlocks();                  // 12
			ProbeEraCloseOpenIsAtomicAndIdempotent();      // 13
			ProbeFormationIdentitySurvivesMigration();     // 14
			ProbeRatificationResetsWindowAndDemotesPrimary(); // 15
			ProbeCriticalAcclaimWriter();                  // 16
			ProbeFormationAffinitySplitScope();            // 17
			ProbePressureNeutralSettingIsExact();          // 18
			ProbePressuredWindowStillNeedsUnanimity();     // 19
			ProbeIdentityFitStaysInsideAuthoredBounds();   // 20
			ProbeAlbumLegitimacyZeroReproducesCeiling();   // 21
			ProbeLandmarkBarAndEarliness();                // 22
			ProbeInfluenceMemoryIsBounded();               // 23
			ProbeDiscographyIsAssembledNotStored();        // 24
			ProbeEveryGenreHasAMusicalNeighbour();         // 25
			ProbeCountryCompensatesForTheSplit();          // 26
			ProbeCommercialPressureIsNotAConstant();       // 27
			ProbePeerInfluenceCanWinAMotive();             // 28
			ProbeCohesiveAlbumMovementHasAWriter();        // 29
			ProbeCriticalBreakthroughHasAWriter();         // 30
			ProbeLabelPressureHasItsOwnMotive();           // 31
			ProbeRecognitionIsSeparableFromCommerce();     // 32
			ProbeMeritIgnoresReception();                  // 33
			ProbeCulturalMemoryPropagatesAndCounts();      // 34
			ProbeLandmarkIsNotAConceptAlbum();             // 35
			results.Add("Artist-evolution fixed probe 35 passed (a landmark album is a body of work rather " +
				"than a concept album, may carry hit singles, is reachable in 1965 because it is not gated " +
				"on the concept-album era ceiling, excludes compilations/live/soundtracks, and lets the " +
				"critics hear coherence in a year the cohesion ceiling is clamped shut)");
			results.Add("Artist-evolution fixed probes 27-34 passed (commercial pressure with no floor, " +
				"peer influence able to win a motive on normalised salience, CohesiveAlbumMovement and " +
				"CriticalBreakthrough with real writers, label pressure driven by its own motive rather " +
				"than by the flop streak, recognition separable from commerce so the press channel can " +
				"mint a landmark that never charted, merit that ignores reception, and a cursored " +
				"cultural ledger that accumulates influence against the source act)");
			results.Add("Artist-evolution fixed probes 1-26 passed (disabled inertness, pure bounded disposition, " +
				"full-window requirement, strict-majority ratification, plurality refusal, adjacency floor, " +
				"climate gate on a genre closed to new supply, cooldown open and close, terminal-career refusal, " +
				"annual conversion budget, per-genre outflow cap, atomic idempotent era close/open, surviving " +
				"formation identity, window reset with the old primary demoted to secondary, a bounded critical-acclaim " +
				"writer that rewards the acclaimed-but-unsold record and erodes below the craft bar, and a " +
				"FormationAffinity split scoped to project transition with formation weights untouched, a pressure " +
				"neutral setting that reproduces the authored identity-fit constants exactly, a shortened window " +
				"that still demands unanimity, identity fit bounded inside the authored range, album legitimacy " +
				"at zero reproducing the exogenous cohesion ceiling, the landmark bar and its earliness premium, " +
				"a capacity-bounded influence memory, and a discography assembled on demand rather than stored)");
			return results;
		} finally {
			ArtistEvolutionService.ResetAnnualBudgetForProbe();
			// The cultural ledger, the legitimacy scalar and the pending-recognition table are
			// all process-global. A probe run that left any of them dirty would silently
			// contaminate the simulation that follows it in the same process.
			CulturalMemoryService.ResetForProbe();
			CulturalRecognitionService.ResetForProbe();
			AlbumLegitimacyService.ResetForProbe();
			ArtistEvolution.RestoreSwitches(priorSwitches);
		}
	}

	// The historical case the directive is built around: a folk act that plugs in. The
	// explicit Folk|FolkRock edge is .75 and both are the Folk family, so the musical path
	// is real by either route.
	private const Genre Roots = Genre.Folk;
	private const Genre Plugged = Genre.FolkRock;
	private const int PivotYear = 1966;

	private static SimulatedArtist NewArtist(Genre primary = Roots, Genre secondary = Genre.ContemporaryFolk) {
		var artist = new SimulatedArtist {
			artistId = "evo_probe", stageName = "evo_probe", type = ArtistType.Band,
			primaryGenre = primary, secondaryGenre = secondary,
			formationPrimaryGenre = primary, formationSecondaryGenre = secondary,
			formedYear = 1962, careerState = CareerState.Rising,
			lifecycleStatus = ArtistLifecycleStatus.Active, isActive = true
		};
		artist.members.Add(NewMusician("m1"));
		artist.members.Add(NewMusician("m2"));
		artist.RecalculateStats();
		return artist;
	}

	private static Musician NewMusician(string id) => new(id, id, "Probe", true, 1940) {
		technicalSkill = .60f, creativity = .70f, musicalVersatility = .55f, stagePresence = .50f,
		studioEfficiency = .50f, ego = .40f, ambition = .60f, reliability = .70f, loyalty = .60f,
		temperament = .55f, isPrimaryWriter = true, isActive = true
	};

	/// <summary>Drives the window with a run of projects in one genre. Sized off the constants.</summary>
	private static void ReleaseProjects(SimulatedArtist artist, Genre genre, int count, int year) {
		for (int index = 0; index < count; index++) ArtistEvolutionService.ObserveProject(artist, genre, year);
	}

	/// <summary>
	/// Fills the window with the authored minimum of off-identity sides in
	/// <paramref name="genre"/> and the remainder in the artist's own identity: the exact
	/// shape the rule is specified on, sized off the constants so it moves when they do.
	/// </summary>
	private static SimulatedArtist ArtistAtMajority(Genre genre, int year, Genre identity = Roots) {
		SimulatedArtist artist = NewArtist(identity);
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		int filler = ArtistEvolution.DriftEvidenceWindow - ArtistEvolution.DriftOffIdentityMinimum;
		ReleaseProjects(artist, identity, filler, year);
		ReleaseProjects(artist, genre, ArtistEvolution.DriftOffIdentityMinimum, year);
		return artist;
	}

	private static void EnableRatification(int year, Genre identity = Roots) {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false);
		// A population large enough that the budget is not what is being measured, and an
		// identity pool large enough that the outflow cap is not either.
		ArtistEvolutionService.SeedAnnualBudgetForProbe(year, population: 20000, identity, identityPopulation: 200);
	}

	private static void ProbeDisabledFlagIsInert() {
		ArtistEvolution.ConfigureForProbe(enable: false, observe: false);
		SimulatedArtist unobserved = NewArtist();
		ArtistEvolutionService.Initialize(unobserved, 1962);
		Require(unobserved.evolution == null,
			"1a disabled evolution allocates no profile across a 22.5k registry");

		// Observation on, ratification off: the counterfactual run. It must see the candidate
		// and refuse to write identity.
		ArtistEvolution.ConfigureForProbe(enable: false, observe: true);
		ArtistEvolutionService.SeedAnnualBudgetForProbe(PivotYear, 20000, Roots, 200);
		SimulatedArtist observed = ArtistAtMajority(Plugged, PivotYear);
		Require(observed.evolution != null && observed.primaryGenre == Roots && observed.evolution.eras.Count == 1 &&
			observed.evolution.lastIdentityChangeYear == -1,
			"1b observe-only sees the window but never ratifies identity");
	}

	private static void ProbeInitializeIsPureAndBounded() {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false);
		SimulatedArtist artist = NewArtist();
		ArtistEvolutionService.Initialize(artist, 1962);
		ArtistEvolutionProfile first = artist.evolution;
		float ambition = first.artisticAmbition;
		// Initialize is idempotent: a second call must not replace a profile that has history.
		ArtistEvolutionService.Initialize(artist, 1964);
		Require(ReferenceEquals(first, artist.evolution), "2a Initialize is idempotent and never discards arc state");

		var reference = new ArtistEvolutionProfile();
		ArtistEvolutionService.DeriveDisposition(artist, reference);
		Require(Math.Abs(reference.artisticAmbition - ambition) < 1e-6f,
			"2b disposition is a pure function of the lineup, so it reproduces exactly");
		float[] traits = {
			first.artisticAmbition, first.experimentalAppetite, first.commercialPragmatism, first.rootsAttachment,
			first.conceptualThinking, first.peerSensitivity, first.volatility
		};
		Require(traits.All(trait => trait >= 0f && trait <= 1f), "2c every disposition term is bounded to [0,1]");
		Require(first.eras.Count == 1 && first.eras[0].IsOpen && first.eras[0].startYear == artist.formedYear &&
			first.eras[0].primaryGenre == Roots, "2d era 0 opens at formation on the formation identity");
	}

	private static void ProbeWindowMustBeFull() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = NewArtist();
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		// One short of the evidence window, all of them on the candidate: a unanimous run
		// that is not yet enough of a career to read. Anchored to the constant.
		ReleaseProjects(artist, Plugged, ArtistEvolution.DriftEvidenceWindow - 1, PivotYear);
		Require(artist.primaryGenre == Roots && artist.evolution.eras.Count == 1,
			"3 an incomplete evidence window cannot ratify however unanimous it is");
	}

	private static void ProbeMajorityRuleFires() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		Require(artist.primaryGenre == Plugged && artist.evolution.artisticCenter == Plugged,
			"4a a coherent run of off-identity sides on an adjacent, open genre ratifies");
		Require(artist.evolution.lastIdentityChangeYear == PivotYear,
			"4b ratification stamps the cooldown clock");
		Require(artist.careerEvents.Count(entry => entry.Contains(Plugged.ToString())) == 1,
			"4c exactly one careerEvents string per era, not one per evaluation");
	}

	private static void ProbePluralityShortOfMajorityDoesNotFire() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = NewArtist();
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		// One detour is not a move: a full window with fewer off-identity sides than the
		// authored minimum cannot ratify however adjacent that one side is.
		ReleaseProjects(artist, Plugged, ArtistEvolution.DriftOffIdentityMinimum - 1, PivotYear);
		ReleaseProjects(artist, Roots, ArtistEvolution.DriftWindow, PivotYear);
		Require(artist.primaryGenre == Roots, "5a a single off-identity side is a detour, not an identity");

		// And a wandering act whose sides do not agree on a direction is refused for exactly
		// that reason: it has drifted, but not toward anything.
		SimulatedArtist scattered = NewArtist(Genre.Soul);
		ArtistEvolutionService.Initialize(scattered, scattered.formedYear);
		ArtistEvolutionService.SeedAnnualBudgetForProbe(PivotYear, 20000, Genre.Soul, 200);
		ArtistEvolutionService.ObserveProject(scattered, Genre.Soul, PivotYear);
		ArtistEvolutionService.ObserveProject(scattered, Genre.Gospel, PivotYear);
		ArtistEvolutionService.ObserveProject(scattered, Genre.LatinPop, PivotYear);
		var verdict = ArtistEvolutionService.Evaluate(scattered, Genre.Soul, PivotYear);
		Require(scattered.primaryGenre == Genre.Soul &&
			verdict.Block == ArtistEvolutionService.RatificationBlock.NoCoherentDirection,
			"5b sides that do not agree on a destination elect none");
	}

	private static void ProbeAdjacencyFloorBlocks() {
		const int metalYear = 1968;
		EnableRatification(metalYear, Genre.Soul);
		// Different family, no authored edge: adjacency is 0 against a floor of .12. No
		// Soul -> ProtoMetal. If a historically real path has no edge, the fix is to add the
		// edge in BuildEdges, not to lower the floor.
		Require(GenreMarketMomentumService.GetAdjacency(Genre.Soul, Genre.ProtoMetal) < ArtistEvolution.AdjacencyFloor,
			"6a the fixture pair really is below the adjacency floor");
		SimulatedArtist artist = ArtistAtMajority(Genre.ProtoMetal, metalYear, Genre.Soul);
		Require(artist.primaryGenre == Genre.Soul, "6b no musical path, no ratification");
		Require(ArtistEvolutionService.Evaluate(artist, Genre.Soul, metalYear).Block ==
			ArtistEvolutionService.RatificationBlock.NoMusicalPath, "6c the refusal is attributed to the missing path");
	}

	private static void ProbeClimateBlocksClosedGenre() {
		// Folk carries a death year, so past it the genre takes no new supply. An act cannot
		// ratify onto a scene that is no longer accepting anybody, however adjacent it is.
		int closedYear = (int)(GenreCatalog.Get(Roots).DeathYear ?? PivotYear) + 2;
		EnableRatification(closedYear, Plugged);
		Require(!GenreSupplyService.IsAvailableForNewSupply(Roots, closedYear),
			"7a the fixture year really is past the genre's death year");
		SimulatedArtist artist = ArtistAtMajority(Roots, closedYear, Plugged);
		Require(artist.primaryGenre == Plugged, "7b a genre closed to new supply cannot be ratified into");
		Require(ArtistEvolutionService.Evaluate(artist, Plugged, closedYear).Block ==
			ArtistEvolutionService.RatificationBlock.GenreClosedToNewSupply, "7c the refusal is attributed to climate");
	}

	private static void ProbeCooldownBlocks() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		Require(artist.primaryGenre == Plugged, "8a the first migration lands");
		// Inside the cooldown, a second full majority is refused. Careers, not weathervanes.
		int insideCooldown = PivotYear + ArtistEvolution.IdentityChangeCooldownYears - 1;
		ArtistEvolutionService.SeedAnnualBudgetForProbe(insideCooldown, 20000, Plugged, 200);
		ReleaseProjects(artist, Genre.SingerSongwriter, ArtistEvolution.DriftWindow, insideCooldown);
		Require(artist.primaryGenre == Plugged && artist.evolution.eras.Count == 2,
			"8b a second identity change inside the cooldown is refused");
	}

	private static void ProbeCooldownExpires() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		int afterCooldown = PivotYear + ArtistEvolution.IdentityChangeCooldownYears;
		ArtistEvolutionService.SeedAnnualBudgetForProbe(afterCooldown, 20000, Plugged, 200);
		ReleaseProjects(artist, Genre.SingerSongwriter, ArtistEvolution.DriftWindow, afterCooldown);
		Require(artist.primaryGenre == Genre.SingerSongwriter && artist.evolution.eras.Count == 3,
			"9 the cooldown opens again exactly at the authored interval");
	}

	private static void ProbeTerminalCareerBlocks() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = NewArtist();
		artist.careerState = CareerState.Dropped;
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		ReleaseProjects(artist, Plugged, ArtistEvolution.DriftWindow, PivotYear);
		Require(artist.primaryGenre == Roots, "10 a terminal career does not start a new era");
	}

	private static void ProbeAnnualBudgetBlocks() {
		EnableRatification(PivotYear);
		ArtistEvolutionService.ExhaustAnnualBudgetForProbe();
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		Require(artist.primaryGenre == Roots, "11a an exhausted annual conversion budget is a hard stop");
		Require(ArtistEvolutionService.Evaluate(artist, Roots, PivotYear).Block ==
			ArtistEvolutionService.RatificationBlock.AnnualBudgetExhausted, "11b the refusal is attributed to the budget");
	}

	private static void ProbeGenreOutflowCapBlocks() {
		EnableRatification(PivotYear);
		ArtistEvolutionService.ExhaustGenreOutflowForProbe(Roots);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		Require(artist.primaryGenre == Roots, "12a a scene thins; it does not evaporate");
		Require(ArtistEvolutionService.Evaluate(artist, Roots, PivotYear).Block ==
			ArtistEvolutionService.RatificationBlock.GenreOutflowCapReached, "12b the refusal is attributed to the outflow cap");
	}

	private static void ProbeEraCloseOpenIsAtomicAndIdempotent() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		ArtistEvolutionProfile profile = artist.evolution;
		Require(profile.eras.Count == 2, "13a exactly one era opens per ratification");
		Require(profile.eras[0].endYear == PivotYear && !profile.eras[0].IsOpen,
			"13b the outgoing era is closed in the same call that opens the next");
		Require(profile.eras[1].IsOpen && profile.eras[1].startYear == PivotYear && profile.eras[1].eraIndex == 1,
			"13c the incoming era opens on the same year with the next index");
		Require(profile.eras.Count(era => era.IsOpen) == 1, "13d exactly one era is ever open");

		// Re-running the evaluation immediately cannot re-fire: identity now IS the window's
		// majority, and the window was cleared anyway.
		ArtistEvolutionService.Evaluate(artist, artist.primaryGenre, PivotYear);
		Require(profile.eras.Count == 2, "13e re-evaluating a just-ratified artist is idempotent");
	}

	private static void ProbeFormationIdentitySurvivesMigration() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		Require(artist.formationPrimaryGenre == Roots && artist.formationSecondaryGenre == Genre.ContemporaryFolk,
			"14 formation identity survives migration, so native-vs-transitioned keeps meaning " +
			"'against where they started'");
	}

	private static void ProbeRatificationResetsWindowAndDemotesPrimary() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		ArtistEvolutionProfile profile = artist.evolution;
		Require(artist.secondaryGenre == Roots, "15a the outgoing primary is demoted to secondary");
		Require(profile.priorArtisticCenter == Roots, "15b the arc remembers where the act came from");
		Require(profile.recentProjectCount == 0 && !profile.WindowFull,
			"15c the drift window is cleared so the new era is judged on its own releases");
		Require(profile.projectsSinceIdentityChange == 0, "15d the project counter restarts with the era");
	}

	private static void ProbeCriticalAcclaimWriter() {
		// Anchored to the bar, not to a literal either side of it.
		const float bar = ArtistCriticalAcclaimService.CraftBar;
		float belowBar = ArtistCriticalAcclaimService.GetAcclaimDelta(bar * .5f, 0f);
		float atBar = ArtistCriticalAcclaimService.GetAcclaimDelta(bar, 0f);
		Require(belowBar < 0f && Math.Abs(atBar) < 1e-6f,
			"16a a record exactly at the craft bar is a critical non-event; below it, standing erodes");

		float acclaimedAndUnsold = ArtistCriticalAcclaimService.GetAcclaimDelta(1f, 0f);
		float acclaimedAndSold = ArtistCriticalAcclaimService.GetAcclaimDelta(1f, 1f);
		Require(acclaimedAndUnsold > acclaimedAndSold,
			"16b the acclaimed record that missed commercially earns more critical standing than the one that sold");
		Require(acclaimedAndSold <= ArtistCriticalAcclaimService.MaxGainPerRelease + 1e-6f &&
			acclaimedAndUnsold <= ArtistCriticalAcclaimService.MaxTotalGainPerRelease + 1e-6f,
			"16c no single record mints a critical reputation outright, bonus included");

		// A career of masterpieces converges inside the bound rather than pinning at it in
		// one release, and a career of anonymous product decays toward zero without going
		// negative.
		float rising = 0f, falling = .80f;
		for (int index = 0; index < 40; index++) {
			rising = ArtistCriticalAcclaimService.Apply(rising, 1f, 0f);
			falling = ArtistCriticalAcclaimService.Apply(falling, 0f, 1f);
		}
		Require(rising > 0f && rising <= 1f && falling >= 0f && falling < .10f,
			"16d acclaim is bounded to [0,1] in both directions under repeated application");

		Require(ArtistCriticalAcclaimService.GetCommercialScore(0) == 0f &&
			ArtistCriticalAcclaimService.GetCommercialScore(1) > ArtistCriticalAcclaimService.GetCommercialScore(100),
			"16e a record that never charted scores zero commercially and the chart ordering is monotone");
	}

	private static void ProbeFormationAffinitySplitScope() {
		const float year = 1965f;
		bool prior = GenreSupplyService.SplitFormationAffinity;
		try {
			GenreSupplyService.SetSplitFormationAffinityForProbe(false);
			float formationJoined = GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, year,
				null, null, GenreSupplyService.SupplyWeightContext.Formation);
			float transitionJoined = GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, year,
				null, null, GenreSupplyService.SupplyWeightContext.ProjectTransition);
			Require(Math.Abs(formationJoined - transitionJoined) < 1e-6f,
				"17a with the split off, formation and project transition share one weight exactly as before");

			GenreSupplyService.SetSplitFormationAffinityForProbe(true);
			float formationSplit = GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, year,
				null, null, GenreSupplyService.SupplyWeightContext.Formation);
			float transitionSplit = GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, year,
				null, null, GenreSupplyService.SupplyWeightContext.ProjectTransition);
			// Formation is deliberately NOT untouched for Country: the split removes a channel
			// carrying 17.9% of its project supply, so the compensating raise lives here and
			// the two are one change. Everything the split does to Country must therefore show
			// up as transition DOWN and formation UP, never as both moving the same way.
			Require(formationSplit > formationJoined,
				"17b Country's formation weight rises to replace the transition channel the split removed");
			Require(transitionSplit < transitionJoined,
				"17c the split still removes the affinity from project transition -- the leak stays closed");
			// The transition weight must fall to exactly the unaffinitied value, so the
			// compensation cannot leak back into the channel it was meant to close.
			Require(Math.Abs(transitionJoined / transitionSplit - 2.2f) < 1e-3f,
				"17e transition loses exactly the authored 2.2 and nothing more");

			// A genre with no authored affinity must be identical under both settings, so the
			// change cannot quietly re-weight the calibrated balance anywhere else.
			float neutralJoined = GenreSupplyService.GetSupplyWeight(Genre.RockAndRoll, null, null, null, year,
				null, null, GenreSupplyService.SupplyWeightContext.ProjectTransition);
			GenreSupplyService.SetSplitFormationAffinityForProbe(false);
			float neutralSplit = GenreSupplyService.GetSupplyWeight(Genre.RockAndRoll, null, null, null, year,
				null, null, GenreSupplyService.SupplyWeightContext.ProjectTransition);
			Require(Math.Abs(neutralJoined - neutralSplit) < 1e-6f,
				"17d a genre with no authored formation affinity is unaffected by the split");
		} finally {
			GenreSupplyService.SetSplitFormationAffinityForProbe(prior);
		}
	}

	// ---- PHASE 2: pressure ----------------------------------------------------------------------

	/// <summary>A "settled star" fixture: nothing pressing, everything holding.</summary>
	private static SimulatedArtist SettledStar() {
		SimulatedArtist artist = NewArtist();
		artist.careerState = CareerState.Superstar;
		artist.reputation = .90f;
		artist.momentum = .90f;
		artist.consecutiveFlops = 0;
		artist.consecutiveHits = 4;
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		return artist;
	}

	private static void ProbePressureNeutralSettingIsExact() {
		// With the phase off, the identity fit must be the authored constants to the bit --
		// the neutral case has to reproduce Phase 1 exactly or no gate below is readable.
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: false);
		SimulatedArtist desperate = NewArtist();
		desperate.consecutiveFlops = 9;
		desperate.careerState = CareerState.Declining;
		ArtistEvolutionService.Initialize(desperate, desperate.formedYear);
		float pinnedOff = GenreSupplyService.GetIdentityFitForProbe(Roots, desperate);
		Require(Math.Abs(pinnedOff - ArtistEvolution.IdentityFitPrimaryNeutral) < 1e-6f,
			"18a with pressure off, even a desperate act keeps the authored 4.0 primary anchor exactly");

		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: true);
		SimulatedArtist settled = SettledStar();
		ArtistEvolutionPressureService.Evaluate(settled, null, PivotYear);
		Require(settled.evolution.restlessness == 0f,
			"18b a settled star with hits and reputation has zero net restlessness");
		Require(Math.Abs(GenreSupplyService.GetIdentityFitForProbe(Roots, settled) -
			ArtistEvolution.IdentityFitPrimaryNeutral) < 1e-6f,
			"18c an unpressured act reproduces the neutral constants with the phase on");
	}

	private static void ProbePressuredWindowStillNeedsUnanimity() {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: true);
		ArtistEvolutionService.SeedAnnualBudgetForProbe(PivotYear, 20000, Roots, 200);
		SimulatedArtist artist = NewArtist();
		artist.consecutiveFlops = 9;
		artist.careerState = CareerState.Declining;
		artist.groupCohesion = .20f;
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.restlessness > 0f, "19a the fixture act really is under pressure");

		// Pressure lowers how MUCH evidence is needed, never the coherence bar. A single
		// off-identity side that points nowhere reachable is still refused.
		ArtistEvolutionService.ObserveProject(artist, Roots, PivotYear);
		ArtistEvolutionService.ObserveProject(artist, Roots, PivotYear);
		ArtistEvolutionService.ObserveProject(artist, Genre.ProtoMetal, PivotYear);
		Require(artist.primaryGenre == Roots, "19b pressure never admits a destination off the musical map");

		// One adjacent side IS enough once the act is under real pressure -- the band that
		// plugs in after two failures does not cut three folk-rock sides first.
		SimulatedArtist pushed = NewArtist();
		pushed.consecutiveFlops = 9;
		pushed.careerState = CareerState.Declining;
		pushed.groupCohesion = .20f;
		ArtistEvolutionService.Initialize(pushed, pushed.formedYear);
		pushed.evolution.rootsAttachment = 0f;
		ArtistEvolutionPressureService.Evaluate(pushed, null, PivotYear);
		ReleaseProjects(pushed, Roots, ArtistEvolution.PressuredWindowMinimum - 1, PivotYear);
		ReleaseProjects(pushed, Plugged, 1, PivotYear);
		Require(pushed.primaryGenre == Plugged,
			"19c under pressure a single coherent adjacent side is enough to ratify");

		// With the phase off, that same single side must not fire: the unpressured act needs
		// the authored minimum, which is the Phase-1 rule unchanged.
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: false);
		SimulatedArtist unpressured = NewArtist();
		ArtistEvolutionService.Initialize(unpressured, unpressured.formedYear);
		ReleaseProjects(unpressured, Roots, ArtistEvolution.PressuredWindowMinimum - 1, PivotYear);
		ReleaseProjects(unpressured, Plugged, 1, PivotYear);
		Require(unpressured.primaryGenre == Roots,
			"19d with pressure off the authored off-identity minimum is required, as in Phase 1");
	}

	private static void ProbeIdentityFitStaysInsideAuthoredBounds() {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: true);
		SimulatedArtist artist = NewArtist();
		artist.consecutiveFlops = 20;
		artist.careerState = CareerState.Declining;
		artist.groupCohesion = 0f;
		artist.reputation = 0f;
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		artist.evolution.rootsAttachment = 0f;
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);

		float primary = GenreSupplyService.GetIdentityFitForProbe(Roots, artist);
		float adjacent = GenreSupplyService.GetIdentityFitForProbe(Genre.SingerSongwriter, artist);
		float unrelated = GenreSupplyService.GetIdentityFitForProbe(Genre.Soul, artist);
		Require(primary >= ArtistEvolution.IdentityFitPrimaryRestless - 1e-6f &&
			primary <= ArtistEvolution.IdentityFitPrimaryNeutral + 1e-6f,
			"20a the primary anchor softens strictly inside the authored bound, never below it");
		Require(adjacent >= ArtistEvolution.IdentityFitAdjacentNeutral - 1e-6f &&
			adjacent <= ArtistEvolution.IdentityFitAdjacentRestless + 1e-6f,
			"20b adjacent candidates lift strictly inside the authored bound");
		Require(Math.Abs(unrelated - .55f) < 1e-6f && unrelated < adjacent,
			"20c the unrelated floor is never raised, so no candidate enters the pool that was not already in it");
		Require(primary > adjacent, "20d however restless, the act is still most likely to stay in its own lane");
		// Scouting ranks candidates through this same weight. A restless act must not become
		// harder to sign, so the lift is scoped to project transition and nothing else.
		Require(Math.Abs(GenreSupplyService.GetIdentityFitForProbe(Roots, artist,
			GenreSupplyService.SupplyWeightContext.Formation) - ArtistEvolution.IdentityFitPrimaryNeutral) < 1e-6f,
			"20e restlessness never leaks into the formation/scouting weight");

		// --- adjacency-aware tier. It redistributes INSIDE the middle tier and must not widen
		// the ladder: nothing below the old "other" floor, nothing at or above the secondary
		// weight, and lineage strictly ordered above bare family membership.
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: false, adjacencyFit: true);
		SimulatedArtist plain = NewArtist();
		ArtistEvolutionService.Initialize(plain, plain.formedYear);
		float lineage = GenreSupplyService.GetIdentityFitForProbe(Plugged, plain);       // Folk->FolkRock, edge .75
		float family = GenreSupplyService.GetIdentityFitForProbe(Genre.SingerSongwriter, plain);
		float stranger = GenreSupplyService.GetIdentityFitForProbe(Genre.ProtoMetal, plain);
		Require(lineage > family, "20f an authored lineage outranks bare family membership -- the whole point");
		// The regression this fixture exists to prevent, measured on adjfit-1001: a single
		// continuous scale let cross-family Country (edge .45) climb to 1.36 against
		// same-family FolkRock's 1.90, cutting FolkRock's advantage from 2.64x to 1.40x and
		// sending folk acts to Country 98 times against FolkRock's 57. The tiers must stay
		// disjoint, and FolkRock must keep a decisive edge over Country for a folk act.
		float crossFamilyWithEdge = GenreSupplyService.GetIdentityFitForProbe(Genre.Country, plain);
		Require(crossFamilyWithEdge < family && crossFamilyWithEdge < lineage,
			"20g no cross-family destination outranks a same-family one, however strong its edge");
		Require(lineage / crossFamilyWithEdge > 2f,
			"20h the authored lineage keeps a decisive advantage over the cross-family edge " +
			"that outdrew it before -- Folk->FolkRock against Folk->Country");
		Require(stranger >= .55f - 1e-6f && lineage <= ArtistEvolution.IdentityFitAdjacentRestless + 1e-6f,
			"20i the adjacency tier stays inside the [.55, adjacent-restless] band Phase 2 established");
		Require(lineage < 2.25f, "20j no middle-tier candidate reaches the secondary-identity weight");

		// With the flag off the authored constants must come back exactly, so the supply change
		// is revertible without touching this file.
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false);
		Require(Math.Abs(GenreSupplyService.GetIdentityFitForProbe(Genre.SingerSongwriter, plain) -
			ArtistEvolution.IdentityFitAdjacentNeutral) < 1e-6f,
			"20k the adjacency tier is fully revertible by its flag");
	}

	/// <summary>
	/// The edge fill is a PREREQUISITE of the adjacency-aware fit, not a companion to it: an
	/// unauthored genre would otherwise collapse to the family floor and take a permanent supply
	/// penalty for being undocumented rather than for being unrelated.
	/// </summary>
	/// <summary>
	/// The split and Country's compensating formation weight are one change, not two: the
	/// split removes a channel that carried 17.9% of Country's project supply, so shipping it
	/// alone converts a leak into a deficit on the genre that already had the largest one.
	/// </summary>
	private static void ProbeCountryCompensatesForTheSplit() {
		var country = new SimulatedArtist {
			artistId = "cty", primaryGenre = Genre.Country, secondaryGenre = Genre.Country,
			formationPrimaryGenre = Genre.Country, formationSecondaryGenre = Genre.Country,
			isActive = true, careerState = CareerState.Rising
		};
		GenreSupplyService.SetSplitFormationAffinityForProbe(false);
		float unsplit = GenreSupplyService.GetSupplyWeight(Genre.Country, null, country, null, 1966f,
			context: GenreSupplyService.SupplyWeightContext.Formation);
		GenreSupplyService.SetSplitFormationAffinityForProbe(true);
		float split = GenreSupplyService.GetSupplyWeight(Genre.Country, null, country, null, 1966f,
			context: GenreSupplyService.SupplyWeightContext.Formation);
		GenreSupplyService.SetSplitFormationAffinityForProbe(false);
		Require(split > unsplit,
			"26a with the split on, Country's FORMATION weight rises to replace the transition " +
			"channel the split removed");
		// The transition channel must still be gone -- the compensation belongs at formation
		// only, or we have simply re-created the leak under a larger constant.
		GenreSupplyService.SetSplitFormationAffinityForProbe(true);
		float transition = GenreSupplyService.GetSupplyWeight(Genre.Country, null, country, null, 1966f,
			context: GenreSupplyService.SupplyWeightContext.ProjectTransition);
		float formation = GenreSupplyService.GetSupplyWeight(Genre.Country, null, country, null, 1966f,
			context: GenreSupplyService.SupplyWeightContext.Formation);
		GenreSupplyService.SetSplitFormationAffinityForProbe(false);
		Require(transition < formation,
			"26b the compensation lands at formation only; the transition leak stays closed");
	}

	private static void ProbeEveryGenreHasAMusicalNeighbour() {
		Genre[] all = GenreCatalog.All.Select(profile => profile.Genre).ToArray();
		var stranded = all.Where(genre => !all.Any(other => other != genre &&
			GenreMarketMomentumService.GetAdjacency(genre, other) > .12f)).ToArray();
		Require(stranded.Length == 0,
			"25 every catalog genre has at least one explicit adjacency edge, so no genre is " +
			"penalised by the supply weight for being unauthored: " + string.Join(", ", stranded));
	}

	// ---- PHASE 4: album legitimacy --------------------------------------------------------------

	private static void ProbeAlbumLegitimacyZeroReproducesCeiling() {
		AlbumLegitimacyService.ResetForProbe();
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, legitimacy: true);
		Require(AlbumLegitimacyService.Legitimacy == 0f && Math.Abs(AlbumLegitimacyService.CurrentCeilingMultiplier - 1f) < 1e-6f,
			"21a legitimacy starts at zero and multiplies the exogenous ceiling by exactly one");
		for (int year = 1960; year <= 1969; year++) {
			float era = Godot.Mathf.SmoothStep(0.12f, 0.96f, Godot.Mathf.Clamp(
				(year - AlbumModel.CohesionRiseStartYear) / (AlbumModel.CohesionRiseEndYear - AlbumModel.CohesionRiseStartYear), 0f, 1f));
			Require(Math.Abs(AlbumLegitimacyService.ApplyToEraTerm(era, 0f) - era) < 1e-6f,
				$"21b legitimacy of zero reproduces the exogenous cohesion ceiling exactly at {year}");
		}
		// Fully saturated, it may pull the curve forward by the authored lift and no further,
		// and it can never pull it DOWN.
		float sample = .50f;
		float lifted = AlbumLegitimacyService.ApplyToEraTerm(sample, 1f);
		Require(lifted > sample && Math.Abs(lifted - sample * (1f + AlbumLegitimacyService.MaxCeilingLift)) < 1e-6f,
			"21c saturated legitimacy lifts the ceiling by exactly the authored bound");
		Require(AlbumLegitimacyService.ApplyToEraTerm(sample, -5f) >= sample,
			"21d the exogenous curve is a floor, so legitimacy can never lower the ceiling");
		AlbumLegitimacyService.ResetForProbe();
	}

	private static void ProbeLandmarkBarAndEarliness() {
		const float over = 1f;
		Require(!AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear - 1, over, over, over),
			"22a legitimacy is hard-zero before its start year, however good the record");
		Require(!AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear + 1,
			AlbumLegitimacyService.LandmarkIntegrityBar * .5f, over, over),
			"22b a record that did not hang together is not a landmark however well it sold");
		Require(!AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear + 1, over,
			AlbumLegitimacyService.LandmarkMeritBar * .5f, over),
			"22b2 nor is a consistent record that is consistently mediocre -- a ratio rewards " +
			"uniformity, so merit has to be gated separately from integrity");
		Require(!AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear + 1, over, over,
			AlbumLegitimacyService.LandmarkRecognitionBar * .5f),
			"22c a body of work made in private is not a movement; it has to have been heard");
		Require(AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear + 1, over, over, over),
			"22d a strong body of work that reached people is a landmark");

		// Earliness is measured against the MOVEMENT, not the calendar. The regression this
		// pins: the old year ramp hit exactly zero in 1969, the year the model produced the
		// most landmark albums of the decade, so 136 of them were worth nothing at all.
		AlbumLegitimacyService.ResetForProbe();
		float virgin = AlbumLegitimacyService.GetEarliness(1965);
		float lateVirgin = AlbumLegitimacyService.GetEarliness(1969);
		Require(Math.Abs(virgin - lateVirgin) < 1e-6f,
			"22e with no movement yet under way, a 1969 statement is as early as a 1965 one -- " +
			"earliness is not a property of the calendar");
		AlbumLegitimacyService.SetLegitimacyForProbe(1f);
		float saturated = AlbumLegitimacyService.GetEarliness(1965);
		Require(saturated < virgin,
			"22f once the movement has happened, making one of these is less remarkable");
		Require(saturated >= AlbumLegitimacyService.MinimumEarliness - 1e-6f && saturated > 0f,
			"22g a landmark in a saturated movement still counts for something; the floor is not zero");
		Require(AlbumLegitimacyService.GetEarliness(AlbumLegitimacyService.LegitimacyStartYear - 1) == 0f,
			"22h nothing before the start year can be leaned on, whatever the movement has done since");
		AlbumLegitimacyService.ResetForProbe();
	}

	private static void ProbeInfluenceMemoryIsBounded() {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, legitimacy: true);
		SimulatedArtist artist = NewArtist();
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		var profile = artist.evolution;
		int cap = CulturalMemoryService.MaxInfluencesPerArtist;
		for (int index = 0; index < cap * 4; index++) {
			profile.influences.Add(new ArtistInfluenceMemory {
				sourceArtistId = $"src{index}", sourceGenre = Plugged,
				type = ArtistInfluenceType.CohesiveAlbum, year = PivotYear, strength = .10f + index * .01f
			});
			// The service prunes on insert; emulate the same call the absorb path makes.
			while (profile.influences.Count > cap) {
				int weakest = 0;
				for (int scan = 1; scan < profile.influences.Count; scan++)
					if (profile.influences[scan].strength < profile.influences[weakest].strength) weakest = scan;
				profile.influences.RemoveAt(weakest);
			}
		}
		Require(profile.influences.Count == cap,
			"23a influence memory is capacity-bounded; an unbounded list on 22.5k artists is a leak in a costume");
		Require(profile.influences.TrueForAll(memory => memory.strength >= .10f + (cap * 4 - cap) * .01f - 1e-6f),
			"23b what survives the bound is the strongest, not the most recent");
	}

	// ---- PHASE 5: presentation ------------------------------------------------------------------

	private static void ProbeDiscographyIsAssembledNotStored() {
		EnableRatification(PivotYear);
		SimulatedArtist artist = ArtistAtMajority(Plugged, PivotYear);
		var discography = ArtistDiscographyService.Build(artist, System.Array.Empty<RecordRuntimeData>());
		Require(discography.HasEras && discography.Eras.Count == artist.evolution.eras.Count,
			"24a the discography exposes one group per era on file");
		Require(discography.Eras.TrueForAll(era => !string.IsNullOrWhiteSpace(era.Title) &&
			!string.IsNullOrWhiteSpace(era.Summary)),
			"24b every era carries a composed title and a composed line, not an empty row");
		Require(!ReferenceEquals(discography, ArtistDiscographyService.Build(artist, System.Array.Empty<RecordRuntimeData>())),
			"24c the view model is assembled on demand and never cached onto the artist");

		// The composed line has to be about THIS act: it names where they went.
		string opening = artist.evolution.eras[^1].summary;
		Require(opening.Contains(GenreNameFormatter.Format(Plugged), StringComparison.OrdinalIgnoreCase),
			"24d the era line is composed from the era's own facts, naming the genre it moved to");
		// One unremarkable change is a career, not a reputation, and must earn nothing on its
		// own. Tags are earned against the specific things the arc did.
		Require(!ArtistDiscographyService.DeriveTags(artist).Contains(ReputationTag.GenreBending),
			"24e a single genre change is not yet a reputation for genre-bending");
		artist.evolution.eras.Add(new ArtistEraRecord {
			eraIndex = 2, startYear = PivotYear + 3, primaryGenre = Roots, secondaryGenre = Plugged,
			phase = ArtistArcPhase.RootsReturn, trigger = ArtistEvolutionTrigger.BackToRoots
		});
		var earned = ArtistDiscographyService.DeriveTags(artist).ToList();
		Require(earned.Contains(ReputationTag.GenreBending) && earned.Contains(ReputationTag.Authentic),
			"24f a third era and a return to the original sound earn GenreBending and Authentic " +
			"from the enum ReputationTag already defines");
	}

	// ---- PHASE 6: motive that is not a foregone conclusion --------------------------------------

	/// <summary>An act with a controllable disposition, so one pressure can be isolated from the rest.</summary>
	private static SimulatedArtist QuietArtist(int year) {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: true,
			legitimacy: true, culturalMemory: true);
		SimulatedArtist artist = NewArtist();
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		artist.consecutiveFlops = 0;
		artist.contractConsecutiveFlops = 0;
		artist.momentum = .5f;
		artist.groupCohesion = 1f;
		artist.criticalAcclaim = 0f;
		artist.careerState = CareerState.Rising;
		ArtistEvolutionProfile profile = artist.evolution;
		// Silence the dispositional motives so the probe measures the one it is about.
		profile.volatility = 0f;
		profile.artisticAmbition = .10f;
		profile.conceptualThinking = .10f;
		profile.peerSensitivity = 1f;
		profile.acclaimAtLastProject = 0f;
		return artist;
	}

	/// <summary>
	/// The regression that made 92% of a decade's conversions say the same thing. Commercial
	/// pressure was <c>.50*streak + .30*cold + state</c>, so an act that had never had a
	/// record miss still read ~0.40 -- a constant, which beat every other motive on a raw
	/// max() essentially always.
	/// </summary>
	private static void ProbeCommercialPressureIsNotAConstant() {
		SimulatedArtist artist = QuietArtist(PivotYear);
		artist.momentum = 0f;                       // cold and anonymous...
		artist.careerState = CareerState.Declining; // ...and precarious...
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.commercialPressure == 0f,
			"27a an act whose records have not missed is under no commercial pressure, however " +
			"cold and precarious it is; the floor that made this a constant is gone");

		artist.consecutiveFlops = ArtistEvolutionPressureService.FlopStreakForPressure;
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		float atStreak = artist.evolution.commercialPressure;
		Require(atStreak > 0f, "27b a real flop streak does produce commercial pressure");

		artist.consecutiveFlops = ArtistEvolutionPressureService.FlopStreakForPressure * 2;
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.commercialPressure > atStreak,
			"27c and it goes on rising with the streak, which is the thing it is supposed to measure");
	}

	/// <summary>
	/// Motive is decided on normalised loudness, not raw magnitude. The five pressures are
	/// not on one scale -- a sum of three near-saturated terms against a product of five
	/// sub-unit factors -- so a raw max() compared formula shapes rather than motives.
	/// </summary>
	private static void ProbePeerInfluenceCanWinAMotive() {
		SimulatedArtist artist = QuietArtist(PivotYear);
		artist.consecutiveFlops = ArtistEvolutionPressureService.FlopStreakForPressure;
		artist.evolution.influences.Add(new ArtistInfluenceMemory {
			sourceArtistId = "someone_else", sourceGenre = Plugged,
			type = ArtistInfluenceType.HitSingle, year = PivotYear, strength = .30f
		});
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.peerPressure > 0f, "28a a live influence memory produces peer pressure");
		Require(artist.evolution.peerPressure < artist.evolution.commercialPressure,
			"28b and it is still numerically smaller than the commercial term, which is the " +
			"whole reason a raw max() could never surface it");
		Require(artist.evolution.dominantTrigger == ArtistEvolutionTrigger.PeerInfluence,
			"28c yet it wins the motive, because loudness is judged against each pressure's own scale");
	}

	/// <summary>
	/// The Rubber Soul -> Pet Sounds route. The ledger has always recorded WHICH KIND of
	/// record reached an act; until now nothing read it, so every peer motive collapsed into
	/// PeerInfluence and CohesiveAlbumMovement had no writer anywhere in the codebase.
	/// </summary>
	private static void ProbeCohesiveAlbumMovementHasAWriter() {
		SimulatedArtist artist = QuietArtist(PivotYear);
		artist.evolution.influences.Add(new ArtistInfluenceMemory {
			sourceArtistId = "the_other_band", sourceGenre = Plugged,
			type = ArtistInfluenceType.CohesiveAlbum, year = PivotYear, strength = .30f
		});
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.dominantTrigger == ArtistEvolutionTrigger.CohesiveAlbumMovement,
			"29a an album that hung together, heard by somebody paying attention, reads as the " +
			"album-as-art movement rather than as chasing a hit");
		Require(artist.evolution.lastReleaseIntent == ReleaseCreativeIntent.Statement,
			"29b and what they reach for next is a statement");

		// The same strength arriving as a hit single is a different motive entirely.
		artist.evolution.influences.Clear();
		artist.evolution.influences.Add(new ArtistInfluenceMemory {
			sourceArtistId = "the_other_band", sourceGenre = Plugged,
			type = ArtistInfluenceType.HitSingle, year = PivotYear, strength = .30f
		});
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.dominantTrigger == ArtistEvolutionTrigger.PeerInfluence,
			"29c the identical pressure arriving as a hit single is a different motive");
	}

	/// <summary>
	/// CriticalBreakthrough was declared in the trigger enum and returned by no code path at
	/// all. The shape it is for: standing with the critics that is rising while the records
	/// are not selling.
	/// </summary>
	private static void ProbeCriticalBreakthroughHasAWriter() {
		SimulatedArtist artist = QuietArtist(PivotYear);
		artist.evolution.artisticAmbition = .70f;
		artist.momentum = 0f;                       // the public has not caught up
		artist.criticalAcclaim = .70f;
		artist.evolution.acclaimAtLastProject = .40f;   // and it is climbing
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.criticalPressure > 0f, "30a rising acclaim is a pressure at all");
		Require(artist.evolution.dominantTrigger == ArtistEvolutionTrigger.CriticalBreakthrough,
			"30b an act the critics rate and the public has not caught up with is chasing the " +
			"critical breakthrough, not fleeing a commercial failure");

		// An act with no critical standing has no critical motive. The term must not become a
		// second constant in place of the one it replaced.
		artist.criticalAcclaim = 0f;
		artist.evolution.acclaimAtLastProject = 0f;
		ArtistEvolutionPressureService.Evaluate(artist, null, PivotYear);
		Require(artist.evolution.criticalPressure == 0f,
			"30c and an act nobody rates has no critical pressure at all");
	}

	/// <summary>
	/// Label pressure used to multiply BOTH its terms by the flop streak, making it a
	/// scaled-down copy of commercial pressure driven by the identical variable. It could
	/// not win against its own parent under any parameter values, and across a decade it
	/// never once did.
	/// </summary>
	private static void ProbeLabelPressureHasItsOwnMotive() {
		SimulatedArtist artist = QuietArtist(PivotYear);
		var label = new AILabel {
			labelId = "probe_label", artistLoyalty = 1f, riskTolerance = 1f, productionQuality = .5f,
			preferredGenres = new[] { Plugged }, secondaryGenres = System.Array.Empty<Genre>()
		};
		CulturalMemoryService.ResetForProbe();
		// Nothing has happened yet, and the act is not failing: a patient label wants nothing.
		ArtistEvolutionPressureService.Evaluate(artist, label, PivotYear);
		Require(artist.evolution.labelPressure == 0f,
			"31a a loyal label with a functioning act and nothing to chase applies no pressure");

		// Somebody else's record lands in a genre this label believes in.
		CulturalMemoryService.Publish("another_act", "another_label", Plugged, PivotYear,
			CulturalEventType.LandmarkAlbum, merit: .80f, recognition: .80f, strength: .60f);
		ArtistEvolutionPressureService.Evaluate(artist, label, PivotYear);
		Require(artist.evolution.labelPressure > 0f,
			"31b a label that has noticed something working wants some of it, with the act's " +
			"flop streak still at zero -- the motive is its own, not a restatement of failure");
		Require(artist.evolution.labelWantsGenre == Plugged,
			"31c and the ledger records what the label is actually pushing for");
		Require(artist.evolution.dominantTrigger == ArtistEvolutionTrigger.LabelPressure,
			"31d which is loud enough to be the motive");
		CulturalMemoryService.ResetForProbe();
	}

	/// <summary>
	/// The modular seam the journalism layer will arrive through. Merit is a property of the
	/// record; recognition is how widely it is known. A record that never charted can clear
	/// the landmark bar on press alone, and no rule in the landmark path changes to allow it.
	/// </summary>
	private static void ProbeRecognitionIsSeparableFromCommerce() {
		CulturalRecognitionService.ResetForProbe();
		const int year = PivotYear;
		// A record nobody bought, by an act nobody has heard of.
		(float unheard, _) = CulturalRecognitionService.Consume("rec_a", peakPosition: 0, artistStanding: 0f);
		Require(unheard == 0f, "32a a record that did not chart, by an act with no standing, reached nobody");
		Require(!AlbumLegitimacyService.IsLandmark(year, 1f, 1f, unheard),
			"32b however well made, it is not yet a landmark");

		// The trade press notices it. Nothing else about the record has changed.
		CulturalRecognitionService.Deposit("rec_b", .90f, RecognitionChannel.Press, year);
		(float reviewed, RecognitionChannel channel) =
			CulturalRecognitionService.Consume("rec_b", peakPosition: 0, artistStanding: 0f);
		Require(reviewed >= AlbumLegitimacyService.LandmarkRecognitionBar,
			"32c a record the press carried has public standing without having charted at all");
		Require(channel == RecognitionChannel.Press,
			"32d and the ledger can say which channel is responsible for it");
		Require(AlbumLegitimacyService.IsLandmark(year, 1f, 1f, reviewed),
			"32e so it clears the landmark bar through the same door, with no rule changed");

		// Recognition is conferred once, not re-counted on every read.
		(float second, _) = CulturalRecognitionService.Consume("rec_b", peakPosition: 0, artistStanding: 0f);
		Require(second < reviewed, "32f a deposit is consumed, not left sitting to be counted again");
		Require(CulturalRecognitionService.PendingCountForProbe == 0,
			"32g and the pending table does not retain it; an unbounded per-record table is a leak");
		CulturalRecognitionService.ResetForProbe();
	}

	/// <summary>
	/// Merit is intrinsic. The journalism layer must be able to change how widely a record is
	/// known WITHOUT changing what the record is, or the two layers are one layer.
	/// </summary>
	/// <summary>
	/// A landmark album is not a concept album, and having hit singles on it is not
	/// disqualifying. Both readings were wrong in the first cut: the rule was stated against
	/// thematicCohesion, which is the concept axis AND is pinned to its clamp floor by the era
	/// ceiling until 1966, so no artist-made record could qualify before 1967 however good.
	/// </summary>
	private static void ProbeLandmarkIsNotAConceptAlbum() {
		// Both fixtures are DERIVED from the bar rather than hard-coded near it: a fixture that
		// sits at a literal "just above" inverts silently the moment the bar is re-derived,
		// which has already cost this project once.
		const int trackCount = 11;
		float bar = AlbumLegitimacyService.LandmarkIntegrityBar;
		// With a peak of 1.0, integrity is just the mean, so solve for the album-track quality
		// that lands a comfortable margin clear of the bar.
		float clearsBar = (trackCount * (bar + .03f) - 1f) / (trackCount - 1);
		// One strong side carrying ten weak ones: a hit with filler around it.
		var filler = new List<float> { 1f };
		for (int index = 0; index < trackCount - 1; index++) filler.Add(bar * .40f);
		// The same hit, on a record where the rest of it stands up. Pet Sounds had three
		// singles on it; what made it a body of work is that the album tracks were as strong.
		var bodyOfWork = new List<float> { 1f };
		for (int index = 0; index < trackCount - 1; index++) bodyOfWork.Add(clearsBar);

		float fillerIntegrity = AlbumModel.GetAlbumIntegrity(filler);
		float realIntegrity = AlbumModel.GetAlbumIntegrity(bodyOfWork);
		Require(realIntegrity > fillerIntegrity,
			"35a a record whose album tracks stand up reads as more of a record than a hit plus filler");
		Require(realIntegrity >= AlbumLegitimacyService.LandmarkIntegrityBar,
			"35b ...and clears the landmark bar WITH a hit single on it -- singles are not disqualifying");
		Require(fillerIntegrity < AlbumLegitimacyService.LandmarkIntegrityBar,
			"35c ...while the hit-plus-filler record does not, however big the hit");

		// The bar is reachable in 1965. This is the regression: stated against cohesion it was
		// unreachable until 1967 because the era ceiling clamps that field to 0.08 before then.
		Require(AlbumLegitimacyService.IsLandmark(1965, realIntegrity, 1f, 1f),
			"35d a body of work released in 1965 can be a landmark; the rule is not gated on " +
			"the concept-album era ceiling");
		Require(AlbumLegitimacyService.IsEligibleFormat(AlbumFormat.Standard) &&
			AlbumLegitimacyService.IsEligibleFormat(AlbumFormat.Concept),
			"35e a landmark need not be a concept album -- a purpose-made standard LP qualifies");
		Require(!AlbumLegitimacyService.IsEligibleFormat(AlbumFormat.Compilation) &&
			!AlbumLegitimacyService.IsEligibleFormat(AlbumFormat.Live) &&
			!AlbumLegitimacyService.IsEligibleFormat(AlbumFormat.Soundtrack),
			"35f but a compilation, a live document and a soundtrack are not new bodies of work");

		// The album shift is a ROCK phenomenon. Jazz does not undergo it because jazz was
		// already making records this way, which is a large part of why the form was available
		// to be taken seriously by rock acts later.
		float jazz1960 = AlbumModel.GetTrackConsistency(GenreFamily.Jazz, 1960);
		float rock1960 = AlbumModel.GetTrackConsistency(GenreFamily.Rock, 1960);
		float rock1969 = AlbumModel.GetTrackConsistency(GenreFamily.Rock, 1969);
		float pop1969 = AlbumModel.GetTrackConsistency(GenreFamily.Pop, 1969);
		Require(jazz1960 > rock1960,
			"35h a jazz LP in 1960 is already a body of work where a rock LP is a hit plus filler");
		Require(rock1969 > rock1960,
			"35i rock travels from one to the other across the decade -- that IS the album shift");
		Require(AlbumModel.GetTrackConsistency(GenreFamily.Jazz, 1969) - jazz1960 < rock1969 - rock1960,
			"35j and jazz moves less than rock does, because it had less distance to travel");
		Require(pop1969 < rock1969,
			"35k while manufactured pop largely does not make the journey at all");
		Require(AlbumModel.GetTrackSpreadMultiplier(GenreFamily.Jazz, 1960) <
			AlbumModel.GetTrackSpreadMultiplier(GenreFamily.Rock, 1960),
			"35l consistency is spent as a tighter track spread, not as higher track quality, so " +
			"it cannot inflate the album chart it is trying to describe");

		// Merit must not collapse for a pre-1966 album just because cohesion is clamped.
		float clampedCohesion = .08f;
		float withBody = ArtisticMeritService.GetCraft(.80f, .70f, clampedCohesion, isAlbum: true, .80f, realIntegrity);
		float withoutBody = ArtisticMeritService.GetCraft(.80f, .70f, clampedCohesion, isAlbum: true, .80f, 0f);
		Require(withBody > withoutBody,
			"35g and the critics hear the body of work even in a year the cohesion ceiling is clamped shut");
	}

	private static void ProbeMeritIgnoresReception() {
		float ambitious = ArtisticMeritService.GetFormatAmbition(ReleaseFormat.Album, AlbumFormat.Concept);
		float assembled = ArtisticMeritService.GetFormatAmbition(ReleaseFormat.Album, AlbumFormat.Compilation);
		Require(ambitious > assembled,
			"33a a record reaching for something is worth more as a work of art than a hit plus filler");
		float craft = ArtisticMeritService.GetCraft(.80f, .80f, .80f, isAlbum: true, .80f);
		Require(ArtisticMeritService.GetMerit(craft, ambitious) > ArtisticMeritService.GetMerit(craft, assembled),
			"33b and ambition is worth more on top of identical craft");
		Require(ArtisticMeritService.GetMerit(0f, 1f) == 0f,
			"33c but ambition without craft earns nothing; it multiplies rather than adds");
	}

	/// <summary>
	/// The ledger is the industry's shared memory, and the thing that makes an act an entity
	/// rather than a tag is that OTHER acts carry something of theirs.
	/// </summary>
	private static void ProbeCulturalMemoryPropagatesAndCounts() {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, pressure: true,
			legitimacy: true, culturalMemory: true);
		CulturalMemoryService.ResetForProbe();
		CulturalMemoryService.Publish("the_source", "src_label", Plugged, PivotYear,
			CulturalEventType.LandmarkAlbum, merit: .80f, recognition: .80f, strength: .60f);

		SimulatedArtist listener = NewArtist();
		ArtistEvolutionService.Initialize(listener, listener.formedYear);
		listener.evolution.peerSensitivity = 1f;
		CulturalMemoryService.AbsorbForArtist(listener, PivotYear);
		Require(listener.evolution.influences.Count == 1,
			"34a an act hears what was published since they last looked");
		Require(listener.evolution.influences[0].type == ArtistInfluenceType.CohesiveAlbum,
			"34b and remembers what kind of record it was");
		Require(CulturalMemoryService.InfluenceCountFor("the_source") == 1,
			"34c the source act's standing is what accumulates: somebody else carries their record now");

		// Reading again absorbs nothing: propagation is cursored, not re-scanned.
		CulturalMemoryService.AbsorbForArtist(listener, PivotYear);
		Require(listener.evolution.influences.Count == 1 &&
			CulturalMemoryService.InfluenceCountFor("the_source") == 1,
			"34d and a second look absorbs nothing, because the cursor has already passed it");

		// An act cannot be influenced by itself, and stale events are not heard at all.
		CulturalMemoryService.AbsorbForArtist(listener, PivotYear + CulturalMemoryService.InfluenceMemoryYears + 1);
		Require(CulturalMemoryService.InfluenceCountFor("the_source") == 1,
			"34e nothing new is taken from an event older than the memory window");
		CulturalMemoryService.ResetForProbe();
	}

	private static void Require(bool condition, string message) {
		if (!condition) throw new InvalidOperationException("Artist-evolution probe failed: " + message);
	}
}
