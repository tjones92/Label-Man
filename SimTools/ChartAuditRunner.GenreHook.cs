using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

// Read-only diagnostics. No catalogue derivation, preparation, registration or random draws.
public partial class ChartAuditRunner {
	private StreamWriter genreFormatUnitsWriter, genreHookLaunchWriter;
	private readonly Dictionary<(int Year, Genre Genre, ReleaseFormat Format), long> genreFormatUnits = new();

	private void OpenGenreHookOutputs(string directory) {
		if (!OS.GetCmdlineUserArgs().Contains("--genre-hook-audit")) return;
		genreFormatUnitsWriter = CreateWriter(Path.Combine(directory, $"{runName}-genre-format-units.csv"));
		genreFormatUnitsWriter.WriteLine("year,genre,format,units");
		genreHookLaunchWriter = CreateWriter(Path.Combine(directory, $"{runName}-genre-hook-launches.csv"));
		genreHookLaunchWriter.WriteLine("week,year,recordId,labelId,tier,artistId,genre,strategy,songId,source,publisherId,compositionHook,hook,production,capability,execution,writerCraft" +
			(AlbumReleasePractice.Enabled ? ",masterId,sourceAlbumRecordId" : ""));
	}

	private void AccumulateGenreFormatUnits(int year, List<RecordRuntimeData> records) {
		if (genreFormatUnitsWriter == null) return;
		foreach (var runtime in records) {
			var key = (year, GenreCatalog.MapLegacy(runtime.baseRecord.primaryGenre, year), runtime.baseRecord.format);
			genreFormatUnits[key] = genreFormatUnits.GetValueOrDefault(key) + runtime.unitsThisWeek;
		}
	}

	private void CaptureGenreHookLaunch(ReleaseStrategyTelemetry strategy, Record record, int year) {
		if (genreHookLaunchWriter == null) return;
		var song = CompositionCatalogService.GetSong(record.songId);
		var fit = PolarSongBehavior.CommittedFit(record);
		var artist = ArtistManager.Instance?.GetArtist(strategy.artistId);
		genreHookLaunchWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(record.recordId), Csv(strategy.labelId), Csv(strategy.tier.ToString()), Csv(strategy.artistId),
			Csv(record.primaryGenre.ToString()), Csv(strategy.strategy.ToString()), Csv(record.songId),
			Csv(record.songSource.ToString()), Csv(song?.rights?.publisherId), song == null ? "" : F(song.commercialHook),
			F(record.hookStrength), F(record.productionQuality), fit == null ? "" : F(fit.capability),
			fit == null ? "" : F(PolarSongBehavior.ExecutionFactor(fit.capability)),
			artist == null ? "" : F(SongMaterialSelectionService.ArtistWriterCraft(artist))
		}) + (AlbumReleasePractice.Enabled ? "," + Csv(record.masterId) + "," + Csv(record.sourceAlbumRecordId) : ""));
	}

	private void CloseGenreHookOutputs() {
		if (genreFormatUnitsWriter != null) {
			foreach (var row in genreFormatUnits.OrderBy(p => p.Key.Year).ThenBy(p => p.Key.Genre).ThenBy(p => p.Key.Format))
				genreFormatUnitsWriter.WriteLine(string.Join(",", row.Key.Year, row.Key.Genre, row.Key.Format, row.Value));
		}
		genreFormatUnitsWriter?.Dispose(); genreHookLaunchWriter?.Dispose();
		genreFormatUnitsWriter = genreHookLaunchWriter = null;
		genreFormatUnits.Clear();
	}
}
