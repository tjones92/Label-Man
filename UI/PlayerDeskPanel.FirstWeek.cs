using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The first-week card (SimTools/OnboardingTutorialDirective.md §3.2, T3): a to-do card pinned under the desk header in
/// place of NEXT UP while it lasts. The whole list is visible, so the player sees the road ahead; the line they're on is
/// taught by the voice whose job it is; one button on the page (the next thing to press) glows. SHOW ME opens the tab
/// that does the step; I KNOW THE WAY puts the card away for good. Once every line ticks, NEXT UP takes over as before.
/// The rules live on <see cref="PlayerDesk"/> (PlayerDesk.FirstWeek.cs).
/// </summary>
public partial class PlayerDeskPanel {
	private PanelContainer firstWeekCard;
	private VBoxContainer firstWeekBody;
	private static readonly Color FirstWeekGlow = new(1.0f, 0.86f, 0.45f);

	/// <summary>Built once beside NEXT UP; filled each refresh.</summary>
	private void BuildFirstWeekCard(Container into) {
		firstWeekCard = new PanelContainer { Visible = false, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var note = PaperStyleBox.Sheet(new Color("f3e7c2"), 14, 8, 8, new Color("8a7048"));
		note.ShadowAlpha = 0.35f; note.Radius = 1; note.BorderWidth = 1;
		firstWeekCard.AddThemeStyleboxOverride("panel", note);
		firstWeekBody = new VBoxContainer();
		firstWeekBody.AddThemeConstantOverride("separation", 4);
		firstWeekCard.AddChild(firstWeekBody);
		into.AddChild(firstWeekCard);
		StartFirstWeekWatch();
	}

	/// <summary>The card while it's on the desk, with NEXT UP hidden; otherwise nothing, and NEXT UP back.</summary>
	private void RenderFirstWeek(PlayerDesk desk) {
		if (firstWeekCard == null) return;
		var step = desk.FirstWeekCurrent;
		firstWeekCard.Visible = step != null;
		nextUpLabel.Visible = step == null;
		if (step == null) return;
		foreach (Node child in firstWeekBody.GetChildren()) { firstWeekBody.RemoveChild(child); child.QueueFree(); }

		// The list, one line each, ticked from the run.
		var top = new HBoxContainer();
		top.AddThemeConstantOverride("separation", 14);
		var title = new Label { Text = "This week", VerticalAlignment = VerticalAlignment.Center };
		title.AddThemeFontOverride("font", PaperTheme.Lettering(LetteringStyle.Script));
		title.AddThemeFontSizeOverride("font_size", 22);
		title.AddThemeColorOverride("font_color", Rust);
		top.AddChild(title);
		var list = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("h_separation", 16);
		foreach (var s in PlayerDesk.FirstWeekSteps) {
			bool done = desk.FirstWeekDone(s), now = s == step;
			var item = new Label { Text = (done ? "✓ " : now ? "▸ " : "· ") + PlayerDesk.FirstWeekTitle(s), VerticalAlignment = VerticalAlignment.Center };
			item.AddThemeFontOverride("font", now ? PaperTheme.SansBold : PaperTheme.Serif);
			item.AddThemeFontSizeOverride("font_size", 15);
			item.AddThemeColorOverride("font_color", done ? new Color("8a7a5c") : now ? Ink : new Color("6b5d45"));
			list.AddChild(item);
		}
		top.AddChild(list);
		var show = Btn("SHOW ME");
		show.TooltipText = "Open the page that does this step.";
		show.Pressed += () => ShowFirstWeekStep(PlayerDesk.Instance);
		top.AddChild(show);
		var away = Btn("I KNOW THE WAY");
		away.TooltipText = "Put the card away for good. NEXT UP still suggests a step.";
		away.Pressed += () => { PlayerDesk.Instance?.PutAwayFirstWeek(); Refresh(); };
		top.AddChild(away);
		firstWeekBody.AddChild(top);

		// The line you're on, taught by the voice whose job it is; and a second voice when it has something to add.
		var (voice, how) = desk.FirstWeekHow(step.Value);
		firstWeekBody.AddChild(FirstWeekVoiceLine(voice, how));
		if (desk.FirstWeekAside(step.Value) is { } aside) firstWeekBody.AddChild(FirstWeekVoiceLine(aside.Voice, aside.Aside));
	}

	private Control FirstWeekVoiceLine(ExecutiveVoice voice, string text) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		var badge = new LinocutIcon().Set(VoiceInstinct(voice), 22f);
		badge.MouseFilter = MouseFilterEnum.Ignore;
		badge.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		row.AddChild(badge);
		var label = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
		label.AddThemeFontOverride("normal_font", PaperTheme.SerifItalic);
		label.AddThemeFontOverride("bold_font", PaperTheme.SansBold);
		label.AddThemeFontSizeOverride("normal_font_size", 15);
		label.AddThemeFontSizeOverride("bold_font_size", 12);
		label.AddThemeColorOverride("default_color", VoiceColor(voice));
		label.Text = $"[b]{VoiceName(voice)}[/b]   {text}";
		row.AddChild(label);
		return row;
	}

