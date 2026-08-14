using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The witness with a rubber stamp. This service never picks a genre: it reads the
/// ones GenreSupplyService already picked and, when the evidence is consistent
/// enough and the path is musically real, ratifies the identity the act has been
/// releasing under. Everything here is deterministic; it draws nothing.
/// </summary>
public static class ArtistEvolutionService {
	public sealed class ArtistEvolutionTelemetry {
		public string artistId;
		public int eraIndex;
		public Genre fromGenre;
		public Genre toGenre;
		public ArtistEvolutionTrigger trigger;
		public ArtistArcPhase phase;
		public float commercialPressure;
		public float artisticPressure;
		public float peerPressure;
		public float labelPressure;
		public float internalPressure;
		public float resistance;
		public bool ratified;
		// Diagnosis columns. Section 8's rule on this project is that mechanism claims
		// reasoned from annual rollups have been flatly wrong and decision telemetry
		// settled them; when a genre moves, this file has to say which conversions moved
		// it and which were refused, and by what.
		public int candidateCount;
		public float adjacency;
		public RatificationBlock block;
	}

	public static event Action<ArtistEvolutionTelemetry> OnEvolutionObservation;

	// ---- INITIALIZATION -------------------------------------------------------------------------

	/// <summary>
	/// Derives disposition from the lineup that is already sitting in the band and opens
	/// era 0. Consumes no RNG: every term is a pure read of generated musician traits, so
	/// a run with evolution off and a run with it observing share one global stream.
	/// </summary>
	public static void Initialize(SimulatedArtist artist, int year) {
		if (!ArtistEvolution.Observing || artist == null || artist.evolution != null) return;
		var profile = new ArtistEvolutionProfile {
			artisticCenter = artist.primaryGenre,
			priorArtisticCenter = artist.primaryGenre,
			confidence = .5f,
			frustration = 0f
		};
		DeriveDisposition(artist, profile);
		profile.eras.Add(new ArtistEraRecord {
			eraIndex = 0,
			startYear = artist.formedYear > 0 ? artist.formedYear : year,
			endYear = 0,
			primaryGenre = artist.primaryGenre,
			secondaryGenre = artist.secondaryGenre,
			phase = ArtistArcPhase.Formative,
			trigger = ArtistEvolutionTrigger.None
		});
		artist.evolution = profile;
	}

	/// <summary>
	/// Disposition is a property of the people in the room, so it is recomputed only when
	/// the room changes. Every term is bounded to [0,1] and every input already exists on
	/// <see cref="Musician"/>; personality generation is free.
	/// </summary>
	public static void DeriveDisposition(SimulatedArtist artist, ArtistEvolutionProfile profile) {
		List<Musician> members = artist.members?.Where(member => member.isActive).ToList() ?? new List<Musician>();
		if (members.Count == 0) {
			profile.dispositionMemberCount = 0;
			return;
		}
		float ambition = members.Average(member => member.ambition);
		float ego = members.Average(member => member.ego);
		float loyalty = members.Average(member => member.loyalty);
		float temperament = members.Average(member => member.temperament);
		float reliability = members.Average(member => member.reliability);
		float creativity = members.Average(member => member.creativity);
		float versatility = members.Average(member => member.musicalVersatility);
		float studioEfficiency = members.Average(member => member.studioEfficiency);
		float dramaRisk = members.Average(member => member.GetDramaRisk());
		// The writer is who decides what the next record sounds like. Fall back to the most
		// creative member so a group with no credited writer is not read as having no voice.
		List<Musician> writers = members.Where(member => member.isPrimaryWriter).ToList();
		float writerCreativity = writers.Count > 0 ? writers.Average(member => member.creativity) : members.Max(member => member.creativity);

		profile.artisticAmbition = Clamp01(.50f * writerCreativity + .35f * ambition + .15f * versatility);
		profile.experimentalAppetite = Clamp01(.50f * versatility + .40f * creativity + .10f * ego);
		profile.commercialPragmatism = Clamp01(.40f * reliability + .30f * (1f - creativity) + .30f * loyalty);
		profile.rootsAttachment = Clamp01(.40f * loyalty + .35f * (1f - versatility) + .25f * temperament);
		profile.conceptualThinking = Clamp01(.45f * writerCreativity + .35f * studioEfficiency + .20f * versatility);
		profile.peerSensitivity = Clamp01(.40f * ambition + .35f * versatility + .25f * ego);
		profile.volatility = Clamp01(.60f * dramaRisk + .40f * (1f - artist.groupCohesion));
		profile.dispositionMemberCount = members.Count;
	}

