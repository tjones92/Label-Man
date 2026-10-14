using Godot;

/// <summary>
/// A dog-eared bottom-right corner that dismisses the sheet it sits on: the corner is cut away to the desk
/// beneath, a lighter flap is folded over it, and hovering lifts the flap a little. It replaces a full-width
/// "FOLD THE PAPER" button, which was the one stock-looking thing on the Morning Paper.
/// </summary>
public partial class FoldCorner : Control {
	public event System.Action Pressed;

	/// <summary>The colour showing where the corner was cut away (the dimmed desk behind the sheet).</summary>
	public Color Cutaway = new(0.10f, 0.07f, 0.04f);
	public Color Flap = new("f3ead0");
	public Color Crease = new("867655");

	private const float Rest = 54f, Lifted = 70f;
	public const float LiftedSize = Lifted;
	private bool hot;

	public FoldCorner() {
		CustomMinimumSize = new Vector2(Lifted, Lifted);
		MouseDefaultCursorShape = CursorShape.PointingHand;
		TooltipText = "Fold the paper";
	}

	public override void _Ready() {
		MouseEntered += () => { hot = true; QueueRedraw(); };
		MouseExited += () => { hot = false; QueueRedraw(); };
	}

	public override void _GuiInput(InputEvent @event) {
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) {
			AcceptEvent();
			Pressed?.Invoke();
		}
	}

	public override void _Draw() {
		float w = Size.X, h = Size.Y, s = hot ? Lifted : Rest;
		// Cut-away corner: the triangle past the fold line.
		DrawColoredPolygon(new[] { new Vector2(w - s, h), new Vector2(w, h), new Vector2(w, h - s) }, Cutaway);
		// The flap, folded back across the fold line, with a soft shadow beneath its edge.
		var flap = new[] { new Vector2(w - s, h), new Vector2(w, h - s), new Vector2(w - s, h - s) };
		DrawColoredPolygon(new[] { flap[0] + new Vector2(2, 1), flap[1] + new Vector2(1, 2), flap[2] + new Vector2(4, 4) }, new Color(0, 0, 0, 0.28f));
		DrawColoredPolygon(flap, hot ? Flap.Lightened(0.35f) : Flap);
		DrawPolyline(new[] { flap[0], flap[1] }, Crease, 1.5f, true);
	}
}
