using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// --local-promotion=K (LpGenreAllocationFollowup.md §98). In the model a label's home advantage enters only at launch
/// (the launch factor and the hometown awareness deposit). Once a record charts, the national awareness blend and the
/// chart floors take over, so a small label's Top 40 record sells 1.1-1.4x per head at home (`stB`). The 1960 station
/// surveys put small Los Angeles labels at about 2.5x at home. Established national indies show none (Liberty, Dot,
/// Imperial about 0.9), and neither do Majors or New York indies. What history had and the model lacks is the label's
/// own week-in, week-out push: the owner and the label's own branches working DJs and one-stops where it has people.
/// <para>Each week, after a live AI single's serviceable intent is known in every region and before the regional purses
/// clear, the intent is reweighted by m_g = 1 + (K − 1)/n in the label's promotion footprint (home region, owned
/// distribution regions, strong regions; n regions) and 1 elsewhere, then rescaled so the record's national intent is
/// unchanged (each region still capped at its physical limit). A home-only label's record has K times the intent per
/// head at home it would otherwise have relative to away. A whole-map footprint (a Major) is untouched. Wholesale houses
/// and P&amp;D deals ship a line but do not promote it, so they are not footprint. Promotion moves where a record sells,
/// not how much is wanted; the purses then clear as before.</para>
/// <para>The first build multiplied raw demand by footprint weights normalised over the map's buyers. That was not
/// neutral: a small record's demand sits in its footprint, so the away weights below 1 fell where it barely sells, and
/// small labels gained 6-8 points of national unit share (§98, `lp3`). Normalising per record fixes that.</para>
/// <para>`--local-promotion-home-share=H` (§99): the owner works the home region, branches the rest. Home takes max(1/n, H)
/// of the K − 1, the other footprint regions split the remainder; a home-only label is unchanged.</para>
/// <para>Singles only; the survey evidence is singles. No draws, and no tier, cash or sales input. Off: never read.</para>
/// </summary>
public static class LocalPromotion {
	public const string Switch = "--local-promotion", HomeShareSwitch = "--local-promotion-home-share";
	private static bool read;
	private static float strength = 1f, homeShare;
	private static int cacheWeek = int.MinValue;
	private static readonly Dictionary<string, HashSet<string>> cache = new(StringComparer.Ordinal);
	private static int regionCount;

	private static void Read() {
		if (read) return;
		strength = GameyAuditArguments.Value(Switch, 1f, 10f) ?? 1f;
		float? h = GameyAuditArguments.Value(HomeShareSwitch, 0f, 1f);
		if (h != null && strength <= 1f) throw new ArgumentException(HomeShareSwitch + " needs " + Switch + "=K.");
		homeShare = h ?? 0f;
		string midTierArg = SimulationArguments.Get().FirstOrDefault(a => a.StartsWith(MidTierSwitch + "=", StringComparison.Ordinal));
		if (midTierArg != null) {
			string mode = midTierArg.Substring(MidTierSwitch.Length + 1);
			midTier = mode switch { "equal" => MidTierMode.Equal, "off" => MidTierMode.Off,
				_ => throw new ArgumentException(MidTierSwitch + " takes equal or off.") };
			if (strength <= 1f) throw new ArgumentException(MidTierSwitch + " needs " + Switch + "=K.");
		}
		string feedbackArg = SimulationArguments.Get().FirstOrDefault(a => a.StartsWith(FeedbackSwitch + "=", StringComparison.Ordinal));
		if (feedbackArg != null) {
			if (strength <= 1f) throw new ArgumentException(FeedbackSwitch + " needs " + Switch + "=K.");
			string value = feedbackArg.Substring(FeedbackSwitch.Length + 1);
			if (value == "preshift") feedbackPreShift = true;
			else foreach (string part in value.Split(',')) feedbackGroups |= part switch {
				"money" => FeedbackGroup.Money, "supply" => FeedbackGroup.Supply, "audience" => FeedbackGroup.Audience,
				"depletion" => FeedbackGroup.Depletion, "restock" => FeedbackGroup.Restock,
				_ => throw new ArgumentException(FeedbackSwitch + " takes preshift, or a comma list of money, supply (= depletion + restock), audience.") };
		}
		string ledgerArg = SimulationArguments.Get().FirstOrDefault(a => a.StartsWith("--local-promotion-ledger=", StringComparison.Ordinal));
		if (ledgerArg != null) {
			if (ledgerArg != "--local-promotion-ledger=preshift") throw new ArgumentException("--local-promotion-ledger takes preshift.");
			if (strength <= 1f) throw new ArgumentException("--local-promotion-ledger needs " + Switch + "=K.");
			ledgerPreShift = true;
		}
		if (strength > 1f) Godot.GD.Print(string.Format(CultureInfo.InvariantCulture, "LOCAL_PROMOTION strength={0} homeShare={1} midTier={2} feedbackPreShift={3}", strength, homeShare, midTier, feedbackPreShift)
			+ (feedbackGroups != FeedbackGroup.None ? " feedbackGroups=" + feedbackGroups : ""));
		read = true;
	}

