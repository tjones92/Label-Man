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
        lines.Add("A second set, listener or report from one bill is still one crowd. These records do not establish who came for this act, who stayed for it, or who would travel to another room. Capacity, admission, other acts and the room itself can differ.");
        return lines.AsReadOnly();
    }
}
