using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

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
/// --lp-act-genre (genre follow-up §10). The genre-supply picker gives about a quarter of all projects a genre other than
/// the act's (CompetitorManager.ChooseEnabledGenreSupply), and an LP project is filed, demanded and charted under it: a
/// teen act's LP as Easy Listening, a country act's as Folk or Comedy. With the switch on an LP project takes the act's
/// identity genre (legacy-mapped for the year) once the format is decided, before its profit gate. Identity is where a
/// pivot lands, so a pivoted act's LPs follow the pivot. An LP's promo single is cut from its tracks and follows it;
/// other singles keep the picker's genre. The format decision and the plan's profits were still made on the picker's
/// genre. Off: never read.
/// </summary>
public static class LpActGenre {
	public const string Switch = "--lp-act-genre";
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }
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

/// <summary>
/// --renewal-earned-option (audit §2.5, the coin flip). After the career rules and the flop rule, an AI label renewed any
/// other act with probability artistLoyalty (0.28-0.78), whatever the act had earned it. With the switch on the option is
/// taken up when the deal paid: the label's realized net on records released under this deal (retired records banked in
/// <see cref="SimulatedArtist.contractRealizedNet"/>, live ones at net to date) plus a loyalty allowance of
/// artistLoyalty x the next advance must be at least zero. A loyal label carries a small loss; nobody carries a large one.
/// An act the label never released under the deal lapses. A deal with positive net is also exempt from the first-contract
/// flop rule: an off-chart act that paid the label is not dropped for chart position. No random draw.
/// </summary>
public static class RenewalEarnedOption {
	public const string Switch = "--renewal-earned-option";
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }

	public static bool Decide(float dealNet, int releasesUnderDeal, float loyalty, float nextAdvance) =>
		releasesUnderDeal > 0 && dealNet + Math.Clamp(loyalty, 0f, 1f) * Math.Max(0f, nextAdvance) >= 0f;

	/// <summary>Banked net of retired records under the deal plus live records' net to date.</summary>
	public static float DealNet(SimulatedArtist artist) {
		float net = artist.contractRealizedNet;
		ChartManager chart = ChartManager.Instance;
		if (chart == null || artist.releaseHistory == null) return net;
		int count = artist.releaseHistory.Count, take = Math.Min(Math.Max(0, artist.contractReleases), count);
		for (int i = count - take; i < count; i++) {
			RecordRuntimeData record = chart.GetRecordRuntimeData(artist.releaseHistory[i]);
			if (record != null) net += record.lifetimeLabelNet - record.SunkReleaseCost;
		}
		return net;
	}

	public static bool Renew(AILabel label, SimulatedArtist artist) =>
		Decide(DealNet(artist), artist.contractReleases, label.artistLoyalty, label.CalculateAdvanceOffer(artist));

	/// <summary>Banks a retired record's realized net against the act's current deal. Off: nothing.</summary>
	public static void OnRetired(RecordRuntimeData record, int currentWeek) {
		if (!Enabled || record?.baseRecord == null) return;
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId);
		if (artist == null || artist.labelId != record.baseRecord.labelId) return;
		if (currentWeek - Math.Max(0, record.weeksSinceRelease) < artist.contractNetStartWeek) return;
		artist.contractRealizedNet += record.lifetimeLabelNet - record.SunkReleaseCost;
	}

	/// <summary>A new or renewed deal starts with nothing banked. Off: nothing.</summary>
	public static void StartDeal(SimulatedArtist artist, int currentWeek) {
		if (!Enabled || artist == null) return;
		artist.contractRealizedNet = 0f;
		artist.contractNetStartWeek = currentWeek;
	}
}

/// <summary>
/// --lp-buyer-word-of-mouth=K (audit §2.4 follow-up). An LP's word of mouth targets appeal² x 0.62 + momentum x 0.25
/// (AlbumSimulator.UpdateAlbum) whether or not anyone has bought it, and it lifts both the purchase conversion
/// (0.72 + wom x 0.45) and regional awareness growth. With the switch on the appeal term is scaled by the LP's buyer reach,
/// units / (units + K): an LP nobody bought makes no talk, and one that has sold K copies makes half. Momentum (already
/// sales-driven) is untouched. Off: never read.
/// </summary>
public static class LpBuyerWordOfMouth {
	public const string Switch = "--lp-buyer-word-of-mouth";
	private static bool read;
	private static float halfReach;
	public static bool Enabled { get { Read(); return halfReach > 0f; } }
	private static void Read() {
		if (read) return;
		halfReach = GameyAuditArguments.Value(Switch, 1f, 10000000f) ?? 0f;
		read = true;
	}
	internal static void SetForChecks(float value) { read = true; halfReach = value; }

