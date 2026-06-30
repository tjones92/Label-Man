using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LabelDetailPanel : Control
{
	public event Action<string> ArtistRequested;
	public event Action Closed;
	private Label nameLabel, chromeLabel;
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
		nameLabel.Text = profile.labelName.ToUpperInvariant();
		chromeLabel.Text = $"{Format(profile.archetype)}  •  {Format(profile.tier)}\n{profile.headquartersCity}  •  Founded {profile.foundedYear}";
		BuildTabs(); Visible = true; MoveToFront();
	}
	public void ClosePanel() { Visible = false; Closed?.Invoke(); }

	private void BuildUi()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
		var shade = new ColorRect { Color = new Color(0, 0, 0, .38f) }; shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(shade);
		var folder = new PanelContainer(); folder.SetAnchorsPreset(LayoutPreset.Center); folder.Position = new Vector2(-540, -380); folder.Size = new Vector2(1080, 760); AddChild(folder);
		folder.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("cba96a"), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color("654a27"), ContentMarginLeft = 34, ContentMarginRight = 34, ContentMarginTop = 28, ContentMarginBottom = 28 });
		var root = new VBoxContainer(); root.AddThemeConstantOverride("separation", 10); folder.AddChild(root);
		var header = new HBoxContainer(); root.AddChild(header); nameLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill }; nameLabel.AddThemeFontSizeOverride("font_size", 30); header.AddChild(nameLabel);
		var close = new Button { Text = "CLOSE  ×" }; close.Pressed += ClosePanel; header.AddChild(close);
		chromeLabel = new Label(); chromeLabel.AddThemeFontSizeOverride("font_size", 17); root.AddChild(chromeLabel);
		tabs = new HBoxContainer(); tabs.AddThemeConstantOverride("separation", 4); root.AddChild(tabs);
		var paper = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; paper.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("f1e5c8"), ContentMarginLeft = 28, ContentMarginRight = 28, ContentMarginTop = 24, ContentMarginBottom = 24 }); root.AddChild(paper);
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
		foreach (var artist in label.roster.Where(a => a != null)) { var b = new Button { Text = $"{artist.stageName}   [{Format(artist.careerState)}]", Alignment = HorizontalAlignment.Left }; string id = artist.artistId; b.Pressed += () => ArtistRequested?.Invoke(id); content.AddChild(b); }
	}
	private void ShowTrackRecord()
	{
		AddHeading("BY THE NUMBERS"); AddBody($"{profile.totalReleases} releases  •  {profile.top40Hits} Top 40 hits  •  {profile.numberOneHits} #1 hits");
		var events = label.roster?.SelectMany(a => a.careerEvents).Where(e => e.Contains(label.labelName, StringComparison.OrdinalIgnoreCase)).TakeLast(12).ToList() ?? new();
		AddHeading("NOTABLE MOVES"); AddBody(events.Count == 0 ? "No notable signings or departures on file." : string.Join("\n", events));
	}
	private void AddHeading(string text) { var l = new Label { Text = text }; l.AddThemeFontSizeOverride("font_size", 21); l.AddThemeColorOverride("font_color", new Color("5b351f")); content.AddChild(l); }
	private void AddBody(string text) { var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart }; l.AddThemeFontSizeOverride("font_size", 17); content.AddChild(l); }
	private static string Format(object value) { var s = value?.ToString() ?? ""; return string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString())); }
	private static void Clear(Node node) { foreach (Node child in node.GetChildren()) { node.RemoveChild(child); child.QueueFree(); } }
}
