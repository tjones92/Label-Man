using System;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class SaveLoadRoundTripRunner {
	private void RunProgrammeProjectRoundTrip(int searchWeeks) {
		if (!LabelReleaseProgramme.ProjectJudgementEnabled) throw new InvalidOperationException("Enable --label-project-judgement.");
		var manager = CompetitorManager.Instance;
		ReleaseSlateItem item = null;
		for (int week = 0; week < searchWeeks; week++) {
			AdvanceOneChartWeek();
			item = manager.GetReleaseSlate().FirstOrDefault(i => i.Format == ReleaseFormat.Single && i.PreparedRecord != null && LabelReleaseProgramme.IsOpen(i));
			if (item != null) break;
		}
		if (item == null) throw new InvalidOperationException("No prepared single found in the search window.");
		string slot = item.Id, recordId = item.RecordId, song = item.PreparedRecord.songId;
		float paid = item.PaidProduction;
		var snapshot = WorldStateService.Capture();
		if (snapshot.AiRepertoire.PreparedPicks?.ContainsKey(recordId) != true) throw new InvalidOperationException("Prepared single lost its pick before save.");
		string candidate = snapshot.AiRepertoire.PreparedPicks[recordId].CandidateId;
		var options = SaveGameService.TestJsonOptions;
		string before = JsonSerializer.Serialize(snapshot, options);
		WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(before, options), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
		if (before != JsonSerializer.Serialize(WorldStateService.Capture(), options)) throw new InvalidOperationException("Project world changed during roundtrip.");
		if (VerifyGzipFileIo(snapshot, options, out long bytes) != "ok") throw new InvalidOperationException("Project gzip roundtrip failed.");
		bool observed = false;
		void Observe(ShadowJudgement j) {
			if (j.RecordId != recordId) return;
			if (j.SelectedId != candidate) throw new InvalidOperationException("Restored issue consumed a different song.");
			observed = true;
		}
		AiRepertoireService.OnJudgement += Observe;
		try {
			for (int week = 0; week < 12; week++) {
				AdvanceOneChartWeek(); LabelReleaseProgrammeChecks.CheckLiveState();
				item = manager.GetReleaseSlate().Single(i => i.Id == slot);
				if (item.RecordId != recordId || item.PaidProduction != paid) throw new InvalidOperationException("Restored project reran its paid session.");
				if (LabelReleaseProgramme.IsOpen(item)) continue;
				if (item.State == ReleaseSlateState.Issued && (!observed || ChartManager.Instance.GetRecordRuntimeData(recordId)?.baseRecord.songId != song))
					throw new InvalidOperationException("Restored project did not issue its saved master and pick.");
				if (WorldStateService.Capture().AiRepertoire.PreparedPicks?.ContainsKey(recordId) == true) throw new InvalidOperationException("Resolved project retained a pending pick.");
				GD.Print($"PROGRAMME_PROJECT_ROUNDTRIP passed slot={slot} outcome={item.State} paid={paid} gzipBytes={bytes}");
				GetTree().Quit(0); return;
			}
			throw new InvalidOperationException("Restored prepared project did not resolve in twelve weeks.");
		} finally { AiRepertoireService.OnJudgement -= Observe; }
	}
}
