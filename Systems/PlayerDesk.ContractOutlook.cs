using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>What a label can actually do for an act, as the act weighs it. Not contract terms: these are read
/// from the label's real state, so a small label cannot promise its way into reach it does not have.</summary>
public enum CapabilityAxis { Reach, Promotion, Recording, Attention, Local }

// ============================================================================================
// THE CONTRACT OUTLOOK -- Phase 4 of the scouting expansion directive (§10, "what can this label
// actually do?"). The reservation used to be a fixed fraction of the act's own ask, so every label
// faced the same threshold. Here the act compares this label against its real alternative:
//
//   * what the label can do, read from its state (reach, promotion record, recording money,
//     attention per act, being local) -- never from a checkbox;
//   * what the act weighs, from its existing ambition, pragmatism and prospects;
//   * its outside option -- staying as it is (how much it is working, from the room ledger) or the
//     strongest AI label that could actually reach it (SceneRecruitmentService), has roster room and
//     wants its genre. That is a modeled belief about the market, never an offer, and nothing here
//     writes AI state. A rival's actual offer is PlayerDesk.RivalOffers: the act states it here and the
//     acceptance check weighs it separately, so the belief and the offer are never counted twice;
//   * the relationship: time spent with the act away from the stage, earlier walk-outs, the promises
//     this label kept or broke, introductions and word of mouth (PlayerDesk.Reputation).
//
// The result is a bounded credit (often negative for a label nobody has heard of) that the existing
// acceptance check adds to the package. Pure and deterministic: no GD.Rand*, no world mutation, so a
// refresh or a reload reads the same outlook. The numbers stay internal; the player sees only what the
// act says and what their own label is.
// ============================================================================================
public sealed class ContractOutlook {
    public Dictionary<CapabilityAxis, float> Priorities = new();
    public Dictionary<CapabilityAxis, float> Player = new();
    public Dictionary<CapabilityAxis, float> Alternative = new();
    public float PlayerFit, AlternativeFit;
    /// <summary>How likely the act thinks it is that a better-placed label will want it (0-1).</summary>
    public float Prospects;
    /// <summary>How much the act is working without a label (0-1).</summary>
    public float Work;
    /// <summary>Package-value credit for this label against the act's alternative. Negative when the act has better options.</summary>
    public float Standing;
    public float Relationship;
    /// <summary>Added to the reservation on top of any withheld room: an act playing every week is in no hurry.</summary>
    public float ReservationLift;
    /// <summary>Share of its usual give-room the act withholds from this label because it doubts it (0-0.8).</summary>
    public float RoomWithheld;
    /// <summary>How far the act believes this label's claims of reach, promotion and recording (0.35-1).</summary>
    public float Credibility = 1f;
    /// <summary>What the act risks by signing (0-1): a working week, a shot at something better, songs it cares about.</summary>
    public float Stake;
    /// <summary>Stake times disbelief: the act's reason to hold out against this particular label.</summary>
    public float Doubt;
    /// <summary>Internal only: the label the act's belief is built on. Never shown or named to the player.</summary>
    public string RivalLabelId;
    /// <summary>A real offer the act holds from another label (PlayerDesk.RivalOffers), if any. Unlike the belief above, it
    /// is named and its terms stated, because the act has it in hand.</summary>
    public string CompetingOfferId;
    /// <summary>The axis the act weighs where this label trails its alternative the most, if it trails.</summary>
    public CapabilityAxis? Gap;
    /// <summary>What the act and its people actually said, for the negotiation page. No numbers, no hidden values.</summary>
    public List<string> Stated = new();
    public float Credit => Standing + Relationship;
}

public partial class PlayerDesk {
    private const float OutlookStandingScale = 0.6f;
    private const float OutlookStandingFloor = -0.22f, OutlookStandingCeiling = 0.08f;
    private const float OutlookRelationshipFloor = -0.08f, OutlookRelationshipCeiling = 0.06f;
    private const float OutlookWorkLift = 0.05f;
    /// <summary>A Pushover-by-temperament act with at least this much at stake (negative standing or a working week)
    /// still sits down and talks terms.</summary>
    private const float OutlookFirmStanding = -0.03f, OutlookFirmWork = 0.45f, OutlookHardballStanding = -0.15f;

