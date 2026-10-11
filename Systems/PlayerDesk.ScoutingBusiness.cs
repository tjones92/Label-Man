using System;
using System.Linq;

public sealed class ScoutingBusinessMeeting {
    public string QuoteId { get; init; }
    public string EventId { get; init; }
    public string RoomId { get; init; }
    public string SourcePersonId { get; init; }
    public string SourceName { get; init; }
    public string Claim { get; init; }
    public bool Available { get; init; }
    public string UnavailableReason { get; init; }
}

public partial class PlayerDesk {
    // A real post-set conversation. The preview's claim is never displayed before the action.
    public ScoutingBusinessMeeting PreviewScoutingBusinessMeeting(string artistId) {
        if (Label == null || !LocalScenes.Rooms || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver) return null;
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        var heard = dossier?.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        var act = ArtistManager.Instance.GetArtist(artistId); var room = SceneRoomCatalog.Get(heard?.RoomId);
        var clock = TimeManager.Instance;
        int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (heard == null || act == null || act.lifecycleStatus != ArtistLifecycleStatus.Active ||
            heard.OccurredDay != day || heard.EndMinute > now || room == null ||
            room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId) ||
            !LocalScenePersistenceService.Present(act, room.PlaceId, day / 7) || !SceneEcosystemService.AvailableAt(artistId, room.PlaceId, day)) return null;
        var people = act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active && heard.PersonIds.Contains(m.personId))
            .OrderByDescending(m => m.isBandLeader).ThenByDescending(m => m.isLeadVocalist).ThenBy(m => m.personId, StringComparer.Ordinal).ToArray();
        var speaker = people.FirstOrDefault(m => !LocalSceneRoomService.HasPersonBooking(m.personId, day, now, now + 60, room.Id));
        if (speaker == null) return null;
        var history = (act.sceneMarketHistory ?? new()).Where(h => h.ArtistId == artistId && h.Day <= day)
            .OrderBy(h => h.Day).ThenBy(h => h.Id, StringComparer.Ordinal).TakeLast(8).ToArray();
        string eventId = "act-market-account-v1|" + artistId + "|" + string.Join(";", history.Select(h => h.Id));
        string Claim(SceneMarketDecision h) {
            string when = LocalSceneRoomService.Date(h.Day).ToHeadlineString();
            return h.Kind switch {
                SceneMarketDecisionKind.Signed => $"On {when}, our deal with {h.LabelName ?? "a label"} completed.",
                SceneMarketDecisionKind.AlternativeNotSelected => $"On {when}, {h.LabelName ?? "a label"}'s proposal was not selected in that decision. " +
                    (h.SelectedDealCompleted ? $"The selected deal with {h.SelectedLabelName ?? "a label"} completed." : "The selected signing did not complete; this does not establish a contract."),
                _ => $"On {when}, a signing attempt with {h.LabelName ?? "a label"} did not complete. No reason was recorded in this account."
            };
        }
        string claim = history.Length == 0 ? "We have no record of completed label decisions to report. This does not rule out other approaches or discussions." : string.Join(" ", history.Select(Claim));
        claim += " This is our account of past decisions, not a current offer, a deadline, or proof of a visit or introduction. No reason for those decisions was recorded.";
        string blocked = null;
        if (dossier.Observations.Any(o => o.Kind == ScoutingEvidenceKind.ActBusinessClaim && o.EventId == eventId))
            blocked = "You already recorded this account. A different speaker or another click is not a new market decision.";
        else if (now < room.OpenHour * 60 || now + 60 > room.CloseHour * 60)
            blocked = "Allow a full hour after the set within this room's posted hours.";
        else if (!clock.CanAffordMinutes(60)) blocked = "There is not an hour left for this conversation today.";
        return new() { EventId = eventId, RoomId = room.Id, SourcePersonId = speaker.personId, SourceName = speaker.FullName,
            Claim = claim, Available = blocked == null, UnavailableReason = blocked,
            QuoteId = $"{eventId}|{speaker.personId}|{speaker.FullName}|{act.labelId}|{day}|{now}|{claim}" };
    }

    public bool AskScoutingBusinessHistory(string artistId, string expectedQuoteId, out string message) {
        var meeting = PreviewScoutingBusinessMeeting(artistId);
        if (meeting == null) { message = "Speak with a free member at the room after a set you heard today. Their current presence and performance bookings still apply."; return false; }
        if (expectedQuoteId != null && meeting.QuoteId != expectedQuoteId) { message = "The speaker, time or account changed. Review the conversation again."; return false; }
        if (!meeting.Available) { message = meeting.UnavailableReason; return false; }
        if (!Require(1, out message)) return false;
        // Capture the statement before advancing the world. Later signings never rewrite what was said.
        int occurred = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        Spend(1); scoutingKnowledge.Visit = null;
        var clock = TimeManager.Instance; var dossier = scoutingKnowledge.Dossiers.Single(d => d.ArtistId == artistId);
        dossier.Observations.Add(new() { Id = $"act-account-v1|{Label.labelId}|{meeting.EventId}", EventId = meeting.EventId,
            LabelId = Label.labelId, ArtistId = artistId, ArtistName = dossier.ArtistName, Kind = ScoutingEvidenceKind.ActBusinessClaim,
            RoomId = meeting.RoomId, RoomName = SceneRoomCatalog.Get(meeting.RoomId).Name, SourceId = meeting.SourcePersonId,
            SourceName = meeting.SourceName, Channel = "post-set act conversation", OccurredDay = occurred,
            AcquiredDay = LocalSceneRoomService.Day(clock.CurrentDate), AcquiredMinute = clock.CurrentHour * 60 + clock.CurrentMinute,
            Credibility = "member's account of past label decisions; historical dates are stated in the claim; no current offer or deadline implied",
            Claim = meeting.Claim });
        message = meeting.SourceName + ": " + meeting.Claim; Note(message); Changed?.Invoke(); return true;
    }
}
