using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>One thing that happened (or would have happened) to a person or an act. Telemetry and the Band Room
/// both read these; every field is a sim fact, never prose.</summary>
public sealed class BandLifeEvent {
	public int year;
	public string artistId, stageName, personId, personName, otherPersonId;
	public string eventType;
	public DepartureKind kind;
	public StrainCause cause;
	public float strain;
	public bool everCharted;
	public bool chartedThisYear;
	public LineupConstitution constitution;
	public DeathChannel channel;
	/// <summary>True when the event changed the world; false when it is an observe-only would-be.</summary>
	public bool applied;
	public bool playerOwned;
	public int age;
	public string detail;
}

/// <summary>One year of the annual pass, summarised. Phase 2 sizes the breaker and fits rates from these.</summary>
public sealed class BandLifeAnnualSummary {
	public int year;
	public int actsInScope, groupActs, chartedGroupActs, edges, secretEdges, brewing, ultimatums;
	public float meanStrain, maxStrain, meanMorale;
	public int strainDepartures, breakerCap, breakerDeferred, cooldownDeferred, aiConcessions;
	public readonly int[] departuresByKind = new int[Enum.GetValues(typeof(DepartureKind)).Length];
	public readonly int[] departuresByCause = new int[Enum.GetValues(typeof(StrainCause)).Length];
	public readonly int[] deathsByChannel = new int[Enum.GetValues(typeof(DeathChannel)).Length];
	public int deathsCharting, drafted, draftedCharting, draftEligible, draftReturns;
	public int exhaustion, substanceOnsets, busts, marriages, children, couples, coupleBreakups, affairs, discoveries;
	public int quietDissolutionsCharted, quietDissolutionsNeverCharted;
	public int strainDeparturesCharted, strainDeparturesNeverCharted, actsLosingMemberCharted, actsLosingMemberNeverCharted;
	public int poolSize, poolEntries, poolExpired, replacements, replacementsFromPool, spinOuts, dissolutions, recombinations;
	public int leavingMemberOptions;
}

/// <summary>
/// The band-member simulation's annual pass (SimTools/BandMemberSimulationDirective.md §4). ONE implementation
/// with a scope switch (§6): observe-only measures the whole world and writes only new state; roster scope
/// changes only the player's acts; world scope changes everyone. Nothing here draws from the population or
/// global stream: every chance is a keyed draw on the world seed (<see cref="BandLife.Unit"/>).
/// <para>
/// The pass runs once per year at the population lifecycle's year rollover, over every working act in scope.
/// Strain is a LEVEL, not a sum (§2.14): it relaxes each year, so the thresholds compare a level to a level and
/// a long-lived band is not certain to split. Departures move through visible stages -- Brewing, Ultimatum,
/// Departure (§2.15) -- so the player gets a warning and a window, and AI acts are resolved by a default policy.
/// </para>
/// </summary>
public static class BandLifeService {
	// ==== strain (calibrated in Phase 2 against the observe run) ======================================
	/// <summary>Share of last year's strain that is still there this year. Strain is a level, not a sum.</summary>
	public const float StrainRetention = 0.60f;
	public const float BrewingStrain = 0.45f;
	public const float UltimatumStrain = 0.65f;
	public const float DepartureStrain = 0.85f;
	public const float EdgeEpsilon = 0.01f;
	/// <summary>Good morale delays the stages; bad morale speeds them (§4.5 consumer 1).</summary>
	public const float MoraleStageShift = 0.5f;
	public const float SolventCohesionWeight = 0.30f;
	/// <summary>How much of a year's success and resting cohesion actually absorbs incoming strain.</summary>
	public const float SolventScale = 0.77f;
	/// <summary>Overall level of the pairwise terms (not Burnout). Fitted offline with SimTools/fit_band_life_strain.py
	/// on the 2026-10-07 observe decade: charting groups lose a member to strain at ~10% a year, never-charted groups
	/// rarely (they dissolve quietly), and no cause takes more than ~30% of departures.</summary>
	public const float StrainScale = 1.45f;

	// The first fit (credit .6, spotlight .4, friction .015) left credit and spotlight an order of magnitude under the
	// static personality terms, so any uniform scale-up made Direction and Reliability win every departure. The event
	// terms carry the tail; the static terms are the background they land on.
	public const float CreditMoneyWeight = 2.0f;
	public const float SpotlightWeight = 2.5f;
	public const float DirectionWeight = 0.25f;
	public const float ProjectFrictionWeight = 0.054f;
	public const float ReliabilityWeight = 0.30f;
	public const float SubstanceReliabilityWeight = 0.95f;
	public const float BurnoutWeight = 0.35f;
	public const float PartnerOutsiderWeight = 0.10f;
	public const float RivalryWeight = 0.06f;
	public const float RivalryToDirection = 0.10f;
	public const float RivalryToCredit = 0.05f;
	/// <summary>Sidemen are employees: their strain rarely passes Brewing (§4.3).</summary>
	public const float SidemanStrainScale = 0.50f;
	public const float NameOwnedStrainScale = 0.60f;

	// ==== departures ===================================================================================
	public const int MemberChangeCooldownYears = 1;
	/// <summary>A solo debut that isn't anonymous: the member's own name has to carry a record.</summary>
	public const float SoloLaunchBar = 0.05f;
	public const float SoloSpotlightShare = 0.50f;
	/// <summary>Annual cap on APPLIED strain-driven AI departures. A circuit breaker, not the rate: set at roughly
	/// twice the would-be rate the Phase 2 observe run measured. Deaths, the draft and life events are uncapped.</summary>
	public static int BreakerAnnualCap = 700;
	/// <summary>Of the vacancies the formation servo fills while the pool can staff one, the share built from pooled people.</summary>
	public const float RecombinationShare = 0.25f;

	// ==== life events ====================================================================================
	/// <summary>Fitted on the 2026-10-07 observe decades: .30 cost 3.5-4.5% of charting groups a member to service by
	/// 1968 against the reference set's ~2.75% (3 of 109 acts; wide error). The year shape follows the induction table.</summary>
	public const float DraftActExposure = 0.18f;
	/// <summary>Halved from .0006: travel alone gave 12-14 charting deaths over 1960-65 against ~7 in the reference.</summary>
	public const float TravelDeathRate = 0.00030f;
	public const float IllnessDeathRate = 0.00012f;
	public const float MisadventureDeathRate = 0.00008f;
	public const float SubstanceDeathRate = 0.060f; // with the habit draw: ~15 deaths a seed, nearly all 1968-69 (offline Monte Carlo over the world-run onsets)
	public const float SubstanceDeathThreshold = 0.50f;
	public const float SubstanceOnsetRate = 0.05f;
	/// <summary>Once using, the yearly chance of carrying on, before reliability and the era ramp. Without it a single
	/// onset only decayed and nobody ever reached the bust or death thresholds: the late-decade channel was dead.</summary>
	public const float SubstancePersistence = 0.80f;
	public const float ExhaustionRate = 0.012f;
	public const float MarriageRate = 0.07f;
	public const float ChildrenRate = 0.12f;
	public const float InBandCoupleRate = 0.010f;
	public const float CoupleBreakupRate = 0.25f;
	public const float AffairRate = 0.05f;
	public const float AffairDiscoveryRate = 0.35f;

	// ==== morale =========================================================================================
	public const float MoraleMin = -0.30f, MoraleMax = 0.20f, MoraleRelax = 0.70f;
	public const float MoraleTop40 = 0.10f, MoraleCharted = 0.04f, MoraleFlop = 0.007f, MoraleRoad = 0.04f;
	public const float MoraleHoneymoon = 0.10f, MoraleAcrimony = -0.08f, MoraleDeath = -0.25f, MoraleService = -0.06f;

	// ==== state ==========================================================================================
	private static int lastAnnualYear = -1;
	private static int formationDebt;
	private static int formationCredit;
	private static readonly Dictionary<int, int> appliedStrainDeparturesByYear = new();

	public static event Action<BandLifeEvent> OnEvent;
	public static event Action<BandLifeAnnualSummary> OnAnnualSummary;

	/// <summary>Calibration telemetry (Phase 2): the RAW, unweighted strain terms of every pair each year, so the
	/// weights, retention and thresholds can be fitted by replaying the level offline instead of by decade runs.
	/// Raised only when something subscribes; costs nothing otherwise.</summary>
	public static event Action<PairTermRow> OnPairTerms;
	public sealed class PairTermRow {
		public int year; public string artistId; public bool everCharted; public int chartedNow, top40Now;
		public LineupConstitution constitution; public string personA, personB;
		public float creditRaw, spotlightRaw, directionRaw, projectFriction, reliabilityRaw, substanceRaw, outsiderRaw, rivalry;
		public float scale, workFactor, solvent, strainBefore, strainAfter;
		public float creditShareA, creditShareB, temperamentA, temperamentB, loyaltyA, loyaltyB, egoA, egoB, ambitionA, ambitionB;
		public float reliabilityA, reliabilityB, substanceA, substanceB, spotA, spotB, recognitionA, recognitionB;
		public bool soloViableA, soloViableB, writerA, writerB, leadA, leadB;
		public bool burnoutRow; public float burnoutRaw, morale; public string burnoutDefaultA, burnoutDefaultB;
	}

	public static int LastAnnualYear => lastAnnualYear;
	public static int FormationDebt => formationDebt;
	public static int FormationCredit => formationCredit;

	private sealed class ActYear {
		public SimulatedArtist artist;
		public int year;
		public bool apply;
		public LineupConstitution constitution;
		public List<Musician> present;
		public int chartedNow, top40Now, breakoutsNow, releasesNow, unitsNow;
		public float roadLoad, success, money, fame, restingCohesion, workFactor;
		public bool everCharted;
		public bool honeymoon;
	}

	private sealed class DepartureCandidate {
		public ActYear ctx;
		public Musician leaver;
		public Musician complainer;
		public DepartureKind kind;
		public StrainCause cause;
		public float strain;
		public bool strainDriven;
		public DeathChannel channel;
	}

	// ======================================================================================================
	// ENTRY
	// ======================================================================================================

	/// <summary>
	/// Called at the population lifecycle's year rollover with the NEW year; processes the year that just
	/// ended. A no-op unless the annual pass is active (observe, roster or world scope).
	/// </summary>
	public static void OnYearBoundary(int newYear) {
		if (!BandLife.AnnualPassActive || ArtistManager.Instance == null) return;
		int year = newYear - 1;
		if (year <= lastAnnualYear) return;
		lastAnnualYear = year;
		var summary = new BandLifeAnnualSummary { year = year };

		ReturnDraftees(year, summary);
		var candidates = new List<DepartureCandidate>();
		var acts = ArtistManager.Instance.GetAllArtists()
			.Where(InScope).OrderBy(a => a.artistId, StringComparer.Ordinal).ToList();
		var contexts = new List<ActYear>(acts.Count);
		foreach (SimulatedArtist artist in acts) {
			ActYear ctx = BuildContext(artist, year);
			contexts.Add(ctx);
			ProcessAct(ctx, summary, candidates);
		}
		ResolveDepartures(candidates, summary);
		foreach (PooledPerson gone in PersonPool.ExpireStale(year)) {
			summary.poolExpired++;
			Emit(new BandLifeEvent { year = year, personId = gone.person.personId, personName = gone.person.FullName,
				artistId = gone.lastArtistId, stageName = gone.lastStageName, eventType = "pool-expired", applied = true,
				age = gone.person.GetAge(year) });
		}
		foreach (ActYear ctx in contexts) Snapshot(ctx.artist);
		Summarize(contexts, summary);
		OnAnnualSummary?.Invoke(summary);
	}

