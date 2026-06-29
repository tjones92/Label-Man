// Scripts/Data/ScheduledEvent.cs

using System;

[System.Serializable]
public class ScheduledEvent {
    public string eventId;
    public string title;
    public string description;
    public GameDate date;
    public EventType eventType;
    public EventPriority priority;
    public bool interruptsSkip;
    public bool requiresPlayerAction;
    
    public string relatedContactId;
    public string relatedArtistId;
    public string relatedRecordId;
    
    public ScheduledEvent(string title, GameDate date, EventType type) {
        this.eventId = Guid.NewGuid().ToString();
        this.title = title;
        this.date = date;
        this.eventType = type;
        this.priority = EventPriority.Normal;
        this.interruptsSkip = type != EventType.Reminder;
    }
}

public enum EventType {
    ChartDay,
    RecordRelease,
    TourStart,
    TourEnd,
    GrammyAwards, 
    StudioSessionStart,
    StudioSessionEnd,
    ContractDeadline,
    PressingPlantDelivery,
    MeetingScheduled,
    PaymentDue,
    ArtistCrisis,
    FBIVisit,
    IncomingCall,
    NewsBreak,
    CompetitorMove,
    Reminder,
    Anniversary,
    Birthday
}

public enum EventPriority {
    Low,
    Normal,
    High,
    Critical
}