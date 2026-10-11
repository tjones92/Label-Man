using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

public static class ScoutingCompetitionChecks {
    public static int Run(string artistId) {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_COMPETITION_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        var saved = desk.CaptureState(); var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        var act = am.GetArtist(artistId); var history = act.sceneRecruitmentHistory;
        string owner = act.labelId;
        var rival = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned);
        var row = new SceneRecruitmentRecord { ArtistId = artistId, LabelId = rival.labelId, Week = 4,
            Phase = "DailyMarket", BasePlaceId = desk.CurrentCityId };
        try {
            act.sceneRecruitmentHistory = new() { row, row };
            clock.RestoreClock(LocalSceneRoomService.Date(34), 10, 15);
            Check(!desk.LocalSceneNews().Any(n => n.ArtistId == artistId), "unpublished signing stays private");
            string knowledge = Json(desk.CaptureState().ScoutingKnowledge);
            desk.MarkSceneNewsRead();
            Check(Json(desk.CaptureState().ScoutingKnowledge) == knowledge, "reading before publication acquires no signing");
            clock.RestoreClock(LocalSceneRoomService.Date(35), 10, 15);
            float cash = desk.Label.cashReserves; int population = am.GetAllArtists().Count();
            LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate);
            Godot.GD.Seed(47); uint draw = Godot.GD.Randi(); Godot.GD.Seed(47);
            string world = Json(WorldStateService.Capture());
            var news = desk.LocalSceneNews(); desk.ScoutingSummary(artistId); desk.LocalSceneNews();
            Check(news.Count(n => n.ArtistId == artistId) == 1, "one public report per authoritative signing");
            Check(Json(desk.CaptureState().ScoutingKnowledge) == knowledge, "previews do not acquire reports");
            Check(Json(WorldStateService.Capture()) == world, "previews preserve world");
            Check(Godot.GD.Randi() == draw, "previews preserve RNG");
            world = Json(WorldStateService.Capture());
            Check(desk.SetScoutingPriority(artistId, ScoutingPriority.Archived), "archive known act before publication receipt");
            desk.MarkSceneNewsRead();
            var reports = desk.ScoutingDossiers.Single(d => d.ArtistId == artistId).Observations.Where(o => o.Kind == ScoutingEvidenceKind.PublicReport).ToArray();
            Check(reports.Length == 1, "explicit recap acquisition includes archived dossier and deduplicates rows");
            var report = reports.Single();
            Check(report.OccurredDay == null && report.OccurredWeek == 4 && report.PublishedDay == 35 &&
                report.AcquiredDay == 35 && report.AcquiredMinute == 615, "honest week precision, publication and receipt preserved");
            Check(report.PlaceId == desk.CurrentCityId && report.SourceName == "Local trade report" && report.Claim.Contains(rival.labelName), "source, city and published rival identity retained");
            Check(desk.Label.cashReserves == cash && clock.CurrentHour == 10 && clock.CurrentMinute == 15 &&
                population == am.GetAllArtists().Count() && Json(WorldStateService.Capture()) == world, "reading spends no resources and changes no market or world");
            Check(desk.ScoutingSummary(artistId).Any(l => l.Contains("public signing report") && l.Contains("not earlier inquiries")), "notebook scopes signing separately from private courtship");
            Check(desk.ScoutingProvenance(artistId).Any(l => l.Contains("week beginning") && l.Contains("exact signing day unrecorded")), "provenance does not fabricate occurrence day");
            string acquired = Json(desk.CaptureState().ScoutingKnowledge);
            desk.MarkSceneNewsRead();
            Check(Json(desk.CaptureState().ScoutingKnowledge) == acquired, "repeated read cannot buy more evidence");
            var reload = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
            Check(desk.RestoreState(reload, out _) && Json(desk.CaptureState().ScoutingKnowledge) == acquired, "public report byte roundtrip");
            act.sceneRecruitmentHistory.Clear();
            Check(desk.ScoutingCompetitionSummary(artistId).Contains(rival.labelName), "acquired snapshot survives removal of source record");
            act.labelId = rival.labelId;
            Check(desk.ScoutingOtherKnownLeads(artistId)?.StartsWith("Other leads") == true, "loss suggests existing leads or actual discovery routes");
            Check(desk.ScoutingDossiers.Single(d => d.ArtistId == artistId).Priority == ScoutingPriority.Archived, "loss preserves archived notes");
            // Pending market selection is not an introduction, meeting or source report.
            var privateProbe = desk.CaptureState();
            privateProbe.Notebook.Add(new ProspectNotebookSaveData { Artist = act, Year = clock.CurrentDate.year,
                Month = clock.CurrentDate.month, Day = clock.CurrentDate.day, CityId = desk.CurrentCityId });
            Check(desk.RestoreState(privateProbe, out _), "watch a canonical act for private nomination probe");
            string beforeNomination = Json(desk.CaptureState());
            typeof(PlayerDesk).GetMethod("OnRivalTalentMarketAppointment", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(desk,
                new object[] { new RosterManager.DailyTalentMarketAppointment { Label = rival, SelectedArtist = act, Date = clock.CurrentDate, Outcome = "Nominated" } });
            Check(Json(desk.CaptureState()) == beforeNomination, "internal nomination cannot manufacture public interest");
            act.sceneRecruitmentHistory = new() { new SceneRecruitmentRecord { ArtistId = artistId, LabelId = rival.labelId,
                Week = 5, Phase = "DailyMarket", BasePlaceId = "memphis" } };
            clock.RestoreClock(LocalSceneRoomService.Date(42), 10);
            Check(!desk.LocalSceneNews().Any(n => n.ArtistId == artistId), "another city's report is not a national feed");
            row.Week = 6; act.sceneRecruitmentHistory = new() { row };
            clock.RestoreClock(LocalSceneRoomService.Date(49), 10);
            foreach (string flag in new[] { "--observe-scouting-experience", "--disable-scouting-experience" }) {
                row.Week++; clock.RestoreClock(LocalSceneRoomService.Date((row.Week + 1) * 7), 10);
                LocalScenes.Configure(new[] { flag });
                desk.MarkSceneNewsRead();
                Check(Json(desk.CaptureState().ScoutingKnowledge) == acquired, "off/observer modes retain notes without acquiring reports: " + flag);
            }
            LocalScenes.Configure(Array.Empty<string>());
        } finally {
            LocalScenes.Configure(Array.Empty<string>());
            act.sceneRecruitmentHistory = history; act.labelId = owner;
            desk.RestoreState(saved, out _); clock.RestoreClock(date, hour, minute);
        }
        return checks;
    }
}
