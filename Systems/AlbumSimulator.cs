using Godot;

public static class AlbumSimulator {
	// LP-RATIO RECALIBRATION (2026-08): 0.022 -> 0.045. Albums converted at ~0.17x the single rate
	// early, compounding the pool suppression to ~1.4% album units. Raised toward the single's
	// BASE_PURCHASE_RATE (0.07) but deliberately kept below it -- an LP is a considered, ~4.5x-priced
	// purchase, so a lower per-exposure rate than a single is correct.
	// EARLY-ERA UNITS LEVER (2026-08): 0.045 -> 0.080 -> 0.095. Early years are album-demand-bound below
	// the channel. BasePurchaseRate lives ONLY in realized sales, not the creation prior, so it lifts
	// early units without re-inflating album creation; late years stay channel-capped (inert there).
	// Key finding: BPR SATURATES on its own (~27% at 1960) because the top sellers exhaust their
	// per-record penetration against the buyer pool. It only regains traction once the pool is relieved
	// by GetAlbumPurchaseWillingness (MarketRegion.cs) -- the two levers bind in sequence. Final config:
	// willingness base 0.70 + BPR 0.095 lands 1960 at 29.8% (decade-validated); 1963/1969 unchanged at
	// 40.8/54.7 (channel-bound, match the author target curve). See D7SimRuntimeOptimizationHandoff §4.
	private const float BasePurchaseRate = 0.095f;
	// NORMAL-ALBUM CATALOG DECAY (2026-08, D7 soundtrack subsystem, phase 2): decade31 gave EVERY
	// album a uniform ~41wk median chart life; the author's shape is 12-18wk for normal albums with
	// the long 40-60wk tail reserved for soundtracks/evergreens. Tighten normal decay: start it early
	// (week 8, not 26) and decay steeply (0.92/wk) so the median lands ~15wk and a thin 40+wk tail
	// remains. Soundtracks branch away from this (SoundtrackCatalog* below) and, once the box-office
	// demand curve lands (phase 4), replace catalog decay entirely for AlbumFormat.Soundtrack.
	// 0.92 -> 0.93: the two-seed decade run landed the normal-album median at 11wk, just below the
	// 12-18wk band. Easing the weekly decay ~1pt lifts the median ~1-2wk back into band without
	// re-growing the long tail (see D7SoundtrackCastAlbumHandoff.md §1, §3.3, §4).
	private const float CatalogDecayStartWeeks = 8f;
	private const float CatalogWeeklyDecay = 0.93f;
	// Soundtrack/evergreen long tail: the old uniform-album constants, now reserved for the format
	// that historically earned the 40-60wk (multi-year) run. Placeholder until the phase-4 box-office
	// trajectory replaces flat decay for soundtracks; StageCast wants an even slower decay than this.
	private const float SoundtrackCatalogDecayStartWeeks = 26f;
	private const float SoundtrackCatalogWeeklyDecay = 0.985f;

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
		bool genreMarketLive = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		float appeal = record.GetQuality();
		// Soundtracks bypass genre-bound album demand for a demographic (adult/family) buyer pool, so a
		// small-genre soundtrack (SurfRock beach film, TradPop/Comedy cast album) can still chart.
		float buyerPool = album?.albumFormat == AlbumFormat.Soundtrack && album.externalMedia != null
			? region.GetSoundtrackAlbumMarketSize(album.externalMedia, year)
			: region.GetAlbumMarketSize(record.baseRecord.primaryGenre, year);
		float awareness = record.awareness * 0.45f + data.awareness * 0.55f;
		if (record.currentPosition > 0 && record.currentPosition <= 10) awareness = Mathf.Max(awareness, 0.48f);
		else if (record.currentPosition > 0 && record.currentPosition <= 40) awareness = Mathf.Max(awareness, 0.30f);

