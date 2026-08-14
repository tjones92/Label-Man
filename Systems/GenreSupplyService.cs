using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase-2 AI supply routing. This service is deliberately stateless: callers own
/// RNG and recent-release counts so the disabled stream cannot be disturbed.
/// </summary>
public static class GenreSupplyService {
	public static event Action<TraditionalPopFallbackTelemetry> OnTraditionalPopFallback;
	public sealed class TraditionalPopFallbackTelemetry {
		public string Source;
		public Genre RequestedGenre;
	}

	public static void ReportTraditionalPopFallback(string source, Genre requestedGenre) {
		if (GenreMarketV2.Enabled) OnTraditionalPopFallback?.Invoke(new TraditionalPopFallbackTelemetry {
			Source = source, RequestedGenre = requestedGenre
		});
	}

	public readonly struct GenreSelection {
		public readonly Genre Genre;
		public readonly bool RetainedIdentity;
		/// <summary>True only when the requested annual-floor pool supplied the final weighted choice.</summary>
		public readonly bool UsedCandidateOverride;
		public GenreSelection(Genre genre, bool retainedIdentity, bool usedCandidateOverride = false) {
			Genre = genre;
			RetainedIdentity = retainedIdentity;
			UsedCandidateOverride = usedCandidateOverride;
		}
	}
	/// <summary>
	/// Which question a supply weight is answering. FormationAffinity is a statement about
	/// how many ACTS a genre attracts, so it belongs to the formation question and to the
	/// structural normalizers and scouting scores that rank acts by their own identity. It
	/// is documented as compounding into project transition as well, which is the seam
	/// <see cref="SplitFormationAffinity"/> closes.
	/// </summary>
	public enum SupplyWeightContext { Formation, ProjectTransition }

	/// <summary>
	/// Removes FormationAffinity from project-genre transition, leaving it on formation.
	/// Off by default: this is a change to a calibrated file and wants its own A/B, not a
	/// silent default flip. Country's 2.2 is backed by a measured elasticity and is NOT the
	/// thing being changed here -- only whether it also decides where an existing act's next
	/// record lands, where it currently makes Country the top off-identity destination for
	/// folk acts by 113 to 33 over Folk Rock.
	/// </summary>
	public static bool SplitFormationAffinity { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		foreach (string argument in arguments ?? Array.Empty<string>())
			if (argument == "--split-formation-affinity") SplitFormationAffinity = true;
	}

	internal static void SetSplitFormationAffinityForProbe(bool split) => SplitFormationAffinity = split;

