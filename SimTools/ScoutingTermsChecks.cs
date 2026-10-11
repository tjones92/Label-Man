using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingTermsChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_TERMS_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Terms Checks", "nashville", out string message), "found label: " + message);
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate)
            .First(b => b.Status == SceneBillStatus.Scheduled && b.Appearances.Count > 0 && b.Appearances[0].EndHour <= clock.HardStopHour);
        var act = am.GetArtist(bill.Appearances[0].ArtistId);
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), bill.Appearances[0].StartHour);
        Check(desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, bill.Appearances[0]), out message), "real performance acquired: " + message);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
        act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
        // Controlled clock and funds isolate real contract responses from the opening's affordability.
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), 10, 15); desk.Label.cashReserves = 100000;
        var p = desk.Slate.Single(x => x.Artist.artistId == act.artistId);
        ScoutingObservation[] Statements() => desk.ScoutingDossiers.Single(d => d.ArtistId == act.artistId).Observations
            .Where(o => o.Kind == ScoutingEvidenceKind.BusinessStatement).ToArray();
        Check(Statements().Length == 0 && desk.ScoutingBusinessStatements(act.artistId).Single().Contains("no terms discussion"), "listening does not fabricate wishes");
        float cash = desk.Label.cashReserves;
        Check(desk.ApproachToSign(p, out message), "actual approach: " + message);
        p.Posture = NegotiationPosture.Pushover; p.Talk = null; // Exercise easy and managed owners separately.
        var opening = Statements().Single(); var b = p.Baseline;
        Check(opening.BusinessStatementKind == ScoutingBusinessStatementKind.OpeningAsk && opening.StatedTerms.Advance == b.Advance &&
            opening.StatedTerms.RoyaltyRate == b.RoyaltyRate && opening.StatedTerms.SinglesObligation == b.SinglesObligation, "opening snapshots exact public ask");
        Check(opening.SourceId == act.artistId && opening.Channel == "contract discussion" && opening.PlaceId == desk.CurrentCityId &&
            opening.OccurredDay == bill.Day && opening.AcquiredDay == bill.Day && opening.AcquiredMinute == 615, "source and actual statement time");
        Check(desk.Label.cashReserves == cash && clock.CurrentHour == 10 && clock.CurrentMinute == 15, "recording adds no approach cost");
        string knowledge = Json(desk.CaptureState().ScoutingKnowledge), world = Json(WorldStateService.Capture());
        GD.Seed(144); uint draw = GD.Randi(); GD.Seed(144);
        desk.ScoutingSummary(act.artistId); desk.ScoutingProvenance(act.artistId); desk.ScoutingBusinessStatements(act.artistId);
        Check(Json(desk.CaptureState().ScoutingKnowledge) == knowledge && Json(WorldStateService.Capture()) == world && GD.Randi() == draw, "previews preserve evidence, world and RNG");
        Check(desk.ApproachToSign(p, out _) && Json(desk.CaptureState().ScoutingKnowledge) == knowledge, "reopening generates no second ask");
        var openingSave = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        Check(desk.OpenScoutingDossier(act.artistId, out _) && Json(desk.CaptureState().ScoutingKnowledge) == knowledge, "pad reopening is not a conversation");
        Check(typeof(ScoutingStatedTerms).GetProperties().Length == 6, "public terms DTO excludes evaluator internals");
        Check(!desk.OfferContract(p, b.Advance * .25f, b.RoyaltyRate, b.TermYears, b.SinglesObligation,
            b.LabelOwnsPublishing, b.ArtistCreativeControl, out message) && p.PushoverCountered, "actual easy counter");
        var counter = Statements().Last();
        Check(counter.BusinessStatementKind == ScoutingBusinessStatementKind.Counter && counter.Claim == message &&
            counter.StatedTerms.Advance == p.Draft.Value.Advance && counter.BusinessDiscussionId == opening.BusinessDiscussionId,
            "counter is stated package, correlated to its opening");
        Check(counter.AcquiredMinute == 735 && clock.CurrentHour == 12 && desk.Label.cashReserves == cash, "counter retains completion time without extra cost");
        Check(desk.ScoutingProvenance(act.artistId).Any(l => l.Contains("Counter terms:") && l.Contains("royalty") && l.Contains("12:15")),
            "full counter package remains available with acquisition time in source history");
        Check(!desk.OfferContract(p, b.Advance * .25f, b.RoyaltyRate, b.TermYears, b.SinglesObligation,
            b.LabelOwnsPublishing, b.ArtistCreativeControl, out message), "second poor offer declines");
        Check(Statements().Last().BusinessStatementKind == ScoutingBusinessStatementKind.Declined && Statements().Last().Claim == message,
            "actual decline retained without invented motive");
        int count = Statements().Length;
        Check(!desk.ApproachToSign(p, out _) && Statements().Length == count && clock.CurrentHour == 14, "cooldown failure acquires no response or cost");
        var saved = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        string acquired = Json(saved.ScoutingKnowledge);
        Check(desk.RestoreState(saved, out _) && Json(desk.CaptureState().ScoutingKnowledge) == acquired, "public statements and negotiation identity roundtrip");
        if (OS.GetCmdlineUserArgs().Contains("--scouting-terms-save-check")) {
            string saveSlot = "Terms probe " + Guid.NewGuid().ToString("N");
            string id = act.artistId;
            try {
                Check(SaveGameService.Save(saveSlot, out message), "real compressed save: " + message);
                Check(SaveGameService.Load(saveSlot, out message) && Json(desk.CaptureState().ScoutingKnowledge) == acquired,
                    "real load retains public statements and discussion identity: " + message);
                act = am.GetArtist(id);
            } finally { SaveGameService.Delete(saveSlot); }
        }
        Check(desk.OpenScoutingDossier(act.artistId, out _), "reopen restored negotiation");
        p = desk.Slate.Single(x => x.Artist.artistId == act.artistId); p.CooldownUntil = null;
        act.manager = ManagerArchetype.Visionary; clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), 10, 15);
        Check(desk.ApproachToSign(p, out _) && p.Talk != null, "managed discussion opens");
        Check(Statements().Last().BusinessDiscussionId != opening.BusinessDiscussionId && desk.ScoutingDossiers.Single().BusinessDiscussionCount == 2,
            "distinct real discussions on one date have distinct identity");
        var talk = p.Talk; b = p.Baseline;
        Check(desk.TableOffer(talk, 0, b.RoyaltyRate, b.TermYears, b.SinglesObligation, true, false, out message), "actual rights objection");
        Check(Statements().Last().BusinessStatementKind == ScoutingBusinessStatementKind.Objection && Statements().Last().Claim == message &&
            Statements().Last().StatedTerms == null, "objection records words without inventing counter numbers");
        count = Statements().Length;
        Check(!desk.TableOffer(talk, 0, b.RoyaltyRate, b.TermYears, b.SinglesObligation, true, false, out _) && Statements().Length == count,
            "wrong-stage action adds no evidence");
        Check(desk.WalkFromTalk(talk, out message) && Statements().Last().BusinessStatementKind == ScoutingBusinessStatementKind.SteppedBack &&
            Statements().Last().SourceId == desk.Label.labelId, "voluntary withdrawal attributed to player");
        count = Statements().Length; desk.WalkFromTalk(talk, out _);
        Check(Statements().Length == count, "repeated withdrawal cannot farm evidence");
        act.manager = ManagerArchetype.None; clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), 10, 15);
        Check(desk.ApproachToSign(p, out _), "new discussion after withdrawal");
        p.Posture = NegotiationPosture.Pushover; p.Talk = null; b = p.Baseline;
        count = Statements().Length; desk.Label.cashReserves = 0;
        Check(!desk.OfferContract(p, b.Advance, b.RoyaltyRate, b.TermYears, b.SinglesObligation, b.LabelOwnsPublishing, b.ArtistCreativeControl, out _) &&
            Statements().Length == count && act.labelId == null, "unaffordable offer acquires no agreement or act response");
        desk.Label.cashReserves = cash;
        string history = Json(Statements());
        Check(desk.OfferContract(p, b.Advance, b.RoyaltyRate, b.TermYears, b.SinglesObligation, b.LabelOwnsPublishing, b.ArtistCreativeControl, out message),
            "actual canonical signing: " + message);
        Check(act.labelId == desk.Label.labelId && Statements().Last().BusinessStatementKind == ScoutingBusinessStatementKind.Agreement &&
            Statements().Last().StatedTerms.Advance == b.Advance, "only completed signing produces agreement");
        Check(Json(Statements().SkipLast(1).ToArray()) == history, "signed agreement does not overwrite earlier asks");
        desk.SetScoutingPriority(act.artistId, ScoutingPriority.Archived);
        Check(desk.ScoutingBusinessStatements(act.artistId).Any(l => l.Contains("deal completed")) &&
            desk.ScoutingBusinessStatements(act.artistId).Any(l => l.Contains("Opening ask:")) && desk.ScoutingBusinessStatements(act.artistId).Count == 3,
            "archived history remains concise and reviewable");
        Check(desk.ScoutingBusinessStatements(act.artistId).All(l => !l.Contains("reservation") && !l.Contains("patience") && !l.Contains("utility")),
            "ordinary business summary has no evaluator or patience disclosures");
        // Replay the opening snapshot only: off modes permit existing contract behavior but do not acquire statements.
        foreach (string flag in new[] { "--disable-scouting-experience", "--observe-scouting-experience" }) {
            act.labelId = null; act.careerState = CareerState.Unsigned; act.prospectMarketStatus = ProspectMarketStatus.Seeking;
            Check(desk.RestoreState(openingSave, out _) && desk.OpenScoutingDossier(act.artistId, out _), "restore for off-mode contract probe");
            p = desk.Slate.Single(x => x.Artist.artistId == act.artistId); p.Posture = NegotiationPosture.Pushover; p.Talk = null; b = p.Baseline;
            clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), 10, 15); LocalScenes.Configure(new[] { flag });
            string before = Json(Statements());
            desk.OfferContract(p, b.Advance * .25f, b.RoyaltyRate, b.TermYears, b.SinglesObligation, b.LabelOwnsPublishing, b.ArtistCreativeControl, out _);
            Check(p.PushoverCountered && Json(Statements()) == before, "off mode preserves history without new acquisition: " + flag);
            LocalScenes.Configure(Array.Empty<string>());
        }
        var legacy = JsonSerializer.Deserialize<PlayerSaveData>(Json(openingSave), SaveGameService.TestJsonOptions);
        legacy.ScoutingKnowledge.Dossiers.Single().Observations.RemoveAll(o => o.Kind == ScoutingEvidenceKind.BusinessStatement);
        legacy.ScoutingKnowledge.Dossiers.Single().BusinessDiscussionCount = 0;
        legacy.ScoutingKnowledge.Negotiations.Single().BusinessDiscussionId = null;
        Check(desk.RestoreState(legacy, out _) && desk.OpenScoutingDossier(act.artistId, out _) && Statements().Length == 0, "legacy pending ask has no fabricated statement");
        p = desk.Slate.Single(x => x.Artist.artistId == act.artistId); p.Posture = NegotiationPosture.Pushover; p.Talk = null; b = p.Baseline;
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), 10, 15);
        desk.OfferContract(p, b.Advance * .25f, b.RoyaltyRate, b.TermYears, b.SinglesObligation, b.LabelOwnsPublishing, b.ArtistCreativeControl, out _);
        Check(Statements().Length == 1 && Statements().Single().BusinessStatementKind == ScoutingBusinessStatementKind.Counter &&
            !string.IsNullOrEmpty(Statements().Single().BusinessDiscussionId), "legacy discussion acquires only its next real counter");
        string legacyDiscussion = p.BusinessDiscussionId;
        p.HasBaseline = false; p.PushoverCountered = false; clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), 10, 15);
        LocalScenes.Configure(new[] { "--disable-scouting-experience" });
        Check(desk.ApproachToSign(p, out _) && p.BusinessDiscussionId == null && Statements().Length == 1,
            "new off-mode discussion cannot borrow earlier acquired identity");
        LocalScenes.Configure(Array.Empty<string>());
        p.Posture = NegotiationPosture.Pushover; p.Talk = null; b = p.Baseline;
        desk.OfferContract(p, b.Advance * .25f, b.RoyaltyRate, b.TermYears, b.SinglesObligation, b.LabelOwnsPublishing, b.ArtistCreativeControl, out _);
        Check(Statements().Length == 2 && Statements().Last().BusinessDiscussionId != legacyDiscussion &&
            Statements().All(o => o.BusinessStatementKind == ScoutingBusinessStatementKind.Counter), "reenabled acquisition records new response without retroactive ask");
        GD.Print($"SCOUTING_TERMS_CHECK_PASS checks={checks}");
    }
}
