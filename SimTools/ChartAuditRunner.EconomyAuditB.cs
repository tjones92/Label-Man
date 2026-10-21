using System.Globalization;
using System.IO;

/// <summary>Telemetry for the economy audit's B-section switches; each file is written only while its switch is on.</summary>
public partial class ChartAuditRunner {
	private StreamWriter distributionAdvanceWriter, recordCampaignWriter, operatorPayWriter;
	private void OpenEconomyAuditBOutputs(string directory) {
		if (LabelOperatorPay.MonthlyLimit > 0f) {
			operatorPayWriter = CreateWriter(Path.Combine(directory, $"{runName}-operator-pay.csv"));
			operatorPayWriter.WriteLine("week,year,month,labelId,tier,operatingCashSurplus,pay,cashAfter,monthlyOverhead,outstandingPlantPayables,monthlyCap");
			LabelOperatorPay.Paid += WriteOperatorPay;
		}
		if (DistributionAdvanceRecoupment.Enabled) {
			distributionAdvanceWriter = CreateWriter(Path.Combine(directory, $"{runName}-distribution-advance-recoupment.csv"));
			distributionAdvanceWriter.WriteLine("week,kind,clientId,distributorId,resolution,amount,carriedNet,balanceAfter,advance");
			CompetitorManager.Instance.OnDistributionAdvanceRecoupment += WriteDistributionAdvance;
		}
		if (RecordCampaignSpend.Enabled) {
			recordCampaignWriter = CreateWriter(Path.Combine(directory, $"{runName}-record-campaigns.csv"));
			recordCampaignWriter.WriteLine("week,kind,recordId,labelId,artistId,weeksSinceRelease,position,spend,factorBefore,factorAfter,cashAfter");
			CompetitorManager.Instance.OnRecordCampaign += WriteRecordCampaign;
		}
	}
	private void WriteDistributionAdvance(DistributionAdvanceRecoupmentTelemetry d) {
		if (d.Kind == "Withheld" && forcedDealClient != null && d.ClientId == forcedDealClient.labelId) forcedDealWithheldTotal += d.Amount;
		distributionAdvanceWriter?.WriteLine(string.Join(",", new[] {
			d.Week.ToString(CultureInfo.InvariantCulture), Csv(d.Kind), Csv(d.ClientId), Csv(d.DistributorId), Csv(d.Resolution),
			Exact(d.Amount), Exact(d.CarriedNet), Exact(d.BalanceAfter), Exact(d.Advance)
		}));
	}
	private void WriteRecordCampaign(RecordCampaignTelemetry d) => recordCampaignWriter?.WriteLine(string.Join(",", new[] {
		d.Week.ToString(CultureInfo.InvariantCulture), Csv(d.Kind), Csv(d.RecordId), Csv(d.LabelId), Csv(d.ArtistId),
		d.WeeksSinceRelease.ToString(CultureInfo.InvariantCulture), d.Position.ToString(CultureInfo.InvariantCulture),
		Exact(d.Spend), Exact(d.FactorBefore), Exact(d.FactorAfter), Exact(d.CashAfter)
	}));
	private void CloseEconomyAuditBOutputs() {
		LabelOperatorPay.Paid -= WriteOperatorPay;
		if (CompetitorManager.Instance != null) {
			CompetitorManager.Instance.OnDistributionAdvanceRecoupment -= WriteDistributionAdvance;
			CompetitorManager.Instance.OnRecordCampaign -= WriteRecordCampaign;
		}
		distributionAdvanceWriter?.Dispose(); recordCampaignWriter?.Dispose();
		distributionAdvanceWriter = recordCampaignWriter = null;
		operatorPayWriter?.Dispose(); operatorPayWriter = null;
	}
	private void WriteOperatorPay(AILabel label, float surplus, float pay) {
		GameDate date = TimeManager.Instance.CurrentDate;
		operatorPayWriter?.WriteLine(string.Join(",", new[] { currentAuditWeek.ToString(CultureInfo.InvariantCulture),
			date.year.ToString(CultureInfo.InvariantCulture), date.month.ToString(CultureInfo.InvariantCulture),
			Csv(label.labelId), label.tier.ToString(), Exact(surplus), Exact(pay), Exact(label.cashReserves),
			Exact(label.GetMonthlyOverhead()), Exact(label.outstandingPlantPayables), Exact(LabelOperatorPay.MonthlyLimit) }));
	}
}
