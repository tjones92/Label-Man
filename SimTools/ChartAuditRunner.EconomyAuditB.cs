using System.Globalization;
using System.IO;

/// <summary>Telemetry for the economy audit's B-section switches; each file is written only while its switch is on.</summary>
public partial class ChartAuditRunner {
	private StreamWriter distributionAdvanceWriter, recordCampaignWriter;
	private void OpenEconomyAuditBOutputs(string directory) {
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
		if (CompetitorManager.Instance != null) {
			CompetitorManager.Instance.OnDistributionAdvanceRecoupment -= WriteDistributionAdvance;
			CompetitorManager.Instance.OnRecordCampaign -= WriteRecordCampaign;
		}
		distributionAdvanceWriter?.Dispose(); recordCampaignWriter?.Dispose();
		distributionAdvanceWriter = recordCampaignWriter = null;
	}
}
