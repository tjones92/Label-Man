using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 2 of the media directive, observe-only: what happens after a segment airs, announced headline dates and the hype
/// they gather, and the weekly press pass. Every write lands in <see cref="MediaSaveData"/>; nothing here is read by the
/// economy, no global stream is drawn, and no musician's private state is consulted.
/// </summary>
public static partial class MediaAtmosphereService {
	internal static MediaSaveData State => state;

	private const int HoldLeadMinDays = 15, HoldLeadMaxDays = 28;
	private const float HoldPrecursorBar = 0.30f;
	private const int StoryHistoryDays = 7 * 26;
	// Rival items are kept only as long as anything reads them (the six-week hype window); the label's own clippings keep 26 weeks.
	private const int RivalStoryDays = 7 * 7;

	// ---- after the air date ---------------------------------------------------------------------------------------

	/// <summary>Consequences that wait for the broadcast (§4.5: booked, recorded, cleared, transmitted are different states).
	/// Runs once per original airing, from the weekly tally.</summary>
	internal static void AfterAir(MediaAppearance a, MediaAiring airing) {
		TvProgram program = MediaOutletCatalog.Get(a.ProgramId);
		if (program == null) return;
		int day = BroadcastExposure.DayIndex(a.Date);
		bool national = program.Network != TvNetwork.Local;
		StandardsQuery q = a.StandardsQueryId == null ? null : state.StandardsQueries.FirstOrDefault(x => x.Id == a.StandardsQueryId);
		if (q != null) {
			switch (q.Resolution) {
				case StandardsResolution.Defied:
					Remember(program.Id, a.ArtistId, m => { m.Breaches++; m.ExcludedUntilDay = day + 3 * 365; m.LastNote = "sang the line they'd agreed to change"; });
					if (national) Remember("network:" + program.Network, a.ArtistId, m => { m.Breaches++; m.ExcludedUntilDay = day + 365; m.LastNote = "network review after a broken agreement"; });
					q.Public = true;
					// The same act can win the artistic audience and lose the family one at once (§0.2).
					Attitude(a.ArtistId, 0.15f, -0.25f, 0.02f);
					state.Controversies.Add(new MediaControversy {
						Id = "controversy|" + q.Id, RootId = "controversy|" + q.Id, ArtistId = a.ArtistId, ArtistName = a.ArtistName, StartDay = day, ExpiresDay = day + 84,
						Severity = national ? 0.6f : 0.25f, National = national, Verified = true,
						Summary = $"sang the original line of \"{a.OriginalTitle ?? a.Title}\" on {program.Name} after agreeing to change it",
					});
					break;
				case StandardsResolution.Cut:
					Remember(program.Id, a.ArtistId, m => { m.Breaches++; m.ExcludedUntilDay = day + 3 * 365; m.LastNote = "broke the agreement at the taping; segment cut"; });
					q.Public = MediaStandards.Leaks(q, program, a.ChartPosition, q.Resolution);
					if (q.Public) Attitude(a.ArtistId, 0.06f, -0.08f, 0f);
					break;
				case StandardsResolution.Edited:
					q.Public = MediaStandards.Leaks(q, program, a.ChartPosition, q.Resolution);
					// A compromise costs artistic credibility only when the audience that cares learns of it (§7.3).
					if (q.Public) Attitude(a.ArtistId, -0.08f, 0.02f, 0f);
					break;
			}
		}
		if (a.Cut) return;
		Remember(program.Id, a.ArtistId, m => {
			if (q?.Resolution != StandardsResolution.Defied) m.Delivered++;
			if (q?.Resolution != StandardsResolution.Defied) m.LastNote = a.Performance?.Outcome switch {
				PerformanceOutcome.Electrifying => "brought the house down",
				PerformanceOutcome.Breakdown => a.Performance.Fault == PerformanceFault.Equipment ? "lost the segment to a technical fault" : "came apart on the air",
				PerformanceOutcome.Weak => "a weak segment",
				_ => "arrived on time and delivered",
			};
		});
		if (a.Performance == null || !national) return;
		bool live = !a.Performance.SoundFromMaster;
		if (a.Performance.Outcome == PerformanceOutcome.Electrifying)
			Attitude(a.ArtistId, live ? 0.04f : 0f, 0.02f, program.Kind == TvProgramKind.NationalTeen ? 0.05f : 0.02f);
		else if (a.Performance.Outcome == PerformanceOutcome.Breakdown && a.Broadcast == BroadcastMethod.Live && a.Performance.Fault != PerformanceFault.Equipment)
			Attitude(a.ArtistId, -0.04f, -0.03f, -0.01f);
	}

