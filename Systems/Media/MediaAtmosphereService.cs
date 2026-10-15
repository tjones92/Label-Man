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
	public bool PlayerBooking;
	/// <summary>The advertised headliner of the episode (an announced hold), and the hype it carried into the air date.</summary>
	public bool Headliner;
	public float Hype, EventLift, TuneIn;
	/// <summary>The player's office has been told how it went (once-only receipt).</summary>
	public bool Reported;
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
			$"bestNight=\"{bestNight?.ArtistName}\" {MediaOutletCatalog.Get(bestNight?.ProgramId ?? "")?.Name} {(bestNight?.Airings[0].Exposure.UniqueReach ?? 0) / 1e6:F1}M hype={bestNight?.Hype ?? 0:F2}");
		// Phase 3: where the fans are, and what rival labels did with the envelopes they already had.
		var fans = state.Fans.Values.ToList();
		var loud = LoudestFans(1).FirstOrDefault();
		Godot.GD.Print($"MEDIA_FANS_YEAR year={year} acts={fans.Count} [{string.Join(" ", fans.GroupBy(f => f.State).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count()))}] " +
			$"everNational={fans.Count(f => IsNational(f.Peak))} everPhenomenon={fans.Count(f => f.Peak == FanEpisodeState.CulturalPhenomenon)} " +
			$"loudest=\"{loud?.ArtistName}\" {loud?.State} face=\"{MediaFans.Face(loud)}\"");
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
			foreach (RecordRuntimeData r in charts.GetAllRecords())
				if (r?.baseRecord?.recordId != null) ById[r.baseRecord.recordId] = r;
			var singles = charts.GetAllRecords().Where(Eligible).ToList();
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
			list = Singles.Where(r => DistanceModel.GetCityById(CityOf(r.baseRecord.artistId) ?? "")?.parentRegionId == regionId)
				.Select(r => (Record: r, Units: r.regionalData.TryGetValue(regionId, out var rd) && rd != null ? rd.unitsSoldThisWeek : 0))
				.Where(x => x.Units > 0).OrderByDescending(x => x.Units).ThenBy(x => x.Record.baseRecord.recordId, StringComparer.Ordinal)
				.Take(LocalCandidatesPerRegion / 2).ToList();
			regionalLocals[regionId] = list;
			return list;
		}

		public bool Broke(RecordRuntimeData r) => r.regionalData.Values.Any(rd => rd != null && rd.peakBreakoutScore >= BreakoutBar);
		public int BrokeCount(RecordRuntimeData r) => r.regionalData.Values.Count(rd => rd != null && rd.peakBreakoutScore >= BreakoutBar);
		public float Breadth(RecordRuntimeData r) => Regions.Count == 0 ? 0f : BrokeCount(r) / (float)Regions.Count;
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
	}

	private static void BookEpisode(TvProgram program, BroadcastScheduleVersion version, GameDate date, Evidence evidence) {
		string episodeId = program.Id + "|" + date.year + "-" + date.month.ToString("00") + "-" + date.day.ToString("00");
		if (!episodeIds.Add(episodeId)) return;
		var episode = new MediaEpisode { Id = episodeId, ProgramId = program.Id, Date = date, Repeat = version.RepeatMonths.Contains(date.month) };
		state.Episodes.Add(episode);
		if (episode.Repeat) { BookRepeat(program, version, episode); return; }

		int day = BroadcastExposure.DayIndex(date);
		List<Candidate> pool = Candidates(program, date, evidence);
		episodeDeclines = 0;
		var chosen = new List<Candidate>();
		int slots = program.GuestSlots;
		// The player's confirmed dates were reserved against this episode before it was booked (§5.1): they hold their slots.
		foreach (MediaPitch reserved in state.Pitches.Where(p => p.Status == PitchStatus.Confirmed && p.ProgramId == program.Id && p.AirDay == day)) {
			if (slots <= 0 || !evidence.ById.TryGetValue(reserved.RecordId ?? "", out RecordRuntimeData mine)) continue;
			chosen.Add(new Candidate { Record = mine, Score = 0.5f, Discovery = reserved.Discovery, Pitch = reserved });
			slots--;
		}
		// An announced headliner (§4.5 advertised guest) takes the bill's top spot.
		MediaHold hold = state.Holds.FirstOrDefault(h => h.ProgramId == program.Id && h.Day == day);
		if (hold != null && slots > 0 && evidence.ById.TryGetValue(hold.RecordId, out RecordRuntimeData held) && Available(held, program, date)) {
			var headliner = new Candidate { Record = held, Score = 10f, Headliner = true };
			// The label still has to say yes and afford the trip (§13); a declined headline date goes to the regular bill.
			if (AiAccepts(headliner, program, date, episodeId, evidence)) { chosen.Add(headliner); slots--; }
		}
		// The discovery slot: an eligible record below the top of the chart, chosen on fit and evidence, never guaranteed.
		// Rival labels answer the producer's call in turn; a label that says no passes the slot to the next in line.
		if (program.DiscoverySlot && slots > 0 && !chosen.Any(c => c.Discovery)) {
			Candidate pick = pool.Where(c => c.Discovery && Available(c.Record, program, date) && chosen.All(x => x.Record.baseRecord.artistId != c.Record.baseRecord.artistId))
				.OrderByDescending(c => Rank(c, program, date, "discovery")).FirstOrDefault(c => AiAccepts(c, program, date, episodeId, evidence));
			if (pick != null) { chosen.Add(pick); slots--; }
		}
		var queue = pool.Where(c => !c.Discovery).OrderByDescending(c => Rank(c, program, date, "slot")).ToList();
		int next = 0;
		void Fill() {
			while (slots > 0 && next < queue.Count) {
				Candidate c = queue[next++];
				if (chosen.Any(x => x.Record.baseRecord.artistId == c.Record.baseRecord.artistId) || !Available(c.Record, program, date)) continue;
				if (!AiAccepts(c, program, date, episodeId, evidence)) continue;
				chosen.Add(c); slots--;
			}
		}
		Fill();

		// Standards review (§7.2) before the bill is final: a rival act that withdraws frees its slot for the next in line.
		// The player's queries were raised and answered when the date was offered.
		foreach (Candidate c in chosen.ToList()) {
			if (c.Pitch != null) continue;
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
				chosen.Remove(c); slots++;
				ReleaseReservation(c, episodeId);
				Remember(program.Id, c.Record.baseRecord.artistId, m => { m.Withdrawn++; m.LastNote = "withdrew rather than change the song"; });
			}
		}
		Fill();
		// Telemetry: how much of the finite bill the combined pool filled (player, rivals who said yes).
		Ledger(program.Network == TvNetwork.Local ? "slots-local-filled" : "slots-network-filled", chosen.Count);
		if (slots > 0) Ledger((program.Network == TvNetwork.Local ? "slots-local-empty" : "slots-network-empty") + (episodeDeclines > 0 ? "-declined" : "-nopool"), slots);
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
				Title = record.baseRecord.title, LabelId = record.baseRecord.labelId, Genre = record.baseRecord.primaryGenre,
				ChartPosition = record.currentPosition, Discovery = c.Discovery, Method = program.Method, Broadcast = version.Broadcast,
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
			if (!appearance.Cut) appearance.Performance = MediaPerformance.Resolve(appearance.Id, appearance.Method, appearance.Broadcast, act, appearance.Preparation);
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
			// The region's own acts, further down the reports: cheap to bring in, and a local show's natural bench (§3.1).
			foreach (var (record, units) in evidence.RegionalLocals(region)) {
				if (list.Any(c => c.Record == record)) continue;
				bool hometown = evidence.CityOf(record.baseRecord.artistId) == program.HomeCityId;
				float score = program.GenreFit(record.baseRecord.primaryGenre) * (0.3f + 0.5f * units / (float)Math.Max(1, top)) * (hometown ? 1.6f : 1f);
				list.Add(new Candidate { Record = record, Score = score, Discovery = hometown || record.currentPosition == 0 || record.currentPosition > 60 });
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

	/// <summary>Finite calendars (§5.1): one booking per act per day, a rebooking gap per program, a cap on national dates a week.</summary>
	private static bool Available(RecordRuntimeData record, TvProgram program, GameDate date) {
		string artist = record.baseRecord.artistId;
		int today = BroadcastExposure.DayIndex(date);
		int nationalThisWeek = 0;
		// A breached agreement shuts that program's door for a while (§7.3); the network's other shows only grow wary
		// for a year, never an industry-wide blacklist (§2).
		if (state.ProgramMemory.TryGetValue(program.Id + "|" + artist, out var memory) && memory.ExcludedUntilDay > today) return false;
		if (program.Network != TvNetwork.Local && state.ProgramMemory.TryGetValue("network:" + program.Network + "|" + artist, out var review)
			&& review.ExcludedUntilDay > today && MediaAtmosphere.Unit("network-review|" + program.Id + "|" + artist + "|" + today) < 0.5f) return false;
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
