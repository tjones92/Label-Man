using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// The founding page as three short steps -- who you were, where the office is, what the label is called -- each
// fitting on one screen. Feedback 2026-11-05: the old page was one long scroll with two unexplained stat systems and a
// MONEY AND RUNWAY paragraph explaining pressing costs and overdraft rules a new player doesn't need yet. The money a
// player does need -- what they start with and what the office costs -- now sits beside the town that sets it.
public partial class PlayerDeskPanel {
	private int foundingStep;   // 0 background, 1 home town, 2 name and look
	private static readonly string[] FoundingStepNames = { "YOUR BACKGROUND", "HOME TOWN", "NAME & LOOK" };
	private static readonly Color CardFill = new("ead8ad"), CardEdge = new("8a7048");

	private void PageFounding() {
		var desk = PlayerDesk.Instance;
		List<MarketRegion> regions = ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>();
		var regionName = regions.ToDictionary(r => r.regionId, r => r.regionName);
		List<MarketCity> cities = DistanceModel.GetCities()
			.OrderBy(city => regionName.GetValueOrDefault(city.parentRegionId, city.parentRegionId))
			.ThenByDescending(city => city.isRegionalHub).ThenBy(city => city.distributionTier).ThenBy(city => city.name)
			.ToList();
		if (cities.Count > 0 && cities.All(c => c.cityId != selectedFoundingCityId)) selectedFoundingCityId = cities[0].cityId;

		// A fainter ring on the founding pages: it sat over the town's money figures at full strength.
		if (pageStain != null) { pageStain.Modulate = new Color(1, 1, 1, 0.4f); pageStain.QueueRedraw(); }
		content.AddChild(FoundingBanner());
		if (foundingStep == 0) WelcomeBackStrip();
		content.AddChild(FoundingStepper());

		switch (foundingStep) {
			case 0: FoundingBackgroundStep(); break;
			case 1: FoundingTownStep(cities, regionName); break;
			default: FoundingNameStep(cities); break;
		}
	}

	private void GoToFoundingStep(int step) {
		foundingStep = Mathf.Clamp(step, 0, FoundingStepNames.Length - 1);
		Refresh();
		Callable.From(() => { if (contentScroll != null) contentScroll.ScrollVertical = 0; }).CallDeferred();
	}

	// ---- chrome ------------------------------------------------------------------------------------------------------

