using System;
using System.Linq;

/// <summary>Small shared live snapshot. Only the performance owner creates it at resolution.
/// Keyed variation describes this event; it does not change work, fees, growth or recruitment.</summary>
public static class ScenePerformanceService {
    public static string EventId(SceneBill bill, SceneAppearance slot) => $"performance-v1|{bill.Id}|{slot.ArtistId}|{slot.StartHour}";
    public static ScenePerformanceSnapshot Resolve(SceneBill bill, SceneAppearance slot, SimulatedArtist artist) {
        var people = artist.members.Where(m => slot.PersonIds.Contains(m.personId)).ToArray();
        var voices = people.Where(m => m.isLeadVocalist).ToArray();
        string id = EventId(bill, slot);
        float Sample(string axis, float basis) => Math.Clamp(basis + (float)(LocalSceneIdentityService.AssignmentUnit(
            LocalSceneIdentityService.KeyedSeed, id + "|" + axis) * 2 - 1) * .12f, 0, 1);
        float execution = people.Length == 0 ? artist.musicianship : people.Average(m => m.axesVersion > 0 ? m.instrumentalSkill : m.technicalSkill);
        var knownSongs = slot.Set.Select(s => string.IsNullOrEmpty(s.SongId) ? null : CompositionCatalogService.GetSong(s.SongId)).Where(s => s != null).ToArray();
        bool originals = slot.Set.Any(s => s.Original);
        float material = originals ? artist.songwritingAbility : knownSongs.Length > 0 ? knownSongs.Average(s => s.GetCraftScore()) : .5f;
        return new() { Id = id, Vocals = voices.Length == 0 ? null : Sample("vocals", voices.Average(m => m.axesVersion > 0 ? (m.vocalPower + m.vocalControl) / 2 : m.technicalSkill)),
            Execution = Sample("execution", execution), Material = Sample("material", material),
            Communication = Sample("communication", people.Length > 0 ? people.Average(m => m.stagePresence) : artist.livePerformance), OriginalMaterial = originals };
    }
    public static ScenePerformanceSnapshot Interpret(ScenePerformanceSnapshot live, string labelId, float ability, float coverage) {
        float noise = .08f + .20f * (1 - Math.Clamp(ability, 0, 1)) + .08f * (1 - coverage);
        float Read(string axis, float value) {
            // Stable observer blind spot plus event-specific error. Reopening evidence never improves it.
            float bias = (float)(LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed, $"scout-bias-v1|{labelId}|{axis}") * 2 - 1);
            float error = (float)(LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed, $"scout-event-v1|{labelId}|{live.Id}|{axis}") * 2 - 1);
            return Math.Clamp(value + noise * (.45f * bias + .55f * error), 0, 1);
        }
        return new() { Id = live.Id, Vocals = live.Vocals.HasValue ? Read("vocals", live.Vocals.Value) : null,
            Execution = Read("execution", live.Execution), Material = Read("material", live.Material), Communication = Read("communication", live.Communication), OriginalMaterial = live.OriginalMaterial };
    }
}
