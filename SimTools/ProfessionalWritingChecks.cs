using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ProfessionalWritingChecks {
	public static void Run() {
		void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
		var writers = Enumerable.Range(0, 40).Select(i => new ProfessionalSongwriter {
			writerId = "check-" + i, name = "Writer " + i, primaryGenre = Genre.TeenPop,
			melodyCraft = .35f + i * .014f, hookCraft = .35f + i * .014f,
			lyricCraft = .9f - i * .014f, versatility = .5f
		}).ToList();
		var random = new PcgRandom { Seed = 12345 };
		float N(float mean, float sd) => Math.Clamp(mean + sd * MathF.Sqrt(-2 * MathF.Log(Math.Max(.000001f, random.Randf()))) *
			MathF.Cos(2 * MathF.PI * random.Randf()), 0, 1);
		var songs = Enumerable.Range(0, 8000).Select(i => new SongComposition {
			songId = "check-song-" + i, primaryGenre = Genre.TeenPop, originKind = SongOriginKind.ProfessionalOffice,
			compositionQuality = N(.62f, .15f), melodicStrength = N(.62f, .15f), lyricQuality = N(.56f, .16f), commercialHook = N(.68f, .15f),
			credits = new() { new SongwriterCredit { writerType = WriterEntityType.ProfessionalSongwriter, writerId = writers[i % writers.Count].writerId, share = 1 } }
		}).ToArray();
		Func<SongComposition, float>[] axes = { s => s.compositionQuality, s => s.melodicStrength, s => s.lyricQuality, s => s.commercialHook };
		var before = axes.Select(get => songs.Select(get).OrderBy(v => v).ToArray()).ToArray();
		string snapshot = JsonSerializer.Serialize(songs, SaveGameService.TestJsonOptions);
		ProfessionalWriting.ConditionOpening(songs, writers);
		for (int i = 0; i < axes.Length; i++) Check(before[i].SequenceEqual(songs.Select(axes[i]).OrderBy(v => v)), "Exact marginal distribution on axis " + i);
		var low = songs.Where((s, i) => i % 40 < 10).ToArray();
		var high = songs.Where((s, i) => i % 40 >= 30).ToArray();
		Check(high.Average(s => s.commercialHook) > low.Average(s => s.commercialHook) + .12, "Hook craft persists across songs");
		Check(low.Average(s => s.lyricQuality) > high.Average(s => s.lyricQuality) + .12, "Lyric specialists differ from hook specialists");
		Check(high.Min(s => s.commercialHook) < low.Max(s => s.commercialHook), "Craft does not guarantee song quality");
		var replay = JsonSerializer.Deserialize<SongComposition[]>(snapshot, SaveGameService.TestJsonOptions);
		ProfessionalWriting.ConditionOpening(replay.Reverse(), writers.AsEnumerable().Reverse().ToList());
		Check(JsonSerializer.Serialize(songs, SaveGameService.TestJsonOptions) == JsonSerializer.Serialize(replay, SaveGameService.TestJsonOptions), "Order independent deterministic allocation");
		var byId = writers.ToDictionary(w => w.writerId);
		var song = songs[0];
		float original = ProfessionalWriting.Score(song, byId, w => w.hookCraft, .66f, .15f, .68f, .68f, .15f);
		song.primaryGenre = Genre.Jazz;
		Check(ProfessionalWriting.Score(song, byId, w => w.hookCraft, .66f, .15f, .68f, .68f, .15f) < original, "Assignment mismatch lowers expectation");
		song.primaryGenre = Genre.TeenPop;
		ProfessionalWriting.ConditionNewSong(song, writers);
		Check(axes.All(get => float.IsFinite(get(song)) && get(song) >= 0 && get(song) <= 1), "New song craft bounded");
		if (ProfessionalWriting.Enabled && BandLife.TeamWritingEnabled) {
			var actual = CompositionCatalogService.AllSongs.Where(s => s.originKind == SongOriginKind.ProfessionalOffice).ToArray();
			Check(actual.Any(s => s.credits.Count == 2), "Standing teams write opening songs");
			Check(actual.All(s => Math.Abs(s.credits.Sum(c => c.share) - 1) < .00001), "Credit shares conserved");
			var pub = CompositionCatalogService.Publishers.First();
			var fresh = Enumerable.Range(0, 30).Select(_ => CompositionCatalogService.WriteOfficeSong(pub.publisherId, pub.focusGenres, 1960)).ToArray();
			Check(fresh.Any(s => s.credits.Count == 2), "Standing teams continue writing new office songs");
		}
		GD.Print("ProfessionalWritingChecks PASS: exact catalogue distributions, craft continuity, specialization, variation, fit, determinism and continuing teams.");
	}
}
