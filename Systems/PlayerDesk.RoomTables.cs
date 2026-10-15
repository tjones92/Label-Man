using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Early-game F1/F2: selling 45s at a real room's table on a real night.
///
/// The old walk-up hop table (a synthetic Venue stop) sold any day, with no show, no booker and no crowd. With the room
/// calendar on, a table is now a seat at a dated bill in one of the town's <see cref="SceneRoomCatalog"/> rooms:
/// - <b>Permission.</b> A room only lets you sell if its booker says so. Your own act on tonight's bill brings its records
///   to its own show without asking; otherwise ask the booker for table rights (1h, in his hours). A yes is a term (91
///   days) at the room's cut of the table money; a no stands for the same term unless one of your acts plays the room.
/// - <b>The night.</b> Set up an hour before the first set and stay until the bill ends. The sale settles when the bill
///   resolves: a show that didn't go ahead sells nothing, and the hours are still gone.
/// - <b>The crowd (F2).</b> A room draws on a pool of local regulars (about three houses' worth). The record's appeal sets
///   how many of them would ever buy it; tonight's attendance is the share of that pool in the room, doubled when your act
///   played (they just heard the song). Buyers are remembered per room and title, a third of the town's other room-table
///   buyers count against this room too (the same people go out to more than one room), and 5% turn over a week.
/// Authored values, not sourced: the cuts, the rights chance, the pool, the overlap and the per-night ceiling.
///
/// Old saves: synthetic Venue stops stay as record-hop sites (<see cref="BookRecordHop"/>), keeping their crowd memory.
/// They held no stock, balances or calls, so retiring their walk-up table moves nothing; no table right is inferred.
/// </summary>
public partial class PlayerDesk {
	public sealed class RoomTableRights {
		public string RoomId;
		public bool Granted;
		/// <summary>Scene day of the last ask.</summary>
		public int AskedDay;
		/// <summary>Last scene day the rights hold (granted only).</summary>
		public int ExpiresDay;
		/// <summary>The room's share of the table's gross, agreed at the ask.</summary>
		public float Cut;
		/// <summary>Your acts' shows in the room when asked: a no stands until this grows (or a term passes).</summary>
		public int LabelShowsAtAsk;
	}

	public const int TableRightsHours = 1;
	public const int TableRightsTermDays = 91;
	public const float RoomPoolTurns = 3f;
	public const float RoomTableOffFamilyFit = 0.5f;
	public const float RoomTableOwnActReach = 2f;
	public const float RoomTableOverlapShare = 0.35f;
	public const float RoomTableShareOfHouse = 0.30f;
	public const int RoomTableNightCeiling = 40;

	private readonly Dictionary<string, RoomTableRights> roomTableRights = new(StringComparer.Ordinal);
	/// <summary>roomId -> recordId -> what that room's regulars have bought of it.</summary>
	private readonly Dictionary<string, Dictionary<string, TableCrowd>> roomTableCrowds = new(StringComparer.Ordinal);

	/// <summary>Rooms where a table makes sense on a show night: the clubs, halls, roadhouses and coffeehouses. A supper or
	/// listening room doesn't let a man hawk 45s between sets, and a trade appointment has no audience.</summary>
	public static bool RoomSellsRecords(SceneRoomProfile room) =>
		room != null && room.Kind is SceneRoomKind.Club or SceneRoomKind.CommunityHall or SceneRoomKind.Roadhouse or SceneRoomKind.Coffeehouse;

	/// <summary>The room's share of the table's gross. A hall takes a token for its upkeep; a club wants a real cut.</summary>
	public static float RoomTableCut(SceneRoomKind kind) => kind switch {
		SceneRoomKind.Club => 0.25f,
		SceneRoomKind.Roadhouse => 0.20f,
		SceneRoomKind.Coffeehouse => 0.15f,
		SceneRoomKind.CommunityHall => 0.10f,
		_ => 0.25f,
	};

	private static float RoomTableOpenness(SceneRoomKind kind) => kind switch {
		SceneRoomKind.CommunityHall => 0.15f,
		SceneRoomKind.Coffeehouse => 0.10f,
		SceneRoomKind.Roadhouse => 0.05f,
		_ => 0f,
	};

	/// <summary>Whether the town's walk-up tables have moved onto its rooms' show nights.</summary>
	public static bool RoomTablesActive(string cityId) =>
		LocalScenes.Rooms && SceneRoomCatalog.ForPlace(cityId).Any(RoomSellsRecords);

