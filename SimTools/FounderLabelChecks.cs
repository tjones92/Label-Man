using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public static class FounderLabelChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Founder label: " + why); checks++; }
        int january = 1960 * 12;
        AILabel Firm(float cash = 100f) => new() { labelId = "founder-check", labelName = "Founder Check",
            cashReserves = cash, tier = LabelTier.Small, homeCityId = "memphis",
            founderLastReleasePeriod = january, company = new LabelCompanyState { ownership = LabelOwnershipForm.OwnerOperated } };
        LabelCorporateModel.SetForChecks(true, staff: true, premises: true);
        LabelCorporateModel.SetHomeForChecks(true, 25f);
        FounderLabelLifecycle.SetForChecks(false);
        var off = Firm();
        Check(!FounderLabelLifecycle.ReviewMonth(off, january + 24, false) && off.founderLastReviewPeriod == -1, "off adds no lifecycle state");
        FounderLabelLifecycle.SetForChecks(true);
        var quiet = Firm();
        LabelCorporateModel.InitializeOpeningOrganization(quiet);
        Check(Math.Abs(quiet.GetMonthlyOverhead() - 25f) < .01 && quiet.company.leasedOfficeSuites == 0,
            "a home founder pays the disclosed running allowance without an office or standing retainers");
        Check(!FounderLabelLifecycle.ReviewMonth(quiet, january + 11, false), "eleven quiet months are not twelve");
        Check(FounderLabelLifecycle.ReviewMonth(quiet, january + 12, false), "an unfunded solvent founder can stop after a quiet year");
        Check(!FounderLabelLifecycle.ReviewMonth(quiet, january + 12, false), "monthly review is idempotent");
        Check(!FounderLabelLifecycle.ReviewMonth(Firm(10000), january + 12, false), "funded founders are not retired for weak charts");
        var catalog = Firm(); catalog.recentMonthlyCashFlows = new List<float> { 100f };
        Check(!FounderLabelLifecycle.ReviewMonth(catalog, january + 12, false), "profitable catalog operation is not dormancy");
        Check(!FounderLabelLifecycle.ReviewMonth(Firm(), january + 12, true), "a scheduled project protects the firm");
        var lessor = Firm(); lessor.aiMasterLeases = new List<AiMasterLease> { new() { RecordId = "leased" } };
        Check(!FounderLabelLifecycle.ReviewMonth(lessor, january + 12, false), "outstanding leases protect the operating licensor");
        var borrower = Firm(); borrower.aiBackerLoan = new AiBackerLoan { OutstandingPrincipal = 50f };
        Check(!FounderLabelLifecycle.ReviewMonth(borrower, january + 12, false), "dormancy cannot erase a backer loan");
        var unpaid = Firm(); unpaid.outstandingPlantPayables = 101f;
        Check(!FounderLabelLifecycle.ReviewMonth(unpaid, january + 12, false), "unfunded liabilities are not voluntary dormancy");
        Check(!FounderLabelLifecycle.ReviewMonth(Firm(-1), january + 12, false), "negative cash stays on the insolvency path");
        var staffed = Firm(); staffed.company.departments.Add(new() { employees = .25 });
        Check(!FounderLabelLifecycle.ReviewMonth(staffed, january + 12, false), "paid-staff firms are outside founder dormancy");
        var player = Firm(); player.isPlayerOwned = true;
        Check(!FounderLabelLifecycle.ReviewMonth(player, january + 12, false), "the player's exit remains a player decision");
        var subsidiary = Firm(); subsidiary.ownerLabelId = "parent";
        Check(!FounderLabelLifecycle.ReviewMonth(subsidiary, january + 12, false), "subsidiary closures belong to their parent");
        FounderLabelLifecycle.NoteRelease(quiet, new GameDate(1961, 1, 1));
        Check(quiet.founderQuietMonths == 0 && !FounderLabelLifecycle.ReviewMonth(quiet, january + 13, false), "genuine release activity resets the clock");
        var old = JsonSerializer.Deserialize<AILabel>("{\"cashReserves\":100,\"tier\":3}", SaveGameService.TestJsonOptions);
        old.company = new LabelCompanyState { ownership = LabelOwnershipForm.OwnerOperated };
        Check(!FounderLabelLifecycle.ReviewMonth(old, january + 60, false) && old.founderQuietMonths == 0,
            "an old save gets a full observation grace");
        quiet.exitReason = LabelExitReason.Dormancy;
        var restored = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(quiet, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
        Check(restored.founderLastReleasePeriod == quiet.founderLastReleasePeriod
            && restored.founderLastReviewPeriod == quiet.founderLastReviewPeriod && restored.exitReason == LabelExitReason.Dormancy,
            "full-world saves preserve clock and exit cause");
        var legacy = Firm(); LabelSaveData.From(quiet).ApplyTo(legacy);
        Check(legacy.founderLastReleasePeriod == quiet.founderLastReleasePeriod && legacy.exitReason == LabelExitReason.Dormancy,
            "legacy label saves preserve clock and cause too");
        GameRandom.Seed(4821); float next = GameRandom.Randf(); GameRandom.Seed(4821);
        FounderLabelLifecycle.ReviewMonth(Firm(), january + 12, false);
        Check(GameRandom.Randf() == next, "dormancy adds no random draw");
        GD.Print($"FOUNDER_LABEL_CHECK_PASSED checks={checks}");
    }
}
