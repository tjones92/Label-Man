using System;
using System.Collections.Generic;

public partial class CompetitorManager {
    // A distributor may itself borrow distribution or belong to another company. Both
    // links point toward the firms it depends on; a new link must not lead back to its client.
    internal static bool DistributionChainReachesClient(AILabel client, AILabel distributor, Func<string, AILabel> findLabel) {
        if (client == null || distributor == null) return false;
        var pending = new Stack<AILabel>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        pending.Push(distributor);
        while (pending.Count > 0) {
            var current = pending.Pop();
            if (current == client || current.labelId == client.labelId) return true;
            if (!visited.Add(current.labelId)) continue;
            foreach (string id in new[] { current.ownerLabelId, current.activeDeal?.distributorId }) {
                if (string.IsNullOrEmpty(id)) continue;
                if (id == client.labelId) return true;
                var next = findLabel(id);
                if (next != null) pending.Push(next);
            }
        }
        return false;
    }

    internal float DistributionFundingReserve(AILabel label) {
        float ordinary = label.GetMonthlyOverhead() * DistributionAdvanceRecoupment.ExitReserveMonths;
        if (!LabelReleaseProgramme.Enabled || label.isPlayerOwned) return ordinary;
        // Preserve the ordinary distribution buffer and protect tax, plant, losses and dated work.
        // Unspent slate holds are added once; receivables and paid masters are not available cash.
        return Math.Max(ordinary, EconomicLabelLifecycle.CommitmentReserve(label, signing: true, includeProgramme: false))
            + ProgrammeCashReserved(label.labelId);
    }

    internal float AvailableDistributionFunding(AILabel label, float buyout = 0) =>
        Math.Max(0, label.cashReserves - DistributionFundingReserve(label) - buyout);

    private static float DistributionBuyout(DistributionDeal current) =>
        DistributionAdvanceRecoupment.Enabled ? Math.Max(0, current?.unrecoupedAdvance ?? 0) : 0;

    private bool CanFundDistributionDeal(AILabel distributor, float advance, float buyout = 0) =>
        !LabelReleaseProgramme.Enabled || (distributor?.IsActive == true &&
            float.IsFinite(advance) && advance >= 0 && float.IsFinite(buyout) && buyout >= 0 &&
            (double)advance + buyout <= (double)distributor.cashReserves - DistributionFundingReserve(distributor));

    internal bool TryStartDistributionDeal(AILabel client, AILabel distributor, DistributionDeal offer) {
        if (client == null || offer == null || client.activeDeal != null) return false;
        if (LabelReleaseProgramme.Enabled && (!client.IsActive || client.IsSubsidiary || !CanSignDistributionDeal(client.tier) ||
            StudioParentLabels.IsStudioLabel(client) || client == distributor || distributor?.labelId != offer.distributorId ||
            WouldCreateCircularDeal(client, distributor) ||
            !CanFundDistributionDeal(distributor, offer.advance))) return false;
        client.activeDeal = offer;
        LabelCompanyAccounts.ChangeCash(client, offer.advance, LabelCashKind.Financing);
        if (distributor != null) LabelCompanyAccounts.ChangeCash(distributor, -offer.advance, LabelCashKind.Financing);
        return true;
    }

    internal bool TryCommitDistributionPoach(AILabel client, DistributionDeal current, AILabel incumbent,
        DistributionDeal offer, AILabel major, AILabel dropped = null) {
        if (LabelReleaseProgramme.Enabled && (client == null || current == null || offer == null || incumbent == null ||
            client.activeDeal != current || current.distributorId != incumbent.labelId || offer.distributorId != major?.labelId ||
            !client.IsActive || client.IsSubsidiary || !CanSignDistributionDeal(client.tier) || StudioParentLabels.IsStudioLabel(client) ||
            !incumbent.IsActive || client == major || incumbent == major || WouldCreateCircularDeal(client, major) ||
            (dropped != null && (dropped == client || dropped.IsSubsidiary || dropped.activeDeal?.distributorId != major?.labelId)) ||
            !CanFundDistributionDeal(major, offer.advance, DistributionBuyout(current)))) return false;
        // Validate the whole payment before dropping anybody or changing the old balance. The following
        // settlement has no conditional funding steps: an unfunded offer cannot partly replace a deal.
        DistributionOffersAccepted++;
        if (dropped != null) {
            DistributionDeal endedDeal = dropped.activeDeal;
            dropped.ownedReach = Godot.Mathf.Min(1f, dropped.ownedReach + endedDeal.reachGranted * .50f);
            SettleEndedDistributionAdvance(dropped, endedDeal, DealResolution.Dropped);
            EmitDealEvent(dropped, major, endedDeal, DealResolution.Dropped, dropped.DistributionDependency);
            dropped.activeDeal = null;
        }
        PayDistributionBuyout(client, current, incumbent, offer, major);
        EmitDealEvent(client, incumbent, current, DealResolution.Poached, client.DistributionDependency);
        client.activeDeal = offer;
        LabelCompanyAccounts.ChangeCash(client, offer.advance, LabelCashKind.Financing);
        LabelCompanyAccounts.ChangeCash(major, -offer.advance, LabelCashKind.Financing);
        EmitDealEvent(client, major, offer, DealResolution.Signed, client.DistributionDependency);
        return true;
    }
}
