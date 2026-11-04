using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit A3: the old compilation draw charges .6 single sessions even with zero reusable masters.
/// A fully new LP must pay the ordinary 2.4 sessions. Retain the old four-master discount,
/// interpolate partial inventory, and use the identical basis before and after format generation.
/// No demand, material, quality, format draw or RNG changes directly.
/// </summary>
public static class AlbumMasterCosts {
	public const string Switch = "--album-master-costs";
	private static bool configured, enabled;
	public static bool Enabled => enabled;
	public static bool IsLive => enabled && GenreMarketV2.Enabled &&
		(ChartManager.Instance?.IsGenreMarketV2Live == true || CompetitorManager.Instance?.AlbumPracticeLiveTick == true);
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (enabled && !GenreMarketV2.Enabled) throw new ArgumentException(Switch + " requires the live genre market.");
		configured = true;
	}
	internal static float CompilationMultiplier(int reusedMasters, float fullInventoryMultiplier) =>
		2.4f + (fullInventoryMultiplier - 2.4f) * Math.Clamp(reusedMasters / 4f, 0f, 1f);
	internal static float ExpectedMultiplier(float compilationChance, int reusableMasters, float fullInventoryMultiplier) =>
		Math.Clamp(compilationChance, 0f, 1f) * CompilationMultiplier(reusableMasters, fullInventoryMultiplier) +
		(1f - Math.Clamp(compilationChance, 0f, 1f)) * 2.4f;
}
