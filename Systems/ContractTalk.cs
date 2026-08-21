using System.Collections.Generic;

// ============================================================================================
// CONTRACT NEGOTIATION -- Part 2 of SimTools/ContractNegotiationDirective.md, plus renewal.
//
// The same loop as the Rolodex DJ call (see RolodexCall.cs / RolodexScene.cs / PlayerDesk.
// RolodexVerbs.cs), different nouns: table an offer, they raise ONE objection -- the contract
// axis furthest from what they'd actually take -- and you answer it by sweetening the number,
// trading a term for cash, promising something that costs nothing today, or holding firm and
// seeing whether patience runs out first. Acceptance is not a dice roll: it's a deterministic
// reservation-price check over the same axes already on ContractTermSheet. No new contract
// fields; this is a UI and an acceptance test over what already exists.
//
// The same scene now also drives a RENEWAL: an already-signed act's contract has matured
// (RosterManager.IsContractMatured) and the player is putting new paper in front of them. The
// ask is re-generated off the act's CURRENT stats/manager, so a Star with a Shark now negotiates
// like one, even if they walked in as a Pushover bar band. See PlayerDesk.ContractNegotiation.cs.
// ============================================================================================

/// <summary>How much interaction a signing needs. Most acts are Pushover -- today's single-click
/// accept-or-walk, unchanged. A managed and/or high-drama act opens the negotiation scene.</summary>
public enum NegotiationPosture { Pushover, Firm, Hardball }

/// <summary>The negotiable terms, all already on <see cref="ContractTermSheet"/>.</summary>
public enum ContractAxis { Advance, Royalty, Term, Deliverables, Publishing, CreativeControl }

/// <summary>The answers on offer once they've raised an objection.</summary>
public enum ContractCounter {
	None,
	SweetenAxis,   // go back to the form and raise the named axis
	TradeAxes,     // give back publishing/control, take the advance down in exchange
	Promise,       // more sides, a real push -- no cash now, writes an obligation
	HoldFirm,      // re-table unchanged; softens their number if patience is high, ends it if not
	Walk,          // step back from the table voluntarily
}

/// <summary>Where the scene sits. The UI renders one stage at a time.</summary>
public enum ContractTalkStage { Tabling, Objection, Done }

/// <summary>
/// One negotiation, from the first table to a sign/renew or a walk. Held live on
/// <see cref="PlayerDesk"/> (on the <see cref="PlayerDesk.Prospect"/> for a new signing, or as
/// <see cref="PlayerDesk.PendingRenewal"/> for a renewal) so it survives a UI refresh, the same
/// way <see cref="RolodexCall"/> is held live.
/// </summary>
public sealed class ContractTalk {
	/// <summary>Set for a new signing; null for a renewal, where <see cref="renewalArtist"/> is set
	/// instead. Exactly one of the two is non-null.</summary>
	public PlayerDesk.Prospect prospect;
	public SimulatedArtist renewalArtist;
	public SimulatedArtist Artist => prospect?.Artist ?? renewalArtist;
	public bool IsRenewal => renewalArtist != null;

	public ContractTermSheet ask;                              // the opening ask -- fixed for the whole negotiation
	public NegotiationPosture posture;
	public Dictionary<ContractAxis, float> weights;
	public float reservation;                                  // fraction of the ask's own package value they'll actually take
	public int patienceMax;
	public int patienceLeft;
	public int roundsPlayed;
	public ContractTalkStage stage = ContractTalkStage.Tabling;
	public ContractAxis? objectionAxis;
	public float lastOfferValue;
	public ContractTermSheet? lastOffer;                        // null until the first table
	public readonly List<string> log = new();
}

/// <summary>
/// The renewal-side counterpart to <see cref="PlayerDesk.Prospect"/>: an already-signed act whose
/// contract has matured, with the freshly re-generated ask cached (it's not idempotent -- see
/// <see cref="AILabel.GenerateTermSheet(SimulatedArtist, int)"/> -- so it's drawn once, not on
/// every render). Held live on <see cref="PlayerDesk.PendingRenewal"/>.
/// </summary>
public sealed class RenewalOffer {
	public SimulatedArtist Artist;
	public ContractTermSheet Ask;
	public NegotiationPosture Posture;
	/// <summary>Non-null only once a Firm/Hardball renewal has actually opened the scene.</summary>
	public ContractTalk Talk;
}
