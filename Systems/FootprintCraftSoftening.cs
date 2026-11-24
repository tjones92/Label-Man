using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Two softening screens (SimTools/MajorShareRootCauseHandoff.md, candidate screens a and b). Each keeps the Majors'
/// advantage, better distribution or better studios, and narrows how far it reaches. Both are off unless named and
/// neither draws a random number.
///
/// --connected-arrival=L: under --regional-arrival, a region a live AI single's label does not ship to holds the single
/// until a local-access path fires. With this switch, a single whose commercial fit connected also arrives in a held
/// region L weeks after that region is fit-exposed (at launch, by a neighbour's local top 40, or by the national chart).
/// It models independent distributors and one-stops picking up a regional hit, a little late. Covered and strong
/// regions still get the record at launch, so the label that ships nationally keeps its head start. Flops are never
/// released, and a region that is not fit-exposed still waits for the existing paths.
///
/// --label-craft-floor=F: a single's production reads 0.4 x the label's production quality. In that term, a label below
/// <see cref="CraftReference"/> (the Major envelope's midpoint) reads its value moved F of the way up to it. The order
/// of labels is kept (Major over MidTier over Independent over Small), the Majors' draw at or above the reference is
/// untouched, and the gap below it shrinks by the share 1 − F. The label's stored value, albums, LP tracks and the
/// slate's estimate are unchanged.
///
/// --hit-act-craft-floor=F: the same lift, only for a live non-Major AI single whose act had a Top 40 in the last
/// <see cref="LabelBlindLaunch.HotWindowWeeks"/> weeks when the record is cut (the HitActCadence clock). It models a
/// label spending its best studio, arranger and session players on a proven act (FollowUpGapFableHandoff.md: label
/// craft is half the quality gap left under he095). With --label-craft-floor the larger of the two lifts applies.
/// </summary>
public static class FootprintCraftSoftening {
	public const string ArrivalSwitch = "--connected-arrival";
	public const string CraftSwitch = "--label-craft-floor";
	public const float CraftReference = 0.85f;
	private static bool configured;
	/// <summary>Weeks after fit exposure a connected single arrives in a held region; −1 when off.</summary>
	public static int ArrivalLag { get; private set; } = -1;
	public static bool ConnectedArrival => ArrivalLag >= 0;
	public static float CraftFloor { get; private set; }
	public const string HitActCraftSwitch = "--hit-act-craft-floor";
	public static float HitActCraftFloor { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		ArrivalLag = -1; CraftFloor = 0f; HitActCraftFloor = 0f;
		foreach (string arg in arguments ?? Array.Empty<string>()) {
			if (arg.StartsWith(ArrivalSwitch + "=", StringComparison.Ordinal)) {
				if (!int.TryParse(arg.Substring(ArrivalSwitch.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lag) ||
					lag < 0 || lag > 8)
					throw new ArgumentException($"{ArrivalSwitch} takes a lag from 0 to 8 weeks.");
				ArrivalLag = lag;
			}
			if (arg.StartsWith(CraftSwitch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(CraftSwitch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
					!float.IsFinite(f) || f < 0f || f > 1f)
					throw new ArgumentException($"{CraftSwitch} must be between 0 and 1.");
				CraftFloor = f;
			}
			if (arg.StartsWith(HitActCraftSwitch + "=", StringComparison.Ordinal)) {
				if (!float.TryParse(arg.Substring(HitActCraftSwitch.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
					!float.IsFinite(f) || f < 0f || f > 1f)
					throw new ArgumentException($"{HitActCraftSwitch} must be between 0 and 1.");
				HitActCraftFloor = f;
			}
		}
		if (ConnectedArrival || CraftFloor > 0f || HitActCraftFloor > 0f)
			Godot.GD.Print($"FOOTPRINT_CRAFT_SOFTENING connectedArrivalLag={ArrivalLag} craftFloor={CraftFloor:F2} hitActCraftFloor={HitActCraftFloor:F2}");
		configured = true;
	}

	/// <summary>Called by the chart manager once regional arrival and fit exposure are configured.</summary>
	public static void Validate() {
		EnsureConfigured();
		if (ConnectedArrival && !(RegionalAudience.RegionalArrival && RegionalFitExposure.Enabled))
			throw new ArgumentException($"{ArrivalSwitch} requires --regional-arrival and --regional-fit-exposure.");
	}

	/// <summary>Record generation can run before the chart manager configures; read the arguments then.</summary>
	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }

	internal static void SetForChecks(int arrivalLag, float craftFloor, float hitActCraftFloor = 0f) {
		configured = true; ArrivalLag = arrivalLag; CraftFloor = craftFloor; HitActCraftFloor = hitActCraftFloor;
	}

	/// <summary>The label's production quality as a single's production reads it. Returns <paramref name="quality"/> exactly when off.</summary>
	public static float Craft(float quality, ReleaseFormat format) => Craft(quality, format, null, null, 0);

	/// <summary>As above; under --hit-act-craft-floor a hot act's live non-Major AI single at chart week
	/// <paramref name="week"/> reads the larger lift. Reads the act's clock only while that switch is on.</summary>
	public static float Craft(float quality, ReleaseFormat format, AILabel label, SimulatedArtist artist, int week) {
		EnsureConfigured();
		// --midtier-hit-act-package=craft: a MidTier's hot act reads the reference; off, the input unchanged.
		quality = MidTierHitActPackage.CraftRead(quality, format, label, artist, week);
		float floor = CraftFloor;
		if (HitActCraftFloor > floor && format == ReleaseFormat.Single && quality < CraftReference && HitActLift(label, artist, week))
			floor = HitActCraftFloor;
		return floor <= 0f || format != ReleaseFormat.Single || quality >= CraftReference
			? quality : quality + (CraftReference - quality) * floor;
	}

	private static bool HitActLift(AILabel label, SimulatedArtist artist, int week) {
		if (label == null || artist == null || label.isPlayerOwned || label.tier == LabelTier.Major || week <= 0) return false;
		if (!HitActCadence.ClockEnabled)
			throw new InvalidOperationException(HitActCraftSwitch + " reads the hit-act Top 40 clock (--known-act-airplay or a hit-act cadence switch).");
		return HitActCadence.WeeksSinceTop40(artist, week) <= LabelBlindLaunch.HotWindowWeeks;
	}

	/// <summary>Whether a held region takes a connected single this week. False whenever the switch is off.</summary>
	public static bool Arrives(RecordRuntimeData record, RegionalRecordData data, AILabel label) {
		if (!ConnectedArrival || data == null || label == null || record?.baseRecord == null) return false;
		if (record.baseRecord.format == ReleaseFormat.Album || data.fitExposureAge == RegionalFitExposure.NotExposed) return false;
		return record.weeksSinceRelease >= Math.Max(1, data.fitExposureAge) + ArrivalLag && SingleCommercialFit.Connected(record, label);
	}
}
