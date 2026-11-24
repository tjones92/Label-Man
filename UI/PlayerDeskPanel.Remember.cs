using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// "What do you remember?" (inner-life §3.1a): the four hazy questions about New Year's Eve, one to a page. The Label Man
// is trying to put the night together with a killer headache, so each page comes into focus slowly (the question swims
// as a doubled ghost that settles, the answers surface one at a time), the room behind the folder is the hangover haze
// from the office intro, and the whole sheet throbs on the pulse. The haze eases off with every answer and is gone when
// the practical pages begin: the aspirin kicks in.
//
// Nothing here touches the sim or its RNG; the answers are only held until the label is founded (FoundLabel).
public partial class PlayerDeskPanel {
	private enum NightEntry { Settled, Full, Fade }

	private sealed record NightPrompt(string Question, string Hint);
	private sealed record NightOption(string Text, bool Blank, bool Selected, Action Pick);

	private static readonly NightPrompt[] NightPrompts = {
		new("Where was it?", "Somewhere warm, loud and a long way from sober. Think."),
		new("Who were you drinking with?", "There was a hand on your shoulder. Whose?"),
		new("What were you bragging about?", "You were loud. What were you loud about?"),
		new("How did it end?", "The last thing before the pen."),
	};
	// How hard the head is pounding on each question; it only gets better.
	private static readonly float[] NightHazeByQuestion = { .95f, .80f, .62f, .46f };
	private static readonly string[] NightSettledLines = {
		"Yes. Yes, that's about right.", "That sounds like you.", "Probably.", "It'll have to do.", "If you say so.",
	};
	private const string NightBlankLine = "Nothing there. Just the noise of a room.";
	private const string NightLastLine = "That's all of it. The paper will have to tell you the rest.";
	private static readonly Color NightOxblood = new("8a2f1e");

	private int nightIndex;
	private NightEntry nightEntry = NightEntry.Full;
	private bool nightBusy;
	private float nightFocus = 1f, nightClock, nightHazeNow, nightBlink;
	private bool nightPulseOn;
	private ColorRect nightHaze;
	private ShaderMaterial nightHazeMaterial;
	private Tween nightEntrance, nightAdvance, nightTurn;
	private Button nightNextButton;
	private VBoxContainer nightPage;
	private readonly List<(Control Ghost, Vector2 Lean)> nightGhosts = new();

	/// <summary>A new game: question one, nothing answered, the entrance played in full.</summary>
	private void NightBegin() {
		NightStop();
		nightIndex = 0;
		nightEntry = NightEntry.Full;
		nightWhere = null; nightWho = null; nightBrag = null; nightEnd = null;
	}

	/// <summary>Drops any page animation in flight (a rebuild or a change of step would leave it pointing at freed nodes).</summary>
	private void NightStop() {
		nightEntrance?.Kill(); nightAdvance?.Kill(); nightTurn?.Kill();
		nightEntrance = nightAdvance = nightTurn = null;
		nightBusy = false;
		nightFocus = 1f;
		nightGhosts.Clear();
	}

