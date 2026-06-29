using System.Collections.Generic;
using System.Text;
using Godot;

public partial class RecordDetailPanel : Control
{
	[ExportGroup("Record Info")]
	[Export] private Label titleText;
	[Export] private Label artistText;
	[Export] private Label labelText;
	[Export] private Label genreText;

	[ExportGroup("Chart Stats")]
	[Export] private Label currentPositionText;
	[Export] private Label peakPositionText;
	[Export] private Label weeksOnChartText;
	[Export] private Label movementText;

	[ExportGroup("Description")]
	[Export] private Label descriptionText;
	[Export] private Label regionalHintText;

	[ExportGroup("Reputation Tags")]
	[Export] private Control tagContainer;
	// Tags created dynamically as Labels, no prefab needed

	[ExportGroup("Artist Section")]
	[Export] private Button viewArtistButton;
	[Export] private Label artistPreviewText;

	[ExportGroup("Actions")]
	[Export] private Button closeButton;

	private RecordRuntimeData currentRecord;
	private List<Control> spawnedTags = new List<Control>();

	public override void _Ready()
	{
		if (closeButton != null)
			closeButton.Pressed += () => Visible = false;

		if (viewArtistButton != null)
			viewArtistButton.Pressed += OnViewArtistClicked;
	}

	public void Populate(RecordRuntimeData record)
	{
		currentRecord = record;
		var baseRecord = record.baseRecord;

		if (titleText != null) titleText.Text = $"\"{baseRecord.title}\"";
		if (artistText != null) artistText.Text = baseRecord.artistName;
		if (labelText != null) labelText.Text = GetLabelDisplayName(baseRecord.labelId);
		if (genreText != null) genreText.Text = FormatGenre(baseRecord.primaryGenre);

		if (currentPositionText != null)
			currentPositionText.Text = record.currentPosition > 0
				? $"#{record.currentPosition}"
				: "Off Chart";

		if (peakPositionText != null)
			peakPositionText.Text = record.peakPosition > 0
				? $"Peak: #{record.peakPosition}"
				: "No peak yet";

		if (weeksOnChartText != null)
			weeksOnChartText.Text = record.weeksOnChart > 0
				? $"{record.weeksOnChart} weeks on chart"
				: "New entry";

		if (movementText != null)
			movementText.Text = JournalisticDescriptor.GetChartMovementComment(record);

		if (descriptionText != null)
			descriptionText.Text = JournalisticDescriptor.DescribeRecord(record);

		if (regionalHintText != null)
		{
			var regions = ChartManager.Instance?.GetAllRegions();
			if (regions != null)
				regionalHintText.Text = JournalisticDescriptor.GetRegionalPerformanceHint(record, regions);
		}

		if (artistPreviewText != null)
			artistPreviewText.Text = $"Click to learn more about {baseRecord.artistName}";

		PopulateTags(record);
	}

	private void PopulateTags(RecordRuntimeData record)
	{
		// Clear old tags
		foreach (var tag in spawnedTags)
		{
			if (tag != null) tag.QueueFree();
		}
		spawnedTags.Clear();

		if (tagContainer == null) return;

		var tags = GenerateRecordTags(record);

		foreach (var tag in tags)
		{
			var tagLabel = new Label();
			tagLabel.Text = tag.ToDisplayString();
			tagLabel.AddThemeColorOverride("font_color", Colors.White);
			tagContainer.AddChild(tagLabel);
			spawnedTags.Add(tagLabel);
		}
	}

	private List<ReputationTag> GenerateRecordTags(RecordRuntimeData record)
	{
		var tags = new List<ReputationTag>();
		var r = record.baseRecord;

		if (record.currentPosition == 1) tags.Add(ReputationTag.HitMachine);
		if (record.isBullet) tags.Add(ReputationTag.RisingStar);
		if (record.peakPosition <= 10 && record.currentPosition > 40) tags.Add(ReputationTag.FadingFast);
		if (record.weeksOnChart >= 15) tags.Add(ReputationTag.MainstreamAppeal);

		if (r.hookStrength > 0.8f) tags.Add(ReputationTag.RadioFriendly);
		if (r.originality > 0.75f) tags.Add(ReputationTag.Innovator);
		if (r.originality < 0.25f) tags.Add(ReputationTag.Derivative);
		if (r.controversy > 0.5f) tags.Add(ReputationTag.Controversial);
		if (r.productionQuality > 0.85f) tags.Add(ReputationTag.Professional);

		if (tags.Count > 4) tags = tags.GetRange(0, 4);

		return tags;
	}

	private string FormatGenre(Genre genre)
	{
		return genre switch {
			Genre.RockAndRoll => "Rock & Roll",
			Genre.DooWop => "Doo-Wop",
			Genre.RnB => "R&B",
			Genre.BritishInvasion => "British Invasion",
			Genre.SurfRock => "Surf Rock",
			Genre.GarageRock => "Garage Rock",
			Genre.FolkRock => "Folk Rock",
			Genre.CountryRock => "Country Rock",
			Genre.AcidRock => "Acid Rock",
			Genre.BluesRock => "Blues Rock",
			Genre.HardRock => "Hard Rock",
			Genre.ProgressiveRock => "Progressive Rock",
			Genre.BaroquePop => "Baroque Pop",
			Genre.SunshinePop => "Sunshine Pop",
			Genre.EasyListening => "Easy Listening",
			Genre.BossaNova => "Bossa Nova",
			Genre.SkaRocksteady => "Ska/Rocksteady",
			Genre.TeenPop => "Teen Pop",
			Genre.GirlGroup => "Girl Group",
			Genre.ProtoMetal => "Proto-Metal",
			Genre.ProtoPunk => "Proto-Punk",
			_ => genre.ToString()
		};
	}

	private string GetLabelDisplayName(string labelId)
	{
		if (string.IsNullOrEmpty(labelId)) return "Unknown Label";

		return labelId switch {
			"capitol" => "Capitol Records",
			"atlantic" => "Atlantic Records",
			"motown" => "Motown Records",
			"columbia" => "Columbia Records",
			"rca" => "RCA Victor",
			"decca" => "Decca Records",
			"mercury" => "Mercury Records",
			"stax" => "Stax Records",
			"chess" => "Chess Records",
			"sun" => "Sun Records",
			"vee_jay" => "Vee-Jay Records",
			"philips" => "Philips Records",
			"imperial" => "Imperial Records",
			"liberty" => "Liberty Records",
			_ => labelId.Replace("_", " ")
		};
	}

	private void OnViewArtistClicked()
	{
		if (currentRecord == null) return;
		GD.Print($"View artist: {currentRecord.baseRecord.artistName}");
		// TODO: Open artist detail panel
	}
}