	public static bool IsAvailableForNewSupply(Genre genre, float year) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		if (IsBritishBridgeGenre(canonical)) return IsBritishSupplyBridgeActive(canonical, year);
		GenreLifecycleState lifecycle = GenreCatalog.Get(canonical).GetLifecycle(year);
		return lifecycle is GenreLifecycleState.Emerging or GenreLifecycleState.Established or GenreLifecycleState.Declining;
	}

	/// <summary>Existing signed artists remain release candidates even after their original genre stops accepting new supply.</summary>
	public static bool IsEligibleExistingArtistForRelease(SimulatedArtist artist) => artist != null && artist.isActive;

	/// <summary>
	/// Terminal career states are never valid live-release candidates.  This is
	/// deliberately separate from the legacy predicate above: the disabled
	/// replay keeps its historical selection and RNG boundary byte-for-byte.
	/// </summary>
	public static bool IsTerminalCareerState(CareerState state) => state is
		CareerState.Dropped or CareerState.Disbanded or CareerState.Retired;

	public static bool IsEligibleExistingArtistForEnabledRelease(SimulatedArtist artist) =>
		IsEligibleExistingArtistForRelease(artist) && !IsTerminalCareerState(artist.careerState);

	/// <summary>
	/// Existing canonical identities may retain their genre even when the catalog
	/// is not yet accepting new supply. Their authored pre-emergence baseline is
	/// the seed-scene constraint; rerouting the identity would erase that
	/// distinction and change its format economics. Imported British identities
	/// remain gated by the explicit bridge because they have no domestic seed pool.
	/// </summary>
	public static bool CanRetainExistingProjectGenre(Genre genre, float year) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		if (IsBritishBridgeGenre(canonical)) return IsBritishSupplyBridgeActive(canonical, year);
		return true;
	}

	public static bool IsBritishSupplyBridgeActive(Genre genre, float year) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		return IsBritishBridgeGenre(canonical) && year >= GenreCatalog.Get(canonical).EmergenceYear;
	}

	private static bool IsBritishBridgeGenre(Genre genre) => genre is Genre.BritishPop or Genre.BritishBeat or Genre.BritishBlues;

	public static IReadOnlyList<Genre> GetAvailableGenres(float year) => GenreCatalog.All
		.Where(profile => IsAvailableForNewSupply(profile.Genre, year))
		.OrderBy(profile => profile.Id, StringComparer.Ordinal)
		.Select(profile => profile.Genre)
		.ToArray();

	public static Genre ChooseGenre(AILabel label, SimulatedArtist artist, MarketRegion region, float year,
		IReadOnlyDictionary<Genre, int> recentSupply, float roll, IReadOnlyList<Genre> candidateOverride = null,
		IReadOnlyDictionary<Genre, int> globalRecentSupply = null, bool applyPsychedelicTransitionCompatibility = false) =>
		ChooseGenreWithSelection(label, artist, region, year, recentSupply, roll, candidateOverride, globalRecentSupply,
			applyPsychedelicTransitionCompatibility).Genre;

	/// <summary>Returns the existing deterministic selection plus whether identity retention selected the project.</summary>
	public static GenreSelection ChooseGenreWithSelection(AILabel label, SimulatedArtist artist, MarketRegion region, float year,
		IReadOnlyDictionary<Genre, int> recentSupply, float roll, IReadOnlyList<Genre> candidateOverride = null,
		IReadOnlyDictionary<Genre, int> globalRecentSupply = null, bool applyPsychedelicTransitionCompatibility = false) {
		IReadOnlyList<Genre> candidates = candidateOverride ?? GetAvailableGenres(year);
		if (candidates.Count == 0) {
			Genre fallback = GenreCatalog.MapLegacy(artist?.primaryGenre ?? Genre.TraditionalPop, (int)year);
			if (fallback == Genre.TraditionalPop) ReportTraditionalPopFallback("GenreSupplyService.EmptyCandidateSet", artist?.primaryGenre ?? Genre.TraditionalPop);
			return new GenreSelection(fallback, false);
		}
		Genre identity = GenreCatalog.MapLegacy(artist?.primaryGenre ?? Genre.TraditionalPop, (int)year);
		if (candidateOverride == null && CanRetainExistingProjectGenre(identity, year)) {
			float retention = GetProjectIdentityRetention(identity, year, artist);
			if (roll < retention) return new GenreSelection(identity, true);
			roll = (roll - retention) / (1f - retention);
		}
		bool usedCandidateOverride = candidateOverride != null;
		if (applyPsychedelicTransitionCompatibility) {
			IReadOnlyList<Genre> compatible = FilterProspectivePsychedelicCandidates(candidates, identity, year);
			// An annual supply floor may contain only an incompatible Psychedelic
			// candidate. Preserve the release opportunity by reusing this exact roll
			// against the compatible normal set; never force an incompatible blend.
			if (compatible.Count == 0 && candidateOverride != null) {
				compatible = FilterProspectivePsychedelicCandidates(GetAvailableGenres(year), identity, year);
				usedCandidateOverride = false;
			}
			candidates = compatible;
		}
		if (candidates.Count == 0) {
			if (identity == Genre.TraditionalPop) ReportTraditionalPopFallback("GenreSupplyService.EmptyCompatibleCandidateSet", identity);
			return new GenreSelection(identity, false);
		}
		var weighted = candidates.Select(genre => (genre, weight: GetSupplyWeight(genre, label, artist, region, year,
			recentSupply, globalRecentSupply, SupplyWeightContext.ProjectTransition))).ToArray();
		float total = weighted.Sum(entry => entry.weight);
		if (total <= 0f) return new GenreSelection(weighted[0].genre, false, usedCandidateOverride);
		float target = Mathf.Clamp(roll, 0f, .99999994f) * total;
		float cumulative = 0f;
		foreach ((Genre genre, float weight) in weighted) {
			cumulative += weight;
			if (target < cumulative) return new GenreSelection(genre, false, usedCandidateOverride);
		}
		return new GenreSelection(weighted[^1].genre, false, usedCandidateOverride);
	}

	/// <summary>
	/// Static prospective lineage guard for the one emerging Psychedelic project
	/// seam.  The record's secondary will be the artist's primary identity, so the
	/// authored family/adjacency graph is sufficient; realized releases, timing,
	/// demand, charts, and momentum are deliberately absent.
	/// </summary>
	public static bool IsPsychedelicTransitionCompatible(Genre artistIdentity, float year) {
		Genre canonicalIdentity = GenreCatalog.MapLegacy(artistIdentity, (int)MathF.Floor(year));
		return GenreMarketMomentumService.GetAdjacency(canonicalIdentity, Genre.PsychedelicRock) >= .12f;
	}

	internal static IReadOnlyList<Genre> GetProspectivePsychedelicCandidatesForProbe(
		IReadOnlyList<Genre> candidates, Genre artistIdentity, float year, bool applyCompatibility) =>
		applyCompatibility ? FilterProspectivePsychedelicCandidates(candidates, artistIdentity, year) : candidates;

	private static IReadOnlyList<Genre> FilterProspectivePsychedelicCandidates(
		IReadOnlyList<Genre> candidates, Genre artistIdentity, float year) {
		if (candidates == null || candidates.Count == 0 || IsPsychedelicTransitionCompatible(artistIdentity, year)) return candidates ?? Array.Empty<Genre>();
		return candidates.Where(candidate => GenreCatalog.MapLegacy(candidate, (int)MathF.Floor(year)) != Genre.PsychedelicRock).ToArray();
	}

	public static float GetSupplyWeight(Genre genre, AILabel label, SimulatedArtist artist, MarketRegion region,
		float year, IReadOnlyDictionary<Genre, int> recentSupply = null, IReadOnlyDictionary<Genre, int> globalRecentSupply = null,
		SupplyWeightContext context = SupplyWeightContext.Formation) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		if (!IsAvailableForNewSupply(canonical, year)) return 0f;
		GenreProfile profile = GenreCatalog.Get(canonical);
		float acceptance = GetProspectiveSupplyAcceptance(canonical, profile, region, year);
		// Keep a small nonzero discovery floor, but preserve enough of the authored
		// acceptance range for rising and declining genres to separate. The former
		// .20 floor compressed a .10/.90 pair to only a 3.3x supply distinction.
		float demand = .05f + .95f * Mathf.Clamp(acceptance, 0f, 1f);
		float artistFit = GetIdentityFit(canonical, profile.Family, artist, context);
		float labelFit = GetLabelFit(canonical, profile.Family, label);
		// Emerging carried a .65f penalty, which taxed a genre by 35% during exactly the
		// window a new scene should be forming the acts that build its catalog. It is a
		// one-year window (GetLifecycle: year < EmergenceYear + 1), so it reads as small,
		// but it lands on the launch year and measured formation share against authored
		// baseline share at 0.44-0.45 for genres in that window against ~1.0 for
		// established ones. Declining is unchanged: a genre losing its audience really
		// does stop attracting new acts.
		float lifecycle = profile.GetLifecycle(year) switch {
			GenreLifecycleState.Declining => .35f,
			_ => 1f
		};
		int recent = recentSupply != null && recentSupply.TryGetValue(canonical, out int count) ? count : 0;
		float concentrationBrake = 1f / (1f + Mathf.Min(recent, 8) * .06f);
		float globalConcentrationBrake = GetGlobalConcentrationBrake(canonical, globalRecentSupply);
		float britishBridge = GetBritishBridgeWeight(canonical, year);
		float formationAffinity = SplitFormationAffinity && context == SupplyWeightContext.ProjectTransition
			? 1f : FormationAffinity(canonical, year);
		return Mathf.Max(.000001f, demand * artistFit * labelFit * lifecycle * concentrationBrake
			* globalConcentrationBrake * britishBridge * formationAffinity);
	}

	// ---- FORMATION AFFINITY --------------------------------------------------------------------
	// How many ACTS a genre attracts, as distinct from how many LISTENERS it has. Everything else in
	// this weight descends from `demand`, i.e. the audience baseline -- which silently asserts that a
	// genre's share of working musicians equals its share of the record-buying public. For most genres
	// that is a fair default. For Country it is simply false, and the model shows the damage:
	//
	//   Country artist-population share  6.48% (1960) -> 5.50% (1969)     [falling]
	//   Country market unit-share target 8.38% (1960) -> 11.69% (1969)    [rising]
	//
	// Two things make baseline the wrong lever here. First, market share tracks eligibleRecords
	// (r=0.938 at 1969) far more than baseline (0.756), and Country's realised acceptance is already
	// 0.854-1.00 by region -- it SATURATES at 1.000 in the Southwest -- so extra baseline stops
	// transmitting to the market entirely. Second, this weight is a SHARE over every available genre,
	// and the roster roughly doubles across the decade as new genres emerge, so a flat-baseline genre
	// is diluted by proliferation even when nothing about it changed. Country is exactly that: its
	// baseline is near-flat (.535 -> .68) while its historical target rises.
	//
	// Historically Country's working-act population was enormous relative to its Hot 100 presence --
	// the Nashville session/songwriter economy, the honky-tonk and package-tour circuit, and a
	// regional radio ecology that supported full-time acts who never charted pop. That is precisely a
	// formation-side fact that no audience baseline encodes.
	//
	// Deliberately narrow: default 1.0 for everything else, so this cannot quietly re-weight the
	// calibrated genre balance. NOTE it applies to project selection as well as artist formation (both
	// route through GetSupplyWeight), though 71% of project selections are Retained and never reach
	// this weight, so formation carries most of the effect.
	//
	// MEASURED ELASTICITY (mix6 -> mix7, the 1.0 -> 1.6 Country step over a full decade): 1969 Country
	// market share moved 5.98% -> 8.25%, so realised share goes as roughly affinity^0.68. Both values
	// below are sized off that exponent, and both deliberately UNDERSHOOT their target: the exponent
	// comes from a single step, the two genres now compete for the same formation pool, and this weight
	// is zero-sum across every available genre.
	//
	// COUNTRY, flat 2.2. Uniformly under target all decade (realised/target 0.62-0.85, no year above
	// 0.85), so the deficit has no shape and a flat multiplier is the honest fit. Predicts ~10.2% at
	// 1969 against an 11.69% target.
	//
	// SOUL, ramped 1.0 (<=1962) -> 1.8 (1968+). NOT flat, and this is the point: Soul is OVER its target
	// early -- realised/target 1.36/1.63/1.35 for 1960-62 -- and under from 1964 on, reaching 0.69 by
	// 1969. A flat raise would buy the late years by making a real early over-shoot worse. The ramp
	// leaves 1960-62 untouched and lifts only the years actually short. It is also the historically
	// literal shape: the number of working soul acts exploded across the decade as the Motown, Stax and
	// Atlantic rosters grew, which is a formation fact and not an audience-baseline one. Predicts ~18%
	// at 1969 against a 17.53% target, sized slightly high because Country's simultaneous rise to 2.2
	// pulls from the same pool.
	// COUNTRY UNDER THE SPLIT. The 2.2 above was measured with this weight applied to BOTH
	// artist formation and project transition, and the split removes the second channel. That
	// channel was not incidental: Country drew 1,129 of its 6,310 projects (17.9%) from
	// non-Country identities in mix8, and splitting cut that to 516 and its total supply to
	// 5,365 -- decade-mean realised share 8.31% -> 7.65% against a 9.95% target mean. So the
	// split cannot ship without restoring that supply on the formation side, or it simply
	// converts a leak into a deficit on the genre that already had the largest one.
	//
	// SIZED, not guessed: 9.95/7.65 = 1.30x share needed; at the measured affinity^0.68 that
	// is a 1.47x multiplier, i.e. ~3.24. Set to 3.0 and DELIBERATELY UNDERSHOOTING, exactly as
	// the two values above do, because this weight is zero-sum across every available genre --
	// every point Country takes here comes out of Soul's ramp and the folk family we are
	// trying to protect. Note the exponent now runs on one channel rather than two, so 3.0 may
	// prove light; correct it upward from a measured run rather than downward from a scare.
	private const float CountryAffinity = 2.2f, CountryAffinitySplitCompensated = 3.0f;
	private const float SoulAffinityEarly = 1f, SoulAffinityLate = 1.8f;
	private const int SoulAffinityRampStart = 1962, SoulAffinityRampEnd = 1968;

	private static float FormationAffinity(Genre canonical, float year) => canonical switch {
		// Conditional on the split by design: with both channels live the authored 2.2 stands
		// untouched, so this pair of changes is revertible as a unit rather than leaving a
		// compensating value stranded against a channel that is no longer missing.
		Genre.Country => SplitFormationAffinity ? CountryAffinitySplitCompensated : CountryAffinity,
		Genre.Soul => Mathf.Lerp(SoulAffinityEarly, SoulAffinityLate,
			Mathf.Clamp((year - SoulAffinityRampStart) / (SoulAffinityRampEnd - SoulAffinityRampStart), 0f, 1f)),
		_ => 1f
	};

	internal static float GetProspectiveSupplyAcceptanceForProbe(Genre genre, MarketRegion region, float year) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		return GetProspectiveSupplyAcceptance(canonical, GenreCatalog.Get(canonical), region, year);
	}

	private static float GetProspectiveSupplyAcceptance(Genre canonical, GenreProfile profile, MarketRegion region, float year) {
		if (region == null) return profile.GetBaseline(year);
		if (canonical is not (Genre.Country or Genre.TexMex or Genre.Boogaloo)) return region.GetGenreAcceptance(canonical, year);
		float legacyMomentum = ChartManager.Instance?.GetGenreMomentum(canonical) ??
			(region.genreMomentum != null && region.genreMomentum.TryGetValue(canonical, out float value) ? value : 0f);
		// Do not replace the enabled V2 route with legacy acceptance.  Specialist
		// supply retains catalog, segment, regional, lifecycle, and momentum inputs;
		// only the new centered realized-demand texture is excluded prospectively.
		return GenreAcceptanceService.GetRegionalDemandAcceptanceWithoutCenteredSpecialistTexture(
			canonical, canonical, region, year, legacyMomentum);
	}

	private static float GetProjectIdentityRetention(Genre genre, float year, SimulatedArtist artist = null) {
		GenreProfile profile = GenreCatalog.Get(genre);
		if (year <= profile.EmergenceYear) return .80f;
		float peakBaseline = profile.BaselineKeyframes.Max();
		float baselinePosition = peakBaseline > 0f ? Mathf.Clamp(profile.GetBaseline(year) / peakBaseline, 0f, 1f) : 0f;
		float priorBaseline = profile.GetBaseline(Mathf.Max(1960f, year - 1f));
		float slope = profile.GetBaseline(year) - priorBaseline;
		float retention = .22f + .60f * baselinePosition;
		if (profile.DeathYear.HasValue && year > profile.DeathYear.Value) {
			float yearsSinceDeath = year - profile.DeathYear.Value;
			float tailProgress = 1f - MathF.Exp(-.26f * yearsSinceDeath);
			retention = Mathf.Lerp(retention, .18f, tailProgress);
		} else if (!profile.DeathYear.HasValue && slope < 0f) {
			// No-death profiles still need to leave the established plateau when their
			// authored commercial baseline declines (notably Rock and Roll).
			retention *= 1f - .80f * (1f - baselinePosition);
		}
		if (slope < 0f) retention += slope * .50f;
		int projectHistory = artist?.releaseHistory?.Count ?? 0;
		retention += Mathf.Min(projectHistory, 3) * .03f;
		return Mathf.Clamp(retention, .12f, .88f);
	}

	internal static float GetProjectIdentityRetentionForPortfolio(Genre genre, float year) =>
		GetProjectIdentityRetention(GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year)), year);

	// This is a prospective project-selection brake, not a normalization from realized sales.
	private static float GetGlobalConcentrationBrake(Genre genre, IReadOnlyDictionary<Genre, int> globalRecentSupply) {
		if (globalRecentSupply == null) return 1f;
		int total = globalRecentSupply.Values.Sum();
		if (total < 12) return 1f;
		int count = globalRecentSupply.TryGetValue(genre, out int value) ? value : 0;
		float share = (float)count / total;
		return 1f / (1f + Mathf.Max(0f, share - .20f) * 4f);
	}

	// A bounded import bridge starts at each authored British genre's emergence year.
	// It reallocates existing project opportunities; it never adds release rolls.
	private static float GetBritishBridgeWeight(Genre genre, float year) => genre switch {
		Genre.BritishBeat when year < 1964f => .03f,
		Genre.BritishBeat or Genre.BritishPop when year < 1965f => 3.5f,
		Genre.BritishBeat or Genre.BritishPop when year < 1966f => 2.25f,
		Genre.BritishBlues when year < 1965f => .50f,
		Genre.BritishBlues when year < 1966f => .75f,
		_ => 1f
	};

	/// <summary>
	/// The artist-side term: how hard an act is pinned to its lane. The four constants below
	/// are the NEUTRAL case and are reproduced exactly whenever restlessness is zero -- which
	/// is every artist when evolution or its pressure phase is off. Under pressure the primary
	/// anchor softens and adjacent candidates lift, strictly inside the authored bounds:
	/// never below today's "other" floor, never above today's primary. This biases WHETHER an
	/// act wanders. It does not pick a genre, and it adds no candidate that was not already in
	/// the pool.
	/// </summary>
	private static float GetIdentityFit(Genre genre, GenreFamily family, SimulatedArtist artist,
		SupplyWeightContext context = SupplyWeightContext.Formation) {
		if (artist == null) return 1f;
		Genre primary = GenreCatalog.MapLegacy(artist.primaryGenre);
		Genre secondary = GenreCatalog.MapLegacy(artist.secondaryGenre);
		float restlessness = GetRestlessness(artist, context);
		if (genre == primary) return Mathf.Lerp(ArtistEvolution.IdentityFitPrimaryNeutral,
			ArtistEvolution.IdentityFitPrimaryRestless, restlessness);
		if (genre == secondary) return 2.25f;
		bool sameFamily = (GenreCatalog.TryGet(primary, out GenreProfile p) && p.Family == family) ||
			(GenreCatalog.TryGet(secondary, out GenreProfile s) && s.Family == family);
		// Roots mode aims the lift backward at the sound they started on rather than forward
		// at whatever is adjacent now: the band that strips back to blues after two failed pop
		// singles is this same mechanism running in reverse.
		if (restlessness > 0f && artist.evolution?.rootsMode == true)
			return genre == GenreCatalog.MapLegacy(artist.formationPrimaryGenre)
				? Mathf.Lerp(ArtistEvolution.IdentityFitAdjacentNeutral, ArtistEvolution.IdentityFitAdjacentRestless, restlessness)
				: sameFamily ? ArtistEvolution.IdentityFitAdjacentNeutral : .55f;
		if (!ArtistEvolution.AdjacencyIdentityFit)
			return sameFamily ? Mathf.Lerp(ArtistEvolution.IdentityFitAdjacentNeutral,
				ArtistEvolution.IdentityFitAdjacentRestless, restlessness) : .55f;
		// The act can reach a genre from either identity it holds, so the nearer one governs.
		float adjacency = Mathf.Max(GenreMarketMomentumService.GetAdjacency(primary, genre),
			GenreMarketMomentumService.GetAdjacency(secondary, genre));
		if (adjacency <= 0f) return .55f;
		float reach = Mathf.Clamp(adjacency / AdjacencyFitSaturation, 0f, 1f);
		// Family is the BASE tier and adjacency modulates inside it. A single continuous
		// scale spanning both tiers was tried and measured, and it inverts the thing this is
		// for: a cross-family destination with an authored edge starts from the .55 floor and
		// therefore has far more room to gain than a same-family lineage starting from 1.45.
		// For a Folk act the one-scale version lifted Country (edge .45, cross-family) by 2.5x
		// while lifting FolkRock (edge .75, same-family) by only 1.3x, compressing FolkRock's
		// advantage over Country from 2.64x to 1.40x -- and Country's draw on folk acts rose
		// 71 -> 98 across 1965-69 while FolkRock's fell 61 -> 57, the exact opposite of intent.
		// Keeping the tiers disjoint (cross-family can never reach the same-family floor)
		// preserves the ordering the calibration was built on and spends adjacency only on
		// discriminating WITHIN it.
		float floor = sameFamily ? SameFamilyFitFloor : .55f;
		float neutralCeiling = sameFamily ? AdjacencyFitNeutralCeiling : CrossFamilyFitCeiling;
		float restlessCeiling = sameFamily ? ArtistEvolution.IdentityFitAdjacentRestless : CrossFamilyFitCeiling;
		return Mathf.Lerp(Mathf.Lerp(floor, neutralCeiling, reach),
			Mathf.Lerp(floor, restlessCeiling, reach), restlessness);
	}

	// ---- ADJACENCY-AWARE IDENTITY FIT ----------------------------------------------------------
	// The middle tier was a flat 1.45 for "shares a family with the artist". That bucket is far
	// too coarse to carry the thing it is being asked to predict. The Folk family holds Folk,
	// ContemporaryFolk, FolkRock and SingerSongwriter at an identical 1.45, so the weight cannot
	// tell the authored Folk->FolkRock lineage (explicit edge .75) from a pairing with no authored
	// relationship at all (the .12 family floor). Measured consequence: across 1965-69 only 61
	// folk-family projects were FolkRock, from 56 distinct artists ever, while FolkRock ran 0.9
	// to 1.9 points UNDER its market-share target every year and ContemporaryFolk ran 1.2 to 2.1
	// points OVER its own. The family had the acts; the weight could not aim them.
	//
	// So the tier reads the authored adjacency graph, which IS the lineage map. PREREQUISITE, and
	// the reason this cannot ship before the edge fill: 16 of 45 genres carried no explicit edge,
	// so under this rule they would collapse to the family floor and take a permanent supply
	// penalty for being unauthored rather than for being unrelated.
	// CrossFamilyFitCeiling sits strictly below SameFamilyFitFloor so the two tiers cannot
	// overlap: however strong a cross-family edge is, it never outranks family membership.
	private const float AdjacencyFitSaturation = .75f, AdjacencyFitNeutralCeiling = 1.90f;
	private const float SameFamilyFitFloor = 1.10f, CrossFamilyFitCeiling = 1.05f;

	/// <summary>
	/// Scoped to project transition on purpose. Restlessness is a statement about what the
	/// act records next, not about how attractive they are to a label -- this same weight is
	/// what scouting ranks candidates by, and a restless act must not become harder to sign.
	/// </summary>
	private static float GetRestlessness(SimulatedArtist artist, SupplyWeightContext context) =>
		context == SupplyWeightContext.ProjectTransition && ArtistEvolution.PressureEnabled && artist.evolution != null
			? Mathf.Clamp(artist.evolution.restlessness, 0f, 1f) : 0f;

	internal static float GetIdentityFitForProbe(Genre genre, SimulatedArtist artist,
		SupplyWeightContext context = SupplyWeightContext.ProjectTransition) {
		Genre canonical = GenreCatalog.MapLegacy(genre);
		return GetIdentityFit(canonical, GenreCatalog.Get(canonical).Family, artist, context);
	}

	private static float GetLabelFit(Genre genre, GenreFamily family, AILabel label) {
		if (label == null) return 1f;
		IEnumerable<Genre> specialties = (label.preferredGenres ?? Array.Empty<Genre>())
			.Concat(label.secondaryGenres ?? Array.Empty<Genre>()).Select(genre => GenreCatalog.MapLegacy(genre)).Distinct();
		if (specialties.Contains(genre)) return 2.4f;
		if (specialties.Any(candidate => GenreCatalog.TryGet(candidate, out GenreProfile profile) && profile.Family == family)) return 1.35f;
		return .75f;
	}
}
