using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 4 (§7.1): paid demos -- producing a session in a hired room for an unsigned local act.
///
/// - <b>Clients are real acts.</b> Only an unsigned act working this scene (on a bill in town tonight), wanting a record
///   (a seeking or latent prospect), with the money to pay for it, can be a client. Its money is what its people have
///   actually been paid by the town's rooms this year and last (<see cref="LocalSceneRoomService.Realized"/>) plus what
///   they hold (<see cref="Musician.wealth"/>), plus, for a day-job act, what its people have put aside from their jobs (a
///   half-year at $2 a member a week), less what it has spent on demos with you; it only books a demo
///   costing at most two-thirds of that. Nothing is invented and nothing in the act's own state is changed.
/// - <b>The ask</b> is in person, between sets, while the act is on a bill in your town (1h). The answer reads the
///   label's reputation, whether the act wants a record, whether your acts' people know theirs, whether you've heard
///   them, and how big the bill is against what they have. A no stands for a quarter.
/// - <b>The quote</b> is the cheapest free local room (the studio sheet's own rooms, rates and booked-solid calendar) for
///   <see cref="DemoHours"/> on the first open day 3-14 days out, from <see cref="DemoStartHour"/>, plus an acetate dub
///   for the act to take away, plus the producer's fee. Engineer is the room's own; no hired players.
/// - <b>The deposit</b> is the room and the dub, paid by the client when they agree and <i>held</i>: it is never the label's
///   cash. On the day it pays the room and the cutting room. The fee is paid on delivery and is the label's only income.
///   So the label never advances a session it can't afford, and a deposit can't be spent as if it were profit.
/// - <b>On the day</b> the founder produces it at the room (the session's hours). The act gets an acetate of its own
///   song for its own use: no master, no publishing, no option, no signing. The label hears them work in a studio: a
///   bounded demo sample on the act's scouting file. Signing is a separate negotiation.
/// - <b>Cancellations.</b> If the act can't make it (left town, broke up, lineup changed), the room keeps half its fee
///   from the deposit and the rest goes back. If the founder doesn't turn up, the label refunds the whole deposit, pays
///   the room's half itself, and loses some standing; that act won't book with you for a year. Cancelling a day or
///   more ahead is free; on the day, the room's half is the label's.
/// Authored values, not sourced: the $20 fee, the 3h session, the $20 dub, the lead days, the acceptance weights, the
/// two-thirds affordability rule, the room's half for a same-day cancellation.
/// </summary>
public partial class PlayerDesk {
	public enum DemoStatus { Booked, Delivered, ClientCancelled, LabelMissed, LabelCancelled }

	public const int DemoHours = MinSessionHours;
	public const int DemoStartHour = 10;
	public const int DemoOfferHours = 1;
	public const float DemoProducerFee = 20f;
	public const float DemoDubCost = 20f;
	public const int DemoLeadDays = 3;
	public const int DemoHorizonDays = 14;
	public const int DemoRefusalDays = 91;
	public const int DemoMissedRefusalDays = 365;
	public const float DemoAffordableShare = 2f / 3f;
	public const float DemoNoShowRoomShare = 0.5f;
	public const float DemoMissedReputation = 0.01f;
	/// <summary>What a day-job act's member puts aside a week toward the band (about 2% of a $90 1960 factory wage), and
	/// how many weeks of it the band holds at most (half a year; spending refills over the same span). Authored.</summary>
	public const float DemoDayJobSavingPerWeek = 2f;
	public const int DemoDayJobSavingWeeks = 26;

	public sealed class DemoJob {
		public string Id;
		public string ArtistId;
		public string ArtistName;
		public string RoomId;
		public string RoomName;
		public int Day;
		public int StartHour;
		public int Hours;
		public float RoomCost;
		public float DubCost;
		public float Fee;
		/// <summary>The client's money held against the room and the dub. Never in the label's cash.</summary>
		public float DepositHeld;
		public DemoStatus Status;
		public int AgreedDay;
		public string Outcome;
		public float Gross => RoomCost + DubCost + Fee;
		public float Deposit => RoomCost + DubCost;
	}

	public sealed record DemoQuote(string RoomId, string RoomName, int Day, int StartHour, int Hours, float RoomCost, float DubCost, float Fee) {
		public float Deposit => RoomCost + DubCost;
		public float Gross => RoomCost + DubCost + Fee;
	}

