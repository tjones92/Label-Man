using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingHiredListenerChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_HIRED_LISTENER_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Hired Listener Checks", "nashville", out string message), "found label: " + message);
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate)
            .First(b => b.Appearances.Count >= 2 && b.Appearances[1].EndHour <= clock.HardStopHour && b.Status == SceneBillStatus.Scheduled);
        var slot = bill.Appearances[0]; var room = SceneRoomCatalog.Get(bill.RoomId);
        string eventId = ScenePerformanceService.EventId(bill, slot);
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        var listeners = desk.HiredScoutingListeners(bill.Id, eventId);
        Check(desk.ScoutingListeners(bill.Id, eventId).Count == 0 && listeners.Count > 0, "founding label can hire an existing musician without a roster");
        var listener = listeners.First(); var person = am.GetMusician(listener.PersonId) ?? PersonPool.Get(listener.PersonId)?.person;
        Check(person != null && person.technicalSkill >= SessionEmploymentService.SkillBar && person.sightReading >= SessionEmploymentService.ReadingBar,
            "canonical session-hire qualifications, no invented expert");
        Check(!listeners.Any(l => slot.PersonIds.Contains(l.PersonId) || (slot.BackingPersonIds?.Contains(l.PersonId) ?? false)), "performers/backers excluded");
        Check(listener.ConsultationFee > 0 && listener.Channel == "hired local musician", "paid source distinct from roster companion");
        float expectedFee = PlayerDesk.ScoutingConsultationHourly1960 * SceneLiveEconomics.PriceLevel(bill.Year) * (slot.EndHour - slot.StartHour);
        Check(Math.Abs(listener.ConsultationFee - expectedFee) < .001f, "explicit time-scaled consultation quote");
        string world = Json(WorldStateService.Capture()), knowledge = Json(desk.CaptureState().ScoutingKnowledge);
        GD.Seed(179); uint expectedRng = GD.Randi(); GD.Seed(179);
        Check(Json(listeners) == Json(desk.HiredScoutingListeners(bill.Id, eventId)), "stable quotes on refresh");
        Check(world == Json(WorldStateService.Capture()) && knowledge == Json(desk.CaptureState().ScoutingKnowledge), "preview changes no world, money or knowledge");
        Check(GD.Randi() == expectedRng, "preview consumes no RNG");
        float cash = desk.Label.cashReserves, expenses = desk.Label.monthlyExpenses;
        void NoCost() => Check(clock.CurrentHour == slot.StartHour && clock.CurrentMinute == 0 && desk.Label.cashReserves == cash && desk.Label.monthlyExpenses == expenses, "blocked action costs nothing");
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, "stale", out _), "stale quote rejected"); NoCost();
        var life = person.lifeState; person.lifeState = MemberLifeState.Injured;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, listener.QuoteId, out _), "injury rejected"); NoCost(); person.lifeState = life;
        float reading = person.sightReading; person.sightReading = 0;
        Check(!desk.HiredScoutingListeners(bill.Id, eventId).Any(l => l.PersonId == person.personId), "lost session qualification rejected"); person.sightReading = reading;
        bool vocalist = person.isLeadVocalist; person.isLeadVocalist = !vocalist;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, listener.QuoteId, out _), "changed expertise invalidates quote"); NoCost(); person.isLeadVocalist = vocalist;
        var act = am.GetAllArtists().FirstOrDefault(a => a.members.Any(m => m.personId == person.personId));
        if (act != null) {
            string owner = act.labelId; act.labelId = desk.Label.labelId;
            Check(!desk.HiredScoutingListeners(bill.Id, eventId).Any(l => l.PersonId == person.personId) &&
                !desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, listener.QuoteId, out _), "joining roster invalidates paid quote"); NoCost(); act.labelId = owner;
            int index = act.members.IndexOf(person);
            act.members.RemoveAt(index); PersonPool.Add(new() { person = person, sinceYear = bill.Year });
            Check(desk.HiredScoutingListeners(bill.Id, eventId).Any(l => l.PersonId == person.personId), "canonical local pooled player can consult without joining a roster");
            PersonPool.Take(person.personId);
            Check(!desk.HiredScoutingListeners(bill.Id, eventId).Any(l => l.PersonId == person.personId), "person without act or pool availability cannot consult");
            act.members.Insert(index, person);
        }
        var initialWorld = WorldStateService.Capture(); var rooms = initialWorld.SceneRooms;
        var conflict = LocalSceneRoomService.Copy(rooms);
        conflict.Bills.Add(new SceneBill { Id = "hired-listener-conflict", RoomId = room.Id, Day = bill.Day,
            StartHour = slot.StartHour, EndHour = slot.EndHour, Status = SceneBillStatus.Scheduled,
            Appearances = new() { new() { ArtistId = slot.ArtistId, StartHour = slot.StartHour, EndHour = slot.EndHour,
                Status = SceneBillStatus.Scheduled, BackingPersonIds = new() { person.personId } } } });
        LocalSceneRoomService.BeginRestore(conflict); LocalSceneRoomService.CompleteRestore();
        Check(!desk.HiredScoutingListeners(bill.Id, eventId).Any(l => l.PersonId == person.personId), "backing reservation blocks consultation");
        conflict.Bills.Last().RoomId = SceneRoomCatalog.All.First(r => r.Id != room.Id).Id;
        conflict.Bills.Last().Appearances.Single().StartHour = slot.EndHour + 1;
        conflict.Bills.Last().Appearances.Single().EndHour = slot.EndHour + 2;
        LocalSceneRoomService.BeginRestore(conflict); LocalSceneRoomService.CompleteRestore();
        Check(!desk.HiredScoutingListeners(bill.Id, eventId).Any(l => l.PersonId == person.personId), "other-room booking blocks same-day travel");
        LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore();
        desk.Label.cashReserves = 2 * listener.Admission;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, listener.QuoteId, out _) && clock.CurrentHour == slot.StartHour,
            "admission alone cannot pay consultation"); desk.Label.cashReserves = cash;
        var empty = desk.CaptureState();
        Check(desk.ListenSceneAppearance(bill.Id, eventId, out message), "matched solo action: " + message);
        string soloRooms = Json(WorldStateService.Capture().SceneRooms), soloRead = Json(desk.ScoutingDossiers.Single().Observations.Single().Interpretation);
        string soloContacts = Json(WorldStateService.Capture().ContactNetwork);
        LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore(); desk.RestoreState(empty, out _);
        ContactNetworkService.RehydrateWorld(initialWorld);
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        float wealth = person.wealth;
        Check(desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, listener.QuoteId, out message), "hired listening: " + message);
        var dossier = desk.ScoutingDossiers.Single(); var opinion = dossier.Observations.Single(o => o.Kind == ScoutingEvidenceKind.ListenerOpinion);
        Check(clock.CurrentHour == slot.EndHour && clock.CurrentMinute == 0, "one shared listening interval");
        Check(Math.Abs(cash - desk.Label.cashReserves - (2 * listener.Admission + listener.ConsultationFee)) < .01f &&
            Math.Abs(desk.Label.monthlyExpenses - expenses - listener.ConsultationFee) < .01f, "admission plus consultation charged exactly once through label fee accounting");
        Check(Json(WorldStateService.Capture().SceneRooms) == soloRooms && Json(dossier.Observations.First().Interpretation) == soloRead,
            "shared live result and original player interpretation unchanged");
        Check(soloContacts == Json(WorldStateService.Capture().ContactNetwork) && person.wealth == wealth, "same ordinary contact/session work as solo, no wealth credit from consultation");
        Check(opinion.SourceId == person.personId && opinion.SourceName == person.FullName && opinion.Channel == "hired local musician" &&
            opinion.ConsultationFee == listener.ConsultationFee && opinion.EventId == eventId, "source, cost and shared performance preserved");
        Check(desk.ScoutingProvenance(dossier.ArtistId).Any(l => l.Contains("consultation paid") && l.Contains("hired local musician")), "paid provenance visible");
        Check(desk.ScoutingSummary(dossier.ArtistId).Any(l => l.StartsWith("Heard 1 distinct set")) &&
            desk.ScoutingSummary(dossier.ArtistId).Any(l => l.Contains("Two judgments of one performance")), "opinion is not a second night");
        float paidCash = desk.Label.cashReserves;
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, null, out _) && desk.Label.cashReserves == paidCash,
            "same event cannot be rebought");
        var saved = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        string savedKnowledge = Json(saved.ScoutingKnowledge);
        Check(desk.RestoreState(saved, out _) && Json(desk.CaptureState().ScoutingKnowledge) == savedKnowledge && desk.Label.cashReserves == paidCash,
            "paid evidence, cash and listener admission survive JSON restore");
        if (OS.GetCmdlineUserArgs().Contains("--scouting-hired-listener-save-check")) {
            string saveSlot = "Hired probe " + Guid.NewGuid().ToString("N");
            try {
                Check(SaveGameService.Save(saveSlot, out message), "real compressed save: " + message);
                Check(SaveGameService.Load(saveSlot, out message) && Json(desk.CaptureState().ScoutingKnowledge) == savedKnowledge &&
                    desk.Label.cashReserves == paidCash && Math.Abs(desk.Label.monthlyExpenses - expenses - listener.ConsultationFee) < .01f,
                    "real compressed reload preserves paid opinion, cash, expense and continuous admission: " + message);
                Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, null, out _) && desk.Label.cashReserves == paidCash,
                    "real reload cannot replay paid event");
            } finally { SaveGameService.Delete(saveSlot); }
        }
        // Controlled consecutive appearance: admission is continuous, consultation remains per set.
        var continued = WorldStateService.Capture().SceneRooms; var continuedBill = continued.Bills.Single(b => b.Id == bill.Id);
        var second = LocalSceneRoomService.Copy(slot);
        second.StartHour = continuedBill.Appearances[1].StartHour; second.EndHour = continuedBill.Appearances[1].EndHour;
        second.Status = SceneBillStatus.Scheduled; second.Performance = null; second.Role = "later set";
        continuedBill.Appearances[1] = second;
        LocalSceneRoomService.BeginRestore(continued); LocalSceneRoomService.CompleteRestore();
        string secondEvent = ScenePerformanceService.EventId(continuedBill, second);
        Check(desk.WaitForSceneAppearance(bill.Id, secondEvent, out _), "wait for genuine later appearance");
        var returning = desk.HiredScoutingListeners(bill.Id, secondEvent).Single(l => l.PersonId == person.personId);
        Check(returning.Admission == 0 && returning.ConsultationFee > 0, "continuous visit waives admission but not next set's consultation");
        Check(desk.ListenSceneAppearanceWithListener(bill.Id, secondEvent, person.personId, returning.QuoteId, out _) &&
            Math.Abs(paidCash - desk.Label.cashReserves - returning.ConsultationFee) < .01f, "second consultation billed once without duplicate ticket");
        LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore(); desk.RestoreState(empty, out _);
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour, 30);
        var partial = desk.HiredScoutingListeners(bill.Id, eventId).Single(l => l.PersonId == person.personId);
        Check(Math.Abs(partial.ConsultationFee - listener.ConsultationFee / 2) < .01f, "partial set quotes only remaining time");
        Check(!desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, listener.QuoteId, out _), "old full-set quote rejected");
        Check(desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, partial.QuoteId, out _) &&
            desk.ScoutingDossiers.Single().Observations.All(o => o.Coverage == .5f), "paired partial coverage preserved");
        foreach (string flag in new[] { "--disable-scouting-experience", "--observe-scouting-experience" }) {
            LocalScenes.Configure(new[] { flag });
            Check(desk.HiredScoutingListeners(bill.Id, eventId).Count == 0, "off/observer blocks paid action");
        }
        LocalScenes.Configure(Array.Empty<string>());
        ContactNetworkService.Configure(new[] { "--disable-session-employment" });
        Check(desk.HiredScoutingListeners(bill.Id, eventId).Count == 0, "session supply disable respected");
        ContactNetworkService.Configure(Array.Empty<string>());
        GD.Print($"SCOUTING_HIRED_LISTENER_CHECK_PASS checks={checks}");
    }
}
