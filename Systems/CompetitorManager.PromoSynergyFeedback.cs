using System;
using System.Linq;

public sealed class PromoSynergyDecisionTelemetry {
	public int Week, CareerBand;
	public string LabelId, ArtistId, ProjectId, Strategy;
	public float ExpectedPromoLift, PromoSynergyGain, CannibalizationLoss, RecruitmentScale, CannibalizationScale, EvidenceWeight;
}

public sealed class PromoSynergyOutcomeTelemetry {
	public int Week, CareerBand, PromoPeak;
	public string ProjectId, LabelId, AlbumRecordId;
	public float CreditedRecruitment, ModeledCannibalization, AlbumMarginPerUnit, AwarenessBonus, StockMultiplier;
	public double RawDemand, ShadowDemand, RecruitedUnits, SuppressedUnits;
}

public partial class CompetitorManager {
	public event Action<PromoSynergyDecisionTelemetry> OnPromoSynergyDecision;
	public event Action<PromoSynergyOutcomeTelemetry> OnPromoSynergyOutcome;
	private int promoFeedbackVersion, promoFeedbackCacheWeek = -1, promoFeedbackCacheVersion = -1;
	private readonly (float Recruitment, float Cannibalization, float Weight)[] promoFeedbackCache =
		new (float, float, float)[PromoSynergyFeedback.CareerCohorts];

	/// <summary>A5 scales for a career cohort. Evidence is read in project creation order, so a
	/// resumed world reproduces the same sums as an uninterrupted one.</summary>
	private (float Recruitment, float Cannibalization, float Weight) GetPromoSynergyScales(int careerBand, int currentWeek) {
		if (careerBand < 0 || careerBand >= PromoSynergyFeedback.CareerCohorts) return (1f, 1f, 0f);
		if (promoFeedbackCacheWeek != currentWeek || promoFeedbackCacheVersion != promoFeedbackVersion) {
			var evidence = albumProjects.Select(project => project.promoFeedback).Where(e => e?.finalWeek != null).ToList();
			for (int band = 0; band < PromoSynergyFeedback.CareerCohorts; band++)
				promoFeedbackCache[band] = PromoSynergyFeedback.Estimate(evidence, currentWeek, band);
			promoFeedbackCacheWeek = currentWeek;
			promoFeedbackCacheVersion = promoFeedbackVersion;
		}
		return promoFeedbackCache[careerBand];
	}

	internal (float Recruitment, float Cannibalization, float Weight) PromoSynergyScalesForProbe(int careerBand, int currentWeek) =>
		GetPromoSynergyScales(careerBand, currentWeek);

	internal void InvalidatePromoSynergyCacheForProbe() => promoFeedbackVersion++;
	internal void AddAlbumProjectForProbe(AlbumProject project) { albumProjects.Add(project); promoFeedbackVersion++; }
	internal void RemoveAlbumProjectForProbe(AlbumProject project) { albumProjects.Remove(project); promoFeedbackVersion++; }

	private void AttachPromoSynergyEvidence(AlbumProject project, AILabel label, SimulatedArtist artist, ReleasePlan plan) {
		if (!plan.promoFeedbackEvaluated || project.strategy != ReleaseStrategy.AlbumWithPromo || project.promoSingleRecord == null) return;
		project.promoFeedback = new PromoSynergyEvidence {
			careerBand = plan.promoFeedbackCareerBand,
			creditedRecruitment = Math.Max(0f, plan.expectedPromoLift + plan.promoSynergyGain),
			modeledCannibalization = Math.Max(0f, plan.cannibalizationLoss),
			albumMarginPerUnit = plan.albumMarginPerUnit,
			recruitmentScale = plan.promoRecruitmentScale, cannibalizationScale = plan.promoCannibalizationScale
		};
		OnPromoSynergyDecision?.Invoke(new() {
			Week = pipelineWeek, CareerBand = plan.promoFeedbackCareerBand, LabelId = label.labelId, ArtistId = artist.artistId,
			ProjectId = project.projectId, Strategy = project.strategy.ToString(), ExpectedPromoLift = plan.expectedPromoLift,
			PromoSynergyGain = plan.promoSynergyGain, CannibalizationLoss = plan.cannibalizationLoss,
			RecruitmentScale = plan.promoRecruitmentScale, CannibalizationScale = plan.promoCannibalizationScale,
			EvidenceWeight = plan.promoFeedbackEvidenceWeight
		});
	}

	internal void FinalizePromoSynergyEvidence(AlbumProject project, RecordRuntimeData album) {
		PromoSynergyEvidence evidence = project?.promoFeedback;
		if (evidence == null || evidence.finalWeek != null || album?.promoShadowDemand == null) return;
		double raw = album.rawAlbumDemandBeforeCannibalization, shadow = album.promoShadowDemand.Value;
		evidence.finalWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? pipelineWeek;
		evidence.realizedRecruitmentUnits = Math.Max(0d, raw - shadow);
		evidence.realizedSuppressedUnits = Math.Max(0d, album.suppressedAlbumDemand);
		promoFeedbackVersion++;
		OnPromoSynergyOutcome?.Invoke(new() {
			Week = evidence.finalWeek.Value, CareerBand = evidence.careerBand, PromoPeak = project.promoPeakAtDrop,
			ProjectId = project.projectId, LabelId = album.baseRecord.labelId, AlbumRecordId = album.baseRecord.recordId,
			CreditedRecruitment = evidence.creditedRecruitment, ModeledCannibalization = evidence.modeledCannibalization,
			AlbumMarginPerUnit = evidence.albumMarginPerUnit, AwarenessBonus = project.synergyAwarenessApplied,
			StockMultiplier = project.synergyStockMultiplier, RawDemand = raw, ShadowDemand = shadow,
			RecruitedUnits = evidence.realizedRecruitmentUnits.Value, SuppressedUnits = evidence.realizedSuppressedUnits.Value
		});
	}
}
