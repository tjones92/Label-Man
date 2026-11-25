using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The December sitting as a scene of its own (SimTools/LabelManInnerLifeDirective.md §3.2). While the holder's lawyer is
/// in the office the sitting takes the whole screen: the desk is dimmed and unreachable, the clock can't be touched, and
/// the page is a title plate over the ten Decembers, the lien as a stamped document, the lawyer's lines laid in one at
/// a time, and the three bars as cards that set your real state against his. A choice that follows you to 1970 asks first.
/// When it's settled a stamp comes down on the paper and the screen says what was entered and what next year will ask.
/// The rules stay on <see cref="PlayerDesk"/>; this only draws them.
/// </summary>
public partial class PlayerDeskPanel {
	/// <summary>Lines fade in one after another. Off for headless runs; a check can switch it off to capture a settled frame.</summary>
	public static bool LienStageAnimates = true;

	private static readonly Color StageGreen = new("3a6b4a");
	private static readonly Color StageDanger = new("9a2b1a");
	private const float LienLineGap = 0.5f;

	private CanvasLayer lienStage;
	private PlayerDesk.LienSittingLive lienStageLive;
	private ScrollContainer lienStageScroll;
	private VBoxContainer lienStageHeader, lienStageLeft, lienStageTranscript, lienStageActions;
	private HBoxContainer lienStageFooter;
	private int lienLinesShown, lienLedgerAtOpen;
	private PlayerDesk.LienVerb? lienConfirm;
	private bool lienStampPlayed;
	private Tween lienActionsFade;
	private Control lienArrival;
	private readonly List<Control> lienHeld = new();
	private bool lienActionsHeld;

	private static bool StageAnimates => LienStageAnimates && DisplayServer.GetName() != "headless";

	// ── lifecycle ───────────────────────────────────────────────────────────────────────────────────

	private void SyncLienStage(PlayerDesk desk) {
		var live = desk?.ActiveSitting;
		if (live == null) { CloseLienStage(); return; }
		if (lienStage == null || !IsInstanceValid(lienStage) || lienStageLive != live) { CloseLienStage(); OpenLienStage(desk, live); }
		RenderLienStage(desk, live);
	}

	private void CloseLienStage() {
		lienActionsFade?.Kill();
		lienActionsFade = null;
		if (lienStage != null && IsInstanceValid(lienStage)) {
			lienStage.GetParent()?.RemoveChild(lienStage);
			lienStage.QueueFree();
		}
		lienStage = null; lienStageLive = null; lienConfirm = null;
		lienArrival = null; lienHeld.Clear(); lienActionsHeld = false;
	}

