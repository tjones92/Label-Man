using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Promotion capacity (Big5ChartOpportunityInvestigation.md, "How well a Big-5 single converts", 2026-11-17). Off unless named.
///
/// --major-plug-slots=K: an AI Major can give its full launch package to at most K new singles a calendar month. A period
/// major issued about 20 singles a week and its promotion men worked a few of them ("plug" records); the rest went out to
/// the trade and the dealers and lived or died on their own. In the sim every Big-5 single launched with the full package
/// and converted to the Top 10 at 7.8% per issue against about 1.9% in history, while field singles matched history.
/// --midtier-plug-slots=K does the same for AI MidTier labels.
///
/// Who gets a plug (the label's awareness, not luck):
///  1. A hit act: Star or Superstar, or an act whose last completed chart run was a Top 40. Always plugged while slots
///     remain. Once the month's slots are spent the label may buy one more plug at
///     <see cref="ExtraPlugCostMultiple"/> times its standard marketing budget, if its cash covers four such plugs.
///  2. Any other single: plugged only while more slots remain than the label holds back for its hit acts (one slot per three
///     hit acts on the roster, never more than K), and only if the label's read of the record is at least the median of its
///     own last <see cref="ReadWindow"/> reads. The read is the record's quality plus a keyed error of up to
///     ±<see cref="ReadError"/>. With fewer than <see cref="MinReadsForBar"/> reads on file there is no bar.
///
/// A record that is not plugged launches as a routine issue. It keeps the label's national distribution, but its campaign
/// impact (launch and weekly push), reputation and payola read the reference label (<see cref="LabelBlindLaunch"/> promo), its
/// regional launch factor and launch stock read the reference footprint, and its buyer connection is one shared keyed draw
/// at the shared chance (--label-blind-launch-expose-shared=P, else the reference chance). City reach and the arrival hold
/// stay the label's own. What the label spent on the record is unchanged in this version.
///
/// --plug-hit-act-top10 (with a plug switch): rule 1's hit act is a Star or Superstar, or an act whose last completed chart
/// run reached the Top 10; a last run at #11-40 is an ordinary single under rule 2, and the hit-act reserve counts only these
/// acts. Repeat entries by Big-5 acts whose best was #11-40 reached the Top 10 at 28% a chart event against history's 10%
/// (label-blind), and the plugged ones carried it: 15.6% a launch against 4.0% routine and 4.5% in the field
/// (LpGenreAllocationFollowup.md §90). Off: the Top 40 rule above.
///
/// --plug-graded=G (with a plug switch; not with --plug-hit-act-top10): a hit act keeps the Top 40 rule for slots and the
/// reserve, but only a Star, a Superstar or an act whose last completed run reached the Top 10 gets the full package. Any other
/// hit act (last run #11-60 on an unbroken Top 40 streak) takes its slot as before and launches with the full package at keyed
/// chance G, else as a routine issue (a held plug); with the month's slots spent it goes routine and buys none. So slots are not
/// freed for other acts (§90: that moved plugs to the excess). G = 0: a held plug always; G = 1: the Top 40 rule but no bought plugs.
///
/// Albums, promo singles cut from albums, the player's records, records bought as masters and opening catalog are never
/// routed. Draws nothing from any stream; the read error is a counter-based keyed unit.
/// </summary>
public static class PromotionCapacity {
	public const string MajorSwitch = "--major-plug-slots=";
	public const string MidTierSwitch = "--midtier-plug-slots=";
	public const string HitActTop10Switch = "--plug-hit-act-top10";
	public const string GradedPrefix = "--plug-graded=";
	public const int ReadWindow = 24, MinReadsForBar = 6, HitActsPerReservedSlot = 3;
	public const float ReadError = .05f, ExtraPlugCostMultiple = 1f, ExtraPlugCashCover = 4f;

	private static bool configured;
	public static int MajorSlots { get; private set; } = -1;
	public static int MidTierSlots { get; private set; } = -1;
	public static bool Enabled => MajorSlots >= 0 || MidTierSlots >= 0;
	public static bool HitActTop10 { get; private set; }
	/// <summary>--plug-graded: the full-package chance for a hit act below the Top 10 rung; -1 when off.</summary>
	public static float Graded { get; private set; } = -1f;

