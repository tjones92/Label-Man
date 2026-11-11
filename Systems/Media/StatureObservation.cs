using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

/// <summary>Collects completed-week audience evidence and publishes standing; CSV diagnostics are optional.</summary>
public static class StatureObservation {
	public const string Switch = "--observe-stature-evidence";
	public const string ShadowSwitch = "--observe-stature-shadow=";
	public const string ProgrammeReaderSwitch = "--stature-programme-album-reader";
	public static TextWriter Log;
	public static TextWriter ShadowLog;
	public static StatureWeeklyShadow.Config ShadowConfig;
	// Test-only hook for comparing the inputs supplied by uninterrupted and restored worlds.
	internal static Action<GameDate, IReadOnlyDictionary<string, StatureWeeklyShadow.ArtistEvidence>, string> ReplayEvidenceObserver;
	public static void ConfigureShadow(string path, bool persist) {
		if (!MediaFollowing.Active) throw new InvalidOperationException("Stature shadow requires active media following.");
		string text = File.ReadAllText(path);
		var config = System.Text.Json.JsonSerializer.Deserialize<StatureWeeklyShadow.Config>(text,
			new System.Text.Json.JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true })
			?? throw new InvalidOperationException("Invalid stature calibration file.");
		if (config.Normalization == null || config.Policy == null || string.IsNullOrWhiteSpace(config.Version) ||
			string.IsNullOrWhiteSpace(config.Normalization.Version) || string.IsNullOrWhiteSpace(config.Policy.Version))
			throw new InvalidOperationException("Stature calibration requires versioned normalizer and policy.");
		StatureShadowPolicy.Validate(config.Policy);
		string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
		config.Version += "|sha256:" + hash; config.Normalization.Version += "|sha256:" + hash; config.Policy.Version += "|sha256:" + hash;
		ShadowConfig = config;
		MediaAtmosphereService.PersistExperimentalStature = persist;
	}
	private sealed class Sales {
		public double SingleYoung, SingleAdult, AlbumYoung, AlbumAdult;
		public void Add(ReleaseFormat format, double units, float youngShare) {
			if (format == ReleaseFormat.Single) { SingleYoung += units * youngShare; SingleAdult += units * (1 - youngShare); }
			else if (format == ReleaseFormat.Album) { AlbumYoung += units * youngShare; AlbumAdult += units * (1 - youngShare); }
		}
	}
	// Diagnostic scratch for this clearing week; grants are pre-market, not identified purchases.
	private static readonly Dictionary<(string, string), double[]> followerLaunch = new();
	internal static void BeginFollowerLaunchWeek() { if (Log != null) followerLaunch.Clear(); }
	internal static void RecordFollowerLaunch(Record record, string region, float intent, int grant, int cleared, bool causal) {
		if (Log == null || record == null || string.IsNullOrEmpty(record.artistId) || string.IsNullOrEmpty(region) ||
			(intent <= 0 && grant <= 0) || record.format is not (ReleaseFormat.Single or ReleaseFormat.Album)) return;
		var key = (record.artistId, region);
		if (!followerLaunch.TryGetValue(key, out var values)) followerLaunch[key] = values = new double[6];
		int format = record.format == ReleaseFormat.Single ? 0 : 1;
		values[format] += intent;
		values[2 + format] += grant;
		if (causal) values[4 + format] += Math.Min(grant, cleared);
	}
	private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
	private static string Q(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
	public static void WriteHeader(TextWriter output) => output.WriteLine("date,artistId,artist,labelId,labelTier,genre,careerState,beforeDrop,regionId,regionPopulationMillions,singleYoungUnits,singleAdultUnits,albumYoungUnits,albumAdultUnits,marketSingleYoungUnits,marketSingleAdultUnits,marketAlbumYoungUnits,marketAlbumAdultUnits,tvRegisteredYoungEstimate,tvRegisteredAdultEstimate,followingYoung,followingAdult,followingStatus,singleAdvanceIntent,albumAdvanceIntent,singleYoungRate,singleAdultRate,albumYoungRate,albumAdultRate,singleScale,rawRecognition,effectiveRecognition,cooledLaunchRecognition,buyJoinYoung,buyJoinAdult,tvJoinYoung,tvJoinAdult,pressJoinYoung,pressJoinAdult,seriesJoinYoung,seriesJoinAdult,liveJoinYoung,liveJoinAdult,followerLaunchMode,singleFollowerIntent,albumFollowerIntent,singleFollowerStockGrant,albumFollowerStockGrant,singleFollowerClearedUpperBound,albumFollowerClearedUpperBound");
	public static void WriteShadowHeader(TextWriter output) => output.WriteLine("date,artistId,careerState,beforeDrop,shadowBand,status,snapshotDate,singleYoungScore,singleAdultScore,albumYoungScore,albumAdultScore,tvYoungScore,tvAdultScore,followingYoungScore,followingAdultScore,regionalResponse,regionalDraw,recordReach,conversionReach,previousBand," +
		string.Join(",", new[] { "Rising", "Established", "Star", "Superstar" }.SelectMany(b => new[] { b + "Lower", b + "Upper", b + "Boundary", b + "Regional", b + "Record", b + "Conversion" })));

	internal static void WriteWeek(GameDate date, IReadOnlyList<RecordRuntimeData> records, IReadOnlyList<MarketRegion> regions,
		IReadOnlyDictionary<string, float[]> registeredYoung, IReadOnlyDictionary<string, float[]> registeredAdult,
		IReadOnlyDictionary<(string, string), double[]> bookedJoins) {
		if (Log == null && ShadowConfig == null) return;
		var byAct = new Dictionary<(string, string), Sales>();
		var markets = new Dictionary<string, Sales>(StringComparer.Ordinal);
		foreach (RecordRuntimeData runtime in records) {
			Record record = runtime?.baseRecord;
			if (record == null || record.format is not (ReleaseFormat.Single or ReleaseFormat.Album) || runtime.regionalData == null) continue;
			float share = MediaFollowing.YoungBuyerShare(record.primaryGenre, record.format);
			foreach (var (region, data) in runtime.regionalData) {
				if (data == null || data.unitsSoldThisWeek <= 0) continue;
				if (!markets.TryGetValue(region, out Sales market)) markets[region] = market = new Sales();
				market.Add(record.format, data.unitsSoldThisWeek, share);
				if (string.IsNullOrEmpty(record.artistId)) continue;
				var key = (record.artistId, region);
				if (!byAct.TryGetValue(key, out Sales sales)) byAct[key] = sales = new Sales();
				sales.Add(record.format, data.unitsSoldThisWeek, share);
			}
		}
		var artists = ArtistManager.Instance?.GetAllArtists();
		if (artists == null) return;
		// Preserve GetLabel's first-match behavior without warming its mutable lookup cache.
		var labels = new Dictionary<string, AILabel>(StringComparer.Ordinal);
		foreach (AILabel item in CompetitorManager.Instance?.GetAllLabels() ?? Array.Empty<AILabel>())
			if (item?.labelId != null) labels.TryAdd(item.labelId, item);
		var (singleY, singleA) = MediaFollowing.AdvancePerFollower(ReleaseFormat.Single);
		var (albumY, albumA) = MediaFollowing.AdvancePerFollower(ReleaseFormat.Album);
		var shadowEvidence = ShadowConfig == null ? null : new Dictionary<string, StatureWeeklyShadow.ArtistEvidence>(StringComparer.Ordinal);
		var knownMarkets = regions.Select(r => markets.GetValueOrDefault(r.regionId)).Where(m => m != null).ToArray();
		double[] marketTotals = { knownMarkets.Sum(m => m.SingleYoung), knownMarkets.Sum(m => m.SingleAdult),
			knownMarkets.Sum(m => m.AlbumYoung), knownMarkets.Sum(m => m.AlbumAdult) };
		foreach (SimulatedArtist artist in artists.Where(a => a != null && !a.isPlayerOwned && !string.IsNullOrEmpty(a.artistId) &&
			(shadowEvidence != null || !string.IsNullOrEmpty(a.labelId))).OrderBy(a => a.artistId, StringComparer.Ordinal)) {
			AILabel label = null;
			if (!string.IsNullOrEmpty(artist.labelId) && !labels.TryGetValue(artist.labelId, out label)) continue;
			if (label?.isPlayerOwned == true || (label == null && shadowEvidence == null)) continue;
			MediaFollowingState following = MediaAtmosphereService.FollowingFor(artist.artistId);
			var regional = shadowEvidence == null ? null : new List<StatureWeeklyShadow.Region>();
			double[] totals = new double[8];
			registeredYoung.TryGetValue(artist.artistId, out float[] tvY);
			registeredAdult.TryGetValue(artist.artistId, out float[] tvA);
			for (int i = 0; i < regions.Count; i++) {
				MarketRegion region = regions[i];
				byAct.TryGetValue((artist.artistId, region.regionId), out Sales sales);
				markets.TryGetValue(region.regionId, out Sales market);
				FollowingRegion fr = null;
				following?.Regions.TryGetValue(region.regionId, out fr);
				double[] joins = null;
				bookedJoins?.TryGetValue((artist.artistId, region.regionId), out joins);
				followerLaunch.TryGetValue((artist.artistId, region.regionId), out var follower);
				float young = fr?.YoungTotal() ?? 0f, adult = fr?.Adult ?? 0f;
				if (label != null) Log?.WriteLine(string.Join(",", Q(date.ToShortString()), Q(artist.artistId), Q(artist.stageName), Q(label.labelId),
					label.tier, artist.primaryGenre, artist.careerState, artist.careerStateBeforeDrop, Q(region.regionId), F(region.population),
					F(sales?.SingleYoung ?? 0), F(sales?.SingleAdult ?? 0), F(sales?.AlbumYoung ?? 0), F(sales?.AlbumAdult ?? 0),
					F(market?.SingleYoung ?? 0), F(market?.SingleAdult ?? 0), F(market?.AlbumYoung ?? 0), F(market?.AlbumAdult ?? 0),
					F(tvY?[i] ?? 0), F(tvA?[i] ?? 0), F(young), F(adult), following == null ? "no_following_record" : "observed",
					F(MediaFollowerDemand.RegionIntent(young, adult, ReleaseFormat.Single)), F(MediaFollowerDemand.RegionIntent(young, adult, ReleaseFormat.Album)),
					F(singleY), F(singleA), F(albumY), F(albumA), F(MediaFollowerDemand.SingleScale), F(artist.publicRecognition),
					F(ArtistRecognitionService.EffectiveRecognition(artist)), F(ArtistRecognitionService.ObserveLaunchRecognition(artist)),
					string.Join(",", Enumerable.Range(0, 10).Select(j => F(joins?[j] ?? 0))),
					!MediaFollowerDemand.Active ? "inactive" : MediaFollowerDemand.Causal ? "causal" : "shadow",
					string.Join(",", Enumerable.Range(0, 6).Select(j => F(follower?[j] ?? 0)))));
				if (regional != null) {
					double[] units = { sales?.SingleYoung ?? 0, sales?.SingleAdult ?? 0, sales?.AlbumYoung ?? 0, sales?.AlbumAdult ?? 0 };
					double[] denominator = { market?.SingleYoung ?? 0, market?.SingleAdult ?? 0, market?.AlbumYoung ?? 0, market?.AlbumAdult ?? 0 };
					for (int k = 0; k < 4; k++) totals[k] += units[k];
					totals[4] += tvY?[i] ?? 0; totals[5] += tvA?[i] ?? 0; totals[6] += young; totals[7] += adult;
					regional.Add(new StatureWeeklyShadow.Region { Id = region.regionId, PopulationMillions = region.population,
						SalesShares = Enumerable.Range(0, 4).Select(k => denominator[k] > 0 ? (double?)(units[k] / Math.Max(ShadowConfig.MarketFloorUnits, denominator[k])) : null).ToArray(),
						Following = following == null ? null : (double?)((double)young + adult) });
				}
			}
			if (regional != null) {
				var components = new double?[8];
				for (int k = 0; k < 4; k++) components[k] = marketTotals[k] > 0 ? totals[k] / marketTotals[k] : null;
				components[4] = totals[4]; components[5] = totals[5];
				components[6] = following == null ? null : totals[6]; components[7] = following == null ? null : totals[7];
				shadowEvidence.Add(artist.artistId, new StatureWeeklyShadow.ArtistEvidence { Components = components, Regions = regional, PeerEligible = label != null });
			}
		}
		if (Log != null) followerLaunch.Clear();
		if (shadowEvidence != null) {
			if (ReplayEvidenceObserver != null) {
				double[] Values(Sales s) => new[] { s.SingleYoung, s.SingleAdult, s.AlbumYoung, s.AlbumAdult };
				var source = new {
					Markets = markets.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => Values(p.Value), StringComparer.Ordinal),
					Artists = byAct.GroupBy(p => p.Key.Item1, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
						.ToDictionary(g => g.Key, g => g.OrderBy(p => p.Key.Item2, StringComparer.Ordinal)
							.ToDictionary(p => p.Key.Item2, p => Values(p.Value), StringComparer.Ordinal), StringComparer.Ordinal),
					Records = records.Where(r => r?.baseRecord != null && !string.IsNullOrEmpty(r.baseRecord.artistId) && r.regionalData != null)
						.GroupBy(r => r.baseRecord.artistId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
						.ToDictionary(g => g.Key, g => g.OrderBy(r => r.baseRecord.recordId, StringComparer.Ordinal).Select(r => new {
							Id = r.baseRecord.recordId, Format = r.baseRecord.format.ToString(), Genre = r.baseRecord.primaryGenre.ToString(),
							Regions = r.regionalData.Where(p => p.Value != null).OrderBy(p => p.Key, StringComparer.Ordinal)
								.ToDictionary(p => p.Key, p => p.Value.unitsSoldThisWeek, StringComparer.Ordinal)
						}).ToArray(), StringComparer.Ordinal)
				};
				ReplayEvidenceObserver(date, shadowEvidence, System.Text.Json.JsonSerializer.Serialize(source));
			}
			var day = new DateTime(date.year, date.month, date.day);
			var shadow = MediaAtmosphereService.ExperimentalStatureShadow();
			if (!shadow.Advance(day, shadowEvidence, ShadowConfig)) return;
			OpeningFollowingDiagnostics.Observe(date, shadowEvidence, shadow, ShadowConfig);
			if (ShadowLog != null) foreach (var artist in artists.Where(a => a != null && !string.IsNullOrEmpty(a.artistId) && shadowEvidence.ContainsKey(a.artistId)).OrderBy(a => a.artistId, StringComparer.Ordinal)) {
				shadow.Snapshots.TryGetValue(artist.artistId, out var snapshot);
				var scores = snapshot?.Scores;
				string Optional(double? value) => value.HasValue ? F(value.Value) : "";
				string BandValues(int i) => snapshot == null ? ",,,,," : string.Join(",", F(snapshot.SupportLower[i]), F(snapshot.SupportUpper[i]), F(snapshot.Boundaries[i]),
					F(snapshot.RouteSupportLower[i][0]), F(snapshot.RouteSupportLower[i][1]), F(snapshot.RouteSupportLower[i][2]));
				ShadowLog.WriteLine(string.Join(",", Q(date.ToShortString()), Q(artist.artistId), artist.careerState, artist.careerStateBeforeDrop,
					snapshot?.Band.ToString() ?? "", snapshot?.Status.ToString() ?? "warming_up", snapshot?.Date.ToString("yyyy-MM-dd") ?? "",
					string.Join(",", Enumerable.Range(0, 8).Select(i => scores?[i] is double value ? F(value) : "")),
					Optional(snapshot?.RegionalResponse), Optional(snapshot?.RegionalDraw), Optional(snapshot?.RecordReach), Optional(snapshot?.ConversionReach),
					snapshot?.PreviousBand.ToString() ?? "", string.Join(",", Enumerable.Range(0, 4).Select(BandValues))));
			}
		}
	}
}
