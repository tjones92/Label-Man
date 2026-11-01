using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Celebrity fanbase Phase 3a (SimTools/CelebrityFanbaseDesign.md §3.3.1), behind <c>--media-residencies</c>. Once a week,
/// before next week is booked, producers recast any empty resident chair on the documented series, and each August a
/// network may commission a half hour built around a hot teen act. Residents appear on every original episode outside
/// the guest bill; their later airings reach mostly the same viewers, and those regulars join the act's following each
/// week (the series term in FollowingWeek). Rival acts only; the player's negotiation is Phase 3b.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>Residents' original airings tallied this week, for the following's series term. Consumed the same step.</summary>
	private static readonly List<(MediaAppearance Appearance, MediaAiring Airing)> residentAirings = new();
	/// <summary>Year ledger for MEDIA_RESIDENCY_YEAR, print-only and cleared each year end.</summary>
	private static readonly Dictionary<string, double> residencyLedger = new(StringComparer.Ordinal);
	private static void ResidencyLedger(string key, double amount = 1) => residencyLedger[key] = residencyLedger.GetValueOrDefault(key) + amount;

	public static IReadOnlyList<MediaResidency> Residencies => state.Residencies;

	/// <summary>Puts the saved vehicles back on the dial (after a load, a reset, or a new or renewed series).</summary>
	private static void RestoreVehicles() => MediaOutletCatalog.SetVehicles(MediaResidencies.Vehicles(state.Residencies));

	internal static List<MediaResidency> ResidentsOn(string programId, int day) =>
		state.Residencies.Where(r => r.ProgramId == programId && day >= r.FromDay && day <= r.ToDay).OrderBy(r => r.Chair).ToList();

	private static bool ResidentOn(string programId, string artistId, int day) =>
		state.Residencies.Count > 0 && state.Residencies.Any(r => r.ArtistId == artistId && r.ProgramId == programId && day >= r.FromDay && day <= r.ToDay);

	private static float FollowingSize(string artistId) {
		MediaFollowingState f = FollowingFor(artistId);
		return f == null ? 0f : f.YoungTotal() + f.AdultTotal();
	}

	private static void ResidentAiring(MediaAppearance a, MediaAiring airing) {
		if (MediaFollowing.Active && a.Lane == BillLane.Resident && !a.Cut && !airing.Repeat && !airing.Syndicated) residentAirings.Add((a, airing));
	}

	// ---- casting --------------------------------------------------------------------------------------------------

	private sealed class Prospect {
		public SimulatedArtist Artist;
		public float Recognition, Young;
		public bool HadTop40;
	}

	/// <summary>
	/// Acts a producer could cast this week (§3.3.1): active, signed to a rival label, not a Superstar, and with evidence
	/// (a Top 40 single in two years, a record broken in a region in the last year, or recognition ≥ 0.10). Built once per
	/// pass in artist-id order; reads records, recognition and the following, writes nothing.
	/// </summary>
	private static List<Prospect> Prospects(Evidence evidence, int today) {
		var hit = new HashSet<string>(StringComparer.Ordinal);
		var recent = new HashSet<string>(StringComparer.Ordinal);
		var ids = new SortedSet<string>(StringComparer.Ordinal);
		foreach (RecordRuntimeData r in evidence.All) {
			Record b = r?.baseRecord;
			if (b == null || b.isPlayerOwned || string.IsNullOrEmpty(b.artistId)) continue;
			ids.Add(b.artistId);
			int released = BroadcastExposure.DayIndex(b.releaseDate);
			if (released > today) continue;
			if (r.peakPosition is > 0 and <= 40 && b.format == ReleaseFormat.Single) {
				hit.Add(b.artistId);
				if (released >= today - 730) recent.Add(b.artistId);
			}
			if (released >= today - 365 && evidence.Broke(r)) recent.Add(b.artistId);
		}
		int year = FromDayIndex(today).year;
		var list = new List<Prospect>();
		foreach (string id in ids) {
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(id);
			if (artist == null || !artist.isActive || artist.isPlayerOwned || string.IsNullOrEmpty(artist.labelId) || artist.careerState == CareerState.Superstar) continue;
			if (CompetitorManager.Instance?.GetLabel(artist.labelId) == null) continue;
			bool hadHit = hit.Contains(id) || artist.publicHistory?.Any(e => e.peakPosition is > 0 and <= 40) == true;
			bool evidenced = recent.Contains(id) || artist.publicHistory?.Any(e => e.peakPosition is > 0 and <= 40 && e.year >= year - 2) == true;
			float recognition = ArtistRecognitionService.EffectiveRecognition(artist);
			if (!evidenced && recognition < MediaResidencies.RecognitionEvidence) continue;
			if (state.Residencies.Any(r => r.ArtistId == id && r.ToDay > today)) continue;
			list.Add(new Prospect { Artist = artist, Recognition = recognition, Young = FollowingFor(id)?.YoungTotal() ?? 0f, HadTop40 = hadHit });
		}
		return list;
	}

	private static void ResidenciesWeek(Evidence evidence, GameDate weekEnd) {
		if (!MediaResidencies.Active) return;
		int today = BroadcastExposure.DayIndex(weekEnd);
		foreach (MediaResidency r in state.Residencies) {
			bool on40 = evidence.OnTop40(r.ArtistId);
			if (on40 && today >= r.FromDay && today <= r.ToDay + 7) r.Top40During = true;
			// §9 TV-made stars: an act's first Top 40 within a year of its residency's start.
			if (on40 && !r.HadTop40Before && r.FirstTop40Day < 0 && today >= r.FromDay && today <= r.FromDay + 365) {
				r.FirstTop40Day = today;
				ResidencyLedger("tvMadeStars");
				Godot.GD.Print($"MEDIA_TV_MADE_STAR date={weekEnd.ToShortString()} act=\"{r.ArtistName}\" program={r.ProgramId} kind={r.Kind}" +
					$" weeksIn={(today - r.FromDay) / 7} following={FollowingSize(r.ArtistId):F0} recognition={RecognitionOf(r.ArtistId):F3}");
			}
			if (r.Ended) continue;
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(r.ArtistId);
			// An act that broke up or lost its deal leaves the show.
			if (r.ToDay > today && (artist == null || !artist.isActive || string.IsNullOrEmpty(artist.labelId) || artist.isPlayerOwned)) {
				r.ToDay = today;
				ResidencyLedger("leftEarly");
			}
			if (r.ToDay <= today) EndResidency(r, weekEnd);
		}

		List<Prospect> prospects = null;
		foreach (var (programId, chairs) in MediaResidencies.Chairs) {
			TvProgram program = MediaOutletCatalog.Get(programId);
			if (program == null) continue;
			for (int chair = 0; chair < chairs; chair++) {
				if (state.Residencies.Any(r => r.ProgramId == programId && r.Chair == chair && r.ToDay > today)) continue;
				int first = -1;
				for (int d = today + 1; d <= today + 7 && first < 0; d++) if (program.Runs(FromDayIndex(d))) first = d;
				if (first < 0) continue;
				GameDate from = FromDayIndex(first);
				int end = MediaResidencies.SeasonEndDay(program, from);
				if (end - first < MediaResidencies.MinChairDays) continue;
				MediaResidency incumbent = state.Residencies.LastOrDefault(r => r.ProgramId == programId && r.Chair == chair && r.ToDay >= today - 14);
				prospects ??= Prospects(evidence, today);
				CastChair(program, chair, first, end, prospects, incumbent, weekEnd);
			}
		}

		// Each August, one look at the coming season: renew last year's vehicles that worked, and perhaps commission one.
		if (weekEnd.month == 8 && weekEnd.year >= MediaResidencies.FirstVehicleYear && state.VehicleSeasons.Add(weekEnd.year)) {
			prospects ??= Prospects(evidence, today);
			VehicleSeason(weekEnd, prospects);
		}
	}

	private static float RecognitionOf(string artistId) {
		SimulatedArtist a = ArtistManager.Instance?.GetArtist(artistId);
		return a == null ? 0f : ArtistRecognitionService.EffectiveRecognition(a);
	}

	private static void CastChair(TvProgram program, int chair, int first, int end, List<Prospect> prospects, MediaResidency incumbent, GameDate weekEnd) {
		Prospect best = null;
		float bestScore = 0f;
		foreach (Prospect p in prospects) {
			string id = p.Artist.artistId;
			if (state.Residencies.Any(r => r.ArtistId == id && r.ToDay >= first)) continue;
			if (state.ProgramMemory.TryGetValue(program.Id + "|" + id, out var memory) && memory.ExcludedUntilDay > first) continue;
			float fit = program.GenreFit(p.Artist.primaryGenre);
			if (fit < MediaResidencies.ChairFit) continue;
			float score = MediaResidencies.Score(fit, p.Recognition, p.Young, MediaAtmosphere.Unit("residency|" + program.Id + "|" + chair + "|" + first + "|" + id));
			if (incumbent != null && incumbent.ArtistId == id) score *= MediaResidencies.IncumbentBonus;
			if (score > bestScore) { best = p; bestScore = score; }
		}
		if (best == null) { ResidencyLedger("noProspect"); return; }
		bool kept = incumbent != null && incumbent.ArtistId == best.Artist.artistId;
		var r = NewResidency(best, program.Id, ResidencyKind.Regular, chair, first, end, bestScore);
		if (kept) r.Season = incumbent.Season + 1;
		ResidencyLedger(kept ? "kept" : "cast");
		PrintCast(r, program.Name, best, weekEnd);
	}

	private static MediaResidency NewResidency(Prospect p, string programId, ResidencyKind kind, int chair, int from, int to, float score) {
		var r = new MediaResidency {
			Id = "residency|" + programId + "|" + chair + "|" + from, ProgramId = programId, ArtistId = p.Artist.artistId, ArtistName = p.Artist.stageName,
			LabelId = p.Artist.labelId, Kind = kind, Chair = chair, FromDay = from, ToDay = to, Score = score,
			RecognitionAtStart = p.Recognition, FollowingAtStart = FollowingSize(p.Artist.artistId), HadTop40Before = p.HadTop40,
		};
		state.Residencies.Add(r);
		return r;
	}

	private static void PrintCast(MediaResidency r, string programName, Prospect p, GameDate weekEnd) =>
		Godot.GD.Print($"MEDIA_RESIDENCY date={weekEnd.ToShortString()} kind={r.Kind} program={r.ProgramId} show=\"{programName}\" chair={r.Chair} season={r.Season}" +
			$" act=\"{r.ArtistName}\" genre={p.Artist.primaryGenre} label={r.LabelId} career={p.Artist.careerState} recognition={p.Recognition:F3}" +
			$" young={p.Young:F0} hadTop40={p.HadTop40} score={r.Score:F3} from={FromDayIndex(r.FromDay).ToShortString()} to={FromDayIndex(r.ToDay).ToShortString()}");

	/// <summary>
	/// August (§3.3.1): a vehicle whose first season grew the act's following (recognition, without the following switch)
	/// and gave it a Top 40 single is renewed once; then the season's one new vehicle goes to the best teen act that is a
	/// Regional Favorite or better, or already has 100k young followers. A season with no such act has none.
	/// </summary>
	private static void VehicleSeason(GameDate weekEnd, List<Prospect> prospects) {
		int year = weekEnd.year;
		bool changed = false;
		foreach (MediaResidency v in state.Residencies.Where(r => r.Kind == ResidencyKind.Vehicle && r.SeasonYear == year - 1).ToList()) {
			if (v.Season >= MediaResidencies.MaxVehicleSeasons) continue;
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(v.ArtistId);
			bool grew = MediaFollowing.Active ? FollowingSize(v.ArtistId) > v.FollowingAtStart : RecognitionOf(v.ArtistId) > v.RecognitionAtStart;
			bool renew = grew && v.Top40During && artist != null && artist.isActive && !artist.isPlayerOwned && !string.IsNullOrEmpty(artist.labelId);
			Godot.GD.Print($"MEDIA_RESIDENCY_RENEWAL date={weekEnd.ToShortString()} act=\"{v.ArtistName}\" show=\"{v.ShowName}\" renewed={renew} grew={grew} top40={v.Top40During}");
			if (!renew) continue;
			GameDate premiere = MediaResidencies.VehiclePremiere(year, v.Night);
			var next = new MediaResidency {
				Id = "residency|" + v.ProgramId + "|0|" + BroadcastExposure.DayIndex(premiere), ProgramId = v.ProgramId, ArtistId = v.ArtistId, ArtistName = v.ArtistName,
				LabelId = artist.labelId, Kind = ResidencyKind.Vehicle, FromDay = BroadcastExposure.DayIndex(premiere),
				ToDay = BroadcastExposure.DayIndex(new GameDate(year + 1, 8, 31)), Season = v.Season + 1, Network = v.Network, Night = v.Night,
				SeasonYear = year, ShowName = v.ShowName, Score = v.Score, RecognitionAtStart = RecognitionOf(v.ArtistId),
				FollowingAtStart = FollowingSize(v.ArtistId), HadTop40Before = true,
			};
			state.Residencies.Add(next);
			ResidencyLedger("renewed");
			changed = true;
		}

		Prospect best = null;
		float bestScore = 0f;
		int premiereDay = BroadcastExposure.DayIndex(new GameDate(year, 9, 12));
		foreach (Prospect p in prospects) {
			string id = p.Artist.artistId;
			float fit = MediaOutletCatalog.TeenFit(p.Artist.primaryGenre);
			if (fit < MediaResidencies.VehicleTeenFit || state.Residencies.Any(r => r.ArtistId == id && r.ToDay >= premiereDay)) continue;
			bool hot = EpisodeOf(id) is FanEpisodeState.RegionalFavorite or FanEpisodeState.NationalSensation or FanEpisodeState.CulturalPhenomenon;
			if (!hot && p.Young < MediaResidencies.VehicleFollowing) continue;
			float score = MediaResidencies.Score(fit, p.Recognition, p.Young, MediaAtmosphere.Unit("vehicle|" + year + "|" + id));
			if (score > bestScore) { best = p; bestScore = score; }
		}
		if (best == null) {
			ResidencyLedger("noVehicle");
			Godot.GD.Print($"MEDIA_RESIDENCY_VEHICLE year={year} none: no teen act is a Regional Favorite or has {MediaResidencies.VehicleFollowing:F0} young followers");
		} else {
			var networks = new[] { TvNetwork.ABC, TvNetwork.CBS, TvNetwork.NBC };
			var nights = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
			TvNetwork network = networks[(int)(MediaAtmosphere.Unit("vehicle-network|" + year + "|" + best.Artist.artistId) * 3) % 3];
			DayOfWeek night = nights[(int)(MediaAtmosphere.Unit("vehicle-night|" + year + "|" + best.Artist.artistId) * 5) % 5];
			GameDate premiere = MediaResidencies.VehiclePremiere(year, night);
			var r = NewResidency(best, "vehicle_" + best.Artist.artistId, ResidencyKind.Vehicle, 0, BroadcastExposure.DayIndex(premiere),
				BroadcastExposure.DayIndex(new GameDate(year + 1, 8, 31)), bestScore);
			r.Network = network; r.Night = night; r.SeasonYear = year; r.ShowName = MediaResidencies.VehicleTitle(best.Artist.stageName);
			ResidencyLedger("castVehicle");
			PrintCast(r, r.ShowName, best, weekEnd);
			changed = true;
		}
		if (changed) RestoreVehicles();
	}

	private static void EndResidency(MediaResidency r, GameDate weekEnd) {
		r.Ended = true;
		ResidencyLedger("ended");
		Godot.GD.Print($"MEDIA_RESIDENCY_END date={weekEnd.ToShortString()} kind={r.Kind} program={r.ProgramId} act=\"{r.ArtistName}\" season={r.Season}" +
			$" weeks={(r.ToDay - r.FromDay) / 7} episodes={r.Episodes} seriesJoiners={r.SeriesJoiners:F0}" +
			$" following={r.FollowingAtStart:F0}->{FollowingSize(r.ArtistId):F0} recognition={r.RecognitionAtStart:F3}->{RecognitionOf(r.ArtistId):F3}" +
			$" top40During={r.Top40During} hadTop40Before={r.HadTop40Before}");
	}

	/// <summary>Fixtures: cast one chair of any program from this week's evidence, outside the chair table.</summary>
	internal static MediaResidency CastForChecks(TvProgram program, int chair, GameDate weekEnd, int fromDay, int toDay) {
		int before = state.Residencies.Count;
		CastChair(program, chair, fromDay, toDay, Prospects(new Evidence(ChartManager.Instance), BroadcastExposure.DayIndex(weekEnd)), null, weekEnd);
		return state.Residencies.Count > before ? state.Residencies[^1] : null;
	}

	internal static void RestoreVehiclesForChecks() => RestoreVehicles();

	// ---- the resident on the air -------------------------------------------------------------------------------------

	/// <summary>
	/// This episode's residents (§3.3.1), opening the show outside the guest bill. Each sings its current single (released in
	/// the last 12 weeks, or on the chart) when it has one, else a number from its set: its best-known hit, or a standard.
	/// The label's campaign isn't charged: the producer pays the regulars.
	/// </summary>
	private static List<Candidate> ResidentCandidates(TvProgram program, GameDate date, string episodeId, Evidence evidence) {
		var list = new List<Candidate>();
		if (!MediaResidencies.Active || state.Residencies.Count == 0) return list;
		int day = BroadcastExposure.DayIndex(date);
		foreach (MediaResidency r in ResidentsOn(program.Id, day)) {
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(r.ArtistId);
			if (artist == null || !artist.isActive) continue;
			RecordRuntimeData single = null, latest = null;
			foreach (RecordRuntimeData rec in evidence.All) {
				Record b = rec?.baseRecord;
				if (b?.artistId != r.ArtistId || BroadcastExposure.DayIndex(b.releaseDate) > day) continue;
				if (latest == null || b.releaseDate > latest.baseRecord.releaseDate
					|| b.releaseDate == latest.baseRecord.releaseDate && string.CompareOrdinal(b.recordId, latest.baseRecord.recordId) > 0) latest = rec;
				if (b.format == ReleaseFormat.Single && (rec.currentPosition > 0 || BroadcastExposure.DayIndex(b.releaseDate) >= day - 84)
					&& (single == null || b.releaseDate > single.baseRecord.releaseDate
						|| b.releaseDate == single.baseRecord.releaseDate && string.CompareOrdinal(b.recordId, single.baseRecord.recordId) > 0)) single = rec;
			}
			if (latest == null) continue;
			var c = new Candidate { Record = single ?? latest, Lane = BillLane.Resident, Residency = r, Prep = MediaPreparation.Standard };
			if (single != null) c.Song = PerformedSong.Record;
			else {
				string hit = evidence.BestKnownHit(artist);
				c.Song = hit != null ? PerformedSong.OwnHit : PerformedSong.Standard;
				c.SongTitle = hit ?? "a number from their set";
			}
			list.Add(c);
		}
		return list;
	}

	/// <summary>
	/// Year end: chairs filled and vehicles made, the residents' episodes, series joiners and Top 40 record, and the acts on
	/// a series now with their followings. Read-only.
	/// </summary>
	private static void PrintResidencies(int year) {
		if (!MediaResidencies.Active) return;
		double V(string key) => residencyLedger.GetValueOrDefault(key);
		int first = BroadcastExposure.DayIndex(new GameDate(year, 1, 1)), last = BroadcastExposure.DayIndex(new GameDate(year, 12, 31));
		var thisYear = state.Residencies.Where(r => r.FromDay <= last && r.ToDay >= first).ToList();
		var now = thisYear.Where(r => r.ToDay >= last - 7).OrderBy(r => r.ProgramId, StringComparer.Ordinal).ThenBy(r => r.Chair)
			.Select(r => $"\"{r.ArtistName}\"@{r.ProgramId} {FollowingSize(r.ArtistId) / 1e3:F0}k");
		Godot.GD.Print($"MEDIA_RESIDENCY_YEAR year={year} onAir={thisYear.Count} regulars={thisYear.Count(r => r.Kind == ResidencyKind.Regular)}" +
			$" vehicles={thisYear.Count(r => r.Kind == ResidencyKind.Vehicle)} cast={V("cast")} kept={V("kept")} castVehicle={V("castVehicle")} renewed={V("renewed")}" +
			$" noProspect={V("noProspect")} noVehicle={V("noVehicle")} ended={V("ended")} leftEarly={V("leftEarly")} episodes={V("episodes")}" +
			$" seriesJoiners={V("seriesJoiners") / 1e3:F0}k residentTop40={thisYear.Count(r => r.Top40During)} tvMadeStars={V("tvMadeStars")}" +
			$" now=[{string.Join(" ", now)}]");
		residencyLedger.Clear();
	}
}
