using System;
using System.Collections.Generic;

/// <summary>
/// --label-blind-launch=PARTS: a calibration ablation (SimTools/MajorShareRootCauseHandoff.md, hypothesis 4). A live AI
/// single's launch reads one fixed reference label in place of its own label, so any realization premium that survives
/// is upstream of launch (the roster, the slate, the material). Off unless named; draws nothing, and every draw the
/// launch makes is still made in the same order.
///
/// Parts (comma-separated; "all" = both):
///  promo — the campaign impact in launch awareness and launch radio and in the weekly label push, the reputation term
///          in launch awareness and the payola term in launch radio read <see cref="ReferenceImpact"/>,
///          <see cref="ReferenceReputation"/> and <see cref="ReferencePayola"/>.
///  foot  — the launch footprint: every region is covered (none strong, none home) at <see cref="ReferenceReach"/> for
///          the regional launch factor and launch stock, no region is held for arrival, every city is reached, and the
///          fit-exposure launch chance reads the reference reach (the act's home region stays exposed). Its five reads can
///          be named alone: factor (the regional launch factor), stock (launch stock), city (city reach), hold (the
///          arrival hold), expose (the fit-exposure launch chance); foot = all five.
///
/// --label-blind-launch-dose=D: a softening, not an ablation (SimTools/MajorShareRootCauseHandoff.md, split screen). The
/// footprint works only when its reads move together, so for a live non-Major AI single a keyed share D of regions is
/// opened: in an opened region the launch factor is at least the reference covered factor, every city is in the launch
/// territory, the region is not held for arrival, and the fit-exposure chance is at least the reference chance. One keyed
/// draw per record and region feeds all four reads. Lift only: a label's own covered, strong or home region never reads
/// worse, launch stock is untouched, and Majors are never opened.
///
/// References are the 1960-61 launch-weighted means over every tier in fund-v1-*-gc_of (campaign impact 0.39,
/// reputation 0.37, reach 0.38; payola is the founding draw's midpoint). Albums, the player's records and opening
/// catalog are untouched. Weekly distribution (restock, rack shelf, uncovered terms) is not a launch read and is kept.
/// </summary>
public static class LabelBlindLaunch {
	public const string Switch = "--label-blind-launch";
	public const float ReferenceImpact = .39f, ReferenceReputation = .37f, ReferencePayola = .4f, ReferenceReach = .38f;
	private static bool configured;
	public static bool Promo { get; private set; }
	public static bool Factor { get; private set; }
	public static bool Stock { get; private set; }
	public static bool City { get; private set; }
	public static bool Hold { get; private set; }
	public static bool Expose { get; private set; }
	public static bool Foot => Factor || Stock || City || Hold || Expose;
	private static void SetFoot(bool on) { Factor = Stock = City = Hold = Expose = on; }
	public static bool Enabled => Promo || Foot;

