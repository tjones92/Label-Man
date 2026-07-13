using System;
using System.Collections.Generic;
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

/// <summary>Single enabled Phase-2 acceptance owner. It intentionally consumes, but does not evolve, legacy momentum.</summary>
public static class GenreAcceptanceService {
	private const float DefaultLegacyMomentumInfluence = .3f;
	private const float SingleDemandLegacyIntercept = .60f;
	private const float SingleDemandLegacySlope = .50f;
	private readonly record struct RegionalDemandKey(Genre Primary, Genre Secondary, ulong RegionInstanceId, int YearBits, int MomentumBits);
	private static readonly Dictionary<RegionalDemandKey, float> RegionalDemandCache = new();
	private static int cacheYearBits = int.MinValue;

	public static GenreAcceptanceExplanation Evaluate(Genre genre, MarketRegion region, AudienceSegment segment, float year, float legacyMomentum) {
		Genre canonical = GenreCatalog.MapLegacy(genre, (int)MathF.Floor(year));
		GenreProfile profile = GenreCatalog.Get(canonical);
		float baseline = profile.GetBaseline(year);
		float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
		float reach = profile.SegmentWeights.TryGetValue(segment.ToString(), out float weight) ? weight : 0f;
		float regional = GetRegionalFactor(canonical, region, segment);
		float weightedMean = GetWeightedSegmentReach(profile, region);
		float routing = Mathf.Max(.25f, 1f + (reach - weightedMean) * 1.25f);
		return new GenreAcceptanceExplanation(baseline, reach, capacity, regional, legacyMomentum,
			GetLegacyMomentumContribution(legacyMomentum), routing);
	}

	/// <summary>Blends the normalized segment routes without multiplying population.</summary>
	public static float GetRegionalDemandAcceptance(Genre primary, Genre secondary, MarketRegion region, float year, float legacyMomentum = 0f) {
		int yearBits = BitConverter.SingleToInt32Bits(year);
		if (yearBits != cacheYearBits) {
			RegionalDemandCache.Clear();
			cacheYearBits = yearBits;
		}
		var key = new RegionalDemandKey(primary, secondary, region.GetInstanceId(), yearBits, BitConverter.SingleToInt32Bits(legacyMomentum));
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
			float acceptance = Evaluate(canonicalPrimary, primaryProfile, primaryMean, region, segment, year, legacyMomentum).Effective * primaryWeight;
			if (secondaryWeight > 0f) acceptance += Evaluate(canonicalSecondary, secondaryProfile, secondaryMean, region, segment, year, legacyMomentum).Effective * secondaryWeight;
			total += capacity * acceptance;
		}
		return RegionalDemandCache[key] = Mathf.Clamp(total, 0f, 1f);
	}

	/// <summary>Pure audit decomposition that exactly follows the blended regional acceptance calculation.</summary>
	public static RegionalDemandAcceptanceComponents GetRegionalDemandAcceptanceComponents(
		Genre primary, Genre secondary, MarketRegion region, float year, float legacyMomentum = 0f) {
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
			float primaryRegional = primaryBaseline * GetRegionalFactor(canonicalPrimary, region, segment);
			float primaryRouting = Mathf.Max(.25f, 1f + (primaryProfile.SegmentWeights.GetValueOrDefault(segment.ToString()) - primaryMean) * 1.25f);
			float primaryRouted = primaryRegional * primaryRouting;
			float primaryUnclamped = primaryRouted * (1f + momentumFactor);
			float primaryEffective = Mathf.Clamp(primaryUnclamped, 0f, 1f);
			float secondaryBaseline = 0f, secondaryRegional = 0f, secondaryRouted = 0f, secondaryUnclamped = 0f, secondaryEffective = 0f;
			if (secondaryProfile != null) {
				secondaryBaseline = secondaryProfile.GetBaseline(year);
				secondaryRegional = secondaryBaseline * GetRegionalFactor(canonicalSecondary, region, segment);
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
		float orientation = GenreCatalog.Get(GenreCatalog.MapLegacy(primary, (int)MathF.Floor(year))).SingleOrientation * (1f - secondaryWeight);
		if (secondaryWeight > 0f) orientation += GenreCatalog.Get(GenreCatalog.MapLegacy(secondary, (int)MathF.Floor(year))).SingleOrientation * secondaryWeight;
		float centered = (orientation - .5f) * 2f;
		const float tiltStrength = .22f;
		float rawSingle = 1f + centered * tiltStrength;
		float rawAlbum = 1f - centered * tiltStrength;
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
	private static float GetRegionalFactor(Genre genre, MarketRegion region, AudienceSegment segment) {
		float factor = 1f;
		if (genre == Genre.Country) factor *= region.regionId == "southwest" || region.regionId == "deepsouth" || region.regionId == "greatplains" ? 1.25f : .85f;
		if (genre == Genre.TexMex) factor *= region.regionId == "southwest" ? 1.55f :
			region.regionId == "deepsouth" || region.regionId == "greatplains" ? 1.25f : .85f;
		if (genre == Genre.Boogaloo) factor *= region.regionId == "eastcoast" ? 1.25f : .90f;
		if (genre == Genre.Gospel && segment == AudienceSegment.GospelChurch) factor *= .75f + region.churchNetworkStrength * .5f;
		if ((genre == Genre.RnB || genre == Genre.Soul || genre == Genre.Funk) && segment == AudienceSegment.MainstreamAM) factor *= .6f + region.currentIntegration * .4f;
		return factor;
	}

	private static GenreAcceptanceExplanation Evaluate(Genre canonical, GenreProfile profile, float weightedMean, MarketRegion region, AudienceSegment segment, float year, float legacyMomentum) {
		float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
		float reach = profile.SegmentWeights.TryGetValue(segment.ToString(), out float weight) ? weight : 0f;
		float routing = Mathf.Max(.25f, 1f + (reach - weightedMean) * 1.25f);
		return new GenreAcceptanceExplanation(profile.GetBaseline(year), reach, capacity, GetRegionalFactor(canonical, region, segment),
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
