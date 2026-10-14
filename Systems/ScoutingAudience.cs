using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Projects acquired whole-bill evidence only. Never refreshes from live calendars or artist truth.</summary>
public static class ScoutingAudience {
    private static bool Usable(ScoutingObservation o) =>
        o.Kind is ScoutingEvidenceKind.Performance or ScoutingEvidenceKind.BookerClaim &&
        o.BillAttendance >= 0 && o.BillCapacity > 0 && o.BillAttendance <= o.BillCapacity;

    // Same event/source is a copied report, not another source or another crowd.
    private static ScoutingObservation[] Reports(ScoutingDossier dossier) =>
        (dossier?.Observations ?? new()).Where(Usable)
            .Select((o, i) => (observation: o, key: !string.IsNullOrEmpty(o.EventId) ? $"{o.Kind}|{o.EventId}|{o.SourceId}" :
                !string.IsNullOrEmpty(o.Id) ? o.Id : $"unlinked|{i}"))
            .GroupBy(x => x.key, StringComparer.Ordinal).Select(g => g.First().observation)
            .OrderBy(o => o.OccurredDay ?? int.MinValue).ThenBy(o => o.AcquiredDay).ThenBy(o => o.AcquiredMinute).ToArray();

    private static string Room(ScoutingObservation o) =>
        (o.RoomName ?? "room name not recorded") + " (" + (o.PlaceName ?? "town name not recorded") + ")";
    private static string Date(int? day) => day.HasValue ? LocalSceneRoomService.Date(day.Value).ToHeadlineString() : "date unknown";
    private static string Fullness(ScoutingObservation o) {
        float fullness = (float)o.BillAttendance.Value / o.BillCapacity;
        return fullness >= .85 ? "nearly full" : fullness >= .6 ? "roughly three-quarters full" : fullness >= .35 ? "about half full" : "sparsely occupied";
    }
    private static string Context(ScoutingObservation o) =>
        (o.BillListedActs > 0 ? $"{o.BillListedActs} listed act(s)" : "bill size not recorded") + "; " +
        (o.BillAdmissionQuote is >= 0 && float.IsFinite(o.BillAdmissionQuote.Value)
            ? $"${o.BillAdmissionQuote:0.00} posted admission at your visit" : "admission not recorded");
    private static bool Disagree(IEnumerable<ScoutingObservation> reports) {
        var items = reports.ToArray();
        for (int i = 0; i < items.Length; i++) for (int j = i + 1; j < items.Length; j++) {
            var a = items[i]; var b = items[j];
            // A visual occupancy band cannot contradict a precise report within that same band.
            if (a.BillCapacity != b.BillCapacity ||
                (!string.IsNullOrEmpty(a.RoomId) && !string.IsNullOrEmpty(b.RoomId) && a.RoomId != b.RoomId) ||
                (a.OccurredDay.HasValue && b.OccurredDay.HasValue && a.OccurredDay != b.OccurredDay) ||
                (a.Kind == ScoutingEvidenceKind.BookerClaim && b.Kind == ScoutingEvidenceKind.BookerClaim
                    ? a.BillAttendance != b.BillAttendance : Fullness(a) != Fullness(b))) return true;
        }
        return false;
    }

