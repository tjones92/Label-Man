using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class DistributionOwnershipChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool value, string reason) { if (!value) throw new InvalidOperationException("Distribution ownership: " + reason); checks++; }
        var manager = CompetitorManager.Instance;
        var previous = manager.ProgrammeState;
        var switches = (LabelReleaseProgramme.AllocationEnabled, LabelReleaseProgramme.CapacityEnabled);
        try {
            LabelReleaseProgramme.SetForChecks(true, true);
            var state = new ReleaseProgrammeState(); manager.SetProgrammeForChecks(state);
            AILabel Firm(string id, LabelTier tier = LabelTier.Independent) {
                var label = new AILabel { labelId = "ownership-" + id, labelName = id, tier = tier,
                    status = LabelStatus.Stable, cashReserves = 1000000, homeCityId = "memphis" };
                manager.RegisterLabel(label); return label;
            }
            DistributionDeal Deal(AILabel distributor) => new() { distributorId = distributor.labelId,
                advance = 500, unrecoupedAdvance = 500, reachGranted = .5f, termWeeks = 78 };
            var client = Firm("client"); var buyer = Firm("buyer", LabelTier.Major);
            var bridge = Firm("bridge"); var incumbent = Firm("incumbent"); var dropped = Firm("dropped");
            var labels = new[] { client, buyer, bridge, incumbent, dropped };
            state.Items.Add(new ReleaseSlateItem { LabelId = client.labelId, State = ReleaseSlateState.Scheduled,
                RecordId = "ownership-master", PreparedRecord = new Record { recordId = "ownership-master" },
                PaidProduction = 3000, ReservedMarketing = 1000, ReservedPressing = 4000 });
            bool Reaches() => CompetitorManager.DistributionChainReachesClient(client, buyer, manager.GetLabel);
            string Snapshot() => JsonSerializer.Serialize(new { labels, state, manager.DistributionOffersAccepted }, SaveGameService.TestJsonOptions);
            Check(!Reaches(), "unrelated companies have no dependency loop");
            buyer.ownerLabelId = client.labelId;
            Check(Reaches(), "a directly owned distributor cannot carry its parent");
            var offer = Deal(buyer); string before = Snapshot();
            Check(!manager.SignDistributionDeal(client, offer) && Snapshot() == before,
                "signing with one's subsidiary changes neither cash, contracts, programme nor acceptance count");
            buyer.ownerLabelId = bridge.labelId; bridge.ownerLabelId = client.labelId;
            Check(Reaches(), "ownership ancestry is followed beyond one level");
            before = Snapshot();
            Check(!manager.SignDistributionDeal(client, offer) && Snapshot() == before, "a nested ownership loop is rejected at signing");
            bridge.ownerLabelId = null; bridge.activeDeal = Deal(client);
            Check(Reaches(), "an ownership link followed by distribution can close a loop");
            buyer.ownerLabelId = null; buyer.activeDeal = Deal(bridge); bridge.activeDeal = null; bridge.ownerLabelId = client.labelId;
            Check(Reaches(), "a distribution link followed by ownership can close a loop");
            buyer.activeDeal = Deal(client); bridge.ownerLabelId = null;
            Check(Reaches(), "ordinary reverse distribution contracts remain circular");
            buyer.activeDeal = null; buyer.ownerLabelId = bridge.labelId;
            Check(!Reaches(), "a subsidiary distributor can still carry an unrelated independent");
            // Both branches reach the same parent: revisiting a firm is not itself a new cycle.
            buyer.activeDeal = Deal(bridge);
            Check(!Reaches(), "converging ownership and distribution paths do not reject an unrelated client");
            bridge.ownerLabelId = buyer.labelId;
            Check(!Reaches(), "a pre-existing unrelated cycle terminates without inventing a path to the client");
            bridge.ownerLabelId = null; buyer.activeDeal = null; buyer.ownerLabelId = client.labelId;
            var old = Deal(incumbent); client.activeDeal = old; dropped.activeDeal = Deal(buyer);
            before = Snapshot();
            Check(!manager.TryCommitDistributionPoach(client, old, incumbent, offer, buyer, dropped) && Snapshot() == before && offer.unrecoupedAdvance == 500,
                "a circular replacement preserves the incumbent claim, weakest client, new claim and every cash hold");
            client.activeDeal = Deal(buyer); before = Snapshot();
            Check(!manager.AbsorbLabel(client, buyer, .9f) && Snapshot() == before,
                "a subsidiary cannot absorb its own parent or discard its prepared master");
            buyer.ownerLabelId = bridge.labelId; bridge.ownerLabelId = client.labelId; before = Snapshot();
            Check(!manager.AbsorbLabel(client, buyer, .9f) && Snapshot() == before, "absorption also rejects indirect ownership cycles");
            client.activeDeal = null; bridge.ownerLabelId = null; client.ownerLabelId = incumbent.labelId; before = Snapshot();
            Check(!manager.SignDistributionDeal(client, offer) && Snapshot() == before, "a stale offer cannot sign a newly absorbed subsidiary");
            client.ownerLabelId = null; client.tier = LabelTier.MidTier; before = Snapshot();
            Check(!manager.SignDistributionDeal(client, offer) && Snapshot() == before, "a stale offer cannot sign a client that has outgrown distribution contracts");
            client.tier = LabelTier.Independent;
            Check(manager.SignDistributionDeal(client, offer) && client.activeDeal == offer,
                "a funded, unrelated subsidiary distributor remains usable");
            Check(Math.Abs(client.cashReserves - 1000500) < .1 && Math.Abs(buyer.cashReserves - 999500) < .1 &&
                manager.ProgrammeCashReserved(client.labelId) == 5000, "valid subsidiary financing conserves cash and leaves slate holds intact");
            var restored = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(buyer, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
            Check(restored.ownerLabelId == bridge.labelId, "the distributor's corporate parent survives serialization");
            client.activeDeal = null; buyer.ownerLabelId = client.labelId;
            LabelReleaseProgramme.SetForChecks(false, false);
            Check(manager.SignDistributionDeal(client, Deal(buyer)), "switches off retain the legacy signing policy");
            GD.Print($"DISTRIBUTION_OWNERSHIP_CHECK passed checks={checks} studio={StudioParentLabels.Enabled}");
        } finally {
            manager.SetProgrammeForChecks(previous);
            LabelReleaseProgramme.SetForChecks(switches.Item1, switches.Item2);
        }
    }
}
