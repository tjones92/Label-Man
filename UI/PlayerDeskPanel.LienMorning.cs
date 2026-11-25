using System;
using System.Linq;
using Godot;

/// <summary>
/// The morning after (SimTools/LabelManInnerLifeDirective.md §3.1) as a scene of its own, lit like noon after a bad night:
/// the lawyer's letter naming the holder, the founding money in the cigar box, what the night took struck off
/// one item at a time, the terms of the lien in plain words, and the four voices arguing about what happened, each
/// from what the player said they remembered. The hangover shader that dressed the founding pages starts heavy and
/// clears as the voices land, so the intro, the questions and this scene are one headache.
/// It plays once on the first desk after founding; "Read the letter again" on the OFFICE tab brings it back without the
/// haze or the pauses.
/// </summary>
public partial class PlayerDeskPanel {
	private CanvasLayer lienMorningStage, lienMorningHazeLayer;
	private ShaderMaterial lienMorningHaze;
	private ColorRect lienMorningHazeRect;
	private Tween lienMorningTween;
	private const float MorningSeconds = 11f;

	/// <summary>The voices argue about the night, once, on the first desk after founding (§3.1).</summary>
	private void ShowLienMorningIfPending(PlayerDesk desk) {
		if (lienMorningOpen || !desk.LienMorningPending || !IsVisibleInTree()) return;
		if (desk.LienMorningLines().Count == 0) { desk.DismissLienMorning(); return; }
		// The knock at the door comes first (onboarding §3.1): who he is and what he wants, then the letter and its rules.
		if (!firstMorningShown) { OpenFirstMorning(desk); return; }
		OpenLienMorning(desk, replay: false);
	}

	private void CloseLienMorning(bool dismiss) {
		lienMorningTween?.Kill();
		lienMorningTween = null;
		foreach (var layer in new[] { lienMorningStage, lienMorningHazeLayer }) {
			if (layer == null || !IsInstanceValid(layer)) continue;
			layer.GetParent()?.RemoveChild(layer);
			layer.QueueFree();
		}
		lienMorningStage = lienMorningHazeLayer = null;
		lienMorningHaze = null; lienMorningHazeRect = null;
		bool wasOpen = lienMorningOpen;
		lienMorningOpen = false;
		if (dismiss && wasOpen) PlayerDesk.Instance?.DismissLienMorning();
	}

	private void OpenLienMorning(PlayerDesk desk, bool replay) {
		CloseFirstMorning();
		CloseLienMorning(false);
		lienMorningOpen = true;
		var lien = desk.Lien;
		var p = desk.LienProfile;
		bool animate = StageAnimates && !replay;
		var due = lien.PendingDeadline ?? lien.Deadline;
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int days = Math.Max(0, (new DateTime(due.year, due.month, due.day) - new DateTime(today.year, today.month, today.day)).Days);
		var lines = desk.LienMorningLines();
		var voices = lines.Where(l => l.voice != ExecutiveVoice.None).ToList();
		var narration = lines.Where(l => l.voice == ExecutiveVoice.None).ToList();

		lienMorningStage = new CanvasLayer { Layer = 59 };
		var root = new Control { MouseFilter = MouseFilterEnum.Stop, Theme = PaperTheme.Build() };
		root.SetAnchorsPreset(LayoutPreset.FullRect);
		lienMorningStage.AddChild(root);

		// Noon through the blinds.
		var sun = new ColorRect { Color = new Color("dcc890"), MouseFilter = MouseFilterEnum.Ignore };
		sun.SetAnchorsPreset(LayoutPreset.FullRect);
		root.AddChild(sun);
		root.AddChild(new LightBlinds());

		var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_top", 26);
		margin.AddThemeConstantOverride("margin_bottom", 26);
		void Sides() {
			int side = Mathf.Max(30, (int)((root.Size.X - 1240f) / 2f));
			margin.AddThemeConstantOverride("margin_left", side);
			margin.AddThemeConstantOverride("margin_right", side);
		}
		Sides();
		root.Resized += Sides;
		root.AddChild(margin);

		var card = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		var sheet = PaperStyleBox.Sheet(PaperTheme.Paper, 34, 18, 26, PaperTheme.Rust).Decorated(false, false, 7);
		sheet.BorderWidth = 3; sheet.Radius = 3; sheet.ShadowAlpha = 0.55f;
		card.AddThemeStyleboxOverride("panel", sheet);
		margin.AddChild(card);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 8);
		card.AddChild(column);

