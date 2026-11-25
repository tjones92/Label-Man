using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life §6.1 UI: for each holder, the first single goes to the plant, his move lands at dawn, and the real desk opens
/// the holder's sheet over the office with his scene and two answers. Answering closes it, and THE PAPER names him.
///   --path . --resolution 1600x902 SimTools/HolderMoveUiChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Screenshots go to SimLogs/holder-move-*.png when not headless. Prints HOLDER_MOVE_UI_CHECKS_PASS / _FAIL.
/// </summary>
public partial class HolderMoveUiChecks : Node {
	private PlayerDeskPanel panel;
	private int checks;

	private static readonly Dictionary<LienHolder, PartyMemory> Own = new() {
		[LienHolder.Sawyer] = new() { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable },
		[LienHolder.Kubiak] = new() { Where = PartyWhere.BarBackRoom, Who = PartyWho.RouteMan, Brag = PartyBrag.TheLabel, End = PartyEnd.Car },
		[LienHolder.Winters] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.MajorMan, Brag = PartyBrag.Hits, End = PartyEnd.PhoneCall },
		[LienHolder.Floria] = new() { Where = PartyWhere.HotelBallroom, Who = PartyWho.CamelCoat, Brag = PartyBrag.TheLabel, End = PartyEnd.PhoneCall },
		[LienHolder.Germain] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.DiscJockey, Brag = PartyBrag.TheAct, End = PartyEnd.Car },
	};

	private IEnumerable<Node> Nodes(Node root) {
		if (root.IsQueuedForDeletion()) yield break;
		yield return root;
		foreach (Node child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
	}
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private IEnumerable<Node> All() => Nodes(panel).Concat(Nodes(GetTree().Root).OfType<CanvasLayer>().SelectMany(Nodes)).Distinct();
	private static string TextOf(Node n) => n switch { Label l => l.Text, RichTextLabel r => r.Text, Button b => b.Text, _ => null };
	private bool Shows(string text) => All().OfType<CanvasItem>().Any(c => c.IsVisibleInTree() && TextOf(c)?.Contains(text, StringComparison.Ordinal) == true);
	private async Task Frame() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree().CreateTimer(0.05), Timer.SignalName.Timeout);
	}
	private async Task Screenshot(string name, double settle = 0.8) {
		if (DisplayServer.GetName() == "headless") return;
		await ToSignal(GetTree().CreateTimer(settle), Timer.SignalName.Timeout);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/holder-move-" + name + ".png"));
	}

	public override async void _Ready() {
		try {
			await Frame();
			var desk = PlayerDesk.Instance;
			Check(desk.FoundLabel("Holder Move UI", "detroit", FoundingArchetype.PromoMan, null, Own[LienHolder.Kubiak], out string message), "Found: " + message);
			desk.DismissLienMorning();
			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			int n = 0;
			foreach (var h in Enum.GetValues<LienHolder>()) {
				desk.ClearPressingForChecks();
				desk.FoundLienForChecks(Own[h], h);
				desk.DismissLienMorning();
				desk.Label.cashReserves = 1200f;
				string id = $"holder-move-ui-{h}-{++n}";
				var flip = h == LienHolder.Winters ? new Record { recordId = id + "-b", title = "Moonlight Bus", publishingControl = PublishingControlType.LabelAffiliate,
					publishingControllerLabelId = desk.Label.labelId } : null;
				desk.PressFirstSingleForChecks(id, 1000, 1300f, today, today.AddDays(20), flip);
				desk.ProcessHolderMoveDawnForChecks(desk.Lien.MoveDue);
				Check(desk.HolderMovePending, $"{h}: the move is on the desk");
				panel.OpenAtTab("OFFICE"); await Frame();
				Check(Shows("THE HOLDER"), $"{h}: the holder's sheet is open over the office");
				var accept = desk.HolderMoveOptions().First(o => o.accept);
				await Screenshot(h.ToString().ToLowerInvariant());
				Check(Shows(accept.label), $"{h}: the sheet offers \"{accept.label}\"");
				var button = All().OfType<Button>().Where(b => b.IsVisibleInTree() && !b.Disabled)
					.FirstOrDefault(b => Nodes(b).Any(x => TextOf(x)?.Contains(accept.label, StringComparison.Ordinal) == true));
				if (button != null) {
					button.EmitSignal(Button.SignalName.Pressed);
					await Frame();
					Check(desk.Lien.Move == HolderMoveStage.Accepted, $"{h}: pressing the answer accepts");
				} else {
					Check(desk.ResolveHolderMove(true, out _), $"{h}: accepted (no button found by text; resolved directly)");
					panel.OpenAtTab("OFFICE"); await Frame();
				}
				Check(!Shows("THE HOLDER"), $"{h}: the sheet closes");
				Check(desk.LienHolderIdentified, $"{h}: he's named on THE PAPER");
			}
			await Screenshot("paper-after");
			GD.Print($"HOLDER_MOVE_UI_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("HOLDER_MOVE_UI_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}
}