		int regionalCumulativeUnitsBeforeSale = data.unitsSoldTotal;
		float observedPenetration = regionalCumulativeUnitsBeforeSale / Mathf.Max(1f, buyerPool);
		float penetration = CalculateEffectiveRegionalPenetration(data, regionalCumulativeUnitsBeforeSale, buyerPool, genreMarketLive);
		float exhaustion = CalculateAlbumExhaustion(penetration);
		float conversion = BasePurchaseRate * Mathf.Pow(appeal, 2.5f) * exhaustion;
		conversion *= 0.72f + record.wordOfMouth * 0.45f;
		conversion *= 0.85f + Mathf.Max(0f, data.sentiment) * 0.25f;
		conversion *= 1f + AlbumModel.GetAlbumEraWeight(year) * (album?.packaging ?? 0f) * 0.12f;
		conversion *= record.weeksSinceRelease switch { <= 2 => 0.72f, <= 6 => 1.05f, <= 12 => 1f, _ => 0.92f };
		float catalogDecayMultiplier = GetAlbumTimeDecayMultiplier(album, record.weeksSinceRelease, month);
		conversion *= catalogDecayMultiplier;
		conversion *= MarketSeasonality.GetAlbumSalesMultiplier(year, month, liveTick);
		float formatTilt = GenreAcceptanceService.GetLiveFormatMultiplier(record.baseRecord.primaryGenre,
			record.baseRecord.secondaryGenre, ReleaseFormat.Album, year,
			region.GetAlbumOpportunityWeight(record.baseRecord.primaryGenre, year, genreMarketLive), genreMarketLive);
		conversion *= formatTilt;
		conversion *= 1f - region.distribution.difficulty * 0.25f;
		if (genreMarketLive) conversion *= GetLiveLabelDemandScale(label?.tier);
		else if (label?.tier == LabelTier.Major) conversion *= 0.72f;
		else if (label?.tier == LabelTier.MidTier) conversion *= 0.88f;

