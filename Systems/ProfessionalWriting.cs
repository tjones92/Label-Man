using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Stage one: persistent craft explains part of a professional song's variation.
/// No market-success, access, fee or delivery changes. No draws from shared random streams.</summary>
public static class ProfessionalWriting {
	public static readonly bool Enabled = SimulationArguments.Get().Contains("--professional-writing") || SimulationArguments.Get().Contains("--professional-writing-market");
	public const float WriterWeight = .60f, SongWeight = .80f;

	// Craft is standardized against the existing writer population parameters. The remaining
	// .8 song term retains 64% of the original variance, including misses and exceptional work.
	internal static float Score(SongComposition song, IReadOnlyDictionary<string, ProfessionalSongwriter> writers,
		Func<ProfessionalSongwriter, float> craft, float writerMean, float writerSd, float value, float mean, float sd) {
		var credited = song.credits.Where(c => c.writerType is WriterEntityType.ProfessionalSongwriter or WriterEntityType.Musician &&
			c.writerId != null && writers.ContainsKey(c.writerId)).Select(c => writers[c.writerId]).ToArray();
		if (credited.Length == 0) return (value - mean) / sd;
		float effective = credited.Average(craft);
		if (credited.Length > 1) {
			// Complementary specialists contribute without summing their abilities.
			var crafts = credited.Select(TeamWritingService.Of).ToArray();
			var delta = TeamWritingService.Delta(crafts, 0, 0);
			float friction = crafts.Max(c => c.Melody) - crafts[0].Melody - delta.Melody;
			effective = credited.Max(craft) - friction;
		}
		float fit = credited.Average(w => song.primaryGenre == w.primaryGenre ? 1f :
			song.primaryGenre == w.secondaryGenre ? .85f : .4f + .4f * w.versatility);
		fit += (1 - fit) * .35f * Math.Clamp(song.writingAssignmentFamiliarity, 0, 1);
		return WriterWeight * ((effective - writerMean) / writerSd + .5f * (fit - .8f)) +
			SongWeight * (value - mean) / sd;
	}

	/// <summary>One-time opening allocation: reorder existing values within each genre. This
	/// preserves every marginal quantile, tail count, mean and variance, without inflating supply.</summary>
	internal static void ConditionOpening(IEnumerable<SongComposition> catalogue, IList<ProfessionalSongwriter> writers) {
		var byId = writers.ToDictionary(w => w.writerId, StringComparer.Ordinal);
		foreach (var group in catalogue.Where(s => s.originKind == SongOriginKind.ProfessionalOffice).GroupBy(s => s.primaryGenre)) {
			var songs = group.OrderBy(s => s.songId, StringComparer.Ordinal).ToArray();
			Allocate(s => s.compositionQuality, (s, v) => s.compositionQuality = v, w => w.melodyCraft, .62f, .16f, .62f, .15f);
			Allocate(s => s.melodicStrength, (s, v) => s.melodicStrength = v, w => w.melodyCraft, .62f, .16f, .62f, .15f);
			Allocate(s => s.lyricQuality, (s, v) => s.lyricQuality = v, w => w.lyricCraft, .60f, .16f, .56f, .16f);
			Allocate(s => s.commercialHook, (s, v) => s.commercialHook = v, w => w.hookCraft, .66f, .15f, .68f, .15f);
			void Allocate(Func<SongComposition, float> get, Action<SongComposition, float> set,
				Func<ProfessionalSongwriter, float> craft, float wm, float ws, float mean, float sd) {
				var values = songs.Select(get).OrderBy(v => v).ToArray();
				var order = songs.OrderBy(s => Score(s, byId, craft, wm, ws, get(s), mean, sd))
					.ThenBy(s => s.songId, StringComparer.Ordinal).ToArray();
				for (int i = 0; i < order.Length; i++) set(order[i], values[i]);
			}
		}
	}

	internal static void ConditionNewSong(SongComposition song, IList<ProfessionalSongwriter> writers, ProfessionalSongwriter collaborator = null) {
		var byId = writers.ToDictionary(w => w.writerId, StringComparer.Ordinal);
		if (collaborator != null) byId[collaborator.writerId] = collaborator;
		float Value(Func<ProfessionalSongwriter, float> craft, float wm, float ws, float value, float mean, float sd) =>
			Math.Clamp(mean + sd * Score(song, byId, craft, wm, ws, value, mean, sd), 0f, 1f);
		song.compositionQuality = Value(w => w.melodyCraft, .62f, .16f, song.compositionQuality, .62f, .15f);
		song.melodicStrength = Value(w => w.melodyCraft, .62f, .16f, song.melodicStrength, .62f, .15f);
		song.lyricQuality = Value(w => w.lyricCraft, .60f, .16f, song.lyricQuality, .56f, .16f);
		song.commercialHook = Value(w => w.hookCraft, .66f, .15f, song.commercialHook, .68f, .15f);
	}
}
