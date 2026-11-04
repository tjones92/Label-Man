using System;
using System.IO;
using System.Linq;

// Read-only provenance, including in --calibration. No new simulation or save fields.
public partial class ChartAuditRunner {
	private StreamWriter writingLaunches, writingDemoTrace;
	private string writingAlbumPath;
	private string WritingOwner(string labelId) {
		var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
		while (!string.IsNullOrEmpty(labelId) && seen.Add(labelId)) {
			var label = ChartManager.Instance.GetLabelById(labelId);
			if (string.IsNullOrEmpty(label?.ownerLabelId)) break;
			labelId = label.ownerLabelId;
		}
		return labelId;
	}
	private void OpenWritingConcentrationOutputs(string directory) {
		if (!SimulationArguments.Get().Contains("--writing-concentration-audit")) return;
		File.WriteAllText(Path.Combine(directory, runName + "-writing-profile.json"), System.Text.Json.JsonSerializer.Serialize(new {
			ProfessionalWriting = ProfessionalWriting.Enabled, Market = ProfessionalWritingMarket.Enabled,
			PlannedAssignments = ProfessionalWritingMarket.PlannedAssignmentsEnabled, DeliveryOffers = ProfessionalWritingMarket.DeliveryOffersEnabled,
			Cowriting = BandLife.CowritingEnabled, TeamWriting = BandLife.TeamWritingEnabled,
			Selection = ReleaseJudgement.Select.ToString(), ProfessionalWritingMarket.LicenseHoldWeeks
		}, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
		writingLaunches = CreateWriter(Path.Combine(directory, runName + "-writing-launches.csv"));
		writingAlbumPath = Path.Combine(directory, runName + "-writing-album-tracks.csv");
		writingLaunches.WriteLine("week,recordId,songId,artistId,labelId,ownerGroup,tier,careerState,source,route,writerId,orderId,orderArtistId,compositionHook,compositionQuality,recordHook,production,runtimeQuality,recognition,priorTop40");
		writingDemoTrace = CreateWriter(Path.Combine(directory, runName + "-writing-demos.csv"));
		writingDemoTrace.WriteLine("week,songId,writerId,labelId,artistId,ownerGroup,tier,careerState,status,released,availableWeek,holdUntilWeek,priorityUntilWeek,feePaid,orderId,shortlistActive,shortlistRecorded,shortlistArchived,compositionHook,compositionQuality");
		CaptureWritingConcentrationWeek(0);
	}
	private void CaptureWritingLaunch(int week, RecordRuntimeData runtime) {
		if (writingLaunches == null) return;
		var r = runtime.baseRecord;
		var label = ChartManager.Instance.GetLabelById(r.labelId);
		var artist = ArtistManager.Instance.GetArtist(r.artistId);
		var song = CompositionCatalogService.GetSong(r.songId);
		var order = ProfessionalWritingMarket.State?.Orders.FirstOrDefault(o => o.SongId != null && o.SongId == r.songId);
		string route = r.isCover ? "Cover" : order != null ? "Commission" : song?.originKind == SongOriginKind.ProfessionalOffice ? "ProfessionalCatalogue" : "OtherOriginal";
		writingLaunches.WriteLine(string.Join(",", week, Csv(r.recordId), Csv(r.songId), Csv(r.artistId), Csv(r.labelId), Csv(WritingOwner(r.labelId)),
			Csv(label?.tier.ToString()), Csv(runtime.launchCareerState.ToString()), Csv(r.songSource.ToString()), route, Csv(ProfessionalWritingMarket.SupplierFor(song)),
			Csv(order?.Id), Csv(order?.ArtistId), DF(r.compositionHook), DF(r.compositionQuality), DF(r.hookStrength), DF(r.productionQuality), DF(runtime.GetQuality()),
			DF(artist?.publicRecognition ?? 0), artist?.top40Hits ?? 0));
	}
	private void CaptureWritingConcentrationWeek(int week) {
		if (writingDemoTrace == null || ProfessionalWritingMarket.State == null) return;
		var state = ProfessionalWritingMarket.State;
		// CaptureWorld only exposes the existing state reference; do not initialize or judge repertoires here.
		var view = new WorldSaveData();
		AiRepertoireService.CaptureWorld(view);
		var candidates = view.AiRepertoire?.Acts.Values.SelectMany(a => a.Candidates).Where(c => c.SongId != null).ToLookup(c => c.SongId);
		var orders = state.Orders.Where(o => o.SongId != null).ToLookup(o => o.SongId);
		foreach (var d in state.Demos.Values.OrderBy(d => d.SongId, StringComparer.Ordinal)) {
			var label = string.IsNullOrEmpty(d.LabelId) ? null : ChartManager.Instance.GetLabelById(d.LabelId);
			var artist = string.IsNullOrEmpty(d.ArtistId) ? null : ArtistManager.Instance.GetArtist(d.ArtistId);
			var song = CompositionCatalogService.GetSong(d.SongId);
			var cs = candidates?[d.SongId].Where(c => c.ArtistId == d.ArtistId).ToArray() ?? Array.Empty<ShadowCandidate>();
			writingDemoTrace.WriteLine(string.Join(",", week, Csv(d.SongId), Csv(d.WriterId), Csv(d.LabelId), Csv(d.ArtistId), Csv(WritingOwner(d.LabelId)),
				Csv(label?.tier.ToString()), Csv(artist?.careerState.ToString()), d.Status, d.Released, d.AvailableWeek, d.HoldUntilWeek, d.PriorityUntilWeek, DF(d.FeePaid),
				Csv(orders[d.SongId].FirstOrDefault()?.Id), cs.Count(c => c.Disposition == ShadowDisposition.Active),
				cs.Count(c => c.Disposition == ShadowDisposition.Recorded), cs.Count(c => c.Disposition == ShadowDisposition.Archived),
				DF(song?.commercialHook ?? 0), DF(song?.compositionQuality ?? 0)));
		}
	}
	private void CloseWritingConcentrationOutputs() {
		if (writingAlbumPath != null) {
			using var tracks = CreateWriter(writingAlbumPath);
			tracks.WriteLine("projectId,albumRecordId,artistId,labelId,tierAtSchedule,careerAtSchedule,scheduledWeek,dropWeek,terminalState,masterId,songId,source,writerId,orderId,hook");
			foreach (var p in CompetitorManager.Instance.GetAlbumProjects()) {
				if (p.albumRecord?.album == null) continue;
				foreach (var t in p.albumRecord.album.GetAllTracks()) {
					var song = CompositionCatalogService.GetSong(t.songId);
					var order = ProfessionalWritingMarket.State?.Orders.FirstOrDefault(o => o.SongId != null && o.SongId == t.songId);
					tracks.WriteLine(string.Join(",", Csv(p.projectId), Csv(p.albumRecord.recordId), Csv(p.artistId), Csv(p.currentLabelId), p.tierAtSchedule,
						p.careerStateAtSchedule, p.scheduledWeek, p.dropWeek, p.terminalState, Csv(t.masterId), Csv(t.songId), t.songSource,
						Csv(ProfessionalWritingMarket.SupplierFor(song)), Csv(order?.Id), DF(song?.commercialHook ?? 0)));
				}
			}
			writingAlbumPath = null;
		}
		writingLaunches?.Dispose(); writingLaunches = null;
		writingDemoTrace?.Dispose(); writingDemoTrace = null;
	}
}
