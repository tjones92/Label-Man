using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One dated airing of an appearance in a set of markets: the original broadcast, a repeat, or a syndicated tape.</summary>
public sealed class MediaAiring {
	public GameDate Date;
	/// <summary>The local start in the player's clock for a single-city airing; for a network airing, the Eastern origin.</summary>
	public int StartMinute;
	public string CityId;            // null for a network airing
	public bool Repeat;
	public bool Syndicated;
	public ExposureEstimate Exposure;
	/// <summary>Set once the airing's date has passed and its audience has entered the act's tally. Receipts make it once-only.</summary>
	public bool Tallied;
}

/// <summary>An act booked into one episode's slot (§5.1 "confirmed"). Observe mode books only rival acts.</summary>
public sealed class MediaAppearance {
	public string Id, ProgramId, EpisodeId;
	public GameDate Date;
	public int SlotIndex, SlotCount;
	public string ArtistId, ArtistName, RecordId, Title, LabelId;
	public Genre Genre;
	public int ChartPosition;          // frozen evidence at booking; 0 = off the national chart
	public bool Discovery;
	public string RepeatOfId;          // a repeat replays this appearance with reduced novelty
	public PerformanceMethod Method;
	public BroadcastMethod Broadcast;
	public List<MediaAiring> Airings = new();
	/// <summary>Resolved at booking from the performers' own skills and the rehearsal arranged; shown only after air (§4.5).</summary>
	public MediaPerformanceResult Performance;
	public MediaPreparation Preparation = MediaPreparation.Standard;
	public string StandardsQueryId;
	/// <summary>The producer or network removed the segment: nobody saw it (§6, §7.3).</summary>
	public bool Cut;
	/// <summary>Set when the act sang a different song than the record booked (an alternate offered to standards).</summary>
	public string OriginalTitle;
	/// <summary>The part of the bill the act filled, and what it sang. <see cref="RecordId"/> is the label campaign the date
	/// was charged to; only <see cref="PerformedSong.Record"/> means the song on the air was that record.</summary>
	public BillLane Lane;
	public PerformedSong Song;
	public bool PlayerBooking;
	/// <summary>The advertised headliner of the episode (an announced hold), and the hype it carried into the air date.</summary>
	public bool Headliner;
	public float Hype, EventLift, TuneIn;
	/// <summary>The player's office has been told how it went (once-only receipt).</summary>
	public bool Reported;
}

/// <summary>
/// What a guest sang (SimTools/MediaTvHeadlinerBookingDesign.md). On 1960-64 Sullivan bills about 80% of songs were
/// standards, show tunes and album material, 14% other acts' current hits, and 7% the singer's own hits.
/// </summary>
public enum PerformedSong {
	/// <summary>The booked record itself.</summary>
	Record,
	/// <summary>The act's own best-known earlier hit.</summary>
	OwnHit,
	/// <summary>Another act's current hit.</summary>
	CurrentCover,
	/// <summary>A standard or show tune from the songbook.</summary>
	Standard,
	/// <summary>A comedian's routine.</summary>
	Routine,
}

public sealed class MediaEpisode {
	public string Id, ProgramId;
	public GameDate Date;
	public bool Repeat;
	public List<string> AppearanceIds = new();
}

/// <summary>Compact per-act media history (§2 coverage ledger). Media-owned; no economic system reads it.</summary>
public sealed class MediaActTally {
	public string ArtistId, ArtistName;
	public int Appearances, NationalAppearances, LocalAppearances;
	public double ViewerMinutes, EstimatedReach, TeenImpressions;
	public string LastProgramId;
	public GameDate LastDate = GameDate.StartDate;
	/// <summary>Programs this act has aired on: a second visit to the same show reaches mostly the same regular viewers.</summary>
	public List<string> ProgramsSeen = new();
	public int PressItems;
	public double PeakEpisodeAudience;
	public string BestNight;
}

public sealed class MediaSaveData {
	public int ConfigVersion { get; set; }
	public int BookedThroughDay { get; set; } = -1;
	public List<MediaEpisode> Episodes { get; set; } = new();
	public List<MediaAppearance> Appearances { get; set; } = new();
	public Dictionary<string, MediaActTally> Tallies { get; set; } = new();
	// Phase 2 (all media-owned; nothing the economy reads).
	public List<StandardsQuery> StandardsQueries { get; set; } = new();
	public List<MediaStory> Stories { get; set; } = new();
	public Dictionary<string, MediaProgramMemory> ProgramMemory { get; set; } = new();
	public Dictionary<string, MediaContactMemory> Contacts { get; set; } = new();
	/// <summary>Audience attitude per act: [artistic, family, teen], small and signed, drifting 10% a week toward neutral (§2).</summary>
	public Dictionary<string, float[]> Credibility { get; set; } = new();
	public List<MediaControversy> Controversies { get; set; } = new();
	public List<MediaHold> Holds { get; set; } = new();
	/// <summary>Weeks each current record has spent at No. 1 (media's own count; the chart keeps no such field).</summary>
	public Dictionary<string, int> WeeksAtNumberOne { get; set; } = new();
	/// <summary>recordId|regionId pairs already reported as breaking, so a breakout is news once.</summary>
	public HashSet<string> BreakoutsReported { get; set; } = new();
	public List<MediaPitch> Pitches { get; set; } = new();
	public List<MediaInquiry> Inquiries { get; set; } = new();
	public List<MediaExclusive> Exclusives { get; set; } = new();
	public int PlayerSequence { get; set; }
	/// <summary>Running average of a No. 1 record's weekly units: the yardstick for "bigger than an ordinary No. 1".</summary>
	public float NumberOneUnits { get; set; }
	// Phase 3 (media-owned; nothing the economy reads).
	/// <summary>Teen attachment, fan episodes and member spotlight by act (§2, §10).</summary>
	public Dictionary<string, MediaFanState> Fans { get; set; } = new();
	/// <summary>Regional audience 3f: each act's smoothed share of a region's single sales, by artistId then regionId.</summary>
	public Dictionary<string, Dictionary<string, float>> SalesStanding { get; set; } = new();
	/// <summary>Regional audience 3f: the regions where each act is latched as a top seller.</summary>
	public Dictionary<string, HashSet<string>> TopSellerIn { get; set; } = new();
	/// <summary>AI campaign allocation views by record (§12.2): estimates of an envelope already charged, with reservation receipts.</summary>
	public Dictionary<string, AiMediaAllocation> AiAllocations { get; set; } = new();
	public List<AiMediaPitch> AiPitches { get; set; } = new();
	/// <summary>This year's AI planner counters (policies, pitches, acceptances, declines by reason), printed and cleared at year end.</summary>
	public Dictionary<string, int> AiLedger { get; set; } = new();
}

/// <summary>
/// The television half of Phase 1 (directive §3-§4, observe-only). Once a week, after the completed-week settlement,
/// producers book next week's episodes from the chart that just froze; each booking carries its computed exposure,
/// which stays hidden until the air date passes. Rival acts only: a player act enters through a negotiation (Phase 2).
/// <para>
/// Writes nothing outside this class, draws only keyed <see cref="MediaAtmosphere.Unit"/> values, and is called as an
/// explicit step of <c>ChartManager.OnWeekEnded</c>, never as an event subscriber (§15.1).
/// </para>
/// </summary>
public static partial class MediaAtmosphereService {
	private const int HistoryWeeks = 26;
	private const int MaxNationalPerActPerWeek = 2;
	/// <summary>Weeks before a name booked on standing returns to the same primetime variety hour. Of 823 Sullivan returns
	/// 1959-70, 81% came more than 13 weeks later; the four in consecutive weeks were British Invasion package deals.</summary>
	private const int StandingReturnWeeks = 13;
	private const float RepeatNovelty = 0.55f;
	/// <summary>Share of a returning program's audience that is new to the act (initial design value; regulars dominate).</summary>
	private const double ReturnVisitNewShare = 0.25;
	private const int LocalCandidatesPerRegion = 40;

