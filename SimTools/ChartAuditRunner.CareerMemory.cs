using System.Globalization;
using System.IO;

// Optional A3 evidence, including calibration mode. The existing revision CSV keeps its frozen schema.
public partial class ChartAuditRunner {
	private StreamWriter careerMemoryWriter;

	private void OpenCareerMemoryOutputs(string directory) {
		if (!CareerBandedFormatMemory.Enabled) return;
		careerMemoryWriter = CreateWriter(Path.Combine(directory, $"{runName}-format-memory-career-revisions.csv"));
		careerMemoryWriter.WriteLine("week,chartWeek,releaseId,labelId,estimatorLane,careerBand,releaseWeek,releaseAge,revisionKind,normalizedResidual,maturityWeight");
	}

	private void WriteCareerMemoryRevision(FormatMemoryRevisionTelemetry revision) {
		if (careerMemoryWriter == null || revision == null) return;
		careerMemoryWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture),
			(ChartManager.Instance?.GetCurrentChartWeek() ?? 0).ToString(CultureInfo.InvariantCulture),
			Csv(revision.releaseId), Csv(revision.labelId), Csv(revision.estimatorLane.ToString()),
			revision.careerBand?.ToString(CultureInfo.InvariantCulture) ?? "Unknown",
			revision.releaseWeek.ToString(CultureInfo.InvariantCulture), revision.releaseAge.ToString(CultureInfo.InvariantCulture),
			Csv(revision.revisionKind), F(revision.normalizedResidual), F(revision.maturityWeight)
		}));
	}

	private void CloseCareerMemoryOutputs() {
		careerMemoryWriter?.Dispose();
		careerMemoryWriter = null;
	}
}
