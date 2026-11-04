using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

/// <summary>
/// The one consumer-demand adapter of MediaAtmosphereDirective §12.3, off unless named.
/// <para>
/// Television that played the record itself (<see cref="PerformedSong.Record"/>) leaves each region a small signed
/// request on that title: <see cref="Gain"/> × the share of the region's people who newly saw it × a performance
/// factor, carried at <see cref="WeeklyCarry"/> a week after. In <c>ChartManager</c>'s singles clearing, after
/// serviceable intent and physical limits are known and before format budgets, <see cref="Allocate"/> moves intent
/// between titles of the same region under the directive's limits: the regional total is kept exactly, no title moves
/// more than 5% of its intent (10% for an act in a national fan sensation, replacing the 5%), no title passes its
/// physical limit, at most 1% of the region's intent moves, a title with no intent stays at zero, and a title is a donor
/// or a recipient, never both. Unmatched requests are cancelled. Integer rounding is stable and caps hold after it.
/// </para>
/// <list type="bullet">
/// <item><c>--observe-media-demand</c> (shadow): requests and allocations are computed and logged; clearing reads the
/// control intent, so every economy CSV must match the control.</item>
/// <item><c>--media-demand</c> (causal, requires <see cref="MediaAtmosphere.CausalSwitch"/>): clearing reads the
/// adjusted intent.</item>
/// </list>
/// Singles only; albums get no media multiplier. Draws no RNG.
/// </summary>
public static class MediaDemand {
	public const string ShadowSwitch = "--observe-media-demand";
	public const string CausalSwitch = "--media-demand";

	/// <summary>Request per unit of a region's population newly reached. A first Sullivan spot (≈14% of a region) asks for
	/// about 5%, the routine ceiling; a talk spot (≈4%) about 1.4%; a local show in its own region a fraction of a percent.
	/// Initial design value, sized against the caps, not fitted.</summary>
	public const float Gain = 0.35f;
	public const float RoutineCap = 0.05f;
	public const float FrenzyCap = 0.10f;
	/// <summary>Half the summed absolute change may be at most this share of the region's singles intent.</summary>
	public const float RegionTransferShare = 0.01f;
	/// <summary>A request's weekly carry after the week it first applies.</summary>
	public const float WeeklyCarry = 0.5f;
	public const float RequestFloor = 1e-4f;

	/// <summary>Diagnostic (<c>--media-demand-permute</c>, causal only): each region's requests go to unrelated titles in a
	/// keyed order, so the same volume moves without television choosing who gets it. Separates targeting from reshuffle.</summary>
	public const string PermuteSwitch = "--media-demand-permute";
	public static bool Permute { get; private set; }

	/// <summary>Request source (requires shadow or causal demand): teen fan and national mainstream magazine items ask for
	/// the record they are about (<c>MediaAtmosphereService.DemandPressWeek</c>).</summary>
	public const string PressSwitch = "--media-demand-press";
	/// <summary>Request source (requires shadow or causal demand): a public controversy asks for less of the act's booked
	/// record, and a major one qualifies the title for the 10% ceiling (<c>MediaAtmosphereService.DemandControversy</c>).</summary>
	public const string ControversySwitch = "--media-demand-controversy";
	public static bool PressSource { get; private set; }
	public static bool ControversySource { get; private set; }

	/// <summary>Request per national reader of a feature, a fraction of the television <see cref="Gain"/>: readers see no
	/// performance. A Life feature (20M readers) asks for about 1.3%; a teen magazine feature (1.5M) about 0.1%; news items a
	/// fifth of that. Initial design value, sized against the caps, not fitted.</summary>
	public const float PressGain = 0.10f;
	/// <summary>A controversy's request per unit of severity, negative: a national on-air defiance (severity 0.6) asks for
	/// about −7%, bound by the 10% major-controversy ceiling. Initial design value, not fitted.</summary>
	public const float ControversyGain = 0.12f;
	/// <summary>A national controversy at this severity or more is the directive's "major controversy" (§12.3).</summary>
	public const float MajorControversySeverity = 0.5f;

	private static bool configured, shadow, causal;
	public static bool Shadow => shadow && !causal && MediaAtmosphere.Observing;
	public static bool Causal => causal && MediaAtmosphere.Causal;
	public static bool Active => Shadow || Causal;

