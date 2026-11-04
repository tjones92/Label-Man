using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Opt-in, profit-funded compensation for the AI firm's owner-manager.
/// A calibration assumption, not a period wage quote. Unit prices and contribution are unchanged.</summary>
public static class LabelOperatorPay {
	public static float MonthlyLimit { get; private set; }
	private static bool configured;
	public static event Action<AILabel, float, float> Paid;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		MonthlyLimit = LabelPressingEconomics.ReadParameter((arguments ?? Array.Empty<string>()).ToArray(),
			"--label-operator-monthly-pay=", 0f, 0f, 2000f);
		configured = true;
	}
	internal static void SetForChecks(float value) => MonthlyLimit = value;
	internal static float Pay(AILabel label, float operatingCashSurplus) {
		if (MonthlyLimit <= 0f || label == null || label.isPlayerOwned || operatingCashSurplus <= 0f) return 0f;
		float overhead = label.GetMonthlyOverhead();
		float protectedCash = 6f * overhead + Math.Max(0f, label.outstandingPlantPayables);
		// Retain the operating surplus the existing self-built distribution policy
		// needs before paying the owner. Marginal firms retain their reinvestment base.
		float distributableSurplus = Math.Max(0f, operatingCashSurplus - 2f * overhead);
		float pay = Math.Min(MonthlyLimit, Math.Min(distributableSurplus * .5f, Math.Max(0f, label.cashReserves - protectedCash)));
		if (pay <= 0f) return 0f;
		label.cashReserves -= pay;
		label.monthlyExpenses += pay;
		Paid?.Invoke(label, operatingCashSurplus, pay);
		return pay;
	}
}
