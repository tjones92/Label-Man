using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RosterManager : Node {
	public static RosterManager Instance { get; private set; }
	
	[ExportGroup("Configuration")]
	[Export] private float weeklyScoutChance = 0.08f;
	[Export] private float monthlyRosterReviewChance = 0.5f;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	
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
	
	public void InitializeAllRosters(List<AILabel> labels, int year) {
		GD.Print($"RosterManager: Initializing rosters for {labels.Count} labels...");
		foreach (var label in labels) {
			label.InitializeRoster();
			PopulateInitialRoster(label, year);
		}
		if (debugMode) PrintRosterStats(labels);
		GD.Print("RosterManager: Initialization complete");
	}
	
	private void PopulateInitialRoster(AILabel label, int year) {
		float fillRatio = label.tier switch {
			LabelTier.Major => (float)GD.RandRange(0.6, 0.85),
			LabelTier.MidTier => (float)GD.RandRange(0.5, 0.75),
			LabelTier.Independent => (float)GD.RandRange(0.4, 0.7),
			LabelTier.Small => (float)GD.RandRange(0.3, 0.6),
			LabelTier.Boutique => (float)GD.RandRange(0.5, 0.8),
			_ => 0.5f
		};
		
		int targetSize = Mathf.RoundToInt(label.maxRosterSize * fillRatio);
		for (int i = 0; i < targetSize; i++) {
			var artist = FindArtistForLabel(label, year);
			if (artist != null) InitialSignArtist(label, artist, year);
		}
	}
	
	private SimulatedArtist FindArtistForLabel(AILabel label, int year) {
		if (ArtistManager.Instance == null) return null;
		var candidates = new List<SimulatedArtist>();
		
		foreach (var genre in label.preferredGenres) {
			candidates.AddRange(ArtistManager.Instance.GetUnsignedByGenre(genre));
		}
		
		if (candidates.Count == 0) candidates = ArtistManager.Instance.GetUnsignedArtists();
		if (candidates.Count == 0) return null;
		
		var scored = candidates
			.Select(a => (artist: a, score: ScoreArtistForLabel(a, label)))
			.Where(x => x.score > 0)
			.OrderByDescending(x => x.score)
			.Take(10)
			.ToList();
		
		if (scored.Count == 0) return null;
		
		float totalScore = scored.Sum(s => s.score);
		float roll = (float)GD.RandRange(0f, totalScore);
		float cumulative = 0f;
		
		foreach (var (artist, score) in scored) {
			cumulative += score;
			if (roll <= cumulative) return artist;
		}
		return scored[0].artist;
	}
	
	private float ScoreArtistForLabel(SimulatedArtist artist, AILabel label) {
		float score = 0f;
		float quality = artist.CalculateBaseQuality();
		score += quality * (0.5f + label.scoutingAbility * 0.5f);
		
		if (label.preferredGenres.Contains(artist.primaryGenre)) score += 0.4f;
		else if (label.secondaryGenres != null && label.secondaryGenres.Contains(artist.primaryGenre)) score += 0.2f;
		
		if (artist.reputation < 0.1f) score *= 0.5f + (label.riskTolerance * 0.5f);
		score *= (float)GD.RandRange(0.8f, 1.2f);
		return score;
	}
	
	private void InitialSignArtist(AILabel label, SimulatedArtist artist, int year) {
		float advanceRange = label.tier switch {
			LabelTier.Major => (float)GD.RandRange(2000f, 8000f),
			LabelTier.MidTier => (float)GD.RandRange(800f, 3000f),
			LabelTier.Independent => (float)GD.RandRange(300f, 1200f),
			LabelTier.Small => (float)GD.RandRange(100f, 500f),
			LabelTier.Boutique => (float)GD.RandRange(200f, 800f),
			_ => (float)GD.RandRange(200f, 800f)
		};
		
		artist.labelId = label.labelId;
		artist.signedYear = year - (int)GD.RandRange(0, 5);
		artist.careerState = CareerState.NewSigning;
		artist.royaltyRate = label.CalculateRoyaltyRate(artist);
		artist.unrecoupedAdvance = advanceRange;
		artist.contractLength = (int)GD.RandRange(3, 6);
		artist.contractExpiresYear = year + artist.contractLength;
		artist.weeksSinceLastRelease = (int)GD.RandRange(0, 52);
		
		if (GD.Randf() < 0.3f) {
			artist.totalReleases = (int)GD.RandRange(1, 5);
			artist.weeksSinceLastRelease = (int)GD.RandRange(4, 30);
			
			if (GD.Randf() < 0.4f) {
				artist.top40Hits = (int)GD.RandRange(1, 3);
				artist.careerState = CareerState.Rising;
				artist.momentum = (float)GD.RandRange(0.1f, 0.4f);
				artist.reputation = (float)GD.RandRange(0.1f, 0.3f);
			}
			if (GD.Randf() < 0.15f) {
				artist.top10Hits = (int)GD.RandRange(1, 2);
				artist.careerState = CareerState.Established;
				artist.momentum = (float)GD.RandRange(0.2f, 0.5f);
				artist.reputation = (float)GD.RandRange(0.2f, 0.5f);
			}
		}
		
		label.roster.Add(artist);
		ArtistManager.Instance.SignArtist(artist, label.labelId, artist.signedYear);
	}
	
	private void OnWeekEnded(GameDate date) {
		UpdateArtistCooldowns();
		if (GD.Randf() < weeklyScoutChance) ProcessScouting(date.year);
	}
	
	private void UpdateArtistCooldowns() {
		var labels = GetAllLabels();
		if (labels == null) return;
		foreach (var label in labels) {
			foreach (var artist in label.roster) artist.weeksSinceLastRelease++;
		}
	}
	
	private void ProcessScouting(int year) {
		var labels = GetAllLabels();
		if (labels == null) return;
		
		var scoutingLabels = labels.Where(l => l.ShouldScoutNewArtist()).OrderBy(_ => GD.Randf()).Take(3);
		foreach (var label in scoutingLabels) TrySignNewArtist(label, year);
	}
	
	private void TrySignNewArtist(AILabel label, int year) {
		var candidates = ArtistManager.Instance.GetTopUnsignedTalent(20, label.preferredGenres.FirstOrDefault());
		if (candidates.Count == 0) return;
		
		var bestCandidate = label.EvaluateForSigning(candidates);
		if (bestCandidate != null && label.CanAffordToSign(label.CalculateAdvanceOffer(bestCandidate))) {
			float advance = label.SignArtist(bestCandidate, year);
			CompetitorManager.Instance?.RecordExpense(label, advance);
			ArtistManager.Instance.SignArtist(bestCandidate, label.labelId, year);
			if (debugMode) GD.Print($"SIGNING: {label.labelName} signs {bestCandidate.stageName} ({bestCandidate.primaryGenre})");
		}
	}
	
	private void OnMonthChanged(GameDate date) {
		var labels = GetAllLabels();
		if (labels == null) return;
		foreach (var label in labels) {
			ProcessContractExpirations(label, date.year);
			if (GD.Randf() < monthlyRosterReviewChance) ProcessRosterReview(label, date.year);
		}
	}
	
	private void ProcessContractExpirations(AILabel label, int year) {
		var expiring = label.roster.Where(a => a.contractExpiresYear <= year).ToList();
		foreach (var artist in expiring) {
			bool wantToResign = ShouldResignArtist(label, artist);
			if (wantToResign && label.CanAffordToSign(label.CalculateAdvanceOffer(artist))) {
				float newAdvance = label.CalculateAdvanceOffer(artist);
				artist.unrecoupedAdvance = newAdvance;
				artist.contractLength = label.CalculateContractLength(artist);
				artist.contractExpiresYear = year + artist.contractLength;
				artist.royaltyRate = label.CalculateRoyaltyRate(artist);
				CompetitorManager.Instance?.RecordExpense(label, newAdvance);
				artist.careerEvents.Add($"{year}: Re-signed with {label.labelName}");
				if (debugMode) GD.Print($"RE-SIGN: {label.labelName} re-signs {artist.stageName}");
			} else {
				label.DropArtist(artist, year, "contract expired");
				ArtistManager.Instance.DropArtist(artist, year);
				if (debugMode) GD.Print($"CONTRACT END: {artist.stageName} leaves {label.labelName}");
			}
		}
	}
	
	private bool ShouldResignArtist(AILabel label, SimulatedArtist artist) {
		if (artist.careerState >= CareerState.Star) return true;
		if (artist.careerState == CareerState.Rising && artist.momentum > 0.2f) return true;
		if (artist.careerState == CareerState.Established && artist.consecutiveFlops < 2) return true;
		if (artist.careerState == CareerState.Declining) return false;
		if (artist.careerState == CareerState.NewSigning && artist.totalReleases >= 2 && artist.top40Hits == 0) return false;
		return GD.Randf() < label.artistLoyalty;
	}
	
	private void ProcessRosterReview(AILabel label, int year) {
		var toReview = label.roster.Where(a => label.ShouldDropArtist(a)).ToList();
		foreach (var artist in toReview) {
			label.DropArtist(artist, year, "poor performance");
			ArtistManager.Instance.DropArtist(artist, year);
			if (debugMode) GD.Print($"DROPPED: {label.labelName} drops {artist.stageName} (flops: {artist.consecutiveFlops})");
		}
	}
	
	public SimulatedArtist GetArtistForRelease(AILabel label) => label.GetArtistForRelease(TimeManager.Instance?.CurrentDate.year ?? 1960);
	
	public void RecordReleased(SimulatedArtist artist, string recordId) {
		artist.weeksSinceLastRelease = 0;
		artist.totalReleases++;
		artist.releaseHistory.Add(recordId);
	}
	
	public void RecordChartRunComplete(SimulatedArtist artist, RecordRuntimeData record) {
		if (artist == null || record == null || record.artistChartRunCompleted) return;
		artist.UpdateAfterChartRun(record.peakPosition, record.weeksOnChart, record.totalUnitsSold);
		record.artistChartRunCompleted = true;
		var label = GetLabelById(artist.labelId);
		if (label != null && label.ShouldDropArtist(artist)) {
			int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
			label.DropArtist(artist, year, "poor performance");
			ArtistManager.Instance.DropArtist(artist, year);
		}
	}
	
	private List<AILabel> GetAllLabels() => ChartManager.Instance?.GetAllLabels();
	private AILabel GetLabelById(string labelId) => ChartManager.Instance?.GetLabelById(labelId);
	
	private void PrintRosterStats(List<AILabel> labels) {
		GD.Print("=== ROSTER STATS ===");
		int totalArtists = labels.Sum(l => l.roster.Count);
		int totalCapacity = labels.Sum(l => l.maxRosterSize);
		GD.Print($"Total Artists Signed: {totalArtists} / {totalCapacity} capacity ({100f * totalArtists / totalCapacity:F1}%)");
		
		var byTier = labels.GroupBy(l => l.tier);
		foreach (var group in byTier) {
			int signed = group.Sum(l => l.roster.Count);
			int capacity = group.Sum(l => l.maxRosterSize);
			GD.Print($"  {group.Key}: {signed} / {capacity}");
		}
	}
	
	public void DebugPrintRosterStats() {
		var labels = GetAllLabels();
		if (labels != null) PrintRosterStats(labels);
	}
}
