using Godot;
using System.Linq;

public partial class PlayerDeskPanel {
	private void DrawBooksLinks() {
		var links = new HBoxContainer();
		links.AddChild(ChoiceButton("FINANCES", currentTab == 4, () => GoToTab(4, PageFinances)));
		links.AddChild(ChoiceButton("LEDGER", currentTab == LedgerTab, () => GoToTab(LedgerTab, PageLedger)));
		content.AddChild(links);
	}

	private void DrawRouteStock(PlayerDesk desk) {
		var stock = desk.PressedSinglesOnHand().ToList();
		if (stock.Count == 0) Body("No sellable singles on hand.");
		else content.AddChild(new LedgerTable().Set("Stock for the shops", new[] { "Single", "Copies on hand" },
			stock.Select(single => LedgerTable.Entry(single.Title, single.OnHand.ToString("N0")))));
	}

	private void DrawTownShopSummary(PlayerDesk desk) {
		var shops = desk.StopsInCity(desk.CurrentCityId).Where(stop => stop.Kind == PlayerDesk.StopKind.Shop)
			.OrderBy(stop => stop.DisplayName).ToArray();
		if (shops.Length == 0) return;
		SectionHeading("local-shop-summary", $"SHOP ACCOUNTS · {desk.CurrentCity?.name}", false);
		content.AddChild(new LedgerTable().Set(null, new[] { "Shop", "On shelf", "Balance", "Clerk" },
			shops.Select(shop => LedgerTable.Entry(shop.DisplayName, shop.OnHand.Values.Sum(lot => lot.Remaining).ToString("N0"),
				LedgerTable.Money(shop.OpenBalance), PlayerDesk.KnowsRecordShop(shop) ? "Met" : "—"))));
		content = contentRoot;
	}

}
