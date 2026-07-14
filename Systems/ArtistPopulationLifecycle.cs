using System;
using System.Collections.Generic;

/// <summary>
/// Directive 6 feature boundary.  This is deliberately independent from the
/// Genre Market V2 switch so that the old 45-stream replay has no new work,
/// allocations, or random draws when population renewal is off.
/// </summary>
public static class ArtistPopulationLifecycle {
	private static bool configured;
	private static bool enabled;
	public static bool Enabled => enabled;
	public static bool IsLive => enabled && GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--enable-artist-population-lifecycle") enable = true;
			if (argument == "--disable-artist-population-lifecycle") disable = true;
		}
		if (enable && disable)
			throw new ArgumentException("--enable-artist-population-lifecycle and --disable-artist-population-lifecycle cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		if (enabled && !GenreMarketV2.Enabled)
			throw new ArgumentException("Artist population lifecycle requires Genre Market V2 to be enabled.");
		configured = true;
	}
}
