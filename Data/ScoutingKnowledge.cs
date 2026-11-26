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
    /// <summary>Who was in the room for this set and who came for it (Phase 5). Null on snapshots resolved before it existed.</summary>
    public SetAudience Audience { get; set; }
}
/// <summary>The audience of one set, resolved once by the room owner whether or not anybody from a label is there. The
/// room's own crowd is never credited to an act: only the people who came in for this set are.</summary>
public sealed class SetAudience {
    /// <summary>The crowd in the room during the set.</summary>
    public int Present { get; set; }
    /// <summary>People who came in for this act's set -- its own draw, not the room's regulars or the other acts'.</summary>
    public int ArrivedFor { get; set; }
    /// <summary>Share of the room still there at the end of the set (0-1).</summary>
    public float StayedShare { get; set; }
    /// <summary>Calls for particular songs: people who know the act's material from earlier nights here.</summary>
    public int Requests { get; set; }
}
public enum ScoutingEvidenceKind { Performance, BookerClaim, LegacyImpression, PublicReport, ListenerOpinion, ActBusinessClaim, BusinessStatement, MeetingStatement, RehearsalSample, DemoSample, RivalInterest }
public enum ScoutingBusinessStatementKind { OpeningAsk, Counter, Objection, Declined, SteppedBack, Agreement }
// Only stated contractual fields, never utility, reservation, posture or patience.
public sealed class ScoutingStatedTerms {
    public float Advance { get; set; }
    public float RoyaltyRate { get; set; }
    public int TermYears { get; set; }
    public int SinglesObligation { get; set; }
    public bool LabelOwnsPublishing { get; set; }
    public bool ArtistCreativeControl { get; set; }
    public static ScoutingStatedTerms From(ContractTermSheet terms) => new() {
        Advance = terms.Advance, RoyaltyRate = terms.RoyaltyRate, TermYears = terms.TermYears,
        SinglesObligation = terms.SinglesObligation, LabelOwnsPublishing = terms.LabelOwnsPublishing,
        ArtistCreativeControl = terms.ArtistCreativeControl
    };
}
public enum ScoutingQuestion { None, Vocals, Ensemble, Material, Stage }
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
    public string RoomName { get; set; }
    public string SourceId { get; set; }
    public string SourceName { get; set; }
    public string Channel { get; set; }
    public int? OccurredDay { get; set; }
    public int? OccurredWeek { get; set; }
    public int? PublishedDay { get; set; }
    public string PlaceId { get; set; }
    public string PlaceName { get; set; }
    public int AcquiredDay { get; set; }
    // Unknown for older observations; never infer a historical time on load.
    public int? AcquiredMinute { get; set; }
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
    public string MeetingId { get; set; }
    public string BusinessDiscussionId { get; set; }
    public ScoutingBusinessStatementKind BusinessStatementKind { get; set; }
    public ScoutingStatedTerms StatedTerms { get; set; }
    public int? BillAttendance { get; set; }
    public int BillCapacity { get; set; }
    // Acquired bill context; absent in older evidence. Quote is posted admission, not paid cost.
    public int? BillListedActs { get; set; }
    public float? BillAdmissionQuote { get; set; }
    // Set-level audience seen from the room (a heard set) or the booker's record of the act's draw. Absent in older evidence.
    public int? SetPresent { get; set; }
    public int? SetArrivals { get; set; }
    public float? SetStayedShare { get; set; }
    public int? SetRequests { get; set; }
    /// <summary>The nights a booker's draw figure averages over; null for a set the player watched.</summary>
    public int? DrawNights { get; set; }
    // Optional paid consultation cost. Zero for existing and roster-listener observations.
    public float ConsultationFee { get; set; }
    public string Conditions { get; set; }
}
public sealed class ScoutingDossier {
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public ScoutingPriority Priority { get; set; }
    public ScoutingQuestion Question { get; set; }
    public int BusinessDiscussionCount { get; set; }
    public List<ScoutingObservation> Observations { get; set; } = new();
}
public sealed class ScoutingVisit {
    public string BillId { get; set; }
    public int Day { get; set; }
    public int EndMinute { get; set; }
    public float AdmissionPaid { get; set; }
    public List<string> ListenerIds { get; set; } = new();
}
public sealed class FoundingActMemory {
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string LabelId { get; set; }
    public int Day { get; set; }
    public ContractTermSheet Terms { get; set; }
    public string Context { get; set; }
    public int? Minute { get; set; }
    public string ObservationId { get; set; }
    public List<string> PersonIds { get; set; } = new();
}
public sealed class ScoutingNegotiationState {
    public string ArtistId { get; set; }
    public string BusinessDiscussionId { get; set; }
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
    // False only for a label whose history starts here. Null means older, unknown history.
    public bool? FoundingHistoryStarted { get; set; }
    public List<ScoutingNegotiationState> Negotiations { get; set; } = new();
    /// <summary>Approaches to heard acts that were not looking for a deal. Null on older saves.</summary>
    public List<ScoutingDirectApproach> Approaches { get; set; }
    /// <summary>The venue you are standing in today, if any (null: elsewhere in town). Older saves: null.</summary>
    public string AtRoomId { get; set; }
    public int? AtRoomDay { get; set; }
    /// <summary>Phone numbers you hold for acts, and who refused to give one until when. Null on older saves.</summary>
    public List<ScoutingActNumber> ActNumbers { get; set; }
    public List<ScoutingNumberRefusal> NumberRefusals { get; set; }
}
/// <summary>A number that reaches one member of an act. It stops reaching the act when that person leaves it.</summary>
public sealed class ScoutingActNumber {
    public string ArtistId { get; set; }
    public string PersonId { get; set; }
    public string PersonName { get; set; }
    /// <summary>Who gave it: the member after a set, or a booker (by name).</summary>
    public string Source { get; set; }
    public int Day { get; set; }
}
/// <summary>A source declined to give an act's number; it can be asked again from UntilDay.</summary>
public sealed class ScoutingNumberRefusal {
    public string ArtistId { get; set; }
    public string SourceId { get; set; }
    public int UntilDay { get; set; }
}
/// <summary>WantsAnotherLook and Agreed are appended (saves store the number). WantsAnotherLook: a careful act asked to be
/// heard again first. Agreed: the act said yes to you, and signs from the notebook once its steps are taken.
/// Considering and Ready belong to older saves (the 8-day answer through the weekly activation pass).</summary>
public enum ScoutingApproachStatus { Considering, Ready, Declined, Lapsed, WantsAnotherLook, Agreed }
/// <summary>What stands between an act's yes and a signature (SigningRealismBreakdown.md §6, step 3).</summary>
public enum ScoutingApproachStepKind {
    /// <summary>A member is under 21, so a parent signs for them. You go and meet the family.</summary>
    Parents,
    /// <summary>A shark, svengali or visionary manager wants a sit-down before anybody signs anything.</summary>
    Manager,
    /// <summary>One member isn't sure. At the band's next set they come over with one objection (step 9), and you answer it.</summary>
    Holdout,
    /// <summary>A comfortable act hearing you out wants one answer before it talks terms (step 9).</summary>
    Objection
}
/// <summary>One dated step before an agreed act signs. The player takes it; the clock never does.</summary>
public sealed class ScoutingApproachStep {
    public ScoutingApproachStepKind Kind { get; set; }
    /// <summary>Who you're seeing: the minors' names, the manager, or the member holding out.</summary>
    public string Who { get; set; }
    /// <summary>The first day it can happen: Sunday for a family, the next weekday for a manager, the act's next posted
    /// set for a holdout. Null for a holdout whose next date isn't posted, when the next set you hear counts.</summary>
    public int? Day { get; set; }
    /// <summary>The room of the holdout's set, when it's posted.</summary>
    public string RoomName { get; set; }
    /// <summary>Who the manager is, introduced ("Harold Caldwell, Sal's father"), as read when the yes was given
    /// (ManagerIdentity). On a Parents step, set when the family is the manager and the visit is the sit-down too.</summary>
    public string Manager { get; set; }
    /// <summary>Manager step: where you sit down with him.</summary>
    public string Place { get; set; }
    /// <summary>Manager step: the manager is a woman (a mother managing her children's act).</summary>
    public bool Female { get; set; }
    public bool Done { get; set; }
    public int DoneDay { get; set; }
    /// <summary>What was said when it was taken.</summary>
    public string Said { get; set; }
    /// <summary>Holdout: the member's personId, so the objection is read from that person (step 9). Null on older saves.</summary>
    public string PersonId { get; set; }
    /// <summary>Holdout or Objection: the objection has been put to you, and the day it was (step 9).</summary>
    public bool Raised { get; set; }
    public int RaisedDay { get; set; }
    public CourtshipObjection Objection { get; set; }
    /// <summary>What they said, quoted without the speaker.</summary>
    public string ObjectionLine { get; set; }
    /// <summary>Holdout: you told them to take their time on this day; the next set you hear after it settles it.</summary>
    public int? WaitFrom { get; set; }
    /// <summary>The objection was answered with a bluff that landed (the Fixer's check).</summary>
    public bool Bluffed { get; set; }
    /// <summary>A bluff they saw through: the day, and the state it was tried on. It can't be tried again on that state.</summary>
    public int? BluffFailedDay { get; set; }
    public string BluffFailedKey { get; set; }
}
/// <summary>A label asked an act that was off the market whether it would hear an offer. The act's answer comes from its
/// disposition (ActDisposition): a hungry act says yes (tonight, or after its steps), a careful act asks to be heard
/// again first, a comfortable act says no and why. Older saves hold season-keyed declines; those now hold only while the
/// act is still comfortable.</summary>
public sealed class ScoutingDirectApproach {
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public int Day { get; set; }
    public ScoutingApproachStatus Status { get; set; }
    /// <summary>Considering: the day the act's side said it would answer by. Otherwise the day of the answer.</summary>
    public int UntilDay { get; set; }
    /// <summary>What the act's side said, as said (null on older saves).</summary>
    public string Said { get; set; }
    /// <summary>Agreed: the steps before they sign, in order. Empty when they'll sign tonight.</summary>
    public List<ScoutingApproachStep> Steps { get; set; } = new();
    /// <summary>Lapsed: the act said yes and you never acted on it before it ran out (regard remembers). False on older saves.</summary>
    public bool LetLapse { get; set; }
    /// <summary>Agreed: a comfortable act that trusts you agreed to hear you out. It talks terms at the table rather than
    /// signing what it's handed, because its reason for staying put still stands.</summary>
    public bool Hearing { get; set; }
}
