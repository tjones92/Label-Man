using System;
using System.Diagnostics;
using Godot;

/// <summary>Opt-in startup timings; never consumes simulation randomness or changes initialization.</summary>
public static class StartupProfile {
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static long previous;
    public static void Mark(string stage) {
        if (!Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--profile-startup")) return;
        long now = Clock.ElapsedMilliseconds;
        GD.Print($"STARTUP_PROFILE stage={stage} elapsed_ms={now} stage_ms={now - previous}");
        previous = now;
    }
}