	public static float Reach(long unitsSold) {
		Read();
		if (halfReach <= 0f) return 1f;
		double units = Math.Max(0L, unitsSold);
		return (float)(units / (units + halfReach));
	}
}

/// <summary>
/// --lp-floor-probe=a,b,... (audit 2.14, diagnostic only, never for a candidate). Strong one-cause probes on why an LP by
/// an unknown act still sells. Each named cause is switched off for every LP with a roster act (artistless LPs untouched):
/// launch = the label's launch awareness (marketing + reputation x 0.1); push = weekly awareness growth from label push
/// (record x 0.03, regional x 0.018); tail = push ends after week 12 instead of running at 0.12 for life; tier = the LP
/// tier purchase constant is 1; wom = the 0.72 floor of the word-of-mouth conversion multiplier is 0. Off: never read.
/// </summary>
public static class LpFloorProbe {
	public const string Switch = "--lp-floor-probe=";
	private static readonly string[] Known = { "launch", "push", "tail", "tier", "wom" };
	private static bool read;
	private static System.Collections.Generic.HashSet<string> causes = new();
	private static void Read() {
		if (read) return;
		causes = new();
		foreach (string arg in SimulationArguments.Get())
			if (arg.StartsWith(Switch, StringComparison.Ordinal))
				foreach (string c in arg[Switch.Length..].Split(',', StringSplitOptions.RemoveEmptyEntries)) {
					if (Array.IndexOf(Known, c) < 0) throw new ArgumentException(Switch + " unknown cause " + c);
					causes.Add(c);
				}
		read = true;
	}
	internal static void SetForChecks(params string[] values) { read = true; causes = new(values); }
	public static bool Enabled { get { Read(); return causes.Count > 0; } }
	private static bool On(string cause, bool isAlbum, string artistId) {
		Read();
		return isAlbum && causes.Count > 0 && causes.Contains(cause) && !string.IsNullOrEmpty(artistId);
	}
	public static bool Launch(bool isAlbum, string artistId) => On("launch", isAlbum, artistId);
	public static bool Push(string artistId) => On("push", true, artistId);
	public static bool Tail(string artistId) => On("tail", true, artistId);
	public static bool Tier(string artistId) => On("tier", true, artistId);
	public static bool Wom(string artistId) => On("wom", true, artistId);
}

/// <summary>
/// --lp-launch-standing=K (audit 2.15). The label's launch awareness on an LP (marketing impact + reputation x 0.1) is the
/// main reason an unknown act's LP sells (2.14: removing it cuts unknown-act LPs 54%, hit acts 23%). With the switch on
/// that term is scaled by the act's durable standing, units / (units + K), where units is everything the act has sold:
/// the credited career total (<see cref="SimulatedArtist.totalUnitsSold"/>; opening careers do not add to it, but their
/// prewarmed live records count below)
/// plus its live records not yet credited. A debut LP by an act with nothing sold gets no label launch awareness. An LP
/// with no roster act is untouched. Read once at launch; no saved state. Off: never read.
/// </summary>
public static class LpLaunchStanding {
	public const string Switch = "--lp-launch-standing";
	private static bool read;
	private static float halfUnits;
	public static bool Enabled { get { Read(); return halfUnits > 0f; } }
	private static void Read() {
		if (read) return;
		halfUnits = GameyAuditArguments.Value(Switch, 1f, 10000000f) ?? 0f;
		read = true;
	}
	internal static void SetForChecks(float value) { read = true; halfUnits = value; }

	/// <summary>units / (units + K). 1 when off.</summary>
	public static float Standing(long units) {
		Read();
		if (halfUnits <= 0f) return 1f;
		double u = Math.Max(0L, units);
		return (float)(u / (u + halfUnits));
	}

	/// <summary>The act's units to date: credited career units plus its uncredited live records.</summary>
	public static long ActUnits(SimulatedArtist artist) =>
		artist == null ? 0L : Math.Max(0L, (long)artist.totalUnitsSold) + (ChartManager.Instance?.UncreditedLiveUnits(artist.artistId) ?? 0L);

	/// <summary>The launch multiplier for an LP by <paramref name="artist"/>. 1 when off or when there is no act.</summary>
	public static float Factor(SimulatedArtist artist) => !Enabled || artist == null ? 1f : Standing(ActUnits(artist));
}

