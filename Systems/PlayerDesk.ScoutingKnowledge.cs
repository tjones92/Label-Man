using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerDesk {
    private ScoutingKnowledgeSaveData scoutingKnowledge = new();
    private readonly Dictionary<string, Prospect> scoutingNegotiations = new(StringComparer.Ordinal);
    public IReadOnlyList<ScoutingDossier> ScoutingDossiers => LocalSceneRoomService.Copy(scoutingKnowledge.Dossiers).AsReadOnly();
    public FoundingActMemory FoundingAct => LocalSceneRoomService.Copy(scoutingKnowledge.FoundingAct);
    private ScoutingKnowledgeSaveData CaptureScoutingKnowledge() {
        var saved = LocalSceneRoomService.Copy(scoutingKnowledge);
        saved.Negotiations = scoutingNegotiations.Values.Select(p => new ScoutingNegotiationState {
            ArtistId = p.Artist.artistId, BusinessDiscussionId = p.BusinessDiscussionId, Baseline = p.Baseline, HasBaseline = p.HasBaseline, Draft = p.Draft, Posture = p.Posture,
            CooldownUntil = p.CooldownUntil, Countered = p.PushoverCountered, Talk = DetachedTalk(p.Talk), Log = p.Talk?.log.ToList() ?? new() }).ToList();
        return saved;
    }
    private static ContractTalk DetachedTalk(ContractTalk t) {
        if (t == null) return null;
        var copy = new ContractTalk { ask = t.ask, posture = t.posture, weights = t.weights == null ? null : new(t.weights),
            reservation = t.reservation, patienceMax = t.patienceMax, patienceLeft = t.patienceLeft, roundsPlayed = t.roundsPlayed,
            stage = t.stage, objectionAxis = t.objectionAxis, lastOfferValue = t.lastOfferValue, lastOffer = t.lastOffer, draftOffer = t.draftOffer, sessionPromised = t.sessionPromised,
            promotionPromised = t.promotionPromised, distributionPromised = t.distributionPromised, outlookCredit = t.outlookCredit, outlookLift = t.outlookLift, capabilityGap = t.capabilityGap,
            capabilityObjection = t.capabilityObjection, outlookStated = t.outlookStated?.ToList() ?? new() };
        copy.log.AddRange(t.log); return copy;
    }
    private void RememberScoutingNegotiation(Prospect prospect) {
        if (HasScoutingEvidence(prospect.Artist.artistId)) scoutingNegotiations[prospect.Artist.artistId] = prospect;
    }
    private void RestoreScoutingKnowledge(ScoutingKnowledgeSaveData saved) {
        scoutingKnowledge = LocalSceneRoomService.Copy(saved) ?? new();
        // A missing first-signature history cannot be reconstructed from today's roster.
        // Leave legacy memories intact, but never nominate a later signing as their founder.
        if (Label != null && (scoutingKnowledge.FoundingAct != null || !scoutingKnowledge.FoundingHistoryStarted.HasValue))
            scoutingKnowledge.FoundingHistoryStarted = true;
        scoutingKnowledge.Dossiers ??= new();
        scoutingKnowledge.Approaches?.RemoveAll(a => a == null || string.IsNullOrEmpty(a.ArtistId) || !Enum.IsDefined(a.Status));
        scoutingNegotiations.Clear();
        foreach (var state in scoutingKnowledge.Negotiations ?? new()) {
            var artist = ArtistManager.Instance?.GetArtist(state.ArtistId);
            if (artist == null) continue;
            var p = new Prospect { Artist = artist, BusinessDiscussionId = state.BusinessDiscussionId, Baseline = state.Baseline, HasBaseline = state.HasBaseline,
                Draft = state.Draft, Posture = state.Posture, CooldownUntil = state.CooldownUntil, PushoverCountered = state.Countered };
            p.Talk = DetachedTalk(state.Talk); if (p.Talk != null) { p.Talk.prospect = p; p.Talk.log.Clear(); p.Talk.log.AddRange(state.Log ?? new()); }
            scoutingNegotiations[state.ArtistId] = p;
        }
        scoutingKnowledge.Negotiations.Clear();
        foreach (var entry in notebook) {
            if (scoutingKnowledge.Dossiers.Any(d => d.ArtistId == entry.Artist.artistId)) continue;
            var dossier = Dossier(entry.Artist.artistId, entry.Artist.stageName);
            dossier.Priority = ScoutingPriority.Watch;
            dossier.Observations.Add(new() { Id = $"legacy|{Label.labelId}|{entry.Artist.artistId}", LabelId = Label.labelId,
                ArtistId = entry.Artist.artistId, ArtistName = entry.Artist.stageName, Kind = ScoutingEvidenceKind.LegacyImpression,
                AcquiredDay = LocalSceneRoomService.Day(entry.LastSeen), Channel = "old notebook", Credibility = "provenance unknown",
                Claim = "An earlier notebook impression survives. Its source and listening coverage were not recorded; it is not an independent performance sample." });
        }
    }
    private ScoutingDossier Dossier(string artistId, string name) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier != null) return dossier;
        dossier = new() { ArtistId = artistId, ArtistName = name }; scoutingKnowledge.Dossiers.Add(dossier); return dossier;
    }
    public bool HasScoutingEvidence(string artistId) => scoutingKnowledge.Dossiers.Any(d => d.ArtistId == artistId &&
        d.Observations.Any(o => o.Kind == ScoutingEvidenceKind.Performance));
    public bool SetScoutingPriority(string artistId, ScoutingPriority priority) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier == null || !Enum.IsDefined(priority)) return false;
        dossier.Priority = priority;
        if (priority == ScoutingPriority.Archived) notebook.RemoveAll(n => n.Artist?.artistId == artistId);
        Changed?.Invoke(); return true;
    }
    public bool OpenScoutingDossier(string artistId, out string message) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        var artist = ArtistManager.Instance?.GetArtist(artistId);
        if (dossier == null || artist == null) { message = "The historical notes remain, but this act is no longer available on the pad."; return false; }
        var last = dossier.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        if (last == null) { message = "Only a legacy impression is recorded. Hear an announced set for fresh evidence."; return false; }
        var room = SceneRoomCatalog.Get(last.RoomId);
        var existing = scoutingNegotiations.GetValueOrDefault(artistId) ?? slate.FirstOrDefault(p => p.Artist?.artistId == artistId);
        // Keep the actual negotiation alive when reopening, never redraw its asks or cooldown.
        var prospect = existing ?? new Prospect { Artist = artist, Venue = room?.Category ?? ScoutingVenue.ClubsAndRoadhouses,
            CityId = room?.PlaceId, SceneRoomId = last.RoomId, SceneBillId = last.BillId,
            AskingAdvance = VenueAdvanceAsk(artist, room?.Category ?? ScoutingVenue.ClubsAndRoadhouses, AskScaleFor(room?.PlaceId)),
            Note = "Notebook evidence; reopening is not another listening." };
        prospect.SceneRoomId = last.RoomId; prospect.SceneBillId = last.BillId;
        prospect.CityId = room?.PlaceId; prospect.Venue = room?.Category ?? prospect.Venue;
        prospect.AskingAdvance = prospect.HasBaseline ? prospect.Baseline.Advance : VenueAdvanceAsk(artist, prospect.Venue, AskScaleFor(prospect.CityId));
        if (prospect.LiveSet.Count == 0 && last.Coverage >= .9) {
            prospect.LiveSet.AddRange(last.Set.Select(s => new RepertoireItem { Title = s.Title, SongId = s.SongId, Genre = s.Genre,
                IsOriginal = s.Original, ContentContext = s.ContentContext, SourceTag = s.Original ? "their own" : "standard" }));
            prospect.HeardCount = prospect.LiveSet.Count;
        }
        if (!slate.Contains(prospect)) slate.Add(prospect);
        SlateDate = LocalSceneRoomService.Date(last.OccurredDay ?? last.AcquiredDay);
        message = $"Opened {dossier.ArtistName}'s recorded evidence. No time or money spent.";
        Changed?.Invoke(); return true;
    }
    private static string AxisDescription(float value) => value >= .72f ? "assured in this set" : value >= .48f ? "held together, with questions" : "uneven in this set";
    private static string PerformanceDescription(float value, ScoutingQuestion axis) {
        int level = value >= .72f ? 2 : value >= .48f ? 1 : 0;
        string[] words = axis switch {
            ScoutingQuestion.Vocals => new[] { "The singing sounded uncertain; another listen would help you judge the voice.", "The singing carried the songs, though it wasn't consistently convincing.", "The singing sounded confident and convincing." },
            ScoutingQuestion.Ensemble => new[] { "The playing felt uneven as a group. Hear them again to judge how well they work together.", "The group got through the set together, but the playing didn't always feel settled.", "The players sounded assured together, with a convincing sense of teamwork." },
            ScoutingQuestion.Material => new[] { "The songs didn't make a convincing impression. Hear more of their material before deciding.", "The songs had something to offer, but you weren't sold on the material as a whole.", "The songs made a strong impression and gave the act something worth hearing." },
            _ => new[] { "They struggled to put the performance across. Another room may give you a clearer impression.", "They put the songs across, though their stage presence wasn't consistently compelling.", "They came across confidently on stage and gave the performance a clear presence." }
        };
        return words[level];
    }
    public IReadOnlyList<string> ScoutingPlayerNotes(string artistId) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        var last = dossier == null ? null : ScoutingComparison.Performances(dossier).LastOrDefault();
        var read = last?.Interpretation;
        var sessions = new Queue<ScoutingObservation>((dossier?.Observations ?? new()).Where(o => o.Kind is ScoutingEvidenceKind.DemoSample or ScoutingEvidenceKind.RehearsalSample).TakeLast(2));
        return ScoutingSummary(artistId, false).Select(line => {
            string Limit(ScoutingQuestion axis) {
                var kind = SceneRoomCatalog.Get(last?.RoomId)?.Kind;
                return kind.HasValue && SceneRoomCharacter.Masks(kind.Value, axis) is { } mask ? " Keep in mind: " + mask + "." : "";
            }
            if (line.StartsWith("Heard ") && last != null) {
                var samples = ScoutingComparison.Performances(dossier);
                int dates = samples.Select(o => o.OccurredDay).Distinct().Count();
                return $"Listening history: {samples.Length} {(samples.Length == 1 ? "set" : "sets")} across {dates} {(dates == 1 ? "date" : "dates")}. Last heard at {last.RoomName ?? SceneRoomCatalog.Get(last.RoomId)?.Name}, {LocalSceneRoomService.Date(last.OccurredDay.Value).ToHeadlineString()}. " +
                    (last.Coverage >= .9f ? "You heard the full set." : "You missed part of this set.");
            }
            if (read != null) {
                if (line.StartsWith("Vocals:")) return "Vocals: " + (read.Vocals.HasValue ? PerformanceDescription(read.Vocals.Value, ScoutingQuestion.Vocals) + Limit(ScoutingQuestion.Vocals) : "This was an instrumental set; you haven't heard their singing.");
                if (line.StartsWith("Ensemble / execution:")) return "Ensemble: " + PerformanceDescription(read.Execution, ScoutingQuestion.Ensemble) + Limit(ScoutingQuestion.Ensemble);
                if (line.StartsWith("Material:")) return "Material: " + PerformanceDescription(read.Material, ScoutingQuestion.Material) + Limit(ScoutingQuestion.Material) +
                    (read.OriginalMaterial ? last.Coverage >= .9 ? " You heard an unrecorded original; hearing more of their own songs would help." : " An original was on the bill, but you missed part of the set." : " You heard familiar songs, so their own writing is still a question.");
                if (line.StartsWith("Stage communication:")) return "Stage presence: " + PerformanceDescription(read.Communication, ScoutingQuestion.Stage) + Limit(ScoutingQuestion.Stage);
            }
            if (line.StartsWith("Consistency:")) return PlayerConsistency(dossier);
            if (line.StartsWith("Follow-up:")) return ScoutingPlayerQuestionRead(dossier);
            if ((line.StartsWith("DemoSample:") || line.StartsWith("RehearsalSample:")) && sessions.Count > 0) {
                var sample = sessions.Dequeue();
                return (sample.Kind == ScoutingEvidenceKind.DemoSample ? "Demo: " : "Rehearsal: ") +
                    LocalSceneRoomService.Date(sample.OccurredDay ?? sample.AcquiredDay).ToHeadlineString() + ". " +
                    (sample.Interpretation is { } impression ? PerformanceDescription(impression.Execution, ScoutingQuestion.Ensemble) + " " +
                    (impression.Vocals.HasValue ? PerformanceDescription(impression.Vocals.Value, ScoutingQuestion.Vocals) : "No lead singing was heard.") : "You kept a note of the visit, but no musical impression.") +
                    " This was one agreed song with the players named for the visit.";
            }
            if (line.StartsWith("Audience:")) return "Audience: " + ScoutingAudience.RoomImpression(dossier).First();
            if (line.StartsWith("Recording readiness:")) return "Recording: You haven't heard them on a recording yet. A demo could help you judge how they come across in the studio.";
            if (line.StartsWith("What they want: no")) return "What they want: You haven't asked what they want from a record deal yet.";
            if (line.StartsWith("Competition: no")) return "Competition: You haven't heard of another signing. Ask the act where things stand.";
            if (line.StartsWith("Lineup changed")) return "Lineup: The people in the act have changed or moved on. These notes describe the players you heard then.";
            return line;
        }).ToArray();
    }
    public static string ScoutingPlayerQuestionRead(ScoutingDossier dossier) => dossier.Question == ScoutingQuestion.None ? null : PlayerConsistency(dossier, dossier.Question, "Follow-up: ");
    private static string PlayerConsistency(ScoutingDossier dossier, ScoutingQuestion question = ScoutingQuestion.Ensemble, string prefix = "Consistency: ") {
        float? Axis(ScoutingObservation o) => question switch {
            ScoutingQuestion.Vocals => o.Interpretation?.Vocals, ScoutingQuestion.Material => o.Interpretation?.Material,
            ScoutingQuestion.Stage => o.Interpretation?.Communication, _ => o.Interpretation?.Execution
        };
        string subject = question switch { ScoutingQuestion.Vocals => "singing", ScoutingQuestion.Material => "material", ScoutingQuestion.Stage => "stage presence", _ => "playing" };
        var samples = ScoutingComparison.Performances(dossier);
        if (samples.Length == 0) return prefix + "Hear a set first, then come back on another date to compare.";
        if (samples.Select(o => o.OccurredDay).Distinct().Count() < 2)
            return prefix + "You've heard them on one date. Come back another day to see whether this was a typical performance.";
        var last = samples.Last();
        var usable = samples.Where(o => o.OccurredDay < last.OccurredDay).GroupBy(o => o.OccurredDay).Select(g => g.Last())
            .Where(o => o.Coverage >= .9f && Axis(o).HasValue).ToArray();
        var earlier = usable.LastOrDefault(o => ScoutingComparison.SameContext(o, last)) ?? usable.LastOrDefault();
        if (last.Coverage < .9f || (earlier == null && samples.Where(o => o.OccurredDay < last.OccurredDay).All(o => o.Coverage < .9f)))
            return prefix + "You missed part of a set on one of your visits. Hear a full set on another date for a fair comparison.";
        if (earlier == null || !Axis(last).HasValue) return prefix + "You don't have a clear impression of their " + subject + " on both dates yet. Listen for it next time.";
        float difference = Axis(last).Value - Axis(earlier).Value;
        string impression = Math.Abs(difference) <= .2f ? $"The {subject} left a similar impression on both visits." : difference > 0 ? $"The {subject} made a stronger impression on your later visit." : $"The {subject} made a weaker impression on your later visit.";
        return prefix + impression + " " +
            (!ScoutingComparison.SameContext(earlier, last)
                ? "The room, players or songs changed too, so hear them again before deciding how dependable they are." : "Two visits give you a starting impression; more nights will tell you how dependable they are.");
    }
    public IReadOnlyList<(string Axis, string Read, string Limitation)> ScoutingListeningAxes(string artistId) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        var last = dossier == null ? null : ScoutingComparison.Performances(dossier).LastOrDefault();
        var read = last?.Interpretation;
        if (read == null) return Array.Empty<(string, string, string)>();
        var kind = SceneRoomCatalog.Get(last.RoomId)?.Kind;
        string Limit(ScoutingQuestion axis) => kind.HasValue ? SceneRoomCharacter.Masks(kind.Value, axis) : null;
        string Brief(float value, string low, string middle, string high) => value >= .72f ? high : value >= .48f ? middle : low;
        return new[] {
            ("Vocals", read.Vocals.HasValue ? Brief(read.Vocals.Value, "Uncertain singing", "Carried the songs; not always convincing", "Confident and convincing singing") : "No lead vocals heard", Limit(ScoutingQuestion.Vocals)),
            ("Playing", Brief(read.Execution, "Uneven as a group", "Together, but not always settled", "Assured playing together"), Limit(ScoutingQuestion.Ensemble)),
            ("Material", Brief(read.Material, "Songs didn't convince", "Something to offer; hear more songs", "Songs made a strong impression"), Limit(ScoutingQuestion.Material)),
            ("Stage presence", Brief(read.Communication, "Struggled to put the set across", "Put the songs across; presence varied", "Confident presence on stage"), Limit(ScoutingQuestion.Stage))
        };
    }
    public IReadOnlyList<string> ScoutingSummary(string artistId, bool includeOtherLeads = true) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier == null) return new[] { "An earlier impression; no performance provenance recorded. Further listening can answer specific questions." };
        var samples = ScoutingComparison.Performances(dossier);
        var lines = new List<string>();
        if (samples.Length == 0) lines.Add("No dated performance evidence; source and coverage of earlier impressions remain unknown.");
        else {
            var last = samples.Last(); var read = last.Interpretation;
            string place = last.RoomName ?? SceneRoomCatalog.Get(last.RoomId)?.Name ?? last.RoomId;
            int nights = samples.Select(o => o.OccurredDay).Distinct().Count();
            lines.Add($"Heard {samples.Length} distinct {(samples.Length == 1 ? "set" : "sets")} on {nights} {(nights == 1 ? "date" : "dates")}. Last: {place}, {LocalSceneRoomService.Date(last.OccurredDay.Value).ToHeadlineString()}; {last.Coverage:P0} of the scheduled set.");
            if (read != null) {
                var kind = SceneRoomCatalog.Get(last.RoomId)?.Kind;
                string Room(ScoutingQuestion axis) => kind.HasValue && SceneRoomCharacter.Masks(kind.Value, axis) is { } masked ? " (" + masked + ")" : "";
                lines.Add(read.Vocals.HasValue ? "Vocals: " + AxisDescription(read.Vocals.Value) + Room(ScoutingQuestion.Vocals) + "." : "Vocals: no lead-vocal evidence in this set.");
                lines.Add("Ensemble / execution: " + AxisDescription(read.Execution) + Room(ScoutingQuestion.Ensemble) + ".");
                lines.Add("Material: " + AxisDescription(read.Material) + Room(ScoutingQuestion.Material) + (read.OriginalMaterial ? (last.Coverage >= .9 ? "; heard an unrecorded original. More material could test this impression." : "; an original was scheduled, but partial coverage leaves what you heard uncertain.") : "; familiar material, no original writing demonstrated.") );
                lines.Add("Stage communication: " + AxisDescription(read.Communication) + Room(ScoutingQuestion.Stage) + ".");
            }
            if (ScoutingRoomGap(dossier, samples) is { } gap) lines.Add(gap);
            if (!dossier.Observations.Any(o => o.Kind == ScoutingEvidenceKind.DemoSample)) lines.Add("Recording readiness: no recording evidence acquired.");
            var current = ArtistManager.Instance?.GetArtist(artistId);
            var people = current?.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).OrderBy(id => id, StringComparer.Ordinal);
            if (people == null || !people.SequenceEqual(last.PersonIds.OrderBy(id => id, StringComparer.Ordinal)))
                lines.Add("Lineup changed or the act departed: affected assessments need new evidence. These notes describe the earlier people.");
            lines.Add(ScoutingComparison.Read(dossier, ScoutingQuestion.Ensemble, "Consistency: "));
        }
        foreach (var sample in dossier.Observations.Where(o => o.Kind is ScoutingEvidenceKind.DemoSample or ScoutingEvidenceKind.RehearsalSample).TakeLast(2))
            lines.Add($"{sample.Kind}: {sample.Conditions} {sample.Claim} This sample does not establish future potential; changed lineups need fresh evidence.");
        foreach (var claim in dossier.Observations.Where(o => o.Kind == ScoutingEvidenceKind.BookerClaim).TakeLast(2))
            lines.Add($"{claim.SourceName}, received {LocalSceneRoomService.Date(claim.AcquiredDay).ToHeadlineString()} (local source claim): {claim.Claim}");
        if (dossier.Question != ScoutingQuestion.None) {
            lines.Add("Listening question: " + ScoutingQuestionText(dossier.Question));
            if (dossier.Question != ScoutingQuestion.Ensemble || samples.Length == 0) lines.Add(ScoutingQuestionRead(dossier));
        }
        if (ScoutingListenerComparison(dossier) is { } comparison) lines.Add(comparison);
        if (ScoutingMeetingSummary(artistId) is { } meeting) lines.Add(meeting);
        lines.Add(ScoutingAudience.Summary(dossier));
        if (DirectApproachSummary(artistId) is { } approach) lines.Add(approach);
        lines.AddRange(ScoutingBusinessStatements(artistId));
        lines.Add(ScoutingCompetitionSummary(artistId));
        if (includeOtherLeads) {
            if (ScoutingOtherKnownLeads(artistId) is { } alternatives) lines.Add(alternatives);
            lines.Add(NextScoutingOpportunity(artistId));
        }
        return lines;
    }
    /// <summary>A question every room so far has hidden: which kind of room would answer it. The pinned question
    /// first, then material, vocals, ensemble and stage. Null once some room has let you hear it.</summary>
    internal static string ScoutingRoomGap(ScoutingDossier dossier, ScoutingObservation[] samples) {
        var kinds = samples.Select(o => SceneRoomCatalog.Get(o.RoomId)?.Kind).Where(k => k.HasValue).Select(k => k.Value).Distinct().ToArray();
        if (kinds.Length == 0) return null;
        var order = new[] { dossier.Question, ScoutingQuestion.Material, ScoutingQuestion.Vocals, ScoutingQuestion.Ensemble, ScoutingQuestion.Stage };
        foreach (var axis in order.Where(q => q != ScoutingQuestion.None).Distinct()) {
            if (axis == ScoutingQuestion.Vocals && samples.All(o => o.Interpretation?.Vocals == null)) continue;
            if (!kinds.All(k => SceneRoomCharacter.Masks(k, axis) != null)) continue;
            string where = string.Join(" and ", kinds.Select(k => SceneRoomCharacter.KindName(k) + "s"));
            string better = SceneRoomCharacter.BetterRoomFor(axis);
            return $"Rooms so far: heard only in {where}. {char.ToUpperInvariant(better[0])}{better.Substring(1)} would answer the {ScoutingAxisName(axis)} question better.";
        }
        return null;
    }
    private static string ScoutingAxisName(ScoutingQuestion axis) => axis switch {
        ScoutingQuestion.Vocals => "vocal", ScoutingQuestion.Ensemble => "ensemble", ScoutingQuestion.Material => "material", _ => "stage"
    };
    public IReadOnlyList<string> ScoutingAudienceDetails(string artistId) =>
        ScoutingAudience.Details(scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId));
    public string NextScoutingOpportunity(string artistId) {
        if (!LocalScenes.Rooms) return "The room calendar is disabled; recorded evidence is retained.";
        if (!LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver)
            return "New scouting encounters are disabled; recorded evidence is retained.";
        var next = NextReachableScoutingSet(artistId);
        if (next == null) return "Next: no unheard local set is announced within your working hours. Check other bills or ask a booker for a listening tip. No known rival report means only that nothing has reached you.";
        return $"Next reachable set: {next.RoomName}, {LocalSceneRoomService.Date(next.Day).ToHeadlineString()}, " +
            $"{next.StartMinute / 60:00}:{next.StartMinute % 60:00}–{next.EndMinute / 60:00}:{next.EndMinute % 60:00}. " +
            $"{next.ListeningMinutes}min listening; ${next.Admission:0.00} admission at the current quote. Waiting advances ordinary expenses and other labels' decisions; check the bill on arrival.";
    }
    public IReadOnlyList<string> ScoutingProvenance(string artistId) => scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations
        .Select(o => $"{o.Kind} · {o.SourceName ?? o.Channel} · {o.RoomName ?? SceneRoomCatalog.Get(o.RoomId)?.Name ?? ScenePlaceRegistry.Get(o.PlaceId)?.Name ?? "place unknown"} · occurred {(o.OccurredDay.HasValue ? LocalSceneRoomService.Date(o.OccurredDay.Value).ToHeadlineString() : o.OccurredWeek.HasValue ? "week beginning " + LocalSceneRoomService.Date(o.OccurredWeek.Value * 7).ToHeadlineString() : "unknown")}, acquired {LocalSceneRoomService.Date(o.AcquiredDay).ToHeadlineString()} at {(o.AcquiredMinute.HasValue ? $"{o.AcquiredMinute.Value / 60:00}:{o.AcquiredMinute.Value % 60:00}" : "unknown time")} · {o.Credibility} · {o.Conditions ?? o.Claim}" +
            (o.Kind == ScoutingEvidenceKind.BusinessStatement && o.BusinessStatementKind == ScoutingBusinessStatementKind.Counter && o.StatedTerms != null ? " · Counter terms: " + ScoutingTermsText(o.StatedTerms) : "") +
            (o.Kind == ScoutingEvidenceKind.ListenerOpinion ? $" · {o.Channel}" + (o.ConsultationFee > 0 ? $" · ${o.ConsultationFee:0.00} consultation paid" : "") : "") + (o.Kind is ScoutingEvidenceKind.Performance or ScoutingEvidenceKind.ListenerOpinion ? $" · listened {o.StartMinute / 60:00}:{o.StartMinute % 60:00}–{o.EndMinute / 60:00}:{o.EndMinute % 60:00} · {o.Coverage:P0} coverage · " +
                (o.Coverage >= .9 ? "Heard material: " : "Announced material; individual titles heard are uncertain: ") + string.Join("; ", o.Set.Select(s => s.Title + (s.Original ? " (original)" : " (standard)"))) : "")).ToArray() ?? Array.Empty<string>();
    public bool AskScoutingBooker(string roomId, string artistId, bool bookingHistory, out string message) =>
        AskScoutingSource(roomId, artistId, bookingHistory ? ScoutingSourceQuestion.BookingHistory : ScoutingSourceQuestion.NextDate, null, out message);
    private void RememberFoundingAct(Prospect prospect, ContractTermSheet terms) {
        RememberFoundingAct(prospect.Artist, terms);
    }
    private void RememberFoundingAct(SimulatedArtist artist, ContractTermSheet terms) {
        if (scoutingKnowledge.FoundingAct != null || scoutingKnowledge.FoundingHistoryStarted != false) return;
        scoutingKnowledge.FoundingHistoryStarted = true;
        if (Label.CurrentRosterSize != 1) return;
        var last = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artist.artistId)?.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        scoutingKnowledge.FoundingAct = new() { ArtistId = artist.artistId, ArtistName = artist.stageName,
            LabelId = Label.labelId, Day = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate), Terms = terms,
            Minute = TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute,
            ObservationId = last?.Id, PersonIds = last?.PersonIds.ToList() ?? new(),
            Context = last == null ? "Listening provenance was not recorded." : $"Heard at {last.RoomName ?? SceneRoomCatalog.Get(last.RoomId)?.Name ?? "a room whose name was not recorded"}, {LocalSceneRoomService.Date(last.OccurredDay ?? last.AcquiredDay).ToHeadlineString()}; {last.Coverage:P0} of the scheduled set." };
    }
    public string FoundingActSummary() {
        var memory = scoutingKnowledge.FoundingAct;
        if (memory == null) return scoutingKnowledge.FoundingHistoryStarted == true
            ? "First name on the label: the first signature was not recorded in this history. Later deals will not replace it."
            : null;
        string time = memory.Minute.HasValue ? $" at {memory.Minute.Value / 60:00}:{memory.Minute.Value % 60:00}" : "";
        string treatment = FoundingTreatmentSummary(memory.ArtistId);
        return $"FIRST NAME ON THE LABEL: {memory.ArtistName}, {LocalSceneRoomService.Date(memory.Day).ToHeadlineString()}{time}. {memory.Context} " +
            "Original agreement: " + ScoutingTermsText(ScoutingStatedTerms.From(memory.Terms)) + (treatment.Length > 0 ? " " + treatment : "");
    }
}
