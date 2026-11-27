using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The voices as pop-overs (SimTools/OnboardingTutorialDirective.md §4b, T6): while the first-week card is up, a slip of
/// paper sits beside whatever glows, with a pencil arrow to each glowing control, and the voice whose job it is says what
/// to press, in the register of a voice trying to steer a hungover body (<see cref="PlayerDesk.FirstWeekNudge"/>).
///
/// It never takes the mouse, apart from its own ×, which puts it away until the glow moves on. It lives on its own
/// canvas layer above the paper modals (layer 60), since the glow is often on a modal's button. It's built once and
/// moved on the first-week watch's tick, so it adds no nodes while it follows the glow.
/// </summary>
public partial class PlayerDeskPanel {
	private CanvasLayer firstWeekSlipLayer;
	private PanelContainer firstWeekSlip;
	private LinocutIcon firstWeekSlipBadge;
	private RichTextLabel firstWeekSlipText;
	private Control firstWeekArrows;
	private readonly List<Rect2> firstWeekArrowTargets = new();
	/// <summary>The glow the player put the slip away for; it comes back when the next thing to press changes.</summary>
	private string firstWeekSlipDismissedFor;
	private const float FirstWeekSlipWidth = 330f;

	private void BuildFirstWeekSlip() {
		if (firstWeekSlipLayer != null) return;
		firstWeekSlipLayer = new CanvasLayer { Layer = 61, Name = "FirstWeekSlipLayer" };
		AddChild(firstWeekSlipLayer);

		// The arrows first, so the slip covers their tails.
		firstWeekArrows = new Control { MouseFilter = MouseFilterEnum.Ignore, Name = "FirstWeekArrows" };
		firstWeekArrows.SetAnchorsPreset(LayoutPreset.FullRect);
		firstWeekArrows.Draw += DrawFirstWeekArrows;
		firstWeekSlipLayer.AddChild(firstWeekArrows);

		firstWeekSlip = new PanelContainer { Name = "FirstWeekSlip", Visible = false, MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(FirstWeekSlipWidth, 0) };
		var paper = PaperStyleBox.Sheet(new Color("f6edcf"), 12, 9, 10, new Color("8a7048"));
		paper.ShadowAlpha = 0.45f; paper.Radius = 1; paper.BorderWidth = 1;
		firstWeekSlip.AddThemeStyleboxOverride("panel", paper);
		firstWeekSlip.Theme = Theme;   // a CanvasLayer doesn't inherit the window's theme
		var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 8);
		firstWeekSlipBadge = new LinocutIcon { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin };
		row.AddChild(firstWeekSlipBadge);
		firstWeekSlipText = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore,
			// A width to wrap at before the first layout; without it the text wraps a letter a line and the slip is a column.
			CustomMinimumSize = new Vector2(FirstWeekSlipWidth - 80, 0) };
		firstWeekSlipText.AddThemeFontOverride("normal_font", PaperTheme.SerifItalic);
		firstWeekSlipText.AddThemeFontOverride("bold_font", PaperTheme.SansBold);
		firstWeekSlipText.AddThemeFontSizeOverride("normal_font_size", 16);
		firstWeekSlipText.AddThemeFontSizeOverride("bold_font_size", 12);
		row.AddChild(firstWeekSlipText);
		var close = new Button { Text = "×", Flat = true, Name = "FirstWeekSlipClose", SizeFlagsVertical = SizeFlags.ShrinkBegin,
			TooltipText = "Put this away until the next step.", FocusMode = FocusModeEnum.None };
		close.SetMeta("first_session_ok", true);   // it hides the slip: not a dead click in the first-session trace
		close.AddThemeColorOverride("font_color", new Color("6b5d45"));
		close.AddThemeFontSizeOverride("font_size", 18);
		close.Pressed += () => { firstWeekSlipDismissedFor = FirstWeekSlipKey(); HideFirstWeekSlip(); };
		row.AddChild(close);
		firstWeekSlip.AddChild(row);
		firstWeekSlipLayer.AddChild(firstWeekSlip);
	}

	private void HideFirstWeekSlip() {
		if (firstWeekSlip != null) firstWeekSlip.Visible = false;
		firstWeekArrowTargets.Clear();
		firstWeekArrows?.QueueRedraw();
	}

	/// <summary>What the slip is about: the glowing controls, by identity, so a rebuilt page with the same step keeps it put
	/// away only while the same thing glows.</summary>
	private string FirstWeekSlipKey() => string.Join("|", firstWeekGlowSet.Where(IsInstanceValid).Select(SlipTarget));

	/// <summary>The glowing control as the nudge reads it: a button's text, or what a box or a blank is for.</summary>
	private static string SlipTarget(Control c) => c switch {
		CheckBox b when b.HasMeta("first_week_song") => "song",
		CheckBox => "give",
		Button b => b.Text?.Trim() ?? "",
		_ => "blank",
	};

	/// <summary>Called on every first-week tick after the glow is placed: show, move or hide the slip.</summary>
	private void PlaceFirstWeekSlip(PlayerDesk desk) {
		var hits = firstWeekGlowSet.Where(c => IsInstanceValid(c) && c.IsVisibleInTree() && SlipAnchor(c) != null).ToList();
		if (desk?.FirstWeekCurrent == null || hits.Count == 0 || !IsVisibleInTree()) { HideFirstWeekSlip(); return; }
		string key = FirstWeekSlipKey();
		if (key == firstWeekSlipDismissedFor) { HideFirstWeekSlip(); return; }
		firstWeekSlipDismissedFor = null;
		var nudge = desk.FirstWeekNudge(SlipTarget(hits[0]), hits.Count > 1);
		if (nudge is not { } n) { HideFirstWeekSlip(); return; }

		BuildFirstWeekSlip();
		firstWeekSlipBadge.Set(VoiceInstinct(n.Voice), 24f);
		firstWeekSlipText.AddThemeColorOverride("default_color", VoiceColor(n.Voice));
		string text = $"[b]{VoiceName(n.Voice)}[/b]   {n.Text}";
		if (firstWeekSlipText.Text != text) { firstWeekSlipText.Text = text; firstWeekSlip.ResetSize(); }
		firstWeekSlip.Visible = true;

		// Beside the glow: on whichever side has room, level with the first glowing control, kept inside the window.
		// The ones in view; if none are, one arrow to the page's edge where the first one is.
		var targets = hits.Select(c => (c, a: SlipAnchor(c).Value)).Where(x => x.a == x.c.GetGlobalRect()).Select(x => x.a).ToList();
		bool offPage = targets.Count == 0;
		if (offPage) targets.Add(SlipAnchor(hits[0]).Value);
		Rect2 all = targets.Aggregate((a, b) => a.Merge(b));
		Vector2 view = GetViewportRect().Size, size = firstWeekSlip.Size;
		const float gap = 54f;
		float x = all.Position.X - gap - size.X >= 8 ? all.Position.X - gap - size.X
			: all.End.X + gap + size.X <= view.X - 8 ? all.End.X + gap
			: Mathf.Clamp(all.GetCenter().X - size.X / 2, 8, Mathf.Max(8, view.X - size.X - 8));
		bool beside = x + size.X <= all.Position.X || x >= all.End.X;
		float y = beside ? targets[0].GetCenter().Y - size.Y / 2
			: all.Position.Y - gap - size.Y >= 8 ? all.Position.Y - gap - size.Y : all.End.Y + gap;
		// Scrolled out of sight: the slip sits on the page just inside that edge, over where it is, and the arrow points
		// straight off the page ("down there"), never sideways at whatever happens to be beside it.
		if (offPage && contentScroll != null) {
			Rect2 page = contentScroll.GetGlobalRect(), edge = targets[0];
			bool below = edge.Position.Y >= page.GetCenter().Y;
			x = Mathf.Clamp(edge.GetCenter().X - size.X / 2, page.Position.X + 8, Mathf.Max(page.Position.X + 8, page.End.X - size.X - 8));
			y = below ? page.End.Y - size.Y - 34 : page.Position.Y + 34;
			targets[0] = new Rect2(edge.GetCenter().X - 1, below ? page.End.Y + 4 : page.Position.Y - 10, 2, 6);
		}
		firstWeekSlip.Position = new Vector2(x, Mathf.Clamp(y, 8, Mathf.Max(8, view.Y - size.Y - 8)));

		firstWeekArrowTargets.Clear();
		firstWeekArrowTargets.AddRange(targets.Take(4));
		firstWeekArrows.QueueRedraw();
	}

	/// <summary>Where the arrow points for a glowing control: the control itself when it's on screen; a sliver at the page's
	/// top or bottom edge, level with it, when it's scrolled out of sight (the terms sheet's TABLE OFFER sits below the fold),
	/// so the slip says "down there"; null when it's nowhere to be seen.</summary>
	private Rect2? SlipAnchor(Control c) {
		Rect2 r = c.GetGlobalRect();
		if (contentScroll == null || !contentScroll.IsAncestorOf(c)) return GetViewportRect().Intersects(r) ? r : null;
		Rect2 page = contentScroll.GetGlobalRect();
		if (page.Grow(2).Encloses(r) || page.Intersects(r) && r.Size.Y > page.Size.Y) return r;
		float x = Mathf.Clamp(r.Position.X, page.Position.X, page.End.X - r.Size.X);
		return r.Position.Y >= page.End.Y - 4 ? new Rect2(x, page.End.Y - 6, r.Size.X, 6)
			: r.End.Y <= page.Position.Y + 4 ? new Rect2(x, page.Position.Y, r.Size.X, 6)
			: new Rect2(x, Mathf.Clamp(r.Position.Y, page.Position.Y, page.End.Y - 6), r.Size.X, 6);
	}

	/// <summary>A pencil arrow from the slip's nearest edge to each glowing control: a slight curve, in the ring's blue.</summary>
	private void DrawFirstWeekArrows() {
		if (firstWeekSlip == null || !firstWeekSlip.Visible) return;
		Rect2 slip = firstWeekSlip.GetRect();
		foreach (Rect2 target in firstWeekArrowTargets) {
			Vector2 to = ClosestEdgePoint(target.Grow(7), slip.GetCenter());
			Vector2 from = ClosestEdgePoint(slip, to);
			Vector2 mid = (from + to) / 2 + (to - from).Orthogonal().Normalized() * Mathf.Min(18f, from.DistanceTo(to) * 0.15f);
			var points = new Vector2[13];
			for (int i = 0; i <= 12; i++) {
				float t = i / 12f;
				points[i] = (1 - t) * (1 - t) * from + 2 * (1 - t) * t * mid + t * t * to;
			}
			firstWeekArrows.DrawPolyline(points, FirstWeekRingInk, 3f, true);
			Vector2 dir = (to - points[10]).Normalized();
			Vector2 side = dir.Orthogonal() * 7f;
			firstWeekArrows.DrawColoredPolygon(new[] { to, to - dir * 14f + side, to - dir * 14f - side }, FirstWeekRingInk);
		}
	}

	private static Vector2 ClosestEdgePoint(Rect2 r, Vector2 p) {
		Vector2 c = new(Mathf.Clamp(p.X, r.Position.X, r.End.X), Mathf.Clamp(p.Y, r.Position.Y, r.End.Y));
		if (c != p) return c;
		// Inside: out to the nearest edge.
		float l = p.X - r.Position.X, rr = r.End.X - p.X, t = p.Y - r.Position.Y, b = r.End.Y - p.Y;
		float m = Mathf.Min(Mathf.Min(l, rr), Mathf.Min(t, b));
		return m == l ? new Vector2(r.Position.X, p.Y) : m == rr ? new Vector2(r.End.X, p.Y) : m == t ? new Vector2(p.X, r.Position.Y) : new Vector2(p.X, r.End.Y);
	}
}
