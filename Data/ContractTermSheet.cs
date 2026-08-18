/// <summary>
/// A concrete, player-legible set of demands the manager produces from a label's baseline offer.
/// This is the visible depth: the Shark's "$18k up front, 8%, label keeps publishing, short term"
/// is drama the moment you see it, before any counter-offer minigame exists. The AI accepts-or-
/// declines it; the player sees <see cref="DemandSummary"/> and accepts-or-walks.
/// <see cref="NegotiationDifficulty"/> is stored now, unused, ready for that later minigame.
/// </summary>
public readonly struct ContractTermSheet {
	public readonly float Advance;
	public readonly float RoyaltyRate;
	public readonly int TermYears;
	public readonly int SinglesObligation;
	public readonly bool LabelOwnsPublishing;
	public readonly bool ArtistCreativeControl;
	public readonly float NegotiationDifficulty;   // 0 easy .. 1 brutal (drives future minigame)
	public readonly ManagerArchetype Manager;
	public readonly string ManagerName;
	public readonly string DemandSummary;          // player-facing one-liner

	public ContractTermSheet(float advance, float royalty, int term, int singles, bool labelPub,
		bool artistControl, float difficulty, ManagerArchetype manager, string managerName, string summary) {
		Advance = advance; RoyaltyRate = royalty; TermYears = term; SinglesObligation = singles;
		LabelOwnsPublishing = labelPub; ArtistCreativeControl = artistControl;
		NegotiationDifficulty = difficulty; Manager = manager; ManagerName = managerName; DemandSummary = summary;
	}
}
