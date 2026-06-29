using Godot;

[GlobalClass]
public partial class MediaInfrastructure : Resource {
	[Export] public int totalRadioStations;
	[Export] public bool hasTop40Stations;
	[Export] public bool hasRnBStations;
	[Export] public bool hasCountryStations;
	[Export] public bool hasFMUnderground;
	[Export(PropertyHint.Range, "0,1")] public float radioReach;
	[Export(PropertyHint.Range, "0,1")] public float payolaSusceptibility;
	[Export] public int tvMarketRank;
	[Export] public bool hasLocalMusicShow;
	[Export(PropertyHint.Range, "0,1")] public float bandstandReach;
	[Export] public int jukeboxCount;
	[Export] public int concertVenueCount;
	
	public MediaInfrastructure() {}
}
