using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class SaveLoadRoundTripRunner {
    // Every assembled LP master must carry exactly its plan: the planned released masters as references, the shelved
    // masters plus at most the planned new sessions as other tracks, and a compilation only for a hits package.
    private static void CheckAssembledLp(ReleaseSlateItem item) {
        var plan = item.LpAssembly; var album = item.PreparedRecord?.album;
        if (album == null) throw new InvalidOperationException("Assembled LP has no prepared album: " + item.Id);
        if (!album.trackRefs.Select(t => t.sourceRecordId).SequenceEqual(plan.ReleasedMasterIds))
            throw new InvalidOperationException("Assembled LP references differ from its plan: " + item.Id);
        int others = album.nonSingleTracks.Length;
        if (others < plan.ShelvedMasterIds.Count || others > plan.ShelvedMasterIds.Count + plan.NewSessions)
            throw new InvalidOperationException("Assembled LP track count differs from its plan: " + item.Id);
        if ((album.albumFormat == AlbumFormat.Compilation) != (plan.Kind == LpProposalKind.GreatestHits))
            throw new InvalidOperationException("Only a greatest-hits package is a compilation: " + item.Id);
        if (CompetitorManager.Instance.ProgrammeState.Shelf?.Any(s => plan.ShelvedMasterIds.Contains(s.Record.recordId)) == true)
            throw new InvalidOperationException("A used shelved master stayed on the shelf: " + item.Id);
    }

    private void RunLpAssemblyRoundTrip(int searchWeeks) {
        if (!LabelReleaseProgramme.LpAssemblyEnabled) throw new InvalidOperationException("Enable --lp-assembly for this roundtrip.");
        var manager = CompetitorManager.Instance;
        var seen = new Dictionary<LpProposalKind, int>();
        int reused = 0;
        void Observe(ReleaseSlateItem item, AILabel label, int week) {
            CheckAssembledLp(item);
            seen[item.LpAssembly.Kind] = seen.GetValueOrDefault(item.LpAssembly.Kind) + 1;
            reused += item.LpAssembly.ReleasedMasterIds.Count + item.LpAssembly.ShelvedMasterIds.Count;
        }
        manager.OnLpAssembly += Observe;
        try {
            for (int week = 0; week < searchWeeks; week++) AdvanceOneChartWeek();
            if (seen.Count == 0) throw new InvalidOperationException("No LP was assembled in the search window.");
            var state = manager.ProgrammeState;
            if (state.MasterControl == null || state.MasterControl.Count == 0) throw new InvalidOperationException("No issued single entered the control ledger.");
            string ledger = JsonSerializer.Serialize(state.MasterControl, SaveGameService.TestJsonOptions);
            var options = SaveGameService.TestJsonOptions;
            var snapshot = WorldStateService.Capture();
            string before = JsonSerializer.Serialize(snapshot, options);
            WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(before, options), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
            if (before != JsonSerializer.Serialize(WorldStateService.Capture(), options)) throw new InvalidOperationException("LP assembly world roundtrip changed saved state.");
            if (ledger != JsonSerializer.Serialize(manager.ProgrammeState.MasterControl, options)) throw new InvalidOperationException("Ledger changed on load.");
            string gzip = VerifyGzipFileIo(snapshot, options, out long bytes);
            if (gzip != "ok") throw new InvalidOperationException("LP assembly gzip roundtrip: " + gzip);
            int beforeResume = seen.Values.Sum();
            for (int week = 0; week < 13; week++) { AdvanceOneChartWeek(); LabelReleaseProgrammeChecks.CheckLiveState(); }
            GD.Print($"LP_ASSEMBLY_ROUNDTRIP passed studio={StudioParentLabels.Enabled} " +
                string.Join(" ", seen.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}")) +
                $" reusedMasters={reused} resumedAssemblies={seen.Values.Sum() - beforeResume} ledger={manager.ProgrammeState.MasterControl.Count} " +
                $"shelf={manager.ProgrammeState.Shelf?.Count ?? 0} gzipBytes={bytes}");
            GetTree().Quit(0);
        } finally { manager.OnLpAssembly -= Observe; }
    }
}
