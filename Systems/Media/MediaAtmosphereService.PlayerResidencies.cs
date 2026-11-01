using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Celebrity fanbase Phase 3b (SimTools/CelebrityFanbaseDesign.md §3.3.2): the label's side of a residency. A producer
/// whose first choice for a chair is the label's act offers instead of casting, and the chair waits a week for the
/// answer; the label can take the producer's terms, counter once (length, fee, material, diary), or say no. The label
/// can also put an act up for a chair or a series of its own, which counts at the next casting. A taped series takes
/// the act's weeks: road bookings are capped by its diary weeks, and the taping counts as road load. Fees are the
/// performers', paid per episode through member wealth in the causal window. Every outcome is keyed.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>The player's label, or null in a world without one (every audit run): then nothing here can happen.</summary>
	private static string PlayerLabelId => PlayerDesk.Instance?.Label?.labelId;

	public static IReadOnlyList<MediaResidencyOffer> ResidencyOffers => state.ResidencyOffers;
	public static IReadOnlyList<MediaResidencyPitch> ResidencyPitches => state.ResidencyPitches;
	public static IEnumerable<MediaResidency> PlayerResidencies => state.Residencies.Where(r => r.Player);

	/// <summary>The label's act is out of this casting: it has an open offer somewhere, or turned this program down this season.</summary>
	private static bool TurnedDown(string programId, string artistId, int first) =>
		state.ResidencyOffers.Any(o => o.ArtistId == artistId && (o.Open
			|| o.ProgramId == programId && o.Status is ResidencyOfferStatus.Declined or ResidencyOfferStatus.Lapsed && o.SeasonEndDay >= first));

	private static MediaResidencyPitch ListedPitch(string programId, string artistId, int day) =>
		state.ResidencyPitches.FirstOrDefault(p => p.Status == ResidencyPitchStatus.Listed && p.ProgramId == programId && p.ArtistId == artistId && day <= p.ExpiresDay);

	private static MediaContactMemory NetworkContact(TvNetwork network) {
		string key = "tv:network-" + network;
		return Contact(key, RolodexDirectory.PersonName(key, false), "Head of programming", network.ToString());
	}

	/// <summary>Diary weeks the label's act owes its series in a year, rounded up (§3.3.2).</summary>
	public static int PlayerDiaryWeeks(string artistId, int year) {
		if (artistId == null || state.Residencies.Count == 0) return 0;
		float weeks = state.Residencies.Where(r => r.Player && r.ArtistId == artistId).Sum(r => MediaResidencies.DiaryWeeks(r, year));
		return (int)Math.Ceiling(weeks - 1e-4f);
	}

	/// <summary>Chair programs the label can put an act up for: on the air in the next 26 weeks.</summary>
	public static List<TvProgram> ResidencyPrograms(GameDate today) =>
		MediaResidencies.Chairs.Select(c => MediaOutletCatalog.Get(c.ProgramId))
			.Where(p => p != null && Enumerable.Range(0, 26).Any(w => p.Runs(today.AddDays(7 * w)))).ToList();

	// ---- offers ----------------------------------------------------------------------------------------------------

	private static MediaResidencyOffer NewOffer(string programId, ResidencyKind kind, int chair, int seasonEnd, Prospect best, float score, float runnerUp, int today) {
		var o = new MediaResidencyOffer {
			Id = NextId("residency-offer"), ProgramId = programId, ArtistId = best.Artist.artistId, ArtistName = best.Artist.stageName, Kind = kind, Chair = chair,
			OfferedDay = today, ExpiresDay = today + MediaResidencies.OfferDays, SeasonEndDay = seasonEnd, Score = score,
			Leverage = MediaResidencies.Leverage(score, runnerUp), Status = ResidencyOfferStatus.Open,
		};
		state.ResidencyOffers.Add(o);
		MediaResidencyPitch pitch = ListedPitch(kind == ResidencyKind.Vehicle ? MediaResidencies.VehiclePitch : programId, o.ArtistId, today);
		if (pitch != null) pitch.Status = ResidencyPitchStatus.Used;
		return o;
	}

	private static void EstimateOffer(MediaResidencyOffer o, TvProgram program, GameDate date) {
		BroadcastScheduleVersion version = program.VersionOn(date) ?? program.Schedule.FirstOrDefault();
		if (version == null) return;
		int slots = Math.Max(1, program.SlotsOn(date) + 1);
		var markets = BroadcastExposure.Markets(program, version, date, ChartManager.Instance?.GetAllRegions());
		ExposureEstimate e = BroadcastExposure.Estimate(program, version.DurationMinutes, 0, slots, 0f, markets);
		(o.ReachLow, o.ReachHigh) = e.ReachRange();
		o.TeenShare = e.AverageByGroup.Sum() > 0 ? e.AverageByGroup[(int)ViewerGroup.Teen] / e.AverageByGroup.Sum() : 0;
		o.Daily = MediaResidencies.IsDaily(version);
	}

	private static void OfferChair(TvProgram program, int chair, int first, int end, Prospect best, float score, float runnerUp, GameDate weekEnd) {
		int today = BroadcastExposure.DayIndex(weekEnd);
		MediaResidencyOffer o = NewOffer(program.Id, ResidencyKind.Regular, chair, end, best, score, runnerUp, today);
		o.ShowName = program.Name;
		EstimateOffer(o, program, FromDayIndex(first));
		ResidencyLedger("offered");
		PrintOffer(o, weekEnd, "made");
		ProducerContact(program).Remember(weekEnd, $"Offered {o.ArtistName} a regular spot for the season.");
		PlayerDesk.Instance?.MediaNote($"{ProducerName(program)} at {program.Name} wants {o.ArtistName} as a regular, every episode to " +
			$"{FromDayIndex(end).ToShortString()}. Answer from the office by {FromDayIndex(o.ExpiresDay).ToShortString()}.");
	}

	private static void OfferVehicle(int year, TvNetwork network, DayOfWeek night, Prospect best, float score, float runnerUp, GameDate weekEnd) {
		int today = BroadcastExposure.DayIndex(weekEnd);
		MediaResidencyOffer o = NewOffer("vehicle_" + best.Artist.artistId, ResidencyKind.Vehicle, 0, BroadcastExposure.DayIndex(new GameDate(year + 1, 8, 31)),
			best, score, runnerUp, today);
		o.Network = network; o.Night = night; o.SeasonYear = year; o.ShowName = MediaResidencies.VehicleTitle(best.Artist.stageName);
		TvProgram pilot = MediaResidencies.BuildVehicle(new List<MediaResidency> { new() {
			ProgramId = o.ProgramId, ArtistName = o.ArtistName, ShowName = o.ShowName, Kind = ResidencyKind.Vehicle, Network = network, Night = night, SeasonYear = year } });
		EstimateOffer(o, pilot, MediaResidencies.VehiclePremiere(year, night));
		ResidencyLedger("offeredVehicle");
		PrintOffer(o, weekEnd, "made");
		NetworkContact(network).Remember(weekEnd, $"Offered {o.ArtistName} a series of their own this fall.");
		PlayerDesk.Instance?.MediaNote($"{network.ToString()} wants to build a half-hour series around {o.ArtistName}: \"{o.ShowName}\", " +
			$"{night}s at 7:30 from {MediaResidencies.VehiclePremiere(year, night).ToShortString()}. Answer from the office by {FromDayIndex(o.ExpiresDay).ToShortString()}.");
	}

	private static void PrintOffer(MediaResidencyOffer o, GameDate date, string what, ResidencyTerms? terms = null, float chance = 1f) =>
		Godot.GD.Print($"MEDIA_RESIDENCY_OFFER date={date.ToShortString()} {what} kind={o.Kind} program={o.ProgramId} chair={o.Chair} act=\"{o.ArtistName}\"" +
			$" status={o.Status} score={o.Score:F3} leverage={o.Leverage:F2} daily={o.Daily} reach={o.ReachLow:F0}-{o.ReachHigh:F0}" +
			(terms is { } t ? $" trial={t.Trial} fee={t.FeeMultiple:F1} showSongs={t.ShowSongs} blocks={t.Blocks} chance={chance:F2}" : ""));

	/// <summary>What the producer's terms would cost the act's road this year, for the offer card.</summary>
	public static string DiaryNote(MediaResidencyOffer o, bool blocks) {
		float share = MediaResidencies.DiaryShare(o.Kind, o.Daily, blocks);
		return $"{share:P0} of each week" + (o.Kind == ResidencyKind.Vehicle ? " filming" : " rehearsing and taping");
	}

	/// <summary>
	/// The label's answer (§3.3.2). The producer's own terms are always accepted; a counter is accepted on a keyed draw
	/// against <see cref="MediaResidencies.AcceptanceChance"/>, and only once: a refused counter leaves the original terms.
	/// </summary>
	public static bool AnswerResidencyOffer(PlayerDesk desk, string offerId, bool accept, ResidencyTerms terms, out string message) {
		MediaResidencyOffer o = state.ResidencyOffers.FirstOrDefault(x => x.Id == offerId);
		message = null;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int day = BroadcastExposure.DayIndex(today);
		if (o == null || !o.Open) { message = "That offer isn't open."; return false; }
		TvProgram program = o.Kind == ResidencyKind.Regular ? MediaOutletCatalog.Get(o.ProgramId) : null;
		MediaContactMemory contact = program != null ? ProducerContact(program) : NetworkContact(o.Network);
		if (!accept) {
			o.Status = ResidencyOfferStatus.Declined;
			contact.Remember(today, $"You turned down the series for {o.ArtistName}.");
			PrintOffer(o, today, "declined");
			message = $"Turned down. {contact.Name} will find someone else for {o.ShowName}.";
			return true;
		}
		if (o.Kind == ResidencyKind.Vehicle) terms.Trial = false;
		if (!MediaResidencies.FeeMultiples.Any(f => Math.Abs(f - terms.FeeMultiple) < 1e-4f)) terms.FeeMultiple = 1f;
		ResidencyTerms producer = ResidencyTerms.Producer(o.Kind);
		bool counter = !terms.Same(producer);
		if (counter && o.Countered) { message = $"{contact.Name} already said no to changing the terms. Take theirs or leave it."; return false; }
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(o.ArtistId);
		if (artist == null || !artist.isActive || artist.labelId != desk?.Label?.labelId) { message = $"{o.ArtistName} aren't yours to sign up any more."; return false; }

		// When does it start? A regular joins at the first original episode not yet booked; a vehicle premieres in September.
		int from, to;
		if (o.Kind == ResidencyKind.Vehicle) {
			from = BroadcastExposure.DayIndex(MediaResidencies.VehiclePremiere(o.SeasonYear, o.Night));
			to = o.SeasonEndDay;
		} else {
			from = -1;
			for (int d = Math.Max(state.BookedThroughDay, day) + 1; d <= o.SeasonEndDay && from < 0; d++) {
				GameDate date = FromDayIndex(d);
				if (program.Runs(date) && program.VersionOn(date)?.RepeatMonths.Contains(date.month) != true) from = d;
			}
			if (from < 0) { o.Status = ResidencyOfferStatus.Lapsed; message = "The season ran out while the offer sat on the desk."; return false; }
			to = terms.Trial ? Math.Min(o.SeasonEndDay, from + 7 * MediaResidencies.TrialWeeks - 1) : o.SeasonEndDay;
		}

		float chance = MediaResidencies.AcceptanceChance(o.Kind, o.Daily, terms, o.Leverage);
		if (counter && MediaAtmosphere.Unit("residency-counter|" + o.Id) >= chance) {
			o.Countered = true;
			contact.Trust = Math.Max(-1f, contact.Trust - 0.05f);
			contact.Remember(today, $"Asked for better terms for {o.ArtistName}; they said no.");
			PrintOffer(o, today, "counter-refused", terms, chance);
			message = $"{contact.Name} won't move: {(terms.FeeMultiple > 1f ? "there's no more money in the budget" : "the show is made the way it's made")}. " +
				$"The original terms stand until {FromDayIndex(o.ExpiresDay).ToShortString()}.";
			return true;
		}

		MarkCausalBoundary(today);
		o.Status = ResidencyOfferStatus.Accepted;
		var prospect = new Prospect { Artist = artist, Recognition = RecognitionOf(o.ArtistId), Young = FollowingFor(o.ArtistId)?.YoungTotal() ?? 0f, Player = true,
			HadTop40 = artist.publicHistory?.Any(e => e.peakPosition is > 0 and <= 40) == true };
		MediaResidency r = NewResidency(prospect, o.ProgramId, o.Kind, o.Chair, from, to, o.Score);
		r.Player = true; r.Daily = o.Daily; r.ShowSongs = terms.ShowSongs; r.Blocks = terms.Blocks; r.FeeMultiple = terms.FeeMultiple;
		r.DiaryShare = MediaResidencies.DiaryShare(o.Kind, o.Daily, terms.Blocks);
		r.CausalTerms = InCausalWindow(day);
		r.ReportedMonth = today.year * 12 + today.month - 2;
		if (o.Kind == ResidencyKind.Vehicle) { r.Network = o.Network; r.Night = o.Night; r.SeasonYear = o.SeasonYear; r.ShowName = o.ShowName; RestoreVehicles(); }
		ResidencyLedger(o.Kind == ResidencyKind.Vehicle ? "castVehicle" : "cast");
		ResidencyLedger("playerAccepted");
		PrintOffer(o, today, counter ? "counter-accepted" : "accepted", terms, chance);
		PrintCast(r, o.ShowName, prospect, today);
		int cancelled = desk.TrimRoadForTelevision(artist);
		contact.Trust = Math.Min(1f, contact.Trust + 0.05f);
		contact.Remember(today, $"Signed {o.ArtistName} for {o.ShowName}" + (counter ? " on changed terms." : "."));
		float scale = MediaResidencies.ScalePerEpisode(o.Daily) * terms.FeeMultiple;
		message = $"{o.ArtistName} are on {o.ShowName} from {FromDayIndex(from).ToShortString()} to {FromDayIndex(to).ToShortString()}" +
			(counter ? $" ({contact.Name} agreed to your terms)" : "") + $". Scale ${scale:N0} a performer an episode, paid by the production to the band. " +
			$"{(terms.ShowSongs ? "They sing the show's songs." : "They plug their own records.")} The taping takes {DiaryNote(o, terms.Blocks)}." +
			(cancelled > 0 ? $" {cancelled} weeks of touring this year are cancelled to make room." : "") +
			(r.CausalTerms ? "" : " (Fees are " + ObserveCostNote + ".)");
		return true;
	}

	// ---- the label's pitch -------------------------------------------------------------------------------------------

	/// <summary>Put an act up for a chair program, or for a series of its own (<see cref="MediaResidencies.VehiclePitch"/>).</summary>
	public static bool PitchResidency(PlayerDesk desk, string artistId, string programId, out string message) {
		message = null;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int day = BroadcastExposure.DayIndex(today);
		if (!MediaResidencies.Active) { message = "Residencies are switched off in this world."; return false; }
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(artistId);
		if (artist == null || desk?.Label == null || artist.labelId != desk.Label.labelId) { message = "That act isn't on your roster."; return false; }
		bool vehicle = programId == MediaResidencies.VehiclePitch;
		TvProgram program = vehicle ? null : MediaOutletCatalog.Get(programId);
		if (!vehicle && (program == null || MediaResidencies.Chairs.All(c => c.ProgramId != programId))) { message = "That show has no regulars' chair."; return false; }
		if (state.ResidencyPitches.Any(p => p.ArtistId == artistId && p.ProgramId == programId && p.Status is ResidencyPitchStatus.Sent or ResidencyPitchStatus.Listed && day <= p.ExpiresDay)) {
			message = $"{artist.stageName} are already in front of {(vehicle ? "the networks" : program.Name)}."; return false;
		}
		MarkCausalBoundary(today);
		bool charged = InCausalWindow(day);
		if (charged && !desk.CanPayMedia(PitchHours, PitchPostage, 0, null, out message)) return false;
		var pitch = new MediaResidencyPitch {
			Id = NextId("residency-pitch"), ArtistId = artistId, ArtistName = artist.stageName, ProgramId = programId, SentDay = day, Status = ResidencyPitchStatus.Sent,
		};
		pitch.AnswerDay = day + 2 + (int)(MediaAtmosphere.Unit("residency-pitch-answer|" + pitch.Id) * 3);
		pitch.ExpiresDay = pitch.AnswerDay;
		state.ResidencyPitches.Add(pitch);
		if (charged && TakeReceipt("pitch|" + pitch.Id)) desk.PayMedia(PitchHours, PitchPostage, 0, null);
		string to = vehicle ? "the three networks' programming departments" : $"{ProducerName(program)} at {program.Name}";
		message = $"Sent {artist.stageName}'s photographs, records and press to {to}. Expect an answer in a few days.";
		return true;
	}

	private static void AnswerResidencyPitch(PlayerDesk desk, MediaResidencyPitch pitch, GameDate today, Evidence evidence) {
		int day = BroadcastExposure.DayIndex(today);
		bool vehicle = pitch.ProgramId == MediaResidencies.VehiclePitch;
		TvProgram program = vehicle ? null : MediaOutletCatalog.Get(pitch.ProgramId);
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(pitch.ArtistId);
		string who = vehicle ? "The networks" : $"{ProducerName(program)} at {program?.Name}";
		void Decline(string reason) {
			pitch.Status = ResidencyPitchStatus.Declined; pitch.Reason = reason;
			desk.MediaNote($"{who} passed on {pitch.ArtistName}: {reason}");
			Godot.GD.Print($"MEDIA_RESIDENCY_PITCH date={today.ToShortString()} act=\"{pitch.ArtistName}\" program={pitch.ProgramId} listed=False reason=\"{reason}\"");
		}
		if (artist == null || !vehicle && program == null) { Decline("the package never arrived."); return; }
		float fit = vehicle ? MediaOutletCatalog.TeenFit(artist.primaryGenre) : program.GenreFit(artist.primaryGenre);
		if (vehicle && today.year < MediaResidencies.FirstVehicleYear) { Decline("nobody is building a series around a record act. Yet."); return; }
		if (fit < (vehicle ? MediaResidencies.VehicleTeenFit : MediaResidencies.ChairFit)) { Decline("it isn't the show's kind of music."); return; }
		if (state.Residencies.Any(r => r.ArtistId == pitch.ArtistId && r.ToDay >= day)) { Decline("they're already on a series."); return; }
		if (Prospects(evidence, day).All(p => p.Artist.artistId != pitch.ArtistId)) { Decline("come back when they have a hit, or a town that's asking for them."); return; }
		pitch.Status = ResidencyPitchStatus.Listed;
		int nextAugust = BroadcastExposure.DayIndex(new GameDate(today.month >= 8 ? today.year + 1 : today.year, 8, 31));
		pitch.ExpiresDay = vehicle ? nextAugust : day + MediaResidencies.PitchListDays;
		MediaResidency sitting = vehicle ? null : state.Residencies.Where(r => r.ProgramId == pitch.ProgramId && r.ToDay >= day).OrderBy(r => r.ToDay).FirstOrDefault();
		string when = vehicle ? "The networks decide their fall shows in August."
			: sitting != null ? $"The chair is cast through {FromDayIndex(sitting.ToDay).ToShortString()}; they'll be in the running at the recast."
			: "They'll be in the running when the chair is next cast.";
		if (!vehicle) ProducerContact(program).Remember(today, $"Put {pitch.ArtistName} up for a regular spot.");
		desk.MediaNote($"{who} liked what they saw of {pitch.ArtistName}. {when}");
		Godot.GD.Print($"MEDIA_RESIDENCY_PITCH date={today.ToShortString()} act=\"{pitch.ArtistName}\" program={pitch.ProgramId} listed=True until={FromDayIndex(pitch.ExpiresDay).ToShortString()}");
	}

	// ---- the label's day -----------------------------------------------------------------------------------------------

	/// <summary>
	/// Daily, from PlayerDay: producers answer residency pitches, lists and offers lapse, the performers are paid for the
	/// episodes produced (causal terms only, once each by receipt), and on the first of a month the office hears how the
	/// series went.
	/// </summary>
	private static void ResidencyPlayerDay(PlayerDesk desk, GameDate today) {
		int day = BroadcastExposure.DayIndex(today);
		Evidence evidence = null;
		foreach (MediaResidencyPitch pitch in state.ResidencyPitches) {
			if (pitch.Status == ResidencyPitchStatus.Sent && day >= pitch.AnswerDay) AnswerResidencyPitch(desk, pitch, today, evidence ??= new Evidence(ChartManager.Instance));
			else if (pitch.Status == ResidencyPitchStatus.Listed && day > pitch.ExpiresDay) pitch.Status = ResidencyPitchStatus.Lapsed;
		}
		foreach (MediaResidencyOffer o in state.ResidencyOffers.Where(o => o.Open && day > o.ExpiresDay)) {
			o.Status = ResidencyOfferStatus.Lapsed;
			PrintOffer(o, today, "lapsed");
			desk.MediaNote($"The offer of {o.ShowName} for {o.ArtistName} lapsed: nobody answered. It goes to someone else.");
		}
		int month = today.year * 12 + today.month - 1;
		foreach (MediaResidency r in state.Residencies.Where(r => r.Player && r.FromDay < day && (r.PaidThroughDay < Math.Min(day - 1, r.ToDay) || r.ReportedMonth < month - 1))) {
			var aired = state.Appearances.Where(a => a.ArtistId == r.ArtistId && a.ProgramId == r.ProgramId && a.Lane == BillLane.Resident && a.RepeatOfId == null && !a.Cut)
				.Select(a => (A: a, Day: BroadcastExposure.DayIndex(a.Date))).Where(x => x.Day >= r.FromDay && x.Day <= r.ToDay && x.Day < day).ToList();
			List<Musician> performers = Performers(r.ArtistId);
			float scale = MediaResidencies.ScalePerEpisode(r.Daily) * r.FeeMultiple;
			if (r.CausalTerms)
				foreach (var (a, _) in aired.Where(x => x.Day > r.PaidThroughDay))
					if (TakeReceipt("resident-fee|" + a.Id))
						foreach (Musician m in performers) {
							string key = m.personId + "|" + a.Date.year;
							state.PerformerFees[key] = state.PerformerFees.GetValueOrDefault(key) + scale;
						}
			r.PaidThroughDay = Math.Max(r.PaidThroughDay, Math.Min(day - 1, r.ToDay));
			if (r.ReportedMonth >= month - 1) continue;
			r.ReportedMonth = month - 1;
			GameDate last = today.AddDays(-today.day);
			var those = aired.Where(x => x.A.Date.year == last.year && x.A.Date.month == last.month).ToList();
			if (those.Count == 0) continue;
			double best = those.Max(x => x.A.Airings.FirstOrDefault()?.Exposure.UniqueReach ?? 0);
			float paid = scale * performers.Count * those.Count;
			desk.MediaNote($"{r.ShowName ?? MediaOutletCatalog.Get(r.ProgramId)?.Name}: {r.ArtistName} did {those.Count} episode{(those.Count == 1 ? "" : "s")} in " +
				$"{last.MonthName}; the best reached about {MediaText.Audience(best)}. " +
				(r.CausalTerms ? $"The production paid the band ${paid:N0} in scale." : $"Scale to the band, about ${paid:N0} (" + ObserveCostNote + ")."));
		}
	}

	/// <summary>Fixtures: offer one chair to the label's act as if it topped the casting.</summary>
	internal static MediaResidencyOffer OfferForChecks(TvProgram program, int chair, SimulatedArtist act, GameDate weekEnd, int first, int end, float leverage) {
		var p = new Prospect { Artist = act, Recognition = RecognitionOf(act.artistId), Player = true };
		OfferChair(program, chair, first, end, p, 1f, 1f / Math.Max(1f, leverage), weekEnd);
		return state.ResidencyOffers[^1];
	}

	internal static void ResidencyPlayerDayForChecks(PlayerDesk desk, GameDate today) => ResidencyPlayerDay(desk, today);
}
