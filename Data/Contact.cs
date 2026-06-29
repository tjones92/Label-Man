using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class Contact : Resource {
	
	[ExportGroup("Identity")]
	[Export] public string contactId;
	[Export] public string firstName;
	[Export] public string lastName;
	[Export] public string nickname;
	[Export] public string organization;
	public ContactType contactType;
	[Export] public Texture2D portrait; // Godot uses Texture2D instead of Sprite
	
	[ExportGroup("Location & Reach")]
	[Export] public string primaryCity;
	[Export] public string[] regionsOfInfluence; // Arrays instead of Lists for Godot Export
	[Export] public bool isNational;
	
	[ExportGroup("Professional Stats")]
	[Export(PropertyHint.Range, "0,1")] public float influence = 0.5f;
	[Export(PropertyHint.Range, "0,1")] public float reliability = 0.5f;
	[Export(PropertyHint.Range, "0,1")] public float corruptibility = 0.5f;
	[Export] public string[] genrePreferences;
	[Export] public string[] genreAversions;
	
	[ExportGroup("Starting Relationship")]
	[Export(PropertyHint.Range, "-1,1")] public float startingRelationship = 0f;
	
	[ExportGroup("Availability")]
	[Export(PropertyHint.Range, "0,1")] public float baseReachability = 0.7f;
	[Export] public int yearAvailableFrom = 1960;
	[Export] public int yearAvailableTo = 1969;
	
	public string FullName => !string.IsNullOrEmpty(nickname) ? $"{firstName} \"{nickname}\" {lastName}" : $"{firstName} {lastName}";
	
	public string DisplayName => !string.IsNullOrEmpty(nickname) ? nickname : $"{firstName} {lastName}";
	
	public ContactCategory Category => contactType switch {
		ContactType.Producer => ContactCategory.Creative,
		ContactType.SessionMusician => ContactCategory.Creative,
		ContactType.Songwriter => ContactCategory.Creative,
		ContactType.DJ_Local => ContactCategory.Promotion,
		ContactType.DJ_National => ContactCategory.Promotion,
		ContactType.Journalist_Trade => ContactCategory.Promotion,
		ContactType.Journalist_Consumer => ContactCategory.Promotion,
		ContactType.TVBooker => ContactCategory.Promotion,
		ContactType.Publicist => ContactCategory.Promotion,
		ContactType.PressingPlant => ContactCategory.Distribution,
		ContactType.Distributor => ContactCategory.Distribution,
		ContactType.JukeboxOperator => ContactCategory.Distribution,
		ContactType.RetailBuyer => ContactCategory.Distribution,
		ContactType.Lawyer => ContactCategory.Business,
		ContactType.Banker => ContactCategory.Business,
		ContactType.LabelExecutive => ContactCategory.Business,
		ContactType.TalentScout => ContactCategory.Talent,
		ContactType.ArtistManager => ContactCategory.Talent,
		ContactType.Artist => ContactCategory.Talent,
		ContactType.MobContact => ContactCategory.Underground,
		ContactType.Fixer => ContactCategory.Underground,
		_ => ContactCategory.Business
	};
	
	public string TypeDisplayName => contactType switch {
		ContactType.DJ_Local => "Local DJ",
		ContactType.DJ_National => "National DJ",
		ContactType.Journalist_Trade => "Trade Press",
		ContactType.Journalist_Consumer => "Consumer Press",
		ContactType.TVBooker => "TV Booker",
		ContactType.PressingPlant => "Pressing Plant",
		ContactType.JukeboxOperator => "Jukebox Operator",
		ContactType.RetailBuyer => "Retail Buyer",
		ContactType.LabelExecutive => "Label Executive",
		ContactType.TalentScout => "Talent Scout",
		ContactType.ArtistManager => "Artist Manager",
		ContactType.MobContact => "Connected Guy",
		ContactType.Fixer => "Problem Solver",
		ContactType.SessionMusician => "Session Player",
		_ => contactType.ToString()
	};
	
	public Color CategoryColor => Category switch {
		ContactCategory.Creative => new Color(0.6f, 0.4f, 0.8f),
		ContactCategory.Promotion => new Color(0.9f, 0.6f, 0.2f),
		ContactCategory.Distribution => new Color(0.3f, 0.6f, 0.8f),
		ContactCategory.Business => new Color(0.4f, 0.5f, 0.4f),
		ContactCategory.Talent => new Color(0.8f, 0.3f, 0.5f),
		ContactCategory.Underground => new Color(0.3f, 0.3f, 0.3f),
		_ => Colors.Gray
	};
}
