using System;
using System.Linq;
using Godot;

// ============================================================================================
// GETTING AROUND WITHOUT THE CAR (SimTools/LabelManInnerLifeDirective.md §3.9, Phase 1c).
//
// The night of 31 December 1959 took the car in every run. Until he has wheels again the Label Man gets about by
//   BUS  (default): cheap and slow. A depot wait on every intercity leg, 42 mph, no overtime (the last bus leaves at the
//        end of the working day), a half-hour lost on every errand in town, and only two cartons to hand out in another town;
//   TAXI: fast and dear. Short hops only out of town (no cabbie goes past TaxiMaxMiles), a dollar-fifty an errand in
//        town, three cartons;
//   A CAR, bought (a used sedan or a new one) or got back from whoever's holding it (§3.5): the old unlimited trunk.
//
// Player-only. A desk with no lien (every headless audit, every pre-lien save) keeps the car and never reaches any of this,
// so nothing here can move the AI economy. Every figure is provisional until Phase 0b measures it against the player's cash:
// a first pressing is about $1.3k and the founders open with $800-1,400, so a used car is a real choice.
// ============================================================================================
public partial class PlayerDesk {
	// Ordinals are saved.
	public enum CarKind { Gone, Original, Used, New }
	public enum TransportMode { Car, Bus, Taxi }

	/// <summary>Copies in a carton: what a bus rider can carry out to another town is counted in these.</summary>
	public const int CartonCopies = 50;
	public const int BusCartons = 2;
	public const int TaxiCartons = 3;

	public const float UsedCarPrice = 750f;     // a 1955-57 sedan: a 1958 dealer ad has a '55 Chevrolet at $1,145, so about $500-1,100 by 1960
	public const float NewCarPrice = 2000f;
	private const float UsedCarMph = 58f, UsedCarPerMile = 0.025f;   // the used car is slower and thirstier than the old one
	private const float BusMph = 42f, BusFarePerMile = 0.025f;       // Greyhound, stops included
	private const int BusDepotHours = 1;                             // waiting for the bus
	private const float TaxiMph = 60f, TaxiPerMile = 0.22f;
	/// <summary>No cabbie drives farther than this out of town.</summary>
	public const float TaxiMaxMiles = 75f;
	private const int BusErrandMinutes = 30;       // walking, waiting and riding, folded into one errand
	private const float BusErrandFare = 0.30f;     // a dime-and-a-nickel each way
	private const float TaxiErrandFare = 1.50f;
	public const int CarDealingHours = ActionCosts.QuickMeeting;   // 2 -- the lot, the paper, the keys

	// ── what he's got ────────────────────────────────────────────────────────────────────────────────

	/// <summary>The wheels he has. A desk with no lien still has the old car.</summary>
	public CarKind CarOwned => lien == null ? CarKind.Original : lien.Car;
	public bool HasCar => CarOwned != CarKind.Gone;
	public bool CarRecovered => lien == null || lien.CarRecovered;

	/// <summary>How he gets between towns right now: the car if he has one, otherwise the bus or the cab he picked.</summary>
	public TransportMode ActiveMode => HasCar ? TransportMode.Car : lien.Mode == TransportMode.Taxi ? TransportMode.Taxi : TransportMode.Bus;

	/// <summary>Bus or cab, when there is no car. Only from the office: you can't swap a bus for a cab on the road.</summary>
	public bool SetTransportMode(TransportMode mode, out string message) {
		if (lien == null || HasCar) { message = "You've got a car."; return false; }
		if (mode == TransportMode.Car) { message = "You haven't got a car."; return false; }
		if (!AtHome) { message = "Pick the cab or the bus at the office, before you set out."; return false; }
		lien.Mode = mode;
		message = mode == TransportMode.Taxi ? "Cabs from now on: quick, and every ride costs money." : "The bus from now on: cheap, and it keeps its own hours.";
		Changed?.Invoke();
		return true;
	}

	// ── the road ─────────────────────────────────────────────────────────────────────────────────────