/// <summary>
/// --lp-reception-sigma=S (audit 2.17). The RIAA 1965 survey (2.16) puts the log-spread of LP sales at about 2.0
/// (half of 1,603 popular LPs under 4k, 0.4% over 1M); the model's is about 1.0 within a tier, because an LP's demand is
/// near-fixed by its act, genre and label. Glover's own heading for the data is "Producing and Selling Records Is a Risky
/// Business": dealers and buyers took to some titles and not others. With the switch on every LP with a roster act gets a
/// reception multiplier on store conversion, exp(S z - S^2 / 2) with z standard normal, so the mean multiplier is 1 and the
/// median falls by exp(S^2 / 2). z is keyed on the record id and the world seed (PhiloxCounterRandom), so it consumes no
/// RNG stream, needs no saved state and is the same after a resume. The labels' forecasts do not see it. Off: never read.
/// </summary>
public static class LpReception {
	public const string Switch = "--lp-reception-sigma";
	private const uint Decision = 0x4C505200U; // "LPR\0"
	private static bool read;
	private static float sigma;
	public static bool Enabled { get { Read(); return sigma > 0f; } }
	private static void Read() {
		if (read) return;
		sigma = GameyAuditArguments.Value(Switch, 0.01f, 3f) ?? 0f;
		read = true;
	}
	internal static void SetForChecks(float value) { read = true; sigma = value; }

	/// <summary>The standard normal draw for a record (Box-Muller on two keyed units).</summary>
	internal static double Z(ulong seed, string recordId) {
		double u1 = PhiloxCounterRandom.Unit(seed, recordId, "lp-reception", 0, Decision);
		double u2 = PhiloxCounterRandom.Unit(seed, recordId, "lp-reception", 1, Decision);
		return Math.Sqrt(-2.0 * Math.Log(Math.Max(u1, 1e-300))) * Math.Cos(2.0 * Math.PI * u2);
	}

	internal static float Multiplier(double z) => (float)Math.Exp(sigma * z - sigma * (double)sigma / 2.0);

	/// <summary>The record's reception multiplier, cached on the record. 1 when off or for an LP with no roster act.</summary>
	public static float Factor(RecordRuntimeData record) {
		Read();
		if (sigma <= 0f || record?.baseRecord == null || string.IsNullOrEmpty(record.baseRecord.artistId)
			|| string.IsNullOrEmpty(record.baseRecord.recordId)) return 1f;
		if (float.IsNaN(record.lpReception))
			record.lpReception = Multiplier(Z(RegionalSalesRandom.SavedSeedOrWorld, record.baseRecord.recordId));
		return record.lpReception;
	}
}

/// <summary>
/// --lp-label-standing=push,tier (audit 2.17). After --lp-launch-standing the rest of an unknown act's LP floor on a Major
/// is label-side: campaign push growing awareness (record x 0.03, regional x 0.018; 2.14: -24% for unknown acts) and the
/// tier purchase constant (Major 1.36). The RIAA 1965 survey (2.16) has half of a mostly-major sample under 4k copies;
/// the model's Major LPs have none. With the switch on, an act's LP carries the act's standing from launch
/// (<see cref="LpLaunchStanding.Factor"/>, same K) and: push = label push x standing; tier = 1 + (tier - 1) x standing for
/// a tier constant above 1 (constants below 1 are untouched, so small labels' unknowns are not lifted). An LP with no
/// roster act is untouched. Requires --lp-launch-standing. The standing is stored on the record (saved). Off: never read.
/// </summary>
public static class LpLabelStanding {
	public const string Switch = "--lp-label-standing=";
	private static readonly string[] Known = { "push", "tier" };
	private static bool read, push, tier;
	private static void Read() {
		if (read) return;
		push = tier = false;
		foreach (string arg in SimulationArguments.Get())
			if (arg.StartsWith(Switch, StringComparison.Ordinal))
				foreach (string c in arg[Switch.Length..].Split(',', StringSplitOptions.RemoveEmptyEntries)) {
					if (Array.IndexOf(Known, c) < 0) throw new ArgumentException(Switch + " unknown part " + c);
					if (c == "push") push = true; else tier = true;
				}
		if ((push || tier) && !LpLaunchStanding.Enabled) throw new ArgumentException(Switch + " needs " + LpLaunchStanding.Switch + "=K.");
		read = true;
	}
	internal static void SetForChecks(bool pushOn, bool tierOn) { read = true; push = pushOn; tier = tierOn; }
	public static bool Enabled { get { Read(); return push || tier; } }

	/// <summary>Stores the act's standing on an LP at launch. Does nothing when off, for a single, or for an LP with no act.</summary>
	public static void Stamp(RecordRuntimeData record, bool isAlbum, SimulatedArtist artist) {
		if (!Enabled || record == null || !isAlbum || artist == null) return;
		record.lpLabelStanding = LpLaunchStanding.Factor(artist);
	}

