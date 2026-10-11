using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Phases 3-5 follow-through: historical advances, rival courtship (inquiry, meeting, offer) with sourced knowledge,
/// the AI market courting a pursued act, renewal poaching, the visibility of the player's own pursuit, and set-level
/// audience attribution. Deterministic fixtures on a real heard act; AI labels are only given room, genre and cash so a
/// rival exists.</summary>
public static class ScoutingRivalCourtshipChecks {
    /// <summary>Gives every reachable AI label room, the act's genre and cash, so a real rival exists for an act on the
    /// player's roster, and returns the one that would come for it.</summary>
    static AILabel OpenRenewalRival(SimulatedArtist act) {
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        foreach (var l in CompetitorManager.Instance.GetOperatingLabels().Where(l => !l.isPlayerOwned && !l.IsSubsidiary &&
                     SceneRecruitmentService.Explain(l, act, week).Eligible && l.CurrentRosterSize < l.maxRosterSize)
                     .OrderBy(l => l.labelId, StringComparer.Ordinal).Take(5)) {
            l.operatingRosterTarget = l.CurrentRosterSize + 1; l.preferredGenres = new[] { act.primaryGenre }; l.cashReserves += 200000f;
        }
        return RosterManager.Instance.FindRivalForPlayerRenewal(act, TimeManager.Instance.CurrentDate);
    }

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_RIVAL_COURTSHIP_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        string[] args = OS.GetCmdlineUserArgs();
        string message;
        ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;

        // 0. Flags: historical advances and live-draw recognition are on by default, each with an explicit off control.
        Check(ContractPricing.HistoricalAdvances && LocalScenes.LiveDrawRecognition && LocalScenes.RivalOffers, "historical advances, live-draw recognition and rival offers default on");
        bool Rejects(Action configure) { try { configure(); return false; } catch (ArgumentException) { return true; } }
        Check(Rejects(() => ContractPricing.Configure(new[] { "--enable-historical-advances", "--disable-historical-advances" })), "pricing enable+disable refused");
        Check(Rejects(() => LocalScenes.Configure(args.Concat(new[] { "--enable-live-draw-recognition", "--disable-live-draw-recognition" }))), "recognition enable+disable refused");
        LocalScenes.Configure(args.Append("--disable-live-draw-recognition"));
        Check(!LocalScenes.LiveDrawRecognition && LocalScenes.ScoutingExperience, "--disable-live-draw-recognition turns only that off");
        LocalScenes.Configure(args); ContractPricing.Configure(args);
        Check(SceneRecruitmentService.ActVisibility != null && RosterManager.Instance.PlayerPursuitInterceptor != null, "the desk registers its visibility weight and nomination hook");

        Check(desk.FoundLabel("Rival Courtship", "nashville", out message), "found label: " + message);
        var prospect = ScoutingContractOutlookChecks.PrepareAct(desk); string id = prospect.Artist.artistId; var act = prospect.Artist;
        var savedWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var savedPlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
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
        var heard = desk.ScoutingDossiers.Single(d => d.ArtistId == id).Observations.First(o => o.Kind == ScoutingEvidenceKind.Performance);
        int heardDay = heard.AcquiredDay;

