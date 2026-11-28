using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
	private RolodexRole rolodexRole = RolodexRole.Deejay;
	private readonly Dictionary<RolodexRole, int> rolodexFocusByRole = new();

	private static readonly Dictionary<RolodexRole, Color> RoleInk = new() {
		{ RolodexRole.Deejay, new Color("1b2a52") }, { RolodexRole.Manager, new Color("8f2a22") }, { RolodexRole.Publisher, new Color("5a2d4a") },
		{ RolodexRole.Plant, new Color("4f5c2c") }, { RolodexRole.Distributor, new Color("2f5560") }, { RolodexRole.Dealer, new Color("8a5a1c") },
		{ RolodexRole.Booker, new Color("42624a") }, { RolodexRole.Engineer, new Color("3d3d5c") }, { RolodexRole.Press, new Color("6b3a1c") },
	};

	/// <summary>Surname first, as a real book files them.</summary>
	private static string FilingKey(string name) {
		string[] parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return parts.Length == 0 ? "" : parts[^1] + ", " + string.Join(' ', parts.Take(parts.Length - 1));
	}

	private static readonly RolodexRole[][] RolodexGroups = {
		new[] { RolodexRole.Deejay, RolodexRole.Press },
		new[] { RolodexRole.Publisher, RolodexRole.Manager, RolodexRole.Booker },
		new[] { RolodexRole.Plant, RolodexRole.Engineer, RolodexRole.Distributor, RolodexRole.Dealer }
	};

	private bool inRolodex, showingRolodexCall;
	private PanelContainer rolodexOverlay;
	private VBoxContainer rolodexIndex, rolodexCards, phoneDock;
	private ScrollContainer rolodexScroll;
	private Label rolodexClock;
	private Panel djAvailabilityDot;

	internal bool RolodexOpen => Visible && inRolodex;

	internal void OpenRolodex() {
		if (PlayerDesk.Instance is not { HasLabel: true, IsGameOver: false, LienMorningPending: false }) return;
		if (streetPage == StreetPage.StationCall) PlayerDesk.Instance.EndCall(PlayerDesk.Instance.ActiveCall);
		streetPage = null;
		browsingSaves = false;
		UseStreetLayout(false);
		folder.Hide();
		inRolodex = true;
		deskPanelShade.Color = new Color(0, 0, 0, .23f);
		rolodexOverlay.Show();
		rolodexScroll.ScrollVertical = 0;
		if (UIManager.Instance?.IsOfficePanel(this) == true) UIManager.Instance.FocusOfficePhone(true);
		FirstSessionTrace.TabOpened("ROLODEX");
		Open();
	}

	private void LeaveRolodex() {
		if (!inRolodex) return;
		inRolodex = false;
		deskPanelShade.Color = new Color(0, 0, 0, .42f);
		rolodexOverlay.Hide();
		Clear(rolodexCards);
		Clear(rolodexIndex);
		Clear(phoneDock);
		rolodexPitchRecordId = null;
		PlayerDesk.Instance?.EndCall(PlayerDesk.Instance.ActiveCall);
		if (UIManager.Instance?.IsOfficePanel(this) == true) UIManager.Instance.FocusOfficePhone(false);
	}

	private void BuildRolodexOverlay() {
		rolodexOverlay = new PanelContainer { Name = "RolodexOverlay", Visible = false };
		rolodexOverlay.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		AddChild(rolodexOverlay);
		rolodexOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterLeft);
		rolodexOverlay.OffsetLeft = 56;
		rolodexOverlay.OffsetRight = 1320;
		rolodexOverlay.OffsetTop = -390;
		rolodexOverlay.OffsetBottom = 436;

		var stack = new VBoxContainer();
		stack.AddThemeConstantOverride("separation", 14);
		rolodexOverlay.AddChild(stack);
		var header = new PanelContainer();
		header.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("e9d7af"), 16, 10, 8));
		stack.AddChild(header);
		var commands = new HBoxContainer();
		commands.AddThemeConstantOverride("separation", 14);
		header.AddChild(commands);
		var heading = new Label { Text = "ROLODEX", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		heading.AddThemeFontOverride("font", PaperTheme.Elite);
		heading.AddThemeFontSizeOverride("font_size", 30);
		heading.AddThemeColorOverride("font_color", Ink);
		commands.AddChild(heading);
		rolodexClock = new Label();
		rolodexClock.AddThemeFontOverride("font", PaperTheme.TypedBold);
		rolodexClock.AddThemeColorOverride("font_color", Ink);
		commands.AddChild(rolodexClock);
		var close = Btn("CLOSE  ×");
		close.Pressed += ClosePanel;
		commands.AddChild(close);

		var rolodexBrowser = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		rolodexBrowser.AddThemeConstantOverride("separation", 0);
		stack.AddChild(rolodexBrowser);
		rolodexIndex = new VBoxContainer { Name = "RolodexIndex", CustomMinimumSize = new Vector2(194, 0), SizeFlagsVertical = SizeFlags.ShrinkBegin };
		rolodexIndex.AddThemeConstantOverride("separation", 4);
		rolodexBrowser.AddChild(rolodexIndex);

		var rolodexTray = new PanelContainer { Name = "RolodexTray", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		rolodexBrowser.AddChild(rolodexTray);
		rolodexTray.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("dfc99a"), 18, 18, 6, new Color("60472d")));
		(rolodexScroll, rolodexCards) = RolodexColumn(rolodexTray);

		rolodexBrowser.AddChild(new Control { CustomMinimumSize = new Vector2(22, 0), MouseFilter = MouseFilterEnum.Ignore });
		var dock = new PanelContainer { Name = "PhoneDock", CustomMinimumSize = new Vector2(314, 0) };
		dock.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("f1e5c8"), 14, 16, 6, new Color("806239")));
		rolodexBrowser.AddChild(dock);
		(_, phoneDock) = RolodexColumn(dock);
	}

	private static (ScrollContainer, VBoxContainer) RolodexColumn(PanelContainer parent) {
		var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		parent.AddChild(scroll);
		var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		rows.AddThemeConstantOverride("separation", 10);
		scroll.AddChild(rows);
		return (scroll, rows);
	}

	private void RefreshRolodex(PlayerDesk desk, TimeManager time) {
		contentRoot = content = rolodexCards;
		contentScroll = rolodexScroll;
		Clear(rolodexCards);
		Clear(rolodexIndex);
		Clear(phoneDock);
		djAvailabilityDot = null;

		var call = desk.ActiveCall;
		bool onCall = call != null && call.stage != CallStage.Ended;
		if (showingRolodexCall != onCall) rolodexScroll.ScrollVertical = 0;
		showingRolodexCall = onCall;
		rolodexIndex.Visible = !onCall;
		if (onCall) PageCall(desk, call);
		else {
			var people = RolodexGroups.SelectMany(group => group).Where(role => role != RolodexRole.Deejay)
				.ToDictionary(role => role, role => RolodexDirectory.Cards(desk, role));
			var djs = desk.Rolodex.OrderBy(entry => FilingKey(entry.displayName), StringComparer.OrdinalIgnoreCase).ToList();
			DrawRolodexIndex(people, djs.Count);
			if (rolodexRole == RolodexRole.Deejay) DrawRolodexCards(desk, djs, null);
			else DrawRolodexCards(desk, null, people[rolodexRole]);
		}
		UpdateRolodexClock(time);

		content = phoneDock;
		try {
			if (desk.Calling != PlayerDesk.CallerAt.None) {
				var answer = Primary(desk.Calling == PlayerDesk.CallerAt.Phone ? "PICK UP THE PHONE" : "ANSWER THE DOOR");
				answer.Pressed += AnswerCaller;
				content.AddChild(answer);
			}
			RenderWorkThePhones(desk, onCall);
			content.AddChild(new HSeparator());
			PhoneSection(desk, inPhoneDock: true);
		} finally { content = rolodexCards; }
	}

	internal void UpdateRolodexClock(TimeManager time) {
		if (!RolodexOpen || time == null) return;
		rolodexClock.Text = $"{time.CurrentDate.ToHeadlineString()}  ·  {time.GetTimeString()}";
		if (djAvailabilityDot == null) return;
		bool available = PlayerDesk.Instance.Rolodex.Any(entry => ChartManager.Instance?.GetDeejay(entry.djId) is { } dj
			&& RolodexShifts.ReachableAt(RolodexShifts.ShiftOf(dj), time.CurrentHour));
		djAvailabilityDot.Visible = available;
		djAvailabilityDot.GetParent<Button>().TooltipText = available ? "A deejay is in a call window." : "";
	}

	private void DrawRolodexCards(PlayerDesk desk, List<RolodexEntry> djs, List<DirectoryCard> people) {
		int count = djs?.Count ?? people.Count;
		RolodexRole role = rolodexRole;
		if (count == 0) {
			content.AddChild(RolodexDeck.Card(new Label {
				Text = RolodexDirectory.EmptyNote(role), AutowrapMode = TextServer.AutowrapMode.WordSmart,
				CustomMinimumSize = new Vector2(0, 108)
			}));
			return;
		}

		string NameOf(int i) => djs != null ? djs[i].displayName : people[i].Name;
		int focus = Mathf.Clamp(rolodexFocusByRole.GetValueOrDefault(role), 0, count - 1);
		var nav = new HBoxContainer();
		nav.AddThemeConstantOverride("separation", 10);
		var counter = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
		counter.AddThemeColorOverride("font_color", Heard);
		void ShowCounter(int at) => counter.Text = count > 1
			? $"Card {at + 1} of {count}  ·  Wheel or ← → to flip"
			: "1 card";
		ShowCounter(focus);
		var deck = new RolodexDeck().Set(count, focus,
			i => BuildRolodexCard(desk, djs?[i], people?[i]), NameOf,
			i => { rolodexFocusByRole[role] = i; ShowCounter(i); });
		if (count > 1) {
			var prev = Btn("‹"); prev.CustomMinimumSize = new Vector2(44, 34); prev.TooltipText = "Flip back";
			prev.Pressed += () => deck.Flip(-1);
			nav.AddChild(prev);
		}
		nav.AddChild(counter);
		if (count > 1) {
			var next = Btn("›"); next.CustomMinimumSize = new Vector2(44, 34); next.TooltipText = "Flip forward";
			next.Pressed += () => deck.Flip(1);
			nav.AddChild(next);
		}
		content.AddChild(nav);
		content.AddChild(deck);
	}

	private void DrawRolodexIndex(Dictionary<RolodexRole, List<DirectoryCard>> people, int djCount) {
		foreach (var group in RolodexGroups) {
			if (rolodexIndex.GetChildCount() > 0) rolodexIndex.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
			foreach (RolodexRole role in group) {
				int count = role == RolodexRole.Deejay ? djCount : people[role].Count;
				bool open = rolodexRole == role;
				Color ink = RoleInk[role];
				var tab = Btn($"{RolodexDirectory.TabName(role)}   {count}");
				tab.Alignment = HorizontalAlignment.Left;
				tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
				tab.CustomMinimumSize = new Vector2(0, 44);
				tab.AddThemeFontOverride("font", PaperTheme.SansBold);
				tab.AddThemeFontSizeOverride("font_size", 15);
				StyleBoxFlat Face(Color fill) => new() {
					BgColor = fill, BorderColor = open ? new Color("f9edce") : ink.Darkened(.35f),
					BorderWidthLeft = 2, BorderWidthTop = 1, BorderWidthBottom = 3,
					CornerRadiusTopLeft = 7, CornerRadiusBottomLeft = 7,
					ContentMarginLeft = 14, ContentMarginRight = 26
				};
				var face = ink.Lightened(open ? .23f : .10f);
				if (count == 0) face = face.Lerp(new Color("59534a"), .65f);
				tab.AddThemeStyleboxOverride("normal", Face(face));
				tab.AddThemeStyleboxOverride("pressed", Face(face.Lightened(.18f)));
				tab.AddThemeStyleboxOverride("hover", Face(face.Lightened(.13f)));
				foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
					tab.AddThemeColorOverride(state, count == 0 ? new Color("d5c7ac") : new Color("fff2d5"));
				if (role == RolodexRole.Deejay) {
					var dot = djAvailabilityDot = new Panel { Name = "AvailableNow", MouseFilter = MouseFilterEnum.Ignore };
					dot.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
						BgColor = new Color("a8d888"), BorderColor = new Color("24422d"),
						BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
						CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
					});
					tab.AddChild(dot);
					dot.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterRight);
					dot.OffsetLeft = -20; dot.OffsetRight = -9;
					dot.OffsetTop = -5; dot.OffsetBottom = 6;
				}
				tab.Pressed += () => {
					if (rolodexRole == role) return;
					rolodexRole = role;
					rolodexScroll.ScrollVertical = 0;
					Refresh();
				};
				rolodexIndex.AddChild(tab);
			}
		}
	}

	// ---- the cards ----------------------------------------------------------------------------------------

	/// <summary>One card, built fresh each time the deck asks (the deck builds the next card before it flips to it).</summary>
	private Control BuildRolodexCard(PlayerDesk desk, RolodexEntry dj, DirectoryCard person) {
		var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 4);
		VBoxContainer saved = content;
		// The page helpers (Body, Heading, RenderCarrying...) write into `content`; point it at this card while it is built.
		content = body;
		try {
			if (dj != null) RenderDjCard(desk, dj, body);
			else RenderPersonCard(person, body);
		} finally { content = saved; }
		return RolodexDeck.Card(body);
	}

	private HBoxContainer CardHeader(PortraitFigure figure, string key, string name, string line1, string line2, Color accent, string line2Tip = null) {
		var head = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 108) };
		head.AddThemeConstantOverride("separation", 16);
		head.AddChild(new PortraitPhoto().Set(Portraits.ForFigures("card:" + key, new List<PortraitFigure> { figure }), new Vector2(86, 108), key));
		var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		column.AddThemeConstantOverride("separation", 2);
		var title = new Label { Text = name, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new Vector2(0, 46), VerticalAlignment = VerticalAlignment.Center };
		title.AddThemeFontOverride("font", PaperTheme.Elite);
		title.AddThemeFontSizeOverride("font_size", 28);
		title.AddThemeColorOverride("font_color", Ink);
		column.AddChild(title);
		var a = new Label { Text = line1, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		a.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		a.AddThemeFontSizeOverride("font_size", 15);
		a.AddThemeColorOverride("font_color", Ink);
		column.AddChild(a);
		var b = new Label { Text = line2, TooltipText = line2Tip ?? "", AutowrapMode = TextServer.AutowrapMode.WordSmart };
		b.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		b.AddThemeFontSizeOverride("font_size", 14);
		b.AddThemeColorOverride("font_color", accent);
		column.AddChild(b);
		head.AddChild(column);
		return head;
	}

	private void RenderPersonCard(DirectoryCard person, VBoxContainer body) {
		string line1 = string.Join("  ·  ", new[] { person.Title, person.Firm }.Where(s => !string.IsNullOrEmpty(s)));
		body.AddChild(CardHeader(person.Figure, person.Key, person.Name, line1, string.IsNullOrEmpty(person.Place) ? "" : person.Place, RoleInk[person.Role]));
		foreach ((string text, bool warn) in person.Facts) {
			var line = new Label { Text = "•  " + text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			line.AddThemeFontSizeOverride("font_size", 16);
			line.AddThemeColorOverride("font_color", warn ? Rust : Ink);
			body.AddChild(line);
		}
		if (!string.IsNullOrEmpty(person.GoToTab)) {
			string tab = person.GoToTab;
			var go = Btn(person.GoToLabel ?? "OPEN");
			go.CustomMinimumSize = new Vector2(280, 38);
			go.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			go.Pressed += () => {
				OpenAtTab(tab);
				if (person.Role == RolodexRole.Booker) {
					var room = SceneRoomCatalog.All.FirstOrDefault(r => r.ContactId == person.Key);
					if (room != null) OpenScoutingVenue(PlayerDesk.Instance, room.Id);
				}
			};
			body.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
			body.AddChild(go);
		}
	}

	/// <summary>A reporter's card: portrait, identity, tier, what you know about his hours, and what he is currently
	/// carrying for you. The only card with a phone behind it.</summary>
	private void RenderDjCard(PlayerDesk desk, RolodexEntry entry, VBoxContainer body) {
		Deejay dj = ChartManager.Instance?.GetDeejay(entry.djId);
		RadioStation station = ChartManager.Instance?.GetRadioStation(entry.stationId);
		float rapport = station?.rt?.Rapport(desk.Label?.labelId ?? "") ?? 0f;
		RapportTier tier = RolodexEntry.EffectiveTier(entry, rapport);
		string discoveryLabel = entry.state switch {
			DiscoveryState.HeardOf => "Heard of you",
			DiscoveryState.Introduced => "Introduced",
			DiscoveryState.Known => "Known",
			DiscoveryState.Trusted => "Trusted",
			_ => "New contact"
		};
		string where = station == null ? "Disc jockey" : $"Disc jockey  ·  {station.callsign}  ·  {station.format}";
		string place = station?.cityName ?? "";
		PortraitFigure figure = Portraits.Announcer(entry.djId ?? entry.displayName, dj?.archetype ?? DJArchetype.CompanyMan);
		HBoxContainer head = CardHeader(figure, "dj:" + (entry.djId ?? entry.displayName), entry.displayName, where,
			$"{place}{(place.Length > 0 ? "  ·  " : "")}{RolodexEntry.TierLabel(tier)}  ·  {discoveryLabel}", RolodexEntry.TierColor(tier).Darkened(0.25f));
		body.AddChild(head);

		// The verb first: what you can do with this man is on the visible part of the card, the history below it.
		var openBtn = Primary($"PLACE A CALL  ({PlayerDesk.DialMinutes} min)");
		openBtn.CustomMinimumSize = new Vector2(230, 40);
		openBtn.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		openBtn.Pressed += () => {
			string rid = rolodexPitchRecordId != null && desk.RecordsOnTheTable().Any(item => item.RecordId == rolodexPitchRecordId)
				? rolodexPitchRecordId : desk.RecordsOnTheTable().Select(item => item.RecordId).FirstOrDefault();
			rolodexPitchRecordId = rid;
			desk.PlaceCall(entry, rid, out string msg);
			if (!string.IsNullOrEmpty(msg)) Say(msg);
			Refresh();
		};
		var callRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		callRow.AddThemeConstantOverride("separation", 16);
		callRow.AddChild(openBtn);
		var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		notes.AddThemeConstantOverride("separation", 2);
		callRow.AddChild(notes);
		body.AddChild(callRow);
		VBoxContainer cardContent = content;
		content = notes;
		if (dj != null) Body(RolodexEntry.ArchetypeBlurb(dj.archetype));
		// When to call him. Learned, not given -- an unreached name comes with no hours.
		if (dj != null) {
			Daypart shift = RolodexShifts.ShiftOf(dj);
			if (entry.shiftKnown) {
				int hour = TimeManager.Instance?.CurrentHour ?? 12;
				bool nowGood = RolodexShifts.ReachableAt(shift, hour);
				var hours = new Label { Text = RolodexShifts.WindowAdvice(shift) + (nowGood ? "  ·  Should be in right now." : "  ·  Wrong time of day."), AutowrapMode = TextServer.AutowrapMode.WordSmart };
				hours.AddThemeColorOverride("font_color", nowGood ? new Color("4a7a4a") : Rust);
				content.AddChild(hours);
			} else {
				Body("You don't know the hours yet. Call and find out the hard way.");
			}
		}
		content = cardContent;

		if (dj != null && entry.callbackDate != null) {
			bool due = desk.CallbackDue(entry);
			var callback = new Label { Text = due ? "Your call-back is due now." : $"Call-back set for {desk.CallbackWhen(entry)}." };
			callback.AddThemeColorOverride("font_color", due ? new Color("4a7a4a") : Heard);
			content.AddChild(callback);
			var cancel = Btn("CANCEL THE CALL-BACK");
			cancel.Pressed += () => { desk.CancelCallback(entry); Refresh(); };
			content.AddChild(cancel);
		}

		if (entry.theyOweThem) Body("Owes you one.");
		if (entry.youOweThem) Body("You owe a favor.");
		if (entry.payolaBurned) Body("The cash channel is closed.");
		if (entry.professionallyBurned) Body("No longer takes your word.");
		if (desk.WintersCardLine(entry) is { } clyde) Body(clyde + (desk.WintersBurnedCard(entry) ? " No longer takes your word." : " Expects a favor in return."));

		// What he is actually carrying for you right now -- the record-level commitment, not the mood.
		RenderCarrying(desk, entry);

		if (entry.log.Count > 0) {
			Heading("HISTORY");
			foreach (string line in entry.log.TakeLast(6)) Body(line);
		}
	}
}