	/// <summary>Media's own attitude record (§2 mediaCredibility): small, signed, no reader outside media.</summary>
	private static void Attitude(string artistId, float artistic, float family, float teen) {
		if (artistId == null) return;
		if (!state.Credibility.TryGetValue(artistId, out float[] v)) state.Credibility[artistId] = v = new float[3];
		v[0] = Math.Clamp(v[0] + artistic, -1f, 1f);
		v[1] = Math.Clamp(v[1] + family, -1f, 1f);
		v[2] = Math.Clamp(v[2] + teen, -1f, 1f);
	}

	/// <summary>Attitudes move 10% a week toward neutral (§2 initial value).</summary>
	private static void DecayAttitudes(GameDate weekEnd) {
		foreach (string key in state.Credibility.Keys.ToList()) {
			float[] v = state.Credibility[key];
			for (int i = 0; i < v.Length; i++) v[i] *= 0.9f;
			if (v.All(x => Math.Abs(x) < 0.005f)) state.Credibility.Remove(key);
		}
		foreach (var contact in state.Contacts.Values) contact.Trust *= 0.995f;
		int today = BroadcastExposure.DayIndex(weekEnd);
		int floor = today - StoryHistoryDays;
		state.Stories.RemoveAll(s => (s.Status == StoryStatus.Killed || s.Status == StoryStatus.Lead) && s.LeadDay < today - MediaPress.LeadShelfDays
			|| s.Status == StoryStatus.Scheduled && s.IssueDay < (s.PlayerSubject ? floor : today - RivalStoryDays));
		state.StandardsQueries.RemoveAll(q => BroadcastExposure.DayIndex(q.AirDate) < floor);
		state.Controversies.RemoveAll(c => c.ExpiresDay < floor);
		state.Holds.RemoveAll(h => h.Day < today);
		state.Pitches.RemoveAll(p => p.SentDay < floor && p.Status != PitchStatus.Confirmed && p.Status != PitchStatus.Offered);
		state.Inquiries.RemoveAll(i => i.Day < floor);
		state.Exclusives.RemoveAll(e => e.UntilDay < floor);
	}

	public static float[] AttitudeOf(string artistId) => artistId != null && state.Credibility.TryGetValue(artistId, out var v) ? v : new float[3];
	public static MediaProgramMemory ProgramMemoryFor(string programId, string artistId) =>
		state.ProgramMemory.TryGetValue(programId + "|" + artistId, out var m) ? m : null;
	public static IReadOnlyList<StandardsQuery> StandardsQueries => state.StandardsQueries;
	public static IReadOnlyList<MediaStory> Stories => state.Stories;
	public static IReadOnlyList<MediaControversy> Controversies => state.Controversies;
	public static IReadOnlyList<MediaHold> Holds => state.Holds;

	// ---- chart memory -------------------------------------------------------------------------------------------------

	private static void UpdateChartMemory(Evidence evidence) {
		var onChart = new HashSet<string>(evidence.FullChart.Select(r => r.baseRecord.recordId), StringComparer.Ordinal);
		foreach (RecordRuntimeData r in evidence.FullChart)
			if (r.currentPosition == 1) {
				state.WeeksAtNumberOne[r.baseRecord.recordId] = state.WeeksAtNumberOne.GetValueOrDefault(r.baseRecord.recordId) + 1;
				// What an ordinary No. 1 sells, as a slow running average (about a half-year memory).
				state.NumberOneUnits = state.NumberOneUnits <= 0 ? r.unitsThisWeek : 0.96f * state.NumberOneUnits + 0.04f * r.unitsThisWeek;
			}
		foreach (string id in state.WeeksAtNumberOne.Keys.ToList())
			if (!onChart.Contains(id)) state.WeeksAtNumberOne.Remove(id);
		// A breakout is news once; forget records that have left the market so the set stays small.
		state.BreakoutsReported.RemoveWhere(k => {
			int bar = k.IndexOf('|');
			string record = bar < 0 ? k : k.Substring(0, bar);
			return !evidence.ById.TryGetValue(record, out var rr) || rr.unitsThisWeek <= 0 && rr.currentPosition <= 0;
		});
	}

	// ---- announced dates and hype -------------------------------------------------------------------------------------

