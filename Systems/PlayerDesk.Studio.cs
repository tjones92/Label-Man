using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum StudioDirection { None, HoldBackDrums, LeanIntoLyric, TightenEnding }
public enum StudioBrief { CaptureLiveAct, TightenSingle, Explore }

public sealed class StudioMasterHistory {
    public string SessionId, TakeId, RoomName, Layout, EngineerName;
	public StudioDirection Direction;
    public StudioArrangement Arrangement;
    public int ArrangementRevision;
    public StudioFinishWork FinishWork;
    public int KeptTake, ElapsedMinutes, DurationSeconds;
    public SongForm Form;
    public float Cost;
    public List<string> Participants = new(), Notes = new(), Work = new();
    // v19: the session players who played on the kept take, with the part and the member they replaced.
    public List<string> SessionPlayers = new();
    // v21: how the kept take was set up and made: key, tempo, ends, balance, layout, incident, overdubs or a splice.
    public List<string> Setup = new();
    public static StudioMasterHistory From(PlayerDesk.PendingSession session, PlayerDesk.SessionTake take, StudioFinishWork finish = null, StudioSongStructure structure = null) => new() {
        DurationSeconds = structure?.DurationSeconds ?? 0, Form = structure?.Form ?? SongForm.Unknown,
        SessionId = session.SessionId, TakeId = take.TakeId, RoomName = session.Room.Name, Layout = session.Room.Layout,
		Direction = take.Direction,
        Arrangement = take.Arrangement, ArrangementRevision = take.ArrangementRevision,
        FinishWork = finish?.Copy(),
        KeptTake = take.Number, ElapsedMinutes = session.ElapsedMinutes, Cost = session.Cost,
        Participants = new(session.ParticipantIds), Work = new(session.ActionLog),
        EngineerName = session.Room.EngineerName, SessionPlayers = PlayerDesk.StudioSessionCredits(session, take),
        Setup = PlayerDesk.StudioTakeSetup(session, take),
        Notes = new[] { take.EngineerNote, take.EngineerDetail, take.PerformerNote, take.BandNote, take.ProducerNote, take.ProducerDetail }
            .Where((note, i) => note != null || i is 0 or 2 or 3 or 4).ToList()
    };
}

public partial class PlayerDesk {
    public const int StudioSetupMinutes = 20, StudioTakeMinutes = 12, StudioRehearsalMinutes = 15;
    public const int StudioFinishMinutesPerSide = 10;
    public bool InStudio => pendingSession?.FlowVersion > 0;
    internal static int FinishMinutes(PendingSession session) => session.Cuts.Count * StudioFinishMinutesPerSide;
    public int StudioMinutesLeft(PendingSession session) {
        var time = TimeManager.Instance;
        if (time == null || session == null || StudioDayIndex(session) < 0) return 0;
        return Math.Max(0, StudioDayStart(session) + session.Hours * 60 - time.CurrentHour * 60 - time.CurrentMinute);
    }

    /// <summary>Which day of the booking today is: 0 on the first, later days only on a lockout; -1 off the booking.</summary>
    internal int StudioDayIndex(PendingSession session) {
        var time = TimeManager.Instance;
        if (time == null || session == null) return -1;
        int days = LocalSceneRoomService.Day(time.CurrentDate) - LocalSceneRoomService.Day(session.Date);
        return days == 0 ? 0 : session.Work != null && days > 0 && days < session.Work.DayStarts.Count ? days : -1;
    }
    private int StudioDayStart(PendingSession session) {
        int day = Math.Max(0, StudioDayIndex(session));
        return day == 0 || session.Work == null ? session.StartMinute : session.Work.DayStarts[day];
    }

    /// <summary>A lockout with days still to come: the room stays set up overnight and the calendar may move one day.</summary>
    public bool StudioSleepsOn => pendingSession?.Work is { } w && w.LockoutDays > w.DayStarts.Count && StudioDayIndex(pendingSession) == w.DayStarts.Count - 1;

    /// <summary>Morning on a lockout: the room is the label's again from the start of the working day. The band sleeps it
    /// off (a night recovers most of a day's wear) and, away from home, sleeps in a motel at the label's expense.</summary>
    private void ProcessStudioLockoutDawn(GameDate date) {
        var session = pendingSession;
        if (session?.Work == null || session.Work.LockoutDays <= session.Work.DayStarts.Count) return;
        int days = LocalSceneRoomService.Day(date) - LocalSceneRoomService.Day(session.Date);
        if (days != session.Work.DayStarts.Count) return;
        session.Work.DayStarts.Add((TimeManager.Instance?.WorkdayStartHour ?? 9) * 60);
        session.Work.DayElapsed.Add(0);
        session.WorkloadMinutes *= .3f;
        string line = $"Day {days + 1} of the lockout: the room was left set up overnight.";
        if (session.Room.CityId != Label.homeCityId) {
            float lodging = BandLodgingPerNight(session.ParticipantIds.Count);
            Label.cashReserves -= lodging; Label.monthlyExpenses += lodging;
            line += $" Band motel ${lodging:N0}.";
        }
        session.ActionLog.Add(line);
    }