	/// <summary>
	/// --local-promotion-ledger=preshift (§100, a diagnostic): the act LP audience ledger books a shifted single's cleared
	/// units at the region's pre-shift intent, cleared × pre / shifted, so LP demand sees the unshifted geography.
	/// Identity when the diagnostic is off or the intent was not shifted.
	/// </summary>
	public static int LedgerUnits(int cleared, int serviceable, int preShift) {
		if (!ledgerPreShift || preShift < 0 || cleared <= 0) return cleared;
		return serviceable > 0 ? (int)Math.Round(cleared * (double)preShift / serviceable) : cleared;
	}
	private static bool ledgerPreShift;

	/// <summary>
	/// --local-promotion-feedback=preshift (§103, a diagnostic). Promotion changes two things: who wins each region's
	/// purse (the clearing), and where a record's sales are reported afterwards, which awareness, word of mouth,
	/// breakouts, restocking, fan recruitment, label regional performance and the trade charts all read. With the
	/// diagnostic the clearing stays shifted, but a shifted single's regional sales (and its stock depletion) are
	/// restated at its pre-shift geography before anything else sees them; national units are unchanged.
	/// </summary>
	public const string FeedbackSwitch = "--local-promotion-feedback";
	private static bool feedbackPreShift;
	public static bool FeedbackPreShift { get { Read(); return feedbackPreShift; } }

	/// <summary>
	/// --local-promotion-feedback=money,supply,audience (§104, a diagnostic): the §103 restatement split by reader. The
	/// purses clear shifted and <see cref="RegionalRecordData.unitsSoldThisWeek"/> keeps the cleared units; only the
	/// named groups read the restated units (<see cref="Units"/>, <see cref="UnitsTotal"/>).
	/// Money: distribution skim, distance COGS, strong-region evidence (label books and deal evidence).
	/// Supply: stock depletion, restock and retail position (racks are LPs, never shifted).
	/// Audience: local sales signals (awareness, arrival), cumulative exhaustion, breakouts, fan and media following
	/// recruitment, follower settlement, displaced buyers, genre momentum, regional and trade charts, the act LP ledger.
	/// </summary>
	/// Supply splits into Depletion (the shelf loses the restated units) and Restock (restock and retail position read them).
	[Flags] public enum FeedbackGroup { None = 0, Money = 1, Audience = 4, Depletion = 8, Restock = 16, Supply = Depletion | Restock }
	private static FeedbackGroup feedbackGroups;
	public static bool AnyFeedbackGroup { get { Read(); return feedbackGroups != FeedbackGroup.None; } }
	public static bool FeedbackOn(FeedbackGroup g) { Read(); return (feedbackGroups & g) != 0; }
	/// <summary>This week's units as group g sees them: the cleared units, restated when g is on.</summary>
	public static int Units(RegionalRecordData d, FeedbackGroup g) =>
		d == null ? 0 : d.unitsSoldThisWeek + ((feedbackGroups & g) != 0 ? d.feedbackUnitsDelta : 0);
	/// <summary>Cumulative units as group g sees them.</summary>
	public static int UnitsTotal(RegionalRecordData d, FeedbackGroup g) =>
		d == null ? 0 : d.unitsSoldTotal + ((feedbackGroups & g) != 0 ? d.feedbackUnitsTotalDelta : 0);

