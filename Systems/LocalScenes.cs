using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Phase 1: identity observation only, off by default; no population/economic reader switches.</summary>
public static class LocalScenes {
    public static bool Observing { get; private set; }
    public static void Configure(IEnumerable<string> arguments) {
        string[] args = (arguments ?? Array.Empty<string>()).ToArray();
        bool observe = args.Contains("--observe-local-scenes", StringComparer.Ordinal);
        bool disable = args.Contains("--disable-local-scenes", StringComparer.Ordinal);
        if (observe && disable) throw new ArgumentException("--observe-local-scenes conflicts with --disable-local-scenes.");
        if (args.Any(a => a is "--enable-local-scenes" or "--enable-scene-recruitment" or "--enable-scene-dynamics"))
            throw new ArgumentException("Persistent scenes, recruitment and dynamics are not implemented in Phase 1. Use --observe-local-scenes.");
        Observing = observe;
    }
}
