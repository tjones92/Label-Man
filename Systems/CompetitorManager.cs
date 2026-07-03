using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CompetitorManager : Node {
	public static CompetitorManager Instance { get; private set; }
	private const float AnnualReleaseGrowthRate = 0.30f;
	public const float DealReinvestRate = 0.02f;
	public const float DealReinvestCost = 5000000f;
	public const float DealDependencyLow = 0.35f;
	public const float DealDependencyHigh = 0.56f;
	
	[ExportGroup("Configuration")]
	[Export] private int targetActiveRecords = 500;
	[Export] private int historicalRecordsCount = 150;
	
	[ExportGroup("Economic Settings")]
	[Export] private float baseRoyaltyRate = 0.04f;
	[Export] private Godot.Collections.Dictionary<string, float> pressingCostPerUnitByFormat = new() {
		{ nameof(ReleaseFormat.Single), 0.30f },
		{ nameof(ReleaseFormat.Album), 0.95f },
		{ nameof(ReleaseFormat.EP), 0.55f }
	};
	[Export] private Godot.Collections.Dictionary<string, float> pricePerUnitByFormat = new() {
		{ nameof(ReleaseFormat.Single), 0.89f },
		{ "Album", 3.98f }
	};
	[Export] private float bankruptcyThreshold = 200f;
	[Export] private float monthlyOverheadRate = 0.02f;
	[Export] private bool enableBankruptcy = true;
	[Export] private bool enableAlbums = true;
	[Export(PropertyHint.Range, "0,1,0.01")] private float albumPackagingCostPerUnit = 0.22f;
	[Export] private float albumPackagingFixedCost = 1500f;
	[Export(PropertyHint.Range, "1958,1965,0.1")] private float albumEraWeightStartYear = 1960f;
	[Export(PropertyHint.Range, "1965,1972,0.1")] private float albumEraWeightEndYear = 1968f;
	[Export(PropertyHint.Range, "1960,1967,0.1")] private float albumCohesionRiseStartYear = 1964f;
	[Export(PropertyHint.Range, "1965,1972,0.1")] private float albumCohesionRiseEndYear = 1968f;

	[ExportGroup("Distribution Deals")]
	[Export(PropertyHint.Range, "0,1,0.001")] private float monthlyPullOfferProbability = 0.12f;
	[Export(PropertyHint.Range, "0,1,0.001")] private float monthlyPushOfferProbability = 0.04f;
	[Export(PropertyHint.Range, "0,1,0.001")] private float annualPost1966PushRamp = 0.05f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float pushMastersOwnershipRate = 0.80f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pullMarginSkimMin = 0.15f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pullMarginSkimMax = 0.25f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pushMarginSkimMin = 0.20f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pushMarginSkimMax = 0.35f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealReinvestRate = DealReinvestRate;
	[Export] private float dealReinvestCost = DealReinvestCost;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealDependencyLow = DealDependencyLow;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealDependencyHigh = DealDependencyHigh;
	
	[ExportGroup("Historical Records")]
	[Export] private Record[] historicalRecords;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	
	private int generatedRecordCounter = 0;
	private Dictionary<string, List<string>> labelActiveRecords = new Dictionary<string, List<string>>();
	private Dictionary<string, LabelFinancialHistory> labelFinancials = new Dictionary<string, LabelFinancialHistory>();
	
	private List<AILabel> aiLabels;
	private bool distributionOfferProcessingEnabled = true;
	private readonly Dictionary<(string LabelId, ReleaseFormat Format), RevenueTelemetry> weeklyRevenueByLabelAndFormat = new();
	public int DistributorCollapseCount { get; private set; }
	public int WeeklyReleaseRollsFired { get; private set; }
	public int WeeklySuccessfulReleases { get; private set; }
	public int WeeklyFailedReleaseRolls { get; private set; }
	public int WeeklyCooldownMismatchRolls { get; private set; }
	private bool lastReleaseAttemptFailedArtistSelection;
	public event System.Action<DistributionDealTelemetry> OnDistributionDealEvent;
	
	public override void _EnterTree() {
		if (Instance != null && Instance != this) { QueueFree(); return; }
		Instance = this;
	}
	
	public override void _Ready() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded += OnWeekEnded;
			TimeManager.Instance.OnMonthChanged += OnMonthChanged;
		}
	}
	
	public override void _ExitTree() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded -= OnWeekEnded;
			TimeManager.Instance.OnMonthChanged -= OnMonthChanged;
		}
	}
	
	public void Initialize(List<AILabel> labels) {
		AlbumModel.EraWeightStartYear = albumEraWeightStartYear;
		AlbumModel.EraWeightEndYear = albumEraWeightEndYear;
		AlbumModel.CohesionRiseStartYear = albumCohesionRiseStartYear;
		AlbumModel.CohesionRiseEndYear = albumCohesionRiseEndYear;
		aiLabels = labels;
		foreach (var label in aiLabels) {
			labelActiveRecords[label.labelId] = new List<string>();
			labelFinancials[label.labelId] = new LabelFinancialHistory();
		}
		PopulateInitialRecords();
		GD.Print($"CompetitorManager: Initialized with {aiLabels.Count} labels");
	}
	
	private void PopulateInitialRecords() {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		
		if (historicalRecords != null) {
			foreach (var record in historicalRecords) {
				if (record.releaseDate <= TimeManager.Instance.CurrentDate) {
					ChartManager.Instance.ReleaseRecord(record);
				}
			}
		}
		
		int historicalCount = historicalRecords?.Count(r => r.releaseDate <= TimeManager.Instance.CurrentDate) ?? 0;
		int needed = targetActiveRecords - historicalCount;
		
		var releaseQuotas = CalculateInitialQuotas(needed);
		
		foreach (var label in aiLabels) {
			if (!releaseQuotas.TryGetValue(label.labelId, out int quota)) continue;
			for (int i = 0; i < quota; i++) {
				if (label.roster.Count == 0) continue;
				var artist = label.roster[(int)GD.RandRange(0, label.roster.Count - 1)];
				var record = GenerateRecordFromArtist(label, artist, year);
				int weeksAgo = (int)GD.RandRange(1, 20);
				record.releaseDate = TimeManager.Instance.CurrentDate.SubtractWeeks(weeksAgo);
				ChartManager.Instance.ReleaseRecord(record);
				BootstrapPrewarmRecord(record, artist, label, weeksAgo);
				TrackRelease(label.labelId, record.recordId);
				artist.totalReleases++;
				artist.weeksSinceLastRelease = weeksAgo;
				artist.releaseHistory.Add(record.recordId);
				artist.releasedSingleIds.Add(record.recordId);
			}
		}
		if (debugMode) GD.Print($"CompetitorManager: Populated {needed} initial records from rosters");
	}
	
	private Dictionary<string, int> CalculateInitialQuotas(int totalNeeded) {
		var quotas = new Dictionary<string, int>();
		float totalWeight = aiLabels.Sum(l => GetTierWeight(l.tier) * l.roster.Count);
		if (totalWeight <= 0) {
			int perLabel = totalNeeded / Mathf.Max(1, aiLabels.Count);
			foreach (var label in aiLabels) quotas[label.labelId] = perLabel;
			return quotas;
		}
		foreach (var label in aiLabels) {
			float weight = GetTierWeight(label.tier) * label.roster.Count;
			int quota = Mathf.RoundToInt((weight / totalWeight) * totalNeeded);
			quota = Mathf.Min(quota, label.roster.Count * 3);
			quotas[label.labelId] = quota;
		}
		return quotas;
	}
	
	private float GetTierWeight(LabelTier tier) => tier switch {
		LabelTier.Major => 5f, LabelTier.MidTier => 3f, LabelTier.Independent => 2f,
		LabelTier.Small => 1f, LabelTier.Boutique => 1.5f, _ => 1f
	};
	
	private void BootstrapPrewarmRecord(Record record, SimulatedArtist artist, AILabel label, int weeksOld) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		
		float quality = runtimeData.GetQuality();
		float ageFactor = Mathf.Pow(0.92f, weeksOld);
		
		float campaignImpact = ChartSimulator.GetCampaignImpact(label);
		runtimeData.awareness = Mathf.Clamp(0.15f + (artist.reputation * 0.3f) + (artist.momentum * 0.2f) + (campaignImpact * 0.2f * ageFactor), 0f, 1f);
		runtimeData.radioHeat = Mathf.Clamp((quality * 0.4f + campaignImpact * 0.3f) * ageFactor, 0f, 1f);
		
		if (quality > 0.7f && GD.Randf() < 0.4f) {
			runtimeData.weeksOnChart = (int)GD.RandRange(2, weeksOld);
			runtimeData.peakPosition = (int)GD.RandRange(10, 60);
		} else if (quality > 0.5f && GD.Randf() < 0.3f) {
			runtimeData.weeksOnChart = (int)GD.RandRange(1, weeksOld / 2);
			runtimeData.peakPosition = (int)GD.RandRange(40, 90);
		}
		
		var regions = ChartManager.Instance.GetAllRegions();
		foreach (var region in regions) {
			if (!runtimeData.regionalData.ContainsKey(region.regionId)) {
				runtimeData.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}
			var regionalData = runtimeData.regionalData[region.regionId];
			bool isStrongRegion = label.strongRegions?.Contains(region.regionId) ?? false;
			float regionMod = isStrongRegion ? 1.4f : 1f;
			
			regionalData.awareness = runtimeData.awareness * regionMod * (float)GD.RandRange(0.7, 1.1);
			regionalData.radioPlay = runtimeData.radioHeat * regionMod * (float)GD.RandRange(0.6, 1.0);
			regionalData.sentiment = 0.5f + (quality * 0.3f) + (float)GD.RandRange(-0.1, 0.15);
			regionalData.unitsInStores = (int)GD.RandRange(5000, 20000);
			regionalData.unitsSoldTotal = (int)GD.RandRange(1000, 10000);
		}
	}
	
	private void OnWeekEnded(GameDate date) {
		if (historicalRecords != null) {
			foreach (var record in historicalRecords) {
				if (record.releaseDate == date) {
					ChartManager.Instance.ReleaseRecord(record);
					GD.Print($"Historical release: {record.title} by {record.artistName}");
				}
			}
		}
		ProcessWeeklyRevenue();
		ProcessWeeklyReleases(date);
	}
	
	private void ProcessWeeklyRevenue() {
		weeklyRevenueByLabelAndFormat.Clear();
		foreach (var label in aiLabels) {
			label.weeklyGrossRevenue = 0f;
			label.weeklyCogs = 0f;
			label.weeklyDistributionSkim = 0f;
			label.weeklyArtistRoyalty = 0f;
			label.weeklyNetRevenue = 0f;
			label.weeklyDistributionIncome = 0f;
		}
		foreach (var label in aiLabels) {
			if (!label.IsActive) continue;
			float weeklyRevenue = CalculateLabelRevenue(label);
			label.cashReserves += weeklyRevenue;
			label.monthlyRevenue += weeklyRevenue;
			if (labelFinancials.TryGetValue(label.labelId, out var financials)) {
				financials.lastMonthRevenue += weeklyRevenue;
			}
		}
	}
	
	private float CalculateLabelRevenue(AILabel label) {
		if (!labelActiveRecords.TryGetValue(label.labelId, out var recordIds)) return 0f;
		float totalRevenue = 0f;
		var deadRecords = new List<string>();
		
		foreach (var recordId in recordIds) {
			var runtimeData = ChartManager.Instance.GetRecordRuntimeData(recordId);
			if (runtimeData == null) { deadRecords.Add(recordId); continue; }
			
			float weeklyUnits = runtimeData.unitsThisWeek;
			ReleaseFormat format = runtimeData.baseRecord.format;
			float pricePerUnit = GetPricePerUnit(format);
			float pressingCost = GetPressingCostPerUnit(format);
			if (format == ReleaseFormat.Album) pressingCost += albumPackagingCostPerUnit * (runtimeData.baseRecord.album?.packaging ?? 0f);
			float grossPerUnit = Mathf.Max(0f, pricePerUnit - pressingCost);
			var artist = ArtistManager.Instance?.GetArtist(runtimeData.baseRecord.artistId);
			float artistRoyalty = artist?.royaltyRate ?? 0.05f;
			float skimFraction = label.activeDeal != null
				? Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f)
				: 0.25f * (1f - label.ownedReach);
			float retailGross = weeklyUnits * pricePerUnit;
			float cogs = weeklyUnits * pressingCost;
			float skimAmount = weeklyUnits * grossPerUnit * skimFraction;
			// Keep the existing artist contract convention (royalty on retail). The
			// distribution skim is based on revenue after manufacturing cost.
			float artistPayment = retailGross * artistRoyalty;
			float recordRevenue = weeklyUnits * grossPerUnit - skimAmount - artistPayment;
			totalRevenue += recordRevenue;
			label.weeklyGrossRevenue += retailGross;
			label.weeklyCogs += cogs;
			label.weeklyDistributionSkim += skimAmount;
			label.weeklyArtistRoyalty += artistPayment;
			label.weeklyNetRevenue += recordRevenue;
			RevenueTelemetry formatRevenue = GetOrCreateRevenueTelemetry(label.labelId, format);
			formatRevenue.gross += retailGross;
			formatRevenue.cogs += cogs;
			formatRevenue.distributionSkim += skimAmount;
			formatRevenue.artistRoyalty += artistPayment;
			formatRevenue.labelNet += recordRevenue;
			RouteDistributionSkim(label, skimAmount, format);
			
			if (artist != null) {
				float recouped = Mathf.Min(Mathf.Max(0f, artist.unrecoupedAdvance), artistPayment);
				artist.unrecoupedAdvance = Mathf.Max(0f, artist.unrecoupedAdvance - recouped);
				artist.totalRoyaltyEarnings += artistPayment - recouped;
			}
			
		}
		
		foreach (var dead in deadRecords) {
			recordIds.Remove(dead);
		}
		return totalRevenue;
	}

	private float GetPricePerUnit(ReleaseFormat format) {
		string key = format.ToString();
		return pricePerUnitByFormat != null && pricePerUnitByFormat.TryGetValue(key, out float price)
			? price
			: 0.89f;
	}

	private float GetPressingCostPerUnit(ReleaseFormat format) {
		string key = format.ToString();
		return pressingCostPerUnitByFormat != null && pressingCostPerUnitByFormat.TryGetValue(key, out float cost)
			? cost
			: 0.30f;
	}

	private RevenueTelemetry GetOrCreateRevenueTelemetry(string labelId, ReleaseFormat format) {
		var key = (labelId, format);
		if (!weeklyRevenueByLabelAndFormat.TryGetValue(key, out RevenueTelemetry telemetry)) {
			telemetry = new RevenueTelemetry();
			weeklyRevenueByLabelAndFormat[key] = telemetry;
		}
		return telemetry;
	}

	public IReadOnlyDictionary<(string LabelId, ReleaseFormat Format), RevenueTelemetry> GetWeeklyRevenueByLabelAndFormat() =>
		weeklyRevenueByLabelAndFormat;

	public void SetAlbumsEnabled(bool enabled) => enableAlbums = enabled;

	private void RouteDistributionSkim(AILabel client, float skimAmount, ReleaseFormat format) {
		DistributionDeal deal = client.activeDeal;
		if (deal == null || skimAmount <= 0f) return;
		AILabel distributor = GetLabel(deal.distributorId);
		if (distributor == null || distributor == client) return;

		float recouped = Mathf.Min(Mathf.Max(0f, deal.unrecoupedAdvance), skimAmount);
		deal.unrecoupedAdvance = Mathf.Max(0f, deal.unrecoupedAdvance - recouped);
		distributor.cashReserves += skimAmount;
		distributor.monthlyRevenue += skimAmount;
		distributor.weeklyDistributionIncome += skimAmount;
		GetOrCreateRevenueTelemetry(distributor.labelId, format).distributionIncome += skimAmount;
		if (labelFinancials.TryGetValue(distributor.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthRevenue += skimAmount;
		}
	}
	
	private void ProcessWeeklyReleases(GameDate date) {
		int releasesThisWeek = 0;
		WeeklyReleaseRollsFired = 0;
		WeeklySuccessfulReleases = 0;
		WeeklyFailedReleaseRolls = 0;
		WeeklyCooldownMismatchRolls = 0;
		foreach (var label in aiLabels) {
			if (!label.IsActive) continue;
			if (label.roster.Count == 0) continue;
			
			float releaseChance = CalculateWeeklyReleaseChance(label);
			if (GD.Randf() < releaseChance) {
				WeeklyReleaseRollsFired++;
				if (TryReleaseRecord(label, date)) {
					releasesThisWeek++;
					WeeklySuccessfulReleases++;
				} else {
					WeeklyFailedReleaseRolls++;
					if (lastReleaseAttemptFailedArtistSelection) WeeklyCooldownMismatchRolls++;
				}
			}
		}
		if (debugMode && releasesThisWeek > 0) GD.Print($"Week {date}: {releasesThisWeek} new releases");
	}
	
	private float CalculateWeeklyReleaseChance(AILabel label) {
		float baseChance = label.releasesPerMonth / 4f;
		int yearOffset = Mathf.Max(0, (TimeManager.Instance?.CurrentDate.year ?? 1960) - 1960);
		float yearScale = 1f + (yearOffset * AnnualReleaseGrowthRate);
		float statusMod = label.status switch {
			LabelStatus.Bankrupt => 0f, LabelStatus.Defunct => 0f, LabelStatus.Dying => 0.3f,
			LabelStatus.Struggling => 0.5f, LabelStatus.Stable => 1f, LabelStatus.Rising => 1.2f,
			LabelStatus.Acquired => 0.8f, _ => 1f
		};
		int availableArtists = label.roster.Count(a => a.weeksSinceLastRelease >= 10);
		if (availableArtists == 0) return 0f;
		float availabilityMod = Mathf.Clamp((float)availableArtists / 3f, 0f, 1f);
		return baseChance * yearScale * statusMod * availabilityMod;
	}

	public void RecordRetired(string labelId, string recordId) {
		if (string.IsNullOrEmpty(labelId) || string.IsNullOrEmpty(recordId)) return;
		if (labelActiveRecords.TryGetValue(labelId, out var recordIds)) {
			recordIds.Remove(recordId);
		}
	}
	
	private bool TryReleaseRecord(AILabel label, GameDate date) {
		lastReleaseAttemptFailedArtistSelection = false;
		var artist = RosterManager.Instance?.GetArtistForRelease(label) ?? label.GetArtistForRelease(date.year);
		if (artist == null) {
			lastReleaseAttemptFailedArtistSelection = true;
			return false;
		}

		ReleasePlan plan = DecideRelease(label, artist, date.year);
		var record = GenerateRecordFromArtist(label, artist, date.year, plan.format);
		float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
		float noiseRange = Mathf.Lerp(0.30f, 0.10f, label.scoutingAbility);
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
		float perceivedQualityMult = 0.6f + (perceivedQuality * 0.8f);

		float productionCost = label.GetProductionCost();
		if (record.format == ReleaseFormat.Album) {
			productionCost = productionCost * 2.4f + albumPackagingFixedCost * (record.album?.packaging ?? 0f);
		}
		float marketingBudget = label.GetMarketingBudget(artist) * perceivedQualityMult;
		float totalCost = productionCost + marketingBudget;
		float minReserve = label.GetMonthlyOverhead();
		
		if (label.cashReserves - totalCost < minReserve) {
			float available = label.cashReserves - minReserve - productionCost;
			if (available < 0) return false;
			marketingBudget = available * 0.8f;
			totalCost = productionCost + marketingBudget;
		}
		
		label.cashReserves -= totalCost;
		label.monthlyExpenses += totalCost;
		artist.unrecoupedAdvance += productionCost;
		if (labelFinancials.TryGetValue(label.labelId, out var financials)) {
			financials.lastMonthExpenses += totalCost;
		}
		
		record.releaseDate = date;
		ChartManager.Instance.ReleaseRecord(record);
		ApplyReleasePromotion(record, artist, label, marketingBudget, perceivedQualityMult);
		TrackRelease(label.labelId, record.recordId);
		RosterManager.Instance?.RecordReleased(artist, record.recordId);
		artist.weeksSinceLastRelease = 0;
		artist.releaseHistory.Add(record.recordId);
		if (record.format == ReleaseFormat.Single) artist.releasedSingleIds.Add(record.recordId);
		
		if (debugMode) {
			GD.Print($"🎵 {label.labelName}: '{record.title}' by {artist.stageName} (Quality: {(record.hookStrength + record.productionQuality) / 2f:F2}, Budget: ${totalCost:N0})");
		}
		return true;
	}

	private ReleasePlan DecideRelease(AILabel label, SimulatedArtist artist, int year) {
		if (!enableAlbums) return new() { format = ReleaseFormat.Single };
		float affinity = GetAlbumReleaseAffinity(artist.primaryGenre, year);
		return new() { format = GD.Randf() < affinity ? ReleaseFormat.Album : ReleaseFormat.Single };
	}

	private static float GetAlbumReleaseAffinity(Genre genre, int year) {
		float baseline = genre switch {
			Genre.Jazz or Genre.EasyListening or Genre.Folk or Genre.TraditionalPop or Genre.BossaNova => 0.58f,
			Genre.Country or Genre.Gospel or Genre.Blues => 0.38f,
			Genre.RockAndRoll or Genre.TeenPop or Genre.RnB or Genre.DooWop or Genre.GirlGroup => 0.11f,
			_ => 0.22f
		};
		return Mathf.Clamp(baseline + Mathf.Clamp((year - 1960f) / 9f, 0f, 1f) * 0.22f, 0.05f, 0.82f);
	}

	private struct ReleasePlan {
		public ReleaseFormat format;
	}
	
	private Record GenerateRecordFromArtist(AILabel label, SimulatedArtist artist, int year, ReleaseFormat format = ReleaseFormat.Single) {
		var record = new Record(); // Godot Resource instantiation
		generatedRecordCounter++;
		record.recordId = $"gen_{generatedRecordCounter}";
		record.labelId = label.labelId;
		record.format = format;
		record.isPlayerOwned = false;
		record.artistName = artist.stageName;
		record.artistId = artist.artistId;
		record.primaryGenre = artist.primaryGenre;
		record.secondaryGenre = artist.secondaryGenre;
		
		if (NameGenerator.Instance != null) {
			record.title = NameGenerator.Instance.GenerateSongTitle(record.primaryGenre, year, record.artistName);
		} else {
			record.title = $"Song {generatedRecordCounter}";
		}
		
		float artistQuality = artist.CalculateRecordQuality();
		float studioMod = 1f;
		if (label.strongRegions != null && label.strongRegions.Length > 0) {
			var region = ChartManager.Instance?.GetRegionById(label.strongRegions[0]);
			if (region != null) studioMod = ChartSimulator.GetStudioQualityModifier(region);
		}
		
		float baseQuality = (artistQuality * 0.82f) + (studioMod * 0.18f);
		baseQuality *= studioMod;
		
		record.hookStrength = Mathf.Clamp((artist.songwritingAbility * 0.55f) + (baseQuality * 0.35f) + (float)GD.RandRange(-0.12, 0.18), 0f, 1f);
		record.productionQuality = Mathf.Clamp((label.productionQuality * 0.4f) + (artist.studioPerformance * 0.3f) + (studioMod * 0.2f) + (float)GD.RandRange(-0.05, 0.1), 0f, 1f);
		record.originality = Mathf.Clamp(artist.members.Max(m => m.creativity) * 0.7f + (float)GD.RandRange(0f, 0.3f), 0f, 1f);
		record.danceability = (float)GD.RandRange(0.3, 0.95);
		record.controversy = (float)GD.RandRange(0f, 0.2f);
		
		if (record.primaryGenre == Genre.Gospel) record.controversy = Mathf.Min(record.controversy, 0.05f);
		else if (record.primaryGenre == Genre.RockAndRoll || record.primaryGenre == Genre.GarageRock) record.danceability = Mathf.Max(record.danceability, 0.5f);

		if (format == ReleaseFormat.Album) {
			record.album = GenerateAlbum(label, artist, year);
			record.title = GenerateAlbumTitle(record, year);
			record.hookStrength = record.album.pooledAppeal;
			record.productionQuality = Mathf.Clamp(record.album.pooledAppeal * 0.75f + label.productionQuality * 0.25f, 0f, 1f);
			record.danceability = record.album.pooledAppeal;
		}
		
		return record;
	}

	private Album GenerateAlbum(AILabel label, SimulatedArtist artist, int year) {
		bool adultGenre = artist.primaryGenre is Genre.Jazz or Genre.EasyListening or Genre.Folk or
			Genre.TraditionalPop or Genre.BossaNova or Genre.Country;
		float artistTalent = artist.CalculateBaseQuality();
		float luckyRoll = GD.Randf();
		float cohesionCeiling = AlbumModel.GetMaximumAchievableCohesion(year, artistTalent, label.productionQuality, luckyRoll);
		float thematicCohesion = Mathf.Clamp((float)GD.RandRange(0.10, cohesionCeiling), 0f, cohesionCeiling);

		AlbumFormat albumFormat;
		bool statementViable = cohesionCeiling >= 0.72f && thematicCohesion >= 0.62f;
		if (statementViable && ((year >= 1965 && GD.Randf() < 0.24f) || (year < 1965 && luckyRoll > 0.985f))) {
			albumFormat = AlbumFormat.Concept;
			thematicCohesion = Mathf.Max(thematicCohesion, 0.68f);
		} else if (!adultGenre || (year <= 1963 && GD.Randf() < 0.48f)) {
			albumFormat = AlbumFormat.Compilation;
		} else {
			float typeRoll = GD.Randf();
			albumFormat = typeRoll < 0.12f ? AlbumFormat.Soundtrack : typeRoll < 0.24f ? AlbumFormat.Live : AlbumFormat.Standard;
		}

		var referencedSingles = new List<AlbumTrack>();
		if (albumFormat == AlbumFormat.Compilation) {
			foreach (string recordId in artist.releasedSingleIds.AsEnumerable().Reverse()) {
				if (referencedSingles.Count >= 4) break;
				if (ChartManager.Instance.TryResolveTrackSnapshot(recordId, out AlbumTrack track, out _)) referencedSingles.Add(track);
			}
		}

		int targetTracks = (int)GD.RandRange(9, 13);
		var nonSingleTracks = new List<AlbumTrack>();
		float originalMaterialScale = albumFormat == AlbumFormat.Compilation ? 0.68f : albumFormat == AlbumFormat.Live ? 0.80f : 0.88f;
		while (referencedSingles.Count + nonSingleTracks.Count < targetTracks) {
			float trackQuality = Mathf.Clamp(artistTalent * originalMaterialScale + label.productionQuality * 0.12f + (float)GD.RandRange(-0.16, 0.12), 0.12f, 0.95f);
			nonSingleTracks.Add(new AlbumTrack {
				title = NameGenerator.Instance?.GenerateSongTitle(artist.primaryGenre, year, artist.stageName) ?? $"Album Track {nonSingleTracks.Count + 1}",
				genre = artist.primaryGenre,
				quality = trackQuality,
				isReleasedSingle = false
			});
		}

		float avgTrackMinutes = (float)GD.RandRange(2.45, year >= 1967 ? 4.10 : 3.35);
		var album = new Album {
			albumId = $"album_{generatedRecordCounter}",
			albumFormat = albumFormat,
			trackRefs = referencedSingles.ToArray(),
			nonSingleTracks = nonSingleTracks.ToArray(),
			runtimeMinutes = targetTracks * avgTrackMinutes,
			thematicCohesion = thematicCohesion,
			packaging = Mathf.Clamp(label.productionQuality * Mathf.Lerp(0.35f, 0.85f, AlbumModel.GetAlbumEraWeight(year)) + (float)GD.RandRange(-0.10, 0.12), 0.05f, 1f),
			isStereo = year >= 1968 || GD.Randf() < Mathf.Lerp(0.12f, 0.75f, Mathf.Clamp((year - 1960f) / 8f, 0f, 1f))
		};
		album.pooledAppeal = AlbumModel.CalculatePooledAppeal(album.GetAllTracks(), album.thematicCohesion, year);
		return album;
	}

	private static string GenerateAlbumTitle(Record record, int year) {
		string generated = NameGenerator.Instance?.GenerateSongTitle(record.primaryGenre, year, record.artistName);
		return string.IsNullOrWhiteSpace(generated) ? $"{record.artistName} Album" : generated;
	}
	
	private void ApplyReleasePromotion(Record record, SimulatedArtist artist, AILabel label, float marketingBudget, float perceivedQualityMult) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		bool isAlbum = record.format == ReleaseFormat.Album;
		
		float quality = runtimeData.GetQuality();
		float artistAwareness = artist.GetNewReleaseAwarenessBonus();
		float marketingAwareness = BudgetToImpact(marketingBudget, label.tier) * ChartSimulator.GetCampaignImpact(label) * 0.35f;
		float labelAwareness = label.reputation * 0.1f;
		
		runtimeData.awareness = Mathf.Clamp((isAlbum ? 0.04f : 0.08f) + artistAwareness + marketingAwareness + labelAwareness, 0f, 1f);
		
		float baseRadio = quality * 0.3f;
		float pushRadio = ChartSimulator.GetCampaignImpact(label) * 0.3f;
		float payolaRadio = label.payolaWillingness * 0.15f;
		runtimeData.radioHeat = isAlbum ? 0f : Mathf.Clamp(baseRadio + pushRadio + payolaRadio, 0f, 1f);
		
		var regions = ChartManager.Instance.GetAllRegions();
		foreach (var region in regions) {
			if (!runtimeData.regionalData.ContainsKey(region.regionId)) {
				runtimeData.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}
			var regionalData = runtimeData.regionalData[region.regionId];
			bool isStrongRegion = label.strongRegions?.Contains(region.regionId) ?? false;
			float regionStrength = ChartSimulator.GetRegionalLaunchFactor(label, region.regionId);
			float stockScale = artist.careerState switch {
				CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
				CareerState.Rising => 1.2f, _ => 1.0f
			};
			
			regionalData.unitsInStores = ChartSimulator.CalculateInitialRegionalStock(label, region.regionId, stockScale * (isAlbum ? 0.45f : 1f), perceivedQualityMult);
			regionalData.awareness = Mathf.Clamp(runtimeData.awareness * regionStrength * (float)GD.RandRange(0.8, 1.1), 0f, 1f);
			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			regionalData.radioPlay = isAlbum ? 0f : Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GD.RandRange(0.7, 1.0), 0f, 1f);
			float genreFit = GetGenreFit(record.primaryGenre, region);
			regionalData.sentiment = Mathf.Clamp((quality * 0.6f) + (genreFit * 0.3f) + (float)GD.RandRange(-0.1, 0.15), -1f, 1f);
		}

		runtimeData.initialLaunchAwareness = runtimeData.awareness;
		runtimeData.initialLaunchStock = runtimeData.regionalData.Values.Sum(data => data.unitsInStores);
		runtimeData.launchCareerState = artist.careerState;
		runtimeData.perceivedQualityMultiplier = perceivedQualityMult;
		
		if (debugMode) GD.Print($"  Promotion: Awareness={runtimeData.awareness:F2}, Radio={runtimeData.radioHeat:F2}");
	}
	
	private float BudgetToImpact(float budget, LabelTier tier) {
		float baseline = tier switch {
			LabelTier.Major => 3000f, LabelTier.MidTier => 1500f, LabelTier.Independent => 600f,
			LabelTier.Small => 250f, LabelTier.Boutique => 400f, _ => 500f
		};
		float normalized = budget / baseline;
		return Mathf.Clamp((Mathf.Log(1 + normalized * 9) / Mathf.Log(10)) / 1.5f, 0f, 1f);
	}
	
	private float GetGenreFit(Genre genre, MarketRegion region) {
		if (region.genrePreferences == null) return 0.6f;
		var pref = region.genrePreferences.FirstOrDefault(p => p.genre == genre);
		return pref != null ? 0.5f + (pref.affinity * 0.5f) : 0.5f;
	}
	
	private void TrackRelease(string labelId, string recordId) {
		if (!labelActiveRecords.ContainsKey(labelId)) labelActiveRecords[labelId] = new List<string>();
		labelActiveRecords[labelId].Add(recordId);
	}
	
	private void OnMonthChanged(GameDate date) {
		foreach (var label in aiLabels) ProcessLabelMonth(label, date);
		ProcessDistributionDeals(date);
		if (debugMode) PrintMonthlyReport(date);
	}
	
	private void ProcessLabelMonth(AILabel label, GameDate date) {
		if (!label.IsActive) return;
		
		var financials = labelFinancials.TryGetValue(label.labelId, out var f) ? f : null;
		if (financials == null) {
			financials = new LabelFinancialHistory();
			labelFinancials[label.labelId] = financials;
		}
		
		float overhead = label.GetMonthlyOverhead();
		label.cashReserves -= overhead;
		label.monthlyExpenses += overhead;
		financials.lastMonthExpenses += overhead;
		
		float netIncome = financials.lastMonthRevenue - financials.lastMonthExpenses;
		label.lastMonthlyProfit = netIncome;
		ReinvestDistributionProfit(label, netIncome);
		UpdateLabelStatus(label, financials, netIncome);
		
		label.monthlyRevenue = 0f;
		label.monthlyExpenses = 0f;
		financials.lastMonthRevenue = 0f;
		financials.lastMonthExpenses = 0f;
		
		if (date.month == 1) financials.totalReleasesThisYear = 0;
	}

	public void SetDistributionOfferProcessingEnabled(bool enabled) => distributionOfferProcessingEnabled = enabled;

	private void ReinvestDistributionProfit(AILabel label, float netIncome) {
		if (label.activeDeal == null || netIncome <= 0f) return;
		float reinvestment = netIncome * dealReinvestRate;
		label.cashReserves -= reinvestment;
		label.ownedReach = Mathf.Min(1f, label.ownedReach + (reinvestment / Mathf.Max(1f, dealReinvestCost)));
	}

	private void ProcessDistributionDeals(GameDate date) {
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (AILabel client in aiLabels.Where(label => label.activeDeal != null).ToList()) {
			DistributionDeal deal = client.activeDeal;
			AILabel distributor = GetLabel(deal.distributorId);
			if (!client.IsActive) {
				deal.unrecoupedAdvance = 0f;
				EmitDealEvent(client, distributor, deal, DealResolution.ClientClosed, client.DistributionDependency);
				client.activeDeal = null;
				continue;
			}
			if (distributor == null || !distributor.IsActive) {
				EmitDealEvent(client, distributor, deal, DealResolution.DistributorCollapsed, client.DistributionDependency);
				client.activeDeal = null;
				DistributorCollapseCount++;
				continue;
			}
			if (currentWeek >= deal.signedWeek + deal.termWeeks) ResolveDistributionDeal(client, distributor, currentWeek);
		}

		if (!distributionOfferProcessingEnabled) return;
		foreach (AILabel client in aiLabels.Where(label => label.IsActive && label.activeDeal == null).ToList()) {
			TryGenerateDistributionOffer(client, date.year, currentWeek);
		}
	}

	private void TryGenerateDistributionOffer(AILabel client, int year, int currentWeek) {
		bool eligibleTier = client.tier == LabelTier.Small || client.tier == LabelTier.Boutique || client.tier == LabelTier.Independent;
		if (!eligibleTier) return;

		bool pullTrigger = client.nationalReach < 0.40f && HasStrongRegionalChartRecord(client);
		float pushChance = monthlyPushOfferProbability + (Mathf.Max(0, year - 1966) * annualPost1966PushRamp);
		bool pushTrigger = (client.momentumScore > 0.60f || HasRecentTop40Record(client)) && GD.Randf() < pushChance;
		bool pullOffer = pullTrigger && GD.Randf() < monthlyPullOfferProbability;
		if (!pushTrigger && !pullOffer) return;

		DealOrigin origin = pushTrigger ? DealOrigin.DistributorCourted : DealOrigin.LabelSought;
		AILabel distributor = SelectDistributor(client, origin);
		if (distributor == null) return;
		DistributionDeal offer = GenerateDealTerms(client, distributor, origin, year, currentWeek);
		if (!ShouldAcceptDeal(client, offer)) return;

		client.activeDeal = offer;
		client.cashReserves += offer.advance;
		distributor.cashReserves -= offer.advance;
		EmitDealEvent(client, distributor, offer, DealResolution.Signed, client.DistributionDependency);
	}

	private bool HasStrongRegionalChartRecord(AILabel label) {
		if (ChartManager.Instance == null || label.strongRegions == null) return false;
		var strongRegions = label.strongRegions.ToHashSet(System.StringComparer.Ordinal);
		return ChartManager.Instance.GetAllRecords().Any(record =>
			record.baseRecord.labelId == label.labelId && record.currentPosition > 0 && record.GetQuality() > 0.70f &&
			record.regionalData.Any(pair => strongRegions.Contains(pair.Key) && pair.Value.unitsSoldThisWeek > 0));
	}

	private bool HasRecentTop40Record(AILabel label) => ChartManager.Instance != null &&
		ChartManager.Instance.GetAllRecords().Any(record => record.baseRecord.labelId == label.labelId &&
			record.currentPosition > 0 && record.currentPosition <= 40 && record.weeksSinceRelease <= 52);

	private AILabel SelectDistributor(AILabel client, DealOrigin origin) {
		var weighted = new List<(AILabel Label, float Weight)>();
		foreach (AILabel distributor in aiLabels) {
			if (!IsEligibleDistributor(distributor, client, origin)) continue;
			bool genreFit = distributor.preferredGenres?.Intersect(client.preferredGenres ?? System.Array.Empty<Genre>()).Any() ?? false;
			float weight = (distributor.ownedReach * 0.50f) + (distributor.reputation * 0.30f) + (genreFit ? 0.20f : 0f);
			weight *= distributor.tier switch { LabelTier.Major => 6f, LabelTier.MidTier => 1.5f, _ => 1f };
			if (weight > 0f) weighted.Add((distributor, weight));
		}
		if (weighted.Count == 0) return null;

		float total = weighted.Sum(entry => entry.Weight);
		float roll = GD.Randf() * total;
		foreach (var entry in weighted) {
			roll -= entry.Weight;
			if (roll <= 0f) return entry.Label;
		}
		return weighted[^1].Label;
	}

	private bool IsEligibleDistributor(AILabel distributor, AILabel client, DealOrigin origin) {
		if (distributor == null || distributor == client || !distributor.IsActive) return false;
		bool validTier = distributor.tier == LabelTier.Major || distributor.tier == LabelTier.MidTier ||
			(distributor.tier == LabelTier.Independent && distributor.ownedReach >= 0.65f);
		if (!validTier || WouldCreateCircularDeal(client, distributor)) return false;
		int capacity = distributor.tier switch { LabelTier.Major => 12, LabelTier.MidTier => 6, _ => 3 };
		if (aiLabels.Count(label => label.activeDeal?.distributorId == distributor.labelId) >= capacity) return false;
		float minimumAdvance = origin == DealOrigin.DistributorCourted ? client.GetMonthlyOverhead() * 6f : 0f;
		if (distributor.cashReserves - minimumAdvance <= distributor.GetMonthlyOverhead() * 3f) return false;

		var offeredRegions = distributor.distributionRegions ?? System.Array.Empty<string>();
		if (origin == DealOrigin.LabelSought) {
			return (client.strongRegions ?? System.Array.Empty<string>()).Any(region =>
				!client.HasDistributionInRegion(region) && offeredRegions.Contains(region));
		}
		return offeredRegions.Any(region => !client.HasDistributionInRegion(region));
	}

	private static bool WouldCreateCircularDeal(AILabel client, AILabel distributor) {
		var visited = new HashSet<string>(System.StringComparer.Ordinal);
		AILabel current = distributor;
		while (current?.activeDeal != null && visited.Add(current.labelId)) {
			if (current.activeDeal.distributorId == client.labelId) return true;
			current = Instance?.GetLabel(current.activeDeal.distributorId);
		}
		return false;
	}

	private DistributionDeal GenerateDealTerms(AILabel client, AILabel distributor, DealOrigin origin, int year, int currentWeek) {
		bool push = origin == DealOrigin.DistributorCourted;
		string[] availableRegions = (distributor.distributionRegions ?? System.Array.Empty<string>())
			.Where(region => !client.HasDistributionInRegion(region)).Distinct().ToArray();
		string[] grantedRegions = push
			? availableRegions
			: availableRegions.Intersect(client.strongRegions ?? System.Array.Empty<string>()).ToArray();
		float advance = push
			? client.GetMonthlyOverhead() * (float)GD.RandRange(6f, 12f)
			: (GD.Randf() < 0.35f ? 0f : client.GetMonthlyOverhead() * (float)GD.RandRange(0.5f, 2f));
		advance = Mathf.Min(advance, Mathf.Max(0f, distributor.cashReserves - (distributor.GetMonthlyOverhead() * 3f)));
		return new DistributionDeal {
			distributorId = distributor.labelId,
			reachGranted = push ? (float)GD.RandRange(0.50f, 0.80f) : (float)GD.RandRange(0.30f, 0.50f),
			grantedRegions = grantedRegions,
			marginSkim = push ? (float)GD.RandRange(pushMarginSkimMin, pushMarginSkimMax) : (float)GD.RandRange(pullMarginSkimMin, pullMarginSkimMax),
			ownsMasters = GD.Randf() < (push ? pushMastersOwnershipRate : 0.15f),
			advance = advance,
			unrecoupedAdvance = advance,
			signedWeek = currentWeek,
			termWeeks = push ? (int)GD.RandRange(78, year >= 1967 ? 104 : 156) : (int)GD.RandRange(52, 104),
			origin = origin
		};
	}

	private static bool ShouldAcceptDeal(AILabel client, DistributionDeal offer) {
		float currentReach = client.distributionStrength;
		float projectedReach = Mathf.Clamp(client.ownedReach + offer.reachGranted, 0f, 1f);
		if (projectedReach <= currentReach + 0.05f) return false;

		bool cashPressured = client.cashReserves < client.GetMonthlyOverhead() * 6f || client.consecutiveLossMonths >= 3;
		bool momentumHungry = client.momentumScore > 0.55f || client.status == LabelStatus.Rising;
		float acceptance = 0.20f + (cashPressured ? 0.35f : 0f) + (momentumHungry ? 0.30f : 0f);
		if (offer.origin == DealOrigin.LabelSought) acceptance += 0.20f;
		if (client.tier == LabelTier.Independent && client.ownedReach >= 0.45f && !cashPressured) acceptance -= 0.35f;
		return GD.Randf() < Mathf.Clamp(acceptance, 0.05f, 0.95f);
	}

	private void ResolveDistributionDeal(AILabel client, AILabel distributor, int currentWeek) {
		DistributionDeal deal = client.activeDeal;
		float dependency = client.DistributionDependency;
		if (dependency < dealDependencyLow) {
			client.ownedReach = Mathf.Min(1f, client.ownedReach + (deal.reachGranted * 0.50f));
			EmitDealEvent(client, distributor, deal, DealResolution.Exit, dependency);
			client.activeDeal = null;
		} else if (dependency < dealDependencyHigh) {
			EmitDealEvent(client, distributor, deal, DealResolution.Renew, dependency);
			deal.signedWeek = currentWeek;
		} else if (deal.ownsMasters) {
			AbsorbLabel(client, distributor, dependency);
		} else {
			EmitDealEvent(client, distributor, deal, DealResolution.Renew, dependency);
			deal.marginSkim = Mathf.Min(0.50f, deal.marginSkim + 0.05f);
			deal.reachGranted = Mathf.Max(0.10f, deal.reachGranted * 0.85f);
			deal.signedWeek = currentWeek;
		}
	}

	private void AbsorbLabel(AILabel client, AILabel distributor, float dependency) {
		if (client == null || distributor == null || client == distributor || !client.IsActive || !distributor.IsActive) return;
		DistributionDeal deal = client.activeDeal;
		if (deal == null || deal.distributorId != distributor.labelId || WouldCreateCircularDeal(client, distributor)) return;

		distributor.marketShare += client.marketShare;
		distributor.top40Hits += client.top40Hits;
		distributor.numberOneHits += client.numberOneHits;
		foreach (SimulatedArtist artist in client.roster.ToList()) {
			artist.labelId = distributor.labelId;
			artist.isPlayerOwned = false;
			if (!distributor.roster.Contains(artist)) distributor.roster.Add(artist);
		}
		client.roster.Clear();
		distributor.maxRosterSize = Mathf.Max(distributor.maxRosterSize, distributor.CurrentRosterSize);

		if (!labelActiveRecords.TryGetValue(distributor.labelId, out List<string> distributorRecords)) {
			distributorRecords = new List<string>();
			labelActiveRecords[distributor.labelId] = distributorRecords;
		}
		if (labelActiveRecords.TryGetValue(client.labelId, out List<string> clientRecords)) {
			foreach (string recordId in clientRecords) if (!distributorRecords.Contains(recordId)) distributorRecords.Add(recordId);
			clientRecords.Clear();
		}
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords().Where(record => record.baseRecord.labelId == client.labelId)) {
			record.baseRecord.labelId = distributor.labelId;
		}

		EmitDealEvent(client, distributor, deal, DealResolution.Absorb, dependency);
		client.activeDeal = null;
		if (LabelLifecycleManager.Instance != null) LabelLifecycleManager.Instance.MarkLabelAcquired(client, distributor);
		else client.status = LabelStatus.Acquired;
	}

	private void EmitDealEvent(AILabel client, AILabel distributor, DistributionDeal deal, DealResolution resolution, float dependency) {
		OnDistributionDealEvent?.Invoke(new DistributionDealTelemetry {
			resolution = resolution,
			origin = deal.origin,
			distributorId = deal.distributorId,
			distributorName = distributor?.labelName ?? deal.distributorId,
			clientId = client?.labelId,
			clientName = client?.labelName,
			reachGranted = deal.reachGranted,
			marginSkim = deal.marginSkim,
			ownsMasters = deal.ownsMasters,
			advance = deal.advance,
			signedWeek = deal.signedWeek,
			termWeeks = deal.termWeeks,
			dependency = dependency
		});
	}
	
	private void UpdateLabelStatus(AILabel label, LabelFinancialHistory financials, float netIncome) {
		if (netIncome < 0) financials.consecutiveLossMonths++;
		else financials.consecutiveLossMonths = Mathf.Max(0, financials.consecutiveLossMonths - 1);
		label.consecutiveLossMonths = financials.consecutiveLossMonths;
		
		if (label.cashReserves < bankruptcyThreshold) {
			if (enableBankruptcy && financials.consecutiveLossMonths >= 6) {
				label.status = LabelStatus.Bankrupt;
				GD.Print($"💀 {label.labelName} has gone bankrupt!");
				return;
			}
			label.status = LabelStatus.Dying;
		} else if (label.cashReserves < label.GetMonthlyOverhead() * 3) {
			label.status = LabelStatus.Struggling;
		} else if (financials.consecutiveLossMonths >= 3) {
			label.status = LabelStatus.Dying;
		} else if (netIncome > label.GetMonthlyOverhead() * 2) {
			label.status = LabelStatus.Rising;
		} else if (netIncome > 0) {
			label.status = LabelStatus.Stable;
		}
	}
	
	public AILabel GetLabel(string labelId) => aiLabels?.FirstOrDefault(l => l.labelId == labelId);
	public IReadOnlyList<AILabel> GetAllLabels() => aiLabels ?? (IReadOnlyList<AILabel>)System.Array.Empty<AILabel>();

	public void RegisterLabel(AILabel label) {
		if (label == null || string.IsNullOrEmpty(label.labelId)) return;
		if (aiLabels != null && !aiLabels.Contains(label)) aiLabels.Add(label);
		if (!labelActiveRecords.ContainsKey(label.labelId)) labelActiveRecords[label.labelId] = new List<string>();
		if (!labelFinancials.ContainsKey(label.labelId)) labelFinancials[label.labelId] = new LabelFinancialHistory();
	}

	public void RecordExpense(AILabel label, float amount) {
		if (label == null || amount <= 0f) return;
		label.cashReserves -= amount;
		label.monthlyExpenses += amount;
		if (labelFinancials.TryGetValue(label.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthExpenses += amount;
		}
	}
	
	public List<AILabel> GetActiveLabelsByStatus(LabelStatus status) => aiLabels?.Where(l => l.status == status).ToList() ?? new List<AILabel>();
	
	public List<AILabel> GetOperatingLabels() => aiLabels?.Where(l => l.IsActive).ToList() ?? new List<AILabel>();
	
	public int GetLabelActiveRecordCount(string labelId) => labelActiveRecords.TryGetValue(labelId, out var records) ? records.Count : 0;

	public int GetRecentChartingRecordCount(string labelId, int maxAgeWeeks = 52) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		return ChartManager.Instance.GetAllRecords().Count(record =>
			record.baseRecord.labelId == labelId &&
			record.weeksSinceRelease <= maxAgeWeeks &&
			record.weeksOnChart > 0);
	}
	
	private void PrintMonthlyReport(GameDate date) {
		GD.Print($"=== INDUSTRY REPORT - {date.month}/{date.year} ===");
		var byStatus = aiLabels.GroupBy(l => l.status);
		foreach (var group in byStatus.OrderBy(g => (int)g.Key)) GD.Print($"{group.Key}: {group.Count()} labels");
		
		var topLabels = aiLabels.Where(l => l.status != LabelStatus.Bankrupt).OrderByDescending(l => l.cashReserves).Take(5);
		GD.Print("Top 5 Labels by Cash:");
		foreach (var label in topLabels) {
			int chartingCount = labelActiveRecords.TryGetValue(label.labelId, out var recs) ? recs.Count : 0;
			GD.Print($"  {label.labelName}: ${label.cashReserves:N0} | Roster: {label.roster.Count} | Charting: {chartingCount}");
		}
	}
	
	public void DebugPrintReleaseStats() {
		int totalActive = labelActiveRecords.Values.Sum(l => l.Count);
		GD.Print($"=== RELEASE STATS ===\nTotal Active Records: {totalActive}");
		var topByReleases = labelActiveRecords.OrderByDescending(kvp => kvp.Value.Count).Take(10);
		foreach (var (labelId, records) in topByReleases) {
			var label = GetLabel(labelId);
			GD.Print($"  {label?.labelName ?? labelId}: {records.Count} active");
		}
	}
	
	public void DebugForceRelease() {
		if (aiLabels == null || aiLabels.Count == 0) return;
		var label = aiLabels.Where(l => l.status != LabelStatus.Bankrupt && l.roster.Count > 0).OrderBy(l => GD.Randf()).FirstOrDefault();
		if (label != null) {
			var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			TryReleaseRecord(label, date);
		}
	}
}

public class LabelFinancialHistory {
	public float lastMonthRevenue;
	public float lastMonthExpenses;
	public int consecutiveLossMonths;
	public int totalReleasesThisYear;
}

public sealed class RevenueTelemetry {
	public float gross;
	public float cogs;
	public float distributionSkim;
	public float artistRoyalty;
	public float labelNet;
	public float distributionIncome;
	public float MarketNet => labelNet + distributionIncome;
}