	private static bool InScope(SimulatedArtist a) =>
		a != null && a.lifecycleStatus == ArtistLifecycleStatus.Active && a.observedEndYear == 0 &&
		a.members != null && a.members.Count > 0 &&
		(BandLife.ObservingWorld || (BandLife.RosterChurn && a.isPlayerOwned));

	/// <summary>Whether this act's lineup actually changes (rather than being measured).</summary>
	public static bool Applies(SimulatedArtist a) => a != null && (BandLife.WorldChurn || (BandLife.RosterChurn && a.isPlayerOwned));

	private static void Snapshot(SimulatedArtist a) {
		a.bandLifeYear = lastAnnualYear;
		a.chartedAtYearStart = a.charted;
		a.top40AtYearStart = a.top40Hits;
		a.breakoutsAtYearStart = a.regionalBreakouts;
		a.releasesAtYearStart = a.totalReleases;
		a.unitsAtYearStart = a.totalUnitsSold;
		if (a.writingPartnerships != null)
			foreach (WritingPartnership p in a.writingPartnerships) p.coCreditsAtYearStart = p.coCredits;
	}

	/// <summary>An old save, or an act seen for the first time mid-career, starts its yearly evidence from now
	/// rather than reading its whole lifetime as one year.</summary>
	public static void BaselineSnapshots(IEnumerable<SimulatedArtist> artists) {
		foreach (SimulatedArtist a in artists) if (a != null && a.bandLifeYear < 0 && a.totalReleases > 0) Snapshot(a);
	}

	// ======================================================================================================
	// CONTEXT
	// ======================================================================================================

	public static LineupConstitution ConstitutionOf(SimulatedArtist a) {
		int active = a.members?.Count(m => m != null && m.isActive) ?? 0;
		if (a.type is ArtistType.SoloMale or ArtistType.SoloFemale || active <= 1) return LineupConstitution.Solo;
		if (a.type == ArtistType.Duo || active == 2) return LineupConstitution.Duo;
		if (a.instrumentalPerformance && a.members.Any(m => m.isActive && m.isBandLeader)) return LineupConstitution.LeaderAndSidemen;
		if (a.type == ArtistType.VocalGroup && a.manager == ManagerArchetype.Svengali) return LineupConstitution.NameOwned;
		return LineupConstitution.Band;
	}

	private static ActYear BuildContext(SimulatedArtist a, int year) {
		var ctx = new ActYear {
			artist = a, year = year, apply = Applies(a), constitution = ConstitutionOf(a),
			present = a.members.Where(m => m != null && m.isActive && m.observedGoneYear == 0 &&
				m.lifeState is MemberLifeState.Active or MemberLifeState.StudioOnly).ToList(),
			chartedNow = Mathf.Max(0, a.charted - a.chartedAtYearStart),
			top40Now = Mathf.Max(0, a.top40Hits - a.top40AtYearStart),
			breakoutsNow = Mathf.Max(0, a.regionalBreakouts - a.breakoutsAtYearStart),
			releasesNow = Mathf.Max(0, a.totalReleases - a.releasesAtYearStart),
			unitsNow = Mathf.Max(0, a.totalUnitsSold - a.unitsAtYearStart),
			everCharted = a.charted > 0,
			restingCohesion = a.groupCohesion,
			honeymoon = a.honeymoonUntilYear >= year,
		};
		ctx.success = Mathf.Min(0.35f, (ctx.chartedNow > 0 ? 0.12f : 0f) + (ctx.top40Now > 0 ? 0.12f : 0f) +
			(ctx.breakoutsNow > 0 ? 0.06f : 0f) + 0.03f * Mathf.Min(2, ctx.top40Now));
		ctx.money = Mathf.Clamp(ctx.unitsNow / 150000f, 0f, 1f);
		ctx.fame = Mathf.Clamp(a.publicRecognition * 2.5f, 0f, 1f);
		ctx.roadLoad = RoadLoad(a, ctx);
		ctx.workFactor = 0.3f + ctx.roadLoad;
		return ctx;
	}

	/// <summary>
	/// The annual road and room load an act carried, 0..1. There is no gig simulation for AI acts, so this is
	/// read from the act's state (§4.10's work proxy): a signed act tours on its chart evidence, an unsigned
	/// working act plays the rooms of its scene, a held act is idle. The player's own bookings override it.
	/// </summary>
	private static float RoadLoad(SimulatedArtist a, ActYear ctx) {
		float booked = PlayerRoadLoad?.Invoke(a) ?? -1f;
		if (booked >= 0f) return Mathf.Clamp(booked, 0f, 1f);
		if (!string.IsNullOrEmpty(a.labelId))
			return Mathf.Clamp(0.30f + 0.25f * Mathf.Min(1, ctx.chartedNow) + 0.25f * Mathf.Min(1, ctx.top40Now) +
				0.10f * Mathf.Min(1, ctx.breakoutsNow), 0f, 0.95f);
		if (a.prospectMarketStatus == ProspectMarketStatus.Seeking) return IsClubFamily(a.primaryGenre) ? 0.45f : 0.25f;
		return 0.08f;
	}

	/// <summary>The player's booked road load for one of their acts, or a negative number for "not the player's
	/// act / nothing booked". Set by PlayerDesk; null in headless runs.</summary>
	public static Func<SimulatedArtist, float> PlayerRoadLoad;

	private static GenreFamily FamilyOf(Genre g) => GenreCatalog.TryGet(g, out var p) ? p.Family : GenreFamily.Pop;

	private static bool IsClubFamily(Genre g) => FamilyOf(g) is GenreFamily.Rock or GenreFamily.RhythmAndSoul or
		GenreFamily.Blues or GenreFamily.Jazz or GenreFamily.Country;

	/// <summary>The region model holds only US regions; a British act exists only as a British identity.</summary>
	private static bool IsBritishAct(SimulatedArtist a) =>
		a.formationPrimaryGenre is Genre.BritishInvasion or Genre.BritishBeat or Genre.BritishPop or Genre.BritishBlues or Genre.Skiffle ||
		a.primaryGenre is Genre.BritishInvasion or Genre.BritishBeat or Genre.BritishPop or Genre.BritishBlues or Genre.Skiffle;

	// ======================================================================================================
	// THE PASS, PER ACT
	// ======================================================================================================

	private static void ProcessAct(ActYear ctx, BandLifeAnnualSummary summary, List<DepartureCandidate> candidates) {
		SimulatedArtist a = ctx.artist;
		// A studio-only member makes the records and stays home for the dates.
		foreach (Musician m in ctx.present) if (m.lifeState != MemberLifeState.StudioOnly) m.roadYears += ctx.roadLoad;
		LifeEvents(ctx, summary, candidates);
		if (ctx.present.Count == 0) return;
		UpdateMorale(ctx);
		if (ctx.constitution != LineupConstitution.Solo) {
			Romance(ctx, summary);
			AccrueStrain(ctx);
			AccrueRivalry(ctx);
			EvaluateStages(ctx, summary, candidates);
		}
		if (a.relations != null) {
			a.relations.RemoveAll(e => e.strain < EdgeEpsilon && e.rivalry < EdgeEpsilon && !e.secret);
			if (a.relations.Count == 0) a.relations = null;
		}
	}

	// ---- life events (§4.8) -----------------------------------------------------------------------------

	private static void LifeEvents(ActYear ctx, BandLifeAnnualSummary summary, List<DepartureCandidate> candidates) {
		SimulatedArtist a = ctx.artist;
		int year = ctx.year;
		foreach (Musician m in ctx.present.ToList()) {
			string k = $"{m.personId}|{year}";
			int age = m.GetAge(year);

			// Substance (§4.8): ramps up from 1965, keyed to unreliability, fame and the scene.
			float personalFame = Mathf.Max(Mathf.Clamp(m.personalRecognition * 4f, 0f, 1f), ctx.fame * 0.6f);
			float onset = SubstanceOnsetRate * (1f - m.reliability) * (0.3f + personalFame) * SceneSubstance(a.primaryGenre) *
				SubstanceEra(year) * (0.5f + ctx.roadLoad);
			bool usingNow = m.substanceLoad >= 0.05f;
			float carryOn = usingNow ? Mathf.Clamp(SubstancePersistence * (0.6f + 0.8f * (1f - m.reliability)), 0f, 1f) * SubstanceEra(year) : 0f;
			if (BandLife.Chance(k + "|substance", onset)) {
				bool first = m.substanceLoad < 0.05f;
				m.substanceLoad = Mathf.Clamp(m.substanceLoad + 0.15f + 0.15f * BandLife.Unit(k + "|dose"), 0f, 1f);
				if (first) { summary.substanceOnsets++; EmitPerson(ctx, m, "substance-onset", detail: F(m.substanceLoad)); }
			} else if (BandLife.Chance(k + "|substance-continues", carryOn)) {
				// Habit: a smaller dose on top of what is already there.
				m.substanceLoad = Mathf.Clamp(m.substanceLoad + 0.10f + 0.10f * BandLife.Unit(k + "|dose-continued"), 0f, 1f);
			} else if (m.substanceLoad > 0f) {
				m.substanceLoad = Mathf.Max(0f, m.substanceLoad * 0.85f - 0.01f);
			}
			if (m.substanceLoad > SubstanceDeathThreshold) m.substanceHeavyYears++;
			if (m.substanceLoad > 0.35f && BandLife.Chance(k + "|bust", 0.05f * m.substanceLoad * (0.3f + personalFame))) {
				summary.busts++;
				EmitPerson(ctx, m, "bust", detail: F(m.substanceLoad));
			}

			// Death (§4.8.2): four channels, rare early, the substance channel rising late. The 27 Club is an
			// outcome of where these inputs peak together, never a term keyed on age.
			float successTier = ctx.top40Now > 0 ? 1f : ctx.chartedNow > 0 ? 0.5f : 0f;
			float travel = TravelDeathRate * ctx.roadLoad * (0.3f + 2f * successTier);
			float illness = IllnessDeathRate * Mathf.Exp((age - 40) / 8f);
			float misadventure = MisadventureDeathRate * (0.5f + personalFame);
			float substance = m.substanceLoad > SubstanceDeathThreshold
				? SubstanceDeathRate * (m.substanceLoad - SubstanceDeathThreshold) * (1 + m.substanceHeavyYears) * SubstanceEra(year)
				: 0f;
			float death = travel + illness + misadventure + substance;
			if (BandLife.Chance(k + "|death", death)) {
				float pick = BandLife.Unit(k + "|death-channel") * death;
				DeathChannel channel = (pick -= travel) < 0f ? DeathChannel.Travel : (pick -= illness) < 0f ? DeathChannel.Illness :
					(pick -= misadventure) < 0f ? DeathChannel.Misadventure : DeathChannel.Substance;
				m.lifeState = MemberLifeState.Deceased;
				m.deathYear = year;
				m.deathChannel = channel;
				summary.deathsByChannel[(int)channel]++;
				if (ctx.everCharted) summary.deathsCharting++;
				ctx.present.Remove(m);
				candidates.Add(new DepartureCandidate { ctx = ctx, leaver = m, kind = DepartureKind.Death, channel = channel });
				continue;
			}

			// The draft (§4.8.1): the real induction curve, scaled by one exposure factor.
			if (m.isMale && age >= 19 && age <= 26 && !m.hasChildren && !IsBritishAct(a)) {
				summary.draftEligible++;
				if (BandLife.Chance(k + "|draft", DraftActExposure * DraftCurve.Hazard(year))) {
					m.lifeState = MemberLifeState.Drafted;
					m.lifeStateUntilYear = year + 2;
					summary.drafted++;
					if (ctx.everCharted) summary.draftedCharting++;
					ctx.present.Remove(m);
					candidates.Add(new DepartureCandidate { ctx = ctx, leaver = m, kind = DepartureKind.Service });
					continue;
				}
			}

			// Exhaustion: the road wears a short fuse down. A writer or studio hand stays for the records.
			float exhaustion = ExhaustionRate * m.roadYears * (1f - m.temperament) * Mathf.Clamp((age - 18) / 16f, 0.2f, 1.3f);
			if (m.lifeState == MemberLifeState.Active && BandLife.Chance(k + "|exhaustion", exhaustion)) {
				summary.exhaustion++;
				bool studioRole = ctx.constitution != LineupConstitution.Solo && (m.isPrimaryWriter || m.studioEfficiency > 0.6f);
				if (studioRole) {
					m.lifeState = MemberLifeState.StudioOnly;
					EmitPerson(ctx, m, "studio-only", kind: DepartureKind.StudioOnly, cause: StrainCause.Burnout);
					NotifyPlayer(ctx, m, "studio-only", DepartureKind.StudioOnly, StrainCause.Burnout);
				} else {
					m.lifeState = MemberLifeState.Retired;
					ctx.present.Remove(m);
					candidates.Add(new DepartureCandidate { ctx = ctx, leaver = m, kind = DepartureKind.LifeEvent, cause = StrainCause.Burnout });
					continue;
				}
			}

			// Marriage and children: a partner off the road pulls toward home; a father is draft-exempt.
			bool single = m.partner == null || m.partner.state is PartnerState.Divorced or PartnerState.Separated;
			float ageRamp = age < 20 ? 0.2f : age <= 30 ? 1f : age <= 40 ? 0.5f : 0.15f;
			if (single && BandLife.Chance(k + "|marry", MarriageRate * ageRamp)) {
				bool partnerMale = BandLife.Unit(k + "|partner-sex") < (m.isMale ? 0.03f : 0.97f);
				m.partner = new MusicianPartner {
					name = $"{BandLifeNames.FirstName(partnerMale, k + "|partner")}", isMale = partnerMale,
					sinceYear = year, state = PartnerState.Married,
					sitsInOnSessions = BandLife.Chance(k + "|sits-in", 0.20f)
				};
				summary.marriages++;
				EmitPerson(ctx, m, "marriage", detail: m.partner.name);
				NotifyPlayer(ctx, m, "marriage", DepartureKind.None, StrainCause.Outsider);
			} else if (!single && m.partner.state == PartnerState.Married && !m.hasChildren &&
				BandLife.Chance(k + "|children", ChildrenRate)) {
				m.hasChildren = true;
				summary.children++;
				EmitPerson(ctx, m, "children");
			}
		}
	}

