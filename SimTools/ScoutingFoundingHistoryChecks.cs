using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;

public static class ScoutingFoundingHistoryChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_FOUNDING_HISTORY_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Founding History", "nashville", out string message), "found label: " + message);
        Check(desk.FoundingAct == null && desk.FoundingActSummary() == null && desk.CaptureState().ScoutingKnowledge.FoundingHistoryStarted == false, "new history starts before any signature");
        var plan = desk.ScoutingItinerary().First();
        Check(desk.WaitForSceneAppearance(plan.BillId, plan.EventId, out message) && desk.ListenSceneAppearance(plan.BillId, plan.EventId, out message), "hear actual set: " + message);
        var act = am.GetArtist(plan.ArtistId);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
        act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
        clock.RestoreClock(clock.CurrentDate, 10, 15); desk.Label.cashReserves = 100000;
        var preWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var prePlayer = LocalSceneRoomService.Copy(desk.CaptureState()); var date = clock.CurrentDate;
        void Reset() {
            WorldStateService.Apply(LocalSceneRoomService.Copy(preWorld), date, SimulationSeedBootstrap.RequestedSeed);
            Check(desk.RestoreState(LocalSceneRoomService.Copy(prePlayer), out message), "restore fresh-label baseline");
            clock.RestoreClock(date, 10, 15); act = am.GetArtist(plan.ArtistId);
            Check(desk.OpenScoutingDossier(act.artistId, out message), "open retained evidence");
        }
        PlayerDesk.Prospect Prospect() => desk.Slate.Single(p => p.Artist.artistId == act.artistId);
        void SignEasy() {
            var p = Prospect(); Check(desk.ApproachToSign(p, out message), "approach: " + message);
            p.Posture = NegotiationPosture.Pushover; p.Talk = null; var terms = p.Baseline;
            Check(desk.OfferContract(p, terms.Advance, terms.RoyaltyRate, terms.TermYears, terms.SinglesObligation,
                terms.LabelOwnsPublishing, terms.ArtistCreativeControl, out message), "canonical easy signing: " + message);
        }
        var p = Prospect(); Check(desk.ApproachToSign(p, out message), "initial approach");
        p.Posture = NegotiationPosture.Pushover; p.Talk = null; var ask = p.Baseline;
        float funds = desk.Label.cashReserves; desk.Label.cashReserves = 0;
        Check(!desk.OfferContract(p, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message)
            && desk.FoundingAct == null && desk.CaptureState().ScoutingKnowledge.FoundingHistoryStarted == false, "blocked signing does not consume first signature");
        desk.Label.cashReserves = funds; SignEasy();
        var memory = desk.FoundingAct;
        var observation = desk.ScoutingDossiers.Single(d => d.ArtistId == act.artistId).Observations.Single(o => o.Kind == ScoutingEvidenceKind.Performance);
        Check(memory.ArtistId == act.artistId && memory.LabelId == desk.Label.labelId && memory.Terms.Advance == ask.Advance && memory.Minute == clock.CurrentHour * 60 + clock.CurrentMinute,
            "first signature retains identity, actual terms and completion minute");
        Check(memory.ObservationId == observation.Id && memory.PersonIds.SequenceEqual(observation.PersonIds) && memory.Context.Contains(observation.RoomName) && !memory.Context.Contains(observation.EventId), "context retains acquired coverage and stable provenance without raw event prose");
        string history = Json(memory), summary = desk.FoundingActSummary();
        Check(summary.Contains("publishing") && summary.Contains("royalty") && !summary.Contains("reservation") && !summary.Contains("patience"), "summary shows stated terms only");
        memory.PersonIds.Clear(); memory.ArtistName = "changed";
        Check(Json(desk.FoundingAct) == history, "public memory is detached");
        var capturedMemory = desk.CaptureState().ScoutingKnowledge.FoundingAct;
        capturedMemory.PersonIds.Clear();
        Check(Json(desk.FoundingAct) == history, "save capture detaches retained people");
        string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
        GD.Seed(934); uint draw = GD.Randi(); GD.Seed(934);
        desk.FoundingActSummary(); desk.ScoutingSummary(act.artistId);
        Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player && GD.Randi() == draw, "history views preserve world, player and RNG");
        desk.SetScoutingPriority(act.artistId, ScoutingPriority.Archived);
        Check(Json(desk.FoundingAct) == history, "archiving retains founding history");
        Check(am.DropArtist(act, date.year, desk.Label, ArtistDropReason.Voluntary) && Json(desk.FoundingAct) == history, "canonical departure retains first signature without rewriting its cause");
        act.stageName = "A later name";
        Check(desk.FoundingActSummary() == summary, "rename and departure do not rewrite original name or terms");
        act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.careerState = CareerState.Unsigned;
        clock.RestoreClock(date, 10, 15); desk.OpenScoutingDossier(act.artistId, out _); SignEasy();
        Check(Json(desk.FoundingAct) == history, "later deal on empty roster cannot replace founding signature");
        Check(am.DropArtist(act, date.year, desk.Label, ArtistDropReason.Voluntary), "depart again before history reload");
        string slot = "Founder " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "compressed save/load after departure: " + message);
            Check(Json(desk.FoundingAct) == history && desk.FoundingActSummary() == summary, "full reload preserves original history");
        } finally { SaveGameService.Delete(slot); }
        Reset(); act.manager = ManagerArchetype.Visionary;
        p = Prospect(); Check(desk.ApproachToSign(p, out message) && p.Talk != null, "managed approach"); ask = p.Baseline;
        Check(desk.TableOffer(p.Talk, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message)
            && act.labelId == desk.Label.labelId && desk.FoundingAct?.ArtistId == act.artistId, "managed close records first signature: " + message);
        Reset();
        var option = typeof(PlayerDesk).GetMethod("SignLeavingMember", BindingFlags.NonPublic | BindingFlags.Instance);
        var args = new object[] { act, null };
        Check(option != null && (bool)option.Invoke(desk, args) && desk.FoundingAct?.ArtistId == act.artistId, "existing leaving-member option shares history owner");
        var olderMemory = desk.CaptureState(); olderMemory.ScoutingKnowledge.FoundingHistoryStarted = null;
        olderMemory.ScoutingKnowledge.FoundingAct.Minute = null;
        olderMemory.ScoutingKnowledge.FoundingAct.ObservationId = null;
        olderMemory.ScoutingKnowledge.FoundingAct.PersonIds.Clear();
        Check(desk.RestoreState(olderMemory, out message) && desk.FoundingAct.ArtistId == act.artistId && desk.FoundingAct.Minute == null &&
            desk.CaptureState().ScoutingKnowledge.FoundingHistoryStarted == true, "existing legacy memory survives without inferred time or provenance");
        Reset(); var unheard = desk.CaptureState(); unheard.ScoutingKnowledge.Dossiers.Clear();
        Check(desk.RestoreState(unheard, out message), "restore history without listening evidence");
        args = new object[] { act, null };
        Check((bool)option.Invoke(desk, args) && desk.FoundingAct.ObservationId == null && desk.FoundingAct.PersonIds.Count == 0 &&
            desk.FoundingAct.Context.Contains("not recorded") && desk.FoundingActSummary() != null, "option path cannot fabricate listening and history exists without dossiers");
        Reset(); var legacy = desk.CaptureState(); legacy.ScoutingKnowledge.FoundingHistoryStarted = null;
        Check(desk.RestoreState(legacy, out message) && desk.FoundingAct == null && desk.FoundingActSummary().Contains("not recorded"), "legacy unknown history is explicit and not reconstructed");
        desk.OpenScoutingDossier(act.artistId, out _); SignEasy();
        Check(desk.FoundingAct == null, "legacy later signing cannot invent a founder");
        GD.Print($"SCOUTING_FOUNDING_HISTORY_PASS checks={checks}");
    }
}
