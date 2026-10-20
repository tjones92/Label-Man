using System.Globalization;
using System.IO;

public partial class ChartAuditRunner {
	private StreamWriter promoSynergyDecisionWriter, promoSynergyOutcomeWriter;
	private void OpenPromoSynergyOutputs(string directory) {
		if (!PromoSynergyFeedback.Enabled) return;
		promoSynergyDecisionWriter = CreateWriter(Path.Combine(directory, $"{runName}-promo-synergy-decisions.csv"));
		promoSynergyDecisionWriter.WriteLine("week,projectId,labelId,artistId,careerBand,expectedPromoLift,promoSynergyGain,cannibalizationLoss,recruitmentScale,cannibalizationScale,evidenceWeight");
		promoSynergyOutcomeWriter = CreateWriter(Path.Combine(directory, $"{runName}-promo-synergy-outcomes.csv"));
		promoSynergyOutcomeWriter.WriteLine("week,projectId,labelId,albumRecordId,careerBand,promoPeak,awarenessBonus,stockMultiplier,creditedRecruitment,modeledCannibalization,albumMarginPerUnit,rawDemand,shadowDemand,recruitedUnits,suppressedUnits");
		CompetitorManager.Instance.OnPromoSynergyDecision += WritePromoSynergyDecision;
		CompetitorManager.Instance.OnPromoSynergyOutcome += WritePromoSynergyOutcome;
	}
	private static string Exact(double value) => value.ToString("R", CultureInfo.InvariantCulture);
	private void WritePromoSynergyDecision(PromoSynergyDecisionTelemetry d) => promoSynergyDecisionWriter?.WriteLine(string.Join(",", new[] {
		d.Week.ToString(CultureInfo.InvariantCulture), Csv(d.ProjectId), Csv(d.LabelId), Csv(d.ArtistId), d.CareerBand.ToString(CultureInfo.InvariantCulture),
		Exact(d.ExpectedPromoLift), Exact(d.PromoSynergyGain), Exact(d.CannibalizationLoss), Exact(d.RecruitmentScale),
		Exact(d.CannibalizationScale), Exact(d.EvidenceWeight)
	}));
	private void WritePromoSynergyOutcome(PromoSynergyOutcomeTelemetry d) => promoSynergyOutcomeWriter?.WriteLine(string.Join(",", new[] {
		d.Week.ToString(CultureInfo.InvariantCulture), Csv(d.ProjectId), Csv(d.LabelId), Csv(d.AlbumRecordId), d.CareerBand.ToString(CultureInfo.InvariantCulture),
		d.PromoPeak.ToString(CultureInfo.InvariantCulture), Exact(d.AwarenessBonus), Exact(d.StockMultiplier), Exact(d.CreditedRecruitment),
		Exact(d.ModeledCannibalization), Exact(d.AlbumMarginPerUnit), Exact(d.RawDemand), Exact(d.ShadowDemand), Exact(d.RecruitedUnits), Exact(d.SuppressedUnits)
	}));
	private void ClosePromoSynergyOutputs() {
		if (CompetitorManager.Instance != null) {
			CompetitorManager.Instance.OnPromoSynergyDecision -= WritePromoSynergyDecision;
			CompetitorManager.Instance.OnPromoSynergyOutcome -= WritePromoSynergyOutcome;
		}
		promoSynergyDecisionWriter?.Dispose(); promoSynergyOutcomeWriter?.Dispose();
		promoSynergyDecisionWriter = promoSynergyOutcomeWriter = null;
	}
}
