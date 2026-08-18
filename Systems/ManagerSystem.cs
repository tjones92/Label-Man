using System;
using System.Collections.Generic;

/// <summary>
/// Feature boundary for artist managers (Scouting Mechanic Phase 3). Stamping a manager at
/// generation costs one <c>Randf()</c> draw per artist, which shifts the entire population RNG
/// schedule - so it ships behind its own switch. With the flag OFF no draw is made, every artist
/// stays a neutral <see cref="ManagerArchetype.None"/>, and the term-sheet / affordability path is
/// byte-identical to the pre-manager baseline. With the flag ON managers stamp and the 1960 roster
/// (and every replay seed) reseeds to a new, valid baseline.
/// <para>See SimTools/ScoutingMechanicDirective.md.</para>
/// </summary>
public static class ManagerSystem {
	private static bool configured;
	private static bool enabled;
	public static bool Enabled => enabled;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--enable-managers") enable = true;
			if (argument == "--disable-managers") disable = true;
		}
		if (enable && disable)
			throw new ArgumentException("--enable-managers and --disable-managers cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		configured = true;
	}

	/// <summary>The raw switch position, so a probe suite can put it back.</summary>
	internal static bool CaptureSwitch() => enabled;

	internal static void RestoreSwitch(bool value) {
		configured = true;
		enabled = value;
	}

	internal static void ConfigureForProbe(bool enable) {
		configured = true;
		enabled = enable;
	}

	internal static void ResetForProbe() {
		configured = false; enabled = false;
	}
}
