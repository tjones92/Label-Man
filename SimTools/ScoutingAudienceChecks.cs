using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingAudienceChecks {
    // Controlled retained observations only; no extra bills, fans, fees or world performances.
    public static ScoutingDossier ContextFixture(ScoutingObservation heard) {
        var first = LocalSceneRoomService.Copy(heard);
        first.BillAttendance = 149; first.BillCapacity = 200; first.BillListedActs = 3; first.BillAdmissionQuote = 2;
        var report = LocalSceneRoomService.Copy(first);
        report.Id = "audience-source-one"; report.EventId = report.Id; report.Kind = ScoutingEvidenceKind.BookerClaim;
        report.SourceId = "controlled-booker"; report.SourceName = "Fixture booker"; report.BillAttendance = 150;
        report.BillAdmissionQuote = null; report.Claim = "Controlled whole-bill record"; report.AcquiredDay += 1;
        report.Credibility = "controlled local source record; limited to this room";
        var other = LocalSceneRoomService.Copy(report);
        other.Id = "audience-source-two"; other.EventId = other.Id; other.BillId = "controlled-other-bill";
        other.RoomId = "controlled-other-room"; other.RoomName = "Comparison Hall"; other.PlaceId = "controlled-other-town";
        other.PlaceName = "Comparison town";
        other.OccurredDay += 2; other.AcquiredDay += 2; other.BillAttendance = 20; other.BillCapacity = 500; other.BillListedActs = 1;
        return new() { ArtistId = heard.ArtistId, ArtistName = heard.ArtistName, Observations = new() { first, report, other } };
    }
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_AUDIENCE_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
        Check(desk.FoundLabel("Audience Evidence", "nashville", out string message), "found: " + message);
        var plan = desk.ScoutingItinerary().First();
        var bill = LocalSceneRoomService.Bill(plan.BillId); var room = SceneRoomCatalog.Get(plan.RoomId);
        int listed = bill.Appearances.Select(a => a.ArtistId).Distinct(StringComparer.Ordinal).Count();
        float quote = SceneLiveEconomics.Admission(room, bill.Year);
        Check(desk.WaitForSceneAppearance(plan.BillId, plan.EventId, out message) && desk.ListenSceneAppearance(plan.BillId, plan.EventId, out message), "actual scheduled set: " + message);
        var dossier = desk.ScoutingDossiers.Single(d => d.ArtistId == plan.ArtistId);
        var heard = dossier.Observations.Single(o => o.Kind == ScoutingEvidenceKind.Performance);
        Check(heard.BillListedActs == listed && heard.BillAdmissionQuote == quote && heard.PlaceId == room.PlaceId && heard.PlaceName == ScenePlaceRegistry.Get(room.PlaceId)?.Name, "real listening retains posted context");
        clock.EndDay(); clock.RestoreClock(clock.CurrentDate, room.OpenHour);
        var answer = desk.PreviewScoutingSource(room.Id, plan.ArtistId, ScoutingSourceQuestion.BillAudience);
        Check(answer.Available && answer.BillId == bill.Id && answer.BillListedActs == listed, "actual completed-bill answer");
        int minute = clock.CurrentHour * 60 + clock.CurrentMinute;
        Check(desk.AskScoutingSource(room.Id, plan.ArtistId, ScoutingSourceQuestion.BillAudience, answer.QuoteId, out message), "real paid-time source answer: " + message);
        dossier = desk.ScoutingDossiers.Single(d => d.ArtistId == plan.ArtistId);
        var sourced = dossier.Observations.Last();
        Check(dossier.Observations.Single(o => o.Kind == ScoutingEvidenceKind.Performance).BillAttendance == heard.BillAttendance, "ordinary bill completion cannot retroactively fill listening notes");
        Check(sourced.BillListedActs == listed && sourced.BillAdmissionQuote == null && sourced.PlaceId == room.PlaceId && sourced.AcquiredMinute == minute + 60, "source freezes listed bill, occurrence and real receipt, without inventing historical admission");
        Check(ScoutingAudience.Summary(dossier).Contains("1 distinct whole-bill") && !ScoutingAudience.Summary(dossier).Contains("Reports disagree"), "listening and booker are one crowd, not independent bills");
        Check(!desk.AskScoutingSource(room.Id, plan.ArtistId, ScoutingSourceQuestion.BillAudience, answer.QuoteId, out _) && clock.CurrentHour * 60 + clock.CurrentMinute == minute + 60, "duplicate source acquisition has no time or crowd progress");
        var fixture = ContextFixture(heard);
        string Detail(ScoutingDossier d) => string.Join("\n", ScoutingAudience.Details(d));
        Check(ScoutingAudience.Summary(fixture).Contains("2 distinct whole-bill") && ScoutingAudience.Summary(fixture).Contains("2 recorded room") && ScoutingAudience.Summary(fixture).Contains("2 recorded town"), "different retained bills, rooms and towns counted independently");
        Check(Detail(fixture).Contains("150 attendees against 200 capacity") && !Detail(fixture).Contains("149 attendees") && Detail(fixture).Contains("roughly three-quarters full"), "visual observation stays coarse while source count retains denominator");
        Check(Detail(fixture).Contains("3 listed act") && Detail(fixture).Contains("$2.00 posted admission") && Detail(fixture).Contains("Received") && Detail(fixture).Contains("Fixture booker"), "source, receipt and room/bill context are explicit");
        Check(!ScoutingAudience.Summary(fixture).Contains("Reports disagree"), "exact source count within the visual band is not a contradiction");
        var sameBill = LocalSceneRoomService.Copy(fixture); var secondSet = LocalSceneRoomService.Copy(sameBill.Observations[0]);
        secondSet.Id = "second-set"; secondSet.EventId = "second-appearance"; sameBill.Observations.Add(secondSet);
        Check(ScoutingAudience.Summary(sameBill) == ScoutingAudience.Summary(fixture), "two actual appearance identities on one bill remain one crowd");
        var sameDate = LocalSceneRoomService.Copy(fixture); sameDate.Observations[2].OccurredDay = sameDate.Observations[0].OccurredDay;
        Check(ScoutingAudience.Summary(sameDate).Contains("2 distinct whole-bill"), "two bill identities on one date remain distinct crowds");
        var changed = LocalSceneRoomService.Copy(fixture); changed.Observations[1].BillAttendance = 190;
        Check(ScoutingAudience.Summary(changed).Contains("Reports disagree") && Detail(changed).Contains("conflicting reports"), "visibly incompatible reports remain disputed");
        changed = LocalSceneRoomService.Copy(fixture); var contrary = LocalSceneRoomService.Copy(changed.Observations[1]);
        contrary.Id = "other-source"; contrary.EventId = "other-source-event"; contrary.SourceId = "other-source"; contrary.BillAttendance = 151; changed.Observations.Add(contrary);
        Check(ScoutingAudience.Summary(changed).Contains("Reports disagree") && Detail(changed).Contains("151 attendees"), "different exact source counts remain separate, without silent averaging");
        changed = LocalSceneRoomService.Copy(fixture); var copy = LocalSceneRoomService.Copy(changed.Observations[1]); copy.Id = "copied-note"; changed.Observations.Add(copy);
        Check(ScoutingAudience.Summary(changed) == ScoutingAudience.Summary(fixture) && Detail(changed) == Detail(fixture), "copied event and source deduplicate");
        var opinion = LocalSceneRoomService.Copy(heard); opinion.Kind = ScoutingEvidenceKind.ListenerOpinion; opinion.BillId = "not-another-crowd"; changed.Observations.Add(opinion);
        var demo = LocalSceneRoomService.Copy(opinion); demo.Kind = ScoutingEvidenceKind.DemoSample; changed.Observations.Add(demo);
        Check(Detail(changed) == Detail(fixture), "listener judgments and studio samples do not create audience evidence");
        changed = LocalSceneRoomService.Copy(fixture); foreach (var o in changed.Observations) { o.BillListedActs = null; o.BillAdmissionQuote = null; o.PlaceId = null; o.PlaceName = null; }
        Check(Detail(changed).Contains("bill size not recorded") && Detail(changed).Contains("admission not recorded") && ScoutingAudience.Summary(changed).Contains("0 recorded town"), "legacy fields remain unknown rather than reconstructed from today's room");
        Check(Detail(changed).Contains("town name not recorded"), "missing historical town name stays unknown");
        changed = LocalSceneRoomService.Copy(fixture); foreach (var o in changed.Observations) o.BillId = null;
        Check(ScoutingAudience.Summary(changed).Contains("independent crowds cannot be counted") && Detail(changed).Contains("bill identity unknown"), "missing identity cannot establish independent crowds");
        changed = LocalSceneRoomService.Copy(fixture); foreach (var o in changed.Observations) o.BillCapacity = 0;
        Check(ScoutingAudience.Summary(changed).Contains("no usable"), "missing denominator cannot support occupancy");
        changed = LocalSceneRoomService.Copy(fixture); foreach (var o in changed.Observations) o.BillAttendance = o.BillCapacity + 1;
        Check(ScoutingAudience.Summary(changed).Contains("no usable"), "malformed attendance cannot exceed capacity");
        changed = LocalSceneRoomService.Copy(fixture); foreach (var o in changed.Observations) o.BillAttendance = -1;
        Check(ScoutingAudience.Summary(changed).Contains("no usable"), "negative attendance cannot supply evidence");
        changed = LocalSceneRoomService.Copy(fixture); foreach (var o in changed.Observations) o.BillAttendance = 0;
        Check(Detail(changed).Contains("0 attendees") && Detail(changed).Contains("sparsely occupied") && !Detail(changed).Contains("weak music"), "quiet room is real audience evidence, not a musical verdict");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations[0].BillAttendance = null; changed.Observations.RemoveRange(1, 2);
        Check(Detail(changed).Contains("Later calendar results do not fill old notes automatically"), "unfinished listening cannot borrow later final attendance");
        var player = desk.CaptureState(); player.ScoutingKnowledge.Dossiers = new() { fixture };
        Check(desk.RestoreState(player, out message), "restore controlled retained context");
        string world = Json(WorldStateService.Capture()), knowledge = Json(desk.CaptureState());
        GD.Seed(872); uint draw = GD.Randi(); GD.Seed(872);
        var detail = desk.ScoutingAudienceDetails(plan.ArtistId).ToArray(); var summary = desk.ScoutingSummary(plan.ArtistId);
        Check(summary.Count(l => l.StartsWith("Audience:")) == 1 && summary.Single(l => l.StartsWith("Audience:")).Contains("2 distinct whole-bill"), "notebook uses retained history once");
        Check(world == Json(WorldStateService.Capture()) && knowledge == Json(desk.CaptureState()) && GD.Randi() == draw, "audience views preserve full world, player and RNG");
        var captured = desk.CaptureState(); captured.ScoutingKnowledge.Dossiers.Single().Observations[0].BillAttendance = 1;
        Check(desk.ScoutingAudienceDetails(plan.ArtistId).SequenceEqual(detail), "capture is detached");
        var rooms = LocalSceneRoomService.Copy(WorldStateService.Capture().SceneRooms); rooms.Bills.RemoveAll(b => b.Id == bill.Id);
        LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore();
        Check(desk.ScoutingAudienceDetails(plan.ArtistId).SequenceEqual(detail), "calendar pruning cannot rewrite retained audience evidence");
        Check(desk.SetScoutingPriority(plan.ArtistId, ScoutingPriority.Archived) && desk.ScoutingAudienceDetails(plan.ArtistId).SequenceEqual(detail), "archive retains history");
        foreach (var flag in new[] { "--disable-scouting-experience", "--observe-scouting-experience" }) {
            LocalScenes.Configure(new[] { flag }); Check(desk.ScoutingAudienceDetails(plan.ArtistId).SequenceEqual(detail), "off mode retains acquired audience: " + flag);
        }
        LocalScenes.Configure(Array.Empty<string>());
        string slot = "Audience " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message), "compressed save/load: " + message);
            Check(desk.ScoutingAudienceDetails(plan.ArtistId).SequenceEqual(detail), "full load reproduces source/denominator/context exactly");
        } finally { SaveGameService.Delete(slot); }
        GD.Print($"SCOUTING_AUDIENCE_PASS checks={checks}");
    }
}
