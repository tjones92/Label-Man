using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public readonly struct GenreAcceptanceExplanation {
	public readonly float Baseline, SegmentReach, SegmentCapacity, RegionalFactor, LegacyMomentum, MomentumContribution, Effective;
	public GenreAcceptanceExplanation(float baseline, float reach, float capacity, float regional, float momentum, float momentumContribution, float routing) {
		Baseline = baseline; SegmentReach = reach; SegmentCapacity = capacity; RegionalFactor = regional;
		LegacyMomentum = momentum; MomentumContribution = momentumContribution;
		// The routing preference is centered against this region's capacity-weighted
		// mean. It therefore changes texture without creating a second population.
		// The legacy accumulator is an intensity signal, not an acceptance-point
		// override. Its configured influence therefore supplies a bounded relative
		// lift to the routed catalog value, preserving authored declines instead of
		// pinning mature genres at 1.0.
		Effective = Mathf.Clamp(baseline * regional * routing * (1f + momentumContribution), 0f, 1f);
	}
}

/// <summary>Sequential, read-only decomposition of one blended regional acceptance route.</summary>
public readonly struct RegionalDemandAcceptanceComponents {
	public readonly float CatalogBaseline, RegionalAdjusted, SegmentRouted, PrimaryWeightedRouted;
	public readonly float SecondaryBlendContribution, LegacyMomentum, LegacyMomentumAcceptanceContribution, ClampDelta, Effective;
	public RegionalDemandAcceptanceComponents(float baseline, float regionalAdjusted, float segmentRouted, float primaryWeightedRouted,
		float legacyMomentum, float momentumContribution, float clampDelta, float effective) {
		CatalogBaseline = baseline; RegionalAdjusted = regionalAdjusted; SegmentRouted = segmentRouted;
		PrimaryWeightedRouted = primaryWeightedRouted; SecondaryBlendContribution = segmentRouted - primaryWeightedRouted;
		LegacyMomentum = legacyMomentum; LegacyMomentumAcceptanceContribution = momentumContribution;
		ClampDelta = clampDelta; Effective = effective;
	}
}

/// <summary>Population-weighted fixed-input specialist-route audit after routing, clamping, and Single conversion.</summary>
internal readonly struct SpecialistRoutingProbe {
	public readonly float EffectiveAcceptance, ClampLoss, FinalSingleOpportunity;
	public readonly float ProtectedEffectiveAcceptance, ProtectedClampLoss, ProtectedFinalSingleOpportunity;
	public readonly float SingleOpportunityNormalizer, NormalizedFinalSingleOpportunity;
	public SpecialistRoutingProbe(float effectiveAcceptance, float clampLoss, float finalSingleOpportunity,
		float protectedEffectiveAcceptance, float protectedClampLoss, float protectedFinalSingleOpportunity,
		float singleOpportunityNormalizer, float normalizedFinalSingleOpportunity) {
		EffectiveAcceptance = effectiveAcceptance;
		ClampLoss = clampLoss;
		FinalSingleOpportunity = finalSingleOpportunity;
		ProtectedEffectiveAcceptance = protectedEffectiveAcceptance;
		ProtectedClampLoss = protectedClampLoss;
		ProtectedFinalSingleOpportunity = protectedFinalSingleOpportunity;
		SingleOpportunityNormalizer = singleOpportunityNormalizer;
		NormalizedFinalSingleOpportunity = normalizedFinalSingleOpportunity;
	}
}

/// <summary>Single enabled Phase-2 acceptance owner. It intentionally consumes, but does not evolve, legacy momentum.</summary>
public static class GenreAcceptanceService {
	private const float FormatOrientationStrength = .60f;
	private const float DefaultLegacyMomentumInfluence = .3f;
	private const float SingleDemandLegacyIntercept = .60f;
	private const float SingleDemandLegacySlope = .50f;
	private const float SingleOpportunityNormalizationFloor = .90f;
	private const float SingleOpportunityNormalizationCeiling = 1.10f;
	private const float SingleOpportunityNormalizationStartYear = 1964f;
	private const float SingleOpportunityNormalizationFullYear = 1966f;

