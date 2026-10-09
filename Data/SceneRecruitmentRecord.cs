/// <summary>Contract-time evidence. No implied move or manufactured legacy history.</summary>
public sealed class SceneRecruitmentRecord {
    public string LabelId;
    public string ArtistId;
    // Captured at the event; legacy records remain unknown rather than reading today's genre.
    public Genre? SigningGenre;
    public int Week;
    public int Year;
    public string Phase;
    public string Route;
    public string HqPlaceId;
    public string BasePlaceId;
    public string OriginPlaceId;
    public float? RoadMiles;
    public string Explanation;
    public string AccessEvidence;
}
