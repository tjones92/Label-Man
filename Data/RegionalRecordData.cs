// Scripts/Data/RegionalRecordData.cs

using System;

[Serializable]
public class RegionalRecordData {
	public string regionId;
	
	// Awareness & Sentiment
	public float awareness;      // 0-1: How many people in this region know about the song
	public float sentiment;      // -1 to 1: How much they like it
	
	// Media Presence
	public float radioPlay;      // 0-1: Current radio airplay level
	public float jukeboxPlay;    // 0-1: Jukebox presence
	
	// Distribution
	public int unitsInStores;    // Physical copies available
	public int unitsBackordered; // Demand that couldn't be met
	
	// Sales Tracking
	public int unitsSoldThisWeek;
	public int unitsSoldTotal;
	
	public RegionalRecordData(string regionId) {
		this.regionId = regionId;
		awareness = 0f;
		sentiment = 0f;
		radioPlay = 0f;
		jukeboxPlay = 0f;
		unitsInStores = 0;
		unitsBackordered = 0;
		unitsSoldThisWeek = 0;
		unitsSoldTotal = 0;
	}
}
