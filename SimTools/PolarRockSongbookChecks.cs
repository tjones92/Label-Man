using System;
using System.Linq;
using Godot;

public static class PolarRockSongbookChecks {
	public static void Run() {
		bool prior = PolarSongBehavior.UsePolarFitSelection;
		try {
			PolarSongBehavior.UsePolarFitSelection = false;
			var legacy = SongMaterialSelectionService.GetSourceMixShares(Genre.RockAndRoll, 1960);
			var folk = SongMaterialSelectionService.GetSourceMixShares(Genre.Folk, 1960);
			var legacyPlan = AlbumMaterialPlanner.Plan(Genre.RockAndRoll, 1960, .3f, 12, "probe-act", "probe-album");
			PolarSongBehavior.UsePolarFitSelection = true;
			var rock = SongMaterialSelectionService.GetSourceMixShares(Genre.RockAndRoll, 1960);
			Check(legacy.Std + legacy.Trad > .25f && rock.Std + rock.Trad < .02f && rock.Std > 0 && rock.Trad > 0,
				"rock prior reduced to rare, nonzero exceptions");
			Check(SongMaterialSelectionService.GetSourceMixShares(Genre.Folk, 1960).Trad == folk.Trad, "folk source prior unchanged");
			Check(SongMaterialSelectionService.GetSourceMixShares(Genre.TraditionalPop, 1960, Genre.RockAndRoll).Std < .02f,
				"rock act cannot bypass rarity through a traditional-pop project");
			Check(SongMaterialSelectionService.SongbookExceptionFactor(SongMaterialSource.CoverRecentHit, Genre.RockAndRoll, Genre.RockAndRoll) == 1,
				"recent R&B/blues covers are not songbook exceptions");
			var actor = new SimulatedArtist { primaryGenre = Genre.RockAndRoll, type = ArtistType.Band,
				vocalPower = .6f, musicianship = .6f, studioPerformance = .6f, groupCohesion = .6f,
				members = new() { new Musician { isActive = true, isLeadVocalist = true, technicalSkill = .6f, musicalVersatility = .6f, reliability = .7f } } };
			var pool = SongMaterialSelectionService.RockLiveCoverPool(Genre.RockAndRoll).ToArray();
			int liveRare = 0, liveTotal = 0, albumRare = 0, albumTotal = 0, albumsWithExceptions = 0;
			for (int i = 0; i < 512; i++) {
				actor.artistId = "rock-songbook-fixture-" + i;
				actor.repertoireState = null; // Each fixture ID represents a different act.
				var set = SongMaterialSelectionService.SelectLiveCovers(pool, actor, 1960, 3);
				Check(set.Count == 3 && set.Select(s => s.songId).Distinct().Count() == 3, "live cover count preserved without duplicate songs");
				liveTotal += set.Count; liveRare += set.Count(s => s.EstablishedAsOf(1960) || s.isTraditional);
				var plan = AlbumMaterialPlanner.Plan(Genre.RockAndRoll, 1960, .3f, 12, actor.artistId, "probe-lp-" + i);
				Check(plan.TotalPlanned == 12, "exact album slot count preserved");
				albumTotal += plan.TotalPlanned; albumRare += plan.Standards + plan.Traditional;
				if (plan.Standards + plan.Traditional > 0) albumsWithExceptions++;
				var repeat = AlbumMaterialPlanner.Plan(Genre.RockAndRoll, 1960, .3f, 12, actor.artistId, "probe-lp-" + i);
				Check(plan.Equals(repeat), "album apportionment deterministic");
			}
			Check(liveRare > 0 && (float)liveRare / liveTotal < .05f, "live songbook exceptions are rare and remain possible");
			Check(albumRare > 0 && (float)albumRare / albumTotal < .02f && albumsWithExceptions > 0,
				"rare LP cuts survive fractional rounding");
			PolarSongBehavior.UsePolarFitSelection = false;
			Check(AlbumMaterialPlanner.Plan(Genre.RockAndRoll, 1960, .3f, 12, "probe-act", "probe-album").Equals(legacyPlan), "disabled album allocator unchanged");
			GD.Print($"POLAR_ROCK_SONGBOOK_PASS prior={(rock.Std + rock.Trad):R} live={liveRare}/{liveTotal} album={albumRare}/{albumTotal} albumsWithExceptions={albumsWithExceptions}/512 deterministic=ok disabled=ok");
		} finally { PolarSongBehavior.UsePolarFitSelection = prior; }
	}
	private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("POLAR_ROCK_SONGBOOK_FAIL: " + message); }
}
