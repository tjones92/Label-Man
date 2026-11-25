using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Asking an act that isn't looking for a record deal whether it would hear one (scouting directive §10-§11,
/// Phase 6 opening runway; SimTools/SigningRealismBreakdown.md steps 2-3).
///
/// A heard act answers from its disposition (ActDisposition), read from real state with no draw. A comfortable act says
/// no and why. A careful act wants to be heard again first. A hungry act (or a careful one you came back for) says yes
/// on the spot, and what stands between the yes and a signature is the period's real friction, each a dated step the
/// player takes (§6): a member under 21 means meeting the family on Sunday, a shark, svengali or visionary manager wants
/// a sit-down on the next weekday, and a member holding out settles it at the act's next set, where you should be. With
/// none of those, they sign tonight: terms open in the same conversation.
///
/// An act that agreed is signed straight from Latent (ArtistManager.ReleaseLatentForPlayerSigning, inside the signing
/// call). It's the player's own roster slot, so no AI label loses an activation and the AI market never sees the act on
/// offer. A rival hears of it only through its own channels (the weekly pass may still activate the act, and the pursuit
/// interceptor turns a rival's pick of an act you're pursuing into a courtship). A yes holds for AgreedHoldDays past its
/// last step's date. Older saves' Considering/Ready approaches still run through the weekly activation pass.</summary>
public partial class PlayerDesk {
    /// <summary>Days the act's side took to answer on older saves: always spans one weekly activation pass.</summary>
    public const int DirectApproachAnswerDays = 8;
    /// <summary>A season, for the number-refusal clock (ScoutingPresence). The ask no longer uses it.</summary>
    public const int DirectApproachSeasonDays = 91;
    /// <summary>A careful act wants to be heard on this many different nights before it will say yes.</summary>
    public const int CarefulHeardNights = 2;
    /// <summary>A yes holds this many days past the agreement or its last step's date, whichever is later.</summary>
    public const int AgreedHoldDays = 28;
    /// <summary>Sunday dinner with the family, and the sit-down with a manager.</summary>
    public const int FamilyVisitHours = 2, ManagerSitDownHours = 1;

    private List<ScoutingDirectApproach> Approaches => scoutingKnowledge.Approaches ??= new();
    public IReadOnlyList<ScoutingDirectApproach> DirectApproaches => LocalSceneRoomService.Copy(Approaches).AsReadOnly();

    private static bool IsOffTheMarket(SimulatedArtist a) => a != null && string.IsNullOrEmpty(a.labelId) &&
        a.lifecycleStatus == ArtistLifecycleStatus.Active && a.prospectMarketStatus == ProspectMarketStatus.Latent;

    private ScoutingDirectApproach LatestApproach(string artistId) =>
        artistId == null ? null : scoutingKnowledge.Approaches?.LastOrDefault(a => a.ArtistId == artistId);

    /// <summary>The act's standing yes to this label: agreed, still unsigned and still together. Null otherwise.</summary>
    public ScoutingDirectApproach Agreement(SimulatedArtist artist) {
        var a = LatestApproach(artist?.artistId);
        return a?.Status == ScoutingApproachStatus.Agreed && string.IsNullOrEmpty(artist.labelId) &&
            artist.lifecycleStatus == ArtistLifecycleStatus.Active ? a : null;
    }

    /// <summary>They said yes and every step is taken: terms can open, from Latent if need be.</summary>
    public bool AgreedToSign(SimulatedArtist artist) => Agreement(artist) is { } a && a.Steps.All(s => s.Done);

    /// <summary>The next step before an agreed act signs (earliest date first), or null.</summary>
    public ScoutingApproachStep NextApproachStep(SimulatedArtist artist) =>
        Agreement(artist)?.Steps.Where(s => !s.Done).OrderBy(s => s.Day ?? int.MaxValue).ThenBy(s => s.Kind).FirstOrDefault();

    /// <summary>Whether terms can open for this act now: the market's own rule, or a yes with its steps taken. An act
    /// with a yes still waiting on a step can't skip it, even if the market has picked it up meanwhile.</summary>
    public bool CanTalkTerms(Prospect prospect) => prospect?.Artist != null &&
        (Agreement(prospect.Artist) == null ? SceneDealAvailable(prospect) : AgreedToSign(prospect.Artist));

