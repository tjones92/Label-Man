using Godot;

/// <summary>The four rooms an A&amp;R man can work, as the linocut cut for each handbill.</summary>
public enum VenueGlyph { Club, Theatre, HonkyTonk, Meet }

/// <summary>
/// A linocut of a room: the roadhouse with its sign lit, the theatre's bulbed marquee, the honky-tonk's swing doors under
/// a star, the trade meet's microphone. Drawn the way <see cref="LinocutIcon"/> is (solid ink, the design gouged out in
/// paper colour, a hash-seeded wobble on every edge so a print is the same each time).
/// </summary>
public partial class VenueIcon : Control {
	private static readonly Color Ink = new("1f1a14");
	private VenueGlyph glyph;
	private Color paper = new("f6edd2");

	public VenueIcon Set(VenueGlyph kind, float size = 64f, Color? ground = null) {
		glyph = kind;
		paper = ground ?? paper;
		CustomMinimumSize = new Vector2(size, size);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Ignore;
		QueueRedraw();
		return this;
	}

	private float Jitter(int index) => ((Portraits.Hash($"venue{(int)glyph}:{index}") % 100u) / 100f - 0.5f) * 0.025f;
	private Vector2 P(float x, float y, int index) => new((x + Jitter(index * 2)) * Size.X, (y + Jitter(index * 2 + 1)) * Size.Y);

	private Vector2[] Points(float[] xy, int salt) {
		var points = new Vector2[xy.Length / 2];
		for (int i = 0; i < points.Length; i++) points[i] = P(xy[i * 2], xy[i * 2 + 1], salt * 40 + i);
		return points;
	}
	private void Block(float[] xy, Color color, int salt = 0) => DrawColoredPolygon(Points(xy, salt), color);
	private void Cut(float[] xy, float width, int salt = 0) => DrawPolyline(Points(xy, salt), paper, Mathf.Max(1.4f, width * Size.X), true);
	private void Line(float[] xy, float width, int salt = 0) => DrawPolyline(Points(xy, salt), Ink, Mathf.Max(1.4f, width * Size.X), true);

