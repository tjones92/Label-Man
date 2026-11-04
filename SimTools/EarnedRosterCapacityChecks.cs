using System;
using System.Linq;
using Godot;

/// <summary>--earned-roster-capacity-check: argument validation; trailing earnings by calendar year; nothing is earned
/// before six closed months; capacity is the earnings share over the label's own cost per act, never below the acts held
/// and never above the bound; losses or no observed spend earn nothing.</summary>
public static class EarnedRosterCapacityChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Earned roster capacity: " + message); checks++; }
		bool Rejects(string arg) {
			try { EarnedRosterCapacity.ResetForChecks(); EarnedRosterCapacity.Configure(new[] { arg }); return false; }
			catch (ArgumentException) { return true; }
		}
		float saved = EarnedRosterCapacity.EarningsShare;
		var label = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned && l.IsActive && l.CurrentRosterSize > 2);
		var savedCompany = label.company;
		try {
			Check(Rejects(EarnedRosterCapacity.Switch + "0.01"), "rejects a share below .05");
			Check(Rejects(EarnedRosterCapacity.Switch + "2"), "rejects a share above 1");
			Check(!Rejects(EarnedRosterCapacity.Switch + "0.5") && EarnedRosterCapacity.EarningsShare == .5f, "parses the share");

			int held = label.CurrentRosterSize;
			var company = new LabelCompanyState { recentProjectSpend = held * 2000 / 12.0 };
			label.company = company;
			void Close(int year, int month, double pretax) =>
				EarnedRosterCapacity.Record(company, new LabelCompanyStatement { period = year * 12 + month - 1, pretaxEarnings = pretax });
			Check(EarnedRosterCapacity.Capacity(label) == null, "no statements earn nothing");
			for (int month = 1; month <= 5; month++) Close(1960, month, 10_000);
			Check(EarnedRosterCapacity.Capacity(label) == null, "five months are not yet observed");
			Close(1960, 6, 10_000);
			// $120k a year, half of it, over $2k an act.
			Check(EarnedRosterCapacity.Capacity(label) == Math.Max(held, 30), "six months annualize: share x earnings / cost per act");
			for (int month = 7; month <= 12; month++) Close(1960, month, 10_000);
			Close(1961, 1, 40_000);
			var history = company.earnings;
			Check(history.hasPriorYear && Math.Abs(history.priorYear - 120_000) < 1e-6 && history.months == 1, "a closed year becomes the prior year");
			// Trailing: 40k this year plus eleven months of the prior year at 10k = 150k; half over 2k = 37.
			Check(Math.Abs(EarnedRosterCapacity.TrailingEarnings(history).Value - 150_000) < 1e-6, "trailing twelve months");
			Check(EarnedRosterCapacity.Capacity(label) == Math.Max(held, 37), "capacity uses the trailing year");
			Close(1961, 2, -1_000_000);
			Check(EarnedRosterCapacity.Capacity(label) == held, "losses earn no capacity");
			Close(1961, 3, 100_000_000);
			Check(EarnedRosterCapacity.Capacity(label) == EarnedRosterCapacity.MaximumActs, "the bound holds");
			company.recentProjectSpend = 0;
			Check(EarnedRosterCapacity.Capacity(label) == held, "unobserved spend earns nothing");
			var late = new LabelCompanyState();
			for (int month = 9; month <= 12; month++) EarnedRosterCapacity.Record(late, new LabelCompanyStatement { period = 1960 * 12 + month - 1, pretaxEarnings = 1 });
			EarnedRosterCapacity.Record(late, new LabelCompanyStatement { period = 1961 * 12, pretaxEarnings = 1 });
			Check(!late.earnings.hasPriorYear && EarnedRosterCapacity.TrailingEarnings(late.earnings) == null, "a four-month first year is no prior year");
			label.company = null;
			Check(EarnedRosterCapacity.Capacity(label) == null, "no accounts earn nothing");
		} finally {
			label.company = savedCompany;
			EarnedRosterCapacity.SetForChecks(saved);
		}
		GD.Print($"EARNED_ROSTER_CAPACITY_CHECK passed checks={checks}");
	}
}