	/// <summary>
	/// Pure: a record's cleared units redistributed over its regions in proportion to cleared × pre / serviceable (the
	/// pre-shift intent at each region's realised fill), integers by largest remainders, the total unchanged. Regions
	/// the shift did not move (pre = serviceable) keep their share; a record with no shift is returned unchanged.
	/// </summary>
	public static int[] RestateUnits(IReadOnlyList<int> cleared, IReadOnlyList<int> pre, IReadOnlyList<int> serviceable) {
		int n = cleared.Count;
		var result = new int[n];
		long total = 0; double weightSum = 0;
		var weights = new double[n];
		for (int i = 0; i < n; i++) {
			total += cleared[i];
			weights[i] = serviceable[i] > 0 ? (double)cleared[i] * pre[i] / serviceable[i] : 0d;
			weightSum += weights[i];
		}
		if (total <= 0 || weightSum <= 0) { for (int i = 0; i < n; i++) result[i] = cleared[i]; return result; }
		long assigned = 0;
		var remainders = new List<(double Rem, int Index)>(n);
		for (int i = 0; i < n; i++) {
			double exact = total * weights[i] / weightSum;
			result[i] = (int)Math.Floor(exact);
			assigned += result[i];
			remainders.Add((exact - result[i], i));
		}
		foreach (var r in remainders.OrderByDescending(r => r.Rem).ThenBy(r => r.Index).Take((int)(total - assigned))) result[r.Index]++;
		return result;
	}

	/// <summary>
	/// --local-promotion-midtier=equal|off (§101). The surveys put established national indies (Liberty, Dot, Imperial;
	/// MidTier in the model) at about 0.9 at home: they promoted through national promotion staff and distributors, not
	/// an owner working the home town. Under H 0.75 their Top 40 home premium rose 1.24 → 1.49 (West Coast 1.34 → 1.80,
	/// `lph75f-1001`). Equal: a MidTier's boost is split equally over its footprint (no owner home share). Off: a MidTier
	/// promotes nationally, like a Major, and its records are not shifted. Reads the label's current tier.
	/// </summary>
	public enum MidTierMode { Owner, Equal, Off }
	public const string MidTierSwitch = "--local-promotion-midtier";
	private static MidTierMode midTier = MidTierMode.Owner;

	internal static void SetForChecks(float k, float h = 0f, MidTierMode m = MidTierMode.Owner, FeedbackGroup groups = FeedbackGroup.None) {
		read = true; strength = k; homeShare = h; midTier = m; feedbackPreShift = false; feedbackGroups = groups; cacheWeek = int.MinValue; cache.Clear();
	}

	private static float HomeShareFor(AILabel label) => midTier == MidTierMode.Equal && label.tier == LabelTier.MidTier ? 0f : homeShare;

	public static bool Enabled { get { Read(); return strength > 1f; } }

	/// <summary>The regions the label promotes in: home, owned distribution and strong regions.</summary>
	public static HashSet<string> Footprint(AILabel label) {
		var set = new HashSet<string>(StringComparer.Ordinal);
		if (label == null) return set;
		if (!string.IsNullOrEmpty(label.homeRegion)) set.Add(label.homeRegion);
		foreach (string r in label.distributionRegions ?? Array.Empty<string>()) if (!string.IsNullOrEmpty(r)) set.Add(r);
		foreach (string r in label.strongRegions ?? Array.Empty<string>()) if (!string.IsNullOrEmpty(r)) set.Add(r);
		return set;
	}

	private static HashSet<string> CachedFootprint(AILabel label) {
		ChartManager chart = ChartManager.Instance;
		int week = chart?.GetCurrentChartWeek() ?? 0;
		if (week != cacheWeek) { cache.Clear(); cacheWeek = week; regionCount = chart?.GetAllRegions().Count ?? 7; }
		string key = label.labelId ?? "";
		if (!cache.TryGetValue(key, out var set)) cache[key] = set = Footprint(label);
		return set;
	}

