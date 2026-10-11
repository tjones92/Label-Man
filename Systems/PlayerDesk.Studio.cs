using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum StudioDirection { None, HoldBackDrums, LeanIntoLyric, TightenEnding }
public enum StudioBrief { CaptureLiveAct, TightenSingle, Explore }

public sealed class StudioMasterHistory {
    public string SessionId, TakeId, RoomName, Layout;
	public StudioDirection Direction;
    public int KeptTake, ElapsedMinutes;
    public float Cost;
    public List<string> Participants = new(), Notes = new(), Work = new();
    public static StudioMasterHistory From(PlayerDesk.PendingSession session, PlayerDesk.SessionTake take) => new() {
        SessionId = session.SessionId, TakeId = take.TakeId, RoomName = session.Room.Name, Layout = session.Room.Layout,
		Direction = take.Direction,
        KeptTake = take.Number, ElapsedMinutes = session.ElapsedMinutes, Cost = session.Cost,
        Participants = new(session.ParticipantIds), Work = new(session.ActionLog),
        Notes = new() { take.EngineerNote, take.PerformerNote, take.BandNote, take.ProducerNote }
    };
}

public partial class PlayerDesk {
    public const int StudioSetupMinutes = 20, StudioTakeMinutes = 12, StudioRehearsalMinutes = 15;
    public const int StudioFinishMinutesPerSide = 10;
    public bool InStudio => pendingSession?.FlowVersion > 0;
    internal static int FinishMinutes(PendingSession session) => session.Cuts.Count * StudioFinishMinutesPerSide;
    public int StudioMinutesLeft(PendingSession session) {
        var time = TimeManager.Instance;
        if (time == null || session == null || time.CurrentDate != session.Date) return 0;
        return Math.Max(0, session.StartMinute + session.Hours * 60 - time.CurrentHour * 60 - time.CurrentMinute);
    }

    private bool StartStudioDate(SimulatedArtist artist, List<MaterialChoice> choices, StudioTier tier, int hours, float roomCost, int start, StudioBrief brief, out string message) {
        if (!Enum.IsDefined(brief)) { message = "Unknown session brief."; return false; }
        if (!StudioRoomCatalog.Available(Label.homeCityId, Label.homeRegion).Contains(tier)) { message = "That kind of room is not available locally."; return false; }
        if (StudioSetupMinutes + choices.Count * (StudioTakeMinutes + StudioFinishMinutesPerSide) > hours * 60) {
            message = "This plan cannot fit setup, one full pass per song and finishing. Choose fewer songs or more room time."; return false;
        }
        var people = artist.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
        if (people.Count == 0) { message = "This act has no available performers."; return false; }
        var today = TimeManager.Instance.CurrentDate;
        if (!LocalSceneRoomService.StudioParticipantsFree(Label.labelId, people, today, start, start + hours * 60)) {
            message = "A performer has a show or appointment during this date. Choose another start or a shorter date."; return false;
        }
        float playerFee = SessionPlayerFor(artist.artistId)?.Fee ?? 0;
        if (Label.cashReserves < roomCost + playerFee) { message = $"The room and booked musician cost ${roomCost + playerFee:N0}; you are ${roomCost + playerFee - Label.cashReserves:N0} short."; return false; }
        var session = new PendingSession {
            ArtistId = artist.artistId, Date = today, Tier = tier, Hours = hours, Cost = roomCost + playerFee, ManualSelection = true,
            FlowVersion = 1, StartMinute = start, ParticipantIds = people, Brief = brief,
            SessionId = $"studio:{LocalSceneIdentityService.KeyedSeed}:{Label.labelId}:{artist.artistId}:{today}:{start}:{counter}",
            Room = StudioRoomCatalog.Local(Label.homeCityId, Label.homeRegion, tier),
            Tension = artist.RoomTension(), ProducerCraft = Label.productionQuality,
            StudioPerformance = artist.studioPerformance, LivePerformance = artist.livePerformance
        };
        foreach (var choice in choices) session.Cuts.Add(new SessionCut { Choice = choice });
        // Enter a paid date now, not a future reservation. Only setup advances the player clock.
        if (!SpendMinutes(StudioSetupMinutes)) { message = "Setup no longer fits today."; return false; }
        Label.cashReserves -= roomCost; Label.monthlyExpenses += roomCost;
        artist.unrecoupedAdvance += roomCost; // Preserve the existing room-cost recoupment owner; hired fees stay separate.
        session.SessionLift = ConsumeSessionPlayer(artist);
        session.ElapsedMinutes = StudioSetupMinutes;
        session.WorkloadMinutes = StudioSetupMinutes * .25f;
        session.ActionLog.Add($"Setup: {StudioSetupMinutes} minutes. Room ${roomCost:N0}; booked musician ${playerFee:N0}; stock, playback and house finish included.");
        pendingSession = session;
        KeepSessionPromises(artist);
        Note($"Started a {hours}h paid date at {session.Room.Name} with {artist.stageName}, {choices.Count} side(s), ${session.Cost:N0}.");
        message = "The room is set. Rehearse, give a direction, or cut the first pass.";
        Changed?.Invoke(); return true;
    }

