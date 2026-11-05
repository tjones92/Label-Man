using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Where you are in town, and reaching an act away from its sets (playtest 2026-11-05).
///
/// <para><b>Presence.</b> Being in a city used to put you in every one of its venues at once: you could introduce
/// yourself to a booker across town without going there. Now you are at one venue at a time (or elsewhere in town).
/// Going to a venue is a short walk across town; hearing a set or waiting for one takes you there; driving to
/// another city or starting a new day puts you nowhere. A booker is only spoken to in their own venue.</para>
///
/// <para><b>Numbers.</b> A venue that books an act once a week left the player waiting on its calendar to talk to them.
/// You can ask a member for a number after a set, or ask a booker who has had them in the room. Neither always says
/// yes: the answer is a keyed draw for the season, so asking twice does not reroll it. With a number you can call
/// from anywhere to arrange a meeting, rehearsal or demo, or ask about their past label decisions.</para>
///
/// <para><b>Time.</b> Quick questions cost minutes, not hours: a booker question 10, a tip 15, a number 5-10, a call 10.</para>
/// </summary>
public partial class PlayerDesk {
    public const int CrossTownMinutes = 20;
    public const int BookerQuestionMinutes = 10;
    public const int BookerTipMinutes = 15;
    public const int ActHistoryMinutes = 20;
    public const int AskActNumberMinutes = 5;
    public const int AskBookerNumberMinutes = 10;
    public const int PhoneCallMinutes = 10;
    /// <summary>Share of members who give their number after a set; a booker's base share and its rise per bill you've
    /// heard in the room. Design inputs, uncalibrated.</summary>
    public const float ActGivesNumberShare = .85f, BookerGivesNumberBase = .35f, BookerGivesNumberPerBill = .1f, BookerGivesNumberCap = .85f;
    /// <summary>Hours you can ring an act.</summary>
    public const int PhoneFromHour = 9, PhoneToHour = 21;

    private int SceneDay => LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
    private int NowMinute => (TimeManager.Instance?.CurrentHour ?? 0) * 60 + (TimeManager.Instance?.CurrentMinute ?? 0);

