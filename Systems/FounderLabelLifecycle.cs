using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Opt-in voluntary retirement of an unfunded, inactive founder. No mortality draw.
/// Twelve quiet months and the reference session budget are scenario choices, not historical rates.</summary>
public static class FounderLabelLifecycle {
    public const string Switch = "--founder-label-dormancy";
    public const int QuietMonths = 12;
    public static bool Enabled { get; private set; }
    public static void Configure(IEnumerable<string> arguments) {
        var args = (arguments ?? Array.Empty<string>()).ToArray();
        Enabled = args.Contains(Switch);
        if (Enabled && (!LabelCorporateModel.HomeOperated || !EconomicLabelLifecycle.Enabled))
            throw new ArgumentException(Switch + " requires home-operated micro labels and economic exits.");
    }
    internal static void SetForChecks(bool enabled) => Enabled = enabled;

    public static void NoteRelease(AILabel label, GameDate date) {
        if (!Enabled || label == null || label.isPlayerOwned) return;
        label.founderLastReleasePeriod = LabelCorporateModel.Period(date);
        label.founderQuietMonths = 0;
    }

    internal static bool ReviewMonth(AILabel label, int closingPeriod, bool pendingProject) {
        if (!Enabled || !label.IsActive || !LabelCorporateModel.IsFounderRun(label, label.company)) return false;
        if (closingPeriod <= label.founderLastReviewPeriod) return false;
        label.founderLastReviewPeriod = closingPeriod;
        // New firms and old saves receive a full observation grace, not an invented release history.
        if (label.founderLastReleasePeriod < 0) label.founderLastReleasePeriod = closingPeriod;
        label.founderQuietMonths = Math.Max(0, closingPeriod - label.founderLastReleasePeriod);
        if (label.founderQuietMonths < QuietMonths || pendingProject || label.aiMasterLeases?.Count > 0) return false;
        if (label.aiBackerLoan != null && (label.aiBackerLoan.OutstandingPrincipal > .01f || label.aiBackerLoan.AccruedInterest > .01f)) return false;
        // Dormancy is a solvent decision. Unpaid obligations remain on the bankruptcy path.
        double liabilities = Math.Max(0, label.outstandingPlantPayables) + Math.Max(0, label.company.taxPayable);
        if (label.cashReserves < liabilities || label.RecentMonthlyCashFlow > 0f) return false;
        float available = label.cashReserves - EconomicLabelLifecycle.CommitmentReserve(label, signing: false);
        return available < label.GetProductionCost();
    }
}
