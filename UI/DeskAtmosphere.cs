using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// The painted office kept alive: smoke curling off the ashtray, dust turning in the lamp's light, and the blinds'
/// slats of sun crossing the back wall in the afternoon. None of it is a node tree or a sprite sheet: one control draws
/// all three from the clock, so it costs a few polylines a frame. It sits in the painted layer, so the office light
/// tints it with the painting. Everything is placed in the scene's 1920x1080 design space against the painted props.
///
/// Time is the engine's own, and phases come from fixed numbers, never from a random stream, so none of this can move
/// a seeded run (see the byte-identity notes in the project memory).
/// </summary>
public partial class DeskAtmosphere : Control {
	// Where the painted smoke leaves the ashtray, and where it has thinned out to nothing.
	private static readonly Vector2 SmokeBase = new(1664f, 764f), SmokeTop = new(1716f, 452f);

	private double clock;
	private float hour = 12f;

	public DeskAtmosphere() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public override void _Process(double delta) {
		clock += delta;
		TimeManager time = TimeManager.Instance;
		if (time != null) hour = time.CurrentHour + time.CurrentMinute / 60f;
		QueueRedraw();
	}

	public override void _Draw() {
		DrawBlinds();
		DrawDust();
		DrawSmoke();
	}

	// ---- smoke --------------------------------------------------------------------------------------------

	/// <summary>Three strands rising from the cigarette, each a polyline that sways, widens and fades as it climbs. Their
	/// phases are staggered so there is always one at every height, like a real thread of smoke.</summary>
	private void DrawSmoke() {
		const int strands = 3, steps = 26;
		for (int s = 0; s < strands; s++) {
			float phase = (float)((clock * 0.11 + s / (float)strands) % 1.0);   // 0..1, how high this strand's head has climbed
			float seed = s * 2.17f;
			Vector2 previous = default;
			for (int i = 0; i <= steps; i++) {
				float t = i / (float)steps;                        // 0 at the ashtray, 1 at the head of the strand
				float rise = Mathf.Clamp(phase + 0.18f - t * 0.55f, 0f, 1f);   // the strand's own height along the plume
				float h = t * (0.35f + phase * 0.65f);              // how far up the plume this point is
				float sway = Mathf.Sin(h * 7.2f + (float)clock * 0.9f + seed) * (6f + h * 34f)
					+ Mathf.Sin(h * 3.1f - (float)clock * 0.5f + seed * 1.7f) * (4f + h * 20f);
				Vector2 point = SmokeBase.Lerp(SmokeTop, h) + new Vector2(sway, 0f);
				if (i > 0) {
					float alpha = Mathf.Sin(Mathf.Pi * Mathf.Clamp(t, 0f, 1f)) * (1f - h) * 0.30f * (0.6f + rise * 0.4f);
					if (alpha > 0.004f) DrawLine(previous, point, new Color(0.92f, 0.90f, 0.86f, alpha), 1.4f + h * 5.5f, true);
				}
				previous = point;
			}
		}
	}

	// ---- dust ---------------------------------------------------------------------------------------------

	private const int Motes = 46;

	/// <summary>Motes drifting in the cone of the lamp, over the desk. They fall very slowly, wander sideways and wink as they
	/// turn in and out of the light. Positions are a fixed lattice shuffled by a hash, so the cloud never changes shape between
	/// runs; brightness rises toward evening, when the lamp is the only light there is.</summary>
	private void DrawDust() {
		float evening = Mathf.Clamp((hour - 15f) / 5f, 0f, 1f);
		for (int i = 0; i < Motes; i++) {
			uint h = Portraits.Hash("mote" + i);
			float u = (h % 1000u) / 1000f, v = ((h / 1000u) % 1000u) / 1000f;
			float fall = (float)((clock * (3.5f + (h % 5u))) + v * 560f) % 560f;       // falls through the cone and starts over at the lamp
			float depth = fall / 560f;                                                  // 0 at the lamp, 1 at the desk
			// the cone widens from the lamp (x~330..520) to the desk (x~120..900)
			float left = Mathf.Lerp(330f, 120f, depth), right = Mathf.Lerp(520f, 900f, depth);
			float x = Mathf.Lerp(left, right, u) + Mathf.Sin((float)clock * (0.25f + u * 0.2f) + i) * 12f;
			float y = 120f + fall;
			float twinkle = 0.55f + 0.45f * Mathf.Sin((float)clock * (0.8f + u) + i * 1.9f);
			float edge = Mathf.Sin(Mathf.Pi * depth);                                  // fades in under the shade and out at the desk
			float alpha = (0.18f + 0.30f * evening) * twinkle * edge;
			if (alpha <= 0.01f) continue;
			float radius = 0.9f + (h % 7u) * 0.22f;
			DrawCircle(new Vector2(x, y), radius, new Color(1f, 0.92f, 0.72f, alpha));
			if (radius > 1.8f) DrawCircle(new Vector2(x, y), radius * 2.4f, new Color(1f, 0.90f, 0.65f, alpha * 0.18f));
		}
	}