    private int studioLateBooking;
    private const string StudioMidnightLine = "Midnight came and went: the small hours tire everyone.";
    /// <summary>The clock's late stop: 2 a.m. on the day a late-night date holds the room, otherwise none.</summary>
    private int StudioLateStop() => studioLateBooking > 0 ? studioLateBooking
        : pendingSession is { FlowVersion: > 0, Work.LateNight: true } && StudioDayIndex(pendingSession) == 0 ? StudioLateStopMinute : 0;

    private bool StartStudioDate(SimulatedArtist artist, List<MaterialChoice> choices, string roomId, string engineerId, int hours, StudioBrief brief, out string message, bool backingSingers = false, StudioBookingOptions options = null) {
        if (!Enum.IsDefined(brief)) { message = "Unknown session brief."; return false; }
        options ??= new StudioBookingOptions();
        if (options.Days is < 1 or > StudioMaxLockoutDays || !Enum.IsDefined(options.Layout)) { message = "Unknown booking options."; return false; }
        var offer = StudioRoomIds().Contains(roomId) ? StudioOffer(roomId, artist.artistId, engineerId) : null;
        if (offer == null) { message = "That room is not one you can book from here."; return false; }
        if (offer.Locked != null) { message = offer.Locked; return false; }
        if (offer.Overnight) { message = $"{offer.Room.Name} is a {offer.DriveHours}h drive: too far to drive and record the same day. Drive up with the band and stay the night."; return false; }
        if (engineerId != null && offer.Room.EngineerId != engineerId) {
            message = StudioPeople.HomeRoomId(engineerId) == roomId ? "That engineer is not in the building today."
                : StudioGuestEngineer(offer.Room, artist.artistId, engineerId, offer.StaffedFor, out _, out _) ?? "That engineer can't come today.";
            return false;
        }
        if (!AtHome && StudioTripHere(artist.artistId) == null) { message = "You need to be back in town, or up with this act on a studio trip, to book a room."; return false; }
        var room = offer.Room; var tier = offer.Tier;
        var clock = TimeManager.Instance;
        int now = clock.CurrentHour * 60 + clock.CurrentMinute, start = now + offer.DriveHours * 60;
        if (!StudioLayoutAllowed(options.Layout, room.Tracks)) { message = $"{room.Name}'s machine can't record that way: {StudioEquipment.LayoutText(room.Tracks).ToLowerInvariant()}."; return false; }
        if (options.LateNight && !options.Evening) { message = "A late night is an evening booking that runs on past midnight."; return false; }
        if (options.Evening && options.Days > 1) { message = "An evening rate is for one evening; a lockout holds the room by the day."; return false; }
        if (options.Evening && (!room.EveningWork || start < StudioEveningStartHour * 60)) {
            message = room.EveningWork ? $"The evening rate starts at {StudioEveningStartHour - 12} p.m." : $"{room.Name} doesn't take evening work."; return false;
        }
        if (offer.Local && room.CityId == Label.homeCityId && LocalSceneRoomService.HasStudioAppointment(Label.labelId, LocalSceneRoomService.Day(clock.CurrentDate), now, now + hours * 60)) {
            message = "The studio is reserved for an unsigned demo during this session. Cancel or attend that appointment first."; return false;
        }
        // A late night runs past the hard stop, to the small hours; any other date has to finish by it.
        if (!Require(options.LateNight ? offer.DriveHours : offer.DriveHours + hours, out message)) {
            if (!offer.Local) message = $"The {offer.DriveHours}h drive and a {hours}h date won't fit before the {clock.HardStopTime} hard stop. Start early in the day.";
            return false;
        }
        if (options.LateNight && start + hours * 60 > StudioLateStopMinute) { message = $"A late night runs to 2 a.m. at the latest; {hours}h from the start won't fit. Book fewer hours."; return false; }
        var structures = choices.Select(c => StudioSongStructures.For(c, artist, clock.CurrentDate.year)).ToList();
        if (room.SetupMinutes + structures.Sum(s => StudioSongStructures.PassMinutes(s) + StudioFinishMinutesPerSide) > hours * 60 * options.Days) {
            message = "This plan cannot fit setup, one full pass per song and finishing. Choose fewer songs or more room time."; return false;
        }
        var people = artist.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
        if (people.Count == 0) { message = "This act has no available performers."; return false; }
        if (!SessionPlayerArrived(artist, out message)) return false;
        var today = clock.CurrentDate;
        if (!LocalSceneRoomService.StudioParticipantsFree(Label.labelId, people, today, now, start + hours * 60)) {
            message = "A performer has a show or appointment during this date. Choose another start or a shorter date."; return false;
        }
        float roomCost = Mathf.Round(StudioRoomCost(room, hours) * StudioLockoutFactor(options.Days) * (options.Evening ? StudioEveningRateFactor : 1)), playerFee = SessionPlayerFor(artist.artistId)?.Fee ?? 0;
        // A specialist owner's house players, re-read for the actual window, and backing voices if asked for.
        var house = room.Specialist ? StudioHouseBand(room, artist, today, start, start + hours * 60) : null;
        if (house?.Locked != null) { message = house.Locked; return false; }
        StudioBackingOffer backing = null;
        if (backingSingers) {
            backing = StudioBackingSingers(room.CityId, artist, today, start, start + hours * 60, house?.Players.Select(p => p.PersonId));
            if (backing == null) { message = StudioNeedsBacking(artist) ? "No backing singers are free in that town for this date." : $"{artist.stageName} has its own singers."; return false; }
        }
        StudioCrewOffer producer = null;
        if (options.ProducerId != null) {
            producer = StudioProducerOffer(room.CityId, artist, today, start, start + hours * 60, (house?.Players.Select(p => p.PersonId) ?? Enumerable.Empty<string>()).Concat(backing?.Voices.Select(v => v.PersonId) ?? Enumerable.Empty<string>()));
            if (producer?.PersonId != options.ProducerId) { message = "That producer isn't free for this date."; return false; }
        }
        float hired = (house?.Insists == true ? house.Fee : 0) + (backing?.Fee ?? 0) + (producer?.Fee ?? 0);
        if (Label.cashReserves < roomCost + playerFee + hired + offer.Gas) {
            float need = roomCost + playerFee + hired + offer.Gas;
            message = $"The room{(playerFee + hired > 0 ? ", musicians" : "")}{(offer.Local ? "" : " and gas")} cost ${need:N0}; you are ${need - Label.cashReserves:N0} short."; return false;
        }
        // An out-of-town date starts with the drive, through the ordinary road owner: its hours, its gas, and the
        // player physically in that town afterwards. The act rides along. On a studio trip the drive is behind you.
        if (!offer.Local && !Travel(room.CityId, out message)) return false;
        start = clock.CurrentHour * 60 + clock.CurrentMinute;
        var session = new PendingSession {
            ArtistId = artist.artistId, Date = today, Tier = tier, Hours = hours, Cost = roomCost + playerFee, ManualSelection = true,
            FlowVersion = 1, StartMinute = start, ParticipantIds = people, Brief = brief, BreakdownVersion = StudioExperience.Breakdowns ? 1 : 0, RulesVersion = StudioRulesCurrent,
            SessionId = $"studio:{LocalSceneIdentityService.KeyedSeed}:{Label.labelId}:{artist.artistId}:{today}:{start}:{counter}",
            Room = room,
            Tension = artist.RoomTension(), ProducerCraft = Label.productionQuality,
            StudioPerformance = artist.studioPerformance, LivePerformance = artist.livePerformance
        };
        for (int i = 0; i < choices.Count; i++) session.Cuts.Add(new SessionCut { Choice = choices[i], Structure = structures[i] });
        session.Work = new StudioDateWork {
            Layout = options.Layout, Tracks = Math.Max(1, room.Tracks), StockPasses = StudioStockIncluded(choices.Count, hours * options.Days, structures.Select(StudioSongStructures.PassMinutes).DefaultIfEmpty(StudioTakeMinutes).Average()),
            LockoutDays = options.Days, DayStarts = new() { start }, DayElapsed = new() { 0 }, Night = options.Evening, LateNight = options.LateNight
        };
        // Enter a paid date now, not a future reservation. Only setup advances the player clock (past the hard stop on a late night).
        studioLateBooking = options.LateNight ? StudioLateStopMinute : 0;
        bool setUp = SpendMinutes(room.SetupMinutes);
        studioLateBooking = 0;
        if (!setUp) { message = "Setup no longer fits today."; return false; }
        Label.cashReserves -= roomCost; Label.monthlyExpenses += roomCost;
        artist.unrecoupedAdvance += roomCost; // Preserve the existing room-cost recoupment owner; hired fees stay separate.
        session.SessionLift = ConsumeSessionPlayer(artist, out var booked);
        if (booked != null && artist.members.FirstOrDefault(m => m.personId == booked.ReplacesPersonId) is { } sat)
            session.Hires.Add(new StudioSessionHire {
                PersonId = booked.PersonId ?? "session:" + booked.Name, Name = booked.Name, ReplacesPersonId = sat.personId, ReplacesName = sat.firstName,
                Role = booked.PersonId != null ? booked.Role : sat.primaryRole, Skill = booked.Skill, Reading = booked.SightReading, Fee = booked.Fee, Lift = session.SessionLift,
                EnsembleLift = Sings(sat.primaryRole) ? 0 : Math.Clamp(.25f * (booked.Skill - MemberPlaying(sat)), 0, .06f)
            });
        session.ElapsedMinutes = room.SetupMinutes; session.Work.DayElapsed[0] = room.SetupMinutes;
        // An evening date comes after everyone's day: the band arrives with some of it still on them.
        session.WorkloadMinutes = room.SetupMinutes * .25f + (options.Evening ? StudioEveningWorkload : 0);
        if (!offer.Local) session.ActionLog.Add($"Drove {offer.DriveHours}h to {DistanceModel.GetCityById(room.CityId)?.name} with the band (${offer.Gas:N0} gas).");
        if (room.Renown >= .01f) session.ActionLog.Add($"Room rate ${room.HourlyRate:N0} an hour (posted before its records got around: ${room.BaseRate:N0}).");
        session.ActionLog.Add($"Setup: {room.SetupMinutes} minutes. Room ${roomCost:N0}; booked musician ${playerFee:N0}; stock, playback and house finish included.");
        if (!string.IsNullOrEmpty(room.EngineerName))
            session.ActionLog.Add($"Engineer: {room.EngineerName}{(room.EngineerHomeRoomId != null ? ", " + StudioPeople.RoleText(room) : room.CapabilityVersion >= 4 ? room.EngineerHouse ? ", the chief" : ", the second engineer" : room.EngineerHouse ? "" : ", relief for the house engineer")}" +
                $"{(room.EngineerRequested ? $", asked for by name{(room.EngineerPremium > 0 ? $" (${room.EngineerPremium:N0} an hour more)" : "")}" : "")}" +
                $"{(room.FamiliarDates > 0 ? $", who has worked {room.FamiliarDates} earlier date(s) with this act" : "")}.");
        foreach (var hire in session.Hires.Where(h => h.Kind == StudioHireKind.Replacement)) session.ActionLog.Add($"Session player: {hire.Name} on {RoleWord(hire.Role).ToLowerInvariant()}, in place of {hire.ReplacesName}.");
        if (house != null && house.Reason != null) session.ActionLog.Add("Owner: " + house.Reason);
        SeatHousePlayers(session, artist, house);
        if (backing != null) SeatBackingSingers(session, artist, backing);
        if (producer != null) { SeatCrew(session, producer); session.ProducerCraft = Math.Max(session.ProducerCraft, producer.Craft); }
        if (options.Days > 1) session.ActionLog.Add($"Locked out the room for {options.Days} days at {hours}h a day (block rate).");
        if (options.Evening) session.ActionLog.Add(options.LateNight ? $"Late night at the evening rate: the room after its day clients have gone, booked on past midnight for {hours}h."
            : "Evening rate: the room after its day clients have gone.");
        if (options.Layout != StudioLayout.Together) session.ActionLog.Add($"Layout: {StudioLayoutName(options.Layout).ToLowerInvariant()} ({StudioEquipment.LayoutText(room.Tracks).ToLowerInvariant()}).");
        if (offer.Introduction != null) session.ActionLog.Add("Introduction: " + offer.Introduction);
        CompleteStudioTrip(session);
        pendingSession = session;
        KeepSessionPromises(artist);
        Note($"Started a {hours}h paid date at {session.Room.Name} with {artist.stageName}, {choices.Count} side(s), ${session.Cost:N0}.");
        message = "The room is set. Rehearse, give a direction, or cut the first pass.";
        Changed?.Invoke(); return true;
    }

