using System;
using Godot;

// Getting around without the car (inner-life directive §3.9). A block at the top of the travel section: how he's getting about,
// the car's whereabouts, and the one-click ways to get wheels. Hidden for a desk that has its car.
public partial class PlayerDeskPanel {
	/// <summary>The three words the travel list changes with the way he's going: the button, the noun and what the cost is called.</summary>
	private static (string Button, string Noun, string Cost) TravelWords(PlayerDesk desk) => desk.ActiveMode switch {
		PlayerDesk.TransportMode.Bus => ("BUS TO", "by bus", "fare"),
		PlayerDesk.TransportMode.Taxi => ("CAB TO", "by cab", "fare"),
		_ => ("DRIVE TO", "drive", "gas"),
	};

	private void GettingAroundBlock(PlayerDesk desk) {
		if (desk.Lien == null || desk.HasCar && desk.CarRecovered) return;
		string have = desk.CarOwned switch {
			PlayerDesk.CarKind.Used => "A used sedan, bought. Slower and thirstier than the one you lost.",
			PlayerDesk.CarKind.New => "A new car, bought.",
			PlayerDesk.CarKind.Original => "Your own car.",
			_ => desk.ActiveMode == PlayerDesk.TransportMode.Taxi
				? $"No car. Cabs: quick, ${PlayerDesk.TaxiMaxMiles:N0} miles at most out of town, and every errand costs a fare."
				: "No car. The bus: cheap, slow, the last one leaves when the working day ends, and only " +
				  $"{PlayerDesk.BusCartons * PlayerDesk.CartonCopies} copies a trip to hand out in another town.",
		};
		Body("GETTING AROUND\n" + have);

		if (!desk.HasCar) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			var bus = Btn("TAKE THE BUS");
			var cab = Btn("TAKE CABS");
			bus.Disabled = desk.ActiveMode == PlayerDesk.TransportMode.Bus;
			cab.Disabled = desk.ActiveMode == PlayerDesk.TransportMode.Taxi;
			bus.TooltipText = "Fares run about 2½¢ a mile, a half-hour a stop in town, and the last bus goes when the working day ends.";
			cab.TooltipText = "Quick, and a dollar-fifty an errand in town. Short hops only out of it.";
			bus.Pressed += () => Act(() => { bool done = desk.SetTransportMode(PlayerDesk.TransportMode.Bus, out string m); Say(m, done); return true; });
			cab.Pressed += () => Act(() => { bool done = desk.SetTransportMode(PlayerDesk.TransportMode.Taxi, out string m); Say(m, done); return true; });
			if (!desk.AtHome) { bus.Disabled = true; cab.Disabled = true; bus.TooltipText = cab.TooltipText = "Pick the bus or the cabs at the office, before you set out."; }
			row.AddChild(bus);
			row.AddChild(cab);
			content.AddChild(row);
			if (desk.CarryLimited) Body($"On this trip: {desk.CarryLeft:N0} of {desk.CarryLimit:N0} copies still in your hands.");
		}

		var hunt = desk.CarWhereabouts();
		if (hunt != null) {
			Body($"YOUR CAR\n{hunt.Where} {hunt.Read}" + (hunt.Available ? "" : $"\n{hunt.Blocked}"));
			if (hunt.Available) {
				var back = Btn(hunt.Price is float p && p <= 0f ? "GET YOUR CAR BACK" : hunt.Price is float q ? $"BUY YOUR CAR BACK  (${q:N0})" : "GO AND PAY THE POUND");
				back.TooltipText = $"{PlayerDesk.CarDealingHours}h. " + (hunt.Price == null ? "You pay whatever the window says." : "Everything it takes is in town.");
				back.Pressed += () => Act(() => { bool done = desk.BuyBackCar(out string m); Say(m, done); return true; });
				if (!desk.AtHome) { back.Disabled = true; back.TooltipText = "Every place it could be is in town: get back to the office first."; }
				content.AddChild(back);
			}
		}

		if (desk.Lien != null && desk.CarOwned != PlayerDesk.CarKind.New) {
			var buy = new HBoxContainer();
			buy.AddThemeConstantOverride("separation", 8);
			if (desk.CarOwned == PlayerDesk.CarKind.Gone) {
				var used = Btn($"BUY A USED SEDAN  (${PlayerDesk.UsedCarPrice:N0})");
				used.TooltipText = $"A 1955-57 sedan off the lot. {PlayerDesk.CarDealingHours}h.";
				bool ok = desk.CanBuyCar(false, out string why);
				used.Disabled = !ok; if (!ok) used.TooltipText = why;
				used.Pressed += () => Act(() => { bool done = desk.BuyCar(false, out string m); Say(m, done); return true; });
				buy.AddChild(used);
			}
			var fresh = Btn($"BUY A NEW CAR  (${PlayerDesk.NewCarPrice:N0})");
			fresh.TooltipText = $"Off the showroom floor. {PlayerDesk.CarDealingHours}h.";
			bool freshOk = desk.CanBuyCar(true, out string freshWhy);
			fresh.Disabled = !freshOk; if (!freshOk) fresh.TooltipText = freshWhy;
			fresh.Pressed += () => Act(() => { bool done = desk.BuyCar(true, out string m); Say(m, done); return true; });
			buy.AddChild(fresh);
			content.AddChild(buy);
		}
	}
}
