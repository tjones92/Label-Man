using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// --major-scale-gate=R: a MidTier label promotes to Major only when its recent record gross is at least R times the
/// smallest active Major's. The tier gate otherwise reads roster size (25) and 16 Hot 100 records in 52 weeks -- a count
/// a hot singles label clears on volume alone. In the major-earned-v1 screen Cameo-Parkway cleared it in week 61 in every
/// arm at a trailing gross of $6M against the smallest Major's $11-21M (0.29-0.53x), then doubled to about $20M once the
/// Major roster and branches were granted. A Major was a firm of a Major's size; this asks for that, relative to the
/// Majors actually present, and nothing else -- a label that genuinely outgrows a Major still promotes.
/// Reads LabelCompanyState.recentRevenue (a saved three-month moving average of gross plus service income), so it adds
/// no save state. Off, or without the company ledger, the gate passes and promotion is unchanged.
/// </summary>
public static class MajorScaleGate {
	public const string Switch = "--major-scale-gate";
	private static bool configured;
	public static float Ratio { get; private set; }
	public static bool Enabled => Ratio > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Ratio = 0f;
		foreach (string arg in arguments ?? Array.Empty<string>()) {
			if (arg == Switch) Ratio = 0.75f;
			else if (arg.StartsWith(Switch + "=", StringComparison.Ordinal) &&
				float.TryParse(arg.Substring(Switch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float r))
				Ratio = Math.Max(0f, r);
		}
		configured = true;
	}

	internal static void SetRatioForChecks(float ratio) { configured = true; Ratio = ratio; }

	/// <summary>The smallest active Major's recent gross, or null when there is no Major with company accounts.</summary>
	internal static double? SmallestMajorRevenue(IEnumerable<AILabel> labels, AILabel candidate) {
		double? smallest = null;
		foreach (AILabel major in labels ?? Enumerable.Empty<AILabel>()) {
			if (major == null || major == candidate || major.tier != LabelTier.Major || !major.IsActive || major.isPlayerOwned) continue;
			if (!LabelCorporateModel.Applies(major) || major.company == null || !major.company.accountsOpened) continue;
			double revenue = major.company.recentRevenue;
			if (smallest == null || revenue < smallest) smallest = revenue;
		}
		return smallest;
	}

	public static bool Passes(AILabel candidate, IEnumerable<AILabel> labels) {
		if (!Enabled || candidate == null || !LabelCorporateModel.Applies(candidate) || candidate.company == null) return true;
		double? bar = SmallestMajorRevenue(labels, candidate);
		if (bar == null || bar <= 0) return true;
		double revenue = candidate.company.recentRevenue;
		bool passes = revenue >= Ratio * bar.Value;
		if (!passes) GD.Print($"MAJOR_SCALE_GATE_HOLD id={candidate.labelId} name={candidate.labelName} " +
			$"recentRevenue={revenue:F0} smallestMajor={bar.Value:F0} ratio={revenue / bar.Value:F3} bar={Ratio:F2}");
		return passes;
	}
}
