using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Opt-in (SimTools/ReleaseVolumeCalibrationPlan.md): a growth-eligible label's hard roster capacity comes from what it
/// can fund, not from its tier (<see cref="LabelLifecycleManager.GetRosterCapacityForTier"/>: Major 50, MidTier 25).
/// At each quarterly review the capacity is the acts a share <see cref="EarningsShare"/> of the label's trailing
/// twelve months' pretax earnings would carry at its own recent direct cost per act (recording, campaigns, advances,
/// licensing). Until six months of statements have closed, nothing is earned and the ordinary rule applies.
/// When the existing earned-growth gates pass (profitable, runway, charting), the operating target moves to that
/// capacity instead of one slot; scouting still has to sign the acts. A capacity never drops below the acts held.
/// Which labels may grow is unchanged: launch Small, Boutique and Independent labels stay frozen. No firm or tier
/// number enters; the 1960 catalog counts are a benchmark only. A probe, not a sourced rate.
/// </summary>
public static class EarnedRosterCapacity {
	public const string Switch = "--earned-roster-capacity=";
	// A capacity bound for a pathological statement, far above any 1960 company's roster.
	internal const int MaximumActs = 250;
	// Below a token monthly spend the cost per act is not observed; no capacity is earned from it.
	internal const double MinimumAnnualCostPerAct = 1000;
	private static bool configured;
	public static float EarningsShare { get; private set; }
	public static bool Enabled => EarningsShare > 0;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string value = (arguments ?? Array.Empty<string>()).SingleOrDefault(a => a.StartsWith(Switch, StringComparison.Ordinal));
		if (value != null) {
			if (!float.TryParse(value[Switch.Length..], System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float share) || !float.IsFinite(share) || share < .05f || share > 1f)
				throw new ArgumentException(Switch + " requires a value between 0.05 and 1.");
			EarningsShare = share;
		}
		configured = true;
	}

	internal static void SetForChecks(float share) { configured = true; EarningsShare = share; }
	internal static void ResetForChecks() { configured = false; EarningsShare = 0; }

	// erc-v1 annualized January-February alone at the first review; capacities jumped from 70 to the bound between arms.
	internal const int MinimumObservedMonths = 6;

	/// <summary>Called as each monthly statement closes. A calendar year of at least six months becomes the prior year,
	/// annualized; a shorter first year (a label founded late) leaves none.</summary>
	internal static void Record(LabelCompanyState company, LabelCompanyStatement statement) {
		var history = company.earnings ??= new LabelEarningsHistory { year = LabelCorporateModel.Year(statement.period) };
		int year = LabelCorporateModel.Year(statement.period);
		if (history.year != year) {
			history.hasPriorYear = history.months >= MinimumObservedMonths;
			history.priorYear = history.hasPriorYear ? history.yearToDate * 12 / history.months : 0;
			(history.year, history.months, history.yearToDate) = (year, 0, 0);
		}
		history.yearToDate += statement.pretaxEarnings;
		history.months++;
	}

	/// <summary>Trailing-twelve-month pretax earnings: this year to date plus the rest of the prior year at its monthly
	/// rate. Without a prior year, the year to date annualized once six months have closed; before that, none.</summary>
	internal static double? TrailingEarnings(LabelEarningsHistory history) {
		if (history == null || history.months == 0 && !history.hasPriorYear) return null;
		if (history.hasPriorYear) return history.yearToDate + history.priorYear * Math.Max(0, 12 - history.months) / 12;
		return history.months >= MinimumObservedMonths ? history.yearToDate * 12 / history.months : null;
	}

	/// <summary>The earned hard capacity, or null while earnings are not yet observed (the ordinary one-slot growth
	/// and the existing capacity then apply).</summary>
	internal static int? Capacity(AILabel label) {
		int held = Math.Max(0, label.CurrentRosterSize);
		var company = label.company;
		double? earnings = TrailingEarnings(company?.earnings);
		if (earnings == null) return null;
		double costPerAct = company.recentProjectSpend * 12 / Math.Max(1, held);
		if (costPerAct < MinimumAnnualCostPerAct || earnings <= 0) return held;
		return Math.Clamp((int)Math.Floor(EarningsShare * earnings.Value / costPerAct), held, MaximumActs);
	}
}