	/// <summary>Re-derives only when the lineup size actually moved. Cheap enough to call per project.</summary>
	private static void RefreshDispositionIfLineupChanged(SimulatedArtist artist) {
		ArtistEvolutionProfile profile = artist.evolution;
		if (profile == null) return;
		int active = artist.members?.Count(member => member.isActive) ?? 0;
		if (active == profile.dispositionMemberCount) return;
		DeriveDisposition(artist, profile);
	}

	internal static void EmitObservation(ArtistEvolutionTelemetry telemetry) => OnEvolutionObservation?.Invoke(telemetry);

	// ---- RATIFICATION ---------------------------------------------------------------------------

	/// <summary>Why a candidate that reached the window majority did not become the identity.</summary>
	public enum RatificationBlock {
		None, WindowNotFull, NoMajority, MajorityIsIdentity, NoMusicalPath, GenreClosedToNewSupply,
		WithinCooldown, TerminalCareer, AnnualBudgetExhausted, GenreOutflowCapReached,
		/// <summary>The act has wandered, but the sides do not agree on where to.</summary>
		NoCoherentDirection
	}

	public readonly struct RatificationVerdict {
		public readonly Genre Candidate;
		public readonly int CandidateCount;
		public readonly RatificationBlock Block;
		public readonly float Adjacency;
		public RatificationVerdict(Genre candidate, int candidateCount, RatificationBlock block, float adjacency) {
			Candidate = candidate; CandidateCount = candidateCount; Block = block; Adjacency = adjacency;
		}
		public bool Ratifiable => Block == RatificationBlock.None;
	}

	/// <summary>
	/// Called once per PROJECT -- not once per record. An album released with a promo
	/// single is one project and one entry in the drift window; counting both halves
	/// would let a single creative decision carry two votes.
	/// </summary>
	public static void OnProjectReleased(SimulatedArtist artist, Genre projectGenre, int year, AILabel label = null) {
		if (!ArtistEvolution.IsObservingLive || artist?.evolution == null) return;
		ObserveProject(artist, projectGenre, year, label);
	}

	/// <summary>
	/// The rule itself, without the live-path gate. Probes drive this directly so they
	/// exercise the same predicate the simulation does rather than a parallel copy.
	/// </summary>
	internal static void ObserveProject(SimulatedArtist artist, Genre projectGenre, int year, AILabel label = null) {
		ArtistEvolutionProfile profile = artist.evolution;
		RefreshDispositionIfLineupChanged(artist);
		profile.PushProjectGenre(GenreCatalog.MapLegacy(projectGenre, year));
		ArtistEraRecord era = profile.CurrentEra;
		if (era != null) era.releases++;
		// Recomputed once per project, then read as two floats by the supply weight on the
		// NEXT selection. With the phase off every pressure term stays 0 and restlessness
		// stays 0, which is the neutral case that reproduces Phase 1 exactly.
		AlbumLegitimacyService.AbsorbLandmarks(artist, year);
		if (ArtistEvolution.PressureEnabled) ArtistEvolutionPressureService.Evaluate(artist, label, year);
		EvaluateAndMaybeRatify(artist, year);
	}

