using System;
using System.Collections.Generic;

public enum SceneRoomPayTerms { DoorSplit, HouseWage, Basket, Offering, FlatFee, None }

/// <summary>Dated money and size for the fictional supporting rooms (SimTools/LocalSceneLiveCalibrationDirective.md).
/// Observation-only: fees and receipts are performance facts in the room ledger, and nothing in the world economy
/// reads them yet. Historical anchors are cited beside each constant; every other number is a design input.
/// <c>--disable-scene-live-calibration</c> restores the flat 1960 catalog values for controls.</summary>
public static class SceneLiveEconomics {
    public const int Version = 1;

    /// <summary>BLS CPI-U annual averages, 1982-84 = 100, for 1960-1969. Prices and wages move with it, smoothly,
    /// instead of stepping on authoring-window boundaries.</summary>
    private static readonly float[] Cpi = { 29.6f, 29.9f, 30.2f, 30.6f, 31.0f, 31.5f, 32.4f, 33.4f, 34.8f, 36.7f };
    public static float PriceLevel(int year) => Cpi[Math.Clamp(year - 1960, 0, Cpi.Length - 1)] / Cpi[0];

    /// <summary>1960 city-proper population: Census working paper POP-twps0027, Table 19. City proper understates
    /// the catchment where the metro dwarfs the city (Miami, pre-consolidation Nashville); the quarter-power scale
    /// keeps that error small. Billings is below the table's cutoff: its figure is provisional.</summary>
    private static readonly Dictionary<string, int> Population1960 = new(StringComparer.Ordinal) {
        ["new_york"] = 7_781_984, ["chicago"] = 3_550_404, ["los_angeles"] = 2_479_015, ["philadelphia"] = 2_002_512,
        ["detroit"] = 1_670_144, ["baltimore"] = 939_024, ["houston"] = 938_219, ["cleveland"] = 876_050,
        ["washington"] = 763_956, ["st_louis"] = 750_026, ["san_francisco"] = 740_316, ["boston"] = 697_197,
        ["dallas"] = 679_684, ["new_orleans"] = 627_525, ["pittsburgh"] = 604_332, ["san_antonio"] = 587_718,
        ["seattle"] = 557_087, ["cincinnati"] = 502_550, ["memphis"] = 497_524, ["denver"] = 493_887,
        ["atlanta"] = 487_455, ["minneapolis"] = 482_872, ["kansas_city"] = 475_539, ["phoenix"] = 439_170,
        ["portland"] = 372_676, ["omaha"] = 301_598, ["miami"] = 291_688, ["albuquerque"] = 201_189,
        ["salt_lake_city"] = 189_454, ["nashville"] = 170_874, ["billings"] = 53_000,
    };
    /// <summary>The catalog capacities describe a room in a city of this size.</summary>
    public const float ReferencePopulation = 500_000f;
    public const float CapacityExponent = 0.25f, MinCapacityScale = 0.6f, MaxCapacityScale = 1.6f;

    /// <summary>Union scale for a booked club week: $90 for six nights (Tom Paxton's recollection of Gerde's Folk
    /// City, New York, 1961). Other towns pay in proportion to their authored going rate, New York being the anchor.</summary>
    public const float NewYorkScalePerNight = 15f;
    /// <summary>A local player's flat night at a roadhouse. Anchor: a Florida circuit sideman's $2 night "usually
    /// better than that, but not by much" (Florida Humanities, Traveling Down the Chitlin' Circuit). Provisional.</summary>
    public const float FlatFeePerNight = 3f;
    /// <summary>Basket houses paid only what the hat collected (Gaslight, Cafe Wha?). Per listener, provisional.</summary>
    public const float BasketPerHead = 0.15f;
    /// <summary>A free-will offering at a community or church program. Per listener, provisional.</summary>
    public const float OfferingPerHead = 0.05f;
    /// <summary>The acts' half of a club door (the catalog's existing split).</summary>
    public const float DoorShare = 0.5f;

    public static bool Calibrated => LocalScenes.LiveCalibration;

    public static SceneRoomPayTerms Terms(SceneRoomKind kind) => kind switch {
        SceneRoomKind.Club => SceneRoomPayTerms.DoorSplit,
        SceneRoomKind.ListeningRoom => SceneRoomPayTerms.HouseWage,
        SceneRoomKind.Coffeehouse => SceneRoomPayTerms.Basket,
        SceneRoomKind.CommunityHall => SceneRoomPayTerms.Offering,
        SceneRoomKind.Roadhouse => SceneRoomPayTerms.FlatFee,
        _ => SceneRoomPayTerms.None
    };

    public static float CapacityScale(string placeId) {
        if (placeId == null || !Population1960.TryGetValue(placeId, out int people)) return 1f;
        return Math.Clamp(MathF.Pow(people / ReferencePopulation, CapacityExponent), MinCapacityScale, MaxCapacityScale);
    }
    public static int Capacity(SceneRoomProfile room) => room == null ? 0 : !Calibrated ? room.Capacity
        : Math.Max(1, (int)MathF.Round(room.Capacity * CapacityScale(room.PlaceId)));

    /// <summary>Cover charge or minimum at the door. A listening room asks more than a club; basket houses,
    /// community programs and roadhouses take nothing at the door.</summary>
    public static float Admission(SceneRoomProfile room, int year) {
        if (room == null) return 0f;
        if (!Calibrated) return room.Admission;
        float base1960 = room.Kind switch { SceneRoomKind.Club => 1.00f, SceneRoomKind.ListeningRoom => 1.50f, _ => 0f };
        return MathF.Round(base1960 * PriceLevel(year) * 20f) / 20f;
    }

    /// <summary>The town's going rate relative to New York (the authored unsigned-act ask in CityProfiles).</summary>
    public static float GoingRate(string placeId) => CityProfiles.Get(placeId).AskScale / CityProfiles.Get("new_york").AskScale;

    /// <summary>One act's pay for one set, given the finished bill. Door, basket and offering money is shared across
    /// the acts that played; wages and flat fees are per player.</summary>
    public static float SlotFee(SceneRoomProfile room, SceneBill bill, SceneAppearance slot, int performedActs) {
        if (room == null || !room.IsPerformance || performedActs <= 0) return 0f;
        if (!Calibrated) return bill.GrossReceipts * DoorShare / performedActs;
        float level = PriceLevel(bill.Year), players = slot.PersonIds.Count;
        return Terms(room.Kind) switch {
            SceneRoomPayTerms.DoorSplit => bill.GrossReceipts * DoorShare / performedActs,
            SceneRoomPayTerms.HouseWage => players * NewYorkScalePerNight * GoingRate(room.PlaceId) * level,
            SceneRoomPayTerms.FlatFee => players * FlatFeePerNight * GoingRate(room.PlaceId) * level,
            SceneRoomPayTerms.Basket => bill.Attendance * BasketPerHead * level / performedActs,
            SceneRoomPayTerms.Offering => bill.Attendance * OfferingPerHead * level / performedActs,
            _ => 0f
        };
    }
}
