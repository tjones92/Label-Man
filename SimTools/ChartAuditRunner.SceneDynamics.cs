using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
    private StreamWriter sceneDynamicsWriter;
    private StreamWriter sceneOpportunityWriter;
    private StreamWriter sceneMoveWriter;
    private StreamWriter sceneAttentionWriter;
    private StreamWriter sceneAttentionEvidenceWriter;
    private StreamWriter sceneFeedbackWriter;
    private StreamWriter sceneEcosystemWriter;
    private int sceneOpportunityYear = -1;
    private void WriteSceneDynamicsRows(int week, GameDate date) {
        if (OS.GetCmdlineUserArgs().Contains("--scene-ecosystem-audit")) {
            if (sceneEcosystemWriter == null) {
                sceneEcosystemWriter = CreateWriter(Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), $"{runName}-scene-ecosystem-weekly.csv"));
                sceneEcosystemWriter.WriteLine("week,institutionsEnabled,relocationEnabled,priceEnabled,activePrograms,plannedMoves,arrivals,cancellations,maximumPriceMultiplier");
            }
            int day = LocalSceneRoomService.Day(date);
            var programs = SceneEcosystemService.Programs;
            var moves = SceneEcosystemService.Moves;
            float maximumPrice = ArtistManager.Instance.GetAllArtists().Select(ScenePriceFeedback.Multiplier).DefaultIfEmpty(1).Max();
            sceneEcosystemWriter.WriteLine(FormattableString.Invariant($"{week},{LocalScenes.Institutions},{LocalScenes.Relocation},{LocalScenes.PriceFeedback},{programs.Count(p => p.StartDay <= day && p.ThroughDay >= day)},{moves.Count(m => m.Status == SceneMoveStatus.Planned)},{moves.Count(m => m.Status == SceneMoveStatus.Arrived)},{moves.Count(m => m.Status == SceneMoveStatus.Cancelled)},{maximumPrice:F12}"));
        }
        if (LocalScenes.AttentionFeedbackAudit) {
            if (sceneFeedbackWriter == null) {
                string feedbackDir = ProjectSettings.GlobalizePath("res://SimLogs");
                sceneFeedbackWriter = CreateWriter(Path.Combine(feedbackDir, $"{runName}-scene-feedback-weekly.csv"));
                sceneFeedbackWriter.WriteLine("week,enabled,inputWeek,slateCalls,boostedCandidateObservations,boostedSelectionObservations,changedSlates,maximumMultiplier");
            }
            var feedback = SceneAttentionFeedback.DrainDiagnostics();
            sceneFeedbackWriter.WriteLine(FormattableString.Invariant(
                $"{week},{LocalScenes.AttentionFeedback},{week - 1},{feedback.SlateCalls},{feedback.BoostedCandidateObservations},{feedback.BoostedSelectionObservations},{feedback.ChangedSlates},{feedback.MaximumMultiplier:F12}"));
        }
        if (!OS.GetCmdlineUserArgs().Contains("--observe-scene-dynamics")) return;
        var observations = SceneDynamicsObservations.Capture(date);
        if (sceneDynamicsWriter == null) {
            string dir = ProjectSettings.GlobalizePath("res://SimLogs");
            sceneDynamicsWriter = CreateWriter(Path.Combine(dir, $"{runName}-scene-dynamics-weekly.csv"));
            sceneDynamicsWriter.WriteLine("week,calendarWeek,year,placeId,residents,workingResidents,signedResidents,seekingResidents,latentResidents,bookableResidents,performanceSlots,bookedSlots,bookedActs,recurringActs,activeHqLabels,operatingSlotDeficit,bookableActsPerWeeklySlot,opportunityCoverage,fullEmploymentSlots");
            sceneMoveWriter = CreateWriter(Path.Combine(dir, $"{runName}-scene-move-observations.csv"));
            sceneMoveWriter.WriteLine("week,year,window,artistId,originPlaceId,fromPlaceId,toPlaceId,genre,roadMiles,travelDays,opportunityRatio,reason,unresolvedRequirements");
            sceneAttentionWriter = CreateWriter(Path.Combine(dir, $"{runName}-scene-attention-weekly.csv"));
            sceneAttentionWriter.WriteLine("week,placeId,contributingEvents,signingHeat,rawDecayedImpulse");
            sceneAttentionEvidenceWriter = CreateWriter(Path.Combine(dir, $"{runName}-scene-attention-evidence.csv"));
            sceneAttentionEvidenceWriter.WriteLine("observationWeek,eventId,artistId,labelId,signingWeek,placeId,signingGenre,attribution,decayedImpulse,provenance");
            sceneOpportunityWriter = CreateWriter(Path.Combine(dir, $"{runName}-scene-opportunity-priors.csv"));
            sceneOpportunityWriter.WriteLine("year,placeId,genre,relativeWeight,relative1960Weight,ratioTo1960");
        }
        foreach (var r in observations) sceneDynamicsWriter.WriteLine(FormattableString.Invariant(
            $"{week},{r.CalendarWeek},{r.Year},{r.PlaceId},{r.Residents},{r.WorkingResidents},{r.SignedResidents},{r.SeekingResidents},{r.LatentResidents},{r.BookableResidents},{r.PerformanceSlots},{r.BookedSlots},{r.BookedActs},{r.RecurringActs},{r.LabelCount},{r.OperatingSlotDeficit},{r.BookableActsPerWeeklySlot?.ToString("F6", CultureInfo.InvariantCulture) ?? ""},NamedSupportingRoomsOnly,"));
        foreach (var p in SceneDynamicsMobility.Capture(date)) sceneMoveWriter.WriteLine(FormattableString.Invariant(
            $"{week},{p.Year},{p.Window},{Csv(p.ArtistId)},{Csv(p.OriginPlaceId)},{p.FromPlaceId},{p.ToPlaceId},{p.Genre},{p.RoadMiles:F6},{p.TravelDays},{p.OpportunityRatio:F6},{Csv(p.Reason)},{Csv(p.UnresolvedRequirements)}"));
        var attention = SceneDynamicsAttention.Capture(week);
        foreach (var p in attention.Cities) sceneAttentionWriter.WriteLine(FormattableString.Invariant(
            $"{week},{p.PlaceId},{p.ContributingEvents},{p.SigningHeat:F9},{p.RawDecayedImpulse:F9}"));
        foreach (var e in attention.Evidence) sceneAttentionEvidenceWriter.WriteLine(FormattableString.Invariant(
            $"{week},{Csv(e.EventId)},{Csv(e.ArtistId)},{Csv(e.LabelId)},{e.SigningWeek},{e.PlaceId},{e.SigningGenre?.ToString() ?? ""},{e.Attribution:F6},{e.DecayedImpulse:F9},{Csv(e.Provenance)}"));
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
