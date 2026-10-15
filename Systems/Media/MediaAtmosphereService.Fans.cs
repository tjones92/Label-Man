using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 3 teen attachment, fan episodes and member spotlight (directive §10), observe-only. Once a week, after the airings
/// are tallied and before the press pass, each act's attachment by region takes the week's evidence: where its records
/// sell near the top, how many of a region's teenagers saw it on television and how it went, and what the teen and local
/// papers ran about it. Episodes move only on persistent, independent evidence. The player's acts and the rivals' go
/// through exactly the same arithmetic. Everything lands in <see cref="MediaSaveData.Fans"/>; nothing outside media reads it.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>Original and syndicated airings tallied in this weekly step; consumed by the fan pass in the same step, never saved.</summary>
	private static readonly List<(MediaAppearance Appearance, MediaAiring Airing)> airedThisWeek = new();
	private const int SalesRankDepth = 20;
	private const int FanHistoryLines = 8;
	private const int FanForgetDays = 7 * 26;

	public static IReadOnlyDictionary<string, MediaFanState> Fans => state.Fans;
	public static MediaFanState FanStateFor(string artistId) => artistId != null && state.Fans.TryGetValue(artistId, out var f) ? f : null;
	public static FanEpisodeState EpisodeOf(string artistId) => FanStateFor(artistId)?.State ?? FanEpisodeState.None;
	public static bool IsNational(FanEpisodeState s) => s is FanEpisodeState.NationalSensation or FanEpisodeState.CulturalPhenomenon;

	private static float Spark(MediaPerformanceResult p) => p?.Outcome switch {
		PerformanceOutcome.Electrifying => 1.6f,
		PerformanceOutcome.Engaging => 1.2f,
		PerformanceOutcome.Weak => 0.35f,
		PerformanceOutcome.Breakdown => 0.1f,
		_ => 0.8f,
	};

	/// <summary>Angles that are evidence of interest in the act rather than a retelling of its chart run (§10.1 provenance).</summary>
	private static bool FanCoverage(StoryAngle angle) =>
		angle is StoryAngle.TeenFeature or StoryAngle.TvNotice or StoryAngle.FanScene or StoryAngle.Hometown or StoryAngle.CulturalPhenomenon or StoryAngle.TvAdvance;

	private static void FansWeek(Evidence evidence, GameDate weekEnd) {
		var regions = evidence.Regions;
		int r = regions.Count;
		if (r == 0) { airedThisWeek.Clear(); return; }
		int today = BroadcastExposure.DayIndex(weekEnd);
		int year = weekEnd.year;
		var index = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < r; i++) index[regions[i].regionId] = i;
		float teenShare = BroadcastExposure.GroupShare(ViewerGroup.Teen, year);
		double[] teenPop = regions.Select(x => Math.Max(1.0, x.population * 1e6 * teenShare)).ToArray();
		float[] Row(Dictionary<string, float[]> map, string id) { if (!map.TryGetValue(id, out var v)) map[id] = v = new float[r]; return v; }

		// Sales response: rank in each region's best sellers this week, every label's singles alike.
		var sales = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var best = new Dictionary<string, RecordRuntimeData>(StringComparer.Ordinal);
		var singles = evidence.ById.Values.Where(x => x.baseRecord.format == ReleaseFormat.Single && !string.IsNullOrEmpty(x.baseRecord.artistId) && x.unitsThisWeek > 0)
			.OrderBy(x => x.baseRecord.recordId, StringComparer.Ordinal).ToList();
		foreach (RecordRuntimeData s in singles) {
			string artist = s.baseRecord.artistId;
			if (!best.TryGetValue(artist, out var cur) || Better(s, cur)) best[artist] = s;
		}
		for (int i = 0; i < r; i++) {
			string region = regions[i].regionId;
			var ranked = singles.Select(s => (Record: s, Units: s.regionalData.TryGetValue(region, out var rd) && rd != null ? rd.unitsSoldThisWeek : 0))
				.Where(x => x.Units > 0).OrderByDescending(x => x.Units).ThenBy(x => x.Record.baseRecord.recordId, StringComparer.Ordinal).Take(SalesRankDepth).ToList();
			for (int k = 0; k < ranked.Count; k++) {
				float[] row = Row(sales, ranked[k].Record.baseRecord.artistId);
				row[i] = Math.Max(row[i], 1f - k / (float)SalesRankDepth);
			}
		}

		// Television: the share of a region's teenagers who saw the act this week, scaled by how it went.
		var tv = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var tvResponse = new HashSet<string>(StringComparer.Ordinal);
		var letters = new Dictionary<string, long>(StringComparer.Ordinal);
		var camera = new Dictionary<string, List<(string Key, double Teens, float Spark)>>(StringComparer.Ordinal);
		foreach (var (a, airing) in airedThisWeek) {
			if (a.Cut || airing.Exposure == null || airing.Exposure.UniqueReach <= 0) continue;
			TvProgram program = MediaOutletCatalog.Get(a.ProgramId);
			if (program == null) continue;
			double sum = airing.Exposure.AverageByGroup.Sum();
			double teens = sum <= 0 ? 0 : airing.Exposure.UniqueReach * airing.Exposure.AverageByGroup[(int)ViewerGroup.Teen] / sum;
			if (teens <= 0) continue;
			float spark = Spark(a.Performance);
			float[] row = Row(tv, a.ArtistId);
			if (airing.CityId != null || program.Network == TvNetwork.Local) {
				string region = DistanceModel.GetCityById(airing.CityId ?? program.HomeCityId)?.parentRegionId;
				if (region != null && index.TryGetValue(region, out int i)) row[i] += (float)(teens / teenPop[i]) * spark;
			} else {
				double weightSum = regions.Sum(x => x.population * BroadcastExposure.Clearance(program.Network, x));
				for (int i = 0; i < r && weightSum > 0; i++)
					row[i] += (float)(teens * regions[i].population * BroadcastExposure.Clearance(program.Network, regions[i]) / weightSum / teenPop[i]) * spark;
			}
			if (!airing.Repeat && !airing.Syndicated) {
				long mail = (long)Math.Round(teens / 20_000.0 * spark * (0.7 + 0.6 * MediaAtmosphere.Unit("fanmail|" + a.Id)));
				letters[a.ArtistId] = letters.GetValueOrDefault(a.ArtistId) + mail;
				if (spark >= 1.2f && mail >= 25) tvResponse.Add(a.ArtistId);
			}
			if (!camera.TryGetValue(a.ArtistId, out var shots)) camera[a.ArtistId] = shots = new();
			shots.Add((a.RepeatOfId ?? a.Id, teens, spark));
		}
		airedThisWeek.Clear();

		// Coverage: distinct outlets that ran an item about the act this week, nationally (teen and mainstream) or in a region.
		var press = new Dictionary<string, float[]>(StringComparer.Ordinal);
		var covered = new HashSet<string>(StringComparer.Ordinal);
		var nationalExposure = new HashSet<string>(StringComparer.Ordinal);
		foreach (MediaStory s in state.Stories) {
			if (s.Status != StoryStatus.Scheduled || s.ArtistId == null || s.IssueDay > today) continue;
			PressOutlet outlet = MediaOutletCatalog.GetPress(s.OutletId);
			if (outlet == null) continue;
			if (outlet.Family == PressFamily.TeenFan && s.IssueDay > today - 28) nationalExposure.Add(s.ArtistId);
			if (s.IssueDay <= today - 7 || !FanCoverage(s.Angle)) continue;
			covered.Add(s.ArtistId);
			float[] row = Row(press, s.ArtistId);
			if (outlet.National && outlet.Family is PressFamily.TeenFan or PressFamily.Mainstream) {
				for (int i = 0; i < r; i++) row[i] = Math.Min(2f, row[i] + 1f);
			} else if (outlet.HomeCityId != null && DistanceModel.GetCityById(outlet.HomeCityId)?.parentRegionId is string region && index.TryGetValue(region, out int i)) {
				row[i] = Math.Min(2f, row[i] + 0.5f);
			}
		}
		foreach (MediaAppearance a in state.Appearances) {
			if (a.RepeatOfId != null || a.Cut) continue;
			int day = BroadcastExposure.DayIndex(a.Date);
			if (day > today || day <= today - 28) continue;
			if (MediaOutletCatalog.Get(a.ProgramId)?.Network is TvNetwork tn && tn != TvNetwork.Local) nationalExposure.Add(a.ArtistId);
		}

		var acts = new SortedSet<string>(StringComparer.Ordinal);
		acts.UnionWith(state.Fans.Keys); acts.UnionWith(tv.Keys); acts.UnionWith(press.Keys);
		// Selling alone opens a record only where it sells near the top: a fan record isn't kept for every single in the shops.
		foreach (var pair in sales) if (pair.Value.Any(v => v >= 0.5f)) acts.Add(pair.Key);

		foreach (string id in acts) {
			sales.TryGetValue(id, out float[] sv);
			tv.TryGetValue(id, out float[] tvv);
			press.TryGetValue(id, out float[] pv);
			bool active = sv != null || tvv != null || pv != null;
			if (!state.Fans.TryGetValue(id, out MediaFanState fs)) {
				if (!active) continue;
				best.TryGetValue(id, out var br);
				state.Fans[id] = fs = new MediaFanState { ArtistId = id, ArtistName = br?.baseRecord.artistName ?? ArtistManager.Instance?.GetArtist(id)?.stageName, LastActiveDay = today };
			}
			best.TryGetValue(id, out RecordRuntimeData record);
			if (record != null) fs.ArtistName = record.baseRecord.artistName;
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(id);
			Genre genre = record?.baseRecord.primaryGenre ?? artist?.primaryGenre ?? Genre.TraditionalPop;
			float youth = 0.3f + 0.7f * MediaOutletCatalog.TeenFit(genre);
			bool selling = sv != null && sv.Any(v => v >= 0.25f);

			for (int i = 0; i < r; i++) {
				string region = regions[i].regionId;
				float a = fs.Attachment.TryGetValue(region, out float v) ? v * MediaFans.WeeklyRetention : 0f;
				// Repeated appearances without a compelling record make familiarity, not attachment (§10.1).
				float gain = youth * (0.06f * (sv?[i] ?? 0f) + 0.4f * (tvv?[i] ?? 0f) * (selling ? 1f : 0.3f) + 0.04f * (pv?[i] ?? 0f));
				a = MediaFans.Deposit(a, gain);
				if (a >= 0.01f) fs.Attachment[region] = a; else fs.Attachment.Remove(region);
			}

			// Member spotlight: decays, then the week's airings and their keyed camera shares deposit (§10.2).
			foreach (string key in fs.Spotlight.Keys.ToList()) {
				float s = fs.Spotlight[key] * MediaFans.SpotlightRetention;
				if (s < 0.01f) fs.Spotlight.Remove(key); else fs.Spotlight[key] = s;
			}
			if (camera.TryGetValue(id, out var shotList) && artist != null && artist.members.Count(m => m.isActive) > 1)
				foreach (var (key, teens, spark) in shotList) SpotlightDeposit(fs, artist, "tv|" + key, (float)(0.12 * Math.Min(1.0, teens / 3e6) * spark / 1.2), year);
			if (letters.TryGetValue(id, out long mail)) fs.Letters += mail;

			var week = new FanWeek {
				Day = today,
				Families = (sv != null && sv.Any(x => x >= 0.5f) ? FanEvidence.Sales : 0) | (tvResponse.Contains(id) ? FanEvidence.TvResponse : 0) | (covered.Contains(id) ? FanEvidence.Coverage : 0),
				WarmRegions = fs.Attachment.Values.Count(x => x >= MediaFans.WarmAt),
				HotRegions = fs.Attachment.Values.Count(x => x >= MediaFans.HotAt),
				SellingRegions = sv?.Count(x => x >= 0.5f) ?? 0,
				NationalExposure = nationalExposure.Contains(id),
				Extraordinary = record != null && (record.currentPosition is >= 1 and <= 3 && evidence.Breadth(record) >= 0.6f || Precursor(record, evidence) >= 0.5f),
			};
			if (MediaFans.Advance(fs, week, r)) {
				string line = $"{FromDayIndex(today).ToShortString()}  {MediaFans.Describe(fs.State)}" +
					(fs.State == FanEpisodeState.Cooldown ? $" (was {MediaFans.Describe(fs.TransitionFrom)})" : "");
				fs.History.Insert(0, line);
				while (fs.History.Count > FanHistoryLines) fs.History.RemoveAt(fs.History.Count - 1);
				if (IsNational(fs.State) || IsNational(fs.TransitionFrom))
					Godot.GD.Print($"MEDIA_FANS date={FromDayIndex(today).ToShortString()} act=\"{fs.ArtistName}\" {fs.TransitionFrom}->{fs.State} hot={week.HotRegions}/{r} " +
						$"families={week.Families} letters={fs.Letters} face=\"{MediaFans.Face(fs)}\"");
			}
			if (record != null && record.baseRecord.recordId != fs.BestRecordId) { fs.BestRecordId = record.baseRecord.recordId; fs.BestRecordSinceDay = today; fs.Plateau = false; }
			if (IsNational(fs.State) && !fs.Plateau && today - Math.Max(fs.StateSinceDay, fs.BestRecordSinceDay) >= MediaFans.PlateauDays) {
				fs.Plateau = true;
				fs.History.Insert(0, $"{FromDayIndex(today).ToShortString()}  the same record for months: editors want a new angle");
				while (fs.History.Count > FanHistoryLines) fs.History.RemoveAt(fs.History.Count - 1);
			}
			if (active) fs.LastActiveDay = today;
		}

		// Forget acts nobody has responded to for half a year, once nothing is left to show.
		foreach (string id in state.Fans.Keys.ToList()) {
			MediaFanState fs = state.Fans[id];
			if (fs.State is FanEpisodeState.None && fs.Attachment.Count == 0 && fs.Spotlight.Count == 0 && today - fs.LastActiveDay > 28
				|| today - fs.LastActiveDay > FanForgetDays && fs.State != FanEpisodeState.Cooldown && fs.Attachment.Count == 0)
				state.Fans.Remove(id);
		}

		static bool Better(RecordRuntimeData a, RecordRuntimeData b) {
			int pa = a.currentPosition > 0 ? a.currentPosition : 999, pb = b.currentPosition > 0 ? b.currentPosition : 999;
			return pa != pb ? pa < pb : a.unitsThisWeek != b.unitsThisWeek ? a.unitsThisWeek > b.unitsThisWeek
				: string.CompareOrdinal(a.baseRecord.recordId, b.baseRecord.recordId) < 0;
		}
	}

	/// <summary>One exposure's worth of spotlight split across the members the camera (or the photographer) found.</summary>
	private static void SpotlightDeposit(MediaFanState fs, SimulatedArtist artist, string key, float gain, int year) {
		if (gain <= 0f) return;
		foreach (var (member, share) in MediaFans.CameraShares(artist, key, year)) {
			float s = fs.Spotlight.GetValueOrDefault(member.personId);
			fs.Spotlight[member.personId] = Math.Clamp(s + Math.Min(0.15f, gain * share * 2f) * (1f - s), 0f, 1f);
			fs.Faces[member.personId] = member.FullName;
		}
	}

	/// <summary>A photo session puts one member in front of the readers: weighted by the same camera shares, keyed per session.</summary>
	private static string PhotographMember(MediaFanState fs, SimulatedArtist artist, string key, int year) {
		var shares = MediaFans.CameraShares(artist, "photo|" + key, year);
		if (shares.Count < 2) return null;
		float u = MediaAtmosphere.Unit("photo-pick|" + key), acc = 0f;
		foreach (var (member, share) in shares) {
			acc += share;
			if (u >= acc) continue;
			float s = fs.Spotlight.GetValueOrDefault(member.personId);
			fs.Spotlight[member.personId] = Math.Clamp(s + 0.06f * (1f - s), 0f, 1f);
			fs.Faces[member.personId] = member.FullName;
			return member.FullName;
		}
		return null;
	}

	internal static MediaFanState EnsureFanState(string artistId, string name) {
		if (artistId == null) return null;
		if (!state.Fans.TryGetValue(artistId, out var fs)) state.Fans[artistId] = fs = new MediaFanState { ArtistId = artistId, ArtistName = name };
		return fs;
	}

	// ---- fan stories ---------------------------------------------------------------------------------------------------

	/// <summary>
	/// The world's response to an episode changing state (§10.2 visible world response): a local paper notices the crowd at
	/// a hop, the teen magazines compete for a sensation, the mainstream magazines assign a writer to a phenomenon, and an
	/// act cooling off gets a "whatever happened to" item. Every fact is public: sales reports, the mail the program
	/// received, what other papers ran.
	/// </summary>
	private static void FanLeads(List<MediaPress.Lead> leads, Evidence evidence, int today) {
		foreach (MediaFanState fs in state.Fans.Values.OrderBy(f => f.ArtistId, StringComparer.Ordinal)) {
			if (fs.TransitionDay != today - 1) continue;
			RecordRuntimeData record = fs.BestRecordId != null && evidence.ById.TryGetValue(fs.BestRecordId, out var br) ? br : null;
			FanEpisodeState s = fs.State;
			if (s == FanEpisodeState.LocalExcitement && fs.TransitionFrom != FanEpisodeState.None) continue; // stepping down quietly
			float interest = s switch {
				FanEpisodeState.LocalExcitement => 0.35f,
				FanEpisodeState.RegionalFavorite => 0.45f,
				FanEpisodeState.NationalSensation => 0.8f,
				FanEpisodeState.CulturalPhenomenon => 1.0f,
				FanEpisodeState.Cooldown => 0.35f,
				_ => 0f,
			};
			if (interest <= 0f) continue;
			var lead = NewLead("fans|" + fs.ArtistId + "|" + s + "|" + today, s == FanEpisodeState.CulturalPhenomenon ? StoryAngle.CulturalPhenomenon : StoryAngle.FanScene,
				record, today, interest, evidence);
			lead.Story.ArtistId = fs.ArtistId; lead.Story.ArtistName = fs.ArtistName; lead.Story.FanState = s; lead.Story.Face = MediaFans.Face(fs);
			lead.Story.CityId ??= evidence.CityOf(fs.ArtistId);
			float tone = MediaAtmosphere.Unit("fans-tone|" + lead.Story.Id);
			lead.Story.Tone = s == FanEpisodeState.Cooldown ? StoryTone.Skeptical
				: s == FanEpisodeState.CulturalPhenomenon ? (tone < 0.4f ? StoryTone.Curious : tone < 0.7f ? StoryTone.Admiring : StoryTone.Alarmed)
				: StoryTone.Admiring;
			int warm = fs.Attachment.Values.Count(v => v >= MediaFans.WarmAt);
			lead.Story.Regions = warm;
			Fact(lead, EvidenceSource.RegionalSales, warm > 1 ? $"selling near the top in {warm} regional markets" : "selling near the top of the local reports", today);
			if (fs.Letters > 0) Fact(lead, EvidenceSource.BroadcastAiring, $"about {RoundLetters(fs.Letters):N0} letters to the television programs it has played", today, firsthand: false, confidence: 0.7f);
			int families = MediaPress.FamiliesCovering(state, fs.ArtistId, today - 42, today);
			if (families > 0) Fact(lead, EvidenceSource.OtherPublication, $"written up in {families} kind(s) of paper in the last six weeks", today, firsthand: false);
			switch (s) {
				case FanEpisodeState.LocalExcitement:
				case FanEpisodeState.RegionalFavorite:
					if (DailyFor(lead.Story.CityId) is string daily) lead.Outlets.Add(daily);
					foreach (var pair in fs.Attachment.Where(p => p.Value >= MediaFans.WarmAt).OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Take(3))
						lead.Outlets.AddRange(RegionPapers(pair.Key, specialistOnly: true));
					if (s == FanEpisodeState.RegionalFavorite) lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
					break;
				case FanEpisodeState.NationalSensation:
					lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
					lead.Outlets.AddRange(Outlets(PressFamily.Trade));
					if (DailyFor(lead.Story.CityId) is string d2) lead.Outlets.Add(d2);
					break;
				case FanEpisodeState.CulturalPhenomenon:
					lead.Outlets.AddRange(Outlets(PressFamily.Mainstream));
					lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
					lead.Outlets.AddRange(Outlets(PressFamily.Trade));
					break;
				case FanEpisodeState.Cooldown:
					lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
					break;
			}
			leads.Add(lead);
		}
	}

	public static long RoundLetters(long n) => n < 100 ? n : n < 1000 ? n / 10 * 10 : n / 100 * 100;

	// ---- read-only views --------------------------------------------------------------------------------------------

	/// <summary>The acts the country's fans are loudest about right now, biggest episode first.</summary>
	public static List<MediaFanState> LoudestFans(int count) =>
		state.Fans.Values.Where(f => f.State == FanEpisodeState.Cooldown ? IsNational(f.Peak) : f.State != FanEpisodeState.None)
			.OrderByDescending(f => f.State == FanEpisodeState.Cooldown ? 0 : (int)f.State)
			.ThenByDescending(f => f.Attachment.Values.Sum()).ThenBy(f => f.ArtistId, StringComparer.Ordinal).Take(count).ToList();

	/// <summary>Where an act is warmest, by region name, strongest first.</summary>
	public static List<string> WarmRegionNames(MediaFanState fs, int count) {
		var regions = ChartManager.Instance?.GetAllRegions();
		return fs == null ? new() : fs.Attachment.Where(p => p.Value >= MediaFans.WarmAt).OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Take(count)
			.Select(p => regions?.FirstOrDefault(x => x.regionId == p.Key)?.regionName ?? p.Key).ToList();
	}
}
