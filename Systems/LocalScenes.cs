using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Persistent scenes, recruitment and validated feedback slices default on; explicit off modes preserve controls.</summary>
public static class LocalScenes {
    public static bool Observing { get; private set; }
    public static bool Persisting { get; private set; }
    public static bool Rooms { get; private set; }
    public static bool Recruitment { get; private set; }
    public static bool AttentionFeedback { get; private set; }
    public static bool AttentionFeedbackAudit { get; private set; }
    public static bool PriceFeedback { get; private set; }
    public static bool Institutions { get; private set; }
    public static bool Relocation { get; private set; }
    public static bool ExtendedWorld { get; private set; }
    /// <summary>Dated room size, admission and pay terms (SceneLiveEconomics). Observation-only; default on with rooms.</summary>
    public static bool LiveCalibration { get; private set; }
    /// <summary>Members of an act with no band-life hours allowance are credited their realized room hours.</summary>
    public static bool RoomGrowthCredit { get; private set; }
    /// <summary>Live pay reaches members' wealth (SceneLiveEconomics.MemberLiveIncome). Requires live calibration.</summary>
    public static bool LiveIncome { get; private set; }
    public static void ValidateDependencies() {
        if (Persisting && !ArtistPopulationLifecycle.Enabled)
            throw new ArgumentException("Persistent scenes require the existing artist population lifecycle birth owner.");
        if (Recruitment && (!Persisting || !GenreMarketV2.Enabled))
            throw new ArgumentException("Scene recruitment requires persistent scenes and the genre market.");
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
        if (args.Any(a => a is "--enable-local-scenes" or "--enable-scene-dynamics"))
            throw new ArgumentException("Use --enable-persistent-scenes for persistence and --enable-scene-recruitment for recruitment. Use --enable-scene-attention-feedback for the live discovery channel; --observe-scene-dynamics remains audit-only. Live price, institution and relocation channels have independent flags.");
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
        if (args.Contains("--observe-scene-dynamics", StringComparer.Ordinal) && (!persist || disableRooms))
            throw new ArgumentException("Scene dynamics observation requires persistence and rooms.");
        bool recruitment = args.Contains("--enable-scene-recruitment", StringComparer.Ordinal);
        if (recruitment && (args.Contains("--disable-scene-recruitment", StringComparer.Ordinal) || !persist || observe ||
            args.Contains("--disable-genre-market-v2", StringComparer.Ordinal)))
            throw new ArgumentException("Scene recruitment requires persistence and genre market, and conflicts with observe/disable modes.");
        bool feedback = args.Contains("--enable-scene-attention-feedback", StringComparer.Ordinal);
        bool effectiveRecruitment = recruitment || (persist && !observe
            && !args.Contains("--disable-scene-recruitment", StringComparer.Ordinal)
            && !args.Contains("--disable-genre-market-v2", StringComparer.Ordinal));
        if (feedback && (!effectiveRecruitment || disableRooms
            || args.Contains("--disable-scene-attention-feedback", StringComparer.Ordinal)))
            throw new ArgumentException("Live attention requires persistent rooms and recruitment; conflicting off modes are invalid.");
        bool price = args.Contains("--enable-scene-price-feedback", StringComparer.Ordinal);
        bool institutions = args.Contains("--enable-scene-institutions", StringComparer.Ordinal);
        bool relocation = args.Contains("--enable-scene-relocation", StringComparer.Ordinal);
        foreach (string slice in new[] { "price-feedback", "institutions", "relocation" })
            if (args.Contains("--enable-scene-" + slice, StringComparer.Ordinal) &&
                (args.Contains("--disable-scene-" + slice, StringComparer.Ordinal) || !effectiveRecruitment || disableRooms))
                throw new ArgumentException("Scene " + slice + " requires persistent rooms and recruitment and cannot be enabled and disabled together.");
        bool defaultSlices = effectiveRecruitment && !disableRooms;
        bool extended = args.Contains("--enable-scene-extended-world", StringComparer.Ordinal);
        if (extended && (!defaultSlices || args.Contains("--disable-scene-extended-world", StringComparer.Ordinal)))
            throw new ArgumentException("Extended source scenes require persistent rooms and recruitment and conflict with explicit disable.");
        institutions |= defaultSlices && !args.Contains("--disable-scene-institutions", StringComparer.Ordinal);
        price |= defaultSlices && !args.Contains("--disable-scene-price-feedback", StringComparer.Ordinal);
        relocation |= defaultSlices && institutions && !args.Contains("--disable-scene-relocation", StringComparer.Ordinal);
        if (relocation && !institutions)
            throw new ArgumentException("Scene relocation requires funded scene institutions.");
        AttentionFeedback = feedback || (effectiveRecruitment && !disableRooms &&
            !args.Contains("--disable-scene-attention-feedback", StringComparer.Ordinal));
        PriceFeedback = price;
        Institutions = institutions;
        Relocation = relocation;
        ExtendedWorld = extended || (defaultSlices && !args.Contains("--disable-scene-extended-world", StringComparer.Ordinal));
        AttentionFeedbackAudit = args.Contains("--scene-feedback-audit", StringComparer.Ordinal);
        SceneAttentionFeedback.Reset();
        Observing = observe || persist;
        Persisting = persist;
        Rooms = persist && !disableRooms;
        bool enableLive = args.Contains("--enable-scene-live-calibration", StringComparer.Ordinal);
        bool disableLive = args.Contains("--disable-scene-live-calibration", StringComparer.Ordinal);
        if (enableLive && (disableLive || !Rooms))
            throw new ArgumentException("Live calibration requires scene rooms and cannot be enabled and disabled together.");
        LiveCalibration = Rooms && !disableLive;
        foreach (string slice in new[] { "room-growth-credit", "member-live-income" })
            if (args.Contains("--enable-" + slice, StringComparer.Ordinal) &&
                (args.Contains("--disable-" + slice, StringComparer.Ordinal) || !LiveCalibration))
                throw new ArgumentException("--enable-" + slice + " requires live calibration and cannot be combined with its disable flag.");
        RoomGrowthCredit = Rooms && !args.Contains("--disable-room-growth-credit", StringComparer.Ordinal);
        LiveIncome = LiveCalibration && !args.Contains("--disable-member-live-income", StringComparer.Ordinal);
        Recruitment = recruitment || (persist && !observe
            && !args.Contains("--disable-scene-recruitment", StringComparer.Ordinal)
            && !args.Contains("--disable-genre-market-v2", StringComparer.Ordinal));
    }
}
