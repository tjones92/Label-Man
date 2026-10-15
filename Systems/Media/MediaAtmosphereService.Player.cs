using System;
using System.Collections.Generic;
using System.Linq;

public enum PitchKind { Television, Press }

/// <summary>Stages of a pitch (§5.1). A hold is not a confirmation; booked, recorded, aired are different states.</summary>
public enum PitchStatus { Sent, Declined, Offered, Confirmed, Aired, TurnedDown, Lapsed, Withdrawn, Placed, Passed }

/// <summary>One approach from the label to a producer or a desk, with everything the offer has to show before acceptance (§14).</summary>
public sealed class MediaPitch {
	public string Id, ProgramId, OutletId, RecordId, ArtistId, ArtistName, Title, ContactKey, Reason, QueryId, AppearanceId, CostNote;
	public PitchKind Kind;
	public PitchStatus Status;
	public StoryAngle Angle;
	public int SentDay, AnswerDay, AirDay, OfferExpiresDay;
	public bool Discovery;
	public MediaPreparation Preparation = MediaPreparation.Standard;
	public double ReachLow, ReachHigh, TeenShare;
	public int SlotIndex, SlotCount;
}

public enum InquiryKind { OrdersQuestion, PhotoRequest }

/// <summary>A reporter or editor calling the office with a deadline (§9.2-§9.3). Unanswered by the deadline is "declined to comment".</summary>
public sealed class MediaInquiry {
	public string Id, OutletId, ContactKey, ArtistId, ArtistName, RecordId, Title, StoryId, Answer;
	public InquiryKind Kind;
	public int Day, DeadlineDay;
	public bool Open = true;
	public long ActualUnits, ClaimedUnits;
	/// <summary>Day an inflated figure gets checked against later sales (§9.2: inflated orders can later be checked).</summary>
	public int CheckDay = -1;
	public bool Checked;
}

/// <summary>Photo terms (§8.2): a named subject, an outlet, a scope and an expiry. Never perpetual control of an image.</summary>
public sealed class MediaExclusive {
	public string Id, OutletId, ArtistId, ArtistName, Scope, Romance;
	public bool Exclusive;
	public int FromDay, UntilDay;
	/// <summary>A planted romance is stored as a publicity claim with its origin, never as a real relationship (§8.2).</summary>
	public bool PlantedClaim;
	public int ContradictionDay = -1;
	public bool Contradicted;
}

/// <summary>
/// The label's side of the media world (Phase 2, observe mode). The verbs are real (pitches are considered, offers carry
/// terms, standards ask, reporters call, editors decide) but while media is observe-only the costs they would carry are
/// shown as estimates and never charged: no diary hours, no promo copies, no cash (§12.1). Every outcome is keyed.
/// </summary>
public static partial class MediaAtmosphereService {
	private const int OfferAnswerDays = 4;

	private static string NextId(string prefix) => prefix + "|" + (++state.PlayerSequence);

	public static IReadOnlyList<MediaPitch> Pitches => state.Pitches;
	public static IReadOnlyList<MediaInquiry> Inquiries => state.Inquiries;
	public static IReadOnlyList<MediaExclusive> Exclusives => state.Exclusives;
	public static MediaContactMemory ContactFor(string key) => key != null && state.Contacts.TryGetValue(key, out var c) ? c : null;

	private static MediaContactMemory Contact(string key, string name, string role, string outlet) {
		if (!state.Contacts.TryGetValue(key, out var c)) state.Contacts[key] = c = new MediaContactMemory { Key = key, Name = name, Role = role, OutletName = outlet };
		return c;
	}

	public static string ProducerKey(TvProgram program) => "tv:" + program.Id;
	public static string ProducerName(TvProgram program) {
		string key = ProducerKey(program);
		return RolodexDirectory.PersonName(key, Portraits.Hash(key + "|female") % 5 == 0);
	}
	private static MediaContactMemory ProducerContact(TvProgram program) =>
		Contact(ProducerKey(program), ProducerName(program), program.Network == TvNetwork.Local ? "Producer" : "Talent booker", program.Name);
	private static MediaContactMemory DeskContact(PressOutlet outlet, PressRole role) =>
		Contact(MediaPress.ContactKey(outlet, role), MediaPress.ContactName(outlet, role), MediaPress.RoleTitle(outlet, role), outlet.Name);

	public const string ObserveCostNote = "estimate only: nothing is charged while media runs in observe mode";

	// ---- what the player can pitch ------------------------------------------------------------------------------

	/// <summary>Programs a label could approach from where it is: the local and syndicated shows on its home and current
	/// dials, and the network shows on the air in the coming weeks.</summary>
	public static List<TvProgram> PitchablePrograms(string homeCity, string currentCity, GameDate today) {
		var list = new List<TvProgram>();
		foreach (TvProgram p in MediaOutletCatalog.Programs) {
			bool onSoon = Enumerable.Range(0, 42).Any(d => p.Runs(today.AddDays(d)));
			if (!onSoon) continue;
			if (p.Network == TvNetwork.Local) {
				if (p.HomeCityId == homeCity || p.HomeCityId == currentCity || p.Syndication.Any(m => (m.CityId == homeCity || m.CityId == currentCity) && today >= m.From))
					list.Add(p);
			} else list.Add(p);
		}
		return list.OrderBy(p => p.Network == TvNetwork.Local ? 0 : 1).ThenBy(p => p.Name, StringComparer.Ordinal).ToList();
	}

	/// <summary>Desks a label could send a press sheet to: its town's daily and community papers, the trades, the teen
	/// magazines and critics that are publishing, and the national specialist press.</summary>
	public static List<PressOutlet> PitchableDesks(string homeCity, GameDate today) =>
		MediaOutletCatalog.Press.Where(o => o.Publishing(today) && (o.National || o.HomeCityId == homeCity))
			.OrderBy(o => o.Family == PressFamily.LocalDaily ? 0 : o.Family == PressFamily.Specialist ? 1 : o.Family == PressFamily.Trade ? 2 : 3)
			.ThenBy(o => o.Name, StringComparer.Ordinal).ToList();

