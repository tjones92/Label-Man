using Godot;
using System.Linq;

public partial class PlayerDeskPanel {
	internal void OpenSetFinder() {
		scoutingView = "FIND A SET";
		showScoutingItinerary = true;
		OpenAtTab("A&R");
	}

	internal void OpenScoutingNotebook() {
		scoutingView = "ACT NOTEBOOK";
		OpenAtTab("A&R");
	}

	internal void OpenDoorSet(ScoutingOpportunity set) {
		OpenSetFinder();
		var desk = PlayerDesk.Instance;
		ActUntilDay(set.Playing ? 0 : set.Day, () => {
			string message;
			bool ok = set.Playing
				? desk.ListenSceneAppearance(set.BillId, set.EventId, out message)
				: desk.WaitForSceneAppearance(set.BillId, set.EventId, out message);
			if (ok && set.Playing) OpenTheSet(desk);
			Say(message, ok);
			return ok;
		});
	}

	private void DrawBooksLinks() {
		var links = new HBoxContainer();
		links.AddChild(ChoiceButton("FINANCES", currentTab == 4, () => GoToTab(4, PageFinances)));
		links.AddChild(ChoiceButton("LEDGER", currentTab == LedgerTab, () => GoToTab(LedgerTab, PageLedger)));
		content.AddChild(links);
	}

	private void PageDeskPhone() {
		var contacts = Btn("OPEN THE ROLODEX");
		contacts.Pressed += () => GoToTab(RolodexTab, PageRolodex);
		content.AddChild(contacts);
		PhoneSection(PlayerDesk.Instance);
	}

	private void DrawRouteStock(PlayerDesk desk) {
		var stock = desk.PressedSinglesOnHand().ToList();
		if (stock.Count == 0) return;
		Heading("STOCK FOR THE SHOPS");
		var title = Option();
		foreach (var single in stock) title.AddItem($"“{single.Title}” — {single.OnHand:N0} on hand");
		title.Select(Mathf.Max(0, stock.FindIndex(single => single.RecordId == selectedTownShopSingleId)));
		selectedTownShopSingleId = stock[title.Selected].RecordId;
		title.ItemSelected += index => selectedTownShopSingleId = stock[(int)index].RecordId;
		content.AddChild(title);
	}

}
