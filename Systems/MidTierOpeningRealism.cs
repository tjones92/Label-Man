using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Opt-in (SimTools/MidTierClosureResearch.md): an incorporated MidTier staffs for the roster it actually carries.
/// <see cref="LabelCorporateModel"/> sizes every MidTier's executive, clerical, finance, business-affairs, production,
/// promotion, sales and international desks from a fixed tier term (scale 1.8), so a label with 8 acts pays the same
/// ~$13k a month as one with 25. With the switch on, that tier term is scaled by the label's roster against the tier's
/// normal opening roster (<see cref="ReferenceActs"/>), never above the tier term and never below
/// <see cref="Floor"/> of it. The act- and project-driven terms are unchanged, so a growing label hires as before.
/// It sets costs only: no cash, release, chart or survival rule is touched.
/// </summary>
public static class WorkloadStaffing {
	public const string Switch = "--workload-scaled-staffing";
	// The MidTier lifecycle capacity (25) at the midpoint of the launch fill range (.5-.75): 15.6 acts.
	internal const double ReferenceActs = 15;
	// A skeleton head office (owner, bookkeeper, one promotion man) survives any roster.
	internal const double Floor = .35;
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; Enabled = value; }

	internal static double TierScale(LabelTier tier, double tierScale, double acts) =>
		Enabled && tier == LabelTier.MidTier ? tierScale * Math.Clamp(acts / ReferenceActs, Floor, 1) : tierScale;
}

/// <summary>
/// Opt-in (SimTools/MidTierClosureResearch.md): the four London templates (Parlophone, Pye, EMI, Decca UK) do not
/// open as US operating labels. In 1960 EMI sold in the US through Capitol and Decca UK through London Records;
/// Parlophone and Pye licensed masters to American firms. Based in London, scene recruitment reaches no US act, so
/// the four open with 0-5 acts and Parlophone and Pye carry a MidTier payroll with no catalogue. They belong to the
/// British Invasion mechanic, which should bring them in as licensors. The label count shrinks by four; no
/// procedural label takes their place.
/// </summary>
public static class BritishLabelDeferral {
	public const string Switch = "--defer-british-labels";
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; Enabled = value; }
}
