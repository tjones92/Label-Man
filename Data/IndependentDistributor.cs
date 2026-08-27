using System;
using System.Collections.Generic;

/// <summary>
/// A regional independent record distributor -- the wholesale layer that actually
/// carried independent labels' lines into shops in the 1960s.
///
/// This is deliberately NOT an <see cref="AILabel"/>. Before this existed the only
/// route to national reach was a distribution deal with a bigger label, so every
/// charting independent was by construction somebody's client and the major-owned
/// chart share had a structural floor no calibration could move (handoff section
/// 32.5). An independent distributor grants physical <em>coverage</em> and nothing
/// else: no <see cref="DistributionDeal"/>, no borrowed reach, no master ownership,
/// no owner attribution. A label distributed this way keeps its masters and charts
/// as an independent belonging to nobody.
///
/// Distribution was segregated by geography rather than by label or content -- shops
/// preferred dealing with a handful of houses, so each distributor carried many
/// labels' lines within its own market and a label assembled national coverage one
/// market at a time.
/// </summary>
[Serializable]
public sealed class IndependentDistributor {
	public string distributorId;
	public string distributorName;

	/// <summary>The single market this house services. Coverage is granted for this region only.</summary>
	public string regionId;

	/// <summary>
	/// Label lines this house can carry at once. Generous by design: independent
	/// distribution was not the scarce thing in the 1960s -- having a hit was. The
	/// major-client ceiling of 24 saturated on every seed and froze the whole market
	/// (section 32.2); this capacity exists to be observable, not to bind.
	/// </summary>
	public int clientCapacity;

	/// <summary>Probability this house actually pays for what it sells. Worse in harder markets.</summary>
	public float reliability;

	/// <summary>
	/// Weeks between shipment and payment. Distributors took 90-120 day terms while the
	/// label paid pressing up front -- the squeeze that could bankrupt a small label on a
	/// hit and made a major's P&amp;D offer attractive (section 33.1 stage 3).
	/// </summary>
	public int paymentTermWeeks;

	/// <summary>Share of shipped units returnable unsold under full return privileges.</summary>
	public float returnAllowance;

	/// <summary>Share of units actually reported back to the label. Under-reporting was endemic.</summary>
	public float reportingHonesty;

	// Not readonly: deserialized whole by the full-world save (System.Text.Json can't set a readonly field).
	public HashSet<string> clientLabelIds = new(StringComparer.Ordinal);

	public int CurrentClientCount => clientLabelIds.Count;
	public bool HasCapacity => clientLabelIds.Count < clientCapacity;
	public bool CarriesLabel(string labelId) =>
		!string.IsNullOrEmpty(labelId) && clientLabelIds.Contains(labelId);

	public bool AddClient(string labelId) {
		if (string.IsNullOrEmpty(labelId) || !HasCapacity) return false;
		return clientLabelIds.Add(labelId);
	}

	public bool RemoveClient(string labelId) =>
		!string.IsNullOrEmpty(labelId) && clientLabelIds.Remove(labelId);
}

/// <summary>
/// One week's wholesale billing to one house, payable when the house's terms run out.
/// </summary>
[Serializable]
public struct WholesaleReceivable {
	public int DueWeek;
	public string DistributorId;
	public float Amount;

	public WholesaleReceivable(int dueWeek, string distributorId, float amount) {
		DueWeek = dueWeek;
		DistributorId = distributorId;
		Amount = amount;
	}
}

/// <summary>
/// Dealer-margin-and-flip directive §4, R3: the distributor's real returns mechanism -- a HOLD ON
/// CASH the house books alongside a wholesale billing, never a second charge against units already
/// billed (CompetitorManager.DeferWholesaleBillings's own long-standing comment is correct that
/// charging returnAllowance against billed there would take the same loss twice). Rides next to the
/// WholesaleReceivable it was booked with and matures on the same week, but settles on its own
/// survive-or-die test (ReleaseOrForfeitWholesaleReturnsReserves) rather than the receivable's
/// reliability roll. Billing here is a region-week aggregate, not a single record's, so "is the
/// record still selling" (the directive's framing) becomes "is the label still shipping through
/// this house in this region" -- the closest thing the settlement actually tracks at this
/// granularity. Player-only in practice: DeferWholesaleBillings only ever adds one of these when
/// label.isPlayerOwned, so an AI label's list stays permanently empty.
/// </summary>
[Serializable]
public struct WholesaleReturnsReserve {
	public int ReleaseWeek;
	public string DistributorId;
	public string RegionId;
	public float Amount;

	public WholesaleReturnsReserve(int releaseWeek, string distributorId, string regionId, float amount) {
		ReleaseWeek = releaseWeek;
		DistributorId = distributorId;
		RegionId = regionId;
		Amount = amount;
	}
}
