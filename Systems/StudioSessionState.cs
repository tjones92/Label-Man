using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>v15 working-date state; absent on already-paid legacy batch dates.</summary>
public sealed class StudioSessionState {
    public int Version = 1;
    public StudioRoomSnapshot Room;
    public StudioBrief Brief;
    public int StartMinute, ElapsedMinutes, ActionNumber;
    public float WorkloadMinutes, SessionLift, Tension, ProducerCraft, StudioPerformance, LivePerformance;
    public List<string> ParticipantIds = new(), ActionLog = new();
    // v19 (absent on older dates: no hires, no breakdowns).
    public List<StudioSessionHire> Hires;
    public List<string> DeclinedCallIns;
    public int BreakdownVersion;
    public static StudioSessionState From(PlayerDesk.PendingSession s) => s.FlowVersion == 0 ? null : new() {
        Room = s.Room?.Copy(), Brief = s.Brief, StartMinute = s.StartMinute, ElapsedMinutes = s.ElapsedMinutes, ActionNumber = s.ActionNumber,
        WorkloadMinutes = s.WorkloadMinutes, SessionLift = s.SessionLift, Tension = s.Tension, ProducerCraft = s.ProducerCraft,
        StudioPerformance = s.StudioPerformance, LivePerformance = s.LivePerformance,
        ParticipantIds = new(s.ParticipantIds), ActionLog = new(s.ActionLog),
        Hires = s.Hires.Count == 0 ? null : s.Hires.Select(h => h.Copy()).ToList(),
        DeclinedCallIns = s.DeclinedCallIns.Count == 0 ? null : new(s.DeclinedCallIns), BreakdownVersion = s.BreakdownVersion
    };
    public void Apply(PlayerDesk.PendingSession s) {
        if (Version != 1 || Room == null || string.IsNullOrEmpty(Room.Id) || ElapsedMinutes < 0 || ElapsedMinutes > s.Hours * 60 || ActionNumber < 0 ||
            !float.IsFinite(WorkloadMinutes) || WorkloadMinutes < 0 || !Enum.IsDefined(Brief)) throw new InvalidOperationException("Invalid saved studio date.");
        if (Room.CapabilityVersion >= 3 && (string.IsNullOrEmpty(Room.EngineerId) || string.IsNullOrEmpty(Room.EngineerName) || !Enum.IsDefined(Room.EngineerHabit) ||
            Room.SetupMinutes is < 1 or > 60 || Room.FamiliarDates < 0 ||
            new[] { Room.EngineerEar, Room.EngineerHands, Room.Intimacy, Room.Liveliness, Room.Control }.Any(v => !float.IsFinite(v) || v < 0 || v > 1)))
            throw new InvalidOperationException("Invalid saved studio room.");
        if (Room.CapabilityVersion >= 4 && (!float.IsFinite(Room.EngineerPremium) || Room.EngineerPremium < 0 || Room.Specialist && string.IsNullOrEmpty(Room.Specialty)) ||
            BreakdownVersion is < 0 or > 1 || (Hires ?? new()).Any(h => h == null || !h.Valid) || (Hires ?? new()).GroupBy(h => h.PersonId).Any(g => g.Count() > 1) ||
            (DeclinedCallIns ?? new()).Any(string.IsNullOrEmpty))
            throw new InvalidOperationException("Invalid saved studio personnel.");
        s.FlowVersion = Version; s.Room = Room.Copy(); s.Brief = Brief; s.StartMinute = StartMinute; s.ElapsedMinutes = ElapsedMinutes; s.ActionNumber = ActionNumber;
        s.WorkloadMinutes = WorkloadMinutes; s.SessionLift = SessionLift; s.Tension = Tension; s.ProducerCraft = ProducerCraft;
        s.StudioPerformance = StudioPerformance; s.LivePerformance = LivePerformance;
        s.ParticipantIds = new(ParticipantIds ?? new()); s.ActionLog = new(ActionLog ?? new());
        s.Hires = (Hires ?? new()).Select(h => h.Copy()).ToList(); s.DeclinedCallIns = new(DeclinedCallIns ?? new()); s.BreakdownVersion = BreakdownVersion;
    }
}
