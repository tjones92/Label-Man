using System;
using System.Linq;

/// <summary>Player-only rollout. Observe retains the prior batch date; enabled uses incremental work.</summary>
public static class StudioExperience {
    public enum Rollout { Off, Observe, Enabled }
    public static Rollout Mode { get; private set; } = Rollout.Enabled;
    public static bool Enabled => Mode == Rollout.Enabled;
    public static void Configure(string[] args) {
        var flags = args.Where(a => a.StartsWith("--studio-experience=", StringComparison.Ordinal)).ToArray();
        if (flags.Length > 1) throw new ArgumentException("Supply one studio experience mode.");
        Mode = flags.Length == 0 ? Rollout.Enabled : flags[0][20..] switch {
            "off" => Rollout.Off, "observe" => Rollout.Observe, "enabled" => Rollout.Enabled,
            _ => throw new ArgumentException("Studio experience must be off, observe or enabled.")
        };
    }
}
