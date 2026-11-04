using Godot;

/// <summary>
/// Opening cash by tier, in 1960 dollars. The 1960 world (<see cref="AILabelFactory"/>) and runtime births
/// (<see cref="LabelGenerator"/>) both read this one table. Each kept its own copy before, and the births copy
/// carried inverted Major ($5k-15k) and MidTier ($500-2,000) rows that were unreachable only because births are
/// never those tiers.
///
/// Every tier consumes exactly one uniform draw and reproduces the former flat uniform tables value-for-value.
///
/// Small stays at $1,000-8,000 although documented micro-label founding stakes were lower: Vee-Jay borrowed $500
/// from a pawnbroker (1953), Tamla $800 from the Gordy family fund (1959), and Satellite, later Stax, raised $2,500
/// against Estelle Axton's house (1958). A log-uniform $400-5,000 draw was tried (SmallLabelAttritionAndChartMethod.md
/// section 8). It closed 36% of starting Small labels in 1960 at control prices and 47-55% under dealer-price
/// pressing, and 98% of those closures had never released a record. A first release here needs roughly $1,300-1,500
/// (an advance, the 2x-overhead signing reserve, production, marketing and a month's reserve), and a Small label
/// pays $150 + $30/artist a month whether or not it releases. Real founders spent the whole stake on the first
/// session and ran on pressing-plant and distributor credit with near-zero fixed cost. Lower opening cash is only
/// realistic together with that cost and credit structure.
/// </summary>
public static class LabelStartingFinances {
	/// <summary>Opening cash for <paramref name="tier"/> from one uniform draw in [0, 1].</summary>
	public static float Cash(LabelTier tier, double uniform) {
		double u = Mathf.Clamp(uniform, 0.0, 1.0);
		(double min, double max) = tier switch {
			LabelTier.Major => (50000.0, 200000.0),
			LabelTier.MidTier => (15000.0, 60000.0),
			LabelTier.Independent => (5000.0, 20000.0),
			LabelTier.Boutique => (3000.0, 15000.0),
			LabelTier.Small => (1000.0, 8000.0),
			_ => (5000.0, 5000.0)
		};
		// Same arithmetic as GameRandom.RandRange(min, max), so these tiers reproduce their former values exactly.
		return (float)((u * (max - min)) + min);
	}

	/// <summary>Draws opening cash for <paramref name="tier"/> from the shared stream (one draw).</summary>
	public static float DrawCash(LabelTier tier) => Cash(tier, GameRandom.RandRange(0.0, 1.0));
}
