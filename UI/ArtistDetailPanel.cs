using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ArtistDetailPanel : Control
{
	public event Action<string> LabelRequested;
	public event Action Closed;
	private Label nameLabel, chromeLabel;
	private Button labelButton;
	private HBoxContainer tabs;
	private VBoxContainer content;
	private readonly List<FolderTabButton> tabButtons = new();
	private ArtistPublicProfile profile;
	private SimulatedArtist artist;

	public override void _Ready() { BuildUi(); Visible = false; }

	public void ShowArtist(string artistId, bool isOwnedByPlayer = false)
	{
		artist = ArtistManager.Instance?.GetArtist(artistId);
		profile = ArtistManager.Instance?.GetPublicProfile(artistId);
		if (artist == null || profile == null) { GD.PushWarning($"Artist not found: {artistId}"); return; }
		artist.isPlayerOwned |= isOwnedByPlayer;
		nameLabel.Text = profile.name.ToUpperInvariant();
		chromeLabel.Text = $"{Format(profile.artistType)}  •  {Format(profile.primaryGenre)}  •  {profile.homeRegion}\n{Format(profile.careerState)}  |  Formed {profile.formedYear}";
		labelButton.Text = string.IsNullOrEmpty(profile.labelId) ? "Independent" : $"Label: {profile.labelName}";
		labelButton.Visible = !string.IsNullOrEmpty(profile.labelId);
		BuildTabs();
		Visible = true;
		MoveToFront();
	}

	public void ClosePanel() { Visible = false; Closed?.Invoke(); }

	private void BuildUi()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		var shade = new ColorRect { Color = new Color(0, 0, 0, .38f) }; shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(shade);
		var folder = new PanelContainer(); folder.SetAnchorsPreset(LayoutPreset.Center); folder.Position = new Vector2(-570, -410); folder.Size = new Vector2(1140, 820); AddChild(folder);
		var style = new StyleBoxFlat { BgColor = new Color("d7b978"), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3, BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = new Color("70552c"), ContentMarginLeft = 34, ContentMarginRight = 34, ContentMarginTop = 28, ContentMarginBottom = 28 };
		folder.AddThemeStyleboxOverride("panel", style);
		var root = new VBoxContainer(); root.AddThemeConstantOverride("separation", 10); folder.AddChild(root);
		var header = new HBoxContainer(); root.AddChild(header);
		nameLabel = new Label(); nameLabel.AddThemeFontSizeOverride("font_size", 30); nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill; header.AddChild(nameLabel);
		var close = new Button { Text = "CLOSE  ×" }; close.Pressed += ClosePanel; header.AddChild(close);
		chromeLabel = new Label(); chromeLabel.AddThemeFontSizeOverride("font_size", 17); root.AddChild(chromeLabel);
		labelButton = new Button { Alignment = HorizontalAlignment.Left }; labelButton.Pressed += () => { if (!string.IsNullOrEmpty(profile?.labelId)) LabelRequested?.Invoke(profile.labelId); }; root.AddChild(labelButton);
		tabs = new HBoxContainer(); tabs.AddThemeConstantOverride("separation", 4); root.AddChild(tabs);
		var paper = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		paper.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("f1e5c8"), ContentMarginLeft = 28, ContentMarginRight = 28, ContentMarginTop = 24, ContentMarginBottom = 24 }); root.AddChild(paper);
		var scroll = new ScrollContainer(); paper.AddChild(scroll);
		content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddThemeConstantOverride("separation", 12); scroll.AddChild(content);
	}

	private void BuildTabs()
	{
		Clear(tabs); tabButtons.Clear();
		AddTab("OVERVIEW", ShowOverview); AddTab("DISCOGRAPHY", ShowDiscography); AddTab("GIGOGRAPHY", ShowGigography);
		AddTab("AWARDS", ShowAwards); AddTab("PERSONNEL", ShowPersonnel);
		if (artist.isPlayerOwned) AddTab("CONTRACT", ShowContract);
		ActivateTab(0, ShowOverview);
	}

	private void AddTab(string title, Action page)
	{
		var button = new FolderTabButton { Text = title, CustomMinimumSize = new Vector2(145, 42) };
		int index = tabButtons.Count; button.Pressed += () => ActivateTab(index, page); tabs.AddChild(button); tabButtons.Add(button);
	}

	private void ActivateTab(int index, Action page) { for (int i = 0; i < tabButtons.Count; i++) tabButtons[i].SetActive(i == index); Clear(content); page(); }
	private void ShowOverview()
	{
		AddHeading("PUBLIC FILE"); AddBody(JournalisticDescriptor.DescribeArtist(profile));
		AddHeading("CHART RECORD"); AddBody($"{profile.totalCharted} chart entries   •   {profile.top40Hits} Top 40   •   {profile.top10Hits} Top 10   •   {profile.numberOneHits} #1 hits");
		AddHeading("LINEUP AT A GLANCE"); AddBody(string.Join("\n", profile.personnel.Where(p => p.isActive).Select(p => $"{p.name} — {Format(p.role)}")));
		if (profile.reputationTags.Count > 0) AddBody("REPUTATION: " + string.Join("  •  ", profile.reputationTags.Select(t => t.ToDisplayString())));
	}
	private void ShowDiscography()
	{
		AddHeading("RELEASES"); var records = GetRecords();
		if (records.Count == 0) { AddBody("No releases on file."); return; }
		foreach (var r in records.OrderByDescending(r => r.baseRecord.releaseDate))
			AddBody($"“{r.baseRecord.title}”  —  {r.baseRecord.releaseDate.ToHeadlineString()}\nPeak {(r.peakPosition > 0 ? "#" + r.peakPosition : "—")}  •  {r.weeksOnChart} weeks  •  {ChartDetailPanel.GetSalesTierDescription(r.totalUnitsSold)}");
	}
	private void ShowGigography() { AddHeading("ON THE ROAD"); AddBody("Gigography is coming soon. Touring records are not yet kept by the simulation."); }
	private void ShowAwards()
	{
		AddHeading("AWARDS"); var awards = GetRecords().Where(r => r.isGrammyNominated || r.isGrammyWinner).ToList();
		if (awards.Count == 0) { AddBody("No nominations yet."); return; }
		foreach (var r in awards) AddBody($"{(r.isGrammyWinner ? "GRAMMY WINNER" : "Grammy nominee")} — “{r.baseRecord.title}”");
	}
	private void ShowPersonnel()
	{
		AddHeading("PERSONNEL"); foreach (var p in profile.personnel) AddBody($"{p.name} — {Format(p.role)}\nJoined {p.joinedYear}{(p.isFoundingMember ? "  •  Founding member" : "")}{(!p.isActive ? $"  •  Departed: {p.reasonLeft}" : "")}");
	}
	private void ShowContract()
	{
		AddHeading("INTERNAL — CONTRACT"); AddBody($"Royalty rate: {artist.royaltyRate:P1}\nUnrecouped advance: ${artist.unrecoupedAdvance:N0}\nTerm: {artist.contractLength} years\nExpires: {artist.contractExpiresYear}");
		AddHeading("INTERNAL — TALENT"); AddBody($"Vocals {artist.vocalPower:P0}  •  Musicianship {artist.musicianship:P0}  •  Songwriting {artist.songwritingAbility:P0}\nLive {artist.livePerformance:P0}  •  Studio {artist.studioPerformance:P0}  •  Cohesion {artist.groupCohesion:P0}");
	}
	private List<RecordRuntimeData> GetRecords() => ChartManager.Instance?.GetAllRecords().Where(r => r?.baseRecord?.artistId == profile.artistId).ToList() ?? new();
	private void AddHeading(string text) { var l = new Label { Text = text }; l.AddThemeFontSizeOverride("font_size", 21); l.AddThemeColorOverride("font_color", new Color("5b351f")); content.AddChild(l); }
	private void AddBody(string text) { var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart }; l.AddThemeFontSizeOverride("font_size", 17); content.AddChild(l); }
	private static string Format(object value) { var s = value?.ToString() ?? ""; return string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString())); }
	private static void Clear(Node node) { foreach (Node child in node.GetChildren()) { node.RemoveChild(child); child.QueueFree(); } }
}
