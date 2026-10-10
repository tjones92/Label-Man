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
}
public sealed class SceneRoomStanding {
    public string RoomId { get; set; }
    public string ArtistId { get; set; }
    public int Shows { get; set; }
    public int LastDay { get; set; }
}
public sealed class SceneRoomSaveData {
    public int SchemaVersion { get; set; } = 1;
    public int ContentVersion { get; set; } = 1;
    public int LastScheduledWeek { get; set; } = -1;
    public int LastResolvedDay { get; set; } = -1;
    public int LastResolvedHour { get; set; } = -1;
    // Current and following calendar week only. Lifetime counters are sparse ID-based summaries.
    public List<SceneBill> Bills { get; set; } = new();
    public List<SceneEngagement> Engagements { get; set; } = new();
    public List<SceneWorkAccount> Work { get; set; } = new();
    public List<SceneRoomStanding> Standing { get; set; } = new();
    public long CompletedPerformances { get; set; }
}