	private void NightHazeWanted() {
		if (nightHaze != null) return;
		nightHazeMaterial = new ShaderMaterial { Shader = new Shader { Code = OfficeIntro.HazeShader } };
		nightHaze = new ColorRect { Name = "Headache", Material = nightHazeMaterial, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
		AddChild(nightHaze);
		MoveChild(nightHaze, 0);   // under the shade and the folder: it clouds the room, not the page
		nightHaze.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public override void _Process(double delta) {
		bool wanted = foundingStarted && Visible && foundingStep == 0 && PlayerDesk.Instance?.HasLabel == false;
		float goal = wanted ? NightHazeByQuestion[Mathf.Clamp(nightIndex, 0, NightHazeByQuestion.Length - 1)] : 0f;
		if (!wanted && nightHazeNow < .004f) {
			if (nightHaze?.Visible == true) nightHaze.Visible = false;
			if (nightPulseOn) { folder.Modulate = Colors.White; nightPulseOn = false; }
			return;
		}
		float dt = (float)delta;
		nightClock += dt;
		// A slow ease down the stairs: question to question, and then out into daylight.
		nightHazeNow = Mathf.Lerp(nightHazeNow, goal, 1f - Mathf.Exp(-dt * (wanted ? 1.6f : 1.1f)));
		nightBlink = Mathf.MoveToward(nightBlink, 0f, dt * 2.2f);
		float strength = nightHazeNow;

		if (nightHaze != null) {
			nightHaze.Visible = strength > .004f;
			nightHazeMaterial.SetShaderParameter("haze", strength * (1f + nightBlink * .9f));
			nightHazeMaterial.SetShaderParameter("vignette", strength * .8f + nightBlink * .3f);
			nightHazeMaterial.SetShaderParameter("desat", strength * .22f);
			nightHazeMaterial.SetShaderParameter("t", nightClock);
		}

		float beat = Mathf.Pow(Mathf.Max(Mathf.Sin(nightClock * 2.3f), 0f), 14f) + .6f * Mathf.Pow(Mathf.Max(Mathf.Sin(nightClock * 2.3f - .45f), 0f), 14f);
		// The sheet dims a hair on each beat, as if the lamp flinched.
		float dim = 1f - .07f * strength * Mathf.Min(beat, 1f) - .05f * nightBlink;
		folder.Modulate = new Color(dim, dim, dim, 1f);
		nightPulseOn = true;

		// The doubled vision: the ghost of the question drifts off its twin and slides back on the pulse.
		float sway = .55f + .45f * Mathf.Sin(nightClock * .55f);
		float spread = (.22f + .78f * (1f - nightFocus)) * (.3f + .7f * sway) * Mathf.Min(1f, strength * 1.5f + (1f - nightFocus));
		float ghostAlpha = (.27f + .22f * Mathf.Min(beat, 1f) + .35f * (1f - nightFocus)) * Mathf.Min(1f, strength * 1.8f + (1f - nightFocus));
		for (int i = nightGhosts.Count - 1; i >= 0; i--) {
			(Control ghost, Vector2 lean) = nightGhosts[i];
			if (!IsInstanceValid(ghost)) { nightGhosts.RemoveAt(i); continue; }
			ghost.Position = lean * spread;
			ghost.Modulate = new Color(1, 1, 1, ghostAlpha);
		}
	}

	// ---- the page -----------------------------------------------------------------------------------------------------

	private NightOption[] NightOptions(int question) => question switch {
		0 => NightOpts(PlayerDesk.PartyWhereAnswers, PlayerDesk.PartyWhere.Nothing, nightWhere, v => nightWhere = v),
		1 => NightOpts(PlayerDesk.PartyWhoAnswers, PlayerDesk.PartyWho.Nothing, nightWho, v => nightWho = v),
		2 => NightOpts(PlayerDesk.PartyBragAnswers, PlayerDesk.PartyBrag.Nothing, nightBrag, v => nightBrag = v),
		_ => NightOpts(PlayerDesk.PartyEndAnswers, PlayerDesk.PartyEnd.Nothing, nightEnd, v => nightEnd = v),
	};

	private static NightOption[] NightOpts<T>((T Value, string Text)[] answers, T blank, T? current, Action<T> set) where T : struct, Enum =>
		answers.Select(a => new NightOption(a.Text, EqualityComparer<T>.Default.Equals(a.Value, blank),
			current.HasValue && EqualityComparer<T>.Default.Equals(current.Value, a.Value), () => set(a.Value))).ToArray();

	private bool NightAnswered(int question) => NightOptions(question).Any(o => o.Selected);

	private static string NightReaction(int question, int option, NightOption[] options) {
		if (options[option].Blank) return NightBlankLine;
		if (question == NightPrompts.Length - 1) return NightLastLine;
		return NightSettledLines[(question * 3 + option) % NightSettledLines.Length];
	}

	/// <summary>One question on one page. A rebuild that isn't a page turn paints the settled page straight away.</summary>
	private void FoundingNightStep() {
		NightEntry entry = nightEntry;
		nightEntry = NightEntry.Settled;
		NightStop();
		nightIndex = Mathf.Clamp(nightIndex, 0, NightPrompts.Length - 1);
		NightPrompt prompt = NightPrompts[nightIndex];
		NightOption[] options = NightOptions(nightIndex);
		int chosen = Array.FindIndex(options, o => o.Selected);
		bool full = entry == NightEntry.Full;

		var page = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		page.AddThemeConstantOverride("separation", 12);
		content.AddChild(page);
		nightPage = page;
		page.AddChild(new Control { CustomMinimumSize = new Vector2(0, 30) });
		page.AddChild(NightPips());

		// The question, twice: the ghost slides about behind it until the head clears enough to read.
		var holder = new Control { CustomMinimumSize = new Vector2(0, 70), SizeFlagsHorizontal = SizeFlags.ExpandFill };
		page.AddChild(holder);
		Font display = PaperTheme.Lettering(LetteringStyle.Didone);
		Label ghost = NightLine(prompt.Question, 46, NightOxblood, display);
		Label face = NightLine(prompt.Question, 46, Ink, display);
		holder.AddChild(ghost);
		holder.AddChild(face);
		ghost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		face.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		nightGhosts.Add((ghost, new Vector2(11f, 4f)));

		Label hint = FoundingText(prompt.Hint, 18, Heard, PaperTheme.SerifItalic);
		hint.HorizontalAlignment = HorizontalAlignment.Center;
		page.AddChild(hint);
		var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("separation", 7);
		page.AddChild(list);
		var rows = new List<Button>();
		var reaction = FoundingText(chosen >= 0 ? NightReaction(nightIndex, chosen, options) : " ", 18, Heard, PaperTheme.SerifItalic);
		reaction.HorizontalAlignment = HorizontalAlignment.Center;
		for (int i = 0; i < options.Length; i++) {
			int index = i;
			Button row = NightAnswer(options[i].Text);
			StyleNightRow(row, i == chosen, options[i].Blank);
			if (chosen >= 0 && i != chosen) row.Modulate = new Color(1, 1, 1, .4f);
			row.Pressed += () => NightPick(options, index, rows, reaction);
			list.AddChild(row);
			rows.Add(row);
		}
		page.AddChild(reaction);

		// BACK is always there after the first question; NEXT only once there's something to carry forward.
		var footer = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44) };
		content.AddChild(footer);
		if (nightIndex > 0) {
			var back = Btn("←  BACK");
			back.CustomMinimumSize = new Vector2(120, 42);
			back.Pressed += () => NightTurnTo(nightIndex - 1, NightEntry.Settled);
			footer.AddChild(back);
		}
		footer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		nightNextButton = Primary(nightIndex == NightPrompts.Length - 1 ? "THAT'S ALL I'VE GOT  →" : "NEXT  →");
		nightNextButton.CustomMinimumSize = new Vector2(260, 44);
		nightNextButton.Visible = chosen >= 0;
		nightNextButton.Pressed += NightForward;
		footer.AddChild(nightNextButton);

		if (full) {
			nightFocus = 0f;
			holder.Modulate = new Color(1, 1, 1, 0);
			hint.Modulate = new Color(1, 1, 1, 0);
			foreach (Button row in rows) { row.Modulate = new Color(1, 1, 1, 0); row.MouseFilter = MouseFilterEnum.Ignore; }
			var tween = nightEntrance = CreateTween().SetParallel();
			tween.TweenMethod(Callable.From<float>(v => nightFocus = v), 0f, 1f, 1.5f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(holder, "modulate:a", 1f, .9f);
			tween.TweenProperty(hint, "modulate:a", 1f, .6f).SetDelay(.8);
			for (int i = 0; i < rows.Count; i++) {
				Button row = rows[i];
				double at = 1.1 + i * .24;
				tween.TweenProperty(row, "modulate:a", 1f, .45f).SetDelay(at);
				tween.TweenCallback(Callable.From(() => row.MouseFilter = MouseFilterEnum.Stop)).SetDelay(at);
			}
			// A click on the bare page snaps the rest of the entrance in: the head isn't that bad.
			page.GuiInput += input => {
				if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) nightEntrance?.CustomStep(10);
			};
		} else if (entry == NightEntry.Fade) {
			page.Modulate = new Color(1, 1, 1, 0);
			nightEntrance = CreateTween();
			nightEntrance.TweenProperty(page, "modulate:a", 1f, .35f);
		}
	}