	/// <summary>One-way hours and cost (gas, or the fare) between two towns in the mode he's using.</summary>
	public (int Hours, float Cost) TravelQuote(string fromCityId, string toCityId) => TravelQuote(fromCityId, toCityId, ActiveMode);

	public (int Hours, float Cost) TravelQuote(string fromCityId, string toCityId, TransportMode mode) {
		float miles = DistanceModel.GetRoadMilesBetween(fromCityId, toCityId);
		switch (mode) {
			case TransportMode.Bus:
				return (Mathf.Max(1, Mathf.RoundToInt(miles / BusMph)) + BusDepotHours, Mathf.Max(1f, Mathf.Round(miles * BusFarePerMile)));
			case TransportMode.Taxi:
				return (Mathf.Max(1, Mathf.RoundToInt(miles / TaxiMph)), Mathf.Max(1f, Mathf.Round(miles * TaxiPerMile)));
			default:
				if (lien != null && lien.Car == CarKind.Used)
					return (Mathf.Max(1, Mathf.RoundToInt(miles / UsedCarMph)), Mathf.Round(miles * UsedCarPerMile));
				return DriveQuote(fromCityId, toCityId);
		}
	}

	/// <summary>Why this leg can't be made in this mode, or null. Only the cab has a range.</summary>
	private string TravelRefusal(string fromCityId, string toCityId, TransportMode mode) {
		if (mode != TransportMode.Taxi) return null;
		float miles = DistanceModel.GetRoadMilesBetween(fromCityId, toCityId);
		return miles <= TaxiMaxMiles ? null : $"No cabbie will take you {miles:N0} miles out of town -- that's a bus, or a car of your own.";
	}

	private static string TravelVerb(TransportMode mode) => mode switch { TransportMode.Bus => "Took the bus to", TransportMode.Taxi => "Took a cab to", _ => "Drove to" };
	private static string TravelCostWord(TransportMode mode) => mode == TransportMode.Car ? "gas" : "fare";

	// ── errands in town ──────────────────────────────────────────────────────────────────────────────

	/// <summary>What getting to one errand in town costs: minutes lost and fare. Nothing with a car.</summary>
	public (int Minutes, float Fare) ErrandTransit => lien == null || HasCar ? (0, 0f)
		: ActiveMode == TransportMode.Taxi ? (0, TaxiErrandFare) : (BusErrandMinutes, BusErrandFare);

	/// <summary><see cref="Require"/> for an errand out of the office: the hours, the half-hour on the bus, and the fare if he can pay it.</summary>
	private bool RequireErrand(int hours, out string message) {
		if (!Require(hours, out message)) return false;
		var (minutes, fare) = ErrandTransit;
		if (minutes > 0 && !(TimeManager.Instance?.CanAffordMinutes(hours * 60 + minutes) ?? false)) {
			message = $"This takes {hours}h and the bus eats another {minutes} minutes -- it won't fit before the {TimeManager.Instance.HardStopTime} hard stop. It will be available tomorrow.";
			return false;
		}
		if (fare > 0f && Label.cashReserves < fare) {
			message = $"The cab fare is ${fare:N2} and you haven't got it -- take the bus.";
			return false;
		}
		message = string.Empty;
		return true;
	}

	/// <summary><see cref="Spend"/> for an errand: the hours, then the bus's half-hour and the fare.</summary>
	private void SpendErrand(int hours) {
		Spend(hours);
		var (minutes, fare) = ErrandTransit;
		if (minutes > 0) SpendMinutes(minutes);
		if (fare > 0f) { Label.cashReserves -= fare; Label.monthlyExpenses += fare; }
	}

	// ── what he can carry ────────────────────────────────────────────────────────────────────────────

	/// <summary>True when he's out of his home town with no car: the copies he can hand out are the cartons he carried.</summary>
	public bool CarryLimited => lien != null && !HasCar && !AtHome;

	public int CarryLimit => !CarryLimited ? int.MaxValue : (ActiveMode == TransportMode.Taxi ? TaxiCartons : BusCartons) * CartonCopies;

