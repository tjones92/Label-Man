using System.Globalization;
using System.IO;

public partial class ChartAuditRunner {
	private StreamWriter releaseProfitWriter, releaseCampaignWriter, allInOutcomeWriter;
	private void OpenReleaseEconomicsOutputs(string directory) {
		if (!ReleaseAllInEconomics.Enabled) return;
		releaseProfitWriter = CreateWriter(Path.Combine(directory, $"{runName}-release-profit-gates.csv"));
		releaseProfitWriter.WriteLine("week,labelId,artistId,genre,careerState,strategy,resolvedSingles,expectedCampaign,singleProfit,albumProfit,projectProfit,selectedProfit,portfolioCredit,commit");
		releaseCampaignWriter = CreateWriter(Path.Combine(directory, $"{runName}-release-campaign-costs.csv"));
		releaseCampaignWriter.WriteLine("week,recordId,labelId,productionCost,marketingCost,releaseTimeExpectedNet");
		allInOutcomeWriter = CreateWriter(Path.Combine(directory, $"{runName}-release-all-in-outcomes.csv"));
		allInOutcomeWriter.WriteLine("week,recordId,labelId,format,lifetimeLabelNet,sunkProductionCost,sunkMarketingCost,realizedNet");
		CompetitorManager.Instance.OnReleaseProfitGate += WriteReleaseProfitGate;
		CompetitorManager.Instance.OnReleaseCampaignCost += WriteReleaseCampaignCost;
		CompetitorManager.Instance.OnReleaseOutcome += WriteAllInReleaseOutcome;
	}
	private static string Exact(float value) => value.ToString("R", CultureInfo.InvariantCulture);
	private void WriteReleaseProfitGate(ReleaseProfitGateTelemetry d) => releaseProfitWriter?.WriteLine(string.Join(",", new[] {
		d.Week.ToString(CultureInfo.InvariantCulture), Csv(d.LabelId), Csv(d.ArtistId), Csv(d.Genre), Csv(d.CareerState), Csv(d.Strategy),
		d.ResolvedSingles.ToString(CultureInfo.InvariantCulture), Exact(d.ExpectedCampaign), Exact(d.SingleProfit), Exact(d.AlbumProfit),
		Exact(d.ProjectProfit), Exact(d.SelectedProfit), Exact(d.PortfolioCredit), d.Commit ? "true" : "false"
	}));
	private void WriteReleaseCampaignCost(ReleaseCampaignCostTelemetry d) => releaseCampaignWriter?.WriteLine(string.Join(",", new[] {
		currentAuditWeek.ToString(CultureInfo.InvariantCulture), Csv(d.RecordId), Csv(d.LabelId), Exact(d.Production), Exact(d.Marketing), Exact(d.ExpectedNet)
	}));
	private void WriteAllInReleaseOutcome(ReleaseOutcomeTelemetry d) {
		if (d.sunkMarketingCost == null) return;
		allInOutcomeWriter?.WriteLine(string.Join(",", new[] { currentAuditWeek.ToString(CultureInfo.InvariantCulture), Csv(d.recordId), Csv(d.labelId),
			Csv(d.format.ToString()), Exact(d.lifetimeLabelNet), Exact(d.sunkProductionCost), Exact(d.sunkMarketingCost.Value), Exact(d.realizedNet) }));
	}
	private void CloseReleaseEconomicsOutputs() {
		if (CompetitorManager.Instance != null) {
			CompetitorManager.Instance.OnReleaseProfitGate -= WriteReleaseProfitGate;
			CompetitorManager.Instance.OnReleaseCampaignCost -= WriteReleaseCampaignCost;
			CompetitorManager.Instance.OnReleaseOutcome -= WriteAllInReleaseOutcome;
		}
		releaseProfitWriter?.Dispose(); releaseCampaignWriter?.Dispose(); allInOutcomeWriter?.Dispose();
		releaseProfitWriter = releaseCampaignWriter = allInOutcomeWriter = null;
	}
}
