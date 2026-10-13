using System;
using System.Collections.Generic;
using System.Linq;

public enum ScoutingAppointmentActivity { Conversation, Rehearsal, Demo }
public enum SceneConversationStatus { Scheduled, InProgress, Completed, Cancelled, Missed, Declined }
public sealed class SceneConversation {
    public ScoutingAppointmentActivity Activity { get; set; }
    public List<string> ParticipantIds { get; set; } = new();
    public List<SceneSetSong> Material { get; set; } = new();
    public float Fee { get; set; }
    public bool Paid { get; set; }
    public ScenePerformanceSnapshot Sample { get; set; }
    public string Id { get; set; }
    public string LabelId { get; set; }
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string PersonId { get; set; }
    public string PersonName { get; set; }
    public string RoomId { get; set; }
    public string SourceEventId { get; set; }
    public int AgreedDay { get; set; }
    public int AgreedMinute { get; set; }
    public int Day { get; set; }
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
    public SceneConversationStatus Status { get; set; }
    public string Response { get; set; }
}
public sealed class SceneConversationQuote {
    public ScoutingAppointmentActivity Activity { get; set; }
    public List<string> ParticipantIds { get; set; } = new();
    public List<SceneSetSong> Material { get; set; } = new();
    public float Fee { get; set; }
    public string Id { get; init; }
    public string ArtistId { get; init; }
    public string PersonId { get; init; }
    public string PersonName { get; init; }
    public string RoomId { get; init; }
    public string SourceEventId { get; init; }
    public int Day { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
}

// Extends the existing commitment owner with conversations and controlled unsigned samples.
// Performance bookings, the home studio console and cash accounting retain their existing owners.
public static partial class LocalSceneRoomService {
    public const int ConversationMinutes = 60;
    private static bool Pending(SceneConversation c) => c.Status is SceneConversationStatus.Scheduled or SceneConversationStatus.InProgress;
    public static IReadOnlyList<SceneConversation> Conversations(string labelId) => Array.AsReadOnly(
        Copy(state?.Conversations?.Where(c => c.LabelId == labelId).ToArray()) ?? Array.Empty<SceneConversation>());
    public static SceneConversation Conversation(string id) => Copy(state?.Conversations?.FirstOrDefault(c => c.Id == id));

    private static Musician ConversationPerson(SceneConversation c) => ArtistManager.Instance?.GetArtist(c.ArtistId)?.members
        .FirstOrDefault(m => m.personId == c.PersonId && m.isActive && m.lifeState == MemberLifeState.Active);
    private static bool ConversationBooking(string personId, int day, int start, int end, string roomId, string exceptId = null) =>
        state?.Conversations?.Any(c => c.Id != exceptId && Pending(c) && (c.PersonId == personId || c.ParticipantIds.Contains(personId)) && c.Day == day &&
            (c.RoomId != roomId || c.StartMinute < end && c.EndMinute > start)) == true;
    private static string AppointmentResource(ScoutingAppointmentActivity activity, string roomId, string labelId) => activity switch {
        ScoutingAppointmentActivity.Demo => "home-studio|" + labelId,
        ScoutingAppointmentActivity.Rehearsal => "rehearsal-floor|" + roomId,
        _ => "conversation-table|" + roomId
    };
    private static bool ConversationSlotFree(string labelId, string personId, string roomId, int day, int start, int end, string exceptId = null, ScoutingAppointmentActivity activity = ScoutingAppointmentActivity.Conversation) =>
        !StudioPersonReserved(personId, day, start, end) && PlayerDesk.Instance?.StudioTripHolds(personId, day) != true && !HasPerformanceBooking(personId, day, start, end, roomId) && !ConversationBooking(personId, day, start, end, roomId, exceptId) &&
        !(state?.Conversations?.Any(c => c.Id != exceptId && Pending(c) && c.Day == day && c.StartMinute < end && c.EndMinute > start &&
            (c.LabelId == labelId || AppointmentResource(c.Activity, c.RoomId, c.LabelId) == AppointmentResource(activity, roomId, labelId))) ?? false);

