using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase-2 AI supply routing. This service is deliberately stateless: callers own
/// RNG and recent-release counts so the disabled stream cannot be disturbed.
/// </summary>
public static class GenreSupplyService {
	public readonly struct GenreSelection {
		public readonly Genre Genre;
		public readonly bool RetainedIdentity;
		public GenreSelection(Genre genre, bool retainedIdentity) { Genre = genre; RetainedIdentity = retainedIdentity; }
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

	public static bool IsBritishSupplyBridgeActive(Genre genre, float year) => genre switch {
		Genre.BritishBeat or Genre.BritishPop => year >= 1964f,
		Genre.BritishBlues => year >= 1965f,
		_ => false
	};

	private static bool IsBritishBridgeGenre(Genre genre) => genre is Genre.BritishPop or Genre.BritishBeat or Genre.BritishBlues;

	public static IReadOnlyList<Genre> GetAvailableGenres(float year) => GenreCatalog.All
		.Where(profile => IsAvailableForNewSupply(profile.Genre, year))
		.OrderBy(profile => profile.Id, StringComparer.Ordinal)
		.Select(profile => profile.Genre)
		.ToArray();

	public static Genre ChooseGenre(AILabel label, SimulatedArtist artist, MarketRegion region, float year,
		IReadOnlyDictionary<Genre, int> recentSupply, float roll, IReadOnlyList<Genre> candidateOverride = null,
		IReadOnlyDictionary<Genre, int> globalRecentSupply = null) =>
		ChooseGenreWithSelection(label, artist, region, year, recentSupply, roll, candidateOverride, globalRecentSupply).Genre;

	/// <summary>Returns the existing deterministic selection plus whether identity retention selected the project.</summary>
	public static GenreSelection ChooseGenreWithSelection(AILabel label, SimulatedArtist artist, MarketRegion region, float year,
		IReadOnlyDictionary<Genre, int> recentSupply, float roll, IReadOnlyList<Genre> candidateOverride = null,
		IReadOnlyDictionary<Genre, int> globalRecentSupply = null) {
		IReadOnlyList<Genre> candidates = candidateOverride ?? GetAvailableGenres(year);
		if (candidates.Count == 0) return new GenreSelection(GenreCatalog.MapLegacy(artist?.primaryGenre ?? Genre.TraditionalPop, (int)year), false);
		Genre identity = GenreCatalog.MapLegacy(artist?.primaryGenre ?? Genre.TraditionalPop, (int)year);
		if (candidateOverride == null && CanRetainExistingProjectGenre(identity, year)) {
			float retention = GetProjectIdentityRetention(identity, year);
			if (roll < retention) return new GenreSelection(identity, true);
			roll = (roll - retention) / (1f - retention);
		}
		var weighted = candidates.Select(genre => (genre, weight: GetSupplyWeight(genre, label, artist, region, year, recentSupply, globalRecentSupply))).ToArray();
		float total = weighted.Sum(entry => entry.weight);
		if (total <= 0f) return new GenreSelection(weighted[0].genre, false);
		float target = Mathf.Clamp(roll, 0f, .99999994f) * total;
		float cumulative = 0f;
		foreach ((Genre genre, float weight) in weighted) {
			cumulative += weight;
			if (target < cumulative) return new GenreSelection(genre, false);
		}
		return new GenreSelection(weighted[^1].genre, false);
	}

	public static float GetSupplyWeight(Genre genre, AILabel label, SimulatedArtist artist, MarketRegion region,
		float year, IReadOnlyDictionary<Genre, int> recentSupply = null, IReadOnlyDictionary<Genre, int> globalRecentSupply = null) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		if (!IsAvailableForNewSupply(canonical, year)) return 0f;
		GenreProfile profile = GenreCatalog.Get(canonical);
		float acceptance = region != null ? region.GetGenreAcceptance(canonical, year) : profile.GetBaseline(year);
		float demand = .20f + .80f * Mathf.Clamp(acceptance, 0f, 1f);
		float artistFit = GetIdentityFit(canonical, profile.Family, artist);
		float labelFit = GetLabelFit(canonical, profile.Family, label);
		float lifecycle = profile.GetLifecycle(year) switch {
			GenreLifecycleState.Emerging => .65f,
			GenreLifecycleState.Declining => .35f,
			_ => 1f
		};
		int recent = recentSupply != null && recentSupply.TryGetValue(canonical, out int count) ? count : 0;
		float concentrationBrake = 1f / (1f + Mathf.Min(recent, 8) * .06f);
		float globalConcentrationBrake = GetGlobalConcentrationBrake(canonical, globalRecentSupply);
		float britishBridge = GetBritishBridgeWeight(canonical, year);
		return Mathf.Max(.000001f, demand * artistFit * labelFit * lifecycle * concentrationBrake * globalConcentrationBrake * britishBridge);
	}

	private static float GetProjectIdentityRetention(Genre genre, float year) {
		GenreLifecycleState lifecycle = GenreCatalog.Get(genre).GetLifecycle(year);
		return lifecycle switch {
			GenreLifecycleState.Legacy => .12f,
			GenreLifecycleState.Declining => .30f,
			_ => year < 1961f ? .95f : .78f
		};
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

	// A bounded 1964 import bridge reallocates existing project opportunities; it never adds release rolls.
	private static float GetBritishBridgeWeight(Genre genre, float year) => genre switch {
		Genre.BritishBeat or Genre.BritishPop when year < 1965f => 3.5f,
		Genre.BritishBeat or Genre.BritishPop when year < 1966f => 2.25f,
		Genre.BritishBlues when year < 1966f => 2.25f,
		Genre.BritishBlues when year < 1967f => 1.5f,
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