	private static float SubstanceEra(int year) => year <= 1964 ? 0.25f : year switch {
		1965 => 0.40f, 1966 => 0.60f, 1967 => 0.85f, _ => 1.0f
	};

	private static float SceneSubstance(Genre g) => FamilyOf(g) switch {
		GenreFamily.Rock or GenreFamily.Jazz or GenreFamily.Blues => 1.6f,
		GenreFamily.RhythmAndSoul or GenreFamily.Country => 1.0f,
		GenreFamily.Gospel or GenreFamily.Classical or GenreFamily.NonMusic => 0.3f,
		_ => 0.7f
	};

	/// <summary>Draftees whose two years are up come home: back to their act if it still exists and still wants
	/// them, otherwise into the pool as a free agent.</summary>
	private static void ReturnDraftees(int year, BandLifeAnnualSummary summary) {
		// Observe scope: the draftee never left the lineup; he simply comes back to it.
		foreach (SimulatedArtist a in ArtistManager.Instance.GetAllArtists())
			if (a?.members != null)
				foreach (Musician m in a.members)
					if (m.lifeState == MemberLifeState.Drafted && m.lifeStateUntilYear <= year) {
						m.lifeState = MemberLifeState.Active;
						summary.draftReturns++;
					}
		foreach (PooledPerson p in PersonPool.Ordered().ToList()) {
			Musician m = p.person;
			if (m.lifeState != MemberLifeState.Drafted || m.lifeStateUntilYear > year) continue;
			m.lifeState = MemberLifeState.Active;
			summary.draftReturns++;
			SimulatedArtist old = ArtistManager.Instance.GetArtist(p.lastArtistId);
			bool wanted = old != null && old.lifecycleStatus == ArtistLifecycleStatus.Active && Applies(old) &&
				(old.members.Count(x => x.isActive) < 4 || BandLife.Chance($"{m.personId}|{year}|rejoin", 0.5f));
			if (!wanted) { p.sinceYear = year; p.leftAs = DepartureKind.None; continue; }
			PersonPool.Take(m.personId);
			AlumniRecord stint = old.alumni?.LastOrDefault(r => r.personId == m.personId);
			JoinAct(m, old, stint?.role ?? m.primaryRole, stint?.wasLeadVocalist == true, stint?.wasWriter == true, year);
			old.alumni?.Remove(stint);
			old.careerEvents.Add($"{year}: {m.FullName} back from the service");
			AfterLineupChange(old, year, MoraleHoneymoon * 0.5f);
			Emit(new BandLifeEvent { year = year, artistId = old.artistId, stageName = old.stageName, personId = m.personId,
				personName = m.FullName, eventType = "draft-return", applied = true, playerOwned = old.isPlayerOwned });
		}
	}

	// ---- morale (§4.5) ------------------------------------------------------------------------------------

	private static void UpdateMorale(ActYear ctx) {
		SimulatedArtist a = ctx.artist;
		int flops = Mathf.Max(0, ctx.releasesNow - ctx.chartedNow);
		float m = a.morale * MoraleRelax
			+ MoraleTop40 * Mathf.Min(2, ctx.top40Now) + MoraleCharted * Mathf.Min(3, ctx.chartedNow)
			- MoraleFlop * Mathf.Min(4, flops)
			- (ctx.roadLoad > 0.65f ? MoraleRoad : 0f);
		a.morale = Mathf.Clamp(m, MoraleMin, MoraleMax);
	}

	// ---- romance (§4.9) -------------------------------------------------------------------------------------

	private static void Romance(ActYear ctx, BandLifeAnnualSummary summary) {
		SimulatedArtist a = ctx.artist;
		int year = ctx.year;
		List<Musician> people = ctx.present;
		for (int i = 0; i < people.Count; i++) for (int j = i + 1; j < people.Count; j++) {
			Musician x = people[i], y = people[j];
			string pk = $"{PairKey(x, y)}|{year}";
			bool couple = x.partner?.personId == y.personId && y.partner?.personId == x.personId &&
				x.partner.state is PartnerState.Married or PartnerState.Seeing;
			if (couple) {
				if (BandLife.Chance(pk + "|couple-end", CoupleBreakupRate)) {
					x.partner.state = PartnerState.Separated; y.partner.state = PartnerState.Separated;
					MemberRelation e = Edge(a, x, y);
					AddStrain(e, StrainCause.Romance, 0.50f, year);
					foreach (Musician z in people) if (z != x && z != y) {
						AddStrain(Edge(a, x, z), StrainCause.Romance, 0.08f, year);
						AddStrain(Edge(a, y, z), StrainCause.Romance, 0.08f, year);
					}
					summary.coupleBreakups++;
					EmitPair(ctx, x, y, "couple-ended", StrainCause.Romance, e.strain);
				} else {
					a.morale = Mathf.Min(MoraleMax, a.morale + 0.03f);
				}
				continue;
			}
			bool xFree = x.partner == null || x.partner.state is PartnerState.Divorced or PartnerState.Separated;
			bool yFree = y.partner == null || y.partner.state is PartnerState.Divorced or PartnerState.Separated;
			int together = Mathf.Max(1, year - Mathf.Max(x.joinedYear, y.joinedYear));
			if (xFree && yFree) {
				float mixed = x.isMale != y.isMale ? 1f : 0.25f;
				if (BandLife.Chance(pk + "|couple", InBandCoupleRate * Mathf.Sqrt(together) * mixed)) {
					x.partner = new MusicianPartner { name = y.FullName, personId = y.personId, isMale = y.isMale, sinceYear = year, state = PartnerState.Seeing };
					y.partner = new MusicianPartner { name = x.FullName, personId = x.personId, isMale = x.isMale, sinceYear = year, state = PartnerState.Seeing };
					summary.couples++;
					// Most acts of the era kept a same-sex relationship from the press: it stays rumour (§9 open item).
					EmitPair(ctx, x, y, x.isMale == y.isMale ? "couple-rumoured" : "couple", StrainCause.Romance, 0f);
				}
			}
			// An affair: one member with another member's partner. Created secret, carrying no strain until found.
			TryAffair(ctx, x, y, pk + "|xy", summary);
			TryAffair(ctx, y, x, pk + "|yx", summary);
		}
		// Discovery: road time and press attention bring it out.
		if (a.relations != null)
			foreach (MemberRelation e in a.relations.Where(r => r.secret).ToList()) {
				if (!BandLife.Chance($"{e.personA}|{e.personB}|{year}|discover", AffairDiscoveryRate * (0.5f + ctx.roadLoad + ctx.fame))) continue;
				DiscoverAffair(a, e, year, ctx, summary);
			}
	}

	private static void TryAffair(ActYear ctx, Musician a, Musician b, string key, BandLifeAnnualSummary summary) {
		if (b.partner == null || b.partner.state is not (PartnerState.Married or PartnerState.Seeing)) return;
		if (b.partner.personId == a.personId) return;
		MemberRelation existing = FindEdge(ctx.artist, a, b);
		if (existing?.secret == true) return;
		float hazard = AffairRate * (1f - a.loyalty) * a.ego * ctx.roadLoad * (0.3f + ctx.fame);
		if (!BandLife.Chance(key + "|affair", hazard)) return;
		MemberRelation e = Edge(ctx.artist, a, b);
		e.secret = true;
		e.secretAgainstPersonId = b.personId;
		e.lastEventYear = ctx.year;
		summary.affairs++;
		EmitPair(ctx, a, b, "affair", StrainCause.Romance, 0f);
		NotifyPlayer(ctx, a, "affair-secret", DepartureKind.None, StrainCause.Romance, b);
	}

	/// <summary>The affair comes out: the wronged member's partnership ends and Romance strain spikes on the edge.
	/// Also the Band Room's "tell him" verb.</summary>
	public static void DiscoverAffair(SimulatedArtist a, MemberRelation e, int year) => DiscoverAffair(a, e, year, null, null);

	private static void DiscoverAffair(SimulatedArtist a, MemberRelation e, int year, ActYear ctx, BandLifeAnnualSummary summary) {
		e.secret = false;
		Musician wronged = a.members.FirstOrDefault(m => m.personId == e.secretAgainstPersonId);
		Musician other = a.members.FirstOrDefault(m => m.personId == e.Other(e.secretAgainstPersonId));
		if (wronged?.partner != null) wronged.partner.state = wronged.partner.state == PartnerState.Married ? PartnerState.Divorced : PartnerState.Separated;
		AddStrain(e, StrainCause.Romance, 0.60f, year);
		if (summary != null) summary.discoveries++;
		var evt = new BandLifeEvent { year = year, artistId = a.artistId, stageName = a.stageName, personId = wronged?.personId,
			personName = wronged?.FullName, otherPersonId = other?.personId, eventType = "affair-discovered",
			cause = StrainCause.Romance, strain = e.strain, applied = true, playerOwned = a.isPlayerOwned,
			everCharted = a.charted > 0, constitution = ConstitutionOf(a) };
		Emit(evt);
		if (a.isPlayerOwned) OnPlayerSignal?.Invoke(evt);
	}

