using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>The office's half of PlayerDesk.Callers: the Bakelite phone on the credenza rings and rattles while a call waits to
/// be picked up, a knock sounds at the door, and notes left in the door stay tucked below the knob until read. With the folder
/// closed the phone itself picks up; with it open (it can cover the phone) the folder's PICK UP button does.</summary>
public partial class UIManager {
	/// <summary>The phone's place on the credenza, in the 1920x1080 design space.</summary>
	private static readonly Rect2 RingingPhoneArea = new(1530, 325, 200, 120);
	/// <summary>Where notes left in the door are tucked, below the knob (the door's foot is behind the desk), in the design space.</summary>
	public static readonly Vector2 DoorNotesAt = new(642f, 322f);
	private const float RingSeconds = 2f, RingCycle = 6f;
	private const float ReceiverBouncesPerSecond = 9f, ReceiverLift = .009f;
	private const int RingsAudible = 5, KnocksAudible = 3;

	private Node3D phoneReceiver;
	private Vector3 receiverRest;
	private Button ringingPhone;
	private DoorNotesOnTheFloor doorNotesOnFloor;
	private PlayerDesk.CallerAt calling;
	private double ringClock;
	private int ringsSounded;

	private void BuildCallers() {
		phoneReceiver = officeViewport?.GetNodeOrNull<Node3D>("Office/OfficePhoneReceiver");
		if (phoneReceiver != null) receiverRest = phoneReceiver.Position;
		ringingPhone = new Button { Name = "RingingPhone", Flat = true, FocusMode = Control.FocusModeEnum.None, Visible = false, ZIndex = 35,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand, Position = RingingPhoneArea.Position, Size = RingingPhoneArea.Size,
			TooltipText = "The phone's ringing. Pick it up." };
		StyleHotspot(ringingPhone);
		ringingPhone.Pressed += PickUpFromTheDesk;
		AddChild(ringingPhone);
		doorNotesOnFloor = new DoorNotesOnTheFloor { Read = id => PlayerDeskPanel.ShowDoorNote(this, PlayerDesk.Instance?.ReadDoorNote(id)) };
		paintedLayer.AddChild(doorNotesOnFloor);
		SetProcess(true);
	}

	private void PickUpFromTheDesk() {
		if (PaperModal.IsOpen || morningPaper?.Visible == true) return;
		var desk = PlayerDesk.Instance;
		if (desk == null || desk.Calling == PlayerDesk.CallerAt.None) return;
		if (deskPanel != null && deskPanel.Visible) { deskPanel.AnswerCaller(); return; }
		CallerCard card = desk.PickUp(out string message);
		if (card != null) PlayerDeskPanel.ShowCallerCard(this, card); else ShowAnnouncement(message);
	}

	/// <summary>From UpdateMainHud: start or stop the ringing, and lay the notes by the door.</summary>
	private void UpdateCallers(PlayerDesk desk) {
		var now = desk?.HasLabel == true && !desk.IsGameOver ? desk.Calling : PlayerDesk.CallerAt.None;
		if (now != calling) {
			calling = now;
			ringClock = 0; ringsSounded = 0;
			if (calling != PlayerDesk.CallerAt.Phone) SetReceiverLift(0f);
		}
		if (ringingPhone != null) ringingPhone.Visible = calling == PlayerDesk.CallerAt.Phone && !DeskBlockedByPaper() && !DeskCovered();
		doorNotesOnFloor?.Set(desk?.HasLabel == true && !desk.IsGameOver ? desk.DoorNotes : Array.Empty<DoorNoteSaveData>());
	}

	private bool DeskBlockedByPaper() => morningPaper?.Visible == true || officeIntro?.Visible == true;

