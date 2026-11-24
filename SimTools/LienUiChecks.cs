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
	private async Task Screenshot(string name) {
		if (DisplayServer.GetName() == "headless") return;
		// The lawyer's lines fade in one by one and the stamp comes down late: let the stage settle first.
		await ToSignal(GetTree().CreateTimer(3.0), Timer.SignalName.Timeout);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/lien-ui-" + name + ".png"));
	}

	public override async void _Ready() {
		try {
			await Frame();
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			var sawyer = new PartyMemory { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable };
			Check(desk.FoundLabel("Lien UI Checks", "detroit", FoundingArchetype.ExMusician, null, sawyer, out string message), "Found: " + message);
			Check(desk.Lien?.Holder == LienHolder.Sawyer && desk.LienMorningPending, "Sawyer holds the paper and the morning is pending");

			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			panel.OpenAtTab("OFFICE"); await Frame();

			// The morning after: the voices argue about the night, once.
			Check(Shows("THE MORNING AFTER") && Shows("A card game"), "The morning-after modal shows the voices' theories");
			await Screenshot("morning");
			await Press("GET TO WORK");
			Check(desk.Lien.OpeningSeen && !desk.LienMorningPending, "GET TO WORK dismisses the morning for good");
			panel.OpenAtTab("OFFICE"); await Frame();
			Check(!Shows("THE MORNING AFTER"), "The morning doesn't come back");

			// THE PAPER, before any sitting: the holder isn't named yet, the lawyer is.
			Check(Shows("THE PAPER") && Nodes(panel).OfType<LedgerTable>().Any(t => t.IsVisibleInTree()), "THE PAPER heading and its ledger are on the OFFICE tab");
			Check(Shows("Mr. Halloran sits down with you from"), "Before December the paper says when the sitting opens");
			await Screenshot("paper");

			// December 1960: the sitting window is open, NEXT UP points at it, the OFFICE tab wears a badge.
			desk.Label.cashReserves = 5000f;
			clock.RestoreClock(new GameDate(1960, 12, 20), 10);
			Check(desk.LienSittingDue && PlayerDeskPanel.NextUpHint(desk).Contains("Mr. Halloran"), "NEXT UP points at the sitting");
			panel.OpenAtTab("OFFICE"); await Frame();
			await Press("SIT DOWN WITH MR. HALLORAN");
			Check(desk.ActiveSitting != null && Shows("MR. HALLORAN, FOR DEE-DEE SAWYER"), "The sitting takes over the screen");
			Check(Shows("The December Sitting") && Shows("SITTING 1 OF 10"), "The title plate names the sitting and where it falls in the decade");
			Check(Nodes(panel).OfType<LienDecadeStrip>().Any(s => s.IsVisibleInTree()), "The ten Decembers are on the plate");
			Check(Nodes(panel).OfType<LienStamp>().Any(s => s.IsVisibleInTree()), "The lien is a stamped document");
			Check(Shows("Mr. Sawyer sends his regards"), "The lawyer's opening is in the transcript");
			Check(Shows("WHAT DO YOU OFFER HIM") && Shows("HIS ASK"), "The three bars are cards and he marks the one he stresses");
			await Screenshot("sitting");
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
			await Press("SIT DOWN WITH MR. HALLORAN");
			await Screenshot("reckoning");
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
}
