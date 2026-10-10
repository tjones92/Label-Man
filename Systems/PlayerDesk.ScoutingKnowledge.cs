using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerDesk {
    private ScoutingKnowledgeSaveData scoutingKnowledge = new();
    private readonly Dictionary<string, Prospect> scoutingNegotiations = new(StringComparer.Ordinal);
    public IReadOnlyList<ScoutingDossier> ScoutingDossiers => LocalSceneRoomService.Copy(scoutingKnowledge.Dossiers).AsReadOnly();
    public FoundingActMemory FoundingAct => LocalSceneRoomService.Copy(scoutingKnowledge.FoundingAct);
    private ScoutingKnowledgeSaveData CaptureScoutingKnowledge() {
        var saved = LocalSceneRoomService.Copy(scoutingKnowledge);
        saved.Negotiations = scoutingNegotiations.Values.Select(p => new ScoutingNegotiationState {
            ArtistId = p.Artist.artistId, Baseline = p.Baseline, HasBaseline = p.HasBaseline, Draft = p.Draft, Posture = p.Posture,
            CooldownUntil = p.CooldownUntil, Countered = p.PushoverCountered, Talk = DetachedTalk(p.Talk), Log = p.Talk?.log.ToList() ?? new() }).ToList();
        return saved;
    }
    private static ContractTalk DetachedTalk(ContractTalk t) {
        if (t == null) return null;
        var copy = new ContractTalk { ask = t.ask, posture = t.posture, weights = t.weights == null ? null : new(t.weights),
            reservation = t.reservation, patienceMax = t.patienceMax, patienceLeft = t.patienceLeft, roundsPlayed = t.roundsPlayed,
            stage = t.stage, objectionAxis = t.objectionAxis, lastOfferValue = t.lastOfferValue, lastOffer = t.lastOffer, draftOffer = t.draftOffer };
        copy.log.AddRange(t.log); return copy;
    }
    private void RememberScoutingNegotiation(Prospect prospect) {
        if (HasScoutingEvidence(prospect.Artist.artistId)) scoutingNegotiations[prospect.Artist.artistId] = prospect;
    }
    private void RestoreScoutingKnowledge(ScoutingKnowledgeSaveData saved) {
        scoutingKnowledge = LocalSceneRoomService.Copy(saved) ?? new();
        scoutingKnowledge.Dossiers ??= new();
        scoutingNegotiations.Clear();
        foreach (var state in scoutingKnowledge.Negotiations ?? new()) {
            var artist = ArtistManager.Instance?.GetArtist(state.ArtistId);
            if (artist == null) continue;
            var p = new Prospect { Artist = artist, Baseline = state.Baseline, HasBaseline = state.HasBaseline,
                Draft = state.Draft, Posture = state.Posture, CooldownUntil = state.CooldownUntil, PushoverCountered = state.Countered };
            p.Talk = DetachedTalk(state.Talk); if (p.Talk != null) { p.Talk.prospect = p; p.Talk.log.Clear(); p.Talk.log.AddRange(state.Log ?? new()); }
            scoutingNegotiations[state.ArtistId] = p;
        }
        scoutingKnowledge.Negotiations.Clear();
        foreach (var entry in notebook) {
            if (scoutingKnowledge.Dossiers.Any(d => d.ArtistId == entry.Artist.artistId)) continue;
            var dossier = Dossier(entry.Artist.artistId, entry.Artist.stageName);
            dossier.Priority = ScoutingPriority.Watch;
            dossier.Observations.Add(new() { Id = $"legacy|{Label.labelId}|{entry.Artist.artistId}", LabelId = Label.labelId,
                ArtistId = entry.Artist.artistId, ArtistName = entry.Artist.stageName, Kind = ScoutingEvidenceKind.LegacyImpression,
                AcquiredDay = LocalSceneRoomService.Day(entry.LastSeen), Channel = "old notebook", Credibility = "provenance unknown",
                Claim = "An earlier notebook impression survives. Its source and listening coverage were not recorded; it is not an independent performance sample." });
        }
    }
    private ScoutingDossier Dossier(string artistId, string name) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier != null) return dossier;
        dossier = new() { ArtistId = artistId, ArtistName = name }; scoutingKnowledge.Dossiers.Add(dossier); return dossier;
    }
    public bool HasScoutingEvidence(string artistId) => scoutingKnowledge.Dossiers.Any(d => d.ArtistId == artistId &&
        d.Observations.Any(o => o.Kind == ScoutingEvidenceKind.Performance));
    public bool SetScoutingPriority(string artistId, ScoutingPriority priority) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier == null || !Enum.IsDefined(priority)) return false;
        dossier.Priority = priority;
        if (priority == ScoutingPriority.Archived) notebook.RemoveAll(n => n.Artist?.artistId == artistId);
        Changed?.Invoke(); return true;
    }
    public bool OpenScoutingDossier(string artistId, out string message) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        var artist = ArtistManager.Instance?.GetArtist(artistId);
        if (dossier == null || artist == null) { message = "The historical notes remain, but this act is no longer available on the pad."; return false; }
        var last = dossier.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        if (last == null) { message = "Only a legacy impression is recorded. Hear an announced set for fresh evidence."; return false; }
        var room = SceneRoomCatalog.Get(last.RoomId);
        var existing = scoutingNegotiations.GetValueOrDefault(artistId) ?? slate.FirstOrDefault(p => p.Artist?.artistId == artistId);
        // Keep the actual negotiation alive when reopening, never redraw its asks or cooldown.
        var prospect = existing ?? new Prospect { Artist = artist, Venue = room?.Category ?? ScoutingVenue.ClubsAndRoadhouses,
            CityId = room?.PlaceId, SceneRoomId = last.RoomId, SceneBillId = last.BillId,
            AskingAdvance = VenueAdvanceAsk(artist, room?.Category ?? ScoutingVenue.ClubsAndRoadhouses, AskScaleFor(room?.PlaceId)),
            Note = "Notebook evidence; reopening is not another listening." };
        prospect.SceneRoomId = last.RoomId; prospect.SceneBillId = last.BillId;
        prospect.CityId = room?.PlaceId; prospect.Venue = room?.Category ?? prospect.Venue;
        prospect.AskingAdvance = prospect.HasBaseline ? prospect.Baseline.Advance : VenueAdvanceAsk(artist, prospect.Venue, AskScaleFor(prospect.CityId));
        if (prospect.LiveSet.Count == 0 && last.Coverage >= .9) {
            prospect.LiveSet.AddRange(last.Set.Select(s => new RepertoireItem { Title = s.Title, SongId = s.SongId, Genre = s.Genre,
                IsOriginal = s.Original, ContentContext = s.ContentContext, SourceTag = s.Original ? "their own" : "standard" }));
            prospect.HeardCount = prospect.LiveSet.Count;
        }
        if (!slate.Contains(prospect)) slate.Add(prospect);
        SlateDate = LocalSceneRoomService.Date(last.OccurredDay ?? last.AcquiredDay);
        message = $"Opened {dossier.ArtistName}'s recorded evidence. No time or money spent.";
        Changed?.Invoke(); return true;
    }
    private static string AxisDescription(float value) => value >= .72f ? "assured in this set" : value >= .48f ? "held together, with questions" : "uneven in this set";
    public IReadOnlyList<string> ScoutingSummary(string artistId) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier == null) return new[] { "An earlier impression; no performance provenance recorded. Further listening can answer specific questions." };
        var samples = dossier.Observations.Where(o => o.Kind == ScoutingEvidenceKind.Performance).ToArray();
        var lines = new List<string>();
        if (samples.Length == 0) lines.Add("Legacy impressions only; source and coverage unknown.");
        else {
            var last = samples.Last(); var read = last.Interpretation;
            string place = SceneRoomCatalog.Get(last.RoomId)?.Name ?? last.RoomId;
            int nights = samples.Select(o => o.OccurredDay).Distinct().Count();
            lines.Add($"Heard {samples.Length} distinct {(samples.Length == 1 ? "set" : "sets")} on {nights} {(nights == 1 ? "date" : "dates")}. Last: {place}, {LocalSceneRoomService.Date(last.OccurredDay.Value).ToHeadlineString()}; {last.Coverage:P0} of the scheduled set.");
            if (read != null) {
                lines.Add(read.Vocals.HasValue ? "Vocals: " + AxisDescription(read.Vocals.Value) + "." : "Vocals: no lead-vocal evidence in this set.");
                lines.Add("Ensemble / execution: " + AxisDescription(read.Execution) + ".");
                lines.Add("Material: " + AxisDescription(read.Material) + (read.OriginalMaterial ? (last.Coverage >= .9 ? "; heard an unrecorded original. More material could test this impression." : "; an original was scheduled, but partial coverage leaves what you heard uncertain.") : "; familiar material, no original writing demonstrated.") );
                lines.Add("Stage communication: " + AxisDescription(read.Communication) + ".");
            }
            lines.Add("Recording readiness: no recording evidence from a live set.");
            if (last.BillAttendance.HasValue && last.BillCapacity > 0) {
                float fullness = (float)last.BillAttendance.Value / last.BillCapacity;
                string count = fullness >= .85 ? "nearly full" : fullness >= .6 ? "roughly three-quarters full" : fullness >= .35 ? "about half full" : "sparsely occupied";
                lines.Add($"Audience: the whole bill was {count}. Attendance is not an act's portable following.");
            } else lines.Add("Audience: no completed bill count acquired during this listening; individual draw is unknown.");
            var current = ArtistManager.Instance?.GetArtist(artistId);
            var people = current?.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).OrderBy(id => id, StringComparer.Ordinal);
            if (people == null || !people.SequenceEqual(last.PersonIds.OrderBy(id => id, StringComparer.Ordinal)))
                lines.Add("Lineup changed or the act departed: affected assessments need new evidence. These notes describe the earlier people.");
            if (nights < 2) lines.Add("Consistency: another date could test this impression; two reports from this night are one sample.");
            else {
                var contexts = samples.Select(o => string.Join("|", o.PersonIds.OrderBy(id => id)) + ":" + string.Join("|", o.Set.Select(s => s.SongId ?? s.Title))).Distinct().Count();
                float spread = samples.Max(o => o.Interpretation.Execution) - samples.Min(o => o.Interpretation.Execution);
                lines.Add($"Consistency: {(spread > .2 ? "execution impressions differ" : "execution impressions are broadly similar")} across dates; {(contexts > 1 ? "lineup or material differed" : "similar recorded lineup and material")}. The cause is unresolved.");
            }
        }
        foreach (var claim in dossier.Observations.Where(o => o.Kind == ScoutingEvidenceKind.BookerClaim).TakeLast(2))
            lines.Add($"{claim.SourceName}, received {LocalSceneRoomService.Date(claim.AcquiredDay).ToHeadlineString()} (local source claim): {claim.Claim}");
        lines.Add(NextScoutingOpportunity(artistId));
        return lines;
    }
    public string NextScoutingOpportunity(string artistId) {
        if (!LocalScenes.Rooms) return "The room calendar is disabled; recorded evidence is retained.";
        var date = TimeManager.Instance.CurrentDate; int now = TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute;
        var next = LocalSceneRoomService.Calendar(CurrentCityId, date).SelectMany(b => b.Appearances.Select(a => (b, a)))
            .Where(x => x.a.ArtistId == artistId && x.a.Status == SceneBillStatus.Scheduled && (x.b.Day > LocalSceneRoomService.Day(date) || x.a.EndHour * 60 > now))
            .OrderBy(x => x.b.Day).ThenBy(x => x.a.StartHour).FirstOrDefault();
        if (next.b == null) return "Next: no further local set announced. Check other room bills or ask a booker for a listening tip. No known rival report means only that nothing has reached you.";
        return $"Next: {SceneRoomCatalog.Get(next.b.RoomId)?.Name}, {LocalSceneRoomService.Date(next.b.Day).ToHeadlineString()}, {next.a.StartHour}:00. Check availability before leaving.";
    }
    public IReadOnlyList<string> ScoutingProvenance(string artistId) => scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations
        .Select(o => $"{o.Kind} · {o.SourceName ?? o.Channel} · occurred {(o.OccurredDay.HasValue ? LocalSceneRoomService.Date(o.OccurredDay.Value).ToHeadlineString() : "unknown")}, acquired {LocalSceneRoomService.Date(o.AcquiredDay).ToHeadlineString()} · {o.Credibility} · {o.Conditions ?? o.Claim} · event {o.EventId ?? "unknown"}").ToArray() ?? Array.Empty<string>();
    public bool AskScoutingBooker(string roomId, string artistId, bool bookingHistory, out string message) {
        if (!LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver) { message = "New scouting evidence is disabled; recorded notes are retained."; return false; }
        var room = SceneRoomCatalog.Get(roomId); var artist = ArtistManager.Instance.GetArtist(artistId);
        if (!LocalScenes.Rooms || room == null || artist == null || room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) {
            message = "Visit the room's city to ask its booker."; return false;
        }
        var clock = TimeManager.Instance;
        if (clock.CurrentHour < room.OpenHour || clock.CurrentHour >= room.CloseHour) { message = "The booker is available during posted room hours."; return false; }
        if (!Require(1, out message)) return false;
        int shows = LocalSceneRoomService.ShowsAt(roomId, artistId);
        var next = LocalSceneRoomService.Calendar(CurrentCityId, clock.CurrentDate).FirstOrDefault(b => b.RoomId == roomId && b.Status == SceneBillStatus.Scheduled
            && b.Appearances.Any(a => a.ArtistId == artistId && a.Status == SceneBillStatus.Scheduled));
        string eventId = bookingHistory ? $"booker-history|{roomId}|{artistId}|{shows}" : $"booker-date|{roomId}|{artistId}|{next?.Id ?? "none"}";
        var dossier = Dossier(artistId, artist.stageName);
        if (dossier.Observations.Any(o => o.EventId == eventId)) { message = "You already recorded this answer. There is no new information to buy."; return false; }
        int day = LocalSceneRoomService.Day(clock.CurrentDate);
        string claim = bookingHistory ? $"Our retained work record has {shows} completed appearance(s) here. That says nothing about punctuality or audiences in another room."
            : next == null ? "No further booking for this act is announced here." : $"They are announced here on {LocalSceneRoomService.Date(next.Day).ToHeadlineString()}. The calendar can still change.";
        Spend(1);
        scoutingKnowledge.Visit = null; // A substantive conversation competes with careful listening.
        dossier.Observations.Add(new() { Id = $"claim-v1|{Label.labelId}|{eventId}", EventId = eventId, LabelId = Label.labelId,
            ArtistId = artistId, ArtistName = artist.stageName, Kind = ScoutingEvidenceKind.BookerClaim, RoomId = roomId,
            SourceId = room.ContactId, SourceName = room.ContactName, Channel = "booker conversation", OccurredDay = bookingHistory ? null : next?.Day,
            AcquiredDay = day, Credibility = "local booking record; limited to this room", Claim = claim });
        message = room.ContactName + ": " + claim; Note(message); Changed?.Invoke(); return true;
    }
    private void RememberFoundingAct(Prospect prospect, ContractTermSheet terms) {
        if (scoutingKnowledge.FoundingAct != null || Label.CurrentRosterSize > 1) return;
        var last = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == prospect.Artist.artistId)?.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        scoutingKnowledge.FoundingAct = new() { ArtistId = prospect.Artist.artistId, ArtistName = prospect.Artist.stageName,
            LabelId = Label.labelId, Day = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate), Terms = terms,
            Context = last == null ? "Earlier scouting impression; listening provenance unknown." : $"Heard at {SceneRoomCatalog.Get(last.RoomId)?.Name ?? last.RoomId}; {last.EventId}." };
    }
}
