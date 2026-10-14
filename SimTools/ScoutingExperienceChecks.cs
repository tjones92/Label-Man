using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingExperienceChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_EXPERIENCE_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var clock = TimeManager.Instance; var desk = PlayerDesk.Instance; var am = ArtistManager.Instance;
        Check(LocalScenes.Rooms, "scene room dependency");
        Check(desk.FoundLabel("Scouting Checks", "nashville", out string message), "label fixture: " + message);
        var calendar = LocalSceneRoomService.Calendar("nashville", clock.CurrentDate);
        var bill = calendar.First(b => b.Appearances.Count >= 2 && b.Appearances[1].EndHour <= clock.HardStopHour && b.Status == SceneBillStatus.Scheduled);
        var room = SceneRoomCatalog.Get(bill.RoomId); var slot = bill.Appearances[0];
        string identity = ScenePerformanceService.EventId(bill, slot);
        var act = am.GetArtist(slot.ArtistId);
        int population = am.GetAllArtists().Count();
        // A paired room-state replay: observing the first set and advancing an unvisited clock
        // produce precisely the same performance/work state.
        var world = WorldStateService.Capture();
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        Check(desk.WaitForSceneAppearance(bill.Id, identity, out message), "explicit wait: " + message);
        float cash = desk.Label.cashReserves;
        int previewHour = clock.CurrentHour;
        var preview = desk.NextReachableScoutingSet(act.artistId);
        Check(preview != null && preview.EventId == identity && preview.Playing &&
            preview.ListeningMinutes == (slot.EndHour - slot.StartHour) * 60, "reachable preview identifies actual playing event and remaining minutes");
        Check(desk.Label.cashReserves == cash && clock.CurrentHour == previewHour &&
            desk.ScoutingDossiers.Count == 0, "opportunity preview spends no time, cash or evidence");
        var previewRooms = WorldStateService.Capture().SceneRooms;
        var unavailableRooms = LocalSceneRoomService.Copy(previewRooms);
        unavailableRooms.Bills.Single(b => b.Id == bill.Id).Status = SceneBillStatus.Cancelled;
        LocalSceneRoomService.BeginRestore(unavailableRooms); LocalSceneRoomService.CompleteRestore();
        Check(desk.NextReachableScoutingSet(act.artistId)?.EventId != identity, "cancelled bill is not offered as a next set");
        Check(!desk.ListenSceneAppearance(preview.BillId, preview.EventId, out message) &&
            desk.Label.cashReserves == cash && clock.CurrentHour == previewHour, "stale preview rechecks cancellation without cost");
        unavailableRooms = LocalSceneRoomService.Copy(previewRooms);
        unavailableRooms.Bills.Single(b => b.Id == bill.Id).Appearances[0].EndHour = clock.HardStopHour + 1;
        LocalSceneRoomService.BeginRestore(unavailableRooms); LocalSceneRoomService.CompleteRestore();
        Check(desk.NextReachableScoutingSet(act.artistId)?.EventId != identity, "set beyond hard stop is not offered");
        LocalSceneRoomService.BeginRestore(previewRooms); LocalSceneRoomService.CompleteRestore();
        var emptySave = desk.CaptureState();
        Check(desk.ListenSceneAppearance(bill.Id, identity, out message), "actual set listening: " + message);
        var observedWorld = WorldStateService.Capture().SceneRooms;
        var knowledge = desk.CaptureState().ScoutingKnowledge;
        Check(knowledge.Dossiers.Count == 1 && knowledge.Dossiers[0].Observations.Count == 1, "automatic dossier independent of manual notebook");
        var observation = knowledge.Dossiers[0].Observations[0];
        Check(observation.EventId == identity && observation.PersonIds.SequenceEqual(slot.PersonIds) && observation.Coverage == 1, "event, people and complete coverage preserved");
        Check(clock.CurrentHour == slot.EndHour && clock.CurrentMinute == 0, "one scheduled set spends its actual duration");
        Check(observation.RoomName == room.Name && observation.AcquiredMinute == slot.EndHour * 60, "room name and actual acquisition time retained");
        Check(desk.ScoutingProvenance(act.artistId).Any(l => l.Contains(room.Name) && l.Contains("Heard material:")) &&
            desk.ScoutingProvenance(act.artistId).All(l => !l.Contains("performance-v1|")), "player provenance shows room and material without raw event keys");
        Check(Math.Abs(cash - desk.Label.cashReserves - SceneLiveEconomics.Admission(room, bill.Year)) < .01, "admission charged once");
        Check(observation.BillAttendance == null || observation.BillCapacity > 0, "audience count always has a denominator");
        Check(desk.SetScoutingQuestion(act.artistId, ScoutingQuestion.Ensemble), "pin a purposeful listening question");
        Check(!desk.SetScoutingQuestion(act.artistId, (ScoutingQuestion)99) &&
            !desk.SetScoutingQuestion("missing-act", ScoutingQuestion.Material), "invalid question or unknown dossier rejected");
        Check(desk.ScoutingSummary(act.artistId).Any(l => l.Contains("another date is needed")), "one date leaves consistency question open");
        Check(desk.NextReachableScoutingSet(act.artistId)?.EventId != identity, "next opportunity skips already observed performance");
        Check(desk.Label.cashReserves == cash - SceneLiveEconomics.Admission(room, bill.Year) &&
            clock.CurrentHour == slot.EndHour, "pinning and preview do not spend action resources");
        // Matched acquired evidence isolates date, coverage, source and axis behavior.
        var comparison = new ScoutingDossier { Question = ScoutingQuestion.Ensemble };
        ScoutingObservation Sample(int day, float execution, float material, float coverage = 1) =>
            new() { Kind = ScoutingEvidenceKind.Performance, OccurredDay = day, Coverage = coverage,
                RoomId = room.Id, PersonIds = slot.PersonIds.ToList(),
                Interpretation = new() { Execution = execution, Material = material } };
        comparison.Observations.Add(Sample(bill.Day, .3f, .8f));
        comparison.Observations.Add(Sample(bill.Day, .8f, .2f));
        comparison.Observations.Add(new() { Kind = ScoutingEvidenceKind.BookerClaim, OccurredDay = bill.Day + 1 });
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("another date is needed"), "same-night sets and reports are not independent dates");
        comparison.Observations.Add(Sample(bill.Day + 1, .9f, .1f));
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("broadly similar"), "compare latest acquired set from each date");
        comparison.Question = ScoutingQuestion.Material;
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("broadly similar"), "material question uses material impressions");
        comparison.Observations[1].Interpretation.Material = .8f;
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("less assured"), "material disagreement is explicit without causal diagnosis");
        comparison.Question = ScoutingQuestion.Vocals;
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("no comparable vocal evidence"), "instrumental evidence does not imply poor vocals");
        comparison.Question = ScoutingQuestion.Ensemble;
        comparison.Observations.Last().Coverage = .5f;
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("coverage was partial"), "partial sets leave comparison open");
        comparison.Observations.Last().Coverage = 1;
        comparison.Observations.Last().PersonIds.Clear();
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("people unknown"), "changed people qualify comparison");
        comparison.Observations.Reverse();
        Check(PlayerDesk.ScoutingQuestionRead(comparison).Contains("people unknown"), "comparison follows occurrence dates rather than receipt order");
        string summary = Json(desk.ScoutingSummary(act.artistId));
        string recorded = Json(desk.CaptureState().ScoutingKnowledge);
        Check(desk.OpenScoutingDossier(act.artistId, out message) && desk.OpenScoutingDossier(act.artistId, out message), "reopen recorded dossier");
        Check(Json(desk.CaptureState().ScoutingKnowledge) == recorded && Json(desk.ScoutingSummary(act.artistId)) == summary, "refresh/reopening changes no evidence");
        var afterCash = desk.Label.cashReserves; int afterHour = clock.CurrentHour;
        Check(!desk.ListenSceneAppearance(bill.Id, identity, out message) && desk.Label.cashReserves == afterCash && clock.CurrentHour == afterHour, "same occurrence adds no cost or evidence");
        var savedPlayer = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        Check(desk.RestoreState(savedPlayer, out message), "player evidence reload: " + message);
        Check(Json(desk.CaptureState().ScoutingKnowledge) == recorded, "player evidence byte roundtrip");
        Check(desk.ScoutingDossiers.First(d => d.ArtistId == act.artistId).Question == ScoutingQuestion.Ensemble, "question survives serialized player restore");
        Check(desk.SetScoutingPriority(act.artistId, ScoutingPriority.Archived), "archive action");
        Check(desk.ScoutingDossiers.First(d => d.ArtistId == act.artistId).Question == ScoutingQuestion.Ensemble, "archive preserves listening question");
        Check(desk.HasScoutingEvidence(act.artistId) && desk.ScoutingDossiers.First(d => d.ArtistId == act.artistId).Observations.Count == 1, "archive preserves evidence and people");
        Check(desk.SetScoutingPriority(act.artistId, ScoutingPriority.Pursue), "unarchive priority");
        var second = bill.Appearances[1];
        Check(desk.WaitForSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, second), out message), "stay for later appearance: " + message);
        Check(desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, second), out message), "second appearance: " + message);
        Check(desk.Label.cashReserves == afterCash, "two acts within one visit share admission, including reload");
        Check(desk.ScoutingDossiers.Sum(d => d.Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance)) == 2, "two appearances give distinct evidence");
        Check(am.GetAllArtists().Count() == population, "observations never spawn/delete acts");
        // All observers use the same event; observer interpretations can differ but do not mutate it.
        var shared = LocalSceneRoomService.Bill(bill.Id).Appearances[0].Performance;
        string sharedBefore = Json(shared);
        _ = ScenePerformanceService.Interpret(shared, "other-label", .9f, 1);
        Check(Json(shared) == sharedBefore, "interpretation does not mutate performance truth");
        var member = act.members.First(m => slot.PersonIds.Contains(m.personId));
        member.isActive = false;
        Check(desk.ScoutingSummary(act.artistId).Any(l => l.StartsWith("Lineup changed")), "lineup changes mark evidence stale");
        member.isActive = true;
        // Real canonical eligibility is rechecked; useful signed performers cannot be approached.
        var p = new PlayerDesk.Prospect { Artist = act, SceneRoomId = room.Id, SceneBillId = bill.Id };
        string owner = act.labelId; act.labelId = "signed-fixture";
        Check(!desk.ApproachToSign(p, out _), "signed encounter never bypasses ownership"); act.labelId = owner;
        var oldStatus = act.prospectMarketStatus; act.prospectMarketStatus = ProspectMarketStatus.Latent;
        Check(!desk.ApproachToSign(p, out _), "latent performer never bypasses activation"); act.prospectMarketStatus = oldStatus;
        var changedDossier = desk.ScoutingDossiers.First(d => d.ArtistId == act.artistId);
        changedDossier.Observations.Clear();
        Check(desk.HasScoutingEvidence(act.artistId), "returned dossier snapshots are detached");
        string evidenceBeforeModes = Json(desk.CaptureState().ScoutingKnowledge);
        LocalScenes.Configure(new[] { "--disable-scouting-experience" });
        Check(!desk.ListenSceneAppearance(bill.Id, identity, out _) && Json(desk.CaptureState().ScoutingKnowledge) == evidenceBeforeModes, "disable retains evidence and spends nothing");
        Check(desk.NextReachableScoutingSet(act.artistId) == null, "disabled encounters have no actionable follow-up");
        LocalScenes.Configure(new[] { "--observe-scouting-experience" });
        Check(desk.NextReachableScoutingSet(act.artistId) == null, "observer mode has no actionable follow-up");
        Check(LocalScenes.ScoutingExperience && LocalScenes.ScoutingExperienceObserver && !desk.ListenSceneAppearance(bill.Id, identity, out _), "observer resolves shared events without acquisition");
        Check(!desk.AskScoutingBooker(room.Id, act.artistId, true, out _), "observer mode blocks new source claims");
        LocalScenes.Configure(Array.Empty<string>());
        Check(desk.AskScoutingBooker(room.Id, act.artistId, true, out message), "local source question: " + message);
        int conversationHour = clock.CurrentHour; float conversationCash = desk.Label.cashReserves;
        Check(!desk.AskScoutingBooker(room.Id, act.artistId, true, out _) && clock.CurrentHour == conversationHour && desk.Label.cashReserves == conversationCash, "identical claim cannot farm time or support");
        Check(desk.ScoutingDossiers.First(d => d.ArtistId == act.artistId).Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == 1, "a conversation is not a second performance");
        // Source questions share the existing evidence owner, with actual clock and record guards.
        var sourcePlayer = desk.CaptureState(); var sourceRooms = WorldStateService.Capture().SceneRooms;
        var sourceOriginalDate = clock.CurrentDate; var sourceDate = sourceOriginalDate.AddDays(1); int sourceHour = clock.CurrentHour, sourceMinute = clock.CurrentMinute;
        clock.RestoreClock(sourceDate, room.OpenHour);
        string beforePreview = Json(desk.CaptureState().ScoutingKnowledge);
        string beforeRoomPreview = Json(WorldStateService.Capture().SceneRooms);
        var sourcePreview = desk.PreviewScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.NextDate);
        Check(sourcePreview != null && desk.PreviewScoutingSource(room.Id, act.artistId, (ScoutingSourceQuestion)99) == null,
            "valid source preview and invalid question rejection");
        Check(Json(desk.CaptureState().ScoutingKnowledge) == beforePreview && Json(WorldStateService.Capture().SceneRooms) == beforeRoomPreview &&
            clock.CurrentHour == room.OpenHour, "source previews alter neither evidence nor world records nor clock");
        Check(desk.PreviewScoutingSource("missing-room", act.artistId, ScoutingSourceQuestion.NextDate) == null &&
            desk.PreviewScoutingSource(room.Id, "missing-act", ScoutingSourceQuestion.NextDate) == null,
            "unknown sources and acts cannot create evidence");
        var sourceFixture = LocalSceneRoomService.Copy(sourceRooms);
        int sourceDay = LocalSceneRoomService.Day(sourceDate);
        foreach (var completedBill in sourceFixture.Bills.Where(b => b.RoomId == room.Id && b.Status == SceneBillStatus.Performed)) completedBill.Appearances.RemoveAll(a => a.ArtistId == act.artistId);
        sourceFixture.Bills.Add(new SceneBill { Id = "source-previous-bill", RoomId = room.Id, Day = sourceDay - 1,
            Year = sourceDate.year, EndHour = room.CloseHour, Status = SceneBillStatus.Performed, Capacity = 200, Attendance = 150,
            Appearances = new() { new SceneAppearance { ArtistId = act.artistId, Status = SceneBillStatus.Performed } } });
        LocalSceneRoomService.BeginRestore(sourceFixture); LocalSceneRoomService.CompleteRestore();
        var audience = desk.PreviewScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.BillAudience);
        Check(audience.Available && audience.BillId == "source-previous-bill" && audience.BillAttendance == 150 && audience.BillCapacity == 200 &&
            audience.Claim.Contains("whole bill") && audience.OccurredDay == sourceDay - 1,
            "booker can report retained earlier bill with denominator and scope");
        Check(!desk.AskScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.BillAudience, "stale-source", out _) &&
            clock.CurrentHour == room.OpenHour && Json(desk.CaptureState().ScoutingKnowledge) == beforePreview,
            "stale source quote blocks without time or evidence");
        int beforeConversation = clock.CurrentHour; float beforeConversationCash = desk.Label.cashReserves;
        Check(desk.AskScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.BillAudience, audience.QuoteId, out message),
            "completed bill source conversation: " + message);
        var audienceClaim = desk.ScoutingDossiers.Single(d => d.ArtistId == act.artistId).Observations.Last();
        Check(clock.CurrentHour == beforeConversation + 1 && desk.Label.cashReserves == beforeConversationCash &&
            desk.CaptureState().ScoutingKnowledge.Visit == null, "conversation spends exactly one hour, no fee, ends continuous listening visit");
        Check(audienceClaim.Kind == ScoutingEvidenceKind.BookerClaim && audienceClaim.SourceId == room.ContactId &&
            audienceClaim.BillAttendance == 150 && audienceClaim.BillCapacity == 200 && audienceClaim.OccurredDay == sourceDay - 1 &&
            audienceClaim.AcquiredDay == sourceDay && audienceClaim.AcquiredMinute == clock.CurrentHour * 60 && audienceClaim.Interpretation == null,
            "source snapshot preserves source, occurrence versus receipt, scope and no musical truth");
        var sourceSaved = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        Check(desk.RestoreState(sourceSaved, out message) && !desk.AskScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.BillAudience, audience.QuoteId, out _) &&
            clock.CurrentHour == beforeConversation + 1, "source answer survives reload and cannot be bought twice");
        sourceFixture.Bills.RemoveAll(b => b.Id == "source-previous-bill");
        LocalSceneRoomService.BeginRestore(sourceFixture); LocalSceneRoomService.CompleteRestore();
        Check(desk.ScoutingDossiers.Single(d => d.ArtistId == act.artistId).Observations.Last().Claim == audience.Claim,
            "acquired source snapshot survives source calendar pruning");
        var noCount = desk.PreviewScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.BillAudience);
        Check(noCount.BillAttendance == null && noCount.Claim.Contains("individual draw is unknown"), "missing retained bill never invents audience");
        sourceFixture.Bills.Add(new SceneBill { Id = "source-announced-bill", RoomId = room.Id, Day = sourceDay,
            Year = sourceDate.year, StartHour = room.OpenHour + 2, EndHour = room.CloseHour, Status = SceneBillStatus.Scheduled,
            Appearances = new() { new SceneAppearance { ArtistId = act.artistId, StartHour = room.OpenHour + 2, EndHour = room.OpenHour + 3,
                Status = SceneBillStatus.Scheduled, Set = new() { new SceneSetSong { Title = "Source fixture original", Original = true } } } } });
        // Remove other upcoming appearances for this act to isolate programme identity.
        foreach (var existingBill in sourceFixture.Bills.Where(b => b.Id != "source-announced-bill" && b.RoomId == room.Id))
            existingBill.Appearances.RemoveAll(a => a.ArtistId == act.artistId && a.Status == SceneBillStatus.Scheduled);
        LocalSceneRoomService.BeginRestore(sourceFixture); LocalSceneRoomService.CompleteRestore();
        var programme = desk.PreviewScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.AnnouncedMaterial);
        Check(programme.BillId == "source-announced-bill" && programme.Claim.Contains("Source fixture original (original)") &&
            programme.Claim.Contains("not evidence that you heard"), "programme is a scoped source claim, not heard material");
        var announcedDate = desk.PreviewScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.NextDate);
        sourceFixture.Bills.Single(b => b.Id == "source-announced-bill").Appearances[0].StartHour--;
        LocalSceneRoomService.BeginRestore(sourceFixture); LocalSceneRoomService.CompleteRestore();
        Check(!desk.AskScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.NextDate, announcedDate.QuoteId, out _) &&
            clock.CurrentHour == beforeConversation + 1, "same bill programme time revision invalidates a captured quote without cost");
        sourceFixture.Bills.Single(b => b.Id == "source-announced-bill").Status = SceneBillStatus.Cancelled;
        LocalSceneRoomService.BeginRestore(sourceFixture); LocalSceneRoomService.CompleteRestore();
        Check(!desk.AskScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.AnnouncedMaterial, programme.QuoteId, out _) &&
            clock.CurrentHour == beforeConversation + 1, "programme cancellation invalidates captured source quote without cost");
        clock.RestoreClock(sourceDate, room.CloseHour - 1, 30);
        Check(desk.PreviewScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.AnnouncedMaterial)?.Available == false,
            "subhour room closing blocks a full conversation");
        clock.RestoreClock(sourceDate, clock.HardStopHour);
        Check(!desk.AskScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.AnnouncedMaterial, null, out _),
            "source conversation respects player hard stop");
        LocalScenes.Configure(new[] { "--observe-scouting-experience" });
        Check(desk.PreviewScoutingSource(room.Id, act.artistId, ScoutingSourceQuestion.NextDate) == null,
            "observer mode exposes no actionable source quote");
        LocalScenes.Configure(Array.Empty<string>());
        LocalSceneRoomService.BeginRestore(sourceRooms); LocalSceneRoomService.CompleteRestore();
        Check(desk.RestoreState(sourcePlayer, out message), "restore source fixture player");
        clock.RestoreClock(sourceOriginalDate, sourceHour, sourceMinute);
        checks += ScoutingCompetitionChecks.Run(act.artistId);
        checks += ScoutingLossChecks.Run(act.artistId);
        // Controlled consent fixture on the same canonical act, after its actual observed set.
        var career = act.careerState; var manager = act.manager; var lifecycle = act.lifecycleStatus;
        act.labelId = null; act.careerState = CareerState.Unsigned; act.prospectMarketStatus = ProspectMarketStatus.Seeking;
        act.lifecycleStatus = ArtistLifecycleStatus.Active; act.manager = ManagerArchetype.Shark;
        p = new PlayerDesk.Prospect { Artist = act, SceneRoomId = room.Id, SceneBillId = bill.Id, Venue = room.Category, CityId = room.PlaceId };
        Check(!p.FollowedUp && desk.ApproachToSign(p, out message), "one actual listening can justify an approach: " + message);
        Check(p.Talk != null, "managed negotiation fixture");
        p.Talk.roundsPlayed = 1; p.Talk.patienceLeft = 1; p.Talk.stage = ContractTalkStage.Objection;
        p.Talk.log.Add("A saved counter on the term.");
        string negotiationState = Json(desk.CaptureState().ScoutingKnowledge);
        var negotiatingSave = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        Check(desk.RestoreState(negotiatingSave, out message) && Json(desk.CaptureState().ScoutingKnowledge) == negotiationState, "pending negotiation, asks, counters and patience roundtrip");
        Check(desk.OpenScoutingDossier(act.artistId, out message), "reopen negotiation dossier");
        var reopened = desk.Slate.Single(x => x.Artist.artistId == act.artistId);
        Check(desk.ApproachToSign(reopened, out message) && reopened.Talk.roundsPlayed == 1 && reopened.Talk.patienceLeft == 1, "reopening does not reset negotiation progress");
        reopened.CooldownUntil = clock.CurrentDate.AddDays(10);
        Check(!desk.ApproachToSign(reopened, out _), "reopening does not bypass refused contact cooldown");
        Check(!desk.OfferContract(reopened, 0, .01f, 1, 0, false, true, out _), "direct signing cannot bypass cooldown");
        act.labelId = owner; act.careerState = career; act.manager = manager; act.lifecycleStatus = lifecycle; act.prospectMarketStatus = oldStatus;
        // Legacy impressions are not fabricated visits.
        var legacy = desk.CaptureState(); legacy.ScoutingKnowledge = null;
        legacy.Notebook.Add(new ProspectNotebookSaveData { Artist = act, Year = bill.Year, Month = 1, Day = 1, Note = "old note" });
        Check(desk.RestoreState(legacy, out message), "legacy migration: " + message);
        Check(desk.ScoutingDossiers.Single().Observations.Single().Kind == ScoutingEvidenceKind.LegacyImpression && !desk.HasScoutingEvidence(act.artistId), "legacy provenance remains unknown, no invented performance");
        // Replay the same world without attending, then compare all scene economics/work.
        LocalSceneRoomService.BeginRestore(world.SceneRooms); LocalSceneRoomService.CompleteRestore();
        LocalSceneRoomService.Advance(LocalSceneRoomService.Date(bill.Day), slot.EndHour);
        Check(Json(WorldStateService.Capture().SceneRooms) == Json(observedWorld), "visited versus unvisited performance/economy equality");
        GD.Print($"SCOUTING_EXPERIENCE_CHECK_PASS checks={checks}");
    }
}
