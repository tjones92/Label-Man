using System;
using System.Collections.Generic;

/// <summary>
/// Feature boundary for regional audience causality (SimTools/RegionalAudienceDesign.md, audit step 3). Each
/// sub-step has its own switch so it can be measured alone and kept off as the decade A/B's control. All of them
/// change which state a calculation reads, never how many draws it makes.
/// <para>
/// <see cref="LocalLifecycleClock"/> (3b): a single's post-week-8 demand-age decay in a region runs on that
/// region's local age (weeks since the record came into play there) instead of the record's national age, so a
/// region that discovers a record late gets its own life rather than inheriting a record already aged elsewhere.
/// Live (Genre Market V2) demand only; the frozen pre-live route is unchanged.
/// </para>
/// </summary>
public static class RegionalAudience {
	private static bool configured;
	private static bool localLifecycleClock;
	public static bool LocalLifecycleClock => localLifecycleClock;

	public static void Configure(bool localLifecycleClockDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--local-lifecycle-clock") enable = true;
			if (argument == "--no-local-lifecycle-clock") disable = true;
		}
		if (enable && disable)
			throw new ArgumentException("--local-lifecycle-clock and --no-local-lifecycle-clock cannot be used together.");
		localLifecycleClock = enable || (!disable && localLifecycleClockDefault);
		configured = true;
	}

	internal static void ConfigureForProbe(bool localClock) { configured = true; localLifecycleClock = localClock; }
}