	/// <summary>
	/// How much a record looks like an event before anyone has written about it: weeks at No. 1, a top-three place, a
	/// second hit at once, and how many of the country's regional markets it has actually broken in. All read from the
	/// chart and the regional sales the market already settled.
	/// </summary>
	public static float Precursor(RecordRuntimeData record, IReadOnlyList<RecordRuntimeData> chart, int regions, float breakoutBar) {
		if (record?.baseRecord == null || regions <= 0) return 0f;
		int pos = record.currentPosition;
		if (pos <= 0 || pos > 10) return 0f;
		int atOne = state.WeeksAtNumberOne.GetValueOrDefault(record.baseRecord.recordId);
		// A reign: an ordinary No. 1 holds two or three weeks; six is a season.
		float reign = Math.Min(1f, atOne / 6f) * 0.75f + (pos <= 3 ? 0.25f : 0.1f);
		// Size: selling far past what an ordinary No. 1 sells (media's own running average), not merely being No. 1.
		float ordinary = state.NumberOneUnits <= 0 ? 0f : record.unitsThisWeek / state.NumberOneUnits;
		float size = Smooth((ordinary - 1.0f) / 1.0f);
		// Several hits at once: the rarest signature of all.
		int otherHits = chart.Count(r => r != record && r.baseRecord.artistId == record.baseRecord.artistId && r.currentPosition > 0 && r.currentPosition <= 20);
		float many = Math.Min(1f, otherHits / 2f);
		int broke = record.regionalData.Values.Count(rd => rd != null && rd.peakBreakoutScore >= breakoutBar);
		return (0.45f * reign + 0.35f * size + 0.20f * many) * Math.Min(1f, broke / (float)regions);
	}

	private static float Precursor(RecordRuntimeData record, Evidence evidence) =>
		Precursor(record, evidence.FullChart, evidence.Regions.Count, evidence.BreakoutBar);

	/// <summary>
	/// Hype at the air date (the user-requested route to a Beatles-sized night): the precursor, times novelty (a first visit
	/// to this program, and to primetime at all, is an event; a return visit isn't), times advance coverage, counted as
	/// distinct press families with items running before the air date. One root counts once per family, so ten papers
	/// repeating the booking announcement are one family's worth, not ten (§9.4). Phase 3's fan episodes join it there.
	/// </summary>
	private static float Hype(RecordRuntimeData record, TvProgram program, int airDay, Evidence evidence) {
		float precursor = Precursor(record, evidence);
		MediaActTally t = TallyFor(record.baseRecord.artistId);
		bool seenHere = t?.ProgramsSeen.Contains(program.Id) == true;
		bool seenPrimetime = t?.ProgramsSeen.Any(p => MediaOutletCatalog.Get(p)?.Kind == TvProgramKind.PrimetimeVariety) == true;
		float novelty = (seenHere ? 0.4f : 1f) * (seenPrimetime ? 0.85f : 1f);
		float hype = precursor * novelty * (0.3f + 0.7f * Coverage(record.baseRecord.artistId, airDay));
		// Phase 3 gate: tune-in and the ceiling belong to acts the public already behaves toward as a sensation or a phenomenon.
		return Math.Min(hype, MediaFans.HypeCap(EpisodeOf(record.baseRecord.artistId)));
	}

	/// <summary>Advance coverage in [0,1]: distinct press families running an item about the act in the six weeks before
	/// the air date, with mainstream magazines counting twice (they reach the parents).</summary>
	public static float Coverage(string artistId, int airDay) {
		var families = state.Stories.Where(s => s.ArtistId == artistId && s.Status == StoryStatus.Scheduled && s.IssueDay >= airDay - 42 && s.IssueDay < airDay)
			.Select(s => MediaOutletCatalog.GetPress(s.OutletId)?.Family).Where(f => f != null).Distinct().ToList();
		int weight = families.Count + (families.Contains(PressFamily.Mainstream) ? 1 : 0);
		return Math.Min(1f, weight / 5f);
	}

	/// <summary>
	/// What hype does to an episode's audience: share taken from competing programs (up to <see cref="BroadcastExposure.MaxEventLift"/>)
	/// and, at the very top, people who would not have watched at all switching on (up to <see cref="BroadcastExposure.MaxTuneIn"/>
	/// of the absent). Routine bookings sit at zero; it takes a conjunction of rare things to approach the ceiling, and a
	/// world may never produce one. Local shows have no event lift; network teen shows have a smaller one.
	/// </summary>
	public static (float Lift, float TuneIn) EventDraw(float hype, TvProgram program) {
		if (program == null || program.Network == TvNetwork.Local || hype <= 0f) return (0f, 0f);
		float scale = program.Kind == TvProgramKind.PrimetimeVariety ? 1f : 0.6f;
		float lift = BroadcastExposure.MaxEventLift * Smooth((hype - 0.25f) / 0.65f) * scale;
		float tune = BroadcastExposure.MaxTuneIn * Smooth((hype - 0.50f) / 0.45f) * scale;
		return (lift, tune);
	}

