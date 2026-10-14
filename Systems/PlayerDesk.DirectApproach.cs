using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Asking an act that isn't looking for a record deal whether it would hear one (scouting directive §10-§11,
/// Phase 6 opening runway). On the opening calendar nearly every unsigned act on a bill is off the market, and the
/// population owner brings acts back at random across the country, never the ones a label is actually courting. A
/// 12-week opening reached its first willing act on day 36-63, and never in Billings.
///
/// The player never activates anyone. A heard act either agrees to think it over or declines for the season, on a
/// keyed draw that a re-ask cannot reroll. Acts thinking it over are handed to ArtistManager's weekly activation pass
/// (DirectApproachProvider), capped by the label's open roster slots. Once back on the market they are an ordinary
/// prospect: other labels can bid, and the pursuit interceptor turns a rival's pick into a courtship.</summary>
public partial class PlayerDesk {
    /// <summary>Share of approached acts that agree to think it over. Design input, uncalibrated.</summary>
    public const float DirectApproachConsiderShare = 0.65f;
    /// <summary>Days the act's side takes to answer: always spans one weekly activation pass.</summary>
    public const int DirectApproachAnswerDays = 8;
    /// <summary>A season: a declining act can be asked again when it ends.</summary>
    public const int DirectApproachSeasonDays = 91;

    private List<ScoutingDirectApproach> Approaches => scoutingKnowledge.Approaches ??= new();
    public IReadOnlyList<ScoutingDirectApproach> DirectApproaches => LocalSceneRoomService.Copy(Approaches).AsReadOnly();

    private static bool IsOffTheMarket(SimulatedArtist a) => a != null && string.IsNullOrEmpty(a.labelId) &&
        a.lifecycleStatus == ArtistLifecycleStatus.Active && a.prospectMarketStatus == ProspectMarketStatus.Latent;

    /// <summary>The pad offers to ask, rather than to discuss terms: a heard act that is off the market.</summary>
    public bool CanAskDirectApproach(SimulatedArtist artist) => Label != null && LocalScenes.ScoutingExperience &&
        !LocalScenes.ScoutingExperienceObserver && IsOffTheMarket(artist) && HasScoutingEvidence(artist.artistId);

    /// <summary>Called when the signing gate refuses a heard act. True with the act's answer when the act was off the
    /// market and heard by this label; false leaves the caller's ordinary refusal.</summary>
    private bool TryDirectApproach(SimulatedArtist artist, out string message) {
        message = null;
        if (Label == null || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver ||
            !IsOffTheMarket(artist) || !HasScoutingEvidence(artist.artistId)) return false;
        int today = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        var prior = Approaches.LastOrDefault(a => a.ArtistId == artist.artistId);
        if (prior?.Status == ScoutingApproachStatus.Considering) {
            message = $"{artist.stageName}'s side is still thinking over your interest. They said they'd answer by {LocalSceneRoomService.Date(prior.UntilDay).ToHeadlineString()}.";
            return true;
        }
        if (prior?.Status == ScoutingApproachStatus.Declined && today < prior.UntilDay) {
            message = $"{artist.stageName} told you they want to keep playing locally this season. You can ask again after {LocalSceneRoomService.Date(prior.UntilDay).ToHeadlineString()}.";
            return true;
        }
        if (!Label.HasRosterSpace) { message = "Roster is full."; return true; }
        int season = today / DirectApproachSeasonDays;
        bool considers = StableUnit($"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|direct-approach|{Label.labelId}|{artist.artistId}|{season}") < DirectApproachConsiderShare;
        var record = new ScoutingDirectApproach { ArtistId = artist.artistId, ArtistName = artist.stageName, Day = today,
            Status = considers ? ScoutingApproachStatus.Considering : ScoutingApproachStatus.Declined,
            UntilDay = considers ? today + DirectApproachAnswerDays : (season + 1) * DirectApproachSeasonDays };
        Approaches.Add(record);
        message = considers
            ? $"{artist.stageName} aren't looking for a record deal, but they'll think over an offer from a label that came to hear them. Their side will answer by {LocalSceneRoomService.Date(record.UntilDay).ToHeadlineString()}."
            : $"{artist.stageName} want to keep playing locally this season. You can ask again after {LocalSceneRoomService.Date(record.UntilDay).ToHeadlineString()}.";
        Note(message); Changed?.Invoke();
        return true;
    }

    /// <summary>For ArtistManager's weekly activation pass: acts still thinking it over and still off the market, oldest
    /// first, no more than the label has open roster slots.</summary>
    private IReadOnlyCollection<string> DirectApproachIds() {
        if (Label == null || scoutingKnowledge.Approaches == null) return null;
        int open = Math.Max(0, Label.maxRosterSize - (Label.roster?.Count ?? 0));
        return Approaches.Where(a => a.Status == ScoutingApproachStatus.Considering && IsOffTheMarket(ArtistManager.Instance?.GetArtist(a.ArtistId)))
            .OrderBy(a => a.Day).ThenBy(a => a.ArtistId, StringComparer.Ordinal).Take(open).Select(a => a.ArtistId).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Dawn: the act's side answers once the owner has decided.</summary>
    private void AdvanceDirectApproaches(GameDate date) {
        if (scoutingKnowledge.Approaches == null) return;
        int today = LocalSceneRoomService.Day(date), week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        foreach (var a in Approaches.Where(a => a.Status == ScoutingApproachStatus.Considering)) {
            var artist = ArtistManager.Instance?.GetArtist(a.ArtistId);
            if (artist == null || !string.IsNullOrEmpty(artist.labelId) || artist.lifecycleStatus != ArtistLifecycleStatus.Active) {
                a.Status = ScoutingApproachStatus.Lapsed;
                Note($"{a.ArtistName} are no longer in a position to take your offer.");
            } else if (ArtistManager.Instance.IsEligibleForPopulationSigning(artist, week)) {
                a.Status = ScoutingApproachStatus.Ready;
                Note($"{a.ArtistName}'s side: they'll hear an offer from you now. Other labels can hear of it too.");
            } else if (today > a.UntilDay) {
                a.Status = ScoutingApproachStatus.Lapsed;
                Note($"{a.ArtistName} decided to keep playing locally for now. Asking again later is a fresh conversation.");
            }
        }
    }

    private bool IsConsideringApproach(string artistId) => scoutingKnowledge.Approaches?.Any(a => a.ArtistId == artistId &&
        a.Status is ScoutingApproachStatus.Considering or ScoutingApproachStatus.Ready) == true;

    /// <summary>The notebook line for the latest approach, or null.</summary>
    internal string DirectApproachSummary(string artistId) {
        var a = scoutingKnowledge.Approaches?.LastOrDefault(x => x.ArtistId == artistId);
        if (a == null) return null;
        string when = LocalSceneRoomService.Date(a.Day).ToHeadlineString(), until = LocalSceneRoomService.Date(a.UntilDay).ToHeadlineString();
        return a.Status switch {
            ScoutingApproachStatus.Considering => $"Approach ({when}): they weren't looking for a deal and are thinking yours over; answer due by {until}.",
            ScoutingApproachStatus.Ready => $"Approach ({when}): they agreed to hear an offer. Other labels can now hear they're available.",
            ScoutingApproachStatus.Declined => $"Approach ({when}): they want to keep playing locally this season; ask again after {until}.",
            _ => $"Approach ({when}): it came to nothing; asking again later is a fresh conversation."
        };
    }
}
