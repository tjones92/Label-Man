using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingListenerChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_LISTENER_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Listener Checks", "nashville", out string message), "found label: " + message);
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate)
            .First(b => b.Appearances.Count >= 2 && b.Appearances[1].EndHour <= clock.HardStopHour && b.Status == SceneBillStatus.Scheduled);
        var slot = bill.Appearances[0]; string eventId = ScenePerformanceService.EventId(bill, slot);
        var listenerAct = am.GetArtist(bill.Appearances[1].ArtistId);
        var person = listenerAct.members.First(m => m.isActive && m.lifeState == MemberLifeState.Active && !slot.PersonIds.Contains(m.personId));
        listenerAct.labelId = desk.Label.labelId;
        var room = SceneRoomCatalog.Get(bill.RoomId);
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        var rooms = WorldStateService.Capture().SceneRooms;
        var empty = desk.CaptureState(); float cash = desk.Label.cashReserves;
        var listeners = desk.ScoutingListeners(bill.Id, eventId);
        var listener = listeners.FirstOrDefault(p => p.PersonId == person.personId);
        Check(listener != null, "locally present free roster musician is available");
        Check(!listeners.Any(p => slot.PersonIds.Contains(p.PersonId) || (slot.BackingPersonIds?.Contains(p.PersonId) ?? false)), "performers and backers cannot be independent listeners");
        GameRandom.Seed(79); uint expected = GameRandom.Randi(); GameRandom.Seed(79);
        string world = Json(WorldStateService.Capture()); string knowledge = Json(desk.CaptureState().ScoutingKnowledge);
        Check(Json(listeners) == Json(desk.ScoutingListeners(bill.Id, eventId)), "availability quote stable on refresh");
        Check(world == Json(WorldStateService.Capture()) && knowledge == Json(desk.CaptureState().ScoutingKnowledge), "preview creates no world or knowledge changes");
        Check(GameRandom.Randi() == expected, "preview consumes no world RNG");
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, "missing-person", null, out _) && clock.CurrentHour == slot.StartHour && desk.Label.cashReserves == cash, "invalid listener rejected without cost");
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, "stale", out _) && clock.CurrentHour == slot.StartHour, "stale quote rejected without time");
        float oldStage = person.stagePresence; person.stagePresence += .01f;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, listener.QuoteId, out _) && desk.Label.cashReserves == cash, "changed listener expertise invalidates the quote without cost");
        person.stagePresence = oldStage;
        person.isActive = false;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, listener.QuoteId, out _), "departure invalidates availability"); person.isActive = true;
        var priorLife = person.lifeState; person.lifeState = MemberLifeState.Injured;
        Check(!desk.ScoutingListeners(bill.Id, eventId).Any(p => p.PersonId == person.personId), "injured musician unavailable"); person.lifeState = priorLife;
        string labelOwner = listenerAct.labelId; listenerAct.labelId = "other-label";
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, listener.QuoteId, out _), "roster quote cannot silently become a paid external hire"); listenerAct.labelId = labelOwner;
        var conflict = LocalSceneRoomService.Copy(rooms);
        conflict.Bills.Add(new SceneBill { Id = "listener-conflict", RoomId = bill.RoomId, Day = bill.Day,
            StartHour = slot.StartHour, EndHour = slot.EndHour, Status = SceneBillStatus.Scheduled,
            Appearances = new() { new() { ArtistId = listenerAct.artistId, StartHour = slot.StartHour, EndHour = slot.EndHour,
                Status = SceneBillStatus.Scheduled, PersonIds = new() { person.personId } } } });
        LocalSceneRoomService.BeginRestore(conflict); LocalSceneRoomService.CompleteRestore();
        Check(!desk.ScoutingListeners(bill.Id, eventId).Any(p => p.PersonId == person.personId), "overlapping performance blocks attendance");
        var conflictingSlot = conflict.Bills.Last().Appearances.Single(); conflictingSlot.PersonIds.Clear(); conflictingSlot.BackingPersonIds = new() { person.personId };
        LocalSceneRoomService.BeginRestore(conflict); LocalSceneRoomService.CompleteRestore();
        Check(!desk.ScoutingListeners(bill.Id, eventId).Any(p => p.PersonId == person.personId), "house backing also blocks attendance");
        conflict.Bills.Last().RoomId = SceneRoomCatalog.All.First(r => r.Id != bill.RoomId).Id; conflictingSlot.StartHour = slot.EndHour + 1; conflictingSlot.EndHour = slot.EndHour + 2;
        LocalSceneRoomService.BeginRestore(conflict); LocalSceneRoomService.CompleteRestore();
        Check(!desk.ScoutingListeners(bill.Id, eventId).Any(p => p.PersonId == person.personId), "another-room booking blocks same-day teleporting even without time overlap");
        LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore();
        desk.Label.cashReserves = 0;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, listener.QuoteId, out _) && clock.CurrentHour == slot.StartHour, "total admission affordability enforced"); desk.Label.cashReserves = cash;
        Check(desk.ListenSceneAppearance(bill.Id, eventId, out message), "matched solo listening: " + message);
        string soloWorld = Json(WorldStateService.Capture().SceneRooms);
        string soloRead = Json(desk.ScoutingDossiers.Single().Observations.Single().Interpretation);
        LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore(); desk.RestoreState(empty, out _);
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        Check(desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, listener.QuoteId, out message), "shared co-listening: " + message);
        var dossier = desk.ScoutingDossiers.Single(); var heard = dossier.Observations.Single(o => o.Kind == ScoutingEvidenceKind.Performance);
        var opinion = dossier.Observations.Single(o => o.Kind == ScoutingEvidenceKind.ListenerOpinion);
        Check(Json(WorldStateService.Capture().SceneRooms) == soloWorld && Json(heard.Interpretation) == soloRead, "co-listener does not alter shared performance, work, attendance or player's interpretation");
        Check(clock.CurrentHour == slot.EndHour && clock.CurrentMinute == 0, "one set duration, no extra meeting hour");
        Check(Math.Abs(cash - desk.Label.cashReserves - 2 * SceneLiveEconomics.Admission(room, bill.Year)) < .01f, "both visitors' admissions charged once");
        Check(opinion.EventId == heard.EventId && opinion.SourceId == person.personId && opinion.SourceName == person.FullName && opinion.Coverage == heard.Coverage && opinion.PersonIds.SequenceEqual(heard.PersonIds), "observer identity and shared event/context preserved");
        Check(desk.ScoutingSummary(dossier.ArtistId).Any(l => l.Contains("Two judgments of one performance")), "comparison explains source correlation");
        Check(desk.ScoutingSummary(dossier.ArtistId).Any(l => l.StartsWith("Heard 1 distinct set")), "second opinion is not another performance sample");
        Check(desk.ScoutingProvenance(dossier.ArtistId).Any(l => l.Contains(person.FullName) && l.Contains("coverage") && l.Contains("not another date")), "second opinion has acquired coverage and source provenance");
        float afterCash = desk.Label.cashReserves;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, null, out _) && desk.Label.cashReserves == afterCash, "revisiting same performance cannot buy another opinion");
        var saved = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        string acquired = Json(saved.ScoutingKnowledge);
        Check(desk.RestoreState(saved, out message) && Json(desk.CaptureState().ScoutingKnowledge) == acquired, "opinions and listener admission survive JSON reload");
        Check(desk.CaptureState().ScoutingKnowledge.Visit.ListenerIds.Contains(person.personId), "admitted listener retained across restore");
        var nextBill = LocalSceneRoomService.Bill(bill.Id); int now = clock.CurrentHour * 60;
        Check(desk.AdmissionForVisit(nextBill, room, now) == 0, "co-listening preserves continuous player admission");
        Check(!desk.ScoutingListeners(bill.Id, ScenePerformanceService.EventId(nextBill, nextBill.Appearances[1])).Any(p => p.PersonId == person.personId), "listener cannot review their own next performance");
        // A controlled second real appearance tests both visitors' continuous admission.
        var continued = WorldStateService.Capture().SceneRooms;
        var continuedBill = continued.Bills.Single(b => b.Id == bill.Id);
        var second = LocalSceneRoomService.Copy(slot);
        second.StartHour = continuedBill.Appearances[1].StartHour; second.EndHour = continuedBill.Appearances[1].EndHour;
        second.Performance = null; second.Status = SceneBillStatus.Scheduled; second.Role = "later set";
        continuedBill.Appearances[1] = second;
        LocalSceneRoomService.BeginRestore(continued); LocalSceneRoomService.CompleteRestore();
        string secondEvent = ScenePerformanceService.EventId(continuedBill, second);
        var returning = desk.ScoutingListeners(bill.Id, secondEvent).Single(p => p.PersonId == person.personId);
        Check(returning.Admission == 0, "same admitted listener owes no second ticket within continuous visit");
        Check(desk.ListenSceneAppearanceWithListener(bill.Id, secondEvent, person.personId, returning.QuoteId, out _) &&
            desk.Label.cashReserves == afterCash, "both visitors hear actual later set without another admission");
        Check(desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == 2 &&
            desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.ListenerOpinion) == 2,
            "each real appearance has one listening and one source opinion");
        desk.SetScoutingQuestion(dossier.ArtistId, ScoutingQuestion.Ensemble);
        Check(desk.ScoutingSummary(dossier.ArtistId).Any(l => l.Contains("another date is needed")), "two co-listened sets still do not establish multiple-date consistency");
        // Pure acquired comparisons cover disagreement, instrumental absence and partial coverage.
        opinion.Interpretation.Execution = Math.Clamp(heard.Interpretation.Execution + (heard.Interpretation.Execution < .5f ? .5f : -.5f), 0, 1);
        Check(PlayerDesk.ScoutingListenerComparison(dossier).Contains("ensemble, different impressions"), "disagreement shown as unresolved evidence, not a truth correction");
        heard.Interpretation.Vocals = null; opinion.Interpretation.Vocals = null; heard.Coverage = .5f;
        Check(PlayerDesk.ScoutingListenerComparison(dossier).Contains("no lead-vocal evidence") && PlayerDesk.ScoutingListenerComparison(dossier).Contains("only part"), "instrumental and partial evidence remain explicit");
        LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore(); desk.RestoreState(empty, out _);
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour, 30);
        var partial = desk.ScoutingListeners(bill.Id, eventId).First(p => p.PersonId == person.personId);
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, listener.QuoteId, out _), "changed coverage invalidates previous quote");
        Check(desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, partial.QuoteId, out _) && desk.ScoutingDossiers.Single().Observations.All(o => o.Coverage == .5f), "partial co-listening retains identical partial coverage");
        foreach (string flag in new[] { "--observe-scouting-experience", "--disable-scouting-experience" }) {
            LocalScenes.Configure(new[] { flag });
            Check(desk.ScoutingListeners(bill.Id, eventId).Count == 0 && !desk.ListenSceneAppearanceWithListener(bill.Id, eventId, person.personId, null, out _), "off/observer mode blocks co-listening: " + flag);
        }
        LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCOUTING_LISTENER_CHECK_PASS checks={checks}");
    }
}
