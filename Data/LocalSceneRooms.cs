using System;
using System.Collections.Generic;

public enum SceneRoomKind { Club, ListeningRoom, Coffeehouse, CommunityHall, Roadhouse, TradeEvent }
public enum SceneBillStatus { Scheduled, Performed, Cancelled }

public sealed record SceneRoomProfile(string Id, string PlaceId, string Name, SceneRoomKind Kind,
    PlayerDesk.ScoutingVenue Category, string Community, string LateCommunity, string Source,
    IReadOnlyList<GenreFamily> Families, IReadOnlyList<DayOfWeek> Nights, int OpenHour, int CloseHour,
    int Capacity, float Admission, string ContactId, string ContactName) {
    public bool Fictional => true;
    public int FromYear => 1960;
    public int ThroughYear => 1969;
    public bool IsPerformance => Kind != SceneRoomKind.TradeEvent;
    /// <summary>The booker's name with the role and room, for provenance lines that must say who could know.</summary>
    public string ContactTitle => $"{ContactName}, booker at {Name}";
    public string Programming(int year) => year < 1965 ? Community : LateCommunity;
}

public sealed class SceneSetSong {
    public string SongId { get; set; }
    public string Title { get; set; }
    public Genre Genre { get; set; }
    public bool Original { get; set; }
    public SongContentContext ContentContext { get; set; }
}
public sealed class SceneAppearance {
    public ScenePerformanceSnapshot Performance { get; set; }
    public string ArtistId { get; set; }
    public List<string> PersonIds { get; set; } = new();
    public string Role { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public bool Residency { get; set; }
    public SceneBillStatus Status { get; set; }
    public string CancellationReason { get; set; }
    public float Fee { get; set; }
    public List<SceneSetSong> Set { get; set; } = new();
    /// <summary>House-band players backing the act (not members of it). Null on older saves.</summary>
    public List<string> BackingPersonIds { get; set; }
}
public sealed class SceneBill {
    public string Id { get; set; }
    public string RoomId { get; set; }
    public int Day { get; set; }
    public int Year { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    /// <summary>The room's capacity when the bill was made (0 on bills saved before live calibration).</summary>
    public int Capacity { get; set; }
    public int ExpectedAudience { get; set; }
    public int Attendance { get; set; }
    public float GrossReceipts { get; set; }
    public SceneBillStatus Status { get; set; }
    public string Response { get; set; }
    public List<SceneAppearance> Appearances { get; set; } = new();
}
public sealed class SceneEngagement {
    public string RoomId { get; set; }
    public string ArtistId { get; set; }
    public int StartDay { get; set; }
    public int ThroughDay { get; set; }
}
public sealed class SceneWorkAccount {
    public string ArtistId { get; set; }
    public string PersonId { get; set; }
    public int Year { get; set; }
    public float StageHours { get; set; }
    public float FeeShare { get; set; }
    public float AttributedHours { get; set; }
    public float BackgroundHours { get; set; }
    public float UnbudgetedHours { get; set; }
    public float AttributedRoadHours { get; set; }
    public float BackgroundRoadHours { get; set; }
    /// <summary>House-band work backing ArtistId, which is not this person's own act.</summary>
    public bool Backing { get; set; }
}
public sealed class SceneRoomStanding {
    public string RoomId { get; set; }
    public string ArtistId { get; set; }
    public int Shows { get; set; }
    public int LastDay { get; set; }
    /// <summary>Appearances before the calendar began (the world-start pre-roll). History the room remembers;
    /// never counted toward a live bill's expected audience. Zero on legacy saves.</summary>
    public int PriorShows { get; set; }
    /// <summary>The room's record of who came in for this act (Phase 5): sets attributed, people who came for them, and the
    /// share of the room that stayed, summed. Zero on older saves.</summary>
    public int DrawSets { get; set; }
    public int DrawTotal { get; set; }
    public float StayedTotal { get; set; }
}
public sealed class SceneRoomSaveData {
    public long ConversationOrdinal { get; set; }
    public List<SceneConversation> Conversations { get; set; } = new();
    public int SchemaVersion { get; set; } = 1;
    public int ContentVersion { get; set; } = 1;
    public int LastScheduledWeek { get; set; } = -1;
    public int LastResolvedDay { get; set; } = -1;
    public int LastResolvedHour { get; set; } = -1;
    /// <summary>Weeks of room history pre-rolled at world start; zero on legacy saves and with the prewarm off.</summary>
    public int PriorWeeks { get; set; }
    // Current and following calendar week only. Lifetime counters are sparse ID-based summaries.
    public List<SceneBill> Bills { get; set; } = new();
    public List<SceneEngagement> Engagements { get; set; } = new();
    public List<SceneWorkAccount> Work { get; set; } = new();
    public List<SceneRoomStanding> Standing { get; set; } = new();
    public long CompletedPerformances { get; set; }
    /// <summary>House-band regulars: shows each player has backed in each room.</summary>
    public List<SceneHouseBandStanding> HouseStanding { get; set; } = new();
    /// <summary>Early-game §6.3: nights a booker gave the player's act. Empty in every AI-only world.</summary>
    public List<SceneBooking> Bookings { get; set; } = new();
    /// <summary>Early-game §6.3: what each booker last said to the player about an act, so a no stands until something changes.</summary>
    public List<SceneBookerPitch> BookerPitches { get; set; } = new();
}
/// <summary>A night a room's booker agreed to give an act the player pitched. Placed on the bill when that week is
/// scheduled, under every ordinary rule; it can still fail to play, and says why.</summary>
public sealed class SceneBooking {
    public string RoomId { get; set; }
    public string ArtistId { get; set; }
    public string LabelId { get; set; }
    public int Day { get; set; }
    /// <summary>Scheduled until the week is billed; then Performed or Cancelled from the act's own appearance.</summary>
    public SceneBillStatus Status { get; set; }
    public bool Billed { get; set; }
    public string Note { get; set; }
}
public sealed class SceneBookerPitch {
    public string RoomId { get; set; }
    public string ArtistId { get; set; }
    public string LabelId { get; set; }
    public int Day { get; set; }
    public bool Accepted { get; set; }
    /// <summary>The act's shows in the room when it was pitched: a no stands until this grows (or a term passes).</summary>
    public int ShowsAtPitch { get; set; }
}
public sealed class SceneHouseBandStanding {
    public string RoomId { get; set; }
    public string PersonId { get; set; }
    public int Shows { get; set; }
    public int LastDay { get; set; }
}
