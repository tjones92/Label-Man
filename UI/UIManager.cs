using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class UIManager : Control
{
	public static UIManager Instance;

	[ExportGroup("Views")]
	[Export] private Control ledgerPanel;
	[Export] private Control dialoguePanel;
	[Export] private Control chartPanel;
	[Export] private ArtistDetailPanel artistDetailPanel;
	[Export] private LabelDetailPanel labelDetailPanel;
	private Button calendarButton;
	private TextureRect officeBackdrop;
	private Tween officeLightTween;
	private PopupPanel calendarPopup;
	private SpinBox skipDaysInput;
	private PlayerDeskPanel deskPanel;
	private Button deskButton;
	private Control paintedLayer;   // text that lives ON the painted desk; tinted with it by the office light
	private PaintedRecordLabels paintedRecords;   // the player's label on the wall record and the top of the stack
	private PaintedGoldRecord paintedGold;        // the gold record on the wall, once a record passes the bar
	private PanelContainer mainHud;
	private RecordJacketWidget recordJacket;
	private Label hudDate, hudNextUp, hudTicker;
	private readonly Label[] calendarCards = new Label[4];   // month, weekday, day, year
	private PanelContainer propTag;
	private Label propTagText;
	private Tween propTagTween;
	private int propTagTicket;
	private Control morningPaper;
	private Label paperDate, paperHeading, paperEdition, paperPrice, paperMasthead, paperExtraBand;
	private Control paperSheet;
	private VBoxContainer paperBody;
	private Font paperSerif;
	private ScrollContainer paperScroll;
	private bool paperQueued, uiOpenBeforePaper;
	private PanelContainer announcement;
	private Label announcementText;
	private int announcementTicket;
	private readonly List<string> queuedMorningDigests = new();

	[ExportGroup("State")]
	public bool isUIOpen = false;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		PaperTheme.Apply(GetWindow());
		if (artistDetailPanel != null) artistDetailPanel.LabelRequested += id => OpenLabel(id);
		if (labelDetailPanel != null) labelDetailPanel.ArtistRequested += id => OpenArtist(id);
		officeBackdrop = GetNodeOrNull<TextureRect>("TextureRect");
		calendarButton = GetNodeOrNull<Button>("CalendarBtn");
		BuildPaintedLayer();
		if (calendarButton != null) {
			// A transparent hotspot over the painted flip calendar. The date is lettered on the painted cards
			// themselves (BuildCalendarCards), so nothing here is stretched to fit the prop.
			calendarButton.Flat = true;
			calendarButton.Text = "";
			calendarButton.FocusMode = Control.FocusModeEnum.None;
			calendarButton.GuiInput += OnCalendarGuiInput;
			calendarButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
			StyleHotspot(calendarButton, 8);
			BuildCalendarCards();
			calendarButton.MouseEntered += () => ShowPropTag("THE CALENDAR  -  click: next day  -  right-click: skip ahead", new Vector2(1545, 470));
			calendarButton.MouseExited += HidePropTag;
			UpdateCalendarButton(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
		}
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnDayStarted += OnMorningStarted;
			TimeManager.Instance.OnClockRestored += OnClockRestored;
			TimeManager.Instance.OnDayStarted += UpdateCalendarButton;
			TimeManager.Instance.OnClockRestored += UpdateCalendarButton;
		}

		deskPanel = GetNodeOrNull<PlayerDeskPanel>("PlayerDeskPanel");
		deskButton = GetNodeOrNull<Button>("DeskBtn");
		if (deskButton != null) {
			deskButton.Text = "OPEN THE OFFICE";
			deskButton.TooltipText = "Open your label's departments and daily work.";
			deskButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
			deskButton.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
				BgColor = new Color("342619"), BorderColor = new Color("e4cf9b"),
				BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
				CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
			});
			deskButton.AddThemeStyleboxOverride("hover", new StyleBoxFlat {
				BgColor = new Color("6b3a1c"), BorderColor = new Color("f1e5c8"),
				BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
				CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
			});
			deskButton.AddThemeColorOverride("font_color", new Color("f1e5c8"));
			deskButton.AddThemeColorOverride("font_hover_color", Colors.White);
			deskButton.AddThemeFontSizeOverride("font_size", 18);
			deskButton.Pressed += OnClick_Desk;
		}
		billboardButton = GetNodeOrNull<TextureButton>("BillboardButton");
		if (billboardButton != null) {
			billboardButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
			// A TextureButton with no texture draws nothing, so the rim-light is a panel it shows on hover.
			var rim = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false, ZIndex = 1 };
			rim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			rim.AddThemeStyleboxOverride("panel", HotspotRim());
			billboardButton.AddChild(rim);
			billboardButton.MouseEntered += () => { rim.Visible = true; ShowPropTag("THE TRADES  -  this week's Hot 100", new Vector2(270, 484)); };
			billboardButton.MouseExited += () => { rim.Visible = false; HidePropTag(); };
		}
		BuildDeskProps();
		BuildMainHud();
		BuildMorningPaper();
		if (PlayerDesk.Instance != null) {
			PlayerDesk.Instance.Changed += UpdateMainHud;
			PlayerDesk.Instance.Announcement += ShowAnnouncement;
		}
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnHourChanged += OnHourChangedForHud;
			TimeManager.Instance.OnDayStarted += OnDayStartedForLight;
			TimeManager.Instance.OnClockRestored += OnDayStartedForLight;
		}
		ApplyOfficeLight(animate: false);
		UpdateMainHud();
		// A new player should land on the founding card instead of having to guess that the office
		// tab opens the game. Loaded games and an existing label stay on the desk scene as before.
		if (PlayerDesk.Instance?.HasLabel != true) OnClick_Desk();
	}

	// ── Painted-desk text ───────────────────────────────────────────────────────────────────────
	// Everything lettered on the painting (the calendar's cards) sits in one layer
	// beneath the panels, so it takes the office light with the painting instead of glowing against it at night.
	private static readonly Color GreaseRed = new("b3361f");
	private static readonly Color StampRed = new("a8322a");

	private void BuildPaintedLayer() {
		paintedLayer = new Control { Name = "PaintedLayer", MouseFilter = Control.MouseFilterEnum.Ignore };
		paintedLayer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(paintedLayer);
		// Just above the bare painting, below the hotspots and every panel.
		MoveChild(paintedLayer, officeBackdrop != null ? officeBackdrop.GetIndex() + 1 : 0);
		paintedRecords = new PaintedRecordLabels();
		paintedLayer.AddChild(paintedRecords);
		paintedGold = new PaintedGoldRecord();
		paintedLayer.AddChild(paintedGold);
	}

	// Centre, tilt (rad) and size of each painted flip card in the scene's 1920x1080 space.
	private static readonly (Vector2 Centre, float Tilt, Vector2 Size)[] CalendarCardRects = {
		(new Vector2(1380, 543), 0.10f, new Vector2(128, 100)),    // month
		(new Vector2(1526, 548), 0.13f, new Vector2(136, 112)),    // day
		(new Vector2(1683, 580), 0.105f, new Vector2(148, 122)),   // year
	};

	/// <summary>The date lettered on the three painted flip cards: month, day (with the weekday above it), year.
	/// Each card is placed, tilted and sized to the painting's own card, so the type sits flat on the paper.</summary>
	private void BuildCalendarCards() {
		// calendarCards: 0 month, 1 weekday, 2 day, 3 year
		int[] slot = { 0, 2, 3 };
		for (int i = 0; i < 3; i++) {
			var (centre, tilt, size) = CalendarCardRects[i];
			var label = new Label {
				MouseFilter = Control.MouseFilterEnum.Ignore, Size = size,
				HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
				Position = centre - size / 2, PivotOffset = size / 2, Rotation = tilt
			};
			label.AddThemeFontOverride("font", PaperTheme.SansBold);
			paintedLayer.AddChild(label);
			calendarCards[slot[i]] = label;
		}
		// The weekday rides above the day number on the middle card.
		var weekday = new Label {
			MouseFilter = Control.MouseFilterEnum.Ignore, Size = new Vector2(136, 26),
			HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
		};
		weekday.PivotOffset = weekday.Size / 2;
		weekday.Position = CalendarCardRects[1].Centre + new Vector2(0, 38) - weekday.Size / 2;
		weekday.Rotation = CalendarCardRects[1].Tilt;
		weekday.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		weekday.AddThemeFontSizeOverride("font_size", 20);
		paintedLayer.AddChild(weekday);
		calendarCards[1] = weekday;
	}

	/// <summary>A transparent hotspot: nothing drawn at rest, an amber rim-light and an 8% brighten on hover.</summary>
	private static StyleBoxFlat HotspotRim(int radius = 10) => new() {
		BgColor = new Color(1f, 0.89f, 0.68f, 0.08f), BorderColor = new Color("f2b35a"),
		BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
		CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius
	};

	private static void StyleHotspot(Button button, int radius = 10) {
		StyleBoxFlat rim = HotspotRim(radius);
		button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("hover", rim);
		button.AddThemeStyleboxOverride("pressed", rim);
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
	}

	/// <summary>The typed paper tag that names a prop while the pointer is on it. One tag, moved to each prop.</summary>
	private void BuildPropTag() {
		propTag = new PanelContainer { Name = "PropTag", Visible = false, ZIndex = 12, MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
		propTag.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("f6ecd0"), BorderColor = PaperTheme.Rust,
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2, CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
			ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 6,
			ShadowColor = new Color(0, 0, 0, 0.4f), ShadowSize = 6, ShadowOffset = new Vector2(1, 3)
		});
		propTagText = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Center };
		propTagText.AddThemeFontOverride("font", PaperTheme.TypedBold);
		propTagText.AddThemeFontSizeOverride("font_size", 15);
		propTagText.AddThemeColorOverride("font_color", PaperTheme.Ink);
		propTag.AddChild(propTagText);
		AddChild(propTag);
	}

	private void ShowPropTag(string text, Vector2 above) {
		if (propTag == null || isUIOpen) return;
		propTagText.Text = text;
		propTag.Visible = true;
		propTag.Size = propTag.GetCombinedMinimumSize();
		Vector2 view = GetViewportRect().Size;
		propTag.Position = new Vector2(
			Mathf.Clamp(above.X - propTag.Size.X / 2f, 12f, Mathf.Max(12f, view.X - propTag.Size.X - 12f)),
			Mathf.Clamp(above.Y - propTag.Size.Y - 6f, 8f, Mathf.Max(8f, view.Y - propTag.Size.Y - 8f)));
		propTagTicket++;
		propTagTween?.Kill();
		propTagTween = CreateTween();
		propTagTween.TweenProperty(propTag, "modulate:a", 1.0f, 0.12);
	}

	private void HidePropTag() {
		if (propTag == null || !propTag.Visible) return;
		int ticket = ++propTagTicket;
		propTagTween?.Kill();
		propTagTween = CreateTween();
		propTagTween.TweenProperty(propTag, "modulate:a", 0.0f, 0.1);
		propTagTween.TweenCallback(Callable.From(() => { if (ticket == propTagTicket) propTag.Visible = false; }));
	}

	private void BuildMainHud() {
		BuildPropTag();
		// The day's state sits in a paper widget under OPEN THE OFFICE: date, clock and cash, the next step, and
		// the latest office news. (Pencilled on the ledger pad it read as part of the painting, not as a prompt.)
		mainHud = new PanelContainer { Name = "MainHud", MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 10 };
		mainHud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
		// The desk shortcut occupies the upper corner. Keep the news underneath its hit area.
		mainHud.Position = new Vector2(18, 110);
		mainHud.CustomMinimumSize = new Vector2(510, 0);
		mainHud.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.96f, 0.91f, 0.78f, 0.91f), ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 8, ContentMarginBottom = 8 });
		var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		Label Line(Font font, int size, Color color, int maxLines = 0) {
			var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, ClipText = true, CustomMinimumSize = new Vector2(480, 0) };
			if (font != null) label.AddThemeFontOverride("font", font);
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", color);
			if (maxLines > 0) {
				label.ClipText = false; label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				label.MaxLinesVisible = maxLines; label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			}
			return label;
		}
		hudDate = Line(null, 17, new Color("2b2115"));
		hudNextUp = Line(PaperTheme.TypedBold, 15, GreaseRed, 2);
		hudTicker = Line(null, 15, new Color("6b3a1c"));
		foreach (Label label in new[] { hudDate, hudNextUp, hudTicker }) stack.AddChild(label);
		mainHud.AddChild(stack);
		AddChild(mainHud);

		// The record jacket mirrors the OPEN THE OFFICE button from the opposite corner.
		recordJacket = new RecordJacketWidget { ZIndex = 10 };
		recordJacket.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		recordJacket.OffsetLeft = -(40 + RecordJacketWidget.JacketWidth);
		recordJacket.OffsetRight = -40;
		recordJacket.OffsetTop = 40;
		recordJacket.GrowHorizontal = Control.GrowDirection.Begin;
		recordJacket.Clicked += () => OpenOfficeAt("CATALOG");
		AddChild(recordJacket);

		// A banner for moments that deserve a beat of their own (the vinyl landing). Above the morning paper's
		// shade, top-centre, click-through: it announces and gets out of the way.
		announcement = new PanelContainer { Name = "Announcement", Visible = false, ZIndex = 40, MouseFilter = Control.MouseFilterEnum.Ignore };
		announcement.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
		announcement.GrowHorizontal = Control.GrowDirection.Both;
		announcement.OffsetTop = 210;   // clear of the record jacket in the top corner
		var banner = PaperStyleBox.Sheet(new Color("f1e5c8"), 24, 12, 8, new Color("b5541c"));
		banner.BorderWidth = 3; banner.Radius = 4; banner.ShadowAlpha = 0.35f;
		announcement.AddThemeStyleboxOverride("panel", banner);
		announcementText = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
		announcementText.AddThemeFontSizeOverride("font_size", 22);
		announcementText.AddThemeColorOverride("font_color", new Color("2b2115"));
		announcement.AddChild(announcementText);
		AddChild(announcement);
	}

	private void ShowAnnouncement(string text) {
		if (announcement == null || string.IsNullOrWhiteSpace(text)) return;
		announcementText.Text = text;
		announcement.Show();
		int ticket = ++announcementTicket;   // a newer banner must not be hidden by an older banner's timer
		GetTree().CreateTimer(7.0).Timeout += () => { if (ticket == announcementTicket) announcement.Hide(); };
	}

	private void UpdateMainHud() {
		if (hudDate == null) return;
		mainHud.Visible = !isUIOpen;
		if (isUIOpen) { propTagTicket++; propTag.Visible = false; }
		if (recordJacket != null) {
			recordJacket.Refresh(PlayerDesk.Instance);
			if (isUIOpen) recordJacket.Visible = false;
		}
		foreach (Button prop in deskProps) prop.Visible = !isUIOpen;
		if (billboardButton != null) billboardButton.Visible = !isUIOpen;
		TimeManager time = TimeManager.Instance;
		PlayerDesk desk = PlayerDesk.Instance;
		paintedRecords?.Set(desk?.Label != null ? LabelBrand.For(desk.Label) : null, desk?.Label?.labelName);
		if (paintedGold != null) {
			int dayKey = time == null ? 0 : time.CurrentDate.year * 400 + time.CurrentDate.month * 32 + time.CurrentDate.day;
			string wentGold = paintedGold.Refresh(desk, desk?.Label != null ? LabelBrand.For(desk.Label) : null, desk?.Label?.labelName, dayKey);
			if (wentGold != null) ShowAnnouncement($"GOLD RECORD  —  \"{wentGold}\" has passed {PaintedGoldRecord.GoldUnits:N0} copies");
		}
		bool overdrawn = desk?.Label != null && desk.Label.cashReserves < 0f;
		string cash = desk?.Label == null ? "" : $"  •  {(overdrawn ? "−" : "")}${Mathf.Abs(desk.Label.cashReserves):N0} cash";
		hudDate.Text = time == null ? "" : $"{time.CurrentDate.ToHeadlineString()}  •  {time.GetTimeString()}{cash}";
		hudDate.AddThemeColorOverride("font_color", overdrawn ? StampRed : new Color("2b2115"));
		bool hasStep = desk?.HasLabel == true && !desk.IsGameOver && desk.Label != null;
		hudNextUp.Text = hasStep ? "NEXT UP: " + PlayerDeskPanel.NextUpHint(desk) : "";
		hudNextUp.Visible = hasStep;
		hudTicker.Text = desk?.Log.FirstOrDefault() ?? "No office news yet.";
	}

	public void RefreshMainHud() => UpdateMainHud();

	private void OnClockRestored(GameDate date) {
		paintedGold?.Forget();
		DismissMorningPaper();
		UpdateMainHud();
	}

	private void OnMorningStarted(GameDate date) {
		UpdateMainHud();
		PlayerDesk desk = PlayerDesk.Instance;
		if (desk?.HasLabel == true && !string.IsNullOrEmpty(desk.MorningDigest)
			&& !queuedMorningDigests.Contains(desk.MorningDigest))
			queuedMorningDigests.Add(desk.MorningDigest);
		// Capture each day's summary before a multi-day skip replaces it with the next morning's digest.
		if (paperQueued) return;
		paperQueued = true;
		Callable.From(() => {
			paperQueued = false;
			PlayerDesk currentDesk = PlayerDesk.Instance;
			if (currentDesk?.HasLabel != true || currentDesk.IsGameOver || queuedMorningDigests.Count == 0) {
				queuedMorningDigests.Clear();
				return;
			}
			string[] digests = queuedMorningDigests.ToArray();
			queuedMorningDigests.Clear();
			string DigestDate(string digest) {
				int end = digest.IndexOf(": ", System.StringComparison.Ordinal);
				return end >= 0 ? digest.Substring(0, end) : digest;
			}
			paperDate.Text = digests.Length == 1
				? DigestDate(digests[0]).ToUpperInvariant()
				: $"{DigestDate(digests[0])} — {DigestDate(digests[digests.Length - 1])}".ToUpperInvariant();
			paperHeading.Text = digests.Length == 1 ? "YESTERDAY AT THE LABEL" : "WHILE YOU WERE AWAY";
			string[] dayDigests = digests.Length > 1
				? digests.Where(digest => !digest.EndsWith("a quiet day at the office.", System.StringComparison.Ordinal)).ToArray()
				: digests;
			if (dayDigests.Length == 0) dayDigests = digests.TakeLast(1).ToArray();
			GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			// A big enough day earns an EXTRA: a red band over the nameplate, a special edition, a dearer price.
			bool extra = PaperExtras.IsExtra(dayDigests);
			paperExtraBand.Visible = extra;
			paperEdition.Text = extra ? "SPECIAL EXTRA EDITION" : $"{today.DayName.ToUpperInvariant()} EDITION";
			paperPrice.Text = extra || today.DayOfWeek == System.DayOfWeek.Sunday ? "TEN CENTS" : "FIVE CENTS";
			RenderPaper(dayDigests, digests.Length > 1, currentDesk.TakeTradeNews());
			paperScroll.ScrollVertical = 0;
			if (!morningPaper.Visible) uiOpenBeforePaper = isUIOpen;
			morningPaper.Show();
			morningPaper.MoveToFront();
			// The paper lands on the desk: a short fade and settle rather than a pop.
			morningPaper.Modulate = new Color(1, 1, 1, 0);
			paperSheet.Scale = new Vector2(0.96f, 0.96f);
			var land = CreateTween().SetParallel(true);
			land.TweenProperty(morningPaper, "modulate:a", 1.0f, 0.18);
			land.TweenProperty(paperSheet, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			isUIOpen = true;
			UpdateMainHud();
		}).CallDeferred();
	}

	private Label PaperText(string text, int size, Color color, Font font = null) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontOverride("font", font ?? paperSerif);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static Control PaperRule(float thickness, Color color) =>
		new ColorRect { Color = color, CustomMinimumSize = new Vector2(0, thickness), MouseFilter = MouseFilterEnum.Ignore };

	/// <summary>Lays the paper out like a paper: the day's front-page story as a headline across the top, other front-page
	/// items under it, and the small stuff (then the trade) in two ruled columns. A multi-day skip groups each day under
	/// its dateline.</summary>
	private void RenderPaper(string[] dayDigests, bool multiDay, System.Collections.Generic.IReadOnlyList<string> trade) {
		foreach (Node child in paperBody.GetChildren()) child.QueueFree();
		Color ink = new("30291d"), rust = new("6b3a1c"), body = new("4a4132"), rule = new("867655");
		string DigestDate(string digest) {
			int end = digest.IndexOf(": ", System.StringComparison.Ordinal);
			return end >= 0 ? digest.Substring(0, end) : digest;
		}
		VBoxContainer lastLeft = null, lastRight = null;
		int leftChars = 0, rightChars = 0;
		foreach (string digest in dayDigests) {
			int dateEnd = digest.IndexOf(": ", System.StringComparison.Ordinal);
			string events = dateEnd >= 0 ? digest.Substring(dateEnd + 2) : digest;
			string[] stories = PaperExtras.RollUp(events.Split("  •  ", System.StringSplitOptions.RemoveEmptyEntries)
				.Distinct(System.StringComparer.OrdinalIgnoreCase))
				.Select(story => char.ToUpperInvariant(story[0]) + story.Substring(1)).ToArray();
			if (multiDay) {
				paperBody.AddChild(PaperText(DigestDate(digest).ToUpperInvariant(), 15, rust, PaperTheme.SansSemiBold));
				paperBody.AddChild(PaperRule(1, rule));
			}
			var front = stories.Where(PlayerDesk.IsFrontPageStory).ToList();
			var rest = stories.Where(story => !PlayerDesk.IsFrontPageStory(story)).ToList();
			// The lead is the day's real headline. With no front-page news the first item still leads, smaller.
			bool realLead = front.Count > 0;
			string lead = realLead ? front[0] : rest.Count > 0 ? rest[0] : null;
			if (!realLead && rest.Count > 0) rest.RemoveAt(0);
			if (lead != null) {
				Label headline = PaperText(lead, realLead ? 36 : 26, ink, PaperTheme.SerifBold);
				// A story about one of the player's acts carries the act's halftone photo beside the headline.
				SimulatedArtist leadAct = realLead ? PaperExtras.ActFor(lead, PlayerDesk.Instance) : null;
				if (leadAct != null) {
					var leadRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					leadRow.AddThemeConstantOverride("separation", 18);
					leadRow.AddChild(headline);
					leadRow.AddChild(PaperExtras.PhotoSlot(leadAct));
					paperBody.AddChild(leadRow);
				} else paperBody.AddChild(headline);
			}
			foreach (string story in front.Skip(1)) paperBody.AddChild(PaperText(story, 23, ink, PaperTheme.SerifBold));
			bool lastDay = ReferenceEquals(digest, dayDigests[^1]);
			bool hasTrade = trade != null && trade.Count > 0 && lastDay;
			if (rest.Count == 0 && !hasTrade) { if (multiDay) paperBody.AddChild(PaperRule(1, rule)); continue; }

			if (lead != null) paperBody.AddChild(PaperRule(1, rule));
			var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			left.AddThemeConstantOverride("separation", 10);
			right.AddThemeConstantOverride("separation", 10);
			leftChars = rightChars = 0;
			foreach (string story in rest) {
				// Flow into whichever column is shorter, so the two stay level.
				bool intoLeft = leftChars <= rightChars;
				string stamp = PaperExtras.StampFor(story);
				(intoLeft ? left : right).AddChild(stamp != null ? PaperExtras.Clipping(story, stamp, 17, body) : PaperText(story, 17, body));
				if (intoLeft) leftChars += story.Length + 40; else rightChars += story.Length + 40;
			}
			var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			columns.AddThemeConstantOverride("separation", 18);
			columns.AddChild(left);
			columns.AddChild(new ColorRect { Color = new Color(rule, 0.7f), CustomMinimumSize = new Vector2(1, 0), MouseFilter = MouseFilterEnum.Ignore });
			columns.AddChild(right);
			paperBody.AddChild(columns);
			lastLeft = left; lastRight = right;
			if (multiDay) paperBody.AddChild(PaperRule(1, rule));
		}
		// What the rest of the business did: the chart, a market breaking, a rival's signing. Not the player's ledger.
		// It runs as a rubric at the foot of the shorter column of the latest day.
		if (trade != null && trade.Count > 0 && lastLeft != null) {
			VBoxContainer target = leftChars <= rightChars ? lastLeft : lastRight;
			target.AddChild(PaperRule(1, rule));
			target.AddChild(PaperText("THE TRADE", 15, rust, PaperTheme.SansSemiBold));
			foreach (string line in trade) target.AddChild(PaperText(line, 17, body));
		}
		// A quiet day is filled, not hidden: the weather, the price board and an ad, so there is always a paper to read.
		int storyCount = dayDigests.Sum(digest => digest.Split("  •  ", System.StringSplitOptions.RemoveEmptyEntries).Length);
		GameDate paperDay = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		AILabel house = PlayerDesk.Instance?.Label;
		if (lastLeft != null && lastRight != null) {
			VBoxContainer shorter = leftChars <= rightChars ? lastLeft : lastRight, longer = ReferenceEquals(shorter, lastLeft) ? lastRight : lastLeft;
			shorter.AddChild(PaperExtras.Weather(paperDay, house, ink, rust));
			if (storyCount < 6) {
				longer.AddChild(PaperExtras.PriceBoard(paperDay, ink, rust));
				shorter.AddChild(PaperExtras.Ad(paperDay, house, ink));
			}
		} else {
			// Nothing was set in columns today (one line of news, or none): give the paper its own two.
			var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			left.AddThemeConstantOverride("separation", 12);
			right.AddThemeConstantOverride("separation", 12);
			left.AddChild(PaperExtras.Weather(paperDay, house, ink, rust));
			left.AddChild(PaperExtras.PriceBoard(paperDay, ink, rust));
			right.AddChild(PaperExtras.Ad(paperDay, house, ink));
			var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			columns.AddThemeConstantOverride("separation", 18);
			columns.AddChild(left);
			columns.AddChild(new ColorRect { Color = new Color(rule, 0.7f), CustomMinimumSize = new Vector2(1, 0), MouseFilter = MouseFilterEnum.Ignore });
			columns.AddChild(right);
			paperBody.AddChild(PaperRule(1, rule));
			paperBody.AddChild(columns);
		}
		// A record of the player's went out: the label takes a small ad under the news, in its own crest and ink.
		string released = dayDigests.SelectMany(digest => digest.Split("  •  ", System.StringSplitOptions.RemoveEmptyEntries))
			.FirstOrDefault(story => story.Contains("RELEASED:", System.StringComparison.Ordinal));
		AILabel playerLabel = PlayerDesk.Instance?.Label;
		if (released != null && playerLabel != null) paperBody.AddChild(ReleaseAd(released, playerLabel, ink, rule));
	}

	/// <summary>The label's ad for a record that just went out: the 45's centre label, "NEW ON" the label in its
	/// lettering, the title and the act. Built off the story the desk logged ("RELEASED: "Title" b/w "Flip" by Act (date).").</summary>
	private Control ReleaseAd(string story, AILabel label, Color ink, Color rule) {
		var match = System.Text.RegularExpressions.Regex.Match(story, "RELEASED: \"(?<a>[^\"]+)\"(?: b/w \"(?<b>[^\"]+)\")? by (?<act>.+?) \\(");
		string title = match.Success ? match.Groups["a"].Value : "A new release";
		string flip = match.Success && match.Groups["b"].Success ? match.Groups["b"].Value : null;
		string act = match.Success ? match.Groups["act"].Value : null;
		LabelBrand brand = LabelBrand.For(label);

		var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
		frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color(1f, 1f, 1f, 0.12f), BorderColor = ink,
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 16, ContentMarginRight = 22, ContentMarginTop = 12, ContentMarginBottom = 12
		});
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);
		frame.AddChild(row);
		row.AddChild(new LabelCrest().Set(brand, label.labelName, 92f, LabelCrest.Mode.Disc45));

		var text = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
		text.AddThemeConstantOverride("separation", 1);
		row.AddChild(text);
		Label Line(string value, Font font, int size, Color color) {
			var line = new Label { Text = value };
			line.AddThemeFontOverride("font", font);
			line.AddThemeFontSizeOverride("font_size", size);
			line.AddThemeColorOverride("font_color", color);
			return line;
		}
		text.AddChild(Line($"NEW ON {brand.DisplayName(label.labelName).ToUpperInvariant()}", PaperTheme.Lettering(brand.Lettering), brand.Lettering == LetteringStyle.Script ? 26 : 17, brand.Pair.Ink));
		text.AddChild(Line($"“{title}”", PaperTheme.SerifBold, 27, ink));
		if (flip != null) text.AddChild(Line($"backed with “{flip}”", paperSerif, 16, ink));
		if (act != null) text.AddChild(Line($"by {act}", paperSerif, 17, ink));
		text.AddChild(new ColorRect { Color = brand.Pair.Accent, CustomMinimumSize = new Vector2(0, 3), MouseFilter = MouseFilterEnum.Ignore });
		text.AddChild(Line("A 45 at better record shops everywhere", PaperTheme.SansSemiBold, 12, rule.Darkened(0.35f)));
		return frame;
	}

	private void BuildMorningPaper() {
		morningPaper = new Control { Name = "MorningPaper", Visible = false, ZIndex = 30, MouseFilter = MouseFilterEnum.Stop };
		morningPaper.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(morningPaper);
		var shade = new ColorRect { Color = new Color(0, 0, 0, .55f) };
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		shade.GuiInput += input => {
			if (input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left) {
				DismissMorningPaper();
				shade.AcceptEvent();
			}
		};
		morningPaper.AddChild(shade);

		// The sheet is a plain Control so the fold corner can sit on its own corner, outside the layout.
		paperSheet = new Control { MouseFilter = MouseFilterEnum.Stop };
		paperSheet.SetAnchorsPreset(LayoutPreset.Center);
		paperSheet.OffsetLeft = -450; paperSheet.OffsetRight = 450;
		paperSheet.OffsetTop = -335; paperSheet.OffsetBottom = 335;
		paperSheet.PivotOffset = new Vector2(450, 335);
		morningPaper.AddChild(paperSheet);
		var sheet = new PanelContainer { MouseFilter = MouseFilterEnum.Pass };
		sheet.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		// Newsprint: greyer and rougher than the office's ledger paper.
		var newsprint = PaperStyleBox.Sheet(new Color("e6dbbf"), 42, 26, 18, new Color("867655"));
		newsprint.Grain = 1.4f; newsprint.Falloff = 0.8f; newsprint.Burn = 0.8f;
		sheet.AddThemeStyleboxOverride("panel", newsprint);
		paperSheet.AddChild(sheet);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 8);
		sheet.AddChild(column);
		Font serif = PaperTheme.Serif;   // bundled Gelasio, so the paper reads the same on every OS
		paperSerif = serif;
		Color ink = new("30291d");
		Label Small(string text, HorizontalAlignment alignment, Font font, int size) {
			var label = new Label { Text = text, HorizontalAlignment = alignment, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			label.AddThemeFontOverride("font", font);
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", ink);
			return label;
		}

		// The ears: edition on the left, price on the right, either side of the date.
		var ears = new HBoxContainer();
		paperEdition = Small("", HorizontalAlignment.Left, PaperTheme.SansSemiBold, 13);
		paperDate = Small("", HorizontalAlignment.Center, PaperTheme.SansSemiBold, 14);
		paperPrice = Small("", HorizontalAlignment.Right, PaperTheme.SansSemiBold, 13);
		ears.AddChild(paperEdition); ears.AddChild(paperDate); ears.AddChild(paperPrice);
		column.AddChild(ears);

		// The nameplate: Abril Fatface, big, between a thick rule and a hairline.
		paperExtraBand = Small("EXTRA  ★  EXTRA  ★  EXTRA", HorizontalAlignment.Center, PaperTheme.Lettering(LetteringStyle.HeavySlab), 20);
		paperExtraBand.AddThemeColorOverride("font_color", StampRed);
		paperExtraBand.Visible = false;
		column.AddChild(paperExtraBand);
		column.AddChild(PaperRule(4, ink));
		column.AddChild(PaperRule(1, ink));
		Label masthead = Small("The Morning Paper", HorizontalAlignment.Center, PaperTheme.Lettering(LetteringStyle.Didone), 64);
		paperMasthead = masthead;
		column.AddChild(masthead);
		column.AddChild(PaperRule(1, ink));
		column.AddChild(PaperRule(4, ink));

		paperHeading = Small("YESTERDAY AT THE LABEL", HorizontalAlignment.Left, PaperTheme.SansSemiBold, 15);
		paperHeading.AddThemeColorOverride("font_color", new Color("6b3a1c"));
		column.AddChild(paperHeading);
		paperScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		column.AddChild(paperScroll);
		var bodyGutter = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		bodyGutter.AddThemeConstantOverride("margin_right", 10);
		paperScroll.AddChild(bodyGutter);
		paperBody = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		paperBody.AddThemeConstantOverride("separation", 12);
		bodyGutter.AddChild(paperBody);

		// The dog-ear folds the paper away.
		var corner = new FoldCorner { ZIndex = 2 };
		corner.SetAnchorsPreset(LayoutPreset.BottomRight);
		corner.OffsetLeft = -FoldCorner.LiftedSize; corner.OffsetTop = -FoldCorner.LiftedSize;
		corner.OffsetRight = 0; corner.OffsetBottom = 0;
		corner.Pressed += DismissMorningPaper;
		paperSheet.AddChild(corner);
	}

	private bool paperSliding;

	private void DismissMorningPaper() {
		if (morningPaper?.Visible != true || paperSliding) return;
		isUIOpen = uiOpenBeforePaper;
		// Folded away: the sheet slides off the desk toward you, tipping as it goes, and the room fades back in.
		paperSliding = true;
		paperSheet.PivotOffset = new Vector2(450, 335);
		var slide = CreateTween().SetParallel(true);
		slide.TweenProperty(paperSheet, "position:y", paperSheet.Position.Y + 760f, 0.26).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		slide.TweenProperty(paperSheet, "rotation_degrees", -4.5f, 0.26).SetEase(Tween.EaseType.In);
		slide.TweenProperty(morningPaper, "modulate:a", 0f, 0.26).SetEase(Tween.EaseType.In);
		slide.Finished += () => {
			paperSliding = false;
			morningPaper.Hide();
			morningPaper.Modulate = Colors.White;
			paperSheet.Position = Vector2.Zero;
			paperSheet.RotationDegrees = 0f;
			UpdateMainHud();
		};
		UpdateMainHud();
	}

	public override void _Input(InputEvent input) {
		if (morningPaper?.Visible == true && input.IsActionPressed("ui_cancel")) {
			DismissMorningPaper();
			GetViewport().SetInputAsHandled();
		}
	}

	// Props on the desk art that open a department. Rects are in the scene's 1920x1080 design space.
	private readonly List<Button> deskProps = new();
	private TextureButton billboardButton;

	private void BuildDeskProps()
	{
		// Rects hug the painted prop; Tag is the point the paper tag hangs above.
		var props = new (string Caption, string Tab, Rect2 Area, Vector2 Tag)[] {
			("THE PHONE  -  the Rolodex: DJs, stations and calls", "ROLODEX", new Rect2(1480, 330, 252, 134), new Vector2(1606, 326)),
			("THE STACKS  -  your catalog", "CATALOG", new Rect2(1740, 306, 180, 174), new Vector2(1830, 302)),
			("THE STACKS  -  your catalog", "CATALOG", new Rect2(846, 326, 286, 112), new Vector2(989, 322)),
			("THE TYPEWRITER  -  ledger of money, acts and records", "LEDGER", new Rect2(1165, 290, 313, 158), new Vector2(1322, 286)),
			("THE CHECKBOOK  -  the books and finances", "FINANCES", new Rect2(18, 606, 810, 282), new Vector2(420, 602)),
			("THE PAD  -  scout and sign acts (A&R)", "A&R", new Rect2(108, 892, 1188, 188), new Vector2(700, 888)),
		};
		int insertAt = deskPanel != null ? deskPanel.GetIndex() : GetChildCount();
		foreach (var (caption, tab, area, tag) in props) {
			var button = new Button {
				Flat = true, FocusMode = Control.FocusModeEnum.None,
				MouseDefaultCursorShape = Control.CursorShape.PointingHand,
				Position = area.Position, Size = area.Size, Name = "Prop" + tab.Replace("&", "") + insertAt
			};
			StyleHotspot(button);
			string target = tab, text = caption;
			Vector2 tagPoint = tag;
			button.MouseEntered += () => ShowPropTag(text, tagPoint);
			button.MouseExited += HidePropTag;
			button.Pressed += () => OpenOfficeAt(target);
			AddChild(button);
			MoveChild(button, insertAt++);   // beneath the office panel and the paper, above the bare scene
			deskProps.Add(button);
		}
	}

	private void OpenOfficeAt(string tab)
	{
		if (morningPaper?.Visible == true || isUIOpen || deskPanel == null) return;
		deskPanel.OpenAtTab(tab);
		isUIOpen = true;
		UpdateMainHud();
	}

	public void OnClick_Desk()
	{
		if (morningPaper?.Visible == true) return;
		if (deskPanel == null) { GD.PushWarning("PlayerDeskPanel is missing from the scene!"); return; }
		if (isUIOpen && !deskPanel.Visible) return;
		deskPanel.Open();
		isUIOpen = true;
		UpdateMainHud();
	}

	public override void _ExitTree()
	{
		if (PlayerDesk.Instance != null) {
			PlayerDesk.Instance.Changed -= UpdateMainHud;
			PlayerDesk.Instance.Announcement -= ShowAnnouncement;
		}
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnDayStarted -= OnMorningStarted;
			TimeManager.Instance.OnClockRestored -= OnClockRestored;
			TimeManager.Instance.OnDayStarted -= UpdateCalendarButton;
			TimeManager.Instance.OnClockRestored -= UpdateCalendarButton;
			TimeManager.Instance.OnHourChanged -= OnHourChangedForHud;
			TimeManager.Instance.OnDayStarted -= OnDayStartedForLight;
			TimeManager.Instance.OnClockRestored -= OnDayStartedForLight;
		}
	}

	private void OnHourChangedForHud(int _) { ApplyOfficeLight(animate: true); UpdateMainHud(); }
	private void OnDayStartedForLight(GameDate _) => ApplyOfficeLight(animate: false);

	// ── Office light ────────────────────────────────────────────────────────────────────────────
	// One painted desk serves every hour, so the hour tints it: full daylight through the working day,
	// warm toward six, a dusk blue by eight, night after nine. Modulate only darkens, and it takes the
	// whole painted room with it, lamp included. When a real night-lit background exists, crossfade to it
	// with the same OfficeTint(hour) deciding the blend and drop the modulate.
	private static readonly (float Hour, Color Tint)[] OfficeLightKeys = {
		(9f,  new Color(1.00f, 1.00f, 1.00f)),
		(15f, new Color(1.00f, 1.00f, 1.00f)),
		(17f, new Color(1.00f, 0.93f, 0.82f)),   // low sun through the blinds
		(19f, new Color(0.84f, 0.74f, 0.74f)),   // dusk
		(21f, new Color(0.52f, 0.56f, 0.76f)),   // night
	};

	public static Color OfficeTint(float hour) {
		if (hour <= OfficeLightKeys[0].Hour) return OfficeLightKeys[0].Tint;
		for (int i = 1; i < OfficeLightKeys.Length; i++) {
			if (hour > OfficeLightKeys[i].Hour) continue;
			var (h0, c0) = OfficeLightKeys[i - 1];
			var (h1, c1) = OfficeLightKeys[i];
			return c0.Lerp(c1, (hour - h0) / (h1 - h0));
		}
		return OfficeLightKeys[^1].Tint;
	}

	private void ApplyOfficeLight(bool animate) {
		if (officeBackdrop == null || TimeManager.Instance == null) return;
		Color target = OfficeTint(TimeManager.Instance.CurrentHour + TimeManager.Instance.CurrentMinute / 60f);
		officeLightTween?.Kill();
		if (!animate) { officeBackdrop.Modulate = target; if (paintedLayer != null) paintedLayer.Modulate = target; return; }
		officeLightTween = CreateTween().SetParallel(true);
		officeLightTween.TweenProperty(officeBackdrop, "modulate", target, 0.8);
		if (paintedLayer != null) officeLightTween.TweenProperty(paintedLayer, "modulate", target, 0.8);
	}

	public void OpenArtist(string artistId, bool isOwnedByPlayer = false, int startTab = 0)
	{
		if (string.IsNullOrEmpty(artistId) || artistDetailPanel == null) return;
		StackDossiers(artistDetailPanel, labelDetailPanel);
		artistDetailPanel.ShowArtist(artistId, isOwnedByPlayer, startTab);
		isUIOpen = true;
		UpdateMainHud();
	}

	// The two dossiers stack (close the artist and the label is still under it). The open folder tab lifts
	// itself with ZIndex, and a z lift beats tree order, so the buried dossier's tab would otherwise draw
	// through the one on top. Give the dossier being opened its own band above the other's.
	private static void StackDossiers(Control top, Control buried)
	{
		if (buried != null) buried.ZIndex = 2;
		top.ZIndex = 4;
	}

	/// <summary>Opens an act's dossier straight to its DISCOGRAPHY page (tab index 1).</summary>
	public void OpenDiscography(string artistId, bool isOwnedByPlayer = false) => OpenArtist(artistId, isOwnedByPlayer, 1);

	public void OpenLabel(string labelId, bool isOwnedByPlayer = false)
	{
		if (string.IsNullOrEmpty(labelId) || labelDetailPanel == null) return;
		StackDossiers(labelDetailPanel, artistDetailPanel);
		labelDetailPanel.ShowLabel(labelId, isOwnedByPlayer);
		isUIOpen = true;
		UpdateMainHud();
	}

	public void OnClick_Ledger()
	{
		if (isUIOpen) return;
		OpenPanel(ledgerPanel);
		GD.Print("Ledger Opened: Time to check the books.");
	}

	public void OnClick_Charts()
	{
		if (isUIOpen) return;

		if (chartPanel != null)
		{
			chartPanel.Visible = true;
			isUIOpen = true;
			UpdateMainHud();

			// Assuming ChartUI script is attached directly to the chartPanel node
			var chartController = chartPanel as ChartUI;
			if (chartController != null)
			{
				chartController.OpenChart();
			}
			else
			{
				GD.PushWarning("ChartUI component not found on chartPanel!");
			}
			
			GD.Print("Charts Opened: Viewing the Hot 100.");
		}
		else
		{
			GD.PushWarning("Chart Panel is not assigned in UIManager!");
		}
	}

	public void OnClick_Phone()
	{
		if (isUIOpen) return;
		OpenPanel(dialoguePanel);
		UpdateMainHud();
		GD.Print("Phone Answered: Narrative event starting...");
	}

	public void OnClick_Calendar()
	{
		if (isUIOpen) return;
		if (TimeManager.Instance == null) return;
		TimeManager.Instance.SkipDays(1);
		GD.Print($"Calendar advanced to {TimeManager.Instance.CurrentDate.ToLongString()}.");
	}

	public void AdvanceOneDayFromDesk()
	{
		if (PlayerDesk.Instance?.HasLabel != true || TimeManager.Instance == null) return;
		TimeManager.Instance.SkipDays(1);
	}

	public void OpenCalendarSkipOptionsFromDesk() => ShowCalendarPopup();

	private void OnCalendarGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Right) return;
		ShowCalendarPopup();
		calendarButton.AcceptEvent();
	}

	private void ShowCalendarPopup()
	{
		if (TimeManager.Instance == null) return;
		if (calendarPopup == null) BuildCalendarPopup();

		var nextEvent = TimeManager.Instance.GetNextEvent();
		var nextButton = calendarPopup.GetNode<Button>("Margin/Options/NextEvent");
		// The calendar only lists chart days; the vinyl landing or a ship date can come sooner, and a skip
		// stops for those (and for a ringing office) too, so the button names whichever is first.
		var ownEvent = PlayerDesk.Instance?.NextPlayerEvent(TimeManager.Instance.CurrentDate);
		if (ownEvent != null && (nextEvent == null || ownEvent.Value.Date < nextEvent.date))
			nextButton.Text = $"Skip to next event ({ownEvent.Value.Title}, {ownEvent.Value.Date.ToHeadlineString()})";
		else
			nextButton.Text = nextEvent == null
				? "No upcoming event"
				: $"Skip to next event ({nextEvent.title}, {nextEvent.date.ToHeadlineString()})";
		nextButton.Disabled = nextEvent == null && ownEvent == null;
		nextButton.TooltipText = "Stops early for the vinyl landing, a ship date, or a call at the office while you are there.";
		calendarPopup.PopupCentered(new Vector2I(500, 300));
	}

	private void BuildCalendarPopup()
	{
		calendarPopup = new PopupPanel { Name = "CalendarPopup" };
		AddChild(calendarPopup);
		var margin = new MarginContainer { Name = "Margin" };
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 22);
		margin.AddThemeConstantOverride("margin_right", 22);
		margin.AddThemeConstantOverride("margin_top", 18);
		margin.AddThemeConstantOverride("margin_bottom", 18);
		calendarPopup.AddChild(margin);
		var options = new VBoxContainer { Name = "Options" };
		options.AddThemeConstantOverride("separation", 10);
		margin.AddChild(options);
		var title = new Label { Text = "ADVANCE CALENDAR" };
		title.AddThemeFontSizeOverride("font_size", 22);
		title.AddThemeFontOverride("font", PaperTheme.SansBold);
		title.AddThemeColorOverride("font_color", PaperTheme.Rust);
		options.AddChild(title);

		var friday = new Button { Text = "Skip to Friday" };
		friday.Pressed += () => RunCalendarSkip(() => TimeManager.Instance.SkipToFriday());
		options.AddChild(friday);
		var next = new Button { Name = "NextEvent", Text = "Skip to next event" };
		next.Pressed += () => RunCalendarSkip(() => TimeManager.Instance.SkipToNextEvent());
		options.AddChild(next);

		var daysRow = new HBoxContainer();
		skipDaysInput = new SpinBox { MinValue = 1, MaxValue = 365, Value = 7, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		daysRow.AddChild(skipDaysInput);
		var skipDays = new Button { Text = "Skip days" };
		skipDays.Pressed += () => RunCalendarSkip(() => TimeManager.Instance.SkipDays((int)skipDaysInput.Value));
		daysRow.AddChild(skipDays);
		options.AddChild(daysRow);
		var close = new Button { Text = "Close" };
		close.Pressed += calendarPopup.Hide;
		options.AddChild(close);
	}

	private void RunCalendarSkip(System.Func<ScheduledEvent> skip)
	{
		calendarPopup.Hide();
		var interruptedBy = skip();
		string reason = interruptedBy == null ? "" : $" (stopped for {interruptedBy.title})";
		GD.Print($"Calendar advanced to {TimeManager.Instance.CurrentDate.ToLongString()}{reason}.");
		// The vinyl landing already raises its own banner; the rest would otherwise stop the skip silently.
		if (interruptedBy != null && interruptedBy.eventType is EventType.IncomingCall or EventType.RecordRelease)
			ShowAnnouncement($"SKIP STOPPED  —  {interruptedBy.title}");
	}

	private void UpdateCalendarButton(GameDate date)
	{
		if (calendarCards[0] == null) return;
		// Weekends are in stamp red, like the printed calendars of the day.
		Color ink = date.IsWeekend ? StampRed : new Color("2b2115");
		void Letter(Label label, string text, int size) {
			label.Text = text;
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", new Color(ink, 0.9f));
		}
		Letter(calendarCards[0], date.ShortMonthName.ToUpperInvariant(), 42);
		Letter(calendarCards[1], date.DayName[..3].ToUpperInvariant(), 20);
		Letter(calendarCards[2], date.day.ToString(), 62);
		Letter(calendarCards[3], date.year.ToString(), 40);
	}

	public void OnClick_CloseAll()
	{
		DismissMorningPaper();
		if (ledgerPanel != null) ledgerPanel.Visible = false;
		if (dialoguePanel != null) dialoguePanel.Visible = false;
		if (chartPanel != null) chartPanel.Visible = false;
		if (artistDetailPanel != null) artistDetailPanel.Visible = false;
		if (labelDetailPanel != null) labelDetailPanel.Visible = false;
		if (deskPanel != null) deskPanel.Visible = false;

		isUIOpen = false;
		UpdateMainHud();
	}

	private void OpenPanel(Control panel)
	{
		if (panel != null)
		{
			panel.Visible = true;
			isUIOpen = true;
			UpdateMainHud();
		}
	}
}
