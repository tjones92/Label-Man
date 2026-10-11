using System;
using System.Collections.Generic;
using System.Linq;

// Ephemeral availability/price quote; the room remains the performance owner.
public sealed class ScoutingListener {
    public string PersonId { get; init; }
    public string Name { get; init; }
    public string Expertise { get; init; }
    public float Vocals { get; init; }
    public float Execution { get; init; }
    public float Material { get; init; }
    public float Stage { get; init; }
    public float Admission { get; init; }
    public float ConsultationFee { get; init; }
    public string Channel { get; init; } = "accompanying roster musician";
    public string QuoteId { get; init; }
}

public partial class PlayerDesk {
    // A game-design consultation price, scaled by the existing price-level owner; not a historical tariff.
    public const float ScoutingConsultationHourly1960 = 25f;
    public IReadOnlyList<ScoutingListener> ScoutingListeners(string billId, string eventId) => AvailableScoutingListeners(billId, eventId, false);
    public IReadOnlyList<ScoutingListener> HiredScoutingListeners(string billId, string eventId) => AvailableScoutingListeners(billId, eventId, true);
    private IReadOnlyList<ScoutingListener> AvailableScoutingListeners(string billId, string eventId, bool hired) {
        if (Label == null || !LocalScenes.Rooms || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver)
            return Array.Empty<ScoutingListener>();
        var bill = LocalSceneRoomService.Bill(billId); var clock = TimeManager.Instance;
        var slot = bill?.Appearances.FirstOrDefault(a => ScenePerformanceService.EventId(bill, a) == eventId);
        var room = SceneRoomCatalog.Get(bill?.RoomId);
        int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (room?.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId) || slot == null ||
            bill.Day != day || bill.Status != SceneBillStatus.Scheduled || slot.Status != SceneBillStatus.Scheduled ||
            now < slot.StartHour * 60 || now >= slot.EndHour * 60 || HasObservedPerformance(eventId) ||
            !clock.CanAffordMinutes(slot.EndHour * 60 - now)) return Array.Empty<ScoutingListener>();
        var admitted = scoutingKnowledge.Visit is { } visit && visit.BillId == bill.Id && visit.Day == day && visit.EndMinute == now
            ? visit.ListenerIds ?? new() : new List<string>();
        var listeners = new List<ScoutingListener>();
        void Offer(Musician person, string affiliation) {
            if (string.IsNullOrEmpty(person.personId) || person.lifeState != MemberLifeState.Active ||
                slot.PersonIds.Contains(person.personId) || (slot.BackingPersonIds?.Contains(person.personId) ?? false) ||
                LocalSceneRoomService.HasPersonBooking(person.personId, day, now, slot.EndHour * 60, room.Id)) return;
            float voice = person.isLeadVocalist ? person.axesVersion > 0 ? (person.vocalPower + person.vocalControl) / 2 : person.technicalSkill : .25f;
            float execution = person.axesVersion > 0 ? person.instrumentalSkill : person.technicalSkill;
            float material = person.isPrimaryWriter ? person.creativity : .25f;
            float admission = admitted.Contains(person.personId) ? 0 : SceneLiveEconomics.Admission(room, bill.Year);
            float fee = hired ? ScoutingConsultationHourly1960 * SceneLiveEconomics.PriceLevel(bill.Year) * (slot.EndHour * 60 - now) / 60f : 0;
            string channel = hired ? "hired local musician" : "accompanying roster musician";
            string expertise = RoleWord(person.primaryRole) + (person.isLeadVocalist ? "; vocalist" : "; general vocal impression") +
                (person.isPrimaryWriter ? "; writer" : "; general material impression");
            float total = AdmissionForVisit(bill, room, now) + admission + fee;
            listeners.Add(new() { PersonId = person.personId, Name = person.FullName, Expertise = expertise,
                Vocals = voice, Execution = execution, Material = material, Stage = person.stagePresence,
                Admission = admission, ConsultationFee = fee, Channel = channel,
                QuoteId = $"listener-v2|{bill.Id}|{eventId}|{day}|{now}|{slot.EndHour}|{person.personId}|{affiliation}|{channel}|{expertise}|{total:R}|{voice:R}|{execution:R}|{material:R}|{person.stagePresence:R}" });
        }
        if (hired) {
            var affiliations = LocalSceneIdentityService.ArtistsBasedAt(room.PlaceId).Select(id => ArtistManager.Instance.GetArtist(id))
                .Where(a => a != null).SelectMany(a => a.members.Select(m => (m.personId, act: a)))
                .Where(x => !string.IsNullOrEmpty(x.personId)).GroupBy(x => x.personId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().act, StringComparer.Ordinal);
            foreach (var person in SessionEmploymentService.ListeningCandidates(room.PlaceId, day)) {
                var act = affiliations.GetValueOrDefault(person.personId);
                if (act?.labelId == Label.labelId || act?.artistId == slot.ArtistId) continue;
                Offer(person, act == null ? "pool" : act.artistId + "|" + act.labelId);
            }
        } else {
            foreach (var act in ArtistManager.Instance.GetAllArtists().Where(a => a.labelId == Label.labelId &&
                a.artistId != slot.ArtistId && a.lifecycleStatus == ArtistLifecycleStatus.Active &&
                LocalScenePersistenceService.Present(a, room.PlaceId, day / 7) && SceneEcosystemService.AvailableAt(a.artistId, room.PlaceId, day)))
                foreach (var person in act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active))
                    Offer(person, act.artistId + "|" + act.labelId);
        }
        return listeners.GroupBy(p => p.PersonId, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(p => p.Name, StringComparer.Ordinal).ThenBy(p => p.PersonId, StringComparer.Ordinal).ToArray();
    }

    public bool ListenSceneAppearanceWithListener(string billId, string eventId, string personId, string expectedQuoteId, out string message) {
        var listener = ScoutingListeners(billId, eventId).Concat(HiredScoutingListeners(billId, eventId)).FirstOrDefault(p => p.PersonId == personId);
        if (listener == null) { message = "That musician cannot accompany this set now. They must be locally present, active and free of performance bookings; performers cannot be independent listeners to their own set."; return false; }
        if (expectedQuoteId != null && expectedQuoteId != listener.QuoteId) { message = "The listening time, price or listener changed. Review the choice before attending."; return false; }
        var bill = LocalSceneRoomService.Bill(billId);
        return ListenSceneRoom(bill.RoomId, eventId, out message, listener);
    }

    private void RememberScoutingListener(ScoutingDossier dossier, ScoutingObservation heard, ScenePerformanceSnapshot performance, ScoutingListener listener) {
        string source = "musician|" + listener.PersonId;
        var voices = ScenePerformanceService.Interpret(performance, source, listener.Vocals, heard.Coverage);
        var ensemble = ScenePerformanceService.Interpret(performance, source, listener.Execution, heard.Coverage);
        var material = ScenePerformanceService.Interpret(performance, source, listener.Material, heard.Coverage);
        var stage = ScenePerformanceService.Interpret(performance, source, listener.Stage, heard.Coverage);
        var opinion = LocalSceneRoomService.Copy(heard);
        opinion.Id = $"listener-opinion-v1|{Label.labelId}|{heard.EventId}|{listener.PersonId}";
        opinion.Kind = ScoutingEvidenceKind.ListenerOpinion; opinion.SourceId = listener.PersonId; opinion.SourceName = listener.Name;
        opinion.Channel = listener.Channel; opinion.ConsultationFee = listener.ConsultationFee;
        opinion.Credibility = "independent observer of the same performance; not another date or recording evidence";
        opinion.Claim = listener.Expertise;
        opinion.Interpretation = new() { Id = performance.Id, Vocals = voices.Vocals, Execution = ensemble.Execution,
            Material = material.Material, Communication = stage.Communication, OriginalMaterial = performance.OriginalMaterial };
        dossier.Observations.Add(opinion);
    }

    public static string ScoutingListenerComparison(ScoutingDossier dossier) {
        var heard = dossier.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        var opinion = dossier.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.ListenerOpinion && o.EventId == heard?.EventId);
        if (heard?.Interpretation == null || opinion?.Interpretation == null) return null;
        string Compare(float first, float second) => Math.Abs(first - second) > .15f ? "different impressions" : "broadly similar impressions";
        string vocal = heard.Interpretation.Vocals.HasValue && opinion.Interpretation.Vocals.HasValue
            ? Compare(heard.Interpretation.Vocals.Value, opinion.Interpretation.Vocals.Value) : "no lead-vocal evidence";
        return $"Second listener — {opinion.SourceName} ({opinion.Claim}): vocals, {vocal}; ensemble, {Compare(heard.Interpretation.Execution, opinion.Interpretation.Execution)}; " +
            $"material, {Compare(heard.Interpretation.Material, opinion.Interpretation.Material)}; stage, {Compare(heard.Interpretation.Communication, opinion.Interpretation.Communication)}. " +
            (heard.Coverage < .9f ? "Both heard only part of the set. " : "") +
            "Two judgments of one performance, not two nights. Disagreement leaves a question to test; agreement does not establish recording readiness or future potential.";
    }
}