	/// <summary>True when the switch moves this record: an AI single whose label promotes in some but not all regions.</summary>
	public static bool Shifts(RecordRuntimeData record, AILabel label) {
		if (!Enabled || label == null || record?.baseRecord == null) return false;
		if (record.baseRecord.format != ReleaseFormat.Single || record.baseRecord.isPlayerOwned || label.isPlayerOwned) return false;
		if (midTier == MidTierMode.Off && label.tier == LabelTier.MidTier) return false;
		int n = CachedFootprint(label).Count;
		return n > 0 && n < regionCount;
	}

	/// <summary>This week's shifted serviceable intent for one record, region by region.</summary>
	public static int[] Shift(AILabel label, IReadOnlyList<string> regions, IReadOnlyList<int> serviceable, IReadOnlyList<int> limits) {
		HashSet<string> footprint = CachedFootprint(label);
		var weights = new float[regions.Count];
		float h = HomeShareFor(label);
		for (int i = 0; i < regions.Count; i++) weights[i] = Weight(footprint, regions[i], strength, label.homeRegion, h);
		return ShiftValues(weights, serviceable, limits);
	}

	/// <summary>
	/// 1 + (K − 1)·share in the footprint, 1 elsewhere. The home region's share is max(1/n, H)
	/// (<c>--local-promotion-home-share=H</c>; 0 = an equal split); the other footprint regions split the rest equally.
	/// A home-only label gets K at home whatever H is.
	/// </summary>
	public static float Weight(ICollection<string> footprint, string region, float k, string home = null, float homeShare = 0f) {
		int n = footprint.Count;
		if (n == 0 || !footprint.Contains(region)) return 1f;
		bool weighted = homeShare > 0f && !string.IsNullOrEmpty(home) && footprint.Contains(home) && n > 1;
		if (!weighted) return 1f + (k - 1f) / n;
		float h = Math.Max(1f / n, homeShare);
		return region == home ? 1f + (k - 1f) * h : 1f + (k - 1f) * (1f - h) / (n - 1);
	}

	/// <summary>
	/// Pure: s'_i ∝ s_i·w_i with the same total, water-filled under limit_i (a capped region's excess goes to the record's
	/// other regions in proportion), integers by largest remainders. The total is kept whenever the limits allow it
	/// (§99: the §98 build capped after scaling and lost 2.4% of national singles intent to the LP budget). Used by the checks.
	/// </summary>
	public static int[] ShiftValues(IReadOnlyList<float> weights, IReadOnlyList<int> serviceable, IReadOnlyList<int> limits) {
		int count = serviceable.Count;
		var result = new int[count];
		long total = 0; double weighted = 0;
		for (int i = 0; i < count; i++) { total += serviceable[i]; weighted += (double)serviceable[i] * weights[i]; }
		if (total <= 0 || weighted <= 0) { for (int i = 0; i < count; i++) result[i] = serviceable[i]; return result; }
		var exact = new double[count];
		var capped = new bool[count];
		for (int pass = 0; pass <= count; pass++) {
			double capTotal = 0, freeWeight = 0;
			for (int i = 0; i < count; i++) {
				if (capped[i]) capTotal += Math.Max(0, limits[i]);
				else freeWeight += (double)serviceable[i] * weights[i];
			}
			double room = Math.Max(0d, total - capTotal);
			bool newCap = false;
			for (int i = 0; i < count; i++) {
				if (capped[i]) { exact[i] = Math.Max(0, limits[i]); continue; }
				exact[i] = freeWeight > 0 ? room * serviceable[i] * weights[i] / freeWeight : 0d;
				if (exact[i] > Math.Max(0, limits[i])) { capped[i] = true; newCap = true; }
			}
			if (!newCap) break;
		}
		long assigned = 0;
		var remainders = new List<(double Rem, int Index)>(count);
		for (int i = 0; i < count; i++) {
			result[i] = (int)Math.Min(Math.Floor(exact[i]), Math.Max(0, limits[i]));
			assigned += result[i];
			if (result[i] < limits[i]) remainders.Add((exact[i] - result[i], i));
		}
		foreach (var r in remainders.OrderByDescending(r => r.Rem).ThenBy(r => r.Index).Take((int)Math.Max(0, total - assigned))) result[r.Index]++;
		return result;
	}
}