    private bool SpendStudioWork(PendingSession session, int minutes, string description, out string message, bool finishing = true) {
        if (IsGameOver || TimeManager.Instance?.IsGameOver == true) { message = "The operating window has ended."; return false; }
        int reserve = finishing ? 0 : FinishMinutes(session);
        if (StudioMinutesLeft(session) < minutes + reserve || !RequireAtStudio(session, out message)) {
            message = "That work will not fit while protecting the quoted finishing time. Keep a take, move to the other side, or finish."; return false;
        }
        if (!SpendMinutes(minutes)) { message = "That work cannot fit before today's hard stop."; return false; }
        session.ElapsedMinutes += minutes; session.ActionNumber++;
        // Past midnight on a late night, every minute tires the band a quarter again.
        int clockNow = TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute, small = Math.Min(minutes, clockNow - 24 * 60);
        if (small > 0) {
            session.WorkloadMinutes += small * StudioSmallHoursWorkload;
            if (!session.ActionLog.Contains(StudioMidnightLine)) session.ActionLog.Add(StudioMidnightLine);
        }
        if (session.Work != null) session.Work.DayElapsed[Math.Clamp(StudioDayIndex(session), 0, session.Work.DayElapsed.Count - 1)] += minutes;
        session.ActionLog.Add($"{description}: {minutes} minutes.");
        message = description + "."; return true;
    }

