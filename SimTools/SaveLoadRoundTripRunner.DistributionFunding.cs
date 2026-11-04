using System;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class SaveLoadRoundTripRunner {
    private void RunDistributionFundingRoundTrip(int weeks) {
        if (!LabelReleaseProgramme.AllocationEnabled || !LabelReleaseProgramme.CapacityEnabled || !DistributionAdvanceRecoupment.Enabled)
            throw new InvalidOperationException("Funding roundtrip requires the full programme and advance recoupment.");
        for (int week = 0; week < weeks && !TimeManager.Instance.IsGameOver; week++) AdvanceOneChartWeek();
        var manager = CompetitorManager.Instance;
        var item = manager.GetReleaseSlate().FirstOrDefault(i => i.PreparedRecord != null && LabelReleaseProgramme.IsOpen(i) &&
            manager.GetLabel(i.LabelId) is { IsActive: true, IsSubsidiary: false, isPlayerOwned: false, activeDeal: null } label &&
            CompetitorManager.CanSignDistributionDeal(label.tier));
        if (item == null) throw new InvalidOperationException("No uncontracted operating client with a prepared master for the funding roundtrip.");
        var client = manager.GetLabel(item.LabelId);
        var incumbent = manager.GetAllLabels().First(l => l.IsActive && !l.isPlayerOwned && l != client && l.tier == LabelTier.MidTier && manager.AvailableDistributionFunding(l) >= 800);
        var major = manager.GetAllLabels().First(l => l.IsActive && !l.isPlayerOwned && l.tier == LabelTier.Major && manager.AvailableDistributionFunding(l) >= 1300);
        DistributionDeal Deal(AILabel distributor, float advance) => new() { distributorId = distributor.labelId, advance = advance,
            unrecoupedAdvance = advance, reachGranted = .5f, marginSkim = .2f, signedWeek = ChartManager.Instance.GetCurrentChartWeek(),
            termWeeks = 78, grantedRegions = distributor.distributionRegions ?? Array.Empty<string>() };
        var old = Deal(incumbent, 800);
        if (!manager.TryStartDistributionDeal(client, incumbent, old)) throw new InvalidOperationException("The staged incumbent advance could not be funded.");
        var offer = Deal(major, 500);
        string clientId = client.labelId, majorId = major.labelId, slotId = item.Id, masterId = item.RecordId;
        float paid = item.PaidProduction;
        string programmeBefore = JsonSerializer.Serialize(manager.ProgrammeState, SaveGameService.TestJsonOptions);
        double cashBefore = (double)client.cashReserves + major.cashReserves + incumbent.cashReserves;
        if (!manager.TryCommitDistributionPoach(client, old, incumbent, offer, major) || offer.advance != 500 || offer.unrecoupedAdvance != 1300 ||
            old.unrecoupedAdvance != 0 || Math.Abs(cashBefore - ((double)client.cashReserves + major.cashReserves + incumbent.cashReserves)) > .1 ||
            programmeBefore != JsonSerializer.Serialize(manager.ProgrammeState, SaveGameService.TestJsonOptions))
            throw new InvalidOperationException("The live buyout changed cash conservation, carried debt or the prepared programme.");
        WorldSaveData snapshot = WorldStateService.Capture();
        string before = JsonSerializer.Serialize(snapshot, SaveGameService.TestJsonOptions);
        WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(before, SaveGameService.TestJsonOptions),
            TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
        if (before != JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions))
            throw new InvalidOperationException("The distribution funding world roundtrip changed saved state.");
        string gzipStatus = VerifyGzipFileIo(snapshot, SaveGameService.TestJsonOptions, out long compressedBytes);
        if (gzipStatus != "ok") throw new InvalidOperationException("Distribution funding gzip roundtrip: " + gzipStatus);
        client = manager.GetLabel(clientId);
        item = manager.GetReleaseSlate().Single(i => i.Id == slotId);
        if (client.activeDeal?.distributorId != majorId || client.activeDeal.advance != 500 || client.activeDeal.unrecoupedAdvance != 1300 ||
            item.RecordId != masterId || item.PreparedRecord?.recordId != masterId || item.PaidProduction != paid)
            throw new InvalidOperationException("The carried loan or prepaid master was lost on load.");
        AdvanceOneChartWeek(); LabelReleaseProgrammeChecks.CheckLiveState();
        if (client.activeDeal?.distributorId != majorId || client.activeDeal.unrecoupedAdvance < 0 || client.activeDeal.unrecoupedAdvance > 1300)
            throw new InvalidOperationException("Resumed recoupment lost or increased the carried loan.");
        GD.Print($"DISTRIBUTION_FUNDING_ROUNDTRIP passed studio={StudioParentLabels.Enabled} weeks={weeks} client={clientId} major={majorId} newAdvance=500 carriedBalance=1300 prepared={masterId} gzipBytes={compressedBytes}");
        GetTree().Quit(0);
    }
}
