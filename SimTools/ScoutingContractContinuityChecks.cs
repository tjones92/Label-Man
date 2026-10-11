using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingContractContinuityChecks {
    public static SimulatedArtist PrepareRenewal(PlayerDesk desk, bool managed) {
        var clock = TimeManager.Instance;
        clock.RestoreClock(GameDate.StartDate, 9);
        var next = desk.ScoutingItinerary().First();
        if (!desk.WaitForSceneAppearance(next.BillId, next.EventId, out string message) ||
            !desk.ListenSceneAppearance(next.BillId, next.EventId, out message)) throw new InvalidOperationException(message);
        var act = ArtistManager.Instance.GetArtist(next.ArtistId);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.prospectMarketStatus = ProspectMarketStatus.Seeking;
        act.lifecycleStatus = ArtistLifecycleStatus.Active; act.isActive = true; act.manager = ManagerArchetype.None;
        desk.Label.cashReserves = 100000;
        clock.RestoreClock(clock.CurrentDate, 9);
        if (!desk.OpenScoutingDossier(act.artistId, out message)) throw new InvalidOperationException(message);
        var prospect = desk.Slate.Single(p => p.Artist == act);
        if (!desk.ApproachToSign(prospect, out message)) throw new InvalidOperationException(message);
        prospect.Posture = NegotiationPosture.Pushover; prospect.Talk = null;
        var ask = prospect.Baseline;
        if (!desk.OfferContract(prospect, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation,
                ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message)) throw new InvalidOperationException(message);
        // Deliveries, rather than an artificial clock jump, mature this controlled contract.
        act.contractSinglesObligation = 2; act.contractReleases = 2;
        act.manager = managed ? ManagerArchetype.Visionary : ManagerArchetype.None;
        if (!desk.ApproachRenewal(act, out message)) throw new InvalidOperationException(message);
        if (!managed) { desk.PendingRenewal.Posture = NegotiationPosture.Pushover; desk.PendingRenewal.Talk = null; }
        return act;
    }

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_CONTRACT_CONTINUITY_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Contract Continuity", "nashville", out string message), "found label: " + message);
        var act = PrepareRenewal(desk, true); string id = act.artistId;
        var talk = desk.PendingRenewal.Talk;
        Check(talk != null, "managed renewal uses existing acceptance owner");
        var ask = desk.PendingRenewal.Ask;
        Check(desk.TableOffer(talk, ask.Advance * .75f, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, true, false, out message) &&
            talk.stage == ContractTalkStage.Objection, "real hard-line objection: " + message);
        string renewal = Json(desk.CaptureState().RenewalDiscussion), world = Json(WorldStateService.Capture());
        GD.Seed(177); uint draw = GD.Randi(); GD.Seed(177);
        Check(desk.ApproachRenewal(act, out message) && ReferenceEquals(talk, desk.PendingRenewal.Talk) &&
            Json(desk.CaptureState().RenewalDiscussion) == renewal && GD.Randi() == draw && Json(WorldStateService.Capture()) == world,
            "reopening retains terms, rounds, objection and RNG");
        var savedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var savedPlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        void Reset() {
            LocalScenes.Configure(Array.Empty<string>());
            WorldStateService.Apply(LocalSceneRoomService.Copy(savedWorld), date, SimulationSeedBootstrap.RequestedSeed);
            Check(desk.RestoreState(LocalSceneRoomService.Copy(savedPlayer), out message), "restore live discussion: " + message);
            clock.RestoreClock(date, hour, minute); act = am.GetArtist(id); talk = desk.PendingRenewal.Talk; ask = desk.PendingRenewal.Ask;
        }
        var detached = desk.CaptureState().RenewalDiscussion;
        detached.Log.Clear(); detached.Talk.weights.Clear();
        Check(Json(desk.CaptureState().RenewalDiscussion) == renewal, "capture detaches all mutable discussion collections");
        Reset();
        var input = LocalSceneRoomService.Copy(savedPlayer);
        Check(desk.RestoreState(input, out message), "restore input accepted");
        input.RenewalDiscussion.Log.Clear(); input.RenewalDiscussion.Talk.weights.Clear();
        Check(Json(desk.CaptureState().RenewalDiscussion) == renewal && ReferenceEquals(desk.PendingRenewal.Artist, am.GetArtist(id)), "restore detaches input and binds canonical act only");
        string slot = "Renewal " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "actual compressed renewal save/load: " + message);
            Check(Json(desk.CaptureState().RenewalDiscussion) == renewal && ReferenceEquals(desk.PendingRenewal.Talk.renewalArtist, am.GetArtist(id)), "compressed reload retains ask, draft, patience, rounds, log and binding");
        } finally { SaveGameService.Delete(slot); }
        Reset();
        Check(desk.PlayNegotiationCounter(talk, ContractCounter.Promise, out message) && talk.sessionPromised &&
            desk.CaptureState().RenewalDiscussion.Talk.sessionPromised, "a session promise is part of the saved renewal discussion");
        Reset(); var oldTalk = talk;
        Check(desk.RestoreState(LocalSceneRoomService.Copy(savedPlayer), out message), "replace live state from save");
        int now = clock.CurrentHour * 60 + clock.CurrentMinute;
        Check(!desk.PlayNegotiationCounter(oldTalk, ContractCounter.HoldFirm, out message) && clock.CurrentHour * 60 + clock.CurrentMinute == now,
            "old talk object cannot spend or commit after reload");
        Reset();
        var other = am.GetAllArtists().First(a => a != act && a.lifecycleStatus == ArtistLifecycleStatus.Active && a.isActive);
        var detachedAct = LocalSceneRoomService.Copy(act);
        Check(!desk.ApproachRenewal(detachedAct, out message) && Json(desk.CaptureState().RenewalDiscussion) == renewal, "detached act cannot replace current renewal");
        foreach (var corrupt in new Action<RenewalDiscussionSaveData>[] {
            s => s.LabelId = "other-label", s => s.Talk.patienceLeft = -1, s => s.Talk.weights.Clear(),
            s => s.OriginalContract = null, s => s.Talk.renewalArtist = act }) {
            var invalid = LocalSceneRoomService.Copy(savedPlayer); corrupt(invalid.RenewalDiscussion);
            string playerBefore = Json(desk.CaptureState());
            Check(!desk.RestoreState(invalid, out message) && Json(desk.CaptureState()) == playerBefore, "malformed discussion rejected before player mutation");
        }
        foreach (string flag in new[] { "--disable-scouting-experience", "--observe-scouting-experience" }) {
            LocalScenes.Configure(new[] { flag });
            Check(Json(desk.CaptureState().RenewalDiscussion) == renewal, "feature mode retains existing contract discussion");
        }
        Reset(); var legacy = LocalSceneRoomService.Copy(savedPlayer); legacy.RenewalDiscussion = null;
        Check(desk.RestoreState(legacy, out message) && desk.PendingRenewal == null, "legacy missing discussion creates no invented asks or rounds");
        Reset(); var originalTerms = RenewalContractIdentity.From(act); float cash = desk.Label.cashReserves;
        act.labelId = other.labelId ?? "another-label"; desk.Label.roster.Remove(act);
        Check(!desk.PlayNegotiationCounter(talk, ContractCounter.HoldFirm, out message) && desk.Label.cashReserves == cash &&
            RenewalContractIdentity.From(act) == originalTerms && clock.CurrentHour * 60 + clock.CurrentMinute == hour * 60 + minute,
            "lost ownership blocks hold before time or contract mutation");
        Check(desk.WalkFromTalk(talk, out message) && act.labelId != desk.Label.labelId && RenewalContractIdentity.From(act) == originalTerms, "stale voluntary dismissal cannot reclaim or drop rival act");
        Reset(); act.contractExpiresWeek++;
        Check(!desk.PlayNegotiationCounter(talk, ContractCounter.HoldFirm, out message) && desk.RenewalDiscussionUnavailable()?.Contains("changed") == true,
            "changed contract cannot be replaced by old discussion even if still delivery-matured");
        Check(desk.StepBackFromRenewal(out message) && desk.PendingRenewal == null, "changed-contract discussion can be dismissed without contract consequences");
        Reset(); desk.Label.cashReserves = 0;
        Check(!desk.PlayNegotiationCounter(talk, ContractCounter.HoldFirm, out message) && talk.patienceLeft == savedPlayer.RenewalDiscussion.Talk.patienceLeft &&
            clock.CurrentHour * 60 + clock.CurrentMinute == hour * 60 + minute, "unaffordable hold costs no time or patience");
        Reset();
        // Use the ordinary clock callback to isolate a real resource change during a meeting.
        var beforeContract = RenewalContractIdentity.From(act); int beforeEvents = act.careerEvents.Count;
        Check(desk.PlayNegotiationCounter(talk, ContractCounter.SweetenAxis, out message), "return to tabling");
        Action<int> consumeCash = _ => desk.Label.cashReserves = 0;
        clock.OnHourChanged += consumeCash;
        try { Check(!desk.TableOffer(talk, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation,
            ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message), "cash changes during accepted-package meeting block commit"); }
        finally { clock.OnHourChanged -= consumeCash; }
        Check(RenewalContractIdentity.From(act) == beforeContract && act.careerEvents.Count == beforeEvents && desk.Label.cashReserves == 0 &&
            clock.CurrentHour * 60 + clock.CurrentMinute == hour * 60 + minute + PlayerDesk.NegotiationRoundHours * 60 &&
            talk.stage == ContractTalkStage.Tabling && desk.PendingRenewal != null, "consumed time retained, no advance or contract mutation, editable terms survive failed close");
        Reset(); Check(desk.PlayNegotiationCounter(talk, ContractCounter.SweetenAxis, out message), "table successful renewal");
        cash = desk.Label.cashReserves;
        Check(desk.TableOffer(talk, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message), "managed renewal succeeds through existing evaluator");
        Check(desk.PendingRenewal == null && talk.stage == ContractTalkStage.Done && desk.Label.cashReserves == cash - ask.Advance && act.contractReleases == 0,
            "successful renewal pays once and consumes existing discussion");
        Check(!desk.TableOffer(talk, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message), "completed renewal cannot replay");
        Reset(); desk.PendingRenewal.Posture = NegotiationPosture.Pushover; desk.PendingRenewal.Talk = null;
        beforeContract = RenewalContractIdentity.From(act); beforeEvents = act.careerEvents.Count;
        clock.OnHourChanged += consumeCash;
        try { Check(!desk.RenewContract(act, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation,
            ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message), "easy renewal also rechecks cash after meeting"); }
        finally { clock.OnHourChanged -= consumeCash; }
        Check(RenewalContractIdentity.From(act) == beforeContract && act.careerEvents.Count == beforeEvents && desk.PendingRenewal?.Draft != null,
            "easy failed close preserves draft and old contract");
        Reset(); desk.PendingRenewal.Posture = NegotiationPosture.Pushover; desk.PendingRenewal.Talk = null;
        Check(!desk.RenewContract(act, float.NaN, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, true, false, out message), "nonfinite offers blocked");
        cash = desk.Label.cashReserves;
        Check(desk.RenewContract(act, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message) &&
            desk.Label.cashReserves == cash - ask.Advance, "easy renewal successful payment remains intact");
        Reset();
        GD.Print($"SCOUTING_CONTRACT_CONTINUITY_PASS checks={checks}");
    }
}