	public static string TravelNote(string homeCity, TvProgram program) {
		string where = program.Network == TvNetwork.Local ? program.HomeCityId : program.VersionOn(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate)?.ProductionCityId
			?? program.Schedule.Last().ProductionCityId;
		if (where == null || where == homeCity) return "no travel";
		float miles = DistanceModel.GetRoadMilesBetween(homeCity, where);
		int nights = miles > 600 ? 3 : 2;
		float fare = miles > 600 ? 0.05f * miles * 2 : 0.02f * miles * 2;
		return $"{DistanceModel.GetCityById(where)?.name}: about {miles:N0} miles; ~${fare + nights * PlayerDesk.HotelNightly * 3:N0} travel and {nights} nights for a trio";
	}

	public static string PreparationNote(MediaPreparation prep) => prep switch {
		MediaPreparation.Extra => "two rehearsals and a camera walk-through (~$40, 6h)",
		MediaPreparation.Minimal => "turn up and do it (no rehearsal cost)",
		_ => "one run-through with the arrangement cut to time (~$15, 2h)",
	};

	// ---- television ---------------------------------------------------------------------------------------------

	/// <summary>Send a disc and a short press sheet to a producer (§3.1). The answer comes in a few days.</summary>
	public static bool PitchTelevision(PlayerDesk desk, RecordRuntimeData record, string programId, out string message) {
		message = null;
		TvProgram program = MediaOutletCatalog.Get(programId);
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (!MediaAtmosphere.Observing) { message = "Media is switched off in this world."; return false; }
		if (program == null || record?.baseRecord == null) { message = "Pick a record and a program."; return false; }
		if (record.baseRecord.format != ReleaseFormat.Single) { message = "Producers book a single the dancers and the viewers can hear on the radio."; return false; }
		if (state.Pitches.Any(p => p.Kind == PitchKind.Television && p.ProgramId == programId && p.ArtistId == record.baseRecord.artistId
			&& p.Status is PitchStatus.Sent or PitchStatus.Offered or PitchStatus.Confirmed)) {
			message = $"{program.Name} already has this act in hand."; return false;
		}
		int day = BroadcastExposure.DayIndex(today);
		var pitch = new MediaPitch {
			Id = NextId("pitch"), Kind = PitchKind.Television, ProgramId = programId, RecordId = record.baseRecord.recordId, ArtistId = record.baseRecord.artistId,
			ArtistName = record.baseRecord.artistName, Title = record.baseRecord.title, ContactKey = ProducerKey(program), SentDay = day, Status = PitchStatus.Sent,
			CostNote = "1 promo copy, $0.15 postage, 1h at the desk (" + ObserveCostNote + ")",
		};
		pitch.AnswerDay = day + 2 + (int)(MediaAtmosphere.Unit("pitch-answer|" + pitch.Id) * 3);
		state.Pitches.Add(pitch);
		ProducerContact(program).Remember(today, $"Sent \"{pitch.Title}\" with a press sheet.");
		message = $"\"{pitch.Title}\" is on its way to {ProducerName(program)} at {MediaOutletCatalog.DisplayName(program, today)}. Expect an answer in a few days.";
		return true;
	}

