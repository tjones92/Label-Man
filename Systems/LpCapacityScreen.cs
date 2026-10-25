using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Independent, opt-in LP work and purchase-capacity scenarios. No demand multiplier, single budget
/// change, new RNG or save state. Purchase additions are LP-only after the existing mixed allocation completes.</summary>
public static class LpCapacityScreen {
	public const string ProgrammeSwitch = "--lp-programme-scale=";
	public const string PurchaseSwitch = "--lp-purchase-share=";
	public const string AuditSwitch = "--lp-capacity-audit";
	public static float ProgrammeScale { get; private set; } = 1;
	public static float PurchaseShare { get; private set; }
	public static bool Audit { get; private set; }
	public static bool PurchaseEnabled => PurchaseShare > 0;
	private static bool configured;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		ProgrammeScale = Read(args, ProgrammeSwitch, 1, 1, 4);
		PurchaseShare = Read(args, PurchaseSwitch, 0, .35f, .65f);
		if (args.Any(a => a.StartsWith(ProgrammeSwitch, StringComparison.Ordinal)) &&
			(!args.Contains(LabelReleaseProgramme.AllocationSwitch) || !args.Contains(LabelReleaseProgramme.CapacitySwitch)))
			throw new ArgumentException(ProgrammeSwitch + " requires slate and programme capacity.");
		if (PurchaseEnabled && args.Any(a => a.StartsWith("--album-purchase-volume=", StringComparison.Ordinal)))
			throw new ArgumentException("LP capacity screen cannot be combined with the album purchase-volume retrenchment.");
		Audit = args.Contains(AuditSwitch) || ProgrammeScale != 1 || PurchaseEnabled;
		configured = true;
	}
	private static float Read(string[] args, string prefix, float fallback, float min, float max) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length == 0) return fallback;
		if (values.Length != 1 || !float.TryParse(values[0][prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
			!float.IsFinite(value) || value < min || value > max) throw new ArgumentException(prefix + " requires one finite value in range.");
		return value;
	}
	internal static float AlbumRate(AILabel label, float rate) => ProgrammeScale == 1 || label.isPlayerOwned ? rate : rate * ProgrammeScale;
	internal static int AdditionalAlbumCapacity(int baseSingleCapacity, int legacyAlbumCapacity) => !PurchaseEnabled ? 0 :
		Math.Max(0, (int)Math.Round(baseSingleCapacity * ((double)PurchaseShare / (1 - PurchaseShare)), MidpointRounding.AwayFromZero) - legacyAlbumCapacity);
	internal static void SetForChecks(float programme, float purchase, bool audit = true) {
		configured = true; ProgrammeScale = programme; PurchaseShare = purchase; Audit = audit;
	}
	internal static void ResetForChecks() { configured = false; ProgrammeScale = 1; PurchaseShare = 0; Audit = false; }
}
