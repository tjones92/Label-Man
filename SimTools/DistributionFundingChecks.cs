using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class DistributionFundingChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool value, string reason) { if (!value) throw new InvalidOperationException("Distribution funding: " + reason); checks++; }
        bool Near(double a, double b) => Math.Abs(a - b) < .1;
        var manager = CompetitorManager.Instance;
        var previous = manager.ProgrammeState;
        var switches = (LabelReleaseProgramme.AllocationEnabled, LabelReleaseProgramme.CapacityEnabled, DistributionAdvanceRecoupment.Enabled);
        var live = manager.GetAllLabels().First(l => l.IsActive && l.tier == LabelTier.Major && !l.isPlayerOwned);
        float liveCash = live.cashReserves;
        string liveOwner = live.ownerLabelId;
        try {
            LabelReleaseProgramme.SetForChecks(true, true); DistributionAdvanceRecoupment.SetForChecks(true);
            var state = new ReleaseProgrammeState(); manager.SetProgrammeForChecks(state);
            AILabel Firm(string id, LabelTier tier = LabelTier.Independent) {
                var firm = new AILabel { labelId = id, labelName = id, tier = tier, status = LabelStatus.Stable,
                    cashReserves = 100000, homeCityId = "memphis" };
                LabelCompanyAccounts.Open(firm); return firm;
            }
            DistributionDeal Deal(AILabel owner, float advance) => new() { distributorId = owner.labelId, advance = advance,
                unrecoupedAdvance = advance, reachGranted = .5f, marginSkim = .2f, termWeeks = 78 };
            void SetCash(AILabel firm, float amount) => LabelCompanyAccounts.ChangeCash(firm, amount - firm.cashReserves, LabelCashKind.Financing);
            var client = Firm("funding-client"); var lender = Firm("funding-lender", LabelTier.Major);
			// Give the quoting fixture a real operating bill even under home-founder compensation.
			client.company.retainerQuote += 1000;
            float ordinary = manager.DistributionFundingReserve(lender);
            var hold = new ReleaseSlateItem { LabelId = lender.labelId, State = ReleaseSlateState.Scheduled,
                ReservedMarketing = 1000, ReservedPressing = 4000, PaidProduction = 7000 };
            state.Items.Add(hold);
            Check(Near(manager.DistributionFundingReserve(lender), ordinary + 5000), "unspent holds enter the funding reserve once; paid masters do not");
            Check(ordinary >= lender.GetMonthlyOverhead() * 3 && ordinary >= EconomicLabelLifecycle.CommitmentReserve(lender, true, includeProgramme: false),
                "the funding floor protects ordinary operations and existing tax/plant/loss reserves");
            float reserve = manager.DistributionFundingReserve(lender);
            SetCash(lender, reserve + 1250);
            var quote = manager.GenerateDealTerms(client, lender, DealOrigin.DistributorCourted, 1966, 20, buyout: 800);
            Check(Near(quote.advance, 450) && quote.unrecoupedAdvance == quote.advance && lender.cashReserves == reserve + 1250,
                "a quoted new advance leaves room for the incumbent's buyout without moving cash");
            float clientCash = client.cashReserves;
            Check(!manager.TryStartDistributionDeal(client, lender, Deal(lender, 1251)) && client.activeDeal == null && client.cashReserves == clientCash,
                "an unfunded new contract has no cash or contract side effects");
            var signed = Deal(lender, 1250);
            Check(manager.TryStartDistributionDeal(client, lender, signed) && Near(lender.cashReserves, reserve) && Near(client.cashReserves, clientCash + 1250),
                "an exactly funded advance transfers cash and leaves release commitments protected");
            Check(!manager.TryStartDistributionDeal(client, lender, signed) && Near(lender.cashReserves, reserve), "signing cannot pay the same advance twice");
            int period = LabelCorporateModel.Period(TimeManager.Instance.CurrentDate);
            Check(Near(LabelCompanyAccounts.CloseMonth(client, period).pretaxEarnings, 0) && Near(LabelCompanyAccounts.CloseMonth(lender, period).pretaxEarnings, 0),
                "new advance payments remain financing rather than operating income or expense");
            var incumbent = Firm("funding-incumbent"); var major = Firm("funding-buyer", LabelTier.Major);
            var target = Firm("funding-target"); var dropped = Firm("funding-dropped");
            var old = Deal(incumbent, 800); target.activeDeal = old;
            var dropDeal = Deal(major, 0); dropped.activeDeal = dropDeal;
            var offer = Deal(major, 500);
            var buyerHold = new ReleaseSlateItem { LabelId = major.labelId, State = ReleaseSlateState.InPreparation, ReservedProduction = 5000 };
            state.Items.Add(buyerHold);
            float buyerReserve = manager.DistributionFundingReserve(major);
            SetCash(major, buyerReserve + 1000);
            string Snapshot() => JsonSerializer.Serialize(new { target, incumbent, major, dropped, old, offer, state }, SaveGameService.TestJsonOptions);
            string before = Snapshot();
            Check(!manager.BuyOutDistributionAdvance(target, old, incumbent, offer, major) && Snapshot() == before,
                "a buyout cannot ignore the cash needed for its accompanying advance");
            Check(!manager.TryCommitDistributionPoach(target, old, incumbent, offer, major, dropped) && Snapshot() == before,
                "failed replacement preserves both contracts, the weakest client, every balance and all cash");
            SetCash(major, buyerReserve + 1300);
            float targetCash = target.cashReserves, incumbentCash = incumbent.cashReserves;
            double total = (double)major.cashReserves + target.cashReserves + incumbent.cashReserves;
            Check(manager.TryCommitDistributionPoach(target, old, incumbent, offer, major, dropped), "one budget can fund the full buyout plus new advance");
            Check(old.unrecoupedAdvance == 0 && offer.unrecoupedAdvance == 1300 && offer.advance == 500 && target.activeDeal == offer && dropped.activeDeal == null,
                "a funded replacement retires the old claim and carries it once on the new deal");
            Check(Near(major.cashReserves, buyerReserve) && Near(target.cashReserves, targetCash + 500) && Near(incumbent.cashReserves, incumbentCash + 800) &&
                Near(total, (double)major.cashReserves + target.cashReserves + incumbent.cashReserves), "the three-party financing transfer conserves cash");
            Check(manager.ProgrammeCashReserved(major.labelId) == 5000 && hold.PaidProduction == 7000, "deal financing cannot consume holds or charge paid production");
            before = Snapshot();
            Check(!manager.TryCommitDistributionPoach(target, old, incumbent, offer, major) && Snapshot() == before, "replacement replay cannot buy the same claim twice");
            var restored = JsonSerializer.Deserialize<DistributionDeal>(JsonSerializer.Serialize(offer, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
            Check(restored.advance == 500 && restored.unrecoupedAdvance == 1300, "the new cash advance stays distinct from the carried buyout on save/load");
            var exiting = Firm("funding-exit");
            state.Items.Add(new ReleaseSlateItem { LabelId = exiting.labelId, State = ReleaseSlateState.InPreparation, ReservedProduction = 5000 });
            float exitReserve = manager.DistributionFundingReserve(exiting);
            SetCash(exiting, exitReserve + 250);
            var exit = Deal(live, 1000); float beforeExit = live.cashReserves;
            manager.SettleEndedDistributionAdvance(exiting, exit, DealResolution.Exit);
            Check(Near(exiting.cashReserves, exitReserve) && Near(live.cashReserves, beforeExit + 250) && exit.unrecoupedAdvance == 0,
                "voluntary exit settles only spare cash and writes off the rest without consuming holds");
            LabelReleaseProgramme.SetForChecks(false, false);
            Check(Near(manager.DistributionFundingReserve(lender), lender.GetMonthlyOverhead() * 3), "switches off preserve the legacy funding floor");
            LabelReleaseProgramme.SetForChecks(true, true);
            // Exercise the actual desk acceptance path with an offer whose lender spent its free cash meanwhile.
            var desk = PlayerDesk.Instance;
            Check(desk.FoundLabel("Funding Desk Check", DistanceModel.GetCities().First().cityId, out _), "the player acceptance fixture has a live founder");
            var playerOffer = Deal(live, 500); desk.DebugSetPendingDistributionOffer(playerOffer);
            float liveReserve = manager.DistributionFundingReserve(live);
            SetCash(live, liveReserve + 100); float playerCash = desk.Label.cashReserves;
            Check(!desk.AcceptDistributionOffer(out _) && desk.PendingDistributionOffer == playerOffer && desk.Label.activeDeal == null && desk.Label.cashReserves == playerCash && live.cashReserves == liveReserve + 100,
                "stale player acceptance reports failure and retains the offer without creating cash or a contract");
            SetCash(live, liveReserve + 500);
            live.ownerLabelId = desk.Label.labelId;
            Check(!desk.AcceptDistributionOffer(out _) && desk.PendingDistributionOffer == playerOffer && desk.Label.activeDeal == null &&
                desk.Label.cashReserves == playerCash && live.cashReserves == liveReserve + 500,
                "a now-circular player offer stays pending without moving cash");
            live.ownerLabelId = liveOwner;
            desk.Label.ownerLabelId = live.labelId;
            Check(!desk.AcceptDistributionOffer(out _) && desk.PendingDistributionOffer == playerOffer && desk.Label.activeDeal == null &&
                desk.Label.cashReserves == playerCash && live.cashReserves == liveReserve + 500,
                "a newly absorbed player's stale offer stays pending without moving cash");
            desk.Label.ownerLabelId = null;
            Check(desk.AcceptDistributionOffer(out _) && desk.PendingDistributionOffer == null && desk.Label.activeDeal == playerOffer &&
                Near(live.cashReserves, liveReserve) && Near(desk.Label.cashReserves, playerCash + 500), "a funded player acceptance commits once and clears the desk offer");
            Check(!desk.AcceptDistributionOffer(out _), "a completed player acceptance cannot be replayed");
            GD.Print($"DISTRIBUTION_FUNDING_CHECK passed checks={checks} studio={StudioParentLabels.Enabled}");
        } finally {
            live.ownerLabelId = liveOwner;
            LabelCompanyAccounts.ChangeCash(live, liveCash - live.cashReserves, LabelCashKind.Financing); manager.SetProgrammeForChecks(previous);
            LabelReleaseProgramme.SetForChecks(switches.Item1, switches.Item2); DistributionAdvanceRecoupment.SetForChecks(switches.Item3);
        }
    }
}
