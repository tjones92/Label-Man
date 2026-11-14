using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// --indie-distributor-access=A: launch stock a non-Major AI label ships into a region it has no distribution in. Off,
/// every label gets 0.18 of its covered depth there. Independents cover about 3 of the 7 regions and Majors all 7, so
/// 67% of top-quality Independent singles launched under 5,000 units against 6% of the Big-5's (major-earned-v1 ms,
/// 1960-62), and that footprint, not record quality, carried about 80% of the Top 10 conversion gap. In 1960 a
/// national network of independent distributors took indie records into markets the label never served itself;
/// this probe lets that network carry A of the covered depth. Stock only: launch awareness and radio are unchanged.
/// Draws no random numbers, so the stream is untouched.
/// </summary>
public static class IndieDistributorAccess {
	public const string Switch = "--indie-distributor-access";
	public const float BaseUncoveredAccess = 0.18f;
	private static bool configured;
	public static float Access { get; private set; } = BaseUncoveredAccess;
	public static bool Enabled => Access != BaseUncoveredAccess;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Access = BaseUncoveredAccess;
		foreach (string arg in arguments ?? Array.Empty<string>())
			if (arg.StartsWith(Switch + "=", StringComparison.Ordinal) &&
				float.TryParse(arg.Substring(Switch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
				Access = Math.Clamp(a, 0f, 1f);
		configured = true;
	}

	internal static void SetAccessForChecks(float access) { configured = true; Access = access; }

	public static float UncoveredAccess(AILabel label) =>
		Enabled && label != null && !label.isPlayerOwned && label.tier != LabelTier.Major ? Access : BaseUncoveredAccess;
}
