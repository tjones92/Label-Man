// Scripts/Systems/ChartSimulator.cs
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
	private const float MAJOR_DEMAND_SCALE = 0.60f;
	private const float MID_TIER_DEMAND_SCALE = 0.85f;
	
	private const float TOP_5_VISIBILITY_MULT = 4.5f;
	private const float TOP_10_VISIBILITY_MULT = 3.0f;
	private const float TOP_20_VISIBILITY_MULT = 2.0f;
	private const float TOP_40_VISIBILITY_MULT = 1.4f;
	private const float TOP_100_VISIBILITY_MULT = 1.0f;
	
	private const float WEEKLY_SALES_PER_RECORD_STORE = 250f;
	private const float WEEKLY_SALES_PER_DEPT_STORE = 500f;
	private const float INDIE_DISTRIBUTION_PENALTY = 0.65f;
	
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
		int month,
		int internalChartPosition,
		AILabel label)
	{
		// === 1. POTENTIAL BUYERS ===
		float populationMillions = region.population;
		float buyingPercentage = region.GetBuyingPopulationPercentage();
		float potentialBuyers = populationMillions * 1000000f * buyingPercentage;
		
		// === 2. AWARENESS FILTER ===
		float effectiveAwareness = (record.awareness * 0.4f) + (regionalData.awareness * 0.6f);
		
		if (record.currentPosition > 0 && record.currentPosition <= 10) {
			effectiveAwareness = Mathf.Max(effectiveAwareness, 0.7f);
		} else if (record.currentPosition > 0 && record.currentPosition <= 40) {
			effectiveAwareness = Mathf.Max(effectiveAwareness, 0.4f);
		}
		
		float awareBuyers = potentialBuyers * effectiveAwareness;
		
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
		if (label?.tier == LabelTier.Major) conversionRate *= MAJOR_DEMAND_SCALE;
		else if (label?.tier == LabelTier.MidTier) conversionRate *= MID_TIER_DEMAND_SCALE;
		
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
		conversionRate *= chartVisibility;
		
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
		conversionRate *= momentumBonus;

		// Records eventually leave the active demand cycle even when chart
		// visibility keeps their effective awareness artificially high.
		if (record.weeksSinceRelease > 8) {
			int weeksOverThreshold = record.weeksSinceRelease - 8;
			conversionRate *= Mathf.Pow(DEMAND_AGE_DECAY_RATE, weeksOverThreshold);
		}
		
		// === 8. OTHER MODIFIERS ===
		conversionRate *= 0.6f + genreAcceptance * 0.5f;
		conversionRate *= 0.75f + record.radioHeat * 0.5f;
		conversionRate *= 0.75f + Mathf.Max(0, regionalData.sentiment) * 0.25f;
		conversionRate *= record.GetAwardMultiplier();
		conversionRate *= 1f - (region.distribution.difficulty * 0.3f);
		conversionRate *= GetSeasonalSalesMultiplier(month);
		
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
		float deptCapacity = region.distribution.departmentStoreCount * WEEKLY_SALES_PER_DEPT_STORE;
		float totalCapacity = (storeCapacity + deptCapacity) * region.distribution.inventoryDepth;
		
		if (record.currentPosition > 0 && record.currentPosition <= 20) {
			totalCapacity *= 1.5f;
		}
		
		bool isIndie = record.baseRecord.labelId != null && 
					!region.distribution.hasIndieDistribution &&
					!region.distribution.hasOneStopDistributors;
		if (isIndie) {
			totalCapacity *= INDIE_DISTRIBUTION_PENALTY;
		}
		
		if (regionalData.unitsInStores < rawSales) {
			regionalData.unitsBackordered += Mathf.RoundToInt(rawSales - regionalData.unitsInStores);
			rawSales = regionalData.unitsInStores;
		}
		if (captureBreakoutDiagnostic) {
			regionalData.breakoutBackordersBeforeRestock = regionalData.unitsBackordered;
		}
		
		rawSales = Mathf.Min(rawSales, totalCapacity);
		rawSales *= (float)GD.RandRange(0.96, 1.04);
		
		return Mathf.Max(0, Mathf.RoundToInt(rawSales));
	}
		
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

	private static float GetSeasonalSalesMultiplier(int month) {
		return month switch {
			12 => 1.20f,
			11 => 1.10f,
			1 => 0.90f,
			6 or 7 or 8 => 1.05f,
			_ => 1f
		};
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

	public static float GetRegionalLaunchFactor(AILabel label, string regionId) {
		if (label == null) return 1f;
		bool strong = label.strongRegions?.Contains(regionId) ?? false;
		bool covered = label.distributionRegions?.Contains(regionId) ?? true;
		if (strong) return 1.35f;
		if (covered) return 0.55f + (label.nationalReach * 0.45f);
		return 0.12f + (label.nationalReach * 0.18f);
	}

	public static int CalculateInitialRegionalStock(AILabel label, string regionId, float careerScale, float perceivedQualityMultiplier) {
		if (label == null) return 0;
		bool strong = label.strongRegions?.Contains(regionId) ?? false;
		bool covered = label.distributionRegions?.Contains(regionId) ?? true;
		bool isHome = !string.IsNullOrEmpty(label.homeRegion) && label.homeRegion == regionId;
		float access = covered ? 1f : 0.18f;
		float localDepth = isHome || strong
			? 0.25f + (label.distributionStrength * 0.75f)
			: 0.10f + (label.distributionStrength * 0.75f);
		float strongDepth = strong ? 1.45f : 1f;
		float noise = (float)GD.RandRange(0.85, 1.15);
		int raw = Mathf.RoundToInt(10000f * access * localDepth * strongDepth * careerScale * perceivedQualityMultiplier * noise);
		int floor = isHome || strong ? 100 : 0;
		return Mathf.Max(floor, raw);
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

		if (record.weeksSinceRelease > 8) {
			int weeksOverThreshold = record.weeksSinceRelease - 8;
			float decay = Mathf.Pow(AWARENESS_DECAY_RATE, weeksOverThreshold);
			record.awareness *= decay;
		}
	}
	
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
