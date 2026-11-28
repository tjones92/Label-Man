using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life Phase 1 UI: drives the real desk through the morning-after modal, THE PAPER on the OFFICE tab, a December
/// 1960 sitting paid off, and a 1970 reckoning that hands over the keys and lands on the game-over page with the paper's account.
///   --path . --resolution 1600x902 SimTools/LienUiChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Screenshots go to SimLogs/lien-ui-*.png when not headless. Prints LIEN_UI_CHECKS_PASS / _FAIL.
/// </summary>
public partial class LienUiChecks : Node {
	private PlayerDeskPanel panel;
	private int checks;

	private IEnumerable<Node> Nodes(Node root) {
		if (root.IsQueuedForDeletion()) yield break;
		yield return root;
		foreach (Node child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
	}
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private bool Shows(string text) => Nodes(panel).OfType<Label>().Any(l => l.IsVisibleInTree() && l.Text.Contains(text, StringComparison.Ordinal));
	private async Task Frame() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree().CreateTimer(0.05), Timer.SignalName.Timeout);
	}
	private async Task Press(string text) {
		var button = Nodes(panel).OfType<Button>().Where(b => b.IsVisibleInTree()).FirstOrDefault(b => b.Text.StartsWith(text, StringComparison.Ordinal))
			?? throw new InvalidOperationException("No button: " + text);
		Check(!button.Disabled, "Enabled: " + text);
		button.EmitSignal(Button.SignalName.Pressed);
		await Frame();
	}
	private bool ShowsRich(string text) => Nodes(panel).OfType<RichTextLabel>().Any(l => l.IsVisibleInTree() && l.Text.Contains(text, StringComparison.Ordinal));
	private async Task Screenshot(string name, double settle = 3.0) {
		if (DisplayServer.GetName() == "headless") return;
		// The lawyer's lines fade in one by one and the stamp comes down late: let the stage settle first.
		await ToSignal(GetTree().CreateTimer(settle), Timer.SignalName.Timeout);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/lien-ui-" + name + ".png"));
	}

	public override async void _Ready() {
		try {
			await Frame();
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			if (OS.GetCmdlineUserArgs().Contains("--floria-ui")) {
				await FloriaPurchase(desk, clock);
				GD.Print($"FLORIA_UI_CHECKS_PASS ({checks} checks)");
				GetTree().Quit(0);
				return;
			}
			if (OS.GetCmdlineUserArgs().Contains("--germain-ui")) {
				await GermainTrailWithoutStock(desk, clock);
				GD.Print($"GERMAIN_UI_CHECKS_PASS ({checks} checks)");
				GetTree().Quit(0);
				return;
			}
			var sawyer = new PartyMemory { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable };
			Check(desk.FoundLabel("Lien UI Checks", "detroit", FoundingArchetype.ExMusician, null, sawyer, out string message), "Found: " + message);
			Check(desk.Lien?.Holder == LienHolder.Sawyer && desk.LienMorningPending, "Sawyer holds the paper and the morning is pending");

			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			panel.OpenAtTab("OFFICE"); await Frame();

			// Onboarding T1: a knock at the door first. Connie, who holds her paper, what he wants; then the letter.
			Check(Shows("A Knock at the Door") && !Shows("The Morning After"), "The first morning opens on the knock, before the letter");
			Check(Shows("Connie Boursaw") && Shows("WHERE YOU STAND") && Shows("Dee-Dee Sawyer holds your paper"), "Connie comes to the door, and the slip says whose paper it is");
			await Screenshot("first-morning", 0.3 + 0.9 * 12);
			var read = Nodes(panel).OfType<Button>().Single(b => b.IsVisibleInTree() && b.Text == "READ THE LETTER");
			read.EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(!Shows("A Knock at the Door") && desk.LienMorningPending, "READ THE LETTER closes the knock and leaves the morning pending");

			// The morning after: the voices argue about the night, once.
			Check(Shows("The Morning After") && Shows("A card game"), "The morning-after scene shows the voices' theories");
			Check(Shows("supper club") && Shows("a kid you'd heard once") && Shows("There was a woman"), "The voices argue from what was remembered: where, the brag, and nobody for who");
			Check(ShowsRich("Your watch") && ShowsRich("pedal steel") && Shows("$800"), "The cigar box and the list of what's gone are on the page");
			Check(Shows("WHAT THE LETTER SAYS") && Nodes(panel).OfType<LienDecadeStrip>().Any(s => s.IsVisibleInTree()) && Nodes(panel).OfType<LienStamp>().Any(s => s.IsVisibleInTree()), "The terms, the ten Decembers and the stamped letter are there");
			await Screenshot("morning-haze", 0.6);
			await Screenshot("morning", 11.5);
			await Press("GET TO WORK");
			Check(desk.Lien.OpeningSeen && !desk.LienMorningPending, "GET TO WORK dismisses the morning for good");
			panel.OpenAtTab("OFFICE"); await Frame();
			Check(!Shows("The Morning After"), "The morning doesn't come back");
			Check(Shows("Gone since the night"), "The office keeps the list of what's gone");
			await Press("READ THE LETTER AGAIN");
			Check(Shows("The Morning After") && ShowsRich("pedal steel"), "The letter can be read again");
			await Screenshot("morning-again", 0.3);
			await Press("PUT IT AWAY");
			Check(!Shows("The Morning After") && desk.Lien.OpeningSeen, "Putting it away closes it and leaves the morning seen");

			// THE PAPER, before any sitting: Connie's contract has already named Sawyer (§6.1), and his lawyer.
			Check(Shows("THE PAPER") && Nodes(panel).OfType<LedgerTable>().Any(t => t.IsVisibleInTree()), "THE PAPER heading and its ledger are on the OFFICE tab");
			Check(desk.LienHolderIdentified && !desk.HolderShownHand, "Sawyer is named from the first morning, but not yet met");
			Check(Shows("Mr. Halloran sits down with you from"), "Before December the paper says when the sitting opens");
			await Screenshot("paper");

			// December 1960: the sitting window is open, NEXT UP points at it, the OFFICE tab wears a badge.
			desk.Label.cashReserves = 5000f;
			clock.RestoreClock(new GameDate(1960, 12, 20), 10);
			Check(desk.LienSittingDue && PlayerDeskPanel.NextUpHint(desk).Contains("Mr. Halloran"), "NEXT UP points at the sitting");
			panel.OpenAtTab("OFFICE"); await Frame();
			// The window has opened: the lawyer's letter is on the mat.
			Check(desk.LienNoticePending && Shows("I write on behalf of the holder") && Shows("You may not remember signing it"), "The lawyer's letter arrives when the window opens");
			await Screenshot("notice", 1.4);
			await Press("PUT IT ON THE DESK");
			Check(!desk.LienNoticePending && !Shows("I write on behalf of the holder") && desk.CaptureLienForChecks().NoticeKey == 196101, "Putting it on the desk reads it once, for this deadline, and it's saved");
			await Press("SIT DOWN WITH MR. HALLORAN");
			Check(desk.ActiveSitting != null && Shows("MR. HALLORAN, FOR DEE-DEE SAWYER"), "The sitting takes over the screen");
			if (DisplayServer.GetName() != "headless") {
				Check(Nodes(panel).OfType<LienDoor>().Any(), "The lawyer arrives at a door");
				await Screenshot("arrival-knock", 1.9);
				await Screenshot("arrival-door", 1.8);
			}
			Check(Shows("The December Sitting") && Shows("SITTING 1 OF 10"), "The title plate names the sitting and where it falls in the decade");
			Check(Nodes(panel).OfType<LienDecadeStrip>().Any(s => s.IsVisibleInTree()), "The ten Decembers are on the plate");
			Check(Nodes(panel).OfType<LienStamp>().Any(s => s.IsVisibleInTree()), "The lien is a stamped document");
			Check(Shows("Mr. Sawyer sends his regards"), "The lawyer's opening is in the transcript");
			Check(Shows("WHAT DO YOU OFFER HIM") && Shows("HIS ASK"), "The three bars are cards and he marks the one he stresses");
			await Screenshot("sitting", 5.0);
			panel.OpenAtTab("A&R"); await Frame();
			Check(Shows("MR. HALLORAN, FOR DEE-DEE SAWYER"), "The sitting holds the screen on another tab");
			// A concession that follows you to 1970 asks before it signs.
			float cashBefore = desk.Label.cashReserves, faceBefore = desk.Lien.FaceSum;
			await Press("Ask him to write it onto the paper");
			Check(Shows("follows you to 1970") && desk.Lien.FaceSum == faceBefore && !desk.Lien.Extended, "Writing it onto the paper asks first and signs nothing yet");
			await Screenshot("confirm");
			await Press("THINK AGAIN");
			Check(!Shows("follows you to 1970"), "THINK AGAIN takes it back");
			float before = desk.Label.cashReserves, bar = desk.LienPayBar();
			Check(Math.Abs(before - cashBefore) < 0.01f, "Nothing was spent");
			await Press("Pay him");
			Check(desk.Lien.Extended && desk.Lien.PendingDeadline?.year == 1962 && Math.Abs(desk.Label.cashReserves - (before - bar)) < 1f,
				$"Pay rolls the lien to 1962 and takes ${bar:N0}");
			Check(Shows("Extended.") && Shows("NEXT DECEMBER") && Nodes(panel).OfType<LedgerTable>().Any(t => t.IsVisibleInTree()), "The outcome page says what was entered and what next year will ask");
			await Screenshot("paid");
			await Press("SEE HIM OUT");
			Check(desk.ActiveSitting == null, "SEE HIM OUT ends the sitting");
			panel.OpenAtTab("OFFICE"); await Frame();
			Check(Shows("The lien runs to") && !desk.LienSittingDue, "After the sitting the paper names him and the new date");

			// The reckoning: hand over the keys, see the end of the transcript, then the game-over page with the paper's account.
			desk.Lien.Extended = false; desk.Lien.PendingDeadline = null;
			desk.Lien.Deadline = new GameDate(1970, 1, 1); desk.Lien.BarYear = 1969;
			desk.Label.cashReserves = 100f;
			clock.RestoreClock(new GameDate(1969, 12, 22), 10);
			panel.OpenAtTab("OFFICE"); await Frame();
			Check(Shows("the reckoning"), "In December 1969 the paper says it's the reckoning");
			Check(desk.LienNoticePending && Shows("THE LAST LETTER") && Shows("The paper ends on 1 January 1970"), "The reckoning has its own last letter");
			await Screenshot("notice-reckoning", 1.4);
			await Press("PUT IT ON THE DESK");
			await Press("SIT DOWN WITH MR. HALLORAN");
			await Screenshot("reckoning", 5.0);
			Check(Shows("The Reckoning"), "The plate says it's the reckoning");
			await Press("Hand him the keys");
			Check(!desk.IsGameOver && Shows("This ends the run"), "Handing over the keys asks first");
			await Press("THINK AGAIN");
			Check(!desk.IsGameOver && !Shows("This ends the run"), "THINK AGAIN leaves the keys where they are");
			await Press("Hand him the keys");
			await Press("GIVE HIM THE KEYS");
			Check(desk.IsGameOver && Shows("takes the keys"), "Handing over ends the run, and the transcript's last line still shows");
			await Press("THAT'S THE END OF IT");
			Check(Shows("TAKEN") && Nodes(panel).OfType<LedgerTable>().Any(t => t.IsVisibleInTree()), "The game-over page is titled TAKEN and carries the paper's account");
			await Screenshot("taken");

			GD.Print($"LIEN_UI_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("LIEN_UI_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private async Task GermainTrailWithoutStock(PlayerDesk desk, TimeManager clock) {
		var germain = new PartyMemory { Where = PartyWhere.DjHouseParty, Who = PartyWho.DiscJockey, Brag = PartyBrag.TheAct, End = PartyEnd.Car };
		Check(desk.FoundLabel("Germain UI Checks", "detroit", FoundingArchetype.TradeInsider, null, germain, out string message), "Founded: " + message);
		desk.DismissLienMorning();
		clock.RestoreClock(new GameDate(1960, 12, 20), 9);
		Check(desk.OpenLienSitting(out message), "Germain is identified: " + message);
		desk.CloseLienSitting();
		desk.Lien.Deadline = new GameDate(1965, 1, 1);
		desk.Lien.BarYear = 1964;
		clock.RestoreClock(new GameDate(1964, 6, 1), 9);
		AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
		await Frame();
		panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
		panel.OpenAtTab("OFFICE"); await Frame();
		Check(Shows("Start with Jack's old station"), "The office points to the station");
		Check(!desk.PressedSinglesOnHand().Any() && !desk.PromoSinglesOnHand().Any(), "No selling or promo stock is on hand");
		panel.OpenAtTab("THE ROAD"); await Frame();
		await Press("ASK ABOUT THE OLD 78s");
		await Press("LOOK THROUGH THE USED 78s");
		Check(desk.Lien.GermainLedger == GermainLedgerStage.Found, "The station and shop are reachable with no stock");
		panel.OpenAtTab("OFFICE"); await Frame();
		await Press("READ JACK'S ACCOUNT BOOK");
		Check(desk.GermainLedgerFrozen, "Reading the book at the office freezes the concessions");
	}

	private async Task FloriaPurchase(PlayerDesk desk, TimeManager clock) {
		var floria = new PartyMemory { Where = PartyWhere.HotelBallroom, Who = PartyWho.CamelCoat, Brag = PartyBrag.TheLabel, End = PartyEnd.PhoneCall };
		Check(desk.FoundLabel("Floria UI Checks", "detroit", FoundingArchetype.TradeInsider, null, floria, out var message), "Founded: " + message);
		desk.DismissLienMorning();
		desk.Lien.Deadline = new GameDate(1970, 1, 1);
		desk.Lien.BarYear = 1969;
		clock.RestoreClock(new GameDate(1969, 12, 20), 9);
		AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
		await Frame();
		panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
		panel.OpenAtTab("OFFICE"); await Frame();
		Check(Shows("PURCHASE OPTION"), "Lowe's letter warns about the purchase option");
		await Press("PUT IT ON THE DESK");
		await Press("SIT DOWN WITH MR. LOWE");
		Check(Shows("The Buyout") && Shows("PURCHASE OFFERS"), "The purchase takes over the sitting");
		await Press("Sign with");
		Check(!desk.IsGameOver && Shows("Signing transfers ownership"), "The sale asks for confirmation before transferring ownership");
		await Press("THINK AGAIN");
		Check(!desk.Label.IsSubsidiary && !desk.IsGameOver, "Cancelling leaves the label independent");
		await Press("Sign with");
		await Press("SIGN THE SALE");
		Check(desk.IsGameOver && desk.Label.IsSubsidiary && Shows("enforced the buyout"), "Signing transfers the label and shows the outcome");
		await Press("THAT'S THE END OF IT");
		Check(Shows("TAKEN") && Nodes(panel).OfType<LedgerTable>().Any(table => table.IsVisibleInTree()) &&
			desk.Lien.Ledger.Any(entry => entry.Kind == LienLedgerKind.FloriaBuyout && entry.Detail != null),
			"The game-over page carries the paper and the ledger records the sale");
	}
}