    /// <summary>A recollection of acquired observations, never today's calendar.</summary>
    public static IReadOnlyList<string> RoomImpression(ScoutingDossier dossier) {
        var seen = dossier?.Observations.Where(o => o.Kind == ScoutingEvidenceKind.Performance && (o.SetPresent.HasValue || Usable(o)))
            .OrderBy(o => o.OccurredDay).ThenBy(o => o.EndMinute).LastOrDefault();
        var lines = new List<string>();
        string Sentence(string text) => char.ToUpperInvariant(text[0]) + text[1..] + ".";
        if (seen != null) {
            int? present = seen.SetPresent ?? seen.BillAttendance;
            float? ratio = present.HasValue && seen.BillCapacity > 0 ? (float)present.Value / seen.BillCapacity : null;
            string fullness = ratio >= .85f ? "nearly full" : ratio >= .6f ? "busy, with a few spaces left" : ratio >= .35f ? "about half full" : ratio.HasValue ? "quiet, with plenty of space" : "a crowd you didn't size up";
            lines.Add($"{seen.RoomName ?? "The room"} was {fullness} when you watched. · {Date(seen.OccurredDay)}");
            if (seen.SetStayedShare.HasValue) lines.Add(Sentence(Stayed(seen.SetStayedShare.Value)) + (seen.SetRequests > 0 ? " People called out for their songs." : ""));
            if (seen.SetArrivals.HasValue) lines.Add(Sentence(Arrivals(seen.SetArrivals.Value, seen.SetPresent ?? 0)));
        } else {
            var report = Reports(dossier).LastOrDefault();
            lines.Add(report == null ? "You haven't made a note of the crowd yet. Look around on your next visit." : $"{report.SourceName ?? "The booker"} described {report.RoomName ?? "the room"} as {Fullness(report)} on {Date(report.OccurredDay)}.");
        }
        return lines;
    }

