using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Stocking probe for AI singles. Off, a launch fills every reachable region's shelves at once: a median
/// 11k-69k copies per single by tier, more than most period small-label singles sold in their lives, and in the
/// default model those copies are never manufactured or billed. A period first run was a few hundred to a few
/// thousand copies, then reorders where it sold (DemandAttentionImplementation, "Launch order versus period
/// evidence").
///
/// When on, the launch allocation is scaled by <see cref="Share"/>, keeping its regional shape and at least
/// <see cref="MinimumShelf"/> copies in any region it reached. Sell-through reorders let a dealer reorder any
/// record that ran out with live demand, charted or not, through the ordinary restock path and its service and
/// capacity limits; without them an uncharted single could never sell past its first run. No random draws.
/// </summary>
public static class LaunchStocking {
	public const string SharePrefix = "--launch-stock-share=";
	public const string ReorderSwitch = "--sell-through-reorders";
	public const int MinimumShelf = 25;
	private static bool configured;
	public static float Share { get; private set; } = 1f;
	public static bool SellThroughReorders { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string raw = args.FirstOrDefault(a => a.StartsWith(SharePrefix, StringComparison.Ordinal))?.Substring(SharePrefix.Length);
		if (raw != null && (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float share) || share <= 0f || share > 1f))
			throw new ArgumentException(SharePrefix + " takes a share above 0 and at most 1.");
		Share = raw == null ? 1f : float.Parse(raw, CultureInfo.InvariantCulture);
		SellThroughReorders = args.Contains(ReorderSwitch);
		configured = true;
	}

	internal static void SetForChecks(float share, bool reorders) { configured = true; Share = share; SellThroughReorders = reorders; }

	public static bool AppliesToLaunch(Record record) =>
		Share < 1f && record != null && !record.isPlayerOwned && record.format != ReleaseFormat.Album;

	public static int ScaleLaunch(int units) => units <= 0 ? units : Math.Max(MinimumShelf, (int)MathF.Round(units * Share));

	/// <summary>A dealer reorders an AI single that ran out while buyers were still asking for it.</summary>
	public static bool NeedsReorder(RecordRuntimeData record, int backorders, float rawDemand) =>
		SellThroughReorders && record?.baseRecord != null && !record.baseRecord.isPlayerOwned &&
		record.baseRecord.format != ReleaseFormat.Album && backorders > 0 && rawDemand > 0f;
}
