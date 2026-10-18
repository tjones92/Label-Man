using System;
using Godot;

/// <summary>Scoped caches of immutable startup reads. Disable for matched-world regression comparisons.</summary>
public static class StartupCaches {
    public static bool Enabled => !Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--disable-startup-caches");
    /// <summary>--verify-runtime-caches: incremental runtime caches (standards pools, the person pool's order)
    /// recompute from scratch on every read and throw on any difference. Slow; for validation runs only.</summary>
    public static readonly bool Verify = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--verify-runtime-caches");
}
