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
	public GameDate LastDate;
	/// <summary>Programs this act has aired on: a second visit to the same show reaches mostly the same regular viewers.</summary>
	public List<string> ProgramsSeen = new();
}

public sealed class MediaSaveData {
	public int ConfigVersion { get; set; }
	public int BookedThroughDay { get; set; } = -1;
	public List<MediaEpisode> Episodes { get; set; } = new();
	public List<MediaAppearance> Appearances { get; set; } = new();
	public Dictionary<string, MediaActTally> Tallies { get; set; } = new();
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
public static class MediaAtmosphereService {
	private const int HistoryWeeks = 26;
	private const int MaxNationalPerActPerWeek = 2;
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
		BookThrough(start.AddDays(start.DaysUntilFriday), start, charts);
	}

	/// <summary>Called by ChartManager after the completed week's settlement. Tallies the airings that ran this week,
	/// then books Saturday through Friday from the chart that just froze. A late airing after this cutoff waits for next week.</summary>
	public static void AfterCompletedWeek(GameDate weekEnd, ChartManager charts) {
		if (!MediaAtmosphere.Observing || charts == null) return;
		long started = System.Diagnostics.Stopwatch.GetTimestamp();
		TallyAiredThrough(weekEnd);
		BookThrough(weekEnd.AddDays(7), weekEnd.AddDays(1), charts);
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
	}

