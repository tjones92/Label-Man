using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingMeetingChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_MEETING_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Meeting Checks", "nashville", out string message), "label: " + message);
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate).First(b => b.Status == SceneBillStatus.Scheduled &&
            b.Appearances.Count > 0 && b.Appearances[0].EndHour < Math.Min(clock.HardStopHour, SceneRoomCatalog.Get(b.RoomId).CloseHour));
        var slot = bill.Appearances[0]; string artistId = slot.ArtistId;
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        Check(desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message), "actual first listening: " + message);
        var act = am.GetArtist(artistId);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
        act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
        desk.Label.cashReserves = 100000;
        var baseWorld = WorldStateService.Capture(); var basePlayer = desk.CaptureState();
        var baseDate = clock.CurrentDate; int baseHour = clock.CurrentHour, baseMinute = clock.CurrentMinute;
        void Reset() {
            LocalScenes.Configure(Array.Empty<string>());
            WorldStateService.Apply(LocalSceneRoomService.Copy(baseWorld), baseDate, SimulationSeedBootstrap.RequestedSeed);
            if (!desk.RestoreState(LocalSceneRoomService.Copy(basePlayer), out message)) throw new InvalidOperationException(message);
            clock.RestoreClock(baseDate, baseHour, baseMinute);
        }
        SceneConversationQuote Quote() => desk.PreviewScoutingInvitation(artistId);
        SceneConversation Book() {
            var q = Quote();
            if (q == null || !desk.ArrangeScoutingMeeting(artistId, q.Id, out message)) throw new InvalidOperationException("No fixture slot: " + message);
            return desk.ScoutingMeetings.Last();
        }
        ScoutingObservation[] MeetingEvidence() => desk.ScoutingDossiers.Single(d => d.ArtistId == artistId).Observations
            .Where(o => o.Kind == ScoutingEvidenceKind.MeetingStatement).ToArray();
        var quote = Quote();
        Check(quote != null && quote.Day > bill.Day && quote.EndMinute - quote.StartMinute == 60, "real future slot within existing room calendar");
        string beforeWorld = Json(WorldStateService.Capture()), beforeKnowledge = Json(desk.CaptureState().ScoutingKnowledge);
        GD.Seed(433); uint expected = GD.Randi(); GD.Seed(433);
        Quote(); desk.ScoutingMeetings.ToArray(); desk.ScoutingSummary(artistId); desk.ScoutingProvenance(artistId);
        Check(Json(WorldStateService.Capture()) == beforeWorld && Json(desk.CaptureState().ScoutingKnowledge) == beforeKnowledge &&
            GD.Randi() == expected && clock.CurrentHour == baseHour && clock.CurrentMinute == baseMinute, "previews preserve world, knowledge, RNG and clock");
        Check(!desk.ArrangeScoutingMeeting(artistId, "stale", out _) && Json(WorldStateService.Capture()) == beforeWorld &&
            Json(desk.CaptureState().ScoutingKnowledge) == beforeKnowledge, "stale invitation costs nothing and records nothing");
        float cash = desk.Label.cashReserves;
        var accepted = Book();
        Check(accepted.Status == SceneConversationStatus.Scheduled && accepted.PersonId == quote.PersonId && accepted.RoomId == bill.RoomId,
            "actual consent books named canonical speaker and room slot");
        Check(clock.CurrentHour * 60 + clock.CurrentMinute == baseHour * 60 + baseMinute + 15 && desk.Label.cashReserves == cash,
            "scheduling consumes exactly fifteen minutes and no fee");
        Check(desk.CaptureState().ScoutingKnowledge.Visit?.EndMinute == baseHour * 60 + baseMinute + 15,
            "brief invitation remains in the continuous paid visit without acquiring concurrent music");
        Check(MeetingEvidence().Single().SourceId == accepted.PersonId && MeetingEvidence().Single().Claim == accepted.Response &&
            MeetingEvidence().Single().AcquiredMinute == baseHour * 60 + baseMinute + 15 &&
            desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == 1, "source/time retained; scheduling is not new listening");
        Check(desk.ScoutingDossiers.Single().Observations.All(o => o.Kind != ScoutingEvidenceKind.BusinessStatement) &&
            am.GetArtist(artistId).labelId == null, "appointment generates no ask, agreement or ownership hold");
        Check(LocalSceneRoomService.HasPersonBooking(accepted.PersonId, accepted.Day, accepted.StartMinute, accepted.EndMinute, accepted.RoomId), "speaker time is reserved");
        Check(!desk.ArrangeScoutingMeeting(artistId, quote.Id, out _) && desk.ScoutingMeetings.Count == 1 && clock.CurrentMinute == 15,
            "reopening or repeating invitation cannot duplicate commitment or spend time");
        Check(!desk.AttendScoutingMeeting(accepted.Id, out _) && clock.CurrentHour == baseHour && clock.CurrentMinute == 15, "early attendance does not consume the meeting");
        string frozenRooms = Json(WorldStateService.Capture().SceneRooms), frozenEvidence = Json(desk.CaptureState().ScoutingKnowledge);
        string saveSlot = "Meeting probe " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(saveSlot, out message), "compressed save: " + message);
            Check(SaveGameService.Load(saveSlot, out message) && Json(WorldStateService.Capture().SceneRooms) == frozenRooms &&
                Json(desk.CaptureState().ScoutingKnowledge) == frozenEvidence && desk.ScoutingMeetings.Single().Id == accepted.Id,
                "real compressed load keeps consent, slot, ordinal and evidence: " + message);
        } finally { SaveGameService.Delete(saveSlot); }
        Check(Quote() == null, "loaded meeting cannot acquire a second invitation");
        var validRoomSave = WorldStateService.Capture().SceneRooms;
        var invalid = LocalSceneRoomService.Copy(validRoomSave); invalid.Conversations.Add(LocalSceneRoomService.Copy(invalid.Conversations.Single())); invalid.ConversationOrdinal++;
        bool rejected = false; try { LocalSceneRoomService.ValidateRestore(invalid); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && Json(WorldStateService.Capture().SceneRooms) == frozenRooms, "duplicate saved IDs rejected before mutation");
        invalid = LocalSceneRoomService.Copy(validRoomSave); var conflict = LocalSceneRoomService.Copy(invalid.Conversations.Single());
        conflict.Id += "-other"; conflict.ArtistId += "-other"; invalid.Conversations.Add(conflict); invalid.ConversationOrdinal++;
        rejected = false; try { LocalSceneRoomService.ValidateRestore(invalid); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "overlapping saved speaker/room/label slots rejected");
        invalid = LocalSceneRoomService.Copy(validRoomSave); invalid.Conversations.Single().StartMinute = 0; invalid.Conversations.Single().EndMinute = 60;
        rejected = false; try { LocalSceneRoomService.ValidateRestore(invalid); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "saved appointments outside published room hours rejected");
        invalid = LocalSceneRoomService.Copy(validRoomSave); conflict = LocalSceneRoomService.Copy(invalid.Conversations.Single());
        conflict.Id += "-repeat"; conflict.Status = SceneConversationStatus.Cancelled; invalid.Conversations.Add(conflict); invalid.ConversationOrdinal++;
        rejected = false; try { LocalSceneRoomService.ValidateRestore(invalid); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "same source encounter cannot manufacture repeated saved consent");
        clock.RestoreClock(LocalSceneRoomService.Date(accepted.Day), accepted.StartMinute / 60, accepted.StartMinute % 60);
        int end = accepted.EndMinute;
        Check(desk.AttendScoutingMeeting(accepted.Id, out message), "actual meeting hands off to contract discussion: " + message);
        Check(clock.CurrentHour * 60 + clock.CurrentMinute == end && desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Completed &&
            desk.CaptureState().ScoutingKnowledge.Visit == null && desk.Label.cashReserves == cash, "one hour, no fee, completed slot, visit ended");
        Check(desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == 1 &&
            desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.BusinessStatement) == 1 &&
            desk.ScoutingDossiers.Single().Observations.Last(o => o.Kind == ScoutingEvidenceKind.BusinessStatement).BusinessStatementKind == ScoutingBusinessStatementKind.OpeningAsk,
            "only actual discussion records opening terms; no extra performance or fabricated agreement");
        int count = desk.ScoutingDossiers.Single().Observations.Count;
        Check(!desk.AttendScoutingMeeting(accepted.Id, out _) && clock.CurrentHour * 60 + clock.CurrentMinute == end,
            "completed appointment cannot repeat or spend another hour");
        count = desk.ScoutingDossiers.Single().Observations.Count;
        Check(!desk.AttendScoutingMeeting(accepted.Id, out _) && desk.ScoutingDossiers.Single().Observations.Count == count,
            "repeat completion notice is deduplicated");

        Reset(); accepted = Book();
        clock.RestoreClock(LocalSceneRoomService.Date(accepted.Day), accepted.StartMinute / 60);
        void SignDuringHour(int hour) { am.GetArtist(artistId).labelId = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned).labelId; }
        clock.OnHourChanged += SignDuringHour;
        try {
            Check(!desk.AttendScoutingMeeting(accepted.Id, out _) && clock.CurrentHour * 60 + clock.CurrentMinute == accepted.EndMinute &&
                desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled &&
                desk.ScoutingDossiers.Single().Observations.All(o => o.Kind != ScoutingEvidenceKind.BusinessStatement) &&
                !MeetingEvidence().Any(o => o.EventId.EndsWith("|attended")), "availability change during consumed hour prevents stale completion and terms");
        } finally { clock.OnHourChanged -= SignDuringHour; }

        Reset(); accepted = Book();
        clock.RestoreClock(LocalSceneRoomService.Date(accepted.Day), accepted.StartMinute / 60 - 1, 45);
        Check(desk.WaitForScoutingMeeting(accepted.Id, out _) && clock.CurrentHour * 60 + clock.CurrentMinute == accepted.StartMinute &&
            desk.ScoutingDossiers.Single().Observations.All(o => o.Kind != ScoutingEvidenceKind.BusinessStatement), "waiting reaches exact start without pretending a discussion occurred");
        Check(desk.AttendScoutingMeeting(accepted.Id, out _), "waiting then attending completes ordinary handoff");
        Reset(); accepted = Book(); cash = desk.Label.cashReserves;
        LocalScenes.Configure(new[] { "--disable-scouting-experience" });
        Check(Quote() == null && !desk.AttendScoutingMeeting(accepted.Id, out _) && desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Scheduled,
            "off mode retains appointment, blocks acquisition/attendance");
        string offKnowledge = Json(desk.CaptureState().ScoutingKnowledge);
        Check(desk.CancelScoutingMeeting(accepted.Id, out _) && desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled &&
            Json(desk.CaptureState().ScoutingKnowledge) == offKnowledge && desk.Label.cashReserves == cash,
            "off mode can release a commitment without acquiring new scouting evidence");
        LocalScenes.Configure(Array.Empty<string>());
        Check(!LocalSceneRoomService.HasPersonBooking(accepted.PersonId, accepted.Day, accepted.StartMinute, accepted.EndMinute, accepted.RoomId), "cancellation releases the actual speaker slot");
        Reset(); accepted = Book(); LocalScenes.Configure(new[] { "--observe-scouting-experience" });
        Check(Quote() == null && !desk.ArrangeScoutingMeeting(artistId, quote.Id, out _) && desk.ScoutingMeetings.Single().Id == accepted.Id, "observer mode creates no new appointment");

        Reset(); am.GetArtist(artistId).prospectMarketStatus = ProspectMarketStatus.Latent;
        var declined = Book();
        Check(declined.Status == SceneConversationStatus.Declined && MeetingEvidence().Single().Claim == declined.Response &&
            !LocalSceneRoomService.HasPersonBooking(declined.PersonId, declined.Day, declined.StartMinute, declined.EndMinute, declined.RoomId), "genuine refusal acquires words without reserving time");
        Check(Quote() == null, "same encounter cannot repeat a declined request");
        Reset(); accepted = Book(); am.GetArtist(artistId).labelId = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned).labelId;
        clock.SpendMinutes(1);
        Check(desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled && !desk.AttendScoutingMeeting(accepted.Id, out _) &&
            desk.ScoutingDossiers.Single().Observations.All(o => o.Kind != ScoutingEvidenceKind.BusinessStatement), "rival ownership cancels appointment; it never reserved signing access");

        Reset(); accepted = Book();
        var speaker = am.GetArtist(artistId).members.Single(m => m.personId == accepted.PersonId); speaker.isActive = false; clock.SpendMinutes(1);
        Check(desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled && desk.ScoutingMeetings.Single().Response.Contains("speaker"), "departed speaker invalidates appointment without substituting another person");
        Reset(); accepted = Book();
        var revised = WorldStateService.Capture().SceneRooms;
        revised.Bills.Add(new SceneBill { Id = "meeting-booking-conflict", RoomId = accepted.RoomId, Day = accepted.Day,
            Year = LocalSceneRoomService.Date(accepted.Day).year, StartHour = accepted.StartMinute / 60, EndHour = accepted.EndMinute / 60,
            Appearances = new() { new SceneAppearance { ArtistId = artistId, PersonIds = new() { accepted.PersonId },
                StartHour = accepted.StartMinute / 60, EndHour = accepted.EndMinute / 60 } } });
        LocalSceneRoomService.BeginRestore(revised); LocalSceneRoomService.CompleteRestore(); clock.SpendMinutes(1);
        Check(desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled && desk.ScoutingMeetings.Single().Response.Contains("booking"), "changed real performance booking cancels stale conversation slot");

        Reset(); accepted = Book(); clock.RestoreClock(LocalSceneRoomService.Date(accepted.Day), accepted.StartMinute / 60, 1); clock.SpendMinutes(1);
        Check(desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Missed && !desk.WaitForScoutingMeeting(accepted.Id, out _) &&
            desk.ScoutingDossiers.Single().Observations.All(o => o.Kind != ScoutingEvidenceKind.BusinessStatement), "missed start remains missed and acquires no ask");
        Reset(); accepted = Book(); Check(desk.SetScoutingPriority(artistId, ScoutingPriority.Archived) && desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Scheduled,
            "archiving notes never cancels a real commitment");
        clock.RestoreClock(LocalSceneRoomService.Date(accepted.Day), accepted.StartMinute / 60);
        // Exercise the contract owner's roster guard after consuming the agreed conversation.
        desk.Label.maxRosterSize = 0;
        Check(!desk.AttendScoutingMeeting(accepted.Id, out _) && desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Completed &&
            desk.ScoutingDossiers.Single().Observations.All(o => o.Kind != ScoutingEvidenceKind.BusinessStatement), "meeting never bypasses roster capacity or fabricates agreement");

        Reset(); accepted = Book();
        int days = 0; void CountDay(GameDate date) => days++;
        // A controlled external signing during normal waiting establishes lack of an act hold.
        void SignOnDay(GameDate date) { am.GetArtist(artistId).labelId = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned).labelId; }
        clock.OnDayEnded += CountDay; clock.OnDayStarted += SignOnDay;
        try {
            Check(!desk.WaitForScoutingMeeting(accepted.Id, out _) && days >= 1 && clock.CurrentDate > baseDate &&
                desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled, "waiting advances ordinary day events and stops when external ownership invalidates meeting");
        } finally { clock.OnDayEnded -= CountDay; clock.OnDayStarted -= SignOnDay; }
        Reset();
        var legacy = LocalSceneRoomService.Copy(baseWorld.SceneRooms); legacy.Conversations = null; legacy.ConversationOrdinal = 0;
        LocalSceneRoomService.BeginRestore(legacy); LocalSceneRoomService.CompleteRestore();
        Check(desk.ScoutingMeetings.Count == 0 && Quote() != null, "v11 room state gains empty appointments, not invented consent");
        Check(SaveGameService.CurrentVersion >= 13, "old executables reject saves containing authoritative appointments");
        GD.Print($"SCOUTING_MEETING_CHECK_PASS checks={checks}");
    }
}