	/// <summary>Copies he can still place on this trip. int.MaxValue when nothing limits him.</summary>
	public int CarryLeft => !CarryLimited ? int.MaxValue : Math.Max(0, CarryLimit - lien.TripCopies);

	private void NoteCarried(int copies) { if (CarryLimited && copies > 0) lien.TripCopies += copies; }

	private string CarryEmptyMessage() =>
		$"You've handed out everything you could carry on this trip ({CarryLimit} copies) -- head home for more, or get a car.";

	// ── buying a car ─────────────────────────────────────────────────────────────────────────────────

	public float CarPrice(bool fresh) => fresh ? NewCarPrice : UsedCarPrice;

	public bool CanBuyCar(bool fresh, out string message) {
		if (lien == null) { message = "You've already got a car."; return false; }
		if (lien.Car == CarKind.New) { message = "You've already got a new car."; return false; }
		if (!fresh && lien.Car != CarKind.Gone) { message = "You've already got a car."; return false; }
		if (!AtHome) { message = "Cars are bought at the lot in town: get back to the office first."; return false; }
		float price = CarPrice(fresh);
		if (Label.cashReserves < price) { message = $"You're ${price - Label.cashReserves:N0} short of the ${price:N0}."; return false; }
		return Require(CarDealingHours, out message);
	}

	public bool BuyCar(bool fresh, out string message) {
		if (!CanBuyCar(fresh, out message)) return false;
		float price = CarPrice(fresh);
		Spend(CarDealingHours);
		Label.cashReserves -= price;
		Label.monthlyExpenses += price;
		lien.Car = fresh ? CarKind.New : CarKind.Used;
		lien.TripCopies = 0;
		Note(fresh ? $"Bought a new car for ${price:N0}." : $"Bought a used sedan for ${price:N0}.");
		message = fresh ? $"A new car, ${price:N0}. The trunk is yours again." : $"A used sedan, ${price:N0}. It'll get you there, slower.";
		Changed?.Invoke();
		return true;
	}

	// ── getting yours back (§3.9) ────────────────────────────────────────────────────────────────────

	/// <summary>Where the lost car is, and what it takes to get it back.</summary>
	public sealed record CarHunt(string Where, string Read, float? Price, bool Available, string Blocked);

	private const float SawyerCarPrice = 1100f;       // "high": Dee-Dee knows what it's worth to you
	private const float KubiakCarPrice = 700f, KubiakCarBadYearPrice = 450f;
	private const int KubiakBadYear = 1964;           // his route is failing by mid-decade (§3.5 H2)
	private const float PoundTow = 60f, PoundPerMonth = 20f;
	private const int PoundAuctionMonths = 12;
	private const float FloriaCarPrice = 800f;
	private const int FloriaLotYear = 1962;           // the lot's ad turns up in the paper, for a man with no Street to hear of it sooner
	private const float GermainStorage = 120f;
	private const int GermainManagerGoneYear = 1964;
	public const int CarKnowledgeBar = 3;

	private int InstinctOf(ExecutiveVoice voice) => Score(InstinctProfile, voice);

	/// <summary>The months the car has sat in the pound, counted from the night it was towed.</summary>
	private int PoundMonths() {
		if (lien == null) return 0;
		var today = LienToday;
		return Math.Max(0, (today.year - lien.Signed.year) * 12 + today.month - lien.Signed.month);
	}

