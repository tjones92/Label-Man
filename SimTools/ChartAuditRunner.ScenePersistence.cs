using System;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
    private void WriteSceneParticipationCensus(string phase) {
        if (!OS.GetCmdlineUserArgs().Contains("--local-scene-persistence-audit")) return;
        string dir = ProjectSettings.GlobalizePath("res://SimLogs");
        Directory.CreateDirectory(dir);
        using var writer = new StreamWriter(Path.Combine(dir, $"{runName}-scene-participation-{phase}.csv"));
        writer.WriteLine("artistId,placeId,sceneId,relationship,performanceLevel,startWeek,endWeek,dealStatus,labelId,lifecycle");
        int residents = 0, duplicateResidents = 0, terminalResidents = 0, missingPeople = 0;
        foreach (var act in ArtistManager.Instance.GetAllArtists().OrderBy(a => a.artistId, StringComparer.Ordinal)) {
            int current = act.sceneParticipations?.Count(p => p.relationship == SceneRelationship.Resident && !p.endWeek.HasValue) ?? 0;
            residents += current;
            if (current > 1) duplicateResidents++;
            if (current > 0 && act.lifecycleStatus != ArtistLifecycleStatus.Active) terminalResidents++;
            foreach (var person in act.members.Where(p => p.isActive))
                if (!ReferenceEquals(ArtistManager.Instance.GetMusician(person.personId), person)) missingPeople++;
            foreach (var p in act.sceneParticipations ?? new()) writer.WriteLine(string.Join(",", new[] {
                Csv(act.artistId), Csv(p.placeId), Csv(p.sceneId), p.relationship.ToString(), p.performanceLevel.ToString(),
                p.startWeek.ToString(), p.endWeek?.ToString() ?? "", act.prospectMarketStatus.ToString(), Csv(act.labelId), act.lifecycleStatus.ToString() }));
        }
        GD.Print($"SCENE_PARTICIPATION_CENSUS phase={phase} residents={residents} duplicateResidents={duplicateResidents} terminalResidents={terminalResidents} missingActivePeople={missingPeople}");
        if (duplicateResidents > 0 || terminalResidents > 0 || missingPeople > 0)
            throw new InvalidOperationException("Scene participation census failed authoritative membership/person reconciliation.");
    }
}