    public const int StudioPartRehearsalMinutes = 10;
    /// <summary>A full pass on this song: its length twice (the take and a listen back) plus the reset.</summary>
    public static int StudioPassMinutes(SessionCut cut) => StudioSongStructures.PassMinutes(cut?.Structure);
    public static int StudioRehearsalMinutesFor(SessionCut cut) => StudioSongStructures.RehearsalMinutes(cut?.Structure);

    public bool RehearseStudio(int side, out string message) {
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        int minutes = StudioRehearsalMinutesFor(cut);
        if (!SpendStudioWork(session, minutes, $"Rehearsed {cut.Choice.Title}", out message, false)) return false;
        cut.Rehearsals++; session.WorkloadMinutes += minutes * .6f;
        Changed?.Invoke(); return true;
    }

    /// <summary>Work one part of the song without tape. It helps the aspects that part demands and makes that part
    /// less likely to fail, but tires the band like any other run-through.</summary>
    public bool RehearseStudioPart(int side, StudioSectionKind part, out string message) {
        if (!Enum.IsDefined(part)) { message = "Unknown part of the song."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (cut.Structure == null || !cut.Structure.Parts.Contains(part)) { message = "This song has no such part."; return false; }
        if (!SpendStudioWork(session, StudioPartRehearsalMinutes, $"Rehearsed the {StudioSongStructures.PartName(part)} of {cut.Choice.Title}", out message, false)) return false;
        cut.PartRehearsals[(int)part]++; session.WorkloadMinutes += StudioPartRehearsalMinutes * .6f;
        Changed?.Invoke(); return true;
    }

    public bool DirectStudio(int side, StudioDirection direction, out string message) {
        if (!Enum.IsDefined(direction)) { message = "Unknown studio direction."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (direction == cut.Direction) { message = "That direction is already in place."; return false; }
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        if (direction == StudioDirection.HoldBackDrums && !artist.members.Any(m => m.isActive &&
            MemberRoleService.Plays(m, MusicianRole.Drums))) {
            message = "There is no drummer in this lineup to give that direction to."; return false;
        }
        if (direction == StudioDirection.LeanIntoLyric && artist.instrumentalPerformance) {
            message = "This is an instrumental performance; there is no sung lyric to direct."; return false;
        }
        if (artist?.artistCreativeControl == true && direction == StudioDirection.HoldBackDrums && artist.RoomTension() > .55f) {
            message = "The act declines that balance change under its creative control. The kept performances remain safe."; return false;
        }
        if (!SpendStudioWork(session, 10, $"Direction for {cut.Choice.Title}: {DirectionName(direction)}", out message, false)) return false;
        cut.Direction = direction; session.WorkloadMinutes += 3;
        Changed?.Invoke(); return true;
    }

    public static string DirectionName(StudioDirection direction) => direction switch {
        StudioDirection.HoldBackDrums => "Hold back the drums", StudioDirection.LeanIntoLyric => "Lean into the lyric",
        StudioDirection.TightenEnding => "Tighten the ending", _ => "Return to the live arrangement"
    };

    /// <summary>Where in this song a direction lands, for the talkback list. Empty when the song has no such part.</summary>
    /// <summary>How well the engineer on duty reads a take: ear, habit and familiarity with the act. Pre-v3 rooms keep the original constant.</summary>
    public static float StudioEngineerExpertise(StudioRoomSnapshot room) => room == null || room.CapabilityVersion < 3 ? .65f
        : Math.Clamp(room.EngineerEar + (room.EngineerHabit == StudioEngineerHabit.ListensHard ? .1f : 0) + (room.FamiliarDates > 0 ? .08f : 0), 0, 1);

    public static string DirectionPlace(StudioDirection direction, StudioSongStructure structure) => structure == null ? "" : direction switch {
        StudioDirection.HoldBackDrums when structure.Parts.Contains(StudioSectionKind.Chorus) => " in the choruses",
        StudioDirection.LeanIntoLyric when structure.Parts.Contains(StudioSectionKind.Verse) => " in the verses",
        _ => ""
    };

    public bool BreakStudio(out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1) { message = "No active studio date."; return false; }
        if (!SpendStudioWork(session, 10, "Short break", out message, false)) return false;
        session.WorkloadMinutes = Math.Max(0, session.WorkloadMinutes - 7);
        Changed?.Invoke(); return true;
    }

    public bool CutStudioTake(int side, out string message) {
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        int passMinutes = StudioPassMinutes(cut);
        // A pass is booked as a full one: nobody knows beforehand that it will fall apart.
        if (StudioMinutesLeft(session) < passMinutes + FinishMinutes(session) || !RequireAtStudio(session, out message)) {
            message = "That work will not fit while protecting the quoted finishing time. Keep a take, move to the other side, or finish."; return false;
        }
        if (StudioStockLeft(session) <= 0) {
            message = $"The tape stock is used up. Buy another reel (${StudioReelPrice:N0}) or record over a take you don't need."; return false;
        }
        int number = cut.Takes.Count + 1;
        string key = $"{session.SessionId}|{side}|action:{session.ActionNumber + 1}|take:{number}";
        float Noise(string purpose) => (float)(StudioTakeEvidence.Unit(key + "|" + purpose) * 2 - 1);
        float skill = session.StudioPerformance, fatigue = Math.Clamp(session.WorkloadMinutes / (180 + 120 * skill), 0, 1);
        float learning = Math.Min(.07f, cut.Rehearsals * .025f + (number - 1) * .008f);
        float baseHook = cut.Choice.WrittenSong?.Hook ?? Math.Clamp(skill * .5f + session.LivePerformance * .2f + .15f, 0, 1);
        float vocal = Mathf.Clamp(baseHook * .75f + session.Room.Craft * .10f + .02f + .12f * Noise("vocal") * (1 + session.Tension) - fatigue * .10f, 0, 1);
        // A session player standing in for a looser member tightens the band from their first pass (zero without one).
        float sessionEnsemble = Math.Min(.06f, session.Hires.Sum(h => h.EnsembleLift));   // backing voices carry none
        float ensemble = Mathf.Clamp(skill * .65f + .15f + learning + sessionEnsemble + .10f * Noise("ensemble") - fatigue * .07f, 0, 1);
        float feel = Mathf.Clamp(baseHook * .75f + session.LivePerformance * .12f + .10f * Noise("feel") - fatigue * .08f, 0, 1);
        float captureBase = session.Tier == StudioTier.Budget ? .30f : session.Tier == StudioTier.Top ? .62f : .46f;
        float capture = Mathf.Clamp(captureBase + skill * .22f + session.Room.Craft * .12f + .01f + .09f * Noise("capture") + session.SessionLift, 0, 1);
        // The room's acoustics and the engineer on duty suit some songs more than others (zero on pre-v3 rooms).
        var (roomVocal, roomFeel, roomCapture) = StudioPeople.RoomEffect(session.Room, cut.Structure);
        vocal += roomVocal; feel += roomFeel; capture += roomCapture;
        if (cut.Direction == StudioDirection.HoldBackDrums) { capture += .035f; feel -= .025f; }
        if (cut.Direction == StudioDirection.LeanIntoLyric) { vocal += .04f; ensemble -= .02f; }
        if (cut.Direction == StudioDirection.TightenEnding) { ensemble += .04f; feel -= .02f; }
        // A changed part can suit this pass or disappoint. Producer craft narrows the risk;
        // clicking an alternative never awards originality or a guaranteed better performance.
        float arrangementRead = .025f * session.ProducerCraft + (.055f - .02f * Math.Clamp(session.ProducerCraft, 0, 1)) * Noise("arrangement");
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        var (arrVocal, arrEnsemble, arrFeel, arrCapture) = ArrangementEffect(session, cut, act, arrangementRead);
        vocal += arrVocal; ensemble += arrEnsemble; feel += arrFeel; capture += arrCapture;
        // Rules v2: the song's key, tempo and ends, the balance, the room's open faults and the layout.
        var (shapeVocal, shapeEnsemble, shapeFeel, shapeCapture) = StudioShapeEffect(session, cut, act, fatigue, cut.Takes.Count > 0 ? cut.Takes[^1] : null);
        vocal += shapeVocal; ensemble += shapeEnsemble; feel += shapeFeel; capture += shapeCapture;
        // Practised parts lift the aspects they demand. Zero until a part has been worked on.
        var shape = cut.Structure;
        if (shape != null) {
            float power = StudioSongStructures.PartLift(shape, cut.PartRehearsals, StudioAspect.Power), lyric = StudioSongStructures.PartLift(shape, cut.PartRehearsals, StudioAspect.Lyric);
            if (shape.PowerDemand + shape.LyricDemand > 0) vocal += (power * shape.PowerDemand + lyric * shape.LyricDemand) / (shape.PowerDemand + shape.LyricDemand);
            ensemble += StudioSongStructures.PartLift(shape, cut.PartRehearsals, StudioAspect.Ensemble);
            feel += StudioSongStructures.PartLift(shape, cut.PartRehearsals, StudioAspect.Feel);
        }
        // Voices behind the lead fill out a song that calls for them and clutter one that doesn't: a hired group, or the
        // act's own harmony singers on dates booked under rules v2 (zero with neither).
        float backingVoice = StudioHarmonyVoiceOn(session, act);
        if (backingVoice >= 0) {
            float backing = StudioBackingVocal(StudioBackingCall(shape, cut.Choice.WrittenSong?.Genre ?? act.primaryGenre), backingVoice);
            vocal += backing; feel += Math.Max(0, backing) * .4f;
        }
        var take = new SessionTake {
            TakeId = key, Number = number, Vocal = Mathf.Clamp(vocal, 0, 1), Ensemble = Mathf.Clamp(ensemble, 0, 1),
			Direction = cut.Direction,
            Arrangement = cut.Arrangement, ArrangementRevision = cut.ArrangementRevision,
            Feel = Mathf.Clamp(feel, 0, 1), Capture = Mathf.Clamp(capture, 0, 1),
            Hook = Mathf.Clamp(vocal * .55f + feel * .30f + ensemble * .15f + session.SessionLift * .3f, 0, 1), Production = Mathf.Clamp(capture, 0, 1)
        };
        take.Minutes = passMinutes;
        take.SessionPersonIds = session.Hires.Count == 0 ? null : session.Hires.Select(h => h.PersonId).ToList();
        if (StudioRulesV2(session)) take.Work = new StudioTakeWork {
            KeyShift = cut.Work?.KeyShift ?? 0, TempoShift = cut.Work?.TempoShift ?? 0, Trimmed = cut.Work?.Trimmed == true, KeepPickup = cut.Work?.KeepPickup == true,
            Balance = cut.Work?.Balance ?? StudioBalance.Natural, Layout = session.Work?.Layout ?? StudioLayout.Together, DurationSeconds = shape?.DurationSeconds ?? 0
        };
        if (shape != null) {
            // Located reads are frozen with the pass: later rehearsal cannot rewrite where this take went wrong.
            var (section, aspect, severity) = StudioSongStructures.Locate(shape, take, cut.PartRehearsals, cut.Direction, fatigue, session.ProducerCraft, key);
            take.TroubleSection = severity >= .40f ? section : -1;
            take.ProducerDetail = StudioSongStructures.TroubleNote(shape, section, aspect, severity, key);
            take.EngineerDetail = StudioSongStructures.BalanceNote(shape, ArtistManager.Instance.GetArtist(session.ArtistId), take.Capture, key,
                session.Room.CapabilityVersion >= 3 ? .04f + .12f * (1 - StudioEngineerExpertise(session.Room)) : .1f);
            // A loose band can fall apart at its weak part. The chance rests on the player that part rests on;
            // a tight band almost never stops. Dates begun before breakdowns existed never break down.
            if (session.BreakdownVersion > 0 && section >= 0) {
                var fault = StudioFault(session, ArtistManager.Instance.GetArtist(session.ArtistId), aspect);
                // Mostly the band as a unit, partly the one player the weak part rests on.
                float tightness = .35f * fault.Skill + .65f * skill;
                if (StudioTakeEvidence.Unit(key + "|breakdown") < StudioBreakdownChance(severity, tightness, session.Tension, fatigue))
                    BreakDown(take, shape, section, aspect, fault, key);
                // Even the best band flubs now and then: a false start or a fluffed entrance early in the song.
                else if (StudioTakeEvidence.Unit(key + "|flub") < StudioFlubChance(fatigue))
                    Flub(session, take, shape, ArtistManager.Instance.GetArtist(session.ArtistId), key);
            }
        }
        // Rules v2: something small may happen in the room on a pass that got through.
        if (take.Work != null && !take.Incomplete) {
            var (incVocal, incEnsemble, incFeel, incCapture) = StudioIncidentRoll(session, cut, act, take, fatigue, key);
            if (!take.Incomplete && (incVocal != 0 || incEnsemble != 0 || incFeel != 0 || incCapture != 0)) {
                take.Vocal = Mathf.Clamp(take.Vocal + incVocal, 0, 1); take.Ensemble = Mathf.Clamp(take.Ensemble + incEnsemble, 0, 1);
                take.Feel = Mathf.Clamp(take.Feel + incFeel, 0, 1); take.Capture = Mathf.Clamp(take.Capture + incCapture, 0, 1);
                take.Hook = Mathf.Clamp(take.Vocal * .55f + take.Feel * .30f + take.Ensemble * .15f + session.SessionLift * .3f, 0, 1); take.Production = take.Capture;
            }
        }
        if (!SpendStudioWork(session, take.Minutes, take.Work?.Incident == StudioIncident.BrokenString ? $"{cut.Choice.Title}, take {number}: a string broke" : take.FalseStart ? $"{cut.Choice.Title}, take {number}: false start" : take.Incomplete ? $"{cut.Choice.Title}, take {number}: broke down in {shape.Sections[take.TroubleSection].Label}"
            : $"Cut {cut.Choice.Title}, take {number}", out message, false)) return false;
        cut.Takes.Add(take); session.WorkloadMinutes += take.Minutes;
        if (session.Work != null && (!take.FalseStart || take.Work?.Incident == StudioIncident.BrokenString)) session.Work.PassesUsed++;
        if (take.Work?.IncidentNote != null) session.ActionLog.Add(take.Work.IncidentNote);
        StudioTakeEvidence.Observe(session, cut, side, session.ProducerCraft, skill);
        // A new pass never changes a kept selection or destroys an earlier performance.
        message = take.Work?.Incident == StudioIncident.BrokenString ? $"Take {number} stopped: {take.Work.IncidentNote} Earlier takes stay safe."
            :         message = take.FalseStart ? $"Take {number} was a false start. {take.ProducerDetail} It cost {take.Minutes} minutes."
            : take.Incomplete ? $"Take {number} broke down and can't be kept. {take.ProducerDetail} Earlier takes stay safe."
            : $"Take {number} is complete and retained. Compare it with the earlier passes before choosing.";
        Changed?.Invoke(); return true;
    }

    private bool StudioSide(int side, out PendingSession session, out SessionCut cut, out string message) {
        session = pendingSession; cut = null;
        if (session?.FlowVersion != 1 || side < 0 || side >= session.Cuts.Count) { message = "No such side in an active studio date."; return false; }
        var artist = ArtistManager.Instance?.GetArtist(session.ArtistId);
        if (artist == null || artist.labelId != Label?.labelId) { message = "That act is no longer available for this date."; return false; }
        if (!session.ParticipantIds.SequenceEqual(artist.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).Distinct().OrderBy(id => id, StringComparer.Ordinal))) {
            message = "The performing lineup changed. Finish the kept tapes or abandon this date before booking another lineup."; return false;
        }
        cut = session.Cuts[side]; message = ""; return true;
    }