		// ── the plate and the first of ten Decembers ──
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 20);
		var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		left.AddThemeConstantOverride("separation", 0);
		left.AddChild(StageLabel("NEW YEAR'S DAY, 1960  ·  ABOUT NOON", PaperTheme.Elite, 14, PaperTheme.Fade, false));
		left.AddChild(StageLabel("The Morning After", PaperTheme.Lettering(LetteringStyle.Didone), 42, PaperTheme.Rust, false));
		row.AddChild(left);
		var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkEnd, SizeFlagsVertical = SizeFlags.ShrinkEnd };
		right.AddThemeConstantOverride("separation", 2);
		right.AddChild(StageLabel($"{days} DAYS TO RUN", PaperTheme.Elite, 18, PaperTheme.Oxblood, false, HorizontalAlignment.Right));
		right.AddChild(StageLabel($"Falls due {due.ToHeadlineString()}", PaperTheme.Typed, 14, PaperTheme.Fade, false, HorizontalAlignment.Right));
		row.AddChild(right);
		column.AddChild(row);
		if (narration.Count > 0) column.AddChild(StageLabel(narration[0].text, PaperTheme.SerifItalic, 17, new Color("4a3f2a")));
		column.AddChild(new LienDecadeStrip().Set(new System.Collections.Generic.List<PlayerDesk.LienYearOutcome>(), 0, false));
		column.AddChild(new ColorRect { Color = new Color("b99759"), CustomMinimumSize = new Vector2(0, 2) });

		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		column.AddChild(scroll);
		var body = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 22);
		scroll.AddChild(body);

		// ── column one: the letter ──
		var letterColumn = new VBoxContainer { CustomMinimumSize = new Vector2(340, 0), SizeFlagsVertical = SizeFlags.ShrinkBegin };
		body.AddChild(letterColumn);
		var letter = new PanelContainer();
		letter.AddThemeStyleboxOverride("panel", StageBox(PaperTheme.Field, PaperTheme.Edge, 2, 18, 16));
		letterColumn.AddChild(letter);
		var lv = new VBoxContainer();
		lv.AddThemeConstantOverride("separation", 8);
		letter.AddChild(lv);
		lv.AddChild(StageLabel("BY HAND", PaperTheme.Elite, 13, PaperTheme.Fade, false));
		lv.AddChild(StageLabel(
			$"Sir,\n\nLast night, some time after midnight, you executed an instrument creating a lien on all property you hold now and all that {desk.Label.labelName} will hold hereafter. It secures {StageMoney(lien.FaceSum)}.\n\n" +
			$"The sum falls due on {due.ToHeadlineString()}. An extension may be sought at a sitting in the last two weeks of December.",
			PaperTheme.Typed, 14, PaperTheme.Ink));
		// The holder is named as soon as the run knows him (Sawyer from the first morning: Connie's paper has his name on
		// it). Author, 2026-11-25: each holder bends the run toward one thing, so the player should know whose paper it is
		// at once; the mystery is why he wanted it. All five are named at founding; the first morning (T1) introduces him.
		bool named = desk.LienHolderIdentified;
		// Wraps when named: the holder's title is a clause ("who owns the River Lantern, …") and would widen the letter.
		lv.AddChild(StageLabel(named ? $"Held by:  {p.Name}, {p.Title}" : "Held by:  ______________________", PaperTheme.Typed, 14, PaperTheme.Ink, named));
		var stamp = new LienStamp().Set("Falls due", due.ToHeadlineString(), PaperTheme.Oxblood, -0.05f);
		lv.AddChild(stamp);
		lv.AddChild(StageLabel(p.Lawyer, PaperTheme.Lettering(LetteringStyle.Script), 30, PaperTheme.Ink, false));
		lv.AddChild(StageLabel(named ? $"for {p.Name}" : "for the holder", PaperTheme.SerifItalic, 14, PaperTheme.Fade, false));
		// What came with the paper. The narration always said it, but the page only ever showed its first and last lines,
		// so a Sawyer player met Connie by stumbling on the ROSTER tab (playtest 2026-11-25).
		if (desk.LienUnderTheLetter() is { } under) {
			var slip = new PanelContainer();
			slip.AddThemeStyleboxOverride("panel", StageBox(PaperTheme.Field, PaperTheme.Edge, 1, 14, 10));
			var sv = new VBoxContainer();
			sv.AddThemeConstantOverride("separation", 4);
			slip.AddChild(sv);
			sv.AddChild(StageLabel("UNDER THE LETTER", PaperTheme.Elite, 13, PaperTheme.Fade, false));
			sv.AddChild(StageLabel(under, PaperTheme.SerifItalic, 15, PaperTheme.Ink));
			letterColumn.AddChild(slip);
		}

		// ── column two: what's left, what's gone, the terms ──
		var middle = new VBoxContainer { CustomMinimumSize = new Vector2(320, 0), SizeFlagsVertical = SizeFlags.ShrinkBegin };
		middle.AddThemeConstantOverride("separation", 14);
		body.AddChild(middle);

		var box = new PanelContainer();
		box.AddThemeStyleboxOverride("panel", StageBox(new Color("5b3d22"), new Color("2e1d0e"), 3, 16, 12));
		var bv = new VBoxContainer();
		bv.AddThemeConstantOverride("separation", 0);
		bv.AddChild(StageLabel("THE CIGAR BOX", PaperTheme.Elite, 13, new Color("d9b873"), false));
		bv.AddChild(StageLabel(StageMoney(desk.Label.cashReserves), PaperTheme.TypedBold, 36, new Color("f3e6c4"), false));
		bv.AddChild(StageLabel("The founding money. Everything that's left.", PaperTheme.SerifItalic, 14, new Color("d9c9a0")));
		box.AddChild(bv);
		middle.AddChild(box);

		var gone = new VBoxContainer();
		gone.AddThemeConstantOverride("separation", 4);
		StageHeading(gone, "GONE, AND YOU DON'T KNOW WHERE");
		var items = desk.LienGoneList();
		var struck = new System.Collections.Generic.List<(RichTextLabel Label, string Text)>();
		foreach (string raw in items) {
			string text = char.ToUpperInvariant(raw[0]) + raw[1..];
			var rich = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
			rich.AddThemeFontOverride("normal_font", PaperTheme.Serif);
			rich.AddThemeFontSizeOverride("normal_font_size", 17);
			rich.AddThemeColorOverride("default_color", PaperTheme.Ink);
			rich.Text = animate ? text : $"[s]{text}[/s]";
			gone.AddChild(rich);
			struck.Add((rich, text));
		}
		middle.AddChild(gone);

		var terms = new VBoxContainer();
		terms.AddThemeConstantOverride("separation", 6);
		StageHeading(terms, "WHAT THE LETTER SAYS");
		terms.AddChild(StageLabel("Every December his lawyer sits down with you. Meet one bar and the lien rolls another year:", PaperTheme.Serif, 15, PaperTheme.Ink));
		terms.AddChild(StageLabel("PAY  the interest and a slice of principal, in cash.\nSHOW  the label is worth more than last year.\nCONCEDE  give something up.", PaperTheme.Typed, 13, PaperTheme.Rust));
		terms.AddChild(StageLabel("Miss one and he seizes what he can and gives you three months. Miss twice running and he takes the label. " +
			"The paper forbids paying it off before 1 January 1970.", PaperTheme.SerifItalic, 14, PaperTheme.Fade));
		middle.AddChild(terms);

		// ── column three: the voices ──
		var voiceColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkBegin };
		voiceColumn.AddThemeConstantOverride("separation", 12);
		body.AddChild(voiceColumn);
		var voiceHeading = new VBoxContainer();
		voiceHeading.AddThemeConstantOverride("separation", 4);
		StageHeading(voiceHeading, "WHAT THE VOICES MAKE OF IT");
		voiceColumn.AddChild(voiceHeading);
		var voiceNotes = voices.Select(v => StageLine(v)).ToList();
		foreach (var note in voiceNotes) voiceColumn.AddChild(note);

		// ── the footer ──
		column.AddChild(new ColorRect { Color = new Color("b99759"), CustomMinimumSize = new Vector2(0, 1) });
		var footer = new HBoxContainer();
		footer.AddThemeConstantOverride("separation", 14);
		Control closing = narration.Count > 1
			? StageLabel(narration[^1].text, PaperTheme.SerifItalic, 17, new Color("4a3f2a"))
			: new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		footer.AddChild(closing);
		var go = new Button {
			Text = replay ? "PUT IT AWAY" : "GET TO WORK", CustomMinimumSize = new Vector2(260, 44), ThemeTypeVariation = PaperTheme.Primary,
			ShortcutInTooltip = false, Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } },
		};
		go.Pressed += () => CloseLienMorning(dismiss: true);
		footer.AddChild(go);
		column.AddChild(footer);

		AddChild(lienMorningStage);
		go.GrabFocus();

		// ── the headache, and the order things come into focus ──
		if (animate) {
			void Reveal(Control c, float delay) {
				c.Modulate = new Color(1, 1, 1, 0);
				c.CreateTween().TweenProperty(c, "modulate:a", 1.0f, 0.6).SetDelay(delay);
			}
			Reveal(letter, 0.4f);
			Reveal(box, 1.0f);
			Reveal(gone, 1.8f);
			for (int i = 0; i < struck.Count; i++) {
				var (label, text) = struck[i];
				var tween = label.CreateTween();
				tween.TweenInterval(2.3f + i * 0.55f);
				tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(label)) label.Text = $"[s]{text}[/s]"; }));
			}
			Reveal(terms, 3.9f);
			Reveal(voiceHeading, 4.6f);
			for (int i = 0; i < voiceNotes.Count; i++) Reveal(voiceNotes[i], 5.2f + i * 1.1f);
			Reveal(closing, 5.2f + voiceNotes.Count * 1.1f);
			// The stamp comes down on the letter.
			stamp.Modulate = new Color(1, 1, 1, 0);
			stamp.Scale = new Vector2(1.7f, 1.7f);
			var thunk = stamp.CreateTween().SetParallel(true);
			thunk.TweenProperty(stamp, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In).SetDelay(1.3);
			thunk.TweenProperty(stamp, "modulate:a", 1.0f, 0.12).SetDelay(1.3);

			lienMorningHaze = new ShaderMaterial { Shader = new Shader { Code = OfficeIntro.HazeShader } };
			lienMorningHazeRect = new ColorRect { Name = "Headache", Material = lienMorningHaze, MouseFilter = MouseFilterEnum.Ignore };
			lienMorningHazeRect.SetAnchorsPreset(LayoutPreset.FullRect);
			lienMorningHazeLayer = new CanvasLayer { Layer = 60 };
			lienMorningHazeLayer.AddChild(lienMorningHazeRect);
			AddChild(lienMorningHazeLayer);
			SetMorningHaze(0f);
			lienMorningTween = lienMorningHazeLayer.CreateTween();
			lienMorningTween.TweenMethod(Callable.From<float>(SetMorningHaze), 0f, 1f, MorningSeconds);
		}
	}

	/// <summary>0 = the headache at its worst, 1 = clear. Eased so it lingers while the letter is read and lifts as the voices speak.</summary>
	private void SetMorningHaze(float u) {
		if (lienMorningHaze == null || !IsInstanceValid(lienMorningHazeRect)) return;
		float strength = Mathf.Pow(1f - u, 1.5f);
		lienMorningHazeRect.Visible = strength > 0.01f;
		lienMorningHaze.SetShaderParameter("haze", strength);
		lienMorningHaze.SetShaderParameter("vignette", strength * 0.8f);
		lienMorningHaze.SetShaderParameter("desat", strength * 0.22f);
		lienMorningHaze.SetShaderParameter("t", u * MorningSeconds);
	}
}
