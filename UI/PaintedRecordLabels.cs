using Godot;

/// <summary>
/// The player's label printed onto the records already in the desk painting: the framed record on the wall and the
/// top disc of the left-hand stack each get the label's 45 centre label (paper ground, name on an arc, monogram).
/// It lives in the painted layer, so the office light tints it with the painting. Nothing is drawn until a label is
/// founded; the painting's own blank labels show through until then.
/// </summary>
public partial class PaintedRecordLabels : Control {
	// Centres and radii in the scene's 1920x1080 design space, matched to the painted discs.
	private static readonly Vector2 WallCentre = new(1366f, 155f);
	private const float WallRadius = 25f;
	private static readonly Vector2 StackCentre = new(991f, 347f);
	private const float StackRadius = 37f;
	private const float StackSquash = 0.22f;   // the stack is seen almost edge-on

	private LabelBrand brand;
	private string labelName = "";

	public PaintedRecordLabels() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		Visible = false;
	}

	/// <summary>Dresses the records in this label's colours; null hides them (no label yet).</summary>
	public void Set(LabelBrand labelBrand, string name) {
		brand = labelBrand;
		labelName = name ?? "";
		Visible = brand != null;
		QueueRedraw();
	}

	public override void _Draw() {
		if (brand == null) return;
		// The flat disc on the wall.
		LabelCrest.DrawDisc45(this, new Rect2(WallCentre - Vector2.One * WallRadius, Vector2.One * WallRadius * 2f), brand, labelName);
		// The top record of the stack: the same label squashed onto the disc's ellipse.
		DrawSetTransform(StackCentre, 0f, new Vector2(1f, StackSquash));
		LabelCrest.DrawDisc45(this, new Rect2(Vector2.One * -StackRadius, Vector2.One * StackRadius * 2f), brand, labelName);
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}
}