	public readonly struct SingleOpportunityReconciliation {
		public readonly float EnabledOpportunity, AcceptedOpportunity, EnabledToAcceptedRatio;
		public readonly float AnchorEnabledToAcceptedRatio, Normalization;
		public SingleOpportunityReconciliation(float enabledOpportunity, float acceptedOpportunity,
			float enabledToAcceptedRatio, float anchorEnabledToAcceptedRatio, float normalization) {
			EnabledOpportunity = enabledOpportunity;
			AcceptedOpportunity = acceptedOpportunity;
			EnabledToAcceptedRatio = enabledToAcceptedRatio;
			AnchorEnabledToAcceptedRatio = anchorEnabledToAcceptedRatio;
			Normalization = normalization;
		}
	}
	private readonly record struct RegionalDemandKey(Genre Primary, Genre Secondary, ulong RegionInstanceId, int YearBits, int MomentumBits,
		bool IncludeCenteredSpecialistTexture);
	private static readonly Dictionary<RegionalDemandKey, float> RegionalDemandCache = new();
	private static int cacheYearBits = int.MinValue;
	private readonly record struct SpecialistSingleNormalizerKey(Genre Primary, Genre Secondary, int Year);
	private static readonly Dictionary<SpecialistSingleNormalizerKey, float> SpecialistSingleNormalizerCache = new();