	/// <summary>The page that does the step.</summary>
	private void ShowFirstWeekStep(PlayerDesk desk) {
		var step = desk?.FirstWeekCurrent;
		if (step == null) return;
		switch (step.Value) {
			case FirstWeekStep.Answer:
				Say("END THE DAY is in the header, beside the WAIT buttons. The Morning Paper says who called back.", true);
				break;
			case FirstWeekStep.Record:
				if (desk.Session != null) UIManager.Instance?.OpenStudio(desk.Session.ArtistId);
				else GoToTab(1, PageRoster);
				break;
			default:
				GoToTab(0, PageAandR);
				break;
		}
	}

	/// <summary>The buttons each line points at, best first. The first one on screen glows; only ever one.</summary>
	private static string[] FirstWeekTargets(FirstWeekStep step) => step switch {
		FirstWeekStep.Hear => new[] { "LISTEN", "GO & WAIT" },
		FirstWeekStep.Ask => new[] { "IF THEY'D HEAR AN OFFER", "ASK IF THEY'D HEAR AN OFFER", "TALK TO THEM" },
		FirstWeekStep.Answer => new[] { "END THE DAY" },
		FirstWeekStep.Terms => new[] { "TERMS — MAKE AN OFFER", "DISCUSS TERMS", "TALK TO THEM" },
		FirstWeekStep.Sign => new[] { "TABLE OFFER", "OFFER CONTRACT" },
		_ => new[] { "TO THE ROSTER", "TAKE INTO STUDIO", "RETURN TO STUDIO" },
	};

	/// <summary>Glow on the next thing to press, if it's on screen. Rebuilt pages bring fresh buttons, so it's re-applied
	/// after every refresh; the card's own buttons never glow.</summary>
	private void HighlightFirstWeekTarget(PlayerDesk desk) {
		var step = desk?.FirstWeekCurrent;
		if (step == null) return;
		var buttons = new List<Button>();
		void Walk(Node n) {
			if (n == firstWeekCard) return;
			if (n is Button b && b.IsVisibleInTree() && !b.Disabled && !HeldActWhileOwnSigned(desk, b)) buttons.Add(b);
			foreach (Node c in n.GetChildren()) Walk(c);
		}
		Walk(this);
		// A modal (the act's conversation, the contract sheet) sits over the page: its buttons come first.
		var modal = buttons.Where(b => FindParentOfType<PaperModal>(b) != null).ToList();
		Button hit = null;
		foreach (var pool in new[] { modal, buttons }) {
			foreach (string target in FirstWeekTargets(step.Value)) {
				hit = pool.FirstOrDefault(b => b.Text.StartsWith(target, System.StringComparison.Ordinal));
				if (hit != null) break;
			}
			if (hit != null) break;
		}
		GlowOn(hit);
	}

	/// <summary>A studio button for the holder's act (Connie) once the player has signed one of their own: the card means the
	/// act they just signed. Connie's still glows when she's the only act.</summary>
	private static bool HeldActWhileOwnSigned(PlayerDesk desk, Button b) {
		if (!b.HasMeta("artist_id") || !desk.FirstWeekDone(FirstWeekStep.Sign)) return false;
		var artist = ArtistManager.Instance?.GetArtist(b.GetMeta("artist_id").AsString());
		return desk.IsHeldByHolder(artist);
	}

	private Button firstWeekGlowing;
	private Tween firstWeekPulse;

	/// <summary>Only ever one glowing button: the old one goes back to plain when the next thing to press changes.</summary>
	private void GlowOn(Button hit) {
		if (hit == firstWeekGlowing && IsInstanceValid(hit) && firstWeekPulse?.IsValid() == true) return;
		if (firstWeekPulse?.IsValid() == true) firstWeekPulse.Kill();
		if (firstWeekGlowing != null && IsInstanceValid(firstWeekGlowing)) {
			firstWeekGlowing.SelfModulate = Colors.White;
			firstWeekGlowing.RemoveMeta("first_week_glow");
		}
		firstWeekGlowing = hit;
		firstWeekPulse = null;
		if (hit == null) return;
		hit.SetMeta("first_week_glow", true);
		firstWeekPulse = hit.CreateTween().SetLoops();
		firstWeekPulse.TweenProperty(hit, "self_modulate", FirstWeekGlow, 0.6).SetTrans(Tween.TransitionType.Sine);
		firstWeekPulse.TweenProperty(hit, "self_modulate", Colors.White, 0.6).SetTrans(Tween.TransitionType.Sine);
	}

	private static T FindParentOfType<T>(Node n) where T : class {
		for (var p = n?.GetParent(); p != null; p = p.GetParent()) if (p is T t) return t;
		return null;
	}

	/// <summary>Modals open without a refresh, so the glow looks again twice a second while the card is up.</summary>
	private void StartFirstWeekWatch() {
		var timer = new Timer { WaitTime = 0.5, Autostart = true };
		timer.Timeout += () => {
			var desk = PlayerDesk.Instance;
			if (desk?.FirstWeekCurrent == null || !IsVisibleInTree()) { GlowOn(null); return; }
			HighlightFirstWeekTarget(desk);
		};
		AddChild(timer);
	}
}
