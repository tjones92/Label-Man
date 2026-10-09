using System;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
    private void WriteSceneRecruitmentCensus(string phase) {
        if (!OS.GetCmdlineUserArgs().Contains("--scene-recruitment-audit")) return;
        string dir = ProjectSettings.GlobalizePath("res://SimLogs");
        Directory.CreateDirectory(dir);
        using var writer = new StreamWriter(Path.Combine(dir, $"{runName}-scene-recruitment-{phase}.csv"));
        writer.WriteLine("labelId,artistId,week,year,phase,route,hqPlaceId,basePlaceId,originPlaceId,roadMiles,accessEvidence,explanation");
        foreach (var act in ArtistManager.Instance.GetAllArtists().OrderBy(a => a.artistId, StringComparer.Ordinal))
            foreach (var r in act.sceneRecruitmentHistory ?? new())
                writer.WriteLine(string.Join(",", new[] { Csv(r.LabelId), Csv(r.ArtistId), r.Week.ToString(), r.Year.ToString(),
                    Csv(r.Phase), Csv(r.Route), Csv(r.HqPlaceId), Csv(r.BasePlaceId), Csv(r.OriginPlaceId),
                    r.RoadMiles?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) ?? "", Csv(r.AccessEvidence), Csv(r.Explanation) }));
        using var labels = new StreamWriter(Path.Combine(dir, $"{runName}-scene-recruitment-labels-{phase}.csv"));
        labels.WriteLine("labelId,tier,hqPlaceId,active,rosterSize,operatingTarget,unfilledSlots,accessiblePool,discoverySlate");
        var supply = ArtistManager.Instance.GetUnsignedArtists();
        foreach (var label in ChartManager.Instance.GetAllLabels().OrderBy(l => l.labelId, StringComparer.Ordinal)) {
            var slate = RosterManager.SceneSupplyForProbe(label, TimeManager.Instance.CurrentDate.year, false, supply, out int count);
            labels.WriteLine(string.Join(",", new[] { Csv(label.labelId), label.tier.ToString(), Csv(label.geography?.basePlaceId),
                label.IsActive.ToString(), label.CurrentRosterSize.ToString(), label.OperatingRosterTarget.ToString(),
                Math.Max(0, label.OperatingRosterTarget - label.CurrentRosterSize).ToString(), count.ToString(), slate.Count.ToString() }));
        }
    }
}
