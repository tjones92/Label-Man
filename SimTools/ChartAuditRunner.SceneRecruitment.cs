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
        if (phase == "start" && LocalScenes.Recruitment) {
            using var initialization = new StreamWriter(Path.Combine(dir, $"{runName}-scene-initialization-labels.csv"));
            initialization.WriteLine("labelId,tier,hqPlaceId,target,filled,unfilled,accessiblePreferred,accessibleSecondary,accessibleAny,meanTrueQuality,strongActs");
            foreach (var label in ChartManager.Instance.GetAllLabels().OrderBy(l => l.labelId, StringComparer.Ordinal)) {
                if (!RosterManager.Instance.SceneInitialization.TryGetValue(label.labelId, out var o)) continue;
                initialization.WriteLine(string.Join(",", new[] { Csv(label.labelId), label.tier.ToString(), Csv(label.geography?.basePlaceId),
                    o.Target.ToString(), o.Filled.ToString(), (o.Target - o.Filled).ToString(),
                    o.AccessiblePreferred.ToString(), o.AccessibleSecondary.ToString(), o.AccessibleAny.ToString(),
                    o.MeanQuality.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), o.StrongActs.ToString() }));
            }
            using var appointments = new StreamWriter(Path.Combine(dir, $"{runName}-scene-initialization-appointments.csv"));
            appointments.WriteLine("round,labelId,artistId,slateSize,trueQuality,perceivedQuality");
            foreach (var a in RosterManager.Instance.SceneInitializationAppointments)
                appointments.WriteLine(string.Join(",", new[] { a.Round.ToString(), Csv(a.LabelId), Csv(a.ArtistId), a.SlateSize.ToString(),
                    a.TrueQuality.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                    a.PerceivedQuality.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) }));
        }

        var supply = ArtistManager.Instance.GetUnsignedArtists();
        foreach (var label in ChartManager.Instance.GetAllLabels().OrderBy(l => l.labelId, StringComparer.Ordinal)) {
            var slate = RosterManager.SceneSupplyForProbe(label, TimeManager.Instance.CurrentDate.year, false, supply, out int count);
            labels.WriteLine(string.Join(",", new[] { Csv(label.labelId), label.tier.ToString(), Csv(label.geography?.basePlaceId),
                label.IsActive.ToString(), label.CurrentRosterSize.ToString(), label.OperatingRosterTarget.ToString(),
                Math.Max(0, label.OperatingRosterTarget - label.CurrentRosterSize).ToString(), count.ToString(), slate.Count.ToString() }));
        }
    }
}