	/// <summary>The car's whereabouts under this holder, or null when the old car is back (or there was never a lien).</summary>
	public CarHunt CarWhereabouts() {
		if (lien == null || lien.CarRecovered) return null;
		int year = LienToday.year;
		switch (lien.Holder) {
			case LienHolder.Sawyer:
				return new CarHunt("Behind the room. Dee-Dee drives it.", "Dee-Dee knows exactly what it's worth to you.", SawyerCarPrice, true, null);
			case LienHolder.Kubiak: {
				bool bad = year >= KubiakBadYear;
				float price = bad ? KubiakCarBadYearPrice : KubiakCarPrice;
				return new CarHunt("Kubiak's yard, \"held as security\".",
					bad ? "His route is failing and he sells anything. The price has come down." : "He sells anything, but not cheap in a good year.", price, true, null);
			}
			case LienHolder.Winters: {
				int months = PoundMonths();
				if (months >= PoundAuctionMonths)
					return new CarHunt("The city pound sold it at auction.", "A year is as long as they keep a car. It's gone.", null, false, "It was auctioned. You'll have to buy another.");
				float fee = PoundTow + PoundPerMonth * months;
				bool reads = InstinctOf(ExecutiveVoice.Suit) >= CarKnowledgeBar;
				return new CarHunt("The city pound: towed from outside the DJ's house.",
					reads ? $"The Suit reads the schedule: ${PoundTow:N0} tow and ${PoundPerMonth:N0} a month. ${fee:N0} today, and it's auctioned after {PoundAuctionMonths} months."
						: "Whatever the fee is, it grows every month, and after a year they sell the car.",
					reads ? fee : null, true, null);
			}
			case LienHolder.Floria: {
				bool finds = InstinctOf(ExecutiveVoice.Street) >= CarKnowledgeBar || year >= FloriaLotYear;
				if (!finds)
					return new CarHunt("With the hotel valet, once. The garage sold it unclaimed.", "You don't know which lot it ended up on.", null, false,
						"You'd have to hear where it went: the Street would, or the lot will advertise it in a year or two.");
				return new CarHunt("A used-car lot on the edge of town.", InstinctOf(ExecutiveVoice.Street) >= CarKnowledgeBar
					? "The Street knew the lot before you'd finished asking." : "The lot has put an advertisement in the paper.", FloriaCarPrice, true, null);
			}
			default: {
				bool fixes = InstinctOf(ExecutiveVoice.Fixer) >= CarKnowledgeBar;
				if (fixes)
					return new CarHunt("The station lot. The manager won't release it to anybody connected to Jack.",
						"The Fixer makes one phone call and proves you aren't. The manager hands over the keys.", 0f, true, null);
				if (year >= GermainManagerGoneYear)
					return new CarHunt("The station lot.", "The manager who wouldn't release it has moved on. The new one wants the storage.", GermainStorage, true, null);
				return new CarHunt("The station lot. The manager won't release it to anybody connected to Jack.", "He'll talk to you when he's gone, or when someone vouches for you.", null, false,
					"The manager won't release it to anyone connected to Jack. A Fixer could talk him round; so could time.");
			}
		}
	}

	public bool BuyBackCar(out string message) {
		if (lien == null) { message = "You've already got your car."; return false; }
		var hunt = CarWhereabouts();
		if (hunt == null) { message = "You've already got your car back."; return false; }
		if (!hunt.Available) { message = hunt.Blocked; return false; }
		if (!AtHome) { message = "Every place it could be is in town: get back to the office first."; return false; }
		float price = hunt.Price ?? (PoundTow + PoundPerMonth * PoundMonths());   // you pay what the window says, read or not
		if (Label.cashReserves < price) { message = $"You're ${price - Label.cashReserves:N0} short of the ${price:N0} it takes."; return false; }
		if (!Require(CarDealingHours, out message)) return false;
		Spend(CarDealingHours);
		if (price > 0f) { Label.cashReserves -= price; Label.monthlyExpenses += price; }
		lien.CarRecovered = true;
		if (lien.Car != CarKind.New) lien.Car = CarKind.Original;
		lien.TripCopies = 0;
		Note(price > 0f ? $"Got your own car back for ${price:N0}." : "Got your own car back.");
		message = price > 0f ? $"Your own car, back for ${price:N0}." : "Your own car, back, for the price of a phone call.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Checks only: the car a lien-less desk or a new lien has.</summary>
	internal void SetCarForChecks(CarKind kind) { if (lien != null) lien.Car = kind; }
}
