using System;

public sealed class SingleCapitalRiskTelemetry {
	public string LabelId, ArtistId, SlotId, Tier;
	public int Week, CareerBand;
	public bool Prepared, ForecastClamped;
	public float Cash, Reserve, Overhead, Capital, Forecast, Marketing;
	public SingleCapitalRisk.Candidate Candidate;
	public SingleCapitalRisk.Assessment Risk;
}

public partial class CompetitorManager {
	private float SingleRiskInitialPressing(AILabel label) => LabelPressingEconomics.InitialRunReserve(label,
		ReleaseFormat.Single, GetPressingCostPerUnit(ReleaseFormat.Single));

	public event Action<SingleCapitalRiskTelemetry> OnSingleCapitalRisk;
	private bool PassesSingleCapitalRisk(AILabel label, SimulatedArtist artist, ReleaseSlateItem item,
		DecisionContext decision, float marketing, float unreserved) {
		if (!SingleCapitalRisk.Enabled || label.isPlayerOwned || item.Format != ReleaseFormat.Single) return true;
		// DecideRelease fills these indices before reading the prior; the funding read must do the same.
		decision.qualityQuartile = GetQualityQuartile(decision.qualityEstimate);
		decision.careerBand = GetCareerBandIndex(artist.careerState, out _);
		int band = GetMemoryCareerBand(artist.careerState, null);
		var memory = label.GetOrCreateRevenueMemory(RevenueEstimatorLane.OrphanSingle);
		float expectedCampaign = ReleaseAllInEconomics.IsLive
			? ReleaseAllInEconomics.ExpectedCampaign(label.GetMarketingBudget(artist), decision.qualityEstimate) : 0;
		float prior = SinglePriorNetAtBookedPrice(decision) - expectedCampaign, forecast = prior;
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true) {
			var responsive = GetResponsiveMemory(memory, pipelineWeek, CareerBandedFormatMemory.Enabled ? band : -1);
			forecast = SingleForecastRealization.Enabled
				? SingleForecastRealization.Forecast(prior, SingleForecastRealization.Ratio(memory.observations, pipelineWeek))
				: prior + responsive.Confidence * responsive.Residual * Math.Max(1f, Math.Abs(prior));
		}
		float beforeCampaign = forecast + expectedCampaign;
		float initial = SingleRiskInitialPressing(label);
		var candidate = SingleCapitalRisk.FromForecast(beforeCampaign, decision.singleProductionCost, marketing, initial, item.PreparedRecord != null);
		var distribution = SingleCapitalRisk.Build(memory.observations, pipelineWeek, candidate, band);
		var risk = SingleCapitalRisk.Evaluate(distribution, unreserved);
		OnSingleCapitalRisk?.Invoke(new() { LabelId = label.labelId, ArtistId = artist.artistId, SlotId = item.Id,
			Tier = label.tier.ToString(), Week = pipelineWeek, CareerBand = band, Prepared = item.PreparedRecord != null,
			ForecastClamped = beforeCampaign + decision.singleProductionCost + initial < 0,
			Cash = label.cashReserves, Reserve = label.cashReserves - unreserved,
			Overhead = label.GetMonthlyOverhead(), Capital = Math.Max(0, unreserved) * SingleCapitalRisk.CapitalFraction,
			Forecast = (float)candidate.Mean, Marketing = marketing, Candidate = candidate, Risk = risk });
		return risk.Commit;
	}

	private void CaptureSingleRiskExpectation(RecordRuntimeData runtime, AILabel label, SimulatedArtist artist, DecisionContext decision) {
		if (!SingleCapitalRisk.Enabled || runtime.projectRole != ProjectRecordRole.OrphanSingle) return;
		decision.qualityQuartile = GetQualityQuartile(decision.qualityEstimate);
		decision.careerBand = GetCareerBandIndex(artist.careerState, out _);
		// The net prior already includes initial pressing loss. Adding back that commitment reverses it
		// to recover contribution; the candidate subtracts the current commitment exactly once.
		runtime.capitalRiskExpectedContribution = Math.Max(0, SinglePriorNetAtBookedPrice(decision) +
			decision.singleProductionCost + SingleRiskInitialPressing(label));
	}
}
