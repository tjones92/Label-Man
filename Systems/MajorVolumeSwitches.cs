using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Big-5 single volume (LpGenreAllocationFollowup.md §57), off unless named.
///
/// --major-gate-exemption: a Major-tier AI label's planned single is not refused by the single capital-risk test, and its
/// project review records the act's own session instead of holding for stronger material. Waiting for a commissioned or
/// licensed song that is already on its way still waits. History: the five majors issued ~1,080 singles in 1960
/// (LabelReleaseProgrammeImplementation.md), and most singles of the period lost money; the sim's Majors plan 16 a month
/// but these two gates expire 200-400 of their extra slots a year (§57). Cash, the all-in profit gate at issue, artist
/// spacing, material delivery and every other tier are unchanged. No random number is drawn.
///
/// --major-opening-roster=N: a Major-tier AI label opens 1960 with at least N signed acts. Its roster cap is raised to
/// N where lower, and its launch signing target becomes max(drawn target, N). The fill draw is still consumed, so the
/// draw stream keeps its shape; the extra signings come from the same launch pool. History (§57): ~70-90 releasing acts
/// per major, ~55-60 at the sim's issue scale; the sim's Majors open with ~32-40.
///
/// --major-slate-pool=N: a Major-tier AI label's ordinary single slot is filled from at most N of its eligible acts, not
/// from its whole eligible roster. The N are the lowest keyed rolls for that slot (a counter-based roll, nothing drawn from
/// the stream); acts with a Top 40 inside the hot window are always reviewed on top of the N. Scoring and the pick among the
/// reviewed acts are unchanged. Financed hit follow-up slots, LPs and every other tier are untouched. Purpose: the Big-5
/// choose each slot from 34-42 scored acts against 2.8-4.5 at small labels (MajorShareRootCauseHandoff.md hypothesis 1);
/// this kill test splits slate selection from supply in the Big-5 per-launch premium.
///
/// --major-newcomer-craft=C: when a Major-tier AI label cuts a single by an act that is still Unsigned or a NewSigning, the
/// label-craft term of the single's production reads min(label craft, C) instead of the label's craft. Established acts,
/// albums, LP tracks, the label's stored value and the slate's estimate are unchanged, and the session's draws are the
/// same. Purpose: Big-5 newcomer singles carry production .74-.75 against the field's .65-.66, mostly label craft (.79-.86
/// against .64-.66); history's ~1.2x Major premium suggests an unknown on a major got a routine staff session, while the
/// label's best producers worked its proven acts (§57 candidate 2). C = .65 is the field newcomers' mean label craft.
/// </summary>
public static class MajorVolumeSwitches {
	public const string GateSwitch = "--major-gate-exemption";
	public const string RosterSwitch = "--major-opening-roster=";
	public const string PoolSwitch = "--major-slate-pool=";
	public const string NewcomerCraftSwitch = "--major-newcomer-craft=";
	/// <summary>
	/// --major-catalog-veterans=N (Big5ChartOpportunityInvestigation.md, 2026-11-17): at the January 1960 opening-career
	/// pass, N acts on each AI Major's opening roster (keyed order, skipping acts that already hold an opening career) are
	/// stamped as catalog veterans: Established, a working career since 1946-55 with only occasional national placings.
	/// History: each major issued ~200 singles a year from a deep contract roster (LabelReleaseProgrammeImplementation.md),
	/// while the sim's majors had ~8 established releasing acts each and filled the slate with newcomers (66% of 1961 Big-5
	/// singles). No act is added; existing roster acts get a history. Draws are keyed on the act id.
	/// </summary>
	public const string CatalogVeteransSwitch = "--major-catalog-veterans=";
	/// <summary>
	/// --major-signing-quota=K: an AI Major makes at most K in-run signings (daily and weekly talent markets) per calendar
	/// year. Opening-roster allocation is not counted. Once the quota is reached the label has no vacancy until January.
	/// Purpose: the sim's majors signed 58 (1960) and 152 (1961) acts a seed, nearly all newcomers, against a historical
	/// roster that turned over slowly. Player-driven signings (rival offers, master buyers) are not gated.
	/// </summary>
	public const string SigningQuotaSwitch = "--major-signing-quota=";
	/// <summary>
	/// --major-catalog-veterans-quiet (second version, 2026-11-17): a catalog veteran's history no longer ends in a forced
	/// recent Top 40. Its last three sides missed or placed 61-100, and the two-flop cap is not applied, so the act starts
	/// with low recognition and the live ladder reads its real flop streak. Only the tail shaping changes; the keyed draws
	/// before it are the same. Needs --major-catalog-veterans.
	/// </summary>
	public const string QuietVeteransSwitch = "--major-catalog-veterans-quiet";
	/// <summary>
	/// --major-catalog-veterans-retained: a catalog veteran's opening contract runs at least through 1961 (expiry week
	/// 105 or later), and while that opening term runs the act cannot be dropped on performance and its contract does not
	/// mature early on delivered sides. Demotion on the ladder (Established to Declining) still happens. The protection
	/// ends at the opening expiry week; renewal is then the ordinary decision. Needs --major-catalog-veterans.
	/// </summary>
	public const string RetainedVeteransSwitch = "--major-catalog-veterans-retained";
	/// <summary>The first week a retained veteran's opening term may end: week 105 is the first week of 1962.</summary>
	public const int RetainedTermWeek = 105;
	public static bool QuietVeterans { get; private set; }
	public static bool RetainedVeterans { get; private set; }
	private static bool configured;
	public static int CatalogVeterans { get; private set; }
	/// <summary>The yearly in-run signing quota of an AI Major; -1 when off.</summary>
	public static int SigningQuota { get; private set; } = -1;
	private static readonly Dictionary<string, int> signingsThisYear = new(StringComparer.Ordinal);
	private static int quotaYear = int.MinValue;
	public static bool GateExemption { get; private set; }
	public static int OpeningRoster { get; private set; }
	public static int SlatePoolSize { get; private set; }
	/// <summary>The craft ceiling for a Major newcomer's single; 0 when off.</summary>
	public static float NewcomerCraftCap { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		GateExemption = args.Contains(GateSwitch);
		OpeningRoster = 0;
		var values = args.Where(a => a.StartsWith(RosterSwitch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + RosterSwitch.TrimEnd('=') + " once.");
		if (values.Length == 1) {
			if (!int.TryParse(values[0].Substring(RosterSwitch.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 200)
				throw new ArgumentException(RosterSwitch.TrimEnd('=') + " needs a whole number of acts in [1, 200].");
			OpeningRoster = n;
		}
		SlatePoolSize = 0;
		var pools = args.Where(a => a.StartsWith(PoolSwitch, StringComparison.Ordinal)).ToArray();
		if (pools.Length > 1) throw new ArgumentException("Name " + PoolSwitch.TrimEnd('=') + " once.");
		if (pools.Length == 1) {
			if (!int.TryParse(pools[0].Substring(PoolSwitch.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 200)
				throw new ArgumentException(PoolSwitch.TrimEnd('=') + " needs a whole number of acts in [1, 200].");
			SlatePoolSize = n;
		}
		NewcomerCraftCap = 0f;
		var crafts = args.Where(a => a.StartsWith(NewcomerCraftSwitch, StringComparison.Ordinal)).ToArray();
		if (crafts.Length > 1) throw new ArgumentException("Name " + NewcomerCraftSwitch.TrimEnd('=') + " once.");
		if (crafts.Length == 1) {
			if (!float.TryParse(crafts[0].Substring(NewcomerCraftSwitch.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float c) ||
				!float.IsFinite(c) || c < .3f || c > 1f)
				throw new ArgumentException(NewcomerCraftSwitch.TrimEnd('=') + " needs a craft level in [0.3, 1].");
			NewcomerCraftCap = c;
		}
		CatalogVeterans = 0;
		var veterans = args.Where(a => a.StartsWith(CatalogVeteransSwitch, StringComparison.Ordinal)).ToArray();
		if (veterans.Length > 1) throw new ArgumentException("Name " + CatalogVeteransSwitch.TrimEnd('=') + " once.");
		if (veterans.Length == 1) {
			if (!int.TryParse(veterans[0].Substring(CatalogVeteransSwitch.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 80)
				throw new ArgumentException(CatalogVeteransSwitch.TrimEnd('=') + " needs a whole number of acts in [1, 80].");
			CatalogVeterans = n;
		}
		SigningQuota = -1;
		var quotas = args.Where(a => a.StartsWith(SigningQuotaSwitch, StringComparison.Ordinal)).ToArray();
		if (quotas.Length > 1) throw new ArgumentException("Name " + SigningQuotaSwitch.TrimEnd('=') + " once.");
		if (quotas.Length == 1) {
			if (!int.TryParse(quotas[0].Substring(SigningQuotaSwitch.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) || k < 0 || k > 200)
				throw new ArgumentException(SigningQuotaSwitch.TrimEnd('=') + " needs a whole number of signings in [0, 200].");
			SigningQuota = k;
		}
		signingsThisYear.Clear(); quotaYear = int.MinValue;
		QuietVeterans = args.Contains(QuietVeteransSwitch);
		RetainedVeterans = args.Contains(RetainedVeteransSwitch);
		if ((QuietVeterans || RetainedVeterans) && CatalogVeterans <= 0)
			throw new ArgumentException(QuietVeteransSwitch + " and " + RetainedVeteransSwitch + " need " + CatalogVeteransSwitch + "N.");
		if (GateExemption || OpeningRoster > 0 || SlatePoolSize > 0 || NewcomerCraftCap > 0f || CatalogVeterans > 0 || SigningQuota >= 0)
			GD.Print($"MAJOR_VOLUME gateExemption={GateExemption} openingRoster={OpeningRoster} slatePool={SlatePoolSize} newcomerCraft={NewcomerCraftCap:F2} " +
				$"catalogVeterans={CatalogVeterans} quiet={QuietVeterans} retained={RetainedVeterans} signingQuota={SigningQuota}");
		configured = true;
	}

	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }
	internal static void SetForChecks(bool gate, int roster, int pool = 0, float newcomerCraft = 0f, bool quiet = false, bool retained = false) {
		configured = true; GateExemption = gate; OpeningRoster = roster; SlatePoolSize = pool; NewcomerCraftCap = newcomerCraft;
		QuietVeterans = quiet; RetainedVeterans = retained;
	}

	/// <summary>True when the quiet tail shapes catalog veterans' histories.</summary>
	public static bool QuietVeteranTail() { EnsureConfigured(); return QuietVeterans; }

	/// <summary>Under --major-catalog-veterans-retained, lengthens a just-stamped veteran's opening term to run at least
	/// through 1961 and records the week its protection ends. No-op when off.</summary>
	public static void RetainOpeningTerm(SimulatedArtist artist, int year) {
		EnsureConfigured();
		if (!RetainedVeterans || artist == null) return;
		if (artist.contractExpiresWeek < RetainedTermWeek) artist.contractExpiresWeek = RetainedTermWeek;
		artist.contractExpiresYear = Math.Max(artist.contractExpiresYear, year + (artist.contractExpiresWeek - 1) / 52);
		artist.catalogVeteranTermWeek = artist.contractExpiresWeek;
	}

	/// <summary>True while a retained catalog veteran is inside its opening term: no performance drop and no early
	/// maturity. False when off, for any other act, and from the opening expiry week on.</summary>
	public static bool RetainsVeteran(SimulatedArtist artist, int currentWeek) {
		EnsureConfigured();
		return RetainedVeterans && artist != null && artist.openingArchetype == OpeningCareerArchetype.CatalogVeteran &&
			artist.catalogVeteranTermWeek > 0 && currentWeek < artist.catalogVeteranTermWeek;
	}

	/// <summary><see cref="RetainsVeteran(SimulatedArtist, int)"/> at the current chart week.</summary>
	public static bool RetainsVeteran(SimulatedArtist artist) {
		EnsureConfigured();
		return RetainedVeterans && RetainsVeteran(artist, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
	}

	/// <summary>The label craft a single's production reads. <paramref name="craft"/> exactly, unless the switch is on and an
	/// AI Major cuts a single for an Unsigned or NewSigning act; then min(craft, C).</summary>
	public static float NewcomerCraft(float craft, ReleaseFormat format, AILabel label, SimulatedArtist artist) {
		EnsureConfigured();
		if (NewcomerCraftCap <= 0f || format != ReleaseFormat.Single || !IsAiMajor(label) || artist == null ||
			artist.careerState is not (CareerState.Unsigned or CareerState.NewSigning)) return craft;
		return Math.Min(craft, NewcomerCraftCap);
	}

	private static bool IsAiMajor(AILabel label) => label != null && !label.isPlayerOwned && label.tier == LabelTier.Major;

	/// <summary>How many opening-roster acts an AI label's catalog-veteran pass stamps; 0 when off or not an AI Major.</summary>
	public static int CatalogVeteransFor(AILabel label) {
		EnsureConfigured();
		return CatalogVeterans > 0 && IsAiMajor(label) ? CatalogVeterans : 0;
	}

	/// <summary>True when an AI Major has used its in-run signing quota for <paramref name="year"/>. False when off.</summary>
	public static bool SigningQuotaReached(AILabel label, int year) {
		EnsureConfigured();
		if (SigningQuota < 0 || !IsAiMajor(label)) return false;
		if (year != quotaYear) return SigningQuota == 0;
		return signingsThisYear.GetValueOrDefault(label.labelId) >= SigningQuota;
	}

	/// <summary>Counts an in-run talent-market signing toward the label's quota. No-op when off or not an AI Major.</summary>
	public static void NoteSigning(AILabel label, int year) {
		EnsureConfigured();
		if (SigningQuota < 0 || !IsAiMajor(label)) return;
		if (year != quotaYear) { signingsThisYear.Clear(); quotaYear = year; }
		signingsThisYear[label.labelId] = signingsThisYear.GetValueOrDefault(label.labelId) + 1;
	}

	/// <summary>True when a Major's single skips the capital-risk test and the material hold. False when off.</summary>
	public static bool Exempt(AILabel label) {
		EnsureConfigured();
		return GateExemption && IsAiMajor(label);
	}

	/// <summary>The launch signing target; <paramref name="drawn"/> unchanged when off or not a Major. Raises the roster cap
	/// to N where lower.</summary>
	public static int OpeningTarget(AILabel label, int drawn) {
		EnsureConfigured();
		if (OpeningRoster <= 0 || !IsAiMajor(label)) return drawn;
		if (label.maxRosterSize < OpeningRoster) label.maxRosterSize = OpeningRoster;
		return Math.Max(drawn, OpeningRoster);
	}

	/// <summary>True when this label's ordinary single slots are filled from a capped pool.</summary>
	public static bool SlatePoolCapped(AILabel label) {
		EnsureConfigured();
		return SlatePoolSize > 0 && IsAiMajor(label);
	}

	/// <summary>The acts a capped slot reviews, in their original order: every hot act, plus the <see cref="SlatePoolSize"/>
	/// other acts with the lowest keyed rolls (ties by id). The full list when no more than N acts are cold.</summary>
	public static List<T> SlatePool<T>(IReadOnlyList<T> acts, Func<T, bool> hot, Func<T, double> roll, Func<T, string> id) {
		var cold = acts.Where(a => !hot(a)).ToList();
		if (cold.Count <= SlatePoolSize) return acts.ToList();
		var keep = new HashSet<T>(cold.OrderBy(roll).ThenBy(id, StringComparer.Ordinal).Take(SlatePoolSize));
		return acts.Where(a => hot(a) || keep.Contains(a)).ToList();
	}
}