	private static MediaSaveData state = new();
	// Booking-pass indexes, rebuilt from state at the start of each pass (never saved).
	private static Dictionary<string, List<MediaAppearance>> byArtist;
	private static HashSet<string> episodeIds;

	public static IReadOnlyList<MediaEpisode> Episodes => state.Episodes;
	public static IReadOnlyList<MediaAppearance> Appearances => state.Appearances;
	public static IReadOnlyDictionary<string, MediaActTally> Tallies => state.Tallies;
	public static MediaActTally TallyFor(string artistId) => artistId != null && state.Tallies.TryGetValue(artistId, out var t) ? t : null;

	// ---- the weekly step -------------------------------------------------------------------------------------

	/// <summary>Start of game: book the opening days from the prewarmed chart so the first paper has listings.</summary>
	public static void Initialize(GameDate start, ChartManager charts) {
		if (!MediaAtmosphere.Observing || charts == null) return;
		if (state.BookedThroughDay >= 0) return;
		var evidence = new Evidence(charts);
		SeedOpeningAttachment(evidence, start);
		BookThrough(start.AddDays(start.DaysUntilFriday), start, evidence);
	}

	/// <summary>Called by ChartManager after the completed week's settlement. Tallies the airings that ran this week,
	/// then books Saturday through Friday from the chart that just froze. A late airing after this cutoff waits for next week.</summary>
	public static void AfterCompletedWeek(GameDate weekEnd, ChartManager charts) {
		if (!MediaAtmosphere.Observing || charts == null) return;
		long started = System.Diagnostics.Stopwatch.GetTimestamp();
		labelIndex = null;
		TallyAiredThrough(weekEnd);
		var evidence = new Evidence(charts);
		// The order is the contract (§15.1): what aired is tallied, the frozen chart is read, the fans respond to the
		// completed week, rival labels plan, headline dates are announced, the press writes about all of it, and only then
		// is next week booked, so a booking's hype counts only items already scheduled to run before its air date.
		UpdateChartMemory(evidence);
		FansWeek(evidence, weekEnd);
		AiPlanWeek(evidence, weekEnd);
		PlaceHolds(evidence, weekEnd);
		PressWeek(evidence, weekEnd);
		BookThrough(weekEnd.AddDays(7), weekEnd.AddDays(1), evidence);
		DecayAttitudes(weekEnd);
		Prune(weekEnd);
		elapsedTicks += System.Diagnostics.Stopwatch.GetTimestamp() - started;
		if (weekEnd.month == 12 && weekEnd.day >= 25) PrintYear(weekEnd.year);
	}

	private static long elapsedTicks;

