using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Phase 4, credible offers: a rival's real offer with a decision day, the distribution promise, relationship
/// credit (introductions, earlier dealings, word of mouth) and the first act's broken-promise consequences. Deterministic
/// fixtures on a real heard act; AI labels are only given room and cash so a rival exists.</summary>
public static class ScoutingCredibleOffersChecks {
    /// <summary>At week 0 every reachable AI label sits at its operating target. Give the reachable ones room, the act's genre
    /// and cash, so a real rival exists, and return the one that would come for the act: the best-paying, so its offer is
    /// real competition for a small label's paper.</summary>
    public static AILabel OpenRival(SimulatedArtist act) {
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        var reachable = CompetitorManager.Instance.GetOperatingLabels().Where(l => !l.isPlayerOwned && !l.IsSubsidiary &&
            SceneRecruitmentService.Explain(l, act, week).Eligible && l.CurrentRosterSize < l.maxRosterSize)
            .OrderBy(l => l.labelId, StringComparer.Ordinal).Take(5).ToList();
        foreach (var l in reachable) { l.operatingRosterTarget = l.CurrentRosterSize + 1; l.preferredGenres = new[] { act.primaryGenre }; l.cashReserves += 200000f; }
        var richest = reachable.OrderByDescending(l => l.CalculateManagerAdjustedAdvance(act)).ThenBy(l => l.labelId, StringComparer.Ordinal).FirstOrDefault();
        return richest == null ? null : RosterManager.Instance.FindRivalToPlayer(act, TimeManager.Instance.CurrentDate, richest.labelId);
    }

    /// <summary>Packages that clear the act's own reservation: the first that still loses to the pending rival offer, and
    /// the first that beats it. Found by search over the money and the gives.</summary>
    public static (ContractTermSheet? Losing, ContractTermSheet? Winning) RivalBoundTerms(PlayerDesk desk, ContractTalk talk, RivalOffer offer) {
        var ask = talk.ask;
        ContractTermSheet? losing = null, winning = null;
        float top = Mathf.Max(ask.Advance * 1.5f, offer.Terms.Advance * 1.2f);
        foreach (bool control in new[] { false, true })
        foreach (bool publishing in new[] { true, false })
        foreach (int term in new[] { ask.TermYears, 1 }.Distinct())
        for (int step = 0; step <= 40; step++) {
            float advance = Mathf.Ceil(Mathf.Lerp(PlayerDesk.MinimumAdvanceForAcceptance(talk), top, step / 40f) / 5f) * 5f;
            var s = new ContractTermSheet(advance, ask.RoyaltyRate, term, ask.SinglesObligation, publishing, control,
                ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);
            if (!desk.ClearsReservationForCheck(talk, s)) continue;
            if (!desk.BeatsRivalOfferOf(talk, s)) losing ??= s; else winning ??= s;
        }
        return (losing, winning);
    }

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_CREDIBLE_OFFERS_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        string[] args = OS.GetCmdlineUserArgs();
        string message;

        // 0. Flags: rival offers and the room prewarm are on by default, with explicit off controls.
        Check(LocalScenes.RivalOffers && LocalScenes.RoomPrewarm, "rival offers and the room prewarm are on by default");
        bool Rejects(params string[] extra) { try { LocalScenes.Configure(args.Concat(extra)); return false; } catch (ArgumentException) { return true; } }
        Check(Rejects("--enable-rival-offers", "--disable-rival-offers"), "enable and disable together are refused");
        LocalScenes.Configure(args.Append("--disable-rival-offers"));
        Check(!LocalScenes.RivalOffers && LocalScenes.ContractOutlook, "--disable-rival-offers turns offers off and leaves the outlook");
        LocalScenes.Configure(args);

        Check(desk.FoundLabel("Credible Offers", "nashville", out message), "found label: " + message);
        var prospect = ScoutingContractOutlookChecks.PrepareAct(desk); string id = prospect.Artist.artistId; var act = prospect.Artist;
        string Scene(SimulatedArtist a) => LocalScenePersistenceService.SceneIdFor(a?.geography?.basePlaceId);
        var savedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var savedPlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        int Minute() => clock.CurrentHour * 60 + clock.CurrentMinute;
        int Today() => LocalSceneRoomService.Day(clock.CurrentDate);
        void Restore(PlayerSaveData player, GameDate when, int h, int m, WorldSaveData world) {
            LocalScenes.Configure(args);
            WorldStateService.Apply(LocalSceneRoomService.Copy(world), when, SimulationSeedBootstrap.RequestedSeed);
            Check(desk.RestoreState(LocalSceneRoomService.Copy(player), out message), "restore: " + message);
            clock.RestoreClock(when, h, m);
            if (desk.Slate.All(p => p.Artist.artistId != id)) desk.OpenScoutingDossier(id, out _);
            prospect = desk.Slate.FirstOrDefault(p => p.Artist.artistId == id);
            act = am.GetArtist(id);
        }
        void Reset() => Restore(savedPlayer, date, hour, minute, savedWorld);