	// ---- strain (§4.4) -------------------------------------------------------------------------------------

	private static void AccrueStrain(ActYear ctx) {
		SimulatedArtist a = ctx.artist;
		List<Musician> people = ctx.present;
		float totalRecognition = people.Sum(m => m.personalRecognition);
		float Spot(Musician m) => totalRecognition > 0.005f ? m.personalRecognition / totalRecognition : 1f / people.Count;
		float solvent = (ctx.success + ctx.restingCohesion * SolventCohesionWeight) * SolventScale;
		float scale = ctx.constitution switch {
			LineupConstitution.LeaderAndSidemen => SidemanStrainScale,
			LineupConstitution.NameOwned => NameOwnedStrainScale,
			_ => 1f
		} * (ctx.honeymoon ? 0.5f : 1f);
		float projectFriction = ProjectFrictionWeight * Mathf.Sqrt(ctx.releasesNow);
		Musician leader = people.FirstOrDefault(m => m.isBandLeader) ?? people[0];

		var incoming = new float[7];
		for (int i = 0; i < people.Count; i++) for (int j = i + 1; j < people.Count; j++) {
			Musician x = people[i], y = people[j];
			// Leader and sidemen: only the leader's edges exist; sidemen are employees, not partners.
			if (ctx.constitution == LineupConstitution.LeaderAndSidemen && x != leader && y != leader) continue;
			Array.Clear(incoming, 0, incoming.Length);

			// Each cause is computed RAW (unweighted) and then weighted, so calibration can replay the level offline.
			// CreditAndMoney: this act's credit share gap (the per-stint ledger), felt by the member with less,
			// in proportion to how much they want it -- a contented sideman generates nothing.
			float shareX = CompositionCatalogService.GetStintCreditShare(x.personId, a);
			float shareY = CompositionCatalogService.GetStintCreditShare(y.personId, a);
			float resentY = Mathf.Max(0f, shareX - shareY) * y.ambition * y.ego * (0.3f + 0.7f * y.creativity);
			float resentX = Mathf.Max(0f, shareY - shareX) * x.ambition * x.ego * (0.3f + 0.7f * x.creativity);
			float creditRaw = Mathf.Max(resentX, resentY) * (0.25f + 0.75f * ctx.money);

			// Spotlight: has the public learned one name? Read on spotlight SHARE, never raw recognition (§2.4).
			float spotX = Spot(x), spotY = Spot(y);
			float spotlightRaw = Mathf.Abs(spotX - spotY) * Mathf.Max(x.ego, y.ego) * ctx.fame;

			// Direction: until identity lives on people (Phase 7), the distance between what each member brings to
			// the act's experimental appetite and its commercial pragmatism.
			float dExp = (0.5f * x.musicalVersatility + 0.4f * x.creativity + 0.1f * x.ego) - (0.5f * y.musicalVersatility + 0.4f * y.creativity + 0.1f * y.ego);
			float dPrag = (0.4f * x.reliability + 0.3f * (1f - x.creativity) + 0.3f * x.loyalty) - (0.4f * y.reliability + 0.3f * (1f - y.creativity) + 0.3f * y.loyalty);
			float directionRaw = Mathf.Sqrt(dExp * dExp + dPrag * dPrag) * (0.5f + Mathf.Max(x.creativity, y.creativity));

			// Reliability: sign fixed (§2.10) -- high temperament is even-tempered, so the DRAMA term is (1 - t).
			float reliabilityRaw = Mathf.Max(1f - x.reliability, 1f - y.reliability) * Mathf.Max(1f - x.temperament, 1f - y.temperament);
			float substanceRaw = Mathf.Max(x.substanceLoad, y.substanceLoad);

			// Outsider: a partner who sits in on the sessions.
			float outsiderRaw = x.partner?.sitsInOnSessions == true && x.partner.state == PartnerState.Married ||
				y.partner?.sitsInOnSessions == true && y.partner.state == PartnerState.Married ? 1f : 0f;

			MemberRelation existing = FindEdge(a, x, y);
			float rivalry = existing?.rivalry ?? 0f;
			incoming[(int)StrainCause.CreditAndMoney] = CreditMoneyWeight * creditRaw + rivalry * RivalryToCredit;
			incoming[(int)StrainCause.Spotlight] = SpotlightWeight * spotlightRaw;
			incoming[(int)StrainCause.Direction] = DirectionWeight * directionRaw + projectFriction + rivalry * RivalryToDirection;
			incoming[(int)StrainCause.Reliability] = ReliabilityWeight * reliabilityRaw + SubstanceReliabilityWeight * substanceRaw;
			incoming[(int)StrainCause.Outsider] = PartnerOutsiderWeight * outsiderRaw;
			float strainBefore = existing?.strain ?? 0f;

			float total = 0f;
			for (int c = 0; c < incoming.Length; c++) { incoming[c] *= scale * ctx.workFactor * StrainScale; total += incoming[c]; }
			float excess = Mathf.Max(0f, total - solvent);
			if (OnPairTerms != null)
				OnPairTerms(new PairTermRow {
					year = ctx.year, artistId = a.artistId, everCharted = ctx.everCharted, chartedNow = ctx.chartedNow, top40Now = ctx.top40Now,
					constitution = ctx.constitution, personA = x.personId, personB = y.personId, creditRaw = creditRaw, spotlightRaw = spotlightRaw,
					directionRaw = directionRaw, projectFriction = projectFriction, reliabilityRaw = reliabilityRaw, substanceRaw = substanceRaw,
					outsiderRaw = outsiderRaw, rivalry = rivalry, scale = scale, workFactor = ctx.workFactor, solvent = solvent,
					strainBefore = strainBefore, strainAfter = existing == null && excess <= 0f ? 0f : Mathf.Clamp(strainBefore * StrainRetention + excess, 0f, 1f), creditShareA = shareX, creditShareB = shareY,
					temperamentA = x.temperament, temperamentB = y.temperament, loyaltyA = x.loyalty, loyaltyB = y.loyalty,
					egoA = x.ego, egoB = y.ego, ambitionA = x.ambition, ambitionB = y.ambition, reliabilityA = x.reliability,
					reliabilityB = y.reliability, substanceA = x.substanceLoad, substanceB = y.substanceLoad, spotA = spotX, spotB = spotY,
					recognitionA = x.personalRecognition, recognitionB = y.personalRecognition,
					soloViableA = IsSoloViable(x, a, ctx.year), soloViableB = IsSoloViable(y, a, ctx.year),
					writerA = x.isPrimaryWriter, writerB = y.isPrimaryWriter, leadA = x.isLeadVocalist, leadB = y.isLeadVocalist
				});
			if (existing == null && excess <= 0f) continue;
			MemberRelation e = existing ?? Edge(a, x, y);
			e.strain *= StrainRetention;
			for (int c = 0; c < e.causeWeight.Length; c++) e.causeWeight[c] *= StrainRetention;
			if (excess > 0f) {
				e.strain = Mathf.Clamp(e.strain + excess, 0f, 1f);
				for (int c = 0; c < incoming.Length; c++) e.causeWeight[c] += excess * incoming[c] / total;
				e.lastEventYear = ctx.year;
			}
		}

		// Burnout is shared, not a grudge: it lands on the weakest relationship in the room.
		float burnoutRaw = ctx.roadLoad * (1f - people.Min(m => m.temperament)) * (ctx.honeymoon ? 0.5f : 1f);
		float burnout = BurnoutWeight * burnoutRaw;
		if (OnPairTerms != null && people.Count >= 2) {
			var byTemper = people.OrderBy(m => m.temperament).ThenBy(m => m.personId, StringComparer.Ordinal).ToList();
			OnPairTerms(new PairTermRow { year = ctx.year, artistId = a.artistId, everCharted = ctx.everCharted, chartedNow = ctx.chartedNow,
				top40Now = ctx.top40Now, constitution = ctx.constitution, burnoutRow = true, burnoutRaw = burnoutRaw, morale = a.morale,
				burnoutDefaultA = byTemper[0].personId, burnoutDefaultB = byTemper[1].personId, solvent = solvent });
		}
		if (burnout > 0.005f && people.Count >= 2) {
			MemberRelation weakest = a.relations?.Where(r => !r.secret && people.Any(p => p.personId == r.personA) &&
				people.Any(p => p.personId == r.personB)).OrderByDescending(r => r.strain).ThenBy(r => r.personA, StringComparer.Ordinal).FirstOrDefault();
			if (weakest == null) {
				var ordered = people.OrderBy(m => m.temperament).ThenBy(m => m.personId, StringComparer.Ordinal).ToList();
				weakest = Edge(a, ordered[0], ordered[1]);
			}
			AddStrain(weakest, StrainCause.Burnout, burnout, ctx.year);
		}
	}

	private static void AccrueRivalry(ActYear ctx) {
		SimulatedArtist a = ctx.artist;
		if (a.writingPartnerships == null) return;
		foreach (WritingPartnership p in a.writingPartnerships) {
			Musician x = ctx.present.FirstOrDefault(m => m.personId == p.personA);
			Musician y = ctx.present.FirstOrDefault(m => m.personId == p.personB);
			if (x == null || y == null) continue;
			int written = Mathf.Max(0, p.coCredits - p.coCreditsAtYearStart);
			MemberRelation e = FindEdge(a, x, y);
			if (e != null) e.rivalry *= StrainRetention;
			if (written == 0) continue;
			e ??= Edge(a, x, y);
			e.rivalry = Mathf.Clamp(e.rivalry + RivalryWeight * Mathf.Sqrt(written) * Mathf.Min(x.creativity, y.creativity) *
				Mathf.Min(x.ambition, y.ambition) * 4f, 0f, 1f);
		}
	}

	// ---- stages (§4.6) ---------------------------------------------------------------------------------------

	private static void EvaluateStages(ActYear ctx, BandLifeAnnualSummary summary, List<DepartureCandidate> candidates) {
		SimulatedArtist a = ctx.artist;
		if (a.relations == null) { foreach (Musician m in ctx.present) ResetStage(m); return; }
		// Each edge has one aggrieved side: the person who would complain about it. Their stage reads the
		// worst edge they are aggrieved on.
		var worst = new Dictionary<Musician, (MemberRelation Edge, Musician Other)>();
		foreach (MemberRelation e in a.relations) {
			if (e.secret) continue;
			Musician x = ctx.present.FirstOrDefault(m => m.personId == e.personA);
			Musician y = ctx.present.FirstOrDefault(m => m.personId == e.personB);
			if (x == null || y == null) continue;
			(Musician complainer, Musician other) = Complainer(e, x, y, a);
			if (!worst.TryGetValue(complainer, out var w) || e.strain > w.Edge.strain) worst[complainer] = (e, other);
		}
		foreach (Musician m in ctx.present) {
			if (!worst.TryGetValue(m, out var w)) { ResetStage(m); continue; }
			float s = w.Edge.strain - a.morale * MoraleStageShift;
			DepartureStage prior = m.departureStage;
			StrainCause cause = w.Edge.DominantCause;
			bool depart = s >= DepartureStrain || (prior == DepartureStage.Ultimatum && s >= BrewingStrain);
			if (depart) {
				candidates.Add(new DepartureCandidate { ctx = ctx, complainer = m, leaver = Leaver(w.Edge, m, w.Other, a, ctx.year),
					kind = DepartureKind.Acrimony, cause = cause, strain = w.Edge.strain, strainDriven = true });
				continue;
			}
			if (s >= UltimatumStrain || (prior == DepartureStage.Brewing && s >= BrewingStrain)) {
				if (prior != DepartureStage.Ultimatum) {
					SetStage(m, DepartureStage.Ultimatum, cause, w.Other, ctx.year);
					summary.ultimatums++;
					EmitStage(ctx, m, w.Other, "stage-ultimatum", cause, w.Edge.strain);
					if (!a.isPlayerOwned && ApplyAiPolicy(ctx, m, w.Other, w.Edge, cause)) summary.aiConcessions++;
				}
				continue;
			}
			if (s >= BrewingStrain) {
				if (prior == DepartureStage.Content) {
					summary.brewing++;
					EmitStage(ctx, m, w.Other, "stage-brewing", cause, w.Edge.strain);
				}
				SetStage(m, DepartureStage.Brewing, cause, w.Other, ctx.year);
				m.brewingYears++;
				continue;
			}
			ResetStage(m);
		}
	}

