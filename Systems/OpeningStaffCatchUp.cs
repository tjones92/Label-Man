using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// --opening-staff-catch-up=M (LpGenreAllocationFollowup.md §89): for the first M months of the run, a label's monthly
/// organization review hires each understaffed department's whole gap, not a quarter of it once a quarter. The cash test is
/// unchanged, and contraction still happens only at the quarterly reviews. A label opens as a company already staffed for the
/// business it runs in 1960. The opening sizing reads no distribution clients and caps projects at the label's release rate,
/// so a Major's coverage fell from 0.85 to 0.73 in 1960 while it hired 25% a quarter, and its planned single rate fell with it
/// (§89; other tiers stayed at 0.9 or above). After the window the ordinary quarterly 25% hiring applies. Off unless named.
/// </summary>
public static class OpeningStaffCatchUp {
	public const string Switch = "--opening-staff-catch-up=";
	private static bool configured;
	/// <summary>Months from the first period in which reviews hire the whole gap; 0 when off.</summary>
	public static int Months { get; private set; }
	private static int firstPeriod = int.MinValue;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(Switch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + Switch.TrimEnd('=') + " once.");
		Months = 0;
		if (values.Length == 1) {
			if (!int.TryParse(values[0].Substring(Switch.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) || m < 1 || m > 60)
				throw new ArgumentException(Switch.TrimEnd('=') + " needs a whole number of months in [1, 60].");
			Months = m;
			GD.Print($"OPENING_STAFF_CATCH_UP months={Months}");
		}
		firstPeriod = int.MinValue;
		configured = true;
	}

	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }
	internal static void SetForChecks(int months, int start) { configured = true; Months = months; firstPeriod = start; }

	/// <summary>Whether a review in <paramref name="period"/> is inside the window. The window starts at the run's opening
	/// period (the 1960 start date).</summary>
	public static bool Applies(int period) {
		EnsureConfigured();
		if (Months <= 0) return false;
		if (firstPeriod == int.MinValue) firstPeriod = LabelCorporateModel.Period(GameDate.StartDate);
		return period >= firstPeriod && period < firstPeriod + Months;
	}
}
