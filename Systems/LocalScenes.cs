using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Persistent discovery defaults on; explicit off and identity-only modes preserve regression controls.</summary>
public static class LocalScenes {
    public static bool Observing { get; private set; }
    public static bool Persisting { get; private set; }
    public static bool Rooms { get; private set; }
    public static void ValidateDependencies() {
        if (Persisting && !ArtistPopulationLifecycle.Enabled)
            throw new ArgumentException("Persistent scenes require the existing artist population lifecycle birth owner.");
    }
    public static void Configure(IEnumerable<string> arguments) {
        string[] args = (arguments ?? Array.Empty<string>()).ToArray();
        bool observe = args.Contains("--observe-local-scenes", StringComparer.Ordinal);
        bool disable = args.Contains("--disable-local-scenes", StringComparer.Ordinal);
        bool persist = args.Contains("--enable-persistent-scenes", StringComparer.Ordinal);
        bool disablePersistence = args.Contains("--disable-persistent-scenes", StringComparer.Ordinal);
        if (persist && (disable || args.Contains("--disable-persistent-scenes", StringComparer.Ordinal)))
            throw new ArgumentException("Persistent scenes conflict with the requested scene disable flag.");
        if (observe && disable) throw new ArgumentException("--observe-local-scenes conflicts with --disable-local-scenes.");
        if (args.Any(a => a is "--enable-local-scenes" or "--enable-scene-recruitment" or "--enable-scene-dynamics"))
            throw new ArgumentException("Use --enable-persistent-scenes for persistence. Geographic recruitment and dynamics are not implemented.");
        // An explicit observation/disable mode wins over the development default.
        // Dependency-off controls remain clean; explicit enable still requires validation.
        bool defaultPersistence = !observe && !disable && !disablePersistence
            && !args.Contains("--disable-artist-population-lifecycle", StringComparer.Ordinal)
            && !args.Contains("--disable-genre-market-v2", StringComparer.Ordinal);
        persist |= defaultPersistence;

        bool enableRooms = args.Contains("--enable-scene-rooms", StringComparer.Ordinal);
        bool disableRooms = args.Contains("--disable-scene-rooms", StringComparer.Ordinal);
        if (enableRooms && (disableRooms || !persist))
            throw new ArgumentException("Scene rooms require persistence and cannot be enabled and disabled together.");
        Observing = observe || persist;
        Persisting = persist;
        Rooms = persist && !disableRooms;
    }
}
