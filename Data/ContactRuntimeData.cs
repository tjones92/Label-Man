using System.Collections.Generic;
using Godot;

public class ContactRuntimeData {
	public Contact baseContact;
	
	public bool isDiscovered;
	public float relationshipScore;
	public RelationshipTier relationshipTier;
	public int daysSinceLastContact;
	public int totalInteractions;
	
	public bool youOweThem;
	public bool theyOweYou;
	public string favorDescription;
	
	public AvailabilityStatus currentStatus;
	public string statusReason;
	
	public float fbiAttention;
	public bool isUnderInvestigation;
	public bool isBurnedBridge;
	
	public List<InteractionRecord> history;
	
	public ContactRuntimeData(Contact contact) {
		baseContact = contact;
		isDiscovered = false;
		relationshipScore = contact.startingRelationship;
		UpdateRelationshipTier();
		daysSinceLastContact = 999;
		totalInteractions = 0;
		youOweThem = false;
		theyOweYou = false;
		favorDescription = "";
		currentStatus = AvailabilityStatus.Available;
		statusReason = "";
		fbiAttention = 0f;
		isUnderInvestigation = false;
		isBurnedBridge = false;
		history = new List<InteractionRecord>();
	}
	
	public void UpdateRelationshipTier() {
		if (isBurnedBridge) relationshipTier = RelationshipTier.Burned;
		else if (relationshipScore < -0.3f) relationshipTier = RelationshipTier.Cold;
		else if (relationshipScore < 0.2f) relationshipTier = RelationshipTier.Acquaintance;
		else if (relationshipScore < 0.5f) relationshipTier = RelationshipTier.Friendly;
		else if (relationshipScore < 0.8f) relationshipTier = RelationshipTier.Loyal;
		else relationshipTier = RelationshipTier.InYourPocket;
	}
	
	public Color RelationshipColor => relationshipTier switch {
		RelationshipTier.Burned => new Color(0.5f, 0f, 0f),
		RelationshipTier.Cold => new Color(0.5f, 0.5f, 0.5f),
		RelationshipTier.Acquaintance => new Color(0.9f, 0.9f, 0.8f),
		RelationshipTier.Friendly => new Color(0.7f, 0.85f, 0.7f),
		RelationshipTier.Loyal => new Color(0.5f, 0.75f, 0.5f),
		RelationshipTier.InYourPocket => new Color(0.9f, 0.8f, 0.4f),
		_ => Colors.White
	};
	
	public string RelationshipLabel => relationshipTier switch {
		RelationshipTier.Burned => "BURNED",
		RelationshipTier.Cold => "Cold",
		RelationshipTier.Acquaintance => "Acquaintance",
		RelationshipTier.Friendly => "Friendly",
		RelationshipTier.Loyal => "Loyal",
		RelationshipTier.InYourPocket => "In Your Pocket",
		_ => "Unknown"
	};
	
	public bool CanCall => !isBurnedBridge && currentStatus == AvailabilityStatus.Available;
}
