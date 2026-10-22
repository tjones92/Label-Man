using System;
using Godot;

/// <summary>--distress-master-sales: trigger, eligibility and evidence price. The sale itself runs through the existing
/// master-deal payment and transfer, checked by the round trip, cash reconciliation and the switch-off replay.</summary>
public static class DistressMasterSaleChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string why) { if (!value) throw new InvalidOperationException("Distress master sales: " + why); checks++; }
		bool Near(float a, float b) => Math.Abs(a - b) < .01f;
		AILabel Firm(LabelTier tier = LabelTier.Boutique, float cash = -2000f, int insolvent = 1) => new AILabel {
			labelId = "distress-check", tier = tier, status = LabelStatus.Dying, cashReserves = cash, insolventMonthEnds = insolvent };

		AiMasterDeals.SetEnabledForChecks(true);
		AiMasterDeals.SetDistressForChecks(false);
		Check(AiMasterDeals.DistressNeed(Firm(), true) == 0f, "off: no distress need is ever raised");
		AiMasterDeals.SetDistressForChecks(true);
		var due = Firm();
		Check(Near(AiMasterDeals.DistressNeed(due, true), 2000f + due.GetMonthlyOverhead()),
			"need is the overdraft plus one month of mandatory overhead");
		Check(AiMasterDeals.DistressNeed(Firm(insolvent: 0), true) == 0f, "a first negative month-end keeps its existing grace");
		Check(AiMasterDeals.DistressNeed(Firm(cash: 10f), true) == 0f, "a label back in cash needs no sale");
		Check(AiMasterDeals.DistressNeed(due, false) == 0f, "requires the economic label lifecycle");
		foreach (LabelTier tier in new[] { LabelTier.Small, LabelTier.Independent })
			Check(AiMasterDeals.DistressNeed(Firm(tier), true) > 0f, tier + " sellers are eligible");
		foreach (LabelTier tier in new[] { LabelTier.Major, LabelTier.MidTier })
			Check(AiMasterDeals.DistressNeed(Firm(tier), true) == 0f, tier + " firms are not distress sellers");
		var player = Firm(); player.isPlayerOwned = true;
		Check(AiMasterDeals.DistressNeed(player, true) == 0f, "the player's label is excluded");
		var subsidiary = Firm(); subsidiary.ownerLabelId = "parent";
		Check(AiMasterDeals.DistressNeed(subsidiary, true) == 0f, "subsidiaries are excluded");
		var closed = Firm(); closed.status = LabelStatus.Bankrupt;
		Check(AiMasterDeals.DistressNeed(closed, true) == 0f, "closed firms cannot sell");
		AiMasterDeals.SetEnabledForChecks(false);
		Check(AiMasterDeals.DistressNeed(Firm(), true) == 0f, "requires --ai-master-deals");
		AiMasterDeals.SetEnabledForChecks(true);

		RecordRuntimeData Live(int now, int previous, int total, float net, int sincePeak) => new RecordRuntimeData {
			unitsThisWeek = now, unitsPreviousWeek = previous, totalUnitsSold = total, lifetimeLabelNet = net, weeksSincePeakUnits = sincePeak };
		Check(AiMasterDeals.DistressValue(Live(0, 1000, 50000, 10000f, 4)) == 0f, "a record that has stopped selling is worth nothing");
		Check(AiMasterDeals.DistressValue(Live(1000, 1000, 50000, -500f, 4)) == 0f, "a loss-making record is worth nothing");
		Check(AiMasterDeals.DistressValue(null) == 0f, "no runtime, no value");
		// Falling 2,000 -> 1,000 a week: carry 0.5, one more week's units; $0.20 a unit; half to a forced seller.
		Check(Near(AiMasterDeals.DistressValue(Live(1000, 2000, 50000, 10000f, 3)), 100f), "a falling record is valued at its observed decay");
		// A climb is not extrapolated: valued at the 0.9 carry cap, nine weeks of current sales.
		Check(Near(AiMasterDeals.DistressValue(Live(1000, 500, 50000, 10000f, 0)), 900f), "a climbing record is valued as if at its peak");
		Check(Near(AiMasterDeals.DistressValue(Live(1000, 1000, 50000, 10000f, 2)), 900f), "a flat record is capped at the same carry");
		Check(AiMasterDeals.DistressValue(Live(1000, 2000, 50000, 10000f, 3)) < AiMasterDeals.DistressValue(Live(1000, 1100, 50000, 10000f, 3)),
			"a faster fade lowers the price");

		GameRandom.Seed(5521); float next = GameRandom.Randf(); GameRandom.Seed(5521);
		AiMasterDeals.DistressNeed(due, true); AiMasterDeals.DistressValue(Live(1000, 2000, 50000, 10000f, 3));
		Check(next == GameRandom.Randf(), "trigger and valuation consume no random draws");
		AiMasterDeals.SetDistressForChecks(false);
		AiMasterDeals.SetEnabledForChecks(false);
		GD.Print($"DISTRESS_MASTER_SALE_CHECK_PASSED checks={checks}");
	}
}