	private static float Smooth(float x) { x = Math.Clamp(x, 0f, 1f); return x * x * (3f - 2f * x); }

	/// <summary>
	/// Network bookers announce a headline date weeks ahead for a record that already looks like an event (§4.5 advertised
	/// guest). The announcement is what gives the press time to build the night. Rival acts only; one hold per program in
	/// the window, one per act anywhere.
	/// </summary>
	private static void PlaceHolds(Evidence evidence, GameDate weekEnd) {
		int today = BroadcastExposure.DayIndex(weekEnd);
		foreach (TvProgram program in MediaOutletCatalog.Programs) {
			if (!program.AnnouncesHeadliners || program.BillOn(FromDayIndex(today)).Chart == 0) continue;
			if (state.Holds.Any(h => h.ProgramId == program.Id && h.Day > today)) continue;
			int target = -1;
			for (int d = today + HoldLeadMinDays; d <= today + HoldLeadMaxDays; d++) {
				GameDate date = FromDayIndex(d);
				var version = program.VersionOn(date);
				if (version == null || version.RepeatMonths.Contains(date.month)) continue;
				// Primetime only: an afternoon dance show doesn't stage an event night.
				if (version.StartMinute < 19 * 60) continue;
				target = d; break;
			}
			if (target < 0) continue;
			RecordRuntimeData best = null;
			float bestScore = 0f;
			foreach (RecordRuntimeData r in evidence.Chart) {
				if (r.currentPosition <= 0 || r.currentPosition > 10) continue;
				string artist = r.baseRecord.artistId;
				if (state.Holds.Any(h => h.ArtistId == artist && h.Day > today)) continue;
				if (state.ProgramMemory.TryGetValue(program.Id + "|" + artist, out var m) && m.ExcludedUntilDay > target) continue;
				if (state.Appearances.Any(a => a.ArtistId == artist && a.ProgramId == program.Id && a.RepeatOfId == null
					&& target - BroadcastExposure.DayIndex(a.Date) < 7 * program.RebookWeeks)) continue;
				float fit = program.GenreFit(r.baseRecord.primaryGenre);
				if (fit < 0.6f) continue;
				float precursor = Precursor(r, evidence);
				if (precursor < HoldPrecursorBar) continue;
				// Announce only a date the label would take (§13): a specialist or artistic house can turn a booker down.
				MediaPolicy? policy = PolicyOf(r.baseRecord.labelId, artist, today);
				if (policy != null && !AiMediaPlanner.Considers(program, policy.Value, ArtistManager.Instance?.GetArtist(artist), r.baseRecord.primaryGenre, target, out _)) continue;
				float score = precursor * fit * (0.9f + 0.2f * MediaAtmosphere.Unit("hold|" + program.Id + "|" + target + "|" + r.baseRecord.recordId));
				if (score > bestScore) { bestScore = score; best = r; }
			}
			if (best == null) continue;
			state.Holds.Add(new MediaHold {
				ProgramId = program.Id, ArtistId = best.baseRecord.artistId, ArtistName = best.baseRecord.artistName, RecordId = best.baseRecord.recordId,
				LabelId = best.baseRecord.labelId, Day = target, PlacedDay = today, Precursor = Precursor(best, evidence),
			});
		}
	}

	// ---- the press pass -----------------------------------------------------------------------------------------------

	private static IEnumerable<string> Outlets(PressFamily family) =>
		MediaOutletCatalog.Press.Where(p => p.Family == family).Select(p => p.Id);

	private static string DailyFor(string cityId) => cityId == null ? null : "daily_" + cityId;

	/// <summary>City papers in a region: its dailies and its community press.</summary>
	private static IEnumerable<string> RegionPapers(string regionId, bool specialistOnly) =>
		MediaOutletCatalog.Press.Where(p => p.HomeCityId != null && DistanceModel.GetCityById(p.HomeCityId)?.parentRegionId == regionId
			&& (specialistOnly ? p.Family == PressFamily.Specialist : true)).Select(p => p.Id);

	private static MediaPress.Lead NewLead(string root, StoryAngle angle, RecordRuntimeData record, int day, float interest, Evidence evidence) {
		Record rec = record?.baseRecord;
		var story = new MediaStory {
			Id = root, RootId = root, Angle = angle, Status = StoryStatus.Lead, LeadDay = day, Interest = interest,
			ArtistId = rec?.artistId, ArtistName = rec?.artistName, RecordId = rec?.recordId, Title = rec?.title, LabelId = rec?.labelId,
			Genre = rec?.primaryGenre ?? Genre.TraditionalPop, ChartPosition = record?.currentPosition ?? 0, PlayerSubject = rec?.isPlayerOwned == true,
			CityId = rec?.artistId == null ? null : evidence.CityOf(rec.artistId),
		};
		return new MediaPress.Lead { Story = story };
	}