		float rawDemandBeforeCannibalization = CalculateRawDemandBeforeCannibalization(buyerPool, awareness, conversion);
		float rawSales = rawDemandBeforeCannibalization * (1f - record.cannibalizationSuppression);
		data.albumBuyerPoolThisWeek = buyerPool;
		data.albumAwarenessThisWeek = awareness;
		data.albumObservedPenetrationThisWeek = observedPenetration;
		data.albumEffectivePenetrationThisWeek = penetration;
		data.albumExhaustionThisWeek = exhaustion;
		data.albumCatalogDecayMultiplierThisWeek = catalogDecayMultiplier;
		data.albumFormatTiltThisWeek = formatTilt;
		data.albumConversionThisWeek = conversion;
		data.albumRawDemandBeforeCannibalizationThisWeek = rawDemandBeforeCannibalization;
		data.albumRawDemandAfterCannibalizationThisWeek = rawSales;
		data.albumUnitsInStoresBeforeSaleThisWeek = data.unitsInStores;
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
		if (!(GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)) {
			return Mathf.Max(0, Mathf.RoundToInt(rawSales));
		}
		data.storeCapacityThisWeek = Mathf.Max(0, Mathf.FloorToInt(capacity));
		data.serviceableIntentThisWeek = Mathf.Clamp(Mathf.RoundToInt(rawSales), 0,
			Mathf.Min(data.unitsInStores, data.storeCapacityThisWeek));
		return data.serviceableIntentThisWeek;
	}

	internal static float CalculateEffectiveRegionalPenetration(RegionalRecordData data, int regionalCumulativeUnitsBeforeSale,
		float buyerPool, bool genreMarketLive) {
		float observedPenetration = regionalCumulativeUnitsBeforeSale / Mathf.Max(1f, buyerPool);
		if (!genreMarketLive) return observedPenetration;
		float effectivePenetration = Mathf.Max(data.albumPeakEffectivePenetration, observedPenetration);
		data.albumPeakEffectivePenetration = effectivePenetration;
		return effectivePenetration;
	}

	// Weekly time-decay multiplier for an album's realized demand. A soundtrack carrying an
	// ExternalMediaProfile follows a BOX-OFFICE trajectory (phase 4) instead of flat catalog decay;
	// everything else (and a soundtrack with no profile) uses the tightened normal-album decay.
	internal static float GetAlbumTimeDecayMultiplier(Album album, int weeksSinceRelease, int month) {
		if (album?.albumFormat == AlbumFormat.Soundtrack && album.externalMedia != null)
			return GetSoundtrackBoxOfficeMultiplier(album.externalMedia, weeksSinceRelease, month);
		return GetCatalogDecayMultiplier(album?.albumFormat ?? AlbumFormat.Standard, weeksSinceRelease);
	}

	// Weekly catalog-decay multiplier, branched by format. Normal albums (Standard/Concept/Live/
	// Compilation/EP) decay early and steeply to land a ~15wk median chart life. The Soundtrack branch
	// is the fallback for a soundtrack that somehow lacks a profile; real soundtracks take the
	// box-office path in GetAlbumTimeDecayMultiplier above.
	internal static float GetCatalogDecayMultiplier(AlbumFormat format, int weeksSinceRelease) {
		(float startWeeks, float weeklyDecay) = format == AlbumFormat.Soundtrack
			? (SoundtrackCatalogDecayStartWeeks, SoundtrackCatalogWeeklyDecay)
			: (CatalogDecayStartWeeks, CatalogWeeklyDecay);
		return weeksSinceRelease > startWeeks
			? Mathf.Pow(weeklyDecay, weeksSinceRelease - startWeeks) : 1f;
	}

	// The box-office demand shape that gives soundtracks their historical run profile (handoff §3.3).
	// FilmScore/FilmSong: premiere-anchored, decaying at a rate set by boxOfficeTrajectory -- a flop
	// (bo~0) dies in ~3wk, a blockbuster (bo~1) sustains a high multiplier for 40-60+ wk. StageCast:
	// a lower ceiling but an ABSURDLY slow decay, so a hit cast album can hover near the chart for
	// 2-3 years (tourist catalog buying). Prestige titles that survive to the next awards season get a
	// Q1 resurrection bump. Returns a demand multiplier in the same role as catalog decay (1.0 at launch).
	internal static float GetSoundtrackBoxOfficeMultiplier(ExternalMediaProfile profile, int weeksSinceRelease, int month) {
		float w = Mathf.Max(0, weeksSinceRelease);
		float bo = Mathf.Clamp(profile.boxOfficeTrajectory, 0f, 1f);
		float mult;
		if (profile.isBlockbuster) {
			// The 1-3/decade monster tier. These ran 200-350 CHART WEEKS (5-7 years): West Side Story
			// 341wk (54 at No.1), Sound of Music 233wk (109 in Top 10), Camelot 265wk. A near-flat decay
			// (~0.9955/wk ~ 154wk half-life) lets a blockbuster hover on the chart for multiple years
			// before finally fading -- far slower than the 40-60wk median class. See memory
			// [[blockbuster-soundtrack-longevity]] for the historical targets.
			float weeklyDecay = Mathf.Lerp(0.994f, 0.9975f, bo);
			mult = Mathf.Pow(weeklyDecay, w);
		} else if (profile.sourceType == ExternalMediaSourceType.StageCast) {
			// Very slow decay: even a modest cast album persists for a year+, a hit for 2-3 years.
			float weeklyDecay = Mathf.Lerp(0.986f, 0.997f, bo);
			mult = Mathf.Pow(weeklyDecay, w) * Mathf.Lerp(0.70f, 1f, bo);
		} else {
			// Premiere-anchored, centered on the CATEGORY target: soundtracks as a class run 40-60wk
			// (handoff §1/§4). Floor raised 0.95->0.966 so a modest film (bo~0.48 median, decay ~0.979
			// -> ~32wk half-life -> ~50wk run) lands in-band; a flop (bo~0.2) still fades by ~35wk. The
			// two prior passes centered the class at ~6wk then ~18wk, both under the 40-60wk target.
			float weeklyDecay = Mathf.Lerp(0.966f, 0.992f, bo);
			float launchRamp = w < 3f ? Mathf.Lerp(0.85f, 1f, w / 3f) : 1f;
			mult = Mathf.Pow(weeklyDecay, w) * launchRamp;
		}
		// Awards-season resurrection: a prestige title still alive the next Q1 gets a visible bump.
		if ((month == 1 || month == 2 || month == 3) && profile.awardsPrestige > 0.5f && w > 20f)
			mult *= 1f + profile.awardsPrestige * 0.8f;
		return mult;
	}

	internal static float CalculateRawDemandBeforeCannibalization(float buyerPool, float awareness, float conversion) =>
		buyerPool * awareness * conversion;

	internal static float CalculateAlbumExhaustion(float effectivePenetration) =>
		Mathf.Max(0.15f, 1f / (1f + effectivePenetration * 4f));

	internal static float GetLiveLabelDemandScale(LabelTier? tier) => tier switch {
		LabelTier.Major => 1.36f,
		LabelTier.MidTier => 0.95f,
		LabelTier.Independent => 0.57f,
		LabelTier.Boutique => 0.63f,
		LabelTier.Small => 0.50f,
		_ => 1f
	};

	public static void UpdateRegionalState(RecordRuntimeData record, RegionalRecordData data) {
		float localGrowth = (record.currentLabelPush * 0.018f + record.wordOfMouth * 0.010f) * (1f - data.awareness);
		data.awareness = Mathf.Clamp(data.awareness + localGrowth, 0f, 1f);
		data.sentiment = Mathf.Lerp(data.sentiment, record.GetQuality(), 0.06f);
	}

	public static float CalculateChartPoints(RecordRuntimeData record) =>
		record.unitsThisWeek * (1f + Mathf.Max(-0.15f, record.momentum) * 0.15f);
}
