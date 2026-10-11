using Godot;

/// <summary>Fictional local recording services, with stable identities. No historical facility claims.</summary>
public sealed class StudioRoomSnapshot {
    public string Id;
    public string CityId;
    public string Name;
    public string Character;
    public string Layout = "Whole ensemble on mono tape";
    public float HourlyRate;
    public float Craft;
    public int CapabilityVersion = 1;
}

public static class StudioRoomCatalog {
    public static PlayerDesk.StudioTier[] Available(string cityId, string regionId) {
        float quality = PlayerDesk.StudioQualityIn(cityId, regionId);
        return quality >= .78f ? new[] { PlayerDesk.StudioTier.Budget, PlayerDesk.StudioTier.Mid, PlayerDesk.StudioTier.Top }
            : quality >= .5f ? new[] { PlayerDesk.StudioTier.Budget, PlayerDesk.StudioTier.Mid } : new[] { PlayerDesk.StudioTier.Budget };
    }
    public static StudioRoomSnapshot Local(string cityId, string regionId, PlayerDesk.StudioTier tier) {
        string city = DistanceModel.GetCityById(cityId)?.name ?? "Local";
        return new() {
            Id = $"studio:{cityId}:{(int)tier}", CityId = cityId,
            Name = city + (tier == PlayerDesk.StudioTier.Budget ? " Recording Service" : tier == PlayerDesk.StudioTier.Mid ? " Sound Workshop" : " Studio Theatre"),
            Character = tier == PlayerDesk.StudioTier.Budget ? "A straightforward room: close together, with a little room spill."
                : tier == PlayerDesk.StudioTier.Mid ? "A controlled room with a dependable house balance." : "A spacious room with a lively ensemble sound.",
            HourlyRate = PlayerDesk.StudioHourlyRateIn(cityId, regionId, tier),
            Craft = PlayerDesk.StudioQualityIn(cityId, regionId)
        };
    }
}
