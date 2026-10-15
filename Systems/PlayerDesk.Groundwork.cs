using System;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 1B (§6): release preparation that creates later opportunities rather than paying for
/// an hour spent. This part is §6.2, dealer advance orders taken off an acetate. An accepted order is a reservation
/// in the one inbound-call ledger the DJ pre-orders already use -- COD when the copies are delivered, never cash now,
/// never sell-through, buzz or plant credit by itself.
/// </summary>
public partial class PlayerDesk {
	/// <summary>
	/// Play the acetate across a shop or operator's counter and ask him to hold copies for the pressing. Same daily
	/// approach gate and visit hours as a counter pitch, and the same yes/no -- base chance, relationship, what he
	/// hears in the record (<see cref="RecordAppeal"/> reads the unreleased master), all behind the access split.
	/// The acetate stays in the player's hand. A yes writes a pre-order sized off what this dealer would take of this
	/// record, capped at the existing pre-order bands; a no is a pass that stands until real evidence (a call in, see
	/// GenerateInboundCalls) reopens it. Returns false only when the approach could not be made.
	/// </summary>
	public bool PitchAcetateAtStop(string stopId, string recordId, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null) { message = "No such account."; return false; }
		if (stop.Kind is not (StopKind.Shop or StopKind.Op)) { message = $"{stop.DisplayName} doesn't write counter orders."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town to play it for him."; return false; }
		Master master = masters.FirstOrDefault(m => m.Record?.recordId == recordId);
		if (master == null || master.Released) { message = "Advance orders are for a record that isn't out yet."; return false; }
		if (AcetatesFor(recordId) <= 0) { message = $"Cut an acetate of \"{master.SongTitle}\" first -- he has to hear it."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{master.SongTitle}\" isn't yours to sell right now -- the master's out."; return false; }
		if (!CheckDailyApproach(stop, out message)) return false;
		if (stop.PassedRecordIds.Contains(recordId)) {
			message = $"{stop.DisplayName} already passed on \"{master.SongTitle}\" -- nothing's changed since. Let it catch on somewhere first.";
			return false;
		}
		if (HasOpenCall(stopId, recordId)) {
			message = $"{stop.DisplayName} is already holding copies of \"{master.SongTitle}\" for the pressing.";
			return false;
		}
		int hours = StopVisitHours(stop.Kind);
		if (!Require(hours, out message)) return false;
		Spend(hours);
		stop.LastApproachDate = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		CollectAtCity(stop.CityId);

		float acceptChance = Mathf.Clamp(
			(PitchBaseChance + stop.Relationship * PitchRelationshipWeight + PitchEarPull(stop, recordId)) * RetailAccessFactor(stop, recordId),
			0.03f, 0.97f);
		if (GD.Randf() > acceptChance) {
			TouchStop(stop, 0.02f);   // a listen that ended in a no still counts as an introduction
			stop.PassedRecordIds.Add(recordId);
			Note($"{stop.DisplayName} in {CityName(stop.CityId)} heard the acetate of \"{master.SongTitle}\" and passed -- \"bring it back when somebody's playing it.\"");
			message = $"{stop.DisplayName} heard it and passed ({hours}h).";
			Changed?.Invoke();
			return true;
		}

		int band = stop.Kind == StopKind.Op ? PreOrderOpMax : PreOrderShopMax;
		int qty = Mathf.Clamp(SuggestedPlacement(stop, recordId), 1, band);
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		AddCall(stop, recordId, qty, InboundCallReason.PreOrder, week, consignment: false, expiresWeek: PreOrderExpiryWeek(recordId, week));
		TouchStop(stop, 0.03f);
		workedCities.Add(stop.CityId);
		Note($"{stop.DisplayName} in {CityName(stop.CityId)} heard the acetate of \"{master.SongTitle}\" and asked to be held {qty:N0} -- COD when the copies come.");
		message = $"{stop.DisplayName} will take {qty:N0} of \"{master.SongTitle}\" COD when the pressing lands ({hours}h). Nothing is paid until you deliver.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>When a pre-order lapses: the ordinary call window, but never before two weeks past the vinyl's due date
	/// (five weeks out when nothing is at the plant yet). Shared by the DJ and the counter routes.</summary>
	private int PreOrderExpiryWeek(string recordId, int week) {
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		PressOrder order = pressOrders.FirstOrDefault(o => o.RecordId == recordId);
		int weeksToVinyl = order == null ? 5 : Mathf.CeilToInt(Mathf.Max(0, TimeManager.Instance?.DaysBetween(today, order.Arrives) ?? 0) / 7f);
		return Mathf.Max(week + CallExpiryWeeks, week + weeksToVinyl + 2);
	}

	/// <summary>§6.2: a plant taking its quoted time must not erase the orders it was taken against. When a run is ordered
	/// (or a later run moves the date), every open pre-order on the title is held until two weeks past that arrival.
	/// An order never shortens; one that already lapsed stays lapsed.</summary>
	private void HoldPreOrdersForArrival(string recordId) {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		int until = PreOrderExpiryWeek(recordId, week);
		foreach (InboundCall call in inboundCalls.Where(c => c.RecordId == recordId && c.Reason == InboundCallReason.PreOrder))
			call.ExpiresWeek = Math.Max(call.ExpiresWeek, until);
	}

	// ── §6.3 Pitch the act to a real booker ─────────────────────────────────────────────────────────────

	public const int BookerPitchHours = 1;
	/// <summary>The booker is in from mid-afternoon until his room closes.</summary>
	public const int BookerFromHour = 14;
	/// <summary>A no stands for a quarter (a booking term) unless the act has played the room since.</summary>
	public const int BookerRefusalDays = 91;

	/// <summary>The booker's yes, before the roll: how the act plays, how well it is known, and the room's own history
	/// with it (saturating at the rotation's four shows). Authored weights, uncalibrated.</summary>
	public static float BookerAcceptChance(SimulatedArtist act, int showsInRoom) => Mathf.Clamp(
		0.10f + 0.45f * Mathf.Clamp(act.livePerformance, 0f, 1f) + 0.25f * Mathf.Clamp(act.reputation, 0f, 1f)
		+ 0.20f * Mathf.Min(1f, showsInRoom / (float)LocalSceneRoomService.StandingSaturation), 0.05f, 0.90f);

	/// <summary>
	/// §6.3: ask a room's booker, in person, for a night for one of your acts. The room has to suit the act's music and
	/// the act has to be working that scene; the night must be one the room opens, past the announced calendar and within
	/// six weeks. A yes is a dated booking placed on that night's bill when it is drawn up, under every ordinary rule, so
	/// it can still fall through -- and only a set actually played counts. The gig's money is the performers' (the label
	/// takes nothing here; commissions are F5's). A no stands for a quarter unless the act plays the room meanwhile.
	/// </summary>
	public bool PitchActToBooker(string roomId, string artistId, int day, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		SceneRoomProfile room = SceneRoomCatalog.Get(roomId);
		if (room == null || !room.IsPerformance) { message = "That room doesn't book acts."; return false; }
		SimulatedArtist act = ArtistManager.Instance?.GetArtist(artistId);
		if (act == null || act.labelId != Label.labelId) { message = "That act isn't on your roster."; return false; }
		if (LocalScenePersistenceService.SceneIdFor(CurrentCityId) != room.PlaceId) { message = $"You have to be in town to see the booker at {room.Name}."; return false; }
		var clock = TimeManager.Instance;
		if (clock == null || clock.CurrentHour < BookerFromHour || clock.CurrentHour >= room.CloseHour) {
			message = $"{room.ContactName} is in from {BookerFromHour - 12} p.m. until {room.Name} closes."; return false;
		}
		int today = LocalSceneRoomService.Day(clock.CurrentDate), first = LocalSceneRoomService.FirstBookableDay;
		if (day < first || day > first + LocalSceneRoomService.BookingHorizonDays) {
			message = $"He books from {LocalSceneRoomService.Date(first).ToShortString()} to {LocalSceneRoomService.Date(first + LocalSceneRoomService.BookingHorizonDays).ToShortString()}; the nights before that are already set."; return false;
		}
		if (!SceneLiveEconomics.OpenOn(room, LocalSceneRoomService.Date(day).DayOfWeek)) { message = $"{room.Name} isn't open that night."; return false; }
		int year = LocalSceneRoomService.Date(day).year;
		if (!room.Families.Contains(GenreCatalog.Get(GenreCatalog.MapLegacy(act.primaryGenre, year)).Family)) {
			message = $"{room.Name} doesn't put on {GenreNameFormatter.Format(act.primaryGenre)}; he'd say so before you sat down."; return false;
		}
		if (LocalSceneRoomService.Bookings.Any(b => b.RoomId == roomId && b.ArtistId == artistId && b.Status == SceneBillStatus.Scheduled)) {
			message = $"{act.stageName} already has a night coming at {room.Name}."; return false;
		}
		int shows = LocalSceneRoomService.ShowsAt(roomId, artistId);
		var last = LocalSceneRoomService.LastPitch(roomId, artistId);
		if (last != null && !last.Accepted && today - last.Day < BookerRefusalDays && shows <= last.ShowsAtPitch) {
			message = $"{room.ContactName} said no to {act.stageName} on {LocalSceneRoomService.Date(last.Day).ToShortString()}. Nothing's changed since; let them play somewhere first.";
			return false;
		}
		if (!Require(BookerPitchHours, out message)) return false;
		Spend(BookerPitchHours);

		// He can only book an act that is actually working this scene; he'd know.
		bool present = LocalSceneRoomService.Eligible(act, room, day);
		bool yes = present && GD.Randf() < Mathf.Max(0.05f, BookerAcceptChance(act, shows) - BackerRoomPenalty(roomId));
		LocalSceneRoomService.RecordPitch(new SceneBookerPitch { RoomId = roomId, ArtistId = artistId, LabelId = Label.labelId, Day = today, Accepted = yes, ShowsAtPitch = shows });
		string night = LocalSceneRoomService.Date(day).ToHeadlineString();
		if (!yes) {
			message = !present ? $"{room.ContactName}: \"{act.stageName}? They're not working round here. Bring me an act I can see.\""
				: $"{room.ContactName} heard you out and passed on {act.stageName} for now.";
			Note($"Pitched {act.stageName} to {room.ContactTitle}. {message}");
			Changed?.Invoke();
			return true;
		}
		LocalSceneRoomService.AddBooking(new SceneBooking { RoomId = roomId, ArtistId = artistId, LabelId = Label.labelId, Day = day, Status = SceneBillStatus.Scheduled, Note = "Promised; goes on the bill when the week is drawn up." });
		message = $"{room.ContactName} pencils {act.stageName} in at {room.Name} on {night}. The door money is the band's; it goes on the bill when that week's drawn up.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>The advance orders held for a title: accounts, copies and the COD value at the dealer price. A reservation
	/// for the release plan (§6.2) -- not cash, not sell-through, not a promise the dealer won't cancel.</summary>
	public (int Accounts, int Copies, float CodValue) AdvanceOrdersFor(string recordId) {
		var held = inboundCalls.Where(c => c.RecordId == recordId && c.Reason == InboundCallReason.PreOrder).ToList();
		int copies = held.Sum(c => c.RequestedQty);
		return (held.Count, copies, copies * DealerPrice);
	}
}
