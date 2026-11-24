using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/Top10TurnoverFindings.md §11.13-14), off unless named. A live single's servable intent
/// is clipped at the region's shelf stock before the market ration (the fixed singles purse) turns intent into units,
/// at about 0.16. Stock is in units, intent is about six times larger, so a hit is held at its per-label stock ceiling
/// while its shops sell only ~17% of the copies they hold. <c>--post-ration-stock-clip</c> moves the stock limit to the
/// units: intent is clipped only by the shelf sales cap, and a single's cleared units are clipped at its stock after
/// clearing, the excess becoming backorders. Albums are unchanged. No draws: off, the run is byte-identical.
/// </summary>
public static class PostRationStockClip {
	public const string Switch = "--post-ration-stock-clip";
	/// <summary>
	/// §11.16: with the stock clip moved, the shelf sales cap (stores × 250 + department stores × 500, × depth, ×1.5 in
	/// the Top 20) is the next per-region ceiling on intent. <c>--post-ration-shelf-cap</c> (needs the stock clip) moves
	/// it too: intent is unclipped, and cleared units are clipped at the lower of stock and the shelf cap.
	/// </summary>
	public const string ShelfSwitch = "--post-ration-shelf-cap";
	private static bool configured, enabled, shelfEnabled;
	private static long clippedUnits, clippedIntents, checkedIntents;
	public static bool Enabled => enabled;
	public static bool ShelfEnabled => shelfEnabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		configured = true;
		var args = (arguments ?? Array.Empty<string>()).ToList();
		enabled = args.Contains(Switch);
		shelfEnabled = args.Contains(ShelfSwitch);
		if (shelfEnabled && !enabled) throw new ArgumentException($"{ShelfSwitch} requires {Switch}.");
		if (enabled) GD.Print(shelfEnabled ? "POST_RATION_STOCK_CLIP on (shelf cap after the ration)" : "POST_RATION_STOCK_CLIP on");
	}

	internal static void SetForChecks(bool value, bool shelf = false) { configured = true; enabled = value; shelfEnabled = value && shelf; }

	/// <summary>Whether this live single's intent skips the pre-ration stock clip.</summary>
	public static bool SkipsIntentClip(bool liveSingle) => enabled && liveSingle;

	/// <summary>Whether this live single's intent also skips the shelf sales cap (applied to its units instead).</summary>
	public static bool SkipsShelfCap(bool liveSingle) => shelfEnabled && liveSingle;

	/// <summary>The units limit after the ration: stock, and with the shelf switch the shelf cap too.</summary>
	public static int UnitLimit(int stock, int shelfCap) => shelfEnabled ? Math.Min(stock, Math.Max(0, shelfCap)) : stock;

	/// <summary>Clips cleared units at stock (spillover first), books the excess as backorders; returns units removed.</summary>
	public static int ClipCleared(ref int cleared, ref int spillover, ref int localCleared, ref int backorders, int stock) {
		checkedIntents++;
		int excess = cleared - Math.Max(0, stock);
		if (excess <= 0) return 0;
		int spill = Math.Min(excess, spillover);
		spillover -= spill;
		localCleared -= excess - spill;
		cleared -= excess;
		backorders += excess;
		clippedUnits += excess;
		clippedIntents++;
		return excess;
	}

	public static void Report(int week) {
		if (enabled && week > 0 && week % 26 == 0)
			GD.Print(FormattableString.Invariant(
				$"POST_RATION_STOCK_CLIP week={week} clippedIntents={clippedIntents} of {checkedIntents} clippedUnits={clippedUnits}"));
	}
}