	/// <summary>
	/// Producer consideration (§5.2): eligibility first, then a weighted look at local evidence, fit, the relationship and
	/// the discovery slot. Local shows are moderately easy for a plausible act; network shows want the chart.
	/// </summary>
	private static void ConsiderTelevision(PlayerDesk desk, MediaPitch pitch, GameDate today) {
		TvProgram program = MediaOutletCatalog.Get(pitch.ProgramId);
		RecordRuntimeData record = ChartManager.Instance?.GetAllRecords().FirstOrDefault(r => r.baseRecord?.recordId == pitch.RecordId);
		MediaContactMemory contact = ProducerContact(program);
		int day = BroadcastExposure.DayIndex(today);
		void Decline(string reason) {
			pitch.Status = PitchStatus.Declined; pitch.Reason = reason;
			contact.Remember(today, "Passed on \"" + pitch.Title + "\": " + reason);
			desk?.MediaNote($"{contact.Name} at {program.Name} passed on {pitch.ArtistName}: {reason}");
		}
		if (program == null || record == null) { Decline("the record never arrived."); return; }
		if (ProgramMemoryFor(program.Id, pitch.ArtistId) is { } m && m.ExcludedUntilDay > day) { Decline("not after last time."); return; }

		float fit = program.GenreFit(record.baseRecord.primaryGenre);
		string region = program.Network == TvNetwork.Local ? DistanceModel.GetCityById(program.HomeCityId)?.parentRegionId : null;
		float local = 0f;
		if (region != null && record.regionalData.TryGetValue(region, out var rd) && rd != null)
			local = Math.Clamp(rd.awareness * 1.5f + Math.Min(1f, rd.unitsSoldThisWeek / 400f), 0f, 1f);
		bool hometown = desk?.Label?.homeCityId == program.HomeCityId;
		int pos = record.currentPosition;
		bool broke = record.regionalData.Values.Any(x => x != null && x.peakBreakoutScore >= (CompetitorManager.Instance?.RegionalBreakoutDealThreshold ?? 0.2f));
		float p;
		string why;
		if (program.Network == TvNetwork.Local) {
			p = 0.20f + 0.35f * fit + 0.35f * local + (hometown ? 0.15f : 0f) + 0.15f * contact.Trust;
			why = fit < 0.4f ? "it isn't a record the dancers want." : local < 0.1f ? "nobody here is asking for it yet." : "the bill is full for the next few weeks.";
		} else if (program.Kind == TvProgramKind.NationalTeen) {
			p = pos > 0 && pos <= program.ChartCeiling ? 0.25f + 0.45f * (1f - pos / (float)program.ChartCeiling) * fit
				: program.DiscoverySlot && broke ? 0.18f * fit : 0.02f;
			why = pos <= 0 && !broke ? "they book records the national chart already shows." : "they went with a bigger record.";
		} else {
			p = pos > 0 && pos <= program.ChartCeiling ? 0.10f + 0.5f * (1f - pos / (float)program.ChartCeiling) * fit : 0f;
			why = "a family variety hour books names the parents already know.";
		}
		p += 0.1f * contact.Trust;
		if (MediaAtmosphere.Unit("pitch-consider|" + pitch.Id) >= Math.Clamp(p, 0f, 0.95f)) { Decline(why); return; }

		// An offer for a real date: the first episode not yet booked, a week or more away.
		int first = Math.Max(state.BookedThroughDay + 1, day + 8 + OfferAnswerDays);
		int air = -1;
		for (int d = first; d < first + 56; d++) {
			GameDate date = FromDayIndex(d);
			var v = program.VersionOn(date);
			if (v == null || v.RepeatMonths.Contains(date.month)) continue;
			if (state.Pitches.Any(x => x.Status == PitchStatus.Confirmed && x.ArtistId == pitch.ArtistId && x.AirDay == d)) continue;
			air = d; break;
		}
		if (air < 0) { Decline("they're off the air for a while."); return; }
		GameDate airDate = FromDayIndex(air);
		BroadcastScheduleVersion version = program.VersionOn(airDate);
		pitch.Status = PitchStatus.Offered;
		pitch.AirDay = air;
		pitch.OfferExpiresDay = day + OfferAnswerDays;
		pitch.Discovery = pos <= 0 || pos > program.ChartCeiling;
		pitch.SlotCount = Math.Max(1, program.GuestSlots);
		pitch.SlotIndex = program.Kind == TvProgramKind.PrimetimeVariety ? 0 : pitch.Discovery ? 0 : Math.Min(1, pitch.SlotCount - 1);
		var markets = BroadcastExposure.Markets(program, version, airDate, ChartManager.Instance?.GetAllRegions());
		ExposureEstimate e = BroadcastExposure.Estimate(program, version.DurationMinutes, pitch.SlotIndex, pitch.SlotCount, 0f, markets);
		(pitch.ReachLow, pitch.ReachHigh) = e.ReachRange();
		pitch.TeenShare = e.AverageByGroup.Sum() > 0 ? e.AverageByGroup[(int)ViewerGroup.Teen] / e.AverageByGroup.Sum() : 0;
		pitch.CostNote = TravelNote(desk?.Label?.homeCityId, program) + "; " + PreparationNote(MediaPreparation.Standard) + " (" + ObserveCostNote + ")";
		StandardsQuery q = MediaStandards.Review(program, airDate, pitch.Id, record, region, today);
		if (q != null) {
			q.PlayerQuery = true;
			q.Deadline = FromDayIndex(pitch.OfferExpiresDay);
			state.StandardsQueries.Add(q);
			pitch.QueryId = q.Id;
		}
		contact.Remember(today, $"Offered {airDate.ToShortString()} for {pitch.ArtistName}.");
		desk?.MediaNote($"{contact.Name} at {MediaOutletCatalog.DisplayName(program, today)} offered {pitch.ArtistName} a spot on {airDate.ToShortString()}" +
			(q != null ? $". {q.Authority} has a question about \"{pitch.Title}\"." : ". Answer from the office."));
	}

	/// <summary>Accept or turn down an offered date (§5.1). Acceptance reserves the slot against that episode.</summary>
	public static bool AnswerOffer(PlayerDesk desk, string pitchId, bool accept, MediaPreparation prep, out string message) {
		MediaPitch pitch = state.Pitches.FirstOrDefault(p => p.Id == pitchId);
		message = null;
		if (pitch == null || pitch.Status != PitchStatus.Offered) { message = "That offer isn't open."; return false; }
		TvProgram program = MediaOutletCatalog.Get(pitch.ProgramId);
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		MediaContactMemory contact = ProducerContact(program);
		if (!accept) {
			pitch.Status = PitchStatus.TurnedDown;
			contact.Remember(today, "You turned down the date.");
			message = "Turned down. No harm done for a date you never confirmed.";
			return true;
		}
		StandardsQuery q = pitch.QueryId == null ? null : state.StandardsQueries.FirstOrDefault(x => x.Id == pitch.QueryId);
		if (q != null && q.Resolution == StandardsResolution.Open) { message = $"{q.Authority} needs an answer about the song first."; return false; }
		if (q != null && q.Resolution == StandardsResolution.Withdrawn) { message = "You withdrew from this one."; return false; }
		if (pitch.AirDay <= state.BookedThroughDay) {
			pitch.Status = PitchStatus.Lapsed;
			message = "Too late: the producer filled the date while it sat on your desk.";
			return false;
		}
		pitch.Status = PitchStatus.Confirmed;
		pitch.Preparation = prep;
		contact.Remember(today, $"Confirmed {FromDayIndex(pitch.AirDay).ToShortString()}.");
		message = $"Confirmed: {pitch.ArtistName} on {MediaOutletCatalog.DisplayName(program, today)}, {FromDayIndex(pitch.AirDay).ToShortString()}. " +
			$"Preparation: {PreparationNote(prep)}.";
		return true;
	}

	/// <summary>The options a standards query actually allows for this booking (§7.3).</summary>
	public static List<StandardsResponse> StandardsOptions(StandardsQuery q) {
		TvProgram program = MediaOutletCatalog.Get(q.ProgramId);
		var record = ChartManager.Instance?.GetAllRecords().FirstOrDefault(r => r.baseRecord?.recordId == q.RecordId);
		bool alternate = record != null && ChartManager.Instance.GetAllRecords().Any(r => r.baseRecord?.artistId == q.ArtistId && r.baseRecord.recordId != q.RecordId
			&& r.baseRecord.format == ReleaseFormat.Single && MediaStandards.FindingsFor(r).Count == 0);
		return MediaStandards.Options(q, program?.Method ?? PerformanceMethod.Mime, alternate).ToList();
	}

