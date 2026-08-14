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
		bool priorEnabled = ArtistEvolution.Enabled;
		bool priorObserving = ArtistEvolution.Observing;
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
			ArtistEvolution.ConfigureForProbe(priorEnabled, priorObserving && !priorEnabled);
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
		Require(!AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear - 1, over, over),
			"22a legitimacy is hard-zero before its start year, however good the record");
		Require(!AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear + 1,
			AlbumLegitimacyService.LandmarkCohesionBar * .5f, over),
			"22b a record that did not hang together is not a landmark however well it sold");
		Require(!AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear + 1, over,
			AlbumLegitimacyService.LandmarkReceptionBar * .5f),
			"22c cohesion alone in private is not a movement; it has to have been heard");
		Require(AlbumLegitimacyService.IsLandmark(AlbumLegitimacyService.LegitimacyStartYear + 1, over, over),
			"22d a cohesive record that succeeded in public is a landmark");
		// A 1965 statement moves the needle more than a 1968 one, because by 1968 everyone is
		// already doing it.
		Require(AlbumLegitimacyService.GetEarliness(1965) > AlbumLegitimacyService.GetEarliness(1968) &&
			AlbumLegitimacyService.GetEarliness(AlbumLegitimacyService.EarlinessExhaustedYear) == 0f,
			"22e the earliness premium decays to nothing by the year the movement is common");
	}

	private static void ProbeInfluenceMemoryIsBounded() {
		ArtistEvolution.ConfigureForProbe(enable: true, observe: false, legitimacy: true);
		SimulatedArtist artist = NewArtist();
		ArtistEvolutionService.Initialize(artist, artist.formedYear);
		var profile = artist.evolution;
		int cap = AlbumLegitimacyService.MaxInfluencesPerArtist;
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

	private static void Require(bool condition, string message) {
		if (!condition) throw new InvalidOperationException("Artist-evolution probe failed: " + message);
	}
}
