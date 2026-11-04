using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingBusinessChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_BUSINESS_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Business Checks", "nashville", out string message), "found label: " + message);
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate)
            .First(b => b.Appearances.Count >= 2 && b.EndHour <= clock.HardStopHour && b.Status == SceneBillStatus.Scheduled);
        var slot = bill.Appearances[0]; var act = am.GetArtist(slot.ArtistId); string eventId = ScenePerformanceService.EventId(bill, slot);
        var labels = ChartManager.Instance.GetAllLabels().Where(l => !l.isPlayerOwned).ToArray();
        var winner = ChartManager.Instance.GetLabelById(act.labelId) ?? labels.First(); var loser = labels.First(l => l.labelId != winner.labelId);
        var originOwner = act.labelId; act.labelId = winner.labelId;
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        var signed = new RosterManager.DailyTalentMarketAppointment { Date = clock.CurrentDate, Label = winner, SelectedArtist = act,
            Outcome = "AcceptedArtistChoice", WinnerLabelId = winner.labelId, CollisionOfferCount = 2 };
        var lost = new RosterManager.DailyTalentMarketAppointment { Date = clock.CurrentDate, Label = loser, SelectedArtist = act,
            Outcome = "LostArtistChoice", WinnerLabelId = winner.labelId, CollisionOfferCount = 2 };
        var negative = new RosterManager.DailyTalentMarketAppointment { Date = clock.CurrentDate, Label = loser, SelectedArtist = act, Outcome = "Nominated" };
        var knowledgeBefore = Json(desk.CaptureState().ScoutingKnowledge);
        GameRandom.Seed(193); uint expected = GameRandom.Randi(); GameRandom.Seed(193);
        RosterManager.RecordScoutingMarketDecisions(new[] { negative });
        Check(act.sceneMarketHistory == null, "internal nomination is not a market-history event");
        RosterManager.RecordScoutingMarketDecisions(new[] { signed, lost });
        Check(act.sceneMarketHistory.Count == 2 && act.sceneMarketHistory.Any(h => h.Kind == SceneMarketDecisionKind.Signed) &&
            act.sceneMarketHistory.Any(h => h.Kind == SceneMarketDecisionKind.AlternativeNotSelected), "actual resolved outcome kinds retained");
        Check(act.sceneMarketHistory.All(h => h.Day == bill.Day && h.ArtistId == act.artistId && h.SelectedDealCompleted &&
            h.SelectedLabelId == winner.labelId), "exact date, shared selection and completed-deal provenance retained");
        Check(GameRandom.Randi() == expected && desk.Label.cashReserves == 900 && Json(desk.CaptureState().ScoutingKnowledge) == knowledgeBefore,
            "world-history recording consumes no RNG/cash and acquires no player knowledge");
        Check(JsonSerializer.Deserialize<SimulatedArtist>("{\"artistId\":\"legacy-act\"}", SaveGameService.TestJsonOptions).sceneMarketHistory == null,
            "legacy acts receive no fabricated market history");
        Check((int)ScoutingEvidenceKind.PublicReport == 3 && (int)ScoutingEvidenceKind.ListenerOpinion == 4,
            "new evidence kind preserves prior serialized enum ordinals");
        int winnerOrdinal = winner.scoutingAppointmentOrdinal, loserOrdinal = loser.scoutingAppointmentOrdinal;
        winner.scoutingAppointmentOrdinal++; loser.scoutingAppointmentOrdinal++;
        signed.Outcome = "InvalidatedBeforeCommit"; act.labelId = originOwner;
        RosterManager.RecordScoutingMarketDecisions(new[] { signed, lost });
        Check(act.sceneMarketHistory.Count == 4 && act.sceneMarketHistory.Any(h => h.Kind == SceneMarketDecisionKind.SigningAttemptNotCompleted && !h.SelectedDealCompleted) &&
            act.sceneMarketHistory.Any(h => h.Kind == SceneMarketDecisionKind.AlternativeNotSelected && !h.SelectedDealCompleted),
            "failed selected commit and its alternative do not imply a completed contract");
        signed.Outcome = "AcceptedArtistChoice"; act.labelId = winner.labelId;
        winner.scoutingAppointmentOrdinal = winnerOrdinal; loser.scoutingAppointmentOrdinal = loserOrdinal;
        string history = Json(act.sceneMarketHistory);
        RosterManager.RecordScoutingMarketDecisions(new[] { signed, lost });
        Check(Json(act.sceneMarketHistory) == history, "market event deduplicated by genuine appointment identity");
        var fake = JsonSerializer.Deserialize<SimulatedArtist>(Json(act), SaveGameService.TestJsonOptions);
        signed.SelectedArtist = fake;
        RosterManager.RecordScoutingMarketDecisions(new[] { signed });
        Check(Json(act.sceneMarketHistory) == history && Json(fake.sceneMarketHistory) == history, "detached act cannot receive owner history"); signed.SelectedArtist = act;
        Check(desk.PreviewScoutingBusinessMeeting(act.artistId) == null, "cannot converse before hearing a real set");
        act.labelId = originOwner;
        Check(desk.ListenSceneAppearance(bill.Id, eventId, out message), "hear actual set: " + message);
        var preview = desk.PreviewScoutingBusinessMeeting(act.artistId);
        Check(preview?.Available == true, "free actual performer can speak after the set during room hours");
        Check(preview.Claim.Contains(winner.labelName) && preview.Claim.Contains(loser.labelName) && preview.Claim.Contains("not a current offer"),
            "past alternatives are named and scoped without fake urgency");
        Check(!preview.Claim.Contains("utility") && !preview.Claim.Contains("reservation") && !preview.Claim.Contains("potential"), "no evaluator or talent truth disclosed");
        float cash = desk.Label.cashReserves; int now = clock.CurrentHour * 60 + clock.CurrentMinute;
        string world = Json(WorldStateService.Capture()), before = Json(desk.CaptureState());
        GameRandom.Seed(197); expected = GameRandom.Randi(); GameRandom.Seed(197);
        Check(Json(preview) == Json(desk.PreviewScoutingBusinessMeeting(act.artistId)), "preview stable on refresh");
        Check(world == Json(WorldStateService.Capture()) && before == Json(desk.CaptureState()) && GameRandom.Randi() == expected,
            "preview writes nothing and consumes no world RNG");
        Check(!desk.AskScoutingBusinessHistory(act.artistId, "stale", out _) && desk.Label.cashReserves == cash &&
            clock.CurrentHour * 60 + clock.CurrentMinute == now, "stale quote costs nothing");
        var speaker = am.GetMusician(preview.SourcePersonId); var life = speaker.lifeState; speaker.lifeState = MemberLifeState.Injured;
        Check(!desk.AskScoutingBusinessHistory(act.artistId, preview.QuoteId, out _), "injury or replacement speaker invalidates quote"); speaker.lifeState = life;
        var sourceRooms = WorldStateService.Capture().SceneRooms;
        var conflict = LocalSceneRoomService.Copy(sourceRooms);
        conflict.Bills.Add(new SceneBill { Id = "business-speaker-conflict", RoomId = bill.RoomId, Day = bill.Day,
            StartHour = now / 60, EndHour = now / 60 + 1, Status = SceneBillStatus.Scheduled,
            Appearances = new() { new() { ArtistId = act.artistId, StartHour = now / 60, EndHour = now / 60 + 1,
                Status = SceneBillStatus.Scheduled, PersonIds = act.members.Where(m => m.isActive).Select(m => m.personId).ToList() } } });
        LocalSceneRoomService.BeginRestore(conflict); LocalSceneRoomService.CompleteRestore();
        Check(desk.PreviewScoutingBusinessMeeting(act.artistId) == null, "a performing cast cannot simultaneously hold the conversation");
        LocalSceneRoomService.BeginRestore(sourceRooms); LocalSceneRoomService.CompleteRestore();
        preview = desk.PreviewScoutingBusinessMeeting(act.artistId);
        Check(desk.AskScoutingBusinessHistory(act.artistId, preview.QuoteId, out message), "source acquisition: " + message);
        var dossier = desk.ScoutingDossiers.Single(); var claim = dossier.Observations.Single(o => o.Kind == ScoutingEvidenceKind.ActBusinessClaim);
        Check(clock.CurrentHour * 60 + clock.CurrentMinute == now + 60 && desk.Label.cashReserves == cash,
            "one ordinary hour, no conversation fee or parallel listening credit");
        Check(desk.CaptureState().ScoutingKnowledge.Visit == null, "substantial conversation ends continuous admission");
        Check(claim.SourceId == preview.SourcePersonId && claim.SourceName == preview.SourceName && claim.Claim == preview.Claim &&
            claim.OccurredDay == bill.Day && claim.AcquiredMinute == now + 60 && claim.Channel == "post-set act conversation", "statement and receipt retain actual source/time/context");
        Check(dossier.Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == 1 &&
            desk.ScoutingSummary(act.artistId).Any(l => l.StartsWith("Heard 1 distinct set")), "business account is not another performance sample");
        Check(desk.ScoutingCompetitionSummary(act.artistId).Contains(loser.labelName) &&
            desk.ScoutingProvenance(act.artistId).Any(l => l.Contains(speaker.FullName) && l.Contains("no current offer")), "notebook and provenance show acquired history");
        string acquired = Json(desk.CaptureState().ScoutingKnowledge);
        Check(!desk.AskScoutingBusinessHistory(act.artistId, null, out _) && Json(desk.CaptureState().ScoutingKnowledge) == acquired,
            "repeated account does not spend time or buy new evidence");
        var saved = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        Check(desk.RestoreState(saved, out _) && Json(desk.CaptureState().ScoutingKnowledge) == acquired, "business account JSON restore exact");
        var copiedAct = JsonSerializer.Deserialize<SimulatedArtist>(Json(act), SaveGameService.TestJsonOptions);
        Check(Json(copiedAct.sceneMarketHistory) == history, "canonical artist history serializes through existing save schema");
        if (OS.GetCmdlineUserArgs().Contains("--scouting-business-save-check")) {
            string saveSlot = "Business probe " + Guid.NewGuid().ToString("N");
            try {
                Check(SaveGameService.Save(saveSlot, out message), "real compressed save: " + message);
                Check(SaveGameService.Load(saveSlot, out message) && Json(desk.CaptureState().ScoutingKnowledge) == acquired &&
                    Json(am.GetArtist(act.artistId).sceneMarketHistory) == history, "real load preserves owner history and acquired source: " + message);
                act = am.GetArtist(act.artistId);
            } finally { SaveGameService.Delete(saveSlot); }
        }
        act.sceneMarketHistory.Clear();
        Check(desk.ScoutingCompetitionSummary(act.artistId).Contains(loser.labelName), "acquired claim survives bounded source-history pruning");
        foreach (string flag in new[] { "--observe-scouting-experience", "--disable-scouting-experience" }) {
            LocalScenes.Configure(new[] { flag });
            Check(desk.PreviewScoutingBusinessMeeting(act.artistId) == null && !desk.AskScoutingBusinessHistory(act.artistId, null, out _) &&
                Json(desk.CaptureState().ScoutingKnowledge) == acquired, "off/observer modes retain evidence without acquiring conversations");
        }
        LocalScenes.Configure(Array.Empty<string>());
        var limitClock = clock.CurrentDate;
        signed.SelectedArtist = act; act.labelId = winner.labelId;
        winner = ChartManager.Instance.GetLabelById(winner.labelId); signed = new() { Date = limitClock, Label = winner, SelectedArtist = act,
            Outcome = "AcceptedUncontested", WinnerLabelId = winner.labelId };
        int ordinal = winner.scoutingAppointmentOrdinal;
        for (int i = 0; i < RosterManager.ScoutingMarketHistoryLimit + 4; i++) { winner.scoutingAppointmentOrdinal = ordinal + i; RosterManager.RecordScoutingMarketDecisions(new[] { signed }); }
        Check(act.sceneMarketHistory.Count == RosterManager.ScoutingMarketHistoryLimit, "unacquired market history bounded per act");
        winner.scoutingAppointmentOrdinal = ordinal; act.labelId = originOwner;
        string retained = Json(act.sceneMarketHistory);
        LocalScenes.Configure(new[] { "--disable-scouting-market-history" }); RosterManager.RecordScoutingMarketDecisions(new[] { signed });
        Check(Json(act.sceneMarketHistory) == retained, "history-off control preserves prior events and adds none"); LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCOUTING_BUSINESS_CHECK_PASS checks={checks}");
    }
}
