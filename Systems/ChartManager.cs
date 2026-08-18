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
	[Export] private bool marketSeasonalityEnabled = true;
	// NOTE (player slice): chart_manager.tscn OVERRIDES both of these to true so the
	// game is playable when launched with no command-line flags -- the player-facing
	// desk scouts the unsigned talent market, which only exists on the enabled
	// lifecycle path. These field defaults stay false; the scene is the override.
	//
	// Before the next headless run: either pass the flags explicitly (every documented
	// run command already does, and an explicit flag still wins over the scene default),
	// or set genreMarketV2Enabled/artistPopulationLifecycleEnabled back to false on the
	// ChartManager node in chart_manager.tscn. The one case the override silently
	// changes is a headless run that passes NEITHER --enable-* nor --disable-*: that
	// run used to be the disabled path and is now the enabled one.
	[Export] private bool genreMarketV2Enabled = false;
	[Export] private bool artistPopulationLifecycleEnabled = false;
	[Export] private bool artistEvolutionEnabled = false;
	[Export] private bool artistRecognitionEnabled = false;

	[ExportGroup("AI Labels")]
	private List<AILabel> aiLabels;

	[ExportGroup("Genre Momentum Settings")]
	[Export] private float momentumDecayRate = 0.9f;
	[Export] private float momentumInfluence = 0.3f;
	public float GenreMomentumInfluence => momentumInfluence;
	public bool IsGenreMarketV2Live => GenreMarketV2.Enabled && currentChartWeek > 0;
	[Export] private float chartPositionWeight = 0.01f;
	[Export] private float salesWeight = 0.00001f;

	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;

	public RecordRuntimeData GetRecordRuntimeData(string recordId) {
		return recordById.TryGetValue(recordId ?? string.Empty, out RecordRuntimeData record) ? record : null;
	}

	// Runtime state
	private int currentChartWeek;
	private bool canonicalLiveIdentitiesApplied;
	private Zeitgeist baseZeitgeist;
	private Dictionary<Genre, float> genreMomentum;
	private List<RecordRuntimeData> allRecords = new List<RecordRuntimeData>();
	private readonly Dictionary<string, RecordRuntimeData> recordById = new(StringComparer.Ordinal);
	private List<RecordRuntimeData> currentChart = new List<RecordRuntimeData>();
	private List<RecordRuntimeData> currentAlbumChart = new List<RecordRuntimeData>();
	// The reporter-station panel (radio design docs a/b/f). Owned here, run inline in SimulateWeek
	// like ChartSimulator. Phase 1: built and aged by era events but INERT -- its output does not
	// reach radioPlay until the Phase-2 aggregation. Uses its own RNG (never the global GD stream)
	// so standing it up cannot perturb the seeded simulation.
	private StationNetwork stationNetwork;
	// The payola ledger (radio doc d). Player-facing; AI labels don't use it, so it is inert in
	// headless audits (no arrangements -> ActivePayola returns 0 -> candidacy unchanged).
	private PayolaLedger payolaLedger;
	// Salt so the station RNG is a distinct-but-reproducible stream off the audit seed.
	private const ulong StationSeedSalt = 0x5241_4449_4F2AUL; // "RADIO*"
	private const ulong PayolaSeedSalt = 0x5041_594F_4C41UL;  // "PAYOLA"
	private const int BubblingUnderSize = 15;
	// Weekly persistence of regional airplay. Hold is what survives from last week before the pull
	// toward the current national target; together they set how fast a record leaves rotation once
	// its heat collapses. Load-bearing only since airplay reached the chart ranking.
	private const float RegionalRadioHold = 0.92f;
	private const float RegionalRadioLerp = 0.15f;
	// Reporters ARE the survey panel; the tail carries reach (radio design doc a 3.5). radioPlay is
	// Lerp(tail, reporter, REPORTER_PANEL_WEIGHT). Held at 0 through the Phase-2a plumbing swap so
	// radioPlay == tail == the former formula (byte-identical); raised in Phase 2b once the reporter
	// playlists are proven and the economic effect can be measured against the V3.1 baseline.
	private const float REPORTER_PANEL_WEIGHT = 0.13f;
	private const int NeverChartedHorizonWeeks = 5;
	private const int NeverChartedMaximumAgeWeeks = 18;
	private const int ChartedRelevanceHorizonWeeks = 8;
	private const int RetirementSalesFloor = 50;
	private readonly Dictionary<string, float> regionalDemandScaleById = new(StringComparer.Ordinal);
	[ExportGroup("Album Catalog")]
	[Export] private int albumCatalogSalesFloor = 10;
	[Export] private int albumNeverChartedToleranceWeeks = 26;
	[Export] private int albumChartedToleranceWeeks = 52;
	private const float RetirementRegionRadioCap = 0.05f;
	private Dictionary<RecordRuntimeData, int> bubblingUnderPositions = new Dictionary<RecordRuntimeData, int>();
	private Dictionary<RecordRuntimeData, int> albumBubblingUnderPositions = new Dictionary<RecordRuntimeData, int>();
	private readonly Dictionary<string, AlbumTrack> retiredTrackArchive = new();
	[Export(PropertyHint.Range, "0,1,0.01")] private float compStalenessFactor = 0.70f;
	private readonly Dictionary<string, int> compUseCountByRecordId = new();
	private Dictionary<RecordRuntimeData, float> previousChartPoints = new Dictionary<RecordRuntimeData, float>();
	private Dictionary<string, AILabel> labelLookup = new Dictionary<string, AILabel>();
	private const float WeeklyRegionalPurchaseCapacityMultiplier = 1.34f;
	// Album title intents substantially overlap because each title is evaluated
	// against the same format buyer pool. Convert their summed serviceable intent
	// into one market-wide format opportunity before common regional clearing.
	private const float AlbumIntentOverlapPressure = 2f;
	// LP-RATIO RECALIBRATION (2026-08). The album channel capacity (= baseCapacity x these shares x
	// era) now SETS album unit share, because album intent saturates it. Sized to the author's LP:45
	// unit targets: album/single ratio ~0.43 at 1960 (LP 30%) rising to ~1.22 at 1969 (LP 55%), read
	// against GetAlbumDemandEraProgress (rise 1957->1972, so era ~0.20 at 1960, ~0.80 at 1969).
	// Album unit share ~= share/(1+share) once demand saturates the channel.
	// CONVEX RESHAPE (2026-08): the former linear form (0.17 + 1.30 x era) nailed the 1960/1967/1969 LP
	// anchors but ran the mid-decade ~6 pts hot (1963 40.8 vs 35, 1965 46.3 vs 40) -- the LP:45 target
	// curve dips below the endpoint chord in the middle, which a straight line cannot follow. Refit as a
	// convex quadratic in era: least-squares through the author LP anchors 30/35/40/50/55 at 60/63/65/
	// 67/69, each mapped to share = LP/(1-LP). Vertex at era ~0.15 (below the 0.20 floor), so monotone
	// increasing across the in-sim range [0.20,0.80]. Realized LP now ~29.7/35.1/41.4/48.5/55.5.
	private const float AlbumChannelShareEraC0 = 0.46136f;   // era^0
	private const float AlbumChannelShareEraC1 = -0.58405f;  // era^1
	private const float AlbumChannelShareEraC2 = 1.95531f;   // era^2
	// A donor contributes only portable purchase opportunity left idle by its own
	// local market.  These bounds deliberately prevent the region graph from
	// becoming a disguised national pool.
	private const float SpilloverMaximumExportShare = 0.75f;
	private const float SpilloverMaximumImportShare = 0.15f;
	// The breakout score at which a record enters LocalTraction and, with it, the
	// self-reinforcing discovery basin (ApplyBreakoutDiscovery adds awareness and radio
	// that feed back into evidence). Below this a record neither collapses (that floor is
	// 0.18) nor climbs — it stalls. Offer-attempt telemetry showed a large Independent/
	// Boutique/Small population peaking in the resulting 0.18-0.24 limbo and signing at
	// only ~35% against ~84% just above it, which is where the decade's charting breadth
	// was being lost. Lowered 0.24 -> 0.20 to admit the upper part of that band to the
	// discovery ramp while leaving the 0.18 collapse floor intact. Incumbents sit at the
	// 0.40 RegionalBreakout stage with their discovery gains already capped, so the relief
	// is confined to the tail. regionalBreakoutDealThreshold in CompetitorManager tracks
	// this value.
	private const float LocalTractionActivationScore = 0.20f;
	private readonly List<MarketClearingRegionalSummary> lastMarketClearingSummaries = new();
	private readonly List<MarketSpilloverTransfer> lastMarketSpilloverTransfers = new();
	private CompletedWeekSettlement lastCompletedWeekSettlement;

	public sealed class MarketClearingRegionalSummary {
		public string RegionId;
		public int ActiveIntentCount, BasePurchaseCapacity, AlbumChannelCapacity, PurchaseCapacity, ClearedSingleUnits, ClearedAlbumUnits, PhysicalBackorders, MarketDisplacedDemand;
		public int SingleFormatBudget, AlbumFormatBudget;
		public int LocalClearedUnits, UnusedAfterLocal, ExportBudget, ExportedCapacity, ImportLimit, ImportedCapacity, SpilloverClearedUnits;
		public float RawSingleDemand, RawAlbumDemand, ServiceableSingleIntent, ServiceableAlbumIntent, EffectiveAlbumIntent, AlbumOverlapPressure;
		public int InventoryViolationCount, AllocationViolationCount, ReconciliationDelta;
		public int ClearedTotalUnits => ClearedSingleUnits + ClearedAlbumUnits;
		public int ServiceableTotalIntent => Mathf.RoundToInt(ServiceableSingleIntent + ServiceableAlbumIntent);
	}
	public sealed class MarketSpilloverTransfer {
		public string DonorRegionId;
		public string RecipientRegionId;
		public int DonorUnusedLocal, DonorExportBudget, RecipientResidualDemand, RecipientImportLimit, TransferredCapacity;
		public int ClearedSingleUnits, ClearedAlbumUnits, EdgeViolationCount, ReconciliationDelta;
	}
	public sealed class CompletedWeekSettlement {
		public int SettlementId;
		public GameDate Date;
		public IReadOnlyList<CompletedWeekSettlementEntry> Entries;
		public bool IsBooked;
		public bool IsAuditAcknowledged;
		public int TotalUnits => Entries?.Sum(entry => entry.Units) ?? 0;

		// Booking filtered the whole frozen entry list once per label and retirement scanned
		// it once per retired record: 1,496 labels x 17,030 entries a week by 1969, roughly six
		// billion string comparisons across the decade. Both indexes are built once per frozen
		// entry list on first use. Each label's list keeps source order, so per-label iteration
		// -- and therefore the order revenue floats accumulate in -- is unchanged.
		private static readonly CompletedWeekSettlementEntry[] NoEntries = Array.Empty<CompletedWeekSettlementEntry>();
		private IReadOnlyList<CompletedWeekSettlementEntry> indexedEntries;
		private Dictionary<string, List<CompletedWeekSettlementEntry>> entriesByLabelId;
		private Dictionary<string, CompletedWeekSettlementEntry> entryByRecordId;

		public IReadOnlyList<CompletedWeekSettlementEntry> EntriesForLabel(string labelId) {
			EnsureIndexes();
			return entriesByLabelId != null &&
				entriesByLabelId.TryGetValue(labelId ?? string.Empty, out List<CompletedWeekSettlementEntry> forLabel)
					? forLabel : NoEntries;
		}

		/// <summary>The first entry for a record id, matching the linear scan this replaces.</summary>
		public CompletedWeekSettlementEntry FindEntry(string recordId) {
			EnsureIndexes();
			return entryByRecordId != null &&
				entryByRecordId.TryGetValue(recordId ?? string.Empty, out CompletedWeekSettlementEntry entry)
					? entry : null;
		}

		private void EnsureIndexes() {
			if (Entries == null) {
				indexedEntries = null;
				entriesByLabelId = null;
				entryByRecordId = null;
				return;
			}
			if (ReferenceEquals(indexedEntries, Entries)) return;
			var byLabel = new Dictionary<string, List<CompletedWeekSettlementEntry>>(StringComparer.Ordinal);
			var byRecord = new Dictionary<string, CompletedWeekSettlementEntry>(StringComparer.Ordinal);
			foreach (CompletedWeekSettlementEntry entry in Entries) {
				// Both ids are non-null for every entry a settlement is built from; the coalesce
				// only keeps the dictionary key legal and does not change which entries match.
				string labelKey = entry.LabelId ?? string.Empty;
				if (!byLabel.TryGetValue(labelKey, out List<CompletedWeekSettlementEntry> forLabel)) {
					forLabel = new List<CompletedWeekSettlementEntry>();
					byLabel[labelKey] = forLabel;
				}
				forLabel.Add(entry);
				string recordKey = entry.RecordId ?? string.Empty;
				if (!byRecord.ContainsKey(recordKey)) byRecord[recordKey] = entry;
			}
			entriesByLabelId = byLabel;
			entryByRecordId = byRecord;
			indexedEntries = Entries;
		}
	}
	public sealed class CompletedWeekSettlementEntry {
		public RecordRuntimeData Record;
		public string RecordId, LabelId, LabelTier, Genre;
		public ReleaseFormat Format;
		public int Units;
		public IReadOnlyList<CompletedWeekSettlementRegion> Regions;
		public float Gross, ManufacturingCost, ArtistRoyalty, DistributionSkim, LabelNet, DistributionIncome, MarketNet;
		public string DistributionRecipientLabelId;
		public int BookedCount, AuditedCount;
		public bool RetiredAfterSettlement;
	}
	public sealed class CompletedWeekSettlementRegion {
		public string RegionId;
		public int RawIntent, ServiceableIntent, LocalCleared, SpilloverCleared, FinalCleared;
		public int PhysicalBackorders, MarketDisplacedDemand, InventoryMovement;
	}
	private sealed class MarketIntent {
		public RecordRuntimeData Record;
		public RegionalRecordData Data;
		public int Serviceable, SpilloverCleared;
		public int Cleared;
		public float Fraction;
	}

	internal readonly struct FormatClearingBudget {
		public readonly int Single, Album;
		public readonly float EffectiveAlbum;
		public FormatClearingBudget(int single, int album, float effectiveAlbum) {
			Single = single;
			Album = album;
			EffectiveAlbum = effectiveAlbum;
		}
	}
	public IReadOnlyList<MarketClearingRegionalSummary> GetLastMarketClearingSummaries() => lastMarketClearingSummaries;
	public IReadOnlyList<MarketSpilloverTransfer> GetLastMarketSpilloverTransfers() => lastMarketSpilloverTransfers;
	public CompletedWeekSettlement GetLastCompletedWeekSettlement() => lastCompletedWeekSettlement;
	public int GetAlbumCatalogSalesFloor() => albumCatalogSalesFloor;

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
	public event Action<List<RecordRuntimeData>> OnAlbumChartCalculated;
	public event Action<RecordRuntimeData> OnRecordEnteredChart;
	public event Action<RecordRuntimeData> OnRecordHitNumberOne;
	public event Action<RecordRuntimeData> OnRecordChartUpdated;
	public event Action<RecordRuntimeData> OnRecordLeftChart;
	public event Action<RecordRuntimeData> OnRecordRetired;
	public event Action<Genre, float> OnGenreMomentumChanged;
	/// <summary>Raised once after sales are frozen and before any record can retire.</summary>
	public event Action<CompletedWeekSettlement> OnWeekSettlement;
	public int RetiredTrackResolutionAttempts { get; private set; }
	public int RetiredTrackResolutionMisses { get; private set; }
	public int RetiredTrackArchiveHits { get; private set; }

	// ========================================================================
	// GODOT LIFECYCLE
	// ========================================================================

	public override void _EnterTree() {
		if (Instance != null && Instance != this) {
			QueueFree();
			return;
		}
		Instance = this;
		// Resolve the scene default and any audit override before population or
		// prewarming can consume simulation state.
		MarketSeasonality.Configure(marketSeasonalityEnabled, OS.GetCmdlineUserArgs());
		GenreMarketV2.Configure(genreMarketV2Enabled, OS.GetCmdlineUserArgs());
		ArtistPopulationLifecycle.Configure(artistPopulationLifecycleEnabled, OS.GetCmdlineUserArgs());
		ArtistEvolution.Configure(artistEvolutionEnabled, OS.GetCmdlineUserArgs());
		ArtistRecognition.Configure(artistRecognitionEnabled, OS.GetCmdlineUserArgs());
		GenreSupplyService.Configure(OS.GetCmdlineUserArgs());

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
			// The enabled lifecycle needs a real unsigned talent market, but that
			// reserve must not inflate or reorder the frozen 3,000-artist launch
			// roster allocation. Generate it afterward on the isolated population
			// stream, before pre-warm or live scouting begins.
			ArtistManager.Instance?.MaterializeEnabledInitialUnsignedReserve(year);
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
		if (LabelLifecycleManager.Instance != null && aiLabels != null) {
			LabelLifecycleManager.Instance.InitializeLabels(aiLabels, year);
		}

		// 5. Initialize regions
		InitializeRegions();

		// 6. Set zeitgeist
		UpdateBaseZeitgeist(year);

		// 6b. Build the reporter-station panel. Regions already have segmentCapacities (step 5), and
		// the audit seed is applied pre-autoload, so the roster is reproducible. Inert in Phase 1.
		ulong stationSeed = (SimulationSeedBootstrap.RequestedSeed ?? 1UL) ^ StationSeedSalt;
		stationNetwork = new StationNetwork(stationSeed);
		stationNetwork.BuildRosters(allRegions, year);
		payolaLedger = new PayolaLedger(stationNetwork, stationSeed ^ PayolaSeedSalt);
		stationNetwork.ActivePayolaLookup = payolaLedger.ActivePayola;   // candidacy reads player bribes here
		GD.Print($"ChartManager: Station panel built -- {stationNetwork.StationCount} reporter stations across {allRegions?.Length ?? 0} regions");
		stationNetwork.LogProjectedMix(allRegions);

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
		foreach (Genre g in GenreDomains.Current) {
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

	public void RegisterLabel(AILabel label) {
		if (label == null || string.IsNullOrEmpty(label.labelId)) return;
		if (aiLabels != null && !aiLabels.Contains(label)) aiLabels.Add(label);
		labelLookup[label.labelId] = label;
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
	// PRE-WARMING
	// ========================================================================

	private void PrewarmSimulation() {
		if (debugMode) GD.Print("ChartManager: Pre-warming simulation...");
		foreach (var region in allRegions) region.SetGenreMarketV2Live(false);

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
		RebuildRecordIndex();

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
		ApplyCanonicalLiveIdentities(date.year);
		foreach (var region in allRegions) region.SetGenreMarketV2Live(true);

		SimulateWeek(triggerEvents: true);
		if (GenreMarketV2.Enabled) {
			long freezeProfileStart = SimulationPerformanceProfiler.Begin();
			FreezeCompletedWeekSettlement(date);
			SimulationPerformanceProfiler.EndFreezeSettlement(freezeProfileStart);
			// Booking is an explicit state transition, not an unordered event subscriber.
			// It must complete before the immutable settlement becomes visible to audit.
			long bookProfileStart = SimulationPerformanceProfiler.Begin();
			CompetitorManager.Instance?.BookCompletedWeekSettlement(lastCompletedWeekSettlement);
			SimulationPerformanceProfiler.EndBookSettlement(bookProfileStart);
			if (lastCompletedWeekSettlement?.IsBooked != true)
				throw new InvalidOperationException($"Settlement {lastCompletedWeekSettlement?.SettlementId} was not booked.");
			long auditEventProfileStart = SimulationPerformanceProfiler.Begin();
			OnWeekSettlement?.Invoke(lastCompletedWeekSettlement);
			SimulationPerformanceProfiler.EndSettlementAuditEvent(auditEventProfileStart);
			if (OnWeekSettlement != null && lastCompletedWeekSettlement?.IsAuditAcknowledged != true)
				throw new InvalidOperationException($"Settlement {lastCompletedWeekSettlement.SettlementId} was not acknowledged by its audit consumer.");
		}

		foreach (var record in allRecords) {
			if (record.isGrammyWinner && record.weeksOfGrammyBump > 0) {
				record.weeksOfGrammyBump--;
			}
		}

		long momentumProfileStart = SimulationPerformanceProfiler.Begin();
		UpdateGenreMomentum();
		SimulationPerformanceProfiler.EndGenreMomentum(momentumProfileStart);

		long cullProfileStart = SimulationPerformanceProfiler.Begin();
		CullDeadRecords(includeChartedRecords: currentChartWeek % 4 == 0);
		SimulationPerformanceProfiler.EndCullDeadRecords(cullProfileStart);
		// Directive 6 owns one explicit post-chart sequence.  Formation is never
		// reached by prewarm because this method only runs on a live weekly tick.
		long lifecycleProfileStart = SimulationPerformanceProfiler.Begin();
		ArtistManager.Instance?.AdvancePopulationLifecycle(date);
		SimulationPerformanceProfiler.EndPopulationLifecycle(lifecycleProfileStart);
	}

	private void FreezeCompletedWeekSettlement(GameDate date) {
		lastCompletedWeekSettlement = new CompletedWeekSettlement {
			SettlementId = currentChartWeek,
			// Audit checkpoints describe the state reached by this completed tick.
			// TimeManager advances to that next-week checkpoint after the synchronous
			// Friday callback, so key settlement years to the same calendar boundary.
			Date = date.AddDays(7),
			Entries = allRecords.Select(record => {
				bool retirementEligible = IsRecordRetirable(record, currentChartWeek % 4 == 0);
				var entry = new CompletedWeekSettlementEntry {
					Record = record,
					RecordId = record.baseRecord.recordId,
					LabelId = record.baseRecord.labelId,
					LabelTier = GetLabelById(record.baseRecord.labelId)?.tier.ToString() ?? "Unknown",
					Genre = record.baseRecord.primaryGenre.ToString(),
					Format = record.baseRecord.format,
					Units = record.unitsThisWeek,
					RetiredAfterSettlement = retirementEligible,
					Regions = record.regionalData.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new CompletedWeekSettlementRegion {
						RegionId = pair.Key,
						RawIntent = Mathf.RoundToInt(pair.Value.rawDemandThisWeek),
						ServiceableIntent = pair.Value.serviceableIntentThisWeek,
						FinalCleared = pair.Value.unitsSoldThisWeek,
						LocalCleared = pair.Value.localClearedThisWeek,
						SpilloverCleared = pair.Value.spilloverClearedThisWeek,
						PhysicalBackorders = pair.Value.unitsBackordered,
						MarketDisplacedDemand = pair.Value.marketDisplacedDemandThisWeek,
						InventoryMovement = pair.Value.unitsSoldThisWeek
					}).ToArray()
				};
				return entry;
			}).OrderBy(entry => entry.RecordId, StringComparer.Ordinal).ToArray()
		};
	}

	public void AcknowledgeSettlementAudit(CompletedWeekSettlement settlement) {
		if (settlement == null || settlement != lastCompletedWeekSettlement || !settlement.IsBooked)
			throw new InvalidOperationException("Attempted to acknowledge a stale or unbooked settlement.");
		if (settlement.IsAuditAcknowledged)
			throw new InvalidOperationException($"Settlement {settlement.SettlementId} was acknowledged more than once.");
		foreach (CompletedWeekSettlementEntry entry in settlement.Entries) {
			if (entry.AuditedCount != 0) throw new InvalidOperationException($"Settlement {settlement.SettlementId} record {entry.RecordId} was audited more than once.");
			entry.AuditedCount = 1;
		}
		settlement.IsAuditAcknowledged = true;
	}


	private void ApplyCanonicalLiveIdentities(int year) {
		if (!GenreMarketV2.Enabled || canonicalLiveIdentitiesApplied) return;
		foreach (AILabel label in aiLabels ?? Enumerable.Empty<AILabel>()) {
			label.preferredGenres = CanonicalizeGenres(label.preferredGenres, year);
			label.secondaryGenres = CanonicalizeGenres(label.secondaryGenres, year);
			foreach (SimulatedArtist artist in label.roster ?? Enumerable.Empty<SimulatedArtist>()) CanonicalizeArtistGenres(artist, year);
		}
		foreach (SimulatedArtist artist in ArtistManager.Instance?.GetUnsignedArtists() ?? new List<SimulatedArtist>()) CanonicalizeArtistGenres(artist, year);
		foreach (RecordRuntimeData runtime in allRecords) {
			GenreMigration.Canonicalize(runtime.baseRecord);
			CanonicalizeAlbumTrackGenres(runtime.baseRecord.album, year);
		}
		canonicalLiveIdentitiesApplied = true;
	}

	private static Genre[] CanonicalizeGenres(IEnumerable<Genre> genres, int year) => (genres ?? Enumerable.Empty<Genre>())
		.Select(genre => GenreCatalog.MapLegacy(genre, year)).Distinct().ToArray();
	private static void CanonicalizeArtistGenres(SimulatedArtist artist, int year) {
		if (artist == null) return;
		artist.primaryGenre = GenreCatalog.MapLegacy(artist.primaryGenre, year);
		artist.secondaryGenre = GenreCatalog.MapLegacy(artist.secondaryGenre, year);
	}
	private static void CanonicalizeAlbumTrackGenres(Album album, int year) {
		if (album == null) return;
		foreach (AlbumTrack track in album.GetAllTracks()) if (track != null) track.genre = GenreCatalog.MapLegacy(track.genre, track.releaseDate.year > 0 ? track.releaseDate.year : year);
	}

	private void OnYearChanged(GameDate date) {
		UpdateBaseZeitgeist(date.year);

		foreach (var region in allRegions) {
			region.InitializeRuntimeState(date.year);
		}

		// Age the station panel into the new year (Boss conversion, FM emergence) without rebuilding
		// rosters, so cultivated relationships persist. Inert in Phase 1.
		stationNetwork?.OnYearChanged(allRegions, date.year);
	}

	private void UpdateBaseZeitgeist(int year) {
		baseZeitgeist = Zeitgeist.GetForYear(year);
		if (debugMode) GD.Print($"ChartManager: Base zeitgeist updated for {year}");
	}

	// ========================================================================
	// NEW RELEASES
	// ========================================================================

	public void ReleaseRecord(Record record, AILabel releasingLabel = null) {
		// Prewarm stays on its historical path. New live enabled releases acquire
		// durable canonical identities before any market calculation sees them.
		if (GenreMarketV2.Enabled && currentChartWeek > 0) GenreMigration.Canonicalize(record);
		var runtimeData = new RecordRuntimeData(record);
		if (ArtistPopulationLifecycle.Enabled) {
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(record.artistId);
			runtimeData.artistContractSequenceAtRelease = artist?.contractSequence ?? -1;
		}
		float perceivedQualityMult = 1f;
		if (releasingLabel != null && !record.isPlayerOwned) {
			float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
			float noiseRange = Mathf.Lerp(0.30f, 0.10f, releasingLabel.scoutingAbility);
			float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
			perceivedQualityMult = 0.6f + (perceivedQuality * 0.8f);
		}

		float quality = runtimeData.GetQuality();
		float labelPush = releasingLabel != null ? ChartSimulator.GetCampaignImpact(releasingLabel) : 0.2f;

		if (record.format == ReleaseFormat.Album) {
			runtimeData.awareness = 0.06f + quality * 0.06f + labelPush * 0.08f;
			runtimeData.radioHeat = 0f;
		} else {
			runtimeData.awareness = 0.12f + (quality * 0.08f) + (labelPush * 0.15f);
			runtimeData.radioHeat = 0.08f + (labelPush * 0.12f);
		}

		foreach (var region in allRegions) {
			var regionalData = new RegionalRecordData(region.regionId);
			runtimeData.regionalData[region.regionId] = regionalData;
		}

		allRecords.Add(runtimeData);
		recordById[runtimeData.baseRecord.recordId] = runtimeData;

		if (releasingLabel != null && !record.isPlayerOwned) {
			PromoteRecordAI(runtimeData, releasingLabel, perceivedQualityMult);
		}

		if (debugMode) GD.Print($"Released: {record.title} by {record.artistName} (awareness: {runtimeData.awareness:F2}, radio: {runtimeData.radioHeat:F2})");
	}

	private void PromoteRecordAI(RecordRuntimeData record, AILabel label, float perceivedQualityMult) {
		bool isAlbum = record.baseRecord.format == ReleaseFormat.Album;
		bool genreMarketLive = GenreMarketV2.Enabled && currentChartWeek > 0;
		float acceptanceYear = genreMarketLive ? GetContinuousSimulationYear() : 0f;
		float legacyMomentum = genreMarketLive ? GetGenreMomentum(record.baseRecord.primaryGenre) : 0f;
		float campaignImpact = ChartSimulator.GetCampaignImpact(label);
		float launchReach = label.EffectiveNationalReachForRecord(record.baseRecord?.recordId);
		float broadLaunch = isAlbum
			? 0.035f + campaignImpact * (0.06f + launchReach * 0.06f)
			: 0.06f + (campaignImpact * (0.10f + launchReach * 0.10f));
		record.awareness = Mathf.Max(broadLaunch, record.awareness);
		record.awareness = Mathf.Clamp(record.awareness, 0f, 1f);

		record.radioHeat = isAlbum ? 0f : Mathf.Max(0.1f, record.radioHeat);
		record.radioHeat += isAlbum ? 0f : campaignImpact * 0.12f;
		record.radioHeat = Mathf.Clamp(record.radioHeat, 0f, 1f);

		// The seed is the record's first week of rotation, not its eventual rotation, so it carries
		// the same build weight the weekly regional pass applies from here on.
		float launchRadioBuild = ChartSimulator.GetRadioBuildWeight(record.weeksSinceRelease);

		var initialStock = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var region in allRegions) {
			if (!record.regionalData.ContainsKey(region.regionId)) continue;

			var data = record.regionalData[region.regionId];
			float regionStrength = ChartSimulator.GetRegionalLaunchFactor(label, region.regionId, record.baseRecord?.recordId);
			int units = ChartSimulator.CalculateInitialRegionalStock(label, region.regionId, isAlbum ? 0.45f : 1f, perceivedQualityMult, record.baseRecord?.recordId);
			initialStock[region.regionId] = units;
			data.unitsInStores = units;

			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			float genreRadio = genreMarketLive
				? GenreAcceptanceService.GetRegionalRadioOpportunity(record.baseRecord.primaryGenre, record.baseRecord.secondaryGenre, region, acceptanceYear, legacyMomentum)
				: 1f;
			if (MarketSeasonality.Enabled && currentChartWeek > 0) {
				float radioOpportunity = MarketSeasonality.GetRadioOpportunity(TimeManager.Instance?.CurrentDate.year ?? 1960,
					TimeManager.Instance?.CurrentDate.month ?? 1, liveTick: true);
				data.radioPlay = isAlbum ? 0f : (0.15f + (float)GD.RandRange(0.1, 0.25)) * campaignImpact * regionStrength / radioDifficulty * radioOpportunity * genreRadio * launchRadioBuild;
			} else data.radioPlay = isAlbum ? 0f : (0.15f + (float)GD.RandRange(0.1, 0.25)) * campaignImpact * regionStrength / radioDifficulty * genreRadio * launchRadioBuild;
			data.awareness = (0.15f + (float)GD.RandRange(0.05, 0.15)) * campaignImpact * regionStrength;

			float quality = (record.baseRecord.hookStrength + record.baseRecord.productionQuality) / 2f;
			float genreFit = GetGenreFit(record.baseRecord.primaryGenre, region);
			data.sentiment = (quality * 0.7f + genreFit * 0.3f) + (float)GD.RandRange(-0.05, 0.1);
		}
		IReadOnlyDictionary<string, int> allocatedStock = ChartSimulator.RedistributeInitialRegionalStockAllocation(
			record.baseRecord.primaryGenre, TimeManager.Instance?.CurrentDate.year ?? 1960, genreMarketLive, allRegions, initialStock);
		foreach (var region in allRegions) {
			if (record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data))
				data.unitsInStores = allocatedStock.GetValueOrDefault(region.regionId);
		}

		record.initialLaunchAwareness = record.awareness;
		record.initialLaunchStock = record.regionalData.Values.Sum(data => data.unitsInStores);
		SimulatedArtist promotedArtist = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId);
		record.launchCareerState = promotedArtist?.careerState ?? CareerState.Unsigned;
		record.perceivedQualityMultiplier = perceivedQualityMult;
		ArtistRecognitionService.RecordLaunchAudit(promotedArtist, record);

		if (debugMode) {
			int totalStock = record.regionalData.Values.Sum(d => d.unitsInStores);
			GD.Print($"Promoted {record.baseRecord.title}: {totalStock:N0} total units stocked, awareness={record.awareness:F2}");
		}
	}

	// ========================================================================
	// CHART SIMULATION (Core Loop)
	// ========================================================================

	private void SimulateWeek(bool triggerEvents) {
		long profileStart = SimulationPerformanceProfiler.Begin();
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int month = TimeManager.Instance?.CurrentDate.month ?? 1;
		bool genreMarketLive = GenreMarketV2.Enabled && currentChartWeek > 0;
		float acceptanceYear = GetContinuousSimulationYear();
		float singleOpportunityNormalization = GenreAcceptanceService.GetLiveSingleOpportunityNormalization(
			allRegions, acceptanceYear, genreMarketLive);
		var albumSubstitutionByGenre = new Dictionary<Genre, float>();

		// === STEP 1: Update global record state ===
		foreach (var record in allRecords) {
			record.weeksSinceRelease++;

			AILabel label = GetLabelById(record.baseRecord.labelId);
			// Radio heat is record-wide, so resolve its single national input from the
			// population-weighted regional routes. Passing 1 here removed the legacy
			// acceptance damping altogether and inflated the enabled economy.
			bool isAlbum = record.baseRecord.format == ReleaseFormat.Album;
			float genreAcceptance = 1f;
			if (!isAlbum) {
				if (genreMarketLive) {
					// The radio-only acceptance, then the per-genre radio-acceptance multiplier
					// (GenreCatalog.GetRadioAcceptance) -- the chart-efficiency dimension that lets a
					// genre be small-selling yet heavily programmed. It rides ONLY into radio heat here,
					// never sales, and is amplified by AIRPLAY_CONVEXITY downstream.
					float radioAcceptance = GenreAcceptanceService.GetNationalDemandAcceptance(record.baseRecord.primaryGenre,
						record.baseRecord.secondaryGenre, allRegions, acceptanceYear, GetGenreMomentum(record.baseRecord.primaryGenre));
					Genre canonicalPrimary = GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, (int)Mathf.Floor(acceptanceYear));
					genreAcceptance = Mathf.Clamp(radioAcceptance * GenreCatalog.GetRadioAcceptance(canonicalPrimary), 0f, 1f);
				} else genreAcceptance = GetEffectiveGenreAcceptance(record.baseRecord.primaryGenre);
			}
			float artistHeat = CalculateArtistHeat(record.baseRecord.artistId);

			if (isAlbum) {
				long albumProfileStart = SimulationPerformanceProfiler.Begin();
				if (!albumSubstitutionByGenre.TryGetValue(record.baseRecord.primaryGenre, out float substitutionPropensity)) {
					substitutionPropensity = CompetitorManager.Instance?.CalculateSubstitutionPropensity(
						record.baseRecord.primaryGenre, year) ?? 0f;
					albumSubstitutionByGenre[record.baseRecord.primaryGenre] = substitutionPropensity;
				}
				AlbumSimulator.UpdateAlbum(record, label, artistHeat, substitutionPropensity);
				SimulationPerformanceProfiler.EndAlbumUpdate(albumProfileStart);
			}
			else ChartSimulator.UpdateRecord(record, label, genreAcceptance, artistHeat);
		}

		// === STEP 2: Calculate regional sales ===
		// The frozen route intentionally remains byte-for-byte the former immediate
		// commit path.  Common clearing is enabled only after the live boundary.
		if (genreMarketLive) {
			CalculateLiveRegionalSalesWithMarketClearing(year, month, triggerEvents, acceptanceYear,
				singleOpportunityNormalization);
		} else foreach (var record in allRecords) {
			int totalSales = 0;
			float quality = record.GetQuality();
			AILabel label = GetLabelById(record.baseRecord.labelId);
			bool isAlbum = record.baseRecord.format == ReleaseFormat.Album;
			float legacyMomentum = genreMarketLive && !isAlbum ? GetGenreMomentum(record.baseRecord.primaryGenre) : 0f;
			float legacyNationalAcceptance = !genreMarketLive && !isAlbum ? GetEffectiveGenreAcceptance(record.baseRecord.primaryGenre) : 1f;

			foreach (var region in allRegions) {
				if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData regionalData)) {
					regionalData = new RegionalRecordData(region.regionId);
					record.regionalData[region.regionId] = regionalData;
				}

				// Albums do not consume this argument. Avoid resolving seven unused
				// acceptances per album, and retain the single's routed result for radio.
				float blendedAcceptance = 1f;
				if (!isAlbum) {
					blendedAcceptance = genreMarketLive
						? GenreAcceptanceService.GetRegionalDemandAcceptance(record.baseRecord.primaryGenre, record.baseRecord.secondaryGenre, region, acceptanceYear, legacyMomentum)
						: (region.GetGenreAcceptance(record.baseRecord.primaryGenre, acceptanceYear) * 0.6f) + (legacyNationalAcceptance * 0.4f);
					if (genreMarketLive) {
						regionalData.genreMarketAcceptanceWeek = currentChartWeek;
						regionalData.genreDemandAcceptanceThisWeek = blendedAcceptance;
						regionalData.genreRadioOpportunityThisWeek = GenreAcceptanceService.GetRegionalRadioOpportunity(
							record.baseRecord.primaryGenre, region, acceptanceYear, blendedAcceptance);
					}
				}

				int regionalSales = isAlbum
					? AlbumSimulator.CalculateRegionalSales(record, region, regionalData, year, month, triggerEvents, label)
					: ChartSimulator.CalculateRegionalSales(
						record,
						region,
						regionalData,
						quality,
						blendedAcceptance,
						year,
						month,
						triggerEvents,
						GetInternalPreviousPosition(record),
						label,
						genreMarketLive && !record.cohortOpportunityColdStartFallback ? record.cohortOpportunityNormalizer : singleOpportunityNormalization
					);

				regionalData.unitsInStores = Mathf.Max(0, regionalData.unitsInStores - regionalSales);
				regionalData.unitsSoldThisWeek = regionalSales;
				regionalData.unitsSoldTotal += regionalSales;

				totalSales += regionalSales;
			}

			ChartSimulator.FinalizeWeeklySales(record, totalSales);
			if (record.baseRecord.format != ReleaseFormat.Album) ChartSimulator.UpdateSaturation(record, allRegions);
		}

		// Demand evidence is evaluated before replenishment so inventory exhaustion
		// cannot masquerade as audience growth.
		foreach (var record in allRecords) {
			if (record.baseRecord.format != ReleaseFormat.Album) UpdateRegionalBreakoutState(record, year);
		}

		// === STEP 2.5: RESTOCK HOT RECORDS ===
		RestockHotRecords();

		// Payola ledger tick (radio doc d): decay/expire arrangements and adjudicate scandal before the
		// playlist meeting reads the boost cache. Inert while there are no player arrangements.
		payolaLedger?.Tick(currentChartWeek, year, month);

		// === STEP 2.75: REPORTER PLAYLIST MEETING ===
		// Reporter stations re-cut their playlists against this week's settled sales, BEFORE the radio
		// aggregation reads them (design doc a 3.1). Uses the network's own RNG, so while
		// REPORTER_PANEL_WEIGHT is 0 this is computed-but-unconsumed and simulation output is unchanged.
		stationNetwork?.UpdatePlaylists(allRecords, allRegions, currentChartWeek, year);

		// === STEP 3: Update regional awareness/radio ===
		foreach (var record in allRecords) {
			if (record.baseRecord.format == ReleaseFormat.Album) {
				foreach (var data in record.regionalData.Values) AlbumSimulator.UpdateRegionalState(record, data);
			} else {
				UpdateRecordRegionalData(record);
				ApplyBreakoutDiscovery(record);
			}
		}

		// === STEP 4: Calculate chart points ===
		// The week's survey is drawn first and cached on each record. Billboard polled about 110
		// outlets by hand rather than counting units, so the published score is a sample. Drawing here
		// -- once per record, before anything reads a point total -- is what keeps the ranking, the
		// mid-chart exit log and the audit telemetry all looking at the same numbers.
		var chartPoints = new Dictionary<RecordRuntimeData, float>();
		foreach (var record in allRecords.Where(record => record.baseRecord.format != ReleaseFormat.Album)) {
			record.surveySampleThisWeek = ChartSimulator.DrawSurveySample(record.unitsThisWeek);
		}
		foreach (var record in allRecords.Where(record => record.baseRecord.format != ReleaseFormat.Album)) {
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
		OnChartCalculated?.Invoke(new List<RecordRuntimeData>(currentChart));

		int albumChartSize = GetAlbumChartSize(TimeManager.Instance?.CurrentDate ?? new GameDate(year, 1, 1));
		var albumRanking = allRecords
			.Where(record => record.baseRecord.format == ReleaseFormat.Album && record.unitsThisWeek > 0)
			.OrderByDescending(AlbumSimulator.CalculateChartPoints)
			.ThenByDescending(record => record.unitsThisWeek)
			.Take(albumChartSize + BubblingUnderSize)
			.ToList();
		AssignChartPositions(albumRanking, triggerEvents, ReleaseFormat.Album, albumChartSize, albumBubblingUnderPositions);
		currentAlbumChart = albumRanking.Take(albumChartSize).ToList();
		OnAlbumChartCalculated?.Invoke(new List<RecordRuntimeData>(currentAlbumChart));
		OfferAlbumChartToLandmarkRule(year);
		UpdateRecordRelevanceClocks();
		previousChartPoints = chartPoints;
		SimulationPerformanceProfiler.EndSimulateWeek(profileStart);
	}

	private void CalculateLiveRegionalSalesWithMarketClearing(int year, int month, bool triggerEvents,
		float acceptanceYear, float singleOpportunityNormalization) {
		lastMarketClearingSummaries.Clear();
		lastMarketSpilloverTransfers.Clear();
		var intentsByRegion = allRegions.ToDictionary(region => region.regionId, _ => new List<MarketIntent>(), StringComparer.Ordinal);
		// Demand evaluation deliberately retains record-major / region-minor order so
		// the existing sales jitter consumes precisely the same RNG sequence.
		foreach (RecordRuntimeData record in allRecords) {
			float quality = record.GetQuality();
			AILabel label = GetLabelById(record.baseRecord.labelId);
			bool isAlbum = record.baseRecord.format == ReleaseFormat.Album;
			float legacyMomentum = !isAlbum ? GetGenreMomentum(record.baseRecord.primaryGenre) : 0f;
			foreach (MarketRegion region in allRegions) {
				if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data)) {
					data = new RegionalRecordData(region.regionId);
					record.regionalData[region.regionId] = data;
				}
				float blendedAcceptance = 1f;
				if (!isAlbum) {
					blendedAcceptance = GenreAcceptanceService.GetRegionalDemandAcceptance(record.baseRecord.primaryGenre,
						record.baseRecord.secondaryGenre, region, acceptanceYear, legacyMomentum);
					data.genreMarketAcceptanceWeek = currentChartWeek;
					data.genreDemandAcceptanceThisWeek = blendedAcceptance;
					data.genreRadioOpportunityThisWeek = GenreAcceptanceService.GetRegionalRadioOpportunity(
						record.baseRecord.primaryGenre, region, acceptanceYear, blendedAcceptance);
				}
				int serviceable = isAlbum
					? AlbumSimulator.CalculateRegionalSales(record, region, data, year, month, triggerEvents, label)
					: ChartSimulator.CalculateRegionalSales(record, region, data, quality, blendedAcceptance, year, month,
					triggerEvents, GetInternalPreviousPosition(record), label,
					record.cohortOpportunityColdStartFallback ? singleOpportunityNormalization : record.cohortOpportunityNormalizer);
				int physicalLimit = Mathf.Min(data.unitsInStores, data.storeCapacityThisWeek);
				serviceable = Mathf.Clamp(serviceable, 0, Mathf.Max(0, physicalLimit));
				data.serviceableIntentThisWeek = serviceable;
				data.marketDisplacedDemandThisWeek = 0;
				intentsByRegion[region.regionId].Add(new MarketIntent { Record = record, Data = data, Serviceable = serviceable });
			}
		}

		var summariesByRegion = new Dictionary<string, MarketClearingRegionalSummary>(StringComparer.Ordinal);
		// Stage A: retain the accepted common local clearing unchanged.
		foreach (MarketRegion region in allRegions) {
			List<MarketIntent> intents = intentsByRegion[region.regionId];
			int baseCapacity = Mathf.Max(0, Mathf.RoundToInt(region.population * 1_000_000f * region.GetBuyingPopulationPercentage()
				* WeeklyRegionalPurchaseCapacityMultiplier));
			float albumChannelMaturity = AlbumModel.GetRetailFulfillmentMaturity(year);
			float albumEra = region.GetAlbumDemandEraProgress(year);
			float albumChannelShare = albumChannelMaturity *
				(AlbumChannelShareEraC0 + AlbumChannelShareEraC1 * albumEra + AlbumChannelShareEraC2 * albumEra * albumEra);
			int albumChannelCapacity = Mathf.RoundToInt(baseCapacity * albumChannelShare);
			int capacity = baseCapacity + albumChannelCapacity;
			var summary = new MarketClearingRegionalSummary {
				RegionId = region.regionId,
				BasePurchaseCapacity = baseCapacity,
				AlbumChannelCapacity = albumChannelCapacity,
				PurchaseCapacity = capacity
			};
			foreach (MarketIntent intent in intents) {
				bool album = intent.Record.baseRecord.format == ReleaseFormat.Album;
				summary.ActiveIntentCount += intent.Serviceable > 0 ? 1 : 0;
				if (album) {
					summary.RawAlbumDemand += intent.Data.rawDemandThisWeek;
					summary.ServiceableAlbumIntent += intent.Serviceable;
				}
				else { summary.RawSingleDemand += intent.Data.rawDemandThisWeek; summary.ServiceableSingleIntent += intent.Serviceable; }
				summary.PhysicalBackorders += intent.Data.unitsBackordered;
			}
			FormatClearingBudget budget = CalculateFormatClearingBudget(
				Mathf.RoundToInt(summary.ServiceableSingleIntent),
				Mathf.RoundToInt(summary.ServiceableAlbumIntent), baseCapacity,
				CalculateAlbumIntentOverlapPressure(albumChannelMaturity), albumChannelMaturity,
				albumChannelCapacity);
			summary.SingleFormatBudget = budget.Single;
			summary.AlbumFormatBudget = budget.Album;
			summary.EffectiveAlbumIntent = budget.EffectiveAlbum;
			summary.AlbumOverlapPressure = CalculateAlbumIntentOverlapPressure(albumChannelMaturity);
			AllocateProportionalLocal(intents.Where(intent =>
				intent.Record.baseRecord.format == ReleaseFormat.Single).ToList(), budget.Single);
			AllocateProportionalLocal(intents.Where(intent =>
				intent.Record.baseRecord.format == ReleaseFormat.Album).ToList(), budget.Album);
			foreach (MarketIntent intent in intents) intent.Data.localClearedThisWeek = intent.Cleared;
			summary.LocalClearedUnits = intents.Sum(intent => intent.Cleared);
			summary.UnusedAfterLocal = Mathf.Max(0, capacity - summary.LocalClearedUnits);
			summary.ExportBudget = CalculateSpilloverExportBudget(summary.UnusedAfterLocal);
			summary.ImportLimit = Mathf.FloorToInt(capacity * SpilloverMaximumImportShare);
			summariesByRegion[region.regionId] = summary;
		}

		// Stage B: a deterministic maximum flow over the one-hop region graph.
		// Donors and recipients are disjoint after local clearing, so no capacity can
		// be forwarded through an intermediate market in the same week.
		ApplyBoundedRegionalSpillover(intentsByRegion, summariesByRegion);

		foreach (MarketRegion region in allRegions) {
			List<MarketIntent> intents = intentsByRegion[region.regionId];
			MarketClearingRegionalSummary summary = summariesByRegion[region.regionId];
			foreach (MarketIntent intent in intents) {
				bool album = intent.Record.baseRecord.format == ReleaseFormat.Album;
				if (album) summary.ClearedAlbumUnits += intent.Cleared;
				else summary.ClearedSingleUnits += intent.Cleared;
				intent.Data.marketDisplacedDemandThisWeek = intent.Serviceable - intent.Cleared;
				intent.Data.spilloverClearedThisWeek = intent.SpilloverCleared;
				summary.MarketDisplacedDemand += intent.Data.marketDisplacedDemandThisWeek;
				if (intent.Cleared > intent.Serviceable || intent.Cleared > intent.Data.unitsInStores || intent.Cleared > intent.Data.storeCapacityThisWeek)
					summary.InventoryViolationCount++;
				intent.Data.unitsInStores = Mathf.Max(0, intent.Data.unitsInStores - intent.Cleared);
				intent.Data.unitsSoldThisWeek = intent.Cleared;
				intent.Data.unitsSoldTotal += intent.Cleared;
			}
			summary.ReconciliationDelta = intents.Sum(intent => intent.Cleared) - summary.ClearedTotalUnits;
			if (summary.LocalClearedUnits > summary.PurchaseCapacity || summary.ExportedCapacity > summary.ExportBudget ||
				summary.ImportedCapacity > summary.ImportLimit || summary.ImportedCapacity > summary.ServiceableTotalIntent - summary.LocalClearedUnits ||
				summary.ClearedTotalUnits > summary.ServiceableTotalIntent || summary.ClearedAlbumUnits > summary.AlbumFormatBudget ||
				summary.ReconciliationDelta != 0)
				summary.AllocationViolationCount++;
			lastMarketClearingSummaries.Add(summary);
		}
		foreach (RecordRuntimeData record in allRecords) {
			int totalSales = record.regionalData.Values.Sum(data => data.unitsSoldThisWeek);
			ChartSimulator.FinalizeWeeklySales(record, totalSales);
			if (record.baseRecord.format != ReleaseFormat.Album) ChartSimulator.UpdateSaturation(record, allRegions);
		}
	}

	private sealed class SpilloverFlowEdge {
		public int To, Reverse, Capacity, InitialCapacity;
		public string DonorRegionId, RecipientRegionId;
	}

	private static SpilloverFlowEdge AddSpilloverFlowEdge(List<SpilloverFlowEdge>[] graph, int from, int to, int capacity,
		string donorRegionId = null, string recipientRegionId = null) {
		var forward = new SpilloverFlowEdge { To = to, Reverse = graph[to].Count, Capacity = capacity,
			InitialCapacity = capacity, DonorRegionId = donorRegionId, RecipientRegionId = recipientRegionId };
		var reverse = new SpilloverFlowEdge { To = from, Reverse = graph[from].Count, Capacity = 0 };
		graph[from].Add(forward); graph[to].Add(reverse);
		return forward;
	}

	private void ApplyBoundedRegionalSpillover(Dictionary<string, List<MarketIntent>> intentsByRegion,
		Dictionary<string, MarketClearingRegionalSummary> summaries) {
		var donors = summaries.Values.Where(row => row.ExportBudget > 0).OrderBy(row => row.RegionId, StringComparer.Ordinal).ToList();
		var recipients = summaries.Values.Where(row => row.ImportLimit > 0 &&
			GetAllocatableResidual(intentsByRegion[row.RegionId], row) > 0)
			.OrderBy(row => row.RegionId, StringComparer.Ordinal).ToList();
		if (donors.Count == 0 || recipients.Count == 0) return;
		var donorIndex = donors.Select((row, index) => (row.RegionId, Node: index + 1)).ToDictionary(pair => pair.RegionId, pair => pair.Node, StringComparer.Ordinal);
		var recipientIndex = recipients.Select((row, index) => (row.RegionId, Node: index + 1 + donors.Count)).ToDictionary(pair => pair.RegionId, pair => pair.Node, StringComparer.Ordinal);
		int source = 0, sink = donors.Count + recipients.Count + 1;
		var graph = Enumerable.Range(0, sink + 1).Select(_ => new List<SpilloverFlowEdge>()).ToArray();
		foreach (MarketClearingRegionalSummary donor in donors) AddSpilloverFlowEdge(graph, source, donorIndex[donor.RegionId], donor.ExportBudget);
		foreach (MarketClearingRegionalSummary recipient in recipients) {
			int residual = GetAllocatableResidual(intentsByRegion[recipient.RegionId], recipient);
			AddSpilloverFlowEdge(graph, recipientIndex[recipient.RegionId], sink, Mathf.Min(residual, recipient.ImportLimit));
		}
		var transferEdges = new List<SpilloverFlowEdge>();
		foreach (MarketClearingRegionalSummary donor in donors) {
			foreach (string neighborId in GetNeighborRegionIds(donor.RegionId).OrderBy(id => id, StringComparer.Ordinal)) {
				if (!recipientIndex.TryGetValue(neighborId, out int recipientNode)) continue;
				transferEdges.Add(AddSpilloverFlowEdge(graph, donorIndex[donor.RegionId], recipientNode, donor.ExportBudget,
					donor.RegionId, neighborId));
			}
		}
		// Edmonds-Karp is compact, deterministic under the sorted graph construction,
		// and exact for this small fixed regional graph.
		while (true) {
			var parentNode = Enumerable.Repeat(-1, graph.Length).ToArray();
			var parentEdge = Enumerable.Repeat(-1, graph.Length).ToArray();
			var queue = new Queue<int>(); queue.Enqueue(source); parentNode[source] = source;
			while (queue.Count > 0 && parentNode[sink] < 0) {
				int node = queue.Dequeue();
				for (int edgeIndex = 0; edgeIndex < graph[node].Count; edgeIndex++) {
					SpilloverFlowEdge edge = graph[node][edgeIndex];
					if (edge.Capacity <= 0 || parentNode[edge.To] >= 0) continue;
					parentNode[edge.To] = node; parentEdge[edge.To] = edgeIndex; queue.Enqueue(edge.To);
				}
			}
			if (parentNode[sink] < 0) break;
			int pushed = int.MaxValue;
			for (int node = sink; node != source; node = parentNode[node]) pushed = Mathf.Min(pushed, graph[parentNode[node]][parentEdge[node]].Capacity);
			for (int node = sink; node != source; node = parentNode[node]) {
				SpilloverFlowEdge edge = graph[parentNode[node]][parentEdge[node]];
				edge.Capacity -= pushed; graph[node][edge.Reverse].Capacity += pushed;
			}
		}
		var imports = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (SpilloverFlowEdge edge in transferEdges) {
			int transfer = edge.InitialCapacity - edge.Capacity;
			if (transfer <= 0) continue;
			MarketClearingRegionalSummary donor = summaries[edge.DonorRegionId], recipient = summaries[edge.RecipientRegionId];
			donor.ExportedCapacity += transfer; recipient.ImportedCapacity += transfer;
			imports[edge.RecipientRegionId] = imports.TryGetValue(edge.RecipientRegionId, out int previous) ? previous + transfer : transfer;
			lastMarketSpilloverTransfers.Add(new MarketSpilloverTransfer { DonorRegionId = edge.DonorRegionId, RecipientRegionId = edge.RecipientRegionId,
				DonorUnusedLocal = donor.UnusedAfterLocal, DonorExportBudget = donor.ExportBudget,
				RecipientResidualDemand = GetAllocatableResidual(intentsByRegion[recipient.RegionId], recipient),
				RecipientImportLimit = recipient.ImportLimit,
				TransferredCapacity = transfer });
		}
		foreach (var pair in imports.OrderBy(pair => pair.Key, StringComparer.Ordinal)) {
			MarketClearingRegionalSummary summary = summaries[pair.Key];
			int remainingAlbumBudget = Mathf.Max(0, summary.AlbumFormatBudget -
				intentsByRegion[pair.Key].Where(intent => intent.Record.baseRecord.format == ReleaseFormat.Album)
					.Sum(intent => intent.Cleared));
			List<MarketIntent> residualIntents = intentsByRegion[pair.Key]
				.Where(intent => intent.Serviceable > intent.Cleared &&
					(intent.Record.baseRecord.format != ReleaseFormat.Album || remainingAlbumBudget > 0)).ToList();
			AllocateProportionalSpillover(residualIntents, pair.Value);
			summary.SpilloverClearedUnits = residualIntents.Sum(intent => intent.SpilloverCleared);
		}
		// Attribute recipient format results across its inbound edges deterministically.
		foreach (IGrouping<string, MarketSpilloverTransfer> group in lastMarketSpilloverTransfers.GroupBy(row => row.RecipientRegionId)) {
			int single = intentsByRegion[group.Key].Where(intent => intent.Record.baseRecord.format == ReleaseFormat.Single).Sum(intent => intent.SpilloverCleared);
			single = Mathf.Clamp(single, 0, group.Sum(row => row.TransferredCapacity));
			AllocateTransferFormats(group.OrderBy(row => row.DonorRegionId, StringComparer.Ordinal).ToList(), single);
		}
	}

	private static void AllocateProportionalSpillover(List<MarketIntent> intents, int capacity) {
		int total = intents.Sum(intent => intent.Serviceable - intent.Cleared);
		int allocated = Mathf.Min(Mathf.Max(0, capacity), total);
		if (allocated == 0 || total == 0) return;
		int assigned = 0;
		foreach (MarketIntent intent in intents) {
			float exact = (intent.Serviceable - intent.Cleared) * (float)allocated / total;
			int units = Mathf.FloorToInt(exact); intent.Cleared += units; intent.SpilloverCleared += units; intent.Fraction = exact - units; assigned += units;
		}
		int remaining = allocated - assigned;
		foreach (MarketIntent intent in intents.OrderByDescending(intent => intent.Fraction).ThenBy(intent => intent.Record.baseRecord.recordId, StringComparer.Ordinal).Take(remaining)) { intent.Cleared++; intent.SpilloverCleared++; }
	}

	private static void AllocateProportionalLocal(List<MarketIntent> intents, int budget) {
		int total = intents.Sum(intent => intent.Serviceable);
		int allocated = Mathf.Min(Mathf.Max(0, budget), total);
		if (allocated == 0 || total == 0) return;
		int assigned = 0;
		foreach (MarketIntent intent in intents) {
			float exact = intent.Serviceable * (float)allocated / total;
			intent.Cleared = Mathf.FloorToInt(exact);
			intent.Fraction = exact - intent.Cleared;
			assigned += intent.Cleared;
		}
		int remaining = allocated - assigned;
		foreach (MarketIntent intent in intents.OrderByDescending(intent => intent.Fraction)
			.ThenBy(intent => intent.Record.baseRecord.recordId, StringComparer.Ordinal).Take(remaining)) intent.Cleared++;
	}

	private static int GetAllocatableResidual(List<MarketIntent> intents, MarketClearingRegionalSummary summary) {
		int single = intents.Where(intent => intent.Record.baseRecord.format == ReleaseFormat.Single)
			.Sum(intent => intent.Serviceable - intent.Cleared);
		int albumCleared = intents.Where(intent => intent.Record.baseRecord.format == ReleaseFormat.Album)
			.Sum(intent => intent.Cleared);
		int album = Mathf.Min(
			Mathf.Max(0, summary.AlbumFormatBudget - albumCleared),
			intents.Where(intent => intent.Record.baseRecord.format == ReleaseFormat.Album)
				.Sum(intent => intent.Serviceable - intent.Cleared));
		return Mathf.Max(0, single + album);
	}

	internal static float CalculateAlbumIntentOverlapPressure(float retailMaturity) =>
		Mathf.Lerp(AlbumIntentOverlapPressure, 0f, Mathf.Clamp(retailMaturity, 0f, 1f));

	internal static FormatClearingBudget CalculateFormatClearingBudget(
		int singleIntent, int albumIntent, int capacity, float overlapPressure = AlbumIntentOverlapPressure,
		float albumChannelMaturity = 0f, int albumChannelCapacity = 0) {
		int single = Mathf.Max(0, singleIntent);
		int album = Mathf.Max(0, albumIntent);
		int available = Mathf.Max(0, capacity);
		if (available == 0 || (single == 0 && album == 0))
			return new FormatClearingBudget(0, 0, 0f);
		if (albumChannelMaturity >= 1f) {
			// Once the existing retail transition is mature, Singles and Albums each
			// have a bounded regional purchase channel. Album capacity expands with the
			// existing demand-era curve instead of cloning the full Singles channel.
			return new FormatClearingBudget(Mathf.Min(single, available),
				Mathf.Min(album, Mathf.Max(0, albumChannelCapacity)), album);
		}
		// Every Album title currently presents the full regional buyer pool. Treat
		// their aggregate serviceable intent as overlapping format demand in a
		// Single-led market before allocating common regional capacity. The existing
		// retail-maturity transition removes that correction once Albums have their
		// own established fulfillment channel.
		float effectiveAlbum = album * (float)available /
			Mathf.Max(1f, available + Mathf.Max(0f, overlapPressure) * album);
		float effectiveTotal = single + effectiveAlbum;
		int albumBudget = effectiveTotal <= available
			? Mathf.Min(album, Mathf.RoundToInt(effectiveAlbum))
			: Mathf.Min(album, Mathf.RoundToInt(available * effectiveAlbum / effectiveTotal));
		int singleBudget = Mathf.Min(single, available - albumBudget);
		return new FormatClearingBudget(Mathf.Max(0, singleBudget),
			Mathf.Max(0, albumBudget), effectiveAlbum);
	}

	internal static int CalculateSpilloverExportBudget(int unusedAfterLocal) =>
		Mathf.FloorToInt(Mathf.Max(0, unusedAfterLocal) * SpilloverMaximumExportShare);

	private static void AllocateTransferFormats(List<MarketSpilloverTransfer> transfers, int singleUnits) {
		int total = transfers.Sum(row => row.TransferredCapacity), assigned = 0;
		var fractions = new Dictionary<MarketSpilloverTransfer, float>();
		foreach (MarketSpilloverTransfer transfer in transfers) {
			float exact = transfer.TransferredCapacity * (float)singleUnits / Mathf.Max(1, total);
			transfer.ClearedSingleUnits = Mathf.FloorToInt(exact); transfer.ClearedAlbumUnits = transfer.TransferredCapacity - transfer.ClearedSingleUnits;
			assigned += transfer.ClearedSingleUnits; fractions[transfer] = exact - transfer.ClearedSingleUnits;
		}
		foreach (MarketSpilloverTransfer transfer in transfers.OrderByDescending(row => fractions[row]).ThenBy(row => row.DonorRegionId, StringComparer.Ordinal).Take(singleUnits - assigned)) {
			transfer.ClearedSingleUnits++; transfer.ClearedAlbumUnits--;
		}
	}

	private void UpdateRecordRelevanceClocks() {
		foreach (RecordRuntimeData record in allRecords) {
			if (record.currentPosition > 0) record.lastChartedAge = record.weeksSinceRelease;
			int salesFloor = record.baseRecord.format == ReleaseFormat.Album ? albumCatalogSalesFloor : RetirementSalesFloor;
			if (record.unitsThisWeek >= salesFloor) {
				record.lastSalesAboveRetirementFloorAge = record.weeksSinceRelease;
			}
		}
	}

	private int GetInternalPreviousPosition(RecordRuntimeData record) {
		if (record.currentPosition > 0) return record.currentPosition;
		var positions = record.baseRecord.format == ReleaseFormat.Album ? albumBubblingUnderPositions : bubblingUnderPositions;
		return positions.TryGetValue(record, out int position) ? position : 0;
	}

	public int GetAlbumChartSize(GameDate date) {
		if (date < new GameDate(1961, 4, 1)) return 40;
		if (date < new GameDate(1963, 8, 1)) return 50;
		if (date < new GameDate(1967, 5, 1)) return 150;
		return 200;
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
				bool isCovered = label.HasDistributionInRegionForRecord(region.regionId, record.baseRecord?.recordId);

				int stockBeforeSales = data.unitsInStores + data.unitsSoldThisWeek;
				bool specialistUnchartedService = IsSpecialistUnchartedRestockEligible(record.baseRecord.primaryGenre,
					GenreMarketV2.Enabled && IsGenreMarketV2Live, data.unitsBackordered, data.rawDemandThisWeek);
				bool albumUnchartedService = IsAlbumUnchartedRestockEligible(record.baseRecord.format,
					GenreMarketV2.Enabled && IsGenreMarketV2Live, data.unitsBackordered, data.rawDemandThisWeek,
					record.weeksSinceRelease, record.weeksOnChart > 0, GetWeeksSinceLastCharted(record),
					albumChartedToleranceWeeks * 3, albumNeverChartedToleranceWeeks);
				bool livePhysicalBackorder = GenreMarketV2.Enabled && IsGenreMarketV2Live &&
					data.unitsBackordered > 0 && data.rawDemandThisWeek > 0f;
				bool preChartDemandNeedsRestock = record.currentPosition == 0 &&
					(data.breakoutScore >= 0.20f || specialistUnchartedService || albumUnchartedService ||
					 (label.activeDeal?.grantedRegions?.Contains(region.regionId) ?? false)) &&
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
					float physicalCapacity = RegionalPhysicalCapacity(region, record, data, isCovered);
					data.breakoutMaxCapacity = Mathf.RoundToInt(physicalCapacity * (isCovered
						? 0.55f + label.distributionStrength * 0.65f
						: 0.20f));
					data.breakoutCapacityCapped = false;
				}

				if (needsRestock) {
					float demandSignal = CalculateRestockDemandSignal(data.rawDemandThisWeek,
						data.unitsSoldThisWeek, data.unitsBackordered, livePhysicalBackorder);
					float serviceLevel = isCovered
						? 0.70f + (label.distributionStrength * 0.80f)
						: 0.18f + (label.distributionStrength * 0.25f);
					// The rack jobber serviced its own racks weekly and bought a proven record
					// from whoever had it, so a hit stayed on department-store shelves in markets
					// where its label had no network of its own. This is the commercial shortcut
					// to retail of section 33.1 stage 2, and it is why a regional label could
					// have a national hit without a major's branch distribution. It lifts an
					// uncovered proven record toward -- never past -- a distributed one.
					float rackService = ChartSimulator.GetRackJobberAccess(record.currentPosition, data.peakBreakoutScore) *
						ChartSimulator.GetRackJobberEraWeight(TimeManager.Instance?.CurrentDate.year ?? 1960);
					if (!isCovered && rackService > 0f)
						serviceLevel = Mathf.Max(serviceLevel, rackService * ChartSimulator.RackServiceShareOfDistributed *
							(0.70f + (label.distributionStrength * 0.80f)));
					// DISTANCE-4B: neutral in 4a; 4b applies city-distance reach to restock service.
					serviceLevel *= DistanceModel.GetEffectiveReach(label, DistanceModel.GetHubCityIdForRegion(region.regionId));
					int restockAmount = CalculateRestockAmount(data.rawDemandThisWeek, data.unitsBackordered,
						demandSignal, serviceLevel, albumUnchartedService,
						AlbumModel.GetRetailFulfillmentMaturity(TimeManager.Instance?.CurrentDate.year ?? 1960));
					int requestedRestock = restockAmount;

					float physicalCapacity = RegionalPhysicalCapacity(region, record, data, isCovered);
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

	/// <summary>
	/// Specialist demand that has already produced a physical backorder may request
	/// ordinary uncharted replenishment without first satisfying a broad-market
	/// breakout score. It neither changes demand nor applies to disabled/prewarm
	/// execution or non-specialist records.
	/// </summary>
	internal static bool IsSpecialistUnchartedRestockEligible(Genre primaryGenre, bool live, int backorders, float rawDemand) =>
		live && GenreAcceptanceService.IsSpecialistFulfillmentGenre(primaryGenre) &&
		backorders > 0 && rawDemand > 0f;

	/// <summary>
	/// Album demand is not required to win a Singles-oriented breakout score
	/// before the physical distribution system may replenish it. This only opens
	/// the ordinary bounded regional restock path; its reach, service-level, and
	/// store-capacity limits remain authoritative.
	/// </summary>
	internal static bool IsAlbumUnchartedRestockEligible(ReleaseFormat format, bool live, int backorders, float rawDemand) =>
		live && format == ReleaseFormat.Album && backorders > 0 && rawDemand > 0f;

	/// <summary>
	/// Automatic Album backorder service launches a title and carries a recently
	/// charted title through a bounded catalog grace period. It is not a perpetual
	/// replenishment entitlement: once an uncharted title exhausts that relevance
	/// window, its remaining shelf stock must sell through before retirement.
	/// </summary>
	internal static bool IsAlbumUnchartedRestockEligible(ReleaseFormat format, bool live,
		int backorders, float rawDemand, int ageWeeks, bool hasCharted,
		int weeksSinceLastCharted, int automaticAgeWeeks, int chartGraceWeeks) =>
		IsAlbumUnchartedRestockEligible(format, live, backorders, rawDemand) &&
		(ageWeeks < Mathf.Max(0, automaticAgeWeeks) ||
		 (hasCharted && weeksSinceLastCharted < Mathf.Max(0, chartGraceWeeks)));

	/// <summary>
	/// Live replenishment with an observed physical backlog is driven by current
	/// demand plus that recent backlog. The legacy breakout blend discounts both
	/// inputs and can keep any stocked format below its regional purchase capacity
	/// after the restock path has opened. Coverage, distance reach, service level,
	/// and shelf capacity still bound the resulting request in RestockHotRecords.
	/// </summary>
	internal static float CalculateRestockDemandSignal(float rawDemand, int unitsSold, int backorders,
		bool livePhysicalBackorder) =>
		livePhysicalBackorder
			? Mathf.Max(0f, rawDemand) + Mathf.Max(0, backorders)
			: Mathf.Max(0f, rawDemand) * 0.65f + Mathf.Max(0, unitsSold) * 0.35f + Mathf.Max(0, backorders) * 0.25f;

	/// <summary>
	/// Current demand remains subject to the regional delivery service level until
	/// the established half of the Album-era curve, when retail fulfillment closes
	/// the remaining delivery gap.
	/// An Album backorder is already an observed local physical order, so applying
	/// delivery attrition to it again creates a persistent shortfall. Catch-up
	/// remains bounded by the same per-record regional shelf capacity.
	/// </summary>
	internal static int CalculateRestockAmount(float rawDemand, int backorders, float demandSignal,
		float serviceLevel, bool fulfillAlbumBacklog, float albumRetailMaturity) {
		float boundedService = Mathf.Max(0f, serviceLevel);
		float albumCurrentDemandService = Mathf.Lerp(boundedService, 1f,
			Mathf.Clamp(albumRetailMaturity, 0f, 1f));
		float requested = fulfillAlbumBacklog
			? Mathf.Max(0f, rawDemand) * albumCurrentDemandService + Mathf.Max(0, backorders)
			: Mathf.Max(0f, demandSignal) * boundedService;
		return Mathf.Max(0, Mathf.RoundToInt(requested));
	}

	// Legacy method for external calls
	public void CalculateChart() {
		SimulateWeek(triggerEvents: true);
		UpdateGenreMomentum();
	}

	private void UpdateRecordRegionalData(RecordRuntimeData record) {
		bool seasonalRadio = MarketSeasonality.Enabled && currentChartWeek > 0;
		bool genreMarketLive = GenreMarketV2.Enabled && currentChartWeek > 0;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int month = TimeManager.Instance?.CurrentDate.month ?? 1;
		float radioOpportunity = seasonalRadio ? MarketSeasonality.GetRadioOpportunity(year, month, liveTick: true) : 1f;
		float acceptanceYear = genreMarketLive ? GetContinuousSimulationYear() : 0f;
		float legacyMomentum = genreMarketLive ? GetGenreMomentum(record.baseRecord.primaryGenre) : 0f;
		// One decision per region per week, taken against this week's settled sales -- step 2 has
		// already run FinalizeWeeklySales, so the support ratio and the weeks-since-peak clock are
		// current, and a drop landing here reaches the same week's chart points in step 4.
		float stationDropChance = ChartSimulator.GetStationDropChance(
			ChartSimulator.GetSalesSupportRatio(record), record.weeksSincePeakUnits);
		float carriedPanelWeight = 0f, totalPanelWeight = 0f;
		foreach (var region in allRegions) {
			if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data)) {
				data = new RegionalRecordData(region.regionId);
				record.regionalData[region.regionId] = data;
			}

			// Awareness decay
			data.awareness *= 0.92f;

			// The playlist meeting. A record only becomes a candidate once it is actually in rotation
			// here, so the draw is not spent on the thousands of live records carrying none, and the
			// latch means this region is never asked again.
			if (stationDropChance > 0f && ChartSimulator.IsStationDropCandidate(data) &&
				(float)GD.RandRange(0.0, 1.0) < stationDropChance) {
				data.stationsDropped = true;
				data.stationDropAge = record.weeksSinceRelease;
			}
			// Reach x population is the weighting CalculateChartPoints pays airplay on, so this is the
			// share of the panel that still carries the record rather than a count of regions.
			float panelWeight = region.media != null ? region.media.radioReach * region.population : 0f;
			totalPanelWeight += panelWeight;
			if (!data.stationsDropped) carriedPanelWeight += panelWeight;

			// TAIL contribution -- the former single-value formula, now the aggregate-station term.
			// Computed exactly as before into `tail`; data.radioPlay remains the weekly accumulator
			// (it is read above and re-derived below) and the value the rest of the pipeline reads.
			float tail;
			if (data.stationsDropped) {
				// Off the playlist. Skipping the lerp entirely is the whole point of the mechanic --
				// what it replaces was an exponential, and an exponential is what a drop is not -- and
				// nothing anywhere re-adds rotation to a latched region.
				tail = ChartSimulator.GetDroppedRotation(data.radioPlay);
			} else {
				// Radio play: decay + pull toward national heat
				float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
				float genreRadio = 1f;
				if (genreMarketLive) {
					genreRadio = data.genreMarketAcceptanceWeek == currentChartWeek
						? data.genreRadioOpportunityThisWeek
						: GenreAcceptanceService.GetRegionalRadioOpportunity(record.baseRecord.primaryGenre, record.baseRecord.secondaryGenre, region, acceptanceYear, legacyMomentum);
				}
				// Stations add a record over several weeks. Without this the release ramp throttled sales
				// to 8.7% of peak in week one while rotation arrived at full campaign strength, leaving
				// airplay at 77.3% of a new record's chart points and debuts near #73 instead of #90.
				float radioBuild = ChartSimulator.GetRadioBuildWeight(record.weeksSinceRelease);
				// tailAccess is 1 in Phase 2a (folded in when the reporter/tail format split is dialled
				// in at Phase 2b), so this expression is arithmetically identical to the former target.
				float targetRegionalRadio = (seasonalRadio ? record.radioHeat / radioDifficulty * radioOpportunity * genreRadio : record.radioHeat / radioDifficulty * genreRadio) * radioBuild;
				// Stations phase a record out of rotation rather than dropping it, and this is the whole
				// plateau: sales fall to 65% of peak in a single week, so airplay only holds a record up
				// after its sales peak if it decays slower than that. The old 0.85/0.20 pair settled to
				// 0.68 a week with the target at zero -- faster than sales, so no plateau. 0.92/0.15
				// settles to 0.78. These were never load-bearing before airplay reached the ranking.
				tail = Mathf.Lerp(data.radioPlay * RegionalRadioHold, targetRegionalRadio, RegionalRadioLerp);
			}
			data.tailRadioPlay = tail;

			// REPORTER contribution + combine (radio design doc a 3.5). The reporters ARE the survey
			// panel; the tail carries reach. Phase 2a holds REPORTER_PANEL_WEIGHT at 0, so radioPlay is
			// exactly `tail` -- byte-identical to the former behaviour. Phase 2b raises the weight and
			// feeds the per-station playlists computed by StationNetwork.UpdatePlaylists().
			if (REPORTER_PANEL_WEIGHT > 0f && stationNetwork != null) {
				float reporterRadioPlay = stationNetwork.ReporterRadioPlay(record.baseRecord.recordId, region.regionId);
				data.radioPlay = Mathf.Lerp(tail, reporterRadioPlay, REPORTER_PANEL_WEIGHT);
			} else {
				data.radioPlay = tail;
			}

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
		// Derived telemetry, not an input: nothing reads this back, so the drop reaches the chart
		// only through the regional rotation it zeroes. Keeping the national radioHeat term out of it
		// is deliberate -- AIRPLAY_CONVEXITY raises rotation to the fifth power, so a multiplicative
		// cut applied to heat AND to carriage would compound to the sixth and make the mechanic's
		// severity an artifact of an exponent that section 11.6.3 already calls provisional. Airplay
		// points are linear in this number, which is the well-conditioned lever.
		record.radioPanelShare = totalPanelWeight > 0f ? carriedPanelWeight / totalPanelWeight : 1f;
	}

	/// <summary>
	/// This region's record-buying population as a share of the largest authored
	/// region's, used to restate absolute weekly unit thresholds in region-relative
	/// terms. Anchoring on the largest market leaves that market's long-standing
	/// calibration untouched and only relieves the smaller ones, so this removes a
	/// structural handicap rather than retuning the model.
	/// </summary>
	// Shelf a record can physically occupy in a market: record-store racks it can always
	// reach, plus department-store racks it has to earn. The rack share is gated on the
	// record being proven and weighted by the decade's shift toward rack and discount retail.
	private static float RegionalPhysicalCapacity(
		MarketRegion region, RecordRuntimeData record, RegionalRecordData data, bool labelShipsHere) {
		if (region?.distribution == null) return 0f;
		float rackShelf = labelShipsHere ? 1f : ChartSimulator.GetRackJobberShelfMultiplier(
			record?.currentPosition ?? 0, data?.peakBreakoutScore ?? 0f,
			TimeManager.Instance?.CurrentDate.year ?? 1960);
		return (region.distribution.recordStoreCount * 100f) +
			(region.distribution.departmentStoreCount * 200f * rackShelf);
	}

	internal float GetRegionalDemandScale(MarketRegion region) {
		if (region == null) return 1f;
		if (regionalDemandScaleById.TryGetValue(region.regionId, out float cached)) return cached;

		float reference = 0f;
		if (allRegions != null) {
			foreach (MarketRegion candidate in allRegions) {
				if (candidate == null) continue;
				reference = Mathf.Max(reference, candidate.GetRecordBuyingPopulation());
			}
		}
		float scale = CalculateRegionalDemandScale(region.GetRecordBuyingPopulation(), reference);
		regionalDemandScaleById[region.regionId] = scale;
		return scale;
	}

	/// <summary>
	/// Share of the national record-buying population covered by the given regions.
	/// A distributor's borrowed reach is worth exactly the market its owned network
	/// actually reaches, so this is the correct basis for deal terms.
	/// </summary>
	public float GetNationalMarketShareForRegions(IEnumerable<string> regionIds) {
		if (regionIds == null || allRegions == null) return 0f;
		var covered = new HashSet<string>(regionIds, StringComparer.Ordinal);
		float total = 0f;
		float reached = 0f;
		foreach (MarketRegion region in allRegions) {
			if (region == null) continue;
			float buyingPopulation = region.GetRecordBuyingPopulation();
			total += buyingPopulation;
			if (covered.Contains(region.regionId)) reached += buyingPopulation;
		}
		return total > 0f ? Mathf.Clamp(reached / total, 0f, 1f) : 0f;
	}

	/// <summary>
	/// A degenerate or unauthored region must not silently divide by zero; falling
	/// back to the unscaled thresholds preserves the previous behavior exactly.
	/// </summary>
	internal static float CalculateRegionalDemandScale(float regionBuyingPopulation, float referenceBuyingPopulation) =>
		referenceBuyingPopulation > 0f && regionBuyingPopulation > 0f
			? regionBuyingPopulation / referenceBuyingPopulation
			: 1f;

	/// <summary>
	/// Weekly volume evidence for one region, expressed against that region's own
	/// buying population. Two records selling the same share of their local market
	/// must produce the same evidence regardless of how large that market is.
	/// </summary>
	// Volume already carries the largest single weight below. The former
	// 0.55 + 0.45 * volume envelope multiplied the whole score by volume a second time,
	// so a record at volume 0.61 kept 82.5% of its evidence while one at 0.98 kept 99%.
	// That is the double count section 10.1 of the chart-access handoff identified;
	// region scaling corrected the thresholds feeding volumeInput but left this envelope
	// intact. Narrowing it to 0.70 + 0.30 * volume leaves high-volume incumbents
	// effectively unchanged (0.990 -> 0.993) and relieves the low-volume tail
	// (0.825 -> 0.884), which is where the never-charting population sits.
	internal static float CalculateBreakoutEvidence(float volumeInput, float velocityInput,
		float sustainedInput, float audienceInput, float mediaInput, float genreFit,
		float quality, float unmetInput) {
		float evidence = volumeInput * 0.30f + velocityInput * 0.15f + sustainedInput * 0.09f +
			audienceInput * 0.12f + mediaInput * 0.10f + genreFit * 0.08f +
			quality * 0.08f + unmetInput * 0.08f;
		return evidence * (0.70f + volumeInput * 0.30f);
	}

	internal static float CalculateBreakoutVolumeInput(float rawDemandThisWeek, float unitsSoldThisWeek, float regionScale) {
		float scale = Mathf.Max(regionScale, 0.000001f);
		float rawVolume = Mathf.Clamp((rawDemandThisWeek - 150f * scale) / (3500f * scale), 0f, 1f);
		float fulfilledVolume = Mathf.Clamp(unitsSoldThisWeek / (3000f * scale), 0f, 1f);
		return rawVolume * 0.70f + fulfilledVolume * 0.30f;
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
			bool covered = label.HasDistributionInRegionForRecord(region.regionId, record.baseRecord?.recordId);
			if (covered) coveredCount++;

			// Regional breakout asks whether a record is breaking out *in this
			// region*, which is a per-capita question. The thresholds below were
			// authored as absolute weekly unit counts, so they silently encoded the
			// largest market's scale and applied it everywhere. Regional record
			// buying populations span roughly 12x, so a record performing
			// identically per capita scored an order of magnitude less evidence
			// outside the two biggest regions -- 99% of Rockies record-weeks and
			// 90% of Deep South record-weeks fell under the flat 150-unit floor and
			// produced no volume or velocity evidence at all. Because that evidence
			// gates distribution offers and breakout diffusion, entire regions were
			// structurally locked out of the national chart.
			float regionScale = GetRegionalDemandScale(region);
			float demandFloor = 150f * regionScale;
			float previousDemand = data.previousRawDemand;
			float velocity = previousDemand >= demandFloor
				? (data.rawDemandThisWeek - previousDemand) / previousDemand
				: 0f;
			data.salesVelocity = Mathf.Clamp(velocity, -1f, 2f);
			if (previousDemand >= demandFloor && velocity > 0.04f) data.sustainedGrowthWeeks++;
			else if (velocity < -0.08f) data.sustainedGrowthWeeks = 0;

			float volumeInput = CalculateBreakoutVolumeInput(data.rawDemandThisWeek, data.unitsSoldThisWeek, regionScale);
			float velocityInput = Mathf.Clamp((velocity + 0.10f) / 0.65f, 0f, 1f);
			float audienceInput = Mathf.Clamp(data.awareness, 0f, 1f);
			float mediaInput = Mathf.Clamp(data.radioPlay * 0.75f + data.jukeboxPlay * 0.25f, 0f, 1f);
			float genreFit = region.GetGenreAcceptance(record.baseRecord.primaryGenre, year);
			// Unmet demand is deliberately NOT multiplied by volumeInput. Scaling it by
			// volume counted volume a third time and cancelled the signal for exactly the
			// labels it describes: a record selling out where its label cannot restock has
			// low fulfilled volume by construction. Measured over 1.27M record-region-weeks,
			// regions a label does not cover carry backorders in 31.05% of weeks against
			// 1.88% for covered ones, so the old form muted proven demand precisely where
			// distribution was the binding constraint -- which is the signal a distributor
			// historically acted on.
			float unmetInput = Mathf.Clamp(data.unitsBackordered / Mathf.Max(750f * regionScale, data.rawDemandThisWeek), 0f, 1f);
			float sustainedInput = Mathf.Clamp(data.sustainedGrowthWeeks / 3f, 0f, 1f);

			float evidence = CalculateBreakoutEvidence(volumeInput, velocityInput, sustainedInput,
				audienceInput, mediaInput, genreFit, quality, unmetInput);
			float response = evidence >= data.breakoutScore ? 0.48f : 0.28f;
			data.breakoutScore = Mathf.Lerp(data.breakoutScore, evidence, response);
			data.peakBreakoutScore = Mathf.Max(data.peakBreakoutScore, data.breakoutScore);

			if (data.breakoutScore >= LocalTractionActivationScore) {
				data.tractionWeeks++;
			} else {
				data.tractionWeeks = Mathf.Max(0, data.tractionWeeks - 1);
			}
			if (evidence < 0.18f || (velocity < -0.35f && data.rawDemandThisWeek < 1500f * regionScale)) data.collapseWeeks++;
			else data.collapseWeeks = 0;

			if (data.breakoutScore >= 0.40f && data.tractionWeeks >= 2) {
				data.breakoutStage = RegionalBreakoutStage.RegionalBreakout;
			} else if (data.breakoutScore >= LocalTractionActivationScore && data.breakoutStage < RegionalBreakoutStage.RegionalBreakout) {
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
			if (previousDemand >= demandFloor) { velocityTotal += data.salesVelocity; velocityCount++; }
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

	// Discovery reinforcement ramps from zero at LocalTractionActivationScore to full a
	// fixed 0.40 above it, so lowering the activation shifts the whole ramp down in
	// lockstep and a record newly admitted to the basin earns a correspondingly small
	// initial gain rather than a step.
	internal static float CalculateBreakoutDiscoveryStrength(float breakoutScore) =>
		Mathf.Clamp((breakoutScore - LocalTractionActivationScore) / 0.40f, 0f, 1f);

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

			float strength = CalculateBreakoutDiscoveryStrength(source.breakoutScore);
			float localAwarenessGain = source.breakoutStage >= RegionalBreakoutStage.RegionalBreakout
				? 0.006f + strength * 0.014f
				: 0.001f + strength * 0.003f;
			float localRadioGain = source.breakoutStage >= RegionalBreakoutStage.RegionalBreakout
				? 0.0025f + strength * 0.007f
				: strength * 0.001f;
			localAwarenessGain *= discoveryScale;
			localRadioGain *= discoveryScale;
			source.awareness = Mathf.Min(0.58f, source.awareness + localAwarenessGain);
			// Regional discovery still builds awareness and jukebox presence in a market whose
			// stations have cut the record -- people hear about it, and the box in the diner does not
			// take orders from a programme director -- but it cannot put it back on the playlist. The
			// drop is a one-way latch and every writer of radioPlay has to honour it or it leaks.
			if (!source.stationsDropped) {
				source.radioPlay = Mathf.Min(0.45f, source.radioPlay + localRadioGain);
				source.breakoutRadioGain += localRadioGain;
			}
			source.jukeboxPlay = Mathf.Min(0.55f, source.jukeboxPlay + strength * 0.006f * discoveryScale);
			source.breakoutAwarenessGain += localAwarenessGain;

			float womGain = strength * (source.breakoutStage >= RegionalBreakoutStage.RegionalBreakout ? 0.005f : 0.001f) * discoveryScale;
			record.wordOfMouth = Mathf.Min(0.72f, record.wordOfMouth + womGain);
			source.breakoutWordOfMouthGain += womGain;
			nationalGain += womGain * 0.30f;

			if (source.breakoutStage < RegionalBreakoutStage.RegionalBreakout || source.tractionWeeks < 2) continue;
			float propagationCapacity = 0.25f + label.EffectiveNationalReachForRecord(record.baseRecord?.recordId) * 0.45f +
				label.DistributionStrengthForRecord(record.baseRecord?.recordId) * 0.30f;
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
				if (!neighbor.stationsDropped) {
					neighbor.radioPlay = Mathf.Min(0.24f, neighbor.radioPlay + neighborRadioGain);
					neighbor.breakoutRadioGain += neighborRadioGain;
				}
				neighbor.breakoutAwarenessGain += neighborAwarenessGain;
			}
		}
		float crossoverBreadth = Mathf.Clamp((record.crossoverCandidateStrength - 0.15f) / 0.35f, 0f, 1f);
		float crossoverGain = crossoverBreadth * 0.015f * discoveryScale;
		record.awareness = Mathf.Min(0.60f,
			record.awareness + Mathf.Min(0.005f * discoveryScale, nationalGain) + crossoverGain);
	}

	public static string[] GetNeighborRegionIds(string regionId) => regionId switch {
		"eastcoast" => new[] { "greatlakes", "deepsouth" },
		"greatlakes" => new[] { "eastcoast", "deepsouth", "greatplains" },
		"greatplains" => new[] { "greatlakes", "rockies", "southwest" },
		"deepsouth" => new[] { "eastcoast", "greatlakes", "southwest" },
		"southwest" => new[] { "deepsouth", "rockies", "westcoast", "greatplains" },
		"rockies" => new[] { "greatplains", "southwest", "westcoast" },
		"westcoast" => new[] { "rockies", "southwest" },
		_ => Array.Empty<string>()
	};

	private void AssignChartPositions(List<RecordRuntimeData> sortedRecords, bool triggerEvents) {
		AssignChartPositions(sortedRecords, triggerEvents, ReleaseFormat.Single, chartSize, bubblingUnderPositions);
	}

	private void AssignChartPositions(
		List<RecordRuntimeData> sortedRecords,
		bool triggerEvents,
		ReleaseFormat format,
		int publishedChartSize,
		Dictionary<RecordRuntimeData, int> bubblingPositions) {
		var wasOnChart = new HashSet<RecordRuntimeData>(
			allRecords.Where(r => r.baseRecord.format == format && r.currentPosition > 0)
		);
		var previousBubbling = new HashSet<RecordRuntimeData>(bubblingPositions.Keys);
		bubblingPositions.Clear();

		for (int i = 0; i < sortedRecords.Count; i++) {
			var record = sortedRecords[i];
			int newPosition = i + 1;
			bool isPublished = newPosition <= publishedChartSize;

			if (!isPublished) {
				bubblingPositions[record] = newPosition;
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
				: (previousBubbling.Contains(record) ? publishedChartSize + 1 : 0);
			record.lastWeekPosition = internalPreviousPosition <= publishedChartSize ? internalPreviousPosition : 0;

			if (record.currentPosition == 0) {
				if (record.weeksOnChart == 0) record.weeksOnChart = 1;
				else record.weeksOnChart++;
				if (triggerEvents) OnRecordEnteredChart?.Invoke(record);
			} else {
				record.weeksOnChart++;
			}

			// Update peak - ONLY if actually on chart
			if (newPosition > 0 && newPosition <= publishedChartSize) {
				if (record.peakPosition == 0 || newPosition < record.peakPosition) {
					record.peakPosition = newPosition;
				}
			}

			// Hit #1 for first time
			if (newPosition == 1 && record.lastWeekPosition != 1) {
				if (triggerEvents) {
					OnRecordHitNumberOne?.Invoke(record);
					// Settle break-claims (radio doc c): reward stations that broke this record early,
					// from the regions, before it was validated. Candidacy/reputation only -- no chart effect.
					stationNetwork?.CreditStationsOnChartEntry(record, currentChartWeek, isNumberOne: true);
				}
			}
			// First time reaching the top 10 (the other settlement milestone -- not chart entry, too noisy).
			if (triggerEvents && newPosition <= 10 && (record.lastWeekPosition == 0 || record.lastWeekPosition > 10)) {
				stationNetwork?.CreditStationsOnChartEntry(record, currentChartWeek, isNumberOne: false);
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
		foreach (Genre g in GenreDomains.Current) {
			genreMomentum[g] *= momentumDecayRate;
		}

		foreach (var record in currentChart) {
			Genre genre = record.baseRecord.primaryGenre;

			float positionScore = (chartSize - record.currentPosition + 1) * chartPositionWeight;
			float salesScore = record.unitsThisWeek * salesWeight;

			if (record.isBullet) {
				positionScore *= 1.5f;
			}

			// Momentum is scoped to the active genre domain: it is seeded, decayed and
			// clamped over GenreDomains.Current, so a genre outside that domain has no
			// slot to accumulate into and would never be decayed if one were created.
			// ArtistManager.GetRelatedGenre is not domain-aware and hands back canonical
			// genres — BluesRock draws BritishBlues — which threw KeyNotFoundException on
			// the disabled route as soon as such a pairing charted. Every read path here
			// already guards the same way.
			AddGenreMomentum(genre, positionScore + salesScore);

			if (record.baseRecord.secondaryGenre != record.baseRecord.primaryGenre) {
				AddGenreMomentum(record.baseRecord.secondaryGenre, (positionScore + salesScore) * 0.3f);
			}
		}

		foreach (Genre g in GenreDomains.Current) {
			genreMomentum[g] = Mathf.Clamp(genreMomentum[g], -0.5f, 1f);
		}

		if (debugMode) {
			var topGenres = genreMomentum
				.OrderByDescending(kvp => kvp.Value)
				.Take(5);
			GD.Print($"Top genre momentum: {string.Join(", ", topGenres.Select(kvp => $"{kvp.Key}:{kvp.Value:F2}"))}");
		}
	}

	private void AddGenreMomentum(Genre genre, float contribution) {
		if (genreMomentum.ContainsKey(genre)) genreMomentum[genre] += contribution;
	}

	public float GetEffectiveGenreAcceptance(Genre genre) {
		if (GenreMarketV2.Enabled && currentChartWeek > 0) {
			int releaseYear = TimeManager.Instance?.CurrentDate.year ?? 1960;
			Genre canonical = GenreCatalog.MapLegacy(genre, releaseYear);
			float baseline = GenreCatalog.Get(canonical).GetBaseline(GetContinuousSimulationYear());
			float legacyMomentum = genreMomentum.ContainsKey(genre) ? genreMomentum[genre] : 0f;
			return Mathf.Clamp(baseline + (legacyMomentum * momentumInfluence), 0.05f, 1f);
		}
		float baseAcceptance = 0.5f;
		if (baseZeitgeist != null && baseZeitgeist.genreAcceptance.ContainsKey(genre)) {
			baseAcceptance = baseZeitgeist.genreAcceptance[genre];
		}

		float momentum = genreMomentum.ContainsKey(genre) ? genreMomentum[genre] : 0f;
		float adjusted = baseAcceptance + (momentum * momentumInfluence);

		return Mathf.Clamp(adjusted, 0.05f, 1f);
	}

	private static float GetContinuousSimulationYear() {
		GameDate date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int daysInYear = DateTime.IsLeapYear(date.year) ? 366 : 365;
		int dayOfYear = new DateTime(date.year, date.month, date.day).DayOfYear;
		return date.year + (dayOfYear - 1f) / daysInYear;
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
		var recordsToRetire = allRecords.Where(record => IsRecordRetirable(record, includeChartedRecords)).ToList();

		foreach (var record in recordsToRetire) RetireRecord(record);
		// RetireRecord no longer removes from allRecords one-by-one (List.Remove is O(N), so retiring R
		// records was O(R*N) -- the accidental quadratic that made this phase grow ~N^2.8 with album
		// count). Batch the removal into a single O(N) pass with the same net result and record order.
		if (recordsToRetire.Count > 0) {
			var retired = new HashSet<RecordRuntimeData>(recordsToRetire);
			allRecords.RemoveAll(retired.Contains);
		}

		if (debugMode && recordsToRetire.Count > 0) {
			GD.Print($"ChartManager: Retired {recordsToRetire.Count} dead records. Active: {allRecords.Count}");
		}
	}

	private bool IsRecordRetirable(RecordRuntimeData record, bool includeChartedRecords) {
			if (record.baseRecord.format == ReleaseFormat.Album) {
				if (record.currentPosition != 0 || record.unitsThisWeek >= albumCatalogSalesFloor) return false;
				if (record.weeksOnChart == 0) return record.weeksSinceRelease >= albumNeverChartedToleranceWeeks;
				return GetWeeksSinceLastCharted(record) >= albumChartedToleranceWeeks &&
					GetWeeksSinceSalesAboveRetirementFloor(record) >= albumChartedToleranceWeeks;
			}
			if (record.currentPosition != 0 || record.unitsThisWeek >= RetirementSalesFloor) return false;

			bool neverChartedExpired = record.weeksOnChart == 0 &&
				// Confirmed dead stock leaves after five consecutive under-floor weeks.
				// The older-catalog backstop prevents a title that repeatedly resets the
				// clock from receiving an effectively open-ended shelf life.
				(GetWeeksSinceSalesAboveRetirementFloor(record) >= NeverChartedHorizonWeeks ||
				 record.weeksSinceRelease > NeverChartedMaximumAgeWeeks);
			bool chartedExpired = includeChartedRecords &&
				record.weeksOnChart > 0 &&
				record.totalUnitsSold > 0 &&
				GetTotalRadioPlay(record) < 0.1f;
			bool chartedRelevanceExpired = record.weeksOnChart > 0 &&
				record.totalUnitsSold > 0 &&
				(GetWeeksSinceLastCharted(record) >= ChartedRelevanceHorizonWeeks ||
				 GetWeeksSinceSalesAboveRetirementFloor(record) >= ChartedRelevanceHorizonWeeks);

			return neverChartedExpired || chartedExpired || chartedRelevanceExpired;
	}

	private void RetireRecord(RecordRuntimeData record) {
		if (record?.baseRecord == null) return;
		if (lastCompletedWeekSettlement?.Entries != null) {
			CompletedWeekSettlementEntry entry = lastCompletedWeekSettlement.FindEntry(record.baseRecord.recordId);
			if (entry != null) entry.RetiredAfterSettlement = true;
		}
		if (record.baseRecord.format == ReleaseFormat.Single) {
			retiredTrackArchive[record.baseRecord.recordId] = CreateTrackSnapshot(record);
		}
		OnRecordRetired?.Invoke(record);
		// Prune the retired record from the reporter stations' per-record dictionaries (radio doc c).
		stationNetwork?.OnRecordRetired(record.baseRecord.recordId);

		var artist = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId);
		if (artist != null) {
			RosterManager.Instance?.RecordChartRunComplete(artist, record);
		}

		CompetitorManager.Instance?.RecordRetired(record);
		// allRecords removal is batched by the caller (CullDeadRecords) to avoid O(R*N) list scans.
		recordById.Remove(record.baseRecord.recordId);
	}

	private void RebuildRecordIndex() {
		recordById.Clear();
		foreach (RecordRuntimeData record in allRecords) recordById[record.baseRecord.recordId] = record;
	}

	private static AlbumTrack CreateTrackSnapshot(RecordRuntimeData record) => new() {
		sourceRecordId = record.baseRecord.recordId,
		title = record.baseRecord.title,
		genre = record.baseRecord.primaryGenre,
		quality = record.GetQuality(),
		hookStrength = record.baseRecord.hookStrength,
		productionQuality = record.baseRecord.productionQuality,
		danceability = record.baseRecord.danceability,
		isReleasedSingle = true,
		releaseDate = record.baseRecord.releaseDate,
		peakPosition = record.peakPosition
	};

	public bool TryResolveTrackSnapshot(string recordId, out AlbumTrack track, out bool resolvedFromRetiredArchive) {
		RetiredTrackResolutionAttempts++;
		RecordRuntimeData live = GetRecordRuntimeData(recordId);
		if (live != null && live.baseRecord.format == ReleaseFormat.Single) {
			track = CreateTrackSnapshot(live);
			resolvedFromRetiredArchive = false;
			return true;
		}
		if (retiredTrackArchive.TryGetValue(recordId, out AlbumTrack archived)) {
			RetiredTrackArchiveHits++;
			track = archived;
			resolvedFromRetiredArchive = true;
			return true;
		}
		RetiredTrackResolutionMisses++;
		track = null;
		resolvedFromRetiredArchive = false;
		return false;
	}

	/// <summary>
	/// Resolves a reusable-single snapshot without changing audit counters. Analytic
	/// release priors use this read-only seam so evaluating a strategy has no side effects.
	/// </summary>
	public bool TryGetTrackSnapshot(string recordId, out AlbumTrack track) {
		RecordRuntimeData live = GetRecordRuntimeData(recordId);
		if (live != null && live.baseRecord.format == ReleaseFormat.Single) {
			track = CreateTrackSnapshot(live);
			return true;
		}
		if (retiredTrackArchive.TryGetValue(recordId, out AlbumTrack archived)) {
			track = archived;
			return true;
		}
		track = null;
		return false;
	}

	public int GetCompUseCount(string recordId) =>
		!string.IsNullOrEmpty(recordId) && compUseCountByRecordId.TryGetValue(recordId, out int count) ? count : 0;

	public float GetCompFreshness(string recordId) =>
		Mathf.Pow(Mathf.Clamp(compStalenessFactor, 0f, 1f), GetCompUseCount(recordId));

	public void RegisterCompUse(string recordId) {
		if (string.IsNullOrEmpty(recordId)) return;
		compUseCountByRecordId[recordId] = GetCompUseCount(recordId) + 1;
	}

	private float GetTotalRadioPlay(RecordRuntimeData record) {
		float total = 0f;
		foreach (var data in record.regionalData.Values) {
			total += Mathf.Min(data.radioPlay, RetirementRegionRadioCap);
		}
		return total;
	}

	public int GetWeeksSinceLastCharted(RecordRuntimeData record) =>
		record.lastChartedAge >= 0 ? record.weeksSinceRelease - record.lastChartedAge : record.weeksSinceRelease;

	public int GetWeeksSinceSalesAboveRetirementFloor(RecordRuntimeData record) =>
		record.lastSalesAboveRetirementFloorAge >= 0
			? record.weeksSinceRelease - record.lastSalesAboveRetirementFloorAge
			: record.weeksSinceRelease;

	public float GetRetirementRadioPlay(RecordRuntimeData record) => GetTotalRadioPlay(record);

	// ========================================================================
	// PUBLIC API
	// ========================================================================

	public List<MarketRegion> GetAllRegions() => new List<MarketRegion>(allRegions);

	public List<RecordRuntimeData> GetCurrentChart() => new List<RecordRuntimeData>(currentChart);
	/// <summary>
	/// Offers this week's published album chart to the landmark rule. A record is recognised
	/// while it is climbing, not when it retires, so the offer has to happen here.
	/// <para>
	/// Bounded by the PUBLISHED chart size (tens of entries), never by the album population,
	/// and each record self-guards after it publishes -- so this is a handful of float
	/// compares a week and cannot become one of this project's accidental quadratics.
	/// </para>
	/// </summary>
	private void OfferAlbumChartToLandmarkRule(int year) {
		if (!ArtistEvolution.AlbumLegitimacyEnabled) return;
		foreach (RecordRuntimeData record in currentAlbumChart) {
			if (record.landmarkPublished || record.baseRecord == null) continue;
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId);
			if (artist == null) continue;
			AlbumLegitimacyService.OnAlbumChartWeek(artist, record, year);
		}
	}

	public List<RecordRuntimeData> GetCurrentAlbumChart() => new List<RecordRuntimeData>(currentAlbumChart);

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

	/// <summary>
	/// External hook for events that put a record on the air in one market. It honours the station
	/// drop latch: once a market's stations have cut a record, nothing puts it back into current
	/// rotation, because a re-add is the one thing that would make the returns defect worse. The
	/// awareness half still lands -- the event happened, people heard about it.
	/// </summary>
	public void AddRadioPlay(string recordId, string regionId, float amount) {
		var record = GetRecordRuntimeData(recordId);
		if (record != null && record.regionalData.ContainsKey(regionId)) {
			if (!record.regionalData[regionId].stationsDropped) record.regionalData[regionId].radioPlay += amount;
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
