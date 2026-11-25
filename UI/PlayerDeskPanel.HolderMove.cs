using Godot;

/// <summary>
/// The holder's first move (SimTools/LabelManInnerLifeDirective.md §6.1): while the first single is at the plant, the man who
/// holds the paper shows himself. A sheet on the dimmed desk with the scene line by line (the holder in his own voice, then the
/// voices in the margin), and the two answers. The rules live on <see cref="PlayerDesk"/> (PlayerDesk.HolderMove.cs).
/// </summary>
public partial class PlayerDeskPanel {
	private CanvasLayer holderMoveStage;
	private bool holderMoveOpen;

	private void ShowHolderMoveIfPending(PlayerDesk desk) {
		if (holderMoveOpen || lienMorningOpen || lienNoticeOpen || !desk.HolderMovePending || !IsVisibleInTree()) return;
		OpenHolderMove(desk);
	}

	private void CloseHolderMove() {
		if (holderMoveStage != null && IsInstanceValid(holderMoveStage)) {
			holderMoveStage.GetParent()?.RemoveChild(holderMoveStage);
			holderMoveStage.QueueFree();
		}
		holderMoveStage = null;
		holderMoveOpen = false;
	}

	private void OpenHolderMove(PlayerDesk desk) {
		CloseHolderMove();
		holderMoveOpen = true;

		holderMoveStage = new CanvasLayer { Layer = 58 };
		var root = new Control { MouseFilter = MouseFilterEnum.Stop, Theme = PaperTheme.Build() };
		root.SetAnchorsPreset(LayoutPreset.FullRect);
		holderMoveStage.AddChild(root);
		var dim = new ColorRect { Color = new Color(0.06f, 0.04f, 0.025f, 0.82f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(dim);
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(center);

		var sheet = PaperStyleBox.Sheet(PaperTheme.Field, 36, 28, 24, PaperTheme.Edge).Decorated(false, false, 7);
		sheet.BorderWidth = 2; sheet.Radius = 2; sheet.ShadowAlpha = 0.6f;
		var page = new PanelContainer { CustomMinimumSize = new Vector2(720, 0) };
		page.AddThemeStyleboxOverride("panel", sheet);
		center.AddChild(page);

		// The scene scrolls; the two answers stay pinned under it, so neither is ever below the fold.
		var outer = new VBoxContainer();
		outer.AddThemeConstantOverride("separation", 12);
		page.AddChild(outer);
		float sceneHeight = Mathf.Clamp(GetViewportRect().Size.Y - 420f, 260f, 520f);
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(700, sceneHeight), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		outer.AddChild(scroll);
		var v = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		v.AddThemeConstantOverride("separation", 12);
		scroll.AddChild(v);

		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		var head = new HBoxContainer();
		head.AddChild(StageLabel("THE HOLDER", PaperTheme.Elite, 15, PaperTheme.Oxblood, false));
		head.AddChild(StageLabel(today.ToHeadlineString(), PaperTheme.Typed, 14, PaperTheme.Fade, false, HorizontalAlignment.Right));
		v.AddChild(head);
		v.AddChild(new ColorRect { Color = new Color(PaperTheme.Edge, 0.5f), CustomMinimumSize = new Vector2(0, 1) });

		foreach (var line in desk.HolderMoveLines()) v.AddChild(StageLine(line));

		outer.AddChild(new ColorRect { Color = new Color(PaperTheme.Edge, 0.5f), CustomMinimumSize = new Vector2(0, 1) });
		var options = new VBoxContainer();
		options.AddThemeConstantOverride("separation", 8);
		outer.AddChild(options);
		foreach (var opt in desk.HolderMoveOptions()) {
			var o = opt;
			RenderOption(new CallOption {
				label = o.label, subLabel = o.subLabel, voice = o.voice, enabled = o.enabled, disabledReason = o.disabledReason, cash = o.cash,
			}, () => {
				bool ok = desk.ResolveHolderMove(o.accept, out string m);
				CloseHolderMove();
				if (!string.IsNullOrEmpty(m)) Say(m, ok);
				Refresh();
			}, options);
		}

		AddChild(holderMoveStage);

		if (StageAnimates) {
			OfficeFoley.Play(this, "paper");
			page.Modulate = new Color(1, 1, 1, 0);
			var settle = page.CreateTween();
			settle.TweenProperty(page, "modulate:a", 1.0f, 0.4);
		}
	}
}
