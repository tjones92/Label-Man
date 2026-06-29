// Scripts/Data/InteractionRecord.cs

[System.Serializable]
public class InteractionRecord {
	public int gameDay;
	public string interactionType;
	public string summary;
	public float relationshipChange;
	
	public InteractionRecord(int day, string type, string summary, float change = 0f) {
		this.gameDay = day;
		this.interactionType = type;
		this.summary = summary;
		this.relationshipChange = change;
	}
}