    private static bool StudioPersonReserved(string id, int day, int start, int end) => PlayerDesk.Instance?.Session is { FlowVersion: > 0 } session &&
        Day(session.Date) == day && (session.ParticipantIds.Contains(id) || session.Hires.Any(h => h.PersonId == id)) && session.StartMinute < end && session.StartMinute + session.Hours * 60 > start;

    private static bool RehearsalRoomOccupied(string roomId, int day, int start, int end) => state?.Bills.Any(b =>
        b.RoomId == roomId && b.Day == day && b.Status != SceneBillStatus.Cancelled && b.Appearances.Any(a => a.Status != SceneBillStatus.Cancelled && a.StartHour * 60 < end && a.EndHour * 60 > start)) == true;
    // Pure preview: search only already scheduled weeks; do not generate a room, bill or gig.
    internal static SceneConversationQuote QuoteConversation(AILabel label, string artistId, string personId, string roomId, string sourceEventId, ScoutingAppointmentActivity activity = ScoutingAppointmentActivity.Conversation) {
        if (!LocalScenes.Rooms || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver || state == null ||
            !ReferenceEquals(PlayerDesk.Instance?.Label, label)) return null;
        var act = ArtistManager.Instance.GetArtist(artistId); var room = SceneRoomCatalog.Get(roomId); var clock = TimeManager.Instance;
        var person = act?.members.FirstOrDefault(m => m.personId == personId && m.isActive && m.lifeState == MemberLifeState.Active);
        int nowDay = Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (room == null || person == null || act.lifecycleStatus != ArtistLifecycleStatus.Active ||
            state.Conversations.Any(c => c.LabelId == label.labelId && c.ArtistId == artistId &&
                (Pending(c) || c.SourceEventId == sourceEventId))) return null;
        var participants = activity == ScoutingAppointmentActivity.Conversation ? new[] { personId } :
            act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        int duration = activity == ScoutingAppointmentActivity.Demo ? PlayerDesk.MinSessionHours * 60 : ConversationMinutes;
        if (!Enum.IsDefined(activity) || participants.Length == 0) return null;
        if (activity == ScoutingAppointmentActivity.Demo && (PlayerDesk.Instance.Session != null || PlayerDesk.Instance.CurrentCityId != label.homeCityId)) return null;
        if (activity != ScoutingAppointmentActivity.Conversation && participants.Length > 1 && !person.isBandLeader) return null;
        int through = Math.Min((state.LastScheduledWeek + 1) * 7 - 1, Day(GameDate.EndDate));
        for (int day = nowDay + 1; day <= through; day++) {
            var date = Date(day);
            if (date.year < room.FromYear || date.year > room.ThroughYear || (activity != ScoutingAppointmentActivity.Demo && !SceneLiveEconomics.OpenOn(room, date.DayOfWeek)) ||
                !LocalScenePersistenceService.Present(act, room.PlaceId, day / 7) || !SceneEcosystemService.AvailableAt(artistId, room.PlaceId, day)) continue;
            for (int minute = (activity == ScoutingAppointmentActivity.Demo ? clock.WorkdayStartHour : room.OpenHour) * 60; minute + duration <= Math.Min(activity == ScoutingAppointmentActivity.Demo ? clock.HardStopHour : room.CloseHour, clock.HardStopHour) * 60; minute += 60) {
                if (activity == ScoutingAppointmentActivity.Rehearsal && RehearsalRoomOccupied(roomId, day, minute, minute + duration)) continue;
                if (participants.Any(id => !ConversationSlotFree(label.labelId, id, roomId, day, minute, minute + duration, activity: activity))) continue;
                return new() { Activity = activity, ParticipantIds = participants.ToList(), Fee = activity == ScoutingAppointmentActivity.Demo ? PlayerDesk.Instance.SessionCost(PlayerDesk.StudioTier.Budget, PlayerDesk.MinSessionHours) : 0, ArtistId = artistId, PersonId = personId, PersonName = person.FullName, RoomId = roomId,
                    SourceEventId = sourceEventId, Day = day, StartMinute = minute, EndMinute = minute + duration,
                    Id = $"conversation-quote-v1|{label.labelId}|{artistId}|{personId}|{person.FullName}|{roomId}|{sourceEventId}|{day}|{minute}|{act.labelId}|{act.prospectMarketStatus}|{nowDay}|{now}|{activity}|{PlayerDesk.Instance.SessionCost(PlayerDesk.StudioTier.Budget, PlayerDesk.MinSessionHours)}|{string.Join(",", participants)}" };
            }
        }
        return null;
    }