	// ---- blinds -------------------------------------------------------------------------------------------

	/// <summary>Daylight through the venetian blinds: slanted bands across the back wall that come up in the morning, slide
	/// down it as the sun climbs and turn amber by five, then go out with the daylight.</summary>
	private void DrawBlinds() {
		float strength = Mathf.SmoothStep(7f, 10.5f, hour) * (1f - Mathf.SmoothStep(17.5f, 19.5f, hour));
		if (strength <= 0.01f) return;
		float low = Mathf.Clamp((hour - 15f) / 3f, 0f, 1f);                            // 0 midday, 1 low sun
		Color noon = new(1f, 0.97f, 0.86f), amber = new(1f, 0.74f, 0.42f);
		Color tint = noon.Lerp(amber, low);
		float slide = (hour - 12f) * 30f;                                              // the bands move with the sun
		const float wallTop = 0f, wallBottom = 478f, slant = -150f, pitch = 74f, width = 30f;
		for (int i = -3; i < 14; i++) {
			float x0 = 640f + i * pitch + slide;
			var points = new[] {
				new Vector2(x0, wallTop), new Vector2(x0 + width, wallTop),
				new Vector2(x0 + width + slant, wallBottom), new Vector2(x0 + slant, wallBottom)
			};
			float a = 0.12f * strength * (0.8f + 0.2f * Mathf.Sin(i * 1.3f));
			var colors = new[] {
				new Color(tint, a), new Color(tint, a),
				new Color(tint, 0f), new Color(tint, 0f)
			};
			DrawPolygon(points, colors);
		}
	}
}

/// <summary>
/// The left-hand stack of records, grown with the catalogue. The painting has a stack with a disc on top; each record the
/// label has made (cut, pressed or out) lays another sleeve on it. The sleeves are drawn in the painting's own
/// perspective (the top face of the painted stack is the quad below), a hair darker at the front and darker still on the
/// right, so the pile reads as lit from the lamp at the upper left. The label's 45 centre label is lifted with the top.
/// </summary>
public partial class PaintedStack : Control {
	// The top face of the painted stack in 1920x1080 space: back-left, back-right, front-right, front-left.
	private static readonly Vector2 BackLeft = new(918f, 328f), BackRight = new(1133f, 337f), FrontRight = new(1080f, 361f), FrontLeft = new(847f, 351f);
	public const float SleeveThickness = 6.5f;
	public const int MaxSleeves = 9;
	private static readonly string[] Stock = { "ccb98c", "b8a276", "d8c79b", "a9946b", "c4ad7f", "8f7a55", "bda37a", "a8523c", "d2c3a0", "4a6a7a", "c9b283" };

	private int sleeves;

	public PaintedStack() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	/// <summary>How far the top of the stack has been lifted, for whatever is drawn on it.</summary>
	public static float Lift(int count) => Mathf.Clamp(count, 0, MaxSleeves) * SleeveThickness;

	public void Set(int recordCount) {
		int next = Mathf.Clamp(recordCount, 0, MaxSleeves);
		if (next == sleeves) return;
		sleeves = next;
		QueueRedraw();
	}

	public override void _Draw() {
		for (int i = 1; i <= sleeves; i++) {
			Vector2 up = new(0f, -i * SleeveThickness);
			Vector2 lb = BackLeft + up, rb = BackRight + up, rf = FrontRight + up, lf = FrontLeft + up;
			Color stock = new(Stock[(int)(Portraits.Hash("sleeve" + i) % (uint)Stock.Length)]);
			Vector2 down = new(0f, SleeveThickness + 0.6f);
			// front edge, then the right-hand edge, then the top face
			DrawColoredPolygon(new[] { lf, rf, rf + down, lf + down }, stock.Darkened(0.28f));
			DrawColoredPolygon(new[] { rb, rf, rf + down, rb + down }, stock.Darkened(0.46f));
			DrawColoredPolygon(new[] { lb, rb, rf, lf }, stock.Lightened(0.06f));
			// the printed edge of the sleeve, and a thread of light along its front
			DrawLine(lf, rf, new Color(1f, 0.96f, 0.82f, 0.35f), 1f, true);
			DrawLine(lf + down, rf + down, new Color(0f, 0f, 0f, 0.25f), 1f, true);
		}
	}
}

