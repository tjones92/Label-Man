using System;
using Godot;

public static class LabelOperatorPayChecks {
	public static void Run() {
		float saved = LabelOperatorPay.MonthlyLimit;
		int checks = 0, events = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Operator pay: " + why); checks++; }
		void Observe(AILabel label, float surplus, float pay) { events++; }
		LabelOperatorPay.Paid += Observe;
		AILabel Firm(float cash = 5000f) => new() { labelId = "operator-pay-check", tier = LabelTier.Small, cashReserves = cash };
		try {
			var firm = Firm();
			LabelOperatorPay.SetForChecks(0f);
			Check(LabelOperatorPay.Pay(firm, 2000f) == 0f && firm.cashReserves == 5000f && events == 0, "off is exact");
			LabelOperatorPay.SetForChecks(400f);
			Check(LabelOperatorPay.Pay(firm, 2000f) == 400f && firm.cashReserves == 4600f && firm.monthlyExpenses == 400f && events == 1,
				"one payment reaches cash, expenses and telemetry once");
			Check(LabelOperatorPay.Pay(Firm(), -10f) == 0f, "loss-making months do not pay");
			Check(LabelOperatorPay.Pay(Firm(), 100f) == 0f, "the reinvestment surplus is retained before owner pay");
			Check(LabelOperatorPay.Pay(Firm(), 400f) == 50f, "at least half the surplus above reinvestment remains in the firm");
			Check(LabelOperatorPay.Pay(Firm(850f), 2000f) == 0f, "six months of ordinary overhead is protected");
			var payable = Firm(); payable.outstandingPlantPayables = 4000f;
			Check(LabelOperatorPay.Pay(payable, 2000f) == 100f && payable.cashReserves == 4900f, "plant bills are protected in addition to runway");
			var player = Firm(); player.isPlayerOwned = true;
			Check(LabelOperatorPay.Pay(player, 2000f) == 0f, "the player's personal cash is not charged another owner salary");
			GameRandom.Seed(547); float expected = GameRandom.Randf(); GameRandom.Seed(547);
			LabelOperatorPay.Pay(Firm(), 2000f);
			Check(GameRandom.Randf() == expected, "pay consumes no random draws");
			GD.Print($"LABEL_OPERATOR_PAY_CHECK passed checks={checks}");
		} finally { LabelOperatorPay.Paid -= Observe; LabelOperatorPay.SetForChecks(saved); }
	}
}