    internal static bool RequestConversation(AILabel label, SceneConversationQuote expected, bool coolingDown,
        out SceneConversation result, out string message) {
        result = null;
        var current = expected == null ? null : QuoteConversation(label, expected.ArtistId, expected.PersonId, expected.RoomId, expected.SourceEventId, expected.Activity);
        if (current == null || current.Id != expected.Id) { message = "The speaker or proposed slot changed. Review the invitation again."; return false; }
        var act = ArtistManager.Instance.GetArtist(current.ArtistId); var clock = TimeManager.Instance;
        bool consents = !coolingDown && ArtistManager.Instance.IsEligibleForPopulationSigning(act, ChartManager.Instance.GetCurrentChartWeek());
        string response = consents ? $"I can meet at {SceneRoomCatalog.Get(current.RoomId).Name} on {Date(current.Day).ToHeadlineString()}, " +
            $"{current.StartMinute / 60:00}:{current.StartMinute % 60:00}, for an hour to discuss a record deal. That isn't a promise to sign." :
            "I'm not arranging a record discussion right now. Hearing us doesn't commit us to a meeting or a deal.";
        if (current.Activity != ScoutingAppointmentActivity.Conversation) response = consents ?
            $"We agree to a {current.Activity.ToString().ToLowerInvariant()} with the named lineup, one agreed song, {current.EndMinute - current.StartMinute} minutes, ${current.Fee:0} payable at attendance. Cancel before starting without charge; consumed time is not refunded. No record deal or release is agreed." :
            "We are not consenting to an unsigned rehearsal or demo now.";
        var record = new SceneConversation { Id = $"conversation-v1|{LocalSceneIdentityService.KeyedSeed}|{label.labelId}|{++State.ConversationOrdinal}",
            Activity = current.Activity, ParticipantIds = current.ParticipantIds, Material = Copy(expected.Material), Fee = current.Fee, LabelId = label.labelId, ArtistId = act.artistId, ArtistName = act.stageName, PersonId = current.PersonId, PersonName = current.PersonName,
            RoomId = current.RoomId, SourceEventId = current.SourceEventId, Day = current.Day, StartMinute = current.StartMinute,
            EndMinute = current.EndMinute, AgreedDay = Day(clock.CurrentDate), AgreedMinute = clock.CurrentHour * 60 + clock.CurrentMinute,
            Status = consents ? SceneConversationStatus.Scheduled : SceneConversationStatus.Declined, Response = response };
        State.Conversations.Add(record); result = Copy(record); message = current.PersonName + ": “" + response + "”"; return true;
    }

