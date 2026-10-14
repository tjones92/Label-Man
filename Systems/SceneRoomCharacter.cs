using System;

/// <summary>What sets one fictional room apart for a listener (scouting directive §4, §6, Phase 6): who books it,
/// how it pays its acts, what its crowd does, what you can and can't hear there, and where you get a word with
/// the players. Player-side only. Nothing in the AI world reads it, and every pick is a fixed hash of the room id,
/// so it draws no randomness and is the same in every world.
///
/// Character belongs to the kind of ROOM, never to the people who play or listen in it. A loud room hides
/// lyrics from every observer equally. It says nothing about the talent or reliability of its acts.</summary>
public static class SceneRoomCharacter {
    /// <summary>Extra interpretation noise per assessment axis, by room kind (added to ScenePerformanceService's
    /// observer noise). Zero means the room hides nothing on that axis. Design inputs, uncalibrated.</summary>
    public readonly record struct Hearing(float Vocals, float Execution, float Material, float Communication);

    public static Hearing HearingIn(SceneRoomKind kind) => kind switch {
        // Loud and moving: you hear the band and how a floor answers it; words and vocal detail get lost.
        SceneRoomKind.Club => new(.05f, 0f, .07f, 0f),
        // Quiet and seated: everything comes through, but a polite room never tests stage command.
        SceneRoomKind.ListeningRoom => new(0f, 0f, 0f, .05f),
        // Unamplified and close: songs and voices are clear; a full band's drive doesn't show.
        SceneRoomKind.Coffeehouse => new(0f, .04f, 0f, .02f),
        // A big plain hall with echo: group singing carries; words and fine playing blur at the back.
        SceneRoomKind.CommunityHall => new(0f, .03f, .04f, 0f),
        // Bar noise and shouted requests: you see a band hold a room; songs and voices compete.
        SceneRoomKind.Roadhouse => new(.04f, 0f, .05f, 0f),
        // A listening appointment has nobody to win over.
        SceneRoomKind.TradeEvent => new(0f, 0f, 0f, .08f),
        _ => default
    };

    /// <summary>The axis a room makes hard to judge, as a short clause for the notebook, or null.</summary>
    public static string Masks(SceneRoomKind kind, ScoutingQuestion axis) => (kind, axis) switch {
        (SceneRoomKind.Club, ScoutingQuestion.Material) => "the words were hard to follow over the club",
        (SceneRoomKind.Club, ScoutingQuestion.Vocals) => "vocal detail was lost in the club's noise",
        (SceneRoomKind.Roadhouse, ScoutingQuestion.Material) => "the songs competed with the bar",
        (SceneRoomKind.Roadhouse, ScoutingQuestion.Vocals) => "the voice competed with the bar",
        (SceneRoomKind.CommunityHall, ScoutingQuestion.Material) => "the hall's echo blurred the words",
        (SceneRoomKind.CommunityHall, ScoutingQuestion.Ensemble) => "fine playing blurred in the hall's echo",
        (SceneRoomKind.Coffeehouse, ScoutingQuestion.Ensemble) => "a small acoustic room doesn't show a band's full drive",
        (SceneRoomKind.ListeningRoom, ScoutingQuestion.Stage) => "a seated, polite room doesn't test stage command",
        (SceneRoomKind.TradeEvent, ScoutingQuestion.Stage) => "there was no paying audience to win over",
        _ => null
    };

    /// <summary>The kind of room that would answer a question the last room couldn't, as a plain phrase.</summary>
    public static string BetterRoomFor(ScoutingQuestion axis) => axis switch {
        ScoutingQuestion.Material or ScoutingQuestion.Vocals => "a quieter room (a coffeehouse or listening room)",
        ScoutingQuestion.Ensemble => "a club or roadhouse, where a band plays at full strength",
        ScoutingQuestion.Stage => "a room with a crowd to win over (a club or roadhouse)",
        _ => null
    };

    public static string KindName(SceneRoomKind kind) => kind switch {
        SceneRoomKind.Club => "club",
        SceneRoomKind.ListeningRoom => "listening room",
        SceneRoomKind.Coffeehouse => "coffeehouse",
        SceneRoomKind.CommunityHall => "community hall",
        SceneRoomKind.Roadhouse => "roadhouse",
        SceneRoomKind.TradeEvent => "trade listening session",
        _ => "room"
    };

    public static string Crowd(SceneRoomKind kind) => kind switch {
        SceneRoomKind.Club => "A dancing crowd at the bar and on the floor; people come and go between sets.",
        SceneRoomKind.ListeningRoom => "A seated audience with a minimum, listening rather than dancing.",
        SceneRoomKind.Coffeehouse => "A small seated crowd close to the performers.",
        SceneRoomKind.CommunityHall => "A program audience of families and neighbors.",
        SceneRoomKind.Roadhouse => "Working people at tables and the bar; requests get shouted from the floor.",
        SceneRoomKind.TradeEvent => "Publishers, bookers and studio people at a listening appointment; no paying audience.",
        _ => ""
    };

