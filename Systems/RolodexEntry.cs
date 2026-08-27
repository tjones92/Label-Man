using System.Collections.Generic;
using Godot;

/// <summary>Discovery ladder for a DJ contact. Unknown DJs have no entry; the first entry starts at
/// HeardOf. Known/Trusted are reached by <see cref="RolodexEntry.MaybePromoteState"/> as rapport climbs;
/// the ladder only ever ratchets forward -- a later dip in rapport does not erase earned trust.</summary>
public enum DiscoveryState { HeardOf, Introduced, Known, Trusted }

/// <summary>Derived relationship tier for card display. Computed from rt.labelRapport at render time.
/// Burned overrides the rapport read entirely -- shown only when BOTH burn channels are gone; a
/// single-channel burn (see <see cref="RolodexEntry.payolaBurned"/>/<see cref="RolodexEntry.professionallyBurned"/>)
/// still shows the underlying tier plus a channel-specific warning, since it isn't the whole relationship.</summary>
public enum RapportTier { Stranger, Cold, Acquaintance, Friendly, Warm, Loyal, Burned }

/// <summary>
/// The player's read on one reporter DJ. A thin wrapper: all causal truth lives on <see cref="Deejay"/>
/// and <see cref="StationRuntime.labelRapport"/> — nothing here is duplicated from those. The card reads
/// them at render time through <see cref="RolodexEntry.ClassifyRapport"/>.
/// </summary>
public sealed class RolodexEntry {
    public string djId;          // → ChartManager.GetDeejay(djId)
    public string stationId;     // → ChartManager.GetRadioStation(stationId)
    public DiscoveryState state;
    public string displayName;   // synthesized at discovery (e.g. "Wolfman Larry Bell")
    public string portraitKey;   // archetype-based key for the portrait set (Phase 2: archetype monogram)
    public bool youOweThem;      // you owe them (Rolodex Phase 5: set when a personal pitch flops on them)
    public bool theyOweThem;     // they owe you (set when a pitched record actually delivers); spent by Ask a Favor

    // Channel-specific burn (Rolodex Phase 5): burned-for-payola is not burned-professionally.
    // Set by a payola bust (ProcessPayolaScandals) -- the cash channel specifically is too hot. Blocks
    // Payola only; Personal Pitch and the pure-business Ad-Buy still work if the DJ is still in the chair.
    public bool payolaBurned;
    // Set the first time you learn when this jock is actually at the station -- by reaching him, or by
    // being told he is not in. Until then the card cannot tell you what hours to try, which is the
    // whole reason the first cold call is worth something.
    public bool shiftKnown;
    // Set when a Personal Pitch's record-memory settles badly (ProcessRecordMemories) -- he trusted your
    // ear and got burned. Blocks Personal Pitch only; a straight cash Ad-Buy needs no trust, and Payola
    // is untouched by it.
    public bool professionallyBurned;

    // Directive §3.3: OfferToBringIt sets a soft appointment -- "he'll expect you" -- consumed by a
    // Wait for Him visit at this DJ's station stop for a bonus, and lost (a small rapport cost, applied
    // when it expires unfulfilled) if you never show. Empty recordId = no appointment standing.
    public string appointmentRecordId = "";
    public int appointmentExpiresWeek;

    // Record-memory (Rolodex Phase 5): a Personal Pitch stakes your word on a record. Settled some weeks
    // later against the record's ACTUAL sales since the pitch -- never invented, always the real settlement.
    public sealed class PendingRecordMemory {
        public string recordId;
        public string recordTitle;
        public int evalWeek;        // chart week the claim settles
        public long unitsAtPitch;   // RecordRuntimeData.totalUnitsSold baseline at the moment of the pitch
    }
    public List<PendingRecordMemory> pendingMemories = new();

    public List<string> log = new();

    /// <summary>Derive the relationship tier from the live rapport score. One source of truth.</summary>
    public static RapportTier ClassifyRapport(float rapport, DiscoveryState state) {
        if (rapport >= 0.6f) return RapportTier.Loyal;
        if (rapport >= 0.35f) return RapportTier.Warm;
        if (rapport >= 0.15f) return RapportTier.Friendly;
        if (rapport > 0f)     return RapportTier.Acquaintance;
        return state == DiscoveryState.HeardOf ? RapportTier.Stranger : RapportTier.Cold;
    }

    /// <summary>The tier the card actually shows. Fully "Burned Bridge" only when both channels are gone;
    /// a single-channel burn keeps showing the real rapport tier (the UI layers a channel warning on top).</summary>
    public static RapportTier EffectiveTier(RolodexEntry entry, float rapport) =>
        entry != null && entry.payolaBurned && entry.professionallyBurned
            ? RapportTier.Burned
            : ClassifyRapport(rapport, entry?.state ?? DiscoveryState.HeardOf);

    /// <summary>Ratchet the discovery state up as rapport earns it. Never downgrades -- trust already
    /// earned survives a later cold spell. Call after any action that raises rapport.</summary>
    public void MaybePromoteState(float rapport) {
        if (state == DiscoveryState.Trusted) return;
        DiscoveryState target =
            rapport >= 0.6f ? DiscoveryState.Trusted :
            rapport >= 0.35f ? DiscoveryState.Known :
            state;
        if (target > state) state = target;
    }