        // 1. A rival's offer: real terms from its own pricing, a decision day, no draws, no world writes.
        Reset();
        act.evolution.artisticAmbition = 0.95f; // talks terms with a new label, so there is a table to beat
        var bidder = OpenRival(act); // AI labels given room and cash; restored by the next Reset
        Check(bidder != null, "fixture needs a reachable rival");
        string worldBefore = Json(WorldStateService.Capture());
        GameRandom.Seed(931); uint draw = GameRandom.Randi(); GameRandom.Seed(931);
        var offer = desk.MakeRivalOfferForCheck(act, bidder);
        Check(GameRandom.Randi() == draw && Json(WorldStateService.Capture()) == worldBefore, "making an offer draws nothing and writes no world state");
        Check(offer.RivalLabelId == bidder.labelId && offer.Status == RivalOfferStatus.Pending && offer.DecisionDay == Today() + PlayerDesk.RivalOfferDecisionDays &&
            offer.Terms.Advance == Mathf.Ceil(bidder.CalculateManagerAdjustedAdvance(act) / 5f) * 5f && offer.Terms.TermYears is >= 1 and <= 7,
            "the offer is the rival's own pricing with a decision day");
        Check(Json(PlayerDesk.RivalOfferTerms(bidder, act, Today(), clock.CurrentDate.year)) == Json(offer.Terms), "the terms are deterministic");
        Check(offer.Disclosure == RivalOfferDisclosure.Rumored && desk.RivalOfferLine(id).Contains(bidder.labelName) && !desk.RivalOfferLine(id).Contains("$"),
            "before any talk the player hears who and when, not the terms");
        Check(desk.AddToNotebook(prospect, out message), "notebook: " + message);
        var entry = desk.Notebook.First(n => n.Artist.artistId == id);
        Check(!desk.CanHoldWithHandshake(entry, out string reason) && reason.Contains(bidder.labelName), "a handshake does not hold an act past another label's offer: " + reason);