	[ThreadStatic] private static int footScope;
	public const string DoseSwitch = "--label-blind-launch-dose";
	/// <summary>The share of a non-Major single's regions opened to the reference footprint; 0 when off.</summary>
	public static float Dose { get; private set; }
	/// <summary>--label-blind-launch-dose-hot-acts: the dose opens only a hit act's single, one whose act had a Top 40 in the
	/// last <see cref="HotWindowWeeks"/> weeks (the HitActCadence clock), so it targets the follow-up gap
	/// (MajorShareRootCauseHandoff.md, fit208-v3) and leaves cold launches on their own footprint.</summary>
	public const string DoseHotActsSwitch = "--label-blind-launch-dose-hot-acts";
	public const int HotWindowWeeks = 26;
	public static bool DoseHotActsOnly { get; private set; }
	[ThreadStatic] private static int doseScope;
	[ThreadStatic] private static string doseRecordId;
	public static float ReferenceFactor => 0.55f + ReferenceReach * 0.45f;
	public static float ReferenceExposureChance => RegionalFitExposure.LaunchBase + RegionalFitExposure.LaunchReachSlope * ReferenceReach;
	/// <summary>--label-blind-launch-dose-exposure=P: the fit-exposure chance an opened region reads at least, in place of
	/// the reference chance (0.57, about an Independent's own, so the plain dose opens holds but exposes almost nothing
	/// more; FollowUpGapFableHandoff.md). A Major's own chance is about 0.95. Needs the dose; off reads the reference.</summary>
	public const string DoseExposureSwitch = "--label-blind-launch-dose-exposure";
	private static float? doseExposure;
	public static float DoseExposureChance => doseExposure ?? ReferenceExposureChance;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Promo = false; SetFoot(false); Dose = 0f; DoseHotActsOnly = false; doseExposure = null;
		foreach (string arg in arguments ?? Array.Empty<string>()) {
			if (arg == Switch) { Promo = true; SetFoot(true); continue; }
			if (arg == DoseHotActsSwitch) { DoseHotActsOnly = true; continue; }
			if (arg.StartsWith(DoseExposureSwitch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(DoseExposureSwitch.Length + 1), System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float chance) || !float.IsFinite(chance) || chance < 0f || chance > 1f)
					throw new ArgumentException($"{DoseExposureSwitch} must be between 0 and 1.");
				doseExposure = chance;
				continue;
			}
			if (arg.StartsWith(DoseSwitch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(DoseSwitch.Length + 1), System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float dose) || !float.IsFinite(dose) || dose < 0f || dose > 1f)
					throw new ArgumentException($"{DoseSwitch} must be between 0 and 1.");
				Dose = dose;
				continue;
			}
			if (!arg.StartsWith(Switch + "=", StringComparison.Ordinal)) continue;
			foreach (string part in arg.Substring(Switch.Length + 1).Split(',', StringSplitOptions.RemoveEmptyEntries)) {
				switch (part) {
					case "all": Promo = true; SetFoot(true); break;
					case "promo": Promo = true; break;
					case "foot": SetFoot(true); break;
					case "factor": Factor = true; break;
					case "stock": Stock = true; break;
					case "city": City = true; break;
					case "hold": Hold = true; break;
					case "expose": Expose = true; break;
					default: throw new ArgumentException($"{Switch} parts are promo, foot (factor, stock, city, hold, expose) or all; got '{part}'.");
				}
			}
		}
		if (DoseHotActsOnly && Dose <= 0f) throw new ArgumentException(DoseHotActsSwitch + " needs " + DoseSwitch + "=D.");
		if (doseExposure.HasValue && Dose <= 0f) throw new ArgumentException(DoseExposureSwitch + " needs " + DoseSwitch + "=D.");
		configured = true;
	}

	internal static void SetDoseHotActsForChecks(bool on) { configured = true; DoseHotActsOnly = on; }
	internal static void SetDoseExposureForChecks(float? chance) { configured = true; doseExposure = chance; }

	/// <summary>Whether the act had a Top 40 within <see cref="HotWindowWeeks"/> of <paramref name="week"/>.</summary>
	/// The clock is configured after this switch, so it is checked here: without it no in-run Top 40 is ever recorded.
	public static bool HotAct(SimulatedArtist artist, int week) {
		if (!HitActCadence.ClockEnabled)
			throw new InvalidOperationException(DoseHotActsSwitch + " reads the hit-act Top 40 clock (--known-act-airplay or a hit-act cadence switch).");
		return artist != null && week > 0 && HitActCadence.WeeksSinceTop40(artist, week) <= HotWindowWeeks;
	}

	internal static void SetForChecks(bool promo, bool foot) { configured = true; Promo = promo; SetFoot(foot); footScope = 0; }
	internal static void SetDoseForChecks(float dose) { configured = true; Dose = dose; doseScope = 0; doseRecordId = null; }
	internal static void SetFootPartsForChecks(bool factor, bool stock, bool city, bool hold, bool expose) {
		configured = true; Factor = factor; Stock = stock; City = city; Hold = hold; Expose = expose; footScope = 0;
	}

	/// <summary>A live AI single: the only launches the ablation touches.</summary>
	public static bool AppliesTo(Record record, AILabel label) =>
		record != null && label != null && !label.isPlayerOwned && !record.isPlayerOwned && record.format != ReleaseFormat.Album;

	public static bool AppliesTo(RecordRuntimeData record, AILabel label) =>
		record?.baseRecord != null && AppliesTo(record.baseRecord, label) && record.baseRecord.releaseDate > GameDate.StartDate;

	public static bool PromoFor(RecordRuntimeData record, AILabel label) => Promo && AppliesTo(record, label);

	/// <summary>The label's campaign impact, or the reference for a live AI single under promo.</summary>
	public static float Impact(float impact, RecordRuntimeData record, AILabel label) => PromoFor(record, label) ? ReferenceImpact : impact;
	public static float Reputation(RecordRuntimeData record, AILabel label) => PromoFor(record, label) ? ReferenceReputation : label.reputation;
	public static float Payola(RecordRuntimeData record, AILabel label) => PromoFor(record, label) ? ReferencePayola : label.payolaWillingness;

	/// <summary>Opens the footprint scope for one launch; the launch-footprint reads below see it until disposed.</summary>
	/// <paramref name="artist"/> and <paramref name="week"/> are read only under <see cref="DoseHotActsOnly"/>.
	public static FootScope Launch(RecordRuntimeData record, AILabel label, SimulatedArtist artist = null, int week = 0) =>
		new(Foot && AppliesTo(record, label), Dose > 0f && label?.tier != LabelTier.Major && AppliesTo(record, label)
			&& (!DoseHotActsOnly || HotAct(artist, week)) ? record.baseRecord.recordId : null);

	/// <summary>Inside a dosed launch: whether this region is opened to the reference footprint. False whenever the dose is off.</summary>
	public static bool Opened(string regionId) =>
		doseScope > 0 && doseRecordId != null && regionId != null && BandLife.Unit("blind-dose|" + doseRecordId + "|" + regionId) < Dose;

	/// <summary>The launch factor an opened region reads: never below the reference covered factor. Identity when not opened.</summary>
	public static float DoseFactor(string regionId, float factor) => factor < ReferenceFactor && Opened(regionId) ? ReferenceFactor : factor;

	/// <summary>Inside a live AI single's launch under any foot part.</summary>
	public static bool InFoot => footScope > 0;
	public static bool InFactor => InFoot && Factor;
	public static bool InStock => InFoot && Stock;
	public static bool InCity => InFoot && City;
	public static bool InHold => InFoot && Hold;
	public static bool InExpose => InFoot && Expose;

	public readonly struct FootScope : IDisposable {
		private readonly bool active;
		private readonly string dosed, previousDosed;
		public FootScope(bool active, string dosedRecordId = null) {
			this.active = active; dosed = dosedRecordId; previousDosed = doseRecordId;
			if (active) footScope++;
			if (dosed != null) { doseScope++; doseRecordId = dosed; }
		}
		public void Dispose() {
			if (active) footScope--;
			if (dosed != null) { doseScope--; doseRecordId = previousDosed; }
		}
	}
}
