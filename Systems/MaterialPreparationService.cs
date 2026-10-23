using Godot;

/// <summary>Shared player/AI supply and rehearsal terms. These retain the existing desk assumptions.</summary>
public static class MaterialPreparationService {
    public const float CommissionFee = 150f;
    public const int CommissionDeliveryDays = 7, MinCoverLearnDays = 3, MaxCoverLearnDays = 14;
    public static int CoverLearnDays(SimulatedArtist artist) {
        if (artist == null) return MaxCoverLearnDays;
        float ability = Mathf.Clamp(artist.musicianship * .45f + artist.groupCohesion * .30f + artist.studioPerformance * .25f, 0, 1);
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(MaxCoverLearnDays, MinCoverLearnDays, ability)), MinCoverLearnDays, MaxCoverLearnDays);
    }
}