    // World clock owns expiry and changed availability. No new appointment is inferred on load.
    internal static void RefreshConversationsForClock() {
        var clock = TimeManager.Instance;
        if (LocalScenes.Rooms && !restoring && clock != null) AdvanceConversations(Day(clock.CurrentDate), clock.CurrentHour * 60 + clock.CurrentMinute);
    }
    private static void AdvanceConversations(int day, int minute) {
        foreach (var c in state?.Conversations ?? new()) {
            if (!Pending(c)) continue;
            string reason = ConversationUnavailable(c);
            if (reason != null) { c.Status = SceneConversationStatus.Cancelled; c.Response = reason; }
            else if (day > c.Day || day == c.Day && minute > c.EndMinute || c.Status == SceneConversationStatus.Scheduled && day == c.Day && minute > c.StartMinute) {
                c.Status = c.Status == SceneConversationStatus.InProgress ? SceneConversationStatus.Cancelled : SceneConversationStatus.Missed;
                c.Response = c.Status == SceneConversationStatus.Missed ? "The agreed start passed without a meeting. No conversation or terms were acquired." :
                    "The meeting did not complete within its agreed slot. No agreement was recorded.";
            }
        }
    }
    private static string ConversationUnavailable(SceneConversation c) {
        var act = ArtistManager.Instance?.GetArtist(c.ArtistId); var room = SceneRoomCatalog.Get(c.RoomId);
        if (act?.lifecycleStatus != ArtistLifecycleStatus.Active || ConversationPerson(c) == null) return "The agreed speaker is no longer available with this act.";
        if (room == null || !LocalScenePersistenceService.Present(act, room.PlaceId, c.Day / 7) || !SceneEcosystemService.AvailableAt(c.ArtistId, room.PlaceId, c.Day))
            return "The act is no longer available at the agreed place and date.";
        if (!ArtistManager.Instance.IsEligibleForPopulationSigning(act, ChartManager.Instance.GetCurrentChartWeek()))
            return "Their side is no longer taking record discussions. The appointment gave no hold on the act.";
        if (c.Activity != ScoutingAppointmentActivity.Conversation && !c.ParticipantIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(
            act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).OrderBy(id => id, StringComparer.Ordinal)))
            return "The agreed lineup changed. Nobody is silently substituted.";
        if (c.Activity == ScoutingAppointmentActivity.Rehearsal && RehearsalRoomOccupied(c.RoomId, c.Day, c.StartMinute, c.EndMinute))
            return "The rehearsal room has a performance booking during the agreed slot.";
        if (c.Activity == ScoutingAppointmentActivity.Demo && PlayerDesk.Instance?.Session != null)
            return "The label's studio console is occupied; complete or scrap its session first.";
        if ((c.ParticipantIds.Count == 0 ? new[] { c.PersonId } : c.ParticipantIds.ToArray()).Any(id => !ConversationSlotFree(c.LabelId, id, c.RoomId, c.Day, c.StartMinute, c.EndMinute, c.Id, c.Activity)))
            return "The agreed speaker or conversation slot now conflicts with a booking.";
        return null;
    }
    internal static void MarkAppointmentPaid(string id, string labelId) {
        var c = state?.Conversations.FirstOrDefault(c => c.Id == id && c.LabelId == labelId);
        if (c?.Status == SceneConversationStatus.InProgress && c.Activity == ScoutingAppointmentActivity.Demo) c.Paid = true;
    }
    internal static bool HasStudioAppointment(string labelId, int day, int start, int end) => state?.Conversations.Any(c => Pending(c) &&
        c.LabelId == labelId && c.Activity == ScoutingAppointmentActivity.Demo && c.Day == day && c.StartMinute < end && c.EndMinute > start) == true;

