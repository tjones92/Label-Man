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
	private static bool suppressInitialReserve;
	public static bool Enabled => enabled;
	/// <summary>Diagnostic-only boundary. It is meaningful only on the enabled lifecycle path.</summary>
	public static bool SuppressInitialReserve => enabled && suppressInitialReserve;
	public static bool IsLive => enabled && GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
	internal static bool ShouldMaterializeInitialReserveFor(bool lifecycleEnabled, bool suppressReserve) => lifecycleEnabled && !suppressReserve;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false, suppressReserve = false, materializeReserve = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--enable-artist-population-lifecycle") enable = true;
			if (argument == "--disable-artist-population-lifecycle") disable = true;
			if (argument == "--suppress-enabled-initial-reserve") suppressReserve = true;
			if (argument == "--materialize-enabled-initial-reserve") materializeReserve = true;
		}
		if (enable && disable)
			throw new ArgumentException("--enable-artist-population-lifecycle and --disable-artist-population-lifecycle cannot be used together.");
		if (suppressReserve && materializeReserve)
			throw new ArgumentException("--suppress-enabled-initial-reserve and --materialize-enabled-initial-reserve cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		if (enabled && !GenreMarketV2.Enabled)
			throw new ArgumentException("Artist population lifecycle requires Genre Market V2 to be enabled.");
		// Keep the disabled compatibility path independent of the diagnostic flag.
		suppressInitialReserve = enabled && suppressReserve;
		configured = true;
	}
}
