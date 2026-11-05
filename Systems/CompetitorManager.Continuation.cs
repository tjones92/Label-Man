using System;
using System.Linq;

public partial class CompetitorManager {
	internal ProjectContinuationSaveData CaptureProjectContinuation() => new() {
		Counter = generatedProjectCounter,
		Records = albumProjects.Concat(pendingAlbumProjects).GroupBy(p => p.projectId).ToDictionary(g => g.Key, g => {
			var p = g.First();
			bool IsLive(Record record) => record != null && ReferenceEquals(record, ChartManager.Instance.GetRecordRuntimeData(record.recordId)?.baseRecord);
			return new ProjectRecordLinksSaveData { AlbumIsLive = IsLive(p.albumRecord), PromoIsLive = IsLive(p.promoSingleRecord) };
		}, StringComparer.Ordinal)
	};
}
