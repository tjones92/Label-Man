using System;
using System.Linq;
using Godot;

/// <summary>--singles-ration-knee-check: with no knee the ration is the exponent's own, bit for bit; with a knee the
/// priority holds above it, every title below it clears at one rate, the purse is spent exactly, no title clears more
/// than its intent, and the order of titles never changes.</summary>
public static class SinglesRationKneeChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Singles ration knee: " + message); checks++; }
		Check(SinglesRationExponent.KneeRank == 0, "there is no knee by default");
		int[] rack = { 1_000, 500, 100, 20, 5, 0 };
		double[] whole = SinglesRationExponent.Shares(rack, 325, 1.5f), none = SinglesRationExponent.Shares(rack, 325, 1.5f, 0),
			kneed = SinglesRationExponent.Shares(rack, 325, 1.5f, 2), wide = SinglesRationExponent.Shares(rack, 325, 1.5f, 5);
		Check(whole.SequenceEqual(none), "knee 0 is the exponent alone, bit for bit");
		Check(whole.SequenceEqual(wide), "a knee at or past the last wanted title is the exponent alone");
		Check(Math.Abs(kneed.Sum() - 325) < 1e-6 && kneed[5] == 0, "the purse is spent exactly and an unwanted title clears nothing");
		Check(Math.Abs(kneed[2] / 100 - kneed[3] / 20) < 1e-9 && Math.Abs(kneed[3] / 20 - kneed[4] / 5) < 1e-9,
			"every title below the knee clears the same fraction of its intent");
		Check(Math.Abs(kneed[1] / 500 - kneed[2] / 100) < 1e-9, "the ration is continuous at the knee title");
		Check(kneed[0] / 1_000 > kneed[1] / 500 && kneed[0] > 200, "above the knee the most-wanted title still keeps more of its intent");
		Check(kneed[4] > whole[4] && kneed[0] < whole[0], "the knee moves units from the top of the list to the bottom");
		Check(kneed.Zip(kneed.Skip(1), (a, b) => a >= b).All(x => x), "the order of titles is unchanged");
		// A flat ration below the knee is the proportional share of what the top two leave.
		double left = 325 - kneed[0] - kneed[1];
		Check(Math.Abs(kneed[2] - left * 100 / 125.0) < 1e-6, "below the knee the remaining purse is split in proportion to intent");
		// A purse close to total intent caps the largest titles at their intent, from the top down.
		double[] tight = SinglesRationExponent.Shares(rack, 1_600, 2f, 3);
		Check(tight[0] == 1_000 && tight[1] == 500 && Math.Abs(tight.Sum() - 1_600) < 1e-6
			&& tight.Zip(rack, (a, b) => a <= b + 1e-9).All(x => x), "no title clears more than its intent");
		// Ties at the knee: titles equal to the knee intent take the exponent's weight, which equals the flat weight there.
		int[] tied = { 400, 100, 100, 100, 10 };
		double[] t = SinglesRationExponent.Shares(tied, 200, 1.35f, 2);
		Check(t[1] == t[2] && t[2] == t[3] && Math.Abs(t[4] / 10 - t[1] / 100) < 1e-9 && Math.Abs(t.Sum() - 200) < 1e-6,
			"titles tied at the knee clear alike");
		GD.Print($"SINGLES_RATION_KNEE_CHECK passed checks={checks}");
	}
}