    // ========================================================================================
    // WHAT A LABEL CAN DO
    // ========================================================================================

    /// <summary>A label's capabilities as one act sees them. Same reading for the player and for the AI label an act
    /// measures it against, so neither side gets a formula of its own.</summary>
    /// <param name="sessionUnit">What one session costs this label: the player's cheapest room, an AI label's production cost.</param>
    /// <param name="credibility">How far the act believes the label's claims (see <see cref="CredibilityOf"/>).</param>
    internal static Dictionary<CapabilityAxis, float> CapabilitiesOf(AILabel label, SimulatedArtist artist, int week, float sessionUnit, float credibility) {
        if (label == null) return IndependentCapabilities(0f);
        float sessions = Mathf.Max(0f, label.cashReserves) / Mathf.Max(1f, sessionUnit);
        int others = label.roster?.Count(a => a != artist) ?? 0;
        return new() {
            // Distribution the label has: its own trucks and wholesalers, plus any national access it holds or borrows.
            [CapabilityAxis.Reach] = credibility * Mathf.Clamp(0.6f * label.effectiveNationalReach + 0.4f * label.ownedReach, 0f, 1f),
            // Achievable promotion: what it spends on marketing and what it has actually charted.
            [CapabilityAxis.Promotion] = credibility * Mathf.Clamp(0.45f * label.marketingPower + 0.35f * Mathf.Min(1f, label.top40Hits / 4f) +
                0.20f * label.reputation, 0f, 1f),
            // Funded recording: how many sessions its cash would pay for, and the quality of what comes out.
            [CapabilityAxis.Recording] = credibility * Mathf.Clamp(0.5f * Mathf.Min(1f, sessions / 6f) + 0.5f * label.productionQuality, 0f, 1f),
            // Attention it can realistically give: a short roster is the small label's advantage, and the act can see it.
            [CapabilityAxis.Attention] = Mathf.Clamp(1f - others / 10f, 0.10f, 0.95f),
            [CapabilityAxis.Local] = LocalityOf(label, artist, week),
        };
    }

    /// <summary>Distribution, promotion and recording are claims until a label has something to show for them: records
    /// out, records that charted, and (for the player) promises kept. Attention and being local the act can see for
    /// itself, so they are never discounted. An established AI label reads as fully credible.</summary>
    internal static float CredibilityOf(AILabel label, int keptPromises) => label == null ? 0f :
        Mathf.Clamp(0.35f + 0.08f * Mathf.Min(5, label.totalReleases) + 0.10f * Mathf.Min(3, label.top40Hits) + 0.04f * Mathf.Min(3, keptPromises), 0.35f, 1f);

    /// <summary>AI labels carry a scene geography and use the recruitment owner's own routes. The player's label is
    /// founded in a town without one, so its home town is compared with the act's base scene: the same scene is
    /// local, a short drive is the circuit, anything else is out of town.</summary>
    private static float LocalityOf(AILabel label, SimulatedArtist artist, int week) {
        if (!string.IsNullOrEmpty(label.geography?.basePlaceId)) return SceneRecruitmentService.Explain(label, artist, week).Locality;
        string home = LocalScenePersistenceService.SceneIdFor(label.homeCityId) ?? label.homeCityId;
        string actScene = LocalScenePersistenceService.SceneIdFor(artist?.geography?.basePlaceId);
        if (home == null || actScene == null) return 0f;
        if (home == actScene) return 1f;
        float miles = DistanceModel.GetRoadMilesBetween(home, actScene);
        return float.IsFinite(miles) && miles > 0f && miles <= 225f ? 0.5f : 0f;
    }

    /// <summary>Staying as they are: their own people and their own rooms, nobody to put a record out.</summary>
    private static Dictionary<CapabilityAxis, float> IndependentCapabilities(float work) => new() {
        [CapabilityAxis.Reach] = 0f, [CapabilityAxis.Promotion] = 0f,
        [CapabilityAxis.Recording] = 0.05f + 0.10f * work,
        [CapabilityAxis.Attention] = 1f, [CapabilityAxis.Local] = 1f,
    };

