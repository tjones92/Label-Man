using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Experimental (FanbaseEfficacyEvidence.md, part B): how a region's singles purse is shared when intent exceeds it.
/// Today every title clears the same fraction of its intent, so a record wanted by ten times as many buyers sells ten
/// times as many, never more, and no single after 1960 sells more than about 120k in a week. A pocket-money buyer who
/// wants several records buys the one they want most first, so the most-wanted titles keep more of their intent.
/// With exponent g each title clears λ·intent^g, never more than its intent, with λ set so the purse is spent exactly.
/// g = 1 is today's proportional ration (and runs today's code). The ranking of titles by intent is unchanged.
/// </summary>
public static class SinglesRationExponent {
	public const string Switch = "--singles-ration-exponent=";
	public static float Exponent { get; private set; } = 1f;
	public static bool Enabled => Exponent != 1f;
	private static bool configured;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (!argument.StartsWith(Switch, StringComparison.Ordinal)) continue;
			if (!float.TryParse(argument[Switch.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float g)
				|| !float.IsFinite(g) || g < 1f || g > 3f)
				throw new ArgumentException(Switch + " must be finite and between 1 and 3.");
			Exponent = g;
		}
		configured = true;
	}

	internal static void SetForChecks(float g) { Exponent = g; configured = true; }

	/// <summary>
	/// Exact (unrounded) clearing per title: min(intent, λ·intent^g) summing to <paramref name="budget"/>, which must not
	/// exceed the total intent. Titles are capped at their intent from the largest down (g ≥ 1 caps the largest first).
	/// </summary>
	public static double[] Shares(IReadOnlyList<int> intents, double budget, float g) {
		int n = intents.Count;
		var exact = new double[n];
		if (n == 0 || budget <= 0) return exact;
		int[] order = Enumerable.Range(0, n).Where(i => intents[i] > 0).OrderByDescending(i => intents[i]).ThenBy(i => i).ToArray();
		double[] w = new double[n];
		double weightLeft = 0;
		foreach (int i in order) { w[i] = Math.Pow(intents[i], g); weightLeft += w[i]; }
		double left = budget;
		int k = 0;
		double lambda = 0;
		while (k < order.Length) {
			lambda = weightLeft > 0 ? left / weightLeft : 0;
			int top = order[k];
			if (lambda * w[top] < intents[top]) break;
			exact[top] = intents[top]; left -= intents[top]; weightLeft -= w[top]; k++;
		}
		for (int j = k; j < order.Length; j++) exact[order[j]] = Math.Min(intents[order[j]], lambda * w[order[j]]);
		return exact;
	}
}
