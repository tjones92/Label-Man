using System;
using System.Collections.Generic;
using System.Linq;

public enum StudioEngineerHabit { CloseMicrophone, RidesTheFader, QuickSetup, ListensHard }

/// <summary>One finished or abandoned incremental date, remembered by the label: who engineered it, for which act,
/// and which parts of the songs were heard to fail. Return visits read this; nothing here is a quality multiplier.</summary>
public sealed class StudioVisit {
    public string SessionId, RoomId, RoomName, CityId, EngineerId, EngineerName, ArtistId;
    public GameDate Date;
    public int Passes, KeptSides;
    public bool Printed;
    public List<int> FlaggedParts = new();
    public StudioVisit Copy() => new() {
        SessionId = SessionId, RoomId = RoomId, RoomName = RoomName, CityId = CityId, EngineerId = EngineerId, EngineerName = EngineerName,
        ArtistId = ArtistId, Date = Date, Passes = Passes, KeptSides = KeptSides, Printed = Printed, FlaggedParts = new(FlaggedParts ?? new())
    };
}

/// <summary>The people and acoustics of the fictional local rooms. Every value is derived from a stable key, never a
/// random stream, so viewing a room card cannot move a seeded run. Ranges are provisional authored design roles.</summary>
public static class StudioPeople {
    public const int FamiliarSetupMinutes = 12;
    private static float Between(string key, float low, float high) => low + (float)StudioTakeEvidence.Unit(key) * (high - low);

    /// <summary>Acoustic character per room role: intimacy flatters a close vocal, liveliness lifts big ensemble
    /// songs, control keeps loud parts from spilling. A cheap close room can suit a ballad better than the theatre.</summary>
    public static (float Intimacy, float Liveliness, float Control) Acoustics(PlayerDesk.StudioTier tier) => tier switch {
        PlayerDesk.StudioTier.Budget => (.70f, .30f, .35f),
        PlayerDesk.StudioTier.Mid => (.50f, .35f, .75f),
        _ => (.30f, .80f, .55f)
    };

    /// <summary>One of a room's two engineers, as the room has them on a given day.</summary>
    public sealed class Engineer {
        public string Id, Name;
        public bool Chief, Away, OnByDefault;
        public float Ear, Hands;
        public StudioEngineerHabit Habit;
    }

    /// <summary>A room's chief and second engineer on this date. The chief is away cutting elsewhere on roughly one day
    /// in six, and on about half the remaining days is booked with the room's regular clients, so a new label gets the
    /// second engineer unless it asks. Pure and keyed: listing them draws no gameplay randomness.</summary>
    public static List<Engineer> Engineers(StudioRoomSnapshot room, PlayerDesk.StudioTier tier, GameDate date) {
        string day = $"{date.year}-{date.month}-{date.day}";
        bool away = StudioTakeEvidence.Unit($"{room.Id}|engineer-away|{day}") < 1 / 6.0;
        bool booked = !away && StudioTakeEvidence.Unit($"{room.Id}|chief-booked|{day}") < .5;
        (float earLow, float earHigh, float handsLow, float handsHigh) = room.Specialist ? (.70f, .95f, .60f, .90f) : tier switch {
            PlayerDesk.StudioTier.Budget => (.45f, .75f, .35f, .60f),
            PlayerDesk.StudioTier.Mid => (.55f, .85f, .50f, .75f),
            _ => (.60f, .90f, .55f, .85f)
        };
        Engineer Make(string id, bool chief) => new() {
            Id = id, Chief = chief, Away = chief && away, OnByDefault = chief ? !away && !booked : away || booked,
            Name = RolodexDirectory.PersonName("studio-engineer:" + id, StudioTakeEvidence.Unit(id + "|female") < .12),
            // A second engineer is competent but has a weaker ear and hands than the chief's range allows.
            Ear = Between(id + "|ear", earLow, earHigh) - (chief ? 0 : .12f),
            Hands = Between(id + "|hands", handsLow, handsHigh) - (chief ? 0 : .06f),
            Habit = (StudioEngineerHabit)(int)(StudioTakeEvidence.Unit(id + "|habit") * 4 % 4)
        };
        return new() { Make(room.Id + "|house", true), Make(room.Id + "|second", false) };
    }

    /// <summary>The chief's premium when asked for by name: a quarter of the room's hourly rate, at least $3.</summary>
    public static float ChiefPremium(StudioRoomSnapshot room) => Math.Max(3, (float)Math.Round(room.HourlyRate * .25f));

