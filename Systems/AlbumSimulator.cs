using Godot;

public static class AlbumSimulator {
	private const float BasePurchaseRate = 0.022f;
	private const float CatalogDecayStartWeeks = 26f;
	private const float CatalogWeeklyDecay = 0.985f;

	public static void UpdateAlbum(RecordRuntimeData record, AILabel label, float artistHeat, float substitutionPropensity) {
		RecordRuntimeData linkedPromo = string.IsNullOrEmpty(record.linkedPromoSingleId)
			? null
			: ChartManager.Instance?.GetRecordRuntimeData(record.linkedPromoSingleId);
		float singleHeat = linkedPromo != null ? Mathf.Clamp(linkedPromo.radioHeat, 0f, 1f) : 0f;
		record.linkedPromoRuntimeActive = linkedPromo != null;
		record.linkedPromoSingleHeat = singleHeat;
		record.albumSubstitutionPropensity = substitutionPropensity;
		record.cannibalizationSuppression = Mathf.Clamp(CompetitorManager.Instance?.CannibalizationStrength ?? 0f, 0f, 1f)
			* singleHeat * substitutionPropensity;
		float appeal = record.GetQuality();
		record.artistHeat = artistHeat;
		float campaign = ChartSimulator.GetCampaignImpact(label);
		float campaignAge = record.weeksSinceRelease <= 4 ? 1f : record.weeksSinceRelease <= 12 ? 0.45f : 0.12f;
		record.currentLabelPush = campaign * campaignAge;
		record.totalLabelInvestment += record.currentLabelPush;
		float chartDiscovery = record.currentPosition > 0 ? Mathf.Clamp((201f - record.currentPosition) / 200f, 0f, 1f) * 0.025f : 0f;
		float organic = (0.004f + appeal * 0.012f + artistHeat * 0.008f + record.currentLabelPush * 0.03f + chartDiscovery) * (1f - record.awareness);
		record.awareness = Mathf.Clamp(record.awareness + organic, 0f, 1f);
		float targetWom = Mathf.Pow(appeal, 2f) * 0.62f + record.momentum * 0.25f;
		record.wordOfMouth = Mathf.Lerp(record.wordOfMouth, Mathf.Max(0f, targetWom), 0.12f);
		// Album demand is not radio-led; retain only a small promotional proxy.
		record.radioHeat = Mathf.Lerp(record.radioHeat, 0.05f * campaign, 0.18f);
	}

	public static int CalculateRegionalSales(RecordRuntimeData record, MarketRegion region, RegionalRecordData data, int year, int month, bool liveTick, AILabel label) {
		Album album = record.baseRecord.album;
		float appeal = record.GetQuality();
		float buyerPool = region.GetAlbumMarketSize(record.baseRecord.primaryGenre, year);
		float awareness = record.awareness * 0.45f + data.awareness * 0.55f;
		if (record.currentPosition > 0 && record.currentPosition <= 10) awareness = Mathf.Max(awareness, 0.48f);
		else if (record.currentPosition > 0 && record.currentPosition <= 40) awareness = Mathf.Max(awareness, 0.30f);

		float penetration = data.unitsSoldTotal / Mathf.Max(1f, buyerPool);
		float exhaustion = Mathf.Max(0.15f, 1f / (1f + penetration * 4f));
		float conversion = BasePurchaseRate * Mathf.Pow(appeal, 2.5f) * exhaustion;
		conversion *= 0.72f + record.wordOfMouth * 0.45f;
		conversion *= 0.85f + Mathf.Max(0f, data.sentiment) * 0.25f;
		conversion *= 1f + AlbumModel.GetAlbumEraWeight(year) * (album?.packaging ?? 0f) * 0.12f;
		conversion *= record.weeksSinceRelease switch { <= 2 => 0.72f, <= 6 => 1.05f, <= 12 => 1f, _ => 0.92f };
		if (record.weeksSinceRelease > CatalogDecayStartWeeks) {
			conversion *= Mathf.Pow(CatalogWeeklyDecay, record.weeksSinceRelease - CatalogDecayStartWeeks);
		}
		conversion *= MarketSeasonality.GetAlbumSalesMultiplier(year, month, liveTick);
		bool genreMarketLive = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		conversion *= GenreAcceptanceService.GetLiveFormatMultiplier(record.baseRecord.primaryGenre,
			record.baseRecord.secondaryGenre, ReleaseFormat.Album, year,
			region.GetAcceptedAlbumOpportunityWeight(record.baseRecord.primaryGenre, year), genreMarketLive);
		conversion *= 1f - region.distribution.difficulty * 0.25f;
		if (label?.tier == LabelTier.Major) conversion *= 0.72f;
		else if (label?.tier == LabelTier.MidTier) conversion *= 0.88f;

		float rawDemandBeforeCannibalization = buyerPool * awareness * conversion;
		float rawSales = rawDemandBeforeCannibalization * (1f - record.cannibalizationSuppression);
		record.rawAlbumDemandBeforeCannibalization += rawDemandBeforeCannibalization;
		record.suppressedAlbumDemand += rawDemandBeforeCannibalization - rawSales;
		if (record.linkedPromoRuntimeActive) record.albumDemandWithActiveLinkedPromo += rawDemandBeforeCannibalization;
		else record.albumDemandWithInactiveLinkedPromo += rawDemandBeforeCannibalization;
		record.albumDemandWeightedSingleHeat += rawDemandBeforeCannibalization * record.linkedPromoSingleHeat;
		record.albumDemandWeightedSubstitutionPropensity += rawDemandBeforeCannibalization * record.albumSubstitutionPropensity;
		record.albumDemandWeightedSuppression += rawDemandBeforeCannibalization * record.cannibalizationSuppression;
		data.rawDemandThisWeek = rawSales;
		data.unitsBackordered = Mathf.RoundToInt(data.unitsBackordered * 0.55f);
		if (data.unitsInStores < rawSales) {
			data.unitsBackordered += Mathf.RoundToInt(rawSales - data.unitsInStores);
			rawSales = data.unitsInStores;
		}
		float capacity = (region.distribution.recordStoreCount * 130f + region.distribution.departmentStoreCount * 300f) * region.distribution.inventoryDepth;
		rawSales = Mathf.Min(rawSales, capacity) * (float)GD.RandRange(0.97, 1.03);
		return Mathf.Max(0, Mathf.RoundToInt(rawSales));
	}

	public static void UpdateRegionalState(RecordRuntimeData record, RegionalRecordData data) {
		float localGrowth = (record.currentLabelPush * 0.018f + record.wordOfMouth * 0.010f) * (1f - data.awareness);
		data.awareness = Mathf.Clamp(data.awareness + localGrowth, 0f, 1f);
		data.sentiment = Mathf.Lerp(data.sentiment, record.GetQuality(), 0.06f);
	}

	public static float CalculateChartPoints(RecordRuntimeData record) =>
		record.unitsThisWeek * (1f + Mathf.Max(-0.15f, record.momentum) * 0.15f);
}
