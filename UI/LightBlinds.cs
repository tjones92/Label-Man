using Godot;

/// <summary>Noon through venetian blinds: slanted bars of shadow across the whole screen. A backdrop that says it is
/// daytime and the head is splitting, without a texture.</summary>
public partial class LightBlinds : Control {
	public Color Shade = new(0.30f, 0.18f, 0.06f, 0.16f);
	public float Pitch = 78f, Bar = 34f, Slant = 0.16f;

	public LightBlinds() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public override void _Notification(int what) { if (what == NotificationResized) QueueRedraw(); }

	public override void _Draw() {
		float w = Size.X, drop = w * Slant;
		for (float y = -drop - Pitch; y < Size.Y + Pitch; y += Pitch) {
			DrawColoredPolygon(new[] { new Vector2(0, y), new Vector2(w, y + drop), new Vector2(w, y + drop + Bar), new Vector2(0, y + Bar) }, Shade);
		}
	}
}
