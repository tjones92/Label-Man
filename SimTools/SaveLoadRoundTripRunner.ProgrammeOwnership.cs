using System;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class SaveLoadRoundTripRunner {
    private void RunProgrammeOwnershipRoundTrip(int searchWeeks) {
        if (!LabelReleaseProgramme.AllocationEnabled || !LabelReleaseProgramme.CapacityEnabled)
            throw new InvalidOperationException("Ownership roundtrip requires the full programme.");
        var manager = CompetitorManager.Instance;
        AILabel client = null;
        ReleaseSlateItem delivery = null;
        for (int week = 0; week < searchWeeks && !TimeManager.Instance.IsGameOver; week++) {
            AdvanceOneChartWeek();
            delivery = manager.GetReleaseSlate().FirstOrDefault(i => i.State == ReleaseSlateState.AwaitingDelivery &&
                manager.GetLabel(i.LabelId) is { IsActive: true, IsSubsidiary: false, isPlayerOwned: false } label && label.tier != LabelTier.Major &&
                (label.activeDeal == null || manager.GetLabel(label.activeDeal.distributorId)?.tier == LabelTier.Major) &&
                label.cashReserves > EconomicLabelLifecycle.CommitmentReserve(label, false) + label.GetStandardMarketingBudget() &&
                manager.GetReleaseSlate().Any(p => p.LabelId == i.LabelId && LabelReleaseProgramme.IsOpen(p) && p.PreparedRecord != null));
            if (delivery != null) { client = manager.GetLabel(delivery.LabelId); break; }
        }
        if (client == null) throw new InvalidOperationException("No funded independent with a pending LP and prepared slate master in the search window.");
        var parent = client.activeDeal == null ? manager.GetAllLabels().First(l => l.IsActive && l.tier == LabelTier.Major && !l.isPlayerOwned)
            : manager.GetLabel(client.activeDeal.distributorId);
        // Supply only a missing distribution contract. Existing balances go through the real absorption settlement.
        client.activeDeal ??= new DistributionDeal { distributorId = parent.labelId, reachGranted = .5f, termWeeks = 52 };
        string clientId = client.labelId, parentId = parent.labelId, lpId = delivery.RecordId;
        var prepared = manager.GetReleaseSlate().First(i => i.LabelId == clientId && LabelReleaseProgramme.IsOpen(i) && i.PreparedRecord != null);
        string preparedId = prepared.Id, preparedRecordId = prepared.RecordId;
        float paidProduction = prepared.PaidProduction;
        var options = SaveGameService.TestJsonOptions;
        string programmeBefore = JsonSerializer.Serialize(manager.ProgrammeState, options);
        string projectsBefore = JsonSerializer.Serialize(manager.GetAlbumProjects(), options);
        string rosterBefore = string.Join("|", client.roster.Select(a => $"{a.artistId}:{a.labelId}:{a.contractSequence}"));
        var capacityBefore = LabelReleaseProgramme.MonthlyCapacity(client);
        float childHolds = manager.ProgrammeCashReserved(clientId), parentHolds = manager.ProgrammeCashReserved(parentId);
        float totalCash = client.cashReserves + parent.cashReserves;
        if (!manager.AbsorbLabel(client, parent, .9f)) throw new InvalidOperationException("The live absorption failed.");
        if (client.ownerLabelId != parentId || !client.IsActive || client.activeDeal != null ||
            client.company.ownership != LabelOwnershipForm.CorporateDivision ||
            rosterBefore != string.Join("|", client.roster.Select(a => $"{a.artistId}:{a.labelId}:{a.contractSequence}")) ||
            capacityBefore != LabelReleaseProgramme.MonthlyCapacity(client) || Math.Abs(totalCash - client.cashReserves - parent.cashReserves) > .1f ||
            childHolds != manager.ProgrammeCashReserved(clientId) || parentHolds != manager.ProgrammeCashReserved(parentId) ||
            programmeBefore != JsonSerializer.Serialize(manager.ProgrammeState, options) || projectsBefore != JsonSerializer.Serialize(manager.GetAlbumProjects(), options))
            throw new InvalidOperationException("Live absorption changed the subsidiary's roster, projects, programme, cash or capacity.");
        int absorptionWeek = ChartManager.Instance.GetCurrentChartWeek();
        WorldSaveData snapshot = WorldStateService.Capture();
        string before = JsonSerializer.Serialize(snapshot, options);
        WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(before, options), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
        if (before != JsonSerializer.Serialize(WorldStateService.Capture(), options))
            throw new InvalidOperationException("Acquired subsidiary world roundtrip changed saved state.");
        string gzipStatus = VerifyGzipFileIo(snapshot, options, out long compressedBytes);
        if (gzipStatus != "ok") throw new InvalidOperationException("Acquired subsidiary gzip roundtrip: " + gzipStatus);
        client = manager.GetLabel(clientId); parent = manager.GetLabel(parentId);
        prepared = manager.GetReleaseSlate().Single(i => i.Id == preparedId);
        if (client.ownerLabelId != parentId || prepared.RecordId != preparedRecordId || prepared.PreparedRecord?.recordId != preparedRecordId ||
            prepared.PaidProduction != paidProduction || manager.GetReleaseSlate().Single(i => i.RecordId == lpId).State != ReleaseSlateState.AwaitingDelivery)
            throw new InvalidOperationException("Ownership, prepared master or pending delivery was lost on load.");
        LabelReleaseProgrammeChecks.CheckLiveState();
        for (int week = 0; week < 12; week++) {
            AdvanceOneChartWeek();
            LabelReleaseProgrammeChecks.CheckLiveState();
            foreach (var label in new[] { client, parent }) {
                float pool = label.GetStandardMarketingBudget() * Math.Max(1f, Math.Min(4f, label.releasesPerMonth));
                if (manager.ProgrammePromotionCommittedPeak(label.labelId) > pool + .1f)
                    throw new InvalidOperationException("Parent or subsidiary promotion was overbooked after absorption.");
            }
            var lp = manager.GetReleaseSlate().Single(i => i.RecordId == lpId);
            if (lp.State == ReleaseSlateState.Cancelled) throw new InvalidOperationException("The acquired subsidiary's pending LP was cancelled instead of delivered.");
            if (lp.State != ReleaseSlateState.Issued) continue;
            var record = ChartManager.Instance.GetRecordRuntimeData(lpId);
            if (record?.baseRecord.labelId != clientId || client.ownerLabelId != parentId || !client.IsActive)
                throw new InvalidOperationException("Delivered LP lost its operating imprint or corporate owner.");
            GD.Print($"LABEL_RELEASE_OWNERSHIP_ROUNDTRIP passed studio={StudioParentLabels.Enabled} absorptionWeek={absorptionWeek} deliveryWeek={lp.ActualWeek} client={clientId} parent={parentId} prepared={preparedRecordId} lp={lpId} gzipBytes={compressedBytes}");
            GetTree().Quit(0); return;
        }
        throw new InvalidOperationException("The acquired subsidiary's pending LP did not deliver within twelve resumed weeks.");
    }
}