	/// <summary>An answer: it goes dark and sure of itself, the rest fall away, and after a beat the page turns.</summary>
	private void NightPick(NightOption[] options, int index, List<Button> rows, Label reaction) {
		if (nightBusy) return;
		options[index].Pick();
		nightEntrance?.CustomStep(10);
		nightEntrance?.Kill();
		nightFocus = 1f;
		for (int i = 0; i < rows.Count; i++) {
			StyleNightRow(rows[i], i == index, options[i].Blank);
			rows[i].MouseFilter = MouseFilterEnum.Stop;
			CreateTween().TweenProperty(rows[i], "modulate:a", i == index ? 1f : .4f, .22f);
		}
		reaction.Text = NightReaction(nightIndex, index, options);
		reaction.Modulate = new Color(1, 1, 1, 0);
		CreateTween().TweenProperty(reaction, "modulate:a", 1f, .45f);
		nightNextButton.Visible = true;
		nightAdvance?.Kill();
		nightAdvance = CreateTween();
		nightAdvance.TweenInterval(1.35);
		nightAdvance.TweenCallback(Callable.From(NightForward));
	}

	private void NightForward() {
		if (nightIndex < NightPrompts.Length - 1) NightTurnTo(nightIndex + 1, NightAnswered(nightIndex + 1) ? NightEntry.Fade : NightEntry.Full);
		else NightClear();
	}

