using Godot;

/// <summary>
/// The lawyer's letter: when the December window opens (two weeks before the deadline) it is on the mat the next time
/// you're at the desk. A typed sheet laid on the dimmed desk, the lawyer's signature, a RECEIVED stamp, and the date he'll
/// call from. The wording changes for the first deadline, a second notice after a missed one, and the last letter of the
/// reckoning (PlayerDesk.LienNotice). It plays once per deadline.
/// </summary>
public partial class PlayerDeskPanel {
	private CanvasLayer lienNoticeStage;
	private bool lienNoticeOpen;

	private void ShowLienNoticeIfPending(PlayerDesk desk) {
		if (lienNoticeOpen || lienMorningOpen || !desk.LienNoticePending || !IsVisibleInTree()) return;
		OpenLienNotice(desk);
	}

	private void CloseLienNotice(bool dismiss) {
		if (lienNoticeStage != null && IsInstanceValid(lienNoticeStage)) {
			lienNoticeStage.GetParent()?.RemoveChild(lienNoticeStage);
			lienNoticeStage.QueueFree();
		}
		lienNoticeStage = null;
		bool wasOpen = lienNoticeOpen;
		lienNoticeOpen = false;
		if (dismiss && wasOpen) PlayerDesk.Instance?.DismissLienNotice();
	}

	private void OpenLienNotice(PlayerDesk desk) {
		CloseLienNotice(false);
		lienNoticeOpen = true;
		var p = desk.LienProfile;
		var (text, tag) = desk.LienNotice();
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		bool reckoning = desk.Lien.IsReckoning;

		lienNoticeStage = new CanvasLayer { Layer = 58 };
		var root = new Control { MouseFilter = MouseFilterEnum.Stop, Theme = PaperTheme.Build() };
		root.SetAnchorsPreset(LayoutPreset.FullRect);
		lienNoticeStage.AddChild(root);
		var dim = new ColorRect { Color = new Color(0.06f, 0.04f, 0.025f, 0.82f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(dim);
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(center);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 18);
		center.AddChild(column);

		var sheet = PaperStyleBox.Sheet(PaperTheme.Field, 40, 30, 24, PaperTheme.Edge).Decorated(false, false, 11);
		sheet.BorderWidth = 2; sheet.Radius = 2; sheet.ShadowAlpha = 0.6f;
		var letter = new PanelContainer { CustomMinimumSize = new Vector2(580, 0), Rotation = -0.012f };
		letter.AddThemeStyleboxOverride("panel", sheet);
		letter.Resized += () => letter.PivotOffset = letter.Size / 2f;
		column.AddChild(letter);

		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 10);
		letter.AddChild(v);
		var head = new HBoxContainer();
		head.AddChild(StageLabel(tag, PaperTheme.Elite, reckoning ? 20 : 15, reckoning ? PaperTheme.Oxblood : PaperTheme.Fade, false));
		head.AddChild(StageLabel(today.ToHeadlineString(), PaperTheme.Typed, 14, PaperTheme.Fade, false, HorizontalAlignment.Right));
		v.AddChild(head);
		v.AddChild(new ColorRect { Color = new Color(PaperTheme.Edge, 0.5f), CustomMinimumSize = new Vector2(0, 1) });
		v.AddChild(StageLabel(text, PaperTheme.Typed, 15, PaperTheme.Ink));
		v.AddChild(StageLabel("Yours faithfully,", PaperTheme.SerifItalic, 16, PaperTheme.Fade, false));
		var sign = new HBoxContainer();
		var who = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		who.AddThemeConstantOverride("separation", 0);
		who.AddChild(StageLabel(p.Lawyer, PaperTheme.Lettering(LetteringStyle.Script), 36, PaperTheme.Ink, false));
		who.AddChild(StageLabel("for the holder", PaperTheme.SerifItalic, 14, PaperTheme.Fade, false));
		sign.AddChild(who);
		var stamp = new LienStamp().Set("Received", today.ToHeadlineString(), PaperTheme.Oxblood, 0.07f);
		stamp.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		sign.AddChild(stamp);
		v.AddChild(sign);

		var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		var put = new Button {
			Text = "PUT IT ON THE DESK", CustomMinimumSize = new Vector2(280, 44), ThemeTypeVariation = PaperTheme.Primary,
			ShortcutInTooltip = false, Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } },
		};
		put.Pressed += () => CloseLienNotice(dismiss: true);
		buttons.AddChild(put);
		column.AddChild(buttons);

		AddChild(lienNoticeStage);
		put.GrabFocus();

		if (StageAnimates) {
			OfficeFoley.Play(this, "paper");
			letter.Modulate = new Color(1, 1, 1, 0);
			letter.Scale = new Vector2(0.94f, 0.94f);
			var settle = letter.CreateTween().SetParallel(true);
			settle.TweenProperty(letter, "modulate:a", 1.0f, 0.45);
			settle.TweenProperty(letter, "scale", Vector2.One, 0.45).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			stamp.Modulate = new Color(1, 1, 1, 0);
			stamp.Scale = new Vector2(1.7f, 1.7f);
			var thunk = stamp.CreateTween().SetParallel(true);
			thunk.TweenProperty(stamp, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In).SetDelay(0.9);
			thunk.TweenProperty(stamp, "modulate:a", 1.0f, 0.12).SetDelay(0.9);
		}
	}
}