        // 1. Historical advances: an unproven act's price is its own going rate with a modest premium for a bigger buyer;
        //    real money follows a proven record; the legacy flag restores the old tier prices exactly.
        Reset();
        var byTier = CompetitorManager.Instance.GetOperatingLabels().Where(l => !l.isPlayerOwned)
            .GroupBy(l => l.tier).ToDictionary(g => g.Key, g => g.OrderBy(l => l.labelId, StringComparer.Ordinal).First());
        var major = byTier[LabelTier.Major]; var indie = byTier.GetValueOrDefault(LabelTier.Independent) ?? byTier[LabelTier.Small];
        float going = ContractPricing.GoingRate(act);
        float majorOffer = major.CalculateAdvanceOffer(act), indieOffer = indie.CalculateAdvanceOffer(act), legacy = major.LegacyAdvanceOffer(act);
        GD.Print($"RIVAL_COURTSHIP pricing ask=${prospect.AskingAdvance:N0} going=${going:N0} major=${majorOffer:N0} {indie.tier}=${indieOffer:N0} legacyMajor=${legacy:N0}");
        Check(Mathf.IsEqualApprox(majorOffer, Mathf.Min(legacy, going * ContractPricing.TierPremium(LabelTier.Major))), "a major prices an unknown act at its going rate plus the major's premium");
        Check(majorOffer <= prospect.AskingAdvance * 10f && majorOffer < legacy / 20f, "an unknown act's major offer is the same order as its ask to the player, not the legacy $10k");
        Check(majorOffer > indieOffer && majorOffer <= indieOffer * 2f, "a bigger buyer pays a little more, never a different order of magnitude");
        CareerState was = act.careerState;
        act.careerState = CareerState.Star;
        Check(Mathf.IsEqualApprox(major.CalculateAdvanceOffer(act), major.LegacyAdvanceOffer(act)), "a proven act commands the full legacy price");
        act.careerState = CareerState.Rising;
        float rising = major.CalculateAdvanceOffer(act);
        Check(rising > majorOffer && rising < major.LegacyAdvanceOffer(act), "a rising act prices between the two");
        act.careerState = was;
        ContractPricing.ConfigureForCheck(false);
        Check(Mathf.IsEqualApprox(major.CalculateAdvanceOffer(act), legacy) && ContractPricing.EstimatedFirstAdvance(LabelTier.Small, 800f) == 800f, "--disable-historical-advances restores the legacy prices");
        ContractPricing.ConfigureForCheck(true);
        Check(ContractPricing.EstimatedFirstAdvance(LabelTier.Small, 800f) < 400f, "a small label's scouting gate estimates a working act's price, not $800");
        var bidder = ScoutingCredibleOffersChecks.OpenRival(act);
        Check(bidder != null, "fixture needs a reachable rival");
        var paper = PlayerDesk.RivalOfferTerms(bidder, act, Today(), clock.CurrentDate.year);
        Check(paper.Advance <= Mathf.Max(prospect.AskingAdvance, going) * 3f, $"the rival's paper is priced to the act (${paper.Advance:N0})");

        // 2. Visibility: one signal per event, live between its delay and expiry; repeats of one event add nothing.
        Reset();
        Check(desk.PursuitVisibility(id, heardDay) == 0f && Mathf.IsEqualApprox(desk.PursuitVisibility(id, heardDay + 2), 0.04f) &&
            desk.PursuitVisibility(id, heardDay + 16) == 0f, "a night at the back of the room is seen by a few, after a delay, for a while");
        var dossier = desk.ScoutingDossiers.Single(d => d.ArtistId == id);
        float once = desk.PursuitVisibility(id, heardDay + 3);
        Check(desk.ApproachToSign(prospect, out message), "open a discussion: " + message);
        float talking = desk.PursuitVisibility(id, Today() + 2);
        Check(talking > once + 0.15f && talking <= 1f, $"paper on the table is far more visible ({once:0.00} -> {talking:0.00})");
        double weight = desk.AiVisibilityWeightForCheck(act);
        Check(weight >= 1d && weight <= 1d + PlayerDesk.VisibilityDiscoveryBoost, "the AI slate weight is bounded by the scene attention ceiling");

