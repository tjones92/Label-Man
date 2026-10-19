using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingComparisonChecks {
    // Controlled acquired evidence for comparison and rendered notebook fixtures.
    // This does not create performances, bookings, work, or another act.
    public static ScoutingDossier ContextFixture(ScoutingObservation source) {
        ScoutingObservation Sample(string id, int day, int minute, float execution) {
            var copy = LocalSceneRoomService.Copy(source);
            copy.Id = id; copy.EventId = id; copy.OccurredDay = day; copy.AcquiredDay = day;
            copy.StartMinute = minute; copy.EndMinute = minute + 60; copy.Coverage = 1;
            // Some launch acts have no announced repertoire. Supply explicitly controlled
            // fixture context so these probes test comparisons rather than authored content.
            if (copy.Set.Count == 0) copy.Set.Add(new SceneSetSong { Title = "Controlled comparison material", Original = true });
            if (copy.PersonIds.Count == 0) copy.PersonIds = ArtistManager.Instance.GetArtist(source.ArtistId).members
                .Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).ToList();
            copy.Interpretation ??= new(); copy.Interpretation.Execution = execution;
            copy.Interpretation.Vocals = .6f; copy.Interpretation.Material = .6f; copy.Interpretation.Communication = .6f;
            return copy;
        }
        int day = source.OccurredDay.Value;
        return new() { ArtistId = source.ArtistId, ArtistName = source.ArtistName, Question = ScoutingQuestion.Ensemble,
            Observations = new() { Sample("comparison-old-date", day, 600, .8f),
                Sample("comparison-rough-set", day + 1, 600, .1f), Sample("comparison-later-set", day + 1, 720, .8f),
                Sample("comparison-new-date", day + 2, 600, .8f) } };
    }

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_COMPARISON_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
        Check(desk.FoundLabel("Comparison Checks", "nashville", out string message), "label: " + message);
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate)
            .First(b => b.Status == SceneBillStatus.Scheduled && b.Appearances.Count > 0 && b.Appearances[0].EndHour <= clock.HardStopHour);
        var slot = bill.Appearances[0];
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        Check(desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message), "real initial listening: " + message);
        var source = desk.ScoutingDossiers.Single().Observations.Single();
        var fixture = ContextFixture(source);
        string Read(ScoutingDossier d) => PlayerDesk.ScoutingQuestionRead(d);
        var latest = fixture.Observations.Last(); var earlier = fixture.Observations[^2];
        Check(Read(fixture).Contains("broadly similar impressions") && !Read(fixture).Contains("less assured"), "same-night spread is not across-date consistency");
        Check(Read(fixture).Contains("similar recorded people, material and room") && Read(fixture).Contains("Two dated sets"), "comparable complete contexts support dated comparison: " + Read(fixture));
        Check(Read(fixture).Contains(LocalSceneRoomService.Date(earlier.OccurredDay.Value).ToHeadlineString()) &&
            Read(fixture).Contains(LocalSceneRoomService.Date(latest.OccurredDay.Value).ToHeadlineString()), "comparison states both historical dates");
        var changed = LocalSceneRoomService.Copy(fixture);
        changed.Observations[^2].PersonIds.Add("different-person");
        Check(Read(changed).Contains(LocalSceneRoomService.Date(source.OccurredDay.Value).ToHeadlineString()) &&
            !Read(changed).Contains("people differed"), "prefer retained comparable context over more recent different people");
        changed.Observations.RemoveAt(0);
        changed.Observations[0].PersonIds.Add("different-person");
        Check(Read(changed).Contains("performing or backing people differed") && Read(changed).Contains("cannot establish consistency"), "different lineup is descriptive, not consistent conditions");
        changed = LocalSceneRoomService.Copy(fixture);
        foreach (var o in changed.Observations.Take(3)) o.BackingPersonIds.Add("different-backing");
        Check(Read(changed).Contains("performing or backing people differed"), "backing musicians are part of performance context");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations.Last().Set[0].Title = "Changed material";
        changed.Observations.Last().Set[0].SongId = "different-song";
        Check(Read(changed).Contains("material or its order differed") && Read(changed).Contains("cannot establish consistency"), "different material has explicit limits");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations.Last().RoomId = "another-room";
        Check(Read(changed).Contains("room differed"), "room context retained");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations.Last().Set.Clear();
        Check(Read(changed).Contains("material unknown") && Read(changed).Contains("cannot establish consistency"), "missing material cannot be claimed comparable");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations.Last().PersonIds.Clear();
        Check(Read(changed).Contains("people unknown"), "missing people are not matching lineups");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations.Last().Coverage = .5f;
        Check(Read(changed).Contains("coverage was partial") && !Read(changed).Contains("similar impressions"), "partial latest set cannot borrow a complete same-night set");
        changed = LocalSceneRoomService.Copy(fixture); foreach (var o in changed.Observations.Take(3)) o.Coverage = .5f;
        Check(Read(changed).Contains("earlier dates"), "partial earlier dates leave comparison open");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations[^2].Coverage = .5f;
        Check(Read(changed).Contains(LocalSceneRoomService.Date(source.OccurredDay.Value).ToHeadlineString()), "choose a complete earlier comparable date when available");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations.Last().Interpretation = null;
        Check(Read(changed).Contains("interpretation is missing"), "missing latest interpretation does not silently reuse older evidence");
        changed.Question = ScoutingQuestion.Vocals;
        Check(Read(changed).Contains("no comparable vocal evidence"), "no vocals does not penalize instrumental acts");
        changed = LocalSceneRoomService.Copy(fixture); changed.Question = ScoutingQuestion.Material;
        changed.Observations.Last().Interpretation.Material = .2f;
        Check(Read(changed).Contains("less assured"), "material question uses its own acquired axis");
        changed.Question = ScoutingQuestion.Stage; changed.Observations.Last().Interpretation.Communication = .9f;
        Check(Read(changed).Contains("stronger impression"), "stage question uses communication");
        changed = LocalSceneRoomService.Copy(fixture); changed.Observations.RemoveRange(0, 3);
        var duplicate = LocalSceneRoomService.Copy(changed.Observations.Single()); duplicate.OccurredDay += 1;
        changed.Observations.Add(duplicate);
        Check(Read(changed).Contains("another date is needed"), "duplicate event identity is not independent evidence even with changed date");
        changed.Observations.Add(new() { Kind = ScoutingEvidenceKind.BookerClaim, OccurredDay = duplicate.OccurredDay + 1 });
        changed.Observations.Add(new() { Kind = ScoutingEvidenceKind.ListenerOpinion, OccurredDay = duplicate.OccurredDay + 1, Interpretation = new() });
        Check(Read(changed).Contains("another date is needed"), "source claims and other listeners never supply another performed date");
        changed.Question = ScoutingQuestion.None;
        Check(Read(changed) == null, "unpinned question returns no comparison");

        var save = desk.CaptureState(); save.ScoutingKnowledge.Dossiers = new() { fixture };
        Check(desk.RestoreState(save, out message), "restore controlled acquired context fixture");
        string world = Json(WorldStateService.Capture()), knowledge = Json(desk.CaptureState().ScoutingKnowledge);
        int hour = clock.CurrentHour, minute = clock.CurrentMinute; float cash = desk.Label.cashReserves;
        GameRandom.Seed(155); uint next = GameRandom.Randi(); GameRandom.Seed(155);
        var summary = desk.ScoutingSummary(source.ArtistId).ToArray(); desk.ScoutingProvenance(source.ArtistId);
        Check(summary.Single(l => l.StartsWith("Consistency:")).Contains("broadly similar impressions"), "ordinary notebook uses same pair, not all-set range");
        Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState().ScoutingKnowledge) == knowledge &&
            GameRandom.Randi() == next && hour == clock.CurrentHour && minute == clock.CurrentMinute && cash == desk.Label.cashReserves,
            "comparison, summary and provenance preserve world, knowledge, RNG, time and money");
        var serialized = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
        Check(desk.RestoreState(serialized, out _) && desk.ScoutingSummary(source.ArtistId).SequenceEqual(summary), "serialized dossier reproduces exact comparison");
        string slotName = "Compare probe " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slotName, out message), "compressed save: " + message);
            Check(SaveGameService.Load(slotName, out message) && desk.ScoutingSummary(source.ArtistId).SequenceEqual(summary), "real compressed load reproduces comparison: " + message);
        } finally { SaveGameService.Delete(slotName); }
        LocalScenes.Configure(new[] { "--disable-scouting-experience" });
        Check(desk.ScoutingSummary(source.ArtistId).Single(l => l.StartsWith("Consistency:")) == summary.Single(l => l.StartsWith("Consistency:")), "off retains historical comparison");
        LocalScenes.Configure(new[] { "--observe-scouting-experience" });
        Check(desk.ScoutingSummary(source.ArtistId).Single(l => l.StartsWith("Consistency:")) == summary.Single(l => l.StartsWith("Consistency:")), "observer retains historical comparison");
        LocalScenes.Configure(Array.Empty<string>());
        var act = ArtistManager.Instance.GetArtist(source.ArtistId); var person = act.members.First(m => m.isActive && m.lifeState == MemberLifeState.Active);
        bool wasActive = person.isActive; person.isActive = false;
        Check(desk.ScoutingSummary(source.ArtistId).Any(l => l.Contains("Lineup changed")) &&
            desk.ScoutingSummary(source.ArtistId).Any(l => l.Contains("historical listening judgments")), "current lineup change marks historical judgments stale");
        person.isActive = wasActive;
        var absent = desk.CaptureState(); absent.ScoutingKnowledge.Dossiers.Single().Observations.Last().Interpretation = null;
        Check(desk.RestoreState(absent, out _) && desk.ScoutingSummary(source.ArtistId).Any(l => l.Contains("interpretation is missing")), "notebook tolerates missing retained interpretation");
        GD.Print($"SCOUTING_COMPARISON_CHECK_PASS checks={checks}");
    }
}
