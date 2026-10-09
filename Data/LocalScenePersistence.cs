using System;
using System.Collections.Generic;

public enum SceneRelationship { Resident, RegularVisitor, Alumnus, TouringGuest, SessionWorker }
// Independent of ProspectMarketStatus. These values confer no skill or contract eligibility.
public enum ScenePerformanceLevel { Latent, Occasional, Working, Circuit, Headliner }

[Serializable]
public sealed class SceneParticipation {
    public string sceneId;
    public string placeId;
    public SceneRelationship relationship;
    public ScenePerformanceLevel performanceLevel;
    public int startYear;
    public int startWeek;
    public int? endWeek;
    public string provenance;
}

public sealed class SceneEncounterWindow {
    public string SceneId { get; set; }
    public int Venue { get; set; }
    public int Week { get; set; }
    public List<string> ArtistIds { get; set; } = new();
}

public sealed class ScenePersistenceSaveData {
    public int SchemaVersion { get; set; } = 1;
    public int LastProcessedWeek { get; set; } = -1;
    // A bounded current-week cast, not a booking calendar or a second artist population.
    public List<SceneEncounterWindow> Windows { get; set; } = new();
}

public sealed class SceneDiscovery {
    public string ArtistId { get; set; }
    public string FirstPlaceId { get; set; }
    public int FirstWeek { get; set; }
    public string LastPlaceId { get; set; }
    public int LastWeek { get; set; }
    public int Venue { get; set; }
}
