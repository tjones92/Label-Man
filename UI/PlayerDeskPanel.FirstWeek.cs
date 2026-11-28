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
		away.Pressed += () => { PlayerDesk.Instance?.PutAwayFirstWeek(); FirstSessionTrace.CardPutAway(); Refresh(); };
		top.AddChild(away);
		firstWeekBody.AddChild(top);
		// The teaching isn't on the card any more (SigningExperienceAudit §3.5): each voice speaks in the place the work
		// happens (FirstWeekTeaching below, The Set, The Talk, The Table). The card lists the lines and points the way.
	}

	/// <summary>The current line's voices, in the place its work happens. Nothing when the card is away.</summary>
	private void FirstWeekTeaching(PlayerDesk desk, FirstWeekPlace place) {
		var lines = desk.FirstWeekLinesAt(place);
		if (lines.Count == 0) return;
		var box = new VBoxContainer { Name = "FirstWeekTeaching" };
		box.AddThemeConstantOverride("separation", 4);
		foreach (var (voice, text) in lines)
			if (voice != PlayerDesk.Instance?.FatigueQuietVoice) box.AddChild(FirstWeekVoiceLine(voice, text));
		content.AddChild(box);
	}

	/// <summary>Lines the desk worked out for this moment (a counter at the table), on the page as voices.</summary>
	private void FirstWeekLines(List<(ExecutiveVoice Voice, string Text)> lines) {
		if (lines.Count == 0) return;
		var box = new VBoxContainer { Name = "FirstWeekTeaching" };
		box.AddThemeConstantOverride("separation", 4);
		foreach (var (voice, text) in lines)
			if (voice != PlayerDesk.Instance?.FatigueQuietVoice) box.AddChild(FirstWeekVoiceLine(voice, text));
		content.AddChild(box);
	}

	/// <summary>Set while the terms sheet is built after a counter in the first week: the term they're stuck on, and their ask.</summary>
	private (ContractAxis Axis, ContractTermSheet Ask)? firstWeekSweeten;

	/// <summary>Marks the blank for the term they're stuck on, with where it starts and their number, so the glow can sit on
	/// it until the player moves it (then TABLE AGAIN glows). Nothing outside a first-week counter, or when it already
	/// matches their ask.</summary>
	private void TagFirstWeekField(SpinBox advance, SpinBox royalty, SpinBox term, SpinBox singles, CheckBox labelPub, CheckBox artistControl) {
		if (firstWeekSweeten is not { } fw) return;
		Control field = fw.Axis switch {
			ContractAxis.Advance => advance, ContractAxis.Royalty => royalty, ContractAxis.Term => term,
			ContractAxis.Deliverables => singles, ContractAxis.Publishing => labelPub, _ => artistControl,
		};
		double to = fw.Axis switch {
			ContractAxis.Advance => Mathf.Round(fw.Ask.Advance),
			ContractAxis.Royalty => Mathf.Round(fw.Ask.RoyaltyRate * 4000f) / 40f,
			ContractAxis.Term => fw.Ask.TermYears,
			ContractAxis.Deliverables => fw.Ask.SinglesObligation,
			ContractAxis.Publishing => fw.Ask.LabelOwnsPublishing ? 1 : 0,
			_ => fw.Ask.ArtistCreativeControl ? 1 : 0,
		};
		double from = FirstWeekFieldValue(field);
		if (Mathf.IsEqualApprox((float)from, (float)to)) return;
		field.SetMeta("first_week_field", true);
		field.SetMeta("first_week_from", from);
		field.SetMeta("first_week_to", to);
	}

	private static double FirstWeekFieldValue(Control field) => field is SpinBox s ? s.Value : field is BaseButton b && b.ButtonPressed ? 1 : 0;

	/// <summary>The same lines as transcript reads, for the scenes drawn as a call (The Talk, The Table).</summary>
	private static IEnumerable<CallLine> FirstWeekReads(PlayerDesk desk, FirstWeekPlace place) =>
		desk.FirstWeekLinesAt(place).Select(l => new CallLine { beat = RolodexSceneBeat.PassiveRead, voice = l.Voice, text = l.Text });

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
			case FirstWeekStep.Answer when desk.FirstWeekVisitToday:
				OpenAppointmentBook(desk);
				break;
			case FirstWeekStep.Answer when desk.FirstPendingApproachStep != null:
			case FirstWeekStep.Answer when desk.FirstWeekHearAgainId != null:
				GoToTab(0, PageAandR);
				break;
			case FirstWeekStep.Answer:
				Say("END THE DAY is in the header, beside the WAIT buttons. The Morning Paper says who called back.", true);
				break;
			case FirstWeekStep.Record when desk.FirstWeekStudioTomorrow:
				Say("Too late for a studio date today. END THE DAY is in the header; book the room in the morning.", true);
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
	private string[] FirstWeekTargets(PlayerDesk desk, FirstWeekStep step) => step switch {
		// A no so far: not waiting, but hearing somebody else and asking them. Never the act that already said no.
		FirstWeekStep.Answer when desk.FirstWeekNeedsAnotherAct => theTalkArtistId != null && desk.DirectApproaches.All(a => a.ArtistId != theTalkArtistId)
			? new[] { "▸  Would you hear an offer from us?", "▸  Let's sit down on terms." }
			: new[] { "END THE CONVERSATION", "FIND A SET", "NEXT SETS" },
		// Asked to come back: the asking in The Talk, else the set finder (where its next set glows), else a day on.
		// Until they've been heard again, asking again gets the same answer: leave the conversation and go to their set.
		FirstWeekStep.Answer when desk.FirstWeekHearAgainId is { } again =>
			desk.WaitingToBeHeardAgain(ArtistManager.Instance?.GetArtist(again))
				? new[] { "END THE CONVERSATION", "FIND A SET", "NEXT SETS", "END THE DAY" }
				: new[] { "▸  Would you hear an offer from us?", "▸  Let's sit down on terms.", "FIND A SET", "NEXT SETS", "END THE DAY" },
		// Never a set: the player picks which act to hear (audit §2.7). The view glows; the voices talk about tonight's rooms.
		FirstWeekStep.Hear => new[] { "FIND A SET", "NEXT SETS" },
		// The Talk's lines (PlayerDesk.TheTalk) lead when the conversation has the page.
		FirstWeekStep.Ask => new[] { "▸  Would you hear an offer from us?", "▸  Let's sit down on terms.", "ASK IF THEY'D HEAR AN OFFER", "TALK TO THEM", "SIT DOWN ON TERMS" },
		// The appointment book (onboarding T5): their times are picked in the conversation (glowing together, below); on
		// the day, the book; before it, a day on. The book never reminds, so the glow goes to the book, not the visit.
		FirstWeekStep.Answer when desk.FirstWeekVisit is { } v && PlayerDesk.NeedsATime(v) => new[] { "TALK TO THEM", "END THE DAY" },
		// Today, but not yet: the sets that fit before it's time to go glow with the wait (HighlightFirstWeekTarget); then
		// the book, then going there.
		FirstWeekStep.Answer when desk.FirstWeekVisitLater => new[] { "END THE CONVERSATION", "LET THEM GO", "CLOSE THE BOOK",
			(desk.FirstWeekLeaveAt ?? 0) - desk.NowMinute >= 180 ? "WAIT 3h" : "WAIT 1h" },
		FirstWeekStep.Answer when desk.FirstWeekVisitToday => new[] { "GO THERE", "THE BOOK" },
		FirstWeekStep.Answer when desk.FirstWeekVisit is { } v && PlayerDesk.Booked(v) => new[] { "END THE CONVERSATION", "CLOSE THE BOOK", "END THE DAY" },
		FirstWeekStep.Answer => new[] { "▸  I'll come to Sunday dinner", "▸  I'll sit down with", "MEET THE FAMILY", "SIT DOWN WITH THE MANAGER", "END THE DAY" },
		FirstWeekStep.Terms => new[] { "▸  Let's sit down on terms.", "SIT DOWN ON TERMS", "TALK TO THEM" },
		// Too late for the meeting: leave the sheet and end the day. Otherwise the sheet, or the way back to it.
		FirstWeekStep.Sign when desk.FirstWeekTableTomorrow => new[] { "NOT NOW", "END THE CONVERSATION", "END THE DAY" },
		// A counter: give them part of what they named (the Suit says so on the page).
		FirstWeekStep.Sign => new[] { "SWEETEN IT", "TABLE OFFER", "TABLE AGAIN", "▸  Let's sit down on terms.", "SIT DOWN ON TERMS", "TALK TO THEM" },
		// Too late for a date: the signing card's TO THE ROSTER (a modal, looked at first), then the day's end.
		FirstWeekStep.Record when desk.FirstWeekStudioTomorrow => new[] { "TO THE ROSTER", "END THE DAY" },
		// A booking that warns opens a modal over the sheet: go ahead with it. (A push-back has its own glow, above.)
		_ => new[] { "TO THE ROSTER", "TAKE INTO STUDIO", "RETURN TO STUDIO", "BOOK IT" },
	};

	/// <summary>Glow on the next thing to press, if it's on screen. Rebuilt pages bring fresh buttons, so it's re-applied
	/// after every refresh; the card's own buttons never glow.</summary>
	private void HighlightFirstWeekTarget(PlayerDesk desk) {
		var step = desk?.FirstWeekCurrent;
		if (step == null) return;
		var buttons = new List<Button>();
		var fields = new List<Control>();
		void Walk(Node n) {
			if (n == firstWeekCard) return;
			if (n is Control f && f.HasMeta("first_week_field") && f.IsVisibleInTree()) fields.Add(f);
			if (n is Button b && b.IsVisibleInTree() && !b.Disabled && !HeldActWhileOwnSigned(desk, b)) buttons.Add(b);
			foreach (Node c in n.GetChildren()) Walk(c);
		}
		Walk(this);
		// A modal (the act's conversation, the contract sheet) sits over the page: its buttons come first.
		var modal = buttons.Where(b => FindParentOfType<PaperModal>(b) != null).ToList();
		// The set the player chose and walked to: its LISTEN is the next thing to press.
		Button hit = desk.FirstWeekLooking ? buttons.FirstOrDefault(b => b.HasMeta("first_week_here")) : null;
		if (hit != null) { GlowOn(hit); return; }
		// Choosing who to hear: with the sets listed, every one of them glows and the player picks (playtest 2026-11-26: a
		// glowing FIND A SET over the open finder was a press that did nothing).
		if (modal.Count == 0 && (step == FirstWeekStep.Hear || step == FirstWeekStep.Answer && desk.FirstWeekNeedsAnotherAct && theTalkArtistId == null)) {
			var sets = buttons.Where(b => b.HasMeta("scouting_artist") && b.Text is "GO & WAIT" or "LISTEN").Cast<Control>().ToList();
			if (sets.Count > 0) { GlowOnAll(sets); return; }
		}
		// An act that asked to be heard again: its next set, then (at The Set) talking to it when it comes off the stand.
		if (desk.FirstWeekHearAgainId is { } again && modal.Count == 0) {
			bool atTheSet = buttons.Any(b => b.Text == "LET THEM GO");
			hit = atTheSet
				? buttons.FirstOrDefault(b => theSetQueue.FirstOrDefault() == again ? b.Text is "TALK TO THEM" or "SIT DOWN ON TERMS" : b.Text == "LET THEM GO")
				: buttons.FirstOrDefault(b => b.HasMeta("scouting_artist") && b.GetMeta("scouting_artist").AsString() == again);
			// The sets are listed and theirs isn't among them yet: a day on.
			if (hit == null && !atTheSet && buttons.Any(b => b.HasMeta("scouting_artist"))) hit = buttons.FirstOrDefault(b => b.Text == "END THE DAY");
			if (hit != null) { GlowOn(hit); return; }
		}
		// The studio's booking sheet: two songs ticked (or every one there is), then the room booked. The sheet is long, so
		// the glow is scrolled into view when it moves.
		if (studioMode && step == FirstWeekStep.Record) {
			var songs = buttons.Where(b => b.HasMeta("first_week_song")).ToList();
			// Too late for a date today: back to the desk, where the day ends.
			if (desk.FirstWeekStudioTomorrow && modal.Count == 0) {
				GlowOn(buttons.FirstOrDefault(b => b.Text.StartsWith("‹ BACK", System.StringComparison.Ordinal)));
				return;
			}
			static bool Refused(Button b) => b.HasMeta("first_week_refused") && b.GetMeta("first_week_refused").AsBool();
			// The act pushed back on a song (THE ACT PUSHES BACK): set it aside while there's another to tick; when they've
			// turned down everything, insist (or, under creative control, set it aside and go back for material).
			if (modal.FirstOrDefault(b => b.Text == "SET THIS ASIDE") is { } aside) {
				GlowOn(songs.Any(b => !Refused(b)) ? aside : modal.FirstOrDefault(b => b.Text.StartsWith("INSIST", System.StringComparison.Ordinal)) ?? aside);
				return;
			}
			if (songs.Count > 0 && modal.Count == 0) {
				var next = songs.Count(b => b.ButtonPressed) < 2 ? songs.FirstOrDefault(b => !b.ButtonPressed && !Refused(b)) : null;
				hit = next ?? buttons.FirstOrDefault(b => b.Text.StartsWith("BOOK THE ROOM", System.StringComparison.Ordinal))
					// Nothing ticked and nothing left they'd play: back to the roster for a song they want.
					?? buttons.FirstOrDefault(b => b.Text.StartsWith("‹ BACK", System.StringComparison.Ordinal));
				bool moved = hit != firstWeekGlowing;
				GlowOn(hit);
				if (moved && hit != null) contentScroll?.EnsureControlVisible(hit);
				return;
			}
		}
		// A visit later today: every set that's over before it's time to leave glows, with the wait beside them.
		if (step == FirstWeekStep.Answer && desk.FirstWeekVisitLater && modal.Count == 0 && buttons.Any(b => b.HasMeta("set_ends"))) {
			int leave = desk.SceneDay * 24 * 60 + (desk.FirstWeekLeaveAt ?? 0);
			var fits = buttons.Where(b => b.HasMeta("set_ends") && b.GetMeta("set_ends").AsInt32() <= leave && b.Text is "GO & WAIT" or "LISTEN").Cast<Control>().ToList();
			string wait = (desk.FirstWeekLeaveAt ?? 0) - desk.NowMinute >= 180 ? "WAIT 3h" : "WAIT 1h";
			if (buttons.FirstOrDefault(b => b.Text == wait) is { } waitButton) fits.Add(waitButton);
			if (fits.Count > 0) { GlowOnAll(fits); return; }
		}
		// Their side's two times for a visit: both glow, and the player picks (the appointment book).
		if (step == FirstWeekStep.Answer && desk.FirstWeekVisit is { } visit && PlayerDesk.NeedsATime(visit)) {
			var times = (modal.Count > 0 ? modal : buttons).Where(b => b.Text.TrimEnd().EndsWith("suits me.", System.StringComparison.Ordinal)).Cast<Control>().ToList();
			if (times.Count > 0) { GlowOnAll(times); return; }
		}
		// Back from a counter: the blank for the term they're stuck on, until the player moves it; then TABLE AGAIN.
		if (step == FirstWeekStep.Sign && modal.Count == 0 && !desk.FirstWeekTableTomorrow
			&& fields.FirstOrDefault(f => Mathf.IsEqualApprox((float)FirstWeekFieldValue(f), (float)f.GetMeta("first_week_from").AsDouble())) is { } field) {
			if (field != firstWeekGlowing) firstWeekScrollTo = field;   // after the page's layout, on the next tick
			GlowOn(field);
			return;
		}
		foreach (var pool in new[] { modal, buttons }) {
			foreach (string target in FirstWeekTargets(desk, step.Value)) {
				hit = pool.FirstOrDefault(b => b.Text.StartsWith(target, System.StringComparison.Ordinal));
				if (hit != null) break;
			}
			if (hit != null) break;
		}
		// The counter's answers sit under the table's whole transcript: bring the one that glows into view, once the
		// rebuilt page has its layout (the watch's next tick).
		if (hit != null && hit != firstWeekGlowing && hit.Text == "SWEETEN IT") firstWeekScrollTo = hit;
		GlowOn(hit);
	}

	/// <summary>A studio button for the holder's act (Connie) once the player has signed one of their own: the card means the
	/// act they just signed. Connie's still glows when she's the only act.</summary>
	private static bool HeldActWhileOwnSigned(PlayerDesk desk, Button b) {
		if (!b.HasMeta("artist_id") || !desk.FirstWeekDone(FirstWeekStep.Sign)) return false;
		var artist = ArtistManager.Instance?.GetArtist(b.GetMeta("artist_id").AsString());
		return desk.IsHeldByHolder(artist);
	}

	/// <summary>The first of what glows (what the scroll and the checks follow).</summary>
	private Control firstWeekGlowing;
	private List<Control> firstWeekGlowSet = new();
	private Control firstWeekScrollTo;
	private readonly List<Tween> firstWeekPulses = new();
	/// <summary>The grease-pencil ring's ink: a copy editor's blue, which reads on the beige paper and on the oxblood buttons
	/// alike (a yellow tint on the face vanished on the red ones; playtest 2026-11-26).</summary>
	private static readonly Color FirstWeekRingInk = new("2f5fb3");

	/// <summary>One thing to press glows. The old glow goes back to plain when the next thing to press changes.</summary>
	private void GlowOn(Control hit) => GlowOnAll(hit == null ? null : new List<Control> { hit });

	/// <summary>Several equal choices glow together (the sets you could go and hear): the card points the way and leaves the
	/// choice to the player. Each gets a pulsing ring, drawn around it like a pencil circle, and a light tint.</summary>
	private void GlowOnAll(List<Control> hits) {
		hits = hits?.Where(h => h != null && IsInstanceValid(h)).Distinct().ToList() ?? new();
		bool same = hits.Count == firstWeekGlowSet.Count && hits.Zip(firstWeekGlowSet).All(p => p.First == p.Second);
		if (same && (hits.Count == 0 || firstWeekPulses.Count > 0 && firstWeekPulses.All(t => t.IsValid()))) return;
		foreach (var t in firstWeekPulses) if (t.IsValid()) t.Kill();
		firstWeekPulses.Clear();
		foreach (var old in firstWeekGlowSet.Where(IsInstanceValid)) {
			old.SelfModulate = Colors.White;
			old.Modulate = Colors.White;
			old.RemoveMeta("first_week_glow");
			if (old.GetNodeOrNull("FirstWeekRing") is Node ring) { old.RemoveChild(ring); ring.QueueFree(); }
		}
		firstWeekGlowSet = hits;
		firstWeekGlowing = hits.FirstOrDefault();
		foreach (var hit in hits) {
			hit.SetMeta("first_week_glow", true);
			// The ring sits just outside the control and never takes the mouse.
			var ring = new Panel { Name = "FirstWeekRing", MouseFilter = MouseFilterEnum.Ignore };
			ring.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
				BgColor = Colors.Transparent, DrawCenter = false, BorderColor = FirstWeekRingInk,
				BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
				CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7,
			});
			ring.SetAnchorsPreset(LayoutPreset.FullRect);
			ring.OffsetLeft = -5; ring.OffsetTop = -5; ring.OffsetRight = 5; ring.OffsetBottom = 5;
			hit.AddChild(ring);
			var ringPulse = ring.CreateTween().SetLoops();
			ringPulse.TweenProperty(ring, "modulate:a", 0.25f, 0.6).SetTrans(Tween.TransitionType.Sine);
			ringPulse.TweenProperty(ring, "modulate:a", 1f, 0.6).SetTrans(Tween.TransitionType.Sine);
			firstWeekPulses.Add(ringPulse);
			// A button tints its own face; a typed blank (a SpinBox draws its text in a child) tints as a whole.
			string property = hit is Button ? "self_modulate" : "modulate";
			var pulse = hit.CreateTween().SetLoops();
			pulse.TweenProperty(hit, property, FirstWeekGlow, 0.6).SetTrans(Tween.TransitionType.Sine);
			pulse.TweenProperty(hit, property, Colors.White, 0.6).SetTrans(Tween.TransitionType.Sine);
			firstWeekPulses.Add(pulse);
		}
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
			if (desk?.FirstWeekCurrent == null) { GlowOn(null); HideFirstWeekSlip(); return; }
			if (!IsVisibleInTree()) {
				GlowOnAll(UIManager.Instance?.OfficeFirstWeekTargets(desk));
				HideFirstWeekSlip();
				return;
			}
			if (firstWeekScrollTo != null && IsInstanceValid(firstWeekScrollTo) && firstWeekScrollTo.IsVisibleInTree()) contentScroll?.EnsureControlVisible(firstWeekScrollTo);
			firstWeekScrollTo = null;
			HighlightFirstWeekTarget(desk);
			PlaceFirstWeekSlip(desk);   // the voice of the moment, beside the glow (T6)
		};
		AddChild(timer);
	}
}