	/// <summary>Multiplier on the label's push growth. 1 when off or unstamped.</summary>
	public static float Push(RecordRuntimeData record) {
		Read();
		return push && record?.lpLabelStanding is float s ? s : 1f;
	}

	/// <summary>The tier purchase constant for this LP: its premium above 1 scaled by standing. Unchanged when off or unstamped.</summary>
	public static float Tier(RecordRuntimeData record, float tierScale) {
		Read();
		return tier && tierScale > 1f && record?.lpLabelStanding is float s ? 1f + (tierScale - 1f) * s : tierScale;
	}
}

/// <summary>
/// --hit-package-demand=K (LpGenreAllocationFollowup.md §10). A greatest-hits package sells to the people who bought the act's hit
/// singles; in the model it sold only to its genre's album buyers, like any LP, so youth packages reached the weekly top
/// 10 and dropped off in three weeks (half the units of an adult package). With the switch on, a Compilation's store buyer
/// pool in each region gains K x the buyers of the released singles it carries, split by the region's share of population.
/// A single's buyers are read from its chart peak on the track snapshot (<see cref="SingleBuyers"/>, the 1960-61 median
/// lifetime units by peak in cbg-v2b), so pre-1960 opening masters, which carry only a peak, count like live singles. The
/// added buyers go through the same awareness, conversion and exhaustion as the genre's buyers. The labels' forecasts do
/// not see it. Off: never read.
/// <para>--hit-package-demand-youth: the added buyers are also scaled by the package genre's Youth segment weight
/// (GenreSegmentRouting; TeenPop 0.71, Rock and Roll 0.58, TradPop 0.15, Jazz 0). An adult act's single buyers are mostly
/// the adults already in its genre's album pool, so adding all of them counts them twice; a teen act's are mostly teenagers
/// in no album pool. Billboard's 1961 mono year-end top 50 has six hit collections, four of them youth. Needs
/// --hit-package-demand=K. Off: never read.</para>
/// <para>--hit-package-demand-studio=S: a studio LP (any format but Compilation and Soundtrack) that carries released
/// singles also gains S x K x their buyers (with the youth weighting when named). A star's studio LP led by its current
/// hit sold to that hit's buyers: Billboard's 1961 mono top 50 has Ricky Nelson's Rick Is 21 and two Ray Charles LPs. An
/// LP with no released singles on it gains nothing. Needs --hit-package-demand=K. Off: never read.</para>
/// <para>--hit-package-decay=D: a Compilation's weekly catalogue decay after week 8 is D instead of the normal-LP rate
/// (hit collections such as Encore of Golden Hits and Johnny's Greatest Hits were long chart runners). Off: never read.</para>
/// </summary>
public static class HitPackageDemand {
	public const string Switch = "--hit-package-demand", DecaySwitch = "--hit-package-decay", YouthSwitch = "--hit-package-demand-youth",
		StudioSwitch = "--hit-package-demand-studio";
	private static bool read, youth;
	private static float share, decay, studio;
	private static double totalPopulation;
	private static void Read() {
		if (read) return;
		share = GameyAuditArguments.Value(Switch, 0f, 5f) ?? 0f;
		decay = GameyAuditArguments.Value(DecaySwitch, 0.5f, 1f) ?? 0f;
		youth = GameyAuditArguments.Has(YouthSwitch);
		studio = GameyAuditArguments.Value(StudioSwitch, 0f, 1f) ?? 0f;
		if ((youth || studio > 0f) && share <= 0f) throw new ArgumentException(YouthSwitch + " and " + StudioSwitch + " need " + Switch + "=K.");
		read = true;
	}
	internal static void SetForChecks(float k, float d, bool youthWeighted = false, float studioShare = 0f) {
		read = true; share = k; decay = d; youth = youthWeighted; studio = studioShare;
	}
	public static bool YouthWeighted { get { Read(); return youth; } }

	/// <summary>The genre's Youth segment weight (legacy-mapped for the year); 0 for a genre with no profile.</summary>
	public static float YouthWeight(Genre genre, int year) =>
		GenreCatalog.TryGet(GenreCatalog.MapLegacy(genre, year), out GenreProfile profile)
		&& profile.SegmentWeights.TryGetValue("Youth", out float weight) ? weight : 0f;
	public static bool Enabled { get { Read(); return share > 0f; } }
	public static bool DecayEnabled { get { Read(); return decay > 0f; } }
	public static float Decay { get { Read(); return decay; } }

