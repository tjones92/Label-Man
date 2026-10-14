using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum RivalOfferStatus { Pending, Accepted, Declined, Withdrawn, Lapsed }
/// <summary>How the player knows of the offer: word from somebody who could know (who and when, not the terms), from the act's
/// own side, or not at all -- an offer nobody told the player about still stands, and the act may answer it unseen.</summary>
public enum RivalOfferDisclosure { Rumored, Disclosed, Unknown }

/// <summary>
/// A real offer an AI label has put in front of an unsigned act the player is pursuing, with the day the act will answer
/// it. Made only through the player's crowding roll, by a label the recruitment owner says can reach, afford and roster
/// the act today; signed only by that label, on exactly these terms, on the decision day -- unless the player signs the
/// act first. One record carries the offer, what the player knew of it and when, and how it ended. IDs only.
/// </summary>
public sealed class RivalOffer {
    public string Id { get; set; }
    public string PlayerLabelId { get; set; }
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string RivalLabelId { get; set; }
    public string RivalLabelName { get; set; }
    public int MadeDay { get; set; }
    /// <summary>The act answers the offer at dawn on this day.</summary>
    public int DecisionDay { get; set; }
    public ContractTermSheet Terms { get; set; }
    public RivalOfferDisclosure Disclosure { get; set; }
    /// <summary>When the player first heard of it, and when the act's side told them the terms (if it has).</summary>
    public int KnownDay { get; set; }
    public int? DisclosedDay { get; set; }
    public RivalOfferStatus Status { get; set; }
    public int? ResolvedDay { get; set; }
    /// <summary>The courtship that led here (PlayerDesk.RivalCourtship); null for an offer made before courtships existed.</summary>
    public string CourtshipId { get; set; }
    /// <summary>An offer to one of the player's own acts whose contract has run out: accepted, the act leaves the label.</summary>
    public bool Renewal { get; set; }
}

// ============================================================================================
// RIVAL OFFERS -- Phase 4 of the scouting expansion directive (§9-§10, "actual outside options").
//
// The crowding roll (PlayerDesk.ProcessRivalCrowding) used to give a notebook act a three-day
// "circling" warning and then sign it to the rival. The rival's interest is now an offer: real
// terms from that label's own pricing, a decision day, and a record of what the player was told.
// The act weighs it at the player's table -- the player has to beat the paper, or show why this
// label is the better home -- and takes it on the day unless the player has signed them.
//
// Nothing here runs without a player label, and nothing draws the global RNG: the terms are the
// label's pure advance and royalty pricing plus a stable-hash term, and the signing applies them as
// given. The world write is the same rival signing the crowding roll already made.
// ============================================================================================
public partial class PlayerDesk {
    /// <summary>Days an act takes to answer a rival's offer.</summary>
    public const int RivalOfferDecisionDays = 5;

    private readonly List<RivalOffer> rivalOffers = new();
    public IReadOnlyList<RivalOffer> RivalOffers => LocalSceneRoomService.Copy(rivalOffers).AsReadOnly();

