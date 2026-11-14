using System;
using System.Globalization;

/// <summary>
/// Three screened fixes from SimTools/GameyBehaviorAudit.md. All off unless named; none draws a random number. Each reads
/// the command line on first use, so world generation (which runs before the chart manager configures anything) sees them.
/// </summary>
internal static class GameyAuditArguments {
	internal static bool Has(string name) => Array.IndexOf(SimulationArguments.Get(), name) >= 0;
	internal static float? Value(string name, float minimum, float maximum) {
		foreach (string arg in SimulationArguments.Get()) {
			if (!arg.StartsWith(name + "=", StringComparison.Ordinal)) continue;
			if (!float.TryParse(arg.Substring(name.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
				!float.IsFinite(value) || value < minimum || value > maximum)
				throw new ArgumentException($"{name} must be between {minimum} and {maximum}.");
			return value;
		}
		return null;
	}
}

/// <summary>
/// --lp-earned-discovery=F (audit finding 1). An LP's national awareness grows every week by 0.004 + 0.012 x appeal of the
/// people who have not heard of it, whoever made it (AlbumSimulator.UpdateAlbum). That term is the same for an unknown act
/// on a one-room label and for a star, never decays, and is why an LP by an act whose best single sold under 1,000 copies
/// still sells a median 17-19k. With the switch on the term is scaled by F + (1 - F) x known, where known is how much of an
/// audience the act has: its chart heat this week or its release-awareness bonus (momentum, reputation, recognition)
/// against <see cref="KnownActBonus"/>, whichever is larger. F is the share an act nobody knows keeps. Label push, chart
/// discovery and the heat term are untouched. A record with no roster act (a soundtrack) is untouched.
/// </summary>
public static class LpEarnedDiscovery {
	public const string Switch = "--lp-earned-discovery";
	/// <summary>The release-awareness bonus at which an act counts as fully known (an established act with some momentum).</summary>
	public const float KnownActBonus = 0.30f;
	private static bool read;
	private static float floor = 1f;
	public static bool Enabled { get { Read(); return floor < 1f; } }
	private static void Read() {
		if (read) return;
		floor = GameyAuditArguments.Value(Switch, 0f, 1f) ?? 1f;
		read = true;
	}
	internal static void SetForChecks(float value) { read = true; floor = value; }
	internal static float FloorForChecks { get { Read(); return floor; } }

	public static float Known(float artistHeat, float actBonus) =>
		Math.Clamp(Math.Max(artistHeat, actBonus / KnownActBonus), 0f, 1f);

	/// <summary>The multiplier on the unconditional discovery term. Exactly 1 when off.</summary>
	public static float Factor(float artistHeat, float actBonus) {
		Read();
		return floor >= 1f ? 1f : floor + (1f - floor) * Known(artistHeat, actBonus);
	}
}

/// <summary>
/// --staggered-opening-contracts (audit finding 4). A seeded 1960 contract expires at max(1961, signed year + term) and its
/// week is (expiry year - 1960) x 52, so every seeded deal matures on a 1 January and every deal already past its term is
/// clamped to 1 January 1961: 637 contract-expiry drops on one day, 40% of all drops in two years. With the switch on each
/// seeded deal keeps its expiry year but matures on its own anniversary week inside the twelve months before that
/// 1 January, keyed on the act's id.
/// </summary>
public static class StaggeredOpeningContracts {
	public const string Switch = "--staggered-opening-contracts";
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }

	/// <summary>The expiry week of a seeded deal. Off: the year boundary. On: a week in the 52 before it, from the act's id.</summary>
	public static int ExpiryWeek(string artistId, int expiresYear, int startYear) {
		int boundary = (expiresYear - startYear) * 52;
		if (!Enabled) return boundary;
		uint hash = 2166136261u;
		foreach (char c in artistId ?? string.Empty) { hash ^= c; hash *= 16777619u; }
		hash ^= hash >> 15; hash *= 0x2c1b3c6du; hash ^= hash >> 12;
		return boundary - 51 + (int)(hash % 52u);
	}
}

/// <summary>
/// --receivable-backed-overdraft=H (audit finding 3). EconomicLabelLifecycle bankrupts a label after two month-ends with
/// cash below zero and never counts what wholesale houses owe it, so a label with a hit and receivables several times its
/// overdraft is liquidated (16% of bankruptcies in the control). With the switch on, a label whose overdraft is no more
/// than H of its outstanding wholesale receivables is carried by its plant and distributors: the month does not count
/// toward insolvency, the label is Struggling, and it pays <see cref="MonthlyCharge"/> of the overdraft that month. Once
/// the overdraft passes H of the receivables the ordinary two-month rule runs.
/// </summary>
public static class ReceivableBackedOverdraft {
	public const string Switch = "--receivable-backed-overdraft";
	/// <summary>A factor's or plant's monthly carrying charge on the overdraft: 1.5% a month, a scenario value.</summary>
	public const float MonthlyCharge = 0.015f;
	private static bool read;
	private static float share;
	public static float Share { get { if (!read) { share = GameyAuditArguments.Value(Switch, 0f, 1f) ?? 0f; read = true; } return share; } }
	internal static void SetForChecks(float value) { read = true; share = value; }

	public static bool Carries(float cash, float receivables) =>
		Share > 0f && cash < 0f && -cash <= Share * Math.Max(0f, receivables);
}
