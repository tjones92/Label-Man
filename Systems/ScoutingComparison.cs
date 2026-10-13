using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Reads only retained observations. No world rolls, state writes, or private artist fields.</summary>
public static class ScoutingComparison {
    private static bool SamePeople(ScoutingObservation a, ScoutingObservation b) =>
        a.PersonIds.Count > 0 && b.PersonIds.Count > 0 &&
        a.PersonIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(b.PersonIds.OrderBy(id => id, StringComparer.Ordinal)) &&
        a.BackingPersonIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(b.BackingPersonIds.OrderBy(id => id, StringComparer.Ordinal));

    private static bool SameMaterial(ScoutingObservation a, ScoutingObservation b) =>
        a.Set.Count > 0 && b.Set.Count > 0 &&
        a.Set.Select(s => (s.SongId ?? s.Title, s.Original)).SequenceEqual(b.Set.Select(s => (s.SongId ?? s.Title, s.Original)));

    private static bool SameRoom(ScoutingObservation a, ScoutingObservation b) =>
        !string.IsNullOrEmpty(a.RoomId) && a.RoomId == b.RoomId;

    internal static bool SameContext(ScoutingObservation a, ScoutingObservation b) =>
        SamePeople(a, b) && SameMaterial(a, b) && SameRoom(a, b);

    private static float? Axis(ScoutingObservation o, ScoutingQuestion question) => question switch {
        ScoutingQuestion.Vocals => o.Interpretation?.Vocals,
        ScoutingQuestion.Material => o.Interpretation?.Material,
        ScoutingQuestion.Stage => o.Interpretation?.Communication,
        _ => o.Interpretation?.Execution
    };

    internal static ScoutingObservation[] Performances(ScoutingDossier dossier) =>
        dossier.Observations.Where(o => o.Kind == ScoutingEvidenceKind.Performance && o.OccurredDay.HasValue)
            .OrderBy(o => o.OccurredDay).ThenBy(o => o.StartMinute).ThenBy(o => o.EndMinute)
            .Select((o, index) => (observation: o, key: o.EventId ?? o.Id ?? $"unknown|{index}"))
            .GroupBy(x => x.key, StringComparer.Ordinal).Select(g => g.First().observation)
            .OrderBy(o => o.OccurredDay).ThenBy(o => o.StartMinute).ThenBy(o => o.EndMinute).ToArray();

    // The latest acquired set is the question's subject. Prefer a complete earlier date
    // with comparable recorded context; never average sets from one night into consistency.
    public static string Read(ScoutingDossier dossier, ScoutingQuestion question, string prefix) {
        var samples = Performances(dossier);
        if (samples.Length == 0) return prefix + "hear an announced set first; an old impression or source claim cannot answer this question.";
        var latest = samples.Last();
        var priorDates = samples.Where(o => o.OccurredDay < latest.OccurredDay)
            .GroupBy(o => o.OccurredDay).Select(g => g.Last()).ToArray();
        if (priorDates.Length == 0) return prefix + "another date is needed. More sets or reports from this night cannot establish consistency.";
        if (latest.Coverage < .9f) return prefix + "coverage was partial on the latest date. Hear a complete set before drawing a comparison.";
        if (!Axis(latest, question).HasValue) return prefix + MissingAxis(question);
        var usable = priorDates.Where(o => o.Coverage >= .9f && Axis(o, question).HasValue).ToArray();
        if (usable.Length == 0) return prefix + (priorDates.All(o => o.Coverage < .9f) ?
            "coverage was partial on the earlier dates. Hear a complete set on another date before drawing a comparison." : MissingAxis(question));
        var earlier = usable.LastOrDefault(o => SameContext(o, latest)) ?? usable.Last();
        float difference = Axis(latest, question).Value - Axis(earlier, question).Value;
        string impression = Math.Abs(difference) <= .2f ? "broadly similar impressions" :
            difference > 0 ? "a stronger impression on the later date" : "a less assured impression on the later date";
        var context = new List<string>();
        if (!SamePeople(earlier, latest)) context.Add(earlier.PersonIds.Count == 0 || latest.PersonIds.Count == 0 ? "people unknown" : "performing or backing people differed");
        if (!SameMaterial(earlier, latest)) context.Add(earlier.Set.Count == 0 || latest.Set.Count == 0 ? "material unknown" : "material or its order differed");
        if (!SameRoom(earlier, latest)) context.Add(string.IsNullOrEmpty(earlier.RoomId) || string.IsNullOrEmpty(latest.RoomId) ? "room unknown" : "room differed");
        string basis = context.Count == 0 ? "similar recorded people, material and room" : string.Join("; ", context);
        return prefix + $"{impression}, comparing {LocalSceneRoomService.Date(earlier.OccurredDay.Value).ToHeadlineString()} and " +
            $"{LocalSceneRoomService.Date(latest.OccurredDay.Value).ToHeadlineString()}; {basis}. " +
            (context.Count == 0 ? "Two dated sets support this comparison, not a verdict on every night. " :
                "These contexts cannot establish consistency under the same conditions. ") +
            "These are historical listening judgments; the cause and future development remain open.";
    }

    private static string MissingAxis(ScoutingQuestion question) => question == ScoutingQuestion.Vocals ?
        "there is no comparable vocal evidence on both dates. An instrumental set leaves the vocal question open." :
        "an acquired interpretation is missing for this question. Hear another complete set; old notes cannot supply the missing judgment.";
}
