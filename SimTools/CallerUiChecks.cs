using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// PlayerDesk.Callers in the real office: a shop's call rings while he waits at the desk, the receiver bounces in its cradle,
/// and a hotspot over the phone picks up. With the folder open the PICK UP button pulses beside the waits. Picking up shows
/// the caller's card and the receiver settles. A knock reads ANSWER THE DOOR. A note left in the door is tucked below the knob
/// and opens when clicked, and is gone after.
///   --path . --resolution 1600x900 SimTools/CallerUiChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Screenshots go to SimLogs/caller-ui-*.png when not headless. Prints CALLER_UI_CHECKS_PASS / _FAIL.
/// </summary>
public partial class CallerUiChecks : Node {
	private PlayerDeskPanel panel;
	private int checks;

	private IEnumerable<Node> Nodes(Node root) {
		if (root.IsQueuedForDeletion()) yield break;
		yield return root;
		foreach (Node child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
	}
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private IEnumerable<Node> All() => Nodes(GetTree().Root);
	private static string TextOf(Node n) => n switch { Label l => l.Text, RichTextLabel r => r.Text, Button b => b.Text, _ => null };
	private bool Shows(string text) => All().OfType<CanvasItem>().Any(c => c.IsVisibleInTree() && TextOf(c)?.Contains(text, StringComparison.Ordinal) == true);
	private Button ButtonNamed(string text) => All().OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && !b.Disabled && b.Text.Contains(text, StringComparison.Ordinal));
	private async Task Frame() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree().CreateTimer(0.05), Timer.SignalName.Timeout);
	}
	private async Task Screenshot(string name, double settle = 0.6) {
		if (DisplayServer.GetName() == "headless") return;
		await ToSignal(GetTree().CreateTimer(settle), Timer.SignalName.Timeout);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/caller-ui-" + name + ".png"));
	}

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			await Frame();
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(desk.FoundLabel("Caller UI Checks", "detroit", out string message), "Found: " + message);
			desk.DismissLienMorning();
			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			panel = Nodes(this).OfType<PlayerDeskPanel>().First(p => p.Name != "StudioWindow");
			void HidePaper() { if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false; }
			async Task FoldPaper() {
				await Frame();
				typeof(UIManager).GetMethod("DismissMorningPaper", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(UIManager.Instance, null);
				await ToSignal(GetTree().CreateTimer(0.5), Timer.SignalName.Timeout);
			}
			async Task CloseFolder() {
				foreach (var m in All().OfType<PaperModal>().ToList()) m.Cancel();
				await FoldPaper();
				if (ButtonNamed("CLOSE  ×") is { } close) close.EmitSignal(Button.SignalName.Pressed);
				await Frame();
			}
			int Today() => LocalSceneRoomService.Day(clock.CurrentDate);
			var shop = desk.StopsInCity("detroit").First(s => s.Kind == PlayerDesk.StopKind.Shop);
			shop.LastVisitWeek = 1;
			desk.QueueRingForChecks(shop, "chk_ui_call");
			var ring = desk.PendingRingsForChecks.First(r => r.RecordId == "chk_ui_call");
			while (Today() < ring.Day) { clock.EndDay(); HidePaper(); }
			clock.RestoreClock(clock.CurrentDate, (ring.Tries[0] - 10) / 60, (ring.Tries[0] - 10) % 60);
			await CloseFolder();

			var phone = Nodes(GetTree().Root).OfType<Node3D>().FirstOrDefault(n => n.Name.ToString().Contains("telephone", StringComparison.OrdinalIgnoreCase));
			Check(phone != null, $"the office has its Bakelite phone ({phone?.Name})");
			var receiver = phone.GetParent().GetNodeOrNull<Node3D>("OfficePhoneReceiver");
			Check(receiver != null, "the phone has a separate receiver");
			var baseRest = phone.Transform;
			var receiverRest = receiver.Transform;
			var ashtray = All().OfType<Button>().Single(b => b.Name == "OfficeAshtray" && b.IsVisibleInTree());
			var ash = All().OfType<Node3D>().Where(n => n.Name.ToString().StartsWith("OfficeAshtrayWaste")).ToArray();
			Check(ash.Length == PlayerDesk.AshtrayCapacity && ash.All(n => !n.Visible), "the ashtray starts clean");
			ashtray.EmitSignal(Button.SignalName.Pressed);
			await Frame();
			var waitMenu = All().OfType<PanelContainer>().Single(p => p.Name == "OfficeWaitMenu");
			Check(waitMenu.IsVisibleInTree() && !PaperModal.IsOpen &&
				ashtray.GetGlobalRect().HasPoint(waitMenu.GetGlobalRect().GetCenter()), "the wait menu sits over the ashtray without dimming the room");
			Check(ButtonNamed("WAIT 1h") != null && Shows("WAIT FOR EVENING"), "the popover offers the other two waits");
			await Screenshot("ashtray-wait");
			var wait = ButtonNamed("WAIT 3h");
			wait.EmitSignal(Button.SignalName.Pressed);
			Check(desk.Calling == PlayerDesk.CallerAt.Phone, "waiting in the room stops for the phone");
			await Frame();
			Check(!waitMenu.Visible && ash.Count(n => n.Visible) == 1 && desk.CaptureState().AshtrayBatches == 1,
				"the wait adds one visible batch to the current day's save");
			var hotspot = All().OfType<Button>().FirstOrDefault(b => b.Name == "RingingPhone");
			//GD.Print($"  debug hotspot={hotspot != null} visible={hotspot?.Visible} inTree={hotspot?.IsVisibleInTree()} parent={hotspot?.GetParent()?.Name} parentVisible={(hotspot?.GetParent() as CanvasItem)?.IsVisibleInTree()} paper={(GetTree().Root.FindChild("MorningPaper", true, false) as Control)?.Visible} intro={(GetTree().Root.FindChild("OfficeIntro", true, false) as CanvasItem)?.Visible}");
			Check(hotspot is { } && hotspot.IsVisibleInTree(), "a hotspot sits over the ringing phone");
			float lowestLift = float.PositiveInfinity, highestLift = 0f;
			bool baseStill = true, receiverVertical = true;
			for (int i = 0; i < 12; i++) {
				await ToSignal(GetTree().CreateTimer(0.025), Timer.SignalName.Timeout);
				Vector3 offset = receiver.Position - receiverRest.Origin;
				lowestLift = Mathf.Min(lowestLift, offset.Y);
				highestLift = Mathf.Max(highestLift, offset.Y);
				baseStill &= phone.Transform.IsEqualApprox(baseRest);
				receiverVertical &= Mathf.IsZeroApprox(offset.X) && Mathf.IsZeroApprox(offset.Z) && receiver.Basis.IsEqualApprox(receiverRest.Basis);
			}
			Check(baseStill && receiverVertical, "the base stays still and the receiver moves straight up and down");
			Check(lowestLift >= 0f && lowestLift < .001f && highestLift is > .006f and < .01f, "the receiver hops just off the cradle and lands again during the ring");
			await Screenshot("01-ringing", 0.2);

			panel.OpenAtTab("ROSTER"); await Frame();
			var pickUp = ButtonNamed("PICK UP THE PHONE");
			Check(pickUp != null, "with the folder open, PICK UP THE PHONE is beside the waits");
			Check(!hotspot.IsVisibleInTree(), "and the hotspot over the phone stands down (the folder can cover it)");
			await Screenshot("02-folder-ringing", 0.3);
			pickUp.EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(Shows("THE PHONE") && Shows(shop.DisplayName) && desk.HasOpenCall(shop.StopId, "chk_ui_call"), "picked up: the card names the shop, and the call is open");
			await Screenshot("03-card");
			Check(ButtonNamed("HANG UP") is { } hang, "the card hangs up");
			ButtonNamed("HANG UP").EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(desk.Calling == PlayerDesk.CallerAt.None && ButtonNamed("PICK UP THE PHONE") == null && !hotspot.IsVisibleInTree(), "it's quiet: no button, no hotspot");
			Check(receiver.Transform.IsEqualApprox(receiverRest), "the receiver is back in its cradle");

			desk.QueueVisitForChecks("nobody", clock.CurrentHour * 60 + clock.CurrentMinute + 20);
			desk.PassTime(1, out _, stopForCallers: true);
			await Frame();
			Check(desk.Calling == PlayerDesk.CallerAt.Door && ButtonNamed("ANSWER THE DOOR") != null && !hotspot.IsVisibleInTree(), "a knock: ANSWER THE DOOR, and the phone stays still");
			ButtonNamed("ANSWER THE DOOR").EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(Shows("AT THE DOOR"), "opening the door shows who was there");
			await CloseFolder();

			desk.LeaveDoorNoteForChecks("Sol Kramer", "Came by about the Rhythms. Nobody in. I can see you Monday at 3 or Tuesday at 10. Let me know which.");
			desk.LeaveDoorNoteForChecks("Jack Fitch", "Had a set open Thursday. Gave it to somebody else.");
			await Frame();
			var notes = All().OfType<Control>().Where(c => c.GetParent() is DoorNotesOnTheFloor && c.IsVisibleInTree()).OfType<Button>().ToList();
			Check(notes.Count == 2, $"two notes are tucked in the door ({notes.Count})");
			await Screenshot("04-door-notes");
			notes[0].EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(Shows("LEFT IN THE DOOR") && Shows("Sol Kramer") && desk.DoorNotes.Count == 1, "picked up and read: the note opens, and one is left on the floor");
			await Screenshot("05-note");
			await CloseFolder();

			var roomViewport = All().OfType<SubViewport>().Single(v => v.Name == "RoomViewport");
			var camera = roomViewport.GetCamera3D();
			var wideShot = camera.GlobalTransform;
			var rolodexProp = All().OfType<Node3D>().Single(n => n.Name == "OfficeRolodex");
			Check(rolodexProp.IsVisibleInTree() && !camera.IsPositionBehind(rolodexProp.GlobalPosition), "the physical Rolodex sits in the 3D office");
			Check(roomViewport.GetNode<Node3D>("Office/Left record sleeves").IsVisibleInTree() &&
				!roomViewport.GetNode<Node3D>("Office/Right record sleeves").IsVisibleInTree(), "the Rolodex replaces the right stack and the left stack remains");
			var phoneButton = All().OfType<Button>().Single(b => b.Name == "OfficePhoneRolodex");
			Check(phoneButton.IsVisibleInTree(), "the phone and Rolodex share an office hotspot");
			phoneButton.EmitSignal(Button.SignalName.Pressed);
			await ToSignal(GetTree().CreateTimer(.25), Timer.SignalName.Timeout);
			Check(panel.RolodexOpen && !panel.FilesOpen && !camera.GlobalTransform.IsEqualApprox(wideShot), "the hotspot opens a standalone view and dollies the camera");
			Check(panel.FindChild("OfficeFolder", true, false) is PanelContainer folder && !folder.IsVisibleInTree(), "Files chrome is hidden in the phone view");
			var index = panel.FindChild("RolodexIndex", true, false);
			var tabs = index.GetChildren().OfType<Button>().ToArray();
			Check(tabs.Select(b => b.Text.Split("   ")[0]).SequenceEqual(new[] {
				"DEEJAYS", "PRESS & TV", "PUBLISHERS", "MANAGERS", "BOOKERS", "PLANT", "ENGINEERS", "DISTRIBUTOR", "SHOPS & OPS"
			}) && tabs.All(b => int.TryParse(b.Text.Split("   ").Last(), out _)), "vertical tabs are ordered by group and show every count");
			tabs[2].EmitSignal(Button.SignalName.Pressed);
			await Frame();
			var publishers = RolodexDirectory.Cards(desk, RolodexRole.Publisher);
			Check(publishers.Count > 1 && publishers.Any(card => card.Figure.Female) &&
				publishers.Where(card => card.Figure.Female).SelectMany(card => card.Facts).All(fact =>
					!System.Text.RegularExpressions.Regex.IsMatch(fact.Text, @"\b(he|him|his)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)),
				"female publisher traits contain no male pronouns");
			var dock = panel.FindChild("PhoneDock", true, false);
			Check(Nodes(dock).OfType<Button>().Any(b => b.Text.StartsWith("WORK THE PHONES") && b.IsVisibleInTree()),
				"cold calls stay in the phone dock with Publishers selected");
			var deck = Nodes(panel).OfType<RolodexDeck>().Single();
			int firstCard = deck.Focus;
			Input.ParseInputEvent(new InputEventKey { Keycode = Key.Right, Pressed = true });
			await ToSignal(GetTree().CreateTimer(.12), Timer.SignalName.Timeout);
			Check(deck.Focus == (firstCard + 1) % publishers.Count, "right arrow flips the front card");
			await Screenshot("06-phone-rolodex");
			Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true });
			await ToSignal(GetTree().CreateTimer(.25), Timer.SignalName.Timeout);
			Check(!panel.Visible && phoneButton.IsVisibleInTree() && camera.GlobalTransform.IsEqualApprox(wideShot), "Esc returns to the wide office shot");

			clock.EndDay();
			await Frame();
			Check(desk.AshtrayBatches == 0 && ash.All(n => !n.Visible), "dawn clears the ashtray");

			GD.Print($"CALLER_UI_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception ex) {
			GD.PrintErr($"CALLER_UI_CHECKS_FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
			GetTree().Quit(1);
		}
	}
}