	private static void Fact(MediaPress.Lead lead, EvidenceSource source, string fact, int day, bool firsthand = true, float confidence = 1f) =>
		lead.Story.Evidence.Add(new MediaEvidence { Source = source, Fact = fact, Firsthand = firsthand, Day = day, Confidence = confidence });

	/// <summary>
	/// The weekly press pass (§8-§9.4): public evidence becomes leads, editors spend their page budgets, items are scheduled
	/// into dated issues. Every lead is built from the chart, regional sales, an actual broadcast, a witnessed public
	/// incident, a booking announcement or the label's own pitch. Nothing private enters.
	/// </summary>
	private static void PressWeek(Evidence evidence, GameDate weekEnd) {
		int today = BroadcastExposure.DayIndex(weekEnd) + 1;
		int year = weekEnd.year;
		var leads = new List<MediaPress.Lead>();
		bool criticsActive(PressOutlet o) => o.Publishing(weekEnd);

		// Chart news: a new No. 1, a top-40 debut, a jump into the top ten. Trades first; the act's home-town paper wants it too.
		foreach (RecordRuntimeData r in evidence.FullChart) {
			int pos = r.currentPosition, last = r.lastWeekPosition;
			if (pos <= 0) continue;
			bool newOne = pos == 1 && last != 1, bigDebut = last <= 0 && pos <= 40, topTen = pos <= 10 && (last <= 0 || last > 10), debut = last <= 0;
			if (!newOne && !bigDebut && !topTen && !debut) continue;
			float interest = newOne ? 0.95f : topTen ? 0.6f : bigDebut ? 0.5f : 0.3f;
			var lead = NewLead("chart|" + r.baseRecord.recordId + "|" + today, StoryAngle.ChartNews, r, today, interest, evidence);
			Fact(lead, EvidenceSource.NationalChart, newOne ? "No. 1 on the national chart this week" : debut ? $"enters the national chart at No. {pos}" : $"up to No. {pos} on the national chart", today);
			lead.Outlets.AddRange(Outlets(PressFamily.Trade));
			if (pos <= 40 && DailyFor(lead.Story.CityId) is string daily) lead.Outlets.Add(daily);
			if (pos <= 30) lead.Outlets.AddRange(Outlets(PressFamily.Specialist).Where(id => MediaOutletCatalog.GetPress(id).National));
			leads.Add(lead);
		}

		// Breakouts: newly breaking regional markets, from the sales reports the market already settled.
		foreach (RecordRuntimeData r in evidence.ById.Values) {
			if (r.baseRecord.format != ReleaseFormat.Single || r.unitsThisWeek <= 0 || string.IsNullOrEmpty(r.baseRecord.artistId)) continue;
			var fresh = new List<string>();
			foreach (var pair in r.regionalData)
				if (pair.Value != null && pair.Value.peakBreakoutScore >= evidence.BreakoutBar && state.BreakoutsReported.Add(r.baseRecord.recordId + "|" + pair.Key)) fresh.Add(pair.Key);
			if (fresh.Count == 0) continue;
			int total = evidence.BrokeCount(r);
			var lead = NewLead("breakout|" + r.baseRecord.recordId + "|" + today, StoryAngle.Breakout, r, today, Math.Min(0.9f, 0.35f + 0.1f * fresh.Count + 0.03f * total), evidence);
			lead.Story.Regions = total;
			string names = string.Join(", ", fresh.Take(3).Select(id => evidence.Regions.FirstOrDefault(x => x.regionId == id)?.regionName ?? id));
			Fact(lead, EvidenceSource.RegionalSales, $"breaking in {names} on dealers' sales reports", today);
			lead.Outlets.AddRange(Outlets(PressFamily.Trade));
			foreach (string region in fresh) lead.Outlets.AddRange(RegionPapers(region, specialistOnly: true));
			if (DailyFor(lead.Story.CityId) is string daily) lead.Outlets.Add(daily);
			// The critics hear about scenes before the chart does.
			lead.Outlets.AddRange(MediaOutletCatalog.Press.Where(p => p.Family == PressFamily.RockCriticism && p.HomeCityId != null
				&& fresh.Contains(DistanceModel.GetCityById(p.HomeCityId)?.parentRegionId ?? "")).Select(p => p.Id));
			leads.Add(lead);
		}

		// Television that aired this week: a notice in the act's home paper and the host city's; teen editors watch the teen
		// shows; mainstream editors notice only a very large night or a public calamity.
		int weekStart = today - 7;
		foreach (MediaAppearance a in state.Appearances) {
			int day = BroadcastExposure.DayIndex(a.Date);
			if (day < weekStart || day >= today || a.RepeatOfId != null || a.Cut || a.Airings.Count == 0) continue;
			TvProgram program = MediaOutletCatalog.Get(a.ProgramId);
			if (program == null) continue;
			evidence.ById.TryGetValue(a.RecordId ?? "", out RecordRuntimeData r);
			double reach = a.Airings[0].Exposure.UniqueReach;
			PerformanceOutcome outcome = a.Performance?.Outcome ?? PerformanceOutcome.FlatAdequate;
			bool good = outcome >= PerformanceOutcome.Engaging;
			string root = "tv|" + a.Id;
			string seen = $"seen by an estimated {MediaText.Audience(reach)} on {MediaOutletCatalog.DisplayName(program, a.Date)}";
			var lead = NewLead(root, StoryAngle.TvNotice, r, today, 0.2f + (good ? 0.2f : 0f) + (outcome == PerformanceOutcome.Electrifying ? 0.2f : 0f), evidence);
			lead.Story.ArtistId = a.ArtistId; lead.Story.ArtistName = a.ArtistName; lead.Story.Title = a.Title; lead.Story.ProgramId = a.ProgramId; lead.Story.AppearanceId = a.Id;
			lead.Story.PlayerSubject = a.PlayerBooking || lead.Story.PlayerSubject;
			lead.Story.Tone = outcome == PerformanceOutcome.Electrifying ? StoryTone.Admiring : outcome <= PerformanceOutcome.Weak ? StoryTone.Skeptical : StoryTone.Neutral;
			Fact(lead, EvidenceSource.BroadcastAiring, seen + (a.Performance != null ? ": " + a.Performance.Account : ""), day);
			bool hometown = lead.Story.CityId != null && lead.Story.CityId == program.HomeCityId;
			if (program.Network == TvNetwork.Local) {
				if (hometown) lead.Story.Interest += 0.3f;
				lead.Outlets.Add(DailyFor(program.HomeCityId));
			} else {
				lead.Story.Interest += 0.15f;
				if (DailyFor(lead.Story.CityId) is string daily) { lead.Outlets.Add(daily); }
				if (good) lead.Outlets.AddRange(Outlets(PressFamily.Trade));
				if (program.Kind == TvProgramKind.NationalTeen && good && MediaOutletCatalog.TeenFit(a.Genre) >= 0.8f) lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
				lead.Outlets.AddRange(Outlets(PressFamily.Specialist).Where(id => MediaOutletCatalog.GetPress(id).National));
				// The news magazines notice a live disaster only when it happens to a name their readers know.
				float recognition = ArtistRecognitionService.EffectiveRecognition(ArtistManager.Instance?.GetArtist(a.ArtistId));
				bool famous = a.ChartPosition is > 0 and <= 10 || recognition >= 0.45f;
				// A star's night going wrong is news everywhere, the way a Beatle or a Doors appearance was.
				bool star = a.ChartPosition is > 0 and <= 3 || recognition >= 0.70f
					|| EpisodeOf(a.ArtistId) is FanEpisodeState.NationalSensation or FanEpisodeState.CulturalPhenomenon;
				bool calamity = outcome == PerformanceOutcome.Breakdown && a.Broadcast == BroadcastMethod.Live && reach > 15e6 && famous;
				if (calamity || a.Headliner && reach > 40e6) {
					lead.Story.Interest += calamity ? 0.6f : 0.9f;
					lead.Story.Angle = calamity ? StoryAngle.TvNotice : StoryAngle.CulturalPhenomenon;
					lead.Story.Tone = calamity ? StoryTone.Curious : StoryTone.Curious;
					// A lesser name's disaster is a paragraph in one news magazine; a star's runs in all of them.
					var mainstream = Outlets(PressFamily.Mainstream).ToList();
					if (calamity && !star && mainstream.Count > 0)
						lead.Outlets.Add(mainstream[(int)(MediaAtmosphere.Unit("calamity-desk|" + a.Id) * mainstream.Count) % mainstream.Count]);
					else lead.Outlets.AddRange(mainstream);
				}
			}
			leads.Add(lead);
		}

		// Announced headline dates (§4.5): the advance items that build an event night.
		foreach (MediaHold hold in state.Holds.Where(h => h.Day > today)) {
			TvProgram program = MediaOutletCatalog.Get(hold.ProgramId);
			if (program == null || !evidence.ById.TryGetValue(hold.RecordId, out RecordRuntimeData r)) continue;
			float precursor = Precursor(r, evidence);
			var lead = NewLead("hold|" + hold.ProgramId + "|" + hold.ArtistId + "|" + hold.Day, StoryAngle.TvAdvance, r, today, 0.45f + 0.5f * precursor, evidence);
			lead.Story.ProgramId = hold.ProgramId;
			Fact(lead, EvidenceSource.LabelStatement, $"booked to headline {program.Name} on {FromDayIndex(hold.Day).ToShortString()} (network announcement)", hold.PlacedDay, firsthand: false);
			Fact(lead, EvidenceSource.NationalChart, $"currently No. {r.currentPosition}", today);
			lead.Outlets.AddRange(Outlets(PressFamily.Trade));
			if (DailyFor(lead.Story.CityId) is string daily) lead.Outlets.Add(daily);
			if (MediaOutletCatalog.TeenFit(r.baseRecord.primaryGenre) >= 0.6f) lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
			lead.Outlets.AddRange(Outlets(PressFamily.Specialist).Where(id => MediaOutletCatalog.GetPress(id).National));
			if (precursor >= 0.6f) lead.Outlets.AddRange(Outlets(PressFamily.Mainstream));
			leads.Add(lead);
		}

		// Public standards disputes and controversy (§7.4): local report, wider pickup, institutional reaction.
		foreach (StandardsQuery q in state.StandardsQueries) {
			if (!q.Public || q.Resolution == StandardsResolution.Open || BroadcastExposure.DayIndex(q.AirDate) < weekStart || BroadcastExposure.DayIndex(q.AirDate) >= today) continue;
			TvProgram program = MediaOutletCatalog.Get(q.ProgramId);
			evidence.ById.TryGetValue(q.RecordId ?? "", out RecordRuntimeData r);
			bool defied = q.Resolution == StandardsResolution.Defied;
			var lead = NewLead((defied ? "controversy|" : "standards|") + q.Id, defied ? StoryAngle.Controversy : StoryAngle.StandardsDispute, r, today,
				defied ? 0.8f : 0.45f, evidence);
			lead.Story.ArtistId = q.ArtistId; lead.Story.ArtistName = q.ArtistName; lead.Story.Title = q.Title; lead.Story.ProgramId = q.ProgramId;
			lead.Story.PlayerSubject = q.PlayerQuery || lead.Story.PlayerSubject;
			lead.Story.Tone = defied ? StoryTone.Alarmed : q.Resolution == StandardsResolution.Edited ? StoryTone.Skeptical : StoryTone.Neutral;
			Fact(lead, defied ? EvidenceSource.PublicIncident : EvidenceSource.OtherPublication,
				$"{q.Authority} objected to {q.Evidence} in \"{q.Title}\". {q.Outcome}", BroadcastExposure.DayIndex(q.AirDate), firsthand: defied);
			lead.Outlets.AddRange(Outlets(PressFamily.Trade));
			if (program?.Network != TvNetwork.Local) {
				if (defied) lead.Outlets.AddRange(Outlets(PressFamily.Mainstream));
				lead.Outlets.AddRange(Outlets(PressFamily.RockCriticism).Where(id => criticsActive(MediaOutletCatalog.GetPress(id))));
				if (defied) lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
			}
			if (DailyFor(lead.Story.CityId) is string daily) lead.Outlets.Add(daily);
			leads.Add(lead);
		}

		// Teen features: a teen-fit act with a current hit and a face the readers have seen on television.
		foreach (RecordRuntimeData r in evidence.FullChart) {
			if (r.currentPosition <= 0 || r.currentPosition > 20 || MediaOutletCatalog.TeenFit(r.baseRecord.primaryGenre) < 0.8f) continue;
			MediaActTally t = TallyFor(r.baseRecord.artistId);
			int teenShows = t?.ProgramsSeen.Count(p => MediaOutletCatalog.Get(p)?.Kind is TvProgramKind.NationalTeen or TvProgramKind.LocalTeenDance) ?? 0;
			if (teenShows == 0) continue;
			// Editors know which faces generate mail (§8.2): a following lifts the feature, and the letters name a member.
			MediaFanState fans = FanStateFor(r.baseRecord.artistId);
			float following = fans?.State switch {
				FanEpisodeState.CulturalPhenomenon => 0.4f, FanEpisodeState.NationalSensation => 0.3f, FanEpisodeState.RegionalFavorite => 0.15f, _ => 0f,
			};
			var lead = NewLead("teen|" + r.baseRecord.artistId + "|" + weekEnd.year + "-" + weekEnd.month, StoryAngle.TeenFeature, r, today,
				0.3f + 0.12f * Math.Min(3, teenShows) + (r.currentPosition <= 5 ? 0.2f : 0f) + following, evidence);
			lead.Story.Tone = StoryTone.Admiring;
			lead.Story.Face = MediaFans.Face(fans);
			lead.Story.FanState = fans?.State ?? FanEpisodeState.None;
			Fact(lead, EvidenceSource.BroadcastAiring, $"seen on {teenShows} teen program(s)", today);
			Fact(lead, EvidenceSource.NationalChart, $"No. {r.currentPosition} this week", today);
			lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
			leads.Add(lead);
		}

		// Cultural salience (§8.3): mainstream editors cover popular music only when the evidence is extraordinary.
		foreach (RecordRuntimeData r in evidence.FullChart.Where(x => x.currentPosition >= 1 && x.currentPosition <= 3)) {
			float precursor = Precursor(r, evidence);
			if (precursor < 0.65f || state.WeeksAtNumberOne.GetValueOrDefault(r.baseRecord.recordId) < 4) continue;
			// A chart run alone is one evidence family; the mainstream assignment waits for the fans to be part of the story (§10.2).
			if (!IsNational(EpisodeOf(r.baseRecord.artistId))) continue;
			var lead = NewLead("phenomenon|" + r.baseRecord.artistId + "|" + year + "q" + ((weekEnd.month - 1) / 3), StoryAngle.CulturalPhenomenon, r, today,
				0.9f + 0.5f * (precursor - 0.65f) / 0.35f, evidence);
			float u = MediaAtmosphere.Unit("phenomenon-tone|" + lead.Story.Id);
			lead.Story.Tone = u < 0.4f ? StoryTone.Curious : u < 0.7f ? StoryTone.Admiring : StoryTone.Alarmed;
			Fact(lead, EvidenceSource.NationalChart, $"{state.WeeksAtNumberOne[r.baseRecord.recordId]} weeks at No. 1", today);
			Fact(lead, EvidenceSource.RegionalSales, $"breaking in {evidence.BrokeCount(r)} of {evidence.Regions.Count} regional markets", today);
			lead.Outlets.AddRange(Outlets(PressFamily.Mainstream));
			lead.Outlets.AddRange(Outlets(PressFamily.TeenFan));
			leads.Add(lead);
		}

		// Reviews: critics respond to the record itself (§8.4). The Voice's music coverage grows through the decade.
		foreach (RecordRuntimeData r in evidence.FullChart) {
			if (r.lastWeekPosition > 0 || MediaOutletCatalog.CriticFit(r.baseRecord.primaryGenre) < 0.7f) continue;
			float craft = (r.baseRecord.hookStrength + r.baseRecord.productionQuality) * 0.5f;
			var lead = NewLead("review|" + r.baseRecord.recordId, StoryAngle.Review, r, today, 0.35f + 0.1f * Math.Clamp(year - 1964, 0, 4), evidence);
			Fact(lead, EvidenceSource.RecordItself, "the record", today, confidence: craft);
			lead.Outlets.AddRange(Outlets(PressFamily.RockCriticism).Where(id => criticsActive(MediaOutletCatalog.GetPress(id))));
			lead.Outlets.AddRange(Outlets(PressFamily.Specialist).Where(id => MediaOutletCatalog.GetPress(id).Cadence != PressCadence.Daily));
			leads.Add(lead);
		}

		FanLeads(leads, evidence, today);
		AiPressLeads(leads, evidence, today);
		PlayerPressLeads(leads, evidence, today);
		Ledger("press-leads", leads.Count);
		MediaPress.Decide(leads, state, today);
		AfterDecide(today);
		AiAfterDecide(today);
		foreach (MediaStory s in state.Stories.Where(s => s.Status == StoryStatus.Scheduled && s.IssueDay >= today && s.IssueDay < today + 7 && s.ArtistId != null)) {
			MediaActTally t = TallyFor(s.ArtistId);
			if (t != null) t.PressItems++;
		}
	}

	// ---- read-only views ----------------------------------------------------------------------------------------------

	/// <summary>Items in an issue on a date, published only once that date has come (§4.5 for print).</summary>
	public static IEnumerable<MediaStory> PublishedOn(int day) =>
		state.Stories.Where(s => s.Status == StoryStatus.Scheduled && s.IssueDay == day);

	public static IEnumerable<MediaStory> PublishedAbout(string artistId, int today) =>
		state.Stories.Where(s => s.ArtistId == artistId && s.Status == StoryStatus.Scheduled && s.IssueDay <= today).OrderByDescending(s => s.IssueDay);
}