    /// <summary>Puts the engineer on the board: the one asked for if they are in today, otherwise the room's choice.</summary>
    public static void StaffFor(StudioRoomSnapshot room, PlayerDesk.StudioTier tier, GameDate date, string requestedId = null) {
        var staff = Engineers(room, tier, date);
        var asked = staff.FirstOrDefault(e => e.Id == requestedId && !e.Away);
        var on = asked ?? staff.First(e => e.OnByDefault);
        room.EngineerId = on.Id; room.EngineerName = on.Name; room.EngineerHouse = on.Chief;
        room.EngineerEar = on.Ear; room.EngineerHands = on.Hands; room.EngineerHabit = on.Habit;
        room.EngineerRequested = asked != null && !asked.OnByDefault;
        room.EngineerPremium = room.EngineerRequested && on.Chief ? ChiefPremium(room) : 0;
    }

    public static string HabitText(StudioEngineerHabit habit) => habit switch {
        StudioEngineerHabit.CloseMicrophone => "sets the singer close to the microphone, which helps a quiet lyric",
        StudioEngineerHabit.RidesTheFader => "rides the fader through the loud parts, so less of the band spills over the vocal",
        StudioEngineerHabit.QuickSetup => "gets a sound fast; setup takes a few minutes less",
        _ => "listens hard on playback and explains what the tape shows"
    };

    /// <summary>The previous date this engineer worked with this act, if any.</summary>
    public static StudioVisit LastWith(IEnumerable<StudioVisit> visits, string engineerId, string artistId) =>
        visits?.Where(v => v.EngineerId == engineerId && v.ArtistId == artistId).OrderByDescending(v => v.Date.year * 10000 + v.Date.month * 100 + v.Date.day).FirstOrDefault();

    public static int SetupMinutes(StudioRoomSnapshot room, bool familiar) =>
        Math.Max(10, (familiar ? FamiliarSetupMinutes : PlayerDesk.StudioSetupMinutes) - (room.EngineerHabit == StudioEngineerHabit.QuickSetup ? 5 : 0));

    /// <summary>How the room and engineer change a pass of this song. Bounded to a few hundredths on each aspect.</summary>
    public static (float Vocal, float Feel, float Capture) RoomEffect(StudioRoomSnapshot room, StudioSongStructure song) {
        if (room == null || room.CapabilityVersion < 3 || song == null) return (0, 0, 0);
        float big = (song.PowerDemand + song.EnsembleDemand) * .5f - .5f, close = song.LyricDemand - .5f;
        if (song.Instrumental) close = -.2f;
        float vocal = .25f * (room.Intimacy - .5f) * (close - big);
        if (room.EngineerHabit == StudioEngineerHabit.CloseMicrophone && !song.Instrumental) vocal += .015f * Math.Max(0, close + .3f);
        float feel = .25f * (room.Liveliness - .5f) * big;
        float spill = .08f * (1 - room.Control) * Math.Max(0, big + .1f) * (room.EngineerHabit == StudioEngineerHabit.RidesTheFader ? .5f : 1);
        float capture = (room.EngineerHands - .5f) * .08f - spill;
        return (Math.Clamp(vocal, -.04f, .04f), Math.Clamp(feel, -.04f, .04f), Math.Clamp(capture, -.05f, .04f));
    }

    /// <summary>The engineer's opinion of a song in this room: an expectation, read through that engineer's ear: a weaker
    /// ear can be wrong about a close call. Says nothing about any take.</summary>
    public static string SongInRoom(StudioRoomSnapshot room, StudioSongStructure song, string title) {
        if (room == null || room.CapabilityVersion < 3 || song == null) return null;
        var (vocal, feel, _) = RoomEffect(room, song);
        float big = (song.PowerDemand + song.EnsembleDemand) * .5f - .5f;
        float spill = -.08f * (1 - room.Control) * Math.Max(0, big + .1f) * (room.EngineerHabit == StudioEngineerHabit.RidesTheFader ? .5f : 1);
        float expected = vocal + feel + spill + (float)(StudioTakeEvidence.Unit($"{room.EngineerId}|opinion|{title}") * 2 - 1) * .02f * (1 - room.EngineerEar);
        string first = room.EngineerName.Split(' ')[0];
        return expected > .006f ? $"{first} on \"{title}\": \"That will sit nicely in this room.\""
            : expected < -.006f ? $"{first} on \"{title}\": \"This room may fight that one{(spill < -.01f ? "; the loud parts will spill" : "")}.\""
            : $"{first} on \"{title}\": \"The room won't help or hurt it much.\"";
    }

