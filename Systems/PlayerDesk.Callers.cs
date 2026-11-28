using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A booker's open set, offered on the phone. Good until the end of the day he rang: after that he's given it to
/// the next name on his list.</summary>
public sealed class BookerOfferSaveData {
	public string Id { get; set; }
	public string RoomId { get; set; }
	public string BillId { get; set; }
	/// <summary>The night, the hour of the set, and the act that dropped out (null when the night was a set short).</summary>
	public int Night { get; set; }
	public int Hour { get; set; }
	public string DroppedArtistId { get; set; }
	/// <summary>The day he rang. The offer holds until that day ends.</summary>
	public int RangDay { get; set; }
}

/// <summary>A note left in the office door while he was out: who, what they wrote, and the act it's about.</summary>
public sealed class DoorNoteSaveData {
	public string Id { get; set; }
	public string From { get; set; }
	public string Text { get; set; }
	public int Day { get; set; }
	public string ArtistId { get; set; }
}

/// <summary>A call or visit already put on the line, so the same open set, tip or visit isn't queued twice. Pruned once
/// its day is well past.</summary>
public sealed class CallKeySaveData {
	public string Key { get; set; }
	public int Day { get; set; }
}

/// <summary>What the Label Man heard when he picked up (or opened the door): who, what they said, and what follows.</summary>
public sealed class CallerCard {
	public bool AtTheDoor;
	public string From, Said, After;
	/// <summary>A booker's open set: the offer to take or turn down while he's still on the line.</summary>
	public string OfferId;
	/// <summary>A manager at the door about a yes that's waiting on a time: the act, to pick a time there and then.</summary>
	public string ArtistId;
}

/// <summary>
/// The ringing phone, the booker's open set, a rival's tip and the note left in the door (OnboardingTutorialDirective.md §4c,
/// T5 step 3 follow-on). Built on the phone that rings once (PlayerDesk.Phone):
///
/// - **A ringing phone you can see.** A wait at the desk (WAIT 1h/3h, WAIT FOR EVENING) stops at the minute the phone rings
///   or somebody knocks. The phone on the desk rings and rattles until he picks it up, which takes a few minutes and shows
///   who it is and what they want. If he lets it ring (any other move of the clock, the day's end), that try is gone: a shop
///   tries again in the afternoon, anyone else is gone. Any other time spent at the desk still picks up as before: he's
///   there, and he answers, and the call is a ledger line.
/// - **A booker's open set.** A booker he's met rings when a set on the announced calendar comes open (an act billed who
///   can't play it now, or a night the room couldn't fill), two to five days out, if the label has an act who could work
///   it. He rings once, in the afternoon, a day or more before the night. Answered, the set is his for the rest of that day
///   (on the line, or by calling back from THE PHONE); after that it goes to the next name. Missed, it's gone, and he
///   never knows.
/// - **A rival's tip.** When another label's A&amp;R looks at an act he's heard, a booker he's met whose room has had them
///   rings the next day: "Somebody from Chess was in asking about them." Answered, the act's leads say so whatever his
///   Fixer. Missed, gone.
/// - **A note left in the door.** Rare visitors leave word. A manager whose yes is still waiting on a time comes by a day or two
///   before the first time he offered: in, he's at the door, and the time can be picked there; out, he leaves a note. A
///   manager stood up at a sit-down leaves his new times in the door (PlayerDesk.AppointmentBook). Notes stay in the door
///   until read.
///
/// Player-side only: every hour and day is a keyed hash, nothing draws on a random stream, and nothing the AI reads moves
/// unless the player takes an open set (which puts his act on that night's bill, as a pitched night does).
/// </summary>
public partial class PlayerDesk {
	/// <summary>Picking up and hearing them out.</summary>
	public const int PickUpMinutes = 5;
	/// <summary>Ringing a booker back about his open set.</summary>
	public const int CallBackBookerMinutes = 10;
	/// <summary>Bookers ring about an open set between these days ahead of the night, in the afternoon (they're in from two).</summary>
	private const int OpenSetFromDays = 2, OpenSetToDays = 5, BookerRingFrom = 14 * 60, BookerRingSpan = 3 * 60;
	/// <summary>A tip rings the day after the look, late morning to late afternoon.</summary>
	private const int TipRingFrom = 11 * 60, TipRingSpan = 6 * 60;
	/// <summary>A manager comes by a day or two before the first time he offered (the first dawn inside that), during office hours.</summary>
	private const int VisitDaysBefore = 2, VisitFrom = 10 * 60, VisitSpan = 6 * 60;
	/// <summary>Notes shown by the door. Any more are read from the oldest first.</summary>
	public const int DoorNotesShown = 4;

