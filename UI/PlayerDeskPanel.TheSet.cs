using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
	private static readonly Color GigInk = new("f1e4d9"), GigMuted = new("b9a99f"), GigAmber = new("e6b779");
	private static readonly Color GigGround = new("171316"), GigTape = new("211a20");
	private readonly List<string> theSetQueue = new();
	private readonly List<Button> setChoices = new();
	private PanelContainer liveSet;
	private ColorRect gigShade;
	private ScrollContainer gigScroll;
	private static readonly Lazy<Texture2D> GigBackdrop = new(() => {
		const string path = "res://Art/Gig/room_at_night.png";
		if (ResourceLoader.Exists(path)) return GD.Load<Texture2D>(path);
		return Godot.FileAccess.FileExists(path) ? ImageTexture.CreateFromImage(Image.LoadFromFile(path)) : null;
	});
	private SetScene stagedSet;
	private bool setRevealed, focusSetBeat;
	private int setTapeOffset;
	private bool VenueTalkActive => theSetQueue.Count == 0 && stagedSet != null && theTalkArtistId == stagedSet.ArtistId;
	private int shownTalkLines = -1;

	private void OpenTheSet(PlayerDesk desk) {
		theSetQueue.Clear();
		theSetQueue.AddRange(desk.Slate.Select(p => p.Artist?.artistId).Where(id => id != null && desk.BuildSetScene(id) != null).Distinct());
		ResetSetBeat();
		if (theSetQueue.Count == 0) ShowLatestScoutingAct(desk);
		else if (streetPage != null) streetPage = StreetPage.Sets;
	}

	private void DismissTheSet() {
		if (VenueTalkActive) {
			PlayerDesk.Instance.EndTalk(theTalkArtistId);
			theTalkArtistId = null;
		}
		theSetQueue.Clear();
		ResetSetBeat();
	}

	private void ResetSetBeat() {
		stagedSet = null;
		setRevealed = false;
		focusSetBeat = true;
		setTapeOffset = 0;
		shownTalkLines = -1;
	}

	private bool RenderTheSet(PlayerDesk desk) {
		while (theSetQueue.Count > 0 && stagedSet == null) {
			stagedSet = desk.BuildSetScene(theSetQueue[0]);
			if (stagedSet == null) theSetQueue.RemoveAt(0);
		}
		if (stagedSet is not { } scene) return false;
		var talk = VenueTalkActive ? desk.BuildTalkScene(scene.ArtistId) : null;
		if (VenueTalkActive && talk == null) {
			DismissTheSet();
			liveSet?.Hide();
			gigShade?.Hide();
			return false;
		}
		if (liveSet == null) {
			gigShade = new ColorRect { Color = new Color("110d11ed"), MouseFilter = MouseFilterEnum.Ignore };
			AddChild(gigShade);
			gigShade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			liveSet = new PanelContainer { Name = "LiveSet" };
			liveSet.AddThemeStyleboxOverride("panel", GigPanel(GigGround, new Color("735340"), 2));
			AddChild(liveSet);
			liveSet.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
			liveSet.GrowHorizontal = GrowDirection.Both;
			liveSet.GrowVertical = GrowDirection.Both;
			liveSet.OffsetLeft = -740; liveSet.OffsetRight = 740;
			liveSet.OffsetTop = -450; liveSet.OffsetBottom = 450;
		}
		if (!focusSetBeat && IsInstanceValid(gigScroll) && gigScroll.IsVisibleInTree()) setTapeOffset = gigScroll.ScrollVertical;
		Clear(liveSet);
		setChoices.Clear();
		gigShade.Show();
		liveSet.Show();
		streetOverlay.Hide();

		var split = new HBoxContainer();
		split.AddThemeConstantOverride("separation", 0);
		liveSet.AddChild(split);
		var artist = ArtistManager.Instance.GetArtist(scene.ArtistId);
		string genre = artist != null ? GenreNameFormatter.Format(artist.primaryGenre) : null;
		split.AddChild(BuildGigStage(desk, scene, talk != null));

		var side = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1, CustomMinimumSize = new Vector2(500, 0) };
		side.AddThemeStyleboxOverride("panel", GigPanel(GigTape, new Color("493039"), 1, 28));
		split.AddChild(side);
		var tape = new VBoxContainer();
		tape.AddThemeConstantOverride("separation", 14);
		side.AddChild(tape);

		var heading = new HBoxContainer();
		tape.AddChild(heading);
		var title = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		heading.AddChild(title);
		title.AddChild(GigLabel(scene.ArtistName, PaperTheme.SerifBold, 34, GigInk));
		if (genre != null) title.AddChild(GigLabel(genre.ToUpperInvariant(), PaperTheme.TypedBold, 14, GigAmber));
		var map = GigButton("CITY MAP");
		map.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		map.TooltipText = talk != null ? "End the conversation and open the city map."
			: "Leave the rest of this bill and open the city map. The sets you heard are in your notebook.";
		map.Pressed += () => {
			DismissTheSet();
			ShowStreetPage(StreetPage.Map);
		};
		heading.AddChild(map);
		if (talk != null) title.AddChild(GigLabel("TALKING WITH " + talk.Speaker.ToUpperInvariant(), PaperTheme.TypedBold, 14, GigMuted));
		tape.AddChild(GigRule());

		var scroll = new ScrollContainer { Name = "SetDialogueTape", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsVertical = SizeFlags.ExpandFill };
		tape.AddChild(scroll);
		contentScroll = gigScroll = scroll;
		var beats = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		beats.AddThemeConstantOverride("separation", 17);
		scroll.AddChild(beats);
		foreach (string line in scene.Room) beats.AddChild(GigLabel(line, PaperTheme.SerifItalic, 22, GigInk));
		if (scene.SetSnapshot != null) beats.AddChild(GigLabel(scene.SetSnapshot, PaperTheme.Serif, 22, GigInk));

		var decisions = new VBoxContainer();
		decisions.AddThemeConstantOverride("separation", 6);
		tape.AddChild(GigRule());
		tape.AddChild(decisions);

		Control newBeat = null;
		if (talk != null) {
			var (opening, lastReply) = DrawGigTalk(desk, talk, beats, decisions);
			if (shownTalkLines < 0) newBeat = opening;
			else if (shownTalkLines != talk.Lines.Count) newBeat = lastReply;
			shownTalkLines = talk.Lines.Count;
		} else if (!setRevealed) {
			var listen = GigButton("LISTEN CLOSELY", true);
			listen.Pressed += () => {
				setRevealed = true;
				focusSetBeat = true;
				Refresh();
			};
			decisions.AddChild(listen);
			if (focusSetBeat) Callable.From(() => { if (IsInstanceValid(listen) && listen.IsVisibleInTree()) listen.GrabFocus(); }).CallDeferred();
		} else {
			var revealed = new VBoxContainer();
			revealed.AddThemeConstantOverride("separation", 17);
			beats.AddChild(revealed);
			foreach (var read in scene.Reads) revealed.AddChild(GigRead(read));
			revealed.AddChild(GigLabel(scene.OffStage, PaperTheme.SerifItalic, 20, GigMuted));
			if (focusSetBeat) {
				newBeat = revealed.GetChild<Control>(0);
				revealed.Modulate = new Color(1, 1, 1, 0);
				revealed.CreateTween().TweenProperty(revealed, "modulate:a", 1f, .19f);
			}
			DrawSetChoices(desk, scene, artist, decisions, scroll);
		}
		focusSetBeat = false;
		int offset = setTapeOffset;
		Callable.From(() => {
			if (!IsInstanceValid(scroll) || scroll.IsQueuedForDeletion()) return;
			scroll.ScrollVertical = offset;
			if (IsInstanceValid(newBeat)) scroll.EnsureControlVisible(newBeat);
		}).CallDeferred();
		return true;
	}

	private Control BuildGigStage(PlayerDesk desk, SetScene scene, bool talking) {
		var stage = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.35f, ClipContents = true };
		var baseColor = new ColorRect { Color = new Color("30201c"), MouseFilter = MouseFilterEnum.Ignore };
		stage.AddChild(baseColor);
		baseColor.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		if (GigBackdrop.Value is { } backdrop) {
			var art = new TextureRect { Texture = backdrop, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore };
			stage.AddChild(art);
			art.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		}
		var scrim = new TextureRect {
			Texture = new GradientTexture2D {
				Gradient = new Gradient { Offsets = new[] { 0f, .53f, 1f },
					Colors = new[] { new Color("191115ed"), new Color("24181b10"), new Color("160f13e8") } },
				FillFrom = Vector2.Zero, FillTo = Vector2.Down
			},
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = MouseFilterEnum.Ignore
		};
		stage.AddChild(scrim);
		scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var foreground = new VBoxContainer();
		stage.AddChild(foreground);
		foreground.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		foreground.OffsetLeft = 34; foreground.OffsetRight = -34;
		foreground.OffsetTop = 34; foreground.OffsetBottom = -30;
		foreground.AddThemeConstantOverride("separation", 10);
		string freshness = desk.ShowsFatigue ? $"  ·  {desk.FatigueState.ToString().ToUpperInvariant()}" : "";
		string clockTime = PlayerDesk.ClockText(talking ? desk.NowMinute : scene.StartMinute);
		foreground.AddChild(GigLabel($"{LocalSceneRoomService.Date(scene.Day).ToHeadlineString()}  ·  {clockTime}  ·  {Money(desk.Label.cashReserves)} cash{freshness}",
			PaperTheme.TypedBold, 15, GigInk));
		var gap = new Control { CustomMinimumSize = new Vector2(0, 24) };
		foreground.AddChild(gap);
		foreground.AddChild(GigLabel(scene.RoomName, PaperTheme.SerifBold, 56, GigAmber));
		var context = new[] { scene.Crowd, scene.Admission is float cover ? CoverText(cover) : null,
			scene.EndMinute > scene.StartMinute ? StudioClockStrip.DurationText(scene.EndMinute - scene.StartMinute) + " heard" : null };
		foreground.AddChild(GigLabel(string.Join("   ·   ", context.Where(line => line != null)).ToUpperInvariant(), PaperTheme.TypedBold, 16, GigInk));
		foreground.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

		if (scene.Songs.Count == 0 && scene.SongsNote == null) return stage;
		var setlist = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin, CustomMinimumSize = new Vector2(440, 0) };
		setlist.AddThemeStyleboxOverride("panel", GigPanel(new Color("21171aee"), new Color("966c4d"), 1, 18));
		foreground.AddChild(setlist);
		var list = new VBoxContainer();
		list.AddThemeConstantOverride("separation", 7);
		setlist.AddChild(list);
		list.AddChild(GigLabel("SETLIST", PaperTheme.TypedBold, 15, GigAmber));
		foreach (var (song, original) in scene.Songs) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 16);
			var songName = GigLabel(song, PaperTheme.Serif, 19, GigInk);
			songName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			row.AddChild(songName);
			row.AddChild(GigLabel(original ? "ORIGINAL" : "COVER", PaperTheme.TypedBold, 12, original ? GigAmber : GigMuted));
			list.AddChild(row);
		}
		if (scene.SongsNote != null) list.AddChild(GigLabel(scene.SongsNote, PaperTheme.SerifItalic, 17, GigMuted));
		return stage;
	}

	private void DrawSetChoices(PlayerDesk desk, SetScene scene, SimulatedArtist artist, VBoxContainer decisions, ScrollContainer scroll) {
		string id = scene.ArtistId;
		var dossier = desk.ScoutingDossiers.FirstOrDefault(d => d.ArtistId == id);
		if (artist != null && dossier != null) {
			bool terms = string.IsNullOrEmpty(artist.labelId) && ScoutingActStanding(desk, artist).Terms;
			var talk = GigChoice(decisions, terms ? "SIT DOWN ON TERMS" : "TALK TO THEM", scene.TalkPrompt, true);
			talk.Pressed += () => {
				if (terms) {
					DismissTheSet();
					scoutingView = "ACT NOTEBOOK";
					FocusScoutingAct(id);
				}
				PressScoutingPrimary(desk, dossier, terms);
			};
		}
		foreach (var companion in new[] { AfterSetCompanion.Act, AfterSetCompanion.Booker }) {
			string companionName = desk.AfterSetDrinkCompanion(id, companion);
			if (companionName == null) continue;
			var drink = GigChoice(decisions, $"DRINK WITH {companionName.ToUpperInvariant()}", "30m · +1h fatigue");
			drink.TooltipText = "Stay for 30 minutes and get to know them a little better. The drink adds one hour of fatigue.";
			AddTonightPreview(drink, desk, desk.NowMinute + PlayerDesk.AfterSetDrinkMinutes, PlayerDesk.AfterSetDrinkDebt);
			drink.Pressed += () => Act(() => {
				setTapeOffset = scroll.ScrollVertical;
				bool ok = desk.ShareAfterSetDrink(id, companion, out string message);
				Say(message, ok);
				return ok;
			});
		}

		string next = theSetQueue.Count > 1 ? desk.ScoutingDossiers.FirstOrDefault(d => d.ArtistId == theSetQueue[1])?.ArtistName : null;
		var go = GigChoice(decisions, "LET THEM GO", next == null ? null : $"Next on the bill · {next}");
		go.Pressed += () => {
			theSetQueue.Remove(id);
			ResetSetBeat();
			if (theSetQueue.Count == 0) scoutingView = "FIND A SET";
			Refresh();
		};
	}

	private (Control Opening, Control LastReply) DrawGigTalk(PlayerDesk desk, TalkScene talk, VBoxContainer beats, VBoxContainer decisions) {
		var setting = GigLabel(talk.Setting, PaperTheme.SerifItalic, 20, GigMuted);
		beats.AddChild(setting);
		Control lastReply = setting;
		foreach (var line in talk.Lines) {
			if (line.beat == RolodexSceneBeat.PassiveRead && line.voice != ExecutiveVoice.None && line.voice == desk.FatigueQuietVoice) continue;
			var turn = GigRead(line);
			beats.AddChild(turn);
			lastReply = turn;
		}

		foreach (var read in FirstWeekReads(desk, FirstWeekPlace.TheTalk))
			if (read.voice != desk.FatigueQuietVoice) beats.AddChild(GigRead(read));

		beats.AddChild(GigLabel("WHAT DO YOU SAY", PaperTheme.TypedBold, 15, GigAmber));
		foreach (var option in talk.Options) {
			var choice = GigButton("▸  " + option.Line);
			choice.Disabled = !option.Enabled;
			choice.TooltipText = option.Enabled ? option.Cost ?? "" : option.Why ?? "";
			choice.AddThemeColorOverride("font_disabled_color", GigMuted);
			choice.AddThemeStyleboxOverride("disabled", GigPanel(GigGround, new Color("493039"), 1, 8));
			if (option.Voice != ExecutiveVoice.None && option.Enabled)
				choice.AddThemeColorOverride("font_color", GigVoiceColor(option.Voice));
			var answer = new VBoxContainer();
			answer.AddThemeConstantOverride("separation", 3);
			beats.AddChild(answer);
			answer.AddChild(choice);
			string detail = option.Enabled ? option.Cost : option.Why;
			if (!string.IsNullOrEmpty(detail))
				answer.AddChild(GigLabel(detail, PaperTheme.SerifItalic, 17, GigMuted));
			var verb = option.Verb;
			if (option.Enabled) choice.Pressed += () => SayTalkLine(desk, talk.ArtistId, verb);
		}
		var end = GigButton("END THE CONVERSATION");
		end.TooltipText = "Return to their card in the act notebook.";
		end.Pressed += () => EndTheTalk(desk, talk.ArtistId);
		decisions.AddChild(end);
		return (setting, lastReply);
	}

	private Button GigChoice(VBoxContainer into, string caption, string detail = null, bool primary = false) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		into.AddChild(row);
		var number = GigLabel($"{setChoices.Count + 1}.", PaperTheme.TypedBold, 16, GigAmber);
		number.AutowrapMode = TextServer.AutowrapMode.Off;
		number.CustomMinimumSize = new Vector2(28, 0);
		row.AddChild(number);
		var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 2);
		row.AddChild(text);
		if (detail != null) text.AddChild(GigLabel(detail, PaperTheme.SerifItalic, 16, GigMuted));
		var button = GigButton(caption, primary);
		text.AddChild(button);
		setChoices.Add(button);
		return button;
	}

	private static Color GigVoiceColor(ExecutiveVoice voice) => voice switch {
		ExecutiveVoice.Ear => new Color("e8a0b8"), ExecutiveVoice.Street => new Color("9bcea6"),
		ExecutiveVoice.Suit => new Color("e5bea0"), ExecutiveVoice.Fixer => new Color("a8bcd9"), _ => GigAmber
	};

	private static Control GigRead(CallLine read) {
		Color accent = GigVoiceColor(read.voice);
		var card = new PanelContainer();
		var style = GigPanel(new Color("2b222a"), accent, 0, 14);
		style.BorderWidthLeft = 3;
		card.AddThemeStyleboxOverride("panel", style);
		var text = new VBoxContainer();
		text.AddThemeConstantOverride("separation", 6);
		card.AddChild(text);
		string speaker = read.voice != ExecutiveVoice.None ? VoiceName(read.voice) : read.speaker;
		if (!string.IsNullOrEmpty(speaker)) text.AddChild(GigLabel(speaker.ToUpperInvariant(), PaperTheme.TypedBold, 15, accent));
		var line = GigLabel(read.text, PaperTheme.Serif, 21, GigInk);
		if (read.voice != ExecutiveVoice.None) line.SetMeta("set_voice", (int)read.voice);
		text.AddChild(line);
		return card;
	}

	public override void _UnhandledKeyInput(InputEvent input) {
		if (liveSet?.IsVisibleInTree() != true || !setRevealed || PaperModal.IsOpen ||
			input is not InputEventKey { Pressed: true, Echo: false, AltPressed: false, CtrlPressed: false, MetaPressed: false } key) return;
		int index = key.Keycode switch { Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, Key.Key4 => 3, _ => -1 };
		if (index < 0 || index >= setChoices.Count) return;
		GetViewport().SetInputAsHandled();
		setChoices[index].EmitSignal(Button.SignalName.Pressed);
	}

	private static ColorRect GigRule() => new() { Color = new Color("65434b"), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore };

	private static Label TheSetNarration(string text) =>
		GigLabel(text, PaperTheme.SerifItalic, 17, new Color("4a3d28"));

	private static Label GigLabel(string text, Font font, int size, Color ink) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", ink);
		return label;
	}

	private static StyleBoxFlat GigPanel(Color fill, Color edge, int border, int inset = 0) => new() {
		BgColor = fill, BorderColor = edge, BorderWidthLeft = border, BorderWidthTop = border,
		BorderWidthRight = border, BorderWidthBottom = border,
		ContentMarginLeft = inset, ContentMarginRight = inset, ContentMarginTop = inset, ContentMarginBottom = inset
	};

	private static Button GigButton(string caption, bool primary = false) {
		var button = new Button { Text = caption, Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(110, 35),
			AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseDefaultCursorShape = CursorShape.PointingHand };
		button.AddThemeFontOverride("font", PaperTheme.TypedBold);
		button.AddThemeFontSizeOverride("font_size", 15);
		button.AddThemeColorOverride("font_color", primary ? GigAmber : GigInk);
		foreach (string state in new[] { "hover", "pressed", "focus" }) button.AddThemeColorOverride("font_" + state + "_color", GigInk);
		button.AddThemeStyleboxOverride("normal", GigPanel(primary ? new Color("39272b") : GigTape, new Color("765346"), 1, 8));
		button.AddThemeStyleboxOverride("hover", GigPanel(new Color("49323a"), GigAmber, 1, 8));
		button.AddThemeStyleboxOverride("pressed", GigPanel(new Color("523e3d"), GigAmber, 1, 8));
		button.AddThemeStyleboxOverride("focus", GigPanel(new Color(0, 0, 0, 0), GigAmber, 1, 8));
		return button;
	}
}
