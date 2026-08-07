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
		var weighted = candidates.Select(genre => (genre, weight: GetSupplyWeight(genre, label, artist, region, year, recentSupply, globalRecentSupply))).ToArray();
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
		float year, IReadOnlyDictionary<Genre, int> recentSupply = null, IReadOnlyDictionary<Genre, int> globalRecentSupply = null) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		if (!IsAvailableForNewSupply(canonical, year)) return 0f;
		GenreProfile profile = GenreCatalog.Get(canonical);
		float acceptance = GetProspectiveSupplyAcceptance(canonical, profile, region, year);
		// Keep a small nonzero discovery floor, but preserve enough of the authored
		// acceptance range for rising and declining genres to separate. The former
		// .20 floor compressed a .10/.90 pair to only a 3.3x supply distinction.
		float demand = .05f + .95f * Mathf.Clamp(acceptance, 0f, 1f);
		float artistFit = GetIdentityFit(canonical, profile.Family, artist);
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
		return Mathf.Max(.000001f, demand * artistFit * labelFit * lifecycle * concentrationBrake * globalConcentrationBrake * britishBridge);
	}

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

	private static float GetIdentityFit(Genre genre, GenreFamily family, SimulatedArtist artist) {
		if (artist == null) return 1f;
		Genre primary = GenreCatalog.MapLegacy(artist.primaryGenre);
		Genre secondary = GenreCatalog.MapLegacy(artist.secondaryGenre);
		if (genre == primary) return 4f;
		if (genre == secondary) return 2.25f;
		if ((GenreCatalog.TryGet(primary, out GenreProfile p) && p.Family == family) ||
			(GenreCatalog.TryGet(secondary, out GenreProfile s) && s.Family == family)) return 1.45f;
		return .55f;
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