	private static readonly Dictionary<string, (int Month, int Used)> used = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, List<float>> reads = new(StringComparer.Ordinal);
	private static readonly HashSet<string> routine = new(StringComparer.Ordinal);
	[ThreadStatic] private static int routineScope;
	public static long Plugged, Routine, Bought;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		MajorSlots = Parse(args, MajorSwitch);
		MidTierSlots = Parse(args, MidTierSwitch);
		HitActTop10 = args.Contains(HitActTop10Switch);
		if (HitActTop10 && !Enabled) throw new ArgumentException(HitActTop10Switch + " needs " + MajorSwitch.TrimEnd('=') + " or " + MidTierSwitch.TrimEnd('=') + ".");
		var graded = args.Where(a => a.StartsWith(GradedPrefix, StringComparison.Ordinal)).ToArray();
		if (graded.Length > 1) throw new ArgumentException("Name " + GradedPrefix.TrimEnd('=') + " once.");
		Graded = -1f;
		if (graded.Length == 1) {
			if (!float.TryParse(graded[0].Substring(GradedPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float g) || !(g >= 0f && g <= 1f))
				throw new ArgumentException(GradedPrefix.TrimEnd('=') + " needs a chance in [0, 1].");
			if (!Enabled || HitActTop10) throw new ArgumentException(GradedPrefix.TrimEnd('=') + " needs a plug switch and not " + HitActTop10Switch + ".");
			Graded = g;
		}
		Reset();
		if (Enabled) GD.Print($"PROMOTION_CAPACITY majorSlots={MajorSlots} midTierSlots={MidTierSlots} hitActTop10={HitActTop10} graded={Graded}");
		configured = true;
	}