	public static string ResponseLabel(StandardsResponse r) => r switch {
		StandardsResponse.ComplyEdit => "Agree to the change",
		StandardsResponse.AlternateSong => "Offer another song",
		StandardsResponse.ProducerDefends => "Ask the producer to fight for it",
		StandardsResponse.Withdraw => "Withdraw from the date",
		StandardsResponse.PromiseThenDefy => "Say yes; let the act sing it as written",
		_ => r.ToString(),
	};

	/// <summary>
	/// Answer a standards query. The act has the say where it has the creative control, and always on its own microphone:
	/// a label can recommend a change, never order a defiance (§7.3).
	/// </summary>
	public static bool AnswerStandards(PlayerDesk desk, string queryId, StandardsResponse response, out string message) {
		StandardsQuery q = state.StandardsQueries.FirstOrDefault(x => x.Id == queryId);
		message = null;
		if (q == null || q.Resolution != StandardsResolution.Open) { message = "That question has been settled."; return false; }
		if (!StandardsOptions(q).Contains(response)) { message = "That isn't possible on this show."; return false; }
		TvProgram program = MediaOutletCatalog.Get(q.ProgramId);
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(q.ArtistId);
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		bool needsConsent = response == StandardsResponse.PromiseThenDefy || response == StandardsResponse.ComplyEdit && artist?.artistCreativeControl == true;
		if (needsConsent && !MediaStandards.ActConsents(artist, response, q.Id)) {
			message = response == StandardsResponse.ComplyEdit
				? $"{q.ArtistName} won't change the line. It's their call under the contract."
				: $"{q.ArtistName} won't promise one thing and sing another.";
			return false;
		}
		var record = ChartManager.Instance?.GetAllRecords().FirstOrDefault(r => r.baseRecord?.artistId == q.ArtistId && r.baseRecord.recordId != q.RecordId
			&& r.baseRecord.format == ReleaseFormat.Single && MediaStandards.FindingsFor(r).Count == 0);
		Settle(q, response, program, program.VersionOn(q.AirDate) ?? program.Schedule.Last(), artist, record);
		MediaContactMemory contact = ProducerContact(program);
		MediaPitch pitch = state.Pitches.FirstOrDefault(p => p.QueryId == q.Id);
		switch (q.Resolution) {
			case StandardsResolution.Edited: contact.Trust = Math.Min(1f, contact.Trust + 0.05f); contact.Remember(today, "Agreed to the standards change without a fight."); break;
			case StandardsResolution.SongChanged: contact.Trust = Math.Min(1f, contact.Trust + 0.05f); contact.Remember(today, "Offered another song when standards asked."); break;
			case StandardsResolution.Withdrawn:
				contact.Trust = Math.Max(-1f, contact.Trust - 0.05f);
				contact.Remember(today, "Withdrew before confirming rather than change the song.");
				if (pitch != null) pitch.Status = PitchStatus.Withdrawn;
				break;
			case StandardsResolution.Cleared: contact.Remember(today, "Argued for the original; the network let it through."); break;
		}
		message = q.Outcome;
		return true;
	}

	// ---- press ----------------------------------------------------------------------------------------------------

	public static bool PitchPress(PlayerDesk desk, RecordRuntimeData record, string outletId, StoryAngle angle, out string message) {
		message = null;
		PressOutlet outlet = MediaOutletCatalog.GetPress(outletId);
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (!MediaAtmosphere.Observing) { message = "Media is switched off in this world."; return false; }
		if (outlet == null || record?.baseRecord == null) { message = "Pick a record and a desk."; return false; }
		if (state.Pitches.Any(p => p.Kind == PitchKind.Press && p.OutletId == outletId && p.RecordId == record.baseRecord.recordId)) {
			message = $"{outlet.Name} already has your sheet on \"{record.baseRecord.title}\"."; return false;
		}
		PressRole role = MediaPress.RoleFor(outlet, StoryAngle.LabelPitch);
		var pitch = new MediaPitch {
			Id = NextId("press"), Kind = PitchKind.Press, OutletId = outletId, RecordId = record.baseRecord.recordId, ArtistId = record.baseRecord.artistId,
			ArtistName = record.baseRecord.artistName, Title = record.baseRecord.title, ContactKey = MediaPress.ContactKey(outlet, role), Angle = angle,
			SentDay = BroadcastExposure.DayIndex(today), Status = PitchStatus.Sent, CostNote = "1 promo copy, a photo, postage, 1h (" + ObserveCostNote + ")",
		};
		state.Pitches.Add(pitch);
		DeskContact(outlet, role).Remember(today, $"Sent a press sheet on \"{pitch.Title}\" ({AngleLabel(angle).ToLowerInvariant()}).");
		message = $"Sent to {MediaPress.ContactName(outlet, role)} at {outlet.Name}. The editor decides at the next issue.";
		return true;
	}

	public static string AngleLabel(StoryAngle angle) => angle switch {
		StoryAngle.Hometown => "Local act makes a record",
		StoryAngle.Breakout => "Where it's selling",
		StoryAngle.CommunityNews => "Who made it and how",
		_ => "New release",
	};