    // ---- presence ---------------------------------------------------------------------------------------------------
    /// <summary>The venue you are in right now, or null.</summary>
    public string PresentRoomId {
        get {
            var k = scoutingKnowledge;
            if (k.AtRoomId == null || k.AtRoomDay != SceneDay) return null;
            return SceneRoomCatalog.Get(k.AtRoomId)?.PlaceId == LocalScenePersistenceService.SceneIdFor(CurrentCityId) ? k.AtRoomId : null;
        }
    }
    public bool IsAtSceneRoom(string roomId) => roomId != null && PresentRoomId == roomId;
    private void MarkAtSceneRoom(string roomId) { scoutingKnowledge.AtRoomId = roomId; scoutingKnowledge.AtRoomDay = SceneDay; }
    private void LeaveSceneRoom() { scoutingKnowledge.AtRoomId = null; scoutingKnowledge.AtRoomDay = null; }
    /// <summary>The reason a venue action needs you there first, or null when you are there.</summary>
    public string NotAtRoomReason(SceneRoomProfile room) => room == null ? "No such venue."
        : room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId) ? "This venue is in another town: drive there first."
        : IsAtSceneRoom(room.Id) ? null : $"You're not at {room.Name}. Go there first ({CrossTownMinutes} min across town).";

    public bool CanGoToSceneRoom(string roomId, out string reason) {
        var room = SceneRoomCatalog.Get(roomId);
        reason = Label == null || !LocalScenes.Rooms || room == null ? "No such venue."
            : room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId) ? "This venue is in another town: drive there first."
            : IsAtSceneRoom(roomId) ? $"You're already at {room.Name}."
            : TimeManager.Instance?.CanAffordMinutes(CrossTownMinutes) != true ? $"There isn't {CrossTownMinutes} minutes left in your day."
            : null;
        return reason == null;
    }
    public bool GoToSceneRoom(string roomId, out string message) {
        if (!CanGoToSceneRoom(roomId, out message)) return false;
        var room = SceneRoomCatalog.Get(roomId);
        SpendMinutes(CrossTownMinutes);
        scoutingKnowledge.Visit = null; // leaving a paid visit: coming back costs admission again
        MarkAtSceneRoom(roomId);
        int now = NowMinute;
        message = $"You head across town to {room.Name}." + (now < room.OpenHour * 60 || now >= room.CloseHour * 60
            ? $" It's shut: posted hours are {room.OpenHour:00}:00–{room.CloseHour:00}:00." : "");
        Note(message); Changed?.Invoke(); return true;
    }

    /// <summary>Walk to a venue as part of going to hear or wait for a set there. False (nothing spent) if the walk
    /// would not fit; <paramref name="walked"/> is the minutes it took (0 when you were already there).</summary>
    private bool WalkToRoomFor(SceneRoomProfile room, out int walked) {
        walked = 0;
        if (IsAtSceneRoom(room.Id)) return true;
        if (TimeManager.Instance?.CanAffordMinutes(CrossTownMinutes) != true) return false;
        SpendMinutes(CrossTownMinutes); walked = CrossTownMinutes;
        scoutingKnowledge.Visit = null;
        MarkAtSceneRoom(room.Id);
        return true;
    }

    // ---- numbers ----------------------------------------------------------------------------------------------------
    private List<ScoutingActNumber> ActNumbers => scoutingKnowledge.ActNumbers ??= new();
    private List<ScoutingNumberRefusal> NumberRefusals => scoutingKnowledge.NumberRefusals ??= new();

    /// <summary>A number that still reaches a member of the act, or null.</summary>
    public ScoutingActNumber ActNumber(string artistId) {
        var act = ArtistManager.Instance?.GetArtist(artistId);
        if (act == null || scoutingKnowledge.ActNumbers == null) return null;
        return ActNumbers.LastOrDefault(n => n.ArtistId == artistId &&
            act.members.Any(m => m.personId == n.PersonId && m.isActive && m.lifeState == MemberLifeState.Active));
    }
    private ScoutingNumberRefusal Refusal(string artistId, string sourceId) =>
        NumberRefusals.LastOrDefault(r => r.ArtistId == artistId && r.SourceId == sourceId && SceneDay < r.UntilDay);
    private int SeasonEnd => (SceneDay / DirectApproachSeasonDays + 1) * DirectApproachSeasonDays;
    private float NumberDraw(string artistId, string sourceId) =>
        StableUnit($"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|act-number|{Label.labelId}|{artistId}|{sourceId}|{SceneDay / DirectApproachSeasonDays}");
    private static Musician NumberHolder(SimulatedArtist act) => act?.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active)
        .OrderByDescending(m => m.isBandLeader).ThenByDescending(m => m.isLeadVocalist).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();

    /// <summary>Ask a member for a number after a set you heard today, at the venue. Null reason when you can.</summary>
    public string ActNumberAfterSetBlocked(string artistId) {
        if (!MeetingActionsEnabled) return "Unavailable.";
        if (ActNumber(artistId) != null) return "You already have their number.";
        var speaker = InvitationSpeaker(artistId);
        if (speaker.person == null) return "Ask a free member after a set you heard today, at the venue.";
        if (Refusal(artistId, "act") is { } r) return $"They'd rather you came to the shows for now. Ask again after {LocalSceneRoomService.Date(r.UntilDay).ToHeadlineString()}.";
        return TimeManager.Instance.CanAffordMinutes(AskActNumberMinutes) ? null : "No time left today.";
    }
    public bool AskActForNumber(string artistId, out string message) {
        message = ActNumberAfterSetBlocked(artistId);
        if (message != null) return false;
        var (_, person) = InvitationSpeaker(artistId); var act = ArtistManager.Instance.GetArtist(artistId);
        SpendMinutes(AskActNumberMinutes);
        if (NumberDraw(artistId, "act") < ActGivesNumberShare) {
            ActNumbers.Add(new() { ArtistId = artistId, PersonId = person.personId, PersonName = person.FullName, Source = person.FullName + ", after a set", Day = SceneDay });
            message = $"{person.FullName} writes down a number for you. You can call {act.stageName} from anywhere now.";
        } else {
            NumberRefusals.Add(new() { ArtistId = artistId, SourceId = "act", UntilDay = SeasonEnd });
            message = $"{person.FullName} would rather you caught them at the shows for now.";
        }
        Note(message); Changed?.Invoke(); return true;
    }

    /// <summary>Ask a booker you've met for an act's number, at the booker's venue during its hours. The booker has to
    /// have had the act play there; the better they know you (bills you've heard in their room) the likelier a yes.</summary>
    public string BookerNumberBlocked(string roomId, string artistId) {
        var room = SceneRoomCatalog.Get(roomId);
        if (!MeetingActionsEnabled || room == null || ArtistManager.Instance.GetArtist(artistId) == null) return "Unavailable.";
        if (ActNumber(artistId) != null) return "You already have their number.";
        if (!KnowsSceneBooker(roomId)) return "Meet this venue's booker first.";
        if (LocalSceneRoomService.ShowsAt(roomId, artistId) == 0) return $"{room.ContactName} hasn't had them play {room.Name}.";
        if (NotAtRoomReason(room) is { } away) return away;
        int now = NowMinute;
        if (now < room.OpenHour * 60 || now + AskBookerNumberMinutes > room.CloseHour * 60) return "The booker is in during posted venue hours.";
        if (Refusal(artistId, room.ContactId) is { } r) return $"{room.ContactName} won't hand it out yet. Ask again after {LocalSceneRoomService.Date(r.UntilDay).ToHeadlineString()}.";
        return TimeManager.Instance.CanAffordMinutes(AskBookerNumberMinutes) ? null : "No time left today.";
    }
    public bool AskBookerForNumber(string roomId, string artistId, out string message) {
        message = BookerNumberBlocked(roomId, artistId);
        if (message != null) return false;
        var room = SceneRoomCatalog.Get(roomId); var act = ArtistManager.Instance.GetArtist(artistId);
        float share = Math.Min(BookerGivesNumberCap, BookerGivesNumberBase + BookerGivesNumberPerBill * SceneContactFamiliarity(room.ContactId));
        var holder = NumberHolder(act);
        SpendMinutes(AskBookerNumberMinutes);
        if (holder != null && NumberDraw(artistId, room.ContactId) < share) {
            ActNumbers.Add(new() { ArtistId = artistId, PersonId = holder.personId, PersonName = holder.FullName, Source = room.ContactName + ", booker at " + room.Name, Day = SceneDay });
            message = $"{room.ContactName} gives you {holder.FullName}'s number. You can call {act.stageName} from anywhere now.";
        } else {
            NumberRefusals.Add(new() { ArtistId = artistId, SourceId = room.ContactId, UntilDay = SeasonEnd });
            message = $"{room.ContactName} doesn't give out players' numbers to people {(SceneContactFamiliarity(room.ContactId) > 0 ? "they hardly know" : "they don't know")}. Hearing more of their bills may change that next season.";
        }
        Note(message); Changed?.Invoke(); return true;
    }

    /// <summary>The member you can ring right now, with the last set you heard (meetings are arranged against it).</summary>
    private (ScoutingObservation heard, Musician person) PhoneSpeaker(string artistId) {
        var number = ActNumber(artistId);
        var act = ArtistManager.Instance?.GetArtist(artistId);
        var heard = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        int now = NowMinute;
        if (number == null || act?.lifecycleStatus != ArtistLifecycleStatus.Active || heard == null || SceneRoomCatalog.Get(heard.RoomId) == null ||
            now < PhoneFromHour * 60 || now + PhoneCallMinutes > PhoneToHour * 60 || TimeManager.Instance?.CanAffordMinutes(PhoneCallMinutes) != true) return default;
        var person = act.members.FirstOrDefault(m => m.personId == number.PersonId && m.isActive && m.lifeState == MemberLifeState.Active);
        // Out playing a set right now: nobody answers.
        if (person == null || LocalSceneRoomService.IsOnStage(person.personId, SceneDay, now, now + PhoneCallMinutes)) return default;
        return (heard, person);
    }
    /// <summary>Why you can't ring the act right now, or null when you can.</summary>
    public string PhoneBlocked(string artistId) {
        var number = ActNumber(artistId);
        if (number == null) return "You don't have a number for them.";
        int now = NowMinute;
        if (now < PhoneFromHour * 60 || now + PhoneCallMinutes > PhoneToHour * 60) return $"Call between {PhoneFromHour}:00 and {PhoneToHour}:00.";
        if (TimeManager.Instance?.CanAffordMinutes(PhoneCallMinutes) != true) return "No time left today.";
        return PhoneSpeaker(artistId).person == null ? $"{number.PersonName} doesn't pick up; they may be out playing. Try later." : null;
    }
}