    /// <summary>What an act imagines a proven label would give it, if it waited for one: some distribution, a real push,
    /// paid sessions, not much of anyone's time, and probably not in its own town. The act's picture, never a real offer.</summary>
    private static readonly Dictionary<CapabilityAxis, float> ProvenLabel = new() {
        [CapabilityAxis.Reach] = 0.35f, [CapabilityAxis.Promotion] = 0.35f, [CapabilityAxis.Recording] = 0.55f,
        [CapabilityAxis.Attention] = 0.70f, [CapabilityAxis.Local] = 0.50f,
    };
    /// <summary>Waiting for a label nobody has met yet is worth half of one that could actually reach them.</summary>
    private const float WaitingDiscount = 0.5f;
    /// <summary>How much of its usual give-room an act withholds per unit of doubt, and the most it ever withholds.</summary>
    private const float DoubtRoomShare = 1.5f, MaxRoomWithheld = 0.8f;
    /// <summary>Stake times doubt at or above which an easy act still talks terms, and the level that makes it Hardball.</summary>
    private const float OutlookFirmDoubt = 0.12f, OutlookHardballDoubt = 0.45f;

    // ========================================================================================
    // WHAT THE ACT WEIGHS
    // ========================================================================================

    /// <summary>The existing evolution profile, read for capabilities instead of terms. Pragmatic acts want the record
    /// in shops and pushed; ambitious acts want studio time and a label that works for them; acts with modest
    /// prospects value somebody who knows their rooms.</summary>
    internal static Dictionary<CapabilityAxis, float> CapabilityPriorities(SimulatedArtist artist, float prospects) {
        float ambition = Mathf.Clamp(artist?.evolution?.artisticAmbition ?? 0.5f, 0f, 1f);
        float pragmatism = Mathf.Clamp(artist?.evolution?.commercialPragmatism ?? 0.5f, 0f, 1f);
        var w = new Dictionary<CapabilityAxis, float> {
            [CapabilityAxis.Reach] = 0.15f + 0.35f * pragmatism + 0.20f * prospects,
            [CapabilityAxis.Promotion] = 0.10f + 0.25f * pragmatism,
            [CapabilityAxis.Recording] = 0.10f + 0.30f * ambition,
            [CapabilityAxis.Attention] = 0.10f + 0.25f * ambition,
            [CapabilityAxis.Local] = 0.10f + 0.15f * (1f - prospects),
        };
        float total = w.Values.Sum();
        foreach (CapabilityAxis axis in w.Keys.ToList()) w[axis] /= total;
        return w;
    }

    private static float Fit(Dictionary<CapabilityAxis, float> priorities, Dictionary<CapabilityAxis, float> caps) =>
        priorities.Sum(kv => kv.Value * caps.GetValueOrDefault(kv.Key));

    /// <summary>How busy the act is without a label: rooms it is booked into, a standing engagement, and how long it
    /// has worked these rooms. Read from the room ledger only.</summary>
    private static float WorkLevel((IReadOnlyList<string> Booked, string Engagement, int Shows) work) =>
        Mathf.Clamp(0.18f * work.Booked.Count + (work.Engagement != null ? 0.40f : 0f) + Mathf.Min(0.30f, work.Shows / 40f), 0f, 1f);

    /// <summary>What the act believes its chances are with a bigger label, from things the act itself knows: its
    /// public standing, its heat, its working life, and whether it has held a contract before. Never hidden talent.</summary>
    private static float ProspectsOf(SimulatedArtist artist, float work) {
        bool experienced = artist.contractSequence > 0 || artist.careerStateBeforeDrop != CareerState.Unsigned;
        return Mathf.Clamp(0.45f * artist.reputation + 0.35f * artist.momentum + 0.35f * work + (experienced ? 0.15f : 0f), 0f, 1f);
    }

    // ========================================================================================
    // THE OUTLOOK
    // ========================================================================================

