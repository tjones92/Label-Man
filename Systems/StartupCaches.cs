using System;
using Godot;

/// <summary>Scoped caches of immutable startup reads. Disable for matched-world regression comparisons.</summary>
public static class StartupCaches {
    public static bool Enabled => !Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--disable-startup-caches");
}
