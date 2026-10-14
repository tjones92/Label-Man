using System.Collections.Generic;

/// <summary>Source context only: no foreign sales region, simulated foreign label or consumer economy.</summary>
public sealed record SceneSourceProfile(string PlaceId, string EarlyPractice, string LatePractice,
    string BusinessPath, string Source, bool Provisional);

/// <summary>A label's dated, simulated introduction. This is A&R access, never ownership of a master.</summary>
public sealed class SceneBusinessConnection {
    public string Id { get; set; }
    public string SourcePlaceId { get; set; }
    public int FromWeek { get; set; }
    public int ThroughWeek { get; set; }
    public string Provenance { get; set; }
}

public sealed record SceneSpecialistInstitution(string Id, string PlaceId, SceneInstitutionKind Kind,
    string Practice, IReadOnlyList<GenreFamily> Families, string Source) {
    public bool Fictional => true;
}
