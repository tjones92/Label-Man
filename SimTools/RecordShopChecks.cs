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
			await Press(panel, "GO OUT TO THE SHOPS");
			Check(desk.NowMinute == beforeOfficeHandoff, "Opening the route doesn't spend travel time");
			var map = Nodes(panel).OfType<CityShopMap>().Single();
			Check(map.GetChildren().OfType<Button>().Count() == shops.Count, "Every Detroit shop has a clickable pin");
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
			panel.OpenAtTab("THE ROAD");
			await Frame();
			await Press(panel, "PLAY ‘Counter Side’");
			Check(desk.HasOpenCall(shop.StopId, master.Record.recordId) || shop.PassedRecordIds.Contains(master.Record.recordId),
				"The selected counter takes an order or records a pass through the existing ledger");
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