	private static void SetStage(Musician m, DepartureStage stage, StrainCause cause, Musician other, int year) {
		if (m.departureStage != stage) m.stageEnteredYear = year;
		m.departureStage = stage;
		m.stageCause = cause;
		m.stageAgainstPersonId = other?.personId;
	}

	private static void ResetStage(Musician m) {
		m.departureStage = DepartureStage.Content;
		m.brewingYears = 0;
		m.stageAgainstPersonId = null;
	}

	/// <summary>Who is aggrieved on an edge, by its dominant cause.</summary>
	private static (Musician Complainer, Musician Other) Complainer(MemberRelation e, Musician x, Musician y, SimulatedArtist a) {
		StrainCause cause = e.DominantCause;
		Musician c = cause switch {
			StrainCause.CreditAndMoney => CompositionCatalogService.GetStintCreditShare(x.personId, a) <=
				CompositionCatalogService.GetStintCreditShare(y.personId, a) ? x : y,
			StrainCause.Spotlight => x.ego * (1f - x.loyalty) >= y.ego * (1f - y.loyalty) ? x : y,
			StrainCause.Direction => x.ambition * x.ego >= y.ambition * y.ego ? x : y,
			// The reliable one complains about the unreliable one.
			StrainCause.Reliability => x.reliability - x.substanceLoad >= y.reliability - y.substanceLoad ? x : y,
			StrainCause.Burnout => x.temperament <= y.temperament ? x : y,
			StrainCause.Romance => x.loyalty >= y.loyalty ? x : y,
			StrainCause.Outsider => x.partner?.sitsInOnSessions == true ? y : x,
			_ => x
		};
		return (c, c == x ? y : x);
	}

	/// <summary>Who actually goes. Usually the aggrieved member; for Reliability and Outsider, the other one is
	/// let go; for Spotlight, the famous one walks if a solo career will carry them.</summary>
	private static Musician Leaver(MemberRelation e, Musician complainer, Musician other, SimulatedArtist a, int year) {
		switch (e.DominantCause) {
			case StrainCause.Reliability:
			case StrainCause.Outsider:
				return other;
			case StrainCause.Spotlight:
				return IsSoloViable(other, a, year) ? other : complainer;
			default:
				return complainer;
		}
	}

	/// <summary>
	/// The solo gate (§2.4, §4.6): the member wants it (WouldConsiderSoloCareer, now with real inputs), the public
	/// learned their name rather than the band's (spotlight share), and that name can carry a record (an absolute
	/// bar on their own recognition). The old gate compared member to act recognition and opened with time.
	/// </summary>
	public static bool IsSoloViable(Musician m, SimulatedArtist a, int year) {
		if (m == null || a == null) return false;
		var active = a.members.Where(x => x.isActive && x.observedGoneYear == 0).ToList();
		float total = active.Sum(x => x.personalRecognition);
		float spot = total > 0.005f ? m.personalRecognition / total : 0f;
		int yearsInGroup = Mathf.Max(0, year - m.joinedYear);
		int hits = a.top40Hits;
		return m.WouldConsiderSoloCareer(yearsInGroup, hits) && spot >= SoloSpotlightShare && m.personalRecognition >= SoloLaunchBar;
	}

	/// <summary>
	/// The AI's default answer to an ultimatum (§4.6): a charting act concedes credit to a writer or pays a
	/// non-writer some of the time, and a burned-out writer is kept for the records. Otherwise nothing changes
	/// and the stage runs. Writes only band-life state (edges, partnerships, life state), so it is the same in
	/// observe and in world scope.
	/// </summary>
	private static bool ApplyAiPolicy(ActYear ctx, Musician m, Musician other, MemberRelation e, StrainCause cause) {
		SimulatedArtist a = ctx.artist;
		string k = $"{a.artistId}|{m.personId}|{ctx.year}|ai-policy";
		switch (cause) {
			case StrainCause.CreditAndMoney when ctx.everCharted && m.isPrimaryWriter && other != null && other.isPrimaryWriter:
				if (!BandLife.Chance(k, 0.35f)) return false;
				CowritingService.ConcedePact(a, m, other, ctx.year);
				Relieve(e, 0.5f);
				ResetStage(m);
				EmitPerson(ctx, m, "ai-concede-pact", cause: cause, other: other);
				return true;
			case StrainCause.CreditAndMoney when ctx.everCharted:
				if (!BandLife.Chance(k, 0.25f)) return false;
				Relieve(e, 0.6f);
				ResetStage(m);
				EmitPerson(ctx, m, "ai-pay", cause: cause, other: other);
				return true;
			case StrainCause.Burnout when m.isPrimaryWriter || m.studioEfficiency > 0.6f:
				m.lifeState = MemberLifeState.StudioOnly;
				Relieve(e, 0.4f);
				ResetStage(m);
				EmitPerson(ctx, m, "studio-only", kind: DepartureKind.StudioOnly, cause: cause);
				return true;
		}
		return false;
	}

	// ======================================================================================================
	// DEPARTURES
	// ======================================================================================================

	private static void ResolveDepartures(List<DepartureCandidate> candidates, BandLifeAnnualSummary summary) {
		int year = lastAnnualYear;
		// Life-event departures are not the strain mechanism and are never deferred.
		var strain = candidates.Where(c => c.strainDriven).ToList();
		var life = candidates.Where(c => !c.strainDriven).ToList();
		// One leaver per act per year from the strain mechanism (the cooldown), the highest strain first.
		var byAct = strain.GroupBy(c => c.ctx.artist.artistId)
			.Select(g => g.OrderByDescending(c => c.strain).ThenBy(c => c.leaver.personId, StringComparer.Ordinal).First())
			.ToList();
		summary.cooldownDeferred += strain.Count - byAct.Count;
		// The breaker: a cap on APPLIED AI departures. When it trips, the lowest-strain departures wait a year.
		var ordered = byAct.OrderByDescending(c => c.strain)
			.ThenBy(c => BandLife.Unit($"breaker|{c.ctx.artist.artistId}|{c.leaver.personId}|{year}")).ToList();
		summary.breakerCap = BreakerAnnualCap;
		changedThisYear.Clear();
		foreach (DepartureCandidate c in life) Depart(c, summary);
		int aiStrainDepartures = 0;
		foreach (DepartureCandidate c in ordered) {
			SimulatedArtist a = c.ctx.artist;
			if (!c.ctx.present.Contains(c.leaver) || (c.leaver != c.complainer && !c.ctx.present.Contains(c.complainer))) continue;
			// The cooldown: one lineup change per act per year. A death or a draft this year already used it.
			if (changedThisYear.Contains(a.artistId)) { summary.cooldownDeferred++; continue; }
			if (!a.isPlayerOwned) {
				aiStrainDepartures++;
				// Observe scope counts what the breaker WOULD defer but lets the would-be departure stand, so the
				// raw rate it measures is the one the breaker gets sized against.
				if (aiStrainDepartures > BreakerAnnualCap) {
					summary.breakerDeferred++;
					if (c.ctx.apply) continue;
				}
			}
			Depart(c, summary);
		}
		appliedStrainDeparturesByYear[year] = Math.Min(aiStrainDepartures, BreakerAnnualCap);
	}

	private static readonly HashSet<string> changedThisYear = new(StringComparer.Ordinal);

	/// <summary>What kind of exit a strain-driven departure is, from who leaves and why.</summary>
	private static DepartureKind StrainKind(DepartureCandidate c) {
		SimulatedArtist a = c.ctx.artist;
		if (c.ctx.constitution == LineupConstitution.Duo) return DepartureKind.Dissolution;
		if ((c.cause is StrainCause.Reliability or StrainCause.Outsider) && c.leaver != c.complainer) return DepartureKind.Fired;
		if (IsSoloViable(c.leaver, a, c.ctx.year)) return DepartureKind.SoloCareer;
		return DepartureKind.Acrimony;
	}

	private static void Depart(DepartureCandidate c, BandLifeAnnualSummary summary) {
		ActYear ctx = c.ctx;
		SimulatedArtist a = ctx.artist;
		int year = ctx.year;
		// An earlier departure this pass may already have ended the act.
		if (a.lifecycleStatus != ArtistLifecycleStatus.Active || a.observedEndYear != 0) return;
		changedThisYear.Add(a.artistId);
		if (c.strainDriven) {
			c.kind = StrainKind(c);
			summary.strainDepartures++;
			summary.departuresByCause[(int)c.cause]++;
			if (ctx.everCharted) summary.strainDeparturesCharted++; else summary.strainDeparturesNeverCharted++;
		}
		summary.departuresByKind[(int)c.kind]++;
		Musician m = c.leaver;
		ctx.present.Remove(m);

		bool dissolve = c.kind == DepartureKind.Dissolution || WouldDissolve(ctx, m, c);
		var evt = new BandLifeEvent {
			year = year, artistId = a.artistId, stageName = a.stageName, personId = m.personId, personName = m.FullName,
			otherPersonId = c.complainer != null && c.complainer != m ? c.complainer.personId : null,
			eventType = dissolve && c.kind != DepartureKind.Dissolution ? "departure-dissolves" : "departure",
			kind = c.kind, cause = c.cause, strain = c.strain, everCharted = ctx.everCharted, chartedThisYear = ctx.chartedNow > 0,
			constitution = ctx.constitution, channel = c.channel, applied = ctx.apply, playerOwned = a.isPlayerOwned,
			age = m.GetAge(year)
		};
		Emit(evt);

		if (!ctx.apply) {
			// Observe: the counterfactual. The person is measured as gone; the economy's lineup is untouched.
			if (c.kind != DepartureKind.Service) m.observedGoneYear = year;
			if (dissolve) { a.observedEndYear = year; summary.dissolutions++; }
			ShockSurvivors(ctx, c.kind, apply: false);
			return;
		}

		if (a.isPlayerOwned) OnPlayerSignal?.Invoke(evt);
		// A drafted VOICE doesn't leave: the act goes dormant around him and the label lives off what's in the
		// can until he's home (RCA kept Elvis on the charts 1958-60). He stays on the roll as Drafted.
		if (c.kind == DepartureKind.Service && IsVoice(m, ctx)) {
			a.careerEvents.Add($"{year}: {m.FullName} drafted; the act waits for him");
			ShockSurvivors(ctx, c.kind, apply: true);
			return;
		}
		RemoveFromAct(a, m, c.kind, ReasonFor(c), year);
		if (c.kind == DepartureKind.SoloCareer) {
			SimulatedArtist solo = ArtistManager.Instance.CreateSoloSpinOut(m, a, year);
			if (solo != null) {
				summary.spinOuts++;
				formationDebt++;
				Emit(new BandLifeEvent { year = year, artistId = solo.artistId, stageName = solo.stageName, personId = m.personId,
					personName = m.FullName, otherPersonId = a.artistId, eventType = "solo-spinout", kind = DepartureKind.SoloCareer,
					applied = true, playerOwned = a.isPlayerOwned, everCharted = ctx.everCharted });
				if (!a.isPlayerOwned && TryAiLeavingMemberOption(a, solo, year)) summary.leavingMemberOptions++;
			}
		} else if (c.kind == DepartureKind.Service) {
			PersonPool.Add(new PooledPerson { person = m, lastArtistId = a.artistId, lastStageName = a.stageName,
				lastGenre = a.primaryGenre, homeRegion = a.homeRegion, sinceYear = year, leftAs = DepartureKind.Service });
		} else if (c.kind != DepartureKind.Death && PersonPool.HasCareerToContinue(m, year, CompositionCatalogService.HasAnyWriterCredit(m.personId))) {
			PersonPool.Add(new PooledPerson { person = m, lastArtistId = a.artistId, lastStageName = a.stageName,
				lastGenre = a.primaryGenre, homeRegion = a.homeRegion, sinceYear = year, leftAs = c.kind });
			summary.poolEntries++;
		}

		ShockSurvivors(ctx, c.kind, apply: true);
		if (dissolve) {
			EndAct(a, year, c.kind == DepartureKind.Death ? $"{m.FullName} died" : $"{m.FullName} left", summary);
			return;
		}
		AfterLineupChange(a, year, 0f);
		if (a.isPlayerOwned) return;   // the player chooses whether and whom to replace (Band Room)
		bool replace = ctx.everCharted || ctx.constitution is LineupConstitution.NameOwned or LineupConstitution.LeaderAndSidemen ||
			!BandLife.Chance($"{a.artistId}|{year}|{m.personId}|fold", 0.25f + 0.6f * c.strain);
		if (!replace) { EndAct(a, year, $"folded after {m.FullName} left", summary); return; }
		Musician hire = HireReplacement(a, m, year, out bool fromPool);
		summary.replacements++;
		if (fromPool) summary.replacementsFromPool++;
		Emit(new BandLifeEvent { year = year, artistId = a.artistId, stageName = a.stageName, personId = hire.personId,
			personName = hire.FullName, otherPersonId = m.personId, eventType = fromPool ? "replacement-pool" : "replacement-new",
			applied = true, everCharted = ctx.everCharted });
	}