	/// <summary>A slim version of the title card: the name and the one line, without eating half the page.</summary>
	private Control FoundingBanner() {
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 16);
		var title = FoundingText("Start a Label", 40, Ink, PaperTheme.Lettering(LetteringStyle.Didone));
		title.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		title.AutowrapMode = TextServer.AutowrapMode.Off;
		row.AddChild(title);
		var line = FoundingText("Find the acts. Cut the records. Keep the doors open.", 16, Heard, PaperTheme.SerifItalic);
		line.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		row.AddChild(line);
		return row;
	}

	/// <summary>A returning player's way back in, as one line rather than a section.</summary>
	private void WelcomeBackStrip() {
		SaveGameService.SaveInfo? newest = SaveGameService.NewestSave();
		if (!newest.HasValue) return;
		SaveGameService.SaveInfo last = newest.Value;
		string slot = last.Slot;
		var strip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		strip.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(CardFill, 0.6f), BorderColor = CardEdge,
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ContentMarginLeft = 12, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6 });
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		strip.AddChild(row);
		var text = FoundingText($"Welcome back — {last.LabelName}, {last.InGameDate.ToHeadlineString()}" +
			$"  ·  {(SaveGameService.IsAutosaveSlot(slot) ? "autosave" : $"\"{slot}\"")}", 15, Ink, PaperTheme.Sans);
		text.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		row.AddChild(text);
		var cont = Primary("CONTINUE");
		cont.CustomMinimumSize = new Vector2(150, 34);
		cont.Pressed += () => { bool ok = SaveGameService.Load(slot, out string message); Say(message, ok); Refresh(); };
		row.AddChild(cont);
		content.AddChild(strip);
	}

	/// <summary>1 · 2 · 3 across the page: where you are, and a click back to a step you've already been through.</summary>
	private Control FoundingStepper() {
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 6);
		for (int i = 0; i < FoundingStepNames.Length; i++) {
			int step = i;
			bool current = i == foundingStep, done = i < foundingStep;
			var tab = new Button { Text = $"{i + 1}   {FoundingStepNames[i]}", SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 38),
				FocusMode = FocusModeEnum.None, Disabled = i > foundingStep };
			var face = new StyleBoxFlat { BgColor = current ? Rust : done ? CardFill : new Color(CardFill, 0.35f), BorderColor = current ? Rust : CardEdge,
				BorderWidthBottom = current ? 0 : 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1 };
			foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" }) tab.AddThemeStyleboxOverride(state, face);
			Color ink = current ? Paper : done ? Ink : new Color(Ink, 0.45f);
			foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_disabled_color" }) tab.AddThemeColorOverride(state, ink);
			tab.AddThemeFontOverride("font", PaperTheme.SansBold);
			tab.AddThemeFontSizeOverride("font_size", 14);
			if (done) { tab.TooltipText = "Go back to this step."; tab.Pressed += () => GoToFoundingStep(step); }
			row.AddChild(tab);
		}
		return row;
	}

	/// <summary>BACK on the left, the way forward on the right.</summary>
	private void FoundingFooter(string next, Action onNext, bool canGoBack = true) {
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		if (canGoBack && foundingStep > 0) {
			var back = Btn("←  BACK");
			back.CustomMinimumSize = new Vector2(120, 42);
			back.Pressed += () => GoToFoundingStep(foundingStep - 1);
			row.AddChild(back);
		}
		row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		var go = Primary(next);
		go.CustomMinimumSize = new Vector2(260, 44);
		go.Pressed += onNext;
		row.AddChild(go);
		content.AddChild(row);
	}

	// ---- step 1: who you were -----------------------------------------------------------------------------------------

	private void FoundingBackgroundStep() {
		var cards = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		cards.AddThemeConstantOverride("separation", 10);
		content.AddChild(cards);
		foreach (FoundingArchetype arch in Enum.GetValues<FoundingArchetype>()) cards.AddChild(BackgroundCard(arch));

		var selected = FoundingArchetypeData.Get(selectedArchetype);
		var description = FoundingText(selected.Description, 16, Ink, PaperTheme.Serif);
		content.AddChild(description);

		// The two sets of numbers, each under a heading that says whose they are.
		var blocks = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		blocks.AddThemeConstantOverride("separation", 10);
		content.AddChild(blocks);
		var you = FoundingBlock(blocks, "YOU", "How well you read people, records and rooms. Hover each for what it does.");
		you.AddChild(InstinctRow(selected.Instincts, false));
		var label = FoundingBlock(blocks, "YOUR LABEL STARTS WITH", "These grow as the label does.");
		label.AddChild(StatRow(("Scouting", selected.ScoutingAbility), ("Production", selected.ProductionQuality), ("Marketing", selected.MarketingPower)));

		FoundingFooter("NEXT:  HOME TOWN  →", () => GoToFoundingStep(1));
	}

	/// <summary>One background as a card: its print, its name, its line and the money it starts with.</summary>
	private Control BackgroundCard(FoundingArchetype arch) {
		var data = FoundingArchetypeData.Get(arch);
		bool selected = arch == selectedArchetype;
		var card = new Button { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 150), FocusMode = FocusModeEnum.None,
			TooltipText = ArchetypeMechanics(arch) };
		var face = new StyleBoxFlat { BgColor = selected ? Rust : CardFill, BorderColor = selected ? Rust : CardEdge,
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 };
		var hover = (StyleBoxFlat)face.Duplicate();
		if (!selected) hover.BgColor = new Color("f3e6c2");
		card.AddThemeStyleboxOverride("normal", face);
		card.AddThemeStyleboxOverride("pressed", face);
		card.AddThemeStyleboxOverride("hover", hover);
		card.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		card.Pressed += () => { selectedArchetype = arch; Refresh(); };

		var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
		column.SetAnchorsPreset(LayoutPreset.FullRect);
		column.AddThemeConstantOverride("separation", 4);
		card.AddChild(column);
		var print = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		var patch = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
		patch.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Paper, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4, ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2 });
		patch.AddChild(new LinocutIcon().Set(StrongestInstinct(data.Instincts), 42f));
		print.AddChild(patch);
		column.AddChild(print);
		Color ink = selected ? Paper : Ink;
		Label Centered(string text, int size, Font font, Color color) {
			var l = FoundingText(text, size, color, font);
			l.HorizontalAlignment = HorizontalAlignment.Center;
			l.MouseFilter = MouseFilterEnum.Ignore;
			return l;
		}
		column.AddChild(Centered(data.Name.Replace("The ", "").ToUpperInvariant(), 15, PaperTheme.SansBold, ink));
		column.AddChild(Centered(data.Tagline, 14, PaperTheme.SerifItalic, ink));
		column.AddChild(Centered($"${data.Capital:N0} to start", 15, PaperTheme.SansSemiBold, selected ? new Color("f6d98a") : Rust));
		return card;
	}

	// ---- step 2: the home town and the money ----------------------------------------------------------------------------

	private void FoundingTownStep(List<MarketCity> cities, Dictionary<string, string> regionName) {
		if (cities.Count == 0) { Body("No towns loaded."); return; }
		MarketCity city = cities.First(c => c.cityId == selectedFoundingCityId);
		var selected = FoundingArchetypeData.Get(selectedArchetype);

		var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		columns.AddThemeConstantOverride("separation", 20);
		content.AddChild(columns);

		// The atlas is the picker: every town where it really is, its region tinted. Click a town to make it home.
		var atlas = new UsMap { CustomMinimumSize = new Vector2(0, 420), SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsStretchRatio = 1.55f, SelectedCityId = city.cityId, HighlightRegionId = city.parentRegionId,
			TooltipText = "Click a town to make it your home. Bold towns are regional hubs." };
		atlas.CityNote = id => {
			MarketCity town = DistanceModel.GetCityById(id);
			return town == null ? null : $"${CityProfiles.MonthlyOverhead(id):N0} a month · {regionName.GetValueOrDefault(town.parentRegionId, town.parentRegionId)}{(town.isRegionalHub ? " · regional hub" : "")}";
		};
		atlas.CityPressed += id => {
			if (cities.All(c => c.cityId != id)) return;
			selectedFoundingCityId = id;
			Refresh();
		};
		columns.AddChild(atlas);

		var card = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		card.AddThemeConstantOverride("separation", 8);
		columns.AddChild(card);
		card.AddChild(FoundingText(city.name, 30, Ink, PaperTheme.SerifBold));
		card.AddChild(FoundingText($"{regionName.GetValueOrDefault(city.parentRegionId, city.parentRegionId)}{(city.isRegionalHub ? "  ·  regional hub" : "")}",
			15, Rust, PaperTheme.SansSemiBold));
		card.AddChild(MoneyBox(selected.Capital, city));
		card.AddChild(FoundingText(TownFacts(city, includeOffice: false), 14, Ink, PaperTheme.Sans));

		FoundingFooter("NEXT:  NAME THE LABEL  →", () => GoToFoundingStep(2));
	}

	/// <summary>The money a new player needs, and only that: what they start with, what this town's office costs, how
	/// long that is, and the one rule that ends the game. Everything else waits until it matters.</summary>
	private Control MoneyBox(float capital, MarketCity city) {
		float overhead = CityProfiles.MonthlyOverhead(city.cityId);
		int months = overhead > 0 ? Mathf.FloorToInt(capital / overhead) : 0;
		var box = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		box.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("f6eed6"), BorderColor = Rust,
			BorderWidthLeft = 3, ContentMarginLeft = 14, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 });
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 4);
		box.AddChild(column);
		var figures = new VBoxContainer();
		figures.AddThemeConstantOverride("separation", 0);
		column.AddChild(figures);
		void Figure(string value, string caption) {
			var stack = new HBoxContainer();
			stack.AddThemeConstantOverride("separation", 10);
			var big = FoundingText(value, 24, Ink, PaperTheme.SerifBold);
			var small = FoundingText(caption, 12, Heard, PaperTheme.SansSemiBold);
			small.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			big.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			big.CustomMinimumSize = new Vector2(72, 0);
			big.AutowrapMode = small.AutowrapMode = TextServer.AutowrapMode.Off;
			stack.AddChild(big);
			stack.AddChild(small);
			figures.AddChild(stack);
		}
		Figure($"${capital:N0}", "TO START");
		Figure($"${overhead:N0}", "A MONTH FOR THE OFFICE");
		Figure($"{months}", "MONTHS OF RENT IN HAND");
		column.AddChild(FoundingText("The bank looks at your cash on the 1st of every month. Three months in the red in a row and the doors close.",
			14, Ink, PaperTheme.Serif));
		return box;
	}

	// ---- step 3: the name and the look ----------------------------------------------------------------------------------

	private void FoundingNameStep(List<MarketCity> cities) {
		var nameEdit = new LineEdit { PlaceholderText = "What's the label called?", Text = foundingLabelName, CustomMinimumSize = new Vector2(0, 46),
			SizeFlagsHorizontal = SizeFlags.ExpandFill };
		StyleField(nameEdit);
		nameEdit.AddThemeFontSizeOverride("font_size", 20);
		content.AddChild(nameEdit);

		var brandPicker = new LabelBrandPicker(foundingBrand, foundingLabelName);
		brandPicker.Changed += () => foundingBrand = brandPicker.Customised ? brandPicker.Current : null;
		nameEdit.TextChanged += value => { foundingLabelName = value; brandPicker.SetLabelName(value); };
		content.AddChild(FoundingText("The crest, colours and lettering go on your office, contracts, records and the paper. Leave them and the name picks for you.",
			14, Heard, PaperTheme.Sans));
		content.AddChild(brandPicker);

		var selected = FoundingArchetypeData.Get(selectedArchetype);
		MarketCity city = cities.FirstOrDefault(c => c.cityId == selectedFoundingCityId) ?? cities.FirstOrDefault();
		if (city != null)
			content.AddChild(FoundingText($"{selected.Name}  ·  {city.name}  ·  ${selected.Capital:N0} to start  ·  ${CityProfiles.MonthlyOverhead(city.cityId):N0} a month",
				15, Rust, PaperTheme.SansSemiBold));
		FoundingFooter("OPEN THE DOORS", () => {
			if (city == null) { Say("No towns loaded.", false); return; }
			bool ok = PlayerDesk.Instance.FoundLabel(nameEdit.Text, city.cityId, selectedArchetype, brandPicker.Current, out string message);
			Say(message, ok);
			if (ok) foundingStep = 0;
			Refresh();
		});
		Callable.From(() => { if (IsInstanceValid(nameEdit) && string.IsNullOrEmpty(nameEdit.Text)) nameEdit.GrabFocus(); }).CallDeferred();
	}

	// ---- helpers ------------------------------------------------------------------------------------------------------

	private static Label FoundingText(string text, int size, Color color, Font font) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static VBoxContainer FoundingBlock(Container parent, string heading, string hint) {
		var block = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		block.AddThemeConstantOverride("separation", 6);
		var h = FoundingText(heading, 14, Rust, PaperTheme.SansBold);
		h.TooltipText = hint;
		h.MouseFilter = MouseFilterEnum.Pass;
		block.AddChild(h);
		parent.AddChild(block);
		return block;
	}
}
