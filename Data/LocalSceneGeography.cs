using System;
using System.Collections.Generic;

/// <summary>Country-aware place identity, separate from a sales region or playable catchment.</summary>
public sealed class ScenePlace {
    public string Id { get; init; }
    public string Name { get; init; }
    public string CountryCode { get; init; }
    public string MarketRegionId { get; init; }
    public string PlayableCityId { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string CoordinateSource { get; init; }
}

public enum PlaceEvidence { Unknown, Inferred, Simulated, Explicit }
public enum PlaceDatePrecision { Year, Month, Day }

/// <summary>One dated working-base change. No gameplay reader consumes moves in Phase 1.</summary>
[Serializable]
public sealed class PlaceMove {
    public string fromPlaceId;
    public string toPlaceId;
    public int year;
    public int month;
    public int day;
    public PlaceDatePrecision precision;
    public string reason;
    public PlaceEvidence evidence;
}

/// <summary>
/// An act's origin is its formation place; a person's origin is personal birthplace when known.
/// Unknown legacy origins stay null. Base assignment never rewrites commercial homeRegion.
/// </summary>
[Serializable]
public sealed class GeographicIdentity {
    public string originPlaceId;
    public PlaceEvidence originEvidence;
    public string basePlaceId;
    public PlaceEvidence baseEvidence;
    public string assignmentSource;
    public int assignmentVersion;
    public List<PlaceMove> moves;
}

// Admit institutions before programming/bills arrive. These types imply no invented opening dates.
public enum SceneInstitutionKind {
    BarClub, RoadhouseHonkyTonk, TheatreSupperClubHotel, DanceHallBallroomArmory,
    Coffeehouse, CommunitySpace, Campus, ReligiousSpace, RehearsalSpace,
    Studio, Publisher, Radio, RecordShop, Distributor, BookingOffice
}

public sealed class SceneIdentitySaveData {
    public int ContentVersion { get; set; }
    public int AssignmentVersion { get; set; }
    public ulong WorldSeed { get; set; }
}