        // 3. A rival's courtship starts by listening. No draws, no world writes; nothing to hear until someone could know.
        Reset();
        bidder = ScoutingCredibleOffersChecks.OpenRival(act);
        clock.RestoreClock(LocalSceneRoomService.Date(heardDay + 1), 9);
        string worldBefore = Json(WorldStateService.Capture());
        GD.Seed(941); uint draw = GD.Randi(); GD.Seed(941);
        var court = desk.StartCourtshipForCheck(act, bidder, RivalCourtshipOrigin.Crowding);
        Check(GD.Randi() == draw && Json(WorldStateService.Capture()) == worldBefore, "starting a courtship draws nothing and writes no world state");
        Check(court.Stage == RivalCourtshipStage.Inquiry && court.NextDay >= Today() + 2 && court.NextDay <= Today() + 4 && desk.RivalInterestLine(id) == null,
            "an inquiry is a man listening at the back: nobody tells the player yet");
        // The booker of the room it happened in tells the player the next time the player is there.
        desk.SetCourtshipRoomForCheck(court.Id, heard.RoomId);
        desk.AdvanceRivalCourtshipsForCheck();
        Check(desk.RivalInterestLine(id) == null, "a visit before the inquiry is not news of it");
        var revisit = LocalSceneRoomService.Copy(heard); revisit.Id = "revisit"; revisit.EventId = "revisit-event"; revisit.AcquiredDay = Today() + 1;
        var withVisit = LocalSceneRoomService.Copy(desk.CaptureState());
        withVisit.ScoutingKnowledge.Dossiers.Single(d => d.ArtistId == id).Observations.Add(revisit);
        Check(desk.RestoreState(withVisit, out message), "inject a later visit: " + message);
        clock.RestoreClock(LocalSceneRoomService.Date(Today() + 1), 9);
        desk.AdvanceRivalCourtshipsForCheck();
        var booker = desk.RivalCourtships.Single(c => c.ArtistId == id).Known.SingleOrDefault(k => k.Channel == RivalSightingChannel.RoomBooker);
        Check(booker != null && booker.Day == revisit.AcquiredDay && booker.Text.Contains(bidder.labelName) && desk.RivalInterestLine(id)?.Contains(bidder.labelName) == true,
            "the room's booker names the label when the player is next in the room");
        var interest = desk.ScoutingDossiers.Single(d => d.ArtistId == id).Observations.Where(o => o.Kind == ScoutingEvidenceKind.RivalInterest).ToList();
        Check(interest.Count == 1 && interest[0].SourceName.Contains("booker") && interest[0].AcquiredDay == revisit.AcquiredDay, "the notebook keeps it as sourced, dated evidence");
        desk.AdvanceRivalCourtshipsForCheck();
        Check(desk.ScoutingDossiers.Single(d => d.ArtistId == id).Observations.Count(o => o.Kind == ScoutingEvidenceKind.RivalInterest) == 1, "the same news is not delivered twice");
        // The step to a meeting: either the rival passes (stable hash), or it sits down with them.
        court = desk.RivalCourtships.Single(c => c.ArtistId == id);
        bool fades = PlayerDesk.StableUnitForCheck($"{seed}|{court.Id}|fade") < PlayerDesk.RivalInterestFadeShare;
        clock.RestoreClock(LocalSceneRoomService.Date(court.NextDay), 6);
        GD.Seed(942); draw = GD.Randi(); GD.Seed(942);
        desk.AdvanceRivalCourtshipsForCheck();
        Check(GD.Randi() == draw, "moving a courtship on draws nothing");
        court = desk.RivalCourtships.Single(c => c.ArtistId == id);
        Check(fades ? court.Stage == RivalCourtshipStage.Faded && court.EndReason.Contains("passed") : court.Stage == RivalCourtshipStage.Meeting && court.MeetingDay == Today(),
            $"interest can end: the stable hash decides ({(fades ? "faded" : "met")})");

