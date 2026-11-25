using Godot;

/// <summary>
/// The office door, drawn in the dark: a closed leaf with a seam of hall light under it; a leaf that swings open on its
/// hinge and lets the light in; and a man's silhouette in the doorway, hat and briefcase-shoulders, with the light
/// spilling past him onto the floor. All three states are driven by <see cref="SetOpen"/>, <see cref="SetVisitor"/> and
/// <see cref="SetShake"/> so a tween (or a knock) can play them.
/// </summary>
public partial class LienDoor : Control {
	private float open, visitor, shake;
	private static readonly Color Wood = new(0.24f, 0.155f, 0.09f);
	private static readonly Color Light = new(0.97f, 0.79f, 0.43f);
	private static readonly Color Shadow = new(0.025f, 0.02f, 0.015f);

	public LienDoor() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public void SetOpen(float value) { open = value; QueueRedraw(); }
	public void SetVisitor(float value) { visitor = value; QueueRedraw(); }
	public void SetShake(float value) { shake = value; QueueRedraw(); }

	public override void _Notification(int what) { if (what == NotificationResized) QueueRedraw(); }

	public override void _Draw() {
		float h = Mathf.Min(Size.Y * 0.74f, 540f), w = h * 0.46f;
		float cx = Size.X / 2f, floor = Size.Y * 0.74f, top = floor - h;
		var doorway = new Rect2(cx - w / 2f, top, w, h);

		// The hall behind the door: dark, then lit.
		DrawRect(doorway, Shadow);
		float lit = Mathf.Clamp(open * 2.2f, 0f, 1f);
		if (lit > 0f) {
			DrawRect(doorway, new Color(Light, 0.92f * lit));
			var spill = new[] {
				new Vector2(doorway.Position.X, floor), new Vector2(doorway.End.X, floor),
				new Vector2(doorway.End.X + w * 1.3f * open, Size.Y), new Vector2(doorway.Position.X - w * 1.3f * open, Size.Y),
			};
			DrawColoredPolygon(spill, new Color(Light, 0.2f * open));
		}
		if (visitor > 0.01f) DrawVisitor(cx, floor, h * 0.86f, visitor);

		// The leaf, hinged on the left; it narrows and leans as it swings toward the room.
		float angle = open * 1.2f;
		float width = w * Mathf.Cos(angle), lean = w * Mathf.Sin(angle) * 0.16f;
		float x0 = doorway.Position.X + shake;
		DrawColoredPolygon(new[] { new Vector2(x0, top), new Vector2(x0 + width, top - lean), new Vector2(x0 + width, floor + lean), new Vector2(x0, floor) }, Wood);
		if (open < 0.3f) {
			var panel = new Color(0f, 0f, 0f, 0.22f);
			DrawRect(new Rect2(x0 + width * 0.12f, top + h * 0.07f, width * 0.76f, h * 0.38f), panel, false, 3f);
			DrawRect(new Rect2(x0 + width * 0.12f, top + h * 0.53f, width * 0.76f, h * 0.40f), panel, false, 3f);
			DrawCircle(new Vector2(x0 + width * 0.86f, top + h * 0.55f), 7f, new Color("c8a24a"));
		}
		// The seam of light under a shut door.
		if (open < 0.05f) DrawRect(new Rect2(doorway.Position.X + 5f, floor - 3f, w - 10f, 3f), new Color(Light, 0.85f));
		DrawRect(new Rect2(doorway.Position.X - 7f, top - 7f, w + 14f, h + 7f), new Color(0.12f, 0.08f, 0.05f), false, 14f);
		DrawLine(new Vector2(0f, floor), new Vector2(Size.X, floor), new Color(1f, 1f, 1f, 0.05f), 2f);
	}

	/// <summary>A man in a hat and a coat against the light. Ten units tall, built from a few shapes.</summary>
	private void DrawVisitor(float cx, float floor, float height, float alpha) {
		float u = height / 10f, y0 = floor - height;
		var ink = new Color(Shadow, alpha);
		DrawColoredPolygon(Ellipse(new Vector2(cx, y0 + 1.95f * u), 0.62f * u, 0.8f * u), ink);                       // head
		DrawColoredPolygon(new[] { new Vector2(cx - 0.8f * u, y0 + 1.1f * u), new Vector2(cx - 0.75f * u, y0), new Vector2(cx + 0.75f * u, y0), new Vector2(cx + 0.8f * u, y0 + 1.1f * u) }, ink); // crown
		DrawColoredPolygon(Ellipse(new Vector2(cx, y0 + 1.15f * u), 1.4f * u, 0.22f * u), ink);                         // brim
		DrawColoredPolygon(new[] {                                                                                      // neck, shoulders, coat
			new Vector2(cx - 0.3f * u, y0 + 2.6f * u), new Vector2(cx + 0.3f * u, y0 + 2.6f * u), new Vector2(cx + 1.5f * u, y0 + 3.1f * u),
			new Vector2(cx + 1.85f * u, y0 + 8.3f * u), new Vector2(cx - 1.85f * u, y0 + 8.3f * u), new Vector2(cx - 1.5f * u, y0 + 3.1f * u),
		}, ink);
		DrawColoredPolygon(new[] { new Vector2(cx - 1.15f * u, y0 + 8.2f * u), new Vector2(cx - 0.12f * u, y0 + 8.2f * u), new Vector2(cx - 0.18f * u, floor), new Vector2(cx - 1.1f * u, floor) }, ink);   // legs
		DrawColoredPolygon(new[] { new Vector2(cx + 0.12f * u, y0 + 8.2f * u), new Vector2(cx + 1.15f * u, y0 + 8.2f * u), new Vector2(cx + 1.1f * u, floor), new Vector2(cx + 0.18f * u, floor) }, ink);
	}

	private static Vector2[] Ellipse(Vector2 center, float rx, float ry) {
		var points = new Vector2[24];
		for (int i = 0; i < points.Length; i++) {
			float a = Mathf.Tau * i / points.Length;
			points[i] = center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
		}
		return points;
	}
}