	private static string ReasonFor(DepartureCandidate c) => c.kind switch {
		DepartureKind.Death => c.channel switch {
			DeathChannel.Travel => "Died on the road", DeathChannel.Illness => "Died after an illness",
			DeathChannel.Substance => "Found at home", _ => "Died"
		},
		DepartureKind.Service => "Drafted",
		DepartureKind.SoloCareer => "Went solo",
		DepartureKind.Fired => c.cause == StrainCause.Reliability ? "Fired (unreliable)" : "Fired",
		DepartureKind.LifeEvent => "Left the road",
		DepartureKind.Dissolution => "The act split",
		_ => c.cause switch {
			StrainCause.CreditAndMoney => "Quit over credit and money",
			StrainCause.Spotlight => "Quit over the spotlight",
			StrainCause.Direction => "Musical differences",
			StrainCause.Romance => "Quit after a falling-out",
			StrainCause.Burnout => "Burned out",
			_ => "Quit"
		}
	};

	/// <summary>A departure ends the act when it takes the act's voice with it (§4.7): a duo has no one left, the
	/// last voiced member is gone, or a Band loses the member who held the majority of its voice weight. A
	/// name-owned act's owner always keeps the name.</summary>
	private static bool WouldDissolve(ActYear ctx, Musician leaver, DepartureCandidate c) {
		if (ctx.constitution == LineupConstitution.NameOwned) return false;
		if (ctx.constitution is LineupConstitution.Duo or LineupConstitution.Solo) return c.kind != DepartureKind.Service;
		var remaining = ctx.present.Where(m => m != leaver).ToList();
		if (remaining.Count == 0) return c.kind != DepartureKind.Service;
		if (ctx.constitution == LineupConstitution.LeaderAndSidemen) return leaver.isBandLeader && c.kind != DepartureKind.Service;
		float total = VoiceWeight(leaver, ctx.artist) + remaining.Sum(m => VoiceWeight(m, ctx.artist));
		return c.kind != DepartureKind.Service && VoiceWeight(leaver, ctx.artist) / total > 0.5f;
	}

	private static bool IsVoice(Musician m, ActYear ctx) =>
		ctx.constitution == LineupConstitution.Solo ||
		(ctx.constitution == LineupConstitution.LeaderAndSidemen ? m.isBandLeader :
			m.isLeadVocalist && !ctx.present.Any(x => x != m && x.isLeadVocalist));

	public static float VoiceWeight(Musician m, SimulatedArtist a) =>
		1f + (m.isLeadVocalist ? 1.5f : 0f) + (m.isPrimaryWriter ? 1f : 0f) + CompositionCatalogService.GetStintCreditShare(m.personId, a);

	/// <summary>The survivors feel it (§4.8.2 for deaths): morale drops, and after a death the strain between
	/// those left is partly forgiven.</summary>
	private static void ShockSurvivors(ActYear ctx, DepartureKind kind, bool apply) {
		SimulatedArtist a = ctx.artist;
		float shock = kind switch {
			DepartureKind.Death => MoraleDeath, DepartureKind.Service => MoraleService,
			DepartureKind.Acrimony or DepartureKind.SoloCareer => MoraleAcrimony, DepartureKind.Fired => MoraleAcrimony * 0.5f,
			_ => 0f
		};
		a.morale = Mathf.Clamp(a.morale + shock, MoraleMin, MoraleMax);
		if (kind == DepartureKind.Death && a.relations != null)
			foreach (MemberRelation e in a.relations) Relieve(e, 0.6f);
	}

	private static void Relieve(MemberRelation e, float keep) {
		e.strain *= keep;
		for (int i = 0; i < e.causeWeight.Length; i++) e.causeWeight[i] *= keep;
	}

	// ======================================================================================================
	// LINEUP MECHANICS (apply mode only)
	// ======================================================================================================

	/// <summary>Takes a person out of an act: a frozen alumni record stays behind, the Musician moves on.</summary>
	public static void RemoveFromAct(SimulatedArtist a, Musician m, DepartureKind kind, string reason, int year) {
		a.alumni ??= new List<AlumniRecord>();
		a.alumni.Add(new AlumniRecord {
			personId = m.personId, name = m.FullName, role = m.primaryRole, joinedYear = m.joinedYear, leftYear = year,
			reason = reason, departureKind = kind, wasLeadVocalist = m.isLeadVocalist, wasWriter = m.isPrimaryWriter
		});
		a.members.Remove(m);
		m.isActive = false;
		m.reasonLeft = reason;
		ResetStage(m);
		a.relations?.RemoveAll(e => e.Involves(m.personId));
		if (a.relations?.Count == 0) a.relations = null;
		a.careerEvents.Add($"{year}: {m.FullName} left ({reason})");
		a.lastMemberChangeYear = year;
	}

	/// <summary>A person joins an act. Membership facts reset; person facts (traits, skills, recognition, life
	/// state, road years) carry over (§4.1).</summary>
	public static void JoinAct(Musician m, SimulatedArtist a, MusicianRole role, bool lead, bool writer, int year) {
		m.primaryRole = role;
		m.isLeadVocalist = lead;
		m.isPrimaryWriter = writer || (m.isPrimaryWriter && m.creativity > 0.6f);
		m.isBandLeader = false;
		m.joinedYear = year;
		m.isFoundingMember = false;
		m.isActive = true;
		m.reasonLeft = null;
		m.observedGoneYear = 0;
		ResetStage(m);
		a.members.Add(m);
		ArtistManager.Instance?.RegisterMusician(m);
		MemberAxesService.EnsureAxes(m, a, year);
		a.careerEvents.Add($"{year}: {m.FullName} joined ({role})");
		a.lastMemberChangeYear = year;
	}

	/// <summary>Stats, identity and disposition re-derive for the new lineup; the new lineup gets a honeymoon.</summary>
	public static void AfterLineupChange(SimulatedArtist a, int year, float moraleBump) {
		if (a.members.Count == 0) return;
		if (a.members.All(m => !m.isLeadVocalist) && !a.instrumentalPerformance) {
			Musician singer = a.members.Where(m => m.isActive).OrderByDescending(m => m.technicalSkill + m.stagePresence)
				.ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
			if (singer != null) singer.isLeadVocalist = true;
		}
		if (a.members.All(m => !m.isBandLeader)) {
			Musician leader = a.members.Where(m => m.isActive).OrderByDescending(m => m.isLeadVocalist).ThenByDescending(m => m.ego)
				.ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
			if (leader != null) leader.isBandLeader = true;
		}
		a.RecalculateStats();
		ArtistEvolutionService.RefreshDispositionIfLineupChanged(a);
		a.honeymoonUntilYear = year + 1;
		a.morale = Mathf.Clamp(a.morale + moraleBump, MoraleMin, MoraleMax);
	}

	/// <summary>
	/// Finds the departed member's replacement: a pooled person matched on role, region, scene and skill, or, if
	/// the pool has no fit, a new person generated from keyed draws (never population RNG).
	/// </summary>
	public static Musician HireReplacement(SimulatedArtist a, Musician departed, int year, out bool fromPool) {
		PooledPerson best = FindPoolReplacement(a, departed.primaryRole, departed.isLeadVocalist, year);
		Musician hire;
		if (best != null) {
			PersonPool.Take(best.person.personId);
			hire = best.person;
			fromPool = true;
		} else {
			hire = GenerateKeyedPerson(a, departed.primaryRole, departed.isMale, year, $"replace|{a.artistId}|{departed.personId}|{year}");
			fromPool = false;
		}
		JoinAct(hire, a, departed.primaryRole, departed.isLeadVocalist, departed.isPrimaryWriter && hire.creativity > 0.55f, year);
		AfterLineupChange(a, year, MoraleHoneymoon);
		return hire;
	}

	public static PooledPerson FindPoolReplacement(SimulatedArtist a, MusicianRole role, bool lead, int year) =>
		PersonPool.Ordered()
			.Where(p => p.person.lifeState == MemberLifeState.Active && p.person.GetAge(year) < 45 && p.lastArtistId != a.artistId)
			.Select(p => (p, score: RoleMatch(role, lead, p.person) + (p.homeRegion == a.homeRegion ? 1f : 0f) +
				SceneMatch(p.lastGenre, a.primaryGenre) + (p.person.GetAge(year) <= 35 ? 0.5f : 0f) + p.person.technicalSkill))
			.Where(x => RoleMatch(role, lead, x.p.person) >= 2f)
			.OrderByDescending(x => x.score).ThenBy(x => x.p.person.personId, StringComparer.Ordinal)
			.Select(x => x.p).FirstOrDefault();

	private static float RoleMatch(MusicianRole role, bool lead, Musician m) {
		if (lead) return MemberAxesService.IsSinger(m) ? 3f : 0f;
		if (m.primaryRole == role) return 3f;
		bool guitar(MusicianRole r) => r is MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar;
		bool keys(MusicianRole r) => r is MusicianRole.Piano or MusicianRole.Organ;
		bool horn(MusicianRole r) => r is MusicianRole.Saxophone or MusicianRole.Trumpet;
		bool vox(MusicianRole r) => r is MusicianRole.LeadVocals or MusicianRole.BackingVocals;
		if (guitar(role) && guitar(m.primaryRole) || keys(role) && keys(m.primaryRole) || horn(role) && horn(m.primaryRole) ||
			vox(role) && vox(m.primaryRole)) return 2f;
		return m.primaryRole == MusicianRole.MultiInstrumentalist ? 2f : 0f;
	}

