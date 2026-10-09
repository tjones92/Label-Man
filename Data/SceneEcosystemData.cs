using System.Collections.Generic;

public sealed class SceneProgram {
    public string Id { get; set; }
    public string RoomId { get; set; }
    public string SponsorId { get; set; }
    public int StartDay { get; set; }
    public int ThroughDay { get; set; }
    public float Cost { get; set; }
    public string Provenance { get; set; }
}
public enum SceneMoveStatus { Planned, Arrived, Cancelled }
public sealed class SceneFundedMove {
    public string Id { get; set; }
    public string ArtistId { get; set; }
    public string SponsorId { get; set; }
    public string ProgramId { get; set; }
    public string FromPlaceId { get; set; }
    public string ToPlaceId { get; set; }
    public int DepartureDay { get; set; }
    public int ArrivalDay { get; set; }
    public List<string> PersonIds { get; set; } = new();
    public float Cost { get; set; }
    public SceneMoveStatus Status { get; set; }
    public string Reason { get; set; }
}
public sealed class SceneEcosystemSaveData {
    public int SchemaVersion { get; set; } = 1;
    public int LastProcessedDay { get; set; } = -1;
    public List<SceneProgram> Programs { get; set; } = new();
    public List<SceneFundedMove> Moves { get; set; } = new();
}
