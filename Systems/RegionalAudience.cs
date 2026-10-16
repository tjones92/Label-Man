using System;
using System.Collections.Generic;

/// <summary>
/// Feature boundary for regional audience causality (SimTools/RegionalAudienceDesign.md, audit step 3). Each
/// sub-step has its own switch so it can be measured alone and kept off as the decade A/B's control.
/// <para>
/// <see cref="LocalLifecycleClock"/> (3b): a single's post-week-8 demand-age decay in a region runs on that
/// region's local age (weeks since the record came into play there) instead of the record's national age, so a
/// region that discovers a record late gets its own life rather than inheriting a record already aged elsewhere.
/// Live (Genre Market V2) demand only; the frozen pre-live route is unchanged.
/// </para>
/// <para>
/// <see cref="LocalRadioFatigue"/> (3c): a region's stations drop a record on that region's own sales support and
/// weeks since its local best week, and the rotation a region's stations move toward blends national heat with
/// the heat its own sales list would earn (<see cref="LocalHeatWeight"/>). Changes the drop-roll count, so the RNG
/// stream moves with it.
/// </para>
/// </summary>
public static class RegionalAudience {
	private static bool configured;
	private static bool localLifecycleClock, localRadioFatigue;
	public static bool LocalLifecycleClock => localLifecycleClock;
	public static bool LocalRadioFatigue => localRadioFatigue;
	/// <summary>λ: the share of a region's radio target taken from its own sales list; national heat keeps the rest.</summary>
	public const float LocalHeatWeight = 0.35f;

	public static void Configure(bool localLifecycleClockDefault, bool localRadioFatigueDefault, IEnumerable<string> arguments) {
		if (configured) return;
		var args = new List<string>(arguments ?? Array.Empty<string>());
		localLifecycleClock = Switch(args, "local-lifecycle-clock", localLifecycleClockDefault);
		localRadioFatigue = Switch(args, "local-radio-fatigue", localRadioFatigueDefault);
		configured = true;
	}

	private static bool Switch(List<string> args, string name, bool sceneDefault) {
		bool enable = args.Contains("--" + name), disable = args.Contains("--no-" + name);
		if (enable && disable) throw new ArgumentException($"--{name} and --no-{name} cannot be used together.");
		return enable || (!disable && sceneDefault);
	}

	internal static void ConfigureForProbe(bool localClock, bool localRadio = false) {
		configured = true; localLifecycleClock = localClock; localRadioFatigue = localRadio;
	}
}