	private static float SceneMatch(Genre from, Genre to) {
		if (from == to) return 1.5f;
		if (FamilyOf(from) == FamilyOf(to)) return 1f;
		return GenreMarketMomentumService.GetAdjacency(from, to) > 0.12f ? 0.75f : 0f;
	}

	/// <summary>A new person, every value a keyed draw on the world seed. Means and spreads match generation.</summary>
	public static Musician GenerateKeyedPerson(SimulatedArtist a, MusicianRole role, bool isMale, int year, string key) {
		string id = ArtistManager.Instance.NextMusicianId();
		(string first, string last) = BandLifeNames.Person(isMale, key);
		var m = new Musician(id, first, last, isMale, year - (19 + (int)(BandLife.Unit(key + "|age") * 10f)));
		float Stat(string k, float mean, float sd) => Mathf.Clamp(mean + sd * BandLife.Normal(key + "|" + k), 0f, 1f);
		m.technicalSkill = Stat("tech", 0.48f, 0.18f);
		m.creativity = Stat("cre", 0.40f, 0.22f);
		m.musicalVersatility = Stat("ver", 0.45f, 0.20f);
		m.stagePresence = Stat("stage", 0.42f, 0.22f);
		m.studioEfficiency = Stat("studio", 0.50f, 0.20f);
		m.ego = Stat("ego", 0.38f, 0.20f);
		m.ambition = Stat("amb", 0.50f, 0.20f);
		m.reliability = Stat("rel", 0.66f, 0.18f);
		m.loyalty = Stat("loy", 0.58f, 0.20f);
		m.temperament = Stat("temp", 0.56f, 0.20f);
		m.primaryRole = role;
		return m;
	}

	/// <summary>Ends an act through the band-life door: dissolution after a departure. The lifecycle sweep removes
	/// it from rosters; the surviving members go to the pool if they have careers to continue.</summary>
	private static void EndAct(SimulatedArtist a, int year, string why, BandLifeAnnualSummary summary) {
		summary.dissolutions++;
		// The other half of the servo seam: an act band life ends is a vacancy the formation servo refills, so the
		// population is conserved (§7.4). World scope only -- a roster-scope split must leave the AI world untouched.
		if (BandLife.WorldChurn) formationCredit++;
		bool group = ArtistManager.IsGroupAct(a);
		foreach (Musician m in a.members.Where(x => x.isActive).ToList()) {
			bool career = PersonPool.HasCareerToContinue(m, year, CompositionCatalogService.HasAnyWriterCredit(m.personId));
			RemoveFromAct(a, m, DepartureKind.Dissolution, "The act split", year);
			if (career) {
				PersonPool.Add(new PooledPerson { person = m, lastArtistId = a.artistId, lastStageName = a.stageName,
					lastGenre = a.primaryGenre, homeRegion = a.homeRegion, sinceYear = year, leftAs = DepartureKind.Dissolution });
				summary.poolEntries++;
			}
		}
		ArtistManager.Instance.EndActForBandLife(a, year, why, group);
		var evt = new BandLifeEvent { year = year, artistId = a.artistId, stageName = a.stageName, eventType = "dissolution",
			kind = DepartureKind.Dissolution, applied = true, playerOwned = a.isPlayerOwned, everCharted = a.charted > 0, detail = why };
		Emit(evt);
		if (a.isPlayerOwned) OnPlayerSignal?.Invoke(evt);
	}

	/// <summary>
	/// A quiet dissolution through the population lifecycle (ApplyTerminalExit). Measured in every scope -- it is
	/// the "failure looks like quiet dissolution" half of §7 measure 1 -- and, where lineups apply, the people
	/// are released to the pool instead of deleted.
	/// </summary>
	public static void OnLifecycleTerminalExit(SimulatedArtist a, int year) {
		if (!BandLife.AnnualPassActive || a == null) return;
		Emit(new BandLifeEvent { year = year, artistId = a.artistId, stageName = a.stageName, eventType = "quiet-dissolution",
			everCharted = a.charted > 0, applied = Applies(a), constitution = ConstitutionOf(a), playerOwned = a.isPlayerOwned });
		quietThisYear[a.charted > 0 ? 1 : 0]++;
		if (!Applies(a)) return;
		foreach (Musician m in a.members.ToList()) {
			if (m.lifeState is MemberLifeState.Deceased) continue;
			if (!PersonPool.HasCareerToContinue(m, year, CompositionCatalogService.HasAnyWriterCredit(m.personId))) continue;
			RemoveFromAct(a, m, DepartureKind.Dissolution, "The act split", year);
			PersonPool.Add(new PooledPerson { person = m, lastArtistId = a.artistId, lastStageName = a.stageName,
				lastGenre = a.primaryGenre, homeRegion = a.homeRegion, sinceYear = year, leftAs = DepartureKind.Dissolution });
		}
	}

	private static readonly int[] quietThisYear = new int[2];

	/// <summary>
	/// The leaving-member clause, AI side (§4.7): the departing member's old label exercises its option when
	/// the member's own name clears the solo bar and the label can afford the advance. Uses the ordinary AI
	/// signing sequence, so the economy books it like any other signing.
	/// </summary>
	private static bool TryAiLeavingMemberOption(SimulatedArtist from, SimulatedArtist solo, int year) {
		AILabel label = ChartManager.Instance?.GetAllLabels()?.FirstOrDefault(l => l.labelId == from.labelId);
		if (label == null || !label.IsActive || label.isPlayerOwned) return false;
		Musician person = solo.members.FirstOrDefault();
		if (!from.contractLeavingMemberOption || person == null || person.personalRecognition < SoloLaunchBar) return false;
		if (!label.CanAffordToSign(label.CalculateManagerAdjustedAdvance(solo))) return false;
		float advance = label.SignArtist(solo, year);
		CompetitorManager.Instance?.RecordExpense(label, advance);
		ArtistManager.Instance.SignArtist(solo, label.labelId, year);
		solo.careerEvents.Add($"{year}: {label.labelName} exercised its leaving-member option");
		return true;
	}

	// ======================================================================================================
	// PLAYER DECISIONS (the Band Room) -- the same departure and hiring code the annual pass uses
	// ======================================================================================================

	/// <summary>The player lets a member go, or fires one. Applied at once; the usual aftermath follows (pool,
	/// solo spin-out, dissolution if the act's voice walks out, survivors' morale) and a Departure visit asks
	/// who, if anyone, replaces them.</summary>
	public static void PlayerDepart(SimulatedArtist a, Musician m, DepartureKind kind, StrainCause cause, int year) {
		if (a == null || m == null || !a.members.Contains(m)) return;
		ActYear ctx = BuildContext(a, year);
		ctx.apply = true;
		if (!ctx.present.Contains(m)) ctx.present.Add(m);
		if (kind == DepartureKind.Acrimony && IsSoloViable(m, a, year)) kind = DepartureKind.SoloCareer;
		var c = new DepartureCandidate { ctx = ctx, leaver = m, complainer = m, kind = kind, cause = cause,
			strain = WorstEdgeOf(a, m)?.strain ?? 0f };
		Depart(c, new BandLifeAnnualSummary { year = year });
	}

	/// <summary>The player hires a replacement: a pooled person they chose, or a newcomer drawn from keyed values.</summary>
	public static Musician PlayerHire(SimulatedArtist a, MusicianRole role, bool lead, bool writer, string poolPersonId,
		Musician departed, int year) {
		if (a == null || a.lifecycleStatus != ArtistLifecycleStatus.Active) return null;
		Musician hire;
		bool fromPool = poolPersonId != null;
		if (fromPool) {
			PooledPerson p = PersonPool.Take(poolPersonId);
			if (p == null) return null;
			hire = p.person;
		} else {
			bool male = departed?.isMale ?? BandLife.Unit($"player-hire|{a.artistId}|{year}|sex") < 0.72f;
			hire = GenerateKeyedPerson(a, role, male, year, $"player-hire|{a.artistId}|{departed?.personId}|{year}|{a.members.Count}");
		}
		JoinAct(hire, a, role, lead, writer && hire.creativity > 0.55f, year);
		AfterLineupChange(a, year, MoraleHoneymoon);
		Emit(new BandLifeEvent { year = year, artistId = a.artistId, stageName = a.stageName, personId = hire.personId,
			personName = hire.FullName, otherPersonId = departed?.personId, eventType = fromPool ? "replacement-pool" : "replacement-new",
			applied = true, playerOwned = a.isPlayerOwned, everCharted = a.charted > 0 });
		return hire;
	}

	// ======================================================================================================
	// FORMATION SERVO (§2.19)
	// ======================================================================================================

	/// <summary>
	/// THE SERVO SEAM. People-built acts -- solo spin-outs created at the year boundary -- fill formation
	/// vacancies; they never add acts on top. <see cref="ArtistManager"/>.MaterializeRuntimeFormation calls this
	/// with the week's fresh-formation count and forms that many fewer fresh acts. Zero whenever lineup churn is
	/// off, so the formation loop is untouched.
	/// </summary>
	public static int ConsumeFormationDebt(int freshFormations) {
		if (formationDebt <= 0 || freshFormations <= 0) return 0;
		int paid = Math.Min(formationDebt, freshFormations);
		formationDebt -= paid;
		return paid;
	}

	/// <summary>
	/// The servo's refill for acts band life ended (§7.4). Without it the lifecycle's own exits are refilled by the
	/// calibrated formation rate but band-life dissolutions are not, and the active population ran 2.5-3.3% short by
	/// 1968. The year's dissolutions land at the boundary; the refill is spread evenly over the weeks left in the
	/// year, so the replacements form on the calendar like any other act instead of as one January cohort. These
	/// are extra fresh formations on top of the servo's quota (they don't count toward its annual ceiling): they
	/// replace acts, they don't stand in for the servo's own demand-driven ones.
	/// </summary>
	public static int ReleaseFormationCredit(int weeksLeftInYear) {
		if (formationCredit <= 0) return 0;
		int release = (formationCredit + Math.Max(1, weeksLeftInYear) - 1) / Math.Max(1, weeksLeftInYear);
		formationCredit -= release;
		return release;
	}