	public static GenreAcceptanceExplanation Evaluate(Genre genre, MarketRegion region, AudienceSegment segment, float year, float legacyMomentum) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		GenreProfile profile = GenreCatalog.Get(canonical);
		float baseline = profile.GetBaseline(year);
		float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
		float reach = profile.SegmentWeights.TryGetValue(segment.ToString(), out float weight) ? weight : 0f;
		float regional = GetRegionalFactor(canonical, region, segment, year);
		float weightedMean = GetWeightedSegmentReach(profile, region);
		float routing = Mathf.Max(.25f, 1f + (reach - weightedMean) * 1.25f);
		return new GenreAcceptanceExplanation(baseline, reach, capacity, regional, legacyMomentum,
			GetLegacyMomentumContribution(legacyMomentum), routing);
	}

	/// <summary>Blends the normalized segment routes without multiplying population.</summary>
	public static float GetRegionalDemandAcceptance(Genre primary, Genre secondary, MarketRegion region, float year, float legacyMomentum = 0f) {
		return GetRegionalDemandAcceptance(primary, secondary, region, year, legacyMomentum, includeCenteredSpecialistTexture: true);
	}

	/// <summary>
	/// Prospective specialist supply retains the enabled catalog, segment, regional,
	/// lifecycle, and momentum route while excluding only the realized-demand
	/// centered specialist texture.
	/// </summary>
	internal static float GetRegionalDemandAcceptanceWithoutCenteredSpecialistTexture(
		Genre primary, Genre secondary, MarketRegion region, float year, float legacyMomentum = 0f) =>
		GetRegionalDemandAcceptance(primary, secondary, region, year, legacyMomentum, includeCenteredSpecialistTexture: false);

	private static float GetRegionalDemandAcceptance(Genre primary, Genre secondary, MarketRegion region, float year,
		float legacyMomentum, bool includeCenteredSpecialistTexture) {
		int yearBits = BitConverter.SingleToInt32Bits(year);
		if (yearBits != cacheYearBits) {
			RegionalDemandCache.Clear();
			cacheYearBits = yearBits;
		}
		var key = new RegionalDemandKey(primary, secondary, region.GetInstanceId(), yearBits, BitConverter.SingleToInt32Bits(legacyMomentum),
			includeCenteredSpecialistTexture);
		if (RegionalDemandCache.TryGetValue(key, out float cached)) return cached;
		float secondaryWeight = primary == secondary ? 0f : .20f;
		float primaryWeight = 1f - secondaryWeight;
		Genre canonicalPrimary = GenreCatalog.MapLegacy(primary, (int)MathF.Floor(year));
		Genre canonicalSecondary = GenreCatalog.MapLegacy(secondary, (int)MathF.Floor(year));
		GenreProfile primaryProfile = GenreCatalog.Get(canonicalPrimary);
		GenreProfile secondaryProfile = secondaryWeight > 0f ? GenreCatalog.Get(canonicalSecondary) : null;
		float primaryMean = GetWeightedSegmentReach(primaryProfile, region);
		float secondaryMean = secondaryProfile != null ? GetWeightedSegmentReach(secondaryProfile, region) : 0f;
		float total = 0f;
		foreach (AudienceSegment segment in SegmentCapacityModel.All) {
			float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
			if (capacity <= 0f) continue;
			float acceptance = Evaluate(canonicalPrimary, primaryProfile, primaryMean, region, segment, year, legacyMomentum,
				includeCenteredSpecialistTexture).Effective * primaryWeight;
			if (secondaryWeight > 0f) acceptance += Evaluate(canonicalSecondary, secondaryProfile, secondaryMean, region, segment, year,
				legacyMomentum, includeCenteredSpecialistTexture).Effective * secondaryWeight;
			total += capacity * acceptance;
		}
		return RegionalDemandCache[key] = Mathf.Clamp(total, 0f, 1f);
	}

	/// <summary>Pure audit decomposition that exactly follows the blended regional acceptance calculation.</summary>
	public static RegionalDemandAcceptanceComponents GetRegionalDemandAcceptanceComponents(
		Genre primary, Genre secondary, MarketRegion region, float year, float legacyMomentum = 0f) {
		return GetRegionalDemandAcceptanceComponents(primary, secondary, region, year, legacyMomentum, includeCenteredSpecialistTexture: true);
	}

	private static RegionalDemandAcceptanceComponents GetRegionalDemandAcceptanceComponents(
		Genre primary, Genre secondary, MarketRegion region, float year, float legacyMomentum, bool includeCenteredSpecialistTexture) {
		float secondaryWeight = primary == secondary ? 0f : .20f;
		float primaryWeight = 1f - secondaryWeight;
		GenreProfile primaryProfile = GenreCatalog.Get(GenreCatalog.MapLegacy(primary, (int)MathF.Floor(year)));
		GenreProfile secondaryProfile = secondaryWeight > 0f
			? GenreCatalog.Get(GenreCatalog.MapLegacy(secondary, (int)MathF.Floor(year))) : null;
		Genre canonicalPrimary = GenreCatalog.MapLegacy(primary, (int)MathF.Floor(year));
		Genre canonicalSecondary = secondaryWeight > 0f ? GenreCatalog.MapLegacy(secondary, (int)MathF.Floor(year)) : canonicalPrimary;
		float primaryMean = GetWeightedSegmentReach(primaryProfile, region);
		float secondaryMean = secondaryProfile != null ? GetWeightedSegmentReach(secondaryProfile, region) : 0f;
		float momentumFactor = GetLegacyMomentumContribution(legacyMomentum);
		float baseline = 0f, regionalAdjusted = 0f, segmentRouted = 0f, primaryWeightedRouted = 0f;
		float momentumContribution = 0f, clampDelta = 0f, effective = 0f;
		foreach (AudienceSegment segment in SegmentCapacityModel.All) {
			float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
			if (capacity <= 0f) continue;
			float primaryBaseline = primaryProfile.GetBaseline(year);
			float primaryRegional = primaryBaseline * GetRegionalFactor(canonicalPrimary, region, segment, year, includeCenteredSpecialistTexture);
			float primaryRouting = Mathf.Max(.25f, 1f + (primaryProfile.SegmentWeights.GetValueOrDefault(segment.ToString()) - primaryMean) * 1.25f);
			float primaryRouted = primaryRegional * primaryRouting;
			float primaryUnclamped = primaryRouted * (1f + momentumFactor);
			float primaryEffective = Mathf.Clamp(primaryUnclamped, 0f, 1f);
			float secondaryBaseline = 0f, secondaryRegional = 0f, secondaryRouted = 0f, secondaryUnclamped = 0f, secondaryEffective = 0f;
			if (secondaryProfile != null) {
				secondaryBaseline = secondaryProfile.GetBaseline(year);
				secondaryRegional = secondaryBaseline * GetRegionalFactor(canonicalSecondary, region, segment, year, includeCenteredSpecialistTexture);
				float secondaryRouting = Mathf.Max(.25f, 1f + (secondaryProfile.SegmentWeights.GetValueOrDefault(segment.ToString()) - secondaryMean) * 1.25f);
				secondaryRouted = secondaryRegional * secondaryRouting;
				secondaryUnclamped = secondaryRouted * (1f + momentumFactor);
				secondaryEffective = Mathf.Clamp(secondaryUnclamped, 0f, 1f);
			}
			baseline += capacity * (primaryWeight * primaryBaseline + secondaryWeight * secondaryBaseline);
			regionalAdjusted += capacity * (primaryWeight * primaryRegional + secondaryWeight * secondaryRegional);
			segmentRouted += capacity * (primaryWeight * primaryRouted + secondaryWeight * secondaryRouted);
			primaryWeightedRouted += capacity * primaryWeight * primaryRouted;
			momentumContribution += capacity * (primaryWeight * (primaryUnclamped - primaryRouted) + secondaryWeight * (secondaryUnclamped - secondaryRouted));
			clampDelta += capacity * (primaryWeight * (primaryEffective - primaryUnclamped) + secondaryWeight * (secondaryEffective - secondaryUnclamped));
			effective += capacity * (primaryWeight * primaryEffective + secondaryWeight * secondaryEffective);
		}
		return new RegionalDemandAcceptanceComponents(baseline, regionalAdjusted, segmentRouted, primaryWeightedRouted,
			legacyMomentum, momentumContribution, clampDelta, effective);
	}

	public static float GetRegionalRadioOpportunity(Genre primary, Genre secondary, MarketRegion region, float year, float legacyMomentum = 0f) {
		float routed = GetRegionalDemandAcceptance(primary, secondary, region, year, legacyMomentum);
		return GetRegionalRadioOpportunity(primary, region, year, routed);
	}

	/// <summary>Applies radio infrastructure to an acceptance value already resolved for this record-region-week.</summary>
	public static float GetRegionalRadioOpportunity(Genre primary, MarketRegion region, float year, float routedAcceptance) {
		Genre canonical = GenreCatalog.MapLegacy(primary, (int)MathF.Floor(year));
		bool fmDependent = canonical is Genre.PsychedelicRock or Genre.AcidRock or Genre.HardRock or Genre.ProtoMetal or Genre.ProgressiveRock or Genre.ProtoPunk;
		if (fmDependent && (region.media?.hasFMUnderground != true || year < 1967f)) routedAcceptance *= .45f;
		// This is the existing acceptance-to-opportunity curve, not a second generic
		// radio bonus. The previous .55 + .65x curve raised even a neutral 0.50
		// acceptance to .875 and created demand before a record had earned it.
		return Mathf.Clamp(.60f + routedAcceptance * .50f, .35f, 1.10f);
	}

	/// <summary>
	/// Enabled Single conversion transfer. It retains the accepted legacy
	/// conversion once a genre is available, while a smooth availability gate
	/// removes demand for absent or near-absent catalog acceptance. This is
	/// intentionally distinct from radio opportunity, which remains an
	/// infrastructure signal rather than sales.
	/// </summary>
	public static float GetEnabledSingleDemandMultiplier(float acceptance) {
		float bounded = Mathf.Clamp(acceptance, 0f, 1f);
		float legacyTransfer = SingleDemandLegacyIntercept + SingleDemandLegacySlope * bounded;
		float availabilityGate = Mathf.SmoothStep(0f, .50f, bounded);
		return legacyTransfer * availabilityGate;
	}

	/// <summary>
	/// Reconciles only the time drift of the supplied Single portfolio. The 1960
	/// enabled/accepted relationship is the anchor, so the V2 catalog's starting
	/// level and all within-year genre/region differences remain intact. Supply
	/// weights are prospective fixed inputs; realized releases, units, chart
	/// outcomes, and annual gate results never enter this calculation.
	/// </summary>
	public static SingleOpportunityReconciliation GetSingleOpportunityReconciliation(
		IEnumerable<MarketRegion> regions, float year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? Array.Empty<MarketRegion>();
		(float enabled, float accepted) current = CalculateSuppliedSingleOpportunity(regionArray, year);
		(float enabled, float accepted) anchor = CalculateSuppliedSingleOpportunity(regionArray, 1960f);
		float currentRatio = current.enabled / Mathf.Max(.000001f, current.accepted);
		float anchorRatio = anchor.enabled / Mathf.Max(.000001f, anchor.accepted);
		float boundedNormalization = Mathf.Clamp(anchorRatio / Mathf.Max(.000001f, currentRatio),
			SingleOpportunityNormalizationFloor, SingleOpportunityNormalizationCeiling);
		float activation = Mathf.Clamp((year - SingleOpportunityNormalizationStartYear) /
			(SingleOpportunityNormalizationFullYear - SingleOpportunityNormalizationStartYear), 0f, 1f);
		activation = activation * activation * (3f - 2f * activation);
		float normalization = Mathf.Lerp(1f, boundedNormalization, activation);
		return new SingleOpportunityReconciliation(current.enabled, current.accepted, currentRatio, anchorRatio, normalization);
	}

	public static float GetLiveSingleOpportunityNormalization(IEnumerable<MarketRegion> regions, float year, bool live) =>
		!live || year <= SingleOpportunityNormalizationStartYear
			? 1f
			: GetSingleOpportunityReconciliation(regions, year).Normalization;

	private static (float enabled, float accepted) CalculateSuppliedSingleOpportunity(
		IReadOnlyList<MarketRegion> regions, float year) {
		IReadOnlyList<Genre> supplied = GenreSupplyService.GetAvailableGenres(year);
		IReadOnlyDictionary<Genre, float> initialIdentities = ArtistManager.GetEnabledInitialPrimaryGenrePrior();
		if (regions.Count == 0 || supplied.Count == 0) return (1f, 1f);
		float enabledTotal = 0f;
		float acceptedTotal = 0f;
		float populationTotal = 0f;
		foreach (MarketRegion region in regions) {
			float populationWeight = region.population * 1000000f * region.GetBuyingPopulationPercentage();
			if (populationWeight <= 0f) continue;
			float newEnabled = 0f;
			float newAccepted = 0f;
			float supplyTotal = 0f;
			foreach (Genre genre in supplied) {
				// Use the catalog/lifecycle supply prior rather than the live regional
				// acceptance path. The latter contains mutable genre momentum and would
				// turn a structural normalizer into an evaluation-order-sensitive cache.
				float supplyWeight = GenreSupplyService.GetSupplyWeight(genre, null, null, null, year);
				if (supplyWeight <= 0f) continue;
				float routedAcceptance = GetRegionalDemandAcceptance(genre, genre, region, year, 0f);
				float albumOpportunity = region.GetMarketAlbumOpportunityWeight(year);
				float formatTilt = GetFormatMultiplier(genre, genre, ReleaseFormat.Single, year, albumOpportunity);
				float acceptedAcceptance = region.GetLegacyGenreAcceptance(genre, year, includeMomentum: false);
				newEnabled += supplyWeight * GetEnabledSingleDemandMultiplier(routedAcceptance) * formatTilt;
				newAccepted += supplyWeight * (SingleDemandLegacyIntercept + SingleDemandLegacySlope * acceptedAcceptance);
				supplyTotal += supplyWeight;
			}
			float retainedEnabled = 0f;
			float retainedAccepted = 0f;
			float retainedShare = 0f;
			foreach ((Genre genre, float initialShare) in initialIdentities) {
				float retention = GenreSupplyService.GetProjectIdentityRetentionForPortfolio(genre, year);
				float weight = initialShare * retention;
				float routedAcceptance = GetRegionalDemandAcceptance(genre, genre, region, year, 0f);
				float albumOpportunity = region.GetMarketAlbumOpportunityWeight(year);
				float formatTilt = GetFormatMultiplier(genre, genre, ReleaseFormat.Single, year, albumOpportunity);
				float acceptedAcceptance = region.GetLegacyGenreAcceptance(genre, year, includeMomentum: false);
				retainedEnabled += weight * GetEnabledSingleDemandMultiplier(routedAcceptance) * formatTilt;
				retainedAccepted += weight * (SingleDemandLegacyIntercept + SingleDemandLegacySlope * acceptedAcceptance);
				retainedShare += weight;
			}
			float newShare = Mathf.Max(0f, 1f - retainedShare);
			float regionEnabled = retainedEnabled + (supplyTotal > 0f ? newShare * newEnabled / supplyTotal : 0f);
			float regionAccepted = retainedAccepted + (supplyTotal > 0f ? newShare * newAccepted / supplyTotal : 0f);
			enabledTotal += populationWeight * regionEnabled;
			acceptedTotal += populationWeight * regionAccepted;
			populationTotal += populationWeight;
		}
		return populationTotal > 0f
			? (enabledTotal / populationTotal, acceptedTotal / populationTotal)
			: (1f, 1f);
	}

	/// <summary>Applies the same configured legacy accumulator influence used by the established national path.</summary>
	public static float GetLegacyMomentumContribution(float legacyMomentum) {
		float influence = ChartManager.Instance?.GenreMomentumInfluence ?? DefaultLegacyMomentumInfluence;
		return legacyMomentum * influence;
	}

	/// <summary>
	/// Resolves the one national radio-evolution input from the same regional routes
	/// used by realized demand. The weighting is by existing buying population, so it
	/// neither creates a second audience nor lets a small region set national heat.
	/// </summary>
	public static float GetNationalDemandAcceptance(
		Genre primary,
		Genre secondary,
		IEnumerable<MarketRegion> regions,
		float year,
		float legacyMomentum = 0f) {
		float weightedAcceptance = 0f;
		float totalBuyingPopulation = 0f;
		foreach (MarketRegion region in regions) {
			if (region == null) continue;
			float buyingPopulation = region.population * 1000000f * region.GetBuyingPopulationPercentage();
			if (buyingPopulation <= 0f) continue;
			weightedAcceptance += buyingPopulation * GetRegionalDemandAcceptance(primary, secondary, region, year, legacyMomentum);
			totalBuyingPopulation += buyingPopulation;
		}
		return totalBuyingPopulation > 0f ? Mathf.Clamp(weightedAcceptance / totalBuyingPopulation, 0f, 1f) : 0f;
	}

	/// <summary>
	/// Centered relative format suitability, normalized against the accepted era
	/// opportunity. This preserves total opportunity when the market is not 50/50:
	/// (1 - albumOpportunity) * single + albumOpportunity * album == 1.
	/// </summary>
	public static float GetFormatMultiplier(Genre primary, Genre secondary, ReleaseFormat format, float year,
		float albumOpportunity = .5f) {
		if (!GenreMarketV2.Enabled || format is not (ReleaseFormat.Single or ReleaseFormat.Album)) return 1f;
		float secondaryWeight = primary == secondary ? 0f : .20f;
		float orientation = GenreCatalog.GetFormatOrientation(GenreCatalog.MapLegacy(primary, (int)MathF.Floor(year)), year) * (1f - secondaryWeight);
		if (secondaryWeight > 0f) orientation += GenreCatalog.GetFormatOrientation(GenreCatalog.MapLegacy(secondary, (int)MathF.Floor(year)), year) * secondaryWeight;
		float centered = (orientation - .5f) * 2f;
		float rawSingle = 1f + centered * FormatOrientationStrength;
		float rawAlbum = 1f - centered * FormatOrientationStrength;
		float albumWeight = Mathf.Clamp(albumOpportunity, 0f, 1f);
		float normalizer = (1f - albumWeight) * rawSingle + albumWeight * rawAlbum;
		return (format == ReleaseFormat.Single ? rawSingle : rawAlbum) / Mathf.Max(.000001f, normalizer);
	}

	/// <summary>
	/// Runtime format seam. Enabled configuration alone is insufficient: Directive 5
	/// begins on the first live tick, so prewarm must retain a neutral multiplier.
	/// The explicit live argument also makes the boundary independently probeable.
	/// </summary>
	public static float GetLiveFormatMultiplier(Genre primary, Genre secondary, ReleaseFormat format, float year,
		float albumOpportunity, bool live) =>
		live ? GetFormatMultiplier(primary, secondary, format, year, albumOpportunity) : 1f;

	/// <summary>
	/// Fixed-input correction for the nonlinear Single-transfer seam.  It preserves
	/// regional acceptance texture for radio/routing while restoring the protected
	/// population-weighted Single opportunity after the runtime transfer and format
	/// operations.  The cache key intentionally excludes live momentum and outcomes.
	/// </summary>
	internal static float GetLiveSpecialistSingleOpportunityNormalizer(Genre primary, Genre secondary, int year, bool live) {
		Genre canonicalPrimary = GenreCatalog.MapLegacy(primary, year);
		Genre canonicalSecondary = GenreCatalog.MapLegacy(secondary, year);
		if (!live || !IsSpecialist(canonicalPrimary) && !IsSpecialist(canonicalSecondary)) return 1f;
		var key = new SpecialistSingleNormalizerKey(canonicalPrimary, canonicalSecondary, year);
		if (SpecialistSingleNormalizerCache.TryGetValue(key, out float cached)) return cached;
		return SpecialistSingleNormalizerCache[key] = GetFixedInputSpecialistRoutingProbe(canonicalPrimary, canonicalSecondary, year).SingleOpportunityNormalizer;
	}

	internal static SpecialistRoutingProbe GetFixedInputSpecialistRoutingProbe(Genre genre, float year) =>
		GetFixedInputSpecialistRoutingProbe(genre, genre, year);

	internal static SpecialistRoutingProbe GetFixedInputSpecialistRoutingProbe(Genre primary, Genre secondary, float year) {
		MarketRegion[] regions = SpecialistBuyingPopulationPriors.Select(pair => {
			var region = new MarketRegion {
				regionId = pair.Key,
				population = pair.Value,
				youthPercentage = .25f,
				averageIncome = 1f,
				urbanization = .6f,
				blackPopulation = .15f,
				collegeCount = 12,
				culturalProgressivism = .5f,
				churchNetworkStrength = .25f,
				currentIntegration = .5f,
				media = new MediaInfrastructure { hasFMUnderground = true, radioReach = .5f }
			};
			region.segmentCapacities = SegmentCapacityModel.Create(region, (int)MathF.Floor(year));
			return region;
		}).ToArray();
		return GetPostRoutingSpecialistProbe(primary, secondary, regions, year);
	}

	private static SpecialistRoutingProbe GetPostRoutingSpecialistProbe(Genre primary, Genre secondary, IEnumerable<MarketRegion> regions, float year) {
		MarketRegion[] fixedRegions = regions.Where(region => region != null).ToArray();
		float globalSingleNormalization = GetLiveSingleOpportunityNormalization(fixedRegions, year, live: true);
		float effective = 0f, clampLoss = 0f, finalSingle = 0f;
		float protectedEffective = 0f, protectedClampLoss = 0f, protectedFinalSingle = 0f, totalPopulation = 0f;
		foreach (MarketRegion region in fixedRegions) {
			float buyingPopulation = region.population * 1000000f * region.GetBuyingPopulationPercentage();
			if (buyingPopulation <= 0f) continue;
			RegionalDemandAcceptanceComponents route = GetRegionalDemandAcceptanceComponents(primary, secondary, region, year, 0f,
				includeCenteredSpecialistTexture: true);
			RegionalDemandAcceptanceComponents protectedRoute = GetRegionalDemandAcceptanceComponents(primary, secondary, region, year, 0f,
				includeCenteredSpecialistTexture: false);
			float runtimeFormat = GetLiveFormatMultiplier(primary, secondary, ReleaseFormat.Single, year,
				region.GetMarketAlbumOpportunityWeight(year), live: true);
			float singleOpportunity = GetEnabledSingleDemandMultiplier(route.Effective) * globalSingleNormalization * runtimeFormat;
			float protectedSingleOpportunity = GetEnabledSingleDemandMultiplier(protectedRoute.Effective) * globalSingleNormalization * runtimeFormat;
			effective += buyingPopulation * route.Effective;
			clampLoss += buyingPopulation * route.ClampDelta;
			finalSingle += buyingPopulation * singleOpportunity;
			protectedEffective += buyingPopulation * protectedRoute.Effective;
			protectedClampLoss += buyingPopulation * protectedRoute.ClampDelta;
			protectedFinalSingle += buyingPopulation * protectedSingleOpportunity;
			totalPopulation += buyingPopulation;
		}
		if (totalPopulation <= 0f) return new SpecialistRoutingProbe(0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f);
		effective /= totalPopulation;
		clampLoss /= totalPopulation;
		finalSingle /= totalPopulation;
		protectedEffective /= totalPopulation;
		protectedClampLoss /= totalPopulation;
		protectedFinalSingle /= totalPopulation;
		float normalizer = protectedFinalSingle / Mathf.Max(.000001f, finalSingle);
		return new SpecialistRoutingProbe(effective, clampLoss, finalSingle, protectedEffective, protectedClampLoss,
			protectedFinalSingle, normalizer, finalSingle * normalizer);
	}

	private static bool IsSpecialist(Genre genre) => genre is Genre.Country or Genre.TexMex or Genre.Boogaloo;

	/// <summary>Fixed specialist membership shared by the stock-service seam; this is not a live demand signal.</summary>
	internal static bool IsSpecialistFulfillmentGenre(Genre genre) => IsSpecialist(genre);
	// Fixed 1960 buying-population priors, derived from the seven authored region
	// resources and their static purchasing-capacity inputs.  These are only used
	// to center a texture; they never read release, sales, stock, chart, or momentum
	// state.  Their absolute scale is immaterial, but the explicit values make the
	// national conservation invariant stable and independently probeable.
	private static readonly IReadOnlyDictionary<string, float> SpecialistBuyingPopulationPriors =
		new Dictionary<string, float>(StringComparer.Ordinal) {
			["eastcoast"] = .725120036f, ["greatlakes"] = .515502883f, ["greatplains"] = .192183251f,
			["deepsouth"] = .167874111f, ["southwest"] = .184606423f, ["rockies"] = .059087843f,
			["westcoast"] = .320871449f
		};

	private static float GetRegionalFactor(Genre genre, MarketRegion region, AudienceSegment segment, float year,
		bool includeCenteredSpecialistTexture = true) {
		float factor = 1f;
		if (includeCenteredSpecialistTexture) factor *= GetCenteredSpecialistTexture(genre, year, region?.regionId);
		if (genre == Genre.Gospel && segment == AudienceSegment.GospelChurch) factor *= .75f + region.churchNetworkStrength * .5f;
		if ((genre == Genre.RnB || genre == Genre.Soul || genre == Genre.Funk) && segment == AudienceSegment.MainstreamAM) factor *= .6f + region.currentIntegration * .4f;
		return factor;
	}

	/// <summary>
	/// Pure authored specialist texture.  The target multipliers encode only the
	/// desired regional order; the fixed buying-population priors center every
	/// affected genre/year to exactly one national opportunity.
	/// </summary>
	internal static float GetCenteredSpecialistTextureForProbe(Genre genre, float year, string regionId) =>
		GetCenteredSpecialistTexture(genre, year, regionId);

	private static float GetCenteredSpecialistTexture(Genre genre, float year, string regionId) {
		if (genre is not (Genre.Country or Genre.TexMex or Genre.Boogaloo)) return 1f;
		float target = GetAuthoredSpecialistTarget(genre, year, regionId);
		float weightedTarget = SpecialistBuyingPopulationPriors.Sum(pair =>
			pair.Value * GetAuthoredSpecialistTarget(genre, year, pair.Key));
		float totalPopulation = SpecialistBuyingPopulationPriors.Values.Sum();
		return weightedTarget > 0f ? target * totalPopulation / weightedTarget : 1f;
	}

	private static float GetAuthoredSpecialistTarget(Genre genre, float year, string regionId) {
		// The texture is deliberately time-invariant within the decade.  Year remains
		// an explicit fixed input so a future authored table can vary by historical
		// era without turning this helper into a realized-outcome normalizer.
		_ = year;
		return genre switch {
			Genre.Country => regionId switch {
				"southwest" => 1.55f, "deepsouth" => 1.30f, "greatplains" => 1.25f, _ => .80f
			},
			Genre.TexMex => regionId switch {
				"southwest" => 1.80f, "deepsouth" => 1.20f, "greatplains" => 1.15f, _ => .75f
			},
			Genre.Boogaloo => regionId == "eastcoast" ? 1.60f : .80f,
			_ => 1f
		};
	}

	private static GenreAcceptanceExplanation Evaluate(Genre canonical, GenreProfile profile, float weightedMean, MarketRegion region,
		AudienceSegment segment, float year, float legacyMomentum, bool includeCenteredSpecialistTexture = true) {
		float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
		float reach = profile.SegmentWeights.TryGetValue(segment.ToString(), out float weight) ? weight : 0f;
		float routing = Mathf.Max(.25f, 1f + (reach - weightedMean) * 1.25f);
		return new GenreAcceptanceExplanation(profile.GetBaseline(year), reach, capacity,
			GetRegionalFactor(canonical, region, segment, year, includeCenteredSpecialistTexture),
			legacyMomentum, GetLegacyMomentumContribution(legacyMomentum), routing);
	}

	private static float GetWeightedSegmentReach(GenreProfile profile, MarketRegion region) {
		float weightedMean = 0f;
		foreach (AudienceSegment candidate in SegmentCapacityModel.All) {
			float candidateCapacity = region.segmentCapacities?.Shares.TryGetValue(candidate, out float candidateShare) == true ? candidateShare : 0f;
			float candidateReach = profile.SegmentWeights.TryGetValue(candidate.ToString(), out float candidateWeight) ? candidateWeight : 0f;
			weightedMean += candidateCapacity * candidateReach;
		}
		return weightedMean;
	}
}