    /// <summary>Reads the outlook for this act and this label now. Deterministic and side-effect free.</summary>
    internal ContractOutlook ReadContractOutlook(SimulatedArtist artist, bool renewal) {
        var outlook = new ContractOutlook();
        if (artist == null || Label == null || !LocalScenes.ContractOutlook) return outlook;
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        int today = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);

        var work = LocalSceneRoomService.ObserveActWork(artist.artistId, today);
        outlook.Work = WorkLevel(work);
        outlook.Prospects = ProspectsOf(artist, outlook.Work);
        outlook.Priorities = CapabilityPriorities(artist, outlook.Prospects);
        int keptByLabel = contractCommitments.Count(c => c.LabelId == Label.labelId && c.Status == ContractCommitmentStatus.Kept);
        outlook.Credibility = CredibilityOf(Label, keptByLabel);
        outlook.Player = CapabilitiesOf(Label, artist, week, SessionPromiseQuote(), outlook.Credibility);
        var independent = IndependentCapabilities(outlook.Work);
        float independentFit = Fit(outlook.Priorities, independent);

        // The strongest label that could actually reach them, has room, and wants what they play -- or, if none can,
        // the proven label they imagine they could wait for, at a discount. Either pull is scaled by how likely the
        // act thinks it is to be noticed at all.
        AILabel rival = null; float rivalPull = 0f; Dictionary<CapabilityAxis, float> rivalCaps = null;
        foreach (AILabel label in CompetitorManager.Instance?.GetOperatingLabels() ?? new List<AILabel>()) {
            if (label == null || label.isPlayerOwned || label.labelId == Label.labelId || label.IsSubsidiary || !label.HasOperatingRosterSpace) continue;
            float genre = label.preferredGenres?.Contains(artist.primaryGenre) == true ? 1f
                : label.secondaryGenres?.Contains(artist.primaryGenre) == true ? 0.6f : 0f;
            if (genre <= 0f || !SceneRecruitmentService.Explain(label, artist, week).Eligible) continue;
            var caps = CapabilitiesOf(label, artist, week, label.GetProductionCost(), CredibilityOf(label, 0));
            float pull = genre * Mathf.Max(0f, Fit(outlook.Priorities, caps) - independentFit);
            if (pull > rivalPull || (pull == rivalPull && pull > 0f && string.CompareOrdinal(label.labelId, rival?.labelId) < 0)) {
                rival = label; rivalPull = pull; rivalCaps = caps;
            }
        }
        float waitingPull = WaitingDiscount * Mathf.Max(0f, Fit(outlook.Priorities, ProvenLabel) - independentFit);
        bool waiting = rival == null;
        float pullUsed = Mathf.Max(waitingPull, rivalPull);
        var alternativeCaps = waiting || waitingPull > rivalPull ? ProvenLabel : rivalCaps;
        float chance = outlook.Prospects;
        outlook.RivalLabelId = waiting ? null : rival?.labelId;
        foreach (CapabilityAxis axis in Enum.GetValues<CapabilityAxis>()) {
            float stay = independent[axis];
            outlook.Alternative[axis] = alternativeCaps == null ? stay : Mathf.Lerp(stay, Mathf.Max(stay, alternativeCaps[axis]), chance);
        }
        outlook.PlayerFit = Fit(outlook.Priorities, outlook.Player);
        outlook.AlternativeFit = independentFit + chance * pullUsed;
        outlook.Standing = Mathf.Clamp(OutlookStandingScale * (outlook.PlayerFit - outlook.AlternativeFit), OutlookStandingFloor, OutlookStandingCeiling);
        // What the act risks by tying itself to this label: a working week, a shot at something better, songs it cares
        // about. Against a label with nothing to show yet, that stake becomes how much of its ask the act holds out for.
        float ambition = Mathf.Clamp(artist.evolution?.artisticAmbition ?? 0.5f, 0f, 1f);
        outlook.Stake = Mathf.Clamp(Mathf.Max(Mathf.Max(outlook.Work, outlook.Prospects), 1.6f * (ambition - 0.45f)), 0f, 1f);
        outlook.Doubt = outlook.Stake * (1f - outlook.Credibility);
        outlook.ReservationLift = OutlookWorkLift * outlook.Work;
        outlook.RoomWithheld = Mathf.Clamp(DoubtRoomShare * outlook.Doubt, 0f, MaxRoomWithheld);
        outlook.Relationship = RelationshipCredit(artist, renewal, outlook.Stated);
        var offer = renewal ? null : PendingOffer(artist.artistId);
        outlook.CompetingOfferId = offer?.Id;