	public override void _Process(double delta) {
		if (calling == PlayerDesk.CallerAt.None) return;
		double before = ringClock;
		ringClock += delta;
		double inCycle = ringClock % RingCycle;
		bool cycleStarted = (int)(before / RingCycle) != (int)(ringClock / RingCycle) || before == 0;
		if (calling == PlayerDesk.CallerAt.Phone) {
			if (cycleStarted && ringsSounded < RingsAudible) { ringsSounded++; OfficeFoley.Play(this, "ring", -9f); }
			float lift = 0f;
			if (inCycle < RingSeconds) {
				// A short arc followed by a beat of contact with the cradle.
				float phase = (float)(inCycle * ReceiverBouncesPerSecond % 1.0);
				float flight = Mathf.Min(phase / .8f, 1f);
				lift = ReceiverLift * 4f * flight * (1f - flight);
			}
			SetReceiverLift(lift);
		} else if (cycleStarted && ringsSounded < KnocksAudible) {
			ringsSounded++;
			for (int i = 0; i < 3; i++) GetTree().CreateTimer(i * .32).Timeout += () => OfficeFoley.Play(this, "knock", -6f);
		}
	}

	private void SetReceiverLift(float lift) {
		if (phoneReceiver == null) return;
		Vector3 position = receiverRest + Vector3.Up * lift;
		if (phoneReceiver.Position == position) return;
		phoneReceiver.Position = position;
		if (officeViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
			officeViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
	}
}

/// <summary>Notes left in the office door: folded slips tucked below the knob, oldest first. Clicking one takes it out and
/// reads it (it's gone from the door). A read of <see cref="PlayerDesk.DoorNotes"/>; nothing is stored here.</summary>
public partial class DoorNotesOnTheFloor : Control {
	public Action<string> Read;
	private static readonly (Vector2 Offset, float Tilt)[] Lay = {
		(new Vector2(0f, 0f), -0.08f), (new Vector2(30f, 22f), 0.10f), (new Vector2(-14f, 44f), 0.04f), (new Vector2(22f, 64f), -0.14f),
	};
	private const float NoteWidth = 74f, NoteHeight = 46f;

	public DoorNotesOnTheFloor() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public void Set(IReadOnlyList<DoorNoteSaveData> notes) {
		string key = string.Join("|", notes.Select(n => n.Id));
		if (key == (string)GetMeta("notes", "")) return;
		SetMeta("notes", key);
		foreach (Node child in GetChildren()) child.QueueFree();
		var shown = notes.OrderBy(n => n.Day).Take(PlayerDesk.DoorNotesShown).ToList();
		for (int i = 0; i < shown.Count; i++) {
			var note = shown[i];
			var (offset, tilt) = Lay[i % Lay.Length];
			var hit = new NoteButton { Flat = true, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand,
				Position = UIManager.DoorNotesAt + offset, Size = new Vector2(NoteWidth, NoteHeight), Rotation = tilt,
				TooltipText = $"A note left in the door, from {note.From}. Take it out and read it." };
			hit.PivotOffset = hit.Size / 2f;
			foreach (string s in new[] { "normal", "hover", "pressed", "focus" }) hit.AddThemeStyleboxOverride(s, new StyleBoxEmpty());
			string id = note.Id;
			hit.Pressed += () => Read?.Invoke(id);
			AddChild(hit);
		}
		Visible = shown.Count > 0;
	}

	private partial class NoteButton : Button {
		public override void _Draw() {
			// A folded sheet: shadow, the paper, the crease, a line of pencil showing through.
			DrawRect(new Rect2(new Vector2(3f, 4f), Size), new Color(0f, 0f, 0f, 0.35f));
			DrawRect(new Rect2(Vector2.Zero, Size), new Color("efe6cf"));
			DrawRect(new Rect2(0f, 0f, Size.X, Size.Y / 2f), new Color("e3d8bd"));
			DrawLine(new Vector2(0f, Size.Y / 2f), new Vector2(Size.X, Size.Y / 2f), new Color("b9a983"), 1f, true);
			DrawLine(new Vector2(9f, Size.Y * .72f), new Vector2(Size.X - 14f, Size.Y * .70f), new Color(.25f, .25f, .35f, .35f), 1.2f, true);
			DrawLine(new Vector2(9f, Size.Y * .84f), new Vector2(Size.X - 26f, Size.Y * .83f), new Color(.25f, .25f, .35f, .30f), 1.2f, true);
			DrawRect(new Rect2(Vector2.Zero, Size), IsHovered() ? new Color("f2b35a") : new Color("8a7a58"), false, IsHovered() ? 2f : 1f);
		}
		public override void _Notification(int what) {
			if (what is (int)NotificationMouseEnter or (int)NotificationMouseExit) QueueRedraw();
		}
	}
}