	/// <summary>The phone ringing (or a knock at the door) right now, with nobody picking up yet. Only a wait at the desk
	/// stops for one.</summary>
	private PendingRing ringingNow;
	private readonly List<BookerOfferSaveData> bookerOffers = new();
	private readonly List<DoorNoteSaveData> doorNotes = new();
	private readonly List<CallKeySaveData> callKeys = new();
	private bool pickingUp;

	public enum CallerAt { None, Phone, Door }
	/// <summary>Is the phone ringing, or somebody knocking, right now?</summary>
	public CallerAt Calling => ringingNow == null || Label == null ? CallerAt.None : ringingNow.Kind == RingKind.Visit ? CallerAt.Door : CallerAt.Phone;
	public IReadOnlyList<DoorNoteSaveData> DoorNotes => doorNotes;

	// ---- the ringing phone ----

	/// <summary>The first try due after now and within <paramref name="minutes"/>, today.</summary>
	private PendingRing NextRingWithin(int minutes) {
		int today = SceneDay, now = NowMinute;
		return pendingRings.Where(r => r.Day == today && r.Tries.Count > 0 && r.Tries[0] > now && r.Tries[0] <= now + minutes)
			.OrderBy(r => r.Tries[0]).FirstOrDefault();
	}

	/// <summary>A wait at the desk that stops when the phone rings or somebody knocks. Returns true when it stopped.</summary>
	private bool WaitUntilCaller(int minutes, out string message) {
		message = null;
		PendingRing ring = NextRingWithin(minutes);
		if (ring == null) return false;
		int until = ring.Tries[0] - NowMinute;
		pendingRings.Remove(ring);   // taken off the line first, so the clock moving to it doesn't answer it for him
		using (AtTheDesk()) TimeManager.Instance.SpendMinutes(until);
		ringingNow = ring;
		message = ring.Kind == RingKind.Visit ? "Somebody's knocking at the door." : "The phone's ringing.";
		return true;
	}

	/// <summary>He didn't pick up: the clock moved on, or the day ended. That try is gone.</summary>
	private void LetItRing() {
		if (ringingNow == null || pickingUp) return;
		PendingRing ring = ringingNow;
		ringingNow = null;
		if (ring.Tries.Count > 0) ring.Tries.RemoveAt(0);
		if (ring.Tries.Count > 0) { pendingRings.Add(ring); return; }
		MissRing(ring);
	}

	/// <summary>Pick up the ringing phone, or open the door. A few minutes at the desk. Returns what they said.</summary>
	public CallerCard PickUp(out string message) {
		message = null;
		if (ringingNow == null || Label == null) { message = "Nothing's ringing."; return null; }
		PendingRing ring = ringingNow;
		var card = new CallerCard { AtTheDoor = ring.Kind == RingKind.Visit };
		pickingUp = true;
		try {
			ringingNow = null;
			lastCallerCard = card;
			AnswerRing(ring);
			if (TimeManager.Instance?.CanAffordMinutes(PickUpMinutes) == true) DeskSpendMinutes(PickUpMinutes);
		} finally { pickingUp = false; lastCallerCard = null; }
		if (card.Said == null) {
			card.From ??= card.AtTheDoor ? "At the door" : "The phone";
			card.Said = card.AtTheDoor ? "Nobody there by the time you got to it." : "A click. Whoever it was hung up.";
		}
		message = card.AtTheDoor ? $"You opened the door to {card.From}." : $"You picked up: {card.From}.";
		Changed?.Invoke();
		return card;
	}

	/// <summary>While a pick-up is answering a ring, the answer writes what was said here.</summary>
	private CallerCard lastCallerCard;
	private void Said(string from, string said, string after = null) {
		if (lastCallerCard == null) return;
		lastCallerCard.From = from; lastCallerCard.Said = said; lastCallerCard.After = after;
	}

	// ---- answering and missing the new kinds ----

