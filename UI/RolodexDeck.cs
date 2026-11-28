using System;
using Godot;

/// <summary>
/// The Rolodex as an object: the card in front, the names of the cards behind it fanned up over its top edge, and the
/// mouse wheel to turn them. Wheel down flips the front card forward and down onto the desk so the next one is in
/// front; wheel up brings the last one back up. The flip is a real fold (the card shortens to its bottom edge while it
/// darkens), with the next card already standing behind it.
///
/// The deck owns no content. The page hands it a builder that makes the card for an index, a name for the fan, and a
/// callback for when the front card changes. The wheel is read in <c>_Input</c> and swallowed over the deck, because
/// the page scroll container and the card's own buttons would otherwise take the event first.
/// </summary>
public partial class RolodexDeck : VBoxContainer {
	private const int FanDepth = 4;
	private const float FanStep = 19f, TabHeight = 19f;
	private const double FlipSeconds = .08;

	private int count, focus;
	private Func<int, Control> build;
	private Func<int, string> nameAt;
	private Action<int> onFocus;
	private FanStrip fan;
	private EdgeStrip edges;
	private MarginContainer stage;
	private Control front;
	private bool flipping;

	public int Focus => focus;
	public Action CardFlicked;

	public RolodexDeck Set(int cardCount, int focusIndex, Func<int, Control> buildCard, Func<int, string> nameOfCard, Action<int> focusChanged) {
		count = cardCount; focus = Math.Clamp(focusIndex, 0, Math.Max(0, cardCount - 1));
		build = buildCard; nameAt = nameOfCard; onFocus = focusChanged;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddThemeConstantOverride("separation", 0);

		fan = new FanStrip { Deck = this, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(fan);
		stage = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipContents = false };
		AddChild(stage);
		front = build(focus);
		stage.AddChild(front);
		edges = new EdgeStrip { Deck = this, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(edges);
		return this;
	}

	public int CardsBehind => Math.Min(FanDepth, Math.Max(0, count - 1));
	public int CardsFlipped => Math.Min(3, focus);

	/// <summary>Turns one card; +1 is wheel down (forward), -1 is wheel up. Wraps round, like the real thing.</summary>
	public void Flip(int direction) {
		if (flipping || count < 2) return;
		flipping = true;
		int next = (focus + direction + count) % count;
		Control incoming = build(next);
		Control outgoing = front;
		front = incoming;
		focus = next;
		onFocus?.Invoke(next);
		CardFlicked?.Invoke();
		fan.QueueRedraw(); edges.QueueRedraw(); edges.UpdateMinimumSize();

		bool forward = direction > 0;
		stage.AddChild(incoming);
		if (forward) stage.MoveChild(incoming, 0);
		else incoming.Modulate = Colors.Transparent;
		Callable.From(() => {
			if (!IsInsideTree() || IsQueuedForDeletion()) return;
			Control turning = forward ? outgoing : incoming;
			turning.PivotOffset = new Vector2(turning.Size.X / 2f, turning.Size.Y);
			var folded = new Color(.62f, .55f, .46f);
			float restY = turning.Position.Y;
			if (!forward) {
				turning.Scale = new Vector2(1f, .02f);
				turning.Position += new Vector2(0, 8);
				turning.Modulate = folded;
			}
			var tween = CreateTween().SetParallel(true).SetTrans(Tween.TransitionType.Quad)
				.SetEase(forward ? Tween.EaseType.In : Tween.EaseType.Out);
			tween.TweenProperty(turning, "scale:y", forward ? .02f : 1f, FlipSeconds);
			tween.TweenProperty(turning, "position:y", forward ? restY + 8 : restY, FlipSeconds);
			tween.TweenProperty(turning, "modulate", forward ? folded : Colors.White, FlipSeconds);
			tween.Finished += () => {
				stage.RemoveChild(outgoing);
				outgoing.QueueFree();
				flipping = false;
			};
		}).CallDeferred();
	}

	public override void _Input(InputEvent @event) {
		if (count < 2 || !IsVisibleInTree() || PaperModal.IsOpen) return;
		int direction;
		if (@event is InputEventKey { Pressed: true, AltPressed: false, CtrlPressed: false, MetaPressed: false, Keycode: Key.Left or Key.Right } key)
			direction = key.Keycode == Key.Right ? 1 : -1;
		else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown or MouseButton.WheelUp } mouse) {
			var point = GetGlobalMousePosition();
			if (!GetGlobalRect().HasPoint(point)) return;
			if (GetParent().GetParent() is ScrollContainer scroll && !scroll.GetGlobalRect().HasPoint(point)) return;
			direction = mouse.ButtonIndex == MouseButton.WheelDown ? 1 : -1;
		} else return;
		GetViewport().SetInputAsHandled();
		Flip(direction);
	}

	/// <summary>The index-card look every card in the book shares: a red head rule over blue ruled lines, and the long
	/// slot the Rolodex's rod passes through, cut into the bottom edge.</summary>
	public static PanelContainer Card(Control body, Color? fill = null) {
		var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var paper = PaperStyleBox.Sheet(fill ?? new Color("f5edd2"), 22, 16, 10);
		paper.HeaderRule = 56f; paper.RuleSpacing = 26f; paper.Burn = 0.8f; paper.Falloff = 0.6f;
		paper.ContentMarginBottom = 30;
		card.AddThemeStyleboxOverride("panel", paper);
		card.AddChild(body);
		var slot = new SlotOverlay { MouseFilter = MouseFilterEnum.Ignore };
		slot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		card.AddChild(slot);
		return card;
	}

	private partial class SlotOverlay : Control {
		public override void _Draw() {
			// The overlay fills the card's CONTENT area, so the slot is drawn just below it, in the bottom margin.
			var slot = new Rect2(Size.X / 2f - 46f, Size.Y + 9f, 92f, 11f);
			DrawStyleBox(new StyleBoxFlat { BgColor = new Color("3a2b18"), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 }, slot);
			DrawLine(new Vector2(slot.Position.X + 4, slot.Position.Y + 1.5f), new Vector2(slot.End.X - 4, slot.Position.Y + 1.5f), new Color(0, 0, 0, 0.6f), 2f);
		}
	}

	/// <summary>The cards standing behind the front one: each peeks FanStep higher, a little narrower, with its name on a tab
	/// that sticks up at a position of its own, the way the alphabet tabs stagger along a Rolodex card.</summary>
	private partial class FanStrip : Control {
		public RolodexDeck Deck;

		public override Vector2 _GetMinimumSize() => new(0, Deck.CardsBehind * FanStep + (Deck.CardsBehind > 0 ? TabHeight : 0f) + 2f);

		public override void _Draw() {
			int behind = Deck.CardsBehind;
			float top = Size.Y;
			for (int k = behind; k >= 1; k--) {
				float y = top - k * FanStep;
				float inset = k * 9f;
				var body = new Rect2(inset, y, Size.X - inset * 2f, FanStep + 6f);
				string name = Deck.nameAt?.Invoke((Deck.focus + k) % Deck.count) ?? "";
				float shade = 0.045f * k;
				Color paper = new Color("f5edd2").Darkened(shade);
				// the tab: staggered along the card by a hash of the name
				Font font = PaperTheme.Elite;
				int size = 15;
				float width = Mathf.Min(font.GetStringSize(name, HorizontalAlignment.Left, -1, size).X + 26f, Mathf.Max(80f, body.Size.X * 0.5f));
				uint h = Portraits.Hash(name + k);
				float room = Mathf.Max(0f, body.Size.X - width - 60f);
				float tabX = body.Position.X + 30f + room * ((h % 100u) / 100f);
				var tab = new Rect2(tabX, y - TabHeight + 3f, width, TabHeight);
				DrawRect(new Rect2(tab.Position + new Vector2(1.5f, 1.5f), tab.Size), new Color(0, 0, 0, 0.18f));
				DrawRect(tab, paper);
				DrawRect(tab, new Color("8a7048"), false, 1f);
				DrawRect(body, paper);
				DrawRect(body, new Color("8a7048"), false, 1f);
				DrawRect(new Rect2(tab.Position.X + 1, y - 0.5f, tab.Size.X - 2, 2f), paper);   // the tab grows out of the card
				DrawString(font, new Vector2(tab.Position.X + 12f, tab.Position.Y + 15f), name, HorizontalAlignment.Left, width - 20f, size, PaperTheme.Ink);
			}
		}
	}

	/// <summary>The cards already turned over, seen edge-on in a few thin strips under the front card.</summary>
	private partial class EdgeStrip : Control {
		public RolodexDeck Deck;

		public override Vector2 _GetMinimumSize() => new(0, Deck.CardsFlipped > 0 ? 6f + Deck.CardsFlipped * 5f : 2f);

		public override void _Draw() {
			for (int i = 0; i < Deck.CardsFlipped; i++) {
				float inset = 8f + i * 8f;
				var strip = new Rect2(inset, i * 5f, Size.X - inset * 2f, 4f);
				DrawRect(strip, new Color("e9dfc0").Darkened(0.04f * i));
				DrawRect(strip, new Color("8a7048"), false, 1f);
			}
		}
	}
}
