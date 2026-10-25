using System.Globalization;
using System.IO;

public partial class ChartAuditRunner {
	private StreamWriter releaseProfitWriter, releaseCampaignWriter, allInOutcomeWriter;
	private StreamWriter singleCapitalRiskWriter;
	private StreamWriter singleCapitalOutcomeWriter;
	private void OpenReleaseEconomicsOutputs(string directory) {
		if (SingleCapitalRisk.Enabled) {
			singleCapitalRiskWriter = CreateWriter(Path.Combine(directory, $"{runName}-single-capital-risk.csv"));
			singleCapitalRiskWriter.WriteLine("week,labelId,artistId,slotId,tier,cash,reserve,overhead,capital,forecastNet,marketing,samples,effectiveWeight,distributionMean,lossProbability,exhaustionProbability,riskPremium,adjustedNet,commit,modelVersion,policy,careerBand,prepared,expectedContribution,recording,pressing,futureCost,effectiveSamples,ready,status,meanError,lossFloor,minNet,maxNet,meanCommit,logCommit,forecastClamped");
			CompetitorManager.Instance.OnSingleCapitalRisk += WriteSingleCapitalRisk;
			singleCapitalOutcomeWriter = CreateWriter(Path.Combine(directory, $"{runName}-single-capital-risk-outcomes.csv"));
			singleCapitalOutcomeWriter.WriteLine("week,recordId,labelId,expectedContribution,finalContribution,productionAndInitialPressing,sunkMarketing,realizedNet");
			CompetitorManager.Instance.OnReleaseOutcome += WriteSingleCapitalOutcome;
		}
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
	private void WriteSingleCapitalRisk(SingleCapitalRiskTelemetry d) {
		var r = d.Risk;
		singleCapitalRiskWriter.WriteLine(string.Join(",", d.Week, Csv(d.LabelId), Csv(d.ArtistId), Csv(d.SlotId), Csv(d.Tier),
			Exact(d.Cash), Exact(d.Reserve), Exact(d.Overhead), Exact(d.Capital), Exact(d.Forecast), Exact(d.Marketing), r.Samples,
			r.Weight.ToString("R", CultureInfo.InvariantCulture), r.DistributionMean.ToString("R", CultureInfo.InvariantCulture),
			r.LossProbability.ToString("R", CultureInfo.InvariantCulture), r.ExhaustionProbability.ToString("R", CultureInfo.InvariantCulture),
			r.Premium.ToString("R", CultureInfo.InvariantCulture), r.AdjustedNet.ToString("R", CultureInfo.InvariantCulture), r.Commit ? "true" : "false",
			SingleCapitalRisk.ModelVersion, SingleCapitalRisk.Policy.ToString().ToLowerInvariant(), d.CareerBand, d.Prepared ? "true" : "false",
			d.Candidate.ExpectedContribution.ToString("R", CultureInfo.InvariantCulture), d.Candidate.Recording.ToString("R", CultureInfo.InvariantCulture),
			d.Candidate.Pressing.ToString("R", CultureInfo.InvariantCulture), d.Candidate.FutureCost.ToString("R", CultureInfo.InvariantCulture),
			r.EffectiveSamples.ToString("R", CultureInfo.InvariantCulture), r.Ready ? "true" : "false", Csv(r.Status),
			r.MeanError.ToString("R", CultureInfo.InvariantCulture), r.LossFloor.ToString("R", CultureInfo.InvariantCulture),
			r.MinimumNet.ToString("R", CultureInfo.InvariantCulture), r.MaximumNet.ToString("R", CultureInfo.InvariantCulture),
			r.MeanCommit ? "true" : "false", r.LogCommit ? "true" : "false", d.ForecastClamped ? "true" : "false"));
	}
	private void WriteReleaseProfitGate(ReleaseProfitGateTelemetry d) => releaseProfitWriter?.WriteLine(string.Join(",", new[] {
		d.Week.ToString(CultureInfo.InvariantCulture), Csv(d.LabelId), Csv(d.ArtistId), Csv(d.Genre), Csv(d.CareerState), Csv(d.Strategy),
		d.ResolvedSingles.ToString(CultureInfo.InvariantCulture), Exact(d.ExpectedCampaign), Exact(d.SingleProfit), Exact(d.AlbumProfit),
		Exact(d.ProjectProfit), Exact(d.SelectedProfit), Exact(d.PortfolioCredit), d.Commit ? "true" : "false"
	}));
	private void WriteSingleCapitalOutcome(ReleaseOutcomeTelemetry d) {
		if (d.capitalRiskExpectedContribution is not float prior) return;
		singleCapitalOutcomeWriter?.WriteLine(string.Join(",", currentAuditWeek, Csv(d.recordId), Csv(d.labelId), Exact(prior),
			Exact(d.lifetimeLabelNet), Exact(d.sunkProductionCost), d.sunkMarketingCost.HasValue ? Exact(d.sunkMarketingCost.Value) : "",
			Exact(d.realizedNet)));
	}
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
			CompetitorManager.Instance.OnSingleCapitalRisk -= WriteSingleCapitalRisk;
			CompetitorManager.Instance.OnReleaseOutcome -= WriteSingleCapitalOutcome;
			CompetitorManager.Instance.OnReleaseProfitGate -= WriteReleaseProfitGate;
			CompetitorManager.Instance.OnReleaseCampaignCost -= WriteReleaseCampaignCost;
			CompetitorManager.Instance.OnReleaseOutcome -= WriteAllInReleaseOutcome;
		}
		releaseProfitWriter?.Dispose(); releaseCampaignWriter?.Dispose(); allInOutcomeWriter?.Dispose();
		releaseProfitWriter = releaseCampaignWriter = allInOutcomeWriter = null;
		singleCapitalRiskWriter?.Dispose(); singleCapitalRiskWriter = null;
		singleCapitalOutcomeWriter?.Dispose(); singleCapitalOutcomeWriter = null;
	}
}
