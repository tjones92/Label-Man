/// <summary>
/// A concrete, player-legible set of demands the manager produces from a label's baseline offer.
/// This is the visible depth: the Shark's "$18k up front, 8%, label keeps publishing, short term"
/// is drama the moment you see it. The AI accepts-or-declines it; the player sees
/// <see cref="DemandSummary"/> and, for a Pushover act, accepts-or-walks. <see cref="NegotiationDifficulty"/>
/// now drives the Part 2 negotiation scene (see PlayerDesk.ContractNegotiation.cs and
/// SimTools/ContractNegotiationDirective.md): posture, patience, and the room between the ask and
/// what the act will actually take.
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