	/// <summary>
	/// Recombination (§4.7): when the servo fills a vacancy with a group and the pool holds people who fit it --
	/// region, a scene adjacent to the vacancy's genre, age -- some of the generated lineup is swapped for them.
	/// The fresh act was generated first, so the population stream drew exactly what it always draws; the
	/// discarded generated members simply never enter the world. World scope only.
	/// </summary>
	public static bool TryRecombine(SimulatedArtist fresh, int year) {
		if (!BandLife.WorldChurn || fresh == null || !ArtistManager.IsGroupAct(fresh) || fresh.members.Count < 2) return false;
		if (!BandLife.Chance($"recombine|{fresh.artistId}", RecombinationShare)) return false;
		var fits = PersonPool.Ordered().Where(p => p.person.lifeState == MemberLifeState.Active && p.person.GetAge(year) < 40 &&
			SceneMatch(p.lastGenre, fresh.primaryGenre) >= 0.75f &&
			(p.homeRegion == fresh.homeRegion || BandLife.Unit($"recombine-region|{p.person.personId}|{fresh.artistId}") < 0.25f)).ToList();
		if (fits.Count < 2) return false;
		int swapped = 0;
		foreach (Musician slot in fresh.members.ToList()) {
			PooledPerson fit = fits.FirstOrDefault(p => RoleMatch(slot.primaryRole, slot.isLeadVocalist, p.person) >= 2f);
			if (fit == null) continue;
			fits.Remove(fit);
			PersonPool.Take(fit.person.personId);
			int index = fresh.members.IndexOf(slot);
			Musician person = fit.person;
			ArtistManager.Instance.UnregisterMusician(slot.personId);
			person.primaryRole = slot.primaryRole; person.isLeadVocalist = slot.isLeadVocalist; person.isBandLeader = slot.isBandLeader;
			person.isPrimaryWriter = slot.isPrimaryWriter || person.isPrimaryWriter; person.joinedYear = year;
			person.isFoundingMember = true; person.isActive = true; person.reasonLeft = null; person.observedGoneYear = 0;
			ResetStage(person);
			fresh.members[index] = person;
			ArtistManager.Instance.RegisterMusician(person);
			if (++swapped >= Mathf.Max(2, fresh.members.Count / 2)) break;
		}
		if (swapped < 2) return swapped > 0;
		fresh.RecalculateStats();
		ArtistEvolutionService.RefreshDispositionIfLineupChanged(fresh);
		fresh.careerEvents.Add($"{year}: Formed by former members of other acts");
		recombinationsThisYear++;
		return true;
	}

	private static int recombinationsThisYear;

	// ======================================================================================================
	// PLAYER SEAM (Band Room)
	// ======================================================================================================

	/// <summary>Raised for things that happen to the PLAYER's acts: stage changes, life events, departures.
	/// PlayerDesk turns them into Band Room visits. Never raised for AI acts.</summary>
	public static event Action<BandLifeEvent> OnPlayerSignal;

	private static void NotifyPlayer(ActYear ctx, Musician m, string type, DepartureKind kind, StrainCause cause, Musician other = null) {
		if (!ctx.artist.isPlayerOwned) return;
		OnPlayerSignal?.Invoke(new BandLifeEvent { year = ctx.year, artistId = ctx.artist.artistId, stageName = ctx.artist.stageName,
			personId = m.personId, personName = m.FullName, otherPersonId = other?.personId, eventType = type, kind = kind,
			cause = cause, applied = ctx.apply, playerOwned = true, everCharted = ctx.everCharted, constitution = ctx.constitution });
	}

	// ======================================================================================================
	// EDGES
	// ======================================================================================================

	private static string PairKey(Musician x, Musician y) =>
		string.CompareOrdinal(x.personId, y.personId) <= 0 ? $"{x.personId}|{y.personId}" : $"{y.personId}|{x.personId}";

	public static MemberRelation FindEdge(SimulatedArtist a, Musician x, Musician y) {
		if (a.relations == null) return null;
		(string p, string q) = string.CompareOrdinal(x.personId, y.personId) <= 0 ? (x.personId, y.personId) : (y.personId, x.personId);
		return a.relations.FirstOrDefault(e => e.personA == p && e.personB == q);
	}

	public static MemberRelation Edge(SimulatedArtist a, Musician x, Musician y) {
		MemberRelation e = FindEdge(a, x, y);
		if (e != null) return e;
		(string p, string q) = string.CompareOrdinal(x.personId, y.personId) <= 0 ? (x.personId, y.personId) : (y.personId, x.personId);
		e = new MemberRelation { personA = p, personB = q };
		a.relations ??= new List<MemberRelation>();
		a.relations.Add(e);
		return e;
	}

	public static void AddStrain(MemberRelation e, StrainCause cause, float amount, int year) {
		if (amount <= 0f) return;
		e.strain = Mathf.Clamp(e.strain + amount, 0f, 1f);
		e.causeWeight[(int)cause] += amount;
		e.lastEventYear = year;
	}

	/// <summary>The strain a member carries on the worst edge they are on (any side). For cards and prose.</summary>
	public static MemberRelation WorstEdgeOf(SimulatedArtist a, Musician m) =>
		a?.relations?.Where(e => !e.secret && e.Involves(m.personId)).OrderByDescending(e => e.strain).FirstOrDefault();

	// ======================================================================================================
	// TELEMETRY
	// ======================================================================================================

	private static void Emit(BandLifeEvent evt) => OnEvent?.Invoke(evt);

	private static void EmitPerson(ActYear ctx, Musician m, string type, DepartureKind kind = DepartureKind.None,
		StrainCause cause = StrainCause.Direction, Musician other = null, string detail = null) =>
		Emit(new BandLifeEvent { year = ctx.year, artistId = ctx.artist.artistId, stageName = ctx.artist.stageName,
			personId = m.personId, personName = m.FullName, otherPersonId = other?.personId, eventType = type, kind = kind,
			cause = cause, everCharted = ctx.everCharted, chartedThisYear = ctx.chartedNow > 0, constitution = ctx.constitution,
			applied = ctx.apply, playerOwned = ctx.artist.isPlayerOwned, age = m.GetAge(ctx.year), detail = detail });

	private static void EmitPair(ActYear ctx, Musician x, Musician y, string type, StrainCause cause, float strain) =>
		Emit(new BandLifeEvent { year = ctx.year, artistId = ctx.artist.artistId, stageName = ctx.artist.stageName,
			personId = x.personId, personName = x.FullName, otherPersonId = y.personId, eventType = type, cause = cause,
			strain = strain, everCharted = ctx.everCharted, constitution = ctx.constitution, applied = ctx.apply,
			playerOwned = ctx.artist.isPlayerOwned, age = x.GetAge(ctx.year) });

	private static void EmitStage(ActYear ctx, Musician m, Musician other, string type, StrainCause cause, float strain) {
		var evt = new BandLifeEvent { year = ctx.year, artistId = ctx.artist.artistId, stageName = ctx.artist.stageName,
			personId = m.personId, personName = m.FullName, otherPersonId = other?.personId, eventType = type, cause = cause,
			strain = strain, everCharted = ctx.everCharted, chartedThisYear = ctx.chartedNow > 0, constitution = ctx.constitution,
			applied = ctx.apply, playerOwned = ctx.artist.isPlayerOwned, age = m.GetAge(ctx.year) };
		Emit(evt);
		if (ctx.artist.isPlayerOwned) OnPlayerSignal?.Invoke(evt);
	}

	private static void Summarize(List<ActYear> contexts, BandLifeAnnualSummary s) {
		var groups = contexts.Where(c => c.constitution != LineupConstitution.Solo).ToList();
		s.actsInScope = contexts.Count;
		s.groupActs = groups.Count;
		s.chartedGroupActs = groups.Count(c => c.everCharted);
		var edges = contexts.SelectMany(c => c.artist.relations ?? Enumerable.Empty<MemberRelation>()).ToList();
		s.edges = edges.Count(e => !e.secret);
		s.secretEdges = edges.Count(e => e.secret);
		s.meanStrain = edges.Count == 0 ? 0f : edges.Average(e => e.strain);
		s.maxStrain = edges.Count == 0 ? 0f : edges.Max(e => e.strain);
		s.meanMorale = contexts.Count == 0 ? 0f : contexts.Average(c => c.artist.morale);
		s.poolSize = PersonPool.Count;
		s.quietDissolutionsNeverCharted = quietThisYear[0];
		s.quietDissolutionsCharted = quietThisYear[1];
		quietThisYear[0] = quietThisYear[1] = 0;
		s.recombinations = recombinationsThisYear;
		recombinationsThisYear = 0;
	}

	private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

	// ======================================================================================================
	// SAVE / LOAD
	// ======================================================================================================

	public static void CaptureWorld(WorldSaveData w) {
		w.BandLife = new BandLifeSaveData {
			Pool = PersonPool.Capture(),
			WorldSeed = SimulationSeedBootstrap.RequestedSeed.HasValue ? null : BandLife.WorldSeed,
			LastAnnualYear = lastAnnualYear,
			FormationDebt = formationDebt,
			FormationCredit = formationCredit,
			DeparturesByYear = new Dictionary<int, int>(appliedStrainDeparturesByYear),
		};
	}

	public static void RehydrateWorld(WorldSaveData w, int year) {
		BandLifeSaveData s = w.BandLife;
		if (s == null) {
			// A v3 save: an empty pool, no edges, and this year's evidence counted from now, not from 1960.
			PersonPool.Reset();
			lastAnnualYear = year - 1;
			formationDebt = 0;
			formationCredit = 0;
			appliedStrainDeparturesByYear.Clear();
			if (ArtistManager.Instance != null) BaselineSnapshots(ArtistManager.Instance.GetAllArtists());
			return;
		}
		BandLife.RestoreWorldSeed(s.WorldSeed);
		PersonPool.Rehydrate(s.Pool);
		foreach (PooledPerson p in PersonPool.All) ArtistManager.Instance?.RegisterMusician(p.person);
		lastAnnualYear = s.LastAnnualYear;
		formationDebt = s.FormationDebt;
		formationCredit = s.FormationCredit;
		appliedStrainDeparturesByYear.Clear();
		foreach (var kv in s.DeparturesByYear ?? new Dictionary<int, int>()) appliedStrainDeparturesByYear[kv.Key] = kv.Value;
	}

	internal static void ResetForProbe() {
		lastAnnualYear = -1; formationDebt = 0; formationCredit = 0; appliedStrainDeparturesByYear.Clear(); recombinationsThisYear = 0;
		quietThisYear[0] = quietThisYear[1] = 0;
		PersonPool.Reset();
	}
}

/// <summary>The historical induction curve (Data/DraftInductions.csv): inductions over the eligible pool, per year.</summary>
public static class DraftCurve {
	private static Dictionary<int, float> hazard;

	public static float Hazard(int year) {
		hazard ??= Load();
		if (hazard.TryGetValue(year, out float h)) return h;
		return year < 1958 ? hazard[1958] : hazard[1970];
	}

	private static Dictionary<int, float> Load() {
		var table = new Dictionary<int, float>();
		try {
			using var file = Godot.FileAccess.Open("res://Data/DraftInductions.csv", Godot.FileAccess.ModeFlags.Read);
			if (file != null) {
				file.GetLine();
				while (!file.EofReached()) {
					string line = file.GetLine();
					if (string.IsNullOrWhiteSpace(line)) continue;
					string[] cells = line.Split(',');
					if (cells.Length < 3) continue;
					int year = int.Parse(cells[0], CultureInfo.InvariantCulture);
					float inductions = float.Parse(cells[1], CultureInfo.InvariantCulture);
					float pool = float.Parse(cells[2], CultureInfo.InvariantCulture);
					if (pool > 0f) table[year] = inductions / pool;
				}
			}
		} catch (Exception ex) {
			GD.PrintErr($"DraftCurve: could not read Data/DraftInductions.csv ({ex.Message}); using the embedded copy.");
			table.Clear();
		}
		if (table.Count == 0) {
			// The same series, embedded so a stripped export still has the curve.
			int[] years = { 1958, 1959, 1960, 1961, 1962, 1963, 1964, 1965, 1966, 1967, 1968, 1969, 1970 };
			float[] inductions = { 142246, 96143, 86602, 118586, 82060, 119265, 112386, 230991, 382010, 228263, 296406, 283586, 162746 };
			float[] pools = { 8.7e6f, 8.85e6f, 9.0e6f, 9.2e6f, 9.5e6f, 9.9e6f, 10.3e6f, 10.8e6f, 11.3e6f, 11.8e6f, 12.3e6f, 12.7e6f, 13.0e6f };
			for (int i = 0; i < years.Length; i++) table[years[i]] = inductions[i] / pools[i];
		}
		return table;
	}
}