	/// <summary>The label's press sheets become leads for exactly the desk they were sent to, built from the record's
	/// public evidence plus the label's own statement. An honest modest story can be useful (§9.2).</summary>
	private static void PlayerPressLeads(List<MediaPress.Lead> leads, Evidence evidence, int today) {
		foreach (MediaPitch pitch in state.Pitches.Where(p => p.Kind == PitchKind.Press && p.Status == PitchStatus.Sent && p.SentDay < today)) {
			PressOutlet outlet = MediaOutletCatalog.GetPress(pitch.OutletId);
			if (outlet == null || !evidence.ById.TryGetValue(pitch.RecordId ?? "", out RecordRuntimeData r)) { pitch.Status = PitchStatus.Passed; continue; }
			int broke = evidence.BrokeCount(r);
			string region = outlet.HomeCityId == null ? null : DistanceModel.GetCityById(outlet.HomeCityId)?.parentRegionId;
			float local = region != null && r.regionalData.TryGetValue(region, out var rd) && rd != null ? Math.Clamp(rd.awareness * 1.5f + Math.Min(1f, rd.unitsSoldThisWeek / 400f), 0f, 1f) : 0f;
			bool hometown = outlet.HomeCityId != null && evidence.CityOf(r.baseRecord.artistId) == outlet.HomeCityId
				|| outlet.HomeCityId != null && PlayerDesk.Instance?.Label?.homeCityId == outlet.HomeCityId;
			float interest = outlet.Family switch {
				PressFamily.LocalDaily => 0.25f + (hometown ? 0.3f : 0f) + 0.3f * local,
				PressFamily.Specialist => 0.2f + 0.3f * local + 0.1f * Math.Min(3, broke),
				PressFamily.Trade => 0.15f + 0.12f * Math.Min(4, broke) + (r.currentPosition > 0 ? 0.25f : 0f),
				PressFamily.TeenFan => r.currentPosition > 0 && r.currentPosition <= 30 ? 0.35f : 0.05f,
				PressFamily.RockCriticism => 0.25f + 0.3f * (r.baseRecord.hookStrength + r.baseRecord.productionQuality) * 0.5f,
				_ => r.currentPosition == 1 ? 0.6f : 0.02f,
			};
			// Angle fit: the hometown story suits the hometown paper; the breakout sheet suits the trades.
			if (pitch.Angle == StoryAngle.Hometown && outlet.Family == PressFamily.LocalDaily && hometown) interest += 0.1f;
			if (pitch.Angle == StoryAngle.Breakout && broke == 0) interest -= 0.15f;
			MediaContactMemory contact = ContactFor(pitch.ContactKey);
			interest += 0.1f * (contact?.Trust ?? 0f);
			var lead = NewLead("pitch|" + pitch.Id, outlet.Family == PressFamily.RockCriticism ? StoryAngle.Review : StoryAngle.LabelPitch, r, today, interest, evidence);
			lead.Story.Angle = outlet.Family == PressFamily.RockCriticism ? StoryAngle.Review : pitch.Angle == StoryAngle.Hometown ? StoryAngle.Hometown : StoryAngle.LabelPitch;
			lead.Story.PlayerSubject = true;
			Fact(lead, EvidenceSource.LabelStatement, $"the label's press sheet on \"{r.baseRecord.title}\"", pitch.SentDay, firsthand: true, confidence: 0.6f);
			if (broke > 0) Fact(lead, EvidenceSource.RegionalSales, $"selling in {broke} regional market(s) on dealers' reports", today);
			if (r.currentPosition > 0) Fact(lead, EvidenceSource.NationalChart, $"No. {r.currentPosition} on the national chart", today);
			if (outlet.Family == PressFamily.RockCriticism) Fact(lead, EvidenceSource.RecordItself, "the record", today, confidence: (r.baseRecord.hookStrength + r.baseRecord.productionQuality) * 0.5f);
			lead.Outlets.Add(outlet.Id);
			leads.Add(lead);
			pitch.Status = PitchStatus.Passed; // until the editor's decision says otherwise (AfterDecide)
			pitch.Reason = "pitch|" + pitch.Id;
		}
	}

	/// <summary>After editors decide: tell the label which sheets made the paper, and let reporters call about items on the
	/// label's records before the issue closes (§9.3).</summary>
	private static void AfterDecide(int today) {
		PlayerDesk desk = PlayerDesk.Instance;
		GameDate date = FromDayIndex(today);
		foreach (MediaPitch pitch in state.Pitches.Where(p => p.Kind == PitchKind.Press && p.Reason == "pitch|" + p.Id)) {
			MediaStory placed = state.Stories.FirstOrDefault(s => s.RootId == "pitch|" + pitch.Id && s.Status == StoryStatus.Scheduled);
			PressOutlet outlet = MediaOutletCatalog.GetPress(pitch.OutletId);
			MediaContactMemory contact = ContactFor(pitch.ContactKey);
			pitch.Reason = null;
			if (placed != null) {
				pitch.Status = PitchStatus.Placed;
				pitch.AirDay = placed.IssueDay;
				contact?.Remember(date, $"Ran your sheet on \"{pitch.Title}\" ({FromDayIndex(placed.IssueDay).ToShortString()}).");
				if (contact != null) contact.Trust = Math.Min(1f, contact.Trust + 0.05f);
				desk?.MediaNote($"{outlet?.Name} will run an item on \"{pitch.Title}\" ({FromDayIndex(placed.IssueDay).ToShortString()}).");
			} else {
				contact?.Remember(date, $"Passed on \"{pitch.Title}\": not enough there yet.");
				desk?.MediaNote($"{outlet?.Name} passed on your sheet for \"{pitch.Title}\".");
			}
		}
		if (desk?.Label == null) return;
		// A trade reporter checks figures on a label's breaking record before the issue closes.
		foreach (MediaStory s in state.Stories.Where(s => s.PlayerSubject && s.Status == StoryStatus.Scheduled && s.LeadDay == today && s.IssueDay > today + 1)) {
			PressOutlet outlet = MediaOutletCatalog.GetPress(s.OutletId);
			if (outlet == null) continue;
			if (outlet.Family == PressFamily.Trade && (s.Angle == StoryAngle.Breakout || s.Angle == StoryAngle.ChartNews)
				&& MediaAtmosphere.Unit("inquiry|" + s.Id) < 0.6f && !state.Inquiries.Any(i => i.StoryId == s.Id)) {
				var r = ChartManager.Instance?.GetAllRecords().FirstOrDefault(x => x.baseRecord?.recordId == s.RecordId);
				state.Inquiries.Add(new MediaInquiry {
					Id = NextId("inquiry"), Kind = InquiryKind.OrdersQuestion, OutletId = outlet.Id, ContactKey = MediaPress.ContactKey(outlet, PressRole.Reporter),
					ArtistId = s.ArtistId, ArtistName = s.ArtistName, RecordId = s.RecordId, Title = s.Title, StoryId = s.Id, Day = today, DeadlineDay = s.IssueDay - 1,
					ActualUnits = r?.totalUnitsSold ?? 0,
				});
				desk.MediaNote($"{MediaPress.ContactName(outlet, PressRole.Reporter)} of {outlet.Name} called: how many has \"{s.Title}\" actually sold?");
			}
			if (outlet.Family == PressFamily.TeenFan && s.Angle == StoryAngle.TeenFeature && !state.Inquiries.Any(i => i.StoryId == s.Id)) {
				state.Inquiries.Add(new MediaInquiry {
					Id = NextId("inquiry"), Kind = InquiryKind.PhotoRequest, OutletId = outlet.Id, ContactKey = MediaPress.ContactKey(outlet, PressRole.Editor),
					ArtistId = s.ArtistId, ArtistName = s.ArtistName, RecordId = s.RecordId, Title = s.Title, StoryId = s.Id, Day = today, DeadlineDay = s.IssueDay - 28,
				});
				desk.MediaNote($"{MediaPress.ContactName(outlet, PressRole.Editor)} at {outlet.Name} wants photographs of {s.ArtistName}.");
			}
		}
	}

