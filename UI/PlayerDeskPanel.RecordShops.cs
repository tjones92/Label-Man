using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
	private string selectedTownShopSingleId;

	private void DrawRecordShop(PlayerDesk desk, PlayerDesk.PlayerStop shop) {
		Body(PlayerDesk.ShopWindow(shop));
		if (PlayerDesk.KnowsRecordShop(shop)) {
			Body(PlayerDesk.ShopClerk(shop));
			Body($"Clerk relationship {shop.Relationship:P0}.");
		}
		else {
			var visit = Btn("MEET THE CLERK (1h)");
			visit.TooltipText = "Introduce the label and ask which records people come in for. Local fares and travel time apply.";
			visit.Pressed += () => {
				bool ok = desk.VisitRecordShop(shop.StopId, out string message);
				if (ok) message += " " + PlayerDesk.ShopClerk(shop);
				Say(message, ok);
				RefreshKeepingScroll();
			};
			content.AddChild(visit);
		}

		if (desk.PreOrderNote(shop.StopId) is { } preorder) Body(preorder);
		int shelved = shop.OnHand.Values.Sum(lot => lot.Remaining);
		if (shelved > 0) Body($"{Plural.Of(shelved, "copy")} of yours still on the shelf.");
		if (shop.OpenBalance > 0.5f) Body($"They’re holding {Money(shop.OpenBalance)} for you.");

		foreach (var master in desk.Masters.Where(m => !m.Released && m.Record != null && desk.AcetatesFor(m.Record.recordId) > 0)) {
			string recordId = master.Record.recordId;
			if (desk.ShopRecordRead(shop, recordId) is { } read) Body($"“{master.SongTitle}” — {read}");
			content.AddChild(AcetateCounterButton(desk, shop, recordId, $"PLAY ‘{master.SongTitle}’"));
		}

		var onHand = desk.PressedSinglesOnHand().ToList();
		if (onHand.Count == 0 && !desk.HasRunner) {
			Body("No sellable copies on hand.");
			return;
		}
		OptionButton singlePick = null;
		if (onHand.Count > 0) {
			singlePick = Option();
			foreach (var single in onHand) singlePick.AddItem($"“{single.Title}” — {single.OnHand:N0} on hand");
			int selected = onHand.FindIndex(single => single.RecordId == selectedTownShopSingleId);
			singlePick.Select(Mathf.Max(0, selected));
			singlePick.ItemSelected += index => {
				selectedTownShopSingleId = onHand[(int)index].RecordId;
				RefreshKeepingScroll();
			};
			content.AddChild(singlePick);
			if (desk.ShopRecordRead(shop, onHand[singlePick.Selected].RecordId) is { } read) Body(read);
		}
		content.AddChild(StopSlip(desk, shop, shop.Kind, onHand, singlePick, desk.HasWorkedStopToday(shop.StopId)));
	}

	private Button AcetateCounterButton(PlayerDesk desk, PlayerDesk.PlayerStop stop, string recordId, string caption) {
		bool passed = stop.PassedRecordIds.Contains(recordId), holding = desk.HasOpenCall(stop.StopId, recordId);
		bool worked = desk.HasWorkedStopToday(stop.StopId), masterOut = desk.IsMasterOut(recordId);
		var play = Btn($"{caption}{(holding ? " — holding" : passed ? " — passed" : worked ? " — tomorrow" : $" ({PlayerDesk.StopVisitEstimate(stop.Kind)})")}");
		play.Disabled = holding || passed || worked || masterOut;
		play.TooltipText = holding ? "Already holding copies for the pressing."
			: passed ? "They heard it and passed. New demand can bring them back."
			: worked ? "You've worked this counter today. Come back tomorrow."
			: masterOut ? "The master is out. You can pitch it when it comes back."
			: "Play the acetate and ask them to hold copies.";
		play.Pressed += () => {
			bool ok = desk.PitchAcetateAtStop(stop.StopId, recordId, out string message);
			Say(message, ok);
			RefreshKeepingScroll();
		};
		return play;
	}
}