	/// <summary>
	/// Records what the era actually achieved, at the same moment the commercial outcome
	/// lands on the artist. Counting as it happens is what lets a discography be rendered
	/// without a pass over release history per artist.
	/// </summary>
	public static void OnChartRunComplete(SimulatedArtist artist, int peakPosition, bool cohesiveAlbum) {
		if (!ArtistEvolution.Observing || artist?.evolution == null) return;
		ArtistEraRecord era = artist.evolution.CurrentEra;
		if (era == null) return;
		bool charted = peakPosition > 0 && peakPosition <= 100;
		if (charted) era.chartedReleases++;
		if (peakPosition > 0 && peakPosition <= 40) era.top40Releases++;
		if (charted && (era.bestPeakPosition == 0 || peakPosition < era.bestPeakPosition)) era.bestPeakPosition = peakPosition;
		if (cohesiveAlbum) era.cohesiveAlbums++;
	}

	private static void EvaluateAndMaybeRatify(SimulatedArtist artist, int year) {
		ArtistEvolutionProfile profile = artist.evolution;
		Genre identity = GenreCatalog.MapLegacy(artist.primaryGenre, year);
		RatificationVerdict verdict = Evaluate(artist, identity, year);
		// Silence is the common case: most projects are retained and most windows have no
		// majority at all. Only a real candidate is worth a telemetry row -- ratified or
		// blocked, both answer "how often would an act have converted, and to what".
		// Anchored to the verdict rather than to a count literal, so a change to the window
		// or majority constants cannot silently start suppressing legitimate rows.
		if (verdict.Block is RatificationBlock.WindowNotFull or RatificationBlock.NoMajority
			or RatificationBlock.MajorityIsIdentity) return;

		ArtistEvolutionTrigger trigger = DeriveTrigger(artist, verdict.Candidate, year);
		bool ratify = verdict.Ratifiable && ArtistEvolution.Enabled;
		if (ratify) Ratify(artist, identity, verdict.Candidate, trigger, year);
		EmitObservation(new ArtistEvolutionTelemetry {
			artistId = artist.artistId,
			eraIndex = profile.CurrentEra?.eraIndex ?? 0,
			fromGenre = identity,
			toGenre = verdict.Candidate,
			trigger = trigger,
			phase = profile.phase,
			commercialPressure = profile.commercialPressure,
			artisticPressure = profile.artisticPressure,
			peerPressure = profile.peerPressure,
			labelPressure = profile.labelPressure,
			internalPressure = profile.internalPressure,
			resistance = profile.resistance,
			ratified = ratify,
			candidateCount = verdict.CandidateCount,
			adjacency = verdict.Adjacency,
			block = verdict.Block
		});
	}

	/// <summary>
	/// The consolidation rule, in the order the guardrails are cheapest to test. Pure:
	/// it reads state and returns a verdict, so a probe and the counterfactual run
	/// exercise exactly the predicate the live path uses.
	/// </summary>
	public static RatificationVerdict Evaluate(SimulatedArtist artist, Genre identity, int year) {
		ArtistEvolutionProfile profile = artist.evolution;
		if (!HasSufficientWindow(profile)) return new RatificationVerdict(identity, 0, RatificationBlock.WindowNotFull, 0f);
		(Genre candidate, int count, float coherence) = GetDriftDestination(profile, identity);
		if (candidate == identity || count < RequiredOffIdentitySides(profile))
			return new RatificationVerdict(identity, count, RatificationBlock.NoMajority, coherence);
		if (coherence < ArtistEvolution.DriftCoherenceBar)
			return new RatificationVerdict(candidate, count, RatificationBlock.NoCoherentDirection, coherence);

		float adjacency = GenreMarketMomentumService.GetAdjacency(identity, candidate);
		RatificationBlock block =
			adjacency < ArtistEvolution.AdjacencyFloor ? RatificationBlock.NoMusicalPath :
			!GenreSupplyService.IsAvailableForNewSupply(candidate, year) ? RatificationBlock.GenreClosedToNewSupply :
			profile.lastIdentityChangeYear >= 0 && year - profile.lastIdentityChangeYear < ArtistEvolution.IdentityChangeCooldownYears
				? RatificationBlock.WithinCooldown :
			GenreSupplyService.IsTerminalCareerState(artist.careerState) ? RatificationBlock.TerminalCareer :
			!HasAnnualBudget(year) ? RatificationBlock.AnnualBudgetExhausted :
			!HasGenreOutflow(identity, year) ? RatificationBlock.GenreOutflowCapReached :
			RatificationBlock.None;
		return new RatificationVerdict(candidate, count, block, adjacency);
	}

