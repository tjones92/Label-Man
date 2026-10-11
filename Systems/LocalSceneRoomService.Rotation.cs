using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Bills that rotate. A working room booked a fresh bill every night, not the same two acts for a quarter.
/// Liverpool, 1961: a Cavern evening carried four groups (Blue Genes, Beatles, Gerry and the Pacemakers, Remo Four,
/// 25 July); a Liverpool Jazz Society afternoon had three acts alternating over five hours (15 March); a lunchtime week
/// ran the Beatles Monday, Wednesday and Friday, the Big Three Tuesday and Derry and the Seniors Thursday. A favoured act
/// worked constantly (the Beatles: 31 Aintree Institute dates in a year, at least 280 Cavern shows in 2.5 years) but
/// beside a changing cast. Rooms also kept residents (the King Kasuals at Club Del Morocco, Nashville, 1962).
/// So: bills sized by the kind of room; the booker's strongest working acts hold standing engagements and close most
/// nights; the rest of the bill is drawn fresh each night, weighted toward acts the room knows and acts that play well.
/// Standing compounds (an act the room keeps booking gets booked more), so a core of familiar acts forms around the
/// regulars, with new faces still getting nights.
/// Draws are the room owner's keyed draws; no global RNG. <c>--disable-scene-room-rotation</c> restores the old rule.</summary>
public static partial class LocalSceneRoomService {
    /// <summary>Standing engagements a room offers at once: its resident acts.</summary>
    public const int RegularsPerRoom = 2;
    /// <summary>Booking weight = (1 + how well the act plays + how well known it is) x (1 + the room's history with it).
    /// Live performance and reputation add up to +2 each; the room's own history multiplies by up to 1 + 6 (saturating
    /// at 4 shows), so a room builds a core of acts it knows. Design inputs, not calibrated.</summary>
    public const double StandingWeight = 6, LiveWeight = 2, ReputationWeight = 2;
    public const int StandingSaturation = 4;

    /// <summary>Acts on a night's bill before any funded program set. A supper/listening room books a featured act and
    /// one other; clubs, roadhouses, coffeehouses and community programs fill the night with three.</summary>
    public static int BaseSlots(SceneRoomProfile room) => !LocalScenes.RoomRotation || room == null ? 2
        : room.Kind == SceneRoomKind.ListeningRoom ? 2 : 3;

    /// <summary>Shows the booker counts. The live calendar counts live shows only, so a pre-rolled history never moves a
    /// live bill (Prewarm); while the past itself is being pre-rolled, that history is the room's memory.</summary>
    private static int BookerShows(string roomId, string artistId, bool preRoll) =>
        standing.TryGetValue(StandingKey(roomId, artistId), out var s) ? s.Shows + (preRoll ? s.PriorShows : 0) : 0;

    private static double BookingWeight(SceneRoomProfile room, SimulatedArtist act, bool preRoll) =>
        (1 + LiveWeight * Math.Clamp(act.livePerformance, 0f, 1f) + ReputationWeight * Math.Clamp(act.reputation, 0f, 1f))
        * (1 + StandingWeight * Math.Min(1.0, BookerShows(room.Id, act.artistId, preRoll) / (double)StandingSaturation));

    /// <summary>Tonight's booking order: one of the room's regulars to close, then everyone else (other regulars included)
    /// by a weighted draw keyed to this room and night (Efraimidis-Spirakis: largest log(u)/w first).</summary>
    private static IEnumerable<SimulatedArtist> NightOrder(SceneRoomProfile room, int day, IReadOnlyCollection<SimulatedArtist> members,
            HashSet<string> regularIds, bool preRoll) {
        double Key(SimulatedArtist a) => Math.Log(Math.Max(1e-12, Draw($"night|{room.Id}|{day}|{a.artistId}"))) / BookingWeight(room, a, preRoll);
        var ordered = members.OrderByDescending(Key).ThenBy(a => a.artistId, StringComparer.Ordinal).ToList();
        var top = ordered.FirstOrDefault(a => regularIds.Contains(a.artistId));
        return top == null ? ordered : ordered.Where(a => !ReferenceEquals(a, top)).Prepend(top);
    }

    /// <summary>Offer standing engagements, up to the room's regulars, through the term's end: the booker's strongest
    /// working acts by the same weight (so history in the room counts). Re-offered each term, so a rising act can take a
    /// lapsed regular's place.</summary>
    private static void OfferEngagements(SceneRoomProfile room, int day, IReadOnlyCollection<SimulatedArtist> members,
            HashSet<string> regularIds, bool preRoll) {
        int open = RegularsPerRoom - regularIds.Count;
        // A trade listening session is an appointment, not a residency.
        if (open <= 0 || !room.IsPerformance) return;
        foreach (var act in members.Where(a => !regularIds.Contains(a.artistId) && Regular(a, room))
                .OrderByDescending(a => BookingWeight(room, a, preRoll)).ThenBy(a => a.artistId, StringComparer.Ordinal).Take(open).ToArray()) {
            State.Engagements.Add(new() { RoomId = room.Id, ArtistId = act.artistId, StartDay = day, ThroughDay = (Term(day) + 1) * 91 - 1 });
            regularIds.Add(act.artistId);
        }
    }

    /// <summary>A working resident of the room's own scene with no planned move: the acts a room can hold as regulars.</summary>
    private static bool Regular(SimulatedArtist act, SceneRoomProfile room) => !SceneEcosystemService.HasPlannedMove(act.artistId) &&
        act.sceneParticipations.Any(p => p.relationship == SceneRelationship.Resident && p.sceneId == room.PlaceId &&
            !p.endWeek.HasValue && p.performanceLevel >= ScenePerformanceLevel.Working);
}
