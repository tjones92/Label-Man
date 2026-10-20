using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Release Judgement Phase 1 gate (directive "Meaningful invariant checks" 1, 2, 8 as they apply to an observe-only pool).
/// Drives the shadow's weekly step on the real generated world without simulating the economy.
///   --path . SimTools/ReleaseJudgementChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints RELEASE_JUDGEMENT_CHECKS_PASS / _FAIL.
/// </summary>
public partial class ReleaseJudgementChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static readonly JsonSerializerOptions Json = SaveGameService.TestJsonOptions;
	private static readonly GameDate Start = new(1960, 1, 8);

	public override void _Ready() {
		try {
			ObserveOnlyAndBounded();
			StableReads();
			DomainSeparation();
			SaveContinuation();
			Selection();
			Disabled();
			GD.Print($"RELEASE_JUDGEMENT_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("RELEASE_JUDGEMENT_CHECKS_FAIL: " + e);
			GetTree().Quit(1);
		} finally {
			ReleaseJudgement.SetForProbe(false);
			AiRepertoireService.Reset();
		}
	}

	private static void Run(int fromWeek, int weeks) {
		for (int w = fromWeek; w < fromWeek + weeks; w++) AiRepertoireService.Step(Start.AddDays(7 * (w - 1)), w);
	}

	private static string StateJson() {
		var world = new WorldSaveData();
		AiRepertoireService.CaptureWorld(world);
		return JsonSerializer.Serialize(world.AiRepertoire, Json);
	}

	private static IEnumerable<AILabel> Labels() => ChartManager.Instance.GetAllLabels()
		.Where(l => l != null && !l.isPlayerOwned && l.roster != null && l.status != LabelStatus.Defunct && l.status != LabelStatus.Bankrupt);

	// Invariant 1: the shadow writes nothing the economy reads and keeps its shortlist bounded.
	private void ObserveOnlyAndBounded() {
		ReleaseJudgement.SetForProbe(true);
		AiRepertoireService.Reset();
		ulong rng = GameRandom.StateForChecks;
		int songs = CompositionCatalogService.SongCount;
		int records = ChartManager.Instance.GetAllRecords().Count;
		double cash = Labels().Sum(l => (double)l.cashReserves);
		int releases = Labels().SelectMany(l => l.roster).Sum(a => a.totalReleases);
		Run(1, 60);
		Check(GameRandom.StateForChecks == rng, "60 shadow weeks draw nothing from the global RNG stream");
		Check(CompositionCatalogService.SongCount == songs, "the shadow registers no catalogue songs");
		Check(ChartManager.Instance.GetAllRecords().Count == records, "the shadow creates no records");
		Check(Labels().Sum(l => (double)l.cashReserves) == cash && Labels().SelectMany(l => l.roster).Sum(a => a.totalReleases) == releases,
			"label cash and act release counters are untouched");
		var acts = AiRepertoireService.Acts.Values.ToList();
		var health = AiRepertoireService.LastHealth;
		Check(acts.Count > 100 && health.Recordable > acts.Count && AiRepertoireService.Counters.Keys.Count(k => k.StartsWith("arrival:")) >= 3,
			$"the pool fills from several sources ({acts.Count} acts, {health.Recordable} recordable, {health.Offered} offered)");
		Check(acts.All(a => a.Candidates.Count(c => c.Disposition == ShadowDisposition.Active) <= ReleaseJudgement.Parameters.ShortlistCap),
			$"no act holds more than {ReleaseJudgement.Parameters.ShortlistCap} active candidates");
		Check(acts.All(a => a.Candidates.Where(c => c.Disposition == ShadowDisposition.Active && c.SongId != null)
				.GroupBy(c => c.SongId).All(g => g.Count() == 1)), "an act never holds the same catalogue song twice");
		var ids = acts.SelectMany(a => a.Candidates).Select(c => c.Id).ToList();
		Check(ids.Distinct(StringComparer.Ordinal).Count() == ids.Count, "candidate ids are unique");
		Check(acts.SelectMany(a => a.Candidates).Where(c => c.SongId != null).All(c => CompositionCatalogService.GetSong(c.SongId) != null),
			"every catalogue reference resolves to an existing composition");
		Check(acts.SelectMany(a => a.Candidates).All(c => c.Disposition == ShadowDisposition.Active || 60 - c.DispositionWeek <= ReleaseJudgement.Parameters.ArchiveRetentionWeeks),
			"archived entries are pruned after the retention window");
	}

	// Invariant 1-2: reading twice gives the same judgement, changes no state, and perception stays inside its band.
	private void StableReads() {
		var label = Labels().OrderBy(l => l.labelId, StringComparer.Ordinal)
			.First(l => l.roster.Any(a => AiRepertoireService.Acts.TryGetValue(a.artistId, out var act) && act.Candidates.Count(c => c.Readiness == ShadowReadiness.Recordable && c.Disposition == ShadowDisposition.Active) >= 2));
		var artist = label.roster.First(a => AiRepertoireService.Acts.TryGetValue(a.artistId, out var act) && act.Candidates.Count(c => c.Readiness == ShadowReadiness.Recordable && c.Disposition == ShadowDisposition.Active) >= 2);
		string before = StateJson();
		ulong rng = GameRandom.StateForChecks;
		var first = AiRepertoireService.Judge(label, artist, 1961, 60);
		var second = AiRepertoireService.Judge(label, artist, 1961, 60);
		Check(first.BestId == second.BestId && first.BestPerceived == second.BestPerceived && first.Recordable == second.Recordable,
			$"rereading {artist.stageName}'s shortlist returns the same judgement ({first.BestId} at {first.BestPerceived:F3})");
		Check(StateJson() == before && GameRandom.StateForChecks == rng, "a judgement changes no shadow state and draws nothing");
		var act = AiRepertoireService.Acts[artist.artistId];
		var reversed = act.Candidates.AsEnumerable().Reverse().ToList();
		var original = act.Candidates.ToList();
		act.Candidates.Clear(); act.Candidates.AddRange(reversed);
		var reordered = AiRepertoireService.Judge(label, artist, 1961, 60);
		act.Candidates.Clear(); act.Candidates.AddRange(original);
		Check(reordered.BestId == first.BestId, "a different enumeration order picks the same song");
		bool inBand = AiRepertoireService.Acts.Values.Take(400).All(a => {
			var owner = ChartManager.Instance.GetLabelById(a.LabelId);
			var who = ArtistManager.Instance.GetArtist(a.ArtistId);
			return owner == null || who == null || a.Candidates.All(c => {
				float read = AiRepertoireService.Read(c, owner, who, out float band);
				float truth = AiRepertoireService.ExpectedRecording(c, who, owner, out _, out _, out _);
				return Math.Abs(read - truth) <= band + 1e-5f || read is 0f or 1f;
			});
		});
		Check(inBand, "every label read lies within its scouting band of the true expectation");
	}

	// The keyed streams: removing one act from a roster must not reroll anyone else's material.
	private void DomainSeparation() {
		AiRepertoireService.Reset();
		Run(1, 20);
		var label = Labels().OrderBy(l => l.labelId, StringComparer.Ordinal).First(l => l.roster.Count >= 3);
		var removed = label.roster.OrderBy(a => a.artistId, StringComparer.Ordinal).First();
		var kept = label.roster.OrderBy(a => a.artistId, StringComparer.Ordinal).Skip(1).ToList();
		var other = Labels().OrderBy(l => l.labelId, StringComparer.Ordinal).Last(l => l != label && l.roster.Count > 0).roster
			.OrderBy(a => a.artistId, StringComparer.Ordinal).First();
		string Of(string id) => JsonSerializer.Serialize(AiRepertoireService.Acts[id], Json);
		string keptBefore = Of(kept[0].artistId), otherBefore = Of(other.artistId);
		int index = label.roster.IndexOf(removed);
		label.roster.RemoveAt(index);
		try {
			AiRepertoireService.Reset();
			Run(1, 20);
			Check(!AiRepertoireService.Acts.ContainsKey(removed.artistId), "an act off every roster gets no shortlist");
			Check(Of(kept[0].artistId) == keptBefore, "a labelmate's shortlist is identical without the removed act");
			Check(Of(other.artistId) == otherBefore, "another label's act is identical without the removed act");
		} finally {
			label.roster.Insert(index, removed);
		}
	}

	// Invariant 8: a saved shadow restores exactly and continues identically, also against the never-saved run.
	private void SaveContinuation() {
		AiRepertoireService.Reset();
		Run(1, 30);
		var world = new WorldSaveData();
		AiRepertoireService.CaptureWorld(world);
		string saved = JsonSerializer.Serialize(world, Json);
		Check(SaveGameService.WritingVersion == (CareerBandedFormatMemory.HasState ? 24 : 23),
			"a world carrying the shadow writes its required save version, including banded memory when active");
		Run(31, 15);
		string uninterrupted = StateJson();
		string Resume() {
			AiRepertoireService.RehydrateWorld(JsonSerializer.Deserialize<WorldSaveData>(saved, Json));
			Run(31, 15);
			return StateJson();
		}
		string a = Resume(), b = Resume();
		Check(a == b, "two loads of the same save continue identically");
		Check(a == uninterrupted, $"a reload continues exactly as the never-saved shadow ({a.Length / 1024} KB of state)");
		AiRepertoireService.RehydrateWorld(JsonSerializer.Deserialize<WorldSaveData>(saved, Json));
		Check(StateJson() == JsonSerializer.Serialize(JsonSerializer.Deserialize<WorldSaveData>(saved, Json).AiRepertoire, Json),
			"the restored shortlists equal the saved ones field for field");
	}

	private static Record Scratch(AILabel label, SimulatedArtist artist, string id) => new() {
		recordId = id, labelId = label.labelId, artistId = artist.artistId, artistName = artist.stageName, format = ReleaseFormat.Single,
		primaryGenre = artist.primaryGenre, secondaryGenre = artist.secondaryGenre, title = "Check " + id,
		hookStrength = 0.6f, productionQuality = 0.6f, danceability = 0.6f, originality = 0.5f
	};

	private static ShadowCandidate Top(ShadowActRepertoire act, AILabel label, SimulatedArtist artist, ReleaseSelectMode mode) =>
		act.Candidates.Where(c => c.Disposition == ShadowDisposition.Active && c.Readiness == ShadowReadiness.Recordable)
			.OrderByDescending(c => AiRepertoireService.SelectionScore(c, label, artist, mode)).ThenBy(c => c.Id, StringComparer.Ordinal).First();

	// Phase 2A: the record is cut from the ranked shortlist song, an original is authored once, consumption follows the
	// actual recording, and an empty shortlist falls back without consuming anything.
	private void Selection() {
		ReleaseJudgement.SetForProbe(true, select: ReleaseSelectMode.Perceived);
		AiRepertoireService.Reset();
		Run(1, 30);
		bool Fits(ShadowActRepertoire a) {
			var ready = a.Candidates.Where(c => c.Disposition == ShadowDisposition.Active && c.Readiness == ShadowReadiness.Recordable).ToList();
			return ready.Count >= 3 && ready.Any(c => c.Source == SongMaterialSource.ArtistWritten && c.SongId == null);
		}
		var label = Labels().OrderBy(l => l.labelId, StringComparer.Ordinal)
			.First(l => l.roster.Any(a => AiRepertoireService.Acts.TryGetValue(a.artistId, out var act) && Fits(act)));
		var artist = label.roster.OrderBy(a => a.artistId, StringComparer.Ordinal)
			.First(a => AiRepertoireService.Acts.TryGetValue(a.artistId, out var act) && Fits(act));
		var act = AiRepertoireService.Acts[artist.artistId];
		// Make the unauthored original the top pick by consuming anything ranked above it, so authoring is exercised.
		while (Top(act, label, artist, ReleaseSelectMode.Perceived) is { } top && !(top.Source == SongMaterialSource.ArtistWritten && top.SongId == null)) {
			if (act.Candidates.Count(c => c.Disposition == ShadowDisposition.Active && c.Readiness == ShadowReadiness.Recordable) <= 1) break;
			top.Disposition = ShadowDisposition.Archived; top.DispositionWeek = 30; top.DispositionReason = "check";
		}
		ShadowCandidate expected = Top(act, label, artist, ReleaseSelectMode.Perceived);
		ulong rng = GameRandom.StateForChecks;
		int songs = CompositionCatalogService.SongCount;
		var r1 = Scratch(label, artist, "rjcheck_1");
		var m1 = AiRepertoireService.SelectMaterial(label, artist, r1, r1.primaryGenre, 1960);
		Check(m1 != null && AiRepertoireService.PendingForChecks(r1.recordId)?.CandidateId == expected.Id,
			$"the perceived ranking cuts the record from the top shortlist song ({expected.Id}, {expected.Source})");
		Check(GameRandom.StateForChecks == rng, "selection and realization draw nothing from the global stream");
		bool original = expected.Source == SongMaterialSource.ArtistWritten;
		if (original) Check(expected.SongId != null && CompositionCatalogService.SongCount == songs + 1 && m1.Song.songId == expected.SongId,
			"an original is authored as one composition on its first recording");
		var r2 = Scratch(label, artist, "rjcheck_2");
		var m2 = AiRepertoireService.SelectMaterial(label, artist, r2, r2.primaryGenre, 1960);
		Check(m2?.Song?.songId == m1.Song.songId && CompositionCatalogService.SongCount == songs + (original ? 1 : 0),
			"a second take of the same unconsumed song reuses its composition and authors nothing new");
		SongMaterialApplicationService.Apply(r1, m1, label, artist);
		Check(r1.songId == m1.Song.songId, "the realized record carries the selected composition");
		ShadowJudgement seen = null;
		void Capture(ShadowJudgement j) => seen = j;
		AiRepertoireService.OnJudgement += Capture;
		try { AiRepertoireService.ObserveSingleRelease(label, artist, r1, ReleaseStrategy.OrphanSingle, Start); }
		finally { AiRepertoireService.OnJudgement -= Capture; }
		Check(seen?.Outcome == "pool" && seen.SelectedId == expected.Id && expected.Disposition == ShadowDisposition.ShadowIssued,
			"the release consumes exactly the song it was cut from");
		var r3 = Scratch(label, artist, "rjcheck_3");
		var m3 = AiRepertoireService.SelectMaterial(label, artist, r3, r3.primaryGenre, 1960);
		Check(m3 == null || AiRepertoireService.PendingForChecks(r3.recordId)?.CandidateId != expected.Id, "a consumed song is not cut again");

		ReleaseJudgement.SetForProbe(true, select: ReleaseSelectMode.Fit);
		var fitLabel = Labels().OrderBy(l => l.labelId, StringComparer.Ordinal)
			.First(l => l.roster.Any(a => a != artist && AiRepertoireService.Acts.TryGetValue(a.artistId, out var x) && Fits(x)));
		var fitArtist = fitLabel.roster.OrderBy(a => a.artistId, StringComparer.Ordinal)
			.First(a => a != artist && AiRepertoireService.Acts.TryGetValue(a.artistId, out var x) && Fits(x));
		ShadowCandidate byFit = Top(AiRepertoireService.Acts[fitArtist.artistId], fitLabel, fitArtist, ReleaseSelectMode.Fit);
		var r4 = Scratch(fitLabel, fitArtist, "rjcheck_4");
		AiRepertoireService.SelectMaterial(fitLabel, fitArtist, r4, r4.primaryGenre, 1960);
		Check(AiRepertoireService.PendingForChecks(r4.recordId)?.CandidateId == byFit.Id, "the fit-only arm cuts the song with the best exact fit score");

		foreach (var c in act.Candidates.Where(c => c.Disposition == ShadowDisposition.Active)) { c.Disposition = ShadowDisposition.Archived; c.DispositionWeek = 30; }
		int issued = act.Candidates.Count(c => c.Disposition == ShadowDisposition.ShadowIssued);
		var r5 = Scratch(label, artist, "rjcheck_5");
		var m5 = AiRepertoireService.SelectMaterial(label, artist, r5, r5.primaryGenre, 1960);
		AiRepertoireService.ObserveSingleRelease(label, artist, r5, ReleaseStrategy.OrphanSingle, Start);
		Check(m5 == null && act.Candidates.Count(c => c.Disposition == ShadowDisposition.ShadowIssued) == issued,
			"an empty shortlist falls back to the legacy choice and consumes nothing");
		ReleaseJudgement.SetForProbe(true);
	}

	// Off is absent: no steps, no save block, no version bump.
	private void Disabled() {
		ReleaseJudgement.SetForProbe(false);
		AiRepertoireService.Reset();
		Run(1, 10);
		var world = new WorldSaveData();
		AiRepertoireService.CaptureWorld(world);
		Check(AiRepertoireService.Acts.Count == 0 && world.AiRepertoire == null && SaveGameService.WritingVersion != 23,
			"with the shadow off nothing is tracked, saved or versioned");
		var label = Labels().First(l => l.roster.Count > 0);
		var record = ChartManager.Instance.GetAllRecords().First(r => r.baseRecord.format == ReleaseFormat.Single).baseRecord;
		AiRepertoireService.ObserveSingleRelease(label, label.roster[0], record, ReleaseStrategy.OrphanSingle, Start);
		Check(AiRepertoireService.Acts.Count == 0, "a release observation is a no-op with the shadow off");
	}
}
