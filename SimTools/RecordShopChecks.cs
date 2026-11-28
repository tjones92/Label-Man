using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>Checks the handoff from Distribution to the shops and the first acetate pitch with no vinyl.</summary>
public partial class RecordShopChecks : Node {
	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			await Frame();
			var desk = PlayerDesk.Instance;
			Check(desk.FoundLabel("Record Shop Checks", "detroit", FoundingArchetype.PromoMan, out var message), message);
			desk.DismissLienMorning();
			var shops = desk.StopsInCity("detroit").Where(stop => stop.Kind == PlayerDesk.StopKind.Shop).ToList();
			Check(shops.Count(stop => !stop.Specialist) > shops.Count / 2 && shops.Any(stop => stop.Specialist),
				"The seeded city has broad-stock shops and a minority of specialists");
			Check(shops.Select(PlayerDesk.ShopWindow).Distinct().Count() > 1, "Storefront descriptions differ with shop size");
			Check(!desk.PressedSinglesOnHand().Any() && !desk.Masters.Any(), "The route opens before the first master or pressing");

			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			var panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			panel.OpenAtTab("DISTRIBUTION");
			await Frame();
			Check(!Nodes(panel).OfType<CityShopMap>().Any(), "The paperwork leaves shop visits to the road");
			int beforeOfficeHandoff = desk.NowMinute;
			await Press(panel, "OPEN CITY MAP");
			Check(desk.NowMinute == beforeOfficeHandoff, "Opening the route doesn't spend travel time");
			var map = Nodes(panel).OfType<CityShopMap>().Single();
			Check(map.GetChildren().OfType<Button>().Count(pin => pin.Name.ToString().StartsWith("ShopPin_")) == shops.Count,
				"Every Detroit shop has a clickable pin alongside the other destinations");
			Check(Shows(panel, "PLAYING TODAY") && !Shows(panel, "This week"), "The city map opens on today’s bill without the desk checklist");
			var inspector = (Control)panel.FindChild("CityMapInspector", true, false);
			float mapShare = map.Size.X / (map.Size.X + inspector.Size.X);
			Check(mapShare >= .60f && mapShare <= .70f, $"The map and inspector share the overlay ({mapShare:P0} map)");
			await Press(panel, "ACT NOTEBOOK");
			Check(Shows(panel, "A&R NOTEBOOK") && !Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text is "ROSTER" or "DISTRIBUTION" or "BOOKS"),
				"The notebook opens within the street overlay");
			await Press(panel, "‹ CITY MAP");
			await Press(panel, "RADIO");
			map = Nodes(panel).OfType<CityShopMap>().Single();
			Check(!map.GetChildren().OfType<Button>().Any(pin => pin.Name.ToString().StartsWith("ShopPin_")), "The radio filter hides shop pins");
			var station = desk.StopsInCity(desk.CurrentCityId).First(stop => stop.Kind == PlayerDesk.StopKind.Station);
			map.GetNode<Button>("TownPin_" + station.StopId).EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(Shows(panel, station.DisplayName) && Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text == "ASK WHAT'S ON THE SURVEY"),
				"A radio pin opens its existing station actions before a pressing");
			await Press(panel, "STUDIO & BAND ROOM");
			Nodes(panel).OfType<CityShopMap>().Single().GetNode<Button>("TownPin_studio").EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(Shows(panel, "No session booked") && Nodes(panel).OfType<Button>().Single(b => b.IsVisibleInTree() && b.Text == "TAKE INTO STUDIO").Disabled,
				"Studio explains why it is locked before the label has an act");
			await Press(panel, "RECORD SHOPS");
			map = Nodes(panel).OfType<CityShopMap>().Single();
			var shop = shops.First(stop => stop.Specialist);
			map.GetNode<Button>("ShopPin_" + shop.StopId).EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(Nodes(panel).OfType<Label>().Any(label => label.Text.Equals(shop.DisplayName, StringComparison.OrdinalIgnoreCase)),
				"Clicking the pin opens that shop’s counter");
			Check(!Shows(panel, PlayerDesk.ShopClerk(shop)), "The clerk’s read starts undiscovered");
			float cash = desk.Label.cashReserves;
			await Press(panel, "MEET THE CLERK");
			Check(PlayerDesk.KnowsRecordShop(shop) && Shows(panel, PlayerDesk.ShopClerk(shop)), "A visit reveals the clerk’s repertoire");
			Check(desk.Label.cashReserves < cash && !desk.VisitRecordShop(shop.StopId, out _), "The bus fare is charged and the introduction is one-time");
			Check(desk.CaptureState().StopState.Any(saved => saved.StopId == shop.StopId && saved.Relationship > 0f),
				"The introduction is captured by the existing stop save state");

			var act = ArtistManager.Instance.GetAllArtists().First(artist => artist.isActive && string.IsNullOrEmpty(artist.labelId));
			act.labelId = desk.Label.labelId;
			desk.Label.roster.Add(act);
			var master = desk.StageShelfMasterForChecks(act, "record_shop_check", "Counter Side", .80f, .80f);
			master.Record.primaryGenre = Genre.Jazz;
			var jazzShop = new PlayerDesk.PlayerStop { Kind = PlayerDesk.StopKind.Shop, Repertoire = PlayerDesk.ShopRepertoire.Jazz, Specialist = true, CityId = shop.CityId };
			var countryShop = new PlayerDesk.PlayerStop { Kind = PlayerDesk.StopKind.Shop, Repertoire = PlayerDesk.ShopRepertoire.Country, Specialist = true, CityId = shop.CityId };
			Check(desk.DebugAcceptChance(jazzShop, master.Record.recordId) > desk.DebugAcceptChance(countryShop, master.Record.recordId),
				"The same side has a better shot with a matching specialist");
			Check(desk.CutAcetate(master.Record.recordId, out message), message);
			await Frame();
			await Press(panel, "PLAY ‘Counter Side’");
			Check(desk.HasOpenCall(shop.StopId, master.Record.recordId) || shop.PassedRecordIds.Contains(master.Record.recordId),
				"The selected counter takes an order or records a pass through the existing ledger");
			await Press(panel, "STUDIO & BAND ROOM");
			Nodes(panel).OfType<CityShopMap>().Single().GetNode<Button>("TownPin_studio").EmitSignal(Button.SignalName.Pressed);
			await Frame();
			await Press(panel, "TAKE INTO STUDIO");
			if (desk.Roster.Count() > 1) await Press(panel, act.stageName);
			var studioWindow = Nodes(this).OfType<PlayerDeskPanel>().Single(window => window.Name == "StudioWindow");
			Check(studioWindow.Visible && !panel.Visible, "The studio pin opens the existing booking window");
			await Press(studioWindow, "‹ BACK TO CITY MAP");
			Check(panel.StreetOpen && Shows(panel, "No session booked"), "Closing the booking window returns to the selected studio pin");
			GD.Print("RECORD_SHOP_CHECKS_PASS");
			GetTree().Quit();
		} catch (Exception e) {
			GD.PrintErr("RECORD_SHOP_CHECKS_FAIL: " + e);
			GetTree().Quit(1);
		}
	}

	private static void Check(bool ok, string detail) {
		if (!ok) throw new InvalidOperationException(detail);
		GD.Print("ok: " + detail);
	}

	private async Task Frame() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private static IEnumerable<Node> Nodes(Node root) {
		if (root.IsQueuedForDeletion()) yield break;
		yield return root;
		foreach (Node child in root.GetChildren())
			foreach (var node in Nodes(child)) yield return node;
	}

	private static bool Shows(Node root, string text) => Nodes(root).OfType<Label>()
		.Any(label => label.IsVisibleInTree() && label.Text.Contains(text, StringComparison.Ordinal));

	private async Task Press(Node root, string text) {
		var button = Nodes(root).OfType<Button>().Single(b => b.IsVisibleInTree() && b.Text.StartsWith(text, StringComparison.Ordinal));
		Check(!button.Disabled, "Enabled: " + text);
		button.EmitSignal(Button.SignalName.Pressed);
		await Frame();
	}
}