	// (peak, median lifetime single units): cbg-v2b seeds 2002/1001, singles launched 1960-61, bucket midpoints.
	private static readonly (float Peak, float Units)[] Anchors =
		{ (1, 878000), (3, 608000), (8, 405000), (15, 295000), (30, 165000), (55, 74000), (85, 31000) };

	/// <summary>Buyers of a single that peaked at <paramref name="peak"/> (log-log interpolation). 0 for an uncharted side.</summary>
	public static float SingleBuyers(int peak) {
		if (peak < 1 || peak > 100) return 0f;
		if (peak <= Anchors[0].Peak) return Anchors[0].Units;
		int i = 1;
		while (i < Anchors.Length - 1 && peak > Anchors[i].Peak) i++;
		var (p0, u0) = Anchors[i - 1];
		var (p1, u1) = Anchors[i];
		double t = (Math.Log(peak) - Math.Log(p0)) / (Math.Log(p1) - Math.Log(p0));
		return (float)Math.Exp(Math.Log(u0) + t * (Math.Log(u1) - Math.Log(u0)));
	}

	/// <summary>National hit buyers a package carries: the sum over its released-single tracks.</summary>
	public static float PackageBuyers(Album album) {
		if (album?.trackRefs == null) return 0f;
		float sum = 0f;
		foreach (AlbumTrack track in album.trackRefs)
			if (track != null && track.isReleasedSingle) sum += SingleBuyers(track.peakPosition);
		return sum;
	}

	/// <summary>Buyers added to a region's pool for this LP. 0 when off, for a soundtrack, and for a studio LP unless
	/// --hit-package-demand-studio is named.</summary>
	public static float RegionalBuyers(Album album, MarketRegion region, Genre genre, int year) {
		if (!Enabled || album == null || region == null || album.albumFormat == AlbumFormat.Soundtrack) return 0f;
		float scope = album.albumFormat == AlbumFormat.Compilation ? 1f : studio;
		if (scope <= 0f) return 0f;
		if (totalPopulation <= 0) {
			var regions = ChartManager.Instance?.GetAllRegions();
			if (regions != null) foreach (MarketRegion r in regions) totalPopulation += Math.Max(0f, r?.population ?? 0f);
			if (totalPopulation <= 0) return 0f;
		}
		float k = scope * (youth ? share * YouthWeight(genre, year) : share);
		return (float)(k * PackageBuyers(album) * Math.Max(0f, region.population) / totalPopulation);
	}

	internal static void SetPopulationForChecks(double total) => totalPopulation = total;
}

/// <summary>
/// --album-chart-reporting=Country=0.5,... (LpGenreAllocationFollowup.md §11). The album chart ranks LPs on store units,
/// so a country star's LP that sells like a pop LP charts like one. Billboard's pop LP chart was compiled from pop
/// dealers' reports; country LPs sold largely through country outlets, and Billboard's 1961 mono year-end top 50 has one
/// country title (Johnny Horton's Greatest Hits). With the switch on, a named genre's chart points are multiplied by its
/// weight. Units, sales and money are untouched; chart position still feeds awareness. Soundtracks keep weight 1 whatever
/// their mapped genre. Off: never read.
/// </summary>
public static class AlbumChartReporting {
	public const string Switch = "--album-chart-reporting=";
	private static Dictionary<Genre, float> weights;
	private static Dictionary<Genre, float> Weights => weights ??= GenreCatalog.ParseProbe(Switch, false).ToDictionary(p => p.genre, p => p.value);
	public static bool Enabled => Weights.Count > 0;
	internal static void SetForChecks(Dictionary<Genre, float> value) => weights = value ?? new();

	/// <summary>The chart-point weight for an LP. 1 when off, for a soundtrack, or for an unnamed genre.</summary>
	public static float Weight(Genre genre, AlbumFormat format) =>
		format == AlbumFormat.Soundtrack || !Weights.TryGetValue(GenreCatalog.MapLegacy(genre), out float w) ? 1f : w;
}

