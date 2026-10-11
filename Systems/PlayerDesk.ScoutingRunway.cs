using System;

public partial class PlayerDesk {
    /// <summary>For the opening-runway audit (SimTools/ScoutingRunwayChecks.cs): whether an act on a posted bill would
    /// discuss a record deal today, and the ask the pad would show after hearing them in that room. Reads the same
    /// owners as the pad and the signing gate; changes nothing. Not a player surface: the player learns this only
    /// by hearing the act and opening the conversation.</summary>
    internal (bool Available, float Ask) SceneDealQuote(string artistId, string roomId) {
        var artist = ArtistManager.Instance?.GetArtist(artistId);
        var room = SceneRoomCatalog.Get(roomId);
        if (artist == null || room == null) return (false, 0);
        var prospect = new Prospect { Artist = artist, Venue = room.Category, CityId = room.PlaceId };
        bool available = string.IsNullOrEmpty(artist.labelId) && artist.prospectMarketStatus != ProspectMarketStatus.Latent &&
            SceneDealAvailable(prospect);
        return (available, VenueAdvanceAsk(artist, room.Category, AskScaleFor(room.PlaceId)));
    }
}
