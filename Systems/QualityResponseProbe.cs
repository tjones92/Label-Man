using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Stacked quality response diagnostics (Big5ChartOpportunityInvestigation.md, 2026-11-17), off unless named.
/// Among top-tier newcomers in all seven regions, the Big-5 Top 10 edge vanishes once log quality is in the model,
/// but the log-quality slope is ~12 in the odds against a designed purchase q^4. Several quality terms stack.
///
/// --wom-quality-once: word of mouth reads quality once. Its stock already builds at q^2.2 (UpdateWordOfMouth); the
/// awareness it adds no longer multiplies by a second quality term, max(0, (q - .45) * 2.2), but by that term's value
/// at the reference quality. Candidate repair: it removes a double count.
///
/// --purchase-quality-exponent=K: the purchase demand curve is qRef^4 * (q / qRef)^K instead of q^4. Diagnostic only.
/// It sizes how much the purchase term's steepness carries; SingleVolumeLaneAndHitTailRepairHandoff.md forbids
/// tuning QUALITY_EXPONENT in isolation for adoption.
///
/// Both leave a record at qRef = 0.68 (the median launch quality of the reference runs) exactly where it is today,
/// and no random number is drawn.
/// </summary>
public static class QualityResponseProbe {
	public const string WomSwitch = "--wom-quality-once";
	public const string PurchaseSwitch = "--purchase-quality-exponent=";
	public const float ReferenceQuality = 0.68f;
	private const float DesignExponent = 4f;
	private static bool configured;
	public static bool WomQualityOnce { get; private set; }
	/// <summary>The purchase exponent; 0 when off (the design exponent applies).</summary>
	public static float PurchaseExponent { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		WomQualityOnce = args.Contains(WomSwitch);
		PurchaseExponent = 0f;
		var values = args.Where(a => a.StartsWith(PurchaseSwitch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + PurchaseSwitch.TrimEnd('=') + " once.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0].Substring(PurchaseSwitch.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float k) ||
				!float.IsFinite(k) || k < .5f || k > 6f)
				throw new ArgumentException(PurchaseSwitch.TrimEnd('=') + " needs an exponent in [0.5, 6].");
			PurchaseExponent = k;
		}
		if (WomQualityOnce || PurchaseExponent > 0f)
			GD.Print($"QUALITY_RESPONSE womOnce={WomQualityOnce} purchaseExponent={PurchaseExponent:F2} qRef={ReferenceQuality:F2}");
		configured = true;
	}

	internal static void SetForChecks(bool womOnce, float purchaseExponent) {
		WomQualityOnce = womOnce; PurchaseExponent = purchaseExponent; configured = true;
	}

	/// <summary>Purchase demand curve. Off: exactly Mathf.Pow(quality, 4).</summary>
	public static float DemandCurve(float quality) =>
		PurchaseExponent <= 0f
			? Mathf.Pow(quality, DesignExponent)
			: Mathf.Pow(ReferenceQuality, DesignExponent) * Mathf.Pow(Mathf.Max(0f, quality) / ReferenceQuality, PurchaseExponent);

	/// <summary>Word-of-mouth effectiveness on awareness. Off: exactly max(0, (q - .45) * 2.2).</summary>
	public static float WomEffectiveness(float quality) =>
		Mathf.Max(0, ((WomQualityOnce ? ReferenceQuality : quality) - 0.45f) * 2.2f);
}
