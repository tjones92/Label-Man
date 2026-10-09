using System.Collections.Generic;

[System.Serializable]
public class ArtistPublicProfile
{
	public string artistId;
	public string name;
	public ArtistType artistType;
	public bool isBand;
	public string homeRegion;
	// Display formation hometown independently of the mutable working base and sales region.
	public string homeCity;
	public string baseCity;
	public string formationPlaceId;
	public string basePlaceId;
	public PlaceEvidence originEvidence;
	public PlaceEvidence baseEvidence;
	public Genre primaryGenre;
	public Genre secondaryGenre;
	public int formedYear;
	public CareerState careerState;
	public string labelId;
	public string labelName;
	public int totalCharted;
	public int top40Hits;
	public int top10Hits;
	public int numberOneHits;
	public int highestPosition;
	public int totalWeeksOnChart;
	public int totalRecordsReleased;
	public List<ReputationTag> reputationTags = new();
	public List<ArtistPersonnelProfile> personnel = new();
}

[System.Serializable]
public class ArtistPersonnelProfile
{
	public string name;
	public MusicianRole role;
	public int joinedYear;
	public bool isFoundingMember;
	public bool isActive;
	public string reasonLeft;
}
