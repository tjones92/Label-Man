using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum RosterDepartureKind { TalksBrokeDown, Disbanded, Retired, SignedElsewhere, Other }

/// <summary>An act leaving the player's roster, recorded the dawn it is found gone, with how things stood: whether any
/// promise to it was still broken and unanswered when it went. Historical; nothing rebuilds it from the roster later.</summary>
public sealed class RosterDeparture {
    public string LabelId { get; set; }
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public int Day { get; set; }
    public RosterDepartureKind Kind { get; set; }
    /// <summary>Promises this label broke to the act and never answered, as of the day it left.</summary>
    public int UnansweredBroken { get; set; }
    public bool Founding { get; set; }
}

// ============================================================================================
// TRUST FOLLOWS TREATMENT -- Phase 4/6 of the scouting expansion directive (§5, §10-§11).
//
// What an act has heard about this label comes from people who could have told it: a member who
// knows somebody on the label's roster (the contact network), people who know an act this label
// made promises to, and -- for the first name on the label -- the town that act calls home. Each is
// bounded, dated and causal: no story without a connection, none older than three years, and a
// fair departure carries no penalty at all. Player-side only; read by the contract outlook.
// ============================================================================================
public partial class PlayerDesk {
    /// <summary>How long a kept or broken promise is still talked about.</summary>
    public const int WordOfMouthDays = 3 * 365;
    private const float BrokenWordCredit = -0.02f, FoundingBrokenWordCredit = -0.03f;
    private const float KeptWordCredit = 0.01f, FoundingKeptWordCredit = 0.015f;
    private const float WordOfMouthFloor = -0.05f, WordOfMouthCeiling = 0.02f;
    private const float IntroductionCredit = 0.015f, FalloutCredit = -0.01f, FormerActCredit = 0.01f;

    private readonly List<RosterDeparture> rosterDepartures = new();
    private readonly HashSet<string> rosterSeen = new(StringComparer.Ordinal);
    public IReadOnlyList<RosterDeparture> RosterDepartures => LocalSceneRoomService.Copy(rosterDepartures).AsReadOnly();

    /// <summary>What the act believes its chances are with a bigger label, read now. Pure.</summary>
    private float ProspectsNow(SimulatedArtist artist) => artist == null ? 0f :
        ProspectsOf(artist, WorkLevel(LocalSceneRoomService.ObserveActWork(artist.artistId, TodayIndex())));

    private static string SceneOf(SimulatedArtist artist) => LocalScenePersistenceService.SceneIdFor(artist?.geography?.basePlaceId);
    private static string TownName(string sceneId) => ScenePlaceRegistry.Get(sceneId)?.Name ?? DistanceModel.GetCityById(sceneId)?.name ?? "town";

    // ========================================================================================
    // DEPARTURES
    // ========================================================================================

