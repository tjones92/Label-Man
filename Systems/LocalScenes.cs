using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Phase 1: identity observation only, off by default; no population/economic reader switches.</summary>
public static class LocalScenes {
    public static bool Observing { get; private set; }
    public static bool Persisting { get; private set; }
    public static void ValidateDependencies() {
        if (Persisting && !ArtistPopulationLifecycle.Enabled)
            throw new ArgumentException("Persistent scenes require the existing artist population lifecycle birth owner.");
    }
    public static void Configure(IEnumerable<string> arguments) {
        string[] args = (arguments ?? Array.Empty<string>()).ToArray();
        bool observe = args.Contains("--observe-local-scenes", StringComparer.Ordinal);
        bool disable = args.Contains("--disable-local-scenes", StringComparer.Ordinal);
        bool persist = args.Contains("--enable-persistent-scenes", StringComparer.Ordinal);
        if (persist && (disable || args.Contains("--disable-persistent-scenes", StringComparer.Ordinal)))
            throw new ArgumentException("Persistent scenes conflict with the requested scene disable flag.");
        if (observe && disable) throw new ArgumentException("--observe-local-scenes conflicts with --disable-local-scenes.");
        if (args.Any(a => a is "--enable-local-scenes" or "--enable-scene-recruitment" or "--enable-scene-dynamics"))
            throw new ArgumentException("Use --enable-persistent-scenes for persistence. Geographic recruitment and dynamics are not implemented.");
        Observing = observe || persist;
        Persisting = persist;
    }
}
