// Scripts/Systems/ChartManager.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ChartManager : Node {
	public static ChartManager Instance { get; private set; }

	[ExportGroup("Configuration")]
	[Export] private MarketRegion[] allRegions;
	[Export] private int chartSize = 100;
	[Export] private int targetActiveRecords = 500;
	[Export] private int prewarmWeeks = 8;

	[ExportGroup("AI Labels")]
	private List<AILabel> aiLabels;

	[ExportGroup("Genre Momentum Settings")]
	[Export] private float momentumDecayRate = 0.9f;
	[Export] private float momentumInfluence = 0.3f;
	[Export] private float chartPositionWeight = 0.01f;
	[Export] private float salesWeight = 0.00001f;

	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;

	public RecordRuntimeData GetRecordRuntimeData(string recordId) {
		return allRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
	}

	// Runtime state
	private int currentChartWeek;
	private Zeitgeist baseZeitgeist;
	private Dictionary<Genre, float> genreMomentum;
	private List<RecordRuntimeData> allRecords = new List<RecordRuntimeData>();
	private List<RecordRuntimeData> currentChart = new List<RecordRuntimeData>();
	private const int BubblingUnderSize = 15;
	private const int NeverChartedHorizonWeeks = 14;
	private const int RetirementSalesFloor = 50;
	private Dictionary<RecordRuntimeData, int> bubblingUnderPositions = new Dictionary<RecordRuntimeData, int>();
	private Dictionary<RecordRuntimeData, float> previousChartPoints = new Dictionary<RecordRuntimeData, float>();
	private Dictionary<string, AILabel> labelLookup = new Dictionary<string, AILabel>();

	// Artist heat cache
	private Dictionary<string, float> artistHeatCache = new Dictionary<string, float>();
	private int artistHeatCacheWeek = -1;

	public List<AILabel> GetAllLabels() {
		return aiLabels;
	}

	// Record ID counter
	private int recordIdCounter = 0;

	// Events
	public event Action<List<RecordRuntimeData>> OnChartCalculated;
	public event Action<RecordRuntimeData> OnRecordEnteredChart;
	public event Action<RecordRuntimeData> OnRecordHitNumberOne;
	public event Action<RecordRuntimeData> OnRecordChartUpdated;
	public event Action<RecordRuntimeData> OnRecordLeftChart;
	public event Action<Genre, float> OnGenreMomentumChanged;

	// ========================================================================
	// GODOT LIFECYCLE
	// ========================================================================

	public override void _EnterTree() {
		if (Instance != null && Instance != this) {
			QueueFree();
			return;
		}
		Instance = this;

		InitializeGenreMomentum();
		GenerateAILabelsIfNeeded();
		InitializeRegions();
	}

	public override void _Ready() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded += OnWeekEnded;
			TimeManager.Instance.OnYearChanged += OnYearChanged;
		}

		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;

		// 1. Generate artist pool first
		GD.Print("=== INITIALIZATION STEP 1: Artist Pool ===");
		if (ArtistManager.Instance != null) {
			ArtistManager.Instance.GenerateInitialPool(year);
			GD.Print($"Artist pool size: {ArtistManager.Instance.GetUnsignedArtists().Count}");
		} else {
			GD.PushError("ArtistManager.Instance is NULL!");
		}

		// 2. Generate labels
		GD.Print("=== INITIALIZATION STEP 2: Labels ===");
		GenerateAILabelsIfNeeded();
		GD.Print($"Labels generated: {aiLabels?.Count ?? 0}");

		// 3. Populate rosters
		GD.Print("=== INITIALIZATION STEP 3: Rosters ===");
		if (RosterManager.Instance != null && aiLabels != null) {
			RosterManager.Instance.InitializeAllRosters(aiLabels, year);
			int totalSigned = aiLabels.Sum(l => l.CurrentRosterSize);
			GD.Print($"Total artists signed to labels: {totalSigned}");
		} else {
			GD.PushError($"RosterManager.Instance: {RosterManager.Instance != null}, aiLabels: {aiLabels != null}");
		}

		// 4. Initialize competitor manager
		GD.Print("=== INITIALIZATION STEP 4: Competitor Manager ===");
		if (CompetitorManager.Instance != null && aiLabels != null) {
			CompetitorManager.Instance.Initialize(aiLabels);
			GD.Print($"CompetitorManager initialized");
		} else {
			GD.PushError($"CompetitorManager.Instance: {CompetitorManager.Instance != null}");
		}

		// 5. Initialize regions
		InitializeRegions();

		// 6. Set zeitgeist
		UpdateBaseZeitgeist(year);

		// 7. Pre-warm
		GD.Print("=== INITIALIZATION STEP 5: Pre-warm ===");
		GD.Print($"Records before prewarm: {allRecords.Count}");
		PrewarmSimulation();
		GD.Print($"Records after prewarm: {allRecords.Count}");
		GD.Print($"Chart size: {currentChart.Count}");
	}

	public override void _ExitTree() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded -= OnWeekEnded;
			TimeManager.Instance.OnYearChanged -= OnYearChanged;
		}
	}

	// ========================================================================
	// INITIALIZATION
	// ========================================================================

	private void InitializeGenreMomentum() {
		genreMomentum = new Dictionary<Genre, float>();
		foreach (Genre g in Enum.GetValues(typeof(Genre))) {
			genreMomentum[g] = 0f;
		}
	}

	private void InitializeRegions() {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		foreach (var region in allRegions) {
			region.InitializeRuntimeState(year);
		}
	}

	private void GenerateAILabelsIfNeeded() {
		int targetLabels = 600;

		if (aiLabels == null || aiLabels.Count < targetLabels) {
			if (NameGenerator.Instance == null) {
				GD.PushWarning("ChartManager: NameGenerator not ready, using fallback names");
			}

			GD.Print($"ChartManager: Generating {targetLabels} AI labels...");
			aiLabels = AILabelFactory.GenerateAllLabels(targetLabels);

			labelLookup.Clear();
			foreach (var label in aiLabels) {
				if (!string.IsNullOrEmpty(label.labelId)) {
					labelLookup[label.labelId] = label;
				}
			}

			GD.Print($"ChartManager: Generated {aiLabels.Count} labels, {labelLookup.Count} in lookup");
		}
	}

	// ========================================================================
	// LABEL & REGION LOOKUP
	// ========================================================================

	public AILabel GetLabelById(string labelId) {
		if (string.IsNullOrEmpty(labelId)) return null;
		labelLookup.TryGetValue(labelId, out var label);
		return label;
	}

	public string GetLabelName(string labelId) {
		var label = GetLabelById(labelId);
		return label != null ? label.labelName : labelId;
	}

	public MarketRegion GetRegionById(string regionId) {
		return allRegions.FirstOrDefault(r => r.regionId == regionId);
	}

	// ========================================================================
	// ARTIST HEAT
	// ========================================================================

	private float CalculateArtistHeat(string artistId) {
		if (artistHeatCacheWeek != currentChartWeek) {
			artistHeatCache.Clear();
			artistHeatCacheWeek = currentChartWeek;

			var artistRecords = allRecords
				.Where(r => r.currentPosition > 0 || r.peakPosition > 0)
				.GroupBy(r => r.baseRecord.artistId);

			foreach (var group in artistRecords) {
				float heat = 0f;
				int hitCount = 0;

				foreach (var record in group) {
					if (record.currentPosition > 0 && record.currentPosition <= 10) {
						heat += 0.3f;
					} else if (record.currentPosition > 0 && record.currentPosition <= 40) {
						heat += 0.15f;
					} else if (record.currentPosition > 0) {
						heat += 0.05f;
					}

					if (record.peakPosition > 0 && record.peakPosition <= 10) {
						hitCount++;
						heat += 0.1f;
					} else if (record.peakPosition > 0 && record.peakPosition <= 40) {
						hitCount++;
						heat += 0.05f;
					}
				}

				heat += Mathf.Min(hitCount * 0.05f, 0.3f);
				artistHeatCache[group.Key] = Mathf.Clamp(heat, 0f, 1f);
			}
		}

		return artistHeatCache.TryGetValue(artistId, out float cachedHeat) ? cachedHeat : 0f;
	}

	// ========================================================================
	// GENRE AVAILABILITY BY YEAR
	// ========================================================================

	private bool IsGenreAvailableInYear(Genre genre, int year) {
		return genre switch {
			Genre.TraditionalPop => true,
			Genre.Jazz => true,
			Genre.Country => true,
			Genre.Gospel => true,
			Genre.RnB => true,
			Genre.DooWop => year <= 1965,
			Genre.RockAndRoll => true,
			Genre.TeenPop => true,
			Genre.Soul => year >= 1961,
			Genre.GirlGroup => year >= 1961 && year <= 1966,
			Genre.Motown => year >= 1961,
			Genre.SurfRock => year >= 1962 && year <= 1966,
			Genre.Folk => year >= 1963,
			Genre.BritishInvasion => year >= 1964,
			Genre.GarageRock => year >= 1964,
			Genre.FolkRock => year >= 1965,
			Genre.Funk => year >= 1965,
			Genre.BluesRock => year >= 1965,
			Genre.Psychedelic => year >= 1966,
			Genre.BaroquePop => year >= 1966,
			Genre.SunshinePop => year >= 1966,
			Genre.AcidRock => year >= 1967,
			Genre.ProgressiveRock => year >= 1967,
			Genre.Bubblegum => year >= 1967,
			Genre.CountryRock => year >= 1968,
			Genre.HardRock => year >= 1968,
			Genre.ProtoMetal => year >= 1969,
			Genre.ProtoPunk => year >= 1968,
			_ => true
		};
	}

	// ========================================================================
	// PRE-WARMING
	// ========================================================================

	private void PrewarmSimulation() {
		if (debugMode) GD.Print("ChartManager: Pre-warming simulation...");

		for (int week = 0; week < prewarmWeeks; week++) {
			SimulateWeek(triggerEvents: false);

			if (debugMode && week == 0) {
				var topByPoints = allRecords
					.OrderByDescending(r => r.unitsThisWeek)
					.Take(5);
				GD.Print($"Prewarm Week {week}: Top sales = {string.Join(", ", topByPoints.Select(r => r.unitsThisWeek))}");
			}
		}

		if (debugMode) GD.Print($"ChartManager: Generated {allRecords.Count} initial records");

		int preCount = allRecords.Count;
		allRecords.RemoveAll(r =>
			r.currentPosition == 0 &&
			r.peakPosition == 0 &&
			r.totalUnitsSold < 500
		);

		currentChartWeek = 0;

	foreach (var record in allRecords) {
		if (record.peakPosition > 0) {
			OnRecordChartUpdated?.Invoke(record);
			
			if (record.currentPosition == 0) {
				OnRecordLeftChart?.Invoke(record);
			}
		}
	}


	if (debugMode) {
			GD.Print($"ChartManager: Pre-warm complete. Culled {preCount - allRecords.Count} dead records.");
			GD.Print($"ChartManager: {allRecords.Count} active records, {currentChart.Count} on chart.");
			DebugPrintTopTen();
		}
	}

	// ========================================================================
	// WEEKLY CYCLE
	// ========================================================================

	private void OnWeekEnded(GameDate date) {
		currentChartWeek++;

		SimulateWeek(triggerEvents: true);

		foreach (var record in allRecords) {
			if (record.isGrammyWinner && record.weeksOfGrammyBump > 0) {
				record.weeksOfGrammyBump--;
			}
		}

		UpdateGenreMomentum();

		CullDeadRecords(includeChartedRecords: currentChartWeek % 4 == 0);
	}

	private void OnYearChanged(GameDate date) {
		UpdateBaseZeitgeist(date.year);

		foreach (var region in allRegions) {
			region.InitializeRuntimeState(date.year);
		}
	}

	private void UpdateBaseZeitgeist(int year) {
		baseZeitgeist = Zeitgeist.GetForYear(year);
		if (debugMode) GD.Print($"ChartManager: Base zeitgeist updated for {year}");
	}

	// ========================================================================
	// NEW RELEASES
	// ========================================================================

	public void ReleaseRecord(Record record, AILabel releasingLabel = null) {
		var runtimeData = new RecordRuntimeData(record);
		float perceivedQualityMult = 1f;
		if (releasingLabel != null && !record.isPlayerOwned) {
			float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
			float noiseRange = Mathf.Lerp(0.30f, 0.10f, releasingLabel.scoutingAbility);
			float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
			perceivedQualityMult = 0.6f + (perceivedQuality * 0.8f);
		}

		float quality = runtimeData.GetQuality();
		float labelPush = releasingLabel != null ? ChartSimulator.GetCampaignImpact(releasingLabel) : 0.2f;

		runtimeData.awareness = 0.12f + (quality * 0.08f) + (labelPush * 0.15f);
		runtimeData.radioHeat = 0.08f + (labelPush * 0.12f);

		foreach (var region in allRegions) {
			var regionalData = new RegionalRecordData(region.regionId);
			runtimeData.regionalData[region.regionId] = regionalData;
		}

		allRecords.Add(runtimeData);

		if (releasingLabel != null && !record.isPlayerOwned) {
			PromoteRecordAI(runtimeData, releasingLabel, perceivedQualityMult);
		}

		if (debugMode) GD.Print($"Released: {record.title} by {record.artistName} (awareness: {runtimeData.awareness:F2}, radio: {runtimeData.radioHeat:F2})");
	}

	private void PromoteRecordAI(RecordRuntimeData record, AILabel label, float perceivedQualityMult) {
		float campaignImpact = ChartSimulator.GetCampaignImpact(label);
		float broadLaunch = 0.06f + (campaignImpact * (0.10f + label.nationalReach * 0.10f));
		record.awareness = Mathf.Max(broadLaunch, record.awareness);
		record.awareness = Mathf.Clamp(record.awareness, 0f, 1f);

		record.radioHeat = Mathf.Max(0.1f, record.radioHeat);
		record.radioHeat += campaignImpact * 0.12f;
		record.radioHeat = Mathf.Clamp(record.radioHeat, 0f, 1f);

		foreach (var region in allRegions) {
			if (!record.regionalData.ContainsKey(region.regionId)) continue;

			var data = record.regionalData[region.regionId];
			float regionStrength = ChartSimulator.GetRegionalLaunchFactor(label, region.regionId);
			int units = ChartSimulator.CalculateInitialRegionalStock(label, region.regionId, 1f, perceivedQualityMult);
			data.unitsInStores = units;

			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			data.radioPlay = (0.15f + (float)GD.RandRange(0.1, 0.25)) * campaignImpact * regionStrength / radioDifficulty;
			data.awareness = (0.15f + (float)GD.RandRange(0.05, 0.15)) * campaignImpact * regionStrength;

			float quality = (record.baseRecord.hookStrength + record.baseRecord.productionQuality) / 2f;
			float genreFit = GetGenreFit(record.baseRecord.primaryGenre, region);
			data.sentiment = (quality * 0.7f + genreFit * 0.3f) + (float)GD.RandRange(-0.05, 0.1);
		}

		record.initialLaunchAwareness = record.awareness;
		record.initialLaunchStock = record.regionalData.Values.Sum(data => data.unitsInStores);
		record.launchCareerState = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId)?.careerState ?? CareerState.Unsigned;
		record.perceivedQualityMultiplier = perceivedQualityMult;

		if (debugMode) {
			int totalStock = record.regionalData.Values.Sum(d => d.unitsInStores);
			GD.Print($"Promoted {record.baseRecord.title}: {totalStock:N0} total units stocked, awareness={record.awareness:F2}");
		}
	}

	// ========================================================================
	// CHART SIMULATION (Core Loop)
	// ========================================================================

	private void SimulateWeek(bool triggerEvents) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;

		// === STEP 1: Update global record state ===
		foreach (var record in allRecords) {
			record.weeksSinceRelease++;

			AILabel label = GetLabelById(record.baseRecord.labelId);
			float genreAcceptance = GetEffectiveGenreAcceptance(record.baseRecord.primaryGenre);
			float artistHeat = CalculateArtistHeat(record.baseRecord.artistId);

			ChartSimulator.UpdateRecord(record, label, genreAcceptance, artistHeat);
		}

		// === STEP 2: Calculate regional sales ===
		foreach (var record in allRecords) {
			int totalSales = 0;
			float quality = record.GetQuality();

			foreach (var region in allRegions) {
				if (!record.regionalData.ContainsKey(region.regionId)) {
					record.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
				}

				var regionalData = record.regionalData[region.regionId];

				float regionalAcceptance = region.GetGenreAcceptance(record.baseRecord.primaryGenre, year);
				float nationalAcceptance = GetEffectiveGenreAcceptance(record.baseRecord.primaryGenre);
				float blendedAcceptance = (regionalAcceptance * 0.6f) + (nationalAcceptance * 0.4f);

				int regionalSales = ChartSimulator.CalculateRegionalSales(
					record,
					region,
					regionalData,
					quality,
					blendedAcceptance,
					TimeManager.Instance?.CurrentDate.month ?? 1,
					GetInternalPreviousPosition(record)
				);

				regionalData.unitsInStores = Mathf.Max(0, regionalData.unitsInStores - regionalSales);
				regionalData.unitsSoldThisWeek = regionalSales;
				regionalData.unitsSoldTotal += regionalSales;

				totalSales += regionalSales;
			}

			ChartSimulator.FinalizeWeeklySales(record, totalSales);
			ChartSimulator.UpdateSaturation(record, allRegions);
		}

		// Demand evidence is evaluated before replenishment so inventory exhaustion
		// cannot masquerade as audience growth.
		foreach (var record in allRecords) {
			UpdateRegionalBreakoutState(record, year);
		}

		// === STEP 2.5: RESTOCK HOT RECORDS ===
		RestockHotRecords();

		// === STEP 3: Update regional awareness/radio ===
		foreach (var record in allRecords) {
			UpdateRecordRegionalData(record);
			ApplyBreakoutDiscovery(record);
		}

		// === STEP 4: Calculate chart points ===
		var chartPoints = new Dictionary<RecordRuntimeData, float>();
		foreach (var record in allRecords) {
			float points = ChartSimulator.CalculateChartPoints(record, allRegions);
			if (record.unitsThisWeek == 0 && points > 0) points *= 0.1f;
			if (points > 0) {
				chartPoints[record] = points;
			}
		}

		// === STEP 5: Sort by points ===
		var rawRanking = chartPoints
			.OrderByDescending(kvp => kvp.Value)
			.ThenByDescending(kvp => kvp.Key.unitsThisWeek)
			.Select(kvp => kvp.Key)
			.ToList();

		var rawPositions = rawRanking
			.Select((record, index) => new { record, position = index + 1 })
			.ToDictionary(x => x.record, x => x.position);

		// Rank by the best of raw position and the sales-gated inertia cap. This
		// preserves relative point order when no record qualifies for protection.
		var sortedByPoints = rawRanking
			.Select(record => {
				int previousPosition = GetInternalPreviousPosition(record);
				int rawPosition = rawPositions[record];
				return new {
					record,
					rawPosition,
					effectivePosition = ChartSimulator.GetInertiaPositionCap(record, previousPosition, rawPosition)
				};
			})
			.OrderBy(x => x.effectivePosition)
			.ThenBy(x => x.rawPosition)
			.Take(chartSize + BubblingUnderSize)
			.Select(x => x.record)
			.ToList();

		LogMidChartExits(chartPoints, rawRanking, sortedByPoints);

		// === STEP 6: Apply position calculations ===
		AssignChartPositions(sortedByPoints, triggerEvents);
		currentChart = sortedByPoints.Take(chartSize).ToList();
		previousChartPoints = chartPoints;
	}

	private int GetInternalPreviousPosition(RecordRuntimeData record) {
		if (record.currentPosition > 0) return record.currentPosition;
		return bubblingUnderPositions.TryGetValue(record, out int position) ? position : 0;
	}

	private void LogMidChartExits(
		Dictionary<RecordRuntimeData, float> chartPoints,
		List<RecordRuntimeData> rawRanking,
		List<RecordRuntimeData> bufferedRanking) {
		if (!debugMode || rawRanking.Count < chartSize) return;

		float cutoff = chartPoints[rawRanking[chartSize - 1]];
		var establishedOnly = rawRanking.Where(r => r.weeksSinceRelease > 3).ToList();
		float establishedCutoff = establishedOnly.Count >= chartSize
			? chartPoints[establishedOnly[chartSize - 1]]
			: 0f;
		float entrantCutoffLift = Mathf.Max(0f, cutoff - establishedCutoff);
		var published = new HashSet<RecordRuntimeData>(bufferedRanking.Take(chartSize));

		foreach (var record in allRecords.Where(r => r.currentPosition >= 40 && r.currentPosition <= 60 && r.weeksOnChart >= 10 && !published.Contains(r))) {
			float points = chartPoints.TryGetValue(record, out float current) ? current : 0f;
			float prior = previousChartPoints.TryGetValue(record, out float previous) ? previous : points;
			float organicDecline = Mathf.Max(0f, prior - points);
			GD.Print($"CHART EXIT DIAGNOSTIC: {record.baseRecord.title} | prior #{record.currentPosition}, weeks {record.weeksOnChart} | raw points {points:F1}, #100 cutoff {cutoff:F1}, gap {Mathf.Max(0f, cutoff - points):F1} | own organic decline {organicDecline:F1} | new-release cutoff lift {entrantCutoffLift:F1}");
		}
	}

	private void RestockHotRecords() {
		foreach (var record in allRecords) {
			AILabel label = GetLabelById(record.baseRecord.labelId);
			if (label == null) continue;

			foreach (var region in allRegions) {
				if (!record.regionalData.TryGetValue(region.regionId, out var data)) continue;
				bool isCovered = label.distributionRegions?.Contains(region.regionId) ?? true;

				int stockBeforeSales = data.unitsInStores + data.unitsSoldThisWeek;
				bool preChartDemandNeedsRestock = record.currentPosition == 0 &&
					data.breakoutScore >= 0.20f &&
					(data.unitsBackordered > 250 || data.rawDemandThisWeek > data.unitsInStores * 0.45f);
				bool chartedNeedsRestock = record.currentPosition > 0 &&
					(data.unitsBackordered > 500 ||
					(data.unitsInStores < data.unitsSoldThisWeek * 2 && record.currentPosition <= 40));
				bool needsRestock = chartedNeedsRestock || preChartDemandNeedsRestock;
				bool captureBreakoutDiagnostic = !record.baseRecord.isPlayerOwned &&
					record.weeksSinceRelease >= 1 &&
					record.weeksSinceRelease <= 14;
				if (captureBreakoutDiagnostic) {
					data.breakoutDiagnosticObserved = true;
					data.breakoutPreRestockStock = data.unitsInStores;
					data.breakoutTriggered = preChartDemandNeedsRestock;
					data.breakoutRequestedRestock = 0;
					data.breakoutAppliedRestock = 0;
					int physicalCapacity = region.distribution.recordStoreCount * 100 +
						region.distribution.departmentStoreCount * 200;
					data.breakoutMaxCapacity = Mathf.RoundToInt(physicalCapacity * (isCovered
						? 0.55f + label.distributionStrength * 0.65f
						: 0.20f));
					data.breakoutCapacityCapped = false;
				}

				if (needsRestock) {
					float demandSignal = (data.rawDemandThisWeek * 0.65f) + (data.unitsSoldThisWeek * 0.35f) + (data.unitsBackordered * 0.25f);
					float serviceLevel = isCovered
						? 0.70f + (label.distributionStrength * 0.80f)
						: 0.18f + (label.distributionStrength * 0.25f);
					int restockAmount = Mathf.RoundToInt(demandSignal * serviceLevel);
					int requestedRestock = restockAmount;

					int physicalCapacity = region.distribution.recordStoreCount * 100 +
									region.distribution.departmentStoreCount * 200;
					int maxCapacity = Mathf.RoundToInt(physicalCapacity * (isCovered
						? 0.55f + label.distributionStrength * 0.65f
						: 0.20f));
					restockAmount = Mathf.Min(restockAmount, maxCapacity - data.unitsInStores);
					if (captureBreakoutDiagnostic) {
						data.breakoutRequestedRestock = requestedRestock;
						data.breakoutAppliedRestock = Mathf.Max(0, restockAmount);
						data.breakoutCapacityCapped = requestedRestock > Mathf.Max(0, maxCapacity - data.unitsInStores);
					}

					if (restockAmount > 0) {
						data.unitsInStores += restockAmount;
						data.unitsBackordered = Mathf.Max(0, data.unitsBackordered - restockAmount);
					}
				}
			}
		}
	}

	// Legacy method for external calls
	public void CalculateChart() {
		SimulateWeek(triggerEvents: true);
		UpdateGenreMomentum();
	}

	private void UpdateRecordRegionalData(RecordRuntimeData record) {
		foreach (var region in allRegions) {
			if (!record.regionalData.ContainsKey(region.regionId)) {
				record.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}

			var data = record.regionalData[region.regionId];

			// Awareness decay
			data.awareness *= 0.92f;

			// Radio play: decay + pull toward national heat
			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			float targetRegionalRadio = record.radioHeat / radioDifficulty;
			data.radioPlay = Mathf.Lerp(data.radioPlay * 0.85f, targetRegionalRadio, 0.2f);

			// Radio builds regional awareness
			data.awareness += data.radioPlay * 0.12f;
			data.awareness = Mathf.Clamp(data.awareness, 0f, 1f);

			// Jukebox decay
			data.jukeboxPlay *= 0.95f;

			// Word of mouth in region
			if (data.sentiment > 0.5f && data.awareness > 0.3f) {
				float wordOfMouth = data.sentiment * data.awareness * 0.015f;
				data.awareness = Mathf.Clamp(data.awareness + wordOfMouth, 0f, 1f);
			}
		}
	}

	private void UpdateRegionalBreakoutState(RecordRuntimeData record, int year) {
		AILabel label = GetLabelById(record.baseRecord.labelId);
		if (label == null) return;

		float quality = record.GetQuality();
		int breakoutMarkets = 0;
		int testMarkets = 0;
		float strongest = 0f;
		float velocityTotal = 0f;
		int velocityCount = 0;
		int unmetDemand = 0;
		int coveredCount = 0;

		foreach (MarketRegion region in allRegions) {
			if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data)) continue;
			bool covered = label.distributionRegions?.Contains(region.regionId) ?? true;
			if (covered) coveredCount++;

			float previousDemand = data.previousRawDemand;
			float velocity = previousDemand >= 150f
				? (data.rawDemandThisWeek - previousDemand) / previousDemand
				: 0f;
			data.salesVelocity = Mathf.Clamp(velocity, -1f, 2f);
			if (previousDemand >= 150f && velocity > 0.04f) data.sustainedGrowthWeeks++;
			else if (velocity < -0.08f) data.sustainedGrowthWeeks = 0;

			float rawVolume = Mathf.Clamp((data.rawDemandThisWeek - 150f) / 3500f, 0f, 1f);
			float fulfilledVolume = Mathf.Clamp(data.unitsSoldThisWeek / 3000f, 0f, 1f);
			float volumeInput = rawVolume * 0.70f + fulfilledVolume * 0.30f;
			float velocityInput = Mathf.Clamp((velocity + 0.10f) / 0.65f, 0f, 1f);
			float audienceInput = Mathf.Clamp(data.awareness, 0f, 1f);
			float mediaInput = Mathf.Clamp(data.radioPlay * 0.75f + data.jukeboxPlay * 0.25f, 0f, 1f);
			float genreFit = region.GetGenreAcceptance(record.baseRecord.primaryGenre, year);
			float unmetInput = volumeInput * Mathf.Clamp(data.unitsBackordered / Mathf.Max(750f, data.rawDemandThisWeek), 0f, 1f);
			float sustainedInput = Mathf.Clamp(data.sustainedGrowthWeeks / 3f, 0f, 1f);

			float evidence = volumeInput * 0.34f + velocityInput * 0.15f + sustainedInput * 0.09f +
				audienceInput * 0.12f + mediaInput * 0.10f + genreFit * 0.08f +
				quality * 0.08f + unmetInput * 0.04f;
			evidence *= 0.55f + volumeInput * 0.45f;
			float response = evidence >= data.breakoutScore ? 0.48f : 0.28f;
			data.breakoutScore = Mathf.Lerp(data.breakoutScore, evidence, response);
			data.peakBreakoutScore = Mathf.Max(data.peakBreakoutScore, data.breakoutScore);

			if (data.breakoutScore >= 0.24f) {
				data.tractionWeeks++;
			} else {
				data.tractionWeeks = Mathf.Max(0, data.tractionWeeks - 1);
			}
			if (evidence < 0.18f || (velocity < -0.35f && data.rawDemandThisWeek < 1500f)) data.collapseWeeks++;
			else data.collapseWeeks = 0;

			if (data.breakoutScore >= 0.40f && data.tractionWeeks >= 2) {
				data.breakoutStage = RegionalBreakoutStage.RegionalBreakout;
			} else if (data.breakoutScore >= 0.24f && data.breakoutStage < RegionalBreakoutStage.RegionalBreakout) {
				data.breakoutStage = RegionalBreakoutStage.LocalTraction;
			} else if (data.collapseWeeks >= 2 && data.breakoutStage < RegionalBreakoutStage.RegionalBreakout) {
				data.breakoutStage = RegionalBreakoutStage.None;
			}
			if (data.collapseWeeks >= 2 && data.breakoutStage >= RegionalBreakoutStage.RegionalBreakout) {
				data.breakoutStage = RegionalBreakoutStage.LocalTraction;
			}

			if (data.breakoutStage >= RegionalBreakoutStage.RegionalBreakout) breakoutMarkets++;
			if (data.neighboringMarketTestStrength >= 0.08f) testMarkets++;
			strongest = Mathf.Max(strongest, data.breakoutScore);
			if (previousDemand >= 150f) { velocityTotal += data.salesVelocity; velocityCount++; }
			unmetDemand += data.unitsBackordered;

			data.breakoutVolumeInput = volumeInput;
			data.breakoutVelocityInput = velocityInput;
			data.breakoutAudienceInput = audienceInput;
			data.breakoutMediaInput = mediaInput;
			data.breakoutGenreFitInput = genreFit;
			data.breakoutQualityInput = quality;
			data.breakoutUnmetDemandInput = unmetInput;
			data.breakoutAwarenessGain = 0f;
			data.breakoutRadioGain = 0f;
			data.breakoutWordOfMouthGain = 0f;
			data.previousRawDemand = data.rawDemandThisWeek;
		}

		record.regionalBreakoutCount = breakoutMarkets;
		record.neighboringMarketTestCount = testMarkets;
		record.peakRegionalBreakoutStrength = Mathf.Max(record.peakRegionalBreakoutStrength, strongest);
		record.sustainedSalesVelocity = velocityCount > 0 ? velocityTotal / velocityCount : 0f;
		record.unmetRegionalDemand = unmetDemand;
		record.coveredRegionCount = coveredCount;
		float marketBreadth = Mathf.Clamp((breakoutMarkets + testMarkets * 0.35f) / 2.5f, 0f, 1f);
		record.crossoverCandidateStrength = strongest * marketBreadth;
		if ((breakoutMarkets >= 2 || (breakoutMarkets >= 1 && testMarkets >= 2)) && strongest >= 0.46f) {
			foreach (RegionalRecordData data in record.regionalData.Values) {
				if (data.breakoutStage >= RegionalBreakoutStage.RegionalBreakout)
					data.breakoutStage = RegionalBreakoutStage.NationalCrossoverCandidate;
			}
		}
	}

	private void ApplyBreakoutDiscovery(RecordRuntimeData record) {
		AILabel label = GetLabelById(record.baseRecord.labelId);
		if (label == null) return;
		// Chart exposure remains the larger engine, but proven regional discovery
		// does not disappear merely because the record crossed position 100.
		float discoveryScale = record.currentPosition > 0 ? 0.75f : 1f;

		float nationalGain = 0f;
		foreach (MarketRegion sourceRegion in allRegions) {
			if (!record.regionalData.TryGetValue(sourceRegion.regionId, out RegionalRecordData source)) continue;
			if (source.breakoutStage < RegionalBreakoutStage.LocalTraction) continue;

			float strength = Mathf.Clamp((source.breakoutScore - 0.24f) / 0.40f, 0f, 1f);
			float localAwarenessGain = source.breakoutStage >= RegionalBreakoutStage.RegionalBreakout
				? 0.006f + strength * 0.014f
				: 0.001f + strength * 0.003f;
			float localRadioGain = source.breakoutStage >= RegionalBreakoutStage.RegionalBreakout
				? 0.0025f + strength * 0.007f
				: strength * 0.001f;
			localAwarenessGain *= discoveryScale;
			localRadioGain *= discoveryScale;
			source.awareness = Mathf.Min(0.58f, source.awareness + localAwarenessGain);
			source.radioPlay = Mathf.Min(0.45f, source.radioPlay + localRadioGain);
			source.jukeboxPlay = Mathf.Min(0.55f, source.jukeboxPlay + strength * 0.006f * discoveryScale);
			source.breakoutAwarenessGain += localAwarenessGain;
			source.breakoutRadioGain += localRadioGain;

			float womGain = strength * (source.breakoutStage >= RegionalBreakoutStage.RegionalBreakout ? 0.005f : 0.001f) * discoveryScale;
			record.wordOfMouth = Mathf.Min(0.72f, record.wordOfMouth + womGain);
			source.breakoutWordOfMouthGain += womGain;
			nationalGain += womGain * 0.30f;

			if (source.breakoutStage < RegionalBreakoutStage.RegionalBreakout || source.tractionWeeks < 2) continue;
			float propagationCapacity = 0.25f + label.nationalReach * 0.45f + label.distributionStrength * 0.30f;
			foreach (string neighborId in GetNeighborRegionIds(sourceRegion.regionId)) {
				if (!record.regionalData.TryGetValue(neighborId, out RegionalRecordData neighbor)) continue;
				float testGain = strength * propagationCapacity * 0.10f * discoveryScale;
				neighbor.neighboringMarketTestStrength = Mathf.Clamp(neighbor.neighboringMarketTestStrength * 0.78f + testGain, 0f, 1f);
				neighbor.breakoutSourceRegionId = sourceRegion.regionId;
				if (neighbor.breakoutStage < RegionalBreakoutStage.RegionalBreakout)
					neighbor.breakoutStage = RegionalBreakoutStage.NeighboringMarketTest;
				float neighborAwarenessGain = 0.002f + testGain * 0.040f;
				float neighborRadioGain = testGain * 0.012f;
				neighbor.awareness = Mathf.Min(0.34f, neighbor.awareness + neighborAwarenessGain);
				neighbor.radioPlay = Mathf.Min(0.24f, neighbor.radioPlay + neighborRadioGain);
				neighbor.breakoutAwarenessGain += neighborAwarenessGain;
				neighbor.breakoutRadioGain += neighborRadioGain;
			}
		}
		float crossoverBreadth = Mathf.Clamp((record.crossoverCandidateStrength - 0.15f) / 0.35f, 0f, 1f);
		float crossoverGain = crossoverBreadth * 0.015f * discoveryScale;
		record.awareness = Mathf.Min(0.60f,
			record.awareness + Mathf.Min(0.005f * discoveryScale, nationalGain) + crossoverGain);
	}

	private static string[] GetNeighborRegionIds(string regionId) => regionId switch {
		"eastcoast" => new[] { "midwest", "deepsouth" },
		"midwest" => new[] { "eastcoast", "deepsouth", "rockies" },
		"deepsouth" => new[] { "eastcoast", "midwest", "southwest" },
		"southwest" => new[] { "deepsouth", "rockies", "westcoast" },
		"rockies" => new[] { "midwest", "southwest", "westcoast" },
		"westcoast" => new[] { "rockies", "southwest" },
		_ => Array.Empty<string>()
	};

	private void AssignChartPositions(List<RecordRuntimeData> sortedRecords, bool triggerEvents) {
		var wasOnChart = new HashSet<RecordRuntimeData>(
			allRecords.Where(r => r.currentPosition > 0)
		);
		var previousBubbling = new HashSet<RecordRuntimeData>(bubblingUnderPositions.Keys);
		bubblingUnderPositions.Clear();

		for (int i = 0; i < sortedRecords.Count; i++) {
			var record = sortedRecords[i];
			int newPosition = i + 1;
			bool isPublished = newPosition <= chartSize;

			if (!isPublished) {
				bubblingUnderPositions[record] = newPosition;
				if (record.currentPosition > 0) {
					record.lastWeekPosition = record.currentPosition;
					record.currentPosition = 0;
					record.isBullet = false;
					record.isAnchor = true;
					if (triggerEvents) OnRecordLeftChart?.Invoke(record);
				}
				wasOnChart.Remove(record);
				continue;
			}

			int internalPreviousPosition = record.currentPosition > 0
				? record.currentPosition
				: (previousBubbling.Contains(record) ? chartSize + 1 : 0);
			record.lastWeekPosition = internalPreviousPosition <= chartSize ? internalPreviousPosition : 0;

			if (record.currentPosition == 0) {
				if (record.weeksOnChart == 0) record.weeksOnChart = 1;
				else record.weeksOnChart++;
				if (triggerEvents) OnRecordEnteredChart?.Invoke(record);
			} else {
				record.weeksOnChart++;
			}

			// Update peak - ONLY if actually on chart
			if (newPosition > 0 && newPosition <= chartSize) {
				if (record.peakPosition == 0 || newPosition < record.peakPosition) {
					record.peakPosition = newPosition;
				}
			}

			// Hit #1 for first time
			if (newPosition == 1 && record.lastWeekPosition != 1) {
				if (triggerEvents) OnRecordHitNumberOne?.Invoke(record);
			}

			// Update peak
			if (record.peakPosition == 0 || newPosition < record.peakPosition) {
				record.peakPosition = newPosition;
			}

			if (newPosition <= 10) {
				record.weeksInTopTen++;
			} else {
				record.weeksInTopTen = Mathf.Max(0, record.weeksInTopTen - 1);
			}

			// Movement indicators
			if (record.lastWeekPosition > 0) {
				int movement = record.lastWeekPosition - newPosition;
				record.isBullet = movement >= 3;
				record.isAnchor = movement <= -3;
			} else {
				record.isBullet = newPosition <= 40;
				record.isAnchor = false;
			}

			record.currentPosition = newPosition;
			if (triggerEvents) OnRecordChartUpdated?.Invoke(record);
			wasOnChart.Remove(record);
		}

		// Records that fell off
		foreach (var record in wasOnChart) {
			record.lastWeekPosition = record.currentPosition;
			record.currentPosition = 0;
			record.isBullet = false;
			record.isAnchor = true;
			if (triggerEvents) OnRecordLeftChart?.Invoke(record);
		}
	}

	// ========================================================================
	// GENRE MOMENTUM
	// ========================================================================

	private void UpdateGenreMomentum() {
		foreach (Genre g in Enum.GetValues(typeof(Genre))) {
			genreMomentum[g] *= momentumDecayRate;
		}

		foreach (var record in currentChart) {
			Genre genre = record.baseRecord.primaryGenre;

			float positionScore = (chartSize - record.currentPosition + 1) * chartPositionWeight;
			float salesScore = record.unitsThisWeek * salesWeight;

			if (record.isBullet) {
				positionScore *= 1.5f;
			}

			genreMomentum[genre] += positionScore + salesScore;

			if (record.baseRecord.secondaryGenre != record.baseRecord.primaryGenre) {
				genreMomentum[record.baseRecord.secondaryGenre] += (positionScore + salesScore) * 0.3f;
			}
		}

		foreach (Genre g in Enum.GetValues(typeof(Genre))) {
			genreMomentum[g] = Mathf.Clamp(genreMomentum[g], -0.5f, 1f);
		}

		if (debugMode) {
			var topGenres = genreMomentum
				.OrderByDescending(kvp => kvp.Value)
				.Take(5);
			GD.Print($"Top genre momentum: {string.Join(", ", topGenres.Select(kvp => $"{kvp.Key}:{kvp.Value:F2}"))}");
		}
	}

	public float GetEffectiveGenreAcceptance(Genre genre) {
		float baseAcceptance = 0.5f;
		if (baseZeitgeist != null && baseZeitgeist.genreAcceptance.ContainsKey(genre)) {
			baseAcceptance = baseZeitgeist.genreAcceptance[genre];
		}

		float momentum = genreMomentum.ContainsKey(genre) ? genreMomentum[genre] : 0f;
		float adjusted = baseAcceptance + (momentum * momentumInfluence);

		return Mathf.Clamp(adjusted, 0.05f, 1f);
	}

	public float GetGenreMomentum(Genre genre) {
		return genreMomentum.ContainsKey(genre) ? genreMomentum[genre] : 0f;
	}

	// ========================================================================
	// HELPERS
	// ========================================================================

	private float GetGenreFit(Genre genre, MarketRegion region) {
		if (region.genrePreferences == null) return 0.75f;

		var pref = region.genrePreferences.FirstOrDefault(p => p.genre == genre);
		if (pref != null) {
			return 0.75f + (pref.affinity * 0.5f);
		}
		return 0.75f;
	}

	private void CullDeadRecords(bool includeChartedRecords) {
		var recordsToRetire = allRecords.Where(record => {
			if (record.currentPosition != 0 || record.unitsThisWeek >= RetirementSalesFloor) return false;

			bool neverChartedExpired = record.weeksOnChart == 0 &&
				record.weeksSinceRelease > NeverChartedHorizonWeeks;
			bool chartedExpired = includeChartedRecords &&
				record.weeksOnChart > 0 &&
				record.totalUnitsSold > 0 &&
				GetTotalRadioPlay(record) < 0.1f;

			return neverChartedExpired || chartedExpired;
		}).ToList();

		foreach (var record in recordsToRetire) RetireRecord(record);

		if (debugMode && recordsToRetire.Count > 0) {
			GD.Print($"ChartManager: Retired {recordsToRetire.Count} dead records. Active: {allRecords.Count}");
		}
	}

	private void RetireRecord(RecordRuntimeData record) {
		if (record?.baseRecord == null) return;

		var artist = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId);
		if (artist != null) {
			RosterManager.Instance?.RecordChartRunComplete(artist, record);
		}

		CompetitorManager.Instance?.RecordRetired(record.baseRecord.labelId, record.baseRecord.recordId);
		allRecords.Remove(record);
	}

	private float GetTotalRadioPlay(RecordRuntimeData record) {
		float total = 0f;
		foreach (var data in record.regionalData.Values) {
			total += data.radioPlay;
		}
		return total;
	}

	// ========================================================================
	// PUBLIC API
	// ========================================================================

	public List<MarketRegion> GetAllRegions() => new List<MarketRegion>(allRegions);

	public List<RecordRuntimeData> GetCurrentChart() => new List<RecordRuntimeData>(currentChart);

	public RecordRuntimeData GetRecordAtPosition(int position) {
		if (position > 0 && position <= currentChart.Count) {
			return currentChart[position - 1];
		}
		return null;
	}

	public List<RecordRuntimeData> GetPlayerRecords() {
		return allRecords.Where(r => r.baseRecord.isPlayerOwned).ToList();
	}

	public List<RecordRuntimeData> GetAllRecords() => new List<RecordRuntimeData>(allRecords);

	public int GetCurrentChartWeek() => currentChartWeek;

	public Zeitgeist GetCurrentZeitgeist() => baseZeitgeist;

	public void AddRadioPlay(string recordId, string regionId, float amount) {
		var record = allRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
		if (record != null && record.regionalData.ContainsKey(regionId)) {
			record.regionalData[regionId].radioPlay += amount;
			record.regionalData[regionId].awareness += amount * 0.1f;
			record.regionalData[regionId].awareness = Mathf.Clamp(record.regionalData[regionId].awareness, 0f, 1f);
		}
	}

	public void AddAwareness(string recordId, string regionId, float amount) {
		var record = allRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
		if (record != null && record.regionalData.ContainsKey(regionId)) {
			record.regionalData[regionId].awareness = Mathf.Clamp(
				record.regionalData[regionId].awareness + amount, 0f, 1f
			);
		}
	}

	public void SetSentiment(string recordId, string regionId, float value) {
		var record = allRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
		if (record != null && record.regionalData.ContainsKey(regionId)) {
			record.regionalData[regionId].sentiment = Mathf.Clamp(value, -1f, 1f);
		}
	}

	public void ModifySentiment(string recordId, string regionId, float delta) {
		var record = allRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
		if (record != null && record.regionalData.ContainsKey(regionId)) {
			record.regionalData[regionId].sentiment = Mathf.Clamp(
				record.regionalData[regionId].sentiment + delta, -1f, 1f
			);
		}
	}

	public void ShipRecords(string recordId, string regionId, int units) {
		var record = allRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
		if (record != null && record.regionalData.ContainsKey(regionId)) {
			record.regionalData[regionId].unitsInStores += units;
		}
	}

	public RegionalRecordData GetRegionalData(string recordId, string regionId) {
		var record = allRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
		if (record != null && record.regionalData.ContainsKey(regionId)) {
			return record.regionalData[regionId];
		}
		return null;
	}

	/// <summary>
	/// Returns records currently charting by a specific artist
	/// </summary>
	public List<RecordRuntimeData> GetArtistChartingRecords(string artistId) {
		return allRecords
			.Where(r => r.baseRecord.artistId == artistId && r.currentPosition > 0)
			.OrderBy(r => r.currentPosition)
			.ToList();
	}

	/// <summary>
	/// Returns total sales for an artist across all their records
	/// </summary>
	public int GetArtistTotalSales(string artistId) {
		return allRecords
			.Where(r => r.baseRecord.artistId == artistId)
			.Sum(r => r.totalUnitsSold);
	}

	/// <summary>
	/// Returns records by label
	/// </summary>
	public List<RecordRuntimeData> GetLabelRecords(string labelId) {
		return allRecords
			.Where(r => r.baseRecord.labelId == labelId)
			.ToList();
	}

	/// <summary>
	/// Returns charting records by label
	/// </summary>
	public List<RecordRuntimeData> GetLabelChartingRecords(string labelId) {
		return allRecords
			.Where(r => r.baseRecord.labelId == labelId && r.currentPosition > 0)
			.OrderBy(r => r.currentPosition)
			.ToList();
	}

	/// <summary>
	/// Returns top N records by total sales
	/// </summary>
	public List<RecordRuntimeData> GetTopSellingRecords(int count) {
		return allRecords
			.OrderByDescending(r => r.totalUnitsSold)
			.Take(count)
			.ToList();
	}

	/// <summary>
	/// Returns records that have hit #1
	/// </summary>
	public List<RecordRuntimeData> GetNumberOneHits() {
		return allRecords
			.Where(r => r.peakPosition == 1)
			.OrderByDescending(r => r.totalUnitsSold)
			.ToList();
	}

	/// <summary>
	/// Returns records in a specific genre currently on the chart
	/// </summary>
	public List<RecordRuntimeData> GetChartingByGenre(Genre genre) {
		return currentChart
			.Where(r => r.baseRecord.primaryGenre == genre || r.baseRecord.secondaryGenre == genre)
			.ToList();
	}

	// ========================================================================
	// DEBUG
	// ========================================================================

	public void DebugPrintTopTen() {
		GD.Print($"=== BILLBOARD HOT 100 - Week {currentChartWeek} ===");
		for (int i = 0; i < Mathf.Min(10, currentChart.Count); i++) {
			var record = currentChart[i];

			string movement;
			if (record.lastWeekPosition == 0) {
				movement = "NEW";
			} else if (record.isBullet) {
				movement = $"▲{record.lastWeekPosition - record.currentPosition}";
			} else if (record.isAnchor) {
				movement = $"▼{record.currentPosition - record.lastWeekPosition}";
			} else if (record.lastWeekPosition > record.currentPosition) {
				movement = $"+{record.lastWeekPosition - record.currentPosition}";
			} else if (record.lastWeekPosition < record.currentPosition) {
				movement = $"-{record.currentPosition - record.lastWeekPosition}";
			} else {
				movement = "=";
			}

			GD.Print($"#{record.currentPosition} [{movement}] \"{record.baseRecord.title}\" - {record.baseRecord.artistName} ({record.baseRecord.primaryGenre}) | {record.unitsThisWeek:N0} units | Wks: {record.weeksOnChart}");
		}
	}

	public void DebugPrintTopForty() {
		GD.Print($"=== BILLBOARD HOT 100 TOP 40 - Week {currentChartWeek} ===");
		for (int i = 0; i < Mathf.Min(40, currentChart.Count); i++) {
			var record = currentChart[i];

			string movement;
			if (record.lastWeekPosition == 0) {
				movement = "NEW";
			} else if (record.lastWeekPosition > record.currentPosition) {
				movement = $"+{record.lastWeekPosition - record.currentPosition}";
			} else if (record.lastWeekPosition < record.currentPosition) {
				movement = $"-{record.currentPosition - record.lastWeekPosition}";
			} else {
				movement = "=";
			}

			string label = GetLabelName(record.baseRecord.labelId);
			GD.Print($"#{record.currentPosition} [{movement}] \"{record.baseRecord.title}\" - {record.baseRecord.artistName} | {label} | {record.unitsThisWeek:N0} units | Peak: {record.peakPosition}");
		}
	}

	public void DebugPrintGenreMomentum() {
		GD.Print("=== GENRE MOMENTUM ===");
		var sorted = genreMomentum
			.OrderByDescending(kvp => kvp.Value)
			.ToList();

		foreach (var (genre, momentum) in sorted) {
			float baseAccept = baseZeitgeist?.genreAcceptance.GetValueOrDefault(genre, 0.5f) ?? 0.5f;
			float effective = GetEffectiveGenreAcceptance(genre);
			GD.Print($"{genre}: Base={baseAccept:F2} Momentum={momentum:F3} Effective={effective:F2}");
		}
	}

	public void DebugPrintLabelStats() {
		GD.Print("=== LABEL STATISTICS ===");

		var labelStats = allRecords
			.GroupBy(r => r.baseRecord.labelId)
			.Select(g => new {
				LabelId = g.Key,
				TotalRecords = g.Count(),
				ChartingRecords = g.Count(r => r.currentPosition > 0),
				TotalSales = g.Sum(r => r.totalUnitsSold),
				NumberOnes = g.Count(r => r.peakPosition == 1),
				TopTens = g.Count(r => r.peakPosition <= 10 && r.peakPosition > 0)
			})
			.OrderByDescending(s => s.TotalSales)
			.Take(15);

		foreach (var stats in labelStats) {
			string labelName = GetLabelName(stats.LabelId);
			GD.Print($"{labelName}: {stats.ChartingRecords}/{stats.TotalRecords} charting | {stats.TotalSales:N0} total sales | #1s: {stats.NumberOnes} | Top 10s: {stats.TopTens}");
		}
	}

	public void DebugPrintRegionStats() {
		GD.Print("=== REGIONAL STATISTICS ===");

		foreach (var region in allRegions) {
			float avgAwareness = 0f;
			float avgRadioPlay = 0f;
			int totalStoreUnits = 0;
			int recordCount = 0;

			foreach (var record in allRecords) {
				if (record.regionalData.TryGetValue(region.regionId, out var data)) {
					avgAwareness += data.awareness;
					avgRadioPlay += data.radioPlay;
					totalStoreUnits += data.unitsInStores;
					recordCount++;
				}
			}

			if (recordCount > 0) {
				avgAwareness /= recordCount;
				avgRadioPlay /= recordCount;
			}

			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			GD.Print($"{region.regionName}: Pop={region.population:F1}M | Stores={region.distribution.recordStoreCount}+{region.distribution.departmentStoreCount} | Stations={region.media.totalRadioStations} (diff={radioDifficulty:F2}) | AvgAware={avgAwareness:F2} | AvgRadio={avgRadioPlay:F2}");
		}
	}

	public void DebugPrintSimulationHealth() {
		GD.Print("=== SIMULATION HEALTH CHECK ===");

		int activeRecords = allRecords.Count;
		int chartingRecords = currentChart.Count;
		int highMomentum = allRecords.Count(r => r.momentum > 0.1f);
		int negativeMomentum = allRecords.Count(r => r.momentum < -0.1f);
		int highAwareness = allRecords.Count(r => r.awareness > 0.5f);
		int noSales = allRecords.Count(r => r.unitsThisWeek == 0 && r.currentPosition > 0);

		float avgSales = currentChart.Count > 0 ? (float)currentChart.Average(r => r.unitsThisWeek) : 0f;
		float maxSales = currentChart.Count > 0 ? currentChart.Max(r => r.unitsThisWeek) : 0f;
		float avgMomentum = allRecords.Count > 0 ? (float)allRecords.Average(r => r.momentum) : 0f;

		GD.Print($"Total Records: {activeRecords}");
		GD.Print($"On Chart: {chartingRecords}");
		GD.Print($"High Momentum (>0.1): {highMomentum}");
		GD.Print($"Negative Momentum (<-0.1): {negativeMomentum}");
		GD.Print($"High Awareness (>0.5): {highAwareness}");
		GD.Print($"Zero Sales on Chart: {noSales}");
		GD.Print($"Avg Weekly Sales (chart): {avgSales:N0}");
		GD.Print($"Max Weekly Sales: {maxSales:N0}");
		GD.Print($"Avg Momentum: {avgMomentum:F3}");

		if (noSales > 10) GD.PushWarning("WARNING: Many charting records with zero sales!");
		if (avgSales < 1000) GD.PushWarning("WARNING: Average sales seem too low!");
		if (avgSales > 100000) GD.PushWarning("WARNING: Average sales seem too high!");
		if (highMomentum < 5) GD.PushWarning("WARNING: Very few records with positive momentum!");
	}

	public void DebugForceCalculate() {
		CalculateChart();
		UpdateGenreMomentum();
	}
}
