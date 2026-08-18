using System;
using System.Collections.Generic;

/// <summary>
/// Feature boundary for the 1960 star canopy. The initial-roster seeding
/// (RosterManager.InitialSignArtist) caps careerState at Established, so the launch world has no
/// Star/Superstar incumbents even though the runtime ladder grows them - an unnatural ecosystem with
/// no star canopy at day one. With this flag ON, a deterministic post-pass promotes a handful of the
/// best acts on the big labels to a realistic top tier. With it OFF no artist is touched, no draw is
/// made, and the seeded roster is byte-identical to the pre-canopy baseline.
/// <para>See SimTools/ScoutingMechanicDirective.md.</para>
/// </summary>
public static class StarCanopy {
	private static bool configured;
	private static bool enabled;
	public static bool Enabled => enabled;

	// Rough historical top tier for the whole 1960 industry.
	public const int SuperstarCount = 6;
	public const int StarCount = 24;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--seed-star-canopy") enable = true;
			if (argument == "--no-seed-star-canopy") disable = true;
		}
		if (enable && disable)
			throw new ArgumentException("--seed-star-canopy and --no-seed-star-canopy cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		configured = true;
	}

	internal static bool CaptureSwitch() => enabled;
	internal static void RestoreSwitch(bool value) { configured = true; enabled = value; }
	internal static void ConfigureForProbe(bool enable) { configured = true; enabled = enable; }
	internal static void ResetForProbe() { configured = false; enabled = false; }
}
