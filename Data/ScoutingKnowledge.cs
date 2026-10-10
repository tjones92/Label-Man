using System.Collections.Generic;

/// <summary>Descriptive live result, owned by a completed appearance, never by a scout.
/// Scores are developer/save data; ordinary UI uses evidence-based descriptions.</summary>
public sealed class ScenePerformanceSnapshot {
    public string Id { get; set; }
    public int Version { get; set; } = 1;
    public float? Vocals { get; set; }
    public float Execution { get; set; }
    public float Material { get; set; }
    public float Communication { get; set; }
    public bool OriginalMaterial { get; set; }
}
public enum ScoutingEvidenceKind { Performance, BookerClaim, LegacyImpression }
public enum ScoutingPriority { Discovered, Watch, Pursue, Archived }
public sealed class ScoutingObservation {
    public string Id { get; set; }
    public string LabelId { get; set; }
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public ScoutingEvidenceKind Kind { get; set; }
    public string EventId { get; set; }
    public string BillId { get; set; }
    public string RoomId { get; set; }
    public string SourceId { get; set; }
    public string SourceName { get; set; }
    public string Channel { get; set; }
    public int? OccurredDay { get; set; }
    public int AcquiredDay { get; set; }
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
    public float Coverage { get; set; }
    public int InterpretationVersion { get; set; } = 1;
    public string Credibility { get; set; }
    public List<string> PersonIds { get; set; } = new();
    public List<string> BackingPersonIds { get; set; } = new();
    public List<SceneSetSong> Set { get; set; } = new();
    public ScenePerformanceSnapshot Interpretation { get; set; }
    public string Claim { get; set; }
    public int? BillAttendance { get; set; }
    public int BillCapacity { get; set; }
    public string Conditions { get; set; }
}
public sealed class ScoutingDossier {
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public ScoutingPriority Priority { get; set; }
    public List<ScoutingObservation> Observations { get; set; } = new();
}
public sealed class ScoutingVisit {
    public string BillId { get; set; }
    public int Day { get; set; }
    public int EndMinute { get; set; }
    public float AdmissionPaid { get; set; }
}
public sealed class FoundingActMemory {
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string LabelId { get; set; }
    public int Day { get; set; }
    public ContractTermSheet Terms { get; set; }
    public string Context { get; set; }
}
public sealed class ScoutingNegotiationState {
    public string ArtistId { get; set; }
    public ContractTermSheet Baseline { get; set; }
    public bool HasBaseline { get; set; }
    public ContractTermSheet? Draft { get; set; }
    public NegotiationPosture Posture { get; set; }
    public GameDate? CooldownUntil { get; set; }
    public bool Countered { get; set; }
    public ContractTalk Talk { get; set; }
    public List<string> Log { get; set; } = new();
}
public sealed class ScoutingKnowledgeSaveData {
    public int Version { get; set; } = 1;
    public List<ScoutingDossier> Dossiers { get; set; } = new();
    public ScoutingVisit Visit { get; set; }
    public FoundingActMemory FoundingAct { get; set; }
    public List<ScoutingNegotiationState> Negotiations { get; set; } = new();
}
