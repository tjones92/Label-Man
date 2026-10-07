using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LabelDetailPanel : Control
{
	public event Action<string> ArtistRequested;
	public event Action Closed;
	private Label nameLabel, chromeLabel;
	private LabelCrest crest;
	private ColorRect letterheadRule;
	private HBoxContainer tabs;
	private VBoxContainer content;
	private readonly List<FolderTabButton> tabButtons = new();
	private AILabel label;
	private LabelPublicProfile profile;

	public override void _Ready() { BuildUi(); Visible = false; }
	public void ShowLabel(string labelId, bool isOwnedByPlayer = false)
	{
		label = ChartManager.Instance?.GetLabelById(labelId) ?? LabelLifecycleManager.Instance?.GetLabelById(labelId);
		if (label == null) { GD.PushWarning($"Label not found: {labelId}"); return; }
		label.isPlayerOwned |= isOwnedByPlayer; profile = label.GetPublicProfile();
		ApplyBrand();
		chromeLabel.Text = $"{Format(profile.archetype)}  •  {Format(profile.tier)}\n{profile.headquartersCity}  •  Founded {profile.foundedYear}";
		BuildTabs(); Visible = true; MoveToFront();
	}
	public void ClosePanel() { Visible = false; Closed?.Invoke(); }
	/// <summary>Dresses the header as the label's letterhead: crest, name in its lettering and ink, a rule in its accent.</summary>
	private void ApplyBrand()
	{
		LabelBrand brand = LabelBrand.For(label);
		crest.Set(brand, label.labelName, 56f);
		nameLabel.Text = brand.DisplayName(profile.labelName);
		nameLabel.AddThemeFontOverride("font", PaperTheme.Lettering(brand.Lettering));
		nameLabel.AddThemeFontSizeOverride("font_size", brand.Lettering == LetteringStyle.Script ? 44 : 34);
		nameLabel.AddThemeColorOverride("font_color", brand.Pair.Ink);
		letterheadRule.Color = brand.Pair.Accent;
	}

	private void BuildUi()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
		var shade = new ColorRect { Color = new Color(0, 0, 0, .38f) }; shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(shade);
		var folder = new PanelContainer(); folder.SetAnchorsPreset(LayoutPreset.Center); folder.Position = new Vector2(-540, -380); folder.Size = new Vector2(1080, 760); AddChild(folder);
		var folderPaper = new PaperStyleBox { Fill = new Color("cba96a"), Border = new Color("654a27"), BorderWidth = 2, Radius = 3, ShadowSize = 24, ShadowAlpha = 0.55f, ShadowOffset = new Vector2(0, 10), Burn = 1.15f };
		folderPaper.ContentMarginLeft = 34; folderPaper.ContentMarginRight = 34; folderPaper.ContentMarginTop = 28; folderPaper.ContentMarginBottom = 28;
		folder.AddThemeStyleboxOverride("panel", folderPaper);
		var root = new VBoxContainer(); root.AddThemeConstantOverride("separation", 10); folder.AddChild(root);
		var header = new HBoxContainer(); header.AddThemeConstantOverride("separation", 14); root.AddChild(header); crest = new LabelCrest(); header.AddChild(crest); nameLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis }; nameLabel.AddThemeFontSizeOverride("font_size", 30); header.AddChild(nameLabel);
		// The letterhead rule: the label's accent colour under its name.
		letterheadRule = new ColorRect { CustomMinimumSize = new Vector2(0, 4), Color = new Color("8a7048") }; root.AddChild(letterheadRule);
		var close = new Button { Text = "CLOSE  ×" }; close.Pressed += ClosePanel; header.AddChild(close);
		chromeLabel = new Label(); chromeLabel.AddThemeFontSizeOverride("font_size", 17); root.AddChild(chromeLabel);
		tabs = new HBoxContainer(); tabs.AddThemeConstantOverride("separation", 4); root.AddChild(tabs);
		var paper = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; paper.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("f1e5c8"), 28, 24, 6).Decorated(clip: false, ring: true, seed: 11)); root.AddChild(paper);
		var scroll = new ScrollContainer(); paper.AddChild(scroll); content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddThemeConstantOverride("separation", 12); scroll.AddChild(content);
	}
	private void BuildTabs() { Clear(tabs); tabButtons.Clear(); AddTab("OVERVIEW", ShowOverview); AddTab("ROSTER", ShowRoster); AddTab("TRACK RECORD", ShowTrackRecord); ActivateTab(0, ShowOverview); }
	private void AddTab(string title, Action page) { var b = new FolderTabButton { Text = title, CustomMinimumSize = new Vector2(190, 42) }; int i = tabButtons.Count; b.Pressed += () => ActivateTab(i, page); tabs.AddChild(b); tabButtons.Add(b); }
	private void ActivateTab(int index, Action page) { for (int i = 0; i < tabButtons.Count; i++) tabButtons[i].SetActive(i == index); Clear(content); page(); }
	private void ShowOverview()
	{
		AddHeading("TRADE PROFILE"); AddBody(profile.descriptionBlurb); AddBody(profile.statusImpression);
		AddHeading("SPECIALTIES"); AddBody(profile.preferredGenres.Length == 0 ? "No particular specialty on file." : string.Join("  •  ", profile.preferredGenres.Select(GenreNameFormatter.Format)));
		if (label.isPlayerOwned) { AddHeading("INTERNAL — FINANCIALS"); AddBody($"Cash reserves: ${label.cashReserves:N0}\nMonthly revenue: ${label.monthlyRevenue:N0}\nMonthly expenses: ${label.monthlyExpenses:N0}\nDebt: ${label.debtLevel:N0}"); }
	}
	private void ShowRoster()
	{
		AddHeading("SIGNED ROSTER"); if (label.roster == null || label.roster.Count == 0) { AddBody("No signed artists on file."); return; }
		// Each signing is a 45 sleeve, as on the player's own roster: the publicity photo, the name typed across the top, and this
		// label's disc showing through the die-cut window.
		foreach (var artist in label.roster.Where(a => a != null)) {
			var sleeve = new SleeveCard().Set(artist, label, $"{GenreNameFormatter.Format(artist.primaryGenre)}  •  {Format(artist.careerState)}", false);
			sleeve.AddFact($"{artist.totalReleases} {(artist.totalReleases == 1 ? "release" : "releases")}   •   {artist.top40Hits} Top 40");
			string id = artist.artistId;
			var open = new Button { Text = "DOSSIER" };
			open.Pressed += () => ArtistRequested?.Invoke(id);
			sleeve.AddVerb(open);
			content.AddChild(sleeve);
		}
	}
	private void ShowTrackRecord()
	{
		AddHeading("BY THE NUMBERS"); AddBody($"{profile.totalReleases} releases  •  {profile.top40Hits} Top 40 hits  •  {profile.numberOneHits} #1 hits");
		var events = label.roster?.SelectMany(a => a.careerEvents).Where(e => e.Contains(label.labelName, StringComparison.OrdinalIgnoreCase)).TakeLast(12).ToList() ?? new();
		AddHeading("NOTABLE MOVES"); AddBody(events.Count == 0 ? "No notable signings or departures on file." : string.Join("\n", events));
	}
	private void AddHeading(string text) {
		// A typed rubric over a hairline, as on the Morning Paper, rather than a bigger copy of the body face.
		var l = new Label { Text = text };
		l.AddThemeFontOverride("font", PaperTheme.SansSemiBold); l.AddThemeFontSizeOverride("font_size", 15); l.AddThemeColorOverride("font_color", PaperTheme.Rust);
		content.AddChild(l);
		content.AddChild(new ColorRect { Color = new Color(PaperTheme.Rust, 0.45f), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore });
	}
	private void AddBody(string text) { var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart }; l.AddThemeFontOverride("font", PaperTheme.Serif); l.AddThemeFontSizeOverride("font_size", 18); l.AddThemeColorOverride("font_color", PaperTheme.Ink); content.AddChild(l); }
	private static string Format(object value) { var s = value?.ToString() ?? ""; return string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString())); }
	private static void Clear(Node node) { foreach (Node child in node.GetChildren()) { node.RemoveChild(child); child.QueueFree(); } }
}
