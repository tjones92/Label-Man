using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Studio renown: a room becomes known for the label's records cut there. Historically a hit was what put a
/// room on the map and filled its calendar: Arthur Alexander's "You Better Move On" (cut at FAME, 1961) paid for Rick
/// Hall's larger Avalon Avenue studio and drew outside acts to it. Renown here is reputation and demand: a higher rate
/// with a lag, a busier chief and fuller calendar for acts the room doesn't owe. It is never a quality stat, and the label
/// that made the room's name keeps a regular's rate, so one hit does not price it out of its own room.
/// Every value is a pure function of remembered hits and the date; nothing draws gameplay randomness.</summary>
public partial class PlayerDesk {
    /// <summary>Weeks after a record first charts before word of it reaches other clients, and before it is fully felt.</summary>
    public const int StudioRenownLagWeeks = 4, StudioRenownFullWeeks = 16;
    /// <summary>Half-life of a hit's pull once it is fully felt: a room is known for a record for a couple of years.</summary>
    public const float StudioRenownHalfLifeWeeks = 104f;
    /// <summary>The most renown raises a room's posted rate, and the share of that rise the label that made its name pays.</summary>
    public const float StudioRenownRateRise = .40f, StudioRegularRateShare = .5f;
    /// <summary>On a renowned room's busiest days it is booked solid for acts it owes nothing to.</summary>
    public const float StudioBookedSolidAtFullRenown = .30f;

    /// <summary>What one charting record is worth to a room's name: a top-ten hit counts fully, a top-forty record half,
    /// a minor chart record a little. Provisional authored weights.</summary>
    public static float StudioHitWeight(int peak) => peak <= 0 ? 0 : peak <= 10 ? 1f : peak <= 40 ? .5f : .15f;

    /// <summary>How much of a hit's pull is felt this many days after it first charted: nothing for the first weeks,
    /// ramping to full, then fading.</summary>
    public static float StudioHitPull(int daysSinceCharted) {
        float weeks = daysSinceCharted / 7f;
        if (weeks < StudioRenownLagWeeks) return 0;
        if (weeks < StudioRenownFullWeeks) return (weeks - StudioRenownLagWeeks) / (StudioRenownFullWeeks - StudioRenownLagWeeks);
        return MathF.Pow(.5f, (weeks - StudioRenownFullWeeks) / StudioRenownHalfLifeWeeks);
    }

    /// <summary>The room's renown on a date, in [0, 1): saturating, so a second hit adds less than the first.</summary>
    public float StudioRenown(string roomId, GameDate date) {
        float sum = StudioHitsAt(roomId).Sum(h => StudioHitWeight(h.Peak) * StudioHitPull(LocalSceneRoomService.Day(date) - LocalSceneRoomService.Day(h.Charted)));
        return 1f - MathF.Exp(-sum / 1.5f);
    }

    public IEnumerable<StudioHit> StudioHitsAt(string roomId) =>
        studioVisits.Where(v => v.RoomId == roomId && v.Hits != null).SelectMany(v => v.Hits);
    private bool StudioActCutAHitAt(string roomId, string artistId) =>
        studioVisits.Any(v => v.RoomId == roomId && v.ArtistId == artistId && v.Hits?.Count > 0);
    private bool StudioLabelCutAHitAt(string roomId) => studioVisits.Any(v => v.RoomId == roomId && v.Hits?.Count > 0);

    /// <summary>Posts the room's renown and rate for this booking: the market rate, or the regular's rate for a label
    /// whose records made the room's name. Called before staffing, which reads the renown.</summary>
    private void ApplyStudioRenown(StudioRoomSnapshot room, string artistId, GameDate date) {
        room.BaseRate = room.HourlyRate;
        room.Renown = StudioRenown(room.Id, date);
        room.ActCutAHit = StudioActCutAHitAt(room.Id, artistId);
        float rise = StudioRenownRateRise * room.Renown * (StudioLabelCutAHitAt(room.Id) ? StudioRegularRateShare : 1);
        room.HourlyRate = Mathf.Round(room.BaseRate * (1 + rise));
    }

    /// <summary>Why a renowned room cannot take this act today, or null. The owner keeps time for the act that cut the
    /// room's hit; other acts take their chances with a full calendar. Keyed per room and day.</summary>
    private static string StudioBookedSolid(StudioRoomSnapshot room, GameDate date) {
        if (room.Renown <= 0 || room.ActCutAHit) return null;
        if (StudioTakeEvidence.Unit($"{room.Id}|booked-solid|{date.year}-{date.month}-{date.day}") >= StudioBookedSolidAtFullRenown * room.Renown) return null;
        return $"{room.Name} is booked solid today: the calendar has been full since word of its records got around. Try another day.";
    }

    /// <summary>The room's reputation in a sentence for its card, or null when it has none yet.</summary>
    public string StudioRenownText(StudioRoomSnapshot room) {
        if (room == null) return null;
        var hits = StudioHitsAt(room.Id).OrderBy(h => h.Peak).ThenBy(h => h.RecordId, StringComparer.Ordinal).ToList();
        if (hits.Count == 0) return null;
        var best = hits[0];
        string known = $"Known for \"{best.Title}\" by {best.ArtistName} (#{best.Peak}){(hits.Count > 1 ? $" and {hits.Count - 1} more of your records" : "")}.";
        if (room.Renown < .01f) return known + " Word of it hasn't got around yet; the rate and calendar are unchanged.";
        string rate = room.HourlyRate > room.BaseRate
            ? $" The posted rate went up from ${room.BaseRate:N0}; as the label that cut it you pay the regular's ${room.HourlyRate:N0}."
            : "";
        return known + rate + (room.ActCutAHit ? " The owner keeps time and the chief for this act." : " The room is busier: the chief is booked more often, and some days it is full.");
    }

    /// <summary>Weekly: notes how the records printed on remembered dates have charted. The best position only ever
    /// improves; the week a record first charts is the week the trade saw it.</summary>
    private void UpdateStudioHits(GameDate date) {
        if (studioVisits.Count == 0) return;
        var released = ReleasedRecords.Where(r => r.baseRecord?.recordId != null && r.peakPosition > 0).ToDictionary(r => r.baseRecord.recordId);
        if (released.Count == 0) return;
        foreach (var visit in studioVisits.Where(v => v.RecordIds?.Count > 0)) {
            foreach (string id in visit.RecordIds) {
                if (!released.TryGetValue(id, out var rec)) continue;
                visit.Hits ??= new();
                var hit = visit.Hits.FirstOrDefault(h => h.RecordId == id);
                if (hit == null) {
                    visit.Hits.Add(new StudioHit { RecordId = id, Title = rec.baseRecord.title, Charted = date, Peak = rec.peakPosition,
                        ArtistName = ArtistManager.Instance?.GetArtist(rec.baseRecord.artistId)?.stageName ?? "your act" });
                } else hit.Peak = Math.Min(hit.Peak, rec.peakPosition);
            }
        }
    }
}
