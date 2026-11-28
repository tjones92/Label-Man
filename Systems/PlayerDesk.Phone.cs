using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A call on its way to the office (or a visitor on his way to the door): who, about what, and the hour (or two)
/// it rings.</summary>
public sealed class PendingRing {
	/// <summary>An account phoning about stock, a jock calling back about a mailed record, a booker with a set to fill, a
	/// booker's tip about a rival, or a manager coming by the office.</summary>
	public PlayerDesk.RingKind Kind;
	public string StopId, RecordId, StationId, RegionId;
	public int Qty;
	public PlayerDesk.InboundCallReason Reason;
	public int Week, ExpiresWeek;
	public bool Consignment;
	/// <summary>Booker calls: the room. A booker's open set: the bill, the night, the hour and the act that dropped out
	/// (null when the night was a set short). A tip: the act and the rival label. A visit: the act.</summary>
	public string RoomId, ArtistId, LabelId, BillId, DroppedArtistId;
	public int Night, SlotHour;
	/// <summary>The scene day it rings, and the minutes past midnight of each try, soonest first.</summary>
	public int Day;
	public List<int> Tries = new();
}

public sealed class PendingRingSaveData {
	public int Kind { get; set; }
	public string StopId { get; set; }
	public string RecordId { get; set; }
	public string StationId { get; set; }
	public string RegionId { get; set; }
	public int Qty { get; set; }
	public int Reason { get; set; }
	public int Week { get; set; }
	public int ExpiresWeek { get; set; }
	public bool Consignment { get; set; }
	public int Day { get; set; }
	public List<int> Tries { get; set; } = new();
	public string RoomId { get; set; }
	public string ArtistId { get; set; }
	public string LabelId { get; set; }
	public string BillId { get; set; }
	public string DroppedArtistId { get; set; }
	public int Night { get; set; }
	public int SlotHour { get; set; }
	/// <summary>It was ringing (or knocking) when the game was saved, with nobody picking up yet.</summary>
	public bool Ringing { get; set; }
}

/// <summary>A known account that rang while he was out. It comes up the next time he's across their counter.</summary>
public sealed class MissedRingSaveData {
	public string StopId { get; set; }
	public string RecordId { get; set; }
	public int Day { get; set; }
}

/// <summary>The phone rings once (OnboardingTutorialDirective.md §4c, T5 step 3). A call arrives at an hour of the day,
/// keyed on the caller, the record and the week, so the same week replays the same. If he's in the office at that hour,
/// he answers it, and it becomes the open demand the stop rows and THE PHONE show. If he isn't, it's gone: a shop tries
/// twice (morning and afternoon) and then buys from whoever answered; a stranger, a station's counter or a jock rings once.
/// Most misses never surface. A known account mentions it the next time he's at their counter.
///
/// "In the office" means at home, in time he spent at the desk: waiting there, on the phone, packing a mailing, typing.
/// Any other clock move is time out (a set, an errand, a counter, a meeting, the road). Time nobody spent (the rest of a
/// day he ended, a day skipped) passes wherever he is, so a day skipped at home is a day by the phone.
///
/// There is no answering service (author, 2026-11-26). The secretary (about 1961) is the thing that answers for him.
/// Player-side only: the ring times are keyed hashes, no RNG draw, and nothing the AI reads moves.</summary>
public partial class PlayerDesk {
	public enum RingKind { Account, DjCallback, BookerSlot, RivalTip, Visit }

	/// <summary>Shops ring between nine and noon, and try again between one and five.</summary>
	private const int RingMorningFrom = 9 * 60, RingMorningSpan = 3 * 60, RingAfternoonFrom = 13 * 60, RingAfternoonSpan = 4 * 60;
	/// <summary>A miss is mentioned at the counter only while it's this fresh.</summary>
	private const int MissedRingMemoryDays = 42;

	private readonly List<PendingRing> pendingRings = new();
	private readonly List<MissedRingSaveData> missedRings = new();
	private int deskTimeDepth;
	/// <summary>This session's tally, for the checks and the player bots. Saved, so a reload keeps the count.</summary>
	public int RingsAnswered { get; private set; }
	public int RingsMissed { get; private set; }

	/// <summary>Calls on their way, for the checks. The desk never shows these: he doesn't know a call is coming.</summary>
	internal IReadOnlyList<PendingRing> PendingRingsForChecks => pendingRings;
	internal IReadOnlyList<MissedRingSaveData> MissedRingsForChecks => missedRings;

