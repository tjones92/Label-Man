using Godot;

public enum InstinctKind { Ear, Street, Suit, Fixer }

/// <summary>
/// The four instincts as linocut prints: solid black shapes with the lines of the design gouged out of them, the edges
/// a little uneven the way a hand-cut block prints. THE EAR is an ear with the sound arriving; THE STREET a lamp post
/// over a kerb and a brick wall; THE SUIT a jacket, lapels and a tie; THE FIXER a briefcase with a bill showing at
/// the lip. Each is drawn from polygons in a unit square, jittered by a hash so a print is the same every time.
/// </summary>
public partial class LinocutIcon : Control {
	private static readonly Color Ink = new("1f1a14");
	private InstinctKind kind;
	private Color paper = new("f1e5c8");

	public LinocutIcon Set(InstinctKind instinct, float size = 46f, Color? ground = null) {
		kind = instinct;
		paper = ground ?? paper;
		CustomMinimumSize = new Vector2(size, size);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Ignore;
		QueueRedraw();
		return this;
	}

	private float Jitter(int index) => ((Portraits.Hash($"lino{(int)kind}:{index}") % 100u) / 100f - 0.5f) * 0.03f;

	private Vector2 P(float x, float y, int index) => new((x + Jitter(index * 2)) * Size.X, (y + Jitter(index * 2 + 1)) * Size.Y);

	private void Block(float[] xy, Color color, int salt = 0) {
		var points = new Vector2[xy.Length / 2];
		for (int i = 0; i < points.Length; i++) points[i] = P(xy[i * 2], xy[i * 2 + 1], salt * 40 + i);
		DrawColoredPolygon(points, color);
	}

	private void Cut(float[] xy, float width, int salt = 0) {
		var points = new Vector2[xy.Length / 2];
		for (int i = 0; i < points.Length; i++) points[i] = P(xy[i * 2], xy[i * 2 + 1], salt * 40 + i);
		DrawPolyline(points, paper, Mathf.Max(1.2f, width * Size.X), true);
	}

	private void Line(float[] xy, float width, int salt = 0) {
		var points = new Vector2[xy.Length / 2];
		for (int i = 0; i < points.Length; i++) points[i] = P(xy[i * 2], xy[i * 2 + 1], salt * 40 + i);
		DrawPolyline(points, Ink, Mathf.Max(1.2f, width * Size.X), true);
	}

