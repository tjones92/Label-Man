using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit B6: AI-to-AI publishing pays the period mechanical, once.
///
/// Off (the default), with routing live, an AI record expenses a 2c-a-side mechanical in COGS that
/// reaches nobody, and then also routes 11% of list (PublishingShareOfGross, about 9.8c a copy) to
/// whoever controls the composition. A cover pays its composition twice, at about five times the
/// 1909 Act's 2c, and AI catalogs are worth several times the period figure.
///
/// When on, an AI record's routed pool is the period mechanical: 2c a copy for a single's A-side
/// (the B-side's 2c returns to the label as house publishing), and 2c per track for an LP. The
/// mechanical already charged in COGS funds it: the label pays out only the writer, transfer and
/// external slices of that pool, and gets back whatever it expensed beyond them. Player records and
/// player-controlled compositions keep their existing period treatment. Without routing, nothing changes.
/// </summary>
public static class PeriodAiPublishing {
	public const string Switch = "--period-ai-publishing";
	private static bool configured, enabled;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	/// <summary>The mechanical owed on the routed composition(s) of this week's units.</summary>
	internal static float Pool(ReleaseFormat format, float units, float expensedComposition) =>
		format == ReleaseFormat.Single ? Math.Max(0f, units) * MechanicalRoyaltyService.RatePerCopy
		: expensedComposition > 0f ? expensedComposition
		: Math.Max(0f, units) * LabelPressingEconomics.ExpectedAlbumCompositionCost;

	/// <summary>Change to the label's net: the expensed mechanical comes back, the externally owed slices go out.</summary>
	internal static float NetAdjustment(float expensedComposition, float writer, float transfer, float leak) =>
		expensedComposition - (writer + transfer + leak);
}
