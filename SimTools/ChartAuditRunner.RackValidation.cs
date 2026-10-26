using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
	private StreamWriter rackStoreValidationWriter;
	private StreamWriter rackClubValidationWriter;
	private string rackFinancePath;
	private long rackValidationClubTotal;
	private void OpenRackValidation(string directory) {
		if (RackJobberChannel.Enabled) rackFinancePath = Path.Combine(directory, $"{runName}-rack-finance-weekly.csv");
		if (!OS.GetCmdlineUserArgs().Contains("--rack-validation-audit")) return;
		rackStoreValidationWriter = CreateWriter(Path.Combine(directory, $"{runName}-rack-store-weekly.csv"));
		rackStoreValidationWriter.WriteLine("week,year,labelTier,lpUnits,liveLps,chartedLps,launchChartEntrants");
		rackClubValidationWriter = CreateWriter(Path.Combine(directory, $"{runName}-rack-club-weekly.csv"));
		rackClubValidationWriter.WriteLine("week,year,units");
		rackValidationClubTotal = RecordClubChannel.Annual.Where(r => r.Role != "Licensor").Sum(r => r.Units);
	}
	private readonly System.Collections.Generic.HashSet<(int, string)> rackValidationCharted = new();
	private void WriteRackValidation(ChartManager.CompletedWeekSettlement settlement) {
		if (rackStoreValidationWriter == null) return;
		long clubTotal = RecordClubChannel.Annual.Where(r => r.Role != "Licensor").Sum(r => r.Units);
		rackClubValidationWriter.WriteLine(string.Join(",", settlement.SettlementId.ToString(CultureInfo.InvariantCulture),
			settlement.Date.year.ToString(CultureInfo.InvariantCulture), (clubTotal - rackValidationClubTotal).ToString(CultureInfo.InvariantCulture)));
		rackValidationClubTotal = clubTotal;
		foreach (var tier in settlement.Entries.Where(e => e.Format == ReleaseFormat.Album).GroupBy(e => e.LabelTier)) {
			int entrants = 0;
			foreach (var entry in tier.Where(e => e.Record.currentPosition > 0)) {
				bool first = rackValidationCharted.Add((settlement.Date.year, entry.RecordId));
				if (first && demandObservations.TryGetValue(entry.RecordId, out var observed) && observed.Kind == "Launch") entrants++;
			}
			rackStoreValidationWriter.WriteLine(string.Join(",", settlement.SettlementId.ToString(CultureInfo.InvariantCulture),
				settlement.Date.year.ToString(CultureInfo.InvariantCulture), Csv(tier.Key), tier.Sum(e => (long)e.Units).ToString(CultureInfo.InvariantCulture),
				tier.Count().ToString(CultureInfo.InvariantCulture), tier.Count(e => e.Record.currentPosition > 0).ToString(CultureInfo.InvariantCulture),
				entrants.ToString(CultureInfo.InvariantCulture)));
		}
	}
	private void CloseRackValidation() {
		rackStoreValidationWriter?.Dispose(); rackStoreValidationWriter = null;
		rackClubValidationWriter?.Dispose(); rackClubValidationWriter = null;
		if (rackFinancePath == null) return;
		using var writer = CreateWriter(rackFinancePath);
		writer.WriteLine("week,year,labelId,labelTier,units,gross,soldCogs,returnWriteOff");
		foreach (var row in RackJobberChannel.FinanceRows)
			writer.WriteLine(string.Join(",", row.Week.ToString(CultureInfo.InvariantCulture), row.Year.ToString(CultureInfo.InvariantCulture),
				Csv(row.LabelId), Csv(row.LabelTier), row.Units.ToString(CultureInfo.InvariantCulture),
				row.Gross.ToString("R", CultureInfo.InvariantCulture), row.SoldCogs.ToString("R", CultureInfo.InvariantCulture),
				row.ReturnWriteOff.ToString("R", CultureInfo.InvariantCulture)));
	}
}