	private void OpenLienStage(PlayerDesk desk, PlayerDesk.LienSittingLive live) {
		lienStageLive = live; lienLinesShown = 0; lienConfirm = null; lienStampPlayed = false;
		lienLedgerAtOpen = desk.Lien.Ledger.Count;

		// Below the paper modals (60), so a confirmation from elsewhere would still sit on top.
		lienStage = new CanvasLayer { Layer = 58 };
		var root = new Control { MouseFilter = MouseFilterEnum.Stop, Theme = PaperTheme.Build() };
		root.SetAnchorsPreset(LayoutPreset.FullRect);
		lienStage.AddChild(root);

		var dim = new ColorRect { Color = new Color(0.06f, 0.04f, 0.025f, 0.93f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(dim);

		var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_top", 26);
		margin.AddThemeConstantOverride("margin_bottom", 26);
		void Sides() {
			int side = Mathf.Max(30, (int)((root.Size.X - 1200f) / 2f));
			margin.AddThemeConstantOverride("margin_left", side);
			margin.AddThemeConstantOverride("margin_right", side);
		}
		Sides();
		root.Resized += Sides;
		root.AddChild(margin);

		var card = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		var sheet = PaperStyleBox.Sheet(PaperTheme.Paper, 34, 18, 26, PaperTheme.Rust).Decorated(false, false, 5);
		sheet.BorderWidth = 3; sheet.Radius = 3; sheet.ShadowAlpha = 0.6f;
		card.AddThemeStyleboxOverride("panel", sheet);
		margin.AddChild(card);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 8);
		card.AddChild(column);

		lienStageHeader = new VBoxContainer();
		lienStageHeader.AddThemeConstantOverride("separation", 4);
		column.AddChild(lienStageHeader);
		column.AddChild(new ColorRect { Color = new Color("b99759"), CustomMinimumSize = new Vector2(0, 2) });

		lienStageScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		column.AddChild(lienStageScroll);
		var body = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 26);
		lienStageScroll.AddChild(body);

		lienStageLeft = new VBoxContainer { CustomMinimumSize = new Vector2(310, 0) };
		lienStageLeft.AddThemeConstantOverride("separation", 12);
		body.AddChild(lienStageLeft);

		var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		right.AddThemeConstantOverride("separation", 16);
		body.AddChild(right);

		var frame = new PanelContainer();
		frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("eadcb8"), BorderColor = new Color("c9b48a"),
			BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
			ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 14, ContentMarginBottom = 14,
		});
		lienStageTranscript = new VBoxContainer();
		lienStageTranscript.AddThemeConstantOverride("separation", 12);
		frame.AddChild(lienStageTranscript);
		right.AddChild(frame);

		lienStageActions = new VBoxContainer();
		lienStageActions.AddThemeConstantOverride("separation", 10);
		right.AddChild(lienStageActions);

		column.AddChild(new ColorRect { Color = new Color("b99759"), CustomMinimumSize = new Vector2(0, 1) });
		lienStageFooter = new HBoxContainer();
		lienStageFooter.AddThemeConstantOverride("separation", 14);
		column.AddChild(lienStageFooter);

		AddChild(lienStage);
		if (StageAnimates) BuildLienArrival(desk, root);
	}

	private void RenderLienStage(PlayerDesk desk, PlayerDesk.LienSittingLive live) {
		if (lienStage == null || !IsInstanceValid(lienStage)) return;
		var p = desk.LienProfile;
		bool firstRender = lienLinesShown == 0;
		Clear(lienStageHeader); Clear(lienStageLeft); Clear(lienStageActions); Clear(lienStageFooter);

		StageHeader(desk, p);
		StagePaper(desk, p);
		int fresh = AppendLienLines(live, p);
		switch (live.stage) {
			case CallStage.Open: StageOffers(desk, live); break;
			case CallStage.Pushback: StageCounters(desk, live); break;
			default: StageOutcome(desk, live); break;
		}
		StageFooter(desk, live);

		// The offers wait for the lawyer to finish, and the lawyer waits for the door: while he is still arriving, lines and offers are held.
		lienActionsFade?.Kill();
		lienActionsFade = null;
		lienStageActions.Modulate = Colors.White;
		bool arriving = lienArrival != null && IsInstanceValid(lienArrival);
		if (StageAnimates && (fresh > 0 || arriving)) {
			lienStageActions.Modulate = new Color(1, 1, 1, 0);
			lienActionsHeld = true;
		}
		if (!arriving) FlushLienReveal();
		if (fresh > 0 && !firstRender) ScrollLienStageToEnd();
	}

	/// <summary>Lets the held lines fade in one after another, then the offers.</summary>
	private void FlushLienReveal() {
		int count = lienHeld.Count;
		for (int i = 0; i < count; i++) {
			var held = lienHeld[i];
			if (IsInstanceValid(held)) held.CreateTween().TweenProperty(held, "modulate:a", 1.0f, 0.5).SetDelay(i * LienLineGap);
		}
		lienHeld.Clear();
		if (!lienActionsHeld) return;
		lienActionsHeld = false;
		lienActionsFade?.Kill();
		lienActionsFade = lienStageActions.CreateTween();
		lienActionsFade.TweenProperty(lienStageActions, "modulate:a", 1.0f, 0.45).SetDelay(count * LienLineGap + 0.1);
	}

	// ── the knock ───────────────────────────────────────────────────────────────────────────────────

	/// <summary>The lawyer arrives: a dark screen, a door, how he knocks, and then he is in the chair. Click or Escape skips it.
	/// The lines and offers are held until the door has done its work.</summary>
	private void BuildLienArrival(PlayerDesk desk, Control root) {
		bool reckoning = desk.Lien.IsReckoning;
		var overlay = new Control { MouseFilter = MouseFilterEnum.Stop };
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(overlay);
		lienArrival = overlay;
		var black = new ColorRect { Color = new Color(0.015f, 0.012f, 0.01f), MouseFilter = MouseFilterEnum.Ignore };
		black.SetAnchorsPreset(LayoutPreset.FullRect);
		overlay.AddChild(black);
		var door = new LienDoor();
		overlay.AddChild(door);

		var line = StageLabel(desk.LienArrivalLine(), PaperTheme.SerifItalic, 24, new Color("e6d6a8"), true, HorizontalAlignment.Center);
		line.SetAnchorsPreset(LayoutPreset.BottomWide);
		line.OffsetLeft = 260; line.OffsetRight = -260; line.OffsetTop = -150; line.OffsetBottom = -70;
		line.Modulate = new Color(1, 1, 1, 0);
		overlay.AddChild(line);

		var skip = new Button {
			Text = "SKIP  ·  ESC", MouseDefaultCursorShape = CursorShape.PointingHand, ShortcutInTooltip = false,
			Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } },
		};
		overlay.AddChild(skip);
		skip.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
		skip.OffsetLeft = -190; skip.OffsetRight = -26; skip.OffsetTop = -64; skip.OffsetBottom = -24;

		bool ended = false;
		void At(float seconds, Action what) {
			var tween = overlay.CreateTween();
			tween.TweenInterval(seconds);
			tween.TweenCallback(Callable.From(() => { if (!ended && IsInstanceValid(overlay)) what(); }));
		}
		void Animate(float from, float to, float seconds, Action<float> set) {
			if (IsInstanceValid(overlay)) overlay.CreateTween().TweenMethod(Callable.From(set), from, to, seconds);
		}
		void Knock(float strength) {
			OfficeFoley.Play(overlay, "knock", -2f);
			Animate(strength, 0f, 0.2f, door.SetShake);
		}
		void End() {
			if (ended) return;
			ended = true;
			if (!IsInstanceValid(overlay)) return;
			var fade = overlay.CreateTween();
			fade.TweenProperty(overlay, "modulate:a", 0.0f, 0.7);
			fade.TweenCallback(Callable.From(() => {
				lienArrival = null;
				if (IsInstanceValid(overlay)) overlay.QueueFree();
				if (lienStage != null && lienStageLive != null) FlushLienReveal();
			}));
		}
		skip.Pressed += End;
		overlay.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true }) End(); };

		if (reckoning) {
			// No knock: he is already standing in the open door.
			door.SetOpen(1f); door.SetVisitor(1f);
			OfficeFoley.Play(overlay, "door", -6f);
			At(0.3f, () => Animate(0f, 1f, 0.8f, v => line.Modulate = new Color(1, 1, 1, v)));
			At(3.2f, End);
			return;
		}
		At(0.7f, () => Knock(7f));
		At(1.05f, () => Knock(-6f));
		At(1.4f, () => Knock(5f));
		At(0.9f, () => Animate(0f, 1f, 0.9f, v => line.Modulate = new Color(1, 1, 1, v)));
		At(2.7f, () => { OfficeFoley.Play(overlay, "door", -3f); Animate(0f, 1f, 1.0f, door.SetOpen); });
		At(3.4f, () => Animate(0f, 1f, 0.6f, door.SetVisitor));
		At(4.6f, End);
	}

	private async void ScrollLienStageToEnd() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (lienStageScroll != null && IsInstanceValid(lienStageScroll))
			lienStageScroll.ScrollVertical = (int)lienStageScroll.GetVScrollBar().MaxValue;
	}

	// ── small builders ──────────────────────────────────────────────────────────────────────────────

	private static Label StageLabel(string text, Font font, int size, Color color, bool wrap = true, HorizontalAlignment align = HorizontalAlignment.Left) {
		var label = new Label { Text = text, HorizontalAlignment = align, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		if (wrap) label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static StyleBoxFlat StageBox(Color bg, Color border, int borderWidth, int marginX, int marginY) => new() {
		BgColor = bg, BorderColor = border,
		BorderWidthTop = borderWidth, BorderWidthBottom = borderWidth, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth,
		ContentMarginLeft = marginX, ContentMarginRight = marginX, ContentMarginTop = marginY, ContentMarginBottom = marginY,
	};

	private static void StageHeading(Container into, string text) {
		into.AddChild(StageLabel(text, PaperTheme.Elite, 16, PaperTheme.Rust, false));
		into.AddChild(new ColorRect { Color = new Color(PaperTheme.Edge, 0.45f), CustomMinimumSize = new Vector2(0, 1) });
	}

	private static string StageMoney(float amount) => $"${Math.Max(0f, amount):N0}";

	// ── the title plate and the decade ──────────────────────────────────────────────────────────────

	private void StageHeader(PlayerDesk desk, PlayerDesk.LienHolderProfile p) {
		var lien = desk.Lien;
		bool lastChance = lien.ConsecutiveFailures > 0 && !lien.IsReckoning;
		string title = desk.ActiveSitting.IsFloriaBuyout ? "The Buyout" : lien.IsReckoning ? "The Reckoning" : lastChance ? "The Last Chance" : "The December Sitting";
		string sitting = lien.IsReckoning ? "THE END OF THE PAPER" : $"SITTING {Math.Clamp(lien.BarYear - 1959, 1, 10)} OF 10";
		string date = TimeManager.Instance?.CurrentDate.ToLongString() ?? "";

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 20);
		var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		left.AddThemeConstantOverride("separation", 0);
		left.AddChild(StageLabel($"{p.Lawyer.ToUpperInvariant()}, FOR {p.Name.ToUpperInvariant()}", PaperTheme.Elite, 14, PaperTheme.Fade, false));
		left.AddChild(StageLabel(title, PaperTheme.Lettering(LetteringStyle.Didone), 42, PaperTheme.Rust, false));
		row.AddChild(left);
		var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkEnd, SizeFlagsVertical = SizeFlags.ShrinkEnd };
		right.AddThemeConstantOverride("separation", 2);
		right.AddChild(StageLabel(sitting, PaperTheme.Elite, 18, lien.IsReckoning ? PaperTheme.Oxblood : PaperTheme.Rust, false, HorizontalAlignment.Right));
		right.AddChild(StageLabel(date, PaperTheme.Typed, 14, PaperTheme.Fade, false, HorizontalAlignment.Right));
		row.AddChild(right);
		lienStageHeader.AddChild(row);

		var outcomes = desk.LienYearOutcomes();
		int now = lien.IsReckoning ? 10 : Math.Clamp(lien.Extended ? outcomes.Count - 1 : outcomes.Count, 0, 9);
		lienStageHeader.AddChild(new LienDecadeStrip().Set(outcomes, now, lien.IsReckoning));
	}

	// ── the lien as a document ──────────────────────────────────────────────────────────────────────

	private void StagePaper(PlayerDesk desk, PlayerDesk.LienHolderProfile p) {
		var lien = desk.Lien;
		var paper = new PanelContainer();
		paper.AddThemeStyleboxOverride("panel", StageBox(PaperTheme.Field, PaperTheme.Edge, 2, 16, 14));
		lienStageLeft.AddChild(paper);
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 8);
		paper.AddChild(box);

		box.AddChild(StageLabel("THE LIEN", PaperTheme.Elite, 24, PaperTheme.Rust, false));
		box.AddChild(StageLabel($"Signed {lien.Signed.ToHeadlineString()}", PaperTheme.SerifItalic, 14, PaperTheme.Fade, false));
		box.AddChild(StageLabel($"Held by {p.Name}, {p.Title}", PaperTheme.Serif, 15, PaperTheme.Ink));
		box.AddChild(new ColorRect { Color = new Color(PaperTheme.Edge, 0.5f), CustomMinimumSize = new Vector2(0, 1) });

		GameDate due = lien.PendingDeadline ?? lien.Deadline;
		var stamp = new LienStamp();
		if (lien.Extended) stamp.Set("Extended", "to " + due.ToHeadlineString(), StageGreen, -0.05f);
		else if (lien.IsReckoning) stamp.Set("The reckoning", lien.Deadline.ToHeadlineString(), PaperTheme.Oxblood, -0.05f);
		else stamp.Set("Falls due", lien.Deadline.ToHeadlineString(), PaperTheme.Oxblood, -0.05f);
		box.AddChild(stamp);

		void Row(string name, string value, bool total = false) {
			var line = new HBoxContainer();
			line.AddChild(StageLabel(name, total ? PaperTheme.SerifBold : PaperTheme.Serif, 15, total ? PaperTheme.Ink : PaperTheme.Fade, false));
			var amount = StageLabel(value, total ? PaperTheme.TypedBold : PaperTheme.Typed, 16, PaperTheme.Ink, false, HorizontalAlignment.Right);
			amount.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
			line.AddChild(amount);
			box.AddChild(line);
		}
		Row("Face sum", StageMoney(lien.FaceSum));
		if (lien.PrincipalPaid > 0.5f) Row("Paid down", StageMoney(lien.PrincipalPaid));
		Row("Interest", lien.Rate <= 0f ? "none" : $"{lien.Rate:P0} a year");
		float interestBy = lien.InterestDueBy(lien.Deadline);
		if (interestBy > 0.5f) Row("Interest owed", StageMoney(interestBy));
		box.AddChild(new ColorRect { Color = new Color(PaperTheme.Ink, 0.7f), CustomMinimumSize = new Vector2(0, 1.5f) });
		Row("Outstanding", StageMoney(lien.Outstanding), total: true);

		if (desk.ActiveSitting.IsFloriaBuyout && !desk.IsGameOver)
			AddStageWarning(lienStageLeft, "The firm's purchase option takes effect on the deadline if you leave it unsigned.");
		else if (lien.IsReckoning)
			AddStageWarning(lienStageLeft, "At the reckoning only money counts: the whole balance, in cash, or he takes the label.");
		else if (lien.ConsecutiveFailures > 0 && !lien.Extended)
			AddStageWarning(lienStageLeft, $"You missed the last deadline. Miss this one and {p.Name} takes the label.");

		if (lien.Extended && !lienStampPlayed && StageAnimates) {
			// The stamp comes down on the paper.
			lienStampPlayed = true;
			stamp.Modulate = new Color(1, 1, 1, 0);
			stamp.Scale = new Vector2(1.7f, 1.7f);
			var tween = stamp.CreateTween().SetParallel(true);
			tween.TweenProperty(stamp, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In).SetDelay(0.25);
			tween.TweenProperty(stamp, "modulate:a", 1.0f, 0.12).SetDelay(0.25);
		} else if (lien.Extended) lienStampPlayed = true;
	}

	private static void AddStageWarning(Container into, string text) {
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", StageBox(new Color(PaperTheme.Oxblood, 0.08f), PaperTheme.Oxblood, 2, 12, 10));
		panel.AddChild(StageLabel(text, PaperTheme.SerifBold, 15, PaperTheme.Oxblood));
		into.AddChild(panel);
	}

	// ── the lawyer's lines ──────────────────────────────────────────────────────────────────────────

	private int AppendLienLines(PlayerDesk.LienSittingLive live, PlayerDesk.LienHolderProfile p) {
		int fresh = 0;
		for (; lienLinesShown < live.transcript.Count; lienLinesShown++) {
			Control line = StageLine(live.transcript[lienLinesShown]);
			lienStageTranscript.AddChild(line);
			if (StageAnimates) {
				line.Modulate = new Color(1, 1, 1, 0);
				lienHeld.Add(line);
			}
			fresh++;
		}
		return fresh;
	}

	private Control StageLine(CallLine line) {
		string quoted = $"“{line.text.Trim('“', '”', '"')}”";
		var voiceColor = VoiceColor(line.voice);

		if (line.isPlayer) {
			var wrap = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			wrap.AddThemeConstantOverride("margin_left", 70);
			var v = new VBoxContainer();
			v.AddThemeConstantOverride("separation", 2);
			string who = line.voice == ExecutiveVoice.None ? "YOU" : $"YOU  ·  {VoiceName(line.voice)}";
			v.AddChild(StageLabel(who, PaperTheme.Elite, 13, new Color("4a3a6b"), false, HorizontalAlignment.Right));
			v.AddChild(StageLabel(quoted, PaperTheme.Serif, 17, new Color("4a3a6b"), true, HorizontalAlignment.Right));
			wrap.AddChild(v);
			return wrap;
		}

		if (line.voice != ExecutiveVoice.None) {
			// An instinct read: a note in the margin, in your own head and not on the record.
			var wrap = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			wrap.AddThemeConstantOverride("margin_left", 28);
			var note = new PanelContainer();
			var style = StageBox(new Color(0.55f, 0.42f, 0.2f, 0.10f), new Color(voiceColor, 0.75f), 0, 12, 8);
			style.BorderWidthLeft = 4;
			note.AddThemeStyleboxOverride("panel", style);
			var h = new HBoxContainer();
			h.AddThemeConstantOverride("separation", 12);
			var badge = new LinocutIcon().Set(VoiceInstinct(line.voice), 32f);
			badge.MouseFilter = MouseFilterEnum.Ignore;
			badge.SizeFlagsVertical = SizeFlags.ShrinkBegin;
			h.AddChild(badge);
			var v = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			v.AddThemeConstantOverride("separation", 0);
			v.AddChild(StageLabel(VoiceName(line.voice), PaperTheme.Elite, 12, voiceColor, false));
			v.AddChild(StageLabel(line.text, PaperTheme.SerifItalic, 16, voiceColor));
			h.AddChild(v);
			note.AddChild(h);
			wrap.AddChild(note);
			return wrap;
		}

		if (!string.IsNullOrEmpty(line.speaker)) {
			var v = new VBoxContainer();
			v.AddThemeConstantOverride("separation", 2);
			v.AddChild(StageLabel(line.speaker.ToUpperInvariant(), PaperTheme.Elite, 13, PaperTheme.Rust, false));
			v.AddChild(StageLabel(quoted, PaperTheme.Serif, 19, PaperTheme.Ink));
			return v;
		}

		return StageLabel(line.text, PaperTheme.SerifItalic, 17, new Color("4a3f2a"));
	}

	// ── the three bars ──────────────────────────────────────────────────────────────────────────────

	private void StageOffers(PlayerDesk desk, PlayerDesk.LienSittingLive live) {
		var lien = desk.Lien;
		var p = desk.LienProfile;
		StageHeading(lienStageActions, live.IsFloriaBuyout ? "PURCHASE OFFERS" : lien.IsReckoning ? "HOW DO YOU SETTLE IT" : "WHAT DO YOU OFFER HIM");
		var opts = desk.LienSittingOptions(live);
		if (live.IsFloriaBuyout) {
			foreach (var offer in opts.Where(o => o.verb != PlayerDesk.LienVerb.WalkOut))
				lienStageActions.AddChild(StageOption(desk, live, offer));
			return;
		}
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 14);
		lienStageActions.AddChild(row);

		float cash = Mathf.Max(0f, desk.Label.cashReserves);
		float pay = desk.LienPayBar();
		bool refuses = desk.LienRefusesPay;
		float payRatio = refuses ? 0f : pay <= 0f ? 1f : Mathf.Min(1f, cash / pay);
		string payStatus = refuses ? "He won't take money from you any more."
			: cash >= pay ? $"You have {StageMoney(cash)}. Enough." : $"You have {StageMoney(cash)}. {StageMoney(pay - cash)} short.";

		if (lien.IsReckoning) {
			row.AddChild(StageCard(desk, live, "PAY IT OFF", false, StageMoney(pay), "The balance and every cent of interest, today, in cash.", payRatio, payStatus, cash >= pay,
				opts.Where(o => o.verb == PlayerDesk.LienVerb.PayInFull)));
			row.AddChild(StageCard(desk, live, "HAND OVER THE KEYS", false, "The label", "Walk away from the paper and leave him the label.", null, "There's no coming back from this.", false,
				opts.Where(o => o.verb == PlayerDesk.LienVerb.HandOver)));
			return;
		}

		var show = desk.LienShow();
		long units = desk.LienUnitsSinceSitting();
		int bar = desk.LienShowUnitsBar();
		float showRatio = show.Met ? 1f : lien.BarYear <= 1960 || bar <= 0 ? 0f : Mathf.Min(1f, (float)units / bar);
		string showAsk = lien.FloriaBuyoutWaived ? "The valuation" : lien.BarYear <= 1960 ? "A record out" : $"{bar:N0} copies";
		float stock = desk.LienStockValue();
		float stockRatio = pay <= 0f ? 1f : Mathf.Min(1f, stock / pay);
		bool favour = opts.Any(o => o.verb == PlayerDesk.LienVerb.ConcedeFavour && o.enabled);

		row.AddChild(StageCard(desk, live, "PAY", p.Stresses == PlayerDesk.LienBar.Pay, StageMoney(pay), "Cash from the till, today.", payRatio, payStatus, cash >= pay && !refuses,
			opts.Where(o => o.verb == PlayerDesk.LienVerb.Pay)));
		row.AddChild(StageCard(desk, live, "SHOW", p.Stresses == PlayerDesk.LienBar.Show, showAsk, "Prove the label is worth more than it was.", showRatio, show.Met ? "Enough to show him." : "Not enough to show him yet.", show.Met,
			opts.Where(o => o.verb is PlayerDesk.LienVerb.Show or PlayerDesk.LienVerb.ShowStub)));
		if (!desk.GermainLedgerFrozen)
			row.AddChild(StageCard(desk, live, "CONCEDE", p.Stresses == PlayerDesk.LienBar.Concede, "Something he'll take", "What you can't pay in cash, he'll take another way.", favour ? 1f : stockRatio,
				favour ? "He's counting the favour on your record." : $"Pressed stock on hand: {StageMoney(stock)} of the {StageMoney(pay)}.", favour || stock >= pay && pay > 0f,
				opts.Where(o => o.verb is PlayerDesk.LienVerb.ConcedeFavour or PlayerDesk.LienVerb.ConcedeStock or PlayerDesk.LienVerb.WriteItOn or PlayerDesk.LienVerb.ConcedeCutIn or PlayerDesk.LienVerb.ConcedeNetwork)));
	}

	private Control StageCard(PlayerDesk desk, PlayerDesk.LienSittingLive live, string title, bool hisAsk, string ask, string caption,
		float? ratio, string status, bool met, IEnumerable<PlayerDesk.LienOption> options) {
		var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		card.AddThemeStyleboxOverride("panel", hisAsk
			? StageBox(PaperTheme.PaperLight, PaperTheme.Oxblood, 3, 14, 12)
			: StageBox(PaperTheme.PaperLight, PaperTheme.Edge, 2, 14, 12));
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 8);
		card.AddChild(v);

		var head = new HBoxContainer();
		head.AddChild(StageLabel(title, PaperTheme.Elite, 20, PaperTheme.Rust, false));
		if (hisAsk) {
			var tag = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkEnd, SizeFlagsVertical = SizeFlags.ShrinkCenter };
			tag.AddThemeStyleboxOverride("panel", StageBox(new Color(0, 0, 0, 0), PaperTheme.Oxblood, 2, 8, 2));
			var word = StageLabel("HIS ASK", PaperTheme.Elite, 12, PaperTheme.Oxblood, false);
			word.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			tag.AddChild(word);
			head.AddChild(tag);
		}
		v.AddChild(head);
		v.AddChild(StageLabel(ask, PaperTheme.TypedBold, 24, PaperTheme.Ink, false));
		v.AddChild(StageLabel(caption, PaperTheme.SerifItalic, 14, PaperTheme.Fade));

		if (ratio.HasValue) {
			var meter = new ProgressBar { MinValue = 0, MaxValue = 1, Value = ratio.Value, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 14) };
			meter.AddThemeStyleboxOverride("background", StageBox(new Color("e0cfa6"), PaperTheme.Edge, 1, 0, 0));
			meter.AddThemeStyleboxOverride("fill", StageBox(met ? StageGreen : PaperTheme.Rust, Colors.Transparent, 0, 0, 0));
			v.AddChild(meter);
		}
		v.AddChild(StageLabel(status, PaperTheme.SerifBold, 14, met ? StageGreen : PaperTheme.Oxblood));
		v.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

		foreach (var opt in options) v.AddChild(StageOption(desk, live, opt));
		return card;
	}

	private Control StageOption(PlayerDesk desk, PlayerDesk.LienSittingLive live, PlayerDesk.LienOption opt) {
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 3);
		var button = new Button {
			Text = opt.label, Disabled = !opt.enabled, AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 42),
			MouseDefaultCursorShape = opt.enabled ? CursorShape.PointingHand : CursorShape.Arrow,
		};
		if (opt.verb is PlayerDesk.LienVerb.Pay or PlayerDesk.LienVerb.PayInFull) button.ThemeTypeVariation = PaperTheme.Primary;
		else if (opt.verb == PlayerDesk.LienVerb.HandOver) PaperModal.StylePrimary(button, StageDanger);
		button.Pressed += () => ChooseLien(desk, live, opt);
		box.AddChild(button);

		string sub = opt.enabled ? opt.subLabel : opt.disabledReason;
		if (!string.IsNullOrEmpty(sub)) box.AddChild(StageLabel(sub, PaperTheme.SerifItalic, 14, opt.enabled ? PaperTheme.Fade : Heard));
		if (lienConfirm == opt.verb) box.AddChild(StageConfirm(desk, live, opt));
		return box;
	}

	// ── a choice that follows you asks first ────────────────────────────────────────────────────────

	private static bool LienNeedsConfirm(PlayerDesk.LienVerb verb, PlayerDesk.LienSittingLive live) =>
		verb is PlayerDesk.LienVerb.WriteItOn or PlayerDesk.LienVerb.ConcedeStock or PlayerDesk.LienVerb.HandOver or PlayerDesk.LienVerb.PayInFull
			or PlayerDesk.LienVerb.ConcedeCutIn or PlayerDesk.LienVerb.ConcedeNetwork or PlayerDesk.LienVerb.FloriaSign or PlayerDesk.LienVerb.FloriaRivalBid
		|| (verb == PlayerDesk.LienVerb.WalkOut && live.stage == CallStage.Open);

	private static (string Warning, string Yes) LienConfirmText(PlayerDesk.LienOption opt, PlayerDesk.LienSittingLive live) => opt.verb switch {
		PlayerDesk.LienVerb.WriteItOn => ("This one follows you to 1970. The face sum grows, and he remembers it at the reckoning.", "SIGN IT"),
		PlayerDesk.LienVerb.ConcedeStock => ("The copies leave your shelf for good. You can't sell them afterwards.", "HAND IT OVER"),
		PlayerDesk.LienVerb.ConcedeCutIn => ("His name goes on the credit, and he's paid his quarter of the writing. The writers will notice, and they won't forget it.", "GIVE HIM THE CREDIT"),
		PlayerDesk.LienVerb.ConcedeNetwork => ("It works, and it's on the books as consulting. If a station is exposed, the fine and the burned DJ are yours.", "SEND HIS BOYS"),
		PlayerDesk.LienVerb.HandOver => ("This ends the run. He takes the catalogue and the lease.", "GIVE HIM THE KEYS"),
		PlayerDesk.LienVerb.PayInFull => ($"{StageMoney(opt.cash)} comes out of the till, all at once, and the paper is torn up.", "COUNT IT OUT"),
		PlayerDesk.LienVerb.FloriaSign or PlayerDesk.LienVerb.FloriaRivalBid => ("Signing transfers ownership of the label and ends the run.", "SIGN THE SALE"),
		PlayerDesk.LienVerb.WalkOut when live.IsFloriaBuyout => ("If you leave without a deal, the firm's purchase takes effect on the deadline.", "YES, SHOW HIM OUT"),
		_ => ("Nothing will be settled. If you can't pay on the 1st, he seizes what he can.", "YES, SHOW HIM OUT"),
	};

	private void ChooseLien(PlayerDesk desk, PlayerDesk.LienSittingLive live, PlayerDesk.LienOption opt) {
		if (LienNeedsConfirm(opt.verb, live) && lienConfirm != opt.verb) { lienConfirm = opt.verb; RenderLienStage(desk, live); return; }
		lienConfirm = null;
		if (!desk.ChooseLienOption(live, opt, out string message)) Say(message, false);
		Refresh();
	}

	private Control StageConfirm(PlayerDesk desk, PlayerDesk.LienSittingLive live, PlayerDesk.LienOption opt) {
		var (warning, yes) = LienConfirmText(opt, live);
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", StageBox(new Color(PaperTheme.Oxblood, 0.08f), PaperTheme.Oxblood, 2, 10, 8));
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 8);
		v.AddChild(StageLabel(warning, PaperTheme.SerifBold, 14, PaperTheme.Oxblood));
		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 8);
		var confirm = new Button { Text = yes, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 38) };
		PaperModal.StylePrimary(confirm, StageDanger);
		confirm.Pressed += () => ChooseLien(desk, live, opt);
		buttons.AddChild(confirm);
		var back = new Button { Text = opt.verb == PlayerDesk.LienVerb.WalkOut ? "STAY" : "THINK AGAIN", CustomMinimumSize = new Vector2(0, 38) };
		back.Pressed += () => { lienConfirm = null; RenderLienStage(desk, live); };
		buttons.AddChild(back);
		v.AddChild(buttons);
		panel.AddChild(v);
		return panel;
	}

	// ── answering a pushback ────────────────────────────────────────────────────────────────────────

	private void StageCounters(PlayerDesk desk, PlayerDesk.LienSittingLive live) {
		StageHeading(lienStageActions, "HOW DO YOU ANSWER THAT");
		foreach (var opt in desk.LienCounterOptions(live)) RenderLienOption(desk, live, opt, lienStageActions);
	}

	// ── what was settled ────────────────────────────────────────────────────────────────────────────

	private void StageOutcome(PlayerDesk desk, PlayerDesk.LienSittingLive live) {
		var lien = desk.Lien;
		var p = desk.LienProfile;
		bool over = desk.IsGameOver;
		bool extended = !over && live.stage == CallStage.Resolved;
		GameDate due = lien.PendingDeadline ?? lien.Deadline;

		string head; Color ink; string sub;
		if (over) {
			bool clean = lien.Ending == "PaidClean";
			bool sold = lien.Ending == "SoldToMajor";
			head = sold ? "Sold to a Major." : clean ? "Paid clean." : "Taken.";
			ink = clean || sold ? StageGreen : PaperTheme.Oxblood;
			sub = live.IsFloriaBuyout ? desk.GameOverReason
				: clean ? "The paper is torn up. The label is yours." : $"{p.Name} has the catalogue and the lease. It's over.";
		} else if (extended) {
			head = "Extended."; ink = StageGreen;
			sub = $"The lien now runs to {due.ToHeadlineString()}.";
		} else {
			head = "Nothing settled."; ink = PaperTheme.Rust;
			sub = live.IsFloriaBuyout ? $"The purchase can go through on {lien.Deadline.ToHeadlineString()}." : $"On {lien.Deadline.ToHeadlineString()} he takes {StageMoney(desk.LienPayBar())} if it's there, or seizes what he can. " + (lien.ConsecutiveFailures > 0
				? "You've already missed one. If he can't take it this time, he takes the label."
				: "He'll give you three months after that. Miss those too and it's over.");
		}
		lienStageActions.AddChild(StageLabel(head, PaperTheme.Lettering(LetteringStyle.Didone), 36, ink, false));
		lienStageActions.AddChild(StageLabel(sub, PaperTheme.Serif, 17, PaperTheme.Ink));
		if (!extended) return;

		var rows = new List<LedgerTable.Row>();
		foreach (var e in lien.Ledger.Skip(lienLedgerAtOpen)) {
			switch (e.Kind) {
				case PlayerDesk.LienLedgerKind.Paid: rows.Add(LedgerTable.Entry("Paid on the paper", LedgerTable.Deduct(e.Amount))); break;
				case PlayerDesk.LienLedgerKind.ConcededStock: rows.Add(LedgerTable.Entry($"Pressed stock handed over ({e.Detail})", LedgerTable.Deduct(e.Amount))); break;
				case PlayerDesk.LienLedgerKind.WrittenOn: rows.Add(LedgerTable.Entry("Written onto the paper", LedgerTable.Money(e.Amount))); break;
			}
		}
		rows.Add(LedgerTable.Entry("Face sum now", LedgerTable.Money(lien.FaceSum)));
		rows.Add(LedgerTable.Total("Still outstanding", LedgerTable.Money(lien.Outstanding)));
		rows.Add(LedgerTable.Entry("Runs to", due.ToHeadlineString()));
		lienStageActions.AddChild(new LedgerTable().Set("Entered in the ledger", null, rows));

		var next = desk.LienNextBars();
		float interest = lien.InterestDueBy(due);
		string ask = next.Reckoning
			? $"Next December is the reckoning. On {due.ToHeadlineString()} he wants every cent, about {StageMoney(lien.Outstanding + interest)}, or he takes the label."
			: $"Next December he'll want about {StageMoney(Mathf.Min(next.Slice, lien.Outstanding) + interest)} to roll it, or {next.Units:N0} copies sold since today" +
			  (next.DueYear - 1 >= 1962 ? (desk.LienShowHitBar == 1 ? ", or a Top 40 record" : ", or two Top 40 records") : "") +
			  (desk.GermainLedgerFrozen ? "." : ", or something given up.");
		var note = new PanelContainer();
		note.AddThemeStyleboxOverride("panel", StageBox(new Color(0.55f, 0.42f, 0.2f, 0.10f), PaperTheme.Edge, 1, 14, 10));
		var nv = new VBoxContainer();
		nv.AddThemeConstantOverride("separation", 4);
		nv.AddChild(StageLabel("NEXT DECEMBER", PaperTheme.Elite, 13, PaperTheme.Rust, false));
		nv.AddChild(StageLabel(ask, PaperTheme.Serif, 16, PaperTheme.Ink));
		note.AddChild(nv);
		lienStageActions.AddChild(note);
		// §4.3: the year survived is a point; spend it here, or later on the OFFICE tab.
		InstinctPointButtons(desk, lienStageActions);
	}

	// ── the way out ─────────────────────────────────────────────────────────────────────────────────

	private void StageFooter(PlayerDesk desk, PlayerDesk.LienSittingLive live) {
		if (live.stage is CallStage.Resolved or CallStage.Ended) {
			lienStageFooter.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
			var done = new Button { Text = desk.IsGameOver ? "THAT'S THE END OF IT" : "SEE HIM OUT", CustomMinimumSize = new Vector2(280, 44), ThemeTypeVariation = PaperTheme.Primary };
			done.Pressed += () => { PlayerDesk.Instance.CloseLienSitting(); Refresh(); };
			lienStageFooter.AddChild(done);
			return;
		}
		if (live.stage != CallStage.Open) return;
		var walk = desk.LienSittingOptions(live).FirstOrDefault(o => o.verb == PlayerDesk.LienVerb.WalkOut);
		if (walk == null) return;
		if (lienConfirm == PlayerDesk.LienVerb.WalkOut) {
			lienStageFooter.AddChild(StageLabel(LienConfirmText(walk, live).Warning, PaperTheme.SerifBold, 14, PaperTheme.Oxblood));
			var stay = new Button { Text = "STAY", CustomMinimumSize = new Vector2(120, 40) };
			stay.Pressed += () => { lienConfirm = null; RenderLienStage(desk, live); };
			lienStageFooter.AddChild(stay);
			var leave = new Button { Text = LienConfirmText(walk, live).Yes, CustomMinimumSize = new Vector2(220, 40) };
			PaperModal.StylePrimary(leave, StageDanger);
			leave.Pressed += () => ChooseLien(desk, live, walk);
			lienStageFooter.AddChild(leave);
			return;
		}
		var show = new Button { Text = "SHOW HIM OUT", CustomMinimumSize = new Vector2(200, 40) };
		show.Pressed += () => ChooseLien(desk, live, walk);
		lienStageFooter.AddChild(show);
		lienStageFooter.AddChild(StageLabel(walk.subLabel, PaperTheme.SerifItalic, 14, Heard));
	}
}