	public override void _Draw() {
		float s = Size.X;
		switch (glyph) {
			case VenueGlyph.Club:
				Block(new[] { .08f, .44f, .92f, .44f, .92f, .91f, .08f, .91f }, Ink);                              // the front
				Block(new[] { .04f, .28f, .96f, .28f, .96f, .46f, .04f, .46f }, Ink, 1);                           // the sign board
				Cut(new[] { .12f, .37f, .30f, .37f }, .045f, 2); Cut(new[] { .38f, .37f, .54f, .37f }, .045f, 3); Cut(new[] { .62f, .37f, .88f, .37f }, .045f, 4);   // neon, lit
				Block(new[] { .40f, .60f, .60f, .60f, .60f, .91f, .40f, .91f }, paper, 5);                         // the door
				Block(new[] { .14f, .56f, .31f, .56f, .31f, .74f, .14f, .74f }, paper, 6);                         // windows
				Block(new[] { .69f, .56f, .86f, .56f, .86f, .74f, .69f, .74f }, paper, 7);
				Line(new[] { .52f, .04f, .44f, .14f, .54f, .15f, .46f, .26f }, .04f, 8);                           // the neon flash
				break;
			case VenueGlyph.Theatre:
				Block(new[] { .06f, .40f, .18f, .16f, .82f, .16f, .94f, .40f }, Ink);                              // the marquee
				for (int bulb = 0; bulb < 8; bulb++) {
					float x = .12f + bulb * .105f;
					Block(new[] { x, .30f, x + .055f, .30f, x + .055f, .36f, x, .36f }, paper, 10 + bulb);        // bulbs along the canopy
				}
				Block(new[] { .12f, .42f, .22f, .42f, .22f, .90f, .12f, .90f }, Ink, 1);                           // columns
				Block(new[] { .78f, .42f, .88f, .42f, .88f, .90f, .78f, .90f }, Ink, 2);
				Block(new[] { .30f, .46f, .70f, .46f, .70f, .90f, .30f, .90f }, Ink, 3);                           // the entrance
				Cut(new[] { .50f, .46f, .50f, .90f }, .03f, 4); Cut(new[] { .39f, .50f, .39f, .88f }, .02f, 5); Cut(new[] { .61f, .50f, .61f, .88f }, .02f, 6);
				Block(new[] { .04f, .90f, .96f, .90f, .96f, .97f, .04f, .97f }, Ink, 7);                           // the step
				break;
			case VenueGlyph.HonkyTonk: {
				Block(new[] { .12f, .10f, .88f, .10f, .88f, .32f, .12f, .32f }, Ink);                              // the hanging sign
				var star = new float[20];
				for (int i = 0; i < 10; i++) {
					float angle = -Mathf.Pi / 2f + i * Mathf.Pi / 5f, r = i % 2 == 0 ? .095f : .042f;
					star[i * 2] = .50f + Mathf.Cos(angle) * r; star[i * 2 + 1] = .21f + Mathf.Sin(angle) * r * 1.0f;
				}
				Block(star, paper, 1);                                                                             // a star cut out of it
				Block(new[] { .10f, .32f, .19f, .32f, .19f, .90f, .10f, .90f }, Ink, 2);                           // posts
				Block(new[] { .81f, .32f, .90f, .32f, .90f, .90f, .81f, .90f }, Ink, 3);
				Block(new[] { .24f, .50f, .48f, .50f, .47f, .80f, .24f, .80f }, Ink, 4);                           // the swing doors
				Block(new[] { .52f, .50f, .76f, .50f, .76f, .80f, .53f, .80f }, Ink, 5);
				Cut(new[] { .36f, .50f, .36f, .80f }, .022f, 6); Cut(new[] { .64f, .50f, .64f, .80f }, .022f, 7);
				Block(new[] { .05f, .86f, .95f, .86f, .95f, .91f, .05f, .91f }, Ink, 8);                           // the hitching rail
				break; }
			case VenueGlyph.Meet:
				DrawCircle(P(.50f, .28f, 0), .19f * s, Ink);                                                       // the microphone head
				Cut(new[] { .34f, .22f, .66f, .22f }, .024f, 1); Cut(new[] { .33f, .29f, .67f, .29f }, .024f, 2); Cut(new[] { .36f, .36f, .64f, .36f }, .024f, 3);
				Cut(new[] { .50f, .10f, .50f, .46f }, .022f, 4);
				Block(new[] { .41f, .46f, .59f, .46f, .55f, .55f, .45f, .55f }, Ink, 5);                           // the collar
				Block(new[] { .465f, .55f, .535f, .55f, .535f, .82f, .465f, .82f }, Ink, 6);                       // the stand
				Block(new[] { .26f, .82f, .74f, .82f, .80f, .92f, .20f, .92f }, Ink, 7);                           // the base
				for (int arc = 0; arc < 2; arc++) {
					float radius = (.27f + arc * .09f) * s;
					DrawArc(P(.50f, .28f, 20 + arc), radius, -0.9f, 0.9f, 12, Ink, Mathf.Max(2f, .04f * s), true);                  // the sound going out, both sides
					DrawArc(P(.50f, .28f, 22 + arc), radius, Mathf.Pi - 0.9f, Mathf.Pi + 0.9f, 12, Ink, Mathf.Max(2f, .04f * s), true);
				}
				break;
		}
	}
}

/// <summary>
/// One room on the A&amp;R board, printed as a handbill: the room's linocut, its name in poster slab, who plays it, what
/// they ask and when the door is open, with the open or shut state struck on it. The picked room is pinned up and
/// ringed in the label's red. It is a <see cref="Button"/> so it takes focus and clicks like the dropdown it replaces;
/// everything drawn on it is a mouse-ignoring child, because a script draw runs before the button's own stylebox.
/// </summary>
public partial class VenueHandbill : Button {
	private static readonly Color Ink = new("2b2115");

