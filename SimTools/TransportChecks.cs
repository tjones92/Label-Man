using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life Phase 1c (§3.9): getting around without the car. A desk with no lien keeps the old car and quotes exactly what
/// DriveQuote always did; a lien takes the car, and the bus (default) and the cab quote their own hours, fares and carry
/// limits; the last bus leaves when the working day ends and a cab won't go past TaxiMaxMiles; errands in town cost a half-hour
/// or a fare; a used or new car ends it; each holder keeps the car in his own place at his own price; and all of it survives a
/// save (an older save keeps the car it had).
///   --path . SimTools/TransportChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints TRANSPORT_CHECKS_PASS / _FAIL.
/// </summary>
public partial class TransportChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	private static readonly Dictionary<LienHolder, PartyMemory> Own = new() {
		[LienHolder.Sawyer] = new() { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable },
		[LienHolder.Kubiak] = new() { Where = PartyWhere.BarBackRoom, Who = PartyWho.RouteMan, Brag = PartyBrag.TheLabel, End = PartyEnd.Car },
		[LienHolder.Winters] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.MajorMan, Brag = PartyBrag.Hits, End = PartyEnd.PhoneCall },
		[LienHolder.Floria] = new() { Where = PartyWhere.HotelBallroom, Who = PartyWho.CamelCoat, Brag = PartyBrag.TheLabel, End = PartyEnd.PhoneCall },
		[LienHolder.Germain] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.DiscJockey, Brag = PartyBrag.TheAct, End = PartyEnd.Car },
	};

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance;
			var clock = TimeManager.Instance;
			clock.RestoreClock(new GameDate(1960, 1, 1), 9);
			Check(desk.FoundLabel("Transport Checks", "detroit", FoundingArchetype.ExMusician, null, Own[LienHolder.Sawyer], out var message), "Found: " + message);
			string home = desk.Label.homeCityId;
			var reach = desk.ReachableCities().OrderBy(c => DistanceModel.GetRoadMilesBetween(home, c.cityId)).ThenBy(c => c.cityId, StringComparer.Ordinal).ToList();
			Check(reach.Count >= 2, $"{reach.Count} towns within reach of {home}");
			NoLien(desk, home, reach);
			Without(desk, home, reach);
			Bus(desk, clock, home, reach);
			Taxi(desk, clock, home, reach);
			Errands(desk);
			Buying(desk, clock);
			foreach (var holder in Enum.GetValues<LienHolder>()) Holder(desk, clock, holder);
			Save(desk, clock);
			GD.Print($"TRANSPORT_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("TRANSPORT_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private static string Nearest(PlayerDesk desk) => desk.ReachableCities()
		.OrderBy(c => DistanceModel.GetRoadMilesBetween(desk.Label.homeCityId, c.cityId)).ThenBy(c => c.cityId, StringComparer.Ordinal).First().cityId;

	private void Reset(PlayerDesk desk, TimeManager clock, LienHolder holder, CarKind car = CarKind.Gone, int year = 1960, int month = 1, int hour = 9) {
		clock.RestoreClock(new GameDate(1960, 1, 1), 9);
		desk.FoundLienForChecks(Own[holder], holder);
		desk.SetCarForChecks(car);
		desk.Label.cashReserves = 5000f;
		clock.RestoreClock(new GameDate(year, month, 1), hour);
	}

	private void NoLien(PlayerDesk desk, string home, List<MarketCity> reach) {
		desk.RestoreLienForChecks(null);
		Check(desk.HasCar && desk.ActiveMode == TransportMode.Car && desk.CarOwned == CarKind.Original, "A desk with no lien keeps the old car");
		Check(reach.All(c => desk.TravelQuote(home, c.cityId) == desk.DriveQuote(home, c.cityId)), "With no lien every quote is exactly the old DriveQuote");
		Check(desk.ErrandTransit == (0, 0f) && desk.CarryLeft == int.MaxValue && !desk.CarryLimited && desk.CarWhereabouts() == null,
			"With no lien errands are free, nothing limits what he carries and there is no car to look for");
	}

	private void Without(PlayerDesk desk, string home, List<MarketCity> reach) {
		desk.FoundLienForChecks(Own[LienHolder.Sawyer], LienHolder.Sawyer);
		Check(!desk.HasCar && desk.CarOwned == CarKind.Gone && !desk.CarRecovered && desk.ActiveMode == TransportMode.Bus, "The night took the car: he starts on the bus");
		foreach (var c in reach) {
			var car = desk.DriveQuote(home, c.cityId);
			var bus = desk.TravelQuote(home, c.cityId);
			var cab = desk.TravelQuote(home, c.cityId, TransportMode.Taxi);
			Check(bus.Hours > car.Hours - 1 && bus.Hours >= 2 && bus.Cost >= 1f, $"{c.name}: the bus ({bus.Hours}h, ${bus.Cost:N0}) is slower than the drive ({car.Hours}h)");
			Check(cab.Hours < bus.Hours && cab.Cost > bus.Cost && cab.Cost > car.Gas, $"{c.name}: the cab ({cab.Hours}h, ${cab.Cost:N0}) is quicker than the bus and dearer than the bus or the gas (${car.Gas:N0})");
		}
		var far = reach.Last();
		Check(desk.TravelQuote(home, far.cityId).Cost < desk.TravelQuote(home, far.cityId, TransportMode.Taxi).Cost, "The bus is cheaper than a cab to the farthest town");
	}

	private void Bus(PlayerDesk desk, TimeManager clock, string home, List<MarketCity> reach) {
		Reset(desk, clock, LienHolder.Sawyer, hour: 9);
		Check(desk.ErrandTransit == (30, 0.30f), "On the bus an errand costs a half-hour and thirty cents: " + desk.ErrandTransit);
		var near = reach.First();
		var quote = desk.TravelQuote(home, near.cityId);
		float cash = desk.Label.cashReserves; int hour = clock.CurrentHour;
		Check(desk.DriveTo(near.cityId, out var message), "Bus to " + near.name + ": " + message);
		Check(Near(cash - desk.Label.cashReserves, quote.Cost) && clock.CurrentHour - hour == quote.Hours, $"It cost the fare (${quote.Cost:N0}) and {quote.Hours}h");
		Check(desk.CurrentCityId == near.cityId && desk.CarryLimited && desk.CarryLimit == BusCartons * CartonCopies && desk.CarryLeft == desk.CarryLimit,
			$"Out of town on the bus he carries {BusCartons} cartons: {desk.CarryLeft} of {desk.CarryLimit}");
		desk.Lien.TripCopies = desk.CarryLimit - 10;
		Check(desk.CarryLeft == 10, "Ten copies left to hand out");
		desk.Lien.TripCopies = desk.CarryLimit;
		Check(desk.CarryLeft == 0, "None left once the cartons are gone");
		clock.RestoreClock(clock.CurrentDate, 9);
		Check(desk.DriveHome(out message), "Bus home: " + message);
		Check(desk.AtHome && desk.Lien.TripCopies == 0 && !desk.CarryLimited, "Home again: empty hands, nothing limits him");

		// The farthest town a bus can reach inside a working day, started late enough that it can't make it back before six.
		var farthest = reach.Last(c => desk.TravelQuote(home, c.cityId).Hours <= 8);
		int hoursFar = desk.TravelQuote(home, farthest.cityId).Hours;
		clock.RestoreClock(clock.CurrentDate, clock.RegularWorkdayEndHour - hoursFar + 1);
		cash = desk.Label.cashReserves;
		Check(!desk.DriveTo(farthest.cityId, out message) && message.Contains("last bus") && desk.AtHome && Near(cash, desk.Label.cashReserves),
			$"A {hoursFar}h bus that would run past the end of the day is refused, and costs nothing: {message}");
		desk.SetCarForChecks(CarKind.Original);
		Check(desk.DriveTo(farthest.cityId, out message), "In his own car the same trip goes, into overtime: " + message);
		Check(!desk.CarryLimited, "A driver carries what he likes");
		desk.DriveHome(out _);
	}

	private void Taxi(PlayerDesk desk, TimeManager clock, string home, List<MarketCity> reach) {
		Reset(desk, clock, LienHolder.Sawyer, hour: 9);
		Check(desk.SetTransportMode(TransportMode.Taxi, out var message) && desk.ActiveMode == TransportMode.Taxi, "Cabs from now on: " + message);
		Check(desk.ErrandTransit == (0, 1.5f), "In a cab an errand costs a fare, not time: " + desk.ErrandTransit);
		var past = reach.FirstOrDefault(c => DistanceModel.GetRoadMilesBetween(home, c.cityId) > TaxiMaxMiles);
		var within = reach.FirstOrDefault(c => DistanceModel.GetRoadMilesBetween(home, c.cityId) <= TaxiMaxMiles);
		Check(past != null || within != null, "There is a town to try");
		if (past != null) {
			float cash = desk.Label.cashReserves;
			Check(!desk.DriveTo(past.cityId, out message) && message.Contains("cabbie") && Near(cash, desk.Label.cashReserves) && desk.AtHome,
				$"No cabbie goes the {DistanceModel.GetRoadMilesBetween(home, past.cityId):N0} miles to {past.name}: {message}");
		}
		if (within != null) {
			Check(desk.DriveTo(within.cityId, out message), $"A cab to {within.name}, within the range: {message}");
			Check(desk.CarryLimit == TaxiCartons * CartonCopies, $"A cab carries {TaxiCartons} cartons");
			desk.DriveHome(out _);
		}
		Check(desk.SetTransportMode(TransportMode.Bus, out _) && desk.ActiveMode == TransportMode.Bus, "And back to the bus");
		desk.SetCarForChecks(CarKind.Original);
		Check(!desk.SetTransportMode(TransportMode.Taxi, out _), "With a car there's nothing to pick");
		desk.SetCarForChecks(CarKind.Gone);
	}

	private void Errands(PlayerDesk desk) {
		desk.SetCarForChecks(CarKind.Gone);
		desk.SetTransportMode(TransportMode.Bus, out _);
		Check(desk.ErrandTransit == (30, 0.30f), "Bus errand: 30 minutes, $0.30");
		desk.SetTransportMode(TransportMode.Taxi, out _);
		Check(desk.ErrandTransit == (0, 1.5f), "Cab errand: $1.50");
		desk.SetCarForChecks(CarKind.Used);
		Check(desk.ErrandTransit == (0, 0f), "A car of any age makes errands free");
		desk.SetCarForChecks(CarKind.Gone);
		desk.SetTransportMode(TransportMode.Bus, out _);
	}

	private void Buying(PlayerDesk desk, TimeManager clock) {
		Reset(desk, clock, LienHolder.Sawyer, hour: 9);
		string home = desk.Label.homeCityId;
		desk.Label.cashReserves = 100f;
		Check(!desk.CanBuyCar(false, out var message) && message.Contains("short"), "With $100 he can't buy the sedan: " + message);
		desk.Label.cashReserves = 5000f;
		var far = desk.ReachableCities().OrderByDescending(c => DistanceModel.GetRoadMilesBetween(home, c.cityId)).First();
		var original = desk.DriveQuote(home, far.cityId);
		int hour = clock.CurrentHour;
		Check(desk.BuyCar(false, out message), "A used sedan: " + message);
		Check(Near(5000f - desk.Label.cashReserves, UsedCarPrice) && clock.CurrentHour - hour == CarDealingHours && desk.CarOwned == CarKind.Used && desk.ActiveMode == TransportMode.Car,
			$"${UsedCarPrice:N0} and {CarDealingHours}h, and he has wheels");
		var used = desk.TravelQuote(home, far.cityId);
		Check(used.Hours >= original.Hours && used.Cost > original.Gas, $"The used sedan is slower and thirstier than the old car: {used} against {original}");
		Check(!desk.CanBuyCar(false, out _), "He can't buy a second used car");
		float cash = desk.Label.cashReserves;
		Check(desk.BuyCar(true, out message) && desk.CarOwned == CarKind.New && Near(cash - desk.Label.cashReserves, NewCarPrice), "A new car: " + message);
		Check(desk.TravelQuote(home, far.cityId) == original, "The new car drives like the old one");
		Check(!desk.CanBuyCar(true, out _), "Nothing left to buy");
		desk.Label.cashReserves = 5000f;
		Reset(desk, clock, LienHolder.Sawyer, hour: 9);
		desk.DriveTo(Nearest(desk), out _);
		Check(!desk.CanBuyCar(false, out message) && message.Contains("lot in town"), "Cars are bought in town, not on the road: " + message);
		desk.DriveHome(out _);
	}

	private void Holder(PlayerDesk desk, TimeManager clock, LienHolder holder) {
		Reset(desk, clock, holder, month: 3);
		var hunt = desk.CarWhereabouts();
		Check(hunt != null, $"{holder}: the car is somewhere");
		float cash = desk.Label.cashReserves;
		switch (holder) {
			case LienHolder.Sawyer:
				Check(hunt.Available && Near(hunt.Price ?? -1, 1100f), "Sawyer: behind the room, $1,100 (a high price)");
				break;
			case LienHolder.Kubiak:
				Check(hunt.Available && Near(hunt.Price ?? -1, 700f), "Kubiak: his yard, $700 in a good year");
				clock.RestoreClock(new GameDate(1964, 3, 1), 9);
				Check(Near(desk.CarWhereabouts().Price ?? -1, 450f), "Kubiak: $450 once his route is failing (1964)");
				clock.RestoreClock(new GameDate(1960, 3, 1), 9);
				break;
			case LienHolder.Winters:
				Check(hunt.Available && hunt.Price == null, "Winters: the pound won't say its fee to a man with the Suit at 1");
				float fee = 60f + 20f * 2;   // signed in January, now March
				Check(desk.BuyBackCar(out var m) && Near(cash - desk.Label.cashReserves, fee), $"He pays what the window says, ${fee:N0} after two months: {m}");
				break;
			case LienHolder.Floria:
				Check(hunt.Available && Near(hunt.Price ?? -1, 800f), "Floria: a used-car lot; the Street at 4 knows which");
				break;
			case LienHolder.Germain:
				Check(!hunt.Available && hunt.Blocked.Contains("Jack"), "Germain: the manager won't release it to anyone connected to Jack (Fixer 2)");
				Check(!desk.BuyBackCar(out _) && Near(cash, desk.Label.cashReserves), "And he can't buy it");
				clock.RestoreClock(new GameDate(1964, 3, 1), 9);
				hunt = desk.CarWhereabouts();
				Check(hunt.Available && Near(hunt.Price ?? -1, 120f), "Germain: the manager has moved on by 1964; the storage is $120");
				break;
		}
		if (holder == LienHolder.Winters) {
			Check(desk.CarRecovered && desk.CarOwned == CarKind.Original && desk.CarWhereabouts() == null, "Winters: the old car is back");
			Reset(desk, clock, holder, month: 1, year: 1961);
			hunt = desk.CarWhereabouts();
			Check(!hunt.Available && hunt.Price == null && hunt.Where.Contains("auction") && !desk.BuyBackCar(out _), "Winters: after a year the pound has auctioned it");
			return;
		}
		cash = desk.Label.cashReserves;
		float price = hunt.Price ?? 0f;
		desk.DriveTo(Nearest(desk), out _);
		Check(!desk.BuyBackCar(out var away) && away.Contains("in town"), "He has to be at the office to buy it back: " + away);
		desk.DriveHome(out _);
		Check(desk.BuyBackCar(out var done), $"{holder}: bought back ({done})");
		Check(desk.CarRecovered && desk.CarOwned == CarKind.Original && desk.ActiveMode == TransportMode.Car && desk.CarWhereabouts() == null,
			$"{holder}: the old car is his again");
		Check(cash - desk.Label.cashReserves >= price - .01f, $"{holder}: it cost ${price:N0} (plus the bus fares out and back)");
		Check(!desk.BuyBackCar(out _), $"{holder}: it can't be bought twice");
	}

	private void Save(PlayerDesk desk, TimeManager clock) {
		Reset(desk, clock, LienHolder.Sawyer, CarKind.Used);
		desk.SetCarForChecks(CarKind.Used);
		desk.Lien.Mode = TransportMode.Taxi;
		desk.Lien.TripCopies = 33;
		var data = desk.CaptureLienForChecks();
		desk.RestoreLienForChecks(data);
		Check(desk.CarOwned == CarKind.Used && desk.Lien.Mode == TransportMode.Taxi && desk.Lien.TripCopies == 33 && !desk.CarRecovered, "Car, mode and trip load survive a save");
		data.Car = null;
		desk.RestoreLienForChecks(data);
		Check(desk.CarOwned == CarKind.Original && desk.CarRecovered && desk.CarWhereabouts() == null, "An older save without the car field keeps the car it had");
	}
}