	/// <summary>
	/// Answer a reporter or editor. Orders: "facts" (the real figure), "inflate" (a claim that can be checked later),
	/// "decline" (the item runs with "declined to comment"). Photos: "exclusive", "supplied", "decline", with a romance
	/// policy of "private", "dreamy" or "plant" (a planted story needs the act's consent and stays a claim).
	/// </summary>
	public static bool AnswerInquiry(PlayerDesk desk, string inquiryId, string answer, string romance, out string message) {
		MediaInquiry inquiry = state.Inquiries.FirstOrDefault(i => i.Id == inquiryId);
		message = null;
		if (inquiry == null || !inquiry.Open) { message = "That call has been dealt with."; return false; }
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int day = BroadcastExposure.DayIndex(today);
		PressOutlet outlet = MediaOutletCatalog.GetPress(inquiry.OutletId);
		MediaStory story = state.Stories.FirstOrDefault(s => s.Id == inquiry.StoryId);
		MediaContactMemory contact = DeskContact(outlet, inquiry.Kind == InquiryKind.OrdersQuestion ? PressRole.Reporter : PressRole.Editor);
		if (inquiry.Kind == InquiryKind.OrdersQuestion) {
			var r = ChartManager.Instance?.GetAllRecords().FirstOrDefault(x => x.baseRecord?.recordId == inquiry.RecordId);
			inquiry.ActualUnits = r?.totalUnitsSold ?? inquiry.ActualUnits;
			switch (answer) {
				case "facts":
					inquiry.ClaimedUnits = inquiry.ActualUnits;
					if (story != null) { story.ClaimedUnits = inquiry.ActualUnits; story.Evidence.Add(new MediaEvidence { Source = EvidenceSource.LabelStatement, Fact = $"the label reports {inquiry.ActualUnits:N0} sold", Firsthand = true, Day = day }); }
					contact.Trust = Math.Min(1f, contact.Trust + 0.1f);
					contact.Remember(today, $"Gave straight figures on \"{inquiry.Title}\".");
					message = "You gave the real figure. The item will carry it.";
					break;
				case "inflate":
					inquiry.ClaimedUnits = Math.Max(inquiry.ActualUnits * 5 / 2, inquiry.ActualUnits + 5000);
					inquiry.CheckDay = day + 21 + (int)(MediaAtmosphere.Unit("inflate-check|" + inquiry.Id) * 21);
					if (story != null) {
						story.ClaimedUnits = inquiry.ClaimedUnits; story.Inaccurate = true; story.InaccurateClaim = $"orders of {inquiry.ClaimedUnits:N0} claimed by the label";
						story.Evidence.Add(new MediaEvidence { Source = EvidenceSource.LabelStatement, Fact = $"the label claims {inquiry.ClaimedUnits:N0} sold", Firsthand = true, Day = day, Confidence = 0.4f });
					}
					contact.Remember(today, $"Quoted {inquiry.ClaimedUnits:N0} on \"{inquiry.Title}\".");
					message = $"You told them {inquiry.ClaimedUnits:N0}. It'll run. Distributors and buyers read the trades too, and they know their own orders.";
					break;
				default:
					if (story != null) story.Declined = true;
					contact.Remember(today, $"Declined to comment on \"{inquiry.Title}\".");
					message = "No comment. The item runs shorter, with a line saying so.";
					break;
			}
		} else {
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(inquiry.ArtistId);
			if (answer == "decline") {
				contact.Remember(today, $"Turned down photographs of {inquiry.ArtistName}.");
				message = "Declined. The feature may run on what they already have, or not at all.";
			} else {
				bool exclusive = answer == "exclusive";
				// The act controls its own participation (§8.2): read from the members' own disposition, keyed.
				float ego = artist?.members?.Where(m => m.isActive).Select(m => m.ego).DefaultIfEmpty(0.5f).Average() ?? 0.5f;
				float ambition = artist?.evolution?.artisticAmbition ?? 0.5f;
				bool agrees = MediaAtmosphere.Unit("photo-consent|" + inquiry.Id + "|" + answer) < Math.Clamp(0.85f - 0.4f * Math.Max(0f, ambition - 0.6f) + 0.1f * (ego - 0.5f), 0.3f, 0.97f);
				if (!agrees) { message = $"{inquiry.ArtistName} won't sit for a pin-up session. Their call."; return false; }
				bool plant = romance == "plant";
				if (plant && MediaAtmosphere.Unit("romance-consent|" + inquiry.Id) >= 0.35f) { message = $"{inquiry.ArtistName} won't go along with an invented romance."; return false; }
				state.Exclusives.Add(new MediaExclusive {
					Id = NextId("exclusive"), OutletId = outlet.Id, ArtistId = inquiry.ArtistId, ArtistName = inquiry.ArtistName, Exclusive = exclusive,
					Scope = exclusive ? $"new photographs of {inquiry.ArtistName}, first use in {outlet.Name}" : "supplied publicity photographs",
					FromDay = day, UntilDay = day + (exclusive ? 28 : 0), Romance = romance ?? "private", PlantedClaim = plant,
					ContradictionDay = plant ? day + 28 + (int)(MediaAtmosphere.Unit("romance-check|" + inquiry.Id) * 56) : -1,
				});
				contact.Trust = Math.Min(1f, contact.Trust + (exclusive ? 0.15f : 0.05f));
				contact.Remember(today, exclusive ? $"Gave an exclusive session with {inquiry.ArtistName} (four weeks)." : $"Sent photographs of {inquiry.ArtistName}.");
				message = exclusive ? $"An exclusive session for {outlet.Name}: their pictures, first use, for four weeks. Not every photograph forever."
					: "Photographs sent.";
				if (plant) message += " The romance angle is in the copy now. It's a story, and stories get checked.";
			}
		}
		inquiry.Open = false;
		inquiry.Answer = answer;
		return true;
	}