	/// <summary>Checks only: put a shop's sold-out call on the line, keyed as the weekly roll would.</summary>
	internal void QueueRingForChecks(PlayerStop stop, string recordId, InboundCallReason reason = InboundCallReason.SoldOut) =>
		QueueAccountRing(stop, recordId, 12, reason, ChartManager.Instance?.GetCurrentChartWeek() ?? 0, consignment: false);
	/// <summary>Checks only: a jock's call-back about a mailed record, on the line as a mailing would put it.</summary>
	internal void QueueDjRingForChecks(string stationId, string regionId, string recordId) =>
		QueueRing(new PendingRing { Kind = RingKind.DjCallback, StationId = stationId, RegionId = regionId, RecordId = recordId,
			Week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0 });
	/// <summary>Checks only: across his counter, as any stop verb would.</summary>
	internal void TouchStopForChecks(PlayerStop stop) => TouchStop(stop, 0f);
	/// <summary>Checks only: settle the tries due now, as the hour hand would after time at the desk or out.</summary>
	internal void ResolveRingsForChecks(bool inOffice) => ResolveRings(inOffice);

	/// <summary>Is he at the office, on the clock, right now? True only inside time spent at the desk.</summary>
	public bool AtTheDeskNow => AtHome && deskTimeDepth > 0;

	private sealed class DeskTimeScope : IDisposable {
		private PlayerDesk desk;
		public DeskTimeScope(PlayerDesk d) { desk = d; d.deskTimeDepth++; }
		public void Dispose() { if (desk != null) { desk.deskTimeDepth--; desk = null; } }
	}
	/// <summary>Time spent inside this is time at the desk: a call that rings during it is answered.</summary>
	private IDisposable AtTheDesk() => new DeskTimeScope(this);
	private bool DeskSpendMinutes(int minutes) { using (AtTheDesk()) return SpendMinutes(minutes); }
	private void DeskSpend(int hours) { using (AtTheDesk()) Spend(hours); }

	/// <summary>Instead of logging a call the moment the week turns, put it on the line for an hour in the coming days.
	/// Starts tomorrow (word has to travel), never on a Sunday. A shop tries twice; anyone else rings once.</summary>
	private void QueueRing(PendingRing ring) {
		int h = RingHash(ring);
		int day = SceneDay + 1;
		for (int skip = h % 6; ; day++) {
			if (LocalSceneRoomService.Date(day).DayOfWeek == DayOfWeek.Sunday) continue;
			if (skip-- == 0) break;
		}
		ring.Day = day;
		ring.Tries = new List<int> { RingMorningFrom + h / 7 % RingMorningSpan };
		if (RingsTwice(ring)) ring.Tries.Add(RingAfternoonFrom + h / 1301 % RingAfternoonSpan);
		pendingRings.Add(ring);
	}

	/// <summary>A call (or a visit) that comes on a given day, once, at a keyed minute inside [from, from + span).</summary>
	private void QueueRingOn(PendingRing ring, int day, int from, int span) {
		ring.Day = day;
		ring.Tries = new List<int> { from + RingHash(ring) / 7 % Math.Max(1, span) };
		pendingRings.Add(ring);
	}

	private int RingHash(PendingRing ring) =>
		StableHash($"{Label?.labelId}|{ring.Kind}|{ring.StopId ?? ring.StationId ?? ring.RoomId}|{ring.RecordId ?? ring.ArtistId}|{ring.Week}|{(int)ring.Reason}|{ring.BillId}|{ring.SlotHour}|Ring");

	/// <summary>The table in §4c: a shop sold out or getting requests tries twice, morning and afternoon; a station adding
	/// the record and a jock calling back ring once.</summary>
	private static bool RingsTwice(PendingRing ring) => ring.Kind == RingKind.Account && ring.Reason != InboundCallReason.StationAdded;

	private void QueueAccountRing(PlayerStop stop, string recordId, int qty, InboundCallReason reason, int week, bool consignment, int expiresWeek = 0) {
		if (pendingRings.Any(r => r.Kind == RingKind.Account && r.StopId == stop.StopId && r.RecordId == recordId)) return;
		QueueRing(new PendingRing { Kind = RingKind.Account, StopId = stop.StopId, RecordId = recordId, Qty = qty, Reason = reason,
			Week = week, ExpiresWeek = expiresWeek, Consignment = consignment });
	}

	/// <summary>Already ringing, or already open: either way the stop doesn't need another call about this title.</summary>
	private bool CallOnTheWay(string stopId, string recordId) =>
		HasOpenCall(stopId, recordId) || pendingRings.Any(r => r.Kind == RingKind.Account && r.StopId == stopId && r.RecordId == recordId);

