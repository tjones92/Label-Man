using System;
using System.Linq;

/// <summary>Player-only rollout. Observe retains the prior batch date; enabled uses incremental work.</summary>
public static class StudioExperience {
    public enum Rollout { Off, Observe, Enabled }
    public static Rollout Mode { get; private set; } = Rollout.Enabled;
    public static bool Enabled => Mode == Rollout.Enabled;
    /// <summary>Whether new dates can break down or flub (default on). <c>--studio-breakdowns=off</c> books dates as before.</summary>
    public static bool Breakdowns { get; private set; } = true;
    public static void Configure(string[] args) {
        var breakdowns = args.Where(a => a.StartsWith("--studio-breakdowns=", StringComparison.Ordinal)).ToArray();
        if (breakdowns.Length > 1) throw new ArgumentException("Supply one studio breakdown mode.");
        Breakdowns = breakdowns.Length == 0 || breakdowns[0][20..] switch {
            "on" => true, "off" => false, _ => throw new ArgumentException("Studio breakdowns must be on or off.")
        };
        var flags = args.Where(a => a.StartsWith("--studio-experience=", StringComparison.Ordinal)).ToArray();
        if (flags.Length > 1) throw new ArgumentException("Supply one studio experience mode.");
        Mode = flags.Length == 0 ? Rollout.Enabled : flags[0][20..] switch {
            "off" => Rollout.Off, "observe" => Rollout.Observe, "enabled" => Rollout.Enabled,
            _ => throw new ArgumentException("Studio experience must be off, observe or enabled.")
        };
    }
}
