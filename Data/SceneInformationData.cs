using System.Collections.Generic;

public sealed class SceneContactKnowledge {
    public string ContactId { get; set; }
    public List<string> HeardBillIds { get; set; } = new();
}
public sealed class SceneLead {
    public string Id { get; set; }
    public string ArtistId { get; set; }
    public string RoomId { get; set; }
    public string BillId { get; set; }
    public string ContactId { get; set; }
    public int ReceivedDay { get; set; }
    public int PerformanceDay { get; set; }
    public int ExpiresDay { get; set; }
    public string Evidence { get; set; }
}
public sealed class SceneInformationKnowledge {
    public List<SceneContactKnowledge> Contacts { get; set; } = new();
    public List<SceneLead> Leads { get; set; } = new();
    public Dictionary<string, int> ReadThroughDay { get; set; } = new();
    public Dictionary<string, int> ReadEvents { get; set; } = new();
}
public sealed record SceneNewsItem(string Id, string PlaceId, string ArtistId, int EventDay,
    int AvailableDay, string Source, string Text);