	/// <summary>
	/// A pressured act may act on a shorter window, but only a UNANIMOUS one. An unpressured
	/// act -- or any act at all with Phase 2 off -- needs the full window, which is the
	/// Phase-1 rule unchanged.
	/// </summary>
	private static bool IsPressuredShortWindow(ArtistEvolutionProfile profile) =>
		ArtistEvolution.PressureEnabled && profile.restlessness > 0f &&
		profile.WindowLength >= ArtistEvolution.PressuredWindowMinimum;

	private static bool HasSufficientWindow(ArtistEvolutionProfile profile) =>
		profile.WindowLength >= ArtistEvolution.DriftEvidenceWindow || IsPressuredShortWindow(profile);

	/// <summary>
	/// Under pressure one off-identity side is enough to read as a move; unpressured, it
	/// takes two. Pressure shortens the evidence required, never the coherence bar.
	/// </summary>
	private static int RequiredOffIdentitySides(ArtistEvolutionProfile profile) =>
		IsPressuredShortWindow(profile) ? 1 : ArtistEvolution.DriftOffIdentityMinimum;

	/// <summary>
	/// Where the recent off-identity sides point, as a body. Each off-identity side votes
	/// for every reachable destination in proportion to how close it sits to it, so a run of
	/// scattered but same-direction records elects the genre at their centre. An exact repeat
	/// votes 1.0 for itself and therefore still wins outright -- the old majority rule is the
	/// ceiling case of this one, not a casualty of it.
	/// </summary>
	private static (Genre Genre, int OffIdentityCount, float Coherence) GetDriftDestination(
		ArtistEvolutionProfile profile, Genre identity) {
		int length = profile.WindowLength;
		int offCount = 0;
		for (int i = 0; i < length; i++) if (profile.recentProjectGenres[i] != identity) offCount++;
		if (offCount == 0) return (identity, 0, 0f);

		Genre best = identity;
		float bestScore = 0f;
		for (int i = 0; i < length; i++) {
			Genre candidate = profile.recentProjectGenres[i];
			if (candidate == identity) continue;
			// Deliberately NOT filtered by adjacency here. The vote elects where the drift
			// actually points; the musical-path guardrail then refuses it out loud if that
			// is somewhere unreachable. Filtering first would let the rule quietly re-elect a
			// second-choice destination the act was not moving toward, and would erase the
			// refusal reason that told us four of the first twelve candidates had no path.
			float score = 0f;
			for (int j = 0; j < length; j++) {
				if (profile.recentProjectGenres[j] == identity) continue;
				score += GenreMarketMomentumService.GetAdjacency(profile.recentProjectGenres[j], candidate);
			}
			score /= offCount;
			if (score <= bestScore) continue;
			best = candidate;
			bestScore = score;
		}
		return (best, offCount, bestScore);
	}

