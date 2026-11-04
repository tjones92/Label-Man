using System;

/// <summary>Finite anonymous title audience. Enthusiasts overlap owners/nonowners;
/// they are not an additional purchase pool. Pure research state transition.</summary>
public sealed class BoundedAudienceState {
	public double Capacity { get; }
	public double Interested { get; private set; }
	public double Owners { get; private set; }
	public double Enthusiasts { get; private set; }
	public double Intent { get; private set; }
	public double Acquired { get; private set; }
	public double NewEnthusiasts { get; private set; }
	public double BeforeInterest { get; private set; }
	public double BeforeOwners { get; private set; }
	public double BeforeEnthusiasts { get; private set; }
	private bool pending;
	public BoundedAudienceState(double capacity, double initialAware) {
		if (!double.IsFinite(capacity) || capacity < 0) throw new ArgumentException("Invalid audience capacity");
		Capacity = capacity; Interested = capacity * Math.Clamp(initialAware, 0, 1);
	}
	public double Prepare(double listeningContacts, double externalContacts, double enjoyment, double purchaseRate) {
		if (pending) throw new InvalidOperationException("Audience demand prepared twice before settlement");
		if (!double.IsFinite(listeningContacts + externalContacts + enjoyment + purchaseRate)
			|| listeningContacts < 0 || externalContacts < 0 || purchaseRate < 0)
			throw new ArgumentException("Invalid audience hazards");
		BeforeInterest = Interested; BeforeOwners = Owners; BeforeEnthusiasts = Enthusiasts;
		double response = Math.Clamp(enjoyment, 0, 1);
		Acquired = Math.Max(0, Capacity - Owners - Interested) * (1 - Math.Exp(-externalContacts - response * listeningContacts));
		Interested += Acquired;
		Enthusiasts *= .75;
		NewEnthusiasts = (Capacity - Enthusiasts) * (1 - Math.Exp(-.35 * response * listeningContacts));
		Enthusiasts += NewEnthusiasts;
		Intent = Interested * (1 - Math.Exp(-purchaseRate));
		pending = true;
		return Intent;
	}
	public void Settle(double actualPurchases) {
		if (!pending) throw new InvalidOperationException("Audience settlement without demand");
		if (!double.IsFinite(actualPurchases) || actualPurchases < 0 || actualPurchases > Intent + 1e-5)
			throw new InvalidOperationException("Audience purchases exceed finite intent");
		Owners += actualPurchases;
		Interested = Math.Max(0, Interested - actualPurchases) * .9;
		pending = false;
		if (Owners + Interested > Capacity + 1e-5 || Enthusiasts > Capacity + 1e-5)
			throw new InvalidOperationException("Audience conservation failed");
	}
}
