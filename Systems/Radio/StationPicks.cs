using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// §9m probe (SimTools/RegionalSyncDesign.md): a block of new-release picks on each station's regional (5e) playlist that
/// is not chosen by local sales standing. Under <c>standing</c> the meeting follows each market's sellers, so the panel
/// stops lifting the young, small, mostly non-Major records the national meeting rotated, and the chart loses the
/// airplay offset that held Majors below their sales ranking (§9k). This reserves <c>--station-picks=N</c> slots in a
/// tier for records at most <c>--station-picks-age</c> weeks old, ranked by one of two non-sales scores:
/// <c>trial</c>, the national meeting's own candidacy (size-blind national support, heat, the DJ's read), and <c>ear</c>,
/// the DJ's read of quality and genre with no sales or heat term. Picks are seated after the ranking, from records it did
/// not seat. Off (0) unless named, and only the regional meeting reads it; it draws nothing of its own.
/// </summary>
public static class StationPicks {
	public enum PickScore { Trial, Ear }
	private const string SlotsPrefix = "--station-picks=";
	private const string TierPrefix = "--station-picks-tier=";
	private const string ScorePrefix = "--station-picks-score=";
	private const string AgePrefix = "--station-picks-age=";
	private static bool configured;
	public static int Slots { get; private set; }
	public static SpinTier Tier { get; private set; } = SpinTier.Light;
	public static PickScore Score { get; private set; } = PickScore.Trial;
	public static int MaxAgeWeeks { get; private set; } = 6;
	public static bool Enabled => Slots > 0;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string a in arguments ?? Array.Empty<string>()) {
			if (a.StartsWith(SlotsPrefix, StringComparison.Ordinal)) Slots = PositiveInt(a, SlotsPrefix);
			else if (a.StartsWith(AgePrefix, StringComparison.Ordinal)) MaxAgeWeeks = PositiveInt(a, AgePrefix);
			else if (a.StartsWith(TierPrefix, StringComparison.Ordinal))
				Tier = a.Substring(TierPrefix.Length) switch {
					"light" => SpinTier.Light,
					"mid" => SpinTier.Mid,
					"high" => SpinTier.High,
					_ => throw new ArgumentException(TierPrefix + " takes light, mid or high.")
				};
			else if (a.StartsWith(ScorePrefix, StringComparison.Ordinal))
				Score = a.Substring(ScorePrefix.Length) switch {
					"trial" => PickScore.Trial,
					"ear" => PickScore.Ear,
					_ => throw new ArgumentException(ScorePrefix + " takes trial or ear.")
				};
		}
		configured = true;
	}

	private static int PositiveInt(string argument, string prefix) {
		if (!int.TryParse(argument.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value <= 0)
			throw new ArgumentException(prefix + " takes a positive whole number.");
		return value;
	}
}
