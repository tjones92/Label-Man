using System.Collections.Generic;
using Godot;

/// <summary>
/// The ten Decembers on one line: a pip for each year's sitting (met, no-show, seized, this one, still to come) running
/// to the oxblood seal of 1 January 1970. It is the milestone's place in the decade at a glance.
/// </summary>
public partial class LienDecadeStrip : Control {
	private static readonly Color Met = new("3a6b4a"), NoShow = new("b07a1c");
	private IReadOnlyList<PlayerDesk.LienYearOutcome> outcomes = new List<PlayerDesk.LienYearOutcome>();
	private int now;
	private bool reckoning;
	private const int Years = 10;

	/// <param name="pastOutcomes">One outcome per year already settled, oldest first.</param>
	/// <param name="nowIndex">The pip this sitting is for (0 = 1960); past <see cref="Years"/> means the reckoning.</param>
	public LienDecadeStrip Set(IReadOnlyList<PlayerDesk.LienYearOutcome> pastOutcomes, int nowIndex, bool isReckoning) {
		outcomes = pastOutcomes; now = nowIndex; reckoning = isReckoning;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		MouseFilter = MouseFilterEnum.Ignore;
		QueueRedraw();
		return this;
	}

	public override Vector2 _GetMinimumSize() => new(560f, 74f);

	public override void _Notification(int what) { if (what == NotificationResized) QueueRedraw(); }

	public override void _Draw() {
		Font typed = PaperTheme.Typed, bold = PaperTheme.TypedBold;
		float left = 30f, sealX = Size.X - 58f, last = sealX - 82f, step = (last - left) / (Years - 1), y = 27f;
		DrawLine(new Vector2(left, y), new Vector2(sealX - 26f, y), new Color(PaperTheme.Edge, 0.55f), 2f);
		for (int i = 0; i < Years; i++) {
			var c = new Vector2(left + i * step, y);
			bool settled = i < outcomes.Count;
			bool here = i == now && !reckoning;
			if (here) DrawCircle(c, 19f, new Color(PaperTheme.Highlighter, 0.55f));
			if (settled) Pip(c, outcomes[i]);
			else if (here) {
				DrawCircle(c, 12f, PaperTheme.Highlighter);
				DrawArc(c, 12f, 0f, Mathf.Tau, 28, PaperTheme.Rust, 3f, true);
				DrawCircle(c, 3.5f, PaperTheme.Rust);
			} else DrawArc(c, 11f, 0f, Mathf.Tau, 28, new Color(PaperTheme.Edge, 0.7f), 1.5f, true);
			string year = "’" + (60 + i);
			DrawString(here ? bold : typed, new Vector2(c.X - 20f, y + 40f), year, HorizontalAlignment.Center, 40f, 13, here ? PaperTheme.Rust : new Color(PaperTheme.Fade, 0.85f));
		}
		// The seal at the end of the line: the reckoning.
		var seal = new Rect2(sealX - 30f, y - 17f, 60f, 34f);
		Color wax = reckoning ? PaperTheme.Oxblood : new Color(PaperTheme.Oxblood, 0.62f);
		if (reckoning) DrawRect(seal.Grow(5f), new Color(PaperTheme.Highlighter, 0.55f));
		DrawRect(seal, wax);
		DrawRect(seal.Grow(-3f), new Color(1, 1, 1, 0.5f), false, 1f);
		DrawString(bold, new Vector2(seal.Position.X, y + 6f), "1970", HorizontalAlignment.Center, seal.Size.X, 17, Colors.White);
		DrawString(reckoning ? bold : typed, new Vector2(sealX - 50f, y + 40f), "RECKONING", HorizontalAlignment.Center, 100f, 12, reckoning ? PaperTheme.Oxblood : new Color(PaperTheme.Fade, 0.85f));
	}

	private void Pip(Vector2 c, PlayerDesk.LienYearOutcome outcome) {
		Color fill = outcome switch { PlayerDesk.LienYearOutcome.Met => Met, PlayerDesk.LienYearOutcome.NoShow => NoShow, _ => PaperTheme.Oxblood };
		DrawCircle(c, 12f, fill);
		DrawArc(c, 12f, 0f, Mathf.Tau, 28, new Color(PaperTheme.Ink, 0.7f), 1.2f, true);
		var white = new Color(1, 1, 1, 0.95f);
		switch (outcome) {
			case PlayerDesk.LienYearOutcome.Met:
				DrawPolyline(new[] { c + new Vector2(-5f, 0.5f), c + new Vector2(-1.5f, 4.5f), c + new Vector2(5.5f, -4.5f) }, white, 2.6f, true);
				break;
			case PlayerDesk.LienYearOutcome.Seized:
				DrawLine(c + new Vector2(-4.5f, -4.5f), c + new Vector2(4.5f, 4.5f), white, 2.6f, true);
				DrawLine(c + new Vector2(-4.5f, 4.5f), c + new Vector2(4.5f, -4.5f), white, 2.6f, true);
				break;
			default:
				DrawLine(c + new Vector2(0f, -6f), c + new Vector2(0f, 2f), white, 2.8f, true);
				DrawCircle(c + new Vector2(0f, 5.5f), 1.7f, white);
				break;
		}
	}
}
