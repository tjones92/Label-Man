using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// --launch-caps-major-only scopes <see cref="LaunchPushSaturation"/> and <see cref="LaunchStockSpread"/> to Major-tier
/// releasing labels. Both caps are one-sided above a 0.55 knee, which also catches what non-Majors EARN: 43% of
/// Independent launches sit above the knee on reach won through distribution (half owned, half borrowed through deals;
/// about a 13% stock-depth cut), MidTiers about 90% (about 20%). The caps were built for the Majors' founding launch
/// machinery, so with this switch a non-Major's depth and campaign impact pass untouched. Off: both caps act on every
/// label, as before.
/// </summary>
public static class LaunchCapScope {
	public const string MajorOnlySwitch = "--launch-caps-major-only";
	private static bool configured;
	public static bool MajorOnly { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		MajorOnly = (arguments ?? Array.Empty<string>()).Contains(MajorOnlySwitch);
		configured = true;
	}

	internal static void SetMajorOnlyForChecks(bool value) { configured = true; MajorOnly = value; }

	public static bool Applies(AILabel label) => !MajorOnly || label?.tier == LabelTier.Major;
}