    internal static bool StudioParticipantsFree(string labelId, IEnumerable<string> people, GameDate date, int start, int end) =>
        people.All(id => !StudioPersonReserved(id, Day(date), start, end) &&
            !(state?.Bills.Any(b => b.Day == Day(date) && b.Status != SceneBillStatus.Cancelled && b.Appearances.Any(a =>
                a.Status != SceneBillStatus.Cancelled && a.StartHour * 60 < end && a.EndHour * 60 > start &&
                (a.PersonIds.Contains(id) || (a.BackingPersonIds?.Contains(id) ?? false)))) ?? false) &&
            !(state?.Conversations.Any(c => Pending(c) && c.Day == Day(date) && c.StartMinute < end && c.EndMinute > start &&
                (c.LabelId == labelId || c.PersonId == id || c.ParticipantIds.Contains(id))) ?? false));
    internal static bool BeginConversation(string id, string labelId, out string message) {
        var c = state?.Conversations.FirstOrDefault(c => c.Id == id && c.LabelId == labelId); var clock = TimeManager.Instance;
        if (c == null || c.Status != SceneConversationStatus.Scheduled) { message = c?.Response ?? "This appointment is not scheduled."; return false; }
        string reason = ConversationUnavailable(c);
        if (reason != null) { c.Status = SceneConversationStatus.Cancelled; c.Response = message = reason; return false; }
        int day = Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (day != c.Day || now != c.StartMinute) { message = "Attend at the agreed start. Use WAIT FOR MEETING to reach it; late arrivals cannot fit the full conversation."; return false; }
        c.Status = SceneConversationStatus.InProgress; message = "Meeting started."; return true;
    }
    internal static bool FinishConversation(string id, string labelId, out string message) {
        var c = state?.Conversations.FirstOrDefault(c => c.Id == id && c.LabelId == labelId);
        if (c?.Status != SceneConversationStatus.InProgress) { message = c?.Response ?? "This meeting did not complete."; return false; }
        var clock = TimeManager.Instance;
        if (Day(clock.CurrentDate) != c.Day || clock.CurrentHour * 60 + clock.CurrentMinute != c.EndMinute) {
            message = "The full agreed hour has not completed."; return false;
        }
        string reason = ConversationUnavailable(c);
        if (reason != null) { c.Status = SceneConversationStatus.Cancelled; c.Response = message = reason; return false; }
        if (c.Activity != ScoutingAppointmentActivity.Conversation) {
            var act = ArtistManager.Instance.GetArtist(c.ArtistId);
            var people = act.members.Where(m => c.ParticipantIds.Contains(m.personId)).ToArray();
            var voices = people.Where(m => m.isLeadVocalist).ToArray();
            float Sample(string axis, float value) => Math.Clamp(value + (float)(LocalSceneIdentityService.AssignmentUnit(
                LocalSceneIdentityService.KeyedSeed, c.Id + "|" + axis) * 2 - 1) * .18f, 0, 1);
            c.Sample = new() { Id = c.Id, Vocals = voices.Length == 0 ? null : Sample("vocals", voices.Average(m => m.axesVersion > 0 ? (m.vocalPower + m.vocalControl) / 2 : m.technicalSkill)),
                Execution = Sample("execution", c.Activity == ScoutingAppointmentActivity.Demo ? people.Average(m => m.studioEfficiency) : people.Average(m => m.axesVersion > 0 ? m.instrumentalSkill : m.technicalSkill)) };
        }
        c.Status = SceneConversationStatus.Completed; c.Response = message = c.Activity == ScoutingAppointmentActivity.Conversation ? "Met for the agreed hour. Contract availability and terms are checked through the existing discussion." : $"Completed the agreed {c.Activity.ToString().ToLowerInvariant()}; one bounded sample, no master or contract."; return true;
    }
    internal static bool CancelConversation(string id, string labelId, out string message, string reason = null) {
        var c = state?.Conversations.FirstOrDefault(c => c.Id == id && c.LabelId == labelId);
        if (c == null || c.Status != SceneConversationStatus.Scheduled && !(reason != null && c.Status == SceneConversationStatus.InProgress)) {
            message = "Only a scheduled appointment can be cancelled."; return false;
        }
        c.Status = SceneConversationStatus.Cancelled; c.Response = message = reason ?? "Your label cancelled the meeting and released the speaker and conversation slot."; return true;
    }
    private static void ValidateConversations(SceneRoomSaveData saved) {
        var records = saved.Conversations ?? new();
        if (saved.ConversationOrdinal != records.Count || records.Select(c => c?.Id).Distinct().Count() != records.Count ||
            records.Any(c => c == null || string.IsNullOrEmpty(c.Id) || string.IsNullOrEmpty(c.LabelId) || string.IsNullOrEmpty(c.ArtistId) ||
                string.IsNullOrEmpty(c.PersonId) || string.IsNullOrEmpty(c.SourceEventId) || SceneRoomCatalog.Get(c.RoomId) == null ||
                c.Day < 0 || c.AgreedDay < 0 || c.Day <= c.AgreedDay || c.AgreedMinute < 0 || c.AgreedMinute >= 1440 ||
                c.StartMinute < 0 || c.EndMinute > 1440 || c.EndMinute - c.StartMinute != (c.Activity == ScoutingAppointmentActivity.Demo ? PlayerDesk.MinSessionHours * 60 : ConversationMinutes) || !Enum.IsDefined(c.Activity) || !Enum.IsDefined(c.Status)))
            throw new InvalidOperationException("Invalid saved conversation appointments.");
        if (records.Any(c => c.ParticipantIds == null || !float.IsFinite(c.Fee) || c.Fee < 0 || c.Activity == ScoutingAppointmentActivity.Demo && (c.Fee <= 0 || c.Paid && c.Status is SceneConversationStatus.Scheduled or SceneConversationStatus.Declined or SceneConversationStatus.Missed || c.Status == SceneConversationStatus.Completed && !c.Paid) || c.Activity != ScoutingAppointmentActivity.Demo && (c.Fee != 0 || c.Paid) ||
            c.Activity != ScoutingAppointmentActivity.Conversation && (c.ParticipantIds == null || c.ParticipantIds.Count == 0 ||
            c.ParticipantIds.Any(string.IsNullOrEmpty) || c.ParticipantIds.Distinct().Count() != c.ParticipantIds.Count || !c.ParticipantIds.Contains(c.PersonId) ||
            c.Material == null || c.Material.Count != 1) || c.Sample != null && c.Status != SceneConversationStatus.Completed || c.Activity != ScoutingAppointmentActivity.Conversation && c.Status == SceneConversationStatus.Completed && c.Sample == null))
            throw new InvalidOperationException("Invalid saved scouting session terms or participants.");
        if (records.Any(c => (c.Activity == ScoutingAppointmentActivity.Demo ? c.StartMinute < TimeManager.Instance.WorkdayStartHour * 60 || c.EndMinute > TimeManager.Instance.HardStopHour * 60 : c.StartMinute < SceneRoomCatalog.Get(c.RoomId).OpenHour * 60 || c.EndMinute > SceneRoomCatalog.Get(c.RoomId).CloseHour * 60) ||
                Date(c.Day).year < SceneRoomCatalog.Get(c.RoomId).FromYear || Date(c.Day).year > SceneRoomCatalog.Get(c.RoomId).ThroughYear ||
                c.Activity != ScoutingAppointmentActivity.Demo && !SceneLiveEconomics.OpenOn(SceneRoomCatalog.Get(c.RoomId), Date(c.Day).DayOfWeek)) ||
            records.GroupBy(c => (c.LabelId, c.ArtistId, c.SourceEventId)).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Invalid conversation date, room hours or repeated invitation encounter.");
        foreach (var c in records.Where(Pending)) if (records.Any(other => other.Id != c.Id && Pending(other) &&
            (other.LabelId == c.LabelId && other.ArtistId == c.ArtistId || other.Day == c.Day &&
                ((other.PersonId == c.PersonId || other.ParticipantIds.Contains(c.PersonId) || c.ParticipantIds.Contains(other.PersonId) || c.ParticipantIds.Intersect(other.ParticipantIds).Any()) && other.RoomId != c.RoomId || other.StartMinute < c.EndMinute && other.EndMinute > c.StartMinute &&
                    ((other.PersonId == c.PersonId || other.ParticipantIds.Contains(c.PersonId) || c.ParticipantIds.Contains(other.PersonId) || c.ParticipantIds.Intersect(other.ParticipantIds).Any()) || other.LabelId == c.LabelId || AppointmentResource(other.Activity, other.RoomId, other.LabelId) == AppointmentResource(c.Activity, c.RoomId, c.LabelId))))))
            throw new InvalidOperationException("Conflicting saved conversation appointments.");
    }
}