	/// <summary>The page goes under, the head throbs once, and the next question surfaces.</summary>
	private void NightTurnTo(int question, NightEntry entry) {
		if (nightBusy) return;
		nightAdvance?.Kill();
		nightBusy = true;
		nightBlink = 1f;
		var turn = nightTurn = CreateTween().SetParallel();
		if (IsInstanceValid(nightPage)) turn.TweenProperty(nightPage, "modulate:a", 0f, .3f);
		turn.Chain().TweenCallback(Callable.From(() => {
			nightBusy = false;
			nightIndex = question;
			nightEntry = entry;
			Refresh();
		}));
	}

	/// <summary>The last answer is in: the pages go quiet, the haze lifts, and the practical pages come up clear.</summary>
	private void NightClear() {
		if (nightBusy) return;
		nightAdvance?.Kill();
		nightBusy = true;
		nightBlink = 1f;
		var turn = nightTurn = CreateTween().SetParallel();
		if (IsInstanceValid(nightPage)) turn.TweenProperty(nightPage, "modulate:a", 0f, .4f);
		turn.Chain().TweenCallback(Callable.From(() => {
			nightBusy = false;
			GoToFoundingStep(1);
			content.Modulate = new Color(1, 1, 1, 0);
			CreateTween().TweenProperty(content, "modulate:a", 1f, .7f);
		}));
	}

	// ---- parts --------------------------------------------------------------------------------------------------------

	private static Label NightLine(string text, int size, Color color, Font font) {
		var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off, MouseFilter = MouseFilterEnum.Ignore };
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	/// <summary>Four marks across the top: the ones you've been through are filled, the one you're on is ringed.</summary>
	private Control NightPips() {
		var pips = new Control { CustomMinimumSize = new Vector2(0, 18), SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
		int current = nightIndex;
		pips.Draw += () => {
			float step = 30f, left = pips.Size.X / 2f - step * (NightPrompts.Length - 1) / 2f;
			for (int i = 0; i < NightPrompts.Length; i++) {
				var at = new Vector2(left + i * step, 9f);
				if (i == current) pips.DrawArc(at, 6f, 0f, Mathf.Tau, 24, NightOxblood, 2f);
				else if (i < current || NightAnswered(i)) pips.DrawCircle(at, 4.5f, new Color(Ink, .8f));
				else pips.DrawArc(at, 4f, 0f, Mathf.Tau, 20, new Color(Ink, .3f), 1.5f);
			}
		};
		pips.Resized += pips.QueueRedraw;
		return pips;
	}

	private static Button NightAnswer(string text) {
		var row = new Button { Text = text, Alignment = HorizontalAlignment.Left, FocusMode = FocusModeEnum.None,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(760, 50), MouseDefaultCursorShape = CursorShape.PointingHand };
		row.AddThemeFontOverride("font", PaperTheme.SerifItalic);
		row.AddThemeFontSizeOverride("font_size", 22);
		return row;
	}

	private static StyleBoxFlat NightRowBox(Color fill, Color edge, int left) => new() {
		BgColor = fill, BorderColor = edge,
		BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
		CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
		ContentMarginLeft = left, ContentMarginRight = 18, ContentMarginTop = 8, ContentMarginBottom = 8,
	};

	private static void StyleNightRow(Button row, bool selected, bool blank) {
		StyleBoxFlat rest = NightRowBox(selected ? NightOxblood : new Color(CardFill, .35f), selected ? NightOxblood : new Color(CardEdge, .55f), 22);
		StyleBoxFlat hover = NightRowBox(selected ? NightOxblood : new Color("f3e6c2"), selected ? NightOxblood : CardEdge, 34);   // leans in to be read
		row.AddThemeStyleboxOverride("normal", rest);
		row.AddThemeStyleboxOverride("pressed", rest);
		row.AddThemeStyleboxOverride("hover", hover);
		row.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		Color ink = selected ? Paper : blank ? Heard : Ink;
		foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color" }) row.AddThemeColorOverride(state, ink);
	}
}