    /// <summary>The pad offers to ask, rather than to discuss terms: a heard act that is off the market and hasn't said
    /// yes to you already.</summary>
    public bool CanAskDirectApproach(SimulatedArtist artist) => Label != null && LocalScenes.ScoutingExperience &&
        !LocalScenes.ScoutingExperienceObserver && IsOffTheMarket(artist) && HasScoutingEvidence(artist.artistId) && Agreement(artist) == null;

    /// <summary>The act's disposition toward this label today (ActDisposition), with the share of what you've heard them
    /// play that's their own. Null without a label or an act.</summary>
    public DispositionRead ReadDisposition(SimulatedArtist artist) {
        if (Label == null || artist == null) return null;
        var sets = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artist.artistId)?.Observations
            .Where(o => o.Kind == ScoutingEvidenceKind.Performance).SelectMany(o => o.Set ?? new()).ToList();
        float? originals = sets == null || sets.Count == 0 ? null : sets.Count(s => s.Original) / (float)sets.Count;
        return ActDisposition.Read(artist, Label, LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate), originals);
    }

    private IEnumerable<int> HeardDays(string artistId) => scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations
        .Where(o => o.Kind == ScoutingEvidenceKind.Performance).Select(o => o.OccurredDay ?? o.AcquiredDay).Distinct() ?? Enumerable.Empty<int>();

    /// <summary>Different nights you've heard this act play.</summary>
    public int HeardNights(string artistId) => HeardDays(artistId).Count();

    /// <summary>A no that still holds: the act said no and is still comfortable today. An older save's season decline
    /// is read the same way, so it lifts as soon as the act's reason does.</summary>
    public bool StillDeclining(SimulatedArtist artist) {
        var prior = LatestApproach(artist?.artistId);
        return prior?.Status == ScoutingApproachStatus.Declined && IsOffTheMarket(artist) &&
            ReadDisposition(artist)?.Kind == DealDisposition.Comfortable;
    }

    /// <summary>A careful act's request still stands: you haven't heard them again since.</summary>
    public bool WaitingToBeHeardAgain(SimulatedArtist artist) {
        var prior = LatestApproach(artist?.artistId);
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
    private static string Headline(int day) => LocalSceneRoomService.Date(day).ToHeadlineString();
    private static string Names(IReadOnlyList<string> names) => names.Count switch {
        0 => "", 1 => names[0], 2 => $"{names[0]} and {names[1]}", _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1]
    };

    /// <summary>The first day on or after <paramref name="from"/> that falls on one of <paramref name="days"/>.</summary>
    private static int NextDayOn(int from, params DayOfWeek[] days) {
        int d = from;
        while (!days.Contains(LocalSceneRoomService.Date(d).DayOfWeek)) d++;
        return d;
    }

    /// <summary>What stands between a yes and a signature, from the read's frictions. Sunday for the family (they're at
    /// church in the morning and home after), the manager at his place, and the next posted set for a holdout.
    /// Who the manager is (ManagerIdentity) sets where and when you sit down: the club booker at his room on a night it's
    /// open, family on Sunday at the kitchen table, everyone else at his place on a weekday. When the manager is the
    /// minor's own family, the Sunday visit is the sit-down too: one step, not two.</summary>
    internal List<ScoutingApproachStep> StepsFor(DispositionRead read, SimulatedArtist artist, int today) {
        var steps = new List<ScoutingApproachStep>();
        var manager = read.DemandingManager ? ManagerIdentity.Read(artist, today) : null;
        bool familyManager = manager?.Kind == ManagerKind.Family;
        if (read.Minors.Count > 0) steps.Add(new() { Kind = ScoutingApproachStepKind.Parents, Who = Names(read.Minors),
            Day = NextDayOn(today + 1, DayOfWeek.Sunday), Manager = familyManager ? ManagerIdentity.Introduce(manager) : null });
        if (manager != null && !(familyManager && read.Minors.Count > 0)) {
            int day = manager.Kind switch {
                ManagerKind.Family => NextDayOn(today + 1, DayOfWeek.Sunday),
                ManagerKind.ClubBooker when SceneRoomCatalog.Get(manager.RoomId) is { Nights.Count: > 0 } room => NextDayOn(today + 1, room.Nights.ToArray()),
                _ => NextDayOn(today + 1, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday)
            };
            steps.Add(new() { Kind = ScoutingApproachStepKind.Manager, Who = manager.Name, Manager = ManagerIdentity.Introduce(manager),
                Place = manager.Place, Female = manager.Female, Day = day });
        }
        if (read.Holdout != null) {
            var next = NextReachableScoutingSet(artist.artistId);
            steps.Add(new() { Kind = ScoutingApproachStepKind.Holdout, Who = read.Holdout,
                Day = next != null && next.Day > today ? next.Day : null, RoomName = next != null && next.Day > today ? next.RoomName : null });
        }
        return steps;
    }

    /// <summary>The step as the act's side puts it to you.</summary>
    public static string StepAsk(ScoutingApproachStep s, SimulatedArtist artist) => s.Kind switch {
        ScoutingApproachStepKind.Parents => (s.Who.Contains(" and ") ? $"{s.Who} are" : $"{s.Who} is") +
            $" under 21, so a parent signs. The family wants to meet you first: Sunday, {Headline(s.Day ?? 0)}, after church." +
            (s.Manager != null ? $" {s.Manager}, handles their business too, so that's the sit-down as well." : ""),
        ScoutingApproachStepKind.Manager => $"{(s.Manager != null ? s.Manager + "," : s.Who)} handles their business, and nobody signs anything until you've sat down with " +
            $"{(s.Female ? "her" : "him")}: {Headline(s.Day ?? 0)}, at {s.Place ?? "his office"}.",
        _ => $"{s.Who} isn't sure yet. They'll settle it at their next set" +
            (s.Day is { } d ? $", {s.RoomName}, {Headline(d)}, and you should be there." : ", and you should be there. Their next date isn't posted, so a rehearsal would do.")
    };

    /// <summary>A few words for the notebook card.</summary>
    public static string StepShort(ScoutingApproachStep s) => s.Kind switch {
        ScoutingApproachStepKind.Parents => $"meet the family Sunday ({LocalSceneRoomService.Date(s.Day ?? 0).ShortMonthName} {LocalSceneRoomService.Date(s.Day ?? 0).day})",
        ScoutingApproachStepKind.Manager => $"sit down with {s.Who}",
        _ => $"{s.Who} wants you at their next set"
    };

    /// <summary>Called when the signing gate refuses a heard act, or before terms open for one that said yes. True with
    /// the act's answer when the ask is the conversation (a no, a request, a step still to take); false lets terms open
    /// (with <paramref name="message"/> carrying a yes said just now) or leaves the caller's ordinary refusal.</summary>
    private bool TryDirectApproach(SimulatedArtist artist, out string message) {
        message = null;
        if (Label == null || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver || artist == null) return false;
        if (Agreement(artist) != null) {
            SettleHoldout(artist);
            var step = NextApproachStep(artist);
            if (step == null) return false;
            message = $"{artist.stageName} said yes. {StepAsk(step, artist)}";
            return true;
        }
        if (!IsOffTheMarket(artist) || !HasScoutingEvidence(artist.artistId)) return false;
        int today = SceneDay;
        var prior = LatestApproach(artist.artistId);
        if (prior?.Status == ScoutingApproachStatus.Considering) {
            message = $"{artist.stageName}'s side is still talking it over. They said they'd answer by {Headline(prior.UntilDay)}.";
            return true;
        }
        if (!Label.HasRosterSpace) { message = "Roster is full."; return true; }
        var read = ReadDisposition(artist);
        string who = SpeakerFor(artist);
        var status = ScoutingApproachStatus.Agreed;
        if (read.Kind == DealDisposition.Comfortable) status = ScoutingApproachStatus.Declined;
        else if (read.Kind == DealDisposition.Careful && HeardNights(artist.artistId) < CarefulHeardNights) status = ScoutingApproachStatus.WantsAnotherLook;
        // A careful act you came back for says so; its reason line is the request it made the first time.
        string said = status == ScoutingApproachStatus.Agreed && read.Kind == DealDisposition.Careful
            ? $"{who}: \"You came back. All right, we'll hear what you're offering.\""
            : ActDisposition.Line(read, artist, who, HomeTownName);
        if (status == ScoutingApproachStatus.Agreed) {
            var steps = StepsFor(read, artist, today);
            Approaches.Add(new ScoutingDirectApproach { ArtistId = artist.artistId, ArtistName = artist.stageName, Day = today,
                Status = status, UntilDay = today, Said = said, Steps = steps });
            if (steps.Count == 0) {
                // Nothing between the yes and a signature: terms open in the same conversation (ApproachToSign goes on).
                message = said;
                Note($"{said} They'd sign tonight.");
                return false;
            }
            message = said + " " + string.Join(" ", steps.Select(s => StepAsk(s, artist)));
            Note(message);
            Changed?.Invoke();
            return true;
        }
        // Asking again with nothing changed is the same conversation: say it again, don't file it twice.
        bool repeat = prior != null && prior.Status == status && prior.Said == said;
        if (!repeat) Approaches.Add(new ScoutingDirectApproach { ArtistId = artist.artistId, ArtistName = artist.stageName, Day = today,
            Status = status, UntilDay = today, Said = said });
        message = status == ScoutingApproachStatus.Declined
            ? $"{said} {ActDisposition.WhatWouldChange(read)}"
            : NextReachableScoutingSet(artist.artistId) is { } next
                ? $"{said} They're on next at {next.RoomName}, {Headline(next.Day)}."
                : $"{said} Their next date isn't posted yet.";
        if (!repeat) Note(message);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Go and see the family, or sit down with the manager: in the act's town, on or after the agreed day, for the
    /// hours it takes. The manager's demands still land on the term sheet (AILabel.GenerateTermSheet); this is the meeting
    /// before them. A holdout is settled by being at their next set (SettleHoldout), not by a verb.</summary>
    public bool TakeApproachStep(string artistId, ScoutingApproachStepKind kind, out string message) {
        var artist = ArtistManager.Instance?.GetArtist(artistId);
        var agreed = Agreement(artist);
        var step = agreed?.Steps.FirstOrDefault(s => s.Kind == kind && !s.Done);
        if (step == null || kind == ScoutingApproachStepKind.Holdout) { message = "There's nothing waiting on that."; return false; }
        int today = SceneDay;
        if (step.Day is { } day && today < day) {
            message = kind == ScoutingApproachStepKind.Parents ? $"The family expects you Sunday, {Headline(day)}." : $"{step.Who} will see you {Headline(day)}.";
            return false;
        }
        string town = LocalScenePersistenceService.SceneIdFor(artist.geography?.basePlaceId);
        if (town != null && town != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) {
            message = $"You'd have to be in {DistanceModel.GetCityById(town)?.name ?? "their town"}."; return false;
        }
        int hours = kind == ScoutingApproachStepKind.Parents ? FamilyVisitHours : ManagerSitDownHours;
        if (!Require(hours, out message)) return false;
        Spend(hours);
        step.Done = true; step.DoneDay = today;
        step.Said = kind == ScoutingApproachStepKind.Parents ? FamilyLine(artist, step) : ManagerLine(artist, step);
        message = step.Said + " " + AfterStep(artist);
        Note(message);
        Changed?.Invoke();
        return true;
    }

    private static string FamilyLine(SimulatedArtist artist, ScoutingApproachStep s) {
        bool several = s.Who.Contains(" and ");
        return $"Sunday dinner with {s.Who}'s {(several ? "families" : "family")}. They wanted to know what a record company does " +
            "with a young person's money, and whether there'll be time for school. You told them. " +
            $"{(several ? "The parents" : "A parent")} will sign where you show them." +
            (s.Manager != null ? $" {s.Manager.Split(',')[0]} does their business too, and made it plain the terms go through the family." : "");
    }

    /// <summary>The sit-down: where it happened (who the manager is) and what kind of man he was at it (the archetype,
    /// which is what he'll be at the table).</summary>
    private static string ManagerLine(SimulatedArtist artist, ScoutingApproachStep s) {
        string he = s.Female ? "she" : "he", He = s.Female ? "She" : "He";
        string where = $"You sat down with {(s.Manager != null ? s.Manager + "," : s.Who)} at {s.Place ?? "his office"}.";
        return where + " " + artist.manager switch {
            ManagerArchetype.Shark => $"{He} heard you out and gave nothing away. {He}'ll do the talking at the table, and {he} wants the advance in writing.",
            ManagerArchetype.Svengali => $"{He} talked about {artist.stageName} like {he} invented them. {He}'ll be at the table, and {he}'ll want a say in the record.",
            ManagerArchetype.Visionary => $"{He} wanted to hear your plans for the record before anything else, and liked enough of them to bring the act to the table.",
            _ => "The act can sign."
        };
    }

    private string AfterStep(SimulatedArtist artist) => NextApproachStep(artist) is { } next
        ? "Still to come: " + StepAsk(next, artist)
        : $"{artist.stageName} {ActGrammar.Verb(artist, "is", "are")} ready to sign. Sit down on terms from their card.";

    /// <summary>A holdout comes round when you're in the room with the band: any set you hear on or after the agreed date
    /// (or after the yes, when no date was posted), or a rehearsal you arranged after the yes (PlayerDesk.ScoutingMeetings),
    /// for a band whose next date is a long way off.</summary>
    private void SettleHoldout(SimulatedArtist artist) {
        var agreed = Agreement(artist);
        var step = agreed?.Steps.FirstOrDefault(s => s.Kind == ScoutingApproachStepKind.Holdout && !s.Done);
        if (step == null) return;
        int from = step.Day ?? agreed.Day + 1;
        var seen = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artist.artistId)?.Observations
            .Where(o => o.Kind == ScoutingEvidenceKind.Performance && (o.OccurredDay ?? o.AcquiredDay) >= from ||
                o.Kind == ScoutingEvidenceKind.RehearsalSample)
            .Where(o => (o.OccurredDay ?? o.AcquiredDay) > agreed.Day)
            .OrderBy(o => o.OccurredDay ?? o.AcquiredDay).FirstOrDefault();
        if (seen == null) return;
        step.Done = true; step.DoneDay = seen.OccurredDay ?? seen.AcquiredDay;
        step.Said = seen.Kind == ScoutingEvidenceKind.RehearsalSample
            ? $"{step.Who} walked you out after the rehearsal. \"You came all the way out here. All right, if you're serious, so am I.\""
            : $"{step.Who} came over after the set. \"You came. All right, if you're serious, so am I.\"";
        Note(step.Said + " " + AfterStep(artist));
    }

    /// <summary>A set you just heard may be the one a holdout was waiting on (PlayerDesk.SceneRooms).</summary>
    private void SettleHoldoutAfterHearing(string artistId) {
        if (scoutingKnowledge.Approaches == null) return;
        SettleHoldout(ArtistManager.Instance?.GetArtist(artistId));
    }

    /// <summary>For ArtistManager's weekly activation pass: older saves' acts still thinking it over and still off the
    /// market, oldest first, no more than the label has open roster slots.</summary>
    private IReadOnlyCollection<string> DirectApproachIds() {
        if (Label == null || scoutingKnowledge.Approaches == null) return null;
        int open = Math.Max(0, Label.maxRosterSize - (Label.roster?.Count ?? 0));
        return Approaches.Where(a => a.Status == ScoutingApproachStatus.Considering && IsOffTheMarket(ArtistManager.Instance?.GetArtist(a.ArtistId)))
            .OrderBy(a => a.Day).ThenBy(a => a.ArtistId, StringComparer.Ordinal).Take(open).Select(a => a.ArtistId).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The day a yes stops holding: the agreement or its last dated step, plus AgreedHoldDays.</summary>
    public static int AgreementHoldsUntil(ScoutingDirectApproach a) =>
        Math.Max(a.Day, a.Steps.Select(s => s.Day ?? a.Day).DefaultIfEmpty(a.Day).Max()) + AgreedHoldDays;

    /// <summary>Dawn: older saves' acts answer once the owner has decided; a yes lapses if the act is gone or tired of
    /// waiting; a holdout settles if you heard the set.</summary>
    private void AdvanceDirectApproaches(GameDate date) {
        if (scoutingKnowledge.Approaches == null) return;
        int today = LocalSceneRoomService.Day(date), week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        foreach (var a in Approaches.Where(a => a.Status == ScoutingApproachStatus.Agreed).ToList()) {
            var artist = ArtistManager.Instance?.GetArtist(a.ArtistId);
            if (artist == null || !string.IsNullOrEmpty(artist.labelId) || artist.lifecycleStatus != ArtistLifecycleStatus.Active) {
                if (artist?.labelId == Label?.labelId && artist != null) continue;   // signed to you: the yes is spent, not lapsed
                a.Status = ScoutingApproachStatus.Lapsed;
                Note($"{a.ArtistName} {ActGrammar.Verb(artist, "is", "are")} no longer in a position to sign with you" +
                    (string.IsNullOrEmpty(artist?.labelId) ? "." : $": they're with {ChartManager.Instance?.GetLabelName(artist.labelId) ?? "another label"} now."));
            } else if (today > AgreementHoldsUntil(a)) {
                a.Status = ScoutingApproachStatus.Lapsed;
                Note($"{a.ArtistName} stopped waiting on you. Ask them again the next time you see them.");
            } else SettleHoldout(artist);
        }
        foreach (var a in Approaches.Where(a => a.Status == ScoutingApproachStatus.Considering)) {
            var artist = ArtistManager.Instance?.GetArtist(a.ArtistId);
            if (artist == null || !string.IsNullOrEmpty(artist.labelId) || artist.lifecycleStatus != ArtistLifecycleStatus.Active) {
                a.Status = ScoutingApproachStatus.Lapsed;
                Note($"{a.ArtistName} {ActGrammar.Verb(artist, "is", "are")} no longer in a position to take your offer.");
            } else if (ArtistManager.Instance.IsEligibleForPopulationSigning(artist, week)) {
                a.Status = ScoutingApproachStatus.Ready;
                var next = NextReachableScoutingSet(a.ArtistId);
                string where = ActNumber(a.ArtistId) is { } number
                    ? $"Open their card in the act notebook to talk terms, or ring {number.PersonName} to set up a meeting."
                    : next == null
                    ? "Open their card in the act notebook to talk terms. To meet them, catch a set or ask a booker for their number."
                    : $"Open their card in the act notebook to talk terms. Their next set: {next.RoomName}, {Headline(next.Day)}, {next.StartMinute / 60:00}:{next.StartMinute % 60:00}.";
                Note($"{a.ArtistName}'s side: they'll hear an offer from you now. {where} Other labels can hear of it too.");
            } else if (today > a.UntilDay) {
                a.Status = ScoutingApproachStatus.Lapsed;
                Note(Label?.HasRosterSpace == false
                    ? $"{a.ArtistName}'s side called back, but your roster was full. Ask them again when you have room."
                    : $"{a.ArtistName}'s side never called back. Ask them again the next time you see them.");
            }
        }
    }

    private bool IsConsideringApproach(string artistId) => scoutingKnowledge.Approaches?.Any(a => a.ArtistId == artistId &&
        a.Status is ScoutingApproachStatus.Considering or ScoutingApproachStatus.Ready or ScoutingApproachStatus.Agreed) == true;

    /// <summary>The notebook line for the latest approach, or null. Quotes what the act's side said when it's on file.</summary>
    internal string DirectApproachSummary(string artistId) {
        var a = LatestApproach(artistId);
        if (a == null) return null;
        string when = Headline(a.Day), until = Headline(a.UntilDay);
        string said = string.IsNullOrEmpty(a.Said) ? "" : " " + a.Said;
        var artist = ArtistManager.Instance?.GetArtist(artistId);
        return a.Status switch {
            ScoutingApproachStatus.Agreed => $"Asked ({when}): they said yes.{said}" + string.Concat(a.Steps.Select(s => s.Done
                ? $" Done ({Headline(s.DoneDay)}): {s.Said}" : " Still to do: " + StepAsk(s, artist))) +
                (a.Steps.All(s => s.Done) ? $" Their yes holds until {Headline(AgreementHoldsUntil(a))}." : ""),
            ScoutingApproachStatus.Considering => $"Asked ({when}): they're talking it over; answer due by {until}.{said}",
            ScoutingApproachStatus.Ready => $"Asked ({when}): they'll hear an offer. Talk terms from their card. Other labels can now hear they're available.",
            ScoutingApproachStatus.Declined => $"Asked ({when}): they said no.{said}",
            ScoutingApproachStatus.WantsAnotherLook => $"Asked ({when}): they want to see you again first.{said}",
            _ => $"Asked ({when}): it came to nothing. Asking again is a fresh conversation."
        };
    }
}