	private static int Parse(string[] args, string prefix) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + prefix.TrimEnd('=') + " once.");
		if (values.Length == 0) return -1;
		if (!int.TryParse(values[0].Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) || k < 0 || k > 100)
			throw new ArgumentException(prefix.TrimEnd('=') + " needs a whole number of plugs a month in [0, 100].");
		return k;
	}

	private static void Reset() { used.Clear(); reads.Clear(); routine.Clear(); routineScope = 0; Plugged = Routine = Bought = Held = 0; }

	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }

	internal static void SetForChecks(int major, int midTier, bool hitActTop10 = false, float graded = -1f) {
		configured = true; MajorSlots = major; MidTierSlots = midTier; HitActTop10 = hitActTop10; Graded = graded; Reset();
	}

	/// <summary>The month's plug slots for an AI label; -1 when the label is outside every switch.</summary>
	public static int SlotsFor(AILabel label) {
		EnsureConfigured();
		if (label == null || label.isPlayerOwned) return -1;
		return label.tier switch { LabelTier.Major => MajorSlots, LabelTier.MidTier => MidTierSlots, _ => -1 };
	}

	/// <summary>A hit act: Star or Superstar, or the act's last completed chart run was a Top 40 (--plug-hit-act-top10: a Top 10).</summary>
	public static bool HitAct(SimulatedArtist artist) =>
		artist != null && (artist.careerState is CareerState.Star or CareerState.Superstar ||
			(HitActTop10 ? artist.lastRunPeak is > 0 and <= 10 : artist.consecutiveHits > 0));

	/// <summary>--plug-graded: a hit act that earns the full package (a star or a Top 10 last run).</summary>
	public static bool FullPackageAct(SimulatedArtist artist) =>
		artist != null && (artist.careerState is CareerState.Star or CareerState.Superstar || artist.lastRunPeak is > 0 and <= 10);

	/// <summary>The label's read of a record: quality plus a keyed error in [-ReadError, +ReadError].</summary>
	public static float Read(string recordId, float quality) =>
		quality + ReadError * (2f * (float)BandLife.Unit("plug-read|" + recordId) - 1f);

	/// <summary>True when the record launched as a routine issue (its promo reads the reference label).</summary>
	public static bool IsRoutine(string recordId) => recordId != null && routine.Count > 0 && routine.Contains(recordId);

	/// <summary>Inside the launch of a routine issue: the launch-footprint reads see the reference footprint.</summary>
	public static bool InRoutine => routineScope > 0;

	/// <summary>HeldPlug (--plug-graded): the slot is used but the single launches as a routine issue.</summary>
	public enum Outcome { NotRouted, Plugged, BoughtPlug, Routine, HeldPlug }
	public static long Held;

	/// <summary>The decision for one launch, with no side effects except the label's month counter, its read history and
	/// (for a bought plug) the charge passed to <paramref name="charge"/>. <paramref name="month"/> is year * 12 + month.</summary>
	public static Outcome Decide(AILabel label, SimulatedArtist artist, string recordId, float quality, int month, Action<float> charge) {
		int k = SlotsFor(label);
		if (k < 0 || recordId == null) return Outcome.NotRouted;
		string id = label.labelId;
		if (!used.TryGetValue(id, out var u) || u.Month != month) u = (month, 0);
		bool hit = HitAct(artist);
		float read = Read(recordId, quality);
		if (!reads.TryGetValue(id, out var history)) reads[id] = history = new List<float>();
		float bar = history.Count >= MinReadsForBar ? Median(history) : float.NegativeInfinity;
		history.Add(read);
		if (history.Count > ReadWindow) history.RemoveAt(0);

		Outcome outcome;
		bool partial = hit && Graded >= 0f && !FullPackageAct(artist);
		if (partial) {
			outcome = u.Used >= k ? Outcome.Routine
				: BandLife.Unit("plug-grade|" + recordId) < Graded ? Outcome.Plugged : Outcome.HeldPlug;
		} else if (hit) {
			if (u.Used < k) outcome = Outcome.Plugged;
			else {
				float price = ExtraPlugCostMultiple * label.GetStandardMarketingBudget();
				if (price > 0f && label.cashReserves >= ExtraPlugCashCover * price) { charge?.Invoke(price); outcome = Outcome.BoughtPlug; }
				else outcome = Outcome.Routine;
			}
		} else {
			int hitActs = label.roster?.Count(HitAct) ?? 0;
			int reserve = Math.Min(k, (hitActs + HitActsPerReservedSlot - 1) / HitActsPerReservedSlot);
			outcome = u.Used < k - reserve && read >= bar ? Outcome.Plugged : Outcome.Routine;
		}
		if (outcome != Outcome.Routine) u.Used++;
		used[id] = u;
		switch (outcome) {
			case Outcome.Plugged: Plugged++; break;
			case Outcome.BoughtPlug: Bought++; Plugged++; break;
			case Outcome.Routine: Routine++; routine.Add(recordId); break;
			case Outcome.HeldPlug: Held++; Routine++; routine.Add(recordId); break;
		}
		return outcome;
	}

	private static float Median(List<float> values) {
		var sorted = values.OrderBy(v => v).ToList();
		int n = sorted.Count;
		return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2f;
	}

	/// <summary>Routes one launch and, for a routine issue, opens the routine footprint scope until disposed. Live AI singles
	/// at labels in scope only; everything else returns an inactive scope and leaves no trace.</summary>
	public static RoutineScope Launch(RecordRuntimeData record, AILabel label, SimulatedArtist artist, int week, Action<float> charge) {
		EnsureConfigured();
		if (!Enabled || !LabelBlindLaunch.AppliesTo(record, label) || record.projectRole == ProjectRecordRole.PromoSingle)
			return new RoutineScope(false);
		var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		string recordId = record.baseRecord.recordId;
		float quality = record.GetQuality();
		Outcome outcome = Decide(label, artist, recordId, quality, date.year * 12 + date.month, charge);
		if (outcome == Outcome.NotRouted) return new RoutineScope(false);
		GD.Print($"PROMO_PLUG week={week} label={label.labelId} tier={label.tier} record={recordId} artist={artist?.artistId} " +
			$"career={artist?.careerState} hit={HitAct(artist)} quality={quality:F3} outcome={outcome}");
		return new RoutineScope(outcome is Outcome.Routine or Outcome.HeldPlug);
	}

	public readonly struct RoutineScope : IDisposable {
		private readonly bool active;
		public RoutineScope(bool active) { this.active = active; if (active) routineScope++; }
		public void Dispose() { if (active) routineScope--; }
	}
}