	private static void BookThrough(GameDate last, GameDate firstCandidate, ChartManager charts) {
		int firstDay = Math.Max(state.BookedThroughDay + 1, BroadcastExposure.DayIndex(firstCandidate));
		int lastDay = BroadcastExposure.DayIndex(last);
		if (lastDay < firstDay) return;
		var evidence = new Evidence(charts);
		byArtist = state.Appearances.GroupBy(a => a.ArtistId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
		episodeIds = new HashSet<string>(state.Episodes.Select(e => e.Id), StringComparer.Ordinal);
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
	private sealed class Evidence {
		public readonly List<RecordRuntimeData> Chart;
		public readonly Dictionary<string, List<(RecordRuntimeData Record, int Units)>> RegionalBest = new(StringComparer.Ordinal);
		public readonly IReadOnlyList<MarketRegion> Regions;
		public readonly float BreakoutBar;
		private readonly Dictionary<string, string> artistCity = new(StringComparer.Ordinal);

		public Evidence(ChartManager charts) {
			Chart = charts.GetCurrentChart().Where(Eligible).ToList();
			Regions = charts.GetAllRegions();
			BreakoutBar = CompetitorManager.Instance?.RegionalBreakoutDealThreshold ?? 0.20f;
			var singles = charts.GetAllRecords().Where(Eligible).ToList();
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

		public bool Broke(RecordRuntimeData r) => r.regionalData.Values.Any(rd => rd != null && rd.peakBreakoutScore >= BreakoutBar);
	}

	// ---- booking ------------------------------------------------------------------------------------------------

	private sealed class Candidate {
		public RecordRuntimeData Record;
		public float Score;
		public bool Discovery;
	}

	private static void BookEpisode(TvProgram program, BroadcastScheduleVersion version, GameDate date, Evidence evidence) {
		string episodeId = program.Id + "|" + date.year + "-" + date.month.ToString("00") + "-" + date.day.ToString("00");
		if (!episodeIds.Add(episodeId)) return;
		var episode = new MediaEpisode { Id = episodeId, ProgramId = program.Id, Date = date, Repeat = version.RepeatMonths.Contains(date.month) };
		state.Episodes.Add(episode);
		if (episode.Repeat) { BookRepeat(program, version, episode); return; }

		List<Candidate> pool = Candidates(program, date, evidence);
		var chosen = new List<Candidate>();
		int slots = program.GuestSlots;
		// The discovery slot: an eligible record below the top of the chart, chosen on fit and evidence, never guaranteed.
		if (program.DiscoverySlot) {
			Candidate pick = pool.Where(c => c.Discovery && Available(c.Record, program, date)).OrderByDescending(c => Rank(c, program, date, "discovery")).FirstOrDefault();
			if (pick != null) { chosen.Add(pick); slots--; }
		}
		foreach (Candidate c in pool.Where(c => !c.Discovery).OrderByDescending(c => Rank(c, program, date, "slot"))) {
			if (slots <= 0) break;
			if (chosen.Any(x => x.Record.baseRecord.artistId == c.Record.baseRecord.artistId) || !Available(c.Record, program, date)) continue;
			chosen.Add(c); slots--;
		}
		if (chosen.Count == 0) return;

		// Running order: a variety bill closes on its biggest name; a dance show plays them as they come.
		List<Candidate> order = program.Kind == TvProgramKind.PrimetimeVariety
			? chosen.OrderBy(c => c.Score).ToList()
			: chosen.OrderByDescending(c => Rank(c, program, date, "order")).ToList();
		for (int i = 0; i < order.Count; i++) {
			RecordRuntimeData record = order[i].Record;
			var appearance = new MediaAppearance {
				Id = episodeId + "|" + i, ProgramId = program.Id, EpisodeId = episodeId, Date = date, SlotIndex = i, SlotCount = order.Count,
				ArtistId = record.baseRecord.artistId, ArtistName = record.baseRecord.artistName, RecordId = record.baseRecord.recordId,
				Title = record.baseRecord.title, LabelId = record.baseRecord.labelId, Genre = record.baseRecord.primaryGenre,
				ChartPosition = record.currentPosition, Discovery = order[i].Discovery, Method = program.Method, Broadcast = version.Broadcast,
			};
			float guestDraw = i == order.Count - 1 && record.currentPosition > 0 && record.currentPosition <= 40 ? 0.10f * (1f - (record.currentPosition - 1) / 40f) : 0f;
			AddAirings(appearance, program, version, guestDraw, 1f, evidence.Regions);
			Store(appearance);
			episode.AppearanceIds.Add(appearance.Id);
		}
	}

	/// <summary>A summer rerun: the slot replays an earlier appearance. No new booking, fee or incident (§4.5).</summary>
	private static void BookRepeat(TvProgram program, BroadcastScheduleVersion version, MediaEpisode episode) {
		GameDate source = episode.Date.AddDays(-7 * 16);
		MediaEpisode original = state.Episodes.Where(e => e.ProgramId == program.Id && !e.Repeat && e.Date <= source && e.AppearanceIds.Count > 0)
			.OrderByDescending(e => e.Date).FirstOrDefault();
		if (original == null) return;
		foreach (string id in original.AppearanceIds) {
			MediaAppearance old = state.Appearances.FirstOrDefault(a => a.Id == id);
			if (old == null) continue;
			var repeat = new MediaAppearance {
				Id = episode.Id + "|r" + old.SlotIndex, ProgramId = program.Id, EpisodeId = episode.Id, Date = episode.Date, SlotIndex = old.SlotIndex,
				SlotCount = old.SlotCount, ArtistId = old.ArtistId, ArtistName = old.ArtistName, RecordId = old.RecordId, Title = old.Title,
				LabelId = old.LabelId, Genre = old.Genre, ChartPosition = old.ChartPosition, RepeatOfId = old.Id, Method = old.Method, Broadcast = old.Broadcast,
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
		appearance.Airings.Add(new MediaAiring {
			Date = appearance.Date, StartMinute = version.StartMinute, CityId = program.Network == TvNetwork.Local ? program.HomeCityId : null,
			Exposure = BroadcastExposure.Estimate(program, version.DurationMinutes, appearance.SlotIndex, appearance.SlotCount, guestDraw, markets, novelty),
		});
		// One recorded appearance travels as tape: each syndication market is its own dated airing, never a multiplier.
		foreach (BroadcastExposure.Market market in BroadcastExposure.SyndicatedAirings(program, appearance.Date))
			appearance.Airings.Add(new MediaAiring {
				Date = market.Date, StartMinute = market.LocalStartMinute, CityId = market.Id, Syndicated = true,
				Exposure = BroadcastExposure.Estimate(program, version.DurationMinutes, appearance.SlotIndex, appearance.SlotCount, 0f, new[] { market }, novelty),
			});
	}

	private static List<Candidate> Candidates(TvProgram program, GameDate date, Evidence evidence) {
		var list = new List<Candidate>();
		if (program.Network == TvNetwork.Local) {
			string region = DistanceModel.GetCityById(program.HomeCityId)?.parentRegionId;
			if (region == null || !evidence.RegionalBest.TryGetValue(region, out var best) || best.Count == 0) return list;
			int top = best[0].Units;
			foreach (var (record, units) in best) {
				bool hometown = evidence.CityOf(record.baseRecord.artistId) == program.HomeCityId;
				float local = units / (float)Math.Max(1, top);
				float score = program.GenreFit(record.baseRecord.primaryGenre) * (0.4f + local) * (hometown ? 1.6f : 1f);
				// A local producer's discovery slot goes to a hometown act or a record off the national chart that the town is buying.
				bool discovery = hometown || record.currentPosition == 0 || record.currentPosition > 60;
				list.Add(new Candidate { Record = record, Score = score, Discovery = discovery });
			}
			return list;
		}
		foreach (RecordRuntimeData record in evidence.Chart) {
			int pos = record.currentPosition;
			if (pos <= 0) continue;
			bool ordinary = pos <= program.ChartCeiling;
			bool discovery = program.DiscoverySlot && !ordinary && evidence.Broke(record);
			if (!ordinary && !discovery) continue;
			float strength = ordinary ? 1f - (pos - 1) / (float)Math.Max(1, program.ChartCeiling) : 0.35f;
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

	/// <summary>Producer judgment: fit and evidence, with a keyed spread so the same chart doesn't book the same bill
	/// everywhere. Keyed on program, date and record, so a reload or a second read gives the same answer.</summary>
	private static float Rank(Candidate c, TvProgram program, GameDate date, string purpose) =>
		c.Score * (0.55f + 0.9f * MediaAtmosphere.Unit(program.Id + "|" + BroadcastExposure.DayIndex(date) + "|" + c.Record.baseRecord.recordId + "|" + purpose));

	/// <summary>Finite calendars (§5.1): one booking per act per day, a rebooking gap per program, a cap on national dates a week.</summary>
	private static bool Available(RecordRuntimeData record, TvProgram program, GameDate date) {
		string artist = record.baseRecord.artistId;
		int today = BroadcastExposure.DayIndex(date);
		int nationalThisWeek = 0;
		if (!byArtist.TryGetValue(artist, out var mine)) return true;
		foreach (MediaAppearance a in mine) {
			if (a.RepeatOfId != null) continue;
			int day = BroadcastExposure.DayIndex(a.Date);
			if (day == today) return false;
			if (a.ProgramId == program.Id && today - day < 7 * program.RebookWeeks) return false;
			if (Math.Abs(today - day) < 7 && MediaOutletCatalog.Get(a.ProgramId)?.Network != TvNetwork.Local) nationalThisWeek++;
		}
		return program.Network == TvNetwork.Local || nationalThisWeek < MaxNationalPerActPerWeek;
	}

	// ---- tallies and history ---------------------------------------------------------------------------------------

	private static void TallyAiredThrough(GameDate weekEnd) {
		int cutoff = BroadcastExposure.DayIndex(weekEnd);
		double population = (ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>()).Sum(r => r.population) * 1e6;
		foreach (MediaAppearance a in state.Appearances) {
			foreach (MediaAiring airing in a.Airings) {
				if (airing.Tallied || BroadcastExposure.DayIndex(airing.Date) > cutoff) continue;
				airing.Tallied = true;
				if (!state.Tallies.TryGetValue(a.ArtistId, out MediaActTally t))
					state.Tallies[a.ArtistId] = t = new MediaActTally { ArtistId = a.ArtistId, ArtistName = a.ArtistName };
				t.ArtistName = a.ArtistName;
				if (!airing.Repeat && !airing.Syndicated) {
					t.Appearances++;
					if (MediaOutletCatalog.Get(a.ProgramId)?.Network == TvNetwork.Local) t.LocalAppearances++; else t.NationalAppearances++;
					t.LastProgramId = a.ProgramId; t.LastDate = a.Date;
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
