using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Opt-in economy calibration of LP purchase budgets, with period prices unchanged.
/// The withdrawn budget is not transferred to Singles or to another region.</summary>
public static class AlbumSalesVolume {
	public static float Multiplier { get; private set; } = 1f;
	private static bool configured;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Multiplier = LabelPressingEconomics.ReadParameter((arguments ?? Array.Empty<string>()).ToArray(),
			"--album-purchase-volume=", 1f, .25f, 1f);
		configured = true;
	}
	internal static void SetForChecks(float value) => Multiplier = value;
	internal static int Budget(int original) => Multiplier == 1f ? original : (int)MathF.Floor(Math.Max(0, original) * Multiplier);
}