/// <summary>
/// --soundtrack-pool-breadth=F (LpGenreAllocationFollowup.md §11). A soundtrack sells to an adult and family pool of
/// about 440k buyers nationally, and its rack sales (1.4-2.4x its store sales in weeks 4-26) spend the same pool, so a
/// top-10 soundtrack is 22% exhausted by week 12 and charts a median 10-12 weeks against the 40-60 the soundtrack design
/// targets. Billboard's 1961 top 50 is half cast and film albums released in 1956-60. With the switch on a soundtrack's
/// buyer pool is F times wider and each buyer's weekly purchase rate is divided by F: the launch week sells the same,
/// and the pool exhausts F times more slowly. Off: never read.
/// </summary>
public static class SoundtrackPoolBreadth {
	public const string Switch = "--soundtrack-pool-breadth";
	private static bool read;
	private static float breadth;
	private static void Read() { if (!read) { breadth = GameyAuditArguments.Value(Switch, 1f, 20f) ?? 0f; read = true; } }
	internal static void SetForChecks(float value) { read = true; breadth = value; }
	public static bool Enabled { get { Read(); return breadth > 1f; } }
	public static float Breadth { get { Read(); return breadth > 1f ? breadth : 1f; } }
}

/// <summary>
/// --soundtrack-hit-tail=R (LpGenreAllocationFollowup.md §12). A hit film or show kept finding new audiences for a year
/// or more (roadshow runs, touring companies), and its cast or film album sold to each new wave of them, so Billboard's
/// 1961 top 50 carries about ten points of 1960 soundtracks. In the model a soundtrack's buyer pool is fixed and
/// exhaustion ends a top-10 soundtrack's run before its box-office decay does. With the switch on, a soundtrack that has
/// peaked in the album top 10 has an audience that renews: after week <see cref="StartWeek"/> its exhaustion reads its
/// penetration divided by 1 + R per week, so its box-office trajectory, not a spent pool, sets how long it lasts.
/// Weekly demand size is untouched, and soundtracks that never reached the top 10 are untouched. Off: never read.
/// </summary>
public static class SoundtrackHitTail {
	public const string Switch = "--soundtrack-hit-tail";
	public const int StartWeek = 12;
	public const int DefaultHitPeak = 10;
	// --soundtrack-hit-tail-peak=N (§25): the album chart peak that counts as a hit. On a trailing-blend chart
	// (--album-chart-blend) few new soundtracks peak in the top 10, so the bar moves down the chart. Default 10.
	public const string PeakSwitch = "--soundtrack-hit-tail-peak";
	private static bool read;
	private static float rate;
	private static int hitPeak;
	private static void Read() {
		if (read) return;
		rate = GameyAuditArguments.Value(Switch, 0f, 1f) ?? 0f;
		hitPeak = (int)(GameyAuditArguments.Value(PeakSwitch, 1f, 200f) ?? DefaultHitPeak);
		read = true;
	}
	internal static void SetForChecks(float value, int peak = DefaultHitPeak) { read = true; rate = value; hitPeak = peak; }
	public static int HitPeak { get { Read(); return hitPeak; } }
	public static bool Enabled { get { Read(); return rate > 0f; } }
	public static float Rate { get { Read(); return rate; } }

	/// <summary>The divisor on a title's penetration: 1 when off, for a non-soundtrack, or for a soundtrack that has
	/// not peaked at <see cref="HitPeak"/> or better.</summary>
	public static float Renewal(RecordRuntimeData record) {
		if (!Enabled || record == null) return 1f;
		Album album = record.baseRecord?.album;
		if (album?.albumFormat != AlbumFormat.Soundtrack || album.externalMedia == null) return 1f;
		if (record.peakPosition <= 0 || record.peakPosition > HitPeak) return 1f;
		return 1f + Rate * Math.Max(0, record.weeksSinceRelease - StartWeek);
	}
}

/// <summary>
/// --opening-soundtrack-catalogue=N (LpGenreAllocationFollowup.md §13). About ten of the 24 soundtrack points in
/// Billboard's 1961 top 50 are cast and film albums from 1956-59 (South Pacific, My Fair Lady, The Sound of Music,
/// Ben-Hur), and the opening world seeds no soundtracks at all. With the switch on, the first live week mints N
/// soundtracks that were top-10 hits released <see cref="MinAgeWeeks"/>-<see cref="MaxAgeWeeks"/> weeks before the
/// start: high box office, a top-10 peak, a long chart history, regional awareness at their premiere buzz, and part of
/// their buyer pool already spent. No licence fee is charged (it was paid before 1960). Meant to run with
/// --soundtrack-hit-tail, which keeps a top-10 soundtrack's audience renewing; without it they are mostly spent. Off:
/// never read.
/// </summary>
public static class OpeningSoundtrackCatalogue {
	public const string Switch = "--opening-soundtrack-catalogue";
	public const int MinAgeWeeks = 26, MaxAgeWeeks = 210;
	/// <summary>Share of each region's soundtrack pool the title has already sold.</summary>
	public const float SpentPool = 0.5f;
	private static bool read;
	private static int count;
	private static void Read() { if (!read) { count = (int)(GameyAuditArguments.Value(Switch, 0f, 40f) ?? 0f); read = true; } }
	internal static void SetForChecks(int value) { read = true; count = value; }
	public static bool Enabled { get { Read(); return count > 0; } }
	public static int Count { get { Read(); return count; } }
}

