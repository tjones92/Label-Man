using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class LabelReleaseOwnershipChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool value, string reason) { if (!value) throw new InvalidOperationException("Programme ownership: " + reason); checks++; }
        bool Near(double a, double b) => Math.Abs(a - b) < .1;
        var manager = CompetitorManager.Instance;
        var previous = manager.ProgrammeState;
        var programme = (LabelReleaseProgramme.AllocationEnabled, LabelReleaseProgramme.CapacityEnabled);
        var corporate = (LabelCorporateModel.Ledger, LabelCorporateModel.Staff, LabelCorporateModel.Premises,
            LabelCorporateModel.Tax, LabelCorporateModel.Distributions, LabelCorporateModel.Capital,
            LabelCorporateModel.Commercial, LabelCorporateModel.RecordExcise);
        try {
            LabelReleaseProgramme.SetForChecks(true, true);
            LabelCorporateModel.SetForChecks(true, distributions: true, capital: true);
            var state = new ReleaseProgrammeState(); manager.SetProgrammeForChecks(state);
            AILabel Firm(string id, LabelTier tier) => new() { labelId = id, labelName = id, tier = tier,
                status = LabelStatus.Stable, cashReserves = 100000, homeCityId = "memphis" };
            var parent = Firm("programme-parent", LabelTier.Major);
            var child = Firm("programme-child", LabelTier.Independent);
            LabelCompanyAccounts.Open(parent); LabelCompanyAccounts.Open(child);
            child.activeDeal = new DistributionDeal { distributorId = parent.labelId, reachGranted = .5f };
            var artist = new SimulatedArtist { artistId = "subsidiary-act", labelId = child.labelId, contractSequence = 4 };
            child.roster.Add(artist);
            var ready = new ReleaseSlateItem { Id = "subsidiary-ready", LabelId = child.labelId, ArtistId = artist.artistId,
                ContractSequence = 4, State = ReleaseSlateState.Scheduled, IntendedWeek = 6, DueWeek = 6, PreparedWeek = 3,
                PreparedRecord = new Record { recordId = "subsidiary-master", labelId = child.labelId }, RecordId = "subsidiary-master",
                PaidProduction = 5000, ReservedMarketing = 200, ReservedPressing = 800 };
            var planned = new ReleaseSlateItem { Id = "subsidiary-planned", LabelId = child.labelId, State = ReleaseSlateState.InPreparation,
                ReservedProduction = 1000, ReservedMarketing = 1000, DueWeek = 10 };
            var delayed = new ReleaseSlateItem { Id = "subsidiary-delayed", LabelId = child.labelId, ArtistId = artist.artistId,
                Format = ReleaseFormat.Album, State = ReleaseSlateState.AwaitingDelivery, RecordId = "subsidiary-lp", PaidProduction = 7000 };
            state.Items.AddRange(new[] { ready, planned, delayed });
            state.PlannedThrough[child.labelId] = 12;
            state.Clocks[artist.artistId] = new ProgrammeClock { Identity = "subsidiary-act", SingleWeek = 1, AlbumWeek = -10 };
            state.LabelIdentities[child.labelId] = LabelReleaseProgramme.LabelIdentity(child);
            state.Campaigns.Add(new ProgrammeCampaign { LabelId = child.labelId, RecordId = "subsidiary-campaign", Week = 1, Spend = 100, Share = .2f });
            string slate = JsonSerializer.Serialize(state, SaveGameService.TestJsonOptions);
            float cash = child.cashReserves;
            double protectedWithoutHolds = LabelCorporateModel.ProtectedCash(child) - 3000;
            Check(Near(manager.ProgrammeCashReserved(child.labelId), 3000), "prepared and unprepared holds share the subsidiary's cash reserve");
            LabelReleaseProgramme.SetForChecks(false, false);
            Check(Near(LabelCorporateModel.ProtectedCash(child), protectedWithoutHolds), "switches off omit programme cash protection");
            LabelReleaseProgramme.SetForChecks(true, true);
            Check(Near(LabelCorporateModel.ProtectedCash(child), protectedWithoutHolds + 3000), "discretionary spending protects the holds exactly once");
            CompetitorManager.ApplySubsidiaryAbsorption(child, parent);
            Check(child.IsActive && child.IsSubsidiary && child.ownerLabelId == parent.labelId && child.tier == LabelTier.Independent,
                "absorption keeps the subsidiary operational at its own capacity tier");
            Check(child.roster.Single() == artist && artist.labelId == child.labelId && artist.contractSequence == 4,
                "absorption retains the roster's imprint and contract sequence");
            Check(child.cashReserves == cash && child.company.ownership == LabelOwnershipForm.CorporateDivision,
                "ownership changes without sweeping cash or paying for production again");
            Check(JsonSerializer.Serialize(state, SaveGameService.TestJsonOptions) == slate,
                "absorption leaves dates, masters, clocks, campaigns, identity and planning frontier intact");
            Check(LabelReleaseProgramme.LabelIdentity(child) == state.LabelIdentities[child.labelId], "corporate ownership does not reroll semantic scheduling identity");
            Check(manager.ProgrammeCashReserved(parent.labelId) == 0 && manager.ProgrammePromotionCommittedPeak(parent.labelId) == 0,
                "subsidiary holds and campaigns are not copied into the parent's resource book");
            var restored = JsonSerializer.Deserialize<ReleaseProgrammeState>(slate, SaveGameService.TestJsonOptions);
            Check(restored.Items.Single(i => i.Id == ready.Id).PreparedWeek == 3 && restored.Items.Single(i => i.Id == delayed.Id).State == ReleaseSlateState.AwaitingDelivery,
                "prepared sessions and delayed delivery retain their state through serialization");
            child.company.undistributedEarnings = 100000;
            child.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 100000 };
            child.recentMonthlyCashFlows.Add(10000);
            child.cashReserves = (float)LabelCorporateModel.ProtectedCash(child) + 500;
            float parentCash = parent.cashReserves;
            double total = child.cashReserves + parent.cashReserves;
            LabelCorporateModel.PayDistribution(child, 1960 * 12 + 2, id => id == parent.labelId ? parent : null);
            Check(Near(parent.cashReserves - parentCash, 500) && Near(child.cashReserves, LabelCorporateModel.ProtectedCash(child)),
                "the parent's dividend takes only cash above operating needs and release holds");
            Check(Near(total, child.cashReserves + parent.cashReserves) && ready.PaidProduction == 5000,
                "the payout conserves company-family cash and leaves paid production unchanged");
            float once = child.cashReserves;
            LabelCorporateModel.PayDistribution(child, 1960 * 12 + 2, id => parent);
            Check(child.cashReserves == once, "replaying a payout cannot spend the protected cash");
            manager.RescheduleProgrammeItem(ready, 7, ReleaseSlateState.Postponed, "ownership-check");
            manager.RescheduleProgrammeItem(planned, 11, ReleaseSlateState.Postponed, "ownership-check");
            LabelCorporateModel.PayDistribution(child, 1960 * 12 + 5, id => parent);
            Check(Near(parent.cashReserves - parentCash, 3500) && Near(child.cashReserves, LabelCorporateModel.ProtectedCash(child)),
                "released holds become available at a later payout without a fictitious cash refund");
            var equipment = Firm("programme-equipment", LabelTier.Independent); LabelCompanyAccounts.Open(equipment);
            equipment.productionQuality = .5f; equipment.recentMonthlyCashFlows.Add(10000);
            equipment.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 100000 };
            var equipmentHold = new ReleaseSlateItem { LabelId = equipment.labelId, State = ReleaseSlateState.InPreparation, ReservedProduction = 60000 };
            state.Items.Add(equipmentHold);
            equipment.cashReserves = (float)LabelCorporateModel.ProtectedCash(equipment);
            float equipmentCash = equipment.cashReserves;
            Check(!LabelCorporateModel.TryProductionUpgrade(equipment, 1964, 3) && equipment.cashReserves == equipmentCash,
                "equipment purchases cannot spend money reserved for the programme");
            equipmentHold.State = ReleaseSlateState.Cancelled;
            Check(LabelCorporateModel.TryProductionUpgrade(equipment, 1964, 6) && equipment.cashReserves < equipmentCash,
                "released programme cash can fund an otherwise eligible later capital purchase");
            GD.Print($"LABEL_RELEASE_OWNERSHIP_CHECK passed checks={checks} studio={StudioParentLabels.Enabled}");
        } finally {
            manager.SetProgrammeForChecks(previous);
            LabelReleaseProgramme.SetForChecks(programme.Item1, programme.Item2);
            LabelCorporateModel.SetForChecks(corporate.Item1, corporate.Item2, corporate.Item3, corporate.Item4, corporate.Item5, corporate.Item6, corporate.Item7, corporate.Item8);
        }
    }
}
