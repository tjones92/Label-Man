using System;
using Godot;

public sealed class ReleaseProfitGateTelemetry {
	public string LabelId, ArtistId, Genre, CareerState, Strategy;
	public int Year, Week, ResolvedSingles;
	public float ExpectedCampaign, SingleProfit, AlbumProfit, ProjectProfit, SelectedProfit, PortfolioCredit;
	public bool Commit;
}

public sealed class ReleaseCampaignCostTelemetry {
	public string RecordId, LabelId;
	public float Production, Marketing, ExpectedNet;
}

public partial class CompetitorManager {
	internal bool TryReleaseForEconomicsProbe(AILabel label, SimulatedArtist artist, GameDate date) =>
		TryReleaseRecord(label, date, scheduledSingleArtist: artist);
	public event Action<ReleaseProfitGateTelemetry> OnReleaseProfitGate;
	public event Action<ReleaseCampaignCostTelemetry> OnReleaseCampaignCost;

	private bool PassesReleaseProfitGate(AILabel label, SimulatedArtist artist, int year, Genre genre, ReleasePlan plan) {
		if (!ReleaseAllInEconomics.IsLive || !plan.economicsEvaluated) return true;
		float albumProfit = plan.projectedAlbumStandaloneNet - plan.albumPortfolioCredit;
		float projectProfit = plan.projectedAlbumWithPromoNet - plan.albumPortfolioCredit;
		float selected = plan.format == ReleaseFormat.Single ? plan.projectedOrphanSingleNet :
			plan.strategy == ReleaseStrategy.AlbumWithPromo ? projectProfit : albumProfit;
		bool commit = ReleaseAllInEconomics.ShouldCommit(selected) &&
			(plan.format != ReleaseFormat.Album || ReleaseAllInEconomics.ShouldCommit(albumProfit));
		OnReleaseProfitGate?.Invoke(new() {
			LabelId = label.labelId, ArtistId = artist.artistId, Year = year, Week = pipelineWeek,
			Genre = genre.ToString(), CareerState = artist.careerState.ToString(), Strategy = plan.strategy.ToString(),
			ExpectedCampaign = plan.expectedMarketingBudget, SingleProfit = plan.projectedOrphanSingleNet,
			AlbumProfit = albumProfit, ProjectProfit = projectProfit, SelectedProfit = selected,
			PortfolioCredit = plan.albumPortfolioCredit, ResolvedSingles = plan.resolvedSingles, Commit = commit
		});
		if (!commit) {
			// Genre selection provisionally books one supply slot before the format fork.
			// Waiting creates no release and must not satisfy the annual supply floor.
			if (annualGenreSupplyByLabel.TryGetValue(label.labelId, out var recent)) UndoSupplySlot(recent, genre);
			UndoSupplySlot(annualGenreSupplyGlobal, genre);
		}
		return commit;
	}

	internal static void UndoSupplySlot(System.Collections.Generic.Dictionary<Genre, int> counts, Genre genre) {
		if (!counts.TryGetValue(genre, out int count)) return;
		if (count <= 1) counts.Remove(genre); else counts[genre] = count - 1;
	}

	private void AttributeReleaseCampaign(RecordRuntimeData runtime, float actualMarketing) {
		if (runtime?.sunkMarketingCost == null) return;
		runtime.sunkMarketingCost = Mathf.Max(0f, actualMarketing);
		runtime.releaseTimeOpportunityScale = Mathf.Max(1f, Mathf.Max(Mathf.Abs(runtime.releaseTimeExpectedNet), runtime.SunkReleaseCost));
		OnReleaseCampaignCost?.Invoke(new() {
			RecordId = runtime.baseRecord.recordId, LabelId = runtime.baseRecord.labelId,
			Production = runtime.sunkProductionCost, Marketing = runtime.sunkMarketingCost.Value,
			ExpectedNet = runtime.releaseTimeExpectedNet
		});
	}
}