	private static void PrintYear(int year) {
		var mine = state.Appearances.Where(a => a.Date.year == year && a.RepeatOfId == null).ToList();
		var top = state.Tallies.Values.OrderByDescending(t => t.EstimatedReach).FirstOrDefault();
		Godot.GD.Print($"MEDIA_YEAR year={year} appearances(last26w)={mine.Count} national={mine.Count(a => MediaOutletCatalog.Get(a.ProgramId)?.Network != TvNetwork.Local)} " +
			$"acts={state.Tallies.Count} topAct=\"{top?.ArtistName}\" topReachM={(top?.EstimatedReach ?? 0) / 1e6:F1} weeklyStepMs={elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency:F0}");
		// Phase 2 texture: what the press ran, what standards asked, and the biggest single night of the year.
		var items = state.Stories.Where(s => s.Status == StoryStatus.Scheduled && s.IssueDay >= BroadcastExposure.DayIndex(new GameDate(year, 1, 1))).ToList();
		var byFamily = items.GroupBy(s => MediaOutletCatalog.GetPress(s.OutletId)?.Family.ToString() ?? "?").OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count());
		var queries = state.StandardsQueries.Where(q => q.AirDate.year == year).ToList();
		var bestNight = mine.Where(a => a.Airings.Count > 0).OrderByDescending(a => a.Airings[0].Exposure.UniqueReach).FirstOrDefault();
		Godot.GD.Print($"MEDIA_PRESS year={year} items(last26w)={items.Count} [{string.Join(" ", byFamily)}] standards={queries.Count} " +
			$"[{string.Join(" ", queries.GroupBy(q => q.Resolution).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count()))}] " +
			$"controversies={state.Controversies.Count(c => FromDayIndex(c.StartDay).year == year)} " +
			$"outcomes=[{string.Join(" ", mine.Where(a => a.Performance != null).GroupBy(a => a.Performance.Outcome).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count()))}] " +
			$"nationalLive=[{string.Join(" ", mine.Where(a => a.Performance != null && a.Broadcast == BroadcastMethod.Live && a.Method != PerformanceMethod.Mime && MediaOutletCatalog.Get(a.ProgramId)?.BooksNationally == true).GroupBy(a => a.Performance.Outcome).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count()))}] " +
			$"bestNight=\"{bestNight?.ArtistName}\" {MediaOutletCatalog.Get(bestNight?.ProgramId ?? "")?.Name} {(bestNight?.Airings[0].Exposure.UniqueReach ?? 0) / 1e6:F1}M hype={bestNight?.Hype ?? 0:F2}");
		// Phase 3: where the fans are, and what rival labels did with the envelopes they already had.
		var fans = state.Fans.Values.ToList();
		var loud = LoudestFans(1).FirstOrDefault();
		Godot.GD.Print($"MEDIA_FANS_YEAR year={year} acts={fans.Count} [{string.Join(" ", fans.GroupBy(f => f.State).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count()))}] " +
			$"everNational={fans.Count(f => IsNational(f.Peak))} everPhenomenon={fans.Count(f => f.Peak == FanEpisodeState.CulturalPhenomenon)} " +
			$"loudest=\"{loud?.ArtistName}\" {loud?.State} face=\"{MediaFans.Face(loud)}\"");
		// Who the national bookers leaned on this year, by lane: a check on a few names crowding every bill.
		var busiest = mine.Where(a => MediaOutletCatalog.Get(a.ProgramId)?.BooksNationally == true).GroupBy(a => a.ArtistId)
			.OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(6)
			.Select(g => $"\"{g.First().ArtistName}\" {g.Count()} [{string.Join(" ", g.GroupBy(a => a.Lane).OrderBy(x => x.Key).Select(x => x.Key + ":" + x.Count()))}]");
		Godot.GD.Print($"MEDIA_BUSIEST year={year} {string.Join(" ", busiest)}");
		var allocations = state.AiAllocations.Values.ToList();
		Godot.GD.Print($"MEDIA_AI year={year} allocations={allocations.Count} balanced={allocations.All(a => a.Balanced)} " +
			$"tvReserved=${allocations.Sum(a => a.TvReservedCents) / 100.0:N0} of ${allocations.Sum(a => a.TvCents) / 100.0:N0} " +
			$"[{string.Join(" ", state.AiLedger.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + ":" + p.Value))}]");
		state.AiLedger.Clear();
	}

	private static void BookThrough(GameDate last, GameDate firstCandidate, Evidence evidence) {
		int firstDay = Math.Max(state.BookedThroughDay + 1, BroadcastExposure.DayIndex(firstCandidate));
		int lastDay = BroadcastExposure.DayIndex(last);
		if (lastDay < firstDay) return;
		byArtist = state.Appearances.GroupBy(a => a.ArtistId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
		episodeIds = new HashSet<string>(state.Episodes.Select(e => e.Id), StringComparer.Ordinal);
		pitchedTv = new HashSet<string>(state.AiPitches.Where(p => p.Kind == PitchKind.Television && p.Status == PitchStatus.Sent).Select(p => p.RecordId + "|" + p.ProgramId), StringComparer.Ordinal);
		for (int day = firstDay; day <= lastDay; day++) {
			GameDate date = FromDayIndex(day);
			foreach (TvProgram program in MediaOutletCatalog.Programs) {
				BroadcastScheduleVersion version = program.VersionOn(date);
				if (version != null) BookEpisode(program, version, date, evidence);
			}
		}
		state.BookedThroughDay = lastDay;
		state.ConfigVersion = MediaAtmosphere.ConfigVersion;
	}

	// ---- evidence frozen at the settlement -------------------------------------------------------------------

	/// <summary>What bookers can see this week: the national chart and each region's best-selling singles. Built once per
	/// booking pass from read-only accessors; nothing here is written back.</summary>
	internal sealed class Evidence {
		public readonly List<RecordRuntimeData> Chart;
		/// <summary>The full national chart, player records included: the press reports on everyone (§9.4).</summary>
		public readonly List<RecordRuntimeData> FullChart;
		public readonly Dictionary<string, List<(RecordRuntimeData Record, int Units)>> RegionalBest = new(StringComparer.Ordinal);
		public readonly IReadOnlyList<MarketRegion> Regions;
		public readonly float BreakoutBar;
		public readonly Dictionary<string, RecordRuntimeData> ById = new(StringComparer.Ordinal);
		public readonly List<RecordRuntimeData> Singles;
		private readonly Dictionary<string, string> artistCity = new(StringComparer.Ordinal);

		public Evidence(ChartManager charts) {
			FullChart = charts.GetCurrentChart().Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single).ToList();
			Chart = FullChart.Where(Eligible).ToList();
			Regions = charts.GetAllRegions();
			BreakoutBar = CompetitorManager.Instance?.RegionalBreakoutDealThreshold ?? 0.20f;
			all = charts.GetAllRecords();
			foreach (RecordRuntimeData r in all)
				if (r?.baseRecord?.recordId != null) ById[r.baseRecord.recordId] = r;
			var singles = all.Where(Eligible).ToList();
			Singles = singles;
			foreach (MarketRegion region in Regions) {
				RegionalBest[region.regionId] = singles
					.Select(r => (Record: r, Units: r.regionalData.TryGetValue(region.regionId, out var rd) ? rd.unitsSoldThisWeek : 0))
					.Where(x => x.Units > 0)
					.OrderByDescending(x => x.Units).ThenBy(x => x.Record.baseRecord.recordId, StringComparer.Ordinal)
					.Take(LocalCandidatesPerRegion).ToList();
			}
		}

		private static bool Eligible(RecordRuntimeData r) =>
			r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single && !r.baseRecord.isPlayerOwned && !string.IsNullOrEmpty(r.baseRecord.artistId);

		public string CityOf(string artistId) {
			if (artistCity.TryGetValue(artistId, out string city)) return city;
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(artistId);
			string place = artist?.geography?.basePlaceId ?? artist?.geography?.originPlaceId;
			city = ScenePlaceRegistry.Get(place)?.PlayableCityId;
			artistCity[artistId] = city;
			return city;
		}

		private readonly Dictionary<string, List<(RecordRuntimeData Record, int Units)>> regionalLocals = new(StringComparer.Ordinal);

		/// <summary>Records by acts based in a region that sell there at all, best first: the town's own acts a local producer
		/// can call on when the visiting names can't come. Built once per region per pass.</summary>
		public List<(RecordRuntimeData Record, int Units)> RegionalLocals(string regionId) {
			if (regionalLocals.TryGetValue(regionId, out var list)) return list;
			// Each single's home region is found once per pass, not once per region asked about.
			if (singleRegions == null) {
				singleRegions = new string[Singles.Count];
				for (int i = 0; i < Singles.Count; i++)
					singleRegions[i] = DistanceModel.GetCityById(CityOf(Singles[i].baseRecord.artistId) ?? "")?.parentRegionId;
			}
			list = Singles.Where((r, i) => singleRegions[i] == regionId)
				.Select(r => (Record: r, Units: r.regionalData.TryGetValue(regionId, out var rd) && rd != null ? rd.unitsSoldThisWeek : 0))
				.Where(x => x.Units > 0).OrderByDescending(x => x.Units).ThenBy(x => x.Record.baseRecord.recordId, StringComparer.Ordinal)
				.Take(LocalCandidatesPerRegion / 2).ToList();
			regionalLocals[regionId] = list;
			return list;
		}

		private string[] singleRegions;
		// Peaks are frozen with the settlement for the whole pass; programs ask about the same records day after day.
		private readonly Dictionary<RecordRuntimeData, bool> broke = new();
		public bool Broke(RecordRuntimeData r) {
			if (broke.TryGetValue(r, out bool result)) return result;
			result = r.regionalData.Values.Any(rd => rd != null && rd.peakBreakoutScore >= BreakoutBar);
			broke.Add(r, result);
			return result;
		}
		public int BrokeCount(RecordRuntimeData r) => r.regionalData.Values.Count(rd => rd != null && rd.peakBreakoutScore >= BreakoutBar);
		public float Breadth(RecordRuntimeData r) => Regions.Count == 0 ? 0f : BrokeCount(r) / (float)Regions.Count;

		private readonly List<RecordRuntimeData> all;
		private List<StandingAct> standing;
		private HashSet<string> top40Artists;

		/// <summary>Acts on this week's national top 40: a current hit is the chart lane's business.</summary>
		public bool OnTop40(string artistId) {
			top40Artists ??= new HashSet<string>(Chart.Where(r => r.currentPosition is > 0 and <= 40).Select(r => r.baseRecord.artistId), StringComparer.Ordinal);
			return top40Artists.Contains(artistId);
		}

		/// <summary>
		/// The recording acts a booker can call on their names (§5.2 standing fame): signed rival acts with some public
		/// recognition and a live record for the label to charge the date to. Built once per pass, in artist-id order;
		/// reads recognition, chart history and the opening careers' public history, writes nothing.
		/// </summary>
		public List<StandingAct> StandingActs() {
			if (standing != null) return standing;
			var latest = new Dictionary<string, RecordRuntimeData>(StringComparer.Ordinal);
			var lastHit = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (RecordRuntimeData r in all) {
				Record rec = r?.baseRecord;
				if (rec == null || rec.isPlayerOwned || string.IsNullOrEmpty(rec.artistId)) continue;
				if (!latest.TryGetValue(rec.artistId, out RecordRuntimeData cur) || rec.releaseDate > cur.baseRecord.releaseDate
					|| rec.releaseDate == cur.baseRecord.releaseDate && string.CompareOrdinal(rec.recordId, cur.baseRecord.recordId) > 0)
					latest[rec.artistId] = r;
				if (r.peakPosition is > 0 and <= 40) lastHit[rec.artistId] = Math.Max(lastHit.GetValueOrDefault(rec.artistId), rec.releaseDate.year);
			}
			standing = new List<StandingAct>();
			foreach (var (id, vehicle) in latest.OrderBy(p => p.Key, StringComparer.Ordinal)) {
				SimulatedArtist artist = ArtistManager.Instance?.GetArtist(id);
				if (artist == null || !artist.isActive || artist.isPlayerOwned || string.IsNullOrEmpty(artist.labelId)) continue;
				float recognition = ArtistRecognitionService.EffectiveRecognition(artist);
				if (recognition < StandingFloorAny) continue;
				int hit = lastHit.GetValueOrDefault(id);
				if (artist.publicHistory != null)
					foreach (PublicReleaseEntry e in artist.publicHistory)
						if (e.peakPosition is > 0 and <= 40) hit = Math.Max(hit, e.year);
				if (artist.hitCadenceLastTop40Week != HitActCadence.Unset) hit = Math.Max(hit, 1960 + artist.hitCadenceLastTop40Week / 52);
				standing.Add(new StandingAct {
					Artist = artist, Vehicle = vehicle, Recognition = recognition, LastHitYear = hit,
					Comedy = artist.primaryGenre == Genre.Comedy || vehicle.baseRecord.primaryGenre == Genre.Comedy,
				});
			}
			return standing;
		}

		/// <summary>The act's best-known chart record by title: its opening-career history or a live record that charted.</summary>
		public string BestKnownHit(SimulatedArtist artist) {
			string best = null;
			int bestPeak = int.MaxValue, bestWeeks = 0;
			void Consider(string title, int peak, int weeks) {
				if (string.IsNullOrEmpty(title) || peak <= 0 || peak > 40) return;
				if (peak < bestPeak || peak == bestPeak && weeks > bestWeeks) { best = title; bestPeak = peak; bestWeeks = weeks; }
			}
			if (artist.publicHistory != null)
				foreach (PublicReleaseEntry e in artist.publicHistory) Consider(e.title, e.peakPosition, e.weeksCharted);
			foreach (RecordRuntimeData r in all)
				if (r?.baseRecord?.artistId == artist.artistId) Consider(r.baseRecord.title, r.peakPosition, r.weeksOnChart);
			return best;
		}
	}

	/// <summary>Least recognition for any standing booking; programs set their own, higher floors.</summary>
	private const float StandingFloorAny = 0.05f;

	internal sealed class StandingAct {
		public SimulatedArtist Artist;
		public RecordRuntimeData Vehicle;
		public float Recognition;
		/// <summary>Release year of the act's last top-40 record; 0 = never had one (an album or nightclub name).</summary>
		public int LastHitYear;
		public bool Comedy;
	}

	// ---- booking ------------------------------------------------------------------------------------------------

	private sealed class Candidate {
		public RecordRuntimeData Record;
		public float Score;
		public bool Discovery;
		public bool Headliner;
		public MediaPitch Pitch;
		public StandardsQuery Query;
		public RecordRuntimeData Alternate;
		/// <summary>The rehearsal a rival label can afford for this date (null: its usual).</summary>
		public MediaPreparation? Prep;
		public BillLane Lane;
		public PerformedSong Song;
		/// <summary>What the act sings when it isn't the booked record (null: the record's own title).</summary>
		public string SongTitle;
	}

	private static void BookEpisode(TvProgram program, BroadcastScheduleVersion version, GameDate date, Evidence evidence) {
		string episodeId = program.Id + "|" + date.year + "-" + date.month.ToString("00") + "-" + date.day.ToString("00");
		if (!episodeIds.Add(episodeId)) return;
		var episode = new MediaEpisode { Id = episodeId, ProgramId = program.Id, Date = date, Repeat = version.RepeatMonths.Contains(date.month) };
		state.Episodes.Add(episode);
		if (episode.Repeat) { BookRepeat(program, version, episode); return; }

		int day = BroadcastExposure.DayIndex(date);
		BillPlan bill = program.BillOn(date);
		// Free slots by lane (§5.2 bill balance): chart, standing, comedy.
		int[] free = { bill.Chart, bill.Standing, bill.Comedy };
		int Free() => free[0] + free[1] + free[2];
		List<Candidate> pool = free[0] > 0 ? Candidates(program, date, evidence) : new List<Candidate>();
		episodeDeclines = 0;
		var chosen = new List<Candidate>();
		// The player's confirmed dates were reserved against this episode before it was booked (§5.1): they hold their slots,
		// in their own lane when it has room, otherwise in whatever lane does.
		foreach (MediaPitch reserved in state.Pitches.Where(p => p.Status == PitchStatus.Confirmed && p.ProgramId == program.Id && p.AirDay == day)) {
			if (Free() <= 0 || !evidence.ById.TryGetValue(reserved.RecordId ?? "", out RecordRuntimeData mine)) continue;
			int lane = free[(int)reserved.Lane] > 0 ? (int)reserved.Lane : Array.FindIndex(free, f => f > 0);
			chosen.Add(new Candidate { Record = mine, Score = 0.5f, Discovery = reserved.Discovery, Pitch = reserved, Lane = (BillLane)lane });
			free[lane]--;
		}
		// An announced headliner (§4.5 advertised guest) takes the bill's top spot.
		MediaHold hold = state.Holds.FirstOrDefault(h => h.ProgramId == program.Id && h.Day == day);
		if (hold != null && free[0] > 0 && evidence.ById.TryGetValue(hold.RecordId, out RecordRuntimeData held) && Available(held, program, date)) {
			var headliner = new Candidate { Record = held, Score = 10f, Headliner = true };
			// The label still has to say yes and afford the trip (§13); a declined headline date goes to the regular bill.
			if (AiAccepts(headliner, program, date, episodeId, evidence)) { chosen.Add(headliner); free[0]--; }
		}
		// The discovery slot: an eligible record below the top of the chart, chosen on fit and evidence, never guaranteed.
		// Rival labels answer the producer's call in turn; a label that says no passes the slot to the next in line.
		if (program.DiscoverySlot && free[0] > 0 && !chosen.Any(c => c.Discovery)) {
			Candidate pick = pool.Where(c => c.Discovery && Available(c.Record, program, date) && chosen.All(x => x.Record.baseRecord.artistId != c.Record.baseRecord.artistId))
				.OrderByDescending(c => Rank(c, program, date, "discovery")).FirstOrDefault(c => AiAccepts(c, program, date, episodeId, evidence));
			if (pick != null) { chosen.Add(pick); free[0]--; }
		}
		// One ranked queue per lane; the standing and comedy pools are built only for a bill that has those slots.
		var queues = new List<Candidate>[3];
		var next = new int[3];
		queues[0] = pool.Where(c => !c.Discovery).OrderByDescending(c => Rank(c, program, date, "slot")).ToList();
		for (int lane = 1; lane < 3; lane++)
			queues[lane] = free[lane] > 0
				? StandingCandidates(program, date, evidence, (BillLane)lane).OrderByDescending(c => Rank(c, program, date, "standing")).ToList()
				: new List<Candidate>();
		void Fill() {
			for (int lane = 0; lane < 3; lane++) {
				List<Candidate> queue = queues[lane];
				while (free[lane] > 0 && next[lane] < queue.Count) {
					Candidate c = queue[next[lane]++];
					if (chosen.Any(x => x.Record.baseRecord.artistId == c.Record.baseRecord.artistId) || !Available(c.Record, program, date, lane == 0 ? MaxNationalPerActPerWeek : 1, standing: lane == (int)BillLane.Standing)) continue;
					if (!AiAccepts(c, program, date, episodeId, evidence)) continue;
					c.Lane = (BillLane)lane;
					chosen.Add(c); free[lane]--;
				}
			}
		}
		Fill();
		foreach (Candidate c in chosen) ChooseSong(c, program, episodeId, evidence);

		// Standards review (§7.2) before the bill is final: a rival act that withdraws frees its slot for the next in line.
		// The player's queries were raised and answered when the date was offered. A standard, a cover or a comedy routine
		// isn't the record the standards department has on file.
		foreach (Candidate c in chosen.ToList()) {
			if (c.Pitch != null || c.Song != PerformedSong.Record) continue;
			StandardsQuery query = MediaStandards.Review(program, date, episodeId + "|" + c.Record.baseRecord.recordId, c.Record, RegionOf(program), date.AddDays(-6));
			if (query == null) continue;
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(c.Record.baseRecord.artistId);
			RecordRuntimeData alternate = AlternateFor(c.Record, evidence);
			StandardsResponse response = AiStandardsChoice(query, artist, program, alternate != null, c.Record.baseRecord.labelId, day);
			Settle(query, response, program, version, artist, alternate);
			state.StandardsQueries.Add(query);
			c.Query = query;
			c.Alternate = alternate;
			if (query.Resolution == StandardsResolution.Withdrawn) {
				chosen.Remove(c); free[(int)c.Lane]++;
				ReleaseReservation(c, episodeId);
				Remember(program.Id, c.Record.baseRecord.artistId, m => { m.Withdrawn++; m.LastNote = "withdrew rather than change the song"; });
			}
		}
		int before = chosen.Count;
		Fill();
		foreach (Candidate c in chosen.Skip(before)) ChooseSong(c, program, episodeId, evidence);
		// Telemetry: how much of the finite bill the combined pool filled (player, rivals who said yes).
		string scope = program.BooksNationally ? "network" : "local";
		Ledger("slots-" + scope + "-filled", chosen.Count);
		if (Free() > 0) Ledger("slots-" + scope + "-empty" + (episodeDeclines > 0 ? "-declined" : "-nopool"), Free());
		if (bill.Standing > 0) { Ledger("slots-standing-filled", chosen.Count(c => c.Lane == BillLane.Standing)); Ledger("slots-standing-empty", free[1]); }
		if (bill.Comedy > 0) { Ledger("slots-comedy-filled", chosen.Count(c => c.Lane == BillLane.Comedy)); Ledger("slots-comedy-empty", free[2]); }
		foreach (Candidate c in chosen) Ledger("song-" + c.Lane + "-" + c.Song);
		if (chosen.Count == 0) return;

		// Running order: a variety bill closes on its biggest name; a dance show plays them as they come.
		List<Candidate> order = program.Kind == TvProgramKind.PrimetimeVariety
			? chosen.OrderBy(c => c.Score).ToList()
			: chosen.OrderByDescending(c => c.Headliner ? 99f : Rank(c, program, date, "order")).ToList();

		// The advertised headliner's hype lifts the whole episode: viewers come for the event and stay for the bill.
		Candidate top = order.FirstOrDefault(c => c.Headliner);
		float hype = top == null ? 0f : Hype(top.Record, program, day, evidence);
		(float eventLift, float tuneIn) = EventDraw(hype, program);

		for (int i = 0; i < order.Count; i++) {
			Candidate c = order[i];
			RecordRuntimeData record = c.Record;
			var appearance = new MediaAppearance {
				Id = episodeId + "|" + i, ProgramId = program.Id, EpisodeId = episodeId, Date = date, SlotIndex = i, SlotCount = order.Count,
				ArtistId = record.baseRecord.artistId, ArtistName = record.baseRecord.artistName, RecordId = record.baseRecord.recordId,
				Title = c.SongTitle ?? record.baseRecord.title, LabelId = record.baseRecord.labelId, Genre = record.baseRecord.primaryGenre,
				ChartPosition = record.currentPosition, Discovery = c.Discovery, Method = program.Method, Broadcast = version.Broadcast, Lane = c.Lane, Song = c.Song,
				PlayerBooking = c.Pitch != null, Headliner = c.Headliner, Hype = c.Headliner ? hype : 0f, EventLift = eventLift, TuneIn = tuneIn,
				Preparation = c.Pitch?.Preparation ?? c.Prep ?? AiPreparation(record.baseRecord.labelId),
			};
			if (c.Pitch == null && pitchedTv.Contains(record.baseRecord.recordId + "|" + program.Id)) {
				AiMediaPitch sent = state.AiPitches.FirstOrDefault(p => p.Kind == PitchKind.Television && p.Status == PitchStatus.Sent
					&& p.RecordId == record.baseRecord.recordId && p.ProgramId == program.Id);
				if (sent != null) { sent.Status = PitchStatus.Confirmed; Ledger("pitch-booked"); }
			}
			if (c.Pitch != null) {
				c.Pitch.AppearanceId = appearance.Id;
				c.Query = c.Pitch.QueryId == null ? null : state.StandardsQueries.FirstOrDefault(q => q.Id == c.Pitch.QueryId);
				if (c.Query?.Resolution == StandardsResolution.SongChanged) c.Alternate = AlternateFor(record, evidence, includePlayer: true);
			}
			if (c.Query != null) {
				appearance.StandardsQueryId = c.Query.Id;
				c.Query.AppearanceId = appearance.Id;
				if (c.Query.Resolution == StandardsResolution.SongChanged) {
					appearance.OriginalTitle = appearance.Title;
					appearance.Title = c.Alternate?.baseRecord.title ?? "a song from their stage set";
				}
				if (c.Query.Resolution == StandardsResolution.Cut) appearance.Cut = true;
			}
			SimulatedArtist act = ArtistManager.Instance?.GetArtist(appearance.ArtistId);
			if (!appearance.Cut) appearance.Performance = MediaPerformance.Resolve(appearance.Id, appearance.Method, appearance.Broadcast, act, appearance.Preparation,
				program.ProductionReadiness);
			// A taped breakdown nobody would retake is cut in the edit; a live one goes out as it happened.
			if (appearance.Performance is { Outcome: PerformanceOutcome.Breakdown } && appearance.Broadcast == BroadcastMethod.Taped) appearance.Cut = true;
			float guestDraw = i == order.Count - 1 && record.currentPosition > 0 && record.currentPosition <= 40 ? 0.10f * (1f - (record.currentPosition - 1) / 40f) : 0f;
			AddAirings(appearance, program, version, guestDraw, 1f, evidence.Regions);
			Store(appearance);
			episode.AppearanceIds.Add(appearance.Id);
		}
		if (hold != null) state.Holds.Remove(hold);
		MediaAppearance headline = top == null ? null : state.Appearances.LastOrDefault(a => a.EpisodeId == episodeId && a.Headliner);
		if (headline != null && headline.Airings.Count > 0)
			Godot.GD.Print($"MEDIA_EVENT date={date.ToShortString()} program={program.Id} act=\"{headline.ArtistName}\" hype={hype:F2} lift={eventLift:F2} tuneIn={tuneIn:F2} " +
				$"reachM={headline.Airings[0].Exposure.UniqueReach / 1e6:F1} cut={headline.Cut}");
	}

	private static string RegionOf(TvProgram program) =>
		program.HomeCityId == null ? null : DistanceModel.GetCityById(program.HomeCityId)?.parentRegionId;

	/// <summary>Another current single by the same act to offer standards instead (§7.3 "offer another song").</summary>
	private static RecordRuntimeData AlternateFor(RecordRuntimeData record, Evidence evidence, bool includePlayer = false) {
		string artist = record.baseRecord.artistId;
		IEnumerable<RecordRuntimeData> pool = includePlayer ? evidence.ById.Values : evidence.Singles;
		return pool.Where(r => r.baseRecord.artistId == artist && r.baseRecord.recordId != record.baseRecord.recordId && r.baseRecord.format == ReleaseFormat.Single
				&& MediaStandards.FindingsFor(r).Count == 0)
			.OrderByDescending(r => r.unitsThisWeek).ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal).FirstOrDefault();
	}

	/// <summary>How much rehearsal a rival label arranges: the established houses prepare; a small shop sends the act as it is.</summary>
	private static MediaPreparation AiPreparation(string labelId) {
		AILabel label = CompetitorManager.Instance?.GetLabel(labelId);
		if (label == null) return MediaPreparation.Standard;
		float u = MediaAtmosphere.Unit("prep|" + labelId);
		return label.tier == LabelTier.Major ? (u < 0.5f ? MediaPreparation.Extra : MediaPreparation.Standard)
			: label.tier == LabelTier.Small || label.tier == LabelTier.Boutique ? (u < 0.35f ? MediaPreparation.Minimal : MediaPreparation.Standard)
			: MediaPreparation.Standard;
	}

	/// <summary>Carry a response through to its resolution (§7.3). Producer support and network clearance can diverge; a
	/// promise broken on a live microphone airs, on tape it is usually caught and the segment removed.</summary>
	internal static void Settle(StandardsQuery query, StandardsResponse response, TvProgram program, BroadcastScheduleVersion version, SimulatedArtist artist, RecordRuntimeData alternate) {
		query.Response = response;
		switch (response) {
			case StandardsResponse.ComplyEdit:
				query.Resolution = StandardsResolution.Edited;
				query.Outcome = "Agreed to the change for the broadcast; the record itself is untouched.";
				break;
			case StandardsResponse.AlternateSong:
				query.Resolution = StandardsResolution.SongChanged;
				query.Outcome = alternate != null ? $"Offered \"{alternate.baseRecord.title}\" instead; standards cleared it." : "Did another song from the act's set instead.";
				break;
			case StandardsResponse.ProducerDefends:
				if (MediaStandards.NetworkClears(query, program)) {
					query.Resolution = StandardsResolution.Cleared;
					query.Outcome = "The producer argued for the original and " + query.Authority + " let it through.";
				} else if (program.Method != PerformanceMethod.Mime && MediaStandards.ActConsents(artist, StandardsResponse.ComplyEdit, query.Id)) {
					query.Resolution = StandardsResolution.Edited;
					query.Outcome = "The producer argued for it and lost; the act agreed to the change.";
				} else {
					query.Resolution = StandardsResolution.Withdrawn;
					query.Outcome = "The producer argued for it and lost; the act withdrew.";
				}
				break;
			case StandardsResponse.PromiseThenDefy:
				bool caught = version.Broadcast == BroadcastMethod.Taped && MediaStandards.CaughtOnTape(query);
				query.Resolution = caught ? StandardsResolution.Cut : StandardsResolution.Defied;
				query.Outcome = caught ? "Promised the change, sang the original at the taping; the segment was cut before broadcast."
					: "Promised the change, then sang the original line on the air.";
				break;
			case StandardsResponse.AcceptRemoval:
				query.Resolution = StandardsResolution.Cut;
				query.Outcome = "The segment was removed.";
				break;
			default:
				query.Resolution = StandardsResolution.Withdrawn;
				query.Outcome = "Withdrew from the date rather than change the song.";
				break;
		}
	}

	private static void Remember(string programId, string artistId, Action<MediaProgramMemory> change) {
		string key = programId + "|" + artistId;
		if (!state.ProgramMemory.TryGetValue(key, out var memory)) state.ProgramMemory[key] = memory = new MediaProgramMemory { ProgramId = programId, ArtistId = artistId };
		change(memory);
	}

	/// <summary>A summer rerun: the slot replays an earlier appearance. No new booking, fee or incident (§4.5).</summary>
	private static void BookRepeat(TvProgram program, BroadcastScheduleVersion version, MediaEpisode episode) {
		GameDate source = episode.Date.AddDays(-7 * 16);
		MediaEpisode original = state.Episodes.Where(e => e.ProgramId == program.Id && !e.Repeat && e.Date <= source && e.AppearanceIds.Count > 0)
			.OrderByDescending(e => e.Date).FirstOrDefault();
		if (original == null) return;
		foreach (string id in original.AppearanceIds) {
			MediaAppearance old = state.Appearances.FirstOrDefault(a => a.Id == id);
			// A cut segment has nothing to repeat, and a repeat never replays an incident (§4.5).
			if (old == null || old.Cut) continue;
			var repeat = new MediaAppearance {
				Id = episode.Id + "|r" + old.SlotIndex, ProgramId = program.Id, EpisodeId = episode.Id, Date = episode.Date, SlotIndex = old.SlotIndex,
				SlotCount = old.SlotCount, ArtistId = old.ArtistId, ArtistName = old.ArtistName, RecordId = old.RecordId, Title = old.Title,
				LabelId = old.LabelId, Genre = old.Genre, ChartPosition = old.ChartPosition, RepeatOfId = old.Id, Method = old.Method, Broadcast = old.Broadcast,
				Lane = old.Lane, Song = old.Song,
			};
			AddAirings(repeat, program, version, 0f, RepeatNovelty, ChartManager.Instance?.GetAllRegions());
			foreach (MediaAiring airing in repeat.Airings) airing.Repeat = true;
			Store(repeat);
			episode.AppearanceIds.Add(repeat.Id);
		}
	}

	private static void Store(MediaAppearance appearance) {
		state.Appearances.Add(appearance);
		if (!byArtist.TryGetValue(appearance.ArtistId, out var list)) byArtist[appearance.ArtistId] = list = new List<MediaAppearance>();
		list.Add(appearance);
	}

	private static void AddAirings(MediaAppearance appearance, TvProgram program, BroadcastScheduleVersion version, float guestDraw, float novelty,
		IReadOnlyList<MarketRegion> regions) {
		var markets = BroadcastExposure.Markets(program, version, appearance.Date, regions);
		// A cut segment keeps one empty airing record so its date still settles once; nobody saw it.
		appearance.Airings.Add(new MediaAiring {
			Date = appearance.Date, StartMinute = version.StartMinute, CityId = program.Network == TvNetwork.Local ? program.HomeCityId : null,
			Exposure = appearance.Cut ? new ExposureEstimate()
				: BroadcastExposure.Estimate(program, version.DurationMinutes, appearance.SlotIndex, appearance.SlotCount, guestDraw, markets, novelty,
					appearance.EventLift, appearance.TuneIn),
		});
		if (appearance.Cut) return;
		// One recorded appearance travels as tape: each syndication market is its own dated airing, never a multiplier.
		foreach (BroadcastExposure.Market market in BroadcastExposure.SyndicatedAirings(program, appearance.Date))
			appearance.Airings.Add(new MediaAiring {
				Date = market.Date, StartMinute = market.LocalStartMinute, CityId = market.Id, Syndicated = true,
				Exposure = BroadcastExposure.Estimate(program, version.DurationMinutes, appearance.SlotIndex, appearance.SlotCount, 0f, new[] { market }, novelty),
			});
	}

	private static List<Candidate> Candidates(TvProgram program, GameDate date, Evidence evidence) {
		var list = new List<Candidate>();
		if (!program.BooksNationally) {
			string region = DistanceModel.GetCityById(program.HomeCityId)?.parentRegionId;
			if (region == null || !evidence.RegionalBest.TryGetValue(region, out var best) || best.Count == 0) return list;
			int top = best[0].Units;
			// A country or soul hour books its own audience's records, not whatever the town is buying.
			float fitFloor = program.Kind == TvProgramKind.MusicShow ? 0.5f : 0f;
			foreach (var (record, units) in best) {
				if (program.GenreFit(record.baseRecord.primaryGenre) < fitFloor) continue;
				bool hometown = evidence.CityOf(record.baseRecord.artistId) == program.HomeCityId;
				float local = units / (float)Math.Max(1, top);
				float score = program.GenreFit(record.baseRecord.primaryGenre) * (0.4f + local) * (hometown ? 1.6f : 1f);
				// A local producer's discovery slot goes to a hometown act or a record off the national chart that the town is buying.
				bool discovery = hometown || record.currentPosition == 0 || record.currentPosition > 60;
				list.Add(new Candidate { Record = record, Score = score, Discovery = discovery });
			}
			// The region's own acts, further down the reports: cheap to bring in, and a local show's natural bench (§3.1).
			foreach (var (record, units) in evidence.RegionalLocals(region)) {
				if (list.Any(c => c.Record == record) || program.GenreFit(record.baseRecord.primaryGenre) < fitFloor) continue;
				bool hometown = evidence.CityOf(record.baseRecord.artistId) == program.HomeCityId;
				float score = program.GenreFit(record.baseRecord.primaryGenre) * (0.3f + 0.5f * units / (float)Math.Max(1, top)) * (hometown ? 1.6f : 1f);
				list.Add(new Candidate { Record = record, Score = score, Discovery = hometown || record.currentPosition == 0 || record.currentPosition > 60 });
			}
			return list;
		}
		// A specialist hour with no ceiling takes any record of its music on the national chart.
		int ceiling = program.ChartCeiling > 0 ? program.ChartCeiling : 100;
		float nationalFitFloor = program.Kind == TvProgramKind.MusicShow ? 0.5f : 0f;
		foreach (RecordRuntimeData record in evidence.Chart) {
			int pos = record.currentPosition;
			if (pos <= 0 || program.GenreFit(record.baseRecord.primaryGenre) < nationalFitFloor) continue;
			bool ordinary = pos <= ceiling;
			bool discovery = program.DiscoverySlot && !ordinary && evidence.Broke(record);
			if (!ordinary && !discovery) continue;
			float strength = ordinary ? 1f - (pos - 1) / (float)Math.Max(1, ceiling) : 0.35f;
			float score = program.GenreFit(record.baseRecord.primaryGenre) * (0.25f + strength);
			list.Add(new Candidate { Record = record, Score = score, Discovery = discovery });
		}
		// The discovery slot can also go to a record breaking in a region but not yet on the national chart.
		if (program.DiscoverySlot)
			foreach (var best in evidence.RegionalBest.Values)
				foreach (var (record, _) in best.Take(10))
					if (record.currentPosition == 0 && evidence.Broke(record) && list.All(c => c.Record != record))
						list.Add(new Candidate { Record = record, Score = program.GenreFit(record.baseRecord.primaryGenre) * 0.3f, Discovery = true });
		return list;
	}

	/// <summary>
	/// Standing and comedy candidates (§5.2): recording acts the audience already knows, scored on recognition, the
	/// program's audience fit, and how long ago the last hit was. A current top-40 act is left to the chart lane when the
	/// bill has one. Each candidate carries the act's latest live record as the campaign the date is charged to.
	/// </summary>
	private static List<Candidate> StandingCandidates(TvProgram program, GameDate date, Evidence evidence, BillLane lane) {
		var list = new List<Candidate>();
		bool chartLane = program.BillOn(date).Chart > 0;
		// A local program's standing guests are its own region's names; a network or national talk show calls anyone.
		string region = program.BooksNationally ? null : DistanceModel.GetCityById(program.HomeCityId)?.parentRegionId;
		foreach (StandingAct s in evidence.StandingActs()) {
			if (s.Comedy != (lane == BillLane.Comedy) || s.Recognition < (s.Comedy ? program.ComedyFloor : program.StandingFloor)) continue;
			float fit = program.GenreFit(s.Vehicle.baseRecord.primaryGenre);
			if (fit < 0.5f) continue;
			if (chartLane && lane == BillLane.Standing && evidence.OnTop40(s.Artist.artistId)) continue;
			if (region != null && DistanceModel.GetCityById(evidence.CityOf(s.Artist.artistId) ?? "")?.parentRegionId != region) continue;
			int years = s.LastHitYear > 0 ? Math.Max(0, date.year - s.LastHitYear) : 6;
			float recency = 1f / (1f + 0.12f * years);
			list.Add(new Candidate { Record = s.Vehicle, Score = 2f * s.Recognition * fit * recency, Lane = lane });
		}
		return list;
	}

	/// <summary>
	/// What a booked act sings (SimTools/MediaTvHeadlinerBookingDesign.md). A chart booking sings its record. A standing
	/// act with a fresh single sings that; otherwise it sings what such acts sang on 1960-64 variety bills: mostly a
	/// standard from its own songbook, sometimes another act's current hit, now and then its own best-known hit. A
	/// comedian does a routine. Keyed on the episode and act, so a second read gives the same song.
	/// </summary>
	private static void ChooseSong(Candidate c, TvProgram program, string episodeId, Evidence evidence) {
		Record rec = c.Record.baseRecord;
		if (c.Lane == BillLane.Chart || c.Pitch != null) { c.Song = PerformedSong.Record; return; }
		if (c.Lane == BillLane.Comedy) {
			c.Song = PerformedSong.Routine;
			c.SongTitle = rec.format == ReleaseFormat.Single ? "a routine" : $"a routine from \"{rec.title}\"";
			return;
		}
		string key = episodeId + "|" + rec.artistId;
		// A charting single gets sung half the time, a fresh one that isn't selling a fifth: even acts with a current
		// record mostly did standards on a variety bill.
		float r = MediaAtmosphere.Unit("song-record|" + key);
		if (rec.format == ReleaseFormat.Single && (c.Record.currentPosition > 0 ? r < 0.5f : c.Record.weeksSinceRelease <= 12 && r < 0.2f)) {
			c.Song = PerformedSong.Record; return;
		}
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.artistId);
		float u = MediaAtmosphere.Unit("song|" + key);
		string ownHit = artist == null ? null : evidence.BestKnownHit(artist);
		GenreFamily family = GenreCatalog.TryGet(rec.primaryGenre, out var profile) ? profile.Family : GenreFamily.Pop;
		if (ownHit != null && u < 0.10f) { c.Song = PerformedSong.OwnHit; c.SongTitle = ownHit; return; }
		if (u < 0.28f) {
			var hits = evidence.Chart.Where(r => r.currentPosition is > 0 and <= 30 && r.baseRecord.artistId != rec.artistId
					&& (GenreCatalog.TryGet(r.baseRecord.primaryGenre, out var p) ? p.Family : GenreFamily.Pop) == family)
				.OrderBy(r => r.currentPosition).ToList();
			if (hits.Count > 0) {
				c.Song = PerformedSong.CurrentCover;
				c.SongTitle = hits[(int)(MediaAtmosphere.Unit("cover|" + key) * hits.Count) % hits.Count].baseRecord.title;
				return;
			}
		}
		// The songbook: the act's family's best-known standards, read from the catalog without touching its draws.
		var book = CompositionCatalogService.GetStandardsForFamily(family);
		if (book == null || book.Count == 0) book = CompositionCatalogService.GetStandardsForFamily(GenreFamily.Pop);
		var known = book?.Where(s => !string.IsNullOrEmpty(s.title)).OrderByDescending(s => s.adultFamiliarity).ThenBy(s => s.songId, StringComparer.Ordinal).Take(40).ToList();
		if (known != null && known.Count > 0) {
			c.Song = PerformedSong.Standard;
			c.SongTitle = known[(int)(MediaAtmosphere.Unit("standard|" + key) * known.Count) % known.Count].title;
			return;
		}
		if (ownHit != null) { c.Song = PerformedSong.OwnHit; c.SongTitle = ownHit; return; }
		c.Song = PerformedSong.Record;
	}

	/// <summary>Producer judgment: fit and evidence, with a keyed spread so the same chart doesn't book the same bill
	/// everywhere. Keyed on program, date and record, so a reload or a second read gives the same answer.</summary>
	private static float Rank(Candidate c, TvProgram program, GameDate date, string purpose) =>
		c.Score * Interest(c, program) * (0.55f + 0.9f * MediaAtmosphere.Unit(program.Id + "|" + BroadcastExposure.DayIndex(date) + "|" + c.Record.baseRecord.recordId + "|" + purpose));

	/// <summary>What makes a booker look twice (§5.2, §10.2): a disc on the desk from the label, and fans already asking.</summary>
	private static float Interest(Candidate c, TvProgram program) {
		float k = pitchedTv.Contains(c.Record.baseRecord.recordId + "|" + program.Id) ? 1.2f : 1f;
		FanEpisodeState fans = EpisodeOf(c.Record.baseRecord.artistId);
		k *= program.Kind == TvProgramKind.PrimetimeVariety
			? fans switch { FanEpisodeState.CulturalPhenomenon => 1.3f, FanEpisodeState.NationalSensation => 1.1f, _ => 1f }
			: fans switch { FanEpisodeState.CulturalPhenomenon => 1.4f, FanEpisodeState.NationalSensation => 1.25f, FanEpisodeState.RegionalFavorite => 1.1f, _ => 1f };
		return k;
	}

	/// <summary>Finite calendars (§5.1): one booking per act per day, a rebooking gap per program, a cap on national dates a
	/// week. A name booked for its standing gets one national date a week and waits a season before returning to the same
	/// variety hour; an act with a current hit can take two and comes back sooner.</summary>
	private static bool Available(RecordRuntimeData record, TvProgram program, GameDate date, int maxNationalThisWeek = MaxNationalPerActPerWeek,
		bool standing = false) {
		string artist = record.baseRecord.artistId;
		int today = BroadcastExposure.DayIndex(date);
		int rebookWeeks = standing && program.Kind == TvProgramKind.PrimetimeVariety ? Math.Max(program.RebookWeeks, StandingReturnWeeks) : program.RebookWeeks;
		int nationalThisWeek = 0;
		// A breached agreement shuts that program's door for a while (§7.3); the network's other shows only grow wary
		// for a year, never an industry-wide blacklist (§2).
		if (state.ProgramMemory.TryGetValue(program.Id + "|" + artist, out var memory) && memory.ExcludedUntilDay > today) return false;
		if (program.BooksNationally && state.ProgramMemory.TryGetValue("network:" + program.Network + "|" + artist, out var review)
			&& review.ExcludedUntilDay > today && MediaAtmosphere.Unit("network-review|" + program.Id + "|" + artist + "|" + today) < 0.5f) return false;
		if (!byArtist.TryGetValue(artist, out var mine)) return true;
		foreach (MediaAppearance a in mine) {
			if (a.RepeatOfId != null) continue;
			int day = BroadcastExposure.DayIndex(a.Date);
			if (day == today) return false;
			if (a.ProgramId == program.Id && today - day < 7 * rebookWeeks) return false;
			if (Math.Abs(today - day) < 7 && MediaOutletCatalog.Get(a.ProgramId)?.BooksNationally == true) nationalThisWeek++;
		}
		return !program.BooksNationally || nationalThisWeek < maxNationalThisWeek;
	}

	// ---- tallies and history ---------------------------------------------------------------------------------------

	private static void TallyAiredThrough(GameDate weekEnd) {
		int cutoff = BroadcastExposure.DayIndex(weekEnd);
		airedThisWeek.Clear();
		double population = (ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>()).Sum(r => r.population) * 1e6;
		foreach (MediaAppearance a in state.Appearances) {
			foreach (MediaAiring airing in a.Airings) {
				if (airing.Tallied || BroadcastExposure.DayIndex(airing.Date) > cutoff) continue;
				airing.Tallied = true;
				airedThisWeek.Add((a, airing));
				if (!state.Tallies.TryGetValue(a.ArtistId, out MediaActTally t))
					state.Tallies[a.ArtistId] = t = new MediaActTally { ArtistId = a.ArtistId, ArtistName = a.ArtistName };
				t.ArtistName = a.ArtistName;
				if (!airing.Repeat && !airing.Syndicated) {
					AfterAir(a, airing);
					if (a.Cut) continue;
					t.Appearances++;
					if (MediaOutletCatalog.Get(a.ProgramId)?.Network == TvNetwork.Local) t.LocalAppearances++; else t.NationalAppearances++;
					t.LastProgramId = a.ProgramId; t.LastDate = a.Date;
					if (airing.Exposure.UniqueReach > t.PeakEpisodeAudience) {
						t.PeakEpisodeAudience = airing.Exposure.UniqueReach;
						t.BestNight = MediaOutletCatalog.Get(a.ProgramId)?.Name + ", " + a.Date.ToShortString();
					}
				}
				t.ViewerMinutes += airing.Exposure.ViewerMinutes;
				t.TeenImpressions += airing.Exposure.AverageByGroup[(int)ViewerGroup.Teen];
				// A show's regulars watch every week: a return visit (or its repeat) is mostly the same people. Only a
				// first visit to a program reaches its audience as new; later ones add the share who weren't there before.
				bool returning = t.ProgramsSeen.Contains(a.ProgramId);
				if (!returning) t.ProgramsSeen.Add(a.ProgramId);
				double fresh = airing.Exposure.UniqueReach * (returning ? ReturnVisitNewShare : 1.0);
				t.EstimatedReach = BroadcastExposure.CombineReach(t.EstimatedReach, fresh, population);
			}
		}
	}

	private static void Prune(GameDate weekEnd) {
		int floor = BroadcastExposure.DayIndex(weekEnd) - 7 * HistoryWeeks;
		var keep = new HashSet<string>(state.Episodes.Where(e => BroadcastExposure.DayIndex(e.Date) >= floor).Select(e => e.Id), StringComparer.Ordinal);
		state.Episodes.RemoveAll(e => !keep.Contains(e.Id));
		// An appearance stays while any of its airings (a late syndicated tape) is still to be tallied.
		state.Appearances.RemoveAll(a => !keep.Contains(a.EpisodeId) && a.Airings.All(x => x.Tallied));
	}

	// ---- read-only views for the paper and the Rolodex ----------------------------------------------------------------

	/// <summary>Whether a network program airs in a city: ABC's thin clearance means some towns never got it. Keyed per
	/// city and program, so the answer is stable.</summary>
	public static bool ClearsIn(TvProgram program, string cityId) {
		if (program.Network == TvNetwork.Local) return program.HomeCityId == cityId;
		// Every big market had all three networks; the gaps were two-station towns, where the third network's shows
		// were picked up piecemeal or not at all. So a metro over a million always clears, and a small one may not.
		if (MediaOutletCatalog.MetroPopulationMillions(cityId, 1960) >= 1.0f) return true;
		MarketRegion region = ChartManager.Instance?.GetRegionById(DistanceModel.GetCityById(cityId)?.parentRegionId ?? "");
		return MediaAtmosphere.Unit("clears|" + program.Id + "|" + cityId) < BroadcastExposure.Clearance(program.Network, region);
	}

	/// <summary>Local clock minute an airing starts in a city.</summary>
	public static int LocalMinuteIn(TvProgram program, MediaAiring airing, string cityId) {
		if (airing.CityId != null) return airing.StartMinute;
		var version = program.VersionOn(airing.Date);
		return BroadcastExposure.LocalStart(version?.Carriage ?? CarriageRule.NetworkPrimetime, airing.StartMinute, MediaOutletCatalog.ZoneOf(cityId));
	}

	/// <summary>Everything airing in a city on a date with a music guest: (program, appearance, airing, local minute).</summary>
	public static List<(TvProgram Program, MediaAppearance Appearance, MediaAiring Airing, int LocalMinute)> AiringIn(string cityId, GameDate date) {
		var list = new List<(TvProgram, MediaAppearance, MediaAiring, int)>();
		if (!MediaAtmosphere.Observing || cityId == null) return list;
		foreach (MediaAppearance a in state.Appearances) {
			TvProgram program = MediaOutletCatalog.Get(a.ProgramId);
			if (program == null) continue;
			foreach (MediaAiring airing in a.Airings) {
				if (airing.Date != date) continue;
				bool here = airing.CityId != null ? airing.CityId == cityId : ClearsIn(program, cityId);
				if (here) list.Add((program, a, airing, LocalMinuteIn(program, airing, cityId)));
			}
		}
		return list.OrderBy(x => x.Item4).ThenBy(x => x.Item2.SlotIndex).ToList();
	}

	/// <summary>The biggest airings of a date across the country, for "last night" notes. Only airings already past.</summary>
	public static List<(TvProgram Program, MediaAppearance Appearance, MediaAiring Airing)> BiggestAired(GameDate date, GameDate today, int count) {
		if (!MediaAtmosphere.Observing || date >= today) return new();
		return state.Appearances.SelectMany(a => a.Airings.Where(x => x.Date == date && !x.Syndicated).Select(x => (Program: MediaOutletCatalog.Get(a.ProgramId), Appearance: a, Airing: x)))
			.Where(x => x.Program != null && x.Program.Network != TvNetwork.Local)
			.OrderByDescending(x => x.Airing.Exposure.UniqueReach).Take(count).ToList();
	}

	// ---- persistence ------------------------------------------------------------------------------------------------

	public static void CaptureWorld(WorldSaveData world) { if (world != null) world.Media = state; }
	public static void RehydrateWorld(WorldSaveData world) => state = world?.Media ?? new MediaSaveData();
	internal static void ResetForProbe() => state = new MediaSaveData();

	public static GameDate FromDayIndex(int day) {
		DateTime d = new DateTime(1960, 1, 1).AddDays(day);
		return new GameDate(d.Year, d.Month, d.Day);
	}
}