/// <summary>
/// WHILE YOU WERE OUT: pink message slips pinned to the panelling beside the door for calls the answering service took
/// and the call-backs that have come due. At most three show, fanned and tilted, with a pin through each; any more are a
/// line of type. Pressing a slip opens the department that holds the call. It is a read of
/// <see cref="PlayerDesk.DeskMessages"/>, nothing is stored.
/// </summary>
public partial class MessageSlips : Control {
	public Action<string> Open;

	private static readonly (Vector2 Position, float Tilt)[] Places = {
		(new Vector2(838f, 62f), -0.06f), (new Vector2(972f, 72f), 0.05f), (new Vector2(902f, 140f), 0.02f),
	};
	private const float SlipWidth = 136f, SlipHeight = 92f;

	private List<PlayerDesk.DeskMessage> messages = new();
	private int overflow;

	public MessageSlips() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public void Set(IReadOnlyList<PlayerDesk.DeskMessage> items) {
		messages = new List<PlayerDesk.DeskMessage>();
		for (int i = 0; i < items.Count && i < Places.Length; i++) messages.Add(items[i]);
		overflow = Math.Max(0, items.Count - Places.Length);
		foreach (Node child in GetChildren()) child.QueueFree();
		for (int i = 0; i < messages.Count; i++) {
			PlayerDesk.DeskMessage message = messages[i];
			var (position, tilt) = Places[i];
			var hit = new SlipButton { Message = message, Flat = true, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
			hit.Position = position; hit.Size = new Vector2(SlipWidth, SlipHeight); hit.PivotOffset = hit.Size / 2f; hit.Rotation = tilt;
			hit.TooltipText = $"{message.From}: {message.Text}";
			hit.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
			hit.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(1f, 0.89f, 0.68f, 0.10f), BorderColor = new Color("f2b35a"), BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2 });
			hit.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
			hit.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
			string tab = message.Tab;
			hit.Pressed += () => Open?.Invoke(tab);
			AddChild(hit);
		}
		Visible = messages.Count > 0;
		QueueRedraw();
	}

	public override void _Draw() {
		if (messages.Count == 0 || overflow <= 0) return;
		DrawString(PaperTheme.TypedBold, new Vector2(842f, 262f), $"+{overflow} more message{(overflow == 1 ? "" : "s")} on the desk", HorizontalAlignment.Left, -1, 13, new Color(0.96f, 0.86f, 0.74f, 0.9f));
	}

	private partial class SlipButton : Button {
		public PlayerDesk.DeskMessage Message;

		public override void _Draw() {
			// shadow, paper, the printed head, the typed lines, the tick box and a pin
			DrawRect(new Rect2(new Vector2(3f, 4f), Size), new Color(0f, 0f, 0f, 0.30f));
			DrawRect(new Rect2(Vector2.Zero, Size), new Color("f1b3ab"));
			DrawRect(new Rect2(Vector2.Zero, new Vector2(Size.X, 17f)), new Color("e48d84"));
			DrawRect(new Rect2(Vector2.Zero, Size), new Color("a8554c"), false, 1f);
			DrawString(PaperTheme.SansBold, new Vector2(8f, 12.5f), "WHILE YOU WERE OUT", HorizontalAlignment.Left, Size.X - 12f, 10, new Color("5a1d16"));
			DrawString(PaperTheme.TypedBold, new Vector2(8f, 32f), Message.From, HorizontalAlignment.Left, Size.X - 14f, 11, new Color("2b2115"));
			DrawMultilineString(PaperTheme.Typed, new Vector2(8f, 46f), Message.Text, HorizontalAlignment.Left, Size.X - 16f, 10, 3, new Color("3a2c18"));
			DrawRect(new Rect2(8f, Size.Y - 18f, 9f, 9f), new Color("5a1d16"), false, 1f);
			DrawLine(new Vector2(9.5f, Size.Y - 13.5f), new Vector2(12f, Size.Y - 10.5f), new Color("a3261a"), 1.6f, true);
			DrawLine(new Vector2(12f, Size.Y - 10.5f), new Vector2(17f, Size.Y - 19.5f), new Color("a3261a"), 1.6f, true);
			DrawString(PaperTheme.SansSemiBold, new Vector2(22f, Size.Y - 10f), "PLEASE CALL BACK", HorizontalAlignment.Left, -1, 9, new Color("5a1d16"));
			// the pin
			var pin = new Vector2(Size.X / 2f, 6f);
			DrawCircle(pin + new Vector2(1.5f, 2f), 4.5f, new Color(0f, 0f, 0f, 0.35f));
			DrawCircle(pin, 4.5f, new Color("b3261a"));
			DrawCircle(pin + new Vector2(-1.2f, -1.4f), 1.5f, new Color(1f, 1f, 1f, 0.6f));
		}
	}
}