	/// <summary>Every try whose hour has come: answered if he's at the desk in town, otherwise passed to the next try or
	/// lost. Runs on every clock move (with whether that time was at the desk) and at dawn for what's left of yesterday
	/// (time nobody spent, which passed wherever he was).</summary>
	private void ResolveRings(bool inOffice) {
		if (Label == null || pendingRings.Count == 0) return;
		int today = SceneDay, now = NowMinute;
		foreach (PendingRing ring in pendingRings.OrderBy(r => r.Day).ThenBy(r => r.Tries.FirstOrDefault()).ToList()) {
			while (ring.Tries.Count > 0 && (ring.Day < today || ring.Day == today && ring.Tries[0] <= now)) {
				if (inOffice) { pendingRings.Remove(ring); AnswerRing(ring); break; }
				ring.Tries.RemoveAt(0);
				if (ring.Tries.Count == 0) { pendingRings.Remove(ring); MissRing(ring); }
			}
		}
	}

	private void AnswerRing(PendingRing ring) {
		if (ring.Kind != RingKind.Visit) RingsAnswered++;
		if (AnswerCaller(ring)) return;
		if (ring.Kind == RingKind.DjCallback) {
			RadioStation station = (ChartManager.Instance?.ReporterStationsInRegion(ring.RegionId) ?? Array.Empty<RadioStation>())
				.FirstOrDefault(s => s?.stationId == ring.StationId);
			if (station == null) return;
			string title = TitleForRecord(ring.RecordId);
			if (rolodex.Any(e => e.stationId == station.stationId)) return;
			if (EnsureStationEntry(station, discover: true, $"He phoned about \"{title}\" -- didn't expect that.") == null) return;
			Note($"The phone: a jock at {station.callsign} got the \"{title}\" mailing and rang to say so. He's in the Rolodex now.");
			FlagSkipStop($"A DJ called back about \"{title}\"", EventType.IncomingCall, officeOnly: true);
			return;
		}
		PlayerStop stop = GetStop(ring.StopId);
		if (stop == null || HasOpenCall(stop.StopId, ring.RecordId)) return;
		AddCall(stop, ring.RecordId, ring.Qty, ring.Reason, ring.Week, ring.Consignment, ring.ExpiresWeek);
		// "Inbound calls in two cities the same week" (directive §7) counts the calls he picked up, as they come in.
		MaybeUnlockRunnerFromCalls(ring.Week);
	}

	private void MissRing(PendingRing ring) {
		if (ring.Kind != RingKind.Visit) RingsMissed++;
		if (MissCaller(ring)) return;
		if (ring.Kind != RingKind.Account) return; // the spin is gone, and he never knows
		PlayerStop stop = GetStop(ring.StopId);
		// A stranger who got no answer just rings the next label. Only somebody he's dealt with remembers it.
		if (stop == null || stop.LastVisitWeek <= 0) return;
		missedRings.RemoveAll(m => m.StopId == ring.StopId && m.RecordId == ring.RecordId);
		missedRings.Add(new MissedRingSaveData { StopId = ring.StopId, RecordId = ring.RecordId, Day = ring.Day });
	}

	/// <summary>At the counter: "I tried you Tuesday." The one way he learns that the phone rang and nobody was there.</summary>
	private void MentionMissedRing(PlayerStop stop) {
		if (stop == null || missedRings.Count == 0) return;
		var mine = missedRings.Where(m => m.StopId == stop.StopId).ToList();
		if (mine.Count == 0) return;
		missedRings.RemoveAll(m => m.StopId == stop.StopId);
		MissedRingSaveData last = mine.OrderBy(m => m.Day).Last();
		if (SceneDay - last.Day > MissedRingMemoryDays) return;
		int ago = SceneDay - last.Day;
		string when = ago <= 0 ? "this morning" : ago == 1 ? "yesterday" : ago < 7 ? LocalSceneRoomService.Date(last.Day).DayName : $"back on the {LocalSceneRoomService.Date(last.Day).day}{Ordinal(LocalSceneRoomService.Date(last.Day).day)}";
		Note($"{stop.DisplayName} in {CityName(stop.CityId)}: \"I tried you {when} about '{TitleForRecord(last.RecordId)}'. Nobody picked up, so I filled the hole with something else.\"");
	}

	private static string Ordinal(int n) => (n % 100) is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

	private void PruneMissedRings() => missedRings.RemoveAll(m => SceneDay - m.Day > MissedRingMemoryDays);