        // 4. The AI market chose an act the player is pursuing: the label courts it instead of signing on the spot.
        Reset();
        bidder = ScoutingCredibleOffersChecks.OpenRival(act);
        var stranger = am.GetAllArtists().Where(a => string.IsNullOrEmpty(a.labelId) && a.lifecycleStatus == ArtistLifecycleStatus.Active && a.artistId != id &&
            !desk.IsPursuing(a.artistId)).OrderBy(a => a.artistId, StringComparer.Ordinal).First();
        Check(!desk.InterceptAiNominationForCheck(stranger, bidder) && desk.RivalCourtships.All(c => c.ArtistId != stranger.artistId), "an act the player isn't pursuing is the AI's to sign");
        Check(desk.IsPursuing(id) && desk.InterceptAiNominationForCheck(act, bidder), "a pursued act is taken over");
        court = desk.RivalCourtships.Single(c => c.ArtistId == id);
        Check(court.Origin == RivalCourtshipOrigin.DailyMarket && court.Stage == RivalCourtshipStage.Meeting && string.IsNullOrEmpty(act.labelId),
            "the label goes straight to a meeting; nobody is signed");
        Check(desk.InterceptAiNominationForCheck(act, bidder) && desk.RivalCourtships.Count(c => c.ArtistId == id) == 1, "a second nomination opens no second courtship");
        // Heard today, so the scene is talking: the meeting reaches the player the next day, not today.
        Check(court.Known.Any(k => k.Channel == RivalSightingChannel.SceneTalk && k.Day == Today() + 1 && !k.Delivered) && desk.RivalInterestLine(id) == null,
            "scene talk reaches the player overnight, never the same day");
        // A handshake makes the rival wait; meanwhile the word arrives. When the hold is gone, it puts paper down.
        RosterManager.Instance.SetPlayerHold(id, clock.CurrentDate.AddDays(5));
        clock.RestoreClock(LocalSceneRoomService.Date(court.NextDay), 6);
        desk.AdvanceRivalCourtshipsForCheck();
        Check(desk.RivalCourtships.Single(c => c.ArtistId == id).Stage == RivalCourtshipStage.Meeting && desk.PendingRivalOffer(id) == null, "a handshake holds the rival off");
        Check(desk.RivalInterestLine(id)?.Contains(bidder.labelName) == true && desk.RivalInterestLine(id).Contains("Nobody has mentioned an offer"),
            "by then word around town names the label, and says no more than it knows");
        RosterManager.Instance.ClearPlayerHold(id);
        clock.RestoreClock(LocalSceneRoomService.Date(Today() + 1), 6);
        desk.AdvanceRivalCourtshipsForCheck();
        court = desk.RivalCourtships.Single(c => c.ArtistId == id);
        var offer = desk.PendingRivalOffer(id);
        Check(court.Stage == RivalCourtshipStage.Offer && offer != null && offer.CourtshipId == court.Id && offer.RivalLabelId == bidder.labelId,
            "the meeting becomes a real offer from that label");
        Check(offer.Disclosure == RivalOfferDisclosure.Rumored, "word of the offer comes from the scene the player has been working, without the terms");
        clock.RestoreClock(LocalSceneRoomService.Date(offer.DecisionDay), 6);
        desk.ResolveRivalOffersForCheck();
        Check(am.GetArtist(id).labelId == bidder.labelId && desk.RivalCourtships.Single(c => c.ArtistId == id).Stage == RivalCourtshipStage.Ended,
            "the act takes it on the day and the courtship ends");

        // 5. Out of earshot: an offer nobody told the player about stands, and the act answers it unseen.
        Reset();
        bidder = ScoutingCredibleOffersChecks.OpenRival(act);
        clock.RestoreClock(LocalSceneRoomService.Date(heardDay + PlayerDesk.SceneTalkDays + 3), 9);
        desk.InterceptAiNominationForCheck(act, bidder);
        court = desk.RivalCourtships.Single(c => c.ArtistId == id);
        Check(court.Known.Count == 0, "a meeting nobody could tell the player about stays unknown");
        int logBefore = desk.Log.Count;
        clock.RestoreClock(LocalSceneRoomService.Date(court.NextDay), 6);
        desk.AdvanceRivalCourtshipsForCheck();
        offer = desk.PendingRivalOffer(id);
        Check(offer?.Disclosure == RivalOfferDisclosure.Unknown && desk.RivalOfferLine(id) == null && desk.Log.Count == logBefore &&
            !desk.ScoutingCompetitionSummary(id).Contains(bidder.labelName), "the offer is real but unknown: no line, no note, nothing in the notebook");
        clock.RestoreClock(LocalSceneRoomService.Date(offer.DecisionDay), 6);
        desk.ResolveRivalOffersForCheck();
        Check(am.GetArtist(id).labelId == bidder.labelId && desk.Log.Count == logBefore, "the act signs elsewhere without the desk being told");