/// <summary>
/// --soundtrack-origination-early=F (LpGenreAllocationFollowup.md §14). Billboard's 1960 year-end top 50 holds 8
/// soundtracks, 6-7 of them released before 1960 and 1-2 in 1960; the model's 1960 top 50 holds 5-10 soundtracks
/// released in 1960 in every run so far. With --opening-soundtrack-catalogue supplying the old titles, the weekly
/// origination rate (28 a year in 1960, ExternalMediaService.OriginationsForYear) double counts the early market. With the
/// switch on, originations in years before <see cref="EndYear"/> are × F; later years are untouched. Off: never read.
/// </summary>
public static class SoundtrackOriginationEarly {
	public const string Switch = "--soundtrack-origination-early";
	public const int EndYear = 1962;
	private static bool read;
	private static float factor;
	private static void Read() { if (!read) { factor = GameyAuditArguments.Value(Switch, 0f, 1f) ?? 1f; read = true; } }
	internal static void SetForChecks(float value) { read = true; factor = value; }
	public static bool Enabled { get { Read(); return factor < 1f; } }
	/// <summary>The origination multiplier for a year: F before <see cref="EndYear"/> when on, else 1.</summary>
	public static float Scale(int year) => Enabled && year < EndYear ? factor : 1f;
}

/// <summary>
/// --soundtrack-launch-awareness=k (LpGenreAllocationFollowup.md §15). A new soundtrack launches with awareness at its
/// film or show's premiere buzz (sourcePopularity, about 0.3-0.8), against 0.06-0.20 for an ordinary LP, so 10-18 new
/// soundtracks a year reach the album top 10 against Billboard's 1-2 in 1960. With the switch on the launch seed is
/// premiere buzz × k. Opening-catalogue titles are untouched. Off: never read.
/// </summary>
public static class SoundtrackLaunchAwareness {
	public const string Switch = "--soundtrack-launch-awareness";
	private static bool read;
	private static float scale;
	private static void Read() { if (!read) { scale = GameyAuditArguments.Value(Switch, 0f, 1f) ?? 1f; read = true; } }
	internal static void SetForChecks(float value) { read = true; scale = value; }
	public static bool Enabled { get { Read(); return scale < 1f; } }
	public static float Scale { get { Read(); return scale; } }
}

/// <summary>
/// --soundtrack-format-tilt=F (LpGenreAllocationFollowup.md §18). A soundtrack's buyer pool is drawn from the adult and
/// family segments regardless of genre (MarketRegion.GetSoundtrackAlbumMarketSize), but its store conversion still
/// carries its mapped genre's album format tilt (Classical 1.72 for 60% of film scores, Comedy 1.42), so new 1960
/// soundtracks reach the top 10 at 1.8× an ordinary LP's tilt (§17). With the switch on, a soundtrack with a media
/// profile takes tilt F in place of its genre's. Off: never read.
/// </summary>
public static class SoundtrackFormatTilt {
	public const string Switch = "--soundtrack-format-tilt";
	private static bool read;
	private static float tilt;
	private static void Read() { if (!read) { tilt = GameyAuditArguments.Value(Switch, 0.1f, 3f) ?? 0f; read = true; } }
	internal static void SetForChecks(float value) { read = true; tilt = value; }
	public static bool Enabled { get { Read(); return tilt > 0f; } }
	public static float Tilt { get { Read(); return tilt; } }
}

/// <summary>
/// --soundtrack-origination-season=S (LpGenreAllocationFollowup.md §21). Billboard's 1960 releases in the 1961 top 50
/// (Exodus, Camelot, Never on Sunday) came out in the last quarter of 1960, the Broadway season and the holiday and
/// roadshow film releases, and peaked the next year; the model originates soundtracks evenly through the year. With the
/// switch on the weekly origination rate is × 1 + S·cos(2π(month − 11) / 12): a November peak, a May trough, mean 1
/// over the year. The weekly chance caps at one origination, so a large S loses a little volume in the peak months.
/// Off: never read.
/// </summary>
public static class SoundtrackOriginationSeason {
	public const string Switch = "--soundtrack-origination-season";
	public const int PeakMonth = 11;
	private static bool read;
	private static float strength;
	private static void Read() { if (!read) { strength = GameyAuditArguments.Value(Switch, 0f, 1f) ?? 0f; read = true; } }
	internal static void SetForChecks(float value) { read = true; strength = value; }
	public static bool Enabled { get { Read(); return strength > 0f; } }
	public static float Weight(int month) =>
		Enabled ? 1f + strength * MathF.Cos(2f * MathF.PI * (month - PeakMonth) / 12f) : 1f;
}

