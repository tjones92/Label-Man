using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A working week. Two limits on how often an act plays, neither a cap on a good act:
/// <para><b>Day jobs.</b> Most local acts kept their jobs and played weekends: Friday and Saturday nights and Sunday
/// programs. A coffeehouse open stage is the weeknight exception (the Gaslight and Gerde's ran their hootenannies on
/// Monday and Tuesday nights, for whoever turned up with a guitar). An act lives off music, and so takes weeknight work,
/// when its records sell, it holds a standing engagement in a room, or a member who can afford not to work.</para>
/// <para><b>Two rooms in a day.</b> A full-time act can work a second room in its town the same day, if there is time
/// to pack up and cross town between sets (the Beatles often played a Cavern lunchtime and an evening dance elsewhere,
/// and sometimes a third room). At most two rooms a day; day-job acts never double; house-band backers stay in one
/// room. The catalog's room hours (afternoon halls and roadhouses, evening clubs and coffeehouses) and families
/// decide which acts can double at all.</para>
/// Both follow room rotation: <c>--disable-scene-room-rotation</c> turns them off with it. <c>--disable-scene-day-jobs</c>
/// and <c>--disable-scene-double-rooms</c> are the separate controls.</summary>
public static partial class LocalSceneRoomService {
    /// <summary>Hours between the end of one set and the start of a set in another room: pack up, cross town, set up.</summary>
    public const int TravelHours = 1;
    public const int MaxRoomsPerDay = 2;
    /// <summary>The scheduler's place marker for a player working two rooms today: matches no room, so no house band picks them.</summary>
    private const string SecondRoom = "(second room)";

    /// <summary>Weekend nights: a day-job act plays these. Sunday holds the community and church programs.</summary>
    public static bool Weekend(DayOfWeek day) => day is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>An act that lives off music. Records that sell (a chart entry, or a career past its first signing); a
    /// standing engagement in a room; enough room work to live on (<see cref="ProShows"/>); a young, hungry line-up
    /// that has gone pro before the work arrives (<see cref="GonePro"/>: the Beatles quit their jobs unsigned, for
    /// Hamburg and the Cavern); or a member who can afford not to work (the band-life StudioOnly threshold). A contract
    /// alone is not enough: most acts on small local labels kept their jobs. Everyone else keeps a day job.</summary>
    public static bool FullTime(SimulatedArtist act, int day) => FullTime(act, State.Engagements
        .Where(e => e.StartDay <= day && e.ThroughDay >= day).Select(e => e.ArtistId).ToHashSet(StringComparer.Ordinal));
    /// <param name="engaged">Acts holding a standing engagement that day (the scheduler keeps this set per day).</param>
    private static bool FullTime(SimulatedArtist act, HashSet<string> engaged) => act != null && (Traction(act) ||
        engaged.Contains(act.artistId) || ActShows(act.artistId) >= ProShows || GonePro(act) ||
        act.members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active && MemberWealthService.Norm(m) >= MemberWealthService.StudioOnlyAffordNorm));

    /// <summary>Live room shows at which an act has steady enough work to quit its jobs:
    /// half a year of weekly gigs. A weekend act reaches it in about three months of full weekends. Design input.</summary>
    public const int ProShows = 26;
    /// <summary>Gone pro: every active member at least this ambitious and no older than <see cref="ProMaxAge"/> (no
    /// family to keep). Design inputs.</summary>
    public const float ProAmbition = 0.6f;
    public const int ProMaxAge = 25;
    public static bool GonePro(SimulatedArtist act) {
        int year = Date(Math.Max(0, State.LastResolvedDay)).year;
        var people = act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToArray();
        return people.Length > 0 && people.All(m => m.ambition >= ProAmbition && m.birthYear > 0 && year - m.birthYear <= ProMaxAge);
    }
    /// <summary>Live room shows an act has played, every room (a derived index over standing; never saved). Pre-rolled
    /// history is left out, as in the booker's own count, so a prewarm never moves a live bill.</summary>
    public static int ActShows(string artistId) => artistId != null && actShows.TryGetValue(artistId, out int n) ? n : 0;

    /// <summary>Records that pay: a chart entry, or a career the label is already building.</summary>
    public static bool Traction(SimulatedArtist act) => act.charted > 0 ||
        act.careerState is CareerState.Rising or CareerState.Established or CareerState.Star or CareerState.Superstar;

    /// <summary>Can this act take tonight's slot around its job? Full-time acts work any night the room is open.</summary>
    private static bool FitsWorkingWeek(SimulatedArtist act, SceneRoomProfile room, int day, bool fullTime) =>
        !LocalScenes.DayJobs || fullTime || Weekend(Date(day).DayOfWeek) || room.Kind == SceneRoomKind.Coffeehouse;

    /// <summary>One set already on a person's or act's day: where, when, and in what capacity.</summary>
    private readonly record struct DaySet(string RoomId, string ArtistId, int Start, int End, bool Backing);

    private static void AddDaySet(Dictionary<(int Day, string Id), List<DaySet>> sets, (int Day, string Id) key, DaySet set) {
        if (!sets.TryGetValue(key, out var list)) sets[key] = list = new();
        list.Add(set);
    }

    /// <summary>May this act's people play a set at <paramref name="hour"/> in <paramref name="room"/>, given the rest of
    /// their day? The old rule: nobody already working another room that day. With double rooms, a full-time act may
    /// add one more room, if every one of its people's other sets that day is with this act, as a member, in that one
    /// other room, and ends (or starts) far enough away to cross town.</summary>
    private static bool DayAllows(SimulatedArtist act, SceneRoomProfile room, int day, int hour, string[] people, bool fullTime,
            Dictionary<(int Day, string Id), List<DaySet>> actSets, Dictionary<(int Day, string Id), List<DaySet>> personSets) {
        var elsewhere = new List<DaySet>();
        if (actSets.TryGetValue((day, act.artistId), out var mine)) elsewhere.AddRange(mine.Where(s => s.RoomId != room.Id));
        foreach (string p in people)
            if (personSets.TryGetValue((day, p), out var theirs)) elsewhere.AddRange(theirs.Where(s => s.RoomId != room.Id));
        if (elsewhere.Count == 0) return true;
        if (!LocalScenes.DoubleRooms || !fullTime) return false;
        return elsewhere.All(s => s.ArtistId == act.artistId && !s.Backing && SceneRoomCatalog.Get(s.RoomId)?.PlaceId == room.PlaceId &&
                (s.End + TravelHours <= hour || hour + 1 + TravelHours <= s.Start)) &&
            elsewhere.Select(s => s.RoomId).Distinct().Count() <= MaxRoomsPerDay - 1;
    }
}