	/// <summary>The kinds this file owns. True when it handled the answer.</summary>
	private bool AnswerCaller(PendingRing ring) {
		switch (ring.Kind) {
			case RingKind.BookerSlot: AnswerBookerSlot(ring); return true;
			case RingKind.RivalTip: AnswerTip(ring); return true;
			case RingKind.Visit: AnswerVisit(ring); return true;
			case RingKind.Account: SayAccount(ring); return false;
			case RingKind.DjCallback: SayJock(ring); return false;
		}
		return false;
	}

	private bool MissCaller(PendingRing ring) {
		switch (ring.Kind) {
			case RingKind.BookerSlot: case RingKind.RivalTip: return true;   // he rang the next name; nobody ever says
			case RingKind.Visit: MissVisit(ring); return true;
		}
		return false;
	}

	private void SayAccount(PendingRing ring) {
		if (lastCallerCard == null) return;
		PlayerStop stop = GetStop(ring.StopId);
		string title = TitleForRecord(ring.RecordId), who = stop?.DisplayName ?? "A counter";
		string said = ring.Reason switch {
			InboundCallReason.SoldOut => $"\"We're out of '{title}' and they're still asking. Can you get us another {ring.Qty:N0}?\"",
			InboundCallReason.Requests => $"\"Kids keep coming in for '{title}'. We don't carry it. Can you bring some by?\"",
			InboundCallReason.StationAdded => $"\"They're playing '{title}' on the radio and people are asking for it here. We'd take some.\"",
			InboundCallReason.AdjacentCity => $"\"We hear '{title}' is moving in the next town over. We'd take some too.\"",
			InboundCallReason.OneStopTest => $"\"We'd like to try a carton of '{title}' and see if our stores can move it.\"",
			InboundCallReason.PreOrder => $"\"A jock played the acetate of '{title}'. Hold me {ring.Qty:N0} when the pressing comes in.\"",
			_ => $"\"It's about '{title}'.\""
		};
		Said($"{who}{(stop != null ? $", {CityName(stop.CityId)}" : "")}", said,
			ring.Consignment ? "They'd want it on consignment. It's on THE PHONE in the Rolodex, and on their row." : "COD's fine with them. It's on THE PHONE in the Rolodex, and on their row.");
	}

	private void SayJock(PendingRing ring) {
		if (lastCallerCard == null) return;
		RadioStation station = (ChartManager.Instance?.ReporterStationsInRegion(ring.RegionId) ?? Array.Empty<RadioStation>())
			.FirstOrDefault(s => s?.stationId == ring.StationId);
		Said(station != null ? $"A jock at {station.callsign}" : "A disc jockey",
			$"\"Got your '{TitleForRecord(ring.RecordId)}' in the mail. I like it. Thought I'd say so.\"",
			"He's in the Rolodex now. Ring him when you've got the next one.");
	}

	// ---- a booker's open set ----

	/// <summary>At dawn: a booker he's met with a set coming open in the next few days, for an act the label could put in.</summary>
	private void QueueBookerCalls() {
		if (Label == null || !LocalScenes.Rooms) return;
		int today = SceneDay;
		var acts = Roster.Where(a => a != null && a.lifecycleStatus == ArtistLifecycleStatus.Active).ToList();
		if (acts.Count == 0) return;
		foreach (SceneRoomProfile room in IntroducedSceneBookers.Where(r => r.IsPerformance).OrderBy(r => r.Id, StringComparer.Ordinal)) {
			if (pendingRings.Any(r => r.Kind == RingKind.BookerSlot && r.RoomId == room.Id)) continue;
			foreach (var open in LocalSceneRoomService.OpenSets(room.Id, today + OpenSetFromDays, today + OpenSetToDays).OrderBy(o => o.Day).ThenBy(o => o.Hour)) {
				string key = $"slot|{open.BillId}|{open.Hour}";
				if (callKeys.Any(k => k.Key == key)) continue;
				if (!acts.Any(a => LocalSceneRoomService.Eligible(a, room, open.Day) && !LocalSceneRoomService.PlayingThatDay(a, open.Day))) continue;
				callKeys.Add(new CallKeySaveData { Key = key, Day = open.Day });
				var ring = new PendingRing { Kind = RingKind.BookerSlot, RoomId = room.Id, BillId = open.BillId, Night = open.Day,
					SlotHour = open.Hour, DroppedArtistId = open.DroppedArtistId };
				int span = Math.Max(1, open.Day - today - 1);   // a day or more before the night
				QueueRingOn(ring, today + 1 + StableHash(key) % span, BookerRingFrom, BookerRingSpan);
				break;
			}
		}
	}

