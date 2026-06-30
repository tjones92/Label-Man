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

		if (currentChartWeek % 4 == 0) {
			CullDeadRecords();
		}
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

		float quality = runtimeData.GetQuality();
		float labelPush = releasingLabel != null ? releasingLabel.marketingPower * releasingLabel.budgetLevel : 0.2f;

		runtimeData.awareness = 0.12f + (quality * 0.08f) + (labelPush * 0.15f);
		runtimeData.radioHeat = 0.08f + (labelPush * 0.12f);

		foreach (var region in allRegions) {
			var regionalData = new RegionalRecordData(region.regionId);
			runtimeData.regionalData[region.regionId] = regionalData;
		}

		allRecords.Add(runtimeData);

		if (releasingLabel != null && !record.isPlayerOwned) {
			PromoteRecordAI(runtimeData, releasingLabel);
		}

		if (debugMode) GD.Print($"Released: {record.title} by {record.artistName} (awareness: {runtimeData.awareness:F2}, radio: {runtimeData.radioHeat:F2})");
	}

	private void PromoteRecordAI(RecordRuntimeData record, AILabel label) {
		float minAwareness = label.tier switch {
			LabelTier.Major => 0.25f,
			LabelTier.MidTier => 0.18f,
			LabelTier.Independent => 0.12f,
			LabelTier.Small => 0.08f,
			LabelTier.Boutique => 0.10f,
			_ => 0.10f
		};

		record.awareness = Mathf.Max(minAwareness, record.awareness);
		record.awareness += label.marketingPower * label.budgetLevel * 0.15f;
		record.awareness = Mathf.Clamp(record.awareness, 0f, 1f);

		record.radioHeat = Mathf.Max(0.1f, record.radioHeat);
		record.radioHeat += label.marketingPower * label.budgetLevel * 0.12f;
		record.radioHeat = Mathf.Clamp(record.radioHeat, 0f, 1f);

		foreach (var region in allRegions) {
			if (!record.regionalData.ContainsKey(region.regionId)) continue;

			var data = record.regionalData[region.regionId];
			float regionStrength = label.strongRegions.Contains(region.regionId) ? 1.8f : 1f;

			int baseStock = label.tier switch {
				LabelTier.Major => 15000,
				LabelTier.MidTier => 8000,
				LabelTier.Independent => 4000,
				LabelTier.Small => 2000,
				LabelTier.Boutique => 3000,
				_ => 3000
			};

			int units = Mathf.RoundToInt(
				baseStock *
				regionStrength *
				label.distributionStrength *
				(0.7f + region.distribution.inventoryDepth * 0.3f) *
				(float)GD.RandRange(0.8, 1.2)
			);

			units = Mathf.Max(units, 1500);
			data.unitsInStores = units;

			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			data.radioPlay = (0.15f + (float)GD.RandRange(0.1, 0.25)) * label.budgetLevel * regionStrength / radioDifficulty;
			data.awareness = (0.15f + (float)GD.RandRange(0.05, 0.15)) * label.budgetLevel * regionStrength;

			float quality = (record.baseRecord.hookStrength + record.baseRecord.productionQuality) / 2f;
			float genreFit = GetGenreFit(record.baseRecord.primaryGenre, region);
			data.sentiment = (quality * 0.7f + genreFit * 0.3f) + (float)GD.RandRange(-0.05, 0.1);
		}

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
					blendedAcceptance
				);

				regionalData.unitsInStores = Mathf.Max(0, regionalData.unitsInStores - regionalSales);
				regionalData.unitsSoldThisWeek = regionalSales;
				regionalData.unitsSoldTotal += regionalSales;

				totalSales += regionalSales;
			}

			ChartSimulator.FinalizeWeeklySales(record, totalSales);
		}

		// === STEP 2.5: RESTOCK HOT RECORDS ===
		RestockHotRecords();

		// === STEP 3: Update regional awareness/radio ===
		foreach (var record in allRecords) {
			UpdateRecordRegionalData(record);
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
		var sortedByPoints = chartPoints
			.OrderByDescending(kvp => kvp.Value)
			.ThenByDescending(kvp => kvp.Key.unitsThisWeek)
			.Select(kvp => kvp.Key)
			.Take(chartSize)
			.ToList();

		// === STEP 6: Apply position calculations ===
		AssignChartPositions(sortedByPoints, triggerEvents);
		currentChart = sortedByPoints;
	}

	private void RestockHotRecords() {
		foreach (var record in allRecords) {
			if (record.currentPosition <= 0) continue;

			AILabel label = GetLabelById(record.baseRecord.labelId);
			if (label == null) continue;

			foreach (var region in allRegions) {
				if (!record.regionalData.TryGetValue(region.regionId, out var data)) continue;

				bool needsRestock = data.unitsBackordered > 500 ||
								(data.unitsInStores < data.unitsSoldThisWeek * 2 && record.currentPosition <= 40);

				if (needsRestock) {
					float demandSignal = data.unitsSoldThisWeek + (data.unitsBackordered * 0.5f);
					int restockAmount = Mathf.RoundToInt(demandSignal * label.distributionStrength * 1.5f);

					int maxCapacity = region.distribution.recordStoreCount * 100 +
									region.distribution.departmentStoreCount * 200;
					restockAmount = Mathf.Min(restockAmount, maxCapacity - data.unitsInStores);

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

	private void AssignChartPositions(List<RecordRuntimeData> sortedRecords, bool triggerEvents) {
		var wasOnChart = new HashSet<RecordRuntimeData>(
			allRecords.Where(r => r.currentPosition > 0)
		);

		for (int i = 0; i < sortedRecords.Count; i++) {
			var record = sortedRecords[i];
			int newPosition = i + 1;

			record.lastWeekPosition = record.currentPosition;

			if (record.currentPosition == 0) {
				record.weeksOnChart = 1;
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

	private void CullDeadRecords() {
		int preCount = allRecords.Count;

		allRecords.RemoveAll(r =>
			r.currentPosition == 0 &&
			r.weeksOnChart > 0 &&
			r.unitsThisWeek < 50 &&
			r.totalUnitsSold > 0 &&
			GetTotalRadioPlay(r) < 0.1f
		);

		if (debugMode && preCount != allRecords.Count) {
			GD.Print($"ChartManager: Culled {preCount - allRecords.Count} dead records. Active: {allRecords.Count}");
		}
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
