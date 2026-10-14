using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingContractOutlookChecks {
    /// <summary>Terms that clear the act's ordinary reservation but not the one its doubt about this label holds out
    /// for: the package where only the label stands in the way. Found by search; null if none exists.</summary>
    public static ContractTermSheet? LabelBoundTerms(ContractTalk talk) {
        var ask = talk.ask;
        float minAdvance = PlayerDesk.MinimumAdvanceForAcceptance(talk), credit = PlayerDesk.OutlookCreditOf(talk);
        foreach (bool control in new[] { ask.ArtistCreativeControl, false }.Distinct())
        foreach (bool publishing in new[] { ask.LabelOwnsPublishing, true }.Distinct())
        for (int singles = ask.SinglesObligation; singles <= ask.SinglesObligation + 8; singles += 2)
        for (int term = ask.TermYears; term <= 7; term++)
        for (int adv = 0; adv <= 20; adv++)
        for (int step = 0; step <= 60; step++) {
            float advance = Mathf.Ceil(Mathf.Lerp(ask.Advance, minAdvance, adv / 20f));
            float royalty = Mathf.Round(ask.RoyaltyRate * (1f - step / 75f) * 10000f) / 10000f;
            if (royalty < PlayerDesk.PlayerRoyaltyFloor) break;
            var sheet = new ContractTermSheet(advance, royalty, term, singles, publishing, control,
                ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);
            float value = PlayerDesk.PackageValueOf(talk, sheet);
            if (PlayerDesk.HardLineBroken(talk, sheet) == null && advance >= minAdvance &&
                value >= talk.reservation - talk.outlookLift + 0.002f && value + credit < talk.reservation - 0.002f) return sheet;
        }
        return null;
    }

    /// <summary>A real heard act, made eligible and unmanaged, with its dossier open; not yet approached.</summary>
    public static PlayerDesk.Prospect PrepareAct(PlayerDesk desk) {
        var clock = TimeManager.Instance;
        clock.RestoreClock(GameDate.StartDate, 9);
        var next = desk.ScoutingItinerary().First();
        if (!desk.WaitForSceneAppearance(next.BillId, next.EventId, out string message) ||
            !desk.ListenSceneAppearance(next.BillId, next.EventId, out message)) throw new InvalidOperationException(message);
        var act = ArtistManager.Instance.GetArtist(next.ArtistId);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.prospectMarketStatus = ProspectMarketStatus.Seeking;
        act.lifecycleStatus = ArtistLifecycleStatus.Active; act.isActive = true; act.manager = ManagerArchetype.None;
        act.reputation = 0f; act.momentum = 0f;
        act.evolution ??= new ArtistEvolutionProfile();
        desk.Label.cashReserves = 100000;
        clock.RestoreClock(clock.CurrentDate, 9);
        if (!desk.OpenScoutingDossier(act.artistId, out message)) throw new InvalidOperationException(message);
        return desk.Slate.Single(p => p.Artist == act);
    }

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_CONTRACT_OUTLOOK_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        int week() => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        Check(desk.FoundLabel("Contract Outlook", "nashville", out string message), "found label: " + message);
        var prospect = PrepareAct(desk); string id = prospect.Artist.artistId; var act = prospect.Artist;
        var savedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var savedPlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        int Minute() => clock.CurrentHour * 60 + clock.CurrentMinute;
        void Restore(PlayerSaveData player, GameDate when, int h, int m, WorldSaveData world) {
            LocalScenes.Configure(Array.Empty<string>());
            WorldStateService.Apply(LocalSceneRoomService.Copy(world), when, SimulationSeedBootstrap.RequestedSeed);
            Check(desk.RestoreState(LocalSceneRoomService.Copy(player), out message), "restore: " + message);
            clock.RestoreClock(when, h, m);
            if (desk.Slate.All(p => p.Artist.artistId != id)) desk.OpenScoutingDossier(id, out _);
            prospect = desk.Slate.FirstOrDefault(p => p.Artist.artistId == id);
            act = am.GetArtist(id);
        }
        void Reset() => Restore(savedPlayer, date, hour, minute, savedWorld);
        void Ambition(float value) { act.evolution ??= new ArtistEvolutionProfile(); act.evolution.artisticAmbition = value; }

        // 1. Reading the outlook is pure: same answer twice, no RNG, no world or player change.
        Reset(); Ambition(0.9f);
        string worldBefore = Json(WorldStateService.Capture()), playerBefore = Json(desk.CaptureState());
        GD.Seed(919); uint draw = GD.Randi(); GD.Seed(919);
        var first = desk.ReadContractOutlook(act, false); var second = desk.ReadContractOutlook(act, false);
        Check(Json(first) == Json(second), "outlook is deterministic");
        Check(GD.Randi() == draw && Json(WorldStateService.Capture()) == worldBefore && Json(desk.CaptureState()) == playerBefore && Minute() == hour * 60 + minute,
            "reading draws nothing, spends nothing and writes nothing");

        // 2. A new label's claims are discounted; what the act can see for itself is not.
        float cred = PlayerDesk.CredibilityOf(desk.Label, 0);
        var full = PlayerDesk.CapabilitiesOf(desk.Label, act, week(), desk.SessionPromiseQuote(), 1f);
        Check(Mathf.IsEqualApprox(cred, 0.35f) && Mathf.IsEqualApprox(first.Credibility, cred), "a label with nothing out is mostly paper");
        Check(Mathf.IsEqualApprox(first.Player[CapabilityAxis.Recording], cred * full[CapabilityAxis.Recording]) &&
            Mathf.IsEqualApprox(first.Player[CapabilityAxis.Reach], cred * full[CapabilityAxis.Reach]) &&
            first.Player[CapabilityAxis.Attention] == full[CapabilityAxis.Attention] && first.Player[CapabilityAxis.Local] == 1f,
            "distribution and recording are discounted; attention and being local are not");

        // 3. Stakes: an ambitious act holds out against an unproven label, says why, and talks terms.
        Ambition(0.1f); var modest = desk.ReadContractOutlook(act, false);
        Ambition(0.95f); var ambitious = desk.ReadContractOutlook(act, false);
        Check(ambitious.Stake >= modest.Stake && ambitious.RoomWithheld >= modest.RoomWithheld && ambitious.Doubt >= 0.12f &&
            (modest.Stake >= 0.8f || ambitious.RoomWithheld > modest.RoomWithheld),
            $"ambition raises the stake and the hold-out (withheld {modest.RoomWithheld:0.00} -> {ambitious.RoomWithheld:0.00})");
        Check(PlayerDesk.PostureFor(act, ambitious) != NegotiationPosture.Pushover, "an ambitious act talks terms with a nobody label");
        Check(ambitious.Stated.Any(l => l.Contains("never heard of your label")) && ambitious.Stated.Any(l => l.Contains("songs")), "the act says what it doubts");
        Check(ambitious.Stated.All(l => !l.Any(char.IsDigit) || l.Contains("No offer")), "no numbers in what they say");
        foreach (var o in new[] { modest, ambitious })
            Check((int)PlayerDesk.PostureFor(act, o) >= (int)PlayerDesk.PostureOf(act), "the outlook never makes an act easier than its temperament");

        // 4. A label with records out is believed: the doubt goes, and the line with it.
        int releases = desk.Label.totalReleases, hits = desk.Label.top40Hits;
        desk.Label.totalReleases = 5; desk.Label.top40Hits = 3;
        var proven = desk.ReadContractOutlook(act, false);
        Check(Mathf.IsEqualApprox(proven.Credibility, 1f) && proven.Doubt == 0f && proven.RoomWithheld == 0f && ambitious.RoomWithheld > 0f &&
            proven.Stated.All(l => !l.Contains("never heard")), "proof of work removes the unproven-label hold-out");
        desk.Label.totalReleases = releases; desk.Label.top40Hits = hits;

        // 5. A real rival within reach becomes the act's belief -- never an offer, never named, never written to.
        Reset(); Ambition(0.5f);
        // At week 0 every reachable AI label is at its operating roster target, so no real rival exists and the outlook
        // must not invent one. The fixture opens a slot on the reachable labels (restored by the next Reset).
        var accessible = CompetitorManager.Instance.GetOperatingLabels().Where(l => !l.isPlayerOwned && !l.IsSubsidiary &&
            SceneRecruitmentService.Explain(l, act, week()).Eligible).OrderBy(l => l.labelId, StringComparer.Ordinal).ToList();
        Check(accessible.Count > 0 && accessible.All(l => !l.HasOperatingRosterSpace) && desk.ReadContractOutlook(act, false).RivalLabelId == null,
            "full AI rosters give no rival");
        var reachable = accessible.Where(l => l.CurrentRosterSize < l.maxRosterSize).Take(5).ToList();
        Check(reachable.Count > 0, "fixture needs a reachable AI label below its maximum roster");
        var before = desk.ReadContractOutlook(act, false);
        foreach (var l in reachable) { l.operatingRosterTarget = l.CurrentRosterSize + 1; l.preferredGenres = new[] { act.primaryGenre }; }
        act.reputation = 0.8f; act.momentum = 0.4f;
        worldBefore = Json(WorldStateService.Capture());
        var pressed = desk.ReadContractOutlook(act, false);
        Check(pressed.RivalLabelId != null && reachable.Any(l => l.labelId == pressed.RivalLabelId), "the alternative is a real reachable label");
        Check(pressed.Standing < before.Standing && pressed.AlternativeFit > before.AlternativeFit, "a real alternative lowers the label's standing");
        Check(pressed.Stated.Any(l => l.Contains("wider distribution") && l.Contains("No offer has been made")), "stated as a belief, explicitly not an offer");
        var rivalName = reachable.First(l => l.labelId == pressed.RivalLabelId).labelName;
        Check(pressed.Stated.All(l => !l.Contains(rivalName)) && !desk.OwnLabelSummary().Contains(rivalName), "the rival is never named");
        Check(Json(WorldStateService.Capture()) == worldBefore, "reading a rival writes no AI or world state");

        // 6. At the table: the outlook is in the talk, and when only the label holds the deal back, the act says so.
        Reset(); Ambition(0.95f);
        Check(desk.ApproachToSign(prospect, out message) && prospect.Talk != null, "an ambitious act opens the negotiation: " + message);
        var talk = prospect.Talk; var ask = talk.ask;
        Check(talk.outlookLift > 0f && talk.outlookStated.Count > 0 && talk.capabilityGap.HasValue, "the talk carries the outlook");
        var opened = LocalSceneRoomService.Copy(desk.CaptureState()); var openedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture());
        var openedDate = clock.CurrentDate; int openedHour = clock.CurrentHour, openedMinute = clock.CurrentMinute;
        void Reopen() { Restore(opened, openedDate, openedHour, openedMinute, openedWorld); talk = prospect.Talk; }
        var labelBound = LabelBoundTerms(talk);
        Check(labelBound.HasValue, $"fixture needs terms between the ordinary and the held-out reservation (lift {talk.outlookLift:0.000}, reservation {talk.reservation:0.000})");
        var bound = labelBound.Value;
        bool Table(ContractTermSheet s) => desk.TableOffer(talk, s.Advance, s.RoyaltyRate, s.TermYears, s.SinglesObligation,
            s.LabelOwnsPublishing, s.ArtistCreativeControl, out message);
        int patience = talk.patienceLeft;
        Check(Table(bound) && talk.stage == ContractTalkStage.Objection && talk.capabilityObjection == talk.capabilityGap, "the objection is the label, not a term");
        Check(talk.patienceLeft == patience - 1 && !talk.log[0].Any(char.IsDigit) && am.GetArtist(id).labelId != desk.Label.labelId,
            "a label objection costs patience, shows no number and signs nothing");

        // 7. The promotion promise: free to propose, scored, affordable, recorded beside the session promise.
        Reopen();
        float cash = desk.Label.cashReserves; GD.Seed(921); draw = GD.Randi(); GD.Seed(921); worldBefore = Json(WorldStateService.Capture());
        Check(desk.AddPromotionPromise(talk, out message) && talk.promotionPromised && !desk.AddPromotionPromise(talk, out _), "promotion joins the package once: " + message);
        Check(GD.Randi() == draw && desk.Label.cashReserves == cash && Json(WorldStateService.Capture()) == worldBefore &&
            Minute() == openedHour * 60 + openedMinute && desk.ContractCommitments.Count == 0, "proposing a push spends, draws and commits nothing");
        Check(desk.WithdrawPromotionPromise(talk, out message) && !talk.promotionPromised && !desk.WithdrawPromotionPromise(talk, out _), "the push can be withdrawn");
        float reserve = desk.Label.GetMonthlyOverhead() * 2f;
        Check(desk.AddPromotionPromise(talk, out message), "promise again");
        desk.Label.cashReserves = bound.Advance + reserve + PlayerDesk.PromotionPromiseQuote() * 0.5f;
        Check(!Table(bound) && message.Contains("trade ad") && Minute() == openedHour * 60 + openedMinute && talk.patienceLeft == patience,
            "an unaffordable push blocks before time or patience: " + message);
        Reopen();
        Check(desk.AddPromotionPromise(talk, out message) && desk.AddSessionPromise(talk, out message), "both promises on the table");
        int agreed = LocalSceneRoomService.Day(clock.CurrentDate);
        Check(Table(new ContractTermSheet(ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl,
            ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary)) && talk.stage == ContractTalkStage.Done &&
            am.GetArtist(id).labelId == desk.Label.labelId, "the ask with both promises signs: " + message);
        var commitments = desk.ContractCommitments;
        var push = commitments.Single(c => c.Kind == ContractCommitmentKind.FundedPromotion);
        Check(commitments.Count == 2 && push.Status == ContractCommitmentStatus.Open && push.DueDay == agreed + PlayerDesk.PromotionReleaseDeadlineDays &&
            push.QuotedCost == PlayerDesk.PromotionPromiseQuote() && push.ReleaseDay == null && desk.OpenCommitmentsFor(id).Count == 2,
            "one promotion and one session commitment, the push due once there is a record");
        var signed = LocalSceneRoomService.Copy(desk.CaptureState()); var signedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture());
        var signedDate = clock.CurrentDate; int signedHour = clock.CurrentHour, signedMinute = clock.CurrentMinute;

        // 8. Kept: the first record comes out, the clock starts, and only a half-page ad on it keeps the promise.
        int released = agreed + 10;
        ChartManager.Instance.ReleaseRecord(new Record { recordId = "outlook_push_single", title = "Outlook Push", artistId = id,
            artistName = act.stageName, labelId = desk.Label.labelId, isPlayerOwned = true, format = ReleaseFormat.Single,
            primaryGenre = act.primaryGenre, releaseDate = LocalSceneRoomService.Date(released) });
        clock.RestoreClock(LocalSceneRoomService.Date(released), 22); clock.EndDay();
        push = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.FundedPromotion);
        Check(push.ReleaseDay == released && push.DueDay == released + PlayerDesk.PromotionPromiseDays && push.Status == ContractCommitmentStatus.Open,
            "the release starts the promotion clock");
        clock.RestoreClock(clock.CurrentDate, 9);
        Check(desk.BuyTradeAd("outlook_push_single", TradeAdTier.QuarterPage, out message) &&
            desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.FundedPromotion).Status == ContractCommitmentStatus.Open,
            "a quarter-page ad is not the promised push");
        Check(desk.BuyTradeAd("outlook_push_single", TradeAdTier.HalfPage, out message), "half-page ad: " + message);
        var after = desk.ContractCommitments;
        Check(after.Single(c => c.Kind == ContractCommitmentKind.FundedPromotion).Status == ContractCommitmentStatus.Kept &&
            after.Single(c => c.Kind == ContractCommitmentKind.FundedSession).Status == ContractCommitmentStatus.Open, "the push keeps only the promotion promise");
        Check(PlayerDesk.CredibilityOf(desk.Label, after.Count(c => c.Status == ContractCommitmentStatus.Kept)) > cred, "a kept promise is proof of work");

        // 9. Missed: no record by the deadline breaks it once, says so, and the renewal remembers.
        Restore(signed, signedDate, signedHour, signedMinute, signedWorld);
        int events = act.careerEvents.Count;
        clock.RestoreClock(LocalSceneRoomService.Date(agreed + PlayerDesk.PromotionReleaseDeadlineDays), 22); clock.EndDay();
        var missed = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.FundedPromotion);
        Check(missed.Status == ContractCommitmentStatus.Missed && act.careerEvents.Skip(events).Count(e => e.Contains("never put out the record")) == 1,
            "a record that never came out breaks the promise once");
        act.contractSinglesObligation = 2; act.contractReleases = 2; clock.RestoreClock(clock.CurrentDate, 9);
        Check(desk.ApproachRenewal(act, out message) && desk.PendingRenewal.Ask.DemandSummary.Contains("push you promised"), "renewal ask names the broken push: " + message);
        Check(desk.ContractOutlookLines(act, true).Any(l => l.Contains("broke")), "the act remembers the broken promise at the table");
        desk.StepBackFromRenewal(out _);

        // 10. Easy acts: the floor moves with the outlook, both ways.
        var easyAsk = new ContractTermSheet(1000f, 0.05f, 3, 4, true, false, 0.1f, ManagerArchetype.None, null, "");
        ContractTermSheet At(float fraction) => new(Mathf.Ceil(1000f * fraction), 0.05f, 3, 4, true, false, 0.1f, ManagerArchetype.None, null, "");
        Check(!PlayerDesk.ClearsPushoverFloorOf(easyAsk, At(0.38f), 0f) && PlayerDesk.ClearsPushoverFloorOf(easyAsk, At(0.38f), 0.03f),
            "an act glad of any label takes a little less");
        Check(PlayerDesk.ClearsPushoverFloorOf(easyAsk, At(0.42f), 0f) && !PlayerDesk.ClearsPushoverFloorOf(easyAsk, At(0.42f), -0.05f),
            "an act with somewhere better to go holds a little more");

        // 11. Save/load keeps the outlook and the promises on the table; malformed outlooks are refused before mutation.
        Reopen(); Check(desk.AddPromotionPromise(talk, out message), "promise before save");
        string slot = "Outlook " + Guid.NewGuid().ToString("N");
        try {
            var expected = (talk.outlookCredit, talk.outlookLift, talk.capabilityGap, talk.reservation, Json(talk.outlookStated));
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "compressed save/load: " + message);
            Check(desk.OpenScoutingDossier(id, out message), "reopen after load: " + message);
            var t = desk.Slate.First(p => p.Artist.artistId == id).Talk;
            Check(t != null && t.promotionPromised && (t.outlookCredit, t.outlookLift, t.capabilityGap, t.reservation, Json(t.outlookStated)) == expected,
                "outlook and promise survive reload unchanged");
        } finally { SaveGameService.Delete(slot); }
        Reopen(); Check(desk.AddPromotionPromise(talk, out message), "promise before capture");
        var withTalk = LocalSceneRoomService.Copy(desk.CaptureState());
        foreach (var corrupt in new Action<PlayerSaveData>[] {
            s => s.ScoutingKnowledge.Negotiations.Single(n => n.ArtistId == id).Talk.outlookCredit = float.NaN,
            s => s.ScoutingKnowledge.Negotiations.Single(n => n.ArtistId == id).Talk.outlookLift = 0.9f,
            s => s.ScoutingKnowledge.Negotiations.Single(n => n.ArtistId == id).Talk.capabilityGap = (CapabilityAxis)99 }) {
            var invalid = LocalSceneRoomService.Copy(withTalk); corrupt(invalid);
            string state = Json(desk.CaptureState());
            Check(!desk.RestoreState(invalid, out message) && Json(desk.CaptureState()) == state, "malformed outlook rejected before mutation");
        }
        foreach (var corrupt in new Action<PlayerSaveData>[] {
            s => s.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.FundedSession).ReleaseDay = agreed,
            s => s.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.FundedPromotion).DueDay = agreed + PlayerDesk.PromotionReleaseDeadlineDays + 1 }) {
            var invalid = LocalSceneRoomService.Copy(signed); corrupt(invalid);
            string state = Json(desk.CaptureState());
            Check(!desk.RestoreState(invalid, out message) && Json(desk.CaptureState()) == state, "malformed promotion commitment rejected before mutation");
        }
        var legacy = LocalSceneRoomService.Copy(withTalk);
        var legacyTalk = legacy.ScoutingKnowledge.Negotiations.Single(n => n.ArtistId == id).Talk;
        legacyTalk.outlookCredit = 0f; legacyTalk.outlookLift = 0f; legacyTalk.capabilityGap = null; legacyTalk.outlookStated = null; legacyTalk.promotionPromised = false;
        Check(desk.RestoreState(legacy, out message), "an older talk without an outlook restores: " + message);

        // 12. The control flag turns the outlook off: temperament alone, the old acceptance check.
        Reset(); Ambition(0.95f);
        LocalScenes.Configure(new[] { "--disable-contract-outlook" });
        var off = desk.ReadContractOutlook(act, false);
        Check(off.Credit == 0f && off.ReservationLift == 0f && off.RoomWithheld == 0f && off.Stated.Count == 0 && PlayerDesk.PostureFor(act, off) == PlayerDesk.PostureOf(act),
            "disabled outlook reproduces temperament-only posture and no credit");
        LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCOUTING_CONTRACT_OUTLOOK_PASS checks={checks}");
    }

    /// <summary>Diagnostics: how the outlook reads every local unsigned act for a newly founded label. Prints only.</summary>
    public static void Survey(string city) {
        var desk = PlayerDesk.Instance;
        if (!desk.FoundLabel("Outlook Survey", city, out string message)) throw new InvalidOperationException(message);
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        var local = ArtistManager.Instance.GetAllArtists().Where(a => string.IsNullOrEmpty(a.labelId) && a.isActive && a.lifecycleStatus == ArtistLifecycleStatus.Active)
            .Where(a => LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId) == city).ToList();
        GD.Print("OUTLOOK_SURVEY local status: " + string.Join(", ", local.GroupBy(a => $"{a.careerState}/{a.prospectMarketStatus}/seq{Math.Min(1, a.contractSequence)}")
            .OrderByDescending(g => g.Count()).Select(g => $"{g.Key}={g.Count()}")) +
            $"; signable now={local.Count(a => ArtistManager.Instance.IsEligibleForPopulationSigning(a, week))}");
        var caps = PlayerDesk.CapabilitiesOf(desk.Label, local.FirstOrDefault(), week, desk.SessionPromiseQuote(), PlayerDesk.CredibilityOf(desk.Label, 0));
        GD.Print($"OUTLOOK_SURVEY city={city} acts={local.Count} label=" + string.Join(" ", caps.Select(kv => $"{kv.Key}={kv.Value:0.00}")));
        var history = local.Select(a => LocalSceneRoomService.ObserveActWork(a.artistId, LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate))).ToList();
        GD.Print($"OUTLOOK_SURVEY rooms prewarm={(LocalScenes.RoomPrewarm ? LocalScenes.RoomPrewarmWeeks + "wk" : "off")} withShows={history.Count(h => h.LifetimeShows > 0)} " +
            $"shows12+={history.Count(h => h.LifetimeShows >= 12)} booked={history.Count(h => h.BookedRoomIds.Count > 0)} engaged={history.Count(h => h.EngagementRoomId != null)}");
        int day0 = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        foreach (var room in SceneRoomCatalog.ForPlace(city).Where(r => r.IsPerformance))
            GD.Print($"OUTLOOK_SURVEY room {room.Id} nights={Enumerable.Range(0, 7).Count(d => SceneLiveEconomics.OpenOn(room, (DayOfWeek)d))} " +
                $"slots={SceneEcosystemService.Slots(room, day0)} eligible={local.Count(a => LocalSceneRoomService.Eligible(a, room, day0))} " +
                $"withHistory={local.Count(a => LocalSceneRoomService.ShowsAt(room.Id, a.artistId) > 0)}");
        // Booking spread over the pre-rolled history: how many acts a room actually used, and how much of it one act took.
        foreach (var room in SceneRoomCatalog.ForPlace(city).Where(r => r.IsPerformance)) {
            int[] shows = local.Select(a => LocalSceneRoomService.ShowsAt(room.Id, a.artistId)).Where(s => s > 0).OrderByDescending(s => s).ToArray();
            int total = shows.Sum();
            if (total == 0) continue;
            GD.Print($"OUTLOOK_SURVEY spread {room.Id} sets={total} acts={shows.Length} top={shows[0]} top1share={shows[0] / (float)total:0.00} " +
                $"top3share={shows.Take(3).Sum() / (float)total:0.00} median={shows[shows.Length / 2]} acts12+={shows.Count(s => s >= 12)}");
        }
        var rows = local.Select(a => (Act: a, O: desk.ReadContractOutlook(a, false))).ToList();
        foreach (var group in rows.GroupBy(r => (Old: PlayerDesk.PostureOf(r.Act), New: PlayerDesk.PostureFor(r.Act, r.O))).OrderBy(g => g.Key.Old).ThenBy(g => g.Key.New))
            GD.Print($"OUTLOOK_SURVEY posture {group.Key.Old}->{group.Key.New} n={group.Count()}");
        float[] credits = rows.Select(r => r.O.Credit).OrderBy(x => x).ToArray();
        if (credits.Length > 0)
            GD.Print($"OUTLOOK_SURVEY credit min={credits[0]:0.000} p25={credits[credits.Length / 4]:0.000} median={credits[credits.Length / 2]:0.000} " +
                $"p75={credits[credits.Length * 3 / 4]:0.000} max={credits[^1]:0.000} rival={rows.Count(r => r.O.RivalLabelId != null)} " +
                $"working={rows.Count(r => r.O.Work >= 0.45f)} meanWork={rows.Average(r => r.O.Work):0.00} meanProspects={rows.Average(r => r.O.Prospects):0.00} " +
                $"meanWithheld={rows.Average(r => r.O.RoomWithheld):0.00} meanStake={rows.Average(r => r.O.Stake):0.00}");
        foreach (var gap in rows.Where(r => r.O.Gap.HasValue).GroupBy(r => r.O.Gap.Value)) GD.Print($"OUTLOOK_SURVEY gap {gap.Key} n={gap.Count()}");
        foreach (var r in rows.OrderBy(r => r.O.Credit).Take(3).Concat(rows.OrderByDescending(r => r.O.Credit).Take(2)))
            GD.Print($"OUTLOOK_SURVEY act {r.Act.stageName}: credit={r.O.Credit:0.000} work={r.O.Work:0.00} prospects={r.O.Prospects:0.00} " +
                $"rival={r.O.RivalLabelId ?? "-"} | {string.Join(" | ", r.O.Stated)}");
    }
}
