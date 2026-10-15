using System;
using System.Collections.Generic;

/// <summary>
/// Feature boundary for coherent January 1960 careers (SimTools/OpeningCareersDesign.md). With it ON, a
/// post-roster pass gives ~75 launch acts a compact pre-1960 history and derives their counters, ages,
/// recognition and members' fame from it; it supersedes the quality-ranked <see cref="StarCanopy"/>.
/// Every draw is keyed on the world seed and the act id, so with it OFF no artist is touched and the
/// world is byte-identical; with it ON nothing else in the stream moves.
/// </summary>
public static class OpeningCareers {
	private static bool configured;
	private static bool enabled;
	public static bool Enabled => enabled;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--opening-careers") enable = true;
			if (argument == "--no-opening-careers") disable = true;
		}
		if (enable && disable)
			throw new ArgumentException("--opening-careers and --no-opening-careers cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		configured = true;
	}

	internal static void ConfigureForProbe(bool enable) { configured = true; enabled = enable; }
	internal static void ResetForProbe() { configured = false; enabled = false; }
}
