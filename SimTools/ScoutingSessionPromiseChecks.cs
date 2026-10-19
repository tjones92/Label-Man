using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingSessionPromiseChecks {
    /// <summary>A real heard act, made eligible and managed so that its approach opens the negotiation scene.</summary>
    public static PlayerDesk.Prospect PrepareNegotiation(PlayerDesk desk) {
        var clock = TimeManager.Instance;
        clock.RestoreClock(GameDate.StartDate, 9);
        var next = desk.ScoutingItinerary().First();
        if (!desk.WaitForSceneAppearance(next.BillId, next.EventId, out string message) ||
            !desk.ListenSceneAppearance(next.BillId, next.EventId, out message)) throw new InvalidOperationException(message);
        var act = ArtistManager.Instance.GetArtist(next.ArtistId);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.prospectMarketStatus = ProspectMarketStatus.Seeking;
        act.lifecycleStatus = ArtistLifecycleStatus.Active; act.isActive = true; act.manager = ManagerArchetype.Visionary;
        desk.Label.cashReserves = 100000;
        clock.RestoreClock(clock.CurrentDate, 9);
        if (!desk.OpenScoutingDossier(act.artistId, out message)) throw new InvalidOperationException(message);
        var prospect = desk.Slate.Single(p => p.Artist == act);
        if (!desk.ApproachToSign(prospect, out message) || prospect.Talk == null) throw new InvalidOperationException("Need a negotiation: " + message);
        return prospect;
    }

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_SESSION_PROMISE_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Session Promise", "nashville", out string message), "found label: " + message);
        var prospect = PrepareNegotiation(desk); string id = prospect.Artist.artistId;
        var talk = prospect.Talk; var ask = talk.ask;
        int Minute() => clock.CurrentHour * 60 + clock.CurrentMinute;
        var savedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var savedPlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        void Restore(PlayerSaveData player, GameDate when, int h, int m, WorldSaveData world) {
            LocalScenes.Configure(Array.Empty<string>());
            WorldStateService.Apply(LocalSceneRoomService.Copy(world), when, SimulationSeedBootstrap.RequestedSeed);
            Check(desk.RestoreState(LocalSceneRoomService.Copy(player), out message), "restore: " + message);
            clock.RestoreClock(when, h, m);
            // The slate is not saved; reopening the dossier relinks the retained negotiation without redrawing it.
            if (desk.Slate.All(p => p.Artist.artistId != id)) desk.OpenScoutingDossier(id, out _);
            prospect = desk.Slate.FirstOrDefault(p => p.Artist.artistId == id);
            talk = prospect?.Talk;
        }
        void Reset() => Restore(savedPlayer, date, hour, minute, savedWorld);
        // An initial table opens the objection where the promise is offered, without signing.
        bool TableAt(float advance) => desk.TableOffer(talk, advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation,
            ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message);
        Reset();
        Check(talk != null && talk.stage == ContractTalkStage.Tabling, "restored scouting negotiation is reachable");
        Check(TableAt(PlayerDesk.MinimumAdvanceForAcceptance(talk) - 5) && talk.stage == ContractTalkStage.Objection, "lowball opens an objection: " + message);
        string objectionPlayer = Json(desk.CaptureState()), objectionWorld = Json(WorldStateService.Capture());
        savedPlayer = desk.CaptureState(); date = clock.CurrentDate; hour = clock.CurrentHour; minute = clock.CurrentMinute;
        savedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture());

        // 1. The promise is a term on the table: free to propose, no RNG, no world change.
        float cash = desk.Label.cashReserves;
        GameRandom.Seed(311); uint draw = GameRandom.Randi(); GameRandom.Seed(311);
        Check(desk.PlayNegotiationCounter(talk, ContractCounter.Promise, out message) && talk.sessionPromised && talk.stage == ContractTalkStage.Tabling,
            "promise joins the package: " + message);
        Check(GameRandom.Randi() == draw && Json(WorldStateService.Capture()) == objectionWorld && desk.Label.cashReserves == cash &&
            Minute() == hour * 60 + minute && desk.ContractCommitments.Count == 0, "proposing spends nothing, draws nothing and commits nothing");
        Check(desk.WithdrawSessionPromise(talk, out message) && !talk.sessionPromised, "promise can be withdrawn before agreement");
        Check(!desk.WithdrawSessionPromise(talk, out message), "nothing left to withdraw");
        Check(desk.AddSessionPromise(talk, out message) && talk.sessionPromised && talk.stage == ContractTalkStage.Tabling &&
            !desk.AddSessionPromise(talk, out message), "the tabling form can include the promise once");
        Check(desk.WithdrawSessionPromise(talk, out message), "withdraw again");

        // 2. The promise is scored: the same package fails without it and signs with it.
        float credit = PlayerDesk.SessionPromiseValue(talk.Artist);
        ContractTermSheet? found = null;
        float minAdvance = PlayerDesk.MinimumAdvanceForAcceptance(talk);
        foreach (bool control in new[] { ask.ArtistCreativeControl, false }.Distinct())
        for (int singles = ask.SinglesObligation; singles <= ask.SinglesObligation + 8 && found == null; singles += 2)
        for (int term = ask.TermYears; term <= 7 && found == null; term++)
        for (int adv = 0; adv <= 10 && found == null; adv++)
        for (int step = 0; step <= 60 && found == null; step++) {
            float advance = Mathf.Ceil(Mathf.Lerp(ask.Advance, minAdvance, adv / 10f));
            float royalty = Mathf.Round(ask.RoyaltyRate * (1f - step / 75f) * 10000f) / 10000f;
            if (royalty < PlayerDesk.PlayerRoyaltyFloor || advance < minAdvance) break;
            var sheet = new ContractTermSheet(advance, royalty, term, singles, ask.LabelOwnsPublishing, control,
                ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);
            // The contract outlook is part of every package's score, promised or not.
            float value = PlayerDesk.PackageValueOf(talk, sheet) + PlayerDesk.OutlookCreditOf(talk);
            if (PlayerDesk.HardLineBroken(talk, sheet) == null && value < talk.reservation - 0.002f && value + credit > talk.reservation + 0.002f) found = sheet;
        }
        Check(found.HasValue, $"fixture needs a package between the two thresholds (reservation {talk.reservation:0.000}, credit {credit:0.000})");
        var between = found.Value;
        bool TableSheet(ContractTermSheet s) => desk.TableOffer(talk, s.Advance, s.RoyaltyRate, s.TermYears, s.SinglesObligation,
            s.LabelOwnsPublishing, s.ArtistCreativeControl, out message);
        Reset(); Check(desk.PlayNegotiationCounter(talk, ContractCounter.SweetenAxis, out message), "back to tabling");
        Check(TableSheet(between) && talk.stage == ContractTalkStage.Objection && desk.ContractCommitments.Count == 0, "unpromised package does not clear");
        Reset(); Check(desk.PlayNegotiationCounter(talk, ContractCounter.Promise, out message), "promise");
        cash = desk.Label.cashReserves; int agreed = LocalSceneRoomService.Day(clock.CurrentDate); float quote = desk.SessionPromiseQuote();
        Check(TableSheet(between) && talk.stage == ContractTalkStage.Done && am.GetArtist(id).labelId == desk.Label.labelId, "promised package signs: " + message);
        var c = desk.ContractCommitments.Single();
        Check(c.Status == ContractCommitmentStatus.Open && c.ArtistId == id && c.AgreedDay == agreed && c.DueDay == agreed + PlayerDesk.SessionPromiseDays &&
            c.QuotedCost == quote && !c.Renewal && c.LabelId == desk.Label.labelId, "one open commitment with due date and quote");
        Check(desk.Label.cashReserves == cash - between.Advance, "signing pays the advance only; the session is paid when booked");
        Check(desk.OpenCommitmentFor(id)?.Id == c.Id, "roster card can read the open promise");
        var signedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var signedPlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var signedDate = clock.CurrentDate; int signedHour = clock.CurrentHour, signedMinute = clock.CurrentMinute;

        // 3. Affordability includes the promised session, checked before any time is spent.
        Reset(); Check(desk.PlayNegotiationCounter(talk, ContractCounter.Promise, out message), "promise");
        float reserve = desk.Label.GetMonthlyOverhead() * 2f;
        desk.Label.cashReserves = between.Advance + reserve + quote * 0.5f;
        Check(desk.Label.CanAffordToSign(between.Advance) && !desk.Label.CanAffordToSign(between.Advance + quote), "fixture cash covers advance but not session");
        Check(!TableSheet(between) && message.Contains("promised session") && Minute() == hour * 60 + minute && desk.ContractCommitments.Count == 0 &&
            talk.patienceLeft == savedPlayer.ScoutingKnowledge.Negotiations.Single(n => n.ArtistId == id).Talk.patienceLeft,
            "unaffordable promise blocks before time or patience");
        Check(!desk.PlayNegotiationCounter(talk, ContractCounter.SweetenAxis, out message), "still tabling");

        // 4. Cash that leaves during the meeting voids the close; time stays spent.
        Reset(); Check(desk.PlayNegotiationCounter(talk, ContractCounter.Promise, out message), "promise");
        Action<int> drain = _ => desk.Label.cashReserves = between.Advance + reserve + quote * 0.5f;
        clock.OnHourChanged += drain;
        try { Check(!TableSheet(between), "drained cash blocks the close"); } finally { clock.OnHourChanged -= drain; }
        Check(am.GetArtist(id).labelId != desk.Label.labelId && desk.ContractCommitments.Count == 0 &&
            Minute() == hour * 60 + minute + PlayerDesk.NegotiationRoundHours * 60 && talk.sessionPromised,
            "no contract, no commitment, meeting time consumed, promise still on the table");

        // 5. Real compressed save/load keeps a promise on the table and a signed commitment.
        Reset(); Check(desk.PlayNegotiationCounter(talk, ContractCounter.Promise, out message), "promise");
        string slot = "Promise " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "compressed save/load: " + message);
            Check(desk.OpenScoutingDossier(id, out message), "reopen dossier after load: " + message);
            var reloaded = desk.Slate.First(p => p.Artist.artistId == id);
            Check(reloaded.Talk?.sessionPromised == true && reloaded.Talk.stage == ContractTalkStage.Tabling, "tabled promise survives reload");
            Restore(signedPlayer, signedDate, signedHour, signedMinute, signedWorld);
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "compressed save/load signed: " + message);
            Check(Json(desk.ContractCommitments) == Json(signedPlayer.ContractCommitments), "open commitment survives reload");
        } finally { SaveGameService.Delete(slot); }

        // 6. Kept: a real paid session inside the window.
        Restore(signedPlayer, signedDate, signedHour, signedMinute, signedWorld);
        var act = am.GetArtist(id); int kept = c.DueDay - 3;
        clock.RestoreClock(LocalSceneRoomService.Date(kept), 9);
        var material = desk.MaterialOptionsFor(act).Take(1).ToList();
        cash = desk.Label.cashReserves; int events = act.careerEvents.Count;
        Check(material.Count == 1 && desk.StartSession(act, material, PlayerDesk.StudioTier.Budget, PlayerDesk.MinSessionHours, out message, overrideRefusal: true),
            "real session books: " + message);
        var keptRecord = desk.ContractCommitments.Single();
        Check(keptRecord.Status == ContractCommitmentStatus.Kept && keptRecord.ResolvedDay == kept && desk.Label.cashReserves < cash, "paid session keeps the promise");
        desk.ScrapSession();
        clock.RestoreClock(LocalSceneRoomService.Date(c.DueDay), 22); clock.EndDay();
        Check(desk.ContractCommitments.Single().Status == ContractCommitmentStatus.Kept && act.careerEvents.Count == events, "a kept promise is never later missed");

        // 7. Missed: the ordinary dawn after the due date records the miss once.
        Restore(signedPlayer, signedDate, signedHour, signedMinute, signedWorld); act = am.GetArtist(id);
        events = act.careerEvents.Count;
        clock.RestoreClock(LocalSceneRoomService.Date(c.DueDay), 22); clock.EndDay();
        var missed = desk.ContractCommitments.Single();
        Check(missed.Status == ContractCommitmentStatus.Missed && missed.ResolvedDay == c.DueDay + 1 && !missed.RemedyApplied &&
            act.careerEvents.Count == events + 1 && act.careerEvents.Last().Contains("never booked"), "miss recorded on the act's history");
        clock.EndDay();
        Check(act.careerEvents.Count == events + 1 && desk.ContractCommitments.Single().Status == ContractCommitmentStatus.Missed, "miss is not repeated");
        clock.RestoreClock(clock.CurrentDate, 9);
        material = desk.MaterialOptionsFor(act).Take(1).ToList();
        Check(desk.StartSession(act, material, PlayerDesk.StudioTier.Budget, PlayerDesk.MinSessionHours, out message, overrideRefusal: true) &&
            desk.ContractCommitments.Single().Status == ContractCommitmentStatus.Missed, "a late session cannot repair the record");
        desk.ScrapSession();

        // Remedy: the next renewal ask carries the miss, and the completed renewal answers it.
        act.contractSinglesObligation = 2; act.contractReleases = 2;
        clock.RestoreClock(clock.CurrentDate, 9);
        Check(desk.ApproachRenewal(act, out message) && desk.PendingRenewal.Ask.DemandSummary.Contains("never booked"), "renewal ask names the miss: " + message);
        var renewalAsk = desk.PendingRenewal.Ask; var rtalk = desk.PendingRenewal.Talk;
        Check(rtalk != null, "managed renewal negotiates");
        Check(desk.TableOffer(rtalk, renewalAsk.Advance * .5f - 5, renewalAsk.RoyaltyRate, renewalAsk.TermYears, renewalAsk.SinglesObligation,
            renewalAsk.LabelOwnsPublishing, renewalAsk.ArtistCreativeControl, out message) && rtalk.stage == ContractTalkStage.Objection, "renewal objection");
        Check(desk.PlayNegotiationCounter(rtalk, ContractCounter.Promise, out message) && rtalk.sessionPromised, "a fresh promise can be offered after a miss");
        Check(desk.TableOffer(rtalk, renewalAsk.Advance, renewalAsk.RoyaltyRate, renewalAsk.TermYears, renewalAsk.SinglesObligation,
            renewalAsk.LabelOwnsPublishing, renewalAsk.ArtistCreativeControl, out message) && desk.PendingRenewal == null, "renewal at the ask completes: " + message);
        var after = desk.ContractCommitments;
        Check(after.Count == 2 && after.Single(x => x.Id == c.Id).RemedyApplied && after.Single(x => x.Id != c.Id) is { Renewal: true, Status: ContractCommitmentStatus.Open },
            "miss answered once; the renewal's promise is its own open commitment");

        // 8. Void: an act that leaves the roster releases the promise without a broken-promise history.
        Restore(signedPlayer, signedDate, signedHour, signedMinute, signedWorld); act = am.GetArtist(id);
        Check(am.DropArtist(act, clock.CurrentDate.year, desk.Label, ArtistDropReason.ContractExpired), "drop act");
        events = act.careerEvents.Count;
        clock.RestoreClock(clock.CurrentDate, 22); clock.EndDay();
        Check(desk.ContractCommitments.Single().Status == ContractCommitmentStatus.Void && !act.careerEvents.Skip(events).Any(e => e.Contains("never booked")),
            "departure voids the promise");

        // 9. Malformed saves are rejected before mutation; a legacy save invents nothing.
        Restore(signedPlayer, signedDate, signedHour, signedMinute, signedWorld);
        foreach (var corrupt in new Action<PlayerSaveData>[] {
            s => s.ContractCommitments[0].LabelId = "other-label", s => s.ContractCommitments.Add(LocalSceneRoomService.Copy(s.ContractCommitments[0])),
            s => s.ContractCommitments[0].ResolvedDay = 3, s => s.ContractCommitments[0].QuotedCost = float.NaN,
            s => s.ContractCommitments[0].DueDay = -1, s => s.ContractCommitments[0].RemedyApplied = true }) {
            var invalid = LocalSceneRoomService.Copy(signedPlayer); corrupt(invalid);
            string before = Json(desk.CaptureState());
            Check(!desk.RestoreState(invalid, out message) && Json(desk.CaptureState()) == before, "malformed commitment rejected before mutation");
        }
        var legacy = LocalSceneRoomService.Copy(signedPlayer); legacy.ContractCommitments = null;
        Check(desk.RestoreState(legacy, out message) && desk.ContractCommitments.Count == 0, "legacy save has no invented promise");
        Restore(signedPlayer, signedDate, signedHour, signedMinute, signedWorld);
        foreach (string flag in new[] { "--disable-scouting-experience", "--observe-scouting-experience" }) {
            LocalScenes.Configure(new[] { flag });
            Check(Json(desk.CaptureState().ContractCommitments) == Json(signedPlayer.ContractCommitments), "feature mode keeps contract commitments");
        }
        LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCOUTING_SESSION_PROMISE_PASS checks={checks}");
    }
}
