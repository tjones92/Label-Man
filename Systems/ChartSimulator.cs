// Scripts/Systems/ChartSimulator.cs
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class ChartSimulator {
	
	// =======================================================================
	// CONFIGURATION - Tuned for 1960 Reality
	// =======================================================================
	
	private const float BASE_POTENTIAL_AUDIENCE = 12000000f;
	private const float BASE_AWARENESS_GROWTH = 0.012f; 
	private const float RADIO_AWARENESS_MULT = 0.18f;
	private const float WORD_OF_MOUTH_MULT = 0.14f;     
	private const float ARTIST_HEAT_AWARENESS_BONUS = 0.18f;
	private const float AWARENESS_DECAY_RATE = 0.95f;
	
	private const float RADIO_QUALITY_WEIGHT = 0.7f;    
	private const float RADIO_MOMENTUM_WEIGHT = 0.25f;
	private const float RADIO_LABEL_WEIGHT = 0.4f;
	private const float RADIO_FATIGUE_DECAY = 0.88f;
	
	private const float BASE_PURCHASE_RATE = 0.07f;
	private const float QUALITY_EXPONENT = 4.0f;
	private const float SATURATION_POWER = 0.45f;
	private const float DEMAND_AGE_DECAY_RATE = 0.91f;
	private const float LegacyMajorDemandScale = 0.60f;
	private const float LegacyMidTierDemandScale = 0.85f;
	
	private const float TOP_5_VISIBILITY_MULT = 4.5f;
	private const float TOP_10_VISIBILITY_MULT = 3.0f;
	private const float TOP_20_VISIBILITY_MULT = 2.0f;
	private const float TOP_40_VISIBILITY_MULT = 1.4f;
	private const float TOP_100_VISIBILITY_MULT = 1.0f;
	
	private const float WEEKLY_SALES_PER_RECORD_STORE = 250f;
	private const float WEEKLY_SALES_PER_DEPT_STORE = 500f;

	// Rack jobbers ran the record departments of department stores, discount chains and
	// supermarkets (handoff section 33.1 stage 2). They stocked narrow, high-turn inventory
	// -- the proven hits -- so the rack is an amplifier of a record that is already selling,
	// never a way to break an unproven one. Their share of retail grew across the decade at
	// the expense of the mom-and-pop record store.
	//
	// The authored departmentStoreCount is a 1960 baseline and stays intact: gating it on
	// proof instead cut every unproven record's shelf by ~79% and every 1960 record's by 60%,
	// which crowded the chart onto incumbents and dropped cumulative breadth below the
	// reference run. What a proven record earns is extra rack space on top of the authored
	// baseline, and the decade's shift toward rack retail scales that bonus rather than the
	// baseline (section 12: do not rewrite an accepted calibration to add a mechanism).
	private const float RACK_ERA_FLOOR = 0.30f;
	private const int RACK_ERA_START_YEAR = 1960;
	private const int RACK_ERA_FULL_YEAR = 1969;
	private const float RACK_MAX_SHELF_BONUS = 0.80f;
	/// <summary>
	/// A jobber restocking its own racks with a record that turns over is a real but partial
	/// substitute for the label being able to ship to that market itself. Lifting an uncovered
	/// record all the way to parity overstated it physically and, because the lift only reaches
	/// records that are already proven, amplified the biggest sellers on a hundred-slot chart
	/// and cost cumulative breadth.
	/// </summary>
	internal const float RackServiceShareOfDistributed = 0.50f;
	private const float RACK_REGIONAL_PROOF_FLOOR = 0.30f;
	private const float RACK_REGIONAL_PROOF_FULL = 0.55f;
	
	private const float HIT_MOMENTUM_BONUS = 0.3f;
	
	private const float BASE_INERTIA = 0.80f;       
	private const float INERTIA_QUALITY_OVERRIDE = 0.15f;
	private const float MIN_SALES_FOR_FULL_INERTIA = 8000f;
	
	private const float MOMENTUM_SMOOTHING = 0.22f;     
	private const float MOMENTUM_QUALITY_FLOOR = -0.12f;
	private const float MOMENTUM_CLAMP = 0.35f;
	
	// =======================================================================
	// MAIN UPDATE
	// =======================================================================
	
	public static void UpdateRecord(RecordRuntimeData record, AILabel label, float genreAcceptance, float artistHeat) {
		record.artistHeat = artistHeat;
		float quality = record.GetQuality();
		
		UpdateLabelPush(record, label);
		UpdateRadioHeat(record, label, quality, genreAcceptance);
		UpdateAwareness(record, quality);
		UpdateWordOfMouth(record, quality);
	}
	
	public static void FinalizeWeeklySales(RecordRuntimeData record, int totalSales) {
		record.unitsPreviousWeek = record.unitsThisWeek;
		record.unitsThisWeek = totalSales;
		record.totalUnitsSold += totalSales;
		UpdateMomentum(record);
	}
	
	// =======================================================================
	// REGIONAL SALES CALCULATION
	// =======================================================================
	
	public static int CalculateRegionalSales(
		RecordRuntimeData record, 
		MarketRegion region, 
		RegionalRecordData regionalData,
		float quality,
		float genreAcceptance,
		int year,
		int month,
		bool liveTick,
		int internalChartPosition,
		AILabel label,
		float singleOpportunityNormalization = 1f)
	{
		// === 1. POTENTIAL BUYERS ===
		float populationMillions = region.population;
		float buyingPercentage = region.GetBuyingPopulationPercentage();
		float potentialBuyers = populationMillions * 1000000f * buyingPercentage;
		
		// === 2. AWARENESS FILTER ===
		float effectiveAwareness = (record.awareness * 0.4f) + (regionalData.awareness * 0.6f);
		bool stagedLiveDemand = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		
		if (record.currentPosition > 0 && record.currentPosition <= 10) {
			effectiveAwareness = Mathf.Max(effectiveAwareness, 0.7f);
		} else if (record.currentPosition > 0 && record.currentPosition <= 40) {
			effectiveAwareness = Mathf.Max(effectiveAwareness, 0.4f);
		}
		regionalData.salesRecordAwarenessThisWeek = record.awareness;
		regionalData.salesRegionalAwarenessThisWeek = regionalData.awareness;
		regionalData.salesEffectiveAwarenessThisWeek = effectiveAwareness;
		regionalData.salesRadioHeatThisWeek = record.radioHeat;
		regionalData.salesRegionalRadioPlayThisWeek = regionalData.radioPlay;
		
		float baselineAwareness = Mathf.Clamp(effectiveAwareness, 0f, 1f);
		
		// === 3. MARKET EXHAUSTION ===
		float potentialAudience = GetRegionalPotentialAudience(record, region, quality);
		
		float regionalSold = regionalData.unitsSoldTotal;
		float penetration = regionalSold / Mathf.Max(1f, potentialAudience);
		
		float exhaustionFactor = 1f / (1f + Mathf.Pow(penetration * 3f, SATURATION_POWER));
		exhaustionFactor = Mathf.Max(exhaustionFactor, 0.08f);
		
		// === 4. DEMAND CURVE ===
		float demandCurve = Mathf.Pow(quality, QUALITY_EXPONENT);
		float conversionRate = BASE_PURCHASE_RATE * demandCurve * exhaustionFactor;
		// The high-volume label families dominate every measured sales window.
		// Keep indie-family conversion intact instead of applying another blanket
		// purchase-rate reduction that erases their narrow charting margin.
		if (stagedLiveDemand) conversionRate *= GetLiveLabelDemandScale(label, record.baseRecord?.recordId);
		else if (label?.tier == LabelTier.Major) conversionRate *= LegacyMajorDemandScale;
		else if (label?.tier == LabelTier.MidTier) conversionRate *= LegacyMidTierDemandScale;
		
		// === 5. CHART VISIBILITY BONUS ===
		float chartVisibility = GetChartVisibilityMultiplier(internalChartPosition);
		if (internalChartPosition <= 0) {
			// Proven local discovery softens, but never erases, the uncharted moat.
			// Even the strongest regional signal remains below #100's 1.0 exposure.
			float regionalDiscovery = Mathf.Clamp((regionalData.breakoutScore - 0.24f) / 0.40f, 0f, 1f);
			regionalDiscovery = Mathf.Max(regionalDiscovery, regionalData.neighboringMarketTestStrength * 0.60f);
			chartVisibility = 0.40f + regionalDiscovery * 0.55f;
		}
		regionalData.breakoutVisibilityMultiplier = chartVisibility;
		float chartSignal = Mathf.Max(.01f, chartVisibility);
		if (!stagedLiveDemand) conversionRate *= chartVisibility;
		
		// === 6. LAUNCH BOOST ===
		float launchBoost = 1.0f;
		if (record.weeksSinceRelease <= 1) {
			launchBoost = 2.0f + (record.currentLabelPush * 2.5f);
		} else if (record.weeksSinceRelease <= 2) {
			launchBoost = 1.5f + (record.currentLabelPush * 1.0f);
		} else if (record.weeksSinceRelease <= 3) {
			launchBoost = 1.2f + (record.currentLabelPush * 0.4f);
		}
		conversionRate *= launchBoost;
		
		// === 7. MOMENTUM BONUS ===
		float momentumBonus = 1f + Mathf.Clamp(record.momentum, -0.2f, 0.5f);
		if (!stagedLiveDemand) conversionRate *= momentumBonus;

		// Records eventually leave the active demand cycle even when chart
		// visibility keeps their effective awareness artificially high.
		if (record.weeksSinceRelease > 8) {
			int weeksOverThreshold = record.weeksSinceRelease - 8;
			conversionRate *= Mathf.Pow(DEMAND_AGE_DECAY_RATE, weeksOverThreshold);
		}
		
		// === 8. OTHER MODIFIERS ===
		bool useGenreMarketV2DemandTransfer = stagedLiveDemand;
		if (useGenreMarketV2DemandTransfer) {
			conversionRate *= GenreAcceptanceService.GetEnabledSingleDemandMultiplier(genreAcceptance);
			if (singleOpportunityNormalization != 1f) conversionRate *= singleOpportunityNormalization;
		} else conversionRate *= 0.6f + genreAcceptance * 0.5f;
		conversionRate *= GenreAcceptanceService.GetLiveFormatMultiplier(record.baseRecord.primaryGenre,
			record.baseRecord.secondaryGenre, ReleaseFormat.Single, year,
			region.GetAlbumOpportunityWeight(record.baseRecord.primaryGenre, year, useGenreMarketV2DemandTransfer),
			useGenreMarketV2DemandTransfer);
		if (useGenreMarketV2DemandTransfer) conversionRate *= GenreAcceptanceService.GetLiveSpecialistSingleOpportunityNormalizer(
			record.baseRecord.primaryGenre, record.baseRecord.secondaryGenre, year, live: true);
		if (!stagedLiveDemand) conversionRate *= 0.75f + record.radioHeat * 0.5f;
		conversionRate *= 0.75f + Mathf.Max(0, regionalData.sentiment) * 0.25f;
		conversionRate *= record.GetAwardMultiplier();
		conversionRate *= 1f - (region.distribution.difficulty * 0.3f);
		conversionRate *= MarketSeasonality.GetSingleSalesMultiplier(year, month, liveTick);
		
		// The enabled staged model requires a bounded baseline. The disabled branch
		// retains its historical un-clamped awareness value and rounding contract.
		float awareBuyers = potentialBuyers * (stagedLiveDemand ? baselineAwareness : effectiveAwareness);
		if (stagedLiveDemand) {
			SingleDemandStages stages = CalculateSingleDemandStages(potentialBuyers, baselineAwareness, chartSignal,
				Mathf.Max(.01f, momentumBonus), Mathf.Max(.01f, .75f + record.radioHeat * .5f), demandCurve,
				genreAcceptance, GenreAcceptanceService.GetLiveFormatMultiplier(record.baseRecord.primaryGenre,
					record.baseRecord.secondaryGenre, ReleaseFormat.Single, year,
					region.GetEnabledAlbumOpportunityWeight(record.baseRecord.primaryGenre, year), true),
				conversionRate / Mathf.Max(.000001f, BASE_PURCHASE_RATE * demandCurve));
			awareBuyers = stages.AwareBuyers;
			conversionRate = stages.IntrinsicConversionRate;
			regionalData.demandPotentialAudience = stages.PotentialAudience;
			regionalData.demandBaselineAwareness = stages.BaselineAwareness;
			regionalData.demandEarnedDiscoveryExposure = stages.EarnedDiscoveryExposure;
			regionalData.demandAwareBuyers = stages.AwareBuyers;
			regionalData.demandIntrinsicQualityFactor = stages.IntrinsicQualityFactor;
			regionalData.demandAcceptanceFactor = stages.AcceptanceFactor;
			regionalData.demandFormatFactor = stages.FormatFactor;
			regionalData.demandIntrinsicConversionRate = stages.IntrinsicConversionRate;
			regionalData.demandChartSignal = chartSignal;
			regionalData.demandMomentumSignal = Mathf.Max(.01f, momentumBonus);
			regionalData.demandRadioSignal = Mathf.Max(.01f, .75f + record.radioHeat * .5f);
		}
		float rawSales = awareBuyers * conversionRate;
		// Backorders represent recent unmet intent, not a permanent bank of future
		// purchases. Most stale intent expires before this week's demand is added.
		regionalData.unitsBackordered = Mathf.RoundToInt(regionalData.unitsBackordered * 0.35f);
		regionalData.rawDemandThisWeek = rawSales;
		bool captureBreakoutDiagnostic = !record.baseRecord.isPlayerOwned &&
			record.weeksSinceRelease >= 1 &&
			record.weeksSinceRelease <= 14;
		if (captureBreakoutDiagnostic) {
			regionalData.breakoutDiagnosticAge = record.weeksSinceRelease;
			regionalData.breakoutWeekStartStock = regionalData.unitsInStores;
			regionalData.breakoutRawSales = rawSales;
			regionalData.breakoutAwareBuyers = awareBuyers;
			regionalData.breakoutConversionRate = conversionRate;
		}
		
		// === 9. SUPPLY CONSTRAINTS ===
		float storeCapacity = region.distribution.recordStoreCount * WEEKLY_SALES_PER_RECORD_STORE;
		// Department-store shelf: the authored baseline, plus rack shelf a proven record earns
		// in a market its label cannot ship to itself. Applying that bonus to every proven
		// record instead simply amplified the biggest sellers, and on a hundred-slot chart an
		// amplifier is zero-sum -- it pushed marginal independents off and took cumulative
		// breadth back to the reference run. Where the label already has a network the rack is
		// part of the authored baseline; where it has none, the jobber buying a record that
		// turns over is the only way onto that market's shelves.
		bool labelShipsHere = label.HasDistributionInRegionForRecord(region.regionId, record.baseRecord?.recordId);
		float rackShelf = labelShipsHere ? 1f : GetRackJobberShelfMultiplier(record.currentPosition,
			regionalData?.peakBreakoutScore ?? 0f, TimeManager.Instance?.CurrentDate.year ?? RACK_ERA_START_YEAR);
		float deptCapacity = region.distribution.departmentStoreCount * WEEKLY_SALES_PER_DEPT_STORE * rackShelf;
		float totalCapacity = (storeCapacity + deptCapacity) * region.distribution.inventoryDepth;

		if (record.currentPosition > 0 && record.currentPosition <= 20) {
			totalCapacity *= 1.5f;
		}

		// A former indie-distribution penalty stood here. It tested
		// "!hasIndieDistribution && !hasOneStopDistributors", but every authored region has
		// one-stops, so the branch was unreachable in every run this model has ever done --
		// and it keyed off labelId being non-null rather than off the label being an
		// independent, so it would have charged majors identically had it fired. Access for a
		// label without its own network in a region is now carried by the coverage model and
		// by the rack channel above.

		if (regionalData.unitsInStores < rawSales) {
			regionalData.unitsBackordered += Mathf.RoundToInt(rawSales - regionalData.unitsInStores);
			rawSales = regionalData.unitsInStores;
		}
		if (captureBreakoutDiagnostic) {
			regionalData.breakoutBackordersBeforeRestock = regionalData.unitsBackordered;
		}
		
		rawSales = Mathf.Min(rawSales, totalCapacity);
		rawSales *= (float)GD.RandRange(0.96, 1.04);
		if (!(GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)) {
			// Frozen disabled/prewarm behavior, including the historical post-jitter
			// rounding semantics, is intentionally retained unchanged.
			return Mathf.Max(0, Mathf.RoundToInt(rawSales));
		}
		regionalData.storeCapacityThisWeek = Mathf.Max(0, Mathf.FloorToInt(totalCapacity));
		// Jitter is deliberately drawn in the legacy order.  The live caller may
		// subsequently ration this serviceable intent against the common market.
		regionalData.serviceableIntentThisWeek = Mathf.Clamp(Mathf.RoundToInt(rawSales), 0,
			Mathf.Min(regionalData.unitsInStores, regionalData.storeCapacityThisWeek));
		return regionalData.serviceableIntentThisWeek;
	}
		
	/// <summary>Pure enabled Single demand stages; discovery is owned only here.</summary>
	internal static SingleDemandStages CalculateSingleDemandStages(float potentialAudience, float baselineAwareness,
		float chartSignal, float momentumSignal, float radioSignal, float intrinsicQualityFactor,
		float acceptanceFactor, float formatFactor, float otherConversionFactor) {
		float boundedBase = Mathf.Clamp(baselineAwareness, 0f, 1f);
		// Move the historical discovery multipliers into awareness odds exactly once.
		// Chart, momentum, and radio are correlated views of the same discovery event,
		// so use their geometric mean instead of compounding all three as independent
		// multipliers. One stays neutral, equally weak/strong signals retain their
		// level, and a second or third signal cannot multiply the audience again.
		float discoveryProduct = Mathf.Max(.000001f, chartSignal) *
			Mathf.Max(.000001f, momentumSignal) * Mathf.Max(.000001f, radioSignal);
		float discoveryMultiplier = Mathf.Pow(discoveryProduct, 1f / 3f);
		float awareFraction = boundedBase <= 0f ? 0f : boundedBase >= 1f ? 1f :
			boundedBase * discoveryMultiplier / (1f - boundedBase + boundedBase * discoveryMultiplier);
		float exposure = awareFraction > boundedBase
			? (awareFraction - boundedBase) / Mathf.Max(.000001f, 1f - boundedBase)
			: 0f;
		float conversion = BASE_PURCHASE_RATE * Mathf.Max(0f, intrinsicQualityFactor) * Mathf.Max(0f, otherConversionFactor);
		return new SingleDemandStages(Mathf.Max(0f, potentialAudience), boundedBase, exposure,
			Mathf.Max(0f, potentialAudience) * Mathf.Clamp(awareFraction, 0f, 1f), intrinsicQualityFactor,
			acceptanceFactor, formatFactor, conversion);
	}

	/// <summary>
	/// Realized Single demand follows the distribution capabilities a label has now,
	/// rather than the tier it had when it was generated. The former fixed switch
	/// continued to charge Independent labels 0.55 after they built national reach
	/// while granting Boutique labels 1.20 regardless of reach. That made promotion,
	/// self-built expansion, and completed distribution deals largely cosmetic at
	/// the chart-access seam. This continuous scale retains the calibrated national
	/// label ceiling while allowing earned reach to change outcomes.
	/// </summary>
	internal static float GetLiveLabelDemandScale(AILabel label) =>
		label == null ? 1f : CalculateLiveLabelDemandScale(label.distributionStrength, label.effectiveNationalReach);

	/// <summary>
	/// Demand scale for one release. A distribution deal carries the record that
	/// earned it and the label's subsequent output, so a record outside the contract
	/// sells on the label's own reach rather than the distributor's borrowed network.
	/// </summary>
	internal static float GetLiveLabelDemandScale(AILabel label, string recordId) =>
		label == null ? 1f : CalculateLiveLabelDemandScale(
			label.DistributionStrengthForRecord(recordId), label.EffectiveNationalReachForRecord(recordId));

	internal static float CalculateLiveLabelDemandScale(float distributionStrength, float nationalReach) =>
		Mathf.Clamp(0.45f + Mathf.Clamp(distributionStrength, 0f, 1f) * 0.55f +
			Mathf.Clamp(nationalReach, 0f, 1f) * 0.35f, 0.55f, 1.20f);

	private static float GetGenreMarketReach(Genre genre) {
		return genre switch {
			Genre.TraditionalPop => 0.95f,
			Genre.RockAndRoll => 0.85f,
			Genre.Soul => 0.70f,
			Genre.RnB => 0.65f,
			Genre.TeenPop => 0.75f,
			Genre.DooWop => 0.60f,
			Genre.Country => 0.50f,
			Genre.Gospel => 0.35f,
			Genre.Jazz => 0.40f,
			Genre.Folk => 0.45f,
			Genre.BritishInvasion => 0.80f,
			Genre.Psychedelic => 0.50f,
			Genre.SurfRock => 0.55f,
			_ => 0.60f
		};
	}

	private static float GetChartVisibilityMultiplier(int position) {
		if (position <= 0) return 0.4f;
		if (position <= 5) return TOP_5_VISIBILITY_MULT;
		if (position <= 10) return TOP_10_VISIBILITY_MULT;
		if (position <= 20) return TOP_20_VISIBILITY_MULT;
		if (position <= 40) return TOP_40_VISIBILITY_MULT;
		return TOP_100_VISIBILITY_MULT;
	}

	// Returns the furthest position an established record may fall this week.
	// Low-quality novelty records receive less protection; quality itself never adds
	// protection beyond BASE_INERTIA. Weak sales and sustained decline remove it.
	public static int GetInertiaPositionCap(RecordRuntimeData record, int previousPosition, int rawPosition) {
		if (previousPosition <= 0 || rawPosition <= previousPosition) return rawPosition;
		if (record.unitsThisWeek <= 0 || record.weeksNegative >= 3 || record.momentum <= -0.20f) return rawPosition;

		float salesGate = Mathf.Clamp(record.unitsThisWeek / MIN_SALES_FOR_FULL_INERTIA, 0f, 1f);
		float quality = record.GetQuality();
		float qualityAdjustment = (1f - quality) * INERTIA_QUALITY_OVERRIDE;
		float inertia = Mathf.Max(0f, BASE_INERTIA - qualityAdjustment) * salesGate;

		if (previousPosition <= 40 && record.momentum > 0f) {
			inertia = Mathf.Min(BASE_INERTIA, inertia + record.momentum * HIT_MOMENTUM_BONUS * salesGate);
		}

		int rawDrop = rawPosition - previousPosition;
		int allowedDrop = Mathf.Max(1, Mathf.CeilToInt(rawDrop * (1f - inertia)));
		return previousPosition + allowedDrop;
	}
		
	// =======================================================================
	// RADIO HEAT
	// =======================================================================
		
	private static void UpdateRadioHeat(RecordRuntimeData record, AILabel label, float quality, float genreAcceptance) {
		float qualityFactor = Mathf.Pow(quality, 1.8f) * RADIO_QUALITY_WEIGHT; 
		float pushFactor = record.currentLabelPush * RADIO_LABEL_WEIGHT;
		float momentumFactor = Mathf.Max(0, record.momentum) * RADIO_MOMENTUM_WEIGHT;
		
		float targetHeat = (qualityFactor + pushFactor + momentumFactor) * genreAcceptance;
		targetHeat += record.artistHeat * 0.12f;
		
		if (record.currentPosition > 0 && record.currentPosition <= 10) {
			targetHeat += 0.25f;
		} else if (record.currentPosition > 0 && record.currentPosition <= 40) {
			targetHeat += 0.1f;
		}
		
		if (record.weeksSinceRelease > 8) {
			int weeksOverThreshold = record.weeksSinceRelease - 8;
			float fatigue = Mathf.Pow(RADIO_FATIGUE_DECAY, weeksOverThreshold);
			targetHeat *= fatigue;
		}

		float lerpRate = (targetHeat > record.radioHeat) ? 0.28f : 
						(record.weeksSinceRelease > 12) ? 0.22f : 0.10f;
		
		record.radioHeat = Mathf.Lerp(record.radioHeat, targetHeat, lerpRate);
		record.radioHeat = Mathf.Clamp(record.radioHeat, 0f, 1f);
	}
	
	public static float GetRadioDifficulty(MarketRegion region) {
		// Godot Mathf lacks Log10, so we use natural Log divided by Log(10)
		float log10 = Mathf.Log(region.media.totalRadioStations + 1) / Mathf.Log(10);
		float log16 = Mathf.Log(16) / Mathf.Log(10);
		
		float difficulty = log10 / log16;
		return Mathf.Clamp(difficulty, 0.3f, 2.5f);
	}
	
	// =======================================================================
	// LABEL PUSH
	// =======================================================================
	
	public static float GetCampaignImpact(AILabel label) {
		if (label == null) return 0.02f;
		// Budget sustains and broadens campaigns; marketing controls spend efficiency.
		// Distribution is deliberately absent: it fulfills demand rather than creating it.
		float spendCapacity = 0.45f + (label.budgetLevel * 0.55f);
		return Mathf.Clamp(label.marketingPower * spendCapacity, 0f, 1f);
	}

	/// <summary>
	/// How much of a market's rack shelf a record can claim. A national top-40 hit is fully
	/// racked; one charting below that is partially racked; one proven only in this region is
	/// racked by the jobber servicing it, which is how a regional hit reached mainstream
	/// retail with no major-label deal at all. An unproven record gets no rack space.
	/// </summary>
	internal static float GetRackJobberAccess(int chartPosition, float regionalBreakoutPeak) {
		float national = chartPosition >= 1 && chartPosition <= 40 ? 1f
			: chartPosition >= 1 && chartPosition <= 100 ? 0.55f
			: 0f;
		float regional = Mathf.Clamp(
			(regionalBreakoutPeak - RACK_REGIONAL_PROOF_FLOOR) / (RACK_REGIONAL_PROOF_FULL - RACK_REGIONAL_PROOF_FLOOR),
			0f, 1f) * 0.70f;
		return Mathf.Clamp(Mathf.Max(national, regional), 0f, 1f);
	}

	/// <summary>
	/// Weight of the rack channel by year. Rack jobbing and discount retail expanded through
	/// the 1960s while mom-and-pop record stores contracted, so the same department-store
	/// shelf is worth progressively more across the decade.
	/// </summary>
	internal static float GetRackJobberEraWeight(int year) => Mathf.Lerp(RACK_ERA_FLOOR, 1f,
		Mathf.Clamp(
			(year - RACK_ERA_START_YEAR) / (float)(RACK_ERA_FULL_YEAR - RACK_ERA_START_YEAR), 0f, 1f));

	/// <summary>
	/// Department-store shelf a record commands, as a multiple of the authored 1960 baseline.
	/// Never below 1: the rack channel adds shelf for a record that has proven it turns over,
	/// and cannot take shelf away from one that has not.
	/// </summary>
	internal static float GetRackJobberShelfMultiplier(int chartPosition, float regionalBreakoutPeak, int year) =>
		1f + (GetRackJobberAccess(chartPosition, regionalBreakoutPeak) *
			GetRackJobberEraWeight(year) * RACK_MAX_SHELF_BONUS);

	public static float GetRegionalLaunchFactor(AILabel label, string regionId, string recordId = null) {
		if (label == null) return 1f;
		bool strong = label.strongRegions?.Contains(regionId) ?? false;
		bool covered = label.HasDistributionInRegionForRecord(regionId, recordId);
		float reach = label.EffectiveNationalReachForRecord(recordId);
		if (strong) return 1.35f;
		if (covered) return 0.55f + (reach * 0.45f);
		return 0.12f + (reach * 0.18f);
	}

	public static int CalculateInitialRegionalStock(AILabel label, string regionId, float careerScale, float perceivedQualityMultiplier, string recordId = null) {
		if (label == null) return 0;
		bool strong = label.strongRegions?.Contains(regionId) ?? false;
		bool covered = label.HasDistributionInRegionForRecord(regionId, recordId);
		bool isHome = !string.IsNullOrEmpty(label.homeRegion) && label.homeRegion == regionId;
		float reachForRecord = label.DistributionStrengthForRecord(recordId);
		float access = covered ? 1f : 0.18f;
		float localDepth = isHome || strong
			? 0.25f + (reachForRecord * 0.75f)
			: 0.10f + (reachForRecord * 0.75f);
		float strongDepth = strong ? 1.45f : 1f;
		float noise = (float)GD.RandRange(0.85, 1.15);
		// DISTANCE-4B: neutral in 4a; 4b turns regional reach into real stock friction.
		float reachFactor = DistanceModel.GetEffectiveReach(label, DistanceModel.GetHubCityIdForRegion(regionId));
		int raw = Mathf.RoundToInt(10000f * access * localDepth * strongDepth * careerScale * perceivedQualityMultiplier * noise * reachFactor);
		int floor = isHome || strong ? 100 : 0;
		return Mathf.Max(floor, raw);
	}

	/// <summary>
	/// Redistributes already-drawn per-region stock. Callers use this after their
	/// established launch loop so disabled execution retains its exact RNG order.
	/// </summary>
	public static IReadOnlyDictionary<string, int> RedistributeInitialRegionalStockAllocation(Genre primaryGenre, int year,
		bool live, IEnumerable<MarketRegion> regions, IReadOnlyDictionary<string, int> baselineStock) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		int[] baseline = regionArray.Select(region => baselineStock?.GetValueOrDefault(region.regionId) ?? 0).ToArray();
		int[] allocated = AllocateSpecialistInitialStock(primaryGenre, year, live,
			regionArray.Select(region => region.regionId).ToArray(), baseline);
		var result = new Dictionary<string, int>(regionArray.Length, System.StringComparer.Ordinal);
		for (int i = 0; i < regionArray.Length; i++) result[regionArray[i].regionId] = allocated[i];
		return result;
	}

	internal static int[] AllocateSpecialistInitialStockForProbe(Genre primaryGenre, int year, bool live,
		IReadOnlyList<string> regionIds, IReadOnlyList<int> baselineStock) =>
		AllocateSpecialistInitialStock(primaryGenre, year, live, regionIds, baselineStock);

	private static int[] AllocateSpecialistInitialStock(Genre primaryGenre, int year, bool live,
		IReadOnlyList<string> regionIds, IReadOnlyList<int> baselineStock) {
		int count = System.Math.Min(regionIds?.Count ?? 0, baselineStock?.Count ?? 0);
		var unchanged = Enumerable.Range(0, count).Select(index => System.Math.Max(0, baselineStock[index])).ToArray();
		Genre canonical = GenreCatalog.MapLegacy(primaryGenre, year);
		if (!live || !GenreAcceptanceService.IsSpecialistFulfillmentGenre(canonical) || count == 0) return unchanged;

		int nationalBudget = unchanged.Sum();
		if (nationalBudget <= 0) return unchanged;
		var weighted = new float[count];
		float weightedTotal = 0f;
		for (int i = 0; i < count; i++) {
			weighted[i] = unchanged[i] * GenreAcceptanceService.GetCenteredSpecialistTextureForProbe(canonical, year, regionIds[i]);
			weightedTotal += weighted[i];
		}
		if (weightedTotal <= 0f) return unchanged;

		var allocated = new int[count];
		var remainders = new float[count];
		int assigned = 0;
		for (int i = 0; i < count; i++) {
			float exact = nationalBudget * weighted[i] / weightedTotal;
			allocated[i] = Mathf.FloorToInt(exact);
			remainders[i] = exact - allocated[i];
			assigned += allocated[i];
		}
		foreach (int index in Enumerable.Range(0, count).OrderByDescending(index => remainders[index]).ThenBy(index => index)) {
			if (assigned >= nationalBudget) break;
			allocated[index]++;
			assigned++;
		}
		return allocated;
	}

	private static void UpdateLabelPush(RecordRuntimeData record, AILabel label) {
		if (label == null) {
			record.currentLabelPush = 0.02f;
			return;
		}
		
		float basePush = GetCampaignImpact(label);
		
		float weekFactor = record.weeksSinceRelease switch {
			0 or 1 => 1.0f,
			2 or 3 => 0.9f,
			4 or 5 => 0.6f,
			6 or 7 => 0.3f,
			_ => 0.1f
		};
		
		if (record.currentPosition > 0 && record.currentPosition <= 20) {
			weekFactor = Mathf.Max(weekFactor, 0.85f);
		} else if (record.momentum > 0.15f && record.weeksSinceRelease < 14) {
			weekFactor = Mathf.Max(weekFactor, 0.7f);
		}
		
		record.currentLabelPush = basePush * weekFactor;
		record.totalLabelInvestment += record.currentLabelPush;
	}
	
	// =======================================================================
	// AWARENESS
	// =======================================================================
	
	private static void UpdateAwareness(RecordRuntimeData record, float quality) {
		if (record.weeksSinceRelease <= 1 && record.awareness < 0.02f) {
			float initialAwareness = record.artistHeat * ARTIST_HEAT_AWARENESS_BONUS;
			initialAwareness += 0.04f;
			record.awareness = Mathf.Max(record.awareness, initialAwareness);
		}
		
		float radioGrowth = record.radioHeat * RADIO_AWARENESS_MULT;
		
		float womEffectiveness = Mathf.Max(0, (quality - 0.45f) * 2.2f); 
		float womGrowth = record.wordOfMouth * WORD_OF_MOUTH_MULT * womEffectiveness;
		
		float chartVisibility = 0f;
		if (record.currentPosition > 0) {
			if (record.currentPosition <= 5) chartVisibility = 0.12f;
			else if (record.currentPosition <= 10) chartVisibility = 0.08f;
			else if (record.currentPosition <= 20) chartVisibility = 0.05f;
			else if (record.currentPosition <= 40) chartVisibility = 0.025f;
			else {
				float normalizedRank = (101f - record.currentPosition) / 100f;
				chartVisibility = Mathf.Pow(normalizedRank, 3f) * 0.02f;
			}
		}
		
		float organicGrowth = BASE_AWARENESS_GROWTH * quality;
		float growthRoom = 1f - record.awareness;
		
		float totalGrowth = (radioGrowth + womGrowth + chartVisibility + organicGrowth) * growthRoom;
		record.awareness = Mathf.Clamp(record.awareness + totalGrowth, 0f, 1f);

		record.awareness = ApplyWeeklyAwarenessAgeDecay(record.awareness, record.weeksSinceRelease);
	}

	// Awareness is mutable stock, so the post-peak rate is applied once per
	// elapsed week. Raising the rate to the record's age and then applying that
	// increasingly large factor to last week's already-decayed stock produced a
	// triangular exponent: by age 18 the stock had received .95^55 instead of
	// .95^10. That erased the slow regional-to-national breakouts this system is
	// intended to model.
	internal static float ApplyWeeklyAwarenessAgeDecay(float awareness, int weeksSinceRelease) =>
		weeksSinceRelease > 8
			? Mathf.Max(0f, awareness) * AWARENESS_DECAY_RATE
			: Mathf.Max(0f, awareness);
	
	// =======================================================================
	// WORD OF MOUTH
	// =======================================================================
	
	private static void UpdateWordOfMouth(RecordRuntimeData record, float quality) {
		float qualityWOM = Mathf.Pow(quality, 2.2f) * 0.55f;
		
		float chartWOM = 0f;
		if (record.currentPosition > 0 && record.currentPosition <= 40) {
			chartWOM = (40f - record.currentPosition) / 40f * 0.35f;
		}
		
		float momentumFactor = record.momentum * 0.45f; 
		
		float targetWOM = Mathf.Max(0f, qualityWOM + chartWOM + momentumFactor);
		record.wordOfMouth = Mathf.Lerp(record.wordOfMouth, targetWOM, 0.22f);
	}
	
	// =======================================================================
	// SATURATION
	// =======================================================================
	
	public static void UpdateSaturation(RecordRuntimeData record, MarketRegion[] regions) {
		float weightedPenetration = 0f;
		float totalPotentialAudience = 0f;
		float quality = record.GetQuality();

		foreach (var region in regions) {
			if (!record.regionalData.TryGetValue(region.regionId, out var regionalData)) continue;

			float potentialAudience = GetRegionalPotentialAudience(record, region, quality);
			float penetration = regionalData.unitsSoldTotal / Mathf.Max(1f, potentialAudience);
			weightedPenetration += penetration * potentialAudience;
			totalPotentialAudience += potentialAudience;
		}

		record.saturation = totalPotentialAudience > 0f
			? weightedPenetration / totalPotentialAudience
			: 0f;
	}

	private static float GetRegionalPotentialAudience(RecordRuntimeData record, MarketRegion region, float quality) {
		float qualityAppeal = 0.3f + (quality * 0.7f);
		float genreReach = GetGenreMarketReach(record.baseRecord.primaryGenre);
		return BASE_POTENTIAL_AUDIENCE * qualityAppeal * genreReach * (region.population / 50f);
	}
	
	// =======================================================================
	// MOMENTUM
	// =======================================================================
	
	private static void UpdateMomentum(RecordRuntimeData record) {
		float salesChange = 0f;
		
		if (record.unitsPreviousWeek > 100) {
			salesChange = (float)(record.unitsThisWeek - record.unitsPreviousWeek) / record.unitsPreviousWeek;
			salesChange = Mathf.Clamp(salesChange, -MOMENTUM_CLAMP, MOMENTUM_CLAMP); 
		} else if (record.unitsThisWeek > 500) {
			salesChange = 0.4f;
		} else if (record.unitsThisWeek > 100) {
			salesChange = 0.2f;
		}
		
		float quality = record.GetQuality();
		float momentumFloor = MOMENTUM_QUALITY_FLOOR * (1.4f - quality);
		float targetMomentum = Mathf.Max(salesChange, momentumFloor);
		
		record.momentum = Mathf.Lerp(record.momentum, targetMomentum, MOMENTUM_SMOOTHING);
		
		if (record.momentum > record.peakMomentum) {
			record.peakMomentum = record.momentum;
		}
		
		if (record.momentum > 0.02f) {
			record.weeksPositive++;
			record.weeksNegative = 0;
		} else if (record.momentum < -0.02f) {
			record.weeksNegative++;
			record.weeksPositive = 0;
		}
	}
	
	// =======================================================================
	// CHART POINTS
	// =======================================================================
	
	// Changed List<MarketRegion> to MarketRegion[] to match ChartManager
	public static float CalculateChartPoints(RecordRuntimeData record, MarketRegion[] regions) {
		float salesPoints = record.unitsThisWeek;
		
		float airplayPoints = 0f;
		foreach (var region in regions) {
			if (!record.regionalData.ContainsKey(region.regionId)) continue;
			var data = record.regionalData[region.regionId];
			
			if (region.media != null) {
				airplayPoints += data.radioPlay * region.media.radioReach * region.population * 25f;
			}
		}
		
		return salesPoints + (airplayPoints * 0.15f);
	}
	
	// =======================================================================
	// STUDIO QUALITY
	// =======================================================================
	
	public static float GetStudioQualityModifier(MarketRegion recordingRegion) {
		if (recordingRegion?.musicIndustry == null) {
			return 0.7f;
		}
		
		var infra = recordingRegion.musicIndustry;
		
		float modifier = 0.55f + (infra.studioQuality * 0.45f);
		float studioBonus = Mathf.Min(infra.recordingStudioCount * 0.015f, 0.15f);
		float signatureBonus = infra.hasSignatureSound ? 0.08f : 0f;
		float majorBonus = infra.hasMajorLabelPresence ? 0.05f : 0f;
		
		return Mathf.Clamp(modifier + studioBonus + signatureBonus + majorBonus, 0.5f, 1.15f);
	}
}

public readonly struct SingleDemandStages {
	public readonly float PotentialAudience, BaselineAwareness, EarnedDiscoveryExposure, AwareBuyers;
	public readonly float IntrinsicQualityFactor, AcceptanceFactor, FormatFactor, IntrinsicConversionRate;
	public SingleDemandStages(float potentialAudience, float baselineAwareness, float earnedDiscoveryExposure, float awareBuyers,
		float intrinsicQualityFactor, float acceptanceFactor, float formatFactor, float intrinsicConversionRate) {
		PotentialAudience = potentialAudience; BaselineAwareness = baselineAwareness; EarnedDiscoveryExposure = earnedDiscoveryExposure;
		AwareBuyers = awareBuyers; IntrinsicQualityFactor = intrinsicQualityFactor; AcceptanceFactor = acceptanceFactor;
		FormatFactor = formatFactor; IntrinsicConversionRate = intrinsicConversionRate;
	}
}