	public static VenueHandbill Make(VenueGlyph glyph, string title, string plays, string ask, string hours, bool open, bool selected, int seed) {
		var card = new VenueHandbill {
			CustomMinimumSize = new Vector2(0, 214), SizeFlagsHorizontal = SizeFlags.ExpandFill,
			FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand, ClipText = false
		};
		Color fill = selected ? new Color("fbf4df") : (seed % 2 == 0 ? new Color("eadcb4") : new Color("e4d5a8"));
		PaperStyleBox Face(Color paperFill, bool lift) {
			var style = PaperStyleBox.Sheet(paperFill, 0, 0, lift ? 14 : 8, selected ? PaperTheme.Rust : new Color("8c6f38"));
			style.BorderWidth = selected ? 3 : 1;
			style.Burn = selected ? 0.5f : 0.9f;
			return style;
		}
		card.AddThemeStyleboxOverride("normal", Face(fill, selected));
		card.AddThemeStyleboxOverride("hover", Face(fill.Lightened(0.08f), true));
		card.AddThemeStyleboxOverride("pressed", Face(fill, selected));
		card.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

		var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		foreach (string side in new[] { "left", "right" }) margin.AddThemeConstantOverride("margin_" + side, 14);
		margin.AddThemeConstantOverride("margin_top", 14);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		card.AddChild(margin);
		var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 3);
		margin.AddChild(column);

		var top = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		column.AddChild(top);
		top.AddChild(new VenueIcon().Set(glyph, 62f, fill));
		top.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
		top.AddChild(new RubberStamp().Set(open ? "Open" : "Shut", open ? new Color("2f6b2a") : RubberStamp.Red, open ? -0.09f : 0.08f, 12));

		Label Line(string text, Font font, int size, Color color, bool wrap = false) {
			var line = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			if (wrap) line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			line.AddThemeFontOverride("font", font);
			line.AddThemeFontSizeOverride("font_size", size);
			line.AddThemeColorOverride("font_color", color);
			return line;
		}
		Label heading = Line(title.ToUpperInvariant(), PaperTheme.Lettering(LetteringStyle.HeavySlab), 17, Ink, true);
		heading.CustomMinimumSize = new Vector2(0, 46);   // two lines, so the rows line up across the board
		column.AddChild(heading);
		column.AddChild(Line(plays, PaperTheme.SerifItalic, 15, PaperTheme.Rust, true));
		column.AddChild(new ColorRect { Color = new Color(Ink, 0.5f), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore });
		column.AddChild(Line(ask, PaperTheme.Typed, 14, Ink));
		column.AddChild(Line(hours, PaperTheme.Typed, 14, Ink));

		if (selected) {
			var pin = new Pin { MouseFilter = MouseFilterEnum.Ignore };
			pin.SetAnchorsPreset(LayoutPreset.CenterTop);
			pin.OffsetLeft = -12; pin.OffsetRight = 12; pin.OffsetTop = -9; pin.OffsetBottom = 15; pin.ZIndex = 2;
			card.AddChild(pin);
		}
		return card;
	}

	/// <summary>A red-headed pushpin through the top of the picked handbill.</summary>
	private partial class Pin : Control {
		public override void _Draw() {
			Vector2 c = new(Size.X / 2f, Size.Y / 2f);
			DrawCircle(c + new Vector2(2f, 3f), 8f, new Color(0, 0, 0, 0.3f));
			DrawCircle(c, 8f, new Color("a8322a"));
			DrawArc(c, 8f, 0f, Mathf.Tau, 20, new Color("5c1a14"), 1.4f, true);
			DrawCircle(c + new Vector2(-2.5f, -2.5f), 2.4f, new Color(1f, 0.85f, 0.8f, 0.8f));
		}
	}
}