        // 6. The act's own side tells the player once they have talked.
        Reset();
        bidder = ScoutingCredibleOffersChecks.OpenRival(act);
        Check(desk.ApproachToSign(prospect, out message), "talk terms: " + message);
        desk.InterceptAiNominationForCheck(act, bidder);
        court = desk.RivalCourtships.Single(c => c.ArtistId == id);
        Check(court.Known.Any(k => k.Channel == RivalSightingChannel.ActSide && k.Day == Today() && k.Delivered) && desk.RivalInterestLine(id).Contains("sat down"),
            "the act tells the player the same day that a label sat down with them");
        clock.RestoreClock(LocalSceneRoomService.Date(court.NextDay), 6);
        desk.AdvanceRivalCourtshipsForCheck();
        Check(desk.PendingRivalOffer(id)?.Disclosure == RivalOfferDisclosure.Disclosed && desk.RivalOfferLine(id).Contains("$"), "and the terms of the offer");

        // 7. A rival that can no longer take the act lets the courtship fade.
        Reset();
        bidder = ScoutingCredibleOffersChecks.OpenRival(act);
        desk.InterceptAiNominationForCheck(act, bidder);
        court = desk.RivalCourtships.Single(c => c.ArtistId == id);
        CompetitorManager.Instance.GetLabel(bidder.labelId).cashReserves = 0f;
        clock.RestoreClock(LocalSceneRoomService.Date(court.NextDay), 6);
        desk.AdvanceRivalCourtshipsForCheck();
        Check(desk.RivalCourtships.Single(c => c.ArtistId == id).Stage == RivalCourtshipStage.Faded && desk.PendingRivalOffer(id) == null, "no cash, no offer");