        float worst = 0f;
        foreach (CapabilityAxis axis in Enum.GetValues<CapabilityAxis>()) {
            float behind = outlook.Priorities[axis] * Mathf.Max(0f, outlook.Alternative[axis] - outlook.Player[axis]);
            if (behind > worst + 0.0001f) { worst = behind; outlook.Gap = axis; }
        }
        if (outlook.Standing > OutlookFirmStanding) outlook.Gap = null;
        // Nothing better within reach, but the act still doubts an unproven label: the objection is whichever claim it
        // weighs most that the label can't yet back up.
        if (outlook.Gap == null && outlook.Doubt >= OutlookFirmDoubt)
            outlook.Gap = new[] { CapabilityAxis.Reach, CapabilityAxis.Promotion, CapabilityAxis.Recording }
                .OrderByDescending(a => outlook.Priorities[a] * Mathf.Max(0f, ProvenLabel[a] - outlook.Player[a])).ThenBy(a => a).First();
        StateOutlook(artist, outlook, work, offer == null && !waiting && chance * pullUsed >= 0.04f,
            offer == null && waiting && chance * waitingPull >= 0.03f, ambition, offer == null ? null : RivalOfferLine(artist.artistId));
        return outlook;
    }

    /// <summary>Trust follows treatment, bounded so no cheap repeated action buys a signature: time spent with the act
    /// away from the stage, talks that broke down before, the promises this label kept or broke to them (at renewal or
    /// on coming back), an introduction through somebody on the roster, an earlier stay on the label, and what their
    /// people have heard about how the label treats others (PlayerDesk.Reputation).</summary>
    private float RelationshipCredit(SimulatedArtist artist, bool renewal, List<string> stated) {
        float credit = 0f;
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artist.artistId);
        var seen = dossier?.Observations ?? new List<ScoutingObservation>();
        if (seen.Any(o => o.Kind is ScoutingEvidenceKind.RehearsalSample or ScoutingEvidenceKind.DemoSample)) {
            credit += 0.02f; stated.Add("You've already put time into them away from the stage, and they know it.");
        } else if (seen.Any(o => o.Kind == ScoutingEvidenceKind.MeetingStatement)) {
            credit += 0.015f; stated.Add("You've sat down with them before; this isn't a cold call.");
        }
        int walkouts = seen.Count(o => o.Kind == ScoutingEvidenceKind.BusinessStatement && o.BusinessStatementKind == ScoutingBusinessStatementKind.Declined);
        if (walkouts > 0) { credit -= Mathf.Min(0.03f, 0.015f * walkouts); stated.Add("They remember talks with you breaking down before."); }
        {
            var mine = contractCommitments.Where(c => c.ArtistId == artist.artistId && c.LabelId == Label.labelId).ToList();
            int kept = mine.Count(c => c.Status == ContractCommitmentStatus.Kept);
            int missed = mine.Count(c => c.Status == ContractCommitmentStatus.Missed);
            int answered = mine.Count(c => c.Status == ContractCommitmentStatus.Missed && c.RemedyApplied);
            credit += Mathf.Min(0.05f, 0.025f * kept) - 0.04f * (missed - answered) - 0.015f * answered;
            if (kept > 0) stated.Add(kept == 1 ? "You kept the promise you made them last time." : $"You kept the {kept} promises you made them.");
            if (missed > 0) stated.Add(missed == 1 ? "They haven't forgotten the promise you broke." : $"They haven't forgotten the {missed} promises you broke.");
        }
        if (!renewal) credit += IntroductionCreditFor(artist, stated) + FormerActCreditFor(artist, stated);
        credit += WordOfMouthCredit(artist, stated);
        return Mathf.Clamp(credit, OutlookRelationshipFloor, OutlookRelationshipCeiling);
    }

    /// <summary>The page shows what the act and its people said, built only from facts the game holds: rooms they are
    /// booked into, what they keep asking about, and a belief about the market, which is never presented as an offer.</summary>
    private static void StateOutlook(SimulatedArtist artist, ContractOutlook outlook,
            (IReadOnlyList<string> Booked, string Engagement, int Shows) work, bool believesInRival, bool willWait, float ambition, string offerLine = null) {
        var lines = new List<string>();
        if (outlook.Doubt >= OutlookFirmDoubt)
            lines.Add(outlook.Credibility <= 0.36f
                ? "They've never heard of your label. What you say about distribution or the studio counts for little until there's a record to show for it."
                : "Your label is still new to them. They'll weigh what you've actually put out over what you say you'll do.");
        var rooms = work.Booked.Distinct(StringComparer.Ordinal).Select(id => SceneRoomCatalog.Get(id)?.Name).Where(n => n != null).Take(2).ToList();
        string engagement = SceneRoomCatalog.Get(work.Engagement)?.Name;
        if (engagement != null) lines.Add($"They hold a standing engagement at {engagement}. They aren't short of work.");
        else if (rooms.Count > 0) lines.Add($"They're booked at {string.Join(" and ", rooms)} in the coming days{(outlook.Work >= OutlookFirmWork ? ". They aren't short of work." : ".")}");
        else if (work.Shows >= 10) lines.Add("They've played these rooms for a while, though nothing is on the calendar right now.");
        else lines.Add("They aren't working much right now.");
        if (offerLine != null) lines.Add(offerLine);
        if (believesInRival)
            lines.Add("They believe a label with wider distribution could take an interest in them, and they'd want to hear it out. No offer has been made that you know of.");
        else if (willWait)
            lines.Add("They're in no hurry. They'd sooner wait for a label with a name than sign the wrong paper.");
        if (ambition >= 0.70f) lines.Add("They care what happens to their songs.");
        CapabilityAxis top = outlook.Priorities.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
        lines.Add(top switch {
            CapabilityAxis.Reach => "They keep asking who would get a record into shops outside town.",
            CapabilityAxis.Promotion => "They want to know who would push a record once it's out.",
            CapabilityAxis.Recording => "They want to know there's money for the studio.",
            CapabilityAxis.Attention => "They want a label that would actually work for them, not file them.",
            _ => "They'd rather deal with somebody who knows these rooms.",
        });
        outlook.Stated.InsertRange(0, lines);
    }

    /// <summary>What the act says it is weighing, for a page with no open talk (an easy signing or renewal).</summary>
    public IReadOnlyList<string> ContractOutlookLines(SimulatedArtist artist, bool renewal) =>
        ReadContractOutlook(artist, renewal).Stated.AsReadOnly();

    /// <summary>The player's own label, in facts the player already has: no score, nothing about the act.</summary>
    public string OwnLabelSummary() {
        if (Label == null) return "";
        int acts = Label.CurrentRosterSize;
        float runway = Label.cashReserves / Mathf.Max(1f, Label.GetMonthlyOverhead());
        string distribution = Label.activeDeal != null ? "a distribution deal in force"
            : Label.nationalReach >= 0.3f ? "national distribution of its own"
            : Label.ownedReach >= 0.2f ? "regional distribution of its own" : "no distribution beyond what you carry yourself";
        return $"Your label: {acts} {(acts == 1 ? "act" : "acts")} on the roster, {Label.totalReleases} {(Label.totalReleases == 1 ? "release" : "releases")}, " +
            $"{Label.top40Hits} Top 40; {distribution}; " +
            (runway > 24f ? "more than two years of overhead in the bank." : $"about {Mathf.FloorToInt(Mathf.Max(0f, runway))} months of overhead in the bank.");
    }

    /// <summary>The capability objection: what they say when the terms are not the problem, the label is.</summary>
    private static string CapabilityObjectionLine(ContractTalk talk) {
        string manager = talk.ask.ManagerName;
        string who = string.IsNullOrEmpty(manager) ? "They" : manager;
        return talk.capabilityObjection switch {
            CapabilityAxis.Reach => $"\"{who} need{(who == "They" ? "" : "s")} to know who gets the record into shops past this town. Nobody's heard of your distribution.\"",
            CapabilityAxis.Promotion => "\"Nobody's heard of your label. Who's going to push the record once it's out?\"",
            CapabilityAxis.Recording => "\"How do they know there's money for a proper session? Paper's cheap.\"",
            CapabilityAxis.Attention => "\"They're doing all right on their own. What would you actually do for them that they can't?\"",
            CapabilityAxis.Local => "\"You're not from around here. They want somebody who knows these rooms.\"",
            _ => "\"It isn't the numbers. They're not sure what a label like yours can do for them.\"",
        };
    }

    /// <summary>Posture is temperament plus stakes: an act with a real alternative, or a full week of work, is not a
    /// one-click signing for a label that can't match it.</summary>
    internal static NegotiationPosture PostureFor(SimulatedArtist artist, ContractOutlook outlook) {
        NegotiationPosture posture = PostureOf(artist);
        if (outlook == null) return posture;
        // An act with another label's paper in hand talks terms with anybody.
        if (posture == NegotiationPosture.Pushover && outlook.CompetingOfferId != null) posture = NegotiationPosture.Firm;
        if (posture == NegotiationPosture.Pushover && (outlook.Standing <= OutlookFirmStanding || outlook.Work >= OutlookFirmWork ||
                outlook.Doubt >= OutlookFirmDoubt))
            posture = NegotiationPosture.Firm;
        if (posture == NegotiationPosture.Firm && (outlook.Standing <= OutlookHardballStanding || outlook.Doubt >= OutlookHardballDoubt))
            posture = NegotiationPosture.Hardball;
        return posture;
    }

    /// <summary>Copies the hidden parts of the outlook onto the talk, where the acceptance check and the save read them.</summary>
    private static void ApplyOutlook(ContractTalk talk, ContractOutlook outlook) {
        if (talk == null || outlook == null) return;
        talk.outlookCredit = outlook.Credit;
        talk.capabilityGap = outlook.Gap;
        // Doubt takes away part of what the act would normally concede; a working week adds a little on top.
        float room = Mathf.Max(0f, 1f - talk.reservation);
        float lifted = Mathf.Min(0.97f, talk.reservation + room * outlook.RoomWithheld + outlook.ReservationLift);
        talk.outlookLift = Mathf.Max(0f, lifted - talk.reservation);
        talk.reservation = lifted;
        talk.outlookStated = outlook.Stated.ToList();
    }

    /// <summary>Everything the acceptance check adds to the package: promises on the table, and the outlook.</summary>
    private float AcceptanceCredit(ContractTalk talk) => PromiseValue(talk) + (talk?.outlookCredit ?? 0f);

    /// <summary>The label is the objection when the terms would clear against the act's ordinary reservation and only
    /// the outlook (its doubt, or a better alternative) holds the deal back, or when that outweighs the worst single
    /// term. Money below the act's floor is always the money.</summary>
    private static CapabilityAxis? CapabilityObjectionFor(ContractTalk talk, ContractTermSheet offer) {
        if (talk?.capabilityGap == null) return null;
        float outlookPressure = talk.outlookLift - Mathf.Min(0f, talk.outlookCredit);
        if (outlookPressure <= 0.005f) return null;
        if (talk.ask.Advance > 0f && offer.Advance < MinimumAdvanceForAcceptance(talk)) return null;
        bool termsClear = PackageValue(offer, talk.ask, talk.weights) >= talk.reservation - talk.outlookLift;
        float worstTerm = talk.weights.Max(kv => kv.Value * Mathf.Max(0f, 1f - AxisTerm(kv.Key, offer, talk.ask)));
        return termsClear || outlookPressure >= worstTerm ? talk.capabilityGap : null;
    }

    /// <summary>Fixture access to the hidden acceptance credit. Never rendered.</summary>
    internal static float OutlookCreditOf(ContractTalk talk) => talk?.outlookCredit ?? 0f;
}