	/// <summary>Sets your acts have played in a room, pre-rolled history included.</summary>
	public int LabelShowsAt(string roomId) => Roster.Sum(a => LocalSceneRoomService.ShowsAt(roomId, a.artistId));

	private static int SceneToday => LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);

	/// <summary>Current rights at a room, or null. Expired rights read as none.</summary>
	public RoomTableRights TableRightsAt(string roomId) =>
		roomTableRights.TryGetValue(roomId ?? "", out var r) && r.Granted && r.ExpiresDay >= SceneToday ? r : null;

	/// <summary>The last answer at a room, granted or not, for the UI.</summary>
	public RoomTableRights LastTableRightsAsk(string roomId) => roomTableRights.GetValueOrDefault(roomId ?? "");

	/// <summary>The booker's yes, before the roll: how much your acts have played the room, and how open the room is to a
	/// table at all. A room that knew you before is easier. Authored weights.</summary>
	public float TableRightsChance(SceneRoomProfile room) {
		float shows = Mathf.Min(1f, LabelShowsAt(room.Id) / (float)LocalSceneRoomService.StandingSaturation);
		bool knewYou = roomTableRights.TryGetValue(room.Id, out var prior) && prior.Granted;
		return Mathf.Clamp(0.20f + 0.45f * shows + RoomTableOpenness(room.Kind) + (knewYou ? 0.20f : 0f) - BackerRoomPenalty(room.Id), 0.05f, 0.85f);
	}

	/// <summary>Ask a room's booker, in person, for the right to sell your records at a table on his nights.</summary>
	public bool AskForTableRights(string roomId, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		SceneRoomProfile room = SceneRoomCatalog.Get(roomId);
		if (!LocalScenes.Rooms || !RoomSellsRecords(room)) { message = "That room doesn't put a record table out."; return false; }
		if (LocalScenePersistenceService.SceneIdFor(CurrentCityId) != room.PlaceId) { message = $"You have to be in town to see the booker at {room.Name}."; return false; }
		var clock = TimeManager.Instance;
		if (clock == null || clock.CurrentHour < BookerFromHour || clock.CurrentHour >= room.CloseHour) {
			message = $"{room.ContactName} is in from {BookerFromHour - 12} p.m. until {room.Name} closes."; return false;
		}
		int today = SceneToday, shows = LabelShowsAt(roomId);
		if (TableRightsAt(roomId) is { } held) {
			message = $"You already have a table at {room.Name} through {LocalSceneRoomService.Date(held.ExpiresDay).ToShortString()}."; return false;
		}
		if (roomTableRights.TryGetValue(roomId, out var last) && !last.Granted && today - last.AskedDay < TableRightsTermDays && shows <= last.LabelShowsAtAsk) {
			message = $"{room.ContactName} said no to a table on {LocalSceneRoomService.Date(last.AskedDay).ToShortString()}. Nothing's changed; get one of your acts on his stage first.";
			return false;
		}
		if (!Require(TableRightsHours, out message)) return false;
		Spend(TableRightsHours);

		bool yes = GD.Randf() < TableRightsChance(room);
		float cut = RoomTableCut(room.Kind);
		roomTableRights[roomId] = new RoomTableRights {
			RoomId = roomId, Granted = yes, AskedDay = today, ExpiresDay = yes ? today + TableRightsTermDays : today,
			Cut = cut, LabelShowsAtAsk = shows,
		};
		message = yes
			? $"{room.ContactName} will let you set up a table at {room.Name} on his nights through {LocalSceneRoomService.Date(today + TableRightsTermDays).ToShortString()}. The room takes {cut:P0} of what the table takes."
			: $"{room.ContactName} doesn't want a record table in {room.Name} -- not from a label he hasn't seen on his stage.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>A night you could work a table: a bill in a room that sells, with your rights there or your act on it.</summary>
	public sealed record RoomTableNight(SceneRoomProfile Room, SceneBill Bill, bool OwnAct, RoomTableRights Rights);

	/// <summary>Announced bills in this town you could sell at, today first.</summary>
	public IReadOnlyList<RoomTableNight> RoomTableNights(string cityId) {
		if (Label == null || !LocalScenes.Rooms || TimeManager.Instance == null) return Array.Empty<RoomTableNight>();
		var date = TimeManager.Instance.CurrentDate;
		int today = LocalSceneRoomService.Day(date), hour = TimeManager.Instance.CurrentHour;
		var nights = new List<RoomTableNight>();
		foreach (var bill in LocalSceneRoomService.AnnouncedCalendar(cityId, date)) {
			if (bill.Status != SceneBillStatus.Scheduled || bill.Day == today && bill.EndHour <= hour) continue;
			var room = SceneRoomCatalog.Get(bill.RoomId);
			if (!RoomSellsRecords(room)) continue;
			bool own = OwnActOn(bill);
			var rights = roomTableRights.TryGetValue(room.Id, out var r) && r.Granted && r.ExpiresDay >= bill.Day ? r : null;
			if (own || rights != null) nights.Add(new RoomTableNight(room, bill, own, rights));
		}
		return nights.OrderBy(n => n.Bill.Day).ThenBy(n => n.Bill.StartHour).ThenBy(n => n.Room.Id, StringComparer.Ordinal).ToList();
	}

	private bool OwnActOn(SceneBill bill) =>
		bill.Appearances.Any(a => a.Status != SceneBillStatus.Cancelled && ArtistManager.Instance?.GetArtist(a.ArtistId)?.labelId == Label?.labelId);

	/// <summary>
	/// Work the table at a room's bill tonight: set up an hour before the first set, stay through the last, and sell to
	/// whoever came. Settles when the bill resolves -- a night that doesn't go ahead sells nothing. Cash at list, less the
	/// room's cut (none when your own act's set is the reason you're there).
	/// </summary>
	public bool WorkRoomTable(string roomId, string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		SceneRoomProfile room = SceneRoomCatalog.Get(roomId);
		if (!LocalScenes.Rooms || !RoomSellsRecords(room)) { message = "That room doesn't put a record table out."; return false; }
		if (LocalScenePersistenceService.SceneIdFor(CurrentCityId) != room.PlaceId) { message = "You have to be in town for that."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to sell."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to sell right now -- the master's out."; return false; }
		PressStock stockOnHand = StockFor(recordId);
		if (stockOnHand == null || stockOnHand.Remaining <= 0) { message = "None of that pressed on hand -- order a run and let it come in first."; return false; }
		RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		if (rec == null) { message = "Only a released single goes on a table."; return false; }
		var clock = TimeManager.Instance;
		if (clock == null) { message = "No clock."; return false; }
		int today = SceneToday, now = clock.CurrentHour * 60 + clock.CurrentMinute;
		SceneBill bill = LocalSceneRoomService.Calendar(CurrentCityId, clock.CurrentDate)
			.FirstOrDefault(b => b.RoomId == roomId && b.Day == today && b.Status == SceneBillStatus.Scheduled && b.EndHour * 60 > now);
		if (bill == null) { message = $"{room.Name} has no bill on tonight."; return false; }
		bool ownAct = OwnActOn(bill);
		RoomTableRights rights = TableRightsAt(roomId);
		if (!ownAct && rights == null) { message = $"{room.ContactName} hasn't given you a table at {room.Name}. Ask him first, or get one of your acts on the bill."; return false; }
		int setUp = (bill.StartHour - 1) * 60;
		if (now < setUp) { message = $"The table goes up an hour before the first set: {Clock12(bill.StartHour - 1)}."; return false; }
		int minutes = Math.Max(60, bill.EndHour * 60 - now);
		// Turning up for the last set sells to the last set's crowd, not the whole night's.
		float staffed = Mathf.Clamp((bill.EndHour * 60 - Math.Max(now, bill.StartHour * 60)) / (float)Math.Max(60, (bill.EndHour - bill.StartHour) * 60), 0f, 1f);
		if (!Require(0, out message)) return false;
		if (!clock.CanAffordMinutes(minutes)) { message = $"Staying through to {Clock12(bill.EndHour)} would pass today's hard stop."; return false; }

		clock.SpendMinutes(minutes);
		var resolved = LocalSceneRoomService.Bill(bill.Id);
		workedCities.Add(CurrentCityId);
		if (resolved == null || resolved.Status != SceneBillStatus.Performed) {
			message = $"The bill at {room.Name} didn't go ahead tonight. You packed the records back up.";
			Note(message);
			Changed?.Invoke();
			return true;
		}
		bool ownPlayed = resolved.Appearances.Any(a => a.Status == SceneBillStatus.Performed && ArtistManager.Instance?.GetArtist(a.ArtistId)?.labelId == Label.labelId);
		GameDate date = clock.CurrentDate;
		int qty = Math.Min(Mathf.RoundToInt(RoomTableTake(room, resolved, recordId, ownPlayed, date) * staffed), stockOnHand.Remaining);
		if (qty <= 0) {
			message = $"Worked the table at {room.Name} to {Clock12(resolved.EndHour)} for {resolved.Attendance:N0} people -- nobody bought \"{TitleForRecord(recordId)}\".";
			Note(message);
			Changed?.Invoke();
			return true;
		}
		stockOnHand.Remaining -= qty;
		NoteRoomTableSale(room.Id, recordId, qty, date);
		BookTrunkSale(rec, qty, new PlayerStop { StopId = room.Id, CityId = CurrentCityId, Kind = StopKind.Venue }, cashNow: true);
		float cutRate = ownPlayed ? 0f : rights?.Cut ?? RoomTableCut(room.Kind);
		float cut = qty * ChannelPrice(StopKind.Venue) * cutRate;
		if (cut > 0f) {
			Label.cashReserves -= cut;
			Label.monthlyExpenses += cut;
			rec.lifetimeLabelNet -= cut;
		}
		message = $"Sold {qty:N0} of \"{TitleForRecord(recordId)}\" at the table at {room.Name} ({resolved.Attendance:N0} in){(cut > 0f ? $"; the room took ${cut:N2}" : "")}.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>The regulars a room draws on: about three houses' worth of local people over a few weeks.</summary>
	private static float RoomAudiencePool(SceneBill bill) => Mathf.Max(1f, LocalSceneRoomService.CapacityOf(bill) * RoomPoolTurns);

	/// <summary>How many of this room's regulars would ever buy the title, on its appeal here and the room's music.</summary>
	private float RoomTableCeiling(SceneRoomProfile room, SceneBill bill, string recordId) {
		float appeal = RecordAppeal(recordId, CurrentCityId) ?? EarFloorAppeal;
		float t = Mathf.InverseLerp(EarFloorAppeal, EarCeilingAppeal, appeal);
		var record = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId)?.baseRecord;
		int year = LocalSceneRoomService.Date(bill.Day).year;
		bool fits = record != null && room.Families.Contains(GenreCatalog.Get(GenreCatalog.MapLegacy(record.primaryGenre, year)).Family);
		return RoomAudiencePool(bill) * Mathf.Lerp(TableTakeUpMin, TableTakeUpMax, Mathf.Pow(t, CounterTakeUpCurve)) * (fits ? 1f : RoomTableOffFamilyFit);
	}

	private float RoomTableBoughtOwn(string roomId, string recordId, GameDate today) {
		if (!roomTableCrowds.TryGetValue(roomId, out var titles) || !titles.TryGetValue(recordId, out TableCrowd c)) return 0f;
		return c.Bought * Mathf.Pow(1f - TableCrowdTurnoverPerWeek, Math.Max(0, DaysBetween(c.LastSale, today)) / 7f);
	}

	/// <summary>This room's buyers of the title, plus the share of the town's other room-table buyers who also come here.</summary>
	private float RoomTableBought(SceneRoomProfile room, string recordId, GameDate today) {
		float own = RoomTableBoughtOwn(room.Id, recordId, today), others = 0f;
		foreach (var other in SceneRoomCatalog.ForPlace(room.PlaceId))
			if (other.Id != room.Id) others += RoomTableBoughtOwn(other.Id, recordId, today);
		return own + RoomTableOverlapShare * others;
	}

	/// <summary>What the table sells tonight: the interested regulars still without it, times the share of the pool that
	/// came in (doubled when your act played), within a share of the house and a per-night ceiling.</summary>
	private int RoomTableTake(SceneRoomProfile room, SceneBill bill, string recordId, bool ownPlayed, GameDate today) {
		float pool = RoomAudiencePool(bill);
		float left = Mathf.Max(0f, RoomTableCeiling(room, bill, recordId) - RoomTableBought(room, recordId, today));
		float reach = Mathf.Min(1f, bill.Attendance / pool * (ownPlayed ? RoomTableOwnActReach : 1f));
		int qty = Mathf.RoundToInt(left * reach);
		return Math.Max(0, Math.Min(Math.Min(qty, Mathf.FloorToInt(left)), Math.Min(RoomTableNightCeiling, Mathf.FloorToInt(bill.Attendance * RoomTableShareOfHouse))));
	}

	private void NoteRoomTableSale(string roomId, string recordId, int qty, GameDate today) {
		if (!roomTableCrowds.TryGetValue(roomId, out var titles)) roomTableCrowds[roomId] = titles = new(StringComparer.Ordinal);
		titles[recordId] = new TableCrowd { Bought = RoomTableBoughtOwn(roomId, recordId, today) + qty, LastSale = today };
	}

	/// <summary>Read-only for probes and UI: interested regulars at a room still without the title (no bill needed; uses the room's capacity).</summary>
	public int RoomTableBuyersLeft(string roomId, string recordId) {
		var room = SceneRoomCatalog.Get(roomId);
		if (room == null) return 0;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		var probe = new SceneBill { RoomId = roomId, Day = LocalSceneRoomService.Day(today), Capacity = SceneLiveEconomics.Capacity(room) };
		return Mathf.FloorToInt(Mathf.Max(0f, RoomTableCeiling(room, probe, recordId) - RoomTableBought(room, recordId, today)));
	}

	/// <summary>Checks and the save round-trip only: stage a room's answer and its crowd memory without a roll.</summary>
	internal void StageRoomTableForChecks(string roomId, bool granted, int askedDay, float cut, string recordId, float bought, GameDate lastSale) {
		roomTableRights[roomId] = new RoomTableRights { RoomId = roomId, Granted = granted, AskedDay = askedDay,
			ExpiresDay = granted ? askedDay + TableRightsTermDays : askedDay, Cut = cut, LabelShowsAtAsk = LabelShowsAt(roomId) };
		if (recordId == null) return;
		if (!roomTableCrowds.TryGetValue(roomId, out var titles)) roomTableCrowds[roomId] = titles = new(StringComparer.Ordinal);
		titles[recordId] = new TableCrowd { Bought = bought, LastSale = lastSale };
	}

	/// <summary>Checks only: a room's own remembered buyers of a title, undecayed.</summary>
	internal float RoomTableCrowdForChecks(string roomId, string recordId) =>
		roomTableCrowds.TryGetValue(roomId, out var titles) && titles.TryGetValue(recordId, out var c) ? c.Bought : -1f;

	// ── Save ────────────────────────────────────────────────────────────────────────────────────────

	private List<RoomTableSaveData> CaptureRoomTables() {
		var ids = roomTableRights.Keys.Concat(roomTableCrowds.Keys).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal);
		return ids.Select(id => {
			roomTableRights.TryGetValue(id, out var r);
			roomTableCrowds.TryGetValue(id, out var crowds);
			return new RoomTableSaveData {
				RoomId = id, HasRights = r != null, Granted = r?.Granted ?? false, AskedDay = r?.AskedDay ?? 0, ExpiresDay = r?.ExpiresDay ?? 0,
				Cut = r?.Cut ?? 0f, LabelShowsAtAsk = r?.LabelShowsAtAsk ?? 0,
				Crowds = (crowds ?? new()).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new TableCrowdSaveData {
					RecordId = kv.Key, Bought = kv.Value.Bought, Year = kv.Value.LastSale.year, Month = kv.Value.LastSale.month, Day = kv.Value.LastSale.day,
				}).ToList(),
			};
		}).ToList();
	}

	private void RestoreRoomTables(List<RoomTableSaveData> saved) {
		roomTableRights.Clear();
		roomTableCrowds.Clear();
		foreach (var row in saved ?? new List<RoomTableSaveData>()) {
			if (string.IsNullOrEmpty(row?.RoomId) || SceneRoomCatalog.Get(row.RoomId) == null) continue;
			if (row.HasRights)
				roomTableRights[row.RoomId] = new RoomTableRights { RoomId = row.RoomId, Granted = row.Granted, AskedDay = row.AskedDay,
					ExpiresDay = row.ExpiresDay, Cut = row.Cut, LabelShowsAtAsk = row.LabelShowsAtAsk };
			foreach (var crowd in row.Crowds ?? new List<TableCrowdSaveData>()) {
				if (crowd?.RecordId == null || crowd.Year <= 0) continue;
				if (!roomTableCrowds.TryGetValue(row.RoomId, out var titles)) roomTableCrowds[row.RoomId] = titles = new(StringComparer.Ordinal);
				titles[crowd.RecordId] = new TableCrowd { Bought = crowd.Bought, LastSale = new GameDate(crowd.Year, crowd.Month, crowd.Day) };
			}
		}
	}
}

/// <summary>Early-game F1: one room's table rights and the record memory of its regulars. Absent in older saves.</summary>
public sealed class RoomTableSaveData {
	public string RoomId { get; set; }
	public bool HasRights { get; set; }
	public bool Granted { get; set; }
	public int AskedDay { get; set; }
	public int ExpiresDay { get; set; }
	public float Cut { get; set; }
	public int LabelShowsAtAsk { get; set; }
	public List<TableCrowdSaveData> Crowds { get; set; } = new();
}