	/// <summary>Ask a desk to correct an inaccurate item (§9.4). The correction runs under the same root; the original stays in history.</summary>
	public static bool RequestCorrection(PlayerDesk desk, string storyId, out string message) {
		MediaStory s = state.Stories.FirstOrDefault(x => x.Id == storyId);
		message = null;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int day = BroadcastExposure.DayIndex(today);
		if (s == null || !s.Inaccurate || s.Corrected || !s.PublishedBy(day)) { message = "Nothing to correct there."; return false; }
		PressOutlet outlet = MediaOutletCatalog.GetPress(s.OutletId);
		s.Corrected = true;
		bool ownClaim = s.InaccurateClaim?.Contains("claimed by the label") == true;
		var correction = new MediaStory {
			Id = "correction|" + s.Id, RootId = s.RootId, OutletId = s.OutletId, Angle = StoryAngle.Correction, Status = StoryStatus.Scheduled,
			ArtistId = s.ArtistId, ArtistName = s.ArtistName, RecordId = s.RecordId, Title = s.Title, LabelId = s.LabelId, PlayerSubject = true,
			LeadDay = day, IssueDay = MediaPress.NextIssueDay(outlet, day + 1), Interest = 0.2f,
		};
		correction.Evidence.Add(new MediaEvidence { Source = EvidenceSource.LabelStatement, Fact = ownClaim ? "the label revised its own figure" : "the label pointed out the error: " + s.InaccurateClaim, Firsthand = true, Day = day });
		state.Stories.Add(correction);
		MediaContactMemory contact = DeskContact(outlet, MediaPress.RoleFor(outlet, s.Angle));
		contact.Trust = Math.Min(1f, contact.Trust + (ownClaim ? 0.02f : 0.05f));
		contact.Remember(today, ownClaim ? $"Corrected your own figure on \"{s.Title}\"." : $"Pointed out an error on \"{s.Title}\"; they ran a correction.");
		message = $"{outlet.Name} will run a correction ({FromDayIndex(correction.IssueDay).ToShortString()}). The original item doesn't vanish; fewer people repeat it.";
		return true;
	}

	// ---- the label's day ------------------------------------------------------------------------------------------

