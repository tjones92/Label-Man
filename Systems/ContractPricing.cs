using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ScoutingVenue = PlayerDesk.ScoutingVenue;

/// <summary>
/// What a record deal paid up front, by who the act was and who was buying.
///
/// The legacy AI price started a major at $5,000 and multiplied by 1.5 for "competition", so a major offered an
/// unknown bar band about $10,000 -- three hundred times what the same act asked the player's label for. That was
/// never the period's shape. An act with no record behind it got little or nothing up front from anybody: the Beatles'
/// 1962 EMI paper carried a penny a single and no advance worth the name, 1950s royalties ran 3-5% on 90% of sales,
/// and the label recouped its recording costs from the act's royalties. What a big label offered an unknown was its
/// distribution, its promotion men, its studio and its name -- which the act weighs in the contract outlook
/// (<see cref="PlayerDesk.CapabilitiesOf"/>) -- not a bigger cheque. Real money followed a track record: RCA bought
/// Elvis's contract for $35,000 only after Sun had made him a regional star.
///
/// So the advance is the act's own going rate -- the same room-banded figure it asks the player for -- with a modest
/// premium for a bigger buyer, rising toward the legacy figure only as the act's record proves out. Royalty, term
/// and sides are untouched. <c>--disable-historical-advances</c> restores the legacy prices exactly.
/// </summary>
public static class ContractPricing {
    public static bool HistoricalAdvances { get; private set; } = true;

    public static void Configure(IEnumerable<string> arguments) {
        string[] args = (arguments ?? Array.Empty<string>()).ToArray();
        bool enable = args.Contains("--enable-historical-advances", StringComparer.Ordinal);
        bool disable = args.Contains("--disable-historical-advances", StringComparer.Ordinal);
        if (enable && disable) throw new ArgumentException("--enable-historical-advances and --disable-historical-advances cannot be used together.");
        HistoricalAdvances = !disable;
    }

    internal static void ConfigureForCheck(bool historical) => HistoricalAdvances = historical;

    /// <summary>The genres that are, in period terms, the trade's manufactured product: youth pop assembled and worked by
    /// professionals. They arrive represented and with a number in mind.</summary>
    public static readonly HashSet<Genre> ManufacturedGenres = new() {
        Genre.TeenPop, Genre.Bubblegum, Genre.GirlGroup, Genre.DooWop, Genre.SunshinePop, Genre.RockAndRoll
    };

    /// <summary>What an unknown act asks to sign, by the room it works. The room IS the price band in 1960: a roadhouse
    /// act takes dinner money and a promise, a professional act working a supper club has a going rate, and the trade's
    /// manufactured product arrives with people behind it. The player's asks use these same bands.</summary>
    public static float VenueBase(ScoutingVenue venue) => venue switch {
        ScoutingVenue.HonkyTonks             => 20f,    // a fifth and a steak dinner
        ScoutingVenue.ClubsAndRoadhouses     => 25f,    // "you're buying, right?"
        ScoutingVenue.TheatresAndSupperClubs => 260f,   // working pros with a going rate
        ScoutingVenue.IndustryMeets          => 600f,   // polished product, professionally represented
        _                                    => 25f
    };

    /// <summary>The room an act works when nobody has watched it in one: representation and the trade's product move an act
    /// up the bands. Pure and cheap -- the AI's affordability gate asks it per candidate per day.</summary>
    public static ScoutingVenue WorkingRoom(SimulatedArtist artist) {
        bool represented = artist.manager != ManagerArchetype.None;
        bool manufactured = ManufacturedGenres.Contains(artist.primaryGenre);
        return represented ? (manufactured ? ScoutingVenue.IndustryMeets : ScoutingVenue.TheatresAndSupperClubs) : ScoutingVenue.ClubsAndRoadhouses;
    }

    /// <summary>The act's talent and standing scale inside its room's band (0.5x-2x talent, standing on top). Same shape as
    /// the legacy offer curve, so the two read consistently.</summary>
    public static float ActScale(SimulatedArtist artist) =>
        (0.5f + artist.CalculateBaseQuality() * 1.5f) * (1f + artist.reputation * 2f + artist.momentum * 1.5f);

    /// <summary>The act's own going rate before any manager, label-independent: what it would ask anybody.</summary>
    public static float GoingRate(SimulatedArtist artist) =>
        VenueBase(WorkingRoom(artist)) * ActScale(artist) * ScenePriceFeedback.Multiplier(artist);

    /// <summary>What a bigger buyer adds for an unproven act: a little more, never a different order of magnitude.</summary>
    public static float TierPremium(LabelTier tier) => tier switch {
        LabelTier.Major => 1.6f, LabelTier.MidTier => 1.35f, LabelTier.Boutique => 1.1f,
        LabelTier.Independent => 1.0f, LabelTier.Small => 0.8f, _ => 1.0f
    };

    /// <summary>How far the act's record entitles it to real money (0 unproven, 1 proven). The best career it has had
    /// counts, so a star between labels still prices as one; a Top 40 hit at least half-proves an act.</summary>
    public static float ProvenShare(SimulatedArtist artist) {
        static float Of(CareerState s) => s switch {
            CareerState.Superstar or CareerState.Star or CareerState.Established => 1f,
            CareerState.Rising => 0.5f, _ => 0f
        };
        float share = Mathf.Max(Of(artist.careerState), Of(artist.careerStateBeforeDrop));
        if (artist.top40Hits > 0) share = Mathf.Max(share, 0.5f);
        return share;
    }

    /// <summary>The label's advance: the act's going rate with the tier premium while unproven, moving geometrically to the
    /// legacy figure as the record proves out. Never above the legacy figure.</summary>
    public static float Advance(LabelTier tier, SimulatedArtist artist, float legacy) {
        if (!HistoricalAdvances) return legacy;
        float unproven = Mathf.Min(legacy, GoingRate(artist) * TierPremium(tier));
        float proven = ProvenShare(artist);
        if (proven <= 0f || unproven <= 0f) return unproven;
        if (proven >= 1f) return legacy;
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(unproven), Mathf.Log(legacy), proven));
    }

    /// <summary>The scouting gate's estimate of a first contract, before it knows who: a working professional's going rate at
    /// this tier's premium. Legacy keeps the flat tier prices.</summary>
    public static float EstimatedFirstAdvance(LabelTier tier, float legacy) =>
        HistoricalAdvances ? VenueBase(ScoutingVenue.TheatresAndSupperClubs) * 1.25f * TierPremium(tier) : legacy;

    /// <summary>The scale an act reads an offer's money against when choosing between labels: the best that act could be
    /// offered by anybody (a major's price for it). Legacy keeps the flat $12,000.</summary>
    public static float ChoiceMoneyScale(SimulatedArtist artist, Func<float> majorLegacy) =>
        HistoricalAdvances ? Mathf.Max(1f, Advance(LabelTier.Major, artist, majorLegacy())) : 12000f;
}