        // 2. At the table: the act says what it holds, and talks terms.
        Check(desk.ApproachToSign(prospect, out message) && prospect.Talk != null && prospect.Posture != NegotiationPosture.Pushover, "an act holding an offer talks terms: " + message);
        var talk = prospect.Talk; var ask = talk.ask;
        var told = desk.PendingRivalOffer(id);
        Check(told.Disclosure == RivalOfferDisclosure.Disclosed && told.DisclosedDay == Today() && desk.RivalOfferLine(id).Contains("$"), "opening talks discloses the terms");
        Check(talk.outlookStated.Any(l => l.Contains(bidder.labelName) && l.Contains("$")) && talk.outlookStated.All(l => !l.Contains("No offer has been made")),
            "the outlook states the real offer and drops the belief");
        Check(PlayerDesk.PostureFor(act, desk.ReadContractOutlook(act, false)) != NegotiationPosture.Pushover, "any act with paper in hand talks terms");
        var opened = LocalSceneRoomService.Copy(desk.CaptureState()); var openedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture());
        var openedDate = clock.CurrentDate; int openedHour = clock.CurrentHour, openedMinute = clock.CurrentMinute;
        void Reopen() { Restore(opened, openedDate, openedHour, openedMinute, openedWorld); talk = prospect.Talk; bidder = CompetitorManager.Instance.GetLabel(bidder.labelId); }

        // 3. Terms the act would take on their own still lose to better paper; terms that beat it sign, and the rival is turned down.
        var (losing, winning) = RivalBoundTerms(desk, talk, told);
        GD.Print($"CREDIBLE_OFFERS table ask=${ask.Advance:N0} rival={PlayerDesk.RivalTermsText(told.Terms)} losing={(losing.HasValue ? "$" + losing.Value.Advance : "none")} winning={(winning.HasValue ? "$" + winning.Value.Advance : "none")}");
        Check(losing.HasValue && winning.HasValue, "fixture needs terms that clear the act's own reservation but lose to the rival, and terms that beat it");
        var nearAsk = new ContractTermSheet(Mathf.Ceil(ask.Advance * 1.5f), ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, true, false,
            ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);
        Check(told.Terms.Advance < ask.Advance * 5f || !desk.BeatsRivalOfferOf(talk, nearAsk), "money far above the act's ask still counts: half again their ask doesn't beat it");
        bool Table(ContractTermSheet s) => desk.TableOffer(talk, s.Advance, s.RoyaltyRate, s.TermYears, s.SinglesObligation, s.LabelOwnsPublishing, s.ArtistCreativeControl, out message);
        int patience = talk.patienceLeft;
        Check(Table(losing.Value) && talk.stage == ContractTalkStage.Objection && talk.log[0].Contains(bidder.labelName) && talk.capabilityObjection == null &&
            talk.patienceLeft == patience - 1 && string.IsNullOrEmpty(am.GetArtist(id).labelId) && desk.PendingRivalOffer(id) != null,
            "the objection names the rival's paper, costs a round and signs nothing: " + message);
        Reopen();
        Check(Table(winning.Value) && talk.stage == ContractTalkStage.Done && am.GetArtist(id).labelId == desk.Label.labelId, "paper that beats the rival's signs: " + message);
        Check(desk.RivalOffers.Single(o => o.ArtistId == id).Status == RivalOfferStatus.Declined && desk.PendingRivalOffer(id) == null, "the rival is turned down");
        Check(desk.ScoutingCompetitionSummary(id).Contains("turned down"), "the notebook records how the offer ended");

        // 4. The decision day: the act takes the rival's paper, exactly as offered, from that label only.
        Reopen();
        clock.RestoreClock(LocalSceneRoomService.Date(told.DecisionDay - 1), 6);
        desk.ResolveRivalOffersForCheck();
        Check(desk.PendingRivalOffer(id) != null && string.IsNullOrEmpty(act.labelId), "nothing happens before the decision day");
        clock.RestoreClock(LocalSceneRoomService.Date(told.DecisionDay), 6);
        GameRandom.Seed(932); draw = GameRandom.Randi(); GameRandom.Seed(932);
        desk.ResolveRivalOffersForCheck();
        Check(GameRandom.Randi() == draw, "the rival's signing draws no global RNG");
        act = am.GetArtist(id);
        Check(act.labelId == bidder.labelId && bidder.roster.Contains(act) && act.unrecoupedAdvance == told.Terms.Advance && act.royaltyRate == told.Terms.RoyaltyRate &&
            act.contractLength == told.Terms.TermYears && desk.RivalOffers.Single(o => o.ArtistId == id).Status == RivalOfferStatus.Accepted,
            "on the day, the act signs with the rival on exactly the offered terms");
        Check(!Table(winning.Value) && act.labelId == bidder.labelId, "the player's table is closed once they've signed elsewhere: " + message);

        // 5. A rival that can no longer take the act withdraws; an act that is gone lapses the offer.
        Reopen();
        bidder.cashReserves = 0f;
        clock.RestoreClock(LocalSceneRoomService.Date(told.DecisionDay), 6);
        desk.ResolveRivalOffersForCheck();
        Check(desk.RivalOffers.Single(o => o.ArtistId == id).Status == RivalOfferStatus.Withdrawn && string.IsNullOrEmpty(am.GetArtist(id).labelId),
            "an offer the rival can't honour falls through and the act stays unsigned");
        Reopen();
        act.lifecycleStatus = ArtistLifecycleStatus.Retired;
        desk.ResolveRivalOffersForCheck();
        Check(desk.RivalOffers.Single(o => o.ArtistId == id).Status == RivalOfferStatus.Lapsed, "an act that is gone lapses the offer");

        // 6. Save/load keeps a pending, disclosed offer; malformed offers are refused before mutation; older saves invent none.
        Reopen();
        string slot = "Credible " + Guid.NewGuid().ToString("N");
        try {
            string expected = Json(desk.RivalOffers);
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "compressed save/load: " + message);
            Check(Json(desk.RivalOffers) == expected && desk.RivalOfferLine(id) != null, "the offer survives reload unchanged");
        } finally { SaveGameService.Delete(slot); }
        foreach (var corrupt in new Action<PlayerSaveData>[] {
            s => s.RivalOffers[0].PlayerLabelId = "someone_else",
            s => s.RivalOffers[0].DecisionDay = s.RivalOffers[0].MadeDay - 1,
            s => s.RivalOffers[0].DisclosedDay = null,
            s => s.RivalOffers[0].Terms = new ContractTermSheet(float.NaN, 0.05f, 2, 3, true, false, 0f, ManagerArchetype.None, null, null),
            s => { var twin = LocalSceneRoomService.Copy(s.RivalOffers[0]); twin.Id += "|2"; s.RivalOffers.Add(twin); },
            s => s.RivalOffers[0].Status = (RivalOfferStatus)42 }) {
            var invalid = LocalSceneRoomService.Copy(opened); corrupt(invalid);
            string state = Json(desk.CaptureState());
            Check(!desk.RestoreState(invalid, out message) && Json(desk.CaptureState()) == state, "malformed offer rejected before mutation");
        }
        var older = LocalSceneRoomService.Copy(opened); older.RivalOffers = null;
        Check(desk.RestoreState(older, out message) && desk.RivalOffers.Count == 0, "an older save holds no offers and none are invented");

        // 7. The distribution promise: free to propose, scored, refused when the label already has a line.
        Reset();
        act.evolution.artisticAmbition = 0.95f; act.evolution.commercialPragmatism = 0.8f;
        Check(desk.ApproachToSign(prospect, out message) && prospect.Talk != null, "a talk without a rival: " + message);
        talk = prospect.Talk; ask = talk.ask;
        float cash = desk.Label.cashReserves; float baseCredit = desk.AcceptanceCreditForCheck(talk); int before = Minute();
        GameRandom.Seed(933); draw = GameRandom.Randi(); GameRandom.Seed(933); worldBefore = Json(WorldStateService.Capture());
        Check(desk.AddDistributionPromise(talk, out message) && talk.distributionPromised && !desk.AddDistributionPromise(talk, out _), "distribution joins the package once: " + message);
        Check(GameRandom.Randi() == draw && desk.Label.cashReserves == cash && Minute() == before && Json(WorldStateService.Capture()) == worldBefore && desk.ContractCommitments.Count == 0,
            "proposing distribution spends, draws and commits nothing");
        float credit = desk.AcceptanceCreditForCheck(talk) - baseCredit;
        Check(credit >= 0.02f && credit <= 0.08f, $"the promise is scored inside the session range ({credit:0.000})");
        Check(desk.WithdrawDistributionPromise(talk, out message) && !talk.distributionPromised, "it can be withdrawn");
        desk.Label.independentDistributionRegions.Add(desk.Label.homeRegion);
        Check(!desk.AddDistributionPromise(talk, out message) && message.Contains("already"), "a line the act can already see is not a promise: " + message);
        desk.Label.independentDistributionRegions.Remove(desk.Label.homeRegion);
        Check(desk.AddDistributionPromise(talk, out message), "promise again");
        int agreed = Today();
        var generous = new ContractTermSheet(ask.Advance * 1.5f, ask.RoyaltyRate, 1, ask.SinglesObligation, false, true,
            ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);
        Check(Table(generous) && talk.stage == ContractTalkStage.Done && am.GetArtist(id).labelId == desk.Label.labelId, "a generous package with the promise signs: " + message);
        var line = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.Distribution);
        Check(line.Status == ContractCommitmentStatus.Open && line.DueDay == agreed + PlayerDesk.PromotionReleaseDeadlineDays && line.QuotedCost == 0f && line.ReleaseDay == null,
            "one distribution commitment, due once there is a record");
        Check(desk.FoundingAct?.ArtistId == id, "the first signing is the first name on the label");
        desk.TrackRosterDeparturesForCheck(); // the dawn watch now knows the act is on the roster
        var signed = LocalSceneRoomService.Copy(desk.CaptureState()); var signedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture());
        var signedDate = clock.CurrentDate; int signedHour = clock.CurrentHour, signedMinute = clock.CurrentMinute;
        void Resign() => Restore(signed, signedDate, signedHour, signedMinute, signedWorld);

        // Two acts to hear the story: one from the founding act's own town, one from elsewhere who knows nobody.
        var neighbour = am.GetAllArtists().Where(a => a.artistId != id && string.IsNullOrEmpty(a.labelId) && a.lifecycleStatus == ArtistLifecycleStatus.Active &&
            Scene(a) == Scene(act) && a.members.Any(m => m.isActive)).OrderBy(a => a.artistId, StringComparer.Ordinal).First();
        bool Knows(SimulatedArtist x, SimulatedArtist y) => x.members.Any(m => y.members.Any(o => m.personId == o.personId || ContactNetworkService.Edge(m.personId, o.personId) != null));
        var stranger = am.GetAllArtists().Where(a => a.artistId != id && string.IsNullOrEmpty(a.labelId) && a.lifecycleStatus == ArtistLifecycleStatus.Active &&
            Scene(a) != null && Scene(a) != Scene(act) && a.members.Count(m => m.isActive) >= 2 && !Knows(a, act)).OrderBy(a => a.artistId, StringComparer.Ordinal).First();
        string neighbourId = neighbour.artistId, strangerId = stranger.artistId;
        ContractOutlook Read(string artistId) => desk.ReadContractOutlook(am.GetArtist(artistId), false);
        float baseNeighbour = Read(neighbourId).Relationship, baseStranger = Read(strangerId).Relationship;

        // 8. Kept: a record out and a wholesale line on it keep the promise -- and the first act's town hears it.
        int released = agreed + 10;
        ChartManager.Instance.ReleaseRecord(new Record { recordId = "credible_line_single", title = "Credible Line", artistId = id,
            artistName = act.stageName, labelId = desk.Label.labelId, isPlayerOwned = true, format = ReleaseFormat.Single,
            primaryGenre = act.primaryGenre, releaseDate = LocalSceneRoomService.Date(released) });
        clock.RestoreClock(LocalSceneRoomService.Date(released), 22); clock.EndDay();
        line = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.Distribution);
        Check(line.ReleaseDay == released && line.DueDay == released + PlayerDesk.DistributionPromiseDays && line.Status == ContractCommitmentStatus.Open,
            "the release starts the distribution clock; no line yet");
        desk.Label.independentDistributionRegions.Add(desk.Label.homeRegion);
        clock.RestoreClock(clock.CurrentDate, 22); clock.EndDay();
        line = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.Distribution);
        Check(line.Status == ContractCommitmentStatus.Kept && line.ResolvedDay == Today(), "a wholesale house carrying the record keeps the promise");
        var keptNeighbour = Read(neighbourId);
        Check(keptNeighbour.Stated.Any(l => l.Contains(act.stageName) && l.Contains("kept your word")) && keptNeighbour.Relationship > baseNeighbour,
            "the first act's own town hears the promise was kept");
        Check(Read(strangerId).Stated.All(l => !l.Contains(act.stageName)), "an act that knows nobody connected hears nothing");
        Check(desk.FoundingActSummary().Contains("kept the promise"), "the founding history records the kept promise");

        // 9. Broken: no record by the deadline. Their town hears it, a connected stranger hears it, an unconnected one doesn't.
        Resign();
        int events = act.careerEvents.Count;
        clock.RestoreClock(LocalSceneRoomService.Date(agreed + PlayerDesk.PromotionReleaseDeadlineDays), 22); clock.EndDay();
        line = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.Distribution);
        int brokenDay = Today();
        Check(line.Status == ContractCommitmentStatus.Missed && act.careerEvents.Skip(events).Count(e => e.Contains("never put out the record it promised to distribute")) == 1,
            "a record that never came out breaks the distribution promise once");
        var heard = Read(neighbourId);
        Check(heard.Stated.Any(l => l.Contains("first name on your label") && l.Contains("broke your word")) && heard.Relationship <= baseNeighbour - 0.025f, $"the founding act's town hears the broken promise ({baseNeighbour:0.000} -> {heard.Relationship:0.000})");
        Check(Read(strangerId).Stated.All(l => !l.Contains(act.stageName)) && Mathf.IsEqualApprox(Read(strangerId).Relationship, baseStranger), "no connection, no story");
        var strangerMember = am.GetArtist(strangerId).members.First(m => m.isActive);
        var actMember = act.members.First(m => m.isActive);
        ContactNetworkService.Link(strangerMember.personId, actMember.personId, ContactKind.Session, clock.CurrentDate.year);
        var connected = Read(strangerId);
        Check(connected.Stated.Any(l => l.Contains(strangerMember.FullName) && l.Contains("broke a promise")) && connected.Relationship < baseStranger - 0.015f,
            "somebody who knows the act's people hears it");
        act.contractSinglesObligation = 2; act.contractReleases = 2; clock.RestoreClock(clock.CurrentDate, 9);
        Check(desk.ApproachRenewal(act, out message) && desk.PendingRenewal.Ask.DemandSummary.Contains("distribution you promised"), "the renewal ask names it: " + message);
        desk.StepBackFromRenewal(out _);
        Check(desk.FoundingActSummary().Contains("broke a promise"), "the founding history records the broken promise");
        clock.RestoreClock(LocalSceneRoomService.Date(brokenDay + PlayerDesk.WordOfMouthDays + 1), 9);
        Check(Read(neighbourId).Stated.All(l => !l.Contains("first name on your label")), "the story is not told forever");

        // 10. The founding act leaves after the broken promise: recorded with it, and the story comes back up.
        clock.RestoreClock(LocalSceneRoomService.Date(brokenDay), 9);
        desk.Label.roster.Remove(act); act.labelId = null; act.lifecycleStatus = ArtistLifecycleStatus.Disbanded;
        desk.TrackRosterDeparturesForCheck();
        var gone = desk.RosterDepartures.Single(d => d.ArtistId == id);
        Check(gone.Kind == RosterDepartureKind.Disbanded && gone.UnansweredBroken == 1 && gone.Founding, "the departure records the unanswered broken promise");
        Check(desk.FoundingActSummary().Contains("left with a broken promise unanswered"), "the founding history says how they left");
        string brokenSave = Json(desk.CaptureState());
        slot = "Credible " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "save/load with a departure: " + message);
            var back = desk.RosterDepartures.Single(d => d.ArtistId == id);
            Check(back.Kind == gone.Kind && back.Day == gone.Day && back.UnansweredBroken == 1 && back.Founding, "the departure survives reload");
        } finally { SaveGameService.Delete(slot); }
        var badDeparture = JsonSerializer.Deserialize<PlayerSaveData>(brokenSave, SaveGameService.TestJsonOptions);
        badDeparture.RosterDepartures[0].Kind = (RosterDepartureKind)77;
        string stateNow = Json(desk.CaptureState());
        Check(!desk.RestoreState(badDeparture, out message) && Json(desk.CaptureState()) == stateNow, "a malformed departure is refused before mutation");

        // 11. An amicable departure: nothing broken, so no penalty -- and the act would come back on fair terms.
        Resign();
        desk.Label.roster.Remove(act); act.labelId = null; act.lifecycleStatus = ArtistLifecycleStatus.Disbanded;
        desk.TrackRosterDeparturesForCheck();
        Check(desk.RosterDepartures.Single(d => d.ArtistId == id).UnansweredBroken == 0 && desk.FoundingActSummary().Contains("parted on fair terms"),
            "a departure with nothing broken is fair");
        Check(Read(neighbourId).Stated.All(l => !l.Contains("broke")) && Read(neighbourId).Relationship >= baseNeighbour - 0.0001f, "a fair departure carries no penalty");
        act.lifecycleStatus = ArtistLifecycleStatus.Active;
        Check(desk.ReadContractOutlook(act, false).Stated.Any(l => l.Contains("parted on fair terms")), "the act remembers leaving on fair terms");

        // 12. An introduction: one of their people knows somebody on your roster. A fallout cuts the other way.
        Resign();
        float plain = Read(strangerId).Relationship;
        var members = am.GetArtist(strangerId).members.Where(m => m.isActive).OrderBy(m => m.personId, StringComparer.Ordinal).ToList();
        ContactNetworkService.Link(members[0].personId, actMember.personId, ContactKind.Session, clock.CurrentDate.year);
        var introduced = Read(strangerId);
        Check(introduced.Stated.Any(l => l.Contains(members[0].FullName) && l.Contains("on your label")) && introduced.Relationship > plain + 0.01f,
            "an introduction through the roster helps");
        var others = act.members.Where(m => m.isActive && m.personId != actMember.personId).ToList();
        if (others.Count > 0) {
            ContactNetworkService.Link(members[1].personId, others[0].personId, ContactKind.FormerBandmate, clock.CurrentDate.year, fallout: true);
            var both = Read(strangerId);
            Check(both.Stated.Any(l => l.Contains("fell out")) && both.Relationship < introduced.Relationship, "a fallout with somebody on the roster counts against");
        }

        Reset();
        GD.Print($"SCOUTING_CREDIBLE_OFFERS_PASS checks={checks}");
    }
}
