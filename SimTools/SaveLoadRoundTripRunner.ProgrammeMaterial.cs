using System;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class SaveLoadRoundTripRunner {
    private void RunProgrammeMaterialRoundTrip(int searchWeeks) {
        if (!LabelReleaseProgramme.MaterialSearchEnabled) throw new InvalidOperationException("Enable --anticipatory-material-search for this roundtrip.");
        var manager = CompetitorManager.Instance;
        ReleaseSlateItem item = null;
        for (int week = 0; week < searchWeeks; week++) {
            AdvanceOneChartWeek();
            item = manager.GetReleaseSlate().FirstOrDefault(i => i.MaterialSearch?.State == ProgrammeMaterialState.Pending);
            if (item != null) break;
        }
        if (item == null) throw new InvalidOperationException("No pending material work in the search window.");
        string slotId = item.Id, songId = item.MaterialSearch.SongId;
        float paid = item.MaterialSearch.PaidFee;
        var options = SaveGameService.TestJsonOptions;
        var snapshot = WorldStateService.Capture();
        string before = JsonSerializer.Serialize(snapshot, options);
        WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(before, options), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
        if (before != JsonSerializer.Serialize(WorldStateService.Capture(), options)) throw new InvalidOperationException("Material world roundtrip changed saved state.");
        string gzip = VerifyGzipFileIo(snapshot, options, out long bytes);
        if (gzip != "ok") throw new InvalidOperationException("Material gzip roundtrip: " + gzip);
        for (int week = 0; week < 12; week++) {
            AdvanceOneChartWeek();
            item = manager.GetReleaseSlate().Single(i => i.Id == slotId);
            if (item.MaterialSearch.SongId != songId || item.MaterialSearch.PaidFee != paid) throw new InvalidOperationException("Resumed work changed composition or fee.");
            LabelReleaseProgrammeChecks.CheckLiveState();
            if (item.MaterialSearch.State is ProgrammeMaterialState.Pending or ProgrammeMaterialState.Ready) continue;
            if (item.MaterialSearch.State == ProgrammeMaterialState.Consumed) {
                var record = item.PreparedRecord ?? ChartManager.Instance.GetRecordRuntimeData(item.RecordId)?.baseRecord;
                if (record?.songId != songId) throw new InvalidOperationException("Resumed session did not use its planned composition.");
            }
            GD.Print($"PROGRAMME_MATERIAL_ROUNDTRIP passed studio={StudioParentLabels.Enabled} slot={slotId} song={songId} paidFee={paid} outcome={item.MaterialSearch.State} gzipBytes={bytes}");
            GetTree().Quit(0); return;
        }
        throw new InvalidOperationException("Resumed material work did not resolve within twelve weeks.");
    }
}