	private readonly List<DemoJob> demoJobs = new();
	/// <summary>artistId -> scene day until which that act won't hear a demo pitch from the label.</summary>
	private readonly Dictionary<string, int> demoRefusals = new(StringComparer.Ordinal);
	private int demoOrdinal;

	public IReadOnlyList<DemoJob> DemoJobs => demoJobs;
	public int? DemoRefusedUntil(string artistId) => demoRefusals.TryGetValue(artistId ?? "", out int until) && until >= DemoToday ? until : null;
	private static int DemoToday => LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);

	/// <summary>What an act's people have been paid by the scene this year and last, plus what they hold, less what the
	/// act has already paid you for demos. The act's own state is read, never written.</summary>
	public float DemoClientMeans(SimulatedArtist act) {
		if (act == null) return 0f;
		int year = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).year, today = DemoToday;
		var people = act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active && !string.IsNullOrEmpty(m.personId)).ToList();
		float earned = 0f;
		foreach (var m in people) {
			earned += LocalSceneRoomService.Realized(act.artistId, m.personId, year).Pay + LocalSceneRoomService.Realized(act.artistId, m.personId, year - 1).Pay;
			earned += Mathf.Max(0f, m.wealth);
		}
		// Author decision 2026-10-14: most local acts keep their day jobs (LocalSceneRoomService.FullTime), and the band's
		// money comes from them: each member puts a little aside, up to half a year's worth. Spending refills over the same span.
		bool dayJobs = !LocalSceneRoomService.FullTime(act, today);
		if (dayJobs) earned += people.Count * DemoDayJobSavingPerWeek * DemoDayJobSavingWeeks;
		float spent = demoJobs.Where(j => j.ArtistId == act.artistId && (!dayJobs || today - j.AgreedDay < DemoDayJobSavingWeeks * 7)).Sum(j => j.Status switch {
			DemoStatus.Delivered => j.Gross,
			DemoStatus.ClientCancelled => j.RoomCost * DemoNoShowRoomShare,
			DemoStatus.Booked => j.Deposit,
			_ => 0f,
		});
		return Mathf.Max(0f, earned - spent);
	}

	private static bool WantsARecord(SimulatedArtist act) => act.prospectMarketStatus is ProspectMarketStatus.Seeking or ProspectMarketStatus.Latent;

	/// <summary>Unsigned acts on a bill in this town today, still to play or playing: the people you could pitch a demo
	/// to between sets. With each, the bill and why it can't be pitched (null when it can).</summary>
	public IReadOnlyList<(SimulatedArtist Act, SceneBill Bill, string Block)> DemoProspects(string cityId) {
		var list = new List<(SimulatedArtist, SceneBill, string)>();
		var clock = TimeManager.Instance;
		if (Label == null || clock == null || !LocalScenes.Rooms) return list;
		int today = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var bill in LocalSceneRoomService.AnnouncedCalendar(cityId, clock.CurrentDate).Where(b => b.Day == today && b.Status == SceneBillStatus.Scheduled && b.EndHour * 60 > now)
			.OrderBy(b => b.StartHour).ThenBy(b => b.RoomId, StringComparer.Ordinal)) {
			var room = SceneRoomCatalog.Get(bill.RoomId);
			if (room == null || !room.IsPerformance) continue;
			foreach (var slot in bill.Appearances.Where(a => a.Status != SceneBillStatus.Cancelled)) {
				var act = ArtistManager.Instance?.GetArtist(slot.ArtistId);
				if (act == null || !string.IsNullOrEmpty(act.labelId) || !seen.Add(act.artistId)) continue;
				list.Add((act, bill, DemoPitchBlock(act, bill)));
			}
		}
		return list;
	}

	private string DemoPitchBlock(SimulatedArtist act, SceneBill bill) {
		var clock = TimeManager.Instance;
		int now = clock.CurrentHour * 60 + clock.CurrentMinute;
		if (!WantsARecord(act)) return $"{act.stageName} aren't looking to make a record.";
		if (DemoRefusedUntil(act.artistId) is int until) return $"{act.stageName} said no; ask again after {LocalSceneRoomService.Date(until).ToShortString()}.";
		if (demoJobs.Any(j => j.ArtistId == act.artistId && j.Status == DemoStatus.Booked)) return $"{act.stageName} already have a demo booked with you.";
		var quote = DemoQuoteFor(act);
		if (quote == null) return "No local room is free in the next two weeks.";
		if (quote.Gross > DemoClientMeans(act) * DemoAffordableShare) return $"{act.stageName} haven't the money for a ${quote.Gross:N0} demo.";
		if (now < bill.StartHour * 60) return $"Catch them between sets, from {Clock12(bill.StartHour)}.";
		return null;
	}

	/// <summary>The quote: the cheapest local room free on the first open day 3-14 days out, the dub and the fee.</summary>
	public DemoQuote DemoQuoteFor(SimulatedArtist act) {
		if (Label == null || act == null) return null;
		int today = DemoToday;
		var people = act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active && !string.IsNullOrEmpty(m.personId)).Select(m => m.personId).ToList();
		var roomIds = StudioRoomCatalog.Available(Label.homeCityId, Label.homeRegion).Select(t => StudioRoomCatalog.LocalId(Label.homeCityId, t)).ToList();
		for (int day = today + DemoLeadDays; day <= today + DemoHorizonDays; day++) {
			var date = LocalSceneRoomService.Date(day);
			// Busy elsewhere that day (another room's bill in the session hours), or our own demo already in a room that day.
			if (people.Any(p => LocalSceneRoomService.HasPersonBooking(p, day, DemoStartHour * 60, (DemoStartHour + DemoHours) * 60, null))) continue;
			if (demoJobs.Any(j => j.Status == DemoStatus.Booked && j.Day == day)) continue;
			DemoQuote best = null;
			foreach (string id in roomIds) {
				float cost; string name;
				if (StudioExperience.Enabled) {
					var room = StudioRoom(id, act.artistId, null, date);
					if (room == null || room.Specialist || StudioBookedSolid(room, date) != null) continue;
					cost = StudioRoomCost(room, DemoHours); name = room.Name;
				} else {
					StudioRoomCatalog.ById(id, out var tier);
					cost = SessionCost(tier, DemoHours); name = StudioRoomCatalog.ById(id, out _)?.Name ?? "the local room";
				}
				if (best == null || cost < best.RoomCost) best = new DemoQuote(id, name, day, DemoStartHour, DemoHours, R2(cost), DemoDubCost, DemoProducerFee);
			}
			if (best != null) return best;
		}
		return null;
	}

	/// <summary>The act's yes, before the roll. The affordability gate already guarantees they can pay, so price only
	/// weighs where the bill takes more than a third of what they have. Authored weights.</summary>
	public float DemoAcceptChance(SimulatedArtist act, DemoQuote quote) {
		float means = Mathf.Max(1f, DemoClientMeans(act));
		bool seeking = act.prospectMarketStatus == ProspectMarketStatus.Seeking;
		var ours = Roster.SelectMany(a => a.members.Where(m => m.isActive && !string.IsNullOrEmpty(m.personId)).Select(m => m.personId)).ToList();
		bool known = act.members.Any(m => !string.IsNullOrEmpty(m.personId) && ours.Any(o => ContactNetworkService.Edge(o, m.personId) is { Fallout: false }));
		bool heard = HasScoutingEvidence(act.artistId);
		return Mathf.Clamp(0.20f + (seeking ? 0.25f : 0.10f) + 0.25f * Mathf.Clamp(Label.reputation, 0f, 1f) + (known ? 0.15f : 0f) + (heard ? 0.10f : 0f)
			- 0.30f * Mathf.Max(0f, quote.Gross / means - 1f / 3f), 0.05f, 0.85f);
	}

	/// <summary>§7.1: pitch a demo to an act between sets. A yes books the room, takes the deposit into hold, and dates it.</summary>
	public bool OfferDemo(string artistId, out string message) {
		message = null;
		var act = ArtistManager.Instance?.GetArtist(artistId);
		var prospect = DemoProspects(CurrentCityId).FirstOrDefault(p => p.Act.artistId == artistId);
		if (act == null || prospect.Act == null) { message = "They're not on a bill in town tonight."; return false; }
		if (prospect.Block != null) { message = prospect.Block; return false; }
		if (!Require(DemoOfferHours, out message)) return false;
		Spend(DemoOfferHours);
		var quote = DemoQuoteFor(act);
		float chance = DemoAcceptChance(act, quote);
		if (GD.Randf() >= chance) {
			demoRefusals[artistId] = DemoToday + DemoRefusalDays;
			message = $"{act.stageName} heard you out at the bar: ${quote.Gross:N0} for a demo is more than they want to spend right now.";
			Note(message);
			Changed?.Invoke();
			return true;
		}
		var job = new DemoJob {
			Id = $"demo-{++demoOrdinal}", ArtistId = act.artistId, ArtistName = act.stageName, RoomId = quote.RoomId, RoomName = quote.RoomName,
			Day = quote.Day, StartHour = quote.StartHour, Hours = quote.Hours, RoomCost = quote.RoomCost, DubCost = quote.DubCost, Fee = quote.Fee,
			DepositHeld = quote.Deposit, Status = DemoStatus.Booked, AgreedDay = DemoToday,
		};
		demoJobs.Add(job);
		message = $"{act.stageName} will cut a demo with you at {quote.RoomName} on {LocalSceneRoomService.Date(quote.Day).ToShortString()}, " +
			$"{Clock12(quote.StartHour)} for {quote.Hours}h. They put down ${quote.Deposit:N2} for the room and the dub (held for the room, not yours); " +
			$"your ${quote.Fee:N0} is paid when you hand them the acetate.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	private string DemoActUnavailable(DemoJob job) {
		var act = ArtistManager.Instance?.GetArtist(job.ArtistId);
		if (act == null || act.lifecycleStatus != ArtistLifecycleStatus.Active) return $"{job.ArtistName} have broken up.";
		var people = act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToList();
		if (people.Count == 0) return $"{job.ArtistName} have nobody left to play.";
		string scene = LocalScenePersistenceService.SceneIdFor(Label.homeCityId);
		if (scene != null && !LocalScenePersistenceService.Present(act, scene, job.Day / 7)) return $"{job.ArtistName} have left town.";
		return null;
	}

	/// <summary>Produce a booked demo: at home, on its day, from its hour. Pays the room and the dub from the deposit, takes
	/// the fee, gives the act its acetate and puts a demo sample on their scouting file.</summary>
	public bool ProduceDemo(string jobId, out string message) {
		var job = demoJobs.FirstOrDefault(j => j.Id == jobId);
		if (job == null || job.Status != DemoStatus.Booked) { message = "No such demo booked."; return false; }
		var clock = TimeManager.Instance;
		if (DemoToday != job.Day) { message = $"The demo is booked for {LocalSceneRoomService.Date(job.Day).ToShortString()}."; return false; }
		if (!AtHome) { message = $"The demo is at {job.RoomName}; you have to be home."; return false; }
		if (clock.CurrentHour < job.StartHour - 1 || clock.CurrentHour > job.StartHour + 1) { message = $"The room is booked from {Clock12(job.StartHour)}."; return false; }
		if (pendingSession != null || InStudio) { message = "Finish the label's own studio date first."; return false; }
		if (DemoActUnavailable(job) is { } gone) {
			SettleClientCancelled(job, gone);
			message = job.Outcome;
			Changed?.Invoke();
			return true;
		}
		if (!Require(job.Hours, out message)) return false;
		Spend(job.Hours);
		// The deposit pays the room and the cutting room; the client pays the fee on delivery.
		job.DepositHeld = 0f;
		job.Status = DemoStatus.Delivered;
		Label.cashReserves += job.Fee;
		Label.monthlyRevenue += job.Fee;
		DemoSample(job);
		job.Outcome = $"Produced {job.ArtistName}'s demo at {job.RoomName}. They left with an acetate of their song and paid your ${job.Fee:N0}.";
		message = job.Outcome;
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>What the producer heard: a bounded studio sample, like the scouting demo, on the act's own file.</summary>
	private void DemoSample(DemoJob job) {
		var act = ArtistManager.Instance.GetArtist(job.ArtistId);
		var people = act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToArray();
		if (people.Length == 0) return;
		var voices = people.Where(m => m.isLeadVocalist).ToArray();
		float Sample(string axis, float value) => Math.Clamp(value + (float)(LocalSceneIdentityService.AssignmentUnit(
			LocalSceneIdentityService.KeyedSeed, job.Id + "|" + Label.labelId + "|" + axis) * 2 - 1) * .18f, 0, 1);
		var sample = new ScenePerformanceSnapshot { Id = job.Id,
			Vocals = voices.Length == 0 ? null : Sample("vocals", voices.Average(m => m.axesVersion > 0 ? (m.vocalPower + m.vocalControl) / 2 : m.technicalSkill)),
			Execution = Sample("execution", people.Average(m => m.studioEfficiency)) };
		var interpretation = ScenePerformanceService.Interpret(sample, Label.labelId, Label.scoutingAbility, 1);
		var dossier = Dossier(act.artistId, act.stageName);
		int year = LocalSceneRoomService.Date(job.Day).year;
		dossier.Observations.Add(new() { Id = $"paid-demo-v1|{Label.labelId}|{job.Id}", EventId = job.Id, LabelId = Label.labelId,
			ArtistId = act.artistId, ArtistName = act.stageName, Kind = ScoutingEvidenceKind.DemoSample, RoomName = job.RoomName,
			SourceId = Label.labelId, SourceName = "Your demo session", PersonIds = people.Select(m => m.personId).ToList(),
			Set = new() { new SceneSetSong { Title = LocalSceneRoomService.OriginalTitle(act, year), Genre = act.primaryGenre, Original = true } },
			OccurredDay = job.Day, AcquiredDay = job.Day, AcquiredMinute = (job.StartHour + job.Hours) * 60,
			StartMinute = job.StartHour * 60, EndMinute = (job.StartHour + job.Hours) * 60, Coverage = 1, Interpretation = interpretation,
			Conditions = $"Paid demo: the act's own song, {job.Hours}h at {job.RoomName}, client-funded. No audience or release evidence.",
			Credibility = "bounded controlled sample; no future ceiling or signing commitment",
			Claim = "Ensemble in this sample: " + AxisDescription(interpretation.Execution).Replace("set", "sample") + ". " +
				(interpretation.Vocals.HasValue ? "Vocals: " + AxisDescription(interpretation.Vocals.Value).Replace("set", "sample") + "." : "No lead-vocal evidence.") });
	}

	/// <summary>The act couldn't make it: the room keeps half its fee from the deposit, the rest goes back to them.</summary>
	private void SettleClientCancelled(DemoJob job, string why) {
		float kept = R2(job.RoomCost * DemoNoShowRoomShare);
		job.DepositHeld = 0f;
		job.Status = DemoStatus.ClientCancelled;
		job.Outcome = $"{why} The demo at {job.RoomName} is off: the room kept ${kept:N2} of their deposit and the rest went back to them. Nothing was owed to you.";
		Note(job.Outcome);
	}

	/// <summary>The founder didn't produce it: refund the whole deposit, pay the room's half from the label, lose standing.</summary>
	private void SettleLabelMissed(DemoJob job) {
		float roomShare = R2(job.RoomCost * DemoNoShowRoomShare);
		job.DepositHeld = 0f;
		job.Status = DemoStatus.LabelMissed;
		Label.cashReserves -= roomShare;
		Label.monthlyExpenses += roomShare;
		Label.reputation = Mathf.Max(0f, Label.reputation - DemoMissedReputation);
		demoRefusals[job.ArtistId] = job.Day + DemoMissedRefusalDays;
		job.Outcome = $"You never turned up for {job.ArtistName}'s demo at {job.RoomName}. Their ${job.Deposit:N2} went back to them, the room billed you ${roomShare:N2}, and word gets round.";
		Note(job.Outcome);
	}

	/// <summary>Call off a booked demo. A day or more ahead it costs nothing; on the day the room's half is yours.</summary>
	public bool CancelDemo(string jobId, out string message) {
		var job = demoJobs.FirstOrDefault(j => j.Id == jobId);
		if (job == null || job.Status != DemoStatus.Booked) { message = "No such demo booked."; return false; }
		float roomShare = job.Day - DemoToday >= 1 ? 0f : R2(job.RoomCost * DemoNoShowRoomShare);
		job.DepositHeld = 0f;
		job.Status = DemoStatus.LabelCancelled;
		if (roomShare > 0f) { Label.cashReserves -= roomShare; Label.monthlyExpenses += roomShare; }
		job.Outcome = $"Called off {job.ArtistName}'s demo. Their ${job.Deposit:N2} went back to them" + (roomShare > 0f ? $"; the room billed you ${roomShare:N2} for the short notice." : ".");
		message = job.Outcome;
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Morning: settle any demo whose day has passed unproduced (OnDayStarted).</summary>
	private void SettlePastDemos() {
		int today = DemoToday;
		foreach (var job in demoJobs.Where(j => j.Status == DemoStatus.Booked && j.Day < today).ToList()) {
			if (DemoActUnavailable(job) is { } gone) SettleClientCancelled(job, gone);
			else SettleLabelMissed(job);
		}
	}

	// ── Checks ──────────────────────────────────────────────────────────────────────────────────────

	/// <summary>Checks only: book a job as <see cref="OfferDemo"/> would on a yes, without the pitch or the roll.</summary>
	internal DemoJob StageDemoForChecks(SimulatedArtist act, DemoQuote quote) {
		var job = new DemoJob { Id = $"demo-{++demoOrdinal}", ArtistId = act.artistId, ArtistName = act.stageName, RoomId = quote.RoomId, RoomName = quote.RoomName,
			Day = quote.Day, StartHour = quote.StartHour, Hours = quote.Hours, RoomCost = quote.RoomCost, DubCost = quote.DubCost, Fee = quote.Fee,
			DepositHeld = quote.Deposit, Status = DemoStatus.Booked, AgreedDay = DemoToday };
		demoJobs.Add(job);
		return job;
	}

	// ── Save ────────────────────────────────────────────────────────────────────────────────────────

	private DemoSaveData CaptureDemos() => demoJobs.Count == 0 && demoRefusals.Count == 0 ? null : new DemoSaveData {
		Ordinal = demoOrdinal,
		Jobs = demoJobs.Select(j => new DemoJobSaveData { Id = j.Id, ArtistId = j.ArtistId, ArtistName = j.ArtistName, RoomId = j.RoomId, RoomName = j.RoomName,
			Day = j.Day, StartHour = j.StartHour, Hours = j.Hours, RoomCost = j.RoomCost, DubCost = j.DubCost, Fee = j.Fee, DepositHeld = j.DepositHeld,
			Status = (int)j.Status, AgreedDay = j.AgreedDay, Outcome = j.Outcome }).ToList(),
		Refusals = demoRefusals.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new DemoRefusalSaveData { ArtistId = kv.Key, UntilDay = kv.Value }).ToList(),
	};

	private void RestoreDemos(DemoSaveData data) {
		demoJobs.Clear(); demoRefusals.Clear(); demoOrdinal = 0;
		if (data == null) return;
		demoOrdinal = data.Ordinal;
		foreach (var j in data.Jobs ?? new()) {
			if (string.IsNullOrEmpty(j?.Id) || string.IsNullOrEmpty(j.ArtistId)) continue;
			demoJobs.Add(new DemoJob { Id = j.Id, ArtistId = j.ArtistId, ArtistName = j.ArtistName, RoomId = j.RoomId, RoomName = j.RoomName, Day = j.Day,
				StartHour = j.StartHour, Hours = j.Hours, RoomCost = j.RoomCost, DubCost = j.DubCost, Fee = j.Fee, DepositHeld = j.DepositHeld,
				Status = (DemoStatus)j.Status, AgreedDay = j.AgreedDay, Outcome = j.Outcome });
		}
		foreach (var r in data.Refusals ?? new()) if (!string.IsNullOrEmpty(r?.ArtistId)) demoRefusals[r.ArtistId] = r.UntilDay;
	}
}

/// <summary>Early-game Phase 4: paid demo bookings, their held deposits and outcomes, and standing refusals. Absent in older saves.</summary>
public sealed class DemoSaveData {
	public int Ordinal { get; set; }
	public List<DemoJobSaveData> Jobs { get; set; } = new();
	public List<DemoRefusalSaveData> Refusals { get; set; } = new();
}

public sealed class DemoJobSaveData {
	public string Id { get; set; }
	public string ArtistId { get; set; }
	public string ArtistName { get; set; }
	public string RoomId { get; set; }
	public string RoomName { get; set; }
	public int Day { get; set; }
	public int StartHour { get; set; }
	public int Hours { get; set; }
	public float RoomCost { get; set; }
	public float DubCost { get; set; }
	public float Fee { get; set; }
	public float DepositHeld { get; set; }
	public int Status { get; set; }
	public int AgreedDay { get; set; }
	public string Outcome { get; set; }
}

public sealed class DemoRefusalSaveData {
	public string ArtistId { get; set; }
	public int UntilDay { get; set; }
}
