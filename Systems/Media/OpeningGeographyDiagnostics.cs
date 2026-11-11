using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>Fresh-start, passive release and regional source census for offline geography shadows.
/// It neither relocates followers nor supplies standing or budgets to gameplay.</summary>
public static class OpeningGeographyDiagnostics {
	private sealed record ReleaseRow(string Artist, string Name, string Archetype, int Year, string Format,
		int Peak, int Markets, string Origin, double Young, double Adult);
	private static readonly List<ReleaseRow> releases = new();
	private static readonly HashSet<string> regionalActs = new(StringComparer.Ordinal);
	private static TextWriter regionalLog;
	private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
	private static string Q(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
	public static void Reset() { releases.Clear(); regionalActs.Clear(); regionalLog = null; }
	internal static void RecordRelease(SimulatedArtist artist, PublicReleaseEntry entry, float young, float adult) {
		releases.Add(new(artist.artistId, artist.stageName, artist.openingArchetype.ToString(), entry.year,
			entry.format.ToString(), entry.peakPosition, entry.markets, entry.regionId, young, adult));
		if (artist.openingArchetype == OpeningCareerArchetype.RegionalName) regionalActs.Add(artist.artistId);
	}
	public static void Open(TextWriter releaseLog, TextWriter regions) {
		regionalLog = regions;
		releaseLog.WriteLine("artistId,artist,openingArchetype,year,format,peakPosition,markets,origin,seedYoung,seedAdult");
		foreach (var r in releases.Where(r => MediaAtmosphereService.FollowingFor(r.Artist) != null))
			releaseLog.WriteLine(string.Join(",", Q(r.Artist), Q(r.Name), r.Archetype, r.Year, r.Format,
				r.Peak, r.Markets, Q(r.Origin), F(r.Young), F(r.Adult)));
		regions.WriteLine("date,artistId,region,populationMillions,followingYoung,followingAdult,openingYoung,openingAdult,openingYoungByYear,salesResponse");
	}
	public static void Close() => regionalLog = null;
	internal static void Observe(GameDate date, string artist, StatureWeeklyShadow.ArtistEvidence evidence,
		StatureWeeklyShadow shadow, StatureWeeklyShadow.Config config) {
		if (regionalLog == null || !regionalActs.Contains(artist)) return;
		var fs = MediaAtmosphereService.FollowingFor(artist);
		foreach (var region in evidence.Regions) {
			FollowingRegion fr = null; fs?.Regions.TryGetValue(region.Id, out fr);
			var smooth = shadow.RegionalHistory[artist][region.Id];
			double? response = smooth.All(n => n.HasValue) ? Math.Min(1, smooth.Max(n => n.Value) / config.LocalSalesShareReference) : null;
			var origin = fr?.OpeningOrigin;
			regionalLog.WriteLine(string.Join(",", Q(date.ToShortString()), Q(artist), Q(region.Id), F(region.PopulationMillions),
				region.Following.HasValue ? F(fr?.YoungTotal() ?? 0) : "", region.Following.HasValue ? F(fr?.Adult ?? 0) : "",
				F(origin?.YoungTotal ?? 0), F(origin?.Adult ?? 0),
				Q(JsonSerializer.Serialize(origin?.Young ?? new Dictionary<int, double>())), response.HasValue ? F(response.Value) : ""));
		}
	}
}
