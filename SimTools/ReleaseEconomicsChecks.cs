using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ReleaseEconomicsChecks {
	public static void Run() {
		bool saved = ReleaseAllInEconomics.Enabled;
		int checks = 0;
		void Check(bool value, string why) { if (!value) throw new InvalidOperationException("Release economics: " + why); checks++; }
		try {
			ReleaseAllInEconomics.SetForChecks(false, false);
			Check(!ReleaseAllInEconomics.ShouldCommit(0f) && !ReleaseAllInEconomics.ShouldCommit(-1f) &&
				!ReleaseAllInEconomics.ShouldCommit(float.NaN) && ReleaseAllInEconomics.ShouldCommit(1f), "positive finite profit is required");
			Check(Math.Abs(ReleaseAllInEconomics.ExpectedCampaign(1000f, .5f) - 1000f) < .001f,
				"campaign forecast uses the same perceived-quality scale as actual budgets");
			var record = new Record { recordId = "cost-probe", format = ReleaseFormat.Single };
			var runtime = new RecordRuntimeData(record) { lifetimeLabelNet = 500f, sunkProductionCost = 100f };
			string legacy = JsonSerializer.Serialize(runtime, SaveGameService.TestJsonOptions);
			Check(!legacy.Contains("sunkMarketingCost") && !legacy.Contains("SunkReleaseCost") && runtime.SunkReleaseCost == 100f,
				"legacy JSON and cost basis remain unchanged");
			runtime.sunkMarketingCost = 50f;
			var restored = JsonSerializer.Deserialize<RecordRuntimeData>(JsonSerializer.Serialize(runtime, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(restored.sunkMarketingCost == 50f && restored.SunkReleaseCost == 150f, "AI runtime marketing cost round-trips");
			Check(RuntimeRecordSaveData.From(runtime).ToRuntime().sunkMarketingCost == 50f, "player-record DTO preserves an attributed cost");
			var economics = CompetitorManager.GetResponsiveMemoryEconomicsForProbe(500f, runtime.SunkReleaseCost, 850f, .5f, false);
			Check(economics.EstimatedOutcome == 850f && economics.AgeMatchedExpected == 350f,
				"provisional estimates annualize receipts and subtract production plus marketing once");
			var memory = new FormatRevenueMemory();
			memory.observations.Add(new() { releaseId = "old", careerBand = 0, normalizedResidual = 3f, maturityWeight = 1f });
			memory.observations.Add(new() { releaseId = "new", careerBand = 0, normalizedResidual = -1f, maturityWeight = 1f, allInEconomics = true });
			var manager = CompetitorManager.Instance;
			float pooled = manager.ResponsiveMemoryForProbe(memory, 10, 0).Residual;
			ReleaseAllInEconomics.SetForChecks(true, true);
			Check(manager.ResponsiveMemoryForProbe(memory, 10, 0).Residual == -1f, "all-in memory excludes outcomes with unknown campaign cost");
			ReleaseAllInEconomics.SetForChecks(false, false);
			Check(manager.ResponsiveMemoryForProbe(memory, 10, 0).Residual == pooled, "changing cost basis cannot reuse the wrong same-week cache");
			var counts = new Dictionary<Genre, int> { [Genre.Country] = 1, [Genre.Folk] = 3 };
			CompetitorManager.UndoSupplySlot(counts, Genre.Country); CompetitorManager.UndoSupplySlot(counts, Genre.Folk);
			Check(!counts.ContainsKey(Genre.Country) && counts[Genre.Folk] == 2, "waiting restores provisional genre-supply slots");

			ReleaseAllInEconomics.SetForChecks(true, true);
			var label = manager.GetAllLabels().First(l => l.IsActive && l.roster.Any(a => a.members?.Count > 0 && CompetitorManager.IsEligibleForEnabledFormatDecision(a)));
			var artist = label.roster.First(a => a.members?.Count > 0 && CompetitorManager.IsEligibleForEnabledFormatDecision(a));
			float reach = label.ownedReach; var deal = label.activeDeal; var discography = artist.releasedSingleIds;
			float cash = label.cashReserves; int cooldown = artist.weeksSinceLastRelease, releases = artist.totalReleases, contractReleases = artist.contractReleases;
			int records = ChartManager.Instance.GetAllRecords().Count, songs = CompositionCatalogService.SongCount, projects = manager.GetAlbumProjects().Count;
			ReleaseProfitGateTelemetry decision = null;
			void Observe(ReleaseProfitGateTelemetry d) => decision = d;
			manager.OnReleaseProfitGate += Observe;
			try {
				label.ownedReach = 0f; label.activeDeal = null; artist.releasedSingleIds = new();
				bool committed = manager.TryReleaseForEconomicsProbe(label, artist, TimeManager.Instance.CurrentDate);
				Check(!committed && decision?.Commit == false && (decision.SelectedProfit <= 0f || decision.AlbumProfit <= 0f),
					"the actual release fork passes a loss before creating material");
				Check(label.cashReserves == cash && artist.weeksSinceLastRelease == cooldown && artist.totalReleases == releases &&
					artist.contractReleases == contractReleases && ChartManager.Instance.GetAllRecords().Count == records &&
					CompositionCatalogService.SongCount == songs && manager.GetAlbumProjects().Count == projects,
					"passing consumes no cash, master, cooldown, contract release or album project");
			} finally { label.ownedReach = reach; label.activeDeal = deal; artist.releasedSingleIds = discography; manager.OnReleaseProfitGate -= Observe; }
			Check(SaveGameService.WritingVersion >= 25, "active all-in worlds require the reader that retains campaign costs");
			var existing = ChartManager.Instance.GetAllRecords()[0]; var oldCost = existing.sunkMarketingCost;
			try {
				existing.sunkMarketingCost = 0f; ReleaseAllInEconomics.SetForChecks(false, false);
				Check(SaveGameService.WritingVersion >= 25, "retained cost facts require v25 even after the switch is disabled");
			} finally { existing.sunkMarketingCost = oldCost; }
			GD.Print($"RELEASE_ECONOMICS_CHECK passed checks={checks}");
		} finally { ReleaseAllInEconomics.SetForChecks(saved); }
	}
}
