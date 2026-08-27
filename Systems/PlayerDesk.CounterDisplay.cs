using Godot;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §9 -- THE COUNTER AND THE WINDOW.
//
// Small, cheap, and the reason a serviced dealer outsells an unserviced one. Both verbs only ever
// touch a stop the player already has real history with -- a window card needs the record already
// placed there, an in-store appearance needs the act to have some real local standing -- exactly the
// directive's own framing ("it is the second thing you do in a town, not the first").
// ============================================================================================
public partial class PlayerDesk : Node {

	public const int WindowCardMinutes = 60;
	public const int WindowCardMaxPromoCopies = 2;
	private const float WindowCardCostMin = 8f;
	private const float WindowCardCostMax = 20f;
	// Bounded lift folded into ProcessTrunkDay's own sell-through appeal term while live -- see
	// PlayerDesk.cs. Never a source of units on its own, just a reason a serviced dealer outsells one.
	public const float WindowCardSellThroughBoost = 0.10f;
	private const int WindowCardWeeks = 6;

	/// <summary>Directive §9: "window cards / counter display -- a per-city print buy (~$8-20) plus a
	/// promo copy or two... a bounded lift on that stop's sell-through appeal term and a relationship
	/// tick." Needs the record already placed here -- a card in an empty window sells nothing.</summary>
	public bool BuyWindowCard(string stopId, string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Shop) { message = "Not a shop with a window to dress."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town to put a card up."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single for the window."; return false; }
		if (!stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot) || lot.Placed <= 0) {
			message = $"{stop.DisplayName} isn't carrying \"{TitleForRecord(recordId)}\" yet -- place it first.";
			return false;
		}
		float cost = Mathf.Round((float)GD.RandRange(WindowCardCostMin, WindowCardCostMax));
		if (Label.cashReserves < cost) { message = $"You're ${cost - Label.cashReserves:N0} short of the ${cost:N0} print run."; return false; }
		if (TimeManager.Instance?.CanAffordMinutes(WindowCardMinutes) != true) { message = "Not enough of the day left."; return false; }
		PressStock stockOnHand = StockFor(recordId);
		int promoSweetener = Mathf.Min(WindowCardMaxPromoCopies, stockOnHand?.PromoRemaining ?? 0);

		SpendMinutes(WindowCardMinutes);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		if (promoSweetener > 0 && stockOnHand != null) stockOnHand.PromoRemaining -= promoSweetener;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		lot.WindowCardExpiresWeek = week + WindowCardWeeks - 1;
		TouchStop(stop, 0.04f);

		string title = TitleForRecord(recordId);
		Note($"Put a window card up for \"{title}\" at {stop.DisplayName} for ${cost:N0}"
			+ (promoSweetener > 0 ? $", and left {promoSweetener} promo cop{(promoSweetener == 1 ? "y" : "ies")} as a sweetener." : "."));
		message = $"Card's up at {stop.DisplayName} -- ${cost:N0}, good for about {WindowCardWeeks} weeks.";
		Changed?.Invoke();
		return true;
	}

	public const int InStoreAppearanceHours = 6; // the act's day, the player's day
	private const float InStoreRelationshipGain = 0.15f;
	private const float InStoreAwareness = 0.015f;
	// "Requires an act with some local standing" -- reuses the same RegionalBuzz read the daily trunk
	// sell-through itself trusts, rather than inventing a second local-fame number.
	private const float InStoreLocalStandingBar = 0.18f;
	private const float InStorePlacementMultiplier = 2.5f;

	/// <summary>Directive §9: "the act signs copies on a Saturday... a real spike in that stop's Placed
	/// lot and a jump in the stop's relationship, plus a small regional awareness write. Requires an act
	/// with some local standing, so it is the second thing you do in a town, not the first."</summary>
	public bool BookInStoreAppearance(string stopId, string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Shop) { message = "Not a shop that can host a signing."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town for that."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to sign."; return false; }
		if (!stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot) || lot.Placed <= 0) {
			message = $"{stop.DisplayName} isn't carrying \"{TitleForRecord(recordId)}\" yet -- this is the second thing you do here, not the first.";
			return false;
		}
		RecordRuntimeData rec = FindReleasedRecord(recordId);
		if (rec?.baseRecord == null) { message = "Can't book an appearance for that one."; return false; }
		if (ArtistManager.Instance?.GetArtist(rec.baseRecord.artistId) == null) { message = "No act on record for that single -- nobody to sign."; return false; }
		if (RegionalBuzz(rec, stop.CityId) < InStoreLocalStandingBar) {
			message = $"Nobody in {CityName(stop.CityId)} knows the act well enough yet to draw a line.";
			return false;
		}
		PressStock stockOnHand = StockFor(recordId);
		if (stockOnHand == null || stockOnHand.Remaining <= 0) { message = "None of that pressed on hand -- order a run and let it come in first."; return false; }
		if (!Require(InStoreAppearanceHours, out message)) return false;

		Spend(InStoreAppearanceHours);
		int qty = Mathf.Min(Mathf.RoundToInt(SuggestedPlacement(stop) * InStorePlacementMultiplier), stockOnHand.Remaining);
		stockOnHand.Remaining -= qty;
		lot.Remaining += qty;
		lot.Placed += qty;
		lot.DaysSinceRestock = 0;
		TouchStop(stop, InStoreRelationshipGain);
		string regionId = DistanceModel.GetCityById(stop.CityId)?.parentRegionId;
		if (!string.IsNullOrEmpty(regionId)) ChartManager.Instance?.AddAwareness(recordId, regionId, InStoreAwareness);

		string title = TitleForRecord(recordId);
		Note($"The act signed copies of \"{title}\" at {stop.DisplayName} on a Saturday -- {qty:N0} more on the shelf and a line out the door.");
		message = $"Signing at {stop.DisplayName} went well -- {qty:N0} more of \"{title}\" on hand there now.";
		Changed?.Invoke();
		return true;
	}
}