/// <summary>
/// --soundtrack-build-weeks=B (LpGenreAllocationFollowup.md §21). A cast album or roadshow film score built its sales
/// for months as the show or picture found its audience (The Sound of Music, Nov 1959, reached #1 in Jan 1960); the
/// model's box-office multiplier starts at its peak (a 3-week ramp from 0.85). With the switch on, a soundtrack's
/// box-office multiplier is × 0.35 + 0.65·(weeks / B) until week B. Opening-catalogue titles (26+ weeks old) are past
/// any B up to 26. Off: never read.
/// </summary>
public static class SoundtrackBuild {
	public const string Switch = "--soundtrack-build-weeks";
	public const float StartShare = 0.35f;
	private static bool read;
	private static float weeks;
	private static void Read() { if (!read) { weeks = GameyAuditArguments.Value(Switch, 0f, 26f) ?? 0f; read = true; } }
	internal static void SetForChecks(float value) { read = true; weeks = value; }
	public static bool Enabled { get { Read(); return weeks > 0f; } }
	public static float Factor(float weeksSinceRelease) =>
		!Enabled || weeksSinceRelease >= weeks ? 1f : StartShare + (1f - StartShare) * Math.Max(0f, weeksSinceRelease) / weeks;
}

/// <summary>
/// --album-chart-blend=L (LpGenreAllocationFollowup.md §24). The album chart ranks this week's store units, and LP demand
/// peaks at launch (the follower book is spent over three weeks), so model LPs debut at their peak inside the top half and
/// fall: 92-223 top-10 debuts a year and 23-35 No. 1s against Billboard's ~0 and 10-12. Billboard's LP chart was compiled
/// from dealer reports; its LPs entered at the bottom (#105-148 of 150 in 1962) and climbed. With the switch on an LP's
/// chart points are a trailing blend, points = L x this week's points + (1 - L) x last week's blend, so a launch spike
/// enters low and a steady seller climbs to its level over about 1/L weeks. An LP first seen at <see cref="SeedAgeWeeks"/>
/// weeks or older (the opening catalogue, opening soundtracks) is seeded at its own points; anything younger starts at 0.
/// Units, sales and money are untouched; chart position still feeds awareness and career reads. The blend is runtime state and is not saved (a probe for headless runs). Off: never read.
/// </summary>
public static class AlbumChartBlend {
	public const string Switch = "--album-chart-blend";
	private static bool read;
	public const int SeedAgeWeeks = 8;
	private static float lambda;
	private static void Read() { if (!read) { lambda = GameyAuditArguments.Value(Switch, 0.05f, 1f) ?? 1f; read = true; } }
	internal static void SetForChecks(float value) { read = true; lambda = value; }
	public static bool Enabled { get { Read(); return lambda < 1f; } }
	/// <summary>--album-chart-blend-floors-raw (§26): the album demand's chart reads (the top-10 and top-40 awareness
	/// floors and chart discovery, <see cref="AlbumSimulator"/>) use last week's unblended sales rank instead of the
	/// published blended position, so the blend changes what the chart shows but not who gets chart exposure at launch.
	/// Rack access and career reads keep the published chart. Read only with the blend on.</summary>
	public const string FloorsRawSwitch = "--album-chart-blend-floors-raw";
	private static bool? floorsRaw;
	public static bool FloorsRaw => Enabled && (floorsRaw ??= GameyAuditArguments.Has(FloorsRawSwitch));
	internal static void SetFloorsRawForChecks(bool value) => floorsRaw = value;
	/// <summary>The chart position album demand reads: the raw sales rank under the raw-floors switch, else the chart.</summary>
	public static int DemandPosition(RecordRuntimeData record) => FloorsRaw ? record.albumRawRank : record.currentPosition;
	/// <summary>True when an LP's first blend should start at its own points (it was already old when first seen).</summary>
	public static bool Seeds(bool seen, int weeksSinceRelease) => !seen && weeksSinceRelease >= SeedAgeWeeks;
	/// <summary>The next blended chart points. <paramref name="seed"/> starts the blend at this week's points.</summary>
	public static float Next(float previous, float points, bool seed) {
		Read();
		return seed || lambda >= 1f ? points : lambda * points + (1f - lambda) * previous;
	}
}