	/// <summary>Daily, from PlayerDesk.OnDayStarted, only with a player label: producers answer, offers lapse, aired dates
	/// are reported, inquiries pass their deadlines, inflated figures and planted stories meet the facts.</summary>
	public static void PlayerDay(PlayerDesk desk, GameDate today) {
		if (!MediaAtmosphere.Observing || desk?.Label == null) return;
		int day = BroadcastExposure.DayIndex(today);
		foreach (MediaPitch pitch in state.Pitches.Where(p => p.Kind == PitchKind.Television).ToList()) {
			if (pitch.Status == PitchStatus.Sent && day >= pitch.AnswerDay) ConsiderTelevision(desk, pitch, today);
			else if (pitch.Status == PitchStatus.Offered && day > pitch.OfferExpiresDay) {
				pitch.Status = PitchStatus.Lapsed;
				TvProgram program = MediaOutletCatalog.Get(pitch.ProgramId);
				if (program != null) ProducerContact(program).Remember(today, "Never heard back about the date; it went to someone else.");
				desk.MediaNote($"The {MediaOutletCatalog.Get(pitch.ProgramId)?.Name} date for {pitch.ArtistName} lapsed: nobody answered.");
			}
		}
		foreach (MediaAppearance a in state.Appearances.Where(a => a.PlayerBooking && !a.Reported && BroadcastExposure.DayIndex(a.Date) < day)) {
			a.Reported = true;
			TvProgram program = MediaOutletCatalog.Get(a.ProgramId);
			MediaPitch pitch = state.Pitches.FirstOrDefault(p => p.AppearanceId == a.Id);
			if (pitch != null) pitch.Status = PitchStatus.Aired;
			MediaContactMemory contact = ProducerContact(program);
			StandardsQuery q = a.StandardsQueryId == null ? null : state.StandardsQueries.FirstOrDefault(x => x.Id == a.StandardsQueryId);
			if (a.Cut) {
				contact.Trust = Math.Max(-1f, contact.Trust - (q?.Resolution == StandardsResolution.Cut ? 0.5f : 0.1f));
				contact.Remember(a.Date, q?.Resolution == StandardsResolution.Cut ? "Sang the line they'd agreed to change; the segment was cut." : "The segment didn't make the broadcast.");
				desk.MediaNote($"{a.ArtistName}'s segment on {program?.Name} was cut before broadcast. Nobody saw it.");
				continue;
			}
			double reach = a.Airings.FirstOrDefault()?.Exposure.UniqueReach ?? 0;
			double teens = a.Airings.FirstOrDefault()?.Exposure.AverageByGroup[(int)ViewerGroup.Teen] ?? 0;
			if (q?.Resolution == StandardsResolution.Defied) {
				contact.Trust = Math.Max(-1f, contact.Trust - 0.6f);
				contact.Remember(a.Date, "Sang the line they'd agreed to change, live on the air.");
			} else if (a.Performance?.Outcome == PerformanceOutcome.Breakdown && a.Performance.Fault == PerformanceFault.Preparation) {
				contact.Trust = Math.Max(-1f, contact.Trust - 0.1f);
				contact.Remember(a.Date, "Under-rehearsed; it showed.");
			} else {
				contact.Trust = Math.Min(1f, contact.Trust + (a.Performance?.Outcome >= PerformanceOutcome.Engaging ? 0.2f : 0.1f));
				contact.Remember(a.Date, "Arrived on time and delivered.");
			}
			// A little mail: a rough count of letters, proportional to the teenagers who saw it and how it went. Observation only.
			float spark = a.Performance?.Outcome switch { PerformanceOutcome.Electrifying => 3f, PerformanceOutcome.Engaging => 1.6f, PerformanceOutcome.FlatAdequate => 0.8f, _ => 0.3f };
			int letters = (int)Math.Round(teens / 20_000.0 * spark * (0.7 + 0.6 * MediaAtmosphere.Unit("mail|" + a.Id)));
			string mail = letters >= 2 ? $" About {letters} letters came in afterwards." : letters == 1 ? " One letter came in afterwards." : "";
			desk.MediaNote($"TV: {a.ArtistName} on {MediaOutletCatalog.DisplayName(program, a.Date)}. {a.Performance?.Account} " +
				$"An estimated {MediaText.Audience(reach)} saw it.{mail}");
		}
		foreach (MediaInquiry inquiry in state.Inquiries.Where(i => i.Open && day > i.DeadlineDay)) {
			inquiry.Open = false; inquiry.Answer = "no-answer";
			MediaStory story = state.Stories.FirstOrDefault(s => s.Id == inquiry.StoryId);
			if (story != null && inquiry.Kind == InquiryKind.OrdersQuestion) story.Declined = true;
		}
		foreach (MediaInquiry inquiry in state.Inquiries.Where(i => !i.Checked && i.CheckDay >= 0 && day >= i.CheckDay)) {
			inquiry.Checked = true;
			var r = ChartManager.Instance?.GetAllRecords().FirstOrDefault(x => x.baseRecord?.recordId == inquiry.RecordId);
			long actual = r?.totalUnitsSold ?? inquiry.ActualUnits;
			if (actual >= inquiry.ClaimedUnits * 0.6) continue; // the record grew into the claim
			PressOutlet outlet = MediaOutletCatalog.GetPress(inquiry.OutletId);
			MediaContactMemory contact = DeskContact(outlet, PressRole.Reporter);
			contact.Trust = Math.Max(-1f, contact.Trust - 0.4f);
			contact.Remember(today, $"Your figure on \"{inquiry.Title}\" didn't hold up against the dealers' reports.");
			var followUp = new MediaStory {
				Id = "followup|" + inquiry.Id, RootId = "followup|" + inquiry.Id, OutletId = outlet.Id, Angle = StoryAngle.OrdersFollowUp, Tone = StoryTone.Skeptical,
				Status = StoryStatus.Scheduled, ArtistId = inquiry.ArtistId, ArtistName = inquiry.ArtistName, RecordId = inquiry.RecordId, Title = inquiry.Title,
				PlayerSubject = true, LeadDay = day, IssueDay = MediaPress.NextIssueDay(outlet, day + outlet.LeadDays), ClaimedUnits = inquiry.ClaimedUnits,
			};
			followUp.Evidence.Add(new MediaEvidence { Source = EvidenceSource.RegionalSales, Fact = $"dealers' reports put it near {actual:N0}, not the {inquiry.ClaimedUnits:N0} the label claimed", Firsthand = true, Day = day });
			state.Stories.Add(followUp);
			desk.MediaNote($"{outlet.Name} is checking the figure you gave on \"{inquiry.Title}\" against dealers' reports.");
		}
		foreach (MediaExclusive e in state.Exclusives.Where(x => x.PlantedClaim && !x.Contradicted && x.ContradictionDay >= 0 && day >= x.ContradictionDay)) {
			e.Contradicted = true;
			if (MediaAtmosphere.Unit("romance-contradict|" + e.Id) >= 0.4f) continue;
			PressOutlet outlet = MediaOutletCatalog.GetPress(e.OutletId);
			MediaContactMemory contact = DeskContact(outlet, PressRole.Editor);
			contact.Trust = Math.Max(-1f, contact.Trust - 0.3f);
			contact.Remember(today, $"The romance you planted for {e.ArtistName} came apart in another magazine.");
			desk.MediaNote($"Another magazine contradicted the romance story you planted about {e.ArtistName}. {outlet.Name}'s editor is not pleased, and neither is the act.");
		}
	}

	// ---- reading for the office --------------------------------------------------------------------------------------

	public static IEnumerable<MediaStory> PlayerClippings(int today) =>
		state.Stories.Where(s => s.PlayerSubject && s.Status == StoryStatus.Scheduled && s.IssueDay <= today).OrderByDescending(s => s.IssueDay);

	public static IEnumerable<StandardsQuery> OpenPlayerQueries() =>
		state.StandardsQueries.Where(q => q.PlayerQuery && q.Resolution == StandardsResolution.Open);
}
