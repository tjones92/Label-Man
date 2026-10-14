using System;

/// <summary>
/// Dated recording equipment per room (directive slice 4). Track count describes the recording layout a room can
/// offer, never a quality ceiling: a mono room still cuts whole performances, and a cheap close room can still make the
/// better record.
/// <para>
/// <b>Historical anchors, not a census.</b> Ampex's Sel-Sync made overdubbing on multitrack tape practical from the
/// mid-1950s; three-track machines were the working pop format from the late 1950s to the mid-1960s (backing on two
/// tracks, the lead vocal on the third); Atlantic installed an eight-track in 1958, an exception; four-track machines
/// were what a dealer could sell by 1963 (Abbey Road adopted the four-track J37 in 1964); Motown went to eight-track
/// around 1964-65. Sources: History of multitrack recording (Wikipedia), Abbey Road's J37 history, a former Motown
/// draftsman's recollection (soul-source.co.uk). Each room's adoption year below is an authored range by room role and
/// town, keyed per room so rooms in the same town differ. Directive slice 5 (bouncing, larger setups) builds on this.
/// </para>
/// </summary>
public static class StudioEquipment {
    /// <summary>First year a room of this role usually had 2, 3, 4 and 8 tracks. Provisional authored years.</summary>
    private static int[] Years(PlayerDesk.StudioTier tier, bool specialist) => specialist || tier == PlayerDesk.StudioTier.Top
        ? new[] { 1957, 1959, 1963, 1966 }
        : tier == PlayerDesk.StudioTier.Mid ? new[] { 1960, 1963, 1965, 1969 } : new[] { 1963, 1966, 1968, 1972 };
    private static readonly int[] TrackSteps = { 2, 3, 4, 8 };

    /// <summary>The tracks this room's machine has in this year. Strong recording towns adopt a year earlier, weak ones a
    /// year later, and each room's own purchase falls within a two-year window. Pure and keyed.</summary>
    public static int TracksFor(StudioRoomSnapshot room, PlayerDesk.StudioTier tier, int year) {
        if (room == null) return 1;
        int shift = room.Craft >= .85f ? -1 : room.Craft < .6f ? 1 : 0;
        var years = Years(tier, room.Specialist);
        int tracks = 1;
        for (int i = 0; i < years.Length; i++) {
            int jitter = (int)(StudioTakeEvidence.Unit($"{room.Id}|tracks|{i}") * 3) % 3;
            if (year >= years[i] + shift + jitter) tracks = TrackSteps[i];
        }
        return tracks;
    }

    public static string LayoutText(int tracks) => tracks switch {
        <= 1 => "Mono machine: the whole performance goes down together",
        2 => "Two-track machine: the band on one track, the vocal on the other",
        3 => "Three-track machine: band, vocal, and a spare track for an added part",
        _ => $"{tracks}-track machine: band, vocal and room for added parts"
    };
}

public partial class PlayerDesk {
    /// <summary>Whether a capture layout is possible on a machine with this many tracks.</summary>
    public static bool StudioLayoutAllowed(StudioLayout layout, int tracks) => layout switch {
        StudioLayout.VocalApart => tracks >= 2,
        StudioLayout.ThreeTrack => tracks >= 3,
        _ => true
    };

    public static string StudioLayoutName(StudioLayout layout) => layout switch {
        StudioLayout.VocalApart => "Vocal on its own track",
        StudioLayout.ThreeTrack => "Band, vocal and a spare track",
        _ => "Everyone together"
    };
}

/// <summary>What the player asks for when booking a date beyond the room, songs and hours.</summary>
public sealed class StudioBookingOptions {
    public StudioLayout Layout = StudioLayout.Together;
    public int Days = 1;
    public bool Evening;
    /// <summary>An evening date that runs past midnight (needs Evening).</summary>
    public bool LateNight;
    public string ProducerId;
}

public partial class PlayerDesk {
    /// <summary>A lockout keeps the room set up for up to three days at a block rate. Provisional authored figures.</summary>
    public const int StudioMaxLockoutDays = 3;
    public static float StudioLockoutFactor(int days) => days <= 1 ? 1 : days * .9f;
    /// <summary>Evening work: the room after its day clients, cheaper, with a band that has already done a day. Norman Petty
    /// recorded the Crickets late at night in Clovis; here an evening date runs from 6 p.m. to the hard stop, or, booked
    /// to run late, on into the small hours.</summary>
    public const int StudioEveningStartHour = 18;
    public const float StudioEveningRateFactor = .75f, StudioEveningWorkload = 25f;
    /// <summary>The latest a late-night date runs: 2 a.m., as minutes from the booking day's midnight. Past midnight
    /// every minute of work tires the band a quarter again. Provisional authored figures.</summary>
    public const int StudioLateStopMinute = 26 * 60;
    public const float StudioSmallHoursWorkload = .25f;
}