    public static string HearingText(SceneRoomKind kind) => kind switch {
        SceneRoomKind.Club => "Loud. You hear the band and how a floor answers it; words and fine vocal detail get lost.",
        SceneRoomKind.ListeningRoom => "Quiet and close. Voices, arrangements and songs come through; a polite room tests stage command less.",
        SceneRoomKind.Coffeehouse => "Nearly unamplified. Words and writing come through; a full band's power doesn't show.",
        SceneRoomKind.CommunityHall => "A big plain room with echo. Group singing carries; lyrics and fine playing blur at the back.",
        SceneRoomKind.Roadhouse => "Noisy. You see how a band holds a room and takes requests; songs and voices compete with the bar.",
        SceneRoomKind.TradeEvent => "A quiet room with nobody to win over; you hear the music, not how a crowd takes it.",
        _ => ""
    };

    /// <summary>Where you get a word with the players. Describes access, not consent: talking terms is separate.</summary>
    public static string Talk(SceneRoomKind kind) => kind switch {
        SceneRoomKind.Club => "Players are at the bar between sets and after the last one.",
        SceneRoomKind.ListeningRoom => "Introductions go through the booker; players rarely work the tables.",
        SceneRoomKind.Coffeehouse => "Performers sit among the audience between sets.",
        SceneRoomKind.CommunityHall => "The organizer introduces performers after the program.",
        SceneRoomKind.Roadhouse => "Players are easy to reach at the bar; nothing is arranged.",
        SceneRoomKind.TradeEvent => "Introductions are the point of the session.",
        _ => ""
    };

    public static string Pay(SceneRoomKind kind) => SceneLiveEconomics.Terms(kind) switch {
        SceneRoomPayTerms.DoorSplit => "Acts split the door.",
        SceneRoomPayTerms.HouseWage => "The house pays a set wage.",
        SceneRoomPayTerms.Basket => "No door; the basket goes round for the performers.",
        SceneRoomPayTerms.Offering => "No door; a free-will offering.",
        SceneRoomPayTerms.FlatFee => "Players get a flat night's fee.",
        _ => "Nobody is paid to play."
    };

    // ---- the booker -------------------------------------------------------------------------------------------
    private static readonly string[] FirstNames = {
        "Ada", "Al", "Bea", "Bernie", "Cal", "Cora", "Dell", "Dora", "Earl", "Edna", "Floyd", "Fran", "Gus", "Hattie",
        "Hal", "Ida", "Ike", "Inez", "Jack", "June", "Lew", "Lottie", "Mack", "Mae", "Nate", "Nell", "Otto", "Opal",
        "Pearl", "Rudy", "Ruth", "Sid", "Stella", "Tom", "Vera", "Walt", "Wilma", "Zeke", "Lena", "Manny"
    };
    private static readonly string[] Surnames = {
        "Abbott", "Ashby", "Barlow", "Beaumont", "Booker", "Calloway", "Chavez", "Conley", "Dawson", "Delgado", "Dorsey",
        "Easley", "Farrow", "Fitch", "Garrison", "Gibbs", "Hale", "Haskins", "Holloway", "Ingram", "Jarvis", "Keene",
        "Lacey", "Lomax", "Lowe", "Mabry", "Marsh", "McCray", "Nash", "Ortega", "Pace", "Pruett", "Quarles", "Rankin",
        "Rios", "Sayles", "Shaw", "Slater", "Tate", "Tibbs", "Upshaw", "Varner", "Voss", "Wade", "Whitlock", "Yates"
    };

    public static string BookerName(string roomId) =>
        FirstNames[Pick(FirstNames.Length, roomId + "|booker-first")] + " " + Surnames[Pick(Surnames.Length, roomId + "|booker-last")];

    /// <summary>The year the booker started booking this room: what their booking memory can cover.</summary>
    public static int BookingSince(string roomId) => 1946 + Pick(14, roomId + "|booker-since");

    /// <summary>What the booker knows and wants, stated as a scope rather than a trait.</summary>
    public static string BookerRemit(SceneRoomProfile room) =>
        $"{room.ContactName} has booked {room.Name} since {BookingSince(room.Id)}. Knows this room's nights and acts, not the rest of town; " +
        (room.Kind == SceneRoomKind.TradeEvent ? "arranges the sessions for the publishers and studios who pay for them."
            : "filling the room is the job, so the regulars are the ones they talk up.");

    /// <summary>The room in a few plain lines, for the board.</summary>
    public static string Describe(SceneRoomProfile room, int year) =>
        $"{Capitalized(KindName(room.Kind))}, about {SceneLiveEconomics.Capacity(room)} people. {Pay(room.Kind)} {Crowd(room.Kind)} " +
        $"{HearingText(room.Kind)} {Talk(room.Kind)} Programming: {room.Programming(year)}.";

    private static string Capitalized(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    private static int Pick(int length, string key) {
        const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
        ulong hash = offset;
        foreach (char c in "scene-room-character-v1|" + key) { hash ^= c; hash = unchecked(hash * prime); }
        return (int)((hash >> 33) % (ulong)length);
    }
}
