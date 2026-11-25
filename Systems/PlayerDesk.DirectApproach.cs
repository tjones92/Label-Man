using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Asking an act that isn't looking for a record deal whether it would hear one (scouting directive §10-§11,
/// Phase 6 opening runway). On the opening calendar nearly every unsigned act on a bill is off the market, and the
/// population owner brings acts back at random across the country, never the ones a label is actually courting. A
/// 12-week opening reached its first willing act on day 36-63, and never in Billings.
///
/// The player never activates anyone. A heard act answers from its disposition (ActDisposition, signing realism step 2),
/// read from real state with no draw: a hungry act thinks it over, a careful act wants to be heard again first, and a
/// comfortable act says no and why. Asking again gets the same answer until something real changes; there is no
/// season date. (The keyed 65% coin this replaces is gone.) Acts thinking it over are handed to ArtistManager's weekly activation pass
/// (DirectApproachProvider), capped by the label's open roster slots. Once back on the market they are an ordinary
/// prospect: other labels can bid, and the pursuit interceptor turns a rival's pick into a courtship.</summary>
public partial class PlayerDesk {
    /// <summary>Days the act's side takes to answer: always spans one weekly activation pass.</summary>
    public const int DirectApproachAnswerDays = 8;
    /// <summary>A season, for the number-refusal clock (ScoutingPresence). The ask no longer uses it.</summary>
    public const int DirectApproachSeasonDays = 91;
    /// <summary>A careful act wants to be heard on this many different nights before it will think an offer over.</summary>
    public const int CarefulHeardNights = 2;

    private List<ScoutingDirectApproach> Approaches => scoutingKnowledge.Approaches ??= new();
    public IReadOnlyList<ScoutingDirectApproach> DirectApproaches => LocalSceneRoomService.Copy(Approaches).AsReadOnly();

    private static bool IsOffTheMarket(SimulatedArtist a) => a != null && string.IsNullOrEmpty(a.labelId) &&
        a.lifecycleStatus == ArtistLifecycleStatus.Active && a.prospectMarketStatus == ProspectMarketStatus.Latent;

    /// <summary>The pad offers to ask, rather than to discuss terms: a heard act that is off the market.</summary>
    public bool CanAskDirectApproach(SimulatedArtist artist) => Label != null && LocalScenes.ScoutingExperience &&
        !LocalScenes.ScoutingExperienceObserver && IsOffTheMarket(artist) && HasScoutingEvidence(artist.artistId);