    public bool RunStudioDate(out string message) => RunStudioDate(new StudioDelegation(), out message);

    /// <summary>Hand the rest of the date to the producer: the same actions, clock, notes and refusals as playing it by
    /// hand, under the brief and a hard spending limit (zero: hire nobody, buy nothing). The producer can stop and hand
    /// back at a take that sounds like the record, at an idea someone in the room has, or when a side is in danger.</summary>
    public bool RunStudioDate(StudioDelegation d, out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1) { message = "No incremental date to run."; return false; }
        d ??= new StudioDelegation();
        float spent = 0;
        string who = session.Hires.FirstOrDefault(h => h.Kind == StudioHireKind.Producer)?.Name ?? "The producer";
        int passesBefore = session.Cuts.Sum(c => c.Takes.Count);
        string Done(string text) { int passes = session.Cuts.Sum(c => c.Takes.Count) - passesBefore; Changed?.Invoke(); return passes > 0 ? $"{text} ({passes} pass{(passes == 1 ? "" : "es")} this run{(spent > 0 ? $", ${spent:N0} spent" : "")}.)" : text; }
        bool Afford(float fee) => fee <= d.SpendLimit - spent + .001f;
        bool Pass(int side, out string said) {
            if (StudioStockLeft(session) <= 0 && session.Work != null) {
                if (!Afford(StudioReelPrice) || !BuyStudioStock(out said)) { said = "The tape stock ran out and buying more is beyond the spending limit. Earlier takes remain safe."; return false; }
                spent += StudioReelPrice;
            }
            return CutStudioTake(side, out said);
        }
        for (int side = 0; side < session.Cuts.Count; side++) {
            var cut = session.Cuts[side]; if (cut.KeptTake >= 0) continue;
            int pass = StudioPassMinutes(cut);
            int reserve = FinishMinutes(session) + session.Cuts.Skip(side + 1).Where(c => c.Takes.Count == 0).Sum(StudioPassMinutes);
            if (d.StopAtThreat && session.Cuts.Skip(side + 1).Any(c => c.Takes.Count == 0) && StudioMinutesLeft(session) < pass + reserve + StudioPassMinutes(cut)) {
                message = Done($"{who} stopped: there is barely time for one pass of \"{cut.Choice.Title}\" and the other side. Your call."); return false;
            }
            if (cut.Takes.Count == 0 && session.Brief == StudioBrief.TightenSingle && cut.Rehearsals == 0 &&
                StudioMinutesLeft(session) >= StudioRehearsalMinutesFor(cut) + pass + reserve && !RehearseStudio(side, out message)) return false;
            // Get one complete pass down. After a breakdown the producer works the part that fell apart when time allows;
            // the producer never hires a replacement on the label's behalf.
            for (int attempt = 0; StudioTakeEvidence.Choose(cut.Takes) < 0; attempt++) {
                var stopped = cut.Takes.Count > 0 ? cut.Takes[^1] : null;
                if (attempt >= 3 || StudioMinutesLeft(session) < pass + reserve) {
                    message = Done(cut.Takes.Count == 0 ? "The date ended with an uncut side; existing takes remain safe."
                        : $"\"{cut.Choice.Title}\" kept breaking down, so {(who == "The producer" ? "the producer" : who)} stopped. Earlier takes remain safe; the decision is yours."); return false;
                }
                if (stopped?.Incomplete == true && !stopped.FalseStart && cut.Structure != null && stopped.TroubleSection >= 0 && StudioMinutesLeft(session) >= StudioPartRehearsalMinutes + pass + reserve &&
                    !RehearseStudioPart(side, cut.Structure.Sections[stopped.TroubleSection].Kind, out message)) return false;
                if (!Pass(side, out message)) { message = Done(message); return false; }
            }
            // A song that sounds bare without voices behind the lead: call the town's singers in, if the limit allows.
            if (StudioBackingCallIn() is { } group && Afford(group.Fee) && StudioMinutesLeft(session) >= group.ArrivalMinutes + pass + reserve) {
                if (CallInBackingSingers(out _)) { spent += group.Fee; if (!Pass(side, out message)) { message = Done(message); return false; } }
            }
            if (d.StopAtIdea && StudioIdeas(side).FirstOrDefault(i => i.Arrangement != null || i.Shape != null) is { } idea) {
                message = Done($"{who} stopped to hear an idea out. {idea.Text}"); return false;
            }
            // Stop on a promising attributed read. At most one retry, with the flip and finishing protected.
            var chosen = cut.Takes[StudioTakeEvidence.Choose(cut.Takes)];
            if (d.StopAtPromising && chosen.ProducerNote?.StartsWith("This pass has conviction", StringComparison.Ordinal) == true) {
                message = Done($"{who} stopped: take {chosen.Number} of \"{cut.Choice.Title}\" sounds like the record. Have a listen before keeping it."); return false;
            }
            if ((chosen.ProducerNote.Contains("another pass") || session.Brief == StudioBrief.Explore) && StudioMinutesLeft(session) >= pass + reserve) {
                // Tightening for a single works on the part the producer heard fail, when that still leaves time for the retry.
                var heard = cut.Takes[^1];
                if (session.Brief == StudioBrief.TightenSingle && heard.TroubleSection >= 0 && cut.Structure != null &&
                    StudioMinutesLeft(session) >= StudioPartRehearsalMinutes + pass + reserve &&
                    !RehearseStudioPart(side, cut.Structure.Sections[heard.TroubleSection].Kind, out message)) return false;
                if (!Pass(side, out message)) { message = Done(message); return false; }
            }
            KeepTake(side, StudioTakeEvidence.Choose(cut.Takes));
        }
        message = Done($"{who} has kept a performance for each side. Review the takes or finish the masters.");
        return true;
    }
}

/// <summary>What the producer may do when running the date: spend up to a limit on stock and singers, and where to stop.</summary>
public sealed class StudioDelegation {
    public float SpendLimit;
    public bool StopAtPromising, StopAtIdea, StopAtThreat;
}