    private bool SpendStudioWork(PendingSession session, int minutes, string description, out string message, bool finishing = true) {
        if (IsGameOver || TimeManager.Instance?.IsGameOver == true) { message = "The operating window has ended."; return false; }
        int reserve = finishing ? 0 : FinishMinutes(session);
        if (StudioMinutesLeft(session) < minutes + reserve || !RequireHome(out message)) {
            message = "That work will not fit while protecting the quoted finishing time. Keep a take, move to the other side, or finish."; return false;
        }
        if (!SpendMinutes(minutes)) { message = "That work cannot fit before today's hard stop."; return false; }
        session.ElapsedMinutes += minutes; session.ActionNumber++;
        session.ActionLog.Add($"{description}: {minutes} minutes.");
        message = description + "."; return true;
    }

    public bool RehearseStudio(int side, out string message) {
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (!SpendStudioWork(session, StudioRehearsalMinutes, $"Rehearsed {cut.Choice.Title}", out message, false)) return false;
        cut.Rehearsals++; session.WorkloadMinutes += StudioRehearsalMinutes * .6f;
        Changed?.Invoke(); return true;
    }

    public bool DirectStudio(int side, StudioDirection direction, out string message) {
        if (!Enum.IsDefined(direction)) { message = "Unknown studio direction."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (direction == cut.Direction) { message = "That direction is already in place."; return false; }
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        if (direction == StudioDirection.HoldBackDrums && !artist.members.Any(m => m.isActive &&
            (m.primaryRole == MusicianRole.Drums || m.secondaryRole == MusicianRole.Drums))) {
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

    public bool BreakStudio(out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1) { message = "No active studio date."; return false; }
        if (!SpendStudioWork(session, 10, "Short break", out message, false)) return false;
        session.WorkloadMinutes = Math.Max(0, session.WorkloadMinutes - 7);
        Changed?.Invoke(); return true;
    }

    public bool CutStudioTake(int side, out string message) {
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (!SpendStudioWork(session, StudioTakeMinutes, $"Cut {cut.Choice.Title}, take {cut.Takes.Count + 1}", out message, false)) return false;
        int number = cut.Takes.Count + 1;
        string key = $"{session.SessionId}|{side}|action:{session.ActionNumber}|take:{number}";
        float Noise(string purpose) => (float)(StudioTakeEvidence.Unit(key + "|" + purpose) * 2 - 1);
        float skill = session.StudioPerformance, fatigue = Math.Clamp(session.WorkloadMinutes / (180 + 120 * skill), 0, 1);
        float learning = Math.Min(.07f, cut.Rehearsals * .025f + (number - 1) * .008f);
        float baseHook = cut.Choice.WrittenSong?.Hook ?? Math.Clamp(skill * .5f + session.LivePerformance * .2f + .15f, 0, 1);
        float vocal = Mathf.Clamp(baseHook * .75f + session.Room.Craft * .10f + .02f + .12f * Noise("vocal") * (1 + session.Tension) - fatigue * .10f, 0, 1);
        float ensemble = Mathf.Clamp(skill * .65f + .15f + learning + .10f * Noise("ensemble") - fatigue * .07f, 0, 1);
        float feel = Mathf.Clamp(baseHook * .75f + session.LivePerformance * .12f + .10f * Noise("feel") - fatigue * .08f, 0, 1);
        float captureBase = session.Tier == StudioTier.Budget ? .30f : session.Tier == StudioTier.Top ? .62f : .46f;
        float capture = Mathf.Clamp(captureBase + skill * .22f + session.Room.Craft * .12f + .01f + .09f * Noise("capture") + session.SessionLift, 0, 1);
        if (cut.Direction == StudioDirection.HoldBackDrums) { capture += .035f; feel -= .025f; }
        if (cut.Direction == StudioDirection.LeanIntoLyric) { vocal += .04f; ensemble -= .02f; }
        if (cut.Direction == StudioDirection.TightenEnding) { ensemble += .04f; feel -= .02f; }
        var take = new SessionTake {
            TakeId = key, Number = number, Vocal = Mathf.Clamp(vocal, 0, 1), Ensemble = Mathf.Clamp(ensemble, 0, 1),
			Direction = cut.Direction,
            Feel = Mathf.Clamp(feel, 0, 1), Capture = Mathf.Clamp(capture, 0, 1),
            Hook = Mathf.Clamp(vocal * .55f + feel * .30f + ensemble * .15f + session.SessionLift * .3f, 0, 1), Production = Mathf.Clamp(capture, 0, 1)
        };
        cut.Takes.Add(take); session.WorkloadMinutes += StudioTakeMinutes;
        StudioTakeEvidence.Observe(session, cut, side, session.ProducerCraft, skill);
        // A new pass never changes a kept selection or destroys an earlier performance.
        message = $"Take {number} is complete and retained. Compare it with the earlier passes before choosing.";
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

    public bool RunStudioDate(out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1) { message = "No incremental date to run."; return false; }
        for (int side = 0; side < session.Cuts.Count; side++) {
            var cut = session.Cuts[side]; if (cut.KeptTake >= 0) continue;
            int otherEmpty = session.Cuts.Skip(side + 1).Count(c => c.Takes.Count == 0);
            int reserve = FinishMinutes(session) + otherEmpty * StudioTakeMinutes;
            if (cut.Takes.Count == 0 && session.Brief == StudioBrief.TightenSingle && cut.Rehearsals == 0 &&
                StudioMinutesLeft(session) >= StudioRehearsalMinutes + StudioTakeMinutes + reserve && !RehearseStudio(side, out message)) return false;
            if (cut.Takes.Count == 0) {
                if (StudioMinutesLeft(session) < StudioTakeMinutes + reserve || !CutStudioTake(side, out message)) { message = "The date ended with an uncut side; existing takes remain safe."; return false; }
            }
            // Stop on a promising attributed read. At most one retry, with the flip and finishing protected.
            var chosen = cut.Takes[StudioTakeEvidence.Choose(cut.Takes)];
            if ((chosen.ProducerNote.Contains("another pass") || session.Brief == StudioBrief.Explore) && StudioMinutesLeft(session) >= StudioTakeMinutes + reserve) {
                if (!CutStudioTake(side, out message)) return false;
            }
            KeepTake(side, StudioTakeEvidence.Choose(cut.Takes));
        }
        message = "The producer has kept a performance for each side. Review the takes or finish the masters.";
        Changed?.Invoke(); return true;
    }
}
