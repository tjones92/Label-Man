using System.Collections.Generic;

[System.Serializable]
public class LabelPublicProfile
{
	public string labelId;
	public string labelName;
	public string founderName;
	public string headquartersCity;
	public LabelArchetype archetype;
	public LabelTier tier;
	public int foundedYear;
	public Genre[] preferredGenres;
	public int totalReleases;
	public int top40Hits;
	public int numberOneHits;
	public List<string> rosterArtistNames = new();
	public string statusImpression;
	public string descriptionBlurb;
}
