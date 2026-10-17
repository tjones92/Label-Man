using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Default slower wholesale-house spread (SimTools/SmallLabelAttritionAndChartMethod.md §10).
/// --no-slow-wholesale-spread retains the old comparison.
///
/// Wholesale placement read the running <c>peakBreakoutScore &gt;= .20</c> in any market as proof, and when every
/// proven market was already covered it hopped to a bordering one. A Small label proven only at home (which it already
/// covers) therefore hopped outward every time its monthly roll came up: 76% of 1960 Small placements under the
/// regional-hit deal gate were hops into a market where nothing had sold. Coverage of a Small release still reached
/// 4-5 regions inside a year.
///
/// Under the switch a house is still the right channel for an unproven local record, so placement is not gated on a
/// hit. It is paced instead:
/// <list type="bullet">
/// <item>A direct placement in a market needs a record that has held LocalTraction there for
/// <see cref="SustainedTractionWeeks"/> weeks (<see cref="RegionalRecordData.tractionWeeks"/>, which counts up while the
/// current score is at .20 and down otherwise, so the proof fades with the record).</item>
/// <item>A hop to a bordering market needs a regional hit in the source market now
/// (<see cref="RegionalHitDealGate.IsRecent"/>): the house next door took the line because it was selling hard one region
/// over, not because the label existed.</item>
/// </list>
/// </summary>
public static class WholesaleSpreadGate {
	public const string Switch = "--slow-wholesale-spread";
	private static bool configured, enabled = true;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = !args.Contains("--no-slow-wholesale-spread");
		if (args.Contains(Switch) && !enabled) throw new ArgumentException("Conflicting wholesale spread switches.");
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; enabled = value; }

	// Model choice: three net weeks of LocalTraction in the market. Offline on gate1-2002-52 this keeps 37 of the 94
	// direct Small placements (4 weeks: 25), before any behavioural response.
	public const int SustainedTractionWeeks = 3;

	public static bool IsSustained(RegionalRecordData data) => data != null && data.tractionWeeks >= SustainedTractionWeeks;
}
