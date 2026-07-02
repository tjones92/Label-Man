using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CompetitorManager : Node {
	public static CompetitorManager Instance { get; private set; }
	private const float AnnualReleaseGrowthRate = 0.30f;
	
	[ExportGroup("Configuration")]
	[Export] private int targetActiveRecords = 500;
	[Export] private int historicalRecordsCount = 150;
	
	[ExportGroup("Economic Settings")]
	[Export] private float baseRoyaltyRate = 0.04f;
	[Export(PropertyHint.Range, "0,0.89,0.01")] private float pressingCostPerUnit = 0.30f;
	[Export] private float bankruptcyThreshold = 200f;
	[Export] private float monthlyOverheadRate = 0.02f;
	[Export] private bool enableBankruptcy = true;
	
	[ExportGroup("Historical Records")]
	[Export] private Record[] historicalRecords;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	
	private int generatedRecordCounter = 0;
	private Dictionary<string, List<string>> labelActiveRecords = new Dictionary<string, List<string>>();
	private Dictionary<string, LabelFinancialHistory> labelFinancials = new Dictionary<string, LabelFinancialHistory>();
	
	private List<AILabel> aiLabels;
	
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
		foreach (var label in aiLabels) {
			label.weeklyGrossRevenue = 0f;
			label.weeklyCogs = 0f;
			label.weeklyDistributionSkim = 0f;
			label.weeklyArtistRoyalty = 0f;
			label.weeklyNetRevenue = 0f;
			label.weeklyDistributionIncome = 0f;
		}
		foreach (var label in aiLabels) {
			if (label.status == LabelStatus.Bankrupt || label.status == LabelStatus.Defunct) continue;
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
			float pricePerUnit = 0.89f;
			float grossPerUnit = Mathf.Max(0f, pricePerUnit - pressingCostPerUnit);
			var artist = ArtistManager.Instance?.GetArtist(runtimeData.baseRecord.artistId);
			float artistRoyalty = artist?.royaltyRate ?? 0.05f;
			float skimFraction = label.activeDeal != null
				? Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f)
				: 0.25f * (1f - label.ownedReach);
			float retailGross = weeklyUnits * pricePerUnit;
			float cogs = weeklyUnits * pressingCostPerUnit;
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
			RouteDistributionSkim(label, skimAmount);
			
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

	private void RouteDistributionSkim(AILabel client, float skimAmount) {
		DistributionDeal deal = client.activeDeal;
		if (deal == null || skimAmount <= 0f) return;
		AILabel distributor = GetLabel(deal.distributorId);
		if (distributor == null || distributor == client) return;

		float recouped = Mathf.Min(Mathf.Max(0f, deal.unrecoupedAdvance), skimAmount);
		deal.unrecoupedAdvance = Mathf.Max(0f, deal.unrecoupedAdvance - recouped);
		distributor.cashReserves += skimAmount;
		distributor.monthlyRevenue += skimAmount;
		distributor.weeklyDistributionIncome += skimAmount;
		if (labelFinancials.TryGetValue(distributor.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthRevenue += skimAmount;
		}
	}
	
	private void ProcessWeeklyReleases(GameDate date) {
		int releasesThisWeek = 0;
		foreach (var label in aiLabels) {
			if (label.status == LabelStatus.Bankrupt || label.status == LabelStatus.Defunct) continue;
			if (label.roster.Count == 0) continue;
			
			float releaseChance = CalculateWeeklyReleaseChance(label);
			if (GD.Randf() < releaseChance) {
				if (TryReleaseRecord(label, date)) releasesThisWeek++;
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
		var artist = RosterManager.Instance?.GetArtistForRelease(label) ?? label.GetArtistForRelease(date.year);
		if (artist == null) return false;

		var record = GenerateRecordFromArtist(label, artist, date.year);
		float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
		float noiseRange = Mathf.Lerp(0.30f, 0.10f, label.scoutingAbility);
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
		float perceivedQualityMult = 0.6f + (perceivedQuality * 0.8f);

		float productionCost = label.GetProductionCost();
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
		
		if (debugMode) {
			GD.Print($"🎵 {label.labelName}: '{record.title}' by {artist.stageName} (Quality: {(record.hookStrength + record.productionQuality) / 2f:F2}, Budget: ${totalCost:N0})");
		}
		return true;
	}
	
	private Record GenerateRecordFromArtist(AILabel label, SimulatedArtist artist, int year) {
		var record = new Record(); // Godot Resource instantiation
		generatedRecordCounter++;
		record.recordId = $"gen_{generatedRecordCounter}";
		record.labelId = label.labelId;
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
		
		return record;
	}
	
	private void ApplyReleasePromotion(Record record, SimulatedArtist artist, AILabel label, float marketingBudget, float perceivedQualityMult) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		
		float quality = runtimeData.GetQuality();
		float artistAwareness = artist.GetNewReleaseAwarenessBonus();
		float marketingAwareness = BudgetToImpact(marketingBudget, label.tier) * ChartSimulator.GetCampaignImpact(label) * 0.35f;
		float labelAwareness = label.reputation * 0.1f;
		
		runtimeData.awareness = Mathf.Clamp(0.08f + artistAwareness + marketingAwareness + labelAwareness, 0f, 1f);
		
		float baseRadio = quality * 0.3f;
		float pushRadio = ChartSimulator.GetCampaignImpact(label) * 0.3f;
		float payolaRadio = label.payolaWillingness * 0.15f;
		runtimeData.radioHeat = Mathf.Clamp(baseRadio + pushRadio + payolaRadio, 0f, 1f);
		
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
			
			regionalData.unitsInStores = ChartSimulator.CalculateInitialRegionalStock(label, region.regionId, stockScale, perceivedQualityMult);
			regionalData.awareness = Mathf.Clamp(runtimeData.awareness * regionStrength * (float)GD.RandRange(0.8, 1.1), 0f, 1f);
			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			regionalData.radioPlay = Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GD.RandRange(0.7, 1.0), 0f, 1f);
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
		if (debugMode) PrintMonthlyReport(date);
	}
	
	private void ProcessLabelMonth(AILabel label, GameDate date) {
		if (label.status == LabelStatus.Bankrupt || label.status == LabelStatus.Defunct) return;
		
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
		UpdateLabelStatus(label, financials, netIncome);
		
		label.monthlyRevenue = 0f;
		label.monthlyExpenses = 0f;
		financials.lastMonthRevenue = 0f;
		financials.lastMonthExpenses = 0f;
		
		if (date.month == 1) financials.totalReleasesThisYear = 0;
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

	public void RecordExpense(AILabel label, float amount) {
		if (label == null || amount <= 0f) return;
		label.cashReserves -= amount;
		label.monthlyExpenses += amount;
		if (labelFinancials.TryGetValue(label.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthExpenses += amount;
		}
	}
	
	public List<AILabel> GetActiveLabelsByStatus(LabelStatus status) => aiLabels?.Where(l => l.status == status).ToList() ?? new List<AILabel>();
	
	public List<AILabel> GetOperatingLabels() => aiLabels?.Where(l => l.status != LabelStatus.Bankrupt && l.status != LabelStatus.Defunct).ToList() ?? new List<AILabel>();
	
	public int GetLabelActiveRecordCount(string labelId) => labelActiveRecords.TryGetValue(labelId, out var records) ? records.Count : 0;
	
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
