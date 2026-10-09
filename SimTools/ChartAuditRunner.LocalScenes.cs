using System;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
    /// <summary>Separate diagnostics; exclude only this file from matched economic hash comparisons.</summary>
    private void WriteSceneIdentityCensus(string phase) {
        if (!OS.GetCmdlineUserArgs().Contains("--local-scene-identity-audit")) return;
        string dir = ProjectSettings.GlobalizePath("res://SimLogs");
        Directory.CreateDirectory(dir);
        using var writer = new StreamWriter(Path.Combine(dir, $"{runName}-scene-identity-{phase}.csv"));
        writer.WriteLine("entity,id,legacyRegion,originPlaceId,basePlaceId,originEvidence,baseEvidence,assignmentSource");
        void Row(string type, string id, string region, GeographicIdentity geo) => writer.WriteLine(string.Join(",", new[] {
            type, Csv(id), Csv(region), Csv(geo?.originPlaceId), Csv(geo?.basePlaceId),
            (geo?.originEvidence ?? PlaceEvidence.Unknown).ToString(), (geo?.baseEvidence ?? PlaceEvidence.Unknown).ToString(), Csv(geo?.assignmentSource)
        }));
        foreach (var act in ArtistManager.Instance.GetAllArtists().OrderBy(a => a.artistId, StringComparer.Ordinal)) {
            Row("act", act.artistId, act.homeRegion, act.geography);
            foreach (var person in act.members.OrderBy(p => p.personId, StringComparer.Ordinal)) Row("person", person.personId, act.homeRegion, person.geography);
        }
        foreach (var pooled in PersonPool.All.OrderBy(p => p.person.personId, StringComparer.Ordinal)) Row("pooled-person", pooled.person.personId, pooled.homeRegion, pooled.person.geography);
        foreach (var label in ChartManager.Instance.GetAllLabels().OrderBy(l => l.labelId, StringComparer.Ordinal)) Row("label", label.labelId, label.homeRegion, label.geography);
    }
}