	/// <summary>What he set in motion and is waiting to hear about (§4c: the book lists it, so going out is a known risk
	/// even though the call isn't). Mailings out, accounts running low on a title he left them, acts thinking it over.</summary>
	public List<string> WaitingOn() {
		var list = new List<string>();
		if (Label == null) return list;
		foreach (PendingMailing m in pendingMailings.OrderBy(m => m.MailedWeek))
			list.Add($"The \"{TitleForRecord(m.RecordId)}\" mailing to {ChartManager.Instance?.GetRegionById(m.RegionId)?.regionName ?? m.RegionId}: " +
				$"{m.StationIds.Count} jock{(m.StationIds.Count == 1 ? "" : "s")}. If one of them likes it, he'll ring. Once.");
		// A jock who liked the acetate said he'd mention it to the shops. Which counters, and when, he doesn't know.
		foreach (string recordId in pendingRings.Where(r => r.Reason == InboundCallReason.PreOrder).Select(r => r.RecordId).Distinct(StringComparer.Ordinal))
			list.Add($"Counters that heard about \"{TitleForRecord(recordId)}\" on the radio: they'll ring to have copies held.");
		var released = new HashSet<string>(ReleasedRecords.Where(r => r?.baseRecord != null).Select(r => r.baseRecord.recordId), StringComparer.Ordinal);
		foreach (PlayerStop stop in EnsureStops().Values.Where(s => s.LastVisitWeek > 0).OrderBy(s => s.DisplayName, StringComparer.Ordinal))
			foreach (var kv in stop.OnHand.Where(kv => released.Contains(kv.Key) && kv.Value.Placed > 0 && kv.Value.Remaining <= Math.Max(SoldOutOnHandThreshold * 2, kv.Value.Placed / 3)))
				if (!HasOpenCall(stop.StopId, kv.Key))
					list.Add($"{stop.DisplayName} ({CityName(stop.CityId)}) is getting low on \"{TitleForRecord(kv.Key)}\". When it's gone, they'll ring for more.");
		foreach (var a in scoutingKnowledge.Approaches?.Where(a => a.Status == ScoutingApproachStatus.Considering) ?? Enumerable.Empty<ScoutingDirectApproach>())
			list.Add($"{a.ArtistName} are thinking it over. They'll answer by {LocalSceneRoomService.Date(a.UntilDay).ToHeadlineString()}.");
		return list;
	}

	private List<PendingRingSaveData> SavePendingRings() => pendingRings.Select(r => SaveRing(r, false))
		.Concat(ringingNow == null ? Enumerable.Empty<PendingRingSaveData>() : new[] { SaveRing(ringingNow, true) }).ToList();

	private static PendingRingSaveData SaveRing(PendingRing r, bool ringing) => new() {
		Kind = (int)r.Kind, StopId = r.StopId, RecordId = r.RecordId, StationId = r.StationId, RegionId = r.RegionId, Qty = r.Qty,
		Reason = (int)r.Reason, Week = r.Week, ExpiresWeek = r.ExpiresWeek, Consignment = r.Consignment, Day = r.Day, Tries = r.Tries.ToList(),
		RoomId = r.RoomId, ArtistId = r.ArtistId, LabelId = r.LabelId, BillId = r.BillId, DroppedArtistId = r.DroppedArtistId,
		Night = r.Night, SlotHour = r.SlotHour, Ringing = ringing
	};

	private void RestorePendingRings(PlayerSaveData data) {
		pendingRings.Clear();
		ringingNow = null;
		foreach (PendingRingSaveData s in data.PendingRings ?? new List<PendingRingSaveData>()) {
			var ring = new PendingRing { Kind = (RingKind)s.Kind, StopId = s.StopId, RecordId = s.RecordId, StationId = s.StationId,
				RegionId = s.RegionId, Qty = s.Qty, Reason = (InboundCallReason)s.Reason, Week = s.Week, ExpiresWeek = s.ExpiresWeek,
				Consignment = s.Consignment, Day = s.Day, Tries = s.Tries?.ToList() ?? new List<int>(),
				RoomId = s.RoomId, ArtistId = s.ArtistId, LabelId = s.LabelId, BillId = s.BillId, DroppedArtistId = s.DroppedArtistId,
				Night = s.Night, SlotHour = s.SlotHour };
			if (s.Ringing && ringingNow == null) ringingNow = ring; else pendingRings.Add(ring);
		}
		RestoreCallers(data);
		missedRings.Clear();
		missedRings.AddRange(data.MissedRings ?? new List<MissedRingSaveData>());
		RingsAnswered = data.RingsAnswered;
		RingsMissed = data.RingsMissed;
	}
}