        // 8. Save/load keeps courtships and what was heard; malformed ones are refused before mutation; older saves invent none.
        Reset();
        bidder = ScoutingCredibleOffersChecks.OpenRival(act);
        Check(desk.ApproachToSign(prospect, out message), "talk terms: " + message);
        desk.InterceptAiNominationForCheck(act, bidder);
        var courted = LocalSceneRoomService.Copy(desk.CaptureState());
        string slot = "Courtship " + Guid.NewGuid().ToString("N");
        try {
            string expected = Json(desk.RivalCourtships);
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "compressed save/load: " + message);
            Check(Json(desk.RivalCourtships) == expected && desk.RivalInterestLine(id) != null, "the courtship survives reload unchanged");
        } finally { SaveGameService.Delete(slot); }
        foreach (var corrupt in new Action<PlayerSaveData>[] {
            s => s.RivalCourtships[0].PlayerLabelId = "someone_else",
            s => s.RivalCourtships[0].MeetingDay = null,
            s => s.RivalCourtships[0].Known[0].Channel = (RivalSightingChannel)42,
            s => s.RivalCourtships[0].EndedDay = 3,
            s => { var twin = LocalSceneRoomService.Copy(s.RivalCourtships[0]); twin.Id += "|2"; s.RivalCourtships.Add(twin); },
            s => s.RivalCourtships[0].Stage = (RivalCourtshipStage)9 }) {
            var invalid = LocalSceneRoomService.Copy(courted); corrupt(invalid);
            string state = Json(desk.CaptureState());
            Check(!desk.RestoreState(invalid, out message) && Json(desk.CaptureState()) == state, "malformed courtship rejected before mutation");
        }
        var older = LocalSceneRoomService.Copy(courted); older.RivalCourtships = null;
        Check(desk.RestoreState(older, out message) && desk.RivalCourtships.Count == 0, "an older save holds no courtships and none are invented");

        // 9. Renewal: a player's act on run-out paper is free to listen, and its side tells the player.
        Reset();
        act.evolution.artisticAmbition = 0.95f;
        Check(desk.ApproachToSign(prospect, out message) && prospect.Talk != null, "talk terms: " + message);
        var ask = prospect.Talk.ask;
        var generous = new ContractTermSheet(ask.Advance * 1.5f, ask.RoyaltyRate, 1, ask.SinglesObligation, false, true,
            ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);
        Check(desk.TableOffer(prospect.Talk, generous.Advance, generous.RoyaltyRate, generous.TermYears, generous.SinglesObligation,
            generous.LabelOwnsPublishing, generous.ArtistCreativeControl, out message) && act.labelId == desk.Label.labelId, "sign the act: " + message);
        desk.TrackRosterDeparturesForCheck(); // the dawn watch now knows the act is on the roster
        int week = ChartManager.Instance.GetCurrentChartWeek();
        act.contractExpiresWeek = week; act.contractExpiresYear = clock.CurrentDate.year;
        Check(RosterManager.IsContractMatured(act, clock.CurrentDate.year, week), "the act's paper has run out");
        var poacher = OpenRenewalRival(act);
        Check(poacher != null && poacher.labelId != desk.Label.labelId, "a label could take on the run-out act");
        Check(RosterManager.Instance.FindRivalToPlayer(act, clock.CurrentDate) == null, "the ordinary finder never reaches a signed act");
        court = desk.StartCourtshipForCheck(act, poacher, RivalCourtshipOrigin.Renewal);
        Check(court.Stage == RivalCourtshipStage.Meeting && court.Known.Single().Channel == RivalSightingChannel.ActSide && desk.RivalInterestLine(id).Contains("run out"),
            "the act's side says another label has been in touch now the paper has run out");
        clock.RestoreClock(LocalSceneRoomService.Date(court.NextDay), 6);
        desk.AdvanceRivalCourtshipsForCheck();
        offer = desk.PendingRivalOffer(id);
        Check(offer != null && offer.Renewal && offer.Disclosure == RivalOfferDisclosure.Disclosed && desk.RivalOfferLine(id).Contains("$"), "the poacher's offer, with terms, from the act's side");
        clock.RestoreClock(clock.CurrentDate, 9);
        Check(desk.ApproachRenewal(act, out message) && desk.PendingRenewal.Posture != NegotiationPosture.Pushover && desk.PendingRenewal.Talk != null,
            "an act holding another label's paper talks terms at renewal: " + message);
        var renewTalk = desk.PendingRenewal.Talk;
        Check(renewTalk.outlookStated.Any(l => l.Contains(poacher.labelName)), "the renewal table states the offer");
        var atTable = LocalSceneRoomService.Copy(desk.CaptureState()); var atTableWorld = LocalSceneRoomService.Copy(WorldStateService.Capture());
        var atTableDate = clock.CurrentDate; int atTableHour = clock.CurrentHour, atTableMinute = clock.CurrentMinute;
        // Not renewed by the day: the act lets the paper end and signs with the poacher on exactly those terms.
        clock.RestoreClock(LocalSceneRoomService.Date(offer.DecisionDay), 6);
        desk.ResolveRivalOffersForCheck();
        act = am.GetArtist(id);
        Check(act.labelId == poacher.labelId && !desk.Label.roster.Contains(act) && act.unrecoupedAdvance == offer.Terms.Advance &&
            desk.PendingRenewal == null && desk.RivalOffers.Single(o => o.Id == offer.Id).Status == RivalOfferStatus.Accepted,
            "unrenewed, the act leaves for the poacher's paper");
        desk.TrackRosterDeparturesForCheck();
        Check(desk.RosterDepartures.Single(d => d.ArtistId == id).Kind == RosterDepartureKind.SignedElsewhere, "recorded as signed elsewhere");
        // Renewed on paper that beats the poacher's: the act stays and turns the offer down.
        Restore(atTable, atTableDate, atTableHour, atTableMinute, atTableWorld);
        renewTalk = desk.PendingRenewal.Talk;
        var pending = desk.PendingRivalOffer(id);
        var (losing, winning) = ScoutingCredibleOffersChecks.RivalBoundTerms(desk, renewTalk, pending);
        GD.Print($"RIVAL_COURTSHIP renewal ask=${renewTalk.ask.Advance:N0} poacher={PlayerDesk.RivalTermsText(pending.Terms)} losing={(losing.HasValue ? "$" + losing.Value.Advance : "none")} winning={(winning.HasValue ? "$" + winning.Value.Advance : "none")}");
        Check(winning.HasValue, "some renewal package beats the poacher");
        if (losing.HasValue) Check(!desk.BeatsRivalOfferOf(renewTalk, losing.Value), "a renewal that clears the act's own reservation can still lose to the poacher");
        Check(desk.TableOffer(renewTalk, winning.Value.Advance, winning.Value.RoyaltyRate, winning.Value.TermYears, winning.Value.SinglesObligation,
            winning.Value.LabelOwnsPublishing, winning.Value.ArtistCreativeControl, out message) && desk.PendingRenewal == null &&
            !RosterManager.IsContractMatured(act, clock.CurrentDate.year, week), "paper that beats the poacher renews: " + message);
        desk.ResolveRivalOffersForCheck();
        Check(desk.RivalOffers.Single(o => o.Id == pending.Id).Status == RivalOfferStatus.Declined && act.labelId == desk.Label.labelId, "the poacher is turned down");

        // 10. Who came for the set: resolved once by the room, carried by a heard set, never the room's own crowd.
        Reset();
        var bill = LocalSceneRoomService.Bill(heard.BillId);
        var mine = bill.Appearances.Single(a => ScenePerformanceService.EventId(bill, a) == heard.EventId);
        var aud = mine.Performance?.Audience;
        Check(aud != null && aud.ArrivedFor >= 0 && aud.ArrivedFor <= aud.Present && aud.StayedShare is >= .15f and <= 1f, "the set has an attributed audience");
        int credited = bill.Appearances.Where(a => a.Performance?.Audience != null).Sum(a => a.Performance.Audience.ArrivedFor);
        Check(credited < aud.Present || aud.Present == 0, "the room's own crowd is never credited to any act");
        Check(heard.Coverage < .5f ? heard.SetArrivals == null : heard.SetArrivals == aud.ArrivedFor && heard.SetPresent == aud.Present,
            "a watched set carries what the room showed, only if most of it was watched");
        var record = LocalSceneRoomService.DrawAt(heard.RoomId, id);
        Check(record.HasValue && record.Value.Nights >= 1, "the room keeps a record of the act's draw");
        Check(LocalSceneRoomService.LiveDrawRecognitionPerHead * LocalSceneRoomService.LiveDrawRecognitionHeadCap <= 1.5e-4f,
            "one set's recognition is bounded far below a chart entry");
        string details = string.Join("\n", ScoutingAudience.DrawDetails(desk.ScoutingDossiers.Single(d => d.ArtistId == id)));
        Check(details.Contains("your eyes") && (details.Contains("one room only") || details.Contains("nobody has seen")) && !details.Contains(aud.ArrivedFor + " people"),
            "the notebook says what you saw in bands, and that one room says nothing about travel");
        var two = LocalSceneRoomService.Copy(desk.ScoutingDossiers.Single(d => d.ArtistId == id));
        var away = LocalSceneRoomService.Copy(two.Observations.First(o => o.Kind == ScoutingEvidenceKind.Performance));
        away.Id = "away"; away.EventId = "away-event"; away.RoomId = "controlled-away-room"; away.PlaceId = "controlled-away-town"; away.SetArrivals = 30; away.SetPresent = 80;
        two.Observations.Add(away);
        var home = two.Observations.First(o => o.Kind == ScoutingEvidenceKind.Performance); home.SetArrivals = Math.Max(1, home.SetArrivals ?? 1);
        Check(string.Join("\n", ScoutingAudience.DrawDetails(two)).Contains("across 2 towns"), "draw seen in two towns reads as travelling that far");
        // The booker's count: the fifth question.
        clock.EndDay(); var room = SceneRoomCatalog.Get(heard.RoomId); clock.RestoreClock(clock.CurrentDate, room.OpenHour);
        var answer = desk.PreviewScoutingSource(room.Id, id, ScoutingSourceQuestion.ActDraw);
        Check(answer != null && answer.Available && answer.DrawNights >= 1 && answer.Claim.Contains("come in for their set"), "the booker reports the act's own draw: " + answer?.Claim);
        Check(desk.AskScoutingSource(room.Id, id, ScoutingSourceQuestion.ActDraw, answer.QuoteId, out message), "ask: " + message);
        Check(!desk.AskScoutingSource(room.Id, id, ScoutingSourceQuestion.ActDraw, answer.QuoteId, out _), "the same count is not sold twice");
        details = string.Join("\n", ScoutingAudience.DrawDetails(desk.ScoutingDossiers.Single(d => d.ArtistId == id)));
        Check(details.Contains("a night come in for them"), "the notebook keeps the booker's count with its source");

        Reset();
        GD.Print($"SCOUTING_RIVAL_COURTSHIP_PASS checks={checks}");
    }
}