	private void AnswerBookerSlot(PendingRing ring) {
		SceneRoomProfile room = SceneRoomCatalog.Get(ring.RoomId);
		if (room == null) return;
		var offer = new BookerOfferSaveData { Id = $"offer|{ring.BillId}|{ring.SlotHour}", RoomId = ring.RoomId, BillId = ring.BillId,
			Night = ring.Night, Hour = ring.SlotHour, DroppedArtistId = ring.DroppedArtistId, RangDay = SceneDay };
		bookerOffers.RemoveAll(o => o.Id == offer.Id);
		bookerOffers.Add(offer);
		string night = NightText(offer.Night), set = $"the {ClockText(offer.Hour * 60)} set";
		string dropped = ArtistManager.Instance?.GetArtist(offer.DroppedArtistId ?? "")?.stageName;
		string said = dropped != null
			? $"\"{dropped} can't make {night}. I've got {set} open at {room.Name}. One of yours want it? I need to know today.\""
			: $"\"I'm a set short at {room.Name} on {night}: {set}. One of yours want it? I need to know today.\"";
		Note($"The phone: {room.ContactTitle}. {said.Trim('"')} Answer him from THE PHONE by tonight, or he rings the next label.");
		FlagSkipStop($"{room.ContactName} rang: a set's open at {room.Name}", EventType.IncomingCall, officeOnly: true);
		Said(room.ContactTitle, said, "Put one of your acts in, or tell him no. The offer stands until tonight; after that he rings the next label.");
		if (lastCallerCard != null) lastCallerCard.OfferId = offer.Id;
	}

	private static string NightText(int day) {
		var d = LocalSceneRoomService.Date(day);
		return $"{d.DayName} the {d.day}{Ordinal(d.day)}";
	}

	/// <summary>Open sets a booker rang about today, still good.</summary>
	public IReadOnlyList<BookerOfferSaveData> BookerOffers => bookerOffers.Where(o => o.RangDay == SceneDay && o.Night > SceneDay).ToList();

	public BookerOfferSaveData BookerOffer(string offerId) => BookerOffers.FirstOrDefault(o => o.Id == offerId);

	/// <summary>The label's acts that could work the open set: playing that scene and that kind of room, free that night.</summary>
	public List<SimulatedArtist> ActsForOffer(string offerId) {
		var offer = BookerOffer(offerId);
		var room = SceneRoomCatalog.Get(offer?.RoomId ?? "");
		if (offer == null || room == null) return new();
		return Roster.Where(a => a != null && a.lifecycleStatus == ArtistLifecycleStatus.Active && LocalSceneRoomService.Eligible(a, room, offer.Night)
			&& !LocalSceneRoomService.PlayingThatDay(a, offer.Night)).OrderBy(a => a.stageName, StringComparer.Ordinal).ToList();
	}

	/// <summary>One line for the offer: "Ray Coles, booker at River Lantern: the 9 PM set, Thursday night, the 14th".</summary>
	public string OfferText(BookerOfferSaveData offer) {
		var room = SceneRoomCatalog.Get(offer.RoomId);
		return $"{room?.ContactTitle ?? "A booker"}: the {ClockText(offer.Hour * 60)} set on {NightText(offer.Night)}";
	}

	/// <summary>Put an act in the open set. On the line it's part of the call; otherwise it's a call back from the office.</summary>
	public bool TakeBookerOffer(string offerId, string artistId, bool onTheLine, out string message) {
		var offer = BookerOffer(offerId);
		var room = SceneRoomCatalog.Get(offer?.RoomId ?? "");
		var act = ArtistManager.Instance?.GetArtist(artistId);
		if (offer == null || room == null) { message = "He's given that set to somebody else."; return false; }
		if (act == null || act.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		if (!onTheLine) {
			if (!AtHome) { message = $"The phone's at the office. Ring {room.ContactName} back from there."; return false; }
			if (TimeManager.Instance?.CanAffordMinutes(CallBackBookerMinutes) != true) { message = "It's too late to ring anyone tonight."; return false; }
		}
		var open = new LocalSceneRoomService.OpenSet(offer.BillId, offer.RoomId, offer.Night, offer.Hour, offer.DroppedArtistId);
		if (!LocalSceneRoomService.FillOpenSet(open, act, Label.labelId, out string reason)) { message = reason; return false; }
		if (!onTheLine) DeskSpendMinutes(CallBackBookerMinutes);
		bookerOffers.RemoveAll(o => o.Id == offerId);
		message = $"{room.ContactName} puts {act.stageName} in the {ClockText(offer.Hour * 60)} set at {room.Name} on {NightText(offer.Night)}. " +
			$"The door money is the band's, less your {LocalSceneRoomService.BookingCommissionRate:P0} if they play.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	public void TurnDownBookerOffer(string offerId) {
		var offer = BookerOffer(offerId);
		if (offer == null) return;
		bookerOffers.RemoveAll(o => o.Id == offerId);
		Note($"You told {SceneRoomCatalog.Get(offer.RoomId)?.ContactName ?? "the booker"} no thanks for {NightText(offer.Night)}.");
		Changed?.Invoke();
	}

	// ---- a rival's tip ----

	/// <summary>Another label's A&amp;R looked at an act he's heard. A booker he's met, whose room has had them, rings tomorrow.</summary>
	private void QueueRivalTip(SimulatedArtist act, string rivalLabelId, int lookDay) {
		if (Label == null || act == null || !HasScoutingEvidence(act.artistId)) return;
		if (pendingRings.Any(r => r.Kind == RingKind.RivalTip && r.ArtistId == act.artistId)) return;
		string place = LocalScenePersistenceService.SceneIdFor(act.geography?.basePlaceId);
		var room = IntroducedSceneBookers.Where(r => r.IsPerformance && r.PlaceId == place && LocalSceneRoomService.ShowsAt(r.Id, act.artistId) > 0)
			.OrderBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault();
		if (room == null) return;
		string key = $"tip|{act.artistId}|{rivalLabelId}";
		if (callKeys.Any(k => k.Key == key && lookDay - k.Day < RivalLookDays)) return;
		callKeys.RemoveAll(k => k.Key == key);
		callKeys.Add(new CallKeySaveData { Key = key, Day = lookDay });
		QueueRingOn(new PendingRing { Kind = RingKind.RivalTip, RoomId = room.Id, ArtistId = act.artistId, LabelId = rivalLabelId, Day = lookDay },
			Math.Max(SceneDay, lookDay) + 1, TipRingFrom, TipRingSpan);
	}

	private void AnswerTip(PendingRing ring) {
		var room = SceneRoomCatalog.Get(ring.RoomId);
		var act = ArtistManager.Instance?.GetArtist(ring.ArtistId);
		if (room == null || act == null) return;
		string rival = ChartManager.Instance?.GetLabelName(ring.LabelId) ?? "another label";
		bool free = string.IsNullOrEmpty(act.labelId);
		string said = free
			? $"\"Somebody from {rival} was in asking about {act.stageName}. Thought you'd want to know.\""
			: $"\"Somebody from {rival} was in asking about {act.stageName}. Looks like you heard already.\"";
		if (free) foreach (var look in sceneInformation.RivalLooks?.Where(l => l.ArtistId == act.artistId && l.LabelId == ring.LabelId) ?? Enumerable.Empty<RivalLook>())
			look.ToldBy = room.ContactName;
		Note($"The phone: {room.ContactTitle}. {said.Trim('"')}");
		Said(room.ContactTitle, said, free ? $"If you want {act.stageName}, you'll have to move before {rival} does." : null);
	}

	// ---- a manager at the door ----

	/// <summary>At dawn: a manager whose yes is still waiting on a time comes by, a day or two before the first time he offered.
	/// Once per set of times.</summary>
	private void QueueVisits() {
		if (Label == null || scoutingKnowledge.Approaches == null) return;
		int today = SceneDay;
		foreach (var a in scoutingKnowledge.Approaches.Where(a => a.Status == ScoutingApproachStatus.Agreed)) {
			var step = a.Steps.FirstOrDefault(s => s.Kind == ScoutingApproachStepKind.Manager && NeedsATime(s));
			int ahead = step == null ? 0 : step.Offered[0].Day - today;
			if (ahead < 1 || ahead > VisitDaysBefore) continue;
			if (LocalSceneRoomService.Date(today).DayOfWeek == DayOfWeek.Sunday) continue;
			string key = $"visit|{a.ArtistId}|{step.Offered[0].Day}";
			if (callKeys.Any(k => k.Key == key)) continue;
			callKeys.Add(new CallKeySaveData { Key = key, Day = step.Offered[0].Day });
			QueueRingOn(new PendingRing { Kind = RingKind.Visit, ArtistId = a.ArtistId }, today, Math.Max(VisitFrom, NowMinute + 30), VisitSpan);
		}
	}

	private ScoutingApproachStep WaitingVisit(string artistId, out ScoutingDirectApproach approach) {
		approach = scoutingKnowledge.Approaches?.LastOrDefault(a => a.ArtistId == artistId && a.Status == ScoutingApproachStatus.Agreed);
		return approach?.Steps.FirstOrDefault(s => s.Kind == ScoutingApproachStepKind.Manager && NeedsATime(s));
	}

	private void AnswerVisit(PendingRing ring) {
		var step = WaitingVisit(ring.ArtistId, out var a);
		if (step == null) return;
		string he = step.Female ? "she" : "he";
		string said = $"\"I was passing. I still haven't heard when you want to sit down about {a.ArtistName}. {OfferedText(step)}. Which is it?\"";
		Note($"{step.Who} came by the office about {a.ArtistName}: {OfferedText(step)}. Pick one from their card, or the book.");
		Said(step.Who, said, $"Pick one now and {he}'ll write it down too.");
		if (lastCallerCard != null) lastCallerCard.ArtistId = a.ArtistId;
	}

	private void MissVisit(PendingRing ring) {
		var step = WaitingVisit(ring.ArtistId, out var a);
		if (step == null) return;
		string text = $"Came by about {a.ArtistName}. Nobody in. I can see you {OfferedText(step)}. Let me know which.";
		LeaveDoorNote(step.Who, text, a.ArtistId);
		Note($"{step.Who} came by the office about {a.ArtistName} while you were out, and left a note in the door.");
	}

	// ---- notes left in the door ----

	/// <summary>A note left in the office door. It stays there until he reads it.</summary>
	private void LeaveDoorNote(string from, string text, string artistId = null) {
		int day = SceneDay;
		doorNotes.Add(new DoorNoteSaveData { Id = $"note|{day}|{NowMinute}|{doorNotes.Count}|{from}", From = from, Text = text, Day = day, ArtistId = artistId });
		FlagSkipStop($"A note in the door from {from}", EventType.IncomingCall, officeOnly: true);
	}

	/// <summary>Take a note out of the door and read it. It's gone from the door.</summary>
	public DoorNoteSaveData ReadDoorNote(string id) {
		var note = doorNotes.FirstOrDefault(n => n.Id == id);
		if (note == null) return null;
		doorNotes.Remove(note);
		Changed?.Invoke();
		return note;
	}

	// ---- the day ----

	private void CallersAtDawn() {
		bookerOffers.RemoveAll(o => o.RangDay < SceneDay || o.Night <= SceneDay);
		callKeys.RemoveAll(k => SceneDay - k.Day > 60);
		QueueBookerCalls();
		QueueVisits();
	}

	private void RestoreCallers(PlayerSaveData data) {
		bookerOffers.Clear();
		bookerOffers.AddRange(data.BookerOffers ?? new List<BookerOfferSaveData>());
		doorNotes.Clear();
		doorNotes.AddRange(data.DoorNotes ?? new List<DoorNoteSaveData>());
		callKeys.Clear();
		callKeys.AddRange(data.CallKeys ?? new List<CallKeySaveData>());
	}

	// ---- fixture access, never rendered ----
	internal PendingRing RingingForChecks => ringingNow;
	internal IReadOnlyList<BookerOfferSaveData> AllBookerOffersForChecks => bookerOffers;
	internal void QueueBookerCallsForChecks() => QueueBookerCalls();
	internal void QueueVisitsForChecks() => QueueVisits();
	internal void QueueRivalTipForChecks(SimulatedArtist act, string labelId) => QueueRivalTip(act, labelId, SceneDay);
	internal void LeaveDoorNoteForChecks(string from, string text) { LeaveDoorNote(from, text); Changed?.Invoke(); }
	/// <summary>A manager coming by today at a given minute, as the dawn would put him on the way.</summary>
	internal void QueueVisitForChecks(string artistId, int minute) =>
		pendingRings.Add(new PendingRing { Kind = RingKind.Visit, ArtistId = artistId, Day = SceneDay, Tries = new List<int> { minute } });
}