    private int TodayIndex() => LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);

    private RivalOffer PendingOffer(string artistId) => artistId == null ? null :
        rivalOffers.FirstOrDefault(o => o.ArtistId == artistId && o.Status == RivalOfferStatus.Pending);

    /// <summary>The pending offer for this act, detached, if the player knows of one.</summary>
    public RivalOffer PendingRivalOffer(string artistId) => LocalSceneRoomService.Copy(PendingOffer(artistId));

    /// <summary>What the rival would actually sign: its own advance and royalty pricing for this act (pure), the manager's
    /// demands as a term sheet carries them, and the term and sides drawn from a stable hash rather than the global RNG.</summary>
    internal static ContractTermSheet RivalOfferTerms(AILabel rival, SimulatedArtist artist, int day, int year) {
        ManagerProfile.Modifiers mods = ManagerProfile.Of(artist.manager);
        ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
        int Range(string what, int min, int max) =>
            Math.Min(max, min + (int)(StableUnit($"{seed}|{rival.labelId}|{artist.artistId}|{day}|RivalOffer{what}") * (max - min + 1)));
        float advance = Mathf.Ceil(rival.CalculateManagerAdjustedAdvance(artist) / 5f) * 5f;
        float royalty = artist.manager == ManagerArchetype.None ? rival.CalculateRoyaltyRate(artist)
            : Mathf.Clamp(rival.CalculateRoyaltyRate(artist) * mods.RoyaltyDemandMult, 0.02f, 0.15f);
        (int min, int max) span = artist.careerState switch {
            CareerState.Superstar => (1, 3), CareerState.Star => (2, 4), CareerState.Established => (3, 5), CareerState.Rising => (2, 3), _ => (1, 2)
        };
        int bias = (rival.tier is LabelTier.Major or LabelTier.MidTier ? 1 : 0) + (rival.artistLoyalty > 0.65f ? 1 : 0) -
            (rival.tier is LabelTier.Small or LabelTier.Boutique ? 1 : 0);
        int term = Math.Clamp(Range("Term", span.min, span.max) + bias, 1, 7);
        term = artist.manager switch { ManagerArchetype.Shark => Math.Max(1, term - 1), ManagerArchetype.Svengali => Math.Min(7, term + 1), _ => term };
        int singles = year > AILabel.SinglesObligationFinalYear ? 0 : artist.careerState switch {
            CareerState.Superstar or CareerState.Star or CareerState.Established => 0,
            CareerState.Rising => Range("Sides", 4, 8),
            _ => Range("Sides", 3, 6)
        };
        return new ContractTermSheet(advance, royalty, term, singles, !mods.DemandsArtistPublishing, mods.DemandsArtistControl,
            mods.NegotiationDifficulty, artist.manager, artist.managerName, null);
    }

    /// <summary>Whether the act's own side would tell the player about an offer: they are in a terms discussion with the
    /// player, or have sat down with them. Otherwise the player hears only who is making an offer and when it's due.</summary>
    private bool ActWouldDisclose(string artistId) =>
        OpenDiscussions(artistId).Any() ||
        scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations
            .Any(o => o.Kind is ScoutingEvidenceKind.MeetingStatement or ScoutingEvidenceKind.BusinessStatement) == true;

    /// <summary>Every live prospect object holding a terms discussion with this act (the pad and the kept negotiations).</summary>
    private IEnumerable<Prospect> OpenDiscussions(string artistId) =>
        slate.Where(p => p?.Artist?.artistId == artistId && p.HasBaseline)
            .Concat(scoutingNegotiations.TryGetValue(artistId, out var kept) && kept.HasBaseline ? new[] { kept } : Array.Empty<Prospect>())
            .Distinct();

    /// <summary>The rival puts paper in front of the act. The act's own side tells the player if it is talking to them (or
    /// is on the player's roster); otherwise word reaches the player only through a musician who knows the act or talk
    /// around a scene the player has been working, and without the terms. Nobody else, and the offer goes unseen.</summary>
    private RivalOffer MakeRivalOffer(SimulatedArtist artist, AILabel rival, GameDate date, RivalCourtship courtship = null) {
        int today = LocalSceneRoomService.Day(date);
        bool renewal = courtship?.Origin == RivalCourtshipOrigin.Renewal;
        bool disclosed = renewal || ActWouldDisclose(artist.artistId);
        bool rumored = !disclosed && (courtship == null || RosterContactFor(artist) != null || WithinSceneTalk(artist, today));
        var offer = new RivalOffer {
            Id = $"{Label.labelId}|{artist.artistId}|{rival.labelId}|{today}", PlayerLabelId = Label.labelId,
            ArtistId = artist.artistId, ArtistName = artist.stageName, RivalLabelId = rival.labelId, RivalLabelName = rival.labelName,
            MadeDay = today, DecisionDay = today + RivalOfferDecisionDays, Terms = RivalOfferTerms(rival, artist, today, date.year),
            Disclosure = disclosed ? RivalOfferDisclosure.Disclosed : rumored ? RivalOfferDisclosure.Rumored : RivalOfferDisclosure.Unknown,
            KnownDay = today, DisclosedDay = disclosed ? today : null, Status = RivalOfferStatus.Pending,
            CourtshipId = courtship?.Id, Renewal = renewal };
        rivalOffers.Add(offer);
        if (offer.Disclosure != RivalOfferDisclosure.Unknown)
            Note(disclosed
                ? $"{artist.stageName}'s side tells you {rival.labelName} has offered them {RivalTermsText(offer.Terms)}. They answer {rival.labelName} by {LocalSceneRoomService.Date(offer.DecisionDay).ToHeadlineString()}." +
                  (renewal ? " Their contract with you has run out: renew on better paper before then, or they go." : "")
                : $"Word is {rival.labelName} has made {artist.stageName} an offer, and wants an answer by {LocalSceneRoomService.Date(offer.DecisionDay).ToHeadlineString()}. You haven't heard the terms.");
        // An easy act with a real offer in hand no longer signs on one click: it wants to talk terms.
        foreach (Prospect p in OpenDiscussions(artist.artistId).ToList()) {
            if (p.Posture != NegotiationPosture.Pushover || p.Talk != null) continue;
            p.Posture = NegotiationPosture.Firm;
            p.PushoverCountered = false;
            OpenNegotiation(p, ReadContractOutlook(artist, renewal: false));
        }
        // Likewise an easy renewal already opened: the act now has something to weigh it against.
        if (renewal && PendingRenewal?.Artist == artist && PendingRenewal.Posture == NegotiationPosture.Pushover && PendingRenewal.Talk == null) {
            var outlook = ReadContractOutlook(artist, renewal: true);
            PendingRenewal.Posture = NegotiationPosture.Firm;
            ContractTalk talk = BuildTalk(artist, PendingRenewal.Ask, NegotiationPosture.Firm);
            ApplyOutlook(talk, outlook);
            talk.renewalArtist = artist;
            PendingRenewal.Talk = talk;
        }
        return offer;
    }

    /// <summary>Opening or reopening talks: the act's side tells the player what it has been offered.</summary>
    private void DiscloseRivalOffer(string artistId) {
        var offer = PendingOffer(artistId);
        if (offer == null || offer.Disclosure == RivalOfferDisclosure.Disclosed) return;
        if (offer.Disclosure == RivalOfferDisclosure.Unknown) offer.KnownDay = TodayIndex();
        offer.Disclosure = RivalOfferDisclosure.Disclosed; offer.DisclosedDay = TodayIndex();
        Note($"{offer.ArtistName}'s side confirms {offer.RivalLabelName}'s offer: {RivalTermsText(offer.Terms)}, answered by {LocalSceneRoomService.Date(offer.DecisionDay).ToHeadlineString()}.");
    }

    /// <summary>Dawn: every pending offer whose day has come is answered. The act signs with the rival on exactly the terms
    /// offered, if that label still can; otherwise the offer has fallen through. Acts that are gone or signed lapse it.</summary>
    private void ResolveRivalOffers(GameDate date) {
        int today = LocalSceneRoomService.Day(date);
        foreach (RivalOffer offer in rivalOffers.Where(o => o.Status == RivalOfferStatus.Pending).ToList()) {
            var artist = ArtistManager.Instance?.GetArtist(offer.ArtistId);
            if (offer.Renewal) { ResolveRenewalOffer(offer, artist, date, today); EndOfferCourtship(offer, today); continue; }
            if (artist == null || !string.IsNullOrEmpty(artist.labelId) || artist.lifecycleStatus != ArtistLifecycleStatus.Active) {
                offer.Status = artist?.labelId == Label?.labelId && artist?.labelId != null ? RivalOfferStatus.Declined : RivalOfferStatus.Lapsed;
                offer.ResolvedDay = today;
                continue;
            }
            if (today < offer.DecisionDay) continue;
            offer.ResolvedDay = today;
            AILabel taker = RosterManager.Instance?.SignRivalOfferToPlayerAct(artist, date, offer.RivalLabelId, offer.Terms);
            bool seen = offer.Disclosure != RivalOfferDisclosure.Unknown;
            if (taker != null) {
                offer.Status = RivalOfferStatus.Accepted;
                if (seen) Note($"{artist.stageName} took {taker.labelName}'s offer and signed with them.");
            } else {
                offer.Status = RivalOfferStatus.Withdrawn;
                if (seen) Note($"{offer.RivalLabelName}'s offer for {artist.stageName} fell through -- they can't take on the act now. {artist.stageName} is still unsigned.");
            }
            EndOfferCourtship(offer, today);
        }
        foreach (RivalOffer offer in rivalOffers.Where(o => o.Status != RivalOfferStatus.Pending && o.CourtshipId != null)) EndOfferCourtship(offer, today);
    }

    private void EndOfferCourtship(RivalOffer offer, int today) {
        var c = rivalCourtships.FirstOrDefault(x => x.Id == offer.CourtshipId);
        if (c != null && c.Stage == RivalCourtshipStage.Offer && offer.Status != RivalOfferStatus.Pending)
            EndCourtship(c, RivalCourtshipStage.Ended, offer.ResolvedDay ?? today, "offer " + offer.Status.ToString().ToLowerInvariant());
    }

    /// <summary>The decision day on an offer to the player's own act. Renewed in time: turned down. Still on run-out paper
    /// and the rival still able: the act lets its contract end and signs with the rival on exactly those terms.</summary>
    private void ResolveRenewalOffer(RivalOffer offer, SimulatedArtist artist, GameDate date, int today) {
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        bool ours = artist != null && artist.labelId == Label?.labelId && artist.lifecycleStatus == ArtistLifecycleStatus.Active;
        if (!ours || !RosterManager.IsContractMatured(artist, date.year, week)) {
            offer.Status = ours ? RivalOfferStatus.Declined : RivalOfferStatus.Lapsed; offer.ResolvedDay = today;
            if (ours) Note($"{artist.stageName} turned down {offer.RivalLabelName}'s offer and stayed with you.");
            return;
        }
        if (today < offer.DecisionDay) return;
        offer.ResolvedDay = today;
        AILabel able = RosterManager.Instance?.FindRivalForPlayerRenewal(artist, date, offer.RivalLabelId);
        if (able?.labelId != offer.RivalLabelId || !able.CanAffordToSign(offer.Terms.Advance)) {
            offer.Status = RivalOfferStatus.Withdrawn;
            Note($"{offer.RivalLabelName}'s offer for {artist.stageName} fell through. They're still yours on the old paper until you renew.");
            return;
        }
        if (PendingRenewal?.Artist == artist) { if (PendingRenewal.Talk != null) PendingRenewal.Talk.stage = ContractTalkStage.Done; PendingRenewal = null; }
        ArtistManager.Instance?.DropArtist(artist, date.year, Label, ArtistDropReason.ContractExpired);
        maturedNotified.Remove(artist.artistId);
        AILabel taker = RosterManager.Instance?.SignRivalOfferToPlayerAct(artist, date, offer.RivalLabelId, offer.Terms);
        offer.Status = taker != null ? RivalOfferStatus.Accepted : RivalOfferStatus.Withdrawn;
        Note(taker != null
            ? $"{artist.stageName} let their contract with you run out and signed with {taker.labelName}."
            : $"{artist.stageName} let their contract with you run out, but {offer.RivalLabelName}'s deal fell through. They're a free agent.");
    }

    /// <summary>The player signed the act: any offer it had is turned down.</summary>
    private void DeclineRivalOffersFor(SimulatedArtist artist) {
        foreach (RivalOffer offer in rivalOffers.Where(o => o.ArtistId == artist.artistId && o.Status == RivalOfferStatus.Pending)) {
            offer.Status = RivalOfferStatus.Declined; offer.ResolvedDay = TodayIndex();
            Note($"{artist.stageName} turned down {offer.RivalLabelName}'s offer.");
        }
    }

    public static string RivalTermsText(ContractTermSheet t) =>
        $"${t.Advance:N0} up front, {t.RoyaltyRate:P1} royalty, {t.TermYears} {(t.TermYears == 1 ? "year" : "years")}" +
        (t.SinglesObligation > 0 ? $", {t.SinglesObligation} sides owed" : "") +
        (t.LabelOwnsPublishing ? "" : ", they keep the publishing") + (t.ArtistCreativeControl ? ", they keep the final word" : "");

    /// <summary>What the player knows of a pending offer, for the notebook, the pad and the table. Null when none.</summary>
    public string RivalOfferLine(string artistId) {
        var offer = PendingOffer(artistId);
        if (offer == null || offer.Disclosure == RivalOfferDisclosure.Unknown) return null;
        string due = LocalSceneRoomService.Date(offer.DecisionDay).ToHeadlineString();
        return offer.Disclosure == RivalOfferDisclosure.Disclosed
            ? $"{offer.RivalLabelName} has offered them {RivalTermsText(offer.Terms)}. They answer by {due}."
            : $"Word is {offer.RivalLabelName} has made them an offer; they want an answer by {due}. Talk to them to hear the terms.";
    }

    /// <summary>The act's side of a rival offer, said at the table. Facts the act holds; nothing hidden.</summary>
    private string RivalOfferObjectionLine(ContractTalk talk) {
        var offer = PendingOffer(talk.Artist.artistId);
        string who = string.IsNullOrEmpty(talk.ask.ManagerName) ? "They" : talk.ask.ManagerName;
        return offer == null ? "\"There's other paper on the table. Yours has to beat it.\""
            : $"\"{offer.RivalLabelName} has put paper in front of us: {RivalTermsText(offer.Terms)}. " +
              $"{(who == "They" ? "We" : who)} answer{(who == "They" ? "" : "s")} them by {LocalSceneRoomService.Date(offer.DecisionDay).ToHeadlineString()}. " +
              "Beat it, or show us why you're the better home.\"";
    }

    /// <summary>Against a real offer, what each label can do weighs more than in the act's general outlook: with advances
    /// priced to the act (ContractPricing), the paper's money is close, and the buzz, the record and the push behind each
    /// label decide it. Bounded both ways: a label nobody has heard of still has to make up the gap in the terms.</summary>
    private const float RivalEdgeScale = 1.0f, RivalEdgeFloor = -0.30f, RivalEdgeCeiling = 0.15f;

    /// <summary>What this label can do against the offering label, as the act weighs both: the same capability reading the
    /// outlook uses, with the rival's offer in hand (no discount for doubt it will come). Pure.</summary>
    private float RivalOfferEdge(SimulatedArtist artist, RivalOffer offer, ContractOutlook outlook) {
        var rival = CompetitorManager.Instance?.GetLabel(offer.RivalLabelId);
        if (rival == null || outlook?.Priorities == null || outlook.Priorities.Count == 0) return 0f;
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        var theirs = CapabilitiesOf(rival, artist, week, rival.GetProductionCost(), CredibilityOf(rival, 0));
        return Mathf.Clamp(RivalEdgeScale * (Fit(outlook.Priorities, outlook.Player) - Fit(outlook.Priorities, theirs)),
            RivalEdgeFloor, RivalEdgeCeiling);
    }

    /// <summary>The act signs with the player only on paper that beats the rival's offer, counting what each label can do
    /// for it and the player's promises and relationship. A renewal is weighed against an offer made for it at run-out;
    /// an act with no offer always passes.</summary>
    private bool BeatsRivalOffer(ContractTalk talk, ContractTermSheet offer) {
        if (talk?.Artist == null) return true;
        var rival = PendingOffer(talk.Artist.artistId);
        if (rival == null || rival.Renewal != talk.IsRenewal) return true;
        var outlook = ReadContractOutlook(talk.Artist, renewal: talk.IsRenewal);
        float edge = RivalOfferEdge(talk.Artist, rival, outlook) + outlook.Relationship;
        // Both papers are read against one reference. Against the act's own ask the advance term saturates at 1.5x, so
        // a rival's $5,000 would read the same as the player's $45; against the larger of the two, money counts in full.
        ContractTermSheet a = talk.ask;
        var reference = new ContractTermSheet(Mathf.Max(a.Advance, rival.Terms.Advance), a.RoyaltyRate, a.TermYears, a.SinglesObligation,
            a.LabelOwnsPublishing, a.ArtistCreativeControl, a.NegotiationDifficulty, a.Manager, a.ManagerName, a.DemandSummary);
        return PackageValue(offer, reference, talk.weights) + PromiseValue(talk) + edge >= PackageValue(rival.Terms, reference, talk.weights);
    }

    /// <summary>Fixture access: whether this package beats the pending offer. Never rendered.</summary>
    internal bool BeatsRivalOfferOf(ContractTalk talk, ContractTermSheet offer) => BeatsRivalOffer(talk, offer);

    /// <summary>Fixture access: make the offer a reachable rival would make today. Never rendered.</summary>
    internal RivalOffer MakeRivalOfferForCheck(SimulatedArtist artist, AILabel rival) =>
        LocalSceneRoomService.Copy(MakeRivalOffer(artist, rival, TimeManager.Instance?.CurrentDate ?? GameDate.StartDate));

    /// <summary>Fixture access: run the dawn answer for today. Never rendered.</summary>
    internal void ResolveRivalOffersForCheck() => ResolveRivalOffers(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);

    /// <summary>Fixture access: the ordinary acceptance check (reservation, floor, hard line), without any rival. Never rendered.</summary>
    internal bool ClearsReservationForCheck(ContractTalk talk, ContractTermSheet offer) => ClearsReservation(talk, offer, AcceptanceCredit(talk));

    /// <summary>Fixture access: everything the acceptance check adds to the package. Never rendered.</summary>
    internal float AcceptanceCreditForCheck(ContractTalk talk) => AcceptanceCredit(talk);

    private List<RivalOffer> CaptureRivalOffers() => rivalOffers.Count == 0 ? null : LocalSceneRoomService.Copy(rivalOffers);

    private static bool ValidateRivalOffers(List<RivalOffer> saved, string labelId, out string message) {
        message = "The saved rival offers are invalid; no player state was restored.";
        if (saved == null) return true; // Older saves hold no offers; none are invented.
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var o in saved) {
            if (o == null || string.IsNullOrEmpty(o.Id) || !ids.Add(o.Id) || o.PlayerLabelId != labelId || string.IsNullOrEmpty(o.ArtistId) ||
                string.IsNullOrEmpty(o.RivalLabelId) || !Enum.IsDefined(o.Status) || !Enum.IsDefined(o.Disclosure) ||
                o.MadeDay < 0 || o.DecisionDay < o.MadeDay || o.KnownDay < o.MadeDay ||
                (o.Disclosure == RivalOfferDisclosure.Disclosed) != o.DisclosedDay.HasValue || o.DisclosedDay < o.KnownDay ||
                (o.Status == RivalOfferStatus.Pending) == o.ResolvedDay.HasValue || o.ResolvedDay < o.MadeDay ||
                !float.IsFinite(o.Terms.Advance) || o.Terms.Advance < 0f || !float.IsFinite(o.Terms.RoyaltyRate) ||
                o.Terms.RoyaltyRate < 0f || o.Terms.RoyaltyRate > 0.15f || o.Terms.TermYears is < 1 or > 7 || o.Terms.SinglesObligation < 0 ||
                saved.Count(x => x?.ArtistId == o.ArtistId && x.Status == RivalOfferStatus.Pending) > 1) return false;
        }
        return true;
    }

    private void RestoreRivalOffers(List<RivalOffer> saved) {
        rivalOffers.Clear();
        if (saved != null) rivalOffers.AddRange(LocalSceneRoomService.Copy(saved));
    }
}