	public override void _Draw() {
		float s = Size.X;
		switch (kind) {
			case InstinctKind.Ear:
				Block(new[] { .55f, .10f, .72f, .14f, .82f, .28f, .84f, .46f, .76f, .60f, .67f, .70f, .65f, .82f, .57f, .92f, .46f, .90f, .42f, .80f, .45f, .70f, .36f, .60f, .30f, .46f, .30f, .30f, .38f, .18f }, Ink);
				Cut(new[] { .52f, .22f, .66f, .27f, .71f, .40f, .64f, .53f, .55f, .62f }, .036f, 1);        // the helix
				Cut(new[] { .50f, .43f, .56f, .36f, .62f, .42f, .57f, .51f, .50f, .50f }, .03f, 2);          // the bowl
				Cut(new[] { .52f, .66f, .54f, .78f }, .03f, 3);
				for (int ring = 0; ring < 3; ring++) {
					float radius = (.14f + ring * .075f) * s;
					DrawArc(P(.30f, .46f, 90 + ring), radius, Mathf.Pi * 0.82f, Mathf.Pi * 1.18f, 14, Ink, Mathf.Max(1.4f, .032f * s), true);   // sound arriving
				}
				break;
			case InstinctKind.Street:
				Block(new[] { .60f, .30f, .93f, .30f, .93f, .90f, .60f, .90f }, Ink);                        // a brick wall
				for (int row = 0; row < 5; row++) {
					float y = .38f + row * .105f;
					Cut(new[] { .60f, y, .93f, y }, .018f, 4 + row);
					for (float x = .66f + (row % 2) * .08f; x < .92f; x += .16f) Cut(new[] { x, y, x, y + .105f }, .016f, 12 + row);
				}
				Block(new[] { .46f, .28f, .52f, .28f, .53f, .86f, .45f, .86f }, Ink, 1);                     // the post
				Block(new[] { .40f, .84f, .58f, .84f, .60f, .91f, .38f, .91f }, Ink, 2);
				Line(new[] { .49f, .30f, .40f, .20f, .26f, .20f }, .04f, 3);                                  // the arm
				Block(new[] { .12f, .18f, .36f, .18f, .32f, .30f, .16f, .30f }, Ink, 4);                      // the lamp head
				Cut(new[] { .18f, .22f, .30f, .22f }, .02f, 20); Cut(new[] { .19f, .26f, .29f, .26f }, .018f, 21);
				for (int ray = 0; ray < 5; ray++) {
					float x = .14f + ray * .045f;
					Line(new[] { x, .34f, x - .03f + ray * .01f, .46f }, .02f, 30 + ray);                     // the light it throws
				}
				Block(new[] { .06f, .90f, .96f, .90f, .96f, .97f, .06f, .97f }, Ink, 5);                      // the kerb
				for (float x = .14f; x < .94f; x += .2f) Cut(new[] { x, .90f, x - .02f, .97f }, .016f, 40);
				break;
			case InstinctKind.Suit:
				Block(new[] { .30f, .18f, .44f, .12f, .50f, .22f, .56f, .12f, .70f, .18f, .88f, .30f, .91f, .92f, .09f, .92f, .12f, .30f }, Ink);
				Block(new[] { .44f, .15f, .56f, .15f, .50f, .50f }, paper, 1);                                // the shirt, a V under the chin
				Block(new[] { .485f, .19f, .515f, .19f, .525f, .44f, .50f, .52f, .475f, .44f }, Ink, 2);      // the tie
				Cut(new[] { .49f, .30f, .51f, .27f }, .016f, 5); Cut(new[] { .485f, .37f, .515f, .34f }, .016f, 6);
				Cut(new[] { .44f, .13f, .31f, .48f, .47f, .60f }, .03f, 3);                                   // lapels
				Cut(new[] { .56f, .13f, .69f, .48f, .53f, .60f }, .03f, 4);
				Block(new[] { .64f, .52f, .76f, .50f, .77f, .57f, .65f, .59f }, paper, 6);                    // the pocket square
				Cut(new[] { .16f, .70f, .84f, .70f }, .014f, 8);
				break;
			case InstinctKind.Fixer:
				Block(new[] { .14f, .42f, .86f, .42f, .88f, .84f, .12f, .84f }, Ink);                         // the briefcase
				Line(new[] { .36f, .42f, .37f, .30f, .63f, .30f, .64f, .42f }, .045f, 1);                    // the handle
				Cut(new[] { .13f, .60f, .87f, .60f }, .025f, 2);
				Block(new[] { .43f, .55f, .57f, .55f, .57f, .67f, .43f, .67f }, paper, 3);                    // the latch
				Block(new[] { .485f, .58f, .515f, .58f, .515f, .64f, .485f, .64f }, Ink, 4);
				Block(new[] { .60f, .20f, .80f, .26f, .77f, .42f, .58f, .38f }, paper, 5);                    // the bill, half out of the lip
				Line(new[] { .60f, .20f, .80f, .26f, .77f, .42f, .58f, .38f, .60f, .20f }, .014f, 6);
				DrawString(PaperTheme.SerifBold, new Vector2(.64f * s, .36f * s), "$", HorizontalAlignment.Left, -1, Mathf.RoundToInt(.18f * s), Ink);
				break;
		}
	}
}

/// <summary>A score out of five drawn as vinyl: a pressed disc for each point, an empty ring for each one missing. Replaces the
/// star glyphs. <paramref name="value"/> is 0..1 (or use <see cref="SetOutOfFive"/>).</summary>
public partial class VinylRating : Control {
	private int filled;
	private float diameter = 17f;

	public VinylRating Set(float unitValue, float discSize = 17f) => SetOutOfFive(Mathf.RoundToInt(Mathf.Clamp(unitValue, 0f, 1f) * 5f), discSize);

	public VinylRating SetOutOfFive(int discs, float discSize = 17f) {
		filled = Mathf.Clamp(discs, 0, 5);
		diameter = discSize;
		CustomMinimumSize = new Vector2(5 * (discSize + 3f), discSize + 2f);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Ignore;
		QueueRedraw();
		return this;
	}

	public override void _Draw() {
		for (int i = 0; i < 5; i++) {
			var centre = new Vector2(diameter / 2f + i * (diameter + 3f), Size.Y / 2f);
			float r = diameter / 2f;
			if (i < filled) {
				DrawCircle(centre + new Vector2(0.8f, 1.2f), r, new Color(0, 0, 0, 0.25f));
				DrawCircle(centre, r, new Color("14110e"));
				DrawArc(centre, r * 0.78f, 0f, Mathf.Tau, 24, new Color("3c342b"), 1f, true);
				DrawArc(centre, r * 0.58f, 0f, Mathf.Tau, 24, new Color("3c342b"), 1f, true);
				DrawCircle(centre, r * 0.38f, new Color("a8322a"));
				DrawCircle(centre, Mathf.Max(0.9f, r * 0.1f), new Color("f1e5c8"));
				DrawArc(centre, r * 0.88f, -2.5f, -1.3f, 10, new Color(1f, 1f, 1f, 0.22f), 1.4f, true);
			} else {
				DrawArc(centre, r - 0.8f, 0f, Mathf.Tau, 24, new Color("6b5a3a", 0.55f), 1.4f, true);
				DrawCircle(centre, Mathf.Max(0.9f, r * 0.1f), new Color("6b5a3a", 0.5f));
			}
		}
	}
}