    /// <summary>Dawn: an act that was on the roster yesterday and isn't today has left. Recorded once, with how it left
    /// and whether a broken promise went with it.</summary>
    private void TrackRosterDepartures(GameDate date) {
        if (Label?.roster == null) return;
        var now = Label.roster.Where(a => a != null).Select(a => a.artistId).ToHashSet(StringComparer.Ordinal);
        foreach (string id in rosterSeen.Where(id => !now.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToList()) {
            var artist = ArtistManager.Instance?.GetArtist(id);
            var kind = artist == null ? RosterDepartureKind.Other
                : artist.lifecycleStatus == ArtistLifecycleStatus.Disbanded ? RosterDepartureKind.Disbanded
                : artist.lifecycleStatus == ArtistLifecycleStatus.Retired ? RosterDepartureKind.Retired
                : !string.IsNullOrEmpty(artist.labelId) && artist.labelId != Label.labelId ? RosterDepartureKind.SignedElsewhere
                : artist.lastDropReason == ArtistDropReason.ContractExpired ? RosterDepartureKind.TalksBrokeDown
                : RosterDepartureKind.Other;
            int broken = contractCommitments.Count(c => c.ArtistId == id && c.LabelId == Label.labelId &&
                c.Status == ContractCommitmentStatus.Missed && !c.RemedyApplied);
            bool founding = scoutingKnowledge.FoundingAct?.ArtistId == id;
            rosterDepartures.Add(new RosterDeparture { LabelId = Label.labelId, ArtistId = id, ArtistName = artist?.stageName ?? id,
                Day = LocalSceneRoomService.Day(date), Kind = kind, UnansweredBroken = broken, Founding = founding });
            if (founding && broken > 0)
                Note($"{artist?.stageName ?? "Your first act"} have left the label with your broken promise unanswered. Word of it is all over {TownName(SceneOf(artist))}.");
            else if (founding)
                Note($"{artist?.stageName ?? "Your first act"}, the first name on the label, have moved on. You parted on fair terms.");
        }
        rosterSeen.Clear(); rosterSeen.UnionWith(now);
    }

    /// <summary>The roster as it stands is the baseline after founding or a load: nothing that left before is invented.</summary>
    private void ResetRosterWatch() {
        rosterSeen.Clear();
        if (Label?.roster != null) rosterSeen.UnionWith(Label.roster.Where(a => a != null).Select(a => a.artistId));
    }

    /// <summary>Called once, the dawn a promise is recorded broken. The first name on the label is the one its home town
    /// knows the label by, so that story is told there.</summary>
    private void OnPromiseBroken(ContractCommitment c, SimulatedArtist artist) {
        if (scoutingKnowledge.FoundingAct?.ArtistId != c.ArtistId) return;
        Note($"Word gets round {TownName(SceneOf(artist))}: the label broke its word to {artist.stageName}, the first act it ever signed.");
    }

    // ========================================================================================
    // WHAT THE ACT HAS HEARD
    // ========================================================================================

    private static IEnumerable<string> PeopleOf(SimulatedArtist act, bool everyone) =>
        act?.members?.Where(m => m?.personId != null && (everyone || m.isActive)).Select(m => m.personId) ?? Enumerable.Empty<string>();

    /// <summary>The first pair of people, one in each act, who know each other (or the same person in both).
    /// Null when nobody connects them.</summary>
    private static (Musician Ours, Musician Theirs, ContactEdge Edge)? Connection(SimulatedArtist act, SimulatedArtist other, bool othersFormer) {
        if (act == null || other == null) return null;
        foreach (Musician m in act.members.Where(m => m?.personId != null && m.isActive).OrderBy(m => m.personId, StringComparer.Ordinal))
        foreach (Musician o in other.members.Where(o => o?.personId != null && (othersFormer || o.isActive)).OrderBy(o => o.personId, StringComparer.Ordinal)) {
            if (m.personId == o.personId) return (m, o, null);
            if (ContactNetworkService.Enabled && ContactNetworkService.Edge(m.personId, o.personId) is { } edge) return (m, o, edge);
        }
        return null;
    }

    /// <summary>Word of mouth: kept and broken promises to acts this one's people know, within
    /// <see cref="WordOfMouthDays"/>. The founding act's story is also known across its home town. Bounded, dated, causal.</summary>
    private float WordOfMouthCredit(SimulatedArtist artist, List<string> stated) {
        if (Label == null || artist == null) return 0f;
        int today = TodayIndex();
        string founding = scoutingKnowledge.FoundingAct?.ArtistId;
        string scene = SceneOf(artist);
        float credit = 0f;
        foreach (var story in contractCommitments.Where(c => c.LabelId == Label.labelId && c.ArtistId != artist.artistId &&
                c.Status is ContractCommitmentStatus.Missed or ContractCommitmentStatus.Kept && c.ResolvedDay.HasValue)
                .GroupBy(c => c.ArtistId).OrderBy(g => g.Key, StringComparer.Ordinal)) {
            var other = ArtistManager.Instance?.GetArtist(story.Key);
            if (other == null) continue;
            // A broken promise never answered is the story told; otherwise a kept one is. A departure over a broken
            // promise brings the story back up the day they leave.
            var broken = story.Where(c => c.Status == ContractCommitmentStatus.Missed && !c.RemedyApplied).ToList();
            var leftOver = rosterDepartures.LastOrDefault(d => d.ArtistId == story.Key && d.UnansweredBroken > 0);
            int brokenDay = broken.Count == 0 ? -1 : Math.Max(broken.Max(c => c.ResolvedDay.Value), leftOver?.Day ?? -1);
            var kept = story.Where(c => c.Status == ContractCommitmentStatus.Kept).ToList();
            int keptDay = kept.Count == 0 ? -1 : kept.Max(c => c.ResolvedDay.Value);
            bool isBroken = brokenDay >= 0 && today - brokenDay <= WordOfMouthDays;
            bool isKept = !isBroken && broken.Count == 0 && keptDay >= 0 && today - keptDay <= WordOfMouthDays;
            if (!isBroken && !isKept) continue;
            bool foundingStory = story.Key == founding && scene != null && SceneOf(other) == scene;
            var link = Connection(artist, other, othersFormer: true);
            if (link == null && !foundingStory) continue;
            string who = link == null ? null : link.Value.Ours.FullName;
            if (isBroken) {
                credit += foundingStory ? FoundingBrokenWordCredit : BrokenWordCredit;
                stated.Add(foundingStory
                    ? $"Everybody around {TownName(scene)} knows {other.stageName} were the first name on your label, and that you broke your word to them."
                    : $"{who} knows {other.stageName}'s people. They've heard you broke a promise to them.");
            } else {
                credit += foundingStory ? FoundingKeptWordCredit : KeptWordCredit;
                stated.Add(foundingStory
                    ? $"{other.stageName}, the first act you signed, say you kept your word to them. People around {TownName(scene)} have heard."
                    : $"{who} knows {other.stageName}'s people. They've heard you keep your word.");
            }
        }
        return Mathf.Clamp(credit, WordOfMouthFloor, WordOfMouthCeiling);
    }

    /// <summary>An introduction: one of their people knows somebody on the label's roster. A fallout cuts the other way.
    /// An act the label has broken its word to and never answered vouches for nobody; that story travels as word of mouth.</summary>
    private float IntroductionCreditFor(SimulatedArtist artist, List<string> stated) {
        if (Label?.roster == null || artist == null || !ContactNetworkService.Enabled) return 0f;
        float credit = 0f; bool introduced = false, fellOut = false;
        foreach (SimulatedArtist ours in Label.roster.Where(a => a != null && a != artist).OrderBy(a => a.artistId, StringComparer.Ordinal)) {
            if (contractCommitments.Any(c => c.ArtistId == ours.artistId && c.Status == ContractCommitmentStatus.Missed && !c.RemedyApplied)) continue;
            foreach (Musician m in artist.members.Where(m => m?.personId != null && m.isActive).OrderBy(m => m.personId, StringComparer.Ordinal))
            foreach (Musician o in ours.members.Where(o => o?.personId != null && o.isActive).OrderBy(o => o.personId, StringComparer.Ordinal)) {
                var edge = ContactNetworkService.Edge(m.personId, o.personId);
                if (edge == null) continue;
                if (edge.Fallout && !fellOut) {
                    fellOut = true; credit += FalloutCredit;
                    stated.Add($"{m.FullName} fell out with {o.FullName}, who plays in {ours.stageName} on your label.");
                } else if (!edge.Fallout && !introduced) {
                    introduced = true; credit += IntroductionCredit;
                    stated.Add($"{m.FullName} knows {o.FullName} from {ours.stageName}, on your label. They've heard what it's like working with you.");
                }
            }
        }
        return credit;
    }

    /// <summary>An act that has been on the label before and left on fair terms remembers it.</summary>
    private float FormerActCreditFor(SimulatedArtist artist, List<string> stated) {
        var left = rosterDepartures.LastOrDefault(d => d.ArtistId == artist?.artistId && d.LabelId == Label?.labelId);
        if (left == null || left.UnansweredBroken > 0 || left.Kind == RosterDepartureKind.TalksBrokeDown) return 0f;
        stated.Add("They've been on your label before and parted on fair terms.");
        return FormerActCredit;
    }

    // ========================================================================================
    // THE FIRST NAME ON THE LABEL
    // ========================================================================================

    /// <summary>How the founding act has been treated, for the founding history: promises kept and broken, and how
    /// (if) it left. Built from the commitment and departure records only.</summary>
    private string FoundingTreatmentSummary(string artistId) {
        if (Label == null || artistId == null) return "";
        var mine = contractCommitments.Where(c => c.ArtistId == artistId && c.LabelId == Label.labelId).ToList();
        int kept = mine.Count(c => c.Status == ContractCommitmentStatus.Kept);
        var broken = mine.Where(c => c.Status == ContractCommitmentStatus.Missed).ToList();
        var parts = new List<string>();
        if (kept > 0) parts.Add(kept == 1 ? "You kept the promise you made them." : $"You kept the {kept} promises you made them.");
        if (broken.Count > 0) {
            int open = broken.Count(c => !c.RemedyApplied);
            parts.Add((broken.Count == 1 ? "You broke a promise to them" : $"You broke {broken.Count} promises to them") +
                $" (first on {LocalSceneRoomService.Date(broken.Min(c => c.ResolvedDay ?? c.DueDay)).ToHeadlineString()})" +
                (open == 0 ? ", and answered for it at renewal." : ". The town they came from has heard about it."));
        }
        var left = rosterDepartures.LastOrDefault(d => d.ArtistId == artistId && d.LabelId == Label.labelId);
        if (left != null) {
            string how = left.Kind switch {
                RosterDepartureKind.TalksBrokeDown => "renewal talks broke down",
                RosterDepartureKind.Disbanded => "the band broke up",
                RosterDepartureKind.Retired => "they retired",
                RosterDepartureKind.SignedElsewhere => "they signed elsewhere",
                _ => "they moved on",
            };
            parts.Add($"Left the label {LocalSceneRoomService.Date(left.Day).ToHeadlineString()}: {how}. " +
                (left.UnansweredBroken > 0 ? "They left with a broken promise unanswered." : "You parted on fair terms."));
        }
        return string.Join(" ", parts);
    }

    private List<RosterDeparture> CaptureRosterDepartures() => rosterDepartures.Count == 0 ? null : LocalSceneRoomService.Copy(rosterDepartures);

    private static bool ValidateRosterDepartures(List<RosterDeparture> saved, string labelId, out string message) {
        message = "The saved roster departures are invalid; no player state was restored.";
        return saved == null || saved.All(d => d != null && d.LabelId == labelId && !string.IsNullOrEmpty(d.ArtistId) &&
            Enum.IsDefined(d.Kind) && d.Day >= 0 && d.UnansweredBroken >= 0);
    }

    private void RestoreRosterDepartures(List<RosterDeparture> saved) {
        rosterDepartures.Clear();
        if (saved != null) rosterDepartures.AddRange(LocalSceneRoomService.Copy(saved));
        ResetRosterWatch();
    }

    /// <summary>Fixture access: run the dawn roster watch for today. Never rendered.</summary>
    internal void TrackRosterDeparturesForCheck() => TrackRosterDepartures(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
}