    public static string AcousticsText(StudioRoomSnapshot room) => room.Specialist ? room.Character : room.Intimacy >= .6f ? "Dry and close: flatters an intimate vocal; loud choruses spill."
        : room.Liveliness >= .6f ? "Big and lively: lifts a full band and a big voice; a hushed lyric can get lost."
        : "Controlled and even: loud parts stay separate, but the room adds little of its own.";
    /// <summary>The room's sound in two or three words, for the room card's tag.</summary>
    public static string SoundTag(StudioRoomSnapshot room) => room.CapabilityVersion < 3 ? null : room.Specialist ? room.Specialty
        : room.Intimacy >= .6f ? "Dry & close" : room.Liveliness >= .6f ? "Big & lively" : "Controlled & even";
    /// <summary>What the room's sound does to a record, in one line, without the tag.</summary>
    public static string AcousticsShort(StudioRoomSnapshot room) => room.Specialist ? room.Character : room.Intimacy >= .6f ? "Flatters an intimate vocal; loud choruses spill."
        : room.Liveliness >= .6f ? "Lifts a full band and a big voice; a hushed lyric can get lost." : "Loud parts stay separate, but the room adds little of its own.";
}

public partial class PlayerDesk {
    private readonly List<StudioVisit> studioVisits = new();
    public IReadOnlyList<StudioVisit> StudioVisits => studioVisits.Select(v => v.Copy()).ToList();

    /// <summary>What the engineer on duty remembers about this act, for the booking card. Null on a first meeting.</summary>
    public string StudioMemory(StudioRoomSnapshot room, string artistId) {
        var last = StudioPeople.LastWith(studioVisits, room?.EngineerId, artistId);
        if (last == null) return null;
        string first = room.EngineerName.Split(' ')[0];
        int trouble = (last.FlaggedParts ?? new()).Select((n, i) => (n, i)).Where(x => x.n > 0).OrderByDescending(x => x.n).ThenBy(x => x.i).Select(x => x.i).DefaultIfEmpty(-1).First();
        string remembered = trouble < 0 ? "nothing came apart worth noting" : $"the {StudioSongStructures.PartName((StudioSectionKind)trouble)} gave them trouble";
        return $"{first} engineered their date on {last.Date.ToHeadlineString()} ({last.Passes} {(last.Passes == 1 ? "pass" : "passes")}). " +
            $"{first} remembers where they stand at the microphones, so setup should take {room.SetupMinutes}m, and that {remembered}.";
    }

    private void RememberStudioVisit(PendingSession session, bool printed) {
        if (session?.FlowVersion != 1 || session.Room?.CapabilityVersion < 3 || string.IsNullOrEmpty(session.Room.EngineerId)) return;
        if (studioVisits.Any(v => v.SessionId == session.SessionId)) return;
        var flagged = new List<int>(new int[Enum.GetValues<StudioSectionKind>().Length]);
        foreach (var cut in session.Cuts) foreach (var take in cut.Takes)
            if (cut.Structure != null && take.TroubleSection >= 0 && take.TroubleSection < cut.Structure.Sections.Count) flagged[(int)cut.Structure.Sections[take.TroubleSection].Kind]++;
        studioVisits.Add(new StudioVisit {
            SessionId = session.SessionId, RoomId = session.Room.Id, RoomName = session.Room.Name, CityId = session.Room.CityId,
            EngineerId = session.Room.EngineerId, EngineerName = session.Room.EngineerName, ArtistId = session.ArtistId, Date = session.Date,
            Passes = session.Cuts.Sum(c => c.Takes.Count), KeptSides = session.Cuts.Count(c => c.KeptTake >= 0), Printed = printed, FlaggedParts = flagged
        });
    }

    private List<StudioVisit> CaptureStudioVisits() => studioVisits.Count == 0 ? null : studioVisits.Select(v => v.Copy()).ToList();
    private static bool ValidateStudioVisits(List<StudioVisit> saved, out string message) {
        int parts = Enum.GetValues<StudioSectionKind>().Length;
        bool valid = saved == null || saved.All(v => v != null && !string.IsNullOrEmpty(v.SessionId) && !string.IsNullOrEmpty(v.EngineerId) &&
            !string.IsNullOrEmpty(v.ArtistId) && v.Passes >= 0 && v.KeptSides >= 0 && v.FlaggedParts != null && v.FlaggedParts.Count == parts &&
            v.FlaggedParts.All(n => n >= 0)) && saved.Select(v => v.SessionId).Distinct().Count() == saved.Count;
        message = valid ? "" : "The saved studio history is damaged.";
        return valid;
    }
    private void RestoreStudioVisits(List<StudioVisit> saved) {
        studioVisits.Clear();
        if (saved != null) studioVisits.AddRange(saved.Select(v => v.Copy()));
    }
}
