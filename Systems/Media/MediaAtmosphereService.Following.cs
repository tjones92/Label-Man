using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Celebrity fanbase Phase 0 (SimTools/CelebrityFanbaseDesign.md §10), observe-only behind <c>--observe-following</c>.
/// Once a week, after the fans respond to the completed week, every act's following takes the week's buyers (all
/// formats, split young and adult) and the television viewers who registered it, and loses drift, ageing, silence,
/// disappointment and switching. Adult regard warms from adult buying, adult viewing and mainstream press. Salience
/// smooths each act's share of the nation's attention and moves the sensation and phenomenon states. Everything lands
/// in <see cref="MediaSaveData.Following"/> and the telemetry; nothing outside media reads it.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>Airings tallied this week with their fresh reach, for the following pass. Consumed the same step, never saved.</summary>
	private static readonly List<(MediaAppearance Appearance, MediaAiring Airing, double Fresh)> followingAirings = new();
	private const int AdultRankDepth = 20;
	private const float FollowingPruneFloor = 25f;
	private const float FollowingLogFloor = 1000f;

	public static IReadOnlyDictionary<string, MediaFollowingState> Following => state.Following;
	public static MediaFollowingState FollowingFor(string artistId) => artistId != null && state.Following.TryGetValue(artistId, out var f) ? f : null;

	/// <summary>Year ledger for MEDIA_FOLLOWING (people), print-only and cleared each year end.</summary>
	private static readonly Dictionary<string, double> followingLedger = new(StringComparer.Ordinal);
	private static void FollowingLedger(string key, double amount) => followingLedger[key] = followingLedger.GetValueOrDefault(key) + amount;
	/// <summary>Joiners since each act's last CSV row (buyers, viewers), print-only.</summary>
	private static readonly Dictionary<string, (double Buy, double Tv)> followingSinceRow = new(StringComparer.Ordinal);
	private static readonly List<string> followingTransitions = new();
	private static float yearTopLead, yearTopSalience;
	private static string yearTopName;

	private static void FollowingAiring(MediaAppearance a, MediaAiring airing, double fresh) {
		if (MediaFollowing.Active && !a.Cut && fresh > 0) followingAirings.Add((a, airing, fresh));
	}

	/// <summary>
	/// January 1960: an act walks in with the following its last five years of hits built (OpeningCareerService history),
	/// sized off the same arithmetic as a weekly buyer at middling heat: a No. 1 about 1M units, a Top 10 500k, a Top 40
	/// 200k, a quarter fewer for each year back. Cohorts are dated to the hit's year, so a 1956 teen fan ages out on time.
	/// </summary>
	private static void SeedOpeningFollowing(Evidence evidence, GameDate start) {
		if (!MediaFollowing.Active || ArtistManager.Instance == null || evidence.Regions == null || evidence.Regions.Count == 0) return;
		double totalPop = evidence.Regions.Sum(x => (double)x.population);
		if (totalPop <= 0) return;
		int today = BroadcastExposure.DayIndex(start);
		foreach (SimulatedArtist a in ArtistManager.Instance.GetAllArtists()) {
			if (a?.publicHistory == null || a.publicHistory.Count == 0) continue;
			var youngByYear = new Dictionary<int, float>();
			float adult = 0f;
			float young = MediaFollowing.YoungBuyerShare(a.primaryGenre, ReleaseFormat.Single);
			foreach (PublicReleaseEntry e in a.publicHistory) {
				if (e.year < start.year - 5 || e.peakPosition <= 0) continue;
				float units = e.peakPosition == 1 ? 1_000_000f : e.peakPosition <= 10 ? 500_000f : e.peakPosition <= 40 ? 200_000f : 50_000f;
				float people = units * MediaFollowing.CBuy * MediaFollowing.HeatFactor(0.5f) * (float)Math.Pow(0.75, Math.Max(0, start.year - e.year));
				youngByYear[e.year] = youngByYear.GetValueOrDefault(e.year) + people * young;
				adult += people * (1f - young);
			}
			if (youngByYear.Values.Sum() + adult < FollowingPruneFloor) continue;
			var fs = new MediaFollowingState { ArtistId = a.artistId, ArtistName = a.stageName, LastActiveDay = today };
			foreach (MarketRegion region in evidence.Regions) {
				float w = (float)(region.population / totalPop);
				var fr = new FollowingRegion { Adult = adult * w };
				foreach (var (year, n) in youngByYear) fr.Young[year] = n * w;
				fs.Regions[region.regionId] = fr;
			}
			state.Following[a.artistId] = fs;
		}
	}

	private static void FollowingWeek(Evidence evidence, GameDate weekEnd) {
		var regions = evidence.Regions;
		int r = regions.Count;
		if (!MediaFollowing.Active || r == 0) { followingAirings.Clear(); residentAirings.Clear(); return; }
		int today = BroadcastExposure.DayIndex(weekEnd);
		int year = weekEnd.year;
		var index = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < r; i++) index[regions[i].regionId] = i;
		float youngShare = BroadcastExposure.GroupShare(ViewerGroup.Teen, year) + BroadcastExposure.GroupShare(ViewerGroup.YoungAdult, year);
		float adultShare = BroadcastExposure.GroupShare(ViewerGroup.Adult, year);
		double[] youngPop = regions.Select(x => Math.Max(1.0, x.population * 1e6 * youngShare)).ToArray();
		double[] adultPop = regions.Select(x => Math.Max(1.0, x.population * 1e6 * adultShare)).ToArray();
		float wYoung = youngShare / (youngShare + adultShare);
		float[] Row(Dictionary<string, float[]> map, string id) { if (!map.TryGetValue(id, out var v)) map[id] = v = new float[r]; return v; }

		// Attention last week: each act above the sensation bar takes its excess from everyone else's registration.
		var excess = new Dictionary<string, float>(StringComparer.Ordinal);
		float totalExcess = 0f, switchingSalience = 0f;
		var sensations = new Dictionary<string, float>(StringComparer.Ordinal);
		foreach (MediaFollowingState s in state.Following.Values) {
			float sal = s.Salience();
			if (sal > MediaFollowing.SensationShare) { excess[s.ArtistId] = sal - MediaFollowing.SensationShare; totalExcess += excess[s.ArtistId]; }
			if (s.State != SalienceState.None) { sensations[s.ArtistId] = sal; switchingSalience += sal; }
		}

		// Buyers: every record's units this week, by region, split young and adult by genre and format.
		var buyYoung = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var buyAdult = new Dictionary<string, float[]>(StringComparer.Ordinal);
		// Attention units (§7.1): the same buyers with an LP counted at a third of a single, national per act (young, adult).
		var attentionUnits = new Dictionary<string, double[]>(StringComparer.Ordinal);
		var releases = new List<RecordRuntimeData>();
		var top40 = new HashSet<string>(StringComparer.Ordinal);
		var charting = new HashSet<string>(StringComparer.Ordinal);
		var latestRelease = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (RecordRuntimeData rec in evidence.All) {
			Record b = rec?.baseRecord;
			if (b == null || string.IsNullOrEmpty(b.artistId)) continue;
			int released = BroadcastExposure.DayIndex(b.releaseDate);
			if (released <= today) {
				latestRelease[b.artistId] = Math.Max(latestRelease.GetValueOrDefault(b.artistId, -1), released);
				if (state.Following.TryGetValue(b.artistId, out var seen0) ? !seen0.ReleasesSeen.Contains(b.recordId) : released > today - 14) releases.Add(rec);
			}
			if (b.format == ReleaseFormat.Single && rec.currentPosition is >= 1 and <= 40) top40.Add(b.artistId);
			if (b.format == ReleaseFormat.Single && rec.currentPosition is >= 1 and <= 100) charting.Add(b.artistId);
			if (rec.regionalData == null) continue;
			float y = MediaFollowing.YoungBuyerShare(b.primaryGenre, b.format);
			float weight = b.format == ReleaseFormat.Single ? 1f : MediaFollowing.AlbumAttentionWeight;
			double[] att = null;
			float[] yr = null, ar = null;
			foreach (var (regionId, rd) in rec.regionalData) {
				if (rd == null || rd.unitsSoldThisWeek <= 0 || !index.TryGetValue(regionId, out int i)) continue;
				yr ??= Row(buyYoung, b.artistId); ar ??= Row(buyAdult, b.artistId);
				yr[i] += rd.unitsSoldThisWeek * y;
				ar[i] += rd.unitsSoldThisWeek * (1f - y);
				att ??= attentionUnits.TryGetValue(b.artistId, out var existing) ? existing : attentionUnits[b.artistId] = new double[2];
				att[0] += rd.unitsSoldThisWeek * y * weight;
				att[1] += rd.unitsSoldThisWeek * (1f - y) * weight;
			}
		}

		// Television: the viewers who saw the act fresh and registered it (§3.1), by region and audience.
		var regYoung = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var regAdult = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var seenAdult = new Dictionary<string, float[]>(StringComparer.Ordinal);
		foreach (var (a, airing, fresh) in followingAirings) {
			ExposureEstimate x = airing.Exposure;
			if (x?.ReachByRegion == null || x.UniqueReach <= 0) continue;
			double groups = x.AverageByGroup.Sum();
			if (groups <= 0) continue;
			double y = (x.AverageByGroup[(int)ViewerGroup.Teen] + x.AverageByGroup[(int)ViewerGroup.YoungAdult]) / groups;
			double ad = x.AverageByGroup[(int)ViewerGroup.Adult] / groups;
			double freshShare = Math.Clamp(fresh / x.UniqueReach, 0, 1);
			MediaActTally t = TallyFor(a.ArtistId);
			bool national = MediaOutletCatalog.Get(a.ProgramId)?.Network != TvNetwork.Local;
			int datesBefore = Math.Max(0, (t?.NationalAppearances ?? 0) - (national ? 1 : 0));
			float attention = MediaFollowing.Attention(totalExcess - excess.GetValueOrDefault(a.ArtistId));
			float perf = MediaFollowing.RegistrationPerformance(a.Performance);
			Genre genre = ArtistManager.Instance?.GetArtist(a.ArtistId)?.primaryGenre ?? a.Genre;
			double k = MediaFollowing.R0 * perf * MediaFollowing.Novelty(datesBefore) * attention * freshShare;
			float[] ry = Row(regYoung, a.ArtistId), ra = Row(regAdult, a.ArtistId), sa = Row(seenAdult, a.ArtistId);
			foreach (var (regionId, reach) in x.ReachByRegion) {
				if (reach <= 0 || !index.TryGetValue(regionId, out int i)) continue;
				ry[i] += (float)(reach * y * k * MediaFollowing.AudienceFit(genre, young: true));
				ra[i] += (float)(reach * ad * k * MediaFollowing.AudienceFit(genre, young: false));
				sa[i] += (float)(reach * ad * freshShare * perf / adultPop[i]);
			}
		}
		followingAirings.Clear();

		// Series (Phase 3a, §3.3.1): a resident's regulars join every week, from the week's best episode and all its
		// viewers, not only the fresh ones registration counts.
		var serYoung = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var serAdult = new Dictionary<string, float[]>(StringComparer.Ordinal);
		foreach (var week in residentAirings.GroupBy(x => x.Appearance.ArtistId + "|" + x.Appearance.ProgramId, StringComparer.Ordinal)) {
			var (a, airing) = week.OrderByDescending(x => x.Airing.Exposure?.UniqueReach ?? 0).ThenBy(x => x.Appearance.Id, StringComparer.Ordinal).First();
			// A daily show's week reaches more people than its best night: scale each region's reach by √episodes, at most 2x.
			double spread = MediaResidencies.WeekViewers(1.0, week.Count());
			ExposureEstimate x = airing.Exposure;
			if (x?.ReachByRegion == null || x.UniqueReach <= 0) continue;
			double groups = x.AverageByGroup.Sum();
			if (groups <= 0) continue;
			double y = (x.AverageByGroup[(int)ViewerGroup.Teen] + x.AverageByGroup[(int)ViewerGroup.YoungAdult]) / groups;
			double ad = x.AverageByGroup[(int)ViewerGroup.Adult] / groups;
			float perf = MediaFollowing.RegistrationPerformance(a.Performance);
			Genre genre = ArtistManager.Instance?.GetArtist(a.ArtistId)?.primaryGenre ?? a.Genre;
			float fy = MediaFollowing.AudienceFit(genre, young: true), fa = MediaFollowing.AudienceFit(genre, young: false);
			float[] sy = Row(serYoung, a.ArtistId), sa = Row(serAdult, a.ArtistId);
			double offered = 0;
			foreach (var (regionId, reach) in x.ReachByRegion) {
				if (reach <= 0 || !index.TryGetValue(regionId, out int i)) continue;
				double seen = reach * spread;
				float jy = MediaResidencies.SeriesJoiners(seen, y, fy, perf, young: true), ja = MediaResidencies.SeriesJoiners(seen, ad, fa, perf, young: false);
				sy[i] += jy; sa[i] += ja; offered += jy + ja;
			}
			MediaResidency residency = state.Residencies.LastOrDefault(z => z.ArtistId == a.ArtistId && z.ProgramId == a.ProgramId);
			if (residency != null) residency.SeriesJoiners += offered;
			ResidencyLedger("seriesJoiners", offered);
		}
		residentAirings.Clear();

		// Press: this week's items about an act, one per root at the widest outlet that ran it; mainstream and local
		// dailies also warm adult regard where they are read.
		var coverage = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
		var adultPress = new Dictionary<string, float[]>(StringComparer.Ordinal);
		// Phase 2 (§2.2, §4): fan-magazine and specialist readers who join, per act and root at the widest outlet, young and adult.
		var readRoots = new Dictionary<string, Dictionary<string, (PressOutlet Outlet, double People)>>(StringComparer.Ordinal);
		foreach (MediaStory s in state.Stories) {
			if (s.Status != StoryStatus.Scheduled || s.ArtistId == null || s.IssueDay > today || s.IssueDay <= today - 7) continue;
			PressOutlet outlet = MediaOutletCatalog.GetPress(s.OutletId);
			if (outlet == null || outlet.Family == PressFamily.Trade) continue;
			if (!coverage.TryGetValue(s.ArtistId, out var roots)) coverage[s.ArtistId] = roots = new Dictionary<string, double>(StringComparer.Ordinal);
			string root = s.RootId ?? s.Id;
			roots[root] = Math.Max(roots.GetValueOrDefault(root), outlet.ReadersMillions * 1e6 * outlet.Fit(s.Genre));
			if (MediaRecognition.Registration && outlet.Family is PressFamily.TeenFan or PressFamily.Specialist) {
				if (!readRoots.TryGetValue(s.ArtistId, out var mine)) readRoots[s.ArtistId] = mine = new Dictionary<string, (PressOutlet, double)>(StringComparer.Ordinal);
				double people = outlet.ReadersMillions * 1e6 * Math.Clamp(outlet.Fit(s.Genre), 0f, 1f) * MediaFollowing.ReadConversion(s.Angle);
				if (!mine.TryGetValue(root, out var widest) || people > widest.People) mine[root] = (outlet, people);
				FollowingLedger("readOffered" + outlet.Family, people);
			}
			float[] row = null;
			if (outlet.National && outlet.Family == PressFamily.Mainstream) {
				row = Row(adultPress, s.ArtistId);
				for (int i = 0; i < r; i++) row[i] = Math.Min(2f, row[i] + 1f);
			} else if (outlet.Family == PressFamily.LocalDaily && outlet.HomeCityId != null
				&& DistanceModel.GetCityById(outlet.HomeCityId)?.parentRegionId is string region && index.TryGetValue(region, out int i)) {
				row = Row(adultPress, s.ArtistId);
				row[i] = Math.Min(2f, row[i] + 0.5f);
			}
		}

		// Reader joiners by region: a national magazine's spread by each region's young or adult people, a city paper's in its
		// home region. A teen magazine's readers are young; a specialist paper's split like an LP's buyers of the genre.
		var readYoung = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var readAdult = new Dictionary<string, float[]>(StringComparer.Ordinal);
		double youngPopTotal = youngPop.Sum(), adultPopTotal = adultPop.Sum();
		foreach (var (id, roots) in readRoots) {
			Genre genre = ArtistManager.Instance?.GetArtist(id)?.primaryGenre ?? Genre.TraditionalPop;
			foreach (var (outlet, people) in roots.Values) {
				float y = outlet.Family == PressFamily.TeenFan ? 1f : MediaFollowing.YoungBuyerShare(genre, ReleaseFormat.Album);
				int home = !outlet.National && DistanceModel.GetCityById(outlet.HomeCityId)?.parentRegionId is string region && index.TryGetValue(region, out int h) ? h : -1;
				if (!outlet.National && home < 0) continue;
				float[] ry = Row(readYoung, id), ra = Row(readAdult, id);
				for (int i = 0; i < r; i++) {
					if (home >= 0 && i != home) continue;
					ry[i] += (float)(people * y * (home >= 0 ? 1.0 : youngPop[i] / youngPopTotal));
					ra[i] += (float)(people * (1f - y) * (home >= 0 ? 1.0 : adultPop[i] / adultPopTotal));
				}
			}
		}

		// Adult purchase standing: each region's top acts by adult buyers this week, on the attachment's 1 - k/20 scale.
		var adultRank = new Dictionary<string, float[]>(StringComparer.Ordinal);
		for (int i = 0; i < r; i++) {
			var ranked = buyAdult.Where(p => p.Value[i] > 0f).OrderByDescending(p => p.Value[i]).ThenBy(p => p.Key, StringComparer.Ordinal).Take(AdultRankDepth).ToList();
			for (int k = 0; k < ranked.Count; k++) Row(adultRank, ranked[k].Key)[i] = 1f - k / (float)AdultRankDepth;
		}

		// National totals for this week's attention shares.
		double totalYoung = attentionUnits.Values.Sum(v => v[0]), totalAdult = attentionUnits.Values.Sum(v => v[1]);
		double totalRegYoung = regYoung.Values.Sum(v => v.Sum()), totalRegAdult = regAdult.Values.Sum(v => v.Sum());
		double totalCoverage = coverage.Values.Sum(v => v.Values.Sum());

		var acts = new SortedSet<string>(state.Following.Keys, StringComparer.Ordinal);
		acts.UnionWith(buyYoung.Keys); acts.UnionWith(regYoung.Keys); acts.UnionWith(coverage.Keys); acts.UnionWith(readYoung.Keys); acts.UnionWith(serYoung.Keys);
		var releasesByAct = releases.GroupBy(x => x.baseRecord.artistId, StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.OrderBy(x => BroadcastExposure.DayIndex(x.baseRecord.releaseDate)).ThenBy(x => x.baseRecord.recordId, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);
		float driftYoung = MediaFollowing.Weekly(MediaFollowing.YoungDriftYear), driftAdult = MediaFollowing.Weekly(MediaFollowing.AdultDriftYear);
		float silenceYoung = MediaFollowing.Weekly(MediaFollowing.SilenceYoungYear), silenceAdult = MediaFollowing.Weekly(MediaFollowing.SilenceAdultYear);
		bool monthRow = weekEnd.day <= 7;

		foreach (string id in acts) {
			buyYoung.TryGetValue(id, out float[] by); buyAdult.TryGetValue(id, out float[] ba);
			regYoung.TryGetValue(id, out float[] gy); regAdult.TryGetValue(id, out float[] ga);
			readYoung.TryGetValue(id, out float[] py); readAdult.TryGetValue(id, out float[] pa);
			serYoung.TryGetValue(id, out float[] seriesY); serAdult.TryGetValue(id, out float[] seriesA);
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(id);
			Genre genre = artist?.primaryGenre ?? Genre.TraditionalPop;
			float fitY = MediaFollowing.AudienceFit(genre, young: true), fitA = MediaFollowing.AudienceFit(genre, young: false);
			state.Fans.TryGetValue(id, out MediaFanState fans);
			bool exists = state.Following.TryGetValue(id, out MediaFollowingState fs);
			if (!exists) {
				// A record isn't kept for every act in the shops: only once a week's joiners would make a real start.
				double would = 0;
				for (int i = 0; i < r; i++)
					would += ((by?[i] ?? 0f) + (ba?[i] ?? 0f)) * MediaFollowing.CBuy * MediaFollowing.HeatFactor(0f) + (gy?[i] ?? 0f) * MediaFollowing.CView + (ga?[i] ?? 0f) * MediaFollowing.CView
						+ (py?[i] ?? 0f) + (pa?[i] ?? 0f) + (seriesY?[i] ?? 0f) + (seriesA?[i] ?? 0f);
				if (would < FollowingPruneFloor) continue;
				fs = new MediaFollowingState { ArtistId = id, ArtistName = artist?.stageName, LastActiveDay = today };
				state.Following[id] = fs;
			}
			fs.ArtistName = artist?.stageName ?? fs.ArtistName;
			if (latestRelease.TryGetValue(id, out int last)) fs.LastReleaseDay = Math.Max(fs.LastReleaseDay, last);
			if (top40.Contains(id)) fs.LastTop40Day = today;

			// Releases first: a disappointing record loses followers before its own buyers can join.

			if (releasesByAct.TryGetValue(id, out var mine)) {
				foreach (RecordRuntimeData rec in mine) {
					if (!fs.ReleasesSeen.Add(rec.baseRecord.recordId)) continue;
					bool single = rec.baseRecord.format == ReleaseFormat.Single;
					List<float> recent = single ? fs.RecentSingles : fs.RecentAlbums;
					float q = rec.GetQuality();
					bool fresh = BroadcastExposure.DayIndex(rec.baseRecord.releaseDate) > today - 14;
					if (fresh) {
						float loss = MediaFollowing.Disappointment(recent, q) * (fs.HardenedUntilDay >= today ? MediaFollowing.HardenedDisappointment : 1f);
						var (perY, perA) = MediaFollowing.AdvancePerFollower(rec.baseRecord.format);
						float young = fs.YoungTotal(), adult = fs.AdultTotal();
						if (loss > 0f) {
							FollowingLedger("disappointment", (young + adult) * loss);
							foreach (FollowingRegion fr in fs.Regions.Values) {
								foreach (int c in fr.Young.Keys.ToList()) fr.Young[c] *= 1f - loss;
								fr.Adult *= 1f - loss;
							}
						}
						float advance = (young * perY + adult * perA) * (1f - loss);
						FollowingLedger(single ? "advanceSingles" : "advanceAlbums", advance);
						FollowingLedger(single ? "releasesSingles" : "releasesAlbums", 1);
						FollowingRow("release", weekEnd, fs, genre, $"{advance:F0}", $"{rec.baseRecord.format}|q={q:F3}|avg={(recent.Count > 0 ? recent.Average() : 0):F3}|loss={loss:F3}|{Csv(rec.baseRecord.title)}");
					}
					recent.Add(q);
					while (recent.Count > MediaFollowing.QualityMemory) recent.RemoveAt(0);
				}
			}

			// Regard: decays, then the week's adult buying, adult viewing and adult press deposit (§4.1).
			adultRank.TryGetValue(id, out float[] rank);
			seenAdult.TryGetValue(id, out float[] seenA);
			adultPress.TryGetValue(id, out float[] press);
			for (int i = 0; i < r; i++) {
				string region = regions[i].regionId;
				float g = fs.Regard.TryGetValue(region, out float v) ? v * MediaFollowing.RegardRetention : 0f;
				g = MediaFollowing.RegardDeposit(g, 0.06f * (rank?[i] ?? 0f) + 0.4f * (seenA?[i] ?? 0f) + 0.04f * (press?[i] ?? 0f));
				if (g >= 0.01f) fs.Regard[region] = g; else fs.Regard.Remove(region);
			}

			// The following itself: losses on the standing stock, then this week's joiners (§2.2, §2.3).
			bool silent = fs.LastReleaseDay < 0 || today - fs.LastReleaseDay > MediaFollowing.SilenceDays;
			float switching = MediaFollowing.Weekly(MediaFollowing.SwitchingPerSalienceYear * (switchingSalience - sensations.GetValueOrDefault(id)));
			float youngLoss = driftYoung + (silent ? silenceYoung : 0f) + switching, adultLoss = driftAdult + (silent ? silenceAdult : 0f);
			double joinBuy = 0, joinTv = 0, joinPress = 0, joinSeries = 0;
			for (int i = 0; i < r; i++) {
				string region = regions[i].regionId;
				if (!fs.Regions.TryGetValue(region, out FollowingRegion fr)) fr = new FollowingRegion();
				float youngBefore = 0f, aged = 0f;
				foreach (int c in fr.Young.Keys.ToList()) {
					float n = fr.Young[c];
					youngBefore += n;
					float out_ = n * MediaFollowing.AgeOutThisWeek((today - BroadcastExposure.DayIndex(new GameDate(c, 7, 1))) / 365.25);
					n -= out_; aged += out_;
					n *= 1f - youngLoss;
					if (n < 1f) fr.Young.Remove(c); else fr.Young[c] = n;
				}
				FollowingLedger("ageOut", aged);
				FollowingLedger("drift", (youngBefore - aged) * driftYoung + fr.Adult * driftAdult);
				if (silent) FollowingLedger("silence", (youngBefore - aged) * silenceYoung + fr.Adult * silenceAdult);
				FollowingLedger("switching", (youngBefore - aged) * switching);
				fr.Adult = fr.Adult * (1f - adultLoss);
				double fitPopA = adultPop[i] * fitA, fitPopY = youngPop[i] * fitY;
				float toAdult = aged * MediaFollowing.AgeOutToAdult;
				fr.Adult = (float)Math.Min(fitPopA, fr.Adult + toAdult);
				FollowingLedger("ageToAdult", toAdult);

				float heatY = MediaFollowing.HeatFactor(fans != null && fans.Attachment.TryGetValue(region, out float att) ? att : 0f);
				float heatA = MediaFollowing.HeatFactor(fs.Regard.GetValueOrDefault(region));
				float buyJoinY = (by?[i] ?? 0f) * MediaFollowing.CBuy * heatY, tvJoinY = (gy?[i] ?? 0f) * MediaFollowing.CView;
				float buyJoinA = (ba?[i] ?? 0f) * MediaFollowing.CBuy * heatA, tvJoinA = (ga?[i] ?? 0f) * MediaFollowing.CView;
				float readJoinY = py?[i] ?? 0f, readJoinA = pa?[i] ?? 0f;
				float seriesJoinY = seriesY?[i] ?? 0f, seriesJoinA = seriesA?[i] ?? 0f;
				float youngNow = fr.YoungTotal();
				float youngAfter = MediaFollowing.Join(youngNow, buyJoinY + tvJoinY + readJoinY + seriesJoinY, fitPopY);
				if (youngAfter > youngNow) fr.Young[year] = fr.Young.GetValueOrDefault(year) + (youngAfter - youngNow);
				float adultAfter = MediaFollowing.Join(fr.Adult, buyJoinA + tvJoinA + readJoinA + seriesJoinA, fitPopA);
				// Joiners are booked by source in proportion to what each offered; saturation scales them all alike.
				double Room(float before, float after, float offered) => offered > 0f ? (after - before) / offered : 0.0;
				double ry = Room(youngNow, youngAfter, buyJoinY + tvJoinY + readJoinY + seriesJoinY), ra = Room(fr.Adult, adultAfter, buyJoinA + tvJoinA + readJoinA + seriesJoinA);
				joinBuy += buyJoinY * ry + buyJoinA * ra;
				joinTv += tvJoinY * ry + tvJoinA * ra;
				joinPress += readJoinY * ry + readJoinA * ra;
				joinSeries += seriesJoinY * ry + seriesJoinA * ra;
				fr.Adult = adultAfter;
				if (fr.Young.Count == 0 && fr.Adult < 1f) fs.Regions.Remove(region); else fs.Regions[region] = fr;
			}
			FollowingLedger("joinBuy", joinBuy);
			FollowingLedger("joinTv", joinTv);
			FollowingLedger("joinPress", joinPress);
			FollowingLedger("joinSeries", joinSeries);
			var since = followingSinceRow.GetValueOrDefault(id);
			followingSinceRow[id] = (since.Buy + joinBuy, since.Tv + joinTv);

			// This week's attention shares (§7.1), smoothed.
			attentionUnits.TryGetValue(id, out double[] at);
			double sy = at?[0] ?? 0, sad = at?[1] ?? 0, ty = gy?.Sum() ?? 0, tad = ga?.Sum() ?? 0;
			double cov = coverage.TryGetValue(id, out var roots) ? roots.Values.Sum() : 0;
			float salesY = totalYoung > 0 ? (float)(sy / totalYoung) : 0f, salesA = totalAdult > 0 ? (float)(sad / totalAdult) : 0f;
			float tvY = totalRegYoung > 0 ? (float)(ty / totalRegYoung) : 0f, tvA = totalRegAdult > 0 ? (float)(tad / totalRegAdult) : 0f;
			float tvAll = totalRegYoung + totalRegAdult > 0 ? (float)((ty + tad) / (totalRegYoung + totalRegAdult)) : 0f;
			fs.Family[MediaFollowing.SalesFamily] = MediaFollowing.Smooth(fs.Family[MediaFollowing.SalesFamily], wYoung * salesY + (1f - wYoung) * salesA);
			fs.Family[MediaFollowing.TvFamily] = MediaFollowing.Smooth(fs.Family[MediaFollowing.TvFamily], tvAll);
			fs.Family[MediaFollowing.PressFamily] = MediaFollowing.Smooth(fs.Family[MediaFollowing.PressFamily], totalCoverage > 0 ? (float)(cov / totalCoverage) : 0f);
			float ws = MediaFollowing.FamilyWeight[MediaFollowing.SalesFamily], wt = MediaFollowing.FamilyWeight[MediaFollowing.TvFamily];
			fs.Audience[0] = MediaFollowing.Smooth(fs.Audience[0], (ws * salesY + wt * tvY) / (ws + wt));
			fs.Audience[1] = MediaFollowing.Smooth(fs.Audience[1], (ws * salesA + wt * tvA) / (ws + wt));
			fs.YoungFamily[MediaFollowing.SalesFamily] = MediaFollowing.Smooth(fs.YoungFamily[MediaFollowing.SalesFamily], salesY);
			fs.YoungFamily[MediaFollowing.TvFamily] = MediaFollowing.Smooth(fs.YoungFamily[MediaFollowing.TvFamily], tvY);

			float total = fs.YoungTotal() + fs.AdultTotal();
			if (total > fs.PeakFollowing) { fs.PeakFollowing = total; fs.PeakFollowingDay = today; }
			if (by != null || gy != null || roots != null || seriesY != null) fs.LastActiveDay = today;
			if (monthRow && total >= FollowingLogFloor) {
				var j = followingSinceRow.GetValueOrDefault(id);
				FollowingRow("act", weekEnd, fs, genre, $"{j.Buy:F0}|{j.Tv:F0}", artist?.labelId ?? "");
				followingSinceRow.Remove(id);
			}
		}

		// Breadth bars against this week's field: 3x the mean share of the acts on the national chart.
		MediaFollowing.SetBars(charting.Where(state.Following.ContainsKey).Select(x => state.Following[x]).ToList());
		// States: each act against the strongest other act (the leader against the runner-up).
		var order = state.Following.Values.Select(s => (State: s, Salience: s.Salience()))
			.OrderByDescending(x => x.Salience).ThenBy(x => x.State.ArtistId, StringComparer.Ordinal).ToList();
		float first = order.Count > 0 ? order[0].Salience : 0f, second = order.Count > 1 ? order[1].Salience : 0f;
		if (first > yearTopSalience) { yearTopSalience = first; yearTopName = order[0].State.ArtistName; }
		if (second > 0f) yearTopLead = Math.Max(yearTopLead, first / second);
		if (order.Count > 0) {
			MediaFollowingState lead = order[0].State;
			FollowingRow("week", weekEnd, lead, ArtistManager.Instance?.GetArtist(lead.ArtistId)?.primaryGenre ?? Genre.TraditionalPop,
				$"{first:F5}", $"second={second:F5}|lead={(second > 0 ? first / second : 0):F2}|breadth={MediaFollowing.Breadth(lead)}" +
				$"|bars={string.Join("/", MediaFollowing.FamilyBar.Concat(MediaFollowing.AudienceBar).Select(b => b.ToString("F4", CultureInfo.InvariantCulture)))}" +
				$"|inState={state.Following.Values.Count(s => s.State != SalienceState.None)}|secondAct={Csv(order.Count > 1 ? order[1].State.ArtistName : "")}");
		}
		for (int k = 0; k < order.Count; k++) {
			MediaFollowingState s = order[k].State;
			s.PeakSalience = Math.Max(s.PeakSalience, order[k].Salience);
			SalienceState from = s.State;
			if (!MediaFollowing.Advance(s, k == 0 ? second : first, today)) continue;
			string line = $"{weekEnd.ToShortString()} \"{s.ArtistName}\" {from}->{s.State} salience={order[k].Salience:F4} breadth={MediaFollowing.Breadth(s)} lead={(k == 0 && second > 0 ? first / second : order[k].Salience / Math.Max(first, 1e-6f)):F2} following={s.YoungTotal() + s.AdultTotal():F0} young={s.Audience[0]:F4} adult={s.Audience[1]:F4} youngBreadth={MediaFollowing.YoungBreadth(s)}";
			followingTransitions.Add(line);
			Godot.GD.Print("MEDIA_SALIENCE " + line);
		}

		// Forget acts with nothing left: no following, no regard, no attention.
		foreach (string id in state.Following.Keys.ToList()) {
			MediaFollowingState s = state.Following[id];
			if (s.State == SalienceState.None && s.Regard.Count == 0 && s.YoungTotal() + s.AdultTotal() < FollowingPruneFloor && s.Salience() < 1e-4f) {
				state.Following.Remove(id);
				followingSinceRow.Remove(id);
			}
		}
	}

	private static string Csv(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace(',', ' ').Replace('"', '\'');

	private static void FollowingRow(string kind, GameDate date, MediaFollowingState s, Genre genre, string value, string detail) {
		if (MediaFollowing.Log == null) return;
		if (!followingHeader) {
			MediaFollowing.Log.WriteLine("kind,date,artistId,artist,genre,young,adult,regard,salience,state,peak,breadth,lastReleaseDay,lastTop40Day,value,detail");
			followingHeader = true;
		}
		float regard = s.Regard.Count == 0 ? 0f : s.Regard.Values.Sum() / Math.Max(1, ChartManager.Instance?.GetAllRegions()?.Count ?? 7);
		MediaFollowing.Log.WriteLine(string.Join(",", kind, date.ToShortString(), s.ArtistId, Csv(s.ArtistName), genre,
			s.YoungTotal().ToString("F0", CultureInfo.InvariantCulture), s.AdultTotal().ToString("F0", CultureInfo.InvariantCulture),
			regard.ToString("F3", CultureInfo.InvariantCulture), s.Salience().ToString("F5", CultureInfo.InvariantCulture), s.State, s.Peak,
			MediaFollowing.Breadth(s), s.LastReleaseDay, s.LastTop40Day, value, detail));
	}
	private static bool followingHeader;

	/// <summary>
	/// Year end: the size distribution of followings against the §9 targets, the year's joiners and losses by kind, the
	/// would-be advance orders, and the attention states. Read-only.
	/// </summary>
	private static void PrintFollowing(int year) {
		if (!MediaFollowing.Active) return;
		double V(string key) => followingLedger.GetValueOrDefault(key);
		var sizes = state.Following.Values.Select(s => (State: s, Young: s.YoungTotal(), Adult: s.AdultTotal())).ToList();
		int Over(float n) => sizes.Count(x => x.Young + x.Adult >= n);
		var top = sizes.OrderByDescending(x => x.Young + x.Adult).ThenBy(x => x.State.ArtistId, StringComparer.Ordinal).Take(5)
			.Select(x => $"\"{x.State.ArtistName}\" {x.Young / 1e3:F0}k/{x.Adult / 1e3:F0}k");
		double people = sizes.Sum(x => x.Young + x.Adult);
		double losses = V("drift") + V("ageOut") - V("ageToAdult") + V("silence") + V("disappointment") + V("switching") + V("controversy");
		Godot.GD.Print($"MEDIA_FOLLOWING year={year} acts={sizes.Count} over10k={Over(1e4f)} over100k={Over(1e5f)} over1M={Over(1e6f)} people={people / 1e6:F2}M" +
			$" young={sizes.Sum(x => x.Young) / 1e6:F2}M joinBuy={V("joinBuy") / 1e6:F2}M joinTv={V("joinTv") / 1e6:F2}M joinPress={V("joinPress") / 1e6:F2}M joinSeries={V("joinSeries") / 1e6:F2}M [offered teen/specialist={V("readOfferedTeenFan") / 1e6:F2}/{V("readOfferedSpecialist") / 1e6:F2}]" +
			$" losses={losses / 1e6:F2}M [drift={V("drift") / 1e6:F2} ageOut={V("ageOut") / 1e6:F2} toAdult={V("ageToAdult") / 1e6:F2} silence={V("silence") / 1e6:F2}" +
			$" disappointment={V("disappointment") / 1e6:F2} switching={V("switching") / 1e6:F2} controversy={V("controversy") / 1e6:F2}]" +
			$" advance[singles/LPs]={V("advanceSingles") / Math.Max(1, V("releasesSingles")) / 1e3:F1}k/{V("advanceAlbums") / Math.Max(1, V("releasesAlbums")) / 1e3:F1}k per release" +
			$" top=[{string.Join(" ", top)}]");
		var states = state.Following.Values.Where(s => s.State != SalienceState.None).ToList();
		Godot.GD.Print($"MEDIA_SALIENCE_YEAR year={year} sensations={states.Count(s => s.State == SalienceState.NationalSensation)} phenomena={states.Count(s => s.State == SalienceState.CulturalPhenomenon)}" +
			$" transitions={followingTransitions.Count} everPhenomenon={state.Following.Values.Count(s => s.Peak == SalienceState.CulturalPhenomenon)}" +
			$" topSalience={yearTopSalience:F4} \"{yearTopName}\" maxLead={yearTopLead:F2}");
		followingLedger.Clear();
		followingTransitions.Clear();
		yearTopLead = 0f; yearTopSalience = 0f; yearTopName = null;
	}
}