    public static string Summary(ScoutingDossier dossier) {
        var reports = Reports(dossier);
        if (reports.Length == 0) return "Audience: no usable completed whole-bill attendance evidence acquired. Ask a room's booker about a completed bill; individual draw is unknown.";
        var linked = reports.Where(o => !string.IsNullOrEmpty(o.BillId)).GroupBy(o => o.BillId, StringComparer.Ordinal).ToArray();
        if (linked.Length == 0) return "Audience: retained reports have no bill identity; independent crowds cannot be counted. Review their sources. Individual draw and portable following remain unknown.";
        int rooms = linked.SelectMany(g => g).Select(o => o.RoomId).Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).Count();
        int places = linked.SelectMany(g => g).Select(o => o.PlaceId).Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).Count();
        bool conflict = linked.Any(g => Disagree(g));
        return $"Audience: {linked.Length} distinct whole-bill record(s), {rooms} recorded room(s), {places} recorded town(s). " +
            (conflict ? "Reports disagree on at least one bill; review the sources. " : "") +
            "Crowd size includes the other acts and room context; individual draw and portable following remain unknown.";
    }

    public static IReadOnlyList<string> Details(ScoutingDossier dossier) {
        var reports = Reports(dossier);
        var lines = new List<string> { Summary(dossier) };
        foreach (var group in reports.Select((o, i) => (o, key: !string.IsNullOrEmpty(o.BillId) ? o.BillId : $"unlinked|{i}"))
            .GroupBy(x => x.key, StringComparer.Ordinal)) {
            var observations = group.Select(x => x.o).ToArray();
            var first = observations[0];
            lines.Add($"{Room(first)} · {Date(first.OccurredDay)} · whole bill" +
                (string.IsNullOrEmpty(first.BillId) ? " · bill identity unknown" : "") +
                (Disagree(observations) ? " · conflicting reports" : "") + ".");
            foreach (var report in observations) {
                string receipt = Date(report.AcquiredDay) + (report.AcquiredMinute.HasValue ? $" at {report.AcquiredMinute / 60:00}:{report.AcquiredMinute % 60:00}" : " at unknown time");
                string audience = report.Kind == ScoutingEvidenceKind.BookerClaim
                    ? $"{report.SourceName ?? "Unnamed source"}'s retained report: {report.BillAttendance} attendees against {report.BillCapacity} capacity"
                    : $"Your listening: {Fullness(report)} against {report.BillCapacity} capacity; {report.Coverage:P0} of the scheduled set heard";
                string location = report.RoomId != first.RoomId || report.OccurredDay != first.OccurredDay
                    ? $" Reported at {Room(report)}, {Date(report.OccurredDay)}." : "";
                lines.Add($"{audience}. {Context(report)}.{location} Received {receipt}. " + (report.Credibility ?? "source credibility not recorded") + ".");
            }
        }
        if (reports.Length == 0) lines.Add("Listening before a bill completes does not supply its final attendance. Later calendar results do not fill old notes automatically.");
        lines.Add("A second set, listener or report from one bill is still one crowd. Capacity, admission, other acts and the room itself can differ.");
        lines.AddRange(DrawDetails(dossier));
        return lines.AsReadOnly();
    }

    // ---- Phase 5: who came for this act ---------------------------------------------------------------

    private static ScoutingObservation[] DrawReports(ScoutingDossier dossier) =>
        (dossier?.Observations ?? new()).Where(o => o.SetArrivals is >= 0 &&
                o.Kind is ScoutingEvidenceKind.Performance or ScoutingEvidenceKind.BookerClaim)
            .GroupBy(o => o.Kind == ScoutingEvidenceKind.Performance ? "seen|" + (o.EventId ?? o.Id) : "booker|" + o.RoomId, StringComparer.Ordinal)
            // A booker's later count supersedes the earlier one from the same room; a night you watched is one night.
            .Select(g => g.OrderBy(o => o.AcquiredDay).ThenBy(o => o.AcquiredMinute ?? 0).Last())
            .OrderBy(o => o.OccurredDay ?? o.AcquiredDay).ThenBy(o => o.AcquiredDay).ToArray();

    /// <summary>What a watcher at the back can tell about who came in for a set: a band, never a head count.</summary>
    private static string Arrivals(int arrived, int present) =>
        arrived <= 0 ? "nobody you could pick out came in for their set" :
        arrived < 8 ? "a handful came in for their set" :
        arrived < 20 ? "a dozen or so came in for their set" :
        present > 0 && arrived >= present * .45 ? "a good part of the room was there for them" : "a few dozen came in for their set";
    private static string Stayed(float share) =>
        share >= .75f ? "most of the room stayed through" : share >= .5f ? "about half the room stayed through" : "the room thinned while they played";

    /// <summary>The act's own draw where it has been seen: by you, set by set, or by a booker's count of their nights. Then
    /// whether it travels -- only rooms and towns where somebody actually saw people come for them count.</summary>
    public static IReadOnlyList<string> DrawDetails(ScoutingDossier dossier) {
        var reports = DrawReports(dossier);
        var lines = new List<string>();
        if (reports.Length == 0) {
            lines.Add("Their own draw: not seen yet. Watch most of a set to see who comes in for it, or ask a booker who comes in for them.");
            return lines;
        }
        foreach (var o in reports) {
            if (o.Kind == ScoutingEvidenceKind.Performance)
                lines.Add($"{Room(o)} · {Date(o.OccurredDay)} · your eyes: {Arrivals(o.SetArrivals.Value, o.SetPresent ?? 0)}; {Stayed(o.SetStayedShare ?? 0f)}" +
                    (o.SetRequests > 0 ? "; people called out for songs of theirs" : "") + ". One night in one room.");
            else
                lines.Add($"{Room(o)} · {o.SourceName ?? "the booker"}, received {Date(o.AcquiredDay)}: about {o.SetArrivals} a night come in for them over {o.DrawNights} night(s) there. Their count, their room.");
        }
        int rooms = reports.Where(o => o.SetArrivals > 0).Select(o => o.RoomId).Where(id => id != null).Distinct(StringComparer.Ordinal).Count();
        int towns = reports.Where(o => o.SetArrivals > 0).Select(o => o.PlaceId).Where(id => id != null).Distinct(StringComparer.Ordinal).Count();
        lines.Add(rooms == 0 ? "Their own draw: nobody has seen people come in for them yet."
            : towns > 1 ? $"Their own draw has been seen in {rooms} rooms across {towns} towns. It travels at least that far."
            : rooms > 1 ? $"Their own draw has been seen in {rooms} rooms in one town. Whether it travels out of town is unknown."
            : "Their own draw has been seen in one room only. Whether anybody would follow them to another room is unknown.");
        return lines;
    }
}