	/// <summary>
	/// Closes the era the act was living in and opens the one they have already been
	/// recording. Atomic and idempotent: the window is cleared and the cooldown stamped
	/// in the same call, so a second call in the same year cannot re-open the same era.
	/// </summary>
	private static void Ratify(SimulatedArtist artist, Genre from, Genre to, ArtistEvolutionTrigger trigger, int year) {
		ArtistEvolutionProfile profile = artist.evolution;
		ArtistEraRecord closing = profile.CurrentEra;
		if (closing != null && closing.IsOpen) {
			closing.endYear = year;
			// The era's own predecessor is the genre it demoted when it opened, not whatever
			// the profile's prior center happens to be now -- recomposing with the latter
			// would rewrite a closed era's opening line with a later era's history.
			closing.summary = ArtistEraSummaryComposer.Compose(artist, closing, closing.secondaryGenre);
		}

		// formationPrimaryGenre / formationSecondaryGenre are deliberately untouched: the
		// native-vs-transitioned telemetry keys off them and must keep meaning "against
		// where they started", not "against where they were last week".
		artist.primaryGenre = to;
		artist.secondaryGenre = from;
		profile.priorArtisticCenter = from;
		profile.artisticCenter = to;
		profile.lastIdentityChangeYear = year;
		profile.projectsSinceIdentityChange = 0;
		profile.phase = DerivePhase(artist, trigger);
		profile.ResetWindow();
		var opening = new ArtistEraRecord {
			eraIndex = profile.eras.Count,
			startYear = year,
			endYear = 0,
			primaryGenre = to,
			secondaryGenre = from,
			phase = profile.phase,
			trigger = trigger
		};
		// The opening line is written from the motive, which is known now; the outcome
		// clause is added when the era closes and there is something to report.
		opening.summary = ArtistEraSummaryComposer.Compose(artist, opening, from);
		profile.eras.Add(opening);
		// One string per ERA, never one per evaluation. careerEvents is a narration buffer
		// on 22.5k artists, not a data structure; the typed era list is the data.
		artist.careerEvents.Add($"{year}: moved from {from} toward {to} ({trigger})");
		ChargeConversion(from, year);
	}

	private static ArtistEvolutionTrigger DeriveTrigger(SimulatedArtist artist, Genre candidate, int year) {
		if (candidate == GenreCatalog.MapLegacy(artist.formationPrimaryGenre, year)) return ArtistEvolutionTrigger.BackToRoots;
		// With motive available, the dominant pressure IS the motive; the outcome-history
		// reading below is the Phase-1 fallback for when nothing is pressing.
		if (ArtistEvolution.PressureEnabled && artist.evolution.dominantTrigger != ArtistEvolutionTrigger.None &&
			artist.evolution.restlessness > 0f) return artist.evolution.dominantTrigger;
		if (artist.consecutiveFlops >= 2) return ArtistEvolutionTrigger.CommercialFailure;
		if (artist.consecutiveHits >= 2) return ArtistEvolutionTrigger.CommercialBreakthrough;
		if (GenreCatalog.Get(candidate).GetLifecycle(year) == GenreLifecycleState.Emerging) return ArtistEvolutionTrigger.GenreClimateShift;
		return ArtistEvolutionTrigger.PersonalAmbition;
	}

	private static ArtistArcPhase DerivePhase(SimulatedArtist artist, ArtistEvolutionTrigger trigger) => trigger switch {
		ArtistEvolutionTrigger.BackToRoots => ArtistArcPhase.RootsReturn,
		ArtistEvolutionTrigger.CommercialFailure => ArtistArcPhase.CommercialPivot,
		_ => artist.careerState switch {
			CareerState.Superstar or CareerState.Star or CareerState.Established => ArtistArcPhase.Consolidation,
			CareerState.Rising => ArtistArcPhase.Breakthrough,
			CareerState.Declining => ArtistArcPhase.Declining,
			CareerState.Dropped or CareerState.Retired or CareerState.Disbanded => ArtistArcPhase.Legacy,
			_ => ArtistArcPhase.HitSeeking
		}
	};

	// ---- ANNUAL GUARDRAIL BUDGET ----------------------------------------------------------------
	// Identity migration is a positive feedback loop: retention is read from the identity
	// genre's own baseline curve, so converting onto a rising genre makes staying there more
	// likely than staying put was. These caps are the circuit breaker, not the design -- if
	// they bind in most years the rule is mistuned and wants fixing upstream, not a bigger cap.

