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
		string bound = (arguments ?? Array.Empty<string>()).SingleOrDefault(a => a.StartsWith(DeliveryBoundSwitch, StringComparison.Ordinal));
		if (bound != null) {
			if (!float.TryParse(bound[DeliveryBoundSwitch.Length..], System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float rate) || !float.IsFinite(rate) || rate < .5f || rate > 6f)
				throw new ArgumentException(DeliveryBoundSwitch + " requires a value between 0.5 and 6.");
			DeliveryReleasesPerAct = rate;
		}
		TierCeiling = (arguments ?? Array.Empty<string>()).Contains(TierCeilingSwitch);
		if ((DeliveryReleasesPerAct > 0f || TierCeiling) && !Enabled)
			throw new ArgumentException($"{DeliveryBoundSwitch} and {TierCeilingSwitch} bound {Switch}; they require it.");
		configured = true;
	}

	internal static void SetForChecks(float share, float deliveryReleasesPerAct = 0f, bool tierCeiling = false) {
		configured = true; EarningsShare = share; DeliveryReleasesPerAct = deliveryReleasesPerAct; TierCeiling = tierCeiling;
	}
	internal static void ResetForChecks() { configured = false; EarningsShare = 0; DeliveryReleasesPerAct = 0; TierCeiling = false; }

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
	internal static int? Capacity(AILabel label) => Capacity(label,
		DeliveryReleasesPerAct > 0f ? CompetitorManager.Instance?.GetRecentReleasedRecordCount(label.labelId, 52) ?? 0 : 0);

	internal static int? Capacity(AILabel label, int releasesLastYear) {
		int held = Math.Max(0, label.CurrentRosterSize);
		var company = label.company;
		double? earnings = TrailingEarnings(company?.earnings);
		if (earnings == null) return null;
		double costPerAct = company.recentProjectSpend * 12 / Math.Max(1, held);
		if (costPerAct < MinimumAnnualCostPerAct || earnings <= 0) return held;
		int earned = (int)Math.Floor(EarningsShare * earnings.Value / costPerAct);
		// Opt-in bounds (see DeliveryBoundSwitch, TierCeilingSwitch). Spend divided by acts held falls as idle acts pile up,
		// so the earnings rule alone ratchets to MaximumActs; these cap it by what the label issues and by firm size.
		if (DeliveryReleasesPerAct > 0f) earned = Math.Min(earned, DeliveryBound(releasesLastYear));
		if (TierCeiling) earned = Math.Min(earned, Ceiling(label.tier));
		return Math.Clamp(earned, held, MaximumActs);
	}

	/// <summary>
	/// --roster-delivery-bound=R: a label holds at most the acts its last 52 weeks of releases (any format) could serve at R
	/// records an act a year, with <see cref="DeliveryHeadroom"/> to grow into. 1960 measured: 2,987 signed AI acts issued
	/// 3,479 singles (1.16 each), falling to 0.51 by 1969 as rosters grew to 6,941 on a flat ~3,500 singles; Majors held
	/// ~200 acts on ~120 singles, MidTiers ~70 on 11-15. History's acts issued two or three singles a year.
	/// </summary>
	public const string DeliveryBoundSwitch = "--roster-delivery-bound=";
	public static float DeliveryReleasesPerAct { get; private set; }
	internal const float DeliveryHeadroom = 1.25f;
	internal static int DeliveryBound(int releasesLastYear) =>
		(int)Math.Floor(Math.Max(0, releasesLastYear) * DeliveryHeadroom / DeliveryReleasesPerAct);

	/// <summary>
	/// --roster-tier-ceiling: a permanent ceiling by tier, from 1960 issue counts at two to three singles an act
	/// (LabelReleaseProgrammeImplementation.md): Columbia ~360 singles (~150 acts), King and Dot ~130-150 (~60-75),
	/// Imperial and Liberty ~70-80 (~30-40). No documented 1960s roster headcount was found; these are estimates.
	/// </summary>
	public const string TierCeilingSwitch = "--roster-tier-ceiling";
	public static bool TierCeiling { get; private set; }
	internal static int Ceiling(LabelTier tier) => tier switch {
		LabelTier.Major => 150, LabelTier.MidTier => 75, LabelTier.Independent => 40, LabelTier.Boutique => 15, _ => 8
	};
}
