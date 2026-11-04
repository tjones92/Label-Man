using System;
using Godot;

public static class AlbumVolumeChecks {
	public static void Run() {
		float volume = AlbumSalesVolume.Multiplier;
		float initial = LabelPressingEconomics.InitialCommitment, weeks = LabelPressingEconomics.RepressWeeks;
		float albumInitial = LabelPressingEconomics.AlbumInitialCommitment, albumWeeks = LabelPressingEconomics.AlbumRepressWeeks;
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Album volume: " + why); checks++; }
		try {
			AlbumSalesVolume.SetForChecks(1f);
			Check(AlbumSalesVolume.Budget(101) == 101, "the default is exact");
			AlbumSalesVolume.SetForChecks(.75f);
			Check(AlbumSalesVolume.Budget(101) == 75 && AlbumSalesVolume.Budget(0) == 0, "LP budgets are bounded and round down");
			var original = ChartManager.CalculateFormatClearingBudget(200, 300, 200, albumChannelMaturity: 1f, albumChannelCapacity: 120);
			int reduced = AlbumSalesVolume.Budget(original.Album), capacity = 320 - (original.Album - reduced);
			Check(original.Single == 200 && reduced == 90 && capacity == 290 && original.Single + reduced <= capacity,
				"withdrawn LP purchasing cannot become spare Singles or spillover capacity");
			LabelPressingEconomics.SetCommitmentsForChecks(1.5f, 12f);
			LabelPressingEconomics.SetAlbumCommitmentsForChecks(1f, 8f);
			var label = new AILabel { pressingForecastAlbumLogUnits = MathF.Log(8001f), pressingForecastSingleLogUnits = MathF.Log(8001f) };
			RecordRuntimeData Fresh(ReleaseFormat format) => new(new Record { format = format,
				releaseDate = new GameDate(1961, 1, 6) }) { weeksSinceRelease = 1 };
			var album = Fresh(ReleaseFormat.Album); var single = Fresh(ReleaseFormat.Single);
			Check(LabelPressingEconomics.Press(album, label, 100).InitialRun == 8000
				&& LabelPressingEconomics.Press(single, label, 100).InitialRun == 12000, "formats commit their configured forecasts");
			Check(LabelPressingEconomics.Press(album, label, 8000).Repressed == 64000
				&& LabelPressingEconomics.Press(single, label, 12000).Repressed == 144000, "formats reorder their configured weeks");
			GameRandom.Seed(654); float expected = GameRandom.Randf(); GameRandom.Seed(654);
			AlbumSalesVolume.Budget(5000);
			Check(GameRandom.Randf() == expected, "budgeting consumes no random draws");
			GD.Print($"ALBUM_VOLUME_CHECK passed checks={checks}");
		} finally {
			AlbumSalesVolume.SetForChecks(volume);
			LabelPressingEconomics.SetCommitmentsForChecks(initial, weeks);
			LabelPressingEconomics.SetAlbumCommitmentsForChecks(albumInitial, albumWeeks);
		}
	}
}