	private static int budgetYear = int.MinValue;
	private static int annualConversionBudget;
	private static int annualConversions;
	private static readonly Dictionary<Genre, int> AnnualGenreOutflowCap = new();
	private static readonly Dictionary<Genre, int> AnnualGenreOutflow = new();

	private static void EnsureAnnualBudget(int year) {
		if (budgetYear == year) return;
		budgetYear = year;
		annualConversions = 0;
		AnnualGenreOutflow.Clear();
		AnnualGenreOutflowCap.Clear();
		// One sweep of the registry per YEAR -- ten sweeps across a decade run. There is no
		// weekly sweep over the artist registry in this design and there must not be one.
		int population = 0;
		IReadOnlyCollection<SimulatedArtist> registry = ArtistManager.Instance?.GetAllArtists();
		if (registry != null) foreach (SimulatedArtist candidate in registry) {
			if (!candidate.isActive || GenreSupplyService.IsTerminalCareerState(candidate.careerState)) continue;
			population++;
			Genre canonical = GenreCatalog.MapLegacy(candidate.primaryGenre, year);
			AnnualGenreOutflowCap[canonical] = AnnualGenreOutflowCap.GetValueOrDefault(canonical) + 1;
		}
		annualConversionBudget = Mathf.CeilToInt(population * ArtistEvolution.AnnualConversionBudgetShare);
		foreach (Genre genre in AnnualGenreOutflowCap.Keys.ToArray())
			// A scene thins; it does not evaporate. The floor of 1 keeps a six-act scene from
			// being frozen solid by integer truncation rather than by a real cap.
			AnnualGenreOutflowCap[genre] = Mathf.Max(1, Mathf.RoundToInt(AnnualGenreOutflowCap[genre] * ArtistEvolution.AnnualGenreOutflowCap));
	}

	private static bool HasAnnualBudget(int year) {
		EnsureAnnualBudget(year);
		return annualConversions < annualConversionBudget;
	}

	private static bool HasGenreOutflow(Genre identity, int year) {
		EnsureAnnualBudget(year);
		return AnnualGenreOutflow.GetValueOrDefault(identity) < AnnualGenreOutflowCap.GetValueOrDefault(identity, int.MaxValue);
	}

	private static void ChargeConversion(Genre from, int year) {
		EnsureAnnualBudget(year);
		annualConversions++;
		AnnualGenreOutflow[from] = AnnualGenreOutflow.GetValueOrDefault(from) + 1;
	}

	public static int GetAnnualConversionsForProbe() => annualConversions;
	public static int GetAnnualConversionBudgetForProbe() => annualConversionBudget;

	/// <summary>
	/// Seeds the annual budget directly so a probe can exercise the cap without standing up
	/// a 22.5k registry. Fixtures derive their numbers from the constants, never from a
	/// literal one under the bar -- those silently invert when the bar is re-derived.
	/// </summary>
	internal static void SeedAnnualBudgetForProbe(int year, int population, Genre identity, int identityPopulation) {
		budgetYear = year;
		annualConversions = 0;
		AnnualGenreOutflow.Clear();
		AnnualGenreOutflowCap.Clear();
		annualConversionBudget = Mathf.CeilToInt(population * ArtistEvolution.AnnualConversionBudgetShare);
		AnnualGenreOutflowCap[identity] = Mathf.Max(1, Mathf.RoundToInt(identityPopulation * ArtistEvolution.AnnualGenreOutflowCap));
	}

	internal static void ExhaustAnnualBudgetForProbe() => annualConversions = annualConversionBudget;
	internal static void ExhaustGenreOutflowForProbe(Genre identity) =>
		AnnualGenreOutflow[identity] = AnnualGenreOutflowCap.GetValueOrDefault(identity, int.MaxValue);
	internal static void ResetAnnualBudgetForProbe() {
		budgetYear = int.MinValue;
		annualConversions = 0;
		AnnualGenreOutflow.Clear();
		AnnualGenreOutflowCap.Clear();
	}

	private static float Clamp01(float value) => Mathf.Clamp(value, 0f, 1f);
}
