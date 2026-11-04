using System.Globalization;
using System.IO;
using System.Linq;

public partial class ChartAuditRunner {
	private StreamWriter albumPracticeWriter;
	private void OpenAlbumPracticeOutputs(string directory) {
		// The release slate takes advance-single decisions through the same rule.
		if (!AlbumReleasePractice.Enabled && !Godot.OS.GetCmdlineUserArgs().Contains(LabelReleaseProgramme.AllocationSwitch)) return;
		albumPracticeWriter = CreateWriter(Path.Combine(directory, $"{runName}-album-practice.csv"));
		albumPracticeWriter.WriteLine("week,year,labelId,artistId,albumId,singleId,genre,format,route,reason,commissionProbability,roll,singleContribution,preparationCost,campaignCost,lpLift,cannibalization,delayCost,incrementalNet,sourceAlbumAge,sourceAlbumPeak,opportunityCost,masterId,songId,hook,production,priorChartedSingles");
		CompetitorManager.Instance.OnAlbumPracticeDecision += OnAlbumPracticeDecision;
	}

	private void OnAlbumPracticeDecision(AlbumPracticeTelemetry row) {
		albumPracticeWriter.WriteLine(string.Join(",", new[] {
			row.Week.ToString(CultureInfo.InvariantCulture), row.Year.ToString(CultureInfo.InvariantCulture),
			Csv(row.LabelId), Csv(row.ArtistId), Csv(row.AlbumId), Csv(row.SingleId), Csv(row.Genre), Csv(row.Format),
			Csv(row.Route), Csv(row.Reason), F(row.CommissionProbability), F(row.Roll), F(row.SingleContribution),
			F(row.PreparationCost), F(row.CampaignCost), F(row.LpLift), F(row.Cannibalization), F(row.DelayCost),
			F(row.IncrementalNet), row.SourceAlbumAge.ToString(CultureInfo.InvariantCulture), row.SourceAlbumPeak.ToString(CultureInfo.InvariantCulture),
			F(row.OpportunityCost), Csv(row.MasterId), Csv(row.SongId), F(row.Hook), F(row.Production), row.PriorChartedSingles.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private void CloseAlbumPracticeOutputs() {
		if (albumPracticeWriter == null) return;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnAlbumPracticeDecision -= OnAlbumPracticeDecision;
		albumPracticeWriter.Dispose(); albumPracticeWriter = null;
	}
}
