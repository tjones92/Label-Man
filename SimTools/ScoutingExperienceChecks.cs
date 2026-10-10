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
        string summary = Json(desk.ScoutingSummary(act.artistId));
        string recorded = Json(knowledge);
        Check(desk.OpenScoutingDossier(act.artistId, out message) && desk.OpenScoutingDossier(act.artistId, out message), "reopen recorded dossier");
        Check(Json(desk.CaptureState().ScoutingKnowledge) == recorded && Json(desk.ScoutingSummary(act.artistId)) == summary, "refresh/reopening changes no evidence");
        var afterCash = desk.Label.cashReserves; int afterHour = clock.CurrentHour;
        Check(!desk.ListenSceneAppearance(bill.Id, identity, out message) && desk.Label.cashReserves == afterCash && clock.CurrentHour == afterHour, "same occurrence adds no cost or evidence");
        var savedPlayer = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        Check(desk.RestoreState(savedPlayer, out message), "player evidence reload: " + message);
        Check(Json(desk.CaptureState().ScoutingKnowledge) == recorded, "player evidence byte roundtrip");
        Check(desk.SetScoutingPriority(act.artistId, ScoutingPriority.Archived), "archive action");
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
        LocalScenes.Configure(new[] { "--observe-scouting-experience" });
        Check(LocalScenes.ScoutingExperience && LocalScenes.ScoutingExperienceObserver && !desk.ListenSceneAppearance(bill.Id, identity, out _), "observer resolves shared events without acquisition");
        Check(!desk.AskScoutingBooker(room.Id, act.artistId, true, out _), "observer mode blocks new source claims");
        LocalScenes.Configure(Array.Empty<string>());
        Check(desk.AskScoutingBooker(room.Id, act.artistId, true, out message), "local source question: " + message);
        int conversationHour = clock.CurrentHour; float conversationCash = desk.Label.cashReserves;
        Check(!desk.AskScoutingBooker(room.Id, act.artistId, true, out _) && clock.CurrentHour == conversationHour && desk.Label.cashReserves == conversationCash, "identical claim cannot farm time or support");
        Check(desk.ScoutingDossiers.First(d => d.ArtistId == act.artistId).Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == 1, "a conversation is not a second performance");
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
