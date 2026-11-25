using Godot;

/// <summary>
/// The first morning (SimTools/OnboardingTutorialDirective.md §3.1, T1): before the lawyer's letter, a short scene at the office
/// door that says who holds the paper and what he wants, in story terms. One sheet in noon light, the scene line by line with the
/// voices in the margin, the plain situation on a slip at the foot, and READ THE LETTER, which opens the Morning After.
/// The words live on <see cref="PlayerDesk"/> (PlayerDesk.FirstMorning.cs). Shown once per session before the Morning After;
/// a reload before GET TO WORK plays it again, which costs nothing.
/// </summary>
public partial class PlayerDeskPanel {
	private CanvasLayer firstMorningStage;
	private bool firstMorningShown;

	private void CloseFirstMorning() {
		if (firstMorningStage != null && IsInstanceValid(firstMorningStage)) {
			firstMorningStage.GetParent()?.RemoveChild(firstMorningStage);
			firstMorningStage.QueueFree();
		}
		firstMorningStage = null;
	}

	/// <summary>The scene, then the Morning After. Holds <c>lienMorningOpen</c> so nothing else opens over it.</summary>
	private void OpenFirstMorning(PlayerDesk desk) {
		CloseFirstMorning();
		var lines = desk.FirstMorningLines();
		firstMorningShown = true;
		if (lines.Count == 0) { OpenLienMorning(desk, replay: false); return; }
		lienMorningOpen = true;
		string wants = desk.HolderWantsPlain();

		firstMorningStage = new CanvasLayer { Layer = 59 };
		var root = new Control { MouseFilter = MouseFilterEnum.Stop, Theme = PaperTheme.Build() };
		root.SetAnchorsPreset(LayoutPreset.FullRect);
		firstMorningStage.AddChild(root);
		var sun = new ColorRect { Color = new Color("dcc890"), MouseFilter = MouseFilterEnum.Ignore };
		sun.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(sun);
		root.AddChild(new LightBlinds());
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(center);

		var sheet = PaperStyleBox.Sheet(PaperTheme.Paper, 36, 26, 24, PaperTheme.Rust).Decorated(false, false, 7);
		sheet.BorderWidth = 3; sheet.Radius = 3; sheet.ShadowAlpha = 0.55f;
		var page = new PanelContainer { CustomMinimumSize = new Vector2(760, 0) };
		page.AddThemeStyleboxOverride("panel", sheet);
		center.AddChild(page);

		var outer = new VBoxContainer();
		outer.AddThemeConstantOverride("separation", 12);
		page.AddChild(outer);
		outer.AddChild(StageLabel(desk.FirstMorningHeading(), PaperTheme.Elite, 14, PaperTheme.Fade, false));
		outer.AddChild(StageLabel("A Knock at the Door", PaperTheme.Lettering(LetteringStyle.Didone), 38, PaperTheme.Rust, false));
		outer.AddChild(new ColorRect { Color = new Color("b99759"), CustomMinimumSize = new Vector2(0, 2) });

		// The scene scrolls; where you stand, and the line after it, stay pinned under it so neither is ever below the fold.
		float sceneHeight = Mathf.Clamp(GetViewportRect().Size.Y - 470f, 220f, 520f);
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(740, sceneHeight), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		outer.AddChild(scroll);
		var v = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		v.AddThemeConstantOverride("separation", 12);
		scroll.AddChild(v);
		var pinned = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		pinned.AddThemeConstantOverride("separation", 10);
		outer.AddChild(pinned);

		var shown = new System.Collections.Generic.List<Control>();
		bool tail = false;
		foreach (var line in lines) {
			Control c;
			tail |= line.voice == ExecutiveVoice.None && string.IsNullOrEmpty(line.speaker) && line.text == wants;
			if (tail && line.text == wants) {
				// The situation, plainly: a slip pinned under the scene, the one thing to take away from it.
				var slip = new PanelContainer();
				slip.AddThemeStyleboxOverride("panel", StageBox(PaperTheme.Field, PaperTheme.Oxblood, 2, 16, 10));
				var sv = new VBoxContainer();
				sv.AddThemeConstantOverride("separation", 4);
				sv.AddChild(StageLabel("WHERE YOU STAND", PaperTheme.Elite, 13, PaperTheme.Oxblood, false));
				sv.AddChild(StageLabel(line.text, PaperTheme.TypedBold, 16, PaperTheme.Ink));
				slip.AddChild(sv);
				c = slip;
			} else c = StageLine(line);
			(tail ? pinned : v).AddChild(c);
			shown.Add(c);
		}

		outer.AddChild(new ColorRect { Color = new Color("b99759"), CustomMinimumSize = new Vector2(0, 1) });
		var footer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		var go = new Button {
			Text = "READ THE LETTER", CustomMinimumSize = new Vector2(260, 44), ThemeTypeVariation = PaperTheme.Primary,
			ShortcutInTooltip = false, Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } },
		};
		go.Pressed += () => {
			CloseFirstMorning();
			lienMorningOpen = false;
			if (PlayerDesk.Instance is { } d && d.LienMorningPending) OpenLienMorning(d, replay: false);
		};
		footer.AddChild(go);
		outer.AddChild(footer);

		AddChild(firstMorningStage);
		go.GrabFocus();

		if (StageAnimates) {
			OfficeFoley.Play(this, "paper");
			for (int i = 0; i < shown.Count; i++) {
				var c = shown[i];
				c.Modulate = new Color(1, 1, 1, 0);
				c.CreateTween().TweenProperty(c, "modulate:a", 1.0f, 0.5).SetDelay(0.3f + i * 0.9f);
			}
		}
	}
}
