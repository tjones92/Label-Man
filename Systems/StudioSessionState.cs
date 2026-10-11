using System;
using System.Collections.Generic;

/// <summary>v15 working-date state; absent on already-paid legacy batch dates.</summary>
public sealed class StudioSessionState {
    public int Version = 1;
    public StudioRoomSnapshot Room;
    public StudioBrief Brief;
    public int StartMinute, ElapsedMinutes, ActionNumber;
    public float WorkloadMinutes, SessionLift, Tension, ProducerCraft, StudioPerformance, LivePerformance;
    public List<string> ParticipantIds = new(), ActionLog = new();
    public static StudioSessionState From(PlayerDesk.PendingSession s) => s.FlowVersion == 0 ? null : new() {
        Room = s.Room, Brief = s.Brief, StartMinute = s.StartMinute, ElapsedMinutes = s.ElapsedMinutes, ActionNumber = s.ActionNumber,
        WorkloadMinutes = s.WorkloadMinutes, SessionLift = s.SessionLift, Tension = s.Tension, ProducerCraft = s.ProducerCraft,
        StudioPerformance = s.StudioPerformance, LivePerformance = s.LivePerformance,
        ParticipantIds = new(s.ParticipantIds), ActionLog = new(s.ActionLog)
    };
    public void Apply(PlayerDesk.PendingSession s) {
        if (Version != 1 || Room == null || string.IsNullOrEmpty(Room.Id) || ElapsedMinutes < 0 || ElapsedMinutes > s.Hours * 60 || ActionNumber < 0 ||
            !float.IsFinite(WorkloadMinutes) || WorkloadMinutes < 0 || !Enum.IsDefined(Brief)) throw new InvalidOperationException("Invalid saved studio date.");
        s.FlowVersion = Version; s.Room = Room; s.Brief = Brief; s.StartMinute = StartMinute; s.ElapsedMinutes = ElapsedMinutes; s.ActionNumber = ActionNumber;
        s.WorkloadMinutes = WorkloadMinutes; s.SessionLift = SessionLift; s.Tension = Tension; s.ProducerCraft = ProducerCraft;
        s.StudioPerformance = StudioPerformance; s.LivePerformance = LivePerformance;
        s.ParticipantIds = new(ParticipantIds ?? new()); s.ActionLog = new(ActionLog ?? new());
    }
}