    /// <summary>The act's disposition toward this label today (ActDisposition), with the share of what you've heard them
    /// play that's their own. Null without a label or an act.</summary>
    public DispositionRead ReadDisposition(SimulatedArtist artist) {
        if (Label == null || artist == null) return null;
        var sets = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artist.artistId)?.Observations
            .Where(o => o.Kind == ScoutingEvidenceKind.Performance).SelectMany(o => o.Set ?? new()).ToList();
        float? originals = sets == null || sets.Count == 0 ? null : sets.Count(s => s.Original) / (float)sets.Count;
        return ActDisposition.Read(artist, Label, LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate), originals);
    }

    /// <summary>Different nights you've heard this act play.</summary>
    public int HeardNights(string artistId) => scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations
        .Where(o => o.Kind == ScoutingEvidenceKind.Performance).Select(o => o.OccurredDay ?? o.AcquiredDay).Distinct().Count() ?? 0;

    /// <summary>A no that still holds: the act said no and is still comfortable today. An older save's season decline
    /// is read the same way, so it lifts as soon as the act's reason does.</summary>
    public bool StillDeclining(SimulatedArtist artist) {
        var prior = artist == null ? null : scoutingKnowledge.Approaches?.LastOrDefault(a => a.ArtistId == artist.artistId);
        return prior?.Status == ScoutingApproachStatus.Declined && IsOffTheMarket(artist) &&
            ReadDisposition(artist)?.Kind == DealDisposition.Comfortable;
    }

    /// <summary>A careful act's request still stands: you haven't heard them again since.</summary>
    public bool WaitingToBeHeardAgain(SimulatedArtist artist) {
        var prior = artist == null ? null : scoutingKnowledge.Approaches?.LastOrDefault(a => a.ArtistId == artist.artistId);
        return prior?.Status == ScoutingApproachStatus.WantsAnotherLook && HeardNights(artist.artistId) < CarefulHeardNights &&
            ReadDisposition(artist)?.Kind == DealDisposition.Careful;
    }

    /// <summary>Whoever talks for the act: the member who'd come over after a set, else the leader or the singer.</summary>
    private string SpeakerFor(SimulatedArtist artist) {
        if (PreviewScoutingBusinessMeeting(artist.artistId)?.SourceName is { Length: > 0 } name) return name;
        var m = artist.members?.Where(x => x != null && x.isActive && x.lifeState == MemberLifeState.Active)
            .OrderByDescending(x => x.isBandLeader).ThenByDescending(x => x.isLeadVocalist).ThenBy(x => x.personId, StringComparer.Ordinal).FirstOrDefault();
        return m == null ? artist.stageName : m.FullName.Trim();
    }

    private string HomeTownName => DistanceModel.GetCityById(Label?.homeCityId)?.name;

    /// <summary>Called when the signing gate refuses a heard act. True with the act's answer when the act was off the
    /// market and heard by this label; false leaves the caller's ordinary refusal.</summary>
    private bool TryDirectApproach(SimulatedArtist artist, out string message) {
        message = null;
        if (Label == null || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver ||
            !IsOffTheMarket(artist) || !HasScoutingEvidence(artist.artistId)) return false;
        int today = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        var prior = Approaches.LastOrDefault(a => a.ArtistId == artist.artistId);
        if (prior?.Status == ScoutingApproachStatus.Considering) {
            message = $"{artist.stageName}'s side is still talking it over. They said they'd answer by {LocalSceneRoomService.Date(prior.UntilDay).ToHeadlineString()}.";
            return true;
        }
        if (!Label.HasRosterSpace) { message = "Roster is full."; return true; }
        var read = ReadDisposition(artist);
        string said = ActDisposition.Line(read, artist, SpeakerFor(artist), HomeTownName);
        var status = ScoutingApproachStatus.Considering;
        if (read.Kind == DealDisposition.Comfortable) status = ScoutingApproachStatus.Declined;
        else if (read.Kind == DealDisposition.Careful && HeardNights(artist.artistId) < CarefulHeardNights) status = ScoutingApproachStatus.WantsAnotherLook;
        // Asking again with nothing changed is the same conversation: say it again, don't file it twice.
        bool repeat = prior != null && prior.Status == status && status != ScoutingApproachStatus.Considering && prior.Said == said;
        if (!repeat) Approaches.Add(new ScoutingDirectApproach { ArtistId = artist.artistId, ArtistName = artist.stageName, Day = today,
            Status = status, UntilDay = status == ScoutingApproachStatus.Considering ? today + DirectApproachAnswerDays : today, Said = said });
        int until = today + DirectApproachAnswerDays;
        message = status switch {
            ScoutingApproachStatus.Declined => $"{said} {ActDisposition.WhatWouldChange(read)}",
            ScoutingApproachStatus.WantsAnotherLook => NextReachableScoutingSet(artist.artistId) is { } next
                ? $"{said} They're on next at {next.RoomName}, {LocalSceneRoomService.Date(next.Day).ToHeadlineString()}."
                : $"{said} Their next date isn't posted yet.",
            _ => read.Kind == DealDisposition.Careful
                ? $"{artist.stageName} {ActGrammar.Verb(artist, "has", "have")} seen you come back. They'll talk it over and answer by {LocalSceneRoomService.Date(until).ToHeadlineString()}."
                : $"{said} They'll talk it over among themselves and answer by {LocalSceneRoomService.Date(until).ToHeadlineString()}."
        };
        if (!repeat) Note(message);
        Changed?.Invoke();
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
                Note($"{a.ArtistName} {ActGrammar.Verb(artist, "is", "are")} no longer in a position to take your offer.");
            } else if (ArtistManager.Instance.IsEligibleForPopulationSigning(artist, week)) {
                a.Status = ScoutingApproachStatus.Ready;
                // Terms open from the act's notebook card (TALK TO THEM). The news says how to reach them: by the number
                // you hold, or at their next set. (Playtest 2026-11-04: the paper said "now" with no way in;
                // 2026-11-05: a once-a-week venue was the only way to reach an act.)
                var next = NextReachableScoutingSet(a.ArtistId);
                string where = ActNumber(a.ArtistId) is { } number
                    ? $"Open their card in the act notebook to talk terms, or ring {number.PersonName} to set up a meeting."
                    : next == null
                    ? "Open their card in the act notebook to talk terms. To meet them, catch a set or ask a booker for their number."
                    : $"Open their card in the act notebook to talk terms. Their next set: {next.RoomName}, {LocalSceneRoomService.Date(next.Day).ToHeadlineString()}, {next.StartMinute / 60:00}:{next.StartMinute % 60:00}.";
                Note($"{a.ArtistName}'s side: they'll hear an offer from you now. {where} Other labels can hear of it too.");
            } else if (today > a.UntilDay) {
                a.Status = ScoutingApproachStatus.Lapsed;
                // The activation pass takes only as many as the label has open roster slots.
                Note(Label?.HasRosterSpace == false
                    ? $"{a.ArtistName}'s side called back, but your roster was full. Ask them again when you have room."
                    : $"{a.ArtistName}'s side never called back. Ask them again the next time you see them.");
            }
        }
    }

    private bool IsConsideringApproach(string artistId) => scoutingKnowledge.Approaches?.Any(a => a.ArtistId == artistId &&
        a.Status is ScoutingApproachStatus.Considering or ScoutingApproachStatus.Ready) == true;

    /// <summary>The notebook line for the latest approach, or null. Quotes what the act's side said when it's on file.</summary>
    internal string DirectApproachSummary(string artistId) {
        var a = scoutingKnowledge.Approaches?.LastOrDefault(x => x.ArtistId == artistId);
        if (a == null) return null;
        string when = LocalSceneRoomService.Date(a.Day).ToHeadlineString(), until = LocalSceneRoomService.Date(a.UntilDay).ToHeadlineString();
        string said = string.IsNullOrEmpty(a.Said) ? "" : " " + a.Said;
        return a.Status switch {
            ScoutingApproachStatus.Considering => $"Asked ({when}): they're talking it over; answer due by {until}.{said}",
            ScoutingApproachStatus.Ready => $"Asked ({when}): they'll hear an offer. Talk terms from their card. Other labels can now hear they're available.",
            ScoutingApproachStatus.Declined => $"Asked ({when}): they said no.{said}",
            ScoutingApproachStatus.WantsAnotherLook => $"Asked ({when}): they want to see you again first.{said}",
            _ => $"Asked ({when}): it came to nothing. Asking again is a fresh conversation."
        };
    }
}
