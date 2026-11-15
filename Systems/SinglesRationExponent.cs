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
/// <para><c>--singles-ration-knee-rank=K</c> (Top10TurnoverFindings.md §13, off unless named): the priority applies among
/// a region's K most-wanted singles only. Below the K-th title every single clears the same fraction of its intent,
/// continuous at the knee: weight = intent · max(intent, knee)^(g−1). The pocket-money choice is between the hits on
/// the shop's chart rack; a record below it is bought by the people who came in for it. The exponent raised over the
/// whole list made the published sales curve fall about twice as fast below #40 as any reference, because it
/// multiplies every rank-keyed demand term by g. Order-preserving like the exponent, so it reshapes units by rank
/// without touching which record outranks which. With no knee the weights are the exponent's own, bit for bit.</para>
/// </summary>
public static class SinglesRationExponent {
	public const string Switch = "--singles-ration-exponent=";
	public const string KneeSwitch = "--singles-ration-knee-rank=";
	public static float Exponent { get; private set; } = 1f;
	/// <summary>0 = no knee: the exponent applies to every title.</summary>
	public static int KneeRank { get; private set; }
	public static bool Enabled => Exponent != 1f;
	private static bool configured;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		int knees = 0;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument.StartsWith(KneeSwitch, StringComparison.Ordinal)) {
				if (++knees > 1) throw new ArgumentException("Name " + KneeSwitch + " once.");
				if (!int.TryParse(argument[KneeSwitch.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank)
					|| rank < 1 || rank > 500)
					throw new ArgumentException(KneeSwitch + " needs an integer rank in [1, 500].");
				KneeRank = rank;
				continue;
			}
			if (!argument.StartsWith(Switch, StringComparison.Ordinal)) continue;
			if (!float.TryParse(argument[Switch.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float g)
				|| !float.IsFinite(g) || g < 1f || g > 3f)
				throw new ArgumentException(Switch + " must be finite and between 1 and 3.");
			Exponent = g;
		}
		if (KneeRank > 0 && !Enabled) throw new ArgumentException(KneeSwitch + " needs " + Switch + " above 1.");
		if (KneeRank > 0) Godot.GD.Print($"SINGLES_RATION_KNEE rank={KneeRank} exponent={Exponent.ToString(CultureInfo.InvariantCulture)}");
		configured = true;
	}

	internal static void SetForChecks(float g, int kneeRank = 0) { Exponent = g; KneeRank = kneeRank; configured = true; }

	/// <summary>
	/// Exact (unrounded) clearing per title: min(intent, λ·weight) summing to <paramref name="budget"/>, which must not
	/// exceed the total intent. The weight is intent^g, or with <paramref name="kneeRank"/> K above 0 and more than K
	/// titles wanted, intent · max(intent, the K-th largest intent)^(g−1). Either way weight ÷ intent never falls as
	/// intent rises, so titles are capped at their intent from the largest down.
	/// </summary>
	public static double[] Shares(IReadOnlyList<int> intents, double budget, float g, int kneeRank = 0) {
		int n = intents.Count;
		var exact = new double[n];
		if (n == 0 || budget <= 0) return exact;
		int[] order = Enumerable.Range(0, n).Where(i => intents[i] > 0).OrderByDescending(i => intents[i]).ThenBy(i => i).ToArray();
		double[] w = new double[n];
		double weightLeft = 0;
		int knee = kneeRank > 0 && order.Length > kneeRank ? intents[order[kneeRank - 1]] : 0;
		double kneeFactor = knee > 0 ? Math.Pow(knee, g - 1.0) : 0;
		foreach (int i in order) {
			w[i] = intents[i] < knee ? intents[i] * kneeFactor : Math.Pow(intents[i], g);
			weightLeft += w[i];
		}
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