	/// <summary>Per-title transfer rows, opened by the audit runner when the adapter is active (§15.3). Print-only.</summary>
	public static TextWriter Log;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == ShadowSwitch) shadow = true;
			if (argument == CausalSwitch) causal = true;
			if (argument == PermuteSwitch) Permute = true;
			if (argument == PressSwitch) PressSource = true;
			if (argument == ControversySwitch) ControversySource = true;
		}
		if ((PressSource || ControversySource) && !shadow && !causal)
			throw new ArgumentException($"{PressSwitch} and {ControversySwitch} are request sources of {CausalSwitch} or {ShadowSwitch}.");
		if (Permute && !causal) throw new ArgumentException($"{PermuteSwitch} is a diagnostic of {CausalSwitch}.");
		if (shadow && causal)
			throw new ArgumentException($"{CausalSwitch} and {ShadowSwitch} cannot be used together; the causal run logs the same rows.");
		if (causal && !MediaAtmosphere.Causal)
			throw new ArgumentException($"{CausalSwitch} is an economic writer; it requires {MediaAtmosphere.CausalSwitch}.");
		configured = true;
	}

	internal static void SetForChecks(bool shadowOn, bool causalOn) { shadow = shadowOn; causal = causalOn; configured = true; }
	internal static void SetSourcesForChecks(bool press, bool controversy) { PressSource = press; ControversySource = controversy; }

	/// <summary>A strong night sells the record a little; a breakdown asks the region to buy a little less of it.</summary>
	public static float PerformanceFactor(MediaPerformanceResult result) => result?.Outcome switch {
		PerformanceOutcome.Breakdown => -0.5f,
		PerformanceOutcome.Weak => 0.3f,
		PerformanceOutcome.Engaging => 1.3f,
		PerformanceOutcome.Electrifying => 1.6f,
		_ => 1f,
	};

	public enum Role { None, Recipient, Donor, NeutralRecipient, NeutralDonor, NoBaseline }

	/// <summary>One single's intent in one region for one week.</summary>
	public sealed class Title {
		public string Key;
		public int Baseline, PhysicalLimit;
		/// <summary>The net request as a share of the baseline, before any cap.</summary>
		public float Request;
		public bool Frenzy;
		/// <summary>Units the title asked to gain (+) or give up (−) after its own caps; what it got; the clearing input.</summary>
		public int Wanted, Granted, Adjusted;
		public Role Role;
	}

	public struct RegionResult {
		public int Total, TransferCap, Moved, WantedUp, WantedDown, GrantedUp, GrantedDown, NeutralIn, NeutralOut, NoBaseline;
	}

	/// <summary>
	/// The constrained transfer for one region (§12.3). Positive requests draw first on titles asking to give units up,
	/// then on neutral titles; negative requests send first to positive requesters, then to neutral titles with physical
	/// headroom. Neutral titles are used in one direction only in a pass. Every limit is checked in integers after rounding.
	/// </summary>
	public static RegionResult Allocate(List<Title> titles) {
		var r = new RegionResult();
		long sum = 0;
		foreach (Title t in titles) sum += Math.Max(0, t.Baseline);
		r.Total = (int)sum;
		r.TransferCap = (int)Math.Floor(RegionTransferShare * sum);
		var up = new List<(Title T, int Cap)>();
		var down = new List<(Title T, int Cap)>();
		var neutralIn = new List<(Title T, int Cap)>();
		var neutralOut = new List<(Title T, int Cap)>();
		foreach (Title t in titles) {
			t.Adjusted = Math.Max(0, t.Baseline); t.Wanted = t.Granted = 0; t.Role = Role.None;
			if (t.Baseline <= 0) { if (t.Request != 0f) { t.Role = Role.NoBaseline; r.NoBaseline++; } continue; }
			float cap = t.Frenzy ? FrenzyCap : RoutineCap;
			float q = Math.Clamp(t.Request, -cap, cap);
			int headroom = Math.Max(0, t.PhysicalLimit - t.Baseline);
			int routine = (int)Math.Floor(RoutineCap * t.Baseline);
			if (q > 0f) {
				t.Wanted = Math.Min((int)Math.Floor(q * t.Baseline), headroom);
				if (t.Wanted > 0) { t.Role = Role.Recipient; up.Add((t, t.Wanted)); r.WantedUp += t.Wanted; }
			} else if (q < 0f) {
				t.Wanted = -Math.Min((int)Math.Floor(-q * t.Baseline), t.Baseline);
				if (t.Wanted < 0) { t.Role = Role.Donor; down.Add((t, -t.Wanted)); r.WantedDown -= t.Wanted; }
			} else {
				if (routine > 0) neutralOut.Add((t, routine));
				int intoCap = Math.Min(routine, headroom);
				if (intoCap > 0) neutralIn.Add((t, intoCap));
			}
		}
		long p = r.WantedUp, n = r.WantedDown;
		long nd = neutralOut.Sum(x => (long)x.Cap), nr = neutralIn.Sum(x => (long)x.Cap);
		long moved = Math.Min(r.TransferCap, Math.Max(Math.Min(p, n + nd), Math.Min(n, p + nr)));
		r.Moved = (int)moved;
		r.GrantedUp = (int)Math.Min(p, moved);
		r.GrantedDown = (int)Math.Min(n, moved);
		r.NeutralIn = r.Moved - r.GrantedUp;
		r.NeutralOut = r.Moved - r.GrantedDown;
		foreach (var (t, give) in Distribute(up, r.GrantedUp)) { t.Granted = give; t.Adjusted += give; }
		foreach (var (t, give) in Distribute(down, r.GrantedDown)) { t.Granted = -give; t.Adjusted -= give; }
		foreach (var (t, give) in Distribute(neutralIn, r.NeutralIn)) { t.Granted = give; t.Adjusted += give; t.Role = Role.NeutralRecipient; }
		foreach (var (t, give) in Distribute(neutralOut, r.NeutralOut)) { t.Granted = -give; t.Adjusted -= give; t.Role = Role.NeutralDonor; }
		return r;
	}

	/// <summary>
	/// The permutation diagnostic, volume-matched: titles with intent are ranked by intent and cut into blocks of
	/// <see cref="PermuteBlock"/> neighbours; inside each block the requests (with their frenzy flags) are rotated in a keyed
	/// order. A request lands on an unrelated title of about the same size, so about the same units move, but television no
	/// longer chooses who receives them.
	/// </summary>
	public static void PermuteRequests(List<Title> titles, int week, string regionId) {
		var live = titles.Where(t => t.Baseline > 0).OrderByDescending(t => t.Baseline).ThenBy(t => t.Key, StringComparer.Ordinal).ToList();
		for (int start = 0; start + 1 < live.Count; start += PermuteBlock) {
			var block = live.Skip(start).Take(PermuteBlock)
				.OrderBy(t => MediaAtmosphere.Unit("demand-permute|" + regionId + "|" + week + "|" + t.Key)).ThenBy(t => t.Key, StringComparer.Ordinal).ToList();
			if (block.Count < 2) break;
			var requests = block.Select(t => (t.Request, t.Frenzy)).ToList();
			for (int i = 0; i < block.Count; i++) (block[(i + 1) % block.Count].Request, block[(i + 1) % block.Count].Frenzy) = requests[i];
		}
	}

	private const int PermuteBlock = 5;

	/// <summary>
	/// Splits an integer amount over capped items in proportion to their caps: floors first, then one unit each to the
	/// largest remainders (ties by key, ordinal). The amount never exceeds the summed caps, so no item passes its cap.
	/// </summary>
	private static IEnumerable<(Title T, int Give)> Distribute(List<(Title T, int Cap)> items, int amount) {
		if (amount <= 0 || items.Count == 0) yield break;
		long total = items.Sum(x => (long)x.Cap);
		if (total <= 0) yield break;
		if (amount > total) throw new InvalidOperationException($"MediaDemand: {amount} units over {total} of capacity.");
		var shares = items.Select(x => (x.T, x.Cap, Floor: (int)(amount * (long)x.Cap / total), Rem: amount * (long)x.Cap % total)).ToList();
		int left = amount - shares.Sum(s => s.Floor);
		var extra = new HashSet<Title>(shares.Where(s => s.Rem > 0).OrderByDescending(s => s.Rem).ThenBy(s => s.T.Key, StringComparer.Ordinal)
			.Take(left).Select(s => s.T));
		foreach (var s in shares) {
			int give = s.Floor + (extra.Contains(s.T) ? 1 : 0);
			if (give > 0) yield return (s.T, give);
		}
	}

	// ---- telemetry (print-only) ----------------------------------------------------------------------------------

	private static readonly Dictionary<string, double> ledger = new(StringComparer.Ordinal);
	private static double Ledger(string key) => ledger.GetValueOrDefault(key);
	private static void Add(string key, double amount) => ledger[key] = ledger.GetValueOrDefault(key) + amount;
	/// <summary>A request source's own counters for the yearly line (print-only).</summary>
	internal static void Count(string key, double amount) => Add(key, amount);
	private static bool logHeader;

	/// <summary>Books one region's pass: totals for the yearly line, and a row per title that asked or moved.</summary>
	public static void Record(int week, int year, string regionId, List<Title> titles, RegionResult r) {
		Add("regionWeeks", 1);
		Add("intent", r.Total);
		Add("wantedUp", r.WantedUp); Add("wantedDown", r.WantedDown);
		Add("moved", r.Moved); Add("grantedUp", r.GrantedUp); Add("grantedDown", r.GrantedDown);
		Add("neutralIn", r.NeutralIn); Add("neutralOut", r.NeutralOut);
		Add("unmatched", r.WantedUp + r.WantedDown - r.GrantedUp - r.GrantedDown);
		Add("noBaseline", r.NoBaseline);
		if (r.Moved >= r.TransferCap && r.TransferCap > 0) Add("regionCapBinding", 1);
		foreach (Title t in titles) {
			if (t.Role is Role.Recipient or Role.Donor) Add("requestTitles", 1);
			if (t.Frenzy && t.Role is Role.Recipient or Role.Donor) Add("frenzyTitles", 1);
			if (t.Role == Role.Recipient && t.Wanted < Math.Floor(Math.Min(t.Request, t.Frenzy ? FrenzyCap : RoutineCap) * t.Baseline)) Add("physicalBinding", 1);
		}
		if (Log == null) return;
		if (!logHeader) {
			Log.WriteLine("week,year,regionId,recordId,baseline,physicalLimit,request,frenzy,wanted,granted,adjusted,role,regionIntent,regionTransferCap,regionMoved");
			logHeader = true;
		}
		foreach (Title t in titles) {
			if (t.Role == Role.None && t.Granted == 0) continue;
			Log.WriteLine(string.Join(",", week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), regionId, t.Key,
				t.Baseline.ToString(CultureInfo.InvariantCulture), t.PhysicalLimit.ToString(CultureInfo.InvariantCulture),
				t.Request.ToString("0.#####", CultureInfo.InvariantCulture), t.Frenzy ? "true" : "false",
				t.Wanted.ToString(CultureInfo.InvariantCulture), t.Granted.ToString(CultureInfo.InvariantCulture), t.Adjusted.ToString(CultureInfo.InvariantCulture),
				t.Role.ToString(), r.Total.ToString(CultureInfo.InvariantCulture), r.TransferCap.ToString(CultureInfo.InvariantCulture), r.Moved.ToString(CultureInfo.InvariantCulture)));
		}
	}

	/// <summary>Year end: what television asked for, what it moved, and where the limits bound. Clears the ledger.</summary>
	public static void PrintYear(int year) {
		if (!Active) return;
		double intent = Ledger("intent"), moved = Ledger("moved");
		Godot.GD.Print($"MEDIA_DEMAND year={year} mode={(Causal ? "causal" : "shadow")} regionWeeks={Ledger("regionWeeks"):0} intent={intent:0}" +
			$" wanted[up/down]={Ledger("wantedUp"):0}/{Ledger("wantedDown"):0} moved={moved:0} ({(intent > 0 ? moved / intent : 0):P3})" +
			$" granted[up/down]={Ledger("grantedUp"):0}/{Ledger("grantedDown"):0} neutral[in/out]={Ledger("neutralIn"):0}/{Ledger("neutralOut"):0}" +
			$" unmatched={Ledger("unmatched"):0} requestTitleWeeks={Ledger("requestTitles"):0} frenzy={Ledger("frenzyTitles"):0}" +
			$" noBaseline={Ledger("noBaseline"):0} regionCapBinding={Ledger("regionCapBinding"):0} physicalBinding={Ledger("physicalBinding"):0}" +
			(PressSource ? $" press[items/absorbed/requests/sum]={Ledger("pressItems"):0}/{Ledger("pressAbsorbed"):0}/{Ledger("pressRequests"):0}/{Ledger("pressRequestSum"):0.####}" : "") +
			(ControversySource ? $" controversy[count/sum]={Ledger("controversies"):0}/{Ledger("controversyRequestSum"):0.####}" : ""));
		ledger.Clear();
		Log?.Flush();
	}
}
