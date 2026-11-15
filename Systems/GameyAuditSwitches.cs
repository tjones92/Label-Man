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
	/// <summary>--receivable-overdraft-charge=C: the monthly carrying charge on the carried overdraft. Default 1.5% a month (the
	/// value screened 2026-11-12). Saulnier and Jacoby (NBER 1943, ch. 7) give about 12% a year on money in use for 1940,
	/// 0.75-1.7% a month across clients; 1960 rates were higher. The candidate bundle uses 0.0125 (adopted 2026-11-15).</summary>
	public const string ChargeSwitch = "--receivable-overdraft-charge";
	public const float DefaultMonthlyCharge = 0.015f;
	private static bool chargeRead;
	private static float charge;
	public static float MonthlyCharge { get { if (!chargeRead) { charge = GameyAuditArguments.Value(ChargeSwitch, 0f, 0.05f) ?? DefaultMonthlyCharge; chargeRead = true; } return charge; } }
	internal static void SetChargeForChecks(float value) { chargeRead = true; charge = value; }
	private static bool read;
	private static float share;
	public static float Share { get { if (!read) { share = GameyAuditArguments.Value(Switch, 0f, 1f) ?? 0f; read = true; } return share; } }
	internal static void SetForChecks(float value) { read = true; share = value; }

	public static bool Carries(float cash, float receivables) =>
		Share > 0f && cash < 0f && -cash <= Share * Math.Max(0f, receivables);
}

/// <summary>
/// --lp-earned-launch (audit finding 1, second screen). Every LP launches with a 0.04 awareness base before the act, the
/// label's marketing and its reputation are added (CompetitorManager.ApplyReleasePromotion, ApplyPromotionSnapshot). With
/// the switch on the base is scaled by how known the act is (<see cref="LpEarnedDiscovery.Known"/> on its release-awareness
/// bonus), so an act nobody knows starts from what its label paid for and is worth. Singles are untouched.
/// </summary>
public static class LpEarnedLaunch {
	public const string Switch = "--lp-earned-launch";
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }

	/// <summary>An LP's launch base. Returns <paramref name="baseAwareness"/> exactly when off.</summary>
	public static float Base(float baseAwareness, float actBonus) =>
		Enabled ? baseAwareness * LpEarnedDiscovery.Known(0f, actBonus) : baseAwareness;
}

/// <summary>
/// --lp-label-demand-parity (audit finding 9). The LP purchase rate is multiplied by a tier constant (Major 1.36 ... Small
/// 0.50, AlbumSimulator.GetLiveLabelDemandScale). --label-demand-parity removed the same double count for singles only;
/// with this switch the LP constant is moved toward 1 by the same parity share (LabelFootprintParity.Demand).
/// </summary>
public static class LpLabelDemandParity {
	public const string Switch = "--lp-label-demand-parity";
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }

	/// <summary>The LP tier scale. Returns <paramref name="tierScale"/> exactly when off.</summary>
	public static float Apply(float tierScale) => Enabled ? LabelFootprintParity.Demand(tierScale) : tierScale;
}

/// <summary>
/// --anniversary-opening-contracts (audit finding 4, author's version, 2026-11-15). A seeded deal is given a signing week
/// inside its signing year (keyed on the act's id, no random draw) and matures on that anniversary after its term: an act
/// signed in October 1957 to a three-year term comes due in October 1960. A deal whose term ran out before 1960 is on its
/// yearly option and comes due on its next anniversary inside 1960. Unlike --staggered-opening-contracts, a live deal is not
/// pulled a year earlier. Takes precedence over that switch when both are named.
/// </summary>
public static class AnniversaryOpeningContracts {
	public const string Switch = "--anniversary-opening-contracts";
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }

	/// <summary>The week of year (0-51) the act signed, from its id.</summary>
	public static int SigningWeek(string artistId) {
		uint hash = 2166136261u;
		foreach (char c in artistId ?? string.Empty) { hash ^= c; hash *= 16777619u; }
		hash ^= hash >> 13; hash *= 0x5bd1e995u; hash ^= hash >> 15;
		return (int)(hash % 52u);
	}

	/// <summary>The run week (1 or later) at which the seeded deal matures.</summary>
	public static int ExpiryWeek(string artistId, int signedYear, int termYears, int startYear) {
		int signed = (signedYear - startYear) * 52 + SigningWeek(artistId);
		if (signed > 0) signed -= 52; // "signed this year" at the opening means within the twelve months before it
		int expiry = signed + System.Math.Max(1, termYears) * 52;
		if (expiry < 1) expiry += ((1 - expiry + 51) / 52) * 52; // lapsed: the next option anniversary
		return expiry;
	}
}

/// <summary>
/// --renewal-live-evidence (audit §2.5). At contract expiry an AI label judged a first-contract act on counters that
/// credit a chart entry only when the record's chart run ends (SimulatedArtist.CompleteChartRun), so a single still
/// climbing counted as a flop. A deal also matured the week its singles obligation was delivered, before that last
/// single had done anything. With the switch on: (1) a record under this deal that is on the chart now, has charted, or
/// has broken out regionally counts as evidence and exempts the act from the flop rule; (2) an AI deal matures on its
/// date only (options came due on dates). Player contracts are untouched.
/// </summary>
public static class RenewalLiveEvidence {
	public const string Switch = "--renewal-live-evidence";
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }

	/// <summary>True when a record released under the current deal is charting, has charted, or has broken out.</summary>
	public static bool HasLiveEvidence(SimulatedArtist artist) {
		ChartManager chart = ChartManager.Instance;
		if (chart == null || artist?.releaseHistory == null) return false;
		int count = artist.releaseHistory.Count, take = Math.Min(Math.Max(0, artist.contractReleases), count);
		for (int i = count - take; i < count; i++) {
			RecordRuntimeData record = chart.GetRecordRuntimeData(artist.releaseHistory[i]);
			if (record != null && IsEvidence(record.currentPosition, record.peakPosition, record.regionalBreakoutCount)) return true;
		}
		return false;
	}

	public static bool IsEvidence(int currentPosition, int peakPosition, int regionalBreakouts) =>
		currentPosition > 0 || peakPosition is > 0 and <= 100 || regionalBreakouts > 0;

	/// <summary>The term clock alone (RosterManager.IsContractMatured without delivery).</summary>
	public static bool TermMatured(SimulatedArtist artist, int year, int currentWeek) =>
		artist != null && (artist.contractExpiresWeek > 0 ? currentWeek >= artist.contractExpiresWeek : artist.contractExpiresYear <= year);
}
