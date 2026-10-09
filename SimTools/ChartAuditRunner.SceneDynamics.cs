using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
    private StreamWriter sceneDynamicsWriter;
    private StreamWriter sceneOpportunityWriter;
    private int sceneOpportunityYear = -1;
    private void WriteSceneDynamicsRows(int week, GameDate date) {
        if (!OS.GetCmdlineUserArgs().Contains("--observe-scene-dynamics")) return;
        var observations = SceneDynamicsObservations.Capture(date);
        if (sceneDynamicsWriter == null) {
            string dir = ProjectSettings.GlobalizePath("res://SimLogs");
            sceneDynamicsWriter = CreateWriter(Path.Combine(dir, $"{runName}-scene-dynamics-weekly.csv"));
            sceneDynamicsWriter.WriteLine("week,calendarWeek,year,placeId,residents,workingResidents,signedResidents,seekingResidents,latentResidents,bookableResidents,performanceSlots,bookedSlots,bookedActs,recurringActs,activeHqLabels,operatingSlotDeficit,bookableActsPerWeeklySlot");
            sceneOpportunityWriter = CreateWriter(Path.Combine(dir, $"{runName}-scene-opportunity-priors.csv"));
            sceneOpportunityWriter.WriteLine("year,placeId,genre,relativeWeight,relative1960Weight,ratioTo1960");
        }
        foreach (var r in observations) sceneDynamicsWriter.WriteLine(FormattableString.Invariant(
            $"{week},{r.CalendarWeek},{r.Year},{r.PlaceId},{r.Residents},{r.WorkingResidents},{r.SignedResidents},{r.SeekingResidents},{r.LatentResidents},{r.BookableResidents},{r.PerformanceSlots},{r.BookedSlots},{r.BookedActs},{r.RecurringActs},{r.LabelCount},{r.OperatingSlotDeficit},{r.BookableActsPerWeeklySlot?.ToString("F6", CultureInfo.InvariantCulture) ?? ""}"));
        int year = observations.First().Year;
        if (sceneOpportunityYear == year) return;
        sceneOpportunityYear = year;
        foreach (var city in observations) foreach (Genre genre in Enum.GetValues<Genre>()) {
            double weight = SceneCityPlacement.Weight(city.PlaceId, genre, year);
            double baseline = SceneCityPlacement.Weight(city.PlaceId, genre, 1960);
            sceneOpportunityWriter.WriteLine(FormattableString.Invariant($"{year},{city.PlaceId},{genre},{weight},{baseline},{weight / baseline}"));
        }
    }
}