    public static string TierLabel(RapportTier tier) => tier switch {
        RapportTier.Stranger     => "Stranger",
        RapportTier.Cold         => "Cold",
        RapportTier.Acquaintance => "Acquaintance",
        RapportTier.Friendly     => "Friendly",
        RapportTier.Warm         => "Warm",
        RapportTier.Loyal        => "Loyal",
        RapportTier.Burned       => "Burned Bridge",
        _                        => "Unknown"
    };

    public static Color TierColor(RapportTier tier) => tier switch {
        RapportTier.Stranger     => new Color("8a8a8a"),
        RapportTier.Cold         => new Color("6b5a3a"),
        RapportTier.Acquaintance => new Color("8a7040"),
        RapportTier.Friendly     => new Color("4a7a4a"),
        RapportTier.Warm         => new Color("4a8a5a"),
        RapportTier.Loyal        => new Color("c8a000"),
        RapportTier.Burned       => new Color("8a2a2a"),
        _                        => new Color("8a8a8a")
    };

    /// <summary>One-line archetype character read — what the card shows at a glance.</summary>
    public static string ArchetypeBlurb(DJArchetype arch) => arch switch {
        DJArchetype.Personality => "Personality jock. Plays what catches his ear.",
        DJArchetype.Tastemaker  => "Serious tastemaker. Breaks acts before the sales do.",
        DJArchetype.Hustler     => "Hustle first. Cash opens doors.",
        DJArchetype.CompanyMan  => "Follows the sheet. Play the format, he plays the record.",
        DJArchetype.Regional    => "Local loyalist. Knows his market better than you do.",
        _                        => "DJ."
    };
}

// ── Save / Load ──────────────────────────────────────────────────────────────────────────

/// <summary>Flat save record for one pending <see cref="RolodexEntry.PendingRecordMemory"/>.</summary>
public sealed class PendingRecordMemorySaveData {
    public string RecordId    { get; set; }
    public string RecordTitle { get; set; }
    public int    EvalWeek    { get; set; }
    public long   UnitsAtPitch{ get; set; }

    public static PendingRecordMemorySaveData From(RolodexEntry.PendingRecordMemory m) => new() {
        RecordId = m.recordId, RecordTitle = m.recordTitle, EvalWeek = m.evalWeek, UnitsAtPitch = m.unitsAtPitch
    };
    public RolodexEntry.PendingRecordMemory ToMemory() => new() {
        recordId = RecordId, recordTitle = RecordTitle, evalWeek = EvalWeek, unitsAtPitch = UnitsAtPitch
    };
}

/// <summary>Flat save record for one <see cref="RolodexEntry"/>.</summary>
public sealed class RolodexEntrySaveData {
    public string DjId          { get; set; }
    public string StationId     { get; set; }
    public int    StateOrdinal  { get; set; }
    public string DisplayName   { get; set; }
    public string PortraitKey   { get; set; }
    public bool   YouOweThem    { get; set; }
    public bool   TheyOweThem   { get; set; }
    public bool   PayolaBurned  { get; set; }
    public bool   ShiftKnown    { get; set; }
    public bool   ProfessionallyBurned { get; set; }
    public string AppointmentRecordId  { get; set; } = "";
    public int    AppointmentExpiresWeek { get; set; }
    public List<PendingRecordMemorySaveData> PendingMemories { get; set; } = new();
    public List<string> Log     { get; set; } = new();

    public static RolodexEntrySaveData From(RolodexEntry e) => new() {
        DjId = e.djId, StationId = e.stationId, StateOrdinal = (int)e.state,
        DisplayName = e.displayName, PortraitKey = e.portraitKey,
        YouOweThem = e.youOweThem, TheyOweThem = e.theyOweThem,
        PayolaBurned = e.payolaBurned, ProfessionallyBurned = e.professionallyBurned,
        ShiftKnown = e.shiftKnown,
        AppointmentRecordId = e.appointmentRecordId, AppointmentExpiresWeek = e.appointmentExpiresWeek,
        PendingMemories = e.pendingMemories.ConvertAll(PendingRecordMemorySaveData.From),
        Log = new List<string>(e.log)
    };

    public RolodexEntry ToEntry() => new() {
        djId = DjId, stationId = StationId,
        state = (DiscoveryState)System.Math.Clamp(StateOrdinal, 0, 3),
        displayName = DisplayName ?? "Unknown DJ", portraitKey = PortraitKey ?? "CompanyMan",
        youOweThem = YouOweThem, theyOweThem = TheyOweThem,
        payolaBurned = PayolaBurned, professionallyBurned = ProfessionallyBurned,
        shiftKnown = ShiftKnown,
        appointmentRecordId = AppointmentRecordId ?? "", appointmentExpiresWeek = AppointmentExpiresWeek,
        pendingMemories = (PendingMemories ?? new List<PendingRecordMemorySaveData>()).ConvertAll(m => m.ToMemory()),
        log = Log ?? new List<string>()
    };
}
